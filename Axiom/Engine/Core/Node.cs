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
using System.Collections;
using System.Diagnostics;
using Axiom.Collections;
using Axiom.Core;
using Axiom.Enumerations;
using Axiom.MathLib;
using Axiom.SubSystems.Rendering;

namespace Axiom.Core
{
	/// <summary>
	///		Class representing a general-purpose node an articulated scene graph.
	/// </summary>
	/// <remarks>
	///		A node in the scene graph is a node in a structured tree. A node contains
	///		information about the transformation which will apply to
	///		it and all of it's children. Child nodes can have transforms of their own, which
	///		are combined with their parent's transformations.
	///		
	///		This is an abstract class - concrete classes are based on this for specific purposes,
	///		e.g. SceneNode, Bone
	///	</remarks>
	public abstract class Node : IRenderable
	{
		#region Protected member variables

		/// <summary>Name of this node.</summary>
		protected String name;
		/// <summary>Parent node (if any)</summary>
		protected Node parent;
		/// <summary>Collection of this nodes child nodes.</summary>
		protected NodeCollection childNodes;
		/// <summary>Collection of this nodes child nodes.</summary>
		protected ArrayList childrenToUpdate;
		/// <summary>Flag to indicate if our transform is out of date.</summary>
		protected bool needUpdate;
		/// <summary>Orientation of this node relative to it's parent.</summary>
		protected Quaternion orientation;
		/// <summary>World orientation of this node based on parents orientation.</summary>
		protected Quaternion derivedOrientation;
		/// <summary>Original orientation of this node, used for resetting to original.</summary>
		protected Quaternion initialOrientation;
		/// <summary></summary>
		protected Quaternion rotationFromInitial;
		/// <summary>Position of this node relative to it's parent.</summary>
		protected Vector3 position;
		/// <summary></summary>
		protected Vector3 derivedPosition;
		/// <summary></summary>
		protected Vector3 initialPosition;
		/// <summary></summary>
		protected Vector3 translationFromInitial;
		/// <summary></summary>
		protected Vector3 scale;
		/// <summary></summary>
		protected Vector3 derivedScale;
		/// <summary></summary>
		protected Vector3 initialScale;
		/// <summary></summary>
		protected Vector3 scaleFromInitial;
		/// <summary></summary>
		protected bool inheritsScale;
		/// <summary>Weight of applied animations so far, used for blending.</summary>
		protected float accumAnimWeight;
		/// <summary>Cached derived transform as a 4x4 matrix.</summary>
		protected Matrix4 cachedTransform;

		#endregion

		#region Static member variables
		
		protected static Material material = null;
		protected static SubMesh subMesh = null;
		protected static long nextUnnamedNodeExtNum = 1;
		
		#endregion

		#region Constructors

		public Node()
		{
			this.name = "Unnamed_" + nextUnnamedNodeExtNum++;

			parent = null;

			// initialize objects
			orientation = initialOrientation = derivedOrientation = Quaternion.Identity;
			position = initialPosition = derivedPosition = Vector3.Zero;
			scale = initialScale = derivedScale = Vector3.UnitScale;
			cachedTransform = Matrix4.Identity;

			inheritsScale = true;

			accumAnimWeight = 0.0f;

			childNodes = new NodeCollection(this);
			childrenToUpdate = new ArrayList();

			// add events for the child collection
			childNodes.Cleared += new CollectionHandler(this.ChildrenCleared);
			childNodes.ItemAdded += new CollectionHandler(this.ChildAdded);
			childNodes.ItemRemoved += new CollectionHandler(this.ChildRemoved);

			NeedUpdate();
		}

