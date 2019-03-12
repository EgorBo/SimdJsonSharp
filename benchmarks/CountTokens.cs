using System.IO;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using Newtonsoft.Json;
using SimdJsonSharp;

namespace Benchmarks
{
    public class CountTokens : BenchmarksBase
    {
        [Benchmark(Baseline = true)]
        [ArgumentsSource(nameof(TestData))]
        public unsafe ulong _SimdJson(byte[] data, string fileName, string fileSize)
        {
            ulong numbersCount = 0;
            fixed (byte* dataPtr = data)
            {
                using (ParsedJson doc = SimdJson.ParseJson(dataPtr, data.Length))
                using (var iterator = new ParsedJsonIterator(doc))
                {
                    while (iterator.MoveForward())
                        if (iterator.IsDouble || iterator.IsInteger)
                            numbersCount++;
                }
            }

            return numbersCount;
        }

        [Benchmark]
        [ArgumentsSource(nameof(TestData))]
        public unsafe ulong _SimdJsonN(byte[] data, string fileName, string fileSize)
        {
            ulong numbersCount = 0;
            fixed (byte* dataPtr = data)
            {
                using (ParsedJsonN doc = SimdJsonN.ParseJson(dataPtr, data.Length))
                using (var iterator = new ParsedJsonIteratorN(doc))
                {
                    while (iterator.MoveForward())
                        //if (iterator.IsDouble || iterator.IsInteger)
                            numbersCount++;
                }
            }

            return numbersCount;
        }

        [Benchmark]
        [ArgumentsSource(nameof(TestData))]
        public ulong _Utf8JsonReader(byte[] data, string fileName, string fileSize)
        {
            ulong numbersCount = 0;
            var reader = new Utf8JsonReader(data, true, default);
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.Number)
                    numbersCount++;
            }

            return numbersCount;
        }

        [Benchmark]
        [ArgumentsSource(nameof(TestData))]
        public ulong JsonNet(byte[] data, string fileName, string fileSize)
        {
            ulong numbersCount = 0;
            using (var streamReader = new StreamReader(new MemoryStream(data)))
            {
                JsonTextReader reader = new JsonTextReader(streamReader);
                while (reader.Read())
                {
                    if (reader.TokenType == JsonToken.Float || 
                        reader.TokenType == JsonToken.Integer)
                        numbersCount++;
                }
            }

            return numbersCount;
        }

        [Benchmark]
        [ArgumentsSource(nameof(TestData))]
        public ulong SpanJsonUtf8(byte[] data, string fileName, string fileSize)
        {
            ulong numbersCount = 0;
            var reader = new SpanJson.JsonReader<byte>(data);
            SpanJson.JsonToken token;
            while ((token = reader.ReadUtf8NextToken()) != SpanJson.JsonToken.None)
            {
                if (token == SpanJson.JsonToken.Number)
                    numbersCount++;
                reader.SkipNextUtf8Value(token);
            }

            return numbersCount;
        }
    }
}
