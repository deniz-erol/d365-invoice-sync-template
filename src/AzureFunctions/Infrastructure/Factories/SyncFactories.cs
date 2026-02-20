using D365InvoiceSync.Application.Interfaces;

namespace D365InvoiceSync.Infrastructure.Factories;

public interface IInvoiceSyncFactory
{
    IInvoiceTransformer CreateTransformer();
    IExternalInvoiceClient CreateClient();
}

public class XeroSyncFactory : IInvoiceSyncFactory
{
    private readonly IServiceProvider _services;

    public XeroSyncFactory(IServiceProvider services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    public IInvoiceTransformer CreateTransformer() => 
        _services.GetRequiredService<XeroInvoiceTransformer>();

    public IExternalInvoiceClient CreateClient() => 
        _services.GetRequiredService<XeroInvoiceClient>();
}

public class QuickBooksSyncFactory : IInvoiceSyncFactory
{
    private readonly IServiceProvider _services;

    public QuickBooksSyncFactory(IServiceProvider services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    public IInvoiceTransformer CreateTransformer() => 
        _services.GetRequiredService<QuickBooksInvoiceTransformer>();

    public IExternalInvoiceClient CreateClient() => 
        throw new NotImplementedException("QuickBooks client not yet implemented");
}

public class SyncFactoryResolver
{
    private readonly IServiceProvider _services;
    private readonly IConfiguration _configuration;

    public SyncFactoryResolver(IServiceProvider services, IConfiguration configuration)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public IInvoiceSyncFactory Resolve()
    {
        var systemType = _configuration.GetValue<string>("ExternalSystemType")?.ToLowerInvariant() ?? "xero";

        return systemType switch
        {
            "xero" => new XeroSyncFactory(_services),
            "quickbooks" => new QuickBooksSyncFactory(_services),
            _ => throw new NotSupportedException($"External system '{systemType}' is not supported")
        };
    }
}
