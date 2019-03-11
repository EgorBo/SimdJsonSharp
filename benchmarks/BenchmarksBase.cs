using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace Benchmarks
{
    [Config(typeof(ConfigWithCustomEnvVars))]
    public class BenchmarksBase
    {
        private class ConfigWithCustomEnvVars : ManualConfig
        {
            private const string JitNoInline = "COMPlus_TieredCompilation";

            public ConfigWithCustomEnvVars()
            {
                Add(Job.Core.With(new[] { new EnvironmentVariable(JitNoInline, "1") }));
            }
        }

        public IEnumerable<object[]> TestData()
        {
            var testDataDir = @"C:\prj\simdjsonsharp\jsonexamples"; // TODO: fix absolute path
            string[] files = Directory.GetFiles(testDataDir, "*.json", SearchOption.AllDirectories).Take(5).ToArray();

            foreach (var file in files)
            {
                byte[] bytes = File.ReadAllBytes(file);
                yield return new object[]
                {
                    bytes,
                    Path.GetFileName(file),
                    Math.Round(bytes.Length / 1000.0, 2).ToString("N") + " Kb"
                };
            }
        }
    }
}
