// This file is a manual port of C code https://github.com/lemire/simdjson to C#
// (c) Daniel Lemire and Geoff Langdale

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

#region stdint types and friends
// if you change something here please change it in other files too
using size_t = System.UInt64;
using uint8_t = System.Byte;
using uint64_t = System.UInt64;
using uint32_t = System.UInt32;
using int64_t = System.Int64;
using char1 = System.SByte;
using static SimdJsonSharp.Utils;
#endregion


namespace SimdJsonSharp
{
    [StructLayout(LayoutKind.Explicit)]
    internal struct simd_input
    {
        // AVX2
        [FieldOffset(0)]
        internal Vector256<uint8_t> lo;
        [FieldOffset(32)]
        internal Vector256<uint8_t> hi;

        // SSE42
        [FieldOffset(0)]
        internal Vector128<uint8_t> v0;
        [FieldOffset(16)]
        internal Vector128<uint8_t> v1;
        [FieldOffset(32)]
        internal Vector128<uint8_t> v2;
        [FieldOffset(48)]
        internal Vector128<uint8_t> v3;
    }


    internal static unsafe class stage1_find_marks
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint64_t compute_quote_mask(uint64_t quote_bits)
        {
            // There should be no such thing with a processing supporting avx2
            // but not clmul.
            if (Pclmulqdq.IsSupported)
            {
                uint64_t quote_mask = Sse2.X64.ConvertToUInt64(Pclmulqdq.CarrylessMultiply(
                    Vector128.Create(quote_bits, 0UL /*C# reversed*/), Vector128.Create((byte) 0xFF).AsUInt64(), 0));
                return quote_mask;
            }
            else
            {
                uint64_t quote_mask = quote_bits ^ (quote_bits << 1);
                quote_mask = quote_mask ^ (quote_mask << 2);
                quote_mask = quote_mask ^ (quote_mask << 4);
                quote_mask = quote_mask ^ (quote_mask << 8);
                quote_mask = quote_mask ^ (quote_mask << 16);
                quote_mask = quote_mask ^ (quote_mask << 32);
                return quote_mask;
            }
        }

#if SIMDJSON_UTF8VALIDATE
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void check_utf8(simd_input @in, utf8_checking_state state)
        {
            //TODO:
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static simd_input fill_input(uint8_t* ptr)
        {
            if (Avx2.IsSupported)
            {
                simd_input @in = new simd_input();
                @in.lo = Avx.LoadVector256((ptr + 0));
                @in.hi = Avx.LoadVector256((ptr + 32));
                return @in;
            }
            else
            {
                simd_input @in = new simd_input();
                @in.v0 = Sse2.LoadVector128((ptr + 0));
                @in.v1 = Sse2.LoadVector128((ptr + 16));
                @in.v2 = Sse2.LoadVector128((ptr + 32));
                @in.v3 = Sse2.LoadVector128((ptr + 48));
                return @in;
            }
        }


        // a straightforward comparison of a mask against input. 5 uops; would be
        // cheaper in AVX512.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint64_t cmp_mask_against_input(simd_input @in, uint8_t m)
        {
            if (Avx2.IsSupported)
            {
                Vector256<byte> mask = Vector256.Create(m);
                Vector256<byte> cmp_res_0 = Avx2.CompareEqual(@in.lo, mask);
                uint64_t res_0 = (uint32_t) Avx2.MoveMask(cmp_res_0);
                Vector256<byte> cmp_res_1 = Avx2.CompareEqual(@in.hi, mask);
                uint64_t res_1 = (uint64_t) Avx2.MoveMask(cmp_res_1);
                return res_0 | (res_1 << 32);
            }
            else
            {
                Vector128<byte> mask = Vector128.Create(m);
                Vector128<byte> cmp_res_0 = Sse2.CompareEqual(@in.v0, mask);
                uint64_t res_0 = (uint64_t)Sse2.MoveMask(cmp_res_0);
                Vector128<byte> cmp_res_1 = Sse2.CompareEqual(@in.v1, mask);
                uint64_t res_1 = (uint64_t)Sse2.MoveMask(cmp_res_1);
                Vector128<byte> cmp_res_2 = Sse2.CompareEqual(@in.v2, mask);
                uint64_t res_2 = (uint64_t)Sse2.MoveMask(cmp_res_2);
                Vector128<byte> cmp_res_3 = Sse2.CompareEqual(@in.v3, mask);
                uint64_t res_3 = (uint64_t)Sse2.MoveMask(cmp_res_3);
                return res_0 | (res_1 << 16) | (res_2 << 32) | (res_3 << 48);
            }
        }

        // find all values less than or equal than the content of maxval (using unsigned arithmetic) 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint64_t unsigned_lteq_against_input(simd_input @in, uint8_t m)
        {
            if (Avx2.IsSupported)
            {
                var maxval = Vector256.Create(m);
                var cmp_res_0 = Avx2.CompareEqual(Avx2.Max(maxval, @in.lo), maxval);
                uint64_t res_0 = (uint32_t)(Avx2.MoveMask(cmp_res_0));
                var cmp_res_1 = Avx2.CompareEqual(Avx2.Max(maxval, @in.hi), maxval);
                uint64_t res_1 = (uint64_t)Avx2.MoveMask(cmp_res_1);
                return res_0 | (res_1 << 32);
            }
            else
            {
                var maxval = Vector128.Create(m);
                var cmp_res_0 = Sse2.CompareEqual(Sse2.Max(maxval, @in.v0), maxval);
                uint64_t res_0 = (uint64_t)Sse2.MoveMask(cmp_res_0);
                var cmp_res_1 = Sse2.CompareEqual(Sse2.Max(maxval, @in.v1), maxval);
                uint64_t res_1 = (uint64_t)Sse2.MoveMask(cmp_res_1);
                var cmp_res_2 = Sse2.CompareEqual(Sse2.Max(maxval, @in.v2), maxval);
                uint64_t res_2 = (uint64_t)Sse2.MoveMask(cmp_res_2);
                var cmp_res_3 = Sse2.CompareEqual(Sse2.Max(maxval, @in.v3), maxval);
                uint64_t res_3 = (uint64_t)Sse2.MoveMask(cmp_res_3);
                return res_0 | (res_1 << 16) | (res_2 << 32) | (res_3 << 48);
            }
        }

        // return a bitvector indicating where we have characters that end an odd-length
        // sequence of backslashes (and thus change the behavior of the next character
        // to follow). A even-length sequence of backslashes, and, for that matter, the
        // largest even-length prefix of our odd-length sequence of backslashes, simply
        // modify the behavior of the backslashes themselves.
        // We also update the prev_iter_ends_odd_backslash reference parameter to
        // indicate whether we end an iteration on an odd-length sequence of
        // backslashes, which modifies our subsequent search for odd-length
        // sequences of backslashes in an obvious way.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint64_t find_odd_backslash_sequences(simd_input @in, ref uint64_t prev_iter_ends_odd_backslash)
        {
            const uint64_t even_bits = 0x5555555555555555UL;
            const uint64_t odd_bits = ~even_bits;
            uint64_t bs_bits = cmp_mask_against_input(@in, (uint8_t)'\\');
            uint64_t start_edges = bs_bits & ~(bs_bits << 1);
            // flip lowest if we have an odd-length run at the end of the prior
            // iteration
            uint64_t even_start_mask = even_bits ^ prev_iter_ends_odd_backslash;
            uint64_t even_starts = start_edges & even_start_mask;
            uint64_t odd_starts = start_edges & ~even_start_mask;
            uint64_t even_carries = bs_bits + even_starts;

            uint64_t odd_carries;
            // must record the carry-out of our odd-carries out of bit 63; this
            // indicates whether the sense of any edge going to the next iteration
            // should be flipped
            bool iter_ends_odd_backslash =
                add_overflow(bs_bits, odd_starts, &odd_carries);

            odd_carries |=
                prev_iter_ends_odd_backslash;  // push in bit zero as a potential end
            // if we had an odd-numbered run at the
            // end of the previous iteration
            prev_iter_ends_odd_backslash = iter_ends_odd_backslash ? 0x1UL : 0x0UL;
            uint64_t even_carry_ends = even_carries & ~bs_bits;
            uint64_t odd_carry_ends = odd_carries & ~bs_bits;
            uint64_t even_start_odd_end = even_carry_ends & odd_bits;
            uint64_t odd_start_even_end = odd_carry_ends & even_bits;
            uint64_t odd_ends = even_start_odd_end | odd_start_even_end;
            return odd_ends;
        }

        // return both the quote mask (which is a half-open mask that covers the first
        // quote
        // in an unescaped quote pair and everything in the quote pair) and the quote
        // bits, which are the simple
        // unescaped quoted bits. We also update the prev_iter_inside_quote value to
        // tell the next iteration
        // whether we finished the final iteration inside a quote pair; if so, this
        // inverts our behavior of
        // whether we're inside quotes for the next iteration.
        // Note that we don't do any error checking to see if we have backslash
        // sequences outside quotes; these
        // backslash sequences (of any length) will be detected elsewhere.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint64_t find_quote_mask_and_bits(simd_input @in, uint64_t odd_ends,
            ref uint64_t prev_iter_inside_quote, ref uint64_t quote_bits, ref uint64_t error_mask)
        {
            quote_bits = cmp_mask_against_input(@in, (uint8_t)'"');
            quote_bits = quote_bits & ~odd_ends;
            uint64_t quote_mask = compute_quote_mask(quote_bits);
            quote_mask ^= prev_iter_inside_quote;
            // All Unicode characters may be placed within the
            // quotation marks, except for the characters that MUST be escaped:
            // quotation mark, reverse solidus, and the control characters (U+0000
            //through U+001F).
            // https://tools.ietf.org/html/rfc8259
            uint64_t unescaped = unsigned_lteq_against_input(@in, 0x1F);
            error_mask |= quote_mask & unescaped;
            // right shift of a signed value expected to be well-defined and standard
            // compliant as of C++20,
            // John Regher from Utah U. says this is fine code
            prev_iter_inside_quote = (uint64_t)((int64_t)(quote_mask) >> 63);
            return quote_mask;
        }


