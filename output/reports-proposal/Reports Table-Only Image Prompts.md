# Reports table-only image prompt set

Mode: built-in image generation. The existing Reports Page Concept PNG was used as a strict visual reference for the RJTech shell, layout, colors, typography, cards, charts, and tables.

Shared constraints for every screen: 1536 by 1024 desktop mockup; five tabs only (Sales Overview, Product Sales, Transactions, Inventory, Deliveries); use only the approved reporting-table fields or aggregates derived directly from them; display ProductID, CategoryID, CustomerID, CheckoutID, and DeliveryID when names are unavailable; no checkboxes, trend table columns, status dots, Calendar entry, Refund Rate, installment information, or unsupported detail fields.

## Sales Overview

Create a Sales Overview tab based only on tblSalesOverview, tblWeeklySales, tblMonthlySales, and tblYearlySales. Show total sales, transactions, items sold, customers, today/weekly/monthly/yearly sales, last sale date, and last updated. Provide Weekly, Monthly, and Yearly switching. The selected period controls both the chart and a history table using that period table's exact fields.

## Product Sales

Create a Product Sales tab based only on tblBestSellingProduct, tblLeastSellingProduct, and tblSalesByCategory. Filter by stored Period and DateGenerated, with ID search. Show best-product KPIs, sales by CategoryID, a best/least comparison, and separate best/least tables using ProductID, TotalQuantitySold, TotalSalesAmount, Period, and DateGenerated. Do not invent product or category names.

## Transactions

Create a Transactions tab based only on tblTransaction. Filter by TransactionDate, TransactionType, PaymentMethod, and Status. Show filtered amount/count/status cards, transaction amount by day, amount by payment method, and a table containing TransactionID, CheckoutID, CustomerID, TransactionType, PaymentMethod, Amount, TransactionDate, and Status. Do not invent customer names.

## Inventory

Create an Inventory tab based only on tblInventoryOverview, tblProductStockSummary, tblProductCategoryOverview, tblMostStockedProduct, and tblLeastStockedProduct. Label it as a current stored snapshot. Show overview totals, status distribution, quantity by CategoryID, ProductStockSummary's exact fields, and most/least stocked ProductID callouts. Do not add names, prices, or historical date filters.

## Deliveries

Create a Deliveries tab based only on tblDeliveryOverview, tblDeliverySummary, and tblProductDeliverySummary. Show overview totals, LastDeliveryDate, items by DeliveryID, quantities by ProductID, and the two summary tables using their exact fields. Do not show a date range, product/delivery names, receiver, batch, or previous/new quantities.
