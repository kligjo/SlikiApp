using UmbracoCms.Web;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.ConfigureWebHost();
builder.Configuration.ConfigureAppConfiguration(builder.Environment);
builder.Services.ConfigureServices(builder.Environment, builder.Configuration);

WebApplication app = builder.Build();

await app.BootUmbracoAsync();

app.ConfigureWebApplication();

await app.RunAsync();