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
using System.Collections;
using System.Diagnostics;
using Axiom.MathLib;

namespace Axiom.SubSystems.Rendering {
    /// <summary>
    ///		Records the state of all the vertex buffer bindings required to provide a vertex declaration
    ///		with the input data it needs for the vertex elements.
    ///	 </summary>
    ///	 <remarks>
    ///		Why do we have this binding list rather than just have VertexElement referring to the
    ///		vertex buffers direct? Well, in the underlying APIs, binding the vertex buffers to an
    ///		index (or 'stream') is the way that vertex data is linked, so this structure better
    ///		reflects the realities of that. In addition, by separating the vertex declaration from
    ///		the list of vertex buffer bindings, it becomes possible to reuse bindings between declarations
    ///		and vice versa, giving opportunities to reduce the state changes required to perform rendering.
    /// </remarks>
    public class VertexBufferBinding {
        #region Member variables
		
        protected Hashtable bindingMap = new Hashtable();
        protected ushort highIndex;
		
        #endregion

        #region Methods
		
        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <param name="buffer"></param>
        /// DOC
        public virtual void SetBinding(ushort index, HardwareVertexBuffer buffer) {
            bindingMap[index] = buffer;
            highIndex = (ushort)MathUtil.Max(highIndex, index);
        }

        /// <summary>
        /// 
        /// </summary>
        /// DOC
        public virtual void UnsetBinding(ushort index) {
            Debug.Assert(bindingMap.ContainsKey(index), "Cannot find buffer for index" + index);

            bindingMap.Remove(index);

        }

        /// <summary>
        /// 
        /// </summary>
        /// DOC
        public virtual void UnsetAllBindings() {
            bindingMap.Clear();
        }

        public virtual HardwareVertexBuffer GetBuffer(ushort index) {
            Debug.Assert(bindingMap.ContainsKey(index), "No buffer is bound to index " + index);

            return (HardwareVertexBuffer)bindingMap[index];
        }

        #endregion
		
        #region Properties
		
        /// <summary>
        /// 
        /// </summary>
        /// DOC
        /// TODO: Change this to strongly typed later on
        public virtual IDictionary Bindings {
            get { return bindingMap; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// DOC
        public virtual ushort NextIndex {
            get { return highIndex; }
        }

        #endregion
    }
}
