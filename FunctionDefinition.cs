using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime.Tree;

namespace AntlrCodeGenerator
{
    public class FunctionDefinition : CodeGeneratorVisitor
    {

        public Scope ParentScope { get; set; }
        private List<ITerminalNode> Parameters { get; set; }
        private IParseTree block;


        public FunctionDefinition(Scope parentScope, List<ITerminalNode> parameters, IParseTree block)
        {
            this.ParentScope = parentScope;
            this.Parameters = parameters;
            this.block = block;

        }

        public Value Invoke(List<Value> args, Dictionary<string, FunctionDefinition> functions, string funcName)
        {

            if (args.Count() != this.Parameters.Count())
            {
                throw new System.Exception("Illegal Function call");
            }
            Scope scopeNext = new Scope(ParentScope, true); // create function scope

            for (int i = 0; i < this.Parameters.Count(); i++)
            {
                var value = args[i];
                //function call

                scopeNext.AssignParameter(this.Parameters[i].GetText(), value);
            }


            //     CodeGeneratorVisitor._scope = scopeNext;
            //    // evalVistorNext._functions = functions;
          //  base.SetScope(scopeNext, functions);


            // var ret = Value.VOID;
            // try
            // {
            //  // base.Visit(this.block);
            // }
            // catch (ReturnValue returnValue)
            // {
            //     ret = returnValue.value;
            // }

            return ret;
        }


    }

    public static class LanguageExtension
    {

        public static void TryUpdate(this Dictionary<string, object> st, string key, object value)
        {
            //update dictionary if exits
            if (st.ContainsKey(key))
            {
                st[key] = st[key];
            }
            else
            {
                st.Add(key, value);
            }

        }

        public static int GetValue(this Dictionary<string, object> st, string key)
        {
            if (st.ContainsKey(key))
            {
                return (int)st[key];
            }
            else
            {
                return 0;
            }

        }
    }
}