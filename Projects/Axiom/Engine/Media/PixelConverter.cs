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
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.IO;
using System.Diagnostics;

using Axiom.Core;
using Axiom.CrossPlatform;

#endregion Namespace Declarations

namespace Axiom.Media
{

	///<summary>
	///    A class to convert/copy pixels of the same or different formats
	///</summary>
	public class PixelConverter
	{

		///<summary>
		/// Description of Pixel Formats.
		///</summary>
		public class PixelFormatDescription
		{

			#region Fields

			// Name of the format, as in the enum
			public string name;
			// The pixel format
			public PixelFormat format;
			// Number of bytes one element (color value) takes.
			public byte elemBytes;
			// Pixel format flags, see enum PixelFormatFlags for the bit field
			// definitions 
			public PixelFormatFlags flags;
			// Component type 
			public PixelComponentType componentType;
			// Component count
			public byte componentCount;
			// Number of bits for red(or luminance), green, blue, alpha
			public byte rbits, gbits, bbits, abits; /*, ibits, dbits, ... */
			// Masks and shifts as used by packers/unpackers */
			public uint rmask, gmask, bmask, amask;
			public byte rshift, gshift, bshift, ashift;

			#endregion Fields

			#region Constructor

			public PixelFormatDescription( string name,
										  PixelFormat format,
										  byte elemBytes,
										  PixelFormatFlags flags,
										  PixelComponentType componentType,
										  byte componentCount,
										  byte rbits,
										  byte gbits,
										  byte bbits,
										  byte abits,
										  uint rmask,
										  uint gmask,
										  uint bmask,
										  uint amask,
										  byte rshift,
										  byte gshift,
										  byte bshift,
										  byte ashift )
			{
				this.name = name;
				this.format = format;
				this.elemBytes = elemBytes;
				this.flags = flags;
				this.componentType = componentType;
				this.componentCount = componentCount;
				this.rbits = rbits;
				this.gbits = gbits;
				this.bbits = bbits;
				this.abits = abits;
				this.rmask = rmask;
				this.gmask = gmask;
				this.bmask = bmask;
				this.amask = amask;
				this.rshift = rshift;
				this.gshift = gshift;
				this.bshift = bshift;
                this.ashift = ashift;
			}

			#endregion Constructor
		}


