using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace D365InvoiceSync;

public class XeroApiClient : IExternalApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<XeroApiClient> _logger;
    private readonly SecretClient _keyVaultClient;

    public XeroApiClient(
        HttpClient httpClient, 
        IConfiguration config,
        ILogger<XeroApiClient> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
        _keyVaultClient = new SecretClient(
            new Uri(config.GetValue<string>("KeyVaultUri")!), 
            new DefaultAzureCredential());
    }

    public async Task<ApiResult> CreateInvoiceAsync(ExternalInvoice invoice)
    {
        try
        {
            var token = await GetAccessTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);
            _httpClient.DefaultRequestHeaders.Add("Xero-tenant-id", await GetTenantIdAsync());

            var xeroInvoice = new
            {
                Type = "ACCREC",
                Contact = new { ContactID = invoice.ContactId },
                Date = invoice.Date.ToString("yyyy-MM-dd"),
                DueDate = invoice.DueDate.ToString("yyyy-MM-dd"),
                Reference = invoice.Reference,
                CurrencyCode = invoice.Currency,
                LineItems = invoice.LineItems.Select(line => new
                {
                    Description = line.Description,
                    Quantity = line.Quantity,
                    UnitAmount = line.UnitAmount,
                    LineAmount = line.LineTotal,
                    AccountCode = "200" // Default sales account
                }).ToList()
            };

            var content = new StringContent(
                JsonSerializer.Serialize(xeroInvoice),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(
                "https://api.xero.com/api.xro/2.0/Invoices", 
                content);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                var invoiceResponse = JsonSerializer.Deserialize<XeroInvoiceResponse>(result);
                
                return new ApiResult
                {
                    Success = true,
                    ExternalId = invoiceResponse?.Invoices?.FirstOrDefault()?.InvoiceID
                };
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Xero API error: {StatusCode} - {Error}", 
                    response.StatusCode, error);
                
                return new ApiResult
                {
                    Success = false,
                    ErrorMessage = $"Xero API returned {response.StatusCode}: {error}"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating invoice in Xero");
            return new ApiResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private async Task<string> GetAccessTokenAsync()
    {
        // In production, implement proper OAuth2 flow with refresh token
        // For template, retrieve from Key Vault
        var tokenSecret = await _keyVaultClient.GetSecretAsync("XeroAccessToken");
        return tokenSecret.Value.Value;
    }

    private async Task<string> GetTenantIdAsync()
    {
        var tenantSecret = await _keyVaultClient.GetSecretAsync("XeroTenantId");
        return tenantSecret.Value.Value;
    }
}

public class QuickBooksApiClient : IExternalApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<QuickBooksApiClient> _logger;

    public QuickBooksApiClient(
        HttpClient httpClient,
        IConfiguration config,
        ILogger<QuickBooksApiClient> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
    }

    public async Task<ApiResult> CreateInvoiceAsync(ExternalInvoice invoice)
    {
        // QuickBooks API implementation
        // Similar pattern to Xero but with QB-specific API
        throw new NotImplementedException("QuickBooks implementation coming soon");
    }
}

public class ApiClientFactory
{
    private readonly IServiceProvider _services;
    private readonly IConfiguration _config;

    public ApiClientFactory(IServiceProvider services, IConfiguration config)
    {
        _services = services;
        _config = config;
    }

    public IExternalApiClient CreateClient()
    {
        var systemType = _config.GetValue<string>("ExternalSystemType")?.ToLower() ?? "xero";
        
        return systemType switch
        {
            "xero" => _services.GetRequiredService<XeroApiClient>(),
            "quickbooks" => _services.GetRequiredService<QuickBooksApiClient>(),
            _ => throw new NotSupportedException($"External system '{systemType}' not supported")
        };
    }
}

public class XeroInvoiceResponse
{
    public List<XeroInvoice>? Invoices { get; set; }
}

public class XeroInvoice
{
    public string? InvoiceID { get; set; }
    public string? InvoiceNumber { get; set; }
}
