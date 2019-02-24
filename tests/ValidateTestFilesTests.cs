using System;
using System.IO;
using System.Reflection;
using Xunit;

namespace SimdJsonSharp.Tests
{
    public class ValidateTestFilesTests
    {
        [Fact]
        public unsafe void ValidateAllFiles()
        {
            string currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string testDataDir = Path.Combine(currentDir, @"../../../../external/simdjson/jsonexamples");

            string[] files = Directory.GetFiles(testDataDir, "*.json", SearchOption.AllDirectories);
            // 20 files, ~15Mb of JSON
            Assert.NotEmpty(files);
            foreach (string file in files)
            {
                byte[] fileData = File.ReadAllBytes(file);
                fixed (byte* ptr = fileData)
                    using (ParsedJson doc = SimdJson.ParseJson(ptr, fileData.Length))
                        Assert.True(doc.IsValid);
            }
        }
    }
}
