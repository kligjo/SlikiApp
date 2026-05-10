using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Net.Http.Headers;
using Sliki.Web.Components;
using Sliki.Web.Models;
using Sliki.Web.Options;
using Sliki.Web.Services;
using Sliki.Web.Utilities;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services
    .AddOptions<BlobStorageOptions>()
    .Bind(builder.Configuration.GetSection(BlobStorageOptions.SectionName))
    .Validate(
        options => Uri.TryCreate(options.ServiceUri, UriKind.Absolute, out _),
        "BlobStorage:ServiceUri must be a valid absolute URI.")
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
    return new BlobServiceClient(new Uri(options.ServiceUri), new DefaultAzureCredential());
});
builder.Services.AddSingleton<ImageFileValidator>();
builder.Services.AddSingleton<IImageStorageService, AzureBlobImageStorageService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

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

app.MapPost(
        "/api/images/upload",
        async Task<IResult> (
            HttpRequest request,
            IImageStorageService imageStorageService,
            ImageFileValidator imageFileValidator,
            CancellationToken cancellationToken) =>
        {
            var form = await request.ReadFormAsync(cancellationToken);
            var file = form.Files["file"] ?? form.Files.FirstOrDefault();

            if (file is null)
            {
                return Results.BadRequest(new { error = "Select an image file to upload." });
            }

            if (file.Length <= 0)
            {
                return Results.BadRequest(new { error = "The selected file is empty." });
            }

            if (file.Length > imageFileValidator.MaxUploadBytes)
            {
                return Results.BadRequest(new
                {
                    error = $"The file exceeds the limit of {FileSizeFormatter.Format(imageFileValidator.MaxUploadBytes)}."
                });
            }

            await using var uploadedFileStream = file.OpenReadStream();
            await using var memoryStream = new MemoryStream(capacity: file.Length > int.MaxValue ? int.MaxValue : (int)file.Length);
            await uploadedFileStream.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;

            byte[] headerBytes;
            if (memoryStream.TryGetBuffer(out var buffer))
            {
                headerBytes = buffer.AsSpan(0, (int)Math.Min(memoryStream.Length, 32)).ToArray();
            }
            else
            {
                var bytes = memoryStream.ToArray();
                headerBytes = bytes.AsSpan(0, Math.Min(bytes.Length, 32)).ToArray();
            }

            var validation = imageFileValidator.Validate(file.FileName, file.ContentType, file.Length, headerBytes);
            if (!validation.IsValid || string.IsNullOrWhiteSpace(validation.NormalizedContentType))
            {
                return Results.BadRequest(new { error = validation.ErrorMessage ?? "The image failed validation." });
            }

            memoryStream.Position = 0;
            var result = await imageStorageService.UploadAsync(
                new UploadImageRequest(
                    file.FileName,
                    validation.NormalizedContentType,
                    file.Length,
                    memoryStream),
                progress: null,
                cancellationToken);

            return Results.Ok(result);
        })
    .DisableAntiforgery();

app.Run();
