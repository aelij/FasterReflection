using System;

namespace FasterReflection
{
    /// <summary>
    /// Assembly metadata
    /// </summary>
    public sealed class AssemblyDefinition
    {
        internal AssemblyDefinition(string name, Version version, string location)
        {
            Name = name;
            Version = version;
            Location = location;
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the version.
        /// </summary>
        public Version Version { get; }
        public string Location { get; }
    }
}