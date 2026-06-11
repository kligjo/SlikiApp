using System.Text.Json.Serialization;
using Azure.Identity;
using Azure.Storage.Blobs;
using Asp.Versioning;
using UmbracoCms.Web.Api;
using UmbracoCms.Web.Controllers;
using UmbracoCms.Web.Helpers;
using UmbracoCms.Web.Helpers.Extensions;
using UmbracoCms.Web.Infrastructure;
using UmbracoCms.Web.Infrastructure.Configuration;
using UmbracoCms.Web.Infrastructure.Configuration.Options;
using UmbracoCms.Web.Infrastructure.ContentFinders;
using UmbracoCms.Web.Infrastructure.Middlewares;
using UmbracoCms.Web.Infrastructure.Middlewares.CustomResponseCaching;
using UmbracoCms.Web.Infrastructure.NotificationHandlers;
using UmbracoCms.Web.Services;
using UmbracoCms.Web.Services.Assets;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Options;
using Scrutor;
using SimpleMvcSitemap;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Infrastructure.DependencyInjection;
using Umbraco.Cms.Web.Common.Routing;

namespace UmbracoCms.Web;

/// <summary>
/// Responsible for app startup and configuration.
/// </summary>
public static class Startup
{
    /// <summary>
    /// Configures the <see cref="IWebHost"/>.
    /// </summary>
    public static void ConfigureWebHost(this WebApplicationBuilder builder)
    {
        builder.WebHost.ConfigureKestrel(options =>
        {
            // Remove Kestrel Server header
            options.AddServerHeader = false;
        });

        if (builder.Configuration.GetRuntimeMode() != RuntimeMode.Production)
        {
            builder.WebHost.UseStaticWebAssets();
        }
    }

    /// <summary>
    /// Configures the app <see cref="IConfiguration"/>.
    /// </summary>
    public static void ConfigureAppConfiguration(this ConfigurationManager configuration, IWebHostEnvironment environment)
    {
        string? appSettingsOverride = Environment.GetEnvironmentVariable("APPSETTINGS_OVERRIDE");

        if (environment.IsDevelopment() && appSettingsOverride is null or "")
        {
            appSettingsOverride = "Debug";
        }

        configuration.AddJsonFile($"appsettings.Overrides.{appSettingsOverride}.json", optional: true, reloadOnChange: true);
    }

