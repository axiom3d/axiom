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

using System.IO;
using System.Text;

using Axiom.CrossPlatform;
using Axiom.Graphics;
using Axiom.Media;

using ResourceHandle = System.UInt64;

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
	public abstract class Texture : Resource, ICopyable<Texture>
	{
		#region Fields and Properties

		protected bool internalResourcesCreated;
		protected int requestedMipmapCount;

		#region UseCount Property

		public int UseCount { get; set; }

		#endregion UseCount Property

		#region Width Property

		/// <summary>Width of this texture.</summary>
		protected int width = 512;

		/// <summary>
		/// Gets the width (in pixels) of this texture.
		/// </summary>
		/// <ogre name="getWidth" />
		/// <ogre name="setWidth" />
		public int Width
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return width;
			}

			[OgreVersion( 1, 7, 2 )]
			set
			{
				width = srcWidth = value;
			}
		}

		#endregion Width Property

		#region Height Property

		/// <summary>Height of this texture.</summary>
		protected int height = 512;

		/// <summary>
		/// Gets the height (in pixels) of this texture.
		/// </summary>
		/// <ogre name="setHeight" />
		/// <ogre name="getHeight" />
		public int Height
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return height;
			}

			[OgreVersion( 1, 7, 2 )]
			set
			{
				height = srcHeight = value;
			}
		}

		#endregion Height Property

		#region Depth Property

		/// <summary>Depth of this texture.</summary>
		protected int depth = 1;

		/// <summary>
		/// Gets the depth of this texture (for volume textures).
		/// </summary>
		/// <ogre name="setDepth" />
		/// <ogre name="getDepth" />
		public int Depth
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return depth;
			}

			[OgreVersion( 1, 7, 2 )]
			set
			{
				depth = srcDepth = value;
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

		/// <summary>
		/// Gets whether or not the PixelFormat of this texture contains an alpha component.
		/// </summary>
		/// <ogre name="hasAlpha" />
		public virtual bool HasAlpha
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return PixelUtil.HasAlpha( format );
			}
		}

		#endregion HasAlpha Property

		#region TreatLuminanceAsAlpha Property

		protected bool treatLuminanceAsAlpha;

		/// <summary>
		/// Gets or sets a value indicating whether to treat luminence as aplha.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if treat luminence as aplha; otherwise, <c>false</c>.
		/// </value>
		public bool TreatLuminanceAsAlpha
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return treatLuminanceAsAlpha;
			}

			[OgreVersion( 1, 7, 2 )]
			set
			{
				treatLuminanceAsAlpha = value;
			}
		}

		#endregion TreatLuminanceAsAlpha Property

		#region Gamma Property

		/// <summary>Gamma setting for this texture.</summary>
		protected float gamma = 1.0f;

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
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return gamma;
			}

			[OgreVersion( 1, 7, 2 )]
			set
			{
				gamma = value;
			}
		}

		#endregion Gamma Property

		#region Format Property

		/// <summary>Pixel format of this texture.</summary>
		protected PixelFormat format = PixelFormat.Unknown;

		/// <summary>
		/// Gets the PixelFormat of this texture.
		/// </summary>
		/// <ogre name="getFormat" />
		public PixelFormat Format
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return format;
			}
		}

		#endregion Format Property

		#region MipmapCount Property

		/// <summary>Number of mipmaps present in this texture.</summary>
		protected int mipmapCount;

		/// <summary>
		/// Number of mipmaps present in this texture.
		/// </summary>
		/// <ogre name="setNumMipmaps" />
		/// <ogre name="getNumMipmaps" />
		public int MipmapCount
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return mipmapCount;
			}

			[OgreVersion( 1, 7, 2 )]
			set
			{
				requestedMipmapCount = mipmapCount = value;
			}
		}

		#endregion MipmapCount Property

		#region MipmapsHardwareGenerated Property

		/// <summary>Are the mipmaps generated in hardware?</summary>
		protected bool mipmapsHardwareGenerated;

		/// <summary>
		/// Gets or sets a value indicating whether mipmaps are hardware generated.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if mipmaps are hardware generated; otherwise, <c>false</c>.
		/// </value>
		public virtual bool MipmapsHardwareGenerated
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return mipmapsHardwareGenerated;
			}
		}

		#endregion MipmapsHardwareGenerated Property

		#region TextureType Property

		/// <summary>Type of texture, i.e. 1D, 2D, Cube, Volume.</summary>
		protected TextureType textureType = TextureType.TwoD;

		/// <summary>
		/// Type of texture, i.e. 2d, 3d, cubemap.
		/// </summary>
		/// <ogre name="setTextureType" />
		/// <ogre name="getTextureType" />
		public virtual TextureType TextureType
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return textureType;
			}

			[OgreVersion( 1, 7, 2 )]
			set
			{
				textureType = value;
			}
		}

		#endregion TextureType Property

		#region Usage Property

		/// <summary>Specifies how this texture will be used.</summary>
		protected TextureUsage usage = TextureUsage.Default;

		/// <summary>
		/// Gets the intended usage of this texture, whether for standard usage
		/// or as a render target.
		/// </summary>
		/// <ogre name="setUsage" />
		/// <ogre name="getUsage" />
		public virtual TextureUsage Usage
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return usage;
			}

			[OgreVersion( 1, 7, 2 )]
			set
			{
				usage = value;
			}
		}

		#endregion Usage Property

		#region SrcWidth Property

		/// <summary>Original source width if this texture had been modified.</summary>
		protected int srcWidth;

		/// <summary>Original source width if this texture had been modified.</summary>
		/// <ogre name="geteSrcWidth" />
		public virtual int SrcWidth
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return srcWidth;
			}
		}

		#endregion SrcWidth Property

		#region SrcHeight Property

		/// <summary>Original source height if this texture had been modified.</summary>
		protected int srcHeight;

		/// <summary>Original source height if this texture had been modified.</summary>
		/// <ogre name="getSrcHeight" />
		public virtual int SrcHeight
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return srcHeight;
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
		protected int srcDepth;

		/// <summary>Original depth of the input texture (only applicable for 3D textures).</summary>
		/// <ogre name="getSrcDepth" />
		public virtual int SrcDepth
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return srcDepth;
			}
		}

		#endregion SrcDepth Property

		#region SrcFormat Property

		/// <summary>Original format of the input texture (only applicable for 3D textures).</summary>
		protected PixelFormat srcFormat = PixelFormat.Unknown;

		/// <summary>Original format of the input texture (only applicable for 3D textures).</summary>
		/// <ogre name="getSrcDepth" />
		public virtual PixelFormat SrcFormat
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return srcFormat;
			}
		}

		#endregion SrcFormat Property

		#region DesiredFormat Property

		/// <summary>Desired format of the input texture (only applicable for 3D textures).</summary>
		protected PixelFormat desiredFormat = PixelFormat.Unknown;

		/// <summary>Desired format of the input texture (only applicable for 3D textures).</summary>
		/// <ogre name="getSrcDepth" />
		public virtual PixelFormat DesiredFormat
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return desiredFormat;
			}
		}

		#endregion DesiredFormat Property

		#region DesiredBitDepth

		protected ushort desiredFloatBitDepth;
		protected ushort desiredIntegerBitDepth;

		/// <summary>
		/// Desired bit depth for integer pixel format textures.
		/// </summary>
		/// <remarks>
		/// Available values: 0, 16 and 32, where 0 (the default) means keep original format
		/// as it is. This value is number of bits for a channel of the pixel.
		/// </remarks>
		public virtual ushort DesiredIntegerBitDepth
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return desiredIntegerBitDepth;
			}

			[OgreVersion( 1, 7, 2 )]
			set
			{
				desiredIntegerBitDepth = value;
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
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return desiredFloatBitDepth;
			}

			[OgreVersion( 1, 7, 2 )]
			set
			{
				desiredFloatBitDepth = value;
			}
		}

		#endregion DesiredBitDepth

		#region FSAA Properties

		protected int fsaa;

		/// <summary>
		/// Get the level of multisample AA to be used if this texture is a rendertarget.
		/// </summary>
		/// <ogre name="getFSAA" />
		public int FSAA
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return fsaa;
			}
		}

		protected string fsaaHint;

		/// <summary>
		/// Get the multisample AA hint if this texture is a rendertarget.
		/// </summary>
		public virtual string FSAAHint
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return fsaaHint;
			}
		}

		/// <summary>
		/// Set the level of multisample AA to be used if this texture is a rendertarget.
		/// </summary>
		/// <note>
		/// This option will be ignored if TU_RENDERTARGET is not part of the
		/// usage options on this texture, or if the hardware does not support it. 
		/// </note>
		/// <param name="fsaa">The number of samples</param>
		/// <param name="fsaaHint">Any hinting text <see cref="Root.CreateRenderWindow"/></param>
		[OgreVersion( 1, 7, 2 )]
		public void SetFSAA( int fsaa, string fsaaHint )
		{
			this.fsaa = fsaa;
			this.fsaaHint = fsaaHint;
		}

		#endregion FSAA Properties

		#region HardwareGammaEnabled Property

		protected bool hwGamma;

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
		public virtual bool HardwareGammaEnabled
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return hwGamma;
			}

			[OgreVersion( 1, 7, 2 )]
			set
			{
				hwGamma = value;
			}
		}

		#endregion HardwareGammaEnabled Property

		/// <summary>
		/// Return the number of faces this texture has. This will be 6 for a cubemap texture and 1 for a 1D, 2D or 3D one.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public virtual int FaceCount
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
		/// <param name="isManual">if set to <c>true</c> [is manual].</param>
		/// <param name="loader">The loader.</param>
		[OgreVersion( 1, 7, 2 )]
