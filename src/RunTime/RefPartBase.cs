using System;
using System.Net;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace ScriptInterpreter.RunTime
{
    public abstract class RefPartBase
    {
        internal StringPart ConverToStringPart()
        {
            Contract.Assert(this is StringPart);
            return this as StringPart;
        }
        internal FuncPart ConverToFuncPart()
        {
            Contract.Assert(this is FuncPart);
            return this as FuncPart;
        }

        internal TablePart ConverToTablePart()
        {
            Contract.Assert(this is TablePart);
            return this as TablePart;
        }

        public string StringValue
        {
            get { return ConverToStringPart().Value; }
        }

        public static StringPart CreateStrPart(string value)
        {
            return new StringPart(value);
        }

        public static FuncPart CreateFuncPart(Action func, int argCount, bool isUncertainParameters)
        {
            return new FuncPart(func, argCount,isUncertainParameters);
        }

        internal static TablePart CreateTablePart()
        {
            return new TablePart();
        }

    }

    public sealed class StringPart : RefPartBase
    {
        private string _value;

        public string Value
        {
            get { return _value; }
            set { _value = value; }
        }

        public StringPart(string value)
        {
            _value = value;
        }
    }

    public struct UntParAndCount
    {
        public Int32 Count;
        public bool IsUncertainParameters;
    }

    public sealed class FuncPart : RefPartBase
    {
        private Action _value;

        public Action Value
        {
            get { return _value; }
            set { _value = value; }
        }
        private Int32 _argsCount;

        public Int32 ArgsCount
        {
            get { return _argsCount; }
            set { _argsCount = value; }
        }

        private ScriptObject[] _upVals;

        public bool IsUncertainParameters;

        public ScriptObject[] UpVals
        {
            get { return _upVals; }

            set { _upVals = value; }
        }

        public FuncPart(Action func, Int32 argCount, bool isUncertainParameters)
        {
            _value = func;

            _argsCount = argCount;

            IsUncertainParameters = isUncertainParameters;
        }
    }

    public sealed class TablePart : RefPartBase
    {
        private Dictionary<string, ScriptObject> _value = new Dictionary<string,ScriptObject>();

        //数组段的利用率以不低于 50% 为准
        private const int N_Size = 50;

        //默认数组段大小为0
        private ScriptObject[]  _array = new ScriptObject[0];

        public ScriptObject[] ArrayPart
        {
            get { return _array; }
        }

        public ScriptObject MetaTable { get; set; }


        //数组部分实际有效大小
        private int count;

        public int Count
        {
            get { return count; }
        }

        public TablePart() { }

        internal void Clear()
        {
            _value.Clear();

            Array.Clear(_array, 0, _array.Length);
        }
        /// <summary>
        ///    先这样处理吧,没有找到算法的具体实现
        /// </summary>
        public void AddFileld(int index,ScriptObject value)
        {
            int newsize;

            if (index < 0)
            {
                AddFileld(index.ToString(), value);
            }
            else if (index < _array.Length)
            {
                _array[index] = value;
                ++count;
            }
            else if ((newsize = (count + 1) * 2) >= index)
            {
                Array.Resize(ref _array, newsize);
                _array[index] = value;
                ++count;
            }
            else
            {
                AddFileld(index.ToString(), value);
            }
        }
   
        public void AddFileld(string fileldName, ScriptObject scriptValue)
        {
            _value[fileldName] = scriptValue;
        }

        internal void AddInArray(ScriptObject obj)
        {
            AddFileld(count,obj);
        }
        internal bool Remove(string filed)
        {
            return _value.Remove(filed);
        }

        internal bool Remove(int index)
        {
            if (index < 0 || index >= _array.Length)

                return Remove(index.ToString());

            _array[index] = null;
            return true;
        }

        /// <summary>
        ///   不存在就返回null
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public ScriptObject TryGetValue(string name)
        {
            ScriptObject resoult = null;

            if (_value.TryGetValue(name, out resoult) == true)
            {
                return resoult;
            }
            return resoult;
        }

        public ScriptObject IndexAt(int index)
        {
            if (index < 0 || index >= _array.Length) return TryGetValue(index.ToString());

            return _array[index];
        }


     
    }
}
