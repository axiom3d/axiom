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

#region Namespace Declarations

using System.Diagnostics;
using System.IO;

using Axiom.Core;
using Axiom.CrossPlatform;
using Axiom.Graphics;
using Axiom.Math;
using Axiom.Media;

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
		protected byte mLayerIdx;

		/// <summary>
		/// RGBA
		/// </summary>
		protected byte mChannel;

		/// <summary>
		/// in pixel format
		/// </summary>
		protected byte mChannelOffset;

		protected BasicBox mDirtyBox;
		protected bool mDirty;
		protected HardwarePixelBuffer mBuffer;
		protected float[] mData;

		#region - propeties -

		/// <summary>
		/// Get's the parent terrain.
		/// </summary>
		public Terrain Parent
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return mParent;
			}
		}

		/// <summary>
		/// Get's the index of the layer this is targetting
		/// </summary>
		public byte LayerIndex
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return mLayerIdx;
			}
		}

		/// <summary>
		/// Get a float array of the whole blend data. 
		/// </summary>
		public float[] BlendPointer
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return mData;
			}
		}

		#endregion - propeties -

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent">The parent terrain</param>
		/// <param name="layerIdx">The layer index (should be 1 or higher)</param>
		/// <param name="buf">The buffer holding the data</param>
		[OgreVersion( 1, 7, 2 )]
		public TerrainLayerBlendMap( Terrain parent, byte layerIdx, HardwarePixelBuffer buf )
		{
			mParent = parent;
			mLayerIdx = layerIdx;
			mChannel = (byte)( ( mLayerIdx - 1 ) % 4 );
			mDirty = false;
			mBuffer = buf;
			mData = new float[ mBuffer.Width * mBuffer.Height * sizeof ( float ) ];

			// we know which of RGBA we need to look at, now find it in the format
			// because we can't guarantee what precise format the RS gives us
			PixelFormat fmt = mBuffer.Format;
			var rgbaShift = PixelUtil.GetBitShifts( fmt );
			mChannelOffset = (byte)( rgbaShift[ mChannel ] / 8 ); // /8 convert to bytes
#if AXIOM_BIG_ENDIAN
	// invert (dealing bytewise)
            mChannelOffset = (byte)( PixelUtil.GetNumElemBytes( fmt ) - mChannelOffset - 1 );
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
		[OgreVersion( 1, 7, 2 )]
		public void ConverWorldToUVSpace( Vector3 worldPost, ref Real outX, ref Real outY )
		{
			var terrainSpace = Vector3.Zero;
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
		[OgreVersion( 1, 7, 2 )]
		public void ConvertUVToWorldSpace( Real x, Real y, ref Vector3 worldPos )
		{
			mParent.GetPosition( x, 1.0f - y, 0, ref worldPos );
		}

		/// <summary>
		/// Convert local space values (0,1) to image space (0, imageSize).
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void ConvertUVToImageSpace( Real x, Real y, ref int outX, ref int outY )
		{
			outX = (int)( x * ( mBuffer.Width - 1 ) );
			outY = (int)( y * ( mBuffer.Height - 1 ) );
		}

		/// <summary>
		/// Convert image space (0, imageSize) to local space values (0,1).
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void ConvertImageToUVSpace( int x, int y, ref Real outX, ref Real outY )
		{
			outX = x / (Real)( mBuffer.Width - 1 );
			outY = y / (Real)( mBuffer.Height - 1 );
		}

		/// <summary>
		///  Convert image space (0, imageSize) to terrain space values (0,1).
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void ConvertImageToTerrainSpace( int x, int y, ref Real outX, ref Real outY )
		{
			ConvertImageToUVSpace( x, y, ref outX, ref outY );
			outY = 1.0f - outY;
		}

		/// <summary>
		/// Convert terrain space values (0,1) to image space (0, imageSize).
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void ConvertTerrainToImageSpace( Real x, Real y, ref int outX, ref int outY )
		{
			ConvertUVToImageSpace( x, 1.0f - y, ref outX, ref outY );
		}

		/// <summary>
		/// Get a single value of blend information, in image space.
		/// </summary>
		/// <param name="x">x,y Coordinates of the point of data to get, in image space (top down)</param>
		/// <returns>The blend data</returns>
		[OgreVersion( 1, 7, 2 )]
		public float GetBlendValue( int x, int y )
		{
			return mData[ y * mBuffer.Width + x ];
		}

		/// <summary>
		/// Set a single value of blend information (0 = transparent, 255 = solid)
		/// </summary>
		/// <param name="x">x,y Coordinates of the point of data to get, in image space (top down)</param>
		/// <param name="y"></param>
		/// <param name="val">The blend value to set (0..1)</param>
		[OgreVersion( 1, 7, 2 )]
		public void SetBlendValue( int x, int y, float val )
		{
			mData[ y * mBuffer.Width + x ] = val;
			DirtyRect( new Rectangle( x, y, x + 1, y + 1 ) );
		}

		/// <summary>
		/// Indicate that all of the blend data is dirty and needs updating.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void Dirty()
		{
			var rect = new Rectangle();
			rect.Top = 0;
			rect.Bottom = mBuffer.Height;
			rect.Left = 0;
			rect.Right = mBuffer.Width;
			DirtyRect( rect );
		}

		/// <summary>
		/// Indicate that a portion of the blend data is dirty and needs updating.
		/// </summary>
		/// <param name="rect">Rectangle in image space</param>
		[OgreVersion( 1, 7, 2 )]
		public void DirtyRect( Rectangle rect )
		{
			if ( mDirty )
			{
				mDirtyBox.Left = System.Math.Min( mDirtyBox.Left, (int)rect.Left );
				mDirtyBox.Top = System.Math.Min( mDirtyBox.Top, (int)rect.Top );
				mDirtyBox.Right = System.Math.Max( mDirtyBox.Right, (int)rect.Right );
				mDirtyBox.Bottom = System.Math.Max( mDirtyBox.Bottom, (int)rect.Bottom );
			}
			else
			{
				mDirtyBox = new BasicBox( (int)rect.Left, (int)rect.Top, (int)rect.Right, (int)rect.Bottom );
				mDirty = true;
			}
		}

		/// <summary>
		/// Publish any changes you made to the blend data back to the blend map. 
		/// </summary>
		/// <note>
		/// Can only be called in the main render thread.
		/// </note>
		[OgreVersion( 1, 7, 2 )]
		public void Update()
		{
			if ( mData != null && mDirty )
			{
				using ( var mDataBuf = BufferBase.Wrap( mData ) )
				{
					var pSrcBase = mDataBuf + ( mDirtyBox.Top * mBuffer.Width + mDirtyBox.Left );
					var pDstBase = mBuffer.Lock( mDirtyBox, BufferLocking.Normal ).Data;
					pDstBase += mChannelOffset;
					var dstInc = PixelUtil.GetNumElemBytes( mBuffer.Format );

#if !AXIOM_SAFE_ONLY
					unsafe
#endif
					{
						for ( int y = 0; y < mDirtyBox.Height; ++y )
						{
							var pSrc = ( pSrcBase + ( y * mBuffer.Width ) * sizeof ( float ) ).ToFloatPointer();
							var pDst = pDstBase + ( y * mBuffer.Width * dstInc );
							for ( int x = 0; x < mDirtyBox.Width; ++x )
							{
								pDst.ToBytePointer()[ 0 ] = (byte)( pSrc[ x ] * 255 );
								pDst += dstInc;
							}
						}
					}

					mBuffer.Unlock();
					mDirty = false;
				}

				// make sure composite map is updated
				// mDirtyBox is in image space, convert to terrain units
				var compositeMapRect = new Rectangle();
				var blendToTerrain = (float)mParent.Size / (float)mBuffer.Width;
				compositeMapRect.Left = (long)( mDirtyBox.Left * blendToTerrain );
				compositeMapRect.Right = (long)( mDirtyBox.Right * blendToTerrain + 1 );
				compositeMapRect.Top = (long)( ( mBuffer.Height - mDirtyBox.Bottom ) * blendToTerrain );
				compositeMapRect.Bottom = (long)( ( mBuffer.Height - mDirtyBox.Top ) * blendToTerrain + 1 );
				mParent.DirtyCompositeMapRect( compositeMapRect );
				mParent.UpdateCompositeMapWithDelay();
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
		[OgreVersion( 1, 7, 2 )]
		public void Blit( ref PixelBox src, BasicBox dstBox )
		{
			var srcBox = src;

			if ( srcBox.Width != dstBox.Width || srcBox.Height != dstBox.Height )
			{
				// we need to rescale src to dst size first (also confvert format)
				var tmpData = new float[ dstBox.Width * dstBox.Height ];
				srcBox = new PixelBox( dstBox.Width, dstBox.Height, 1, PixelFormat.L8, BufferBase.Wrap( tmpData ) );
				Image.Scale( src, srcBox );
			}

			//pixel conversion
			var dstMemBox = new PixelBox( dstBox.Width, dstBox.Height, dstBox.Depth, PixelFormat.L8, BufferBase.Wrap( mData ) );
			PixelConverter.BulkPixelConversion( src, dstMemBox );

			if ( srcBox != src )
			{
				// free temp
				srcBox = null;
			}
			var dRect = new Rectangle( dstBox.Left, dstBox.Top, dstBox.Right, dstBox.Bottom );
			DirtyRect( dRect );
		}

		[OgreVersion( 1, 7, 2 )]
		public void Blit( ref PixelBox src )
		{
			Blit( ref src, new BasicBox( 0, 0, 0, mBuffer.Width, mBuffer.Height, 1 ) );
		}

		/// <summary>
		/// Load an image into this blend layer. 
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void LoadImage( Image img )
		{
			var pBox = img.GetPixelBox();
			Blit( ref pBox );
		}

		/// <summary>
		/// Load an image into this blend layer. 
		/// </summary>
		/// <param name="stream">Stream containing the image data</param>
		/// <param name="extension">Extension identifying the image type, if the stream data doesn't identify</param>
		[OgreVersion( 1, 7, 2 )]
		public void LoadImage( Stream stream, string extension )
		{
			var img = Image.FromStream( stream, extension );
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
		[OgreVersion( 1, 7, 2 )]
		public void LoadImage( string fileName, string groupName )
		{
			var img = Image.FromFile( fileName, groupName );
			LoadImage( img );
		}

		[OgreVersion( 1, 7, 2 )]
		protected void Download()
		{
#if !AXIOM_SAFE_ONLY
			unsafe
#endif
			{
				using ( var pDst = BufferBase.Wrap( mData ) )
				{
					var pDstPtr = pDst.ToFloatPointer();
					var pDstIdx = 0;
					//download data
					var box = new BasicBox( 0, 0, mBuffer.Width, mBuffer.Height );
					var pBox = mBuffer.Lock( box, BufferLocking.ReadOnly );
					var pSrc = pBox.Data.ToBytePointer();
					var pSrcIdx = (int)mChannelOffset;
					var srcInc = PixelUtil.GetNumElemBytes( mBuffer.Format );
					for ( var y = box.Top; y < box.Bottom; ++y )
					{
						for ( var x = box.Left; x < box.Right; ++x )
						{
							pDstPtr[ pDstIdx++ ] = (float)( ( pSrc[ pSrcIdx ] ) / 255.0f );
							pSrcIdx += srcInc;
						}
					}
					mBuffer.Unlock();
				}
			}
		}

		protected void Upload()
		{
			throw new System.NotImplementedException();
		}
	}
}
