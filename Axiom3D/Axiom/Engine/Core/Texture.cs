#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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

using System;
using System.IO;

namespace Axiom
{
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
        #region Member variables

        /// <summary>Width of this texture.</summary>
        protected int width;
        /// <summary>Height of this texture.</summary>
        protected int height;
        /// <summary>Depth of this texture.</summary>
        protected int depth;
        /// <summary>Bits per pixel in this texture.</summary>
        protected int finalBpp;
        /// <summary>Original source width if this texture had been modified.</summary>
        protected int srcWidth;
        /// <summary>Original source height if this texture had been modified.</summary>
        protected int srcHeight;
        /// <summary>Original source bits per pixel if this texture had been modified.</summary>
        protected int srcBpp;
        /// <summary>Does this texture have an alpha component?</summary>
        protected bool hasAlpha;
        /// <summary>Pixel format of this texture.</summary>
        protected PixelFormat format;
        /// <summary>Specifies how this texture will be used.</summary>
        protected TextureUsage usage;
        /// <summary>Type of texture, i.e. 1D, 2D, Cube, Volume.</summary>
        protected TextureType textureType;
        /// <summary>Number of mipmaps present in this texture.</summary>
        protected int numMipMaps;
        /// <summary>Gamma setting for this texture.</summary>
        protected float gamma;

        #endregion

        #region Constructors

        #endregion

        #region Methods

        /// <summary>
        ///    Specifies whether this texture should use 32 bit color or not.
        /// </summary>
        /// <param name="enable">true if this should be treated as 32-bit, false if it should be 16-bit.</param>
        public void Enable32Bit( bool enable )
        {
            finalBpp = ( enable == true ) ? 32 : 16;
        }


        /// <summary>
        ///    Loads data from an Image directly into this texture.
        /// </summary>
        /// <param name="image"></param>
        public abstract void LoadImage( Image image );

        /// <summary>
        ///    Loads raw image data from the stream into this texture.
        /// </summary>
        /// <param name="data">The raw, decoded image data.</param>
        /// <param name="width">Width of the texture data.</param>
        /// <param name="height">Height of the texture data.</param>
        /// <param name="format">Format of the supplied image data.</param>
        public void LoadRawData( Stream data, int width, int height, PixelFormat format )
        {
            // load the raw data
            Image image = Image.FromRawStream( data, width, height, format );

            // call the polymorphic LoadImage implementation
            LoadImage( image );
        }

        #endregion

        #region Properties

        /// <summary>
        ///    Gets the width (in pixels) of this texture.
        /// </summary>
        public int Width
        {
            get
            {
                return width;
            }
        }

        /// <summary>
        ///    Gets the height (in pixels) of this texture.
        /// </summary>
        public int Height
        {
            get
            {
                return height;
            }
        }

        /// <summary>
        ///    Gets the depth of this texture (for volume textures).
        /// </summary>
        public int Depth
        {
            get
            {
                return depth;
            }
        }

        /// <summary>
        ///    Gets the bits per pixel found within this texture data.
        /// </summary>
        public int Bpp
        {
            get
            {
                return finalBpp;
            }
        }

        /// <summary>
        ///    Gets whether or not the PixelFormat of this texture contains an alpha component.
        /// </summary>
        public bool HasAlpha
        {
            get
            {
                return hasAlpha;
            }
        }

        /// <summary>
        ///    Gets/Sets the gamma adjustment factor for this texture.
        /// </summary>
        /// <remarks>
        ///    Must be called before any variation of Load.
        /// </remarks>
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

        /// <summary>
        ///    Gets the PixelFormat of this texture.
        /// </summary>
        public PixelFormat Format
        {
            get
            {
                return format;
            }
        }

        /// <summary>
        ///    Number of mipmaps present in this texture.
        /// </summary>
        public int NumMipMaps
        {
            get
            {
                return numMipMaps;
            }
            set
            {
                numMipMaps = value;
            }
        }

        /// <summary>
        ///    Type of texture, i.e. 2d, 3d, cubemap.
        /// </summary>
        public TextureType TextureType
        {
            get
            {
                return textureType;
            }
        }

        /// <summary>
        ///     Gets the intended usage of this texture, whether for standard usage
        ///     or as a render target.
        /// </summary>
        public TextureUsage Usage
        {
            get
            {
                return usage;
            }
        }

        #endregion Properties

        #region Implementation of Resource

        /// <summary>
        ///		Implementation of IDisposable to determine how resources are disposed of.
        /// </summary>
        public override void Dispose()
        {
            // call polymorphic Unload method
            Unload();
        }

        #endregion
    }
}
