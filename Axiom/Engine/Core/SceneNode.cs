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
using Axiom.Collections;
using Axiom.SubSystems.Rendering;

namespace Axiom.Core
{
	/// <summary>
	///		Represents a node in a scene graph.
	/// </summary>
	/// <remarks>
	///		A SceneNode is a type of Node which is used to organize objects in a scene.
	///		It has the same hierarchical transformation properties of the generic Node class,
	///		but also adds the ability to attach world objects to the node, and stores hierarchical
	///		bounding volumes of the nodes in the tree.
	///		Child nodes are contained within the bounds of the parent, and so on down the
	///		tree, allowing for fast culling.
	/// </remarks>
	// INC: In progress.
	public class SceneNode : Node
	{
		#region Member variables

		/// <summary>A collection of all objects attached to this scene node.</summary>
		protected MovableObjectCollection objectList;
		/// <summary>Reference to the scene manager who created me.</summary>
		protected SceneManager creator;
		/// <summary>Renderable bounding box for this node.</summary>
		protected WireBoundingBox wireBox;
		/// <summary>Whether or not to display this node's bounding box.</summary>
		protected bool showBoundingBox;
		/// <summary>Bounding box.  Updated through Update.</summary>
		protected AxisAlignedBox worldAABB;

		#endregion

		#region Constructors

		/// <summary>
		///		Basic constructor.  Takes a scene manager reference to record the creator.
		/// </summary>
		/// <remarks>
		///		Can be created manually, but should be left the Create* Methods.
		/// </remarks>
		/// <param name="creator"></param>
		public SceneNode(SceneManager creator) : base()
		{
			this.creator = creator;

			objectList = new MovableObjectCollection();

			// set up event handlers so we know when items are added, removed, and cleared.
			objectList.Cleared += new CollectionHandler(this.ObjectsCleared);
			objectList.ItemAdded += new CollectionHandler(this.ObjectAttached);
			objectList.ItemRemoved += new CollectionHandler(this.ObjectRemoved);

			worldAABB = AxisAlignedBox.Null;

			NeedUpdate();

		}

		/// <summary>
		///		Overloaded constructor.  Takes a scene manager reference to record the creator, and a name for the node.
		/// </summary>
		/// <param name="creator"></param>
		/// <param name="name"></param>
		public SceneNode(SceneManager creator, String name) : base(name)
		{
			this.creator = creator;

			objectList = new MovableObjectCollection();

			// set up event handlers so we know when items are added, removed, and cleared.
			objectList.Cleared += new CollectionHandler(this.ObjectsCleared);
			objectList.ItemAdded += new CollectionHandler(this.ObjectAttached);
			objectList.ItemRemoved += new CollectionHandler(this.ObjectRemoved);

			worldAABB = AxisAlignedBox.Null;

			NeedUpdate();
		}

		#endregion

		#region Properties

		/// <summary>
		///		Gets/Sets whether or not to display the bounding box for this node.
		/// </summary>
		public bool ShowBoundingBox
		{
			get { return showBoundingBox; }
			set { showBoundingBox = value; }
		}

		/// <summary>
		///		Gets a MovableObjectColletion containing this SceneNode's attached objects.
		/// </summary>
		/// <remarks>
		///		Use this property for adding and removing objects.
		/// </remarks>
		public MovableObjectCollection Objects
		{
			get { return objectList; }
		}

		/// <summary>
		///		Gets a reference to the SceneManager that created this node.
		/// </summary>
		public SceneManager Creator
		{
			get { return creator; }
		}

		/// <summary>
		///		Gets the axis-aligned bounding box of this node (and hence all child nodes).
		/// </summary>
		/// <remarks>
		///		Usage not recommended unless you are extending a SceneManager, because the bounding box returned
        ///		from this method is only up to date after the SceneManager has called Update.
		/// </remarks>
		public AxisAlignedBox WorldAABB
		{
			get { return worldAABB; }
		}

		#endregion

		#region Methods

		/// <summary>
		///		Internal method to update the Node.
		/// </summary>
		/// <remarks>
		///		Updates this scene node and any relevant children to incorporate transforms etc.
        ///		Don't call this yourself unless you are writing a SceneManager implementation.
		/// </remarks>
		/// <param name="?"></param>
		/// <param name="hasParentChanged"></param>
		internal override void Update(bool updateChildren, bool hasParentChanged)
		{
			// call base class method
			base.Update(updateChildren, hasParentChanged);

			UpdateBounds();
		}