#if NET_40
        public Texture( ResourceManager parent, string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader = null )
#else
		public Texture( ResourceManager parent, string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader )
#endif
			: base( parent, name, handle, group, isManual, loader )
		{
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

#if !NET_40
		public Texture( ResourceManager parent, string name, ResourceHandle handle, string group )
			: this( parent, name, handle, group, false, null ) {}
#endif

		#endregion Construction and Destruction

		#region Methods

		/// <summary>
		/// Loads raw image data from the stream into this texture.
		/// </summary>
		/// <note>
		/// Important: only call this from outside the load() routine of a 
		/// Resource. Don't call it within (including ManualResourceLoader) - use
		/// _loadImages() instead. This method is designed to be external, 
		/// performs locking and checks the load status before loading.
		/// </note>
		/// <param name="data">The raw, decoded image data.</param>
		/// <param name="width">Width of the texture data.</param>
		/// <param name="height">Height of the texture data.</param>
		/// <param name="format">Format of the supplied image data.</param>
		/// <ogre name="loadRawData" />
		[OgreVersion( 1, 7, 2 )]
		public virtual void LoadRawData( Stream data, int width, int height, PixelFormat format )
		{
			// load the raw data
			var image = Image.FromRawStream( data, width, height, format );

			// call the polymorphic LoadImage implementation
			LoadImage( image );
		}

		/// <summary>
		/// Loads data from an Image directly into this texture.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public virtual void LoadImage( Image image )
		{
			var old = _loadingState.Value;
			if ( old != Core.LoadingState.Unloaded && old != Core.LoadingState.Prepared )
			{
				return;
			}

			if ( !_loadingState.Cas( old, Core.LoadingState.Loading ) )
			{
				return;
			}

			// Scope lock for actual loading
			try
			{
				lock ( _loadingStatusMutex )
				{
					LoadImages( new Image[]
					            {
					            	image
					            } );
				}
			}
			catch
			{
				// Reset loading in-progress flag in case failed for some reason
				_loadingState.Value = old;
				// Re-throw
				throw;
			}

			_loadingState.Value = Core.LoadingState.Loaded;

			// Notify manager
			if ( this.Creator != null )
			{
				this.Creator.NotifyResourceLoaded( this );
			}

			// No deferred loading events since this method is not called in background
		}

		/// <summary>
		/// Sets the pixel format for the texture surface; can only be set before load().
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public virtual void SetFormat( PixelFormat pf )
		{
			format = pf;
			desiredFormat = pf;
			srcFormat = pf;

			srcBpp = PixelUtil.GetNumElemBytes( pf );
		}

		/// <summary>
		/// Sets desired bit depth for integer and float pixel format.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public virtual void SetDesiredBitDepths( ushort integerBitDepth, ushort floatBitDepth )
		{
			desiredIntegerBitDepth = integerBitDepth;
			desiredFloatBitDepth = floatBitDepth;
		}

		/// <see cref="Resource.calculateSize"/>
		[OgreVersion( 1, 7, 2 )]
		protected override int calculateSize()
		{
			return FaceCount * PixelUtil.GetMemorySize( Width, Height, Depth, Format );
		}

		/// <summary>
		/// Internal method to load the texture from a set of images. 
		/// <note>
		/// Do NOT call this method unless you are inside the load() routine
		/// already, e.g. a ManualResourceLoader. It is not threadsafe and does
		/// not check or update resource loading status.
		/// </note>
		/// </summary>
		///<param name="images">
		/// Vector of pointers to Images. If there is only one image
		/// in this vector, the faces of that image will be used. If there are multiple
		/// images in the vector each image will be loaded as a face.
		/// </param>
		[OgreVersion( 1, 7, 2 )]
		public virtual void LoadImages( Image[] images )
		{
			if ( images.Length < 1 )
			{
				throw new AxiomException( "Cannot load empty vector of images" );
			}

			// Set desired texture size and properties from images[0]
			srcWidth = width = images[ 0 ].Width;
			srcHeight = height = images[ 0 ].Height;
			srcDepth = depth = images[ 0 ].Depth;

			// Get source image format and adjust if required
			srcFormat = images[ 0 ].Format;
			if ( treatLuminanceAsAlpha && srcFormat == PixelFormat.L8 )
			{
				srcFormat = PixelFormat.A8;
			}

			if ( desiredFormat != PixelFormat.Unknown )
			{
				// If have desired format, use it
				format = desiredFormat;
			}
			else
			{
				// Get the format according with desired bit depth
				format = PixelUtil.GetFormatForBitDepths( srcFormat, desiredIntegerBitDepth, desiredFloatBitDepth );
			}

			// The custom mipmaps in the image have priority over everything
			var imageMips = images[ 0 ].NumMipMaps;
			if ( imageMips > 0 )
			{
				mipmapCount = requestedMipmapCount = imageMips;
				// Disable flag for auto mip generation
				usage &= ~TextureUsage.AutoMipMap;
			}

			// Create the texture
			CreateInternalResources();

			// Check if we're loading one image with multiple faces
			// or a vector of images representing the faces
			int faces;
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
			if ( faces > this.FaceCount )
			{
				faces = this.FaceCount;
			}

			// Say what we're doing
			if ( TextureManager.Instance.Verbose )
			{
				var msg = new StringBuilder();
				msg.AppendFormat( "Texture: {0}: Loading {1} faces( {2}, {3}x{4}x{5} ) with", _name, faces, PixelUtil.GetFormatName( images[ 0 ].Format ), images[ 0 ].Width, images[ 0 ].Height, images[ 0 ].Depth );
				if ( !( mipmapsHardwareGenerated && mipmapCount == 0 ) )
				{
					msg.AppendFormat( " {0}", mipmapCount );
				}

				if ( ( usage & TextureUsage.AutoMipMap ) == TextureUsage.AutoMipMap )
				{
					msg.AppendFormat( "{0} generated mipmaps", mipmapsHardwareGenerated ? " hardware" : string.Empty );
				}
				else
				{
					msg.Append( " custom mipmaps" );
				}

				msg.AppendFormat( " from {0}.\n\t", multiImage ? "multiple Images" : "an Image" );

				// Print data about first destination surface
				var buf = GetBuffer( 0, 0 );
				msg.AppendFormat( " Internal format is {0} , {1}x{2}x{3}.", PixelUtil.GetFormatName( buf.Format ), buf.Width, buf.Height, buf.Depth );

				LogManager.Instance.Write( msg.ToString() );
			}

			// Main loading loop
			// imageMips == 0 if the image has no custom mipmaps, otherwise contains the number of custom mips
			for ( var mip = 0; mip <= imageMips; ++mip )
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
					}

					// Sets to treated format in case is difference
					src.Format = srcFormat;

					if ( gamma != 1.0f )
					{
						// Apply gamma correction
						// Do not overwrite original image but do gamma correction in temporary buffer
						var bufSize = PixelUtil.GetMemorySize( src.Width, src.Height, src.Depth, src.Format );
						var buff = new byte[ bufSize ];
						var buffer = BufferBase.Wrap( buff );

						var corrected = new PixelBox( src.Width, src.Height, src.Depth, src.Format, buffer );
						PixelConverter.BulkPixelConversion( src, corrected );

						Image.ApplyGamma( corrected.Data, gamma, corrected.ConsecutiveSize, PixelUtil.GetNumElemBits( src.Format ) );

						// Destination: entire texture. BlitFromMemory does
						// the scaling to a power of two for us when needed
						GetBuffer( i, mip ).BlitFromMemory( corrected );
					}
					else
					{
						// Destination: entire texture. BlitFromMemory does
						// the scaling to a power of two for us when needed
						GetBuffer( i, mip ).BlitFromMemory( src );
					}
				}
			}

			// Update size (the final size, not including temp space)
			Size = this.FaceCount * PixelUtil.GetMemorySize( width, height, depth, format );
		}

		/// <summary>
		/// Creates the internal texture resources for this texture.
		/// </summary>
		/// <remarks>
		/// This method creates the internal texture resources (pixel buffers, 
		/// texture surfaces etc) required to begin using this texture. You do
		/// not need to call this method directly unless you are manually creating
		/// a texture, in which case something must call it, after having set the
		/// size and format of the texture (e.g. the ManualResourceLoader might
		/// be the best one to call it). If you are not defining a manual texture,
		/// or if you use one of the self-contained load...() methods, then it will be
		/// called for you.
		/// </remarks>
		[OgreVersion( 1, 7, 2 )]
		public virtual void CreateInternalResources()
		{
			if ( !internalResourcesCreated )
			{
				createInternalResources();
				internalResourcesCreated = true;
			}
		}

		/// <summary>
		/// Implementation of creating internal texture resources
		/// </summary>
		protected abstract void createInternalResources();

		/// <summary>
		/// Frees internal texture resources for this texture.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void FreeInternalResources()
		{
			if ( internalResourcesCreated )
			{
				freeInternalResources();
				internalResourcesCreated = false;
			}
		}

		/// <summary>
		/// Implementation of freeing internal texture resources
		/// </summary>
		protected abstract void freeInternalResources();

		/// <summary>
		/// Default implementation of unload which calls freeInternalResources
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		protected override void unload()
		{
			FreeInternalResources();
		}

		/// <summary>
		/// Copies (and maybe scales to fit) the contents of this texture to
		/// another texture.
		/// </summary>
		[OgreVersion( 1, 7, 2, "Original name was CopyToTexture" )]
		public virtual void CopyTo( Texture target )
		{
			if ( target.FaceCount != this.FaceCount )
			{
				throw new AxiomException( "Texture types must match!" );
			}

			var numMips = Axiom.Math.Utility.Min( this.MipmapCount, target.MipmapCount );
			if ( ( usage & TextureUsage.AutoMipMap ) == TextureUsage.AutoMipMap || ( target.Usage & TextureUsage.AutoMipMap ) == TextureUsage.AutoMipMap )
			{
				numMips = 0;
			}

			for ( var face = 0; face < this.FaceCount; face++ )
			{
				for ( var mip = 0; mip <= numMips; mip++ )
				{
					target.GetBuffer( face, mip ).Blit( GetBuffer( face, mip ) );
				}
			}
		}

		/// <summary>
		/// Identify the source file type as a string, either from the extension
		/// or from a magic number.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		protected string GetSourceFileType()
		{
			if ( string.IsNullOrEmpty( _name ) )
			{
				return string.Empty;
			}

			var pos = _name.LastIndexOf( "." );
			if ( pos != -1 && pos < ( _name.Length - 1 ) )
			{
				return _name.Substring( pos + 1 ).ToLower();
			}
			else
			{
				// No extension
				Stream dstream = null;
				try
				{
					dstream = ResourceGroupManager.Instance.OpenResource( _name, _group, true, null );
				}
				catch {}
				if ( dstream == null && TextureType == Graphics.TextureType.CubeMap )
				{
					// try again with one of the faces (non-dds)
					try
					{
						dstream = ResourceGroupManager.Instance.OpenResource( _name + "_rt", _group, true, null );
					}
					catch {}
				}

				if ( dstream != null )
				{
					return Image.GetFileExtFromMagic( dstream );
				}
			}

			return string.Empty;
		}

		/// <summary>
		/// Populate an Image with the contents of this texture.
		/// </summary>
		/// <param name="destImage">The target image (contents will be overwritten)</param>
		/// <param name="includeMipMaps">Whether to embed mipmaps in the image</param>
		[OgreVersion( 1, 7, 2 )]
