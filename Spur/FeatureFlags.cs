namespace Spur;

/// <summary>
/// A collection of feature flags for the app.
/// </summary>

public static class FeatureFlags
{
    // NB! These names should match those in appsettings.json.
    public const string DevController = "DevController";
    public const string Deauthorize = "Deauthorize";
}
