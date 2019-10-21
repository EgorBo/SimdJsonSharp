using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using Xunit;
using System.Text.Json;
using System.Text;

namespace SimdJsonSharp.Tests
{
    public class ValidateTestFilesTests
    {
        private string testDataDir;

        public ValidateTestFilesTests()
        {
            string currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            testDataDir = Path.Join(Directory.GetParent(currentDir).Parent.Parent.Parent.FullName, "jsonexamples");
        }

        [Fact]
        public unsafe void ValidateAllFiles()
        {
            string[] files = Directory.GetFiles(testDataDir, "*.json", SearchOption.AllDirectories);
            // 20 files, ~15Mb of JSON
            Assert.NotEmpty(files);
            foreach (string file in files)
            {
                byte[] fileData = File.ReadAllBytes(file);
                fixed (byte* ptr = fileData)
                    using (ParsedJson doc = SimdJson.ParseJson(ptr, (ulong)fileData.Length))
                        Assert.True(doc.IsValid);
            }
        }

        [Fact]
        public unsafe void ValidateAllFilesN()
        {
            string[] files = Directory.GetFiles(testDataDir, "*.json", SearchOption.AllDirectories);
            // 20 files, ~15Mb of JSON
            Assert.NotEmpty(files);
            foreach (string file in files)
            {
                byte[] fileData = File.ReadAllBytes(file);
                fixed (byte* ptr = fileData)
                    using (ParsedJsonN doc = SimdJsonN.ParseJson(ptr, fileData.Length))
                        Assert.True(doc.IsValid());
            }
        }

        [Fact]
        public unsafe void ValidateStrings()
        {
            string invalidJson = @"{ ""name"": ""\udc00\ud800\uggggxy"" }";
            var bytes = Encoding.UTF8.GetBytes(invalidJson);

            fixed (byte* ptr = bytes)
            {
                using (ParsedJson doc = SimdJson.ParseJson(ptr, (ulong)bytes.Length))
                {
                    Assert.False(doc.IsValid);
                    Assert.Throws<InvalidOperationException>(() => doc.CreateIterator());
                }
            }
        }

        [Fact]
        public unsafe void ParseDoubles()
        {
            byte[] fileData = File.ReadAllBytes(Path.Combine(testDataDir, "canada.json"));
            var simdDoubles = new List<double>();
            var referenceDoubles = new List<double>();

            fixed (byte* ptr = fileData)
            {
                using (ParsedJson doc = SimdJson.ParseJson(ptr, (ulong)fileData.Length))
                {
                    using (var iterator = doc.CreateIterator())
                    {
                        while (iterator.MoveForward())
                        {
                            if (iterator.IsDouble || iterator.IsInteger)
                            {
                                simdDoubles.Add(iterator.GetDouble());
                            }
                        }
                    }
                }
            }

            // compare with doubles from Utf8JsonReader
            Utf8JsonReader reader = new Utf8JsonReader(fileData, true, default);
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.Number) //Utf8JsonReader doesn't have a token type for Double/Float
                {
                    referenceDoubles.Add(reader.GetDouble());
                }
            }

            for (int i = 0; i < simdDoubles.Count; i++)
            {
                var doubleSimd = simdDoubles[i];
                var doubleRef = referenceDoubles[i];
                // TODO: compare
            }
        }
    }
}
