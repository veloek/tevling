namespace Tevling.Services;

public class RandomToggleService : IRandomToggleService
{
    private bool _isEnabled = false;

    public bool IsEnabled()
    {
        return _isEnabled;
    }

    public void SetRandomEnabled(bool changed)
    {
        _isEnabled = changed;
    }
}
