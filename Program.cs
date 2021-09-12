using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using AntlrCodeGenerator.CodeGenerator;
using System.IO;

namespace AntlrCodeGenerator
{
    public class Program
    {

        public static void Parse()
        {
            try
            {

                var text = File.ReadAllText(@"out\input.rs");
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

        private static void Main(string[] args)
        {

            Parse();

        }


    }
}