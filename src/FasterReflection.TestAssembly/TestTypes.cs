// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedTypeParameter

namespace FasterReflection.TestAssembly
{
    public class TypeWithDefaultConstructor { }

    public class TypeWithNonDefaultConstructor
    {
        public TypeWithNonDefaultConstructor(bool b) { }
    }

    public class GenericTypeArity1<T> { }

    public class GenericTypeArity2<T1, T2> { }

    public class BaseType { }

    public class BaseTypeWithBclBaseType : System.EventArgs { }

    public class DerivedType : BaseType { }

    public class DerivedTypeWithBclBaseType : BaseTypeWithBclBaseType { }

    public interface IInterfaceType { }

    public interface IInterfaceType2 { }

    public class InterfaceImplemetingType : IInterfaceType { }

    public class InterfaceImplemetingTypeWithBaseImplementingAnotherInterface : InterfaceImplemetingType, IInterfaceType2 { }

    public class BclInterfaceImplementingType : System.IDisposable
    {
        public void Dispose() { }
    }

    public class InnerTypeContainerType
    {
        public class InnerTypeLevel1
        {
            public class InnerTypeLevel2 { }
        }
    }

    internal class InternalType { }

    public struct ValueTypeType { }

    public static class Constants
    {
        public const string InternalTypeName = nameof(InternalType);
    }
}