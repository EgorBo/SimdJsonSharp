using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace SimdJsonSharp
{
    public static unsafe class Extensions
    {
        /// <summary>
        /// Convert byte get_type() int System.Text.Json.JsonTokenType
        /// </summary>
        public static JsonTokenType GetTokenType(this in iterator iterator)
        {
            switch (iterator.get_type())
            {
                case (byte)'d': // 'd' for floats
                case (byte)'l':
                    return JsonTokenType.Number;
                case (byte)'n':
                    return JsonTokenType.Null;
                case (byte)'t':
                    return JsonTokenType.True;
                case (byte)'f':
                    return JsonTokenType.False;
                case (byte)'{':
                    return JsonTokenType.StartObject;
                case (byte)'}':
                    return JsonTokenType.EndObject;
                case (byte)'[':
                    return JsonTokenType.StartArray;
                case (byte)']':
                    return JsonTokenType.EndArray;
                case (byte)'"':
                    return JsonTokenType.String;
                default:
                    return JsonTokenType.None;
            }
        }

        public static string GetUtf16String(this in iterator iterator)
        {
            return new string(iterator.get_string());
        }
    }
}
