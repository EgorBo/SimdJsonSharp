using System;
using System.Text;

namespace SimdJsonSharp
{
    public static unsafe partial class SimdJsonN // 'N' stands for Native
    {
        public const string NativeLib = @"SimdJsonNative";

        public static uint MinifyJson(byte* jsonDataPtr, long jsonDataLength, byte* output) => 
            (uint)JsonMinify(jsonDataPtr, jsonDataLength, output);

        public static string MinifyJson(byte[] inputBytes)
        {
            byte[] outputBytes = new byte[inputBytes.Length]; // no Span<T> and ArrayPool in ns2.0

            fixed (byte* inputBytesPtr = inputBytes)
            fixed (byte* outputBytesPtr = outputBytes)
            {
                uint bytesWritten = MinifyJson(inputBytesPtr, inputBytes.Length, outputBytesPtr);
                return Encoding.UTF8.GetString(outputBytes, 0, (int)bytesWritten);
            }
        }

        public static string MinifyJson(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            return MinifyJson(inputBytes);
        }

        public static ParsedJsonN ParseJson(byte[] jsonData)
        {
            fixed (byte* jsonDataPtr = jsonData)
                return ParseJson(jsonDataPtr, jsonData.Length);
        }

        public static ParsedJsonN ParseJson(byte* jsonDataPtr, long jsonDataLength, bool reallocifneeded = true)
        {
            ParsedJsonN pj = new ParsedJsonN();
            bool ok = pj.AllocateCapacity((uint)jsonDataLength, 1024);
            if (ok)
            {
                JsonParse(jsonDataPtr, jsonDataLength, pj, reallocifneeded);
            }
            else
            {
                throw new InvalidOperationException("failure during memory allocation");
            }
            return pj;
        }
    }

    // Extend auto-generated stuff here

    public unsafe partial class ParsedJsonIteratorN // 'N' stands for Native
    {
        internal static readonly UTF8Encoding _utf8Encoding = new UTF8Encoding(false, true);

        public string GetUtf16String() => _utf8Encoding.GetString((byte*)GetUtf8String(), (int)GetStringLength());
    }
}
