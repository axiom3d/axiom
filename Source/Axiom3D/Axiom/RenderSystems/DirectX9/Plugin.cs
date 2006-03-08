#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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

#region Namespace Declarations

using System;

using Axiom;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9
{
    /// <summary>
    /// Summary description for Plugin.
    /// </summary>
    [PluginMetadata(Name = "DirectX",
        Description="Axiom DirectX 9 Renderer", Subsystem=typeof(RenderSystemManager))]
    public sealed class Plugin : IPlugin
    {
        #region Fields

        /// <summary>
        ///     Factory for HLSL programs.
        /// </summary>
        private HLSL.HLSLProgramFactory factory = new HLSL.HLSLProgramFactory();
        /// <summary>
        ///     Reference to the render system instance.
        /// </summary>
        private RenderSystem renderSystem = new D3D9RenderSystem();

        #endregion Fields

        #region Implementation of IPlugin

        public void Start()
        {
            RenderSystemNamespaceExtender renderNamespace = (RenderSystemNamespaceExtender)
                Vfs.Instance["/Axiom/RenderSystems/"];

            // add an instance of this plugin to the list of available RenderSystems
            renderNamespace.RegisterRenderSystem("Direct3D9", renderSystem);

            // register the HLSL program manager
            HighLevelGpuProgramManager.Instance.AddFactory( factory );

            _isStarted = true;
        }

        public void Stop()
        {
            // nothiing at the moment
            renderSystem.Shutdown();
        }

        private bool _isStarted = false;

        public bool IsStarted
        {
            get { return _isStarted; }
        }

        #endregion
    }
}
