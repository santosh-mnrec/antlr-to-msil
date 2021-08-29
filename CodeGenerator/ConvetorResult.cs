using System;
using System.Text;

namespace AntlrCodeGenerator
{
    public class CodeBuilder
    {
        private StringBuilder _result = new StringBuilder();


        public void Append(string code) => _result.Append(code);
        private void AppendCode(string code) => _result.Append(code);

        public void AppendCodeLine(string code) => _result.Append(code+Environment.NewLine);
        public void InitializeVariable(string variableName, string value)
        {
            Append("stloc " + variableName + "\n");
            Append("ldc.i4 " + value + "\n");
            Append("stloc " + variableName + "\n");


        }
        public string GetCode()
        {

            var r = _result.ToString();
            _result.Clear();
            return r;
        }
    }
}