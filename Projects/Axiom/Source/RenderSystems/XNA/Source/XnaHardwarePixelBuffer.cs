#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006  Axiom Project Team

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
using System.Drawing;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

using VertexDeclaration = Axiom.Graphics.VertexDeclaration;
using Root = Axiom.Core.Root;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Media;

using XNA = Microsoft.Xna.Framework;
using XFG = Microsoft.Xna.Framework.Graphics;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9
{
    /// <summary>
    /// 	Xna implementation of HardwarePixelBuffer
    /// </summary>
    public class XnaHardwarePixelBuffer : HardwarePixelBuffer
    {

        #region Fields

        protected TimingMeter timingMeter = MeterManager.GetMeter( "BlitFromMemory", "XnaHardwarePixelBuffer" );

        ///<summary>
        ///    XFGDevice pointer
        ///</summary>
        protected XFG.GraphicsDevice device;
        ///<summary>
        ///    Surface abstracted by this buffer
        ///</summary>
        protected XFG.Surface surface;
        ///<summary>
        ///    Volume abstracted by this buffer
        ///</summary>
        protected XFG.Volume volume;
        ///<summary>
        ///    Temporary surface in main memory if direct locking of mSurface is not possible
        ///</summary>
        protected XFG.Surface tempSurface;
        ///<summary>
        ///    Temporary volume in main memory if direct locking of mVolume is not possible
        ///</summary>
        protected XFG.Volume tempVolume;
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

        #endregion Fields

        #region Constructors

        public XnaHardwarePixelBuffer( BufferUsage usage )
            :
            base( 0, 0, 0, Axiom.Media.PixelFormat.Unknown, usage, false, false )
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
        public XFG.Surface Surface
        {
            get
            {
                return surface;
            }
        }

        #endregion Properties

        #region Methods

        ///<summary>
        ///    Call this to associate a Xna surface with this pixel buffer
        ///</summary>
        public void Bind( XFG.Device device, XFG.Surface surface, bool update )
        {
            this.device = device;
            this.surface = surface;

            XFG.SurfaceDescription desc = surface.Description;
            Width = desc.Width;
            Height = desc.Height;
            Depth = 1;
            Format = XFGHelper.ConvertEnum( desc.Format );
            // Default
            RowPitch = Width;
            SlicePitch = Height * Width;
            sizeInBytes = PixelUtil.GetMemorySize( Width, Height, Depth, Format );

            if ( ( (int)usage & (int)TextureUsage.RenderTarget ) != 0 )
                CreateRenderTextures( update );
        }

        ///<summary>
        ///    Call this to associate a XFG volume with this pixel buffer
        ///</summary>
        public void Bind( XFG.Device device, XFG.Volume volume, bool update )
        {
            this.device = device;
            this.volume = volume;

            XFG.VolumeDescription desc = volume.Description;
            Width = desc.Width;
            Height = desc.Height;
            Depth = desc.Depth;
            Format = XFGHelper.ConvertEnum( desc.Format );
            // Default
            RowPitch = Width;
            SlicePitch = Height * Width;
            sizeInBytes = PixelUtil.GetMemorySize( Width, Height, Depth, Format );

            if ( ( (int)usage & (int)TextureUsage.RenderTarget ) != 0 )
                CreateRenderTextures( update );
        }

        ///<summary>
        ///    Util functions to convert a XFG locked rectangle to a pixel box
        ///</summary>
        protected static void FromXFGLock( PixelBox rval, int pitch, DX.GraphicsStream stream )
        {
            rval.RowPitch = pitch / PixelUtil.GetNumElemBytes( rval.Format );
            rval.SlicePitch = rval.RowPitch * rval.Height;
            Debug.Assert( ( pitch % PixelUtil.GetNumElemBytes( rval.Format ) ) == 0 );
            rval.Data = stream.InternalData;
        }

        ///<summary>
        ///    Util functions to convert a XFG LockedBox to a pixel box
        ///</summary>
        protected static void FromXFGLock( PixelBox rval, XFG.LockedBox lbox, XFG.GraphicsStream stream )
        {
            rval.RowPitch = lbox.RowPitch / PixelUtil.GetNumElemBytes( rval.Format );
            rval.SlicePitch = lbox.SlicePitch / PixelUtil.GetNumElemBytes( rval.Format );
            Debug.Assert( ( lbox.RowPitch % PixelUtil.GetNumElemBytes( rval.Format ) ) == 0 );
            Debug.Assert( ( lbox.SlicePitch % PixelUtil.GetNumElemBytes( rval.Format ) ) == 0 );
            rval.Data = stream.InternalData;
        }

        ///<summary>
        ///    Convert Ogre integer Box to XFG rectangle
        ///</summary>
        protected static System.Drawing.Rectangle ToXFGRectangle( BasicBox lockBox )
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
        ///    Convert Axiom Box to XFG box
        ///</summary>
        protected static XFG.Box ToXFGBox( BasicBox lockBox )
        {
            XFG.Box pbox = new XFG.Box();
            pbox.Left = lockBox.Left;
            pbox.Right = lockBox.Right;
            pbox.Top = lockBox.Top;
            pbox.Bottom = lockBox.Bottom;
            pbox.Front = lockBox.Front;
            pbox.Back = lockBox.Back;
            return pbox;
        }

        ///<summary>
        ///    Convert Axiom PixelBox extent to XFG rectangle
        ///</summary>
        protected static System.Drawing.Rectangle ToXFGRectangleExtent( PixelBox lockBox )
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
        ///    Convert Axiom PixelBox extent to XFG box
        ///</summary>
        protected static XFG.Box ToXFGBoxExtent( PixelBox lockBox )
        {
            XFG.Box pbox = new XFG.Box();
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
                throw new Exception( "DirectX does not allow locking of or directly writing to RenderTargets. Use BlitFromMemory if you need the contents; " +
                                    "in XFG9HardwarePixelBuffer.LockImpl" );
            // Set extents and format
            PixelBox rval = new PixelBox( lockBox, Format );
            // Set locking flags according to options
            XFG.LockFlags flags = XFG.LockFlags.None;
            switch ( options )
            {
                case BufferLocking.Discard:
                    // XFG only likes XFG.LockFlags.Discard if you created the texture with XFGUSAGE_DYNAMIC
                    // debug runtime flags this up, could cause problems on some drivers
                    if ( ( usage & BufferUsage.Dynamic ) != 0 )
                        flags |= XFG.LockFlags.Discard;
                    break;
                case BufferLocking.ReadOnly:
                    flags |= XFG.LockFlags.ReadOnly;
                    break;
                default:
                    break;
            }

            if ( surface != null )
            {
                // Surface
                DX.GraphicsStream data = null;
                int pitch = 0;
                if ( lockBox.Left == 0 && lockBox.Top == 0 &&
                    lockBox.Right == Width && lockBox.Bottom == Height )
                {
                    // Lock whole surface
                    data = surface.LockRectangle( flags, out pitch );
                }
                else
                {
                    System.Drawing.Rectangle prect = ToXFGRectangle( lockBox ); // specify range to lock
                    data = surface.LockRectangle( prect, flags, out pitch );
                }
                if ( data == null )
                    throw new Exception( "Surface locking failed; in XFG9HardwarePixelBuffer.LockImpl" );
                FromXFGLock( rval, pitch, data );
            }
            else
            {
                // Volume
                XFG.Box pbox = ToXFGBox( lockBox ); // specify range to lock
                XFG.LockedBox lbox; // Filled in by XFG

                DX.GraphicsStream data = volume.LockBox( pbox, flags, out lbox );
                FromXFGLock( rval, lbox, data );
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
                foreach ( XFGRenderTexture trt in sliceTRT )
                    trt.Rebind( this );
                return;
            }

            DestroyRenderTextures();
            if ( surface == null )
                throw new Exception( "Rendering to 3D slices not supported yet for Direct3D; in " +
                                    "XFG9HardwarePixelBuffer.CreateRenderTexture" );
            // Create render target for each slice
            sliceTRT.Clear();
            Debug.Assert( Depth == 1 );
            for ( int zoffset = 0; zoffset < Depth; ++zoffset )
            {
                string name = "rtt/" + this.ID;
                RenderTexture trt = new XFGRenderTexture( name, this );
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
                RenderTexture trt = sliceTRT[i];
                if ( trt != null )
                    Root.Instance.RenderSystem.DestroyRenderTarget( trt.Name );
            }
            // sliceTRT.Clear();
        }

        ///<summary>
        ///    @copydoc HardwarePixelBuffer.Blit
        ///</summary>
        public override void Blit( HardwarePixelBuffer _src, BasicBox srcBox, BasicBox dstBox )
        {
            XnaHardwarePixelBuffer src = (XnaHardwarePixelBuffer)_src;
            if ( surface != null && src.surface != null )
            {
                // Surface-to-surface
                System.Drawing.Rectangle dsrcRect = ToXFGRectangle( srcBox );
                System.Drawing.Rectangle ddestRect = ToXFGRectangle( dstBox );
                // XFGXLoadSurfaceFromSurface
                XFG.SurfaceLoader.FromSurface( surface, ddestRect, src.surface, dsrcRect, XFG.Filter.None, 0 );
            }
            else if ( volume != null && src.volume != null )
            {
                // Volume-to-volume
                XFG.Box dsrcBox = ToXFGBox( srcBox );
                XFG.Box ddestBox = ToXFGBox( dstBox );
                // XFGXLoadVolumeFromVolume
                XFG.VolumeLoader.FromVolume( volume, ddestBox, src.volume, dsrcBox, XFG.Filter.None, 0 );
            }
            else
                // Software fallback   
                base.Blit( _src, srcBox, dstBox );
        }

        ///<summary>
        ///    @copydoc HardwarePixelBuffer.BlitFromMemory
        ///</summary>
        public override void BlitFromMemory( PixelBox src, BasicBox dstBox )
        {
            using ( AutoTimer timer = new AutoTimer( timingMeter ) )
            {
                BlitFromMemoryImpl( src, dstBox );
            }
        }

        protected void BlitFromMemoryImpl( PixelBox src, BasicBox dstBox )
        {
            // TODO: This currently does way too many copies.  We copy
            // from src to a converted buffer (if needed), then from 
            // converted to a byte array, then into the temporary surface,
            // and finally from the temporary surface to the real surface.
            PixelBox converted = src;
            GCHandle bufGCHandle = new GCHandle();
            // convert to pixelbuffer's native format if necessary
            if ( XFGHelper.ConvertEnum( src.Format ) == XFG.Format.Unknown )
            {
                int bufSize = PixelUtil.GetMemorySize( src.Width, src.Height, src.Depth, Format );
                byte[] newBuffer = new byte[bufSize];
                bufGCHandle = GCHandle.Alloc( newBuffer, GCHandleType.Pinned );
                converted = new PixelBox( src.Width, src.Height, src.Depth, Format, bufGCHandle.AddrOfPinnedObject() );
                PixelConverter.BulkPixelConversion( src, converted );
            }

            // int formatBytes = PixelUtil.GetNumElemBytes(converted.Format);
            XFG.Surface tmpSurface = device.CreateOffscreenPlainSurface( converted.Width, converted.Height, XFGHelper.ConvertEnum( converted.Format ), XFG.Pool.Scratch );
            int pitch;
            // Ideally I would be using the Array mechanism here, but that doesn't seem to work
            DX.GraphicsStream buf = tmpSurface.LockRectangle( XFG.LockFlags.NoSystemLock, out pitch );
            buf.Position = 0;
            unsafe
            {
                int bufSize = PixelUtil.GetMemorySize( converted.Width, converted.Height, converted.Depth, converted.Format );
                byte* srcPtr = (byte*)converted.Data.ToPointer();
                byte[] ugh = new byte[bufSize];
                for ( int i = 0; i < bufSize; ++i )
                    ugh[i] = srcPtr[i];
                buf.Write( ugh );
            }
            tmpSurface.UnlockRectangle();
            buf.Dispose();

            //ImageInformation imageInfo = new ImageInformation();
            //imageInfo.Format = XFGHelper.ConvertEnum(converted.Format);
            //imageInfo.Width = converted.Width;
            //imageInfo.Height = converted.Height;
            //imageInfo.Depth = converted.Depth;
            if ( surface != null )
            {
                // I'm trying to write to surface using the data in converted
                System.Drawing.Rectangle srcRect = ToXFGRectangleExtent( converted );
                System.Drawing.Rectangle destRect = ToXFGRectangle( dstBox );
                XFG.SurfaceLoader.FromSurface( surface, destRect, tmpSurface, srcRect, XFG.Filter.None, 0 );
            }
            else
            {
                XFG.Box srcBox = ToXFGBoxExtent( converted );
                XFG.Box destBox = ToXFGBox( dstBox );
                Debug.Assert( false, "Volume textures not yet supported" );
                // VolumeLoader.FromStream(volume, destBox, converted.Data, converted.RowPitch * converted.SlicePitch * formatBytes, srcBox, Filter.None, 0);
                XFG.VolumeLoader.FromStream( volume, destBox, buf, srcBox, XFG.Filter.None, 0 );
            }

            tmpSurface.Dispose();

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
        ///    @copydoc HardwarePixelBuffer.BlitToMemory
        ///</summary>
        public override void BlitToMemory( BasicBox srcBox, PixelBox dst )
        {
            // Decide on pixel format of temp surface
            PixelFormat tmpFormat = Format;
            if ( XFGHelper.ConvertEnum( dst.Format ) == XFG.Format.Unknown )
                tmpFormat = dst.Format;
            if ( surface != null )
            {
                Debug.Assert( srcBox.Depth == 1 && dst.Depth == 1 );
                // Create temp texture
                XFG.Texture tmp =
                    new XFG.Texture( device, dst.Width, dst.Height,
                                    1, // 1 mip level ie topmost, generate no mipmaps
                                    0, XFGHelper.ConvertEnum( tmpFormat ),
                                    XFG.Pool.Scratch );
                XFG.Surface subSurface = tmp.GetSurfaceLevel( 0 );
                // Copy texture to this temp surface
                System.Drawing.Rectangle destRect, srcRect;
                srcRect = ToXFGRectangle( srcBox );
                destRect = ToXFGRectangleExtent( dst );

                XFG.SurfaceLoader.FromSurface( subSurface, destRect, surface, srcRect, XFG.Filter.None, 0 );

                // Lock temp surface and copy it to memory
                int pitch; // Filled in by XFG
                DX.GraphicsStream data = subSurface.LockRectangle( XFG.LockFlags.ReadOnly, out pitch );
                // Copy it
                PixelBox locked = new PixelBox( dst.Width, dst.Height, dst.Depth, tmpFormat );
                FromXFGLock( locked, pitch, data );
                PixelConverter.BulkPixelConversion( locked, dst );
                subSurface.UnlockRectangle();
                // Release temporary surface and texture
                subSurface.Dispose();
                tmp.Dispose();
            }
            else
            {
                // Create temp texture
                XFG.VolumeTexture tmp =
                    new XFG.VolumeTexture( device, dst.Width, dst.Height, dst.Depth,
                                          0, XFG.Usage.None,
                                          XFGHelper.ConvertEnum( tmpFormat ),
                                          XFG.Pool.Scratch );
                XFG.Volume subVolume = tmp.GetVolumeLevel( 0 );
                // Volume
                XFG.Box ddestBox = ToXFGBoxExtent( dst );
                XFG.Box dsrcBox = ToXFGBox( srcBox );

                XFG.VolumeLoader.FromVolume( subVolume, ddestBox, volume, dsrcBox, XFG.Filter.None, 0 );
                // Lock temp surface and copy it to memory
                XFG.LockedBox lbox; // Filled in by XFG
                DX.GraphicsStream data = subVolume.LockBox( XFG.LockFlags.ReadOnly, out lbox );
                // Copy it
                PixelBox locked = new PixelBox( dst.Width, dst.Height, dst.Depth, tmpFormat );
                FromXFGLock( locked, lbox, data );
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
                // Hardware mipmaps
                mipTex.GenerateMipSubLevels();
            else
            {
                // Software mipmaps
                XFG.TextureLoader.FilterTexture( mipTex, 0, XFG.Filter.Box );
            }
        }

        ///<summary>
        ///    Function to set mipmap generation
        ///</summary>
        public void SetMipmapping( bool doMipmapGen, bool HWMipmaps, XFG.BaseTexture mipTex )
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
            return sliceTRT[zoffset];
        }

        ///<summary>
        ///    Notify TextureBuffer of destruction of render target
        ///</summary>
        public override void ClearSliceRTT( int zoffset )
        {
            sliceTRT[zoffset] = null;
        }

        protected override void dispose( bool disposeManagedResources )
        {
            if ( !isDisposed )
            {
                if ( disposeManagedResources )
                {
                }
                DestroyRenderTextures();
            }
            isDisposed = true;

            // If it is available, make the call to the
            // base class's Dispose(Boolean) method
            base.dispose( disposeManagedResources );
        }

        #endregion Methods

    }
}
