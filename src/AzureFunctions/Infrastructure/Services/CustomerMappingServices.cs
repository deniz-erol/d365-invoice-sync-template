using D365InvoiceSync.Application.Interfaces;

namespace D365InvoiceSync.Infrastructure.Services;

public class InMemoryCustomerMappingService : ICustomerMappingService
{
    private readonly ILogger<InMemoryCustomerMappingService> _logger;
    private readonly Dictionary<string, string> _mappings;

    public InMemoryCustomerMappingService(ILogger<InMemoryCustomerMappingService> logger, IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Load mappings from configuration
        _mappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        
        var mappingsSection = configuration.GetSection("CustomerMappings");
        foreach (var mapping in mappingsSection.GetChildren())
        {
            var d365Account = mapping.Key;
            var externalId = mapping.Value;
            if (!string.IsNullOrEmpty(d365Account) && !string.IsNullOrEmpty(externalId))
            {
                _mappings[d365Account] = externalId;
                _logger.LogDebug("Loaded customer mapping: {D365Account} -> {ExternalId}", d365Account, externalId);
            }
        }
        
        _logger.LogInformation("Loaded {Count} customer mappings from configuration", _mappings.Count);
    }

    public Task<string> GetExternalContactIdAsync(string d365CustomerAccount, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(d365CustomerAccount);

        if (_mappings.TryGetValue(d365CustomerAccount, out var externalId))
        {
            _logger.LogDebug("Found mapping for customer {D365Account} -> {ExternalId}", 
                d365CustomerAccount, externalId);
            return Task.FromResult(externalId);
        }

        _logger.LogWarning("No mapping found for customer {D365Account}, using account as-is", 
            d365CustomerAccount);
        
        // Fallback: use D365 account as external ID
        return Task.FromResult(d365CustomerAccount);
    }

    public void AddMapping(string d365Account, string externalId)
    {
        _mappings[d365Account] = externalId;
    }
}

public class SqlCustomerMappingService : ICustomerMappingService
{
    private readonly string _connectionString;
    private readonly ILogger<SqlCustomerMappingService> _logger;

    public SqlCustomerMappingService(IConfiguration configuration, ILogger<SqlCustomerMappingService> logger)
    {
        _connectionString = configuration.GetConnectionString("CustomerMappingDb") 
            ?? throw new ArgumentException("CustomerMappingDb connection string not configured");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> GetExternalContactIdAsync(string d365CustomerAccount, CancellationToken cancellationToken = default)
    {
        // TODO: Implement SQL query to lookup mapping
        // Example: SELECT ExternalId FROM CustomerMappings WHERE D365Account = @account
        
        _logger.LogInformation("SQL mapping lookup for {Account} - Not yet implemented", d365CustomerAccount);
        return await Task.FromResult(d365CustomerAccount);
    }
}
