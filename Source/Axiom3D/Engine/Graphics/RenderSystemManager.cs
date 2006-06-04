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

#endregion Namespace Declarations
			
namespace Axiom
{
    /// <summary>
    /// Manages available render systems
    /// </summary>
    [Subsystem("RenderSystemManager")]
    public class RenderSystemManager : ISubsystem
    {
        #region protected members
        /// <summary>
        /// Namespace extender
        /// </summary>
        protected RenderSystemNamespaceExtender renderNamespace = null;
        #endregion

        #region initialization
        /// <summary>
        /// Initalizes the RenderSystemManager
        /// </summary>
        /// <returns></returns>
        public bool Initialize()
        {
            // prevent double initialization
            if (_isInitialized)
                return true;

            // register the render system namespace
            renderNamespace = new RenderSystemNamespaceExtender();
            Vfs.Instance.RegisterNamespace(renderNamespace);

            // request RenderSystemManager plugins...
            List<PluginMetadataAttribute> plugins =
                PluginManager.Instance.RequestSubsystemPlugins(this);

            // ... and initialize them
            foreach (PluginMetadataAttribute pluginData in plugins)
            {
                IPlugin plugin = PluginManager.Instance.GetPlugin(pluginData.Name);
                if (!plugin.IsStarted)
                    plugin.Start();
            }

            _isInitialized = true;

            return true;
        }

        private bool _isInitialized = false;
        /// <summary>
        /// Checks whether the render system manager is already initialized
        /// </summary>
        public bool IsInitialized
        {
            get
            {
                return _isInitialized;
            }
        }
        #endregion

        #region Singleton implementation
        private static RenderSystemManager instance = null;

        internal RenderSystemManager()
        {
            if (instance == null)
            {
                instance = this;
                instance.Initialize();
            }
        }

        /// <summary>
        ///     Gets the singleton instance of this class.
        /// </summary>
        public static RenderSystemManager Instance
        {
            get
            {
                return instance;
            }
        }
        #endregion

    }
}
