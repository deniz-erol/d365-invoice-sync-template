using D365InvoiceSync.Domain.Models;

namespace D365InvoiceSync.Application.Interfaces;

public interface IInvoiceTransformer
{
    Task<ExternalInvoice> TransformAsync(D365Invoice sourceInvoice, CancellationToken cancellationToken = default);
}

public interface IExternalInvoiceClient
{
    Task<InvoiceSyncResult> CreateInvoiceAsync(ExternalInvoice invoice, CancellationToken cancellationToken = default);
}

public interface IInvoiceSyncService
{
    Task<InvoiceSyncResult> SyncInvoiceAsync(D365Invoice invoice, CancellationToken cancellationToken = default);
}

public interface ICustomerMappingService
{
    Task<string> GetExternalContactIdAsync(string d365CustomerAccount, CancellationToken cancellationToken = default);
}

public interface IMessageHandler<T>
{
    Task<MessageHandlerResult> HandleAsync(T message, CancellationToken cancellationToken = default);
}

public record MessageHandlerResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public HandlerAction Action { get; init; }
}

public enum HandlerAction
{
    Complete,
    Retry,
    DeadLetter
}
