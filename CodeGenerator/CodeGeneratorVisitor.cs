using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using AntlrCodeGenerator.CodeGenerator;
using static CompileParser;

namespace AntlrCodeGenerator
{

    public class CodeGeneratorVisitor : CompileBaseVisitor<Value>
    {

        private CodeBuilder codeBuilder = new CodeBuilder();

        string main = "";
        string fn = "";
        string header = "";
        private Scope currentScope = new Scope();

        private List<Function> fns = new List<Function>();
        public CodeGeneratorVisitor()
        {
            codeBuilder.LoadInstructions(0, ".assembly extern mscorlib\n{\n}\n");
            codeBuilder.LoadInstructions(0, ".assembly " + "Program" + "\n{\n}\n\n.module " + "test" + ".exe\n");
            codeBuilder.LoadInstructions(0, ".class private auto ansi beforefieldinit Program extends [System.Runtime]System.Object ");
            codeBuilder.LoadInstructions(0, "{\n");
            codeBuilder.LoadInstructions(0, " .method private hidebysig static void  Main(string[] args) cil managed {");
            codeBuilder.LoadInstructions(2, " .entrypoint");
            codeBuilder.LoadInstructions(2, " .maxstack  8");
            header = codeBuilder.GetCode();
        }


        public override Value VisitParse([NotNull] ParseContext context)
        {

            var x = Visit(context.block());
            codeBuilder.LoadInstructions(2, main);
            codeBuilder.LoadInstructions(2, "ret \n}");

            codeBuilder.LoadInstructions(2, fn);

            var code = header + codeBuilder.GetCode() + "\n}";
            File.WriteAllText(@"out\test.il", code);
            return Value.VOID;
        }

        public override Value VisitPrintlnFunctionCall([NotNull] PrintlnFunctionCallContext context)
        {

            Visit(context.expression());

            if (context.GetChild(2).GetText().Contains("%d"))
            {
                codeBuilder.EmitInBuiltFunctionCall("int32");

            }
            else if (context.GetChild(2).GetText().Contains("%s"))
            {

                codeBuilder.EmitInBuiltFunctionCall("string");

            }








            return Value.VOID;
        }
        public override Value VisitBlock(CompileParser.BlockContext context)
        {

            foreach (var vdx in context.statement())
            {
                this.Visit(vdx);

            }

            foreach (var fdx in context.functionDecl())
            {
                this.Visit(fdx);

            }
            if (context.expression() != null)
            {
                var v = Visit(context.expression());
                return v;
            }
            return Value.VOID;
        }

        //visit assigment
        public override Value VisitAssignment(CompileParser.AssignmentContext context)
        {


            String varName = context.Identifier().GetText();
            codeBuilder.LoadInstructions(3, EmitLocals(varName));
            var variable = currentScope.Resolve(varName);
            var value = this.Visit(context.expression());
            currentScope.Assign(varName, value);
            codeBuilder.LoadInstructions(3, OpCodes.StLoc, varName);
            return new Value(value);


        }
        public override Value VisitIdentifierExpression(IdentifierExpressionContext ctx)
        {


            var identifier = ctx.Identifier().GetText();
            var variable = currentScope.Resolve(identifier);
            if (!variable.IsNumber())
            {
                variable = currentScope.Parent?.Resolve(identifier);
            }


            if (variable != null && currentScope.FunctionArguments.Contains(identifier))
            {
                if (!currentScope.Variables.ContainsKey(identifier))
                {
                    codeBuilder.LoadInstructions(2, OpCodes.LdInt4 , variable.ToString());
                }
                else
                {
                    codeBuilder.LoadInstructions(2, OpCodes.LdArg , ctx.Identifier().GetText());
                }
            }
            else
            {
                codeBuilder.LoadInstructions(2, OpCodes.LdLoc , ctx.Identifier().GetText());
            }

            return (variable == null || variable?.ToString() == "NULL") ? Value.VOID : new Value(variable);


        }


