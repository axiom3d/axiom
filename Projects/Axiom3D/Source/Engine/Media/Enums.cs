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
        ///    24-bit pixel format, 8 bits for red, green and blue.
        /// </summary>
        R8G8B8,
        /// <summary>
        ///    24-bit pixel format, 8 bits for blue, green and red.
        /// </summary>
        B8G8R8,
        /// <summary>
        ///    32-bit pixel format, 8 bits for alpha, red, green and blue.
        /// </summary>
        A8R8G8B8,
        /// <summary>
        ///    32-bit pixel format, 8 bits for blue, green, red and alpha.
        /// </summary>
        B8G8R8A8,
        /// <summary>
        ///    32-bit pixel format, 2 bits for alpha, 10 bits for red, green and blue.
        /// </summary>
        A2R10G10B10,
        /// <summary>
        ///    32-bit pixel format, 10 bits for blue, green and red, 2 bits for alpha.
        /// </summary>
        B10G10R10A2,
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
        DXT5
    }
}
