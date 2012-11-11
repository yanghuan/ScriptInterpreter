using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Diagnostics;
using System.Linq.Expressions;

namespace ScriptInterpreter.RunTime
{
    [StructLayout(LayoutKind.Explicit)]
    public struct ValueObject
    {

        [FieldOffset(0)]
        private double _number;

        public double Number
        {
            get { return _number; }
            set { _number = value; }
        }

        [FieldOffset(0)]
        private bool _boolean;

        public bool Boolean
        {
            get { return _boolean; }
            set { _boolean = value; }
        }

        [FieldOffset(8)]
        private RefPartBase _refHandle;

        public RefPartBase RefPartHandle
        {
            get { return _refHandle; }
            set { _refHandle = value; }
        }
    }

    public sealed class ValueType
    {
        private ValueType() { }

        public const int NIL = 0;   // nil
        public const int BOOLEAN = NIL + 1;   //boolean
        public const int NUMBER = BOOLEAN + 1;   //number;
        public const int STRING = NUMBER + 1;   //string
        public const int FUNCTION = STRING + 1;   //function
        public const int TABLE = FUNCTION + 1;   //table

    }
    public class ScriptObject
    {
        public ValueObject Value;

        private int _type;

        public int Type
        {
            get { return _type; }

            set
            {
                _type = value;
            }
        }

        private ScriptObject() { }

        public string GetString()
        {
            switch (Type)
            {
                case ValueType.NIL:
                    return "nil";

                case ValueType.BOOLEAN:
                    return Value.Boolean.ToString();

                case ValueType.NUMBER:
                    return Value.Number.ToString();

                case ValueType.STRING:
                    return Value.RefPartHandle.ConverToStringPart().Value;

                case ValueType.FUNCTION:

                    return "function";

                case ValueType.TABLE:

                    return "table";

                default:
                    Contract.Assert(false, "未定义");
                    return "未定义";
            }
        }

        public override string ToString()
        {
            throw new NotImplementedException("ScriptObject的ToString()方法不用");
        }

        public string GetTypeof()
        {
            switch (Type)
            {
                case ValueType.NIL:
                    return "nil";

                case ValueType.BOOLEAN:
                    return "boolean";

                case ValueType.NUMBER:
                    return "number";

                case ValueType.STRING:
                    return "string";

                case ValueType.FUNCTION:
                    return "function";

                case ValueType.TABLE:
                    return "table";

                default:
                    Contract.Assert(false);
                    return "未知";
            }
        }

        /// <summary>
        ///    拷贝一个ScriptObject
        /// </summary>
        internal static ScriptObject CreateScriptObject(ScriptObject obj)
        {
            ScriptObject s = new ScriptObject();
            Instructor.AssignVar(s, obj);
            return s;
        }

        /// <summary>
        ///    产生一个 nil
        /// </summary>
        /// <returns></returns>
        public static ScriptObject CreateNil()
        {
            ScriptObject s = new ScriptObject();
            s.Type = ValueType.NIL;
            return s;
        }
        /// <summary>
        ///    产生一个 num
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        internal static ScriptObject CreateNum(double num)
        {
            ScriptObject s = new ScriptObject();
            s.Type = ValueType.NUMBER;
            s.Value.Number = num;
            return s;
        }
        /// <summary>
        ///    产生一个bool
        /// </summary>
        /// <param name="boolean"></param>
        /// <returns></returns>
        internal static ScriptObject CreateBool(bool boolean)
        {
            ScriptObject s = new ScriptObject();
            s.Type = ValueType.BOOLEAN;
            s.Value.Boolean = boolean;
            return s;
        }

        /// <summary>
        ///    产生一个表
        /// </summary>
        /// <returns></returns>
        internal static ScriptObject CreateTable()
        {
            ScriptObject s = new ScriptObject();
            s.Type = ValueType.TABLE;
            s.Value.RefPartHandle = RefPartBase.CreateTablePart();
            return s;
        }

        /// <summary>
        ///    产生一个字符串
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        internal static ScriptObject CreateString(string value)
        {
            ScriptObject s = new ScriptObject();
            s.Type = ValueType.STRING;
            s.Value.RefPartHandle = RefPartBase.CreateStrPart(value);
            return s;
        }

        /// <summary>
        ///    产生一个函数
        /// </summary>
        /// <param name="function"></param>
        /// <returns></returns>
        internal static ScriptObject CreateFunction(Action func, int argCount, bool isUncertainParameters)
        {
            ScriptObject s = new ScriptObject();
            s.Type = ValueType.FUNCTION;
            s.Value.RefPartHandle = RefPartBase.CreateFuncPart(func, argCount, isUncertainParameters);
            return s;
        }


