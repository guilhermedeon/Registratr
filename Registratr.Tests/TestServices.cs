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

    // Mismatched Open Generics
    public interface IValidator<T> : IScopedService { }
    public class ClosedGenericValidator<T> : IValidator<string> { }

    public interface IPair<T1, T2> : IScopedService { }
    public class SwappedPair<T1, T2> : IPair<T2, T1> { }

    // Multiple interfaces for instance identity testing
    public interface IServiceOne { }
    public interface IServiceTwo { }

    [Singleton]
    public class MultiInterfaceSingletonService : IServiceOne, IServiceTwo { }

    public interface IScopedOne { }
    public interface IScopedTwo { }

    [Scoped]
    public class MultiInterfaceScopedService : IScopedOne, IScopedTwo { }

    // Short attribute aliases
    public interface IShortSingleton { }
    [Singleton]
    public class ShortSingletonService : IShortSingleton { }

    public interface IShortScoped { }
    [Scoped]
    public class ShortScopedService : IShortScoped { }

    public interface IShortTransient { }
    [Transient]
    public class ShortTransientService : IShortTransient { }

    // Ignored interface with conflicting lifetime attribute
    [IgnoreRegistration]
    [Scoped]
    public interface IIgnoredWithConflictingLifetime { }

    [Singleton]
    public class ServiceWithIgnoredConflictingInterface : IIgnoredWithConflictingLifetime { }

    // Class with only ignored business interfaces
    [IgnoreRegistration]
    public interface IOnlyIgnoredInterface { }

    [Scoped]
    public class ClassWithOnlyIgnoredInterface : IOnlyIgnoredInterface { }

    // Overridable service for duplicate strategy tests
    public interface IOverridableService { }
    public class InitialOverridableService : IOverridableService { }

    [Singleton]
    public class ReplacementOverridableService : IOverridableService { }
}
