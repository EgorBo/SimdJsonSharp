// This file is a manual port of C code https://github.com/lemire/simdjson to C#
// (c) Daniel Lemire and Geoff Langdale

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

using static SimdJsonSharp.Utils;

#region stdint types and friends
using size_t = System.UInt64;
using char1 = System.Byte;
using uint8_t = System.Byte;
using uint32_t = System.UInt32;
#endregion

namespace SimdJsonSharp
{
    internal static unsafe class stringparsing
    {
        // begin copypasta
        // These chars yield themselves: " \ /
        // b -> backspace, f -> formfeed, n -> newline, r -> cr, t -> horizontal tab
        // u not handled in this table as it's complex
        static ReadOnlySpan<byte> escape_map => new uint8_t[256] // Roslyn hack
        {
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0x0.
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0x22, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x2f,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,

            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0x4.
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x5c, 0, 0, 0, // 0x5.
            0, 0, 0x08, 0, 0, 0, 0x0c, 0, 0, 0, 0, 0, 0, 0, 0x0a, 0, // 0x6.
            0, 0, 0x0d, 0, 0x09, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // 0x7.

            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,

            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        };

        // handle a unicode codepoint
        // write appropriate values into dest
        // src will advance 6 bytes or 12 bytes
        // dest will advance a variable amount (return via pointer)
        // return true if the unicode codepoint was valid
        // We work in little-endian then swap at write time

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool handle_unicode_codepoint(uint8_t** src_ptr, uint8_t** dst_ptr)
        {
            // hex_to_u32_nocheck fills high 16 bits of the return value with 1s if the
            // conversion isn't valid; we defer the check for this to inside the 
            // multilingual plane check
            uint32_t code_point = hex_to_u32_nocheck(*src_ptr + 2);
            *src_ptr += 6;
            // check for low surrogate for characters outside the Basic
            // Multilingual Plane.
            if (code_point >= 0xd800 && code_point < 0xdc00)
            {
                if (((*src_ptr)[0] != '\\') || (*src_ptr)[1] != 'u')
                {
                    return false;
                }

                uint32_t code_point_2 = hex_to_u32_nocheck(*src_ptr + 2);

                // if the first code point is invalid we will get here, as we will go past
                // the check for being outside the Basic Multilingual plane. If we don't
                // find a \u immediately afterwards we fail out anyhow, but if we do, 
                // this check catches both the case of the first code point being invalid
                // or the second code point being invalid.
                if ((code_point | code_point_2) >> 16 != 0)
                {
                    return false;
                }

                code_point = (((code_point - 0xd800) << 10) | (code_point_2 - 0xdc00)) + 0x10000;
                *src_ptr += 6;
            }

            size_t offset = codepoint_to_utf8(code_point, *dst_ptr);
            *dst_ptr += offset;
            return offset > 0;
        }

        // Holds backslashes and quotes locations.
        internal struct parse_string_helper
        {
            public uint32_t bs_bits;
            public uint32_t quote_bits;
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static parse_string_helper find_bs_bits_and_quote_bits(uint8_t* src, uint8_t* dst)
        {
            if (Avx2.IsSupported)
            {
                // this can read up to 31 bytes beyond the buffer size, but we require 
                // SIMDJSON_PADDING of padding
                var v = Avx.LoadVector256(src);
                // store to dest unconditionally - we can overwrite the bits we don't like
                // later
                Avx.Store((dst), v);
                var quote_mask = Avx2.CompareEqual(v, Vector256.Create((uint8_t) '"'));
                return new parse_string_helper
                {
                    bs_bits = (uint32_t) Avx2.MoveMask(Avx2.CompareEqual(v, Vector256.Create((uint8_t) '\\'))), // bs_bits
                    quote_bits = (uint32_t) Avx2.MoveMask(quote_mask) // quote_bits
                };
            }
            else // SSE42
            {
                // this can read up to 31 bytes beyond the buffer size, but we require 
                // SIMDJSON_PADDING of padding
                var v = Sse2.LoadVector128((src));
                // store to dest unconditionally - we can overwrite the bits we don't like
                // later
                Sse2.Store((dst), v);
                var quote_mask = Sse2.CompareEqual(v, Vector128.Create((uint8_t) '"'));
                return new parse_string_helper
                {
                    bs_bits = (uint32_t) Sse2.MoveMask(Sse2.CompareEqual(v,
                        Vector128.Create((uint8_t) '\\'))), // bs_bits
                    quote_bits = (uint32_t) Sse2.MoveMask(quote_mask) // quote_bits
                };
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool parse_string(uint8_t* buf, size_t len, ParsedJson pj, uint32_t depth, uint32_t offset)
        {
            pj.WriteTape((ulong) (pj.current_string_buf_loc - pj.string_buf), (char1) '"');
            uint8_t* src = &buf[offset + 1]; // we know that buf at offset is a "
            uint8_t* dst = pj.current_string_buf_loc + sizeof(uint32_t);
            uint8_t* start_of_string = dst;
            while (true)
            {
                parse_string_helper helper = find_bs_bits_and_quote_bits(src, dst);
                if (((helper.bs_bits - 1) & helper.quote_bits) != 0)
                {
                    // we encountered quotes first. Move dst to point to quotes and exit
                    // find out where the quote is...
                    uint32_t quote_dist = (uint32_t) trailingzeroes(helper.quote_bits);

                    // NULL termination is still handy if you expect all your strings to be NULL terminated?
                    // It comes at a small cost
                    dst[quote_dist] = 0;

                    uint32_t str_length = (uint32_t) ((dst - start_of_string) + quote_dist);
                    memcpy(pj.current_string_buf_loc, &str_length, sizeof(uint32_t));
                    ///////////////////////
                    // Above, check for overflow in case someone has a crazy string (>=4GB?)
                    // But only add the overflow check when the document itself exceeds 4GB
                    // Currently unneeded because we refuse to parse docs larger or equal to 4GB.
                    ////////////////////////

                    // we advance the point, accounting for the fact that we have a NULL termination
                    pj.current_string_buf_loc = dst + quote_dist + 1;

                    return true;
                }

                if (((helper.quote_bits - 1) & helper.bs_bits) != 0)
                {
                    // find out where the backspace is
                    uint32_t bs_dist = (uint32_t) trailingzeroes(helper.bs_bits);
                    uint8_t escape_char = src[bs_dist + 1];
                    // we encountered backslash first. Handle backslash
                    if (escape_char == 'u')
                    {
                        // move src/dst up to the start; they will be further adjusted
                        // within the unicode codepoint handling code.
                        src += bs_dist;
                        dst += bs_dist;
                        if (!handle_unicode_codepoint(&src, &dst))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        // simple 1:1 conversion. Will eat bs_dist+2 characters in input and
                        // write bs_dist+1 characters to output
                        // note this may reach beyond the part of the buffer we've actually
                        // seen. I think this is ok
                        uint8_t escape_result = escape_map[escape_char]; // TODO: https://github.com/dotnet/coreclr/issues/25894
                        if (escape_result == 0u)
                        {
                            return false; // bogus escape value is an error
                        }

                        dst[bs_dist] = escape_result;
                        src += bs_dist + 2;
                        dst += bs_dist + 1;
                    }
                }
                else
                {
                    // they are the same. Since they can't co-occur, it means we encountered
                    // neither.
                    if (!Avx2.IsSupported)
                    {
                        src += 16; // sse42
                        dst += 16;
                    }
                    else
                    {
                        src += 32; // avx2
                        dst += 32;
                    }
                }
            }
        }
    }
}
