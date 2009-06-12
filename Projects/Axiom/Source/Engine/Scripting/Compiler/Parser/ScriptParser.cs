#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2007  Axiom Project Team

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
using System.Text;
using System.Collections.Generic;

#endregion Namespace Declarations
			
namespace Axiom.Scripting.Compiler.Parser
{
	public class ScriptParser
	{
		enum ParserState
		{
			Ready,
			Object
		}

		public ScriptParser()
		{
		}

		public IList<ConcreteNode> Parse( IList<ScriptToken> tokens )
		{

			List<ConcreteNode> nodes = new List<ConcreteNode>();

			ParserState state = ParserState.Ready;
			ScriptToken token;
			ConcreteNode parent = null, node;

			int iter = 0;
			int end = tokens.Count;

			while ( iter != end )
			{
				token = tokens[ iter ];
				switch ( state )
				{
					case ParserState.Ready:
						switch ( token.type )
						{
							case Tokens.Word:
								switch ( token.lexeme )
								{
									case "import":
										node = new ConcreteNode();
										node.token = token.lexeme;
										node.file = token.file;
										node.line = token.line;
										node.type = ConcreteNodeType.Import;

										// The next token is the target
										iter++;
										if ( iter == end || ( tokens[ iter ].type != Tokens.Word && tokens[ iter ].type != Tokens.Quote ) )
											throw new Exception( String.Format( "Expected import target at line {0}", node.line ) );

										ConcreteNode temp = new ConcreteNode();
										temp.parent = node;
										temp.file = tokens[ iter ].file;
										temp.line = tokens[ iter ].line;
										temp.type = tokens[ iter ].type == Tokens.Word ? ConcreteNodeType.Word : ConcreteNodeType.Quote;
										if ( temp.type == ConcreteNodeType.Quote )
											temp.token = tokens[ iter ].lexeme.Substring( 1, token.lexeme.Length - 2 );
										else
											temp.token = tokens[ iter ].lexeme;
										node.children.Add( temp );

										// The second-next token is the source
										iter++; // "from"
										iter++;
										if ( iter == end || ( tokens[ iter ].type != Tokens.Word && tokens[ iter ].type != Tokens.Quote ) )
											throw new Exception( String.Format( "Expected import source at line {0}", node.line ) );

										temp = new ConcreteNode();
										temp.parent = node;
										temp.file = tokens[ iter ].file;
										temp.line = tokens[ iter ].line;
										temp.type = tokens[ iter ].type == Tokens.Word ? ConcreteNodeType.Word : ConcreteNodeType.Quote;
										if ( temp.type == ConcreteNodeType.Quote )
											temp.token = tokens[ iter ].lexeme.Substring( 1, token.lexeme.Length - 2 );
										else
											temp.token = tokens[ iter ].lexeme;
										node.children.Add( temp );

										// Consume all the newlines
										iter = SkipNewlines( tokens, iter, end );

										// Insert the node
										if ( parent != null )
										{
											node.parent = parent;
											parent.children.Add( node );
										}
										else
										{
											node.parent = null;
											nodes.Add( node );
										}
										node = null;
										break;

									case "set":
										break;

									default:
										node = new ConcreteNode();
										node.file = token.file;
										node.line = token.line;
										node.type = token.type == Tokens.Word ? ConcreteNodeType.Word : ConcreteNodeType.Quote;
										if ( node.type == ConcreteNodeType.Quote )
											node.token = token.lexeme.Substring( 1, token.lexeme.Length - 2 );
										else
											node.token = token.lexeme;

										// Insert the node
										if ( parent != null )
										{
											node.parent = parent;
											parent.children.Add( node );
										}
										else
										{
											node.parent = null;
											nodes.Add( node );
										}

										// Set the parent
										parent = node;

										// Switch states
										state = ParserState.Object;

										node = null;

										break;
								}
								break;

							case Tokens.RightBrace:
								// Go up one level if we can
								if ( parent != null )
									parent = parent.parent;

								node = new ConcreteNode();
								node.file = token.file;
								node.line = token.line;
								node.token = token.lexeme;
								node.type = ConcreteNodeType.RightBrace;

								// Consume all the newlines
								iter = SkipNewlines( tokens, iter, end );

								// Insert the node
								if ( parent != null )
								{
									node.parent = parent;
									parent.children.Add( node );
								}
								else
								{
									node.parent = null;
									nodes.Add( node );
								}

								// Move up another level
								if ( parent != null )
									parent = parent.parent;

								node = null;

								break;
						}
						break;
					case ParserState.Object:
						switch ( token.type )
						{
							case Tokens.Newline:
								// Look ahead to the next non-newline token and if it isn't an {, this was a property
								int next = SkipNewlines( tokens, iter, end );
								if ( next == end || ( tokens[ next ].type != Tokens.LeftBrace ) )
								{
									// Ended a property here
									if ( parent != null )
										parent = parent.parent;
									state = ParserState.Ready;
								}
								node = null;
								break;

							case Tokens.Colon:
								node = new ConcreteNode();
								node.file = token.file;
								node.line = token.line;
								node.token = token.lexeme;
								node.type = ConcreteNodeType.Colon;

								// The next token is the parent object
								iter++;
								if ( iter == end || ( tokens[ iter ].type != Tokens.Word && tokens[ iter ].type != Tokens.Quote ) )
									throw new Exception( String.Format( "Expected object identifier at line {0}", node.line ) );

								ConcreteNode temp = new ConcreteNode();
								temp.parent = node;
								temp.file = tokens[ iter ].file;
								temp.line = tokens[ iter ].line;
								temp.type = tokens[ iter ].type == Tokens.Word ? ConcreteNodeType.Word : ConcreteNodeType.Quote;
								if ( temp.type == ConcreteNodeType.Quote )
									temp.token = tokens[ iter ].lexeme.Substring( 1, token.lexeme.Length - 2 );
								else
									temp.token = tokens[ iter ].lexeme;
								node.children.Add( temp );

								// Skip newlines
								iter = SkipNewlines( tokens, iter, end );

								// Insert the node
								if ( parent != null )
								{
									node.parent = parent;
									parent.children.Add( node );
								}
								else
								{
									node.parent = null;
									nodes.Add( node );
								}

								node = null;
								break;

							case Tokens.LeftBrace:
								node = new ConcreteNode();
								node.file = token.file;
								node.line = token.line;
								node.token = token.lexeme;
								node.type = ConcreteNodeType.LeftBrace;

								// Skip newlines
								iter = SkipNewlines( tokens, iter, end );

								// Insert the node
								if ( parent != null )
								{
									node.parent = parent;
									parent.children.Add( node );
								}
								else
								{
									node.parent = null;
									nodes.Add( node );
								}

								// Set the parent
								parent = node;

								// Change the state
								state = ParserState.Ready;

								node = null;
								break;

							case Tokens.RightBrace:

								// Go up one level if we can
								if ( parent != null )
								{
									parent = parent.parent;
								}

								// If the parent is currently a { then go up again
								if ( parent != null && parent.type == ConcreteNodeType.LeftBrace && parent.parent != null )
								{
									parent = parent.parent;
								
								}

								node = new ConcreteNode();
								node.file = token.file;
								node.line = token.line;
								node.token = token.lexeme;
								node.type = ConcreteNodeType.RightBrace;

								// Skip newlines
								iter = SkipNewlines( tokens, iter, end );

								// Insert the node
								if ( parent != null )
								{
									node.parent = parent;
									parent.children.Add( node );
								}
								else
								{
									node.parent = null;
									nodes.Add( node );
								}

								//Move up another level
								if ( parent != null )
									parent = parent.parent;

								node = null;
								state = ParserState.Ready;

								break;

							case Tokens.Variable:
								node = new ConcreteNode();
								node.file = token.file;
								node.line = token.line;
								node.token = token.lexeme;
								node.type = ConcreteNodeType.Variable;

								// Insert the node
								if ( parent != null )
								{
									node.parent = parent;
									parent.children.Add( node );
								}
								else
								{
									node.parent = null;
									nodes.Add( node );
								}

								node = null;
								break;

							case Tokens.Quote:
								node = new ConcreteNode();
								node.file = token.file;
								node.line = token.line;
								node.token = token.lexeme.Substring( 1, token.lexeme.Length - 2 );
								node.type = ConcreteNodeType.Quote;

								// Insert the node
								if ( parent != null )
								{
									node.parent = parent;
									parent.children.Add( node );
								}
								else
								{
									node.parent = null;
									nodes.Add( node );
								}

								node = null;
								break;

							case Tokens.Word:
								node = new ConcreteNode();
								node.file = token.file;
								node.line = token.line;
								node.token = token.lexeme;
								node.type = ConcreteNodeType.Word;

								// Insert the node
								if ( parent != null )
								{
									node.parent = parent;
									parent.children.Add( node );
								}
								else
								{
									node.parent = null;
									nodes.Add( node );
								}

								node = null;
								break;
						}
						break;
				}
				iter++;
			}

			return nodes;
		}

