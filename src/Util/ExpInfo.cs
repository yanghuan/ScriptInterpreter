using System;
using System.Net;
using ScriptInterpreter.Parse;
using System.Diagnostics.Contracts;

namespace ScriptInterpreter.Util
{
    /// <summary>
    ///    表达式工具类吧
    /// </summary>
    public static class ExpInfo
    {
        public const int OrPrec = 4;

        //相等和不等的优先级数,例如 == ,!=
        public const  int EqPrec = 9;

        //比较运算的优先级数  ,例如 >,<,>=,<=,
        public const int OrdPrec = 10;

        //加和减运算优先级,  + ,-
        public const int AddPrec = 12;

        //乘除,取 mod 运算优先级数 , *,/,%(注意mod运算目前不支持,小数不能取mod)
        public const int MulPrec = 13;


        internal static int OpPrec(int token)
        {
            switch(token)
            {
                case Tokens.EQEQ:         //  ==
                case Tokens.BANGEQ:       // !=
                         return EqPrec;

                case Tokens.GT:            //  >
                case Tokens.GTEQ:         //   >=
                case Tokens.LT:            //  <
                case Tokens.LTEQ:         //   <=
                         return OrdPrec;

                case Tokens.PLUS:         //  +
                case Tokens.SUB:           //  -
                        return AddPrec;            //加和减据返回12 优先级数

                case Tokens.STAR:          //  *      
                case Tokens.SLASH:         //  /
                case Tokens.PERCENT:       //  %
                        return MulPrec;

                    default:
                        return -1;    //其他操作符,均设置为最低
            }
           
        }
    }
}
