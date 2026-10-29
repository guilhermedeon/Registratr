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

            // Interface registration is forwarded to the concrete implementation
            Assert.Contains(services, d => d.ServiceType == typeof(IMarkerScoped) && d.ImplementationFactory != null);
            // Concrete registration exists directly
            Assert.Contains(services, d => d.ServiceType == typeof(MarkerScopedService) && d.ImplementationType == typeof(MarkerScopedService));

            var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var fromInterface = scope.ServiceProvider.GetRequiredService<IMarkerScoped>();
            var fromSelf = scope.ServiceProvider.GetRequiredService<MarkerScopedService>();

            Assert.NotNull(fromInterface);
            Assert.NotNull(fromSelf);
            Assert.Same(fromInterface, fromSelf);
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

        [Fact]
        public void AddRegistratr_OpenGenericImplementingClosedGenericInterface_IsNotRegisteredAsOpenGeneric()
        {
            var services = new ServiceCollection();
            services.AddRegistratr(_testAssembly);

            // ClosedGenericValidator<T> implements IValidator<string>, NOT IValidator<T>
            // It must NOT be registered as IValidator<>
            Assert.DoesNotContain(services, d => d.ServiceType == typeof(IValidator<>));
        }

        [Fact]
        public void AddRegistratr_OpenGenericWithSwappedTypeParameters_IsNotRegistered()
        {
            var services = new ServiceCollection();
            services.AddRegistratr(_testAssembly);

            // SwappedPair<T1, T2> implements IPair<T2, T1>, type arguments do not match sequence
            Assert.DoesNotContain(services, d => d.ServiceType == typeof(IPair<,>));
        }

        [Fact]
        public void AddRegistratr_MultipleInterfaces_PreservesSingletonInstanceIdentity()
        {
            var services = new ServiceCollection();
            services.AddRegistratr(_testAssembly);

            var provider = services.BuildServiceProvider();
            var svcOne = provider.GetRequiredService<IServiceOne>();
            var svcTwo = provider.GetRequiredService<IServiceTwo>();

            Assert.NotNull(svcOne);
            Assert.NotNull(svcTwo);
            Assert.Same(svcOne, svcTwo);
        }

        [Fact]
        public void AddRegistratr_MultipleInterfaces_PreservesScopedInstanceIdentity()
        {
            var services = new ServiceCollection();
            services.AddRegistratr(_testAssembly);

            var provider = services.BuildServiceProvider();

            using (var scope1 = provider.CreateScope())
            {
                var one = scope1.ServiceProvider.GetRequiredService<IScopedOne>();
                var two = scope1.ServiceProvider.GetRequiredService<IScopedTwo>();

                Assert.NotNull(one);
                Assert.NotNull(two);
                Assert.Same(one, two);
            }

            using (var scope1 = provider.CreateScope())
            using (var scope2 = provider.CreateScope())
            {
                var s1 = scope1.ServiceProvider.GetRequiredService<IScopedOne>();
                var s2 = scope2.ServiceProvider.GetRequiredService<IScopedOne>();

                // Different scopes should have distinct instances
                Assert.NotSame(s1, s2);
            }
        }

        [Fact]
        public void AddRegistratr_WithShortAttributes_RegistersExpectedLifetimes()
        {
            var services = new ServiceCollection();
            services.AddRegistratr(_testAssembly);

            var singletonDesc = services.FirstOrDefault(d => d.ServiceType == typeof(IShortSingleton));
            var scopedDesc = services.FirstOrDefault(d => d.ServiceType == typeof(IShortScoped));
            var transientDesc = services.FirstOrDefault(d => d.ServiceType == typeof(IShortTransient));

            Assert.NotNull(singletonDesc);
            Assert.Equal(ServiceLifetime.Singleton, singletonDesc.Lifetime);

            Assert.NotNull(scopedDesc);
            Assert.Equal(ServiceLifetime.Scoped, scopedDesc.Lifetime);

            Assert.NotNull(transientDesc);
            Assert.Equal(ServiceLifetime.Transient, transientDesc.Lifetime);
        }

        [Fact]
        public void AddRegistratr_IgnoredInterfaceWithConflictingLifetime_DoesNotTriggerException()
        {
            var services = new ServiceCollection();

            // ServiceWithIgnoredConflictingInterface has [Singleton] and implements [IgnoreRegistration] [Scoped] IIgnoredWithConflictingLifetime
            // Scanning must NOT throw InvalidOperationException
            var exception = Record.Exception(() => services.AddRegistratr(_testAssembly));
            Assert.Null(exception);

            // When RegisterAsSelf is enabled, it should be registered as Singleton
            var servicesWithSelf = new ServiceCollection();
            servicesWithSelf.AddRegistratr(_testAssembly, options => options.RegisterAsSelf = true);

            var desc = servicesWithSelf.FirstOrDefault(d => d.ServiceType == typeof(ServiceWithIgnoredConflictingInterface));
            Assert.NotNull(desc);
            Assert.Equal(ServiceLifetime.Singleton, desc.Lifetime);
        }

        [Fact]
        public void AddRegistratr_ClassWithOnlyIgnoredInterfaces_RespectsRegisterAsSelf()
        {
            // When RegisterAsSelf is false, it shouldn't be registered
            var servicesWithoutSelf = new ServiceCollection();
            servicesWithoutSelf.AddRegistratr(_testAssembly, options => options.RegisterAsSelf = false);
            Assert.DoesNotContain(servicesWithoutSelf, d => d.ServiceType == typeof(ClassWithOnlyIgnoredInterface));

            // When RegisterAsSelf is true, it should be registered directly
            var servicesWithSelf = new ServiceCollection();
            servicesWithSelf.AddRegistratr(_testAssembly, options => options.RegisterAsSelf = true);
            var desc = servicesWithSelf.FirstOrDefault(d => d.ServiceType == typeof(ClassWithOnlyIgnoredInterface));
            Assert.NotNull(desc);
            Assert.Equal(ServiceLifetime.Scoped, desc.Lifetime);
            Assert.Equal(typeof(ClassWithOnlyIgnoredInterface), desc.ImplementationType);
        }

        [Fact]
        public void AddRegistratr_DuplicateStrategyOverwrite_ReplacesExistingRegistration()
        {
            var services = new ServiceCollection();
            services.AddTransient<IOverridableService, InitialOverridableService>();

            // Overwrite strategy should replace InitialOverridableService with ReplacementOverridableService
            services.AddRegistratr(_testAssembly, options =>
            {
                options.DuplicateStrategy = DuplicateStrategy.Overwrite;
                options.TypeFilter = t => t == typeof(ReplacementOverridableService);
            });

            var descriptors = services.Where(d => d.ServiceType == typeof(IOverridableService)).ToList();
            Assert.Single(descriptors);

            var provider = services.BuildServiceProvider();
            var instance = provider.GetRequiredService<IOverridableService>();
            Assert.IsType<ReplacementOverridableService>(instance);
        }

        [Fact]
        public void AddRegistratr_DuplicateStrategySkipIfDuplicate_PreservesExistingRegistration()
        {
            var services = new ServiceCollection();
            services.AddTransient<IOverridableService, InitialOverridableService>();

            // SkipIfDuplicate (TryAdd) should NOT replace InitialOverridableService
            services.AddRegistratr(_testAssembly, options =>
            {
                options.DuplicateStrategy = DuplicateStrategy.SkipIfDuplicate;
                options.TypeFilter = t => t == typeof(ReplacementOverridableService);
            });

            var descriptors = services.Where(d => d.ServiceType == typeof(IOverridableService)).ToList();
            Assert.Single(descriptors);

            var provider = services.BuildServiceProvider();
            var instance = provider.GetRequiredService<IOverridableService>();
            Assert.IsType<InitialOverridableService>(instance);
        }

        [Fact]
        public void AddRegistratr_IncludeOpenGenericsFalse_SkipsOpenGenerics()
        {
            var services = new ServiceCollection();
            services.AddRegistratr(_testAssembly, options => options.IncludeOpenGenerics = false);

            Assert.DoesNotContain(services, d => d.ServiceType == typeof(IRepository<>));
        }

        [Fact]
        public void AddRegistratr_NullServices_ThrowsArgumentNullException()
        {
            IServiceCollection? nullServices = null;

            Assert.Throws<ArgumentNullException>(() => nullServices!.AddRegistratr());
            Assert.Throws<ArgumentNullException>(() => nullServices!.AddRegistratr(_testAssembly));
            Assert.Throws<ArgumentNullException>(() => nullServices!.AddRegistratr(new[] { _testAssembly }));
            Assert.Throws<ArgumentNullException>(() => nullServices!.AddRegistratr(typeof(MarkerSingletonService)));
            Assert.Throws<ArgumentNullException>(() => nullServices!.AddRegistratr(_ => { }, typeof(MarkerSingletonService)));
            Assert.Throws<ArgumentNullException>(() => nullServices!.AddCustomConfiguredServices());
            Assert.Throws<ArgumentNullException>(() => nullServices!.AddCustomConfiguredServices(new List<Assembly> { _testAssembly }));
            Assert.Throws<ArgumentNullException>(() => nullServices!.AddCustomConfiguredServices(new[] { _testAssembly }));
        }

        [Fact]
        public void AddRegistratr_Parameterless_ScansCallingAssembly()
        {
            var services = new ServiceCollection();
            // Calling AddRegistratr() should scan this assembly without CS0121 overload ambiguity
            services.AddRegistratr();

            Assert.Contains(services, d => d.ServiceType == typeof(IMarkerSingleton));
            Assert.Contains(services, d => d.ServiceType == typeof(IShortSingleton));
        }

        [Fact]
        public void AddRegistratr_SingleAssemblyOverload_WorksWithConfiguration()
        {
            var services = new ServiceCollection();
            services.AddRegistratr(_testAssembly, options => options.RegisterAsSelf = true);

            Assert.Contains(services, d => d.ServiceType == typeof(MarkerScopedService));
        }
    }
}
