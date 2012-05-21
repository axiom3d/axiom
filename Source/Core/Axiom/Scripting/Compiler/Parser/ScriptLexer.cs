#region LGPL License

/*
Axiom Graphics Engine Library
Copyright © 2003-2011 Axiom Project Team

The overall design, and a majority of the core engine and rendering code 
contained within this library is a derivative of the open source Object Oriented 
Graphics Engine OGRE, which can be found at http://ogre.sourceforge.net.  
Many thanks to the OGRE team for maintaining such a high quality project.

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
*/

#endregion

#region SVN Version Information

// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Text;

#endregion Namespace Declarations

namespace Axiom.Scripting.Compiler.Parser
{
	public class ScriptLexer
	{
		private enum ScriptState
		{
			Ready,
			Comment,
			MultiComment,
			Word,
			Quote,
			Var,
			PossibleComment
		}

		public ScriptLexer()
		{
		}

		/// <summary>
		/// Tokenizes the given input and returns the list of tokens found
		/// </summary>
		/// <param name="str"></param>
		/// <param name="source"></param>
		/// <returns></returns>
		public IList<ScriptToken> Tokenize( String str, String source )
		{
			const char varOpener = '$',
			           quote = '"',
			           slash = '/',
			           backslash = '\\',
			           openbrace = '{',
			           closebrace = '}',
			           colon = ':',
			           star = '*';
			var c = (char)0;
			var lastChar = (char)0;

			var lexeme = new StringBuilder();
			uint line = 1, lastQuote = 0;
			var state = ScriptState.Ready;

			var tokens = new List<ScriptToken>();

			for ( var index = 0; index < str.Length; index++ )
			{
				lastChar = c;
				c = str[ index ];

				if ( c == quote )
				{
					lastQuote = line;
				}

				switch ( state )
				{
						#region Ready

					case ScriptState.Ready:
						if ( c == slash && lastChar == slash )
						{
							// Comment start, clear out the lexeme
							lexeme = new StringBuilder();
							state = ScriptState.Comment;
						}
						else if ( c == star && lastChar == slash )
						{
							// Comment start, clear out the lexeme
							lexeme = new StringBuilder();
							state = ScriptState.MultiComment;
						}
						else if ( c == quote )
						{
							// Clear out the lexeme ready to be filled with quotes!
							lexeme = new StringBuilder( c.ToString() );
							state = ScriptState.Quote;
						}
						else if ( c == varOpener )
						{
							// Set up to read in a variable
							lexeme = new StringBuilder( c.ToString() );
							state = ScriptState.Var;
						}
						else if ( IsNewline( c ) )
						{
							lexeme = new StringBuilder( c.ToString() );
							SetToken( lexeme, line, source, tokens );
						}
						else if ( !IsWhitespace( c ) )
						{
							lexeme = new StringBuilder( c.ToString() );
							if ( c == slash )
							{
								state = ScriptState.PossibleComment;
							}
							else
							{
								state = ScriptState.Word;
							}
						}
						break;

						#endregion Ready

						#region Comment

					case ScriptState.Comment:
						// This newline happens to be ignored automatically
						if ( IsNewline( c ) )
						{
							state = ScriptState.Ready;
						}
						break;

						#endregion Comment

						#region MultiComment

					case ScriptState.MultiComment:
						if ( c == slash && lastChar == star )
						{
							state = ScriptState.Ready;
						}
						break;

						#endregion MultiComment

						#region PossibleComment

					case ScriptState.PossibleComment:
						if ( c == slash && lastChar == slash )
						{
							lexeme = new StringBuilder();
							state = ScriptState.Comment;
							break;
						}
						else if ( c == star && lastChar == slash )
						{
							lexeme = new StringBuilder();
							state = ScriptState.MultiComment;
							break;
						}
						else
						{
							state = ScriptState.Word;
						}
						break;

						#endregion PossibleComment

						#region Word

					case ScriptState.Word:
						if ( IsNewline( c ) )
						{
							SetToken( lexeme, line, source, tokens );
							lexeme = new StringBuilder( c.ToString() );
							SetToken( lexeme, line, source, tokens );
							state = ScriptState.Ready;
						}
						else if ( IsWhitespace( c ) )
						{
							SetToken( lexeme, line, source, tokens );
							state = ScriptState.Ready;
						}
						else if ( c == openbrace || c == closebrace || c == colon )
						{
							SetToken( lexeme, line, source, tokens );
							lexeme = new StringBuilder( c.ToString() );
							SetToken( lexeme, line, source, tokens );
							state = ScriptState.Ready;
						}
						else
						{
							lexeme.Append( c );
						}
						break;

						#endregion Word

						#region Quote

					case ScriptState.Quote:
						if ( c != backslash )
						{
							// Allow embedded quotes with escaping
							if ( c == quote && lastChar == backslash )
							{
								lexeme.Append( c );
							}
							else if ( c == quote )
							{
								lexeme.Append( c );
								SetToken( lexeme, line, source, tokens );
								state = ScriptState.Ready;
							}
							else
							{
								// Backtrack here and allow a backslash normally within the quote
								if ( lastChar == backslash )
								{
									lexeme.Append( "\\" );
									lexeme.Append( c );
								}
								else
								{
									lexeme.Append( c );
								}
							}
						}
						break;

						#endregion Quote

						#region Var

					case ScriptState.Var:
						if ( IsNewline( c ) )
						{
							SetToken( lexeme, line, source, tokens );
							lexeme = new StringBuilder( c.ToString() );
							SetToken( lexeme, line, source, tokens );
							state = ScriptState.Ready;
						}
						else if ( IsWhitespace( c ) )
						{
							SetToken( lexeme, line, source, tokens );
							state = ScriptState.Ready;
						}
						else if ( c == openbrace || c == closebrace || c == colon )
						{
							SetToken( lexeme, line, source, tokens );
							lexeme = new StringBuilder( c.ToString() );
							SetToken( lexeme, line, source, tokens );
							state = ScriptState.Ready;
						}
						else
						{
							lexeme.Append( c );
						}
						break;

						#endregion Var
				}

				// Separate check for newlines just to track line numbers
				if ( IsNewline( c ) )
				{
					line++;
				}
			}

			// Check for valid exit states
			if ( state == ScriptState.Word || state == ScriptState.Var )
			{
				if ( lexeme.Length != 0 )
				{
					SetToken( lexeme, line, source, tokens );
				}
			}
			else
			{
				if ( state == ScriptState.Quote )
				{
					throw new Exception( String.Format( "no matching \" found for \" at line {0}", lastQuote ) );
				}
			}

			return tokens;
		}

