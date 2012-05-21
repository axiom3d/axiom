#region MIT/X11 License

//Copyright © 2003-2012 Axiom 3D Rendering Engine Project
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.

#endregion License

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGLES2.GLSLES
{
	/// <summary>
	/// Ogre assumes that there are separate vertex and fragment programs to deal with but
	///    GLSL ES has one program pipeline object that represents the active vertex and fragment program objects
	///    during a rendering state.  GLSL vertex and fragment program objects are compiled separately
	///    and then attached to a program object and then the program pipeline object is linked.
	///    Since Ogre can only handle one vertex program stage and one fragment program stage being active
	///    in a pass, the GLSL ES Program Pipeline Manager does the same.  The GLSL ES Program Pipeline
	///    Manager acts as a state machine and activates a pipeline object based on the active
	///    vertex and fragment program.  Previously created pipeline objects are stored along with a unique
	///    key in a hash_map for quick retrieval the next time the pipeline object is required.
	/// </summary>
	internal class GLSLESProgramPipelineManager : GLSLESProgramManagerCommon
	{
		private Dictionary<long, GLSLESProgramPipeline> programPipelines;
		private GLSLESProgramPipeline activeProgramPipeline;
		private static GLSLESProgramPipelineManager _instance = null;

		public GLSLESProgramPipelineManager()
		{
			this.activeProgramPipeline = null;
		}

		~GLSLESProgramPipelineManager()
		{
			//Iterate through map container and delete program pipelines
			foreach ( var key in this.programPipelines.Keys )
			{
				this.programPipelines[ key ] = null;
			}
		}

		public GLSLESProgramPipeline ActiveProgramPipeline
		{
			get
			{
				//if there is an active link program then return it
				if ( this.activeProgramPipeline != null )
				{
					return this.activeProgramPipeline;
				}

				//No active link program so find one or make a new one
				//Is there an active key?
				long activeKey = 0;

				if ( activeVertexGpuProgram != null )
				{
					activeKey = activeVertexGpuProgram.ProgramID << 32;
				}
				if ( activeFragmentGpuProgram != null )
				{
					activeKey += activeFragmentGpuProgram.ProgramID;
				}

				//Only return a program pipeline object if a vertex or fragment stage exist
				if ( activeKey > 0 )
				{
					//Find the key in the hash map
					if ( !this.programPipelines.ContainsKey( activeKey ) )
					{
						this.activeProgramPipeline = new GLSLESProgramPipeline( activeVertexGpuProgram, activeFragmentGpuProgram );
						this.programPipelines.Add( activeKey, new GLSLESProgramPipeline( activeVertexGpuProgram, activeFragmentGpuProgram ) );
					}
					else
					{
						//Found a link program in map container so make it active
						this.activeProgramPipeline = this.programPipelines[ activeKey ];
					}
				}
				//Make the program object active
				if ( this.activeProgramPipeline != null )
				{
					this.activeProgramPipeline.Activate();
				}

				return this.activeProgramPipeline;
			}
		}

		public GLSLESGpuProgram ActiveVertexLinkProgram
		{
			set
			{
				if ( value != activeVertexGpuProgram )
				{
					activeVertexGpuProgram = value;
					this.activeProgramPipeline = null;
				}
			}
		}

		public GLSLESGpuProgram ActiveFragmentLinkProgram
		{
			set
			{
				if ( value != activeFragmentGpuProgram )
				{
					activeFragmentGpuProgram = value;
					this.activeProgramPipeline = null;
				}
			}
		}

		public static GLSLESProgramPipelineManager Instance
		{
			get
			{
				if ( _instance == null )
				{
					_instance = new GLSLESProgramPipelineManager();
				}
				return _instance;
			}
		}
	}
}
