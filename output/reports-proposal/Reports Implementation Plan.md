# RJTech Reports implementation plan

Status: proposed, September 6, 2026. This package contains planning and design artifacts only. Application implementation has not started.

## Recommended purpose

Reports should answer a specific business question for a selected period, show how its totals were calculated, and let the user inspect or export the matching records. The Dashboard remains the quick overview. Reports provides deeper filtering, reconciliation, and printable records.

The most important rule is to keep **order value**, **money received**, and **outstanding balances** separate. They have different dates, sources, and meanings. Never add them together as a single sales total.

The sample PNG uses fictional data and illustrative charts. It shows the proposed layout, not live database results. The Word proposal contains the same design and a reviewable summary of the functionality.

## Report areas and functionality

| Area | Question answered | Filters and date basis | Results |
| --- | --- | --- | --- |
| Sales Orders | What did customers purchase? | Purchase date; current checkout status; category; customer; payment type; recorded checkout payment method | Order Value, Orders, Units Sold, Average Order; daily value chart; category totals; order details |
| Collections | Which installment payments were recorded? | Payment date; receipt method; customer; installment reference | Recorded Installment Receipts, payment count, distinct plans, average receipt; payment history |
| Installments | What is still owed today? | Current snapshot; customer; plan status; term; optional separate plan start-date range | Open plan count, current outstanding, overdue plan count, outstanding on overdue plans; balance and due-date details |
| Inventory | What needs restocking now? | Current snapshot; category; brand; stock status | Product count, units on hand, low-stock count, out-of-stock count; quantity, reorder level, current selling price |
| Deliveries | What stock arrived? | Delivery date; batch; received by; product; category; include archived records by default | Delivery count, received units, distinct products; delivery and item detail |
| Customers | Who purchased in the selected period? | Purchase date; current eligible checkout status; customer search | Distinct transacting customers, order count, order value, average value per transacting customer; customer activity |

Collections must display the subtitle **Recorded Installment Payments** in the initial release. Down payments and full-payment purchases are not individual rows in the existing installment payment ledger. The initial report cannot claim to represent all cash receipts or net collections.

Inventory and Installments are explicitly current snapshots, even when the user has previously selected a historical Sales Orders period. Hide the ordinary activity date filter for these tabs. A separate installment start-date filter, if used, selects a cohort of plans whose **current** balances are shown.

## Page layout and interactions

1. Add Reports to the existing navigation and use the shared header, blue actions, rounded cards, typography, spacing, and light/dark theme variables.
2. Show horizontal report tabs. Make them scrollable on smaller screens; stack summary cards and filters as space narrows.
3. Show report-specific filters. Date presets: Today, This Week, This Month, Last Month, and Custom. Use Monday as the proposed start of the week. Default activity reports to the current month in Asia/Manila.
4. Treat changed controls as draft filters until **Generate Report** is selected. Show an applied-filter summary and mark unapplied changes. Export uses the applied result. Reset restores the active tab’s defaults.
5. Put the date basis, current-status scope, currency, and generation timestamp near the results. Show the snapshot timestamp prominently on Inventory and Installments.
6. Summary cards and charts represent the complete matching result. Pagination changes only visible rows. Offer 10, 25, or 50 rows; use a stable secondary ID sort. The sample image uses five rows for illustration.
7. Search appropriate formatted codes and names; preserve existing checkout, product, delivery, and installment code formatters. Category and payment filters must use stored values rather than invented labels.
8. Open existing Details pages through record links or the View action. Preserve the applied filters and page on return. No checkboxes, trend column, or decorative dots inside status badges.
9. Provide labelled controls, keyboard focus, accessible chart summaries, useful empty states, and recoverable errors. Do not display a failed query as a legitimate zero total. Disable empty exports with an explanation or export a clearly labelled empty report.
10. Reuse the existing dark-mode preference and theme-change event. Update chart labels, gridlines, tooltips, and empty states when the theme changes. Print remains light and legible.

## Metric contract

### Sales Orders

