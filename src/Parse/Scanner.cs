using System;
using ScriptInterpreter.Util;
using System.Diagnostics;


namespace ScriptInterpreter.Parse
{
    public sealed class Scanner : Tokens
    {
        private int _token;
        private int line;
        private int col;

        public int Line
        {
            get { return line; }
        }
        public int Col
        {
            get { return col; }
        }
        private string tokenStr;

        public string TokenString
        {
            get { return tokenStr; }
        }

        int _radix;


        public int Token
        {
            get { return _token; }
        }

        private char[] sbuf = new char[128];
        private int sp;

        private Keywords keywords;


        private string _buf;
        private int bp;
        private char ch;


        public bool IsTableValueKey()
        {
            if (_token == IDENTIFIER || _token == STRINGLITERAL)
            {
                int bp = this.bp;

                char ch = _buf[bp];

                while (ch == ' ' || ch == '\t' || ch == '\n' || ch == '\r')
                {
                    bp++;

                    if (bp >= Buflen - 1) return false;

                    ch = _buf[bp];
                }
                if (ch == ':') return true;

                return false;
            }
            return false;
        }



        private int Buflen
        {
            get { return _buf.Length; }
        }

        public int Pos
        {
            get { return bp; }
        }


        public Scanner()
        {
            keywords = new Keywords();
        }
        internal void InputString(string commandString)
        {
            _buf = commandString + EOI;

            line = 1;
            col = 0;
            bp = -1;

            ScanChar();//检查字符，并读入。
            NextToken();//读入下一个标示符。
        }

        public void Accept(char tokenchar)
        {
            if (ch == tokenchar)
            {
                ScanChar();
            }
            else
            {
                //语法错误处理
                Debug.WriteLine("语法错误处理");
            }
        }


        public void NextToken()
        {
            sp = 0;              //注意,每次移动一个标识符时,标志符缓冲区要清掉
            tokenStr = null;     //清掉
            while (true)
            {
                switch (ch)
                {
                    case ' ':     //空格

                    case TAB:     //  tab 键  水平平移

                    case FF:     //换页

                    case CR:     //回车

                    case LF:     //换行
                        ScanChar();
                        break;
                    case 'A':

                    case 'B':

                    case 'C':

                    case 'D':

                    case 'E':

                    case 'F':

                    case 'G':

                    case 'H':

                    case 'I':

                    case 'J':

                    case 'K':

                    case 'L':

                    case 'M':

                    case 'N':

                    case 'O':

                    case 'P':

                    case 'Q':

                    case 'R':

                    case 'S':

                    case 'T':

                    case 'U':

                    case 'V':

                    case 'W':

                    case 'X':

                    case 'Y':

                    case 'Z':

                    case 'a':

                    case 'b':

                    case 'c':

                    case 'd':

                    case 'e':

                    case 'f':

                    case 'g':

                    case 'h':

                    case 'i':

                    case 'j':

                    case 'k':

                    case 'l':

                    case 'm':

                    case 'n':

                    case 'o':

                    case 'p':

                    case 'q':

                    case 'r':

                    case 's':

                    case 't':

                    case 'u':

                    case 'v':

                    case 'w':

                    case 'x':

                    case 'y':

                    case 'z':

                    //  case '$':

                    case '_':
                        ScanIdent();
                        return;

                    case '0':
                        ScanChar();
                        if (ch == 'x' || ch == 'X')
                        {
                            ScanChar();
                            //if (digit(16) < 0)
                            //{
                            //    lexError("invalid.hex.number");
                            //}
                            //scanNumber(16);
                        }
                        else
                        {
                            PutChar('0');
                            ScanNumber(8);
                        }
                        return;

                    case '1':

                    case '2':

                    case '3':

                    case '4':

                    case '5':

                    case '6':

                    case '7':

                    case '8':

                    case '9':
                        ScanNumber(10);
                        return;
                    case '.':
                        ScanChar();
                        if(ch=='.')          //还是点的话就是字符串的链接
                        {
                             ScanChar();
                             if (ch == '.') 
                             {

                                 ScanChar();
                                 _token = UntPar;
                             }
                             else  _token = Link;
                        }
                        else   _token = DOT;
                        return;
                    case ',':
                        ScanChar();
                        _token = COMMA;
                        return;

                    case ';':
                        ScanChar();
                        _token = SEMI;
                        return;

                    case '(':
                        ScanChar();
                        _token = LPAREN;
                        return;

                    case ')':
                        ScanChar();
                        _token = RPAREN;
                        return;

                    case '[':
                        ScanChar();
                        _token = LBRACKET;
                        return;

                    case ']':
                        ScanChar();
                        _token = RBRACKET;
                        return;

                    case '{':
                        ScanChar();
                        _token = LBRACE;
                        return;

                    case '}':
                        ScanChar();
                        _token = RBRACE;
                        return;

                    case '/':
                        ScanChar();
                        if (ch == '/')
                        {
                            do
                            {
                                ScanChar();
                            } while (ch != CR && ch != LF);     //跳过一行
                            break;    //跳出switch
                        }
                        else if (ch == '*')   //段落注释
                        {
                            SkipComment();    //跳过注释

                            //  Accept('/');

                            if (ch == '/')
                            {
                                ScanChar();
                            }
                            else
                            {
                                //Error
                                throw new NotSupportedException();
                            }
                            break;  //跳出switch
                        }
                        else if (ch == '=')
                        {
                            _token = SLASHEQ;
                            ScanChar();
                        }
                        else
                        {
                            _token = SLASH;
                            return;
                        }
                        return;
                    case '\'':  //接受字符串
                        ScanChar();
                        while (ch != '\'' && ch != CR && ch != LF && bp < Buflen)
                            ScanLitChar();

                        if (ch == '\'')
                        {
                            _token = STRINGLITERAL;
                            ScanChar();
                        }
                        else
                        {
                            //  Error
                        }
                        tokenStr = new string(sbuf, 0, sp);
                        return;

                    case '\"':                   //接受字符串
                        ScanChar();
                        while (ch != '\"' && ch != CR && ch != LF && bp < Buflen)
                            ScanLitChar();

                        if (ch == '\"')
                        {
                            _token = STRINGLITERAL;
                            ScanChar();
                        }
                        else
                        {
                            //  Error
                        }
                        tokenStr = new string(sbuf, 0, sp);
                        return;
                    default:
                        if (IsSpecial(ch))
                        {
                            ScanOperator();
                        }
                        else if (bp == Buflen || ch == EOI && bp + 1 == Buflen)
                        {
                            _token = EOF;
                        }
                        else if (!IsIdentifierPart(ch))
                        {
                            tokenStr = new string(sbuf, 0, sp);
                            _token = keywords.key(tokenStr);
                            return;
                        }
                        return;
                }

            }
        }

