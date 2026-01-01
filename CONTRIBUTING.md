# Contributing to NIS2 .NET Shield

Thank you for your interest in contributing to NIS2 .NET Shield! 🛡️

This project is open source and welcomes contributions from the community. Whether you're a .NET developer, a security expert, or simply a user with feedback, your contribution is valuable.

## How to Contribute

### 🐛 Reporting Bugs

1. Check if the bug has already been reported in [Issues](../../issues)
2. If not, open a new issue using the "Bug Report" template
3. Include: .NET version, OS, steps to reproduce

### 💡 Proposing New Features

1. Open an issue with the "Feature Request" template
2. Describe the use case and value for NIS2 compliance
3. Wait for feedback before starting implementation

### 🔧 Submitting Pull Requests

1. **Fork** the repository
2. Create a branch: `git checkout -b feature/feature-name`
3. Write tests for new features
4. Make sure all tests pass:
   ```bash
   dotnet test
   ```
5. Open a Pull Request with a clear description

## Development Environment Setup

```bash
# Clone
git clone https://github.com/nis2shield/dotnet-nis2-shield.git
cd dotnet-nis2-shield

# Restore dependencies
dotnet restore

# Build
dotnet build

# Run tests
dotnet test --verbosity normal
```

## Project Structure

```
src/
└── Nis2Shield.AspNetCore/
    ├── ActiveDefense/       # Rate limiting, Tor blocking, Session Guard
    ├── Configuration/       # Options and settings
    ├── Logging/             # Forensic logging
    ├── Notifications/       # Webhook notifications
    ├── Siem/                # SIEM connectors
    └── Nis2Middleware.cs    # Core middleware
tests/
└── Nis2Shield.Tests/        # Unit and integration tests
```

## Code Style

- Follow [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use `dotnet format` for formatting
- Use nullable reference types
- Document public APIs with XML comments

## Areas Where Contributions Are Needed

| Area | Required Skills | Priority |
|------|-----------------|----------|
| Code security review | Cybersecurity | 🔴 High |
| New SIEM connectors | Splunk, QRadar, Graylog | 🟡 Medium |
| Penetration testing | Pentesting | 🔴 High |
| Documentation | English | 🟢 Low |
| Compliance checks | NIS2, GDPR | 🔴 High |

## Questions?

Open an issue with the `question` tag or contact the maintainers.

Thank you! 🙏
