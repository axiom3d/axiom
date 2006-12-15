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
using System.IO;

using DotNet3D.Math;

using ResourceHandle = System.UInt64;
using System.Text;
using System.Collections.Generic;

#endregion Namespace Declarations

namespace Axiom
{
    #region Enumerations

    /// <summary>
    ///    Enum identifying the texture type.
    /// </summary>
    public enum TextureType
    {
        /// <summary>
        ///    1D texture, used in combination with 1D texture coordinates.
        /// </summary>
        [ScriptEnum( "1d" )]
        OneD = 1,
        /// <summary>
        ///    2D texture, used in combination with 2D texture coordinates (default).
        /// </summary>
        [ScriptEnum( "2d" )]
        TwoD = 2,
        /// <summary>
        ///    3D volume texture, used in combination with 3D texture coordinates.
        /// </summary>
        [ScriptEnum( "3d" )]
        ThreeD = 3,
        /// <summary>
        ///    3D cube map, used in combination with 3D texture coordinates.
        /// </summary>
        [ScriptEnum( "cubic" )]
        CubeMap = 4
    }

    /// <summary>
    ///		Specifies how a texture is to be used in the engine.
    /// </summary>
    public enum TextureUsage
    {
        /// <summary>
        ///	default to automatic mipmap generation static textures
        ///	</summary>
        Default = AutoMipmap | StaticWriteOnly,
        /// <summary>
        ///	Target of rendering.  Example would be a billboard in a wrestling or sports game, or rendering a movie to a texture.
        /// setting this flag will ignore all other texture usages except TextureUsage.AutoMipMap
        ///	</summary>
        RenderTarget = 0x200,
        Static = BufferUsage.Static,
		Dynamic = BufferUsage.Dynamic,
		WriteOnly = BufferUsage.WriteOnly,
		StaticWriteOnly = BufferUsage.StaticWriteOnly, 
		DynamicWriteOnly = BufferUsage.DynamicWriteOnly,
		DynamicWriteOnlyDiscardable = BufferUsage.DynamicWriteOnlyDiscardable,
		/// mipmaps will be automatically generated for this texture
		AutoMipmap = 0x100,
    }

    #endregion Enumerations

    /// <summary>
    ///		Abstract class representing a Texture resource.
    /// </summary>
    /// <remarks>
    ///		The actual concrete subclass which will exist for a texture
    ///		is dependent on the rendering system in use (Direct3D, OpenGL etc).
    ///		This class represents the commonalities, and is the one 'used'
    ///		by programmers even though the real implementation could be
    ///		different in reality. Texture objects are created through
    ///		the 'Create' method of the TextureManager concrete subclass.
    /// </remarks>
    public abstract class Texture : Resource
    {
        #region Fields and Properties

        private bool _internalResourcesCreated = false;

        #region Width Property

        /// <summary>Width of this texture.</summary>
        private int _width;
        /// <summary>
        ///    Gets the width (in pixels) of this texture.
        /// </summary>
        /// <ogre name="getWidth" />
        /// <ogre name="setWidth" />
        public int Width
        {
            get
            {
                return width;
            }
            set
            {
                _width = value;
            }
        }

        #endregion Width Property

        #region Height Property

        /// <summary>Height of this texture.</summary>
        private int _height;
        /// <summary>
        ///    Gets the height (in pixels) of this texture.
        /// </summary>
        /// <ogre name="setHeight" />
        /// <ogre name="getHeight" />
        public int Height
        {
            get
            {
                return _height;
            }
            set
            {
                _height = value;
            }
        }

        #endregion Height Property

        #region Depth Property

        /// <summary>Depth of this texture.</summary>
        private int _depth;
        /// <summary>
        ///    Gets the depth of this texture (for volume textures).
        /// </summary>
        /// <ogre name="setDepth" />
        /// <ogre name="getDepth" />
        public int Depth
        {
            get
            {
                return _depth;
            }
            set
            {
                _depth = value;
            }
        }

        #endregion Depth Property

        #region Bpp Property

        /// <summary>Bits per pixel in this texture.</summary>
        private int _finalBpp;
        /// <summary>
        ///    Gets the bits per pixel found within this texture data.
        /// </summary>
        public int Bpp
        {
            get
            {
                return _finalBpp;
            }
            protected set
            {
                _finalBpp = value;
            }
        }

        #endregion Bpp Property

        #region HasAlpha Property

