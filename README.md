# NIS2 .NET Shield

[![NuGet](https://img.shields.io/nuget/v/Nis2Shield.AspNetCore.svg)](https://www.nuget.org/packages/Nis2Shield.AspNetCore/)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4.svg)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Build](https://img.shields.io/github/actions/workflow/status/nis2shield/dotnet-nis2-shield/test.yml?branch=main)](https://github.com/nis2shield/dotnet-nis2-shield/actions)

### 🛡️ Security-First Middleware for ASP.NET Core NIS2 Compliance

Companies subject to NIS2 Directive need **demonstrable compliance**. This middleware provides:

1.  **Forensic logging** with HMAC-SHA256 integrity and PII encryption (Art. 21.2.h)
2.  **Rate limiting** to prevent DoS/Brute Force attacks (Art. 21.2.e)
3.  **Session Guard** to detect hijacking via IP/User-Agent validation (Art. 21.2.a)
4.  **Multi-SIEM Presets**: Native connectors for Splunk, Datadog, Elasticsearch

> **Part of the NIS2 Shield Ecosystem**: Use with [`@nis2shield/react-guard`](https://github.com/nis2shield/react-guard) for client-side protection and [`nis2shield/infrastructure`](https://github.com/nis2shield/infrastructure) for a full-stack, audited implementation.

```
┌─────────────────────────────────────────────────────────────┐
│                        Frontend                              │
│  @nis2shield/react-guard                                    │
│  ├── SessionWatchdog (idle detection)                       │
│  ├── AuditBoundary (crash reports)                         │
│  └── → POST /api/nis2/telemetry/                           │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                  Backend (NIS2 Adapter)                      │
│  **Nis2Shield.AspNetCore**                                  │
│  ├── ForensicLogger (HMAC signed logs)                     │
│  ├── RateLimiter, SessionGuard, TorBlocker                 │
│  └── → SIEM (Elasticsearch, Splunk, Datadog)               │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                    Infrastructure                            │
│  nis2shield/infrastructure                                  │
│  ├── Centralized Logging (ELK/Splunk)                       │
│  └── Audited Deployment (Terraform/Helm)                    │
└─────────────────────────────────────────────────────────────┘
```

## 📦 Installation

```bash
dotnet add package Nis2Shield.AspNetCore
```

## ⚙️ Quick Start

### Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

// 1. Register NIS2 Shield
builder.Services.AddNis2Shield(builder.Configuration);

var app = builder.Build();

// 2. Activate Middleware (before Auth)
app.UseNis2Shield();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

### appsettings.json

```json
{
  "Nis2": {
    "Enabled": true,
    "IntegrityKey": "your-hmac-secret-key",
    "Logging": {
      "Enabled": true,
      "AnonymizeIp": true,
      "EncryptPii": true
    },
    "ActiveDefense": {
      "RateLimitEnabled": true,
      "RateLimitThreshold": 100,
      "RateLimitWindowSeconds": 60,
      "BlockTorExitNodes": true
    },
    "SessionGuard": {
      "Enabled": true,
      "SubnetTolerance": 24,
      "AllowUserAgentChange": false
    }
  }
}
```

## 🔒 Features

### Forensic Logging
- Structured JSON logs with HMAC-SHA256 integrity signature
- Automatic PII field encryption (GDPR compliant)
- IP anonymization for privacy compliance

### Active Defense
- **Rate Limiting**: Sliding window protection against application-level DoS attacks
- **Session Guard**: Session hijacking prevention via IP/User-Agent fingerprinting
- **Tor Blocker**: Automatic blocking of Tor exit nodes

### Multi-SIEM Integration
- **Elasticsearch**: Bulk API with daily index rotation
- **Splunk**: HTTP Event Collector (HEC) support
- **Datadog**: Logs API v2 integration

### Webhook Notifications
- Real-time alerts to **Slack**, **Microsoft Teams**, **Discord**
- Configurable event filtering

## 📖 Recipes

### Banking App with Strict Security

```csharp
builder.Services.AddNis2Shield(options =>
{
    options.IntegrityKey = Environment.GetEnvironmentVariable("NIS2_HMAC_KEY")!;
    
    // Rate Limiting
    options.ActiveDefense.RateLimitEnabled = true;
    options.ActiveDefense.RateLimitThreshold = 50;
    options.ActiveDefense.RateLimitWindowSeconds = 60;
    
    // Session Guard - strict mode
    options.SessionGuard.Enabled = true;
    options.SessionGuard.SubnetTolerance = 32; // exact IP match
    options.SessionGuard.AllowUserAgentChange = false;
});
```

### Healthcare API with Full PII Protection

```csharp
builder.Services.AddNis2Shield(options =>
{
    options.IntegrityKey = Environment.GetEnvironmentVariable("NIS2_HMAC_KEY")!;
    options.Logging.EncryptPii = true;
    options.Logging.AnonymizeIp = true;
    options.Logging.PiiFields = new List<string> { "email", "patient_id", "ssn" };
});
```

### Enterprise with Splunk HEC

```json
{
  "Nis2": {
    "IntegrityKey": "your-hmac-key",
    "Siem": {
      "Enabled": true,
      "Provider": "Splunk",
      "Endpoint": "https://splunk.company.com:8088/services/collector",
      "ApiKey": "your-hec-token",
      "IndexName": "nis2-security"
    }
  }
}
```

### Slack Alert for Security Events

```json
{
  "Nis2": {
    "Webhooks": {
      "Enabled": true,
      "Targets": [
        {
          "Name": "Security Alerts",
          "Url": "https://hooks.slack.com/services/xxx/yyy/zzz",
          "Provider": "Slack",
          "Events": ["rate_limit_exceeded", "tor_node_blocked", "session_hijack_detected"]
        }
      ]
    }
  }
}
```

## 🧪 Testing

```bash
dotnet test
```

## 📄 License

MIT License - see [LICENSE](LICENSE) for details.

## 🤝 Related Projects

| Project | Technology | Package |
|---------|------------|---------|
| [django-nis2-shield](https://github.com/nis2shield/django-nis2-shield) | Django | `pip install django-nis2-shield` |
| [nis2-spring-shield](https://github.com/nis2shield/nis2-spring-shield) | Spring Boot | Maven Central |
| [@nis2shield/express-middleware](https://github.com/nis2shield/express-nis2-middleware) | Express | `npm install @nis2shield/express-middleware` |
| [@nis2shield/react-guard](https://github.com/nis2shield/react-guard) | React | `npm install @nis2shield/react-guard` |

---

**[Documentation](https://nis2shield.com/dotnet-shield/)** · **[NuGet](https://www.nuget.org/packages/Nis2Shield.AspNetCore/)** · **[Changelog](CHANGELOG.md)**
