# Registratr

A lightweight, convention-based dependency injection library for .NET 8+. Automatically registers your services into `IServiceCollection` using marker interfaces, lifetime attributes, or open generics.

## Features

- **Marker Interfaces**: Register dependencies by implementing `ITransientService`, `IScopedService`, or `ISingletonService`.
- **Lifetime Attributes**: Tag classes or interfaces with `[TransientService]`, `[ScopedService]`, or `[SingletonService]` without polluting your interface hierarchy.
- **Open Generics Support**: Automatically maps and registers open generic definitions (e.g., `IRepository<T>` mapped to `Repository<T>`).
- **Clean Interface Resolution**: Automatically extracts implemented business interfaces across assemblies while filtering out system interfaces (`IDisposable`, `IAsyncDisposable`, `IComparable`, etc.).
- **Safe Assembly Scanning**: Handles `ReflectionTypeLoadException` safely and ignores abstract or static classes.
- **Ignore Attribute**: Exclude specific classes or interfaces from auto-registration using `[IgnoreRegistration]`.
- **Conflict Prevention**: Detects conflicting lifetime declarations early with clear exceptions.
- **Flexible Options**: Configure duplicate handling (`Append` or `TryAdd`), registering as self (`RegisterAsSelf`), and custom type filters (`TypeFilter`).

---

## Getting Started

### 1. Marker Interfaces

Implement a marker interface on either your business interface or implementation class:

```csharp
using Registratr.Abstractions;

// Scoped service via interface
public interface IOrderService : IScopedService
{
    Task ProcessOrderAsync(int orderId);
}

public class OrderService : IOrderService
{
    public async Task ProcessOrderAsync(int orderId) { /* ... */ }
}

// Singleton concrete service without interface
public class MemoryCacheService : ISingletonService
{
    // ...
}
```

### 2. Lifetime Attributes

Alternatively, use attributes on classes or interfaces:

```csharp
using Registratr.Abstractions;

[ScopedService]
public class CustomerService : ICustomerService
{
    // ...
}

[SingletonService]
public interface IApplicationConfig
{
    // ...
}
```

### 3. Open Generics

Open generic definitions are registered automatically:

```csharp
public interface IRepository<T> : IScopedService { }

public class Repository<T> : IRepository<T> { }

// Resolved as:
// services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
```

### 4. Registration in `Program.cs`

Register services using assemblies or marker types:

```csharp
using Registratr;

var builder = WebApplication.CreateBuilder(args);

// Register by marker type:
builder.Services.AddRegistratr(typeof(Program));

// Or register by assembly with options:
builder.Services.AddRegistratr(
    new[] { typeof(Program).Assembly },
    options =>
    {
        options.DuplicateStrategy = DuplicateStrategy.TryAdd;
        options.RegisterAsSelf = true;
        options.TypeFilter = type => type.Namespace?.StartsWith("MyApp.Services") == true;
    });
```

---

## Configuration Options

| Option | Type | Default | Description |
|---|---|---|---|
| `DuplicateStrategy` | `DuplicateStrategy` | `Append` | `Append` (standard `IServiceCollection` behavior) or `TryAdd` (skips if already registered). |
| `RegisterAsSelf` | `bool` | `false` | When `true`, concrete classes that implement interfaces are registered as both the interface and the concrete type. |
| `IncludeOpenGenerics` | `bool` | `true` | When `true`, open generic definitions (e.g. `Repository<>`) are automatically discovered and registered. |
| `TypeFilter` | `Func<Type, bool>?` | `null` | Optional predicate to filter types discovered during assembly scanning. |
