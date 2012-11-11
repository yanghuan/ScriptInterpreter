using System.Collections.Generic;
using ScriptInterpreter.Util;

namespace ScriptInterpreter.Parse
{
    public class Keywords : Tokens
    {
        private Dictionary<string, int>  _table = new Dictionary<string,int>();

        public Keywords()
        {
            enterKeyword("+", PLUS);
            enterKeyword("-", SUB);
            enterKeyword("!", BANG);
            enterKeyword("%", PERCENT);
            enterKeyword("*", STAR);
            enterKeyword("/", SLASH);
            enterKeyword(">", GT);
            enterKeyword("<", LT);
            enterKeyword("?", QUES);
            enterKeyword(":", COLON);
            enterKeyword("=", EQ);
            enterKeyword("++", PLUSPLUS);
            enterKeyword("--", SUBSUB);
            enterKeyword("==", EQEQ);
            enterKeyword("<=", LTEQ);
            enterKeyword(">=", GTEQ);
            enterKeyword("!=", BANGEQ);

            enterKeyword("local",LOCAL);
            enterKeyword("function", FUNCTION);
            enterKeyword("nil", Nil);
            enterKeyword("this",THIS);
            enterKeyword("false", FALSE);
            enterKeyword("true", TRUE);
            enterKeyword("do", DO);
            enterKeyword("else", ELSE);
            enterKeyword("for", FOR);
            enterKeyword("if", IF);
            enterKeyword("return", RETURN);
            enterKeyword("while", WHILE);
            enterKeyword("break", BREAK);
        }

        private void enterKeyword(string s, int token)
        {
            _table.Add(s,token);
        }

        internal int key(string name)
        {
            int com;
            if (_table.TryGetValue(name, out com) == true)
            {
                return com;
            }
            return IDENTIFIER;
        }
    }
}