        /// <summary>Does this texture have an alpha component?</summary>
        private bool _hasAlpha;
        /// <summary>
        ///    Gets whether or not the PixelFormat of this texture contains an alpha component.
        /// </summary>
        /// <ogre name="hasAlpha" />
        public bool HasAlpha
        {
            get
            {
                return _hasAlpha;
            }
            protected set
            {
                _hasAlpha = value;
            }
        }

        #endregion HasAlpha Property

        #region Gamma Property

        /// <summary>Gamma setting for this texture.</summary>
        private float _gamma;
        /// <summary>
        ///    Gets/Sets the gamma adjustment factor for this texture.
        /// </summary>
        /// <remarks>
        ///    Must be called before any variation of Load.
        /// </remarks>
        /// <ogre name="setGamma" />
        /// <ogre name="getGamma" />
        public float Gamma
        {
            get
            {
                return gamma;
            }
            set
            {
                gamma = value;
            }
        }

        #endregion Gamma Property

        #region Format Property

        /// <summary>Pixel format of this texture.</summary>
        private PixelFormat _format;
        /// <summary>
        ///    Gets the PixelFormat of this texture.
        /// </summary>
        /// <ogre name="getFormat" />
        public PixelFormat Format
        {
            get
            {
                return _format;
            }
            protected set
            {
                _format = value;

                srcBpp = Image.GetNumElemBytes( _format );
                HasAlpha = Image.FormatHasAlpha( _format );

            }
        }

        #endregion Format Property

        #region MipmapCount Property

        /// <summary>Number of mipmaps present in this texture.</summary>
        private int _mipmapCount;
        /// <summary>
        ///    Number of mipmaps present in this texture.
        /// </summary>
        /// <ogre name="setNumMipmaps" />
        /// <ogre name="getNumMipmaps" />
        public int MipmapCount
        {
            get
            {
                return _mipmapCount;
            }
            set
            {
                _mipmapCount = value;
            }
        }

        #endregion MipmapCount Property

        #region TextureType Property

        /// <summary>Type of texture, i.e. 1D, 2D, Cube, Volume.</summary>
        private TextureType _textureType;
        /// <summary>
        ///    Type of texture, i.e. 2d, 3d, cubemap.
        /// </summary>
        /// <ogre name="setTextureType" />
        /// <ogre name="getTextureType" />
        public TextureType TextureType
        {
            get
            {
                return _textureType;
            }
            set
            {
                _textureType = value;
            }
        }

        #endregion TextureType Property

        #region Usage Property

        /// <summary>Specifies how this texture will be used.</summary>
        private TextureUsage _usage;
        /// <summary>
        ///     Gets the intended usage of this texture, whether for standard usage
        ///     or as a render target.
        /// </summary>
        /// <ogre name="setUsage" />
        /// <ogre name="getUsage" />
        public TextureUsage Usage
        {
            get
            {
                return _usage;
            }
            set
            {
                _usage = value;
            }
        }

        #endregion Usage Property

        #region SrcWidth Property

        /// <summary>Original source width if this texture had been modified.</summary>
        private int _srcWidth;
        /// <summary>Original source width if this texture had been modified.</summary>
        /// <ogre name="geteSrcWidth" />
        public int SrcWidth
        {
            get
            {
                return _srcWidth;
            }
            protected set
            {
                _srcWidth = value;
            }
        }

        #endregion SrcWidth Property

        #region SrcHeight Property

        /// <summary>Original source height if this texture had been modified.</summary>
        private int _srcHeight;
        /// <summary>Original source height if this texture had been modified.</summary>
        /// <ogre name="getSrcHeight" />
        public int SrcHeight
        {
            get
            {
                return _srcHeight;
            }
            protected set
            {
                _srcHeight = value;
            }
        }

        #endregion SrcHeight Property

        #region SrcBpp Property

        /// <summary>Original source bits per pixel if this texture had been modified.</summary>
        private int _srcBpp;
        /// <summary>Original source bits per pixel if this texture had been modified.</summary>
        public int srcBpp
        {
            get
            {
                return _srcBpp;
            }
            protected set
            {
                _srcBpp = value;
            }
        }

        #endregion SrcBpp Property

        #region SrcDepth Property

        /// <summary>Original depth of the input texture (only applicable for 3D textures).</summary>
        private int _srcDepth;
        /// <summary>Original depth of the input texture (only applicable for 3D textures).</summary>
        /// <ogre name="getSrcDepth" />
        public int SrcDepth
        {
            get
            {
                return _srcDepth;
            }
            protected set
            {
                _srcDepth = value;
            }
        }

        #endregion SrcDepth Property

