using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;
using Sliki.Web.Options;

namespace Sliki.Web.Services;

public sealed class ProfStorageService : AzureBlobImageStorageService, IProfStorageService
{
    public ProfStorageService(BlobServiceClient client, IOptions<BlobStorageOptions> options)
        : base(client, Microsoft.Extensions.Options.Options.Create(new BlobStorageOptions
        {
            ConnectionString   = options.Value.ConnectionString,
            ServiceUri         = options.Value.ServiceUri,
            ContainerName      = "profsliki",
            MaxUploadBytes     = options.Value.MaxUploadBytes,
            PageSize           = options.Value.PageSize,
            ThumbnailDirectory = options.Value.ThumbnailDirectory,
            AllowedMimeTypes   = [.. options.Value.AllowedMimeTypes]
        }))
    { }
}
