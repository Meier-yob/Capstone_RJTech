namespace Capstone_RJTech.Services;

public static class SalesPeriod
{
    public static DateTime StartOfWeek(DateTime date)
        => date.Date.AddDays(-(((int)date.DayOfWeek + 6) % 7));
}
