using Antlr4.Runtime.Tree;
using System.Collections.Generic;
using System.Linq;

namespace AntlrCodeGenerator.CodeGenerator
{
    public class Function
    {

        public string FnName {get;set;}

        public List<string> LocalVariables {get;set;}=new List<string>();
        public List<Symbol> Arguments {get;set;}=new List<Symbol>();
    }
}