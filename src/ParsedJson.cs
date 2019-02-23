// This file is a manual port of C code https://github.com/lemire/simdjson to C#
// (c) Daniel Lemire

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
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
    public unsafe struct ParsedJson : IDisposable
    {
        public size_t bytecapacity; // indicates how many bits are meant to be supported 
        public size_t depthcapacity; // how deep we can go
        public size_t tapecapacity;
        public size_t stringcapacity;
        public uint32_t current_loc;
        public uint32_t n_structural_indexes;
        public uint32_t* structural_indexes;
        public uint64_t* tape;
        public uint32_t* containing_scope_offset;
        public bytechar* ret_address;
        public uint8_t* string_buf; // should be at least bytecapacity
        public uint8_t* current_string_buf_loc;
        public bool isvalid;

        // if needed, allocate memory so that the object is able to process JSON
        // documents having up to len butes and maxdepth "depth"
        //WARN_UNUSED
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool allocateCapacity(size_t len, size_t maxdepth = DEFAULTMAXDEPTH)
        {
            if ((maxdepth == 0) || (len == 0))
            {
                Debug.WriteLine("capacities must be non-zero ");
                return false;
            }

            if (len > 0)
            {
                if ((len <= bytecapacity) && (depthcapacity < maxdepth))
                    return true;
                deallocate();
            }

            isvalid = false;
            bytecapacity = 0; // will only set it to len after allocations are a success
            n_structural_indexes = 0;
            uint32_t max_structures = (uint32_t) ROUNDUP_N(len, 64) + 2 + 7;
            structural_indexes = Utils.allocate<uint32_t>(max_structures);
            size_t localtapecapacity = ROUNDUP_N(len, 64);
            size_t localstringcapacity = ROUNDUP_N(len, 64);
            string_buf = Utils.allocate<uint8_t>(localstringcapacity);
            tape = Utils.allocate<uint64_t>(localtapecapacity);
            containing_scope_offset = Utils.allocate<uint32_t>(maxdepth);
            ret_address = Utils.allocate<bytechar>(maxdepth);
            if ((string_buf == null) || (tape == null) ||
                (containing_scope_offset == null) || (ret_address == null) || (structural_indexes == null))
            {
                Debug.WriteLine("Could not allocate memory");
                if (ret_address != null) Utils.delete(ret_address);
                if (containing_scope_offset != null) Utils.delete(containing_scope_offset);
                if (tape != null) Utils.delete(tape);
                if (string_buf != null) Utils.delete(string_buf);
                if (structural_indexes != null) Utils.delete(structural_indexes);
                return false;
            }

            bytecapacity = len;
            depthcapacity = maxdepth;
            tapecapacity = localtapecapacity;
            stringcapacity = localstringcapacity;
            return true;
        }

        void deallocate()
        {
            bytecapacity = 0;
            depthcapacity = 0;
            tapecapacity = 0;
            stringcapacity = 0;
            if (ret_address != null) Utils.delete(ret_address);
            if (containing_scope_offset != null) Utils.delete(containing_scope_offset);
            if (tape != null) Utils.delete(tape);
            if (string_buf != null) Utils.delete(string_buf);
            if (structural_indexes != null) Utils.delete(structural_indexes);
            isvalid = false;
        }

        public void Dispose()
        {
            deallocate();
        }

        public bool isValid()
        {
            return isvalid;
        }

        // this should be called when parsing (right before writing the tapes)
        public void init()
        {
            current_string_buf_loc = string_buf;
            current_loc = 0;
            isvalid = false;
        }

        // all nodes are stored on the tape using a 64-bit word.
        //
        // strings, double and ints are stored as
        //  a 64-bit word with a pointer to the actual value
        //
        //
        //
        // for objects or arrays, store [ or {  at the beginning and } and ] at the
        // end. For the openings ([ or {), we annotate them with a reference to the
        // location on the tape of the end, and for then closings (} and ]), we
        // annotate them with a reference to the location of the opening
        //
        //

        // this should be considered a private function
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void write_tape(uint64_t val, uint8_t c)
        {
            tape[current_loc++] = val | (((uint64_t) c) << 56);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void write_tape_s64(int64_t i)
        {
            write_tape(0, (uint8_t) 'l');
            tape[current_loc++] = *((uint64_t*) &i);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void write_tape_double(double d)
        {
            write_tape(0, (uint8_t) 'd');
            //static_assert(sizeof(d) == sizeof(tape[current_loc]), "mismatch size");
            Utils.memcpy(&tape[current_loc++], &d, sizeof(double));
            //tape[current_loc++] = *((uint64_t *)&d);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint32_t get_current_loc()
        {
            return current_loc;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void annotate_previousloc(uint32_t saved_loc, uint64_t val)
        {
            tape[saved_loc] |= val;
        }
    }

    public struct scopeindex_t
    {
        public size_t start_of_scope;
        public uint8_t scope_type;
    }

    public unsafe struct iterator : IDisposable
    {
        size_t depth;
        size_t location; // our current location on a tape
        size_t tape_length;
        uint8_t current_type;
        uint64_t current_val;
        scopeindex_t* depthindex;

        ParsedJson* pj;

        public iterator(ParsedJson* pj)
        {
            this.pj = pj;
            depth = 0;
            location = 0;
            tape_length = 0;
            depthindex = null;
            current_type = 0;
            current_val = 0;

            if (pj->isValid())
            {
                depthindex = allocate<scopeindex_t>(pj->depthcapacity);
                if (depthindex == null)
                {
                    return;
                }

                depthindex[0].start_of_scope = location;
                current_val = pj->tape[location++];
                current_type = (uint8_t)(current_val >> 56);
                depthindex[0].scope_type = current_type;
                if (current_type == 'r')
                {
                    tape_length = current_val & JSONVALUEMASK;
                    if (location < tape_length)
                    {
                        current_val = pj->tape[location];
                        current_type = (uint8_t)(current_val >> 56);
                        depth++;
                        depthindex[depth].start_of_scope = location;
                        depthindex[depth].scope_type = current_type;
                    }
                }
            }
            else
            {
                throw new InvalidOperationException("Json is invalid");
            }
        }

        //WARN_UNUSED
        public bool isOk()
        {
            return location < tape_length;
        }

        // useful for debuging purposes
        public size_t get_tape_location()
        {
            return location;
        }

        // useful for debuging purposes
        public size_t get_tape_length()
        {
            return tape_length;
        }

        // returns the current depth (start at 1 with 0 reserved for the fictitious root node)
        public size_t get_depth()
        {
            return depth;
        }

        // A scope is a series of nodes at the same depth, typically it is either an object ({) or an array ([).
        // The root node has type 'r'.
        public uint8_t get_scope_type()
        {
            return depthindex[depth].scope_type;
        }

        // move forward in document order
        public bool move_forward()
        {
            if (location + 1 >= tape_length)
            {
                return false; // we are at the end!
            }

            // we are entering a new scope
            if ((current_type == '[') || (current_type == '{'))
            {
                depth++;
                depthindex[depth].start_of_scope = location;
                depthindex[depth].scope_type = current_type;
            }

            location = location + 1;
            current_val = pj->tape[location];
            current_type = (uint8_t)(current_val >> 56);
            // if we encounter a scope closure, we need to move up
            while ((current_type == ']') || (current_type == '}'))
            {
                if (location + 1 >= tape_length)
                {
                    return false; // we are at the end!
                }

                depth--;
                if (depth == 0)
                {
                    return false; // should not be necessary
                }

                location = location + 1;
                current_val = pj->tape[location];
                current_type = (uint8_t)(current_val >> 56);
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint8_t get_type()
        {
            return current_type;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // get the int64_t value at this node; valid only if we're at "l"
        public int64_t get_integer()
        {
            if (location + 1 >= tape_length) return 0; // default value in case of error
            return (int64_t)pj->tape[location + 1];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // get the double value at this node; valid only if
        // we're at "d"
        public double get_double()
        {
            if (location + 1 >= tape_length) return double.NaN; // default value in case of error
            double answer;
            Utils.memcpy(&answer, &pj->tape[location + 1], sizeof(double));
            return answer;
        }

        public bool is_object_or_array()
        {
            return is_object_or_array(get_type());
        }

        public bool is_object()
        {
            return get_type() == '{';
        }

        public bool is_array()
        {
            return get_type() == '[';
        }

        public bool is_string()
        {
            return get_type() == '"';
        }

        public bool is_integer()
        {
            return get_type() == 'l';
        }

        public bool is_double()
        {
            return get_type() == 'd';
        }

        public static bool is_object_or_array(uint8_t type)
        {
            return (type == '[' || (type == '{'));
        }

        // when at {, go one level deep, looking for a given key
        // if successful, we are left pointing at the value,
        // if not, we are still pointing at the object ({)
        // (in case of repeated keys, this only finds the first one)
        public bool move_to_key(bytechar* key)
        {
            if (down())
            {
                do
                {
                    Debug.Assert(is_string());
                    bool rightkey = (Utils.strcmp(get_string(), key) == 0);
                    next();
                    if (rightkey) return true;
                } while (next());

                Debug.Assert(up()); // not found
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // get the string value at this node (NULL ended); valid only if we're at "
        // note that tabs, and line endings are escaped in the returned value (see print_with_escapes)
        // return value is valid UTF-8
        public bytechar* get_string()
        {
            return (bytechar*)(pj->string_buf + (current_val & JSONVALUEMASK));
        }

        // throughout return true if we can do the navigation, false
        // otherwise
        // Withing a given scope (series of nodes at the same depth within either an
        // array or an object), we move forward.
        // Thus, given [true, null, {"a":1}, [1,2]], we would visit true, null, { and [.
        // At the object ({) or at the array ([), you can issue a "down" to visit their content.
        // valid if we're not at the end of a scope (returns true).
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool next()
        {
            if ((current_type == '[') || (current_type == '{'))
            {
                // we need to jump
                size_t npos = (current_val & JSONVALUEMASK);
                if (npos >= tape_length)
                {
                    return false; // shoud never happen unless at the root
                }

                uint64_t nextval = pj->tape[npos];
                uint8_t nexttype = (uint8_t)(nextval >> 56);
                if ((nexttype == ']') || (nexttype == '}'))
                {
                    return false; // we reached the end of the scope
                }

                location = npos;
                current_val = nextval;
                current_type = nexttype;
                return true;
            }
            else
            {
                size_t increment = (size_t)((current_type == 'd' || current_type == 'l') ? 2 : 1);
                if (location + increment >= tape_length) return false;
                uint64_t nextval = pj->tape[location + increment];
                uint8_t nexttype = (uint8_t)(nextval >> 56);
                if ((nexttype == ']') || (nexttype == '}'))
                {
                    return false; // we reached the end of the scope
                }

                location = location + increment;
                current_val = nextval;
                current_type = nexttype;
                return true;
            }
        }

        // Withing a given scope (series of nodes at the same depth within either an
        // array or an object), we move backward.
        // Thus, given [true, null, {"a":1}, [1,2]], we would visit ], }, null, true when starting at the end
        // of the scope.
        // At the object ({) or at the array ([), you can issue a "down" to visit their content.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool prev()
        {
            if (location - 1 < depthindex[depth].start_of_scope) return false;
            location -= 1;
            current_val = pj->tape[location];
            current_type = (uint8_t)(current_val >> 56);
            if ((current_type == ']') || (current_type == '}'))
            {
                // we need to jump
                size_t new_location = (current_val & JSONVALUEMASK);
                if (new_location < depthindex[depth].start_of_scope)
                {
                    return false; // shoud never happen
                }

                location = new_location;
                current_val = pj->tape[location];
                current_type = (uint8_t)(current_val >> 56);
            }

            return true;
        }

        // Moves back to either the containing array or object (type { or [) from
        // within a contained scope.
        // Valid unless we are at the first level of the document
        //
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool up()
        {
            if (depth == 1)
            {
                return false; // don't allow moving back to root
            }

            to_start_scope();
            // next we just move to the previous value
            depth--;
            location -= 1;
            current_val = pj->tape[location];
            current_type = (uint8_t)(current_val >> 56);
            return true;
        }

        // Valid if we're at a [ or { and it starts a non-empty scope; moves us to start of
        // that deeper scope if it not empty.
        // Thus, given [true, null, {"a":1}, [1,2]], if we are at the { node, we would move to the
        // "a" node.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool down()
        {
            if (location + 1 >= tape_length) return false;
            if ((current_type == '[') || (current_type == '{'))
            {
                size_t npos = (current_val & JSONVALUEMASK);
                if (npos == location + 2)
                {
                    return false; // we have an empty scope
                }

                depth++;
                location = location + 1;
                depthindex[depth].start_of_scope = location;
                depthindex[depth].scope_type = current_type;
                current_val = pj->tape[location];
                current_type = (uint8_t)(current_val >> 56);
                return true;
            }

            return false;
        }

        // move us to the start of our current scope,
        // a scope is a series of nodes at the same level
        public void to_start_scope()
        {
            location = depthindex[depth].start_of_scope;
            current_val = pj->tape[location];
            current_type = (uint8_t)(current_val >> 56);
        }

        public void Dispose()
        {
            if (depthindex != null)
            {
                Utils.delete(depthindex);
                depthindex = null;
            }
        }
    }
}
