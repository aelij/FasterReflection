using System;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;

namespace FasterReflection
{
    /// <summary>
    /// Provides extensions methods for <see cref="IReflectionMetadataBuilder"/>.
    /// </summary>
    public static class ReflectionMetadataBuilderExtensions
    {
        /// <summary>
        /// Adds the assembly that contains the type specified by <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder">The builder.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void AddAssemblyByType<T>(this IReflectionMetadataBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            var location = GetLocation<T>();
            builder.AddAssembly(location);
            foreach (var additionalPath in GetAdditionalPaths<T>(location))
            {
                builder.AddAssembly(additionalPath);
            }
        }

        /// <summary>
        /// Adds the assembly that contains the type specified by <typeparamref name="T"/>for reference only
        /// (its types won't appear in the result, but they may appear as base classes or interfaces).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder">The builder.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void AddReferenceOnlyAssemblyByType<T>(this IReflectionMetadataBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            var location = GetLocation<T>();
            builder.AddReferenceOnlyAssembly(location);
            foreach (var additionalPath in GetAdditionalPaths<T>(location))
            {
                builder.AddReferenceOnlyAssembly(additionalPath);
            }
        }

        /// <summary>
        /// For types in the corlib also add mscorlib and System.Runtime assemblies.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="location"></param>
        /// <returns></returns>
        private static ImmutableArray<string> GetAdditionalPaths<T>(string location)
        {
            // TODO: this is a workaround.
            // a better solution might be to try loading additional referenced assemblies in
            // ReflectionMetadataBuilder.TryGetAssembly
            var assembly = typeof(T).GetTypeInfo().Assembly;
            if (assembly.FullName == typeof(object).GetTypeInfo().Assembly.FullName)
            {
                var path = Path.GetDirectoryName(location);
                var builder = ImmutableArray.CreateBuilder<string>();

                if (assembly.GetName().Name != "mscorlib")
                {
                    var mscorlib = Path.Combine(path, "mscorlib.dll");
                    if (File.Exists(mscorlib))
                    {
                        builder.Add(mscorlib);
                    }
                }

                var systemRuntime = Path.Combine(path, "System.Runtime.dll");
                if (File.Exists(systemRuntime))
                {
                    builder.Add(systemRuntime);
                }

                return builder.ToImmutable();
            }
            return ImmutableArray<string>.Empty;
        }

        private static string GetLocation<T>()
        {
            return typeof(T).GetTypeInfo().Assembly.Location;
        }
    }
}