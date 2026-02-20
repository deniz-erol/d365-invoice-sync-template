using System.Net.Http.Json;
using Azure.Security.KeyVault.Secrets;
using D365InvoiceSync.Application.Interfaces;
using D365InvoiceSync.Domain.Models;

namespace D365InvoiceSync.Infrastructure.ExternalApis;

public class QuickBooksInvoiceClient : IExternalInvoiceClient
{
    private readonly HttpClient _httpClient;
    private readonly SecretClient _secretClient;
    private readonly ILogger<QuickBooksInvoiceClient> _logger;

    public QuickBooksInvoiceClient(
        HttpClient httpClient,
        SecretClient secretClient,
        IConfiguration configuration,
        ILogger<QuickBooksInvoiceClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _secretClient = secretClient ?? throw new ArgumentNullException(nameof(secretClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _httpClient.BaseAddress = new Uri("https://quickbooks.api.intuit.com/v3/company/");
    }

    public async Task<InvoiceSyncResult> CreateInvoiceAsync(ExternalInvoice invoice, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        _logger.LogWarning("QuickBooks integration is not fully implemented yet");

        // TODO: Implement QuickBooks API integration
        // 1. Get OAuth2 token from Key Vault or refresh
        // 2. Map ExternalInvoice to QuickBooks Invoice model
        // 3. POST to QuickBooks API
        // 4. Handle response and errors

        return new InvoiceSyncResult
        {
            Success = false,
            ErrorMessage = "QuickBooks integration not yet implemented. Configure ExternalSystemType=xero or implement QuickBooks support.",
            Status = SyncStatus.Failed
        };
    }
}
