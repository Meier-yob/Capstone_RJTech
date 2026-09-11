# RJTech Reports three-tab approval draft

Status: approved and implemented. The Reports feature follows this three-tab design and interaction contract.

## Top-level tabs

The Reports page contains exactly three top-level tabs:

1. Sales Overview
2. Inventory Overview
3. Delivery Overview

There are no Product Sales, Transactions, Collections, Installments, or Customers top-level tabs. Product-sales and transaction summaries appear as sections inside Sales Overview.

Report totals, quantities, dates, and statuses come only from the reporting tables assigned to the active tab. Product Name, Category Name, and Customer Name are display labels resolved through the existing ProductID, CategoryID, and CustomerID foreign-key relationships. These lookups do not replace or recalculate report values. Checkout ID, Delivery ID, and Product ID remain visible as formatted identifiers where specified.

## Shared report shell

- Keep the existing RJTech navigation, typography, spacing, rounded cards, blue actions, Print, Export, and light/dark theme.
- Generate Report applies the active tab's controls. Reset restores its defaults.
- Display the relevant LastUpdated value so the user can judge freshness.
- Cards, charts, tables, print, and export come from the same active-tab result.
- Support sorting, search, rows per page, pagination, loading, empty, and error states.
- Tables contain no checkboxes or trend columns. Status values contain no dot indicators.
- A missing summary row is shown as “No generated report data for this selection,” rather than a confirmed zero.

## Sales Overview

### Permitted reporting tables

- tblSalesOverview
- tblWeeklySales
- tblMonthlySales
- tblYearlySales
- tblBestSellingProduct
- tblLeastSellingProduct
- tblSalesByCategory
- tblTransaction

### Page structure

The four header cards are labelled exactly:

1. Total Sales — TotalSales from tblSalesOverview
2. Total Checkouts — TotalTransactions from tblSalesOverview
3. Total Sold Items — TotalItemsSold from tblSalesOverview
4. Total Customers — TotalCustomers from tblSalesOverview

A compact secondary strip shows TodaySales, WeeklySales, MonthlySales, YearlySales, LastSaleDate, and LastUpdated.

The Sales History panel has Weekly, Monthly, and Yearly controls:

- Weekly reads WeeklySalesID, WeekStartDate, WeekEndDate, TotalSales, TotalTransactions, and TotalItemsSold from tblWeeklySales.
- Monthly reads MonthlySalesID, Month, Year, TotalSales, TotalTransactions, and TotalItemsSold from tblMonthlySales.
- Yearly reads YearlySalesID, Year, TotalSales, TotalTransactions, and TotalItemsSold from tblYearlySales.

The control changes both the chart and period-history table. It does not create an arbitrary date range from an overview row.

Product Performance contains:

- Best Selling Product: ProductID, Product Name, TotalQuantitySold, and TotalSalesAmount. Product Name is resolved by ProductID.
- Least Selling Product: ProductID, Product Name, TotalQuantitySold, and TotalSalesAmount. Product Name is resolved by ProductID.
- Sales by Category: Category Name, TotalQuantitySold, and TotalSalesAmount. Category Name is resolved by CategoryID.

Period and DateGenerated remain source fields in their assigned tables when needed to select stored summary rows, but the Sales Overview does not display a separate Period/Generated div, card, or section.

Recent Transactions displays Checkout ID, Customer Name, Payment Type, Payment Method, Amount, Transaction Date, and Status. TransactionID and TransactionType are not displayed. Customer Name is resolved through CustomerID. Payment Type is resolved through the CheckoutID relationship because tblTransaction does not contain a PaymentType column. Checkout ID follows the existing project format `CHK-{CheckoutID:D3}`, such as `CHK-024`. Payment Type uses the project's `Full Payment` and `Installment` values. Status uses the Sales Summary values `Paid`, `Ongoing`, and `Refunded`, without dot indicators. The transaction section may filter by TransactionDate, Payment Type, PaymentMethod, Status, and approved IDs.

### Sales interaction script

