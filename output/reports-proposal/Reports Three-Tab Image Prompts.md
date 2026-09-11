# Reports three-tab image prompt set

Mode: built-in image generation in edit mode. The existing three-tab mockups were used as visual references for the RJTech shell, 1536 by 1024 dimensions, typography, cards, charts, filters, navigation, and tables.

## Shared prompt

Create a high-fidelity RJTech Reports page with exactly three top tabs: Sales Overview, Inventory Overview, and Delivery Overview. Preserve the project sidebar, Installments navigation item, global Search workspace field, light/dark theme support, Print, Export, Generate Report, Reset, timestamps, rounded cards, blue actions, charts, and compact tables. Report values come from the reporting tables assigned to the active tab. Product Name, Category Name, Customer Name, and Payment Type may be resolved through their existing foreign-key relationships for display. Do not include table checkboxes, trend table columns, status dots, Calendar, Refund Rate, installment report content, extra report tabs, or unsupported report values.

## Sales Overview refinement prompt

Use four cards labelled exactly Total Sales, Total Checkouts, Total Sold Items, and Total Customers. Total Checkouts displays tblSalesOverview.TotalTransactions and Total Sold Items displays tblSalesOverview.TotalItemsSold. Retain Today Sales, Weekly Sales, Monthly Sales, Yearly Sales, Last Sale Date, and Last Updated context. Keep Weekly, Monthly, and Yearly controls backed by tblWeeklySales, tblMonthlySales, and tblYearlySales.

Show Sales by Category using Category Name instead of CategoryID. Best Selling Product and Least Selling Product tables include Product ID, Product Name, Qty Sold, and Sales Amount. Remove the separate Period/Generated div or section and rebalance the two product panels across the row.

Recent Transactions displays Checkout ID, Customer Name, Payment Type, Payment Method, Amount, Transaction Date, and Status. Do not display TransactionID or TransactionType. Resolve Customer Name through CustomerID and Payment Type through the CheckoutID relationship. Format Checkout ID exactly like the project code, `CHK-{CheckoutID:D3}`, with examples `CHK-024`, `CHK-023`, `CHK-022`, and `CHK-021`. Payment Type uses `Full Payment` or `Installment`. Status uses `Paid`, `Ongoing`, or `Refunded`, matching Sales Summary and using no dot indicators.

## Inventory Overview refinement prompt

Use tblInventoryOverview for the snapshot cards. Product Stock Summary displays Product ID, Product Name, Category Name, Current Quantity, Reorder Level, Stock Status, and Last Updated. Quantity by Category and Product Category Overview use Category Name instead of CategoryID or CAT codes. Most Stocked Product and Least Stocked Product display both Product ID and Product Name with Current Quantity. Preserve the current-snapshot label and do not add prices or a historical date range.

## Delivery Overview refinement prompt

Use tblDeliveryOverview for Total Deliveries, Total Items Delivered, Total Products Delivered, Last Delivery Date, and Last Updated. Preserve Delivery Summary. Product Delivery Summary displays Product ID, Product Name, Total Quantity Delivered, Total Deliveries, Last Delivery Date, and Last Updated. Keep Search Delivery ID or Product ID and the Last delivery date sort control. Do not add batch, receiver, individual delivery date, previous quantity, new quantity, or an arbitrary date-range filter.

## Revision output

- Three Tab v3 01 Sales Overview.png
- Three Tab v2 02 Inventory Overview.png
- Three Tab v2 03 Delivery Overview.png
