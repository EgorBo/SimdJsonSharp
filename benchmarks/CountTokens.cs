using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Newtonsoft.Json;
using SimdJsonSharp;

namespace Benchmarks
{
    [Config(typeof(ConfigWithCustomEnvVars))]
    public class CountTokens
    {
        private class ConfigWithCustomEnvVars : ManualConfig
        {
            private const string JitNoInline = "COMPlus_TieredCompilation";

            public ConfigWithCustomEnvVars()
            {
                Add(Job.Core
                    .With(new[] { new EnvironmentVariable(JitNoInline, "1") }));
            }
        }

        public IEnumerable<object[]> TestData()
        {
            string currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string testDataDir = Path.Combine(currentDir, @"../../../../external/simdjson/jsonexamples");

            testDataDir = @"C:\prj\simdjsonsharp\external\simdjson\jsonexamples"; // TODO: fix absolute path

            string[] files = Directory.GetFiles(testDataDir, "*.json", SearchOption.AllDirectories);
            files = new [] { @"C:\prj\simdjsonsharp\external\simdjson\jsonexamples\canada.json" };

            foreach (var file in files)
                yield return new object[] {File.ReadAllBytes(file), Path.GetFileName(file)};
        }

        [Benchmark(Baseline = true)]
        [ArgumentsSource(nameof(TestData))]
        public unsafe ulong SimdJsonSharpApi(byte[] data, string fileName)
        {
            ulong numbersCount = 0;
            using (ParsedJson doc = SimdJson.build_parsed_json(data))
            {
                using (var iterator = new iterator(&doc))
                {
                    while (iterator.move_forward())
                    {
                        if (iterator.GetTokenType() == JsonTokenType.Number)
                        {
                            numbersCount++;
                        }
                    }
                }
            }

            return numbersCount;
        }

        [Benchmark]
        [ArgumentsSource(nameof(TestData))]
        public unsafe ulong Utf8JsonReaderApi(byte[] data, string fileName)
        {
            ulong numbersCount = 0;
            var reader = new Utf8JsonReader(data, true, default);
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.Number)
                {
                    numbersCount++;
                }
            }

            return numbersCount;
        }

        [Benchmark]
        [ArgumentsSource(nameof(TestData))]
        public ulong JsonNet(byte[] data, string fileName)
        {
            ulong numbersCount = 0;
            using (var streamReader = new StreamReader(new MemoryStream(data)))
            {
                JsonTextReader reader = new JsonTextReader(streamReader);
                while (reader.Read())
                {
                    if (reader.TokenType == JsonToken.Float ||
                        reader.TokenType == JsonToken.Integer)
                    {
                        numbersCount++;
                    }
                }
            }

            return numbersCount;
        }

        [Benchmark]
        [ArgumentsSource(nameof(TestData))]
        public ulong SpanJsonUtf8(byte[] data, string fileName)
        {
            ulong numbersCount = 0;
            var reader = new SpanJson.JsonReader<byte>(data);
            SpanJson.JsonToken token;
            while ((token = reader.ReadUtf8NextToken()) != SpanJson.JsonToken.None)
            {
                if (token == SpanJson.JsonToken.Number)
                {
                    numbersCount++;
                }
                reader.SkipNextUtf8Value(token);
            }

            return numbersCount;
        }
    }
}
