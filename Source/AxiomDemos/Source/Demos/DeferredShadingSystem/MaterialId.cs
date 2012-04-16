using System;
using System.Collections.Generic;
using System.Text;

namespace Axiom.Demos.DeferredShadingSystem
{
    [Flags()]
    enum MaterialId
    {
        /// <summary>Render as fullscreen quad</summary>
        Quad        = 0x1,
        /// <summary>Render attenuated</summary>
        Attenuated  = 0x2,
        /// <summary>specular component is calculated</summary>
        Specular    = 0x4
    }
}