- Base population: checkouts with DatePurchased in the chosen period and current Status equal to Paid or Ongoing. Cancelled and Refunded are available as explicitly labelled exception filters.
- Order Value: sum Checkout.TotalAmount once per eligible checkout when no category/product filter is applied.
- Orders: count distinct CheckoutID, never the joined item row count.
- Units Sold: sum eligible CheckoutItem.ItemQuantity.
- Average Order: Order Value divided by Orders, with zero displayed for an empty population.
- Item and category revenue: sum stored CheckoutItem.SubTotal. Use stored item prices rather than today’s Product_price.
- With a category/product filter, value and units include matching lines only. Count distinct checkouts containing those lines; label the card/table amount **Matching Item Value** and the average **Average Matching Value per Order**. The checkout Details link still opens the full order.
- Charts use the same eligible lines and filters as the result. For Top N categories, include an Other group so the chart still reconciles. Use current product-category assignments initially and label that basis if needed; editing a category can regroup prior purchases.
- Checkout payment method describes the initial checkout method. It does not describe every later installment payment method.
- Do not silently change the Dashboard definition to match this report. Its paid-only population differs from the proposed default. Explain this difference or explicitly select Paid only when comparing the two.

### Recorded installment payments

- Source: InstallmentPayment rows with Status Paid, selected by PaymentDate.
- Sum PaymentAmount; payment count is the number of included payment records. Average receipt divides that total by payment count with a zero guard.
- Do not silently remove historical receipts when the associated plan later becomes Completed or Cancelled or its checkout becomes Refunded. Current plan status can be an explicit dimension, not an automatic exclusion from historical receipts.
- The stored DownPayment is separate from InstallmentPayment rows. Do not add it into this initial ledger report as if it had an independently recorded receipt timestamp.
- Refunds currently change checkout/plan status and restore stock. They do not record a dated cash payout or reversal. Therefore this report is gross recorded installment receipts, not net cash flow.

### Outstanding and overdue plans

- Current outstanding is the sum of Balance for eligible open installment plans associated with Ongoing checkouts. Use existing business rules to classify Active/Overdue at the generation timestamp without writing status changes.
- Balance includes the contract’s financed amount and interest and excludes the separately stored down payment. It is not simply merchandise order value minus installment receipts.
- **Outstanding on overdue plans** is the entire remaining balance on plans classed as overdue. Do not label it **Amount overdue**, which requires a due-versus-paid calculation.
- A historical month-end balance or 30/60/90-day overdue amount requires agreed due schedules, dated payment allocation, and sufficient history. Defer that feature.

### Inventory, deliveries, and customers

- Inventory uses current product_quantity and existing availability/reorder rules. Avoid double-counting zero-stock products inside low-stock totals unless the UI explicitly states that overlap.
- Optional quantity × current Product_price must be labelled **Stock at selling price**. It is not purchase-cost valuation, profit, or an accounting inventory asset balance.
- Delivery counts use distinct delivery IDs. Units received sum matching delivery-detail quantities. Previous/new quantities describe that delivery record, not current stock. Archived deliveries remain part of history by default.
- Customer reporting means purchase activity, not new registrations. Customer has no registration timestamp, so do not show “new customers this month” or registration growth.

### Dates and freshness

- Use an inclusive start and exclusive next-day end, so records late on the final selected day are included.
- Display Asia/Manila as the business timezone. Existing records use DateTime.Now without timezone metadata; confirm the application server’s historical timezone before converting them. Do not blindly reinterpret old local timestamps as UTC.
- Current checkout status can restate prior-period order totals after refunds/cancellations. Show the current-status basis. Audited historical reports will need status events or saved snapshots.
- Generate all parts of one response from a consistent reporting read where feasible. Export reuses the same applied filter model but may run later, so display its own generation timestamp. Exact preview/export immutability would require a saved result version and is a later feature.

## Export and print

Initial delivery includes CSV and a printable view with browser Save as PDF. All matching rows are exported, not just the current page.

- Share filtering, sorting, field definitions, and aggregation logic between the page, export, and print view.
- Add report title, applied filters, date basis, status scope, currency, generation time, and totals to printable output. Repeat table headers across pages and show page numbering where supported.
- Use descriptive filenames containing the report and selected dates or snapshot timestamp.
- Preserve peso amounts, dates, Unicode names, quotes, commas, and line breaks correctly in CSV. Protect text cells that spreadsheet applications could interpret as formulas.
- Keep personal contact fields optional. Verify access rules for export and print as well as the HTML page.
- XLSX with typed dates/currency and a formatted native PDF can follow if the team needs them. Saved filter presets are optional later work. Scheduled report delivery is outside this proposal.

## Proposed implementation structure

These are planned files and responsibilities, not files added to the application in this task. Paths below are relative to the ASP.NET project directory `Capstone_RJTech/`.

