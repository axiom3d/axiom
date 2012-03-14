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
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.IO;

using Axiom.Core;
using Axiom.CrossPlatform;
using Axiom.Graphics;
using Axiom.Math;
using Axiom.Media;

using CodePoint = System.UInt32;
using ResourceHandle = System.UInt64;
using UVRect = Axiom.Core.RectangleF;

#endregion Namespace Declarations

namespace Axiom.Fonts
{
	/// <summary>
	///		Possible font sources for use in the engine.
	/// </summary>
	public enum FontType
	{
		/// <summary>System truetype fonts, as well as supplementary .ttf files.</summary>
		TrueType,

		/// <summary>Character image map created by an artist.</summary>
		Image
	}

	/// <summary>
	///		This class is simply a way of getting a font texture into the engine and
	///		to easily retrieve the texture coordinates required to accurately render them.
	///		Fonts can either be loaded from precreated textures, or the texture can be generated
	///		using a truetype font. You can either create the texture manually in code, or you
	///		can use an XML font script to define it (probably more practical since you can reuse
	///		the definition more easily)
	/// </summary>
	/// <remarks>	
	/// This class extends both Resource and ManualResourceLoader since it is
	/// both a resource in it's own right, but it also provides the manual load
	/// implementation for the Texture it creates.
	/// </remarks>
	/// 
	/// <ogre name="Font">
	///     <file name="OgreFont.h"   revision="1.14" lastUpdated="5/22/2006" lastUpdatedBy="Borrillis" />
	///     <file name="OgreFont.cpp" revision="1.32.2.2" lastUpdated="5/22/2006" lastUpdatedBy="Borrillis" />
	/// </ogre> 
	/// 
	public class Font : Resource, IManualResourceLoader
	{
		#region Internal Classes and Structures

		public struct GlyphInfo
		{
			public Real aspectRatio;
			public CodePoint codePoint;
			public UVRect uvRect;

			public GlyphInfo( CodePoint id, UVRect rect, Real aspect )
			{
				this.codePoint = id;
				this.uvRect = rect;
				this.aspectRatio = aspect;
			}
		}

		#endregion Internal Classes and Structures

		#region Constants

		private const int BITMAP_HEIGHT = 512;
		private const int BITMAP_WIDTH = 512;
		private const int START_CHAR = 33;
		private const int END_CHAR = 127;

		#endregion

		#region Fields and Properties

		#region MaxBearingY

		/// <summary>
		///  Max distance to baseline of this (truetype) font
		/// </summary>
		private int maxBearingY;

		#endregion MaxBearingY

		#region FontType Property

		/// <summary>
		///    Type of font, either imag based or TrueType.
		/// </summary>
		private FontType _fontType;

		/// <summary>
		///    Type of font.
		/// </summary>
		public FontType Type
		{
			get
			{
				return this._fontType;
			}
			set
			{
				this._fontType = value;
			}
		}

		#endregion FontType Property

		#region Source Property

		/// <summary>
		///    Source of the font (either an image name or a truetype font)
		/// </summary>
		public string Source { get; set; }

		#endregion Source Property

		#region TrueTypeSize Property

		/// <summary>
		///    Size of the truetype font, in points.
		/// </summary>
		private int _ttfSize;

		/// <summary>
		///    Size of the truetype font, in points.
		/// </summary>
		public int TrueTypeSize
		{
			get
			{
				return this._ttfSize;
			}
			set
			{
				this._ttfSize = value;
			}
		}

		#endregion TrueTypeSize Property

		#region TrueTypeResolution Property

		/// <summary>
		///    Resolution (dpi) of truetype font.
		/// </summary>
		private int _ttfResolution;

		/// <summary>
		///    Resolution (dpi) of truetype font.
		/// </summary>
		public int TrueTypeResolution
		{
			get
			{
				return this._ttfResolution;
			}
			set
			{
				this._ttfResolution = value;
			}
		}

