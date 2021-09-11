using Antlr4.Runtime.Tree;
using System.Collections.Generic;
using System.Linq;

namespace AntlrCodeGenerator.CodeGenerator
{
    public class Function
    {

        public string Name { get; set; }

        public List<Value> Arguments { get; set; } = new List<Value>();

    }
}