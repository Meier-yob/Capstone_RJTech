# Excel export checks

Run from the repository root:

```powershell
dotnet run --project tests/ExcelExportChecks/ExcelExportChecks.csproj
```

The executable roundtrips generated workbooks and checks titles, table headers, alternating rows, borders, status fills, bounded column widths, filters, frozen headers, logo handling, literal user text, numeric peso amounts, dates, percentages, nulls, phone numbers, formatted IDs, and meaningful filenames. It also verifies that empty input gives an explanation and that a streamed 10,000-row export retains all records.

The endpoint checks require SQL Server LocalDB (`MSSQLLocalDB`). They create and remove a unique `RJTechExcelExportTest_<guid>` database and never use the application's database. They verify all five controller projections and filtered selections, sales month boundaries, status calculations, empty and invalid selections, cancellation, generation errors, and absence of database writes during export. To run only the workbook checks on a computer without LocalDB, append `-- --workbook-only` to the command.

Representative layout samples and workbooks containing the disposable database fixtures are saved in the executable's ignored `bin` output folder under `samples`. An optional path argument sets another output directory. All sample records are synthetic. No running web server or Microsoft Excel installation is required.
