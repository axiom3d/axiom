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
//     <id value="$Id:"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;

#endregion Namespace Declarations

namespace Axiom.Plugins.XnaMediaCodecs
{
    /// <summary>
    /// Summary description for Plugin.
    /// </summary>
    public sealed class Plugin : Axiom.Core.IPlugin
    {
        #region Fields

        /// <summary>
        ///     Factory for HLSL programs.
        /// </summary>
        private HLSL.HLSLProgramFactory factory = new HLSL.HLSLProgramFactory();

        /// <summary>
        ///     Reference to the render system instance.
        /// </summary>
        private Axiom.Graphics.RenderSystem renderSystem = new RenderSystem();

        #endregion Fields

        #region Implementation of IPlugin

        public void Start()
        {
            // add an instance of this plugin to the list of available RenderSystems
            Axiom.Core.Root.Instance.RenderSystems.Add( "Xna", renderSystem );

            // register the HLSL program manager
            Axiom.Graphics.HighLevelGpuProgramManager.Instance.AddFactory( factory );
        }

        public void Stop()
        {
            // nothing at the moment
            //renderSystem.Shutdown();
        }

        #endregion
    }
}
