using System;
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
            builder.AddAssembly(GetLocation<T>());
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
            builder.AddReferenceOnlyAssembly(GetLocation<T>());
        }

        private static string GetLocation<T>()
        {
            return typeof(T).GetTypeInfo().Assembly.Location;
        }
    }
}