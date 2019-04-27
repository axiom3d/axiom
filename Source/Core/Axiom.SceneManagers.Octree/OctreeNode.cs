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

#endregion LGPL License

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using Axiom;
using Axiom.Collections;
using Axiom.Core;
using Axiom.Math;
using Axiom.Graphics;
using Axiom.Core.Collections;
using System.Collections.Generic;
using System.Diagnostics;

#endregion Namespace Declarations

namespace Axiom.SceneManagers.Octree
{
    /// <summary>
    /// Summary description for OctreeNode.
    /// </summary>
    public class OctreeNode : SceneNode
    {
        #region Member Variables

        protected static long green = 0xFFFFFFFF;

        protected short[] Indexes = {
                                        0, 1, 1, 2, 2, 3, 3, 0, 0, 6, 6, 5, 5, 1, 3, 7, 7, 4, 4, 2, 6, 7, 5, 4
                                    };

        protected long[] Colors = {
                                      green, green, green, green, green, green, green, green
                                  };

        protected Octree octant = null;
        protected AxisAlignedBox localAABB = new AxisAlignedBox();

        #endregion Member Variables

        #region Properties

        /// <summary>
        ///
        /// </summary>
        public AxisAlignedBox LocalAABB
        {
            get
            {
                return this.localAABB;
            }
        }

        /// <summary>
        ///
        /// </summary>
        public Octree Octant
        {
            get
            {
                return this.octant;
            }
            set
            {
                this.octant = value;
            }
        }

        #endregion Properties

        #region Constructors

        public OctreeNode(SceneManager scene)
            : base(scene)
        {
        }

        public OctreeNode(SceneManager scene, string name)
            : base(scene, name)
        {
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        ///     Determines if the center of this node is within the given box.
        /// </summary>
        /// <param name="box"></param>
        /// <returns></returns>
        public bool IsInBox(AxisAlignedBox box)
        {
            Vector3 center = worldAABB.Maximum.MidPoint(worldAABB.Minimum);
            Vector3 min = box.Minimum;
            Vector3 max = box.Maximum;

            return (max > center && min < center);
        }

        /// <summary>
        ///     Adds all the attached scenenodes to the render queue.
        /// </summary>
        /// <param name="cam"></param>
        /// <param name="queue"></param>
        public void AddToRenderQueue(Camera cam, RenderQueue queue)
        {
            int i;
            foreach (MovableObject obj in objectList.Values)
            {
                obj.NotifyCurrentCamera(cam);

                if (obj.IsVisible)
                {
                    obj.UpdateRenderQueue(queue);
                }
            }
        }

        /// <summary>
        ///     Same as SceneNode, only it doesn't care about children...
        /// </summary>
        protected override void UpdateBounds()
        {
            //update bounds from attached objects
            foreach (MovableObject obj in objectList.Values)
            {
                this.localAABB.Merge(obj.BoundingBox);

                worldAABB = obj.GetWorldBoundingBox(true);
            }

            if (!worldAABB.IsNull)
            {
                var oManager = (OctreeSceneManager)creator;
                oManager.UpdateOctreeNode(this);
            }
        }

        /// <summary>
        /// Removes the specified node from the scene graph and the octree, optionally keeping it in the internal node list yet. For internal use.
        /// </summary>
        /// <remarks>
        /// Removes all of the node's child subtree from the octree, but children remain linked to parents.
        /// </remarks>
        /// <param name="child"></param>
        /// <param name="removeFromInternalList"></param>
        protected override void RemoveChild(Node child, bool removeFromInternalList)
        {
            Debug.Assert(child is OctreeNode, "node is not an octree node");

            // remove all linked nodes from octree
            RemoveNodesFromOctree((OctreeNode)child);

            // remove child from scene graph
            base.RemoveChild(child, removeFromInternalList);
        }

        /// <summary>
        /// Removes the specified node and all of it's child subtree from the octree, but not from the scene graph.
        /// </summary>
        /// <remarks>
        /// This iterates the whole node tree starting from the specified node and removes them from octree partitions,
        /// but doesn't remove them from the scene graph.
        /// </remarks>
        /// <param name="child"></param>
        protected void RemoveNodesFromOctree(OctreeNode baseNode)
        {
            foreach (OctreeNode child in baseNode.Children)
            {
                baseNode.RemoveNodesFromOctree(child);
            }

            ((OctreeSceneManager)baseNode.Creator).RemoveOctreeNode(baseNode);
        }

        #endregion Methods
    }
}