| Planned component | Responsibility |
| --- | --- |
| Controllers/ReportsController.cs | Read-only index, CSV, and printable report endpoints; validate requested report and filters |
| Services/ReportsService.cs | Shared query composition, projections, aggregates, and result generation |
| ViewModels/ReportFilterViewModel.cs | Report type, dates, supported filters, search, sort, pagination; normalized applied values |
| ViewModels/ReportsViewModels.cs | Typed report cards, chart series, rows, date basis, and generation metadata |
| Views/Reports/Index.cshtml | Shared Reports shell and report-specific content |
| Views/Reports/Print.cshtml | Printable result using the same report data contract |
| wwwroot/css/reports.css | Responsive report layout using existing theme tokens |
| wwwroot/js/reports.js | Filter interactions, applied-state handling, accessible charts, and theme updates |
| Views/Shared/_Layout.cshtml | Reports navigation entry, following the project’s current conventions |

Prefer small typed report queries behind the service rather than a single large conditional query. Use AsNoTracking, server-side aggregation, projected columns, cancellation tokens, and server-side pagination. Measure query plans before adding indexes. Use authorization already configured by the project; if it is incomplete, establish access requirements before publishing sensitive exports.

Do not reuse installment retrieval methods that call UpdateInstallmentStatusesAsync from a report GET. Extract/reuse pure calculation logic where appropriate. Report generation must not alter business data or create notifications.

## Milestones

### M1 Definitions and shell

Confirm the proposed metric/date rules using a small worked dataset. Add the report filter contract, navigation, layout, applied-filter summary, and responsive light/dark styling. Review the Sales Orders design before connecting all report areas.

Exit: the team can explain each card and date basis; draft versus applied filters is unambiguous; UI works in both themes.

### M2 Sales Orders and exports

Implement Sales Orders totals, matching-line category filtering, charts, search, sorting, pagination, and Details links. Add CSV and Print through the same query/result definitions. Reconcile header totals against stored item subtotals and investigate mismatches instead of hiding them.

Exit: known order totals match cards, charts, table, CSV, and print; no duplicate counting when an order has multiple items.

### M3 Related reports

Add recorded installment receipts, current installment balances, current inventory, delivery history, and customer activity. Show date-basis labels and hide irrelevant filters. Include archived deliveries by default. Preserve receipt history even when a plan’s current status changes.

Exit: each report has correct sources, totals, drilldowns, filters, and a clear statement of current versus historical scope.

### M4 Validation and release preparation

Run the cases below, review actual data volume and query cost, verify export access, and inspect desktop/mobile layouts, themes, and print pagination. Agree realistic performance expectations from representative data rather than inventing a response-time guarantee.

Exit: acceptance cases pass, reports are read-only, and implementation remains inside the agreed initial scope. Later ledger/schema improvements should be separate work.

## Acceptance cases

1. One order with three item rows counts once; units sum quantities; order value does not triple.
2. Filtering one category in a mixed-category checkout includes only matching value and units, with the correct amount label.
3. A late-night purchase on the selected final day is included; the next day’s record is excluded. Test a month boundary and a leap day.
4. An installment purchased in August and completed in September remains an August order; September payments appear by PaymentDate.
5. Down payment and installment receipts are not counted twice; a partial payment lowers the correct current balance.
6. A later refund changes the current-status order population but does not erase stored installment receipt history. No refund cash amount is inferred.
7. Inventory from a past activity tab still says Current snapshot. Low/out-of-stock counts follow the project’s rules.
8. Archived delivery records remain in the default historical report; filtering items does not multiply delivery counts.
9. Totals cover all matching rows across multiple pages. CSV has all matching rows, including correctly escaped names and currency values.
10. Zero rows, zero denominator, invalid dates, query failure, and unsupported filters have clear behavior without misleading totals.
11. Dark mode updates charts and labels; keyboard use, narrow layouts, and multi-page printing remain readable.
12. Opening, refreshing, printing, and exporting reports cause no business-row or notification changes.

## Later data improvements

Add a dated payment ledger for full payments, down payments, installment receipts, and refund reversals before claiming complete or net cash collections. Add refund amount/date/method records for refund-period analytics. Add purchase costs and a cost allocation method before profit or margin. Add a complete stock movement ledger before historical stock balances. Add customer registration timestamps only if registration-growth reporting is needed. Preserve order-line category snapshots and status events if immutable historical reports become a requirement.

## Project evidence reviewed

This proposal was grounded in the current source files: Models/Checkout.cs, CheckoutItem.cs, Installment.cs, InstallmentPayment.cs, Product.cs, ProductCategory.cs, Customer.cs, Delivery.cs, DeliveryDetails.cs; Services/InstallmentService.cs; Controllers/SalesController.cs and DashboardController.cs; and the shared layout/theme patterns. No live business values are represented in the mockup.
