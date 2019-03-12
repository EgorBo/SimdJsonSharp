using System;
using System.Text;
using BenchmarkDotNet.Attributes;
using Newtonsoft.Json.Linq;
using SimdJsonSharp;

namespace Benchmarks
{
    public class MinifyBenchmarks : BenchmarksBase
    {
        [Benchmark(Baseline = true)]
        [ArgumentsSource(nameof(TestData))]
        public string _SimdJsonWithoutValidation(byte[] jsonData, string fileName, string fileSize)
        {
            string json = Encoding.UTF8.GetString(jsonData);
            return SimdJson.MinifyJson(json);
        }

        //[Benchmark]
        [ArgumentsSource(nameof(TestData))]
        public string _SimdJsonNativeWithoutValidation(byte[] jsonData, string fileName, string fileSize)
        {
            string json = Encoding.UTF8.GetString(jsonData);
            return SimdJsonN.MinifyJson(json);
        }

        [Benchmark]
        [ArgumentsSource(nameof(TestData))]
        public unsafe string _SimdJson(byte[] jsonData, string fileName, string fileSize)
        {
            string json = Encoding.UTF8.GetString(jsonData);

            // Validate json first
            // this step is not required for minification, it's here because JSON.NET also does validation
            fixed (byte* dataPtr = jsonData)
            {
                using (var doc = SimdJson.ParseJson(dataPtr, jsonData.Length))
                    if (!doc.IsValid)
                        throw new InvalidOperationException("Json is invalid");
            }

            return SimdJson.MinifyJson(json);
        }

        [Benchmark]
        [ArgumentsSource(nameof(TestData))]
        public string JsonNet(byte[] jsonData, string fileName, string fileSize)
        {
            string json = Encoding.UTF8.GetString(jsonData);
            // let's benchmark string API.
            return JObject.Parse(json).ToString(Newtonsoft.Json.Formatting.None);
        }

        [Benchmark]
        [ArgumentsSource(nameof(TestData))]
        public string SpanJsonUtf16(byte[] jsonData, string fileName, string fileSize)
        {
            string json = Encoding.UTF8.GetString(jsonData);
            // let's benchmark string API.
            return SpanJson.JsonSerializer.Minifier.Minify(json);
        }


        [Benchmark]
        [ArgumentsSource(nameof(TestData))]
        public byte[] SpanJsonUtf8(byte[] jsonData, string fileName, string fileSize)
        {
            // let's benchmark string API.
            return SpanJson.JsonSerializer.Minifier.Minify(jsonData);
        }
    }
}
