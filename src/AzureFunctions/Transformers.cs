namespace D365InvoiceSync;

public class XeroInvoiceTransformer : IInvoiceTransformer
{
    public ExternalInvoice Transform(D365Invoice d365Invoice)
    {
        return new ExternalInvoice
        {
            Reference = d365Invoice.InvoiceId,
            ContactId = MapCustomerToContact(d365Invoice.CustomerAccount),
            Date = d365Invoice.InvoiceDate,
            DueDate = d365Invoice.DueDate,
            Currency = d365Invoice.CurrencyCode,
            Total = d365Invoice.TotalAmount,
            LineItems = d365Invoice.Lines.Select(line => new ExternalLine
            {
                Description = line.Description,
                Quantity = line.Quantity,
                UnitAmount = line.UnitPrice,
                LineTotal = line.LineAmount
            }).ToList()
        };
    }

    private string MapCustomerToContact(string customerAccount)
    {
        // TODO: Implement customer mapping logic
        // This could query a mapping table or use a convention
        return customerAccount;
    }
}

public class QuickBooksTransformer : IInvoiceTransformer
{
    public ExternalInvoice Transform(D365Invoice d365Invoice)
    {
        // QuickBooks has slightly different field names
        return new ExternalInvoice
        {
            Reference = d365Invoice.InvoiceId,
            ContactId = d365Invoice.CustomerAccount,
            Date = d365Invoice.InvoiceDate,
            DueDate = d365Invoice.DueDate,
            Currency = MapCurrency(d365Invoice.CurrencyCode),
            Total = d365Invoice.TotalAmount,
            LineItems = d365Invoice.Lines.Select(line => new ExternalLine
            {
                Description = line.Description,
                Quantity = line.Quantity,
                UnitAmount = line.UnitPrice,
                LineTotal = line.LineAmount
            }).ToList()
        };
    }

    private string MapCurrency(string d365Currency)
    {
        // D365 uses 3-letter codes, QuickBooks might use different format
        return d365Currency.ToUpper();
    }
}

public class TransformerFactory
{
    private readonly IConfiguration _config;

    public TransformerFactory(IConfiguration config)
    {
        _config = config;
    }

    public IInvoiceTransformer CreateTransformer()
    {
        var systemType = _config.GetValue<string>("ExternalSystemType")?.ToLower() ?? "xero";
        
        return systemType switch
        {
            "xero" => new XeroInvoiceTransformer(),
            "quickbooks" => new QuickBooksTransformer(),
            _ => throw new NotSupportedException($"External system '{systemType}' not supported")
        };
    }
}