     // 读取字符和字符串中的下一个各种命令标志
        private void ScanLitChar()
        {
            if (ch == '\\')
            {
                if (_buf[bp + 1] == '\\' && 0 != bp)
                {
                    bp++;
                    col++;
                    PutChar('\\');
                    ScanChar();
                }
                else
                {
                    ScanChar();
                    switch (ch)
                    {
                        case '0':

                        case '1':

                        case '2':

                        case '3':

                        case '4':

                        case '5':

                        case '6':

                        case '7':
                            char leadch = ch;
                            int oct = Digit(8);
                            ScanChar();
                            if ('0' <= ch && ch <= '7')
                            {
                                oct = oct * 8 + Digit(8);
                                ScanChar();
                                if (leadch <= '3' && '0' <= ch && ch <= '7')
                                {
                                    oct = oct * 8 + Digit(8);
                                    ScanChar();
                                }
                            }
                            PutChar((char)oct);
                            break;

                        case 'b':
                            PutChar('\b');//各种字符串中的表示符号的处理方法
                            ScanChar();
                            break;

                        case 't':
                            PutChar('\t');
                            ScanChar();
                            break;

                        case 'n':
                            PutChar('\n');
                            ScanChar();
                            break;

                        case 'f':
                            PutChar('\f');
                            ScanChar();
                            break;

                        case 'r':
                            PutChar('\r');
                            ScanChar();
                            break;

                        case '\'':
                            PutChar('\'');
                            ScanChar();
                            break;

                        case '\"':
                            PutChar('\"');
                            ScanChar();
                            break;

                        case '\\':
                            PutChar('\\');
                            ScanChar();
                            break;

                        default:
                            throw new NotImplementedException();
                    }
                }
            }
            else if (bp != Buflen)
            {
                PutChar(ch);
                ScanChar();
            }
        }

        private int Digit(int p)
        {
            throw new NotImplementedException();
        }

        private void SkipComment()
        {
            while (bp < Buflen)
            {
                switch (ch)
                {
                    case '*':
                        ScanChar();
                        if (ch == '/')
                            return;
                        break;

                    default:
                        ScanChar();
                        break;

                }
            }
        }

