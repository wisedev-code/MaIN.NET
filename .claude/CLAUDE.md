# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

MaIN.NET (Modular Artificial Intelligence Network) is a .NET library for integrating LLMs into AI workflows. It supports local models (via LLamaSharp) and cloud providers (OpenAI, Gemini, DeepSeek, Anthropic, GroqCloud, xAI, Ollama). Published as the `MaIN.NET` NuGet package.

## Build & Test Commands

```bash
# Build the entire solution
dotnet build MaIN.sln

# Build in Release mode
dotnet build MaIN.sln --configuration Release

# Run all unit tests
dotnet test src/MaIN.Core.UnitTests/MaIN.Core.UnitTests.csproj

# Run a single test by name
dotnet test src/MaIN.Core.UnitTests/MaIN.Core.UnitTests.csproj --filter "FullyQualifiedName~TestMethodName"

# Run integration tests (requires local model/API setup)
dotnet test MaIN.Core.IntegrationTests/MaIN.Core.IntegrationTests.csproj

# CI runs: build Release + unit tests only (integration tests excluded)
```

**Target frameworks:** net8.0 and net10.0 (dual-targeting). Tests also target both.

## Architecture

### Layered Structure

```
MaIN.Core → MaIN.Services → MaIN.Domain
                           → MaIN.Infrastructure
```

- **MaIN.Core** — Public API surface. Contains `AIHub` (static facade) and fluent context builders (`ChatContext`, `AgentContext`, `FlowContext`, `ModelContext`, `McpContext`). Entry point for all consumers.
- **MaIN.Services** — Business logic. Service implementations, LLM integrations, agent step processing, command dispatch. DI registration lives in `Services/Bootstrapper.cs`.
- **MaIN.Domain** — Entities (`Chat`, `Message`, `Agent`, `AgentFlow`), model abstractions (`AIModel`, `LocalModel`, `CloudModel`), configuration (`MaINSettings`), and custom exceptions.
- **MaIN.Infrastructure** — Repository implementations for pluggable storage backends: MongoDB, FileSystem, SQLite, SQL Server, and in-memory (default). DI registration in `Infrastructure/Bootstrapper.cs`.

### Key Design Patterns

**Fluent Builder API** — The primary public interface. Each context (`ChatContext`, `AgentContext`, etc.) implements a chain of interfaces to enforce a build order:
```csharp
await AIHub.Chat()
    .WithModel<Gemma2_2b>()
    .WithMessage("Hello")
    .CompleteAsync();
```

**Static Facade** — `AIHub` is the single entry point. Initialized once via DI (`services.AddMaIN(configuration)` → `serviceProvider.UseMaIN()`), then used statically throughout the application.

**Model Registry** — `ModelRegistry` is a concurrent dictionary auto-populated via reflection at startup. Models are abstract records (`AIModel` → `LocalModel` / `CloudModel`) with marker interfaces (`IReasoningModel`, `IVisionModel`). Predefined models live in `Domain/Models/Concrete/`.

**Command Dispatch** — Agent step execution uses `ICommandDispatcher` to route named commands (START, REDIRECT, FETCH_DATA, ANSWER, MCP) to `ICommandHandler<TCommand, TResult>` implementations.

**Step Processing** — Agent workflows are driven by `IStepProcessor` which delegates to `IStepHandler` implementations (`RedirectStepHandler`, `FetchDataStepHandler`, `AnswerStepHandler`, `McpStepHandler`, `BecomeStepHandler`, `CleanupStepHandler`).

### DI Bootstrapping Chain

```csharp
services.AddMaIN(configuration)     // MaIN.Services.Bootstrapper — registers services
    → ConfigureMaIN()               // Core services (singletons) + HTTP clients
    → ConfigureInfrastructure()     // MaIN.Infrastructure.Bootstrapper — repos by BackendType
serviceProvider.UseMaIN()           // MaIN.Core.Bootstrapper — calls AIHub.Initialize()
```

### Backend Types

`BackendType` enum selects the LLM provider: `Self` (local LLamaSharp), `OpenAi`, `Gemini`, `DeepSeek`, `GroqCloud`, `Anthropic`, `Xai`, `Ollama`. Set via `MaINSettings` or per-chat via `.WithBackend()`.

### Testing

- **Unit tests** (`src/MaIN.Core.UnitTests/`): xUnit + Moq. Tests for `ChatContext`, `AgentContext`, `FlowContext`. Use `InternalsVisibleTo` for internal access.
- **Integration tests** (`MaIN.Core.IntegrationTests/`): xUnit with `IntegrationTestBase`. Require model/API access. Excluded from CI.

## Commit Message Convention

```
<type>: <description>
<type>(scope): <description>
```

Types: `feat`, `fix`, `refactor`, `tests`, `cleanup`, `chore`

## Solution Projects

| Project | Role |
|---|---|
| `src/MaIN.Core` | Public API, AIHub, contexts |
| `src/MaIN.Services` | Business logic, LLM services |
| `src/MaIN.Domain` | Entities, models, config, exceptions |
| `src/MaIN.Infrastructure` | Data repositories (Mongo/FS/SQLite/SQL) |
| `src/MaIN.Core.UnitTests` | Unit tests |
| `MaIN.Core.IntegrationTests` | Integration tests |
| `Examples/Examples` | 40+ interactive examples |
| `Examples/Examples.SimpleConsole` | Minimal console example |
| `src/MaIN.InferPage` | Web UI for inference |
