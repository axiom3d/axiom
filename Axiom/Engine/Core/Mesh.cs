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
using Axiom.Animating;
using Axiom.Collections;
using Axiom.Configuration;
using Axiom.MathLib;
using Axiom.Serialization;
using Axiom.SubSystems.Rendering;

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
        #region Member variables

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
        protected SortedList boneAssignmentList = new SortedList();
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
        ///    This method is only valid after calling setSkeletonName.
        ///    You should not need to modify bone assignments during rendering (only the positions of bones) 
        ///    and the engine reserves the right to do some internal data reformatting of this information, 
        ///    depending on render system requirements.
        /// </remarks>
        /// <param name="boneAssignment"></param>
        public void AddBoneAssignment(ref VertexBoneAssignment boneAssignment) {
            boneAssignmentList.Add(boneAssignment.vertexIndex, boneAssignment);
            boneAssignmentsOutOfDate = true;
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
            ushort maxBones = 0;
            ushort currentBones = 0;
            ushort lastVertexIndex = ushort.MaxValue;

            // find the largest number of bones per vertex
            for(int i = 0; i < boneAssignmentList.Count; i++) {
                VertexBoneAssignment boneAssignment =
                    (VertexBoneAssignment)boneAssignmentList.GetByIndex(i);

                if(lastVertexIndex != boneAssignment.vertexIndex) {
                    if(maxBones < currentBones) {
                        maxBones = currentBones;
                    }
                    currentBones = 0;
                } // if

                currentBones++;

                lastVertexIndex = (ushort)boneAssignment.vertexIndex;
            } // for

            if(maxBones > Config.MaxBlendWeights) {
                throw new Exception("Mesh '" + name + "' has too many bone assignments per vertex.");
            }

            numBlendWeightsPerVertex = (short)maxBones;

            // no bone assignments?  get outta here
            if(numBlendWeightsPerVertex == 0) {
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
        protected internal void CompileBoneAssignmentsSoftware(SortedList boneAssignments, short numBlendWeightsPerVertex, VertexData targetVertexData) {
            SoftwareBlendInfo blendInfo = targetVertexData.softwareBlendInfo;

            // create new data buffers
            blendInfo.blendIndices = 
                new byte[targetVertexData.vertexCount * numBlendWeightsPerVertex];
            blendInfo.blendWeights = 
                new float[targetVertexData.vertexCount * numBlendWeightsPerVertex];

            // get the first element of the bone assignment list
            VertexBoneAssignment boneAssignment = 
                (VertexBoneAssignment)boneAssignments.GetByIndex(0);

            int index = 0;
            int i = 0;
                
            // interate through each vertex
            for(int v = 0; v < targetVertexData.vertexCount; v++) {
                for(int b = 0; b < numBlendWeightsPerVertex; b++) {
                    if(boneAssignment.vertexIndex == v) {
                        blendInfo.blendWeights[index] = boneAssignment.weight;
                        blendInfo.blendIndices[index] = (byte)boneAssignment.boneIndex;
                        if(i++ < targetVertexData.vertexCount - 1) {
                            boneAssignment = (VertexBoneAssignment)boneAssignments.GetByIndex(i);
                        }
                    }
                    else {
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
        protected internal void CompileBoneAssignmentsHardware(SortedList boneAssignments, short numBlendWeightsPerVertex, VertexData targetVertexData) {
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
        ///		Gets/Sets the shared VertexData for this mesh.
        /// </summary>
        public VertexData SharedVertexData {
            get { return sharedVertexData; }
            set { sharedVertexData = value; }
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
