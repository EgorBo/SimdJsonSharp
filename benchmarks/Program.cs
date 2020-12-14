using System;
using System.IO;
using System.Reflection;
using BenchmarkDotNet.Running;

namespace Benchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            const string envVariable = "pathToJsonExamples";
            if (Environment.GetEnvironmentVariable(envVariable) == null)
            {
                string root = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string path = Path.Join(Directory.GetParent(root).Parent.Parent.Parent.FullName, "jsonexamples");
                Environment.SetEnvironmentVariable(envVariable, path);
            }
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }
}
