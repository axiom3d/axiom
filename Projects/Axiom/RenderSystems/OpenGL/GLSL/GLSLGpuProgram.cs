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

using Axiom.Graphics;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGL.GLSL
{
	/// <summary>
	///		GLSL low level compiled shader object - this class is used to get at the linked program object 
	///		and provide an interface for GLRenderSystem calls.  GLSL does not provide access to the
	///		low level code of the shader so this class is really just a dummy place holder.
	///		GLSL uses a program object to represent the active vertex and fragment programs used
	///		but Axiom materials maintain seperate instances of the active vertex and fragment programs
	///		which creates a small problem for GLSL integration.  The GLSLGpuProgram class provides the
	///		interface between the GLSLLinkProgramManager , GLRenderSystem, and the active GLSLProgram
	///		instances.
	/// </summary>
	public class GLSLGpuProgram : GLGpuProgram
	{
		#region Fields

		/// <summary>
		///		GL Handle for the shader object.
		/// </summary>
		protected GLSLProgram glslProgram;

		/// <summary>
		///		Keep track of the number of vertex shaders created.
		/// </summary>
		protected static int vertexShaderCount;
		/// <summary>
		///		Keep track of the number of fragment shaders created.
		/// </summary>
		protected static int fragmentShaderCount;
        /// <summary>
        ///		Keep track of the number of geometry shaders created.
        /// </summary>
        protected static int geometryShaderCount;


		#endregion Fields

		#region Constructor

		public GLSLGpuProgram( GLSLProgram parent )
			: base( parent.Creator, parent.Name, parent.Handle, parent.Group, false, null )
		{
			this.Type = parent.Type;
			this.SyntaxCode = "glsl";

			// store off the reference to the parent program
			glslProgram = parent;

			if ( parent.Type == GpuProgramType.Vertex )
			{
				programId = ++vertexShaderCount;
			}
			else if (parent.Type == GpuProgramType.Fragment)
			{
				programId = ++fragmentShaderCount;
			}
			else
			{
			    programId = ++geometryShaderCount;
			}

			// transfer skeletal animation status from parent
			this.IsSkeletalAnimationIncluded = glslProgram.IsSkeletalAnimationIncluded;

			// there is nothing to load
			loadFromFile = false;
		}

		#endregion Constructor

		#region Properties

		/// <summary>
		///		Gets the GLSLProgram for the shader object.
		/// </summary>
		public GLSLProgram GLSLProgram
		{
			get
			{
				return glslProgram;
			}
		}

		#endregion Properties

		#region Resource Implementation

		protected override void load()
		{
			// nothing to do
		}

		protected override void unload()
		{
			// nothing to do
		}

        protected override void LoadFromSource()
        {
            // nothing to load
        }

		#endregion Resource Implementation

		#region GpuProgram Implementation

		public override void Bind()
		{
			// tell the Link Program Manager what shader is to become active
			switch (type)
			{
                case  GpuProgramType.Vertex:
				    GLSLLinkProgramManager.Instance.SetActiveVertexShader( this );
			        break;
                case GpuProgramType.Fragment:
                    GLSLLinkProgramManager.Instance.SetActiveFragmentShader(this);
			        break;
                case GpuProgramType.Geometry:
                    GLSLLinkProgramManager.Instance.SetActiveGeometryShader(this);
			        break;
			}
		}

        public override void Unbind()
        {
            // tell the Link Program Manager what shader is to become inactive
            if (type == GpuProgramType.Vertex)
            {
                GLSLLinkProgramManager.Instance.SetActiveVertexShader(null);
            }
            else if (type == GpuProgramType.Geometry)
            {
                GLSLLinkProgramManager.Instance.SetActiveGeometryShader(null);
            }
            else
            {
                GLSLLinkProgramManager.Instance.SetActiveFragmentShader(null);
            }
        }

		public override void BindParameters( GpuProgramParameters parameters )
		{
			// activate the link program object
			GLSLLinkProgram linkProgram = GLSLLinkProgramManager.Instance.ActiveLinkProgram;

			// pass on parameters from params to program object uniforms
			linkProgram.UpdateUniforms( parameters );
		}

        public override void BindProgramPassIterationParameters(GpuProgramParameters parms)
        {
            // activate the link program object
            GLSLLinkProgram linkProgram = GLSLLinkProgramManager.Instance.ActiveLinkProgram;

            // pass on parameters from params to program object uniforms
            linkProgram.UpdatePassIterationUniforms(parms);
        }

        /// @copydoc GLGpuProgram::getAttributeIndex
        public uint GetAttributeIndex(VertexElementSemantic semantic, uint index)
        {
            // get link program - only call this in the context of bound program
            var linkProgram = GLSLLinkProgramManager.Instance.ActiveLinkProgram;

            if (linkProgram.IsAttributeValid(sementic, index))
            {
                return linkProgram.GetAttributeIndex(semantic, index);
            }
            else
            {
                // fall back to default implementation, allow default bindings
                return base.GetAttributeIndex(semantic, index)
            }
        }

        /// @copydoc GLGpuProgram::isAttributeValid
        public bool IsAttributeValid(VertexElementSemantic semantic, uint index)
        {
            // get link program - only call this in the context of bound program
            var linkProgram = GLSLLinkProgramManager.Instance.ActiveLinkProgram;

            if (linkProgram.IsAttributeValid(sementic, index))
            {
                return true;
            } 
            else
            {
                // fall back to default implementation, allow default bindings
                return base.IsAttributeValid(semantic, index);
            }
        }

		#endregion GpuProgram Implementation
	}
}