		public Node(String name)
		{
			this.name = name;

			// initialize objects
			orientation = initialOrientation = derivedOrientation = Quaternion.Identity;
			position = initialPosition = derivedPosition = Vector3.Zero;
			scale = initialScale = derivedScale = Vector3.UnitScale;
			cachedTransform = Matrix4.Identity;

			inheritsScale = true;

			accumAnimWeight = 0.0f;

			childNodes = new NodeCollection(this);
			childrenToUpdate = new ArrayList();

			// add events for the child collection
			childNodes.Cleared += new CollectionHandler(this.ChildrenCleared);
			childNodes.ItemAdded += new CollectionHandler(this.ChildAdded);
			childNodes.ItemRemoved += new CollectionHandler(this.ChildRemoved);

			NeedUpdate();
		}


		#endregion

		#region Public methods

		/// <summary>
		/// Scales the node, combining it's current scale with the passed in scaling factor. 
		/// </summary>
		/// <remarks>
		///	This method applies an extra scaling factor to the node's existing scale, (unlike setScale
		///	which overwrites it) combining it's current scale with the new one. E.g. calling this 
		///	method twice with Vector3(2,2,2) would have the same effect as setScale(Vector3(4,4,4)) if
		/// the existing scale was 1.
		/// 
		///	Note that like rotations, scalings are oriented around the node's origin.
		///</remarks>
		/// <param name="scale"></param>
		virtual public void Scale(Vector3 factor)
		{
			scale = scale * factor;
			NeedUpdate();
		}

		/// <summary>
		/// Moves the node along the cartesian axes.
		///
		///	This method moves the node by the supplied vector along the
		///	world cartesian axes, i.e. along world x,y,z
		/// </summary>
		/// <param name="scale">Vector with x,y,z values representing the translation.</param>
		virtual public void Translate(Vector3 translate)
		{
			position = position + translate;
			NeedUpdate();
		}
		
		/// <summary>
		/// Moves the node along arbitrary axes.
		/// </summary>
		/// <remarks>
		///	This method translates the node by a vector which is relative to
		///	a custom set of axes.
		///	</remarks>
		/// <param name="pAxes">3x3 Matrix containg 3 column vectors each representing the
		///	X, Y and Z axes respectively. In this format the standard cartesian axes would be expressed as:
		///		1 0 0
		///		0 1 0
		///		0 0 1
		///		i.e. The Identity matrix.
		///	</param>
		/// <param name="move">Vector relative to the supplied axes.</param>
		virtual public void Translate(Matrix3 axes, Vector3 move)
		{
			Vector3 derived = axes * move;
			Translate(derived);
		}


		/// <summary>
		/// Rotate the node around the X-axis.
		/// </summary>
		/// <param name="degrees"></param>
		virtual public void Pitch(float degrees)
		{
			Rotate(Vector3.UnitX, degrees);
		}		
		
		/// <summary>
		/// Rotate the node around the Z-axis.
		/// </summary>
		/// <param name="degrees"></param>
		virtual public void Roll(float degrees)
		{
			Rotate(Vector3.UnitZ, degrees);
		}

		/// <summary>
		/// Rotate the node around the Y-axis.
		/// </summary>
		/// <param name="degrees"></param>
		virtual public void Yaw(float degrees)
		{
			Rotate(Vector3.UnitY, degrees);
		}

		/// <summary>
		/// Rotate the node around an arbitrary axis.
		/// </summary>
		virtual public void Rotate(Vector3 axis, float degrees)
		{
			Quaternion q = Quaternion.FromAngleAxis(MathUtil.DegreesToRadians(degrees), axis);
			Rotate(q);
		}

		/// <summary>
		/// Rotate the node around an arbitrary axis using a Quaternion.
		/// </summary>
		virtual public void Rotate(Quaternion rotation)
		{
			orientation = orientation * rotation;
			NeedUpdate();
		}

		/// <summary>
		/// Resets the nodes orientation (local axes as world axes, no rotation).
		/// </summary>
		virtual public void ResetOrientation()
		{
			orientation = Quaternion.Identity;
			NeedUpdate();
		}
		
