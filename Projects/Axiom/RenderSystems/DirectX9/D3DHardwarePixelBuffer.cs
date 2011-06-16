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
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Root = Axiom.Core.Root;
using Axiom.Graphics;
using Axiom.Media;

using DX = SlimDX;
using D3D = SlimDX.Direct3D9;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9
{
	/// <summary>
	/// 	DirectX implementation of HardwarePixelBuffer
	/// </summary>
	public class D3DHardwarePixelBuffer : HardwarePixelBuffer
	{
		#region Fields

		///<summary>
		///    D3DDevice pointer
		///</summary>
		protected D3D.Device device;
		///<summary>
		///    Surface abstracted by this buffer
		///</summary>
		protected D3D.Surface surface;
		///<summary>
		///    FSAA Surface abstracted by this buffer
		///</summary>
		protected D3D.Surface fsaaSurface;
		///<summary>
		///    Volume abstracted by this buffer
		///</summary>
		protected D3D.Volume volume;
		///<summary>
		///    Temporary surface in main memory if direct locking of mSurface is not possible
		///</summary>
		protected D3D.Surface tempSurface;
		///<summary>
		///    Temporary volume in main memory if direct locking of mVolume is not possible
		///</summary>
		protected D3D.Volume tempVolume;
		///<summary>
		///    Doing Mipmapping?
		///</summary>
		protected bool doMipmapGen;
		///<summary>
		///    Hardware Mipmaps?
		///</summary>
		protected bool HWMipmaps;
		///<summary>
		///    The Mipmap texture?
		///</summary>
		protected D3D.BaseTexture mipTex;
		///<summary>
		///    Render targets
		///</summary>
		protected List<RenderTexture> sliceTRT;

		#endregion Fields

		#region Constructors

		public D3DHardwarePixelBuffer( BufferUsage usage )
			: base( 0, 0, 0, Axiom.Media.PixelFormat.Unknown, usage, false, false )
		{
			device = null;
			surface = null;
			volume = null;
			tempSurface = null;
			tempVolume = null;
			doMipmapGen = false;
			HWMipmaps = false;
			mipTex = null;
			sliceTRT = new List<RenderTexture>();
		}

		#endregion Constructors

		#region Properties

		///<summary>
		///    Accessor for surface
		///</summary>
		public D3D.Surface FSAASurface
		{
			get
			{
				return fsaaSurface;
			}
		}

		///<summary>
		///    Accessor for surface
		///</summary>
		public D3D.Surface Surface
		{
			get
			{
				return surface;
			}
		}

		#endregion Properties

		#region Methods

		///<summary>
		///    Call this to associate a D3D surface with this pixel buffer
		///</summary>
		public void Bind( D3D.Device device, D3D.Surface surface, bool update )
		{
			this.device = device;
			this.surface = surface;

			D3D.SurfaceDescription desc = surface.Description;
			Width = desc.Width;
			Height = desc.Height;
			Depth = 1;
			Format = D3DHelper.ConvertEnum( desc.Format );
			// Default
			RowPitch = Width;
			SlicePitch = Height * Width;
			sizeInBytes = PixelUtil.GetMemorySize( Width, Height, Depth, Format );

			if ( ( (int)usage & (int)TextureUsage.RenderTarget ) != 0 )
				CreateRenderTextures( update );
		}

		///<summary>
		///    Call this to associate a D3D volume with this pixel buffer
		///</summary>
		public void Bind( D3D.Device device, D3D.Volume volume, bool update )
		{
			this.device = device;
			this.volume = volume;

			D3D.VolumeDescription desc = volume.Description;
			Width = desc.Width;
			Height = desc.Height;
			Depth = desc.Depth;
			Format = D3DHelper.ConvertEnum( desc.Format );
			// Default
			RowPitch = Width;
			SlicePitch = Height * Width;
			sizeInBytes = PixelUtil.GetMemorySize( Width, Height, Depth, Format );

			if ( ( (int)usage & (int)TextureUsage.RenderTarget ) != 0 )
				CreateRenderTextures( update );
		}

		///<summary>
		///    Util functions to convert a D3D locked rectangle to a pixel box
		///</summary>
		protected static void FromD3DLock( PixelBox rval, DX.DataRectangle rectangle )
		{
			rval.RowPitch = rectangle.Pitch / PixelUtil.GetNumElemBytes( rval.Format );
			rval.SlicePitch = rval.RowPitch * rval.Height;
			Debug.Assert( ( rectangle.Pitch % PixelUtil.GetNumElemBytes( rval.Format ) ) == 0 );
			rval.Data = rectangle.Data.DataPointer;
		}

		///<summary>
		///    Util functions to convert a D3D LockedBox to a pixel box
		///</summary>
		protected static void FromD3DLock( PixelBox rval, DX.DataBox stream )
		{
			rval.RowPitch = stream.RowPitch / PixelUtil.GetNumElemBytes( rval.Format );
			rval.SlicePitch = stream.SlicePitch / PixelUtil.GetNumElemBytes( rval.Format );
			Debug.Assert( ( stream.RowPitch % PixelUtil.GetNumElemBytes( rval.Format ) ) == 0 );
			Debug.Assert( ( stream.SlicePitch % PixelUtil.GetNumElemBytes( rval.Format ) ) == 0 );
			rval.Data = stream.Data.DataPointer;
		}

		///<summary>
		///    Convert Axiom integer Box to D3D rectangle
		///</summary>
		protected static System.Drawing.Rectangle ToD3DRectangle( BasicBox lockBox )
		{
			Debug.Assert( lockBox.Depth == 1 );
			System.Drawing.Rectangle r = new System.Drawing.Rectangle();
			r.X = lockBox.Left;
			r.Width = lockBox.Width;
			r.Y = lockBox.Top;
			r.Height = lockBox.Height;
			return r;
		}

		///<summary>
		///    Convert Axiom Box to D3D box
		///</summary>
		protected static D3D.Box ToD3DBox( BasicBox lockBox )
		{
			D3D.Box pbox = new D3D.Box();
			pbox.Left = lockBox.Left;
			pbox.Right = lockBox.Right;
			pbox.Top = lockBox.Top;
			pbox.Bottom = lockBox.Bottom;
			pbox.Front = lockBox.Front;
			pbox.Back = lockBox.Back;
			return pbox;
		}

		///<summary>
		///    Convert Axiom PixelBox extent to D3D rectangle
		///</summary>
		protected static System.Drawing.Rectangle ToD3DRectangleExtent( PixelBox lockBox )
		{
			Debug.Assert( lockBox.Depth == 1 );
			System.Drawing.Rectangle r = new System.Drawing.Rectangle();
			r.X = 0;
			r.Width = lockBox.Width;
			r.X = 0;
			r.Height = lockBox.Height;
			return r;
		}

		///<summary>
		///    Convert Axiom PixelBox extent to D3D box
		///</summary>
		protected static D3D.Box ToD3DBoxExtent( PixelBox lockBox )
		{
			D3D.Box pbox = new D3D.Box();
			pbox.Left = 0;
			pbox.Right = lockBox.Width;
			pbox.Top = 0;
			pbox.Bottom = lockBox.Height;
			pbox.Front = 0;
			pbox.Back = lockBox.Depth;
			return pbox;
		}

		///<summary>
		///    Lock a box
		///</summary>
		protected override PixelBox LockImpl( BasicBox lockBox, BufferLocking options )
		{
			// Check for misuse
			if ( ( (int)usage & (int)TextureUsage.RenderTarget ) != 0 )
				throw new Exception( "DirectX does not allow locking of or directly writing to RenderTargets. Use BlitFromMemory if you need the contents." );
			// Set extents and format
			PixelBox rval = new PixelBox( lockBox, Format );
			// Set locking flags according to options
			D3D.LockFlags flags = D3D.LockFlags.None;
			switch ( options )
			{
				case BufferLocking.Discard:
					// D3D only likes D3D.LockFlags.Discard if you created the texture with D3DUSAGE_DYNAMIC
					// debug runtime flags this up, could cause problems on some drivers
					if ( ( usage & BufferUsage.Dynamic ) != 0 )
						flags |= D3D.LockFlags.Discard;
					break;
				case BufferLocking.ReadOnly:
					flags |= D3D.LockFlags.ReadOnly;
					break;
				default:
					break;
			}

			if ( surface != null )
			{
				// Surface
				DX.DataRectangle data = null;
				try
				{
					if ( lockBox.Left == 0 && lockBox.Top == 0 &&
						 lockBox.Right == Width && lockBox.Bottom == Height )
					{
						// Lock whole surface
						data = surface.LockRectangle( flags );
					}
					else
					{
						System.Drawing.Rectangle prect = ToD3DRectangle( lockBox ); // specify range to lock
						data = surface.LockRectangle( prect, flags );
					}
				}
				catch ( Exception e )
				{
					throw new Exception( "Surface locking failed.", e );
				}

				FromD3DLock( rval, data );
			}
			else
			{
				// Volume
				D3D.Box pbox = ToD3DBox( lockBox ); // specify range to lock

				DX.DataBox data = volume.LockBox( pbox, flags );
				FromD3DLock( rval, data );
			}
			return rval;
		}

		///<summary>
		///    Unlock a box
		///</summary>
		protected override void UnlockImpl()
		{
			if ( surface != null )
				// Surface
				surface.UnlockRectangle();
			else
				// Volume
				volume.UnlockBox();

			if ( doMipmapGen )
				GenMipmaps();
		}

		///<summary>
		///    Create (or update) render textures for slices
		///</summary>
		///<param name="update">are we updating an existing texture</param>
		protected void CreateRenderTextures( bool update )
		{
			if ( update )
			{
				Debug.Assert( sliceTRT.Count == Depth );
				foreach ( D3DRenderTexture trt in sliceTRT )
					trt.Rebind( this );
				return;
			}

			DestroyRenderTextures();
			if ( surface == null )
				throw new Exception( "Rendering to 3D slices not supported yet for Direct3D; in " +
									"D3DHardwarePixelBuffer.CreateRenderTexture" );
			// Create render target for each slice
			sliceTRT.Clear();
			Debug.Assert( Depth == 1 );
			for ( int zoffset = 0; zoffset < Depth; ++zoffset )
			{
				string name = "rtt/" + this.ID;
				RenderTexture trt = new D3DRenderTexture( name, this );
				sliceTRT.Add( trt );
				Root.Instance.RenderSystem.AttachRenderTarget( trt );
			}
		}

		///<summary>
		///    Destroy render textures for slices
		///</summary>
		protected void DestroyRenderTextures()
		{
			if ( sliceTRT.Count == 0 )
				return;
			// Delete all render targets that are not yet deleted via _clearSliceRTT
			for ( int i = 0; i < sliceTRT.Count; ++i )
			{
				RenderTexture trt = sliceTRT[ i ];
				if ( trt != null )
					Root.Instance.RenderSystem.DestroyRenderTarget( trt.Name );
			}
			// sliceTRT.Clear();
		}

		///<summary>
		///    Copies a box from another PixelBuffer to a region of the
		///    this PixelBuffer.
		///</summary>
		///<param name="src">Source/dest pixel buffer</param>
		///<param name="srcBox">Image.BasicBox describing the source region in this buffer</param>
		///<param name="dstBox">Image.BasicBox describing the destination region in this buffer</param>
		///<remarks>
		///    The source and destination regions dimensions don't have to match, in which
		///    case scaling is done. This scaling is generally done using a bilinear filter in hardware,
		///    but it is faster to pass the source image in the right dimensions.
		///    Only call this function when both buffers are unlocked.
		///</remarks>
		public override void Blit( HardwarePixelBuffer src, BasicBox srcBox, BasicBox dstBox )
		{
			D3DHardwarePixelBuffer _src = (D3DHardwarePixelBuffer)src;
			if ( surface != null && _src.surface != null )
			{
				// Surface-to-surface
				System.Drawing.Rectangle dsrcRect = ToD3DRectangle( srcBox );
				System.Drawing.Rectangle ddestRect = ToD3DRectangle( dstBox );
				// D3DXLoadSurfaceFromSurface
				D3D.Surface.FromSurface( surface, _src.surface, D3D.Filter.None, 0, dsrcRect, ddestRect );
			}
			else if ( volume != null && _src.volume != null )
			{
				// Volume-to-volume
				D3D.Box dsrcBox = ToD3DBox( srcBox );
				D3D.Box ddestBox = ToD3DBox( dstBox );
				// D3DXLoadVolumeFromVolume
				D3D.Volume.FromVolume( volume, _src.volume, D3D.Filter.None, 0, dsrcBox, ddestBox );
			}
			else
				// Software fallback
				base.Blit( _src, srcBox, dstBox );
		}

		///<summary>
		///    Copies a region from normal memory to a region of this pixelbuffer. The source
		///    image can be in any pixel format supported by Axiom, and in any size.
		///</summary>
		///<param name="src">PixelBox containing the source pixels and format in memory</param>
		///<param name="dstBox">Image.BasicBox describing the destination region in this buffer</param>
		///<remarks>
		///    The source and destination regions dimensions don't have to match, in which
		///    case scaling is done. This scaling is generally done using a bilinear filter in hardware,
		///    but it is faster to pass the source image in the right dimensions.
		///    Only call this function when both  buffers are unlocked.
		///</remarks>
		public override void BlitFromMemory( PixelBox src, BasicBox dstBox )
		{
			// TODO: This currently does way too many copies.  We copy
			// from src to a converted buffer (if needed), then from
			// converted to a byte array, then into the temporary surface,
			// and finally from the temporary surface to the real surface.
			PixelBox converted = src;
			GCHandle bufGCHandle = new GCHandle();
			int bufSize = 0;

			// convert to pixelbuffer's native format if necessary
			if ( D3DHelper.ConvertEnum( src.Format ) == D3D.Format.Unknown )
			{
				bufSize = PixelUtil.GetMemorySize( src.Width, src.Height, src.Depth, Format );
				byte[] newBuffer = new byte[ bufSize ];
				bufGCHandle = GCHandle.Alloc( newBuffer, GCHandleType.Pinned );
				converted = new PixelBox( src.Width, src.Height, src.Depth, Format, bufGCHandle.AddrOfPinnedObject() );
				PixelConverter.BulkPixelConversion( src, converted );
			}

			//int formatBytes = PixelUtil.GetNumElemBytes(converted.Format);
			using ( D3D.Surface tmpSurface = D3D.Surface.CreateOffscreenPlain( device, converted.Width, converted.Height, D3DHelper.ConvertEnum( converted.Format ), D3D.Pool.Scratch ) )
			{ 
				int pitch;
				// Ideally I would be using the Array mechanism here, but that doesn't seem to work
				DX.DataRectangle buf = tmpSurface.LockRectangle( D3D.LockFlags.NoSystemLock );
				{
					buf.Data.Position = 0; // Ensure starting Position
					bufSize = PixelUtil.GetMemorySize( converted.Width, converted.Height, converted.Depth, converted.Format );
					byte[] ugh = new byte[ bufSize ];
					Marshal.Copy( converted.Data, ugh, 0, bufSize );
					buf.Data.Write( ugh, 0, bufSize );
				}
				tmpSurface.UnlockRectangle();

				if ( surface != null )
				{
					// I'm trying to write to surface using the data in converted
					System.Drawing.Rectangle srcRect = ToD3DRectangleExtent( converted );
					System.Drawing.Rectangle destRect = ToD3DRectangle( dstBox );
					D3D.Surface.FromSurface( surface, tmpSurface, D3D.Filter.None, 0, srcRect, destRect );
				}
				else
				{
					throw new NotSupportedException( "BlitFromMemory on Volume Textures not supported." );
					//D3D.Box srcBox = ToD3DBoxExtent( converted );
					//D3D.Box destBox = ToD3DBox( dstBox );
					//D3D.VolumeLoader.FromStream(volume, destBox, converted.Data, converted.RowPitch * converted.SlicePitch * formatBytes, srcBox, Filter.None, 0);
					//D3D.VolumeLoader.FromStream( volume, destBox, buf, srcBox, D3D.Filter.None, 0 );
				}
			}

			// If we allocated a buffer for the temporary conversion, free it here
			// If I used bufPtr to store my temporary data while I converted
			// it, I need to free it here.  This invalidates converted.
			// My data has already been copied to tmpSurface and then to the
			// real surface.
			if ( bufGCHandle.IsAllocated )
				bufGCHandle.Free();

			if ( doMipmapGen )
				GenMipmaps();
		}

		///<summary>
		///    Copies a region of this pixelbuffer to normal memory.
		///</summary>
		///<param name="srcBox">BasicBox describing the source region of this buffer</param>
		///<param name="dst">PixelBox describing the destination pixels and format in memory</param>
		///<remarks>
		///    The source and destination regions don't have to match, in which
		///    case scaling is done.
		///    Only call this function when the buffer is unlocked.
		///</remarks>
		public override void BlitToMemory( BasicBox srcBox, PixelBox dst )
		{
			// Decide on pixel format of temp surface
			PixelFormat tmpFormat = Format;
			if ( D3DHelper.ConvertEnum( dst.Format ) == D3D.Format.Unknown )
				tmpFormat = dst.Format;
			if ( surface != null )
			{
				Debug.Assert( srcBox.Depth == 1 && dst.Depth == 1 );
				// Create temp texture
				D3D.Texture tmp =
					new D3D.Texture( device, dst.Width, dst.Height,
									1, // 1 mip level ie topmost, generate no mipmaps
									0, D3DHelper.ConvertEnum( tmpFormat ),
									D3D.Pool.Scratch );
				D3D.Surface subSurface = tmp.GetSurfaceLevel( 0 );
				// Copy texture to this temp surface
				System.Drawing.Rectangle destRect, srcRect;
				srcRect = ToD3DRectangle( srcBox );
				destRect = ToD3DRectangleExtent( dst );

				D3D.Surface.FromSurface( subSurface, surface, D3D.Filter.None, 0, srcRect, destRect );

				// Lock temp surface and copy it to memory
				int pitch; // Filled in by D3D
				DX.DataRectangle data = subSurface.LockRectangle( D3D.LockFlags.ReadOnly );
				// Copy it
				PixelBox locked = new PixelBox( dst.Width, dst.Height, dst.Depth, tmpFormat );
				FromD3DLock( locked, data );
				PixelConverter.BulkPixelConversion( locked, dst );
				subSurface.UnlockRectangle();
				// Release temporary surface and texture
				subSurface.Dispose();
				tmp.Dispose();
			}
			else
			{
				// Create temp texture
				D3D.VolumeTexture tmp =
					new D3D.VolumeTexture( device, dst.Width, dst.Height, dst.Depth,
										   0, D3D.Usage.None,
										   D3DHelper.ConvertEnum( tmpFormat ),
										   D3D.Pool.Scratch );
				D3D.Volume subVolume = tmp.GetVolumeLevel( 0 );
				// Volume
				D3D.Box ddestBox = ToD3DBoxExtent( dst );
				D3D.Box dsrcBox = ToD3DBox( srcBox );

				D3D.Volume.FromVolume( subVolume, volume, D3D.Filter.None, 0, dsrcBox, ddestBox );
				// Lock temp surface and copy it to memory
				//D3D.LockedBox lbox; // Filled in by D3D
				DX.DataBox data = subVolume.LockBox( D3D.LockFlags.ReadOnly );

				// Copy it
				PixelBox locked = new PixelBox( dst.Width, dst.Height, dst.Depth, tmpFormat );
				FromD3DLock( locked, data );
				PixelConverter.BulkPixelConversion( locked, dst );
				subVolume.UnlockBox();
				// Release temporary surface and texture
				subVolume.Dispose();
				tmp.Dispose();
			}
		}

		///<summary>
		///    Internal function to update mipmaps on update of level 0
		///</summary>
		public void GenMipmaps()
		{
			Debug.Assert( mipTex != null );
			// Mipmapping
			if ( HWMipmaps )
			{
				// Hardware mipmaps
				mipTex.GenerateMipSublevels();
			}
			else
			{
				// Software mipmaps
				mipTex.FilterTexture( 0, D3D.Filter.Box );
			}
		}

		///<summary>
		///    Function to set mipmap generation
		///</summary>
		public void SetMipmapping( bool doMipmapGen, bool HWMipmaps, D3D.BaseTexture mipTex )
		{
			this.doMipmapGen = doMipmapGen;
			this.HWMipmaps = HWMipmaps;
			this.mipTex = mipTex;
		}

		///<summary>
		///    Get rendertarget for z slice
		///</summary>
		public override RenderTexture GetRenderTarget( int zoffset )
		{
			Debug.Assert( ( (int)usage & (int)TextureUsage.RenderTarget ) != 0 );
			Debug.Assert( zoffset < Depth );
			return sliceTRT[ zoffset ];
		}

		///<summary>
		///    Notify TextureBuffer of destruction of render target
		///</summary>
		public override void ClearSliceRTT( int zoffset )
		{
			sliceTRT[ zoffset ] = null;
		}

		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
				}
				DestroyRenderTextures();
			}

			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );
		}

		#endregion Methods

        #region Locking

        internal static readonly object DeviceAccessMutex = new object();

	    public static void LockDeviceAccess()
	    {
	        Monitor.Enter( DeviceAccessMutex );
	    }

        #endregion

	    public static void UnlockDeviceAccess()
	    {
            Monitor.Exit(DeviceAccessMutex);
	    }
	}
}