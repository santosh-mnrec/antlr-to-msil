
using System;
using System.IO;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;

namespace BLanguageMSILGenerator
{

    /// <summary>
    /// This class is used to generate the code for the ErrorListener.
    /// </summary>
    //SOURCE: https://stackoverflow.com/questions/18132078/handling-errors-in-antlr4
    public class DescriptiveErrorListener : BaseErrorListener,IAntlrErrorListener<int>
    {
        public static IAntlrErrorListener<IToken> Instance { get; } = new DescriptiveErrorListener();

        public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
        {
            if (!REPORT_SYNTAX_ERRORS) return;
            string sourceName = recognizer.InputStream.SourceName;
            sourceName = $"{sourceName}:{line}:{charPositionInLine}";
            Console.Error.WriteLine($"{sourceName}: line {line}:{charPositionInLine} {msg}");
        }
        public override void SyntaxError(TextWriter output, IRecognizer recognizer, [Nullable] IToken offendingSymbol, int line, int charPositionInLine, string msg, RecognitionException e)
        {
            this.SyntaxError(output, recognizer, 0, line, charPositionInLine, msg, e);
        }
        static readonly bool REPORT_SYNTAX_ERRORS = true;
    }

}