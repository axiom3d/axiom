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
using Axiom.Exceptions;
using Axiom.MathLib;
using Axiom.MathLib.Collections;
using Axiom.Serialization;
using Axiom.Graphics;

namespace Axiom.Core {
    /// <summary>
    ///    Resource holding data about 3D mesh.
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

        /// <summary>Shared vertex data between multiple meshes.</summary>
        protected VertexData sharedVertexData;
        /// <summary>Collection of sub meshes for this mesh.</summary>
        protected SubMeshCollection subMeshList = new SubMeshCollection();
        /// <summary>Flag that states whether or not the bounding box for this mesh needs to be re-calced.</summary>
        protected bool updateBounds = true;
        /// <summary>Flag that states whether or not this mesh will be loaded from a file, or constructed manually.</summary>
        protected bool isManuallyDefined = false;
        protected AxisAlignedBox boundingBox = AxisAlignedBox.Null;
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

        // LOD settings, declared internal so OgreMeshReader can use them, nobody else needs access
        // to them
        protected internal bool isLodManual;
        protected internal int numLods;
        protected internal ArrayList lodUsageList = new ArrayList();

        // vertex buffer settings
        protected BufferUsage vertexBufferUsage;
        protected BufferUsage indexBufferUsage;
        protected bool useVertexShadowBuffer;
        protected bool useIndexShadowBuffer;

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
            get { return boundingSphereRadius; }
            set { boundingSphereRadius = value; }
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
        ///    
        /// </summary>
        public BufferUsage IndexBufferUsage {
            get {
                return indexBufferUsage;
            }
        }

