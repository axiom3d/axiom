#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006 Axiom Project Team

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
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
//     <id value="$Id:"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using SWF = System.Windows.Forms;

using Axiom.Graphics;
using Axiom.Core;
using Axiom.Configuration;
using Axiom.Media;
using XnaF = Microsoft.Xna.Framework;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.Xna
{
    /// <summary>
    /// Summary description for XnaTexture.
    /// </summary>
    /// <remarks>When loading a cubic texture, the image with the texture base name plus the "_rt", "_lf", "_up", "_dn", "_fr", "_bk" suffixes will automaticaly be loaded to construct it.</remarks>
    class XnaTexture : Texture
    {
        #region Fields and PRoperties

        /// <summary>
        ///     Xna  GRaphicsDevice reference.
        /// </summary>
        private XnaF.Graphics.GraphicsDevice _device;
        /// <summary>
        ///     Actual texture reference.
        /// </summary>
        private XnaF.Graphics.Texture _texture;
        /// <summary>
        ///     1D/2D normal texture.
        /// </summary>
        private XnaF.Graphics.Texture2D _normTexture;
        /// <summary>
        ///     Temporary 1D/2D normal texture.
        /// </summary>
        private XnaF.Graphics.Texture2D tempNormTexture;

        /// <summary>
        ///     Back buffer pixel format.
        /// </summary>
        private XnaF.Graphics.SurfaceFormat _bbPixelFormat;
        /// <summary>
        ///     Direct3D device creation parameters.
        /// </summary>
        private XnaF.Graphics.GraphicsDeviceCreationParameters _devParms;
        /// <summary>
        ///     Direct3D device capability structure.
        /// </summary>
        private XnaF.Graphics.GraphicsDeviceCapabilities _devCaps;
        /// <summary>
        ///     Array to hold texture names used for loading cube textures.
        /// </summary>
        private string[] _cubeFaceNames = new string[ 6 ];


        /// <summary>
        ///		Gets the D3D Texture that is contained withing this Texture.
        /// </summary>
        public XnaF.Graphics.Texture Texture
        {
            get
            {
                return _texture;
            }
        }

        public XnaF.Graphics.Texture2D NormalTexture
        {
            get
            {
                return _normTexture;
            }
        }

        #endregion Fields and Properties

        #region Constructors & Destructors

        public XnaTexture( string name, XnaF.Graphics.GraphicsDevice device, TextureUsage usage, TextureType type )
            : this( name, device, type, 0, 0, 0, PixelFormat.Unknown, usage )
        {
        }

        public XnaTexture( string name, XnaF.Graphics.GraphicsDevice device, TextureType type, int width, int height, int numMipMaps, PixelFormat format, TextureUsage usage )
        {
            Debug.Assert( device != null, "Cannot create a texture without a valid Xna Device." );
            this._device = device;

            this.name = name;
            this.usage = usage;
            this.textureType = type;

            // set the name of the cubemap faces
            if ( this.TextureType == TextureType.CubeMap )
            {
                _constructCubeFaceNames( name );
            }

            // get device caps
            _devCaps = _device.GraphicsDeviceCapabilities;

            // save off the params used to create the Direct3D device
            _devParms = _device.CreationParameters;

            // get the pixel format of the back buffer
            XnaF.Graphics.Texture2D back;
            //device.ResolveBackBuffer( back, 0 );
            //_bbPixelFormat = back.Format;

            _setSrcAttributes( width, height, 1, format );

            // if render target, create the texture up front
            if ( usage == TextureUsage.RenderTarget )
            {
                //CreateTexture();
                isLoaded = true;
            }
        }
        #endregion Constructors & Destructors

        #region Private Methods

        /// <summary>
        ///  
        /// </summary>
        private void _constructCubeFaceNames( string name )
        {
            string baseName, ext;
            string[] postfixes = { "_rt", "_lf", "_up", "_dn", "_fr", "_bk" };

            int pos = name.LastIndexOf( "." );

            baseName = name.Substring( 0, pos );
            ext = name.Substring( pos );

            for ( int i = 0; i < 6; i++ )
            {
                _cubeFaceNames[ i ] = baseName + postfixes[ i ] + ext;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="depth"></param>
        /// <param name="format"></param>
        private void _setSrcAttributes( int width, int height, int depth, PixelFormat format )
        {
            srcWidth = width;
            srcHeight = height;
            srcBpp = Image.GetNumElemBits( format );
            hasAlpha = Image.FormatHasAlpha( format );
        }

        #region Loading Helper Functions

        /// <summary>
        ///    
        /// </summary>
        private void _loadNormalTexture()
        {
            Debug.Assert( textureType == TextureType.OneD || textureType == TextureType.TwoD );

            Stream stream = TextureManager.Instance.FindResourceData( name );

            // use Xna to load the image directly from the stream
            _normTexture = (XnaF.Graphics.Texture2D)XnaF.Graphics.Texture2D.FromFile( _device, stream );

            // store a ref for the base texture interface
            _texture = _normTexture;

            // set the image data attributes
            _setSrcAttributes( _normTexture.Width, _normTexture.Height, 1, ConvertFormat( _normTexture.Format ) );
            //_setFinalAttributes( desc.Width, desc.Height, 1, ConvertFormat( de_normTexturesc.Format ) );

            isLoaded = true;
        }

        #endregion Loading Helper Functions

        #region Create Helper Functions

        /// <summary>
        /// 
        /// </summary>
        private void _createTexture()
        {
            Debug.Assert( srcWidth > 0 && srcHeight > 0 );

            switch ( this.TextureType )
            {
                case TextureType.OneD:
                case TextureType.TwoD:
                    _createNormalTexture();
                    break;

                //case TextureType.CubeMap:
                //    _createCubeTexture();
                //    break;

                default:
                    throw new Exception( "Unknown texture type!" );
            }
        }

        private void _createNormalTexture()
        {
            Debug.Assert( srcWidth > 0 && srcHeight > 0 );

            // use current back buffer format for render textures, else use the one
            // defined by this texture format
            XnaF.Graphics.SurfaceFormat xnaPixelFormat = ( usage == TextureUsage.RenderTarget ) ? _bbPixelFormat : _chooseXnaFormat();

            // set the appropriate usage based on the usage of this texture
            XnaF.Graphics.ResourceUsage xnaUsage = ( usage == TextureUsage.RenderTarget ) ? XnaF.Graphics.ResourceUsage.ResolveTarget : 0;

            // how many mips to use?  make sure its at least one
            int numMips = ( numMipMaps > 0 ) ? numMipMaps : 1;

            if ( _devCaps.TextureCapabilities.SupportsMipMap && numMipMaps > 0 )
            {
                if ( this._canAutoGenMipMaps( xnaUsage, XnaF.Graphics.ResourceType.Texture2D, xnaPixelFormat ) )
                {
                    xnaUsage |= XnaF.Graphics.ResourceUsage.AutoGenerateMipMap;
                    numMips = 0;
                }
                else
                {
                }
            }
            else
            {
                // no mip map support for this kind of texture
                numMipMaps = 0;
                numMips = 1;
            }


            // create the texture
            _normTexture = new XnaF.Graphics.Texture2D( _device, srcWidth, srcHeight, numMips, xnaUsage, xnaPixelFormat );


            // store base reference to the texture
            _texture = _normTexture;

            if ( usage == TextureUsage.RenderTarget )
            {
                //CreateDepthStencil();
            }
        }

        private bool _canAutoGenMipMaps( Microsoft.Xna.Framework.Graphics.ResourceUsage xnaUsage, object p, Microsoft.Xna.Framework.Graphics.SurfaceFormat xnaPixelFormat )
        {
            Debug.Assert( _device != null );

            if ( _device.GraphicsDeviceCapabilities.DriverCapabilities.CanAutoGenerateMipMap )
            {
                // make sure we can do it!
                //return device.CheckDeviceFormat(
                //    devParms.AdapterOrdinal,
                //    devParms.DeviceType,
                //    bbPixelFormat,
                //    srcUsage | XnaF.Graphics.ResourceUsage.AutoGenerateMipMap,
                //    srcType,
                //    srcFormat );
                return true;
            }

            return false;
        }


        #endregion Create Helper Functions

        private void _blitImageToNormalTexture( Image image )
        {
        }

        private XnaF.Graphics.SurfaceFormat _chooseXnaFormat()
        {
            if ( finalBpp > 16 && hasAlpha )
            {
                return XnaF.Graphics.SurfaceFormat.Rgba32;
            }
            else if ( finalBpp > 16 && !hasAlpha )
            {
                return XnaF.Graphics.SurfaceFormat.Bgr32;
            }
            else if ( finalBpp == 16 && hasAlpha )
            {
                return XnaF.Graphics.SurfaceFormat.Bgra4444;
            }
            else if ( finalBpp == 16 && !hasAlpha )
            {
                return XnaF.Graphics.SurfaceFormat.Bgr565;
            }
            else
            {
                throw new Exception( "Unknown pixel format!" );
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        public PixelFormat ConvertFormat( XnaF.Graphics.SurfaceFormat format )
        {
            switch ( format )
            {
                case XnaF.Graphics.SurfaceFormat.Alpha8:
                    return PixelFormat.A8;
                case XnaF.Graphics.SurfaceFormat.LuminanceAlpha8:
                    return PixelFormat.A4L4;
                case XnaF.Graphics.SurfaceFormat.Bgra4444:
                    return PixelFormat.A4R4G4B4;
                case XnaF.Graphics.SurfaceFormat.Color:
                    return PixelFormat.A8R8G8B8;
                case XnaF.Graphics.SurfaceFormat.Bgra1010102:
                    return PixelFormat.A2R10G10B10;
                case XnaF.Graphics.SurfaceFormat.Luminance8:
                    return PixelFormat.L8;
                case XnaF.Graphics.SurfaceFormat.Bgr555:
                case XnaF.Graphics.SurfaceFormat.Bgr565:
                    return PixelFormat.R5G6B5;
                case XnaF.Graphics.SurfaceFormat.Bgr32:
                case XnaF.Graphics.SurfaceFormat.Bgr24:
                    return PixelFormat.R8G8B8;
            }

            return PixelFormat.Unknown;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        public XnaF.Graphics.SurfaceFormat ConvertFormat( PixelFormat format )
        {
            switch ( format )
            {
                case PixelFormat.L8:
                    return XnaF.Graphics.SurfaceFormat.Luminance8;
                case PixelFormat.A8:
                    return XnaF.Graphics.SurfaceFormat.Alpha8;
                case PixelFormat.B5G6R5:
                case PixelFormat.R5G6B5:
                    return XnaF.Graphics.SurfaceFormat.Bgr565;
                case PixelFormat.B4G4R4A4:
                case PixelFormat.A4R4G4B4:
                    return XnaF.Graphics.SurfaceFormat.Bgra4444;
                case PixelFormat.B8G8R8:
                case PixelFormat.R8G8B8:
                    return XnaF.Graphics.SurfaceFormat.Bgr24;
                case PixelFormat.B8G8R8A8:
                case PixelFormat.A8R8G8B8:
                    return XnaF.Graphics.SurfaceFormat.Bgr32;
                case PixelFormat.L4A4:
                case PixelFormat.A4L4:
                    return XnaF.Graphics.SurfaceFormat.LuminanceAlpha8;
                case PixelFormat.B10G10R10A2:
                case PixelFormat.A2R10G10B10:
                    return XnaF.Graphics.SurfaceFormat.Bgra1010102;
            }

            return XnaF.Graphics.SurfaceFormat.Unknown;
        }

        #endregion

        #region Axiom.Core.Texture Implementation

        public override void LoadImage( Image image )
        {
            // we need src image info
            this._setSrcAttributes( image.Width, image.Height, 1, image.Format );
            // create a blank texture
            this._createNormalTexture();
            // set gamma prior to blitting
            Image.ApplyGamma( image.Data, this.gamma, image.Size, image.BitsPerPixel );
            this._blitImageToNormalTexture( image );
            isLoaded = true;
        }

        public override void Load()
        {
            try
            {
                // unload if loaded already
                if ( isLoaded )
                {
                    Unload();
                }

                // log a quick message
                LogManager.Instance.Write( "XNATexture: Loading {0} with {1} mipmaps from an Image.", name, numMipMaps );

                // create a render texture if need be
                if ( usage == TextureUsage.RenderTarget )
                {
                    _createTexture();
                    isLoaded = true;
                    return;
                }

                // create a regular texture
                switch ( this.TextureType )
                {
                    case TextureType.OneD:
                    case TextureType.TwoD:
                        _loadNormalTexture();
                        break;

                    //case TextureType.ThreeD:
                    //    LoadVolumeTexture();
                    //    break;

                    //case TextureType.CubeMap:
                    //    LoadCubeTexture();
                    //    break;

                    default:
                        throw new Exception( "Unsupported texture type!" );
                }

                isLoaded = true;
            }
            catch ( FileNotFoundException e )
            {
                if ( e.Message.StartsWith( "File or assembly" ) && e.Message.IndexOf( "DirectX" ) != -1 )
                {
                    string message = "You do not have the correct release version of DirectX 9.0c or else XNA is not installed. "
                        + "See the README.txt file for more information on the version required and a link to download it "
                        + "or to recompile the Axiom.RenderSystems.Xna project against the version that you do have installed.";
                    System.Windows.Forms.MessageBox.Show( message );
                    throw new AxiomException( message );
                }
                else
                    throw e;
            }
        }

        #endregion Axiom.Core.Texture Implementation

        #region IDisposable Implementation
        /// <summary>
        /// 
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();

            if ( _texture != null )
                _texture.Dispose();
        }

        #endregion IDisposable Implementation
    }
}
