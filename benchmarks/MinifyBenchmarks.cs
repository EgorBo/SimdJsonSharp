using System;
using System.Text;
using BenchmarkDotNet.Attributes;
using Newtonsoft.Json.Linq;
using SimdJsonSharp;

namespace Benchmarks
{
    public class MinifyBenchmarks : BenchmarksBase
    {
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
    }
}
