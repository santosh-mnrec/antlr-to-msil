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

        private readonly CodeBuilder _codeBuilder = new CodeBuilder();

        private string _main = "";
        private string _fn = "";
        private string _header = "";
        //visit for loop
        private int _labelCount = 0;
        private string _labelPrev = "";
        private Scope _currentScope = new Scope();

        private List<Function> _fns = new List<Function>();

        public CodeGeneratorVisitor()
        {
            _codeBuilder.Init();


            _header = _codeBuilder.GetCode();

        }


        public override Value VisitParse([NotNull] ParseContext context)
        {

            var labelTo = MakeLabel(_labelCount);
            _labelCount++;

            _ = Visit(context.block());

            _codeBuilder.LoadInstructions(2, _main);
            _codeBuilder.EmitTryCatch(labelTo);
            _codeBuilder.EmitCatchIL(labelTo);

            _codeBuilder.LoadInstructions(2, _fn);

            var code = _header + _codeBuilder.GetCode() + "\n}";

            File.WriteAllText(@"out\test.il", code);
            return Value.VOID;
        }

        public override Value VisitPrintlnFunctionCall([NotNull] PrintlnFunctionCallContext context)
        {


            Visit(context.expression());
            System.Console.WriteLine($"Executed at {DateTime.UtcNow}");
            if (context.GetChild(2).GetText().Contains("%d"))
            {
                _codeBuilder.EmitInBuiltFunctionCall("int32");

            }
            else if (context.GetChild(2).GetText().Contains("%s"))
            {
                _codeBuilder.EmitInBuiltFunctionCall("string");

            }

            return Value.VOID;
        }
        public override Value VisitBlock(CompileParser.BlockContext context)
        {

            foreach (var statement in context.statement())
            {
                this.Visit(statement);

            }

            foreach (var functionDecl in context.functionDecl())
            {
                this.Visit(functionDecl);

            }

            if (context.expression() == null) return Value.VOID;
            var returnValue = Visit(context.expression());
            if (!Equals(returnValue, Value.VOID))
            {

                _codeBuilder.LoadInstructions(2, OpCodes.Ret);

            }

            return returnValue;
        }

        public override Value VisitVarDeclration([NotNull] VarDeclrationContext context)
        {

            var varName = context.Identifier().GetText();
            var type = context.GetChild(0).GetText();
            _currentScope.assignParam(varName, Value.VOID);

            _codeBuilder.LoadInstructions(2, _codeBuilder.EmitLocals(varName, type.ToInt32()));


            return new Value(varName, type);
        }

        //visit assigment
        public override Value VisitAssignment(CompileParser.AssignmentContext context)
        {

            String varName = context.Identifier().GetText();
            var value = this.Visit(context.expression());
            _currentScope.Assign(varName, value);
            _codeBuilder.LoadInstructions(3, OpCodes.StLoc, varName);
            return new Value(value);

        }
        public override Value VisitIdentifierExpression(IdentifierExpressionContext ctx)
        {

            var identifier = ctx.Identifier().GetText();
            var variable = _currentScope.Resolve(identifier);
            if (variable != null)
            {
                if (_currentScope.LocalVariables.ContainsKey(identifier))
                {
                    switch (_currentScope.Variables.ContainsKey(identifier))
                    {
                        case false when variable.Type == "int32":
                            _codeBuilder.LoadInstructions(2, OpCodes.LdInt4, variable.ToString());
                            break;
                        default:
                            _codeBuilder.LoadInstructions(2, OpCodes.LdArg, ctx.Identifier().GetText());
                            break;
                    }
                }

                else
                {
                    _codeBuilder.LoadInstructions(2, OpCodes.LdLoc, ctx.Identifier().GetText());
                }
            }


            return (variable == null || variable?.ToString() == "NULL") ? Value.VOID : new Value(variable);


        }


        //vist function declration
        public override Value VisitFunctionDecl(CompileParser.FunctionDeclContext context)
        {
            _main += _codeBuilder.GetCode();
            var functionScope = new Scope(_currentScope, "function");
            _currentScope = functionScope;
            _currentScope.ReturnType = context.GetChild(6).GetText();
            var currentFunctionCall = _fns.FirstOrDefault(fn => fn.Name == context.Identifier().GetText());
            var @params = context.idList() != null ? context.idList().Identifier() : new List<ITerminalNode>().ToArray();

            for (int i = 0; i < @params.Length; ++i)
            {
                _currentScope.assignParam(@params[i].GetText(), new Value(currentFunctionCall.Arguments[i].value, currentFunctionCall.Arguments[i].Type));
                _currentScope.LocalVariables.Add(@params[i].GetText(), new Value(currentFunctionCall.Arguments[i].value, currentFunctionCall.Arguments[i].Type));

            }

            _currentScope.ArgCount = @params.Length;
            _codeBuilder.BuildMethod(currentFunctionCall.Arguments.Select(x => x.Type).ToArray(),
            _currentScope.LocalVariables.Select(x => x.Key).ToArray(), context.Identifier().GetText(), _currentScope.ReturnType);
            _codeBuilder.LoadInstructions(2, _codeBuilder.EmitLocals(currentFunctionCall.Arguments.Select(x => x.Type).ToArray()
             , _currentScope.LocalVariables.Select(x => x.Key).ToArray())); ;

            Visit(context.block());
            _codeBuilder.LoadInstructions(2, "ret");
            _codeBuilder.LoadInstructions(2, "}");
            _fn += _codeBuilder.GetCode();
            _fns.Remove(currentFunctionCall);
            _currentScope = functionScope.Parent;
            functionScope = null;


            return Value.VOID;

        }

        public override Value VisitHttpCall([NotNull] HttpCallContext context)
        {

            _codeBuilder.EmitHttpClientStart(context.Identifier().GetText());
            _codeBuilder.EmitHttpClientEnd(context.Identifier().GetText());
            return Value.VOID;
        }
        public override Value VisitIdentifierFunctionCall(IdentifierFunctionCallContext ctx)
        {
            Function currentFn;
            var value = Value.VOID;

            var isRecursive = _fns.Any(x => x.Name == ctx.Identifier().GetText());
            if (isRecursive)
            {
                currentFn = _fns.FirstOrDefault(fn => fn.Name == ctx.Identifier().GetText());
            }

            else
            {

                currentFn = new Function {Name = ctx.Identifier().GetText()};

            }

            if (ctx.exprList() != null && !isRecursive)
            {

                foreach (var vdx in ctx?.exprList()?.expression())
                {

                    var functionArgument = Visit(vdx);
                    var argumentValue = vdx.GetText();
                    if (_currentScope.Resolve(argumentValue) != null)
                    {
                        _currentScope.Resolve(argumentValue);
                    }
                    else
                    {

                        functionArgument.Type = new Value(argumentValue).IsNumber() ? "int32" : "string";
                    }

                    functionArgument.value = argumentValue;

                    currentFn.Arguments.Add(functionArgument);
                }


                var parameterList = string.Join(",", currentFn?.Arguments.Select(x => x.Type).ToArray());

                if (ctx.exprList() != null)
                {


                    _codeBuilder.LoadInstructions(2, $"call {currentFn?.Arguments[0].Type} Program::{ctx.Identifier().GetText()}({parameterList})");
                }
                else if (ctx.exprList() == null)
                {
                    _codeBuilder.LoadInstructions(2, $"call {currentFn?.Arguments[0].Type}  Program::{ctx.Identifier().GetText()}()");
                }


            }
            else
            {
                HandleRecursion(ctx, currentFn);
            }
            _fns.Add(currentFn);

            return value;

        }

        private void HandleRecursion(IdentifierFunctionCallContext ctx, Function currentFn)
        {
            //visit all the arguments

            foreach (var vdx in ctx?.exprList()?.expression())
            {
                var z = Visit(vdx);
            }
            var parameterList = string.Join(",", currentFn?.Arguments.Select(x => x.Type).ToArray());
            _codeBuilder.LoadInstructions(2, $"call {currentFn?.Arguments[0].Type} Program::{ctx.Identifier().GetText()}({parameterList})");
        }

        public override Value VisitFunctionCallExpression(FunctionCallExpressionContext ctx)
        {
            var val = Visit(ctx.functionCall());

            return new Value(val, val.Type);

        }


        //visit add expression

        public override Value VisitStringExpression([NotNull] StringExpressionContext context)
        {

            _codeBuilder.LoadInstructions(2, "ldstr ", context.GetText());
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

                        _codeBuilder.LoadInstructions(2, OpCodes.Add);

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

                            _codeBuilder.LoadInstructions(2, OpCodes.Add);

                            return new Value(left);

                        }
                        else
                        {

                            _codeBuilder.LoadInstructions(2, "call string string::Concat(string,string)");
                            return new Value(left.AsString() + right.AsString());
                        }


                    }
                    if (left.IsString() && right.IsString())
                    {
                        _codeBuilder.LoadInstructions(2, "call string string::Concat(string,string)");

                        return new Value(left.AsString() + right.AsString());

                    }

                    break;

                case CompileParser.Subtract:
                    var l = Visit(context.expression(0));
                    var r = Visit(context.expression(1));
                    if (l.IsNumber() && r.IsNumber())
                    {

                        _codeBuilder.LoadInstructions(2, OpCodes.Sub);


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

                        _codeBuilder.LoadInstructions(2, OpCodes.Mul);
                        if (left.IsNumber() && right.IsNumber())
                            return new Value(left.AsInt() * right.AsInt());
                        else
                            return new Value(left);

                    }
                    break;
                case "/":
                    Visit(context.expression(0));
                    Visit(context.expression(1));
                    _codeBuilder.LoadInstructions(2, OpCodes.Div);
                    break;
                case "%":
                    var l = Visit(context.expression(0));
                    var r = Visit(context.expression(1));
                    if (l.IsNumber() && r.IsNumber())
                    {

                        _codeBuilder.LoadInstructions(2, OpCodes.Rem);
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



            _currentScope.Assign(context.expression().GetChild(0).GetText(), new Value(context.expression().GetChild(2).GetText()));
            return Value.VOID;

        }

        public override Value VisitNumberExpression(NumberExpressionContext ctx)
        {



            _codeBuilder.LoadInstructions(2, OpCodes.LdInt4, ctx.Number().GetText());
            if (ctx.Parent.GetChild(0).GetText() == "return")
            {
                _codeBuilder.LoadInstructions(2, OpCodes.Ret);
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
            System.Console.WriteLine(_currentScope.Resolve(varName));
            string start = context.expression(0).GetText();
            _currentScope.Assign(varName, new Value(start));

            _codeBuilder.LoadInstructions(2, OpCodes.LdInt4, start);

            var type = _currentScope.Resolve(varName);

            var dateType = type.Type ?? "int32";

            //emitlocal
            _codeBuilder.LoadInstructions(2, _codeBuilder.EmitLocals(context.Identifier().GetText(), dateType));
            _codeBuilder.InitializeVariable(varName, start);
            //load start value
            _labelPrev = MakeLabel(_labelCount);
            _labelCount++;
            _codeBuilder.LoadInstructions(0, OpCodes.Br, _labelPrev);
            string labelTo = MakeLabel(_labelCount);
            _labelCount++;
            _labelCount++;

            //labeto
            _codeBuilder.LoadInstructions(0, labelTo + ":");
            _codeBuilder.LoadInstructions(2, OpCodes.LdLoc + varName);
            _codeBuilder.LoadInstructions(2, "ldc.i4 1 ");


            //statemtn
            Visit(context.block());
            _codeBuilder.LoadInstructions(2, OpCodes.Add);
            _codeBuilder.LoadInstructions(2, "stloc ", varName);

            _codeBuilder.LoadInstructions(0, _labelPrev + ":");
            _codeBuilder.LoadInstructions(2, "ldloc " + varName);
            Visit(context.expression(1));

            //compare
            _codeBuilder.LoadInstructions(2, "clt ");

            _codeBuilder.LoadInstructions(0, "brtrue " + labelTo);


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
            _codeBuilder.LoadInstructions(0, "brfalse ", labelElseIf);
            Visit(context.ifStat().block());
            _codeBuilder.LoadInstructions(2, "br ", labelEnd);
            //emit all child of elseif
            _codeBuilder.LoadInstructions(0, labelElseIf + ":");
            foreach (var item in context.elseIfStat())
            {
                //check expression
                Visit(item.expression());
                _codeBuilder.LoadInstructions(0, "brfalse ", labelElse);
                Visit(item.block());
                _codeBuilder.LoadInstructions(2, "br ", labelEnd);

            }
            //emit else
            _codeBuilder.LoadInstructions(0, labelElse + ":");
            if (context.elseStat() != null)
            {
                Visit(context.elseStat().block());
            }
            // //emit end
            _codeBuilder.LoadInstructions(0, labelEnd + ":");

            return Value.VOID;


        }


        public override Value VisitCompExpression([NotNull] CompExpressionContext context)
        {
            //switch case
            if (context.op.Text == "==")
            {
                var l = Visit(context.expression(0));
                var r = Visit(context.expression(1));
                _codeBuilder.LoadInstructions(2, OpCodes.Ceq);
                return new Value(l.AsInt() == r.AsInt());
            }
            if (context.op.Text == "!=")
            {
                Visit(context.expression(0));
                Visit(context.expression(1));
                _codeBuilder.LoadInstructions(2, OpCodes.Ceq);
                _codeBuilder.LoadInstructions(2, OpCodes.LdInt4 + "0");
                _codeBuilder.LoadInstructions(2, OpCodes.Ceq);
            }
            if (context.op.Text == ">")
            {
                Visit(context.expression(0));
                Visit(context.expression(1));
                _codeBuilder.LoadInstructions(2, OpCodes.Cgt);
            }
            if (context.op.Text == "<")
            {
                Visit(context.expression(0));
                Visit(context.expression(1));
                _codeBuilder.LoadInstructions(2, OpCodes.Clt);
            }
            if (context.op.Text == ">=")
            {
                Visit(context.expression(0));
                Visit(context.expression(1));
                _codeBuilder.LoadInstructions(2, OpCodes.Cgt_Un);
                _codeBuilder.LoadInstructions(2, OpCodes.LdInt4 + "0");
                _codeBuilder.LoadInstructions(2, OpCodes.Ceq);
            }
            if (context.op.Text == "<=")
            {
                Visit(context.expression(0));
                Visit(context.expression(1));
                _codeBuilder.LoadInstructions(2, OpCodes.Cgt_Un);
                _codeBuilder.LoadInstructions(2, OpCodes.LdInt4 + "0");
                _codeBuilder.LoadInstructions(2, OpCodes.Ceq);
            }
            return Value.VOID;

        }
        public override Value VisitEqExpression([NotNull] EqExpressionContext context)
        {

            if (context.op.Text == "==")
            {
                Visit(context.expression(0));
                Visit(context.expression(1));
                _codeBuilder.LoadInstructions(2, OpCodes.Ceq);
            }
            if (context.op.Text == "!=")
            {
                Visit(context.expression(0));
                Visit(context.expression(1));
                _codeBuilder.LoadInstructions(2, OpCodes.Ceq);
                _codeBuilder.LoadInstructions(2, OpCodes.LdInt4 + "0");
                _codeBuilder.LoadInstructions(2, OpCodes.Ceq);
            }
            return Value.VOID;
        }
        #region Helper


        #endregion

    }
}