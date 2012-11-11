using System;
using System.Linq.Expressions;
using ScriptInterpreter.RunTime;
using System.Collections.Generic;
using ScriptInterpreter.Parse;
using System.Diagnostics.Contracts;


namespace ScriptInterpreter.Util
{
    public class ExpMark : Tokens
    {
        private int pos;

        internal ExpMark At(int p)
        {
            pos = p;
            return this;
        }

        internal Expression CompilationUnit(TupleStruct<Int32,bool> info,List<Expression> funcList, Expression body, LabelTarget label)
        {
            BlockExpression e = funcList.Count == 0 ? Expression.Block(body) : Expression.Block(Expression.Block(funcList), body);

            return MethodDef(info, e, label, null);
        }

        /// <summary>
        ///    
        /// </summary>
        /// <param name="t"></param>
        /// <param name="count">要退栈的元素数</param>
        /// <returns></returns>
        internal BlockExpression Block(BlockExpression t, int count)
        {
            Contract.Assert(count>=0);

            if(count == 0)
            {
                return t;
            }
            return Expression.Block(t, Expression.Call(typeof(Instructor).GetMethod("StackPop"),count.GetConstantExpression()));
        }

        /// <summary>
        ///    局部变量定义
        /// </summary>
        /// <param name="localName">局部变量名</param>
        /// <param name="init">初始化的值</param>
        /// <param name="IsInMethod">是外部变量,还是函数内变量</param>
        /// <returns>将当前变量压入栈的方法表达式</returns>
        internal Expression LocalVarDef(string localName, Expression init)
        {
            Instructor.CompileIndexStack.AddSymbolVar(localName);

            return Expression.Call(typeof(Instructor).GetMethod("StackPush"), init);
        }

        //数字常量
        internal Expression Number(string num)
        {
            return ScriptObject.CreateNum(double.Parse(num)).GetConstantExpression();
        }

        internal Expression Assign(Expression t, Expression t1)
        {
            return Expression.Call(typeof(Instructor).GetMethod("AssignVar"), t, t1);
        }


        internal Expression Select(Expression t, string ident, bool isLvalue)
        {
            if (t == null)
            {
                return Instructor.GetVarByName(ident, isLvalue);
            }
            else
            {
                return Instructor.GetVarFromTable(t, ident, isLvalue);
            }
        }

        internal Expression Select(Expression t, Expression index, bool isLvalue)
        {
            Contract.Assert(t != null && index != null);

            return Instructor.GetVarFromTable(t, index, isLvalue);
        }

        /// <summary>
        ///    从t 中移除 ident 
        /// </summary>
        /// <param name="t"></param>
        /// <param name="ident"></param>
        /// <returns></returns>
        internal Expression Remove(Expression t, string ident)
        {
            if (t == null)
            {
                return Instructor.RemoveVarByName(ident);
            }
            return Instructor.RemoveVarFromTable(t, ident);
        }

        internal Expression Remove(Expression t, Expression index)
        {
            Contract.Assert(t != null && index != null);

            return Instructor.RemoveVarFromTable(t, index);
        }


        internal Expression PosInc(Expression t, bool isFront)
        {
            if (isFront == true)
            {
                return Expression.Call(typeof(Instructor).GetMethod("IncPos"), t);
            }
            else
            {
                return Expression.Call(typeof(Instructor).GetMethod("PosInc"), t);
            }
        }
        internal Expression PosDec(Expression t, bool isFront)
        {
            if (isFront == true)
            {
                return Expression.Call(typeof(Instructor).GetMethod("DecPos"), t);
            }
            else
            {
                return Expression.Call(typeof(Instructor).GetMethod("PosDec"), t);
            }

        }

        /// <summary>
        ///   增加一个函数内的局部变量
        /// </summary>
        /// <param name="p"></param>
        internal void DefMethodParameter(string localName)
        {
            Instructor.CompileIndexStack.AddSymbolVar(localName);
        }

        /// <summary>
        ///    定义一个全局函数
        /// </summary>
        internal Expression MethodDef(string name,Expression functionValue)
        {
            return Expression.Call(typeof(Instructor).GetMethod("AddGloVar"), name.GetConstantExpression(),functionValue);
        }

        /// <summary>
        ///     定义一个匿名函数
        /// </summary>
        /// <param name="info">
        ///     TupleStruct<Int32,bool>
        ///       @First 函数参数个数
        ///       @Second 是否是不定参数
        /// </param>
        /// <param name="body">函数体</param>
        /// <param name="label">结束标记</param>
        /// <param name="upValues">闭包变量集合</param>
        /// <returns></returns>
        internal Expression MethodDef(TupleStruct<Int32,bool> info, BlockExpression body, LabelTarget label, List<KeyValuePair<string, Int32>> upValues)
        {
            //函数体
            Expression funcBody = Expression.Block(body, Expression.Call(typeof(Instructor).GetMethod("SetReturnVoid")),Expression.Label(label));

            Action func = Expression.Lambda<Action>(funcBody).Compile();   //编译方法体

            Expression o = ScriptObject.CreateFunction(func, info.First, info.Second).GetConstantExpression();

            if (upValues == null || upValues.Count == 0) return o;

            List<Expression> steps = new List<Expression>();

            Expression e = Expression.Call(typeof(Instructor).GetMethod("SetUpvaluesLength"), o, upValues.Count.GetConstantExpression());

            steps.Add(e);

            for (int i = 0; i < upValues.Count; i++)
            {
                KeyValuePair<string, Int32> upvalue = upValues[i];

                e = Expression.Call(typeof(Instructor).GetMethod("SetUpvalue"), o, i.GetConstantExpression(), upvalue.Value.GetConstantExpression());

                steps.Add(e);
            }

            steps.Add(o);

            return Expression.Block(steps);
        }

