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
using Axiom.Enumerations;
using Axiom.SubSystems.Rendering;

namespace Axiom.Core
{
	/// <summary>
	/// Abstract class definining a movable object in a scene.
	/// </summary>
	/// <remarks>
	/// Instances of this class are discrete, relatively small, movable objects
	/// which are attached to SceneNode objects to define their position.						  
	/// </remarks>
	// TODO: Add local OBB / convex hull
	public abstract class SceneObject
	{
		protected SceneNode parentNode;
		protected bool isVisible;
		protected String name;
		protected RenderQueueGroupID renderQueueID;

		public SceneObject()
		{
			this.isVisible = true;

			// set default RenderQueueGroupID for this movable object
			renderQueueID = RenderQueueGroupID.Main;
		}

		#region Properties

		/// <summary>
		///		An abstract method required by subclasses to return the bounding box of this object.
		/// </summary>
		public abstract AxisAlignedBox BoundingBox
		{
			get;
		}

		/// <summary>
		///		Gets the parent node that this object is attached to.
		/// </summary>
		public SceneNode ParentNode
		{
			get
			{
				return parentNode;
			}
		}

		/// <summary>
		///		See if this object is attached to another node.
		/// </summary>
		public bool IsAttached
		{
			get
			{
				return (parentNode == null);
			}
		}

		/// <summary>
		///		States whether or not this object should be visible.
		/// </summary>
		virtual public bool IsVisible
		{
			get
			{
				return isVisible;
			}
			set
			{
				isVisible = value;
			}
		}

		/// <summary>
		///		Name of this SceneObject.
		/// </summary>
		public String Name
		{
			get { return name;}
			set { name = value; }
		}

		#endregion

		#region Internal engine methods
		
		/// <summary>
		///		An abstract method that causes the specified RenderQueue to update itself.  
		/// </summary>
		/// <remarks>This is an internal method used by the engine assembly only.</remarks>
		/// <param name="queue">The render queue that this object should be updated in.</param>
		internal abstract void UpdateRenderQueue(RenderQueue queue);

		/// <summary>
		///		Internal method called to notify the object that it has been attached to a node.
		/// </summary>
		/// <param name="node">Scene node to notify.</param>
		internal virtual void NotifyAttached(SceneNode node)
		{
			parentNode = node;
		}

		/// <summary>
		///		Internal method to notify the object of the camera to be used for the next rendering operation.
		/// </summary>
		/// <remarks>
		///		Certain objects may want to do specific processing based on the camera position. This method notifies
		///		them incase they wish to do this.
		/// </remarks>
		/// <param name="camera"></param>
		internal abstract void NotifyCurrentCamera(Camera camera);

		#endregion
	}
}
