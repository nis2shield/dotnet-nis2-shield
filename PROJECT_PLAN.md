# NIS2 .NET Shield - Project Plan

**Target Framework**: .NET 8 (LTS)
**Package Name**: `Nis2Shield.AspNetCore`
**Repository**: `nis2shield/dotnet-nis2-shield`

## 1. Vision
Bring NIS2 compliance to the Microsoft Enterprise ecosystem by integrating natively as Middleware in the ASP.NET Core pipeline. It must support the existing ecosystem (Dependency Injection, `appsettings.json`, `ILogger`).

## 2. Core Features

### A. Middleware Pipeline (`Nis2Middleware`) ✅
- Request/Response interception.
- **Forensic Logging**: Log serialization in structured JSON.
- **HMAC Signing**: Integrity signature on every log (Art. 21.2.h).
- **PII Encryption**: Integration with options to encrypt sensitive fields (Email, UserID).

### B. Configuration (`appsettings.json`) ✅
- Strongly typed binding on `Nis2Options` class.
- Support for Secret Manager (UserSecrets/KeyVault) for keys.

### C. Active Defense ✅
- **Rate Limiting**: In-memory Sliding Window Rate Limiter.
- **Block List**: Tor exit nodes IP blocking with automatic updates.
- **Session Guard**: Session hijacking detection via IP/User-Agent.

### D. Multi-SIEM Integration ✅
- Elasticsearch (Bulk API)
- Splunk (HEC)
- Datadog (Logs API v2)

### E. Webhook Notifications ✅
- Slack
- Microsoft Teams
- Discord
- Generic HTTP

## 3. Architecture

We leverage native .NET 8 constructs to maximize performance and adoption:

- **Logging**: Write to `ILogger` with HMAC-signed JSON payload.
- **DI**: All services registered in the container via extension methods.
- **HttpClient**: Factory pattern for SIEM and Webhook connectors.

## 4. Roadmap

### Phase 1: Core & Logging ✅ COMPLETE
- [x] Setup Solution (`sln`) and Project (`csproj`).
- [x] Basic `Nis2Middleware` implementation.
- [x] Options Pattern Configuration (`services.Configure<Nis2Options>`).
- [x] JSON Structured Logging with HMAC.

### Phase 2: Active Defense ✅ COMPLETE
- [x] Rate Limiting policy integration (Sliding Window).
- [x] IP Blocking Middleware (Tor Exit Nodes).
- [x] Session Guard for hijacking detection.

### Phase 3: Multi-SIEM & Notifications ✅ COMPLETE
- [x] Elasticsearch Connector.
- [x] Splunk HEC Connector.
- [x] Datadog Logs Connector.
- [x] Webhook Notifier (Slack/Teams/Discord).

### Phase 4: NuGet & Distribution ✅ COMPLETE
- [x] CD Pipeline (`dotnet pack`).
- [x] Publication on NuGet.org.
- [x] README and Examples (`Program.cs`).
- [x] Working Demo App.

### Phase 5: Community & Documentation ✅ COMPLETE
- [x] CHANGELOG.md
- [x] CODE_OF_CONDUCT.md
- [x] CONTRIBUTING.md
- [x] SECURITY.md
- [x] GitHub Issue Templates
- [x] CI Workflow (test.yml)

## 5. Ecosystem Integration
- Must produce logs identical to Django/Spring to be digested by the same infrastructure (`fluent-bit`).

---

## 🚀 v0.2.0 - RELEASED (January 1, 2026)

The **Nis2Shield.AspNetCore v0.2.0** project includes all Active Defense, Multi-SIEM, and Webhooks features!

```bash
dotnet add package Nis2Shield.AspNetCore --version 0.2.0
```