        /// <summary>
        ///    
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
            get { return isManuallyDefined; }
            set { isManuallyDefined = value; }
        }

        /// <summary>
        ///		Gets the current number of Lod levels associated with this mesh.
        /// </summary>
        public int LodLevelCount {
            get { return lodUsageList.Count; }
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
        ///    
        /// </summary>
        public BufferUsage VertexBufferUsage {
            get {
                return vertexBufferUsage;
            }
        }

        /// <summary>
        ///    
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
        /// <param name="boneAssignment"></param>
        public void AddBoneAssignment(ref VertexBoneAssignment boneAssignment) {
            boneAssignmentList.Insert(boneAssignment.vertexIndex, boneAssignment);
            boneAssignmentsOutOfDate = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        ///    Adapted from bump mapping tutorials at:
        ///    http://www.paulsprojects.net/tutorials/simplebump/simplebump.html
        ///    author : paul.baker@univ.ox.ac.uk
        /// </remarks>
        /// <param name="sourceTexCoordSet"></param>
        /// <param name="destTexCoordSet"></param>
        public void BuildTangentVectors(short sourceTexCoordSet, short destTexCoordSet) {
            // temp data buffers
            ushort[] vertIdx = new ushort[3];
            Vector3[] vertPos = new Vector3[3];
            float[] u = new float[3];
            float[] v = new float[3];

            // TODO: rename to NumSubMeshes?
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

                    VertexElement elem2DTC = decl.FindElementBySemantic(VertexElementSemantic.TexCoords, (ushort)sourceTexCoordSet);
                    
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
        /// 
        /// </summary>
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
        ///    Must be called once to compile bone assignments into geometry buffer.
        /// </summary>
        protected internal void CompileBoneAssignments() {
            int maxBones = RationalizeBoneAssignments(sharedVertexData.vertexCount, boneAssignmentList);

            // check for no bone assignments
            if(maxBones == 0) {
                return;
            }

            // figure out which method of bone assignment compilation to use
            if(useSoftwareBlending) {
                CompileBoneAssignmentsSoftware(boneAssignmentList, numBlendWeightsPerVertex, sharedVertexData);
            }
            else {
                CompileBoneAssignmentsHardware(boneAssignmentList, numBlendWeightsPerVertex, sharedVertexData);
            }

            boneAssignmentsOutOfDate = false;
        }

        /// <summary>
        ///    Software blending oriented bone assignment compilation.
        /// </summary>
        protected internal void CompileBoneAssignmentsSoftware(Map boneAssignments, int numBlendWeightsPerVertex, VertexData targetVertexData) {
            SoftwareBlendInfo blendInfo = targetVertexData.softwareBlendInfo;

            // create new data buffers
            blendInfo.blendIndices = 
                new byte[targetVertexData.vertexCount * numBlendWeightsPerVertex];
            blendInfo.blendWeights = 
                new float[targetVertexData.vertexCount * numBlendWeightsPerVertex];

            // get the first element of the bone assignment list
            IEnumerator iter = boneAssignments.GetEnumerator();

            // move to the first position
            iter.MoveNext();
            
            VertexBoneAssignment boneAssignment = (VertexBoneAssignment)((Pair)iter.Current).second;

            int index = 0;
                
            // interate through each vertex
            for(int v = 0; v < targetVertexData.vertexCount; v++) {
                for(int b = 0; b < numBlendWeightsPerVertex; b++) {
                    if(boneAssignment.vertexIndex == v) {
                        blendInfo.blendWeights[index] = boneAssignment.weight;
                        blendInfo.blendIndices[index] = (byte)boneAssignment.boneIndex;

                        iter.MoveNext();
                        boneAssignment = (VertexBoneAssignment)((Pair)iter.Current).second;
                    }
                    else {
                        // Ran out of assignments for this vertex, use weight 0 to indicate empty
                        blendInfo.blendWeights[index] = 0.0f;
                        blendInfo.blendIndices[index] = 0;
                    }

                    // increment the index to be used for the data array indexes
                    index++;
                } // for
            } // for

            // record the number of weights per vertex
            targetVertexData.softwareBlendInfo.numWeightsPerVertex = (ushort)numBlendWeightsPerVertex;
        }

        /// <summary>
        ///    Hardware blending oriented bone assignment compilation.
        /// </summary>
        protected internal void CompileBoneAssignmentsHardware(Map boneAssignments, int numBlendWeightsPerVertex, VertexData targetVertexData) {
            // TODO: Implementation of Mesh.CompileBoneAssignmentsHardware
        }

        /// <summary>
        ///    Applies the animation set passed in, and populates the passed in array of bone matrices.
        /// </summary>
        /// <remarks>
        ///    Internal use only.
        ///    The array of passed in Matrix4 objects must have enough 'slots' for the number
        ///    of bone matrices required (see BoneMatrixCount).
        /// </remarks>
        /// <param name="animSet"></param>
        /// <param name="matrices"></param>
        public void GetBoneMatrices(AnimationStateCollection animSet, Matrix4[] matrices) {
            Debug.Assert(skeleton != null, "Skeleton not present.");

            // delegate down to the skeleton
            skeleton.SetAnimationState(animSet);
            skeleton.GetBoneMatrices(matrices);
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
            VertexElement tex3d = decl.FindElementBySemantic(VertexElementSemantic.TexCoords, (ushort)texCoordSet);

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
                ushort source = binding.NextIndex;
                binding.SetBinding(source, buff3D);
                decl.AddElement(new VertexElement(source, 0, VertexElementType.Float3, VertexElementSemantic.TexCoords, (ushort)texCoordSet));
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

        #region Implementation of Resource

        /// <summary>
        ///		
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

                    throw new Exception("Unsupported mesh format '" + ext + "'");
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
        }

        /// <summary>
        ///		
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
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        // TODO: Create overload which takes no params and auto name the sub mesh
        public SubMesh CreateSubMesh(string name) {
            SubMesh subMesh = new SubMesh(name);

            // set the parent of the subMesh to us
            subMesh.Parent = this;

            // add to the list of child meshes
            subMeshList.Add(subMesh);

            return subMesh;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
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
        ///    Returns the number of bone matrices this mesh uses.
        /// </summary>
        /// <remarks>
        ///    Only applicable if HasSkeleton is true, for internal use only.
        /// </remarks>
        public int BoneMatrixCount {
            get {
                Debug.Assert(skeleton != null, "Skeleton not present.");
  
                return skeleton.BoneCount;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public AxisAlignedBox BoundingBox {
            get {
                //if(updateBounds)
                //	UpdateBounds();

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

        /*internal void UpdateBounds()
        {
            Vector3 min = new Vector3();
            Vector3 max = new Vector3();

            bool first = true;
            bool useShared = false;
            int vert = 0;

            // loop through sub meshes and get their bound info
            for(int i = 0; i < meshList.Count; i++)
            {
                SubMesh subMesh = meshList[i];

                if(subMesh.useSharedVertices)
                {
                    // skip this step and move on to use the shared vertex buffer
                    useShared = true;
                }
                else
                {
                    for (vert = 0; vert < subMesh.vertexBuffer.numVertices * 3; vert += (3 + subMesh.vertexBuffer.vertexStride))
                    {
                        if (first || mesh.vertexBuffer.vertices[vert] < min.x)
                        {
                            min.x = mesh.vertexBuffer.vertices[vert];
                        }
                        if (first || mesh.vertexBuffer.vertices[vert+1] < min.y)
                        {
                            min.y = mesh.vertexBuffer.vertices[vert+1];
                        }
                        if (first || mesh.vertexBuffer.vertices[vert+2] < min.z)
                        {
                            min.z = mesh.vertexBuffer.vertices[vert+2];
                        }
                        if (first || mesh.vertexBuffer.vertices[vert] > max.x)
                        {
                            max.x = mesh.vertexBuffer.vertices[vert];
                        }
                        if (first || mesh.vertexBuffer.vertices[vert+1] > max.y)
                        {
                            max.y = mesh.vertexBuffer.vertices[vert+1];
                        }
                        if (first || mesh.vertexBuffer.vertices[vert+2] > max.z)
                        {
                            max.z = mesh.vertexBuffer.vertices[vert+2];
                        }

                        first = false;
                    } // end for
                } // end if

                if(useShared)
                {
                    for (vert = 0; vert < sharedBuffer.numVertices * 3; vert += (3 + sharedBuffer.vertexStride))
                    {
                        if (first || sharedBuffer.vertices[vert] < min.x)
                        {
                            min.x = sharedBuffer.vertices[vert];
                        }
                        if (first || sharedBuffer.vertices[vert + 1] < min.y)
                        {
                            min.y = sharedBuffer.vertices[vert + 1];
                        }
                        if (first || sharedBuffer.vertices[vert + 2] < min.z)
                        {
                            min.z = sharedBuffer.vertices[vert + 2];
                        }
                        if (first || sharedBuffer.vertices[vert] > max.x)
                        {
                            max.x = sharedBuffer.vertices[vert];
                        }
                        if (first || sharedBuffer.vertices[vert + 1] > max.y)
                        {
                            max.y = sharedBuffer.vertices[vert + 1];
                        }
                        if (first || sharedBuffer.vertices[vert + 2] > max.z)
                        {
                            max.z = sharedBuffer.vertices[vert + 2];
                        }

                        first = false;
                    }
                }
            } // end for

            // set the extents of the bounding box
            boundingBox.SetExtents(min, max);
            updateBounds = false;
        } */

        /// <summary>
        /// 
        /// </summary>
        /// <param name="usage"></param>
        /// <param name="useShadowBuffer"></param>
        /// DOC
        public void SetVertexBufferPolicy(BufferUsage usage, bool useShadowBuffer) {
            vertexBufferUsage = usage;
            useVertexShadowBuffer = useShadowBuffer;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="usage"></param>
        /// <param name="useShadowBuffer"></param>
        /// DOC
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
