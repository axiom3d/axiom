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

using Axiom.Core;
using Axiom.Graphics;
using Axiom.Media;

using OpenTK.Graphics.ES20;

using Glenum = OpenTK.Graphics.ES20.All;
using PixelFormat = Axiom.Media.PixelFormat;

#endregion Namespace Declarations
			
namespace Axiom.RenderSystems.OpenGLES2
{
	public class GLES2HardwarePixelBuffer : HardwarePixelBuffer
	{
		protected PixelBox Buffer;
		protected Glenum GlInternalFormat;
		protected BufferLocking CurrentLockOptions;

		public GLES2HardwarePixelBuffer( int width, int height, int depth, PixelFormat format, BufferUsage usage )
			: base( width, height, depth, format, usage, false, false )
		{
			this.Buffer = new PixelBox( width, height, depth, format );
			this.GlInternalFormat = Glenum.None;
		}

		protected override void dispose( bool disposeManagedResources )
		{
			if (!IsDisposed)
				if ( disposeManagedResources )
				{
					//Force free buffer
					this.Buffer.SafeDispose();
					this.Buffer.Data = null;
				}
			base.dispose( disposeManagedResources );
		}

		private void AllocateBuffer()
		{
			if ( this.Buffer.Data != null )
			{
				//already allocated
				return;
			}

			this.Buffer.Data = BufferBase.Wrap( new byte[ sizeInBytes ] );
		}

		private void FreeBuffer()
		{
			//Free buffer if we're STATIC to save meory
			if ( ( usage & BufferUsage.Static ) == BufferUsage.Static )
			{
				this.Buffer.Data.SafeDispose();
				this.Buffer.Data = null;
			}
		}

		protected virtual void Upload( PixelBox data, BasicBox dest )
		{
			//must be overriden
			throw new Core.AxiomException( "Upload not possible for this pixelbuffer type" );
		}

		protected virtual void Download( PixelBox data )
		{
			//must be overriden
			throw new Core.AxiomException( "Download not possible for this pixelbuffer type" );
		}

		public virtual void BindToFramebuffer( Glenum attachment, int zoffset )
		{
			//must be overriden
			throw new Core.AxiomException( "Framebuffer bind not possible for this pixelbuffer type" );
		}

		protected override Media.PixelBox LockImpl( Media.BasicBox lockBox, BufferLocking options )
		{
			this.AllocateBuffer();
			if ( options != BufferLocking.Discard && ( usage & BufferUsage.WriteOnly ) == 0  )
			{
				//Downoad the old contents of the texture
				this.Download( this.Buffer );
			}
			this.CurrentLockOptions = options;
			lockedBox = lockBox;
			return this.Buffer.GetSubVolume( lockBox );
		}

		protected override void UnlockImpl()
		{
			if ( this.CurrentLockOptions != BufferLocking.ReadOnly )
			{
				//From buffer to card, only upload if was locked for writing
				this.Upload( currentLock, lockedBox );
			}
			this.FreeBuffer();
		}

		public override void BlitFromMemory( Media.PixelBox src, Media.BasicBox dstBox )
		{
			if ( this.Buffer.Contains( dstBox ) == false )
			{
				throw new ArgumentOutOfRangeException( "dstBox", "Destination box out of range" );
			}

			PixelBox scaled;

			if ( src.Width != dstBox.Width || src.Height != dstBox.Height || src.Depth != dstBox.Depth )
			{
				//Scale to destination size
				//This also does pixel format conversion if needed
				this.AllocateBuffer();
				scaled = this.Buffer.GetSubVolume( dstBox );
				Image.Scale( src, scaled, ImageFilter.Bilinear );
			}
			else if ( ( src.Format != format ) || ( ( GLES2PixelUtil.GetGLOriginFormat( src.Format ) == 0 ) && ( src.Format != PixelFormat.R8G8B8 ) ) )
			{
				//Extents match, but format is not accepted as valid source format for GL
				//do conversion in temporary buffer
				this.AllocateBuffer();
				scaled = this.Buffer.GetSubVolume( dstBox );
				PixelConverter.BulkPixelConversion( src, scaled );
				if ( src.Format == PixelFormat.A4R4G4B4 )
				{
					// ARGB->BGRA
					GLES2PixelUtil.ConvertToGLFormat( ref scaled, ref scaled );
				}
			}
			else
			{
				this.AllocateBuffer();
				scaled = src.Clone();

				if ( src.Format == PixelFormat.R8G8B8 )
				{
					scaled.Format = PixelFormat.B8G8R8;
					PixelConverter.BulkPixelConversion( src, scaled );
				}
			}

			this.Upload( scaled, dstBox );
			this.FreeBuffer();
		}