        internal static readonly Vector256<byte> structural_table_avx = Vector256.Create( // TODO: reverse order?
            (uint8_t)44, 125, 0, 0, 0xc0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 58, 123,
            44, 125, 0, 0, 0xc0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 58, 123);
        internal static readonly Vector256<byte> white_table_avx = Vector256.Create( // TODO: reverse order?
            (uint8_t)32, 100, 100, 100, 17, 100, 113, 2, 100, 9, 10, 112, 100, 13, 100, 100,
            32, 100, 100, 100, 17, 100, 113, 2, 100, 9, 10, 112, 100, 13, 100, 100);

        internal static readonly Vector128<byte> structural_table_sse = Vector128.Create(// TODO: reverse order?
            (uint8_t)44, 125, 0, 0, 0xc0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 58, 123);
        internal static readonly Vector128<byte> white_table_sse = Vector128.Create( // TODO: reverse order?
            (uint8_t)32, 100, 100, 100, 17, 100, 113, 2, 100, 9, 10, 112, 100, 13, 100, 100);

        // do a 'shufti' to detect structural JSON characters
        // they are { 0x7b } 0x7d : 0x3a [ 0x5b ] 0x5d , 0x2c
        // these go into the first 3 buckets of the comparison (1/2/4)

        // we are also interested in the four whitespace characters
        // space 0x20, linefeed 0x0a, horizontal tab 0x09 and carriage return 0x0d
        // these go into the next 2 buckets of the comparison (8/16)

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void find_whitespace_and_structurals(simd_input @in, ref uint64_t whitespace, ref uint64_t structurals)
        {
            if (Avx2.IsSupported)
            {

                Vector256<byte> struct_offset = Vector256.Create((uint8_t)0xd4);
                Vector256<byte> struct_mask = Vector256.Create((uint8_t)32);

                Vector256<byte> lo_white = Avx2.CompareEqual(@in.lo,
                    Avx2.Shuffle(white_table_avx, @in.lo));
                Vector256<byte> hi_white = Avx2.CompareEqual(@in.hi,
                    Avx2.Shuffle(white_table_avx, @in.hi));
                uint64_t ws_res_0 = (uint32_t)(Avx2.MoveMask(lo_white));
                uint64_t ws_res_1 = (uint64_t)Avx2.MoveMask(hi_white);
                whitespace = (ws_res_0 | (ws_res_1 << 32));
                Vector256<byte> lo_struct_r1 = Avx2.Add(struct_offset, @in.lo);
                Vector256<byte> hi_struct_r1 = Avx2.Add(struct_offset, @in.hi);
                Vector256<byte> lo_struct_r2 = Avx2.Or(@in.lo, struct_mask);
                Vector256<byte> hi_struct_r2 = Avx2.Or(@in.hi, struct_mask);
                Vector256<byte> lo_struct_r3 = Avx2.Shuffle(structural_table_avx, lo_struct_r1);
                Vector256<byte> hi_struct_r3 = Avx2.Shuffle(structural_table_avx, hi_struct_r1);
                Vector256<byte> lo_struct = Avx2.CompareEqual(lo_struct_r2, lo_struct_r3);
                Vector256<byte> hi_struct = Avx2.CompareEqual(hi_struct_r2, hi_struct_r3);

                uint64_t structural_res_0 =
                    (uint32_t)(Avx2.MoveMask(lo_struct));
                uint64_t structural_res_1 = (uint64_t)Avx2.MoveMask(hi_struct);
                structurals = (structural_res_0 | (structural_res_1 << 32));
            }
            else
            {
                Vector128<byte> struct_offset = Vector128.Create((byte)0xd4);
                Vector128<byte> struct_mask = Vector128.Create((byte)32);

                Vector128<byte> white0 = Sse2.CompareEqual(@in.v0,
                         Ssse3.Shuffle(white_table_sse, @in.v0));
                Vector128<byte> white1 = Sse2.CompareEqual(@in.v1,
                         Ssse3.Shuffle(white_table_sse, @in.v1));
                Vector128<byte> white2 = Sse2.CompareEqual(@in.v2,
                         Ssse3.Shuffle(white_table_sse, @in.v2));
                Vector128<byte> white3 = Sse2.CompareEqual(@in.v3,
                         Ssse3.Shuffle(white_table_sse, @in.v3));
                uint64_t ws_res_0 = (uint64_t)Sse2.MoveMask(white0);
                uint64_t ws_res_1 = (uint64_t)Sse2.MoveMask(white1);
                uint64_t ws_res_2 = (uint64_t)Sse2.MoveMask(white2);
                uint64_t ws_res_3 = (uint64_t)Sse2.MoveMask(white3);

                whitespace = (ws_res_0 | (ws_res_1 << 16) | (ws_res_2 << 32) | (ws_res_3 << 48));

                Vector128<byte> struct1_r1 = Sse2.Add(struct_offset, @in.v0);
                Vector128<byte> struct2_r1 = Sse2.Add(struct_offset, @in.v1);
                Vector128<byte> struct3_r1 = Sse2.Add(struct_offset, @in.v2);
                Vector128<byte> struct4_r1 = Sse2.Add(struct_offset, @in.v3);

                Vector128<byte> struct1_r2 = Sse2.Or(@in.v0, struct_mask);
                Vector128<byte> struct2_r2 = Sse2.Or(@in.v1, struct_mask);
                Vector128<byte> struct3_r2 = Sse2.Or(@in.v2, struct_mask);
                Vector128<byte> struct4_r2 = Sse2.Or(@in.v3, struct_mask);

                Vector128<byte> struct1_r3 = Ssse3.Shuffle(structural_table_sse, struct1_r1);
                Vector128<byte> struct2_r3 = Ssse3.Shuffle(structural_table_sse, struct2_r1);
                Vector128<byte> struct3_r3 = Ssse3.Shuffle(structural_table_sse, struct3_r1);
                Vector128<byte> struct4_r3 = Ssse3.Shuffle(structural_table_sse, struct4_r1);

                Vector128<byte> struct1 = Sse2.CompareEqual(struct1_r2, struct1_r3);
                Vector128<byte> struct2 = Sse2.CompareEqual(struct2_r2, struct2_r3);
                Vector128<byte> struct3 = Sse2.CompareEqual(struct3_r2, struct3_r3);
                Vector128<byte> struct4 = Sse2.CompareEqual(struct4_r2, struct4_r3);

                uint64_t structural_res_0 = (uint64_t)Sse2.MoveMask(struct1);
                uint64_t structural_res_1 = (uint64_t)Sse2.MoveMask(struct2);
                uint64_t structural_res_2 = (uint64_t)Sse2.MoveMask(struct3);
                uint64_t structural_res_3 = (uint64_t)Sse2.MoveMask(struct4);

                structurals = (structural_res_0 | (structural_res_1 << 16) | (structural_res_2 << 32) | (structural_res_3 << 48));
            }
        }