		#endregion TrueTypeResolution Property

		#region Material Property

		/// <summary>
		///    Material create for use on entities by this font.
		/// </summary>
		private Material _material;

		/// <summary>
		///    Gets a reference to the material being used for this font.
		/// </summary>
		public Material Material
		{
			get
			{
				return this._material;
			}
			protected set
			{
				this._material = value;
			}
		}

		#endregion Material Property

		#region texture Property

		/// <summary>
		///    Material create for use on entities by this font.
		/// </summary>
		private Texture _texture;

		/// <summary>
		///    Gets a reference to the material being used for this font.
		/// </summary>
		protected Texture texture
		{
			get
			{
				return this._texture;
			}
			set
			{
				this._texture = value;
			}
		}

		#endregion texture Property

		#region AntiAliasColor Property

		/// <summary>
		///    Sets whether or not the color of this font is antialiased as it is generated
		///    from a TrueType font.
		/// </summary>
		/// <remarks>
		///    This is valid only for a TrueType font. If you are planning on using 
		///    alpha blending to draw your font, then it is a good idea to set this to
		///    false (which is the default), otherwise the darkening of the font will combine
		///    with the fading out of the alpha around the edges and make your font look thinner
		///    than it should. However, if you intend to blend your font using a color blending
		///    mode (add or modulate for example) then it's a good idea to set this to true, in
		///    order to soften your font edges.
		/// </remarks>
		public bool AntialiasColor { get; set; }

		#endregion AntiAliasColor Property

		#region Glyphs Property

		private readonly Dictionary<CodePoint, GlyphInfo> codePoints = new Dictionary<CodePoint, GlyphInfo>();

		public IDictionary<CodePoint, GlyphInfo> Glyphs
		{
			get
			{
				return this.codePoints;
			}
		}

		#endregion Glyphs Property

		#region showLines Property

		protected bool showLines { get; set; }

		#endregion showLines Property

		protected List<KeyValuePair<int, int>> codePointRange = new List<KeyValuePair<int, int>>();

		#endregion Fields and Properties

		#region Constructors and Destructor

		/// <summary>
		///		Constructor, should be called through FontManager.Create().
		/// </summary>
		public Font( ResourceManager parent, string name, ResourceHandle handle, string group )
			: this( parent, name, handle, group, false, null ) { }

		public Font( ResourceManager parent, string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader )
			: base( parent, name, handle, group, isManual, loader ) { }

		#endregion Constructors and Destructor

		#region Methods

		protected void createTexture()
		{
			// Just create the texture here, and point it at ourselves for when
			// it wants to (re)load for real
			string texName = Name + "FontTexture";
			// Create, setting isManual to true and passing self as loader
			texture = (Texture)TextureManager.Instance.Create( texName, Group, true, this, null );
			texture.TextureType = TextureType.TwoD;
			texture.MipmapCount = 0;
			texture.Load();

			TextureUnitState t = Material.GetTechnique( 0 ).GetPass( 0 ).CreateTextureUnitState( texName );
			// Allow min/mag filter, but no mip
			t.SetTextureFiltering( FilterOptions.Linear, FilterOptions.Linear, FilterOptions.None );
		}

		/// <summary>Returns the size in pixels of a box that could contain the whole string.</summary>
		private Pair<int> StrBBox( string text, float char_height, RenderWindow window )
		{
			int height = 0, width = 0;
			Real vsX, vsY, veX, veY;
			int w, h;

			w = window.Width;
			h = window.Height;

			for ( int i = 0; i < text.Length; i++ )
			{
				GetGlyphTexCoords( text[ i ], out vsX, out vsY, out veX, out veY );

				// Calculate view-space width and height of char
				vsY = char_height;
				vsX = GetGlyphAspectRatio( text[ i ] ) * char_height;

				width += (int)( vsX * w );
				if ( vsY * h > height || ( ( i == 0 ) && text[ i - 1 ] == '\n' ) )
				{
					height += (int)( vsY * h );
				}
			}

			return new Pair<int>( height, width );
		}

