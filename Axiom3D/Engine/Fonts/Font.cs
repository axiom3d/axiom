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
using System.Drawing;
using DotNet3D.Math.Collections;

using ResourceHandle = System.UInt64;

#endregion Namespace Declarations
			
namespace Axiom
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

        // arrays for storing texture and display data for each character
        #region texCoordU1 Property

        private float[] _texCoordU1 = new float[ END_CHAR - START_CHAR ];
        protected float[] texCoordU1
        {
            get
            {
                return _texCoordU1;
            }
            set
            {
                _texCoordU1 = value;
            }
        }

        #endregion texCoordU1 Property
			
        #region texCoordU2 Property

        private float[] _texCoordU2 = new float[ END_CHAR - START_CHAR ];
        protected float[] texCoordU2
        {
            get
            {
                return _texCoordU2;
            }
            set
            {
                _texCoordU2 = value;
            }
        }

        #endregion texCoordU2 Property
			
        #region texCoordV2 Property

        private float[] _texCoordV1 = new float[ END_CHAR - START_CHAR ];
        protected float[] texCoordV1
        {
            get
            {
                return _texCoordV1;
            }
            set
            {
                _texCoordV1 = value;
            }
        }

        #endregion texCoordV2 Property
			
        #region texCoordV2 Property

        private float[] _texCoordV2 = new float[ END_CHAR - START_CHAR ];
        protected float[] texCoordV2
        {
            get
            {
                return _texCoordV2;
            }
            set
            {
                _texCoordV2 = value;
            }
        }

        #endregion texCoordV2 Property
			
        #region aspectRatio Property

        private float[] _aspectRatio = new float[ END_CHAR - START_CHAR ];
        protected float[] aspectRatio
        {
            get
            {
                return _aspectRatio;
            }
            set
            {
                _aspectRatio = value;
            }
        }

        #endregion aspectRatio Property
			
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
			
        #endregion

        #region Constructor

        /// <summary>
        ///		Constructor, should be called through FontManager.Create.
        /// </summary>
        public Font( ResourceManager parent, string name, ResourceHandle handle, string group )
            : this( parent, name, handle, group, false, null )
        {
        }

        public Font( ResourceManager parent, string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader )
            : base( parent, name, handle, group, isManual, loader )
        {
        }

        ~Font()
        {
            Unload();
        }

        #endregion Constructor

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
                Texture texture = TextureManager.Instance.Load( Source, Group, TextureType.TwoD, null );

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
            _texture.Unload();
        }

        protected override int calculateSize()
        {
            // permanent resource is in the texture 
            return 0;
        }

        #endregion Implementation of Resource

        #region Methods

        protected void createTexture()
        {
		    // Just create the texture here, and point it at ourselves for when
		    // it wants to (re)load for real
		    String texName = Name + "FontTexture";
		    // Create, setting isManual to true and passing self as loader
		    texture = TextureManager.Instance.Create( texName, Group, true, this);
            texture.TextureType = TextureType.TwoD;
            texture.NumMipMaps = 0;
            texture.Load();

		    TextureUnitState t = Material.GetTechnique(0).GetPass(0).CreateTextureUnitState( texName );
		    // Allow min/mag filter, but no mip
		    t.SetTextureFiltering( FilterOptions.Linear, FilterOptions.Linear, FilterOptions.None );
        }

        /// <summary>Returns the size in pixels of a box that could contain the whole string.</summary>
        Pair<int> StrBBox( string text, float char_height, RenderWindow window )
        {
            Pair< int > ret = new Pair<int>( 0, 0 );
            float vsX, vsY, veX, veY;
            int w, h;

            w = window.Width;
            h = window.Height;

            for( int i = 0; i < text.Length; i++ )
            {
                GetGlyphTexCoords( text[ i ], out vsX, out vsY, out veX, out veY );

                // Calculate view-space width and height of char
                vsY = char_height;
                vsX = GetGlyphAspectRatio( text[ i ] ) * char_height;

                ret.second += (int)(vsX * w);
                if ( vsY * h > ret.first || ( ( i == 0 ) && text[ i - 1 ] == '\n' ) )
                    ret.first += (int)( vsY * h );
            }

            return ret;
        }

        /// <summary>
        ///		Retreives the texture coordinates for the specifed character in this font.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="u1"></param>
        /// <param name="u2"></param>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        public void GetGlyphTexCoords( char c, out float u1, out float u2, out float v1, out float v2 )
        {
            int idx = c - START_CHAR;
            u1 = texCoordU1[idx];
            u2 = texCoordU2[idx];
            v1 = texCoordV1[idx];
            v2 = texCoordV2[idx];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="c"></param>
        /// <param name="u1"></param>
        /// <param name="v1"></param>
        /// <param name="u2"></param>
        /// <param name="v2"></param>
        public void SetGlyphTexCoords( char c, float u1, float v1, float u2, float v2 )
        {
            int idx = c - START_CHAR;
            texCoordU1[idx] = u1;
            texCoordU2[idx] = v1;
            texCoordV1[idx] = u2;
            texCoordV2[idx] = v2;
            aspectRatio[idx] = ( u2 - u1 ) / ( v2 - v1 );
        }

        /// <summary>
        ///		Finds the aspect ratio of the specified character in this font.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public float GetGlyphAspectRatio( char c )
        {
            int idx = c - START_CHAR;

            return aspectRatio[idx];
        }


        #endregion Methods

        #region IManualResourceLoader Members

        public void LoadResource( Resource resource )
        {
            // TODO Revisit after checking current Imaging support in Mono.

            // create a new bitamp with the size defined
            Bitmap bitmap = new Bitmap( BITMAP_WIDTH, BITMAP_HEIGHT, System.Drawing.Imaging.PixelFormat.Format24bppRgb );

            // get a handles to the graphics context of the bitmap
            System.Drawing.Graphics g = System.Drawing.Graphics.FromImage( bitmap );

            // get a font object for the specified font
            System.Drawing.Font font = new System.Drawing.Font( Name, 18 );

            // create a pen for the grid lines
            Pen linePen = new Pen( Color.Red );

            // clear the image to transparent
            g.Clear( Color.Transparent );

            // nice smooth text
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

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
                g.DrawString( c.ToString(), font, Brushes.White, x - 3, y );

                // measure the width and height of the character
                SizeF metrics = g.MeasureString( c.ToString(), font );

                // calculate the texture coords for the character
                // note: flip the y coords by subtracting from 1
                float u1 = (float)x / (float)BITMAP_WIDTH;
                float u2 = ( (float)x + metrics.Width - 4 ) / (float)BITMAP_WIDTH;
                float v1 = 1 - ( (float)y / (float)BITMAP_HEIGHT );
                float v2 = 1 - ( ( (float)y + metrics.Height ) / (float)BITMAP_HEIGHT );
                SetGlyphTexCoords( c, u1, u2, v1, v2 );

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

        }

        #endregion
    }
}
