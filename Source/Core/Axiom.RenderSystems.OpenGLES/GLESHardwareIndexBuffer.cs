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

using Axiom.Core;
using Axiom.Graphics;

using OpenTK.Graphics.ES11;

using OpenGL = OpenTK.Graphics.ES11.GL;
using OpenGLOES = OpenTK.Graphics.ES11.GL.Oes;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGLES
{
	/// <summary>
	/// </summary>
	public class GLESHardwareIndexBuffer : HardwareIndexBuffer
	{
		private const int MapBufferThreshold = 1024 * 32;
		private int _bufferId = 0;
		private BufferBase _scratchPtr;
		private bool _lockedToScratch;
		private bool _scratchUploadOnUnlock;
		private int _scratchOffset;
		private int _scratchSize;
		
		public int BufferID
		{
			get { return this._bufferId; }
		}
		
		public GLESHardwareIndexBuffer( HardwareBufferManagerBase manager, IndexType type, int numIndices, BufferUsage usage, bool useShadowBuffer )
			: base( manager, type, numIndices, usage, false, useShadowBuffer )
		{
			if ( type == IndexType.Size32 )
			{
				throw new AxiomException( "32 bit hardware buffers are not allowed in OpenGL ES." );
			}
			
			if ( !useShadowBuffer )
			{
				throw new AxiomException( "Only support with shadowBuffer" );
			}
			
			OpenGL.GenBuffers( 1, ref this._bufferId );
			GLESConfig.GlCheckError( this );
			if ( this._bufferId == 0 )
			{
				throw new AxiomException( "Cannot create GL index buffer" );
			}
			
			OpenGL.BindBuffer( All.ElementArrayBuffer, this._bufferId );
			GLESConfig.GlCheckError( this );
			OpenGL.BufferData( All.ElementArrayBuffer, new IntPtr( sizeInBytes ), IntPtr.Zero, GLESHardwareBufferManager.GetGLUsage( usage ) );
			GLESConfig.GlCheckError( this );
		}
		
		/// <summary>
		/// </summary>
		protected override void UnlockImpl()
		{
			if ( this._lockedToScratch )
			{
				if ( this._scratchUploadOnUnlock )
				{
					// have to write the data back to vertex buffer
					this.WriteData( this._scratchOffset, this._scratchSize, this._scratchPtr, this._scratchOffset == 0 && this._scratchSize == sizeInBytes );
				}
				// deallocate from scratch buffer
				( (GLESHardwareBufferManager) HardwareBufferManager.Instance ).DeallocateScratch( this._scratchPtr );
				this._lockedToScratch = false;
			}
			else
			{
				OpenGL.BindBuffer( All.ElementArrayBuffer, this._bufferId );
				GLESConfig.GlCheckError( this );
				if ( !OpenGLOES.UnmapBuffer( All.ElementArrayBuffer ) )
				{
					throw new AxiomException( "Buffer data corrupted, please reload" );
				}
			}
			isLocked = false;
		}
		
		/// <summary>
		/// </summary>
		/// <param name="offset"> </param>
		/// <param name="length"> </param>
		/// <param name="locking"> </param>
		/// <returns> </returns>
		protected override BufferBase LockImpl( int offset, int length, BufferLocking locking )
		{
			All access = 0;
			if ( isLocked )
			{
				throw new AxiomException( "Invalid attempt to lock an index buffer that has already been locked" );
			}
			
			BufferBase retPtr = null;
			if ( length < MapBufferThreshold )
			{
				retPtr = ( (GLESHardwareBufferManager) HardwareBufferManager.Instance ).AllocateScratch( length );
				if ( retPtr != null )
				{
					this._lockedToScratch = true;
					this._scratchOffset = offset;
					this._scratchSize = length;
					this._scratchPtr = retPtr;
					this._scratchUploadOnUnlock = ( locking != BufferLocking.ReadOnly );
					
					if ( locking != BufferLocking.Discard )
					{
						this.ReadData( offset, length, retPtr );
					}
				}
			}
			else
			{
				throw new AxiomException( "Invalid Buffer lockSize" );
			}
			
			if ( retPtr == null )
			{
				OpenGL.BindBuffer( All.ElementArrayBuffer, this._bufferId );
				GLESConfig.GlCheckError( this );
				// Use glMapBuffer
				if ( locking == BufferLocking.Discard )
				{
					OpenGL.BufferData( All.ElementArrayBuffer, new IntPtr( sizeInBytes ), IntPtr.Zero, GLESHardwareBufferManager.GetGLUsage( usage ) );
					GLESConfig.GlCheckError( this );
				}
				if ( ( usage & BufferUsage.WriteOnly ) != 0 )
				{
					access = All.WriteOnlyOes;
				}
				
				IntPtr pBuffer = OpenGLOES.MapBuffer( All.ElementArrayBuffer, access );
				GLESConfig.GlCheckError( this );
				if ( pBuffer == IntPtr.Zero )
				{
					throw new AxiomException( "Index Buffer: Out of memory" );
				}
				unsafe
				{
					// return offset
					retPtr = BufferBase.Wrap( pBuffer, sizeInBytes );
				}
				
				this._lockedToScratch = false;
			}
			isLocked = true;
			
			return retPtr;
		}
		
		/// <summary>
		/// </summary>
		/// <param name="offset"> </param>
		/// <param name="length"> </param>
		/// <param name="dest"> </param>
		public override void ReadData( int offset, int length, BufferBase dest )
		{
			if ( useShadowBuffer )
			{
				var srcData = shadowBuffer.Lock( offset, length, BufferLocking.ReadOnly );
				Memory.Copy( srcData, dest, length );
				shadowBuffer.Unlock();
			}
			else
			{
				throw new AxiomException( "Reading hardware buffer is not supported." );
			}
		}
		
		/// <summary>
		/// </summary>
		/// <param name="offset"> </param>
		/// <param name="length"> </param>
		/// <param name="src"> </param>
		/// <param name="discardWholeBuffer"> </param>
		public override void WriteData( int offset, int length, BufferBase src, bool discardWholeBuffer )
		{
			OpenGL.BindBuffer( All.ElementArrayBuffer, this._bufferId );
			GLESConfig.GlCheckError( this );
			// Update the shadow buffer
			if ( useShadowBuffer )
			{
				var destData = shadowBuffer.Lock( offset, length, discardWholeBuffer ? BufferLocking.Discard : BufferLocking.Normal );
				Memory.Copy( src, destData, length );
				shadowBuffer.Unlock();
			}
			
			var srcPtr = src.Ptr;
			if ( offset == 0 && length == sizeInBytes )
			{
				OpenGL.BufferData( All.ElementArrayBuffer, new IntPtr( sizeInBytes ), ref srcPtr, GLESHardwareBufferManager.GetGLUsage( usage ) );
				GLESConfig.GlCheckError( this );
			}
			else
			{
				if ( discardWholeBuffer )
				{
					OpenGL.BufferData( All.ElementArrayBuffer, new IntPtr( sizeInBytes ), IntPtr.Zero, GLESHardwareBufferManager.GetGLUsage( usage ) );
					GLESConfig.GlCheckError( this );
				}
				// Now update the real buffer
				OpenGL.BufferSubData( All.ElementArrayBuffer, new IntPtr( offset ), new IntPtr( length ), ref srcPtr );
				GLESConfig.GlCheckError( this );
			}
			
			if ( src.Ptr != srcPtr )
			{
				LogManager.Instance.Write( "[GLES2] HardwareIndexBuffer.WriteData - buffer pointer modified by GL.BufferData." );
			}
		}
		
		/// <summary>
		/// </summary>
		protected override void UpdateFromShadow()
		{
			if ( useShadowBuffer && shadowUpdated && !suppressHardwareUpdate )
			{
				var srcData = shadowBuffer.Lock( lockStart, lockSize, BufferLocking.ReadOnly );
				OpenGL.BindBuffer( All.ElementArrayBuffer, this._bufferId );
				GLESConfig.GlCheckError( this );
				
				var srcPtr = new IntPtr( srcData.Ptr );
				// Update whole buffer if possible, otherwise normal
				if ( lockStart == 0 && lockSize == sizeInBytes )
				{
					OpenGL.BufferData( All.ElementArrayBuffer, new IntPtr( sizeInBytes ), srcPtr, GLESHardwareBufferManager.GetGLUsage( usage ) );
					GLESConfig.GlCheckError( this );
				}
				else
				{
					OpenGL.BufferSubData( All.ElementArrayBuffer, new IntPtr( lockStart ), new IntPtr( lockSize ), srcPtr );
					GLESConfig.GlCheckError( this );
				}
				shadowBuffer.Unlock();
				shadowUpdated = false;
			}
		}
		
		/// <summary>
		/// </summary>
		/// <param name="disposeManagedResources"> </param>
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					OpenGL.DeleteBuffers( 1, ref this._bufferId );
					GLESConfig.GlCheckError( this );
				}
			}
			
			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );
		}
	}
}