		/// <summary>
		///		Retrieves the texture coordinates for the specifed character in this font.
		/// </summary>
		/// <param name="c"></param>
		/// <param name="u1"></param>
		/// <param name="u2"></param>
		/// <param name="v1"></param>
		/// <param name="v2"></param>
		[Obsolete( "Use Glyphs property" )]
		public void GetGlyphTexCoords( CodePoint c, out Real u1, out Real v1, out Real u2, out Real v2 )
		{
			if ( this.codePoints.ContainsKey( c ) )
			{
				GlyphInfo glyph = this.codePoints[ c ];
				u1 = glyph.uvRect.Top;
				v1 = glyph.uvRect.Left;
				u2 = glyph.uvRect.Bottom;
				v2 = glyph.uvRect.Right;
			}
			else
			{
				u1 = v1 = u2 = v2 = 0.0f;
			}
		}

		/// <summary>
		/// Sets the texture coordinates of a glyph.
		/// </summary>
		/// <param name="c"></param>
		/// <param name="u1"></param>
		/// <param name="v1"></param>
		/// <param name="u2"></param>
		/// <param name="v2"></param>
		/// <remarks>
		/// You only need to call this if you're setting up a font loaded from a texture manually.
		/// </remarks>
		[Obsolete( "Use Glyphs property" )]
		public void SetGlyphTexCoords( CodePoint c, Real u1, Real v1, Real u2, Real v2 )
		{
			SetGlyphTexCoords( c, u1, v1, u2, v2, ( u2 - u1 ) / ( v2 - v1 ) );
		}

		/// <summary>
		/// Sets the texture coordinates of a glyph.
		/// </summary>
		/// <param name="c"></param>
		/// <param name="u1"></param>
		/// <param name="v1"></param>
		/// <param name="u2"></param>
		/// <param name="v2"></param>
		/// <param name="aspect"></param>
		/// <remarks>
		/// You only need to call this if you're setting up a font loaded from a texture manually.
		/// <para>
		/// Also sets the aspect ratio (width / height) of this character. textureAspect
		/// is the width/height of the texture (may be non-square)
		/// </para>
		/// </remarks>
		[Obsolete( "Use Glyphs property" )]
		public void SetGlyphTexCoords( CodePoint c, Real u1, Real v1, Real u2, Real v2, Real aspect )
		{
			var glyph = new GlyphInfo( c, new UVRect( v1, u1, v2, u2 ), aspect * ( u2 - u1 ) / ( v2 - v1 ) );
			if ( this.codePoints.ContainsKey( c ) )
			{
				this.codePoints[ c ] = glyph;
			}
			else
			{
				this.codePoints.Add( c, glyph );
			}
		}

		/// <summary>
		///		Finds the aspect ratio of the specified character in this font.
		/// </summary>
		/// <param name="c"></param>
		/// <returns></returns>
		[Obsolete( "Use Glyphs property" )]
		public float GetGlyphAspectRatio( char c )
		{
			if ( this.codePoints.ContainsKey( c ) )
			{
				return this.codePoints[ c ].aspectRatio;
			}

			return 1.0f;
		}

		#endregion Methods

		#region Implementation of Resource

