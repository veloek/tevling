namespace Tevling;

public class StravaConfig
{
    public int? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? RedirectUri { get; set; }
    public string? VerifyToken { get; set; }
    public int? SubscriptionId { get; set; }
    public string? BaseApiUri { get; set; }
    public string? AuthorizeUri { get; set; }
    public string? TokenUri { get; set; }
    public string? DeauthorizeUri { get; set; }
    public string? SubscriptionUri { get; set; }
    public string? ResponseType { get; set; }
    public string? ApprovalPrompt { get; set; }
    public string? Scope { get; set; }
}
