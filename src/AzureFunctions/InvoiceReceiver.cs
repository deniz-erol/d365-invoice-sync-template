using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Messaging.ServiceBus;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace D365InvoiceSync;

public class InvoiceReceiver
{
    private readonly ILogger<InvoiceReceiver> _logger;
    private readonly IInvoiceTransformer _transformer;
    private readonly IExternalApiClient _apiClient;
    private readonly IConfiguration _config;

    public InvoiceReceiver(
        ILogger<InvoiceReceiver> logger,
        IInvoiceTransformer transformer,
        IExternalApiClient apiClient,
        IConfiguration config)
    {
        _logger = logger;
        _transformer = transformer;
        _apiClient = apiClient;
        _config = config;
    }

    [Function(nameof(InvoiceReceiver))]
    [ServiceBusOutput("invoice-dlq", Connection = "ServiceBusConnection")]
    public async Task<ServiceBusMessage?> Run(
        [ServiceBusTrigger("invoice-posted", "invoice-processor", Connection = "ServiceBusConnection")]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions)
    {
        var invoiceId = message.ApplicationProperties.GetValueOrDefault("InvoiceId", "unknown");
        _logger.LogInformation("Processing invoice: {InvoiceId}", invoiceId);

        try
        {
            // Parse D365 invoice
            var d365Invoice = JsonSerializer.Deserialize<D365Invoice>(message.Body.ToString());
            if (d365Invoice == null)
            {
                _logger.LogError("Failed to deserialize invoice: {InvoiceId}", invoiceId);
                await messageActions.DeadLetterMessageAsync(message, "Deserialization failed");
                return null;
            }

            // Transform to external format
            var externalInvoice = _transformer.Transform(d365Invoice);
            
            // Send to external API
            var result = await _apiClient.CreateInvoiceAsync(externalInvoice);
            
            if (result.Success)
            {
                _logger.LogInformation("Invoice synced successfully: {InvoiceId} -> {ExternalId}", 
                    invoiceId, result.ExternalId);
                await messageActions.CompleteMessageAsync(message);
                return null;
            }
            else
            {
                _logger.LogWarning("API returned error for invoice {InvoiceId}: {Error}", 
                    invoiceId, result.ErrorMessage);
                
                // Check if we should retry
                var deliveryCount = message.DeliveryCount;
                if (deliveryCount < 3)
                {
                    _logger.LogInformation("Retrying invoice {InvoiceId} (attempt {Attempt})", 
                        invoiceId, deliveryCount + 1);
                    await messageActions.AbandonMessageAsync(message);
                    return null;
                }
                else
                {
                    _logger.LogError("Max retries exceeded for invoice {InvoiceId}", invoiceId);
                    await messageActions.DeadLetterMessageAsync(message, 
                        $"Max retries exceeded: {result.ErrorMessage}");
                    return null;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing invoice {InvoiceId}", invoiceId);
            
            // Return message for DLQ
            return new ServiceBusMessage(message.Body)
            {
                ApplicationProperties = 
                {
                    ["OriginalInvoiceId"] = invoiceId,
                    ["ErrorMessage"] = ex.Message,
                    ["Timestamp"] = DateTime.UtcNow
                }
            };
        }
    }
}

// Data models
public record D365Invoice
{
    public string InvoiceId { get; init; } = "";
    public string CustomerAccount { get; init; } = "";
    public DateTime InvoiceDate { get; init; }
    public DateTime DueDate { get; init; }
    public string CurrencyCode { get; init; } = "";
    public decimal TotalAmount { get; init; }
    public List<InvoiceLine> Lines { get; init; } = new();
}

public record InvoiceLine
{
    public string ItemId { get; init; } = "";
    public string Description { get; init; } = "";
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal LineAmount { get; init; }
}

public record ExternalInvoice
{
    public string Reference { get; init; } = "";
    public string ContactId { get; init; } = "";
    public DateTime Date { get; init; }
    public DateTime DueDate { get; init; }
    public string Currency { get; init; } = "";
    public decimal Total { get; init; }
    public List<ExternalLine> LineItems { get; init; } = new();
}

public record ExternalLine
{
    public string Description { get; init; } = "";
    public decimal Quantity { get; init; }
    public decimal UnitAmount { get; init; }
    public decimal LineTotal { get; init; }
}

public record ApiResult
{
    public bool Success { get; init; }
    public string? ExternalId { get; init; }
    public string? ErrorMessage { get; init; }
}

// Interfaces
public interface IInvoiceTransformer
{
    ExternalInvoice Transform(D365Invoice d365Invoice);
}

public interface IExternalApiClient
{
    Task<ApiResult> CreateInvoiceAsync(ExternalInvoice invoice);
}
