# Copilot Instructions for MaIN.NET

## Build, Test, and Lint

```bash
# Build the entire solution
dotnet build MaIN.sln

# Run unit tests only (excludes integration tests, which need real LLM backends)
dotnet test --filter FullyQualifiedName\!~IntegrationTests

# Run a single test by name
dotnet test --filter "FullyQualifiedName~ChatContextTests.ChatContext_Complete_ShouldCallServiceComplete"

# Run all tests in one test class
dotnet test --filter "FullyQualifiedName~ChatContextTests"
```

The CI pipeline (`publish.yml`) runs: restore → build → test (unit only) → pack → NuGet publish. Integration tests are excluded in CI because they require running LLM backends.

## Architecture

MaIN (Modular Artificial Intelligence Network) is a .NET library (NuGet: `MaIN.NET`) for integrating LLMs into applications. It targets `net8.0` and `net10.0`.

### Layer Structure

```
MaIN.Core          → Public API surface (AIHub, contexts, SignalR hub)
  ↓
MaIN.Services      → Business logic (services, factories, command handlers, mappers)
  ↓
MaIN.Domain        → Entities, models, configuration, exceptions
  ↓
MaIN.Infrastructure → Data persistence (repositories for MongoDB, SQLite, FileSystem, SQL, in-memory)
```

Each layer has a `Bootstrapper.cs` that registers its services via `IServiceCollection` extension methods. The chain is: `Core.AddMaIN()` → `Services.ConfigureMaIN()` → `Infrastructure` repository registration.

### Public API: The Hub Pattern

All consumer-facing functionality goes through `AIHub`, a static singleton providing fluent context builders:

```csharp
AIHub.Chat()   // ChatContext   – conversations with LLMs
AIHub.Agent()  // AgentContext  – autonomous agents with behaviours and steps
AIHub.Flow()   // FlowContext   – multi-agent pipelines (loaded from ZIP files)
AIHub.Model()  // ModelContext  – model management
AIHub.Mcp()    // McpContext    – Model Context Protocol operations
```

Contexts use a fluent `With*()` builder pattern, terminating with `CompleteAsync()`, `Create()`, `ProcessAsync()`, etc.

### Three Core Concepts

- **Chat**: A conversation with messages, model reference, inference/memory parameters, and tool configuration.
- **Agent**: An autonomous entity with named behaviours, ordered processing steps (`START`, `ANSWER`, `REDIRECT`, `FETCH_DATA`, `MCP`), and optional RAG knowledge sources.
- **Flow**: A pipeline of ordered agents loaded from ZIP archives containing JSON agent definitions. First agent processes input and passes output to the next.

### Command Dispatch (CQRS-like)

Agent step execution uses `ICommandDispatcher` which routes named commands (`StartCommand`, `RedirectCommand`, `FetchCommand`, `AnswerCommand`, `McpCommand`) to their `ICommandHandler<TCommand, TResult>` implementations.

### Backend Abstraction

`ILLMServiceFactory` creates the appropriate LLM service at runtime based on `BackendType` (OpenAI, Gemini, DeepSeek, GroqCloud, Anthropic, Xai, Ollama, or Self for local LLamaSharp inference).

## Key Conventions

### C# Style
- **Primary constructors** for dependency injection in services:
  ```csharp
  public class ChatService(IChatRepository chatProvider, ILLMServiceFactory llmServiceFactory, MaINSettings settings) : IChatService
  ```
- **Records** for DTOs with `[JsonPropertyName]` attributes for serialization.
- **Nullable reference types** enabled project-wide.
- **ImplicitUsings** enabled; xUnit has a global `<Using Include="Xunit"/>`.

### Mapping Layer
Three-level static extension method mappers in `MaIN.Services.Mappers`:
- `Domain ↔ DTO` (API serialization)
- `Domain ↔ Document` (persistence)

Example: `ChatMapper.ToDto()`, `ChatMapper.ToDomain()`, `ChatMapper.ToDocument()`.

### Repository Pattern
`IChatRepository`, `IAgentRepository`, `IAgentFlowRepository` each have multiple implementations (Default/in-memory, MongoDB, FileSystem, SQLite, SQL). The active implementation is auto-selected based on which settings section is populated in `MaINSettings`.

### DI Registration
- Services, factories, handlers, and the command dispatcher are registered as **Singletons**.
- LLM service implementations, image gen, and TTS services are registered as **Transient**.
- All registration happens in `Bootstrapper.cs` files using `IServiceCollection` extension methods.

### Testing
- **xUnit** with **Moq** for unit tests in `src/MaIN.Core.UnitTests/`.
- Integration tests in `MaIN.Core.IntegrationTests/` (require real backends, excluded from CI).
- Unit tests mock service interfaces and test Hub context fluent builders.

### Configuration
Strongly-typed `MaINSettings` bound from the `"MaIN"` section of `appsettings.json`. Storage backend is determined by which sub-settings object (`MongoDbSettings`, `FileSystemSettings`, `SqliteSettings`, `SqlSettings`) is non-null.

### Ancillary Components
- **ImageGen**: A Python microservice (`ImageGen/`) for image generation, with its own Dockerfile and `requirements.txt`.
- **Frontend**: A separate frontend project in `Frontend/MainFE/`.
- **Scripts**: PowerShell/Bash scripts in `scripts/` for model downloading, API startup, and CLI management.
