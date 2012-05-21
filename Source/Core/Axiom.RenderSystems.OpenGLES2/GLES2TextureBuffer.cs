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

using Axiom.Core;
using Axiom.Graphics;
using Axiom.Media;

using Glenum = OpenTK.Graphics.ES20.All;
using PixelFormat = Axiom.Media.PixelFormat;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGLES2
{
	internal class GLES2TextureBuffer : GLES2HardwarePixelBuffer
	{
		private readonly All target;
		private readonly All faceTarget;
		private readonly int textureID;
		private int face;
		private readonly int level;
		private readonly bool softwareMipmap;

		private List<RenderTexture> sliceTRT;

		public GLES2TextureBuffer( string baseName, All target, int id, int width, int height, All internalFormat, All format, int face, int level, BufferUsage usage, bool crappyCard, bool writeGamma, int fsaa )
			: base( 0, 0, 0, PixelFormat.Unknown, usage )
		{
			this.target = target;
			this.textureID = id;
			this.face = face;
			this.level = level;
			this.softwareMipmap = crappyCard;

			GLES2Config.GlCheckError( this );
			GL.BindTexture( target, this.textureID );
			GLES2Config.GlCheckError( this );

			//Get face identifier
			this.faceTarget = this.target;
			if ( this.target == All.TextureCubeMap )
			{
				this.faceTarget = All.TextureCubeMapPositiveX + face;
			}
			//Calculate the width and height of the texture at this mip level
			this.width = this.level == 0 ? width : (int) ( width / Math.Utility.Pow( 2, level ) );
			this.height = this.level == 0 ? height : (int) ( height / Math.Utility.Pow( 2, level ) );

			if ( this.width < 1 )
			{
				this.width = 1;
			}
			if ( this.height < 1 )
			{
				this.height = 1;
			}

			//Only 2D is supporte so depth is always 1
			depth = 1;

			GlInternalFormat = internalFormat;
			this.format = GLES2PixelUtil.GetClosestAxiomFormat( internalFormat, format );

			rowPitch = this.width;
			slicePitch = this.height * this.width;
			sizeInBytes = PixelUtil.GetMemorySize( this.width, this.height, depth, this.format );
			//Setup a pixel box
			Buffer = new PixelBox( this.width, this.height, depth, this.format );

			if ( this.width == 0 || this.height == 0 || depth == 0 )
			{
				//We are invalid, do not allocat a buffer
				return;
			}

			if ( ( (TextureUsage) usage & TextureUsage.RenderTarget ) == TextureUsage.RenderTarget )
			{
				//Create render target for each slice

				for ( int zoffset = 0; zoffset < depth; zoffset++ )
				{
					var name = "rtt/ " + GetHashCode() + "/" + baseName;
					var rtarget = new GLES2SurfaceDesc { buffer = this, zoffset = zoffset };
					var trt = GLES2RTTManager.Instance.CreateRenderTexture( name, rtarget, writeGamma, fsaa );
					this.sliceTRT.Add( trt );
					Core.Root.Instance.RenderSystem.AttachRenderTarget( this.sliceTRT[ zoffset ] );
				}
			}
		}

		protected override void dispose( bool disposeManagedResources )
		{
			//todo
			if ( true ) // usage & TextureUsage.RenderTarget == TextureUsage.RenderTarget
			{
				//Delete all render targets that are not yet deleted via clearSlicRTT because 
				//the rendertarget was delted by the user.
				for ( int i = 0; i < this.sliceTRT.Count; i++ )
				{
					Core.Root.Instance.RenderSystem.DestroyRenderTarget( this.sliceTRT[ i ].Name );
				}
			}
			base.dispose( disposeManagedResources );
		}

		private void BuildMipmaps( PixelBox data )
		{
			int width, height, logW, logH, level;

			PixelBox scaled = data;
			scaled.Data = data.Data;
			scaled.Left = data.Left;
			scaled.Right = data.Right;
			scaled.Top = data.Top;
			scaled.Bottom = data.Bottom;
			scaled.Front = data.Front;
			scaled.Back = data.Back;

			width = data.Width;
			height = data.Height;

			logW = (int) System.Math.Log( width );
			logH = (int) System.Math.Log( height );
			level = ( logW > logH ? logW : logH );

			for ( int mip = 0; mip < level; mip++ )
			{
				All glFormat = GLES2PixelUtil.GetGLOriginFormat( scaled.Format );
				All dataType = GLES2PixelUtil.GetGLOriginDataType( scaled.Format );

				GL.TexImage2D( this.faceTarget, mip, (int) glFormat, width, height, 0, glFormat, dataType, scaled.Data.Pin() );
				GLES2Config.GlCheckError( this );

				if ( mip != 0 )
				{
					scaled.Data = null;
				}

				if ( width > 1 )
				{
					width /= 2;
				}
				if ( height > 1 )
				{
					height /= 2;
				}

				int sizeInBytes = PixelUtil.GetMemorySize( width, height, 1, data.Format );
				scaled = new PixelBox( width, height, 1, data.Format );
				scaled.Data = BufferBase.Wrap( new byte[ sizeInBytes ] );
				Image.Scale( data, scaled, ImageFilter.Linear );
			}

			//Delete the scaled data for the last level
			if ( level > 0 )
			{
				scaled.Data = null;
			}
		}

		protected override void Upload( PixelBox data, BasicBox dest )
		{
			GL.BindTexture( this.target, this.textureID );
			GLES2Config.GlCheckError( this );

			if ( PixelUtil.IsCompressed( data.Format ) )
			{
				if ( data.Format != format || !data.IsConsecutive )
				{
					var glFormat = GLES2PixelUtil.GetClosestGLInternalFormat( format );
					//Data must be consecutive and at beginning of buffer as PixelStore is not allowed
					//for compressed formats
					if ( dest.Left == 0 && dest.Top == 0 )
					{
						GL.CompressedTexImage2D( this.faceTarget, this.level, glFormat, dest.Width, dest.Height, 0, data.ConsecutiveSize, data.Data.Pin() );
						GLES2Config.GlCheckError( this );
					}
					else
					{
						GL.CompressedTexSubImage2D( this.faceTarget, this.level, dest.Left, dest.Top, dest.Width, dest.Height, glFormat, data.ConsecutiveSize, data.Data.Pin() );
						GLES2Config.GlCheckError( this );
					}
				}
			}
			else if ( this.softwareMipmap )
			{
				if ( data.Width != data.RowPitch )
				{
					//Ogre TODO
					throw new Core.AxiomException( "Unsupported texture format" );
				}
				if ( data.Height * data.Width != data.SlicePitch )
				{
					//Ogre TODO
					throw new Core.AxiomException( "Unsupported texture format" );
				}

				GL.PixelStore( All.UnpackAlignment, 1 );
				GLES2Config.GlCheckError( this );

				this.BuildMipmaps( data );
			}
			else
			{
				if ( data.Width != data.RowPitch )
				{
					//Ogre TODO
					throw new Core.AxiomException( "Unsupported texture format" );
				}
				if ( data.Height * data.Width != data.SlicePitch )
				{
					//Ogre TODO
					throw new Core.AxiomException( "Unsupported texture format" );
				}

				if ( ( data.Width * PixelUtil.GetNumElemBytes( data.Format ) & 3 ) != 0 )
				{
					//Standard alignment of 4 is not right
					GL.PixelStore( All.UnpackAlignment, 1 );
					GLES2Config.GlCheckError( this );
				}
				var dataPtr = data.Data.Pin();
				GL.TexImage2D( this.faceTarget, this.level, (int) GLES2PixelUtil.GetClosestGLInternalFormat( data.Format ), data.Width, data.Height, 0, GLES2PixelUtil.GetGLOriginFormat( data.Format ), GLES2PixelUtil.GetGLOriginDataType( data.Format ), dataPtr );
				//GL.TexSubImage2D( this.faceTarget, this.level, dest.Left, dest.Top, dest.Width, dest.Height, GLES2PixelUtil.GetGLOriginFormat( data.Format ), GLES2PixelUtil.GetGLOriginDataType( data.Format ), dataPtr );
				data.Data.UnPin();
				GLES2Config.GlCheckError( this );
			}
			GL.PixelStore( All.UnpackAlignment, 4 );
			GLES2Config.GlCheckError( this );
		}

		protected override void Download( PixelBox data )
		{
#if GL_NV_get_tex_image
			if ( data.Width != Width || data.Height != Height || data.Depth != Depth )
			{
				throw new Core.AxiomException( "only download of entire buffer is supported by GL" );
			}

			GL.BindTexture( this.target, this.textureID );
			if ( PixelUtil.IsCompressed( data.Format ) )
			{
				if ( data.Format != format || !data.IsConsecutive )
				{
					throw new Core.AxiomException( "Compressed images must be consecutive, in the source format" );
				}

				GL.GetCompressedTexImageNV(this.faceTarget, this.level, data.Data);
			}
			else
			{
				if ( ( data.Width * PixelUtil.GetNumElemBytes( data.Format ) & 3 ) != 0 )
				{
					//Standard alignment of 4 is not right
					GL.PixelStore( Glenum.PackAlignment, 1 );
				}

				//We can only get the entire texture
				GL.GetTexImageNV( this.faceTarget, this.level, GLES2PixelUtil.GetGLOriginFormat( data.Format ), GLES2PixelUtil.GetGLOriginDataType( data.Format ), data.Data );

				//Restore defaults
				GL.PixelStore( Glenum.PackAlignment, 4 );
			}
#else
			throw new Core.AxiomException( "Downloading texture buffers is not supported by OpenGL ES" );
#endif
		}

		public override void BindToFramebuffer( All attachment, int zoffset )
		{
			GL.FramebufferTexture2D( All.Framebuffer, attachment, this.faceTarget, this.textureID, this.level );
			GLES2Config.GlCheckError( this );
		}

		public override void Blit( HardwarePixelBuffer src, BasicBox srcBox, BasicBox dstBox )
		{
			var srct = ( src as GLES2TextureBuffer );
			//Ogre TODO: Check for FBO support first
			//Destination texture must be 2D or Cube
			//Source texture must be 2D
			//Todo: src.Usage is a BufferUsage, but Ogre uses it as a TextureUsage
			if ( false && ( srct.target == All.Texture2D ) )
			{
				this.BlitFromTexture( srct, srcBox, dstBox );
			}
			else
			{
				base.Blit( src, srcBox, dstBox );
			}
		}

		///<summary>
		///  // Very fast texture-to-texture blitter and hardware bi/trilinear scaling implementation using FBO Destination texture must be 1D, 2D, 3D, or Cube Source texture must be 1D, 2D or 3D Supports compressed formats as both source and destination format, it will use the hardware DXT compressor if available. @author W.J. van der Laan
		///</summary>
		///<param name="src"> </param>
		///<param name="srcBox"> </param>
		///<param name="dstBox"> </param>
		private void BlitFromTexture( GLES2TextureBuffer src, BasicBox srcBox, BasicBox dstBox )
		{
			/*Port notes
			 * Ogre immediately returns void, yet much code is provided below
			 * The remaining code will ported if/when Ogre makes use of it
			 */
			return; //Ogre todo add a shader attach...
		}

		public override void BlitFromMemory( PixelBox src, BasicBox dstBox )
		{
			// Fall back to normal GLHardwarePixelBuffer::blitFromMemory in case 
			// - FBO is not supported
			// - Either source or target is luminance due doesn't looks like supported by hardware
			// - the source dimensions match the destination ones, in which case no scaling is needed
			//Ogre TODO: Check that extension is NOT available
			if ( PixelUtil.IsLuminance( src.Format ) || PixelUtil.IsLuminance( this.format ) || ( src.Width == dstBox.Width && src.Height == dstBox.Height && src.Depth == dstBox.Depth ) )
			{
				base.BlitFromMemory( src, dstBox );
				return;
			}

			if ( !Buffer.Contains( dstBox ) )
			{
				throw new ArgumentOutOfRangeException( "dstBox", "Destination box out of range" );
			}

			//For scoped deletion of conversion buffer

			PixelBox srcPB;
			BufferBase buf;
			//first, convert the srcbox to a OpenGL compatible pixel format
			if ( GLES2PixelUtil.GetGLOriginFormat( src.Format ) == 0 )
			{
				//Conver to buffer intenral format
				buf = BufferBase.Wrap( new byte[ PixelUtil.GetMemorySize( src.Width, src.Height, src.Depth, this.format ) ] );

				srcPB = new PixelBox( src.Width, src.Height, src.Depth, this.format, buf );
				PixelConverter.BulkPixelConversion( src, srcPB );
			}
			else
			{
				//No conversion needed
				srcPB = src;
			}

			//Create temporary texture to store source data
			int id = 0;
			All target = All.Texture2D;
			int width = GLES2PixelUtil.OptionalPO2( src.Width );
			int height = GLES2PixelUtil.OptionalPO2( src.Height );
			All format = GLES2PixelUtil.GetClosestGLInternalFormat( src.Format );
			All datatype = GLES2PixelUtil.GetGLOriginDataType( src.Format );

			//Generate texture name
			GL.GenTextures( 1, ref id );
			GLES2Config.GlCheckError( this );

			//Set texture type
			GL.BindTexture( target, id );
			GLES2Config.GlCheckError( this );

			//Allocate texture memory
			GL.TexImage2D( target, 0, (int) format, width, height, 0, format, datatype, IntPtr.Zero );
			GLES2Config.GlCheckError( this );

			var tex = new GLES2TextureBuffer( string.Empty, target, id, width, height, format, (All) src.Format, 0, 0, BufferUsage.StaticWriteOnly, false, false, 0 );

			//Upload data to 0,0,0 in temprary texture
			var tempTarget = new BasicBox( 0, 0, 0, srcPB.Width, srcPB.Height, srcPB.Depth );
			tex.Upload( srcPB, tempTarget );

			//Blit
			this.BlitFromTexture( tex, tempTarget, dstBox );
			GLES2Config.GlCheckError( this );
		}

		public override RenderTexture GetRenderTarget( int slice )
		{
			return this.sliceTRT[ slice ];
		}
	}
}
