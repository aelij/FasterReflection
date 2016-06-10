using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;

namespace FasterReflection
{
    /// <summary>
    /// Reads assembly type metadata.
    /// </summary>
    public sealed class ReflectionMetadataBuilder : IReflectionMetadataBuilder
    {
        private const string CtorSpecialName = ".ctor";
        private const string TypeSeparator = ".";
        private const string InnerTypeSeparator = "+";
        private const string NativeClassAttributeName = "System.Runtime.CompilerServices.NativeCppClassAttribute";
        private const string CompilerGeneratedNameMarker = "<";
        private const string ValueTypeName = "System.ValueType";
        private const string EnumTypeName = "System.Enum";

        private readonly List<AssemblyData> _assemblies;

        /// <summary>
        /// Determines whether to skip native (C++) types. The default is <c>true</c>.
        /// </summary>
        public bool SkipNativeTypes { get; set; } = true;

        /// <summary>
        /// Determines whether to skip compiler-generated types. The default is <c>true</c>.
        /// </summary>
        public bool SkipCompilerGeneratedTypes { get; set; } = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReflectionMetadataBuilder"/> class.
        /// </summary>
        public ReflectionMetadataBuilder()
        {
            _assemblies = new List<AssemblyData>();
        }

        /// <summary>
        /// Adds an assembly file.
        /// </summary>
        /// <param name="location">The location.</param>
        public void AddAssembly(string location)
        {
            if (location == null) throw new ArgumentNullException(nameof(location));
            _assemblies.Add(new AssemblyData(location, isReferenecOnly: false));
        }

        /// <summary>
        /// Adds an assembly file for reference only
        /// (its types won't appear in the result, but they may appear as base classes or interfaces)
        /// </summary>
        /// <param name="location">The location.</param>
        public void AddReferenceOnlyAssembly(string location)
        {
            if (location == null) throw new ArgumentNullException(nameof(location));
            _assemblies.Add(new AssemblyData(location, isReferenecOnly: true));
        }

        /// <summary>
        /// Gets the type definitions and a list of missing assemblies.
        /// </summary>
        /// <returns></returns>
        public ReflectionMetadataResult Build()
        {
            var typeDictionary = new Dictionary<TypeDefinitionKey, TypeDefinition>();
            var types = ImmutableList.CreateBuilder<TypeDefinition>();
            var missingAssembliesBuilder = ImmutableHashSet.CreateBuilder<string>();

            var assemblies = LoadAssemblies();

            try
            {
                var sortedAssemblies = TopologicalSort(assemblies.Select(pair => pair.Value),
                    assemblyData => assemblyData.References
                        .Select(reference => TryGetAssembly(assemblies, reference, assemblyData.MetadataReader, missingAssembliesBuilder))
                        .Where(data => data != null)).ToArray();

                foreach (var assemblyData in sortedAssemblies)
                {
                    foreach (var handle in assemblyData.MetadataReader.TypeDefinitions)
                    {
                        CreateTypeDefinition(assemblies, assemblyData, handle, typeDictionary, types);
                    }

                    foreach (var handle in assemblyData.MetadataReader.ExportedTypes)
                    {
                        CreateExportTypeDefinition(assemblyData, handle, typeDictionary);
                    }
                }
            }
            finally
            {
                DisposeAssemblies(assemblies);
            }

            return new ReflectionMetadataResult(
                types.ToImmutable(), 
                assemblies.Select(x => x.Value.Assembly).ToImmutableList(),
                missingAssembliesBuilder.ToImmutable());
        }

