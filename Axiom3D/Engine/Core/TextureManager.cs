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

#endregion Namespace Declarations
			
namespace Axiom
{
    /// <summary>
    ///    Class for loading & managing textures.
    /// </summary>
    /// <remarks>
    ///     Texture manager serves as an abstract singleton for all API specific texture managers.
    ///		When a class inherits from this and is created, a instance of that class (i.e. GLTextureManager)
    ///		is stored in the global singleton instance of the TextureManager.  
    ///		Note: This will not take place until the RenderSystem is initialized and at least one RenderWindow
    ///		has been created.
    /// </remarks>
    /// 
    /// <ogre name="TextureManager">
    ///     <file name="OgreTextureManager.h"   revision="1.27.2.2" lastUpdated="6/20/2006" lastUpdatedBy="Borrillis" />
    ///     <file name="OgreTextureManager.cpp" revision="1.22.2.5" lastUpdated="6/20/2006" lastUpdatedBy="Borrillis" />
    /// </ogre> 
    /// 
    public abstract class TextureManager : ResourceManager
    {
        #region Singleton implementation

        /// <summary>
        ///     Singleton instance of this class.
        /// </summary>
        private static TextureManager _instance;

        /// <summary>
        ///     Gets the singleton instance of this class.
        /// </summary>
        public static TextureManager Instance
        {
            get
            {
                return _instance;
            }
        }

        #endregion Singleton implementation

        #region Fields and Properties

        #region Is32Bit Property

        private bool _is32Bit;
        /// <summary>
        ///    Flag that indicates whether 32-bit texture are being used.
        /// </summary>
        /// <ogre name="enable32BitTextures" />
        /// <ogre name="isEnable32BitTextures" />
        protected bool Is32Bit
        {
            get
            {
                return _is32Bit;
            }
            set
            {
                _is32Bit = value;
                // Iterate throught all textures
                foreach( NameValueKeyPair nvkp in Resources )
                {
                    Texture texture = (Texture)( nvkp.Value);
                    // Reload loaded and reloadable texture only
                    if (texture.IsLoaded && texture.IsReloadable )
                    {
                        texture.Unload();
                        texture.Is32Bit = _is32Bit;
                        texture.Load();
                    }
                    else
                    {
                        texture.Is32Bit = _is32Bit;
                    }
                }
            }
        }

        #endregion Is32Bit Property

        #region DefaultMipmapCount Property

        private int _defaultMipmapCount = 5;
        /// <summary>
        ///    Default number of mipmaps to be used for loaded textures.
        /// </summary>
        /// <ogre name="setDefaultNumMipmaps" />
        /// <ogre name="getDefaultNumMipmaps" />
        public int DefaultMipmapCount
        {
            get
            {
                return _defaultMipmapCount;
            }
            set
            {
                _defaultMipmapCount = value;
            }
        }

        #endregion DefaultMipmapCount Property
			    
        #endregion Fields and Properties

        #region Constructors and Destructor

        /// <summary>
        ///     Internal constructor.  This class cannot be instantiated externally.
        /// </summary>
        /// <remarks>
        ///     Protected internal because this singleton will actually hold the instance of a subclass
        ///     created by a render system plugin.
        /// </remarks>
        protected TextureManager( bool enable32Bit )
        {

            if ( instance == null )
            {
                instance = this;
            }

            this.ResourceType = "Texture";
            this.LoadingOrder = new Real( 75 );

            // Subclasses should register (when this is fully constructed)
        }

        ~TextureManager()
        {
            // subclasses should unregister with resource group manager

        }

        #endregion Constructors and Destructor

        #region Methods

        #region LoadRawData Method 

        /// <overloads>
        /// <summary>
        /// Loads a texture from a raw data stream.
        /// </summary>
        /// <remarks>The texture will create as manual texture without loader.</remarks>
        /// <param name="name">The name to give the resulting texture</param>
        /// <param name="group">The name of the resource group to assign the texture to</param>
        /// <returns></returns>
        /// <ogre name="loadRawData" />
        /// </overloads>
        public new Texture LoadRawData( string name, string group )
        {
            return LoadRawData( name, group, TextureType.TwoD );
        }

        /// <param name="texType">The type of texture to load/create, defaults to normal 2D textures</param>
        public Texture LoadRawData( string name, string group, TextureType type )
        {
            // load the texture by default with -1 mipmaps (uses default), gamma of 1
            return LoadRawData( name, group, type, -1, 1.0f );
        }

        /// <param name="stream">Incoming data stream</param>
        /// <param name="width">The dimensions of the texture</param>
        /// <param name="height">The dimensions of the texture</param>
        /// <param name="format">
        /// The format of the data being passed in; the manager reserves
        /// the right to create a different format for the texture if the 
        /// original format is not available in this context.
        /// </param>
        /// <param name="texType">The type of texture to load/create, defaults to normal 2D textures</param>
        /// <param name="mipmapCount">
        /// The number of pre-filtered mipmaps to generate. If left to default (-1) then
        /// the TextureManager's default number of mipmaps will be used (see setDefaultNumMipmaps())
        /// If set to MIP_UNLIMITED mipmaps will be generated until the lowest possible
        /// level, 1x1x1.
        /// </param>
        /// <param name="gamma">The gamma adjustment factor to apply to this texture (brightening/darkening)</param>
        /// <returns></returns>
        public virtual Texture LoadRawData( string name, string group, Stream stream, int width, int height, PixelFormat format, TextureType texType, int mipmapCount, Real gamma )
        {
            Texture texture = (Texture)Create( name, group, true, null, null );

            texture.TextureType = type;
            texture.MipmapCount = ( mipmapCount == -1 ) ? DefaultMipmapCount : mipmapCount;
            texture.Gamma = gamma;
            texture.Is32Bit = Is32Bit;
            texture.LoadRawData( stream, width, height, format );

            return texture;
        }

