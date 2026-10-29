namespace Registratr
{
    /// <summary>
    /// Strategy to use when registering services that may already be present in the service collection.
    /// </summary>
    public enum DuplicateStrategy
    {
        /// <summary>
        /// Appends the service registration (standard IServiceCollection behavior allowing multiple implementations).
        /// </summary>
        Append,

        /// <summary>
        /// Only adds the service descriptor if the service type has not already been registered.
        /// </summary>
        TryAdd
    }

    /// <summary>
    /// Options to configure the convention-based registration behavior.
    /// </summary>
    public class RegistrationOptions
    {
        /// <summary>
        /// Gets or sets whether concrete classes should also be registered as themselves in addition to their interfaces.
        /// Default is false.
        /// </summary>
        public bool RegisterAsSelf { get; set; } = false;

        /// <summary>
        /// Gets or sets the strategy for handling duplicate service registrations.
        /// Default is <see cref="DuplicateStrategy.Append"/>.
        /// </summary>
        public DuplicateStrategy DuplicateStrategy { get; set; } = DuplicateStrategy.Append;

        /// <summary>
        /// Gets or sets whether open generic types (e.g., Repository&lt;&gt; implementing IRepository&lt;&gt;) are registered.
        /// Default is true.
        /// </summary>
        public bool IncludeOpenGenerics { get; set; } = true;

        /// <summary>
        /// Optional predicate to filter types discovered during assembly scanning.
        /// Return true to include the type, false to skip it.
        /// </summary>
        public Func<Type, bool>? TypeFilter { get; set; }
    }
}
