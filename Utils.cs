using System;

namespace RequestResponseModule
{
    internal class Utils
    {
        public static bool IsJson(string value) {
            if(String.IsNullOrEmpty(value))
                return false;

            value = value.Trim();

            return ((value.StartsWith("{") && value.EndsWith("}")) || (value.StartsWith("[") && value.EndsWith("]")));
        }
    }
}