        #endregion LoadRawData Method
			
        #region CreateManual Method

        /// <overloads>
        /// <summary>
        ///    Method for creating a new blank texture.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="mipmapCount"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        /// <ogre name="createManual" />
        /// </overloads>
        public Texture CreateManual( string name, string group, TextureType type, int width, int height, int mipmapCount, PixelFormat format )
        {
            return CreateManual( name, group, type, width, height, 1, mipmapCount, format, TextureUsage.Default, null );
        }

        /// <param name="depth"></param>
        public Texture CreateManual( string name, string group, TextureType type, int width, int height, int depth, int mipmapCount, PixelFormat format )
        {
            return CreateManual( name, group, type, width, height, depth, mipmapCount, format, TextureUsage.Default, null );
        }

        /// <param name="depth"></param>
        /// <param name="format"></param>
        /// <param name="usage"></param>
        public Texture CreateManual( string name, string group, TextureType type, int width, int height, int depth, int mipmapCount, PixelFormat format, TextureUsage usage )
        {
            return CreateManual( name, group, type, width, height, depth, mipmapCount, format, usage, null );
        }

        /// <param name="depth"></param>
        /// <param name="format"></param>
        /// <param name="usage"></param>
        /// <param name="loader"></param>
        public virtual Texture CreateManual( string name, string group, TextureType type, int width, int height, int depth, int mipmapCount, PixelFormat format, TextureUsage usage, IManualResourceLoader loader )
        {
            Texture texture = (Texture)Create( name, group, true, loader, null );

            texture.TextureType = type;
            texture.Width = width;
            texture.Height = height;
            texture.Depth = depth;
            texture.MipmapCount = ( mipmapCount == -1 ) ? DefaultMipmapCount : mipmapCount;
            texture.Gamma = gamma;
            texture.Format = format;
            texture.Usage = usage;
            texture.Is32Bit = Is32Bit;
            texture.CreateInternalResources();

            return texture;
        }

        #endregion CreateManual Method
    			
        #region Load Method

        /// <overloads>
        /// <summary>
        ///    Loads a texture with the specified name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <ogre name="load" />
        /// </overloads>
        public new Texture Load( string name, string group )
        {
            return Load( name, group, TextureType.TwoD );
        }

        /// <param name="type" />
        public Texture Load( string name, string group, TextureType type )
        {
            // load the texture by default with -1 mipmaps (uses default), gamma of 1
            return Load( name, group, type, -1, 1.0f );
        }

        /// <param name="mipmapCount"></param>
        public Texture Load( string name, string group, TextureType type, int mipmapCount )
        {
            Load( name, group, type, mipmapCount, 1.0f );
        }

        /// <param name="mipmapCount"></param>
        /// <param name="gamma"></param>
        public Texture Load( string name, string group, TextureType type, int mipmapCount, Real gamma )
        {
            // does this texture exist already?
            Texture texture = (Texture)this[ name ];

            if ( texture == null )
            {
                // create a new texture
                texture = Create( name, group );

                texture.TextureType = type;
                texture.MipmapCount = ( mipmapCount == -1 ) ? DefaultMipmapCount : mipmapCount;
                texture.Gamma = gamma;
                texture.Is32Bit = Is32Bit;

            }
            // call the base class load method
            texture.Load();

            return texture;
        }

        #endregion Load Method
	
        #region LoadImage Method

        /// <overloads>
        /// <summary>
        ///		Loads a pre-existing image into the texture.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="group"></param>
        /// <param name="image"></param>
        /// <returns></returns>
        /// <ogre name="loadImage" />
        /// </overloads>
        public Texture LoadImage( string name, string group, Image image )
        {
            return LoadImage( name, group, image, TextureType.TwoD, -1, 1.0f, 1 );
        }

        /// <param name="texType"></param>
        public Texture LoadImage( string name, string group, Image image, TextureType texType )
        {
            return LoadImage( name, group, image, texType, -1, 1.0f );
        }

        /// <param name="mipmapCount"></param>
        /// <param name="gamma"></param>
        public Texture LoadImage( string name, string group, Image image, TextureType texType, int mipmapCount, float gamma )
        {
            // create a new texture
            Texture texture = (Texture)Create( name, group, true, null, null);

            // set the number of mipmaps to use for this texture
            texture.TextureType = type;
            texture.MipmapCount = ( mipmapCount == -1 ) ? DefaultMipmapCount : mipmapCount;

            // set bit depth and gamma
            texture.Gamma = gamma;
            texture.Is32Bit = Is32Bit;

            // load image data
            texture.LoadImage( image );

            // add the texture to the resource list
            resources[texture.Name] = texture;

            return texture;
        }

        #endregion LoadImage Method

        ///// <summary>
        /////    Returns an instance of Texture that has the supplied name.
        ///// </summary>
        ///// <param name="name"></param>
        ///// <returns></returns>
        //public Texture GetByName( string name )
        //{
        //    return (Texture)this[ name ];
        //}


        #endregion Methods

        #region IDisposable Implementation

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

        #endregion IDisposable Implementation
    }
}
