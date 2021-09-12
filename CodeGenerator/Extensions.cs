using System.Collections.Generic;

namespace AntlrCodeGenerator.CodeGenerator
{
    public static class Extensions
    {

        public static void TryAddOrUpdate(this Dictionary<string, Value> dict, string key, Value value)
        {
            if (dict.ContainsKey(key))
                dict[key] = value;
            else
                dict.Add(key, value);
        }
    
        public static string ToInt32(this string str)
        {
            if (str!=null && str.StartsWith("int"))
                return "int32";
            return str;
        }


    }
}