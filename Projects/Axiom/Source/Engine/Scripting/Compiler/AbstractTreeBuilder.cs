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
				if ( node.type == ConcreteNodeType.Import && _current == null )
				{
					if ( node.children.Count > 2 )
					{
						_compiler.AddError( CompileErrorCode.FewerParametersExpected, node.file, node.line );
						return;
					}
					if ( node.children.Count < 2 )
					{
						_compiler.AddError( CompileErrorCode.StringExpected, node.file, node.line );
						return;
					}

					ImportAbstractNode impl = new ImportAbstractNode();
					impl.line = node.line;
					impl.file = node.file;
					impl.target = node.children[ 0 ].token;
					impl.source = node.children[ 1 ].token;

					asn = impl;
				}
				// variable set = "set" >> 2 children, children[0] == variable
				else if ( node.type == ConcreteNodeType.VariableAssignment )
				{
					if ( node.children.Count > 2 )
					{
						_compiler.AddError( CompileErrorCode.FewerParametersExpected, node.file, node.line );
						return;
					}
					if ( node.children.Count < 2 )
					{
						_compiler.AddError( CompileErrorCode.StringExpected, node.file, node.line );
						return;
					}
					if ( node.children[ 0 ].type != ConcreteNodeType.Variable )
					{
						_compiler.AddError( CompileErrorCode.VariableExpected, node.children[ 0 ].file, node.children[ 0 ].line );
						return;
					}

					String name = node.children[ 0 ].token;
					String value = node.children[ 1 ].token;

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
				else if ( node.type == ConcreteNodeType.Variable )
				{
					if ( node.children.Count != 0 )
					{
						_compiler.AddError( CompileErrorCode.FewerParametersExpected, node.file, node.line );
						return;
					}

					VariableAccessAbstractNode impl = new VariableAccessAbstractNode( _current );
					impl.line = node.line;
					impl.file = node.file;
					impl.name = node.token;

					asn = impl;
				}
				// Handle properties and objects here
				else if ( node.children.Count != 0 )
				{
					// Grab the last two nodes
					ConcreteNode temp1 = null, temp2 = null;

					if ( node.children.Count >= 1 )
						temp1 = node.children[ node.children.Count - 1 ];
					if ( node.children.Count >= 2 )
						temp2 = node.children[ node.children.Count - 2 ];

					// object = last 2 children == { and }
					if ( temp1 != null && temp2 != null &&
						temp1.type == ConcreteNodeType.RightBrace && temp2.type == ConcreteNodeType.LeftBrace )
					{
						if ( node.children.Count < 2 )
						{
							_compiler.AddError( CompileErrorCode.StringExpected, node.file, node.line );
							return;
						}

						ObjectAbstractNode impl = new ObjectAbstractNode( _current );
						impl.line = node.line;
						impl.file = node.file;
						impl.isAbstract = false;

						// Create a temporary detail list
						List<ConcreteNode> temp = new List<ConcreteNode>();
						if ( node.token == "abstract" )
						{
							impl.isAbstract = true;
						}
						else
						{
							temp.Add( node );
						}
						foreach ( ConcreteNode cn in node.children )
							temp.Add( cn );

						// Get the type of object
						IEnumerator<ConcreteNode> iter = temp.GetEnumerator();
						iter.MoveNext();
						impl.cls = iter.Current.token;
						bool validNode = iter.MoveNext();

						// Get the name
						if ( validNode && ( iter.Current.type == ConcreteNodeType.Word || iter.Current.type == ConcreteNodeType.Quote ) )
						{
							impl.name = iter.Current.token;
							validNode = iter.MoveNext();
						}

						// Everything up until the colon is a "value" of this object
						while ( validNode && iter.Current.type != ConcreteNodeType.Colon && iter.Current.type != ConcreteNodeType.LeftBrace )
						{
							if ( iter.Current.type == ConcreteNodeType.Variable )
							{
								VariableAccessAbstractNode var = new VariableAccessAbstractNode( impl );
								var.file = iter.Current.file;
								var.line = iter.Current.line;
								var.type = AbstractNodeType.VariableGet;
								var.name = iter.Current.token;
								impl.values.Add( var );
							}
							else
							{
								AtomAbstractNode atom = new AtomAbstractNode( impl );
								atom.file = iter.Current.file;
								atom.line = iter.Current.line;
								atom.type = AbstractNodeType.Atom;
								atom.value = iter.Current.token;
								impl.values.Add( atom );
							}
							validNode = iter.MoveNext();
						}

						// Find the base
						if ( validNode && iter.Current.type == ConcreteNodeType.Colon )
						{
							if ( iter.Current.children.Count == 0 )
							{
								_compiler.AddError( CompileErrorCode.StringExpected, iter.Current.file, iter.Current.line );
								return;
							}
							impl.baseClass = iter.Current.children[ 0 ].token;
						}

						// Finally try to map the cls to an id
						if ( _compiler.KeywordMap.ContainsKey( impl.cls ) )
						{
							impl.id = _compiler.KeywordMap[ impl.cls ];
						}

						asn = (AbstractNode)impl;
						_current = impl;

						// Visit the children of the {
						AbstractTreeBuilder.Visit( this, temp2.children );

						// Go back up the stack
						_current = impl.parent;
					}
					// Otherwise, it is a property
					else
					{
						PropertyAbstractNode impl = new PropertyAbstractNode( _current );
						impl.line = node.line;
						impl.file = node.file;
						impl.name = node.token;

						if ( _compiler.KeywordMap.ContainsKey( impl.name ) )
						{
							impl.id = _compiler.KeywordMap[ impl.name ];
						}

						asn = (AbstractNode)impl;
						_current = impl;

						// Visit the children of the {
						AbstractTreeBuilder.Visit( this, node.children );

						// Go back up the stack
						_current = impl.parent;
					}
				}
				// Otherwise, it is a standard atom
				else
				{
					AtomAbstractNode impl = new AtomAbstractNode( _current );
					impl.line = node.line;
					impl.file = node.file;
					impl.value = node.token;

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
