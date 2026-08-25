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
builder.Services.AddSingleton<ISlikarStorageService, SlikarStorageService>();
builder.Services.AddSingleton<IProfStorageService, ProfStorageService>();
builder.Services.AddSingleton<ThumbnailService>();
builder.Services.AddSingleton<ThumbnailQueue>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<ThumbnailQueue>());
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
if (builder.Configuration["DisableHttpsRedirect"] != "true")
{
    app.UseHttpsRedirection();
}
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

            // Videos and ZIPs have no server-side thumbnail
            if (download.ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase)
                || download.ContentType == "application/zip")
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

            var ok = await thumbnailService.GenerateAsync(ms, blobName, "", cancellationToken);
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

// ── Slikar container endpoints ────────────────────────────────────────────────

app.MapGet(
    "/slikar/images/{blobName}",
    async Task<IResult> (string blobName, ISlikarStorageService storage, CancellationToken ct) =>
    {
        var image = await storage.OpenReadAsync(blobName, ct);
        return image is null
            ? Results.NotFound()
            : Results.File(image.Content, image.ContentType,
                lastModified: image.LastModified,
                entityTag: EntityTagHeaderValue.Parse(image.ETag),
                enableRangeProcessing: true);
    });

app.MapGet(
    "/slikar/thumbs/{blobName}",
    async Task<IResult> (
        string blobName,
        ThumbnailService thumbnailService,
        ISlikarStorageService storage,
        CancellationToken ct) =>
    {
        if (string.IsNullOrWhiteSpace(blobName) || blobName != Path.GetFileName(blobName))
            return Results.BadRequest();

        if (!thumbnailService.Exists(blobName, "slikar"))
        {
            var download = await storage.OpenReadAsync(blobName, ct);
            if (download is null) return Results.NotFound();

            if (download.ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase)
                || download.ContentType == "application/zip")
            {
                await download.Content.DisposeAsync();
                return Results.NotFound();
            }

            await using var ms = new MemoryStream();
            await download.Content.CopyToAsync(ms, ct);
            await download.Content.DisposeAsync();

            var ok = await thumbnailService.GenerateAsync(ms, blobName, "slikar", ct);
            if (!ok)
            {
                ms.Position = 0;
                return Results.File(ms, "image/jpeg");
            }
        }

        var thumbPath = thumbnailService.ThumbPath(blobName, "slikar");
        return Results.File(thumbPath, "image/jpeg",
            lastModified: File.GetLastWriteTimeUtc(thumbPath),
            entityTag: new EntityTagHeaderValue($"\"{("slikar/" + blobName).GetHashCode():x8}\""));
    });

// Issues a short-lived SAS URL so the client can PUT directly to blob storage.
// The file never passes through the app server — no timeout, no memory pressure.
app.MapPost(
        "/slikar/api/sas",
        async Task<IResult> (
            HttpRequest request,
            ISlikarStorageService storage,
            ImageFileValidator imageFileValidator,
            CancellationToken ct) =>
        {
            var form = await request.ReadFormAsync(ct);
            var fileName = form["fileName"].ToString();
            var contentType = form["contentType"].ToString();
            if (!long.TryParse(form["size"], out var size))
                return Results.BadRequest(new { error = "size is required." });

            // Read the header bytes sent by the client for magic-byte validation
            var headerBase64 = form["headerBase64"].ToString();
            if (string.IsNullOrWhiteSpace(headerBase64))
                return Results.BadRequest(new { error = "headerBase64 is required." });
            byte[] header;
            try { header = Convert.FromBase64String(headerBase64); }
            catch { return Results.BadRequest(new { error = "Invalid headerBase64." }); }

            if (size > imageFileValidator.MaxUploadBytes)
                return Results.BadRequest(new { error = $"Exceeds the {FileSizeFormatter.Format(imageFileValidator.MaxUploadBytes)} limit." });

            var validation = imageFileValidator.Validate(fileName, contentType, size, header);
            if (!validation.IsValid || string.IsNullOrWhiteSpace(validation.NormalizedContentType))
                return Results.BadRequest(new { error = validation.ErrorMessage ?? "Validation failed." });

            var ticket = await storage.GenerateSasUploadUrlAsync(fileName, validation.NormalizedContentType, ct);
            return Results.Ok(ticket);
        })
    .DisableAntiforgery();

// Called by the client after a successful direct-to-blob PUT, so the server can
// set blob properties and enqueue thumbnail generation.
app.MapPost(
        "/slikar/api/complete",
        async Task<IResult> (
            HttpRequest request,
            ISlikarStorageService storage,
            ThumbnailQueue thumbnailQueue,
            CancellationToken ct) =>
        {
            var form = await request.ReadFormAsync(ct);
            var blobName = form["blobName"].ToString();
            var fileName = form["fileName"].ToString();
            var contentType = form["contentType"].ToString();

            if (string.IsNullOrWhiteSpace(blobName) || blobName != Path.GetFileName(blobName))
                return Results.BadRequest(new { error = "Invalid blobName." });

            // Set the original filename metadata so the gallery can display it
            if (!string.IsNullOrWhiteSpace(fileName))
                await storage.SetBlobOriginalFileNameAsync(blobName, fileName, ct);

            // Enqueue thumbnail for images (not video/zip)
            var isImage = !contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase)
                       && contentType != "application/zip";
            if (isImage)
            {
                var download = await storage.OpenReadAsync(blobName, ct);
                if (download is not null)
                {
                    await using var ms = new MemoryStream();
                    await download.Content.CopyToAsync(ms, ct);
                    await download.Content.DisposeAsync();
                    if (ms.TryGetBuffer(out var buf))
                        await thumbnailQueue.EnqueueAsync(blobName, "slikar", buf.ToArray(), ct);
                }
            }

            return Results.Ok();
        })
    .DisableAntiforgery();

