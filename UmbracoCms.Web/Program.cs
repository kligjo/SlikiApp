using UmbracoCms.Web;

try
{
    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    builder.ConfigureWebHost();
    builder.Configuration.ConfigureAppConfiguration(builder.Environment);
    builder.Services.ConfigureServices(builder.Environment, builder.Configuration);

    WebApplication app = builder.Build();

    await app.BootUmbracoAsync();

    app.ConfigureWebApplication();

    await app.RunAsync();
}
catch (Exception ex)
{
    Console.Error.WriteLine("========================================");
    Console.Error.WriteLine("FATAL: Unhandled startup exception:");
    Console.Error.WriteLine(ex.ToString());
    Console.Error.WriteLine("========================================");
    Console.Error.Flush();
    return 1;
}

return 0;