using BenchmarkDotNet.Running;

namespace Benchmarks
{
    public class Program
    {
        public static void Main()
        {
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run();
        }
    }
}
