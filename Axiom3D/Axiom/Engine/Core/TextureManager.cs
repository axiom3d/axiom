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

namespace Axiom
{
    /// <summary>
    ///    Class for loading & managing textures.
    /// </summary>
    /// <remarks>
    ///    Texture manager serves as an abstract singleton for all API specific texture managers.
    ///		When a class inherits from this and is created, a instance of that class (i.e. GLTextureManager)
    ///		is stored in the global singleton instance of the TextureManager.  
    ///		Note: This will not take place until the RenderSystem is initialized and at least one RenderWindow
    ///		has been created.
    /// </remarks>
    public abstract class TextureManager : ResourceManager
    {
        #region Singleton implementation

        /// <summary>
        ///     Singleton instance of this class.
        /// </summary>
        private static TextureManager instance;

        /// <summary>
        ///     Internal constructor.  This class cannot be instantiated externally.
        /// </summary>
        /// <remarks>
        ///     Protected internal because this singleton will actually hold the instance of a subclass
        ///     created by a render system plugin.
        /// </remarks>
        protected internal TextureManager()
        {
            if ( instance == null )
            {
                instance = this;
            }
        }

        /// <summary>
        ///     Gets the singleton instance of this class.
        /// </summary>
        public static TextureManager Instance
        {
            get
            {
                return instance;
            }
        }

        #endregion Singleton implementation

        #region Fields

        /// <summary>
        ///    Flag that indicates whether 32-bit texture are being used.
        /// </summary>
        protected bool is32Bit;

        /// <summary>
        ///    Default number of mipmaps to be used for loaded textures.
        /// </summary>
        protected int defaultNumMipMaps = 5;

        #endregion Fields

        #region Properties

        /// <summary>
        ///    Gets/Sets the default number of mipmaps to be used for loaded textures.
        /// </summary>
        public int DefaultNumMipMaps
        {
            get
            {
                return defaultNumMipMaps;
            }
            set
            {
                defaultNumMipMaps = value;
            }
        }

        #endregion Properties

        #region Methods

        /// <summary>
        ///     Creates a new texture.
        /// </summary>
        /// <param name="name">Name of the texture to create, which is the filename.</param>
        /// <returns>A newly created texture object, API dependent.</returns>
        public override Resource Create( string name )
        {
            return Create( name, TextureType.TwoD );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public abstract Texture Create( string name, TextureType type );

        /// <summary>
        ///    Method for creating a new blank texture.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="numMipMaps"></param>
        /// <param name="format"></param>
        /// <param name="usage"></param>
        /// <returns></returns>
        public abstract Texture CreateManual( string name, TextureType type, int width, int height, int numMipMaps, PixelFormat format, TextureUsage usage );

        /// <summary>
        ///    Loads a texture with the specified name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Texture Load( string name )
        {
            return Load( name, TextureType.TwoD );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Texture Load( string name, TextureType type )
        {
            // load the texture by default with -1 mipmaps (uses default), gamma of 1, priority of 1
            return Load( name, type, -1, 1.0f, 1 );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="numMipMaps"></param>
        /// <param name="gamma"></param>
        /// <param name="priority"></param>
        /// <returns></returns>
        public Texture Load( string name, TextureType type, int numMipMaps, float gamma, int priority )
        {
            // does this texture exist already?
            Texture texture = GetByName( name );

            if ( texture == null )
            {
                // create a new texture
                texture = (Texture)Create( name, type );

                if ( numMipMaps == -1 )
                {
                    texture.NumMipMaps = defaultNumMipMaps;
                }
                else
                {
                    texture.NumMipMaps = numMipMaps;
                }

                // set bit depth and gamma
                texture.Gamma = gamma;
                texture.Enable32Bit( is32Bit );

                // call the base class load method
                base.Load( texture, priority );
            }

            return texture;
        }

        /// <summary>
        ///		Overloaded method.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="image"></param>
        /// <returns></returns>
        public Texture LoadImage( string name, Image image )
        {
            return LoadImage( name, image, TextureType.TwoD, -1, 1.0f, 1 );
        }

        /// <summary>
        ///		Overloaded method.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="image"></param>
        /// <param name="texType"></param>
        /// <returns></returns>
        public Texture LoadImage( string name, Image image, TextureType texType )
        {
            return LoadImage( name, image, texType, -1, 1.0f, 1 );
        }

        /// <summary>
        ///		Loads a pre-existing image into the texture.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="image"></param>
        /// <param name="numMipMaps"></param>
        /// <param name="gamma"></param>
        /// <param name="priority"></param>
        /// <returns></returns>
        public Texture LoadImage( string name, Image image, TextureType texType, int numMipMaps, float gamma, int priority )
        {
            // create a new texture
            Texture texture = (Texture)Create( name, texType );

            // set the number of mipmaps to use for this texture
            if ( numMipMaps == -1 )
            {
                texture.NumMipMaps = defaultNumMipMaps;
            }
            else
            {
                texture.NumMipMaps = numMipMaps;
            }

            // set bit depth and gamma
            texture.Gamma = gamma;
            texture.Enable32Bit( is32Bit );

            // load image data
            texture.LoadImage( image );

            // add the texture to the resource list
            resourceList[texture.Name] = texture;

            return texture;
        }

        /// <summary>
        ///    Returns an instance of Texture that has the supplied name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public new Texture GetByName( string name )
        {
            return (Texture)base.GetByName( name );
        }

        /// <summary>
        ///     Called when the engine is shutting down.    
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();

            if ( this == instance )
            {
                instance = null;
            }
        }

        #endregion Methods
    }
}
