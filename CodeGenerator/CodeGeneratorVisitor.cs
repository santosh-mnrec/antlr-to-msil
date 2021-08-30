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

        private Function func = new Function();





        string main = "";
        string fn = "";
        string header = "";
        private Scope currentScope = new Scope();
        private Stack<string> printOrder = new Stack<string>();
        private List<string> functionArguments = new List<string>();
        public CodeGeneratorVisitor()
        {
            _result.AppendCodeLine(".assembly extern mscorlib\n{\n}\n");
            _result.AppendCodeLine(".assembly " + "Program" + "\n{\n}\n\n.module " + "test" + ".exe\n");
            _result.AppendCodeLine(".class private auto ansi beforefieldinit Program extends [System.Runtime]System.Object {");
            _result.AppendCodeLine(" .method private hidebysig static void  Main(string[] args) cil managed {");
            _result.AppendCodeLine(" .entrypoint");
            header = _result.GetCode();
        }


        public override Value VisitParse([NotNull] ParseContext context)
        {

            Visit(context.block());
            _result.AppendCodeLine(main);
            _result.AppendCodeLine("ret}");

            _result.AppendCodeLine(fn);

            var code = header + _result.GetCode() + "\n}";
            File.WriteAllText(@"out\test.il", code);
            return Value.VOID;
        }

        public override Value VisitPrintlnFunctionCall([NotNull] PrintlnFunctionCallContext context)
        {

            var result = Visit(context.expression());

            if (printOrder.Count() > 0)
            {
                var order = printOrder.Pop();
                if (order == "int")
                {

                    _result.AppendCodeLine("call void [mscorlib]System.Console::WriteLine(int32)");


                }
                else
                {

                    _result.AppendCodeLine("call void [mscorlib]System.Console::WriteLine(string)");
                }
            }
           else if(result.IsNumber()){

                _result.AppendCodeLine("call void [mscorlib]System.Console::WriteLine(int32)");
           }
           else if(result.isString()){

                _result.AppendCodeLine("call void [mscorlib]System.Console::WriteLine(string)");
           }
           else{
                _result.AppendCodeLine("call void [mscorlib]System.Console::WriteLine(object)");
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
            _result.AppendCodeLine(EmitLocals(varName));
            var variable = currentScope.Resolve(varName);
            var value = this.Visit(context.expression());
            if (variable == null)
            {
                currentScope.Assign(varName, value);
            }
            _result.AppendCodeLine(OpCodes.StLoc + varName);
            return Value.VOID;


        }
        public override Value VisitIdentifierExpression(IdentifierExpressionContext ctx)
        {
            // if (_localFunctionVariables.Contains(ctx.Identifier().GetText()))
            // {
            //     //loar arg
            //     _result.AppendCodeLine(OpCodes.LdArg + ctx.Identifier().GetText());
            //     //remove the variable from the local function variables
            //     _localFunctionVariables.Remove(ctx.Identifier().GetText());
            // }
            // else
            // {
            //     //load local
            //     _result.AppendCodeLine(OpCodes.LdLoc + ctx.Identifier().GetText());
            // }
            //currentscope


            var identifier = ctx.Identifier().GetText();
            var variable = currentScope.Resolve(identifier);


            if (currentScope.IsFunction && variable.ToString() != "NULL")
            {
                _result.AppendCodeLine(OpCodes.LdArg + ctx.Identifier().GetText());
            }
            else
            {
                _result.AppendCodeLine(OpCodes.LdLoc + ctx.Identifier().GetText());
            }

            return variable.ToString() == "NULL" ? Value.VOID : new Value(variable);


        }


        //vist function declration
        public override Value VisitFunctionDecl(CompileParser.FunctionDeclContext context)
        {
            main += _result.GetCode();

            var functionScope = new Scope(currentScope, true);
            currentScope = functionScope;
            func.Name = context.Identifier().GetText();


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

            _result.AppendCodeLine(s + "{");
            _result.AppendCodeLine(EmitLocals(GetParameters(@params.ToList())));
            //emit arg to stack
            for (int i = 0; i < @params.Length; ++i)
            {


                functionScope.assignParam(@params[i].GetText(), new Value(functionArguments[i]));

            }


            Visit(context.block());
            _result.AppendCodeLine("ret");
            _result.AppendCodeLine("}");
            func.Parameters = currentScope.Variables;

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

                _result.AppendCodeLine($"call  void Program::{ctx.Identifier().GetText()}({GetFunctionArguments(ctx)})");
            }
            else
            {
                _result.AppendCodeLine($"call  void Program::{ctx.Identifier().GetText()}()");
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


            _result.AppendCodeLine("ldstr " + context.GetText());
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

                        _result.AppendCodeLine(OpCodes.Add);
                        // _result.AppendCodeLine(OpCodes.LdLoc + context.Parent.GetChild(0).GetText());
                        printOrder.Push("int");

                    }
                    else
                    {

                        //append to string
                        _result.AppendCodeLine("call string string::Concat(string,string)");
                        // _result.AppendCodeLine(OpCodes.LdLoc + context.Parent.GetChild(0).GetText());
                        printOrder.Push("string");

                    }

                    break;

                case CompileParser.Subtract:
                    // _result.AppendCodeLine(this.Visit(context.expression(0)));
                    // _result.AppendCodeLine(this.Visit(context.expression(1)));
                    // _result.AppendCodeLine(OpCodes.Sub);
                    break;

                default:

                    break;
            }
            return Value.VOID;

        }

        public override Value VisitMultExpression([NotNull] MultExpressionContext context)
        {

            // //swithc case on op.Text
            // switch (context.op.Text)
            // {
            //     case "*":
            //         _result.AppendCodeLine(Visit(context.expression(0)));
            //         _result.AppendCodeLine(Visit(context.expression(1)));
            //         _result.AppendCodeLine(OpCodes.Mul);
            //         break;
            //     case "/":
            //         _result.AppendCodeLine(Visit(context.expression(0)));
            //         _result.AppendCodeLine(Visit(context.expression(1)));
            //         _result.AppendCodeLine(OpCodes.Div);
            //         break;
            //     case "%":
            //         _result.AppendCodeLine(Visit(context.expression(0)));
            //         _result.AppendCodeLine(Visit(context.expression(1)));
            //         _result.AppendCodeLine(OpCodes.Rem);
            //         break;
            //     default:
            //         break;
            // }


            return Value.VOID;

        }
        public override Value VisitExpressionExpression(CompileParser.ExpressionExpressionContext context)
        {
            Visit(context.expression());

            //add value to current scope
            //currentScope.AddSymbol(context.expression().GetText(), variableDefs.GetSymbolType(context.expression().GetText()));
            currentScope.Assign(context.expression().GetText(), Value.VOID);


            return Value.VOID;

        }


        private bool IsOperator(AssignmentContext ctx)
        {

            //if right side is an expression
            if (ctx.expression()?.GetChild(2)?.ChildCount > 1)
            {
                return true;

            }
            return false;


        }


        public override Value VisitFunctionCallExpression(FunctionCallExpressionContext ctx)
        {


            return Value.VOID;

        }
        public override Value VisitNumberExpression(NumberExpressionContext ctx)
        {


            _result.AppendCodeLine(OpCodes.LdInt4 + ctx.Number().GetText());


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

            // System.Console.WriteLine(context.GetText());
            // Visit(context.Identifier());

            // string varName = context.Identifier().GetText();
            // string start = context.expression(0).GetText();

            // _result.AppendCodeLine(OpCodes.LdInt4 + start);

            // //emitlocal
            // _result.AppendCodeLine(EmitLocals(context.Identifier().GetText()));
            // _result.InitializeVariable(varName, start);
            // //load start value
            // labelPrev = MakeLabel(_labelCount);
            // _labelCount++;
            // _result.AppendCodeLine(OpCodes.Br + labelPrev);
            // string labelTo = MakeLabel(_labelCount);
            // _labelCount++;
            // _labelCount++;

            // //labeto
            // _result.AppendCodeLine(labelTo + ":");
            // _result.AppendCodeLine(OpCodes.LdLoc + varName);
            // _result.AppendCodeLine("ldc.i4 1 ");

            // _result.AppendCodeLine(OpCodes.Add);
            // _result.AppendCodeLine("stloc " + varName);
            // //statemtn
            // _result.AppendCodeLine(Visit(context.block()));
            // _result.AppendCodeLine(labelPrev + ":");
            // _result.AppendCodeLine("ldloc " + varName);
            // _result.AppendCodeLine(Visit(context.expression(1)));
            // //compare
            // _result.AppendCodeLine("clt ");

            // _result.AppendCodeLine("brtrue " + labelTo);

            return Value.VOID;

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
            // _result.AppendCodeLine(Visit(context.ifStat().expression()));
            // _result.AppendCodeLine("brfalse " + labelElseIf);
            // _result.AppendCodeLine(Visit(context.ifStat().block()));
            // _result.AppendCodeLine("br " + labelEnd);
            // //emit all child of elseif
            // _result.AppendCodeLine(labelElseIf + ":");
            // foreach (var item in context.elseIfStat())
            // {
            //     //check expression
            //     _result.AppendCodeLine(Visit(item.expression()));
            //     _result.AppendCodeLine("brfalse " + labelElse);
            //     _result.AppendCodeLine(Visit(item.block()));
            //     _result.AppendCodeLine("br " + labelEnd);

            // }
            // //emit else
            // _result.AppendCodeLine(labelElse + ":");
            // if (context.elseStat() != null)
            // {
            //     _result.AppendCodeLine(Visit(context.elseStat()));
            // }
            // //emit end
            _result.AppendCodeLine(labelEnd + ":");

            return Value.VOID;


        }


        public override Value VisitCompExpression([NotNull] CompExpressionContext context)
        {
            //switch case
            //     if (context.op.Text == "==")
            //     {
            //         _result.AppendCodeLine(Visit(context.expression(0)));
            //         _result.AppendCodeLine(Visit(context.expression(1)));
            //         _result.AppendCodeLine(OpCodes.Ceq);
            //     }
            //     if (context.op.Text == "!=")
            //     {
            //         _result.AppendCodeLine(Visit(context.expression(0)));
            //         _result.AppendCodeLine(Visit(context.expression(1)));
            //         _result.AppendCodeLine(OpCodes.Ceq);
            //         _result.AppendCodeLine(OpCodes.LdInt4 + "0");
            //         _result.AppendCodeLine(OpCodes.Ceq);
            //     }
            //     if (context.op.Text == ">")
            //     {
            //         _result.AppendCodeLine(Visit(context.expression(0)));
            //         _result.AppendCodeLine(Visit(context.expression(1)));
            //         _result.AppendCodeLine(OpCodes.Cgt);
            //     }
            //     if (context.op.Text == "<")
            //     {
            //         _result.AppendCodeLine(Visit(context.expression(0)));
            //         _result.AppendCodeLine(Visit(context.expression(1)));
            //         _result.AppendCodeLine(OpCodes.Clt);
            //     }
            //     if (context.op.Text == ">=")
            //     {
            //         _result.AppendCodeLine(Visit(context.expression(0)));
            //         _result.AppendCodeLine(Visit(context.expression(1)));
            //         _result.AppendCodeLine(OpCodes.Cgt_Un);
            //         _result.AppendCodeLine(OpCodes.LdInt4 + "0");
            //         _result.AppendCodeLine(OpCodes.Ceq);
            //     }
            //     return Value.VOID;
            // }
            // public override Value VisitEqExpression([NotNull] EqExpressionContext context)
            // {

            //     if (context.op.Text == "==")
            //     {
            //         _result.AppendCodeLine(Visit(context.expression(0)));
            //         _result.AppendCodeLine(Visit(context.expression(1)));
            //         _result.AppendCodeLine(OpCodes.Ceq);
            //     }
            //     if (context.op.Text == "!=")
            //     {
            //         _result.AppendCodeLine(Visit(context.expression(0)));
            //         _result.AppendCodeLine(Visit(context.expression(1)));
            //         _result.AppendCodeLine(OpCodes.Ceq);
            //         _result.AppendCodeLine(OpCodes.LdInt4 + "0");
            //         _result.AppendCodeLine(OpCodes.Ceq);
            //     }
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