using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Registratr.Abstractions;
using System.Reflection;

namespace Registratr
{
    /// <summary>
    /// Provides extension methods for automatically discovering and registering services into <see cref="IServiceCollection"/>.
    /// </summary>
    public static class IOC
    {
        private static readonly HashSet<Type> ExcludedInterfaces = new()
        {
            typeof(ISingletonService),
            typeof(IScopedService),
            typeof(ITransientService),
            typeof(IDisposable),
            typeof(IAsyncDisposable),
            typeof(ICloneable),
            typeof(IComparable),
            typeof(IFormattable),
            typeof(IConvertible)
        };

        /// <summary>
        /// Automatically registers services from the specified list of assemblies into <see cref="IServiceCollection"/>.
        /// Preserved for backward compatibility.
        /// </summary>
        public static IServiceCollection AddCustomConfiguredServices(this IServiceCollection services, List<Assembly> assemblies)
        {
            return services.AddCustomConfiguredServices((IEnumerable<Assembly>)assemblies);
        }

        /// <summary>
        /// Automatically registers services from the specified assemblies into <see cref="IServiceCollection"/>.
        /// </summary>
        public static IServiceCollection AddCustomConfiguredServices(this IServiceCollection services, params Assembly[] assemblies)
        {
            return services.AddCustomConfiguredServices((IEnumerable<Assembly>)assemblies);
        }

        /// <summary>
        /// Automatically registers services from the specified assemblies into <see cref="IServiceCollection"/> with custom options.
        /// </summary>
        public static IServiceCollection AddCustomConfiguredServices(
            this IServiceCollection services,
            IEnumerable<Assembly>? assemblies,
            Action<RegistrationOptions>? configure = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (assemblies == null)
            {
                return services;
            }

            var options = new RegistrationOptions();
            configure?.Invoke(options);

            var distinctAssemblies = assemblies.Where(a => a != null).Distinct();

            foreach (var assembly in distinctAssemblies)
            {
                RegisterAssembly(services, assembly, options);
            }

            return services;
        }

        /// <summary>
        /// Automatically registers services from the assemblies containing the specified marker types.
        /// </summary>
        public static IServiceCollection AddCustomConfiguredServices(this IServiceCollection services, params Type[] markerTypes)
        {
            return services.AddCustomConfiguredServices(null, markerTypes);
        }

        /// <summary>
        /// Automatically registers services from the assemblies containing the specified marker types with custom options.
        /// </summary>
        public static IServiceCollection AddCustomConfiguredServices(
            this IServiceCollection services,
            Action<RegistrationOptions>? configure,
            params Type[] markerTypes)
        {
            if (markerTypes == null || markerTypes.Length == 0)
            {
                return services;
            }

            var assemblies = markerTypes.Where(t => t != null).Select(t => t.Assembly).Distinct();
            return services.AddCustomConfiguredServices(assemblies, configure);
        }

        /// <summary>
        /// Semantic alias for <see cref="AddCustomConfiguredServices(IServiceCollection, Assembly[])"/>.
        /// </summary>
        public static IServiceCollection AddRegistratr(this IServiceCollection services, params Assembly[] assemblies)
        {
            return services.AddCustomConfiguredServices(assemblies);
        }

        /// <summary>
        /// Semantic alias for <see cref="AddCustomConfiguredServices(IServiceCollection, IEnumerable{Assembly}, Action{RegistrationOptions}?)"/>.
        /// </summary>
        public static IServiceCollection AddRegistratr(
            this IServiceCollection services,
            IEnumerable<Assembly>? assemblies,
            Action<RegistrationOptions>? configure = null)
        {
            return services.AddCustomConfiguredServices(assemblies, configure);
        }

        /// <summary>
        /// Semantic alias for <see cref="AddCustomConfiguredServices(IServiceCollection, Type[])"/>.
        /// </summary>
        public static IServiceCollection AddRegistratr(this IServiceCollection services, params Type[] markerTypes)
        {
            return services.AddCustomConfiguredServices(markerTypes);
        }

        /// <summary>
        /// Semantic alias for <see cref="AddCustomConfiguredServices(IServiceCollection, Action{RegistrationOptions}?, Type[])"/>.
        /// </summary>
        public static IServiceCollection AddRegistratr(
            this IServiceCollection services,
            Action<RegistrationOptions>? configure,
            params Type[] markerTypes)
        {
            return services.AddCustomConfiguredServices(configure, markerTypes);
        }

        private static void RegisterAssembly(IServiceCollection services, Assembly assembly, RegistrationOptions options)
        {
            var types = GetLoadableTypes(assembly);

            foreach (var type in types)
            {
                if (!IsValidCandidate(type, options))
                {
                    continue;
                }

                var lifetime = DetermineLifetime(type);
                if (lifetime == null)
                {
                    continue;
                }

                RegisterType(services, type, lifetime.Value, options);
            }
        }

