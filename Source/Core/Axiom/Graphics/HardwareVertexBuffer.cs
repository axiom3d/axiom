#region LGPL License

/*
Axiom Graphics Engine Library
Copyright � 2003-2011 Axiom Project Team

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
using Axiom.Core;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
    /// <summary>
    ///		Describes the graphics API independent functionality required by a hardware
    ///		vertex buffer.  
    /// </summary>
    /// <remarks>
    ///		
    /// </remarks>
    public abstract class HardwareVertexBuffer : HardwareBuffer
    {
        #region Member variables

        protected HardwareBufferManagerBase Manager;
        protected int numVertices;
        protected VertexDeclaration vertexDeclaration;
        protected int useCount;

        #endregion

        #region Construction and destruction

        [OgreVersion(1, 7, 2)]
        public HardwareVertexBuffer(HardwareBufferManagerBase manager, VertexDeclaration vertexDeclaration, int numVertices,
                                     BufferUsage usage, bool useSystemMemory, bool useShadowBuffer)
            : base(usage, useSystemMemory, useShadowBuffer)
        {
            this.vertexDeclaration = vertexDeclaration;
            this.numVertices = numVertices;
            this.Manager = manager;

            // calculate the size in bytes of this buffer
            sizeInBytes = vertexDeclaration.GetVertexSize() * numVertices;

            // create a shadow buffer if required
            if (useShadowBuffer)
            {
                shadowBuffer = new DefaultHardwareVertexBuffer(this.Manager, vertexDeclaration, numVertices, BufferUsage.Dynamic);
            }

            this.useCount = 0;
        }

        [OgreVersion(1, 7, 2, "~HardwareVertexBuffer")]
        protected override void dispose(bool disposeManagedResources)
        {
            if (!IsDisposed)
            {
                if (disposeManagedResources)
                {
                    if (this.Manager != null)
                    {
                        this.Manager.NotifyVertexBufferDestroyed(this);
                    }

                    shadowBuffer.SafeDispose();
                    shadowBuffer = null;
                }
            }

            base.dispose(disposeManagedResources);
        }

        #endregion Construction and destruction

        #region Properties

        public VertexDeclaration VertexDeclaration
        {
            get
            {
                return this.vertexDeclaration;
            }
        }

        public int VertexSize
        {
            get
            {
                return this.vertexDeclaration.GetVertexSize();
            }
        }

        public int VertexCount
        {
            get
            {
                return this.numVertices;
            }
        }

        public int UseCount
        {
            get
            {
                return this.useCount;
            }
        }

        #region IsInstanceData

        [OgreVersion(1, 7, 2790)] protected bool isInstanceData;

        [OgreVersion(1, 7, 2790)]
        public bool IsInstanceData
        {
            get
            {
                return this.isInstanceData;
            }
            set
            {
                if (value && !CheckIfVertexInstanceDataIsSupported())
                {
                    throw new AxiomException("vertex instance data is not supported by the render system.");
                }
                // else
                {
                    this.isInstanceData = value;
                }
            }
        }

        #endregion

        #region InstanceDataStepRate

        [OgreVersion(1, 7, 2790)] protected int instanceDataStepRate;

        [OgreVersion(1, 7, 2790)]
        public int InstanceDataStepRate
        {
            get
            {
                return this.instanceDataStepRate;
            }
            set
            {
                if (value > 0)
                {
                    this.instanceDataStepRate = value;
                }
                else
                {
                    throw new AxiomException("Instance data step rate must be bigger then 0.");
                }
            }
        }

        #endregion

        #endregion

        #region Methods

        /// <summary>
        /// Checks if vertex instance data is supported by the render system
        /// </summary>
        /// <returns></returns>
        protected virtual bool CheckIfVertexInstanceDataIsSupported()
        {
            // Use the current render system
            var rs = Root.Instance.RenderSystem;

            // Check if the supported  
            throw new NotImplementedException();
            //return rs.Capabilities.HasCapability(Capabilities.VertexBufferInstanceData);
        }

        #endregion
    }
}