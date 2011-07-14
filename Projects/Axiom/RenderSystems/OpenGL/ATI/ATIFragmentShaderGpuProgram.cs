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

using Axiom.Core;
using Axiom.Graphics;
using Axiom.RenderSystems.OpenGL;

using Tao.OpenGl;

using ResourceHandle = System.UInt64;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGL.ATI
{
	/// <summary>
	/// Summary description for ATIFragmentShaderGpuProgram.
	/// </summary>
	public class ATIFragmentShaderGpuProgram : GLGpuProgram
	{
		public ATIFragmentShaderGpuProgram( ResourceManager parent, string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader )
			: base( parent, name, handle, group, isManual, loader )
		{
            throw new AxiomException("This needs upgrading");
			programType = Gl.GL_FRAGMENT_SHADER_ATI;
			programId = Gl.glGenFragmentShadersATI( 1 );
		}

		#region Implementation of GpuProgram

		protected override void LoadFromSource()
		{
			PixelShader assembler = new PixelShader();

			//bool testError = assembler.RunTests();

			bool error = !assembler.Compile( Source );

			if ( !error )
			{
				Gl.glBindFragmentShaderATI( programId );
				Gl.glBeginFragmentShaderATI();

				// Compile and issue shader commands
				error = !assembler.BindAllMachineInstToFragmentShader();

				Gl.glEndFragmentShaderATI();
			}
			else
			{
			}
		}

		public override void Unload()
		{
			base.Unload();

			// delete the fragment shader for good
			Gl.glDeleteFragmentShaderATI( programId );
		}


		#endregion Implementation of GpuProgram

		#region Implementation of GLGpuProgram

		public override void Bind()
		{
			Gl.glEnable( programType );
			Gl.glBindFragmentShaderATI( programId );
		}

        public override void BindProgramParameters(GpuProgramParameters parms, GpuProgramParameters.GpuParamVariability mask)
		{
			// program constants done internally by compiler for local
			if ( parms.HasFloatConstants )
			{
				for ( int index = 0; index < parms.FloatConstantCount; index++ )
				{
                    using (var entry = parms.GetFloatPointer(index))
                    {
                        // send the params 4 at a time
                        throw new AxiomException( "Update this!" );
                        Gl.glSetFragmentShaderConstantATI( Gl.GL_CON_0_ATI + index, entry.Pointer );
                    }
				}
			}
		}

		public override void Unbind()
		{
			Gl.glDisable( programType );
		}

		#endregion Implementation of GLGpuProgram
	}

	/// <summary>
	/// 
	/// </summary>
	public class ATIFragmentShaderFactory : IOpenGLGpuProgramFactory
	{
		#region IOpenGLGpuProgramFactory Members

		public GLGpuProgram Create( ResourceManager parent, string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader, GpuProgramType type, string syntaxCode )
		{
			// creates and returns a new ATI fragment shader implementation
			GLGpuProgram ret = new ATIFragmentShaderGpuProgram( parent, name, handle, group, isManual, loader );
			ret.Type = type;
			ret.SyntaxCode = syntaxCode;
			return ret;
		}

		#endregion
	}

}
