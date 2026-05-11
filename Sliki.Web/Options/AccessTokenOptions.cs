namespace Sliki.Web.Options;

public sealed class AccessTokenOptions
{
    public const string SectionName = "AccessToken";

    public string QueryParameterName { get; set; } = "access_token";

    public string SharedToken { get; set; } = string.Empty;
}
