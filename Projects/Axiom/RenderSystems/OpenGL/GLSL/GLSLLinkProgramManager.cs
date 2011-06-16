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
using System.Collections;
using System.Collections.Generic;
using Tao.OpenGl;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGL.GLSL
{
	/// <summary>
	///		Axiom assumes that there are seperate vertex and fragment programs to deal with but
	///		GLSL has one program object that represents the active vertex and fragment shader objects
	///		during a rendering state.  GLSL Vertex and fragment 
	///		shader objects are compiled seperately and then attached to a program object and then the
	///		program object is linked.  Since Ogre can only handle one vertex program and one fragment
	///		program being active in a pass, the GLSL Link Program Manager does the same.  The GLSL Link
	///		program manager acts as a state machine and activates a program object based on the active
	///		vertex and fragment program.  Previously created program objects are stored along with a unique
	///		key in a hash_map for quick retrieval the next time the program object is required.
	/// </summary>
	public sealed class GLSLLinkProgramManager : IDisposable
	{
		#region Singleton implementation

		/// <summary>
		///     Singleton instance of this class.
		/// </summary>
		private static GLSLLinkProgramManager instance;

		/// <summary>
		///     Internal constructor.  This class cannot be instantiated externally.
		/// </summary>
		internal GLSLLinkProgramManager()
		{
			if ( instance == null )
			{
				instance = this;
			}

            typeEnumMap = new Dictionary<string, int>
            {
                {"float", Gl.GL_FLOAT},
                {"vec2", Gl.GL_FLOAT_VEC2},
                {"vec3", Gl.GL_FLOAT_VEC3},
                {"vec4", Gl.GL_FLOAT_VEC4},
                {"sampler1D", Gl.GL_SAMPLER_1D},
                {"sampler2D", Gl.GL_SAMPLER_2D},
                {"sampler3D", Gl.GL_SAMPLER_3D},
                {"samplerCube", Gl.GL_SAMPLER_CUBE},
                {"sampler1DShadow", Gl.GL_SAMPLER_1D_SHADOW},
                {"sampler2DShadow", Gl.GL_SAMPLER_2D_SHADOW},
                {"int", Gl.GL_INT},
                {"ivec2", Gl.GL_INT_VEC2},
                {"ivec3", Gl.GL_INT_VEC3},
                {"ivec4", Gl.GL_INT_VEC4},
                {"mat2", Gl.GL_FLOAT_MAT2},
                {"mat3", Gl.GL_FLOAT_MAT3},
                {"mat4", Gl.GL_FLOAT_MAT4},
                // GL 2.1
                {"mat2x2", Gl.GL_FLOAT_MAT2},
                {"mat3x3", Gl.GL_FLOAT_MAT3},
                {"mat4x4", Gl.GL_FLOAT_MAT4},
                {"mat2x3", Gl.GL_FLOAT_MAT2x3},
                {"mat3x2", Gl.GL_FLOAT_MAT3x2},
                {"mat3x4", Gl.GL_FLOAT_MAT3x4},
                {"mat4x3", Gl.GL_FLOAT_MAT4x3},
                {"mat2x4", Gl.GL_FLOAT_MAT2x4},
                {"mat4x2", Gl.GL_FLOAT_MAT4x2},
            };
		}

		/// <summary>
		///     Gets the singleton instance of this class.
		/// </summary>
		public static GLSLLinkProgramManager Instance
		{
			get
			{
				return instance;
			}
		}

		#endregion Singleton implementation

		#region Fields

		/// <summary>
		///		List holding previously created program objects.
		/// </summary>
		private readonly Hashtable linkPrograms = new Hashtable();
		/// <summary>
		///		Currently active vertex GPU program.
		/// </summary>
		private GLSLGpuProgram activeVertexProgram;
        /// <summary>
        ///		Currently active geometry GPU program.
        /// </summary>
        private GLSLGpuProgram activeGeometryProgram;
		/// <summary>
		///		Currently active fragment GPU program.
		/// </summary>
		private GLSLGpuProgram activeFragmentProgram;
		/// <summary>
		///		Currently active link program.
		/// </summary>
		private GLSLLinkProgram activeLinkProgram;

	    private Dictionary<string, int> typeEnumMap;

		#endregion Fields

        #region Properties

        /// <summary>
		///		Get the program object that links the two active shader objects together
		///		if a program object was not already created and linked a new one is created and linked.
		/// </summary>
		public GLSLLinkProgram ActiveLinkProgram
		{
			get
			{
				// if there is an active link program then return it
				if ( activeLinkProgram != null )
				{
					return activeLinkProgram;
				}

				// no active link program so find one or make a new one
				// is there an active key?
				long activeKey = 0;

				if ( activeVertexProgram != null )
				{
					activeKey = activeVertexProgram.ProgramID << 32;
				}
                if (activeGeometryProgram != null)
                {
                    activeKey = activeGeometryProgram.ProgramID << 16;
                }
				if ( activeFragmentProgram != null )
				{
					activeKey += activeFragmentProgram.ProgramID;
				}

				// only return a link program object if a vertex or fragment program exist
				if ( activeKey > 0 )
				{
                    // find the key in the hash map
				    var programFound = linkPrograms[ activeKey ];
                    // program object not found for key so need to create it
                    if (programFound == null)
                    {
                        activeLinkProgram = new GLSLLinkProgram(activeVertexProgram, activeGeometryProgram, activeFragmentProgram);
                        linkPrograms[ activeKey ] = activeLinkProgram;
                    }
                    else
                    {
                        // found a link program in map container so make it active
                        activeLinkProgram = (GLSLLinkProgram)programFound;
                    }

				}

				// make the program object active
				if ( activeLinkProgram != null )
				{
					activeLinkProgram.Activate();
				}

				return activeLinkProgram;
			}
		}

		#endregion Properties

        #region Constructors

        #endregion

		#region Methods

		/// <summary>
		///		Set the active fragment shader for the next rendering state.
		/// </summary>
		/// <remarks>
		///		The active program object will be cleared.
		///		Normally called from the GLSLGpuProgram.BindProgram and UnbindProgram methods
		/// </remarks>
		/// <param name="fragmentProgram"></param>
		public void SetActiveFragmentShader( GLSLGpuProgram fragmentProgram )
		{
			if ( fragmentProgram != activeFragmentProgram )
			{
				activeFragmentProgram = fragmentProgram;

				// active link program is no longer valid
				activeLinkProgram = null;

				// change back to fixed pipeline
				Gl.glUseProgramObjectARB( 0 );
			}
		}

		/// <summary>
		///		Set the active vertex shader for the next rendering state.
		/// </summary>
		/// <remarks>
		///		The active program object will be cleared.
		///		Normally called from the GLSLGpuProgram.BindProgram and UnbindProgram methods
		/// </remarks>
		/// <param name="vertexProgram"></param>
		public void SetActiveVertexShader( GLSLGpuProgram vertexProgram )
		{
			if ( vertexProgram != activeVertexProgram )
			{
				activeVertexProgram = vertexProgram;

				// active link program is no longer valid
				activeLinkProgram = null;

				// change back to fixed pipeline
				Gl.glUseProgramObjectARB( 0 );
			}
		}

		#endregion Methods

		#region IDisposable Members

		/// <summary>
		///     Called when the engine is shutting down.
		/// </summary>
		public void Dispose()
		{
			foreach ( GLSLLinkProgram program in linkPrograms.Values )
			{
				program.Dispose();
			}

			linkPrograms.Clear();
		}

		#endregion IDisposable Members
	}
}