		// Private utility operations

		private void SetToken( StringBuilder lexeme, uint line, String source, List<ScriptToken> tokens )
		{
			const char newline = '\n', openBrace = '{', closeBrace = '}', colon = ':', quote = '\"', var = '$';

			var token = new ScriptToken();
			token.lexeme = lexeme.ToString();
			token.line = line;
			token.file = source;
			var ignore = false;

			// Check the user token map first
			if ( lexeme.Length == 1 && lexeme[ 0 ] == newline )
			{
				token.type = Tokens.Newline;
				if ( tokens.Count != 0 && tokens[ tokens.Count - 1 ].type == Tokens.Newline )
				{
					ignore = true;
				}
			}
			else if ( lexeme.Length == 1 && lexeme[ 0 ] == openBrace )
			{
				token.type = Tokens.LeftBrace;
			}
			else if ( lexeme.Length == 1 && lexeme[ 0 ] == closeBrace )
			{
				token.type = Tokens.RightBrace;
			}
			else if ( lexeme.Length == 1 && lexeme[ 0 ] == colon )
			{
				token.type = Tokens.Colon;
			}
			else if ( lexeme[ 0 ] == var )
			{
				token.type = Tokens.Variable;
			}
			else
			{
				// This is either a non-zero length phrase or quoted phrase
				if ( lexeme.Length >= 2 && lexeme[ 0 ] == quote && lexeme[ lexeme.Length - 1 ] == quote )
				{
					token.type = Tokens.Quote;
				}
				else
				{
					token.type = Tokens.Word;
				}
			}

			if ( !ignore )
			{
				tokens.Add( token );
			}
		}

		private bool IsWhitespace( char c )
		{
			return c == ' ' || c == '\r' || c == '\t';
		}

		private bool IsNewline( char c )
		{
			return c == '\n';
		}
	}
}