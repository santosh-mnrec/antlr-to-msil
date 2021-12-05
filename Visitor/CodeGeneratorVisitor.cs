using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using BLanguageMSILGenerator.CodeGenerator;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static BLanguageParser;

namespace BLanguageMSILGenerator
{

    public class CodeGeneratorVisitor : BLanguageBaseVisitor<Variable>
    {

        private readonly CodeBuilder _codeBuilder = new CodeBuilder();

        private string _mainMethod = "";
        private string _methodBody = "";
        private readonly string _moduleDefnition = "";
        //visit for loop
        private int _labelCount = 0;
        private string _decisionLabel = "";
        private Scope _currentScope = new Scope();

        private readonly List<Function> _functions = new List<Function>();

        public CodeGeneratorVisitor( )
        {
            _codeBuilder.Init();
            _moduleDefnition = _codeBuilder.GetCode();

        }

        public override Variable VisitParse([NotNull] ParseContext context)
        {

            var labelTo = _codeBuilder.MakeLabel(_labelCount);
            _labelCount++;
            _ = Visit(context.block());
            _codeBuilder.LoadInstructions(2, _mainMethod);
            _codeBuilder.EmitTry(labelTo);
            _codeBuilder.EmitCatch(labelTo);
            _codeBuilder.LoadInstructions(2, _methodBody);
            var code = _moduleDefnition + _codeBuilder.GetCode() + "\n}";
            File.WriteAllText(@"C:\temp\test.il", code);
            return Variable.VOID;
        }

        public override Variable VisitPrintlnFunctionCall([NotNull] PrintlnFunctionCallContext context)
        {
            Visit(context.expression());
            _codeBuilder.EmitInBuiltFunctionCall("WriteLine", _codeBuilder.DataTypes[context.GetChild(2).GetText()]);
            return Variable.VOID;
        }

        public override Variable VisitPrintFunctionCall([NotNull] PrintFunctionCallContext context)
        {
            Visit(context.expression());
            _codeBuilder.EmitInBuiltFunctionCall("Write", _codeBuilder.DataTypes[context.GetChild(2).GetText()]);
            return Variable.VOID;
        }
        public override Variable VisitBlock(BlockContext context)
        {

            foreach (var statement in context.statement())
            {
                this.Visit(statement);

            }
            foreach (var functionDecl in context.functionDecl())
            {
                this.Visit(functionDecl);
            }

            if (context.expression() == null)
            {
                return Variable.VOID;
            }
            var returnValue = Visit(context.expression());
            if (!Equals(returnValue, Variable.VOID))
            {

                _codeBuilder.LoadInstructions(2, OpCodes.Ret);
            }

            return returnValue;
        }

        public override Variable VisitVarDeclaration([NotNull] VarDeclarationContext context)
        {

            var varName = context.Identifier().GetText();
            var type = context.GetChild(0).GetText();
            _currentScope.AssignParameter(varName, Variable.VOID);
            _codeBuilder.LoadInstructions(1, _codeBuilder.EmitLocals(varName, type));
            return new Variable(varName, type);
        }

        //visit assigment
        public override Variable VisitAssignment(AssignmentContext context)
        {

            var varName = context.Identifier().GetText();
            var value = this.Visit(context.expression());
            _currentScope.Assign(varName, value);
            _codeBuilder.LoadInstructions(3, OpCodes.StLoc, varName);
            return new Variable(value);

        }
        public override Variable VisitIdentifierExpression(IdentifierExpressionContext ctx)
        {

            var identifier = ctx.Identifier().GetText();
            var variable = _currentScope.Resolve(identifier);
            if (variable != null)
            {
                if (_currentScope.LocalVariables.ContainsKey(identifier))
                {
                    _codeBuilder.LoadInstructions(2, OpCodes.LdArg, ctx.Identifier().GetText());
                }

                else
                {
                    _codeBuilder.LoadInstructions(2, OpCodes.LdLoc, ctx.Identifier().GetText());
                }
            }
            return (variable == null || variable?.ToString() == "NULL") ? Variable.VOID : new Variable(variable);

        }

