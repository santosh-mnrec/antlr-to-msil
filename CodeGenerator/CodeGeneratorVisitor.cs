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

        private readonly CodeBuilder codeBuilder = new CodeBuilder();

        private string main = "";
        private string fn = "";
        private string header = "";
        //visit for loop
        private int _labelCount = 0;
        private string labelPrev = "";
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
            codeBuilder.LoadInstructions(2, " .locals init (class [mscorlib]System.Exception e)");
            codeBuilder.LoadInstructions(2, " .try");
            codeBuilder.LoadInstructions(2, " {");





            header = codeBuilder.GetCode();

        }


        public override Value VisitParse([NotNull] ParseContext context)
        {
            int labelCount = 0;
            var labelTo = MakeLabel(labelCount);
            labelCount++;

            _ = Visit(context.block());

            codeBuilder.LoadInstructions(2, main);
            codeBuilder.EmitTryCatch(labelTo);
            codeBuilder.EmitCatchIL(labelTo);
            // codeBuilder.LoadInstructions(2, "ret \n}");
            codeBuilder.LoadInstructions(2, fn);

            var code = header + codeBuilder.GetCode() + "\n}";

            File.WriteAllText(@"out\test.il", code);
            return Value.VOID;
        }

        public override Value VisitPrintlnFunctionCall([NotNull] PrintlnFunctionCallContext context)
        {


            Visit(context.expression());
            System.Console.WriteLine($"Executed at {DateTime.UtcNow}");
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
                var returnValue = Visit(context.expression());

                return returnValue;


            }
            return Value.VOID;
        }

        public override Value VisitVarDeclration([NotNull] VarDeclrationContext context)
        {

            var varName = context.Identifier().GetText();
            var type = context.GetChild(0).GetText();
            currentScope.assignParam(varName, Value.VOID);

            codeBuilder.LoadInstructions(2, codeBuilder.EmitLocals(varName, type.ToInt32()));


            return new Value(varName, type);
        }

        //visit assigment
        public override Value VisitAssignment(CompileParser.AssignmentContext context)
        {

            String varName = context.Identifier().GetText();
            Value isVariableInSymbolTable = currentScope.Resolve(varName);

            var value = this.Visit(context.expression());
            currentScope.Assign(varName, value);
            codeBuilder.LoadInstructions(3, OpCodes.StLoc, varName);
            return new Value(value);

        }
        public override Value VisitIdentifierExpression(IdentifierExpressionContext ctx)
        {


            var identifier = ctx.Identifier().GetText();
            var variable = currentScope.Resolve(identifier);
            if (variable != null)
            {

                if (variable != null && currentScope.FunctionArguments.Contains(identifier))
                {
                    if (!currentScope.Variables.ContainsKey(identifier) && variable.Type == "int32")
                    {
                        codeBuilder.LoadInstructions(2, OpCodes.LdInt4, variable.ToString());
                    }
                    else
                    {
                        codeBuilder.LoadInstructions(2, OpCodes.LdArg, ctx.Identifier().GetText());
                    }
                }

                else
                {
                    codeBuilder.LoadInstructions(2, OpCodes.LdLoc, ctx.Identifier().GetText());
                }
            }
            if (ctx.indexes() != null)
            {
                foreach (var index in ctx.indexes().expression())
                {
                    var x = this.Visit(index);

                }
            }

            return (variable == null || variable?.ToString() == "NULL") ? Value.VOID : new Value(variable);


        }


        //vist function declration
        public override Value VisitFunctionDecl(CompileParser.FunctionDeclContext context)
        {
            main += codeBuilder.GetCode();
            var functionScope = new Scope(currentScope, "function");
            currentScope = functionScope;
            currentScope.ReturnType = context.GetChild(6).GetText();
            var currentFunctionCall = fns.FirstOrDefault(fn => fn.FnName == context.Identifier().GetText());
            var @params = context.idList() != null ? context.idList().Identifier() : new List<ITerminalNode>().ToArray();

            for (int i = 0; i < @params.Length; ++i)
            {
                currentScope.assignParam(@params[i].GetText(), new Value(currentFunctionCall.Arguments[i].Value, currentFunctionCall.Arguments[i].Type));
                currentScope.FunctionArguments.Add(@params[i].GetText());

            }
            currentScope.ArgCount = @params.Length;
            codeBuilder.BuildMethod(currentFunctionCall.Arguments.Select(x => x.Type).ToArray(),
            currentScope.FunctionArguments.Select(x => x).ToArray(), context.Identifier().GetText(), currentScope.ReturnType);
            codeBuilder.LoadInstructions(2, codeBuilder.EmitLocals(currentFunctionCall.Arguments.Select(x => x.Type).ToArray()
             , currentScope.FunctionArguments.Select(x => x).ToArray())); ;

            Visit(context.block());
            codeBuilder.LoadInstructions(2, "ret");
            codeBuilder.LoadInstructions(2, "}");
            fn += codeBuilder.GetCode();

            currentScope = functionScope.Parent;
            functionScope = null;


            return Value.VOID;

        }


        public override Value VisitIdentifierFunctionCall(IdentifierFunctionCallContext ctx)
        {
            Function currentFn;
            Value value = Value.VOID;

            var isRecursive = fns.Any(x => x.FnName == ctx.Identifier().GetText());
            if (isRecursive)
            {
                currentFn = fns.FirstOrDefault(fn => fn.FnName == ctx.Identifier().GetText());
            }

            else
            {

                currentFn = new Function();
                currentFn.FnName = ctx.Identifier().GetText();

            }

            if (ctx.exprList() != null && !isRecursive)
            {
                //fill the function call

                foreach (var vdx in ctx?.exprList()?.expression())
                {

                    var z = Visit(vdx);
                    var symbol = new Symbol();
                    var functionParameter = vdx.GetText();
                    if (vdx is IdentifierExpressionContext context)
                    {
                        var varName = context.Identifier().GetText().Length > 1 ? context.Identifier().GetText().Substring(1) : context.Identifier().GetText();
                        var variable = currentScope.Resolve(functionParameter);
                        if (currentScope.Resolve(varName) != null)
                        {
                            symbol.Type = currentScope.Resolve(varName).IsNumber() ? "int32" : "string";
                            currentFn.ReturnType = symbol.Type;
                            currentFn.Arguments.Add(symbol);
                        }
                    }
                    else
                    {
                        functionParameter = functionParameter.Length > 1 ? functionParameter.Split("-")[0] : functionParameter;
                        if (currentScope.Resolve(functionParameter) != null)
                        {
                            symbol.Type = currentScope.Resolve(functionParameter).IsNumber() ? "int32" : "string";
                            var variable = currentScope.Resolve(functionParameter);
                            currentScope.Assign(functionParameter, new Value(variable, symbol.Type));
                            value = new Value(functionParameter, symbol.Type);

                        }
                        else
                        {
                            symbol.Type = new Value(functionParameter).IsNumber() ? "int32" : "string";
                        }

                        symbol.Value = functionParameter;
                        value = new Value(functionParameter, symbol.Type);
                        currentFn.Arguments.Add(symbol);
                    }

                }
                var parameterList = string.Join(",", currentFn?.Arguments.Select(x => x.Type).ToArray());

                if (ctx.exprList() != null)
                {


                    codeBuilder.LoadInstructions(2, $"call {currentFn?.Arguments[0].Type} Program::{ctx.Identifier().GetText()}({parameterList})");
                }
                else if (ctx.exprList() == null)
                {
                    codeBuilder.LoadInstructions(2, $"call {currentFn?.Arguments[0].Type}  Program::{ctx.Identifier().GetText()}()");
                }


            }
            else
            {
                //visit all the arguments

                foreach (var vdx in ctx?.exprList()?.expression())
                {
                    var z = Visit(vdx);
                }
                var parameterList = string.Join(",", currentFn?.Arguments.Select(x => x.Type).ToArray());
                codeBuilder.LoadInstructions(2, $"call {currentFn?.Arguments[0].Type} Program::{ctx.Identifier().GetText()}({parameterList})");
            }

            fns.Add(currentFn);

            return value;

        }
        public override Value VisitFunctionCallExpression(FunctionCallExpressionContext ctx)
        {
            var val = Visit(ctx.functionCall());
            if (ctx.indexes() != null)
            {

                foreach (var exp in ctx.indexes().expression())
                {
                    var x = Visit(exp);

                }
            }
            return new Value(val, val.Type);

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

            codeBuilder.LoadInstructions(2, "ldstr ", context.GetText());
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

                        if (left.Type == "int32" && right.Type == "int32")
                        {
                            return new Value(left);
                        }
                        return new Value(left.AsInt() + right.AsInt());

                    }

                    if ((left.IsString() || right.IsString()) || (right.IsNumber() || left.IsNumber()))
                    {
                        if (left.Type == "int32" || right.Type == "int32")
                        {

                            codeBuilder.LoadInstructions(2, OpCodes.Add);

                            return new Value(left);

                        }
                        else
                        {

                            codeBuilder.LoadInstructions(2, "call string string::Concat(string,string)");
                            return new Value(left.AsString() + right.AsString());
                        }


                    }
                    if (left.IsString() && right.IsString())
                    {
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
                    if ((left.IsNumber() || (left.Type == "int32") && (right.IsNumber() || right.Type == "int32")))
                    {

                        codeBuilder.LoadInstructions(2, OpCodes.Mul);
                        if (left.IsNumber() && right.IsNumber())
                            return new Value(left.AsInt() * right.AsInt());
                        else
                            return new Value(left);

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
            var value = Visit(context.expression());


            if (context.indexes() != null)
            {
                foreach (var exp in context.indexes().expression())
                {
                    value = Visit(exp);
                }
            }
            currentScope.Assign(context.expression().GetChild(0).GetText(), new Value(context.expression().GetChild(2).GetText()));
            return Value.VOID;

        }

        public override Value VisitNumberExpression(NumberExpressionContext ctx)
        {



            codeBuilder.LoadInstructions(2, OpCodes.LdInt4, ctx.Number().GetText());
            if (ctx.Parent.GetChild(0).GetText() == "return")
            {
                codeBuilder.LoadInstructions(2, OpCodes.Ret);
            }
            return new Value(ctx.Number().GetText());

        }


        private string MakeLabel(int label)
        {
            return string.Format("IL_{0:x4}", label);
        }
        public override Value VisitForStatement([NotNull] ForStatementContext context)
        {


            Visit(context.Identifier());

            string varName = context.Identifier().GetText();
            System.Console.WriteLine(currentScope.Resolve(varName));
            string start = context.expression(0).GetText();
            currentScope.Assign(varName, new Value(start));

            codeBuilder.LoadInstructions(2, OpCodes.LdInt4, start);

            var type = currentScope.Resolve(varName);

            var dateType = type.IsNumber() ? "int32" : "string";

            //emitlocal
            codeBuilder.LoadInstructions(2, codeBuilder.EmitLocals(context.Identifier().GetText(), dateType));
            codeBuilder.InitializeVariable(varName, start);
            //load start value
            labelPrev = MakeLabel(_labelCount);
            _labelCount++;
            codeBuilder.LoadInstructions(0, OpCodes.Br, labelPrev);
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
            codeBuilder.LoadInstructions(2, "stloc ", varName);

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
            codeBuilder.LoadInstructions(0, "brfalse ", labelElseIf);
            Visit(context.ifStat().block());
            codeBuilder.LoadInstructions(2, "br ", labelEnd);
            //emit all child of elseif
            codeBuilder.LoadInstructions(0, labelElseIf + ":");
            foreach (var item in context.elseIfStat())
            {
                //check expression
                Visit(item.expression());
                codeBuilder.LoadInstructions(0, "brfalse ", labelElse);
                Visit(item.block());
                codeBuilder.LoadInstructions(2, "br ", labelEnd);

            }
            //emit else
            codeBuilder.LoadInstructions(0, labelElse + ":");
            if (context.elseStat() != null)
            {
                Visit(context.elseStat().block());
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
            if (context.op.Text == "<=")
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