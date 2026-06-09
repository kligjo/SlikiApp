using UmbracoCms.Web.Infrastructure.Configuration.Options;
using Microsoft.Extensions.Options;

namespace UmbracoCms.Web.Services.Assets;

/// <summary>
/// Decorator that falls back to an HTTP endpoint for missing assets during development.
/// </summary>
public class DevelopmentFallbackAssetsProvider : IAssetsProvider
{
    private readonly IAssetsProvider _inner;
    private readonly HttpClient _httpClient;
    private readonly DevelopmentOptions _developmentOptions;
    private readonly ILogger<DevelopmentFallbackAssetsProvider> _logger;

    public DevelopmentFallbackAssetsProvider(
        IAssetsProvider inner,
        IHttpClientFactory httpClientFactory,
        IOptions<DevelopmentOptions> developmentOptions,
        ILogger<DevelopmentFallbackAssetsProvider> logger)
    {
        _inner = inner;
        _httpClient = httpClientFactory.CreateClient("AssetsFallback");
        _httpClient.Timeout = TimeSpan.FromSeconds(3);
        _developmentOptions = developmentOptions.Value;
        _logger = logger;
    }

    public async Task<string?> GetContent(string path)
    {
        string? content = await _inner.GetContent(path);
        if (content is not (null or ""))
        {
            return content;
        }

        if (_developmentOptions.AssetsFallbackUri == null)
        {
            return content;
        }

        try
        {
            Uri fallbackUri = new(_developmentOptions.AssetsFallbackUri, path);
            var response = await _httpClient.GetAsync(fallbackUri);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return "";
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve asset from fallback URI: {Path}", path);
            return null;
        }
    }
}
