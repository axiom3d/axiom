#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006 Axiom Project Team

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
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;

using Axiom.Math;
using Axiom.Graphics;

#endregion Namespace Declarations

namespace Axiom.Core
{
    /// <summary>
    /// Summary description for WireBoundingBox.
    /// </summary>
    public sealed class WireBoundingBox : SimpleRenderable
    {
        #region Field and Properties

        private float radius;

        #endregion Field and Properties

        #region Constants

        const int POSITION = 0;
 
        #endregion Constants

        #region Constructors

        /// <summary>
        ///    Default constructor.
        /// </summary>
        public WireBoundingBox()
        {
            vertexData = new VertexData();
            vertexData.vertexCount = 24;
            vertexData.vertexStart = 0;

            // get a reference to the vertex declaration and buffer binding
            VertexDeclaration decl = vertexData.vertexDeclaration;
            VertexBufferBinding binding = vertexData.vertexBufferBinding;

            // add elements for position and color only
            decl.AddElement( POSITION, 0, VertexElementType.Float3, VertexElementSemantic.Position );

            // create a new hardware vertex buffer for the position data
            HardwareVertexBuffer buffer =
                HardwareBufferManager.Instance.CreateVertexBuffer(
                decl.GetVertexSize( POSITION ),
                vertexData.vertexCount,
                BufferUsage.StaticWriteOnly );

            // bind the position buffer
            binding.SetBinding( POSITION, buffer );

			this.Material = (Material)MaterialManager.Instance[ "BaseWhiteNoLighting" ];

        }

        #endregion Constructors

        #region Implementation of SimpleRenderable

        /// <summary>
        /// 
        /// </summary>
        /// <param name="matrices"></param>
        public override void GetWorldTransforms( Matrix4[] matrices )
        {
            // return identity matrix to prevent parent transforms
            matrices[ 0 ] = Matrix4.Identity;
        }

        public void InitAABB( AxisAlignedBox box )
        {
            SetupAABBVertices( box );

            // store the bounding box locally
            this.box = box;
        }

        private void SetupAABBVertices( AxisAlignedBox aab )
        {
            Vector3 max = aab.Maximum;
            Vector3 min = aab.Minimum;

            // set bounding sphere radius
            float lengthSquared = Utility.Max( max.LengthSquared, min.LengthSquared );
            radius = Utility.Sqrt( lengthSquared );

            float maxx = max.x;
            float maxy = max.y;
            float maxz = max.z;

            float minx = min.x;
            float miny = min.y;
            float minz = min.z;

            int i = 0;

            HardwareVertexBuffer buffer = vertexData.vertexBufferBinding.GetBuffer( POSITION );

            IntPtr posPtr = buffer.Lock( BufferLocking.Discard );

            unsafe
            {
                float* pPos = (float*)posPtr.ToPointer();

                // fill in the Vertex array: 12 lines with 2 endpoints each make up a box
                // line 0
                pPos[ i++ ] = minx;
                pPos[ i++ ] = miny;
                pPos[ i++ ] = minz;
                pPos[ i++ ] = maxx;
                pPos[ i++ ] = miny;
                pPos[ i++ ] = minz;
                // line 1
                pPos[ i++ ] = minx;
                pPos[ i++ ] = miny;
                pPos[ i++ ] = minz;
                pPos[ i++ ] = minx;
                pPos[ i++ ] = miny;
                pPos[ i++ ] = maxz;
                // line 2
                pPos[ i++ ] = minx;
                pPos[ i++ ] = miny;
                pPos[ i++ ] = minz;
                pPos[ i++ ] = minx;
                pPos[ i++ ] = maxy;
                pPos[ i++ ] = minz;
                // line 3
                pPos[ i++ ] = minx;
                pPos[ i++ ] = maxy;
                pPos[ i++ ] = minz;
                pPos[ i++ ] = minx;
                pPos[ i++ ] = maxy;
                pPos[ i++ ] = maxz;
                // line 4
                pPos[ i++ ] = minx;
                pPos[ i++ ] = maxy;
                pPos[ i++ ] = minz;
                pPos[ i++ ] = maxx;
                pPos[ i++ ] = maxy;
                pPos[ i++ ] = minz;
                // line 5
                pPos[ i++ ] = maxx;
                pPos[ i++ ] = miny;
                pPos[ i++ ] = minz;
                pPos[ i++ ] = maxx;
                pPos[ i++ ] = miny;
                pPos[ i++ ] = maxz;
                // line 6
                pPos[ i++ ] = maxx;
                pPos[ i++ ] = miny;
                pPos[ i++ ] = minz;
                pPos[ i++ ] = maxx;
                pPos[ i++ ] = maxy;
                pPos[ i++ ] = minz;
                // line 7
                pPos[ i++ ] = minx;
                pPos[ i++ ] = maxy;
                pPos[ i++ ] = maxz;
                pPos[ i++ ] = maxx;
                pPos[ i++ ] = maxy;
                pPos[ i++ ] = maxz;
                // line 8
                pPos[ i++ ] = minx;
                pPos[ i++ ] = maxy;
                pPos[ i++ ] = maxz;
                pPos[ i++ ] = minx;
                pPos[ i++ ] = miny;
                pPos[ i++ ] = maxz;
                // line 9
                pPos[ i++ ] = maxx;
                pPos[ i++ ] = maxy;
                pPos[ i++ ] = minz;
                pPos[ i++ ] = maxx;
                pPos[ i++ ] = maxy;
                pPos[ i++ ] = maxz;
                // line 10
                pPos[ i++ ] = maxx;
                pPos[ i++ ] = miny;
                pPos[ i++ ] = maxz;
                pPos[ i++ ] = maxx;
                pPos[ i++ ] = maxy;
                pPos[ i++ ] = maxz;
                // line 11
                pPos[ i++ ] = minx;
                pPos[ i++ ] = miny;
                pPos[ i++ ] = maxz;
                pPos[ i++ ] = maxx;
                pPos[ i++ ] = miny;
                pPos[ i++ ] = maxz;
            }

            // unlock the buffer
            buffer.Unlock();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="camera"></param>
        /// <returns></returns>
        public override float GetSquaredViewDepth( Camera camera )
        {
            Vector3 min, max, mid, dist;
            min = box.Minimum;
            max = box.Maximum;
            mid = ( ( max - min ) * 0.5f ) + min;
            dist = camera.DerivedPosition - mid;

            return dist.LengthSquared;
        }

        /// <summary>
        ///    Gets the rendering operation required to render the wire box.
        /// </summary>
        /// <param name="op">A reference to a precreate RenderOpertion to be modifed here.</param>
        public override void GetRenderOperation( RenderOperation op )
        {
            op.vertexData = vertexData;
            op.indexData = null;
            op.operationType = OperationType.LineList;
            op.useIndices = false;
        }

        /// <summary>
        ///    Get the local bounding radius of the wire bounding box.
        /// </summary>
        public override float BoundingRadius
        {
            get
            {
                return radius;
            }
        }

        #endregion
    }
}
