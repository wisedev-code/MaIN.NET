# Code Style Guidelines

## Language & Framework

- C# 12+ with `ImplicitUsings` and `Nullable` enabled
- Target frameworks: net8.0 and net10.0

## Conventions

- Use primary constructors for dependency injection in services
- Use `record` types for immutable data structures (models, DTOs)
- Use `sealed` on service implementations (e.g., `sealed class ChatService`)
- Use `is null` / `is not null` instead of `== null` / `!= null`
- Use `async/await` for all I/O-bound operations with `Async` suffix on method names
- Use `ArgumentNullException.ThrowIfNull()` for parameter validation
- Use file-scoped namespaces (`namespace Foo;`)
- Use collection expressions (`[]` and `[.. items]`) where appropriate

## Architecture Rules

- Public API surface lives only in `MaIN.Core` — services and domain types are internal
- Fluent builder interfaces enforce method call order (e.g., `IChatBuilderEntryPoint` → `IChatMessageBuilder` → `IChatConfigurationBuilder`)
- Services registered as singletons; LLM/ImageGen/TTS services as transient
- HTTP clients registered via `IHttpClientFactory` with named clients
- Repository pattern for data access — backend selected by `BackendType` configuration
- Custom exceptions over generic `Exception` — throw domain-specific types from `MaIN.Domain.Exceptions`

## SOLID Principles

- Follow SOLID principles in class and interface design
- Use dependency injection for loose coupling
- Program against interfaces, not implementations
