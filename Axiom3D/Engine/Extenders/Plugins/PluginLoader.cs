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

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Axiom
{
    /// <summary>
    /// Default plugin loader
    /// </summary>
    class PluginLoader : IPluginLoader
    {
        /// <summary>
        /// Creates an instance of the specified plugin type
        /// </summary>
        /// <param name="pluginTypeName">qualified type name of the plugin type</param>
        /// <returns><see cref="IPlugin"/>-compatible object instance 
        /// -OR-
        /// null if the type was not found</returns>
        protected virtual IPlugin createPluginInstance(string pluginTypeName)
        {
            Type pluginType = Type.GetType(pluginTypeName, false);

            if (pluginType != null)
                return createPluginInstance(pluginType);
            else
                return null;
        }

        /// <summary>
        /// Creates an instance of the specified plugin type
        /// </summary>
        /// <param name="pluginType">plugin type</param>
        /// <returns><see cref="IPlugin"/>-compatible object instance 
        /// -OR-
        /// null if the type was not found</returns>
        protected virtual IPlugin createPluginInstance(Type pluginType)
        {
            if (pluginType.GetInterface("IPlugin") == null)
                Trace.Fail("Plugin " + pluginType.FullName +
                    "does not implement IPlugin");
            else
                return (IPlugin)Activator.CreateInstance(pluginType);

            return null;
        }

        public virtual IPlugin LoadPlugin(PluginMetadataAttribute metadata)
        {
            IPlugin plugin = createPluginInstance(metadata.TypeName);
            return plugin;
        }
    }

}
