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

using Axiom.Core;
using Axiom.Graphics;

using GL = OpenTK.Graphics.ES20.GL;
using GLenum = OpenTK.Graphics.ES20.All;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGLES2
{
	internal class GLES2HardwareBufferManager : HardwareBufferManager
	{
		public GLES2HardwareBufferManager()
			: base( new GLES2HardwareBufferManagerBase() ) {}

		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					_baseInstance.Dispose();
				}
			}

			base.dispose( disposeManagedResources );
		}

		public static GLenum GetGLUsage( BufferUsage usage )
		{
			return GLES2HardwareBufferManagerBase.GetGLUsage( usage );
		}

		public static GLenum GetGLType( VertexElementType type )
		{
			return GLES2HardwareBufferManagerBase.GetGLType( type );
		}

		/// <summary>
		/// Allows us to use a pool of memory as a scratch area for hardware buffers. 
		/// This is because GL.MapBuffer is incredibly inefficient, seemingly no matter 
		/// what options we give it. So for the period of lock/unlock, we will instead 
		/// allocate a section of a local memory pool, and use GL.BufferSubDataARB / GL.GetBufferSubDataARB instead.
		/// </summary>
		/// <param name="size"></param>
		public BufferBase AllocateScratch( int size )
		{
			return ( (GLES2HardwareBufferManagerBase) _baseInstance ).AllocateScratch( size );
		}

		public void DeallocateScratch( BufferBase ptr )
		{
			( (GLES2HardwareBufferManagerBase) _baseInstance ).DeallocateScratch( ptr );
		}

		public int MapBufferThreshold
		{
			get { return ( (GLES2HardwareBufferManagerBase) _baseInstance ).MapBufferThreshold; }
			set { ( (GLES2HardwareBufferManagerBase) _baseInstance ).MapBufferThreshold = value; }
		}
	}
}
