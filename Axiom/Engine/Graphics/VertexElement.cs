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

namespace Axiom.Graphics {
    /// <summary>
    /// 	Summary description for VertexElement.
    /// </summary>
    /// DOC
    public class VertexElement : ICloneable {
        #region Member variables
		
        protected ushort source;
        protected int offset;
        protected VertexElementType type;
        protected VertexElementSemantic semantic;
        protected ushort index;

        #endregion
		
        #region Constructors
		
        public VertexElement(ushort source, int offset, VertexElementType type, VertexElementSemantic semantic) {
            this.source = source;
            this.offset = offset;
            this.type = type;
            this.semantic = semantic;
            this.index = 0; 
        }

        public VertexElement(ushort source, int offset, VertexElementType type, VertexElementSemantic semantic, ushort index) {
            this.source = source;
            this.offset = offset;
            this.type = type;
            this.semantic = semantic;
            this.index = index;			
        }
		
        #endregion
		
        #region Methods
		
        /// <summary>
        /// 
        /// </summary>
        /// DOC
        public static int GetTypeSize(VertexElementType type) {

            switch(type) {
                case VertexElementType.Color:
                    return Marshal.SizeOf(typeof(int));

                case VertexElementType.Float1:
                    return Marshal.SizeOf(typeof(float));						

                case VertexElementType.Float2:
                    return Marshal.SizeOf(typeof(float)) * 2;

                case VertexElementType.Float3:
                    return Marshal.SizeOf(typeof(float)) * 3;

                case VertexElementType.Float4:
                    return Marshal.SizeOf(typeof(float)) * 4;

                case VertexElementType.Short1:
                    return Marshal.SizeOf(typeof(short));

                case VertexElementType.Short2:
                    return Marshal.SizeOf(typeof(short)) * 2;

                case VertexElementType.Short3:
                    return Marshal.SizeOf(typeof(short)) * 3;

                case VertexElementType.Short4:
                    return Marshal.SizeOf(typeof(short)) * 4;
            } // end switch

            // keep the compiler happy
            return 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// DOC
        public static int GetTypeCount(VertexElementType type) {
            switch(type) {
                case VertexElementType.Color:
                    return 1;

                case VertexElementType.Float1:
                    return 1;						

                case VertexElementType.Float2:
                    return 2;

                case VertexElementType.Float3:
                    return 3;

                case VertexElementType.Float4:
                    return 4;

                case VertexElementType.Short1:
                    return 1;

                case VertexElementType.Short2:
                    return 2;

                case VertexElementType.Short3:
                    return 3;

                case VertexElementType.Short4:
                    return 4;
            } // end switch			

            // keep the compiler happy
            return 0;
        }

        /// <summary>
        ///		Returns proper enum for a base type multiplied by a value.  This is helpful
        ///		when working with tex coords especially since you might not know the number
        ///		of texture dimensions at runtime, and when creating the VertexBuffer you will
        ///		have to get a VertexElementType based on that amount to creating the VertexElement.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static VertexElementType MultiplyTypeCount(VertexElementType type, int count) {
            switch(type) {
                case VertexElementType.Float1:
                    return (VertexElementType)Enum.Parse(type.GetType(), "Float" + count, true);

                case VertexElementType.Short1:
                    return (VertexElementType)Enum.Parse(type.GetType(), "Short" + count, true);
            }

            throw new Exception("Cannot multiply base vertex element type: " + type.ToString());
        }

        #endregion
		
        #region Properties

        /// <summary>
        /// 
        /// </summary>
        /// DOC
        public ushort Source {
            get { return source; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// DOC
        public int Offset {
            get { return offset; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// DOC
        public VertexElementType Type {
            get { return type; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// DOC
        public VertexElementSemantic Semantic {
            get { return semantic; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// DOC
        public ushort Index {
            get { return index; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// DOC
        public int Size {
            get { return GetTypeSize(type); }
        }

        #endregion

        #region ICloneable Members

        /// <summary>
        ///     Simple memberwise clone since all local fields are value types.
        /// </summary>
        /// <returns></returns>
        public object Clone() {
            return this.MemberwiseClone();
        }

        #endregion
    }
}
