using System;
using System.IO;
using System.Reflection;
using Xunit;

namespace SimdJsonSharp.Tests
{
    public class ValidateTestFilesTests
    {
        [Fact]
        public void ValidateAllFiles()
        {
            string currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string testDataDir = Path.Combine(currentDir, @"../../../../external/simdjson/jsonexamples");

            string[] files = Directory.GetFiles(testDataDir, "*.json", SearchOption.AllDirectories);
            // 20 files, ~15Mb of JSON
            Assert.NotEmpty(files);
            foreach (string file in files)
            {
                ReadOnlySpan<byte> fileData = File.ReadAllBytes(file);
                using (ParsedJson doc = SimdJson.ParseJson(fileData))
                {
                    Assert.True(doc.IsValid);
                }
            }
        }
    }
}
