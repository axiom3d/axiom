#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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

namespace Axiom.Animating
{
	/// <summary>
	///		A collection of Bone objects used to animate a skinned mesh.
	///	 </summary>
	///	 <remarks>
	///		Skeletal animation works by having a collection of 'bones' which are 
	///		actually just joints with a position and orientation, arranged in a tree structure.
	///		For example, the wrist joint is a child of the elbow joint, which in turn is a
	///		child of the shoulder joint. Rotating the shoulder automatically moves the elbow
	///		and wrist as well due to this hierarchy.
	///		<p/>
	///		So how does this animate a mesh? Well every vertex in a mesh is assigned to one or more
	///		bones which affects it's position when the bone is moved. If a vertex is assigned to 
	///		more than one bone, then weights must be assigned to determine how much each bone affects
	///		the vertex (actually a weight of 1.0 is used for single bone assignments). 
	///		Weighted vertex assignments are especially useful around the joints themselves
	///		to avoid 'pinching' of the mesh in this region. 
	///		<p/>
	///		Therefore by moving the skeleton using preset animations, we can animate the mesh. The
	///		advantage of using skeletal animation is that you store less animation data, especially
	///		as vertex counts increase. In addition, you are able to blend multiple animations together
	///		(e.g. walking and looking around, running and shooting) and provide smooth transitions
	///		between animations without incurring as much of an overhead as would be involved if you
	///		did this on the core vertex data.
	///		<p/>
	///		Skeleton definitions are loaded from datafiles, namely the .xsf file format. They
	///		are loaded on demand, especially when referenced by a Mesh.
	/// </remarks>
	public class Skeleton
	{
		/// <summary>Maximum total available bone matrices that are available during blending.</summary>
		public const int MAX_BONE_COUNT = 256;

		public Skeleton()
		{
			//
			// TODO: Add constructor logic here
			//
		}

		/// <summary>
		/// Creates a new bone.
		/// </summary>
		public Bone CreateBone(string name)
		{
			return null;
		}
	}
}
