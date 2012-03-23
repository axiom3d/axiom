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
using System.Diagnostics;
using System.Linq;

using Axiom.Core;
using Axiom.CrossPlatform;
using Axiom.Graphics;
using Axiom.Media;

using D3D9 = SharpDX.Direct3D9;
using DX = SharpDX;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9
{
	/// <summary>
	/// DirectX9 implementation of HardwarePixelBuffer
	/// </summary>
	public class D3D9HardwarePixelBuffer : HardwarePixelBuffer
	{
		#region Nested Types

		[OgreVersion( 1, 7, 2 )]
		protected class BufferResources : DisposableObject
		{
			/// <summary>
			/// Surface abstracted by this buffer
			/// </summary>
			public D3D9.Surface Surface;

			/// <summary>
			/// AA Surface abstracted by this buffer
			/// </summary>
			public D3D9.Surface FsaaSurface;

			/// <summary>
			/// Volume abstracted by this buffer
			/// </summary>
			public D3D9.Volume Volume;

			/// <summary>
			/// Temporary surface in main memory if direct locking of mSurface is not possible
			/// </summary>
			public D3D9.Surface TempSurface;

			/// <summary>
			/// Temporary volume in main memory if direct locking of mVolume is not possible
			/// </summary>
			public D3D9.Volume TempVolume;

			/// <summary>
			/// Mip map texture.
			/// </summary>
			public D3D9.BaseTexture MipTex;

			protected override void dispose( bool disposeManagedResources )
			{
				if ( !this.IsDisposed )
				{
					if ( disposeManagedResources )
					{
						this.Surface.SafeDispose();
						this.Surface = null;

						this.Volume.SafeDispose();
						this.Volume = null;
					}
				}

				base.dispose( disposeManagedResources );
			}
		};

		#endregion Nested Types

		#region Fields

		/// <summary>
		/// Map between device to buffer resources.
		/// </summary>
		protected Dictionary<D3D9.Device, BufferResources> mapDeviceToBufferResources = new Dictionary<D3D9.Device, BufferResources>();

		/// <summary>
		/// Doing Mipmapping?
		/// </summary>
		protected bool doMipmapGen;

		/// <summary>
		/// Hardware Mipmaps?
		/// </summary>
		protected bool HWMipmaps;

		/// <summary>
		/// Render target
		/// </summary>
		protected RenderTexture renderTexture;

		/// <summary>
		/// The owner texture if exists.
		/// </summary>
		protected D3D9Texture ownerTexture;

		/// <summary>
		/// The current lock flags of this surface.
		/// </summary>
		protected D3D9.LockFlags lockFlags;

#if AXIOM_THREAD_SUPPORT
		private static readonly object deviceLockMutex = new object();
#endif

		#endregion Fields

		[OgreVersion( 1, 7, 2 )]
		public D3D9HardwarePixelBuffer( BufferUsage usage, D3D9Texture ownerTexture )
			: base( 0, 0, 0, Media.PixelFormat.Unknown, usage, false, false )
		{
			this.ownerTexture = ownerTexture;
		}

		[OgreVersion( 1, 7, 2, "~D3D9HardwarePixelBuffer" )]
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !this.IsDisposed )
			{
				if ( disposeManagedResources )
				{
					//Entering critical section
					LockDeviceAccess();

					DestroyRenderTexture();

					foreach ( var it in mapDeviceToBufferResources.Values )
					{
						it.SafeDispose();
					}

					mapDeviceToBufferResources.Clear();

					//Leaving critical section
					UnlockDeviceAccess();
				}
			}

			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );
		}

		#region Methods

		///<summary>
		/// Call this to associate a D3D surface with this pixel buffer
		///</summary>
		[OgreVersion( 1, 7, 2 )]
		public void Bind( D3D9.Device dev, D3D9.Surface surface, D3D9.Surface fsaaSurface, bool writeGamma, int fsaa, string srcName, D3D9.BaseTexture mipTex )
		{
			//Entering critical section
			LockDeviceAccess();

			var bufferResources = GetBufferResources( dev );
			var isNewBuffer = false;

			if ( bufferResources == null )
			{
				bufferResources = new BufferResources();
				mapDeviceToBufferResources.Add( dev, bufferResources );
				isNewBuffer = true;
			}

			bufferResources.MipTex = mipTex;
			bufferResources.Surface = surface;
			bufferResources.FsaaSurface = fsaaSurface;

			var desc = surface.Description;
			width = desc.Width;
			height = desc.Height;
			depth = 1;
			format = D3D9Helper.ConvertEnum( desc.Format );
			// Default
			rowPitch = Width;
			slicePitch = Height * Width;
			sizeInBytes = PixelUtil.GetMemorySize( Width, Height, Depth, Format );

			if ( ( (int)usage & (int)TextureUsage.RenderTarget ) != 0 )
			{
				UpdateRenderTexture( writeGamma, fsaa, srcName );
			}

			if ( isNewBuffer && ownerTexture.IsManuallyLoaded )
			{
				foreach ( var it in mapDeviceToBufferResources )
				{
					if ( it.Value != bufferResources && it.Value.Surface != null && it.Key.TestCooperativeLevel().Success && dev.TestCooperativeLevel().Success )
					{
						var fullBufferBox = new BasicBox( 0, 0, 0, Width, Height, Depth );
						var dstBox = new PixelBox( fullBufferBox, Format );

						var data = new byte[ sizeInBytes ];
						using ( var d = BufferBase.Wrap( data ) )
						{
							dstBox.Data = d;
							BlitToMemory( fullBufferBox, dstBox, it.Value, it.Key );
							BlitFromMemory( dstBox, fullBufferBox, bufferResources );
							Array.Clear( data, 0, sizeInBytes );
						}
						break;
					}
				}
			}

			//Leaving critical section
			UnlockDeviceAccess();
		}

		///<summary>
		/// Call this to associate a D3D volume with this pixel buffer
		///</summary>
		[OgreVersion( 1, 7, 2 )]
		public void Bind( D3D9.Device dev, D3D9.Volume volume, D3D9.BaseTexture mipTex )
		{
			//Entering critical section
			LockDeviceAccess();

			var bufferResources = GetBufferResources( dev );
			var isNewBuffer = false;

			if ( bufferResources == null )
			{
				bufferResources = new BufferResources();
				mapDeviceToBufferResources.Add( dev, bufferResources );
				isNewBuffer = true;
			}

			bufferResources.MipTex = mipTex;
			bufferResources.Volume = volume;

			var desc = volume.Description;
			width = desc.Width;
			height = desc.Height;
			depth = desc.Depth;
			format = D3D9Helper.ConvertEnum( desc.Format );
			// Default
			rowPitch = Width;
			slicePitch = Height * Width;
			sizeInBytes = PixelUtil.GetMemorySize( Width, Height, Depth, Format );

			if ( isNewBuffer && ownerTexture.IsManuallyLoaded )
			{
				foreach ( var it in mapDeviceToBufferResources )
				{
					if ( it.Value != bufferResources && it.Value.Volume != null && it.Key.TestCooperativeLevel().Success && dev.TestCooperativeLevel().Success )
					{
						var fullBufferBox = new BasicBox( 0, 0, 0, Width, Height, Depth );
						var dstBox = new PixelBox( fullBufferBox, Format );

						var data = new byte[ sizeInBytes ];
						using ( var d = BufferBase.Wrap( data ) )
						{
							dstBox.Data = d;
							BlitToMemory( fullBufferBox, dstBox, it.Value, it.Key );
							BlitFromMemory( dstBox, fullBufferBox, bufferResources );
							Array.Clear( data, 0, sizeInBytes );
						}
						break;
					}
				}
			}

			//Leaving critical section
			UnlockDeviceAccess();
		}

		[OgreVersion( 1, 7, 2 )]
		protected BufferResources GetBufferResources( D3D9.Device d3d9Device )
		{
			if ( mapDeviceToBufferResources.ContainsKey( d3d9Device ) )
			{
				return mapDeviceToBufferResources[ d3d9Device ];
			}

			return null;
		}

		/// <summary>
		/// Destroy resources associated with the given device.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void DestroyBufferResources( D3D9.Device d3d9Device )
		{
			//Entering critical section
			LockDeviceAccess();

			if ( mapDeviceToBufferResources.ContainsKey( d3d9Device ) )
			{
				mapDeviceToBufferResources[ d3d9Device ].SafeDispose();
				mapDeviceToBufferResources.Remove( d3d9Device );
			}

			//Leaving critical section
			UnlockDeviceAccess();
		}

		/// <summary>
		/// Called when device state is changing. Access to any device should be locked.
		/// Relevant for multi thread application.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public static void LockDeviceAccess()
		{
#if AXIOM_THREAD_SUPPORT
			if ( Configuration.Config.AxiomThreadLevel == 1 )
				System.Threading.Monitor.Enter( deviceLockMutex );
#endif
		}

		/// <summary>
		/// Called when device state change completed. Access to any device is allowed.
		/// Relevant for multi thread application.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public static void UnlockDeviceAccess()
		{
#if AXIOM_THREAD_SUPPORT
			if ( Configuration.Config.AxiomThreadLevel == 1 )
				System.Threading.Monitor.Exit( deviceLockMutex );
#endif
		}

		///<summary>
		/// Util functions to convert a D3D locked rectangle to a pixel box
		///</summary>
		[OgreVersion( 1, 7, 2 )]
		protected static void FromD3DLock( PixelBox rval, DX.DataRectangle lrect )
		{
			var bpp = PixelUtil.GetNumElemBytes( rval.Format );
			var size = 0;

			if ( bpp != 0 )
			{
				rval.RowPitch = lrect.Pitch / bpp;
				rval.SlicePitch = rval.RowPitch * rval.Height;
				Debug.Assert( ( lrect.Pitch % bpp ) == 0 );
				size = lrect.Pitch * rval.Height;
			}
			else if ( PixelUtil.IsCompressed( rval.Format ) )
			{
				rval.RowPitch = rval.Width;
				rval.SlicePitch = rval.Width * rval.Height;
				size = rval.Width * rval.Height;
			}
			else
			{
				throw new AxiomException( "Invalid pixel format" );
			}

			rval.Data = BufferBase.Wrap( lrect.DataPointer, size );
		}

		///<summary>
		/// Util functions to convert a D3D LockedBox to a pixel box
		///</summary>
		[OgreVersion( 1, 7, 2 )]
		protected static void FromD3DLock( PixelBox rval, DX.DataBox lbox )
		{
			var bpp = PixelUtil.GetNumElemBytes( rval.Format );
			var size = 0;

			if ( bpp != 0 )
			{
				rval.RowPitch = lbox.RowPitch / bpp;
				rval.SlicePitch = lbox.SlicePitch / bpp;
				Debug.Assert( ( lbox.RowPitch % bpp ) == 0 );
				Debug.Assert( ( lbox.SlicePitch % bpp ) == 0 );
				size = lbox.RowPitch * rval.Height;
			}
			else if ( PixelUtil.IsCompressed( rval.Format ) )
			{
				rval.RowPitch = rval.Width;
				rval.SlicePitch = rval.Width * rval.Height;
				size = rval.Width * rval.Height;
			}
			else
			{
				throw new AxiomException( "Invalid pixel format" );
			}

			rval.Data = BufferBase.Wrap( lbox.DataPointer, size );
		}

		///<summary>
		/// Convert Axiom integer Box to D3D rectangle
		///</summary>
		[OgreVersion( 1, 7, 2 )]
		protected static System.Drawing.Rectangle ToD3DRectangle( BasicBox lockBox )
		{
			Debug.Assert( lockBox.Depth == 1 );
			var r = new System.Drawing.Rectangle();
			r.X = lockBox.Left;
			r.Width = lockBox.Width;
			r.Y = lockBox.Top;
			r.Height = lockBox.Height;
			return r;
		}

		///<summary>
		/// Convert Axiom Box to D3D box
		///</summary>
		[OgreVersion( 1, 7, 2 )]
		protected static D3D9.Box ToD3DBox( BasicBox lockBox )
		{
			var pbox = new D3D9.Box();
			pbox.Left = lockBox.Left;
			pbox.Right = lockBox.Right;
			pbox.Top = lockBox.Top;
			pbox.Bottom = lockBox.Bottom;
			pbox.Front = lockBox.Front;
			pbox.Back = lockBox.Back;
			return pbox;
		}

		///<summary>
		/// Convert Axiom PixelBox extent to D3D rectangle
		///</summary>
		[OgreVersion( 1, 7, 2 )]
		protected static System.Drawing.Rectangle ToD3DRectangleExtent( PixelBox lockBox )
		{
			Debug.Assert( lockBox.Depth == 1 );
			var r = new System.Drawing.Rectangle();
			r.X = 0;
			r.Width = lockBox.Width;
			r.X = 0;
			r.Height = lockBox.Height;
			return r;
		}

		///<summary>
		/// Convert Axiom PixelBox extent to D3D box
		///</summary>
		[OgreVersion( 1, 7, 2 )]
		protected static D3D9.Box ToD3DBoxExtent( PixelBox lockBox )
		{
			var pbox = new D3D9.Box();
			pbox.Left = 0;
			pbox.Right = lockBox.Width;
			pbox.Top = 0;
			pbox.Bottom = lockBox.Height;
			pbox.Front = 0;
			pbox.Back = lockBox.Depth;
			return pbox;
		}

		///<summary>
		/// Lock a box
		///</summary>
		[OgreVersion( 1, 7, 2 )]
		protected override PixelBox LockImpl( BasicBox lockBox, BufferLocking options )
		{
			//Entering critical section
			LockDeviceAccess();

			// Check for misuse
			if ( ( (int)usage & (int)TextureUsage.RenderTarget ) != 0 )
			{
				throw new AxiomException( "DirectX does not allow locking of or directly writing to RenderTargets. Use BlitFromMemory if you need the contents." );
			}

			// Set locking flags according to options
			var flags = D3D9Helper.ConvertEnum( options, usage );

			if ( mapDeviceToBufferResources.Count == 0 )
			{
				throw new AxiomException( "There are no resources attached to this pixel buffer !!" );
			}

			lockedBox = lockBox;
			lockFlags = flags;

			var bufferResources = mapDeviceToBufferResources.First().Value;

			// Lock the source buffer.
			var lockedBuf = LockBuffer( bufferResources, lockBox, flags );

			//Leaving critical section
			UnlockDeviceAccess();

			return lockedBuf;
		}

		[OgreVersion( 1, 7, 2 )]
		protected PixelBox LockBuffer( BufferResources bufferResources, BasicBox lockBox, D3D9.LockFlags flags )
		{
			// Set extents and format
			// Note that we do not carry over the left/top/front here, since the returned
			// PixelBox will be re-based from the locking point onwards
			var rval = new PixelBox( lockBox.Width, lockBox.Height, lockBox.Depth, this.Format );

			if ( bufferResources.Surface != null )
			{
				//Surface
				DX.DataRectangle lrect; // Filled in by D3D

				if ( lockBox.Left == 0 && lockBox.Top == 0 && lockBox.Right == this.Width && lockBox.Bottom == this.Height )
				{
					// Lock whole surface
					lrect = bufferResources.Surface.LockRectangle( flags );
				}
				else
				{
					var prect = ToD3DRectangle( lockBox );
					lrect = bufferResources.Surface.LockRectangle( prect, flags );
				}

				FromD3DLock( rval, lrect );
			}
			else if ( bufferResources.Volume != null )
			{
				// Volume
				var pbox = ToD3DBox( lockBox ); // specify range to lock
				var lbox = bufferResources.Volume.LockBox( pbox, flags );
				FromD3DLock( rval, lbox );
			}

			return rval;
		}

		///<summary>
		/// Unlock a box
		///</summary>
		[OgreVersion( 1, 7, 2 )]
		protected override void UnlockImpl()
		{
			//Entering critical section
			LockDeviceAccess();

			if ( mapDeviceToBufferResources.Count == 0 )
			{
				throw new AxiomException( "There are no resources attached to this pixel buffer !!" );
			}

			// 1. Update duplicates buffers.
			foreach ( var it in mapDeviceToBufferResources )
			{
				var bufferResources = it.Value;

				// Update duplicated buffer from the from the locked buffer content.
				BlitFromMemory( CurrentLock, lockedBox, bufferResources );
			}

			// 2. Unlock the locked buffer.
			var bufferRes = mapDeviceToBufferResources.First().Value;
			UnlockBuffer( bufferRes );
			if ( doMipmapGen )
			{
				GenMipmaps( bufferRes.MipTex );
			}

			//Leaving critical section
			UnlockDeviceAccess();
		}

		[OgreVersion( 1, 7, 2 )]
		protected void UnlockBuffer( BufferResources bufferResources )
		{
			if ( bufferResources.Surface != null )
			{
				// Surface
				bufferResources.Surface.UnlockRectangle();
			}
			else if ( bufferResources.Volume != null )
			{
				// Volume
				bufferResources.Volume.UnlockBox();
			}
		}

		///<summary>
		/// Copies a box from another PixelBuffer to a region of the
		/// this PixelBuffer.
		///</summary>
		///<param name="rsrc">Source/dest pixel buffer</param>
		///<param name="srcBox">Image.BasicBox describing the source region in this buffer</param>
		///<param name="dstBox">Image.BasicBox describing the destination region in this buffer</param>
		///<remarks>
		/// The source and destination regions dimensions don't have to match, in which
		/// case scaling is done. This scaling is generally done using a bilinear filter in hardware,
		/// but it is faster to pass the source image in the right dimensions.
		/// Only call this function when both buffers are unlocked.
		///</remarks>
		[OgreVersion( 1, 7, 2 )]
		public override void Blit( HardwarePixelBuffer rsrc, BasicBox srcBox, BasicBox dstBox )
		{
			//Entering critical section
			LockDeviceAccess();

			var _src = (D3D9HardwarePixelBuffer)rsrc;
			foreach ( var it in mapDeviceToBufferResources )
			{
				var srcBufferResources = ( (D3D9HardwarePixelBuffer)rsrc ).GetBufferResources( it.Key );
				var dstBufferResources = it.Value;

				if ( srcBufferResources == null )
				{
					throw new AxiomException( "There are no matching resources attached to the source pixel buffer !!" );
				}

				Blit( it.Key, rsrc, srcBox, dstBox, srcBufferResources, dstBufferResources );
			}

			//Leaving critical section
			UnlockDeviceAccess();
		}

		[OgreVersion( 1, 7, 2 )]
		protected void Blit( D3D9.Device d3d9Device, HardwarePixelBuffer rsrc, BasicBox srcBox, BasicBox dstBox, BufferResources srcBufferResources, BufferResources dstBufferResources )
		{
			if ( dstBufferResources.Surface != null && srcBufferResources.Surface != null )
			{
				// Surface-to-surface
				var dsrcRect = ToD3DRectangle( srcBox );
				var ddestRect = ToD3DRectangle( dstBox );

				var srcDesc = srcBufferResources.Surface.Description;

				// If we're blitting from a RTT, try GetRenderTargetData
				// if we're going to try to use GetRenderTargetData, need to use system mem pool

				// romeoxbm: not used even in Ogre
				//var tryGetRenderTargetData = false;

				if ( ( srcDesc.Usage & D3D9.Usage.RenderTarget ) != 0 && srcDesc.MultiSampleType == D3D9.MultisampleType.None )
				{
					// Temp texture
					var tmptex = new D3D9.Texture( d3d9Device, srcDesc.Width, srcDesc.Height, 1, // 1 mip level ie topmost, generate no mipmaps
					                               0, srcDesc.Format, D3D9.Pool.SystemMemory );

					var tmpsurface = tmptex.GetSurfaceLevel( 0 );

					if ( d3d9Device.GetRenderTargetData( srcBufferResources.Surface, tmpsurface ).Success )
					{
						// Hey, it worked
						// Copy from this surface instead
						var res = D3D9.Surface.FromSurface( dstBufferResources.Surface, tmpsurface, D3D9.Filter.Default, 0, dsrcRect, ddestRect );
						if ( res.Failure )
						{
							tmpsurface.SafeDispose();
							tmptex.SafeDispose();
							throw new AxiomException( "D3D9.Surface.FromSurface failed in D3D9HardwarePixelBuffer.Blit" );
						}
						tmpsurface.SafeDispose();
						tmptex.SafeDispose();
						return;
					}
				}

				// Otherwise, try the normal method
				var res2 = D3D9.Surface.FromSurface( dstBufferResources.Surface, srcBufferResources.Surface, D3D9.Filter.Default, 0, dsrcRect, ddestRect );
				if ( res2.Failure )
				{
					throw new AxiomException( "D3D9.Surface.FromSurface failed in D3D9HardwarePixelBuffer.Blit" );
				}
			}
			else if ( dstBufferResources.Volume != null && srcBufferResources.Volume != null )
			{
				// Volume-to-volume
				var dsrcBox = ToD3DBox( srcBox );
				var ddestBox = ToD3DBox( dstBox );

				var res = D3D9.Volume.FromVolume( dstBufferResources.Volume, srcBufferResources.Volume, D3D9.Filter.Default, 0, dsrcBox, ddestBox );
				if ( res.Failure )
				{
					throw new AxiomException( "D3D9.Volume.FromVolume failed in D3D9HardwarePixelBuffer.Blit" );
				}
			}
			else
			{
				// Software fallback
				base.Blit( rsrc, srcBox, dstBox );
			}
		}

		///<summary>
		/// Copies a region from normal memory to a region of this pixelbuffer. The source
		/// image can be in any pixel format supported by Axiom, and in any size.
		///</summary>
		///<param name="src">PixelBox containing the source pixels and format in memory</param>
		///<param name="dstBox">Image.BasicBox describing the destination region in this buffer</param>
		///<remarks>
		/// The source and destination regions dimensions don't have to match, in which
		/// case scaling is done. This scaling is generally done using a bilinear filter in hardware,
		/// but it is faster to pass the source image in the right dimensions.
		/// Only call this function when both  buffers are unlocked.
		///</remarks>
		[OgreVersion( 1, 7, 2 )]
		public override void BlitFromMemory( PixelBox src, BasicBox dstBox )
		{
			//Entering critical section
			LockDeviceAccess();

			foreach ( var it in mapDeviceToBufferResources )
			{
				BlitFromMemory( src, dstBox, it.Value );
			}

			//Leaving critical section
			UnlockDeviceAccess();
		}

		protected void BlitFromMemory( PixelBox src, BasicBox dstBox, BufferResources dstBufferResources )
		{
			// for scoped deletion of conversion buffer
			var converted = src;
			var bufSize = 0;

			// convert to pixelbuffer's native format if necessary
			if ( D3D9Helper.ConvertEnum( src.Format ) == D3D9.Format.Unknown )
			{
				bufSize = PixelUtil.GetMemorySize( src.Width, src.Height, src.Depth, Format );
				var newBuffer = new byte[ bufSize ];
				using ( var data = BufferBase.Wrap( newBuffer ) )
				{
					converted = new PixelBox( src.Width, src.Height, src.Depth, Format, data );
				}
				PixelConverter.BulkPixelConversion( src, converted );
			}

			int rowWidth = 0;
			if ( PixelUtil.IsCompressed( converted.Format ) )
			{
				rowWidth = converted.RowPitch / 4;
				// D3D wants the width of one row of cells in bytes
				if ( converted.Format == PixelFormat.DXT1 )
				{
					// 64 bits (8 bytes) per 4x4 block
					rowWidth *= 8;
				}
				else
				{
					// 128 bits (16 bytes) per 4x4 block
					rowWidth *= 16;
				}
			}
			else
			{
				rowWidth = converted.RowPitch * PixelUtil.GetNumElemBytes( converted.Format );
			}

			if ( dstBufferResources.Surface != null )
			{
				var srcRect = ToD3DRectangle( converted );
				var destRect = ToD3DRectangle( dstBox );

				bufSize = PixelUtil.GetMemorySize( converted.Width, converted.Height, converted.Depth, converted.Format );
				var data = new byte[ bufSize ];
				using ( var dest = BufferBase.Wrap( data ) )
				{
					Memory.Copy( converted.Data, dest, bufSize );
				}

				try
				{
					D3D9.Surface.FromMemory( dstBufferResources.Surface, data, D3D9.Filter.Default, 0, D3D9Helper.ConvertEnum( converted.Format ), rowWidth, srcRect, destRect );
				}
				catch ( Exception e )
				{
					throw new AxiomException( "D3D9.Surface.FromMemory failed in D3D9HardwarePixelBuffer.BlitFromMemory", e );
				}
			}
			else if ( dstBufferResources.Volume != null )
			{
				var srcBox = ToD3DBox( converted );
				var destBox = ToD3DBox( dstBox );
				var sliceWidth = 0;
				if ( PixelUtil.IsCompressed( converted.Format ) )
				{
					sliceWidth = converted.SlicePitch / 16;
					// D3D wants the width of one slice of cells in bytes
					if ( converted.Format == PixelFormat.DXT1 )
					{
						// 64 bits (8 bytes) per 4x4 block
						sliceWidth *= 8;
					}
					else
					{
						// 128 bits (16 bytes) per 4x4 block
						sliceWidth *= 16;
					}
				}
				else
				{
					sliceWidth = converted.SlicePitch * PixelUtil.GetNumElemBytes( converted.Format );
				}

				bufSize = PixelUtil.GetMemorySize( converted.Width, converted.Height, converted.Depth, converted.Format );
				var data = new byte[ bufSize ];
				using ( var dest = BufferBase.Wrap( data ) )
				{
					Memory.Copy( converted.Data, dest, bufSize );
				}

				//TODO note sliceWidth and rowWidth are ignored..
				D3D9.ImageInformation info;
				try
				{
					//D3D9.D3DX9.LoadVolumeFromMemory() not accessible 'cause D3D9.D3DX9 static class is not public
					D3D9.Volume.FromFileInMemory( dstBufferResources.Volume, data, D3D9.Filter.Default, 0, srcBox, destBox, null, out info );
				}
				catch ( Exception e )
				{
					throw new AxiomException( "D3D9.Volume.FromFileInMemory failed in D3D9HardwarePixelBuffer.BlitFromMemory", e );
				}
			}

			if ( doMipmapGen )
			{
				GenMipmaps( dstBufferResources.MipTex );
			}
		}

		///<summary>
		/// Copies a region of this pixelbuffer to normal memory.
		///</summary>
		///<param name="srcBox">BasicBox describing the source region of this buffer</param>
		///<param name="dst">PixelBox describing the destination pixels and format in memory</param>
		///<remarks>
		/// The source and destination regions don't have to match, in which
		/// case scaling is done.
		/// Only call this function when the buffer is unlocked.
		///</remarks>
		[OgreVersion( 1, 7, 2 )]
		public override void BlitToMemory( BasicBox srcBox, PixelBox dst )
		{
			//Entering critical section
			LockDeviceAccess();

			var pair = mapDeviceToBufferResources.First();
			BlitToMemory( srcBox, dst, pair.Value, pair.Key );

			//Leaving critical section
			UnlockDeviceAccess();
		}

		[OgreVersion( 1, 7, 2 )]
		protected void BlitToMemory( BasicBox srcBox, PixelBox dst, BufferResources srcBufferResources, D3D9.Device d3d9Device )
		{
			// Decide on pixel format of temp surface
			PixelFormat tmpFormat = Format;
			if ( D3D9Helper.ConvertEnum( dst.Format ) != D3D9.Format.Unknown )
			{
				tmpFormat = dst.Format;
			}

			if ( srcBufferResources.Surface != null )
			{
				Debug.Assert( srcBox.Depth == 1 && dst.Depth == 1 );
				var srcDesc = srcBufferResources.Surface.Description;
				var temppool = D3D9.Pool.Scratch;

				// if we're going to try to use GetRenderTargetData, need to use system mem pool
				var tryGetRenderTargetData = false;
				if ( ( ( srcDesc.Usage & D3D9.Usage.RenderTarget ) != 0 ) && ( srcBox.Width == dst.Width ) && ( srcBox.Height == dst.Height ) && ( srcBox.Width == this.Width ) && ( srcBox.Height == this.Height ) && ( this.Format == tmpFormat ) )
				{
					tryGetRenderTargetData = true;
					temppool = D3D9.Pool.SystemMemory;
				}

				// Create temp texture
				var tmp = new D3D9.Texture( d3d9Device, dst.Width, dst.Height, 1, // 1 mip level ie topmost, generate no mipmaps
				                            0, D3D9Helper.ConvertEnum( tmpFormat ), temppool );

				var surface = tmp.GetSurfaceLevel( 0 );

				// Copy texture to this temp surface
				var srcRect = ToD3DRectangle( srcBox );
				var destRect = ToD3DRectangle( dst );

				// Get the real temp surface format
				var dstDesc = surface.Description;
				tmpFormat = D3D9Helper.ConvertEnum( dstDesc.Format );

				// Use fast GetRenderTargetData if we are in its usage conditions
				var fastLoadSuccess = false;
				if ( tryGetRenderTargetData )
				{
					var result = d3d9Device.GetRenderTargetData( srcBufferResources.Surface, surface );
					fastLoadSuccess = result.Success;
				}
				if ( !fastLoadSuccess )
				{
					var res = D3D9.Surface.FromSurface( surface, srcBufferResources.Surface, D3D9.Filter.Default, 0, srcRect, destRect );
					if ( res.Failure )
					{
						surface.SafeDispose();
						tmp.SafeDispose();
						throw new AxiomException( "D3D9.Surface.FromSurface failed in D3D9HardwarePixelBuffer.BlitToMemory" );
					}
				}

				// Lock temp surface and copy it to memory
				var lrect = surface.LockRectangle( D3D9.LockFlags.ReadOnly );

				// Copy it
				var locked = new PixelBox( dst.Width, dst.Height, dst.Depth, tmpFormat );
				FromD3DLock( locked, lrect );
				PixelConverter.BulkPixelConversion( locked, dst );
				surface.UnlockRectangle();
				// Release temporary surface and texture
				surface.SafeDispose();
				tmp.SafeDispose();
			}
			else if ( srcBufferResources.Volume != null )
			{
				// Create temp texture
				var tmp = new D3D9.VolumeTexture( d3d9Device, dst.Width, dst.Height, dst.Depth, 0, 0, D3D9Helper.ConvertEnum( tmpFormat ), D3D9.Pool.Scratch );

				var surface = tmp.GetVolumeLevel( 0 );

				// Volume
				var ddestBox = ToD3DBoxExtent( dst );
				var dsrcBox = ToD3DBox( srcBox );

				var res = D3D9.Volume.FromVolume( surface, srcBufferResources.Volume, D3D9.Filter.Default, 0, dsrcBox, ddestBox );
				if ( res.Failure )
				{
					surface.SafeDispose();
					tmp.SafeDispose();
					throw new AxiomException( "D3D9.Surface.FromVolume failed in D3D9HardwarePixelBuffer.BlitToMemory" );
				}

				// Lock temp surface and copy it to memory
				var lbox = surface.LockBox( D3D9.LockFlags.ReadOnly ); // Filled in by D3D

				// Copy it
				var locked = new PixelBox( dst.Width, dst.Height, dst.Depth, tmpFormat );
				FromD3DLock( locked, lbox );
				PixelConverter.BulkPixelConversion( locked, dst );
				surface.UnlockBox();
				// Release temporary surface and texture
				surface.SafeDispose();
				tmp.SafeDispose();
			}
		}

		///<summary>
		/// Internal function to update mipmaps on update of level 0
		///</summary>
		[OgreVersion( 1, 7, 2 )]
		internal void GenMipmaps( D3D9.BaseTexture mipTex )
		{
			Debug.Assert( mipTex != null );

			// Mipmapping
			if ( HWMipmaps )
			{
				// Hardware mipmaps
				mipTex.GenerateMipSubLevels();
			}
			else
			{
				// Software mipmaps
				mipTex.FilterTexture( (int)D3D9.Filter.Default, D3D9.Filter.Default );
			}
		}

		///<summary>
		/// Function to set mipmap generation
		///</summary>
		[OgreVersion( 1, 7, 2 )]
		internal void SetMipmapping( bool doMipmapGen, bool HWMipmaps )
		{
			this.doMipmapGen = doMipmapGen;
			this.HWMipmaps = HWMipmaps;
		}

		///<summary>
		/// Notify TextureBuffer of destruction of render target
		///</summary>
		[OgreVersion( 1, 7, 2 )]
		public override void ClearSliceRTT( int zoffset )
		{
			renderTexture = null;
		}

		/// <summary>
		/// Release surfaces held by this pixel buffer.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void ReleaseSurfaces( D3D9.Device d3d9Device )
		{
			var bufferResources = GetBufferResources( d3d9Device );
			if ( bufferResources != null )
			{
				bufferResources.Surface.SafeDispose();
				bufferResources.Surface = null;

				bufferResources.Volume.SafeDispose();
				bufferResources.Volume = null;
			}
		}

		/// <summary>
		/// Accessor for surface
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public D3D9.Surface GetSurface( D3D9.Device d3d9Device )
		{
			var bufferResources = GetBufferResources( d3d9Device );

			if ( bufferResources != null )
			{
				ownerTexture.CreateTextureResources( d3d9Device );
				bufferResources = GetBufferResources( d3d9Device );
			}

			return bufferResources.Surface;
		}

		/// <summary>
		/// Accessor for AA surface
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public D3D9.Surface GetFSAASurface( D3D9.Device d3d9Device )
		{
			var bufferResources = GetBufferResources( d3d9Device );

			if ( bufferResources != null )
			{
				ownerTexture.CreateTextureResources( d3d9Device );
				bufferResources = GetBufferResources( d3d9Device );
			}

			return bufferResources.FsaaSurface;
		}

		/// <summary>
		/// Get rendertarget for z slice
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public override RenderTexture GetRenderTarget( int zoffset )
		{
			Debug.Assert( ( (int)usage & (int)TextureUsage.RenderTarget ) != 0 );
			Debug.Assert( renderTexture != null );
			return renderTexture;
		}

		/// <summary>
		/// Updates render texture.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		protected void UpdateRenderTexture( bool writeGamma, int fsaa, string srcName )
		{
			if ( renderTexture == null )
			{
				//romeoxbm: in Ogre, there was an (int)this instead of that this.ID
				// Check if we should use that id or, alternatively, the hashcode
				var name = string.Format( "rtt/{0}/{1}", this.ID, srcName );
				renderTexture = new D3D9RenderTexture( name, this, writeGamma, fsaa );
				Root.Instance.RenderSystem.AttachRenderTarget( renderTexture );
			}
		}

		/// <summary>
		/// Destroy render texture.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		protected void DestroyRenderTexture()
		{
			if ( renderTexture != null )
			{
				Root.Instance.RenderSystem.DestroyRenderTarget( renderTexture.Name );
				renderTexture = null;
			}
		}

		#endregion Methods
	};
}
