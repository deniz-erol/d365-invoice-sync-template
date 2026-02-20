# D365 Invoice Sync Template - Project Structure

## Overview

Production-ready Azure integration template for syncing invoices from Dynamics 365 Finance & Operations to external accounting systems.

## Directory Structure

```
d365-invoice-sync-template/
├── README.md                 # Main documentation & landing page
├── LICENSE                   # MIT License
├── .gitignore               # Git ignore rules
│
├── infra/                   # Infrastructure as Code (Bicep)
│   ├── main.bicep          # Main Azure deployment template
│   └── parameters.json     # Default parameter values
│
├── src/                     # Source code
│   └── AzureFunctions/     # .NET 8 Isolated Worker Azure Functions
│       ├── D365InvoiceSync.csproj    # Project file
│       ├── host.json                 # Functions host configuration
│       ├── local.settings.json       # Local development settings
│       ├── Program.cs                # Dependency injection setup
│       ├── InvoiceReceiver.cs        # Main invoice processing function
│       ├── Transformers.cs           # Invoice transformation logic
│       ├── ExternalApiClients.cs     # Xero/QB API clients
│       └── Models/                   # Data models
│           └── InvoiceModels.cs
│
├── docs/                    # Documentation
│   ├── architecture.md     # System architecture & design
│   ├── setup.md           # Step-by-step setup guide
│   ├── customization.md   # Customization options
│   ├── cicd.md           # CI/CD pipeline setup
│   ├── extending.md      # Adding new external systems
│   └── troubleshooting.md # Common issues & solutions
│
└── tests/                   # Test projects
    └── InvoiceSync.Tests/
        ├── InvoiceSync.Tests.csproj
        ├── TransformerTests.cs
        └── ApiClientTests.cs
```

## Key Components

### Azure Functions

- **InvoiceReceiver**: Processes messages from Service Bus, transforms invoices, sends to external API
- **Transformers**: Converts D365 invoice format to Xero/QuickBooks format
- **ExternalApiClients**: OAuth2 authentication and API calls to external systems

### Infrastructure (Bicep)

- Service Bus Namespace with Topics & Subscriptions
- Azure Functions App (.NET 8 isolated)
- Key Vault for secrets
- Application Insights for monitoring
- API Management (optional)

### Documentation

- **setup.md**: Complete deployment guide
- **architecture.md**: System design and data flow
- **customization.md**: How to customize for your needs

## Getting Started

1. Clone this repository
2. Review [docs/architecture.md](docs/architecture.md) for system design
3. Follow [docs/setup.md](docs/setup.md) to deploy
4. Test with sample invoice data

## Support

- 📧 Email: denizerol95@gmail.com
- 💼 LinkedIn: https://linkedin.com/in/denizerol95
- 🐛 Issues: Open a GitHub issue
