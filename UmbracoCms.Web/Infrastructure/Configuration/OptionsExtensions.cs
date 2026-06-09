namespace UmbracoCms.Web.Infrastructure.Configuration;

public static class OptionsExtensions
{
    /// <summary>
    /// Registers an options type bound to the configuration section matching TOptions type name (without "Options" suffix).
    /// </summary>
    public static IServiceCollection AddOptions<TOptions>(this IServiceCollection services, IConfiguration configuration)
        where TOptions : class
    {
        services.AddOptions<TOptions>()
            .Bind(configuration.GetSection<TOptions>())
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }

    /// <summary>
    /// Registers an options type bound to a specific configuration section.
    /// </summary>
    public static IServiceCollection AddOptions<TOptions>(this IServiceCollection services, IConfigurationSection configurationSection)
        where TOptions : class
    {
        services.AddOptions<TOptions>()
            .Bind(configurationSection.GetSection(GetSectionName<TOptions>()))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }

    /// <summary>
    /// Gets the configuration section for the given options type.
    /// </summary>
    public static IConfigurationSection GetSection<TOptions>(this IConfiguration configuration)
    {
        return configuration.GetSection(GetSectionName<TOptions>());
    }

    private static string GetSectionName<TOptions>()
    {
        string name = typeof(TOptions).Name;
        const string suffix = "Options";
        return name.EndsWith(suffix, StringComparison.Ordinal) ? name[..^suffix.Length] : name;
    }
}
