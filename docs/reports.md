# Reports

The Reports page is available at `/Reports` and contains three tabs: Sales Overview, Inventory Overview, and Delivery Overview.

## Data flow

`ReportRefreshService` generates stored summaries from the existing checkout, product, category, and delivery records. The Reports controller then reads the reporting tables for all totals, quantities, dates, and statuses. Product, category, customer, and payment-type labels are resolved through their existing foreign-key relationships for display.

The first Reports request generates summaries when the reporting tables are empty. The **Generate Report** button refreshes all reporting tables inside one database transaction, so users do not see a partially refreshed report.

## Sales Overview

- Cards: Total Sales, Total Checkouts, Total Sold Items, and Total Customers.
- Weekly, monthly, and yearly stored sales history.
- Best and least selling products with product names.
- Sales by category name.
- Recent Transactions displays Checkout ID, Customer Name, Payment Type, Payment Method, Amount, Transaction Date, and Status.
- TransactionID and TransactionType stay in `tblTransaction` but are not displayed.
- Checkout IDs use the existing `CHK-{CheckoutID:D3}` format.
- Recent status values are Paid, Ongoing, and Refunded.

## Inventory Overview

- Snapshot cards use `tblInventoryOverview`.
- Product Stock Summary includes Product Name and Category Name.
- Product Category Overview and its chart display category names rather than category codes.
- Most and Least Stocked Product panels include product names.

## Delivery Overview

- Delivery cards use `tblDeliveryOverview`.
- Delivery Summary uses formatted delivery IDs.
- Product Delivery Summary includes Product Name.

## Output and accessibility

All tabs support print and Excel export. Tables remain available when charts cannot load, table columns are keyboard-sortable, empty states distinguish missing data from zero totals, and all report styling uses the existing theme variables for light and dark mode.

## Database migration

Migration `20260907174749_AddReportingTables` creates the 16 reporting tables. A standalone SQL script for this migration is available at `output/reports-proposal/ReportsMigration.sql`.
