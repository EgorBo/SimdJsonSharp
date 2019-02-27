#if JSON_MINIFY // Adds 500kb to binary because of mask128_epi8 table
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System;
using System.Buffers;
using System.Text;

#region stdint types and friends
// if you change something here please change it in other files too
using size_t = System.UInt64;
using uint8_t = System.Byte;
using uint32_t = System.UInt32;
using uint64_t = System.UInt64;
using int64_t = System.Int64;
using bytechar = System.SByte;
using static SimdJsonSharp.Utils;
#endregion

namespace SimdJsonSharp
{
    internal static unsafe partial class JsonMinifier
    {
        // a straightforward comparison of a mask against input.
        private static uint64_t cmp_mask_against_input_mini(Vector256<byte> input_lo, Vector256<byte> input_hi, Vector256<byte> mask)
        {
            var cmp_res_0 = Avx2.CompareEqual(input_lo, mask);
            uint64_t res_0 = (uint32_t)Avx2.MoveMask(cmp_res_0);
            var cmp_res_1 = Avx2.CompareEqual(input_hi, mask);
            uint64_t res_1 = (uint64_t)Avx2.MoveMask(cmp_res_1);
            return res_0 | (res_1 << 32);
        }

        //C#: copied from immintrin.h:
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector256<ulong> _mm256_loadu2_m128i(ulong* hiaddr, ulong* loaddr)
        {
            var hhi = Sse2.LoadVector128(hiaddr);
            var llo = Sse2.LoadVector128(loaddr);
            var casted = llo.ToVector256();
            return Avx.InsertVector128(casted, hhi, 0x1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void _mm256_storeu2_m128i(byte* hiaddr, byte* loaddr, Vector256<byte> a)
        {
            Sse2.Store(loaddr, a.GetLower());
            Sse2.Store(hiaddr, Avx.ExtractVector128(a, 0x1));
        }

        private static readonly Vector256<byte> s_lut_cntrl = Vector256.Create(
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0x00,
            0x00, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0xFF, 0xFF, 0x00, 0x00, 0xFF, 0x00, 0x00);

        private static readonly Vector256<byte> s_low_nibble_mask = Vector256.Create((byte)
            //  0                           9  a   b  c  d
            16, 0, 0, 0, 0, 0, 0, 0, 0, 8, 12, 1, 2, 9, 0, 0, 16, 0, 0, 0, 0, 0,
            0, 0, 0, 8, 12, 1, 2, 9, 0, 0);

        private static readonly Vector256<byte> s_high_nibble_mask = Vector256.Create((byte)
            //  0     2   3     5     7
            8, 0, 18, 4, 0, 1, 0, 1, 0, 0, 0, 3, 2, 1, 0, 0, 8, 0, 18, 4, 0, 1, 0,
            1, 0, 0, 0, 3, 2, 1, 0, 0);


        // take input from buf and remove useless whitespace, input and output can be
        // the same, result is null terminated, return the string length (minus the null termination)
        public static size_t Minify(uint8_t* buf, size_t len, uint8_t* @out)
        {
            if (!Avx2.IsSupported)
                throw new NotSupportedException("AVX2 is required form SimdJson");

            //C#: load const vectors once (there is no `const _m256` in C#)
            Vector256<byte> lut_cntrl = s_lut_cntrl;
            Vector256<byte> low_nibble_mask = s_low_nibble_mask;
            Vector256<byte> high_nibble_mask = s_high_nibble_mask;

            fixed (byte* mask128_epi8 = s_mask128_epi8)
            {
                // Useful constant masks
                const uint64_t even_bits = 0x5555555555555555UL;
                const uint64_t odd_bits = ~even_bits;
                uint8_t* initout = @out;
                uint64_t prev_iter_ends_odd_backslash =
                    0UL; // either 0 or 1, but a 64-bit value
                uint64_t prev_iter_inside_quote = 0UL; // either all zeros or all ones
                size_t idx = 0;
                if (len >= 64)
                {
                    size_t avxlen = len - 63;

                    for (; idx < avxlen; idx += 64)
                    {
                        Vector256<byte> input_lo = Avx.LoadVector256((buf + idx + 0));
                        Vector256<byte> input_hi = Avx.LoadVector256((buf + idx + 32));
                        uint64_t bs_bits = cmp_mask_against_input_mini(input_lo, input_hi,
                            Vector256.Create((byte) '\\'));
                        uint64_t start_edges = bs_bits & ~(bs_bits << 1);
                        uint64_t even_start_mask = even_bits ^ prev_iter_ends_odd_backslash;
                        uint64_t even_starts = start_edges & even_start_mask;
                        uint64_t odd_starts = start_edges & ~even_start_mask;
                        uint64_t even_carries = bs_bits + even_starts;
                        uint64_t odd_carries;
                        bool iter_ends_odd_backslash = add_overflow(
                            bs_bits, odd_starts, &odd_carries);
                        odd_carries |= prev_iter_ends_odd_backslash;
                        prev_iter_ends_odd_backslash = iter_ends_odd_backslash ? 0x1UL : 0x0UL;
                        uint64_t even_carry_ends = even_carries & ~bs_bits;
                        uint64_t odd_carry_ends = odd_carries & ~bs_bits;
                        uint64_t even_start_odd_end = even_carry_ends & odd_bits;
                        uint64_t odd_start_even_end = odd_carry_ends & even_bits;
                        uint64_t odd_ends = even_start_odd_end | odd_start_even_end;
                        uint64_t quote_bits = cmp_mask_against_input_mini(input_lo, input_hi,
                            Vector256.Create((byte) '"'));
                        quote_bits = quote_bits & ~odd_ends;
                        uint64_t quote_mask = Sse2.X64.ConvertToUInt64(Pclmulqdq.CarrylessMultiply(
                            Vector128.Create(quote_bits, 0UL).AsUInt64(), Vector128.Create((byte) 0xFF).AsUInt64(), 0));
                        quote_mask ^= prev_iter_inside_quote;
                        prev_iter_inside_quote =
                            (uint64_t) ((int64_t) quote_mask >>
                                        63); // might be undefined behavior, should be fully defined in C++20, ok according to John Regher from Utah University

                        Vector256<byte> whitespace_shufti_mask = Vector256.Create((byte) 0x18);
                        Vector256<byte> v_lo = Avx2.And(
                            Avx2.Shuffle(low_nibble_mask, input_lo),
                            Avx2.Shuffle(high_nibble_mask,
                                Avx2.And(Avx2.ShiftRightLogical(input_lo.AsUInt32(), 4).AsByte(),
                                    Vector256.Create((byte) 0x7f))));

                        Vector256<byte> v_hi = Avx2.And(
                            Avx2.Shuffle(low_nibble_mask, input_hi),
                            Avx2.Shuffle(high_nibble_mask,
                                Avx2.And(Avx2.ShiftRightLogical(input_hi.AsUInt32(), 4).AsByte(),
                                    Vector256.Create((byte) 0x7f))));
                        Vector256<byte> tmp_ws_lo = Avx2.CompareEqual(
                            Avx2.And(v_lo, whitespace_shufti_mask), Vector256.Create((byte) 0));
                        Vector256<byte> tmp_ws_hi = Avx2.CompareEqual(
                            Avx2.And(v_hi, whitespace_shufti_mask), Vector256.Create((byte) 0));

                        uint64_t ws_res_0 = (uint32_t) Avx2.MoveMask(tmp_ws_lo);
                        uint64_t ws_res_1 = (uint64_t) Avx2.MoveMask(tmp_ws_hi);
                        uint64_t whitespace = ~(ws_res_0 | (ws_res_1 << 32));
                        whitespace &= ~quote_mask;
                        int mask1 = (int) (whitespace & 0xFFFF);
                        int mask2 = (int) ((whitespace >> 16) & 0xFFFF);
                        int mask3 = (int) ((whitespace >> 32) & 0xFFFF);
                        int mask4 = (int) ((whitespace >> 48) & 0xFFFF);
                        int pop1 = (int)hamming((~whitespace) & 0xFFFF);
                        int pop2 = (int)hamming((~whitespace) & (ulong) (0xFFFFFFFF));
                        int pop3 = (int)hamming((~whitespace) & (ulong) (0xFFFFFFFFFFFF));
                        int pop4 = (int)hamming((~whitespace));
                        var vmask1 =
                            _mm256_loadu2_m128i((ulong*)mask128_epi8 + (mask2 & 0x7FFF)*2,
                                (ulong*)mask128_epi8 + (mask1 & 0x7FFF)*2);
                        var vmask2 =
                            _mm256_loadu2_m128i((ulong*)mask128_epi8 + (mask4 & 0x7FFF)*2,
                                (ulong*)mask128_epi8 + (mask3 & 0x7FFF)*2);
                        var result1 = Avx2.Shuffle(input_lo, vmask1.AsByte());
                        var result2 = Avx2.Shuffle(input_hi, vmask2.AsByte());
                        _mm256_storeu2_m128i((@out + pop1), @out, result1);
                        _mm256_storeu2_m128i((@out + pop3), (@out + pop2),
                            result2);
                        @out += pop4;
                    }
                }

                // we finish off the job... copying and pasting the code is not ideal here,
                // but it gets the job done.
                if (idx < len)
                {
                    uint8_t* buffer = stackalloc uint8_t[64];
                    memset(buffer, 0, 64);
                    memcpy(buffer, buf + idx, len - idx);
                    var input_lo = Avx.LoadVector256((buffer));
                    var input_hi = Avx.LoadVector256((buffer + 32));
                    uint64_t bs_bits =
                        cmp_mask_against_input_mini(input_lo, input_hi, Vector256.Create((byte) '\\'));
                    uint64_t start_edges = bs_bits & ~(bs_bits << 1);
                    uint64_t even_start_mask = even_bits ^ prev_iter_ends_odd_backslash;
                    uint64_t even_starts = start_edges & even_start_mask;
                    uint64_t odd_starts = start_edges & ~even_start_mask;
                    uint64_t even_carries = bs_bits + even_starts;
                    uint64_t odd_carries;
                    //bool iter_ends_odd_backslash = 
                    add_overflow(bs_bits, odd_starts, &odd_carries);
                    odd_carries |= prev_iter_ends_odd_backslash;
                    //prev_iter_ends_odd_backslash = iter_ends_odd_backslash ? 0x1ULL : 0x0ULL; // we never use it
                    uint64_t even_carry_ends = even_carries & ~bs_bits;
                    uint64_t odd_carry_ends = odd_carries & ~bs_bits;
                    uint64_t even_start_odd_end = even_carry_ends & odd_bits;
                    uint64_t odd_start_even_end = odd_carry_ends & even_bits;
                    uint64_t odd_ends = even_start_odd_end | odd_start_even_end;
                    uint64_t quote_bits =
                        cmp_mask_against_input_mini(input_lo, input_hi, Vector256.Create((byte) '"'));
                    quote_bits = quote_bits & ~odd_ends;
                    uint64_t quote_mask = Sse2.X64.ConvertToUInt64(Pclmulqdq.CarrylessMultiply(
                        Vector128.Create(quote_bits, 0UL), Vector128.Create((byte) 0xFF).AsUInt64(), 0));
                    quote_mask ^= prev_iter_inside_quote;
                    // prev_iter_inside_quote = (uint64_t)((int64_t)quote_mask >> 63);// we don't need this anymore

                    Vector256<byte> mask_20 = Vector256.Create((byte) 0x20); // c==32
                    Vector256<byte> mask_70 =
                        Vector256.Create((byte) 0x70); // adding 0x70 does not check low 4-bits
                    // but moves any value >= 16 above 128

                    Vector256<byte> tmp_ws_lo = Avx2.Or(
                        Avx2.CompareEqual(mask_20, input_lo),
                        Avx2.Shuffle(lut_cntrl, Avx2.AddSaturate(mask_70, input_lo)));
                    Vector256<byte> tmp_ws_hi = Avx2.Or(
                        Avx2.CompareEqual(mask_20, input_hi),
                        Avx2.Shuffle(lut_cntrl, Avx2.AddSaturate(mask_70, input_hi)));
                    uint64_t ws_res_0 = (uint32_t) Avx2.MoveMask(tmp_ws_lo);
                    uint64_t ws_res_1 = (uint64_t) Avx2.MoveMask(tmp_ws_hi);
                    uint64_t whitespace = (ws_res_0 | (ws_res_1 << 32));
                    whitespace &= ~quote_mask;

                    if (len - idx < 64)
                    {
                        whitespace |= ((0xFFFFFFFFFFFFFFFF) << (int)(len - idx));
                    }

                    int mask1 = (int) (whitespace & 0xFFFF);
                    int mask2 = (int) ((whitespace >> 16) & 0xFFFF);
                    int mask3 = (int) ((whitespace >> 32) & 0xFFFF);
                    int mask4 = (int) ((whitespace >> 48) & 0xFFFF);
                    int pop1 = (int)hamming((~whitespace) & 0xFFFF);
                    int pop2 = (int)hamming((~whitespace) & 0xFFFFFFFF);
                    int pop3 = (int)hamming((~whitespace) & 0xFFFFFFFFFFFF);
                    int pop4 = (int)hamming((~whitespace));

                    var vmask1 =
                        _mm256_loadu2_m128i((ulong*)mask128_epi8 + (mask2 & 0x7FFF)*2,
                                            (ulong*)mask128_epi8 + (mask1 & 0x7FFF)*2);
                    var vmask2 =
                        _mm256_loadu2_m128i((ulong*)mask128_epi8 + (mask4 & 0x7FFF)*2,
                                            (ulong*)mask128_epi8 + (mask3 & 0x7FFF)*2);
                    var result1 = Avx2.Shuffle(input_lo, vmask1.AsByte());
                    var result2 = Avx2.Shuffle(input_hi, vmask2.AsByte());
                    _mm256_storeu2_m128i((buffer + pop1), buffer,
                        result1);
                    _mm256_storeu2_m128i((buffer + pop3), (buffer + pop2),
                        result2);
                    memcpy(@out, buffer, (size_t) pop4);
                    @out += pop4;
                }

                *@out = (byte) '\0'; // NULL termination
                return (size_t)@out - (size_t)initout;
            }
        }
    }
}
#endif