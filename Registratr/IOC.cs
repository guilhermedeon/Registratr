using Microsoft.Extensions.DependencyInjection;
using Registratr.Abstractions;
using System.Reflection;
//main
namespace Registratr
{
    public static class IOC
    {
        public static IServiceCollection AddCustomConfiguredServices(this IServiceCollection services, List<Assembly> assemblies)
        {
            if (assemblies == null || assemblies.Count == 0)
            {
                return services;
            }

            List<Type> standardTypes = new()
            {
                typeof(ISingletonService),
                typeof(ITransientService),
                typeof(IScopedService),
            };

            List<Type> usedTypes = new();

            List<Type> allTypes = new();
            foreach (Assembly assembly in assemblies)
            {
                var assemblyTypes = assembly.GetTypes()
                .Where(type => type.IsClass || type.IsInterface).Where(type => !standardTypes.Contains(type)).ToList();
                allTypes.AddRange(assemblyTypes);
            }

            foreach (var interfaceType in allTypes.Where(t => t.IsInterface))
            {
                foreach (var classType in allTypes.Where(t => t.IsClass))
                {
                    if (interfaceType.IsAssignableFrom(classType))
                    {
                        services = AddToServices(services, classType, interfaceType: interfaceType);
                        usedTypes.Add(classType);
                    }
                }
                usedTypes.Add(interfaceType);
            }

            foreach (var classType in allTypes.Where(t => t.IsClass || !usedTypes.Contains(t)))
            {
                services = AddToServices(services, classType);
            }

            return services;
        }

        private static IServiceCollection AddToServices(IServiceCollection services, Type concreteType, Type? interfaceType = null)
        {
            if (interfaceType == null)
            {
                if (typeof(ISingletonService).IsAssignableFrom(concreteType)) services.AddSingleton(concreteType);
                if (typeof(IScopedService).IsAssignableFrom(concreteType)) services.AddScoped(concreteType);
                if (typeof(ITransientService).IsAssignableFrom(concreteType)) services.AddTransient(concreteType);
            }
            else
            {
                if (typeof(ISingletonService).IsAssignableFrom(concreteType)) services.AddSingleton(interfaceType, concreteType);
                if (typeof(IScopedService).IsAssignableFrom(concreteType)) services.AddScoped(interfaceType, concreteType);
                if (typeof(ITransientService).IsAssignableFrom(concreteType)) services.AddTransient(interfaceType, concreteType);
            }
            return services;
        }
    }
}
