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

using Axiom.Core;
using Axiom.Math;
using Axiom.Utilities;

#endregion Namespace Declarations

namespace Axiom.Media
{
	public class PixelUtil
	{
		/// <summary>
		///   Returns the size in bytes of an element of the given pixel format.
		/// </summary>
		/// <param name="format"> Pixel format to test. </param>
		/// <returns> Size in bytes. </returns>
		[OgreVersion( 1, 7, 2 )]
		public static int GetNumElemBytes( PixelFormat format )
		{
			return PixelConverter.GetDescriptionFor( format ).elemBytes;
		}

		/// <summary>
		///   Returns the size in bits of an element of the given pixel format.
		/// </summary>
		/// <param name="format"> Pixel format to test. </param>
		/// <returns> Size in bits. </returns>
		[OgreVersion( 1, 7, 2 )]
		public static int GetNumElemBits( PixelFormat format )
		{
			return GetNumElemBytes( format )*8;
		}

		/// <summary>
		///   Gives the number of bits (RGBA) for a format. See remarks.
		/// </summary>
		/// <remarks>
		///   For non-colour formats (dxt, depth) this returns [0,0,0,0].
		/// </remarks>
		[OgreVersion( 1, 7, 2 )]
		public static int[] GetBitDepths( PixelFormat format )
		{
			var rgba = new int[4];
			var des = PixelConverter.GetDescriptionFor( format );
			rgba[ 0 ] = des.rbits;
			rgba[ 1 ] = des.gbits;
			rgba[ 2 ] = des.bbits;
			rgba[ 3 ] = des.abits;
			return rgba;
		}

		/// <summary>
		///   Gives the masks for the R, G, B and A component
		/// </summary>
		/// <remarks>
		///   Only valid for native endian formats
		/// </remarks>
		[OgreVersion( 1, 7, 2 )]
		public static uint[] GetBitMasks( PixelFormat format )
		{
			var rgba = new uint[4];
			var des = PixelConverter.GetDescriptionFor( format );
			rgba[ 0 ] = des.rmask;
			rgba[ 1 ] = des.gmask;
			rgba[ 2 ] = des.bmask;
			rgba[ 3 ] = des.amask;
			return rgba;
		}

		/// <summary>
		///   Gives the bit shifts for R, G, B and A component
		/// </summary>
		/// <remarks>
		///   Only valid for native endian formats
		/// </remarks>
		[OgreVersion( 1, 7, 2 )]
		public static byte[] GetBitShifts( PixelFormat format )
		{
			var rgba = new byte[4];
			var des = PixelConverter.GetDescriptionFor( format );
			rgba[ 0 ] = des.rshift;
			rgba[ 1 ] = des.gshift;
			rgba[ 2 ] = des.bshift;
			rgba[ 3 ] = des.ashift;

			return rgba;
		}

		///<summary>
		///  Returns the size in memory of a region with the given extents and pixel format with consecutive memory layout.
		///</summary>
		///<param name="width"> Width of the area </param>
		///<param name="height"> Height of the area </param>
		///<param name="depth"> Depth of the area </param>
		///<param name="format"> Format of the area </param>
		///<returns> The size in bytes </returns>
		///<remarks>
		///  In case that the format is non-compressed, this simply returns width * height * depth * PixelConverter.GetNumElemBytes(format). In the compressed case, this does serious magic.
		///</remarks>
		[OgreVersion( 1, 7, 2 )]
		public static int GetMemorySize( int width, int height, int depth, PixelFormat format )
		{
			if ( IsCompressed( format ) )
			{
				switch ( format )
				{
					case PixelFormat.DXT1:
						return ( ( width + 3 )/4 )*( ( height + 3 )/4 )*8*depth;

					case PixelFormat.DXT2:
					case PixelFormat.DXT3:
					case PixelFormat.DXT4:
					case PixelFormat.DXT5:
						return ( ( width + 3 )/4 )*( ( height + 3 )/4 )*16*depth;

						// Size calculations from the PVRTC OpenGL extension spec
						// http://www.khronos.org/registry/gles/extensions/IMG/IMG_texture_compression_pvrtc.txt
						// Basically, 32 bytes is the minimum texture size.  Smaller textures are padded up to 32 bytes
					case PixelFormat.PVRTC_RGB2:
					case PixelFormat.PVRTC_RGBA2:
						Contract.Requires( depth == 1 );
						return ( Utility.Max( width, 16 )*Utility.Max( height, 8 )*2 + 7 )/8;

					case PixelFormat.PVRTC_RGB4:
					case PixelFormat.PVRTC_RGBA4:
						Contract.Requires( depth == 1 );
						return ( Utility.Max( width, 8 )*Utility.Max( height, 8 )*4 + 7 )/8;

					default:
						throw new AxiomException( "Invalid compressed pixel format" );
				}
			}
			else
			{
				return width*height*depth*GetNumElemBytes( format );
			}
		}

