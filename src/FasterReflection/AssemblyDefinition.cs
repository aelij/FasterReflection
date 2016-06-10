using System;

namespace FasterReflection
{
    /// <summary>
    /// Assembly metadata
    /// </summary>
    public sealed class AssemblyDefinition
    {
        internal AssemblyDefinition(string name, Version version)
        {
            Name = name;
            Version = version;
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the version.
        /// </summary>
        public Version Version { get; }
    }
}