        //vist function declration
        public override Value VisitFunctionDecl(CompileParser.FunctionDeclContext context)
        {
            main += codeBuilder.GetCode();

            var functionScope = new Scope(currentScope, "function");
            currentScope = functionScope;

            var x = fns.FirstOrDefault(fn => fn.FnName == context.Identifier().GetText());
            var @params = context.idList() != null ? context.idList().Identifier() : new List<ITerminalNode>().ToArray();

            for (int i = 0; i < @params.Length; ++i)
            {
                currentScope.assignParam(@params[i].GetText(), new Value(x.Arguments[i].Value));
                currentScope.FunctionArguments.Add(@params[i].GetText());
            }
            codeBuilder.BuildMethod(x.Arguments.Select(x => x.Type).ToArray(),
            currentScope.FunctionArguments.Select(x => x).ToArray(), context.Identifier().GetText());

            codeBuilder.LoadInstructions(2, codeBuilder.EmitLocals(x.Arguments.Select(x => x.Type).ToArray()
             , currentScope.FunctionArguments.Select(x => x).ToArray())); ;
            Visit(context.block());
            codeBuilder.LoadInstructions(2, "ret");
            codeBuilder.LoadInstructions(2, "}");
            fn += codeBuilder.GetCode();

            currentScope = functionScope.Parent;
            functionScope = null;

            return Value.VOID;

        }

        //assignment

        public string EmitLocals(params string[] parameters)
        {
            string s = ".locals init ( ";
            for (int i = 0; i < parameters.Length; i++)
            {

                s += "int32 " + parameters[i];

                if (i < parameters.Length - 1)
                    s += ",";
            }

            return s + ")";
        }
        //visit function call
        public override Value VisitIdentifierFunctionCall(IdentifierFunctionCallContext ctx)
        {
            var function = new Function();
            function.FnName = ctx.Identifier().GetText();

            if (ctx.exprList() != null)
            {
                //fill the function call

                foreach (var vdx in ctx?.exprList()?.expression())
                {
                    var symbol = new Symbol();
                    var functionParameter = vdx.GetText();
                    if (currentScope.Resolve(functionParameter) != null)
                    {
                        symbol.Type = currentScope.Resolve(functionParameter).IsNumber() ? "int32" : "string";
                    }
                    else
                    {
                        symbol.Type = new Value(functionParameter).IsNumber() ? "int32" : "string";
                    }
                    symbol.Value = functionParameter;
                    function.Arguments.Add(symbol);

                }

            }
            var parameterList = string.Join(",", function?.Arguments.Select(x => x.Type).ToArray());

            if (ctx.exprList() != null)
            {

                Visit(ctx.exprList());

                codeBuilder.LoadInstructions(2, $"call void Program::{ctx.Identifier().GetText()}({parameterList})");
            }
            else
            {
                codeBuilder.LoadInstructions(2, $"call void Program::{ctx.Identifier().GetText()}()");
            }
            fns.Add(function);

            return Value.VOID;

        }
        //get input arguements
        public string GetFunctionArguments(IdentifierFunctionCallContext context)
        {
            string s = "";
            if (context.exprList() != null)
            {
                for (int i = 0; i < context.exprList().expression().Length; i++)
                {
                    s += "int32 ";
                    if (i < context.exprList().expression().Length - 1)

                        s += ",";

                }
            }
            return s;

        }

        //visit add expression

        public override Value VisitStringExpression([NotNull] StringExpressionContext context)
        {


            codeBuilder.LoadInstructions(2, "ldstr " , context.GetText());
            return new Value(context.GetText());
        }
        public override Value VisitAddExpression([NotNull] AddExpressionContext context)
        {

            //switch case on operator
            switch (context.op.Type)
            {
                case CompileParser.Add:
                    var left = this.Visit(context.expression(0));
                    var right = this.Visit(context.expression(1));
                    //if both are ints
                    if (left.IsNumber() && right.IsNumber())
                    {

                        codeBuilder.LoadInstructions(2, OpCodes.Add);


                        return new Value(left.AsInt() + right.AsInt());

                    }

                    if ((left.isString() || right.isString()) || (right.IsNumber() || left.IsNumber()))
                    {
                        if (left.IsNumber() || right.IsNumber())

                            codeBuilder.LoadInstructions(2, "call string [mscorlib]System.Int32::ToString()");
                        //append to string
                        codeBuilder.LoadInstructions(2, "call string string::Concat(string,string)");
                        return new Value(left.AsString() + right.AsString());


                    }
                    if (left.isString() && right.isString())
                    {

                        //append to string
                        codeBuilder.LoadInstructions(2, "call string string::Concat(string,string)");

                        return new Value(left.AsString() + right.AsString());

                    }

                    break;

                case CompileParser.Subtract:
                    var l = Visit(context.expression(0));
                    var r = Visit(context.expression(1));
                    if (l.IsNumber() && r.IsNumber())
                    {

                        codeBuilder.LoadInstructions(2, OpCodes.Sub);


                        return new Value(l.AsInt() + r.AsInt());

                    }
                    break;

                default:

                    break;
            }
            return Value.VOID;

        }

