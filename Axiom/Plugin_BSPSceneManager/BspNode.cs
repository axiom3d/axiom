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
using Axoim.Core;

/// <summary>
///    Encapsulates a node in a BSP tree.
/// </summary>
/// <remarks>
///    A BSP tree represents space partitioned by planes . The space which is
///    partitioned is either the world (in the case of the root node) or the space derived
///    from their parent node. Each node can have elements which are in front or behind it, which are
///    it's children and these elements can either be further subdivided by planes,
///    or they can be undivided spaces or 'leaf nodes' - these are the nodes which actually contain
///    objects and world geometry.The leaves of the tree are the stopping point of any tree walking algorithm,
///    both for rendering and collision detection etc.</p>
///    We choose not to represent splitting nodes and leaves as separate structures, but to merge the two for simplicity
///    of the walking algorithm. If a node is a leaf, the IsLeaf property returns true and both GetFront() and
///    GetBack() return null references. If the node is a partitioning plane IsLeaf returns false and GetFront()
///    and GetBack() will return the corresponding BspNode objects.
/// </remarks>
public class BspSceneNode : SceneNode {
}