		public List<ConcreteNode> ParseChunk( List<ScriptToken> tokens )
		{
			List<ConcreteNode> nodes = new List<ConcreteNode>();
			ConcreteNode node = null;
			foreach ( ScriptToken token in tokens )
			{

				switch ( token.type )
				{
					case Tokens.Variable:
						node = new ConcreteNode();
						node.token = token.lexeme;
						node.type = ConcreteNodeType.Variable;
						node.file = token.file;
						node.line = token.line;
						node.parent = null;
						break;
					case Tokens.Word:
						node = new ConcreteNode();
						node.token = token.lexeme;
						node.type = ConcreteNodeType.Word;
						node.file = token.file;
						node.line = token.line;
						node.parent = null;
						break;
					case Tokens.Quote:
						node = new ConcreteNode();
						node.token = token.lexeme.Substring( 1, token.lexeme.Length - 2 );
						node.type = ConcreteNodeType.Quote;
						node.file = token.file;
						node.line = token.line;
						node.parent = null;
						break;
					default:
						throw new Exception( String.Format( "unexpected token {0} at line {1}.", token.lexeme, token.line ) );
				}

				if ( node != null )
					nodes.Add( node );
			}
			return nodes;
		}

		private ScriptToken GetToken( IEnumerator<ScriptToken> iter, int offset )
		{
			ScriptToken token = new ScriptToken();
			while ( --offset > 1 && iter.MoveNext() != false )
				;

			if ( iter.MoveNext() != false )
				token = iter.Current;
			return token;
		}

		private int SkipNewlines( IList<ScriptToken> tokens, int iter, int end )
		{
			while ( tokens[ iter ].type == Tokens.Newline && ++iter != end )
				;
			return iter;
		}
	}
}
