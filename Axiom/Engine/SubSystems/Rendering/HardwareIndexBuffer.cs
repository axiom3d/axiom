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
using System.Runtime.InteropServices;

namespace Axiom.SubSystems.Rendering {
    /// <summary>
    /// Summary description for IIndexBuffer.
    /// </summary>
    public abstract class HardwareIndexBuffer : HardwareBuffer {
        #region Member variables

        protected IndexType type;
        protected int numIndices;

        #endregion

        #region Constructors

        public HardwareIndexBuffer(IndexType type, int numIndices, BufferUsage usage, bool useSystemMemory, bool useShadowBuffer) 
            : base(usage, useSystemMemory, useShadowBuffer) {
            this.type = type;
            this.numIndices = numIndices;

            // calc the index buffer size
            sizeInBytes = numIndices;

            if(type == IndexType.Size32)
                sizeInBytes *= Marshal.SizeOf(typeof(int));
            else
                sizeInBytes *= Marshal.SizeOf(typeof(short));

            // create a shadow buffer if required
            if(useShadowBuffer) {
                shadowBuffer = new SoftwareIndexBuffer(type, numIndices, BufferUsage.Dynamic);
            }
        }

        #endregion

        #region Properties

        /// <summary>
        ///		Gets an enum specifying whether this index buffer is 16 or 32 bit elements.
        /// </summary>
        public IndexType Type {
            get { return type; }
        }

        #endregion
    }
}
