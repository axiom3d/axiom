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
		/// <param name="src"></param>
		/// <param name="srcBox"></param>
		/// <param name="dstBox"></param>
		public override void Blit( HardwarePixelBuffer src, BasicBox srcBox, BasicBox dstBox )
		{
			base.Blit( src, srcBox, dstBox );
		}

		protected static void BuildMipmaps( PixelBox data )
		{
			throw new NotImplementedException();
		}

		internal void CopyFromFramebuffer( int p )
		{
			throw new NotImplementedException();
		}
	}
}