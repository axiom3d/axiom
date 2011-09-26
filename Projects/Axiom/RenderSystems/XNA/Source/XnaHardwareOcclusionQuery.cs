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

using Axiom.Graphics;
using Microsoft.Xna.Framework.Graphics;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.Xna
{
    /// <summary>
    ///		Xna implementation of a hardware occlusion query.
    /// </summary>
    // Original Author: Lee Sandberg
    public class XnaHardwareOcclusionQuery : HardwareOcclusionQuery
    {
        #region Fields

        /// <summary>
        ///		Reference to the current Xna device object.
        /// </summary>
        private readonly GraphicsDevice device;

        /// <summary>
        ///		Reference to the query object being used.
        /// </summary>
        private readonly OcclusionQuery oQuery;

        #endregion Fields

        #region Constructor

        /// <summary>
        ///		Default constructor.
        /// </summary>
        /// <param name="device">Reference to a Direct3D device.</param>
        public XnaHardwareOcclusionQuery( GraphicsDevice device )
        {
            this.device = device;
            oQuery = new OcclusionQuery( device );
        }

        #endregion Constructor

        #region HardwareOcclusionQuery Members

        /// <summary>
        /// Starts the hardware occlusion query
        /// </summary>
        public override void Begin()
        {
#if !SILVERLIGHT
            // proceed if supported, or silently fail otherwise
            if ( device.GraphicsProfile == GraphicsProfile.HiDef )
            {
                oQuery.Begin();
            }
#endif
        }

        /// <summary>
        /// Pulls the hardware occlusion query.
        /// </summary>
        /// <remarks>
        /// Waits until the query result is available; use <see cref="HardwareOcclusionQuery.IsStillOutstanding"/>
        /// if just want to test if the result is available.
        /// </remarks>
        /// <returns>the resulting number of fragments.</returns>
        public override int PullResults()
        {
            // default to returning a high count.  will be set otherwise if the query runs
            LastFragmentCount = 100000;

#if !SILVERLIGHT
            if ( device.GraphicsProfile == GraphicsProfile.HiDef && oQuery.IsComplete )
            {
                LastFragmentCount = oQuery.PixelCount;
            }
#endif

            return LastFragmentCount;
        }

        /// <summary>
        /// Ends the hardware occlusion test
        /// </summary>
        public override void End()
        {
#if !SILVERLIGHT
            // proceed if supported, or silently fail otherwise
            if ( device.GraphicsProfile == GraphicsProfile.HiDef )
            {
                oQuery.End();
            }
#endif
        }

        /// <summary>
        /// Lets you know when query is done, or still be processed by the Hardware
        /// </summary>
        /// <returns>true if query isn't finished.</returns>
        public override bool IsStillOutstanding()
        {
            return !oQuery.IsComplete;
        }

        #endregion
    }
}