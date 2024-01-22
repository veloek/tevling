namespace Tevling.Services;

public interface IBrowserTime
{
    Task<DateTimeOffset> ConvertToLocal(DateTimeOffset dt, CancellationToken ct = default);
}
