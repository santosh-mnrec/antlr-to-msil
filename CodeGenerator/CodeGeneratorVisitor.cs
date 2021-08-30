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




        private CodeBuilder _result = new CodeBuilder();

        public SymbolTable variableDefs = new SymbolTable();

        string main = "";
        string fn = "";
        string header = "";
        private Scope currentScope = new Scope();
        private Stack<string> printOrder = new Stack<string>();
        private List<string> functionArguments = new List<string>();
        public CodeGeneratorVisitor()
        {
            _result.AppendCodeLine(0, ".assembly extern mscorlib\n{\n}\n");
            _result.AppendCodeLine(0, ".assembly " + "Program" + "\n{\n}\n\n.module " + "test" + ".exe\n");
            _result.AppendCodeLine(0, ".class private auto ansi beforefieldinit Program extends [System.Runtime]System.Object ");
            _result.AppendCodeLine(0, "{\n");
            _result.AppendCodeLine(0, " .method private hidebysig static void  Main(string[] args) cil managed {");
            _result.AppendCodeLine(2, " .entrypoint");
            _result.AppendCodeLine(2, " .maxstack  8");
            header = _result.GetCode();
        }


        public override Value VisitParse([NotNull] ParseContext context)
        {

            Visit(context.block());
            _result.AppendCodeLine(2, main);
            _result.AppendCodeLine(2, "ret \n}");

            _result.AppendCodeLine(2, fn);

            var code = header + _result.GetCode() + "\n}";
            File.WriteAllText(@"out\test.il", code);
            return Value.VOID;
        }

        public override Value VisitPrintlnFunctionCall([NotNull] PrintlnFunctionCallContext context)
        {

            Visit(context.expression());

            if (context.GetChild(2).GetText().Contains("%d"))
            {
                _result.EmitInBuiltFunctionCall("int32");

            }
            else if (context.GetChild(2).GetText().Contains("%s"))
            {

                _result.EmitInBuiltFunctionCall("string");

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
                Visit(context.expression());
            return Value.VOID;
        }

        //visit assigment
        public override Value VisitAssignment(CompileParser.AssignmentContext context)
        {


            String varName = context.Identifier().GetText();
            _result.AppendCodeLine(3, EmitLocals(varName));
            var variable = currentScope.Resolve(varName);
            var value = this.Visit(context.expression());
            if (variable == null)
            {
                currentScope.Assign(varName, value);
            }
            _result.AppendCodeLine(3, OpCodes.StLoc + varName);
            return Value.VOID;


        }
        public override Value VisitIdentifierExpression(IdentifierExpressionContext ctx)
        {


            var identifier = ctx.Identifier().GetText();
            var variable = currentScope.Resolve(identifier);


            if (currentScope.IsScope("function") && variable.ToString() != "NULL")
            {
                if (!currentScope._variables.ContainsKey(identifier))
                {
                    _result.AppendCodeLine(2, OpCodes.LdInt4 + variable);
                }
                else
                {
                    _result.AppendCodeLine(2, OpCodes.LdArg + ctx.Identifier().GetText());
                }
            }
            else
            {
                _result.AppendCodeLine(2, OpCodes.LdLoc + ctx.Identifier().GetText());
            }

            return (variable == null || variable?.ToString() == "NULL") ? Value.VOID : new Value(variable);


        }


        //vist function declration
        public override Value VisitFunctionDecl(CompileParser.FunctionDeclContext context)
        {
            main += _result.GetCode();

            var functionScope = new Scope(currentScope, "function");
            currentScope = functionScope;

            var @params = context.idList() != null ? context.idList().Identifier() : new List<ITerminalNode>().ToArray();
            string s = "";
            s += ".method private hidebysig static void " + context.Identifier().GetText() + "(";
            for (int i = 0; i < @params.Length; ++i)
            {
                s += "int32 " + @params[i];
                if (i < @params.Length - 1)

                    s += ",";
            }
            s += ") cil managed";

            _result.AppendCodeLine(2, s + "{");
            _result.AppendCodeLine(2, EmitLocals(GetParameters(@params.ToList())));
            //emit arg to stack
            for (int i = 0; i < @params.Length; ++i)
            {


                functionScope.assignParam(@params[i].GetText(), new Value(functionArguments[i]));

            }
            functionScope.ArgCount = @params.Count();


            Visit(context.block());
            _result.AppendCodeLine(2, "ret");
            _result.AppendCodeLine(2, "}");
            fn += _result.GetCode();

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

            //fill the function call
            foreach (var vdx in ctx.exprList().expression())
            {
                functionArguments.Add(vdx.GetText());


            }


            if (ctx.exprList() != null)
            {

                Visit(ctx.exprList());

                _result.AppendCodeLine(2, $"call  void Program::{ctx.Identifier().GetText()}({GetFunctionArguments(ctx)})");
            }
            else
            {
                _result.AppendCodeLine(2, $"call  void Program::{ctx.Identifier().GetText()}()");
            }

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


            _result.AppendCodeLine(2, "ldstr " + context.GetText());
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

                        _result.AppendCodeLine(2, OpCodes.Add);

                        return Value.VOID;

                    }
                    if (left.isString() && right.isString())
                    {

                        //append to string
                        _result.AppendCodeLine(2, "call string string::Concat(string,string)");

                        return Value.VOID;

                    }
                    if ((left.isString() || right.isString()) || (right.IsNumber() || left.IsNumber()))
                    {

                        //append to string
                        _result.AppendCodeLine(2, "call string string::Concat(string,string)");
                        printOrder.Push("string");

                    }

                    break;

                case CompileParser.Subtract:
                    Visit(context.expression(0));
                    Visit(context.expression(1));
                    _result.AppendCodeLine(2, OpCodes.Sub);
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
                    Visit(context.expression(0));
                    Visit(context.expression(1));
                    _result.AppendCodeLine(2, OpCodes.Mul);
                    break;
                case "/":
                    Visit(context.expression(0));
                    Visit(context.expression(1));
                    _result.AppendCodeLine(2, OpCodes.Div);
                    break;
                case "%":
                    Visit(context.expression(0));
                    Visit(context.expression(1));
                    _result.AppendCodeLine(2, OpCodes.Rem);
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

            _result.AppendCodeLine(2, OpCodes.LdInt4 + ctx.Number().GetText());

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

            var forScope = new Scope(currentScope, "for");
            currentScope = forScope;
            Visit(context.Identifier());

            string varName = context.Identifier().GetText();
            string start = context.expression(0).GetText();
            currentScope.Assign(varName, new Value(start));

            _result.AppendCodeLine(2, OpCodes.LdInt4 + start);

            //emitlocal
            _result.AppendCodeLine(2, EmitLocals(context.Identifier().GetText()));
            _result.InitializeVariable(varName, start);
            //load start value
            labelPrev = MakeLabel(_labelCount);
            _labelCount++;
            _result.AppendCodeLine(0, OpCodes.Br + labelPrev);
            string labelTo = MakeLabel(_labelCount);
            _labelCount++;
            _labelCount++;

            //labeto
            _result.AppendCodeLine(0, labelTo + ":");
            _result.AppendCodeLine(2, OpCodes.LdLoc + varName);
            _result.AppendCodeLine(2, "ldc.i4 1 ");

            _result.AppendCodeLine(2, OpCodes.Add);
            _result.AppendCodeLine(2, "stloc " + varName);
            //statemtn
            Visit(context.block());
            _result.AppendCodeLine(0, labelPrev + ":");
            _result.AppendCodeLine(2, "ldloc " + varName);
            Visit(context.expression(1));
            //compare
            _result.AppendCodeLine(2, "clt ");

            _result.AppendCodeLine(0, "brtrue " + labelTo);


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
            _result.AppendCodeLine(0, "brfalse " + labelElseIf);
            Visit(context.ifStat().block());
            _result.AppendCodeLine(2, "br " + labelEnd);
            //emit all child of elseif
            _result.AppendCodeLine(0, labelElseIf + ":");
            foreach (var item in context.elseIfStat())
            {
                //check expression
                Visit(item.expression());
                _result.AppendCodeLine(0, "brfalse " + labelElse);
                Visit(item.block());
                _result.AppendCodeLine(2, "br " + labelEnd);

            }
            //emit else
            _result.AppendCodeLine(0, labelElse + ":");
            if (context.elseStat() != null)
            {
                Visit(context.elseStat());
            }
            // //emit end
            _result.AppendCodeLine(0, labelEnd + ":");

            return Value.VOID;


        }


        public override Value VisitCompExpression([NotNull] CompExpressionContext context)
        {
            //switch case
            if (context.op.Text == "==")
            {
                Visit(context.expression(0));
                Visit(context.expression(1));
                _result.AppendCodeLine(2, OpCodes.Ceq);
            }
            if (context.op.Text == "!=")
            {
                Visit(context.expression(0));
                Visit(context.expression(1));
                _result.AppendCodeLine(2, OpCodes.Ceq);
                _result.AppendCodeLine(2, OpCodes.LdInt4 + "0");
                _result.AppendCodeLine(2, OpCodes.Ceq);
            }
            if (context.op.Text == ">")
            {
                Visit(context.expression(0));
                Visit(context.expression(1));
                _result.AppendCodeLine(2, OpCodes.Cgt);
            }
            if (context.op.Text == "<")
            {
                Visit(context.expression(0));
                Visit(context.expression(1));
                _result.AppendCodeLine(2, OpCodes.Clt);
            }
            if (context.op.Text == ">=")
            {
                Visit(context.expression(0));
                Visit(context.expression(1));
                _result.AppendCodeLine(2, OpCodes.Cgt_Un);
                _result.AppendCodeLine(2, OpCodes.LdInt4 + "0");
                _result.AppendCodeLine(2, OpCodes.Ceq);
            }
            return Value.VOID;

        }
        public override Value VisitEqExpression([NotNull] EqExpressionContext context)
        {

            if (context.op.Text == "==")
            {
                Visit(context.expression(0));
                Visit(context.expression(1));
                _result.AppendCodeLine(2, OpCodes.Ceq);
            }
            if (context.op.Text == "!=")
            {
                Visit(context.expression(0));
                Visit(context.expression(1));
                _result.AppendCodeLine(2, OpCodes.Ceq);
                _result.AppendCodeLine(2, OpCodes.LdInt4 + "0");
                _result.AppendCodeLine(2, OpCodes.Ceq);
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