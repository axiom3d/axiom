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

namespace Axiom.Graphics {
    /// <summary>
    /// 	Summary description for VertexDeclaration.
    /// </summary>
    /// DOC
    public class VertexDeclaration : ICloneable {
        #region Member variables

        protected VertexElementList elements = new VertexElementList();

        #endregion

        #region Methods
		
        /// <summary>
        /// 
        /// </summary>
        /// DOC
        public virtual void AddElement(VertexElement element) {
            elements.Add(element);
        }

        public VertexElement FindElementBySemantic(VertexElementSemantic semantic) {
            // call overload with a default of index 0
            return FindElementBySemantic(semantic, 0);
        }

        /// <summary>
        /// 
        /// </summary>
        /// DOC
        public virtual VertexElement FindElementBySemantic(VertexElementSemantic semantic, short index) {
            for(int i = 0; i < elements.Count; i++) {
                VertexElement element = (VertexElement)elements[i];

                // do they match?
                if(element.Semantic == semantic && element.Index == index)
                    return element;
            }

            // not found
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// DOC
        public virtual VertexElementList FindElementBySource(ushort source) {
            VertexElementList elements = new VertexElementList();

            for(int i = 0; i < elements.Count; i++) {
                VertexElement element = (VertexElement)elements[i];

                // do they match?
                if(element.Source == source)
                    elements.Add(element);
            }

            // return the list
            return elements;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// DOC
        public virtual int GetVertexSize(short source) {
            int size = 0;

            for(int i = 0; i < elements.Count; i++) {
                VertexElement element = (VertexElement)elements[i];

                // do they match?
                if(element.Source == source)
                    size += element.Size;
            }

            // return the size
            return size;
        }

        public static bool operator == (VertexDeclaration left, VertexDeclaration right) {
            // if element lists are different sizes, they can't be equal
            if(left.elements.Count != right.elements.Count)
                return false;

            for(int i = 0; i < right.elements.Count; i++) {
                VertexDeclaration a = (VertexDeclaration)left.elements[i];
                VertexDeclaration b = (VertexDeclaration)right.elements[i];

                // if they are not equal, this declaration differs
                if(!(a == b))
                    return false;
            }

            // if we got thise far, they are equal
            return true;
        }

        public static bool operator != (VertexDeclaration left, VertexDeclaration right) {
            return !(left == right);
        }

        #endregion
		
        #region Properties
		
        /// <summary>
        /// 
        /// </summary>
        /// DOC
        public IList Elements {
            get { return elements; }
        }

        #endregion

        #region Object overloads

        /// <summary>
        ///    Override to determine equality between 2 VertexDeclaration objects,
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj) {
            VertexDeclaration decl = obj as VertexDeclaration;

            return (decl == this);
        }

        /// <summary>
        ///    Override GetHashCode.
        /// </summary>
        /// <remarks>
        ///    Done mainly to quash warnings, no real need for it.
        /// </remarks>
        /// <returns></returns>
        // TODO: Does this need to be implemented, dont think we are stuffing these into hashtables.
        public override int GetHashCode() {
            return 0;
        }

        #endregion Object overloads

        #region ICloneable Members

        public object Clone() {
            VertexDeclaration clone = HardwareBufferManager.Instance.CreateVertexDeclaration();

            for(int i = 0; i < elements.Count; i++) {
                VertexElement element = (VertexElement)elements[i];
                clone.AddElement((VertexElement)element.Clone());
            }

            return clone;
        }

        #endregion
    }
}
