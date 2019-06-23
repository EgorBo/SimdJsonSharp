using System.IO;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using Newtonsoft.Json;
using SimdJsonSharp;

namespace Benchmarks
{
    public class CountStrings : BenchmarksBase
    {
        [Benchmark(Baseline = true)]
        [ArgumentsSource(nameof(TestData))]
        public unsafe ulong SimdJsonUtf16(byte[] data, string fileName, string fileSize)
        {
            ulong wordsCount = 0;
            fixed (byte* dataPtr = data)
            {
                using (ParsedJson doc = SimdJson.ParseJson(dataPtr, data.Length))
                using (var iterator = new ParsedJsonIterator(doc))
                {
                    while (iterator.MoveForward())
                    {
                        if (iterator.IsString)
                        {
                            if (iterator.GetUtf16String().StartsWith('a'))
                                wordsCount++;
                        }
                    }
                }
            }

            return wordsCount;
        }

        [Benchmark]
        [ArgumentsSource(nameof(TestData))]
        public unsafe ulong SimdJsonNUtf16(byte[] data, string fileName, string fileSize)
        {
            ulong wordsCount = 0;
            fixed (byte* dataPtr = data)
            {
                using (ParsedJsonN doc = SimdJsonN.ParseJson(dataPtr, data.Length))
                using (var iterator = new ParsedJsonIteratorN(doc))
                {
                    while (iterator.MoveForward())
                    {
                        if (iterator.IsString())
                        {
                            if (iterator.GetUtf16String().StartsWith('a')) // UTF16 in SimdJsonN is expected to be slow for now (see https://github.com/lemire/simdjson/pull/101)
                                wordsCount++;
                        }
                    }
                }
            }

            return wordsCount;
        }
        [Benchmark]
        [ArgumentsSource(nameof(TestData))]
        public unsafe ulong SimdJsonUtf8(byte[] data, string fileName, string fileSize)
        {
            ulong wordsCount = 0;
            fixed (byte* dataPtr = data)
            {
                using (ParsedJson doc = SimdJson.ParseJson(dataPtr, data.Length))
                using (var iterator = new ParsedJsonIterator(doc))
                {
                    while (iterator.MoveForward())
                    {
                        if (iterator.IsString)
                        {
                            if (*iterator.GetUtf8String() == (byte)'a')
                                wordsCount++;
                        }
                    }
                }
            }

            return wordsCount;
        }

        //[Benchmark]
        [ArgumentsSource(nameof(TestData))]
        public unsafe ulong SimdJsonNUtf8(byte[] data, string fileName, string fileSize)
        {
            ulong wordsCount = 0;
            fixed (byte* dataPtr = data)
            {
                using (ParsedJsonN doc = SimdJsonN.ParseJson(dataPtr, data.Length))
                using (var iterator = new ParsedJsonIteratorN(doc))
                {
                    while (iterator.MoveForward())
                    {
                        if (iterator.IsString())
                        {
                            if (*iterator.GetUtf8String() == (byte)'a')
                                wordsCount++;
                        }
                    }
                }
            }

            return wordsCount;
        }

        [Benchmark]
        [ArgumentsSource(nameof(TestData))]
        public ulong _Utf8JsonReader(byte[] data, string fileName, string fileSize)
        {
            ulong wordsCount = 0;
            var reader = new Utf8JsonReader(data, true, default);
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.String)
                {
                    // count all strings starting with 'a' 
                    if (reader.GetString().StartsWith('a'))
                        wordsCount++;
                }
            }

            return wordsCount;
        }

        [Benchmark]
        [ArgumentsSource(nameof(TestData))]
        public ulong JsonNet(byte[] data, string fileName, string fileSize)
        {
            ulong wordsCount = 0;
            using (var streamReader = new StreamReader(new MemoryStream(data)))
            {
                JsonTextReader reader = new JsonTextReader(streamReader);
                while (reader.Read())
                {
                    if (reader.TokenType == JsonToken.String)
                    {
                        // count all strings starting with 'a' 
                        if (reader.ReadAsString().StartsWith('a'))
                            wordsCount++;
                    }
                }
            }

            return wordsCount;
        }

        [Benchmark]
        [ArgumentsSource(nameof(TestData))]
        public ulong SpanJsonUtf8(byte[] data, string fileName, string fileSize)
        {
            ulong wordsCount = 0;
            var reader = new SpanJson.JsonReader<byte>(data);
            SpanJson.JsonToken token;
            while ((token = reader.ReadUtf8NextToken()) != SpanJson.JsonToken.None)
            {
                if (token == SpanJson.JsonToken.String)
                {
                    // count all strings starting with 'a' 
                    if (reader.ReadString().StartsWith('a'))
                        wordsCount++;
                }
                reader.SkipNextUtf8Value(token);
            }

            return wordsCount;
        }
    }
}
