## SimdJsonSharp
C# version of Daniel Lemire's [SimdJson](https://github.com/lemire/simdjson) (ported from C by hands, I tried to keep the same format and API).
Accelerated with `System.Runtime.Intrinsics` (e.g. [see here](https://github.com/EgorBo/SimdJsonSharp/blob/master/src/stage1_find_marks.cs)).

## Benchmarks
The following [benchmark](https://github.com/EgorBo/SimdJsonSharp/blob/master/benchmarks/CountTokens.cs) compares `SimdJsonSharp` with .NET Core 3.0 `Utf8JsonReader` - open a file, 
count tokens with type=number, close the file.

```
|            Method |          data |           fileName |         Mean |       Error |     StdDev | Ratio | RatioSD |
|------------------ |-------------- |------------------- |-------------:|------------:|-----------:|------:|--------:|
|  SimdJsonSharpApi | System.Byte[] | apache_builds.json |    146.84 us |   1.7012 us |  0.4418 us |  1.00 |    0.00 |
| Utf8JsonReaderApi | System.Byte[] | apache_builds.json |    302.01 us |   0.5280 us |  0.1371 us |  2.06 |    0.01 |
|                   |               |                    |              |             |            |       |         |
|  SimdJsonSharpApi | System.Byte[] |        canada.json |  6,853.05 us |  22.6816 us |  5.8903 us |  1.00 |    0.00 |
| Utf8JsonReaderApi | System.Byte[] |        canada.json | 11,294.01 us |  99.9135 us | 25.9472 us |  1.65 |    0.00 |
|                   |               |                    |              |             |            |       |         |
|  SimdJsonSharpApi | System.Byte[] |  citm_catalog.json |  2,205.75 us | 115.4415 us | 29.9798 us |  1.00 |    0.00 |
| Utf8JsonReaderApi | System.Byte[] |  citm_catalog.json |  4,630.50 us |   2.4387 us |  0.6333 us |  2.10 |    0.03 |
|                   |               |                    |              |             |            |       |         |
|  SimdJsonSharpApi | System.Byte[] | github_events.json |     67.65 us |   2.5713 us |  0.6678 us |  1.00 |    0.00 |
| Utf8JsonReaderApi | System.Byte[] | github_events.json |    138.50 us |   0.1843 us |  0.0479 us |  2.05 |    0.02 |
|                   |               |                    |              |             |            |       |         |
|  SimdJsonSharpApi | System.Byte[] |     gsoc-2018.json |  2,580.48 us |   8.6610 us |  2.2492 us |  1.00 |    0.00 |
| Utf8JsonReaderApi | System.Byte[] |     gsoc-2018.json |  4,506.78 us |  28.0399 us |  7.2819 us |  1.75 |    0.00 |
```
