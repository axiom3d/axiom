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
#endregion LGPL License

#region SVN Version Information
// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Runtime.InteropServices;
using System.Text;

using Axiom.Core;
using Axiom.Graphics;
using Axiom.RenderSystems.OpenGL;

using Tao.OpenGl;

using ResourceHandle = System.UInt64;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGL
{
	/// <summary>
	/// Summary description for ARBGpuProgram.
	/// </summary>
	public class ARBGpuProgram : GLGpuProgram
	{
		#region Constructor

		public ARBGpuProgram( ResourceManager parent, string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader )
			: base( parent, name, handle, group, isManual, loader )
		{
			// generate a new program
			Gl.glGenProgramsARB( 1, out programId );
		}

		#endregion Constructor

		#region Finalizer

		~ARBGpuProgram()
		{
			if ( _handle.IsAllocated )
			{
				//GCHandle's a value type, valid even inside the scope of a a finalizer
				_handle.Free();
			}

			//Gl.glDeleteProgramsARB(1, ref programId);
		}

		#endregion Finalizer

		#region Private

		private GCHandle _handle; //handle to managed data

		#endregion Private

		#region Implementation of GpuProgram

		/// <summary>
		///     Load Assembler gpu program source.
		/// </summary>
		protected override void LoadFromSource()
		{
			Gl.glBindProgramARB( programType, programId );

			// MONO: Cannot compile programs when passing in the string as is for whatever reason.
			// would get "Invalid vertex program header", which I assume means the source got mangled along the way
			byte[] bytes = Encoding.ASCII.GetBytes( Source );
			// TODO: We pin the managed 'bytes' to get a pointer to data and get sure they won't move around in memory.
			//       In case glProgramStringARB() doesn't store the pointer internally, we better free the handle yet in this method,
			//       or rather utilize a fixed (byte* sourcePtr = bytes) statement, which cares for unpinning the data even in case
			//       of an exception, where GCHandle.Free() would be missed without try-finally.

			//       In case the above isn't possible, the theory also says that if we have a handle to managed data (except Weak, WeakTrackResurrection handle types)
			//       we should be implementing a finalizer to ensure freeing the handle as that can be treated as an unmanaged resource from this point of view.
			//       I decided not to extend this class with IDisposable for several reasons, including class's user contract,
			//       and the fact that this might be a temporary issue only. So for now only a finalizer will take care for avoiding memory leaks (although minor ones in this case).
			//       So recheck the MONO issue later, the above comment talks about passing the string directly, btw. the method seems to take byte[] as well (if that would work eventually...).
			if ( _handle.IsAllocated )
			{
				_handle.Free();
			}
			_handle = GCHandle.Alloc( bytes, GCHandleType.Pinned );
			IntPtr sourcePtr = _handle.AddrOfPinnedObject();

			Gl.glProgramStringARB( programType, Gl.GL_PROGRAM_FORMAT_ASCII_ARB, Source.Length, sourcePtr );

			// check for any errors
			if ( Gl.glGetError() == Gl.GL_INVALID_OPERATION )
			{
				int pos;
				string error;

				Gl.glGetIntegerv( Gl.GL_PROGRAM_ERROR_POSITION_ARB, out pos );
				error = Gl.glGetString( Gl.GL_PROGRAM_ERROR_STRING_ARB ); // TAO 2.0
				//error = Marshal.PtrToStringAnsi( Gl.glGetString( Gl.GL_PROGRAM_ERROR_STRING_ARB ) );

				throw new Exception( string.Format( "Error on line {0} in program '{1}'\nError: {2}", pos, Name, error ) );
			}
		}

		/// <summary>
		///     Unload GL gpu programs.
		/// </summary>
		public override void Unload()
		{

			if ( IsLoaded )
			{
				if ( _handle.IsAllocated )
				{
					_handle.Free();
				}

				Gl.glDeleteProgramsARB( 1, ref programId );
				base.Unload();

			}
		}

		public override GpuProgramType Type
		{
			get
			{
				return base.Type;
			}
			set
			{
				base.Type = value;
				programType = ( Type == GpuProgramType.Vertex ) ? Gl.GL_VERTEX_PROGRAM_ARB : Gl.GL_FRAGMENT_PROGRAM_ARB;
			}
		}

		#endregion Implementation of GpuProgram

		#region Implementation of GLGpuProgram

		public override void Bind()
		{
			if ( !IsSupported )
				return;
			Gl.glEnable( programType );
			Gl.glBindProgramARB( programType, programId );
		}

		public override void Unbind()
		{
			if ( !IsSupported )
				return;
			Gl.glBindProgramARB( programType, 0 );
			Gl.glDisable( programType );
		}


        [OgreVersion(1, 7, 2790, "using 4f rather than 4fv for uniform upload")]
        public override void BindProgramParameters(GpuProgramParameters parms, GpuProgramParameters.GpuParamVariability mask)
		{
            var type = programType;
    
	        // only supports float constants
	        var floatStruct = parms.FloatLogicalBufferStruct;

	        foreach (var i in floatStruct.Map)
	        {
		        if ((i.Value.Variability & mask) != 0)
		        {
			        var logicalIndex = i.Key;
		            var pFloat = parms.GetFloatPointer();
		            var ptr = i.Value.PhysicalIndex;
			        {
                        for (var j = 0; j < i.Value.CurrentSize; j += 4)
                        {
                            var x = pFloat[ ptr + j ];
                            var y = pFloat[ ptr + j + 1 ];
                            var z = pFloat[ ptr + j + 2 ];
                            var w = pFloat[ ptr + j + 3 ];
                            Gl.glProgramLocalParameter4fARB( type, logicalIndex, x, y, z, w );
                            ++logicalIndex;
                        }
                    }
		        }
	        }
		}

		#endregion Implementation of GLGpuProgram
	}

	/// <summary>
	///     Creates a new ARB gpu program.
	/// </summary>
	public class ARBGpuProgramFactory : IOpenGLGpuProgramFactory
	{
		#region IOpenGLGpuProgramFactory Implementation

		public GLGpuProgram Create( ResourceManager parent, string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader, GpuProgramType type, string syntaxCode )
		{
			GLGpuProgram ret = new ARBGpuProgram( parent, name, handle, group, isManual, loader );
			ret.Type = type;
			ret.SyntaxCode = syntaxCode;
			return ret;
		}

		#endregion IOpenGLGpuProgramFactory Implementation
	}
}