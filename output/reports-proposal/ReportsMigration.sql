BEGIN TRANSACTION;
GO

CREATE TABLE [tblBestSellingProduct] (
    [BestSellingID] int NOT NULL IDENTITY,
    [ProductID] int NOT NULL,
    [TotalQuantitySold] int NOT NULL,
    [TotalSalesAmount] decimal(18,2) NOT NULL,
    [Period] nvarchar(20) NOT NULL,
    [DateGenerated] datetime2 NOT NULL,
    CONSTRAINT [PK_tblBestSellingProduct] PRIMARY KEY ([BestSellingID]),
    CONSTRAINT [FK_tblBestSellingProduct_Products_ProductID] FOREIGN KEY ([ProductID]) REFERENCES [Products] ([product_ID]) ON DELETE CASCADE
);
GO

CREATE TABLE [tblDeliveryOverview] (
    [LastUpdated] datetime2 NOT NULL,
    [TotalDeliveries] int NOT NULL,
    [TotalItemsDelivered] int NOT NULL,
    [TotalProductsDelivered] int NOT NULL,
    [LastDeliveryDate] datetime2 NULL,
    CONSTRAINT [PK_tblDeliveryOverview] PRIMARY KEY ([LastUpdated])
);
GO

CREATE TABLE [tblDeliverySummary] (
    [DeliveryID] int NOT NULL,
    [TotalItems] int NOT NULL,
    [TotalProducts] int NOT NULL,
    [LastUpdated] datetime2 NOT NULL,
    CONSTRAINT [PK_tblDeliverySummary] PRIMARY KEY ([DeliveryID]),
    CONSTRAINT [FK_tblDeliverySummary_Deliveries_DeliveryID] FOREIGN KEY ([DeliveryID]) REFERENCES [Deliveries] ([delivery_ID]) ON DELETE CASCADE
);
GO

CREATE TABLE [tblInventoryOverview] (
    [LastUpdated] datetime2 NOT NULL,
    [TotalProducts] int NOT NULL,
    [TotalQuantity] int NOT NULL,
    [AvailableProducts] int NOT NULL,
    [UnavailableProducts] int NOT NULL,
    [LowStockProducts] int NOT NULL,
    [OutOfStockProducts] int NOT NULL,
    CONSTRAINT [PK_tblInventoryOverview] PRIMARY KEY ([LastUpdated])
);
GO

CREATE TABLE [tblLeastSellingProduct] (
    [LeastSellingID] int NOT NULL IDENTITY,
    [ProductID] int NOT NULL,
    [TotalQuantitySold] int NOT NULL,
    [TotalSalesAmount] decimal(18,2) NOT NULL,
    [Period] nvarchar(20) NOT NULL,
    [DateGenerated] datetime2 NOT NULL,
    CONSTRAINT [PK_tblLeastSellingProduct] PRIMARY KEY ([LeastSellingID]),
    CONSTRAINT [FK_tblLeastSellingProduct_Products_ProductID] FOREIGN KEY ([ProductID]) REFERENCES [Products] ([product_ID]) ON DELETE CASCADE
);
GO

CREATE TABLE [tblLeastStockedProduct] (
    [ProductID] int NOT NULL,
    [CurrentQuantity] int NOT NULL,
    [LastUpdated] datetime2 NOT NULL,
    CONSTRAINT [PK_tblLeastStockedProduct] PRIMARY KEY ([ProductID]),
    CONSTRAINT [FK_tblLeastStockedProduct_Products_ProductID] FOREIGN KEY ([ProductID]) REFERENCES [Products] ([product_ID]) ON DELETE CASCADE
);
GO

CREATE TABLE [tblMonthlySales] (
    [MonthlySalesID] int NOT NULL IDENTITY,
    [Month] int NOT NULL,
    [Year] int NOT NULL,
    [TotalSales] decimal(18,2) NOT NULL,
    [TotalTransactions] int NOT NULL,
    [TotalItemsSold] int NOT NULL,
    CONSTRAINT [PK_tblMonthlySales] PRIMARY KEY ([MonthlySalesID])
);
GO

CREATE TABLE [tblMostStockedProduct] (
    [ProductID] int NOT NULL,
    [CurrentQuantity] int NOT NULL,
    [LastUpdated] datetime2 NOT NULL,
    CONSTRAINT [PK_tblMostStockedProduct] PRIMARY KEY ([ProductID]),
    CONSTRAINT [FK_tblMostStockedProduct_Products_ProductID] FOREIGN KEY ([ProductID]) REFERENCES [Products] ([product_ID]) ON DELETE CASCADE
);
GO

CREATE TABLE [tblProductCategoryOverview] (
    [CategoryID] int NOT NULL,
    [TotalProducts] int NOT NULL,
    [TotalQuantity] int NOT NULL,
    [AvailableQuantity] int NOT NULL,
    [UnavailableQuantity] int NOT NULL,
    [LastUpdated] datetime2 NOT NULL,
    CONSTRAINT [PK_tblProductCategoryOverview] PRIMARY KEY ([CategoryID]),
    CONSTRAINT [FK_tblProductCategoryOverview_ProductCategories_CategoryID] FOREIGN KEY ([CategoryID]) REFERENCES [ProductCategories] ([category_ID]) ON DELETE CASCADE
);
GO

CREATE TABLE [tblProductDeliverySummary] (
    [ProductID] int NOT NULL,
    [TotalQuantityDelivered] int NOT NULL,
    [TotalDeliveries] int NOT NULL,
    [LastDeliveryDate] datetime2 NULL,
    [LastUpdated] datetime2 NOT NULL,
    CONSTRAINT [PK_tblProductDeliverySummary] PRIMARY KEY ([ProductID]),
    CONSTRAINT [FK_tblProductDeliverySummary_Products_ProductID] FOREIGN KEY ([ProductID]) REFERENCES [Products] ([product_ID]) ON DELETE CASCADE
);
GO

