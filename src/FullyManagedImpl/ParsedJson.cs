// This file is a manual port of C code https://github.com/lemire/simdjson to C#
// (c) Daniel Lemire and Geoff Langdale

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;

#region stdint types and friends
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
    public unsafe class ParsedJson : IDisposable
    {
        internal size_t bytecapacity; // indicates how many bits are meant to be supported 
        internal size_t depthcapacity; // how deep we can go
        internal size_t tapecapacity;
        internal size_t stringcapacity;
        internal uint32_t current_loc;
        internal uint32_t n_structural_indexes;
        internal uint32_t* structural_indexes;
        internal uint64_t* tape;
        internal uint32_t* containing_scope_offset;
        internal char1* ret_address;
        internal uint8_t* string_buf; // should be at least bytecapacity
        internal uint8_t* current_string_buf_loc;
        internal bool isvalid;
        internal bool isDisposed;

        public JsonParseError ErrorCode { get; internal set; }

        public ParsedJson()
        {
            if (!Sse42.IsSupported || IntPtr.Size == 4)
                throw new NotSupportedException("SimdJson requires AVX2 or SSE42 and x64");
        }

        // if needed, allocate memory so that the object is able to process JSON
        // documents having up to len bytes and maxdepth "depth"
        public bool AllocateCapacity(size_t len, size_t maxdepth = DEFAULTMAXDEPTH)
        {
            if ((maxdepth == 0) || (len == 0))
            {
                return false;
            }
            if (len > SIMDJSON_MAXSIZE_BYTES)
            {
                return false;
            }
            if ((len <= bytecapacity) && (depthcapacity < maxdepth))
            {
                return true;
            }
            Deallocate();
            isvalid = false;
            bytecapacity = 0; // will only set it to len after allocations are a success
            n_structural_indexes = 0;
            uint32_t max_structures = (uint32_t)(ROUNDUP_N(len, 64) + 2 + 7);
            structural_indexes = allocate<uint32_t>(max_structures);
            // a pathological input like "[[[[..." would generate len tape elements, so need a capacity of len + 1
            size_t localtapecapacity = ROUNDUP_N(len + 1, 64);
            // a document with only zero-length strings... could have len/3 string
            // and we would need len/3 * 5 bytes on the string buffer 
            size_t localstringcapacity = ROUNDUP_N(5 * len / 3 + 32, 64);
            string_buf = allocate <uint8_t>(localstringcapacity);
            tape = allocate <uint64_t>(localtapecapacity);
            containing_scope_offset = allocate <uint32_t>(maxdepth);
            ret_address = allocate<char1>(maxdepth);
            if ((string_buf == null) || (tape == null) ||
                (containing_scope_offset == null) || (ret_address == null) || (structural_indexes == null))
            {
                delete(ret_address);
                delete(containing_scope_offset);
                delete(tape);
                delete(string_buf);
                delete(structural_indexes);
                return false;
            }
            /*
            // We do not need to initialize this content for parsing, though we could
            // need to initialize it for safety.
            memset(string_buf, 0 , localstringcapacity); 
            memset(structural_indexes, 0, max_structures * sizeof(uint32_t)); 
            memset(tape, 0, localtapecapacity * sizeof(uint64_t)); 
            */
            bytecapacity = len;
            depthcapacity = maxdepth;
            tapecapacity = localtapecapacity;
            stringcapacity = localstringcapacity;
            return true;
        }

        private void Deallocate()
        {
            bytecapacity = 0;
            depthcapacity = 0;
            tapecapacity = 0;
            stringcapacity = 0;
            delete(ret_address);
            delete(containing_scope_offset);
            delete(tape);
            delete(string_buf);
            delete(structural_indexes);
            isvalid = false;
        }

        public void Dispose() => Dispose(true);

        private void Dispose(bool disposing)
        {
            if (disposing)
                GC.SuppressFinalize(this);

            if (!isDisposed)
            {
                isDisposed = true;
                Deallocate();
            }
        }

        ~ParsedJson()
        {
            Dispose(false);
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