		/// <summary>
		/// Resets the position / orientation / scale of this node to it's initial state, see SetInitialState for more info.
		/// </summary>
		virtual public void ResetToInitialState()
		{
			position = initialPosition;
			orientation = initialOrientation;
			scale = initialScale;

			// Reset weights
			accumAnimWeight = 0.0f;
			translationFromInitial = Vector3.Zero;
			rotationFromInitial = Quaternion.Identity;
			scaleFromInitial = Vector3.UnitScale;

			NeedUpdate();
		}

		/// <summary>
		/// Sets the current transform of this node to be the 'initial state' ie that
		///	position / orientation / scale to be used as a basis for delta values used
		/// in keyframe animation.
		/// </summary>
		/// <remarks>
		///	You never need to call this method unless you plan to animate this node. If you do
		///	plan to animate it, call this method once you've loaded the node with it's base state,
		///	ie the state on which all keyframes are based.
		///
		///	If you never call this method, the initial state is the identity transform, ie do nothing.
		/// </remarks>
		virtual public void SetInitialState()
		{
			initialOrientation = orientation;
			initialPosition = position;
			initialScale = scale;
		}
		/// <summary>
		/// Creates a new name child node.
		/// </summary>
		/// <param name="pName"></param>
		virtual public Node CreateChild(String name)
		{
			Node newChild = CreateChildImpl(name);
			newChild.Translate(Vector3.Zero);
			newChild.Rotate(Quaternion.Identity);
			ChildNodes.Add(newChild);

			return newChild;
		}

		/// <summary>
		/// Creates a new named child node.
		/// </summary>
		/// <param name="name">Name of the node.</param>
		/// <param name="translate">A vector to specify the position relative to the parent.</param>
		/// <param name="rotate">A quaternion to specify the orientation relative to the parent.</param>
		/// <returns></returns>
		virtual public Node CreateChild(String name, Vector3 translate, Quaternion rotate)
		{
			Node newChild = CreateChildImpl(name);
			newChild.Translate(translate);
			newChild.Rotate(rotate);
			ChildNodes.Add(newChild);

			return newChild;
		}
		
		/// <summary>
		/// Creates a new Child node.
		/// </summary>
		virtual public Node CreateChild()
		{
			Node newChild = CreateChildImpl();
			newChild.Translate(Vector3.Zero);
			newChild.Rotate(Quaternion.Identity);
			ChildNodes.Add(newChild);

			return newChild;
		}

