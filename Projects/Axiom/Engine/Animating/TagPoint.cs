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

using Axiom.Math;
using Axiom.Core;
using Axiom.Collections;
using Axiom.Core.Collections;

#endregion Namespace Declarations

#region Ogre Synchronization Information

/// <ogresynchronization>
///     <file name="TagPoint.h"   revision="1.10.2.2" lastUpdated="10/15/2005" lastUpdatedBy="DanielH" />
///     <file name="TagPoint.cpp" revision="1.12" lastUpdated="10/15/2005" lastUpdatedBy="DanielH" />
/// </ogresynchronization>

#endregion

namespace Axiom.Animating
{
	/// <summary>
	///		A tagged point on a skeleton, which can be used to attach entities to on specific
	///		other entities.
	/// </summary>
	/// <remarks>
	///		A Skeleton, like a Mesh, is shared between Entity objects and simply updated as required
	///		when it comes to rendering. However there are times when you want to attach another object
	///		to an animated entity, and make sure that attachment follows the parent entity's animation
	///		(for example, a character holding a gun in his / her hand). This class simply identifies
	///		attachment points on a skeleton which can be used to attach child objects. 
	///		<p/>
	///		The child objects themselves are not physically attached to this class; as it's name suggests
	///		this class just 'tags' the area. The actual child objects are attached to the Entity using the
	///		skeleton which has this tag point. Use <see cref="Entity.AttachObjectToBone"/> to attach
	///		the objects, which creates a new TagPoint on demand.
	/// </remarks>
	public class TagPoint : Bone
	{
		#region Fields and Properties

		/// <summary>
		///		Reference to the entity that owns this tagpoint.
		/// </summary>
		protected Entity parentEntity;

		/// <summary>
		///		Object attached to this tagpoint.
		/// </summary>
		protected MovableObject childObject;

		/// <summary>
		///		Combined full local transform of this tagpoint.
		/// </summary>
		protected Matrix4 fullLocalTransform;

		/// <summary>
		///		Gets/Sets the object attached to this tagpoint.
		/// </summary>
		public MovableObject ChildObject { get { return childObject; } set { childObject = value; } }

		/// <summary>
		///		Gets/Sets the parent Entity that is using this tagpoint.	
		/// </summary>
		public Entity ParentEntity { get { return parentEntity; } set { parentEntity = value; } }

		/// <summary>
		///		Gets the transform of this node just for the skeleton (not entity).
		/// </summary>
		public Matrix4 FullLocalTransform { get { return fullLocalTransform; } }

		/// <summary>
		///		Transformation matrix of the parent entity.
		/// </summary>
		public Matrix4 ParentEntityTransform { get { return parentEntity.ParentNodeFullTransform; } }

		/// <summary>
		///  Tells the TagPoint whether it should inherit orientation from it's parent entity.
		/// </summary>
		/// <remarks>
		/// If true, this TagPoint's orientation will be affected by
		/// its parent entity's orientation. If false, it will not be affected.
		/// </remarks>
		public bool InheritParentEntityOrientation { get; set; }

		public bool InheritParentEntityScale { get; set; }

		#endregion Fields and Properties

		#region Construction and Destruction

		/// <summary>
		///		Constructor.
		/// </summary>
		/// <param name="handle">Handle to use.</param>
		/// <param name="creator">Skeleton who created this tagpoint.</param>
		public TagPoint( ushort handle, Skeleton creator )
			: base( handle, creator )
		{
			suppressUpdateEvent = true;
			InheritParentEntityOrientation = true;
			InheritParentEntityScale = true;
		}

		#endregion Construction and Destruction

		#region Bone Members

		/// <summary>
		///		Gets the transform of this node including the parent entity and skeleton.
		/// </summary>
		public override Matrix4 FullTransform { get { return base.FullTransform; } }

		/// <summary>
		///		Overridden to update parent entity.
		/// </summary>
		public override void NeedUpdate()
		{
			needParentUpdate = true;
			// // We need to tell parent entities node
			if( parentEntity != null )
			{
				Node n = parentEntity.ParentNode;

				if( n != null )
				{
					n.NeedUpdate();
				}
			}
		}

		protected override void UpdateFromParent()
		{
			base.UpdateFromParent();

			// Save transform for local skeleton
			MakeTransform( derivedPosition, derivedScale, derivedOrientation, ref fullLocalTransform );

			// Include Entity transform
			if( parentEntity != null )
			{
				Node entityParentNode = parentEntity.ParentNode;
				if( entityParentNode != null )
				{
					Quaternion parentQ = entityParentNode.DerivedOrientation;
					if( InheritParentEntityOrientation )
					{
						derivedOrientation = parentQ * derivedOrientation;
					}

					// Incorporate parent entity scale
					Vector3 parentScale = entityParentNode.Scale;
					if( InheritParentEntityScale )
					{
						derivedScale *= parentScale;
					}

					// Change position vector based on parent's orientation
					derivedPosition = parentQ * ( parentScale * derivedPosition );

					// Add altered position vector to parents
					derivedPosition += entityParentNode.DerivedPosition;

					OnUpdatedFromParent();
				}
			}

			if( childObject != null )
			{
				childObject.NotifyMoved();
			}
		}

		#endregion Bone Members
	}
}
