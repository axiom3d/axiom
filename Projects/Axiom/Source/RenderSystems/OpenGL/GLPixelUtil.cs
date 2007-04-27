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
using System.Collections.Generic;
using System.Text;

using Tao.OpenGl;

using Axiom.Media;

#endregion Namespace Declarations
			
namespace Axiom.RenderSystems.OpenGL
{
	static class GLPixelUtil
	{
		/** Takes the OGRE pixel format and returns the appropriate GL one
			@returns a GLenum describing the format, or 0 if there is no exactly matching 
			one (and conversion is needed)
		*/
		public static int GetGLOriginFormat( PixelFormat format )
		{
			switch ( format )
			{
				case PixelFormat.A8:
					return Gl.GL_ALPHA;
				case PixelFormat.L8:
					return Gl.GL_LUMINANCE;
				case PixelFormat.L16:
					return Gl.GL_LUMINANCE;
				case PixelFormat.L4A4:
					return Gl.GL_LUMINANCE_ALPHA;
				case PixelFormat.R3G3B2:
					return Gl.GL_RGB;
				case PixelFormat.A1R5G5B5:
					return Gl.GL_BGRA;
				case PixelFormat.R5G6B5:
					return Gl.GL_RGB;
				case PixelFormat.B5G6R5:
					return Gl.GL_BGR;
				case PixelFormat.A4R4G4B4:
					return Gl.GL_BGRA;
#if OGRE_ENDIAN == OGRE_ENDIAN_BIG
				// Formats are in native endian, so R8G8B8 on little endian is
				// BGR, on big endian it is RGB.
				case PixelFormat.R8G8B8:
					return Gl.GL_RGB;
				case PixelFormat.B8G8R8:
					return Gl.GL_BGR;
#else
            case PixelFormat.R8G8B8:
                return Gl.GL_BGR;
            case PixelFormat.B8G8R8:
                return Gl.GL_RGB;
#endif
				case PixelFormat.X8R8G8B8:
				case PixelFormat.A8R8G8B8:
					return Gl.GL_BGRA;
				case PixelFormat.X8B8G8R8:
				case PixelFormat.A8B8G8R8:
					return Gl.GL_RGBA;
				case PixelFormat.B8G8R8A8:
					return Gl.GL_BGRA;
				case PixelFormat.R8G8B8A8:
					return Gl.GL_RGBA;
				case PixelFormat.A2R10G10B10:
					return Gl.GL_BGRA;
				case PixelFormat.A2B10G10R10:
					return Gl.GL_RGBA;
				case PixelFormat.FLOAT16_R:
					return Gl.GL_LUMINANCE;
				//case PixelFormat.FLOAT16_GR:
				//    return Gl.GL_LUMINANCE_ALPHA;
				case PixelFormat.FLOAT16_RGB:
					return Gl.GL_RGB;
				case PixelFormat.FLOAT16_RGBA:
					return Gl.GL_RGBA;
				case PixelFormat.FLOAT32_R:
					return Gl.GL_LUMINANCE;
				//case PixelFormat.FLOAT32_GR:
				//    return Gl.GL_LUMINANCE_ALPHA;
				case PixelFormat.FLOAT32_RGB:
					return Gl.GL_RGB;
				case PixelFormat.FLOAT32_RGBA:
					return Gl.GL_RGBA;
				case PixelFormat.SHORT_RGBA:
					return Gl.GL_RGBA;
				//case PixelFormat.SHORT_RGB:
				//    return Gl.GL_RGB;
				//case PixelFormat.SHORT_GR:
				//    return Gl.GL_LUMINANCE_ALPHA;
				case PixelFormat.DXT1:
					return Gl.GL_COMPRESSED_RGBA_S3TC_DXT1_EXT;
				case PixelFormat.DXT3:
					return Gl.GL_COMPRESSED_RGBA_S3TC_DXT3_EXT;
				case PixelFormat.DXT5:
					return Gl.GL_COMPRESSED_RGBA_S3TC_DXT5_EXT;
			}

			return 0;
		}
	
		/** Takes the OGRE pixel format and returns type that must be provided
			to GL as data type for reading it into the GPU
			@returns a GLenum describing the data type, or 0 if there is no exactly matching 
			one (and conversion is needed)
		*/
		public static int GetGLOriginDataType( PixelFormat format )
		{
		}
        
        /**	Takes the OGRE pixel format and returns the type that must be provided
			to GL as internal format. GL_NONE if no match exists.
		*/
		public static int GetGLInternalFormat( PixelFormat format )
		{
		}
	
		/**	Takes the OGRE pixel format and returns the type that must be provided
			to GL as internal format. If no match exists, returns the closest match.
		*/
		public static int GetClosestGLInternalFormat( PixelFormat format )
		{
		}
		
		/**	Function to get the closest matching OGRE format to an internal GL format. To be
			precise, the format will be chosen that is most efficient to transfer to the card 
			without losing precision.
			@remarks It is valid for this function to always return PixelFormat.A8R8G8B8.
		*/
		public static PixelFormat GetClosestPixelFormat( int format )
		{
		}
	
		/** Returns the maximum number of Mipmaps that can be generated until we reach
			the mininum format possible. This does not count the base level.
			@param width
				The width of the area
			@param height
				The height of the area
			@param depth
				The depth of the area
			@param format
				The format of the area
			@remarks
				In case that the format is non-compressed, this simply returns
				how many times we can divide this texture in 2 until we reach 1x1.
				For compressed formats, constraints apply on minimum size and alignment
				so this might differ.
		*/
		public static int GetMaxMipmaps( int width, int height, int depth, PixelFormat format )
		{
		}
        
        /** Returns next power-of-two size if required by render system, in case
            RSC_NON_POWER_OF_2_TEXTURES is supported it returns value as-is.
        */
		public static int OptionalPO2( int value )
		{
		}
	}
}
