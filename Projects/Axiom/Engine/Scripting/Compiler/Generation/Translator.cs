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

using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;

using Axiom.Scripting.Compiler.AST;

using Real = System.Single;

#endregion Namespace Declarations

namespace Axiom.Scripting.Compiler
{
	public partial class ScriptCompiler
	{
		public abstract class Translator
		{
			/// This static translation function requests a translation on the given node
			static public void Translate( Translator translator, AbstractNode node )
			{
				// If it an abstract object it is completely skipped
				if ( node.type == AbstractNodeType.Object && ( (ObjectAbstractNode)node ).isAbstract )
					return;

				// First check if the compiler listener will override this node
				bool process = true;

				if ( translator != null && translator._compiler != null && translator._compiler.Listener != null )
				{
					Translator p = null;
					if ( node.type == AbstractNodeType.Object )
						p = translator._compiler.Listener.PreObjectTranslation( (ObjectAbstractNode)node );
					else if ( node.type == AbstractNodeType.Property )
						p = translator._compiler.Listener.PrePropertyTranslation( (PropertyAbstractNode)node );
					if ( p != null )
					{
						// Call the returned translator
						if ( node.type == AbstractNodeType.Object )
							p.ProcessObject( (ObjectAbstractNode)node );
						else if ( node.type == AbstractNodeType.Property )
							p.ProcessProperty( (PropertyAbstractNode)node );
						process = false;
					}
				}

				// Call the suggested translator
				// Or ignore the node if no translator is given
				if ( process && translator != null )
				{
					if ( node.type == AbstractNodeType.Object )
						translator.ProcessObject( (ObjectAbstractNode)node );
					else if ( node.type == AbstractNodeType.Property )
						translator.ProcessProperty( (PropertyAbstractNode)node );
				}
			}

			private ScriptCompiler _compiler;

			protected ScriptCompiler Compiler
			{
				get
				{
					return _compiler;
				}
			}


			protected ScriptCompilerListener CompilerListener
			{
				get
				{
					return _compiler.Listener;
				}
			}

			protected AbstractNode getNodeAt( IList<AbstractNode> nodes, int index )
			{
				return nodes[ index ];
			}

			protected bool getBoolean( AbstractNode node, out bool result )
			{
				result = false;

				if ( node.type != AbstractNodeType.Atom )
				{
					_compiler.AddError( CompileErrorCode.InvalidParameters, node.file, node.line );
					return false;
				}

				AtomAbstractNode atom = (AtomAbstractNode)node;
				if ( atom.id != 1 && atom.id != 0 )
				{
					_compiler.AddError( CompileErrorCode.InvalidParameters, node.file, node.line );
					return false;
				}

				result = atom.id == 1 ? true : false;
				return true;
			}

			protected bool getString( AbstractNode node, out String result )
			{
				result = "";

				if ( node.type != AbstractNodeType.Atom )
				{
					_compiler.AddError( CompileErrorCode.InvalidParameters, node.file, node.line );
					return false;
				}

				AtomAbstractNode atom = (AtomAbstractNode)node;
				result = atom.value;
				return true;

			}

			protected bool getNumber( AbstractNode node, out Real result )
			{
				result = 0.0f;

				if ( node.type != AbstractNodeType.Atom )
				{
					_compiler.AddError( CompileErrorCode.InvalidParameters, node.file, node.line );
					return false;
				}

				AtomAbstractNode atom = (AtomAbstractNode)node;
				if ( !atom.IsNumber )
				{
					_compiler.AddError( CompileErrorCode.InvalidParameters, node.file, node.line );
					return false;
				}
				result = atom.Number;
				return true;
			}

			protected bool getColor( IList<AbstractNode> nodes, int i, out ColorEx result )
			{
				Real[] vals = new Real[ 4 ] { 0, 0, 0, 0 };
				result = ColorEx.White;

				int n = 0;
				while ( i != nodes.Count && n < 4 )
				{
					if ( nodes[ i ].type == AbstractNodeType.Atom && ( (AtomAbstractNode)nodes[ i ] ).IsNumber )
					{
						vals[ n ] = ( (AtomAbstractNode)nodes[ i ] ).Number;
					}
					else
					{
						return false;
					}
					++n;
					++i;
				}

				result = new ColorEx( vals[ 0 ], vals[ 1 ], vals[ 2 ], vals[ 3 ] );
				return true;
			}

			protected bool getMatrix4( IList<AbstractNode> nodes, int i, out Matrix4 m )
			{
				m = new Matrix4();

				int n = 0;
				while ( i != nodes.Count && n < 16 )
				{
					if ( i != nodes.Count )
					{
						if ( nodes[ i ].type == AbstractNodeType.Atom && ( (AtomAbstractNode)nodes[ i ] ).IsNumber )
							m[ n % 4, n / 4 ] = ( (AtomAbstractNode)nodes[ i ] ).Number;
						else
							return false;
					}
					else
					{
						return false;
					}
				}
				return true;
			}

			protected bool getInts( IList<AbstractNode> nodes, int i, out int[] vals, int count )
			{
				bool success = true;
				vals = new int[ count ];
				int n = 0;
				while ( n < count )
				{
					if ( i != nodes.Count )
					{
						if ( nodes[ i ].type == AbstractNodeType.Atom && ( (AtomAbstractNode)nodes[ i ] ).IsNumber )
							vals[ n ] = (int)( (AtomAbstractNode)nodes[ i ] ).Number;
						else
							break;
						++i;
					}
					else
						vals[ n ] = 0;
					++n;
				}

				if ( n < count )
					success = false;

				return success;
			}

			protected bool getFloats( IList<AbstractNode> nodes, int i, out float[] vals, int count )
			{
				bool success = true;
				vals = new float[ count ];

				int n = 0;
				while ( n < count )
				{
					if ( i != nodes.Count )
					{
						if ( nodes[ i ].type == AbstractNodeType.Atom && ( (AtomAbstractNode)nodes[ i ] ).IsNumber )
							vals[ n ] = ( (AtomAbstractNode)nodes[ i ] ).Number;
						else
							break;
						++i;
					}
					else
						vals[ n ] = 0.0f;
					++n;
				}

				if ( n < count )
					success = false;

				return success;
			}

			protected bool translateEnumeration<T>( AbstractNode node, out T property ) 
			{
				// Set default
				property = default(T);

				// Verify Parameters
				if ( node.type != AbstractNodeType.Atom )
				{
					_compiler.AddError( CompileErrorCode.InvalidParameters, node.file, node.line );
					return false;
				}

				AtomAbstractNode atom = (AtomAbstractNode)node;
				if ( _compiler.KeywordMap.ContainsValue( atom.id ) )
				{
					String keyText = "";

					// For this ID, find the script Token 
					foreach ( KeyValuePair<string, uint> item in _compiler.KeywordMap )
					{
						if ( item.Value == atom.id )
							keyText = item.Key;
					}

					// Now reflect over the enumeration to find the Token value
					object val = ScriptEnumAttribute.Lookup( keyText, typeof( T ) );
					if ( val != null )
					{
						property = (T)val;
						return true;
					}
				}
				_compiler.AddError( CompileErrorCode.InvalidParameters, atom.file, atom.line );
				return false;

			}

			public Translator( ScriptCompiler compiler )
			{
				_compiler = compiler;
			}

			/// This function is called to process each object node
			protected abstract void ProcessObject( ObjectAbstractNode obj );
			/// This function is called to process each property node
			protected abstract void ProcessProperty( PropertyAbstractNode prop );
		}

	}
}
