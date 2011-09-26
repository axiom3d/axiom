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
#endregion LGPL License

#region SVN Version Information
// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using Axiom.CrossPlatform;
using Marshal = System.Runtime.InteropServices.Marshal;

using Axiom.Graphics;
using Axiom.Media;

using ResourceHandle = System.UInt64;
using System.Text;

#endregion Namespace Declarations

namespace Axiom.Core
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
		#region Fields and Properties

		#region internalResourcesCreated Property

		private bool _internalResourcesCreated = false;
		/// <summary>
		///
		/// </summary>
		protected bool internalResourcesCreated
		{
			get
			{
				return _internalResourcesCreated;
			}
			set
			{
				_internalResourcesCreated = value;
			}
		}

		#endregion internalResourcesCreated Property

		#region UseCount Property

		public int UseCount
		{
			get;
			set;
		}

		#endregion UseCount Property

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
				return _width;
			}
			set
			{
				_width = _srcWidth = value;
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
				_height = _srcHeight = value;
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
				_depth = _srcDepth = value;
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

		#region TreatLuminanceAsAlpha Property

		private bool _treatLuminanceAsAlpha = false;
		/// <summary>
		/// Gets or sets a value indicating whether to treat luminence as aplha.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if treat luminence as aplha; otherwise, <c>false</c>.
		/// </value>
		public bool TreatLuminanceAsAlpha
		{
			get
			{
				return _treatLuminanceAsAlpha;
			}
			set
			{
				_treatLuminanceAsAlpha = value;
			}
		}

		#endregion TreatLuminanceAsAlpha Property

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
				return _gamma;
			}
			set
			{
				_gamma = value;
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
			set
			{
				_format = value;

				srcBpp = PixelUtil.GetNumElemBytes( _format );
				HasAlpha = PixelUtil.HasAlpha( _format );

			}
		}

		#endregion Format Property

		#region MipmapCount Property

		/// <summary>Number of mipmaps present in this texture.</summary>
		protected int _mipmapCount;
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
                _requestedMipmapCount = _mipmapCount = value;
            }
		}

		#endregion MipmapCount Property

		#region RequestedMipMapCount Property

		/// <summary>Number of mipmaps requested for this texture.</summary>
		private int _requestedMipmapCount;
		/// <summary>
		/// Gets or sets the requested mipmap count.
		/// </summary>
		/// <value>The requested mipmap count.</value>
		protected int RequestedMipmapCount
		{
			get
			{
				return _requestedMipmapCount;
			}
			set
			{
				_requestedMipmapCount = value;
			}
		}

		#endregion RequestedMipMapCount Property

		#region MipmapsHardwareGenerated Property

		/// <summary>Are the mipmaps generated in hardware?</summary>
		private bool _mipmapsHardwareGenerated = false;
		/// <summary>
		/// Gets or sets a value indicating whether mipmaps are hardware generated.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if mipmaps are hardware generated; otherwise, <c>false</c>.
		/// </value>
		protected bool MipmapsHardwareGenerated
		{
			get
			{
				return _mipmapsHardwareGenerated;
			}
			set
			{
				_mipmapsHardwareGenerated = value;
			}
		}

		#endregion MipmapsHardwareGenerated Property

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

		#region SrcFormat Property

		/// <summary>Original format of the input texture (only applicable for 3D textures).</summary>
		private PixelFormat _srcFormat;
		/// <summary>Original format of the input texture (only applicable for 3D textures).</summary>
		/// <ogre name="getSrcDepth" />
		public PixelFormat SrcFormat
		{
			get
			{
				return _srcFormat;
			}
			protected set
			{
				_srcFormat = value;
			}
		}

		#endregion SrcFormat Property

		#region DesiredFormat Property

		/// <summary>Desired format of the input texture (only applicable for 3D textures).</summary>
		private PixelFormat _desiredFormat = PixelFormat.Unknown;
		/// <summary>Desired format of the input texture (only applicable for 3D textures).</summary>
		/// <ogre name="getSrcDepth" />
		public PixelFormat DesiredFormat
		{
			get
			{
				return _desiredFormat;
			}
			protected set
			{
				_desiredFormat = value;
			}
		}

		#endregion DesiredFormat Property

		#region DesiredBitDepth

		private ushort _desiredFloatBitDepth = 0;
		private ushort _desiredIntegerBitDepth = 0;

		/// <summary>
		/// Desired bit depth for integer pixel format textures.
		/// </summary>
		/// <remarks>
		/// Available values: 0, 16 and 32, where 0 (the default) means keep original format
		/// as it is. This value is number of bits for a channel of the pixel.
		/// </remarks>
		public virtual ushort DesiredIntegerBitDepth
		{
			get
			{
				return _desiredIntegerBitDepth;
			}
			set
			{
				_desiredIntegerBitDepth = value;
			}
		}

		/// <summary>
		/// Desired bit depth for float pixel format textures.
		/// </summary>
		/// <remarks>
		/// Available values: 0, 16 and 32, where 0 (the default) means keep original format
		/// as it is. This value is number of bits for a channel of the pixel.
		/// </remarks>
		public virtual ushort DesiredFloatBitDepth
		{
			get
			{
				return _desiredFloatBitDepth;
			}
			set
			{
				_desiredFloatBitDepth = value;
			}
		}

		/// <summary>
		/// Sets desired bit depth for integer and float pixel format.
		/// </summary>
		/// <param name="integerBitDepth"></param>
		/// <param name="floatBitDepth"></param>
		public virtual void SetDesiredBitDepths( ushort integerBitDepth, ushort floatBitDepth )
		{
			_desiredFloatBitDepth = floatBitDepth;
			_desiredIntegerBitDepth = integerBitDepth;
		}

		#endregion DesiredBitDepth

		#region FSAA Properties

		/// <summary></summary>
		private int _fsaa = 0;
		/// <summary></summary>
		/// <ogre name="getFSAA" />
		public int FSAA
		{
			get
			{
				return _fsaa;
			}
			protected set
			{
				_fsaa = value;
			}
		}

		public string FSAAHint
		{
			get;
			protected set;
		}

		public void SetFSAA( int fsaa, string fsaaHint )
		{
			_fsaa = fsaa;
			FSAAHint = fsaaHint;
		}

		#endregion FSAA Properties

		#region HardwareGammaEnabled Property

		private bool _hwGamma;

		/// <summary>
		/// Gets/Sets whether this texture will be set up so that on sampling it, hardware gamma correction is applied.
		/// </summary>
		/// <remarks>
		/// 24-bit textures are often saved in gamma color space; this preserves
		/// precision in the 'darks'. However, if you're performing blending on
		/// the sampled colors, you really want to be doing it in linear space.
		/// One way is to apply a gamma correction value on loading <see cref="Gamma" />,
		/// but this means you lose precision in those dark colors. An alternative
		/// is to get the hardware to do the gamma correction when reading the
		/// texture and converting it to a floating point value for the rest of
		/// the pipeline. This option allows you to do that; it's only supported
		/// in relatively recent hardware (others will ignore it) but can improve
		/// the quality of color reproduction.
		/// Note:
		/// Must be called before any 'load' method since it may affect the
		/// construction of the underlying hardware resources.
		/// Also note this only useful on textures using 8-bit color channels.
		/// </remarks>
		public bool HardwareGammaEnabled
		{
			get
			{
				return _hwGamma;
			}
			set
			{
				_hwGamma = value;
			}
		}

		#endregion HardwareGammaEnabled Property

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

		#region Construction and Destruction

		/// <summary>
		/// Initializes a new instance of the <see cref="Texture"/> class.
		/// </summary>
		/// <param name="parent">The parent.</param>
		/// <param name="name">The name.</param>
		/// <param name="handle">The handle.</param>
		/// <param name="group">The group.</param>
		public Texture( ResourceManager parent, string name, ResourceHandle handle, string group )
			: this( parent, name, handle, group, false, null )
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Texture"/> class.
		/// </summary>
		/// <param name="parent">The parent.</param>
		/// <param name="name">The name.</param>
		/// <param name="handle">The handle.</param>
		/// <param name="group">The group.</param>
		/// <param name="isManual">if set to <c>true</c> [is manual].</param>
		/// <param name="loader">The loader.</param>
		public Texture( ResourceManager parent, string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader )
			: base( parent, name, handle, group, isManual, loader )
		{
			// init defaults; can be overridden before load()
			Height = 512;
			Width = 512;
			Depth = 1;
			RequestedMipmapCount = 0;
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

			//if ( createParamDictionary( "Texture" ) )
			//{
			//    // Define the parameters that have to be present to load
			//    // from a generic source; actually there are none, since when
			//    // predeclaring, you use a texture file which includes all the
			//    // information required.
			//}

			if ( TextureManager.Instance != null )
			{
				var mgr = TextureManager.Instance;
				MipmapCount = mgr.DefaultMipmapCount;
				SetDesiredBitDepths( mgr.PreferredIntegerBitDepth, mgr.PreferredFloatBitDepth );
			}
		}

		#endregion Construction and Destruction

		#region Methods

		/// <summary>
		///    Loads data from an Image directly into this texture.
		/// </summary>
		/// <param name="image"></param>
		/// <ogre name="loadImage" />
		public virtual void LoadImage( Image image )
		{
			lock ( _loadingStatusMutex )
			{
				if ( LoadingState != LoadingState.Unloaded )
				{
					return; // no loading to be done.
				}
				LoadingState = LoadingState.Loading;
			}

			try
			{
				// create a list with one texture to pass it in to the common loading method
				LoadImages( new Image[] { image } );

			}
			catch ( Exception ex )
			{
				lock ( _loadingStatusMutex )
				{
					LoadingState = LoadingState.Unloaded;
				}
				throw ex;
			}

			lock ( _loadingStatusMutex )
			{
				LoadingState = LoadingState.Loaded;
			}
		}

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
			var image = Image.FromRawStream( data, width, height, format );

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
		protected internal void LoadImages( Image[] images )
		{
			int faces;

			Debug.Assert( images.Length >= 1 );
			if ( IsLoaded )
			{
				LogManager.Instance.Write( "Unloading image: {0}", _name );
				Unload();
			}

			// Set desired texture size and properties from images[0]
			_srcWidth = _width = images[ 0 ].Width;
			_srcHeight = _height = images[ 0 ].Height;
			_srcDepth = _depth = images[ 0 ].Depth;

			// Get source image format and adjust if required
			_srcFormat = images[ 0 ].Format;
			if ( _treatLuminanceAsAlpha && _srcFormat == PixelFormat.L8 )
			{
				_srcFormat = PixelFormat.A8;
			}

			if ( _desiredFormat != PixelFormat.Unknown )
			{
				// If have desired format, use it
				_format = _desiredFormat;
			}
			else
			{
				// Get the format according with desired bit depth
				_format = PixelUtil.GetFormatForBitDepths( _srcFormat, _desiredIntegerBitDepth, _desiredFloatBitDepth );
			}

			// The custom mipmaps in the image have priority over everything
			var imageMips = images[ 0 ].NumMipMaps;
			if ( imageMips > 0 )
			{
                MipmapCount = imageMips;
				// Disable flag for auto mip generation
				_usage &= ~TextureUsage.AutoMipMap;
			}

			// Create the texture
			CreateInternalResources();

			// Check if we're loading one image with multiple faces
			// or a vector of images representing the faces
			bool multiImage; // Load from multiple images?
			if ( images.Length > 1 )
			{
				faces = images.Length;
				multiImage = true;
			}
			else
			{
				faces = images[ 0 ].NumFaces;
				multiImage = false;
			}

			// Check wether number of faces in images exceeds number of faces
			// in this texture. If so, clamp it.
			if ( faces > this.faceCount )
				faces = this.faceCount;

			// Say what we're doing
			{ // Scoped
				var msg = new StringBuilder();
				msg.AppendFormat( "Texture: {0}: Loading {1} faces( {2}, {3}x{4}x{5} ) with",
										_name, faces, PixelUtil.GetFormatName( images[ 0 ].Format ),
										images[ 0 ].Width, images[ 0 ].Height, images[ 0 ].Depth );
				if ( !( _mipmapsHardwareGenerated && _mipmapCount == 0 ) )
					msg.AppendFormat( " {0}", _mipmapCount );

				if ( ( _usage & TextureUsage.AutoMipMap ) == TextureUsage.AutoMipMap )
					msg.AppendFormat( "{0} generated mipmaps", _mipmapsHardwareGenerated ? " hardware" : "" );
				else
					msg.Append( " custom mipmaps" );

				msg.AppendFormat( " from {0}.\n\t", multiImage ? "multiple Images" : "an Image" );

				// Print data about first destination surface
				var buf = GetBuffer( 0, 0 );
				msg.AppendFormat( " Internal format is {0} , {1}x{2}x{3}.", PixelUtil.GetFormatName( buf.Format ), buf.Width, buf.Height, buf.Depth );

				LogManager.Instance.Write( msg.ToString() );
			}

			// Main loading loop
			// imageMips == 0 if the image has no custom mipmaps, otherwise contains the number of custom mips
            for (var mip = 0; mip <= imageMips; ++mip)
			{
				for ( var i = 0; i < faces; ++i )
				{
					PixelBox src;
					if ( multiImage )
					{
						// Load from multiple images
						src = images[ i ].GetPixelBox( 0, mip );
					}
					else
					{
						// Load from faces of images[0]
						src = images[ 0 ].GetPixelBox( i, mip );

						if ( _hasAlpha && src.Format == PixelFormat.L8 )
							src.Format = PixelFormat.A8;
					}

                    if (_gamma != 1.0f)
                    {
                        // Apply gamma correction
                        // Do not overwrite original image but do gamma correction in temporary buffer
                        var bufSize = PixelUtil.GetMemorySize( src.Width, src.Height, src.Depth, src.Format );
                        var buff = new byte[bufSize];
                        var buffer = BufferBase.Wrap( buff );
#if !AXIOM_SAFE_ONLY
                        unsafe
#endif
                        {

                            try
                            {
                                var corrected = new PixelBox( src.Width, src.Height, src.Depth, src.Format, buffer );
                                PixelConverter.BulkPixelConversion( src, corrected );

                                Image.ApplyGamma( corrected.Data, _gamma, corrected.ConsecutiveSize,
                                                  PixelUtil.GetNumElemBits( src.Format ) );

                                // Destination: entire texture. BlitFromMemory does
                                // the scaling to a power of two for us when needed
                                GetBuffer( i, mip ).BlitFromMemory( corrected );
                            }
                            finally
                            {
                                //Marshal.FreeHGlobal( buffer );
                            }
                        }
                    }
                    else
                    {
                        // Destination: entire texture. BlitFromMemory does
                        // the scaling to a power of two for us when needed
                        GetBuffer(i, mip).BlitFromMemory(src);
                    }
				}
			}
			// Update size (the final size, not including temp space)
			Size = faces * PixelUtil.GetMemorySize( _width, _height, _depth, _format );

		}

		/// <summary>
		///    Return hardware pixel buffer for a surface. This buffer can then
		///    be used to copy data from and to a particular level of the texture.
		/// </summary>
		/// <param name="face">
		///    Face number, in case of a cubemap texture. Must be 0
		///    for other types of textures.
		///    For cubemaps, this is one of
		///    +X (0), -X (1), +Y (2), -Y (3), +Z (4), -Z (5)
		/// </param>
		/// <param name="mipmap">
		///    Mipmap level. This goes from 0 for the first, largest
		///    mipmap level to getNumMipmaps()-1 for the smallest.
		/// </param>
		/// <remarks>
		///    The buffer is invalidated when the resource is unloaded or destroyed.
		///    Do not use it after the lifetime of the containing texture.
		/// </remarks>
		/// <returns>A shared pointer to a hardware pixel buffer</returns>
		public abstract HardwarePixelBuffer GetBuffer( int face, int mipmap );

		public HardwarePixelBuffer GetBuffer( int face )
		{
			return GetBuffer( face, 0 );
		}
		public HardwarePixelBuffer GetBuffer()
		{
			return GetBuffer( 0, 0 );
		}

		public void CreateInternalResources()
		{
			if ( !_internalResourcesCreated )
			{
				createInternalResources();
				_internalResourcesCreated = true;
			}
		}
		protected abstract void createInternalResources();

		public void FreeInternalResources()
		{
			if ( _internalResourcesCreated )
			{
				freeInternalResources();
				_internalResourcesCreated = false;
			}
		}
		protected abstract void freeInternalResources();

		#endregion Methods

		#region Implementation of Resource

		protected override void unload()
		{
			FreeInternalResources();
		}

		protected override int calculateSize()
		{
			return faceCount * PixelUtil.GetMemorySize( Width, Height, Depth, Format );
		}


		#endregion Implementation of Resource

	}
}