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
	internal class GLSLESPreprocessor
	{
		private static int MaxMacroArgs = 16;

		private static double ClosestPow2( int x )
		{
			if ( ( x & ( x - 1 ) ) == 0 )
			{
				return x;
			}
			var max = System.Math.Pow( 2, System.Math.Ceiling( System.Math.Log( x, 2 ) ) );
			var min = max / 2;
			return ( max - x ) > ( x - min ) ? min : max;
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
			private StringBuilder buffer;
			private static Token error = null;

			public string String
			{
				get { return this.buffer.ToString(); }
				set { this.buffer = new StringBuilder( value ); }
			}

			public Token( Kind type )
				: this( type, string.Empty, 0 ) {}

			public Token( Kind type, string str, int length )
			{
				this.Type = type;
				this.String = str;
				this.Length = length;
				this.buffer = new StringBuilder();
			}

			public static Token Error
			{
				get
				{
					if ( error == null )
					{
						error = new Token( Kind.Error );
					}

					return error;
				}
			}

			public static bool operator ==( Token left, Token right )
			{
				return left.Type == right.Type && left.String == right.String && left.Length == right.Length;
			}

			public static bool operator !=( Token left, Token right )
			{
				if ( left.Type != right.Type || left.String != right.String || left.Length != right.Length )
				{
					return false;
				}

				return true;
			}

			public void Append( string str, int length )
			{
				var t = new Token( Kind.Text, str, length );
				this.Append( t );
			}

			public void Append( Token other )
			{
				if ( other.String == string.Empty || other.String == null )
				{
					return;
				}

				if ( this.String == string.Empty || this.String == null )
				{
					this.String = other.String;
					this.Length = other.Length;
					return;
				}

				this.buffer.Append( other.String );
				this.Length += other.Length;
			}

			public void AppendNL( int count )
			{
				string newLines = "\n" + "\n" + "\n" + "\n" + "\n" + "\n" + "\n" + "\n";

				while ( count > 8 )
				{
					this.Append( newLines, 8 );
					count -= 8;
				}
				if ( count > 0 )
				{
					this.Append( newLines, count );
				}
			}

			public bool GetValue( out long value )
			{
				value = 0;

				long val = 0;
				int i = 0;

				while ( Char.IsWhiteSpace( this.String[ i ] ) )
				{
					i++;
				}

				long baseVal = 10;
				if ( this.String[ i ] == '0' )
				{
					if ( ( this.Length > i + 1 ) && this.String[ i + 1 ] == 'x' )
					{
						baseVal = 16;
						i += 2;
					}
					else
					{
						baseVal = 8;
					}
				}

				for ( ; i < this.Length; i++ )
				{
					char c = this.String[ i ];
					if ( Char.IsWhiteSpace( c ) )
					{
						break;
					}

					if ( c >= 'a' && c <= 'z' )
					{
						c -= (char) ( 'a' - 'A' );
					}

					c -= '0';
					if ( c < 0 )
					{
						return false;
					}

					if ( c > 9 )
					{
						c -= (char) ( 'A' - '9' - 1 );
					}
					if ( c >= baseVal )
					{
						return false;
					}

					val = ( val * baseVal ) + c;
				}
				//Check that all other characters are just spaces
				for ( ; i < this.Length; i++ )
				{
					if ( !Char.IsWhiteSpace( this.String[ i ] ) )
					{
						return false;
					}
				}

				value = val;
				return true;
			}

			public void SetValue( bool value )
			{
				this.SetValue( Convert.ToInt64( value ) );
			}

			public void SetValue( long value )
			{
				string temp = value.ToString();
				this.Length = 0;
				this.Append( temp, temp.Length );
				this.Type = Kind.Number;
			}

			public int CountNL
			{
				get
				{
					if ( this.Type == Kind.EOS || this.Type == Kind.Error )
					{
						return 0;
					}

					int newLines = 0;
					for ( int i = 0; i < this.String.Length; i++ )
					{
						if ( this.String[ i ] == '\n' )
						{
							newLines++;
						}
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

			public delegate Token ExpandMethod( GLSLESPreprocessor parent, int numArgs, Token[] args );

			public bool Expanding;

			public Macro( Token name )
			{
				this.Name = name;
				this.NumArgs = 0;
				this.Args = null;
				this.Next = null;
				this.Expanding = false;
			}

			~Macro()
			{
				this.Args = null;
				this.Next = null;
			}

			public ExpandMethod ExpandFunc;

			public Token Expand( int numArgs, Token[] args, Macro macros )
			{
				this.Expanding = true;

				var cpp = new GLSLESPreprocessor();
				cpp.MacroList = macros;
				//Define a new macro for every argument
				int i;
				for ( i = 0; i < numArgs; i++ )
				{
					cpp.Define( this.Args[ i ].String, this.Args[ i ].Length, args[ i ].String, args[ i ].Length );
				}
				//The rest arguments are empty
				for ( ; i < this.NumArgs; i++ )
				{
					cpp.Define( this.Args[ i ].String, this.Args[ i ].Length, string.Empty, 0 );
				}
				//Now run the macro expansion through the supplimentary preprocessor
				Token xt = cpp.Parse( this.Value );

				this.Expanding = false;

				//Remove the extra macros we have defined
				for ( i = this.NumArgs - 1; i >= 0; i-- )
				{
					cpp.Undef( this.Args[ i ].String, this.Args[ i ].Length );
				}

				cpp.MacroList = null;

				return xt;
			}
		}

		private string Source;
		private int SourceEnd { get; set; }

		private int Line;
		private bool BOL;
		private readonly bool[] outputsEnabled = new bool[ 32 ];
		private Macro MacroList;

		public delegate void ErrorHandlerFunc( object data, int line, string error, string token, int tokenLength );

		public static ErrorHandlerFunc ErrorHandler;

		/// <summary>
		/// User-specific storage, passed to Error()
		/// </summary>
		public object ErrorData;

		private GLSLESPreprocessor( Token token, int line )
		{
			ErrorHandler = DefaultError;
			this.Source = token.String;
			this.SourceEnd = token.String.Length + token.Length;
			for ( int i = 0; i < this.outputsEnabled.Length; i++ )
			{
				this.outputsEnabled[ i ] = true;
			}
			this.Line = line;
			this.BOL = true;
		}

		public GLSLESPreprocessor()
		{
			this.MacroList = null;
		}

		~GLSLESPreprocessor()
		{
			this.MacroList = null;
		}

		public Token GetToken( bool expand )
		{
			int index = 0;
			char c = this.Source[ index ];
			if ( c == '\n' || this.Source == System.Environment.NewLine )
			{
				this.Line++;
				this.BOL = true;
				if ( c == '\r' )
				{
					index++;
				}
				return new Token( Token.Kind.Newline, this.Source, this.Source.Length - index );
			}
			else if ( Char.IsWhiteSpace( c ) )
			{
				while ( index < this.Source.Length && this.Source[ index ] != '\r' && this.Source[ index ] != '\n' && Char.IsWhiteSpace( this.Source[ index ] ) )
				{
					index++;
				}

				return new Token( Token.Kind.Whitespace, this.Source, index );
			}
			else if ( Char.IsDigit( c ) )
			{
				this.BOL = false;
				if ( c == '0' && this.Source[ 1 ] == 'x' ) //hex numbers
				{
					index++;
					while ( index < this.Source.Length && char.IsNumber( this.Source[ index ] ) )
					{
						index++;
					}
				}
				else
				{
					while ( index < this.Source.Length && Char.IsDigit( this.Source[ index ] ) )
					{
						index++;
					}
				}

				return new Token( Token.Kind.Number, this.Source, index );
			}
			else if ( c == '_' || Char.IsLetterOrDigit( c ) )
			{
				this.BOL = false;

				while ( index < this.Source.Length && ( this.Source[ index ] == '_' || Char.IsLetterOrDigit( this.Source[ index ] ) ) )
				{
					index++;
				}

				return new Token( Token.Kind.Number, this.Source, index );
			}
			else if ( c == '"' || c == '\'' )
			{
				this.BOL = false;
				while ( index < this.Source.Length && this.Source[ index ] != c )
				{
					if ( this.Source[ index ] == '\\' )
					{
						index++;
						if ( index >= this.Source.Length )
						{
							break;
						}
					}
					if ( this.Source[ index ] == '\n' )
					{
						this.Line++;
					}
					index++;
				}
				if ( index < this.Source.Length )
				{
					index++;
				}
				return new Token( Token.Kind.String, this.Source, index );
			}
			else if ( c == '/' && this.Source[ index ] == '/' )
			{
				this.BOL = false;
				index++;
				while ( index < this.Source.Length && this.Source[ index ] != '\r' && this.Source[ index ] != '\n' )
				{
					return new Token( Token.Kind.Linecomment, this.Source, index );
				}
			}
			else if ( c == '/' && this.Source[ index ] == '*' )
			{
				this.BOL = false;
				index++;

				while ( index < this.Source.Length && ( this.Source[ 0 ] != '*' || this.Source[ 1 ] != '/' ) )
				{
					if ( this.Source[ index ] == '\n' )
					{
						this.Line++;
					}
					index++;
				}
				if ( index < this.Source.Length && this.Source[ index ] == '*' )
				{
					index++;
				}
				if ( index < this.Source.Length && this.Source[ index ] == '/' )
				{
					index++;
				}
				return new Token( Token.Kind.Comment, this.Source, index );
			}
			else if ( c == '#' && this.BOL )
			{
				//Skip all whitespaces after '#'
				while ( index < this.Source.Length && Char.IsWhiteSpace( this.Source[ index ] ) )
				{
					index++;
				}
				while ( index < this.Source.Length && !char.IsWhiteSpace( this.Source[ index ] ) )
				{
					index++;
				}
				return new Token( Token.Kind.Directive, this.Source, index );
			}
			else if ( c == '\\' && index < this.Source.Length && ( this.Source[ index ] == '\r' || this.Source[ index ] == '\n' ) )
			{
				//Treat backslash-newline as a whole token
				if ( this.Source[ index ] == '\r' )
				{
					index++;
				}
				if ( this.Source[ index ] == '\n' )
				{
					index++;
				}
				this.Line++;
				this.BOL = true;
				return new Token( Token.Kind.Linecont, this.Source, index );
			}
			else
			{
				this.BOL = false;
				//Handle double-char operators here

				if ( c == '>' && this.Source[ index ] == '>' || this.Source[ index ] == '=' )
				{
					index++;
				}
				else if ( c == '<' && this.Source[ index ] == '<' || this.Source[ index ] == '=' )
				{
					index++;
				}
				else if ( c == '!' && this.Source[ index ] == '=' )
				{
					index++;
				}
				else if ( c == '=' && this.Source[ index ] == '=' )
				{
					index++;
				}
				else if ( ( c == '|' || c == '&' || c == '^' ) && this.Source[ index ] == c )
				{
					index++;
				}

				return new Token( Token.Kind.Punctuation, this.Source, index );
			}

			return Token.Error;
		}

		public Token HandleDirective( Token token, int line )
		{
			//Analzye preprocessor directive
			char directive = token.String[ 1 ];
			int dirlength = token.Length - 1;
			while ( dirlength > 0 && Char.IsWhiteSpace( directive ) )
			{
				dirlength--;
				directive++;
			}

			int old_line = this.Line;

			//collect the remaining part of the directive until EOL
			Token t, last = null;
			bool done = false;
			do
			{
				t = this.GetToken( false );
				if ( t.Type == Token.Kind.Newline )
				{
					//No directive arguments
					last = t;
					t.Length = 0;
					done = true;
					break;
				}
			} while ( t.Type == Token.Kind.Whitespace || t.Type == Token.Kind.Linecont || t.Type == Token.Kind.Comment || t.Type == Token.Kind.Linecomment );
			if ( !done )
			{
				for ( ;; )
				{
					last = this.GetToken( false );
					switch ( last.Type )
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

						default:
							break;
					}


					if ( done )
					{
						break;
					}
					else
					{
						t.Append( last );
						t.Type = Token.Kind.Text;
					}
				}
			}
			if ( done )
			{
				this.EnableOutput = false;
				bool outputEnabled = this.EnableOutput;
				bool rc;
				if ( outputEnabled )
				{
					string dir = token.String;

					if ( dir.Contains( "define" ) )
					{
						rc = this.HandleDefine( t, line );
					}
					else if ( dir.Contains( "undef" ) )
					{
						rc = this.HandleUnDef( t, line );
					}
					else if ( dir.Contains( "ifdef" ) )
					{
						rc = this.HandleIfDef( t, line );
					}
					else if ( dir.Contains( "ifndef" ) )
					{
						rc = this.HandleIfDef( t, line );
						if ( rc )
						{
							this.EnableOutput = true;
						}
					}
					else if ( dir.Contains( "if" ) )
					{
						rc = this.HandleIf( t, line );
					}
					else if ( dir.Contains( "else" ) )
					{
						rc = this.HandleElse( t, line );
					}
					else if ( dir.Contains( "endif" ) )
					{
						rc = this.HandleEndIf( t, line );
					}
					else
					{
						//Unknown preprocessor directive, roll back and pass through
						this.Line = old_line;
						this.Source = token.String + token.Length;
						token.Type = Token.Kind.Text;
						return token;
					}

					if ( !rc )
					{
						return new Token( Token.Kind.Error );
					}

					return last;
				}
			}

			//To make compiler happy
			return Token.Error;
		}

		public bool HandleDefine( Token body, int line )
		{
			var cpp = new GLSLESPreprocessor( body, line );

			Token t = cpp.GetToken( false );

			if ( t.Type != Token.Kind.Keyword )
			{
				this.Error( line, "Macro name expected after #define", null );
				return false;
			}

			var m = new Macro( t );
			m.Body = body;
			t = cpp.GetArguments( m.NumArgs, m.Args, false );
			while ( t.Type == Token.Kind.Whitespace )
			{
				t = cpp.GetToken( false );
			}

			switch ( t.Type )
			{
				case Token.Kind.Newline:
				case Token.Kind.EOS:
					//Assing "" to token
					t = new Token( Token.Kind.Text, string.Empty, 0 );
					break;

				case Token.Kind.Error:
					m = null;
					return false;

				default:
					t.Type = Token.Kind.Text;
					t.Length = cpp.SourceEnd - t.String.Length;
					break;
			}

			m.Value = t;
			m.Next = this.MacroList;
			this.MacroList = m;
			return true;
		}

		public bool HandleUnDef( Token body, int line )
		{
			var cpp = new GLSLESPreprocessor( body, line );

			Token t = cpp.GetToken( false );

			if ( t.Type != Token.Kind.Keyword )
			{
				this.Error( line, "Expecting a macro name after #undef, got", t );
				return false;
			}

			//Don't barf if macro does not exist = standard C behavior
			this.Undef( t.String, t.Length );

			do
			{
				t = cpp.GetToken( false );
			} while ( t.Type == Token.Kind.Whitespace || t.Type == Token.Kind.Comment || t.Type == Token.Kind.Linecomment );

			if ( t.Type != Token.Kind.EOS )
			{
				this.Error( line, "Warning: Ignoring garbage after directive", t );
			}

			return true;
		}

		public bool HandleIfDef( Token body, int line )
		{
			if ( this.EnableOutput )
			{
				this.Error( line, "Too many embedded #if directives", null );
				return false;
			}

			var cpp = new GLSLESPreprocessor( body, line );

			Token t = cpp.GetToken( false );

			if ( t.Type != Token.Kind.Keyword )
			{
				this.Error( line, "Expecting a macro name after #ifdef, got", t );
				return false;
			}

			this.EnableOutput = false; //has to be set to false 32 times before this matters

			do
			{
				t = cpp.GetToken( false );
			} while ( t.Type == Token.Kind.Whitespace || t.Type == Token.Kind.Comment || t.Type == Token.Kind.Linecomment );

			if ( t.Type != Token.Kind.EOS )
			{
				this.Error( line, "Warning: Ignoring garbage after directive", t );
			}

			return true;
		}

		public bool HandleIf( Token body, int line )
		{
			var defined = new Macro( new Token( Token.Kind.Keyword, "defined", 7 ) );
			defined.Next = this.MacroList;
			defined.ExpandFunc = ExpandDefined;
			defined.NumArgs = 1;

			//Temporary add the defined() function to the macro list
			this.MacroList = defined;

			long val = 0;
			bool rc = this.GetValue( body, out val, line );

			//Restore the macro list
			this.MacroList = defined.Next;
			defined.Next = null;

			if ( !rc )
			{
				return false;
			}

			for ( int i = 0; i < this.outputsEnabled.Length; i++ )
			{
				this.outputsEnabled[ i ] = true;
			}

			return true;
		}

		public bool HandleElse( Token body, int line )
		{
			bool singleFalse = false;

			for ( int i = 0; i < this.outputsEnabled.Length; i++ )
			{
				if ( this.outputsEnabled[ i ] == false )
				{
					singleFalse = true;
					break;
				}
			}
			if ( singleFalse )
			{
				this.Error( line, "#else without #if", null );
				return false;
			}

			//Negate the result of last #if
			this.EnableOutput ^= true;

			if ( body.Length > 0 )
			{
				this.Error( line, "Warning: Ignoring garbage after #else", body );
			}

			return true;
		}

		public bool HandleEndIf( Token body, int line )
		{
			if ( this.EnableOutput == false )
			{
				this.Error( line, "#endif without #if", null );
				return false;
			}
			if ( body.Length > 0 )
			{
				this.Error( line, "Warning: Ignoring garbage after #endif", body );
			}

			return true;
		}

		public Token GetArgument( Token arg, bool expand )
		{
			do
			{
				arg = this.GetToken( expand );
			} while ( arg.Type == Token.Kind.Whitespace || arg.Type == Token.Kind.Newline || arg.Type == Token.Kind.Comment || arg.Type == Token.Kind.Linecomment || arg.Type == Token.Kind.Linecont );

			if ( !expand )
			{
				if ( arg.Type == Token.Kind.EOS )
				{
					return arg;
				}
				else if ( arg.Type == Token.Kind.Punctuation && ( arg.String[ 0 ] == ',' || arg.String[ 0 ] == ')' ) )
				{
					Token t = arg;
					arg = new Token( Token.Kind.Text, string.Empty, 0 );
					return t;
				}
				else if ( arg.Type != Token.Kind.Keyword )
				{
					this.Error( this.Line, "Unexpected token", arg );
					return new Token( Token.Kind.Error );
				}
			}
			var length = arg.Length;
			while ( true )
			{
				Token t = this.GetToken( expand );
				switch ( t.Type )
				{
					case Token.Kind.EOS:
						this.Error( this.Line, "Unfinished list of arguments", null );
						break;
					case Token.Kind.Error:
						return new Token( Token.Kind.Error );
					case Token.Kind.Punctuation:
						if ( t.String[ 0 ] == ',' || t.String[ 0 ] == ')' )
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

				if ( !expand && t.Type != Token.Kind.Whitespace )
				{
					this.Error( this.Line, "Unexpected token", arg );
					return new Token( Token.Kind.Error );
				}

				arg.Append( t );

				if ( t.Type != Token.Kind.Whitespace )
				{
					length = arg.Length;
				}
			}
		}

		public Token GetArguments( int numArgs, Token[] args, bool expand )
		{
			var margs = new Token[ MaxMacroArgs ];
			int nargs = 0;

			//Suppose we'll leave by the wrong path
			numArgs = 0;
			args = null;

			Token t;
			do
			{
				t = this.GetToken( expand );
			} while ( t.Type == Token.Kind.Whitespace || t.Type == Token.Kind.Comment || t.Type == Token.Kind.Linecomment );

			if ( t.Type != Token.Kind.Punctuation || t.String[ 0 ] != '(' )
			{
				numArgs = 0;
				args = null;
				return t;
			}

			bool done = false;
			while ( true )
			{
				if ( nargs == MaxMacroArgs )
				{
					this.Error( this.Line, "Too many arguments to macro", null );
					return new Token( Token.Kind.Error );
				}

				t = this.GetArgument( args[ nargs++ ], expand );

				switch ( t.Type )
				{
					case Token.Kind.EOS:
						this.Error( this.Line, "Unfinished list of arguments", null );
						break;
					case Token.Kind.Error:
						return new Token( Token.Kind.Error );

					case Token.Kind.Punctuation:
						if ( t.String[ 0 ] == ')' )
						{
							t = this.GetToken( expand );
							done = true;
							break;
						}
						break;
					default:
						break;
				}
				if ( done )
				{
					break;
				}
			}

			if ( done )
			{
				numArgs = nargs;
				args = new Token[ nargs ];
				for ( int i = 0; i < nargs; i++ )
				{
					args[ i ] = margs[ i ];
				}

				return t;
			}

			return Token.Error;
		}

		public Token GetExpression( Token result, int line )
		{
			this.GetExpression( out result, line, 0 );

			return result;
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

		public Token GetExpression( out Token result, int line, int opPriority )
		{
			string tmp = string.Empty;
			do
			{
				result = this.GetToken( true );
			} while ( result.Type == Token.Kind.Whitespace || result.Type == Token.Kind.Newline || result.Type == Token.Kind.Comment || result.Type == Token.Kind.Linecomment || result.Type == Token.Kind.Linecont );

			var op = new Token( Token.Kind.Whitespace, string.Empty, 0 );

			//Handle unary operators here
			if ( result.Type == Token.Kind.Punctuation && result.Length == 1 )
			{
				if ( result.String[ 0 ] == '+' || result.String[ 0 ] == '-' || result.String[ 0 ] == '!' || result.String[ 0 ] == '~' )
				{
					char uop = result.String[ 0 ];
					op = this.GetExpression( out result, line, 12 );
					long val;
					if ( !this.GetValue( result, out val, line ) )
					{
						tmp = "Unary " + uop + " not applicable";
						this.Error( line, tmp, result );
						return new Token( Token.Kind.Error );
					}

					if ( uop == '-' )
					{
						result.SetValue( -val );
					}
					else if ( uop == '!' )
					{
						bool bVal = Convert.ToBoolean( val );
						result.SetValue( !bVal );
					}
					else if ( uop == '~' )
					{
						result.SetValue( ~val );
					}
				}
				else if ( result.String[ 0 ] == '(' )
				{
					op = this.GetExpression( out result, line, 1 );
					if ( op.Type == Token.Kind.Error )
					{
						return op;
					}
					if ( op.Type == Token.Kind.EOS )
					{
						this.Error( line, "Unclosed parenthesis in #if expression", null );
						return new Token( Token.Kind.Error );
					}
					op = this.GetToken( true );
				}
			}

			while ( op.Type == Token.Kind.Whitespace || op.Type == Token.Kind.Newline || op.Type == Token.Kind.Comment || op.Type == Token.Kind.Linecomment || op.Type == Token.Kind.Linecont )

			{
				op = this.GetToken( true );
			}

			while ( true )
			{
				if ( op.Type != Token.Kind.Punctuation )
				{
					return op;
				}

				int prio = 0;
				if ( op.Length == 1 )
				{
					switch ( op.String[ 0 ] )
					{
						case ')':
							return op;
						case '|':
							prio = 4;
							break;
						case '^':
							prio = 5;
							break;
						case '&':
							prio = 6;
							break;
						case '<':
						case '>':
							prio = 8;
							break;
						case '+':
						case '-':
							prio = 10;
							break;
						case '*':
						case '/':
						case '%':
							prio = 11;
							break;
					}
				}
				else if ( op.Length == 2 )
				{
					switch ( op.String[ 0 ] )
					{
						case '|':
							if ( op.String[ 1 ] == '|' )
							{
								prio = 2;
							}
							break;
						case '&':
							if ( op.String[ 1 ] == '&' )
							{
								prio = 3;
							}
							break;
						case '=':
							if ( op.String[ 1 ] == '=' )
							{
								prio = 7;
							}
							break;
						case '!':
							if ( op.String[ 1 ] == '=' )
							{
								prio = 7;
							}
							break;
						case '<':
							if ( op.String[ 1 ] == '=' )
							{
								prio = 8;
							}
							else if ( op.String[ 1 ] == '<' )
							{
								prio = 9;
							}
							break;
						case '>':
							if ( op.String[ 1 ] == '=' )
							{
								prio = 8;
							}
							else if ( op.String[ 1 ] == '>' )
							{
								prio = 9;
							}
							break;
					}
				}

				if ( prio == 0 )
				{
					this.Error( line, "Expecting operator, got", op );
					return new Token( Token.Kind.Error );
				}

				if ( opPriority >= prio )
				{
					return op;
				}

				Token rop;
				Token nextop = this.GetExpression( out rop, line, prio );
				long vlop, vrop;
				if ( !this.GetValue( result, out vlop, line ) )
				{
					tmp = "Left operand of " + op.String + " is not a number";
					this.Error( line, tmp, result );
					return new Token( Token.Kind.Error );
				}
				if ( !this.GetValue( result, out vrop, line ) )
				{
					tmp = "right operand of " + op.String + " is not a number";
					this.Error( line, tmp, result );
					return new Token( Token.Kind.Error );
				}
				bool bvlop = Convert.ToBoolean( vlop ), bvrop = Convert.ToBoolean( vrop );

				switch ( op.String[ 0 ] )
				{
					case '|':
						if ( prio == 2 )
						{
							result.SetValue( bvlop || bvrop );
						}
						else
						{
							result.SetValue( vlop | vrop );
						}
						break;
					case '&':
						if ( prio == 3 )
						{
							result.SetValue( bvlop && bvrop );
						}
						else
						{
							result.SetValue( vlop & vrop );
						}
						break;
					case '<':
						if ( op.Length == 1 )
						{
							result.SetValue( vlop < vrop );
						}
						else if ( prio == 8 )
						{
							result.SetValue( vlop <= vrop );
						}
						else if ( prio == 9 )
						{
							result.SetValue( (int) vlop << (int) vrop );
						}
						break;
					case '>':
						if ( op.Length == 1 )
						{
							result.SetValue( vlop > vrop );
						}
						else if ( prio == 8 )
						{
							result.SetValue( vlop >= vrop );
						}
						else if ( prio == 9 )
						{
							result.SetValue( (int) vlop >> (int) vrop );
						}
						break;
					case '^':
						result.SetValue( vlop ^ vrop );
						break;
					case '!':
						result.SetValue( vlop != vrop );
						break;
					case '=':
						result.SetValue( vlop == vrop );
						break;
					case '+':
						result.SetValue( vlop + vrop );
						break;
					case '-':
						result.SetValue( vlop - vrop );
						break;
					case '*':
						result.SetValue( vlop * vrop );
						break;
					case '/':
					case '%':
						if ( vrop == 0 )
						{
							this.Error( line, "Division by zero", null );
							return new Token( Token.Kind.Error );
						}
						if ( op.String[ 0 ] == '/' )
						{
							result.SetValue( vlop / vrop );
						}
						else
						{
							result.SetValue( vlop % vrop );
						}
						break;
				}
				op = nextop;
			}
		}

		public bool GetValue( Token token, out long value, int line )
		{
			value = 0;
			Token r;
			Token vt = token;

			if ( ( vt.Type == Token.Kind.Keyword || vt.Type == Token.Kind.Text || vt.Type == Token.Kind.Number ) && ( vt.String == string.Empty || vt.String == null ) )
			{
				this.Error( line, "Trying to evaluate an empty expression", null );
				return false;
			}

			if ( vt.Type == Token.Kind.Text )
			{
				var cpp = new GLSLESPreprocessor( token, line );
				cpp.MacroList = this.MacroList;

				Token t = cpp.GetExpression( out r, line, 0 );

				cpp.MacroList = null;

				if ( t.Type == Token.Kind.Error )
				{
					return false;
				}

				if ( t.Type != Token.Kind.EOS )
				{
					this.Error( line, "Garbage afer expression", t );
					return false;
				}

				vt = r;
			}

			Macro m;
			switch ( vt.Type )
			{
				case Token.Kind.EOS:
				case Token.Kind.Error:
					return false;

				case Token.Kind.Text:
				case Token.Kind.Number:
					if ( !vt.GetValue( out value ) )
					{
						this.Error( line, "Not a numeric expression", vt );
						return false;
					}
					break;
				case Token.Kind.Keyword:
					//Try to expand the macro
					m = this.IsDefined( vt );
					if ( m != null && !m.Expanding )
					{
						Token x = this.ExpandMacro( vt );
						m.Expanding = true;
						bool rc = this.GetValue( x, out value, line );
						m.Expanding = false;
						return rc;
					}
					//Undefined macro, "expand to 0 mimic cpp behaviour
					value = 0;
					break;
				default:
					this.Error( line, "Unexpected token", vt );
					break;
			}

			return true;
		}

		public Token ExpandMacro( Token token )
		{
			Macro cur = this.IsDefined( token );

			if ( cur != null && !cur.Expanding )
			{
				var args = new Token[ MaxMacroArgs ];
				int nargs = 0;
				int old_line = this.Line;

				if ( cur.NumArgs != 0 )
				{
					Token t = this.GetArguments( nargs, args, ( cur.ExpandFunc != null ) ? false : true );

					if ( t.Type == Token.Kind.Error )
					{
						args = null;
						return t;
					}

					//Put the token back into the source ppol; we'll handle it later
					if ( t.String != null && t.String != string.Empty )
					{
						this.Source = t.String;
						this.Line -= t.CountNL;
					}
				}

				if ( nargs > cur.NumArgs )
				{
					string tmp = string.Format( "Macro {0}'s passed {1} arguments, but takes just {2}", cur.Name.Length, cur.Name.String, nargs );
					this.Error( old_line, tmp, null );
					return new Token( Token.Kind.Error );
				}

				Token tk = ( cur.ExpandFunc != null ) ? cur.ExpandFunc( this, nargs, args ) : cur.Expand( nargs, args, this.MacroList );
				tk.AppendNL( this.Line - old_line );

				args = null;

				return tk;
			}

			return token;
		}

		public Macro IsDefined( Token token )
		{
			for ( Macro cur = this.MacroList; cur != null; cur = cur.Next )
			{
				if ( cur.Name == token )
				{
					return cur;
				}
			}

			return null;
		}

		public static Token ExpandDefined( GLSLESPreprocessor parent, int numArgs, Token[] args )
		{
			if ( numArgs != 1 )
			{
				parent.Error( parent.Line, "The defined() function takes exactly one argument", null );
				return new Token( Token.Kind.Error );
			}

			string v = ( parent.IsDefined( args[ 0 ] ) != null ) ? "1" : "0";
			return new Token( Token.Kind.Number, v, 1 );
		}

		public Token Parse( Token source )
		{
			this.Source = source.String;
			int sourceEned = this.Source.Length;
			this.Line = 1;
			this.BOL = true;
			this.EnableOutput = true;

			//Accumulate ouput into this token
			var output = new Token( Token.Kind.Text );
			int emptyLines = 0;

			//Enabel output only if all embedded #if's were true
			bool oldOutputEnabled = true;
			bool outputEnabled = true;
			int outputDisabledLine = 0;

			for ( int i = 0; i < sourceEned; i++ )
			{
				int oldLine = this.Line;
				Token t = this.GetToken( true );

				NextToken:
				switch ( t.Type )
				{
					case Token.Kind.Error:
						return t;

					case Token.Kind.EOS:
						return output; //Force termination

					case Token.Kind.Comment:
						//C comments are replaced with single spaces.
						if ( outputEnabled )
						{
							output.Append( " ", 1 );
							output.AppendNL( this.Line - oldLine );
						}
						break;

					case Token.Kind.Linecomment:
						//C++ comments are ignored
						continue;
					case Token.Kind.Directive:
						t = this.HandleDirective( t, oldLine );
						outputEnabled = this.EnableOutput;
						if ( outputEnabled != oldOutputEnabled )
						{
							if ( outputEnabled )
							{
								output.AppendNL( oldLine - outputDisabledLine );
							}
							else
							{
								outputDisabledLine = oldLine;
							}
							oldOutputEnabled = outputEnabled;
						}

						if ( outputEnabled )
						{
							output.AppendNL( this.Line - oldLine - t.CountNL );
						}
						goto NextToken;

					case Token.Kind.Linecont:
						//Backslash-Newline sequences are delted, no matter where.
						emptyLines++;
						break;

					case Token.Kind.Newline:
						if ( emptyLines > 0 )
						{
							// Compensate for the backslash-newline combinations
							// we have encountered, otherwise line numeration is broken
							if ( outputEnabled )
							{
								output.AppendNL( emptyLines );
							}
							emptyLines = 0;
						}
						goto default;
					case Token.Kind.Whitespace:
					default:
						//Passthrough all other tokens
						if ( outputEnabled )
						{
							output.Append( t );
						}
						break;
				}
			}

			if ( this.EnableOutput == false )
			{
				this.Error( this.Line, "Unclosed #if at end of source", null );
				return Token.Error;
			}

			return output;
		}

		public void Define( string macroName, int macroNameLength, string macroValue, int macroValueLength )
		{
			var m = new Macro( new Token( Token.Kind.Keyword, macroName, macroNameLength ) );
			m.Value = new Token( Token.Kind.Text, macroValue, macroValueLength );
			m.Next = this.MacroList;
			this.MacroList = m;
		}

		public void Define( string macroName, int macroNameLength, long macroValue )
		{
			var m = new Macro( new Token( Token.Kind.Keyword, macroName, macroNameLength ) );
			m.Value.SetValue( macroValue );
			m.Next = this.MacroList;
			this.MacroList = m;
		}

		public bool Undef( string macroName, int macroNameLength )
		{
			Macro cur = this.MacroList;
			var name = new Token( Token.Kind.Keyword, macroName, macroNameLength );

			while ( cur != null )
			{
				if ( ( cur ).Name == name )
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

		public string Parse( string source, int length, out int oLength )
		{
			oLength = 0;
			Token retVal = this.Parse( new Token( Token.Kind.Text, source, length ) );
			if ( retVal.Type == Token.Kind.Error )
			{
				return null;
			}

			length = retVal.Length;
			return retVal.String;
		}

		public void Error( int line, string error, Token token )
		{
			if ( token != null )
			{
				ErrorHandler( this.ErrorData, line, error, string.Empty, 0 );
			}
			else
			{
				ErrorHandler( this.ErrorData, line, error, string.Empty, 0 );
			}
		}

		private static void DefaultError( object data, int line, string error, string token, int tokenLength )
		{
			string message = string.Empty;
			message += "Ln: " + line.ToString();
			message += " " + error;

			if ( token != String.Empty )
			{
				message += token;
			}

			Axiom.Core.LogManager.Instance.Write( message );
		}

		/// <summary>
		/// If there's at least one true in the list, return true
		/// </summary>
		public bool EnableOutput
		{
			get
			{
				for ( int i = 0; i < this.outputsEnabled.Length; i++ )
				{
					if ( this.outputsEnabled[ i ] == true )
					{
						return true;
					}
				}

				return false;
			}
			set
			{
				for ( int i = 0; i < this.outputsEnabled.Length; i++ )
				{
					if ( this.outputsEnabled[ i ] != value )
					{
						this.outputsEnabled[ i ] = value;
						break;
					}
				}
			}
		}
	}
}
