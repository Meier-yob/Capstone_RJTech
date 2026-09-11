# RJTech Reports table-only approval draft

Superseded by `Reports Three-Tab Approval Draft.md`. Retained only as the earlier five-tab concept.

Status: awaiting approval. This document updates the proposed Reports information architecture and interaction script. No ASP.NET application code or database schema is changed by this draft.

## Approved data boundary requested by the owner

The Reports page may read only these reporting tables:

- Sales: tblSalesOverview, tblWeeklySales, tblMonthlySales, tblYearlySales
- Product sales: tblBestSellingProduct, tblLeastSellingProduct, tblSalesByCategory
- Transactions: tblTransaction
- Inventory: tblInventoryOverview, tblProductStockSummary, tblProductCategoryOverview, tblMostStockedProduct, tblLeastStockedProduct
- Deliveries: tblDeliveryOverview, tblDeliverySummary, tblProductDeliverySummary

Foreign-key IDs are displayed as IDs. The mockups do not use Product, ProductCategory, Customer, Checkout, Delivery, CheckoutItem, Installment, or InstallmentPayment to obtain names or extra details. Product and category names can be added later only if joins to lookup tables are separately approved or if descriptive names are added to the reporting tables.

## Revised Reports navigation

1. Sales Overview
2. Product Sales
3. Transactions
4. Inventory
5. Deliveries

The earlier standalone Collections, Installments, and Customers concepts are removed. TransactionType, PaymentMethod, Amount, TransactionDate, and Status make tblTransaction the permitted transaction/collection view. The schema has no installment report table and no customer aggregate table. TotalCustomers remains an overview metric; CustomerID remains a transaction dimension.

## Shared page behavior

- Every tab uses the existing RJTech shell, Reports navigation selection, light/dark theme, Print and Export actions, rounded KPI cards, charts, and tables.
- Generate Report applies the visible filters. Reset restores that tab's defaults.
- The screen, print view, and export use the same table-only result and display LastUpdated or DateGenerated.
- Tables have search, sorting, rows-per-page, pagination, empty/loading/error states, and a stable ID sort. They have no selection checkboxes, trend columns, or status bullet dots.
- Period selectors are limited to periods represented by stored summary rows. The interface does not imply arbitrary historical reconstruction when the table contains only a current aggregate.
- IDs are shown as `Product ID`, `Category ID`, `Customer ID`, `Checkout ID`, and `Delivery ID`. They are not replaced with invented names.
- Print and export include the report title, active filters, stored period or snapshot time, and generated/exported time. Initial export may be CSV; XLSX is optional after approval.

## Sales Overview tab

Sources: tblSalesOverview, tblWeeklySales, tblMonthlySales, tblYearlySales.

Top cards show TotalSales, TotalTransactions, TotalItemsSold, and TotalCustomers from tblSalesOverview. A secondary strip shows TodaySales, WeeklySales, MonthlySales, YearlySales, LastSaleDate, and LastUpdated.

The user selects Weekly, Monthly, or Yearly. Weekly displays WeekStartDate, WeekEndDate, TotalSales, TotalTransactions, and TotalItemsSold from tblWeeklySales. Monthly displays Month, Year, and the three totals from tblMonthlySales. Yearly displays Year and the three totals from tblYearlySales. The chart and history table switch together; no date filter claims coverage beyond the stored rows.

## Product Sales tab

Sources: tblBestSellingProduct, tblLeastSellingProduct, tblSalesByCategory.

Filters use stored Period and DateGenerated values. Optional searches accept ProductID or CategoryID. The best- and least-selling lists show ProductID, TotalQuantitySold, TotalSalesAmount, Period, and DateGenerated. The category chart/table shows CategoryID, TotalQuantitySold, TotalSalesAmount, Period, and DateGenerated.

Product names, category names, brands, selling prices, and product details links are excluded because they are not present in these report tables. The page can link or label by ID only under the strict table-only boundary.

## Transactions tab

Source: tblTransaction.

Filters: TransactionDate range, TransactionType, PaymentMethod, Status, and search by TransactionID, CheckoutID, or CustomerID. Cards may derive Total Amount, Transactions, Completed, and Other Statuses from the filtered tblTransaction rows.

Charts show Amount by Payment Method and Transactions by Status. The table uses exactly TransactionID, CheckoutID, CustomerID, TransactionType, PaymentMethod, Amount, TransactionDate, and Status. This tab can answer recorded transaction questions but must not claim customer names or checkout totals from other tables.

## Inventory tab

Sources: tblInventoryOverview, tblProductStockSummary, tblProductCategoryOverview, tblMostStockedProduct, tblLeastStockedProduct.

This is a current snapshot. Cards display TotalProducts, TotalQuantity, AvailableProducts and UnavailableProducts, LowStockProducts, and OutOfStockProducts from tblInventoryOverview. Product rows display ProductID, CurrentQuantity, ReorderLevel, StockStatus, and LastUpdated from tblProductStockSummary.

Category analysis displays CategoryID, TotalProducts, TotalQuantity, AvailableQuantity, UnavailableQuantity, and LastUpdated. Most/least stocked panels use ProductID, CurrentQuantity, and LastUpdated. Product names, category names, prices, and historical stock-at-date filters are excluded.

## Deliveries tab

Sources: tblDeliveryOverview, tblDeliverySummary, tblProductDeliverySummary.

This page is a stored delivery summary, not a raw delivery-event report. Cards display TotalDeliveries, TotalItemsDelivered, TotalProductsDelivered, LastDeliveryDate, and LastUpdated. The delivery table shows DeliveryID, TotalItems, TotalProducts, and LastUpdated. The product-delivery table shows ProductID, TotalQuantityDelivered, TotalDeliveries, LastDeliveryDate, and LastUpdated.

No arbitrary delivery-date filter is shown because tblDeliverySummary has no delivery date. ProductDeliverySummary can filter or sort by LastDeliveryDate, but that value represents only the most recent delivery for that product and cannot reconstruct all deliveries in a date range.

## Interaction script for the proposed page

1. Open Reports. Sales Overview loads by default and shows the latest tblSalesOverview snapshot.
2. Change a period selector or filter. The UI marks it as pending while the current result stays visible.
3. Select Generate Report. Validate only values supported by the active reporting table, query the permitted table set, then update cards, charts, table, applied-filter summary, and timestamp together.
4. Select a chart period on Sales Overview. Switch the chart and history table between weekly, monthly, and yearly rows without mixing their date keys.
5. Search a permitted ID or choose stored filter values. Apply sorting before pagination; totals remain based on the full filtered result.
6. Select Print or Export. Reuse the same applied filters and table-only result contract. Include the table's LastUpdated or DateGenerated value so users can judge freshness.
7. Select Reset. Restore the active tab's default period/filter set and generate its latest stored result.
8. If no stored summary exists, show `No generated report data for this selection` and avoid presenting zero as a confirmed business result.

## Questions resolved by this revision

- Customer names are not displayed. tblTransaction provides CustomerID only.
- Product/category names are not displayed. The reporting tables provide ProductID and CategoryID only.
- Installment reporting is excluded.
- Customer activity reporting is excluded.
- Collections are represented through Transactions and only to the extent recorded in tblTransaction.
- Delivery history has no arbitrary date-range view under the listed fields.
- Inventory is labelled as a current stored snapshot.

## Approval gate before implementation

Implementation should begin only after approval of the five-tab structure, ID-only display, filter limitations, and the meaning of each summary timestamp. If descriptive names are required, approve specific read-only joins to Product, ProductCategory, Customer, Checkout, or Delivery, or add the required names to the report tables before implementation.
