using System.Linq.Expressions;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using ScriptInterpreter.Util;
using ScriptInterpreter.Parse;

namespace ScriptInterpreter.RunTime
{
    public static class Instructor
    {
        internal class CompileStack
        {
            public const int UPVALUE_OFFSET = 2;

            /// <summary>
            ///    有区域范围的符号表
            /// </summary>
            private List<Dictionary<string, Int32>> _blockSymbolTable = new List<Dictionary<string, int>>();

            private Stack<int> _indexStack = new Stack<int>();

            private int _symbolcount;

            public int IndexTop
            {
                get { return _symbolcount; }

                set { _symbolcount = value; }
            }

            public CompileStack() 
            {
            }

            public void EnterBlock()
            {
                Dictionary<string, Int32> symbolTable = new Dictionary<string, int>();

                _blockSymbolTable.Add(symbolTable);
            }

            public int OutBlock()
            {
                int count = _blockSymbolTable[_blockSymbolTable.Count - 1].Count;
                IndexTop -= count;

                _blockSymbolTable.RemoveAt(_blockSymbolTable.Count - 1);

                return count;
            }

            public void AddSymbolVar(string localName)
            {
                Dictionary<string, Int32> symbolTable = _blockSymbolTable[_blockSymbolTable.Count -1];

                int at;

                if (symbolTable.TryGetValue(localName, out at) == false)   //不存在
                {
                    symbolTable.Add(localName, IndexTop++);
                }
                else
                {
                    throw ScriptCompileException.CreateContentExist(localName);
                }
            }

            public int GetSymbolIndex(string localName)
            {
                int currentIndex = _indexStack.Peek() + 1;

                Dictionary<string, Int32> symbolTable;

                int resoult;

                for (int i = _blockSymbolTable.Count - 1; i >= 0; --i)
                {
                    symbolTable = _blockSymbolTable[i];

                    if (symbolTable.TryGetValue(localName, out resoult) == true)
                    {
                        if (resoult >= currentIndex)
                        {
                            return resoult - currentIndex;
                        }
                        else
                        {
                            return -resoult - UPVALUE_OFFSET;
                        }
                    }
                }
                return -1;
            }

            internal void LogIndex()
            {
                _indexStack.Push(IndexTop++);   //记录下函数位置

                EnterBlock();
            }

            internal void PopIndex()
            {
                OutBlock();
                _indexStack.Pop();
                --IndexTop;
            }
            internal void CheckClear()
            {
                Contract.Assert(_indexStack.Count == 0);
                Contract.Assert(_blockSymbolTable.Count == 0);
                Contract.Assert(_symbolcount == 0);
            }

            /// <summary>
            ///   清理编译符号表等信息
            /// </summary>
            internal void Clear()
            {
                _indexStack.Clear();
                _blockSymbolTable.Clear();
                _symbolcount = 0;
            }
        }

        private static CompileStack comStack = new CompileStack();

        internal static CompileStack CompileIndexStack
        {
            get { return comStack; }
        }

        public static ConstantExpression GetConstantExpression(this bool boolValue)
        {
            return Expression.Constant(boolValue);
        }
        public static ConstantExpression GetConstantExpression(this int intValue)
        {
            return Expression.Constant(intValue);
        }
        public static ConstantExpression GetConstantExpression(this string strValue)
        {
            return Expression.Constant(strValue);
        }
        public static ConstantExpression GetConstantExpression(this double numValue)
        {
            return Expression.Constant(numValue);
        }

        public static ConstantExpression GetConstantExpression(this Action numValue)
        {
            return Expression.Constant(numValue);
        }

        public static ConstantExpression GetConstantExpression(this ScriptObject obj)
        {
            return Expression.Constant(obj);
        }

      
        public static Expression GetGlobalVarExpression(string varName, bool isLvalue)
        {

            if (isLvalue == true)     //左值
            {
                return Expression.Call(typeof(Instructor).GetMethod("CreateGlobalVar"), varName.GetConstantExpression());
            }
            else
            {
                return Expression.Call(typeof(Instructor).GetMethod("GetGlobalVar"), varName.GetConstantExpression());
            }
         
        }