        // flatten out values in 'bits' assuming that they are are to have values of idx
        // plus their position in the bitvector, and store these indexes at
        // base_ptr[base] incrementing base as we go
        // will potentially store extra values beyond end of valid bits, so base_ptr
        // needs to be large enough to handle this
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void flatten_bits(uint32_t* base_ptr, ref uint32_t @base, uint32_t idx, uint64_t bits)
        {
            // In some instances, the next branch is expensive because it is mispredicted. 
            // Unfortunately, in other cases,
            // it helps tremendously.
            if (bits == 0) return;
            uint32_t cnt = (uint32_t)hamming(bits);
            uint32_t next_base = @base + cnt;
            idx -= 64;
            base_ptr += @base;
            {
                base_ptr[0] = (uint32_t)(idx + trailingzeroes(bits));
                bits = bits & (bits - 1);
                base_ptr[1] = (uint32_t)(idx + trailingzeroes(bits));
                bits = bits & (bits - 1);
                base_ptr[2] = (uint32_t)(idx + trailingzeroes(bits));
                bits = bits & (bits - 1);
                base_ptr[3] = (uint32_t)(idx + trailingzeroes(bits));
                bits = bits & (bits - 1);
                base_ptr[4] = (uint32_t)(idx + trailingzeroes(bits));
                bits = bits & (bits - 1);
                base_ptr[5] = (uint32_t)(idx + trailingzeroes(bits));
                bits = bits & (bits - 1);
                base_ptr[6] = (uint32_t)(idx + trailingzeroes(bits));
                bits = bits & (bits - 1);
                base_ptr[7] = (uint32_t)(idx + trailingzeroes(bits));
                bits = bits & (bits - 1);
                base_ptr += 8;
            }
            // We hope that the next branch is easily predicted.
            if (cnt > 8)
            {
                base_ptr[0] = (uint32_t)(idx + trailingzeroes(bits));
                bits = bits & (bits - 1);
                base_ptr[1] = (uint32_t)(idx + trailingzeroes(bits));
                bits = bits & (bits - 1);
                base_ptr[2] = (uint32_t)(idx + trailingzeroes(bits));
                bits = bits & (bits - 1);
                base_ptr[3] = (uint32_t)(idx + trailingzeroes(bits));
                bits = bits & (bits - 1);
                base_ptr[4] = (uint32_t)(idx + trailingzeroes(bits));
                bits = bits & (bits - 1);
                base_ptr[5] = (uint32_t)(idx + trailingzeroes(bits));
                bits = bits & (bits - 1);
                base_ptr[6] = (uint32_t)(idx + trailingzeroes(bits));
                bits = bits & (bits - 1);
                base_ptr[7] = (uint32_t)(idx + trailingzeroes(bits));
                bits = bits & (bits - 1);
                base_ptr += 8;
            }
            if (cnt > 16)
            { // unluckly: we rarely get here
              // since it means having one structural or pseudo-structral element 
              // every 4 characters (possible with inputs like "","","",...).
                do
                {
                    base_ptr[0] = (uint32_t)(idx + trailingzeroes(bits));
                    bits = bits & (bits - 1);
                    base_ptr++;
                } while (bits != 0);
            }
            @base = next_base;
        }


