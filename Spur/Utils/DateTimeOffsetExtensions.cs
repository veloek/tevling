namespace Spur.Utils;

public static class DateTimeOffsetExt
{
    public static bool IsSameDateUtc(this DateTimeOffset dto)
    {
        return dto.Date == DateTimeOffset.UtcNow.Date;
    }

    public static bool IsYesterdayUtc(this DateTimeOffset dto)
    {
        return dto.Date == DateTimeOffset.UtcNow.Date.AddDays(-1);
    }

    public static bool IsSameWeekStartingOnMondayUtc(this DateTimeOffset dto)
    {
        return (DateTimeOffset.UtcNow - dto) <= TimeSpan.FromDays(7)
            && (dto.DayOfWeek == DayOfWeek.Sunday
                ? DateTimeOffset.UtcNow.DayOfWeek == DayOfWeek.Sunday
                : dto.DayOfWeek <= DateTimeOffset.UtcNow.DayOfWeek);
    }

    public static bool IsSameMonthUtc(this DateTimeOffset dto)
    {
        return dto.Year == DateTimeOffset.UtcNow.Year
            && dto.Month == DateTimeOffset.UtcNow.Month;
    }

    public static bool IsLastMonthUtc(this DateTimeOffset dto)
    {
        return dto.Year == DateTimeOffset.UtcNow.AddMonths(-1).Year
            && dto.Month == DateTimeOffset.UtcNow.AddMonths(-1).Month;
    }

    public static bool IsSameYearUtc(this DateTimeOffset dto)
    {
        return dto.Year == DateTimeOffset.UtcNow.Year;
    }
}
