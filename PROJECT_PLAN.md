# NIS2 .NET Shield - Project Plan

**Target Framework**: .NET 8 (LTS)
**Package Name**: `Nis2Shield.AspNetCore`
**Repository**: `nis2shield/dotnet-nis2-shield`

## 1. Vision
Portare la compliance NIS2 nell'ecosistema Microsoft Enterprise, integrandosi nativamente come Middleware nel pipeline di ASP.NET Core. Deve supportare l'ecosistema esistente (Dependency Injection, `appsettings.json`, `ILogger`).

## 2. Core Features (MVP)

### A. Middleware Pipeline (`Nis2Middleware`)
- Intercettazione Request/Response.
- **Forensic Logging**: Serializzazione log in JSON strutturato.
- **HMAC Signing**: Firma di integrità su ogni log (Art. 21.2.h).
- **PII Encryption**: Integrazione con `IDataProtectionProvider` per cifrare campi sensibili (Email, UserID).

### B. Configuration (`appsettings.json`)
- Binding fortemente tipizzato su classe `Nis2Options`.
- Supporto per Secret Manager (UserSecrets/KeyVault) per le chiavi.

### C. Active Defense
- **Rate Limiting**: Wrapper o estensione del RateLimiter nativo di .NET 8.
- **Block List**: Blocco IP Tor/Malicious (gestione in-memory o DistributedCache).

### D. Health Checks
- Integrazione con `Microsoft.Extensions.Diagnostics.HealthChecks` per esporre lo stato su `/health/nis2`.

## 3. Architecture

Sfrutteremo i costrutti nativi di .NET 8 per massimizzare le performance e l'adozione:

- **Logging**: Non reinventiamo la ruota, scriviamo su `ILogger` ma formattiamo il payload JSON prima di inviarlo. In alternativa, un Custom Logger Provider.
- **Encryption**: Usiamo le API native di Windows/Linux `DataProtection` per la gestione chiavi, oppure AES standard se vogliamo compatibilità totale cross-platform senza dipendenze esterne.

## 4. Roadmap

### Phase 1: Core & Logging
- [ ] Setup Solution (`sln`) e Project (`csproj`).
- [ ] Implementazione `Nis2Middleware` di base.
- [ ] Configurazione Options Pattern (`services.Configure<Nis2Options>`).
- [ ] JSON Structured Logging con HMAC.

### Phase 2: Active Defense
- [ ] Integrazione Rate Limiting policy.
- [ ] IP Blocking Middleware.
- [ ] Cifratura PII.

### Phase 3: NuGet & Distribution
- [ ] CD Pipeline (`dotnet pack`).
- [ ] Pubblicazione su NuGet.org.
- [ ] README e Esempi (`Program.cs`).

## 5. Ecosystem Integration
- Deve produrre log identici a Django/Spring per essere digerito dalla stessa infrastruttura (`fluent-bit`).