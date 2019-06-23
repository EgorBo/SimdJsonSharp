﻿using System;
using System.IO;
using System.Reflection;
using System.Text;
using Xunit;

namespace SimdJsonSharp.Tests
{
    public class MinifierTests
    {
        [Fact]
        public void ValidateMinifier()
        {
            string json = @"{
                              ""Egor"":  ""Bogatov"" 
                            }
                            ";

            string minifiedJson = SimdJson.MinifyJson(json);
            Assert.Equal(@"{""Egor"":""Bogatov""}", minifiedJson);
            // TODO: more tests
        }

        [Fact]
        public unsafe void ValidateMinimizedJson()
        {
            string currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string testDataDir = Path.Combine(currentDir, @"../../../../jsonexamples");

            string[] files = Directory.GetFiles(testDataDir, "*.json", SearchOption.AllDirectories);
            // 20 files, ~15Mb of JSON
            Assert.NotEmpty(files);
            foreach (string file in files)
            {
                ReadOnlySpan<byte> fileData = File.ReadAllBytes(file);
                Span<byte> output = new byte[fileData.Length];
                SimdJson.MinifyJson(fileData, output, out int bytesWritten);
                output = output.Slice(0, bytesWritten);

                fixed (byte* outPtr = output)
                    using (ParsedJson doc = SimdJson.ParseJson(outPtr, output.Length))
                        Assert.True(doc.IsValid);
            }
        }

        [Fact]
        public unsafe void ValidateMinimizedJsonN()
        {
            string currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string testDataDir = Path.Combine(currentDir, @"../../../../jsonexamples");

            string[] files = Directory.GetFiles(testDataDir, "*.json", SearchOption.AllDirectories);
            // 20 files, ~15Mb of JSON
            Assert.NotEmpty(files);
            foreach (string file in files)
            {
                string minifiedJson = SimdJsonN.MinifyJson(File.ReadAllBytes(file));
                var output = Encoding.UTF8.GetBytes(minifiedJson);

                fixed (byte* outPtr = output)
                    using (ParsedJsonN doc = SimdJsonN.ParseJson(outPtr, output.Length))
                        Assert.True(doc.IsValid());
            }
        }
    }
}