		protected override void load()
		{
			// clarabie - nov 18, 2008
			// modified this to check for an existing material instead of always
			// creating a new one. Allows more flexibility, but also specifically allows us to
			// solve the problem of XNA not having fixed function support

			this._material = (Material)MaterialManager.Instance.GetByName( "Fonts/" + _name );

			if ( this._material == null )
			{
				// create a material for this font
				this._material = (Material)MaterialManager.Instance.Create( "Fonts/" + _name, Group );

				TextureUnitState unitState = null;
				bool blendByAlpha = false;

				if ( this._fontType == FontType.TrueType )
				{
#if !( XBOX || XBOX360 )
					// create the font bitmap on the fly
					createTexture();

					// a texture layer was added in CreateTexture
					unitState = this._material.GetTechnique( 0 ).GetPass( 0 ).GetTextureUnitState( 0 );

					blendByAlpha = true;
#endif
				}
				else
				{
					// load this texture
					// TODO In general, modify any methods like this that throw their own exception rather than returning null, so the caller can decide how to handle a missing resource.

					this._texture = TextureManager.Instance.Load( Source, Group, TextureType.TwoD, 0 );

					blendByAlpha = texture.HasAlpha;
					// pre-created font images
					unitState = Material.GetTechnique( 0 ).GetPass( 0 ).CreateTextureUnitState( Source );
				}

				// Make sure material is aware of colour per vertex.
				this._material.GetTechnique( 0 ).GetPass( 0 ).VertexColorTracking = TrackVertexColor.Diffuse;

				if ( unitState != null )
				{
					// Clamp to avoid fuzzy edges
					unitState.SetTextureAddressingMode( TextureAddressing.Clamp );
					// Allow min/mag filter, but no mip
					unitState.SetTextureFiltering( FilterOptions.Linear, FilterOptions.Linear, FilterOptions.None );
				}

				// set up blending mode
				if ( blendByAlpha )
				{
					this._material.SetSceneBlending( SceneBlendType.TransparentAlpha );
				}
				else
				{
					// assume black background here
					this._material.SetSceneBlending( SceneBlendType.Add );
				}
			}
		}

		protected override void unload()
		{
			if ( this._material != null )
			{
				MaterialManager.Instance.Remove( this._material );
				this._material.Unload();
				this._material = null;
			}

			if ( this._texture != null )
			{
				TextureManager.Instance.Remove( this._texture );
				this._texture.Unload();
				this._texture = null;
			}
		}

		protected override int calculateSize()
		{
			// permanent resource is in the texture 
			return 0;
		}

		#endregion Implementation of Resource

		#region Implementation of IManualResourceLoader

