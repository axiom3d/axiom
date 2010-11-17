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
using System.Collections.Generic;
using Axiom.Core;
using Axiom.Media;
using Axiom.Graphics;
using OpenTK.Graphics.ES11;
using OpenGL = OpenTK.Graphics.ES11.GL;
#endregion

namespace Axiom.RenderSystems.OpenGLES
{
	/// <summary>
	/// Texture surface
	/// </summary>
	public class GLESTextureBuffer : GLESHardwarePixelBuffer
	{
		protected All _target;
		protected All _faceTarget;
		protected int _textureId;
		protected int _face;
		protected int _level;
		protected bool _softwareMipmap;
		protected List<RenderTexture> _sliceTRT;

		#region Construction and Destruction
		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="target"></param>
		/// <param name="id"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="format"></param>
		/// <param name="face"></param>
		/// <param name="level"></param>
		/// <param name="usage"></param>
		/// <param name="crappyCard"></param>
		/// <param name="writeGamma"></param>
		/// <param name="fsaa"></param>
		public GLESTextureBuffer( string basename, All targetfmt, int id, int width, int height, int format, int face, int level, BufferUsage usage, bool crappyCard, bool writeGamma, int fsaa )
			: base( 0, 0, 0, Media.PixelFormat.Unknown, usage )
		{
			_target = targetfmt;
			_textureId = id;
			_face = face;
			_level = level;
			_softwareMipmap = crappyCard;

			GLESConfig.GlCheckError( this );
			OpenGL.BindTexture( All.Texture2D, _textureId );
			GLESConfig.GlCheckError( this );

			// Get face identifier
			_faceTarget = _target;

			// TODO verify who get this
			Width = width;
			Height = height;
			Depth = 1;

			_glInternalFormat = (All)format;
			Format = GLESPixelUtil.GetClosestAxiomFormat( _glInternalFormat );

			RowPitch = Width;
			SlicePitch = Height * Width;
			sizeInBytes = PixelUtil.GetMemorySize( Width, Height, Depth, Format );

			// Set up a pixel box
			_buffer = new PixelBox( Width, Height, Depth, Format );
			if ( Width == 0 || Height == 0 || Depth == 0 )
			{
				/// We are invalid, do not allocate a buffer
				return;
			}

			// Is this a render target?
			if ( ( (int)Usage & (int)TextureUsage.RenderTarget ) != 0 )
			{
				// Create render target for each slice
				for ( int zoffset = 0; zoffset < Depth; zoffset++ )
				{
					string name = string.Empty;
					name = "rtt/" + this.GetHashCode() + "/" + basename;
					GLESSurfaceDescription target = new GLESSurfaceDescription();
					target.Buffer = this;
					target.ZOffset = zoffset;
					RenderTexture trt = GLESRTTManager.Instance.CreateRenderTexture( name, target, writeGamma, fsaa );
					_sliceTRT.Add( trt );
					Root.Instance.RenderSystem.AttachRenderTarget( _sliceTRT[ zoffset ] );
				}
			}

		}

		#endregion Construction and Destruction