        /// <summary>
        ///    IfThenElse 表达式
        /// </summary>
        internal Expression IfThenElse(Expression cond, Expression thenpart, Expression elsepart)
        {
            if (elsepart == null)
            {
                return Expression.IfThen(Instructor.GetCondition(cond),thenpart);
            }
            return Expression.IfThenElse(Instructor.GetCondition(cond),thenpart, elsepart);
        }

    

        /// <summary>
        ///   while 循环表达式
        /// </summary>
        /// <param name="cond"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        internal Expression WhileLoop(Expression cond, Expression body, LabelTarget label)
        {
            return Expression.Loop(
                         Expression.IfThenElse(
                              Instructor.GetCondition(cond),
                              body,
                              Expression.Break(label)
                                               ),
                         label
                               );
        }

        /// <summary>
        ///    do while 循环表达式
        /// </summary>
        /// <param name="body"></param>
        /// <param name="cond"></param>
        /// <param name="labelTarget"></param>
        /// <returns></returns>
        internal Expression DoWhileLoop(Expression body, Expression cond, LabelTarget labelTarget)
        {
            return Expression.Block(body, WhileLoop(cond, body, labelTarget));
        }


        /// <summary>
        ///    for  循环表达式
        /// </summary>
        /// <param name="inits"></param>
        /// <param name="cond"></param>
        /// <param name="steps"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        internal Expression ForLoop(Expression inits, Expression cond, Expression steps, Expression body, LabelTarget labelTarget)
        {
            return Expression.Block(inits, WhileLoop(cond, Expression.Block(body, steps), labelTarget));
        }

        /// <summary>
        ///   break 表达式
        /// </summary>
        /// <param name="label"></param>
        /// <returns></returns>
        internal Expression Break(LabelTarget label)
        {
            return Expression.Break(label);
        }


        /// <summary>
        ///    return 语句
        /// </summary>
        /// <param name="labelTarget"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        internal Expression Return(LabelTarget returnTarget, Expression result)
        {
            Expression t = result == null ? Expression.Call(typeof(Instructor).GetMethod("SetReturnVoid")) : Expression.Call(typeof(Instructor).GetMethod("SetReturn"), result);
            return Expression.Block(t, Expression.Return(returnTarget));
        }

        internal Expression InvokeFunction(Expression own, Expression t, List<Expression> args)
        {
            Expression t0 = Expression.Call(typeof(Instructor).GetMethod("PushFunction"), t);

            Expression t1 = Expression.Call(typeof(Instructor).GetMethod("SetThisValue"), own);

            Expression t2 = Expression.Call(typeof(Instructor).GetMethod("InvokeFuncInStack"));

            if (args == null || args.Count == 0)
            {
                return Expression.Block(t0,t1,t2);
            }

            List<Expression> stepBlock = new List<Expression>();

            stepBlock.Add(t0);   //记录下函数压栈的下表

            foreach (var i in args)
            {
                stepBlock.Add(Expression.Call(typeof(Instructor).GetMethod("PushParameter"), i));   //参数入栈
            }
            stepBlock.Add(t1);     //设置this指针

            stepBlock.Add(t2);      //函数调用

            return Expression.Block(stepBlock);
        }


        /// <summary>
        ///    调用函数t,args为参数
        /// </summary>
        /// <param name="t"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        internal Expression InvokeFunction(Expression t, List<Expression> args)
        {
            Expression t1 = Expression.Call(typeof(Instructor).GetMethod("PushFunction"), t);

            Expression t2 = Expression.Call(typeof(Instructor).GetMethod("InvokeFuncInStack"));

            if (args == null || args.Count == 0)
            {
                return Expression.Block(t1,t2);
            }

            List<Expression> stepBlock = new List<Expression>();

            stepBlock.Add(t1);   //记录下函数压栈的下表

            foreach (var i in args)
            {
                stepBlock.Add(Expression.Call(typeof(Instructor).GetMethod("PushParameter"), i));   //参数入栈
            }
            stepBlock.Add(t2);      //函数调用

            return Expression.Block(stepBlock);
        }

