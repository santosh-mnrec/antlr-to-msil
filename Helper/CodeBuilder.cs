using System.Collections.Generic;
using System.Text;

namespace BLanguageMSILGenerator
{
    public class CodeBuilder
    {
        private readonly StringBuilder _result = new StringBuilder();
        public void Append(string code) => _result.Append(code);
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
        public Dictionary<string, string> DataTypes => _dataTypes;


        public void Init( )
        {
            LoadInstructions(1, ".assembly extern mscorlib\n{\n}\n");
            LoadInstructions(1, ".assembly " + "Program" + "\n{\n}\n\n.module " + "test" + ".exe\n");
            LoadInstructions(1, ".class private auto ansi beforefieldinit Program extends [System.Runtime]System.Object ");
            LoadInstructions(1, "{\n");
            LoadInstructions(1, ".method private hidebysig static void  Main(string[] args) cil managed {");
            LoadInstructions(1, ".entrypoint");
            LoadInstructions(1, ".maxstack  8");
            LoadInstructions(1, ".locals  init (class [System.Net.Http]System.Net.Http.HttpClient client)");
            LoadInstructions(1, ".locals init (class [mscorlib]System.Exception e)");
            LoadInstructions(1, ".try");
            LoadInstructions(1, "{");

        }

        public void EmitTry(string labelTo)
        {

            AppendCodeLine(2,"nop");
            AppendCodeLine(2,"leave.s " + labelTo);
            AppendCodeLine(1,"}");
        }

        public void EmitCatch(string labelTo)
        {

            AppendCodeLine(1, "catch [mscorlib]System.Exception");
            AppendCodeLine(1, "{");
            AppendCodeLine(3, "stloc.0");
            AppendCodeLine(3, $" nop");
            AppendCodeLine(3, "ldloc.0");
            AppendCodeLine(3, "callvirt instance string [mscorlib]System.Exception::get_Message()");
            AppendCodeLine(3, "call void [System.Console]System.Console::WriteLine(string)");
            AppendCodeLine(3, "nop");
            AppendCodeLine(3, "nop");
            AppendCodeLine(3, "leave.s " + labelTo);
            AppendCodeLine(3, "}");
            AppendCodeLine(3, $"{labelTo}: ret");
            AppendCodeLine(1, "}");

        }
        private void AppendCodeLine(int pos, string code)
        {

            for (int i = 0; i < pos; i++)
            {
                _result.Append("\t");
            }
            _result.AppendLine(code);
        }
        public void InitializeVariable(string variableName, string value)
        {
            Append("stloc " + variableName + "\n");
           // Append("ldc.i4 " + value + "\n");
          //  Append("stloc " + variableName + "\n");


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
        public void EmitInBuiltFunctionCall(string functionName,string type)
        {
            AppendCodeLine(2, $"call void [mscorlib]System.Console::{functionName}({type})");
        }
        public string GetCode( )
        {
            var result = _result.ToString();
            _result.Clear();
            return result;
        }


        public void BuildMethod(string[] types, string[] parameters, string methodName, string returnType = "void")
        {
            var methodBody = string.Empty;
            returnType = _dataTypes[returnType];
            methodBody += $".method private hidebysig static {returnType}  {methodName}(";
            for (int i = 0; i < types.Length; ++i)
            {
                methodBody += $"{types[i]} {parameters[i]}";
                if (i < parameters.Length - 1)
                {
                    methodBody += ",";
                }
            }
            methodBody += ") cil managed";
            AppendCodeLine(1, methodBody + "{");
        }

        public string EmitLocals(string[] types, params string[] parameters)
        {

            string localInit = ".locals init ( ";
            for (int i = 0; i < parameters.Length; i++)
            {
                var type = types[i];
                localInit += $"{type} {parameters[i]}";
                if (i < parameters.Length - 1)
                {
                    localInit += ",";
                }
            }

            return localInit + ")";
        }

        public string EmitLocals(string parameter, string type)
        {

            type = _dataTypes[type];
            var localInit = ".locals init ( ";
            localInit += type + " " + parameter;
            return localInit + ")";
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


