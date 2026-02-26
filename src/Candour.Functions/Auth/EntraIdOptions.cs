namespace Candour.Functions.Auth;

public class EntraIdOptions
{
    public bool UseEntraId { get; set; } = true;
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;

    public string MetadataAddress =>
        $"https://login.microsoftonline.com/{TenantId}/v2.0/.well-known/openid-configuration";
}
