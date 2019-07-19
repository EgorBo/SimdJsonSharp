// This file is auto-generated (EgorBo/CppPinvokeGenerator). Do not edit.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace SimdJsonSharp
{
﻿    public unsafe partial class PaddedStringN : SafeHandleZeroOrMinusOneIsInvalid
    {
        #region Handle
        /// <summary>
        /// Pointer to the underlying native object
        /// </summary>
        public IntPtr Handle => DangerousGetHandle();

        /// <summary>
        /// Create PaddedStringN from a native pointer
        /// if letGcDeleteNativeObject is false GC won't release the underlying object (use ONLY if you know it will be handled by some other native object)
        /// </summary>
        public PaddedStringN(IntPtr handle, bool letGcDeleteNativeObject) : base(letGcDeleteNativeObject) => SetHandle(handle);
        #endregion

        #region API
        public PaddedStringN() : base(ownsHandle: true) => SetHandle(padded_string_padded_string_0());

        public PaddedStringN(Int64 length) : base(ownsHandle: true) => SetHandle(padded_string_padded_string_s((IntPtr)length));

        public PaddedStringN(SByte* data, Int64 length) : base(ownsHandle: true) => SetHandle(padded_string_padded_string_cs(data, (IntPtr)length));

        public void Swap(PaddedStringN o) => padded_string_swap_p(Handle, (o == null ? IntPtr.Zero : o.Handle));

        public Int64 Size() => (Int64)(padded_string_size_0(Handle));

        public Int64 Length() => (Int64)(padded_string_length_0(Handle));

        public SByte* Data() => padded_string_data_0(Handle);
        #endregion

        #region DllImports
        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr padded_string_padded_string_0();

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr padded_string_padded_string_s(IntPtr/*size_t*/ length);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr padded_string_padded_string_cs(SByte* data, IntPtr/*size_t*/ length);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern void padded_string_swap_p(IntPtr target, IntPtr o);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr/*size_t*/ padded_string_size_0(IntPtr target);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr/*size_t*/ padded_string_length_0(IntPtr target);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern SByte* padded_string_data_0(IntPtr target);
        #endregion

        #region ReleaseHandle
        protected override bool ReleaseHandle()
        {
            padded_string__delete(Handle);
            return true;
        }

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern void padded_string__delete(IntPtr target);
        #endregion
    }

﻿    public unsafe partial class ProcessedUtfBytesN : SafeHandleZeroOrMinusOneIsInvalid
    {
        #region Handle
        /// <summary>
        /// Pointer to the underlying native object
        /// </summary>
        public IntPtr Handle => DangerousGetHandle();

        /// <summary>
        /// Create ProcessedUtfBytesN from a native pointer
        /// if letGcDeleteNativeObject is false GC won't release the underlying object (use ONLY if you know it will be handled by some other native object)
        /// </summary>
        public ProcessedUtfBytesN(IntPtr handle, bool letGcDeleteNativeObject) : base(letGcDeleteNativeObject) => SetHandle(handle);
        #endregion

        #region API

        #endregion

        #region DllImports

        #endregion

        #region ReleaseHandle
        protected override bool ReleaseHandle()
        {
            processed_utf_bytes__delete(Handle);
            return true;
        }

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern void processed_utf_bytes__delete(IntPtr target);
        #endregion
    }

﻿    public unsafe partial class ParsedJsonN : SafeHandleZeroOrMinusOneIsInvalid
    {
        #region Handle
        /// <summary>
        /// Pointer to the underlying native object
        /// </summary>
        public IntPtr Handle => DangerousGetHandle();

        /// <summary>
        /// Create ParsedJsonN from a native pointer
        /// if letGcDeleteNativeObject is false GC won't release the underlying object (use ONLY if you know it will be handled by some other native object)
        /// </summary>
        public ParsedJsonN(IntPtr handle, bool letGcDeleteNativeObject) : base(letGcDeleteNativeObject) => SetHandle(handle);
        #endregion

        #region API
        /// <summary>
        /// create a ParsedJson container with zero capacity, call allocateCapacity to
        /// allocate memory
        /// </summary>
        public ParsedJsonN() : base(ownsHandle: true) => SetHandle(ParsedJson_ParsedJson_0());

        /// <summary>
        /// if needed, allocate memory so that the object is able to process JSON
        /// documents having up to len bytes and maxdepth "depth"
        /// </summary>
        public Boolean AllocateCapacity(Int64 len, Int64 maxdepth) => ParsedJson_allocateCapacity_ss(Handle, (IntPtr)len, (IntPtr)maxdepth) > 0;

        /// <summary>
        /// returns true if the document parsed was valid
        /// </summary>
        public Boolean IsValid() => ParsedJson_isValid_0(Handle) > 0;

        /// <summary>
        /// return an error code corresponding to the last parsing attempt, see simdjson.h
        /// will return simdjson::UNITIALIZED if no parsing was attempted
        /// </summary>
        public Int32 GetErrorCode() => ParsedJson_getErrorCode_0(Handle);

        /// <summary>
        /// deallocate memory and set capacity to zero, called automatically by the
        /// destructor
        /// </summary>
        public void Deallocate() => ParsedJson_deallocate_0(Handle);

        /// <summary>
        /// this should be called when parsing (right before writing the tapes)
        /// </summary>
        public void Init() => ParsedJson_init_0(Handle);

        /// <summary>
        /// this should be considered a private function
        /// </summary>
        public void WriteTape(UInt64 val, Byte c) => ParsedJson_write_tape_uu(Handle, val, c);

        public void WriteTapeS64(Int64 i) => ParsedJson_write_tape_s64_i(Handle, i);

        public void WriteTapeDouble(Double d) => ParsedJson_write_tape_double_d(Handle, d);

        public UInt32 GetCurrentLoc() => ParsedJson_get_current_loc_0(Handle);

        public void AnnotatePreviousloc(UInt32 saved_loc, UInt64 val) => ParsedJson_annotate_previousloc_uu(Handle, saved_loc, val);
        #endregion

        #region DllImports
        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr ParsedJson_ParsedJson_0();

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern Byte/*bool*/ ParsedJson_allocateCapacity_ss(IntPtr target, IntPtr/*size_t*/ len, IntPtr/*size_t*/ maxdepth);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern Byte/*bool*/ ParsedJson_isValid_0(IntPtr target);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern Int32 ParsedJson_getErrorCode_0(IntPtr target);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ParsedJson_deallocate_0(IntPtr target);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ParsedJson_init_0(IntPtr target);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ParsedJson_write_tape_uu(IntPtr target, UInt64 val, Byte c);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ParsedJson_write_tape_s64_i(IntPtr target, Int64 i);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ParsedJson_write_tape_double_d(IntPtr target, Double d);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern UInt32 ParsedJson_get_current_loc_0(IntPtr target);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ParsedJson_annotate_previousloc_uu(IntPtr target, UInt32 saved_loc, UInt64 val);
        #endregion

        #region ReleaseHandle
        protected override bool ReleaseHandle()
        {
            ParsedJson__delete(Handle);
            return true;
        }

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ParsedJson__delete(IntPtr target);
        #endregion
    }

﻿    public unsafe partial class InvalidJsonN : SafeHandleZeroOrMinusOneIsInvalid
    {
        #region Handle
        /// <summary>
        /// Pointer to the underlying native object
        /// </summary>
        public IntPtr Handle => DangerousGetHandle();

        /// <summary>
        /// Create InvalidJsonN from a native pointer
        /// if letGcDeleteNativeObject is false GC won't release the underlying object (use ONLY if you know it will be handled by some other native object)
        /// </summary>
        public InvalidJsonN(IntPtr handle, bool letGcDeleteNativeObject) : base(letGcDeleteNativeObject) => SetHandle(handle);
        #endregion

        #region API
        public SByte* What() => InvalidJSON_what_0(Handle);
        #endregion

        #region DllImports
        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern SByte* InvalidJSON_what_0(IntPtr target);
        #endregion

        #region ReleaseHandle
        protected override bool ReleaseHandle()
        {
            InvalidJSON__delete(Handle);
            return true;
        }

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern void InvalidJSON__delete(IntPtr target);
        #endregion
    }

﻿    public unsafe partial class ParsedJsonIteratorN : SafeHandleZeroOrMinusOneIsInvalid
    {
        #region Handle
        /// <summary>
        /// Pointer to the underlying native object
        /// </summary>
        public IntPtr Handle => DangerousGetHandle();

        /// <summary>
        /// Create ParsedJsonIteratorN from a native pointer
        /// if letGcDeleteNativeObject is false GC won't release the underlying object (use ONLY if you know it will be handled by some other native object)
        /// </summary>
        public ParsedJsonIteratorN(IntPtr handle, bool letGcDeleteNativeObject) : base(letGcDeleteNativeObject) => SetHandle(handle);
        #endregion

        #region API
        /// <summary>
        /// might throw InvalidJSON if ParsedJson is invalid
        /// </summary>
        public ParsedJsonIteratorN(ParsedJsonN pj_) : base(ownsHandle: true) => SetHandle(iterator_iterator_P((pj_ == null ? IntPtr.Zero : pj_.Handle)));

        public Boolean IsOk() => iterator_isOk_0(Handle) > 0;

        /// <summary>
        /// useful for debuging purposes
        /// </summary>
        public Int64 GetTapeLocation() => (Int64)(iterator_get_tape_location_0(Handle));

        /// <summary>
        /// useful for debuging purposes
        /// </summary>
        public Int64 GetTapeLength() => (Int64)(iterator_get_tape_length_0(Handle));

        /// <summary>
        /// returns the current depth (start at 1 with 0 reserved for the fictitious root node)
        /// </summary>
        public Int64 GetDepth() => (Int64)(iterator_get_depth_0(Handle));

        /// <summary>
        /// A scope is a series of nodes at the same depth, typically it is either an object ({) or an array ([).
        /// The root node has type 'r'.
        /// </summary>
        public Byte GetScopeType() => iterator_get_scope_type_0(Handle);

        /// <summary>
        /// move forward in document order
        /// </summary>
        public Boolean MoveForward() => iterator_move_forward_0(Handle) > 0;

        /// <summary>
        /// retrieve the character code of what we're looking at:
        /// [{"sltfn are the possibilities
        /// </summary>
        public Byte GetTokenType() => iterator_get_type_0(Handle);

        /// <summary>
        /// get the int64_t value at this node; valid only if we're at "l"
        /// </summary>
        public Int64 GetInteger() => iterator_get_integer_0(Handle);

        /// <summary>
        /// get the string value at this node (NULL ended); valid only if we're at "
        /// note that tabs, and line endings are escaped in the returned value (see print_with_escapes)
        /// return value is valid UTF-8
        /// It may contain NULL chars within the string: get_string_length determines the true 
        /// string length.
        /// </summary>
        public SByte* GetUtf8String() => iterator_get_string_0(Handle);

        /// <summary>
        /// return the length of the string in bytes
        /// </summary>
        public UInt32 GetStringLength() => iterator_get_string_length_0(Handle);

        /// <summary>
        /// get the double value at this node; valid only if
        /// we're at "d"
        /// </summary>
        public Double GetDouble() => iterator_get_double_0(Handle);

        public Boolean IsObjectOrArray() => iterator_is_object_or_array_0(Handle) > 0;

        public Boolean IsObject() => iterator_is_object_0(Handle) > 0;

        public Boolean IsArray() => iterator_is_array_0(Handle) > 0;

        public Boolean IsString() => iterator_is_string_0(Handle) > 0;

        public Boolean IsInteger() => iterator_is_integer_0(Handle) > 0;

        public Boolean IsDouble() => iterator_is_double_0(Handle) > 0;

        public Boolean IsTrue() => iterator_is_true_0(Handle) > 0;

        public Boolean IsFalse() => iterator_is_false_0(Handle) > 0;

        public Boolean IsNull() => iterator_is_null_0(Handle) > 0;

        public static Boolean IsObjectOrArray(Byte type) => iterator_is_object_or_array_u(type) > 0;

        /// <summary>
        /// when at {, go one level deep, looking for a given key
        /// if successful, we are left pointing at the value,
        /// if not, we are still pointing at the object ({)
        /// (in case of repeated keys, this only finds the first one).
        /// We seek the key using C's strcmp so if your JSON strings contain
        /// NULL chars, this would trigger a false positive: if you expect that
        /// to be the case, take extra precautions.
        /// </summary>
        public Boolean MoveToKey(SByte* key) => iterator_move_to_key_c(Handle, key) > 0;

        /// <summary>
        /// when at {, go one level deep, looking for a given key
        /// if successful, we are left pointing at the value,
        /// if not, we are still pointing at the object ({)
        /// (in case of repeated keys, this only finds the first one).
        /// The string we search for can contain NULL values.
        /// </summary>
        public Boolean MoveToKey(SByte* key, UInt32 length) => iterator_move_to_key_cu(Handle, key, length) > 0;

        /// <summary>
        /// when at a key location within an object, this moves to the accompanying value (located next to it).
        /// this is equivalent but much faster than calling "next()".
        /// </summary>
        public void MoveToValue() => iterator_move_to_value_0(Handle);

        /// <summary>
        /// Withing a given scope (series of nodes at the same depth within either an
        /// array or an object), we move forward.
        /// Thus, given [true, null, {"a":1}, [1,2]], we would visit true, null, { and [.
        /// At the object ({) or at the array ([), you can issue a "down" to visit their content.
        /// valid if we're not at the end of a scope (returns true).
        /// </summary>
        public Boolean Next() => iterator_next_0(Handle) > 0;

        /// <summary>
        /// Withing a given scope (series of nodes at the same depth within either an
        /// array or an object), we move backward.
        /// Thus, given [true, null, {"a":1}, [1,2]], we would visit ], }, null, true when starting at the end
        /// of the scope.
        /// At the object ({) or at the array ([), you can issue a "down" to visit their content.
        /// </summary>
        public Boolean Prev() => iterator_prev_0(Handle) > 0;

        /// <summary>
        /// Moves back to either the containing array or object (type { or [) from
        /// within a contained scope.
        /// Valid unless we are at the first level of the document
        /// </summary>
        public Boolean Up() => iterator_up_0(Handle) > 0;

        /// <summary>
        /// Valid if we're at a [ or { and it starts a non-empty scope; moves us to start of
        /// that deeper scope if it not empty.
        /// Thus, given [true, null, {"a":1}, [1,2]], if we are at the { node, we would move to the
        /// "a" node.
        /// </summary>
        public Boolean Down() => iterator_down_0(Handle) > 0;

        /// <summary>
        /// move us to the start of our current scope,
        /// a scope is a series of nodes at the same level
        /// </summary>
        public void ToStartScope() => iterator_to_start_scope_0(Handle);
        #endregion

        #region DllImports
        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr iterator_iterator_P(IntPtr pj_);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern Byte/*bool*/ iterator_isOk_0(IntPtr target);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr/*size_t*/ iterator_get_tape_location_0(IntPtr target);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr/*size_t*/ iterator_get_tape_length_0(IntPtr target);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr/*size_t*/ iterator_get_depth_0(IntPtr target);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern Byte iterator_get_scope_type_0(IntPtr target);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern Byte/*bool*/ iterator_move_forward_0(IntPtr target);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern Byte iterator_get_type_0(IntPtr target);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern Int64 iterator_get_integer_0(IntPtr target);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern SByte* iterator_get_string_0(IntPtr target);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern UInt32 iterator_get_string_length_0(IntPtr target);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern Double iterator_get_double_0(IntPtr target);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern Byte/*bool*/ iterator_is_object_or_array_0(IntPtr target);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern Byte/*bool*/ iterator_is_object_0(IntPtr target);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern Byte/*bool*/ iterator_is_array_0(IntPtr target);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern Byte/*bool*/ iterator_is_string_0(IntPtr target);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern Byte/*bool*/ iterator_is_integer_0(IntPtr target);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern Byte/*bool*/ iterator_is_double_0(IntPtr target);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern Byte/*bool*/ iterator_is_true_0(IntPtr target);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern Byte/*bool*/ iterator_is_false_0(IntPtr target);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern Byte/*bool*/ iterator_is_null_0(IntPtr target);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern Byte/*bool*/ iterator_is_object_or_array_u(Byte type);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern Byte/*bool*/ iterator_move_to_key_c(IntPtr target, SByte* key);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern Byte/*bool*/ iterator_move_to_key_cu(IntPtr target, SByte* key, UInt32 length);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern void iterator_move_to_value_0(IntPtr target);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern Byte/*bool*/ iterator_next_0(IntPtr target);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern Byte/*bool*/ iterator_prev_0(IntPtr target);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern Byte/*bool*/ iterator_up_0(IntPtr target);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern Byte/*bool*/ iterator_down_0(IntPtr target);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern void iterator_to_start_scope_0(IntPtr target);
        #endregion

        #region ReleaseHandle
        protected override bool ReleaseHandle()
        {
            iterator__delete(Handle);
            return true;
        }

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern void iterator__delete(IntPtr target);
        #endregion
    }

﻿    public unsafe partial class ScopeIndexTN : SafeHandleZeroOrMinusOneIsInvalid
    {
        #region Handle
        /// <summary>
        /// Pointer to the underlying native object
        /// </summary>
        public IntPtr Handle => DangerousGetHandle();

        /// <summary>
        /// Create ScopeIndexTN from a native pointer
        /// if letGcDeleteNativeObject is false GC won't release the underlying object (use ONLY if you know it will be handled by some other native object)
        /// </summary>
        public ScopeIndexTN(IntPtr handle, bool letGcDeleteNativeObject) : base(letGcDeleteNativeObject) => SetHandle(handle);
        #endregion

        #region API

        #endregion

        #region DllImports

        #endregion

        #region ReleaseHandle
        protected override bool ReleaseHandle()
        {
            scopeindex_t__delete(Handle);
            return true;
        }

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern void scopeindex_t__delete(IntPtr target);
        #endregion
    }

﻿    public unsafe partial class ParseStringHelperN : SafeHandleZeroOrMinusOneIsInvalid
    {
        #region Handle
        /// <summary>
        /// Pointer to the underlying native object
        /// </summary>
        public IntPtr Handle => DangerousGetHandle();

        /// <summary>
        /// Create ParseStringHelperN from a native pointer
        /// if letGcDeleteNativeObject is false GC won't release the underlying object (use ONLY if you know it will be handled by some other native object)
        /// </summary>
        public ParseStringHelperN(IntPtr handle, bool letGcDeleteNativeObject) : base(letGcDeleteNativeObject) => SetHandle(handle);
        #endregion

        #region API

        #endregion

        #region DllImports

        #endregion

        #region ReleaseHandle
        protected override bool ReleaseHandle()
        {
            parse_string_helper__delete(Handle);
            return true;
        }

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern void parse_string_helper__delete(IntPtr target);
        #endregion
    }

﻿    public unsafe static partial class SimdJsonN
    {
        #region API
        public static Boolean AddOverflow(UInt64 value1, UInt64 value2, UInt64* result) => _add_overflow_uuu(value1, value2, result) > 0;

        public static Boolean MulOverflow(UInt64 value1, UInt64 value2, UInt64* result) => _mul_overflow_uuu(value1, value2, result) > 0;

        public static Int32 Trailingzeroes(UInt64 input_num) => _trailingzeroes_u(input_num);

        public static Int32 Leadingzeroes(UInt64 input_num) => _leadingzeroes_u(input_num);

        public static Int32 Hamming(UInt64 input_num) => _hamming_u(input_num);

        /// <summary>
        /// portable version of  posix_memalign
        /// </summary>
        public static void* AlignedMalloc(Int64 alignment, Int64 size) => _aligned_malloc_ss((IntPtr)alignment, (IntPtr)size);

        public static SByte* AlignedMallocChar(Int64 alignment, Int64 size) => _aligned_malloc_char_ss((IntPtr)alignment, (IntPtr)size);

        public static void AlignedFree(void* memblock) => _aligned_free_v(memblock);

        public static void AlignedFreeChar(SByte* memblock) => _aligned_free_char_c(memblock);

        /// <summary>
        /// low-level function to allocate memory with padding so we can read passed the
        /// "length" bytes safely. if you must provide a pointer to some data, create it
        /// with this function: length is the max. size in bytes of the string caller is
        /// responsible to free the memory (free(...))
        /// </summary>
        public static SByte* AllocatePaddedBuffer(Int64 length) => _allocate_padded_buffer_s((IntPtr)length);

        /// <summary>
        /// return non-zero if not a structural or whitespace char
        /// zero otherwise
        /// </summary>
        public static UInt32 IsNotStructuralOrWhitespaceOrNull(Byte c) => _is_not_structural_or_whitespace_or_null_u(c);

        /// <summary>
        /// return non-zero if not a structural or whitespace char
        /// zero otherwise
        /// </summary>
        public static UInt32 IsNotStructuralOrWhitespace(Byte c) => _is_not_structural_or_whitespace_u(c);

        public static UInt32 IsStructuralOrWhitespaceOrNull(Byte c) => _is_structural_or_whitespace_or_null_u(c);

        public static UInt32 IsStructuralOrWhitespace(Byte c) => _is_structural_or_whitespace_u(c);

        /// <summary>
        /// returns a value with the high 16 bits set if not valid
        /// otherwise returns the conversion of the 4 hex digits at src into the bottom 16 bits of the 32-bit
        /// return register
        /// see https://lemire.me/blog/2019/04/17/parsing-short-hexadecimal-strings-efficiently/ 
        /// </summary>
        public static UInt32 HexToU32Nocheck(Byte* src) => _hex_to_u32_nocheck_u(src);

        /// <summary>
        /// given a code point cp, writes to c
        /// the utf-8 code, outputting the length in
        /// bytes, if the length is zero, the code point
        /// is invalid
        /// This can possibly be made faster using pdep
        /// and clz and table lookups, but JSON documents
        /// have few escaped code points, and the following
        /// function looks cheap.
        /// Note: we assume that surrogates are treated separately
        /// </summary>
        public static Int64 CodepointToUtf8(UInt32 cp, Byte* c) => (Int64)(_codepoint_to_utf8_uu(cp, c));

        /// <summary>
        /// ends with zero char
        /// </summary>
        public static void PrintWithEscapes(Byte* src) => _print_with_escapes_u(src);

        /// <summary>
        /// print len chars
        /// </summary>
        public static void PrintWithEscapes(Byte* src, Int64 len) => _print_with_escapes_us(src, (IntPtr)len);

        /// <summary>
        /// Take input from buf and remove useless whitespace, write it to out; buf and
        /// out can be the same pointer. Result is null terminated,
        /// return the string length (minus the null termination).
        /// The accelerated version of this function only runs on AVX2 hardware.
        /// </summary>
        public static Int64 JsonMinify(Byte* buf, Int64 len, Byte* @out) => (Int64)(_jsonminify_usu(buf, (IntPtr)len, @out));

        public static Int64 JsonMinify(SByte* buf, Int64 len, SByte* @out) => (Int64)(_jsonminify_csc(buf, (IntPtr)len, @out));

        public static Int64 JsonMinify(PaddedStringN p, SByte* @out) => (Int64)(_jsonminify_pc((p == null ? IntPtr.Zero : p.Handle), @out));

        /// <summary>
        /// flatten out values in 'bits' assuming that they are are to have values of idx
        /// plus their position in the bitvector, and store these indexes at
        /// base_ptr[base] incrementing base as we go
        /// will potentially store extra values beyond end of valid bits, so base_ptr
        /// needs to be large enough to handle this
        /// </summary>
        public static void FlattenBits(UInt32* base_ptr, UInt32 @base, UInt32 idx, UInt64 bits) => _flatten_bits_uuuu(base_ptr, @base, idx, bits);

        /// <summary>
        /// return a updated structural bit vector with quoted contents cleared out and
        /// pseudo-structural characters added to the mask
        /// updates prev_iter_ends_pseudo_pred which tells us whether the previous
        /// iteration ended on a whitespace or a structural character (which means that
        /// the next iteration
        /// will have a pseudo-structural character at its start)
        /// </summary>
        public static UInt64 FinalizeStructurals(UInt64 structurals, UInt64 whitespace, UInt64 quote_mask, UInt64 quote_bits, UInt64 prev_iter_ends_pseudo_pred) => _finalize_structurals_uuuuu(structurals, whitespace, quote_mask, quote_bits, prev_iter_ends_pseudo_pred);

        public static Int32 FindStructuralBits(Byte* buf, Int64 len, ParsedJsonN pj) => _find_structural_bits_usP(buf, (IntPtr)len, (pj == null ? IntPtr.Zero : pj.Handle));

        public static Int32 FindStructuralBits(SByte* buf, Int64 len, ParsedJsonN pj) => _find_structural_bits_csP(buf, (IntPtr)len, (pj == null ? IntPtr.Zero : pj.Handle));

        /// <summary>
        /// handle a unicode codepoint
        /// write appropriate values into dest
        /// src will advance 6 bytes or 12 bytes
        /// dest will advance a variable amount (return via pointer)
        /// return true if the unicode codepoint was valid
        /// We work in little-endian then swap at write time
        /// </summary>
        public static Boolean HandleUnicodeCodepoint(Byte* src_ptr, Byte* dst_ptr) => _handle_unicode_codepoint_uu(src_ptr, dst_ptr) > 0;

        public static Boolean ParseString(Byte* buf, Int64 len, ParsedJsonN pj, UInt32 depth, UInt32 offset) => _parse_string_usPuu(buf, (IntPtr)len, (pj == null ? IntPtr.Zero : pj.Handle), depth, offset) > 0;

        public static Boolean IsInteger(SByte c) => _is_integer_c(c) > 0;

        public static Boolean IsNotStructuralOrWhitespaceOrExponentOrDecimal(Byte c) => _is_not_structural_or_whitespace_or_exponent_or_decimal_u(c) > 0;

        /// <summary>
        /// check quickly whether the next 8 chars are made of digits
        /// at a glance, it looks better than Mula's
        /// http://0x80.pl/articles/swar-digits-validate.html
        /// </summary>
        public static Boolean IsMadeOfEightDigitsFast(SByte* chars) => _is_made_of_eight_digits_fast_c(chars) > 0;

        /// <summary>
        /// we don't have SSE, so let us use a scalar function
        /// credit: https://johnnylee-sde.github.io/Fast-numeric-string-to-int/
        /// </summary>
        public static UInt32 ParseEightDigitsUnrolled(SByte* chars) => _parse_eight_digits_unrolled_c(chars);

        /// <summary>
        /// This function computes base * 10 ^ (- negative_exponent ).
        /// It is only even going to be used when negative_exponent is tiny.
        /// </summary>
        public static Double SubnormalPower10(Double @base, Int32 negative_exponent) => _subnormal_power10_di(@base, negative_exponent);

        /// <summary>
        /// called by parse_number when we know that the output is a float,
        /// but where there might be some integer overflow. The trick here is to
        /// parse using floats from the start.
        /// Do not call this function directly as it skips some of the checks from
        /// parse_number
        /// This function will almost never be called!!!
        /// Note: a redesign could avoid this function entirely.
        /// </summary>
        public static Boolean ParseFloat(Byte* buf, ParsedJsonN pj, UInt32 offset, Boolean found_minus) => _parse_float_uPub(buf, (pj == null ? IntPtr.Zero : pj.Handle), offset, (Byte)(found_minus ? 1 : 0)) > 0;

        /// <summary>
        /// called by parse_number when we know that the output is an integer,
        /// but where there might be some integer overflow.
        /// we want to catch overflows!
        /// Do not call this function directly as it skips some of the checks from
        /// parse_number
        /// This function will almost never be called!!!
        /// </summary>
        public static Boolean ParseLargeInteger(Byte* buf, ParsedJsonN pj, UInt32 offset, Boolean found_minus) => _parse_large_integer_uPub(buf, (pj == null ? IntPtr.Zero : pj.Handle), offset, (Byte)(found_minus ? 1 : 0)) > 0;

        /// <summary>
        /// parse the number at buf + offset
        /// define JSON_TEST_NUMBERS for unit testing
        /// It is assumed that the number is followed by a structural ({,},],[) character
        /// or a white space character. If that is not the case (e.g., when the JSON document
        /// is made of a single number), then it is necessary to copy the content and append
        /// a space before calling this function.
        /// </summary>
        public static Boolean ParseNumber(Byte* buf, ParsedJsonN pj, UInt32 offset, Boolean found_minus) => _parse_number_uPub(buf, (pj == null ? IntPtr.Zero : pj.Handle), offset, (Byte)(found_minus ? 1 : 0)) > 0;

        public static Boolean IsValidTrueAtom(Byte* loc) => _is_valid_true_atom_u(loc) > 0;

        public static Boolean IsValidFalseAtom(Byte* loc) => _is_valid_false_atom_u(loc) > 0;

        public static Boolean IsValidNullAtom(Byte* loc) => _is_valid_null_atom_u(loc) > 0;

        /// <summary>
        /// **********
        /// The JSON is parsed to a tape, see the accompanying tape.md file
        /// for documentation.
        /// *********
        /// </summary>
        public static Int32 UnifiedMachine(Byte* buf, Int64 len, ParsedJsonN pj) => _unified_machine_usP(buf, (IntPtr)len, (pj == null ? IntPtr.Zero : pj.Handle));

        public static Int32 UnifiedMachine(SByte* buf, Int64 len, ParsedJsonN pj) => _unified_machine_csP(buf, (IntPtr)len, (pj == null ? IntPtr.Zero : pj.Handle));

        /// <summary>
        /// json_parse_implementation is the generic function, it is specialized for various 
        /// SIMD instruction sets, e.g., as json_parse_implementation
        /// <instruction
        /// _set::avx2>
        /// or json_parse_implementation
        /// <instruction
        /// _set::neon> 
        /// </summary>
        public static Int32 JsonParseImplementation(Byte* buf, Int64 len, ParsedJsonN pj, Boolean reallocifneeded) => _json_parse_implementation_usPb(buf, (IntPtr)len, (pj == null ? IntPtr.Zero : pj.Handle), (Byte)(reallocifneeded ? 1 : 0));

        /// <summary>
        /// Parse a document found in buf. 
        /// You need to preallocate ParsedJson with a capacity of len (e.g., pj.allocateCapacity(len)).
        /// The function returns simdjson::SUCCESS (an integer = 0) in case of a success or an error code from 
        /// simdjson/simdjson.h in case of failure such as  simdjson::CAPACITY, simdjson::MEMALLOC, 
        /// simdjson::DEPTH_ERROR and so forth; the simdjson::errorMsg function converts these error codes 
        /// into a string). 
        /// You can also check validity by calling pj.isValid(). The same ParsedJson can be reused for other documents.
        /// If reallocifneeded is true (default) then a temporary buffer is created when needed during processing
        /// (a copy of the input string is made).
        /// The input buf should be readable up to buf + len + SIMDJSON_PADDING if reallocifneeded is false,
        /// all bytes at and after buf + len  are ignored (can be garbage).
        /// The ParsedJson object can be reused.
        /// </summary>
        public static Int32 JsonParse(Byte* buf, Int64 len, ParsedJsonN pj, Boolean reallocifneeded) => _json_parse_usPb(buf, (IntPtr)len, (pj == null ? IntPtr.Zero : pj.Handle), (Byte)(reallocifneeded ? 1 : 0));

        /// <summary>
        /// Parse a document found in buf.
        /// You need to preallocate ParsedJson with a capacity of len (e.g., pj.allocateCapacity(len)).
        /// The function returns simdjson::SUCCESS (an integer = 0) in case of a success or an error code from 
        /// simdjson/simdjson.h in case of failure such as  simdjson::CAPACITY, simdjson::MEMALLOC, 
        /// simdjson::DEPTH_ERROR and so forth; the simdjson::errorMsg function converts these error codes 
        /// into a string). 
        /// You can also check validity
        /// by calling pj.isValid(). The same ParsedJson can be reused for other documents.
        /// If reallocifneeded is true (default) then a temporary buffer is created when needed during processing
        /// (a copy of the input string is made).
        /// The input buf should be readable up to buf + len + SIMDJSON_PADDING  if reallocifneeded is false,
        /// all bytes at and after buf + len  are ignored (can be garbage).
        /// The ParsedJson object can be reused.
        /// </summary>
        public static Int32 JsonParse(SByte* buf, Int64 len, ParsedJsonN pj, Boolean reallocifneeded) => _json_parse_csPb(buf, (IntPtr)len, (pj == null ? IntPtr.Zero : pj.Handle), (Byte)(reallocifneeded ? 1 : 0));

        /// <summary>
        /// We do not want to allow implicit conversion from C string to std::string.
        /// </summary>
        public static Int32 JsonParse(SByte* buf, ParsedJsonN pj) => _json_parse_cP(buf, (pj == null ? IntPtr.Zero : pj.Handle));

        /// <summary>
        /// Parse a document found in in string s.
        /// You need to preallocate ParsedJson with a capacity of len (e.g., pj.allocateCapacity(len)).
        /// The function returns simdjson::SUCCESS (an integer = 0) in case of a success or an error code from 
        /// simdjson/simdjson.h in case of failure such as  simdjson::CAPACITY, simdjson::MEMALLOC, 
        /// simdjson::DEPTH_ERROR and so forth; the simdjson::errorMsg function converts these error codes 
        /// into a string). 
        /// You can also check validity
        /// by calling pj.isValid(). The same ParsedJson can be reused for other documents.
        /// </summary>
        public static Int32 JsonParse(PaddedStringN s, ParsedJsonN pj) => _json_parse_pP((s == null ? IntPtr.Zero : s.Handle), (pj == null ? IntPtr.Zero : pj.Handle));

        /// <summary>
        /// Build a ParsedJson object. You can check validity
        /// by calling pj.isValid(). This does the memory allocation needed for ParsedJson.
        /// If reallocifneeded is true (default) then a temporary buffer is created when needed during processing
        /// (a copy of the input string is made).
        /// the input buf should be readable up to buf + len + SIMDJSON_PADDING  if reallocifneeded is false,
        /// all bytes at and after buf + len  are ignored (can be garbage).
        /// This is a convenience function which calls json_parse.
        /// </summary>
        public static ParsedJsonN BuildParsedJson(Byte* buf, Int64 len, Boolean reallocifneeded) => new ParsedJsonN(_build_parsed_json_usb(buf, (IntPtr)len, (Byte)(reallocifneeded ? 1 : 0)), false);

        /// <summary>
        /// Build a ParsedJson object. You can check validity
        /// by calling pj.isValid(). This does the memory allocation needed for ParsedJson.
        /// If reallocifneeded is true (default) then a temporary buffer is created when needed during processing
        /// (a copy of the input string is made).
        /// The input buf should be readable up to buf + len + SIMDJSON_PADDING if reallocifneeded is false,
        /// all bytes at and after buf + len  are ignored (can be garbage).
        /// This is a convenience function which calls json_parse.
        /// </summary>
        public static ParsedJsonN BuildParsedJson(SByte* buf, Int64 len, Boolean reallocifneeded) => new ParsedJsonN(_build_parsed_json_csb(buf, (IntPtr)len, (Byte)(reallocifneeded ? 1 : 0)), false);

        /// <summary>
        /// We do not want to allow implicit conversion from C string to std::string.
        /// </summary>
        public static ParsedJsonN BuildParsedJson(SByte* buf) => new ParsedJsonN(_build_parsed_json_c(buf), false);

        /// <summary>
        /// Parse a document found in in string s.
        /// You need to preallocate ParsedJson with a capacity of len (e.g., pj.allocateCapacity(len)).
        /// Return SUCCESS (an integer = 0) in case of a success. You can also check validity
        /// by calling pj.isValid(). The same ParsedJson can be reused for other documents.
        /// This is a convenience function which calls json_parse.
        /// </summary>
        public static ParsedJsonN BuildParsedJson(PaddedStringN s) => new ParsedJsonN(_build_parsed_json_p((s == null ? IntPtr.Zero : s.Handle)), false);
        #endregion

        #region DllImports
        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern Byte/*bool*/ _add_overflow_uuu(UInt64 value1, UInt64 value2, UInt64* result);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern Byte/*bool*/ _mul_overflow_uuu(UInt64 value1, UInt64 value2, UInt64* result);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern Int32 _trailingzeroes_u(UInt64 input_num);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern Int32 _leadingzeroes_u(UInt64 input_num);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern Int32 _hamming_u(UInt64 input_num);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern void* _aligned_malloc_ss(IntPtr/*size_t*/ alignment, IntPtr/*size_t*/ size);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern SByte* _aligned_malloc_char_ss(IntPtr/*size_t*/ alignment, IntPtr/*size_t*/ size);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern void _aligned_free_v(void* memblock);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern void _aligned_free_char_c(SByte* memblock);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern SByte* _allocate_padded_buffer_s(IntPtr/*size_t*/ length);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern UInt32 _is_not_structural_or_whitespace_or_null_u(Byte c);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern UInt32 _is_not_structural_or_whitespace_u(Byte c);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern UInt32 _is_structural_or_whitespace_or_null_u(Byte c);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern UInt32 _is_structural_or_whitespace_u(Byte c);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern UInt32 _hex_to_u32_nocheck_u(Byte* src);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr/*size_t*/ _codepoint_to_utf8_uu(UInt32 cp, Byte* c);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern void _print_with_escapes_u(Byte* src);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern void _print_with_escapes_us(Byte* src, IntPtr/*size_t*/ len);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr/*size_t*/ _jsonminify_usu(Byte* buf, IntPtr/*size_t*/ len, Byte* @out);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr/*size_t*/ _jsonminify_csc(SByte* buf, IntPtr/*size_t*/ len, SByte* @out);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr/*size_t*/ _jsonminify_pc(IntPtr p, SByte* @out);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern void _flatten_bits_uuuu(UInt32* base_ptr, UInt32 @base, UInt32 idx, UInt64 bits);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern UInt64 _finalize_structurals_uuuuu(UInt64 structurals, UInt64 whitespace, UInt64 quote_mask, UInt64 quote_bits, UInt64 prev_iter_ends_pseudo_pred);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern Int32 _find_structural_bits_usP(Byte* buf, IntPtr/*size_t*/ len, IntPtr pj);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern Int32 _find_structural_bits_csP(SByte* buf, IntPtr/*size_t*/ len, IntPtr pj);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern Byte/*bool*/ _handle_unicode_codepoint_uu(Byte* src_ptr, Byte* dst_ptr);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr _find_bs_bits_and_quote_bits_uu(Byte* src, Byte* dst);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern Byte/*bool*/ _parse_string_usPuu(Byte* buf, IntPtr/*size_t*/ len, IntPtr pj, UInt32 depth, UInt32 offset);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern Byte/*bool*/ _is_integer_c(SByte c);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern Byte/*bool*/ _is_not_structural_or_whitespace_or_exponent_or_decimal_u(Byte c);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern Byte/*bool*/ _is_made_of_eight_digits_fast_c(SByte* chars);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern UInt32 _parse_eight_digits_unrolled_c(SByte* chars);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern Double _subnormal_power10_di(Double @base, Int32 negative_exponent);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern Byte/*bool*/ _parse_float_uPub(Byte* buf, IntPtr pj, UInt32 offset, Byte/*bool*/ found_minus);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern Byte/*bool*/ _parse_large_integer_uPub(Byte* buf, IntPtr pj, UInt32 offset, Byte/*bool*/ found_minus);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern Byte/*bool*/ _parse_number_uPub(Byte* buf, IntPtr pj, UInt32 offset, Byte/*bool*/ found_minus);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern Byte/*bool*/ _is_valid_true_atom_u(Byte* loc);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern Byte/*bool*/ _is_valid_false_atom_u(Byte* loc);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern Byte/*bool*/ _is_valid_null_atom_u(Byte* loc);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern Int32 _unified_machine_usP(Byte* buf, IntPtr/*size_t*/ len, IntPtr pj);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern Int32 _unified_machine_csP(SByte* buf, IntPtr/*size_t*/ len, IntPtr pj);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern Int32 _json_parse_implementation_usPb(Byte* buf, IntPtr/*size_t*/ len, IntPtr pj, Byte/*bool*/ reallocifneeded);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern Int32 _json_parse_usPb(Byte* buf, IntPtr/*size_t*/ len, IntPtr pj, Byte/*bool*/ reallocifneeded);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern Int32 _json_parse_csPb(SByte* buf, IntPtr/*size_t*/ len, IntPtr pj, Byte/*bool*/ reallocifneeded);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern Int32 _json_parse_cP(SByte* buf, IntPtr pj);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern Int32 _json_parse_pP(IntPtr s, IntPtr pj);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr _build_parsed_json_usb(Byte* buf, IntPtr/*size_t*/ len, Byte/*bool*/ reallocifneeded);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr _build_parsed_json_csb(SByte* buf, IntPtr/*size_t*/ len, Byte/*bool*/ reallocifneeded);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr _build_parsed_json_c(SByte* buf);

        [DllImport(SimdJsonN.NativeLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr _build_parsed_json_p(IntPtr s);
        #endregion
    }
}
