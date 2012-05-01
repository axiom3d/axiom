using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace Axiom.RenderSystems.OpenGLES2.GLSLES
{
    /// <summary>
    /// This is a simplistic C/C++ like preprocessor
    /// It takes a non-zero-terminated string on input and outputs a
    /// non-zero terminated string buffer.
    /// 
    /// This preprocessor was designed specifically for GLSL ES shaders, so
    /// if you want to use for other purposes you might want to check if the feature set it provides is enough for you.
    /// 
    /// Here's a list of supported features
    /// <list type="bullet">
    /// <item>
    /// <description>Fast memroy allocation-less operation (mostly).</description>
    /// </item>
    /// <item>
    /// <description>Line continuation (backslash-newline) is swallowed.</description>
    /// </item>
    /// <item>
    /// <description>Line numeration is fully preserved by inserting empty lines where required.
    /// This is crusical if, say, GLSL ES compiler reports you an error with a line number.</description>
    /// </item>
    /// <item>
    /// <description>#define: Parameterized and non-parameterized macros. Invoking a macro with
    /// less arguments that it takes assigns empty values to missing arguments</description>
    /// </item>
    /// <item>
    /// <description>#undef: Forget defined macros</description>
    /// </item>
    /// <item>
    /// <description>#ifdef/#ifndef/#else/#endif: Condiational suppresion of parts of code.</description>
    /// </item>
    /// <item>
    /// <description>#if: Supports numeric expresiion of any complexity, also supports the defined() psuedo-function.</description>
    /// </item>
    /// </list>
    /// </summary>
    class GLSLESPreprocessor
    {
        static int MaxMacroArgs = 16;
        static double ClosestPow2(int x)
        {
            if ((x & (x - 1)) == 0)
                return x;
            var max = System.Math.Pow(2, System.Math.Ceiling(System.Math.Log(x, 2)));
            var min = max / 2;
            return (max - x) > (x - min) ? min : max;
        }
        
        /// <summary>
        /// An input token.
        /// </summary>
        public class Token
        {
            public enum Kind
            {
                EOS, 
                Error, 
                Whitespace,
                Newline,
                Linecont,
                Number,
                Keyword,
                Punctuation,
                Directive,
                String,
                Comment,
                Linecomment,
                Text
            }
            public Kind Type;
            public int Length;
            StringBuilder buffer;
            static Token error = null;

            public string String
            {
                get { return buffer.ToString(); }
                set { this.buffer = new StringBuilder(value); }
            }

            public Token(Kind type)
                :this(type, string.Empty, 0)
            { }
            public Token(Kind type, string str, int length)
            {
                this.Type = type;
                this.String = str;
                this.Length = length;
                buffer = new StringBuilder();
                
            }
            public static Token Error
            {
                get
                {
                    if (error == null)
                    {
                        error = new Token(Kind.Error);
                    }

                    return error;
                }
            }

            public static bool operator ==(Token left, Token right)
            {
                return left.Type == right.Type && left.String == right.String && left.Length == right.Length; 
            }
            public static bool operator !=(Token left, Token right)
            {
                if (left.Type != right.Type || left.String != right.String || left.Length != right.Length)
                    return false;

                return true;
            }
            public void Append(string str, int length)
            {
                Token t = new Token(Kind.Text, str, length);
                Append(t);
            }
            public void Append(Token other)
            {
                if (other.String == string.Empty || other.String == null)
                    return;

                if (this.String == string.Empty || this.String == null)
                {
                    this.String = other.String;
                    this.Length = other.Length;
                    return;
                }

                buffer.Append(other.String);
                Length += other.Length;
            }
            public void AppendNL(int count)
            {
                string newLines = "\n" + "\n" + "\n" + "\n" + "\n" + "\n" + "\n" + "\n";

                while(count > 8)
                {
                    Append(newLines, 8);
                    count -= 8;
                }
                if (count > 0)
                    Append(newLines, count);
            }
            public bool GetValue(out long value)
            {
                value = 0;

                long val = 0;
                int i = 0;

                while (Char.IsWhiteSpace(String[i]))
                {
                    i++;
                }

                long baseVal = 10;
                if (String[i] == '0')
                {
                    if ((Length > i + 1) && String[i + 1] == 'x')
                    {
                        baseVal = 16;
                        i += 2;
                    }
                    else
                        baseVal = 8;
                }

                for (; i < Length; i++)
                {
                    char c = String[i];
                    if (Char.IsWhiteSpace(c))
                        break;

                    if (c >= 'a' && c <= 'z')
                    {
                        c -= (char)('a' - 'A');
                    }

                    c -= '0';
                    if (c < 0)
                        return false;

                    if (c > 9)
                    {
                        c -= (char)('A' - '9' - 1);
                    }
                    if (c >= baseVal)
                        return false;

                    val = (val * baseVal) + c;
                }
                //Check that all other characters are just spaces
                for (; i < Length; i++)
                {
                    if (!Char.IsWhiteSpace(String[i]))
                        return false;
                }

                value = val;
                return true;
            }
            public void SetValue(bool value)
            {
                this.SetValue(Convert.ToInt64(value));
            }
            public void SetValue(long value)
            {
                string temp = value.ToString();
                Length = 0;
                Append(temp, temp.Length);
                this.Type = Kind.Number;
            }
            public int CountNL
            {
                get
                {
                    if (Type == Kind.EOS || Type == Kind.Error)
                        return 0;

                    int newLines = 0;
                    for (int i = 0; i < String.Length; i++)
                    {
                        if (String[i] == '\n')
                            newLines++;
                    }

                    return newLines;
                }
            }

        }

        public class Macro
        {
            public Token Name;
            public int NumArgs;
            public Token[] Args;
            public Token Value;
            public Token Body;

            public Macro Next;
            public delegate Macro ExpandFunc(GLSLESPreprocessor parent, int numArgs, Token args);
            public bool Expanding;

            public Macro(Token name)
            {
                this.Name = name;
                NumArgs = 0;
                Args = null;
                Next = null;
                Expanding = false;
                
            }
            ~Macro()
            {
                Args = null;
                Next = null;
            }

            public Token Expand(int numArgs, Token[] args, Macro macros)
            {
                Expanding = true;

                GLSLESPreprocessor cpp = new GLSLESPreprocessor();
                cpp.MacroList = macros;
                //Define a new macro for every argument
                int i;
                for (i = 0; i < numArgs; i++)
                {
                    cpp.Define(Args[i].String, Args[i].Length, args[i].String, args[i].Length);
                }
                //The rest arguments are empty
                for (; i < NumArgs; i++)
                {
                    cpp.Define(Args[i].String, Args[i].Length, string.Empty, 0);
                }
                //Now run the macro expansion through the supplimentary preprocessor
                Token xt = cpp.Parse(Value);

                Expanding = false;

                //Remove the extra macros we have defined
                for (i = NumArgs - 1; i >= 0; i--)
                {
                    cpp.Undef(Args[i].String, Args[i].Length);
                }

                cpp.MacroList = null;

                return xt;
            }
        }

        string Source;
        string SourceEnd;
        int Line;
        bool BOL;
        bool[] outputsEnabled = new bool[32];
        Macro MacroList;
        public delegate void ErrorHandlerFunc(object data, int line, string error, string token, int tokenLength);

        public static ErrorHandlerFunc ErrorHandler;
        /// <summary>
        /// User-specific storage, passed to Error()
        /// </summary>
        public object ErrorData;

        private GLSLESPreprocessor(Token token, int line)
        {
            ErrorHandler = DefaultError;
            Source = token.String;
            SourceEnd = token.String + token.Length.ToString();
            for (int i = 0; i < outputsEnabled.Length; i++)
            {
                outputsEnabled[i] = true;
            }
            Line = line;
            BOL = true;
        }
        public GLSLESPreprocessor()
        {
            MacroList = new List<Macro>();
        }
        ~GLSLESPreprocessor()
        {
            MacroList = null;
        }
        public Token GetToken(bool expand)
        {
            int index = 0;
            char c = Source[index];
            if (c == '\n' || Source == System.Environment.NewLine)
            {
                Line++;
                BOL = true;
                if (c == '\r')
                {
                    index++;
                }
                return new Token(Token.Kind.Newline, Source, Source.Length - index);
            }
            else if (Char.IsWhiteSpace(c))
            {
                while (index < Source.Length && Source[index] != '\r' && Source[index] != '\n' && Char.IsWhiteSpace(Source[index]))
                {
                    index++;
                }

                return new Token(Token.Kind.Whitespace, Source, index);

            }
            else if (Char.IsDigit(c))
            {
                BOL = false;
                if (c == '0' && Source[1] == 'x') //hex numbers
                {
                    index++;
                    while (index < Source.Length && char.IsNumber(Source[index]))
                    {
                        index++;
                    }
                }
                else
                    while (index < Source.Length && Char.IsDigit(Source[index]))
                    {
                        index++;
                    }

                return new Token(Token.Kind.Number, Source, index);
            }
            else if (c == '_' || Char.IsLetterOrDigit(c))
            {
                BOL = false;

                while (index < Source.Length && (Source[index] == '_' || Char.IsLetterOrDigit(Source[index])))
                {
                    index++;
                }

                return new Token(Token.Kind.Number, Source, index);
            }
            else if(c == '"' || c == '\'')
            {
                BOL = false;
                while (index < Source.Length && Source[index] != c)
	            {
	                if(Source[index] == '\\')
                    {
                        index++;
                        if(index >= Source.Length)
                            break;
                    }
                    if(Source[index] == '\n')
                        Line++;
                    index++;
	            }
                if(index < Source.Length)
                    index++;
                return new Token(Token.Kind.String, Source, index);
            }
            else if(c == '/' && Source[index] == '/')
            {
                BOL = false;
                index++;
                while (index < Source.Length && Source[index] != '\r' && Source[index] != '\n')
	            {
                    return new Token(Token.Kind.Linecomment, Source, index);
	            }
            }
            else if(c == '/' && Source[index] == '*')
            {
                BOL = false;
                index++;

                while (index < Source.Length && (Source[0] != '*' || Source[1] != '/'))
	            {
	                if(Source[index] == '\n')
                    {
                        Line++;
                    }
                    index++;
                }
                    if(index <  Source.Length && Source[index] == '*')
                        index++;
                    if(index < Source.Length && Source[index] == '/')
                        index++;
                    return new Token(Token.Kind.Comment, Source, index);
	            
            }
            else if(c == '#' && BOL)
            {
                //Skip all whitespaces after '#'
                while (index < Source.Length && Char.IsWhiteSpace(Source[index]))
	            {
	                index++;
	            }
                while(index < Source.Length && !char.IsWhiteSpace(Source[index]))
                    index++;
                return new Token(Token.Kind.Directive, Source, index);
            }
            else if(c == '\\' && index < Source.Length && (Source[index] == '\r' || Source[index] == '\n'))
            {
                //Treat backslash-newline as a whole token
                if(Source[index] == '\r')
                    index++;
                if(Source[index] == '\n')
                    index++;
                Line++;
                BOL = true;
                return new Token(Token.Kind.Linecont, Source, index);
            }
            else
            {
                BOL = false;
                //Handle double-char operators here

                if(c == '>' && Source[index] == '>' || Source[index] == '=')
                    index++;
                else if(c == '<' && Source[index] == '<' || Source[index] == '=')
                    index++;
                else if(c == '!' && Source[index] == '=')
                    index++;
                else if(c == '=' && Source[index] == '=')
                    index++;
                else if((c == '|' || c == '&' || c == '^') && Source[index] == c)
                    index++;

                return new Token(Token.Kind.Punctuation, Source, index);
            }

            
        }
        public Token HandleDirective(Token token, int line)
        {
            //Analzye preprocessor directive
            char directive = token.String[1];
            int dirlength = token.Length - 1;
            while (dirlength > 0 && Char.IsWhiteSpace(directive))
            {
                dirlength--;
                directive++;
            }

            int old_line = Line;

            //collect the remaining part of the directive until EOL
            Token t, last = null;
            bool done = false;
            do
            {
                t = GetToken(false);
                if (t.Type == Token.Kind.Newline)
                {
                    //No directive arguments
                    last = t;
                    t.Length = 0;
                    done = true;
                    break;
                }

            } while (t.Type == Token.Kind.Whitespace ||
                    t.Type == Token.Kind.Linecont || 
                t.Type == Token.Kind.Comment || 
                t.Type == Token.Kind.Linecomment);
            if(!done)
            {
                for (; ; )
                {
                    last = GetToken(false);
                    switch (last.Type)
                    {
                        case Token.Kind.EOS:
                            //Can happen and is not an error
                            done = true;
                            break;
                        case Token.Kind.Linecomment:
                        case Token.Kind.Comment:
                            //Skip comments in macros
                            continue;
                        case Token.Kind.Error:
                            return last;
                        case Token.Kind.Linecont:
                            continue;
                        case Token.Kind.Newline:
                            done = true;
                            break;

                        default: break;
                    }



                    if (done)
                        break;
                    else
                    {
                        t.Append(last);
                        t.Type = Token.Kind.Text;
                    }
                }
            }
            if (done)
            {
                EnableOutput = false;
                bool outputEnabled = EnableOutput;
                bool rc;
                if (outputEnabled)
                {
                    string dir = token.String;

                    if (dir.Contains("define"))
                    {
                        rc = HandleDefine(t, line);
                    }
                    else if (dir.Contains("undef"))
                        rc = HandleUnDef(t, line);
                    else if (dir.Contains("ifdef"))
                        rc = HandleIfDef(t, line);
                    else if (dir.Contains("ifndef"))
                    {
                        rc = HandleIfDef(t, line);
                        if (rc)
                            EnableOutput = true;
                    }
                    else if (dir.Contains("if"))
                        rc = HandleIf(t, line);
                    else if (dir.Contains("else"))
                        rc = HandleElse(t, line);
                    else if (dir.Contains("endif"))
                        rc = HandleEndIf(t, line);
                    else
                    {
                        //Unknown preprocessor directive, roll back and pass through
                        Line = old_line;
                        Source = token.String + token.Length;
                        token.Type = Token.Kind.Text;
                        return token;
                    }

                    if (!rc)
                        return new Token(Token.Kind.Error);

                    return last;
                }
            }

            //To make compiler happy
            return Token.Error;
        }
        public bool HandleDefine(Token body, int line)
        {
            GLSLESPreprocessor cpp = new GLSLESPreprocessor(body, line);

            Token t = cpp.GetToken(false);

            if (t.Type != Token.Kind.Keyword)
            {
                Error(line, "Macro name expected after #define", null);
                return false;
            }

            Macro m = new Macro(t);
            m.Body = body;
            t = cpp.GetArguments(m.NumArgs, m.Args, false);
            while (t.Type == Token.Kind.Whitespace)
            {
                t = cpp.GetToken(false);
            }

            switch (t.Type)
            {
                case Token.Kind.Newline:
                case Token.Kind.EOS:
                    //Assing "" to token
                    t = new Token(Token.Kind.Text, string.Empty, 0);
                    break;

                case Token.Kind.Error:
                    m = null;
                    return false;

                default:
                    t.Type = Token.Kind.Text;
                    t.Length = cpp.SourceEnd - t.String;
                    break;
            }

            m.Value = t;
            m.Next = MacroList;
            MacroList = m;
            return true;
        }
        public bool HandleUnDef(Token body, int line)
        {
            GLSLESPreprocessor cpp = new GLSLESPreprocessor(body, line);

            Token t = cpp.GetToken(false);

            if (t.Type != Token.Kind.Keyword)
            {
                Error(line, "Expecting a macro name after #undef, got", t);
                return false;
            }

            //Don't barf if macro does not exist = standard C behavior
            Undef(t.String, t.Length);

            do
            {
                t = cpp.GetToken(false);
            } while (t.Type == Token.Kind.Whitespace ||
                t.Type == Token.Kind.Comment || t.Type == Token.Kind.Linecomment);

            if (t.Type != Token.Kind.EOS)
            {
                Error(line, "Warning: Ignoring garbage after directive", t);
            }

            return true;
        }
        public bool HandleIfDef(Token body, int line)
        {
            if (EnableOutput)
            {
                Error(line, "Too many embedded #if directives", null);
                return false;
            }

           GLSLESPreprocessor cpp = new GLSLESPreprocessor(body, line);

            Token t = cpp.GetToken(false);

            if (t.Type != Token.Kind.Keyword)
            {
                Error(line, "Expecting a macro name after #ifdef, got", t);
                return false;
            }

            EnableOutput = false; //has to be set to false 32 times before this matters

            do
            {
                t = cpp.GetToken(false);
            } while (t.Type == Token.Kind.Whitespace ||
                       t.Type == Token.Kind.Comment ||
                t.Type == Token.Kind.Linecomment);

            if (t.Type != Token.Kind.EOS)
                Error(line, "Warning: Ignoring garbage after directive", t);

            return true;
        }
        public bool HandleIf(Token body, int line)
        {
            Macro defined = new Macro(new Token(Token.Kind.Keyword, "defined", 7));
            defined.Next = MacroList;
            defined.ExpandFunc = ExpandDefined;
            defined.NumArgs = 1;

            //Temporary add the defined() function to the macro list
            MacroList = defined;

            long val = 0;
            bool rc = GetValue(body, out val, line);

            //Restore the macro list
            MacroList = defined.Next;
            defined.Next = null;

            if (!rc)
                return false;

            for (int i = 0; i < outputsEnabled.Length; i++)
            {
                outputsEnabled[i] = true;
            }

            return true;

        }
        public bool HandleElse(Token body, int line)
        {
            bool singleFalse = false;

            for (int i = 0; i < outputsEnabled.Length; i++)
            {
                if (outputsEnabled[i] == false)
                {
                    singleFalse = true;
                    break;
                }
            }
            if (singleFalse)
            {
                Error(line, "#else without #if", null);
                return false;
            }

            //Negate the result of last #if
            EnableOutput ^= true;

            if (body.Length > 0)
            {
                Error(line, "Warning: Ignoring garbage after #else", body);

            }

            return true;
        }
        public bool HandleEndIf(Token body, int line)
        {
            if (EnableOutput == false)
            {
                Error(line, "#endif without #if", null);
                return false;
            }
            if (body.Length > 0)
                Error(line, "Warning: Ignoring garbage after #endif", body);

            return true;
        }
        public Token GetArgument(Token arg, bool expand)
        {
            do
            {
                arg = GetToken(expand);
            } while (arg.Type == Token.Kind.Whitespace ||
                        arg.Type == Token.Kind.Newline ||
                        arg.Type == Token.Kind.Comment ||
                        arg.Type == Token.Kind.Linecomment ||
                        arg.Type == Token.Kind.Linecont);

            if (!expand)
                if (arg.Type == Token.Kind.EOS)
                    return arg;
                else if (arg.Type == Token.Kind.Punctuation &&
                    (arg.String[0] == ',' ||
                    arg.String[0] == ')'))
                {
                    Token t = arg;
                    arg = new Token(Token.Kind.Text, string.Empty, 0);
                    return t;
                }
                else if (arg.Type != Token.Kind.Keyword)
                {
                    Error(Line, "Unexpected token", arg);
                    return new Token(Token.Kind.Error);
                }
            var length = arg.Length;
            while (true)
            {
                Token t = GetToken(expand);
                switch (t.Type)
                {
                    case Token.Kind.EOS:
                        Error(Line, "Unfinished list of arguments", null);
                        break;
                    case Token.Kind.Error:
                        return new Token(Token.Kind.Error);
                    case Token.Kind.Punctuation:
                        if (t.String[0] == ',' || t.String[0] == ')')
                        {
                            //Trim whitespaces at the end
                            arg.Length = length;
                            return t;
                        }
                        break;
                    case Token.Kind.Comment:
                    case Token.Kind.Linecomment:
                    case Token.Kind.Linecont:
                    case Token.Kind.Newline:
                        //ignore these tokens
                        continue;
                    case Token.Kind.Text:
                        break;
                    default:
                        break;
                }

                if (!expand && t.Type != Token.Kind.Whitespace)
                {
                    Error(Line, "Unexpected token", arg);
                    return new Token(Token.Kind.Error);
                }

                arg.Append(t);

                if (t.Type != Token.Kind.Whitespace)
                    length = arg.Length;
            }
        }
        public Token GetArguments(int numArgs, Token[] args, bool expand)
        {
            Token[] margs = new Token[MaxMacroArgs];
            int nargs = 0;

            //Suppose we'll leave by the wrong path
            numArgs = 0;
            args = null;

            Token t;
            do
            {
                t = GetToken(expand);
            } while (t.Type == Token.Kind.Whitespace ||
                t.Type == Token.Kind.Comment || t.Type == Token.Kind.Linecomment);

            if (t.Type != Token.Kind.Punctuation || t.String[0] != '(')
            {
                numArgs = 0;
                args = null;
                return t;
            }
            
                bool done = false;
            while (true)
            {
                if (nargs == MaxMacroArgs)
                {
                    Error(Line, "Too many arguments to macro", null);
                    return new Token(Token.Kind.Error);
                }

                t = GetArgument(args[nargs++], expand);

                switch (t.Type)
                {
                    case Token.Kind.EOS:
                        Error(Line, "Unfinished list of arguments", null);
                        break;
                    case Token.Kind.Error:
                        return new Token(Token.Kind.Error);

                    case Token.Kind.Punctuation:
                        if (t.String[0] == ')')
                        {
                            t = GetToken(expand);
                            done = true;
                            break;
                        }
                    default:
                        break;
                }
                if (done)
                    break;
            }

            if (done)
            {
                numArgs = nargs;
                args = new Token[nargs];
                for (int i = 0; i < nargs; i++)
                {
                    args[i] = margs[i];
                }

                return t;
            }
        }
        public Token GetExpression(Token result, int line)
        {
            return this.GetExpression(result, line, 0);
        }
        /**
 * Operator priority:
 *  0: Whole expression
 *  1: '(' ')'
 *  2: ||
 *  3: &&
 *  4: |
 *  5: ^
 *  6: &
 *  7: '==' '!='
 *  8: '<' '<=' '>' '>='
 *  9: '<<' '>>'
 * 10: '+' '-'
 * 11: '*' '/' '%'
 * 12: unary '+' '-' '!' '~'
 */
        public Token GetExpression(out Token result, int line, int opPriority)
        {
            string tmp = string.Empty;
            do
            {
                result = GetToken(true);

            } while (result.Type == Token.Kind.Whitespace ||
                        result.Type == Token.Kind.Newline ||
                        result.Type == Token.Kind.Comment ||
                        result.Type == Token.Kind.Linecomment ||
                        result.Type == Token.Kind.Linecont);

            Token op = new Token(Token.Kind.Whitespace, string.Empty, 0);

            //Handle unary operators here
            if (result.Type == Token.Kind.Punctuation && result.Length == 1)
                if (result.String[0] == '+' || result.String[0] == '-' || result.String[0] == '!' || result.String[0] == '~')
                {
                    char uop = result.String[0];
                    op = GetExpression(out result, line, 12);
                    long val;
                    if (!GetValue(result, val, line))
                    {
                        tmp = "Unary " + uop + " not applicable";
                        Error(line, tmp, result);
                        return new Token(Token.Kind.Error);
                    }

                    if (uop == '-')
                    {
                        result.SetValue(-val);
                    }
                    else if (uop == '!')
                    {
                        result.SetValue(!val);
                    }
                    else if (uop == '~')
                    {
                        result.SetValue(~val);
                    }
                    
                }
                else if (result.String[0] == '(')
                {
                    op = GetExpression(out result, line, 1);
                    if (op.Type == Token.Kind.Error)
                        return op;
                    if (op.Type == Token.Kind.EOS)
                    {
                        Error(line, "Unclosed parenthesis in #if expression", null);
                        return new Token(Token.Kind.Error);
                    }
                    op = GetToken(true);
                }

                while (op.Type == Token.Kind.Whitespace ||
                        op.Type == Token.Kind.Newline ||
                        op.Type == Token.Kind.Comment ||
                        op.Type == Token.Kind.Linecomment ||
                        op.Type == Token.Kind.Linecont)
                    
                {
                    op = GetToken(true);
                }

                while (true)
                {
                    if (op.Type != Token.Kind.Punctuation)
                        return op;

                    int prio = 0;
                    if(op.Length == 1)
                        switch (op.String[0])
                        {
                            case ')': return op;
                            case '|': prio = 4; break;
                            case '^': prio = 5; break;
                            case '&': prio = 6; break;
                            case '<':
                            case '>': prio = 8; break;
                            case '+':
                            case '-': prio = 10; break;
                            case '*':
                            case '/':
                            case '%': prio = 11; break;
                        }
                    else if(op.Length == 2)
                        switch (op.String[0])
                        {
                            case '|': if (op.String[1] == '|') prio = 2; break;
                            case '&': if (op.String[1] == '&') prio = 3; break;
                            case '=': if (op.String[1] == '=') prio = 7; break;
                            case '!': if (op.String[1] == '=') prio = 7; break;
                            case '<':
                                if (op.String[1] == '=')
                                    prio = 8;
                                else if (op.String[1] == '<')
                                    prio = 9;
                                break;
                            case '>':
                                if (op.String[1] == '=')
                                    prio = 8;
                                else if (op.String[1] == '>')
                                    prio = 9;
                                break;
                        }

                    if (prio == 0)
                    {
                        Error(line, "Expecting operator, got", op);
                        return new Token(Token.Kind.Error);
                    }

                    if (opPriority >= prio)
                        return op;

                    Token rop;
                    Token nextop = GetExpression(out rop, line, prio);
                    long vlop, vrop;
                    if (!GetValue(result, out vlop, line))
                    {
                        tmp = "Left operand of " + op.String + " is not a number";
                        Error(line, tmp, result);
                        return new Token(Token.Kind.Error);
                    }
                    if (!GetValue(result, out vrop, line))
                    {
                        tmp = "right operand of " + op.String + " is not a number";
                        Error(line, tmp, result);
                        return new Token(Token.Kind.Error);
                    }

                    switch (op.String[0])
                    {
                        case '|':
                            if (prio == 2)
                                result.SetValue(vlop || vrop);
                            else
                                result.SetValue(vlop | vrop);
                            break;
                        case '&':
                            if (prio == 3)
                                result.SetValue(vlop && vrop);
                            else
                                result.SetValue(vlop & vrop);
                            break;
                        case '<':
                            if (op.Length == 1)
                                result.SetValue(vlop < vrop);
                            else if (prio == 8)
                                result.SetValue(vlop <= vrop);
                            else if (prio == 9)
                                result.SetValue(vlop << vrop);
                            break;
                        case '>':
                            if (op.Length == 1)
                                result.SetValue(vlop > vrop);
                            else if (prio == 8)
                                result.SetValue(vlop >= vrop);
                            else if (prio == 9)
                                result.SetValue(vlop >> vrop);
                            break;
                        case '^': result.SetValue(vlop ^ vrop); break;
                        case '!': result.SetValue(vlop != vrop); break;
                        case '=': result.SetValue(vlop == vrop); break;
                        case '+': result.SetValue(vlop + vrop); break;
                        case '-': result.SetValue(vlop - vrop); break;
                        case '*': result.SetValue(vlop * vrop); break;
                        case '/':
                        case '%':
                            if (vrop == 0)
                            {
                                Error(line, "Division by zero", null);
                                return new Token(Token.Kind.Error);
                            }
                            if (op.String[0] == '/')
                                result.SetValue(vlop / vrop);
                            else
                                result.SetValue(vlop % vrop);
                            break;
                                
                    }
                    op = nextop;
                }
            
        }
        public bool GetValue(Token token, out long value, int line)
        {
            value = 0;
            Token r;
            Token vt = token;

            if ((vt.Type == Token.Kind.Keyword ||
                vt.Type == Token.Kind.Text ||
                vt.Type == Token.Kind.Number) &&
                (vt.String == string.Empty || vt.String == null))
            {
                Error(line, "Trying to evaluate an empty expression", null);
                return false;
            }

            if (vt.Type == Token.Kind.Text)
            {
                GLSLESPreprocessor cpp = new GLSLESPreprocessor(token, line);
                cpp.MacroList = MacroList;

                Token t = cpp.GetExpression(r, line);

                cpp.MacroList = null;

                if (t.Type == Token.Kind.Error)
                    return false;

                if (t.Type != Token.Kind.EOS)
                {
                    Error(line, "Garbage afer expression", t);
                    return false;
                }

                vt = r;
            }

            Macro m;
            switch (vt.Type)
            {
                case Token.Kind.EOS:
                case Token.Kind.Error:
                    return false;

                case Token.Kind.Text:
                case Token.Kind.Number:
                    if (!vt.GetValue(out value))
                    {
                        Error(line, "Not a numeric expression", vt);
                        return false;
                    }
                    break;
                case Token.Kind.Keyword:
                    //Try to expand the macro
                    if ((m = IsDefined(vt)) && !m.Expanding)
                    {
                        Token x = ExpandMacro(vt);
                        m.Expanding = true;
                        bool rc = GetValue(x, out value, line);
                        m.Expanding = false;
                        return rc;
                    }
                    //Undefined macro, "expand to 0 mimic cpp behaviour
                    value = 0;
                    break;
                default:
                    Error(line, "Unexpected token", vt);
                    break;
            }

            return true;
        }
        public Token ExpandMacro(Token token)
        {
            Macro cur = IsDefined(token);

            if (cur != null && !cur.Expanding)
            {
                Token args = null;
                int nargs = 0;
                int old_line = Line;

                if (cur.NumArgs != 0)
                {
                    Token t = GetArguments(nargs, args, (cur.ExpandFunc != null) ? false : true);
                    if (t.Type == Token.Kind.Error)
                    {
                        args = null;
                        return t;
                    }

                    //Put the token back into the source ppol; we'll handle it later
                    if (t.String != null && t.String != string.Empty)
                    {
                        Source = t.String;
                        Line -= t.CountNL;
                    }
                }

                if (nargs > cur.NumArgs)
                {
                    string tmp = string.Format("Macro {0}'s passed {1} arguments, but takes just {2}", cur.Name.Length, cur.Name.String, nargs);
                    Error(old_line, tmp, null);
                    return new Token(Token.Kind.Error);
                }

                Token tk = (cur.ExpandFunc != null) ? cur.ExpandFunc(this, nargs, args) : cur.Expand(nargs, args, MacroList);
                tk.AppendNL(Line - old_line);

                args = null;

                return tk;
            }

            return token;
        }
        public Macro IsDefined(Token token)
        {
            foreach (var macro in MacroList)
            {
                var cur = macro.Next;
                if (cur != null)
                {
                    if (cur.Name == token)
                        return cur;
                }
            }

            return null;
        }
        public static Token ExpandDefined(GLSLESPreprocessor parent, int numArgs, Token[] args)
        {
            if (numArgs != 1)
            {
                parent.Error(parent.Line, "The defined() function takes exactly one argument", null);
                return new Token(Token.Kind.Error);
            }

            string v = parent.IsDefined(args[0]) ? "1" : "0";
            return new Token(Token.Kind.Number, v, 1);
        }

        public Token Parse(Token source)
        {
            Source = source.String;
            int sourceEned = Source.Length;
            Line = 1;
            BOL = true;
            EnableOutput = true;

            //Accumulate ouput into this token
            Token output = new Token(Token.Kind.Text);
            int emptyLines = 0;

            //Enabel output only if all embedded #if's were true
            bool oldOutputEnabled = true;
            bool outputEnabled = true;
            int outputDisabledLine = 0;

            for (int i = 0; i < sourceEned; i++)
            {
                int oldLine = Line;
                Token t = GetToken(true);

            NextToken:
                switch (t.Type)
                {
                    case Token.Kind.Error:
                        return t;

                    case Token.Kind.EOS:
                        return output; //Force termination

                    case Token.Kind.Comment:
                        //C comments are replaced with single spaces.
                        if (outputEnabled)
                        {
                            output.Append(" ", 1);
                            output.AppendNL(Line - oldLine);
                        }
                        break;

                    case Token.Kind.Linecomment:
                        //C++ comments are ignored
                        continue;
                    case Token.Kind.Directive:
                        t = HandleDirective(t, oldLine);
                        outputEnabled = EnableOutput;
                        if (outputEnabled != oldOutputEnabled)
                        {
                            if (outputEnabled)
                                output.AppendNL(oldLine - outputDisabledLine);
                            else
                                outputDisabledLine = oldLine;
                            oldOutputEnabled = outputEnabled;
                        }

                        if (outputEnabled)
                            output.AppendNL(Line - oldLine - t.CountNL);
                        goto NextToken;

                    case Token.Kind.Linecont:
                        //Backslash-Newline sequences are delted, no matter where.
                        emptyLines++;
                        break;

                    case Token.Kind.Newline:
                        if (emptyLines > 0)
                        {

                            // Compensate for the backslash-newline combinations
                            // we have encountered, otherwise line numeration is broken
                            if (outputEnabled)
                                output.AppendNL(emptyLines);
                            emptyLines = 0;
                        }
                        goto default;
                    case Token.Kind.Whitespace:
                    default:
                        //Passthrough all other tokens
                        if (outputEnabled)
                            output.Append(t);
                        break;
                }
            }

            if (EnableOutput == false)
            {
                Error(Line, "Unclosed #if at end of source", null);
                return Token.Error;
            }

            return output;
        }
        public void Define(string macroName, int macroNameLength, string macroValue, int macroValueLength)
        {
            Macro m = new Macro(new Token(Token.Kind.Keyword, macroName, macroNameLength));
            m.Value = new Token(Token.Kind.Text, macroValue, macroValueLength);
            m.Next = MacroList;
            MacroList = m;
        }
        public void Define(string macroName, int macroNameLength, long macroValue)
        {
            Macro m = new Macro(new Token(Token.Kind.Keyword, macroName, macroNameLength));
            m.Value.SetValue(macroValue);
            m.Next = MacroList;
            MacroList = m;
        }
        public bool Undef(string macroName, int macroNameLength)
        {
            Macro cur = MacroList;
            Token name = new Token(Token.Kind.Keyword, macroName, macroNameLength);

            while (cur != null)
            {
                if ((cur).Name == name)
                {
                    Macro next = cur.Next;
                    cur.Next = null;
                    cur = null;
                    cur = next;
                    return true;
                }

                cur = cur.Next;
            }

            return false;
        }
        public string Parse(string source, int length, out int oLength)
        {
            oLength = 0;
            Token retVal = Parse(new Token(Token.Kind.Text, source, length));
            if (retVal.Type == Token.Kind.Error)
                return null;

            length = retVal.Length;
            return retVal.String;
        }
        public void Error(int line, string error, Token token)
        {
            if (token != null)
            {
                ErrorHandler(ErrorData, line, error, string.Empty, 0);
            }
            else
            {
                ErrorHandler(ErrorData, line, error, string.Empty, 0);
            }
        }
        static void DefaultError(object data, int line, string error, string token, int tokenLength)
        {
            string message = string.Empty;
            message += "Ln: " + line.ToString();
            message += " " + error;

            if (token != String.Empty)
            {
                message += token;
            }

            Axiom.Core.LogManager.Instance.Write(message);
        }
        /// <summary>
        /// If there's at least one true in the list, return true
        /// </summary>
        public bool EnableOutput
        {
            get
            {
                for (int i = 0; i < outputsEnabled.Length; i++)
                {
                    if (outputsEnabled[i] == true)
                        return true;
                }

                return false;
            }
            set
            {
                for (int i = 0; i < outputsEnabled.Length; i++)
                {
                    if (outputsEnabled[i] != value)
                    {
                        outputsEnabled[i] = value;
                        break;
                    }
                }
            }
        }

    }
}