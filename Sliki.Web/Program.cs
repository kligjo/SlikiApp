using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Net.Http.Headers;
using Sliki.Web.Components;
using Sliki.Web.Models;
using Sliki.Web.Options;
using Sliki.Web.Services;
using Sliki.Web.Utilities;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services
    .AddOptions<BlobStorageOptions>()
    .Bind(builder.Configuration.GetSection(BlobStorageOptions.SectionName))
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.ConnectionString)
            || Uri.TryCreate(options.ServiceUri, UriKind.Absolute, out _),
        "BlobStorage:ConnectionString or BlobStorage:ServiceUri must be configured.")
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.ContainerName),
        "BlobStorage:ContainerName is required.")
    .Validate(
        options => options.MaxUploadBytes > 0,
        "BlobStorage:MaxUploadBytes must be greater than zero.")
    .Validate(
        options => options.PageSize is > 0 and <= 50,
        "BlobStorage:PageSize must be between 1 and 50.")
    .ValidateOnStart();

builder.Services.AddSingleton(sp =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<BlobStorageOptions>>().Value;
    return !string.IsNullOrWhiteSpace(options.ConnectionString)
        ? new BlobServiceClient(options.ConnectionString)
        : new BlobServiceClient(new Uri(options.ServiceUri), new DefaultAzureCredential());
});
builder.Services.AddSingleton<ImageFileValidator>();
builder.Services.AddSingleton<IImageStorageService, AzureBlobImageStorageService>();
builder.Services.AddSingleton<ThumbnailService>();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Full-resolution image proxy (used by lightbox only)
app.MapGet(
    "/images/{blobName}",
    async Task<IResult> (string blobName, IImageStorageService imageStorageService, CancellationToken cancellationToken) =>
    {
        var image = await imageStorageService.OpenReadAsync(blobName, cancellationToken);

        return image is null
            ? Results.NotFound()
            : Results.File(
                image.Content,
                image.ContentType,
                lastModified: image.LastModified,
                entityTag: EntityTagHeaderValue.Parse(image.ETag),
                enableRangeProcessing: true);
    });

// Thumbnail endpoint — serves local cached JPEG, generating on first access
app.MapGet(
    "/thumbs/{blobName}",
    async Task<IResult> (
        string blobName,
        ThumbnailService thumbnailService,
        IImageStorageService imageStorageService,
        CancellationToken cancellationToken) =>
    {
        // Basic path traversal guard
        if (string.IsNullOrWhiteSpace(blobName) || blobName != Path.GetFileName(blobName))
            return Results.BadRequest();

        if (!thumbnailService.Exists(blobName))
        {
            var download = await imageStorageService.OpenReadAsync(blobName, cancellationToken);
            if (download is null) return Results.NotFound();

            // Videos have no server-side thumbnail — JS canvas handles those
            if (download.ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
            {
                await download.Content.DisposeAsync();
                return Results.NotFound();
            }

            await using var ms = new MemoryStream();
            try
            {

                await download.Content.CopyToAsync(ms, cancellationToken);
                await download.Content.DisposeAsync();
            }
            catch { }

            var ok = await thumbnailService.GenerateAsync(ms, blobName, cancellationToken);
            if (!ok)
            {
                // Generation failed (e.g. unsupported format) — stream back the original
                ms.Position = 0;
                return Results.File(ms, download.ContentType);
            }
        }

        var thumbPath = thumbnailService.ThumbPath(blobName);
        return Results.File(
            thumbPath,
            "image/jpeg",
            lastModified: File.GetLastWriteTimeUtc(thumbPath),
            entityTag: new EntityTagHeaderValue($"\"{blobName.GetHashCode():x8}\""));
    });

app.MapPost(
        "/api/images/upload",
        async Task<IResult> (
            HttpRequest request,
            IImageStorageService imageStorageService,
            ImageFileValidator imageFileValidator,
            ThumbnailService thumbnailService,
            CancellationToken cancellationToken) =>
        {
            var form = await request.ReadFormAsync(cancellationToken);
            var file = form.Files["file"] ?? form.Files.FirstOrDefault();

            if (file is null)
                return Results.BadRequest(new { error = "Select an image file to upload." });

            if (file.Length <= 0)
                return Results.BadRequest(new { error = "The selected file is empty." });

            if (file.Length > imageFileValidator.MaxUploadBytes)
                return Results.BadRequest(new
                {
                    error = $"The file exceeds the limit of {FileSizeFormatter.Format(imageFileValidator.MaxUploadBytes)}."
                });

            await using var uploadedFileStream = file.OpenReadStream();
            await using var memoryStream = new MemoryStream(capacity: file.Length > int.MaxValue ? int.MaxValue : (int)file.Length);
            await uploadedFileStream.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;

            byte[] headerBytes;
            if (memoryStream.TryGetBuffer(out var buffer))
                headerBytes = buffer.AsSpan(0, (int)Math.Min(memoryStream.Length, 32)).ToArray();
            else
            {
                var bytes = memoryStream.ToArray();
                headerBytes = bytes.AsSpan(0, Math.Min(bytes.Length, 32)).ToArray();
            }

            var validation = imageFileValidator.Validate(file.FileName, file.ContentType, file.Length, headerBytes);
            if (!validation.IsValid || string.IsNullOrWhiteSpace(validation.NormalizedContentType))
                return Results.BadRequest(new { error = validation.ErrorMessage ?? "The image failed validation." });

            memoryStream.Position = 0;
            var result = await imageStorageService.UploadAsync(
                new UploadImageRequest(
                    file.FileName,
                    validation.NormalizedContentType,
                    file.Length,
                    memoryStream),
                progress: null,
                cancellationToken);

            // Generate thumbnail immediately while we still have the bytes in memory
            if (!validation.NormalizedContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
            {
                await thumbnailService.GenerateAsync(memoryStream, result.BlobName, cancellationToken);
            }

            return Results.Ok(result);
        })
    .DisableAntiforgery()
    .AddEndpointFilter(async (ctx, next) =>
    {
        var sizeFeature = ctx.HttpContext.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature>();
        if (sizeFeature is { IsReadOnly: false })
            sizeFeature.MaxRequestBodySize = null;
        return await next(ctx);
    });

app.Run();
