// This file is auto-generated (EgorBo/CppPinvokeGenerator). Do not edit.

#if (defined WIN32 || defined _WIN32)
#define EXPORTS(returntype) extern "C" __declspec(dllexport) returntype __cdecl
#else
#define EXPORTS(returntype) extern "C" __attribute__((visibility("default"))) returntype
#endif

#include "simdjson.h"


/************* padded_string *************/

//NOT_BOUND:                    public padded_string(basic_string s)
//NOT_BOUND:                    public padded_string(padded_string && o)
//NOT_BOUND:                    private padded_string(const padded_string& o)
//NOT_BOUND:                    private padded_string& operator=(const padded_string& o)
EXPORTS(padded_string*)         padded_string_padded_string_0() { return new padded_string(); }
EXPORTS(padded_string*)         padded_string_padded_string_s(size_t length) { return new padded_string(length); }
EXPORTS(padded_string*)         padded_string_padded_string_cs(char* data, size_t length) { return new padded_string(data, length); }
EXPORTS(void)                   padded_string_swap_p(padded_string* target, padded_string& o) { target->swap(o); }
EXPORTS(size_t)                 padded_string_size_0(padded_string* target) { return target->size(); }
EXPORTS(size_t)                 padded_string_length_0(padded_string* target) { return target->length(); }
EXPORTS(char*)                  padded_string_data_0(padded_string* target) { return target->data(); }
EXPORTS(void)                   padded_string__delete(padded_string* target) { delete target; }


/************* ParsedJson *************/

//NOT_BOUND:                    public ParsedJson(ParsedJson && p)
//NOT_BOUND:                    private ParsedJson(const ParsedJson& p)
//NOT_BOUND:                    public bool printjson(basic_ostream& os)
//NOT_BOUND:                    public bool dump_raw_tape(basic_ostream& os)
//NOT_BOUND:                    private ParsedJson& operator=(const ParsedJson& o)
EXPORTS(ParsedJson*)            ParsedJson_ParsedJson_0() { return new ParsedJson(); }
EXPORTS(bool)                   ParsedJson_allocateCapacity_ss(ParsedJson* target, size_t len, size_t maxdepth) { return target->allocateCapacity(len, maxdepth); }
EXPORTS(bool)                   ParsedJson_isValid_0(ParsedJson* target) { return target->isValid(); }
EXPORTS(void)                   ParsedJson_deallocate_0(ParsedJson* target) { target->deallocate(); }
EXPORTS(void)                   ParsedJson_init_0(ParsedJson* target) { target->init(); }
EXPORTS(void)                   ParsedJson_write_tape_uu(ParsedJson* target, uint64_t val, uint8_t c) { target->write_tape(val, c); }
EXPORTS(void)                   ParsedJson_write_tape_s64_i(ParsedJson* target, int64_t i) { target->write_tape_s64(i); }
EXPORTS(void)                   ParsedJson_write_tape_double_d(ParsedJson* target, double d) { target->write_tape_double(d); }
EXPORTS(uint32_t)               ParsedJson_get_current_loc_0(ParsedJson* target) { return target->get_current_loc(); }
EXPORTS(void)                   ParsedJson_annotate_previousloc_uu(ParsedJson* target, uint32_t saved_loc, uint64_t val) { target->annotate_previousloc(saved_loc, val); }
EXPORTS(void)                   ParsedJson__delete(ParsedJson* target) { delete target; }


/************* ParsedJson::InvalidJSON *************/

EXPORTS(const char*)            InvalidJSON_what_0(ParsedJson::InvalidJSON* target) { return target->what(); }
EXPORTS(void)                   InvalidJSON__delete(ParsedJson::InvalidJSON* target) { delete target; }


/************* ParsedJson::iterator *************/

