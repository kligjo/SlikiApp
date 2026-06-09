using UmbracoCms.Web.Infrastructure.DependencyInjection;

namespace UmbracoCms.Web.Services.Assets;

// Registered manually in Startup.cs (with decoration chain)
public class FileSystemAssetsProvider : IAssetsProvider
{
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly ILogger<FileSystemAssetsProvider> _logger;

    public FileSystemAssetsProvider(IWebHostEnvironment webHostEnvironment, ILogger<FileSystemAssetsProvider> logger)
    {
        _webHostEnvironment = webHostEnvironment;
        _logger = logger;
    }

    public async Task<string?> GetContent(string path)
    {
        try
        {
            var fileInfo = _webHostEnvironment.WebRootFileProvider.GetFileInfo(path);
            if (!fileInfo.Exists)
            {
                return "";
            }

            await using var stream = fileInfo.CreateReadStream();
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading asset file: {Path}", path);
            return null;
        }
    }
}
