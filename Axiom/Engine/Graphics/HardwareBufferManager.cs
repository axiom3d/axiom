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
using Axiom.Core;
using Axiom.Collections;
using Axiom.MathLib.Collections;

namespace Axiom.Graphics {
    /// <summary>
    /// 	Abstract singleton class for managing hardware buffers, a concrete instance
    ///		of this will be created by the RenderSystem.
    /// </summary>
    public abstract class HardwareBufferManager : IDisposable {
        #region Singleton implementation

		protected class BufferComparer : IComparer {
			#region IComparer Members

			public int Compare(object x, object y) {
				HardwareVertexBuffer a = x as HardwareVertexBuffer;
				HardwareVertexBuffer b = y as HardwareVertexBuffer;

				if(a.ID == b.ID) {
					return 0;
				}

				return -1;
			}
			#endregion
		}
        
        protected HardwareBufferManager() { 
            if (instance != null) {
                throw new ApplicationException("HardwareBufferManager initialized twice");
            }
            instance = this; 
            GarbageManager.Instance.Add(instance);
            freeTempVertexBufferMap = new Hashtable(null, new BufferComparer());
        }

        protected static HardwareBufferManager instance;

        public static HardwareBufferManager Instance {
            get { return instance; }
        }

        public void Dispose() {
			// TODO: Destroy all necessary objects

            if (instance == this) {
                instance = null;
            }
        }

        #endregion

        #region Fields

        /// <summary>
        ///     A list of vertex declarations created by this buffer manager.
        /// </summary>
        protected ArrayList vertexDeclarations = new ArrayList();
        /// <summary>
        ///     A list of vertex buffer bindings created by this buffer manager.
        /// </summary>
        protected ArrayList vertexBufferBindings = new ArrayList();
        /// <summary>
        ///     A list of vertex buffers created by this buffer manager.
        /// </summary>
        protected ArrayList vertexBuffers = new ArrayList();
        /// <summary>
        ///     A list of index buffers created by this buffer manager.
        /// </summary>
        protected ArrayList indexBuffers = new ArrayList();

		/// <summary>
		///		Map from original buffer to list of temporary buffers.
		/// </summary>
		protected Hashtable freeTempVertexBufferMap;
		/// <summary>
		///		List of currently licensed temp buffers.
		/// </summary>
		protected ArrayList tempVertexBufferLicenses = new ArrayList();

        #endregion Fields
		
        #region Methods

        /// <summary>
        ///		Creates a hardware vertex buffer.
        /// </summary>
        /// <remarks>
        ///		This method creates a new vertex buffer; this will act as a source of geometry
        ///		data for rendering objects. Note that because the meaning of the contents of
        ///		the vertex buffer depends on the usage, this method does not specify a
        ///		vertex format; the user of this buffer can actually insert whatever data 
        ///		they wish, in any format. However, in order to use this with a RenderOperation,
        ///		the data in this vertex buffer will have to be associated with a semantic element
        ///		of the rendering pipeline, e.g. a position, or texture coordinates. This is done 
        ///		using the VertexDeclaration class, which itself contains VertexElement structures
        ///		referring to the source data.
        ///		<p/>
        ///		Note that because vertex buffers can be shared, they are reference
        ///		counted so you do not need to worry about destroying themm this will be done
        ///		automatically.
        /// </remarks>
        /// <param name="vertexSize">The size in bytes of each vertex in this buffer; you must calculate
        ///		this based on the kind of data you expect to populate this buffer with.</param>
        /// <param name="numVerts">The number of vertices in this buffer.</param>
        /// <param name="usage">One or more members of the BufferUsage enumeration; you are
        ///		strongly advised to use StaticWriteOnly wherever possible, if you need to 
        ///		update regularly, consider WriteOnly and useShadowBuffer=true.</param>
        public abstract HardwareVertexBuffer CreateVertexBuffer(int vertexSize, int numVerts, BufferUsage usage);

        /// <summary>
        ///		Creates a hardware vertex buffer.
        /// </summary>
        /// <remarks>
        ///		This method creates a new vertex buffer; this will act as a source of geometry
        ///		data for rendering objects. Note that because the meaning of the contents of
        ///		the vertex buffer depends on the usage, this method does not specify a
        ///		vertex format; the user of this buffer can actually insert whatever data 
        ///		they wish, in any format. However, in order to use this with a RenderOperation,
        ///		the data in this vertex buffer will have to be associated with a semantic element
        ///		of the rendering pipeline, e.g. a position, or texture coordinates. This is done 
        ///		using the VertexDeclaration class, which itself contains VertexElement structures
        ///		referring to the source data.
        ///		<p/>
        ///		Note that because vertex buffers can be shared, they are reference
        ///		counted so you do not need to worry about destroying themm this will be done
        ///		automatically.
        /// </remarks>
        /// <param name="vertexSize">The size in bytes of each vertex in this buffer; you must calculate
        ///		this based on the kind of data you expect to populate this buffer with.</param>
        /// <param name="numVerts">The number of vertices in this buffer.</param>
        /// <param name="usage">One or more members of the BufferUsage enumeration; you are
        ///		strongly advised to use StaticWriteOnly wherever possible, if you need to 
        ///		update regularly, consider WriteOnly and useShadowBuffer=true.</param>
        /// <param name="useShadowBuffer"></param>
        public abstract HardwareVertexBuffer CreateVertexBuffer(int vertexSize, int numVerts, BufferUsage usage, bool useShadowBuffer);
		
