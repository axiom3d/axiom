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
using System.Runtime.InteropServices;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;
using Axiom.Media;
using CodePoint = System.UInt32;
using Image = Axiom.Media.Image;
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

        #region MaxBearingY
        /// <summary>
        ///  Max distance to baseline of this (truetype) font
        /// </summary>
        private int maxBearingY = 0;
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

        private bool _showLines;
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
                    height += (int)( vsY * h );
            }

            return new Pair<int>( height, width );
        }

        /// <summary>
        ///		Retrieves the texture coordinates for the specified character in this font.
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
            GlyphInfo glyph = new GlyphInfo( c, new UVRect( v1, u1, v2, u2 ), aspect * ( u2 - u1 ) / ( v2 - v1 ) );
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
        [Obsolete( "Use Glyphs property" )]
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
            // clarabie - nov 18, 2008
            // modified this to check for an existing material instead of always
            // creating a new one. Allows more flexibility, but also specifically allows us to
            // solve the problem of XNA not having fixed function support

            _material = (Material)MaterialManager.Instance.GetByName( "Fonts/" + _name );

            if ( _material == null )
            {

                // create a material for this font
                _material = (Material)MaterialManager.Instance.Create( "Fonts/" + _name, Group );

                TextureUnitState unitState = null;
                bool blendByAlpha = false;

                if ( _fontType == FontType.TrueType )
                {
#if !( XBOX || XBOX360 )
                    // create the font bitmap on the fly
                    createTexture();

                    // a texture layer was added in CreateTexture
                    unitState = _material.GetTechnique( 0 ).GetPass( 0 ).GetTextureUnitState( 0 );

                    blendByAlpha = true;
#endif
                }
                else
                {
                    // load this texture
                    // TODO In general, modify any methods like this that throw their own exception rather than returning null, so the caller can decide how to handle a missing resource.
                    _texture = TextureManager.Instance.Load( Source, Group, TextureType.TwoD, 0 );

                    blendByAlpha = texture.HasAlpha;
                    // pre-created font images
                    unitState = Material.GetTechnique( 0 ).GetPass( 0 ).CreateTextureUnitState( Source );
                }

                // Make sure material is aware of colour per vertex.
                _material.GetTechnique( 0 ).GetPass( 0 ).VertexColorTracking = TrackVertexColor.Diffuse;

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
                    _material.SetSceneBlending( SceneBlendType.TransparentAlpha );
                }
                else
                {
                    // assume black background here
                    _material.SetSceneBlending( SceneBlendType.Add );
                }
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

