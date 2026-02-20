# Setup Guide

## Prerequisites

- Azure subscription
- Azure CLI installed
- .NET 8 SDK
- Azure Functions Core Tools
- D365 F&O environment with admin access

## Step 1: Deploy Azure Infrastructure

```bash
# Login to Azure
az login

# Set subscription (if you have multiple)
az account set --subscription "YOUR-SUBSCRIPTION-NAME"

# Create resource group
az group create --name d365-invoice-sync-rg --location westeurope

# Deploy infrastructure
az deployment group create \
  --resource-group d365-invoice-sync-rg \
  --template-file infra/main.bicep \
  --parameters environmentName=prod \
  --parameters externalSystemType=xero \
  --parameters d365OrganizationUrl=https://yourorg.operations.dynamics.com
```

## Step 2: Configure External API Credentials

### Option A: Xero

1. Go to https://developer.xero.com/app/manage
2. Create a new app
3. Generate OAuth2 credentials
4. Store in Key Vault:

```bash
# Set Key Vault name from deployment output
KEYVAULT_NAME=$(az deployment group show -g d365-invoice-sync-rg -n main --query properties.outputs.keyVaultUri.value -o tsv)

# Store credentials
az keyvault secret set --vault-name $KEYVAULT_NAME --name ExternalApiClientId --value "YOUR_XERO_CLIENT_ID"
az keyvault secret set --vault-name $KEYVAULT_NAME --name ExternalApiClientSecret --value "YOUR_XERO_CLIENT_SECRET"
```

### Option B: QuickBooks

1. Go to https://developer.intuit.com/app/developer/dashboard
2. Create an app
3. Get Client ID and Secret
4. Store same as above

### Option C: Custom API

Update `ExternalApiClient.cs` with your custom API authentication logic.

## Step 3: Deploy Azure Functions

```bash
# Navigate to functions project
cd src/AzureFunctions

# Restore packages
dotnet restore

# Build
dotnet build

# Publish
func azure functionapp publish $(az deployment group show -g d365-invoice-sync-rg -n main --query properties.outputs.functionAppName.value -o tsv)
```

## Step 4: Configure D365 F&O Plugin

### 4.1 Install Plugin Registration Tool

Download from: https://www.nuget.org/packages/Microsoft.CrmSdk.XrmTooling.PluginRegistrationTool

### 4.2 Register Plugin Assembly

1. Connect to your D365 environment
2. Register new assembly
3. Upload the compiled plugin DLL
4. Register steps:
   - Message: `Create`
   - Primary Entity: `SalesInvoiceHeader`
   - Event Pipeline: `Post-Operation`
   - Execution Mode: `Asynchronous`

### 4.3 Configure Plugin Settings

Add these secure configurations:

```json
{
  "ServiceBusConnectionString": "<from-key-vault>",
  "TopicName": "invoice-posted"
}
```

## Step 5: Test Integration

### 5.1 Create Test Invoice in D365

1. Go to Accounts receivable → Invoices → All free text invoices
2. Create new invoice
3. Post the invoice

### 5.2 Verify Message in Service Bus

```bash
# Check if message arrived
az servicebus topic subscription show \
  --resource-group d365-invoice-sync-rg \
  --namespace-name d365-invoice-prod-sb \
  --topic-name invoice-posted \
  --name invoice-processor
```

### 5.3 Check Function Logs

```bash
# Stream logs
func azure functionapp logstream $(az deployment group show -g d365-invoice-sync-rg -n main --query properties.outputs.functionAppName.value -o tsv)
```

### 5.4 Verify in External System

Check your Xero/QuickBooks for the synced invoice.

## Step 6: Monitoring & Alerts

### Set Up Alerts

```bash
# Create alert for failed invoices
az monitor metrics alert create \
  --name "InvoiceSyncFailures" \
  --resource-group d365-invoice-sync-rg \
  --scopes $(az deployment group show -g d365-invoice-sync-rg -n main --query properties.outputs.functionAppName.value -o tsv) \
  --condition "count exceptions > 5" \
  --evaluation-frequency 5m \
  --window-size 15m \
  --action email
```

### View Dashboard

Access Application Insights dashboard:
```bash
az monitor app-insights component show \
  --app $(az deployment group show -g d365-invoice-sync-rg -n main --query properties.outputs.appInsightsName.value -o tsv) \
  --resource-group d365-invoice-sync-rg \
  --query properties.portalUrl \
  -o tsv
```

## Troubleshooting

### Messages not appearing in Service Bus

1. Check D365 plugin trace logs
2. Verify plugin is registered on correct entity
3. Check async processing service is running

### Function not triggering

1. Check Function App is running
2. Verify Service Bus connection string
3. Check Application Insights exceptions

### External API errors

1. Check credentials in Key Vault
2. Verify OAuth2 tokens are valid
3. Check external API rate limits

### DLQ messages

1. Review error messages in Service Bus Explorer
2. Check Application Insights for stack traces
3. Manually retry after fixing issue

## Next Steps

- [Configure custom invoice transformations](customization.md)
- [Set up CI/CD pipeline](cicd.md)
- [Add more external systems](extending.md)
