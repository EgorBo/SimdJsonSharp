﻿// This file is a manual port of C code https://github.com/lemire/simdjson to C#
// (c) Daniel Lemire

#define SWAR_NUMBER_PARSING
using System;
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
using bytechar = System.SByte;
using unsigned_bytechar = System.Byte;
using uintptr_t = System.UIntPtr;
using static SimdJsonSharp.Utils;
#endregion


namespace SimdJsonSharp
{
    internal static unsafe class numberparsing
    {
        private static readonly double[] power_of_ten =
        {
            1e-308, 1e-307, 1e-306, 1e-305, 1e-304, 1e-303, 1e-302, 1e-301, 1e-300,
            1e-299, 1e-298, 1e-297, 1e-296, 1e-295, 1e-294, 1e-293, 1e-292, 1e-291,
            1e-290, 1e-289, 1e-288, 1e-287, 1e-286, 1e-285, 1e-284, 1e-283, 1e-282,
            1e-281, 1e-280, 1e-279, 1e-278, 1e-277, 1e-276, 1e-275, 1e-274, 1e-273,
            1e-272, 1e-271, 1e-270, 1e-269, 1e-268, 1e-267, 1e-266, 1e-265, 1e-264,
            1e-263, 1e-262, 1e-261, 1e-260, 1e-259, 1e-258, 1e-257, 1e-256, 1e-255,
            1e-254, 1e-253, 1e-252, 1e-251, 1e-250, 1e-249, 1e-248, 1e-247, 1e-246,
            1e-245, 1e-244, 1e-243, 1e-242, 1e-241, 1e-240, 1e-239, 1e-238, 1e-237,
            1e-236, 1e-235, 1e-234, 1e-233, 1e-232, 1e-231, 1e-230, 1e-229, 1e-228,
            1e-227, 1e-226, 1e-225, 1e-224, 1e-223, 1e-222, 1e-221, 1e-220, 1e-219,
            1e-218, 1e-217, 1e-216, 1e-215, 1e-214, 1e-213, 1e-212, 1e-211, 1e-210,
            1e-209, 1e-208, 1e-207, 1e-206, 1e-205, 1e-204, 1e-203, 1e-202, 1e-201,
            1e-200, 1e-199, 1e-198, 1e-197, 1e-196, 1e-195, 1e-194, 1e-193, 1e-192,
            1e-191, 1e-190, 1e-189, 1e-188, 1e-187, 1e-186, 1e-185, 1e-184, 1e-183,
            1e-182, 1e-181, 1e-180, 1e-179, 1e-178, 1e-177, 1e-176, 1e-175, 1e-174,
            1e-173, 1e-172, 1e-171, 1e-170, 1e-169, 1e-168, 1e-167, 1e-166, 1e-165,
            1e-164, 1e-163, 1e-162, 1e-161, 1e-160, 1e-159, 1e-158, 1e-157, 1e-156,
            1e-155, 1e-154, 1e-153, 1e-152, 1e-151, 1e-150, 1e-149, 1e-148, 1e-147,
            1e-146, 1e-145, 1e-144, 1e-143, 1e-142, 1e-141, 1e-140, 1e-139, 1e-138,
            1e-137, 1e-136, 1e-135, 1e-134, 1e-133, 1e-132, 1e-131, 1e-130, 1e-129,
            1e-128, 1e-127, 1e-126, 1e-125, 1e-124, 1e-123, 1e-122, 1e-121, 1e-120,
            1e-119, 1e-118, 1e-117, 1e-116, 1e-115, 1e-114, 1e-113, 1e-112, 1e-111,
            1e-110, 1e-109, 1e-108, 1e-107, 1e-106, 1e-105, 1e-104, 1e-103, 1e-102,
            1e-101, 1e-100, 1e-99, 1e-98, 1e-97, 1e-96, 1e-95, 1e-94, 1e-93,
            1e-92, 1e-91, 1e-90, 1e-89, 1e-88, 1e-87, 1e-86, 1e-85, 1e-84,
            1e-83, 1e-82, 1e-81, 1e-80, 1e-79, 1e-78, 1e-77, 1e-76, 1e-75,
            1e-74, 1e-73, 1e-72, 1e-71, 1e-70, 1e-69, 1e-68, 1e-67, 1e-66,
            1e-65, 1e-64, 1e-63, 1e-62, 1e-61, 1e-60, 1e-59, 1e-58, 1e-57,
            1e-56, 1e-55, 1e-54, 1e-53, 1e-52, 1e-51, 1e-50, 1e-49, 1e-48,
            1e-47, 1e-46, 1e-45, 1e-44, 1e-43, 1e-42, 1e-41, 1e-40, 1e-39,
            1e-38, 1e-37, 1e-36, 1e-35, 1e-34, 1e-33, 1e-32, 1e-31, 1e-30,
            1e-29, 1e-28, 1e-27, 1e-26, 1e-25, 1e-24, 1e-23, 1e-22, 1e-21,
            1e-20, 1e-19, 1e-18, 1e-17, 1e-16, 1e-15, 1e-14, 1e-13, 1e-12,
            1e-11, 1e-10, 1e-9, 1e-8, 1e-7, 1e-6, 1e-5, 1e-4, 1e-3,
            1e-2, 1e-1, 1e0, 1e1, 1e2, 1e3, 1e4, 1e5, 1e6,
            1e7, 1e8, 1e9, 1e10, 1e11, 1e12, 1e13, 1e14, 1e15,
            1e16, 1e17, 1e18, 1e19, 1e20, 1e21, 1e22, 1e23, 1e24,
            1e25, 1e26, 1e27, 1e28, 1e29, 1e30, 1e31, 1e32, 1e33,
            1e34, 1e35, 1e36, 1e37, 1e38, 1e39, 1e40, 1e41, 1e42,
            1e43, 1e44, 1e45, 1e46, 1e47, 1e48, 1e49, 1e50, 1e51,
            1e52, 1e53, 1e54, 1e55, 1e56, 1e57, 1e58, 1e59, 1e60,
            1e61, 1e62, 1e63, 1e64, 1e65, 1e66, 1e67, 1e68, 1e69,
            1e70, 1e71, 1e72, 1e73, 1e74, 1e75, 1e76, 1e77, 1e78,
            1e79, 1e80, 1e81, 1e82, 1e83, 1e84, 1e85, 1e86, 1e87,
            1e88, 1e89, 1e90, 1e91, 1e92, 1e93, 1e94, 1e95, 1e96,
            1e97, 1e98, 1e99, 1e100, 1e101, 1e102, 1e103, 1e104, 1e105,
            1e106, 1e107, 1e108, 1e109, 1e110, 1e111, 1e112, 1e113, 1e114,
            1e115, 1e116, 1e117, 1e118, 1e119, 1e120, 1e121, 1e122, 1e123,
            1e124, 1e125, 1e126, 1e127, 1e128, 1e129, 1e130, 1e131, 1e132,
            1e133, 1e134, 1e135, 1e136, 1e137, 1e138, 1e139, 1e140, 1e141,
            1e142, 1e143, 1e144, 1e145, 1e146, 1e147, 1e148, 1e149, 1e150,
            1e151, 1e152, 1e153, 1e154, 1e155, 1e156, 1e157, 1e158, 1e159,
            1e160, 1e161, 1e162, 1e163, 1e164, 1e165, 1e166, 1e167, 1e168,
            1e169, 1e170, 1e171, 1e172, 1e173, 1e174, 1e175, 1e176, 1e177,
            1e178, 1e179, 1e180, 1e181, 1e182, 1e183, 1e184, 1e185, 1e186,
            1e187, 1e188, 1e189, 1e190, 1e191, 1e192, 1e193, 1e194, 1e195,
            1e196, 1e197, 1e198, 1e199, 1e200, 1e201, 1e202, 1e203, 1e204,
            1e205, 1e206, 1e207, 1e208, 1e209, 1e210, 1e211, 1e212, 1e213,
            1e214, 1e215, 1e216, 1e217, 1e218, 1e219, 1e220, 1e221, 1e222,
            1e223, 1e224, 1e225, 1e226, 1e227, 1e228, 1e229, 1e230, 1e231,
            1e232, 1e233, 1e234, 1e235, 1e236, 1e237, 1e238, 1e239, 1e240,
            1e241, 1e242, 1e243, 1e244, 1e245, 1e246, 1e247, 1e248, 1e249,
            1e250, 1e251, 1e252, 1e253, 1e254, 1e255, 1e256, 1e257, 1e258,
            1e259, 1e260, 1e261, 1e262, 1e263, 1e264, 1e265, 1e266, 1e267,
            1e268, 1e269, 1e270, 1e271, 1e272, 1e273, 1e274, 1e275, 1e276,
            1e277, 1e278, 1e279, 1e280, 1e281, 1e282, 1e283, 1e284, 1e285,
            1e286, 1e287, 1e288, 1e289, 1e290, 1e291, 1e292, 1e293, 1e294,
            1e295, 1e296, 1e297, 1e298, 1e299, 1e300, 1e301, 1e302, 1e303,
            1e304, 1e305, 1e306, 1e307, 1e308
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool is_integer(bytechar c)
        {
            return (uint8_t)(c - '0') <= 9;
        }

        private static ReadOnlySpan<byte> structural_or_whitespace_or_exponent_or_decimal_negated => new byte[256]
        {
            1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 1, 1, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 0, 1,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 1,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 0, 1, 1,
            1, 1, 1, 1, 1, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 0, 1, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool is_not_structural_or_whitespace_or_exponent_or_decimal(unsigned_bytechar c)
        {
            // Bypass bounds check as c can never 
            // exceed the bounds of structural_or_whitespace_or_exponent_or_decimal_negated
            return Unsafe.AddByteOffset(
                ref MemoryMarshal.GetReference(structural_or_whitespace_or_exponent_or_decimal_negated),
                (IntPtr)c) == 1;
        }

        // check quickly whether the next 8 chars are made of digits
        // at a glance, it looks better than Mula's
        // http://0x80.pl/articles/swar-digits-validate.html
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool is_made_of_eight_digits_fast(bytechar* chars)
        {
            uint64_t val;
            memcpy(&val, chars, 8);
            // a branchy method might be faster:
            // return (( val & 0xF0F0F0F0F0F0F0F0 ) == 0x3030303030303030)
            //  && (( (val + 0x0606060606060606) & 0xF0F0F0F0F0F0F0F0 ) ==
            //  0x3030303030303030);
            return (((val & 0xF0F0F0F0F0F0F0F0) |
                     (((val + 0x0606060606060606) & 0xF0F0F0F0F0F0F0F0) >> 4)) ==
                    0x3333333333333333);
        }

        // C#: static readonly is an alternative to `const _m128`
        private static readonly Vector128<sbyte> mul_1_10 = Vector128.Create((bytechar) 10, 1, 10, 1, 10, 1, 10, 1, 10, 1, 10, 1, 10, 1, 10, 1);
        private static readonly Vector128<short> mul_1_100 = Vector128.Create(100, 1, 100, 1, 100, 1, 100 /*C#reversed*/, 1);
        private static readonly Vector128<short> mul_1_10000 = Vector128.Create(10000, 1, 10000, 1, 10000, 1, 10000, 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint32_t parse_eight_digits_unrolled(bytechar* chars)
        {
            // this actually computes *16* values so we are being wasteful.
            Vector128<sbyte> ascii0 = Vector128.Create((bytechar) '0');
            Vector128<sbyte> input = Sse2.Subtract(Sse2.LoadVector128(chars), ascii0);
            Vector128<short> t1 = Ssse3.MultiplyAddAdjacent(input.AsByte(), mul_1_10);
            Vector128<int> t2 = Sse2.MultiplyAddAdjacent(t1, mul_1_100);
            Vector128<ushort> t3 = Sse41.PackUnsignedSaturate(t2, t2);
            Vector128<int> t4 = Sse2.MultiplyAddAdjacent(t3.AsInt16(), mul_1_10000);
            return Sse2.ConvertToUInt32(t4.AsUInt32()); // only captures the sum of the first 8 digits, drop the rest
        }

        // called by parse_number when we know that the output is a float,
        // but where there might be some integer overflow. The trick here is to
        // parse using floats from the start.
        // Do not call this function directly as it skips some of the checks from
        // parse_number
        //
        // This function will almost never be called!!!
        //
        // Note: a redesign could avoid this function entirely.
        //
        private static bool parse_float(uint8_t* buf, ParsedJson pj, uint32_t offset, bool found_minus)
        {
            bytechar* p = (bytechar*) (buf + offset);
            bool negative = false;
            if (found_minus)
            {
                ++p;
                negative = true;
            }

            double i;
            if (*p == '0')
            {
                // 0 cannot be followed by an integer
                ++p;
                i = 0;
            }
            else
            {
                unsigned_bytechar digit = (unsigned_bytechar) (*p - (bytechar) '0');
                i = digit;
                p++;
                while (is_integer(*p))
                {
                    digit = (unsigned_bytechar) (*p - '0');
                    i = 10 * i + digit;
                    ++p;
                }
            }

            if ('.' == *p)
            {
                ++p;
                double fractionalweight = 1;
                if (is_integer(*p))
                {
                    unsigned_bytechar digit = (unsigned_bytechar) (*p - '0');
                    ++p;
                    fractionalweight *= 0.1;
                    i = i + digit * fractionalweight;
                }
                else
                {
#if JSON_TEST_NUMBERS // for unit testing
                    foundInvalidNumber(buf + offset);
#endif
                    return false;
                }

                while (is_integer(*p))
                {
                    unsigned_bytechar digit = (unsigned_bytechar) (*p - '0');
                    ++p;
                    fractionalweight *= 0.1;
                    i = i + digit * fractionalweight;
                }
            }

            if (('e' == *p) || ('E' == *p))
            {
                ++p;
                bool negexp = false;
                if ('-' == *p)
                {
                    negexp = true;
                    ++p;
                }
                else if ('+' == *p)
                {
                    ++p;
                }

                if (!is_integer(*p))
                {
#if JSON_TEST_NUMBERS // for unit testing
                    foundInvalidNumber(buf + offset);
#endif
                    return false;
                }

                unsigned_bytechar digit = (unsigned_bytechar) (*p - '0');
                int64_t expnumber = digit; // exponential part
                p++;
                if (is_integer(*p))
                {
                    digit = (unsigned_bytechar) (*p - '0');
                    expnumber = 10 * expnumber + digit;
                    ++p;
                }

                if (is_integer(*p))
                {
                    digit = (unsigned_bytechar) (*p - '0');
                    expnumber = 10 * expnumber + digit;
                    ++p;
                }

                if (is_integer(*p))
                {
                    digit = (unsigned_bytechar) (*p - '0');
                    expnumber = 10 * expnumber + digit;
                    ++p;
                }

                if (is_integer(*p))
                {
// we refuse to parse this
#if JSON_TEST_NUMBERS // for unit testing
                    foundInvalidNumber(buf + offset);
#endif
                    return false;
                }

                int exponent = (int) (negexp ? -expnumber : expnumber);
                if ((exponent > 308) || (exponent < -308))
                {
// we refuse to parse this
#if JSON_TEST_NUMBERS // for unit testing
                    foundInvalidNumber(buf + offset);
#endif
                    return false;
                }

                i *= power_of_ten[308 + exponent];
            }

            if (is_not_structural_or_whitespace((byte) *p) != 0)
            {
                return false;
            }

            double d = negative ? -i : i;
            pj.WriteTapeDouble(d);
#if JSON_TEST_NUMBERS // for unit testing
            foundFloat(d, buf + offset);
#endif
            return is_structural_or_whitespace((byte) (*p)) != 0;
        }

        // called by parse_number when we know that the output is an integer,
        // but where there might be some integer overflow.
        // we want to catch overflows!
        // Do not call this function directly as it skips some of the checks from
        // parse_number
        //
        // This function will almost never be called!!!
        //
        static bool parse_large_integer(uint8_t* buf, ParsedJson pj, uint32_t offset, bool found_minus)
        {
            bytechar* p = (bytechar*) (buf + offset);
            bool negative = false;
            if (found_minus)
            {
                ++p;
                negative = true;
            }

            uint64_t i;
            if (*p == '0')
            {
                // 0 cannot be followed by an integer
                ++p;
                i = 0;
            }
            else
            {
                unsigned_bytechar digit = (unsigned_bytechar) (*p - '0');
                i = digit;
                p++;
                // the is_made_of_eight_digits_fast routine is unlikely to help here because
                // we rarely see large integer parts like 123456789
                while (is_integer(*p))
                {
                    digit = (unsigned_bytechar) (*p - '0');
                    if (mul_overflow(i, 10, &i))
                    {
#if JSON_TEST_NUMBERS // for unit testing
                        foundInvalidNumber(buf + offset);
#endif
                        return false; // overflow
                    }

                    if (add_overflow(i, digit, &i))
                    {
#if JSON_TEST_NUMBERS // for unit testing
                        foundInvalidNumber(buf + offset);
#endif
                        return false; // overflow
                    }

                    ++p;
                }
            }

            if (negative)
            {
                if (i > 0x8000000000000000)
                {
                    // overflows!
#if JSON_TEST_NUMBERS // for unit testing
                    foundInvalidNumber(buf + offset);
#endif
                    return false; // overflow
                }
            }
            else
            {
                if (i >= 0x8000000000000000)
                {
                    // overflows!
#if JSON_TEST_NUMBERS // for unit testing
                    foundInvalidNumber(buf + offset);
#endif
                    return false; // overflow
                }
            }

            int64_t signed_answer = negative ? -(int64_t) i : (int64_t) i;
            pj.WriteTapeInt64(signed_answer);
#if JSON_TEST_NUMBERS // for unit testing
            foundInteger(signed_answer, buf + offset);
#endif
            return is_structural_or_whitespace((byte) (*p)) != 0;
        }


        public static bool parse_number(uint8_t* buf, ParsedJson pj, uint32_t offset, bool found_minus)
        {
            bytechar* p = (bytechar*) (buf + offset);
            bool negative = false;
            if (found_minus)
            {
                ++p;
                negative = true;
                if (!is_integer(*p))
                {
                    // a negative sign must be followed by an integer

                    return false;
                }
            }

            bytechar* startdigits = p;

            int64_t i;
            if (*p == '0')
            {
                // 0 cannot be followed by an integer
                ++p;
                if (is_not_structural_or_whitespace_or_exponent_or_decimal((uint8_t) (*p)))
                {
#if JSON_TEST_NUMBERS // for unit testing
                    foundInvalidNumber(buf + offset);
#endif
                    return false;
                }

                i = 0;
            }
            else
            {
                if (!(is_integer(*p)))
                {
                    // must start with an integer
#if JSON_TEST_NUMBERS // for unit testing
                    foundInvalidNumber(buf + offset);
#endif
                    return false;
                }

                unsigned_bytechar digit = (unsigned_bytechar) (*p - '0');
                i = digit;
                p++;
                // the is_made_of_eight_digits_fast routine is unlikely to help here because
                // we rarely see large integer parts like 123456789
                while (is_integer(*p))
                {
                    digit = (unsigned_bytechar) (*p - '0');
                    i = 10 * i + digit; // might overflow
                    ++p;
                }
            }

            int64_t exponent = 0;

            if ('.' == *p)
            {
                ++p;
                bytechar* firstafterperiod = p;
                if (is_integer(*p))
                {
                    unsigned_bytechar digit = (unsigned_bytechar) (*p - '0');
                    ++p;
                    i = i * 10 + digit;
                }
                else
                {
#if JSON_TEST_NUMBERS // for unit testing
                    foundInvalidNumber(buf + offset);
#endif
                    return false;
                }
#if SWAR_NUMBER_PARSING
                // this helps if we have lots of decimals!
                // this turns out to be frequent enough.
                if (is_made_of_eight_digits_fast(p))
                {
                    i = i * 100000000 + parse_eight_digits_unrolled(p);
                    p += 8;
                    // exponent -= 8;
                }
#endif
                while (is_integer(*p))
                {
                    unsigned_bytechar digit = (unsigned_bytechar) (*p - '0');
                    ++p;
                    i = i * 10 + digit; // in rare cases, this will overflow, but that's ok because we have parse_highprecision_float later.
                }

                exponent = firstafterperiod - p;
            }

            int digitcount = (int) (p - startdigits - 1);

            int64_t expnumber = 0; // exponential part
            if (('e' == *p) || ('E' == *p))
            {
                ++p;
                bool negexp = false;
                if ('-' == *p)
                {
                    negexp = true;
                    ++p;
                }
                else if ('+' == *p)
                {
                    ++p;
                }

                if (!is_integer(*p))
                {
#if JSON_TEST_NUMBERS // for unit testing
                    foundInvalidNumber(buf + offset);
#endif
                    return false;
                }

                unsigned_bytechar digit = (unsigned_bytechar) (*p - '0');
                expnumber = digit;
                p++;
                while (is_integer(*p))
                {
                    digit = (unsigned_bytechar) (*p - '0');
                    expnumber = 10 * expnumber + digit;
                    ++p;
                }

                if (is_integer(*p))
                {
                    digit = (unsigned_bytechar) (*p - '0');
                    expnumber = 10 * expnumber + digit;
                    ++p;
                }

                if (is_integer(*p))
                {
                    digit = (unsigned_bytechar) (*p - '0');
                    expnumber = 10 * expnumber + digit;
                    ++p;
                }

                if (is_integer(*p))
                {
                    // we refuse to parse this
#if JSON_TEST_NUMBERS // for unit testing
                    foundInvalidNumber(buf + offset);
#endif
                    return false;
                }

                exponent += (negexp ? -expnumber : expnumber);
            }

            i = negative ? -i : i;
            if ((exponent != 0) || (expnumber != 0))
            {
                if ((digitcount >= 19))
                {
                    // this is uncommon!!!
                    // this is almost never going to get called!!!
                    // we start anew, going slowly!!!
                    return parse_float(buf, pj, offset,
                        found_minus);
                }

                ///////////
                // We want 0.1e1 to be a float.
                //////////
                if (i == 0)
                {
                    pj.WriteTapeDouble(0.0);
#if JSON_TEST_NUMBERS // for unit testing
                    foundFloat(0.0, buf + offset);
#endif
                }
                else
                {
                    if ((exponent > 308) || (exponent < -308))
                    {
                        // we refuse to parse this
#if JSON_TEST_NUMBERS // for unit testing
                        foundInvalidNumber(buf + offset);
#endif
                        return false;
                    }

                    double d = i;
                    d *= power_of_ten[308 + exponent];
                    // d = negative ? -d : d;
                    pj.WriteTapeDouble(d);
#if JSON_TEST_NUMBERS // for unit testing
                    foundFloat(d, buf + offset);
#endif
                }
            }
            else
            {
                if ((digitcount >= 18))
                {
                    // this is uncommon!!!
                    return parse_large_integer(buf, pj, offset,
                        found_minus);
                }

                pj.WriteTapeInt64(i);
#if JSON_TEST_NUMBERS // for unit testing
                foundInteger(i, buf + offset);
#endif
            }

            return is_structural_or_whitespace((uint8_t) (*p)) != 0;
        }
    }
}
