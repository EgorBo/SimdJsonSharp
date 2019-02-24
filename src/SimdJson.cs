// This file is a manual port of C code https://github.com/lemire/simdjson to C#
// (c) Daniel Lemire

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

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
        private static readonly long pagesize = System.Environment.SystemPageSize;

        public static ParsedJson BuildParsedJson(ReadOnlySpan<byte> jsonData)
        {
            fixed (byte* dataPtr = jsonData)
                return BuildParsedJson(dataPtr, (ulong)jsonData.Length);
        }

        public static ParsedJson BuildParsedJson(uint8_t* jsonData, size_t length, bool reallocIfNeeded = true)
        {
            ParsedJson pj = new ParsedJson();
            bool ok = pj.AllocateCapacity(length);
            if (ok)
            {
                ok = JsonParse(jsonData, length, &pj, reallocIfNeeded);
            }
            else
            {
                throw new InvalidOperationException("failure during memory allocation");
            }
            return pj;
        }

        public static bool JsonParse(uint8_t* jsonData, size_t length, ParsedJson* pj, bool reallocIfNeeded = true)
        {
            if (pj->bytecapacity < length)
            {
                Debug.WriteLine("Your ParsedJson cannot support documents that big: " + length);
                return false;
            }
            bool reallocated = false;
            if (reallocIfNeeded)
            {
                // realloc is needed if the end of the memory crosses a page

                if (((size_t)(jsonData + length - 1) % (size_t)pagesize) < SIMDJSON_PADDING)
                {
                    uint8_t* tmpbuf = jsonData;
                    jsonData = (uint8_t*)Utils.allocate_padded_buffer(length);
                    if (jsonData == null) return false;
                    memcpy((void*)jsonData, tmpbuf, length);
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
                if (reallocated) Utils.free((void*)jsonData);
                return false;
            }
            if (reallocated) Utils.free((void*)jsonData);
            return isok;
        }
    }
}