        /// <summary>
        ///     Create a hardware index buffer.
        /// </summary>
        /// <param name="type">
        ///     The type in index, either 16- or 32-bit, depending on how many vertices
        ///     you need to be able to address.
        /// </param>
        /// <param name="numIndices">The number of indexes in the buffer.</param>
        /// <param name="usage">One or more members of the <see cref="BufferUsage"/> enumeration.</param>
        /// <returns></returns>
        public abstract HardwareIndexBuffer CreateIndexBuffer(IndexType type, int numIndices, BufferUsage usage);

        /// <summary>
        ///     Create a hardware index buffer.
        /// </summary>
        /// <param name="type">
        ///     The type in index, either 16- or 32-bit, depending on how many vertices
        ///     you need to be able to address.
        /// </param>
        /// <param name="numIndices">The number of indexes in the buffer.</param>
        /// <param name="usage">One or more members of the <see cref="BufferUsage"/> enumeration.</param>
        /// <param name="useShadowBuffer">
        ///     If set to true, this buffer will be 'shadowed' by one stored in 
        ///     system memory rather than GPU or AGP memory. You should set this flag if you intend 
        ///     to read data back from the index buffer, because reading data from a buffer
        ///     in the GPU or AGP memory is very expensive, and is in fact impossible if you
        ///     specify <see cref="BufferUsage.WriteOnly"/> for the main buffer. If you use this option, all 
        ///     reads and writes will be done to the shadow buffer, and the shadow buffer will
        ///     be synchronised with the real buffer at an appropriate time.
        /// </param>
        /// <returns></returns>
        public abstract HardwareIndexBuffer CreateIndexBuffer(IndexType type, int numIndices, BufferUsage usage, bool useShadowBuffer);

        /// <summary>
        ///     Creates a vertex declaration, may be overridden by certain rendering APIs.
        /// </summary>
        public virtual VertexDeclaration CreateVertexDeclaration() {
            VertexDeclaration decl = new VertexDeclaration();
            vertexDeclarations.Add(decl);
            return decl;
        }

        /// <summary>
        ///     Creates a new VertexBufferBinding.
        /// </summary>
        public virtual VertexBufferBinding CreateVertexBufferBinding() {
            VertexBufferBinding binding = new VertexBufferBinding();
            vertexBufferBindings.Add(binding);
            return binding;
        }

		/// <summary>
		///		Creates a new <see cref="VertexBufferBinding"/>.
		/// </summary>
		/// <param name="binding"></param>
		public virtual void DestroyVertexBufferBinding(VertexBufferBinding binding) {
			vertexBufferBindings.Remove(binding);
		}

		/// <summary>
		///		Destroys a vertex declaration.
		/// </summary>
		/// <remarks>
		///		Subclasses wishing to override this methods should call the base class implementation
		///		first, which removes the object the collection of created objects.
		/// </remarks>
		/// <param name="decl">VertexDeclaration object to destroy.</param>
		public virtual void DestroyVertexDeclaration(VertexDeclaration decl) {
			vertexDeclarations.Remove(decl);
		}

        /// <summary>
        ///     Allocates a copy of a given vertex buffer.
        /// </summary>
        /// <remarks>
        ///     This method allocates a temporary copy of an existing vertex buffer.
        ///     This buffer is subsequently stored and can be made available for 
        ///     other purposes later without incurring the cost of construction / 
        ///     destruction.
        /// </remarks>
        /// <param name="sourceBuffer">The source buffer to use as a copy.</param>
        /// <param name="licenseType">
        ///     The type of license required on this buffer - automatic
        ///     release causes this class to release licenses every frame so that 
        ///     they can be reallocated anew.
        /// </param>
        /// <param name="licensee">
        ///     Reference back to the class requesting the copy, which must
        ///     implement <see cref="IHardwareBufferLicense"/> in order to be notified when the license
        ///     expires.
        /// </param>
        /// <returns></returns>
        public virtual HardwareVertexBuffer AllocateVertexBufferCopy(HardwareVertexBuffer sourceBuffer,
            BufferLicenseRelease licenseType, IHardwareBufferLicensee licensee) {

            return AllocateVertexBufferCopy(sourceBuffer, licenseType, licensee, false);
        }