        private static Expression GetUpVarExpression(string name,int offset)
        {
            //修正偏移
            offset = -offset - CompileStack.UPVALUE_OFFSET;

            //从哈希改为List,考虑到一般情况,UpValue个数不会太多
            List<KeyValuePair<string, Int32>> upValues = RunEnvironment.Instance.Parser.UpValueStack.Peek();

            int i;

            for (i = 0; i < upValues.Count; i++)
            {
                if (upValues[i].Key == name)
                {
                    break;
                }
            }
            if (i >= upValues.Count)
            {
                upValues.Add(new KeyValuePair<string, Int32>(name,offset));
            }
            return Expression.Call(typeof(Instructor).GetMethod("GetUpVar"), i.GetConstantExpression());
        }

        public static Expression GetLocalVarExpression(int offset)
        {
            return Expression.Call(typeof(Instructor).GetMethod("GetLocalVar"), offset.GetConstantExpression());
        }

        public static Expression ConverListToBlock(List<Expression> i)
        {
            return Expression.Block(i);
        }


        /// <summary>
        ///    从当前栈获得对象
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static ScriptObject GetLocalVar(int offset)
        {
            return RunEnvironment.Instance.LocalStack.GetStackVar(offset);
        }

        public static ScriptObject GetUpVar(int offset)
        {
            return RunEnvironment.Instance.LocalStack.GetUpVar(offset);
        }

        public static ScriptObject GetGlobalVar(string name)
        {
            ScriptObject global = RunEnvironment.Instance.Global;
            return Select(global, name);
        }


        public static ScriptObject CreateGlobalVar(string name)
        {
            ScriptObject global = RunEnvironment.Instance.Global;
            return SelectOrCreate(global,name);
            
        }

        public static ScriptObject AssignVar(ScriptObject x, ScriptObject y)
        {
            x.Type = y.Type;
            x.Value = y.Value;
            return x;
        }

        public static ScriptObject LinkStr(ScriptObject x, ScriptObject y)
        {
            if (( x.Type == ValueType.STRING ||  x.Type == ValueType.NUMBER )&&( y.Type == ValueType.STRING  ||  y.Type == ValueType.NUMBER ))
            {
                return ScriptObject.CreateString(x.GetString() + y.GetString());
            }
            throw new Exception("执行无法链接操作");
        }
 
        /// <summary>
        ///    从 table中获取属性
        /// </summary>
        public static Expression GetVarFromTable(Expression t, string ident, bool isLvalue)
        {
            if (isLvalue == true)
            {
                return Expression.Call(typeof(Instructor).GetMethod("SelectOrCreate"), t, ident.GetConstantExpression());
            }
            else
            {
                return Expression.Call(typeof(Instructor).GetMethod("Select"), t, ident.GetConstantExpression());
            }
        }

        /// <summary>
        ///    通过下标从 table中获取属性
        /// </summary>
        internal static Expression GetVarFromTable(Expression t, Expression index, bool isLvalue)
        {
            if (isLvalue == true)
            {
                return Expression.Call(typeof(Instructor).GetMethod("IndexofOrCreate"), t, index);
            }
            else
            {
                return Expression.Call(typeof(Instructor).GetMethod("Indexof"), t, index);
            }
        }

        /// <summary>
        ///     获得变量
        /// </summary>
        /// <param name="name"></param>
        /// <param name="isLvalue"></param>
        /// <returns></returns>
        public static Expression GetVarByName(string name, bool isLvalue)
        {
            int offset;

            if ((offset = Instructor.CompileIndexStack.GetSymbolIndex(name)) >= 0)
            {
                return GetLocalVarExpression(offset);
            }
            else if (offset == -1)
            {
                return GetGlobalVarExpression(name, isLvalue);
            }
            return GetUpVarExpression(name,offset);
        }


