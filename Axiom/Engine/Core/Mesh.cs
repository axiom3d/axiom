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
using System.IO;
using System.Runtime.InteropServices;
using Axiom.Animating;
using Axiom.Collections;
using Axiom.Configuration;
using Axiom.MathLib;
using Axiom.MathLib.Collections;
using Axiom.Serialization;
using Axiom.Graphics;

namespace Axiom.Core {
    /// <summary>
    ///    Resource holding data about a 3D mesh.
    /// </summary>
    /// <remarks>
    ///    This class holds the data used to represent a discrete
    ///    3-dimensional object. Mesh data usually contains more
    ///    than just vertices and triangle information; it also
    ///    includes references to materials (and the faces which use them),
    ///    level-of-detail reduction information, convex hull definition,
    ///    skeleton/bones information, keyframe animation etc.
    ///    However, it is important to note the emphasis on the word
    ///    'discrete' here. This class does not cover the large-scale
    ///    sprawling geometry found in level / landscape data.
    ///    <p/>
    ///    Multiple world objects can (indeed should) be created from a
    ///    single mesh object - see the Entity class for more info.
    ///    The mesh object will have it's own default
    ///    material properties, but potentially each world instance may
    ///    wish to customize the materials from the original. When the object
    ///    is instantiated into a scene node, the mesh material properties
    ///    will be taken by default but may be changed. These properties
    ///    are actually held at the SubMesh level since a single mesh may
    ///    have parts with different materials.
    ///    <p/>
    ///    As described above, because the mesh may have sections of differing
    ///    material properties, a mesh is inherently a compound contruct,
    ///    consisting of one or more SubMesh objects.
    ///    However, it strongly 'owns' it's SubMeshes such that they
    ///    are loaded / unloaded at the same time. This is contrary to
    ///    the approach taken to hierarchically related (but loosely owned)
    ///    scene nodes, where data is loaded / unloaded separately. Note
    ///    also that mesh sub-sections (when used in an instantiated object)
    ///    share the same scene node as the parent.
    /// </remarks>
    /// TODO: Add Clone method
    public class Mesh : Resource {
        #region Fields

        /// <summary>
        ///		Shared vertex data between multiple meshes.
        ///	</summary>
        protected VertexData sharedVertexData;
        /// <summary>
        ///		Collection of sub meshes for this mesh.
        ///	</summary>
        protected SubMeshCollection subMeshList = new SubMeshCollection();
        /// <summary>
        ///		Flag that indicates whether or not this mesh will be loaded from a file, or constructed manually.
        ///	</summary>
        protected bool isManuallyDefined = false;
		/// <summary>
		///		Local bounding box of this mesh.
		/// </summary>
        protected AxisAlignedBox boundingBox = AxisAlignedBox.Null;
		/// <summary>
		///		Radius of this mesh's bounding sphere.
		/// </summary>
        protected float boundingSphereRadius;

        /// <summary>Name of the skeleton bound to this mesh.</summary>
        protected string skeletonName;
         /// <summary>Reference to the skeleton bound to this mesh.</summary>
        protected Skeleton skeleton;
        /// <summary>List of bone assignment for this mesh.</summary>
        protected Map boneAssignmentList = new Map();
        /// <summary>Flag indicating that bone assignments need to be recompiled.</summary>
        protected bool boneAssignmentsOutOfDate;
        /// <summary>Number of blend weights that are assigned to each vertex.</summary>
        protected short numBlendWeightsPerVertex;
        /// <summary>Option whether to use software or hardware blending, there are tradeoffs to both.</summary>
        protected internal bool useSoftwareBlending;

		/// <summary>
		///		Flag indicating the use of manually created LOD meshes.
		/// </summary>
        protected internal bool isLodManual;
		/// <summary>
		///		Number of LOD meshes available.
		/// </summary>
        protected internal int numLods;
		/// <summary>
		///		List of data structures describing LOD usage.
		/// </summary>
        protected internal MeshLodUsageList lodUsageList = new MeshLodUsageList();

		/// <summary>
		///		Usage type for the vertex buffer.
		/// </summary>
        protected BufferUsage vertexBufferUsage;
		/// <summary>
		///		Usage type for the index buffer.
		/// </summary>
        protected BufferUsage indexBufferUsage;
		/// <summary>
		///		Use a shadow buffer for the vertex data?
		/// </summary>
        protected bool useVertexShadowBuffer;
		/// <summary>
		///		Use a shadow buffer for the index data?
		/// </summary>
        protected bool useIndexShadowBuffer;

		/// <summary>
		///		Edge data for use in shadow volume creation.
		/// </summary>
		protected EdgeData edgeData;
		/// <summary>
		///		Flag indicating whether precalculation steps to support shadows have been taken.
		/// </summary>
		protected bool isPreparedForShadowVolumes;

        #endregion

        #region Constructors

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        public Mesh(string name) {
            this.name = name;

            // default to static write only for speed
            vertexBufferUsage = BufferUsage.StaticWriteOnly;
            indexBufferUsage = BufferUsage.StaticWriteOnly;

			// default to having shadow buffers
			useVertexShadowBuffer = true;
			useIndexShadowBuffer = true;

            numLods = 1;
            MeshLodUsage lod = new MeshLodUsage();
            lod.fromSquaredDepth = 0.0f;
            lodUsageList.Add(lod);

            // always use software blending for now
            useSoftwareBlending = true;

            this.SkeletonName = "";
        }

        #endregion

        #region Properties

        /// <summary>
        ///    Bounding spehere radius from this mesh in local coordinates.
        /// </summary>
        public float BoundingSphereRadius {
            get { 
				return boundingSphereRadius; 
			}
            set { 
				boundingSphereRadius = value; 
			}
        }

		/// <summary>
		///		Gets the edge list for this mesh, building it if required. 
		/// </summary>
		/// <remarks>
		///		You must ensure that the Mesh as been prepared for shadow volume 
		///		rendering if you intend to use this information for that purpose.
		/// </remarks>
		public EdgeData EdgeList {
			get {
				if(edgeData == null) {
					BuildEdgeList();
				}

				return edgeData;
			}
		}

        /// <summary>
        ///    Determins whether or not this mesh has a skeleton associated with it.
        /// </summary>
        public bool HasSkeleton {
            get {
                return (skeletonName.Length != 0);
            }
        }

