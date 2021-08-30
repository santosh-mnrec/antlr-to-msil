using System;
using System.Runtime.Serialization;

namespace AntlrCodeGenerator.CodeGenerator
{
    [Serializable]
    internal class ReturnValue : Exception
    {
        public ReturnValue()
        {
        }

      

        public Value value { get; internal set; }
    }
}