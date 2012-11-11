using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using ScriptInterpreter.Lib;
using System.Threading;
using ScriptInterpreter.Parse;

namespace ScriptInterpreter.RunTime
{

    using SW = System.Windows;
    using System.Linq.Expressions;
    using ScriptInterpreter.Util;

    public class StackState
    {
        private List<ScriptObject> stack = new List<ScriptObject>();

        private Stack<int> _contextStartStack = new Stack<int>();

        private Stack<int> _funcPushStack = new Stack<int>();

        /// <summary>
        ///   存放返回值
        /// </summary>
        private ScriptObject returnValue = ScriptObject.CreateNil();

        /// <summary>
        ///   存放this引用
        /// </summary>
        private ScriptObject thisValue = ScriptObject.CreateNil();

        public ScriptObject This
        {
            get { return thisValue; }
            set { thisValue = value; }
        }

        private  int contextStart;

        public int Top
        {
            get { return stack.Count; }
        }

        internal StackState()
        {
            Inint();
        }

        private void Inint()
        {
            contextStart = -1;
        }

        //从此开始进行栈上变量的查找
        public int VarStartPoint
        {
            get { return contextStart + 1; }
        }

        internal ScriptObject GetVarFromBase(int stackIndex)
        {
            return stack[stackIndex];    
        }

        internal ScriptObject GetStackVar(int offset)
        {
            Contract.Assert((contextStart + offset) <= stack.Count);
            return stack[offset + VarStartPoint];    
        }

        internal ScriptObject GetUpVar(int offset)
        {
            Contract.Assert(stack[contextStart].Type == ValueType.FUNCTION);

            return stack[contextStart].Value.RefPartHandle.ConverToFuncPart().UpVals[offset];
        }

     

        /// <summary>
        ///    将函数压栈,准备调用
        /// </summary>
        /// <param name="func"></param>
        internal void PushFunction(ScriptObject func)
        {
            Contract.Assert(func !=null && func.Type == ValueType.FUNCTION);

            stack.Add(func);

            _funcPushStack.Push(stack.Count - 1);  //指向函数
        }
        /// <summary>
        ///    将函数参数压栈,准备调用函数
        /// </summary>
        /// <param name="parameter"></param>
        internal void PushFuncParameter(ScriptObject parameter)
        {
            Contract.Assert(parameter!=null);

            int funcIndex = _funcPushStack.Peek();

            FuncPart func = stack[funcIndex].Value.RefPartHandle.ConverToFuncPart();

            //当前栈中的函数参数小于函数参数大小
            if (Top - (funcIndex + 1) < func.ArgsCount)
            {
                Push(parameter);
            }
            else if (func.IsUncertainParameters == true)   //是不定参数
            {
                if (Top - (funcIndex + 1) == func.ArgsCount)
                {
                    ScriptObject s = ScriptObject.CreateTable();

                    stack.Add(s);
                }
                ScriptObject args = stack[Top - 1];
                args.AddInArray(parameter);
            }
            //否则,则省略掉传入参数
        }

        internal ScriptObject InvokeFunction()
        {
            return InvokeFuncAtIndex(_funcPushStack.Pop());
        }

        /// <summary>
        ///    调用指定位置的函数
        /// </summary>
        private ScriptObject InvokeFuncAtIndex(int at)
        {
            Contract.Assert(stack[at].Type == ValueType.FUNCTION);

            FuncPart invokeFunc = stack[at].Value.RefPartHandle.ConverToFuncPart();

            //如果函数的参数大于压入的参数,则不足部分补Nil
            while (invokeFunc.ArgsCount > Top - (at + 1))
            {
                Push(RunEnvironment.Nil);
            }
            _contextStartStack.Push(contextStart);

            contextStart = at;  //设置当前上下文,很重要

            invokeFunc.Value();     //委托调用

            stack.RemoveRange(at,Top - at);

            contextStart = _contextStartStack.Pop(); //恢复当前上下文    

            Instructor.AssignVar(This,RunEnvironment.Nil);   // this 指针清空

            return returnValue;
        }

        internal ScriptObject GetReturn()
        {
            return returnValue;
        }

        internal void SetReturn(ScriptObject scriptObject)
        {
            Contract.Assert(scriptObject!=null);

            Instructor.AssignVar(returnValue, scriptObject);
        }

        internal void SetReturnVoid()
        {
            SetReturn(RunEnvironment.Nil);
        }

        internal void CheckClear()
        {
            Contract.Assert(stack.Count == 0);
            Contract.Assert(contextStart == -1);
        }






        #region    堆栈操作函数

        internal int Push(ScriptObject obj)
        {
            ScriptObject o = ScriptObject.CreateScriptObject(obj);
            stack.Add(o);
            return Top - 1;
        }

        internal int Push(string str)
        {
            ScriptObject o = ScriptObject.CreateString(str);
            stack.Add(o);
            return Top - 1;
        }

        /// <summary>
        ///   弹出指定个数的元素
        /// </summary>
        /// <param name="count"></param>
        internal void Pop(int count)
        {
            stack.RemoveRange(Top - count, count);
        }





        internal void InvokeFunction(ScriptObject function,params ScriptObject[] args)
        {
            this.PushFunction(function);

            foreach (ScriptObject i in args)
            {
                this.PushFuncParameter(i);
            }

            this.InvokeFunction();
        }


        #endregion
    }


    public sealed class RunEnvironment
    {
        private static readonly Lazy<RunEnvironment> _instance = new Lazy<RunEnvironment>( () => new RunEnvironment());

        private RunEnvironment()
        {
            InitializeComponent();
        }
     
        public static RunEnvironment Instance
        {
            get { return _instance.Value ;}
        }

        private ScriptObject globalPool = ScriptObject.CreateTable();

        private Parser parser = new Parser(new Scanner());

        public Parser Parser
        {
            get { return parser; }
        }

        public ScriptObject Global
        {
            get { return globalPool; }
        }

        private StackState Stack = new StackState();

        public StackState LocalStack
        {
            get { return Stack; }
        }

        /// <summary>
        ///    三个只读全局公共对象,只可用作右值表达式中
        /// </summary>
        public static readonly ScriptObject Nil = ScriptObject.CreateNil();
        public static readonly ScriptObject False = ScriptObject.CreateBool(false);
        public static readonly ScriptObject True = ScriptObject.CreateBool(true);

        //载入一个函数
        internal bool LoadFunction(string name,Action fun,int argCount,bool isUncertainParameters = false)
        {
            ScriptObject s = ScriptObject.CreateFunction(fun, argCount, isUncertainParameters);
            Global.AddFileld(name, s);
            return true;
        }

        private void InitializeComponent()
        {
            Global.AddFileld("_G", Global);
        }

        public void Init()
        {
            Lightbrary.LoadLIB();
        }

        public Expression CompilationUnit(string commandString)
        {
            if (string.IsNullOrWhiteSpace(commandString))
            {
                return null;
            }

            Parser.S.InputString(commandString);

            Expression b = null;

            try
            {
               b  =  Parser.CompilationUnit();

               Instructor.CompileIndexStack.CheckClear();
            }
            catch (ScriptCompileException e)
            {
                Console.WriteLine(e.Message);
                Instructor.CompileIndexStack.Clear();
            }
            return b;
        }

        public int Interprete(string commandString)
        {
            Expression b = CompilationUnit(commandString);

            if (b == null) return -1;

            Action a = Expression.Lambda<Action>(Parser.E.InvokeFunction(b, null)).Compile();

            a();

            RunEnvironment.Instance.LocalStack.CheckClear();

            return 0;
        }
    }
}
