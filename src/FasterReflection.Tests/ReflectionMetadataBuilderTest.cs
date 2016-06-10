using System.Linq;
using FasterReflection.TestAssembly;
using Xunit;
using System.Reflection;

namespace FasterReflection.Tests
{
    public class ReflectionMetadataBuilderTest
    {
        private readonly ReflectionMetadataBuilder _builder;

        public ReflectionMetadataBuilderTest()
        {
            _builder = new ReflectionMetadataBuilder();
            _builder.AddAssemblyByType<TypeWithDefaultConstructor>();
        }

        [Fact]
        public void TypeWithDefaultConstructor()
        {
            var result = _builder.Build();
            var type = GetTypeByNameAndAssert(result, nameof(TypeWithDefaultConstructor));
            Assert.True(type.HasDefaultConstructor);
            Assert.True(type.IsPublic);
            Assert.False(type.IsValueType);
        }

        [Fact]
        public void TypeWithNonDefaultConstructor()
        {
            var result = _builder.Build();
            var type = GetTypeByNameAndAssert(result, nameof(TypeWithNonDefaultConstructor));
            Assert.True(type.HasNonDefaultConstructors);
        }

        [Theory]
        [InlineData(nameof(GenericTypeArity1<object>), 1)]
        [InlineData(nameof(GenericTypeArity2<object, object>), 2)]
        public void TypeWithGenericArguments(string name, int arity)
        {
            var result = _builder.Build();
            var type = GetTypeByNameAndAssert(result, name + "`" + arity);
            Assert.Equal(arity, type.GenericArgumentCount);
        }

        [Fact]
        public void DerivedType()
        {
            var result = _builder.Build();
            var type = GetTypeByNameAndAssert(result, nameof(DerivedType));
            var baseType = type.BaseType;
            Assert.NotNull(baseType);
            Assert.Equal(nameof(BaseType), baseType.Name);
        }

        [Fact]
        public void BaseTypeWithBclBaseTypeWithoutSystemRuntime()
        {
            var result = _builder.Build();
            var type = GetTypeByNameAndAssert(result, nameof(BaseTypeWithBclBaseType));
            var baseType = type.BaseType;
            Assert.Null(baseType);
            Assert.NotEmpty(result.MissingAssembyNames);
        }

        [Fact]
        public void BaseTypeWithBclBaseTypeWithSystemRuntime()
        {
            AddSystemRuntimeAssembly();
            var result = _builder.Build();
            var type = GetTypeByNameAndAssert(result, nameof(BaseTypeWithBclBaseType));
            var baseType = type.BaseType;
            Assert.NotNull(baseType);
            Assert.Equal(nameof(System.EventArgs), baseType.Name);
        }

        [Fact]
        public void DerivedTypeWithBclBaseType()
        {
            AddSystemRuntimeAssembly();
            var result = _builder.Build();
            var type = GetTypeByNameAndAssert(result, nameof(DerivedTypeWithBclBaseType));
            Assert.NotNull(type.BaseType);
            Assert.NotNull(type.BaseType.BaseType);
        }

        [Fact]
        public void InterfaceImplemetingType()
        {
            var result = _builder.Build();
            var type = GetTypeByNameAndAssert(result, nameof(InterfaceImplemetingType));
            Assert.Equal(1, type.Interfaces.Length);
            Assert.Equal(nameof(IInterfaceType), type.Interfaces[0].Name);
        }

        [Fact]
        public void InterfaceImplemetingTypeWithBaseImplementingAnotherInterface()
        {
            var result = _builder.Build();
            var type = GetTypeByNameAndAssert(result, nameof(InterfaceImplemetingTypeWithBaseImplementingAnotherInterface));
            var names = type.AllInterfaces.Select(x => x.Name).ToArray();
            Assert.Contains(nameof(IInterfaceType), names);
            Assert.Contains(nameof(IInterfaceType2), names);
        }

        [Fact]
        public void BclInterfaceImplementingType()
        {
            AddSystemRuntimeAssembly();
            var result = _builder.Build();
            var type = GetTypeByNameAndAssert(result, nameof(BclInterfaceImplementingType));
            Assert.Equal(1, type.Interfaces.Length);
            Assert.Equal(nameof(System.IDisposable), type.Interfaces[0].Name);
        }

        [Fact]
        public void InnerTypeContainerType()
        {
            AddSystemRuntimeAssembly();
            var result = _builder.Build();
            var type = GetTypeByNameAndAssert(result, nameof(TestAssembly.InnerTypeContainerType.InnerTypeLevel1.InnerTypeLevel2));
            Assert.NotNull(type.DeclaringType);
            Assert.Equal(nameof(TestAssembly.InnerTypeContainerType.InnerTypeLevel1), type.DeclaringType.Name);
            Assert.NotNull(type.DeclaringType.DeclaringType);
            Assert.Equal(nameof(InnerTypeContainerType), type.DeclaringType.DeclaringType.Name);
        }

        [Fact]
        public void InternalType()
        {
            var result = _builder.Build();
            var type = GetTypeByNameAndAssert(result, Constants.InternalTypeName);
            Assert.False(type.IsPublic);
        }

        [Fact]
        public void ValueTypeType()
        {
            var result = _builder.Build();
            var type = GetTypeByNameAndAssert(result, nameof(ValueTypeType));
            Assert.True(type.IsValueType);
        }

        private void AddSystemRuntimeAssembly()
        {
            var runtimeAssembly = Assembly.Load(new AssemblyName("System.Runtime, PublicKeyToken=b03f5f7f11d50a3a"));
            _builder.AddReferenceOnlyAssembly(runtimeAssembly.Location);
            _builder.AddReferenceOnlyAssemblyByType<object>();
        }

        private static TypeDefinition GetTypeByNameAndAssert(ReflectionMetadataResult result, string name)
        {
            var types = result.FindTypesByName(name).ToArray();
            Assert.Equal(1, types.Length);
            return types.First();
        }
    }
}