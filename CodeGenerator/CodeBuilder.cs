using System;
using System.Text;

namespace AntlrCodeGenerator
{
    public class CodeBuilder
    {
        private StringBuilder _result = new StringBuilder();


        public void Append(string code) => _result.Append(code);
        private void AppendCode(string code) => _result.Append(code);

        public void AppendCodeLine(int pos, string code)
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
        public void LoadIntegerToStack(string name, Value value)
        {
            AppendCodeLine(2, "ldc.i4 " + value);
            AppendCodeLine(2, "stloc " + name);

        }
        public void LoadToLocal(string value)
        {
            AppendCodeLine(2, "stloc " + value);
            AppendCodeLine(2, "ldloc " + value);


        }
    }
}