		///<summary>
		///    Pixel format database
		///</summary>
		protected static PixelFormatDescription[] UnindexedPixelFormats = new PixelFormatDescription[] {
			new PixelFormatDescription(
				"PF_UNKNOWN", 
				PixelFormat.Unknown,
				/* Bytes per element */ 
				0,  
				/* Flags */
				PixelFormatFlags.None,  
				/* Component type and count */
				PixelComponentType.Byte, 0,
				/* rbits, gbits, bbits, abits */
				0, 0, 0, 0,
				/* Masks and shifts */
				0, 0, 0, 0, 0, 0, 0, 0 
				),
			//-----------------------------------------------------------------------
			new PixelFormatDescription(
				"PF_L8",
				PixelFormat.L8,
				/* Bytes per element */ 
				1,  
				/* Flags */
				PixelFormatFlags.Luminance | PixelFormatFlags.NativeEndian,
				/* Component type and count */
				PixelComponentType.Byte, 1,
				/* rbits, gbits, bbits, abits */
				8, 0, 0, 0,
				/* Masks and shifts */
				0xFF, 0, 0, 0, 0, 0, 0, 0 
				),
			//-----------------------------------------------------------------------
			new PixelFormatDescription(
				"PF_L16",
				PixelFormat.L16,
				/* Bytes per element */ 
				2,  
				/* Flags */
				PixelFormatFlags.Luminance | PixelFormatFlags.NativeEndian,  
				/* Component type and count */
				PixelComponentType.Short, 1,
				/* rbits, gbits, bbits, abits */
				16, 0, 0, 0,
				/* Masks and shifts */
				0xFFFF, 0, 0, 0, 0, 0, 0, 0 
				),
			//-----------------------------------------------------------------------
			new PixelFormatDescription(
				"PF_A8",
				PixelFormat.A8,
				/* Bytes per element */ 
				1,  
				/* Flags */
				PixelFormatFlags.HasAlpha | PixelFormatFlags.NativeEndian,
				/* Component type and count */
				PixelComponentType.Byte, 1,
				/* rbits, gbits, bbits, abits */
				0, 0, 0, 8,
				/* Masks and shifts */
				0, 0, 0, 0xFF, 0, 0, 0, 0 
				),
			//-----------------------------------------------------------------------
			new PixelFormatDescription(
				"PF_A4L4",
				PixelFormat.A4L4,
				/* Bytes per element */ 
				1,  
				/* Flags */
				PixelFormatFlags.HasAlpha | PixelFormatFlags.Luminance | PixelFormatFlags.NativeEndian,
				/* Component type and count */
				PixelComponentType.Byte, 2,
				/* rbits, gbits, bbits, abits */
				4, 0, 0, 4,
				/* Masks and shifts */
				0x0F, 0, 0, 0xF0, 0, 0, 0, 4
				),
			//-----------------------------------------------------------------------
			new PixelFormatDescription(
				 "PF_BYTE_LA",
				 PixelFormat.A8L8,
				/* Bytes per element */ 
				2,  
				/* Flags */
				PixelFormatFlags.HasAlpha | PixelFormatFlags.Luminance,  
				/* Component type and count */
				PixelComponentType.Byte, 2,
				/* rbits, gbits, bbits, abits */
				8, 0, 0, 8,
				/* Masks and shifts */
				0,0,0,0,0,0,0,0
				),
			//-----------------------------------------------------------------------
			new PixelFormatDescription(
				"PF_R5G6B5",
				PixelFormat.R5G6B5,
				/* Bytes per element */ 
				2,  
				/* Flags */
				PixelFormatFlags.NativeEndian,  
				/* Component type and count */
				PixelComponentType.Byte, 3,
				/* rbits, gbits, bbits, abits */
				5, 6, 5, 0,
				/* Masks and shifts */
				0xF800, 0x07E0, 0x001F, 0, 
				11, 5, 0, 0 
				),
			//-----------------------------------------------------------------------
			new PixelFormatDescription(
				"PF_B5G6R5",
				PixelFormat.B5G6R5,
				/* Bytes per element */ 
				2,  
				/* Flags */
				PixelFormatFlags.NativeEndian,  
				/* Component type and count */
				PixelComponentType.Byte, 3,
				/* rbits, gbits, bbits, abits */
				5, 6, 5, 0,
				/* Masks and shifts */
				0x001F, 0x07E0, 0xF800, 0, 
				0, 5, 11, 0 
				),
			//-----------------------------------------------------------------------
			new PixelFormatDescription(
				"PF_A4R4G4B4",
				PixelFormat.A4R4G4B4,
				/* Bytes per element */ 
				2,  
				/* Flags */
				PixelFormatFlags.HasAlpha | PixelFormatFlags.NativeEndian,  
				/* Component type and count */
				PixelComponentType.Byte, 4,
				/* rbits, gbits, bbits, abits */
				4, 4, 4, 4,
				/* Masks and shifts */
				0x0F00, 0x00F0, 0x000F, 0xF000, 
				8, 4, 0, 12 
				),
			//-----------------------------------------------------------------------
			new PixelFormatDescription(
				 "PF_A1R5G5B5",
				 PixelFormat.A1R5G5B5,
				/* Bytes per element */ 
				2,  
				/* Flags */
				PixelFormatFlags.HasAlpha | PixelFormatFlags.NativeEndian,  
				/* Component type and count */
				PixelComponentType.Byte, 4,
				/* rbits, gbits, bbits, abits */
				5, 5, 5, 1,
				/* Masks and shifts */
				0x7C00, 0x03E0, 0x001F, 0x8000, 
				10, 5, 0, 15
				),
			//-----------------------------------------------------------------------
			new PixelFormatDescription(
				"PF_R8G8B8",
				PixelFormat.R8G8B8,
				/* Bytes per element */ 
				3,  // 24 bit integer -- special
				/* Flags */
				PixelFormatFlags.NativeEndian,  
				/* Component type and count */
				PixelComponentType.Byte, 3,
				/* rbits, gbits, bbits, abits */
				8, 8, 8, 0,
				/* Masks and shifts */
				0xFF0000, 0x00FF00, 0x0000FF, 0, 
				16, 8, 0, 0 
				),
			//-----------------------------------------------------------------------
			new PixelFormatDescription(
				"PF_B8G8R8",
				PixelFormat.B8G8R8,
				/* Bytes per element */ 
				3,  // 24 bit integer -- special
				/* Flags */
				PixelFormatFlags.NativeEndian,  
				/* Component type and count */
				PixelComponentType.Byte, 3,
				/* rbits, gbits, bbits, abits */
				8, 8, 8, 0,
				/* Masks and shifts */
				0x0000FF, 0x00FF00, 0xFF0000, 0, 
				0, 8, 16, 0 
				),
			//-----------------------------------------------------------------------
			new PixelFormatDescription(
				"PF_A8R8G8B8",
				PixelFormat.A8R8G8B8,
				/* Bytes per element */ 
				4,  
				/* Flags */
				PixelFormatFlags.HasAlpha | PixelFormatFlags.NativeEndian,  
				/* Component type and count */
				PixelComponentType.Byte, 4,
				/* rbits, gbits, bbits, abits */
				8, 8, 8, 8,
				/* Masks and shifts */
				0x00FF0000, 0x0000FF00, 0x000000FF, 0xFF000000,
				16, 8, 0, 24
				),
			//-----------------------------------------------------------------------
			new PixelFormatDescription(
				 "PF_A8B8G8R8",
				 PixelFormat.A8B8G8R8,
				/* Bytes per element */ 
				4,  
				/* Flags */
				PixelFormatFlags.HasAlpha | PixelFormatFlags.NativeEndian,  
				/* Component type and count */
				PixelComponentType.Byte, 4,
				/* rbits, gbits, bbits, abits */
				8, 8, 8, 8,
				/* Masks and shifts */
				0x000000FF, 0x0000FF00, 0x00FF0000, 0xFF000000,
				0, 8, 16, 24
				),
			//-----------------------------------------------------------------------
			new PixelFormatDescription(
				"PF_B8G8R8A8",
				PixelFormat.B8G8R8A8,
				/* Bytes per element */ 
				4,  
				/* Flags */
				PixelFormatFlags.HasAlpha | PixelFormatFlags.NativeEndian,  
				/* Component type and count */
				PixelComponentType.Byte, 4,
				/* rbits, gbits, bbits, abits */
				8, 8, 8, 8,
				/* Masks and shifts */
				0x0000FF00, 0x00FF0000, 0xFF000000, 0x000000FF,
				8, 16, 24, 0
				),
			//-----------------------------------------------------------------------
			new PixelFormatDescription(
				 "PF_A2R10G10B10",
				 PixelFormat.A2R10G10B10,
				/* Bytes per element */ 
				4,  
				/* Flags */
				PixelFormatFlags.HasAlpha | PixelFormatFlags.NativeEndian,  
				/* Component type and count */
				PixelComponentType.Byte, 4,
				/* rbits, gbits, bbits, abits */
				10, 10, 10, 2,
				/* Masks and shifts */
				0x3FF00000, 0x000FFC00, 0x000003FF, 0xC0000000,
				20, 10, 0, 30
				),
			//-----------------------------------------------------------------------
			new PixelFormatDescription(
				 "PF_A2B10G10R10",
				 PixelFormat.A2B10G10R10,
				/* Bytes per element */ 
				4,  
				/* Flags */
				PixelFormatFlags.HasAlpha | PixelFormatFlags.NativeEndian,  
				/* Component type and count */
				PixelComponentType.Byte, 4,
				/* rbits, gbits, bbits, abits */
				10, 10, 10, 2,
				/* Masks and shifts */
				0x000003FF, 0x000FFC00, 0x3FF00000, 0xC0000000,
				0, 10, 20, 30
				),
			//-----------------------------------------------------------------------
			new PixelFormatDescription(
				"PF_DXT1",
				PixelFormat.DXT1,
				/* Bytes per element */ 
				0,  
				/* Flags */
				PixelFormatFlags.Compressed | PixelFormatFlags.HasAlpha,  
				/* Component type and count */
				PixelComponentType.Byte, 3, // No alpha
				/* rbits, gbits, bbits, abits */
				0, 0, 0, 0,
				/* Masks and shifts */
				0, 0, 0, 0, 0, 0, 0, 0 
				),
			//-----------------------------------------------------------------------
			new PixelFormatDescription(
				"PF_DXT2",
				PixelFormat.DXT2,
				/* Bytes per element */ 
				0,  
				/* Flags */
				PixelFormatFlags.Compressed | PixelFormatFlags.HasAlpha,  
				/* Component type and count */
				PixelComponentType.Byte, 4,
				/* rbits, gbits, bbits, abits */
				0, 0, 0, 0,
				/* Masks and shifts */
				0, 0, 0, 0, 0, 0, 0, 0 
				),
			//-----------------------------------------------------------------------
			new PixelFormatDescription(
				"PF_DXT3",
				PixelFormat.DXT3,
				/* Bytes per element */ 
				0,  
				/* Flags */
				PixelFormatFlags.Compressed | PixelFormatFlags.HasAlpha,  
				/* Component type and count */
				PixelComponentType.Byte, 4,
				/* rbits, gbits, bbits, abits */
				0, 0, 0, 0,
				/* Masks and shifts */
				0, 0, 0, 0, 0, 0, 0, 0 
				),
			//-----------------------------------------------------------------------
			new PixelFormatDescription(
				"PF_DXT4",
				PixelFormat.DXT4,
				/* Bytes per element */ 
				0,  
				/* Flags */
				PixelFormatFlags.Compressed | PixelFormatFlags.HasAlpha,  
				/* Component type and count */
				PixelComponentType.Byte, 4,
				/* rbits, gbits, bbits, abits */
				0, 0, 0, 0,
				/* Masks and shifts */
				0, 0, 0, 0, 0, 0, 0, 0 
				),
			//-----------------------------------------------------------------------
			new PixelFormatDescription(
				"PF_DXT5",
				PixelFormat.DXT5,
				/* Bytes per element */ 
				0,  
				/* Flags */
				PixelFormatFlags.Compressed | PixelFormatFlags.HasAlpha,  
				/* Component type and count */
				PixelComponentType.Byte, 4,
				/* rbits, gbits, bbits, abits */
				0, 0, 0, 0,
				/* Masks and shifts */
				0, 0, 0, 0, 0, 0, 0, 0 
				),
			//-----------------------------------------------------------------------
			new PixelFormatDescription(
				"PF_FLOAT16_RGB",
				PixelFormat.FLOAT16_RGB,
				/* Bytes per element */ 
				6,  
				/* Flags */
				PixelFormatFlags.Float,  
				/* Component type and count */
				PixelComponentType.Float16, 3,
				/* rbits, gbits, bbits, abits */
				16, 16, 16, 0,
				/* Masks and shifts */
				0, 0, 0, 0, 0, 0, 0, 0 
				),
			//-----------------------------------------------------------------------
			new PixelFormatDescription(
				"PF_FLOAT16_RGBA",
				PixelFormat.FLOAT16_RGBA,
				/* Bytes per element */ 
				8,  
				/* Flags */
				PixelFormatFlags.Float,  
				/* Component type and count */
				PixelComponentType.Float16, 4,
				/* rbits, gbits, bbits, abits */
				16, 16, 16, 16,
				/* Masks and shifts */
				0, 0, 0, 0, 0, 0, 0, 0 
				),
			//-----------------------------------------------------------------------
			new PixelFormatDescription(
				"PF_FLOAT32_RGB",
				PixelFormat.FLOAT32_RGB,
				/* Bytes per element */ 
				12,  
				/* Flags */
				PixelFormatFlags.Float,  
				/* Component type and count */
				PixelComponentType.Float32, 3,
				/* rbits, gbits, bbits, abits */
				32, 32, 32, 0,
				/* Masks and shifts */
				0, 0, 0, 0, 0, 0, 0, 0 
				),
			//-----------------------------------------------------------------------
			new PixelFormatDescription(
				"PF_FLOAT32_RGBA",
				PixelFormat.FLOAT32_RGBA,
				/* Bytes per element */ 
				16,  
				/* Flags */
				PixelFormatFlags.Float,  
				/* Component type and count */
				PixelComponentType.Float32, 4,
				/* rbits, gbits, bbits, abits */
				32, 32, 32, 32,
				/* Masks and shifts */
				0, 0, 0, 0, 0, 0, 0, 0 
				),
			//-----------------------------------------------------------------------
			new PixelFormatDescription(
				 "PF_X8R8G8B8",
				 PixelFormat.X8R8G8B8,
				/* Bytes per element */ 
				4,  
				/* Flags */
				PixelFormatFlags.NativeEndian,  
				/* Component type and count */
				PixelComponentType.Byte, 3,
				/* rbits, gbits, bbits, abits */
				8, 8, 8, 0,
				/* Masks and shifts */
				0x00FF0000, 0x0000FF00, 0x000000FF, 0xFF000000,
				16, 8, 0, 24
				),
			//-----------------------------------------------------------------------
			new PixelFormatDescription(
				 "PF_X8B8G8R8",
				 PixelFormat.X8B8G8R8,
				/* Bytes per element */ 
				4,  
				/* Flags */
				PixelFormatFlags.NativeEndian,  
				/* Component type and count */
				PixelComponentType.Byte, 3,
				/* rbits, gbits, bbits, abits */
				8, 8, 8, 0,
				/* Masks and shifts */
				0x000000FF, 0x0000FF00, 0x00FF0000, 0xFF000000,
				0, 8, 16, 24
				),
			//-----------------------------------------------------------------------
			new PixelFormatDescription(
				 "PF_R8G8B8A8",
				 PixelFormat.R8G8B8A8,
				/* Bytes per element */ 
				4,  
				/* Flags */
				PixelFormatFlags.HasAlpha | PixelFormatFlags.NativeEndian,  
				/* Component type and count */
				PixelComponentType.Byte, 4,
				/* rbits, gbits, bbits, abits */
				8, 8, 8, 8,
				/* Masks and shifts */
				0xFF000000, 0x00FF0000, 0x0000FF00, 0x000000FF,
				24, 16, 8, 0
				),
			//-----------------------------------------------------------------------
			new PixelFormatDescription(
				 "PF_DEPTH",
				 PixelFormat.DEPTH,
				/* Bytes per element */ 
				4,  
				/* Flags */
				PixelFormatFlags.Depth, 
				/* Component type and count */
				PixelComponentType.Float32, 1, // ?
				/* rbits, gbits, bbits, abits */
				0, 0, 0, 0,
				/* Masks and shifts */
				0, 0, 0, 0, 0, 0, 0, 0
				),
			//-----------------------------------------------------------------------
			new PixelFormatDescription(
				 "PF_SHORT_RGBA",
				 PixelFormat.SHORT_RGBA,
				/* Bytes per element */ 
				8,  
				/* Flags */
				PixelFormatFlags.HasAlpha,  
				/* Component type and count */
				PixelComponentType.Short, 4,
				/* rbits, gbits, bbits, abits */
				16, 16, 16, 16,
				/* Masks and shifts */
				0, 0, 0, 0, 0, 0, 0, 0
				),
			//-----------------------------------------------------------------------
			new PixelFormatDescription(
				 "PF_R3G3B2",
				 PixelFormat.R3G3B2,
				/* Bytes per element */ 
				1,  
				/* Flags */
				PixelFormatFlags.NativeEndian,  
				/* Component type and count */
				PixelComponentType.Byte, 3,
				/* rbits, gbits, bbits, abits */
				3, 3, 2, 0,
				/* Masks and shifts */
				0xE0, 0x1C, 0x03, 0, 
				5, 2, 0, 0 
				),
			//-----------------------------------------------------------------------
			new PixelFormatDescription(
				"PF_FLOAT16_R",
				PixelFormat.FLOAT16_R,
				/* Bytes per element */ 
				2,  
				/* Flags */
				PixelFormatFlags.Float,  
				/* Component type and count */
				PixelComponentType.Float16, 1,
				/* rbits, gbits, bbits, abits */
				16, 0, 0, 0,
				/* Masks and shifts */
				0, 0, 0, 0, 0, 0, 0, 0 
				),
			//-----------------------------------------------------------------------
			new PixelFormatDescription(
				"PF_FLOAT32_R",
				PixelFormat.FLOAT32_R,
				/* Bytes per element */ 
				4,  
				/* Flags */
				PixelFormatFlags.Float,  
				/* Component type and count */
				PixelComponentType.Float32, 1,
				/* rbits, gbits, bbits, abits */
				32, 0, 0, 0,
				/* Masks and shifts */
				0, 0, 0, 0, 0, 0, 0, 0 
				),
			//-----------------------------------------------------------------------
			new PixelFormatDescription(
				"PF_SHORT_GR",
				PixelFormat.SHORT_GR,
				/* Bytes per element */ 
				4,  
				/* Flags */
				PixelFormatFlags.NativeEndian,  
				/* Component type and count */
				PixelComponentType.Short, 2,
				/* rbits, gbits, bbits, abits */
				16, 16, 0, 0,
				/* Masks and shifts */
				0x0000FFFF, 0xFFFF0000, 0, 0, 0, 16, 0, 0 
				),
			//-----------------------------------------------------------------------
			new PixelFormatDescription(
				"PF_FLOAT16_GR",
				PixelFormat.FLOAT16_GR,
				/* Bytes per element */ 
				4,  
				/* Flags */
				PixelFormatFlags.Float,  
				/* Component type and count */
				PixelComponentType.Float16, 2,
				/* rbits, gbits, bbits, abits */
				16, 16, 0, 0,
				/* Masks and shifts */
				0, 0, 0, 0, 0, 0, 0, 0 
				),
			//-----------------------------------------------------------------------
			new PixelFormatDescription(
				"PF_FLOAT32_GR",
				PixelFormat.FLOAT32_GR,
				/* Bytes per element */ 
				4,  
				/* Flags */
				PixelFormatFlags.Float,  
				/* Component type and count */
				PixelComponentType.Float32, 2,
				/* rbits, gbits, bbits, abits */
				32, 32, 0, 0,
				/* Masks and shifts */
				0, 0, 0, 0, 0, 0, 0, 0 
				),
			//-----------------------------------------------------------------------
			new PixelFormatDescription(
				"PF_SHORT_RGB",
				PixelFormat.SHORT_RGB,
				/* Bytes per element */ 
				6,  
				/* Flags */
				PixelFormatFlags.None,  
				/* Component type and count */
				PixelComponentType.Short, 3,
				/* rbits, gbits, bbits, abits */
				16, 16, 16, 0,
				/* Masks and shifts */
				0, 0, 0, 0, 0, 0, 0, 0 
				)
		};

