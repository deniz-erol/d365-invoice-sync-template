using System.Net.Http.Json;
using System.Text.Json;
using Azure.Security.KeyVault.Secrets;
using D365InvoiceSync.Application.Interfaces;
using D365InvoiceSync.Domain.Models;

namespace D365InvoiceSync.Infrastructure.ExternalApis;

public class XeroInvoiceClient : IExternalInvoiceClient
{
    private readonly HttpClient _httpClient;
    private readonly SecretClient _secretClient;
    private readonly ILogger<XeroInvoiceClient> _logger;
    private readonly string _tenantId;

    public XeroInvoiceClient(
        HttpClient httpClient,
        SecretClient secretClient,
        IConfiguration configuration,
        ILogger<XeroInvoiceClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _secretClient = secretClient ?? throw new ArgumentNullException(nameof(secretClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _tenantId = configuration["Xero:TenantId"] ?? throw new ArgumentException("Xero:TenantId not configured");
        
        _httpClient.BaseAddress = new Uri("https://api.xero.com/api.xro/2.0/");
    }

    public async Task<InvoiceSyncResult> CreateInvoiceAsync(ExternalInvoice invoice, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        try
        {
            var accessToken = await GetAccessTokenAsync(cancellationToken);
            
            using var request = new HttpRequestMessage(HttpMethod.Post, "Invoices");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Add("Xero-tenant-id", _tenantId);
            request.Content = JsonContent.Create(MapToXeroInvoice(invoice));

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var xeroResponse = JsonSerializer.Deserialize<XeroInvoiceResponse>(responseContent);
                var xeroInvoice = xeroResponse?.Invoices?.FirstOrDefault();

                return new InvoiceSyncResult
                {
                    Success = true,
                    ExternalId = xeroInvoice?.InvoiceID,
                    Status = SyncStatus.Synced
                };
            }

            _logger.LogError("Xero API error: {StatusCode} - {Response}", response.StatusCode, responseContent);
            
            var isRetryable = IsRetryableStatusCode(response.StatusCode);
            
            return new InvoiceSyncResult
            {
                Success = false,
                ErrorMessage = $"Xero API error: {response.StatusCode}",
                Status = isRetryable ? SyncStatus.Retryable : SyncStatus.Failed
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error calling Xero API");
            return new InvoiceSyncResult
            {
                Success = false,
                ErrorMessage = "Network error: " + ex.Message,
                Status = SyncStatus.Retryable
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling Xero API");
            return new InvoiceSyncResult
            {
                Success = false,
                ErrorMessage = "Unexpected error: " + ex.Message,
                Status = SyncStatus.Failed
            };
        }
    }

    private static bool IsRetryableStatusCode(System.Net.HttpStatusCode statusCode)
    {
        return statusCode is 
            System.Net.HttpStatusCode.TooManyRequests or
            System.Net.HttpStatusCode.ServiceUnavailable or
            System.Net.HttpStatusCode.GatewayTimeout;
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        var secret = await _secretClient.GetSecretAsync("XeroAccessToken", cancellationToken: cancellationToken);
        return secret.Value.Value;
    }

    private static XeroInvoiceRequest MapToXeroInvoice(ExternalInvoice invoice)
    {
        return new XeroInvoiceRequest
        {
            Type = "ACCREC",
            Contact = new XeroContact { ContactID = Guid.Parse(invoice.ContactId) },
            Date = invoice.Date.ToString("yyyy-MM-dd"),
            DueDate = invoice.DueDate.ToString("yyyy-MM-dd"),
            Reference = invoice.Reference,
            CurrencyCode = invoice.Currency,
            LineItems = invoice.LineItems.Select(line => new XeroLineItem
            {
                Description = line.Description,
                Quantity = line.Quantity,
                UnitAmount = line.UnitAmount,
                LineAmount = line.LineTotal,
                AccountCode = "200"
            }).ToList()
        };
    }

}

// Xero API Models
public class XeroInvoiceRequest
{
    public string Type { get; set; } = string.Empty;
    public XeroContact Contact { get; set; } = new();
    public string Date { get; set; } = string.Empty;
    public string DueDate { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
    public List<XeroLineItem> LineItems { get; set; } = new();
}

public class XeroContact
{
    public Guid ContactID { get; set; }
}

public class XeroLineItem
{
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitAmount { get; set; }
    public decimal LineAmount { get; set; }
    public string AccountCode { get; set; } = string.Empty;
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
