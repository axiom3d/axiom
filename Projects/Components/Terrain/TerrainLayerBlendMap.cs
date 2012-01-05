#region MIT/X11 License

//Copyright © 2003-2011 Axiom 3D Rendering Engine Project
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

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

using Axiom.Core;
using Axiom.Media;
using Axiom.Graphics;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.Components.Terrain
{
	/// <summary>
	/// Class exposing an interface to a blend map for a given layer. 
	/// Each layer after the first layer in a terrain has a blend map which 
	/// expresses how it is alpha blended with the layers beneath. Internally, this
	/// blend map is packed into one channel of an RGB or RGBA texture in
	/// order to use the smallest number of samplers, but this class allows
	/// a caller to manipulate the data more easily without worrying about
	/// this packing. Also, the values you use to interact with the blend map are
	/// floating point, which gives you full precision for updating, but in fact the
	/// values are packed into 8-bit integers in the actual blend map.
	/// </summary>
	public class TerrainLayerBlendMap
	{
		protected Terrain mParent;

		/// <summary>
		/// 
		/// </summary>
		protected byte mLayerIdx;

		/// <summary>
		/// RGBA
		/// </summary>
		protected byte mChannel;

		/// <summary>
		/// in pixel format
		/// </summary>
		protected byte mChannelOffset;

		/// <summary>
		/// 
		/// </summary>
		protected BasicBox mDirtyBox;

		/// <summary>
		/// 
		/// </summary>
		protected bool mDirty;

		/// <summary>
		/// 
		/// </summary>
		protected HardwarePixelBuffer mBuffer;

		/// <summary>
		/// 
		/// </summary>
		protected float[] mData;

		#region - propeties -

		/// <summary>
		/// Get's the parent terrain.
		/// </summary>
		public Terrain Parent { get { return mParent; } }

		/// <summary>
		/// Get's the index of the layer this is targetting
		/// </summary>
		public byte LayerIndex { get { return mLayerIdx; } }

		/// <summary>
		/// Get a float array of the whole blend data. 
		/// </summary>
		public float[] BlendPointer { get { return mData; } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent">The parent terrain</param>
		/// <param name="layerIdx">The layer index (should be 1 or higher)</param>
		/// <param name="buf">The buffer holding the data</param>
		public TerrainLayerBlendMap( Terrain parent, byte layerIdx, HardwarePixelBuffer buf )
		{
			mParent = parent;
			mLayerIdx = layerIdx;
			mChannel = (byte)( ( mLayerIdx - 1 ) % 4 );
			mDirty = false;
			mBuffer = buf;
			mData = new float[mBuffer.Width * mBuffer.Height * sizeof( float )];
			byte[] rgbaShift = new byte[4];
			PixelFormat fmt = mBuffer.Format;
			GetBitShifts( fmt, ref rgbaShift );
			mChannelOffset = (byte)( rgbaShift[ mChannel ] / 8 ); // /8 convert to bytes
#if AXIOM_ENDIAN == AXIOM_ENDIAN_BIG
			//mChannelOffset = (byte)(PixelUtil.GetNumElemBytes(fmt) - mChannelOffset - 1);
#endif
			Download();
		}

		/// <summary>
		/// Helper method - convert a point in world space to UV space based on the
		///	terrain settings.
		/// </summary>
		/// <param name="worldPost">World position</param>
		/// <param name="outX">outX, outY Pointers to variables which will be filled in with the
		///	local UV space value. Note they are deliberately signed Real values, because the
		///	point you supply may be outside of image space and may be between texels.
		///	The values will range from 0 to 1, top/bottom, left/right.</param>
		/// <param name="outY"></param>
		public void ConverWorldToUVSpace( Vector3 worldPost, ref float outX, ref float outY )
		{
			Vector3 terrainSpace = Vector3.Zero;
			mParent.GetTerrainPosition( worldPost, ref terrainSpace );
			outX = terrainSpace.x;
			outY = 1.0f - terrainSpace.y;
		}

		/// <summary>
		/// Helper method - convert a point in local space to worldspace based on the
		///	terrain settings.
		/// </summary>
		/// <param name="x">x,y Local position, ranging from 0 to 1, top/bottom, left/right.</param>
		/// <param name="y">x,y Local position, ranging from 0 to 1, top/bottom, left/right.</param>
		/// <param name="worldPos">Vector will be filled in with the world space value</param>
		public void ConvertUVToWorldSpace( float x, float y, ref Vector3 worldPos )
		{
			mParent.GetPosition( x, 1.0f - y, 0, ref worldPos );
		}

		/// <summary>
		/// Convert local space values (0,1) to image space (0, imageSize).
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="outX"></param>
		/// <param name="outY"></param>
		public void ConvertUVToImageSpace( float x, float y, ref int outX, ref int outY )
		{
			outX = (int)( x * ( mBuffer.Width - 1 ) );
			outY = (int)( y * ( mBuffer.Height - 1 ) );
		}

		/// <summary>
		/// Convert image space (0, imageSize) to local space values (0,1).
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="outX"></param>
		/// <param name="outY"></param>
		public void ConvertImageToUVSpace( int x, int y, ref float outX, ref float outY )
		{
			outX = x / (float)( mBuffer.Width - 1 );
			outY = y / (float)( mBuffer.Height - 1 );
		}

		/// <summary>
		///  Convert image space (0, imageSize) to terrain space values (0,1).
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="outX"></param>
		/// <param name="outY"></param>
		public void ConvertImageToTerrainSpace( int x, int y, ref float outX, ref float outY )
		{
			ConvertImageToUVSpace( x, y, ref outX, ref outY );
			outY = 1.0f - outY;
		}

		/// <summary>
		/// Convert terrain space values (0,1) to image space (0, imageSize).
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="outX"></param>
		/// <param name="outY"></param>
		public void ConvertTerrainToImageSpace( float x, float y, ref int outX, ref int outY )
		{
			ConvertUVToImageSpace( x, 1.0f - y, ref outX, ref outY );
		}

		/// <summary>
		/// Get a single value of blend information, in image space.
		/// </summary>
		/// <param name="x">x,y Coordinates of the point of data to get, in image space (top down)</param>
		/// <param name="y"></param>
		/// <returns>The blend data</returns>
		public float GetBlendValue( int x, int y )
		{
			float ret = 0;
			unsafe
			{
				fixed( float* mDataF = mData )
				{
					//ret = (float)*(mDataF + y * mBuffer.Width + x);
					ret = mDataF[ y * mBuffer.Width + x ];
				}
			}
			return ret;
		}

		/// <summary>
		/// Set a single value of blend information (0 = transparent, 255 = solid)
		/// </summary>
		/// <param name="x">x,y Coordinates of the point of data to get, in image space (top down)</param>
		/// <param name="y"></param>
		/// <param name="val">The blend value to set (0..1)</param>
		public void SetBlendValue( int x, int y, float val )
		{
			if( val != 1.0f && val != 0.0f ) {}
			unsafe
			{
				fixed( float* pData = mData )
				{
					pData[ y * mBuffer.Width + x ] = val;
				}
			}
			DirtyRect( new Rectangle( x, y, x + 1, y + 1 ) );
		}

		/// <summary>
		/// Indicate that all of the blend data is dirty and needs updating.
		/// </summary>
		public void Dirty()
		{
			Rectangle rect = new Rectangle();
			rect.Top = 0;
			rect.Bottom = mBuffer.Height;
			rect.Left = 0;
			rect.Right = mBuffer.Width;
			DirtyRect( rect );
		}

		/// <summary>
		/// Indicate that a portion of the blend data is dirty and needs updating.
		/// </summary>
		public void DirtyRect( Rectangle rect )
		{
			if( mDirty )
			{
				mDirtyBox.Left = System.Math.Min( mDirtyBox.Left, (int)rect.Left );
				mDirtyBox.Top = System.Math.Min( mDirtyBox.Top, (int)rect.Top );
				mDirtyBox.Right = System.Math.Max( mDirtyBox.Right, (int)rect.Right );
				mDirtyBox.Bottom = System.Math.Max( mDirtyBox.Bottom, (int)rect.Bottom );
			}
			else
			{
				if( mDirtyBox == null )
				{
					mDirtyBox = new BasicBox();
				}
				mDirtyBox = new BasicBox( (int)rect.Left, (int)rect.Top, (int)rect.Right, (int)rect.Bottom );
				mDirtyBox.Left = (int)rect.Left;
				mDirtyBox.Right = (int)rect.Right;
				mDirtyBox.Top = (int)rect.Top;
				mDirtyBox.Bottom = (int)rect.Bottom;
				mDirty = true;
			}
		}

		/// <summary>
		/// Blits a set of values into a region on the blend map. 
		/// </summary>
		/// <param name="src">PixelBox containing the source pixels and format </param>
		/// <param name="dstBox">describing the destination region in this map</param>
		/// <remarks>
		/// The source and destination regions dimensions don't have to match, in which
		/// case scaling is done. 
		/// </remarks>
		/// <note>
		/// You can call this method in a background thread if you want.
		/// You still need to call update() to commit the changes to the GPU. 
		/// </note>
		public void Blit( ref PixelBox src, BasicBox dstBox )
		{
			PixelBox srcBox = src;

			if( srcBox.Width != dstBox.Width || srcBox.Height != dstBox.Height )
			{
				// we need to rescale src to dst size first (also confvert format)
				float[] data = new float[dstBox.Width * dstBox.Height];
				unsafe
				{
					fixed( float* pDataF = data )
					{
						srcBox = new PixelBox( dstBox.Width, dstBox.Height, 1, PixelFormat.L8, (IntPtr)pDataF );
						Image.Scale( src, srcBox );
					}
				}
			}
			unsafe
			{
				//pixel conversion
				fixed( float* pDataF = mData )
				{
					PixelBox dstMemBox = new PixelBox( dstBox.Width, dstBox.Height, dstBox.Depth, PixelFormat.L8, (IntPtr)pDataF );
					PixelConverter.BulkPixelConversion( src, dstMemBox );

					if( srcBox != src )
					{
						// free temp
						srcBox = null;
					}
				}
			}

			Rectangle dRect = new Rectangle( dstBox.Left, dstBox.Top, dstBox.Right, dstBox.Bottom );
			DirtyRect( dRect );
		}

		public void Blit( ref PixelBox src )
		{
			Blit( ref src, new BasicBox( 0, 0, 0, mBuffer.Width, mBuffer.Height, 1 ) );
		}

		/// <summary>
		/// Load an image into this blend layer. 
		/// </summary>
		/// <param name="img"></param>
		public void LoadImage( Image img )
		{
			PixelBox pBox = img.GetPixelBox( 0, 0 );
			Blit( ref pBox );
		}

		/// <summary>
		/// Load an image into this blend layer. 
		/// </summary>
		/// <param name="stream"></param>
		/// <param name="extension"></param>
		public void LoadImage( Stream stream, string extension )
		{
			Image img = Image.FromStream( stream, extension );
			LoadImage( img );
		}

		/// <summary>
		/// Load an image into this blend layer. 
		/// </summary>
		/// <param name="stream"></param>
		public void LoadImage( Stream stream )
		{
			LoadImage( stream, string.Empty );
		}

		/// <summary>
		/// Load an image into this blend layer. 
		/// </summary>
		/// <param name="fileName"></param>
		/// <param name="groupName"></param>
		public void LoadImage( string fileName, string groupName )
		{
			Image img = Image.FromFile( fileName );
			LoadImage( img );
		}

		/// <summary>
		/// Publish any changes you made to the blend data back to the blend map. 
		/// </summary>
		/// <note>
		/// Can only be called in the main render thread.
		/// </note>
		public void Update()
		{
			if( mData != null && mDirty )
			{
				unsafe
				{
					// Upload data
					//fixed (float* pmDataF = mData)
					// {
					IntPtr mDataPtr = Memory.PinObject( mData );
					float* pmData = (float*)mDataPtr;
					float* pSrcBase = pmData + mDirtyBox.Top * mBuffer.Width + mDirtyBox.Left;
					Debug.Assert( mDirtyBox.Depth == 1 );
					PixelBox pBox = mBuffer.Lock( mDirtyBox, BufferLocking.Normal );
					byte* pDstBase = (byte*)pBox.Data;
					pDstBase += mChannelOffset;
					int dstInc = PixelUtil.GetNumElemBytes( mBuffer.Format );
					for( int y = 0; y < mDirtyBox.Height; ++y )
					{
						float* pSrc = pSrcBase + y * mBuffer.Width;
						byte* pDst = pDstBase + y * mBuffer.Width * dstInc;
						for( int x = 0; x < mDirtyBox.Width; ++x )
						{
							*pDst = (byte)( *pSrc++ * 255 );
							pDst += dstInc;
						}
					}

					mBuffer.Unlock();

					mDirty = false;
					// }
				}

				// make sure composite map is updated
				// mDirtyBox is in image space, convert to terrain units
				Rectangle compositeMapRect = new Rectangle();
				float blendToTerrain = (float)mParent.Size / (float)mBuffer.Width;
				compositeMapRect.Left = (long)( mDirtyBox.Left * blendToTerrain );
				compositeMapRect.Right = (long)( mDirtyBox.Right * blendToTerrain );
				compositeMapRect.Top = (long)( ( mBuffer.Height - mDirtyBox.Bottom ) * blendToTerrain );
				compositeMapRect.Bottom = (long)( ( mBuffer.Height - mDirtyBox.Top ) * blendToTerrain );
				mParent.DirtyCompositeMapRect( compositeMapRect );
				mParent.UpdateCompositeMapWithDelay();
			}
		}

		/// <summary>
		/// 
		/// </summary>
		protected void Download()
		{
			unsafe
			{
				// fixed (float* pDstF = mData)
				// {
				float* pDst = (float*)Memory.PinObject( mData );
				//download data
				BasicBox box = new BasicBox( 0, 0, mBuffer.Width, mBuffer.Height );
				PixelBox pBox = mBuffer.Lock( box, BufferLocking.ReadOnly );
				byte* pSrc = (byte*)pBox.Data;
				pSrc += mChannelOffset;
				int srcInc = PixelUtil.GetNumElemBytes( mBuffer.Format );
				for( int y = box.Top; y < box.Bottom; y++ )
				{
					for( int x = box.Left; x < box.Right; x++ )
					{
						*pDst++ = (float)( ( *pSrc ) / 255.0f );
						pSrc += srcInc;
					}
				}
				mBuffer.Unlock();
				Memory.UnpinObject( mData );
				// }
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="fmt"></param>
		/// <param name="rgba"></param>
		protected void GetBitShifts( PixelFormat fmt, ref byte[] rgba )
		{
			Debug.Assert( rgba.Length == 4, "rgba byte array must be a length of 4!" );
			PixelConverter.PixelFormatDescription des = PixelConverter.GetDescriptionFor( fmt );
			rgba[ 0 ] = des.rshift;
			rgba[ 1 ] = des.gshift;
			rgba[ 2 ] = des.bshift;
			rgba[ 3 ] = des.ashift;
		}
	}
}
