using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace FasterReflection
{
    /// <summary>
    /// Represents a type definition.
    /// </summary>
    public sealed class TypeDefinition
    {
        private ImmutableArray<TypeDefinition> _allInterfaces;

        internal TypeDefinition(string assemblyName, string ns, string name, string fullName, TypeDefinition baseType, TypeDefinition declaringType, bool isPublic, ImmutableArray<string> attributeTypes, bool isInterface, ImmutableArray<TypeDefinition> interfaces, bool isValueType, bool isAbstract, bool hasDefaultConstructor, bool hasNonDefaultConstructors, int genericArgumentCount)
        {
            AssemblyName = assemblyName;
            Namespace = ns;
            Name = name;
            FullName = fullName;
            BaseType = baseType;
            DeclaringType = declaringType;
            IsPublic = isPublic;
            AttributeTypes = attributeTypes;
            IsInterface = isInterface;
            Interfaces = interfaces;
            IsValueType = isValueType;
            IsAbstract = isAbstract;
            HasDefaultConstructor = hasDefaultConstructor;
            HasNonDefaultConstructors = hasNonDefaultConstructors;
            GenericArgumentCount = genericArgumentCount;
        }

        private ImmutableArray<TypeDefinition> GetAllInterfaces()
        {
            var interfaces = new HashSet<TypeDefinition>();
            var t = this;
            do
            {
                foreach (var @interface in t.Interfaces)
                {
                    interfaces.Add(@interface);
                }
                t = t.BaseType;
            } while (t != null);
            return interfaces.ToImmutableArray();
        }

        /// <summary>
        /// Gets the base type.
        /// </summary>
        public TypeDefinition BaseType { get; }

        /// <summary>
        /// Gets the name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the namespace.
        /// </summary>
        public string Namespace { get; }

        /// <summary>
        /// Gets the name of the assembly.
        /// </summary>
        public string AssemblyName { get; }

        /// <summary>
        /// Gets the attribute types.
        /// </summary>
        public ImmutableArray<string> AttributeTypes { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is interface.
        /// </summary>
        public bool IsInterface { get; }

        /// <summary>
        /// Gets the interfaces directly implemented by this type.
        /// </summary>
        public ImmutableArray<TypeDefinition> Interfaces { get; }

        /// <summary>
        /// Gets all interfaces inmplemented by this type.
        /// </summary>
        public ImmutableArray<TypeDefinition> AllInterfaces
        {
            get
            {
                if (_allInterfaces == null)
                {
                    _allInterfaces = GetAllInterfaces();
                }
                return _allInterfaces;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is a value type.
        /// </summary>
        public bool IsValueType { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is abstract.
        /// </summary>
        public bool IsAbstract { get; }

        /// <summary>
        /// Gets a value indicating whether this instance has a default constructor.
        /// </summary>
        public bool HasDefaultConstructor { get; }

        /// <summary>
        /// Gets a value indicating whether this instance has non default constructors.
        /// </summary>
        public bool HasNonDefaultConstructors { get; }

        /// <summary>
        /// Gets the generic argument count.
        /// </summary>
        public int GenericArgumentCount { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is public.
        /// </summary>
        public bool IsPublic { get; }

        /// <summary>
        /// Gets the declaring type of a nested type.
        /// </summary>
        public TypeDefinition DeclaringType { get; }

        /// <summary>
        /// Gets the full name of the type.
        /// </summary>
        public string FullName { get; }

        /// <summary>
        /// Returns a <see cref="string" /> that represents this instance (same as <see cref="FullName"/>).
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return FullName;
        }

        /// <summary>
        /// Determines whether this type is assignable from the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        /// <remarks>
        /// This method uses reference equality to check type equality,
        /// so both types must be from the same <see cref="ReflectionMetadataResult"/>.
        /// </remarks>
        /// <exception cref="ArgumentNullException"></exception>
        public bool IsAssignableFrom(TypeDefinition type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            if (type.IsInterface)
            {
                return type.AllInterfaces.Contains(this);
            }

            do
            {
                if (type == this)
                {
                    return true;
                }
                type = type.BaseType;
            } while (type != null);

            return false;
        }
    }
}