        /// <summary>
        ///   回收此变量的空间
        /// </summary>
        /// <param name="name"></param>
        /// <param name="IsInMethod"></param>
        /// <returns></returns>
        public static Expression RemoveVarByName(string name)
        {
            int offset;

            if ((offset = Instructor.CompileIndexStack.GetSymbolIndex(name)) != -1)
            {
                 return Expression.Call(typeof(Instructor).GetMethod("ClearStackVarAt"), offset.GetConstantExpression());
            }
            return Expression.Call(typeof(Instructor).GetMethod("ClearTableFileld"), RunEnvironment.Instance.Global.GetConstantExpression(), name.GetConstantExpression());
        }
        /// <summary>
        ///   移除此表中的制定字段
        /// </summary>
        public static Expression RemoveVarFromTable(Expression t, string name)
        {
            return Expression.Call(typeof(Instructor).GetMethod("ClearTableFileld"), t, name.GetConstantExpression());
        }
        public static Expression RemoveVarFromTable(Expression t, Expression index)
        {
            return Expression.Call(typeof(Instructor).GetMethod("ClearTableItem"), t, index);
        }
        /// <summary>
        ///   从表达式结果获得条件需要的bool变量
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static Expression GetCondition(Expression t)
        {
            return Expression.Call(typeof(Instructor).GetMethod("ConverScrObjToBool"), t);
        }


        /// <summary>
        ///    根据对象判断其条件属性
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static bool ConverScrObjToBool(ScriptObject obj)
        {
            if(obj.Type == ValueType.NIL)    // nil 为false
            {
                return false;
            }
            if (obj.Type == ValueType.BOOLEAN)   
            {
                return obj.Value.Boolean;
            }
            return true;
        }


        /// <summary>
        ///    从 own 中选择 filed 字段,没有就创建一个新的
        /// </summary>
        public static ScriptObject SelectOrCreate(ScriptObject own, string filed)
        {
            return own.GetOrCreateFileld(filed);
        }
        
        public static ScriptObject IndexofOrCreate(ScriptObject own, ScriptObject index)
        {
            return own.GetOrCreateFileld(index);
        }

        /// <summary>
        ///   从 own 返回 filed 字段,没有就为nil
        /// </summary>
        public static ScriptObject Select(ScriptObject own, string filed)
        {
            return own.GetFileld(filed);
        }

        public static ScriptObject Indexof(ScriptObject own, ScriptObject index)
        {
            return own.GetFileld(index);
        }


        /// <summary>
        ///    将数值前加1，即 ++i;
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static ScriptObject IncPos(ScriptObject t)
        {
            Contract.Assert(t.Type == ValueType.NUMBER);
            ++t.Value.Number;
            return t;
        }


        /// <summary>
        ///    将数值变量后加1，即 i++;
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static ScriptObject PosInc(ScriptObject t)
        {
            Contract.Assert(t.Type == ValueType.NUMBER);
            ScriptObject newobj = ScriptObject.CreateNum(t.Value.Number);
            t.Value.Number++;
            return newobj;
        }


        /// <summary>
        ///    将数值前减1，即 --i;
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static ScriptObject DecPos(ScriptObject t)
        {
            Contract.Assert(t.Type == ValueType.NUMBER);
            --t.Value.Number;
            return t;
        }

        /// <summary>
        ///    将数值后减1，即 i--;
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static ScriptObject PosDec(ScriptObject t)
        {
            Contract.Assert(t.Type == ValueType.NUMBER);
            ScriptObject newobj = ScriptObject.CreateNum(t.Value.Number);
            t.Value.Number--;
            return newobj;
        }


        /// <summary>
        ///    将字段加入全局变量中
        /// </summary>
        /// <param name="name"></param>
        /// <param name="obj"></param>
        public static void AddGloVar(string name, ScriptObject obj)
        {
            RunEnvironment.Instance.Global.AddFileld(name, obj);
        }

        /// <summary>
        ///    获取全局Nil只读字段
        /// </summary>
        /// <returns></returns>
        public static ScriptObject GetNil()
        {
            return RunEnvironment.Nil;
        }

        /// <summary>
        ///    获取全局False只读字段
        /// </summary>
        /// <returns></returns>
        public static ScriptObject GetFalse()
        {
            return RunEnvironment.False;
        }

        /// <summary>
        ///    获取全局True只读字段
        /// </summary>
        /// <returns></returns>
        public static ScriptObject GetTrue()
        {
            return RunEnvironment.True;
        }




#region     二元运算

        /// <summary>
        ///     加法
        /// </summary>
        public static ScriptObject Add(ScriptObject obj1,ScriptObject obj2)
        {
            if (obj1.Type != obj2.Type)
            {
                return GetNil();
            }
            if (obj1.Type == ValueType.NUMBER)
            {
                ScriptObject s = ScriptObject.CreateNum(obj1.Value.Number + obj2.Value.Number);
                return s;
            }
            return GetNil();
        }

