# D365 F&O Invoice Sync Template

Production-ready Azure integration template for syncing invoices from Dynamics 365 Finance & Operations to external accounting systems.

[![.NET 8](https://img.shields.io/badge/.NET-8.0-blue)](https://dotnet.microsoft.com/)
[![Azure Functions](https://img.shields.io/badge/Azure-Functions-blue)](https://azure.microsoft.com/services/functions/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

## What This Template Does

Syncs invoices from D365 F&O to external systems (Xero, QuickBooks, custom ERP) with:
- **Event-driven architecture** using Azure Service Bus
- **Resilient processing** with retry logic and DLQ
- **Secure authentication** via Managed Identity & OAuth2
- **Full observability** with Application Insights
- **Infrastructure as Code** with Bicep

## Architecture

```
┌─────────────┐     ┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│  D365 F&O   │────▶│  Dataverse   │────▶│ Service Bus  │────▶│   Function   │
│  (Trigger)  │     │    Plugin    │     │    Queue     │     │   (Process)  │
└─────────────┘     └──────────────┘     └──────────────┘     └──────┬───────┘
                                                                      │
                                                                      ▼
                                                             ┌──────────────┐
                                                             │ External API │
                                                             │(Xero/QB/etc) │
                                                             └──────────────┘
```

## Quick Start

```bash
# 1. Clone
git clone https://github.com/deniz-erol/d365-invoice-sync-template.git
cd d365-invoice-sync-template

# 2. Deploy infrastructure
az login
az deployment sub create --location westeurope --template-file infra/main.bicep

# 3. Configure D365 F&O webhook
# See docs/setup.md for detailed steps

# 4. Deploy functions
func azure functionapp publish <your-function-app-name>
```

## What's Included

- ✅ Azure Functions (.NET 8 isolated worker)
- ✅ Service Bus with DLQ configuration
- ✅ API Management security policies
- ✅ Key Vault integration
- ✅ Application Insights monitoring
- ✅ Bicep IaC for one-click deployment
- ✅ Comprehensive documentation
- ✅ Unit & integration tests

## Pricing

| Tier | Includes | Price |
|------|----------|-------|
| **Free** | Source code + basic docs | Free |
| **Pro** | + Video walkthrough + 1hr support | $299 |
| **Enterprise** | + 4hr consultation + customization | $999 |

## Support

- 📧 Email: denizerol95@gmail.com
- 💼 LinkedIn: [linkedin.com/in/denizerol95](https://linkedin.com/in/denizerol95)

## License

MIT - See [LICENSE](LICENSE)

---

**Built by [Deniz Erol](https://linkedin.com/in/denizerol95)** - Azure Integration Specialist