		/// <summary>
		/// 
		/// </summary>
		/// <param name="slice"></param>
		/// <returns></returns>
		public override RenderTexture GetRenderTarget( int slice )
		{
			return base.GetRenderTarget( slice );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="data"></param>
		/// <param name="dest"></param>
		protected override void Upload( PixelBox data, BasicBox dest )
		{
			OpenGL.BindTexture( _target, _textureId );
			GLESConfig.GlCheckError( this );

			if ( PixelUtil.IsCompressed( data.Format ) )
			{
				if ( data.Format != this.Format || !data.IsConsecutive )
				{
					throw new AxiomException( "Compressed images must be consecutive, in the source format" );
				}

				if ( data.Format != Format || !data.IsConsecutive )
					throw new AxiomException( "Compressed images must be consecutive, in the source format." );

				All format = GLESPixelUtil.GetClosestGLInternalFormat( Format );
				// Data must be consecutive and at beginning of buffer as PixelStorei not allowed
				// for compressed formats
				if ( dest.Left == 0 && dest.Top == 0 )
				{
					OpenGL.CompressedTexImage2D( All.Texture2D, _level, format, dest.Width, dest.Height, 0, data.ConsecutiveSize, data.Data );
				}
				else
				{
					OpenGL.CompressedTexSubImage2D( All.Texture2D, _level, dest.Left, dest.Top, dest.Width, dest.Height, format, data.ConsecutiveSize, data.Data );
				}
				GLESConfig.GlCheckError( this );
			}
			else if ( _softwareMipmap )
			{
				if ( data.Width != data.RowPitch )
				{
					//TODO
					throw new AxiomException( "Unsupported Texture format!" );
				}
				if ( data.Height * data.Width != data.SlicePitch )
				{
					//TODO
					throw new AxiomException( "Unsupported Texture format!" );
				}

				OpenGL.PixelStore( All.UnpackAlignment, 1 );
				GLESConfig.GlCheckError( this );
				BuildMipmaps( data );
			}
			else
			{
				if ( data.Width != data.RowPitch )
				{
					//TODO
					throw new AxiomException( "Unsupported Texture format!" );
				}
				if ( data.Height * data.Width != data.SlicePitch )
				{
					//TODO
					throw new AxiomException( "Unsupported Texture format!" );
				}

				if ( ( ( data.Width * PixelUtil.GetNumElemBytes( data.Format ) ) & 3 ) != 0 )
				{
					// Standard alignment of 4 is not right
					OpenGL.PixelStore( All.UnpackAlignment, 1 );
					GLESConfig.GlCheckError( this );
				}
				All form = GLESPixelUtil.GetGLOriginFormat( data.Format );
				All pix = GLESPixelUtil.GetGLOriginDataType( data.Format );
				GLESConfig.GlCheckError( this );
				GL.TexSubImage2D( _faceTarget, _level, dest.Left, dest.Top, dest.Width, dest.Height, GLESPixelUtil.GetGLOriginFormat( data.Format ), GLESPixelUtil.GetGLOriginDataType( data.Format ), data.Data );
				GLESConfig.GlCheckError( this );
			}

			OpenGL.PixelStore( All.UnpackAlignment, 4 );
			GLESConfig.GlCheckError( this );
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="data"></param>
		protected override void Download( PixelBox data )
		{
			throw new AxiomException( "Downloading texture buffers is not supported by OpenGL ES" );
		}
		/// <summary>
		/// Notify TextureBuffer of destruction of render target
		/// </summary>
		/// <param name="data"></param>
		public void ClearRTT( int zoffset )
		{
			Utilities.Contract.Requires( zoffset < _sliceTRT.Count );
			_sliceTRT[ zoffset ] = null;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="attachment"></param>
		/// <param name="zOffset"></param>
		public override void BindToFramebuffer( All attachment, int zOffset )
		{
			Axiom.Utilities.Contract.Requires( zOffset < Depth, "GLESTextureBuffer.BindToFramebuffer, z offset must be smaller then depth" );
			OpenGL.Oes.FramebufferTexture2D( All.FramebufferOes, attachment, _faceTarget, _textureId, _level );
			GLESConfig.GlCheckError( this );
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="p"></param>
		internal void CopyFromFramebuffer( int p )
		{
			OpenGL.BindBuffer( All.Texture2D, _textureId );
			GLESConfig.GlCheckError( this );
			OpenGL.CopyTexSubImage2D( All.Texture2D, _level, 0, 0, 0, 0, Width, Height );
			GLESConfig.GlCheckError( this );
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="src"></param>
		/// <param name="srcBox"></param>
		/// <param name="dstBox"></param>
		public override void Blit( HardwarePixelBuffer src, BasicBox srcBox, BasicBox dstBox )
		{
			GLESTextureBuffer srct = (GLESTextureBuffer)src;
			/// TODO: Check for FBO support first
			/// Destination texture must be 2D
			/// Source texture must be 2D
			if ( ( ( (int)src.Usage & (int)TextureUsage.RenderTarget ) != (int)TextureUsage.RenderTarget ) && ( srct._target == All.Texture2D ) )
			{
				BlitFromTexture( srct, srcBox, dstBox );
			}
			else
			{
				base.Blit( src, srcBox, dstBox );
			}
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="src"></param>
		/// <param name="srcBox"></param>
		/// <param name="dstBox"></param>
		public void BlitFromTexture( GLESTextureBuffer src, BasicBox srcBox, BasicBox dstBox )
		{
			if ( !Root.Instance.RenderSystem.HardwareCapabilities.HasCapability( Capabilities.FrameBufferObjects ) )
			{
				// the following code depends on FBO support, it crashes if FBO is not supported.
				// TODO - write PBUFFER version of this function or a version that doesn't require FBO
				return;
			}

			/// Store reference to FBO manager
			throw new NotImplementedException();
		}
		protected static void BuildMipmaps( PixelBox data )
		{
			int width = 0;
			int height = 0;
			int logW = 0;
			int logH = 0;
			int level = 0;
			PixelBox scaled = data;
			scaled.Data = data.Data;
			scaled.Left = data.Left;
			scaled.Right = data.Right;
			scaled.Top = data.Top;
			scaled.Bottom = data.Bottom;
			scaled.Front = data.Front;
			scaled.Back = data.Back;

			All format = GLESPixelUtil.GetGLOriginFormat( data.Format );
			All dataType = GLESPixelUtil.GetGLOriginDataType( data.Format );
			width = data.Width;
			height = data.Height;

			logW = ComputeLog( width );
			logH = ComputeLog( height );
			level = ( logW > logH ? logW : logH );

			for ( int mip = 0; mip <= level; mip++ )
			{
				format = GLESPixelUtil.GetGLOriginFormat( scaled.Format );
				dataType = GLESPixelUtil.GetGLOriginDataType( scaled.Format );

				OpenGL.TexImage2D( All.Texture2D, mip, (int)format, width, height, 0, format, dataType, scaled.Data );

				GLESConfig.GlCheckError( null );

				if ( mip != 0 )
				{
					scaled.Data = IntPtr.Zero;
				}

				if ( width > 1 )
				{
					width = width / 2;
				}
				if ( height > 1 )
				{
					height = height / 2;
				}

				int sizeInBytes = PixelUtil.GetMemorySize( width, height, 1, data.Format );
				scaled = new PixelBox( width, height, 1, data.Format );
				byte[] dataarr = new byte[ sizeInBytes ];
				scaled.Data = Memory.PinObject( dataarr );
				Image.Scale( data, scaled, ImageFilter.Linear );
			}
		}

	}
}