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
using System.Diagnostics;
using System.Runtime.InteropServices;

using Axiom.Core;
using Axiom.Graphics;

using Tao.OpenGl;

using ResourceHandle = System.UInt64;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGL.Nvidia
{
	/// <summary>
	///     Specialization of GpuProgram that accepts source for DX8 level
	///     gpu programs and allows them to run on nVidia cards that support
	///     register and texture combiners.
	/// </summary>
	public class NvparseFragmentProgram : GLGpuProgram
	{
		#region Constructor

		public NvparseFragmentProgram( ResourceManager parent, string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader )
			: base( parent, name, handle, group, isManual, loader )
		{
			// create a display list
			programId = Gl.glGenLists( 1 );
		}

		#endregion Constructor

		#region GpuProgram Members

		/// <summary>
		///     Loads the raw ASM source and runs Nvparse to send the appropriate
		///     texture/register combiner instructions to the card.
		/// </summary>
		protected override void LoadFromSource()
		{
			// generate a new display list
			Gl.glNewList( programId, Gl.GL_COMPILE );

			int pos = source.IndexOf( "!!" );

			while( pos != -1 && pos != source.Length )
			{
				int newPos = source.IndexOf( "!!", pos + 1 );

				if( newPos == -1 )
				{
					newPos = source.Length;
				}

				string script = source.Substring( pos, newPos - pos );

				nvparse( script );

				string error = nvparse_get_errors();

				if( error != null && error.Length > 0 )
				{
					LogManager.Instance.Write( "nvparse error: {0}", error );
				}

				pos = newPos;
			}

			// ends the declaration of this display list
			Gl.glEndList();
		}

		public override void Unload()
		{
			base.Unload();

			// delete the list
			Gl.glDeleteLists( programId, 1 );
		}

		#endregion GpuProgram Members

		#region GLGpuProgram Members

		/// <summary>
		///     Binds the Nvparse program to the current context.
		/// </summary>
		public override void Bind()
		{
			Gl.glCallList( programId );
			Gl.glEnable( Gl.GL_TEXTURE_SHADER_NV );
			Gl.glEnable( Gl.GL_REGISTER_COMBINERS_NV );
			Gl.glEnable( Gl.GL_PER_STAGE_CONSTANTS_NV );
		}

		/// <summary>
		///     Unbinds the Nvparse program from the current context.
		/// </summary>
		public override void Unbind()
		{
			Gl.glDisable( Gl.GL_TEXTURE_SHADER_NV );
			Gl.glDisable( Gl.GL_REGISTER_COMBINERS_NV );
			Gl.glDisable( Gl.GL_PER_STAGE_CONSTANTS_NV );
		}

		/// <summary>
		///     Called to pass parameters to the Nvparse program.
		/// </summary>
		/// <param name="parms"></param>
		public override void BindParameters( GpuProgramParameters parms )
		{
			// Register combiners uses 2 constants per texture stage (0 and 1)
			// We have stored these as (stage * 2) + const_index
			if( parms.HasFloatConstants )
			{
				for( int index = 0; index < parms.FloatConstantCount; index++ )
				{
					GpuProgramParameters.FloatConstantEntry entry = parms.GetFloatConstant( index );

					if( entry.isSet )
					{
						int combinerStage = Gl.GL_COMBINER0_NV + ( index / 2 );
						int pname = Gl.GL_CONSTANT_COLOR0_NV + ( index % 2 );

						// send the params 4 at a time
						Gl.glCombinerStageParameterfvNV( combinerStage, pname, entry.val );
					}
				}
			}
		}

		#endregion GLGpuProgram Members

		#region Nvparse externs

		private const string NATIVE_LIB = "nvparse.dll";

		[DllImport( NATIVE_LIB, CallingConvention = CallingConvention.Cdecl )]
		private static extern void nvparse( string input );

		[DllImport( NATIVE_LIB, CallingConvention = CallingConvention.Cdecl, EntryPoint = "nvparse_get_errors", CharSet = CharSet.Auto )]
		unsafe private static extern byte** nvparse_get_errorsA();

		/// <summary>
		///     
		/// </summary>
		/// <returns></returns>
		// TODO: Only returns first error for now
		private string nvparse_get_errors()
		{
			unsafe
			{
				byte** ret = nvparse_get_errorsA();
				byte* bytes = ret[ 0 ];

				if( bytes != null )
				{
					int i = 0;
					string error = "";

					while( bytes[ i ] != '\0' )
					{
						error += (char)bytes[ i ];
						i++;
					}

					return error;
				}
			}

			return null;
		}

		#endregion Nvparse externs
	}

	/// <summary>
	///     Factory class which handles requests for DX8 level functionality in 
	///     OpenGL on GeForce3/4 hardware.
	/// </summary>
	public class NvparseProgramFactory : IOpenGLGpuProgramFactory
	{
		#region IOpenGLGpuProgramFactory Members

		public GLGpuProgram Create( ResourceManager parent, string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader, GpuProgramType type, string syntaxCode )
		{
			GLGpuProgram ret = new NvparseFragmentProgram( parent, name, handle, group, isManual, loader );
			ret.SyntaxCode = syntaxCode;
			ret.Type = type;
			return ret;
		}

		#endregion
	}
}