		/// <summary>
		/// Creates a new child node.
		/// </summary>
		/// <param name="translate">A vector to specify the position relative to the parent.</param>
		/// <param name="rotate">A quaternion to specify the orientation relative to the parent.</param>
		/// <returns></returns>
		virtual public Node CreateChild(Vector3 translate, Quaternion rotate)
		{
			Node newChild = CreateChildImpl();
			newChild.Translate(translate);
			newChild.Rotate(rotate);
			ChildNodes.Add(newChild);

			return newChild;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="camera"></param>
		/// <returns></returns>
		public float GetSquaredViewDepth(Camera camera)
		{
			Vector3 difference = this.DerivedPosition - camera.DerivedPosition;

			// return squared length to avoid doing a square root when it is not imperative
			return difference.LengthSquared;
		}

		/// <summary>
		///		To be called in the event of transform changes to this node that require it's recalculation.
		/// </summary>
		/// <remarks>
		///		This not only tags the node state as being 'dirty', it also requests it's parent to 
		///		know about it's dirtiness so it will get an update next time.
		/// </remarks>
		virtual public void NeedUpdate()
		{
			needUpdate = true;

			// make sure we are not the root node
			if(parent != null)
			{
				parent.RequestUpdate(this);
			}

			// all children will be updated shortly
			childrenToUpdate.Clear();
		}

		/// <summary>
		///		Called by children to notify their parent that they need an update.
		/// </summary>
		/// <param name="child"></param>
		virtual public void RequestUpdate(Node child)
		{
			// if we are already going to update everything, this wont matter
			if(needUpdate)
				return;

			// add to the list of children that need updating
			//if(!childrenToUpdate.ContainsKey(child.name))
			if(!childrenToUpdate.Contains(child))
				childrenToUpdate.Add(child);

			// request to update me
			if(parent != null)
				parent.RequestUpdate(this);
		}

		/// <summary>
		///		Called by children to notify their parent that they no longer need an update.
		/// </summary>
		/// <param name="child"></param>
		virtual public void CancelUpdate(Node child)
		{
			// remove this from the list of children to update
			childrenToUpdate.Remove(child);

			// propogate this changed if we are done
			if(childrenToUpdate.Count == 0 && parent != null)
				parent.CancelUpdate(this);
		}

		#endregion

		#region Public Properties
		/// <summary>
		/// The name of this Node object.  It is autogenerated initially, so setting it is optional.
		/// </summary>
		public String Name
		{
			get { return name;	}
			set { name = value; }
		}

		/// <summary>
		/// Get the Parent Node of the current Node.
		/// </summary>
		virtual public Node Parent
		{
			get { return parent; }
			set 
			{ 
				parent = value; 
				NeedUpdate();
			}
		}

		/// <summary>
		/// A list of child nodes for this Node object.
		/// </summary>
		public NodeCollection ChildNodes
		{
			get { return childNodes; }
		}

		/// <summary>
		/// A Quaternion representing the nodes orientation.
		/// </summary>
		virtual public Quaternion Orientation
		{
			get { return orientation; }
			set 
			{ 
				orientation = value; 
				NeedUpdate();
			}
		}

		/// <summary>
		/// The position of the node relative to its parent.
		/// </summary>
		virtual public Vector3 Position
		{
			get { return position; }
			set 
			{	
				position = value;  
				NeedUpdate();
			}
		}

		/// <summary>
		/// The scaling factor applied to this node.
		/// </summary>
		/// <remarks>
		///	Scaling factors, unlike other transforms, are not always inherited by child nodes. 
		///	Whether or not scalings affect both the size and position of the child nodes depends on
		///	the setInheritScale option of the child. In some cases you want a scaling factor of a parent node
		///	to apply to a child node (e.g. where the child node is a part of the same object, so you
		///	want it to be the same relative size and position based on the parent's size), but
		///	not in other cases (e.g. where the child node is just for positioning another object,
		///	you want it to maintain it's own size and relative position). The default is to inherit
		///	as with other transforms.
		///
		///	Note that like rotations, scalings are oriented around the node's origin.
		///	</remarks>
		virtual public Vector3 ScaleFactor
		{
			get { return scale; }
			set 
			{ 
				scale = value; 
				NeedUpdate();  
			}
		}

		/// <summary>
		/// Tells the node whether it should inherit scaling factors from it's parent node.
		/// </summary>
		/// <remarks>
		///	Scaling factors, unlike other transforms, are not always inherited by child nodes. 
		///	Whether or not scalings affect both the size and position of the child nodes depends on
		///	the setInheritScale option of the child. In some cases you want a scaling factor of a parent node
		///	to apply to a child node (e.g. where the child node is a part of the same object, so you
		///	want it to be the same relative size and position based on the parent's size), but
		///	not in other cases (e.g. where the child node is just for positioning another object,
		///	you want it to maintain it's own size and relative position). The default is to inherit
		///	as with other transforms.
		///	If true, this node's scale and position will be affected by its parent's scale. If false,
		///	it will not be affected.
		///</remarks>
		virtual public bool InheritScale
		{
			get { return inheritsScale; }
			set 
			{ 
				inheritsScale = value; 
				NeedUpdate();  
			}
		}

		/// <summary>
		/// Gets a matrix whose columns are the local axes based on
		/// the nodes orientation relative to it's parent.
		/// </summary>
		virtual public Matrix3 LocalAxes
		{
			get 
			{	
				// get the 3 unit Vectors
				Vector3 xAxis = Vector3.UnitX;
				Vector3 yAxis = Vector3.UnitY;
				Vector3 zAxis = Vector3.UnitZ;

				// multpliy each times the current orientation
				xAxis = orientation * xAxis;
				yAxis = orientation * yAxis;
				zAxis = orientation * zAxis;

				return new Matrix3(xAxis, yAxis, zAxis);
			}
		}

		#endregion

		#region Protected methods
		/// <summary>
		///	Triggers the node to update it's combined transforms.
		///
		///	This method is called internally by the engine to ask the node
		///	to update it's complete transformation based on it's parents
		///	derived transform.
		/// </summary>
		// TODO: This was previously protected.  Was made internal to allow access to custom collections.
		virtual internal void UpdateFromParent()
		{
			if(parent != null)
			{
				// combine local orientation with parents
				Quaternion parentOrientation = parent.DerivedOrientation;
				derivedOrientation = parentOrientation * orientation;

				// change position vector based on parent's orientation
				derivedPosition = parentOrientation * position;

				// update scale
				if(inheritsScale)
				{
					// set out own position by parent scale
					Vector3 parentScale = parent.DerivedScale;
					derivedPosition = derivedPosition * parentScale;

					// set own scale, just combine as equivalent axes, no shearing
					derivedScale = scale * parentScale;
				}
				else
				{
					// do not inherit parents scale
					derivedScale = scale;
				}

				// add parents positition to local altered position
				derivedPosition += parent.DerivedPosition;
			}
			else
			{
				// Root node, no parent
				derivedOrientation = orientation;
				derivedPosition = position;
				derivedScale = scale;
			}

		}

		/// <summary>
		/// Internal method for building a Matrix4 from orientation / scale / position. 
		/// </summary>
		/// <remarks>
		///	Transform is performed in the order rotate, scale, translation, i.e. translation is independent
		///	of orientation axes, scale does not affect size of translation, rotation and scaling are always
		///	centered on the origin.
		///	</remarks>
		/// <param name="position"></param>
		/// <param name="scale"></param>
		/// <param name="orientation"></param>
		/// <returns></returns>
		protected Matrix4 MakeTransform(Vector3 position, Vector3 scale, Quaternion orientation)
		{
			Matrix4 destMatrix = Matrix4.Identity;

			// Ordering:
			//    1. Scale
			//    2. Rotate
			//    3. Translate

			// Parent scaling is already applied to derived position
			// Own scale is applied before rotation
			Matrix3 rot3x3;
			Matrix3 scale3x3;
			rot3x3 = orientation.ToRotationMatrix();
			scale3x3 = Matrix3.Zero;
			scale3x3[0,0] = scale.x;
			scale3x3[1,1] = scale.y;
			scale3x3[2,2] = scale.z;

			destMatrix = rot3x3 * scale3x3;

			destMatrix.Translation = position;

			return destMatrix;
		}

		/// <summary>
		/// Internal method for building an inverse Matrix4 from orientation / scale / position. 
		/// </summary>
		/// <remarks>
		///	As makeTransform except it build the inverse given the same data as makeTransform, so
		///	performing -translation, 1/scale, -rotate in that order.
		/// </remarks>
		/// <param name="position"></param>
		/// <param name="scale"></param>
		/// <param name="orientation"></param>
		/// <returns></returns>
		protected Matrix4 MakeInverseTransform(Vector3 position, Vector3 scale, Quaternion orientation)
		{
			Matrix4 destMatrix = Matrix4.Identity;

			// Invert the parameters
			Vector3 invTranslate = -position;
			Vector3 invScale = Vector3.Zero;

			invScale.x = 1.0f / scale.x;
			invScale.y = 1.0f / scale.y;
			invScale.z = 1.0f / scale.z;

			Quaternion invRot = orientation.Inverse();
        
			// Because we're inverting, order is translation, rotation, scale
			// So make translation relative to scale & rotation
			invTranslate.x *= invScale.x; // scale
			invTranslate.y *= invScale.y; // scale
			invTranslate.z *= invScale.z; // scale
			invTranslate = invRot * invTranslate; // rotate

			// Next, make a 3x3 rotation matrix and apply inverse scale
			Matrix3 rot3x3 = invRot.ToRotationMatrix();
			Matrix3 scale3x3= Matrix3.Zero;

			scale3x3[0,0] = invScale.x;
			scale3x3[1,1] = invScale.y;
			scale3x3[2,2] = invScale.z;

			// Set up final matrix with scale & rotation
			destMatrix = scale3x3 * rot3x3;

			destMatrix.Translation = invTranslate;

			return destMatrix;
		}		

		/// <summary>
		/// Must be overridden in subclasses.  Specifies how a Node is created.  CreateChild uses this to create a new one and add it
		/// to the list of child nodes.  This allows subclasses to not have to override CreateChild and duplicate all its functionality.
		/// </summary>
		abstract protected Node CreateChildImpl();

		/// <summary>
		/// Must be overridden in subclasses.  Specifies how a Node is created.  CreateChild uses this to create a new one and add it
		/// to the list of child nodes.  This allows subclasses to not have to override CreateChild and duplicate all its functionality.
		/// </summary>
		/// <param name="name">The name of the node to add.</param>
		abstract protected Node CreateChildImpl(String name);

		#endregion

		#region Internal engine properties

		/// <summary>
		/// Gets the orientation of the node as derived from all parents.
		/// </summary>
		internal virtual Quaternion DerivedOrientation
		{
			get 
			{ 
				if(needUpdate)
					UpdateFromParent();
				
				return derivedOrientation;
			}
		}

		/// <summary>
		/// Gets the position of the node as derived from all parents.
		/// </summary>
		internal virtual Vector3 DerivedPosition
		{
			get 
			{ 
				if(needUpdate)
					UpdateFromParent();
				
				return derivedPosition;
			}
		}

		/// <summary>
		/// Gets the scaling factor of the node as derived from all parents.
		/// </summary>
		internal virtual Vector3 DerivedScale
		{
			get { return derivedScale; }
		}

		/// <summary>
		///	Gets the full transformation matrix for this node.
		/// </summary>
		/// <remarks>
		/// This method returns the full transformation matrix
		/// for this node, including the effect of any parent node
		/// transformations, provided they have been updated using the Node::Update method.
		/// This should only be called by a SceneManager which knows the
		/// derived transforms have been updated before calling this method.
		/// Applications using the engine should just use the relative transforms.
		/// </remarks>
		internal virtual Matrix4 FullTransform
		{
			get 
			{ 
				return MakeTransform(this.DerivedPosition, this.DerivedScale, this.DerivedOrientation); 
			}
		}

		#endregion

		#region Internal engine methods
		/// <summary>
		/// Internal method to update the Node.
		/// Updates this node and any relevant children to incorporate transforms etc.
		///	Don't call this yourself unless you are writing a SceneManager implementation.
		/// </summary>
		/// <param name="updateChildren">If true, the update cascades down to all children. Specify false if you wish to
		/// update children separately, e.g. because of a more selective SceneManager implementation.</param>
		internal virtual void Update(bool updateChildren, bool hasParentChanged)
		{
			// nothin to see here folks, move on
			if(!updateChildren && !needUpdate && !hasParentChanged)
				return;

			// see if we need to process all
			if(needUpdate || hasParentChanged)
			{
				// update from parent
				UpdateFromParent();

				// update all children
				for(int i = 0; i < childNodes.Count; i++)
				{
					Node child = childNodes[i];
					child.Update(true, true);
				}
			}
			else
			{
				// just update selected children
				for(int i = 0; i < childrenToUpdate.Count; i++)
				{
					Node child = (Node)childrenToUpdate[i];
					child.Update(true, false);
				}

				// clear the list
				childrenToUpdate.Clear();
			}

			// reset the flag
			needUpdate = false;
		}


		/// <summary>
		/// This method transforms a Node by a weighted amount from it's
		///	initial state. If weighted transforms have already been applied, 
		///	the previous transforms and this one are blended together based
		/// on their relative weight. This method should not be used in
		///	combination with the unweighted rotate, translate etc methods.
		/// </summary>
		/// <param name="weight"></param>
		/// <param name="translate"></param>
		/// <param name="rotate"></param>
		/// <param name="scale"></param>
		internal virtual void WeightedTransform(float weight, Vector3 translate, Quaternion rotate, Vector3 scale)
		{
			// If no previous transforms, we can just apply
			if (accumAnimWeight == 0.0f)
			{
				rotationFromInitial = rotate;
				translationFromInitial = translate;
				scaleFromInitial = scale;
				accumAnimWeight = weight;
			}
			else
			{
				// Blend with existing
				float factor = weight / (accumAnimWeight + weight);

				translationFromInitial += (translate - translationFromInitial) * factor;
				rotationFromInitial = Quaternion.Slerp(factor, rotationFromInitial, rotate);

				// For scale, find delta from 1.0, factor then add back before applying
				Vector3 scaleDiff = (scale - Vector3.UnitScale) * factor;
				scaleFromInitial = scaleFromInitial * (scaleDiff + Vector3.UnitScale);
				accumAnimWeight += weight;

			}

			// Update final based on bind position + offsets
			orientation = initialOrientation * rotationFromInitial;
			position = initialPosition + translationFromInitial;
			scale = initialScale * scaleFromInitial;
			
			NeedUpdate();
		}
		#endregion
	
		#region IRenderable implementation

		/// <summary>
		///		This is only used if the SceneManager chooses to render the node. This option can be set
		///		for SceneNodes at SceneManager.DisplaySceneNodes, and for entities based on skeletal 
		///		models using Entity.DisplaySkeleton = true.
		///	 </summary>
		public void GetRenderOperation(RenderOperation op)
		{
			// TODO: Implement GetRenderOperation
		}		
	
		/// <summary>
		///		
		/// </summary>
		/// <remarks>
		///		This is only used if the SceneManager chooses to render the node. This option can be set
		///		for SceneNodes at SceneManager.DisplaySceneNodes, and for entities based on skeletal 
		///		models using Entity.DisplaySkeleton = true.
		/// </remarks>
		public Matrix4[] WorldTransforms
		{
			get
			{
				// return the local full transformation
				return new Matrix4[] {FullTransform};
			}
		}
	
		/// <summary>
		///		
		/// </summary>
		/// <remarks>
		///		This is only used if the SceneManager chooses to render the node. This option can be set
		///		for SceneNodes at SceneManager.DisplaySceneNodes, and for entities based on skeletal 
		///		models using Entity.DisplaySkeleton = true.
		/// </remarks>
		public Material Material
		{
			get
			{
				// TODO:  Add Node.Material getter implementation
				return null;
			}
		}

		#endregion

		#region Event handlers

		public bool ChildAdded(object sender, EventArgs e)
		{
			Node child = (Node)sender;

			child.Parent = this;

			return false;
		}

		public bool ChildRemoved(object sender, EventArgs e)
		{
			Node child = (Node)sender;

			// TODO: Think about affects of other Nodes besides the root being null.
			//child.Parent = null;

			// cancel any pending updates to this child
			CancelUpdate(child);

			return false;
		}

		public bool ChildrenCleared(object sender, EventArgs e)
		{
			return false;
		}


		#endregion

		#region IRenderable Members

		public ushort NumWorldTransforms
		{
			// TODO: Finish
			get { return 1; }
		}

		public bool UseIdentityProjection
		{
			get { return false; }
		}

		public bool UseIdentityView
		{
			get { return false; }
		}

		public SceneDetailLevel RenderDetail
		{
			get { return SceneDetailLevel.Solid;	}
		}

		#endregion
	}

}