		/// <summary>
		/// Implementation of ManualResourceLoader::loadResource, called
		/// when the Texture that this font creates needs to (re)load.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public void LoadResource( Resource res )
		{
			// TODO : Revisit after checking current Imaging support in Mono.
#if !(XBOX || XBOX360 || ANDROID || IPHONE || SILVERLIGHT)
			string current = Environment.CurrentDirectory;

			IntPtr ftLibrary = IntPtr.Zero;
			if ( FT.FT_Init_FreeType( out ftLibrary ) != 0 )
			{
				throw new AxiomException( "Could not init FreeType library!" );
			}

			IntPtr face = IntPtr.Zero;
			// Add a gap between letters vert and horz
			// prevents nasty artefacts when letters are too close together
			int char_space = 5;

			// Locate ttf file, load it pre-buffered into memory by wrapping the
			// original DataStream in a MemoryDataStream
			Stream fileStream = ResourceGroupManager.Instance.OpenResource( Source, Group, true, this );

			var ttfchunk = new byte[ fileStream.Length ];
			fileStream.Read( ttfchunk, 0, ttfchunk.Length );

			//Load font
			if ( FT.FT_New_Memory_Face( ftLibrary, ttfchunk, ttfchunk.Length, 0, out face ) != 0 )
			{
				throw new AxiomException( "Could not open font face!" );
			}

			// Convert our point size to freetype 26.6 fixed point format
			int ftSize = this._ttfSize * ( 1 << 6 );

			if ( FT.FT_Set_Char_Size( face, ftSize, 0, (uint)this._ttfResolution, (uint)this._ttfResolution ) != 0 )
			{
				throw new AxiomException( "Could not set char size!" );
			}

			int max_height = 0, max_width = 0;

			// Backwards compatibility - if codepoints not supplied, assume 33-166
			if ( this.codePointRange.Count == 0 )
			{
				this.codePointRange.Add( new KeyValuePair<int, int>( 33, 166 ) );
			}

			// Calculate maximum width, height and bearing
			int glyphCount = 0;
			foreach ( var r in this.codePointRange )
			{
				KeyValuePair<int, int> range = r;
				for ( int cp = range.Key; cp <= range.Value; ++cp, ++glyphCount )
				{
					FT.FT_Load_Char( face, (uint)cp, 4 ); //4 == FT_LOAD_RENDER

					var rec = face.PtrToStructure<FT_FaceRec>();
					var glyp = rec.glyph.PtrToStructure<FT_GlyphSlotRec>();

					if ( ( 2 * ( glyp.bitmap.rows << 6 ) - glyp.metrics.horiBearingY ) > max_height )
					{
						max_height = ( 2 * ( glyp.bitmap.rows << 6 ) - glyp.metrics.horiBearingY );
					}
					if ( glyp.metrics.horiBearingY > this.maxBearingY )
					{
						this.maxBearingY = glyp.metrics.horiBearingY;
					}

					if ( ( glyp.advance.x >> 6 ) + ( glyp.metrics.horiBearingX >> 6 ) > max_width )
					{
						max_width = ( glyp.advance.x >> 6 ) + ( glyp.metrics.horiBearingX >> 6 );
					}
				}
			}

			// Now work out how big our texture needs to be
			int rawSize = ( max_width + char_space ) * ( ( max_height >> 6 ) + char_space ) * glyphCount;

			var tex_side = (int)System.Math.Sqrt( (Real)rawSize );

			// just in case the size might chop a glyph in half, add another glyph width/height
			tex_side += System.Math.Max( max_width, ( max_height >> 6 ) );
			// Now round up to nearest power of two
			var roundUpSize = (int)Bitwise.FirstPO2From( (uint)tex_side );
			// Would we benefit from using a non-square texture (2X width)
			int finalWidth = 0, finalHeight = 0;

			if ( roundUpSize * roundUpSize * 0.5 >= rawSize )
			{
				finalHeight = (int)( roundUpSize * 0.5 );
			}
			else
			{
				finalHeight = roundUpSize;
			}

			finalWidth = roundUpSize;

			Real textureAspec = finalWidth / (Real)finalHeight;
			int pixelBytes = 2;
			int dataWidth = finalWidth * pixelBytes;
			int dataSize = finalWidth * finalHeight * pixelBytes;

			LogManager.Instance.Write( "Font {0} using texture size {1}x{2}", _name, finalWidth.ToString(), finalHeight.ToString() );

			var imageData = new byte[ dataSize ];
			// Reset content (White, transparent)
			for ( int i = 0; i < dataSize; i += pixelBytes )
			{
				imageData[ i + 0 ] = 0xff; // luminance
				imageData[ i + 1 ] = 0x00; // alpha
			}

			int l = 0, m = 0;
			foreach ( var r in this.codePointRange )
			{
				KeyValuePair<int, int> range = r;
				for ( int cp = range.Key; cp <= range.Value; ++cp )
				{
					// Load & render glyph
					int ftResult = FT.FT_Load_Char( face, (uint)cp, 4 ); //4 == FT_LOAD_RENDER
					if ( ftResult != 0 )
					{
						// problem loading this glyph, continue
						LogManager.Instance.Write( "Info: cannot load character '{0}' in font {1}.",
#if (SILVERLIGHT || WINDOWS_PHONE)
							cp,
#else
 char.ConvertFromUtf32( cp ),
#endif
 _name );

						continue;
					}

					var rec = face.PtrToStructure<FT_FaceRec>();
					var glyp = rec.glyph.PtrToStructure<FT_GlyphSlotRec>();
					int advance = glyp.advance.x >> 6;

					if ( glyp.bitmap.buffer == IntPtr.Zero )
					{
						LogManager.Instance.Write( "Info: Freetype returned null for character '{0} in font {1}.",
#if (SILVERLIGHT || WINDOWS_PHONE)
							cp,
#else
 char.ConvertFromUtf32( cp ),
#endif
 _name );
						continue;
					}

#if !AXIOM_SAFE_ONLY
					unsafe
#endif
					{
						BufferBase buffer = BufferBase.Wrap( glyp.bitmap.buffer, glyp.bitmap.rows * glyp.bitmap.pitch );
						byte* bufferPtr = buffer.ToBytePointer();
						int idx = 0;
						BufferBase imageDataBuffer = BufferBase.Wrap( imageData );
						byte* imageDataPtr = imageDataBuffer.ToBytePointer();
						int y_bearing = ( ( this.maxBearingY >> 6 ) - ( glyp.metrics.horiBearingY >> 6 ) );
						int x_bearing = glyp.metrics.horiBearingX >> 6;

						for ( int j = 0; j < glyp.bitmap.rows; j++ )
						{
							int row = j + m + y_bearing;
							int pDest = ( row * dataWidth ) + ( l + x_bearing ) * pixelBytes;
							for ( int k = 0; k < glyp.bitmap.width; k++ )
							{
								if ( AntialiasColor )
								{
									// Use the same greyscale pixel for all components RGBA
									imageDataPtr[ pDest++ ] = bufferPtr[ idx ];
								}
								else
								{
									// Always white whether 'on' or 'off' pixel, since alpha
									// will turn off
									imageDataPtr[ pDest++ ] = 0xFF;
								}
								// Always use the greyscale value for alpha
								imageDataPtr[ pDest++ ] = bufferPtr[ idx++ ];
							} //end k
						} //end j

						buffer.Dispose();
						imageDataBuffer.Dispose();

						SetGlyphTexCoords( (uint)cp, l / (Real)finalWidth, //u1
										   m / (Real)finalHeight, //v1
										   ( l + ( glyp.advance.x >> 6 ) ) / (Real)finalWidth, //u2
										   ( m + ( max_height >> 6 ) ) / (Real)finalHeight, //v2
										   textureAspec );

						// Advance a column
						l += ( advance + char_space );

						// If at end of row
						if ( finalWidth - 1 < l + ( advance ) )
						{
							m += ( max_height >> 6 ) + char_space;
							l = 0;
						}
					}
				}
			} //end foreach

			var memStream = new MemoryStream( imageData );
			Image img = Image.FromRawStream( memStream, finalWidth, finalHeight, PixelFormat.BYTE_LA );

			var tex = (Texture)res;
			// Call internal _loadImages, not loadImage since that's external and 
			// will determine load status etc again, and this is a manual loader inside load()
			var images = new Image[ 1 ];
			images[ 0 ] = img;
			tex.LoadImages( images );
			FT.FT_Done_FreeType( ftLibrary );

			//img.Save( "C:" + Path.DirectorySeparatorChar + Name + ".png" );
			//FileStream file = new FileStream( "C:" + Path.DirectorySeparatorChar + Name + ".fontdef", FileMode.Create );
			//StreamWriter str = new StreamWriter( file );
			//str.WriteLine( Name );
			//str.WriteLine( "{" );
			//str.WriteLine( "\ttype\timage" );
			//str.WriteLine( "\tsource\t{0}.png\n", Name );

			//for ( uint i = 0; i < (uint)( END_CHAR - START_CHAR ); i++ )
			//{
			//    char c = (char)( i + START_CHAR );
			//    str.WriteLine( "\tglyph\t{0}\t{1:F6}\t{2:F6}\t{3:F6}\t{4:F6}", c, Glyphs[ c ].uvRect.Top, Glyphs[ c ].uvRect.Left, Glyphs[ c ].uvRect.Bottom, Glyphs[ c ].uvRect.Right );
			//}
			//str.WriteLine( "}" );
			//str.Close();
			//file.Close();
#endif
		}

		#endregion Implementation of IManualResourceLoader
	}
}