        /// <summary>
        ///   减法
        /// </summary>
        public static ScriptObject Subtract(ScriptObject obj1, ScriptObject obj2)
        {
            if (obj1.Type != obj2.Type)
            {
                return GetNil();
            }
            if (obj1.Type == ValueType.NUMBER)
            {
                ScriptObject s = ScriptObject.CreateNum(obj1.Value.Number - obj2.Value.Number);
                return s;
            }
            return GetNil();
        }

        /// <summary>
        ///     乘法
        /// </summary>
        public static ScriptObject Multiply(ScriptObject obj1, ScriptObject obj2)
        {
            if (obj1.Type != obj2.Type)
            {
                return GetNil();
            }
            if (obj1.Type == ValueType.NUMBER)
            {
                ScriptObject s = ScriptObject.CreateNum(obj1.Value.Number * obj2.Value.Number);
                return s;
            }
            return GetNil();
        }

        /// <summary>
        ///    除法
        /// </summary>
        public static ScriptObject Divide(ScriptObject obj1, ScriptObject obj2)
        {
            if (obj1.Type != obj2.Type)
            {
                return GetNil();
            }
            if (obj1.Type == ValueType.NUMBER)
            {
                ScriptObject s = ScriptObject.CreateNum(obj1.Value.Number / obj2.Value.Number);
                return s;
            }
            return GetNil();
        }

        /// <summary>
        ///    比较是否相等
        /// </summary>
        public static ScriptObject Equal(ScriptObject obj1, ScriptObject obj2)
        {
            if(obj1.Type != obj2.Type)
            {
                return GetNil();
            }
            if(obj1.Type == ValueType.NUMBER)
            {
                return obj1.Value.Number == obj2.Value.Number ? GetTrue() : GetFalse();
            }
            if(obj1.Type == ValueType.BOOLEAN)
            {
                return obj1.Value.Boolean == obj2.Value.Boolean ? GetTrue() : GetFalse();
            }
            if (obj1.Type == ValueType.STRING)
            {
                return obj1.Value.RefPartHandle.StringValue == obj2.Value.RefPartHandle.StringValue ? GetTrue() : GetFalse();
            }
            return GetFalse();
        }

        /// <summary>
        ///    比较是否不相等
        /// </summary>
        public static ScriptObject NotEqual(ScriptObject obj1, ScriptObject obj2)
        {
            if (obj1.Type != obj2.Type)
            {
                return GetNil();
            }
            if (obj1.Type == ValueType.NUMBER)
            {
                return obj1.Value.Number != obj2.Value.Number ? GetTrue() : GetFalse();
            }

            if (obj2.Type == ValueType.BOOLEAN)
            {
                return obj1.Value.Boolean != obj2.Value.Boolean ? GetTrue() : GetFalse();
            }
            return GetFalse();
        }

        /// <summary>
        ///    大于
        /// </summary>
        public static ScriptObject GreaterThan(ScriptObject obj1, ScriptObject obj2)
        {
            if (obj1.Type != obj2.Type)
            {
                return GetNil();
            }
            if (obj1.Type == ValueType.NUMBER)
            {
                return obj1.Value.Number > obj2.Value.Number ? GetTrue() : GetFalse();
            }
            return GetFalse();
        }

        /// <summary>
        ///    大于等于
        /// </summary>
        public static ScriptObject GreaterThanOrEqual(ScriptObject obj1, ScriptObject obj2)
        {
            if (obj1.Type != obj2.Type)
            {
                return GetNil();
            }
            if (obj1.Type == ValueType.NUMBER)
            {
                return obj1.Value.Number < obj2.Value.Number ? GetFalse() : GetTrue();
            }
            return GetFalse();
        }

        /// <summary>
        ///    小于
        /// </summary>
        public static ScriptObject LessThan(ScriptObject obj1, ScriptObject obj2)
        {
            if (obj1.Type != obj2.Type)
            {
                return GetNil();
            }
            if (obj1.Type == ValueType.NUMBER)
            {
                return obj1.Value.Number < obj2.Value.Number ? GetTrue() : GetFalse();
            }
            return GetFalse();
        }

