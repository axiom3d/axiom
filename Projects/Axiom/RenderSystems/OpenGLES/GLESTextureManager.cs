#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2010 Axiom Project Team
This file is part of Axiom.RenderSystems.OpenGLES
C# version developed by bostich.

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
#endregion LGPL License
#region SVN Version Information
// <file>
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations
using System;
using Axiom.Core;
using Axiom.Collections;
using Axiom.Graphics;
using Axiom.Media;
using ResourceHandle = System.Int64;
#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGLES
{
    /// <summary>
    /// GL ES-specific implementation of a TextureManager
    /// </summary>
	public class GLESTextureManager : TextureManager
	{
        protected GLESSupport _glSupport;
        /// <summary>
        /// 
        /// </summary>
        public int WarningTextureID
        {
            get;
            protected set;
        }
        /// <summary>
        /// 
        /// </summary>
        protected void CraeteWarningTexture()
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ttype"></param>
        /// <param name="format"></param>
        /// <param name="usage"></param>
        /// <returns></returns>
        public override PixelFormat GetNativeFormat(TextureType ttype, PixelFormat format, TextureUsage usage)
        {
            return base.GetNativeFormat(ttype, format, usage);
        }
        /// <summary>
        /// Returns whether this render system has hardware filtering supported for the
        /// texture format requested with the given usage options.
        /// </summary>
        /// <param name="ttype">The texture type requested</param>
        /// <param name="format">The pixel format requested</param>
        /// <param name="usage">the kind of usage this texture is intended for, a combination of the TextureUsage flags.</param>
        /// <param name="preciseFormatOnly">
        /// Whether precise or fallback format mode is used to detecting.
        /// In case the pixel format doesn't supported by device, false will be returned
        /// if in precise mode, and natively used pixel format will be actually use to
        /// check if in fallback mode.
        /// </param>
        /// <returns>true if the texture filtering is supported.</returns>
        public override bool IsHardwareFilteringSupported(TextureType ttype, PixelFormat format, int usage, bool preciseFormatOnly)
        {
            throw new NotImplementedException();
        }
        protected override Resource _create(string name, ulong handle, string group, bool isManual, IManualResourceLoader loader, NameValuePairList createParams)
        {
            throw new NotImplementedException();
        }
	}
}

