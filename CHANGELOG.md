# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Session Guard for session hijacking detection
- Multi-SIEM Connectors (Splunk, Datadog, Elasticsearch)
- Webhook Notifications (Slack, Teams, Discord)
- Demo application

## [0.1.0] - 2026-01-01

### Added
- **Forensic Logger**: Structured JSON logging with HMAC-SHA256 signing
  - IP anonymization for GDPR compliance
  - PII encryption support
  - Integrity hash on each log entry
- **Rate Limiter**: In-memory sliding window rate limiting
  - Configurable threshold and window size
  - Per-IP tracking
- **Tor Blocker**: Automatic blocking of Tor exit nodes
  - Cached list with 6-hour refresh
  - Async update mechanism
- **Configuration**: Strongly-typed options via `appsettings.json`
  - `Nis2Options` with logging and active defense sections
  - Support for environment variables and secrets
- **Middleware Pipeline**: ASP.NET Core middleware integration
  - `AddNis2Shield()` extension method
  - `UseNis2Shield()` middleware registration
- **Testing**: Unit and integration tests
- **CI/CD**: NuGet publish workflow

[Unreleased]: https://github.com/nis2shield/dotnet-nis2-shield/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/nis2shield/dotnet-nis2-shield/releases/tag/v0.1.0
