# SimdJsonSharp: Parsing gigabytes of JSON per second
C# version of Daniel Lemire's [SimdJson](https://github.com/lemire/simdjson) (ported from C by hands, I tried to keep the same format and API).
Accelerated with `System.Runtime.Intrinsics` (e.g. [see here](https://github.com/EgorBo/SimdJsonSharp/blob/master/src/stage1_find_marks.cs)).

## Benchmarks
The following [benchmark](https://github.com/EgorBo/SimdJsonSharp/blob/master/benchmarks/CountTokens.cs) compares `SimdJsonSharp` with .NET Core 3.0 `Utf8JsonReader` - open a file, 
count tokens with type=number, close the file.

Tiered Compilation is OFF:
```
|            Method |          data |           fileName |         Mean |
|------------------ |-------------- |------------------- |-------------:|
|  SimdJsonSharpApi | System.Byte[] | apache_builds.json |    151.53 us |
| Utf8JsonReaderApi | System.Byte[] | apache_builds.json |    301.60 us |
|           JsonNet | System.Byte[] | apache_builds.json |    460.96 us |
|                   |               |                    |              |
|  SimdJsonSharpApi | System.Byte[] |        canada.json |  7,017.27 us |
| Utf8JsonReaderApi | System.Byte[] |        canada.json | 11,257.50 us |
|           JsonNet | System.Byte[] |        canada.json | 71,259.25 us |
|                   |               |                    |              |
|  SimdJsonSharpApi | System.Byte[] |  citm_catalog.json |  2,221.63 us |
| Utf8JsonReaderApi | System.Byte[] |  citm_catalog.json |  4,693.20 us |
|           JsonNet | System.Byte[] |  citm_catalog.json |  5,948.48 us |
|                   |               |                    |              |
|  SimdJsonSharpApi | System.Byte[] | github_events.json |     67.70 us |
| Utf8JsonReaderApi | System.Byte[] | github_events.json |    137.86 us |
|           JsonNet | System.Byte[] | github_events.json |    216.27 us |
|                   |               |                    |              |
|  SimdJsonSharpApi | System.Byte[] |     gsoc-2018.json |  2,597.25 us |
| Utf8JsonReaderApi | System.Byte[] |     gsoc-2018.json |  4,485.91 us |
|           JsonNet | System.Byte[] |     gsoc-2018.json |  6,681.71 us |
```

Tiered Compilation is ON:
```
|            Method |          data |           fileName |         Mean |
|------------------ |-------------- |------------------- |-------------:|
|  SimdJsonSharpApi | System.Byte[] | apache_builds.json |    146.42 us |
| Utf8JsonReaderApi | System.Byte[] | apache_builds.json |    225.74 us |
|           JsonNet | System.Byte[] | apache_builds.json |    459.89 us |
|                   |               |                    |              |
|  SimdJsonSharpApi | System.Byte[] |        canada.json |  6,655.92 us |
| Utf8JsonReaderApi | System.Byte[] |        canada.json |  6,430.60 us |
|           JsonNet | System.Byte[] |        canada.json | 69,500.56 us |
|                   |               |                    |              |
|  SimdJsonSharpApi | System.Byte[] |  citm_catalog.json |  2,168.55 us |
| Utf8JsonReaderApi | System.Byte[] |  citm_catalog.json |  3,781.00 us |
|           JsonNet | System.Byte[] |  citm_catalog.json |  5,881.58 us |
|                   |               |                    |              |
|  SimdJsonSharpApi | System.Byte[] | github_events.json |     65.62 us |
| Utf8JsonReaderApi | System.Byte[] | github_events.json |    110.89 us |
|           JsonNet | System.Byte[] | github_events.json |    214.77 us |
|                   |               |                    |              |
|  SimdJsonSharpApi | System.Byte[] |     gsoc-2018.json |  2,494.75 us |
| Utf8JsonReaderApi | System.Byte[] |     gsoc-2018.json |  4,736.56 us |
|           JsonNet | System.Byte[] |     gsoc-2018.json |  6,722.00 us |
```

Environment:
```
// * Summary *

BenchmarkDotNet=v0.11.4, OS=Windows 10.0.17134.590 (1803/April2018Update/Redstone4)
Intel Core i7-8700K CPU 3.70GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
Frequency=3609380 Hz, Resolution=277.0559 ns, Timer=TSC
.NET Core SDK=3.0.100-preview4-010487
  [Host] : .NET Core 3.0.0-preview3-27420-6 (CoreCLR 4.6.27415.73, CoreFX 4.7.19.11509), 64bit RyuJIT
  Core   : .NET Core 3.0.0-preview3-27420-6 (CoreCLR 4.6.27415.73, CoreFX 4.7.19.11509), 64bit RyuJIT
```