#if NET_40
        public virtual void ConvertToImage( out Image destImage, bool includeMipMaps = false )
#else
		public virtual void ConvertToImage( out Image destImage, bool includeMipMaps )
#endif
		{
			var numMips = includeMipMaps ? this.MipmapCount + 1 : 1;
			var dataSize = Image.CalculateSize( numMips, this.FaceCount, this.Width, this.Height, this.Depth, this.Format );

			var pixData = new byte[ dataSize ];
			// if there are multiple faces and mipmaps we must pack them into the data
			// faces, then mips
			var currentPixData = BufferBase.Wrap( pixData );

			for ( var face = 0; face < this.FaceCount; ++face )
			{
				for ( var mip = 0; mip < numMips; ++mip )
				{
					var mipDataSize = PixelUtil.GetMemorySize( this.Width, this.Height, this.Depth, this.Format );

					var pixBox = new PixelBox( this.Width, this.Height, this.Depth, this.Format, currentPixData );
					GetBuffer( face, mip ).BlitToMemory( pixBox );

					currentPixData += mipDataSize;
				}
			}

			currentPixData.Dispose();

			// load, and tell Image to delete the memory when it's done.
			destImage = ( new Image() ).FromDynamicImage( pixData, this.Width, this.Height, this.Depth, this.Format, true, this.FaceCount, numMips - 1 );
		}

