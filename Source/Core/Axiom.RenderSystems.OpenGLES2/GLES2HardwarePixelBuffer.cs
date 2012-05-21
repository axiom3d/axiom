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
}
