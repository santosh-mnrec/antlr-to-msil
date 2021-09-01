using System.Collections.Generic;

namespace AntlrCodeGenerator.CodeGenerator
{
    public static class Extensions
    {

        public static void TryAddOrUpdate(this Dictionary<string, Value> dict, string key, Value value){
            if (dict.ContainsKey(key))
                dict[key] = value;
            else
                dict.Add(key, value);
        }
        
        
    }
}