        //vist function declration
        public override Variable VisitFunctionDecl(FunctionDeclContext context)
        {
            _mainMethod += _codeBuilder.GetCode();
            var functionScope = new Scope(_currentScope, "function");
            _currentScope = functionScope;
            _currentScope.ReturnType = context.GetChild(6).GetText();
            var currentFunctionCall = _functions.FirstOrDefault(fn => fn.Name == context.Identifier().GetText());
            var @params = context.idList() != null ? context.idList().Identifier() : new List<ITerminalNode>().ToArray();
            if (currentFunctionCall != null)
            {
                for (int i = 0; i < @params.Length; ++i)
                {
                    _currentScope.AssignParameter(@params[i]?.GetText(), new Variable(currentFunctionCall.Arguments[i].value, currentFunctionCall.Arguments[i].Type));
                    _currentScope.LocalVariables.Add(@params[i].GetText(), new Variable(currentFunctionCall.Arguments[i].value, currentFunctionCall.Arguments[i].Type));

                }


                //  _currentScope.ArgCount = @params.Length;
                _codeBuilder.BuildMethod(currentFunctionCall.Arguments.Select(x => x.Type).ToArray(),
                _currentScope.LocalVariables.Select(x => x.Key).ToArray(), context.Identifier().GetText(), _currentScope.ReturnType);
                _codeBuilder.LoadInstructions(2, _codeBuilder.EmitLocals(currentFunctionCall.Arguments.Select(x => x.Type).ToArray()
                 , _currentScope.LocalVariables.Select(x => x.Key).ToArray())); ;
            }
            Visit(context.block());
            _codeBuilder.LoadInstructions(2, "}");
            _methodBody += _codeBuilder.GetCode();
            _functions.Remove(currentFunctionCall);
            _currentScope = functionScope.Parent;

            return Variable.VOID;

        }

