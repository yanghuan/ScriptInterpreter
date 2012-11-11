using ScriptInterpreter.RunTime;
using System.Diagnostics;




namespace ScriptInterpreter.Lib
{
    using SW = System.Windows;
    using System.Linq.Expressions;
    using System;
    using ScriptInterpreter.Util;

    public static class Lightbrary
    {
        public static void OutPut()
        {
            StackState s = RunEnvironment.Instance.LocalStack;
            //获得第一个参数
            ScriptObject arg1 = s.GetStackVar(0);

            TablePart tablePart = arg1.Value.RefPartHandle.ConverToTablePart();

            for (int i = 0; i < tablePart.Count; i++)
            {
                Console.Write(tablePart.ArrayPart[i].GetString() + " ");
            }

            s.SetReturnVoid();
        }

        public static void GetTypeOf()
        {
            StackState s = RunEnvironment.Instance.LocalStack;

            ScriptObject arg1 = s.GetStackVar(0);

            ScriptObject resoult = ScriptObject.CreateString(arg1.GetTypeof());
          
            s.SetReturn(resoult);
        }

        public static void LoadString()
        {
            StackState s = RunEnvironment.Instance.LocalStack;

            ScriptObject arg1 = s.GetStackVar(0);

            if (arg1.Type == ScriptInterpreter.RunTime.ValueType.STRING)
            {
                Expression e =  RunEnvironment.Instance.CompilationUnit(arg1.Value.RefPartHandle.StringValue);

                Func<ScriptObject> a = Expression.Lambda<Func<ScriptObject>>(e).Compile();

                ScriptObject resoult = a();

                if (resoult == null)    s.SetReturnVoid();

                else     s.SetReturn(resoult);

                return;
            }
            s.SetReturnVoid();
        }

        public static void Error()
        {
            StackState s = RunEnvironment.Instance.LocalStack;

            ScriptObject arg1 = s.GetStackVar(0);

            if (arg1.Type == ScriptInterpreter.RunTime.ValueType.STRING)
            {
                string message = arg1.GetString();

                throw new ScriptRunTimeException(message);
            }
            s.SetReturnVoid();
        }


        public static void SetMetatable()
        {
            StackState s = RunEnvironment.Instance.LocalStack;

            ScriptObject arg1 = s.GetStackVar(0);

            ScriptObject arg2 = s.GetStackVar(1);

            if (arg1.Type != ScriptInterpreter.RunTime.ValueType.TABLE || arg2.Type != ScriptInterpreter.RunTime.ValueType.TABLE) return;

            TablePart table = arg1.Value.RefPartHandle.ConverToTablePart();

            table.MetaTable = arg2;
        }

        public static void GetMetatable()
        {
            StackState s = RunEnvironment.Instance.LocalStack;

            ScriptObject arg1 = s.GetStackVar(0);

            if (arg1.Type == ScriptInterpreter.RunTime.ValueType.TABLE)
            {
                ScriptObject metaTable = arg1.Value.RefPartHandle.ConverToTablePart().MetaTable;

                if (metaTable!=null)
                {
                    s.SetReturn(metaTable);
                }
            }
            s.SetReturnVoid();
        }

        public static void LoadLIB()
        {
            RunEnvironment r = RunEnvironment.Instance;

            bool res = r.LoadFunction("print", OutPut,0,true);

            res = r.LoadFunction("typeof", GetTypeOf, 1);

            res = r.LoadFunction("loadstring", LoadString, 1);

            res = r.LoadFunction("error", Error, 1);

            res = r.LoadFunction("setmetatable", SetMetatable, 2);

            res = r.LoadFunction("getmetatable", GetMetatable, 1);
        }
    }
}
