using System;
using System.Collections;
using System.Text;
using System.IO;

using Axiom.MathLib;

namespace Axiom
{

    public class LODBucket
    {
        #region Fields and Properties
        protected Region parent;
        protected ushort lod;
        protected float squaredDistance;
        protected MaterialBucketMap materialBucketMap;
        protected QueuedGeometryList queuedGeometryList;

        public Region Parent
        {
            get
            {
                return parent;
            }
        }

        public ushort Lod
        {
            get
            {
                return lod;
            }
        }

        float SquaredDistance
        {
            get
            {
                return squaredDistance;
            }
        }

        #endregion

        #region Constructors
        public LODBucket( Region parent, ushort lod, float lodDist )
        {
            this.parent = parent;
            this.lod = lod;
            this.squaredDistance = lodDist;
        }
        #endregion

        #region Public Methods

        public void Assign( QueuedSubMesh qsm, ushort atlod )
        {
            QueuedGeometry q = new QueuedGeometry();
            queuedGeometryList.Add( q );
            q.position = qsm.position;
            q.orientation = qsm.orientation;
            q.scale = qsm.scale;
            if ( qsm.geometryLodList.Count > atlod )
            {
                // This submesh has enough lods, use the right one
                q.geometry = (SubMeshLodGeometryLink)qsm.geometryLodList[atlod];
            }
            else
            {
                // Not enough lods, use the lowest one we have
                q.geometry = (SubMeshLodGeometryLink)qsm.geometryLodList[qsm.geometryLodList.Count - 1];
            }
            // Locate a material bucket
            MaterialBucket mbucket;
            if ( materialBucketMap.ContainsKey( qsm.materialName ) )
            {
                mbucket = (MaterialBucket)materialBucketMap[qsm.materialName];
            }
            else
            {
                mbucket = new MaterialBucket( this, qsm.materialName );
                materialBucketMap[qsm.materialName] = mbucket;
            }
            mbucket.Assign( q );
        }

        public void Build( bool stencilShadows )
        {
            // Just pass this on to child buckets
            IDictionaryEnumerator iter = materialBucketMap.GetEnumerator();
            while ( iter.MoveNext() )
            {
                MaterialBucket mbucket = (MaterialBucket)iter.Value;
                mbucket.Build( stencilShadows );
            }
        }

        public void AddRenderables( RenderQueue queue, RenderQueueGroupID group, float camSquaredDistance )
        {
            // Just pass this on to child buckets
            IDictionaryEnumerator iter = materialBucketMap.GetEnumerator();
            while ( iter.MoveNext() )
            {
                MaterialBucket mbucket = (MaterialBucket)iter.Value;
                mbucket.AddRenderables( queue, group, camSquaredDistance );
            }
        }

        public IDictionaryEnumerator GetMaterialEnumerator()
        {
            return materialBucketMap.GetEnumerator();
        }

        public void Dump( TextWriter output )
        {
            output.WriteLine( "LOD Bucket {0}", lod );
            output.WriteLine( "------------------" );
            output.WriteLine( "Distance: {0}", Math.Sqrt( squaredDistance ) );
            output.WriteLine( "Number of Materials: {0}", materialBucketMap.Count );
            IDictionaryEnumerator iter = materialBucketMap.GetEnumerator();
            while ( iter.MoveNext() )
            {
                MaterialBucket mbucket = (MaterialBucket)iter.Value;
                mbucket.Dump( output );
            }
            output.WriteLine( "------------------" );
        }
        #endregion
    }

}
