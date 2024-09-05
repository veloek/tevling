namespace Tevling.Utils;

public static class DateTimeOffsetExt
{
    public static bool IsSameDateUtc(this DateTimeOffset dto)
    {
        return dto.UtcDateTime.Date == DateTimeOffset.UtcNow.Date;
    }

    public static bool IsYesterdayUtc(this DateTimeOffset dto)
    {
        return dto.UtcDateTime.Date == DateTimeOffset.UtcNow.Date.AddDays(-1);
    }

    public static bool IsSameWeekStartingOnMondayUtc(this DateTimeOffset dto)
    {
        TimeSpan timeSinceMonday = DateTimeOffset.UtcNow.DayOfWeek == DayOfWeek.Sunday
            ? TimeSpan.FromDays(6)
            : TimeSpan.FromDays((int)DateTimeOffset.UtcNow.DayOfWeek - 1);

        return DateTimeOffset.UtcNow.Date - dto.UtcDateTime.Date <= timeSinceMonday;
    }

    public static bool IsLastWeekStartingOnMondayUtc(this DateTimeOffset dto)
    {
        TimeSpan timeSinceLastMonday = DateTimeOffset.UtcNow.DayOfWeek == DayOfWeek.Sunday
            ? TimeSpan.FromDays(6 + 7)
            : TimeSpan.FromDays((int)DateTimeOffset.UtcNow.DayOfWeek - 1 + 7);

        return DateTimeOffset.UtcNow.Date - dto.UtcDateTime.Date <= timeSinceLastMonday;
    }

    public static bool IsSameMonthUtc(this DateTimeOffset dto)
    {
        return dto.UtcDateTime.Year == DateTimeOffset.UtcNow.Year &&
            dto.UtcDateTime.Month == DateTimeOffset.UtcNow.Month;
    }

    public static bool IsLastMonthUtc(this DateTimeOffset dto)
    {
        return dto.UtcDateTime.Year == DateTimeOffset.UtcNow.AddMonths(-1).Year &&
            dto.UtcDateTime.Month == DateTimeOffset.UtcNow.AddMonths(-1).Month;
    }

    public static bool IsSameYearUtc(this DateTimeOffset dto)
    {
        return dto.UtcDateTime.Year == DateTimeOffset.UtcNow.Year;
    }
}
