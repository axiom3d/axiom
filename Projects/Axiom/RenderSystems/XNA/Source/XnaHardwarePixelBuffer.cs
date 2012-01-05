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

#endregion

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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

using Axiom.RenderSystems.Xna;

using VertexDeclaration = Axiom.Graphics.VertexDeclaration;
using Root = Axiom.Core.Root;

using Axiom.Core;
using Axiom.Graphics;
using Axiom.Media;

using XNA = Microsoft.Xna.Framework;
using XFG = Microsoft.Xna.Framework.Graphics;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.Xna
{
	/// <summary>
	/// 	Xna implementation of HardwarePixelBuffer
	/// </summary>
	public class XnaHardwarePixelBuffer : HardwarePixelBuffer
	{
		#region Fields and Properties

		///<summary>
		///    Xna Device
		///</summary>
		protected XFG.GraphicsDevice device;

		///<summary>
		///    Surface abstracted by this buffer
		///</summary>
		protected ushort mipLevel;

		protected XFG.Texture2D surface;

		///<summary>
		///    FSAA Surface abstracted by this buffer
		///</summary>
		protected XFG.RenderTarget2D fsaaSurface;

		///<summary>
		///    Volume abstracted by this buffer
		///</summary>
		protected XFG.Texture3D volume;

		///<summary>
		///    Temporary surface in main memory if direct locking of mSurface is not possible
		///</summary>
		protected XFG.Texture2D tempSurface;

		///<summary>
		///    Temporary volume in main memory if direct locking of mVolume is not possible
		///</summary>
		protected XFG.Texture3D tempVolume;

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
		protected XFG.Texture mipTex;

		///<summary>
		///    Render targets
		///</summary>
		protected List<RenderTexture> sliceTRT;

		private byte[] _bufferBytes;
		private BasicBox _lockedBox;

		private XFG.RenderTarget2D renderTarget;
		public XFG.RenderTarget2D RenderTarget { get { return renderTarget; } }

		///<summary>
		/// Accessor for surface
		///</summary>
		public XFG.RenderTarget2D FSAASurface { get { return fsaaSurface; } }

		///<summary>
		/// Accessor for surface
		///</summary>
		public XFG.Texture Surface { get { return surface; } set { surface = (XFG.Texture2D)value; } }

		#endregion Fields and Properties

		#region Construction and Destruction

		public XnaHardwarePixelBuffer( BufferUsage usage )
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

		///<summary>
		///</summary>
		///<param name="width"></param>
		///<param name="height"></param>
		///<param name="depth"></param>
		///<param name="format"></param>
		///<param name="usage"></param>
		///<param name="useSystemMemory"></param>
		///<param name="useShadowBuffer"></param>
		public XnaHardwarePixelBuffer( int width, int height, int depth, PixelFormat format, BufferUsage usage, bool useSystemMemory, bool useShadowBuffer )
			: base( width, height, depth, format, usage, useSystemMemory, useShadowBuffer ) {}

		#endregion Construction and Destruction

		#region Methods

		///<summary>
		///    Call this to associate a Xna Texture2D with this pixel buffer
		///</summary>
		public void Bind( XFG.GraphicsDevice device, XFG.Texture2D surface, ushort miplevel, bool update )
		{
			this.device = device;
			this.surface = surface;
			this.mipLevel = miplevel;

			Width = surface.Width / (int)Axiom.Math.Utility.Pow( 2, mipLevel );
			Height = surface.Height / (int)Axiom.Math.Utility.Pow( 2, mipLevel );
			Depth = 1;
			Format = XnaHelper.Convert( surface.Format );
			// Default
			RowPitch = Width;
			SlicePitch = Height * Width;
			sizeInBytes = PixelUtil.GetMemorySize( Width, Height, Depth, Format );

			if( ( (int)usage & (int)TextureUsage.RenderTarget ) != 0 )
			{
				CreateRenderTextures( update );
			}
		}

		public void Bind( XFG.GraphicsDevice device, XFG.RenderTarget surface, bool update )
		{
			this.device = device;
			this.renderTarget = (XFG.RenderTarget2D)surface;

			Width = surface.Width / (int)Axiom.Math.Utility.Pow( 2, mipLevel );
			Height = surface.Height / (int)Axiom.Math.Utility.Pow( 2, mipLevel );
			Depth = 1;
			Format = XnaHelper.Convert( surface.Format );
			// Default
			RowPitch = Width;
			SlicePitch = Height * Width;
			sizeInBytes = PixelUtil.GetMemorySize( Width, Height, Depth, Format );

			if( ( (int)usage & (int)TextureUsage.RenderTarget ) != 0 )
			{
				CreateRenderTextures( update );
			}
		}

		///<summary>
		///    Call this to associate a Xna Texture3D with this pixel buffer
		///</summary>
		public void Bind( XFG.GraphicsDevice device, XFG.Texture3D volume, bool update )
		{
			this.device = device;
			this.volume = volume;

			Width = volume.Width;
			Height = volume.Height;
			Depth = volume.Depth;
			Format = XnaHelper.Convert( volume.Format );
			// Default
			RowPitch = Width;
			SlicePitch = Height * Width;
			sizeInBytes = PixelUtil.GetMemorySize( Width, Height, Depth, Format );

			if( ( (int)usage & (int)TextureUsage.RenderTarget ) != 0 )
			{
				CreateRenderTextures( update );
			}
		}

		///<summary>
		///    Create (or update) render textures for slices
		///</summary>
		///<param name="update">are we updating an existing texture</param>
		protected void CreateRenderTextures( bool update )
		{
			if( update )
			{
				Debug.Assert( sliceTRT.Count == Depth );
				foreach( XnaRenderTexture trt in sliceTRT )
				{
					trt.Rebind( this );
				}
				return;
			}

			DestroyRenderTextures();
			// Create render target for each slice
			sliceTRT.Clear();
			Debug.Assert( Depth == 1 );
			for( int zoffset = 0; zoffset < Depth; ++zoffset )
			{
				string name = "rtt/" + this.ID;
				RenderTexture trt = new XnaRenderTexture( name, this );
				sliceTRT.Add( trt );
				Root.Instance.RenderSystem.AttachRenderTarget( trt );
			}
		}

		///<summary>
		///    Destroy render textures for slices
		///</summary>
		protected void DestroyRenderTextures()
		{
			if( sliceTRT.Count == 0 )
			{
				return;
			}

			for( int i = 0; i < sliceTRT.Count; ++i )
			{
				RenderTexture trt = sliceTRT[ i ];
				if( trt != null )
				{
					Root.Instance.RenderSystem.DestroyRenderTarget( trt.Name );
				}
			}
		}

		///<summary>
		/// Internal function to update mipmaps on update of level 0
		///</summary>
		public void GenMipmaps()
		{
			Debug.Assert( mipTex != null );
			// Mipmapping
			mipTex.GenerateMipMaps( XFG.TextureFilter.Linear );
		}

		#endregion Methods

		#region HardwarePixelBuffer Implementation

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
		///    Only call this function when both buffers are unlocked. 
		///</remarks>
		public override void BlitFromMemory( PixelBox src, BasicBox dstBox )
		{
			PixelBox converted = src;
			GCHandle bufGCHandle = new GCHandle();
			int bufSize = 0;

			// Get src.Data as byte[]
			bufSize = PixelUtil.GetMemorySize( src.Width, src.Height, src.Depth, Format );
			byte[] newBuffer = new byte[bufSize];
			bufGCHandle = GCHandle.Alloc( newBuffer, GCHandleType.Pinned );
			// convert to pixelbuffer's native format if necessary
			if( XnaHelper.Convert( src.Format ) == XFG.SurfaceFormat.Unknown )
			{
				converted = new PixelBox( src.Width, src.Height, src.Depth, Format, bufGCHandle.AddrOfPinnedObject() );
				PixelConverter.BulkPixelConversion( src, converted );
			}
			else
			{
				Memory.Copy( converted.Data, bufGCHandle.AddrOfPinnedObject(), bufSize );
			}

			if( surface != null )
			{
				surface.SetData<byte>( mipLevel, XnaHelper.ToRectangle( dstBox ), newBuffer, 0, bufSize, XFG.SetDataOptions.None );
			}
			else
			{
				throw new NotSupportedException( "BlitFromMemory on Volume Textures not supported." );
			}

			// If we allocated a buffer for the temporary conversion, free it here
			if( bufGCHandle.IsAllocated )
			{
				bufGCHandle.Free();
			}

			if( doMipmapGen )
			{
				GenMipmaps();
			}
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
		public override void BlitToMemory( BasicBox srcBox, PixelBox dst ) {}

		///<summary>
		///    Internal implementation of <see cref="HardwareBuffer.Lock"/>.
		///</summary>
		unsafe protected override PixelBox LockImpl( BasicBox lockBox, BufferLocking options )
		{
			_lockedBox = lockBox;
			// Set extents and format
			PixelBox rval = new PixelBox( lockBox, Format );
			int sizeInBytes = PixelUtil.GetMemorySize( lockBox.Width, lockBox.Height, lockBox.Depth, XnaHelper.Convert( surface.Format ) );
			_bufferBytes = new byte[sizeInBytes];

			surface.GetData( mipLevel,
			                 new Microsoft.Xna.Framework.Rectangle( lockBox.Left, lockBox.Top, lockBox.Right, lockBox.Bottom ),
			                 _bufferBytes, 0, _bufferBytes.Length );

			fixed( byte* bytes = &_bufferBytes[ 0 ] )
			{
				rval.Data = new IntPtr( bytes );
			}

			return rval;
		}

		/// <summary>
		///     Internal implementation of <see cref="HardwareBuffer.Unlock"/>.
		/// </summary>
		protected override void UnlockImpl()
		{
			//set the bytes array inside the texture
			surface.SetData( mipLevel,
			                 new Microsoft.Xna.Framework.Rectangle( _lockedBox.Left, _lockedBox.Top, _lockedBox.Right, _lockedBox.Bottom ),
			                 _bufferBytes, 0,
			                 _bufferBytes.Length,
			                 Microsoft.Xna.Framework.Graphics.SetDataOptions.None );
		}

		public override RenderTexture GetRenderTarget( int slice )
		{
			return sliceTRT[ slice ];
		}

		#endregion HardwarePixelBuffer Implementation

		internal void SetMipmapping( bool doMipmapGen, bool MipmapsHardwareGenerated, Microsoft.Xna.Framework.Graphics.Texture texture )
		{
			this.doMipmapGen = doMipmapGen;
			this.HWMipmaps = MipmapsHardwareGenerated;
			this.mipTex = texture;
		}
	}
}
