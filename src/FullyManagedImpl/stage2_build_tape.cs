// This file is a manual port of C code https://github.com/lemire/simdjson to C#
// (c) Daniel Lemire and Geoff Langdale

#define SIMDJSON_ALLOWANYTHINGINROOT

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

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

// this file specific
using static SimdJsonSharp.numberparsing;
using static SimdJsonSharp.stringparsing;

namespace SimdJsonSharp
{
    internal static unsafe class stage2_build_tape
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool is_valid_true_atom(uint8_t* loc) {
            uint64_t tv = 2314885531981673076; //* (uint64_t*)"true    ";
            uint64_t mask4 = 0x00000000ffffffff;
            uint32_t error = 0;
            uint64_t locval; // we want to avoid unaligned 64-bit loads (undefined in C/C++)
            // this can read up to 7 bytes beyond the buffer size, but we require
            // SIMDJSON_PADDING of padding
            memcpy(&locval, loc, sizeof(uint64_t));
            error = (uint32_t)((locval & mask4) ^ tv);
            error |= is_not_structural_or_whitespace(loc[4]);
            return error == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool is_valid_false_atom(uint8_t* loc) {
            // We have to use an integer constant because the space in the cast
            // below would lead to values illegally being qualified
            // uint64_t fv = *reinterpret_cast<const uint64_t *>("false   ");
            // using this constant (that is the same false) but nulls out the
            // unused bits solves that
            uint64_t fv = 0x00000065736c6166; // takes into account endianness
            uint64_t mask5 = 0x000000ffffffffff;
            // we can't use the 32 bit value for checking for errors otherwise
            // the last character of false (it being 5 byte long!) would be
            // ignored
            uint64_t error = 0;
            uint64_t locval; // we want to avoid unaligned 64-bit loads (undefined in C/C++)
            // this can read up to 7 bytes beyond the buffer size, but we require 
            // SIMDJSON_PADDING of padding
            memcpy(&locval, loc, sizeof(uint64_t));
            error = (locval & mask5) ^ fv;
            error |= is_not_structural_or_whitespace(loc[5]);
            return error == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool is_valid_null_atom(uint8_t* loc) {
            uint64_t nv = 2314885532098524526; //* (uint64_t*)"null    ";
            uint64_t mask4 = 0x00000000ffffffff;
            uint32_t error = 0;
            uint64_t locval; // we want to avoid unaligned 64-bit loads (undefined in C/C++)
            // this can read up to 7 bytes beyond the buffer size, but we require 
            // SIMDJSON_PADDING of padding
            memcpy(&locval, loc, sizeof(uint64_t));
            error = (uint32_t)((locval & mask4) ^ nv);
            error |= is_not_structural_or_whitespace(loc[4]);
            return error == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static JsonParseError unified_machine(bytechar *buf, size_t len, ParsedJson pj)
            => unified_machine((uint8_t*)(buf), len, pj);

        internal static JsonParseError unified_machine(uint8_t* buf, size_t len, ParsedJson pj)
        {
#if !ALLOW_SAME_PAGE_BUFFER_OVERRUN
            memset((uint8_t*) buf + len, 0, SIMDJSON_PADDING); // to please valgrind
#endif
            uint32_t i = 0; // index of the structural character (0,1,2,3...)
            uint32_t idx; // location of the structural character in the input (buf)
            uint8_t c = 0; // used to track the (structural) character we are looking at, updated
            // by UPDATE_CHAR macro
            uint32_t depth = 0; // could have an arbitrary starting depth
            pj.Init(); // sets isvalid to false
            if (pj.bytecapacity < len)
            {
                pj.ErrorCode = JsonParseError.CAPACITY;
                return pj.ErrorCode;
            }

            ////////////////////////////// START STATE /////////////////////////////
            pj.ret_address[depth] = (bytechar) 's';
            pj.containing_scope_offset[depth] = pj.CurrentLoc;
            pj.WriteTape(0, (uint8_t) 'r'); // r for root, 0 is going to get overwritten
            // the root is used, if nothing else, to capture the size of the tape
            depth++; // everything starts at depth = 1, depth = 0 is just for the root, the root may contain an object, an array or something else.
            if (depth >= pj.depthcapacity)
            {
                goto fail;
            }

            idx = pj.structural_indexes[i++];
            c = buf[idx]; //UPDATE_CHAR()
            switch (c)
            {
                case (uint8_t) '{':
                    pj.containing_scope_offset[depth] = pj.CurrentLoc;
                    pj.ret_address[depth] = (bytechar) 's';
                    depth++;
                    if (depth >= pj.depthcapacity)
                    {
                        goto fail;
                    }

                    pj.WriteTape(0, c); // strangely, moving this to object_begin slows things down
                    goto object_begin;
                case (uint8_t) '[':
                    pj.containing_scope_offset[depth] = pj.CurrentLoc;
                    pj.ret_address[depth] = (bytechar) 's';
                    depth++;
                    if (depth >= pj.depthcapacity)
                    {
                        goto fail;
                    }

                    pj.WriteTape(0, c);
                    goto array_begin;
                // A JSON text is a serialized value.  Note that certain previous
                // specifications of JSON constrained a JSON text to be an object or an
                // array.  Implementations that generate only objects or arrays where a
                // JSON text is called for will be interoperable in the sense that all
                // implementations will accept these as conforming JSON texts.
                // https://tools.ietf.org/html/rfc8259
#if SIMDJSON_ALLOWANYTHINGINROOT
                case (uint8_t) '"':
                {
                    if (!parse_string(buf, len, pj, depth, idx))
                    {
                        goto fail;
                    }

                    break;
                }
                case (uint8_t) 't':
                {
                    // we need to make a copy to make sure that the string is space terminated.
                    // this only applies to the JSON document made solely of the true value.
                    // this will almost never be called in practice
                    bytechar* copy = (bytechar*) (allocate<bytechar>(len + SIMDJSON_PADDING));
                    if (copy == null)
                    {
                        goto fail;
                    }

                    memcpy(copy, buf, len);
                    copy[len] = (bytechar) ' ';
                    if (!is_valid_true_atom((uint8_t*) (copy) + idx))
                    {
                        free(copy);
                        goto fail;
                    }

                    free(copy);
                    pj.WriteTape(0, c);
                    break;
                }
                case (uint8_t) 'f':
                {
                    // we need to make a copy to make sure that the string is space terminated.
                    // this only applies to the JSON document made solely of the false value.
                    // this will almost never be called in practice
                    bytechar* copy = (bytechar*) (allocate<bytechar>(len + SIMDJSON_PADDING));
                    if (copy == null)
                    {
                        goto fail;
                    }

                    memcpy(copy, buf, len);
                    copy[len] = (bytechar) ' ';
                    if (!is_valid_false_atom((uint8_t*) (copy) + idx))
                    {
                        free(copy);
                        goto fail;
                    }

                    free(copy);
                    pj.WriteTape(0, c);
                    break;
                }
                case (uint8_t) 'n':
                {
                    // we need to make a copy to make sure that the string is space terminated.
                    // this only applies to the JSON document made solely of the null value.
                    // this will almost never be called in practice
                    bytechar* copy = (bytechar*) (allocate<bytechar>(len + SIMDJSON_PADDING));
                    if (copy == null)
                    {
                        goto fail;
                    }

                    memcpy(copy, buf, len);
                    copy[len] = (bytechar) ' ';
                    if (!is_valid_null_atom((uint8_t*) (copy) + idx))
                    {
                        free(copy);
                        goto fail;
                    }

                    free(copy);
                    pj.WriteTape(0, c);
                    break;
                }
                case (uint8_t) '0':
                case (uint8_t) '1':
                case (uint8_t) '2':
                case (uint8_t) '3':
                case (uint8_t) '4':
                case (uint8_t) '5':
                case (uint8_t) '6':
                case (uint8_t) '7':
                case (uint8_t) '8':
                case (uint8_t) '9':
                {
                    // we need to make a copy to make sure that the string is space terminated.
                    // this is done only for JSON documents made of a sole number
                    // this will almost never be called in practice. We terminate with a space
                    // because we do not want to allow NULLs in the middle of a number (whereas a
                    // space in the middle of a number would be identified in stage 1).
                    bytechar* copy = (bytechar*) (allocate<bytechar>(len + SIMDJSON_PADDING));
                    if (copy == null)
                    {
                        goto fail;
                    }

                    memcpy(copy, buf, len);
                    copy[len] = (bytechar) ' ';
                    if (!parse_number((uint8_t*) (copy), pj, idx, false))
                    {
                        free(copy);
                        goto fail;
                    }

                    free(copy);
                    break;
                }
                case (uint8_t) '-':
                {
                    // we need to make a copy to make sure that the string is NULL terminated.
                    // this is done only for JSON documents made of a sole number
                    // this will almost never be called in practice
                    bytechar* copy = (bytechar*) (allocate<bytechar>(len + SIMDJSON_PADDING));
                    if (copy == null)
                    {
                        goto fail;
                    }

                    memcpy(copy, buf, len);
                    copy[len] = (bytechar) '\0';
                    if (!parse_number((uint8_t*) (copy), pj, idx, true))
                    {
                        free(copy);
                        goto fail;
                    }

                    free(copy);
                    break;
                }
#endif // ALLOWANYTHINGINROOT
                default:
                    goto fail;
            }

            start_continue:
            // the string might not be NULL terminated.
            if (i + 1 == pj.n_structural_indexes)
            {
                goto succeed;
            }
            else
            {
                goto fail;
            }
            ////////////////////////////// OBJECT STATES /////////////////////////////

            object_begin:
            idx = pj.structural_indexes[i++];
            c = buf[idx]; //UPDATE_CHAR()
            switch (c)
            {
                case (uint8_t) '"':
                {
                    if (!parse_string(buf, len, pj, depth, idx))
                    {
                        goto fail;
                    }

                    goto object_key_state;
                }
                case (uint8_t) '}':
                    goto scope_end; // could also go to object_continue
                default:
                    goto fail;
            }

            object_key_state:
            idx = pj.structural_indexes[i++];
            c = buf[idx]; //UPDATE_CHAR()
            if (c != ':')
            {
                goto fail;
            }

            idx = pj.structural_indexes[i++];
            c = buf[idx]; //UPDATE_CHAR()
            switch (c)
            {
                case (uint8_t) '"':
                {
                    if (!parse_string(buf, len, pj, depth, idx))
                    {
                        goto fail;
                    }

                    break;
                }
                case (uint8_t) 't':
                    if (!is_valid_true_atom(buf + idx))
                    {
                        goto fail;
                    }

                    pj.WriteTape(0, c);
                    break;
                case (uint8_t) 'f':
                    if (!is_valid_false_atom(buf + idx))
                    {
                        goto fail;
                    }

                    pj.WriteTape(0, c);
                    break;
                case (uint8_t) 'n':
                    if (!is_valid_null_atom(buf + idx))
                    {
                        goto fail;
                    }

                    pj.WriteTape(0, c);
                    break;
                case (uint8_t) '0':
                case (uint8_t) '1':
                case (uint8_t) '2':
                case (uint8_t) '3':
                case (uint8_t) '4':
                case (uint8_t) '5':
                case (uint8_t) '6':
                case (uint8_t) '7':
                case (uint8_t) '8':
                case (uint8_t) '9':
                {
                    if (!parse_number(buf, pj, idx, false))
                    {
                        goto fail;
                    }

                    break;
                }
                case (uint8_t) '-':
                {
                    if (!parse_number(buf, pj, idx, true))
                    {
                        goto fail;
                    }

                    break;
                }
                case (uint8_t) '{':
                {
                    pj.containing_scope_offset[depth] = pj.CurrentLoc;
                    pj.WriteTape(0, c); // here the compilers knows what c is so this gets optimized
                    // we have not yet encountered } so we need to come back for it
                    pj.ret_address[depth] = (bytechar) 'o';
                    // we found an object inside an object, so we need to increment the depth
                    depth++;
                    if (depth >= pj.depthcapacity)
                    {
                        goto fail;
                    }

                    goto object_begin;
                }
                case (uint8_t) '[':
                {
                    pj.containing_scope_offset[depth] = pj.CurrentLoc;
                    pj.WriteTape(0, c); // here the compilers knows what c is so this gets optimized
                    // we have not yet encountered } so we need to come back for it
                    pj.ret_address[depth] = (bytechar) 'o';
                    // we found an array inside an object, so we need to increment the depth
                    depth++;
                    if (depth >= pj.depthcapacity)
                    {
                        goto fail;
                    }

                    goto array_begin;
                }
                default:
                    goto fail;
            }

            object_continue:
            idx = pj.structural_indexes[i++];
            c = buf[idx]; //UPDATE_CHAR()
            switch (c)
            {
                case (uint8_t) ',':
                    idx = pj.structural_indexes[i++];
                    c = buf[idx]; //UPDATE_CHAR()
                    if (c != '"')
                    {
                        goto fail;
                    }
                    else
                    {
                        if (!parse_string(buf, len, pj, depth, idx))
                        {
                            goto fail;
                        }

                        goto object_key_state;
                    }
                case (uint8_t) '}':
                    goto scope_end;
                default:
                    goto fail;
            }

            ////////////////////////////// COMMON STATE /////////////////////////////

            scope_end:
            // write our tape location to the header scope
            depth--;
            pj.WriteTape(pj.containing_scope_offset[depth], c);
            pj.AnnotatePreviousLoc(pj.containing_scope_offset[depth],
                pj.CurrentLoc);
            if (pj.ret_address[depth] == 'a')
            {
                goto array_continue;
            }
            else if (pj.ret_address[depth] == 'o')
            {
                goto object_continue;
            }
            else goto start_continue;

            ////////////////////////////// ARRAY STATES /////////////////////////////
            array_begin:
            idx = pj.structural_indexes[i++];
            c = buf[idx]; //UPDATE_CHAR()
            if (c == (uint8_t) ']')
            {
                goto scope_end; // could also go to array_continue
            }

            main_array_switch:
            // we call update char on all paths in, so we can peek at c on the
            // on paths that can accept a close square brace (post-, and at start)
            switch (c)
            {
                case (uint8_t) '"':
                {
                    if (!parse_string(buf, len, pj, depth, idx))
                    {
                        goto fail;
                    }

                    break;
                }
                case (uint8_t) 't':
                    if (!is_valid_true_atom(buf + idx))
                    {
                        goto fail;
                    }

                    pj.WriteTape(0, c);
                    break;
                case (uint8_t) 'f':
                    if (!is_valid_false_atom(buf + idx))
                    {
                        goto fail;
                    }

                    pj.WriteTape(0, c);
                    break;
                case (uint8_t) 'n':
                    if (!is_valid_null_atom(buf + idx))
                    {
                        goto fail;
                    }

                    pj.WriteTape(0, c);
                    break; // goto array_continue;

                case (uint8_t) '0':
                case (uint8_t) '1':
                case (uint8_t) '2':
                case (uint8_t) '3':
                case (uint8_t) '4':
                case (uint8_t) '5':
                case (uint8_t) '6':
                case (uint8_t) '7':
                case (uint8_t) '8':
                case (uint8_t) '9':
                {
                    if (!parse_number(buf, pj, idx, false))
                    {
                        goto fail;
                    }

                    break; // goto array_continue;
                }
                case (uint8_t) '-':
                {
                    if (!parse_number(buf, pj, idx, true))
                    {
                        goto fail;
                    }

                    break; // goto array_continue;
                }
                case (uint8_t) '{':
                {
                    // we have not yet encountered ] so we need to come back for it
                    pj.containing_scope_offset[depth] = pj.CurrentLoc;
                    pj.WriteTape(0, c); //  here the compilers knows what c is so this gets optimized
                    pj.ret_address[depth] = (bytechar) 'a';
                    // we found an object inside an array, so we need to increment the depth
                    depth++;
                    if (depth >= pj.depthcapacity)
                    {
                        goto fail;
                    }

                    goto object_begin;
                }
                case (uint8_t) '[':
                {
                    // we have not yet encountered ] so we need to come back for it
                    pj.containing_scope_offset[depth] = pj.CurrentLoc;
                    pj.WriteTape(0, c); // here the compilers knows what c is so this gets optimized
                    pj.ret_address[depth] = (bytechar) 'a';
                    // we found an array inside an array, so we need to increment the depth
                    depth++;
                    if (depth >= pj.depthcapacity)
                    {
                        goto fail;
                    }

                    goto array_begin;
                }
                default:
                    goto fail;
            }

            array_continue:
            idx = pj.structural_indexes[i++];
            c = buf[idx]; //UPDATE_CHAR()
            switch (c)
            {
                case (uint8_t) ',':
                    idx = pj.structural_indexes[i++];
                    c = buf[idx]; //UPDATE_CHAR()
                    goto main_array_switch;
                case (uint8_t) ']':
                    goto scope_end;
                default:
                    goto fail;
            }

            ////////////////////////////// FINAL STATES /////////////////////////////

            succeed:
            depth--;
            if (depth != 0)
            {
                throw new InvalidOperationException("internal bug");
                //abort();
            }

            if (pj.containing_scope_offset[depth] != 0)
            {
                throw new InvalidOperationException("internal bug");
                //abort();
            }

            pj.AnnotatePreviousLoc(pj.containing_scope_offset[depth], pj.CurrentLoc);
            pj.WriteTape(pj.containing_scope_offset[depth], (uint8_t) 'r'); // r is root

            pj.isvalid = true;
            pj.ErrorCode = JsonParseError.SUCCESS;
            return pj.ErrorCode;
            fail:
            // we do not need the next line because this is done by pj.init(), pessimistically.
            // pj.isvalid  = false;
            // At this point in the code, we have all the time in the world.
            // Note that we know exactly where we are in the document so we could,
            // without any overhead on the processing code, report a specific location.
            // We could even trigger special code paths to assess what happened carefully,
            // all without any added cost.
            if (depth >= pj.depthcapacity)
            {
                pj.ErrorCode = JsonParseError.DEPTH_ERROR;
                return pj.ErrorCode;
            }

            switch (c)
            {
                case (uint8_t) '"':
                    pj.ErrorCode = JsonParseError.STRING_ERROR;
                    return pj.ErrorCode;
                case (uint8_t) '0':
                case (uint8_t) '1':
                case (uint8_t) '2':
                case (uint8_t) '3':
                case (uint8_t) '4':
                case (uint8_t) '5':
                case (uint8_t) '6':
                case (uint8_t) '7':
                case (uint8_t) '8':
                case (uint8_t) '9':
                case (uint8_t) '-':
                    pj.ErrorCode = JsonParseError.NUMBER_ERROR;
                    return pj.ErrorCode;
                case (uint8_t) 't':
                    pj.ErrorCode = JsonParseError.T_ATOM_ERROR;
                    return pj.ErrorCode;
                case (uint8_t) 'n':
                    pj.ErrorCode = JsonParseError.N_ATOM_ERROR;
                    return pj.ErrorCode;
                case (uint8_t) 'f':
                    pj.ErrorCode = JsonParseError.F_ATOM_ERROR;
                    return pj.ErrorCode;
                default:
                    break;
            }

            pj.ErrorCode = JsonParseError.TAPE_ERROR;
            return pj.ErrorCode;
        }
    }
}
