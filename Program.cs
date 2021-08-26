using System;
using System.Collections.Generic;
using System.IO;
using Antlr4.StringTemplate;
using Antlr4.StringTemplate.Compiler;
using Antlr4.StringTemplate.Misc;
using Antlr4.Runtime;
namespace AntlrCodeGenerator
{
    public class Program
    {
        static void Main(string[] args)
        {

            var text = File.ReadAllText(@"out\input.txt");
            var input = new AntlrInputStream(text);
            Lexer lexer = new CompileLexer(input);
            CommonTokenStream tokens = new CommonTokenStream(lexer);

            var parser = new CompileParser(tokens);

            var tree = parser.parse();
            Scope scope = new Scope();
            var functions = new Dictionary<string, FunctionDefinition>();


            var codegen = new CodeGeneratorVisitor();
            var x = codegen.Visit(tree);

            var model = new { fn = "santosh" };
            // var x1 = File.ReadAllText(@"Demo.st");
            // var template = new Antlr4.StringTemplate.Template(x1, '$', '$');
        
            // template.Add("func","a");
           //var x2 = template.Render();
          

        }


    }
}