//NOT_BOUND:                    public iterator(const iterator& o)
//NOT_BOUND:                    public iterator(ParsedJson::iterator && o)
//NOT_BOUND:                    public bool print(basic_ostream& os, bool escape_strings = true) const
//NOT_BOUND:                    private iterator& operator=(const iterator& other)
EXPORTS(ParsedJson::iterator*)  iterator_iterator_P(ParsedJson& pj_) { return new ParsedJson::iterator(pj_); }
EXPORTS(bool)                   iterator_isOk_0(ParsedJson::iterator* target) { return target->isOk(); }
EXPORTS(size_t)                 iterator_get_tape_location_0(ParsedJson::iterator* target) { return target->get_tape_location(); }
EXPORTS(size_t)                 iterator_get_tape_length_0(ParsedJson::iterator* target) { return target->get_tape_length(); }
EXPORTS(size_t)                 iterator_get_depth_0(ParsedJson::iterator* target) { return target->get_depth(); }
EXPORTS(uint8_t)                iterator_get_scope_type_0(ParsedJson::iterator* target) { return target->get_scope_type(); }
EXPORTS(bool)                   iterator_move_forward_0(ParsedJson::iterator* target) { return target->move_forward(); }
EXPORTS(uint8_t)                iterator_get_type_0(ParsedJson::iterator* target) { return target->get_type(); }
EXPORTS(int64_t)                iterator_get_integer_0(ParsedJson::iterator* target) { return target->get_integer(); }
EXPORTS(const char*)            iterator_get_string_0(ParsedJson::iterator* target) { return target->get_string(); }
EXPORTS(uint32_t)               iterator_get_string_length_0(ParsedJson::iterator* target) { return target->get_string_length(); }
EXPORTS(double)                 iterator_get_double_0(ParsedJson::iterator* target) { return target->get_double(); }
EXPORTS(bool)                   iterator_is_object_or_array_0(ParsedJson::iterator* target) { return target->is_object_or_array(); }
EXPORTS(bool)                   iterator_is_object_0(ParsedJson::iterator* target) { return target->is_object(); }
EXPORTS(bool)                   iterator_is_array_0(ParsedJson::iterator* target) { return target->is_array(); }
EXPORTS(bool)                   iterator_is_string_0(ParsedJson::iterator* target) { return target->is_string(); }
EXPORTS(bool)                   iterator_is_integer_0(ParsedJson::iterator* target) { return target->is_integer(); }
EXPORTS(bool)                   iterator_is_double_0(ParsedJson::iterator* target) { return target->is_double(); }
EXPORTS(bool)                   iterator_is_true_0(ParsedJson::iterator* target) { return target->is_true(); }
EXPORTS(bool)                   iterator_is_false_0(ParsedJson::iterator* target) { return target->is_false(); }
EXPORTS(bool)                   iterator_is_null_0(ParsedJson::iterator* target) { return target->is_null(); }
EXPORTS(bool)                   iterator_is_object_or_array_u(uint8_t type) { return ParsedJson::iterator::is_object_or_array(type); }
EXPORTS(bool)                   iterator_move_to_key_c(ParsedJson::iterator* target, const char* key) { return target->move_to_key(key); }
EXPORTS(bool)                   iterator_next_0(ParsedJson::iterator* target) { return target->next(); }
EXPORTS(bool)                   iterator_prev_0(ParsedJson::iterator* target) { return target->prev(); }
EXPORTS(bool)                   iterator_up_0(ParsedJson::iterator* target) { return target->up(); }
EXPORTS(bool)                   iterator_down_0(ParsedJson::iterator* target) { return target->down(); }
EXPORTS(void)                   iterator_to_start_scope_0(ParsedJson::iterator* target) { target->to_start_scope(); }
EXPORTS(void)                   iterator__delete(ParsedJson::iterator* target) { delete target; }


/************* ParsedJson::iterator::scopeindex_t *************/

EXPORTS(void)                   scopeindex_t__delete(ParsedJson::iterator::scopeindex_t* target) { delete target; }


/************* Global functions: *************/

