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
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using Axiom.Core;
using Axiom.Math;
using Axiom.Scripting.Compiler.AST;

#endregion Namespace Declarations

namespace Axiom.Scripting.Compiler
{
	public partial class ScriptCompiler
	{
		public abstract class Translator
		{
			/// <summary>
			/// Internal method that checks if this Translator is the right translator for the node
			/// supplied by the <see cref="ScriptTranslatorManager"/>.
			/// </summary>
			/// <param name="nodeId">The Id of the node</param>
			/// <param name="parentId">The Id of the node's parent (if any)</param>
			/// <returns></returns>
			internal abstract bool CheckFor( Keywords nodeId, Keywords parentId );

			/// <summary>
			/// This function translates the given node into Ogre resource(s).
			/// </summary>
			/// <param name="compiler">The compiler invoking this translator</param>
			/// <param name="node">The current AST node to be translated</param>
			public abstract void Translate( ScriptCompiler compiler, AbstractNode node );

			/// <summary>
			/// Retrieves a new translator from the factories and uses it to process the give node
			/// </summary>
			/// <param name="compiler"></param>
			/// <param name="node"></param>
			protected void _processNode( ScriptCompiler compiler, AbstractNode node )
			{
				if ( !(node is ObjectAbstractNode) )
					return;

				var objNode = (ObjectAbstractNode)node;

				// Abstract objects are completely skipped
				if ( objNode != null && objNode.IsAbstract )
					return;

				// Retrieve the translator to use
				var translator = ScriptCompilerManager.Instance.GetTranslator( node );

				if ( translator != null )
					translator.Translate( compiler, node );
				else
				{
					var msg = string.Format( "token {0} is not recognized", objNode.Cls );
					compiler.AddError( CompileErrorCode.UnexpectedToken, node.File, node.Line, msg );
				}
			}

			/// <summary>
			/// Retrieves the node iterator at the given index
			/// </summary>
			/// <param name="nodes"></param>
			/// <param name="index"></param>
			/// <returns></returns>
			protected static AbstractNode getNodeAt( IList<AbstractNode> nodes, int index )
			{
				if ( nodes == null )
					return null;

				if ( index < 0 || index >= nodes.Count )
					return null;

				return nodes[ index ];
			}

			/// <summary>
			/// Converts the node to a boolean and returns true if successful
			/// </summary>
			/// <param name="node"></param>
			/// <param name="result"></param>
			/// <returns>true if successful</returns>
			protected static bool getBoolean( AbstractNode node, out bool result )
			{
				result = false;

				if ( node == null )
					return false;

				if ( !(node is AtomAbstractNode) )
					return false;

				var atom = (AtomAbstractNode)node;
				if ( atom.Id != 1 && atom.Id != 2 )
					return false;

				result = atom.Id == 1 ? true : false;
				return true;
			}

			/// <summary>
			/// Converts the node to a string and returns true if successful
			/// </summary>
			/// <param name="node"></param>
			/// <param name="result"></param>
			/// <returns>true if successful</returns>
			protected static bool getString( AbstractNode node, out String result )
			{
				result = string.Empty;

				if ( node == null )
					return false;

				if ( !(node is AtomAbstractNode) )
					return false;

				var atom = (AtomAbstractNode)node;
				result = atom.Value;
				return true;
			}

			/// <summary>
			/// Converts the node to a Real and returns true if successful
			/// </summary>
			/// <param name="node"></param>
			/// <param name="result"></param>
			/// <returns>true if successful</returns>
			protected static bool getReal( AbstractNode node, out Real result )
			{
				result = 0.0f;

				if ( node == null )
					return false;

				if ( !(node is AtomAbstractNode) )
					return false;

				var atom = (AtomAbstractNode)node;
				if ( !atom.IsNumber )
					return false;

				result = atom.Number;
				return true;
			}

			/// <summary>
			/// Converts the node to a float and returns true if successful
			/// </summary>
			/// <param name="node"></param>
			/// <param name="result"></param>
			/// <returns>true if successful</returns>
			protected static bool getFloat( AbstractNode node, out float result )
			{
				result = 0f;
				Real rResult;

				if ( node == null )
					return false;

				if ( getReal( node, out rResult ) )
				{
					result = rResult;
					return true;
				}

				return false;
			}

			/// <summary>
			/// Converts the node to an int and returns true if successful
			/// </summary>
			/// <param name="node"></param>
			/// <param name="result"></param>
			/// <returns>true if successful</returns>
			protected static bool getInt( AbstractNode node, out int result )
			{
				result = 0;
				Real rResult;

				if ( node == null )
					return false;

				if ( getReal( node, out rResult ) )
				{
					result = (int)rResult;
					return true;
				}

				return false;
			}

