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
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;

using Axiom.Core;
using Axiom.Math;

#endregion Namespace Declarations

#region Ogre Synchronization Information

// <ogresynchronization>
//     <file name="Bone.h"   revision="1.17" lastUpdated="10/15/2005" lastUpdatedBy="DanielH" />
//     <file name="Bone.cpp" revision="1.22" lastUpdated="10/15/2005" lastUpdatedBy="DanielH" />
// </ogresynchronization>

#endregion

namespace Axiom.Animating
{
	/// <summary>
	///    A bone in a skeleton.
	/// </summary>
	/// <remarks>
	///    See Skeleton for more information about the principles behind skeletal animation.
	///    This class is a node in the joint hierarchy. Mesh vertices also have assignments
	///    to bones to define how they move in relation to the skeleton.
	/// </remarks>
	public class Bone : Node
	{
		#region Fields

		/// <summary>The inverse derived transform of the bone in the binding pose.</summary>
		protected Matrix4 bindDerivedInverseTransform;

		/// <summary>The skeleton that created this bone.</summary>
		protected Skeleton creator;

		/// <summary>Numeric handle of this bone.</summary>
		protected ushort handle;

		/// <summary>Bones set as manuallyControlled are not reseted in Skeleton.Reset().</summary>
		protected bool isManuallyControlled;

		#endregion Fields

		#region Constructors

		/// <summary>
		///    Constructor, not to be used directly (use Bone.CreateChild or Skeleton.CreateBone)
		/// </summary>
		public Bone( ushort handle, Skeleton creator )
		{
			this.handle = handle;
			this.isManuallyControlled = false;
			this.creator = creator;
		}

		/// <summary>
		///    Constructor, not to be used directly (use Bone.CreateChild or Skeleton.CreateBone)
		/// </summary>
		public Bone( string name, ushort handle, Skeleton creator )
			: base( name )
		{
			this.handle = handle;
			this.isManuallyControlled = false;
			this.creator = creator;
		}

		#endregion

		#region Methods

		/// <summary>
		///    Creates a new Bone as a child of this bone.
		/// </summary>
		/// <returns></returns>
		protected override Node CreateChildImpl()
		{
			return this.creator.CreateBone();
		}

		/// <summary>
		///    Creates a new Bone as a child of this bone.
		/// </summary>
		/// <param name="name">Name of the bone to create.</param>
		/// <returns></returns>
		protected override Node CreateChildImpl( string name )
		{
			return this.creator.CreateBone( name );
		}

		/// <summary>
		///    Overloaded method.  Passes in Zero and Identity for the last 2 params.
		/// </summary>
		/// <param name="handle">The numeric handle to give the new bone; must be unique within the Skeleton.</param>
		/// <returns></returns>
		public Bone CreateChild( ushort handle )
		{
			return CreateChild( handle, Vector3.Zero, Quaternion.Identity );
		}

		/// <summary>
		///    Creates a new Bone as a child of this bone.
		/// </summary>
		/// <param name="handle">The numeric handle to give the new bone; must be unique within the Skeleton.</param>
		/// <param name="translate">Initial translation offset of child relative to parent.</param>
		/// <param name="rotate">Initial rotation relative to parent.</param>
		/// <returns></returns>
		public Bone CreateChild( ushort handle, Vector3 translate, Quaternion rotate )
		{
			Bone bone = this.creator.CreateBone( handle );
			bone.Translate( translate );
			bone.Rotate( rotate );
			AddChild( bone );

			return bone;
		}

		/// <summary>
		///    Resets the position and orientation of this Bone to the original binding position.
		/// </summary>
		/// <remarks>
		///    Bones are bound to the mesh in a binding pose. They are then modified from this
		///    position during animation. This method returns the bone to it's original position and
		///    orientation.
		/// </remarks>
		public void Reset()
		{
			ResetToInitialState();
		}

		/// <summary>
		///    Sets the current position / orientation to be the 'binding pose' ie the layout in which 
		///    bones were originally bound to a mesh.
		/// </summary>
		public void SetBindingPose()
		{
			SetInitialState();

			// save inverse derived, used for mesh transform later (assumes Update has been called by Skeleton
			MakeInverseTransform( DerivedPosition, Vector3.UnitScale, DerivedOrientation, ref this.bindDerivedInverseTransform );
		}

		#endregion

		#region Properties

		/// <summary>
		///		Determines whether this bone is controlled at runtime.
		/// </summary>
		public bool IsManuallyControlled
		{
			get
			{
				return this.isManuallyControlled;
			}
			set
			{
				this.isManuallyControlled = value;
			}
		}

		/// <summary>
		///    Gets the inverse transform which takes bone space to origin from the binding pose. 
		/// </summary>
		public Matrix4 BindDerivedInverseTransform
		{
			get
			{
				return this.bindDerivedInverseTransform;
			}
		}

		/// <summary>
		///    Gets the numeric handle of this bone.
		/// </summary>
		public ushort Handle
		{
			get
			{
				return this.handle;
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
	///		This is a class because we need it as a reference type to allow for modification
	///		in places where we would only have a copy of the data if it were a struct. 
	/// </remarks>
	public class VertexBoneAssignment : IComparable
	{
		public ushort boneIndex;
		public int vertexIndex;
		public float weight;

		public VertexBoneAssignment() { }

		public VertexBoneAssignment( VertexBoneAssignment other )
		{
			this.vertexIndex = other.vertexIndex;
			this.boneIndex = other.boneIndex;
			this.weight = other.weight;
		}

		#region IComparable Members

		public int CompareTo( object obj )
		{
			if ( obj is VertexBoneAssignment )
			{
				var v = (VertexBoneAssignment)obj;

				if ( this.weight > v.weight )
				{
					return 1;
				}
				if ( this.weight < v.weight )
				{
					return -1;
				}

				if ( this.vertexIndex != v.vertexIndex )
				{
					return this.vertexIndex - v.vertexIndex;
				}

				if ( this.boneIndex != v.boneIndex )
				{
					return this.boneIndex - v.boneIndex;
				}

				return 0;
			}
			return 0;
		}

		#endregion
	}

	public class VertexBoneAssignmentWeightComparer : IComparer<VertexBoneAssignment>
	{
		#region IComparer<VertexBoneAssignment> Members

		/// <summary>Compares two objects and returns a value indicating whether one is less than, equal to or greater than the other.</summary>
		/// <returns>Value Condition Less than zero x is less than y. Zero x equals y. Greater than zero x is greater than y. </returns>
		/// <param name="yVba">Second object to compare. </param>
		/// <param name="xVba">First object to compare. </param>
		/// <filterpriority>2</filterpriority>
		public int Compare( VertexBoneAssignment xVba, VertexBoneAssignment yVba )
		{
			if ( xVba == null && yVba == null )
			{
				return 0;
			}
			else if ( xVba == null )
			{
				return -1;
			}
			else if ( yVba == null )
			{
				return 1;
			}
			else if ( xVba.weight == yVba.weight )
			{
				return 0;
			}
			else if ( xVba.weight < yVba.weight )
			{
				return -1;
			}
			else // if (xVba.weight > yVba.weight)
			{
				return 1;
			}
		}

		#endregion
	}
}
