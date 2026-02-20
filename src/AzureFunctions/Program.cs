using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Dependency Injection
builder.Services.AddSingleton<IInvoiceTransformer>(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    var factory = new TransformerFactory(config);
    return factory.CreateTransformer();
});

builder.Services.AddSingleton<IExternalApiClient>(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    var factory = new ApiClientFactory(provider, config);
    return factory.CreateClient();
});

// HttpClient for external API calls
builder.Services.AddHttpClient<XeroApiClient>();
builder.Services.AddHttpClient<QuickBooksApiClient>();

builder.Build().Run();