    /// <summary>
    /// Configures the services on the <see cref="IServiceCollection"/> container.
    /// </summary>
    public static void ConfigureServices(this IServiceCollection services, IWebHostEnvironment environment, IConfigurationRoot configuration)
    {
        // Configure Application Options
        IConfigurationSection applicationOptionsConfigSection = configuration.GetSection<ApplicationOptions>();
        services.AddOptions<ApplicationOptions>(configuration);
        services.AddOptions<DevelopmentOptions>(applicationOptionsConfigSection);
        services.AddOptions<CacheOptions>(applicationOptionsConfigSection);
        services.AddOptions<AccessTokenOptions>()
            .Bind(configuration.GetSection<AccessTokenOptions>())
            .Validate(options => !string.IsNullOrWhiteSpace(options.QueryParameterName), "AccessToken:QueryParameterName is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.SharedToken), "AccessToken:SharedToken is required.")
            .ValidateOnStart();
        services.AddOptions<BlobStorageOptions>()
            .Bind(configuration.GetSection<BlobStorageOptions>())
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ConnectionString)
                    || Uri.TryCreate(options.ServiceUri, UriKind.Absolute, out _),
                "BlobStorage:ConnectionString or BlobStorage:ServiceUri must be configured.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.ContainerName), "BlobStorage:ContainerName is required.")
            .Validate(options => options.MaxUploadBytes > 0, "BlobStorage:MaxUploadBytes must be greater than zero.")
            .Validate(options => options.PageSize is > 0 and <= 50, "BlobStorage:PageSize must be between 1 and 50.")
            .ValidateOnStart();

        ApplicationOptions applicationOptions = applicationOptionsConfigSection.Get<ApplicationOptions>()
            ?? throw new InvalidOperationException("Unable to bind ApplicationOptions");

        // Caching Services
        services.AddCustomResponseCaching();
        services.AddSingleton<ICacheManager, CacheManager>();

        // Assets Services
        services.AddTransient<IAssetsProvider, FileSystemAssetsProvider>();
        services.ConfigureDevelopmentAssetsFallback(environment, applicationOptions.Development);
        services.Decorate<IAssetsProvider, CachedAssetsProvider>();

        // HttpClient for development fallback
        services.AddHttpClient("AssetsFallback");
        services.AddHttpContextAccessor();
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<BlobStorageOptions>>().Value;
            return !string.IsNullOrWhiteSpace(options.ConnectionString)
                ? new BlobServiceClient(options.ConnectionString)
                : new BlobServiceClient(new Uri(options.ServiceUri), new DefaultAzureCredential());
        });
        services.AddSingleton<ImageFileValidator>();
        services.AddSingleton<IImageStorageService, AzureBlobImageStorageService>();
        services.AddScoped<RequestAccessTokenService>();

        // All other application services that use the DI attributes
        services.Scan(scan => scan
            .FromAssemblies(typeof(NodeProvider).Assembly)
            .AddClasses(classes => classes.WithAttribute<ServiceDescriptorAttribute>())
            .UsingRegistrationStrategy(RegistrationStrategy.Throw)
            .UsingAttributes()
        );

        // Other 3rd party services
        services.AddTransient<ISitemapProvider, SitemapProvider>();

        // Umbraco Services
        services.AddUmbraco(environment, configuration)
            .AddBackOffice(builder =>
            {
                builder.AddMvcOptions(options => options.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider()));
                builder.AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
                builder.AddXmlDataContractSerializerFormatters();
                builder.AddRazorOptions(options =>
                {
                    options.ViewLocationFormats.Add("/{0}.cshtml");
                    options.ViewLocationFormats.Add("/Components/{0}.cshtml");
                    options.ViewLocationFormats.Add("/Components/{0}/{0}.cshtml");
                });
                builder.Services.AddApiVersioning(config =>
                {
                    config.DefaultApiVersion = new ApiVersion(1, 0);
                    config.AssumeDefaultVersionWhenUnspecified = true;
                });

                builder.Services.Configure<RouteOptions>(options =>
                {
                    options.LowercaseUrls = true;
                    options.AppendTrailingSlash = false;
                });

                builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
                {
                    if (applicationOptions.Cors.Origins.Length > 0)
                        policy.WithOrigins(applicationOptions.Cors.Origins);
                    if (applicationOptions.Cors.Methods.Length > 0)
                        policy.WithMethods(applicationOptions.Cors.Methods);
                    if (applicationOptions.Cors.Headers.Length > 0)
                        policy.WithHeaders(applicationOptions.Cors.Headers);
                }));
            })
            .AddWebsite()
            .AddComposers()
            .AddNotificationHandler<ContentCacheRefresherNotification, CacheFlushingNotificationHandler>()
            .AddNotificationHandler<MediaCacheRefresherNotification, CacheFlushingNotificationHandler>()
            .AddNotificationHandler<UmbracoApplicationStartedNotification, SlikiHomeContentNotificationHandler>()
            .SetServerRegistrar<ConfigurationServerRoleAccessor>()
            .SetContentLastChanceFinder<LastChanceContentFinder>()
            .SetDefaultRenderController<DefaultRenderController>()
            .Build();
    }

    /// <summary>
    /// Configures the <see cref="WebApplication"/> request pipeline.
    /// </summary>
    public static void ConfigureWebApplication(this WebApplication app)
    {
        ApplicationOptions applicationOptions = app.Services.GetRequiredService<IOptions<ApplicationOptions>>().Value;

        if (applicationOptions.Development.DeveloperExceptionPage)
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler($"{ErrorPagesHelper.ErrorPathPrefix}/500");
        }

        // Security headers
        app.UseSecurityHeaders(policies => policies
            .AddDefaultSecurityHeaders()
            .AddContentSecurityPolicy(builder =>
            {
                builder.AddObjectSrc().None();
                builder.AddFormAction().Self();
                builder.AddFrameAncestors().Self();
            })
            .AddFrameOptionsSameOrigin()
            .AddContentTypeOptionsNoSniff()
        );  

        app.UseCors();
        app.UseSharedAccessToken();

        // Status Code Pages
        app.UseStatusCodePagesWithReExecute($"{ErrorPagesHelper.ErrorPathPrefix}/{{0}}");
        app.Use((context, next) =>
        {
            // Ensure we clear the UmbracoRouteValues so the Umbraco routing executes again
            context.Features.Set<UmbracoRouteValues>(null);
            context.RequestServices.GetRequiredService<NodeProvider>().Reset();
            return next(context);
        });

        app.UseUmbraco()
            .WithMiddleware(u =>
            {
                u.UseBackOffice();

                app.UseDefaultFiles();

                // Static files
                app.UseStaticFiles();
                app.UseDevelopmentAssetsFallback(applicationOptions.Development);

                app.UseCustomResponseCaching();

                u.UseWebsite();
            })
            .WithEndpoints(u =>
            {
                if (applicationOptions.ServerRole != ServerRole.Subscriber)
                {
                    u.UseBackOfficeEndpoints();
                }

                u.EndpointRouteBuilder.UseRobots();
                u.EndpointRouteBuilder.UseApiFallbackRoute();

                u.UseWebsiteEndpoints();
            });
    }
}
