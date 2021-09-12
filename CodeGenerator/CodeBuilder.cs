using System.Collections.Generic;
using System.Text;

namespace AntlrCodeGenerator
{
    public class CodeBuilder
    {
        private readonly StringBuilder _result = new StringBuilder();


        public void Append(string code) => _result.Append(code);

        //data type lookup
        private Dictionary<string, string> _dataTypes = new Dictionary<string, string>(){

            {"int", "int32"},
            {"float", "float32"},
            {"double", "float64"},
            {"string", "string"},
            {"bool", "bool"},
            {"math","System.Random"},
            {"%d","int32"},
            {"%f","float32"},
            {"%s","string"}


        };

        //expose doctype
        public Dictionary<string, string> DataTypes => _dataTypes;


        public void Init( )
        {
            LoadInstructions(1, ".assembly extern mscorlib\n{\n}\n");
            LoadInstructions(1, ".assembly " + "Program" + "\n{\n}\n\n.module " + "test" + ".exe\n");
            LoadInstructions(1, ".class private auto ansi beforefieldinit Program extends [System.Runtime]System.Object ");
            LoadInstructions(1, "{\n");
            LoadInstructions(1, " .method private hidebysig static void  Main(string[] args) cil managed {");
            LoadInstructions(2, " .entrypoint");
            LoadInstructions(2, " .maxstack  8");
            LoadInstructions(2, " .locals  init (class [System.Net.Http]System.Net.Http.HttpClient client)");
            LoadInstructions(2, " .locals init (class [mscorlib]System.Exception e)");
            LoadInstructions(2, " .try");
            LoadInstructions(2, " {");



        }

        public void EmitTryCatch(string labelTo)
        {


            AppendCodeLine(2, "nop");
            AppendCodeLine(2, " leave.s " + labelTo);
            AppendCodeLine(2, "}");

        }

        public void EmitCatchIL(string labelTo)
        {



            AppendCodeLine(2, "catch [mscorlib]System.Exception");
            AppendCodeLine(2, "{");
            AppendCodeLine(2, "stloc.0");
            AppendCodeLine(2, $" nop");
            AppendCodeLine(2, "ldloc.0");
            AppendCodeLine(2, "callvirt instance string [mscorlib]System.Exception::get_Message()");
            AppendCodeLine(2, "call void [System.Console]System.Console::WriteLine(string)");
            AppendCodeLine(2, "nop");
            AppendCodeLine(2, "nop");
            AppendCodeLine(2, "leave.s " + labelTo);
            AppendCodeLine(2, "}");
            AppendCodeLine(2, $"{labelTo}: ret");
            AppendCodeLine(2, "}");

        }
        private void AppendCodeLine(int pos, string code)
        {

            for (int i = 0; i < pos; i++)
            {
                _result.Append(" ");
            }
            _result.AppendLine(code);
        }
        public void InitializeVariable(string variableName, string value)
        {
            Append("stloc " + variableName + "\n");
            Append("ldc.i4 " + value + "\n");
            Append("stloc " + variableName + "\n");


        }
        public string MakeLabel(int label)
        {
            return string.Format("IL_{0:x4}", label);
        }
        public void LoadInstructions(int space, string opcode, string valie)
        {
            AppendCodeLine(space, $"{opcode} {valie}");
        }
        public void LoadInstructions(int space, string valie)
        {
            AppendCodeLine(space, $"{valie}");
        }
        public void EmitInBuiltFunctionCall(string type)
        {
            AppendCodeLine(2, $"call void [mscorlib]System.Console::WriteLine({type})");
        }
        public string GetCode( )
        {

            var r = _result.ToString();
            _result.Clear();
            return r;
        }


        public void BuildMethod(string[] types, string[] parameters, string methodName, string returnType = "void")
        {
            var s = string.Empty;
            returnType = _dataTypes[returnType];

            s += $".method private hidebysig static {returnType}  {methodName}(";
            for (int i = 0; i < types.Length; ++i)
            {
                s += $"{types[i]} {parameters[i]}";
                if (i < parameters.Length - 1)
                {
                    s += ",";
                }
            }
            s += ") cil managed";

            AppendCodeLine(3, s + "{");
        }

        public string EmitLocals(string[] types, params string[] parameters)
        {

            string s = ".locals init ( ";
            for (int i = 0; i < parameters.Length; i++)
            {

                var type = types[i];
                s += $"{type} {parameters[i]}";

                if (i < parameters.Length - 1)
                {
                    s += ",";
                }
            }

            return s + ")";
        }


        public string EmitLocals(string parameter, string type)
        {

            type = _dataTypes[type];
            string s = ".locals init ( ";

            s += type + " " + parameter;



            return s + ")";
        }


        public void EmitHttpClientStart(string identifier)
        {
            AppendCodeLine(1, "nop");
            AppendCodeLine(1, $"ldloc {identifier}");

        }

        public void EmitHttpClientEnd(string identifier)
        {
            AppendCodeLine(2, "call string [System.IO.FileSystem]System.IO.File::ReadAllText(string)");
            AppendCodeLine(2, $"stloc {identifier}");
            AppendCodeLine(2, $"ldloc {identifier}");
            AppendCodeLine(2, "call void[System.Console] System.Console::WriteLine(string)");
            AppendCodeLine(2, "nop");
        }
    }
}


