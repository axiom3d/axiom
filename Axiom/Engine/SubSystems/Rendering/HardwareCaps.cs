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

namespace Axiom.SubSystems.Rendering {
    /// <summary>
    /// 	This serves as a way to query information about the capabilies of a 3D API and the
    /// 	users hardware configuration.  A RenderSystem should create and initialize an instance
    /// 	of this class during startup so that it will be available for use ASAP for checking caps.
    /// </summary>
    public class HardwareCaps {
        #region Member variables
		
        private Capabilities caps;
        private int numTextureUnits;
        private int stencilBufferBits;
        private int numIndexedMatrices;
        private int maxLights;
        private string vendor;

        #endregion
		
        #region Constructors
		
        public HardwareCaps() {
        }
		
        #endregion
		
        #region Properties

        /// <summary>
        ///		Maximum number of lights supported in the scene at once by the API and/or hardware.
        /// </summary>
        public int MaxLights {
            get { return maxLights; }
            set { maxLights = value; }
        }

        /// <summary>
        ///		Reports on the number of texture units the graphics hardware has available.
        /// </summary>
        public int NumTextureUnits {
            get { return numTextureUnits; }
            set { numTextureUnits = value; }
        }

        /// <summary>
        ///		Number of stencil buffer bits suppported by the hardware.
        /// </summary>
        public int StencilBufferBits {
            get { return stencilBufferBits; }
            set { stencilBufferBits = value; }
        }

        /// <summary>
        ///		Gets/Sets the vendor of the current video card.
        /// </summary>
        public string Vendor {
            get { return vendor; }
            set { vendor = value; }
        }

        #endregion

        #region Methods

        public bool CheckCap(Capabilities cap) {
            return (caps & cap) > 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cap"></param>
        public void SetCap(Capabilities cap) {
            caps |= cap;

            // write out to the debug console
            System.Diagnostics.Debug.WriteLine(String.Format("Hardware Cap: {0}", cap.ToString()));
        }

        #endregion

    }
}
