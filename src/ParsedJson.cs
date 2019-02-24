// This file is a manual port of C code https://github.com/lemire/simdjson to C#
// (c) Daniel Lemire

using System;
using System.Diagnostics;
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
    public unsafe class ParsedJson : IDisposable
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

        public ParsedJson()
        {
            if (!Avx2.IsSupported)
                throw new NotSupportedException("AVX2 is required form SimdJson");
        }

        // if needed, allocate memory so that the object is able to process JSON
        // documents having up to len butes and maxdepth "depth"
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllocateCapacity(size_t len, size_t maxdepth = DEFAULTMAXDEPTH)
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
                Deallocate();
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
                Deallocate();
                return false;
            }

            bytecapacity = len;
            depthcapacity = maxdepth;
            tapecapacity = localtapecapacity;
            stringcapacity = localstringcapacity;
            return true;
        }

        private void Deallocate()
        {
            isvalid = false;
            bytecapacity = 0;
            depthcapacity = 0;
            tapecapacity = 0;
            stringcapacity = 0;

            if (ret_address != null)
            {
                delete(ret_address);
                ret_address = null;
            }

            if (containing_scope_offset != null)
            {
                delete(containing_scope_offset);
                containing_scope_offset = null;
            }

            if (tape != null)
            {
                delete(tape);
                tape = null;
            }

            if (string_buf != null)
            {
                delete(string_buf);
                string_buf = null;
            }

            if (structural_indexes != null)
            {
                delete(structural_indexes);
                structural_indexes = null;
            }
        }

        public void Dispose() => Deallocate();

        ~ParsedJson()
        {
            Dispose();
        }

        public bool IsValid => isvalid;

        // this should be called when parsing (right before writing the tapes)
        public void Init()
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
        public void WriteTape(uint64_t val, uint8_t c)
        {
            tape[current_loc++] = val | (((uint64_t) c) << 56);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteTapeInt64(int64_t i)
        {
            WriteTape(0, (uint8_t) 'l');
            tape[current_loc++] = *((uint64_t*) &i);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteTapeDouble(double d)
        {
            WriteTape(0, (uint8_t) 'd');
            //static_assert(sizeof(d) == sizeof(tape[current_loc]), "mismatch size");
            memcpy(&tape[current_loc++], &d, sizeof(double));
            //tape[current_loc++] = *((uint64_t *)&d);
        }

        public uint32_t CurrentLoc => current_loc;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AnnotatePreviousLoc(uint32_t saved_loc, uint64_t val) => tape[saved_loc] |= val;

        public ParsedJsonIterator CreateIterator() => new ParsedJsonIterator(this);
    }

    internal struct scopeindex_t
    {
        public size_t start_of_scope;
        public uint8_t scope_type;
    }
}