		protected static PixelFormatDescription[] IndexedPixelFormats = null;

		public static void Initialize()
		{
			if ( IndexedPixelFormats != null )
				return;
			IndexedPixelFormats = new PixelFormatDescription[ (int)PixelFormat.Count ];
			foreach ( var d in UnindexedPixelFormats )
			{
				IndexedPixelFormats[ (int)d.format ] = d;
			}
		}

		public static PixelFormatDescription GetDescriptionFor( PixelFormat format )
		{
			lock ( UnindexedPixelFormats )
			{
				Initialize();
			}
			return IndexedPixelFormats[ (int)format ];
		}

		#region Static Bulk Conversion Methods

		//*************************************************************************
		//   Pixel packing/unpacking utilities
		//*************************************************************************


        ///<summary>
        ///    Pack a color value to memory
        ///</summary>
        ///<param name="color">The color</param>
        ///<param name="format">Pixel format in which to write the color</param>
        ///<param name="dest">Destination memory location</param>
        public static void PackColor(ColorEx color, PixelFormat format, BufferBase dest)
        {
            PixelConverter.PackColor(color.r, color.g, color.b, color.a, format, dest);
        }

		///<summary>
		///    Pack a color value to memory
		///</summary>
		///<param name="r">Red component, range 0x00 to 0xFF</param>
        ///<param name="g">Green component, range 0x00 to 0xFF</param>
        ///<param name="b">Blue component, range 0x00 to 0xFF</param>
        ///<param name="a">Alpha component, range 0x00 to 0xFF</param>
		///<param name="format">Pixelformat in which to write the color</param>
		///<param name="dest">Destination memory location</param>
        public static void PackColor(uint r, uint g, uint b, uint a, PixelFormat format, BufferBase dest)
		{
			var des = GetDescriptionFor( format );
			if ( ( des.flags & PixelFormatFlags.NativeEndian ) != 0 )
			{
				// Shortcut for integer formats packing
				var value = ( ( ( Bitwise.FixedToFixed( r, 8, des.rbits ) << des.rshift ) & des.rmask ) |
							  ( ( Bitwise.FixedToFixed( g, 8, des.gbits ) << des.gshift ) & des.gmask ) |
							  ( ( Bitwise.FixedToFixed( b, 8, des.bbits ) << des.bshift ) & des.bmask ) |
							  ( ( Bitwise.FixedToFixed( a, 8, des.abits ) << des.ashift ) & des.amask ) );
				// And write to memory
				Bitwise.IntWrite( dest, des.elemBytes, value );
			}
			else
			{
				// Convert to float
				PackColor( r / 255.0f, g / 255.0f, b / 255.0f, a / 255.0f, format, dest );
			}
		}