CREATE TABLE [tblProductStockSummary] (
    [ProductID] int NOT NULL,
    [CurrentQuantity] int NOT NULL,
    [ReorderLevel] int NOT NULL,
    [StockStatus] nvarchar(30) NOT NULL,
    [LastUpdated] datetime2 NOT NULL,
    CONSTRAINT [PK_tblProductStockSummary] PRIMARY KEY ([ProductID]),
    CONSTRAINT [FK_tblProductStockSummary_Products_ProductID] FOREIGN KEY ([ProductID]) REFERENCES [Products] ([product_ID]) ON DELETE CASCADE
);
GO

CREATE TABLE [tblSalesByCategory] (
    [SalesCategoryID] int NOT NULL IDENTITY,
    [CategoryID] int NOT NULL,
    [TotalQuantitySold] int NOT NULL,
    [TotalSalesAmount] decimal(18,2) NOT NULL,
    [Period] nvarchar(20) NOT NULL,
    [DateGenerated] datetime2 NOT NULL,
    CONSTRAINT [PK_tblSalesByCategory] PRIMARY KEY ([SalesCategoryID]),
    CONSTRAINT [FK_tblSalesByCategory_ProductCategories_CategoryID] FOREIGN KEY ([CategoryID]) REFERENCES [ProductCategories] ([category_ID]) ON DELETE CASCADE
);
GO

CREATE TABLE [tblSalesOverview] (
    [LastUpdated] datetime2 NOT NULL,
    [TotalSales] decimal(18,2) NOT NULL,
    [TotalTransactions] int NOT NULL,
    [TotalItemsSold] int NOT NULL,
    [TotalCustomers] int NOT NULL,
    [TodaySales] decimal(18,2) NOT NULL,
    [WeeklySales] decimal(18,2) NOT NULL,
    [MonthlySales] decimal(18,2) NOT NULL,
    [YearlySales] decimal(18,2) NOT NULL,
    [LastSaleDate] datetime2 NULL,
    CONSTRAINT [PK_tblSalesOverview] PRIMARY KEY ([LastUpdated])
);
GO

CREATE TABLE [tblTransaction] (
    [TransactionID] int NOT NULL IDENTITY,
    [CheckoutID] int NOT NULL,
    [CustomerID] int NOT NULL,
    [TransactionType] nvarchar(30) NOT NULL,
    [PaymentMethod] nvarchar(50) NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [TransactionDate] datetime2 NOT NULL,
    [Status] nvarchar(30) NOT NULL,
    CONSTRAINT [PK_tblTransaction] PRIMARY KEY ([TransactionID]),
    CONSTRAINT [FK_tblTransaction_tblCheckout_CheckoutID] FOREIGN KEY ([CheckoutID]) REFERENCES [tblCheckout] ([CheckoutID]) ON DELETE CASCADE,
    CONSTRAINT [FK_tblTransaction_tblCustomer_CustomerID] FOREIGN KEY ([CustomerID]) REFERENCES [tblCustomer] ([customer_ID]) ON DELETE NO ACTION
);
GO

CREATE TABLE [tblWeeklySales] (
    [WeeklySalesID] int NOT NULL IDENTITY,
    [WeekStartDate] datetime2 NOT NULL,
    [WeekEndDate] datetime2 NOT NULL,
    [TotalSales] decimal(18,2) NOT NULL,
    [TotalTransactions] int NOT NULL,
    [TotalItemsSold] int NOT NULL,
    CONSTRAINT [PK_tblWeeklySales] PRIMARY KEY ([WeeklySalesID])
);
GO

CREATE TABLE [tblYearlySales] (
    [YearlySalesID] int NOT NULL IDENTITY,
    [Year] int NOT NULL,
    [TotalSales] decimal(18,2) NOT NULL,
    [TotalTransactions] int NOT NULL,
    [TotalItemsSold] int NOT NULL,
    CONSTRAINT [PK_tblYearlySales] PRIMARY KEY ([YearlySalesID])
);
GO

CREATE UNIQUE INDEX [IX_tblBestSellingProduct_Period] ON [tblBestSellingProduct] ([Period]);
GO

CREATE INDEX [IX_tblBestSellingProduct_ProductID] ON [tblBestSellingProduct] ([ProductID]);
GO

CREATE UNIQUE INDEX [IX_tblLeastSellingProduct_Period] ON [tblLeastSellingProduct] ([Period]);
GO

CREATE INDEX [IX_tblLeastSellingProduct_ProductID] ON [tblLeastSellingProduct] ([ProductID]);
GO

CREATE UNIQUE INDEX [IX_tblMonthlySales_Year_Month] ON [tblMonthlySales] ([Year], [Month]);
GO

CREATE INDEX [IX_tblSalesByCategory_CategoryID] ON [tblSalesByCategory] ([CategoryID]);
GO

CREATE UNIQUE INDEX [IX_tblSalesByCategory_Period_CategoryID] ON [tblSalesByCategory] ([Period], [CategoryID]);
GO

CREATE UNIQUE INDEX [IX_tblTransaction_CheckoutID] ON [tblTransaction] ([CheckoutID]);
GO

CREATE INDEX [IX_tblTransaction_CustomerID] ON [tblTransaction] ([CustomerID]);
GO

CREATE UNIQUE INDEX [IX_tblWeeklySales_WeekStartDate] ON [tblWeeklySales] ([WeekStartDate]);
GO

CREATE UNIQUE INDEX [IX_tblYearlySales_Year] ON [tblYearlySales] ([Year]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260907174749_AddReportingTables', N'8.0.30');
GO

COMMIT;
GO

