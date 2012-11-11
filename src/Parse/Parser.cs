using ScriptInterpreter.Util;
using System;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using ScriptInterpreter.RunTime;

namespace ScriptInterpreter.Parse
{
    public sealed class Parser : Tokens
    {
        private Scanner _scanner;

        public Scanner S
        {
            get { return _scanner; }
        }


        /**
         *     When terms are parsed, the mode determines which is expected:
         *     mode = EXPR        : an expression
         *     mode = TYPE        : a type
         *     mode = NOPARAMS    : no parameters allowed for type
         */
        private const int EXPR = 1;
        private const int TYPE = 2;
        private const int NOPARAMS = 4;

        private const int LValue = 8;

        /**
         * The current mode.
         */
        private int mode = 0;
        private int lastmode = 0;


        //语法树构造 
        private ExpMark _expMark = new ExpMark();

        public ExpMark E
        {
            get { return _expMark; }
        }

        public Parser(Scanner scanner)
        {
            _scanner = scanner;
        }

        internal Expression CompilationUnit()
        {
            Instructor.CompileIndexStack.LogIndex();

            funcReturnStack.Push(Expression.Label());

            BlockExpression body = BlockStatements();

            Instructor.CompileIndexStack.PopIndex();

            Expression t = E.At(S.Pos).CompilationUnit(new TupleStruct<Int32,bool>(0, false), functionList, body, funcReturnStack.Pop());

            functionList.Clear();

            return t;
        }

        private string Ident()
        {
            if (S.Token == IDENTIFIER)
            {
                string name = S.TokenString;
                S.NextToken();
                return name;
            }
            else
            {
                throw ScriptCompileException.CreateIsNotIdentifier(S.TokenString);
            }
        }

        private Expression VariableInitializer()
        {
            return S.Token == LBRACE ? TableInitializer(null) : GetExpression();
        }


        private KeyValuePair<string, Expression> GetTableField()
        {
            string key = null;

            if (S.IsTableValueKey() == true)
            {
                if (S.Token == IDENTIFIER)
                {
                    key = Ident();
                }
                else
                {
                    key = S.TokenString;
                    S.NextToken();
                }
                Accept(COLON);
            }
            return new KeyValuePair<string, Expression>(key, VariableInitializer());
        }


        private Expression TableInitializer(Expression t)
        {
            Accept(LBRACE);         //   {

            List<KeyValuePair<string, Expression>> elems = new List<KeyValuePair<string, Expression>>();

            if (S.Token != RBRACE)
            {
                elems.Add(GetTableField());

                while (S.Token == COMMA)
                {
                    S.NextToken();
                    elems.Add(GetTableField());
                }
            }

            Accept(RBRACE);        //  }

            return E.At(S.Pos).Table(t, elems);
        }

        public void Accept(int token)
        {
            if (token == SEMI)    //  ; 处理,可以不加;
            {
                if (S.Token == token) S.NextToken();
                return;
            }
            if (S.Token == token)
            {
                S.NextToken();
            }
            else
            {
                throw ScriptCompileException.CreateSyntaxError(S.Line,S.Col,S.Token,S.TokenString);
            }
        }
        private List<Expression> functionList = new List<Expression>();

        //用于return 语句返回
        private Stack<LabelTarget> funcReturnStack = new Stack<LabelTarget>();


        private Stack<List<KeyValuePair<string,Int32>>> _upValueStack = new  Stack<List<KeyValuePair<string,Int32>>>();

        public Stack<List<KeyValuePair<string, Int32>>> UpValueStack
        {
            get { return _upValueStack; }
        }

        private Expression MethodDeclaratorRest()
        {
            List<KeyValuePair<string, Int32>> upvalues = new List<KeyValuePair<string, Int32>>();

            UpValueStack.Push(upvalues);

            Instructor.CompileIndexStack.LogIndex();

            var info = FormalParameters();

            funcReturnStack.Push(Expression.Label());

            Accept(LBRACE);       //  {

            BlockExpression body = BlockStatements();     //方法体

            Accept(RBRACE);       //   }

            Instructor.CompileIndexStack.PopIndex();

            return E.At(S.Pos).MethodDef(info, body, funcReturnStack.Pop(), UpValueStack.Pop());
        }

