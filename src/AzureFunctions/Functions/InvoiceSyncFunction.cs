using Azure.Messaging.ServiceBus;
using D365InvoiceSync.Application.Interfaces;
using D365InvoiceSync.Domain.Models;
using Microsoft.Azure.Functions.Worker;
using System.Text.Json;

namespace D365InvoiceSync.Functions;

public class InvoiceSyncFunction
{
    private readonly IInvoiceSyncService _syncService;
    private readonly ILogger<InvoiceSyncFunction> _logger;
    private readonly int _maxDeliveryCount;

    public InvoiceSyncFunction(
        IInvoiceSyncService syncService,
        ILogger<InvoiceSyncFunction> logger,
        IConfiguration configuration)
    {
        _syncService = syncService ?? throw new ArgumentNullException(nameof(syncService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _maxDeliveryCount = configuration.GetValue("ServiceBus:MaxDeliveryCount", 3);
    }

    [Function(nameof(InvoiceSyncFunction))]
    public async Task Run(
        [ServiceBusTrigger("invoice-posted", "invoice-processor", Connection = "ServiceBusConnection")]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions)
    {
        var invoiceId = message.ApplicationProperties.GetValueOrDefault("InvoiceId", "unknown")?.ToString() ?? "unknown";
        
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["InvoiceId"] = invoiceId,
            ["DeliveryCount"] = message.DeliveryCount,
            ["MessageId"] = message.MessageId
        });

        _logger.LogInformation("Processing invoice sync message");

        try
        {
            var d365Invoice = DeserializeInvoice(message);
            if (d365Invoice == null)
            {
                _logger.LogError("Failed to deserialize invoice from message body");
                await messageActions.DeadLetterMessageAsync(message, deadLetterReason: "DeserializationFailed");
                return;
            }

            var result = await _syncService.SyncInvoiceAsync(d365Invoice);

            await HandleResultAsync(result, message, messageActions, invoiceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception processing invoice {InvoiceId}", invoiceId);
            await HandleExceptionAsync(ex, message, messageActions, invoiceId);
        }
    }

    private static D365Invoice? DeserializeInvoice(ServiceBusReceivedMessage message)
    {
        try
        {
            return JsonSerializer.Deserialize<D365Invoice>(message.Body);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private async Task HandleResultAsync(
        InvoiceSyncResult result, 
        ServiceBusReceivedMessage message, 
        ServiceBusMessageActions messageActions,
        string invoiceId)
    {
        switch (result.Status)
        {
            case SyncStatus.Synced:
                _logger.LogInformation("Invoice {InvoiceId} synced successfully to external system", invoiceId);
                await messageActions.CompleteMessageAsync(message);
                break;

            case SyncStatus.Retryable:
                _logger.LogWarning("Invoice {InvoiceId} failed with retryable error, abandoning for retry", invoiceId);
                await messageActions.AbandonMessageAsync(message);
                break;

            case SyncStatus.Failed:
                _logger.LogError("Invoice {InvoiceId} failed permanently: {Error}", invoiceId, result.ErrorMessage);
                await messageActions.DeadLetterMessageAsync(message, deadLetterReason: "PermanentFailure", deadLetterErrorDescription: result.ErrorMessage);
                break;

            default:
                _logger.LogError("Unknown sync status for invoice {InvoiceId}: {Status}", invoiceId, result.Status);
                await messageActions.DeadLetterMessageAsync(message, deadLetterReason: "UnknownStatus");
                break;
        }
    }

    private async Task HandleExceptionAsync(
        Exception ex, 
        ServiceBusReceivedMessage message, 
        ServiceBusMessageActions messageActions,
        string invoiceId)
    {
        // Check if we should retry based on delivery count
        if (message.DeliveryCount < _maxDeliveryCount)
        {
            _logger.LogWarning(ex, "Exception processing invoice {InvoiceId}, abandoning for retry (attempt {Attempt})", 
                invoiceId, message.DeliveryCount + 1);
            await messageActions.AbandonMessageAsync(message);
        }
        else
        {
            _logger.LogError(ex, "Max retry attempts exceeded for invoice {InvoiceId}, dead lettering", invoiceId);
            await messageActions.DeadLetterMessageAsync(message, 
                deadLetterReason: "MaxRetriesExceeded", 
                deadLetterErrorDescription: ex.Message);
        }
    }
}
