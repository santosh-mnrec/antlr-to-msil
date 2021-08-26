using System;

namespace AntlrCodeGenerator
{
    internal class ReturnValue:Exception
    {
        public Value value;

        public ReturnValue()
        {
        }
    }
}