        public override Value VisitMultExpression([NotNull] MultExpressionContext context)
        {

            // //swithc case on op.Text
            switch (context.op.Text)
            {
                case "*":
                    var left = this.Visit(context.expression(0));
                    var right = this.Visit(context.expression(1));
                    //if both are ints
                    if (left.IsNumber() && right.IsNumber())
                    {

                        codeBuilder.LoadInstructions(2, OpCodes.Mul);


                        return new Value(left.AsInt() + right.AsInt());

                    }
                    break;
                case "/":
                    Visit(context.expression(0));
                    Visit(context.expression(1));
                    codeBuilder.LoadInstructions(2, OpCodes.Div);
                    break;
                case "%":
                    var l = Visit(context.expression(0));
                    var r = Visit(context.expression(1));
                    if (l.IsNumber() && r.IsNumber())
                    {

                        codeBuilder.LoadInstructions(2, OpCodes.Rem);

                        return new Value(l.AsInt() + r.AsInt());

                    }

                    break;
                default:
                    break;
            }


            return Value.VOID;

        }
        public override Value VisitExpressionExpression(CompileParser.ExpressionExpressionContext context)
        {
            Visit(context.expression());

            currentScope.Assign(context.expression().GetChild(0).GetText(), new Value(context.expression().GetChild(2).GetText()));


            return Value.VOID;

        }


        public override Value VisitNumberExpression(NumberExpressionContext ctx)
        {

            codeBuilder.LoadInstructions(2, OpCodes.LdInt4 , ctx.Number().GetText());

            return new Value(ctx.Number().GetText());

        }
        //visit for loop
        int _labelCount = 0;
        string labelPrev = "";

        private string MakeLabel(int label)
        {
            return string.Format("IL_{0:x4}", label);
        }
        public override Value VisitForStatement([NotNull] ForStatementContext context)
        {

            // var forScope = new Scope(currentScope, "for");
            // currentScope = forScope;
            Visit(context.Identifier());

            string varName = context.Identifier().GetText();
            System.Console.WriteLine(currentScope.Resolve(varName));
            string start = context.expression(0).GetText();
            currentScope.Assign(varName, new Value(start));

            codeBuilder.LoadInstructions(2, OpCodes.LdInt4 ,start);

            //emitlocal
            codeBuilder.LoadInstructions(2, EmitLocals(context.Identifier().GetText()));
            codeBuilder.InitializeVariable(varName, start);
            //load start value
            labelPrev = MakeLabel(_labelCount);
            _labelCount++;
            codeBuilder.LoadInstructions(0, OpCodes.Br ,labelPrev);
            string labelTo = MakeLabel(_labelCount);
            _labelCount++;
            _labelCount++;

            //labeto
            codeBuilder.LoadInstructions(0, labelTo + ":");
            codeBuilder.LoadInstructions(2, OpCodes.LdLoc + varName);
            codeBuilder.LoadInstructions(2, "ldc.i4 1 ");


            //statemtn
            Visit(context.block());
            codeBuilder.LoadInstructions(2, OpCodes.Add);
            codeBuilder.LoadInstructions(2, "stloc " , varName);

            codeBuilder.LoadInstructions(0, labelPrev + ":");
            codeBuilder.LoadInstructions(2, "ldloc " + varName);
            Visit(context.expression(1));

            //compare
            codeBuilder.LoadInstructions(2, "clt ");

            codeBuilder.LoadInstructions(0, "brtrue " + labelTo);


            return new Value("int");

        }