        /// <summary>
        ///    Gets the usage setting for this meshes index buffers.
        /// </summary>
        public BufferUsage IndexBufferUsage {
            get {
                return indexBufferUsage;
            }
        }

        /// <summary>
        ///    Gets whether or not this meshes index buffers are shadowed.
        /// </summary>
        public bool UseIndexShadowBuffer {
            get {
                return useIndexShadowBuffer;
            }
        }

        /// <summary>
        ///		Defines whether this mesh is to be loaded from a resource, or created manually at runtime.
        /// </summary>
        public bool IsManuallyDefined {
            get { 
				return isManuallyDefined; 
			}
            set { 
				isManuallyDefined = value; 
			}
        }

		/// <summary>
		///		Gets whether this mesh has already had it's geometry prepared for use in 
		///		rendering shadow volumes.
		/// </summary>
		public bool IsPreparedForShadowVolumes {
			get {
				return isPreparedForShadowVolumes;
			}
		}

        /// <summary>
        ///		Gets the current number of Lod levels associated with this mesh.
        /// </summary>
        public int LodLevelCount {
            get { 
				return lodUsageList.Count; 
			}
        }

        /// <summary>
        ///    Gets the skeleton currently bound to this mesh.
        /// </summary>
        public Skeleton Skeleton {
            get {
                return skeleton;
            }
        }

        /// <summary>
        ///    Get/Sets the name of the skeleton which will be bound to this mesh.
        /// </summary>
        public string SkeletonName {
            get {
                return skeletonName;
            }
            set {
                skeletonName = value;

                if(skeletonName == null || skeletonName.Length == 0) {
                    skeleton = null;
                }
                else {
                    // load the skeleton
                    skeleton = SkeletonManager.Instance.Load(skeletonName);
                }
            }
        }

        /// <summary>
        ///    Gets the usage setting for this meshes vertex buffers.
        /// </summary>
        public BufferUsage VertexBufferUsage {
            get {
                return vertexBufferUsage;
            }
        }

        /// <summary>
        ///    Gets whether or not this meshes vertex buffers are shadowed.
        /// </summary>
        public bool UseVertexShadowBuffer {
            get {
                return useVertexShadowBuffer;
            }
        }

        #endregion Properties

        #region Methods

        /// <summary>
        ///    Assigns a vertex to a bone with a given weight, for skeletal animation. 
        /// </summary>
        /// <remarks>
        ///    This method is only valid after setting SkeletonName.
        ///    You should not need to modify bone assignments during rendering (only the positions of bones) 
        ///    and the engine reserves the right to do some internal data reformatting of this information, 
        ///    depending on render system requirements.
        /// </remarks>
        /// <param name="boneAssignment">Bone assignment to add.</param>
        public void AddBoneAssignment(ref VertexBoneAssignment boneAssignment) {
            boneAssignmentList.Insert(boneAssignment.vertexIndex, boneAssignment);
            boneAssignmentsOutOfDate = true;
        }

		/// <summary>
		///		Builds an edge list for this mesh, which can be used for generating a shadow volume
		///		among other things.
		/// </summary>
		public void BuildEdgeList() {
			EdgeListBuilder builder = new EdgeListBuilder();
			int vertexSetCount = 0;

			if(sharedVertexData != null) {
				builder.AddVertexData(sharedVertexData);
				vertexSetCount++;
			}

			// Prepare the builder using the submesh information
			for(int i = 0; i < subMeshList.Count; i++) {
				SubMesh sm = subMeshList[i];

				if(sm.useSharedVertices) {
					// Use shared vertex data, index as set 0
					builder.AddIndexData(sm.indexData, 0);
				}
				else {
					// own vertex data, add it and reference it directly
					builder.AddVertexData(sm.vertexData);
					builder.AddIndexData(sm.indexData, vertexSetCount++);
				}
			}

			// build the edge data from all accumulate vertex/index buffers
			edgeData = builder.Build();
		}

