-- ============================================================================
-- SEED DEMO DATA: 1 product · 1 delivery (received) · 1 sale · 1 OVERDUE installment
-- ============================================================================
-- Mirrors the real app flow:
--   * Product defined under a category.
--   * A delivery receives 10 units (BATCH-001).
--   * An installment sale sells 1 unit to a customer ~3 months ago.
--   * The installment (6 months, 20% down, 5%/mo interest) has had NO payments,
--     so its next due date is already 2 months past -> the app will raise an
--     "Installment overdue" notification.
--
-- Formula used by InstallmentService.Calculate (₱4,500 / 6 months):
--   DownPayment      = 4,500 * 20%              =   900.00
--   RemainingPrincipal= 4,500 - 900             = 3,600.00
--   InterestAmount   = 3,600 * 5% * 6           = 1,080.00
--   InstallmentTotal = 3,600 + 1,080            = 4,680.00
--   MonthlyPayment   = 4,680 / 6                =   780.00
-- ============================================================================
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;

IF EXISTS (SELECT 1 FROM Products) OR EXISTS (SELECT 1 FROM tblCustomer)
BEGIN
    PRINT 'Seed skipped - the database already contains data. Run the reset script first if you want a clean demo.';
END
ELSE
BEGIN
    DECLARE @categoryID INT, @productID INT, @customerID INT,
            @checkoutID INT, @deliveryID INT;

    -- Purchase/start 3 months ago, delivery ~3.5 months ago (stock arrived first).
    DECLARE @purchaseDate DATETIME = DATEADD(month, -3, GETDATE());
    DECLARE @deliveryDate DATETIME = DATEADD(day, -105, GETDATE());

    -- 1) Category -------------------------------------------------------------
    INSERT INTO ProductCategories (category_name) VALUES ('Computer Accessories');
    SET @categoryID = SCOPE_IDENTITY();

    -- 2) Product (10 received, 1 sold -> 9 in stock) --------------------------
    INSERT INTO Products (product_name, product_brand, product_description, product_quantity, reorder_level, Product_price, product_status, category_ID)
    VALUES ('Mechanical Keyboard', 'Keychron',
            'RGB backlit mechanical keyboard with hot-swappable switches.',
            9, 5, 4500.00, 'In Stock', @categoryID);
    SET @productID = SCOPE_IDENTITY();

    -- 3) Customer -------------------------------------------------------------
    INSERT INTO tblCustomer (customer_FullName, customer_Email, customer_Phone, customer_Address)
    VALUES ('Juan Dela Cruz', 'juan.delacruz@gmail.com', '09171234567', '123 Mabini St., Brgy. San Isidro, Manila');
    SET @customerID = SCOPE_IDENTITY();

    -- 4) Checkout (the sale) --------------------------------------------------
    INSERT INTO tblCheckout (CustomerID, TotalAmount, PaymentMethod, PaymentType, DatePurchased, Status)
    VALUES (@customerID, 4500.00, 'G-Cash', 'Installment', @purchaseDate, 'Ongoing');
    SET @checkoutID = SCOPE_IDENTITY();

    -- 5) Checkout item (1 unit) ----------------------------------------------
    INSERT INTO tblCheckoutItem (CheckoutID, ProductID, SerialNo, ItemQuantity, Price, SubTotal)
    VALUES (@checkoutID, @productID, NULL, 1, 4500.00, 4500.00);

    -- 6) Installment (OVERDUE - zero payments after 3 months) -----------------
    INSERT INTO tblInstallment (CheckoutID, Months, DownPayment, InterestRate, TotalAmount, Balance, MonthlyPayment, MonthsPaid, MonthsRemaining, StartDate, Status)
    VALUES (@checkoutID, 6, 900.00, 5.00, 4680.00, 4680.00, 780.00, 0, 6, @purchaseDate, 'Active');

    -- 7) Purchase history receipt (the ₱900 down payment) ----------------------
    INSERT INTO tblCustomerPurchaseHistory (CustomerID, CheckoutID, PurchaseDate, TotalAmount, PaymentMethod)
    VALUES (@customerID, @checkoutID, @purchaseDate, 900.00, 'G-Cash');

    -- 8) Delivery --------------------------------------------------------------
    INSERT INTO Deliveries (date_delivered, received_by, batch_ID, is_archived)
    VALUES (@deliveryDate, 'RJTech Admin', 'BATCH-001', 0);
    SET @deliveryID = SCOPE_IDENTITY();

    -- 9) Delivery details (10 units received) -----------------------------------
    INSERT INTO DeliveryDetails (product_quantity, previous_quantity, new_quantity, product_ID, delivery_ID)
    VALUES (10, 0, 10, @productID, @deliveryID);

    PRINT 'Demo data seeded.';
END

-- ============================================================================
-- VERIFY SEED + projected notification
-- ============================================================================
SELECT 'PRODUCT' AS [Kind], p.product_ID AS [ID], p.product_name AS [Name], p.Product_price AS [Price], p.product_quantity AS [Stock], p.product_status AS [Status]
FROM Products p
UNION ALL
SELECT 'DELIVERY', d.delivery_ID, d.batch_ID + ' received ' + CAST(dd.product_quantity AS varchar(10)) + ' units', CAST(dd.new_quantity AS decimal(18,2)), 0, d.received_by
FROM Deliveries d JOIN DeliveryDetails dd ON dd.delivery_ID = d.delivery_ID
UNION ALL
SELECT 'SALE (checkout)', ch.CheckoutID, c.customer_FullName, ch.TotalAmount, 0, ch.Status
FROM tblCheckout ch JOIN tblCustomer c ON c.customer_ID = ch.CustomerID
UNION ALL
SELECT 'INSTALLMENT', i.InstallmentID,
       'due ' + CONVERT(varchar(10), DATEADD(month, i.MonthsPaid + 1, i.StartDate), 101)
       + ' - balance ' + FORMAT(i.Balance, 'C', 'en-PH'),
       i.Balance, i.MonthsPaid, i.Status
FROM tblInstallment i;

SELECT
    i.InstallmentID,
    'INS-' + RIGHT('000' + CAST(i.InstallmentID AS varchar(10)), 3) AS DisplayID,
    c.customer_FullName,
    CAST(DATEADD(month, i.MonthsPaid + 1, i.StartDate) AS DATE) AS NextDueDate,
    DATEDIFF(day, CAST(GETDATE() AS DATE), CAST(DATEADD(month, i.MonthsPaid + 1, i.StartDate) AS DATE)) AS DaysUntilDue,
    CASE
        WHEN i.Status IN ('Completed', 'Cancelled') OR i.Balance <= 0 THEN '(none - finished)'
        WHEN CAST(DATEADD(month, i.MonthsPaid + 1, i.StartDate) AS DATE) < CAST(GETDATE() AS DATE) THEN 'installment-overdue'
        WHEN DATEDIFF(day, CAST(GETDATE() AS DATE), CAST(DATEADD(month, i.MonthsPaid + 1, i.StartDate) AS DATE)) <= 7 THEN 'installment-due-soon'
        ELSE '(none - more than 7 days out)'
    END AS ProjectedNotificationType
FROM tblInstallment i
JOIN tblCheckout ch ON ch.CheckoutID = i.CheckoutID
JOIN tblCustomer c   ON c.customer_ID = ch.CustomerID
ORDER BY i.InstallmentID;