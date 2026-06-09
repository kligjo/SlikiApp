namespace UmbracoCms.Web.Infrastructure.Middlewares.CustomResponseCaching;

public static class CustomResponseCachingExtensions
{
    public static IServiceCollection AddCustomResponseCaching(this IServiceCollection services)
    {
        services.AddResponseCaching();
        services.AddSingleton<CustomResponseCachingMemoryCacheFactory>();
        return services;
    }

    public static IApplicationBuilder UseCustomResponseCaching(this IApplicationBuilder app)
    {
        app.UseResponseCaching();
        return app;
    }
}