		public override void BlitToMemory( Media.BasicBox srcBox, Media.PixelBox dst )
		{
			if ( !this.Buffer.Contains( srcBox ) )
			{
				throw new ArgumentOutOfRangeException( "srcBox","source box out of range." );
			}

			if ( srcBox.Left == 0 && srcBox.Right == width && srcBox.Top == 0 && srcBox.Bottom == height && srcBox.Front == 0  && srcBox.Back == depth && dst.Width == width && dst.Height == height && dst.Depth == depth && GLES2PixelUtil.GetGLOriginFormat( dst.Format ) != 0 )
			{
				//The direct case: the user wants the entire texture in a format supported by GL
				//so we don't need an intermediate buffer
				this.Download( dst );
			}
			else
			{
				//Use buffer for intermediate copy
				this.AllocateBuffer();
				//Download entire buffer
				this.Download( this.Buffer );
				if ( srcBox.Width != dst.Width || srcBox.Height != dst.Height || srcBox.Depth != dst.Depth )
				{
					//We need scaling
					Image.Scale( this.Buffer.GetSubVolume( srcBox ), dst, ImageFilter.Bilinear );
				}
				else
				{
					//Just copy the bit that we need
					PixelConverter.BulkPixelConversion( Buffer.GetSubVolume( srcBox) , dst );
				}

				this.FreeBuffer();
			}
		}

		public Glenum GLFormat
		{
			get { return this.GlInternalFormat; }
		}
	}

	internal class GLES2TextureBuffer : GLES2HardwarePixelBuffer
	{
		private readonly Glenum target;
		private readonly Glenum faceTarget;
		private readonly int textureID;
		private int face;
		private readonly int level;
		private readonly bool softwareMipmap;

		private List<RenderTexture> sliceTRT;

		public GLES2TextureBuffer( string baseName, Glenum target, int id, int width, int height, Glenum internalFormat, Glenum format, int face, int level, BufferUsage usage, bool crappyCard, bool writeGamma, int fsaa )
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
			if ( this.target == Glenum.TextureCubeMap )
			{
				this.faceTarget = Glenum.TextureCubeMapPositiveX + face;
			}
			//Calculate the width and height of the texture at this mip level
			this.width = this.level == 0 ? width : (int)(width / Math.Utility.Pow( 2, level ));
			this.height = this.level == 0 ? height : (int)(height / Math.Utility.Pow( 2, level ));

			if ( this.width < 1 )
			{
				this.width = 1;
			}
			if ( this.height < 1 )
			{
				this.height = 1;
			}

			//Only 2D is supporte so depth is always 1
			this.depth = 1;

			this.GlInternalFormat = internalFormat;
			this.format = GLES2PixelUtil.GetClosestAxiomFormat( internalFormat, format );

			rowPitch = this.width;
			slicePitch = this.height * this.width;
			sizeInBytes = PixelUtil.GetMemorySize( this.width, this.height, this.depth, this.format );
			//Setup a pixel box
			Buffer = new PixelBox( this.width, this.height, this.depth, this.format );

			if ( this.width == 0 || this.height == 0 || this.depth == 0 )
			{
				//We are invalid, do not allocat a buffer
				return;
			}

