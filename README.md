# SimdJsonSharp: Parsing gigabytes of JSON per second
C# version of [lemire/simdjson](https://github.com/lemire/simdjson) (by Daniel Lemire and Geoff Langdale - https://arxiv.org/abs/1902.08318) fully ported from C to C#, 
I tried to keep the same format and API). The library accelerates JSON parsing and minification using 
SIMD instructions (AVX2). C# version uses `System.Runtime.Intrinsics` API.

**UPD:** Now it's also available as a set of pinvokes on top of the native lib as a .NETStandard 2.0 library, 
thus there are two implementations:
1) [1.5.0](https://www.nuget.org/packages/SimdJsonSharp.Managed) Fully managed netcoreapp3.0 library (100% port from C to C#)
2) [1.7.0](https://www.nuget.org/packages/SimdJsonSharp.Bindings) netstandard2.0 library with native lib (bindings are generated via [xoofx/CppAst](https://github.com/xoofx/CppAst))

## Benchmarks
The following [benchmark](https://github.com/EgorBo/SimdJsonSharp/blob/master/benchmarks/CountTokens.cs) compares `SimdJsonSharp` with .NET Core 3.0 `Utf8JsonReader`, `Json.NET` and `SpanJson` libraries.
Test json files can be found [here](https://github.com/lemire/simdjson/tree/master/jsonexamples).

### 1. Parse doubles
Open [canada.json](https://raw.githubusercontent.com/lemire/simdjson/master/jsonexamples/canada.json) and parse all coordinates as `System.Double`:
```
|          Method |     fileName |    fileSize |      Mean | Ratio |
|---------------- |------------- |-------------|----------:|------:|
|        SimdJson |  canada.json | 2,251.05 Kb |  4,733 ms |  1.00 |
|  Utf8JsonReader |  canada.json | 2,251.05 Kb | 56,692 ms | 11.98 |
|         JsonNet |  canada.json | 2,251.05 Kb | 70,078 ms | 14.81 |
|    SpanJsonUtf8 |  canada.json | 2,251.05 Kb | 54,878 ms | 11.60 |
```

### 2. Count all tokens
```
|            Method |           fileName |    fileSize |         Mean | Ratio |
|------------------ |------------------- |------------ |-------------:|------:|
|          SimdJson | apache_builds.json |   127.28 Kb |     99.28 us |  1.00 |
|    Utf8JsonReader | apache_builds.json |   127.28 Kb |    226.42 us |  2.28 |
|           JsonNet | apache_builds.json |   127.28 Kb |    461.30 us |  4.64 |
|      SpanJsonUtf8 | apache_builds.json |   127.28 Kb |    168.08 us |  1.69 |
|                   |                    |             |              |       |
|          SimdJson |        canada.json | 2,251.05 Kb |  4,494.44 us |  1.00 |
|    Utf8JsonReader |        canada.json | 2,251.05 Kb |  6,308.01 us |  1.40 |
|           JsonNet |        canada.json | 2,251.05 Kb | 67,718.12 us | 15.06 |
|      SpanJsonUtf8 |        canada.json | 2,251.05 Kb |  6,679.82 us |  1.49 |
|                   |                    |             |              |       |
|          SimdJson |  citm_catalog.json | 1,727.20 Kb |  1,572.78 us |  1.00 |
|    Utf8JsonReader |  citm_catalog.json | 1,727.20 Kb |  3,786.10 us |  2.41 |
|           JsonNet |  citm_catalog.json | 1,727.20 Kb |  5,903.38 us |  3.75 |
|      SpanJsonUtf8 |  citm_catalog.json | 1,727.20 Kb |  3,021.13 us |  1.92 |
|                   |                    |             |              |       |
|          SimdJson | github_events.json |    65.13 Kb |     46.01 us |  1.00 |
|    Utf8JsonReader | github_events.json |    65.13 Kb |    113.80 us |  2.47 |
|           JsonNet | github_events.json |    65.13 Kb |    214.01 us |  4.65 |
|      SpanJsonUtf8 | github_events.json |    65.13 Kb |     89.09 us |  1.94 |
|                   |                    |             |              |       |
|          SimdJson |     gsoc-2018.json | 3,327.83 Kb |  2,209.42 us |  1.00 |
|    Utf8JsonReader |     gsoc-2018.json | 3,327.83 Kb |  4,010.10 us |  1.82 |
|           JsonNet |     gsoc-2018.json | 3,327.83 Kb |  6,729.44 us |  3.05 |
|      SpanJsonUtf8 |     gsoc-2018.json | 3,327.83 Kb |  2,759.59 us |  1.25 |
|                   |                    |             |              |       |
|          SimdJson |   instruments.json |   220.35 Kb |    257.78 us |  1.00 |
|    Utf8JsonReader |   instruments.json |   220.35 Kb |    594.22 us |  2.31 |
|           JsonNet |   instruments.json |   220.35 Kb |    980.42 us |  3.80 |
|      SpanJsonUtf8 |   instruments.json |   220.35 Kb |    409.47 us |  1.59 |
|                   |                    |             |              |       |
|          SimdJson |      truenull.json |    12.00 Kb |  16,032.6 ns |  1.00 |
|    Utf8JsonReader |      truenull.json |    12.00 Kb |  58,365.2 ns |  3.64 |
|           JsonNet |      truenull.json |    12.00 Kb |  60,977.3 ns |  3.80 |
|      SpanJsonUtf8 |      truenull.json |    12.00 Kb |  24,069.2 ns |  1.50 |
```
### 3. Json minification:
```
|                Method |           fileName |    fileSize |         Mean | Ratio |
|---------------------- |------------------- |------------ |-------------:|------:|
|  SimdJsonNoValidation | apache_builds.json |   127.28 Kb |     186.8 us |  1.00 |
|              SimdJson | apache_builds.json |   127.28 Kb |     262.5 us |  1.41 |
|               JsonNet | apache_builds.json |   127.28 Kb |   1,802.6 us |  9.65 |
|                       |                    |             |              |       |
|  SimdJsonNoValidation |        canada.json | 2,251.05 Kb |   4,130.7 us |  1.00 |
|              SimdJson |        canada.json | 2,251.05 Kb |   7,940.7 us |  1.92 |
|               JsonNet |        canada.json | 2,251.05 Kb | 181,884.0 us | 44.06 |
|                       |                    |             |              |       |
|  SimdJsonNoValidation |  citm_catalog.json | 1,727.20 Kb |   2,346.9 us |  1.00 |
|              SimdJson |  citm_catalog.json | 1,727.20 Kb |   4,064.0 us |  1.75 |
|               JsonNet |  citm_catalog.json | 1,727.20 Kb |  34,831.0 us | 14.84 |
```

## Usage
The C# API is not stable yet and currently fully copies the original C-style API
thus it involves some `Unsafe` magic including pointers.

Add nuget package [SimdJsonSharp.Managed](https://www.nuget.org/packages/SimdJsonSharp.Managed) (for .NET Core 3.0)
or [SimdJsonSharp.Bindings](https://www.nuget.org/packages/SimdJsonSharp.Bindings) for a .NETStandard 2.0 package (.NET 4.x, .NET Core 2.x, etc).
```
dotnet add package SimdJsonSharp.Bindings
or
dotnet add package SimdJsonSharp.Managed
```

The following sample parses a file and iterate numeric tokens
```csharp
byte[] bytes = File.ReadAllBytes(somefile);
fixed (byte* ptr = bytes) // pin bytes while we are working on them
using (ParsedJson doc = SimdJson.ParseJson(ptr, bytes.Length))
using (var iterator = doc.CreateIterator())
{
    while (iterator.MoveForward())
    {
        if (iterator.GetTokenType() == JsonTokenType.Number)
            Console.WriteLine("integer: " + iterator.GetInteger());
    }
}
```
**UPD:** for `SimdJsonSharp.Bindings` types are postfixed with 'N', e.g. `ParsedJsonN`

As you can see the API looks similiar to `Utf8JsonReader` that was introduced recently in .NET Core 3.0

Also it's possible to just validate JSON or minify it (remove whitespaces, etc):
```csharp
string someJson = ...;
string minifiedJson = SimdJson.MinifyJson(someJson);
```

## Requirements
* AVX2 enabled CPU 
