// This file is a manual port of C code https://github.com/lemire/simdjson to C#
// (c) Daniel Lemire

using System;
using System.Diagnostics;

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
        public static ParsedJson build_parsed_json(ReadOnlySpan<byte> data)
        {
            fixed (byte* dataPtr = data)
                return build_parsed_json(dataPtr, (ulong)data.Length);
        }

        public static ParsedJson build_parsed_json(uint8_t* buf, size_t len, bool reallocifneeded = true)
        {
            ParsedJson pj = new ParsedJson();
            bool ok = pj.allocateCapacity(len);
            if (ok)
            {
                ok = json_parse(buf, len, &pj, reallocifneeded);
            }
            else
            {
                throw new InvalidOperationException("failure during memory allocation");
            }
            return pj;
        }

        public static bool json_parse(uint8_t* buf, size_t len, ParsedJson* pj, bool reallocifneeded = true)
        {
            if (pj->bytecapacity < len)
            {
                Debug.WriteLine("Your ParsedJson cannot support documents that big: " + len);
                return false;
            }
            bool reallocated = false;
            if (reallocifneeded)
            {
                // realloc is needed if the end of the memory crosses a page
                long pagesize = System.Environment.SystemPageSize;

                if (((size_t)(buf + len - 1) % (size_t)pagesize) < SIMDJSON_PADDING)
                {
                    uint8_t* tmpbuf = buf;
                    buf = (uint8_t*)Utils.allocate_padded_buffer(len);
                    if (buf == null) return false;
                    memcpy((void*)buf, tmpbuf, len);
                    reallocated = true;
                }
            }
            bool isok = stage1_find_marks.find_structural_bits(buf, len, pj);
            if (isok)
            {
                isok = stage2_build_tape.unified_machine(buf, len, pj);
            }
            else
            {
                if (reallocated) Utils.free((void*)buf);
                return false;
            }
            if (reallocated) Utils.free((void*)buf);
            return isok;
        }
    }
}