		///<summary>
		///    Pack a color value to memory
		///</summary>
		///<param name="r">
		///    The four color components, range 0.0f to 1.0f
		///    (an exception to this case exists for floating point pixel
		///    formats, which don't clamp to 0.0f..1.0f)
		///</param>
        ///<param name="g">
        ///    The four color components, range 0.0f to 1.0f
        ///    (an exception to this case exists for floating point pixel
        ///    formats, which don't clamp to 0.0f..1.0f)
        ///</param>
        ///<param name="b">
        ///    The four color components, range 0.0f to 1.0f
        ///    (an exception to this case exists for floating point pixel
        ///    formats, which don't clamp to 0.0f..1.0f)
        ///</param>
        ///<param name="a">
        ///    The four color components, range 0.0f to 1.0f
        ///    (an exception to this case exists for floating point pixel
        ///    formats, which don't clamp to 0.0f..1.0f)
        ///</param>
		///<param name="format">Pixelformat in which to write the color</param>
		///<param name="dest">Destination memory location</param>
        public static void PackColor(float r, float g, float b, float a, PixelFormat format, BufferBase dest)
		{
			// Catch-it-all here
			var des = GetDescriptionFor( format );
			if ( ( des.flags & PixelFormatFlags.NativeEndian ) != 0 )
			{
				// Do the packing
				var value = ( ( Bitwise.FloatToFixed( r, des.rbits ) << des.rshift ) & des.rmask ) |
					( ( Bitwise.FloatToFixed( g, des.gbits ) << des.gshift ) & des.gmask ) |
					( ( Bitwise.FloatToFixed( b, des.bbits ) << des.bshift ) & des.bmask ) |
					( ( Bitwise.FloatToFixed( a, des.abits ) << des.ashift ) & des.amask );
				// And write to memory
				Bitwise.IntWrite( dest, des.elemBytes, value );
			}
			else
			{
#if !AXIOM_SAFE_ONLY
                unsafe
#endif
                {
                    switch (format)
                    {
                        case PixelFormat.FLOAT32_R:
                            var floatdest = (ITypePointer<float>)dest;
                            floatdest[0] = r;
                            break;
                        case PixelFormat.FLOAT32_RGB:
                            floatdest = (ITypePointer<float>)dest;
                            floatdest[0] = r;
                            floatdest[1] = g;
                            floatdest[2] = b;
                            break;
                        case PixelFormat.FLOAT32_RGBA:
                            floatdest = (ITypePointer<float>)dest;
                            floatdest[0] = r;
                            floatdest[1] = g;
                            floatdest[2] = b;
                            floatdest[3] = a;
                            break;
                        case PixelFormat.FLOAT16_R:
                            var ushortdest = (ITypePointer<ushort>)dest;
                            ushortdest[0] = Bitwise.FloatToHalf(r);
                            break;
                        case PixelFormat.FLOAT16_RGB:
                            ushortdest = (ITypePointer<ushort>)dest;
                            ushortdest[0] = Bitwise.FloatToHalf(r);
                            ushortdest[1] = Bitwise.FloatToHalf(g);
                            ushortdest[2] = Bitwise.FloatToHalf(b);
                            break;
                        case PixelFormat.FLOAT16_RGBA:
                            ushortdest = (ITypePointer<ushort>)dest;
                            ushortdest[0] = Bitwise.FloatToHalf(r);
                            ushortdest[1] = Bitwise.FloatToHalf(g);
                            ushortdest[2] = Bitwise.FloatToHalf(b);
                            ushortdest[3] = Bitwise.FloatToHalf(a);
                            break;
                        case PixelFormat.SHORT_RGBA:
                            ushortdest = (ITypePointer<ushort>)dest;
                            ushortdest[0] = (ushort)Bitwise.FloatToFixed(r, 16);
                            ushortdest[1] = (ushort)Bitwise.FloatToFixed(g, 16);
                            ushortdest[2] = (ushort)Bitwise.FloatToFixed(b, 16);
                            ushortdest[3] = (ushort)Bitwise.FloatToFixed(a, 16);
                            break;
                        case PixelFormat.BYTE_LA:
                            var bytedest = (ITypePointer<byte>)dest;
                            bytedest[0] = (byte)Bitwise.FloatToFixed(r, 8);
                            bytedest[1] = (byte)Bitwise.FloatToFixed(a, 8);
                            break;
                        default:
                            // Not yet supported
                            throw new Exception("Pack to " + format + " not implemented, in PixelUtil.PackColor");
                    }
                }
			}
		}
        /// <summary>
        /// Unpack a color value from memory
        /// </summary>
        /// <param name="pf">Pixelformat in which to read the color</param>
        /// <param name="src">Source memory location</param>
        /// <returns>The color is returned here</returns>
        public static ColorEx UnpackColor(PixelFormat pf, BufferBase src)
		{
			ColorEx val;

			UnpackColor( out val.r, out val.g, out val.b, out val.a, pf, src );
			
			return val;
		}