        /// <summary>
        ///    添加字段
        /// </summary>
        /// <param name="name"></param>
        /// <param name="obj"></param>
        internal void AddFileld(string name, ScriptObject obj)
        {
            Contract.Assert(Type == ValueType.TABLE);
            Value.RefPartHandle.ConverToTablePart().AddFileld(name, obj);
        }

        internal void AddFileld(int index, ScriptObject obj)
        {
            Contract.Assert(Type == ValueType.TABLE);
            Value.RefPartHandle.ConverToTablePart().AddFileld(index, obj);
        }

        internal void AddInArray(ScriptObject obj)
        {
            Contract.Assert(Type == ValueType.TABLE);
            Value.RefPartHandle.ConverToTablePart().AddInArray(obj);
        }

        /// <summary>
        ///    以名称获取字段,没有就返回nil
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public ScriptObject GetFileld(string filed)
        {
            Contract.Assert(Type == ValueType.TABLE);

            TablePart tablePart = Value.RefPartHandle.ConverToTablePart();

            ScriptObject s = tablePart.TryGetValue(filed);

            if (s != null) return s;

            ScriptObject metaTable = tablePart.MetaTable;

            if (metaTable != null)
            {
                s = metaTable.GetFileld("_index");

                if (s.Type == ValueType.TABLE)
                {
                    return s.GetFileld(filed);
                }
                else if (s.Type == ValueType.FUNCTION)
                {
                    //多一次拷贝(实际上)
                    //RunEnvironment.Instance.LocalStack.InvokeFunction(s, this, ScriptObject.CreateString(filed));

                    StackState state = RunEnvironment.Instance.LocalStack;

                    state.PushFunction(s);

                    state.Push(this);

                    state.Push(filed);

                    return state.InvokeFunction();
                }
            }
            return RunEnvironment.Nil;
        }

        internal ScriptObject GetFileld(ScriptObject index)
        {
            Contract.Assert(Type == ValueType.TABLE);

            ScriptObject s = null;

            switch (index.Type)
            {
                case ValueType.NUMBER:

                    s = Value.RefPartHandle.ConverToTablePart().IndexAt((int)index.Value.Number);

                    break;

                case ValueType.STRING:

                    s = Value.RefPartHandle.ConverToTablePart().TryGetValue(index.Value.RefPartHandle.ConverToStringPart().Value);

                    break;

                default:
                    Contract.Assert(false);
                    break;
            }
            if (s == null) return RunEnvironment.Nil;

            return s;
        }


        public void RemoveFileld(string filed)
        {
            Contract.Assert(Type == ValueType.TABLE);
            Value.RefPartHandle.ConverToTablePart().Remove(filed);
        }

        internal void RemoveFileld(ScriptObject index)
        {
            Contract.Assert(Type == ValueType.TABLE);

            switch (index.Type)
            {
                case ValueType.NUMBER:

                    int at = (int)index.Value.Number;

                    Value.RefPartHandle.ConverToTablePart().Remove(at);

                    break;

                case ValueType.STRING:

                    string key = index.Value.RefPartHandle.ConverToStringPart().Value;

                    Value.RefPartHandle.ConverToTablePart().Remove(key);

                    break;

                default:
                    Contract.Assert(false);
                    break;
            }
        }

        /// <summary>
        ///    获取字段,没有的话就产生一个nil
        /// </summary>
        /// <param name="filed"></param>
        /// <returns></returns>
        public ScriptObject GetOrCreateFileld(string filed)
        {
            Contract.Assert(Type == ValueType.TABLE);

            TablePart table = Value.RefPartHandle.ConverToTablePart();

            ScriptObject s = table.TryGetValue(filed);

            if (s == null)
            {
                s = ScriptObject.CreateNil();
                table.AddFileld(filed, s);
            }
            return s;
        }

        internal ScriptObject GetOrCreateFileld(ScriptObject index)
        {
            Contract.Assert(Type == ValueType.TABLE);

            TablePart table = Value.RefPartHandle.ConverToTablePart();

            ScriptObject s = null;

            switch (index.Type)
            {
                case ValueType.NUMBER:

                    int at = (int)index.Value.Number;

                    s = Value.RefPartHandle.ConverToTablePart().IndexAt(at);

                    if (s == null)
                    {
                        s = ScriptObject.CreateNil();
                    }
                    table.AddFileld(at, s);
                    break;

                case ValueType.STRING:

                    string key = index.Value.RefPartHandle.ConverToStringPart().Value;

                    s = table.TryGetValue(key);
                    if (s == null)
                    {
                        s = ScriptObject.CreateNil();
                    }
                    table.AddFileld(key, s);

                    break;

                default:
                    Contract.Assert(false);
                    break;
            }
            return s;
        }

        internal void ClearTable()
        {
            Contract.Assert(Type == ValueType.TABLE);

            TablePart table = Value.RefPartHandle.ConverToTablePart();

            table.Clear();
        }
    }
}
