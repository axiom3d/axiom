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
using System.Configuration;
using System.Xml;

namespace Axiom
{
    /// <summary>
    /// The plugin configuration handler
    /// </summary>
    /// <remarks>
    /// <see cref="PluginManager"/> can be configured using the "plugins" configuration
    /// section. Example:
    /// <code>
    /// <![CDATA[
    /// <plugins folders="plugins">
    ///     <plugin type="/Axiom/RenderSystems/DirectX" />
    /// </plugins>
    /// ]]>
    /// </code>
    /// In this example we specify plugin root folder and one plugin to load.
    /// Plugin type must be specified by qualified type names. If no plugin types are
    /// specified, all assemblies are traversed in the specified plugin folder. 
    /// If the folder is not specified, current folder is assumed. If the section is not
    /// present in the app.config file, the behavior is the same.
    /// </remarks>
    public class PluginConfigurationSectionHandler : IConfigurationSectionHandler
    {
        object IConfigurationSectionHandler.Create(object parent, object configContext, System.Xml.XmlNode section)
        {
            PluginManagerConfiguration config =
                new PluginManagerConfiguration(section.Attributes["folder"] == null
                ? "." : section.Attributes["folder"].Value);

            // grab the plugin nodes
            XmlNodeList pluginNodes = section.SelectNodes("plugin");

            // loop through each plugin node and load the plugins
            foreach (XmlNode pluginNode in pluginNodes)
                config.Plugins.Add(pluginNode.Attributes["type"].Value);

            return config;
        }
    }
}