        public override Variable VisitReadFileCall([NotNull] ReadFileCallContext context)
        {

            _codeBuilder.EmitHttpClientStart(context.Identifier().GetText());
            _codeBuilder.EmitHttpClientEnd(context.Identifier().GetText());
            return Variable.VOID;
        }
        public override Variable VisitIdentifierFunctionCall(IdentifierFunctionCallContext ctx)
        {
            Function currentFn;
            var value = Variable.VOID;
            var isRecursive = _functions.Any(x => x.Name == ctx.Identifier().GetText());
            currentFn = isRecursive
                ? _functions.FirstOrDefault(fn => fn.Name == ctx.Identifier().GetText())
                : new Function { Name = ctx.Identifier().GetText() };

            if (ctx.exprList() == null || isRecursive)
            {
                HandleRecursion(ctx, currentFn);
            }
            else
            {

                foreach (var vdx in ctx?.exprList()?.expression())
                {

                    var functionArgument = Visit(vdx);
                    var argumentValue = vdx.GetText();
                    if (_currentScope.Resolve(argumentValue) != null)
                    {
                        var resolveValue = _currentScope.Resolve(argumentValue);
                        functionArgument.Type = resolveValue.Type == null ? resolveValue.GetDataType() : _codeBuilder.DataTypes[resolveValue.Type];
                        functionArgument.value = _currentScope.Resolve(argumentValue).value;
                    }
                    else
                    {
                        functionArgument.Type = new Variable(argumentValue).GetDataType();
                    }
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
            _functions.Add(currentFn);
            return value;

        }

        private void HandleRecursion(IdentifierFunctionCallContext ctx, Function currentFn)
        {
            foreach (var vdx in ctx?.exprList()?.expression())
            {
                Visit(vdx);
            }
            var parameterList = string.Join(",", currentFn?.Arguments.Select(x => x.Type).ToArray() ?? Array.Empty<string>());
            _codeBuilder.LoadInstructions(2, $"call {currentFn?.Arguments[0].Type} Program::{ctx.Identifier().GetText()}({parameterList})");
        }

        public override Variable VisitFunctionCallExpression(FunctionCallExpressionContext ctx)
        {
            var val = Visit(ctx.functionCall());
            return new Variable(val, val.Type);
        }

        //visit add expression
        public override Variable VisitStringExpression([NotNull] StringExpressionContext context)
        {

            _codeBuilder.LoadInstructions(2, OpCodes.LdStr, context.GetText());
            return new Variable(context.GetText());
        }
        public override Variable VisitAddExpression([NotNull] AddExpressionContext context)
        {

            //switch case on operator
            switch (context.op.Type)
            {
                case Add:

                    var left = this.Visit(context.expression(0));
                    var right = this.Visit(context.expression(1));
                    if (_currentScope.ReturnType == "int")
                    {

                        //call add
                        _codeBuilder.LoadInstructions(2, OpCodes.Add);
                        return new Variable("int");
                    }
                    //flat+float
                    if (left.IsFloat() && right.IsFloat())
                    {

                        _codeBuilder.LoadInstructions(2, OpCodes.Add);

                        return left.Type switch
                        {
                            "float32" when right.Type == "float32" => new Variable(left),
                            _ => new Variable(left.ToFloat() + right.ToFloat())
                        };

                    }

                    //int+int
                    if (left.IsNumber() && right.IsNumber())
                    {

                        _codeBuilder.LoadInstructions(2, OpCodes.Add);

                        return left.Type switch
                        {
                            "int32" when right.Type == "int32" => new Variable(left),
                            _ => new Variable(left.ToInteger() + right.ToInteger())
                        };

                    }
                    // string+any
                    if ((left.IsString()))
                    {

                        if (right.IsNumber())
                        {
                            _codeBuilder.LoadInstructions(2, "call instance  string [mscorlib]System.Int32::ToString()");
                        }
                        _codeBuilder.LoadInstructions(2, "call string string::Concat(string,string)");

                        return new Variable(left.ToStr() + right.ToStr());


                    }
                    if (right.IsString())
                    {

                        if (left.IsNumber())
                        {
                            _codeBuilder.LoadInstructions(2, "call instance  string [mscorlib]System.Int32::ToString()");
                        }
                        _codeBuilder.LoadInstructions(2, "call string string::Concat(string,string)");


                        return new Variable(left.ToStr() + right.ToStr());

                    }

                    break;

                case Subtract:
                    var l = Visit(context.expression(0));
                    var r = Visit(context.expression(1));
                    if (l.IsNumber() && r.IsNumber())
                    {

                        _codeBuilder.LoadInstructions(2, OpCodes.Sub);


                        return new Variable(l.ToInteger() + r.ToInteger());

                    }
                    break;

                default:

                    break;
            }
            return Variable.VOID;

        }

        public override Variable VisitMultExpression([NotNull] MultExpressionContext context)
        {

            var left = this.Visit(context.expression(0));
            var right = this.Visit(context.expression(1));
            // //swithc case on op.Text
            switch (context.op.Text)
            {
                case "*":

                    //if both are ints
                    if ((left.IsNumber()
                         || (left.Type == "int32")
                         && (right.IsNumber() || right.Type == "int32")))
                    {

                        _codeBuilder.LoadInstructions(2, OpCodes.Mul);
                        if (left.IsNumber() && right.IsNumber())
                        {
                            return new Variable(left.ToInteger() * right.ToInteger());
                        }
                        else
                        {
                            return new Variable(left);
                        }
                    }
                    break;
                case "/":

                    if (left.IsNumber() && right.IsNumber())
                    {
                        _codeBuilder.LoadInstructions(2, OpCodes.Div);
                    }
                    else
                    {
                        throw new Exception("Invalid data type");
                    }
                    break;
                case "%":

                    if (left.IsNumber() && right.IsNumber())
                    {

                        _codeBuilder.LoadInstructions(2, OpCodes.Rem);
                        return new Variable(left.ToInteger() + right.ToInteger());

                    }

                    break;
                default:
                    break;
            }


            return Variable.VOID;

        }
        public override Variable VisitExpressionExpression(ExpressionExpressionContext context)
        {
            Visit(context.expression());
            _currentScope.Assign(context.expression().GetChild(0).GetText(), new Variable(context.expression().GetChild(2).GetText()));
            return Variable.VOID;

        }

        public override Variable VisitNumberExpression(NumberExpressionContext ctx)
        {

            if (ctx.GetChild(0).GetText().Contains("."))
            {
                _codeBuilder.LoadInstructions(2, OpCodes.LdFloat, ctx.GetChild(0).GetText());
            }
            else if (!ctx.GetChild(0).GetText().Contains("."))
            {

                _codeBuilder.LoadInstructions(2, OpCodes.LdInt4, ctx.Number().GetText());
            }
            else if (ctx.Parent.GetChild(0).GetText() == "return")
            {
                _codeBuilder.LoadInstructions(2, OpCodes.Ret);
            }
            return new Variable(ctx.Number().GetText());

        }



        public override Variable VisitForStatement([NotNull] ForStatementContext context)
        {


            Visit(context.Identifier());
            string varName = context.Identifier().GetText();
            System.Console.WriteLine(_currentScope.Resolve(varName));
            string start = context.expression(0).GetText();
            _currentScope.Assign(varName, new Variable(start));
            _codeBuilder.LoadInstructions(2, OpCodes.LdInt4, start);
            var type = _currentScope.Resolve(varName);
            var dateType = type.Type ?? "int32";
            //emitlocal
            _codeBuilder.LoadInstructions(1, _codeBuilder.EmitLocals(context.Identifier().GetText(), dateType));
            _codeBuilder.InitializeVariable(varName, start);
            //load start value
            _decisionLabel = _codeBuilder.MakeLabel(_labelCount);
            _labelCount++;
            _codeBuilder.LoadInstructions(0, OpCodes.Br, _decisionLabel);
            string labelTo = _codeBuilder.MakeLabel(_labelCount);
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
            _codeBuilder.LoadInstructions(0, _decisionLabel + ":");
            _codeBuilder.LoadInstructions(2, "ldloc " + varName);
            Visit(context.expression(1));
            //compare
            _codeBuilder.LoadInstructions(2, "clt ");
            _codeBuilder.LoadInstructions(0, OpCodes.Brtrue + labelTo);
            return new Variable("int32");

        }

        public override Variable VisitIfStatement([NotNull] IfStatementContext context)
        {

            //generate lable for if elseif* else
            _codeBuilder.MakeLabel(_labelCount);
            _labelCount++;
            var labelElse = _codeBuilder.MakeLabel(_labelCount);
            _labelCount++;
            var labelElseIf = _codeBuilder.MakeLabel(_labelCount);
            _labelCount++;
            var labelEnd = _codeBuilder.MakeLabel(_labelCount);
            _labelCount++;

            //emit if
            Visit(context.ifStat().expression());
            _codeBuilder.LoadInstructions(0, OpCodes.Brfalse, labelElseIf);
            Visit(context.ifStat().block());
            _codeBuilder.LoadInstructions(2, "br ", labelEnd);
            //emit all child of elseif
            _codeBuilder.LoadInstructions(0, labelElseIf + ":");
            foreach (var item in context.elseIfStat())
            {
                //check expression
                Visit(item.expression());
                _codeBuilder.LoadInstructions(0, OpCodes.Brfalse, labelElse);
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

            return Variable.VOID;


        }


        public override Variable VisitCompExpression([NotNull] CompExpressionContext context)
        {
            //switch case
            if (context.op.Text == "==")
            {
                var l = Visit(context.expression(0));
                var r = Visit(context.expression(1));
                _codeBuilder.LoadInstructions(2, OpCodes.Ceq);
                return new Variable(l.ToInteger() == r.ToInteger());
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
            return Variable.VOID;

        }
        public override Variable VisitEqExpression([NotNull] EqExpressionContext context)
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
            return Variable.VOID;
        }
        #region Helper


        #endregion

    }
}