        public override Value VisitIfStatement([NotNull] IfStatementContext context)
        {



            //generate lable for if elseif* else
            string labelTo = MakeLabel(_labelCount);
            _labelCount++;
            string labelElse = MakeLabel(_labelCount);
            _labelCount++;
            string labelElseIf = MakeLabel(_labelCount);
            _labelCount++;
            string labelEnd = MakeLabel(_labelCount);
            _labelCount++;

            //emit if
            Visit(context.ifStat().expression());
            codeBuilder.LoadInstructions(0, "brfalse " , labelElseIf);
            Visit(context.ifStat().block());
            codeBuilder.LoadInstructions(2, "br " , labelEnd);
            //emit all child of elseif
            codeBuilder.LoadInstructions(0, labelElseIf + ":");
            foreach (var item in context.elseIfStat())
            {
                //check expression
                Visit(item.expression());
                codeBuilder.LoadInstructions(0, "brfalse " , labelElse);
                Visit(item.block());
                codeBuilder.LoadInstructions(2, "br " , labelEnd);

            }
            //emit else
            codeBuilder.LoadInstructions(0, labelElse + ":");
            if (context.elseStat() != null)
            {
                Visit(context.elseStat());
            }
            // //emit end
            codeBuilder.LoadInstructions(0, labelEnd + ":");

            return Value.VOID;


        }


        public override Value VisitCompExpression([NotNull] CompExpressionContext context)
        {
            //switch case
            if (context.op.Text == "==")
            {
                var l = Visit(context.expression(0));
                var r = Visit(context.expression(1));
                codeBuilder.LoadInstructions(2, OpCodes.Ceq);
                return new Value(l.AsInt() == r.AsInt());
            }
            if (context.op.Text == "!=")
            {
                Visit(context.expression(0));
                Visit(context.expression(1));
                codeBuilder.LoadInstructions(2, OpCodes.Ceq);
                codeBuilder.LoadInstructions(2, OpCodes.LdInt4 + "0");
                codeBuilder.LoadInstructions(2, OpCodes.Ceq);
            }
            if (context.op.Text == ">")
            {
                Visit(context.expression(0));
                Visit(context.expression(1));
                codeBuilder.LoadInstructions(2, OpCodes.Cgt);
            }
            if (context.op.Text == "<")
            {
                Visit(context.expression(0));
                Visit(context.expression(1));
                codeBuilder.LoadInstructions(2, OpCodes.Clt);
            }
            if (context.op.Text == ">=")
            {
                Visit(context.expression(0));
                Visit(context.expression(1));
                codeBuilder.LoadInstructions(2, OpCodes.Cgt_Un);
                codeBuilder.LoadInstructions(2, OpCodes.LdInt4 + "0");
                codeBuilder.LoadInstructions(2, OpCodes.Ceq);
            }
            return Value.VOID;

        }
        public override Value VisitEqExpression([NotNull] EqExpressionContext context)
        {

            if (context.op.Text == "==")
            {
                Visit(context.expression(0));
                Visit(context.expression(1));
                codeBuilder.LoadInstructions(2, OpCodes.Ceq);
            }
            if (context.op.Text == "!=")
            {
                Visit(context.expression(0));
                Visit(context.expression(1));
                codeBuilder.LoadInstructions(2, OpCodes.Ceq);
                codeBuilder.LoadInstructions(2, OpCodes.LdInt4 + "0");
                codeBuilder.LoadInstructions(2, OpCodes.Ceq);
            }
            return Value.VOID;
        }
        #region Helper
        public string[] GetParameters(List<ITerminalNode> parameters)
        {
            var arguments = new string[parameters.Count];
            for (int i = 0; i < parameters.Count(); i++)
            {
                arguments[i] = parameters[i].GetText();

            }
            return arguments;
        }
        private bool IsFunctionContext(ParserRuleContext context)
        {
            return context.Parent is FunctionDeclContext;
        }

        #endregion

    }
}