        /// <summary>
        ///    Specifies whether this texture is 32 bits or not.
        /// </summary>
        /// <ogre name="enable32Bit" />
        public bool Is32Bit
        {
            get
            {
                return ( _finalBpp == 32 );
            }
            set
            {
                _finalBpp = value ? 32 : 16;
            }
        }

        /// <summary>
        /// Return the number of faces this texture has. This will be 6 for a cubemap texture and 1 for a 1D, 2D or 3D one.
        /// </summary>
        protected int faceCount
        {
            get
            {
                return ( TextureType == TextureType.CubeMap ) ? 6 : 1;
            }
        }

        #endregion Fields and Properties

        #region Constructors and Destructor

        public Texture( ResourceManager parent, string name, ResourceHandle handle, string group )
            : this( parent, name, handle, group, false, null )
        {
        }

        public Texture( ResourceManager parent, string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader )
            : base( parent, name, handle, group, isManual, loader )
        {
            // init defaults; can be overridden before load()
            Height = 512;
            Width = 512;
            Depth = 1;
            RequestedMipMaps = 0;
            MipmapCount = 0;
            MipmapsHardwareGenerated = false;
            Gamma = 1.0f;
            TextureType = TextureType.TwoD;
            Format = PixelFormat.A8R8G8B8;
            Usage = TextureUsage.Default;
            // SrcBpp inited later on

            SrcWidth = 0;
            SrcHeight = 0;
            SrcDepth = 0;

            // FinalBpp inited later on by enable32bit
            // HasAlpha inited later on            

            Is32Bit = false;

            if ( createParamDictionary( "Texture" ) )
            {
                // Define the parameters that have to be present to load
                // from a generic source; actually there are none, since when
                // predeclaring, you use a texture file which includes all the
                // information required.
            }
        }

        #endregion

        #region Methods

        /// <summary>
        ///    Loads data from an Image directly into this texture.
        /// </summary>
        /// <param name="image"></param>
        /// <ogre name="loadImage" />
        public abstract void LoadImage( Image image );

        /// <summary>
        ///    Loads raw image data from the stream into this texture.
        /// </summary>
        /// <param name="data">The raw, decoded image data.</param>
        /// <param name="width">Width of the texture data.</param>
        /// <param name="height">Height of the texture data.</param>
        /// <param name="format">Format of the supplied image data.</param>
        /// <ogre name="loadRawData" />
        public void LoadRawData( Stream data, int width, int height, PixelFormat format )
        {
            // load the raw data
            Image image = Image.FromRawStream( data, width, height, format );

            // call the polymorphic LoadImage implementation
            LoadImage( image );
        }