        //获得Block
        private BlockExpression Block()
        {

            Accept(LBRACE);       //  {

            Instructor.CompileIndexStack.EnterBlock();

            BlockExpression t = BlockStatements();

            int count = Instructor.CompileIndexStack.OutBlock();

            Accept(RBRACE);       //   }

            return E.At(S.Pos).Block(t,count);
        }
        //c
        private BlockExpression BlockStatements()
        {
            List<Expression> stats = new List<Expression>();
            while (true)
            {
                switch (S.Token)   // } 
                {
                    case RBRACE:   // }

                    case EOF:
                        return Expression.Block(stats);

                    case LBRACE:     //  {
                    case IF:        //  if

                    case FOR:      // for

                    case WHILE:    // while

                    case DO:      // do

                    case RETURN:    //return

                    case BREAK:    //break;

                    case CONTINUE:

                        stats.Add(Statement());
                        break;

                    case LOCAL:
                        S.NextToken();

                        if (S.Token == IDENTIFIER)
                        {
                            stats.Add(VariableDeclaratorsRest(Ident()));
                        }
                        else if (S.Token == FUNCTION)
                        {
                            S.NextToken();
                            stats.Add(E.At(S.Pos).LocalVarDef(Ident(), MethodDeclaratorRest()));
                        }
                        else
                        {
                            throw ScriptCompileException.CreateSyntaxError(S.Line,S.Col,S.Token,S.TokenString);
                        }
                        Accept(SEMI);
                        break;

                    case FUNCTION:
                        S.NextToken();

                        functionList.Add(E.At(S.Pos).MethodDef(Ident(), MethodDeclaratorRest()));

                        break;

                    default:
                        Expression t = Term(EXPR | TYPE);

                        Contract.Assert(t != null);

                        stats.Add(t);

                        Accept(SEMI);
                        break;
                }
            }
        }

        private Expression VariableDeclaratorsRest(string localName)
        {
            List<Expression> vdefs = new List<Expression>();

            vdefs.Add(VariableDeclaratorRest(localName));

            while (S.Token == COMMA)
            {
                S.NextToken();
                vdefs.Add(VariableDeclaratorRest(Ident()));
            }
            return Expression.Block(vdefs);
        }

        private Expression VariableDeclaratorRest(string localName)
        {
            Expression init;

            if (S.Token == EQ)     //  =  复制
            {
                S.NextToken();

                init = VariableInitializer();
            }
            else
            {
                init = Instructor.GetNil().GetConstantExpression();
            }
            return E.At(S.Pos).LocalVarDef(localName, init);
        }
        // 用于解决嵌套循环
        private Stack<LabelTarget> loopLabelStack = new Stack<LabelTarget>();



        private Expression Statement()
        {
            switch (S.Token)
            {
                case LBRACE:
                    return Block();

                case IF:
                    {
                        S.NextToken();
                        Expression cond = ParExpression();
                        Expression thenpart = Statement();
                        Expression elsepart = null;
                        if (S.Token == ELSE)
                        {
                            S.NextToken();
                            elsepart = Statement();
                        }
                        return E.At(S.Pos).IfThenElse(cond, thenpart, elsepart);
                    }

                case FOR:
                    {
                        S.NextToken();

                        Accept(LPAREN);

                        Expression inits = S.Token == SEMI ? Expression.Empty() : GetExpression();

                        Accept(SEMI);

                        Expression cond = S.Token == SEMI ? Expression.Empty() : GetExpression();

                        Accept(SEMI);

                        Expression steps = S.Token == RPAREN ? Expression.Empty() : GetExpression();

                        Accept(RPAREN);

                        loopLabelStack.Push(Expression.Label());

                        Expression body = Statement();
                        return E.At(S.Pos).ForLoop(inits, cond, steps, body, loopLabelStack.Pop());

                    }

                case WHILE:
                    {
                        S.NextToken();

                        Expression cond = ParExpression();
                        loopLabelStack.Push(Expression.Label());
                        Expression body = Statement();
                        Expression t = E.At(S.Pos).WhileLoop(cond, body, loopLabelStack.Pop());
                        return t;
                    }

                case DO:
                    {
                        S.NextToken();

                        loopLabelStack.Push(Expression.Label());

                        Expression body = Statement();

                        Accept(WHILE);

                        Expression cond = ParExpression();

                        Expression t = E.At(S.Pos).DoWhileLoop(body, cond,loopLabelStack.Pop());

                        return t;

                    }

                case BREAK:
                    {
                        S.NextToken();
                        //应该先判断是否Peek()为空,以后在处理
                        Expression t = E.At(S.Pos).Break(loopLabelStack.Peek());
                        Accept(SEMI);
                        return t;
                    }

                case RETURN:
                    {
                        S.NextToken();
                        Expression result = S.Token == SEMI ? null : VariableInitializer();
                        //应该先判断是否Peek()为空,以后在处理
                        Expression t = E.At(S.Pos).Return(funcReturnStack.Peek(), result);
                        Accept(SEMI);
                        return t;
                    }
                case ELSE:
                    {
                        throw ScriptCompileException.CreateSyntaxError(S.Line, S.Col, S.Token, S.TokenString);
                    }
                default:
                    {
                        Expression t = GetExpression();
                        Accept(SEMI);
                        return t;
                    }
            }
        }


