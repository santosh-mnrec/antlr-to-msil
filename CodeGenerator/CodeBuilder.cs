using System.Text;

namespace AntlrCodeGenerator
{
    public class CodeBuilder
    {
        private StringBuilder _result = new StringBuilder();


        public void Append(string code) => _result.Append(code);
        private void AppendCode(string code) => _result.Append(code);

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
        public string GetCode()
        {

            var r = _result.ToString();
            _result.Clear();
            return r;
        }


        public void BuildMethod(string[] types, string[] parameters, string methodName,string returnType="void")
        {
            var s = string.Empty;
            if(returnType=="int"){
                returnType="int32";
            }
            s += $".method private hidebysig static {returnType}  {methodName}(";
            for (int i = 0; i < types.Length; ++i)
            {
                s += types[i] + " " + parameters[i];
                if (i < parameters.Length - 1)

                    s += ",";
            }
            s += ") cil managed";

            AppendCodeLine(2, s + "{");
        }

        public string EmitLocals(string[] types, params string[] parameters)
        {

            string s = ".locals init ( ";
            for (int i = 0; i < parameters.Length; i++)
            {

                var type = types[i];
                s += type + " " + parameters[i];

                if (i < parameters.Length - 1)
                    s += ",";
            }

            return s + ")";
        }
        //assignment

        public string EmitLocals(string parameter, string type)
        {
            string s = ".locals init ( ";

            s += type + " " + parameter;



            return s + ")";
        }



    }
}