using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            private const string JitTieredCompilation = "COMPlus_TieredCompilation";

            public ConfigWithCustomEnvVars()
            {
                Add(Job.ShortRun.With(new[] { new EnvironmentVariable(JitTieredCompilation, "1") }));
                Add(Job.ShortRun.With(new[] { new EnvironmentVariable(JitTieredCompilation, "0") }));
            }
        }

        public IEnumerable<object[]> TestData()
        {
            string jsonExamples = Environment.GetEnvironmentVariable("pathToJsonExamples");
            string[] files = Directory.GetFiles(jsonExamples, "*.json", SearchOption.AllDirectories).ToArray();

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
