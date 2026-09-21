-- ============================================================================
-- TEST: Installment due / almost-due notification logic
-- ============================================================================
-- Seeds a sample customer with 5 installments that exercise every branch of
-- InstallmentNotificationService.Synchronize():
--
--   1) Overdue     -> NextDueDate < today            -> type 'installment-overdue'
--   2) Due today   -> NextDueDate == today           -> type 'installment-due-soon'
--   3) Due soon    -> NextDueDate within 7 days      -> type 'installment-due-soon'
--   4) Not due yet -> NextDueDate > 7 days from now  -> NO notification
--   5) Completed   -> Status = 'Completed'           -> NO notification
--
-- Next due date is ALWAYS:  StartDate + (MonthsPaid + 1) months
--
-- Usage:
--   1) Run this script against RJTechInventory to seed + see the projections.
--   2) Open the app, log in, and open the notification bell / page.
--   3) The seeded installments should appear as notifications.
--   4) To remove the seeded data afterward, run the CLEANUP section at the end.
-- ============================================================================
SET NOCOUNT ON;

DECLARE @today      DATE = CAST(GETDATE() AS DATE);

-- Relative StartDates that produce each case regardless of the run date.
DECLARE @overdueStart    DATE = DATEADD(month, -2, @today);   -- due ~1 month ago
DECLARE @dueTodayStart   DATE = DATEADD(month, -1, @today);   -- due ~today
DECLARE @dueSoonStart    DATE = DATEADD(day, -24, @today);    -- due ~6 days from now
DECLARE @notDueStart     DATE = DATEADD(day, -10, @today);    -- due ~20 days from now
DECLARE @completedStart  DATE = DATEADD(month, -6, @today);   -- finished

-- ----------------------------------------------------------------------------
-- 1. SEED DATA
-- ----------------------------------------------------------------------------
IF EXISTS (SELECT 1 FROM tblCustomer WHERE customer_Email = 'installmentnotiftest@gmail.com')
BEGIN
    PRINT 'Test customer already exists — skipping seed. Run CLEANUP first to reseed.';
END
ELSE
BEGIN
    DECLARE @customerID  INT;
    DECLARE @overdueChk  INT, @dueTodayChk INT, @dueSoonChk INT, @notDueChk INT, @completedChk INT;

    INSERT INTO tblCustomer (customer_FullName, customer_Email, customer_Phone, customer_Address)
    VALUES ('Test Installment Customer', 'installmentnotiftest@gmail.com', '09171234567', 'Test Address 123');
    SET @customerID = SCOPE_IDENTITY();

    -- One checkout per installment (installment sales use Status = 'Ongoing').
    INSERT INTO tblCheckout (CustomerID, TotalAmount, PaymentMethod, PaymentType, DatePurchased, Status)
    VALUES (@customerID, 12000.00, 'G-Cash', 'Installment', @overdueStart,   'Ongoing');
    SET @overdueChk  = SCOPE_IDENTITY();
    INSERT INTO tblCheckout (CustomerID, TotalAmount, PaymentMethod, PaymentType, DatePurchased, Status)
    VALUES (@customerID, 12000.00, 'G-Cash', 'Installment', @dueTodayStart, 'Ongoing');
    SET @dueTodayChk = SCOPE_IDENTITY();
    INSERT INTO tblCheckout (CustomerID, TotalAmount, PaymentMethod, PaymentType, DatePurchased, Status)
    VALUES (@customerID, 12000.00, 'G-Cash', 'Installment', @dueSoonStart,  'Ongoing');
    SET @dueSoonChk  = SCOPE_IDENTITY();
    INSERT INTO tblCheckout (CustomerID, TotalAmount, PaymentMethod, PaymentType, DatePurchased, Status)
    VALUES (@customerID, 12000.00, 'G-Cash', 'Installment', @notDueStart,   'Ongoing');
    SET @notDueChk   = SCOPE_IDENTITY();
    INSERT INTO tblCheckout (CustomerID, TotalAmount, PaymentMethod, PaymentType, DatePurchased, Status)
    VALUES (@customerID, 12000.00, 'G-Cash', 'Installment', @completedStart,'Ongoing');
    SET @completedChk = SCOPE_IDENTITY();

    -- Installments (columns: CheckoutID, Months, DownPayment, InterestRate, TotalAmount,
    --                        Balance, MonthlyPayment, MonthsPaid, MonthsRemaining, StartDate, Status)
    INSERT INTO tblInstallment (CheckoutID, Months, DownPayment, InterestRate, TotalAmount, Balance, MonthlyPayment, MonthsPaid, MonthsRemaining, StartDate, Status)
    VALUES (@overdueChk,    12, 1000.00, 5.00, 12000.00, 12000.00, 1000.00, 0, 12, @overdueStart,    'Overdue'),
           (@dueTodayChk,   12, 1000.00, 5.00, 12000.00, 11000.00, 1000.00, 0, 12, @dueTodayStart,   'Active'),
           (@dueSoonChk,    12, 1000.00, 5.00, 12000.00, 11000.00, 1000.00, 0, 12, @dueSoonStart,    'Active'),
           (@notDueChk,     12, 1000.00, 5.00, 12000.00, 11000.00, 1000.00, 0, 12, @notDueStart,     'Active'),
           (@completedChk,  12, 1000.00, 5.00, 12000.00, 0.00,      1000.00, 6,  6,  @completedStart, 'Completed');

    PRINT 'Seeded 5 test installments for Test Installment Customer.';
