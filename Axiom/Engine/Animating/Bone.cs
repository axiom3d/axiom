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
using Axiom.Core;

namespace Axiom.Animating
{
	/// <summary>
	/// Summary description for Bone.
	/// </summary>
	public class Bone : Node
	{
		#region Member variables

		/// <summary>Determines whether this bone is controlled at runtime.</summary>
		private bool manuallyControlled;

		#endregion

		#region Constructors

		public Bone()
		{
		}

		#endregion

		#region Methods

		protected override Node CreateChildImpl()
		{
			return null;
		}

		protected override Node CreateChildImpl(String name)
		{
			return null;
		}

		#endregion

		#region Properties

		/// <summary>
		///		Determines whether this bone is controlled at runtime.
		/// </summary>
		public bool ManuallyControlled
		{
			get { return manuallyControlled; }
			set
			{
				manuallyControlled = value;
			}
		}

		#endregion

	}

	/// <summary>
	///		Records the assignment of a single vertex to a single bone with the corresponding weight.
	///	 </summary>
	///	 <remarks>
	///		This simple struct simply holds a vertex index, bone index and weight representing the
	///		assignment of a vertex to a bone for skeletal animation. There may be many of these
	///		per vertex if blended vertex assignments are allowed.
	/// </remarks>
	public struct VertexBoneAssignment
	{
		ushort vertexIndex;
		ushort boneIndex;
		float weight;
	}
}