        /// <summary>
        ///     Allocates a copy of a given vertex buffer.
        /// </summary>
        /// <remarks>
        ///     This method allocates a temporary copy of an existing vertex buffer.
        ///     This buffer is subsequently stored and can be made available for 
        ///     other purposes later without incurring the cost of construction / 
        ///     destruction.
        /// </remarks>
        /// <param name="sourceBuffer">The source buffer to use as a copy.</param>
        /// <param name="licenseType">
        ///     The type of license required on this buffer - automatic
        ///     release causes this class to release licenses every frame so that 
        ///     they can be reallocated anew.
        /// </param>
        /// <param name="licensee">
        ///     Reference back to the class requesting the copy, which must
        ///     implement <see cref="IHardwareBufferLicense"/> in order to be notified when the license
        ///     expires.
        /// </param>
        /// <param name="copyData">If true, the current data is copied as well as the structure of the buffer.</param>
        /// <returns></returns>
        public virtual HardwareVertexBuffer AllocateVertexBufferCopy(HardwareVertexBuffer sourceBuffer,
            BufferLicenseRelease licenseType, IHardwareBufferLicensee licensee, bool copyData) {

			HardwareVertexBuffer vbuf = null;

			// Locate existing buffer copy in free list
			IList list = (IList)freeTempVertexBufferMap[sourceBuffer];

			if(list == null) {
				list = new ArrayList();
				freeTempVertexBufferMap[sourceBuffer] = list;
			}

			// Are there any free buffers?
			if(list.Count == 0) {
				// copy buffer, use shadow buffer and make dynamic
				vbuf = MakeBufferCopy(sourceBuffer, BufferUsage.DynamicWriteOnly, true);
			}
			else {
				// grab the available buffer and remove it from the free list
				int lastIndex = list.Count - 1;
				vbuf = (HardwareVertexBuffer)list[lastIndex];
				list.RemoveAt(lastIndex);
			}

			// Copy data?
			if(copyData) {
				vbuf.CopyData(sourceBuffer, 0, 0, sourceBuffer.Size, true);
			}
			// Insert copy into licensee list
			tempVertexBufferLicenses.Add(new VertexBufferLicense(sourceBuffer, licenseType, vbuf, licensee));

			return vbuf;
        }

        /// <summary>
        ///     Manually release a vertex buffer copy for others to subsequently use.
        /// </summary>
        /// <remarks>
        ///     Only required if the original call to <see cref="AllocateVertexBufferCopy"/>
        ///     included a licenseType of <see cref="BufferLicenseRelease.Manual"/>. 
        /// </remarks>
        /// <param name="bufferCopy">
        ///     The buffer copy. The caller is expected to no longer use this reference, 
        ///     since another user may well begin to modify the contents of the buffer.
        /// </param>
        public virtual void ReleaseVertexBufferCopy(HardwareVertexBuffer bufferCopy) {
			throw new NotImplementedException();
        }

        /// <summary>
        ///     Internal method for releasing all temporary buffers which have been 
        ///     allocated using <see cref="BufferLicenseRelease.Automatic"/> is called by Axiom.
        /// </summary>
        public virtual void ReleaseBufferCopies() {
			for(int i = tempVertexBufferLicenses.Count - 1; i >= 0; i--) {
				VertexBufferLicense vbl = 
					(VertexBufferLicense)tempVertexBufferLicenses[i];

				// only release licenses set to auto release
				if(vbl.licenseType == BufferLicenseRelease.Automatic) {
					IList list = (IList)freeTempVertexBufferMap[vbl.originalBuffer];

					Debug.Assert(list != null, "There is no license recorded for this buffer.");

					// push the buffer back into the free list
					list.Add(vbl.buffer);

					// remove the license for this buffer
					tempVertexBufferLicenses.RemoveAt(i);
				}
			}
        }

		/// <summary>
		///		Internal method that forces the release of copies of a given buffer.
		/// </summary>
		/// <remarks>
		///		This usually means that the buffer which the copies are based on has
		///		been changed in some fundamental way, and the owner of the original 
		///		wishes to make that known so that new copies will reflect the changes.
		/// </remarks>
		/// <param name="buffer">Buffer to release temp copies of.</param>
		internal void ForceReleaseBufferCopies(HardwareVertexBuffer buffer) {
			//throw new NotImplementedException();
		}

		/// <summary>
		///		Creates  a new buffer as a copy of the source, does not copy data.
		/// </summary>
		/// <param name="source">Source vertex buffer.</param>
		/// <param name="usage">New usage type.</param>
		/// <param name="useShadowBuffer">New shadow buffer choice.</param>
		/// <returns>A copy of the vertex buffer, but data is not copied.</returns>
		protected HardwareVertexBuffer MakeBufferCopy(HardwareVertexBuffer source, BufferUsage usage, bool useShadowBuffer) {
			return CreateVertexBuffer(source.VertexSize, source.VertexCount, usage, useShadowBuffer);
		}

        #endregion
    }
}