        private Dictionary<string, LoadedAssembly> LoadAssemblies()
        {
            var assemblies = new Dictionary<string, LoadedAssembly>();
            var exceptions = new List<Exception>();
            foreach (var assemblyData in _assemblies)
            {
                try
                {
                    var loadedAssemly = new LoadedAssembly(assemblyData);
                    var name = loadedAssemly.Assembly.Name;
                    if (assemblies.ContainsKey(name))
                    {
                        throw new InvalidOperationException($"The assembly '{name}' has already been added.");
                    }
                    assemblies.Add(name, loadedAssemly);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }
            if (exceptions.Count > 0)
            {
                DisposeAssemblies(assemblies);
                throw new AggregateException("One or more assemblies failed to load", exceptions);
            }
            return assemblies;
        }

        private static void DisposeAssemblies(Dictionary<string, LoadedAssembly> assemblies)
        {
            foreach (var loadedAssembly in assemblies)
            {
                loadedAssembly.Value.Dispose();
            }
        }

        private static LoadedAssembly TryGetAssembly(Dictionary<string, LoadedAssembly> assemblies, AssemblyReference assemblyReference, MetadataReader metadataReader, ISet<string> missingAssemblies)
        {
            LoadedAssembly assemblyData;
            var assemblyName = metadataReader.GetString(assemblyReference.Name);
            if (!assemblies.TryGetValue(assemblyName, out assemblyData))
            {
                missingAssemblies.Add(assemblyName);
            }
            return assemblyData;
        }

        private void CreateExportTypeDefinition(LoadedAssembly assemblyData, ExportedTypeHandle handle,
            Dictionary<TypeDefinitionKey, TypeDefinition> typeDictionary)
        {
            var exportedType = assemblyData.MetadataReader.GetExportedType(handle);
            if (!exportedType.IsForwarder) return; // TODO: what does this mean?

            var fullName = GetFullName(
                assemblyData.MetadataReader.GetString(exportedType.Namespace),
                assemblyData.MetadataReader.GetString(exportedType.Name), null);

            var definition = exportedType.Implementation;
            Debug.Assert(definition.Kind == HandleKind.AssemblyReference, "definition.Kind == HandleKind.AssemblyReference; actual = " + definition.Kind);

            var assemblyReference = assemblyData.MetadataReader.GetAssemblyReference((AssemblyReferenceHandle)definition);

            var key = new TypeDefinitionKey(assemblyData.MetadataReader.GetString(assemblyReference.Name), fullName);
            TypeDefinition type;
            if (typeDictionary.TryGetValue(key, out type))
            {
                var newKey = new TypeDefinitionKey(assemblyData.Assembly.Name, fullName);
                if (!typeDictionary.ContainsKey(newKey))
                {
                    typeDictionary.Add(newKey, type);
                }
            }
        }
        
        private TypeDefinition CreateTypeDefinition(Dictionary<string, LoadedAssembly> assemblies, LoadedAssembly assemblyData, TypeDefinitionHandle handle, Dictionary<TypeDefinitionKey, TypeDefinition> typeDictionary, IList<TypeDefinition> types)
        {
            var metadataReader = assemblyData.MetadataReader;
            var typeDefinition = metadataReader.GetTypeDefinition(handle);
            var ns = metadataReader.GetString(typeDefinition.Namespace) ?? string.Empty;
            var name = metadataReader.GetString(typeDefinition.Name);

            if (SkipCompilerGeneratedTypes && (ns.Contains(CompilerGeneratedNameMarker) || name.Contains(CompilerGeneratedNameMarker)))
            {
                return null;
            }

            var declaringType = ResolveTypeHandle(assemblies, assemblyData, typeDefinition.GetDeclaringType(), typeDictionary, types);

            var fullName = GetFullName(ns, name, declaringType);

            var key = new TypeDefinitionKey(assemblyData.Assembly.Name, fullName);
            TypeDefinition def;
            if (typeDictionary.TryGetValue(key, out def))
            {
                return def;
            }

            var attributeNames = typeDefinition.GetCustomAttributes()
                .Select(t => GetAttributeTypeName(metadataReader, t)).ToImmutableArray();

            if (SkipNativeTypes && attributeNames.Contains(NativeClassAttributeName))
            {
                // skip native types
                return null;
            }

            string baseTypeFullName;
            var baseType = ResolveTypeHandle(assemblies, assemblyData, typeDefinition.BaseType, typeDictionary, types, out baseTypeFullName);

            var isPublic = HasAttributes(typeDefinition, TypeAttributes.Public);
            var isInterface = HasAttributes(typeDefinition, TypeAttributes.Interface);

            var isValueType = baseTypeFullName != null && (baseTypeFullName.Equals(ValueTypeName, StringComparison.Ordinal) ||
                                                           baseTypeFullName.Equals(EnumTypeName, StringComparison.Ordinal));
            

            var isAbstract = HasAttributes(typeDefinition, TypeAttributes.Abstract);

            var interfaces = typeDefinition.GetInterfaceImplementations()
                    .Select(t => ResolveTypeHandle(assemblies, assemblyData,
                                metadataReader.GetInterfaceImplementation(t).Interface, typeDictionary, types))
                    .Where(t => t != null)
                    .ToImmutableArray();

            var ctors = typeDefinition.GetMethods().Select(metadataReader.GetMethodDefinition)
                .Where(t => !HasAttributes(t, MethodAttributes.Static) &&
                            HasAttributes(t, MethodAttributes.SpecialName) &&
                            metadataReader.StringComparer.Equals(t.Name, CtorSpecialName)).ToArray();

            var hasDefaultCtor = ctors.Any(x => x.GetParameters().Count == 0 && HasAttributes(x, MethodAttributes.Public));
            var hasNonDefaultCtors = ctors.Any(x => x.GetParameters().Count > 0);

            var genericParameterCount = typeDefinition.GetGenericParameters().Count;

            def = new TypeDefinition(assemblyData.Assembly.Name, ns, name, fullName,
                    baseType,
                    declaringType,
                    isPublic,
                    attributeNames,
                    isInterface,
                    interfaces,
                    isValueType,
                    isAbstract,
                    hasDefaultCtor,
                    hasNonDefaultCtors,
                    genericParameterCount);

            typeDictionary.Add(key, def);

            if (!assemblyData.IsReferenecOnly)
            {
                types.Add(def);
            }

            return def;
        }

        private static bool HasAttributes(System.Reflection.Metadata.TypeDefinition type, TypeAttributes attributes)
        {
            return (type.Attributes & attributes) == attributes;
        }

        private static bool HasAttributes(MethodDefinition method, MethodAttributes attributes)
        {
            return (method.Attributes & attributes) == attributes;
        }

        private TypeDefinition ResolveTypeHandle(Dictionary<string, LoadedAssembly> assemblies,
            LoadedAssembly assemblyData, Handle definition, Dictionary<TypeDefinitionKey, TypeDefinition> typeDictionary,
            IList<TypeDefinition> types)
        {
            string referenceFullName;
            return ResolveTypeHandle(assemblies, assemblyData, definition, typeDictionary, types, out referenceFullName);
        }

        private TypeDefinition ResolveTypeHandle(Dictionary<string, LoadedAssembly> assemblies, LoadedAssembly assemblyData, Handle definition, Dictionary<TypeDefinitionKey, TypeDefinition> typeDictionary, IList<TypeDefinition> types, out string referenceFullName)
        {
            referenceFullName = null;

            if (definition.IsNil || definition.Kind == HandleKind.TypeSpecification)
            {
                return null;
            }

            if (definition.Kind == HandleKind.TypeDefinition)
            {
                return CreateTypeDefinition(assemblies, assemblyData, (TypeDefinitionHandle)definition, typeDictionary, types);
            }

            Debug.Assert(definition.Kind == HandleKind.TypeReference, "definition.Kind == HandleKind.TypeReference; actual = " + definition.Kind);

            string referenceAssemblyName;
            GetReferenceFullNameAndAssembly(definition, assemblyData.MetadataReader, out referenceFullName, out referenceAssemblyName);

            if (!assemblies.ContainsKey(referenceAssemblyName))
            {
                return null;
            }

            TypeDefinition def;
            if (!typeDictionary.TryGetValue(new TypeDefinitionKey(referenceAssemblyName, referenceFullName), out def))
            {
                Debug.Assert(false, "Definition does not exist");
            }
            return def;
        }

        private static void GetReferenceFullNameAndAssembly(Handle definition, MetadataReader metadataReader, out string referenceFullName, out string referenceAssemblyName)
        {
            var typeReference = metadataReader.GetTypeReference((TypeReferenceHandle)definition);

            var references = new Stack<TypeReference>();
            references.Push(typeReference);
            while (typeReference.ResolutionScope.Kind == HandleKind.TypeReference)
            {
                typeReference = metadataReader.GetTypeReference((TypeReferenceHandle)typeReference.ResolutionScope);
                references.Push(typeReference);
            }

            var topReference = references.Pop();
            var topReferenceNs = metadataReader.GetString(topReference.Namespace);
            referenceFullName = (topReferenceNs.Length > 0 ? topReferenceNs + TypeSeparator : string.Empty) +
                                metadataReader.GetString(topReference.Name) +
                                (references.Count > 0
                                    ? InnerTypeSeparator +
                                      string.Join(InnerTypeSeparator, references.Select(x => metadataReader.GetString(x.Name)))
                                    : string.Empty);

            Debug.Assert(typeReference.ResolutionScope.Kind == HandleKind.AssemblyReference,
                "typeReference.ResolutionScope.Kind == HandleKind.AssemblyReference; actual = " +
                typeReference.ResolutionScope.Kind);

            var assemblyReference = metadataReader.GetAssemblyReference((AssemblyReferenceHandle)typeReference.ResolutionScope);
            referenceAssemblyName = metadataReader.GetString(assemblyReference.Name);
        }

        private static string GetFullName(string ns, string name, TypeDefinition declaringType)
        {
            var fullName = new StringBuilder(
                (declaringType?.FullName.Length ?? 0) + InnerTypeSeparator.Length +
                ns.Length + TypeSeparator.Length + name.Length);
            if (declaringType != null)
            {
                fullName.Append(declaringType.FullName);
                fullName.Append(InnerTypeSeparator);
            }
            else if (ns.Length > 0)
            {
                fullName.Append(ns);
                fullName.Append(TypeSeparator);
            }
            fullName.Append(name);
            return fullName.ToString();
        }

        private static string GetAttributeTypeName(MetadataReader metadataReader, CustomAttributeHandle customAttributeHandle)
        {
            var ctor = metadataReader.GetCustomAttribute(customAttributeHandle).Constructor;

            string ns, name;
            if (ctor.Kind == HandleKind.MethodDefinition)
            {
                var type = metadataReader.GetTypeDefinition(
                    metadataReader.GetMethodDefinition((MethodDefinitionHandle)ctor).GetDeclaringType());
                ns = metadataReader.GetString(type.Namespace);
                name = metadataReader.GetString(type.Name);
            }
            else
            {
                var parent = metadataReader.GetMemberReference((MemberReferenceHandle)ctor).Parent;
                Debug.Assert(parent.Kind == HandleKind.TypeReference, "parent.Kind == HandleKind.TypeReference; actual = " + parent.Kind);

                var type = metadataReader.GetTypeReference((TypeReferenceHandle)parent);
                ns = metadataReader.GetString(type.Namespace);
                name = metadataReader.GetString(type.Name);
            }

            return ns + TypeSeparator + name;
        }

        private static IEnumerable<T> TopologicalSort<T>(IEnumerable<T> nodes,
                                                Func<T, IEnumerable<T>> connected)
        {
            var elems = nodes.ToDictionary(node => node,
                                           node => new HashSet<T>(connected(node)));
            while (elems.Count > 0)
            {
                var found = elems.Where(x => x.Value.Count == 0).ToArray();
                if (found.Length == 0)
                {
                    throw new InvalidOperationException("Cyclic assembly references are not allowed");
                }
                var key = found[0].Key;
                elems.Remove(key);
                foreach (var selem in elems)
                {
                    selem.Value.Remove(key);
                }
                yield return key;
            }
        }

        private struct TypeDefinitionKey : IEquatable<TypeDefinitionKey>
        {
            private readonly string _assembly;
            private readonly string _fullName;

            public TypeDefinitionKey(string assembly, string fullName)
            {
                _assembly = assembly;
                _fullName = fullName;
            }

            public bool Equals(TypeDefinitionKey other)
            {
                return string.Equals(_assembly, other._assembly) && string.Equals(_fullName, other._fullName);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                return obj is TypeDefinitionKey && Equals((TypeDefinitionKey)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = _assembly.GetHashCode();
                    hashCode = (hashCode * 397) ^ _fullName.GetHashCode();
                    return hashCode;
                }
            }

            public static bool operator ==(TypeDefinitionKey left, TypeDefinitionKey right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(TypeDefinitionKey left, TypeDefinitionKey right)
            {
                return !left.Equals(right);
            }

            public override string ToString()
            {
                return $"Assembly: {_assembly}, FullName: {_fullName}";
            }
        }

        private class AssemblyData
        {
            public string Location { get; }
            public bool IsReferenecOnly { get; }

            public AssemblyData(string location, bool isReferenecOnly)
            {
                Location = location;
                IsReferenecOnly = isReferenecOnly;
            }
        }

        private class LoadedAssembly : IDisposable
        {
            private readonly FileStream _fileStream;
            private readonly PEReader _peReader;

            public LoadedAssembly(AssemblyData assemblyData)
            {
                IsReferenecOnly = assemblyData.IsReferenecOnly;
                _fileStream = new FileStream(assemblyData.Location, FileMode.Open, FileAccess.Read);
                _peReader = new PEReader(_fileStream, PEStreamOptions.LeaveOpen);
                MetadataReader = _peReader.GetMetadataReader();
                var assemblyDefinition = MetadataReader.GetAssemblyDefinition();
                Assembly = new AssemblyDefinition(MetadataReader.GetString(assemblyDefinition.Name),
                    assemblyDefinition.Version);
                References = MetadataReader.AssemblyReferences.Select(t => MetadataReader.GetAssemblyReference(t)).ToImmutableArray();
            }

            public MetadataReader MetadataReader { get; }

            public AssemblyDefinition Assembly { get; }

            public ImmutableArray<AssemblyReference> References { get; }

            public bool IsReferenecOnly { get; }

            public void Dispose()
            {
                _peReader.Dispose();
                _fileStream.Dispose();
            }
        }
    }
}