# NIS2 .NET Shield - Project Plan

**Target Framework**: .NET 8 (LTS)
**Package Name**: `Nis2Shield.AspNetCore`
**Repository**: `nis2shield/dotnet-nis2-shield`

## 1. Vision
Portare la compliance NIS2 nell'ecosistema Microsoft Enterprise, integrandosi nativamente come Middleware nel pipeline di ASP.NET Core. Deve supportare l'ecosistema esistente (Dependency Injection, `appsettings.json`, `ILogger`).

## 2. Core Features

### A. Middleware Pipeline (`Nis2Middleware`) ✅
- Intercettazione Request/Response.
- **Forensic Logging**: Serializzazione log in JSON strutturato.
- **HMAC Signing**: Firma di integrità su ogni log (Art. 21.2.h).
- **PII Encryption**: Integrazione con opzioni per cifrare campi sensibili (Email, UserID).

### B. Configuration (`appsettings.json`) ✅
- Binding fortemente tipizzato su classe `Nis2Options`.
- Supporto per Secret Manager (UserSecrets/KeyVault) per le chiavi.

### C. Active Defense ✅
- **Rate Limiting**: Sliding Window Rate Limiter in-memory.
- **Block List**: Blocco IP Tor exit nodes con aggiornamento automatico.
- **Session Guard**: Rilevamento session hijacking via IP/User-Agent.

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

Sfruttiamo i costrutti nativi di .NET 8 per massimizzare le performance e l'adozione:

- **Logging**: Scriviamo su `ILogger` con payload JSON firmato HMAC.
- **DI**: Tutti i servizi registrati nel container via extension methods.
- **HttpClient**: Factory pattern per SIEM e Webhook connectors.

## 4. Roadmap

### Phase 1: Core & Logging ✅ COMPLETE
- [x] Setup Solution (`sln`) e Project (`csproj`).
- [x] Implementazione `Nis2Middleware` di base.
- [x] Configurazione Options Pattern (`services.Configure<Nis2Options>`).
- [x] JSON Structured Logging con HMAC.

### Phase 2: Active Defense ✅ COMPLETE
- [x] Integrazione Rate Limiting policy (Sliding Window).
- [x] IP Blocking Middleware (Tor Exit Nodes).
- [x] Session Guard per rilevamento hijacking.

### Phase 3: Multi-SIEM & Notifications ✅ COMPLETE
- [x] Elasticsearch Connector.
- [x] Splunk HEC Connector.
- [x] Datadog Logs Connector.
- [x] Webhook Notifier (Slack/Teams/Discord).

### Phase 4: NuGet & Distribution ✅ COMPLETE
- [x] CD Pipeline (`dotnet pack`).
- [x] Pubblicazione su NuGet.org.
- [x] README e Esempi (`Program.cs`).
- [x] Demo App funzionante.

### Phase 5: Community & Documentation ✅ COMPLETE
- [x] CHANGELOG.md
- [x] CODE_OF_CONDUCT.md
- [x] CONTRIBUTING.md
- [x] SECURITY.md
- [x] GitHub Issue Templates
- [x] CI Workflow (test.yml)

## 5. Ecosystem Integration
- Deve produrre log identici a Django/Spring per essere digerito dalla stessa infrastruttura (`fluent-bit`).

---

## 🚀 v0.2.0 - RELEASED (1 Gennaio 2026)

Il progetto **Nis2Shield.AspNetCore v0.2.0** include tutte le features di Active Defense, Multi-SIEM e Webhooks!

```bash
dotnet add package Nis2Shield.AspNetCore --version 0.2.0
```