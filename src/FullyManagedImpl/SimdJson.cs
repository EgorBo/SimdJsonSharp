// This file is a manual port of C code https://github.com/lemire/simdjson to C#
// (c) Daniel Lemire and Geoff Langdale

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

        internal static JsonParseError JsonParse(uint8_t* jsonData, size_t length, ParsedJson pj, bool reallocIfNeeded = true)
        {
            if (pj.bytecapacity < length)
                return JsonParseError.CAPACITY;

            bool reallocated = false;
            if (reallocIfNeeded)
            {
                // realloc is needed if the end of the memory crosses a page
                if ((size_t)(jsonData + length - 1) % (size_t)pagesize < SIMDJSON_PADDING)
                {
                    uint8_t* tmpbuf = jsonData;
                    jsonData = (uint8_t*)allocate_padded_buffer(length);
                    if (jsonData == null) return JsonParseError.MEMALLOC;
                    memcpy(jsonData, tmpbuf, length);
                    reallocated = true;
                }
            }

            var result = JsonParseError.SUCCESS;
            if (stage1_find_marks.find_structural_bits(jsonData, length, pj))
                result = stage2_build_tape.unified_machine(jsonData, length, pj);
            if (reallocated)
                aligned_free(jsonData);
            return result;
        }
    }

    public enum JsonParseError
    {
        SUCCESS = 0,
        CAPACITY, // This ParsedJson can't support a document that big
        MEMALLOC, // Error allocating memory, most likely out of memory
        TAPE_ERROR, // Something went wrong while writing to the tape (stage 2), this is a generic error
        DEPTH_ERROR, // Your document exceeds the user-specified depth limitation
        STRING_ERROR, // Problem while parsing a string
        T_ATOM_ERROR, // Problem while parsing an atom starting with the letter 't'
        F_ATOM_ERROR, // Problem while parsing an atom starting with the letter 'f'
        N_ATOM_ERROR, // Problem while parsing an atom starting with the letter 'n'
        NUMBER_ERROR, // Problem while parsing a number
        UTF8_ERROR, // the input is not valid UTF-8
        UNITIALIZED, // unknown error, or uninitialized document
        EMPTY, // no structural document found
        UNESCAPED_CHARS, // found unescaped characters in a string.
        UNCLOSED_STRING, // missing quote at the end
        UNEXPECTED_ERROR // indicative of a bug in simdjson
    }
}