		/// <summary>
		/// Unpack a color value from memory
		/// </summary>
		/// <param name="r">The color is returned here (as byte)</param>
		/// <param name="g">The color is returned here (as byte)</param>
		/// <param name="b">The color is returned here (as byte)</param>
		/// <param name="a">The color is returned here (as byte)</param>
		/// <param name="pf">Pixelformat in which to read the color</param>
		/// <param name="src">Source memory location</param>
		/// <remarks>
		/// This function returns the color components in 8 bit precision,
		/// this will lose precision when coming from A2R10G10B10 or floating
		/// point formats.
		/// </remarks>
        public static void UnpackColor(ref byte r, ref byte g, ref byte b, ref byte a, PixelFormat pf, BufferBase src)
		{
			var des = GetDescriptionFor( pf );
			if ( ( des.flags & PixelFormatFlags.NativeEndian ) != 0 )
			{
				// Shortcut for integer formats unpacking
				var value = Bitwise.IntRead( src, des.elemBytes );
				if ( ( des.flags & PixelFormatFlags.Luminance ) != 0 )
					// Luminance format -- only rbits used
					r = g = b = (byte)Bitwise.FixedToFixed( ( value & des.rmask ) >> des.rshift, des.rbits, 8 );
				else
				{
					r = (byte)Bitwise.FixedToFixed( ( value & des.rmask ) >> des.rshift, des.rbits, 8 );
					g = (byte)Bitwise.FixedToFixed( ( value & des.gmask ) >> des.gshift, des.gbits, 8 );
					b = (byte)Bitwise.FixedToFixed( ( value & des.bmask ) >> des.bshift, des.bbits, 8 );
				}
				if ( ( des.flags & PixelFormatFlags.HasAlpha ) != 0 )
				{
					a = (byte)Bitwise.FixedToFixed( ( value & des.amask ) >> des.ashift, des.abits, 8 );
				}
				else
					a = 255; // No alpha, default a component to full
			}
			else
			{
				// Do the operation with the more generic floating point
				float rr, gg, bb, aa;
				UnpackColor( out rr, out gg, out bb, out aa, pf, src );
				r = Bitwise.FloatToByteFixed( rr );
				g = Bitwise.FloatToByteFixed( gg );
				b = Bitwise.FloatToByteFixed( bb );
				a = Bitwise.FloatToByteFixed( aa );
			}
		}
        /// <summary>
        /// Unpack a color value from memory
        /// </summary>
        /// <param name="r">The color is returned here (as float)</param>
        /// <param name="g">The color is returned here (as float)</param>
        /// <param name="b">The color is returned here (as float)</param>
        /// <param name="a">The color is returned here (as float)</param>
        /// <param name="pf">Pixelformat in which to read the color</param>
        /// <param name="src">Source memory location</param>
        public static void UnpackColor(out float r, out float g, out float b, out float a, PixelFormat pf, BufferBase src)
		{
			var des = GetDescriptionFor( pf );
			if ( ( des.flags & PixelFormatFlags.NativeEndian ) != 0 )
			{
				// Shortcut for integer formats unpacking
				var value = Bitwise.IntRead( src, des.elemBytes );
				if ( ( des.flags & PixelFormatFlags.Luminance ) != 0 )
				{
					// Luminance format -- only rbits used
					r = g = b = Bitwise.FixedToFloat(
						( value & des.rmask ) >> des.rshift, des.rbits );
				}
				else
				{
					r = Bitwise.FixedToFloat( ( value & des.rmask ) >> des.rshift, des.rbits );
					g = Bitwise.FixedToFloat( ( value & des.gmask ) >> des.gshift, des.gbits );
					b = Bitwise.FixedToFloat( ( value & des.bmask ) >> des.bshift, des.bbits );
				}
				if ( ( des.flags & PixelFormatFlags.HasAlpha ) != 0 )
					a = Bitwise.FixedToFloat( ( value & des.amask ) >> des.ashift, des.abits );
				else
					a = 1.0f; // No alpha, default a component to full
			}
			else
			{
#if !AXIOM_SAFE_ONLY
                unsafe
#endif
                {
                    switch (pf)
                    {
                        case PixelFormat.FLOAT32_R:
                            var floatsrc = (ITypePointer<float>)src;
                            r = g = b = floatsrc[0];
                            a = 1.0f;
                            break;
                        case PixelFormat.FLOAT32_RGB:
                            floatsrc = (ITypePointer<float>)src;
                            r = floatsrc[0];
                            g = floatsrc[1];
                            b = floatsrc[2];
                            a = 1.0f;
                            break;
                        case PixelFormat.FLOAT32_RGBA:
                            floatsrc = (ITypePointer<float>)src;
                            r = floatsrc[0];
                            g = floatsrc[1];
                            b = floatsrc[2];
                            a = floatsrc[3];
                            break;
                        case PixelFormat.FLOAT16_R:
                            var ushortsrc = (ITypePointer<ushort>)src;
                            r = g = b = Bitwise.HalfToFloat(ushortsrc[0]);
                            a = 1.0f;
                            break;
                        case PixelFormat.FLOAT16_RGB:
                            ushortsrc = (ITypePointer<ushort>)src;
                            r = Bitwise.HalfToFloat(ushortsrc[0]);
                            g = Bitwise.HalfToFloat(ushortsrc[1]);
                            b = Bitwise.HalfToFloat(ushortsrc[2]);
                            a = 1.0f;
                            break;
                        case PixelFormat.FLOAT16_RGBA:
                            ushortsrc = (ITypePointer<ushort>)src;
                            r = Bitwise.HalfToFloat(ushortsrc[0]);
                            g = Bitwise.HalfToFloat(ushortsrc[1]);
                            b = Bitwise.HalfToFloat(ushortsrc[2]);
                            a = Bitwise.HalfToFloat(ushortsrc[3]);
                            break;
                        case PixelFormat.SHORT_RGBA:
                            ushortsrc = (ITypePointer<ushort>)src;
                            r = Bitwise.FixedToFloat(ushortsrc[0], 16);
                            g = Bitwise.FixedToFloat(ushortsrc[1], 16);
                            b = Bitwise.FixedToFloat(ushortsrc[2], 16);
                            a = Bitwise.FixedToFloat(ushortsrc[3], 16);
                            break;
                        case PixelFormat.BYTE_LA:
                            var bytesrc = (ITypePointer<byte>)src;
                            r = g = b = Bitwise.FixedToFloat(bytesrc[0], 8);
                            a = Bitwise.FixedToFloat(bytesrc[1], 8);
                            break;
                        default:
                            // Not yet supported
                            throw new Exception("Unpack from " + pf + " not implemented, in PixelUtil.UnpackColor");
                    }
				}
			}
		}

