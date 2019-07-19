using System;
using System.IO;
using System.Net.Http;
using CppAst;
using CppPinvokeGenerator;
using CppPinvokeGenerator.Templates;

namespace SimdJson
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // input (single header)
            string headerPath = Path.Combine(Environment.CurrentDirectory, "../../src/BindingsForNativeLib/SimdJsonNative/simdjson.h");

            // output
            string cgluePath = Path.Combine(Environment.CurrentDirectory, "../../src/BindingsForNativeLib/SimdJsonNative/bindings.cpp");
            string bindingsPath = Path.Combine(Environment.CurrentDirectory, "../../src/BindingsForNativeLib/SimdJsonSharp.Bindings/Bindings.Generated.cs");
            
            var options = new CppParserOptions();
            // TODO: test on macOS
            options.ConfigureForWindowsMsvc(CppTargetCpu.X86_64);
            options.AdditionalArguments.Add("-std=c++17");
            CppCompilation compilation = CppParser.ParseFile(headerPath, options);

            if (compilation.DumpErrorsIfAny())
            {
                Console.ReadKey();
                return;
            }

            var mapper = new TypeMapper(compilation);
            mapper.RenamingForApi += (nativeName, isMethod) =>
            {
                if (nativeName == "iterator")
                    return "ParsedJsonIteratorN";
                if (!isMethod)
                    return nativeName + "N"; // SimdJsonSharp has two C# APIs: 1) managed 2) bindings - postfixed with 'N'
                if (nativeName == "get_type")
                    return "GetTokenType";
                if (nativeName == "get_string")
                    return "GetUtf8String";
                return nativeName;
            };

            // init_state_machine requires external linkage (impl)
            mapper.RegisterUnsupportedMethod(null, "init_state_machine");

            // Register native types we don't want to bind (or any method with them in parameters)
            mapper.RegisterUnsupportedTypes(
                "simdjson", // it's empty - we don't need it
                "__m128i",
                "simd_input",         
                "utf8_checking_state",
                "basic_string",      // TODO:
                "basic_string_view", // TODO
                "basic_ostream");    // TODO:

            var templateManager = new TemplateManager();

            // Add additional stuff we want to see in the bindings.c
            templateManager
                .AddToCHeader(@"#include ""simdjson.h""")
                .AddToCHeader(@"using namespace simdjson;")
                .SetGlobalFunctionsClassName("SimdJsonN");

            PinvokeGenerator.Generate(mapper,
                templateManager,
                @namespace: "SimdJsonSharp",
                dllImportPath: @"SimdJsonN.NativeLib",
                outCFile: cgluePath,
                outCsFile: bindingsPath);

            Console.WriteLine("Done.");
        }
    }
}
