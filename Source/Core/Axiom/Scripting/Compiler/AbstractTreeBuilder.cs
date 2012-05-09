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
using Axiom.Scripting.Compiler.AST;

#endregion Namespace Declarations

namespace Axiom.Scripting.Compiler
{
	public partial class ScriptCompiler
	{
		private class AbstractTreeBuilder
		{
			private readonly ScriptCompiler _compiler;
			private AbstractNode _current;
			private readonly List<AbstractNode> _nodes;

			public AbstractTreeBuilder( ScriptCompiler compiler )
			{
				this._compiler = compiler;
				this._current = null;
				this._nodes = new List<AbstractNode>();
			}

			public IList<AbstractNode> Result
			{
				get
				{
					return this._nodes;
				}
			}

			private void visit( ConcreteNode node )
			{
				AbstractNode asn = null;

				// Import = "import" >> 2 children, _current == null
				if ( node.Type == ConcreteNodeType.Import && this._current == null )
				{
					if ( node.Children.Count > 2 )
					{
						this._compiler.AddError( CompileErrorCode.FewerParametersExpected, node.File, node.Line );
						return;
					}
					if ( node.Children.Count < 2 )
					{
						this._compiler.AddError( CompileErrorCode.StringExpected, node.File, node.Line );
						return;
					}

					var impl = new ImportAbstractNode();
					impl.Line = node.Line;
					impl.File = node.File;
					impl.Target = node.Children[ 0 ].Token;
					impl.Source = node.Children[ 1 ].Token;

					asn = impl;
				}
					// variable set = "set" >> 2 children, children[0] == variable
				else if ( node.Type == ConcreteNodeType.VariableAssignment )
				{
					if ( node.Children.Count > 2 )
					{
						this._compiler.AddError( CompileErrorCode.FewerParametersExpected, node.File, node.Line );
						return;
					}
					if ( node.Children.Count < 2 )
					{
						this._compiler.AddError( CompileErrorCode.StringExpected, node.File, node.Line );
						return;
					}
					if ( node.Children[ 0 ].Type != ConcreteNodeType.Variable )
					{
						this._compiler.AddError( CompileErrorCode.VariableExpected, node.Children[ 0 ].File, node.Children[ 0 ].Line );
						return;
					}

					var name = node.Children[ 0 ].Token;
					var value = node.Children[ 1 ].Token;

					if ( this._current != null && this._current is ObjectAbstractNode )
					{
						var ptr = (ObjectAbstractNode)this._current;
						ptr.SetVariable( name, value );
					}
					else
					{
						this._compiler.Environment.Add( name, value );
					}
				}
					// variable = $*, no children
				else if ( node.Type == ConcreteNodeType.Variable )
				{
					if ( node.Children.Count != 0 )
					{
						this._compiler.AddError( CompileErrorCode.FewerParametersExpected, node.File, node.Line );
						return;
					}

					var impl = new VariableGetAbstractNode( this._current );
					impl.Line = node.Line;
					impl.File = node.File;
					impl.Name = node.Token;

					asn = impl;
				}
					// Handle properties and objects here
				else if ( node.Children.Count != 0 )
				{
					// Grab the last two nodes
					ConcreteNode temp1 = null, temp2 = null;

					if ( node.Children.Count >= 1 )
					{
						temp1 = node.Children[ node.Children.Count - 1 ];
					}
					if ( node.Children.Count >= 2 )
					{
						temp2 = node.Children[ node.Children.Count - 2 ];
					}

					// object = last 2 children == { and }
					if ( temp1 != null && temp2 != null && temp1.Type == ConcreteNodeType.RightBrace &&
					     temp2.Type == ConcreteNodeType.LeftBrace )
					{
						if ( node.Children.Count < 2 )
						{
							this._compiler.AddError( CompileErrorCode.StringExpected, node.File, node.Line );
							return;
						}

						var impl = new ObjectAbstractNode( this._current );
						impl.Line = node.Line;
						impl.File = node.File;
						impl.IsAbstract = false;

						// Create a temporary detail list
						var temp = new List<ConcreteNode>();
						if ( node.Token == "abstract" )
						{
							impl.IsAbstract = true;
						}
						else
						{
							temp.Add( node );
						}
						temp.AddRange( node.Children );

						// Get the type of object
						IEnumerator<ConcreteNode> iter = temp.GetEnumerator();
						iter.MoveNext();
						impl.Cls = iter.Current.Token;
						var validNode = iter.MoveNext();

						// Get the name
						// Unless the type is in the exclusion list
						if ( validNode && ( iter.Current.Type == ConcreteNodeType.Word || iter.Current.Type == ConcreteNodeType.Quote ) &&
						     !this._compiler._isNameExcluded( impl.Cls, this._current ) )
						{
							impl.Name = iter.Current.Token;
							validNode = iter.MoveNext();
						}

						// Everything up until the colon is a "value" of this object
						while ( validNode && iter.Current.Type != ConcreteNodeType.Colon &&
						        iter.Current.Type != ConcreteNodeType.LeftBrace )
						{
							if ( iter.Current.Type == ConcreteNodeType.Variable )
							{
								var var = new VariableGetAbstractNode( impl );
								var.File = iter.Current.File;
								var.Line = iter.Current.Line;
								var.Name = iter.Current.Token;
								impl.Values.Add( var );
							}
							else
							{
								var atom = new AtomAbstractNode( impl );
								atom.File = iter.Current.File;
								atom.Line = iter.Current.Line;
								atom.Value = iter.Current.Token;
								impl.Values.Add( atom );
							}
							validNode = iter.MoveNext();
						}

						// Find the base
						if ( validNode && iter.Current.Type == ConcreteNodeType.Colon )
						{
							// Children of the ':' are bases
							foreach ( var j in iter.Current.Children )
							{
								impl.Bases.Add( j.Token );
							}

							validNode = iter.MoveNext();
						}

						// Finally try to map the cls to an id
						if ( this._compiler.KeywordMap.ContainsKey( impl.Cls ) )
						{
							impl.Id = this._compiler.KeywordMap[ impl.Cls ];
						}

						asn = (AbstractNode)impl;
						this._current = impl;

						// Visit the children of the {
						AbstractTreeBuilder.Visit( this, temp2.Children );

						// Go back up the stack
						this._current = impl.Parent;
					}
						// Otherwise, it is a property
					else
					{
						var impl = new PropertyAbstractNode( this._current );
						impl.Line = node.Line;
						impl.File = node.File;
						impl.Name = node.Token;

						if ( this._compiler.KeywordMap.ContainsKey( impl.Name ) )
						{
							impl.Id = this._compiler.KeywordMap[ impl.Name ];
						}

						asn = (AbstractNode)impl;
						this._current = impl;

						// Visit the children of the {
						AbstractTreeBuilder.Visit( this, node.Children );

						// Go back up the stack
						this._current = impl.Parent;
					}
				}
					// Otherwise, it is a standard atom
				else
				{
					var impl = new AtomAbstractNode( this._current );
					impl.Line = node.Line;
					impl.File = node.File;
					impl.Value = node.Token;

					if ( this._compiler.KeywordMap.ContainsKey( impl.Value ) )
					{
						impl.Id = this._compiler.KeywordMap[ impl.Value ];
					}

					asn = impl;
				}

				if ( asn != null )
				{
					if ( this._current != null )
					{
						if ( this._current is PropertyAbstractNode )
						{
							var impl = (PropertyAbstractNode)this._current;
							impl.Values.Add( asn );
						}
						else
						{
							var impl = (ObjectAbstractNode)this._current;
							impl.Children.Add( asn );
						}
					}
					else
					{
						this._nodes.Add( asn );
					}
				}
			}

			public static void Visit( AbstractTreeBuilder visitor, IList<ConcreteNode> nodes )
			{
				foreach ( var node in nodes )
				{
					visitor.visit( node );
				}
			}
		}
	}
}