        private static void RegisterType(
            IServiceCollection services,
            Type implementationType,
            ServiceLifetime lifetime,
            RegistrationOptions options)
        {
            var isGenericDefinition = implementationType.IsGenericTypeDefinition;
            var implementedInterfaces = implementationType.GetInterfaces();
            var businessInterfaces = implementedInterfaces.Where(i => !IsSystemOrMarkerInterface(i)).ToList();

            if (businessInterfaces.Count == 0)
            {
                // Concrete-only service (no business interfaces implemented)
                AddDescriptor(services, implementationType, implementationType, lifetime, options.DuplicateStrategy);
                return;
            }

            var eligibleInterfaces = new List<Type>();
            foreach (var iface in businessInterfaces)
            {
                if (IsIgnoredInterface(iface))
                {
                    continue;
                }

                if (isGenericDefinition)
                {
                    if (iface.IsGenericType)
                    {
                        var openIface = iface.GetGenericTypeDefinition();
                        if (openIface.GetGenericArguments().Length == implementationType.GetGenericArguments().Length)
                        {
                            eligibleInterfaces.Add(openIface);
                        }
                    }
                }
                else
                {
                    eligibleInterfaces.Add(iface);
                }
            }

            var distinctInterfaces = eligibleInterfaces.Distinct().ToList();
            if (distinctInterfaces.Count == 0)
            {
                // All business interfaces were marked with [IgnoreRegistration]; do not auto-register.
                return;
            }

            foreach (var iface in distinctInterfaces)
            {
                AddDescriptor(services, iface, implementationType, lifetime, options.DuplicateStrategy);
            }

            if (options.RegisterAsSelf)
            {
                AddDescriptor(services, implementationType, implementationType, lifetime, options.DuplicateStrategy);
            }
        }

        private static void AddDescriptor(
            IServiceCollection services,
            Type serviceType,
            Type implementationType,
            ServiceLifetime lifetime,
            DuplicateStrategy strategy)
        {
            var descriptor = new ServiceDescriptor(serviceType, implementationType, lifetime);
            if (strategy == DuplicateStrategy.TryAdd)
            {
                services.TryAdd(descriptor);
            }
            else
            {
                services.Add(descriptor);
            }
        }

        private static bool IsValidCandidate(Type type, RegistrationOptions options)
        {
            if (!type.IsClass)
            {
                return false;
            }

            // Exclude abstract classes and static classes (which are abstract & sealed in reflection)
            if (type.IsAbstract)
            {
                return false;
            }

            if (type.IsGenericTypeDefinition && !options.IncludeOpenGenerics)
            {
                return false;
            }

            if (type.GetCustomAttribute<IgnoreRegistrationAttribute>() != null)
            {
                return false;
            }

            if (options.TypeFilter != null && !options.TypeFilter(type))
            {
                return false;
            }

            return true;
        }

        internal static ServiceLifetime? DetermineLifetime(Type type)
        {
            var lifetimes = new HashSet<ServiceLifetime>();

            // 1. Marker interfaces
            if (typeof(ISingletonService).IsAssignableFrom(type))
            {
                lifetimes.Add(ServiceLifetime.Singleton);
            }

            if (typeof(IScopedService).IsAssignableFrom(type))
            {
                lifetimes.Add(ServiceLifetime.Scoped);
            }

            if (typeof(ITransientService).IsAssignableFrom(type))
            {
                lifetimes.Add(ServiceLifetime.Transient);
            }

            // 2. Attributes on the class itself
            if (type.GetCustomAttribute<SingletonServiceAttribute>() != null)
            {
                lifetimes.Add(ServiceLifetime.Singleton);
            }

            if (type.GetCustomAttribute<ScopedServiceAttribute>() != null)
            {
                lifetimes.Add(ServiceLifetime.Scoped);
            }

            if (type.GetCustomAttribute<TransientServiceAttribute>() != null)
            {
                lifetimes.Add(ServiceLifetime.Transient);
            }

            // 3. Attributes on implemented interfaces
            foreach (var iface in type.GetInterfaces())
            {
                var ifaceDef = iface.IsGenericType ? iface.GetGenericTypeDefinition() : iface;

                if (iface.GetCustomAttribute<SingletonServiceAttribute>() != null ||
                    ifaceDef.GetCustomAttribute<SingletonServiceAttribute>() != null)
                {
                    lifetimes.Add(ServiceLifetime.Singleton);
                }

                if (iface.GetCustomAttribute<ScopedServiceAttribute>() != null ||
                    ifaceDef.GetCustomAttribute<ScopedServiceAttribute>() != null)
                {
                    lifetimes.Add(ServiceLifetime.Scoped);
                }

                if (iface.GetCustomAttribute<TransientServiceAttribute>() != null ||
                    ifaceDef.GetCustomAttribute<TransientServiceAttribute>() != null)
                {
                    lifetimes.Add(ServiceLifetime.Transient);
                }
            }

            if (lifetimes.Count > 1)
            {
                throw new InvalidOperationException(
                    $"Type '{type.FullName}' has multiple conflicting service lifetimes ({string.Join(", ", lifetimes)}). Only one lifetime can be specified per type.");
            }

            return lifetimes.Count == 1 ? lifetimes.First() : null;
        }

        private static bool IsSystemOrMarkerInterface(Type iface)
        {
            if (ExcludedInterfaces.Contains(iface))
            {
                return true;
            }

            if (iface.IsGenericType)
            {
                var genDef = iface.GetGenericTypeDefinition();
                if (ExcludedInterfaces.Contains(genDef) ||
                    genDef == typeof(IComparable<>) ||
                    genDef == typeof(IEquatable<>))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsIgnoredInterface(Type iface)
        {
            if (iface.GetCustomAttribute<IgnoreRegistrationAttribute>() != null)
            {
                return true;
            }

            if (iface.IsGenericType)
            {
                var genDef = iface.GetGenericTypeDefinition();
                if (genDef.GetCustomAttribute<IgnoreRegistrationAttribute>() != null)
                {
                    return true;
                }
            }

            return false;
        }

        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            if (assembly == null)
            {
                return Enumerable.Empty<Type>();
            }

            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(t => t != null).Select(t => t!);
            }
        }
    }
}
