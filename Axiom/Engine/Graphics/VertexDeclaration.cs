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
    /// 	This class declares the format of a set of vertex inputs, which
    /// 	can be issued to the rendering API through a <see cref="RenderOperation"/>. 
    /// </summary>
    public class VertexDeclaration : ICloneable {
        #region Fields

        /// <summary>
        ///     List of elements that make up this declaration.
        /// </summary>
        protected VertexElementList elements = new VertexElementList();

        #endregion Fields

        #region Methods
		
        /// <summary>
        ///     Adds a new <see cref="VertexElement"/> to this declaration.
        /// </summary>
        public virtual void AddElement(VertexElement element) {
            elements.Add(element);
        }

        /// <summary>
        ///     Adds a new VertexElement to this declaration.
        /// </summary>
        /// <remarks>
        ///     This method adds a single element (positions, normals etc) to the
        ///     vertex declaration. <b>Please read the information in <see cref="VertexDeclaration"/> about
        ///     the importance of ordering and structure for compatibility with older D3D drivers</b>.
        /// </remarks>
        /// <param name="source">
        ///     The binding index of HardwareVertexBuffer which will provide the source for this element.
        /// </param>
        /// <param name="offset">The offset in bytes where this element is located in the buffer.</param>
        /// <param name="type">The data format of the element (3 floats, a color etc).</param>
        /// <param name="semantic">The meaning of the data (position, normal, diffuse color etc).</param>
        public virtual VertexElement AddElement(short source, int offset, VertexElementType type, VertexElementSemantic semantic) {
            return AddElement(source, offset, type, semantic, 0);
        }

        /// <summary>
        ///     Adds a new VertexElement to this declaration.
        /// </summary>
        /// <remarks>
        ///     This method adds a single element (positions, normals etc) to the
        ///     vertex declaration. <b>Please read the information in <see cref="VertexDeclaration"/> about
        ///     the importance of ordering and structure for compatibility with older D3D drivers</b>.
        /// </remarks>
        /// <param name="source">
        ///     The binding index of HardwareVertexBuffer which will provide the source for this element.
        /// </param>
        /// <param name="offset">The offset in bytes where this element is located in the buffer.</param>
        /// <param name="type">The data format of the element (3 floats, a color etc).</param>
        /// <param name="semantic">The meaning of the data (position, normal, diffuse color etc).</param>
        /// <param name="index">Optional index for multi-input elements like texture coordinates.</param>
        public virtual VertexElement AddElement(short source, int offset, VertexElementType type, VertexElementSemantic semantic, int index) {
            VertexElement element = new VertexElement(source, offset, type, semantic, index);
            elements.Add(element);
            return element;
        }

        /// <summary>
        ///     Finds a <see cref="VertexElement"/> with the given semantic, and index if there is more than 
        ///     one element with the same semantic. 
        /// </summary>
        /// <param name="semantic">Semantic to search for.</param>
        /// <returns>If the element is not found, this method returns null.</returns>
        public VertexElement FindElementBySemantic(VertexElementSemantic semantic) {
            // call overload with a default of index 0
            return FindElementBySemantic(semantic, 0);
        }

        /// <summary>
        ///     Finds a <see cref="VertexElement"/> with the given semantic, and index if there is more than 
        ///     one element with the same semantic. 
        /// </summary>
        /// <param name="semantic">Semantic to search for.</param>
        /// <param name="index">Index of item to looks for using the supplied semantic (applicable to tex coords and colors).</param>
        /// <returns>If the element is not found, this method returns null.</returns>
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
        ///     Gets a list of elements which use a given source.
        /// </summary>
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
        ///     Gets the vertex size defined by this declaration for a given source.
        /// </summary>
        /// <param name="source">The buffer binding index for which to get the vertex size.</param>
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

        /// <summary>
        ///     Tests equality of 2 <see cref="VertexElement"/> objects.
        /// </summary>
        /// <param name="left">A <see cref="VertexElement"/></param>
        /// <param name="right">A <see cref="VertexElement"/></param>
        /// <returns>true if equal, false otherwise.</returns>
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

        /// <summary>
        ///     Tests in-equality of 2 <see cref="VertexElement"/> objects.
        /// </summary>
        /// <param name="left">A <see cref="VertexElement"/></param>
        /// <param name="right">A <see cref="VertexElement"/></param>
        /// <returns>true if not equal, false otherwise.</returns>
        public static bool operator != (VertexDeclaration left, VertexDeclaration right) {
            return !(left == right);
        }

        #endregion
		
        #region Properties
		
        /// <summary>
        ///     Gets a <b>read-only</b> list of elements.
        /// </summary>
        public IList Elements {
            get { 
                return VertexElementList.ReadOnly(elements);
            }
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

        /// <summary>
        ///     Clonses this declaration, including a copy of all <see cref="VertexElement"/> objects this declaration holds.
        /// </summary>
        /// <returns></returns>
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
