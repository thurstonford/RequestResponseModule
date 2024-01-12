using System;

namespace RequestResponseModule
{
    internal class Utils
    {
        /// <summary>
        /// Simple check to determine if the value is JSON.
        /// </summary>
        /// <param name="value">The string to check.</param>
        /// <returns>Boolean value indicating whether the input value is JSON.</returns>
        public static bool IsJson(string value) {
            if(String.IsNullOrEmpty(value))
                return false;

            value = value.Trim();

            return ((value.StartsWith("{") && value.EndsWith("}")) || (value.StartsWith("[") && value.EndsWith("]")));
        }
    }
}
