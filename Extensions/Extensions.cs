using System.Collections.Generic;

namespace BLanguageMSILGenerator
{
    public static class Extensions
    {

        public static void TryAddOrUpdate(this Dictionary<string, Variable> dict, string key, Variable variable)
        {
            if (dict != null && dict.ContainsKey(key)){
                variable.Type=variable.GetDataType();
                dict[key] = variable;

            }
            else
                dict?.Add(key, variable);
        }

        public static string GetDataType(this Variable variable)
        {
            if (variable.IsNumber())
                return "int32";
            if (variable.IsFloat())
                return "float32";
            if (variable.IsString())
                return "string";
            return variable.Isboolean() ? "bool" : "null";
        }


    }
}