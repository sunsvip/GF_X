using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz.Utils
{
    public static class ConfigUtil
    {

        public static bool ParseBool(string str)
        {
            switch (str.ToLowerInvariant())
            {
                case "1":
                case "true": return true;
                case "0":
                case "false": return false;
                default: throw new Exception($"Invalid bool value {str}");
            }
        }

        public static bool? ParseNullableBool(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return null;
            }
            switch (str.ToLowerInvariant())
            {
                case "1":
                case "true": return true;
                case "0":
                case "false": return false;
                default: throw new Exception($"Invalid bool value {str}");
            }
        }

        public static int? ParseNullableInt(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return null;
            }
            return int.Parse(str);
        }

        public static long? ParseNullableLong(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return null;
            }
            return long.Parse(str);
        }

        public static float? ParseNullableFloat(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return null;
            }
            return float.Parse(str);
        }

        public static double? ParseNullableDouble(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return null;
            }
            return double.Parse(str);
        }
    }
}
