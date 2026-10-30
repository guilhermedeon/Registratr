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
        /// Automatically registers services from the calling assembly into <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The service collection to add services to.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddCustomConfiguredServices(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            return services.AddCustomConfiguredServices(new[] { Assembly.GetCallingAssembly() }, (Action<RegistrationOptions>?)null);
        }

        /// <summary>
        /// Automatically registers services from the calling assembly into <see cref="IServiceCollection"/> with custom options.
        /// </summary>
        /// <param name="services">The service collection to add services to.</param>
        /// <param name="configure">Optional action to configure registration options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddCustomConfiguredServices(
            this IServiceCollection services,
            Action<RegistrationOptions>? configure)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            return services.AddCustomConfiguredServices(new[] { Assembly.GetCallingAssembly() }, configure);
        }

        /// <summary>
        /// Automatically registers services from the specified assembly into <see cref="IServiceCollection"/> with optional custom options.
        /// </summary>
        /// <param name="services">The service collection to add services to.</param>
        /// <param name="assembly">The assembly to scan for services.</param>
        /// <param name="configure">Optional action to configure registration options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddCustomConfiguredServices(
            this IServiceCollection services,
            Assembly assembly,
            Action<RegistrationOptions>? configure = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            var assemblies = assembly != null ? new[] { assembly } : Enumerable.Empty<Assembly>();
            return services.AddCustomConfiguredServices(assemblies, configure);
        }

        /// <summary>
        /// Automatically registers services from the specified list of assemblies into <see cref="IServiceCollection"/>.
        /// Preserved for backward compatibility.
        /// </summary>
        /// <param name="services">The service collection to add services to.</param>
        /// <param name="assemblies">The list of assemblies to scan.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddCustomConfiguredServices(this IServiceCollection services, List<Assembly> assemblies)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            return services.AddCustomConfiguredServices((IEnumerable<Assembly>)assemblies);
        }

        /// <summary>
        /// Automatically registers services from the specified assemblies into <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The service collection to add services to.</param>
        /// <param name="assemblies">The assemblies to scan for services.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddCustomConfiguredServices(this IServiceCollection services, params Assembly[] assemblies)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            return services.AddCustomConfiguredServices((IEnumerable<Assembly>)assemblies);
        }

        /// <summary>
        /// Automatically registers services from the specified assemblies into <see cref="IServiceCollection"/> with custom options.
        /// </summary>
        /// <param name="services">The service collection to add services to.</param>
        /// <param name="assemblies">The assemblies to scan for services.</param>
        /// <param name="configure">Optional action to configure registration options.</param>
        /// <returns>The service collection for chaining.</returns>
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
        /// <param name="services">The service collection to add services to.</param>
        /// <param name="markerTypes">Marker types whose containing assemblies should be scanned.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddCustomConfiguredServices(this IServiceCollection services, params Type[] markerTypes)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            return services.AddCustomConfiguredServices(null, markerTypes);
        }

        /// <summary>
        /// Automatically registers services from the assemblies containing the specified marker types with custom options.
        /// </summary>
        /// <param name="services">The service collection to add services to.</param>
        /// <param name="configure">Optional action to configure registration options.</param>
        /// <param name="markerTypes">Marker types whose containing assemblies should be scanned.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddCustomConfiguredServices(
            this IServiceCollection services,
            Action<RegistrationOptions>? configure,
            params Type[] markerTypes)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (markerTypes == null || markerTypes.Length == 0)
            {
                return services;
            }

            var assemblies = markerTypes.Where(t => t != null).Select(t => t.Assembly).Distinct();
            return services.AddCustomConfiguredServices(assemblies, configure);
        }

        /// <summary>
        /// Automatically registers services from the calling assembly into <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The service collection to add services to.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddRegistratr(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            return services.AddCustomConfiguredServices(new[] { Assembly.GetCallingAssembly() }, (Action<RegistrationOptions>?)null);
        }

        /// <summary>
        /// Automatically registers services from the calling assembly into <see cref="IServiceCollection"/> with custom options.
        /// </summary>
        /// <param name="services">The service collection to add services to.</param>
        /// <param name="configure">Optional action to configure registration options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddRegistratr(
            this IServiceCollection services,
            Action<RegistrationOptions>? configure)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            return services.AddCustomConfiguredServices(new[] { Assembly.GetCallingAssembly() }, configure);
        }

        /// <summary>
        /// Automatically registers services from the specified assembly into <see cref="IServiceCollection"/> with optional custom options.
        /// </summary>
        /// <param name="services">The service collection to add services to.</param>
        /// <param name="assembly">The assembly to scan for services.</param>
        /// <param name="configure">Optional action to configure registration options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddRegistratr(
            this IServiceCollection services,
            Assembly assembly,
            Action<RegistrationOptions>? configure = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            var assemblies = assembly != null ? new[] { assembly } : Enumerable.Empty<Assembly>();
            return services.AddCustomConfiguredServices(assemblies, configure);
        }

        /// <summary>
        /// Semantic alias for <see cref="AddCustomConfiguredServices(IServiceCollection, Assembly[])"/>.
        /// </summary>
        /// <param name="services">The service collection to add services to.</param>
        /// <param name="assemblies">The assemblies to scan for services.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddRegistratr(this IServiceCollection services, params Assembly[] assemblies)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            return services.AddCustomConfiguredServices(assemblies);
        }

        /// <summary>
        /// Semantic alias for <see cref="AddCustomConfiguredServices(IServiceCollection, IEnumerable{Assembly}, Action{RegistrationOptions}?)"/>.
        /// </summary>
        /// <param name="services">The service collection to add services to.</param>
        /// <param name="assemblies">The assemblies to scan for services.</param>
        /// <param name="configure">Optional action to configure registration options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddRegistratr(
            this IServiceCollection services,
            IEnumerable<Assembly>? assemblies,
            Action<RegistrationOptions>? configure = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            return services.AddCustomConfiguredServices(assemblies, configure);
        }

        /// <summary>
        /// Semantic alias for <see cref="AddCustomConfiguredServices(IServiceCollection, Type[])"/>.
        /// </summary>
        /// <param name="services">The service collection to add services to.</param>
        /// <param name="markerTypes">Marker types whose containing assemblies should be scanned.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddRegistratr(this IServiceCollection services, params Type[] markerTypes)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            return services.AddCustomConfiguredServices(markerTypes);
        }

        /// <summary>
        /// Semantic alias for <see cref="AddCustomConfiguredServices(IServiceCollection, Action{RegistrationOptions}?, Type[])"/>.
        /// </summary>
        /// <param name="services">The service collection to add services to.</param>
        /// <param name="configure">Optional action to configure registration options.</param>
        /// <param name="markerTypes">Marker types whose containing assemblies should be scanned.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddRegistratr(
            this IServiceCollection services,
            Action<RegistrationOptions>? configure,
            params Type[] markerTypes)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            return services.AddCustomConfiguredServices(configure, markerTypes);
        }

        private static void RegisterAssembly(IServiceCollection services, Assembly assembly, RegistrationOptions options)
        {
            var types = GetLoadableTypes(assembly);

            foreach (var type in types)
            {
                try
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
                catch (InvalidOperationException)
                {
                    throw;
                }
                catch (Exception)
                {
                    // Gracefully skip types that fail reflection evaluation (e.g. TypeLoadException, FileNotFoundException)
                    continue;
                }
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
                        var ifaceArgs = iface.GetGenericArguments();
                        var implArgs = implementationType.GetGenericArguments();
                        if (ifaceArgs.Length == implArgs.Length && ifaceArgs.SequenceEqual(implArgs))
                        {
                            eligibleInterfaces.Add(iface.GetGenericTypeDefinition());
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
                // All business interfaces were marked with [IgnoreRegistration] or incompatible open generic definitions.
                if (options.RegisterAsSelf)
                {
                    AddDescriptor(services, implementationType, implementationType, lifetime, options.DuplicateStrategy);
                }
                return;
            }

            if (isGenericDefinition)
            {
                foreach (var iface in distinctInterfaces)
                {
                    AddDescriptor(services, iface, implementationType, lifetime, options.DuplicateStrategy);
                }

                if (options.RegisterAsSelf)
                {
                    AddDescriptor(services, implementationType, implementationType, lifetime, options.DuplicateStrategy);
                }
            }
            else
            {
                if (distinctInterfaces.Count > 1 || options.RegisterAsSelf)
                {
                    // Register concrete implementation directly
                    AddDescriptor(services, implementationType, implementationType, lifetime, options.DuplicateStrategy);

                    // Forward each interface to the concrete registration so that all interfaces share the same instance
                    foreach (var iface in distinctInterfaces)
                    {
                        AddFactoryDescriptor(services, iface, implementationType, lifetime, options.DuplicateStrategy);
                    }
                }
                else
                {
                    // Exactly one interface and RegisterAsSelf is false
                    AddDescriptor(services, distinctInterfaces[0], implementationType, lifetime, options.DuplicateStrategy);
                }
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
            AddDescriptor(services, descriptor, strategy);
        }

        private static void AddFactoryDescriptor(
            IServiceCollection services,
            Type serviceType,
            Type implementationType,
            ServiceLifetime lifetime,
            DuplicateStrategy strategy)
        {
            var descriptor = ServiceDescriptor.Describe(
                serviceType,
                sp => sp.GetRequiredService(implementationType),
                lifetime);
            AddDescriptor(services, descriptor, strategy);
        }

        private static void AddDescriptor(
            IServiceCollection services,
            ServiceDescriptor descriptor,
            DuplicateStrategy strategy)
        {
            if (strategy == DuplicateStrategy.TryAdd || strategy == DuplicateStrategy.SkipIfDuplicate)
            {
                services.TryAdd(descriptor);
            }
            else if (strategy == DuplicateStrategy.Overwrite)
            {
                services.Replace(descriptor);
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
            if (typeof(ISingletonService).IsAssignableFrom(type) && !IsOnlyFromIgnoredInterfaces(type, typeof(ISingletonService)))
            {
                lifetimes.Add(ServiceLifetime.Singleton);
            }

            if (typeof(IScopedService).IsAssignableFrom(type) && !IsOnlyFromIgnoredInterfaces(type, typeof(IScopedService)))
            {
                lifetimes.Add(ServiceLifetime.Scoped);
            }

            if (typeof(ITransientService).IsAssignableFrom(type) && !IsOnlyFromIgnoredInterfaces(type, typeof(ITransientService)))
            {
                lifetimes.Add(ServiceLifetime.Transient);
            }

            // 2. Attributes on the class itself
            if (type.GetCustomAttribute<SingletonServiceAttribute>() != null ||
                type.GetCustomAttribute<SingletonAttribute>() != null)
            {
                lifetimes.Add(ServiceLifetime.Singleton);
            }

            if (type.GetCustomAttribute<ScopedServiceAttribute>() != null ||
                type.GetCustomAttribute<ScopedAttribute>() != null)
            {
                lifetimes.Add(ServiceLifetime.Scoped);
            }

            if (type.GetCustomAttribute<TransientServiceAttribute>() != null ||
                type.GetCustomAttribute<TransientAttribute>() != null)
            {
                lifetimes.Add(ServiceLifetime.Transient);
            }

            // 3. Attributes on implemented interfaces (skipping ignored interfaces)
            foreach (var iface in type.GetInterfaces())
            {
                if (IsIgnoredInterface(iface))
                {
                    continue;
                }

                var ifaceDef = iface.IsGenericType ? iface.GetGenericTypeDefinition() : iface;

                if (iface.GetCustomAttribute<SingletonServiceAttribute>() != null ||
                    ifaceDef.GetCustomAttribute<SingletonServiceAttribute>() != null ||
                    iface.GetCustomAttribute<SingletonAttribute>() != null ||
                    ifaceDef.GetCustomAttribute<SingletonAttribute>() != null)
                {
                    lifetimes.Add(ServiceLifetime.Singleton);
                }

                if (iface.GetCustomAttribute<ScopedServiceAttribute>() != null ||
                    ifaceDef.GetCustomAttribute<ScopedServiceAttribute>() != null ||
                    iface.GetCustomAttribute<ScopedAttribute>() != null ||
                    ifaceDef.GetCustomAttribute<ScopedAttribute>() != null)
                {
                    lifetimes.Add(ServiceLifetime.Scoped);
                }

                if (iface.GetCustomAttribute<TransientServiceAttribute>() != null ||
                    ifaceDef.GetCustomAttribute<TransientServiceAttribute>() != null ||
                    iface.GetCustomAttribute<TransientAttribute>() != null ||
                    ifaceDef.GetCustomAttribute<TransientAttribute>() != null)
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

        private static bool IsOnlyFromIgnoredInterfaces(Type type, Type markerType)
        {
            var interfacesWithMarker = type.GetInterfaces()
                .Where(i => i != markerType && markerType.IsAssignableFrom(i))
                .ToList();

            return interfacesWithMarker.Count > 0 && interfacesWithMarker.All(IsIgnoredInterface);
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
