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
    /// 	Summary description for VertexIndexData.
    /// </summary>
    public class VertexData : ICloneable {
        #region Member variables
		
        public VertexDeclaration vertexDeclaration;
        public VertexBufferBinding vertexBufferBinding;
        public int vertexStart;
        public int vertexCount;

        #endregion

        public VertexData() {
            vertexDeclaration = HardwareBufferManager.Instance.CreateVertexDeclaration();
            vertexBufferBinding = HardwareBufferManager.Instance.CreateVertexBufferBinding();
        }

        #region ICloneable Members

        public object Clone() {
            // TODO:  Add VertexData.Clone implementation
            return null;
        }

		/// <summary>
		///		
		/// </summary>
		/// <param name="copyData"></param>
		/// <returns></returns>
		public VertexData Clone(bool copyData) {
			VertexData dest = new VertexData();

			// Copy vertex buffers in turn
			IEnumerator bindings = vertexBufferBinding.Bindings;

			while(bindings.MoveNext()) {
				DictionaryEntry entry = (DictionaryEntry)bindings.Current;

				short source = (short)entry.Key;
				HardwareVertexBuffer srcbuf = (HardwareVertexBuffer)entry.Value;
				HardwareVertexBuffer dstBuf;

				if (copyData) {
					// create new buffer with the same settings
					dstBuf = 
						HardwareBufferManager.Instance.CreateVertexBuffer(
							srcbuf.VertexSize, srcbuf.VertexCount, srcbuf.Usage,
							srcbuf.IsSystemMemory);

					// copy data
					dstBuf.CopyData(srcbuf, 0, 0, srcbuf.Size, true);
				}
				else {
					// don't copy, point at existing buffer
					dstBuf = srcbuf;
				}

				// Copy binding
				dest.vertexBufferBinding.SetBinding(source, dstBuf);
			}

			// Basic vertex info
			dest.vertexStart = this.vertexStart;
			dest.vertexCount = this.vertexCount;

			// Copy elements
			for (int i = 0; i < vertexDeclaration.ElementCount; i++) {
				VertexElement element = vertexDeclaration.GetElement(i);

				dest.vertexDeclaration.AddElement(
					element.Source,
					element.Offset,
					element.Type,
					element.Semantic,
					element.Index);
			}

			return dest;
		}

        #endregion
    }

	/// <summary>
    /// 	Summary description for VertexIndexData.
    /// </summary>
    public class IndexData : ICloneable {
        #region Member variables

        public HardwareIndexBuffer indexBuffer;
        public int indexStart;
        public int indexCount;
		
        #endregion

        #region ICloneable Members

        public object Clone() {
            // TODO:  Add IndexData.Clone implementation
            return null;
        }

        #endregion
    }
}
