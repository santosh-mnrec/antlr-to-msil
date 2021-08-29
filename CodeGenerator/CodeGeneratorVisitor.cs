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

    public class CodeGeneratorVisitor : CompileBaseVisitor<string>
    {

        //public readonly ILBuilder mainTarget = ILBuilder.Create();

        private CodeBuilder _result = new CodeBuilder();

        public SymbolTable variableDefs = new SymbolTable();

        //scopelist
        public List<string> _scopeList = new List<string>();
        //local function variables
        private List<string> _localFunctionVariables = new List<string>();
        private void AppendLocalFunctionVariable(string variable) => _localFunctionVariables.Add(variable);


        string main = "";
        string fn = "";
        string header = "";
        public CodeGeneratorVisitor()
        {
            _result.AppendCodeLine(".assembly extern mscorlib\n{\n}\n");
            _result.AppendCodeLine(".assembly " + "Program" + "\n{\n}\n\n.module " + "test" + ".exe\n");
            _result.AppendCodeLine(".class private auto ansi beforefieldinit Program extends [System.Runtime]System.Object {");
            _result.AppendCodeLine(" .method private hidebysig static void  Main(string[] args) cil managed {");
            _result.AppendCodeLine(" .entrypoint");
            header = _result.GetCode();

        }
        public override string VisitParse([NotNull] ParseContext context)
        {

            _result.AppendCodeLine(Visit(context.block()));
            _result.AppendCodeLine(main);
            _result.AppendCodeLine("ret}");

            _result.AppendCodeLine(fn);

            var code = header + _result.GetCode() + "\n}";
            File.WriteAllText(@"out\test.il", code);
            return string.Empty;
        }

        public override string VisitPrintlnFunctionCall([NotNull] PrintlnFunctionCallContext context)
        {

            _result.AppendCodeLine(Visit(context.expression()));
            //if identifier is a string
            if (context.expression().GetText().Contains("\""))
            {
                _result.AppendCodeLine("call void [mscorlib]System.Console::WriteLine(string)");
            }
            else
            {
                _result.AppendCodeLine("call void [mscorlib]System.Console::WriteLine(int32)");
            }

            return "";
        }
        public override string VisitBlock(CompileParser.BlockContext context)
        {
            foreach (var fdx in context.functionDecl())
            {
                _result.Append(this.Visit(fdx));

            }
            foreach (var vdx in context.statement())
            {
                this.Visit(vdx);

            }
            if (context.expression() != null)
                Visit(context.expression());
            return "";
        }

        //visit assigment
        public override string VisitAssignment(CompileParser.AssignmentContext context)
        {
            String varName = context.Identifier().GetText();
            _result.AppendCodeLine(EmitLocals(varName));
            if (!variableDefs.IsSymbol(varName))
                variableDefs.AddSymbol(varName);
            _result.AppendCodeLine(this.Visit(context.expression()));
            _result.AppendCodeLine(OpCodes.StLoc + varName);

            return "";

        }
        //vist function declration
        public override string VisitFunctionDecl(CompileParser.FunctionDeclContext context)
        {
            main += _result.GetCode();

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
                //add local function variable
                AppendLocalFunctionVariable(@params[i].GetText());

            }

            if (context.idList()?.Identifier()?.Length > 0)
            {
                for (var i = 0; i < context.idList()?.Identifier()?.Length; i++)
                {
                    //load arg
                    _scopeList.Add(context.idList().Identifier()[i].GetText());

                }
            }

            _result.AppendCodeLine(Visit(context.block()) + "ret");
            _result.AppendCodeLine("}");

            fn += _result.GetCode();

            return "";

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
        public override string VisitIdentifierFunctionCall(IdentifierFunctionCallContext ctx)
        {


            if (ctx.exprList() != null)
            {

                //count args
                int count = ctx.exprList().expression().Length;
                //load args

                _result.AppendCodeLine(Visit(ctx.exprList()));

                _result.AppendCodeLine($"call  void Program::{ctx.Identifier().GetText()}({GetFunctionArguments(ctx)})");
            }
            else
            {
                _result.AppendCodeLine($"call  void Program::{ctx.Identifier().GetText()}()");
            }

            return "";

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

        public override string VisitStringExpression([NotNull] StringExpressionContext context)
        {
            //load string

            //update identifier type
            variableDefs.UpdateSymbolType(context.Parent.GetChild(0).GetText(), "string");
            _result.AppendCodeLine("ldstr " + context.GetText());
            return "";
        }
        public override string VisitAddExpression([NotNull] AddExpressionContext context)
        {

            //switch case on operator
            switch (context.op.Type)
            {
                case CompileParser.Add:
                    _result.AppendCodeLine(this.Visit(context.expression(0)));
                    _result.AppendCodeLine(this.Visit(context.expression(1)));
                    //if both are ints
                    if (variableDefs.GetSymbolType(context.expression(0).GetText()) == "string" 
                    && variableDefs.GetSymbolType(context.expression(1).GetText()) == "string")
                    {
                        //append to string
                        _result.AppendCodeLine("call string string::Concat(string,string)");
                       // _result.AppendCodeLine(OpCodes.Call + " System.String::Concat(System.String,System.String)");
                    }
                    else
                    {
                        _result.AppendCodeLine(OpCodes.Add);

                    }

                    break;

                case CompileParser.Subtract:
                    _result.AppendCodeLine(this.Visit(context.expression(0)));
                    _result.AppendCodeLine(this.Visit(context.expression(1)));
                    _result.AppendCodeLine(OpCodes.Sub);
                    break;

                default:

                    break;
            }
            return "";

        }

        public override string VisitMultExpression([NotNull] MultExpressionContext context)
        {

            //swithc case on op.Text
            switch (context.op.Text)
            {
                case "*":
                    _result.AppendCodeLine(Visit(context.expression(0)));
                    _result.AppendCodeLine(Visit(context.expression(1)));
                    _result.AppendCodeLine(OpCodes.Mul);
                    break;
                case "/":
                    _result.AppendCodeLine(Visit(context.expression(0)));
                    _result.AppendCodeLine(Visit(context.expression(1)));
                    _result.AppendCodeLine(OpCodes.Div);
                    break;
                case "%":
                    _result.AppendCodeLine(Visit(context.expression(0)));
                    _result.AppendCodeLine(Visit(context.expression(1)));
                    _result.AppendCodeLine(OpCodes.Rem);
                    break;
                default:
                    break;
            }


            return "";

        }
        public override string VisitExpressionExpression(CompileParser.ExpressionExpressionContext context)
        {
            _result.AppendCodeLine(Visit(context.expression()));

            return "";

        }

        public override string VisitIdentifierExpression(IdentifierExpressionContext ctx)
        {
            if (_localFunctionVariables.Contains(ctx.Identifier().GetText()))
            {
                //loar arg
                _result.AppendCodeLine(OpCodes.LdArg + ctx.Identifier().GetText());
                //remove the variable from the local function variables
                _localFunctionVariables.Remove(ctx.Identifier().GetText());
            }
            else
            {
                //load local
                _result.AppendCodeLine(OpCodes.LdLoc + ctx.Identifier().GetText());

            }

            return "";

        }

        public override string VisitNumberExpression(NumberExpressionContext ctx)
        {
            variableDefs.UpdateSymbolType(ctx.Parent.GetChild(0).GetText(), "int32");
            _result.AppendCodeLine(OpCodes.LdInt4 + ctx.Number().GetText());


            return "";

        }
        //visit for loop
        int _labelCount = 0;
        string labelPrev = "";

        private string MakeLabel(int label)
        {
            return string.Format("IL_{0:x4}", label);
        }
        public override string VisitForStatement([NotNull] ForStatementContext context)
        {

            System.Console.WriteLine(context.GetText());
            _result.AppendCodeLine(Visit(context.Identifier()));

            string varName = context.Identifier().GetText();
            string start = context.expression(0).GetText();

            _result.AppendCodeLine(OpCodes.LdInt4 + start);

            //emitlocal
            _result.AppendCodeLine(EmitLocals(context.Identifier().GetText()));
            _result.InitializeVariable(varName, start);
            //load start value
            labelPrev = MakeLabel(_labelCount);
            _labelCount++;
            _result.AppendCodeLine(OpCodes.Br + labelPrev);
            string labelTo = MakeLabel(_labelCount);
            _labelCount++;
            _labelCount++;

            //labeto
            _result.AppendCodeLine(labelTo + ":");
            _result.AppendCodeLine(OpCodes.LdLoc + varName);
            _result.AppendCodeLine("ldc.i4 1 ");

            _result.AppendCodeLine(OpCodes.Add);
            _result.AppendCodeLine("stloc " + varName);
            //statemtn
            _result.AppendCodeLine(Visit(context.block()));
            _result.AppendCodeLine(labelPrev + ":");
            _result.AppendCodeLine("ldloc " + varName);
            _result.AppendCodeLine(Visit(context.expression(1)));
            //compare
            _result.AppendCodeLine("clt ");

            _result.AppendCodeLine("brtrue " + labelTo);

            return "";

        }

        public override string VisitIfStatement([NotNull] IfStatementContext context)
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
            _result.AppendCodeLine(Visit(context.ifStat().expression()));
            _result.AppendCodeLine("brfalse " + labelElseIf);
            _result.AppendCodeLine(Visit(context.ifStat().block()));
            _result.AppendCodeLine("br " + labelEnd);
            //emit all child of elseif
            _result.AppendCodeLine(labelElseIf + ":");
            foreach (var item in context.elseIfStat())
            {
                //check expression
                _result.AppendCodeLine(Visit(item.expression()));
                _result.AppendCodeLine("brfalse " + labelElse);
                _result.AppendCodeLine(Visit(item.block()));
                _result.AppendCodeLine("br " + labelEnd);

            }
            //emit else
            _result.AppendCodeLine(labelElse + ":");
            if (context.elseStat() != null)
            {
                _result.AppendCodeLine(Visit(context.elseStat()));
            }
            //emit end
            _result.AppendCodeLine(labelEnd + ":");

            return "";


        }


        public override string VisitCompExpression([NotNull] CompExpressionContext context)
        {
            //switch case
            if (context.op.Text == "==")
            {
                _result.AppendCodeLine(Visit(context.expression(0)));
                _result.AppendCodeLine(Visit(context.expression(1)));
                _result.AppendCodeLine(OpCodes.Ceq);
            }
            if (context.op.Text == "!=")
            {
                _result.AppendCodeLine(Visit(context.expression(0)));
                _result.AppendCodeLine(Visit(context.expression(1)));
                _result.AppendCodeLine(OpCodes.Ceq);
                _result.AppendCodeLine(OpCodes.LdInt4 + "0");
                _result.AppendCodeLine(OpCodes.Ceq);
            }
            if (context.op.Text == ">")
            {
                _result.AppendCodeLine(Visit(context.expression(0)));
                _result.AppendCodeLine(Visit(context.expression(1)));
                _result.AppendCodeLine(OpCodes.Cgt);
            }
            if (context.op.Text == "<")
            {
                _result.AppendCodeLine(Visit(context.expression(0)));
                _result.AppendCodeLine(Visit(context.expression(1)));
                _result.AppendCodeLine(OpCodes.Clt);
            }
            if (context.op.Text == ">=")
            {
                _result.AppendCodeLine(Visit(context.expression(0)));
                _result.AppendCodeLine(Visit(context.expression(1)));
                _result.AppendCodeLine(OpCodes.Cgt_Un);
                _result.AppendCodeLine(OpCodes.LdInt4 + "0");
                _result.AppendCodeLine(OpCodes.Ceq);
            }
            return "";
        }
        public override string VisitEqExpression([NotNull] EqExpressionContext context)
        {

            if (context.op.Text == "==")
            {
                _result.AppendCodeLine(Visit(context.expression(0)));
                _result.AppendCodeLine(Visit(context.expression(1)));
                _result.AppendCodeLine(OpCodes.Ceq);
            }
            if (context.op.Text == "!=")
            {
                _result.AppendCodeLine(Visit(context.expression(0)));
                _result.AppendCodeLine(Visit(context.expression(1)));
                _result.AppendCodeLine(OpCodes.Ceq);
                _result.AppendCodeLine(OpCodes.LdInt4 + "0");
                _result.AppendCodeLine(OpCodes.Ceq);
            }
            return "";
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