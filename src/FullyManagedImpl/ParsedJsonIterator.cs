// This file is a manual port of C code https://github.com/lemire/simdjson to C#
// (c) Daniel Lemire and Geoff Langdale

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

using static SimdJsonSharp.Utils;

#region stdint types and friends
using size_t = System.UInt64;
using char1 = System.SByte;
using int64_t = System.Int64;
using uint8_t = System.Byte;
using uint32_t = System.UInt32;
using uint64_t = System.UInt64;
#endregion


namespace SimdJsonSharp
{
    public unsafe struct ParsedJsonIterator : IDisposable
    {
        ParsedJson pj;
        size_t depth;
        size_t location;     // our current location on a tape
        size_t tape_length;
        uint8_t current_type;
        uint64_t current_val;
        scopeindex_t* depthindex;

        public ParsedJson ParsedJson => pj;

        public ParsedJsonIterator(ParsedJson parsedJson)
        {
            pj = parsedJson;
            depth = 0;
            location = 0;
            tape_length = 0;
            depthindex = allocate<scopeindex_t>(pj.depthcapacity);
            current_type = 0;
            current_val = 0;

            depthindex[0].start_of_scope = location;
            current_val = pj.tape[location++];
            current_type = (uint8_t)(current_val >> 56);
            depthindex[0].scope_type = current_type;
            if (current_type == 'r')
            {
                tape_length = current_val & JSONVALUEMASK;
                if (location < tape_length)
                {
                    current_val = pj.tape[location];
                    current_type = (uint8_t)(current_val >> 56);
                    depth++;
                    depthindex[depth].start_of_scope = location;
                    depthindex[depth].scope_type = current_type;
                }
            }
            else
            {
                throw new InvalidOperationException("Json is invalid");
            }
        }

        public bool IsOk => location < tape_length;

        // useful for debuging purposes
        public size_t TapeLocation => location;

        // useful for debuging purposes
        public size_t TapeLength => tape_length;

        // returns the current depth (start at 1 with 0 reserved for the fictitious root node)
        public size_t Depth => depth;

        // A scope is a series of nodes at the same depth, typically it is either an object ({) or an array ([).
        // The root node has type 'r'.
        public uint8_t GetScopeType() => depthindex[depth].scope_type;

        // move forward in document order
        public bool MoveForward()
        {
            if (location + 1 >= tape_length)
            {
                return false; // we are at the end!
            }

            if ((current_type == '[') || (current_type == '{'))
            {
                // We are entering a new scope
                depth++;
                depthindex[depth].start_of_scope = location;
                depthindex[depth].scope_type = current_type;
            }
            else if ((current_type == ']') || (current_type == '}'))
            {
                // Leaving a scope.
                depth--;
            }
            else if ((current_type == 'd') || (current_type == 'l'))
            {
                // d and l types use 2 locations on the tape, not just one.
                location += 1;
            }

            location += 1;
            current_val = pj.tape[location];
            current_type = (uint8_t)(current_val >> 56);
            return true;
        }

        public void MoveToValue()
        {
            // assume that we are on a key, so move by 1.
            location += 1;
            current_val = pj.tape[location];
            current_type = (uint8_t)(current_val >> 56);
        }

        public bool MoveToKey(char1* key) {
            if(Down()) {
                do {
                    Debug.Assert(IsString);
                    bool rightkey = (strcmp(GetUtf8String(), key) == 0);// null chars would fool this
                    MoveToValue();
                    if(rightkey) { 
                        return true;
                    }
                } while(Next());
                Debug.Assert(Up());// not found
            }
            return false;
        }

        public bool MoveToKey(char1* key, uint32_t length)
        {
            if (Down())
            {
                do
                {
                    Debug.Assert(IsString);
                    bool rightkey = ((GetUtf8StringLength() == length) && (!memcmp(GetUtf8String(), key, length)));
                    MoveToValue();
                    if (rightkey)
                    {
                        return true;
                    }
                } while (Next());
                Debug.Assert(Up());// not found
            }
            return false;
        }

        public bool MoveToIndex(uint32_t index)
        {
            Debug.Assert(IsArray);
            if (Down())
            {
                uint32_t i = 0;
                for (; i < index; i++)
                {
                    if (!Next())
                    {
                        break;
                    }
                }
                if (i == index)
                {
                    return true;
                }
                Debug.Assert(Up());
            }
            return false;
        }

