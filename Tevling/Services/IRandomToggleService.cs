namespace Tevling.Services;

public interface IRandomToggleService
{
    public void SetRandomEnabled(bool changed);
    public bool IsEnabled();
}
