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

using Axiom.Collections;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Media;
using Microsoft.Xna.Framework.Graphics;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.Xna
{
    /// <summary>
    ///     Summary description for XnaTextureManager.
    /// </summary>
    public class XnaTextureManager : TextureManager
    {
        /// <summary>Reference to the XNA device.</summary>
        private GraphicsDevice _device;

        public XnaTextureManager( GraphicsDevice device )
        {
            _device = device;

            Is32Bit = true;
        }

        /// <summary>
        /// Class level dispose method
        /// </summary>
        protected override void dispose( bool disposeManagedResources )
        {
            if ( !IsDisposed )
            {
                if ( disposeManagedResources )
                {
                    _device = null;
                }

                // There are no unmanaged resources to release, but
                // if we add them, they need to be released here.
            }

            // If it is available, make the call to the
            // base class's Dispose(Boolean) method
            base.dispose( disposeManagedResources );
        }

        protected override Resource _create( string name, ulong handle, string group, bool isManual,
                                             IManualResourceLoader loader, NameValuePairList createParams )
        {
            return new XnaTexture( this, name, handle, group, isManual, loader, _device );
        }

        public override PixelFormat GetNativeFormat(TextureType ttype, PixelFormat format, Graphics.TextureUsage usage)
        {
            return PixelFormat.A8B8G8R8;
        }


        public void ReleaseDefaultPoolResources()
        {
            var count = 0;
            foreach ( XnaTexture tex in Resources )
            {
                //TODO : Implement XnaTexture.ReleaseIfDefaultPool()
                //if ( tex.ReleaseIfDefaultPool() )
                //    count++;
            }
            LogManager.Instance.Write( "[XNA] : TextureManager released: {0} unmanaged textures", count );
        }

        public void RecreateDefaultPoolResources()
        {
            var count = 0;
            foreach ( XnaTexture tex in Resources )
            {
                //TODO : Implement XnaTexture.RecreateIfDefaultPool()
                //if ( tex.RecreateIfDefaultPool( device ) )
                //    count++;
            }
            LogManager.Instance.Write( "[XNA] : TextureManager recreated: {0} unmanaged textures", count );
        }

        /// <see cref="Axiom.Core.TextureManager.IsHardwareFilteringSupported(TextureType, PixelFormat, TextureUsage, bool)"/>
#if NET_40
        public override bool IsHardwareFilteringSupported( TextureType ttype, PixelFormat format, TextureUsage usage, bool preciseFormatOnly = false )
#else
        public override bool IsHardwareFilteringSupported( TextureType ttype, PixelFormat format, TextureUsage usage, bool preciseFormatOnly )
#endif
        {
            //TODO
            return false;
        }
    };
}