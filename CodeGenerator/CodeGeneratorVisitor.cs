using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
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
            AppendCodeLine(".assembly " + "test" + "\n{\n}\n\n.module " + "test" + ".exe\n");
            AppendCodeLine(".class private auto ansi beforefieldinit test extends [System.Runtime]System.Object {");
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

            if (context.expression() != null)
            {
                AppendCodeLine(Visit(context.expression()));
                AppendCodeLine("call void [mscorlib]System.Console::WriteLine(int32)");
            }
            else
            {
                AppendCodeLine("call void [mscorlib]System.Console::WriteLine()");
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
            AppendCodeLine("stloc " + varName);




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

            for (var i = 0; i < context.idList().Identifier().Length; i++)
            {
                //load arg
                _scopeList.Add(context.idList().Identifier()[i].GetText());
                //   AppendCodeLine("ldarg " +context.idList().Identifier()[i].GetText());
                //store arg
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


                AppendCodeLine(Visit(ctx.exprList()));

                AppendCodeLine($"call  void test::{ctx.Identifier().GetText()}(int32,int32)");
            }

            return "";

        }

        //visit add expression


        public override string VisitAddExpression([NotNull] AddExpressionContext context)
        {

            if (context.op.Text == "+")
            {

                Log("VisitAddExpression");
                AppendCodeLine(Visit(context.expression(0)));

                AppendCodeLine(Visit(context.expression(1)));
                AppendCodeLine("add");

            }
            if (context.op.Text == "-")
            {

                Log("VisitAddExpression");
                AppendCodeLine(Visit(context.expression(0)));
                AppendCodeLine(Visit(context.expression(1)));
                AppendCodeLine("sub");
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
                AppendCodeLine("mul");
            }
            if (context.op.Text == "/")
            {

                Log("VisitAddExpression");
                AppendCodeLine(Visit(context.expression(0)));
                AppendCodeLine(Visit(context.expression(1)));
                AppendCodeLine("div");
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
            AppendCodeLine("ldloc " + ctx.Identifier().GetText());

            return "";



        }


        public override string VisitNumberExpression(NumberExpressionContext ctx)
        {
            Log("VisitNumberExpression");
            AppendCodeLine("ldc.i4 " + ctx.Number().GetText());

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

            string varName = context.Identifier().GetText();
            string start = context.expression(0).GetText();

            AppendCodeLine("ldc.i4 " + start);


            //emitlocal
            AppendCodeLine(EmitLocals(context.Identifier().GetText()));
            _result.InitializeVariable(varName, start);
            //load start value
            labelPrev = MakeLabel(_labelCount);
            _labelCount++;
            AppendCodeLine("br " + labelPrev);
            string labelTo = MakeLabel(_labelCount);
            _labelCount++;
            _labelCount++;

            //labeto
            AppendCodeLine(labelTo + ":");
            AppendCodeLine("ldloc " + varName);
            AppendCodeLine("ldc.i4 1 ");

            AppendCodeLine("add");
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