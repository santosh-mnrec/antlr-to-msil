using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime.Tree;

namespace AntlrCodeGenerator.CodeGenerator
{
    public class Function
    {

        private Scope _parentScope;
        private List<ITerminalNode> _params;
        private IParseTree _block;

       public Function(Scope parentScope, List<ITerminalNode> @params, IParseTree block)
        {
            ParentScope = parentScope;
            Params = @params;
            Block = block;
        }

        public Scope ParentScope { get => _parentScope; set => _parentScope = value; }
        public List<ITerminalNode> Params { get => _params; set => _params = value; }
        public IParseTree Block { get => _block; set => _block = value; }

        public Value invoke(List<Value> args, Dictionary<string, Function> functions)
        {
            if (args.Count() != Params.Count())
            {
                throw new System.Exception("Illegal Function call");
            }
            Scope scopeNext = new Scope(ParentScope, true); // create function scope

            for (int i = 0; i < Params.Count(); i++)
            {
                var value = args[i];
                scopeNext.AssignParameter(Params[i].GetText(), value);
            }
            // EvalVisitor evalVistorNext = new EvalVisitor(scopeNext,functions);

            // var ret = Value.VOID;
            // try {
            // 	evalVistorNext.visit(this.block);
            // } catch (ReturnValue returnValue) {
            // 	ret = returnValue.value;
            // }
            return Value.VOID;
        }
    }
}