		/// <summary>
		///		Overloaded method.
		/// </summary>
		/// <param name="camera"></param>
		/// <param name="queue"></param>
		virtual public void FindVisibleObjects(Camera camera, RenderQueue queue)
		{
			// call overloaded method
			FindVisibleObjects(camera, queue, true, false);
		}

		/// <summary>
		///		Internal method which locates any visible objects attached to this node and adds them to the passed in queue.
		/// </summary>
		/// <param name="camera">Active camera.</param>
		/// <param name="queue">Queue to which these objects should be added.</param>
		/// <param name="includeChildren">If true, cascades down to all children.</param>
		/// <param name="displayNodes">Renders the local axes for the node.</param>
		virtual public void FindVisibleObjects(Camera camera, RenderQueue queue, bool includeChildren, bool displayNodes)
		{
			// if we aren't visible, then quit now
			if(!camera.IsObjectVisible(worldAABB))
				return;

			// add visible objects to the render queue
			for(int i = 0; i < objectList.Count; i++)
			{
				SceneObject obj = objectList[i];

				// tell attached object about current camera in case it wants to know
				obj.NotifyCurrentCamera(camera);

				// if this object is visible, add it to the render queue
				if(obj.IsVisible)
					obj.UpdateRenderQueue(queue);
			}

			if(includeChildren)
			{
				// ask all child nodes to update the render queue with visible objects
				for(int i = 0; i < childNodes.Count; i++)
				{
					SceneNode childNode = (SceneNode)childNodes[i];
					childNode.FindVisibleObjects(camera, queue, includeChildren, displayNodes);
				}
			}

			// if we wanna display nodes themself..
			if(displayNodes)
			{
				// hey, lets just add ourself right to the render queue
				queue.AddRenderable(this);
			}

			// do we wanna show our beautiful bounding box?
			// do it id either we want it, or the SceneManager dictates it
			if(showBoundingBox || creator.ShowBoundingBoxes)
			{
				AddBoundingBoxToQueue(queue);
			}
		}

		/// <summary>
		///		Adds this nodes bounding box (wireframe) to the RenderQueue.
		/// </summary>
		/// <param name="queue"></param>
		public void AddBoundingBoxToQueue(RenderQueue queue)
		{
			if(wireBox == null)
				wireBox = new WireBoundingBox();

			// add the wire bounding box to the render queue
			wireBox.InitAABB(worldAABB);
			queue.AddRenderable(wireBox);
		}

		/// <summary>
		///		Tell the SceneNode to update the world bound info it stores.
		/// </summary>
		virtual protected void UpdateBounds()
		{
			// reset bounds
			worldAABB.IsNull = true;

			// update bounds from attached objects
			for(int i = 0; i < objectList.Count; i++)
			{
				SceneObject  obj = objectList[i];

				// get the bounding box of the movable object
				// note - the movable object BETTER be cloning the box and returning it, to prevent
				// direct modification to the bounding box
				AxisAlignedBox box = obj.BoundingBox;

				// transform by aggregated transform
				box.Transform(this.FullTransform);

				// merge ours with the current one
				worldAABB.Merge(box);
			}

			// merge with Children
			for(int i = 0; i < childNodes.Count; i++)
			{
				SceneNode child = (SceneNode)childNodes[i];

				// merge our bounding box with that of the child node
				worldAABB.Merge(child.worldAABB);
			}
		}

		#endregion

		#region Event handlers

		/// <summary>
		///		New object attached.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public bool ObjectAttached(object sender, EventArgs e)
		{
			// get the object that was attached
			SceneObject obj = (SceneObject)sender;

			// notify the object that it was attached to us
			obj.NotifyAttached(this);

			// make sure bounds get updated
			NeedUpdate();

			return false;
		}

		/// <summary>
		///		Object removed.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public bool ObjectRemoved(object sender, EventArgs e)
		{
			// get the object that was removed
			SceneObject obj = (SceneObject)sender;

			// notify the object that it was removed (sending in null sets its parent scene node to null)
			obj.NotifyAttached(null);

			// make sure bounds get updated
			NeedUpdate();

			return false;
		}

		/// <summary>
		///		Object list cleared.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public bool ObjectsCleared(object sender, EventArgs e)
		{
			// update everything
			NeedUpdate();

			return false;
		}

		#endregion

		#region Implementation of Node

		/// <summary>
		///		
		/// </summary>
		/// <returns></returns>
		protected override Node CreateChildImpl()
		{
			return creator.CreateSceneNode();
		}
	
		/// <summary>
		///		
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		protected override Node CreateChildImpl(String name)
		{
			SceneNode newNode = creator.CreateSceneNode(name);
			return newNode;
		}

		#endregion
	}
}