1. Open Reports. Sales Overview loads as the default tab and displays the latest tblSalesOverview row.
2. Select Weekly, Monthly, or Yearly. Generate Report loads the matching stored sales-history rows and updates the chart and history table together.
3. Change the stored product-summary selection. Refresh the best-selling, least-selling, and category panels; show Product Name and Category Name through their foreign-key lookups without a separate Period/Generated panel.
4. Change transaction controls. Generate Report loads matching tblTransaction rows, hides TransactionID and TransactionType, resolves Customer Name and Payment Type, formats CheckoutID as `CHK-###`, and uses the Sales Summary statuses.
5. Print or export the entire active Sales Overview result with the same visible cards, customer/product/category names, identifiers, filters, and LastUpdated context.

## Inventory Overview

### Permitted reporting tables

- tblInventoryOverview
- tblProductStockSummary
- tblProductCategoryOverview
- tblMostStockedProduct
- tblLeastStockedProduct

### Page structure

The header cards show TotalProducts, TotalQuantity, AvailableProducts, UnavailableProducts, LowStockProducts, OutOfStockProducts, and LastUpdated from tblInventoryOverview.

Product Stock Summary displays Product ID, Product Name, Category Name, Current Quantity, Reorder Level, Stock Status, and Last Updated. Product Name and Category Name are resolved through ProductID and its existing category relationship.

Product Category Overview displays Category Name, Total Products, Total Quantity, Available Quantity, Unavailable Quantity, and Last Updated. Category Name is resolved by CategoryID; category IDs and category codes are not shown in this section or its chart.

Most Stocked Product and Least Stocked Product each show Product ID, Product Name, Current Quantity, and Last Updated. Product Name is resolved by ProductID.

This tab is labelled as a stored inventory snapshot. It has StockStatus and product/category name or ID search controls but no historical date range, price, or stock-at-date claim.

### Inventory interaction script

1. Open Inventory Overview. Display the latest tblInventoryOverview snapshot and timestamp.
2. Filter Product Stock Summary by StockStatus, ProductID, Product Name, or Category Name. Filter Product Category Overview by Category Name.
3. Keep overview cards tied to the stored overview row; label filtered table totals separately instead of silently changing snapshot totals.
4. Print or export the current snapshot, visible lookup names, active filters, and source LastUpdated values.

## Delivery Overview

### Permitted reporting tables

- tblDeliveryOverview
- tblDeliverySummary
- tblProductDeliverySummary

### Page structure

The header cards show TotalDeliveries, TotalItemsDelivered, TotalProductsDelivered, LastDeliveryDate, and LastUpdated from tblDeliveryOverview.

Delivery Summary lists DeliveryID, TotalItems, TotalProducts, and LastUpdated. Product Delivery Summary lists Product ID, Product Name, Total Quantity Delivered, Total Deliveries, Last Delivery Date, and Last Updated. Product Name is resolved by ProductID.

The tab has searches for DeliveryID, ProductID, or Product Name and sorting by approved numeric/date fields. It does not show batch, receiver, per-delivery date, previous quantity, or new quantity. It has no arbitrary delivery-date filter because tblDeliverySummary does not contain a delivery date.

### Delivery interaction script

1. Open Delivery Overview. Display the latest tblDeliveryOverview row and timestamp.
2. Search Delivery Summary by DeliveryID or sort by TotalItems, TotalProducts, or LastUpdated.
3. Search Product Delivery Summary by ProductID or Product Name; sort by TotalQuantityDelivered, TotalDeliveries, LastDeliveryDate, or LastUpdated.
4. Treat LastDeliveryDate as the latest delivery recorded for a product, not a complete historical date dimension.
5. Print or export both tables, visible product names, active searches/sorts, and source timestamps.

## Implementation approval gate

Implementation begins only after the owner approves:

- the three exact top-level tabs;
- product and transaction sections living inside Sales Overview;
- the four Sales Overview card labels;
- limited Product Name, Category Name, Customer Name, and Payment Type lookup joins for display;
- hidden TransactionID and TransactionType, with project-standard `CHK-###` Checkout ID formatting;
- Sales Summary status values `Paid`, `Ongoing`, and `Refunded` in Recent Transactions;
- no separate Period/Generated panel in Sales Overview;
- no arbitrary historical inventory or delivery date filters.