			/// <summary>
			/// Converts the node to a uint and returns true if successful
			/// </summary>
			/// <param name="node"></param>
			/// <param name="result"></param>
			/// <returns>true if successful</returns>
			protected static bool getUInt( AbstractNode node, out uint result )
			{
				result = 0;
				Real fResult;

				if ( node == null )
					return false;

				if ( getReal( node, out fResult ) )
				{
					result = (uint)fResult;
					return true;
				}

				return false;
			}

			
			protected static bool getColor( IList<AbstractNode> nodes, int i, out ColorEx result )
			{
				return getColor( nodes, i, out result, 4 );
			}

			/// <summary>
			/// Converts the range of nodes to a ColourValue and returns true if successful
			/// </summary>
			/// <param name="nodes"></param>
			/// <param name="i"></param>
			/// <param name="result"></param>
			/// <param name="maxEntries"></param>
			/// <returns>true if successful</returns>
			protected static bool getColor( IList<AbstractNode> nodes, int i, out ColorEx result, int maxEntries )
			{
				var vals = new Real[ 4 ] { 0, 0, 0, 0 };
				result = ColorEx.White;

				if ( nodes == null )
					return false;

				var n = 0;
				while ( i != nodes.Count && n < maxEntries )
				{
					if ( nodes[ i ] is AtomAbstractNode && ( (AtomAbstractNode)nodes[ i ] ).IsNumber )
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
				// return error if we found less than rgb before end, unless constrained
				return ( n >= 3 || n == maxEntries );
			}

			/// <summary>
			/// Converts the range of nodes to a Matrix4 and returns true if successful
			/// </summary>
			/// <param name="nodes"></param>
			/// <param name="i"></param>
			/// <param name="m"></param>
			/// <returns>true if successful</returns>
			protected static bool getMatrix4( IList<AbstractNode> nodes, int i, out Matrix4 m )
			{
				m = new Matrix4();

				if ( nodes == null )
					return false;

				var n = 0;
				while ( i != nodes.Count && n < 16 )
				{
					if ( i != nodes.Count )
					{
						if ( nodes[ i ] is AtomAbstractNode && ( (AtomAbstractNode)nodes[ i ] ).IsNumber )
							m[ n / 4, n % 4 ] = ( (AtomAbstractNode)nodes[ i ] ).Number;
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

			/// <summary>
			/// Converts the range of nodes to an array of ints and returns true if successful
			/// </summary>
			/// <param name="nodes"></param>
			/// <param name="i"></param>
			/// <param name="vals"></param>
			/// <param name="count"></param>
			/// <returns>true if successful</returns>
			protected static bool getInts( IList<AbstractNode> nodes, int i, out int[] vals, int count )
			{
				var success = true;
				vals = new int[ count ];

				if ( nodes == null )
					return false;

				var n = 0;
				while ( n < count )
				{
					if ( i != nodes.Count )
					{
						if ( nodes[ i ] is AtomAbstractNode && ( (AtomAbstractNode)nodes[ i ] ).IsNumber )
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

			/// <summary>
			/// Converts the range of nodes to an array of floats and returns true if successful
			/// </summary>
			/// <param name="nodes"></param>
			/// <param name="i"></param>
			/// <param name="vals"></param>
			/// <param name="count"></param>
			/// <returns>true if successful</returns>
			protected static bool getFloats( IList<AbstractNode> nodes, int i, out float[] vals, int count )
			{
				var success = true;
				vals = new float[ count ];

				if ( nodes == null )
					return false;

				var n = 0;
				while ( n < count )
				{
					if ( i != nodes.Count )
					{
						if ( nodes[ i ] is AtomAbstractNode && ( (AtomAbstractNode)nodes[ i ] ).IsNumber )
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

			//static bool getConstantType(AbstractNodeList::const_iterator i, GpuConstantType *op); 

			/// <summary>
			/// Converts the node to an enum of type T and returns true if successful
			/// </summary>
			/// <typeparam name="T"></typeparam>
			/// <param name="node"></param>
			/// <param name="compiler"></param>
			/// <param name="property"></param>
			/// <returns>true if successful</returns>
			protected bool getEnumeration<T>( AbstractNode node, ScriptCompiler compiler, out T property )
			{
				// Set default
				property = default( T );

				if ( node == null )
					return false;

				// Verify Parameters
				if ( !(node is AtomAbstractNode) )
					return false;

				var atom = (AtomAbstractNode)node;
				if ( compiler.KeywordMap.ContainsValue( atom.Id ) )
				{
					var keyText = string.Empty;

					// For this ID, find the script Token 
					foreach ( var item in compiler.KeywordMap )
					{
						if ( item.Value == atom.Id )
							keyText = item.Key;
					}

					// Now reflect over the enumeration to find the Token value
					var val = ScriptEnumAttribute.Lookup( keyText, typeof( T ) );
					if ( val != null )
					{
						property = (T)val;
						return true;
					}
				}

				return false;
			}

			public Translator()
			{
			}
		}

	}
}
