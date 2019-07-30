// This file is a manual port of C code https://github.com/lemire/simdjson to C#
// (c) Daniel Lemire and Geoff Langdale

using System;
using System.Buffers;
using System.Text;

using static SimdJsonSharp.Utils;

namespace SimdJsonSharp
{
    public static unsafe class SimdJson
    {
        public static ParsedJson ParseJson(byte* jsonData, ulong length, bool reallocIfNeeded = true)
        {
            var pj = new ParsedJson();
            bool ok = pj.AllocateCapacity(length);
            if (ok)
                JsonParse(jsonData, length, pj, reallocIfNeeded);
            else
            {
                pj.isvalid = false;
                pj.ErrorCode = JsonParseError.CAPACITY;
            }
            return pj;
        }

        public static ParsedJson ParseJson(byte* jsonData, int length) => ParseJson(jsonData, (ulong) length, true);

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

        internal static JsonParseError JsonParse(byte* jsonData, UInt64 length, ParsedJson pj, bool reallocIfNeeded = true)
        {
            if (pj.bytecapacity < length)
                return JsonParseError.CAPACITY;

            bool reallocated = false;
            if (reallocIfNeeded)
            {
                byte* tmpbuf = jsonData;
                jsonData = (byte*)allocate_padded_buffer(length);
                if (jsonData == null) return JsonParseError.MEMALLOC;
                memcpy((void*)jsonData, tmpbuf, length);
                reallocated = true;
            }

            JsonParseError stage1_is_ok = stage1_find_marks.find_structural_bits(jsonData, length, pj);
            if (stage1_is_ok != JsonParseError.SUCCESS)
            {
                pj.ErrorCode = stage1_is_ok;
                return pj.ErrorCode;
            }
            JsonParseError res = stage2_build_tape.unified_machine(jsonData, length, pj);
            if (reallocated) { aligned_free((void*)jsonData); }
            return res;

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