        //parExpression = "(" Expression ")"

        private Expression ParExpression()
        {
            Accept(LPAREN);

            Expression t = GetExpression();

            Accept(RPAREN);

            return t;
        }

        Expression Term(int newmode)
        {
            int prevmode = mode;
            mode = newmode;
            Expression t = Term();
            lastmode = mode;
            mode = prevmode;
            return t;
        }


        /*    处理一些二元操作(目前只处理了复制操作)
         *  Expression = Expression1 [ExpressionRest]
         *  ExpressionRest = [AssignmentOperator Expression1]
         *  AssignmentOperator = "=" | "+=" | "-=" | "*=" | "/=" |  "&=" | "|=" | "^=" | "%=" 
         *  Type = Type1
         *  TypeNoParams = TypeNoParams1
         *  StatementExpression = Expression
         *  ConstantExpression = Expression
         */

        private Expression Term()
        {
            Expression t = Term1();

            if ((mode & LValue) != 0)    //左值
            {
                mode = EXPR;
                Expression t1 = Term();
                return E.At(S.Pos).Assign(t, t1);
            }
            else if ((mode & EXPR) != 0 && S.Token == Link)
            {
                S.NextToken();
                Expression t1 = Term();
                return E.At(S.Pos).LinkStr(t, t1);
            }
            else if ((mode & EXPR) != 0 && S.Token == EQ || S.Token >= PLUSEQ && S.Token <= PERCENTEQ)
                return TermRest(t);
            else
                return t;

        }
        private Expression TermRest(Expression t)
        {
            switch (S.Token)
            {
                case PLUSEQ:

                case SUBEQ:

                case STAREQ:

                case SLASHEQ:

                case PERCENTEQ:

                case AMPEQ:

                case BAREQ:

                case CARETEQ:
                    break;

                default:
                    return t;

            }
            return t;
        }

        /*   处理  ?  :  三目运算符,目前不处理
         * Expression1   = Expression2 [Expression1Rest]
         *  Type1         = Type2
         *  TypeNoParams1 = TypeNoParams2
         */
        private Expression Term1()
        {
            Expression t = Term2();
            if ((mode & EXPR) != 0 & S.Token == QUES)
            {
                mode = EXPR;
                return Term1Rest(t);
            }
            else
            {
                return t;
            }
        }

        private Expression Term1Rest(Expression t)
        {
            if (S.Token == QUES)
            {
                int pos = S.Pos;
                S.NextToken();
                Expression t1 = Term();
                Accept(COLON);
                Expression t2 = Term1();
                return E.At(pos).Conditional(t, t1, t2);
            }
            else
            {
                return t;
            }
        }


        //  处理二元运算  + - * /  
        // 获取操作符左边的表达式
        private Expression Term2()
        {
            Expression t = Term3();

            //是右值表达式
            if ((mode & EXPR) != 0 && IsBinaryOperator(S.Token))
            {
                mode = EXPR;
                return Term2Rest(t, ExpInfo.OrPrec);
            }
            else
            {
                return t;
            }
        }
        //是否是二元操作符
        private bool IsBinaryOperator(int token)
        {
            return ExpInfo.OpPrec(token) != -1 ? true : false;
        }
        //操作数栈
        private Stack<Expression> odStack = new Stack<Expression>();

