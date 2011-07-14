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
using System.Runtime.InteropServices;

using Axiom.Core;
using Axiom.Graphics;

using Tao.OpenGl;

using ResourceHandle = System.UInt64;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGL.Nvidia
{
	/// <summary>
	///     Base class for handling nVidia specific extensions for supporting
	///     GeForceFX level gpu programs
	/// </summary>
	/// <remarks>
	///     Subclasses must implement BindParameters since there are differences
	///     in how parameters are passed to NV vertex and fragment programs.
	/// </remarks>
	public abstract class NV3xGpuProgram : GLGpuProgram
	{
		#region Constructor

		public NV3xGpuProgram( ResourceManager parent, string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader )
			: base( parent, name, handle, group, isManual, loader )
		{

		    throw new AxiomException( "This needs upgrading" );
			// generate the program and store the unique name
			Gl.glGenProgramsNV( 1, out programId );

			// find the GL enum for the type of program this is
			programType = ( Type == GpuProgramType.Vertex ) ? Gl.GL_VERTEX_PROGRAM_NV : Gl.GL_FRAGMENT_PROGRAM_NV;
		}

		#endregion Constructor

		#region GpuProgram Members

		/// <summary>
		///     Loads NV3x level assembler programs into the hardware.
		/// </summary>
		protected override void LoadFromSource()
		{
			// bind this program before loading
			Gl.glBindProgramNV( programType, programId );

			// load the ASM source into an NV program
			Gl.glLoadProgramNV( programType, programId, Source.Length, System.Text.Encoding.ASCII.GetBytes( Source ) ); // TAO 2.0
			//Gl.glLoadProgramNV( programType, programId, source.Length, source );

			// get the error string from the NV program loader
			string error = Gl.glGetString( Gl.GL_PROGRAM_ERROR_STRING_NV ); // TAO 2.0
			//string error = Marshal.PtrToStringAnsi( Gl.glGetString( Gl.GL_PROGRAM_ERROR_STRING_NV ) );

			// if there was an error, report it
			if ( error != null && error.Length > 0 )
			{
				int pos;

				// get the position of the error
				Gl.glGetIntegerv( Gl.GL_PROGRAM_ERROR_POSITION_ARB, out pos );

				throw new Exception( string.Format( "Error on line {0} in program '{1}'\nError: {2}", pos, Name, error ) );
			}
		}

		/// <summary>
		///     Overridden to delete the NV program.
		/// </summary>
		public override void Unload()
		{
			base.Unload();

			// delete this NV program
			Gl.glDeleteProgramsNV( 1, ref programId );
		}


		#endregion GpuProgram Members

		#region GLGpuProgram Members

		/// <summary>
		///     Binds an NV program.
		/// </summary>
		public override void Bind()
		{
			// enable this program type
			Gl.glEnable( programType );

			// bind the program to the context
			Gl.glBindProgramNV( programType, programId );
		}

		/// <summary>
		///     Unbinds an NV program.
		/// </summary>
		public override void Unbind()
		{
			// disable this program type
			Gl.glDisable( programType );
		}

		#endregion GLGpuProgram Members
	}

	/// <summary>
	///     GeForceFX class vertex program.
	/// </summary>
	public class VP30GpuProgram : NV3xGpuProgram
	{
		#region Constructor

		public VP30GpuProgram( ResourceManager parent, string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader )
			: base( parent, name, handle, group, isManual, loader )
		{
            throw new AxiomException( "This needs upgrading" );
		}

		#endregion Constructor

		#region GpuProgram Members

		/// <summary>
		///     Binds params by index to the vp30 program.
		/// </summary>
		/// <param name="parms"></param>
        public override void BindProgramParameters(GpuProgramParameters parms, GpuProgramParameters.GpuParamVariability mask)
		{
			if ( parms.HasFloatConstants )
			{
				for ( int index = 0; index < parms.FloatConstantCount; index++ )
				{
					using (var entry = parms.GetFloatPointer( index ))
					{
						// send the params 4 at a time
					    throw new AxiomException( "Update this!" );
						Gl.glProgramParameter4fvNV( programType, index, entry.Pointer );
					}
				}
			}
		}

		/// <summary>
		///     Overriden to return parms set to transpose matrices.
		/// </summary>
		/// <returns></returns>
		public override GpuProgramParameters CreateParameters()
		{
			GpuProgramParameters parms = base.CreateParameters();

			parms.TransposeMatrices = true;

			return parms;
		}

		#endregion GpuProgram Members
	}

	/// <summary>
	///     GeForceFX class fragment program.
	/// </summary>
	public class FP30GpuProgram : NV3xGpuProgram
	{
		#region Constructor

		public FP30GpuProgram( ResourceManager parent, string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader )
			: base( parent, name, handle, group, isManual, loader )
		{
            throw new AxiomException("This needs upgrading");
		}

		#endregion Constructor

		#region GpuProgram members

		/// <summary>
		///     Binds named parameters to fp30 programs.
		/// </summary>
		/// <param name="parms"></param>
        public override void BindProgramParameters(GpuProgramParameters parms, GpuProgramParameters.GpuParamVariability mask)
		{
		    throw new NotImplementedException();
            /*
			if ( parms.HasFloatConstants )
			{
				for ( int index = 0; index < parms.FloatConstantCount; index++ )
				{
					string name = parms.GetNameByIndex( index );

					if ( name != null )
					{
						using (var entry = parms.GetFloatPointer( index ))
					    {

					        // send the params 4 at a time
					        throw new AxiomException( "Update this!" );
					        Gl.glProgramNamedParameter4fvNV( programId, name.Length, System.Text.Encoding.ASCII.GetBytes( name ),
					                                         entry.Pointer ); // TAO 2.0
					        //Gl.glProgramNamedParameter4fvNV( programId, name.Length, name, entry.val );
					    }
					}
				}
			}
             */
		}
		#endregion GpuProgram members
	}

	/// <summary>
	///     Factory class that handles requested for GeForceFX program implementations.
	/// </summary>
	public class NV3xGpuProgramFactory : IOpenGLGpuProgramFactory
	{
		#region IOpenGLGpuProgramFactory Members

		public GLGpuProgram Create( ResourceManager parent, string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader, GpuProgramType type, string syntaxCode )
		{
			GLGpuProgram ret;
			if ( type == GpuProgramType.Vertex )
			{
				ret = new VP30GpuProgram( parent, name, handle, group, isManual, loader );
			}
			else
			{
				ret = new FP30GpuProgram( parent, name, handle, group, isManual, loader );
			}
			ret.Type = type;
			ret.SyntaxCode = syntaxCode;
			return ret;
		}

		#endregion

	}
}