        public uint8_t CurrentType => current_type;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // get the int64_t value at this node; valid only if we're at "l"
        public int64_t GetInteger()
        {
            if (location + 1 >= tape_length) return 0; // default value in case of error
            return (int64_t)pj.tape[location + 1];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // get the double value at this node; valid only if
        // we're at "d"
        public double GetDouble()
        {
            if (location + 1 >= tape_length) return double.NaN; // default value in case of error
            double answer;
            Utils.memcpy(&answer, &pj.tape[location + 1], sizeof(double));
            return answer;
        }

        public bool IsObjectOrArray => current_type == '[' || current_type == '{' ? true : false; //hehe...

        public bool IsObject => current_type == '{';

        public bool IsArray => current_type == '[';

        public bool IsString => current_type == '"';

        public bool IsInteger => current_type == 'l';

        public bool IsDouble => current_type == 'd';


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // get the string value at this node (NULL ended); valid only if we're at "
        // note that tabs, and line endings are escaped in the returned value (see print_with_escapes)
        // return value is valid UTF-8
        public char1* GetUtf8String()
        {
            return (char1*)(pj.string_buf + (current_val & JSONVALUEMASK) + sizeof(uint32_t));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint32_t GetUtf8StringLength()
        {
            uint32_t answer;
            memcpy(&answer, (char1*)(pj.string_buf + (current_val & JSONVALUEMASK)), sizeof(uint32_t));
            return answer;
        }

        internal static readonly UTF8Encoding s_utf8Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetUtf16String()
        {
            return s_utf8Encoding.GetString((byte*)GetUtf8String(), (int)GetUtf8StringLength());
        }

        // throughout return true if we can do the navigation, false
        // otherwise
        // Withing a given scope (series of nodes at the same depth within either an
        // array or an object), we move forward.
        // Thus, given [true, null, {"a":1}, [1,2]], we would visit true, null, { and [.
        // At the object ({) or at the array ([), you can issue a "down" to visit their content.
        // valid if we're not at the end of a scope (returns true).
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Next()
        {
            size_t npos;
            if ((current_type == (uint8_t)'[') || (current_type == (uint8_t)'{'))
            {
                // we need to jump
                npos = (current_val & JSONVALUEMASK);
            }
            else
            {
                npos = (size_t)(location + (size_t)((current_type == (uint8_t)'d' || current_type == (uint8_t)'l') ? 2 : 1));
            }
            uint64_t nextval = pj.tape[npos];
            uint8_t nexttype = (uint8_t)(nextval >> 56);
            if ((nexttype == (uint8_t)']') || (nexttype == (uint8_t)'}'))
            {
                return false; // we reached the end of the scope
            }
            location = npos;
            current_val = nextval;
            current_type = nexttype;
            return true;
        }

        // Withing a given scope (series of nodes at the same depth within either an
        // array or an object), we move backward.
        // Thus, given [true, null, {"a":1}, [1,2]], we would visit ], }, null, true when starting at the end
        // of the scope.
        // At the object ({) or at the array ([), you can issue a "down" to visit their content.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Prev()
        {
            if (location - 1 < depthindex[depth].start_of_scope)
            {
                return false;
            }
            location -= 1;
            current_val = pj.tape[location];
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
                current_val = pj.tape[location];
                current_type = (uint8_t)(current_val >> 56);
            }
            return true;
        }

        // Moves back to either the containing array or object (type { or [) from
        // within a contained scope.
        // Valid unless we are at the first level of the document
        //
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Up()
        {
            if (depth == 1)
            {
                return false; // don't allow moving back to root
            }
            ToStartScope();
            // next we just move to the previous value
            depth--;
            location -= 1;
            current_val = pj.tape[location];
            current_type = (uint8_t)(current_val >> 56);
            return true;
        }

        // Valid if we're at a [ or { and it starts a non-empty scope; moves us to start of
        // that deeper scope if it not empty.
        // Thus, given [true, null, {"a":1}, [1,2]], if we are at the { node, we would move to the
        // "a" node.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Down()
        {
            if (location + 1 >= tape_length)
            {
                return false;
            }
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
                current_val = pj.tape[location];
                current_type = (uint8_t)(current_val >> 56);
                return true;
            }
            return false;
        }

        // move us to the start of our current scope,
        // a scope is a series of nodes at the same level
        public void ToStartScope()
        {
            location = depthindex[depth].start_of_scope;
            current_val = pj.tape[location];
            current_type = (uint8_t)(current_val >> 56);
        }

        public void Dispose() => Dispose(true);

        private void Dispose(bool disposing)
        {
            if (disposing)
                GC.SuppressFinalize(this);

            delete(depthindex);
            depthindex = null;
        }

        /// <summary>
        /// Convert byte get_type() int System.Text.Json.JsonTokenType
        /// </summary>
        public JsonTokenType GetTokenType()
        {
            if (current_type == (byte)'{')
                return JsonTokenType.StartObject;
            if (current_type == (byte)'}')
                return JsonTokenType.EndObject;
            if (current_type == (byte)'l' || current_type == (byte)'d')
                return JsonTokenType.Number;
            if (current_type == (byte)'"')
                return JsonTokenType.String;
            if (current_type == (byte)'[')
                return JsonTokenType.StartArray;
            if (current_type == (byte)']')
                return JsonTokenType.EndArray;
            if (current_type == (byte)'n')
                return JsonTokenType.Null;
            if (current_type == (byte)'t')
                return JsonTokenType.True;
            if (current_type == (byte)'f')
                return JsonTokenType.False;
            return JsonTokenType.None;
        }
    }
}