        /// <summary>
        /// Generic method to load the texture from a set of images. This can be
        /// used by the specific implementation for convience. Implementations
        /// might decide not to use this function if they can use their own image loading
        /// functions.
        /// </summary>
        ///<param name="images">
        /// Vector of pointers to Images. If there is only one image
        /// in this vector, the faces of that image will be used. If there are multiple
        /// images in the vector each image will be loaded as a face.
        /// </param>
        protected void loadImages( List<Image> images )
        {
            if ( images.size() < 1 )
                throw new AxiomException( "Cannot load empty vector of images" );

            if ( IsLoaded )
            {
                LogManager.Instance.Write( "Texture: {0}: Unloading Image", Name );
                Unload();
            }

            // Set desired texture size and properties from images[0]
            SrcWidth = Width = images[ 0 ].getWidth();
            SrcHeight = Height = images[0]->getHeight();
            SrcDepth = mDepth = images[0]->getDepth();
            Format = images[0]->getFormat();
            SrcBpp = Image.GetNumElemBits( Format );
            HasAlpha = Image.FormatHasAlpha( Format );

            if ( Bpp == 16 )
            {
                // Drop down texture internal format
                switch ( Format )
                {
                    case PixelFormat.R8G8B8:
                    case PixelFormat.X8R8G8B8:
                        Format = PixelFormat.R5G6B5;
                        break;

                    case PixelFormat.B8G8R8:
                    case PixelFormat.X8B8G8R8:
                        Format = PixelFormat.B5G6R5;
                        break;

                    case PixelFormat.A8R8G8B8:
                    case PixelFormat.R8G8B8A8:
                    case PixelFormat.A8B8G8R8:
                    case PixelFormat.B8G8R8A8:
                        Format = PixelFormat.A4R4G4B4;
                        break;
                }
            }

            // The custom mipmaps in the image have priority over everything
            int imageMips = images[0].NumMipmaps;

            if ( imageMips > 0 )
            {
                MipmapCount = images[ 0 ].NumMipmaps;
                // Disable flag for auto mip generation
                Usage &= TextureUsage.AutoMipmap;
            }

            // Create the texture
            _createInternalResources();

            // Check if we're loading one image with multiple faces
            // or a vector of images representing the faces
            int faces;
            bool multiImage; // Load from multiple images?
            if ( images.size() > 1 )
            {
                faces = images.size();
                multiImage = true;
            }
            else
            {
                faces = images[0]->getNumFaces();
                multiImage = false;
            }

            // Check wether number of faces in images exceeds number of faces
            // in this texture. If so, clamp it.
            if ( faces > getNumFaces() )
                faces = getNumFaces();

            // Say what we're doing
            StringBuilder str;
            str.AppendFormat( "Texture: {0} : Loading {1} faces ( {2}, {3}x{4}x{5} ) with ", Name, faces, Image.getFormatName( images[0]->getFormat() ), images[0]->getWidth(), images[0]->getHeight(), images[0]->getDepth() );
            if ( !( mMipmapsHardwareGenerated && mNumMipmaps == 0 ) )
                str.Append( mNumMipmaps );
            if ( mUsage & TU_AUTOMIPMAP )
            {
                if ( mMipmapsHardwareGenerated )
                    str.Append( " hardware" );

                str.Append( " generated mipmaps" );
            }
            else
            {
                str.Append( " custom mipmaps" );
            }
            if ( multiImage )
                str.Append( " from multiple Images." );
            else
                str.Append( " from Image." );
            // Scoped
            {
                // Print data about first destination surface
                HardwarePixelBufferSharedPtr buf = getBuffer( 0, 0 );
                str.AppendFormat( " Internal format is {0},{1}x{2}x{3}.", Image.getFormatName( buf->getFormat() ), buf->getWidth(), buf->getHeight(), buf->getDepth() );
            }
            LogManager.Instance.Write( str );

            // Main loading loop
            // imageMips == 0 if the image has no custom mipmaps, otherwise contains the number of custom mips
            for ( int mip = 0; mip <= imageMips; ++mip )
            {
                for ( int i = 0; i < faces; ++i )
                {
                    PixelBox src;
                    if ( multiImage )
                    {
                        // Load from multiple images
                        src = images[i]->getPixelBox( 0, mip );
                    }
                    else
                    {
                        // Load from faces of images[0]
                        src = images[0]->getPixelBox( i, mip );
                    }

                    if ( mGamma != 1.0f )
                    {
                        // Apply gamma correction
                        // Do not overwrite original image but do gamma correction in temporary buffer
                        MemoryDataStreamPtr buf; // for scoped deletion of conversion buffer
                        buf.bind( new MemoryDataStream( Image.getMemorySize( src.getWidth(), src.getHeight(), src.getDepth(), src.format ) ) );

                        PixelBox corrected = PixelBox( src.getWidth(), src.getHeight(), src.getDepth(), src.format, buf->getPtr() );
                        Image.bulkPixelConversion( src, corrected );

                        Image.applyGamma( static_cast<uint8*>( corrected.data ), mGamma, corrected.getConsecutiveSize(), Image.getNumElemBits( src.format ) );

                        // Destination: entire texture. blitFromMemory does the scaling to
                        // a power of two for us when needed
                        getBuffer(i, mip)->blitFromMemory( corrected );
                    }
                    else
                    {
                        // Destination: entire texture. blitFromMemory does the scaling to
                        // a power of two for us when needed
                        getBuffer(i, mip)->blitFromMemory( src );
                    }

                }
            }
            // Update size (the final size, not including temp space)
            mSize = getNumFaces() * Image.getMemorySize( mWidth, mHeight, mDepth, mFormat );

            IsLoaded = true;
        }

        private void _createInternalResources()
        {
            if ( !_internalResourcesCreated )
            {
                createInternalResources();
                _internalResourcesCreated = true;
            }
        }
        protected abstract void createInternalResources();

        private void _freeInternalResources()
        {
            if ( _internalResourcesCreated )
            {
                freeInternalResources();
                _internalResourcesCreated = false;
            }
        }
        protected abstract void freeInternalResources();

        #endregion

        #region Implementation of Resource

        protected override void unload()
        {
            _freeInternalResources();
        }

        protected override int calculateSize()
        {
            return faceCount * Image.MemorySize( Width, Height, Depth, Format );
        }

        #endregion

    }
}
