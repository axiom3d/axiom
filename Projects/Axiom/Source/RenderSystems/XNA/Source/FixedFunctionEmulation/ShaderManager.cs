#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006 Axiom Project Team

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
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;

using Axiom.Graphics;
using Axiom.Core;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.Xna.FixedFunctionEmulation
{
	/// <summary>
	/// 
	/// </summary>
	class ShaderManager
	{
		#region Static Interface

		private static int shaderCount = 0;

		#endregion Static Interface

		#region Nested Types

		internal class ShaderGeneratorMap : Dictionary<String, ShaderGenerator> {}
		protected class VertexBufferDeclaration2FixedFunctionProgramsMap : Dictionary<VertexBufferDeclaration, FixedFunctionPrograms> {}
		protected class State2Declaration2ProgramsMap : Dictionary<FixedFunctionState, VertexBufferDeclaration2FixedFunctionProgramsMap> {}
		protected class Language2State2Declaration2ProgramsMap : Dictionary<String, State2Declaration2ProgramsMap> {}

		#endregion Nested Types

		#region Fields and Properties

		protected ShaderGeneratorMap shaderGeneratorMap = new ShaderGeneratorMap();
		protected Language2State2Declaration2ProgramsMap language2State2Declaration2ProgramsMap=new Language2State2Declaration2ProgramsMap();
		protected List<FixedFunctionPrograms> programsToDeleteAtTheEnd=new List<FixedFunctionPrograms>();

		#endregion Fields and Properties

		#region Construction and Destruction

		public ShaderManager()
		{
            //just delete the previously created shader txt file, will be removed when everything works!
            string[] files = System.IO.Directory.GetFiles(System.IO.Directory.GetCurrentDirectory(),"*.txt");
            foreach (string str in files)
            {
                if (str.StartsWith(System.IO.Directory.GetCurrentDirectory()+"\\shader") == true)
                    System.IO.File.Delete(str);
            }

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
			// Search the maps for a matching program
			State2Declaration2ProgramsMap languageMaps;
			language2State2Declaration2ProgramsMap.TryGetValue( generatorName, out languageMaps );
			if ( languageMaps != null )
			{
                VertexBufferDeclaration2FixedFunctionProgramsMap fixedFunctionStateMaps;
                languageMaps.TryGetValue( state, out fixedFunctionStateMaps );
                if ( fixedFunctionStateMaps != null )
                {
                    FixedFunctionPrograms programs;
                    fixedFunctionStateMaps.TryGetValue( vertexBufferDeclaration, out programs );
					if ( programs != null )
					{
						return programs;
					}
                }
			}

			// If we are here, then one did not exist.
			// Create it.
			return createShaderPrograms( generatorName, vertexBufferDeclaration, state );
		}

		protected FixedFunctionPrograms createShaderPrograms( String generatorName, VertexBufferDeclaration vertexBufferDeclaration, FixedFunctionState state )
		{
			const String vertexProgramName = "VS";
			const String fragmentProgramName = "FP";

			ShaderGenerator shaderGenerator = shaderGeneratorMap[generatorName];
			String shaderSource = shaderGenerator.GetShaderSource( vertexProgramName, fragmentProgramName, vertexBufferDeclaration,	state );

		
			// Vertex program details
			GpuProgramUsage vertexProgramUsage = new GpuProgramUsage(GpuProgramType.Vertex);
			// Fragment program details
			GpuProgramUsage fragmentProgramUsage = new GpuProgramUsage(GpuProgramType.Fragment);


			HighLevelGpuProgram vs;
			HighLevelGpuProgram fs;

			shaderCount++;

			vs = HighLevelGpuProgramManager.Instance.CreateProgram( "VS_" + shaderCount.ToString(), 
																	//ResourceGroupManager.DefaultResourceGroupName,
																	shaderGenerator.Language,
																	GpuProgramType.Vertex );	
			vs.Source = shaderSource;
			vs.SetParam( "entry_point", vertexProgramName );
			vs.SetParam( "target", shaderGenerator.VPTarget );
			vs.Load();

			vertexProgramUsage.Program = vs;
			vertexProgramUsage.Params = vs.CreateParameters();

			fs = HighLevelGpuProgramManager.Instance.CreateProgram( "FS_" + shaderCount.ToString(), 
																	//ResourceGroupManager.DefaultResourceGroupName,
																	shaderGenerator.Language,
																	GpuProgramType.Fragment );	
			fs.Source = shaderSource;
			fs.SetParam( "entry_point", fragmentProgramName );
			fs.SetParam( "target", shaderGenerator.FPTarget );
			fs.Load();

			fragmentProgramUsage.Program = fs;
			fragmentProgramUsage.Params = fs.CreateParameters();

	
            //ok, have to record the new program in the map only if it has not been created before
            FixedFunctionPrograms newPrograms = shaderGenerator.CreateFixedFunctionPrograms();
            newPrograms.FixedFunctionState=state;
            newPrograms.FragmentProgramUsage=fragmentProgramUsage;
            newPrograms.VertexProgramUsage= vertexProgramUsage;

            
            //then check the map to find where to put the new program
            State2Declaration2ProgramsMap languageMaps;
            VertexBufferDeclaration2FixedFunctionProgramsMap fixedFunctionStateMaps;

            if (!language2State2Declaration2ProgramsMap.ContainsKey(generatorName))
            {
                languageMaps = new State2Declaration2ProgramsMap();
                language2State2Declaration2ProgramsMap.Add( generatorName, languageMaps );
            }
            else
            {
                languageMaps = language2State2Declaration2ProgramsMap[ generatorName];
            }

            if ( !languageMaps.ContainsKey( state ) )
            {
                fixedFunctionStateMaps = new VertexBufferDeclaration2FixedFunctionProgramsMap();
                languageMaps.Add( state, fixedFunctionStateMaps );
            }
            else
            {
                fixedFunctionStateMaps = languageMaps[ state ];
            }

            if ( !fixedFunctionStateMaps.ContainsKey( vertexBufferDeclaration ) )
            {
                fixedFunctionStateMaps.Add( vertexBufferDeclaration, newPrograms );
            }

            programsToDeleteAtTheEnd.Add(newPrograms);
            return newPrograms;		
		}

        public void saveShader(string shaderSource)
        {
            //save the shader just to understand and learn why it bugs :)
            string str = "shaderCheck.txt";
            int w = 0;
            while (System.IO.File.Exists(str))
            {
                str = "shaderCheck" + Convert.ToString(w) + ".txt";
                w++;
            }
            System.IO.StreamWriter sw = new System.IO.StreamWriter(str);
            sw.Write(shaderSource);
            sw.Flush();
            sw.Close();
        }

		#endregion Methods

	}

}
