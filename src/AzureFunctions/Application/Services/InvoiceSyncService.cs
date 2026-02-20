using D365InvoiceSync.Application.Interfaces;
using D365InvoiceSync.Domain.Models;

namespace D365InvoiceSync.Application.Services;

public class InvoiceSyncService : IInvoiceSyncService
{
    private readonly IInvoiceTransformer _transformer;
    private readonly IExternalInvoiceClient _externalClient;
    private readonly ILogger<InvoiceSyncService> _logger;

    public InvoiceSyncService(
        IInvoiceTransformer transformer,
        IExternalInvoiceClient externalClient,
        ILogger<InvoiceSyncService> logger)
    {
        _transformer = transformer ?? throw new ArgumentNullException(nameof(transformer));
        _externalClient = externalClient ?? throw new ArgumentNullException(nameof(externalClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<InvoiceSyncResult> SyncInvoiceAsync(D365Invoice invoice, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        _logger.LogInformation("Starting invoice sync for {InvoiceId}", invoice.InvoiceId);

        try
        {
            var externalInvoice = _transformer.Transform(invoice);
            var result = await _externalClient.CreateInvoiceAsync(externalInvoice, cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation("Invoice {InvoiceId} synced successfully with external ID {ExternalId}",
                    invoice.InvoiceId, result.ExternalId);
            }
            else
            {
                _logger.LogWarning("Invoice {InvoiceId} sync failed: {Error}",
                    invoice.InvoiceId, result.ErrorMessage);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error syncing invoice {InvoiceId}", invoice.InvoiceId);
            return new InvoiceSyncResult
            {
                Success = false,
                ErrorMessage = $"Unexpected error: {ex.Message}",
                Status = SyncStatus.Failed
            };
        }
    }
}
