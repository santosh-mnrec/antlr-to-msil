using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime.Tree;

namespace AntlrCodeGenerator.CodeGenerator
{
    public class Function
    {
        //name
        public string Name { get; set; }
        //list of parameters
        public Dictionary<string,Value> Parameters { get; set; }=new Dictionary<string, Value>();
        //return type
 

      
    }
}