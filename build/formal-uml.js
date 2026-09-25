// Formal UML Class Diagram generator — RJTech hierarchical architecture
// Layout: User (God Class) top-center → 3 system columns → data layer at bottom.
const fs = require('fs');

// ---------- helpers ----------
const esc = s => String(s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');

const PW = 15; // line height
const HEADER = 38;
const CAPTION = 16;
const BOTTOM_PAD = 10;

function requiredH(attrs, ops) {
  return HEADER + (attrs.length ? CAPTION + PW * attrs.length : 0)
               + (ops.length ? CAPTION + PW * ops.length : 0) + BOTTOM_PAD;
}

// ---------- palette ----------
const HEADER_FILL = '#1F4E79';
const STROKE = '#3D5A80';
const ATTR_FILL = '#2B3A52';
const OPS_FILL = '#3A4A5F';
const CAP_FILL = '#8A97A8';
const BAND_STROKE = '#C9D4DF';

// ---------- box model ----------
class Box {
  constructor(id, cfg) {
    this.id = id;
    this.title = cfg.title;
    this.stereo = cfg.stereo || '';
    this.attrs = cfg.attrs || [];
    this.ops = cfg.ops || [];
    this.x = cfg.x; this.w = cfg.w;
    this.h = Math.max(cfg.h || 0, requiredH(this.attrs, this.ops));
  }
  get y() { return this._y; }
  set y(v) { this._y = v; }
  get cx() { return this.x + this.w / 2; }
  get right() { return this.x + this.w; }
  get midY() { return this._y + this.h / 2; }
}

// ---------- render a box ----------
function renderBox(b) {
  const parts = [];
  const x = b.x, y = b.y, w = b.w, h = b.h;
  // body
  parts.push(`<rect x="${x}" y="${y}" width="${w}" height="${h}" rx="7" fill="#FFFFFF" stroke="${STROKE}" stroke-width="1.4"/>`);
  // header
  const hasStereo = !!b.stereo;
  const headH = hasStereo ? HEADER : 30;
  const path = hasStereo
    ? `M${x},${y + 7} Q${x},${y} ${x + 7},${y} L${x + w - 7},${y} Q${x + w},${y} ${x + w},${y + 7} L${x + w},${y + headH} L${x},${y + headH} Z`
    : `M${x},${y + 7} Q${x},${y} ${x + 7},${y} L${x + w - 7},${y} Q${x + w},${y} ${x + w},${y + 7} L${x + w},${y + headH} L${x},${y + headH} Z`;
  parts.push(`<path d="${path}" fill="${HEADER_FILL}"/>`);
  if (hasStereo) {
    parts.push(`<text x="${x + 12}" y="${y + 20}" font-family="Segoe UI,Arial" font-size="13.5" font-weight="700" fill="#FFFFFF">${esc(b.title)}</text>`);
    parts.push(`<text x="${x + 12}" y="${y + 35}" font-family="Segoe UI,Arial" font-size="9.5" font-style="italic" fill="#BFD7EE">${esc(b.stereo)}</text>`);
  } else {
    parts.push(`<text x="${x + 12}" y="${y + 21}" font-family="Segoe UI,Arial" font-size="13.5" font-weight="700" fill="#FFFFFF">${esc(b.title)}</text>`);
  }
  let cy = y + headH;
  if (b.attrs.length) {
    parts.push(`<text x="${x + 12}" y="${cy + 11}" font-family="Segoe UI,Arial" font-size="9" font-weight="700" letter-spacing="1" fill="${CAP_FILL}">ATTRIBUTES</text>`);
    cy += CAPTION;
    for (const a of b.attrs) {
      parts.push(`<text x="${x + 14}" y="${cy + 11}" font-family="Consolas,'Courier New',monospace" font-size="12" fill="${ATTR_FILL}">${esc(a)}</text>`);
      cy += PW;
    }
  }
  if (b.ops.length) {
    if (b.attrs.length) {
      parts.push(`<line x1="${x + 8}" y1="${cy}" x2="${x + w - 8}" y2="${cy}" stroke="${BAND_STROKE}" stroke-width="1"/>`);
    }
    parts.push(`<text x="${x + 12}" y="${cy + 11}" font-family="Segoe UI,Arial" font-size="9" font-weight="700" letter-spacing="1" fill="${CAP_FILL}">OPERATIONS</text>`);
    cy += CAPTION;
    for (const o of b.ops) {
      parts.push(`<text x="${x + 14}" y="${cy + 11}" font-family="Consolas,'Courier New',monospace" font-size="12" fill="${OPS_FILL}">${esc(o)}</text>`);
      cy += PW;
    }
  }
  return parts.join('\n');
}

// ---------- boxes (geometry: x and w fixed; y computed by stacking) ----------
const boxes = {};
const defs = [
  // ROOT — God Class
  { id: 'user', cfg: { x: 820, w: 460, h: 230, title: 'User', stereo: '«God Class» — root entity', attrs: [
    '+ UserID : string (PK)', '+ FullName : string', '+ Email : string (UQ)', '+ Phone : string', '+ DateCreated : DateTime'
  ], ops: [
    '+ Login() : bool', '+ SearchProducts(query) : List<Product>', '+ ManageProduct(product) : void', '+ ProcessCheckout(checkout) : bool'
  ]}},
  // COLUMN 1 — USER MANAGEMENT
  { id: 'owner', cfg: { x: 160, w: 500, title: 'Owner', stereo: '«entity» AppUser · tblUser', attrs: [
    '+ UserID : string (PK)', '+ FullName : string', '+ Email : string (UQ)', '+ Username : string (UQ)',
    '+ Password : string (PBKDF2 hash)', '+ Role : string', '+ DateCreated : DateTime'
  ], ops: [
    '+ SignUp()', '+ Login()', '+ Logout()', '+ UpdateProfile()', '+ ChangePassword()', '+ ForgotPassword()'
  ]}},
  { id: 'authSvc', cfg: { x: 160, w: 500, title: 'AuthService', stereo: '«service» UserAuthenticationService', attrs: [], ops: [
    '+ AuthenticateAsync(identifier, password) : AppUser?', '+ CreatePrincipal(user) : ClaimsPrincipal'
  ]}},
  { id: 'pwHash', cfg: { x: 160, w: 500, title: 'PasswordHashService', stereo: '«service»', attrs: [], ops: [
    '+ Hash(password) : string', '+ Verify(password, hash) : bool'
  ]}},
  { id: 'profileSvc', cfg: { x: 160, w: 500, title: 'ProfileService', stereo: '«service» · SettingController', attrs: [], ops: [
    '+ GetProfile() : EditProfileViewModel', '+ UpdateProfile(model) : void', '+ ChangePassword(model) : void'
  ]}},
  { id: 'pwReset', cfg: { x: 160, w: 500, title: 'PasswordResetService', stereo: '«service»', attrs: [], ops: [
    '+ AccountExistsAsync(email) : bool', '+ CreateCodeAsync(email) : PasswordResetCode?', '+ VerifyCodeAsync(id, otp) : bool',
    '+ GetVerifiedRequestAsync(id) : PasswordResetCode?', '+ ResetPasswordAsync(email, newPassword) : bool'
  ]}},
  { id: 'iEmail', cfg: { x: 160, w: 500, title: 'IEmailSender', stereo: '«interface»', attrs: [], ops: [
    '+ SendAsync(recipient, subject, htmlBody) : Task'
  ]}},
  { id: 'smtp', cfg: { x: 160, w: 500, title: 'SmtpEmailSender', stereo: '«service»', attrs: ['− options : EmailOptions'], ops: [
    '+ SendAsync(...) : Task (implements IEmailSender)'
  ]}},
  { id: 'pwCode', cfg: { x: 160, w: 500, title: 'PasswordResetCode', stereo: '«entity» tblPasswordResetCode', attrs: [
    '+ Id : string (PK)', '+ Email : string', '+ OtpHash : string (SHA-256)', '+ ExpiresAt : DateTimeOffset', '+ VerifiedAt : DateTimeOffset?'
  ], ops: []}},
  { id: 'emailOpts', cfg: { x: 160, w: 500, title: 'EmailOptions', stereo: '«config» appsettings', attrs: [
    '+ Host : string', '+ Port : int', '+ Username : string', '+ Password : string', '+ FromAddress : string', '+ EnableSsl : bool'
  ], ops: []}},
  // COLUMN 2 — CONTENT SERVICES
  { id: 'productSvc', cfg: { x: 800, w: 500, title: 'ProductService', stereo: '«service» · ProductController', attrs: [], ops: [
    '+ Create(model) : void', '+ BulkCreate(request) : void', '+ UpdateProductDetails(...) : void', '+ UploadImage(id, image) : void',
    '+ SearchProducts(query) : List<Product>', '+ DeleteProduct(id) : void', '+ FormatProductCode(category, id) : string'
  ]}},
  { id: 'pcat', cfg: { x: 800, w: 500, title: 'ProductCategory', stereo: '«entity» ProductCategories', attrs: [
    '+ category_ID : int (PK)', '+ category_name : string (UQ)'
  ], ops: ['+ CreateCategory()', '+ EditCategory()']}},
  { id: 'product', cfg: { x: 800, w: 500, title: 'Product', stereo: '«entity» Products', attrs: [
    '+ product_ID : int (PK)', '+ product_name : string', '+ product_brand : string', '+ product_description : string?',
    '+ product_Image : byte[]?', '+ product_ImageContentType : string?', '+ product_quantity : int', '+ reorder_level : int',
    '+ Product_price : decimal(18,2)', '+ is_serialized : bool', '+ product_status : string', '+ category_ID : int (FK)', '+ product_code : string (computed)'
  ], ops: ['+ formatted_code() : string', '+ product_code() : string']}},
  { id: 'appNotif', cfg: { x: 800, w: 500, title: 'AppNotification', stereo: '«entity» Notifications', attrs: [
    '+ notification_ID : int (PK)', '+ product_ID : int? (FK)', '+ title : string', '+ message : string', '+ notification_type : string',
    '+ action_url : string', '+ created_at : DateTime', '+ is_read : bool'
  ], ops: []}},
  { id: 'stockNotif', cfg: { x: 800, w: 500, title: 'StockNotificationService', stereo: '«service»', attrs: [], ops: [
    '+ Synchronize() : void'
  ]}},
  { id: 'delivDetails', cfg: { x: 800, w: 500, title: 'DeliveryDetails', stereo: '«entity»', attrs: [
    '+ deldetails_ID : int (PK)', '+ product_quantity : int', '+ previous_quantity : int', '+ new_quantity : int',
    '+ product_ID : int (FK)', '+ delivery_ID : int (FK)'
  ], ops: []}},
  { id: 'delivery', cfg: { x: 800, w: 500, title: 'Delivery', stereo: '«entity» Deliveries', attrs: [
    '+ delivery_ID : int (PK)', '+ date_delivered : DateTime', '+ received_by : string', '+ batch_ID : string (UQ)', '+ is_archived : bool'
  ], ops: []}},
  { id: 'deliverySvc', cfg: { x: 800, w: 500, title: 'DeliveryService', stereo: '«service» · DeliveryController', attrs: [], ops: [
    '+ Receive()', '+ CompleteDelivery(request) : void', '+ GetDeliveries() : List<Delivery>', '+ DeleteDeliveryReceipt(id) : void', '+ SearchProductForDelivery(query)'
  ]}},
  // COLUMN 3 — SALES & PERSISTENCE
  { id: 'salesSvc', cfg: { x: 1440, w: 520, title: 'SalesService', stereo: '«service» · SalesController', attrs: [], ops: [
    '+ SearchCustomers(query)', '+ SearchProducts(query)', '+ CheckSerialNumber(serial) : bool', '+ CompleteCheckout(request) : void',
    '+ RefundCheckout(id) : void', '+ DeleteCheckout(id) : void', '+ DeleteCustomers(ids) : void', '+ RecordInstallmentPayment(request)'
  ]}},
  { id: 'customer', cfg: { x: 1440, w: 520, title: 'Customer', stereo: '«entity» tblCustomer', attrs: [
    '+ customer_ID : int (PK)', '+ customer_FullName : string', '+ customer_Email : string (UQ · @gmail.com)', '+ customer_Phone : string (11 digits)', '+ customer_Address : string'
  ], ops: []}},
  { id: 'checkout', cfg: { x: 1440, w: 520, title: 'Checkout', stereo: '«entity» tblCheckout', attrs: [
    '+ CheckoutID : int (PK)', '+ CustomerID : int (FK)', '+ TotalAmount : decimal(18,2)', '+ PaymentMethod : string',
    '+ PaymentType : string', '+ DatePurchased : DateTime', '+ Status : string'
  ], ops: []}},
  { id: 'checkoutItem', cfg: { x: 1440, w: 520, title: 'CheckoutItem', stereo: '«entity» tblCheckoutItem', attrs: [
    '+ CheckoutItemID : int (PK)', '+ CheckoutID : int (FK)', '+ ProductID : int (FK)', '+ SerialNo : string?',
    '+ ItemQuantity : int', '+ Price : decimal(18,2)', '+ SubTotal : decimal(18,2)'
  ], ops: []}},
  { id: 'installment', cfg: { x: 1440, w: 520, title: 'Installment', stereo: '«entity» tblInstallment', attrs: [
    '+ InstallmentID : int (PK)', '+ CheckoutID : int (FK)', '+ Months : int', '+ DownPayment : decimal(18,2)', '+ InterestRate : decimal(5,2)',
    '+ TotalAmount : decimal(18,2)', '+ Balance : decimal(18,2)', '+ MonthlyPayment : decimal(18,2)', '+ MonthsPaid : int',
    '+ MonthsRemaining : int', '+ StartDate : DateTime', '+ Status : string'
  ], ops: []}},
  { id: 'installmentSvc', cfg: { x: 1440, w: 520, title: 'InstallmentService', stereo: '«service»', attrs: [], ops: [
    '+ Calculate(amount, months) : InstallmentCalculation', '+ RecordPaymentAsync(...) : InstallmentPaymentResult', '+ CalculateMonthsPaid(...) : int',
    '+ DetermineInstallmentStatus(...) : string', '+ UpdateInstallmentStatusesAsync()'
  ]}},
  { id: 'installmentPayment', cfg: { x: 1440, w: 520, title: 'InstallmentPayment', stereo: '«entity» tblInstallmentPayment', attrs: [
    '+ PaymentID : int (PK)', '+ InstallmentID : int (FK)', '+ PaymentMethod : string', '+ PaymentAmount : decimal(18,2)', '+ PaymentDate : DateTime', '+ Status : string'
  ], ops: []}},
  { id: 'purchaseHistory', cfg: { x: 1440, w: 520, title: 'CustomerPurchaseHistory', stereo: '«entity» tblCustomerPurchaseHistory', attrs: [
    '+ HistoryID : int (PK)', '+ CustomerID : int (FK)', '+ CheckoutID : int (FK)', '+ PurchaseDate : DateTime', '+ TotalAmount : decimal(18,2)', '+ PaymentMethod : string'
  ], ops: []}},
  // DATABASE CONNECTOR — bottom layer (wider)
  { id: 'db', cfg: { x: 100, w: 1960, title: 'DatabaseContext — ApplicationDbContext', stereo: '«connector» Entity Persistence Layer', attrs: [
    '+ DbSet<Product> Products', '+ DbSet<ProductCategory> ProductCategories', '+ DbSet<Delivery> Deliveries', '+ DbSet<DeliveryDetails> DeliveryDetails',
    '+ DbSet<AppNotification> Notifications', '+ DbSet<Customer> Customers', '+ DbSet<Checkout> Checkouts', '+ DbSet<CheckoutItem> CheckoutItems',
    '+ DbSet<Installment> Installments', '+ DbSet<InstallmentPayment> InstallmentPayments', '+ DbSet<CustomerPurchaseHistory> CustomerPurchaseHistories',
    '+ DbSet<AppUser> Users', '+ DbSet<PasswordResetCode> PasswordResetCodes'
  ], ops: ['+ SaveChanges() : int', '+ Find<TEntity>(id) : TEntity?']}},
];
defs.forEach(d => { boxes[d.id] = new Box(d.id, d.cfg); });

// ---------- stacking (columns) ----------
const USER_Y = 60;
boxes.user.y = USER_Y;

function stack(names, startY, gap) {
  let y = startY;
  for (const n of names) { boxes[n].y = y; y += boxes[n].h + gap; }
  return y - gap; // last bottom
}
const col1 = ['owner','authSvc','pwHash','profileSvc','pwReset','iEmail','smtp','pwCode','emailOpts'];
const col2 = ['productSvc','pcat','product','appNotif','stockNotif','delivDetails','delivery','deliverySvc'];
const col3 = ['salesSvc','customer','checkout','checkoutItem','installment','installmentSvc','installmentPayment','purchaseHistory'];
const b1 = stack(col1, 330, 20);
const b2 = stack(col2, 330, 20);
const b3 = stack(col3, 330, 20);
const DB_Y = 2040;
boxes.db.y = DB_Y;
const dbBottom = DB_Y + boxes.db.h;

const W = 2120;
const H = 2520;

// ---------- bands ----------
function band(x, y, w, h, label, fill) {
  return `<rect x="${x}" y="${y}" width="${w}" height="${h}" rx="10" fill="${fill}" stroke="${BAND_STROKE}" stroke-width="1" stroke-dasharray="6,4"/>`
    + `<text x="${x + w / 2}" y="${y + 24}" text-anchor="middle" font-family="Segoe UI,Arial" font-size="12.5" font-weight="800" letter-spacing="1.5" fill="#5A6B7E">${esc(label)}</text>`;
}

// ---------- markers ----------
const defsSvg = `
  <defs>
    <marker id="arr" viewBox="0 0 12 12" refX="10.5" refY="6" markerWidth="8" markerHeight="8" orient="auto-start-reverse">
      <path d="M1,1 L11,6 L1,11 Z" fill="#3D5A80"/>
    </marker>
    <marker id="hollow" viewBox="0 0 12 12" refX="10.5" refY="6" markerWidth="8" markerHeight="8" orient="auto-start-reverse">
      <path d="M1,1 L11,6 L1,11 Z" fill="#FFFFFF" stroke="#3D5A80" stroke-width="1.2"/>
    </marker>
    <marker id="diamond" viewBox="0 0 12 12" refX="10.5" refY="6" markerWidth="8" markerHeight="8" orient="auto-start-reverse">
      <path d="M6,0 L12,6 L6,12 L0,6 Z" fill="#3D5A80"/>
    </marker>
    <marker id="dashArr" viewBox="0 0 12 12" refX="10.5" refY="6" markerWidth="8" markerHeight="8" orient="auto-start-reverse">
      <path d="M1,1 L11,6 L1,11 Z" fill="#5B6B7F"/>
    </marker>
    <marker id="dashHollow" viewBox="0 0 12 12" refX="10.5" refY="6" markerWidth="8" markerHeight="8" orient="auto-start-reverse">
      <path d="M1,1 L11,6 L1,11 Z" fill="#FFFFFF" stroke="#5B6B7F" stroke-width="1.2"/>
    </marker>
  </defs>`;

// ---------- connectors ----------
// path(...) builds an SVG path from point list.
const path = pts => 'M' + pts.map(p => p[0].toFixed(1) + ' ' + p[1].toFixed(1)).join(' L');

function lineDef(pts, o = {}) {
  const dash = o.dash ? ' stroke-dasharray="7,5"' : '';
  const marker = o.marker ? ` marker-end="url(#${o.marker})"` : '';
  const col = o.dash ? '#5B6B7F' : '#3D5A80';
  return `<path d="${path(pts)}" fill="none" stroke="${col}" stroke-width="1.6"${dash}${marker}/>`;
}
function label(text, x, y, o = {}) {
  const rot = o.rot ? ` transform="rotate(-90 ${x} ${y})"` : '';
  const anchor = o.anchor || 'middle';
  const fill = o.fill || '#41586F';
  return `<text x="${x}" y="${y}" text-anchor="${anchor}" font-family="Segoe UI,Arial" font-size="${o.size || 10.5}" font-style="italic" fill="${fill}"${rot}>${esc(text)}</text>`;
}
const B = id => boxes[id]; // box getter

// anchor helpers
const right = (id, f = 0.5) => [B(id).right, B(id).y + B(id).h * f];
const left = (id, f = 0.5) => [B(id).x, B(id).y + B(id).h * f];
const top = id => [B(id).cx, B(id).y];
const btm = id => [B(id).cx, B(id).y + B(id).h];
// L-shaped route through a gutter
const gutter = (id, fFrom, ch, toId, fTo) => {
  const a = right(id, fFrom), b = right(toId, fTo);
  return [[a[0], a[1]], [ch, a[1]], [ch, b[1]], [b[0], b[1]]];
};
const gutterLeft = (id, fFrom, ch, toId, fTo) => {
  const a = left(id, fFrom), b = left(toId, fTo);
  return [[a[0], a[1]], [ch, a[1]], [ch, b[1]], [b[0], b[1]]];
};

const lines = [];
const labels = [];

// ===== ROOT connections =====
lines.push(lineDef([[B('user').cx, B('user').y + B('user').h], top('productSvc')], { marker: 'arr' }));
labels.push(label('uses', 1064, 296, { anchor: 'start' }));

lines.push(lineDef([[1080, B('user').y + B('user').h], [B('salesSvc').cx - 20, B('salesSvc').y]], { marker: 'arr' }));
labels.push(label('uses', 1095, 322, { anchor: 'start' }));

// Customer -> User inheritance (routed via right gutter to avoid SalesService)
const uBottom = B('user').y + B('user').h;
const custTop = B('customer').y;
lines.push(lineDef([[1700, custTop], [2050, custTop], [2050, uBottom], [1280, uBottom]], { marker: 'hollow' }));
labels.push(label('inheritance', 1660, 284, { anchor: 'middle' }));
lines.push(lineDef([top('owner'), [900, uBottom]], { marker: 'hollow' }));
labels.push(label('inheritance', 520, 282, { anchor: 'middle' }));

// ===== COLUMN 1 — User Management =====
lines.push(lineDef([btm('owner'), top('authSvc')], { marker: 'arr' }));
lines.push(lineDef([btm('authSvc'), top('pwHash')], { marker: 'arr' }));
lines.push(lineDef(gutter('owner', 0.35, 700, 'profileSvc', 0.5), { marker: 'arr' }));
labels.push(label('manages', 707, B('profileSvc').y - 40, { rot: 1, anchor: 'middle' }));
lines.push(lineDef(gutterLeft('owner', 0.35, 55, 'pwReset', 0.5), { marker: 'arr' }));
labels.push(label('issues OTP', 47, B('pwReset').y - 20, { rot: 1 }));
lines.push(lineDef(gutter('pwReset', 0.5, 724, 'pwCode', 0.5), { marker: 'arr' }));
labels.push(label('creates', 731, B('pwCode').y - 18, { rot: 1 }));
lines.push(lineDef([btm('pwReset'), top('iEmail')], { marker: 'arr' }));
// SmtpEmailSender --implements--> IEmailSender  (adjacent vertical: iEmail sits directly above smtp)
lines.push(lineDef([top('smtp'), btm('iEmail')], { dash: true, marker: 'dashHollow' }));
labels.push(label('implements', 424, B('iEmail').y + B('iEmail').h - 10, { anchor: 'start' }));
lines.push(lineDef(gutter('smtp', 0.9, 712, 'emailOpts', 0.05), { marker: 'arr' }));
labels.push(label('reads', 719, B('emailOpts').midY - 60, { rot: 1 }));
lines.push(lineDef(gutterLeft('pwCode', 0.5, 60, 'db', 0.55), { dash: true, marker: 'dashArr' }));
labels.push(label('persists', 52, B('db').y + 120, { rot: 1 }));

// ===== COLUMN 2 — Content Services =====
lines.push(lineDef([btm('productSvc'), top('pcat')], { marker: 'arr' }));
// pcat --> product  (composition, diamond at whole = ProductCategory)
lines.push(lineDef([[1150, B('pcat').y + B('pcat').h], [1150, B('product').y]], { marker: 'diamond' }));
labels.push(label('1', 1158, B('pcat').y + B('pcat').h - 3, { anchor: 'start' }));
labels.push(label('1..*', 1158, B('product').y + 4, { anchor: 'start' }));
lines.push(lineDef(gutter('productSvc', 0.35, 1340, 'product', 0.5), { marker: 'arr' }));
labels.push(label('manages', 1347, B('product').midY - 60, { rot: 1 }));
lines.push(lineDef([btm('product'), top('appNotif')], { marker: 'arr' }));
labels.push(label('triggers low-stock', 1064, B('appNotif').y - 12, { anchor: 'start' }));
lines.push(lineDef([btm('appNotif'), top('stockNotif')], { marker: 'arr' }));
lines.push(lineDef(gutterLeft('productSvc', 0.55, 745, 'stockNotif', 0.5), { marker: 'arr' }));
labels.push(label('invokes', 738, B('stockNotif').midY - 8, { rot: 1 }));
lines.push(lineDef(gutter('product', 0.62, 1322, 'delivDetails', 0.5), { marker: 'arr' }));
labels.push(label('restocked in', 1329, B('delivDetails').midY, { rot: 1 }));
lines.push(lineDef([btm('delivDetails'), top('delivery')], { marker: 'arr' }));
labels.push(label('belongs to', 1064, B('delivery').y - 12, { anchor: 'start' }));
lines.push(lineDef([top('deliverySvc'), btm('delivery')], { marker: 'arr' }));
lines.push(lineDef(gutter('deliverySvc', 0.5, 1338, 'delivDetails', 0.5), { marker: 'arr' }));
labels.push(label('creates', 1345, B('delivDetails').midY + 20, { rot: 1 }));
lines.push(lineDef(gutterLeft('product', 0.2, 755, 'db', 0.35), { dash: true, marker: 'dashArr' }));
labels.push(label('persists', 748, B('db').y + 60, { rot: 1 }));

// ===== COLUMN 3 — Sales & Persistence =====
lines.push(lineDef([btm('salesSvc'), top('customer')], { marker: 'arr' }));
lines.push(lineDef([[1620, B('customer').y + B('customer').h], [1620, B('checkout').y]], {}));
labels.push(label('1', 1540, B('customer').y + B('customer').h + 10, { anchor: 'middle' }));
labels.push(label('1..*', 1540, B('checkout').y + 10, { anchor: 'middle' }));
lines.push(lineDef(gutterLeft('salesSvc', 0.42, 1390, 'checkout', 0.22), { marker: 'arr' }));
labels.push(label('creates', 1383, B('checkout').midY - 30, { rot: 1 }));
// checkout *-- checkoutItem  (diamond at checkOut)
lines.push(lineDef([[1500, B('checkoutItem').y], [1500, B('checkout').y + B('checkout').h]], { marker: 'diamond' }));
labels.push(label('1', 1472, B('checkout').y + B('checkout').h + 6, { anchor: 'end' }));
labels.push(label('1..*', 1472, B('checkoutItem').y - 6, { anchor: 'end' }));
// checkout o-- installment (0..1)
lines.push(lineDef(gutterLeft('installment', 0.5, 1405, 'checkout', 0.5), { marker: 'diamond' }));
labels.push(label('0..1', 1398, B('checkout').midY, { rot: 1 }));
// salesSvc --delegates--> installmentSvc  (right gutter, avoids left-gutter crowding)
lines.push(lineDef(gutter('salesSvc', 0.65, 1996, 'installmentSvc', 0.5), { marker: 'arr' }));
labels.push(label('delegates', 2003, B('installmentSvc').midY - 20, { rot: 1 }));
// installment *-- installmentPayment (diamond at installment)
lines.push(lineDef(gutterLeft('installmentPayment', 0.5, 1395, 'installment', 0.6), { marker: 'diamond' }));
labels.push(label('1..*', 1388, B('installment').midY + 10, { rot: 1 }));
lines.push(lineDef([btm('installmentSvc'), top('installmentPayment')], { marker: 'arr' }));
labels.push(label('applies', 1708, B('installmentPayment').y - 10, { anchor: 'start' }));
lines.push(lineDef(gutter('salesSvc', 0.5, 2000, 'purchaseHistory', 0.5), { marker: 'arr' }));
labels.push(label('records', 2007, B('purchaseHistory').midY - 10, { rot: 1 }));
lines.push(lineDef(gutter('installmentSvc', 0.62, 1993, 'purchaseHistory', 0.32), { marker: 'arr' }));
labels.push(label('logs payment', 2000, B('purchaseHistory').y + 40, { rot: 1 }));
lines.push(lineDef([btm('purchaseHistory'), [B('db').cx, B('db').y]], { dash: true, marker: 'dashArr' }));
labels.push(label('persists', B('db').cx + 12, B('db').y - 6, { anchor: 'start' }));

// ===== assemble =====
const parts = [];
parts.push(`<svg xmlns="http://www.w3.org/2000/svg" width="${W}" height="${H}" viewBox="0 0 ${W} ${H}" font-family="Segoe UI,Arial">`);
parts.push(defsSvg);
parts.push(`<rect x="0" y="0" width="${W}" height="${H}" fill="#FBFCFE"/>`);

// bands (behind boxes)
parts.push(band(130, 300, 560, b1 - 300 + 25, 'SYSTEM 1 — USER MANAGEMENT', '#F2F6FB'));
parts.push(band(770, 300, 560, b2 - 300 + 25, 'SYSTEM 2 — CONTENT SERVICES', '#F7F5EF'));
parts.push(band(1410, 300, 560, b3 - 300 + 25, 'SYSTEM 3 — SALES & PERSISTENCE (DATABASE CONNECTOR)', '#EFF6EF'));
parts.push(band(60, 2030, 2005, dbBottom - 2030 + 15, 'DATABASE CONNECTOR — ENTITY PERSISTENCE LAYER (ApplicationDbContext)', '#E9EDF2'));

// heading
parts.push(`<text x="${W / 2}" y="38" text-anchor="middle" font-size="19" font-weight="800" fill="#1F4E79">RJTech — Formal UML Class Diagram (Hierarchical Architecture)</text>`);

// connectors
for (const l of lines) parts.push(l);
for (const t of labels) parts.push(t);

// boxes
const order = ['user', 'owner','authSvc','pwHash','profileSvc','pwReset','iEmail','smtp','pwCode','emailOpts',
  'productSvc','pcat','product','appNotif','stockNotif','delivDetails','delivery','deliverySvc',
  'salesSvc','customer','checkout','checkoutItem','installment','installmentSvc','installmentPayment','purchaseHistory','db'];
for (const id of order) parts.push(renderBox(boxes[id]));

// legend
const ly = 2400;
parts.push(`<text x="120" y="${ly}" font-size="11.5" font-weight="700" fill="#41586F">LEGEND</text>`);
function legItem(labelText, x, y, kind) {
  const p1 = kind === 'dep' ? `<line x1="${x}" y1="${y}" x2="${x + 34}" y2="${y}" stroke="#3D5A80" stroke-width="1.6" marker-end="url(#arr)"/>`
            : kind === 'inh' ? `<line x1="${x}" y1="${y}" x2="${x + 34}" y2="${y}" stroke="#3D5A80" stroke-width="1.6" marker-end="url(#hollow)"/>`
            : kind === 'comp' ? `<line x1="${x}" y1="${y}" x2="${x + 34}" y2="${y}" stroke="#3D5A80" stroke-width="1.6" marker-end="url(#diamond)"/>`
            : `<line x1="${x}" y1="${y}" x2="${x + 34}" y2="${y}" stroke="#5B6B7F" stroke-width="1.6" stroke-dasharray="7,5" marker-end="url(#dashArr)"/>`;
  return p1 + `<text x="${x + 42}" y="${y + 4}" font-size="11" fill="#41586F">${labelText}</text>`;
}
parts.push(legItem('Dependency / uses', 120, ly + 26, 'dep'));
parts.push(legItem('Generalization (inheritance)', 120, ly + 52, 'inh'));
parts.push(legItem('Composition (whole) — 1 : 1..*', 520, ly + 26, 'comp'));
parts.push(legItem('Persists (dashed) / Implements', 520, ly + 52, 'dash'));
parts.push(`<text x="120" y="${ly + 78}" font-size="10.5" fill="#5A6B7E" font-style="italic">Composite children (DeliveryDetails, CheckoutItem, InstallmentPayment) are cascade-deleted with their parent. Dashed rows into DatabaseContext are representative — it exposes a DbSet&lt;T&gt; for every «entity» class.</text>`);

parts.push('</svg>');

const out = parts.join('\n');
fs.writeFileSync(process.argv[2], out);
console.log('Wrote', process.argv[2], out.length, 'bytes');