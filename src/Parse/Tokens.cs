namespace ScriptInterpreter.Parse
{
    public class Tokens
    {
        protected Tokens() { }

        /**
    * Tabulator column increment.
    */
        public const int TabInc = 8;     //tab  键水平移动的字符数

        /**
         * Tabulator character.
         */
        public const char BS = (char)8;     //退格

        public const char TAB = (char)9;    //tab键  水平制表符  '\t'


        public const char SP = ' ';       //空格
        /**
         * Line feed character.
         */
        public const char LF = (char)10;      //换行键    '\n'


        public const char CR = (char)13;    //回车键     '\r'

        /**
         * Form feed character.
         */

        //换页键  
        public const char FF = (char)12;

        /**
         * Carriage return character.
         */


        /**
         * End of input character.  Used as a sentinel to denote the
         *  character one beyond the last defined character in a
         *  source file.
         */
        public const char EOI = (char)26;

        public const int EOF = 0;
        public const int ERROR = EOF + 1;
        public const int IDENTIFIER = ERROR + 1;

        public const int PLUS = IDENTIFIER + 1;       //  "+"
        public const int SUB = PLUS + 1;             //  "-"
        public const int BANG = SUB + 1;            //   "!"
        public const int PERCENT = BANG + 1;       //  "%"
        public const int STAR = PERCENT + 1;       //   *
        public const int SLASH = STAR + 1;    //    /
        public const int GT = SLASH + 1;        //  ">"
        public const int LT = GT + 1;             //  "<"
        public const int QUES = LT + 1;          //   ?
        public const int COLON = QUES + 1;        //   :
        public const int EQ = COLON + 1;                  //    "="
        public const int PLUSPLUS = EQ + 1;            //  "++"
        public const int SUBSUB = PLUSPLUS + 1;       //  "--"
        public const int EQEQ = SUBSUB + 1;             //   "=="
        public const int LTEQ = EQEQ + 1;               //   "<="
        public const int GTEQ = LTEQ + 1;               //   ">="
        public const int BANGEQ = GTEQ + 1;              //  "!="


        public const int PLUSEQ = BANGEQ + 1;            //  "+="
        public const int SUBEQ = PLUSEQ + 1;             //  "-="
        public const int STAREQ = SUBEQ + 1;             //   "*="
        public const int SLASHEQ = STAREQ + 1;           //   "/="
        public const int AMPEQ = SLASHEQ + 1;           //    "&="
        public const int BAREQ = AMPEQ + 1;             //     "|="
        public const int CARETEQ = BAREQ + 1;           //    "^="
        public const int PERCENTEQ = CARETEQ + 1;       //    "%="

        public const int GLOBAL = PERCENTEQ + 1;       //  global
        public const int LOCAL = GLOBAL + 1;       //local
        public const int FUNCTION = LOCAL + 1;     //function
        public const int NUMBER = FUNCTION + 1;
        public const int Nil = NUMBER + 1;         //  nil

        public const int THIS = Nil + 1;          // this  
        public const int FALSE = THIS + 1;          //false
        public const int TRUE = FALSE + 1;          //true
        public const int BREAK = FALSE + 1;         //break
        public const int CONTINUE = BREAK + 1;       // continue
        public const int STRINGLITERAL = CONTINUE + 1;    //  string
        public const int Link = STRINGLITERAL + 1;    //   ..

        public const int UntPar = Link + 1;   //  ...

        public const int CHAR = UntPar + 1;      //char
        public const int DO = CHAR + 1;           //do
        public const int ELSE = DO + 1;
        public const int FOR = ELSE + 1;
        public const int IF = FOR + 1;            //if
        public const int INT = IF + 1;
        public const int RETURN = INT + 1;      //return
        public const int WHILE = RETURN + 1;         //while

        public const int DOT = WHILE + 1;     //  .
        public const int COMMA = DOT + 1;      // ,
        public const int SEMI = COMMA + 1;       // ;
        public const int LPAREN = SEMI + 1;    // ( 
        public const int RPAREN = LPAREN + 1;  // )
        public const int LBRACKET = RPAREN + 1;  //[
        public const int RBRACKET = LBRACKET + 1;   //]
        public const int LBRACE = RBRACKET + 1;   // {
        public const int RBRACE = LBRACE + 1;      //  }
    }
}
