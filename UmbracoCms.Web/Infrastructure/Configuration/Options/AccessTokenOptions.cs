namespace UmbracoCms.Web.Infrastructure.Configuration.Options;

public sealed class AccessTokenOptions
{
    public string QueryParameterName { get; set; } = "access_token";

    public string SharedToken { get; set; } = string.Empty;
}