		/// <summary>
		///   Returns wether the format can be packed or unpacked with the packColour() and unpackColour() functions. This is generally not true for compressed and depth formats as they are special. It can only be true for formats with a fixed element size.
		/// </summary>
		/// <returns> true if yes, otherwise false </returns>
		[OgreVersion( 1, 7, 2 )]
		public static bool IsAccessible( PixelFormat format )
		{
			if ( format == PixelFormat.Unknown )
			{
				return false;
			}
			var flags = PixelConverter.GetDescriptionFor( format ).flags;
			return !( ( flags & PixelFormatFlags.Compressed ) > 0 || ( flags & PixelFormatFlags.Depth ) > 0 );
		}

		/// <summary>
		///   Shortcut method to determine if the format is compressed
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public static bool IsCompressed( PixelFormat format )
		{
			return ( PixelConverter.GetDescriptionFor( format ).flags & PixelFormatFlags.Compressed ) > 0;
		}

		/// <summary>
		///   Shortcut method to determine if the format is floating point
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public static bool IsFloatingPoint( PixelFormat format )
		{
			return ( PixelConverter.GetDescriptionFor( format ).flags & PixelFormatFlags.Float ) > 0;
		}

		/// <summary>
		///   Shortcut method to determine if the format has an alpha component
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public static bool HasAlpha( PixelFormat format )
		{
			return ( PixelConverter.GetDescriptionFor( format ).flags & PixelFormatFlags.HasAlpha ) > 0;
		}

		/// <summary>
		///   Shortcut method to determine if the format is a luminance format.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public static bool IsLuminance( PixelFormat format )
		{
			return ( PixelConverter.GetDescriptionFor( format ).flags & PixelFormatFlags.Luminance ) > 0;
		}

		/// <summary>
		///   Shortcut method to determine if the format is in native endian format.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public static bool IsNativeEndian( PixelFormat format )
		{
			return ( PixelConverter.GetDescriptionFor( format ).flags & PixelFormatFlags.NativeEndian ) > 0;
		}

		/// <summary>
		///   Gets the name of an image format
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public static string GetFormatName( PixelFormat format )
		{
			return PixelConverter.GetDescriptionFor( format ).name;
		}

		/// <summary>
		///   Returns the component type for a certain pixel format. Returns <see cref="PixelComponentType.Byte" /> in case there is no clear component type like with compressed formats. This is one of <see
		///    cref="PixelComponentType.Byte" /> , <see cref="PixelComponentType.Short" /> , <see cref="PixelComponentType.Float16" /> , <see
		///    cref="PixelComponentType.Float32" /> .
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public static PixelComponentType GetComponentType( PixelFormat format )
		{
			return PixelConverter.GetDescriptionFor( format ).componentType;
		}

