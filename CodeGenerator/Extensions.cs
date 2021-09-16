using System;
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

        public static string GetDataType(this Value value)
        {
            
            //get data type from object value dictionary and action
           

            if (value.IsNumber())
                return "int32";
            if (value.ToFloat())
                return "float32";
            if (value.IsString())
                return "string";
            if (value.Asbool())
                return "bool";

            return "null";

        }


    }
}