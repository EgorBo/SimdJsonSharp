using System;
using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Running;

namespace Benchmarks
{
    public class Program
    {
        public static void Main()
        {
            BenchmarkRunner.Run<CountTokens>();
        }
    }
}
