# D365 F&O Invoice Sync

An Azure Functions integration that syncs posted invoices from Dynamics 365 Finance & Operations to external accounting systems (Xero, QuickBooks, or any custom target).

[![.NET 8](https://img.shields.io/badge/.NET-8.0-blue)](https://dotnet.microsoft.com/)
[![Azure Functions](https://img.shields.io/badge/Azure%20Functions-v4-blue)](https://azure.microsoft.com/services/functions/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

## Architecture

```
┌─────────────┐     ┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│  D365 F&O   │────▶│  Dataverse   │────▶│ Service Bus  │────▶│   Function   │
│  (Trigger)  │     │    Plugin    │     │    Topic     │     │   (Process)  │
└─────────────┘     └──────────────┘     └──────────────┘     └──────┬───────┘
                                                                      │
                                                                      ▼
                                                             ┌──────────────┐
                                                             │ External API │
                                                             │(Xero/QB/etc) │
                                                             └──────────────┘
```

When an invoice is posted in D365, a Dataverse plugin publishes a message to a Service Bus topic. The Azure Function picks it up, transforms it into the target system's format, and calls the external API.

## Design Decisions

**Why Service Bus instead of a direct webhook?**
Decoupling D365 from the external system means a Xero outage or rate limit doesn't cause D365 to fail. Messages queue up and process when the downstream system recovers.

**Why Managed Identity for Key Vault?**
No credentials to rotate, no secrets in config files, no risk of accidental exposure. The Function's managed identity is granted `Key Vault Secrets User` — that's the full trust chain.

**Why token caching?**
The Xero access token is fetched from Key Vault and cached in memory with a configurable TTL. Without this, every message triggers a Key Vault round-trip, which adds latency and risks hitting the 1200 req/10s throttle limit under load.

**Why a factory pattern for the external client?**
Swapping from Xero to QuickBooks (or any other target) is a single config change (`ExternalSystemType`). No code changes, no redeployment of logic — just point it at a different implementation.

**Retry and DLQ strategy**
The function distinguishes between retryable failures (rate limits, transient network errors) and permanent failures (bad data, auth errors). Retryable messages are abandoned back to the queue; permanent failures go straight to the dead-letter queue with a reason. Max delivery count is configurable.

## Project Structure

```
src/AzureFunctions/
├── Domain/Models/          # D365 and external invoice models
├── Application/
│   ├── Interfaces/         # IInvoiceTransformer, IExternalInvoiceClient, etc.
│   └── Services/           # InvoiceSyncService (orchestrates transform + send)
├── Infrastructure/
│   ├── ExternalApis/       # XeroInvoiceClient
│   ├── Transformers/       # Xero and QuickBooks transformers
│   ├── Factories/          # SyncFactoryResolver (selects impl from config)
│   └── Services/           # Customer account mapping
└── Functions/              # InvoiceSyncFunction (Service Bus trigger)
```

## Configuration

| Key | Description |
|-----|-------------|
| `KeyVaultUri` | Key Vault URI for secret access |
| `ServiceBusConnection` | Service Bus connection string |
| `ExternalSystemType` | `xero` or `quickbooks` |
| `Xero:TenantId` | Xero organisation tenant ID |
| `Xero:BaseUrl` | Xero API base URL |
| `Xero:DefaultAccountCode` | Default GL account code for line items |
| `Xero:AccessTokenSecretName` | Key Vault secret name for the access token |
| `Xero:TokenCacheMinutes` | Token cache TTL in minutes (default: 30) |
| `ServiceBus:MaxDeliveryCount` | Retry attempts before dead-lettering (default: 3) |

## Quick Start

```bash
# 1. Deploy infrastructure
az login
az deployment sub create --location westeurope --template-file infra/main.bicep

# 2. Configure D365 F&O — see docs/setup.md

# 3. Deploy the function
func azure functionapp publish <your-function-app-name>
```

## Contact

📧 [denizerol95@gmail.com](mailto:denizerol95@gmail.com) · 💼 [linkedin.com/in/denizerol95](https://linkedin.com/in/denizerol95)

---

MIT License · Built by [Deniz Erol](https://linkedin.com/in/denizerol95)
