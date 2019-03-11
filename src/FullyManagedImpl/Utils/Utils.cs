// This file is a manual port of C code https://github.com/lemire/simdjson to C#
// (c) Daniel Lemire

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;

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
    public static unsafe class Utils
    {
        public const int SIMDJSON_PADDING = 32; //sizeof(__m256i)
        public const ulong JSONVALUEMASK = 0xFFFFFFFFFFFFFF;
        public const ulong DEFAULTMAXDEPTH = 1024;  // a JSON document with a depth exceeding 1024 is probably de facto invalid

        //C#: ReadOnlySpan<byte> trick I learned here https://github.com/dotnet/coreclr/pull/22100#discussion_r249261548
        private static ReadOnlySpan<byte> structural_or_whitespace_negated => new byte[256] {
            0, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 1, 1, 0, 1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 1, 1, 1, 1,

            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 0, 1, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 0, 1, 1,

            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,

            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1};

        // return non-zero if not a structural or whitespace char
        // zero otherwise
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint32_t is_not_structural_or_whitespace(uint8_t c)
        {
            // Bypass bounds check as c can never 
            // exceed the bounds of structural_or_whitespace_negated
            return Unsafe.AddByteOffset(
                ref MemoryMarshal.GetReference(structural_or_whitespace_negated), 
                (IntPtr)c);
        }

        private static ReadOnlySpan<byte> structural_or_whitespace => new byte[256] {
            1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 1, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint32_t is_structural_or_whitespace(uint8_t c)
        {
            // Bypass bounds check as c can never 
            // exceed the bounds of structural_or_whitespace
            return Unsafe.AddByteOffset(
                ref MemoryMarshal.GetReference(structural_or_whitespace), 
                (IntPtr)c);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint64_t trailingzeroes(UInt64 input_num)
        {
            return Bmi1.X64.TrailingZeroCount(input_num);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint64_t leadingzeroes(UInt64 input_num)
        {
            return Lzcnt.X64.LeadingZeroCount(input_num);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static size_t hamming(UInt64 input_num)
        {
            if (IntPtr.Size == 8)
            {
                return Popcnt.X64.PopCount(input_num);
            }
            else
            {
                return (size_t)(Popcnt.PopCount((UInt32)input_num) +
                             Popcnt.PopCount((UInt32)(input_num >> 32)));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt64 ROUNDUP_N(UInt64 a, UInt64 n) => (((a) + ((n) - 1)) & ~((n) - 1));

        public static unsafe int strcmp(bytechar* s1, bytechar* s2)
        {
            //C#: TODO: optimize/vectorize!!!
            while (*s1 != 0 && (*s1 == *s2))
            {
                s1++;
                s2++;
            }
            return *(byte*)s1 - *(byte*)s2;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void memcpy(void* destination, void* source, UInt64 length)
        {
            Unsafe.CopyBlockUnaligned(destination, source, (uint)length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void memset(void* dst, byte value, uint length)
        {
            Unsafe.InitBlockUnaligned(dst, value, length);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool add_overflow(ulong value1, ulong value2, ulong* result)
        {
            //?? there is no C# version of _addcarry_u64
            // I have no idea what I am doing but it works...
            *result = (value1 + value2);
            if (*result < value1)
            {
                return true;
            }
            return false; 
        }

        public static bool mul_overflow(ulong value1, ulong value2, ulong* result)
        {
            throw new NotImplementedException(":("); // same here - _umul128
        }

        private static ReadOnlySpan<bytechar> digittoval => new bytechar[256] {
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 0,  1,  2,  3,  4,  5,  6,  7,  8,
            9,  -1, -1, -1, -1, -1, -1, -1, 10, 11, 12, 13, 14, 15, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, 10, 11, 12, 13, 14, 15, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1};

        // returns a value with the high 16 bits set if not valid
        // otherwise returns the conversion of the 4 hex digits at src into the bottom 16 bits of the 32-bit
        // return register

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint32_t hex_to_u32_nocheck(uint8_t* src) {
            // all these will sign-extend the chars looked up, placing 1-bits into the high 28 bits of every
            // invalid value. After the shifts, this will *still* result in the outcome that the high 16 bits of any
            // value with any invalid char will be all 1's. We check for this in the caller.
            uint8_t v1 = (uint8_t)digittoval[src[0]];
            uint8_t v2 = (uint8_t)digittoval[src[1]];
            uint8_t v3 = (uint8_t)digittoval[src[2]];
            uint8_t v4 = (uint8_t)digittoval[src[3]];
            return (uint32_t)(v1 << 12 | v2 << 8 | v3 << 4 | v4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static size_t codepoint_to_utf8(uint32_t cp, uint8_t* c)
        {
            if (cp <= 0x7F)
            {
                c[0] = (uint8_t)cp;
                return 1; // ascii
            }
            else if (cp <= 0x7FF)
            {
                c[0] = (uint8_t)((cp >> 6) + 192);
                c[1] = (uint8_t)((cp & 63) + 128);
                return 2; // universal plane
                //  Surrogates are treated elsewhere...
                //} //else if (0xd800 <= cp && cp <= 0xdfff) {
                //  return 0; // surrogates // could put assert here
            }
            else if (cp <= 0xFFFF)
            {
                c[0] = (uint8_t)((cp >> 12) + 224);
                c[1] = (uint8_t)(((cp >> 6) & 63) + 128);
                c[2] = (uint8_t)((cp & 63) + 128);
                return 3;
            }
            else if (cp <= 0x10FFFF)
            { // if you know you have a valid code point, this is not needed
                c[0] = (uint8_t)((cp >> 18) + 240);
                c[1] = (uint8_t)(((cp >> 12) & 63) + 128);
                c[2] = (uint8_t)(((cp >> 6) & 63) + 128);
                c[3] = (uint8_t)((cp & 63) + 128);
                return 4;
            }
            // will return 0 when the code point was too large.
            return 0; // bad r
        }

        // windows only:
        //[DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
        //public static extern void* _aligned_malloc(ulong size, ulong alignment);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bytechar* allocate_padded_buffer(size_t length)
        {
            //return (bytechar*)_aligned_malloc(length + SIMDJSON_PADDING, 64);
            //C#: TODO: _aligned_malloc
            return allocate<bytechar>(length + SIMDJSON_PADDING);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* allocate<T>(size_t len) where T : unmanaged
        {
            return (T*)Marshal.AllocHGlobal(sizeof(T)*(int)len);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void delete<T>(T* buf) where T : unmanaged
        {
            Marshal.FreeHGlobal((IntPtr)buf);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void free(void* buf)
        {
            Marshal.FreeHGlobal((IntPtr)buf);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void aligned_free(void* buf)
        {
            Marshal.FreeHGlobal((IntPtr)buf);
        }
    }
}