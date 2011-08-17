#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2010 Axiom Project Team
This file is part of Axiom.RenderSystems.OpenGLES
C# version developed by bostich.

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
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations
using System;
using Axiom.Graphics;
using Axiom.Core;
using OpenTK.Graphics.ES11;
using OpenGL = OpenTK.Graphics.ES11.GL;
using OpenGLOES = OpenTK.Graphics.ES11.GL.Oes;
#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGLES
{
	/// <summary>
	/// 
	/// </summary>
	public class GLESHardwareIndexBuffer : HardwareIndexBuffer
	{
		const int MapBufferThreshold = 1024 * 32;
		private int _bufferId = 0;
		IntPtr _scratchPtr;
		bool _lockedToScratch;
		bool _scratchUploadOnUnlock;
		int _scratchOffset;
		int _scratchSize;

		public int BufferID
		{
			get
			{
				return _bufferId;
			}
		}

		public GLESHardwareIndexBuffer( HardwareBufferManagerBase mgr, IndexType idxType, int numIndexes, BufferUsage usage, bool useShadowBuffer )
			: base( mgr, idxType, numIndexes, usage, false, useShadowBuffer )
		{
			if ( idxType == IndexType.Size32 )
			{
				throw new AxiomException( "32 bit hardware buffers are not allowed in OpenGL ES." );
			}

			if ( !useShadowBuffer )
			{
				throw new AxiomException( "Only support with shadowBuffer" );
			}

			OpenGL.GenBuffers( 1, ref _bufferId );
			GLESConfig.GlCheckError( this );
			if ( _bufferId == 0 )
			{
				throw new AxiomException( "Cannot create GL index buffer" );
			}

			OpenGL.BindBuffer( All.ElementArrayBuffer, _bufferId );
			GLESConfig.GlCheckError( this );
			OpenGL.BufferData( All.ElementArrayBuffer, new IntPtr( sizeInBytes ), IntPtr.Zero, GLESHardwareBufferManager.GetGLUsage( usage ) );
			GLESConfig.GlCheckError( this );
		}

		/// <summary>
		/// 
		/// </summary>
		protected override void UnlockImpl()
		{
			if ( _lockedToScratch )
			{
				if ( _scratchUploadOnUnlock )
				{
					// have to write the data back to vertex buffer
					WriteData( _scratchOffset, _scratchSize, _scratchPtr, _scratchOffset == 0 && _scratchSize == sizeInBytes );
				}
				// deallocate from scratch buffer
				( (GLESHardwareBufferManager)HardwareBufferManager.Instance ).DeallocateScratch( _scratchPtr );
				_lockedToScratch = false;
			}
			else
			{
				OpenGL.BindBuffer( All.ElementArrayBuffer, _bufferId );
				if ( !OpenGLOES.UnmapBuffer( All.ElementArrayBuffer ) )
				{
					throw new AxiomException( "Buffer data corrupted, please reload" );
				}
			}
			isLocked = false;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="offset"></param>
		/// <param name="length"></param>
		/// <param name="locking"></param>
		/// <returns></returns>
		protected override IntPtr LockImpl( int offset, int length, BufferLocking locking )
		{
			All access = 0;
			if ( isLocked )
			{
				throw new AxiomException( "Invalid attempt to lock an index buffer that has already been locked" );
			}

			IntPtr retPtr = IntPtr.Zero;
			if ( length < MapBufferThreshold )
			{
				retPtr = ( (GLESHardwareBufferManager)HardwareBufferManager.Instance ).AllocateScratch( length );
				if ( retPtr != IntPtr.Zero )
				{
					_lockedToScratch = true;
					_scratchOffset = offset;
					_scratchSize = length;
					_scratchPtr = retPtr;
					_scratchUploadOnUnlock = ( locking != BufferLocking.ReadOnly );

					if ( locking != BufferLocking.Discard )
					{
						ReadData( offset, length, retPtr );
					}
				}
			}
			else
			{
				throw new AxiomException( "Invalid Buffer lockSize" );
			}

			if ( retPtr == IntPtr.Zero )
			{
				OpenGL.BindBuffer( All.ElementArrayBuffer, _bufferId );
				// Use glMapBuffer
				if ( locking == BufferLocking.Discard )
				{
					OpenGL.BufferData( All.ElementArrayBuffer, new IntPtr( sizeInBytes ), IntPtr.Zero, GLESHardwareBufferManager.GetGLUsage( usage ) );
				}
				if ( ( usage & BufferUsage.WriteOnly ) != 0 )
				{
					access = All.WriteOnlyOes;
				}

				IntPtr pBuffer = OpenGLOES.MapBuffer( All.ElementArrayBuffer, access );
				if ( pBuffer == IntPtr.Zero )
				{
					throw new AxiomException( "Index Buffer: Out of memory" );
				}
				unsafe
				{
					// return offset
					retPtr = (IntPtr)( (byte*)pBuffer + offset );
				}

				_lockedToScratch = false;
			}
			isLocked = true;

			return retPtr;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="offset"></param>
		/// <param name="length"></param>
		/// <param name="dest"></param>
		public override void ReadData( int offset, int length, IntPtr dest )
		{
			if ( useShadowBuffer )
			{
				IntPtr srcData = shadowBuffer.Lock( offset, length, BufferLocking.ReadOnly );
				Memory.Copy( srcData, dest, length );
				shadowBuffer.Unlock();
			}
			else
			{
				throw new AxiomException( "Reading hardware buffer is not supported." );
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="offset"></param>
		/// <param name="length"></param>
		/// <param name="src"></param>
		/// <param name="discardWholeBuffer"></param>
		public override void WriteData( int offset, int length, IntPtr src, bool discardWholeBuffer )
		{
			OpenGL.BindBuffer( All.ElementArrayBuffer, _bufferId );
			GLESConfig.GlCheckError( this );
			// Update the shadow buffer
			if ( useShadowBuffer )
			{
				IntPtr destData = shadowBuffer.Lock( offset, length, discardWholeBuffer ? BufferLocking.Discard : BufferLocking.Normal );
				Memory.Copy( src, destData, length );
				shadowBuffer.Unlock();
			}

			if ( offset == 0 && length == sizeInBytes )
			{
				OpenGL.BufferData( All.ElementArrayBuffer, new IntPtr( sizeInBytes ), src, GLESHardwareBufferManager.GetGLUsage( usage ) );
				GLESConfig.GlCheckError( this );
			}
			else
			{
				if ( discardWholeBuffer )
				{
					OpenGL.BufferData( All.ElementArrayBuffer, new IntPtr( sizeInBytes ), IntPtr.Zero, GLESHardwareBufferManager.GetGLUsage( usage ) );
				}
				// Now update the real buffer
				OpenGL.BufferSubData( All.ElementArrayBuffer, new IntPtr( offset ), new IntPtr( length ), src );
				GLESConfig.GlCheckError( this );
			}
		}

		/// <summary>
		/// 
		/// </summary>
		protected override void UpdateFromShadow()
		{
			if ( useShadowBuffer && shadowUpdated && !suppressHardwareUpdate )
			{
				IntPtr srcData = shadowBuffer.Lock( lockStart, lockSize, BufferLocking.ReadOnly );
				OpenGL.BindBuffer( All.ElementArrayBuffer, _bufferId );
				GLESConfig.GlCheckError( this );

				// Update whole buffer if possible, otherwise normal
				if ( lockStart == 0 && lockSize == sizeInBytes )
				{
					OpenGL.BufferData( All.ElementArrayBuffer, new IntPtr( sizeInBytes ), srcData, GLESHardwareBufferManager.GetGLUsage( usage ) );
					GLESConfig.GlCheckError( this );
				}
				else
				{
					OpenGL.BufferSubData( All.ElementArrayBuffer, new IntPtr( lockStart ), new IntPtr( lockSize ), srcData );
					GLESConfig.GlCheckError( this );
				}
				shadowBuffer.Unlock();
				shadowUpdated = false;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="disposeManagedResources"></param>
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					OpenGL.DeleteBuffers( 1, ref _bufferId );
					GLESConfig.GlCheckError( this );
				}
			}

			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );
		}

	}
}