		/// <summary>
		///   Gets the format from given name.
		/// </summary>
		/// <param name="name"> The string of format name </param>
		/// <param name="accessibleOnly"> If true, non-accessible format will treat as invalid format, otherwise, all supported formats are valid. </param>
		/// <param name="caseSensitive"> Should be set true if string match should use case sensitivity. </param>
		/// <returns> The format match the format name, or <see cref="PixelFormat.Unknown" /> if is invalid name. </returns>
		[OgreVersion( 1, 7, 2 )]
#if NET_40
		public static PixelFormat GetFormatFromName( string name, bool accessibleOnly = false, bool caseSensitive = false )
#else
		public static PixelFormat GetFormatFromName( string name, bool accessibleOnly, bool caseSensitive )
#endif
		{
			// We are storing upper-case format names.
			var tmp = caseSensitive ? name : name.ToUpper();

			for ( var i = 0; i < (int)PixelFormat.Count; ++i )
			{
				var pf = (PixelFormat)i;
				if ( !accessibleOnly || IsAccessible( pf ) )
				{
					if ( tmp == GetFormatName( pf ) )
					{
						return pf;
					}
				}
			}
			return PixelFormat.Unknown;
		}

#if !NET_40
		/// <see cref="GetFormatFromName(string, bool, bool)" />
		public static PixelFormat GetFormatFromName( string name )
		{
			return GetFormatFromName( name, false, false );
		}

		/// <see cref="GetFormatFromName(string, bool, bool)" />
		public static PixelFormat GetFormatFromName( string name, bool accessibleOnly )
		{
			return GetFormatFromName( name, accessibleOnly, false );
		}
#endif

		/// <summary>
		///   Returns the similar format but acoording with given bit depths.
		/// </summary>
		/// <param name="format"> The original format. </param>
		/// <param name="integerBits"> Preferred bit depth (pixel bits) for integer pixel format. Available values: 0, 16 and 32, where 0 (the default) means as it is. </param>
		/// <param name="floatBits"> Preferred bit depth (channel bits) for float pixel format. Available values: 0, 16 and 32, where 0 (the default) means as it is. </param>
		/// <returns> The format that similar original format with bit depth according with preferred bit depth, or original format if no convertion occuring. </returns>
		[OgreVersion( 1, 7, 2 )]
		public static PixelFormat GetFormatForBitDepths( PixelFormat format, ushort integerBits, ushort floatBits )
		{
			switch ( integerBits )
			{
				case 16:
					switch ( format )
					{
						case PixelFormat.R8G8B8:
						case PixelFormat.X8R8G8B8:
							return PixelFormat.R5G6B5;

						case PixelFormat.B8G8R8:
						case PixelFormat.X8B8G8R8:
							return PixelFormat.B5G6R5;

						case PixelFormat.A8R8G8B8:
						case PixelFormat.R8G8B8A8:
						case PixelFormat.A8B8G8R8:
						case PixelFormat.B8G8R8A8:
							return PixelFormat.A4R4G4B4;

						default:
							// use the original format
							break;
					}
					break;

				case 32:
					switch ( format )
					{
						case PixelFormat.R5G6B5:
							return PixelFormat.X8R8G8B8;

						case PixelFormat.B5G6R5:
							return PixelFormat.X8B8G8R8;

						case PixelFormat.A4R4G4B4:
							return PixelFormat.A8R8G8B8;

						case PixelFormat.A1R5G5B5:
							return PixelFormat.A2R10G10B10;

						default:
							// use the original format
							break;
					}
					break;

				default:
					// use the original format
					break;
			}

			switch ( floatBits )
			{
				case 16:
					switch ( format )
					{
						case PixelFormat.FLOAT32_R:
							return PixelFormat.FLOAT16_R;

						case PixelFormat.FLOAT32_RGB:
							return PixelFormat.FLOAT16_RGB;

						case PixelFormat.FLOAT32_RGBA:
							return PixelFormat.FLOAT16_RGBA;

						default:
							// use original image format
							break;
					}
					break;

				case 32:
					switch ( format )
					{
						case PixelFormat.FLOAT16_R:
							return PixelFormat.FLOAT32_R;

						case PixelFormat.FLOAT16_RGB:
							return PixelFormat.FLOAT32_RGB;

						case PixelFormat.FLOAT16_RGBA:
							return PixelFormat.FLOAT32_RGBA;

						default:
							// use original image format
							break;
					}
					break;

				default:
					// use original image format
					break;
			}

			return format;
		}
	};
}