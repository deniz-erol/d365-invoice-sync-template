# Architecture Overview

## System Design

This template implements a resilient, event-driven integration between D365 F&O and external accounting systems.

## Components

### 1. D365 F&O Plugin
A Dataverse plugin triggers when invoices are posted in D365 F&O.

```csharp
// Trigger: Invoice posted
// Action: Send message to Service Bus
```

### 2. Azure Service Bus
- **Topic:** `invoice-posted`
- **Subscription:** `invoice-processor` with filter
- **DLQ:** Dead letter queue for failed messages
- **Retry:** 3 attempts with exponential backoff

### 3. Azure Functions (.NET 8)

#### InvoiceReceiver
- **Trigger:** Service Bus message
- **Action:** Transform & validate invoice
- **Output:** HTTP call to external API

#### InvoiceRetryHandler
- **Trigger:** DLQ message (manual or scheduled)
- **Action:** Retry failed invoices

#### HealthMonitor
- **Trigger:** Timer (every 5 minutes)
- **Action:** Check integration health, alert on issues

### 4. External API Integration

Supports:
- Xero (OAuth2)
- QuickBooks (OAuth2)
- Custom REST APIs (API Key or OAuth2)

## Data Flow

```
D365 Invoice Posted
        │
        ▼
Dataverse Plugin
        │
        ▼
Service Bus Topic
        │
        ▼
Azure Function (Process)
        │
        ├── Success → External API
        │
        └── Failure → Retry (3x)
                    │
                    └── Still Fails → DLQ
                                        │
                                        └── Manual Review
```

## Security

| Layer | Implementation |
|-------|---------------|
| Authentication | Managed Identity |
| API Security | OAuth2 via APIM |
| Secrets | Azure Key Vault |
| Network | Private endpoints (optional) |

## Monitoring

- **Application Insights:** Distributed tracing
- **Custom Metrics:** Success rate, latency, DLQ count
- **Alerts:** Failed invoice threshold, DLQ messages
- **Dashboard:** PowerBI or Azure Dashboard

## Error Handling

### Retry Strategy
- Immediate retry: 0 delay
- Second retry: 30 seconds
- Third retry: 2 minutes
- DLQ: After 3 failures

### DLQ Processing
- Manual review interface
- Bulk retry capability
- Failure reason logging

## Scalability

- **Service Bus:** Premium tier supports 1M+ messages/day
- **Functions:** Consumption plan auto-scales
- **API Management:** Built-in rate limiting

## Cost Estimate (Monthly)

| Component | Cost |
|-----------|------|
| Service Bus (Standard) | $10-20 |
| Functions (Consumption) | $5-15 |
| App Insights | $5-10 |
| Key Vault | $0.03 |
| **Total** | **$20-50/month** |

## Deployment

See [setup.md](setup.md) for step-by-step deployment instructions.
