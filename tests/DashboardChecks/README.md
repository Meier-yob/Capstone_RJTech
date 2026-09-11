Run `dotnet run --project tests/DashboardChecks` on Windows with SQL Server LocalDB installed.

These integration checks create and remove a uniquely named `RJTechDashboardTest_*` database. They never connect to the application's database. They verify actual SQL Server query translation, paid-sale and outstanding-balance calculations, inventory rules, dates, filters, historical prices, empty states, and read-only behavior.
