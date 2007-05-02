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

#endregion Namespace Declarations

namespace Axiom.Media
{
	/// <summary>
	///    Various flags that give details on a particular image.
	/// </summary>
	[Flags]
	public enum ImageFlags
	{
		Compressed = 0x00000001,
		CubeMap = 0x00000002,
		Volume = 0x00000004
	}

	/// <summary>
	///    The pixel format used for images.
	/// </summary>
	public enum PixelFormat
	{
		/// <summary>
		///    Unknown pixel format.
		/// </summary>
		Unknown,
		/// <summary>
		///    8-bit pixel format, all bits luminance.
		/// </summary>
		L8,
		/// <summary>
		///    8-bit pixel format, all bits alpha.
		/// </summary>
		A8,
		/// <summary>
		///    8-bit pixel format, 4 bits alpha, 4 bits luminance.
		/// </summary>
		A4L4,
		/// <summary>
		///    8-bit pixel format, 4 bits luminace, 4 bits alpha.
		/// </summary>
		L4A4,
		/// <summary>
		///   8-bit pixel format, 3 bits red, 3 bits green, 2 bits blue.
		/// </summary>
		R3G3B2,
		/// <summary>
		///    16-bit pixel format, 5 bits red, 6 bits green, 5 bits blue.
		/// </summary>
		R5G6B5,
		/// <summary>
		///    16-bit pixel format, 5 bits blue, 6 bits green, 5 bits red.
		/// </summary>
		B5G6R5,
		/// <summary>
		///    16-bit pixel format, 4 bits for alpha, red, green and blue.
		/// </summary>
		A4R4G4B4,
		/// <summary>
		///    16-bit pixel format, 4 bits for blue, green, red and alpha.
		/// </summary>
		B4G4R4A4,
		/// <summary>
		///    16-bit pixel format, 8 bits for alpha, 8 bits for luminance.
		/// </summary>
		A8L8,
		/// <summary>
		///    16-bit pixel format, all bits luminance.
		/// </summary>
		L16,
		/// <summary>
		///    16-bit pixel format, 1 bit for alpha, 5 bits for blue, green and red.
		/// </summary>
		A1R5G5B5,
		/// <summary>
		///    24-bit pixel format, 8 bits for red, green and blue.
		/// </summary>
		R8G8B8,
		/// <summary>
		///    24-bit pixel format, 8 bits for blue, green and red.
		/// </summary>
		B8G8R8,
		/// <summary>
		///    24-bit pixel format, all bits luminance.
		/// </summary>
		L24,
		/// <summary>
		///    32-bit pixel format, 8 bits for alpha, red, green and blue.
		/// </summary>
		A8R8G8B8,
		/// <summary>
		///    32-bit pixel format, 8 bits for alpha, blue, green and red`.
		/// </summary>
		A8B8G8R8,
		/// <summary>
		///    32-bit pixel format, 8 bits for blue, green, red and alpha.
		/// </summary>
		B8G8R8A8,
		/// <summary>
		///    32-bit pixel format, 8 bits for red, green, blue and alpha.
		/// </summary>
		R8G8B8A8,
		/// <summary>
		///    32-bit pixel format, 8 bits for red, green and blue.
		/// </summary>
		X8R8G8B8,
		/// <summary>
		///    32-bit pixel format, 8 bits for blue, green and red.
		/// </summary>
		X8B8G8R8,
		/// <summary>
		///    32-bit pixel format, 2 bits for alpha, 10 bits for red, green and blue.
		/// </summary>
		A2R10G10B10,
		/// <summary>
		///    32-bit pixel format, 10 bits for blue, green and red, 2 bits for alpha.
		/// </summary>
		A2B10G10R10,
		/// <summary>
		///    DDS (DirectDraw Surface) DXT1 format.
		/// </summary>
		DXT1,
		/// <summary>
		///    DDS (DirectDraw Surface) DXT2 format.
		/// </summary>
		DXT2,
		/// <summary>
		///    DDS (DirectDraw Surface) DXT3 format.
		/// </summary>
		DXT3,
		/// <summary>
		///    DDS (DirectDraw Surface) DXT4 format.
		/// </summary>
		DXT4,
		/// <summary>
		///    DDS (DirectDraw Surface) DXT5 format.
		/// </summary>
		DXT5,
		/// <summary>
		///		Depth texture format
		/// </summary>
		DEPTH,
		/// <summary>
		///   64 bit pixel format, 16 bits for red, 16 bits for green, 16 bits for blue, 16 bits for alpha
		/// </summary>
		SHORT_RGBA,
		/// <summary>
		///    16 bit floating point with a single channel (red)
		/// </summary>
		FLOAT16_R,
		/// <summary>
		///    48-bit pixel format, 16 bits (float) for red, 16 bits (float) for green, 16 bits (float) for blue
		/// </summary>
		FLOAT16_RGB,
		/// <summary>
		///    64-bit pixel format, 16 bits (float) for red, 16 bits (float) for green, 16 bits (float) for blue, 16 bits (float) for alpha
		/// </summary>
		FLOAT16_RGBA,
		/// <summary>
		///    32 bit floating point with a single channel (red)
		/// </summary>
		FLOAT32_R,
		/// <summary>
		///    96-bit pixel format, 32 bits (float) for red, 32 bits (float) for green, 32 bits (float) for blue
		/// </summary>
		FLOAT32_RGB,
		/// <summary>
		///    128-bit pixel format, 32 bits (float) for red, 32 bits (float) for green, 32 bits (float) for blue, 32 bits (float) for alpha
		/// </summary>
		FLOAT32_RGBA,
		/// <summary>
		///    The last one, used to size arrays of PixelFormat.  Don't add anything after this one!
		/// </summary>
		Count
	}

	/// <summary>
	///    Flags defining some on/off properties of pixel formats
	/// </summary>
	public enum PixelFormatFlags
	{
		// No flags
		None = 0x00000000,
		// This format has an alpha channel
		HasAlpha = 0x00000001,
		// This format is compressed. This invalidates the values in elemBytes,
		// elemBits and the bit counts as these might not be fixed in a compressed format.
		Compressed = 0x00000002,
		// This is a floating point format
		Float = 0x00000004,
		// This is a depth format (for depth textures)
		Depth = 0x00000008,
		// Format is in native endian. Generally true for the 16, 24 and 32 bits
		// formats which can be represented as machine integers.
		NativeEndian = 0x00000010,
		// This is an intensity format instead of a RGB one. The luminance
		// replaces R,G and B. (but not A)
		Luminance = 0x00000020
	}

	/// <summary>
	///    Pixel component format
	/// </summary>
	public enum PixelComponentType
	{
		Byte = 0,    /// Byte per component (8 bit fixed 0.0..1.0)
		Short = 1,   /// Short per component (16 bit fixed 0.0..1.0))
		Float16 = 2, /// 16 bit float per component
		Float32 = 3, /// 32 bit float per component
		Count = 4    /// Number of pixel types
	}
}
