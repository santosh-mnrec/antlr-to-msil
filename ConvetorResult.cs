using System.Text;

namespace AntlrCodeGenerator
{
    public class ConverterResult
    {
        private StringBuilder _result = new StringBuilder();

        /// <summary>
        /// Appends specified code to the result
        /// </summary>
        /// <param name="code">JavaScript source code</param>
        public void Append(string code) => _result.Append(code);

        /// <summary>
        /// Returns accumulated source code
        /// </summary>
        /// <returns></returns>
        public string GetCode(){

                var r=_result.ToString();
                _result.Clear();
                return r;
        }
    }
}