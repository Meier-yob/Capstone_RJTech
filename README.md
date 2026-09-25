# Capstone_RJTech

ASP.NET Core MVC inventory, sales, installment, and reporting application.

## Requirements

- .NET 8 SDK
- SQL Server LocalDB (the default configuration expects the `MSSQLLocalDB` instance on Windows)
- An SMTP account if password-reset email should be available

## Clone and run

```powershell
git clone https://github.com/Meier-yob/Capstone_RJTech.git
cd Capstone_RJTech
dotnet restore .\Capstone_RJTech\Capstone_RJTech.csproj
dotnet run --project .\Capstone_RJTech\Capstone_RJTech.csproj
```

The application applies pending EF Core migrations during startup. An empty development database receives the schema, default categories/products, and an initial administrator account. Existing business data is not stored in Git and is not copied by `git clone`.

The development seed account is currently `admin` / `Admin123!`. It is a development-only fallback. Change the password immediately after the first login. For a fresh non-development database, set `SeedAdmin:Password` before the first startup; a blank or missing value intentionally stops startup instead of creating a public default account.

For local production-like testing, configure the seed password with user secrets:

```powershell
dotnet user-secrets set --project .\Capstone_RJTech\Capstone_RJTech.csproj "SeedAdmin:Password" "<strong-password>"
```

In a hosted environment, provide the same setting as `SeedAdmin__Password` (and provide `SeedAdmin__Username`, `SeedAdmin__Email`, or `SeedAdmin__FullName` if the defaults need to change).

## Database configuration

The default connection string is in `Capstone_RJTech/appsettings.json` and uses SQL Server LocalDB. For a different database, keep credentials out of source control and use user secrets or environment variables:

```powershell
dotnet user-secrets set --project .\Capstone_RJTech\Capstone_RJTech.csproj "ConnectionStrings:RJTechDatabase" "Server=<server>;Database=<database>;Trusted_Connection=True;TrustServerCertificate=True"
```

If the application is hosted somewhere other than the local Windows development machine, use the connection string appropriate for that host and configure TLS according to the database server.

## Email configuration

The SMTP password is intentionally not stored in `appsettings.json`. Configure it locally when password-reset email is needed:

```powershell
dotnet user-secrets set --project .\Capstone_RJTech\Capstone_RJTech.csproj "Email:Password" "<smtp-password>"
```

The host, port, username, sender, and SSL settings can likewise be overridden with `Email:Host`, `Email:Port`, `Email:Username`, `Email:FromAddress`, and `Email:EnableSsl`. Set `Application:PublicBaseUrl` to the deployed HTTPS URL so password-reset links use the correct host.

## Checks

Run the checks from the repository root:

```powershell
dotnet build .\Capstone_RJTech\Capstone_RJTech.csproj --no-restore
dotnet run --project .\tests\SalesHistoryChecks\SalesHistoryChecks.csproj
dotnet run --project .\tests\DashboardChecks\DashboardChecks.csproj
dotnet run --project .\tests\ExcelExportChecks\ExcelExportChecks.csproj -- --workbook-only
```

The SQL Server checks create uniquely named disposable databases and remove them when they finish. They do not use the configured application database. The Sales History checks also exercise the historical migration chain, payment recording, validation, rollback, and export behavior.

## Project layout

- `Capstone_RJTech/` — ASP.NET Core application
- `Capstone_RJTech/Views/` — MVC views and legacy view assets
- `tests/SalesHistoryChecks/` — sales, installment, migration, and history checks
- `tests/DashboardChecks/` — dashboard SQL integration checks
- `tests/ExcelExportChecks/` — workbook and export checks

Do not delete the `tests` folder when preparing a clone; it contains executable checks that help catch migration and regression problems.
