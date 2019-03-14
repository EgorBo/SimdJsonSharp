// This file is a manual port of C code https://github.com/lemire/simdjson to C#
// (c) Daniel Lemire and Geoff Langdale

#if SIMDJSON_UTF8VALIDATE // NOT TESTED YET!
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
    internal unsafe partial class Utf8Validation
    {
        internal static avx_processed_utf_bytes avxcheckUTF8Bytes(Vector256<byte> current_bytes, ref avx_processed_utf_bytes previous, ref Vector256<byte> has_error)
        {
            avx_processed_utf_bytes pb = new avx_processed_utf_bytes();
            avx_count_nibbles(current_bytes, ref pb);
            avxcheckSmallerThan0xF4(current_bytes, ref has_error);
            Vector256<byte> initial_lengths = avxcontinuationLengths(pb.high_nibbles);
            pb.carried_continuations =
            avxcarryContinuations(initial_lengths, previous.carried_continuations);
            avxcheckContinuations(initial_lengths, pb.carried_continuations, ref has_error);
            Vector256<byte> off1_current_bytes = push_last_byte_of_a_to_b(previous.rawbytes, pb.rawbytes);
            avxcheckFirstContinuationMax(current_bytes, off1_current_bytes, ref has_error);
            avxcheckOverlong(current_bytes, off1_current_bytes, pb.high_nibbles, previous.high_nibbles, ref has_error);
            return pb;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector256<byte> push_last_byte_of_a_to_b(Vector256<byte> a, Vector256<byte> b)
        {
            return Avx2.AlignRight(b, Avx2.Permute2x128(a, b, 0x21), 15);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector256<byte> push_last_2bytes_of_a_to_b(Vector256<byte> a, Vector256<byte> b)
        {
            return Avx2.AlignRight(b, Avx2.Permute2x128(a, b, 0x21), 14);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void avx_count_nibbles(Vector256<byte> bytes, ref avx_processed_utf_bytes answer)
        {
            answer.rawbytes = bytes;
            answer.high_nibbles = Avx2.And(Avx2.ShiftRightLogical(bytes.AsUInt16(), 4).AsByte(), Vector256.Create((byte)0x0F));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void avxcheckSmallerThan0xF4(Vector256<byte> current_bytes, ref Vector256<byte> has_error)
        {
            // unsigned, saturates to 0 below max
            has_error = Avx2.Or(
            has_error, Avx2.SubtractSaturate(current_bytes, Vector256.Create((byte)0xF4)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector256<byte> avxcontinuationLengths(Vector256<byte> high_nibbles)
        {
            return Avx2.Shuffle(
                Vector256.Create((byte)1, 1, 1, 1, 1, 1, 1, 1, // 0xxx (ASCII)
                                 0, 0, 0, 0,             // 10xx (continuation)
                                 2, 2,                   // 110x
                                 3,                      // 1110
                                 4, // 1111, next should be 0 (not checked here)
                                 1, 1, 1, 1, 1, 1, 1, 1, // 0xxx (ASCII)
                                 0, 0, 0, 0,             // 10xx (continuation)
                                 2, 2,                   // 110x
                                 3,                      // 1110
                                 4 // 1111, next should be 0 (not checked here)
                                 ),
                high_nibbles);
        }


        static Vector256<byte> avxcarryContinuations(Vector256<byte> initial_lengths,
                                                    Vector256<byte> previous_carries)
        {
            Vector256<byte> right1 = Avx2.SubtractSaturate(
                push_last_byte_of_a_to_b(previous_carries, initial_lengths),
                Vector256.Create((byte)1));
            Vector256<byte> sum = Avx2.Add(initial_lengths, right1);

            Vector256<byte> right2 = Avx2.SubtractSaturate(
                push_last_2bytes_of_a_to_b(previous_carries, sum), Vector256.Create((byte)2));
            return Avx2.Add(sum, right2);
        }

        static void avxcheckContinuations(Vector256<byte> initial_lengths,
                                                 Vector256<byte> carries, ref Vector256<byte> has_error)
        {

            // overlap || underlap
            // carry > length && length > 0 || !(carry > length) && !(length > 0)
            // (carries > length) == (lengths > 0)
            Vector256<byte> overunder = Avx2.CompareEqual(
                Avx2.CompareGreaterThan(carries.AsSByte(), initial_lengths.AsSByte()).AsByte(),
                Avx2.CompareGreaterThan(initial_lengths.AsSByte(), Vector256<sbyte>.Zero).AsByte());

            has_error = Avx2.Or(has_error, overunder);
        }

        // when 0xED is found, next byte must be no larger than 0x9F
        // when 0xF4 is found, next byte must be no larger than 0x8F
        // next byte must be continuation, ie sign bit is set, so signed < is ok
        static void avxcheckFirstContinuationMax(Vector256<byte> current_bytes,
                                                        Vector256<byte> off1_current_bytes,
                                                        ref Vector256<byte> has_error)
        {
            Vector256<byte> maskED =
                Avx2.CompareEqual(off1_current_bytes, Vector256.Create((byte)0xED));
            Vector256<byte> maskF4 =
                Avx2.CompareEqual(off1_current_bytes, Vector256.Create((byte)0xF4));

            Vector256<byte> badfollowED = Avx2.And(
                Avx2.CompareGreaterThan(current_bytes.AsSByte(), Vector256.Create((byte)0x9F).AsSByte()).AsByte(), maskED);
            Vector256<byte> badfollowF4 = Avx2.And(
                Avx2.CompareGreaterThan(current_bytes.AsSByte(), Vector256.Create((byte)0x8F).AsSByte()).AsByte(), maskF4);

            has_error =
                Avx2.Or(has_error, Avx2.Or(badfollowED, badfollowF4));
        }

        // map off1_hibits => error condition
        // hibits     off1    cur
        // C       => < C2 && true
        // E       => < E1 && < A0
        // F       => < F1 && < 90
        // else      false && false
        static void avxcheckOverlong(Vector256<byte> current_bytes,
                                            Vector256<byte> off1_current_bytes, Vector256<byte> hibits,
                                            Vector256<byte> previous_hibits,
                                            ref Vector256<byte> has_error)
        {
            Vector256<byte> off1_hibits = push_last_byte_of_a_to_b(previous_hibits, hibits);
            Vector256<byte> initial_mins = Avx2.Shuffle(
                //Vector256.Create(-128, -128, -128, -128, -128, -128, -128, -128, -128,
                //                 -128, -128, -128, // 10xx => false
                //                 0xC2, -128,       // 110x
                //                 0xE1,             // 1110
                //                 0xF1, -128, -128, -128, -128, -128, -128, -128, -128,
                //                 -128, -128, -128, -128, // 10xx => false
                //                 0xC2, -128,             // 110x
                //                 0xE1,                   // 1110
                //                 0xF1),
                Vector256.Create(9259542123273814144, 17429353605768446080, 9259542123273814144, 17429353605768446080).AsByte(),
                off1_hibits);

            Vector256<byte> initial_under = Avx2.CompareGreaterThan(initial_mins.AsSByte(), off1_current_bytes.AsSByte()).AsByte();

            Vector256<byte> second_mins = Avx2.Shuffle(
                //Vector256.Create(-128, -128, -128, -128, -128, -128, -128, -128, -128,
                //                 -128, -128, -128, // 10xx => false
                //                 127, 127,         // 110x => true
                //                 0xA0,             // 1110
                //                 0x90, -128, -128, -128, -128, -128, -128, -128, -128,
                //                 -128, -128, -128, -128, // 10xx => false
                //                 127, 127,               // 110x => true
                //                 0xA0,                   // 1110
                //                 0x90),
                Vector256.Create(9259542123273814144, 10421469723328807040, 9259542123273814144, 10421469723328807040).AsByte(),
                off1_hibits);
            Vector256<byte> second_under = Avx2.CompareGreaterThan(second_mins.AsSByte(), current_bytes.AsSByte()).AsByte();
            has_error = Avx2.Or(has_error, Avx2.And(initial_under, second_under));
        }
    }

    internal struct avx_processed_utf_bytes
    {
        public Vector256<byte> rawbytes;
        public Vector256<byte> high_nibbles;
        public Vector256<byte> carried_continuations;
    };
}
#endif