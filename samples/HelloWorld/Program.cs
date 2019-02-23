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
            string helloWorldJson = "{ \"answer\": 42, \"name\": \"Egor\" }";
            ReadOnlySpan<byte> bytes = Encoding.UTF8.GetBytes(helloWorldJson);
            // SimdJson is UTF8 only

            using (ParsedJson doc = SimdJson.build_parsed_json(bytes))
            {
                Console.WriteLine("Is json valid:" + doc.isValid());
                
                // open iterator:
                using (iterator iterator = new iterator(&doc))
                {
                    while (iterator.move_forward())
                    {
                        switch (iterator.GetTokenType())
                        {
                            case JsonTokenType.Number:
                                Console.WriteLine("integer: " + iterator.get_integer());
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
