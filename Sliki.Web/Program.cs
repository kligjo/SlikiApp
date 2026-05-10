using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Net.Http.Headers;
using Sliki.Web.Components;
using Sliki.Web.Options;
using Sliki.Web.Services;

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

app.Run();
