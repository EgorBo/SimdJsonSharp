# SimdJsonSharp: Parsing gigabytes of JSON per second
C# version of Daniel Lemire's [SimdJson](https://github.com/lemire/simdjson) (ported from C by hands, I tried to keep the same format and API).
Accelerated with `System.Runtime.Intrinsics` (e.g. [see here](https://github.com/EgorBo/SimdJsonSharp/blob/master/src/stage1_find_marks.cs)).

## Benchmarks
The following [benchmark](https://github.com/EgorBo/SimdJsonSharp/blob/master/benchmarks/CountTokens.cs) compares `SimdJsonSharp` with .NET Core 3.0 `Utf8JsonReader` - open a file, 
count tokens with type=number, close the file.

```
|            Method |           fileName |    fileSize |         Mean | Ratio |
|------------------ |------------------- |------------ |-------------:|------:|
|  SimdJsonSharpApi | apache_builds.json |   127.28 Kb |     99.28 us |  1.00 |
| Utf8JsonReaderApi | apache_builds.json |   127.28 Kb |    226.42 us |  2.28 |
|           JsonNet | apache_builds.json |   127.28 Kb |    461.30 us |  4.64 |
|      SpanJsonUtf8 | apache_builds.json |   127.28 Kb |    168.08 us |  1.69 |
|                   |                    |             |              |       |
|  SimdJsonSharpApi |        canada.json | 2,251.05 Kb |  4,494.44 us |  1.00 |
| Utf8JsonReaderApi |        canada.json | 2,251.05 Kb |  6,308.01 us |  1.40 |
|           JsonNet |        canada.json | 2,251.05 Kb | 67,718.12 us | 15.06 |
|      SpanJsonUtf8 |        canada.json | 2,251.05 Kb |  6,679.82 us |  1.49 |
|                   |                    |             |              |       |
|  SimdJsonSharpApi |  citm_catalog.json | 1,727.20 Kb |  1,572.78 us |  1.00 |
| Utf8JsonReaderApi |  citm_catalog.json | 1,727.20 Kb |  3,786.10 us |  2.41 |
|           JsonNet |  citm_catalog.json | 1,727.20 Kb |  5,903.38 us |  3.75 |
|      SpanJsonUtf8 |  citm_catalog.json | 1,727.20 Kb |  3,021.13 us |  1.92 |
|                   |                    |             |              |       |
|  SimdJsonSharpApi | github_events.json |    65.13 Kb |     46.01 us |  1.00 |
| Utf8JsonReaderApi | github_events.json |    65.13 Kb |    113.80 us |  2.47 |
|           JsonNet | github_events.json |    65.13 Kb |    214.01 us |  4.65 |
|      SpanJsonUtf8 | github_events.json |    65.13 Kb |     89.09 us |  1.94 |
|                   |                    |             |              |       |
|  SimdJsonSharpApi |     gsoc-2018.json | 3,327.83 Kb |  2,209.42 us |  1.00 |
| Utf8JsonReaderApi |     gsoc-2018.json | 3,327.83 Kb |  4,010.10 us |  1.82 |
|           JsonNet |     gsoc-2018.json | 3,327.83 Kb |  6,729.44 us |  3.05 |
|      SpanJsonUtf8 |     gsoc-2018.json | 3,327.83 Kb |  2,759.59 us |  1.25 |
|                   |                    |             |              |       |
|  SimdJsonSharpApi |   instruments.json |   220.35 Kb |    257.78 us |  1.00 |
| Utf8JsonReaderApi |   instruments.json |   220.35 Kb |    594.22 us |  2.31 |
|           JsonNet |   instruments.json |   220.35 Kb |    980.42 us |  3.80 |
|      SpanJsonUtf8 |   instruments.json |   220.35 Kb |    409.47 us |  1.59 |
|                   |                    |             |              |       |
|  SimdJsonSharpApi |     marine_ik.json | 2,983.47 Kb |  8,510.30 us |  1.00 |
| Utf8JsonReaderApi |     marine_ik.json | 2,983.47 Kb | 11,465.33 us |  1.35 |
|           JsonNet |     marine_ik.json | 2,983.47 Kb | 32,113.43 us |  3.77 |
|      SpanJsonUtf8 |     marine_ik.json | 2,983.47 Kb |  8,885.77 us |  1.04 |
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
