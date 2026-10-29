using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Registratr.Tests
{
    public class ConventionRegistrationTests
    {
        private readonly Assembly _testAssembly = typeof(ConventionRegistrationTests).Assembly;

        [Fact]
        public void AddCustomConfiguredServices_WithMarkerInterfaces_RegistersExpectedLifetimes()
        {
            var services = new ServiceCollection();
            services.AddCustomConfiguredServices(_testAssembly);

            var singletonDesc = services.FirstOrDefault(d => d.ServiceType == typeof(IMarkerSingleton));
            var scopedDesc = services.FirstOrDefault(d => d.ServiceType == typeof(IMarkerScoped));
            var transientDesc = services.FirstOrDefault(d => d.ServiceType == typeof(IMarkerTransient));

            Assert.NotNull(singletonDesc);
            Assert.Equal(ServiceLifetime.Singleton, singletonDesc.Lifetime);
            Assert.Equal(typeof(MarkerSingletonService), singletonDesc.ImplementationType);

            Assert.NotNull(scopedDesc);
            Assert.Equal(ServiceLifetime.Scoped, scopedDesc.Lifetime);
            Assert.Equal(typeof(MarkerScopedService), scopedDesc.ImplementationType);

            Assert.NotNull(transientDesc);
            Assert.Equal(ServiceLifetime.Transient, transientDesc.Lifetime);
            Assert.Equal(typeof(MarkerTransientService), transientDesc.ImplementationType);
        }

        [Fact]
        public void AddCustomConfiguredServices_WithAttributes_RegistersExpectedLifetimes()
        {
            var services = new ServiceCollection();
            services.AddRegistratr(_testAssembly);

            var singletonDesc = services.FirstOrDefault(d => d.ServiceType == typeof(IAttributeSingleton));
            var scopedDesc = services.FirstOrDefault(d => d.ServiceType == typeof(IAttributeScoped));
            var transientDesc = services.FirstOrDefault(d => d.ServiceType == typeof(IAttributeTransient));

            Assert.NotNull(singletonDesc);
            Assert.Equal(ServiceLifetime.Singleton, singletonDesc.Lifetime);
            Assert.Equal(typeof(AttributeSingletonService), singletonDesc.ImplementationType);

            Assert.NotNull(scopedDesc);
            Assert.Equal(ServiceLifetime.Scoped, scopedDesc.Lifetime);
            Assert.Equal(typeof(AttributeScopedService), scopedDesc.ImplementationType);

            Assert.NotNull(transientDesc);
            Assert.Equal(ServiceLifetime.Transient, transientDesc.Lifetime);
            Assert.Equal(typeof(AttributeTransientService), transientDesc.ImplementationType);
        }

        [Fact]
        public void AddCustomConfiguredServices_WithoutInterfaces_RegistersConcreteSelf()
        {
            var services = new ServiceCollection();
            services.AddCustomConfiguredServices(_testAssembly);

            var scopedDesc = services.FirstOrDefault(d => d.ServiceType == typeof(ConcreteOnlyScopedService));
            var singletonDesc = services.FirstOrDefault(d => d.ServiceType == typeof(ConcreteOnlyAttributeSingletonService));

            Assert.NotNull(scopedDesc);
            Assert.Equal(typeof(ConcreteOnlyScopedService), scopedDesc.ImplementationType);
            Assert.Equal(ServiceLifetime.Scoped, scopedDesc.Lifetime);

            Assert.NotNull(singletonDesc);
            Assert.Equal(typeof(ConcreteOnlyAttributeSingletonService), singletonDesc.ImplementationType);
            Assert.Equal(ServiceLifetime.Singleton, singletonDesc.Lifetime);
        }

        [Fact]
        public void AddCustomConfiguredServices_ExcludesSystemAndMarkerInterfaces()
        {
            var services = new ServiceCollection();
            services.AddCustomConfiguredServices(_testAssembly);

            Assert.DoesNotContain(services, d => d.ServiceType == typeof(IDisposable));
            Assert.DoesNotContain(services, d => d.ServiceType == typeof(IAsyncDisposable));
            Assert.DoesNotContain(services, d => d.ServiceType == typeof(Abstractions.IScopedService));
            Assert.DoesNotContain(services, d => d.ServiceType == typeof(Abstractions.ISingletonService));
            Assert.DoesNotContain(services, d => d.ServiceType == typeof(Abstractions.ITransientService));

            // Business interface is registered
            Assert.Contains(services, d => d.ServiceType == typeof(IBusinessService) && d.ImplementationType == typeof(ComplexBusinessService));
        }

        [Fact]
        public void AddCustomConfiguredServices_RegistersOpenGenerics()
        {
            var services = new ServiceCollection();
            services.AddCustomConfiguredServices(_testAssembly);

            var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IRepository<>));
            Assert.NotNull(descriptor);
            Assert.Equal(typeof(Repository<>), descriptor.ImplementationType);
            Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);

            // Verify resolution with ServiceProvider
            var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var repo = scope.ServiceProvider.GetService<IRepository<string>>();
            Assert.NotNull(repo);
            Assert.IsType<Repository<string>>(repo);
        }

        [Fact]
        public void AddCustomConfiguredServices_RespectsIgnoreRegistrationAttribute()
        {
            var services = new ServiceCollection();
            services.AddCustomConfiguredServices(_testAssembly);

            Assert.DoesNotContain(services, d => d.ServiceType == typeof(IIgnoredInterface));
            Assert.DoesNotContain(services, d => d.ImplementationType == typeof(IgnoredInterfaceService));
            Assert.DoesNotContain(services, d => d.ServiceType == typeof(IgnoredClassService));
        }

        [Fact]
        public void AddCustomConfiguredServices_ExcludesAbstractAndStaticClasses()
        {
            var services = new ServiceCollection();
            services.AddCustomConfiguredServices(_testAssembly);

            Assert.DoesNotContain(services, d => d.ImplementationType == typeof(AbstractService));
            Assert.DoesNotContain(services, d => d.ImplementationType == typeof(StaticService));
        }

        [Fact]
        public void DetermineLifetime_WithConflictingLifetimes_ThrowsInvalidOperationException()
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                IOC.DetermineLifetime(typeof(ConflictingLifetimeService));
            });

            Assert.Contains(nameof(ConflictingLifetimeService), ex.Message);
            Assert.Contains("conflicting service lifetimes", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void AddCustomConfiguredServices_WithDuplicateStrategyTryAdd_DoesNotAddDuplicates()
        {
            var services = new ServiceCollection();
            services.AddCustomConfiguredServices(
                new[] { _testAssembly },
                options => options.DuplicateStrategy = DuplicateStrategy.TryAdd);

            var handlers = services.Where(d => d.ServiceType == typeof(IMultiHandler)).ToList();
            Assert.Single(handlers);
        }

        [Fact]
        public void AddCustomConfiguredServices_WithDuplicateStrategyAppend_RegistersMultipleImplementations()
        {
            var services = new ServiceCollection();
            services.AddCustomConfiguredServices(
                new[] { _testAssembly },
                options => options.DuplicateStrategy = DuplicateStrategy.Append);

            var handlers = services.Where(d => d.ServiceType == typeof(IMultiHandler)).ToList();
            Assert.Equal(2, handlers.Count);
        }

        [Fact]
        public void AddCustomConfiguredServices_WithRegisterAsSelf_RegistersBothInterfaceAndSelf()
        {
            var services = new ServiceCollection();
            services.AddCustomConfiguredServices(
                new[] { _testAssembly },
                options => options.RegisterAsSelf = true);

            Assert.Contains(services, d => d.ServiceType == typeof(IMarkerScoped) && d.ImplementationType == typeof(MarkerScopedService));
            Assert.Contains(services, d => d.ServiceType == typeof(MarkerScopedService) && d.ImplementationType == typeof(MarkerScopedService));
        }

        [Fact]
        public void AddCustomConfiguredServices_WithTypeFilter_FiltersCorrectly()
        {
            var services = new ServiceCollection();
            services.AddCustomConfiguredServices(
                new[] { _testAssembly },
                options => options.TypeFilter = t => t == typeof(MarkerSingletonService));

            Assert.Contains(services, d => d.ServiceType == typeof(IMarkerSingleton));
            Assert.DoesNotContain(services, d => d.ServiceType == typeof(IMarkerScoped));
        }

        [Fact]
        public void AddCustomConfiguredServices_WithMarkerTypes_ScansCorrectAssembly()
        {
            var services = new ServiceCollection();
            services.AddRegistratr(typeof(MarkerSingletonService));

            Assert.Contains(services, d => d.ServiceType == typeof(IMarkerSingleton));
        }

        [Fact]
        public void AddCustomConfiguredServices_WithListAssembly_MaintainsBackwardCompatibility()
        {
            var services = new ServiceCollection();
            var list = new List<Assembly> { _testAssembly };

            services.AddCustomConfiguredServices(list);

            Assert.Contains(services, d => d.ServiceType == typeof(IMarkerSingleton));
        }
    }
}
