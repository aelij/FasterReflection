using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace FasterReflection
{
    /// <summary>
    /// Represents the result of a scan by a <see cref="IReflectionMetadataBuilder"/>.
    /// </summary>
    public sealed class ReflectionMetadataResult
    {
        internal ReflectionMetadataResult(ImmutableList<TypeDefinition> typeDefinitions, ImmutableList<AssemblyDefinition> assemblyDefinitions, ImmutableHashSet<string> missingAssembyNames)
        {
            TypeDefinitions = typeDefinitions;
            AssemblyDefinitions = assemblyDefinitions;
            MissingAssembyNames = missingAssembyNames;
        }

        /// <summary>
        /// Gets the type definitions.
        /// </summary>
        public ImmutableList<TypeDefinition> TypeDefinitions { get; }

        /// <summary>
        /// Gets the assembly definitions.
        /// </summary>
        public ImmutableList<AssemblyDefinition> AssemblyDefinitions { get; }

        /// <summary>
        /// Gets assemblies referenced by added assemblies but missing from the scan.
        /// </summary>
        public ImmutableHashSet<string> MissingAssembyNames { get; }

        /// <summary>
        /// Finds types that match the given <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public IEnumerable<TypeDefinition> FindTypesByName(string name)
        {
            return TypeDefinitions.Where(x => x.Name == name);
        }

        /// <summary>
        /// Finds types that match the given <paramref name="fullName"/>.
        /// </summary>
        /// <param name="fullName">The full name.</param>
        /// <returns></returns>
        public IEnumerable<TypeDefinition> FindTypesByFullName(string fullName)
        {
            return TypeDefinitions.Where(x => x.FullName == fullName);
        }
    }
}