        /// <summary>
        ///     Builds tangent space vector required for accurate bump mapping.
        /// </summary>
        /// <remarks>
        ///    Adapted from bump mapping tutorials at:
        ///    http://www.paulsprojects.net/tutorials/simplebump/simplebump.html
        ///    author : paul.baker@univ.ox.ac.uk
        ///    <p/>
        ///    Note: Only the tangent vector is calculated, it is assumed the binormal
        ///    will be calculated in a vertex program.
        /// </remarks>
        /// <param name="sourceTexCoordSet">Source texcoord set that holds the current UV texcoords.</param>
        /// <param name="destTexCoordSet">Destination texcoord set to hold the tangent vectors.</param>
        public void BuildTangentVectors(short sourceTexCoordSet, short destTexCoordSet) {
            // temp data buffers
            ushort[] vertIdx = new ushort[3];
            Vector3[] vertPos = new Vector3[3];
            float[] u = new float[3];
            float[] v = new float[3];

            int numSubMeshes = this.SubMeshCount;

            unsafe {
                // setup a new 3D tex coord buffer for every submesh
                for(int i = 0; i < numSubMeshes; i++) {
                    // the face indices buffer, read only
                    ushort* pIdx = null;
                    // pointer to 2D tex.coords, read only
                    float* p2DTC = null;
                    // pointer to 3D tex.coords, write/read (discard)
                    float* p3DTC = null;
                    // vertex position buffer, read only
                    float* pVPos = null;

                    SubMesh subMesh = GetSubMesh(i);

                    // get index buffer pointer
                    IndexData idxData = subMesh.indexData;
                    HardwareIndexBuffer buffIdx = idxData.indexBuffer;
                    IntPtr indices = buffIdx.Lock(BufferLocking.ReadOnly);
                    pIdx = (ushort*)indices.ToPointer();

                    // get vertex pointer
                    VertexData vertexData;

                    if(subMesh.useSharedVertices) {
                        vertexData = sharedVertexData; 
                    }
                    else {
                        vertexData = subMesh.vertexData;
                    }

                    VertexDeclaration decl = vertexData.vertexDeclaration;
                    VertexBufferBinding binding = vertexData.vertexBufferBinding;

                    // get a 3D tex coord buffer, creating one if it doesn't already exist
                    HardwareVertexBuffer buff3D = GetTangentBuffer(vertexData, destTexCoordSet);

                    // clear it out
                    IntPtr texCoords3D = buff3D.Lock(BufferLocking.Discard);
                    p3DTC = (float*)texCoords3D.ToPointer();

                    // TODO: Create a memset like function
                    for(int j = 0; j < buff3D.Size / Marshal.SizeOf(typeof(float)); j++) {
                        p3DTC[j] = 0;
                    } 

                    VertexElement elem2DTC = decl.FindElementBySemantic(VertexElementSemantic.TexCoords, sourceTexCoordSet);
                    
                    // make sure we have some 2D tex coords to deal with
                    if(elem2DTC == null || elem2DTC.Type != VertexElementType.Float2) {
                        // TODO: Add Name property to SubMesh
                        throw new AxiomException("SubMesh '{0}' of Mesh '{1}' has no 2D texture coordinates.", "<FIXME>", this.name);
                    }

                    // get the 2D tex coord buffer
                    HardwareVertexBuffer buff2D = binding.GetBuffer(elem2DTC.Source);
                    IntPtr locked2DBuffer = buff2D.Lock(BufferLocking.ReadOnly);
                    p2DTC = (float*)locked2DBuffer.ToPointer();

                    // get the vertex position buffer
                    VertexElement elemVPos = decl.FindElementBySemantic(VertexElementSemantic.Position);
                    HardwareVertexBuffer buffVPos = binding.GetBuffer(elemVPos.Source);
                    IntPtr lockedPosBuffer = buffVPos.Lock(BufferLocking.ReadOnly);
                    pVPos = (float*)lockedPosBuffer.ToPointer();

                    int numFaces = idxData.indexCount / 3;
                    int vCount = 0;

                    // loop through all faces to calculate the tangents
                    for(int n = 0; n < numFaces; n++) {
                        for(int a = 0; a < 3; a++) {
                            // get indices of vertices that form a polygon in the position buffer
                            vertIdx[a] = pIdx[vCount++];
                            
                            // get the vertex positions from the position buffer
                            vertPos[a].x = pVPos[3 * vertIdx[a] + 0];
                            vertPos[a].y = pVPos[3 * vertIdx[a] + 1];
                            vertPos[a].z = pVPos[3 * vertIdx[a] + 2];

                            // get the vertex tex coords from the 2D tex coord buffer
                            u[a] = p2DTC[2 * vertIdx[a] + 0];
                            v[a] = p2DTC[2 * vertIdx[a] + 1];
                        } // for v = 1 to 3

                        // calculate the tangent space vector
                        Vector3 tangent = 
                            MathUtil.CalculateTangentSpaceVector(
                                vertPos[0], vertPos[1], vertPos[2],
                                u[0], v[0], u[1], v[1], u[2], v[2]);

                        // write the new tex coords
                        // only tangent is written, the binormal should be derived in the vertex program
                        for(int t = 0; t < 3; t++) {
                            p3DTC[3 * vertIdx[t] + 0] += tangent.x;
                            p3DTC[3 * vertIdx[t] + 1] += tangent.y;
                            p3DTC[3 * vertIdx[t] + 2] += tangent.z;
                        } // for v = 1 to 3
                    } // for each face

                    int numVerts = vertexData.vertexCount;
                    
                    // loop through and normalize all 3d tex coords
                    for(int q = 0; q < numVerts * 3; q += 3) {
                        // read the 3d tex coord
                        Vector3 temp = new Vector3(p3DTC[q + 0], p3DTC[q + 1], p3DTC[q + 2]);

                        // normalize the tex coord
                        temp.Normalize();

                        // write it back to the buffer
                        p3DTC[q + 0] = temp.x;
                        p3DTC[q + 1] = temp.y;
                        p3DTC[q + 2] = temp.z;
                    }

                    // unlock all used buffers
                    buff2D.Unlock();
                    buff3D.Unlock();
                    buffVPos.Unlock();
                    buffIdx.Unlock();
                } // for each subMesh
            } // unsafe
        }

		/// <summary>
		///     Builds tangent space vector required for accurate bump mapping.
		/// </summary>
		/// <remarks>
		///    Adapted from bump mapping tutorials at:
		///    http://www.paulsprojects.net/tutorials/simplebump/simplebump.html
		///    author : paul.baker@univ.ox.ac.uk
		///    <p/>
		///    Note: Only the tangent vector is calculated, it is assumed the binormal
		///    will be calculated in a vertex program.
		/// </remarks>
        public void BuildTangentVectors() {
            // default using the first tex coord set and stuffing the tangent vectors in the 
            BuildTangentVectors(0, 1);
        }

        /// <summary>
        ///    Removes all bone assignments for this mesh. 
        /// </summary>
        /// <remarks>
        ///    This method is for modifying weights to the shared geometry of the Mesh. To assign
        ///    weights to the per-SubMesh geometry, see the equivalent methods on SubMesh.
        /// </remarks>
        public void ClearBoneAssignments() {
            boneAssignmentList.Clear();
            boneAssignmentsOutOfDate = true;
        }

        /// <summary>
        ///    Compile bone assignments into blend index and weight buffers.
        /// </summary>
        protected internal void CompileBoneAssignments() {
            int maxBones = RationalizeBoneAssignments(sharedVertexData.vertexCount, boneAssignmentList);

            // check for no bone assignments
            if(maxBones == 0) {
                return;
            }

			CompileBoneAssignments(boneAssignmentList, maxBones, sharedVertexData);

            boneAssignmentsOutOfDate = false;
        }

