using Antlr4.Runtime.Misc;

namespace AntlrCodeGenerator
{
    public class DemoListner:CompileBaseListener
    {
        
        public override void ExitIdentifierFunctionCall(CompileParser.IdentifierFunctionCallContext context){

            System.Console.WriteLine("Exiting");
        }

        public override void EnterIdentifierFunctionCall([NotNull] CompileParser.IdentifierFunctionCallContext context)
        {
           System.Console.WriteLine("done");
        }
    }
}