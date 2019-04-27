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

#endregion Namespace Declarations

namespace Axiom.Core
{
    /// <summary>
    ///		Any class that wants to entend the functionality of the engine can implement this
    ///		interface.  Classes implementing this interface will automatically be loaded and
    ///		started by the engine during the initialization phase.  Examples of plugins would be
    ///		RenderSystems, SceneManagers, etc, which can register themself using the
    ///		singleton instance of the Engine class.
    /// </summary>
    /// <remarks>
    /// Axiom is very plugin-oriented and you can customize much of its behaviour
    /// by registering new plugins, dynamically if you are using dynamic linking.
    /// This class abstracts the generic interface that all plugins must support.
    /// Within the implementations of this interface, the plugin must call other
    /// Axiom classes in order to register the detailed customizations it is
    /// providing, e.g. registering a new SceneManagerFactory, a new
    /// MovableObjectFactory, or a new RenderSystem.
    /// <para />
    /// Plugins can be linked statically or dynamically. If they are linked
    /// dynamically (ie the plugin is in a DLL ), then you load the plugin by
    /// calling the Root.LoadPlugin method (or some other mechanism which leads
    /// to that call, e.g. app.config), passing the name of the DLL. Axiom
    /// will then call a global init function on that DLL, and it will be
    /// expected to register one or more Plugin implementations using
    /// Root.InstallPlugin. The procedure is very similar if you use a static
    /// linked plugin, except that you simply instantiate the Plugin implementation
    /// yourself and pass it to Root.InstallPlugin.
    /// </remarks>
    /// <note>
    /// Lifecycle of a Plugin instance is very important. The Plugin instance must
    /// remain valid until the Plugin is uninstalled. Here are the things you
    /// must bear in  mind:
    /// <ul><li>Create the Plugin anytime you like</li>
    /// <li>Call Root.InstallPlugin any time whilst Root is valid</li>
    /// <li>Call Root.UninstallPlugin if you like so long as Root is valid. However,
    /// it will be done for you when Root is destroyed, so the Plugin instance must
    /// still be valid at that point if you haven't manually uninstalled it.</li></ul>
    /// The install and uninstall methods will be called when the plugin is
    /// installed or uninstalled. The initialize and shutdown will be called when
    /// there is a system initialization or shutdown, e.g. when Root.Initialize
    /// or Root.Shutdown are called.
    /// </note>
    public interface IPlugin
    {
        // <summary>
        // Unique name for the plugin
        // </summary>
        //string Name
        //{
        //    get;
        //}

        // <summary>
        // Perform the plugin initial installation sequence.
        // </summary>
        // <remarks>
        // An implementation must be supplied for this method. It must perform
        // the startup tasks necessary to install any rendersystem customizations
        // or anything else that is not dependent on system initialization, ie
        // only dependent on the core of Axiom. It must not perform any
        // operations that would create rendersystem-specific objects at this stage,
        // that should be done in Initialize().
        // </remarks>
        //void Install();

        /// <summary>
        /// Perform any tasks the plugin needs to perform on full system initialization.
        /// </summary>
        /// <remarks>
        /// An implementation must be supplied for this method. It is called
        /// just after the system is fully initialized (either after Root.Initialize
        /// if a window is created then, or after the first window is created)
        /// and therefore all rendersystem functionality is available at this
        /// time. You can use this hook to create any resources which are
        /// dependent on a rendersystem or have rendersystem-specific implementations.
        /// </remarks>
        void Initialize();

        /// <summary>
        /// Perform any tasks the plugin needs to perform when the system is shut down.
        /// </summary>
        /// <remarks>
        /// An implementation must be supplied for this method.
        /// This method is called just before key parts of the system are unloaded,
        /// such as rendersystems being shut down. You should use this hook to free up
        /// resources and decouple custom objects from the Axiom system, whilst all the
        /// instances of other plugins (e.g. rendersystems) still exist.
        /// </remarks>
        void Shutdown();

        // <summary>
        // Perform the final plugin uninstallation sequence.
        // </summary>
        // <remarks>
        // An implementation must be supplied for this method. It must perform
        // the cleanup tasks which haven't already been performed in Shutdown()
        // (e.g. final deletion of custom instances, if you kept them around incase
        // the system was reinitialized). At this stage you cannot be sure what other
        // plugins are still loaded or active. It must therefore not perform any
        // operations that would reference any rendersystem-specific objects - those
        // should have been sorted out in the Shutdown method.
        // </remarks>
        //void Uninstall();
    }
}