	    ///<summary>
	    ///    Convert consecutive pixels from one format to another. No dithering or filtering is being done. 
	    ///    Converting from RGB to luminance takes the R channel.  In case the source and destination format match,
	    ///    just a copy is done.
	    ///</summary>
	    ///<param name="srcBytes">Pointer to source region</param>
	    ///<param name="srcOffset"></param>
	    ///<param name="srcFormat">Pixel format of source region</param>
	    ///<param name="dstBytes">Pointer to destination region</param>
	    ///<param name="dstOffset"></param>
	    ///<param name="dstFormat">Pixel format of destination region</param>
	    ///<param name="count"></param>
        public static void BulkPixelConversion(BufferBase srcBytes, int srcOffset, PixelFormat srcFormat,
                                               BufferBase dstBytes, int dstOffset, PixelFormat dstFormat,
											   int count )
		{
			var src = new PixelBox( count, 1, 1, srcFormat, srcBytes );
			src.Offset = srcOffset;
			var dst = new PixelBox( count, 1, 1, dstFormat, dstBytes );
			dst.Offset = dstOffset;
			BulkPixelConversion( src, dst );
		}

		///<summary>
		///    Convert pixels from one format to another. No dithering or filtering is being done. Converting
		///    from RGB to luminance takes the R channel. 
		///</summary>
		///<param name="src">PixelBox containing the source pixels, pitches and format</param>
		///<param name="dst">PixelBox containing the destination pixels, pitches and format</param>
		///<remarks>
		///    The source and destination boxes must have the same
		///    dimensions. In case the source and destination format match, a plain copy is done.
		///</remarks>
		public static void BulkPixelConversion( PixelBox src, PixelBox dst )
		{
			Debug.Assert( src.Width == dst.Width && src.Height == dst.Height && src.Depth == dst.Depth );

			LogManager.Instance.Write( "Converting image from {0} to {1}", PixelUtil.GetFormatName( src.Format ), PixelUtil.GetFormatName( dst.Format ) );

            // Check for compressed formats, we don't support decompression, compression or recoding
			if ( PixelBox.Compressed( src.Format ) || PixelBox.Compressed( dst.Format ) )
			{
				if ( src.Format == dst.Format )
				{
					Memory.Copy( src.Data, dst.Data, src.Offset, dst.Offset, src.ConsecutiveSize );
					return;
				}
				else
					throw new Exception( "This method can not be used to compress or decompress images, in PixelBox.BulkPixelConversion" );
			}

			// The easy case
            if (src.Format == dst.Format)
			{
				// Everything consecutive?
				if ( src.IsConsecutive && dst.IsConsecutive )
				{
					Memory.Copy( src.Data, dst.Data, src.Offset, dst.Offset, src.ConsecutiveSize );
					return;
				}

				// TODO : Use OptimizedPixelConversion to elminate this duplicate code.
#if !AXIOM_SAFE_ONLY
				unsafe
#endif
				{
					var srcBytes = src.Data;
					var dstBytes = dst.Data;
					var srcptr = srcBytes + src.Offset;
					var dstptr = dstBytes + dst.Offset;
					var srcPixelSize = PixelUtil.GetNumElemBytes( src.Format );
					var dstPixelSize = PixelUtil.GetNumElemBytes( dst.Format );

					// Calculate pitches+skips in bytes
					var srcRowPitchBytes = src.RowPitch * srcPixelSize;
					//int srcRowSkipBytes = src.RowSkip * srcPixelSize;
					var srcSliceSkipBytes = src.SliceSkip * srcPixelSize;

					var dstRowPitchBytes = dst.RowPitch * dstPixelSize;
					//int dstRowSkipBytes = dst.RowSkip * dstPixelSize;
					var dstSliceSkipBytes = dst.SliceSkip * dstPixelSize;

					// Otherwise, copy per row
					var rowSize = src.Width * srcPixelSize;
					for ( var z = src.Front; z < src.Back; z++ )
					{
						for ( var y = src.Top; y < src.Bottom; y++ )
						{
                            var s = srcptr.ToBytePointer();
                            var d = dstptr.ToBytePointer();
                            for (var i = 0; i < rowSize; i++)
                                d[i] = s[i];
							srcptr += srcRowPitchBytes;
							dstptr += dstRowPitchBytes;
						}
						srcptr.Ptr += srcSliceSkipBytes;
                        dstptr.Ptr += dstSliceSkipBytes;
					}
				}
				return;
			}


			// Converting to X8R8G8B8 is exactly the same as converting to
			// A8R8G8B8. (same with X8B8G8R8 and A8B8G8R8)
			if ( dst.Format == PixelFormat.X8R8G8B8 || dst.Format == PixelFormat.X8B8G8R8 )
			{
				// Do the same conversion, with A8R8G8B8, which has a lot of 
				// optimized conversions
				var dstFormat = dst.Format == PixelFormat.X8R8G8B8 ? PixelFormat.A8R8G8B8 : PixelFormat.A8B8G8R8;
				var tempdst = new PixelBox( dst, dstFormat, dst.Data );
				BulkPixelConversion( src, tempdst );
				return;
			}

			// Converting from X8R8G8B8 is exactly the same as converting from
			// A8R8G8B8, given that the destination format does not have alpha.
			if ( ( src.Format == PixelFormat.X8R8G8B8 || src.Format == PixelFormat.X8B8G8R8 ) && !PixelUtil.HasAlpha( dst.Format ) )
			{
				// Do the same conversion, with A8R8G8B8, which has a lot of 
				// optimized conversions
				var srcFormat = src.Format == PixelFormat.X8R8G8B8 ? PixelFormat.A8R8G8B8 : PixelFormat.A8B8G8R8;
				var tempsrc = new PixelBox( src, srcFormat, src.Data );
				BulkPixelConversion( tempsrc, dst );
				return;
			}

			if ( OptimizedPixelConversion.DoOptimizedConversion( src, dst ) )
				// If so, good
				return;


			// TODO : Use OptimizedPixelConversion to elminate this duplicate code.
#if !AXIOM_SAFE_ONLY
			unsafe
#endif
			{
				var srcBytes = src.Data;
				var dstBytes = dst.Data;
				var srcptr = srcBytes + src.Offset;
				var dstptr = dstBytes + dst.Offset;
				var srcPixelSize = PixelUtil.GetNumElemBytes( src.Format );
				var dstPixelSize = PixelUtil.GetNumElemBytes( dst.Format );

				// Calculate pitches+skips in bytes
				var srcRowSkipBytes = src.RowSkip * srcPixelSize;
				var srcSliceSkipBytes = src.SliceSkip * srcPixelSize;
				var dstRowSkipBytes = dst.RowSkip * dstPixelSize;
				var dstSliceSkipBytes = dst.SliceSkip * dstPixelSize;

				// The brute force fallback
				float r, g, b, a;
				for ( var z = src.Front; z < src.Back; z++ )
				{
					for ( var y = src.Top; y < src.Bottom; y++ )
					{
						for ( var x = src.Left; x < src.Right; x++ )
						{
							UnpackColor( out r, out g, out b, out a, src.Format, srcptr );
							PackColor( r, g, b, a, dst.Format, dstptr );
							srcptr += srcPixelSize;
							dstptr += dstPixelSize;
						}
						srcptr += srcRowSkipBytes;
						dstptr += dstRowSkipBytes;
					}
					srcptr += srcSliceSkipBytes;
					dstptr += dstSliceSkipBytes;
				}
			}
		}

		#endregion Static Bulk Conversion Methods
	}

}

