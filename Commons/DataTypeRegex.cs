using System.Text.RegularExpressions;

namespace System.DJ.DJson.Commons
{
    class DataTypeRegex
    {
        public static Regex rgInt = null;
        public static Regex rgFloat = null;
        public static Regex rgBool = null;
        public static Regex rgDateTime = null;
        public static Regex rgGuid = null;

        static DataTypeRegex()
        {
            string s = "";
            s = @"(^([0-9]+)$)|(^[\+\-]([0-9]+)$)";
            rgInt = new Regex(s, RegexOptions.IgnoreCase);

            s = @"(^([0-9]+)\.([0-9]+)$)|(^[\+\-][0-9]+\.([0-9]+)$)";
            rgFloat = new Regex(s, RegexOptions.IgnoreCase);

            s = @"(^true$)|(^false$)";
            rgBool = new Regex(s, RegexOptions.IgnoreCase);

            s = @"(^([0-9]{4})[\-\/][0-9]{1,2}[\-\/][0-9]{1,2}\s+[0-9]{1,2}\:[0-9]{1,2}\:([0-9]{1,2})$)|";
            s += @"(^([0-9]{4})[\-\/][0-9]{1,2}[\-\/]([0-9]{1,2})$)|(^([0-9]{1,2})\:[0-9]{1,2}\:([0-9]{1,2})$)";
            rgDateTime = new Regex(s, RegexOptions.IgnoreCase);

            s = @"^([0-9a-z]+)\-[0-9a-z]+\-[0-9a-z]+\-[0-9a-z]+\-([0-9a-z]+)$";
            rgGuid = new Regex(s, RegexOptions.IgnoreCase);
        }

        public static bool isInt(string txt)
        {
            if (string.IsNullOrEmpty(txt)) return false;
            return rgInt.IsMatch(txt);
        }

        public static bool isFloat(string txt)
        {
            if (string.IsNullOrEmpty(txt)) return false;
            return rgFloat.IsMatch(txt);
        }

        public static bool isBool(string txt)
        {
            if (string.IsNullOrEmpty(txt)) return false;
            return rgBool.IsMatch(txt);
        }

        public static bool isDataTime(string txt)
        {
            if (string.IsNullOrEmpty(txt)) return false;
            return rgDateTime.IsMatch(txt);
        }

        public static bool isGuid(string txt)
        {
            if (string.IsNullOrEmpty(txt)) return false;
            return rgGuid.IsMatch(txt);
        }
    }
}