// ── End Slikar ────────────────────────────────────────────────────────────────

// ── Profesionalni Sliki endpoints ─────────────────────────────────────────────

app.MapGet(
    "/prof/images/{blobName}",
    async Task<IResult> (string blobName, IProfStorageService storage, CancellationToken ct) =>
    {
        var image = await storage.OpenReadAsync(blobName, ct);
        return image is null
            ? Results.NotFound()
            : Results.File(image.Content, image.ContentType,
                lastModified: image.LastModified,
                entityTag: EntityTagHeaderValue.Parse(image.ETag),
                enableRangeProcessing: true);
    });

app.MapGet(
    "/prof/thumbs/{blobName}",
    async Task<IResult> (
        string blobName,
        ThumbnailService thumbnailService,
        IProfStorageService storage,
        CancellationToken ct) =>
    {
        if (string.IsNullOrWhiteSpace(blobName) || blobName != Path.GetFileName(blobName))
            return Results.BadRequest();

        if (!thumbnailService.Exists(blobName, "prof"))
        {
            var download = await storage.OpenReadAsync(blobName, ct);
            if (download is null) return Results.NotFound();

            if (download.ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase)
                || download.ContentType == "application/zip")
            {
                await download.Content.DisposeAsync();
                return Results.NotFound();
            }

            await using var ms = new MemoryStream();
            await download.Content.CopyToAsync(ms, ct);
            await download.Content.DisposeAsync();

            var ok = await thumbnailService.GenerateAsync(ms, blobName, "prof", ct);
            if (!ok)
            {
                ms.Position = 0;
                return Results.File(ms, "image/jpeg");
            }
        }

        var thumbPath = thumbnailService.ThumbPath(blobName, "prof");
        return Results.File(thumbPath, "image/jpeg",
            lastModified: File.GetLastWriteTimeUtc(thumbPath),
            entityTag: new EntityTagHeaderValue($"\"{("prof/" + blobName).GetHashCode():x8}\""));
    });

app.MapPost(
        "/prof/api/sas",
        async Task<IResult> (
            HttpRequest request,
            IProfStorageService storage,
            ImageFileValidator imageFileValidator,
            CancellationToken ct) =>
        {
            var form = await request.ReadFormAsync(ct);
            var fileName = form["fileName"].ToString();
            var contentType = form["contentType"].ToString();
            if (!long.TryParse(form["size"], out var size))
                return Results.BadRequest(new { error = "size is required." });

            var headerBase64 = form["headerBase64"].ToString();
            if (string.IsNullOrWhiteSpace(headerBase64))
                return Results.BadRequest(new { error = "headerBase64 is required." });
            byte[] header;
            try { header = Convert.FromBase64String(headerBase64); }
            catch { return Results.BadRequest(new { error = "Invalid headerBase64." }); }

            if (size > imageFileValidator.MaxUploadBytes)
                return Results.BadRequest(new { error = $"Exceeds the {FileSizeFormatter.Format(imageFileValidator.MaxUploadBytes)} limit." });

            var validation = imageFileValidator.Validate(fileName, contentType, size, header);
            if (!validation.IsValid || string.IsNullOrWhiteSpace(validation.NormalizedContentType))
                return Results.BadRequest(new { error = validation.ErrorMessage ?? "Validation failed." });

            var ticket = await storage.GenerateSasUploadUrlAsync(fileName, validation.NormalizedContentType, ct);
            return Results.Ok(ticket);
        })
    .DisableAntiforgery();

app.MapPost(
        "/prof/api/complete",
        async Task<IResult> (
            HttpRequest request,
            IProfStorageService storage,
            ThumbnailQueue thumbnailQueue,
            CancellationToken ct) =>
        {
            var form = await request.ReadFormAsync(ct);
            var blobName = form["blobName"].ToString();
            var fileName = form["fileName"].ToString();
            var contentType = form["contentType"].ToString();

            if (string.IsNullOrWhiteSpace(blobName) || blobName != Path.GetFileName(blobName))
                return Results.BadRequest(new { error = "Invalid blobName." });

            if (!string.IsNullOrWhiteSpace(fileName))
                await storage.SetBlobOriginalFileNameAsync(blobName, fileName, ct);

            var isImage = !contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase)
                       && contentType != "application/zip";
            if (isImage)
            {
                var download = await storage.OpenReadAsync(blobName, ct);
                if (download is not null)
                {
                    await using var ms = new MemoryStream();
                    await download.Content.CopyToAsync(ms, ct);
                    await download.Content.DisposeAsync();
                    if (ms.TryGetBuffer(out var buf))
                        await thumbnailQueue.EnqueueAsync(blobName, "prof", buf.ToArray(), ct);
                }
            }

            return Results.Ok();
        })
    .DisableAntiforgery();

// ── End Profesionalni Sliki ───────────────────────────────────────────────────

app.MapPost(
        "/api/images/upload",
        async Task<IResult> (
            HttpRequest request,
            IImageStorageService imageStorageService,
            ImageFileValidator imageFileValidator,
            ThumbnailQueue thumbnailQueue,
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

            // Enqueue thumbnail — upload response is not blocked
            if (!validation.NormalizedContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase)
                && validation.NormalizedContentType != "application/zip"
                && memoryStream.TryGetBuffer(out var imgBuffer))
            {
                await thumbnailQueue.EnqueueAsync(result.BlobName, "", imgBuffer.ToArray(), cancellationToken);
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
