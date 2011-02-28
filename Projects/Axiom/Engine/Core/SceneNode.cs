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
using System.Diagnostics;
using System.Collections;

using Axiom.Collections;
using Axiom.Math;
using Axiom.Graphics;
using System.Collections.Generic;
using Axiom.Core.Collections;
#endregion Namespace Declarations

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
	public class SceneNode : Node
	{
		#region Fields

		/// <summary>
		///		A collection of all objects attached to this scene node.
		///	</summary>
		protected MovableObjectCollection objectList = new MovableObjectCollection();
		/// <summary>
		///    Gets the number of SceneObjects currently attached to this node.
		/// </summary>
		public int ObjectCount
		{
			get
			{
				return objectList.Count;
			}
		}
		/// <summary>
		/// Gets the list of scene objects attached to this scene node
		/// </summary>
		public ICollection<MovableObject> Objects
		{
			get
			{
				return objectList.Values;
			}
		}

		/// <summary>
		///		Reference to the scene manager who created me.
		///	</summary>
		protected SceneManager creator;
		/// <summary>
		///		Renderable bounding box for this node.
		///	</summary>
		protected WireBoundingBox wireBox;
		/// <summary>
		/// Wireframe bounding box for this scenenode
		/// </summary>
		public WireBoundingBox WireBoundingBox
		{
			get
			{
				return wireBox;
			}
			set
			{
				wireBox = value;
			}
		}
		/// <summary>
		///		Whether or not to display this node's bounding box.
		///	</summary>
		protected bool showBoundingBox;
		/// <summary>
		///		Bounding box. Updated through Update.
		///	</summary>
		protected AxisAlignedBox worldAABB = AxisAlignedBox.Null;
		/// <summary>
		///		Word bounding sphere surrounding this node.
		/// </summary>
		protected Sphere worldBoundingSphere = new Sphere();
		public Sphere WorldBoundingSphere
		{
			get
			{
				return worldBoundingSphere;
			}
		}
		/// <summary>
		///    List of lights within range of this node.
		/// </summary>
		protected LightList lightList = new LightList();
		/// <summary>
		///    Keeps track of whether the list of lights located near this node needs updating.
		/// </summary>
		protected bool lightListDirty;
		/// <summary>
		///		Where to yaw around a fixed axis.
		/// </summary>
		protected bool isYawFixed;
		/// <summary>
		///		Fixed axis to yaw around.
		/// </summary>
		protected Vector3 yawFixedAxis;
		/// <summary>
		///		Auto tracking target.
		/// </summary>
		protected SceneNode autoTrackTarget;
		/// <summary>
		///		Tracking offset for fine tuning.
		/// </summary>
		protected Vector3 autoTrackOffset = Vector3.Zero;
		/// <summary>
		///		Local 'normal' direction vector.
		/// </summary>
		protected Vector3 autoTrackLocalDirection = Vector3.NegativeUnitZ;
		/// <summary>
		///		Determines whether node and children are visible or not.
		/// </summary>
		protected bool visible = true;

		/// <summary>
		/// Is this node a current part of the scene graph?
		/// </summary>
		protected bool isInSceneGraph;

		#endregion Fields

		#region Construction and Destruction

		/// <summary>
		///		Basic constructor.  Takes a scene manager reference to record the creator.
		/// </summary>
		/// <remarks>
		///		Can be created manually, but should be left the Create* Methods.
		/// </remarks>
		/// <param name="creator"></param>
		public SceneNode( SceneManager creator )
			: base()
		{
			this.creator = creator;

			NeedUpdate();

			lightListDirty = true;
		}

		/// <summary>
		///		Overloaded constructor.  Takes a scene manager reference to record the creator, and a name for the node.
		/// </summary>
		/// <param name="creator"></param>
		/// <param name="name"></param>
		public SceneNode( SceneManager creator, string name )
			: base( name )
		{
			this.creator = creator;

			NeedUpdate();

			lightListDirty = true;
		}

		protected override void dispose( bool disposeManagedResources )
		{
			if ( disposeManagedResources )
			{
				foreach ( MovableObject item in objectList )
				{
					item.NotifyAttached( null );
				}
				objectList.Clear();

				if ( wireBox != null )
				{
                    if (!wireBox.IsDisposed)
					    wireBox.Dispose();

					wireBox = null;
				}
			}
			base.dispose( disposeManagedResources );
		}

		#endregion Construction and Destruction

		#region Properties


		/// <summary>
		///		Gets/Sets whether or not to display the bounding box for this node.
		/// </summary>
		public bool ShowBoundingBox
		{
			get
			{
				return showBoundingBox;
			}
			set
			{
				showBoundingBox = value;
			}
		}

		/// <summary>
		///		Gets a reference to the SceneManager that created this node.
		/// </summary>
		public SceneManager Creator
		{
			get
			{
				return creator;
			}
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
			get
			{
				return worldAABB;
			}
		}

		/// <summary>
		///		Gets the offset at which this node is tracking another node, if the node is auto tracking..
		/// </summary>
		public Vector3 AutoTrackOffset
		{
			get
			{
				return autoTrackOffset;
			}
			set
			{
				autoTrackOffset = value;
			}
		}

		/// <summary>
		///		Get the auto tracking local direction for this node, if it is auto tracking.
		/// </summary>
		public Vector3 AutoTrackLocalDirection
		{
			get
			{
				return autoTrackLocalDirection;
			}
			set
			{
				autoTrackLocalDirection = value;
			}
		}

		/// <summary>
		///		Gets the SceneNode that this node is currently tracking, if any.
		/// </summary>
		public SceneNode AutoTrackTarget
		{
			get
			{
				return autoTrackTarget;
			}
			set
			{
				autoTrackTarget = value;
				if ( creator != null )
					creator.NotifyAutoTrackingSceneNode( this, value != null );
			}
		}

		/// <summary>
		///		Sets visibility for this node. If invisible, child nodes will be invisible, too.
		/// </summary>
		public bool IsVisible
		{
			get
			{
				return this.visible;
			}
			set
			{
				this.visible = value;
			}
		}

		/// <summary>
		/// Determines whether this node is in the scene graph, i.e.
		/// whether it's ultimate ancestor is the root scene node.
		/// </summary>
		public virtual bool IsInSceneGraph
		{
			get
			{
				return this.isInSceneGraph;
			}
			protected set
			{
				if ( value != this.isInSceneGraph )
				{
					this.isInSceneGraph = value;
					// notify children
					foreach ( Node child in childNodes.Values )
					{
						( (SceneNode)child ).IsInSceneGraph = this.isInSceneGraph;
					}
				}
			}
		}

		/// <summary>
		/// Only called by SceneManagers
		/// </summary>
		public void SetAsRootNode()
		{
			isInSceneGraph = true;
		}

		public new SceneNode Parent
		{
			get
			{
				return (SceneNode)base.Parent;
			}
			set
			{
				base.Parent = value;
				if ( base.Parent != null )
				{
					IsInSceneGraph = ( (SceneNode)base.Parent ).IsInSceneGraph;
				}
				else
				{
					IsInSceneGraph = false;
				}
			}
		}
		#endregion Properties

		#region Methods

		protected override void OnRename( string oldName )
		{
			//ensure that it is keyed to the name name in the Scene Manager that manages it
			this.creator.RekeySceneNode( oldName, this );
		}

		/// <summary>
		///    Attaches a MovableObject to this scene node.
		/// </summary>
		/// <remarks>
		///    A MovableObject will not show up in the scene until it is attached to a SceneNode.
		/// </remarks>
		/// <param name="obj"></param>
		public virtual void AttachObject( MovableObject obj )
		{
			Debug.Assert( obj != null, "obj != null" );

			objectList.Add( obj );

			// notify the object that it was attached to us
			obj.NotifyAttached( this );

			// make sure bounds get updated
			NeedUpdate();
		}

		/// <summary>
		///		Need to clear list of child objects in addition to base class functionality.
		/// </summary>
		internal override void Clear()
		{
			base.Clear();

			objectList.Clear();
		}


		/// <summary>
		///    Creates a new name child node.
		/// </summary>
		/// <param name="name"></param>
		public virtual SceneNode CreateChildSceneNode( string name )
		{
			return CreateChildSceneNode( name, Vector3.Zero, Quaternion.Identity );
		}

		/// <summary>
		///    Creates a new named child scene node.
		/// </summary>
		/// <param name="name">Name of the node.</param>
		/// <param name="translate">A vector to specify the position relative to the parent.</param>
		/// <returns></returns>
		public virtual SceneNode CreateChildSceneNode( string name, Vector3 translate )
		{
			return CreateChildSceneNode( name, translate, Quaternion.Identity );
		}

		/// <summary>
		///    Creates a new named child scene node.
		/// </summary>
		/// <param name="name">Name of the node.</param>
		/// <param name="translate">A vector to specify the position relative to the parent.</param>
		/// <param name="rotate">A quaternion to specify the orientation relative to the parent.</param>
		/// <returns></returns>
		public virtual SceneNode CreateChildSceneNode( string name, Vector3 translate, Quaternion rotate )
		{
			return (SceneNode)CreateChild( name, translate, rotate );
		}

		/// <summary>
		///    Creates a new child scene node.
		/// </summary>
		public virtual SceneNode CreateChildSceneNode()
		{
			return CreateChildSceneNode( Vector3.Zero, Quaternion.Identity );
		}

		/// <summary>
		///    Creates a new child scene node.
		/// </summary>
		/// <param name="translate">A vector to specify the position relative to the parent.</param>
		/// <returns></returns>
		public virtual SceneNode CreateChildSceneNode( Vector3 translate )
		{
			return CreateChildSceneNode( translate, Quaternion.Identity );
		}

		/// <summary>
		///    Creates a new child scene node.
		/// </summary>
		/// <param name="translate">A vector to specify the position relative to the parent.</param>
		/// <param name="rotate">A quaternion to specify the orientation relative to the parent.</param>
		/// <returns></returns>
		public virtual SceneNode CreateChildSceneNode( Vector3 translate, Quaternion rotate )
		{
			return (SceneNode)CreateChild( translate, rotate );
		}

		/// <summary>
		/// Rekeys the scene object using its new Name
		/// </summary>
		/// <param name="obj"></param>
		public virtual void NotifyAttachedObjectNameChanged( MovableObject obj )
		{
			objectList.Remove( obj.Name );
			objectList.Add( obj );
		}

		/// <summary>
		///    Removes all currently attached SceneObjects from this SceneNode.
		/// </summary>
		/// <remarks>
		///    Bounds for this SceneNode are also updated.
		/// </remarks>
		public virtual void DetachAllObjects()
		{
			// notify each object that it was removed (sending in null sets its parent scene node to null)
			foreach ( MovableObject obj in objectList.Values )
			{
				obj.NotifyAttached( null );
			}
			objectList.Clear();

			// Make sure bounds get updated (must go right to the top)
			NeedUpdate();
		}

		/// <summary>
		///    Removes the specifed object from this scene node.
		/// </summary>
		/// <remarks>
		///    Bounds for this SceneNode are also updated.
		/// </remarks>
		/// <param name="obj">Reference to the object to remove.</param>
		public virtual void DetachObject( MovableObject obj )
		{
			Debug.Assert( obj != null, "obj != null" );

			objectList.Remove( obj.Name );

			// notify the object that it was removed (sending in null sets its parent scene node to null)
			obj.NotifyAttached( null );

			// Make sure bounds get updated (must go right to the top)
			NeedUpdate();
		}

		/// <summary>
		/// Returns a movable object attached to this node by name. Node that this method
		/// is O(n), whereas the integer overload of this method is O(1). Use the integer
		/// version of this method if speed is important.
		/// </summary>
		/// <param name="name">The name of the object to return.</param>
		/// <returns>MovableObject if found. Throws exception of not found.</returns>
		public MovableObject GetObject( string name )
		{
			if ( this.objectList.ContainsKey( name ) )
			{
				return this.objectList[ name ];
			}

			throw new IndexOutOfRangeException( "Invalid key specified." );
		}

		/// <summary>
		/// Returns a movable object attached to this node by index. Node that this method
		/// is O(n), whereas the string overload of this method is O(1). Use the string
		/// version of this method if speed is important.
		/// </summary>
		/// <param name="name">The name of the object to return.</param>
		/// <returns>MovableObject if found. Throws exception of not found.</returns>
		public MovableObject GetObject( int index )
		{
			int i = 0;
			foreach ( MovableObject mo in this.objectList.Values )
			{
				if ( i == index )
					return mo;
				i++;
			}
			throw new IndexOutOfRangeException( "Invalid index specified." );
		}

		/// <summary>
		/// This method removes and destroys the child and all of its children.
		/// </summary>
		/// <param name="name">name of the node to remove and destroy</param>
		/// <remarks>
		/// Unlike removeChild, which removes a single named child from this
		/// node but does not destroy it, this method destroys the child
		/// and all of it's children.
		/// <para>
		/// Use this if you wish to recursively destroy a node as well as
		/// detaching it from it's parent. Note that any objects attached to
		/// the nodes will be detached but will not themselves be destroyed.
		/// </para>
		/// </remarks>
		public virtual void RemoveAndDestroyChild( String name )
		{
			SceneNode child = (SceneNode)this.GetChild( name );
			RemoveAndDestroyChild( child );
		}

		/// <summary>
		/// This method removes and destroys the child and all of its children.
		/// </summary>
		/// <param name="sceneNode">The node to remove and destroy</param>
		/// <remarks>
		/// Unlike removeChild, which removes a single named child from this
		/// node but does not destroy it, this method destroys the child
		/// and all of it's children.
		/// <para>
		/// Use this if you wish to recursively destroy a node as well as
		/// detaching it from it's parent. Note that any objects attached to
		/// the nodes will be detached but will not themselves be destroyed.
		/// </para>
		/// </remarks>
		public virtual void RemoveAndDestroyChild( SceneNode sceneNode )
		{
			sceneNode.RemoveAndDestroyAllChildren();

			this.RemoveChild( sceneNode );
			sceneNode.Creator.DestroySceneNode( sceneNode );
		}

		/// <summary>
		/// Removes and destroys all children in the subtree of this node.
		/// </summary>
		/// <remarks>
		/// Use this to destroy all nodes found in the child subtree of this node and remove
		/// them from the scene graph. Note that all objects attached to the
		/// nodes will be detached but will not be destroyed.
		/// </remarks>
		public virtual void RemoveAndDestroyAllChildren()
		{
			foreach ( SceneNode sn in childNodes.Values )
			{
				sn.RemoveAndDestroyAllChildren();

				// destroy but prevent removing from internal list yet
				this.RemoveChild( sn, false );
				sn.Creator.DestroySceneNode( sn, false );
			}

			// finish removal
			childNodes.Clear();

			NeedUpdate();
		}

		/// <summary>
		///		Internal method to update the Node.
		/// </summary>
		/// <remarks>
		///		Updates this scene node and any relevant children to incorporate transforms etc.
		///		Don't call this yourself unless you are writing a SceneManager implementation.
		/// </remarks>
		/// <param name="updateChildren"></param>
		/// <param name="hasParentChanged"></param>
		protected internal override void Update( bool updateChildren, bool hasParentChanged )
		{
			// call base class method
			base.Update( updateChildren, hasParentChanged );

			UpdateBounds();

			lightListDirty = true;
		}

        /// <summary>
        /// 
        /// </summary>
        protected override void UpdateFromParent()
        {
            base.UpdateFromParent();

            // Notify objects that it has been moved
            foreach(MovableObject currentObject in this.objectList.Values)
                currentObject.NotifyMoved();
        }

		/// <summary>
		///		Overloaded method.
		/// </summary>
		/// <param name="camera"></param>
		/// <param name="queue"></param>
		public virtual void FindVisibleObjects( Camera camera, RenderQueue queue )
		{
			// call overloaded method
			FindVisibleObjects( camera, queue, true, false, false );
		}

		/// <summary>
		///		Overloaded method.
		/// </summary>
		/// <param name="camera"></param>
		/// <param name="queue"></param>
		/// <param name="includeChildren"></param>
		/// <param name="displayNodes"></param>
		public virtual void FindVisibleObjects( Camera camera, RenderQueue queue, bool includeChildren, bool displayNodes )
		{
			// call overloaded method
			FindVisibleObjects( camera, queue, includeChildren, displayNodes, false );
		}

		/// <summary>
		///		Internal method which locates any visible objects attached to this node and adds them to the passed in queue.
		/// </summary>
		/// <param name="camera">Active camera.</param>
		/// <param name="queue">Queue to which these objects should be added.</param>
		/// <param name="includeChildren">If true, cascades down to all children.</param>
		/// <param name="displayNodes">Renders the local axes for the node.</param>
		/// <param name="onlyShadowCasters"></param>
		public virtual void FindVisibleObjects( Camera camera, RenderQueue queue, bool includeChildren, bool displayNodes, bool onlyShadowCasters )
		{
			// if we aren't visible, then quit now
			// TODO: Make sure sphere is calculated properly for all objects, then switch to cull using that
			if ( !camera.IsObjectVisible( worldAABB ) )
				return;

			// add visible objects to the render queue
			//objectListMeter.Enter();
			foreach ( MovableObject obj in objectList.Values )
			{
				// tell attached object about current camera in case it wants to know
				//notifyCameraMeter.Enter();
				obj.NotifyCurrentCamera( camera );
				//notifyCameraMeter.Exit();

				// if this object is visible, add it to the render queue
				if ( obj.IsVisible &&
					( !onlyShadowCasters || obj.CastShadows ) )
				{
					//updateQueueMeter.Enter();
					obj.UpdateRenderQueue( queue );
					//updateQueueMeter.Exit();
				}
			}
			//objectListMeter.Exit();

			//childListMeter.Enter();
			if ( includeChildren )
			{
				// ask all child nodes to update the render queue with visible objects
				foreach ( SceneNode childNode in childNodes.Values )
				{
					if ( childNode.IsVisible )
						childNode.FindVisibleObjects( camera, queue, includeChildren, displayNodes, onlyShadowCasters );
				}
			}
			//childListMeter.Exit();

			// if we wanna display nodes themself..
			if ( displayNodes )
			{
				// hey, lets just add ourself right to the render queue
				queue.AddRenderable( GetDebugRenderable() );
			}

			// do we wanna show our beautiful bounding box?
			// do it if either we want it, or the SceneManager dictates it
			if ( showBoundingBox || ( creator != null && creator.ShowBoundingBoxes ) )
			{
				AddBoundingBoxToQueue( queue );
			}
		}

		public override Node.DebugRenderable GetDebugRenderable()
		{
			Vector3 hs = this.worldAABB.HalfSize;
			Real sz = Utility.Min( hs.x, hs.y );
			sz = Utility.Min( sz, hs.z );
			sz = Utility.Max( sz, (Real)1.0 );
			return base.GetDebugRenderable( sz );
		}

		/// <summary>
		///		Adds this nodes bounding box (wireframe) to the RenderQueue.
		/// </summary>
		/// <param name="queue"></param>
		public void AddBoundingBoxToQueue( RenderQueue queue )
		{
			if ( wireBox == null )
				wireBox = new WireBoundingBox();

			// add the wire bounding box to the render queue
			wireBox.BoundingBox = worldAABB;
			queue.AddRenderable( wireBox );
		}

		/// <summary>
		///		Tell the SceneNode to update the world bound info it stores.
		/// </summary>
		protected virtual void UpdateBounds()
		{
			// reset bounds
			worldAABB.IsNull = true;
			worldBoundingSphere.Center = this.DerivedPosition;
			float radius = worldBoundingSphere.Radius = 0;

			// update bounds from attached objects
			foreach ( MovableObject obj in objectList.Values )
			{
				// update
				worldAABB.Merge( obj.GetWorldBoundingBox( true ) );
				radius = Utility.Max( obj.BoundingRadius, radius );
			}

			// merge with Children
			foreach ( SceneNode child in childNodes.Values )
			{
				// merge our bounding box with that of the child node
				worldAABB.Merge( child.worldAABB );
				radius = Utility.Max( child.worldBoundingSphere.Radius, radius );
			}

			worldBoundingSphere.Radius = radius;

		}

		/// <summary>
		///		Tells the node whether to yaw around it's own local Y axis or a fixed axis of choice.
		/// </summary>
		/// <remarks>
		///		This method allows you to change the yaw behavior of the node - by default, it
		///		yaws around it's own local Y axis when told to yaw with <see cref="TransformSpace.Local"/>,
		///		this makes it yaw around a fixed axis.
		///		You only really need this when you're using auto tracking (<see cref="SetAutoTracking"/>,
		///		because when you're manually rotating a node you can specify the <see cref="TransformSpace"/>
		///		in which you wish to work anyway.
		/// </remarks>
		/// <param name="useFixed">
		///		If true, the axis passed in the second parameter will always be the yaw axis no
		///		matter what the node orientation. If false, the node returns to it's default behavior.
		/// </param>
		/// <param name="fixedAxis">The axis to use if the first parameter is true.</param>
		public void SetFixedYawAxis( bool useFixed, Vector3 fixedAxis )
		{
			isYawFixed = useFixed;
			yawFixedAxis = fixedAxis;
		}

		/// <summary>
		///		Sets a default fixed yaw axis of Y.
		/// </summary>
		/// <param name="useFixed"></param>
		public void SetFixedYawAxis( bool useFixed )
		{
			SetFixedYawAxis( useFixed, Vector3.UnitY );
		}

		/// <summary>
		///		Overridden to apply fixed yaw axis behavior.
		/// </summary>
		/// <param name="degrees"></param>
		public override void Yaw( float degrees )
		{
			Vector3 yAxis;

			if ( isYawFixed )
			{
				// Rotate around fixed yaw axis
				yAxis = yawFixedAxis;
			}
			else
			{
				// Rotate around local Y axis
				yAxis = orientation * Vector3.UnitY;
			}

			Rotate( yAxis, degrees );
		}


		/// <summary>
		///		Points the local Z direction of this node at a point in space.
		/// </summary>
		/// <param name="target">A vector specifying the look at point.</param>
		/// <param name="relativeTo">The space in which the point resides.</param>
		/// <param name="localDirection">
		///		The vector which normally describes the natural direction of the node, usually -Z.
		///	</param>
		public void LookAt( Vector3 target, TransformSpace relativeTo, Vector3 localDirection )
		{
			SetDirection( target - this.DerivedPosition, relativeTo, localDirection );
		}

		public void LookAt( Vector3 target, TransformSpace relativeTo )
		{
			LookAt( target, relativeTo, Vector3.NegativeUnitZ );
		}

		/// <summary>
		///		Enables / disables automatic tracking of another SceneNode.
		/// </summary>
		/// <remarks>
		///		If you enable auto-tracking, this SceneNode will automatically rotate to
		///		point it's -Z at the target SceneNode every frame, no matter how
		///		it or the other SceneNode move. Note that by default the -Z points at the
		///		origin of the target SceneNode, if you want to tweak this, provide a
		///		vector in the 'offset' parameter and the target point will be adjusted.
		/// </remarks>
		/// <param name="enabled">
		///		If true, tracking will be enabled and the 'target' cannot be null.
		///		If false tracking will be disabled and the current orientation will be maintained.
		/// </param>
		/// <param name="target">
		///		Reference to the SceneNode to track. Can be null if and only if the enabled param is false.
		/// </param>
		/// <param name="localDirection">
		///		The local vector considered to be the usual 'direction'
		///		of the node; normally the local -Z but can be another direction.
		/// </param>
		/// <param name="offset">
		///		If supplied, this is the target point in local space of the target node
		///		instead of the origin of the target node. Good for fine tuning the look at point.
		/// </param>
		public void SetAutoTracking( bool enabled, SceneNode target, Vector3 localDirection, Vector3 offset )
		{
			if ( enabled )
			{
				autoTrackTarget = target;
				autoTrackOffset = offset;
				autoTrackLocalDirection = localDirection;
			}
			else
			{
				autoTrackTarget = null;
			}

			if ( creator != null )
			{
				creator.NotifyAutoTrackingSceneNode( this, enabled );
			}
		}

		public void SetAutoTracking( bool enabled, SceneNode target, Vector3 localDirection )
		{
			SetAutoTracking( enabled, target, localDirection, Vector3.Zero );
		}

		public void SetAutoTracking( bool enabled, SceneNode target )
		{
			SetAutoTracking( enabled, target, Vector3.NegativeUnitZ, Vector3.Zero );
		}

		public void SetAutoTracking( bool enabled )
		{
			SetAutoTracking( enabled, null, Vector3.NegativeUnitZ, Vector3.Zero );
		}

		/// <summary>
		///		Sets the node's direction vector ie it's local -z.
		/// </summary>
		/// <remarks>
		///		Note that the 'up' vector for the orientation will automatically be
		///		recalculated based on the current 'up' vector (i.e. the roll will
		///		remain the same). If you need more control, use the <see cref="Orientation"/>
		///		property.
		/// </remarks>
		/// <param name="x">The x component of the direction vector.</param>
		/// <param name="y">The y component of the direction vector.</param>
		/// <param name="z">The z component of the direction vector.</param>
		/// <param name="relativeTo">The space in which this direction vector is expressed.</param>
		/// <param name="localDirection">The vector which normally describes the natural direction
		///		of the node, usually -Z.
		///	</param>
		public void SetDirection( Real x, Real y, Real z, TransformSpace relativeTo, Vector3 localDirectionVector )
		{
			Vector3 dir;
			dir.x = x;
			dir.y = y;
			dir.z = z;
			SetDirection( dir, relativeTo, localDirectionVector );
		}

		public void SetDirection( Real x, Real y, Real z )
		{
			SetDirection( x, y, z, TransformSpace.Local, Vector3.NegativeUnitZ );
		}
		public void SetDirection( Real x, Real y, Real z, TransformSpace relativeTo )
		{
			SetDirection( x, y, z, relativeTo, Vector3.NegativeUnitZ );
		}

		/// <summary>
		///		Sets the node's direction vector ie it's local -z.
		/// </summary>
		/// <remarks>
		///		Note that the 'up' vector for the orientation will automatically be
		///		recalculated based on the current 'up' vector (i.e. the roll will
		///		remain the same). If you need more control, use the <see cref="Orientation"/>
		///		property.
		/// </remarks>
		/// <param name="vec">The direction vector.</param>
		/// <param name="relativeTo">The space in which this direction vector is expressed.</param>
		/// <param name="localDirection">The vector which normally describes the natural direction
		///		of the node, usually -Z.
		///	</param>
		public void SetDirection( Vector3 vec, TransformSpace relativeTo, Vector3 localDirection )
		{
			// Do nothing if given a zero vector
			if ( vec == Vector3.Zero )
			{
				return;
			}

			// Adjust vector so that it is relative to local Z
			Vector3 zAdjustVec;

			if ( localDirection == Vector3.NegativeUnitZ )
			{
				zAdjustVec = -vec;
			}
			else
			{
				Quaternion localToUnitZ = localDirection.GetRotationTo( Vector3.UnitZ );
				zAdjustVec = localToUnitZ * vec;
			}

			zAdjustVec.Normalize();

			Quaternion targetOrientation;

			if ( isYawFixed )
			{
				Vector3 xVec = yawFixedAxis.Cross( zAdjustVec );
				xVec.Normalize();

				Vector3 yVec = zAdjustVec.Cross( xVec );
				yVec.Normalize();

				targetOrientation = Quaternion.FromAxes( xVec, yVec, zAdjustVec );
			}
			else
			{
				Vector3 xAxis, yAxis, zAxis;

				// Get axes from current quaternion
				// get the vector components of the derived orientation vector
				this.DerivedOrientation.ToAxes( out xAxis, out yAxis, out zAxis );

				Quaternion rotationQuat;

				if ( ( zAxis + zAdjustVec ).LengthSquared < 0.00000001f )
				{
					// Oops, a 180 degree turn (infinite possible rotation axes)
					// Default to yaw i.e. use current UP
					rotationQuat = Quaternion.FromAngleAxis( Utility.PI, yAxis );
				}
				else
				{
					// Derive shortest arc to new direction
					rotationQuat = zAxis.GetRotationTo( zAdjustVec );
				}

				targetOrientation = rotationQuat * orientation;
			}

			if ( relativeTo == TransformSpace.Local || parent != null )
			{
				orientation = targetOrientation;
			}
			else
			{
				if ( relativeTo == TransformSpace.Parent )
				{
					orientation = targetOrientation * parent.Orientation.Inverse();
				}
				else if ( relativeTo == TransformSpace.World )
				{
					orientation = targetOrientation * parent.DerivedOrientation.Inverse();
				}
			}
		}

		public void SetDirection( Vector3 vec )
		{
			SetDirection( vec, TransformSpace.Local, Vector3.NegativeUnitZ );
		}

		public void SetDirection( Vector3 vec, TransformSpace relativeTo )
		{
			SetDirection( vec, relativeTo, Vector3.NegativeUnitZ );
		}

		/// <summary>
		///    Allows retrieval of the nearest lights to the center of this SceneNode.
		/// </summary>
		/// <remarks>
		///    This method allows a list of lights, ordered by proximity to the center of
		///    this SceneNode, to be retrieved. Multiple access to this method when neither
		///    the node nor the lights have moved will result in the same list being returned
		///    without recalculation. Can be useful when implementing IRenderable.Lights.
		/// </remarks>
		/// <param name="radius">Parameter to specify lights intersecting a given radius of
		///		this SceneNode's centre</param>
		/// <returns></returns>
		public virtual LightList FindLights( float radius )
		{
			// TEMP FIX
			// If a scene node is static and lights have moved, light list won't change
			// can't use a simple global boolean flag since this is only called for
			// visible nodes, so temporarily visible nodes will not be updated
			// Since this is only called for visible nodes, skip the check for now
			//if(lightListDirty) {
			if ( creator != null )
				creator.PopulateLightList( this.DerivedPosition, radius, lightList );
			else
				lightList.Clear();

			lightListDirty = false;
			//}

			return lightList;
		}

		/// <summary>
		///		Internal method used to update auto-tracking scene nodes.
		/// </summary>
		internal void AutoTrack()
		{
			if ( autoTrackTarget != null )
			{
				LookAt(
					autoTrackTarget.DerivedPosition + autoTrackOffset,
					TransformSpace.World,
					autoTrackLocalDirection );

				// update self and children
				Update( true, true );
			}
		}

		#endregion Methods

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
		protected override Node CreateChildImpl( string name )
		{
			SceneNode newNode = creator.CreateSceneNode( name );
			return newNode;
		}

		#endregion Implementation of Node
	}
}