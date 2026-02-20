namespace D365InvoiceSync.Domain.Models;

public record D365Invoice
{
    public string InvoiceId { get; init; } = string.Empty;
    public string CustomerAccount { get; init; } = string.Empty;
    public DateTime InvoiceDate { get; init; }
    public DateTime DueDate { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public IReadOnlyList<InvoiceLine> Lines { get; init; } = Array.Empty<InvoiceLine>();
}

public record InvoiceLine
{
    public string ItemId { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal LineAmount { get; init; }
}

public record ExternalInvoice
{
    public string Reference { get; init; } = string.Empty;
    public string ContactId { get; init; } = string.Empty;
    public DateTime Date { get; init; }
    public DateTime DueDate { get; init; }
    public string Currency { get; init; } = string.Empty;
    public decimal Total { get; init; }
    public IReadOnlyList<ExternalLine> LineItems { get; init; } = Array.Empty<ExternalLine>();
}

public record ExternalLine
{
    public string Description { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public decimal UnitAmount { get; init; }
    public decimal LineTotal { get; init; }
}

public record InvoiceSyncResult
{
    public bool Success { get; init; }
    public string? ExternalId { get; init; }
    public string? ErrorMessage { get; init; }
    public SyncStatus Status { get; init; }
}

public enum SyncStatus
{
    Synced,
    Failed,
    Retryable,
    DeadLettered
}