#if !( XBOX || XBOX360 || ANDROID || IPHONE)
            string current = Environment.CurrentDirectory;

            IntPtr ftLibrary = IntPtr.Zero;
            if ( FT.FT_Init_FreeType( out ftLibrary ) != 0 )
                throw new AxiomException( "Could not init FreeType library!" );

            IntPtr face = IntPtr.Zero;
            // Add a gap between letters vert and horz
            // prevents nasty artefacts when letters are too close together
            int char_space = 5;

            // Locate ttf file, load it pre-buffered into memory by wrapping the
            // original DataStream in a MemoryDataStream
            Stream fileStream = ResourceGroupManager.Instance.OpenResource( Source, Group, true, this );

            byte[] data = new byte[ fileStream.Length ];
            fileStream.Read( data, 0, data.Length );
            //Load font
            int success = FT.FT_New_Memory_Face( ftLibrary, data, data.Length, 0, out face );
            if ( success != 0 )
            {
                throw new AxiomException( "Could not open font face!" );
            }

            // Convert our point size to freetype 26.6 fixed point format
            int ttfSize = _ttfSize * ( 1 << 6 );

            success = FT.FT_Set_Char_Size( face, ttfSize, 0, (uint)_ttfResolution, (uint)_ttfResolution );
            if ( success != 0 )
            {
                {
                    throw new AxiomException( "Could not set char size!" );
                }
            }
            int max_height = 0, max_width = 0;
            List<KeyValuePair<int, int>> codePointRange = new List<KeyValuePair<int, int>>();
            // Backwards compatibility - if codepoints not supplied, assume 33-166
            if ( codePointRange.Count == 0 )
            {
                codePointRange.Add( new KeyValuePair<int, int>( 33, 166 ) );
            }

            // Calculate maximum width, height and bearing
            int glyphCount = 0;
            foreach ( KeyValuePair<int, int> r in codePointRange )
            {
                KeyValuePair<int, int> range = r;
                for ( int cp = range.Key; cp <= range.Value; ++cp, ++glyphCount )
                {
                    FT.FT_Load_Char( face, (uint)cp, 4 ); //4 == FT_LOAD_RENDER
                    FT_FaceRec rec = (FT_FaceRec)Marshal.PtrToStructure( face, typeof( FT_FaceRec ) );
                    FT_GlyphSlotRec glyp = (FT_GlyphSlotRec)Marshal.PtrToStructure( rec.glyph, typeof( FT_GlyphSlotRec ) );
                    if ( ( 2 * ( glyp.bitmap.rows << 6 ) - glyp.metrics.horiBearingY ) > max_height )
                        max_height = ( 2 * ( glyp.bitmap.rows << 6 ) - glyp.metrics.horiBearingY );
                    if ( glyp.metrics.horiBearingY > maxBearingY )
                        maxBearingY = glyp.metrics.horiBearingY;

                    if ( ( glyp.advance.x >> 6 ) + ( glyp.metrics.horiBearingX >> 6 ) > max_width )
                        max_width = ( glyp.advance.x >> 6 ) + ( glyp.metrics.horiBearingX >> 6 );

                }
            }

            // Now work out how big our texture needs to be
            int rawSize = ( max_width + char_space ) *
                ( ( max_height >> 6 ) + char_space ) * glyphCount;

            int tex_side = (int)System.Math.Sqrt( (Real)rawSize );

            // just in case the size might chop a glyph in half, add another glyph width/height
            tex_side += System.Math.Max( max_width, ( max_height >> 6 ) );
            // Now round up to nearest power of two
            int roundUpSize = (int)Bitwise.FirstPO2From( (uint)tex_side );
            // Would we benefit from using a non-square texture (2X width(
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

            Real textureAspec = (Real)finalWidth / (Real)finalHeight;
            int pixelBytes = 2;
            int dataWidth = finalWidth * pixelBytes;
            int data_size = finalWidth * finalHeight * pixelBytes;

            LogManager.Instance.Write( "Font " + _name + " using texture size " + finalWidth.ToString() + "x" + finalHeight.ToString() );

            byte[] imageData = new byte[ data_size ];
            for ( int i = 0; i < data_size; i += pixelBytes )
            {
                imageData[ i + 0 ] = 0xff;// luminance
                imageData[ i + 1 ] = 0x00;// alpha
            }


            int l = 0, m = 0;
            foreach ( KeyValuePair<int, int> r in codePointRange )
            {
                KeyValuePair<int, int> range = r;
                for ( int cp = range.Key; cp <= range.Value; ++cp )
                {
                    int result = FT.FT_Load_Char( face, (uint)cp, 4 );//4 == FT_LOAD_RENDER
                    if ( result != 0 )
                    {
                        // problem loading this glyph, continue
                        LogManager.Instance.Write( "Info: cannot load character '" + char.ConvertFromUtf32( cp ) + "' in font " + _name + "." );
                        continue;
                    }

                    FT_FaceRec rec = (FT_FaceRec)Marshal.PtrToStructure( face, typeof( FT_FaceRec ) );
                    FT_GlyphSlotRec glyp = (FT_GlyphSlotRec)Marshal.PtrToStructure( rec.glyph, typeof( FT_GlyphSlotRec ) );
                    int advance = glyp.advance.x >> 6;
                    unsafe
                    {
                        if ( glyp.bitmap.buffer == IntPtr.Zero )
                        {
                            LogManager.Instance.Write( "Info: Freetype returned null for character '" + char.ConvertFromUtf32( cp ) + "' in font " + _name + "." );
                            continue;
                        }
                        byte* buffer = (byte*)glyp.bitmap.buffer;
                        byte* imageDataPtr = (byte*)Memory.PinObject( imageData );
                        int y_bearing = ( ( maxBearingY >> 6 ) - ( glyp.metrics.horiBearingY >> 6 ) );
                        int x_bearing = glyp.metrics.horiBearingX >> 6;

                        for ( int j = 0; j < glyp.bitmap.rows; j++ )
                        {
                            int row = j + m + y_bearing;
                            byte* pDest = &imageDataPtr[ ( row * dataWidth ) + ( l + x_bearing ) * pixelBytes ];
                            for ( int k = 0; k < glyp.bitmap.width; k++ )
                            {
                                if ( AntialiasColor )
                                {
                                    // Use the same greyscale pixel for all components RGBA
                                    *pDest++ = *buffer;
                                }
                                else
                                {
                                    // Always white whether 'on' or 'off' pixel, since alpha
                                    // will turn off
                                    *pDest++ = (byte)0xFF;
                                }
                                // Always use the greyscale value for alpha
                                *pDest++ = *buffer++;
                            }//end k
                        }//end j
                        //
                        this.SetGlyphTexCoords( (uint)cp, (Real)l / (Real)finalWidth,//u1
                            (Real)m / (Real)finalHeight,//v1
                            (Real)( l + ( glyp.advance.x >> 6 ) ) / (Real)finalWidth, //u2
                            ( m + ( max_height >> 6 ) ) / (Real)finalHeight, textureAspec ); //v2
                        //    textureAspec );
                        //SetGlyphTexCoords( c, u1, v1, u2, v2 );
                        //Glyphs.Add( new KeyValuePair<CodePoint, GlyphInfo>( (uint)cp,
                        //    new GlyphInfo( (uint)cp,
                        //        new UVRect(
                        //            (Real)l / (Real)finalWidth,//u1
                        //    (Real)m / (Real)finalHeight,//v1
                        //    (Real)( l + ( glyp.advance.x >> 6 ) ) / (Real)finalWidth, //u2
                        //    ( m + ( max_height >> 6 ) ) / (Real)finalHeight //v2
                        //    ), textureAspec ) ) );

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
            }//end foreach

            MemoryStream memStream = new MemoryStream( imageData );
            Image img = Image.FromRawStream( memStream, finalWidth, finalHeight, PixelFormat.BYTE_LA );

            Texture tex = (Texture)resource;
            Image[] images = new Image[ 1 ];
            images[ 0 ] = img;
            tex.LoadImages( images );
            FT.FT_Done_FreeType( ftLibrary );

            //img.Save( "C:\\" + Name + ".png" );
            //FileStream file = new FileStream( "C:\\" + Name + ".fontdef", FileMode.Create );
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