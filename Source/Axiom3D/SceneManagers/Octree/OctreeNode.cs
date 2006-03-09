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
using Axiom;
using Axiom.Core;
using Axiom.MathLib;

namespace Axiom.SceneManagers.Octree
{
    /// <summary>
    /// Summary description for OctreeNode.
    /// </summary>
    public class OctreeNode : SceneNode
    {
        #region Member Variables
        protected static long green = 0xFFFFFFFF;

        protected ushort[] Indexes = { 0, 1, 1, 2, 2, 3, 3, 0, 0, 6, 6, 5, 5, 1, 3, 7, 7, 4, 4, 2, 6, 7, 5, 4 };
        protected long[] Colors = { green, green, green, green, green, green, green, green };
        protected Octree octant = null;
        //protected SceneManager scene;
        protected AxisAlignedBox localAABB = new AxisAlignedBox();
        //protected OctreeSceneManager creator;

        protected NodeCollection children = new NodeCollection();
        #endregion

        #region Properties

        /// <summary>
        /// 
        /// </summary>
        public AxisAlignedBox LocalAABB
        {
            get
            {
                return localAABB;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public Octree Octant
        {
            get
            {
                return octant;
            }
            set
            {
                octant = value;
            }
        }

        #endregion

        #region Methods

        public OctreeNode( SceneManager scene ) : base( scene )
        {
        }

        public OctreeNode( SceneManager scene, string name ) : base( scene, name )
        {
        }

        /// <summary>
        ///     Remove all the children nodes as well from the octree.
        ///	</summary>
        public void RemoveNodeAndchildren()
        {
            int i;

            OctreeSceneManager man = (OctreeSceneManager)this.creator;
            man.RemoveOctreeNode( this );

            for ( i = 0; i < children.Count; i++ )
            {
                //OctreeNode child = (OctreeNode)Childern[i];

                OctreeNode child = (OctreeNode)RemoveChild( i );
                child.RemoveNodeAndchildren();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public override Node RemoveChild( int index )
        {
            OctreeNode child = (OctreeNode)base.RemoveChild( index );
            child.RemoveNodeAndchildren();
            return child;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public override Node RemoveChild( string childName )
        {
            OctreeNode child = (OctreeNode)base.RemoveChild( childName );
            if ( child == null )
                throw new ArgumentException( string.Format( "There is no child with the name '{0}' to remove from node '{1}'.", childName, this.name ) );
            child.RemoveNodeAndchildren();
            return child;
        }

        /// <summary>
        ///     Determines if the center of this node is within the given box.
        /// </summary>
        /// <param name="box"></param>
        /// <returns></returns>
        public bool IsInBox( AxisAlignedBox box )
        {
            Vector3 center = worldAABB.Maximum.MidPoint( worldAABB.Minimum );
            Vector3 min = box.Minimum;
            Vector3 max = box.Maximum;

            return ( max > center && min < center );
        }

        /// <summary>
        ///     Adds all the attached scenenodes to the render queue.
        /// </summary>
        /// <param name="cam"></param>
        /// <param name="queue"></param>
        public void AddToRenderQueue( Camera cam, RenderQueue queue )
        {

            int i;
            for ( i = 0; i < objectList.Count; i++ )
            {
                MovableObject obj = (MovableObject)objectList[i];
                obj.NotifyCurrentCamera( cam );

                if ( obj.IsVisible )
                {
                    obj.UpdateRenderQueue( queue );
                }
            }
        }

        /// <summary>
        ///     Same as SceneNode, only it doesn't care about children...
        /// </summary>
        protected override void UpdateBounds()
        {
            //update bounds from attached objects
            for ( int i = 0; i < objectList.Count; i++ )
            {
                MovableObject obj = objectList[i];

                localAABB.Merge( obj.BoundingBox );

                worldAABB = obj.GetWorldBoundingBox( true );
            }

            if ( !worldAABB.IsNull )
            {
                OctreeSceneManager oManager = (OctreeSceneManager)this.creator;
                oManager.UpdateOctreeNode( this );
            }
        }
    }
        #endregion
}