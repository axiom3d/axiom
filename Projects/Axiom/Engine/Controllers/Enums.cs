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
#endregion

#region SVN Version Information
// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;

using Axiom.Scripting;

#endregion Namespace Declarations

namespace Axiom.Controllers
{
	/// <summary>
	/// Enumerates the wave types usable with the engine.
	/// </summary>
    public enum WaveformType
    {
        /// <summary>Standard sine wave which smoothly changes from low to high and back again.</summary>
        [ScriptEnum("sine")]
        Sine,
        /// <summary>An angular wave with a constant increase / decrease speed with pointed peaks.</summary>
        [ScriptEnum("triangle")]
        Triangle,
        /// <summary>Half of the time is spent at the min, half at the max with instant transition between. </summary>
        [ScriptEnum("square")]
        Square,
        /// <summary>Gradual steady increase from min to max over the period with an instant return to min at the end. </summary>
        [ScriptEnum("sawtooth")]
        Sawtooth,
        /// <summary>Gradual steady decrease from max to min over the period, with an instant return to max at the end. </summary>
        [ScriptEnum("inverse_sawtooth")]
        InverseSawtooth,
        /// <summary>Works like Square, except the high to low transition is controlled by duty cycle. With a duty cycle of 50% (0.5) will give the same output as Square. </summary>
        [ScriptEnum("pulse_width_modulation")]
        PulseWidthModulation
    };
}
