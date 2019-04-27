#region LGPL License

/*
Axiom Graphics Engine Library
Copyright © 2003-2011 Axiom Project Team

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

#region SVN Version Information

// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.IO;
using Axiom.Graphics;
using Axiom.Math;
using Axiom.Collections;
using Axiom.Graphics.Collections;
using Axiom.Core.Collections;
using static Axiom.Math.Utility;

#endregion Namespace Declarations

namespace Axiom.Core
{
    public partial class StaticGeometry
    {
        /// <summary>
        /// The details of a topological region which is the highest level of
        /// partitioning for this class.
        /// </summary>
        /// <remarks>
        /// The size &amp; shape of regions entirely depends on the SceneManager
        /// specific implementation. It is a MovableObject since it will be
        /// attached to a node based on the local centre - in practice it
        /// won't actually move (although in theory it could).
        /// </remarks>
        public class Region : MovableObject, IDisposable
        {
            #region Inner Classes

            public class RegionShadowRenderable : ShadowRenderable
            {
                protected Region parent;
                protected HardwareVertexBuffer positionBuffer;
                protected HardwareVertexBuffer wBuffer;

                public RegionShadowRenderable(Region parent, HardwareIndexBuffer indexBuffer, VertexData vertexData,
                                               bool createSeparateLightCap, bool isLightCap)
                {
                    throw new NotImplementedException();
                }

                public RegionShadowRenderable(Region parent, HardwareIndexBuffer indexBuffer, VertexData vertexData,
                                               bool createSeparateLightCap)
                    : this(parent, indexBuffer, vertexData, createSeparateLightCap, false)
                {
                }

                public HardwareVertexBuffer PositionBuffer
                {
                    get
                    {
                        return this.positionBuffer;
                    }
                }

                public HardwareVertexBuffer WBuffer
                {
                    get
                    {
                        return this.wBuffer;
                    }
                }

                public override void GetWorldTransforms(Matrix4[] matrices)
                {
                    matrices[0] = this.parent.ParentNodeFullTransform;
                }

                public override Quaternion WorldOrientation
                {
                    get
                    {
                        return this.parent.ParentNode.DerivedOrientation;
                    }
                }

                public override Vector3 WorldPosition
                {
                    get
                    {
                        return this.parent.Center;
                    }
                }
            }

            #endregion

            #region Fields and Properties

            protected StaticGeometry parent;
            protected SceneManager sceneMgr;
            protected SceneNode node;
            protected List<QueuedSubMesh> queuedSubMeshes;
            protected UInt32 regionID;
            protected Vector3 center;
            protected LodValueList lodValues;
            protected LodStrategy lodStrategy;
            protected AxisAlignedBox aabb;
            protected Real boundingRadius;
            protected ushort currentLod;
            protected float camDistanceSquared;
            protected List<LODBucket> lodBucketList;
            protected EdgeData edgeList;
            protected ShadowRenderableList shadowRenderables;
            protected bool vertexProgramInUse;

            public StaticGeometry Parent
            {
                get
                {
                    return this.parent;
                }
            }

            public UInt32 ID
            {
                get
                {
                    return this.regionID;
                }
            }

            public Vector3 Center
            {
                get
                {
                    return this.center;
                }
            }

            public override AxisAlignedBox BoundingBox
            {
                get
                {
                    return this.aabb;
                }
            }

            // TODO: Is this right?
            public ushort NumWorldTransforms
            {
                get
                {
                    return 1;
                }
            }

            public override Real BoundingRadius
            {
                get
                {
                    return this.boundingRadius;
                }
            }

            public LightList Lights
            {
                get
                {
                    // Make sure we only update this once per frame no matter how many
                    // times we're asked
                    var frame = Root.Instance.CurrentFrameCount;
                    if (frame > lightListUpdated)
                    {
                        lightList = this.node.FindLights(this.boundingRadius);
                        lightListUpdated = frame;
                    }
                    return lightList;
                }
            }

            public EdgeData EdgeList
            {
                get
                {
                    return this.edgeList;
                }
            }

            public List<LODBucket> LodBucketList
            {
                get
                {
                    return this.lodBucketList;
                }
            }

            #endregion

            #region Constructors

            public Region(StaticGeometry parent, string name, SceneManager mgr, UInt32 regionID, Vector3 center)
                : base(name)
            {
                MovableType = "StaticGeometry";
                this.parent = parent;
                this.sceneMgr = mgr;
                this.regionID = regionID;
                this.center = center;
                this.queuedSubMeshes = new List<QueuedSubMesh>();
                this.lodValues = new LodValueList();
                this.aabb = new AxisAlignedBox();
                this.lodBucketList = new List<LODBucket>();
                this.shadowRenderables = new ShadowRenderableList();
            }

            #endregion

            #region Public Methods

            public void Assign(QueuedSubMesh qsm)
            {
                this.queuedSubMeshes.Add(qsm);

                // update lod distances
                var mesh = qsm.submesh.Parent;
                var lodStrategy = mesh.LodStrategy;
                if (this.lodStrategy == null)
                {
                    this.lodStrategy = lodStrategy;
                    // First LOD mandatory, and always from base lod value
                    this.lodValues.Add(this.lodStrategy.BaseValue);
                }
                else
                {
                    if (this.lodStrategy != lodStrategy)
                    {
                        throw new AxiomException("Lod strategies do not match.");
                    }
                }

                var lodLevels = mesh.LodLevelCount;
                if (qsm.geometryLodList.Count != lodLevels)
                {
                    var msg = string.Format("QueuedSubMesh '{0}' lod count of {1} does not match parent count of {2}",
                                             qsm.submesh.Name, qsm.geometryLodList.Count, lodLevels);
                    throw new AxiomException(msg);
                }

                while (this.lodValues.Count < lodLevels)
                {
                    this.lodValues.Add(0.0f);
                }
                // Make sure LOD levels are max of all at the requested level
                for (ushort lod = 1; lod < lodLevels; ++lod)
                {
                    var meshLod = qsm.submesh.Parent.GetLodLevel(lod);
                    this.lodValues[lod] = Max((float)this.lodValues[lod], meshLod.Value);
                }

                // update bounds
                // Transform world bounds relative to our center
                var localBounds = new AxisAlignedBox(qsm.worldBounds.Minimum - this.center, qsm.worldBounds.Maximum - this.center);
                this.aabb.Merge(localBounds);
                foreach (var corner in localBounds.Corners)
                {
                    this.boundingRadius = Max(this.boundingRadius, corner.Length);
                }
            }

            public void Build(bool stencilShadows, int logLevel)
            {
                // Create a node
                this.node = this.sceneMgr.RootSceneNode.CreateChildSceneNode(name, this.center);
                this.node.AttachObject(this);
                // We need to create enough LOD buckets to deal with the highest LOD
                // we encountered in all the meshes queued
                for (ushort lod = 0; lod < this.lodValues.Count; ++lod)
                {
                    var lodBucket = new LODBucket(this, lod, (float)this.lodValues[lod]);
                    this.lodBucketList.Add(lodBucket);
                    // Now iterate over the meshes and assign to LODs
                    // LOD bucket will pick the right LOD to use
                    IEnumerator iter = this.queuedSubMeshes.GetEnumerator();
                    while (iter.MoveNext())
                    {
                        var qsm = (QueuedSubMesh)iter.Current;
                        lodBucket.Assign(qsm, lod);
                    }
                    // now build
                    lodBucket.Build(stencilShadows, logLevel);
                }

                // Do we need to build an edge list?
                if (stencilShadows)
                {
                    var eb = new EdgeListBuilder();
                    //int vertexSet = 0;
                    foreach (var lod in this.lodBucketList)
                    {
                        foreach (var mat in lod.MaterialBucketMap.Values)
                        {
                            // Check if we have vertex programs here
                            var t = mat.Material.GetBestTechnique();
                            if (null != t)
                            {
                                var p = t.GetPass(0);
                                if (null != p)
                                {
                                    if (p.HasVertexProgram)
                                    {
                                        this.vertexProgramInUse = true;
                                    }
                                }
                            }

                            foreach (var geom in mat.GeometryBucketList)
                            {
                                // Check we're dealing with 16-bit indexes here
                                // Since stencil shadows can only deal with 16-bit
                                // More than that and stencil is probably too CPU-heavy
                                // in any case
                                if (geom.IndexData.indexBuffer.Type != IndexType.Size16)
                                {
                                    throw new AxiomException("Only 16-bit indexes allowed when using stencil shadows");
                                }
                                eb.AddVertexData(geom.VertexData);
                                eb.AddIndexData(geom.IndexData);
                            }
                        }
                    }
                    this.edgeList = eb.Build();
                }
            }

            public override void NotifyCurrentCamera(Camera cam)
            {
                // Determine active lod
                var diff = cam.DerivedPosition - this.center;
                // Distance from the edge of the bounding sphere
                this.camDistanceSquared = diff.LengthSquared - this.boundingRadius * this.boundingRadius;
                // Clamp to 0
                this.camDistanceSquared = Max(0.0f, this.camDistanceSquared);

                var maxDist = this.parent.SquaredRenderingDistance;
                if (this.parent.RenderingDistance > 0 && this.camDistanceSquared > maxDist && cam.UseRenderingDistance)
                {
                    beyondFarDistance = true;
                }
                else
                {
                    beyondFarDistance = false;

                    this.currentLod = (ushort)(this.lodValues.Count - 1);
                    for (ushort i = 0; i < this.lodValues.Count; ++i)
                    {
                        if ((float)this.lodValues[i] > this.camDistanceSquared)
                        {
                            this.currentLod = (ushort)(i - 1);
                            break;
                        }
                    }
                }
            }

            public override void UpdateRenderQueue(RenderQueue queue)
            {
                var lodBucket = this.lodBucketList[this.currentLod];
                lodBucket.AddRenderables(queue, renderQueueID, this.camDistanceSquared);
            }

            public override bool IsVisible
            {
                get
                {
                    return isVisible && !beyondFarDistance;
                }
            }

            public IEnumerator GetShadowVolumeRenderableIterator(ShadowTechnique shadowTechnique, Light light,
                                                                  HardwareIndexBuffer indexBuffer, bool extrudeVertices,
                                                                  float extrusionDistance, ulong flags)
            {
                Debug.Assert(indexBuffer != null, "Only external index buffers are supported right now");
                Debug.Assert(indexBuffer.Type == IndexType.Size16, "Only 16-bit indexes supported for now");

                // Calculate the object space light details
                var lightPos = light.GetAs4DVector();
                var world2Obj = parentNode.FullTransform.Inverse();
                lightPos = world2Obj * lightPos;

                // We need to search the edge list for silhouette edges
                if (this.edgeList == null)
                {
                    throw new Exception("You enabled stencil shadows after the buid process!  In " +
                                         "Region.GetShadowVolumeRenderableIterator");
                }

                // Init shadow renderable list if required
                var init = this.shadowRenderables.Count == 0;

                RegionShadowRenderable esr = null;
                //bool updatedSharedGeomNormals = false;
                for (var i = 0; i < this.edgeList.EdgeGroups.Count; i++)
                {
                    var group = (EdgeData.EdgeGroup)this.edgeList.EdgeGroups[i];
                    if (init)
                    {
                        // Create a new renderable, create a separate light cap if
                        // we're using a vertex program (either for this model, or
                        // for extruding the shadow volume) since otherwise we can
                        // get depth-fighting on the light cap
                        esr = new RegionShadowRenderable(this, indexBuffer, group.vertexData, this.vertexProgramInUse || !extrudeVertices);
                        this.shadowRenderables.Add(esr);
                    }
                    else
                    {
                        esr = (RegionShadowRenderable)this.shadowRenderables[i];
                    }
                    // Extrude vertices in software if required
                    if (extrudeVertices)
                    {
                        ExtrudeVertices(esr.PositionBuffer, group.vertexData.vertexCount, lightPos, extrusionDistance);
                    }
                }
                return (IEnumerator)this.shadowRenderables;
            }

            public void Dump()
            {
                LogManager.Instance.Write("Region {0}", this.regionID);
                LogManager.Instance.Write("--------------------------");
                LogManager.Instance.Write("Center: {0}", this.center);
                LogManager.Instance.Write("Local AABB: {0}", this.aabb);
                LogManager.Instance.Write("Bounding radius: {0}", this.boundingRadius);
                LogManager.Instance.Write("Number of LODs: {0}", this.lodBucketList.Count);
                foreach (var lodBucket in this.lodBucketList)
                {
                    lodBucket.Dump();
                }
                LogManager.Instance.Write("--------------------------");
            }

            #endregion

            /// <summary>
            ///     Remove the region from the scene graph
            /// </summary>
            protected override void dispose(bool disposeManagedResources)
            {
                if (!IsDisposed)
                {
                    if (disposeManagedResources)
                    {
                        if (this.node != null)
                        {
                            this.node.RemoveFromParent();
                            this.sceneMgr.DestroySceneNode(this.node);
                            this.node = null;
                        }

                        foreach (var lodBucket in this.lodBucketList)
                        {
                            lodBucket.Dispose();
                        }
                        this.lodBucketList.Clear();
                    }
                }
            }

            #region MovableObject Implementation

            /// <summary>
            /// Get the 'type flags' for this <see cref="Region"/>.
            /// </summary>
            /// <seealso cref="MovableObject.TypeFlags"/>
            public override uint TypeFlags
            {
                get
                {
                    return (uint)SceneQueryTypeMask.StaticGeometry;
                }
            }

            #endregion MovableObject Implementation
        }
    }
}