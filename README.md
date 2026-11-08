![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat&logo=dotnet&logoColor=white)
![C# 13](https://img.shields.io/badge/C%23-13-239120?style=flat&logo=csharp&logoColor=white)
![Spectre.Console](https://img.shields.io/badge/UI-Spectre.Console-blue?style=flat)
![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)

**IntelScout** is a lightweight, asynchronous C# threat intelligence CLI engine targeting **.NET 10**. Designed for rapid incident response and network telemetry, it interfaces directly with the AbuseIPDB v2 REST API to evaluate IP indicators of compromise in real time.

It supports both terminal queries and batch text-file target lists, using `Spectre.Console` to render execution spinners and styled console reporting.

---

## Technical Highlights

- **Asynchronous Execution & Socket Management:** Utilizes a single static `HttpClient` pattern to prevent socket exhaustion during high-volume batch processing.
- **Dual Target Parsing:** Flexibly accepts inline command-line arguments or bulk target files using **LINQ** pipelines for inline whitespace scrubbing and null-entry filtering.
- **Fault-Tolerant API Engine:** Implements structured exception handling with fallback state rendering (`Connection Failed / ERR`) so individual query timeouts don't crash long-running batch checks.
- **Dynamic Terminal Telemetry:** Features live execution spinners, custom ASCII FIGlet titles, and automatic risk-based color formatting (Green: Low Risk, Yellow: Moderate Risk, Red: High Abuse Score).

---

## System Architecture & Data Flow

```mermaid
flowchart TD
    subgraph Input Layer
        A[CLI Arguments] --> D{Input Type?}
        B[Target File --file] --> D
        C[Default Sample IPs] --> D
    end

    subgraph Core Orchestrator (Program.cs)
        D -->|Valid Targets| E[Execution Engine]
    end

    subgraph Service Layer (AbuseIpDbService)
        E -->|Async GET Request + API Key| F[AbuseIPDB v2 REST API]
        F -->|JSON Payload| G[JsonDocument Parser]
        G -->|Mapped POCO| H[IpResult Object]
    end

    subgraph Presentation Layer (ConsoleRenderer)
        H -->|Risk Score Evaluation| I{Score Range?}
        I -->|"> 50%"| J[Bold Red Markup]
        I -->|"< 15%"| K[Yellow Markup]
        I -->|"Default"| L[Green Markup]
        J & K & L --> M[Spectre.Console Table]
        M --> N[Rendered Terminal Output]
    end