        //操作符号栈
        private Stack<int> opStack = new Stack<int>();

        //处理所有的二元运算,不错的算法,可以使用递推的函数调用代替,每进入下一个函数处理一段优先级,很明显,这样效率会很低,特别在"递归下降语法分析"中
        //而本算法会有优异的性能,通用、简单、高效、易扩展
        private Expression Term2Rest(Expression t, int minprec)
        {
            odStack.Push(t);            //操作数进栈
            int topOp = ERROR;
            while (Prec(S.Token) >= minprec)
            {
                opStack.Push(topOp);    //操作符进栈 
                topOp = S.Token;      //获取当前运算符
                S.NextToken();
                t = Term3();
                odStack.Push(t);
                while (odStack.Count > 0 && opStack.Count > 0 && Prec(topOp) >= Prec(S.Token))
                {
                    Expression t2 = odStack.Pop();
                    Expression t1 = odStack.Pop();
                    t = E.MakeOp(topOp, t1, t2);
                    odStack.Push(t);
                    topOp = opStack.Pop();
                }
            }
            t = odStack.Pop();
            odStack.Clear();
            opStack.Clear();
            return t;
        }

        private int Prec(int token)
        {
            return ExpInfo.OpPrec(token);   //返回操作符的优先级数
        }

        private Expression Term3()
        {
            Expression t = null;

            switch (S.Token)
            {
                case PLUSPLUS:           // 例如 ++i
                    S.NextToken();
                    t = Term3();
                    t = E.At(S.Pos).PosInc(t, true);
                    break;

                case SUBSUB:             //  例如  --i
                    S.NextToken();
                    t = Term3();
                    t = E.At(S.Pos).PosDec(t, true);
                    break;

                case THIS:
                    S.NextToken();

                    if (S.Token == EQ)
                    {
                        throw ScriptCompileException.CreateSyntaxError(S.Line, S.Col, S.Token, S.TokenString);
                    }
                    t = E.At(S.Pos).GetThisValue();

                    break;

                case FALSE:
                    Contract.Assert(S.TokenString == "false");
                    t = E.At(S.Pos).Flase();
                    S.NextToken();
                    break;

                case TRUE:
                    Contract.Assert(S.TokenString == "true");
                    t = E.At(S.Pos).True();
                    S.NextToken();
                    break;

                case NUMBER:                 //数字
                    t = E.At(S.Pos).Number(S.TokenString);
                    S.NextToken();

                    break;

                case STRINGLITERAL:           //字符串

                    t = E.At(S.Pos).String(S.TokenString);
                    S.NextToken();
                    break;


                case FUNCTION:          //函数

                    S.NextToken();

                    t = MethodDeclaratorRest();

                    break;

                case LBRACE:

                    t = TableInitializer(t);

                    break;

                case IDENTIFIER:

                    string ident = Ident();

                    if (S.Token == EQ)
                    {
                        mode = LValue;

                        S.NextToken();

                        if (S.Token != Nil)
                        {
                            t = E.At(S.Pos).Select(t, ident, true);
                        }
                        else
                        {
                            t = E.At(S.Pos).Remove(t, ident);
                            mode = EXPR;
                        }
                    }
                    else
                    {
                        t = E.At(S.Pos).Select(t, ident, false);

                        if (S.Token == LPAREN)
                        {
                            t = Arguments(t);
                        }
                    }
                    break;

                default:
                    throw ScriptCompileException.CreateSyntaxError(S.Line, S.Col, S.Token, S.TokenString);
            }
            while (true)
            {
                if (S.Token == DOT)
                {
                    S.NextToken();

                    string ident = Ident();

                    if (S.Token == EQ)
                    {
                        mode = LValue;

                        S.NextToken();

                        if (S.Token != Nil)
                        {
                            t = E.At(S.Pos).Select(t, ident, true);
                        }
                        else
                        {
                            t = E.At(S.Pos).Remove(t, ident);
                            mode = EXPR;
                        }
                    }
                    else if (S.Token == LPAREN)
                    {
                        Expression own = t;
                        t = E.At(S.Pos).Select(t, ident, false);
                        t = Arguments(own, t);
                    }
                    else
                    {
                        t = E.At(S.Pos).Select(t, ident, false);
                    }
                }
                else if (S.Token == LPAREN)    // (
                {
                    t = Arguments(t);
                }
                else if (S.Token == LBRACKET)   // [
                {
                    S.NextToken();

                    Expression t1 = Term();

                    Accept(RBRACKET);

                    if (S.Token == EQ)
                    {
                        mode = LValue;

                        S.NextToken();

                        if (S.Token != Nil)
                        {
                            t = E.At(S.Pos).Select(t, t1, true);
                        }
                        else
                        {
                            t = E.At(S.Pos).Remove(t, t1);
                            mode = EXPR;
                        }
                    }
                    else if (S.Token == LPAREN)
                    {
                        Expression own = t;
                        t = E.At(S.Pos).Select(t, t1, false);
                        t = Arguments(own, t);
                    }
                    else
                    {
                        t = E.At(S.Pos).Select(t, t1, false);
                    }
                }
                else
                {
                    break;
                }
            }
            while ((S.Token == PLUSPLUS || S.Token == SUBSUB) && (mode & EXPR) != 0)
            {
                mode = EXPR;
                //  t = E.At(S.Pos).Unary(S.Token == PLUSPLUS ? Tree.POSTINC : Tree.POSTDEC, t);
                if (S.Token == PLUSPLUS)    // 后加  例如 i++;
                {
                    t = E.At(S.Pos).PosInc(t, false);
                }
                else
                {
                    t = E.At(S.Pos).PosDec(t, false);
                }
                S.NextToken();
            }
            return t;
        }

