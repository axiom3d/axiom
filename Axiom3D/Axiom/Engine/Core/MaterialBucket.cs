using System;
using System.Collections;
using System.Text;
using System.IO;

using Axiom.MathLib;

namespace Axiom
{

    public class MaterialBucket
    {
        #region Fields and Properties
        protected LODBucket parent;
        protected string materialName;
        protected Material material;
        protected Technique technique;

        protected GeometryBucketList geometryBucketList;
        protected CurrentGeometryMap currentGeometryMap;
        public string MaterialName
        {
            get
            {
                return materialName;
            }
        }

        public LODBucket Parent
        {
            get
            {
                return parent;
            }
        }

        public Material Material
        {
            get
            {
                return material;
            }
        }

        public Technique CurrentTechnique
        {
            get
            {
                return technique;
            }
        }

        #endregion

        #region Constructors

        public MaterialBucket( LODBucket parent, string materialName )
        {
            this.parent = parent;
            this.materialName = materialName;
        }

        #endregion

        #region Proteced Methods
        protected string GetGeometryFormatString( SubMeshLodGeometryLink geom )
        {
            // Formulate an identifying string for the geometry format
            // Must take into account the vertex declaration and the index type
            // Format is (all lines separated by '|'):
            // Index type
            // Vertex element (repeating)
            //   source
            //   semantic
            //   type
            string str = string.Format( "{0}|", geom.indexData.indexBuffer.Type );

            for ( int i = 0; i < geom.vertexData.vertexDeclaration.ElementCount; ++i )
            {
                VertexElement elem = geom.vertexData.vertexDeclaration.GetElement( i );
                str += string.Format( "{0}|{0}|{1}|{2}|", elem.Source, elem.Semantic, elem.Type );
            }
            return str;
        }

        #endregion

        #region Public Methods
        public void Assign( QueuedGeometry qgeom )
        {
            // Look up any current geometry
            string formatString = GetGeometryFormatString( qgeom.geometry );
            bool newBucket = true;
            if ( currentGeometryMap.ContainsKey( formatString ) )
            {
                GeometryBucket gbucket = (GeometryBucket)currentGeometryMap[formatString];
                // Found existing geometry, try to assign
                newBucket = !gbucket.Assign( qgeom );
                // Note that this bucket will be replaced as the 'current'
                // for this format string below since it's out of space
            }
            // Do we need to create a new one?
            if ( newBucket )
            {
                GeometryBucket gbucket = new GeometryBucket( this, formatString, qgeom.geometry.vertexData, qgeom.geometry.indexData );
                // Add to main list
                geometryBucketList.Add( gbucket );
                // Also index in 'current' list
                currentGeometryMap[formatString] = gbucket;
                if ( !gbucket.Assign( qgeom ) )
                {
                    throw new AxiomException( "Somehow we couldn't fit the requested geometry even in a " +
                        "brand new GeometryBucket!! Must be a bug, please report." );
                }
            }
        }

        public void Build( bool stencilShadows )
        {
            material = MaterialManager.Instance.GetByName( materialName );
            if ( null == material )
            {
                throw new AxiomException( "Material '{0}' not found.", materialName );
            }
            material.Load();
            // tell the geometry buckets to build
            IEnumerator iter = geometryBucketList.GetEnumerator();
            while ( iter.MoveNext() )
            {
                GeometryBucket gbucket = (GeometryBucket)iter.Current;
                gbucket.Build( stencilShadows );
            }
        }

        public void AddRenderables( RenderQueue queue, RenderQueueGroupID group, float camDistanceSquared )
        {
            // determine the current material technique
            technique = material.GetTechnique( material.GetLodIndexSquaredDepth( camDistanceSquared ) );

            IEnumerator iter = geometryBucketList.GetEnumerator();
            while ( iter.MoveNext() )
            {
                GeometryBucket gbucket = (GeometryBucket)iter.Current;
                queue.AddRenderable( gbucket, RenderQueue.DEFAULT_PRIORITY, group );
            }
        }

        public IEnumerator GetGeometryEnumerator()
        {
            return geometryBucketList.GetEnumerator();
        }

        public void Dump( TextWriter output )
        {
            output.WriteLine( "Material Bucket {0}", materialName );
            output.WriteLine( "--------------------------------------------------" );
            output.WriteLine( "Geometry buckets: {0}", geometryBucketList.Count );
            for ( int i = 0; i < geometryBucketList.Count; ++i )
            {
                GeometryBucket gbucket = (GeometryBucket)geometryBucketList[i];
                gbucket.Dump( output );
            }
            output.WriteLine( "--------------------------------------------------" );
        }
        #endregion
    }

}