        // return a updated structural bit vector with quoted contents cleared out and
        // pseudo-structural characters added to the mask
        // updates prev_iter_ends_pseudo_pred which tells us whether the previous
        // iteration ended on a whitespace or a structural character (which means that
        // the next iteration
        // will have a pseudo-structural character at its start)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint64_t finalize_structurals(uint64_t structurals, uint64_t whitespace, 
            uint64_t quote_mask, uint64_t quote_bits, ref uint64_t prev_iter_ends_pseudo_pred)
        {
            // mask off anything inside quotes
            structurals &= ~quote_mask;
            // add the real quote bits back into our bitmask as well, so we can
            // quickly traverse the strings we've spent all this trouble gathering
            structurals |= quote_bits;
            // Now, establish "pseudo-structural characters". These are non-whitespace
            // characters that are (a) outside quotes and (b) have a predecessor that's
            // either whitespace or a structural character. This means that subsequent
            // passes will get a chance to encounter the first character of every string
            // of non-whitespace and, if we're parsing an atom like true/false/null or a
            // number we can stop at the first whitespace or structural character
            // following it.

            // a qualified predecessor is something that can happen 1 position before an
            // pseudo-structural character
            uint64_t pseudo_pred = structurals | whitespace;

            uint64_t shifted_pseudo_pred =
                (pseudo_pred << 1) | prev_iter_ends_pseudo_pred;
            prev_iter_ends_pseudo_pred = pseudo_pred >> 63;
            uint64_t pseudo_structurals =
                shifted_pseudo_pred & (~whitespace) & (~quote_mask);
            structurals |= pseudo_structurals;

            // now, we've used our close quotes all we need to. So let's switch them off
            // they will be off in the quote mask and on in quote bits.
            structurals &= ~(quote_bits & ~quote_mask);
            return structurals;
        }

        internal static JsonParseError find_structural_bits(uint8_t* buf, size_t len, ParsedJson pj)
        {
            if (len > pj.bytecapacity)
            {
                return JsonParseError.CAPACITY;
            }

            uint32_t* base_ptr = pj.structural_indexes;
            uint32_t @base = 0;
#if SIMDJSON_UTF8VALIDATE
            utf8_checking_state state;
#endif

            // we have padded the input out to 64 byte multiple with the remainder being
            // zeros

            // persistent state across loop
            // does the last iteration end with an odd-length sequence of backslashes? 
            // either 0 or 1, but a 64-bit value
            uint64_t prev_iter_ends_odd_backslash = 0UL;
            // does the previous iteration end inside a double-quote pair?
            uint64_t prev_iter_inside_quote = 0UL; // either all zeros or all ones
            // does the previous iteration end on something that is a predecessor of a
            // pseudo-structural character - i.e. whitespace or a structural character
            // effectively the very first char is considered to follow "whitespace" for
            // the
            // purposes of pseudo-structural character detection so we initialize to 1
            uint64_t prev_iter_ends_pseudo_pred = 1UL;

            // structurals are persistent state across loop as we flatten them on the
            // subsequent iteration into our array pointed to be base_ptr.
            // This is harmless on the first iteration as structurals==0
            // and is done for performance reasons; we can hide some of the latency of the
            // expensive carryless multiply in the previous step with this work
            uint64_t structurals = 0;

            size_t lenminus64 = len < 64 ? 0 : len - 64;
            size_t idx = 0;
            uint64_t error_mask = 0; // for unescaped characters within strings (ASCII code points < 0x20)

            for (; idx < lenminus64; idx += 64)
            {
                //__builtin_prefetch(buf + idx + 128);
                simd_input @in = fill_input(buf + idx);
#if SIMDJSON_UTF8VALIDATE
                check_utf8(in, state);
#endif
                // detect odd sequences of backslashes
                uint64_t odd_ends = find_odd_backslash_sequences(
                    @in, ref prev_iter_ends_odd_backslash);

                // detect insides of quote pairs ("quote_mask") and also our quote_bits
                // themselves
                uint64_t quote_bits = 0;
                uint64_t quote_mask = find_quote_mask_and_bits(
                    @in, odd_ends, ref prev_iter_inside_quote, ref quote_bits, ref error_mask);

                // take the previous iterations structural bits, not our current iteration,
                // and flatten
                flatten_bits(base_ptr, ref @base, (uint32_t) idx, structurals);

                uint64_t whitespace = 0;
                find_whitespace_and_structurals(@in, ref whitespace, ref structurals);

                // fixup structurals to reflect quotes and add pseudo-structural characters
                structurals = finalize_structurals(structurals, whitespace, quote_mask,
                    quote_bits, ref prev_iter_ends_pseudo_pred);
            }

            ////////////////
            // we use a giant copy-paste which is ugly.
            // but otherwise the string needs to be properly padded or else we
            // risk invalidating the UTF-8 checks.
            ////////////
            if (idx < len)
            {
                uint8_t* tmpbuf = stackalloc uint8_t[64];
                memset(tmpbuf, 0x20, 64);
                memcpy(tmpbuf, buf + idx, len - idx);
                simd_input @in = fill_input(tmpbuf);
#if SIMDJSON_UTF8VALIDATE
                check_utf8<T>(in, state);
#endif
                // detect odd sequences of backslashes
                uint64_t odd_ends = find_odd_backslash_sequences(
                    @in, ref prev_iter_ends_odd_backslash);

                // detect insides of quote pairs ("quote_mask") and also our quote_bits
                // themselves
                uint64_t quote_bits = 0;
                uint64_t quote_mask = find_quote_mask_and_bits(
                    @in, odd_ends, ref prev_iter_inside_quote, ref quote_bits, ref error_mask);

                // take the previous iterations structural bits, not our current iteration,
                // and flatten
                flatten_bits(base_ptr, ref @base, (uint) idx, structurals);

                uint64_t whitespace = 0;
                find_whitespace_and_structurals(@in, ref whitespace, ref structurals);

                // fixup structurals to reflect quotes and add pseudo-strucural characters
                structurals = finalize_structurals(structurals, whitespace, quote_mask,
                    quote_bits, ref prev_iter_ends_pseudo_pred);
                idx += 64;
            }

            // is last string quote closed?
            if (prev_iter_inside_quote != 0)
            {
                return JsonParseError.UNCLOSED_STRING;
            }

            // finally, flatten out the remaining structurals from the last iteration
            flatten_bits(base_ptr, ref @base, (uint) idx, structurals);

            pj.n_structural_indexes = @base;
            // a valid JSON file cannot have zero structural indexes - we should have
            // found something
            if (pj.n_structural_indexes == 0u)
            {
                return JsonParseError.EMPTY;
            }

            if (base_ptr[pj.n_structural_indexes - 1] > len)
            {
                return JsonParseError.UNEXPECTED_ERROR;
            }

            if (len != base_ptr[pj.n_structural_indexes - 1])
            {
                // the string might not be NULL terminated, but we add a virtual NULL ending
                // character.
                base_ptr[pj.n_structural_indexes++] = (uint) len;
            }

            // make it safe to dereference one beyond this array
            base_ptr[pj.n_structural_indexes] = 0;
            if (error_mask != 0)
            {
                return JsonParseError.UNESCAPED_CHARS;
            }
#if SIMDJSON_UTF8VALIDATE
            return check_utf8_errors(state);
#else
            return JsonParseError.SUCCESS;
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static JsonParseError find_structural_bits(char1* buf, size_t len, ParsedJson pj) 
            => find_structural_bits((uint8_t*)(buf), len, pj);
    }
}