        //字符串连接符
        internal Expression LinkStr(Expression t, Expression t1)
        {
            return Expression.Call(typeof(Instructor).GetMethod("LinkStr"), t, t1);
        }
        /// <summary>
        ///    加法
        /// </summary>
        private Expression Add(Expression t1, Expression t2)
        {
            return Expression.Add(t1, t2, typeof(Instructor).GetMethod("Add"));
        }
        /// <summary>
        ///    减法
        /// </summary>
        private Expression Subtract(Expression t1, Expression t2)
        {
            return Expression.Subtract(t1, t2, typeof(Instructor).GetMethod("Subtract"));
        }
        /// <summary>
        ///    乘法
        /// </summary>
        private Expression Multiply(Expression t1, Expression t2)
        {
            return Expression.Multiply(t1, t2, typeof(Instructor).GetMethod("Multiply"));
        }


        /// <summary>
        ///     除法
        /// </summary>
        private Expression Divide(Expression t1, Expression t2)
        {
            return Expression.Divide(t1, t2, typeof(Instructor).GetMethod("Divide"));
        }



        /// <summary>
        ///    判断是否相等
        /// </summary>
        /// <returns>返回的是bool类型</returns>
        internal Expression Equal(Expression t, Expression t1)
        {
            return Expression.Equal(t, t1, false, typeof(Instructor).GetMethod("Equal"));
        }

        /// <summary>
        ///    判断是否不相等
        /// </summary>
        /// <returns>返回的是bool类型</returns>
        internal Expression NotEqual(Expression t, Expression t1)
        {
            return Expression.Equal(t, t1, false, typeof(Instructor).GetMethod("NotEqual"));
        }


        /// <summary>
        ///     判断大于
        /// </summary>
        /// <returns></returns>
        internal Expression GreaterThan(Expression t, Expression t1)
        {
            return Expression.GreaterThan(t, t1, false, typeof(Instructor).GetMethod("GreaterThan")); ;
        }


        /// <summary>
        ///     判断大于等于
        /// </summary>
        /// <returns>返回的是bool类型</returns>
        internal Expression GreaterThanOrEqual(Expression t, Expression t1)
        {
            return Expression.GreaterThanOrEqual(t, t1, false, typeof(Instructor).GetMethod("GreaterThanOrEqual"));
        }

        /// <summary>
        ///     判断小于
        /// </summary>
        /// <returns>返回的是bool类型</returns>
        internal Expression LessThan(Expression t, Expression t1)
        {
            return Expression.LessThan(t, t1, false, typeof(Instructor).GetMethod("LessThan"));
        }

        /// <summary>
        ///     判断小于等于
        /// </summary>
        /// <returns>返回的是bool类型</returns>
        internal Expression LessThanOrEqual(Expression t, Expression t1)
        {
            return Expression.LessThanOrEqual(t, t1, false, typeof(Instructor).GetMethod("LessThanOrEqual"));
        }

        /// <summary>
        ///   根据表达式处理二元运算
        /// </summary>
        /// <param name="topOp"></param>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <returns></returns>
        internal Expression MakeOp(int topOp, Expression t1, Expression t2)
        {
            switch (topOp)
            {
                case EQEQ:          //   == 
                    return Equal(t1, t2);

                case BANGEQ:      //    !=
                    return NotEqual(t1, t2);

                case GT:         //    >
                    return GreaterThan(t1, t2);

                case GTEQ:          //   >=
                    return GreaterThanOrEqual(t1, t2);

                case LT:                //  <
                    return LessThan(t1, t2);

                case LTEQ:             //   <=
                    return LessThanOrEqual(t1, t2);

                case PLUS:             //   +
                    return Add(t1, t2);

                case SUB:            //     -
                    return Subtract(t1, t2);

                case STAR:
                    return Multiply(t1, t2);

                case SLASH:
                    return Divide(t1, t2);


            }
            throw new NotImplementedException();
        }
        /// <summary>
        ///    产生常量字符串
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        internal Expression String(string str)
        {
            return ScriptObject.CreateString(str).GetConstantExpression();
        }
        /// <summary>
        ///    返回False
        /// </summary>
        /// <returns></returns>
        internal Expression Flase()
        {
            return Instructor.GetFalse().GetConstantExpression();
        }
        /// <summary>
        ///    返回True
        /// </summary>
        /// <returns></returns>
        internal Expression True()
        {
            return Instructor.GetTrue().GetConstantExpression();
        }
        /// <summary>
        ///    返回table
        /// </summary>
        internal Expression Table(Expression t, List<KeyValuePair<string, Expression>> elems)
        {
            if (t == null)
            {
                t = Expression.Call(typeof(Instructor).GetMethod("CreateTable"));
            }
            foreach (var i in elems)
            {
                if(i.Key == null)
                {
                    t = Expression.Call(typeof(Instructor).GetMethod("TableAddInArray"), t, i.Value);
                }
                else
                {
                    t = Expression.Call(typeof(Instructor).GetMethod("TableAddFileld"), t, i.Key.GetConstantExpression(), i.Value);
                }
            }
            return t;
        }


        /// <summary>
        ///    三目运算
        /// </summary>
        internal Expression Conditional(Expression t, Expression t1, Expression t2)
        {
            return Expression.Condition(Instructor.GetCondition(t), t1, t2);
        }

        /// <summary>
        ///   获取this指针
        /// </summary>
        internal Expression GetThisValue()
        {
           return Expression.Call(typeof(Instructor).GetMethod("GetThisValue"));
        }

    }
}
