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
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Axiom.Core;

using Axiom.Graphics;
using Axiom.Math;
using Axiom.Media;
using Microsoft.Xna.Framework.Graphics;
using BufferUsage = Axiom.Graphics.BufferUsage;
using Texture = Microsoft.Xna.Framework.Graphics.Texture;

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
        protected GraphicsDevice device;

        ///<summary>
        ///    Surface abstracted by this buffer
        ///</summary>
        protected ushort mipLevel;

        protected CubeMapFace face;

        protected Texture2D surface;

        ///<summary>
        ///    FSAA Surface abstracted by this buffer
        ///</summary>
        protected RenderTarget2D fsaaSurface;

#if !SILVERLIGHT
        ///<summary>
        ///    Volume abstracted by this buffer
        ///</summary>
        protected Texture3D volume;
#endif

        protected TextureCube cube;

        ///<summary>
        ///    Temporary surface in main memory if direct locking of mSurface is not possible
        ///</summary>
        protected Texture2D tempSurface;

#if !SILVERLIGHT
        ///<summary>
        ///    Temporary volume in main memory if direct locking of mVolume is not possible
        ///</summary>
        protected Texture3D tempVolume;
#endif

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
        protected Texture mipTex;

        ///<summary>
        ///    Render targets
        ///</summary>
        protected List<RenderTexture> sliceTRT;

        private byte[] _bufferBytes;
        private BasicBox _lockedBox;

        private RenderTarget2D renderTarget;

        public RenderTarget2D RenderTarget
        {
            get
            {
                return renderTarget;
            }
        }

        ///<summary>
        /// Accessor for surface
        ///</summary>
        public RenderTarget2D FSAASurface
        {
            get
            {
                return fsaaSurface;
            }
        }

        ///<summary>
        /// Accessor for surface
        ///</summary>
        public Texture Surface
        {
            get
            {
                return surface;
            }
            set
            {
                surface = (Texture2D)value;
            }
        }

        #endregion Fields and Properties

        #region Construction and Destruction

        public XnaHardwarePixelBuffer( BufferUsage usage )
            : base( 0, 0, 0, PixelFormat.Unknown, usage, false, false )
        {
            device = null;
            surface = null;
            tempSurface = null;
#if !SILVERLIGHT
            volume = null;
            tempVolume = null;
#endif
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
            : base( width, height, depth, format, usage, useSystemMemory, useShadowBuffer )
        {
        }

        #endregion Construction and Destruction

        #region Methods

        ///<summary>
        ///    Call this to associate a Xna Texture2D with this pixel buffer
        ///</summary>
        public void Bind( GraphicsDevice device, Texture2D surface, ushort miplevel, bool update )
        {
            this.device = device;
            this.surface = surface;
            mipLevel = miplevel;

            width = surface.Width/(int)Utility.Pow( 2, mipLevel );
            height = surface.Height/(int)Utility.Pow( 2, mipLevel );
            depth = 1;
            format = XnaHelper.Convert( surface.Format );
            // Default
            rowPitch = Width * PixelUtil.GetNumElemBytes(Format);
            slicePitch = Height * RowPitch;
            sizeInBytes = PixelUtil.GetMemorySize( Width, Height, Depth, Format );

            if ( ( (int)usage & (int)TextureUsage.RenderTarget ) != 0 )
                CreateRenderTextures( update );
        }

        public void Bind( GraphicsDevice device, TextureCube cube, ushort face, ushort miplevel, bool update )
        {
            this.device = device;
            this.cube = cube;
            this.face = (CubeMapFace)face;
            mipLevel = miplevel;

            width = cube.Size/(int)Utility.Pow( 2, mipLevel );
            height = cube.Size/(int)Utility.Pow( 2, mipLevel );
            depth = 1;
            format = XnaHelper.Convert( cube.Format );
            // Default
            rowPitch = Width;
            slicePitch = Height*Width;
            sizeInBytes = PixelUtil.GetMemorySize( Width, Height, Depth, Format );

            if ( ( (int)usage & (int)TextureUsage.RenderTarget ) != 0 )
            {
                CreateRenderTextures( update );
            }
        }

        public void Bind( GraphicsDevice device, RenderTarget2D surface, bool update )
        {
            this.device = device;
            renderTarget = surface;

            width = surface.Width/(int)Utility.Pow( 2, mipLevel );
            height = surface.Height/(int)Utility.Pow( 2, mipLevel );
            depth = 1;
            format = XnaHelper.Convert( surface.Format );
            // Default
            rowPitch = Width;
            slicePitch = Height*Width;
            sizeInBytes = PixelUtil.GetMemorySize( Width, Height, Depth, Format );

            if ( ( (int)usage & (int)TextureUsage.RenderTarget ) != 0 )
                CreateRenderTextures( update );
        }

#if !SILVERLIGHT
        ///<summary>
        ///    Call this to associate a Xna Texture3D with this pixel buffer
        ///</summary>
        public void Bind( GraphicsDevice device, Texture3D volume, bool update )
        {
            this.device = device;
            this.volume = volume;

            width = volume.Width;
            height = volume.Height;
            depth = volume.Depth;
            format = XnaHelper.Convert( volume.Format );
            // Default
            rowPitch = Width;
            slicePitch = Height*Width;
            sizeInBytes = PixelUtil.GetMemorySize( Width, Height, Depth, Format );

            if ( ( (int)usage & (int)TextureUsage.RenderTarget ) != 0 )
                CreateRenderTextures( update );
        }
#endif

        ///<summary>
        ///    Create (or update) render textures for slices
        ///</summary>
        ///<param name="update">are we updating an existing texture</param>
        protected void CreateRenderTextures( bool update )
        {
            if ( update )
            {
                Debug.Assert( sliceTRT.Count == Depth );
                foreach ( XnaRenderTexture trt in sliceTRT )
                    trt.Rebind( this );
                return;
            }

            DestroyRenderTextures();
            // Create render target for each slice
            sliceTRT.Clear();
            Debug.Assert( Depth == 1 );
            for ( var zoffset = 0; zoffset < Depth; ++zoffset )
            {
                var name = "rtt/" + ID;
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
            if ( sliceTRT.Count == 0 )
                return;

            for ( var i = 0; i < sliceTRT.Count; ++i )
            {
                var trt = sliceTRT[ i ];
                if ( trt != null )
                    Root.Instance.RenderSystem.DestroyRenderTarget( trt.Name );
            }
        }

        ///<summary>
        /// Internal function to update mipmaps on update of level 0
        ///</summary>
        public void GenMipmaps()
        {
            Debug.Assert( mipTex != null );
            // Mipmapping
            //mipTex.GenerateMipMaps( XFG.TextureFilter.Linear );
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
            var converted = src;
            var bufGCHandle = new GCHandle();
            var bufSize = 0;

            // Get src.Data as byte[]
            bufSize = PixelUtil.GetMemorySize( src.Width, src.Height, src.Depth, Format );
            var newBuffer = new byte[bufSize];
            //bufGCHandle = GCHandle.Alloc( newBuffer, GCHandleType.Pinned );
            //XnaHelper.Convert(XFG.SurfaceFormat) would never have returned SurfaceFormat.Unknown anyway...
            //if (XnaHelper.Convert(src.Format) != XFG.SurfaceFormat.Unknown)
            {
                converted = new PixelBox( src.Width, src.Height, src.Depth, Format, BufferBase.Wrap( newBuffer ) );
                PixelConverter.BulkPixelConversion( src, converted );
            }
            //else
            //{
            //    Memory.Copy(converted.Data, BufferBase.Wrap(newBuffer), bufSize);
            //}

            if ( surface != null )
            {
                surface.SetData( mipLevel, XnaHelper.ToRectangle( dstBox ), newBuffer, 0, bufSize );
            }
            else if ( cube != null )
            {
                cube.SetData( face, mipLevel, XnaHelper.ToRectangle( dstBox ), newBuffer, 0, bufSize );
            }
            else
            {
                throw new NotSupportedException( "BlitFromMemory on Volume Textures not supported." );
            }

            // If we allocated a buffer for the temporary conversion, free it here
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
        }

        ///<summary>
        ///    Internal implementation of <see cref="HardwareBuffer.Lock"/>.
        ///</summary>
        protected override PixelBox LockImpl( BasicBox lockBox, BufferLocking options )
        {
            _lockedBox = lockBox;
            // Set extents and format
            var rval = new PixelBox( lockBox, Format );
            var sizeInBytes = PixelUtil.GetMemorySize( lockBox.Width, lockBox.Height, lockBox.Depth,
                                                       XnaHelper.Convert( surface.Format ) );
            if ( _bufferBytes == null || _bufferBytes.Length != sizeInBytes )
            {
                _bufferBytes = new byte[sizeInBytes];
#if !SILVERLIGHT
                if ( surface != null )
                    surface.GetData( mipLevel, XnaHelper.ToRectangle( lockBox ), _bufferBytes, 0, _bufferBytes.Length );
                else if ( cube != null )
                    cube.GetData( face, mipLevel, XnaHelper.ToRectangle( lockBox ), _bufferBytes, 0, _bufferBytes.Length );
                else
                    volume.GetData( mipLevel, lockBox.Left, lockBox.Top, lockBox.Right, lockBox.Bottom,
                                    lockBox.Front, lockBox.Back, _bufferBytes, 0, _bufferBytes.Length );
#endif
            }

            rval.Data = BufferBase.Wrap( _bufferBytes );

            return rval;
        }

        /// <summary>
        ///     Internal implementation of <see cref="HardwareBuffer.Unlock"/>.
        /// </summary>
        protected override void UnlockImpl()
        {
            //set the bytes array inside the texture
            if (surface != null)
                surface.SetData(mipLevel, XnaHelper.ToRectangle(_lockedBox), _bufferBytes, 0, _bufferBytes.Length);
            else if (cube != null)
                cube.SetData(face, mipLevel, XnaHelper.ToRectangle(_lockedBox), _bufferBytes, 0, _bufferBytes.Length);
#if !SILVERLIGHT
            else
                volume.SetData( mipLevel, _lockedBox.Left, _lockedBox.Top, _lockedBox.Right, _lockedBox.Bottom, _lockedBox.Front, _lockedBox.Back, _bufferBytes, 0, _bufferBytes.Length );
#endif
        }

        public override RenderTexture GetRenderTarget( int slice )
        {
            return sliceTRT[ slice ];
        }

        #endregion HardwarePixelBuffer Implementation

        internal void SetMipmapping( bool doMipmapGen, bool MipmapsHardwareGenerated, Texture texture )
        {
            this.doMipmapGen = doMipmapGen;
            HWMipmaps = MipmapsHardwareGenerated;
            mipTex = texture;
        }
    }
}