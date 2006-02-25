using System;
using System.Collections.Generic;
using System.Text;

namespace Axiom
{
    /// <summary>
    /// Represents a plugin, only one instance of which can exist in an application
    /// </summary>
    /// <remarks>
    /// Examples of such plugin are Platform Manager and Render System. This plugin
    /// actually presents an implementation of a certain interface (IPlatformManager),
    /// or type (RenderSystem)
    /// </remarks>
    public interface ISingletonPlugin : IPlugin
    {
        /// <summary>
        /// Returns the reference to the subsystem this singleton plugin 
        /// implements
        /// </summary>
        /// <returns></returns>
        object GetSubsystemImplementation();
    }
}