//NOT_BOUND:                    static void print_with_escapes(const unsigned char* src, basic_ostream& os)
//NOT_BOUND:                    static void print_with_escapes(const unsigned char* src, basic_ostream& os, size_t len)
//NOT_BOUND:                    static void print_with_escapes(const char* src, basic_ostream& os)
//NOT_BOUND:                    static void print_with_escapes(const char* src, basic_ostream& os, size_t len)
//NOT_BOUND:                    padded_string get_corpus(const const basic_string& filename)
//NOT_BOUND:                    static size_t jsonminify(const const basic_string_view& p, char* out)
//NOT_BOUND:                    void dumpbits_always(uint64_t v, const const basic_string& msg)
//NOT_BOUND:                    void dumpbits32_always(uint32_t v, const const basic_string& msg)
//NOT_BOUND:                    void init_state_machine()
//NOT_BOUND:                    int json_parse(const const basic_string& s, ParsedJson& pj)
//NOT_BOUND:                    ParsedJson build_parsed_json(const const basic_string& s)
EXPORTS(bool)                   _add_overflow_uuu(uint64_t value1, uint64_t value2, uint64_t* result) { return add_overflow(value1, value2, result); }
EXPORTS(bool)                   _mul_overflow_uuu(uint64_t value1, uint64_t value2, uint64_t* result) { return mul_overflow(value1, value2, result); }
EXPORTS(int)                    _trailingzeroes_u(uint64_t input_num) { return trailingzeroes(input_num); }
EXPORTS(int)                    _leadingzeroes_u(uint64_t input_num) { return leadingzeroes(input_num); }
EXPORTS(int)                    _hamming_u(uint64_t input_num) { return hamming(input_num); }
EXPORTS(void*)                  _aligned_malloc_ss(size_t alignment, size_t size) { return aligned_malloc(alignment, size); }
EXPORTS(char*)                  _aligned_malloc_char_ss(size_t alignment, size_t size) { return aligned_malloc_char(alignment, size); }
EXPORTS(void)                   _aligned_free_v(void* memblock) { aligned_free(memblock); }
EXPORTS(void)                   _aligned_free_char_c(char* memblock) { aligned_free_char(memblock); }
EXPORTS(char*)                  _allocate_padded_buffer_s(size_t length) { return allocate_padded_buffer(length); }
EXPORTS(uint32_t)               _is_not_structural_or_whitespace_u(uint8_t c) { return is_not_structural_or_whitespace(c); }
EXPORTS(uint32_t)               _is_structural_or_whitespace_u(uint8_t c) { return is_structural_or_whitespace(c); }
EXPORTS(uint32_t)               _hex_to_u32_nocheck_u(const uint8_t* src) { return hex_to_u32_nocheck(src); }
EXPORTS(size_t)                 _codepoint_to_utf8_uu(uint32_t cp, uint8_t* c) { return codepoint_to_utf8(cp, c); }
EXPORTS(void)                   _print_with_escapes_u(const unsigned char* src) { print_with_escapes(src); }
EXPORTS(void)                   _print_with_escapes_us(const unsigned char* src, size_t len) { print_with_escapes(src, len); }
EXPORTS(size_t)                 _jsonminify_usu(const uint8_t* buf, size_t len, uint8_t* out) { return jsonminify(buf, len, out); }
EXPORTS(size_t)                 _jsonminify_csc(const char* buf, size_t len, char* out) { return jsonminify(buf, len, out); }
EXPORTS(size_t)                 _jsonminify_pc(const padded_string& p, char* out) { return jsonminify(p, out); }
EXPORTS(bool)                   _find_structural_bits_usP(const uint8_t* buf, size_t len, ParsedJson& pj) { return find_structural_bits(buf, len, pj); }
EXPORTS(bool)                   _find_structural_bits_csP(const char* buf, size_t len, ParsedJson& pj) { return find_structural_bits(buf, len, pj); }
EXPORTS(bool)                   _handle_unicode_codepoint_uu(const uint8_t** src_ptr, uint8_t** dst_ptr) { return handle_unicode_codepoint(src_ptr, dst_ptr); }
EXPORTS(bool)                   _parse_string_usPuu(const uint8_t* buf, size_t len, ParsedJson& pj, const uint32_t depth, uint32_t offset) { return parse_string(buf, len, pj, depth, offset); }
EXPORTS(bool)                   _is_integer_c(char c) { return is_integer(c); }
EXPORTS(bool)                   _is_not_structural_or_whitespace_or_exponent_or_decimal_or_null_u(unsigned char c) { return is_not_structural_or_whitespace_or_exponent_or_decimal_or_null(c); }
EXPORTS(bool)                   _parse_float_uPub(const const uint8_t* buf, ParsedJson& pj, const uint32_t offset, bool found_minus) { return parse_float(buf, pj, offset, found_minus); }
EXPORTS(bool)                   _parse_large_integer_uPub(const const uint8_t* buf, ParsedJson& pj, const uint32_t offset, bool found_minus) { return parse_large_integer(buf, pj, offset, found_minus); }
EXPORTS(bool)                   _parse_number_uPub(const const uint8_t* buf, ParsedJson& pj, const uint32_t offset, bool found_minus) { return parse_number(buf, pj, offset, found_minus); }
EXPORTS(int)                    _unified_machine_usP(const uint8_t* buf, size_t len, ParsedJson& pj) { return unified_machine(buf, len, pj); }
EXPORTS(int)                    _unified_machine_csP(const char* buf, size_t len, ParsedJson& pj) { return unified_machine(buf, len, pj); }
EXPORTS(int)                    _json_parse_usPb(const uint8_t* buf, size_t len, ParsedJson& pj, bool reallocifneeded) { return json_parse(buf, len, pj, reallocifneeded); }
EXPORTS(int)                    _json_parse_csPb(const char* buf, size_t len, ParsedJson& pj, bool reallocifneeded) { return json_parse(buf, len, pj, reallocifneeded); }
EXPORTS(int)                    _json_parse_pP(const padded_string& s, ParsedJson& pj) { return json_parse(s, pj); }
EXPORTS(ParsedJson)             _build_parsed_json_usb(const uint8_t* buf, size_t len, bool reallocifneeded) { return build_parsed_json(buf, len, reallocifneeded); }
EXPORTS(ParsedJson)             _build_parsed_json_csb(const char* buf, size_t len, bool reallocifneeded) { return build_parsed_json(buf, len, reallocifneeded); }
EXPORTS(ParsedJson)             _build_parsed_json_p(const padded_string& s) { return build_parsed_json(s); }
