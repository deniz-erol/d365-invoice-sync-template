# Customization Guide

This template can be customized for different scenarios.

## Custom Invoice Transformations

### Adding Custom Fields

Edit `Transformers.cs` to map custom D365 fields:

```csharp
public ExternalInvoice Transform(D365Invoice d365Invoice)
{
    return new ExternalInvoice
    {
        // ... existing fields
        
        // Add custom field mapping
        CustomField1 = d365Invoice.ExtensionTables.CustomField1,
        ProjectId = d365Invoice.ProjectId,
        Department = d365Invoice.Department
    };
}
```

### Adding New External Systems

1. Create new transformer class:

```csharp
public class CustomTransformer : IInvoiceTransformer
{
    public ExternalInvoice Transform(D365Invoice d365Invoice)
    {
        // Custom transformation logic
    }
}
```

2. Register in `TransformerFactory`:

```csharp
"custom" => new CustomTransformer(),
```

## Custom API Authentication

If your external API uses API Keys instead of OAuth2:

```csharp
public class CustomApiClient : IExternalApiClient
{
    public async Task<ApiResult> CreateInvoiceAsync(ExternalInvoice invoice)
    {
        var apiKey = await _keyVaultClient.GetSecretAsync("CustomApiKey");
        _httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey.Value.Value);
        
        // ... rest of implementation
    }
}
```

## Custom Business Logic

### Invoice Validation

Add validation before sending to external API:

```csharp
public async Task<ApiResult> CreateInvoiceAsync(ExternalInvoice invoice)
{
    // Validate minimum amount
    if (invoice.Total < 0.01m)
    {
        _logger.LogWarning("Invoice amount too small: {Total}", invoice.Total);
        return new ApiResult 
        { 
            Success = false, 
            ErrorMessage = "Invoice amount below minimum threshold" 
        };
    }
    
    // ... rest of implementation
}
```

### Customer Mapping

Implement custom customer lookup:

```csharp
private async Task<string> MapCustomerToContact(string customerAccount)
{
    // Query mapping table in SQL or Dataverse
    var mapping = await _dbContext.CustomerMappings
        .FirstOrDefaultAsync(c => c.D365Account == customerAccount);
    
    return mapping?.ExternalId ?? customerAccount;
}
```

## Custom Monitoring

### Adding Custom Metrics

```csharp
// In InvoiceReceiver.cs
_logger.LogInformation("InvoiceProcessed", new Dictionary<string, object>
{
    ["InvoiceId"] = invoiceId,
    ["Customer"] = d365Invoice.CustomerAccount,
    ["Amount"] = d365Invoice.TotalAmount,
    ["Currency"] = d365Invoice.CurrencyCode
});
```

### Custom Alerts

Add to `infra/main.bicep`:

```bicep
resource customAlert 'Microsoft.Insights/metricAlerts@2018-03-01' = {
  name: '${baseName}-high-value-invoice-alert'
  properties: {
    // Alert when invoice > $100,000
  }
}
```

## Multi-Tenant Support

For supporting multiple D365 organizations:

1. Add tenant identifier to Service Bus messages
2. Route to different external systems based on tenant
3. Use tenant-specific credentials from Key Vault

See `docs/extending.md` for detailed multi-tenant implementation.
