namespace AntlrCodeGenerator.CodeGenerator
{
    public class OpCodes
    {
        public static string Add="add ";
        public static string And="and ";
        public static string Br="br ";
        public static string Brfalse="bne.un.s ";
        public static string Brtrue="brtrue ";
        public static string Call="call ";
        public static string Callvirt="callvirt ";
        public static string LdArg="ldarg ";
        public static string StLoc="stloc ";
        public static string LdLoc="ldloc ";
        public static string LdInt4="ldc.i4 ";

        public static string Sub { get; internal set; }
        public static string Mul { get; internal set; }
        public static string Div { get; internal set; }
    }

}