#if !NET_40
		/// <see cref="Texture.ConvertToImage(out Image, bool)"/>
		public void ConvertToImage( out Image destImage )
		{
			ConvertToImage( out destImage, false );
		}
#endif

		/// <summary>
		/// Return hardware pixel buffer for a surface. This buffer can then
		/// be used to copy data from and to a particular level of the texture.
		/// </summary>
		/// <param name="face">
		/// Face number, in case of a cubemap texture. Must be 0
		/// for other types of textures.
		/// For cubemaps, this is one of
		/// +X (0), -X (1), +Y (2), -Y (3), +Z (4), -Z (5)
		/// </param>
		/// <param name="mipmap">
		/// Mipmap level. This goes from 0 for the first, largest
		/// mipmap level to getNumMipmaps()-1 for the smallest.
		/// </param>
		/// <remarks>
		/// The buffer is invalidated when the resource is unloaded or destroyed.
		/// Do not use it after the lifetime of the containing texture.
		/// </remarks>
		/// <returns>A shared pointer to a hardware pixel buffer</returns>
		[OgreVersion( 1, 7, 2 )]
#if NET_40
        public abstract HardwarePixelBuffer GetBuffer( int face = 0, int mipmap = 0 );
#else
		public abstract HardwarePixelBuffer GetBuffer( int face, int mipmap );

		/// <see cref="Texture.GetBuffer(int, int)"/>
		public HardwarePixelBuffer GetBuffer()
		{
			return GetBuffer( 0, 0 );
		}

		/// <see cref="Texture.GetBuffer(int, int)"/>
		public HardwarePixelBuffer GetBuffer( int face )
		{
			return GetBuffer( face, 0 );
		}
#endif

		#endregion Methods
	}
}
