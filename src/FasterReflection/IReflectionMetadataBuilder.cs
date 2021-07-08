namespace FasterReflection
{
    /// <summary>
    /// Reads assembly type metadata.
    /// </summary>
    public interface IReflectionMetadataBuilder
    {
        /// <summary>
        /// Adds an assembly file.
        /// </summary>
        /// <param name="location">The location.</param>
        void AddAssembly(string location);

        /// <summary>
        /// Adds an assembly file for reference only
        /// (its types won't appear in the result, but they may appear as base classes or interfaces).
        /// </summary>
        /// <param name="location">The location.</param>
        void AddReferenceOnlyAssembly(string location);

        /// <summary>
        /// Gets the type definitions and a list of missing assemblies.
        /// </summary>
        /// <param name="ingoreDuplicateAssemblies">Ignores exception when the same assembly gets loaded multiple times</param>
        ReflectionMetadataResult Build(bool ingoreDuplicateAssemblies = false);

        /// <summary>
        /// Determines whether to skip native (C++) types. The default is <c>true</c>.
        /// </summary>
        bool SkipNativeTypes { get; set; }

        /// <summary>
        /// Determines whether to skip compiler-generated types. The default is <c>true</c>.
        /// </summary>
        bool SkipCompilerGeneratedTypes { get; set; }
    }
}