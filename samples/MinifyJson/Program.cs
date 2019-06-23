using System;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.Win32.SafeHandles;
using SimdJsonSharp;

namespace MinifyJson
{
    class Program
    {
        static void Main(string[] args)
        {
            ReadOnlySpan<byte> beforeData = LoadEmbeddedFile("MinifyJson.sample.json");

            string beforeString = Encoding.UTF8.GetString(beforeData);
            Console.WriteLine($"Before:\n{beforeString}\nLength={beforeString.Length}");

            string afterString = SimdJson.MinifyJson(beforeString); // there is also Span API
            Console.WriteLine($"\n\nAfter:\n{afterString}.\nLength={afterString.Length}");
        }

        private static byte[] LoadEmbeddedFile(string file)
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(file))
            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                return ms.ToArray();
            }
        }
    }
}
