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

        private ConverterResult _result = new ConverterResult();
        public static Scope _scope;
        public HashSet<string> variableDefs = new HashSet<string>();
        private void AppendCode(string code) => _result.Append(code);

        private void AppendCodeLine() => _result.Append(Environment.NewLine);
        //scopelist
        public List<string> _scopeList = new List<string>();
        private void AppendCodeLine(string code) => _result.Append(code + Environment.NewLine);
        //local function variables
        private List<string> _localFunctionVariables = new List<string>();
        private void AppendLocalFunctionVariable(string variable) => _localFunctionVariables.Add(variable);

        private void Log(string message)
        {
            Console.WriteLine(message);
        }

        string main = "";
        string fn = "";
        string header = "";
        public CodeGeneratorVisitor()
        {
            AppendCodeLine(".assembly extern mscorlib\n{\n}\n");
            AppendCodeLine(".assembly " + "Program" + "\n{\n}\n\n.module " + "test" + ".exe\n");
            AppendCodeLine(".class private auto ansi beforefieldinit Program extends [System.Runtime]System.Object {");
            AppendCodeLine(" .method private hidebysig static void  Main(string[] args) cil managed {");
            AppendCodeLine(" .entrypoint");
            header = _result.GetCode();

        }
        public override string VisitParse([NotNull] ParseContext context)
        {

            Log("Entering Parse");
            AppendCodeLine(Visit(context.block()));
            Log("Ex Parse");
            AppendCodeLine(main);
            AppendCodeLine("ret}");

            AppendCodeLine(fn);

            var code = header + _result.GetCode() + "\n}";
            File.WriteAllText(@"out\test.il", code);
            return string.Empty;
        }

        public override string VisitPrintlnFunctionCall([NotNull] PrintlnFunctionCallContext context)
        {

            AppendCodeLine(Visit(context.expression()));
            //if identifier is a string
            if (context.expression().GetText().Contains("\""))
            {
                AppendCodeLine("call void [mscorlib]System.Console::WriteLine(string)");
            }
            else
            {
                AppendCodeLine("call void [mscorlib]System.Console::WriteLine(int32)");
            }

            return "";
        }
        public override string VisitBlock(CompileParser.BlockContext context)
        {
            foreach (var fdx in context.functionDecl())
            {
                Log("VisitFunctionDecl");
                _result.Append(this.Visit(fdx));

            }
            foreach (var vdx in context.statement())
            {
                Log("VisitStatement");
                this.Visit(vdx);

            }
            if (context.expression() != null)
                Visit(context.expression());
            return "";
        }

        //visit assigment
        public override string VisitAssignment(CompileParser.AssignmentContext context)
        {
            Log("VisitAssignment");

            String varName = context.Identifier().GetText();
            AppendCodeLine(EmitLocals(varName));
            if (!variableDefs.Contains(varName))
                variableDefs.Add(varName);
            AppendCodeLine(this.Visit(context.expression()));
            AppendCodeLine(OpCodes.StLoc + varName);

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

            AppendCodeLine(s + "{");
            AppendCodeLine(EmitLocals(GetParameters(@params.ToList())));
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

            AppendCodeLine(Visit(context.block()) + "ret");
            AppendCodeLine("}");

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

            Log("VisitIdentifierFunctionCall");
            if (ctx.exprList() != null)
            {

                //count args
                int count = ctx.exprList().expression().Length;
                //load args

                AppendCodeLine(Visit(ctx.exprList()));

                AppendCodeLine($"call  void Program::{ctx.Identifier().GetText()}({GetFunctionArguments(ctx)})");
            }
            else
            {
                AppendCodeLine($"call  void Program::{ctx.Identifier().GetText()}()");
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
            AppendCodeLine("ldstr " + context.GetText());
            return "";
        }
        public override string VisitAddExpression([NotNull] AddExpressionContext context)
        {

            if (context.op.Text == "+")
            {

                Log("VisitAddExpression");
                AppendCodeLine(Visit(context.expression(0)));

                AppendCodeLine(Visit(context.expression(1)));
                AppendCodeLine(OpCodes.Add);

            }
            if (context.op.Text == "-")
            {

                Log("VisitAddExpression");
                AppendCodeLine(Visit(context.expression(0)));
                AppendCodeLine(Visit(context.expression(1)));
                AppendCodeLine(OpCodes.Sub);
            }

            return "";

        }

        public override string VisitMultExpression([NotNull] MultExpressionContext context)
        {
            if (context.op.Text == "*")
            {

                Log("VisitAddExpression");
                AppendCodeLine(Visit(context.expression(0)));
                AppendCodeLine(Visit(context.expression(1)));
                AppendCodeLine(OpCodes.Mul);
            }
            if (context.op.Text == "/")
            {

                Log("VisitAddExpression");
                AppendCodeLine(Visit(context.expression(0)));
                AppendCodeLine(Visit(context.expression(1)));
                AppendCodeLine(OpCodes.Div);
            }

            return "";

        }
        public override string VisitExpressionExpression(CompileParser.ExpressionExpressionContext context)
        {
            Log("VisitExpression");
            AppendCodeLine(Visit(context.expression()));

            return "";

        }

        public override string VisitIdentifierExpression(IdentifierExpressionContext ctx)
        {
            Log("VisitIdentifierExpression");
            if (_localFunctionVariables.Contains(ctx.Identifier().GetText()))
            {
                //loar arg
                AppendCodeLine(OpCodes.LdArg + ctx.Identifier().GetText());
                //remove the variable from the local function variables
                _localFunctionVariables.Remove(ctx.Identifier().GetText());
            }
            else
            {
                AppendCodeLine(OpCodes.LdLoc + ctx.Identifier().GetText());
            }

            return "";

        }

        public override string VisitNumberExpression(NumberExpressionContext ctx)
        {
            Log("VisitNumberExpression");
            AppendCodeLine(OpCodes.LdInt4 + ctx.Number().GetText());

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

            //generate for lopp for (int x = 0; x < 10; i++) {Console.WriteLine(x);}

            Log("VisitForStatement");
            System.Console.WriteLine(context.GetText());
            AppendCodeLine(Visit(context.Identifier()));

            string varName = context.Identifier().GetText();
            string start = context.expression(0).GetText();

            AppendCodeLine(OpCodes.LdInt4 + start);

            //emitlocal
            AppendCodeLine(EmitLocals(context.Identifier().GetText()));
            _result.InitializeVariable(varName, start);
            //load start value
            labelPrev = MakeLabel(_labelCount);
            _labelCount++;
            AppendCodeLine(OpCodes.Br + labelPrev);
            string labelTo = MakeLabel(_labelCount);
            _labelCount++;
            _labelCount++;

            //labeto
            AppendCodeLine(labelTo + ":");
            AppendCodeLine(OpCodes.LdLoc + varName);
            AppendCodeLine("ldc.i4 1 ");

            AppendCodeLine(OpCodes.Add);
            AppendCodeLine("stloc " + varName);
            //statemtn
            AppendCodeLine(Visit(context.block()));
            AppendCodeLine(labelPrev + ":");
            AppendCodeLine("ldloc " + varName);
            AppendCodeLine(Visit(context.expression(1)));
            //compare
            AppendCodeLine("clt ");

            AppendCodeLine("brtrue " + labelTo);

            return "";

        }

        public override string VisitIfStatement([NotNull] IfStatementContext context)
        {



            Log("VisitIfStatement");
            //lable for end
            string labelEnd = MakeLabel(_labelCount);
            _labelCount++;

            string labelIf = MakeLabel(_labelCount);
            _labelCount++;
            //lable for else if
            string labelElseIf = MakeLabel(_labelCount);
            _labelCount++;
            //lable for else
            string labelElse = MakeLabel(_labelCount);
            _labelCount++;





            AppendCodeLine(Visit(context.ifStat().expression()));
            AppendCodeLine(OpCodes.Brfalse + labelElseIf);

            AppendCodeLine(Visit(context.ifStat().block()));
            AppendCodeLine(OpCodes.Br + labelEnd);


            //else if
            if (context.elseIfStat() != null)
            {
                AppendCodeLine(labelElseIf + ":");
                //for all else if condition
                foreach (var item in context.elseIfStat())
                {
                    AppendCodeLine(Visit(item.expression()));
                    AppendCodeLine(OpCodes.Brfalse + labelEnd);

                    AppendCodeLine(Visit(item.block()));
                    //exit loop

                }

            }
            //else
            if (context.elseStat() != null)
            {
                AppendCodeLine(labelElse + ":");
                AppendCodeLine(Visit(context.elseStat().block()));
            }

            AppendCodeLine(labelEnd + ":");




            return "";






        }

        private void AppendCodeLine(object ret)
        {
            throw new NotImplementedException();
        }

        public override string VisitCompExpression([NotNull] CompExpressionContext context)
        {
            //switch case
            Log("VisitCompExpression");
            if (context.op.Text == "==")
            {
                AppendCodeLine(Visit(context.expression(0)));
                AppendCodeLine(Visit(context.expression(1)));
                AppendCodeLine(OpCodes.Ceq);
            }
            if (context.op.Text == "!=")
            {
                AppendCodeLine(Visit(context.expression(0)));
                AppendCodeLine(Visit(context.expression(1)));
                AppendCodeLine(OpCodes.Ceq);
                AppendCodeLine(OpCodes.LdInt4 + "0");
                AppendCodeLine(OpCodes.Ceq);
            }
            if (context.op.Text == ">")
            {
                AppendCodeLine(Visit(context.expression(0)));
                AppendCodeLine(Visit(context.expression(1)));
                AppendCodeLine(OpCodes.Cgt);
            }
            if (context.op.Text == "<")
            {
                AppendCodeLine(Visit(context.expression(0)));
                AppendCodeLine(Visit(context.expression(1)));
                AppendCodeLine(OpCodes.Clt);
            }
            if (context.op.Text == ">=")
            {
                AppendCodeLine(Visit(context.expression(0)));
                AppendCodeLine(Visit(context.expression(1)));
                AppendCodeLine(OpCodes.Cgt_Un);
                AppendCodeLine(OpCodes.LdInt4 + "0");
                AppendCodeLine(OpCodes.Ceq);
            }
            return "";
        }
        public override string VisitEqExpression([NotNull] EqExpressionContext context)
        {

            Log("VisitEqExpression");
            if (context.op.Text == "==")
            {
                AppendCodeLine(Visit(context.expression(0)));
                AppendCodeLine(Visit(context.expression(1)));
                AppendCodeLine(OpCodes.Ceq);
            }
            if (context.op.Text == "!=")
            {
                AppendCodeLine(Visit(context.expression(0)));
                AppendCodeLine(Visit(context.expression(1)));
                AppendCodeLine(OpCodes.Ceq);
                AppendCodeLine(OpCodes.LdInt4 + "0");
                AppendCodeLine(OpCodes.Ceq);
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
            Log("IsFunctionContext");

            return context.Parent is FunctionDeclContext;
        }

        #endregion

    }
}