        /// <summary>
        ///    小于等于
        /// </summary>
        public static ScriptObject LessThanOrEqual(ScriptObject obj1, ScriptObject obj2)
        {
            if (obj1.Type != obj2.Type)
            {
                return GetNil();
            }
            if (obj1.Type == ValueType.NUMBER)
            {
                return obj1.Value.Number > obj2.Value.Number ? GetFalse() : GetTrue(); 
            }
            return GetFalse();
        }
#endregion



        public static void StackPop(int count)
        {
            StackState stack = RunEnvironment.Instance.LocalStack;
            stack.Pop(count);
        }


        /// <summary>
        ///    将局部变量压栈
        /// </summary>
        /// <param name="obj"></param>
        public static void StackPush(ScriptObject obj)
        {
            StackState stack = RunEnvironment.Instance.LocalStack;
            stack.Push(obj);
        }

       /// <summary>
       ///    函数压栈
       /// </summary>
       /// <param name="func"></param>
       /// <returns>返回栈中的位置下表</returns>
        public static void PushFunction(ScriptObject func)
        {
            StackState stack = RunEnvironment.Instance.LocalStack;
            stack.PushFunction(func);
        }
        /// <summary>
        ///    函数参数压栈
        /// </summary>
        /// <param name="parameter"></param>
        public static void PushParameter(ScriptObject parameter)
        {
            StackState stack = RunEnvironment.Instance.LocalStack;
            stack.PushFuncParameter(parameter);
        }

        /// <summary>
        ///   调用栈中的函数
        /// </summary>
        public static ScriptObject InvokeFuncInStack()
        {
            StackState stack = RunEnvironment.Instance.LocalStack;

            return stack.InvokeFunction();
        }

        /// <summary>
        ///   设置返回值为空
        /// </summary>
        public static void SetReturnVoid()
        {
            StackState stack = RunEnvironment.Instance.LocalStack;
            stack.SetReturnVoid();
        }

        public static void SetReturn(ScriptObject  res)
        {
            StackState stack = RunEnvironment.Instance.LocalStack;
            stack.SetReturn(res);
        }

      

        /// <summary>
        ///   将栈上某值清空为nil
        /// </summary>
        /// <param name="offset"></param>
        public static void ClearStackVarAt(int offset)
        {
            StackState stack = RunEnvironment.Instance.LocalStack;
            ScriptObject  s =   stack.GetStackVar(offset);
            AssignVar(s, RunEnvironment.Nil);       //仅赋nil,退栈时清空
        }
        /// <summary>
        ///    将表中某字段清掉
        /// </summary>
        public static void ClearTableFileld(ScriptObject  own,string name)
        {
            own.RemoveFileld(name);
        }

        public static void ClearTableItem(ScriptObject own, ScriptObject index)
        {
            own.RemoveFileld(index);
        }


        public static ScriptObject CreateTable()
        {
            return ScriptObject.CreateTable();
        }

        public static ScriptObject TableAddFileld(ScriptObject table,string name,ScriptObject value)
        {
            table.AddFileld(name,value);
            return table;
        }

        public static ScriptObject TableAddInArray(ScriptObject table,ScriptObject value)
        {
            table.AddInArray(value);
            return table;
        }

        /// <summary>
        ///    设置Upvalues的长度
        /// </summary>
        public static void SetUpvaluesLength(ScriptObject function,int length)
        {
            Contract.Assert(function.Type == ValueType.FUNCTION);

            FuncPart func =  function.Value.RefPartHandle.ConverToFuncPart();

            func.UpVals = new ScriptObject[length];
            
        }

        /// <summary>
        ///   设置每一个UpValue
        /// </summary>
        public static void SetUpvalue(ScriptObject function, int at, int stackIndex)
        {
            Contract.Assert(function.Type == ValueType.FUNCTION);

            FuncPart func = function.Value.RefPartHandle.ConverToFuncPart();

            func.UpVals[at] = RunEnvironment.Instance.LocalStack.GetVarFromBase(stackIndex);
        }


        public static void SetThisValue(ScriptObject own)
        {
            StackState stack = RunEnvironment.Instance.LocalStack;
            Instructor.AssignVar(stack.This, own);
        }


        public static ScriptObject GetThisValue()
        {
            StackState stack = RunEnvironment.Instance.LocalStack;
            return stack.This;
        }
    }
}
