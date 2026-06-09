namespace UmbracoCms.Web.Services.Assets;

public interface IAssetsProvider
{
    Task<string?> GetContent(string path);
}
