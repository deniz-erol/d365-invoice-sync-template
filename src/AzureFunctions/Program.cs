using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using D365InvoiceSync.Application.Interfaces;
using D365InvoiceSync.Application.Services;
using D365InvoiceSync.Infrastructure.ExternalApis;
using D365InvoiceSync.Infrastructure.Factories;
using D365InvoiceSync.Infrastructure.Services;
using D365InvoiceSync.Infrastructure.Transformers;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Configuration
var configuration = builder.Configuration;

// Key Vault
builder.Services.AddSingleton<SecretClient>(sp =>
{
    var keyVaultUri = configuration.GetValue<string>("KeyVaultUri");
    return new SecretClient(new Uri(keyVaultUri!), new DefaultAzureCredential());
});

// Customer Mapping
builder.Services.AddSingleton<ICustomerMappingService, InMemoryCustomerMappingService>();

// Transformers
builder.Services.AddSingleton<XeroInvoiceTransformer>();
builder.Services.AddSingleton<QuickBooksInvoiceTransformer>();

// External API Clients
builder.Services.AddHttpClient<XeroInvoiceClient>((sp, client) =>
{
    // Base address is set in the client constructor
});
builder.Services.AddSingleton<XeroInvoiceClient>(sp =>
{
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(XeroInvoiceClient));
    var secretClient = sp.GetRequiredService<SecretClient>();
    var logger = sp.GetRequiredService<ILogger<XeroInvoiceClient>>();
    return new XeroInvoiceClient(httpClient, secretClient, configuration, logger);
});

// Factories
builder.Services.AddSingleton<SyncFactoryResolver>();
builder.Services.AddSingleton<IInvoiceSyncFactory>(sp =>
{
    var resolver = sp.GetRequiredService<SyncFactoryResolver>();
    return resolver.Resolve();
});

// Invoice Sync Service
builder.Services.AddScoped<IInvoiceSyncService>(sp =>
{
    var factory = sp.GetRequiredService<IInvoiceSyncFactory>();
    var transformer = factory.CreateTransformer();
    var client = factory.CreateClient();
    var logger = sp.GetRequiredService<ILogger<InvoiceSyncService>>();
    return new InvoiceSyncService(transformer, client, logger);
});

builder.Build().Run();
