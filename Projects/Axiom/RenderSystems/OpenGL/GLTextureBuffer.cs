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
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id: GLTextureBuffer.cs 1281 2008-05-10 17:28:57Z borrillis $"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Axiom.Core;
using Axiom.Graphics;
using Axiom.Media;

using Tao.OpenGl;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGL
{
	internal class GLTextureBuffer : GLHardwarePixelBuffer
	{
		#region Fields and Properties

		private readonly int _face;
		private readonly int _faceTarget; // same as _target in case of Gl.GL_TEXTURE_xD, but cubemap face for cubemaps
		private readonly BaseGLSupport _glSupport;
		private readonly int _level;

		private readonly List<RenderTexture> _sliceTRT = new List<RenderTexture>();
		private readonly bool _softwareMipmap; // Use GLU for mip mapping
		private readonly int _target;
		private readonly int _textureId;

		#endregion Fields and Properties

		#region Construction and Destruction

		public GLTextureBuffer( string baseName, int target, int id, int face, int level, BufferUsage usage, bool softwareMipmap, BaseGLSupport glSupport, bool writeGamma, int fsaa )
			: base( 0, 0, 0, PixelFormat.Unknown, usage )
		{
			int value;

			this._glSupport = glSupport;

			this._target = target;
			this._textureId = id;
			this._face = face;
			this._level = level;
			this._softwareMipmap = softwareMipmap;

			Gl.glBindTexture( this._target, this._textureId );

			// Get face identifier
			this._faceTarget = this._target;
			if ( this._target == Gl.GL_TEXTURE_CUBE_MAP )
			{
				this._faceTarget = Gl.GL_TEXTURE_CUBE_MAP_POSITIVE_X + this._face;
			}

			// Get width
			Gl.glGetTexLevelParameteriv( this._faceTarget, this._level, Gl.GL_TEXTURE_WIDTH, out value );
			width = value;

			// Get height
			if ( this._target == Gl.GL_TEXTURE_1D )
			{
				value = 1; // Height always 1 for 1D textures
			}
			else
			{
				Gl.glGetTexLevelParameteriv( this._faceTarget, this._level, Gl.GL_TEXTURE_HEIGHT, out value );
			}
			height = value;

			// Get depth
			if ( this._target != Gl.GL_TEXTURE_3D )
			{
				value = 1; // Depth always 1 for non-3D textures
			}
			else
			{
				Gl.glGetTexLevelParameteriv( this._faceTarget, this._level, Gl.GL_TEXTURE_DEPTH, out value );
			}
			depth = value;

			// Get format
			Gl.glGetTexLevelParameteriv( this._faceTarget, this._level, Gl.GL_TEXTURE_INTERNAL_FORMAT, out value );
			GLFormat = value;
			format = GLPixelUtil.GetClosestPixelFormat( value );

			// Default
			rowPitch = Width;
			slicePitch = Height * Width;
			sizeInBytes = PixelUtil.GetMemorySize( Width, Height, Depth, Format );

			// Set up pixel box
			buffer = new PixelBox( Width, Height, Depth, Format );

			if ( Width == 0 || Height == 0 || Depth == 0 )
			{
				/// We are invalid, do not allocate a buffer
				return;
			}

			// Is this a render target?
			if ( ( (TextureUsage)Usage & TextureUsage.RenderTarget ) == TextureUsage.RenderTarget )
			{
				// Create render target for each slice
				this._sliceTRT.Capacity = Depth;
				for ( int zoffset = 0; zoffset < Depth; ++zoffset )
				{
					String name;
					name = String.Format( "{0}/{1}/{2}/{3}", baseName, face, this._level, zoffset );

					GLSurfaceDesc renderTarget;
					renderTarget.Buffer = this;
					renderTarget.ZOffset = zoffset;
					RenderTexture trt = GLRTTManager.Instance.CreateRenderTexture( name, renderTarget, writeGamma, fsaa );
					this._sliceTRT.Add( trt );
					Root.Instance.RenderSystem.AttachRenderTarget( this._sliceTRT[ zoffset ] );
				}
			}
		}

		#endregion Construction and Destruction

		#region Methods

		#endregion Methods

		#region GLHardwarePixelBuffer Implementation

		public override void BindToFramebuffer( int attachment, int zOffset )
		{
			Debug.Assert( zOffset < Depth );
			switch ( this._target )
			{
				case Gl.GL_TEXTURE_1D:
					Gl.glFramebufferTexture1DEXT( Gl.GL_FRAMEBUFFER_EXT, attachment, this._faceTarget, this._textureId, this._level );
					break;
				case Gl.GL_TEXTURE_2D:
				case Gl.GL_TEXTURE_CUBE_MAP:
					Gl.glFramebufferTexture2DEXT( Gl.GL_FRAMEBUFFER_EXT, attachment, this._faceTarget, this._textureId, this._level );
					break;
				case Gl.GL_TEXTURE_3D:
					Gl.glFramebufferTexture3DEXT( Gl.GL_FRAMEBUFFER_EXT, attachment, this._faceTarget, this._textureId, this._level, zOffset );
					break;
			}
		}

		public void CopyFromFrameBuffer( int zOffset )
		{
			Gl.glBindTexture( this._target, this._textureId );
			switch ( this._target )
			{
				case Gl.GL_TEXTURE_1D:
					Gl.glCopyTexSubImage1D( this._faceTarget, this._level, 0, 0, 0, Width );
					break;
				case Gl.GL_TEXTURE_2D:
				case Gl.GL_TEXTURE_CUBE_MAP:
					Gl.glCopyTexSubImage2D( this._faceTarget, this._level, 0, 0, 0, 0, Width, Height );
					break;
				case Gl.GL_TEXTURE_3D:
					Gl.glCopyTexSubImage3D( this._faceTarget, this._level, 0, 0, zOffset, 0, 0, Width, Height );
					break;
			}
		}

		public override void BlitFromMemory( PixelBox src, BasicBox dstBox )
		{
			/// Fall back to normal GLHardwarePixelBuffer.BlitFromMemory in case
			/// - FBO is not supported
			/// - the source dimensions match the destination ones, in which case no scaling is needed
			if ( !this._glSupport.CheckExtension( "GL_EXT_framebuffer_object" ) || ( src.Width == dstBox.Width && src.Height == dstBox.Height && src.Depth == dstBox.Depth ) )
			{
				base.BlitFromMemory( src, dstBox );
				return;
			}
		}

		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					// Dispose managed resources.
					foreach ( RenderTexture rt in this._sliceTRT )
					{
						rt.Dispose();
					}
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
			}

			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );
		}

		public override RenderTexture GetRenderTarget( int offset )
		{
			return this._sliceTRT[ offset ];
		}

		protected override void download( PixelBox data )
		{
			if ( data.Width != Width || data.Height != Height || data.Depth != Depth )
			{
				throw new ArgumentException( "only download of entire buffer is supported by GL" );
			}

			Gl.glBindTexture( this._target, this._textureId );
			if ( PixelUtil.IsCompressed( data.Format ) )
			{
				if ( data.Format != Format || !data.IsConsecutive )
				{
					throw new ArgumentException( "Compressed images must be consecutive, in the source format" );
				}
				// Data must be consecutive and at beginning of buffer as PixelStorei not allowed
				// for compressed formate
				Gl.glGetCompressedTexImageARB( this._faceTarget, this._level, data.Data.Pin() );
				data.Data.UnPin();
			}
			else
			{
				if ( data.Width != data.RowPitch )
				{
					Gl.glPixelStorei( Gl.GL_PACK_ROW_LENGTH, data.RowPitch );
				}
				if ( data.Height * data.Width != data.SlicePitch )
				{
					Gl.glPixelStorei( Gl.GL_PACK_IMAGE_HEIGHT, ( data.SlicePitch / data.Width ) );
				}
				if ( ( ( data.Width * PixelUtil.GetNumElemBytes( data.Format ) ) & 3 ) != 0 )
				{
					// Standard alignment of 4 is not right
					Gl.glPixelStorei( Gl.GL_PACK_ALIGNMENT, 1 );
				}
				// We can only get the entire texture
				Gl.glGetTexImage( this._faceTarget, this._level, GLPixelUtil.GetGLOriginFormat( data.Format ), GLPixelUtil.GetGLOriginDataType( data.Format ), data.Data.Pin() );
				data.Data.UnPin();
				// Restore defaults
				Gl.glPixelStorei( Gl.GL_PACK_ROW_LENGTH, 0 );
				Gl.glPixelStorei( Gl.GL_PACK_IMAGE_HEIGHT, 0 );
				Gl.glPixelStorei( Gl.GL_PACK_ALIGNMENT, 4 );
			}
		}

		protected override void upload( PixelBox box )
		{
			Gl.glBindTexture( this._target, this._textureId );
			if ( PixelUtil.IsCompressed( box.Format ) )
			{
				if ( box.Format != Format || !box.IsConsecutive )
				{
					throw new ArgumentException( "Compressed images must be consecutive, in the source format" );
				}

				int format = GLPixelUtil.GetClosestGLInternalFormat( Format );
				// Data must be consecutive and at beginning of buffer as PixelStorei not allowed
				// for compressed formats
				switch ( this._target )
				{
					case Gl.GL_TEXTURE_1D:
						Gl.glCompressedTexSubImage1DARB( Gl.GL_TEXTURE_1D, this._level, box.Left, box.Width, format, box.ConsecutiveSize, box.Data.Pin() );
						box.Data.UnPin();
						break;
					case Gl.GL_TEXTURE_2D:
					case Gl.GL_TEXTURE_CUBE_MAP:
						Gl.glCompressedTexSubImage2DARB( this._faceTarget, this._level, box.Left, box.Top, box.Width, box.Height, format, box.ConsecutiveSize, box.Data.Pin() );
						box.Data.UnPin();
						break;
					case Gl.GL_TEXTURE_3D:
						Gl.glCompressedTexSubImage3DARB( Gl.GL_TEXTURE_3D, this._level, box.Left, box.Top, box.Front, box.Width, box.Height, box.Depth, format, box.ConsecutiveSize, box.Data.Pin() );
						box.Data.UnPin();
						break;
				}
			}
			else if ( this._softwareMipmap )
			{
				int internalFormat;
				Gl.glGetTexLevelParameteriv( this._target, this._level, Gl.GL_TEXTURE_INTERNAL_FORMAT, out internalFormat );
				if ( box.Width != box.RowPitch )
				{
					Gl.glPixelStorei( Gl.GL_UNPACK_ROW_LENGTH, box.RowPitch );
				}
				if ( box.Height * box.Width != box.SlicePitch )
				{
					Gl.glPixelStorei( Gl.GL_UNPACK_IMAGE_HEIGHT, ( box.SlicePitch / box.Width ) );
				}
				Gl.glPixelStorei( Gl.GL_UNPACK_ALIGNMENT, 1 );

				switch ( this._target )
				{
					case Gl.GL_TEXTURE_1D:
						Glu.gluBuild1DMipmaps( Gl.GL_TEXTURE_1D, internalFormat, box.Width, GLPixelUtil.GetGLOriginFormat( box.Format ), GLPixelUtil.GetGLOriginDataType( box.Format ), box.Data.Pin() );
						box.Data.UnPin();
						break;
					case Gl.GL_TEXTURE_2D:
					case Gl.GL_TEXTURE_CUBE_MAP:
						Glu.gluBuild2DMipmaps( this._faceTarget, internalFormat, box.Width, box.Height, GLPixelUtil.GetGLOriginFormat( box.Format ), GLPixelUtil.GetGLOriginDataType( box.Format ), box.Data.Pin() );
						box.Data.UnPin();
						break;
					case Gl.GL_TEXTURE_3D:
						/* Requires GLU 1.3 which is harder to come by than cards doing hardware mipmapping
							Most 3D textures don't need mipmaps?
						Gl.gluBuild3DMipmaps(
							Gl.GL_TEXTURE_3D, internalFormat,
							box.getWidth(), box.getHeight(), box.getDepth(),
							GLPixelUtil.getGLOriginFormat(box.format), GLPixelUtil.getGLOriginDataType(box.format),
							box.box);
						*/
						Gl.glTexImage3D( Gl.GL_TEXTURE_3D, 0, internalFormat, box.Width, box.Height, box.Depth, 0, GLPixelUtil.GetGLOriginFormat( box.Format ), GLPixelUtil.GetGLOriginDataType( box.Format ), box.Data.Pin() );
						box.Data.UnPin();
						break;
				}
			}
			else
			{
				if ( box.Width != box.RowPitch )
				{
					Gl.glPixelStorei( Gl.GL_UNPACK_ROW_LENGTH, box.RowPitch );
				}
				if ( box.Height * box.Width != box.SlicePitch )
				{
					Gl.glPixelStorei( Gl.GL_UNPACK_IMAGE_HEIGHT, ( box.SlicePitch / box.Width ) );
				}
				if ( ( ( box.Width * PixelUtil.GetNumElemBytes( box.Format ) ) & 3 ) != 0 )
				{
					// Standard alignment of 4 is not right
					Gl.glPixelStorei( Gl.GL_UNPACK_ALIGNMENT, 1 );
				}
				switch ( this._target )
				{
					case Gl.GL_TEXTURE_1D:
						Gl.glTexSubImage1D( Gl.GL_TEXTURE_1D, this._level, box.Left, box.Width, GLPixelUtil.GetGLOriginFormat( box.Format ), GLPixelUtil.GetGLOriginDataType( box.Format ), box.Data.Pin() );
						box.Data.UnPin();
						break;
					case Gl.GL_TEXTURE_2D:
					case Gl.GL_TEXTURE_CUBE_MAP:
						Gl.glTexSubImage2D( this._faceTarget, this._level, box.Left, box.Top, box.Width, box.Height, GLPixelUtil.GetGLOriginFormat( box.Format ), GLPixelUtil.GetGLOriginDataType( box.Format ), box.Data.Pin() );
						box.Data.UnPin();
						break;
					case Gl.GL_TEXTURE_3D:
						Gl.glTexSubImage3D( Gl.GL_TEXTURE_3D, this._level, box.Left, box.Top, box.Front, box.Width, box.Height, box.Depth, GLPixelUtil.GetGLOriginFormat( box.Format ), GLPixelUtil.GetGLOriginDataType( box.Format ), box.Data.Pin() );
						box.Data.UnPin();
						break;
				}
			}
			// Restore defaults
			Gl.glPixelStorei( Gl.GL_UNPACK_ROW_LENGTH, 0 );
			Gl.glPixelStorei( Gl.GL_UNPACK_IMAGE_HEIGHT, 0 );
			Gl.glPixelStorei( Gl.GL_UNPACK_ALIGNMENT, 4 );
		}

		public override void ClearSliceRTT( int zOffset )
		{
			this._sliceTRT[ zOffset ] = null;
		}

		#endregion GLHardwarePixelBuffer Implementation
	}
}
