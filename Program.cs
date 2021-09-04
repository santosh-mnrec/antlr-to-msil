using System;
using System.Collections.Generic;
using System.IO;
using Antlr4.StringTemplate;
using Antlr4.StringTemplate.Compiler;
using Antlr4.StringTemplate.Misc;
using Antlr4.Runtime;
using AntlrCodeGenerator.CodeGenerator;
using Antlr4.Runtime.Tree;
using Antlr4.Runtime.Misc;

namespace AntlrCodeGenerator
{
    public class Program
    {

        public static void Parse()
        {
            try
            {

                var text = File.ReadAllText(@"out\input.txt");
                var input = new AntlrInputStream(text);
                Lexer lexer = new CompileLexer(input);
                CommonTokenStream tokens = new CommonTokenStream(lexer);

                var parser = new CompileParser(tokens);
                parser.RemoveErrorListeners();

                parser.AddErrorListener(new SyntaxErrorListener());
                var tree = parser.parse();


                var codegen = new CodeGeneratorVisitor();
                var result = codegen.Visit(tree);

            }
            catch (ParseCanceledException e)
            {
                System.Console.WriteLine(e.Message);

            }

        }
        static void Main(string[] args)
        {

            Parse();

        }


    }
}