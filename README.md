# SimdJsonSharp: Parsing gigabytes of JSON per second
C# version of Daniel Lemire's [SimdJson](https://github.com/lemire/simdjson) (ported from C by hands, I tried to keep the same format and API).
Accelerated with `System.Runtime.Intrinsics` (e.g. [see here](https://github.com/EgorBo/SimdJsonSharp/blob/master/src/stage1_find_marks.cs)).

## Benchmarks
The following [benchmark](https://github.com/EgorBo/SimdJsonSharp/blob/master/benchmarks/CountTokens.cs) compares `SimdJsonSharp` with .NET Core 3.0 `Utf8JsonReader` - open a file, 
count tokens with type=number, close the file.

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
