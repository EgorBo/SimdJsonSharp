using System;
using System.Runtime.InteropServices;
using System.Text;

namespace SimdJsonSharp
{
    public static unsafe class SimdJsonN // 'N' stands for Native
    {
        public const string NativeLib = @"SimdJsonNative";

        public static uint MinifyJson(byte* jsonDataPtr, int jsonDataLength, byte* output) => 
            (uint)Global_jsonminify(jsonDataPtr, (IntPtr)jsonDataLength, output);

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

        public static ParsedJsonN ParseJson(byte* jsonDataPtr, int jsonDataLength, bool reallocifneeded = true)
        {
            ParsedJsonN pj = new ParsedJsonN();
            bool ok = pj.AllocateCapacity((uint)jsonDataLength, 1024);
            if (ok)
            {
                Global_json_parse(jsonDataPtr, (IntPtr)jsonDataLength, pj.Handle, reallocifneeded);
            }
            else
            {
                throw new InvalidOperationException("failure during memory allocation");
            }
            return pj;
        }


        #region pinvokes
        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)] private static extern sbyte* Global_allocate_padded_buffer(IntPtr length);
        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)] private static extern IntPtr Global_jsonminify(byte* buf, IntPtr len, byte* output);
        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)] private static extern int Global_json_parse(byte* buf, IntPtr len, void* pj, bool reallocifneeded = true);
        #endregion
    }

    // Extend auto-generated stuff here

    public unsafe partial class ParsedJsonN // 'N' stands for Native
    {
    }

    public unsafe partial class ParsedJsonIteratorN // 'N' stands for Native
    {
        internal static readonly UTF8Encoding _utf8Encoding = new UTF8Encoding(false, true);

        public string GetUtf16String() => _utf8Encoding.GetString((byte*)iterator_get_string(Handle), (int)iterator_get_string_length(Handle));
    }
}