        /// <summary>
        ///    Software blending oriented bone assignment compilation.
        /// </summary>
        protected internal void CompileBoneAssignments(Map boneAssignments, int numBlendWeightsPerVertex, VertexData targetVertexData) {
			// Create or reuse blend weight / indexes buffer
			// Indices are always a UBYTE4 no matter how many weights per vertex
			// Weights are more specific though since they are Reals
			VertexDeclaration decl = targetVertexData.vertexDeclaration;
			VertexBufferBinding bind = targetVertexData.vertexBufferBinding;
			short bindIndex;

			VertexElement testElem = decl.FindElementBySemantic(VertexElementSemantic.BlendIndices);

			if (testElem != null) {
				// Already have a buffer, unset it & delete elements
				bindIndex = testElem.Source;

				// unset will cause deletion of buffer
				bind.UnsetBinding(bindIndex);
				decl.RemoveElement(VertexElementSemantic.BlendIndices);
				decl.RemoveElement(VertexElementSemantic.BlendWeights);
			}
			else {
				// Get new binding
				bindIndex = bind.NextIndex;
			}

			int bufferSize = Marshal.SizeOf(typeof(byte)) * 4;
			bufferSize += Marshal.SizeOf(typeof(float)) * numBlendWeightsPerVertex; 

			HardwareVertexBuffer vbuf = 
				HardwareBufferManager.Instance.CreateVertexBuffer(
					bufferSize,
					targetVertexData.vertexCount, 
					BufferUsage.StaticWriteOnly,
					true); // use shadow buffer
	                
			// bind new buffer
			bind.SetBinding(bindIndex, vbuf);
	        
			VertexElement idxElem, weightElem;

			// add new vertex elements
			// Note, insert directly after position to abide by pre-Dx9 format restrictions
			if(decl.GetElement(0).Semantic == VertexElementSemantic.Position) {
				idxElem = decl.InsertElement(1, bindIndex, 0, VertexElementType.UByte4, 
					VertexElementSemantic.BlendIndices);

				weightElem = decl.InsertElement(2, bindIndex, Marshal.SizeOf(typeof(byte)) * 4, 
					VertexElement.MultiplyTypeCount(VertexElementType.Float1, numBlendWeightsPerVertex),
					VertexElementSemantic.BlendWeights);
			}
			else
			{
				// Position is not the first semantic, therefore this declaration is
				// not pre-Dx9 compatible anyway, so just tack it on the end
				idxElem = decl.AddElement(bindIndex, 0, VertexElementType.UByte4, VertexElementSemantic.BlendIndices);
				weightElem = decl.AddElement(bindIndex, Marshal.SizeOf(typeof(byte)) * 4, 
					VertexElement.MultiplyTypeCount(VertexElementType.Float1, numBlendWeightsPerVertex),
					VertexElementSemantic.BlendWeights);
			}

			// get the bone assignment enumerator and move to the first one in the list
			IEnumerator i = boneAssignments.GetEnumerator();
			i.MoveNext();

			// Assign data
			IntPtr ptr = vbuf.Lock(BufferLocking.Discard);

			unsafe {
				byte* pBase = (byte*)ptr.ToPointer();

				// Iterate by vertex
				float* pWeight;
				byte* pIndex;
				bool end = false;

				for (int v = 0; v < targetVertexData.vertexCount; v++) {
					/// Convert to specific pointers
					pWeight = (float*)((byte*)pBase + weightElem.Offset);
					pIndex = pBase + idxElem.Offset;

					for (int bone = 0; bone < numBlendWeightsPerVertex; bone++) {
						Pair result = (Pair)i.Current;
						VertexBoneAssignment ba = (VertexBoneAssignment)result.second;

						// Do we still have data for this vertex?
						if (ba.vertexIndex == v && !end) {
							// If so, write weight
							*pWeight++ = ba.weight;
							*pIndex++ = (byte)ba.boneIndex;

							end = !i.MoveNext();
						}
						else {
							// Ran out of assignments for this vertex, use weight 0 to indicate empty
							*pWeight++ = 0.0f;
							*pIndex++ = 0;
						}
					}

					pBase += vbuf.VertexSize;
				}
			}

			vbuf.Unlock();
        }

        /// <summary>
        ///    Retrieves the level of detail index for the given depth value.
        /// </summary>
        /// <param name="depth"></param>
        /// <returns></returns>
        public int GetLodIndex(float depth) {
            return GetLodIndexSquaredDepth(depth * depth);
        }

        /// <summary>
        ///    Gets the mesh lod level at the specified index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public MeshLodUsage GetLodLevel(int index) {
            Debug.Assert(index < lodUsageList.Count, "index < lodUsageList.Count");

            MeshLodUsage usage = (MeshLodUsage)lodUsageList[index];

            // load the manual lod mesh for this level if not done already
            if(isLodManual && usage.manualMesh == null) {
                usage.manualMesh = MeshManager.Instance.Load(usage.manualName);
            }

            return usage;
        }

        /// <summary>
        ///    
        /// </summary>
        /// <param name="vertexData"></param>
        /// <param name="texCoordSet"></param>
        /// <returns></returns>
        public HardwareVertexBuffer GetTangentBuffer(VertexData vertexData, short texCoordSet) {
            bool needsToBeCreated = false;

            // grab refs to the declarations and bindings
            VertexDeclaration decl = vertexData.vertexDeclaration;
            VertexBufferBinding binding = vertexData.vertexBufferBinding;

            // see if we already have a 3D tex coord buffer
            VertexElement tex3d = decl.FindElementBySemantic(VertexElementSemantic.TexCoords, texCoordSet);

            if(tex3d == null) {
                needsToBeCreated = true;
            }
            else if(tex3d.Type != VertexElementType.Float3) {
                // TODO: Implement RemoveElement
                // decl.RemoveElement(VertexElementSemantic.TexCoords, texCoordSet);
                binding.UnsetBinding(tex3d.Source);

                needsToBeCreated = true;
            }

            HardwareVertexBuffer buff3D;
                
            if(needsToBeCreated) {
                // create the 3D tex coord buffer
                buff3D = HardwareBufferManager.Instance.CreateVertexBuffer(
                    3 * Marshal.SizeOf(typeof(float)),
                    vertexData.vertexCount,
                    BufferUsage.DynamicWriteOnly,
                    true);

                // bind the new buffer accordingly
                short source = binding.NextIndex;
                binding.SetBinding(source, buff3D);
                decl.AddElement(source, 0, VertexElementType.Float3, VertexElementSemantic.TexCoords, texCoordSet);
            }
            else {
                buff3D = binding.GetBuffer(tex3d.Source);
            }

            return buff3D;
        }

        /// <summary>
        ///    Retrieves the level of detail index for the given squared depth value.
        /// </summary>
        /// <remarks>
        ///    Internally the lods are stored at squared depths to avoid having to perform
        ///    square roots when determining the lod. This method allows you to provide a
        ///    squared length depth value to avoid having to do your own square roots.
        /// </remarks>
        /// <param name="squaredDepth"></param>
        /// <returns></returns>
        public int GetLodIndexSquaredDepth(float squaredDepth) {
            for(int i = 0; i < lodUsageList.Count; i++) {
                if(((MeshLodUsage)lodUsageList[i]).fromSquaredDepth > squaredDepth) {
                    return i - 1;
                }
            }

            // if we fall all the wat through, use the higher value
            return lodUsageList.Count - 1;
        }

        /// <summary>
        ///    Gets the sub mesh at the specified index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public SubMesh GetSubMesh(int index) {
            Debug.Assert(index < subMeshList.Count, "index < subMeshList.Count");

