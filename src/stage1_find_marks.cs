// This file is a manual port of C code https://github.com/lemire/simdjson to C#
// (c) Daniel Lemire

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
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
    internal static unsafe class stage1_find_marks
    {

        // a straightforward comparison of a mask against input. 5 uops; would be
        // cheaper in AVX512.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint64_t cmp_mask_against_input(
            Vector256<byte> input_lo,
            Vector256<byte> input_hi,
            Vector256<byte> mask)
        {
            var cmp_res_0 = Avx2.CompareEqual(input_lo, mask);
            uint64_t res_0 = (uint32_t) Avx2.MoveMask(cmp_res_0);
            var cmp_res_1 = Avx2.CompareEqual(input_hi, mask);
            uint64_t res_1 = (uint64_t) Avx2.MoveMask(cmp_res_1);
            return res_0 | (res_1 << 32);
        }

        //C# static readonly fields are an alternative for `const _m128`
        private static readonly Vector256<byte> s_low_nibble_mask = Vector256.Create(
            //  0                           9  a   b  c  d
            (byte)16, 0, 0, 0, 0, 0, 0, 0, 0, 8, 12, 1, 2, 9, 0, 0, 16, 0, 0, 0, 0, 0, 0,
            0, 0, 8, 12, 1, 2, 9, 0, 0);

        private static readonly Vector256<byte> s_high_nibble_mask = Vector256.Create(
            //  0     2   3     5     7
            (byte)8, 0, 18, 4, 0, 1, 0, 1, 0, 0, 0, 3, 2, 1, 0, 0, 8, 0, 18, 4, 0, 1, 0,
            1, 0, 0, 0, 3, 2, 1, 0, 0);

        internal static bool find_structural_bits(uint8_t* buf, size_t len, ParsedJson pj)
        {
            if (len > pj.bytecapacity)
            {
                Console.WriteLine("Your ParsedJson object only supports documents up to " + pj.bytecapacity +
                                  " bytes but you are trying to process " + len + " bytes\n");
                return false;
            }

            uint32_t* base_ptr = pj.structural_indexes;
            uint32_t @base = 0;
#if SIMDJSON_UTF8VALIDATE // NOT TESTED YET!
            var has_error = Vector256<byte>.Zero;
            var previous = new avx_processed_utf_bytes();
            previous.rawbytes = Vector256<byte>.Zero;
            previous.high_nibbles = Vector256<byte>.Zero;
            previous.carried_continuations = Vector256<byte>.Zero;
#endif

            const uint64_t even_bits = 0x5555555555555555UL;
            const uint64_t odd_bits = ~even_bits;

            // for now, just work in 64-byte chunks
            // we have padded the input out to 64 byte multiple with the remainder being
            // zeros

            // persistent state across loop
            uint64_t prev_iter_ends_odd_backslash = 0UL; // either 0 or 1, but a 64-bit value
            uint64_t prev_iter_inside_quote = 0UL; // either all zeros or all ones

            // effectively the very first char is considered to follow "whitespace" for the
            // purposes of psuedo-structural character detection
            uint64_t prev_iter_ends_pseudo_pred = 1UL;
            size_t lenminus64 = len < 64 ? 0 : len - 64;
            size_t idx = 0;
            uint64_t structurals = 0;

            // C#: assign static readonly fields to locals before the loop
            Vector256<byte> low_nibble_mask = s_low_nibble_mask;
            Vector256<byte> high_nibble_mask = s_high_nibble_mask;

            var structural_shufti_mask = Vector256.Create((byte)0x7);
            var whitespace_shufti_mask = Vector256.Create((byte)0x18);
            var slashVec = Vector256.Create((bytechar) '\\').AsByte();
            var ffVec = Vector128.Create((byte) 0xFF).AsUInt64();
            var doubleQuoteVec = Vector256.Create((byte)'"');
            var zeroBVec = Vector256.Create((byte) 0);
            var vec7f = Vector256.Create((byte) 0x7f);

            for (; idx < lenminus64; idx += 64)
            {
                var input_lo = Avx.LoadVector256(buf + idx + 0);
                var input_hi = Avx.LoadVector256(buf + idx + 32);
#if SIMDJSON_UTF8VALIDATE // NOT TESTED YET!
                var highbit = Vector256.Create((byte)0x80);
                if ((Avx.TestZ(Avx2.Or(input_lo, input_hi), highbit)) == true)
                {
                    // it is ascii, we just check continuation
                    has_error = Avx2.Or(
                      Avx2.CompareGreaterThan(previous.carried_continuations.AsSByte(),
                                      Vector256.Create((sbyte)9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9,
                                                       9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9,
                                                       9, 9, 9, 9, 9, 9, 9, 1)).AsByte(), has_error);

                }
                else
                {
                    // it is not ascii so we have to do heavy work
                    previous = Utf8Validation.avxcheckUTF8Bytes(input_lo, ref previous, ref has_error);
                    previous = Utf8Validation.avxcheckUTF8Bytes(input_hi, ref previous, ref has_error);
                }
#endif

                ////////////////////////////////////////////////////////////////////////////////////////////
                //     Step 1: detect odd sequences of backslashes
                ////////////////////////////////////////////////////////////////////////////////////////////
                /// 
                uint64_t bs_bits =
                    cmp_mask_against_input(input_lo, input_hi, slashVec);
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
                    prev_iter_ends_odd_backslash; // push in bit zero as a potential end
                // if we had an odd-numbered run at the
                // end of the previous iteration
                prev_iter_ends_odd_backslash = iter_ends_odd_backslash ? 0x1UL : 0x0UL;
                uint64_t even_carry_ends = even_carries & ~bs_bits;
                uint64_t odd_carry_ends = odd_carries & ~bs_bits;
                uint64_t even_start_odd_end = even_carry_ends & odd_bits;
                uint64_t odd_start_even_end = odd_carry_ends & even_bits;
                uint64_t odd_ends = even_start_odd_end | odd_start_even_end;

                ////////////////////////////////////////////////////////////////////////////////////////////
                //     Step 2: detect insides of quote pairs
                ////////////////////////////////////////////////////////////////////////////////////////////

                uint64_t quote_bits =
                    cmp_mask_against_input(input_lo, input_hi, doubleQuoteVec);
                quote_bits = quote_bits & ~odd_ends;
                uint64_t quote_mask = Sse2.X64.ConvertToUInt64(Pclmulqdq.CarrylessMultiply(
                    Vector128.Create(quote_bits, 0UL /*C# reversed*/), ffVec, 0));

                uint32_t cnt = (uint32_t) hamming(structurals);
                uint32_t next_base = @base + cnt;
                while (structurals != 0)
                {
                    base_ptr[@base + 0] = (uint32_t) idx - 64 + (uint32_t) trailingzeroes(structurals);
                    structurals = structurals & (structurals - 1);
                    base_ptr[@base + 1] = (uint32_t) idx - 64 + (uint32_t) trailingzeroes(structurals);
                    structurals = structurals & (structurals - 1);
                    base_ptr[@base + 2] = (uint32_t) idx - 64 + (uint32_t) trailingzeroes(structurals);
                    structurals = structurals & (structurals - 1);
                    base_ptr[@base + 3] = (uint32_t) idx - 64 + (uint32_t) trailingzeroes(structurals);
                    structurals = structurals & (structurals - 1);
                    base_ptr[@base + 4] = (uint32_t) idx - 64 + (uint32_t) trailingzeroes(structurals);
                    structurals = structurals & (structurals - 1);
                    base_ptr[@base + 5] = (uint32_t) idx - 64 + (uint32_t) trailingzeroes(structurals);
                    structurals = structurals & (structurals - 1);
                    base_ptr[@base + 6] = (uint32_t) idx - 64 + (uint32_t) trailingzeroes(structurals);
                    structurals = structurals & (structurals - 1);
                    base_ptr[@base + 7] = (uint32_t) idx - 64 + (uint32_t) trailingzeroes(structurals);
                    structurals = structurals & (structurals - 1);
                    @base += 8;
                }

                @base = next_base;

                quote_mask ^= prev_iter_inside_quote;
                prev_iter_inside_quote =
                    (uint64_t) ((int64_t) quote_mask >>
                                63); // right shift of a signed value expected to be well-defined and standard compliant as of C++20, John Regher from Utah U. says this is fine code



                var v_lo = Avx2.And(
                    Avx2.Shuffle(low_nibble_mask, input_lo),
                    Avx2.Shuffle(high_nibble_mask,
                        Avx2.And(Avx2.ShiftRightLogical(input_lo.AsUInt32(), 4).AsByte(),
                            vec7f)));

                var v_hi = Avx2.And(
                    Avx2.Shuffle(low_nibble_mask, input_hi),
                    Avx2.Shuffle(high_nibble_mask,
                        Avx2.And(Avx2.ShiftRightLogical(input_hi.AsUInt32(), 4).AsByte(),
                            vec7f)));
                var tmp_lo = Avx2.CompareEqual(
                    Avx2.And(v_lo, structural_shufti_mask), zeroBVec);
                var tmp_hi = Avx2.CompareEqual(
                    Avx2.And(v_hi, structural_shufti_mask), zeroBVec);

                uint64_t structural_res_0 = (uint32_t) Avx2.MoveMask(tmp_lo);
                uint64_t structural_res_1 = (uint64_t) Avx2.MoveMask(tmp_hi);
                structurals = ~(structural_res_0 | (structural_res_1 << 32));

                var tmp_ws_lo = Avx2.CompareEqual(
                    Avx2.And(v_lo, whitespace_shufti_mask), zeroBVec);
                var tmp_ws_hi = Avx2.CompareEqual(
                    Avx2.And(v_hi, whitespace_shufti_mask), zeroBVec);

                uint64_t ws_res_0 = (uint32_t) Avx2.MoveMask(tmp_ws_lo);
                uint64_t ws_res_1 = (uint64_t) Avx2.MoveMask(tmp_ws_hi);
                uint64_t whitespace = ~(ws_res_0 | (ws_res_1 << 32));


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
                // psuedo-structural character
                uint64_t pseudo_pred = structurals | whitespace;
                uint64_t shifted_pseudo_pred = (pseudo_pred << 1) | prev_iter_ends_pseudo_pred;
                prev_iter_ends_pseudo_pred = pseudo_pred >> 63;
                uint64_t pseudo_structurals =
                    shifted_pseudo_pred & (~whitespace) & (~quote_mask);
                structurals |= pseudo_structurals;

                // now, we've used our close quotes all we need to. So let's switch them off
                // they will be off in the quote mask and on in quote bits.
                structurals &= ~(quote_bits & ~quote_mask);

                //Console.WriteLine($"Iter: {idx}, satur: {structurals}");

                //*(uint64_t *)(pj.structurals + idx / 8) = structurals;
            }

            ////////////////
            /// we use a giant copy-paste which is ugly.
            /// but otherwise the string needs to be properly padded or else we
            /// risk invalidating the UTF-8 checks.
            ////////////
            if (idx < len)
            {
                uint8_t* tmpbuf = stackalloc uint8_t[64];
                memset(tmpbuf, 0x20, 64);
                memcpy(tmpbuf, buf + idx, len - idx);
                Vector256<byte> input_lo = Avx.LoadVector256(tmpbuf + 0);
                Vector256<byte> input_hi = Avx.LoadVector256(tmpbuf + 32);
#if SIMDJSON_UTF8VALIDATE // NOT TESTED YET!
                var highbit = Vector256.Create((byte)0x80);
                if ((Avx.TestZ(Avx2.Or(input_lo, input_hi), highbit)) == true)
                {
                    // it is ascii, we just check continuation
                    has_error = Avx2.Or(
                      Avx2.CompareGreaterThan(previous.carried_continuations.AsSByte(),
                                      Vector256.Create((sbyte)9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9,
                                                       9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9,
                                                       9, 9, 9, 9, 9, 9, 9, 1)).AsByte(), has_error);

                }
                else
                {
                    // it is not ascii so we have to do heavy work
                    previous = Utf8Validation.avxcheckUTF8Bytes(input_lo, ref previous, ref has_error);
                    previous = Utf8Validation.avxcheckUTF8Bytes(input_hi, ref previous, ref has_error);
                }
#endif
                ////////////////////////////////////////////////////////////////////////////////////////////
                //     Step 1: detect odd sequences of backslashes
                ////////////////////////////////////////////////////////////////////////////////////////////

                uint64_t bs_bits =
                    cmp_mask_against_input(input_lo, input_hi, slashVec);
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
                //bool iter_ends_odd_backslash =
                add_overflow(bs_bits, odd_starts, &odd_carries);

                odd_carries |=
                    prev_iter_ends_odd_backslash; // push in bit zero as a potential end
                // if we had an odd-numbered run at the
                // end of the previous iteration
                //prev_iter_ends_odd_backslash = iter_ends_odd_backslash ? 0x1ULL : 0x0ULL;
                uint64_t even_carry_ends = even_carries & ~bs_bits;
                uint64_t odd_carry_ends = odd_carries & ~bs_bits;
                uint64_t even_start_odd_end = even_carry_ends & odd_bits;
                uint64_t odd_start_even_end = odd_carry_ends & even_bits;
                uint64_t odd_ends = even_start_odd_end | odd_start_even_end;

                ////////////////////////////////////////////////////////////////////////////////////////////
                //     Step 2: detect insides of quote pairs
                ////////////////////////////////////////////////////////////////////////////////////////////

                uint64_t quote_bits =
                    cmp_mask_against_input(input_lo, input_hi, doubleQuoteVec);
                quote_bits = quote_bits & ~odd_ends;
                uint64_t quote_mask = (uint64_t)Sse2.X64.ConvertToInt64(Pclmulqdq.CarrylessMultiply(
                    Vector128.Create(quote_bits, 0UL /*C# reversed*/), ffVec, 0).AsInt64());
                quote_mask ^= prev_iter_inside_quote;

                //BUG? https://github.com/dotnet/coreclr/issues/22813
                //quote_mask = 60;
                //prev_iter_inside_quote = (uint64_t)((int64_t)quote_mask >> 63); // right shift of a signed value expected to be well-defined and standard compliant as of C++20

                uint32_t cnt = (uint32_t)hamming(structurals);
                uint32_t next_base = @base + cnt;
                while (structurals != 0)
                {
                    base_ptr[@base + 0] = (uint32_t)idx - 64 + (uint32_t)trailingzeroes(structurals);
                    structurals = structurals & (structurals - 1);
                    base_ptr[@base + 1] = (uint32_t)idx - 64 + (uint32_t)trailingzeroes(structurals);
                    structurals = structurals & (structurals - 1);
                    base_ptr[@base + 2] = (uint32_t)idx - 64 + (uint32_t)trailingzeroes(structurals);
                    structurals = structurals & (structurals - 1);
                    base_ptr[@base + 3] = (uint32_t)idx - 64 + (uint32_t)trailingzeroes(structurals);
                    structurals = structurals & (structurals - 1);
                    base_ptr[@base + 4] = (uint32_t)idx - 64 + (uint32_t)trailingzeroes(structurals);
                    structurals = structurals & (structurals - 1);
                    base_ptr[@base + 5] = (uint32_t)idx - 64 + (uint32_t)trailingzeroes(structurals);
                    structurals = structurals & (structurals - 1);
                    base_ptr[@base + 6] = (uint32_t)idx - 64 + (uint32_t)trailingzeroes(structurals);
                    structurals = structurals & (structurals - 1);
                    base_ptr[@base + 7] = (uint32_t)idx - 64 + (uint32_t)trailingzeroes(structurals);
                    structurals = structurals & (structurals - 1);
                    @base += 8;
                }
                @base = next_base;
                // How do we build up a user traversable data structure
                // first, do a 'shufti' to detect structural JSON characters
                // they are { 0x7b } 0x7d : 0x3a [ 0x5b ] 0x5d , 0x2c
                // these go into the first 3 buckets of the comparison (1/2/4)

                // we are also interested in the four whitespace characters
                // space 0x20, linefeed 0x0a, horizontal tab 0x09 and carriage return 0x0d
                // these go into the next 2 buckets of the comparison (8/16)

                var v_lo = Avx2.And(
                    Avx2.Shuffle(low_nibble_mask, input_lo),
                    Avx2.Shuffle(high_nibble_mask,
                        Avx2.And(Avx2.ShiftRightLogical(input_lo.AsUInt32(), 4).AsByte(),
                            vec7f)));

                var v_hi = Avx2.And(
                    Avx2.Shuffle(low_nibble_mask, input_hi),
                    Avx2.Shuffle(high_nibble_mask,
                        Avx2.And(Avx2.ShiftRightLogical(input_hi.AsUInt32(), 4).AsByte(),
                            vec7f)));
                var tmp_lo = Avx2.CompareEqual(
                    Avx2.And(v_lo, structural_shufti_mask), zeroBVec);
                var tmp_hi = Avx2.CompareEqual(
                    Avx2.And(v_hi, structural_shufti_mask), zeroBVec);

                uint64_t structural_res_0 = (uint32_t)Avx2.MoveMask(tmp_lo);
                uint64_t structural_res_1 = (uint64_t)Avx2.MoveMask(tmp_hi);
                structurals = ~(structural_res_0 | (structural_res_1 << 32));

                // this additional mask and transfer is non-trivially expensive,
                // unfortunately
                var tmp_ws_lo = Avx2.CompareEqual(
                    Avx2.And(v_lo, whitespace_shufti_mask), zeroBVec);
                var tmp_ws_hi = Avx2.CompareEqual(
                    Avx2.And(v_hi, whitespace_shufti_mask), zeroBVec);

                uint64_t ws_res_0 = (uint32_t)Avx2.MoveMask(tmp_ws_lo);
                uint64_t ws_res_1 = (uint64_t)Avx2.MoveMask(tmp_ws_hi);
                uint64_t whitespace = ~(ws_res_0 | (ws_res_1 << 32));


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
                // psuedo-structural character
                uint64_t pseudo_pred = structurals | whitespace;
                uint64_t shifted_pseudo_pred = (pseudo_pred << 1) | prev_iter_ends_pseudo_pred;
                prev_iter_ends_pseudo_pred = pseudo_pred >> 63;
                uint64_t pseudo_structurals =
                    shifted_pseudo_pred & (~whitespace) & (~quote_mask);
                structurals |= pseudo_structurals;

                // now, we've used our close quotes all we need to. So let's switch them off
                // they will be off in the quote mask and on in quote bits.
                structurals &= ~(quote_bits & ~quote_mask);
                //*(uint64_t *)(pj.structurals + idx / 8) = structurals;
                idx += 64;
            }
            uint32_t cnt2 = (uint32_t)hamming(structurals);
            uint32_t next_base2 = @base + cnt2;
            while (structurals != 0)
            {
                base_ptr[@base + 0] = (uint32_t)idx - 64 + (uint32_t)trailingzeroes(structurals);
                structurals = structurals & (structurals - 1);
                base_ptr[@base + 1] = (uint32_t)idx - 64 + (uint32_t)trailingzeroes(structurals);
                structurals = structurals & (structurals - 1);
                base_ptr[@base + 2] = (uint32_t)idx - 64 + (uint32_t)trailingzeroes(structurals);
                structurals = structurals & (structurals - 1);
                base_ptr[@base + 3] = (uint32_t)idx - 64 + (uint32_t)trailingzeroes(structurals);
                structurals = structurals & (structurals - 1);
                base_ptr[@base + 4] = (uint32_t)idx - 64 + (uint32_t)trailingzeroes(structurals);
                structurals = structurals & (structurals - 1);
                base_ptr[@base + 5] = (uint32_t)idx - 64 + (uint32_t)trailingzeroes(structurals);
                structurals = structurals & (structurals - 1);
                base_ptr[@base + 6] = (uint32_t)idx - 64 + (uint32_t)trailingzeroes(structurals);
                structurals = structurals & (structurals - 1);
                base_ptr[@base + 7] = (uint32_t)idx - 64 + (uint32_t)trailingzeroes(structurals);
                structurals = structurals & (structurals - 1);
                @base += 8;
            }
            @base = next_base2;

            pj.n_structural_indexes = @base;
            if (base_ptr[pj.n_structural_indexes - 1] > len)
            {
                throw new InvalidOperationException("Internal bug");
            }
            if (len != base_ptr[pj.n_structural_indexes - 1])
            {
                // the string might not be NULL terminated, but we add a virtual NULL ending character. 
                base_ptr[pj.n_structural_indexes++] = (uint32_t)len;
            }
            base_ptr[pj.n_structural_indexes] = 0; // make it safe to dereference one beyond this array

#if SIMDJSON_UTF8VALIDATE // NOT TESTED YET!
            return Avx.TestZ(has_error, has_error);
#else
            return true;
#endif
        }
    }
}

