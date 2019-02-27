// This file is a manual port of C code https://github.com/lemire/simdjson to C#
// (c) Daniel Lemire

using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;

#region stdint types and friends
// if you change something here please change it in other files too
using size_t = System.UInt64;
using uint8_t = System.Byte;
using uint64_t = System.UInt64;
using uint32_t = System.UInt32;
using int64_t = System.Int64;
using bytechar = System.SByte;
using unsigned_bytechar = System.Byte;
using uintptr_t = System.UIntPtr;
using static SimdJsonSharp.Utils;
#endregion

namespace SimdJsonSharp
{
    public static unsafe class SimdJson
    {
        public static ParsedJson ParseJson(byte* jsonData, int length, bool reallocIfNeeded = true)
        {
            var pj = new ParsedJson();
            bool ok = pj.AllocateCapacity((ulong)length);
            if (ok)
                JsonParse(jsonData, (ulong)length, pj, reallocIfNeeded);
            else
                throw new InvalidOperationException("failure during memory allocation");
            return pj;
        }

        public static ParsedJsonIterator ParseJsonAndOpenIterator(byte* jsonData, int length)
        {
            var parsedJson = ParseJson(jsonData, length);
            return new ParsedJsonIterator(parsedJson);
        }

        public static string MinifyJson(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            ReadOnlySpan<byte> inputBytes = Encoding.UTF8.GetBytes(input);
            var length = inputBytes.Length;
            byte[] pool = null;

            try
            {
                Span<byte> span = length <= 2048 ?
                    stackalloc byte[2048] :
                    (pool = ArrayPool<byte>.Shared.Rent(length));

                MinifyJson(inputBytes, span, out int bytesWritten);
                return Encoding.UTF8.GetString(span.Slice(0, bytesWritten));
            }
            finally
            {
                if (pool != null)
                    ArrayPool<byte>.Shared.Return(pool);
            }
        }

        public static void MinifyJson(ReadOnlySpan<byte> input, Span<byte> output, out int bytesWritten)
        {
#if JSON_MINIFY
            if ((uint)input.Length < 1)
            {
                bytesWritten = 0;
                return;
            }

            if ((uint)output.Length < 1)
                throw new ArgumentException("Output is empty");

            //TODO: how to validate output length?

            fixed (byte* inputPtr = input)
            fixed (byte* outputPtr = output)
            {
                bytesWritten = (int)JsonMinifier.Minify(inputPtr, (ulong)input.Length, outputPtr);
            }
#else
            throw new NotSupportedException("SimdJsonSharp was compiled without `JSON_MINIFY`.");
#endif
        }

        private static readonly long pagesize = Environment.SystemPageSize;

        internal static bool JsonParse(uint8_t* jsonData, size_t length, ParsedJson pj, bool reallocIfNeeded = true)
        {
            if (pj.bytecapacity < length)
                throw new InvalidOperationException("Your ParsedJson cannot support documents that big: " + length);

            bool reallocated = false;
            if (reallocIfNeeded)
            {
                // realloc is needed if the end of the memory crosses a page
                if ((size_t)(jsonData + length - 1) % (size_t)pagesize < SIMDJSON_PADDING)
                {
                    uint8_t* tmpbuf = jsonData;
                    jsonData = (uint8_t*)allocate_padded_buffer(length);
                    if (jsonData == null) return false;
                    memcpy(jsonData, tmpbuf, length);
                    reallocated = true;
                }
            }
            bool isok = stage1_find_marks.find_structural_bits(jsonData, length, pj);
            if (isok)
            {
                isok = stage2_build_tape.unified_machine(jsonData, length, pj);
            }
            else
            {
                if (reallocated) free(jsonData);
                return false;
            }
            if (reallocated) free(jsonData);
            return isok;
        }
    }
}
