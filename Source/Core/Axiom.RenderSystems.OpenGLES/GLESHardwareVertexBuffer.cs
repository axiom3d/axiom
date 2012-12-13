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
using GLenum = OpenTK.Graphics.ES11.All;
using All = OpenTK.Graphics.ES11.All;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGLES
{
	/// <summary>
	/// </summary>
	public class GLESHardwareVertexBuffer : HardwareVertexBuffer
	{
		private int _bufferID;
		//Scratch buffer handling
		private bool _lockedToScratch;
		private int _scratchOffset, _scratchSize;
		private BufferBase _scratchPtr;
		private bool _scratchUploadOnUnlock;
		
		public GLESHardwareVertexBuffer( HardwareBufferManagerBase manager, VertexDeclaration vertexDeclaration, int numVertices, BufferUsage usage, bool useShadowBuffer )
			: base( manager, vertexDeclaration, numVertices, usage, false, useShadowBuffer )
		{
			if ( !useShadowBuffer )
			{
				throw new AxiomException( "Only supported with shadowBuffer" );
			}
			
			var buffers = new int[ 1 ];
			GL.GenBuffers( 1, buffers );
			GLESConfig.GlCheckError( this );
			this._bufferID = buffers[ 0 ];
			
			if ( this._bufferID == 0 )
			{
				throw new AxiomException( "Cannot create GL ES vertex buffer" );
			}
			
			( Root.Instance.RenderSystem as GLESRenderSystem ).BindGLBuffer( GLenum.ArrayBuffer, this._bufferID );
			GL.BufferData( GLenum.ArrayBuffer, new IntPtr( sizeInBytes ), IntPtr.Zero, GLESHardwareBufferManager.GetGLUsage( usage ) );
			GLESConfig.GlCheckError( this );
		}
		
		protected override void dispose( bool disposeManagedResources )
		{
			GL.DeleteBuffers( 1, ref this._bufferID );
			GLESConfig.GlCheckError( this );
			
			( Root.Instance.RenderSystem as GLESRenderSystem ).DeleteGLBuffer( GLenum.ArrayBuffer, this._bufferID );
			
			base.dispose( disposeManagedResources );
		}
		
		protected override BufferBase LockImpl( int offset, int length, BufferLocking locking )
		{
			if ( IsLocked )
			{
				throw new AxiomException( "Invalid attempt to lock an index buffer that has already been locked." );
			}
			BufferBase retPtr;
			var glBufManager = ( HardwareBufferManager.Instance as GLESHardwareBufferManager );
			
			//Try to use scratch buffers for smaller buffers
			if ( length < glBufManager.MapBufferThreshold )
			{
				retPtr = glBufManager.AllocateScratch( length );
				
				if ( retPtr != null )
				{
					this._lockedToScratch = true;
					this._scratchOffset = offset;
					this._scratchSize = length;
					this._scratchPtr = retPtr;
					this._scratchUploadOnUnlock = ( locking != BufferLocking.ReadOnly );
					
					if ( locking != BufferLocking.Discard )
					{
						//have to read back the data before returning the pointer
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
				GLenum access = GLenum.Zero;
				( Root.Instance.RenderSystem as GLESRenderSystem ).BindGLBuffer( GLenum.ArrayBuffer, this._bufferID );
				
				if ( locking == BufferLocking.Discard )
				{
					//Discard the buffer
					GL.BufferData( GLenum.ArrayBuffer, new IntPtr( sizeInBytes ), IntPtr.Zero, GLESHardwareBufferManager.GetGLUsage( usage ) );
					GLESConfig.GlCheckError( this );
				}
				if ( ( usage & BufferUsage.WriteOnly ) == BufferUsage.WriteOnly )
				{
					access = GLenum.WriteOnlyOes;
				}
				
				var pbuffer = GL.Oes.MapBuffer( GLenum.ArrayBuffer, access );
				GLESConfig.GlCheckError( this );
				
				if ( pbuffer == IntPtr.Zero )
				{
					throw new AxiomException( "Vertex Buffer: Out of memory" );
				}
				
				//return offsetted
				retPtr = BufferBase.Wrap( pbuffer, sizeInBytes ) + offset;
				this._lockedToScratch = false;
			}
			isLocked = true;
			return retPtr;
		}
		
		protected override void UnlockImpl()
		{
			if ( this._lockedToScratch )
			{
				if ( this._scratchUploadOnUnlock )
				{
					//have to write the ata back to vertex buffer
					this.WriteData( this._scratchOffset, this._scratchSize, this._scratchPtr, this._scratchOffset == 0 && this._scratchSize == sizeInBytes );
				}
				
				( HardwareBufferManager.Instance as GLESHardwareBufferManager ).DeallocateScratch( this._scratchPtr );
				
				this._lockedToScratch = false;
			}
			else
			{
				( Root.Instance.RenderSystem as GLESRenderSystem ).BindGLBuffer( GLenum.ArrayBuffer, this._bufferID );
				
				if ( !GL.Oes.UnmapBuffer( GLenum.ArrayBuffer ) )
				{
					throw new AxiomException( "Buffer data corrupted, please reload" );
				}
			}
			isLocked = false;
		}
		
		public override void ReadData( int offset, int length, BufferBase dest )
		{
			if ( useShadowBuffer )
			{
				var srcData = shadowBuffer.Lock( offset, length, BufferLocking.ReadOnly );
				dest = srcData;
				shadowBuffer.Unlock();
			}
			else
			{
				throw new AxiomException( "Read hardware buffer is not supported" );
			}
		}
		
		public override void WriteData( int offset, int length, BufferBase src, bool discardWholeBuffer )
		{
			//Update the shadow buffer
			if ( useShadowBuffer )
			{
				var destData = shadowBuffer.Lock( offset, length, discardWholeBuffer ? BufferLocking.Discard : BufferLocking.Normal );
				src = destData;
				shadowBuffer.Unlock();
			}
			
			if ( offset == 0 && length == sizeInBytes )
			{
				GL.BufferData( GLenum.ArrayBuffer, new IntPtr( sizeInBytes ), src.Pin(), GLESHardwareBufferManager.GetGLUsage( usage ) );
				GLESConfig.GlCheckError( this );
			}
			else
			{
				if ( discardWholeBuffer )
				{
					GL.BufferData( GLenum.ArrayBuffer, new IntPtr( sizeInBytes ), IntPtr.Zero, GLESHardwareBufferManager.GetGLUsage( usage ) );
					GLESConfig.GlCheckError( this );
				}
				GL.BufferSubData( GLenum.ArrayBuffer, new IntPtr( offset ), new IntPtr( length ), src.Pin() );
				GLESConfig.GlCheckError( this );
			}
		}
		
		protected override void UpdateFromShadow()
		{
			if ( useShadowBuffer && shadowUpdated && !suppressHardwareUpdate )
			{
				var srcData = shadowBuffer.Lock( lockStart, lockSize, BufferLocking.ReadOnly );
				
				( Root.Instance.RenderSystem as GLESRenderSystem ).BindGLBuffer( GLenum.ArrayBuffer, this._bufferID );
				
				//Update whole buffer if possible, otherwise normal
				if ( lockStart == 0 && lockSize == sizeInBytes )
				{
					GL.BufferData( GLenum.ArrayBuffer, new IntPtr( sizeInBytes ), srcData.Pin(), GLESHardwareBufferManager.GetGLUsage( usage ) );
					GLESConfig.GlCheckError( this );
				}
				else
				{
					//Ogre FIXME: GPU frequently stalls here - DJR
					GL.BufferSubData( GLenum.ArrayBuffer, new IntPtr( lockStart ), new IntPtr( lockSize ), srcData.Pin() );
					GLESConfig.GlCheckError( this );
				}
				
				shadowBuffer.Unlock();
				shadowUpdated = false;
			}
		}
		
		public int GLBufferID
		{
			get { return this._bufferID; }
		}
	}
}
