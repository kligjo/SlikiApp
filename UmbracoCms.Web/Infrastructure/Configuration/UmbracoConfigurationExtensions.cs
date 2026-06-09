using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Net.Http.Headers;
using UmbracoCms.Web.Infrastructure.Configuration.Options;
using Umbraco.Cms.Core.Sync;

namespace UmbracoCms.Web.Infrastructure.Configuration;

/// <summary>
/// Extension methods for configuring Umbraco-related services.
/// </summary>
public static class UmbracoConfigurationExtensions
{
    /// <summary>
    /// Configures load balancing services when running in a multi-server environment.
    /// </summary>
    /// <remarks>
    /// For Azure Web App deployments, configure distributed SQL Server caching
    /// and data protection with SQL repository. See README for Azure setup instructions.
    /// </remarks>
    public static IUmbracoBuilder AddLoadBalancingServices(
        this IUmbracoBuilder builder,
        IWebHostEnvironment environment,
        ServerRole serverRole)
    {
        if (serverRole == ServerRole.Single)
        {
            return builder;
        }

        // For load-balanced scenarios, add distributed caching here
        // Example: builder.Services.AddDistributedSqlServerCache(...)

        return builder;
    }

    /// <summary>
    /// Sets the default render controller type for Umbraco by registering a custom controller.
    /// In Umbraco 17, this is achieved by replacing the default IRenderController in DI.
    /// </summary>
    public static IUmbracoBuilder SetDefaultRenderController<T>(this IUmbracoBuilder builder)
        where T : Umbraco.Cms.Web.Common.Controllers.IRenderController
    {
        builder.Services.AddTransient(typeof(Umbraco.Cms.Web.Common.Controllers.IRenderController), typeof(T));
        return builder;
    }

    /// <summary>
    /// Configures static asset serving with caching headers.
    /// </summary>
    public static IServiceCollection ConfigureStaticAssets(this IServiceCollection services)
    {
        services.Configure<StaticFileOptions>(options =>
        {
            options.OnPrepareResponse = context =>
            {
                context.Context.Response.Headers[HeaderNames.CacheControl] = "public, max-age=86400";
            };

            // Add .less MIME type
            FileExtensionContentTypeProvider contentTypeProvider = new();
            contentTypeProvider.Mappings[".less"] = "text/css";
            options.ContentTypeProvider = contentTypeProvider;
        });

        return services;
    }
}
