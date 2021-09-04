using System.Collections.Generic;
using System.IO;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;

namespace AntlrCodeGenerator.CodeGenerator
{
    public readonly struct SyntaxError
    {
        public readonly IRecognizer Recognizer;
        public readonly IToken OffendingSymbol;
        public readonly int Line;
        public readonly int CharPositionInLine;
        public readonly string Message;
        public readonly RecognitionException Exception;

        public SyntaxError(IRecognizer recognizer, IToken offendingSymbol, int line,
                           int charPositionInLine, string message, RecognitionException exception)
        {
            Recognizer = recognizer;
            OffendingSymbol = offendingSymbol;
            Line = line;
            CharPositionInLine = charPositionInLine;
            Message = message;
            Exception = exception;
        }
    }

    public class SyntaxErrorListener : BaseErrorListener
    {
        public readonly List<SyntaxError> Errors = new List<SyntaxError>();


        public override void SyntaxError(TextWriter writer, [NotNull] IRecognizer recognizer, [Nullable] IToken offendingSymbol, int line, int charPositionInLine, [NotNull] string msg, [Nullable] RecognitionException e)
        {
            Errors.Add(new SyntaxError(recognizer, offendingSymbol, line, charPositionInLine, msg, e));

            throw new ParseCanceledException(Errors[0].Message);


        }



    }
    public class ExceptionErrorStrategy : DefaultErrorStrategy
    {
        public override void Recover(Parser recognizer, RecognitionException e)
        {
            throw new ParseCanceledException(e.Message);
        }

        protected override void ReportInputMismatch(Parser recognizer, InputMismatchException e)
        {
            throw new ParseCanceledException(e.Message);
        }

        protected override void ReportMissingToken(Parser recognizer)
        {
            BeginErrorCondition(recognizer);

            var token = recognizer.CurrentToken;
            var expected = new string[recognizer.GetExpectedTokens().Count];

            var msg = "missing " + string.Join(", ", expected);
            recognizer.NotifyErrorListeners(token, msg, null);
        }

    }
}