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

using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.Controllers.Canned
{
    /// <summary>
    ///     Predefined controller function for dealing with animation.
    /// </summary>
    public class AnimationControllerFunction : IControllerFunction<Real>
    {
        #region Fields

        /// <summary>
        ///     The amount of time in seconds it takes to loop through the whole animation sequence.
        /// </summary>
        protected Real sequenceTime;

        /// <summary>
        ///     The offset in seconds at which to start (default is start at 0).
        /// </summary>
        protected Real time;

        #endregion Fields

        #region Constructor

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="sequenceTime">The amount of time in seconds it takes to loop through the whole animation sequence.</param>
        public AnimationControllerFunction(Real sequenceTime)
            : this(sequenceTime, 0.0f)
        {
        }

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="sequenceTime">The amount of time in seconds it takes to loop through the whole animation sequence.</param>
        /// <param name="timeOffset">The offset in seconds at which to start.</param>
        public AnimationControllerFunction(Real sequenceTime, Real timeOffset)
        {
            this.sequenceTime = sequenceTime;
            this.time = timeOffset;
        }

        #endregion

        #region ControllerFunction Members

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourceValue"></param>
        /// <returns></returns>
        public Real Execute(Real sourceValue)
        {
            // assuming source if the time since the last update
            this.time += sourceValue;

            // wrap
            while (this.time >= this.sequenceTime)
            {
                this.time -= this.sequenceTime;
            }

            // return parametric
            return this.time / this.sequenceTime;
        }

        #endregion ControllerFunction Members
    }
}