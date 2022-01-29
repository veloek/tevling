namespace Spur;

public class StravaConfig
{
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? BaseUrl { get; set; }
    public string? VerifyToken { get; set; }
    public string? SubscriptionId { get; set; }
    public string? AuthorizeUrl { get; set; }
    public string? TokenUri { get; set; }
    public string? DeauthorizeUri { get; set; }
    public string? SubscriptionUrl { get; set; }
    public string? ResponseType { get; set; }
    public string? ApprovalPrompt { get; set; }
    public string? Scope {get;set;}
    public string? GrantType { get; set; }
}
