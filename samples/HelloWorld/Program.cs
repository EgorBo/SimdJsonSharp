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
                using (ParsedJson doc = SimdJson.ParseJson(ptr, bytes.Length))
                {
                    Console.WriteLine($"Is json valid:{doc.IsValid}\n");

                    // open iterator:
                    using (var iterator = new ParsedJsonIterator(doc))
                    {
                        while (iterator.MoveForward())
                        {
                            switch (iterator.GetTokenType())
                            {
                                case JsonTokenType.Number:
                                    Console.WriteLine("integer: " + iterator.GetInteger());
                                    break;

                                case JsonTokenType.String:
                                    Console.WriteLine("string: " + iterator.GetUtf16String());
                                    break;
                            }
                        }
                    }
                }
            }
        }
    }
}
