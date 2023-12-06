namespace Spur.Services;

public interface IBrowserTime
{
    Task<DateTimeOffset> ConvertToLocal(DateTimeOffset dt, CancellationToken ct = default);
}
