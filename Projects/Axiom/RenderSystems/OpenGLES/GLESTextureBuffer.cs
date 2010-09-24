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
using Axiom.Utilities;

using OpenTK.Graphics.ES11;
using OpenGL = OpenTK.Graphics.ES11.GL;
using OpenGLOES = OpenTK.Graphics.ES11.GL.Oes;
#endregion

namespace Axiom.RenderSystems.OpenGLES
{
	/// <summary>
	/// Texture surface
	/// </summary>
	public class GLESTextureBuffer : GLESHardwarePixelBuffer
	{
		#region Fields and Properties

		protected All _target;
		protected All _faceTarget;
		protected int _textureId;
		protected int _face;
		protected int _level;
		protected bool _softwareMipmap;
		protected List<RenderTexture> _sliceTRT;

		#endregion Fields and Properties

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

		#region Methods

		protected static void BuildMipmaps( PixelBox data )
		{
		}

		private void BlitFromTexture( GLESTextureBuffer srct, BasicBox srcBox, BasicBox dstBox )
		{
			throw new NotImplementedException();
		}

		public void CopyFromFramebuffer( int p )
		{
			throw new NotImplementedException();
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

		#endregion Methods

		#region GLESHardwarePixelBuffer Implementation

		public override void BindToFramebuffer( All attachment, int zOffset )
		{
			Contract.Requires( zOffset < Depth );
			OpenGLOES.FramebufferTexture2D( All.FramebufferOes, attachment, _faceTarget, _textureId, _level );
			GLESConfig.GlCheckError( this );
		}

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
		/// <param name="src"></param>
		/// <param name="srcBox"></param>
		/// <param name="dstBox"></param>
		public override void Blit( HardwarePixelBuffer src, BasicBox srcBox, BasicBox dstBox )
		{
			GLESTextureBuffer srct = (GLESTextureBuffer)src;
			/// TODO: Check for FBO support first
			/// Destination texture must be 2D
			/// Source texture must be 2D
			//if ( ( ( (int)src.Usage ) & (int)TextureUsage.RenderTarget ) == 0 && ( srct._target == All.Texture2D ) )
			//{
			//    BlitFromTexture( srct, srcBox, dstBox );
			//}
			//else
			{
				base.Blit( src, srcBox, dstBox );
			}
		}

		protected override void Upload( PixelBox data, BasicBox dest )
		{
			GL.BindTexture( _target, _textureId );
			GLESConfig.GlCheckError( this );

			if ( PixelUtil.IsCompressed( data.Format ) )
			{

				if ( data.Format != Format || !data.IsConsecutive )
					throw new AxiomException( "Compressed images must be consecutive, in the source format." );

				All format = GLESPixelUtil.GetClosestGLInternalFormat( Format );
				// Data must be consecutive and at beginning of buffer as PixelStorei not allowed
				// for compressed formats
				if ( dest.Left == 0 && dest.Top == 0 )
				{
					GL.CompressedTexImage2D( All.Texture2D, _level, format, dest.Width, dest.Height, 0, data.ConsecutiveSize, data.Data );
					GLESConfig.GlCheckError( this );
				}
				else
				{
					GL.CompressedTexSubImage2D( All.Texture2D, _level, dest.Left, dest.Top, dest.Width, dest.Height, format, data.ConsecutiveSize, data.Data );
					GLESConfig.GlCheckError( this );
				}
			}
			else if ( _softwareMipmap )
			{
				if ( data.Width != data.RowPitch )
				{
					// TODO
					throw new AxiomException( "Unsupported texture format." );
				}
				if ( data.Height * data.Width != data.SlicePitch )
				{
					// TODO
					throw new AxiomException( "Unsupported texture format." );
				}
				GL.PixelStore( All.UnpackAlignment, 1 );
				GLESConfig.GlCheckError( this );
				BuildMipmaps( data );
			}
			else
			{
				if ( data.Width != data.RowPitch )
				{
					// TODO
					throw new AxiomException( "Unsupported texture format." );
				}
				if ( data.Height * data.Width != data.SlicePitch )
				{
					// TODO
					throw new AxiomException( "Unsupported texture format." );
				}

				if ( ( ( data.Width * PixelUtil.GetNumElemBytes( data.Format ) ) & 3 ) != 0 )
				{
					// Standard alignment of 4 is not right
					GL.PixelStore( All.UnpackAlignment, 1 );
					GLESConfig.GlCheckError( this );
				}

				//GL.TexSubImage2D( _faceTarget, _level, dest.Left, dest.Top, dest.Width, dest.Height, (All)GLESPixelUtil.GetGLOriginFormat( data.Format ), (All)GLESPixelUtil.GetGLOriginDataType( data.Format ), data.Data );

				int[] pixels = new int[ dest.Width * dest.Height ];
				for ( int y = 0; y < dest.Height; y++ )
				{
					for ( int x = 0; x < dest.Width; x++ )
					{
						pixels[ ( y * dest.Width ) + x ] = ColorEx.Red.ToRGBA();
					}
				}
				GL.TexSubImage2D( All.Texture2D, 0, dest.Top, dest.Left, dest.Width, dest.Height, All.Rgba, All.UnsignedByte, pixels );
				GLESConfig.GlCheckError( this );
			}

			GL.PixelStore( All.UnpackAlignment, 4 );
			GLESConfig.GlCheckError( this );

		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="data"></param>
		protected virtual void Download( PixelBox data )
		{
			throw new AxiomException( "Download texture buffers is not supported by OpenGL ES." );
		}

		#endregion GLESHardwarePixelBuffer Implementation

	}
}