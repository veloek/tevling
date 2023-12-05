namespace Spur.Services;

public interface IBrowserTime
{
    Task<DateTime> ConvertToLocal(DateTime dt, CancellationToken ct = default);
}
