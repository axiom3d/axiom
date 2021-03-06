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

#endregion LGPL License

#region SVN Version Information

// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Axiom.Core;
using Axiom.Configuration;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
    /// <summary>
    ///     Struct used to hold hardware morph / pose vertex data information
    /// </summary>
    public class HardwareAnimationData
    {
        protected VertexElement targetVertexElement;
        protected float parametric;

        public VertexElement TargetVertexElement
        {
            get
            {
                return this.targetVertexElement;
            }
            set
            {
                this.targetVertexElement = value;
            }
        }

        public float Parametric
        {
            get
            {
                return this.parametric;
            }
            set
            {
                this.parametric = value;
            }
        }
    }

    /// <summary>
    /// 	Summary class collecting together vertex source information.
    /// </summary>
    public class VertexData : DisposableObject
    {
        #region Fields

        /// <summary>
        ///		Declaration of the vertex to be used in this operation.
        /// </summary>
        public VertexDeclaration vertexDeclaration;

        /// <summary>
        ///		The vertex buffer bindings to be used.
        /// </summary>
        public VertexBufferBinding vertexBufferBinding;

        /// <summary>
        ///		The base vertex index to start from, if using unindexed geometry.
        /// </summary>
        public int vertexStart = 0;

        /// <summary>
        ///		The number of vertices used in this operation.
        /// </summary>
        public int vertexCount = 0;

        /// <summary>
        ///     VertexElements used for hardware morph / pose animation
        /// </summary>
        public List<HardwareAnimationData> HWAnimationDataList;

        /// <summary>
        ///     Number of hardware animation data items used
        /// </summary>
        public int HWAnimDataItemsUsed = 0;

        /// <summary>
        ///		Additional shadow volume vertex buffer storage.
        /// </summary>
        /// <remarks>
        ///		This additional buffer is only used where we have prepared this VertexData for
        ///		use in shadow volume contruction, and where the current render system supports
        ///		vertex programs. This buffer contains the 'w' vertex position component which will
        ///		be used by that program to differentiate between extruded and non-extruded vertices.
        ///		This 'w' component cannot be included in the original position buffer because
        ///		DirectX does not allow 4-component positions in the fixed-function pipeline, and the original
        ///		position buffer must still be usable for fixed-function rendering.
        ///		<p/>
        ///		Note that we don't store any vertex declaration or vertex buffer binding here becuase this
        ///		can be reused in the shadow algorithm.
        /// </remarks>
        public HardwareVertexBuffer hardwareShadowVolWBuffer;

        /// <summary>
        /// Whether this class should delete the declaration and binding
        /// </summary>
        public bool DeleteDclBinding { get; set; }

        private readonly HardwareBufferManagerBase _mgr;

        #endregion Fields

        #region Constructor

        /// <summary>
        ///		Default constructor.  Calls on the current buffer manager to initialize the bindings and declarations.
        /// </summary>
        public VertexData()
            : this(null)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <remarks>This constructor creates the VertexDeclaration and VertexBufferBinding
        /// automatically, and arranges for their deletion afterwards.</remarks>
        /// <param name="mgr">Optional HardwareBufferManager from which to create resources</param>
        [OgreVersion(1, 7, 2)]
        public VertexData(HardwareBufferManagerBase mgr)
            : base()
        {
            this._mgr = mgr != null ? mgr : HardwareBufferManager.Instance;
            this.vertexBufferBinding = HardwareBufferManager.Instance.CreateVertexBufferBinding();
            this.vertexDeclaration = HardwareBufferManager.Instance.CreateVertexDeclaration();
            DeleteDclBinding = true;
        }

        [OgreVersion(1, 7, 2)]
        public VertexData(VertexDeclaration dcl, VertexBufferBinding bind)
            : base()
        {
            // this is a fallback rather than actively used
            this._mgr = HardwareBufferManager.Instance;
            this.vertexDeclaration = dcl;
            this.vertexBufferBinding = bind;
            DeleteDclBinding = false;
        }

        #endregion Constructor

        #region Methods

        /// <summary>
        ///		Clones this vertex data, potentially including replicating any vertex buffers.
        /// </summary>
        /// <returns>A cloned vertex data object.</returns>
        public VertexData Clone()
        {
            return Clone(false);
        }

        /// <summary>
        ///		Clones this vertex data, potentially including replicating any vertex buffers.
        /// </summary>
        /// <param name="copyData">
        ///		If true, makes a copy the vertex buffer in addition to the definition.
        ///		If false, the clone will refer to the same vertex buffer this object refers to.
        /// </param>
        /// <returns>A cloned vertex data object.</returns>
        public VertexData Clone(bool copyData)
        {
            var dest = new VertexData();

            // Copy vertex buffers in turn
            var bindings = this.vertexBufferBinding.Bindings;

            foreach (var source in bindings.Keys)
            {
                var srcbuf = bindings[source];
                HardwareVertexBuffer dstBuf;

                if (copyData)
                {
                    // create new buffer with the same settings
                    dstBuf = HardwareBufferManager.Instance.CreateVertexBuffer(srcbuf.VertexDeclaration, srcbuf.VertexCount,
                                                                                srcbuf.Usage, srcbuf.HasShadowBuffer);

                    // copy data
                    dstBuf.CopyTo(srcbuf, 0, 0, srcbuf.Size, true);
                }
                else
                {
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
            for (var i = 0; i < this.vertexDeclaration.ElementCount; i++)
            {
                var element = this.vertexDeclaration.GetElement(i);

                dest.vertexDeclaration.AddElement(element.Source, element.Offset, element.Type, element.Semantic, element.Index);
            }

            // Copy hardware shadow buffer if set up
            if (this.hardwareShadowVolWBuffer != null)
            {
                dest.hardwareShadowVolWBuffer =
                    HardwareBufferManager.Instance.CreateVertexBuffer(this.hardwareShadowVolWBuffer.VertexDeclaration,
                                                                       this.hardwareShadowVolWBuffer.VertexCount,
                                                                       this.hardwareShadowVolWBuffer.Usage,
                                                                       this.hardwareShadowVolWBuffer.HasShadowBuffer);

                // copy data
                dest.hardwareShadowVolWBuffer.CopyTo(this.hardwareShadowVolWBuffer, 0, 0, this.hardwareShadowVolWBuffer.Size, true);
            }

            // copy anim data
            dest.HWAnimationDataList = this.HWAnimationDataList;
            dest.HWAnimDataItemsUsed = this.HWAnimDataItemsUsed;

            return dest;
        }

        /// <summary>
        ///		Modifies the vertex data to be suitable for use for rendering shadow geometry.
        /// </summary>
        /// <remarks>
        ///		<para>
        ///			Preparing vertex data to generate a shadow volume involves firstly ensuring that the
        ///			vertex buffer containing the positions is a standalone vertex buffer,
        ///			with no other components in it. This method will therefore break apart any existing
        ///			vertex buffers if position is sharing a vertex buffer.
        ///			Secondly, it will double the size of this vertex buffer so that there are 2 copies of
        ///			the position data for the mesh. The first half is used for the original, and the second
        ///			half is used for the 'extruded' version. The vertex count used to render will remain
        ///			the same though, so as not to add any overhead to regular rendering of the object.
        ///			Both copies of the position are required in one buffer because shadow volumes stretch
        ///			from the original mesh to the extruded version.
        ///		</para>
        ///		<para>
        ///			It's important to appreciate that this method can fundamentally change the structure of your
        ///			vertex buffers, although in reality they will be new buffers. As it happens, if other
        ///			objects are using the original buffers then they will be unaffected because the reference
        ///			counting will keep them intact. However, if you have made any assumptions about the
        ///			structure of the vertex data in the buffers of this object, you may have to rethink them.
        ///		</para>
        /// </remarks>
        public void PrepareForShadowVolume()
        {
            /* NOTE
			Sinbad would dearly, dearly love to just use a 4D position buffer in order to
			store the extra 'w' value I need to differentiate between extruded and
			non-extruded sections of the buffer, so that vertex programs could use that.
			Hey, it works fine for GL. However, D3D9 in it's infinite stupidity, does not
			support 4d position vertices in the fixed-function pipeline. If you use them,
			you just see nothing. Since we can't know whether the application is going to use
			fixed function or vertex programs, we have to stick to 3d position vertices and
			store the 'w' in a separate 1D texture coordinate buffer, which is only used
			when rendering the shadow.
			*/

            // Upfront, lets check whether we have vertex program capability
            var renderSystem = Root.Instance.RenderSystem;
            var useVertexPrograms = false;

            if (renderSystem != null && renderSystem.Capabilities.HasCapability(Capabilities.VertexPrograms))
            {
                useVertexPrograms = true;
            }

            // Look for a position element
            var posElem = this.vertexDeclaration.FindElementBySemantic(VertexElementSemantic.Position);

            if (posElem != null)
            {
                var posOldSource = posElem.Source;

                var vbuf = this.vertexBufferBinding.GetBuffer(posOldSource);

                var wasSharedBuffer = false;

                // Are there other elements in the buffer except for the position?
                if (vbuf.VertexSize > posElem.Size)
                {
                    // We need to create another buffer to contain the remaining elements
                    // Most drivers don't like gaps in the declaration, and in any case it's waste
                    wasSharedBuffer = true;
                }

                HardwareVertexBuffer newPosBuffer = null, newRemainderBuffer = null;
                var newRemainderDeclaration = (VertexDeclaration)this.vertexDeclaration.Clone();

                if (wasSharedBuffer)
                {
                    var found = false;
                    var index = 0;
                    do
                    {
                        if (newRemainderDeclaration.GetElement(index).Semantic == VertexElementSemantic.Position)
                        {
                            newRemainderDeclaration.RemoveElement(index);
                            found = true;
                        }
                        index++;
                    }
                    while (!found);

                    newRemainderBuffer = HardwareBufferManager.Instance.CreateVertexBuffer(newRemainderDeclaration, vbuf.VertexCount,
                                                                                            vbuf.Usage, vbuf.HasShadowBuffer);
                }

                // Allocate new position buffer, will be FLOAT3 and 2x the size
                var oldVertexCount = vbuf.VertexCount;
                var newVertexCount = oldVertexCount * 2;

                var newPosDecl = HardwareBufferManager.Instance.CreateVertexDeclaration();
                newPosDecl.AddElement(0, 0, VertexElementType.Float3, VertexElementSemantic.Position);
                newPosBuffer = HardwareBufferManager.Instance.CreateVertexBuffer(newPosDecl, newVertexCount, vbuf.Usage,
                                                                                  vbuf.HasShadowBuffer);

                // Iterate over the old buffer, copying the appropriate elements and initializing the rest
                var baseSrcPtr = vbuf.Lock(BufferLocking.ReadOnly);

                // Point first destination pointer at the start of the new position buffer,
                // the other one half way along
                var destPtr = newPosBuffer.Lock(BufferLocking.Discard);
                // oldVertexCount * 3 * 4, since we are dealing with byte offsets here
                var dest2Ptr = destPtr + (oldVertexCount * 12);

                var prePosVertexSize = 0;
                var postPosVertexSize = 0;
                var postPosVertexOffset = 0;

                if (wasSharedBuffer)
                {
                    // Precalculate any dimensions of vertex areas outside the position
                    prePosVertexSize = posElem.Offset;
                    postPosVertexOffset = prePosVertexSize + posElem.Size;
                    postPosVertexSize = vbuf.VertexSize - postPosVertexOffset;

                    // the 2 separate bits together should be the same size as the remainder buffer vertex
                    Debug.Assert(newRemainderBuffer.VertexSize == (prePosVertexSize + postPosVertexSize));

                    var baseDestRemPtr = newRemainderBuffer.Lock(BufferLocking.Discard);

                    var baseSrcOffset = 0;
                    var baseDestRemOffset = 0;

#if !AXIOM_SAFE_ONLY
                    unsafe
#endif
                    {
                        var pDest = destPtr.ToFloatPointer();
                        var pDest2 = dest2Ptr.ToFloatPointer();

                        int destCount = 0, dest2Count = 0;

                        // Iterate over the vertices
                        for (var v = 0; v < oldVertexCount; v++)
                        {
                            var pSrc = (baseSrcPtr + (posElem.Offset + baseSrcOffset)).ToFloatPointer();

                            // Copy position, into both buffers
                            pDest[destCount++] = pDest2[dest2Count++] = pSrc[0];
                            pDest[destCount++] = pDest2[dest2Count++] = pSrc[1];
                            pDest[destCount++] = pDest2[dest2Count++] = pSrc[2];

                            // now deal with any other elements
                            // Basically we just memcpy the vertex excluding the position
                            if (prePosVertexSize > 0)
                            {
                                Memory.Copy(baseSrcPtr, baseDestRemPtr, baseSrcOffset, baseDestRemOffset, prePosVertexSize);
                            }

                            if (postPosVertexSize > 0)
                            {
                                Memory.Copy(baseSrcPtr, baseDestRemPtr, baseSrcOffset + postPosVertexOffset,
                                             baseDestRemOffset + prePosVertexSize, postPosVertexSize);
                            }

                            // increment the pointer offsets
                            baseDestRemOffset += newRemainderBuffer.VertexSize;
                            baseSrcOffset += vbuf.VertexSize;
                        } // next vertex
                    } // unsafe
                }
                else
                {
                    // copy the data directly
                    Memory.Copy(baseSrcPtr, destPtr, vbuf.Size);
                    Memory.Copy(baseSrcPtr, dest2Ptr, vbuf.Size);
                }

                vbuf.Unlock();
                newPosBuffer.Unlock();

                if (wasSharedBuffer)
                {
                    newRemainderBuffer.Unlock();
                }

                // At this stage, he original vertex buffer is going to be destroyed
                // So we should force the deallocation of any temporary copies
                HardwareBufferManager.Instance.ForceReleaseBufferCopies(vbuf);

                if (useVertexPrograms)
                {
#if !AXIOM_SAFE_ONLY
                    unsafe
#endif
                    {
                        var decl = HardwareBufferManager.Instance.CreateVertexDeclaration();
                        decl.AddElement(0, 0, VertexElementType.Float1, VertexElementSemantic.Position);

                        // Now it's time to set up the w buffer
                        this.hardwareShadowVolWBuffer = HardwareBufferManager.Instance.CreateVertexBuffer(decl, newVertexCount,
                                                                                                           BufferUsage.StaticWriteOnly,
                                                                                                           false);

                        // Fill the first half with 1.0, second half with 0.0
                        var wPtr = this.hardwareShadowVolWBuffer.Lock(BufferLocking.Discard);
                        var pDest = wPtr.ToFloatPointer();
                        var destCount = 0;

                        for (var v = 0; v < oldVertexCount; v++)
                        {
                            pDest[destCount++] = 1.0f;
                        }
                        for (var v = 0; v < oldVertexCount; v++)
                        {
                            pDest[destCount++] = 0.0f;
                        }
                    } // unsafe

                    this.hardwareShadowVolWBuffer.Unlock();
                } // if vertexPrograms

                short newPosBufferSource = 0;

                if (wasSharedBuffer)
                {
                    // Get the a new buffer binding index
                    newPosBufferSource = this.vertexBufferBinding.NextIndex;

                    // Re-bind the old index to the remainder buffer
                    this.vertexBufferBinding.SetBinding(posOldSource, newRemainderBuffer);
                }
                else
                {
                    // We can just re-use the same source idex for the new position buffer
                    newPosBufferSource = posOldSource;
                }

                // Bind the new position buffer
                this.vertexBufferBinding.SetBinding(newPosBufferSource, newPosBuffer);

                // Now, alter the vertex declaration to change the position source
                // and the offsets of elements using the same buffer
                for (var i = 0; i < this.vertexDeclaration.ElementCount; i++)
                {
                    var element = this.vertexDeclaration.GetElement(i);

                    if (element.Semantic == VertexElementSemantic.Position)
                    {
                        // Modify position to point at new position buffer
                        this.vertexDeclaration.ModifyElement(i, newPosBufferSource /* new source buffer */, 0 /* no offset now */,
                                                              VertexElementType.Float3, VertexElementSemantic.Position);
                    }
                    else if (wasSharedBuffer && element.Source == posOldSource && element.Offset > prePosVertexSize)
                    {
                        // This element came after position, remove the position's size
                        this.vertexDeclaration.ModifyElement(i, posOldSource /* same old source */, element.Offset - posElem.Size
                                                              /* less offset now */, element.Type, element.Semantic, element.Index);
                    }
                }
            } // if posElem != null
        }

        /// <summary>
        ///     Allocate elements to serve a holder of morph / pose target data
        ///	    for hardware morphing / pose blending.
        /// </summary>
        /// <remarks>
        ///		This method will allocate the given number of 3D texture coordinate
        ///		sets for use as a morph target or target pose offset (3D position).
        ///		These elements will be saved in hwAnimationDataList.
        ///		It will also assume that the source of these new elements will be new
        ///		buffers which are not bound at this time, so will start the sources to
        ///		1 higher than the current highest binding source. The caller is
        ///		expected to bind these new buffers when appropriate. For morph animation
        ///		the original position buffer will be the 'from' keyframe data, whilst
        ///		for pose animation it will be the original vertex data.
        /// </remarks>
        public void AllocateHardwareAnimationElements(ushort count)
        {
            // Find first free texture coord set
            short texCoord = 0;
            for (var i = 0; i < this.vertexDeclaration.ElementCount; i++)
            {
                var element = this.vertexDeclaration.GetElement(i);
                if (element.Semantic == VertexElementSemantic.TexCoords)
                {
                    ++texCoord;
                }
            }
            Debug.Assert(texCoord <= Config.MaxTextureCoordSets);

            // Increase to correct size
            for (var c = this.HWAnimationDataList.Count; c < count; ++c)
            {
                // Create a new 3D texture coordinate set
                var data = new HardwareAnimationData();
                data.TargetVertexElement = this.vertexDeclaration.AddElement(this.vertexBufferBinding.NextIndex, 0,
                                                                              VertexElementType.Float3,
                                                                              VertexElementSemantic.TexCoords, texCoord++);

                this.HWAnimationDataList.Add(data);
                // Vertex buffer will not be bound yet, we expect this to be done by the
                // caller when it becomes appropriate (e.g. through a VertexAnimationTrack)
            }
        }

        #endregion Methods

        #region IDisposable Implementation

        protected override void dispose(bool disposeManagedResources)
        {
            if (!IsDisposed)
            {
                if (disposeManagedResources)
                {
                    // Dispose managed resources.
                    if (DeleteDclBinding)
                    {
                        this._mgr.DestroyVertexBufferBinding(this.vertexBufferBinding);
                        this._mgr.DestroyVertexDeclaration(this.vertexDeclaration);
                    }
                }

                // There are no unmanaged resources to release, but
                // if we add them, they need to be released here.
            }

            // If it is available, make the call to the
            // base class's Dispose(Boolean) method
            base.dispose(disposeManagedResources);
        }

        #endregion IDisposable Implementation
    }

    /// <summary>
    /// 	Summary class collecting together index data source information.
    /// </summary>
    public class IndexData : DisposableObject
    {
        #region Fields

        /// <summary>
        ///		Reference to the <see cref="HardwareIndexBuffer"/> to use, must be specified if useIndexes = true
        /// </summary>
        public HardwareIndexBuffer indexBuffer;

        /// <summary>
        ///		Index in the buffer to start from for this operation.
        /// </summary>
        public int indexStart;

        /// <summary>
        ///		The number of indexes to use from the buffer.
        /// </summary>
        public int indexCount;

        #endregion Fields

        #region Methods

        /// <summary>
        ///		Creates a copy of the index data object, without a copy of the buffer data.
        /// </summary>
        /// <returns>A copy of this IndexData object without the data.</returns>
        public IndexData Clone()
        {
            return Clone(false);
        }

        /// <summary>
        ///		Clones this vertex data, potentially including replicating any index buffers.
        /// </summary>
        /// <param name="copyData">
        ///		If true, makes a copy the index buffer in addition to the definition.
        ///		If false, the clone will refer to the same index buffer this object refers to.
        /// </param>
        /// <returns>A copy of this IndexData object.</returns>
        public IndexData Clone(bool copyData)
        {
            var clone = new IndexData();

            if (this.indexBuffer != null)
            {
                if (copyData)
                {
                    clone.indexBuffer = HardwareBufferManager.Instance.CreateIndexBuffer(this.indexBuffer.Type,
                                                                                          this.indexBuffer.IndexCount,
                                                                                          this.indexBuffer.Usage,
                                                                                          this.indexBuffer.HasShadowBuffer);

                    // copy all the existing buffer data
                    clone.indexBuffer.CopyTo(this.indexBuffer, 0, 0, this.indexBuffer.Size, true);
                }
                else
                {
                    clone.indexBuffer = this.indexBuffer;
                }
            }

            clone.indexStart = this.indexStart;
            clone.indexCount = this.indexCount;

            return clone;
        }

        #endregion Methods
    }
}