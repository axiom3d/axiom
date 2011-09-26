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

#endregion LGPL License

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using Axiom.Core;
using Axiom.Graphics;
using Microsoft.Xna.Framework.GamerServices;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.Xna
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class XnaGamerServices
    {
        private Root _engine;
        private XnaRenderSystem _renderSystem;
        private RenderWindow _window;

        /// <summary>
        /// Creates a new instance of <see cref="XnaGamerServices"/>
        /// </summary>
        /// <param name="engine">The engine</param>
        /// <param name="renderSystem">the rendersystem in use</param>
        /// <param name="window">The primary window</param>
        public XnaGamerServices( Root engine, XnaRenderSystem renderSystem, RenderWindow window )
        {
            _engine = engine;
            _renderSystem = renderSystem;
            _window = window;
        }

        /// <summary>
        /// Initializes the XNA GamerServicesDispatcher
        /// </summary>
        public void Initialize()
        {
            GamerServicesDispatcher.WindowHandle = (IntPtr)_window[ "WINDOW" ];
            GamerServicesDispatcher.Initialize( _renderSystem );
            _engine.FrameStarted += Update;
        }

        /// <summary>
        /// Stops the gamer services component from updating
        /// </summary>
        public void Shutdown()
        {
            _engine.FrameStarted -= Update;
            _engine = null;
            _renderSystem = null;
            _window = null;
        }

        /// <summary>
        /// Helper method to call <see cref="GamerServicesDispatcher.Update"/> every frame.
        /// </summary>
        /// <param name="sender">object that invoked the event</param>
        /// <param name="e">per-frame specfic arguments</param>
        private void Update( object sender, FrameEventArgs e )
        {
            GamerServicesDispatcher.Update();
        }
    }
}