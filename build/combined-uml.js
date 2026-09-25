// =====================================================================
//  RJTech — Combined Full-Application UML Class Diagram
//  One image containing ALL classes: ViewModels + Controllers + Services
//  + Models (EF entities) + DatabaseContext, all connected.
//  Column-per-module layout with banded stacks + orthogonal lane router.
// =====================================================================
const fs = require('fs');
const esc = s => String(s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');

const PW = 14, HEADER = 40, CAPTION = 15, BOTTOM_PAD = 11;
function requiredH(attrs, ops) {
  return HEADER + (attrs.length ? CAPTION + PW * attrs.length : 0)
               + (ops.length ? CAPTION + PW * ops.length : 0) + BOTTOM_PAD;
}

const STROKE = '#3D5A80', ATTR_FILL = '#2B3A52', OPS_FILL = '#3A4A5F', CAP_FILL = '#8A97A8', BAND_STROKE = '#C9D4DF';
const BAND_COLOR = { vm: '#1F6F54', ctrl: '#1F4E79', svc: '#5B3E77', model: '#2F6472', data: '#333F50' };

class Box {
  constructor(id, cfg) {
    this.id = id; this.title = cfg.t; this.stereo = cfg.s || '';
    this.attrs = cfg.a || []; this.ops = cfg.o || [];
    this.band = cfg.b; this.col = cfg.c;
    this.w = cfg.w; this.x = cfg.x;
    this.h = Math.max(cfg.h || 0, requiredH(this.attrs, this.ops));
    this._y = cfg.y;
  }
  get y() { return this._y; }
  set y(v) { this._y = v; }
  get cx() { return this.x + this.w / 2; }
  get right() { return this.x + this.w; }
  get midY() { return this._y + this.h / 2; }
  get btmY() { return this._y + this.h; }
}

function renderBox(b) {
  const parts = [];
  const x = b.x, y = b.y, w = b.w, h = b.h;
  const fc = BAND_COLOR[b.band] || '#1F4E79';
  parts.push(`<rect x="${x}" y="${y}" width="${w}" height="${h}" rx="7" fill="#FFFFFF" stroke="${STROKE}" stroke-width="1.4"/>`);
  const headH = b.stereo ? HEADER : 30;
  const p = `M${x},${y + 7} Q${x},${y} ${x + 7},${y} L${x + w - 7},${y} Q${x + w},${y} ${x + w},${y + 7} L${x + w},${y + headH} L${x},${y + headH} Z`;
  parts.push(`<path d="${p}" fill="${fc}"/>`);
  if (b.stereo) {
    parts.push(`<text x="${x + 12}" y="${y + 22}" font-family="Segoe UI,Arial" font-size="13" font-weight="700" fill="#FFFFFF">${esc(b.title)}</text>`);
    parts.push(`<text x="${x + 12}" y="${y + 37}" font-family="Segoe UI,Arial" font-size="9.5" font-style="italic" fill="#E3EEF8">${esc(b.stereo)}</text>`);
  } else {
    parts.push(`<text x="${x + 12}" y="${y + 22}" font-family="Segoe UI,Arial" font-size="13" font-weight="700" fill="#FFFFFF">${esc(b.title)}</text>`);
  }
  let cy = y + headH;
  if (b.attrs.length) {
    parts.push(`<text x="${x + 12}" y="${cy + 11}" font-family="Segoe UI,Arial" font-size="9" font-weight="700" letter-spacing="1" fill="${CAP_FILL}">ATTRIBUTES</text>`);
    cy += CAPTION;
    for (const a of b.attrs) { parts.push(`<text x="${x + 14}" y="${cy + 11}" font-family="Consolas,'Courier New',monospace" font-size="11.5" fill="${ATTR_FILL}">${esc(a)}</text>`); cy += PW; }
  }
  if (b.ops.length) {
    if (b.attrs.length) parts.push(`<line x1="${x + 8}" y1="${cy}" x2="${x + w - 8}" y2="${cy}" stroke="${BAND_STROKE}" stroke-width="1"/>`);
    parts.push(`<text x="${x + 12}" y="${cy + 11}" font-family="Segoe UI,Arial" font-size="9" font-weight="700" letter-spacing="1" fill="${CAP_FILL}">OPERATIONS</text>`);
    cy += CAPTION;
    for (const o of b.ops) { parts.push(`<text x="${x + 14}" y="${cy + 11}" font-family="Consolas,'Courier New',monospace" font-size="11.5" fill="${OPS_FILL}">${esc(o)}</text>`); cy += PW; }
  }
  return parts.join('\n');
}

// ---------- content helpers ----------
const vm  = (id, t, a, o) => ({ id, cfg: { t, s: '«viewModel»', a, o, b: 'vm' } });
const ctrl= (id, t, a, o) => ({ id, cfg: { t, s: '«controller»', a, o, b: 'ctrl' } });
const svc = (id, t, s, a, o) => ({ id, cfg: { t, s, a, o, b: 'svc' } });
const model=(id, t, a, o) => ({ id, cfg: { t, s: '«entity» tbl', a, o, b: 'model' } });

const raw = [];
// ============ AUTH ============
raw.push(vm('loginVM','LoginViewModel',['+ Identifier : string','+ Password : string','+ RememberMe : bool'],['+ LoginViewModel()']));
raw.push(vm('signupVM','SignUpViewModel',['+ FullName : string','+ Email : string','+ Username : string','+ Password : string','+ ConfirmPassword : string'],['+ SignUpViewModel()']));
raw.push(vm('forgotVM','ForgotPasswordViewModel',['+ Email : string'],['+ ForgotPasswordViewModel()']));
raw.push(vm('verifyVM','VerifyOtpViewModel',['+ Otp : string','+ RequestId : string'],['+ VerifyOtpViewModel()']));
raw.push(vm('resetVM','ResetPasswordViewModel',['+ Password : string','+ ConfirmPassword : string','+ RequestId : string'],['+ ResetPasswordViewModel()']));
raw.push(ctrl('acctCtrl','AccountController',['− ApplicationDbContext _db','− UserAuthenticationService _auth','− PasswordResetService _reset','− PasswordHashService _hasher','− ILogger<AccountController> _logger'],['+ Login(model) : IActionResult','+ SignUp(model) : IActionResult','+ ForgotPassword(model) : IActionResult','+ ForgotPasswordVerify(model) : IActionResult','+ ResetPassword(model) : IActionResult','+ Logout() : Task']));
raw.push(svc('authSvc','UserAuthenticationService','«service» sealed',['− ApplicationDbContext _db','− PasswordHashService _hasher'],['+ AuthenticateAsync(id, pwd) : AppUser?','+ CreatePrincipal(user) : ClaimsPrincipal']));
raw.push(svc('pwHash','PasswordHashService','«service» sealed',[],['+ Hash(pwd) : string','+ Verify(pwd, hash) : bool']));
raw.push(svc('pwReset','PasswordResetService','«service» sealed',['− ApplicationDbContext _db','− PasswordHashService _hasher','− IEmailSender _emailSender'],['+ AccountExistsAsync(email) : bool','+ CreateCodeAsync(email) : PasswordResetCode?','+ VerifyCodeAsync(id, otp) : PasswordResetCode?','+ ResetPasswordAsync(...) : bool']));
raw.push(svc('iEmail','IEmailSender','«interface»',[],['+ SendAsync(recipient, subject, html) : Task']));
raw.push(svc('smtp','SmtpEmailSender','«service» sealed',['− EmailOptions _options'],['+ SendAsync(...) : Task   (implements IEmailSender)']));
raw.push(svc('emailOpts','EmailOptions','«config» appsettings',['+ Host : string','+ Port : int','+ Username : string','+ Password : string','+ FromAddress : string','+ EnableSsl : bool'],[]));
raw.push(svc('pwTpl','PasswordResetEmailTemplate','«static» helper',[],['+ Build(email, otp, min) : string']));
raw.push(model('appUser','AppUser · tblUser',['+ UserID : string (PK)','+ FullName : string','+ Email : string (UQ)','+ Username : string (UQ)','+ Password : string (hash)','+ Role : string','+ DateCreated : DateTime'],['+ AppUser()']));
raw.push(model('pwCode','PasswordResetCode · tblPasswordResetCode',['+ Id : string (PK)','+ Email : string','+ OtpHash : string','+ ExpiresAt : DateTimeOffset','+ VerifiedAt : DateTimeOffset?'],['+ PasswordResetCode()']));

// ============ SETTINGS ============
raw.push(vm('acctSetVM','AccountSettingsViewModel',['+ Profile : EditProfileViewModel','+ Security : ChangePasswordViewModel'],['+ AccountSettingsViewModel()']));
raw.push(vm('editProfVM','EditProfileViewModel',['+ FullName : string','+ Email : string','+ Username : string','+ Role : string','+ DateCreated : DateTime'],['+ EditProfileViewModel()']));
raw.push(vm('chgPwdVM','ChangePasswordViewModel',['+ CurrentPassword : string','+ NewPassword : string','+ ConfirmPassword : string'],['+ ChangePasswordViewModel()']));
raw.push(ctrl('settingCtrl','SettingController',['− ApplicationDbContext _db','− UserAuthenticationService _auth','− PasswordHashService _hasher'],['+ AccountMenu() : Task<IActionResult>','+ UpdateProfile(model) : Task','+ ChangePassword(model) : Task']));

// ============ DASHBOARD ============
raw.push(vm('dashVM','DashboardViewModel',['+ TotalSales : decimal','+ TotalProducts : int','+ OutstandingAmount : decimal','+ TotalCustomers : int','+ Month / Year : int','+ Years : List<int>','+ SalesOverview : List<SalesChartItem>','+ InventoryHealth : InventoryHealthViewModel','+ TopSellingProducts : List<...>','+ RecentTransactions : List<...>'],['+ PeriodLabel() : string','+ PeriodSales() : decimal']));
raw.push(vm('salesChart','SalesChartItem',['+ Day : int','+ Revenue : decimal'],['+ SalesChartItem()']));
raw.push(vm('weeklyChart','WeeklySalesChartItem',['+ WeekStartDate : DateTime','+ WeekEndDate : DateTime','+ Revenue : decimal'],['+ Label() : string']));
raw.push(vm('invHealthVM','InventoryHealthViewModel',['+ Available : int','+ Unavailable : int','+ LowStock : int','+ OutOfStock : int'],['+ Total() : int']));
raw.push(vm('topSellVM','TopSellingProductViewModel',['+ ProductId : int','+ Product : string','+ Category : string','+ QuantitySold : int','+ Revenue : decimal'],['+ ProductCode() : string']));
raw.push(vm('dashCatVM','DashboardCategoryViewModel',['+ Id : int','+ Name : string'],['+ DashboardCategoryViewModel()']));
raw.push(vm('catSalesVM','CategorySalesViewModel',['+ CategoryId : int','+ Category : string','+ Revenue : decimal'],['+ CategorySalesViewModel()']));
raw.push(vm('recentTxVM','RecentTransactionViewModel',['+ CheckoutId : int','+ Customer : string','+ Items : int','+ PaymentMethod : string','+ TotalAmount : decimal','+ Status : string'],['+ CheckoutCode() : string']));
raw.push(ctrl('homeCtrl','HomeController',[],['+ Dashboard(month, year, cat) : IActionResult','+ Privacy() : IActionResult','+ Archive() : IActionResult','+ Error() : IActionResult']));
raw.push(ctrl('dashCtrl','DashboardController',['− ApplicationDbContext db'],['+ Index(month, year, cat) : Task<IActionResult>','+ CategorySales(month, year, cat) : Task<IActionResult>']));

// ============ PRODUCTS ============
raw.push(vm('prodCreateVM','ProductCreateViewModel',['+ category_ID : int','+ product_name : string','+ product_brand : string','+ product_description : string?','+ product_quantity : int','+ reorder_level : int','+ Product_price : decimal','+ product_status : string','+ IsSerialized : bool'],['+ ProductCreateViewModel()']));
raw.push(vm('bulkReq','BulkCreateProductsRequest','products : List<BulkProductRowDto>?'.split(',').map(s=>'+ '+s),['+ BulkCreateProductsRequest()']));
raw.push(vm('bulkRow','BulkProductRowDto',['+ category_ID : int','+ product_name : string?','+ product_brand : string?','+ Product_price : decimal','+ IsSerialized : bool'],['+ BulkProductRowDto()']));
raw.push(ctrl('prodCtrl','ProductController',['− ApplicationDbContext _db','− ILogger<ProductController> _logger'],['+ Index() : IActionResult','+ Create(model) : IActionResult','+ BulkCreate(request) : IActionResult','+ UpdateProductDetails(...) : IActionResult','+ DeleteProduct(id) : IActionResult','+ CreateCategory(cat) : IActionResult']));
raw.push(ctrl('scanCtrl','ScanSNController',[],['+ Index() : IActionResult','+ ScanSerialNumber() : IActionResult']));
raw.push(model('pCat','ProductCategory · tblProductCategory',['+ category_ID : int (PK)','+ category_name : string (UQ)'],['+ ProductCategory()']));
raw.push(model('product','Product · tblProduct',['+ product_ID : int (PK)','+ product_name : string','+ product_brand : string','+ product_description : string?','+ product_Image : byte[]?','+ product_quantity : int','+ reorder_level : int','+ Product_price : decimal(18,2)','+ is_serialized : bool','+ product_status : string','+ category_ID : int (FK)','+ product_code : string (computed)'],['+ formatted_code() : string']));

// ============ SALES ============
raw.push(vm('checkoutFormVM','CheckoutFormViewModel',['+ CustomerID : int?','+ CustomerFullName/Email/Phone/Address : string','+ PaymentType / Method : string','+ DatePurchased : DateTime','+ Items : List<CheckoutFormItemViewModel>','+ AvailableProducts : List<...>'],['+ CheckoutFormViewModel()']));
raw.push(vm('coItemVM','CheckoutFormItemViewModel',['+ ProductID : int','+ ProductName : string','+ Quantity : int','+ SerialNumbers : List<string>','+ Price : decimal','+ AvailableStock : int','+ IsSerialized : bool'],['+ CheckoutFormItemViewModel()']));
raw.push(vm('coOptVM','CheckoutProductOptionViewModel',['+ ProductId : int','+ Code : string','+ Name : string','+ Category : string','+ Stock : int','+ Price : decimal','+ IsSerialized : bool'],['+ CheckoutProductOptionViewModel()']));
raw.push(vm('coDetailsVM','CheckoutDetailsViewModel',['+ Checkout : Checkout'],['+ CheckoutDetailsViewModel()']));
raw.push(vm('saveCkReq','SaveCheckoutRequest',['+ CustomerID : int?','+ CustomerFullName/Email/Phone/Address : string','+ PaymentType / Method : string','+ InstallmentMonths : int?','+ DownPayment : decimal?','+ Items : List<CheckoutItemRequest>'],['+ SaveCheckoutRequest()']));
raw.push(vm('coItemReq','CheckoutItemRequest',['+ ProductID : int','+ Quantity : int','+ SerialNumbers : List<string>'],['+ CheckoutItemRequest()']));
raw.push(vm('recPayReq','RecordInstallmentPaymentRequest',['+ CheckoutID : int','+ PaymentAmount : decimal','+ PaymentMethod : string'],['+ RecordInstallmentPaymentRequest()']));
raw.push(ctrl('salesCtrl','SalesController',['− ApplicationDbContext _db','− StockNotificationService _stock','− InstallmentService _installments'],['+ Checkout() : IActionResult','+ CompleteCheckout(request) : IActionResult','+ RefundCheckout(id) : IActionResult','+ SearchCustomers(q) : IActionResult','+ SearchProducts(q) : IActionResult','+ RecordInstallmentPayment(req) : Task','+ SalesHistory() : Task','+ DeleteCheckout(id) : IActionResult']));
raw.push(model('customer','Customer · tblCustomer',['+ customer_ID : int (PK)','+ customer_FullName : string','+ customer_Email : string','+ customer_Phone : string','+ customer_Address : string'],['+ Customer()']));
raw.push(model('checkout','Checkout · tblCheckout',['+ CheckoutID : int (PK)','+ CustomerID : int (FK)','+ TotalAmount : decimal(18,2)','+ PaymentMethod : string','+ PaymentType : string','+ DatePurchased : DateTime','+ Status : string'],['+ FormattedCheckoutID() : string']));
raw.push(model('coItem','CheckoutItem · tblCheckoutItem',['+ CheckoutItemID : int (PK)','+ CheckoutID : int (FK)','+ ProductID : int (FK)','+ SerialNo : string?','+ ItemQuantity : int','+ Price : decimal(18,2)','+ SubTotal : decimal(18,2)'],['+ CheckoutItem()']));
raw.push(model('custHist','CustomerPurchaseHistory · tblCustomerPurchaseHistory',['+ HistoryID : int (PK)','+ CustomerID : int (FK)','+ CheckoutID : int (FK)','+ PurchaseDate : DateTime','+ TotalAmount : decimal(18,2)','+ PaymentMethod : string'],['+ FormattedHistoryID() : string']));

// ============ INSTALLMENTS ============
raw.push(vm('instMgmtVM','InstallmentManagementViewModel',['+ Installments : IReadOnlyList<InstallmentListViewModel>'],['+ InstallmentManagementViewModel()']));
raw.push(vm('instListVM','InstallmentListViewModel',['+ InstallmentID : int','+ CheckoutID : int','+ CustomerName/Phone/Email : string','+ MonthlyPayment : decimal','+ Balance : decimal','+ TotalPaid : decimal','+ Months : int','+ MonthsPaid / Remaining : int','+ Status : string','+ ProgressPercentage : decimal'],['+ FormattedInstallmentID() : string']));
raw.push(vm('instDetailsVM','InstallmentDetailsViewModel',['+ Installment : Installment','+ Checkout : Checkout','+ Customer : Customer?','+ TotalPaid : decimal','+ ProgressPercentage : decimal','+ CompletionDate : DateTime?'],['+ InstallmentDetailsViewModel()']));
raw.push(vm('recInstPayVM','RecordInstallmentPaymentViewModel',['+ InstallmentID : int','+ PaymentAmount : decimal','+ PaymentMethod : string'],['+ RecordInstallmentPaymentViewModel()']));
raw.push(ctrl('instCtrl','InstallmentController',['− InstallmentService _installments','− ApplicationDbContext _db','− ILogger<InstallmentController> _logger'],['+ Index() : Task<IActionResult>','+ Details(id) : Task','+ RecordPayment(request) : Task','+ DeleteInstallments(ids) : IActionResult']));
raw.push(svc('instSvc','InstallmentService','«service»',['+ AllowedTerms : int[]','+ PaymentMethods : string[]','− ApplicationDbContext _db'],['+ Calculate(amount, months) : InstallmentCalculation','+ GetInstallmentsAsync() : InstallmentManagementViewModel','+ GetInstallmentDetailsAsync(id) : InstallmentDetailsViewModel?','+ RecordPaymentAsync(id, amt, m) : InstallmentPaymentResult','+ UpdateInstallmentStatusesAsync()']));
raw.push(svc('instCalc','InstallmentCalculation','«record» result',['+ OriginalAmount : decimal','+ DownPayment : decimal','+ RemainingPrincipal : decimal','+ InterestRate : decimal','+ InterestAmount : decimal','+ InstallmentTotal : decimal','+ MonthlyPayment : decimal','+ Months : int'],['+ InstallmentCalculation()']));
raw.push(svc('instPayRes','InstallmentPaymentResult','«record» result',['+ Success : bool','+ Message : string','+ InstallmentID : int?','+ Balance : decimal?','+ MonthsPaid : int','+ MonthsRemaining : int','+ Status : string?','+ NextDue : DateTime?'],['+ InstallmentPaymentResult()']));
raw.push(model('installment','Installment · tblInstallment',['+ InstallmentID : int (PK)','+ CheckoutID : int (FK)','+ Months : int','+ DownPayment : decimal(18,2)','+ InterestRate : decimal(5,2)','+ TotalAmount : decimal(18,2)','+ Balance : decimal(18,2)','+ MonthlyPayment : decimal(18,2)','+ MonthsPaid : int','+ MonthsRemaining : int','+ StartDate : DateTime','+ Status : string'],['+ FormattedInstallmentID() : string']));
raw.push(model('instPay','InstallmentPayment · tblInstallmentPayment',['+ PaymentID : int (PK)','+ InstallmentID : int (FK)','+ PaymentMethod : string','+ PaymentAmount : decimal(18,2)','+ PaymentDate : DateTime','+ Status : string'],['+ InstallmentPayment()']));

// ============ DELIVERY ============
raw.push(vm('delivReq','DeliveryCompleteRequest',['+ received_by : string?','+ delivery_date : DateTime?','+ items : List<DeliveryItemRequest>?'],['+ DeliveryCompleteRequest()']));
raw.push(vm('delivItemReq','DeliveryItemRequest',['+ product_ID : int','+ quantity : int'],['+ DeliveryItemRequest()']));
raw.push(ctrl('delivCtrl','DeliveryController',['− ApplicationDbContext _db','− ILogger<DeliveryController> _logger'],['+ Receive() : IActionResult','+ CompleteDelivery(request) : IActionResult','+ GetDeliveries() : IActionResult','+ DeleteDeliveryReceipts(ids) : IActionResult','+ SearchProductForDelivery(q) : IActionResult']));
raw.push(model('delivery','Delivery · tblDelivery',['+ delivery_ID : int (PK)','+ date_delivered : DateTime','+ received_by : string','+ batch_ID : string (UQ)','+ is_archived : bool'],['+ FormattedBatchID() : string']));
raw.push(model('delivDetails','DeliveryDetails · tblDeliveryDetails',['+ deldetails_ID : int (PK)','+ product_quantity : int','+ previous_quantity : int','+ new_quantity : int','+ product_ID : int (FK)','+ delivery_ID : int (FK)'],['+ DeliveryDetails()']));

// ============ NOTIFICATIONS ============
raw.push(ctrl('notifCtrl','NotificationController',['− ApplicationDbContext _db','− StockNotificationService _stock','− InstallmentNotificationService _installment'],['+ Index() : IActionResult','+ GetNotifications(limit) : IActionResult','+ MarkAsRead(id) : IActionResult','+ MarkAllAsRead() : IActionResult']));
raw.push(svc('stockNotif','StockNotificationService','«service»',['− ApplicationDbContext _db'],['+ Synchronize() : void']));
raw.push(svc('instNotif','InstallmentNotificationService','«service»',['− ApplicationDbContext _db'],['+ Synchronize() : void']));
raw.push(model('appNotif','AppNotification · tblNotification',['+ notification_ID : int (PK)','+ product_ID : int? (FK)','+ title : string','+ message : string','+ notification_type : string','+ action_url : string','+ created_at : DateTime','+ is_read : bool'],['+ AppNotification()']));

// ============ REPORTS ============
raw.push(vm('reportsVM','ReportsViewModel',['+ ActiveTab : string','+ Period : string','+ Month / Year : int','+ Search / Status / Sort : string?','+ Years : List<int>','+ Sales : SalesOverviewViewModel','+ Inventory : InventoryOverviewViewModel','+ Delivery : DeliveryOverviewViewModel'],['+ ReportsViewModel()']));
raw.push(vm('salesOvVM','SalesOverviewViewModel',['+ TotalSales : decimal','+ TotalCheckouts : int','+ TotalSoldItems : int','+ Today/Weekly/Monthly/YearlySales : decimal','+ History : List<ReportChartPoint>','+ SalesByCategory : List<...>','+ RecentTransactions : List<...>'],['+ SalesOverviewViewModel()']));
raw.push(vm('invOvVM','InventoryOverviewViewModel',['+ TotalProducts : int','+ TotalQuantity : int','+ Available/Unavailable/LowStock/OutOfStock : int','+ ProductStock : List<ProductStockReportRow>','+ ProductCategories : List<...>'],['+ InventoryOverviewViewModel()']));
raw.push(vm('delivOvVM','DeliveryOverviewViewModel',['+ TotalDeliveries : int','+ TotalItemsDelivered : int','+ LastDeliveryDate : DateTime?','+ DeliverySummaries : List<...>','+ ProductDeliveries : List<...>'],['+ DeliveryOverviewViewModel()']));
raw.push(ctrl('reportsCtrl','ReportsController',['− ApplicationDbContext db','− ReportComputationService reportComputation','− IExcelExportService excelExport'],['+ Index(tab, period, month, year, ...) : Task','+ Export(tab, period, ...) : Task']));
raw.push(svc('repComputation','ReportComputationService','«service» sealed',['− ApplicationDbContext db'],['+ BuildAsync(ct) : ReportSnapshot']));
raw.push(vm('chartPt','ReportChartPoint',['+ Label : string','+ Value : decimal','+ TotalTransactions : int'],['+ ReportChartPoint()']));
raw.push(vm('prodSalesRow','ProductSalesReportRow',['+ ProductID : int','+ ProductCode/Name : string','+ TotalQuantitySold : int','+ TotalSalesAmount : decimal'],['+ ProductSalesReportRow()']));
raw.push(vm('catSalesRow','CategorySalesReportRow',['+ CategoryID : int','+ CategoryName : string','+ TotalQuantitySold : int','+ TotalSalesAmount : decimal'],['+ CategorySalesReportRow()']));
raw.push(vm('txRow','TransactionReportRow',['+ CheckoutID : int','+ CustomerName : string','+ PaymentType/Method : string','+ Amount : decimal','+ Status : string'],['+ CheckoutCode() : string']));
raw.push(vm('stockRow','ProductStockReportRow',['+ ProductID : int','+ ProductCode/Name : string','+ CategoryName : string','+ CurrentQuantity : int','+ ReorderLevel : int','+ StockStatus : string'],['+ ProductStockReportRow()']));
raw.push(vm('catInvRow','CategoryInventoryReportRow',['+ CategoryName : string','+ TotalProducts : int','+ TotalQuantity : int','+ LowStock/OutOfStockProducts : int'],['+ CategoryInventoryReportRow()']));
raw.push(vm('stockExtremeRow','StockExtremeReportRow',['+ ProductID : int','+ ProductCode/Name : string','+ CurrentQuantity : int'],['+ StockExtremeReportRow()']));
raw.push(vm('delivSumRow','DeliverySummaryReportRow',['+ DeliveryID : int','+ TotalItems : int','+ TotalProducts : int'],['+ DeliveryCode() : string']));
raw.push(vm('prodDelivRow','ProductDeliveryReportRow',['+ ProductID : int','+ ProductCode/Name : string','+ TotalQuantityDelivered : int','+ TotalDeliveries : int'],['+ ProductDeliveryReportRow()']));

// ============ REPORT DATASETS (grid) ============
const setRec = (name, attrs) => ({ id: name, cfg: { t: name, s: '«record» dataset', a: attrs, o: ['+' + name + '()'], b: 'model' } });
raw.push(svc('reportSnap','ReportSnapshot','«record» snapshot',['+ SalesOverview : SalesOverviewReport','+ WeeklySales : List<WeeklySalesReport>','+ MonthlySales : List<MonthlySalesReport>','+ YearlySales : List<YearlySalesReport>','+ BestSellingProducts : List<...>','+ Transactions : List<TransactionReport>','+ InventoryOverview : InventoryOverviewReport','+ ProductStock : List<...>','+ DeliveryOverview : DeliveryOverviewReport'],['+ ReportSnapshot()']));
raw.push(setRec('salesOvRpt',['+ TotalSales : decimal','+ TotalTransactions : int','+ TotalItemsSold : int','+ Today/Weekly/Monthly/Yearly : decimal']));
raw.push(setRec('weeklyRpt',['+ WeeklySalesID : int','+ WeekStartDate : DateTime','+ WeekEndDate : DateTime','+ TotalSales : decimal']));
raw.push(setRec('monthlyRpt',['+ MonthlySalesID : int','+ Month : int','+ Year : int','+ TotalSales : decimal']));
raw.push(setRec('yearlyRpt',['+ YearlySalesID : int','+ Year : int','+ TotalSales : decimal']));
raw.push(setRec('bestSellRpt',['+ BestSellingID : int','+ ProductID : int','+ TotalQuantitySold : int','+ TotalSalesAmount : decimal']));
raw.push(setRec('leastSellRpt',['+ LeastSellingID : int','+ ProductID : int','+ TotalQuantitySold : int','+ TotalSalesAmount : decimal']));
raw.push(setRec('catSalesRpt',['+ SalesCategoryID : int','+ CategoryID : int','+ TotalQuantitySold : int','+ TotalSalesAmount : decimal']));
raw.push(setRec('txRpt',['+ TransactionID : int','+ CheckoutID : int','+ CustomerID : int','+ Amount : decimal','+ Status : string']));
raw.push(setRec('invOvRpt',['+ TotalProducts : int','+ TotalQuantity : int','+ Available/Unavailable : int','+ LowStock/OutOfStock : int']));
raw.push(setRec('stockSumRpt',['+ ProductID : int','+ CurrentQuantity : int','+ ReorderLevel : int','+ StockStatus : string']));
raw.push(setRec('catOvRpt',['+ CategoryID : int','+ TotalProducts : int','+ TotalQuantity : int','+ LowStockProducts : int']));
raw.push(setRec('mostStockRpt',['+ ProductID : int','+ CurrentQuantity : int']));
raw.push(setRec('leastStockRpt',['+ ProductID : int','+ CurrentQuantity : int']));
raw.push(setRec('delivOvRpt',['+ TotalDeliveries : int','+ TotalItemsDelivered : int','+ TotalProductsDelivered : int','+ LastDeliveryDate : DateTime?']));
raw.push(setRec('delivSumRpt',['+ DeliveryID : int','+ TotalItems : int','+ TotalProducts : int']));
raw.push(setRec('prodDelivRpt',['+ ProductID : int','+ TotalQuantityDelivered : int','+ TotalDeliveries : int','+ LastDeliveryDate : DateTime?']));

// ============ EXCEL ============
raw.push(vm('excelSel','ExcelExportSelection',['+ RecordIds : int[]?'],['+ ExcelExportSelection()']));
raw.push(ctrl('excelCtrl','ExcelExportController',['− ApplicationDbContext _db','− IExcelExportService _excel','− InstallmentService _installments'],['+ Inventory(selection) : Task','+ Delivery(selection) : Task','+ Customers(selection) : Task','+ SalesSummary(selection) : Task','+ Installments(selection) : Task','+ SalesHistory(selection) : Task']));
raw.push(svc('iExcel','IExcelExportService','«interface»',[],['+ ExportToExcel(title, cols, rows) : byte[]']));
raw.push(svc('excelSvc','ExcelExportService','«service» sealed',['− IWebHostEnvironment _env','− ILogger<ExcelExportService> _logger'],['+ ExportToExcel(title, cols, rows) : byte[]']));
raw.push(svc('excelCol','ExcelExportColumn','«record» metadata',['+ Header : string','+ Type : ExcelColumnType','+ MinWidth : double','+ MaxWidth : double'],['+ ExcelExportColumn()']));
raw.push(svc('excelFile','ExcelExportFile','«static» helper',[],['+ CreateFileName(module) : string']));
raw.push(svc('excelType','ExcelColumnType','«enum»',['+ Text / Integer / Currency / Date / Percentage / Status'],[]));
raw.push(svc('salesPeriod','SalesPeriod','«static» helper',[],['+ StartOfWeek(date) : DateTime']));

// fix the bulkReq attrs hack (string split produced 1 attr)
raw.find(r => r.id === 'bulkReq').cfg.a = ['+ products : List<BulkProductRowDto>?'];

// ============ registry ============
const boxes = {};
const ASSIGN = {
  auth: ['loginVM','signupVM','forgotVM','verifyVM','resetVM','acctCtrl','authSvc','pwHash','pwReset','iEmail','smtp','emailOpts','pwTpl','appUser','pwCode'],
  settings: ['acctSetVM','editProfVM','chgPwdVM','settingCtrl'],
  dashboard: ['dashVM','salesChart','weeklyChart','invHealthVM','topSellVM','dashCatVM','catSalesVM','recentTxVM','homeCtrl','dashCtrl'],
  products: ['prodCreateVM','bulkReq','bulkRow','prodCtrl','scanCtrl','pCat','product'],
  sales: ['checkoutFormVM','coItemVM','coOptVM','coDetailsVM','saveCkReq','coItemReq','recPayReq','salesCtrl','customer','checkout','coItem','custHist'],
  inst: ['instMgmtVM','instListVM','instDetailsVM','recInstPayVM','instCtrl','instSvc','instCalc','instPayRes','installment','instPay'],
  deliv: ['delivReq','delivItemReq','delivCtrl','delivery','delivDetails'],
  notif: ['notifCtrl','stockNotif','instNotif','appNotif'],
  reports: ['reportsVM','salesOvVM','invOvVM','delivOvVM','reportsCtrl','repComputation'],
  repdata: ['chartPt','prodSalesRow','catSalesRow','txRow','stockRow','catInvRow','stockExtremeRow','delivSumRow','prodDelivRow','reportSnap','salesOvRpt','weeklyRpt','monthlyRpt','yearlyRpt','bestSellRpt','leastSellRpt','catSalesRpt','txRpt','invOvRpt','stockSumRpt','catOvRpt','mostStockRpt','leastStockRpt','delivOvRpt','delivSumRpt','prodDelivRpt'],
  excel: ['excelSel','excelCtrl','iExcel','excelSvc','excelCol','excelFile','excelType'],
};
const repVMids  = ['chartPt','prodSalesRow','catSalesRow','txRow','stockRow','catInvRow','stockExtremeRow','delivSumRow','prodDelivRow'];
const repModelIds = ['reportSnap','salesOvRpt','weeklyRpt','monthlyRpt','yearlyRpt','bestSellRpt','leastSellRpt','catSalesRpt','txRpt','invOvRpt','stockSumRpt','catOvRpt','mostStockRpt','leastStockRpt','delivOvRpt','delivSumRpt','prodDelivRpt'];

const columns = [
  { id: 'auth', label: 'AUTH / ACCOUNT' },
  { id: 'settings', label: 'SETTINGS / PROFILE' },
  { id: 'dashboard', label: 'DASHBOARD' },
  { id: 'products', label: 'PRODUCTS & SCAN' },
  { id: 'sales', label: 'SALES / CHECKOUT' },
  { id: 'inst', label: 'INSTALLMENTS' },
  { id: 'deliv', label: 'DELIVERY' },
  { id: 'notif', label: 'NOTIFICATIONS' },
  { id: 'reports', label: 'REPORTS' },
  { id: 'repdata', label: 'ROWS · REPORT DATASETS', wide: true },
  { id: 'excel', label: 'EXCEL EXPORT' },
];
const COL_W = 500, GAP = 56, CELLWIDE = 1060, MARGIN_L = 40, GRID_GAP = 40;
let cx = MARGIN_L;
columns.forEach((c, i) => { c.i = i; c.x = cx; c.w = c.wide ? CELLWIDE : COL_W; c.right = c.x + c.w; cx = c.right + GAP; });
const W = cx - GAP + MARGIN_L;
const colIdxOf = id => columns.findIndex(c => c.id === id);

for (const [colName, ids] of Object.entries(ASSIGN)) {
  const ci = colIdxOf(colName);
  for (const id of ids) {
    const d = raw.find(r => r.id === id);
    d.cfg.c = ci; d.cfg.w = columns[ci].w - 16; d.cfg.x = columns[ci].x + 8;
    boxes[id] = new Box(id, d.cfg);
  }
}

// data-band boxes
const dbBox = new Box('db', { t: 'ApplicationDbContext', s: '«data» EF Core — persistence · 13 DbSets', a: [
  '+ DbSet<Product> Products','+ DbSet<ProductCategory> ProductCategories','+ DbSet<Delivery> Deliveries','+ DbSet<DeliveryDetails> DeliveryDetails',
  '+ DbSet<AppNotification> Notifications','+ DbSet<Customer> Customers','+ DbSet<Checkout> Checkouts','+ DbSet<CheckoutItem> CheckoutItems',
  '+ DbSet<Installment> Installments','+ DbSet<InstallmentPayment> InstallmentPayments','+ DbSet<CustomerPurchaseHistory> CustomerPurchaseHistories',
  '+ DbSet<AppUser> Users','+ DbSet<PasswordResetCode> PasswordResetCodes'], o: ['+ SaveChanges() : int'], b: 'data' });
dbBox.x = MARGIN_L; dbBox.w = W - MARGIN_L * 2;
// base Controller is the framework MVC base class — it sits IN the Controllers
// band (far right of the EXCEL column) so all inheritance arrows are local and
// do not drag through the model/data seam like a bottom-corner box would.
const baseCtrl = new Box('baseCtrl', { t: 'Controller', s: '«framework» MVC base class', a: [], o: ['+ Controller()'], b: 'ctrl' });
baseCtrl.col = colIdxOf('excel');
baseCtrl.x = columns[baseCtrl.col].x + 8; baseCtrl.w = columns[baseCtrl.col].w - 16;
boxes['baseCtrl'] = baseCtrl;

// ---------- band layout ----------
const BAND_ORDER = ['vm', 'ctrl', 'svc', 'model', 'data'];
const GAP_STACK = { vm: 12, ctrl: 14, svc: 14, model: 14 };
function estH(b) {
  const items = (b.attrs.length ? 1 + b.attrs.length : 0) + (b.ops.length ? 1 + b.ops.length : 0);
  return HEADER + items * PW * 1.25 + 22;
}
function cellHeight(band, col) {
  const ids = Object.keys(boxes).filter(id => boxes[id].band === band && boxes[id].col === col);
  if (col === colIdxOf('repdata') && band === 'vm') return Math.ceil(repVMids.length / 4) * (estH(boxes[repVMids[0]]) + 14);
  if (col === colIdxOf('repdata') && band === 'model') return Math.ceil(repModelIds.length / 4) * (estH(boxes['reportSnap']) + 14);
  let sum = 0;
  for (const id of ids) sum += boxes[id].h + GAP_STACK[band];
  return sum - GAP_STACK[band];
}
const bandTop = {};
let yy = 176;
BAND_ORDER.forEach((band, i) => {
  if (band === 'data') { bandTop[band] = yy + 44; return; }
  bandTop[band] = yy;
  let maxH = 0;
  for (const c of columns) {
    const h = cellHeight(band, c.i);
    if (h > maxH) maxH = h;
  }
  yy = yy + maxH + 84;
});
const DATA_TOP = bandTop.data;

// place stacks + grids
for (const band of BAND_ORDER) {
  if (band === 'data') continue;
  for (const c of columns) {
    const ids = Object.keys(boxes).filter(id => boxes[id].band === band && boxes[id].col === c.i)
      .sort((a, b) => raw.findIndex(r => r.id === a) - raw.findIndex(r => r.id === b));
    if (!ids.length) continue;
    if (colIdxOf('repdata') === c.i && (band === 'vm' || band === 'model')) continue;
    let y = bandTop[band];
    for (const id of ids) { boxes[id].y = y; y += boxes[id].h + GAP_STACK[band]; }
  }
}
function gridPlace(ids, topY, perRow) {
  const x0 = columns[colIdxOf('repdata')].x + 10, x1 = columns[colIdxOf('repdata')].right - 10;
  const w = (x1 - x0 - (perRow - 1) * GRID_GAP) / perRow;
  let y = topY, maxH = 0;
  ids.forEach((id, idx) => {
    const b = boxes[id];
    b.x = x0 + (idx % perRow) * (w + GRID_GAP); b.y = y; b.w = w;
    if (b.h > maxH) maxH = b.h;
    if ((idx + 1) % perRow === 0 || idx === ids.length - 1) { y += maxH + 14; maxH = 0; }
  });
}
gridPlace(repVMids, bandTop.vm, 4);
gridPlace(repModelIds, bandTop.model, 4);

// data band placement
dbBox.y = DATA_TOP;
boxes['db'] = dbBox;
const H = dbBox.btmY + 120;

// ---------- markers ----------
const defsSvg = `
  <defs>
    <marker id="arr" viewBox="0 0 12 12" refX="10.5" refY="6" markerWidth="8" markerHeight="8" orient="auto-start-reverse"><path d="M1,1 L11,6 L1,11 Z" fill="#3D5A80"/></marker>
    <marker id="hollow" viewBox="0 0 12 12" refX="10.5" refY="6" markerWidth="8" markerHeight="8" orient="auto-start-reverse"><path d="M1,1 L11,6 L1,11 Z" fill="#FFFFFF" stroke="#3D5A80" stroke-width="1.2"/></marker>
    <marker id="diamond" viewBox="0 0 12 12" refX="10.5" refY="6" markerWidth="8" markerHeight="8" orient="auto-start-reverse"><path d="M6,0 L12,6 L6,12 L0,6 Z" fill="#3D5A80"/></marker>
    <marker id="hollowDiamond" viewBox="0 0 12 12" refX="10.5" refY="6" markerWidth="8" markerHeight="8" orient="auto-start-reverse"><path d="M6,0 L12,6 L6,12 L0,6 Z" fill="#FFFFFF" stroke="#3D5A80" stroke-width="1.2"/></marker>
    <marker id="dashArr" viewBox="0 0 12 12" refX="10.5" refY="6" markerWidth="8" markerHeight="8" orient="auto-start-reverse"><path d="M1,1 L11,6 L1,11 Z" fill="#5B6B7F"/></marker>
    <marker id="dashHollow" viewBox="0 0 12 12" refX="10.5" refY="6" markerWidth="8" markerHeight="8" orient="auto-start-reverse"><path d="M1,1 L11,6 L1,11 Z" fill="#FFFFFF" stroke="#5B6B7F" stroke-width="1.2"/></marker>
  </defs>`;
const path = pts => 'M' + pts.map(p => p[0].toFixed(1) + ' ' + p[1].toFixed(1)).join(' L');
function lineDef(pts, o = {}) {
  const dash = o.dash ? ' stroke-dasharray="7,5"' : '';
  const marker = o.marker ? ` marker-end="url(#${o.marker})"` : '';
  const col = o.dash ? '#5B6B7F' : '#3D5A80';
  return `<path d="${path(pts)}" fill="none" stroke="${col}" stroke-width="1.6"${dash}${marker}/>`;
}
function label(text, x, y, o = {}) {
  const rot = o.rot ? ` transform="rotate(-90 ${x} ${y})"` : '';
  return `<text x="${x}" y="${y}" text-anchor="${o.anchor || 'middle'}" font-family="Segoe UI,Arial" font-size="${o.size || 10.5}" font-style="italic" fill="#41586F"${rot}>${esc(text)}</text>`;
}
const B = id => boxes[id];

// ---------- routing ----------
// Free vertical corridors = gaps BETWEEN columns (and outer margins). The wide
// REPDATA column is treated as a single column; its interior cells are reached
// via the outer column gaps so lanes never pierce stacked boxes.
const gaps = [];
gaps.push({ x0: 0, x1: columns[0].x, lane: columns[0].x / 2 });
for (let i = 1; i < columns.length; i++) {
  gaps.push({ x0: columns[i - 1].right, x1: columns[i].x, lane: (columns[i - 1].right + columns[i].x) / 2 });
}
const lastCol = columns[columns.length - 1];
gaps.push({ x0: lastCol.right, x1: W, lane: (lastCol.right + W) / 2 });
// interior corridors inside the wide REPDATA column — between grid cells (same
// formula as gridPlace) so fan edges ride narrow lanes instead of crossing the
// grid interior.
{
  const rp = columns[colIdxOf('repdata')];
  const gx0 = rp.x + 10, gx1 = rp.right - 10, perRow = 4;
  if (GRID_GAP > 0) {
    const cw = (gx1 - gx0 - (perRow - 1) * GRID_GAP) / perRow;
    for (let k = 1; k < perRow; k++) {
      const cellRight = gx0 + (k - 1) * (cw + GRID_GAP) + cw;
      const cellLeft = cellRight + GRID_GAP;
      gaps.push({ x0: cellRight, x1: cellLeft, lane: (cellRight + cellLeft) / 2 });
    }
  }
}
function leftGap(b) { let best = null; for (const g of gaps) if (g.lane < b.x && (!best || g.lane > best.lane)) best = g; return best; }
function rightGap(b) { let best = null; for (const g of gaps) if (g.lane > b.right && (!best || g.lane < best.lane)) best = g; return best; }

// Slot allocator — TWO PASSES: first count how many edges share each
// (gap × gutter × side) corridor, then spread their lanes EVENLY across the
// free gap width (clamped to stay inside the gap — never pierce boxes). This
// replaces the old fixed 6px/step scheme which clamped many edges onto the same
// lane (e.g. the 16-edge reportSnap fan all folding onto one x).
const slotNeed = new Map();
const slotUsed = new Map();
function slotKey(gap, gutterY, keyPrefix) { return `${keyPrefix}:${gap.x0}:${gutterY}`; }
// pass 1 — tally corridor usage for every edge
function tallyCorridor(A, Bg) {
  if (canStraight(A, Bg)) return;
  const ba = BAND_ORDER.indexOf(A.band), bb = BAND_ORDER.indexOf(Bg.band);
  let gut;
  if (ba === bb) gut = upperGutter[A.band];
  else gut = ba < bb ? lowerGutter[A.band] : upperGutter[A.band];
  if (Bg.id === 'db') {
    const srcGap = Bg.cx >= A.cx ? rightGap(A) : leftGap(A);
    if (srcGap) slotNeed.set(slotKey(srcGap, lowerGutter.model, 's'), (slotNeed.get(slotKey(srcGap, lowerGutter.model, 's')) || 0) + 1);
    return;
  }
  const bRight = Bg.cx >= A.cx;
  const srcGap = bRight ? rightGap(A) : leftGap(A);
  const tgtGap = bRight ? leftGap(Bg) : rightGap(Bg);
  if (srcGap) slotNeed.set(slotKey(srcGap, gut, 's'), (slotNeed.get(slotKey(srcGap, gut, 's')) || 0) + 1);
  if (tgtGap) slotNeed.set(slotKey(tgtGap, gut, 't'), (slotNeed.get(slotKey(tgtGap, gut, 't')) || 0) + 1);
}
// pass 2 — allocate evenly-spaced lanes
function allocSlot(gap, gutterY, keyPrefix) {
  if (!gap) return (keyPrefix === 's' ? 20 : W - 20);
  const k = slotKey(gap, gutterY, keyPrefix);
  const total = slotNeed.get(k) || 1;
  const i = slotUsed.get(k) || 0; slotUsed.set(k, i + 1);
  const lo = gap.x0 + 4, hi = gap.x1 - 4;
  if (total <= 1) return lo;
  return lo + (hi - lo) * (i / (total - 1));
}

const upperGutter = {}; const lowerGutter = {};
upperGutter.vm = bandTop.vm - 52;
for (let i = 1; i < BAND_ORDER.length; i++) upperGutter[BAND_ORDER[i]] = bandTop[BAND_ORDER[i]] - 22;
for (let i = 0; i < BAND_ORDER.length - 1; i++) lowerGutter[BAND_ORDER[i]] = bandTop[BAND_ORDER[i + 1]] - 22;

function canStraight(A, Bg) {
  if (Math.abs(A.cx - Bg.cx) > 1) return false;
  let y0, y1;
  if (A.btmY <= Bg.y + 0.5) { y0 = A.btmY; y1 = Bg.y; }
  else if (Bg.btmY <= A.y + 0.5) { y0 = Bg.btmY; y1 = A.y; }
  else return false;
  if (y1 - y0 < 2) return true;
  for (const o of Object.values(boxes)) {
    if (o.id === A.id || o.id === Bg.id || o.band === 'data') continue;
    if (o.x <= A.cx + 1 && A.cx - 1 <= o.right) {
      if (o.y > y0 - 3 && o.btmY < y1 + 3) return false;
    }
  }
  return true;
}

function route(A, Bg) {
  if (canStraight(A, Bg)) {
    if (A.btmY <= Bg.y) return [[A.cx, A.btmY], [Bg.cx, Bg.y]];
    return [[A.cx, A.y], [Bg.cx, Bg.btmY]];
  }
  const ba = BAND_ORDER.indexOf(A.band), bb = BAND_ORDER.indexOf(Bg.band);
  let gut;
  if (ba === bb) gut = upperGutter[A.band];
  else gut = ba < bb ? lowerGutter[A.band] : upperGutter[A.band];

  // special: wide DatabaseContext — always enter at the top; drop straight down
  // at the source corridor lane (a gap between columns). 23 controller/entity
  // persistence edges therefore never share one long horizontal run through the
  // model/data seam — each rides its own free vertical corridor into the box top.
  if (Bg.id === 'db') {
    const srcGap = Bg.cx >= A.cx ? rightGap(A) : leftGap(A);
    const lA = srcGap ? allocSlot(srcGap, lowerGutter.model, 's') : 20;
    const xB = Math.min(Math.max(lA, Bg.x + 4), Bg.right - 4), yB = Bg.y;
    return [[A.right, A.midY], [lA, A.midY], [xB, yB]];
  }

  const bRight = Bg.cx >= A.cx;
  const srcGap = bRight ? rightGap(A) : leftGap(A);
  const tgtGap = bRight ? leftGap(Bg) : rightGap(Bg);
  const lA = allocSlot(srcGap, gut, 's');
  const lB = allocSlot(tgtGap, gut, 't');
  const yA = A.midY, yB = Bg.midY;
  const xA = bRight ? A.right : A.x;
  const xB = bRight ? Bg.x : Bg.right;
  return [[xA, yA], [lA, yA], [lA, gut], [lB, gut], [lB, yB], [xB, yB]];
}

// ---------- edge definitions ----------
const edges = [];
function E(a, b, o = {}) { edges.push({ a, b, o }); }
function depU(a, b) { E(a, b, { marker: 'arr' }); }   // arrow at b
function comp(a, b) { E(a, b, { marker: 'diamond' }); } // diamond at b (whole)
function agg(a, b) { E(a, b, { marker: 'hollowDiamond' }); }
function inh(a, b) { E(a, b, { marker: 'hollow' }); }
function impl(a, b) { E(a, b, { marker: 'dashHollow', dash: true }); }
function dash(a, b) { E(a, b, { marker: 'dashArr', dash: true }); }

// controllers -> their view models (upward)
depU('acctCtrl','loginVM'); depU('acctCtrl','signupVM'); depU('acctCtrl','forgotVM'); depU('acctCtrl','verifyVM'); depU('acctCtrl','resetVM');
depU('settingCtrl','acctSetVM'); depU('settingCtrl','editProfVM'); depU('settingCtrl','chgPwdVM');
depU('dashCtrl','dashVM'); depU('homeCtrl','dashVM');
depU('prodCtrl','prodCreateVM'); depU('prodCtrl','bulkReq');
depU('salesCtrl','checkoutFormVM'); depU('salesCtrl','saveCkReq'); depU('salesCtrl','recPayReq'); depU('salesCtrl','coDetailsVM');
depU('instCtrl','instMgmtVM'); depU('instCtrl','instDetailsVM'); depU('instCtrl','recInstPayVM');
depU('delivCtrl','delivReq'); depU('delivCtrl','delivItemReq');
depU('reportsCtrl','reportsVM');
depU('excelCtrl','excelSel');

// controllers -> services
E('acctCtrl','authSvc',{marker:'arr'}); E('acctCtrl','pwHash',{marker:'arr'}); E('acctCtrl','pwReset',{marker:'arr'});
E('settingCtrl','authSvc',{marker:'arr'}); E('settingCtrl','pwHash',{marker:'arr'});
E('salesCtrl','stockNotif',{marker:'arr'}); E('salesCtrl','instSvc',{marker:'arr'});
E('instCtrl','instSvc',{marker:'arr'});
E('notifCtrl','stockNotif',{marker:'arr'}); E('notifCtrl','instNotif',{marker:'arr'});
E('reportsCtrl','repComputation',{marker:'arr'}); E('reportsCtrl','iExcel',{marker:'arr'});
E('excelCtrl','iExcel',{marker:'arr'}); E('excelCtrl','instSvc',{marker:'arr'});

// controllers -> DatabaseContext
for (const c of ['acctCtrl','settingCtrl','dashCtrl','prodCtrl','salesCtrl','instCtrl','delivCtrl','notifCtrl','reportsCtrl','excelCtrl']) E(c,'db',{marker:'arr'});

// service plumbing
E('authSvc','pwHash',{marker:'arr'}); E('pwReset','pwHash',{marker:'arr'}); E('pwReset','iEmail',{marker:'arr'});
impl('smtp','iEmail'); E('smtp','emailOpts',{marker:'arr'}); E('pwReset','pwTpl',{marker:'arr'});
impl('excelSvc','iExcel'); E('excelSvc','excelCol',{marker:'arr'}); E('excelSvc','excelFile',{marker:'arr'});
// services -> entities
E('authSvc','appUser',{marker:'arr'}); E('pwReset','appUser',{marker:'arr'}); E('pwReset','pwCode',{marker:'arr'});
E('instSvc','installment',{marker:'arr'}); E('instSvc','instPay',{marker:'arr'}); E('instSvc','checkout',{marker:'arr'}); E('instSvc','custHist',{marker:'arr'});
E('stockNotif','product',{marker:'arr'}); E('stockNotif','appNotif',{marker:'arr'});
E('instNotif','installment',{marker:'arr'}); E('instNotif','appNotif',{marker:'arr'});
E('repComputation','checkout',{marker:'arr'}); E('repComputation','product',{marker:'arr'}); E('repComputation','delivery',{marker:'arr'}); E('repComputation','installment',{marker:'arr'});
// service result records
agg('instSvc','instCalc'); agg('instSvc','instPayRes'); agg('repComputation','reportSnap');
// service -> vm (produces, upward dashed)
dash('instSvc','instMgmtVM'); dash('instSvc','instDetailsVM');
// report snapshot -> all datasets
for (const id of repModelIds.slice(1)) agg('reportSnap', id);

// VM compositions
comp('acctSetVM','editProfVM'); comp('acctSetVM','chgPwdVM');
comp('dashVM','salesChart'); comp('dashVM','weeklyChart'); comp('dashVM','invHealthVM'); comp('dashVM','topSellVM');
comp('dashVM','dashCatVM'); comp('dashVM','catSalesVM'); comp('dashVM','recentTxVM');
comp('bulkReq','bulkRow'); comp('checkoutFormVM','coItemVM'); comp('checkoutFormVM','coOptVM'); comp('saveCkReq','coItemReq');
comp('instMgmtVM','instListVM'); comp('delivReq','delivItemReq');
comp('reportsVM','salesOvVM'); comp('reportsVM','invOvVM'); comp('reportsVM','delivOvVM');
comp('salesOvVM','chartPt'); comp('salesOvVM','catSalesRow'); comp('salesOvVM','txRow'); agg('salesOvVM','prodSalesRow');
comp('invOvVM','stockRow'); comp('invOvVM','catInvRow'); agg('invOvVM','stockExtremeRow');
comp('delivOvVM','delivSumRow'); comp('delivOvVM','prodDelivRow');

// VM -> model references (same-column only; the instDetailsVM cross-column refs
// to checkout/customer are omitted — connectivity is already shown via
// instSvc->checkout/custHist and the DbContext persists edges below)
depU('coDetailsVM','checkout'); depU('instDetailsVM','installment');

// controllers -> entities (direct data access, same-column reads only; the
// cross-column controller->entity links are omitted because connectivity is
// already expressed via DatabaseContext (dashed persists edges below) and the
// service->entity edges — they only added long crossings across the diagram.
E('prodCtrl','pCat',{marker:'arr'}); E('prodCtrl','product',{marker:'arr'});
E('instCtrl','installment',{marker:'arr'}); E('instCtrl','instPay',{marker:'arr'});
E('delivCtrl','delivery',{marker:'arr'}); E('delivCtrl','delivDetails',{marker:'arr'});
E('scanCtrl','product',{marker:'arr'}); E('notifCtrl','appNotif',{marker:'arr'});

// entity navigations
agg('pCat','product'); agg('customer','checkout'); agg('checkout','installment'); agg('appNotif','product');
comp('product','delivDetails'); comp('product','coItem'); comp('delivery','delivDetails'); comp('checkout','coItem'); comp('installment','instPay');
depU('custHist','customer'); depU('custHist','checkout');

// entities -> DatabaseContext (dashed persistence)
for (const e of ['appUser','pwCode','pCat','product','delivery','delivDetails','customer','checkout','coItem','installment','instPay','custHist','appNotif']) dash(e,'db');
// all controllers inherit base Controller
for (const c of ['acctCtrl','settingCtrl','homeCtrl','dashCtrl','prodCtrl','scanCtrl','salesCtrl','instCtrl','delivCtrl','notifCtrl','reportsCtrl','excelCtrl']) inh(c,'baseCtrl');

// ---------- assemble ----------
const parts = [];
parts.push(`<svg xmlns="http://www.w3.org/2000/svg" width="${W}" height="${H}" viewBox="0 0 ${W} ${H}" font-family="Segoe UI,Arial">`);
parts.push(defsSvg);
parts.push(`<rect x="0" y="0" width="${W}" height="${H}" fill="#FBFCFE"/>`);
parts.push(`<text x="${W / 2}" y="34" text-anchor="middle" font-size="20" font-weight="800" fill="#1F4E79">RJTech — Complete Application UML Class Diagram (All 4 Layers in One Image)</text>`);
parts.push(`<text x="${W / 2}" y="56" text-anchor="middle" font-size="12" fill="#5A6B7E">ViewModels · Controllers · Services · Models (EF entities) · DatabaseContext — every class present and connected</text>`);

let lgx = 30, lgy = 82;
parts.push(`<text x="${lgx}" y="${lgy}" font-size="11.5" font-weight="700" fill="#41586F">LEGEND</text>`);
function legItem(t, k, x, y) {
  const m = k === 'dep' ? 'marker-end="url(#arr)"' : k === 'inh' ? 'marker-end="url(#hollow)"' : k === 'comp' ? 'marker-end="url(#diamond)"' : k === 'agg' ? 'marker-end="url(#hollowDiamond)"' : k === 'dash' ? 'marker-end="url(#dashArr)"' : 'marker-end="url(#dashHollow)"';
  const dash = (k === 'dash' || k === 'impl') ? ' stroke-dasharray="7,5"' : '';
  const col = (k === 'dash' || k === 'impl') ? '#5B6B7F' : '#3D5A80';
  return `<line x1="${x}" y1="${y}" x2="${x + 30}" y2="${y}" stroke="${col}" stroke-width="1.6"${dash} ${m}/>` + `<text x="${x + 38}" y="${y + 4}" font-size="10.5" fill="#41586F">${t}</text>`;
}
parts.push(legItem('Dependency / uses', 'dep', lgx + 80, lgy + 6));
parts.push(legItem('Inheritance', 'inh', lgx + 230, lgy + 6));
parts.push(legItem('Composition', 'comp', lgx + 360, lgy + 6));
parts.push(legItem('Aggregation', 'agg', lgx + 510, lgy + 6));
parts.push(legItem('Persists (dashed)', 'dash', lgx + 660, lgy + 6));
parts.push(legItem('Implements', 'impl', lgx + 830, lgy + 6));
parts.push(`<text x="${lgx + 80}" y="${lgy + 24}" font-size="10" fill="#5A6B7E" font-style="italic">Band colors — green: ViewModels · blue: Controllers · purple: Services · teal: Models · slate: Data.</text>`);

const bandTint = { vm: '#F0F8F3', ctrl: '#F2F6FB', svc: '#F8F4FB', model: '#F0F4F8', data: '#ECF0F4' };
const bandTitle = band => band === 'vm' ? 'LAYER 1 — VIEW MODELS / DTOs' : band === 'ctrl' ? 'LAYER 2 — CONTROLLERS (ASP.NET MVC)' : band === 'svc' ? 'LAYER 3 — SERVICES' : band === 'model' ? 'LAYER 4 — MODELS (EF ENTITIES) & DATASET RECORDS' : 'LAYER 5 — DATA / PERSISTENCE';
BAND_ORDER.forEach((band, i) => {
  const top = bandTop[band] - 16;
  const bot = i < BAND_ORDER.length - 1 ? bandTop[BAND_ORDER[i + 1]] - 28 : dbBox.btmY + 34;
  const x = MARGIN_L - 12, w = W - (MARGIN_L - 12) * 2;
  parts.push(`<rect x="${x}" y="${top}" width="${w}" height="${bot - top}" rx="10" fill="${bandTint[band]}" stroke="${BAND_STROKE}" stroke-width="1" stroke-dasharray="6,4"/>`);
  parts.push(`<text x="${x + 12}" y="${top + 22}" font-family="Segoe UI,Arial" font-size="12" font-weight="800" letter-spacing="1.2" fill="#5A6B7E">${bandTitle(band)}</text>`);
});
// column captions
for (const c of columns) {
  parts.push(`<text x="${(c.x + c.right) / 2}" y="${bandTop.vm - 10}" text-anchor="middle" font-size="11" font-weight="800" letter-spacing="0.5" fill="#3A4A5F">${esc(c.label)}</text>`);
}

// edges — tally corridor usage first (two-pass lane spreading)
for (const e of edges) tallyCorridor(B(e.a), B(e.b));
for (const e of edges) {
  try { parts.push(link(e.a, e.b, e.o)); } catch (err) { console.error('edge fail', e.a, e.b, err.message); }
}
function link(aId, bId, o) {
  const A = B(aId), Bg = B(bId);
  const pts = route(A, Bg);
  return lineDef(pts, o);
}
// labels (after edges)
parts.push(label('creates', B('coItemVM').cx, B('checkoutFormVM').btmY - 4));
parts.push(label('delegates', B('salesCtrl').cx + 40, B('instSvc').y - 10));
parts.push(label('returns / produces', B('instSvc').cx, B('instMgmtVM').btmY - 4, { size: 10 }));
parts.push(label('computes', B('repComputation').cx, B('reportSnap').y - 10));

// boxes
for (const id of Object.keys(boxes)) parts.push(renderBox(boxes[id]));

parts.push(`<text x="${MARGIN_L}" y="${H - 22}" font-size="10.5" fill="#5A6B7E" font-style="italic">Lane-routing keeps connectors in inter-column gutters; dashed rows into ApplicationDbContext are representative DbSet persistence links (one per entity). Shared services (InstallmentService, StockNotificationService, UserAuthenticationService, PasswordHashService) are reused by multiple modules.</text>`);
parts.push('</svg>');

const out = parts.join('\n');
fs.writeFileSync(process.argv[2], out);
console.log('Wrote', process.argv[2], out.length, 'bytes, canvas', W + 'x' + H, 'boxes', Object.keys(boxes).length, 'edges', edges.length);