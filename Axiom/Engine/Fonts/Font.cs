#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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
using System.Drawing;
using System.Text;
using Axiom.Core;
using Axiom.Enumerations;
using Axiom.SubSystems.Rendering;

namespace Axiom.Fonts
{
	/// <summary>
	///		This class is simply a way of getting a font texture into the engine and
	///		to easily retrieve the texture coordinates required to accurately render them.
	///		Fonts can either be loaded from precreated textures, or the texture can be generated
	///		using a truetype font. You can either create the texture manually in code, or you
	///		can use an XML font script to define it (probably more practical since you can reuse
	///		the definition more easily)
	/// </summary>
	public class Font : Axiom.Core.Resource
	{
		#region Constants

		const int BITMAP_HEIGHT = 512;
		const int BITMAP_WIDTH = 512;
		const int START_CHAR = 33;
		const int END_CHAR = 256;
		const bool SHOW_LINES = false;

		#endregion

		#region Member variables

		/// <summary></summary>
		protected FontType fontType;
		/// <summary></summary>
		protected String source;

		// arrays for storing texture and display data for each character
		protected float[] texCoordU1 = new float[END_CHAR - START_CHAR];
		protected float[] texCoordU2 = new float[END_CHAR - START_CHAR];
		protected float[] texCoordV1 = new float[END_CHAR - START_CHAR];
		protected float[] texCoordV2 = new float[END_CHAR - START_CHAR];
		protected float[] aspectRatio = new float[END_CHAR - START_CHAR];

		/// <summary>Material create for use on entities by this font.</summary>
		protected Material material;

		#endregion

		/// <summary>
		///		Constructor, should be called through FontManager.Create.
		/// </summary>
		public Font(String name)
		{
			this.name = name;
		}

		#region Implementation of Resource

		public override void Load()
		{
			// dont load more than once
			if(!isLoaded)
			{
				// create a material for this font
				material = (Material)MaterialManager.Instance.Create("Fonts." + name);

				TextureLayer layer = null;
				bool blendByAlpha = false;

				if(fontType == FontType.TrueType)
				{
					// create the font bitmap on the fly
					CreateTexture();

					// a texture layer was added in CreateTexture
					layer = material.TextureLayers[0];

					material.Lighting = false;
				}
				else
				{
					// TODO: Manually created fonts
				}

				// set texture addressing mode to Clamp to eliminate fuzzy edges
				layer.TextureAddressing = TextureAddressing.Clamp;

				// set up blending mode
				if(blendByAlpha)
					material.SetSceneBlending(SceneBlendType.TransparentAlpha);
				else
					material.SetSceneBlending(SceneBlendType.Add);

				isLoaded = true;
			}
		}

		protected void CreateTexture()
		{
			// create a new bitamp with the size defined
			Bitmap bitmap = new Bitmap(BITMAP_WIDTH, BITMAP_HEIGHT, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

			// get a handles to the graphics context of the bitmap
			Graphics g = Graphics.FromImage(bitmap);

			// get a font object for the specified font
			System.Drawing.Font font = new System.Drawing.Font(name, 18);
			
			// create a pen for the grid lines
			Pen linePen = new Pen(Color.Red);

			// clear the image to transparent
			g.Clear(Color.Transparent);

			// nice smooth text
			//g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
			//g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

			// used for calculating position in the image for rendering the characters
			int x, y, maxHeight;
			x = y = maxHeight = 0;

			// loop through each character in the glyph string and draw it to the bitmap
			for(int i = START_CHAR; i < END_CHAR; i++)
			{
				char c = (char)i;

				// are we gonna wrap?
				if(x + font.Size > BITMAP_WIDTH - 5)
				{
					// increment the y coord and reset x to move to the beginning of next line
					y += maxHeight;
					x = 0;
					maxHeight = 0;

					if(SHOW_LINES)
					{
						// draw a horizontal line underneath this row
						g.DrawLine(linePen, 0, y, BITMAP_WIDTH, y);
					}
				}

				// draw the character
				g.DrawString(c.ToString(), font, Brushes.White, x, y);

				// measure the width and height of the character
				SizeF metrics = g.MeasureString(c.ToString(), font);

				// calculate the texture coords for the character
				// note: flip the y coords by subtracting from 1
				texCoordU1[i - START_CHAR] = (float)x / (float)BITMAP_WIDTH;
				texCoordU2[i - START_CHAR] = ((float)x  + metrics.Width) / (float)BITMAP_WIDTH;
				texCoordV1[i - START_CHAR] = 1 - ((float)y / (float)BITMAP_HEIGHT);
				texCoordV2[i - START_CHAR] = 1 - (((float)y + metrics.Height) / (float)BITMAP_HEIGHT);
				aspectRatio[i - START_CHAR] = metrics.Width / metrics.Height;

				// increment X by the width of the current char
				x += (int)metrics.Width;

				// keep track of the tallest character on this line
				if(maxHeight < (int)metrics.Height)
					maxHeight = (int)metrics.Height;

				if(SHOW_LINES)
				{
					// draw a vertical line after this char
					g.DrawLine(linePen, x, y, x, y + font.Height);
				}
			}  // for

			if(SHOW_LINES)
			{
				// draw the last horizontal line
				g.DrawLine(linePen, 0, y + font.Height, BITMAP_WIDTH, y + font.Height);
			}

			String textureName = name + "FontTexture";

			// load the created image using the texture manager
			TextureManager.Instance.LoadImage(textureName, bitmap);

			// add a texture layer with the name of the texture
			material.AddTextureLayer(textureName);
		}

		/// <summary>
		///		Retreives the texture coordinates for the specifed character in this font.
		/// </summary>
		/// <param name="c"></param>
		/// <param name="u1"></param>
		/// <param name="u2"></param>
		/// <param name="v1"></param>
		/// <param name="v2"></param>
		public void GetCharTexCoords(char c, out float u1, out float u2, out float v1, out float v2)
		{
			int idx = (int)c - START_CHAR;
			u1 = texCoordU1[idx];
			u2 = texCoordU2[idx];
			v1 = texCoordV1[idx];
			v2 = texCoordV2[idx];
		}

		/// <summary>
		///		Finds the aspect ratio of the specified character in this font.
		/// </summary>
		/// <param name="c"></param>
		/// <returns></returns>
		public float GetCharAspectRatio(char c)
		{
			int idx = (int)c - START_CHAR;

			return aspectRatio[idx];
		}

		#endregion
	}
}
