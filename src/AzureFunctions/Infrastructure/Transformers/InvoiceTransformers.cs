using D365InvoiceSync.Application.Interfaces;
using D365InvoiceSync.Domain.Models;

namespace D365InvoiceSync.Infrastructure.Transformers;

public class XeroInvoiceTransformer : IInvoiceTransformer
{
    private readonly ICustomerMappingService _customerMapping;

    public XeroInvoiceTransformer(ICustomerMappingService customerMapping)
    {
        _customerMapping = customerMapping ?? throw new ArgumentNullException(nameof(customerMapping));
    }

    public ExternalInvoice Transform(D365Invoice sourceInvoice)
    {
        ArgumentNullException.ThrowIfNull(sourceInvoice);

        var contactId = _customerMapping.GetExternalContactIdAsync(sourceInvoice.CustomerAccount).GetAwaiter().GetResult();

        return new ExternalInvoice
        {
            Reference = sourceInvoice.InvoiceId,
            ContactId = contactId,
            Date = sourceInvoice.InvoiceDate,
            DueDate = sourceInvoice.DueDate,
            Currency = sourceInvoice.CurrencyCode,
            Total = sourceInvoice.TotalAmount,
            LineItems = sourceInvoice.Lines.Select(TransformLine).ToList()
        };
    }

    private static ExternalLine TransformLine(InvoiceLine line)
    {
        return new ExternalLine
        {
            Description = line.Description,
            Quantity = line.Quantity,
            UnitAmount = line.UnitPrice,
            LineTotal = line.LineAmount
        };
    }
}

public class QuickBooksInvoiceTransformer : IInvoiceTransformer
{
    private readonly ICustomerMappingService _customerMapping;

    public QuickBooksInvoiceTransformer(ICustomerMappingService customerMapping)
    {
        _customerMapping = customerMapping ?? throw new ArgumentNullException(nameof(customerMapping));
    }

    public ExternalInvoice Transform(D365Invoice sourceInvoice)
    {
        ArgumentNullException.ThrowIfNull(sourceInvoice);

        var contactId = _customerMapping.GetExternalContactIdAsync(sourceInvoice.CustomerAccount).GetAwaiter().GetResult();

        return new ExternalInvoice
        {
            Reference = sourceInvoice.InvoiceId,
            ContactId = contactId,
            Date = sourceInvoice.InvoiceDate,
            DueDate = sourceInvoice.DueDate,
            Currency = NormalizeCurrency(sourceInvoice.CurrencyCode),
            Total = sourceInvoice.TotalAmount,
            LineItems = sourceInvoice.Lines.Select(TransformLine).ToList()
        };
    }

    private static ExternalLine TransformLine(InvoiceLine line)
    {
        return new ExternalLine
        {
            Description = line.Description,
            Quantity = line.Quantity,
            UnitAmount = line.UnitPrice,
            LineTotal = line.LineAmount
        };
    }

    private static string NormalizeCurrency(string currencyCode)
    {
        return currencyCode.ToUpperInvariant();
    }
}
