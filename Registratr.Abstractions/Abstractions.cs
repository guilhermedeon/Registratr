namespace Registratr.Abstractions
{
    /// <summary>
    /// Marker interface for services that should be automatically registered with a Singleton lifetime.
    /// </summary>
    public interface ISingletonService
    {
    }

    /// <summary>
    /// Marker interface for services that should be automatically registered with a Scoped lifetime.
    /// </summary>
    public interface IScopedService
    {
    }

    /// <summary>
    /// Marker interface for services that should be automatically registered with a Transient lifetime.
    /// </summary>
    public interface ITransientService
    {
    }

    /// <summary>
    /// Base attribute for declaring service lifetime on a class or interface.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
    public abstract class ServiceLifetimeAttribute : Attribute
    {
    }

    /// <summary>
    /// Marks a class or interface to be registered with a Singleton lifetime.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
    public sealed class SingletonServiceAttribute : ServiceLifetimeAttribute
    {
    }

    /// <summary>
    /// Marks a class or interface to be registered with a Scoped lifetime.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
    public sealed class ScopedServiceAttribute : ServiceLifetimeAttribute
    {
    }

    /// <summary>
    /// Marks a class or interface to be registered with a Transient lifetime.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
    public sealed class TransientServiceAttribute : ServiceLifetimeAttribute
    {
    }

    /// <summary>
    /// Marks a class or interface to be registered with a Singleton lifetime.
    /// Short alias for <see cref="SingletonServiceAttribute"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
    public sealed class SingletonAttribute : ServiceLifetimeAttribute
    {
    }

    /// <summary>
    /// Marks a class or interface to be registered with a Scoped lifetime.
    /// Short alias for <see cref="ScopedServiceAttribute"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
    public sealed class ScopedAttribute : ServiceLifetimeAttribute
    {
    }

    /// <summary>
    /// Marks a class or interface to be registered with a Transient lifetime.
    /// Short alias for <see cref="TransientServiceAttribute"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
    public sealed class TransientAttribute : ServiceLifetimeAttribute
    {
    }

    /// <summary>
    /// Marks a class or interface to be ignored by automatic service registration.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
    public sealed class IgnoreRegistrationAttribute : Attribute
    {
    }
}
