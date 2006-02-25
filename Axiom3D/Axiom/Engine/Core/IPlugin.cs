// arilou

using System;
using System.Collections.Generic;
using System.Text;

namespace Axiom
{
    /// <summary>
    ///		Any class that wants to entend the functionality of the engine can implement this
    ///		interface.  Classes implementing this interface will automatically be loaded and
    ///		started by the engine during the initialization phase.  Examples of plugins would be
    ///		RenderSystems, SceneManagers, etc, which can register themself using the 
    ///		singleton instance of the Engine class.
    /// </summary>
    public interface IPlugin
    {
        void Start();
        void Stop();
    }
}
