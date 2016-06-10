# FasterReflection
Utilizes System.Reflection.Metadata to read type information very fast

## NuGet

```powershell
Install-Package FasterReflection
```

## Example

```csharp
// type defined in MyAssembly.dll
public class MyType { }

var builder = new ReflectionMetadataBuilder();
builder.AddAssembly("MyAssembly.dll");
builder.AddReferenceOnlyAssemblyByType<object>(); // adds mscorlib
var result = builder.Build();
var myType = result.FindTypesByName("MyType").First();

Console.WriteLine(myType.BaseType.FullName);          // 'System.Object'
Console.WriteLine(myType.IsPublic);                   // 'True'
Console.WriteLine(myType.HasNonDefaultConstructors);  // 'False'
Console.WriteLine(myType.GenericArgumentCount);       // '0'
```