			if ( ( (TextureUsage)usage & TextureUsage.RenderTarget) == TextureUsage.RenderTarget )
			{
				//Create render target for each slice

				for ( int zoffset = 0; zoffset < this.depth; zoffset++ )
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
				Glenum glFormat = GLES2PixelUtil.GetGLOriginFormat( scaled.Format );
				Glenum dataType = GLES2PixelUtil.GetGLOriginDataType( scaled.Format );

				GL.TexImage2D( this.faceTarget, mip, (int)glFormat, width, height, 0, glFormat, dataType, scaled.Data.Pin() );

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
				scaled.Data = BufferBase.Wrap( new byte[sizeInBytes] );
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

			if ( PixelUtil.IsCompressed( data.Format ) )
			{
				if ( data.Format != format || !data.IsConsecutive )
				{
					throw new Core.AxiomException( "Compressed images must be consecutive, in the source format" );
					/*
					Glenum format = GLES2PixelUtil.GetClosestGLInternalFormat( this.format );
					//Data must be consecutive and at beginning of buffer as PixelStore is not allowed
					//for compressed formats
					if ( dest.Left == 0 && dest.Top == 0 )
					{
						GL.CompressedTexImage2D( this.faceTarget, this.level, format, dest.Width, dest.Height, 0, data.ConsecutiveSize, data.Data );
					}
					else
					{
						GL.CompressedTexImage2D( this.faceTarget, this.level, dest.Left, dest.Top, dest.Width, dest.Height, format, data.ConsecutiveSize, data.Data );
					}
					*/
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

				GL.PixelStore( Glenum.UnpackAlignment, 1 );
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
					GL.PixelStore( Glenum.UnpackAlignment, 1 );
				}
			}

			GL.TexSubImage2D( this.faceTarget, this.level, dest.Left, dest.Top, dest.Width, dest.Height, GLES2PixelUtil.GetGLOriginFormat( data.Format ), GLES2PixelUtil.GetGLOriginFormat( data.Format ), data.Data.Pin() );
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

		public override void BindToFramebuffer( Glenum attachment, int zoffset )
		{
			GL.FramebufferTexture2D( Glenum.Framebuffer, attachment, this.faceTarget, this.textureID, this.level );
		}

		public override void Blit( HardwarePixelBuffer src, BasicBox srcBox, BasicBox dstBox )
		{
			var srct = ( src as GLES2TextureBuffer );
			//Ogre TODO: Check for FBO support first
			//Destination texture must be 2D or Cube
			//Source texture must be 2D
			//Todo: src.Usage is a BufferUsage, but Ogre uses it as a TextureUsage
			if ( false && ( srct.target == Glenum.Texture2D ) )
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
				throw new ArgumentOutOfRangeException( "dstBox","Destination box out of range" );
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
			Glenum target = Glenum.Texture2D;
			int width = GLES2PixelUtil.OptionalPO2( src.Width );
			int height = GLES2PixelUtil.OptionalPO2( src.Height );
			Glenum format = GLES2PixelUtil.GetClosestGLInternalFormat( src.Format );
			Glenum datatype = GLES2PixelUtil.GetGLOriginDataType( src.Format );

			//Generate texture name
			GL.GenTextures( 1, ref id );
			GLES2Config.GlCheckError( this );

			//Set texture type
			GL.BindTexture( target, id );
			GLES2Config.GlCheckError( this );

			//Allocate texture memory
			GL.TexImage2D( target, 0, (int) format, width, height, 0, format, datatype, IntPtr.Zero );
			GLES2Config.GlCheckError( this );

			var tex = new GLES2TextureBuffer( string.Empty, target, id, width, height, format, (Glenum)src.Format, 0, 0, BufferUsage.StaticWriteOnly, false, false, 0 );

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

	public class GLES2RenderBuffer : GLES2HardwarePixelBuffer
	{
		private int renderBufferID;

		public GLES2RenderBuffer( Glenum format, int width, int height, int numSamples )
			: base( width, height, 1, GLES2PixelUtil.GetClosestAxiomFormat( format, (Glenum)PixelFormat.A8R8G8B8 ), BufferUsage.WriteOnly )
		{
			GlInternalFormat = format;
			//Genearte renderbuffer
			GL.GenRenderbuffers( 1, ref this.renderBufferID );
			//Bind it to FBO
			GL.BindRenderbuffer( Glenum.Renderbuffer, this.renderBufferID );

			//Allocate storage for depth buffer
			if ( numSamples > 0 ) {}
			else
			{
				GL.RenderbufferStorage( Glenum.Renderbuffer, format, width, height );
			}
		}

		protected override void dispose( bool disposeManagedResources )
		{
			GL.DeleteRenderbuffers( 1, ref this.renderBufferID );
			base.dispose( disposeManagedResources );
		}

		public override void BindToFramebuffer( Glenum attachment, int zoffset )
		{
			GL.FramebufferRenderbuffer( Glenum.Framebuffer, attachment, Glenum.Renderbuffer, this.renderBufferID );
		}
	}
}
