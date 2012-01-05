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

using Axiom.Graphics;
using Axiom.Core;

using System.Collections;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.Xna.FixedFunctionEmulation
{
	/// <summary>
	/// 
	/// </summary>
	internal class ShaderManager
	{
		#region Static Interface

		private static int shaderCount = 0;

		#endregion Static Interface

		#region Nested Types

		internal class ShaderGeneratorMap : Dictionary<String, ShaderGenerator> {}

		protected class VertexBufferDeclaration2FixedFunctionProgramsMap : Hashtable /* SortedDictionary<VertexBufferDeclaration, FixedFunctionPrograms> */
		{
			new public FixedFunctionPrograms this[ VertexBufferDeclaration key ]
			{
				get
				{
					if( !ContainsKey( key ) )
					{
						Add( key, null );
					}
					return (FixedFunctionPrograms)base[ key ];
				}
				set
				{
					if( !ContainsKey( key ) )
					{
						Add( key, value );
					}
					else
					{
						base[ key ] = value;
					}
				}
			}
		}

		protected class State2Declaration2ProgramsMap : Hashtable /* SortedDictionary<FixedFunctionState, VertexBufferDeclaration2FixedFunctionProgramsMap>*/
		{
			new public VertexBufferDeclaration2FixedFunctionProgramsMap this[ FixedFunctionState key ]
			{
				get
				{
					if( !ContainsKey( key ) )
					{
						Add( key, new VertexBufferDeclaration2FixedFunctionProgramsMap() );
					}
					return (VertexBufferDeclaration2FixedFunctionProgramsMap)base[ key ];
				}
			}
		}

		protected class Language2State2Declaration2ProgramsMap : Hashtable /* SortedDictionary<String, State2Declaration2ProgramsMap>*/
		{
			new public State2Declaration2ProgramsMap this[ String key ]
			{
				get
				{
					if( !ContainsKey( key ) )
					{
						Add( key, new State2Declaration2ProgramsMap() );
					}
					return (State2Declaration2ProgramsMap)base[ key ];
				}
			}
		}

		#endregion Nested Types

		#region Fields and Properties

		protected ShaderGeneratorMap shaderGeneratorMap = new ShaderGeneratorMap();
		protected Language2State2Declaration2ProgramsMap language2State2Declaration2ProgramsMap = new Language2State2Declaration2ProgramsMap();
		protected List<FixedFunctionPrograms> programsToDeleteAtTheEnd = new List<FixedFunctionPrograms>();

		#endregion Fields and Properties

		#region Construction and Destruction

		public ShaderManager()
		{
			//just delete the previously created shader txt file, will be removed when everything works!
			/*string[] files = System.IO.Directory.GetFiles( System.IO.Directory.GetCurrentDirectory(), "*.txt" );
			foreach ( string str in files )
			{
				if ( str.StartsWith( System.IO.Directory.GetCurrentDirectory() + "\\shader" ) == true )
					System.IO.File.Delete( str );
			}*/
		}

		#endregion Construction and Destruction

		#region Methods

		public void RegisterGenerator( ShaderGenerator generator )
		{
			shaderGeneratorMap.Add( generator.Name, generator );
		}

		public void UnregisterGenerator( ShaderGenerator generator )
		{
			shaderGeneratorMap.Remove( generator.Name );
		}

		public FixedFunctionPrograms GetShaderPrograms( String generatorName, VertexBufferDeclaration vertexBufferDeclaration, FixedFunctionState state )
		{
			/*
			// Search the maps for a matching program
			State2Declaration2ProgramsMap languageMaps = null;
			//language2State2Declaration2ProgramsMap.TryGetValue( generatorName, out languageMaps );
			//if ( language2State2Declaration2ProgramsMap.ContainsKey( generatorName ) )
			languageMaps = language2State2Declaration2ProgramsMap[ generatorName ];
			if ( languageMaps != null )
			{
				VertexBufferDeclaration2FixedFunctionProgramsMap fixedFunctionStateMaps = null;
				//				languageMaps.TryGetValue( state, out fixedFunctionStateMaps );
				//if ( languageMaps.ContainsKey( state ) )
				fixedFunctionStateMaps = languageMaps[ state ];
				if ( fixedFunctionStateMaps != null )
				{
					FixedFunctionPrograms programs = null;
					//fixedFunctionStateMaps.TryGetValue( vertexBufferDeclaration, out programs );
					//if ( fixedFunctionStateMaps.ContainsKey( vertexBufferDeclaration ) )
					programs = fixedFunctionStateMaps[ vertexBufferDeclaration ];
					if ( programs != null )
					{
						return programs;
					}
				}
			}
			*/
			FixedFunctionPrograms programs = language2State2Declaration2ProgramsMap[ generatorName ][ state ][ vertexBufferDeclaration ];
			if( programs != null )
			{
				return programs;
			}
			// If we are here, then one did not exist.
			// Create it.
			return createShaderPrograms( generatorName, vertexBufferDeclaration, state );
		}

		protected FixedFunctionPrograms createShaderPrograms( String generatorName, VertexBufferDeclaration vertexBufferDeclaration, FixedFunctionState state )
		{
			const String vertexProgramName = "VS";
			const String fragmentProgramName = "FP";

			ShaderGenerator shaderGenerator = shaderGeneratorMap[ generatorName ];
			String shaderSource = shaderGenerator.GetShaderSource( vertexProgramName, fragmentProgramName, vertexBufferDeclaration, state );
			if( Root.Instance.RenderSystem.ConfigOptions[ "Save Generated Shaders" ].Value == "Yes" )
			{
				saveShader( state.GetHashCode().ToString(), shaderSource );
			}

			// Vertex program details
			GpuProgramUsage vertexProgramUsage = new GpuProgramUsage( GpuProgramType.Vertex );
			// Fragment program details
			GpuProgramUsage fragmentProgramUsage = new GpuProgramUsage( GpuProgramType.Fragment );

			HighLevelGpuProgram vs;
			HighLevelGpuProgram fs;

			shaderCount++;

			vs = HighLevelGpuProgramManager.Instance.CreateProgram( "VS_" + shaderCount.ToString(),
			                                                        ResourceGroupManager.DefaultResourceGroupName,
			                                                        shaderGenerator.Language,
			                                                        GpuProgramType.Vertex );
			LogManager.Instance.Write( "Created VertexShader {0}", "VS_" + shaderCount.ToString() );
			vs.Source = shaderSource;
			vs.SetParam( "entry_point", vertexProgramName );
			vs.SetParam( "target", shaderGenerator.VPTarget );
			vs.Load();

			vertexProgramUsage.Program = vs;
			//vertexProgramUsage.Params = vs.CreateParameters();

			fs = HighLevelGpuProgramManager.Instance.CreateProgram( "FS_" + shaderCount.ToString(),
			                                                        ResourceGroupManager.DefaultResourceGroupName,
			                                                        shaderGenerator.Language,
			                                                        GpuProgramType.Fragment );
			LogManager.Instance.Write( "Created FragmentProgram {0}", "FS_" + shaderCount.ToString() );
			fs.Source = shaderSource;
			fs.SetParam( "entry_point", fragmentProgramName );
			fs.SetParam( "target", shaderGenerator.FPTarget );
			fs.Load();

			fragmentProgramUsage.Program = fs;
			//fragmentProgramUsage.Params = fs.CreateParameters();

			FixedFunctionPrograms newPrograms = shaderGenerator.CreateFixedFunctionPrograms();
			newPrograms.FixedFunctionState = state;
			newPrograms.FragmentProgramUsage = fragmentProgramUsage;
			newPrograms.VertexProgramUsage = vertexProgramUsage;

			//then save the new program
			language2State2Declaration2ProgramsMap[ generatorName ][ state ][ vertexBufferDeclaration ] = newPrograms;
			programsToDeleteAtTheEnd.Add( newPrograms );

			return newPrograms;
		}

		public void saveShader( string baseFilename, string shaderSource )
		{
			//save the shader just to understand and learn why it bugs :)
			string filename;
			int w = 0;
			do
			{
				filename = baseFilename + Convert.ToString( w ) + ".hlsl";
				w++;
			}
			while( System.IO.File.Exists( filename ) );

			System.IO.StreamWriter sw = new System.IO.StreamWriter( filename );
			sw.Write( shaderSource );
			sw.Flush();
			sw.Close();
		}

		#endregion Methods
	}
}
