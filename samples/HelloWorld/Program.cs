using System;
using System.Text;
using System.Text.Json;
using SimdJsonSharp;

namespace ConsoleApp124
{
    class Program
    {
        static unsafe void Main(string[] args)
        {
            string helloWorldJson = @"{ ""answer"": 42, ""name"": ""Egor"" }";
            ReadOnlySpan<byte> bytes = Encoding.UTF8.GetBytes(helloWorldJson);
            // SimdJson is UTF8 only

            fixed (byte* ptr = bytes)
            {
                // SimdJsonN -- N stands for Native, it means we are using Bindings for simdjson native lib
                // SimdJson -- fully managed .NET Core 3.0 port
                using (ParsedJsonN doc = SimdJsonN.ParseJson(ptr, bytes.Length))
                {
                    Console.WriteLine($"Is json valid:{doc.IsValid}\n");

                    // open iterator:
                    using (var iterator = new ParsedJsonIteratorN(doc))
                    {
                        while (iterator.MoveForward())
                        {
                            if (iterator.IsInteger)
                                Console.WriteLine("integer: " + iterator.GetInteger());
                            if (iterator.IsString)
                                Console.WriteLine("string: " + iterator.GetUtf16String());
                        }
                    }
                }
            }
        }
    }
}
