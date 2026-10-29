using Registratr.Abstractions;

namespace Registratr.Tests
{
    // Marker interface services
    public interface IMarkerSingleton : ISingletonService { }
    public class MarkerSingletonService : IMarkerSingleton { }

    public interface IMarkerScoped : IScopedService { }
    public class MarkerScopedService : IMarkerScoped { }

    public interface IMarkerTransient : ITransientService { }
    public class MarkerTransientService : IMarkerTransient { }

    // Attribute services
    [SingletonService]
    public interface IAttributeSingleton { }
    public class AttributeSingletonService : IAttributeSingleton { }

    public interface IAttributeScoped { }
    [ScopedService]
    public class AttributeScopedService : IAttributeScoped { }

    public interface IAttributeTransient { }
    [TransientService]
    public class AttributeTransientService : IAttributeTransient { }

    // Concrete only (no custom interface)
    public class ConcreteOnlyScopedService : IScopedService { }

    [SingletonService]
    public class ConcreteOnlyAttributeSingletonService { }

    // Multiple interfaces with IDisposable / system interfaces
    public interface IBusinessService : IScopedService { }
    public class ComplexBusinessService : IBusinessService, IDisposable, IAsyncDisposable
    {
        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    // Open Generics
    public interface IRepository<T> : IScopedService { }
    public class Repository<T> : IRepository<T> { }

    // Ignored service
    [IgnoreRegistration]
    public interface IIgnoredInterface : IScopedService { }
    public class IgnoredInterfaceService : IIgnoredInterface { }

    [IgnoreRegistration]
    public class IgnoredClassService : IScopedService { }

    // Abstract / Static classes
    public abstract class AbstractService : IScopedService { }
    public static class StaticService
    {
        public static string Message => "Hello";
    }

    // Conflicting lifetime service (ignored during general scan to not fail other tests)
    [IgnoreRegistration]
    public class ConflictingLifetimeService : ISingletonService, IScopedService { }

    // Multiple implementations for single interface
    public interface IMultiHandler : ITransientService { }
    public class FirstHandler : IMultiHandler { }
    public class SecondHandler : IMultiHandler { }
}
