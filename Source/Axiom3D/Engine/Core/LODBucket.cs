#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006  Axiom Project Team

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
using System.Text;
using System.IO;

using Axiom.Math;

#endregion Namespace Declarations
			
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
            output.WriteLine( "Distance: {0}", Utility.Sqrt( squaredDistance ) );
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
