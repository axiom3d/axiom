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
using System.Collections.Generic;
using System.Text;

using Axiom.Scripting.Compiler.AST;

#endregion Namespace Declarations

namespace Axiom.Scripting.Compiler
{
	public partial class ScriptCompiler
	{
		private class AbstractTreeBuilder
		{
			private ScriptCompiler _compiler;
			private AbstractNode _current;
			private List<AbstractNode> _nodes;

			public AbstractTreeBuilder( ScriptCompiler compiler )
			{
				_compiler = compiler;
				_current = null;
				_nodes = new List<AbstractNode>();
			}

			public IList<AbstractNode> Result
			{
				get
				{
					return _nodes;
				}
			}

			private void visit( ConcreteNode node )
			{
				AbstractNode asn = null;

				// Import = "import" >> 2 children, _current == null
				if ( node.Type == ConcreteNodeType.Import && _current == null )
				{
					if ( node.Children.Count > 2 )
					{
						_compiler.AddError( CompileErrorCode.FewerParametersExpected, node.File, node.Line );
						return;
					}
					if ( node.Children.Count < 2 )
					{
						_compiler.AddError( CompileErrorCode.StringExpected, node.File, node.Line );
						return;
					}

					ImportAbstractNode impl = new ImportAbstractNode();
					impl.line = node.Line;
					impl.file = node.File;
					impl.target = node.Children[ 0 ].Token;
					impl.source = node.Children[ 1 ].Token;

					asn = impl;
				}
				// variable set = "set" >> 2 children, children[0] == variable
				else if ( node.Type == ConcreteNodeType.VariableAssignment )
				{
					if ( node.Children.Count > 2 )
					{
						_compiler.AddError( CompileErrorCode.FewerParametersExpected, node.File, node.Line );
						return;
					}
					if ( node.Children.Count < 2 )
					{
						_compiler.AddError( CompileErrorCode.StringExpected, node.File, node.Line );
						return;
					}
					if ( node.Children[ 0 ].Type != ConcreteNodeType.Variable )
					{
						_compiler.AddError( CompileErrorCode.VariableExpected, node.Children[ 0 ].File, node.Children[ 0 ].Line );
						return;
					}

					String name = node.Children[ 0 ].Token;
					String value = node.Children[ 1 ].Token;

					if ( _current != null && _current.type == AbstractNodeType.Object )
					{
						ObjectAbstractNode ptr = (ObjectAbstractNode)_current;
						ptr.SetVariable( name, value );
					}
					else
					{
						_compiler.Environment.Add( name, value );
					}
				}
				// variable = $*, no children
				else if ( node.Type == ConcreteNodeType.Variable )
				{
					if ( node.Children.Count != 0 )
					{
						_compiler.AddError( CompileErrorCode.FewerParametersExpected, node.File, node.Line );
						return;
					}

					VariableAccessAbstractNode impl = new VariableAccessAbstractNode( _current );
					impl.line = node.Line;
					impl.file = node.File;
					impl.name = node.Token;

					asn = impl;
				}
				// Handle properties and objects here
				else if ( node.Children.Count != 0 )
				{
					// Grab the last two nodes
					ConcreteNode temp1 = null, temp2 = null;

					if ( node.Children.Count >= 1 )
						temp1 = node.Children[ node.Children.Count - 1 ];
					if ( node.Children.Count >= 2 )
						temp2 = node.Children[ node.Children.Count - 2 ];

					// object = last 2 children == { and }
					if ( temp1 != null && temp2 != null &&
						temp1.Type == ConcreteNodeType.RightBrace && temp2.Type == ConcreteNodeType.LeftBrace )
					{
						if ( node.Children.Count < 2 )
						{
							_compiler.AddError( CompileErrorCode.StringExpected, node.File, node.Line );
							return;
						}

						ObjectAbstractNode impl = new ObjectAbstractNode( _current );
						impl.line = node.Line;
						impl.file = node.File;
						impl.isAbstract = false;

						// Create a temporary detail list
						List<ConcreteNode> temp = new List<ConcreteNode>();
						if ( node.Token == "abstract" )
						{
							impl.isAbstract = true;
						}
						else
						{
							temp.Add( node );
						}
						foreach ( ConcreteNode cn in node.Children )
							temp.Add( cn );

						// Get the type of object
						IEnumerator<ConcreteNode> iter = temp.GetEnumerator();
						iter.MoveNext();
						impl.cls = iter.Current.Token;
						bool validNode = iter.MoveNext();

						// Get the name
						if ( validNode && ( iter.Current.Type == ConcreteNodeType.Word || iter.Current.Type == ConcreteNodeType.Quote ) )
						{
							impl.name = iter.Current.Token;
							validNode = iter.MoveNext();
						}

						// Everything up until the colon is a "value" of this object
						while ( validNode && iter.Current.Type != ConcreteNodeType.Colon && iter.Current.Type != ConcreteNodeType.LeftBrace )
						{
							if ( iter.Current.Type == ConcreteNodeType.Variable )
							{
								VariableAccessAbstractNode var = new VariableAccessAbstractNode( impl );
								var.file = iter.Current.File;
								var.line = iter.Current.Line;
								var.type = AbstractNodeType.VariableGet;
								var.name = iter.Current.Token;
								impl.values.Add( var );
							}
							else
							{
								AtomAbstractNode atom = new AtomAbstractNode( impl );
								atom.file = iter.Current.File;
								atom.line = iter.Current.Line;
								atom.type = AbstractNodeType.Atom;
								atom.value = iter.Current.Token;
								impl.values.Add( atom );
							}
							validNode = iter.MoveNext();
						}

						// Find the base
						if ( validNode && iter.Current.Type == ConcreteNodeType.Colon )
						{
							if ( iter.Current.Children.Count == 0 )
							{
								_compiler.AddError( CompileErrorCode.StringExpected, iter.Current.File, iter.Current.Line );
								return;
							}
							impl.baseClass = iter.Current.Children[ 0 ].Token;
						}

						// Finally try to map the cls to an id
						if ( _compiler.KeywordMap.ContainsKey( impl.cls ) )
						{
							impl.id = _compiler.KeywordMap[ impl.cls ];
						}

						asn = (AbstractNode)impl;
						_current = impl;

						// Visit the children of the {
						AbstractTreeBuilder.Visit( this, temp2.Children );

						// Go back up the stack
						_current = impl.parent;
					}
					// Otherwise, it is a property
					else
					{
						PropertyAbstractNode impl = new PropertyAbstractNode( _current );
						impl.line = node.Line;
						impl.file = node.File;
						impl.name = node.Token;

						if ( _compiler.KeywordMap.ContainsKey( impl.name ) )
						{
							impl.id = _compiler.KeywordMap[ impl.name ];
						}

						asn = (AbstractNode)impl;
						_current = impl;

						// Visit the children of the {
						AbstractTreeBuilder.Visit( this, node.Children );

						// Go back up the stack
						_current = impl.parent;
					}
				}
				// Otherwise, it is a standard atom
				else
				{
					AtomAbstractNode impl = new AtomAbstractNode( _current );
					impl.line = node.Line;
					impl.file = node.File;
					impl.value = node.Token;

					if ( _compiler.KeywordMap.ContainsKey( impl.value ) )
					{
						impl.id = _compiler.KeywordMap[ impl.value ];
					}

					asn = impl;
				}

				if ( asn != null )
				{
					if ( _current != null )
					{
						if ( _current.type == AbstractNodeType.Property )
						{
							PropertyAbstractNode impl = (PropertyAbstractNode)_current;
							impl.values.Add( asn );
						}
						else
						{
							ObjectAbstractNode impl = (ObjectAbstractNode)_current;
							impl.children.Add( asn );
						}
					}
					else
					{
						_nodes.Add( asn );
					}
				}
			}

			static public void Visit( AbstractTreeBuilder visitor, IList<ConcreteNode> nodes )
			{
				foreach ( ConcreteNode node in nodes )
					visitor.visit( node );
			}

		}

	}
}