            return subMeshList[index];
        }

        /// <summary>
        ///    Initialise an animation set suitable for use with this mesh.
        /// </summary>
        /// <remarks>
        ///    Only recommended for use inside the engine, not by applications.
        /// </remarks>
        /// <param name="animSet"></param>
        public void InitAnimationState(AnimationStateCollection animSet) {
            Debug.Assert(skeleton != null, "Skeleton not present.");

            // delegate the animation set to the skeleton
            skeleton.InitAnimationState(animSet);

            // Take the opportunity to update the compiled bone assignments
            if(boneAssignmentsOutOfDate) {
                CompileBoneAssignments();
            }

            // compile bone assignments for each sub mesh
            for(int i = 0; i < subMeshList.Count; i++) {
                SubMesh subMesh = subMeshList[i];

                if(subMesh.boneAssignmentsOutOfDate) {
                    subMesh.CompileBoneAssignments();
                }
            } // for
        }

        /// <summary>
        ///    Internal notification, used to tell the Mesh which Skeleton to use without loading it. 
        /// </summary>
        /// <remarks>
        ///    This is only here for unusual situation where you want to manually set up a
        ///    Skeleton. Best to let the engine deal with this, don't call it yourself unless you
        ///    really know what you're doing.
        /// </remarks>
        /// <param name="skeleton"></param>
        public void NotifySkeleton(Skeleton skeleton) {
            skeleton = skeleton;
            skeletonName = skeleton.Name;
        }

		/// <summary>
		///		This method prepares the mesh for generating a renderable shadow volume.
		/// </summary>
		/// <remarks>
		///		Preparing a mesh to generate a shadow volume involves firstly ensuring that the 
		///		vertex buffer containing the positions for the mesh is a standalone vertex buffer,
		///		with no other components in it. This method will therefore break apart any existing
		///		vertex buffers this mesh holds if position is sharing a vertex buffer. 
		///		Secondly, it will double the size of this vertex buffer so that there are 2 copies of 
		///		the position data for the mesh. The first half is used for the original, and the second 
		///		half is used for the 'extruded' version of the mesh. The vertex count of the main 
		///		<see cref="VertexData"/> used to render the mesh will remain the same though, so as not to add any 
		///		overhead to regular rendering of the object.
		///		Both copies of the position are required in one buffer because shadow volumes stretch 
		///		from the original mesh to the extruded version.
		///		<p/>
		///		Because shadow volumes are rendered in turn, no additional
		///		index buffer space is allocated by this method, a shared index buffer allocated by the
		///		shadow rendering algorithm is used for addressing this extended vertex buffer.
		/// </remarks>
		public void PrepareForShadowVolume() {
			if(sharedVertexData != null) {
				sharedVertexData.PrepareForShadowVolume();
			}

			for(int i = 0; i < subMeshList.Count; i++) {
				SubMesh sm = subMeshList[i];

				if(!sm.useSharedVertices) {
					sm.vertexData.PrepareForShadowVolume();
				}
			}

			isPreparedForShadowVolumes = true;
		}

        /// <summary>
        ///     Rationalizes the passed in bone assignment list.
        /// </summary>
        /// <remarks>
        ///     We support up to 4 bone assignments per vertex. The reason for this limit
        ///     is that this is the maximum number of assignments that can be passed into
        ///     a hardware-assisted blending algorithm. This method identifies where there are
        ///     more than 4 bone assignments for a given vertex, and eliminates the bone
        ///     assignments with the lowest weights to reduce to this limit. The remaining
        ///     weights are then re-balanced to ensure that they sum to 1.0.
        /// </remarks>
        /// <param name="vertexCount">The number of vertices.</param>
        /// <param name="assignments">
        ///     The bone assignment list to rationalize. This list will be modified and
        ///     entries will be removed where the limits are exceeded.
        /// </param>
        /// <returns>The maximum number of bone assignments per vertex found, clamped to [1-4]</returns>
        internal int RationalizeBoneAssignments(int vertexCount, Map assignments) {
            int maxBones = 0;
            int currentBones = 0;

            for(int i = 0; i < vertexCount; i++) {
                // gets the numbers of assignments for the current vertex
                currentBones = assignments.Count(i);

                // Deal with max bones update 
                // (note this will record maxBones even if they exceed limit)
                if(maxBones < currentBones) {
                    maxBones = currentBones;
                }

                // does the number of bone assignments exceed limit?
                if(currentBones > Config.MaxBlendWeights) {
                    // TODO: Handle balancing of too many weights
                }

                float totalWeight = 0.0f;

                // Make sure the weights are normalised
                // Do this irrespective of whether we had to remove assignments or not
                //   since it gives us a guarantee that weights are normalised
                //  We assume this, so it's a good idea since some modellers may not
                IEnumerator iter = assignments.Find(i);

                if(iter == null) {
                    continue;
                }

                while(iter.MoveNext()) {
                    VertexBoneAssignment vba = (VertexBoneAssignment)iter.Current;
                    totalWeight += vba.weight;
                }

                // Now normalise if total weight is outside tolerance
                if(!MathUtil.FloatEqual(totalWeight, 1.0f)) {
					// TODO: This is not correct, shouldn't be advancing the current iterator
					// Need to have another one that starts at the same position as the current
                    while(iter.MoveNext()) {
                        VertexBoneAssignment vba = (VertexBoneAssignment)iter.Current;
                        vba.weight /= totalWeight;
                    }
                }
            }

            // Warn that we've reduced bone assignments
            if(maxBones > Config.MaxBlendWeights) {
                string msg = 
                    string.Format("WARNING: Mesh '{0}' includes vertices with more than {1} bone assignments.  The lowest weighted assignments beyond this limit have been removed.", name, Config.MaxBlendWeights);
                Trace.WriteLine(msg);

                maxBones = Config.MaxBlendWeights;
            }

            return maxBones;
        }

        #endregion Methods

		#region Static Methods

		/// <summary>
		///		Performs a software indexed vertex blend, of the kind used for
		///		skeletal animation although it can be used for other purposes. 
		/// </summary>
		/// <remarks>
		///		This function is supplied to update vertex data with blends 
		///		done in software, either because no hardware support is available, 
		///		or that you need the results of the blend for some other CPU operations.
		/// </remarks>
		/// <param name="sourceVertexData">
		///		<see cref="VertexData"/> class containing positions, normals, blend indices and blend weights.
		///	</param>
		/// <param name="targetVertexData">
		///		<see cref="VertexData"/> class containing target position
		///		and normal buffers which will be updated with the blended versions.
		///		Note that the layout of the source and target position / normal 
		///		buffers must be identical, ie they must use the same buffer indexes.
		/// </param>
		/// <param name="matrices">An array of matrices to be used to blend.</param>
		/// <param name="blendNormals">If true, normals are blended as well as positions.</param>
		public static void SoftwareVertexBlend(VertexData sourceVertexData, VertexData targetVertexData, Matrix4[] matrices, bool blendNormals) {
			// Source vectors
			Vector3 sourceVec = Vector3.Zero;
			Vector3 sourceNorm = Vector3.Zero;
			// Accumulation vectors
			Vector3 accumVecPos = Vector3.Zero;
			Vector3 accumVecNorm = Vector3.Zero;

			HardwareVertexBuffer srcPosBuf = null, srcNormBuf = null, srcIdxBuf = null, srcWeightBuf = null;
			HardwareVertexBuffer destPosBuf = null, destNormBuf = null;

			bool srcPosNormShareBuffer = false;
			bool destPosNormShareBuffer = false;
			bool weightsIndexesShareBuffer = false;

			IntPtr ptr = IntPtr.Zero;

			// Get elements for source
			VertexElement srcElemPos = 
				sourceVertexData.vertexDeclaration.FindElementBySemantic(VertexElementSemantic.Position);
			VertexElement srcElemNorm = 
				sourceVertexData.vertexDeclaration.FindElementBySemantic(VertexElementSemantic.Normal);
			VertexElement srcElemBlendIndices = 
				sourceVertexData.vertexDeclaration.FindElementBySemantic(VertexElementSemantic.BlendIndices);
			VertexElement srcElemBlendWeights = 
				sourceVertexData.vertexDeclaration.FindElementBySemantic(VertexElementSemantic.BlendWeights);

			Debug.Assert(srcElemPos != null && srcElemBlendIndices != null && srcElemBlendWeights != null, "You must supply at least positions, blend indices and blend weights");

			// Get elements for target
			VertexElement destElemPos = 
				targetVertexData.vertexDeclaration.FindElementBySemantic(VertexElementSemantic.Position);
			VertexElement destElemNorm = 
				targetVertexData.vertexDeclaration.FindElementBySemantic(VertexElementSemantic.Normal);
        
			// Do we have normals and want to blend them?
			bool includeNormals = blendNormals && (srcElemNorm != null) && (destElemNorm != null);

			// Get buffers for source
			srcPosBuf = sourceVertexData.vertexBufferBinding.GetBuffer(srcElemPos.Source);
			srcIdxBuf = sourceVertexData.vertexBufferBinding.GetBuffer(srcElemBlendIndices.Source);
			srcWeightBuf = sourceVertexData.vertexBufferBinding.GetBuffer(srcElemBlendWeights.Source);

			if (includeNormals)
			{
				srcNormBuf = sourceVertexData.vertexBufferBinding.GetBuffer(srcElemNorm.Source);
				srcPosNormShareBuffer = (srcPosBuf == srcNormBuf);
			}

			// note: reference comparison
			weightsIndexesShareBuffer = (srcIdxBuf == srcWeightBuf);

			// Get buffers for target
			destPosBuf = targetVertexData.vertexBufferBinding.GetBuffer(destElemPos.Source);

			if (includeNormals)
			{
				destNormBuf = targetVertexData.vertexBufferBinding.GetBuffer(destElemNorm.Source);
				destPosNormShareBuffer = (destPosBuf == destNormBuf);
			}

			// Lock source buffers for reading
			Debug.Assert(srcElemPos.Offset == 0, "Positions must be first element in dedicated buffer!");

			unsafe {
				float* pSrcPos = null, pSrcNorm = null, pDestPos = null, pDestNorm = null, pBlendWeight = null;
				byte* pBlendIdx = null;
			
				ptr = srcPosBuf.Lock(BufferLocking.ReadOnly);
				pSrcPos = (float*)ptr.ToPointer();

				if (includeNormals) {
					if (srcPosNormShareBuffer) {
						// Same buffer, must be packed directly after position
						Debug.Assert(srcElemNorm.Offset == (sizeof(float) * 3), "Normals must be packed directly after positions in buffer!");
						// pSrcNorm will not be used
					}
					else {
						// Different buffer
						Debug.Assert(srcElemNorm.Offset == 0, "Normals must be first element in dedicated buffer!");

						ptr = srcNormBuf.Lock(BufferLocking.ReadOnly);
						pSrcNorm = (float*)ptr.ToPointer();
					}
				}

				// Indices must be first in a buffer and be 4 bytes
				Debug.Assert(srcElemBlendIndices.Offset == 0 &&
					srcElemBlendIndices.Type == VertexElementType.UByte4, 
					"Blend indices must be first in a buffer and be VET_UBYTE4");

				ptr = srcIdxBuf.Lock(BufferLocking.ReadOnly);
				pBlendIdx = (byte*)ptr.ToPointer();

				if (weightsIndexesShareBuffer) {
					// Weights must be packed directly after the indices
					Debug.Assert(srcElemBlendWeights.Offset == (sizeof(byte) * 4),
						"Blend weights must be directly after indices in the buffer");

					pBlendWeight = (float*)((byte*)pBlendIdx + srcElemBlendWeights.Offset);
				}
				else {
					// Weights must be at the start of the buffer
					Debug.Assert(srcElemBlendWeights.Offset == 0, 
						"Blend weights must be at the start of a dedicated buffer");

					// Lock buffer
					ptr = srcWeightBuf.Lock(BufferLocking.ReadOnly);
					pBlendWeight = (float*)ptr.ToPointer();
				}

				int numWeightsPerVertex = VertexElement.GetTypeCount(srcElemBlendWeights.Type);

				// Lock destination buffers for writing

				Debug.Assert(destElemPos.Offset == 0,
					"Positions must be first element in dedicated buffer!");

				ptr = destPosBuf.Lock(BufferLocking.Discard);
				pDestPos = (float*)ptr.ToPointer();

				if (includeNormals) {
					if (destPosNormShareBuffer) {
						// Same buffer, must be packed directly after position
						Debug.Assert(destElemNorm.Offset == (sizeof(float) * 3), 
							"Normals must be packed directly after positions in buffer!");
						// pDestNorm will not be used
					}
					else {
						// Different buffer
						Debug.Assert(destElemNorm.Offset == 0, 
							"Normals must be first element in dedicated buffer!");

						ptr = destNormBuf.Lock(BufferLocking.Discard);
						pDestNorm = (float*)ptr.ToPointer();
					}
				}

				// Loop per vertex
				for(int vertIdx = 0; vertIdx < targetVertexData.vertexCount; vertIdx++) {
					// Load source vertex elements
					sourceVec.x = *pSrcPos++;
					sourceVec.y = *pSrcPos++;
					sourceVec.z = *pSrcPos++;

					if (includeNormals) {
						if (srcPosNormShareBuffer) {
							sourceNorm.x = *pSrcPos++;
							sourceNorm.y = *pSrcPos++;
							sourceNorm.z = *pSrcPos++;
						}
						else {
							sourceNorm.x = *pSrcNorm++;
							sourceNorm.y = *pSrcNorm++;
							sourceNorm.z = *pSrcNorm++;
						}
					}

					// Load accumulators
					accumVecPos = Vector3.Zero;
					accumVecNorm = Vector3.Zero;

					// Loop per blend weight 
					for (int blendIdx = 0; blendIdx < numWeightsPerVertex; blendIdx++) {
						// Blend by multiplying source by blend matrix and scaling by weight
						// Add to accumulator
						// NB weights must be normalised!!
						if (*pBlendWeight != 0.0f) {
							// Blend position, use 3x4 matrix
							Matrix4 mat = matrices[*pBlendIdx];

							accumVecPos.x += 
								(mat.m00 * sourceVec.x + 
								mat.m01 * sourceVec.y + 
								mat.m02 * sourceVec.z + 
								mat.m03)
								* (*pBlendWeight);

							accumVecPos.y += 
								(mat.m10 * sourceVec.x + 
								mat.m11 * sourceVec.y + 
								mat.m12 * sourceVec.z + 
								mat.m13)
								* (*pBlendWeight);

							accumVecPos.z += 
								(mat.m20 * sourceVec.x + 
								mat.m21 * sourceVec.y + 
								mat.m22 * sourceVec.z + 
								mat.m23)
								* (*pBlendWeight);

							if (includeNormals) {
								// Blend normal
								// We should blend by inverse transpose here, but because we're assuming the 3x3
								// aspect of the matrix is orthogonal (no non-uniform scaling), the inverse transpose
								// is equal to the main 3x3 matrix
								// Note because it's a normal we just extract the rotational part, saves us renormalising here
								accumVecNorm.x += 
									(mat.m00 * sourceNorm.x + 
									mat.m01 * sourceNorm.y + 
									mat.m02 * sourceNorm.z) 
									* (*pBlendWeight);

								accumVecNorm.y += 
									(mat.m10 * sourceNorm.x + 
									mat.m11 * sourceNorm.y + 
									mat.m12 * sourceNorm.z)
									* (*pBlendWeight);

								accumVecNorm.z += 
									(mat.m20 * sourceNorm.x + 
									mat.m21 * sourceNorm.y + 
									mat.m22 * sourceNorm.z)
									* (*pBlendWeight);
							}

						}
						++pBlendWeight;
						++pBlendIdx;
					}

					// Finish off blend info pointers
					// Make sure we skip over 4 index elements no matter how many we used
					pBlendIdx += (4 - numWeightsPerVertex);

					if(weightsIndexesShareBuffer) {
						// Skip index over weights
						pBlendIdx += sizeof(float) * numWeightsPerVertex;

						// Re-base weights
						pBlendWeight = (float*)((byte*)pBlendIdx + srcElemBlendWeights.Offset);
					}

					// Stored blended vertex in hardware buffer
					*pDestPos++ = accumVecPos.x;
					*pDestPos++ = accumVecPos.y;
					*pDestPos++ = accumVecPos.z;

					// Stored blended vertex in temp buffer
					if (includeNormals) {
						// Normalise
						accumVecNorm.Normalize();

						if (destPosNormShareBuffer) {
							// Pack into same buffer
							*pDestPos++ = accumVecNorm.x;
							*pDestPos++ = accumVecNorm.y;
							*pDestPos++ = accumVecNorm.z;
						}
						else {
							*pDestNorm++ = accumVecNorm.x;
							*pDestNorm++ = accumVecNorm.y;
							*pDestNorm++ = accumVecNorm.z;
						}
					}
				}
				// Unlock source buffers
				srcPosBuf.Unlock();
				srcIdxBuf.Unlock();
			
				if (!weightsIndexesShareBuffer) {
					srcWeightBuf.Unlock();
				}

				if (includeNormals && !srcPosNormShareBuffer) {
					srcNormBuf.Unlock();
				}
			
				// Unlock destination buffers
				destPosBuf.Unlock();

				if (includeNormals && !destPosNormShareBuffer) {
					destNormBuf.Unlock();
				}
			} // unsafe
		}

		#endregion Static Methods

        #region Implementation of Resource

        /// <summary>
        ///		Loads the mesh data.
        /// </summary>
        public override void Load() {
            // unload this first if it is already loaded
            if(isLoaded) {
                Unload();
                isLoaded = false;
            }

            // load this bad boy if it is not to be manually defined
            if(!isManuallyDefined) {
                // get the resource data from MeshManager
                Stream data = MeshManager.Instance.FindResourceData(name);

                // instantiate a mesh reader and pass in the stream data
                OgreMeshReader meshReader = new OgreMeshReader(data);

                string[] parts = name.Split('.');
                string ext = parts[1];

                if(ext != "mesh") {
                    data.Close();

                    throw new AxiomException("Unsupported mesh format '{0}'", ext);
                }

                // mesh loading stats
                int before, after;

                // get the tick count before loading the mesh
                before = Environment.TickCount;

                // import the .mesh file
                meshReader.Import(this);
				
                // get the tick count after loading the mesh
                after = Environment.TickCount;

                // record the time elapsed while loading the mesh
                System.Diagnostics.Trace.WriteLine(string.Format("Mesh: Loaded '{0}', took {1}ms", this.name,  (after - before)));

                // close the stream (we don't need to leave it open here)
                data.Close();
            }

			// prepare the mesh for a shadow volume?
			if(MeshManager.Instance.PrepareAllMeshesForShadowVolumes) {
				PrepareForShadowVolume();
			}
        }

        /// <summary>
        ///		Unloads the mesh data.
        /// </summary>
        public override void Unload() {
            subMeshList.Clear();
        }

        /// <summary>
        ///		
        /// </summary>
        public override void Dispose() {
        }

        /// <summary>
        ///		Creates a new <see cref="SubMesh"/> and gives it a name.
        /// </summary>
        /// <param name="name">Name of the new <see cref="SubMesh"/>.</param>
        /// <returns>A new <see cref="SubMesh"/> with this Mesh as its parent.</returns>
        public SubMesh CreateSubMesh(string name) {
            SubMesh subMesh = new SubMesh(name);

            // set the parent of the subMesh to us
            subMesh.Parent = this;

            // add to the list of child meshes
            subMeshList.Add(subMesh);

            return subMesh;
        }

        /// <summary>
        ///		Creates a new <see cref="SubMesh"/>.
        /// </summary>
        /// <remarks>
        ///		Method for manually creating geometry for the mesh.
        ///		Note - use with extreme caution - you must be sure that
        ///		you have set up the geometry properly.
        /// </remarks>
        /// <returns>A new SubMesh with this Mesh as its parent.</returns>
        public SubMesh CreateSubMesh() {
            string name = string.Format("{0}_SubMesh{1}", this.name, subMeshList.Count);

            SubMesh subMesh = new SubMesh(name);

            // set the parent of the subMesh to us
            subMesh.Parent = this;

            // add to the list of child meshes
            subMeshList.Add(subMesh);

            return subMesh;
        }

        /// <summary>
        ///		Gets/Sets the shared VertexData for this mesh.
        /// </summary>
        public VertexData SharedVertexData {
            get { 
                return sharedVertexData; 
            }
            set { 
                sharedVertexData = value; 
            }
        }

        /// <summary>
        ///    Gets the number of submeshes belonging to this mesh.
        /// </summary>
        public int SubMeshCount {
            get {
                return subMeshList.Count;
            }
        }

        /// <summary>
        ///		Gets/Sets the bounding box for this mesh.
        /// </summary>
        /// <remarks>
        ///		Setting this property is required when building manual meshes now, because Axiom can no longer 
        ///		update the bounds for you, because it cannot necessarily read vertex data back from 
        ///		the vertex buffers which this mesh uses (they very well might be write-only, and even
        ///		if they are not, reading data from a hardware buffer is a bottleneck).
        /// </remarks>
        public AxisAlignedBox BoundingBox {
            get {
                // OPTIMIZE: Cloning to prevent direct modification
                return (AxisAlignedBox)boundingBox.Clone();
            }
            set {
                boundingBox = value;

                float sqLen1 = boundingBox.Minimum.LengthSquared;
                float sqLen2 = boundingBox.Maximum.LengthSquared;

                // update the bounding sphere radius as well
                boundingSphereRadius = MathUtil.Sqrt(MathUtil.Max(sqLen1, sqLen2));
            }
        }

        /// <summary>
		///		Sets the policy for the vertex buffers to be used when loading this Mesh.
        /// </summary>
        /// <remarks>
        ///		By default, when loading the Mesh, static, write-only vertex and index buffers 
        ///		will be used where possible in order to improve rendering performance. 
        ///		However, such buffers
        ///		cannot be manipulated on the fly by CPU code (although shader code can). If you
        ///		wish to use the CPU to modify these buffers, you should call this method. Note,
        ///		however, that it only takes effect after the Mesh has been reloaded. Note that you
        ///		still have the option of manually repacing the buffers in this mesh with your
        ///		own if you see fit too, in which case you don't need to call this method since it
        ///		only affects buffers created by the mesh itself.
        ///		<p/>
        ///		You can define the approach to a Mesh by changing the default parameters to 
        ///		<see cref="MeshManager.Load"/> if you wish; this means the Mesh is loaded with those options
        ///		the first time instead of you having to reload the mesh after changing these options.
        /// </remarks>
		/// <param name="usage">The usage flags, which by default are <see cref="BufferUsage.StaticWriteOnly"/></param>
        /// <param name="useShadowBuffer">
        ///		If set to true, the vertex buffers will be created with a
        ///		system memory shadow buffer. You should set this if you want to be able to
        ///		read from the buffer, because reading from a hardware buffer is a no-no.
        /// </param>
        public void SetVertexBufferPolicy(BufferUsage usage, bool useShadowBuffer) {
            vertexBufferUsage = usage;
            useVertexShadowBuffer = useShadowBuffer;
        }

		/// <summary>
		///		Sets the policy for the index buffers to be used when loading this Mesh.
		/// </summary>
		/// <remarks>
		///		By default, when loading the Mesh, static, write-only vertex and index buffers 
		///		will be used where possible in order to improve rendering performance. 
		///		However, such buffers
		///		cannot be manipulated on the fly by CPU code (although shader code can). If you
		///		wish to use the CPU to modify these buffers, you should call this method. Note,
		///		however, that it only takes effect after the Mesh has been reloaded. Note that you
		///		still have the option of manually repacing the buffers in this mesh with your
		///		own if you see fit too, in which case you don't need to call this method since it
		///		only affects buffers created by the mesh itself.
		///		<p/>
		///		You can define the approach to a Mesh by changing the default parameters to 
		///		<see cref="MeshManager.Load"/> if you wish; this means the Mesh is loaded with those options
		///		the first time instead of you having to reload the mesh after changing these options.
		/// </remarks>
		/// <param name="usage">The usage flags, which by default are <see cref="BufferUsage.StaticWriteOnly"/></param>
		/// <param name="useShadowBuffer">
		///		If set to true, the index buffers will be created with a
		///		system memory shadow buffer. You should set this if you want to be able to
		///		read from the buffer, because reading from a hardware buffer is a no-no.
		/// </param>
        public void SetIndexBufferPolicy(BufferUsage usage, bool useShadowBuffer) {
            indexBufferUsage = usage;
            useIndexShadowBuffer = useShadowBuffer;
        }

        #endregion
    }
    
    ///<summary>
    ///     A way of recording the way each LOD is recorded this Mesh.
    /// </summary>
    public struct MeshLodUsage {
        ///<summary>Squared Z value from which this LOD will apply</summary>
        public float fromSquaredDepth;
         ///<summary>Only relevant if isLodManual is true, the name of the alternative mesh to use</summary>
 	    public string manualName;
        ///<summary>Reference to the manual mesh to avoid looking up each timey</summary>    	
        public Mesh manualMesh;
    }
}
