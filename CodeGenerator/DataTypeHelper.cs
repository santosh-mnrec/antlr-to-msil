namespace AntlrCodeGenerator.CodeGenerator
{
    public static class DataTypeHelper
    {
        
        // is string is integer
        public static bool IsInteger(this string str)
        {
            int i;
            return int.TryParse(str, out i);
        }

        // is string is float
        public static bool IsFloat(this string str)
        {
            float f;
            return float.TryParse(str, out f);
        }

        // is string is double
        public static bool IsDouble(this string str)
        {
            double d;
            return double.TryParse(str, out d);
        }

        // is string is boolean
        public static bool IsBoolean(this string str)
        {
            bool b;
            return bool.TryParse(str, out b);
        }

        // is string is char
        public static bool IsChar(this string str)
        {
            char c;
            return char.TryParse(str, out c);
        }

        //is string
        public static bool IsString(this string str)
        {
            return str.StartsWith("\"") && str.EndsWith("\"");
        }
        

        
    }
}