        // Arguments = "(" [Expression { COMMA Expression }] ")"

        private List<Expression> Arguments()
        {
            int pos = S.Pos;
            List<Expression> args = new List<Expression>();
            if (S.Token == LPAREN)
            {
                S.NextToken();
                if (S.Token != RPAREN)
                {
                    args.Add(GetExpression());
                    while (S.Token == COMMA)
                    {
                        S.NextToken();
                        args.Add(GetExpression());
                    }
                }
                Accept(RPAREN);
            }
            else
            {
                throw ScriptCompileException.CreateSyntaxError(S.Line, S.Col, S.Token, S.TokenString);
            }
            return args;
        }
        private Expression Arguments(Expression own, Expression t)
        {
            int pos = S.Pos;
            List<Expression> args = Arguments();
            return E.At(pos).InvokeFunction(own, t, args);
        }


        private Expression Arguments(Expression t)
        {
            int pos = S.Pos;
            List<Expression> args = Arguments();
            return E.At(pos).InvokeFunction(t, args);
        }
        //是否是左值操作符
        private bool IsLOperator(int p)
        {
            switch (S.Token)
            {
                case EQ:

                case PLUSEQ:

                case SUBEQ:

                case STAREQ:

                case SLASHEQ:

                case PERCENTEQ:

                case AMPEQ:

                case BAREQ:

                case CARETEQ:
                    return true;

                default:
                    return false;
            }
        }

        private Expression GetExpression()
        {
            return Term(EXPR);
        }

        //  FormalParameters = "(" [FormalParameter {"," FormalParameter}] ")"
        private TupleStruct<Int32, bool> FormalParameters()
        {
            int count = 0;
            bool isUncertainParameters = false;

            Accept(LPAREN);     //   (

            if (S.Token != RPAREN)    // )
            {
                isUncertainParameters = FormalParameter();    //处理一个参数

                if (isUncertainParameters == false)
                {
                    count++;
                    while (S.Token == COMMA)
                    {
                        S.NextToken();

                        isUncertainParameters = FormalParameter();    //处理一个参数

                        if (!isUncertainParameters) count++;

                        else break;
                    }
                }
            }
            Accept(RPAREN);      // )

            return new TupleStruct<Int32, bool>() { First = count, Second = isUncertainParameters };

        }

        private bool FormalParameter()
        {
            if (S.Token == IDENTIFIER)            //标识符
            {
                Instructor.CompileIndexStack.AddSymbolVar(S.TokenString);

                S.NextToken();

                return false;
            }
            else if (S.Token == UntPar)
            {
                Instructor.CompileIndexStack.AddSymbolVar("args");
                S.NextToken();
                return true;
            }
            else
            {
                throw ScriptCompileException.CreateSyntaxError(S.Line, S.Col, S.Token, S.TokenString);
            }
        }
    }
}