END

-- ----------------------------------------------------------------------------
-- 2. DIAGNOSTIC: what the notification service SHOULD produce right now
-- ----------------------------------------------------------------------------
SELECT
    i.InstallmentID,
    'INS-' + RIGHT('000' + CAST(i.InstallmentID AS varchar(10)), 3) AS DisplayID,
    c.customer_FullName,
    i.Status,
    CAST(DATEADD(month, i.MonthsPaid + 1, i.StartDate) AS DATE) AS NextDueDate,
    DATEDIFF(day, @today, CAST(DATEADD(month, i.MonthsPaid + 1, i.StartDate) AS DATE)) AS DaysUntilDue,
    CASE
        WHEN i.Status IN ('Completed', 'Cancelled') OR i.Balance <= 0 THEN '(none - finished)'
        WHEN CAST(DATEADD(month, i.MonthsPaid + 1, i.StartDate) AS DATE) < @today THEN 'installment-overdue'
        WHEN DATEDIFF(day, @today, CAST(DATEADD(month, i.MonthsPaid + 1, i.StartDate) AS DATE)) <= 7 THEN 'installment-due-soon'
        ELSE '(none - more than 7 days out)'
    END AS ProjectedNotificationType
FROM tblInstallment i
JOIN tblCheckout ch ON ch.CheckoutID = i.CheckoutID
JOIN tblCustomer c   ON c.customer_ID = ch.CustomerID
WHERE c.customer_Email = 'installmentnotiftest@gmail.com'
ORDER BY i.InstallmentID;

-- ----------------------------------------------------------------------------
-- 3. CLEANUP (run manually when done testing)
--    Removes the seeded customer, its checkouts and installments, plus any
--    notifications the service created for those installments.
-- ----------------------------------------------------------------------------
-- DELETE n
-- FROM Notifications n
-- WHERE n.action_url LIKE '/Installment/Details/%'
--   AND n.action_url IN (
--       SELECT '/Installment/Details/' + CAST(i.InstallmentID AS varchar(10))
--       FROM tblInstallment i
--       JOIN tblCheckout ch ON ch.CheckoutID = i.CheckoutID
--       JOIN tblCustomer c   ON c.customer_ID = ch.CustomerID
--       WHERE c.customer_Email = 'installmentnotiftest@gmail.com');
-- DELETE ip
-- FROM tblInstallmentPayment ip
-- JOIN tblInstallment i ON i.InstallmentID = ip.InstallmentID
-- JOIN tblCheckout ch   ON ch.CheckoutID = i.CheckoutID
-- JOIN tblCustomer c    ON c.customer_ID = ch.CustomerID
-- WHERE c.customer_Email = 'installmentnotiftest@gmail.com';
-- DELETE i
-- FROM tblInstallment i
-- JOIN tblCheckout ch ON ch.CheckoutID = i.CheckoutID
-- JOIN tblCustomer c  ON c.customer_ID = ch.CustomerID
-- WHERE c.customer_Email = 'installmentnotiftest@gmail.com';
-- DELETE ch
-- FROM tblCheckout ch
-- JOIN tblCustomer c ON c.customer_ID = ch.CustomerID
-- WHERE c.customer_Email = 'installmentnotiftest@gmail.com';
-- DELETE FROM tblCustomer WHERE customer_Email = 'installmentnotiftest@gmail.com';