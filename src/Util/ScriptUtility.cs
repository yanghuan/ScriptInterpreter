using System;
using ScriptInterpreter.Parse;

namespace ScriptInterpreter.Util
{
    public struct TupleStruct<T0,T1>
    {
        public T0 First;
        public T1 Second;

        public TupleStruct(T0 t0,T1 t1)
        {
            First = t0;
            Second = t1;
        }
    }

    public class ScriptCompileException : Exception
    {
        private ScriptCompileException(string message)
            : base(message)
        {
        }

        public static ScriptCompileException CreateContentExist(string symbol)
        {
            return new ScriptCompileException(string.Format("上下文已存在符号 {0} ", symbol));
        }

        public static ScriptCompileException CreateIsNotIdentifier(string symbol)
        {
            return new ScriptCompileException(string.Format("符号 {0} 不是标识符", symbol));
        }

        public static ScriptCompileException CreateSyntaxError(int line,int col,int token,string symbol)
        {
            switch (token)
            {
                case Tokens.PLUS:
                    symbol = "+";
                    break;

                case Tokens.SUB:
                    symbol = "-";
                    break;
                default:
                    break;
            }
            return new ScriptCompileException(string.Format("{0}行{1}列->符号{2}附近存在语法错误", line, col,symbol));
        }
    }


    public class ScriptRunTimeException : Exception
    {

        public ScriptRunTimeException(string message)
            : base(message)
        {
            
        }



    }
}