        private void ScanNumber(int radix)
        {
            _radix = radix;

            int digitRadix = (radix <= 10) ? 10 : 16;

            while (Char.IsDigit(ch) == true)
            {
                PutChar(ch);
                ScanChar();
            }
            if (radix <= 10 && ch == '.')
            {
                PutChar(ch);
                ScanChar();
                ScanFractionAndSuffix();
            }
            else if (radix <= 10 && (ch == 'e' || ch == 'E' || ch == 'f' || ch == 'F' || ch == 'd' || ch == 'D'))
            {
                // scanFractionAndSuffix();
            }
            else
            {
                if (ch == 'l' || ch == 'L')
                {
                    ScanChar();
                    //token = LONGLITERAL;
                }
                else
                {
                   
                }
            }
            _token = NUMBER;
            tokenStr = new string(sbuf, 0, sp);
        }

        
        //读取浮点数的小数部分和f，d后缀。  
        private void ScanFractionAndSuffix()
        {
            while (Char.IsDigit(ch) == true)
            {
                PutChar(ch);
                ScanChar();
            }
        }

        /// <summary>
        ///   读取标识符
        /// </summary>
        private void ScanIdent()
        {
            do
            {
                if (sp == sbuf.Length)
                    PutChar(ch);
                else
                    sbuf[sp++] = ch;

                ScanChar();

                switch (ch)
                {
                    case 'A':

                    case 'B':

                    case 'C':

                    case 'D':

                    case 'E':

                    case 'F':

                    case 'G':

                    case 'H':

                    case 'I':

                    case 'J':

                    case 'K':

                    case 'L':

                    case 'M':

                    case 'N':

                    case 'O':

                    case 'P':

                    case 'Q':

                    case 'R':

                    case 'S':

                    case 'T':

                    case 'U':

                    case 'V':

                    case 'W':

                    case 'X':

                    case 'Y':

                    case 'Z':

                    case 'a':

                    case 'b':

                    case 'c':

                    case 'd':

                    case 'e':

                    case 'f':

                    case 'g':

                    case 'h':

                    case 'i':

                    case 'j':

                    case 'k':

                    case 'l':

                    case 'm':

                    case 'n':

                    case 'o':

                    case 'p':

                    case 'q':

                    case 'r':

                    case 's':

                    case 't':

                    case 'u':

                    case 'v':

                    case 'w':

                    case 'x':

                    case 'y':

                    case 'z':

                    //   case '$':

                    case '_':

                    case '0':

                    case '1':

                    case '2':

                    case '3':

                    case '4':

                    case '5':

                    case '6':

                    case '7':

                    case '8':

                    case '9':
                        break;

                    default:
                        if (!IsIdentifierPart(ch) || bp >= Buflen)
                        {
                            tokenStr = new string(sbuf, 0, sp);
                            _token = keywords.key(tokenStr);
                            return;
                        }
                        break;
                }
            } while (true);

        }

        /// <summary>
        ///   读取操作符
        /// </summary>
        private void ScanOperator()
        {
            while (true)
            {
                PutChar(ch);
                string newname = new string(sbuf, 0, sp);
                if (keywords.key(newname) == IDENTIFIER)
                {
                    sp--;
                    break;
                }
                tokenStr = newname;
                _token = keywords.key(newname);
                ScanChar();
                if (!IsSpecial(ch))   //不是
                    break;
            }
        }

        // 如果是操作符得一部分，则返回true，用于判断操作符。      
        public bool IsSpecial(char ch)
        {
            switch (ch)
            {
                case '!':

                case '%':

                case '&':

                case '*':

                case '?':

                case '+':

                case '-':

                case ':':

                case '<':

                case '=':

                case '>':

                case '^':

                case '|':

                case '~':
                    return true;

                default:
                    return false;

            }
        }

        private bool IsIdentifierPart(char ch)
        {
            return false;   //简单处理
        }

        private void PutChar(char ch)
        {
            if (sp == sbuf.Length)
            {
                Array.Resize(ref sbuf, sbuf.Length * 2);
            }
            sbuf[sp++] = ch;
        }
        private void ScanChar()
        {
            bp++;
            ch = _buf[bp];
            switch (ch)
            {
                case '\r':    //回车
                    col = 0;
                    line++;
                    break;
                case '\n':      //换行

                    if (bp == 0 || _buf[bp - 1] != '\r')
                    {
                        col = 0;
                        line++;
                    }
                    break;

                case '\t':     //制表符
                    col = (col / TabInc * TabInc) + TabInc;
                    break;

                default:
                    col++;
                    break;

            }
        }
    }
}

