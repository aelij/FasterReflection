# FasterReflection
Utilizes System.Reflection.Metadata to read type information very fast

## Example

```csharp
public class MyType { }

var builder = new ReflectionMetadataBuilder();
builder.AddAssemblyByType<MyType>();
builder.AddReferenceOnlyAssemblyByType<object>();
var result = builder.Build();
var myType = result.FindTypesByName("MyType").First();

Console.WriteLine(myType.BaseType.FullName);          // 'System.Object'
Console.WriteLine(myType.IsPublic);                   // 'True'
Console.WriteLine(myType.HasNonDefaultConstructors);  // 'False'
Console.WriteLine(myType.GenericArgumentCount);       // '0'
```