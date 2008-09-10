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
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using SDI = System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

using Axiom.Core;
using Axiom.Graphics;
using Axiom.Media;

using ResourceHandle = System.UInt64;
using Real = System.Single;
using CodePoint = System.UInt32;
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
			public CodePoint codePoint;
			public UVRect uvRect;
			public Real aspectRatio;

            public GlyphInfo( CodePoint id, UVRect rect, Real aspect )
            {
                codePoint = id;
                uvRect = rect;
                aspectRatio = aspect;
            }
		}

        #endregion Internal Classes and Structures

        #region Constants

        const int BITMAP_HEIGHT = 512;
		const int BITMAP_WIDTH = 512;
		const int START_CHAR = 33;
		const int END_CHAR = 127;

		#endregion

		#region Fields and Properties


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
				return _fontType;
			}
			set
			{
				_fontType = value;
			}
		}

		#endregion FontType Property

		#region Source Property

		/// <summary>
		///    Source of the font (either an image name or a TrueType font).
		/// </summary>
		private string _source;
		/// <summary>
		///    Source of the font (either an image name or a truetype font)
		/// </summary>
		public string Source
		{
			get
			{
				return _source;
			}
			set
			{
				_source = value;
			}
		}

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
				return _ttfSize;
			}
			set
			{
				_ttfSize = value;
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
				return _ttfResolution;
			}
			set
			{
				_ttfResolution = value;
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
				return _material;
			}
			protected set
			{
				_material = value;
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
				return _texture;
			}
			set
			{
				_texture = value;
			}
		}

		#endregion texture Property

		#region AntiAliasColor Property

		/// <summary>
		///    For TrueType fonts only.
		/// </summary>
		private bool _antialiasColor;
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
		public bool AntialiasColor
		{
			get
			{
				return _antialiasColor;
			}
			set
			{
				_antialiasColor = value;
			}
		}

		#endregion AntiAliasColor Property

        #region Glyphs Property

        Dictionary<CodePoint, GlyphInfo> codePoints = new Dictionary<CodePoint, GlyphInfo>();

        public IDictionary<CodePoint, GlyphInfo> Glyphs
        {
            get
            {
                return codePoints;
            }
        }

        #endregion Glyphs Property

		#region showLines Property

		private bool _showLines = false;
		protected bool showLines
		{
			get
			{
				return _showLines;
			}
			set
			{
				_showLines = value;
			}
		}

		#endregion showLines Property

		#endregion Fields and Properties

		#region Constructors and Destructor

		/// <summary>
		///		Constructor, should be called through FontManager.Create().
		/// </summary>
		public Font( ResourceManager parent, string name, ResourceHandle handle, string group )
			: this( parent, name, handle, group, false, null )
		{
		}

		public Font( ResourceManager parent, string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader )
			: base( parent, name, handle, group, isManual, loader )
		{
		}

		#endregion Constructors and Destructor

		#region Methods

		protected void createTexture()
		{
			// Just create the texture here, and point it at ourselves for when
			// it wants to (re)load for real
			String texName = Name + "FontTexture";
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
		Pair<int> StrBBox( string text, float char_height, RenderWindow window )
		{
			int height = 0, width = 0;
			float vsX, vsY, veX, veY;
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
					height += (int)( vsY * h );
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
            if ( codePoints.ContainsKey( c ) )
            {
                GlyphInfo glyph = codePoints[ c ];
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
            GlyphInfo glyph = new GlyphInfo( c, new UVRect(v1, u1, v2, u2), aspect );
            if ( codePoints.ContainsKey( c ) )
                codePoints[ c ] = glyph;
            else
                codePoints.Add( c, glyph );
		}

		/// <summary>
		///		Finds the aspect ratio of the specified character in this font.
		/// </summary>
		/// <param name="c"></param>
		/// <returns></returns>
        [Obsolete( "Use Glyphs property")]
		public float GetGlyphAspectRatio( char c )
		{
            if ( codePoints.ContainsKey( c ) )
            {
                return codePoints[ c ].aspectRatio;
            }

			return 1.0f;
		}


		#endregion Methods

		#region Implementation of Resource

		protected override void load()
		{
			// create a material for this font
			Material = (Material)MaterialManager.Instance.Create( "Fonts/" + Name, Group );

			TextureUnitState texLayer = null;
			bool blendByAlpha = false;

			if ( _fontType == FontType.TrueType )
			{
				// create the font bitmap on the fly
				createTexture();

				// a texture layer was added in CreateTexture
				texLayer = Material.GetTechnique( 0 ).GetPass( 0 ).GetTextureUnitState( 0 );

				blendByAlpha = true;
			}
			else
			{

				// load this texture
				// TODO In general, modify any methods like this that throw their own exception rather than returning null, so the caller can decide how to handle a missing resource.
				_texture = TextureManager.Instance.Load( Source, Group, TextureType.TwoD, 0 );

				blendByAlpha = texture.HasAlpha;
				// pre-created font images
				texLayer = Material.GetTechnique( 0 ).GetPass( 0 ).CreateTextureUnitState( Source );
			}

			// set texture addressing mode to Clamp to eliminate fuzzy edges
			texLayer.TextureAddressing = TextureAddressing.Clamp;
			// Allow min/mag filter, but no mip
			texLayer.SetTextureFiltering( FilterOptions.Linear, FilterOptions.Linear, FilterOptions.None );

			// set up blending mode
			if ( blendByAlpha )
			{
				Material.SetSceneBlending( SceneBlendType.TransparentAlpha );
			}
			else
			{
				// assume black background here
				Material.SetSceneBlending( SceneBlendType.Add );
			}

		}

		protected override void unload()
		{
            if ( _material != null )
            {
                MaterialManager.Instance.Remove( _material );
                _material.Unload();
                _material = null;
            }

            if ( _texture != null )
            {
                TextureManager.Instance.Remove( _texture );
                _texture.Unload();
                _texture = null;
            }
		}

		protected override int calculateSize()
		{
			// permanent resource is in the texture 
			return 0;
		}

		#endregion Implementation of Resource

		#region Implementation of IManualResourceLoader

		public void LoadResource( Resource resource )
		{
			// TODO : Revisit after checking current Imaging support in Mono.

			// create a new bitamp with the size defined
			System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap( BITMAP_WIDTH, BITMAP_HEIGHT, System.Drawing.Imaging.PixelFormat.Format32bppArgb );

			// get a handles to the graphics context of the bitmap
			System.Drawing.Graphics g = System.Drawing.Graphics.FromImage( bitmap );

			// get a font object for the specified font
			System.Drawing.Font font = new System.Drawing.Font( Name, 18 );

			// create a pen for the grid lines
			System.Drawing.Pen linePen = new System.Drawing.Pen( System.Drawing.Color.Red );

			// clear the image to transparent
			g.Clear( System.Drawing.Color.Transparent );

			// nice smooth text
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

			// used for calculating position in the image for rendering the characters
			int x, y, maxHeight;
			x = y = maxHeight = 0;

			// loop through each character in the glyph string and draw it to the bitmap
			for ( int i = START_CHAR; i < END_CHAR; i++ )
			{
				char c = (char)i;

				// are we gonna wrap?
				if ( x + font.Size > BITMAP_WIDTH - 5 )
				{
					// increment the y coord and reset x to move to the beginning of next line
					y += maxHeight;
					x = 0;
					maxHeight = 0;

					if ( showLines )
					{
						// draw a horizontal line underneath this row
						g.DrawLine( linePen, 0, y, BITMAP_WIDTH, y );
					}
				}

				// draw the character
				g.DrawString( c.ToString(), font, System.Drawing.Brushes.White, x - 3, y );

				// measure the width and height of the character
				System.Drawing.SizeF metrics = g.MeasureString( c.ToString(), font );

				// calculate the texture coords for the character
				// note: flip the y coords by subtracting from 1
				float u1 = (float)x / (float)BITMAP_WIDTH;
				float v1 = (float)y / (float)BITMAP_HEIGHT;

				float u2 = (float)( x + ( metrics.Width - 4 ) ) / (float)BITMAP_WIDTH;
				float v2 = (float)( y + metrics.Height ) / (float)BITMAP_HEIGHT;
				SetGlyphTexCoords( c, u1, v1, u2, v2 );

				// increment X by the width of the current char
				x += (int)metrics.Width - 3;

				// keep track of the tallest character on this line
				if ( maxHeight < (int)metrics.Height )
					maxHeight = (int)metrics.Height;

				if ( showLines )
				{
					// draw a vertical line after this char
					g.DrawLine( linePen, x, y, x, y + font.Height );
				}
			}  // for

			if ( showLines )
			{
				// draw the last horizontal line
				g.DrawLine( linePen, 0, y + font.Height, BITMAP_WIDTH, y + font.Height );
			}
			
			SDI.BitmapData bmd = bitmap.LockBits( new System.Drawing.Rectangle( 0, 0, BITMAP_WIDTH, BITMAP_HEIGHT ), SDI.ImageLockMode.ReadOnly, SDI.PixelFormat.Format32bppArgb );

			byte[] imgData = new byte[ PixelUtil.GetNumElemBytes( PixelFormat.A8R8G8B8 ) * BITMAP_WIDTH * BITMAP_HEIGHT ];

			GCHandle hBuff = GCHandle.Alloc( imgData, GCHandleType.Pinned );

			Memory.Copy( bmd.Scan0, hBuff.AddrOfPinnedObject(), imgData.Length );

			hBuff.Free();

			Image img = new Image();
			img.FromDynamicImage( imgData, BITMAP_WIDTH, BITMAP_HEIGHT, PixelFormat.A8R8G8B8 );

			Texture tex = (Texture)resource;

			tex.LoadImages( new Image[] { img } );

			//bitmap.Save( "C:\\" + Name + ".png" );
			//FileStream file = new FileStream( "C:\\" + Name + ".fontdef", FileMode.Create );
			//StreamWriter str = new StreamWriter( file );
			//str.WriteLine( Name );
			//str.WriteLine( "{" );
			//str.WriteLine( "\ttype\timage" );
			//str.WriteLine( "\tsource\t{0}.png\n", Name );

			//for ( int i = 0; i < END_CHAR - START_CHAR; i++ )
			//{
			//    char c = (char)( i + START_CHAR );
			//    str.WriteLine( "\tglyph\t{0}\t{1:F6}\t{2:F6}\t{3:F6}\t{4:F6}", c, texCoordU1[ i ], texCoordV1[ i ], texCoordU2[ i ], texCoordV2[ i ] );
			//}
			//str.WriteLine( "}" );
			//str.Close();
			//file.Close();
		}

		#endregion Implementation of IManualResourceLoader
	}
}
