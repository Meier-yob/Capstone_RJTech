Run `dotnet run --project tests/SalesHistoryChecks` from the repository root.

The checks require SQL Server LocalDB and create a uniquely named disposable database. They test the migration and one-time import, full checkout and instalment receipts, validation, transactional rollback on receipt failure, final settlement, refund/deletion protection, and the date-only export with linked Payment Type. The configured application database is never used.

`-- --keep-preview` retains a successful test database and prints its exact name for browser verification; otherwise the database is removed in `finally`.

If the default `MSSQLLocalDB` instance is unavailable, pass another disposable-capable LocalDB instance with `-- --instance InstanceName`.
