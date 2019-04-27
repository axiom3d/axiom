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
using System.Text;
using System.IO;
using Axiom.Graphics;
using Axiom.Math;
using Axiom.Collections;

#endregion Namespace Declarations

namespace Axiom.Core
{
    public partial class StaticGeometry
    {
        /// <summary>
        /// A MaterialBucket is a collection of smaller buckets with the same 
        /// Material (and implicitly the same LOD).
        /// </summary>
        public class MaterialBucket : DisposableObject
        {
            #region Fields and Properties

            protected LODBucket parent;
            protected string materialName;
            protected Material material;
            protected Technique technique;

            protected List<GeometryBucket> geometryBucketList;
            protected Dictionary<string, GeometryBucket> currentGeometryMap;

            public string MaterialName
            {
                get
                {
                    return this.materialName;
                }
            }

            public LODBucket Parent
            {
                get
                {
                    return this.parent;
                }
            }

            public Material Material
            {
                get
                {
                    return this.material;
                }
            }

            public Technique CurrentTechnique
            {
                get
                {
                    return this.technique;
                }
            }

            public List<GeometryBucket> GeometryBucketList
            {
                get
                {
                    return this.geometryBucketList;
                }
            }

            #endregion

            #region Constructors

            public MaterialBucket(LODBucket parent, string materialName)
                : base()
            {
                this.parent = parent;
                this.materialName = materialName;
                this.geometryBucketList = new List<GeometryBucket>();
                this.currentGeometryMap = new Dictionary<string, GeometryBucket>();
            }

            #endregion

            #region Proteced Methods

            protected string GetGeometryFormatString(SubMeshLodGeometryLink geom)
            {
                // Formulate an identifying string for the geometry format
                // Must take into account the vertex declaration and the index type
                // Format is (all lines separated by '|'):
                // Index type
                // Vertex element (repeating)
                //   source
                //   semantic
                //   type
                var str = string.Format("{0}|", geom.indexData.indexBuffer.Type);

                for (var i = 0; i < geom.vertexData.vertexDeclaration.ElementCount; ++i)
                {
                    var elem = geom.vertexData.vertexDeclaration.GetElement(i);
                    str += string.Format("{0}|{0}|{1}|{2}|", elem.Source, elem.Semantic, elem.Type);
                }
                return str;
            }

            #endregion

            #region Public Methods

            public void Assign(QueuedGeometry qgeom)
            {
                // Look up any current geometry
                var formatString = GetGeometryFormatString(qgeom.geometry);
                var newBucket = true;
                if (this.currentGeometryMap.ContainsKey(formatString))
                {
                    var gbucket = this.currentGeometryMap[formatString];
                    // Found existing geometry, try to assign
                    newBucket = !gbucket.Assign(qgeom);
                    // Note that this bucket will be replaced as the 'current'
                    // for this format string below since it's out of space
                }
                // Do we need to create a new one?
                if (newBucket)
                {
                    var gbucket = new GeometryBucket(this, formatString, qgeom.geometry.vertexData, qgeom.geometry.indexData);
                    // Add to main list
                    this.geometryBucketList.Add(gbucket);
                    // Also index in 'current' list
                    this.currentGeometryMap[formatString] = gbucket;
                    if (!gbucket.Assign(qgeom))
                    {
                        throw new AxiomException("Somehow we couldn't fit the requested geometry even in a " +
                                                  "brand new GeometryBucket!! Must be a bug, please report.");
                    }
                }
            }

            public void Build(bool stencilShadows, int logLevel)
            {
                if (logLevel <= 1)
                {
                    LogManager.Instance.Write("MaterialBucket.Build: Building material {0}", this.materialName);
                }
                this.material = (Material)MaterialManager.Instance[this.materialName];
                if (null == this.material)
                {
                    throw new AxiomException("Material '{0}' not found.", this.materialName);
                }
                this.material.Load();
                // tell the geometry buckets to build
                foreach (var gbucket in this.geometryBucketList)
                {
                    gbucket.Build(stencilShadows, logLevel);
                }
            }

            public void AddRenderables(RenderQueue queue, RenderQueueGroupID group, Real lodValue)
            {
                // Get batch instance
#warning OGRE-1.6 BatchInstance Implementation
                //BatchInstance batchInstance = Parent.Parent;

                // Get material lod strategy
                var materialLodStrategy = Material.LodStrategy;

                // If material strategy doesn't match, recompute lod value with correct strategy
#warning OGRE-1.6 BatchInstance Implementation needed
                //if ( materialLodStrategy != batchInstance.LodStrategy )
                //    lodValue = materialLodStrategy.GetValue( batchInstance, batchInstance.Camera );

                // determine the current material technique
                this.technique = this.material.GetBestTechnique(this.material.GetLodIndex(lodValue));
                foreach (var gbucket in this.geometryBucketList)
                {
                    queue.AddRenderable(gbucket, RenderQueue.DEFAULT_PRIORITY, group);
                }
            }

            public void Dump()
            {
                LogManager.Instance.Write("Material Bucket {0}", this.materialName);
                LogManager.Instance.Write("--------------------------------------------------");
                LogManager.Instance.Write("Geometry buckets: {0}", this.geometryBucketList.Count);
                foreach (var gbucket in this.geometryBucketList)
                {
                    gbucket.Dump();
                }
                LogManager.Instance.Write("--------------------------------------------------");
            }

            /// <summary>
            ///     Dispose the geometry buckets
            /// </summary>
            protected override void dispose(bool disposeManagedResources)
            {
                if (!IsDisposed)
                {
                    if (disposeManagedResources)
                    {
                        if (this.geometryBucketList != null)
                        {
                            foreach (var gbucket in this.geometryBucketList)
                            {
                                if (!gbucket.IsDisposed)
                                {
                                    gbucket.Dispose();
                                }
                            }
                            this.geometryBucketList.Clear();
                            this.geometryBucketList = null;
                        }


                        if (this.material != null)
                        {
                            if (!this.material.IsDisposed)
                            {
                                this.material.Dispose();
                            }

                            this.material = null;
                        }
                    }
                }

                base.dispose(disposeManagedResources);
            }

            #endregion
        }
    }
}