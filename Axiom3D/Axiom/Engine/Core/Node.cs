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
using System.Collections;
using System.Diagnostics;

using Axiom.MathLib;

namespace Axiom
{
    #region Delegates
    /// <summary>
    /// Signature for the Node.UpdatedFromParent event which provides the newly-updated derived properties for syncronization in a physics engine for instance
    /// </summary>
    public delegate void NodeUpdateHandler( Vector3 derivedPosition, Quaternion derivedOrientation, Vector3 derivedScale );

    #endregion

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
    ///	<ogre headerVersion="1.39" sourceVersion="1.53" />
    public abstract class Node : IRenderable
    {
        #region Events
        /// <summary>
        /// Event which provides the newly-updated derived properties for syncronization in a physics engine for instance
        /// </summary>
        public event NodeUpdateHandler UpdatedFromParent;
        #endregion

        #region Protected member variables

        /// <summary>Name of this node.</summary>
        protected string name;
        /// <summary>Parent node (if any)</summary>
        protected Node parent;
        /// <summary>Collection of this nodes child nodes.</summary>
        protected NodeCollection childNodes;
        public NodeCollection Children
        {
            get
            {
                return childNodes;
            }
        }
        /// <summary>Collection of this nodes child nodes.</summary>
        protected NodeCollection childrenToUpdate;
        /// <summary>Flag to indicate own transform from parent is out of date.</summary>
        protected bool needParentUpdate;
        /// <summary>Flag to indicate all children need to be updated.</summary>
        protected bool needChildUpdate;
        /// <summary>Flag indicating that parent has been notified about update request.</summary>
        protected bool isParentNotified;
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
        /// <summary>Cached relative transform as a 4x4 matrix.</summary>
        protected Matrix4 cachedRelativeTransform;
        /// <summary></summary>
        protected bool needTransformUpdate;
        /// <summary></summary>
        protected bool needRelativeTransformUpdate;
        /// <summary>Material to be used is this node itself will be rendered (axes, or bones).</summary>
        protected Material nodeMaterial;
        /// <summary>SubMesh to be used is this node itself will be rendered (axes, or bones).</summary>
        protected SubMesh nodeSubMesh;

        protected Hashtable customParams = new Hashtable();

        #endregion

        #region Static member variables

        protected static Material material = null;
        protected static SubMesh subMesh = null;
        protected static long nextUnnamedNodeExtNum = 1;
        /// <summary>
        ///    Empty list of lights to return for IRenderable.Lights, since nodes are not lit.
        /// </summary>
        private LightList emptyLightList = new LightList();

        #endregion

        #region Constructors

        /// <summary>
        /// 
        /// </summary>
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

            childNodes = new NodeCollection();
            childrenToUpdate = new NodeCollection();

            NeedUpdate();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        public Node( string name )
        {
            this.name = name;

            // initialize objects
            orientation = initialOrientation = derivedOrientation = Quaternion.Identity;
            position = initialPosition = derivedPosition = Vector3.Zero;
            scale = initialScale = derivedScale = Vector3.UnitScale;
            cachedTransform = Matrix4.Identity;

            inheritsScale = true;

            accumAnimWeight = 0.0f;

            childNodes = new NodeCollection();
            childrenToUpdate = new NodeCollection();

            NeedUpdate();
        }


        #endregion

        #region Public methods
        public void RemoveFromParent()
        {
            if ( parent != null )
                parent.RemoveChild( name );//if this errors, then the parent is out of sync with the child
        }

        /// <summary>
        ///    Adds a node to the list of children of this node.
        /// </summary>
        /// <param name="node"></param>
        public void AddChild( Node child )
        {
            string childName = child.Name;
            if ( child == this )
                throw new ArgumentException( string.Format( "Node '{0}' cannot be added as a child of itself.", childName ) );
            if ( childNodes.ContainsKey( childName ) )
                throw new ArgumentException( string.Format( "Node '{0}' already has a child node with the name '{1}'.", this.name, childName ) );

            child.RemoveFromParent();
            childNodes.Add( childName, child );

            child.NotifyOfNewParent( this );
        }

        /// <summary>
        ///    Removes all child nodes from this node.
        /// </summary>
        public virtual void Clear()
        {
            childNodes.Clear();
        }


        /// <summary>
        ///		Removes all child Nodes attached to this node.
        /// </summary>
        public virtual void RemoveAllChildren()
        {
            Clear();
        }

        public bool HasChild( Node node )
        {
            return childNodes.ContainsValue( node );
        }

        public bool HasChild( string name )
        {
            return childNodes.ContainsKey( name );
        }

        /// <summary>
        ///    Gets a child node by index.
        /// </summary>
        /// <param name="index"></param>
        public Node GetChild( int index )
        {
            return childNodes[index];
        }

        /// <summary>
        ///    Gets a child node by name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Node GetChild( string name )
        {
            return childNodes[name];
        }

        /// <summary>
        ///    Removes the specifed node as a child of this node.
        /// </summary>
        /// <param name="child"></param>
        public virtual void RemoveChild( Node child )
        {
            int index = childNodes.IndexOfKey( child.Name );
            if ( index != -1 )
            {
                RemoveChild( child, index );
            }
        }


        /// <summary>
        ///     Removes the child node with the specified name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual Node RemoveChild( string name )
        {
            int index = childNodes.IndexOfKey( name );//getting the index prevent traversing 2x
            if ( index != -1 )
            {
                Node child = childNodes[index];
                RemoveChild( child, index );
                return child;
            }
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public virtual Node RemoveChild( int index )
        {
            if ( index < 0 || index >= childNodes.Count )
                throw new ArgumentOutOfRangeException( string.Format( "The index must be greater then or equal to 0 and less then {0}, the number of items.", childNodes.Count ) );
            Node child = childNodes[index];
            RemoveChild( child, index );
            return child;
        }


        protected virtual void RemoveChild( Node child, int index )
        {
            CancelUpdate( child );
            child.NotifyOfNewParent( null );
            childNodes.RemoveAt( index );
        }

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
        public virtual void Scale( Vector3 factor )
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
        public virtual void Translate( Vector3 translate )
        {
            Translate( translate, TransformSpace.Parent );
        }

        /// <summary>
        /// Moves the node along the cartesian axes.
        ///
        ///	This method moves the node by the supplied vector along the
        ///	world cartesian axes, i.e. along world x,y,z
        /// </summary>
        /// <param name="scale">Vector with x,y,z values representing the translation.</param>
        public virtual void Translate( Vector3 translate, TransformSpace relativeTo )
        {
            switch ( relativeTo )
            {
                case TransformSpace.Local:
                    // position is relative to parent so transform downwards
                    position += orientation * translate;
                    break;

                case TransformSpace.World:
                    if ( parent != null )
                    {
                        position += parent.DerivedOrientation.Inverse() * translate;
                    }
                    else
                    {
                        position += translate;
                    }

                    break;

                case TransformSpace.Parent:
                    position = position + translate;
                    break;
            }

            NeedUpdate();
        }

        /// <summary>
        /// Moves the node along arbitrary axes.
        /// </summary>
        /// <remarks>
        ///	This method translates the node by a vector which is relative to
        ///	a custom set of axes.
        ///	</remarks>
        /// <param name="axes">3x3 Matrix containg 3 column vectors each representing the
        ///	X, Y and Z axes respectively. In this format the standard cartesian axes would be expressed as:
        ///		1 0 0
        ///		0 1 0
        ///		0 0 1
        ///		i.e. The Identity matrix.
        ///	</param>
        /// <param name="move">Vector relative to the supplied axes.</param>
        public virtual void Translate( Matrix3 axes, Vector3 move )
        {
            Vector3 derived = axes * move;
            Translate( derived, TransformSpace.Parent );
        }

        /// <summary>
        /// Moves the node along arbitrary axes.
        /// </summary>
        /// <remarks>
        ///	This method translates the node by a vector which is relative to
        ///	a custom set of axes.
        ///	</remarks>
        /// <param name="axes">3x3 Matrix containg 3 column vectors each representing the
        ///	X, Y and Z axes respectively. In this format the standard cartesian axes would be expressed as:
        ///		1 0 0
        ///		0 1 0
        ///		0 0 1
        ///		i.e. The Identity matrix.
        ///	</param>
        /// <param name="move">Vector relative to the supplied axes.</param>
        public virtual void Translate( Matrix3 axes, Vector3 move, TransformSpace relativeTo )
        {
            Vector3 derived = axes * move;
            Translate( derived, relativeTo );
        }

        /// <summary>
        /// Rotate the node around the X-axis.
        /// </summary>
        /// <param name="degrees"></param>
        public virtual void Pitch( float degrees, TransformSpace relativeTo )
        {
            Rotate( Vector3.UnitX, degrees, relativeTo );
        }

        /// <summary>
        /// Rotate the node around the X-axis.
        /// </summary>
        /// <param name="degrees"></param>
        public virtual void Pitch( float degrees )
        {
            Rotate( Vector3.UnitX, degrees, TransformSpace.Local );
        }

        /// <summary>
        /// Rotate the node around the Z-axis.
        /// </summary>
        /// <param name="degrees"></param>
        public virtual void Roll( float degrees, TransformSpace relativeTo )
        {
            Rotate( Vector3.UnitZ, degrees, relativeTo );
        }

        /// <summary>
        /// Rotate the node around the Z-axis.
        /// </summary>
        /// <param name="degrees"></param>
        public virtual void Roll( float degrees )
        {
            Rotate( Vector3.UnitZ, degrees, TransformSpace.Local );
        }

        /// <summary>
        /// Rotate the node around the Y-axis.
        /// </summary>
        /// <param name="degrees"></param>
        public virtual void Yaw( float degrees, TransformSpace relativeTo )
        {
            Rotate( Vector3.UnitY, degrees, relativeTo );
        }

        /// <summary>
        /// Rotate the node around the Y-axis.
        /// </summary>
        /// <param name="degrees"></param>
        public virtual void Yaw( float degrees )
        {
            Rotate( Vector3.UnitY, degrees, TransformSpace.Local );
        }

        /// <summary>
        /// Rotate the node around an arbitrary axis.
        /// </summary>
        public virtual void Rotate( Vector3 axis, float degrees, TransformSpace relativeTo )
        {
            Quaternion q = Quaternion.FromAngleAxis( MathUtil.DegreesToRadians( degrees ), axis );
            Rotate( q, relativeTo );
        }

        /// <summary>
        /// Rotate the node around an arbitrary axis.
        /// </summary>
        public virtual void Rotate( Vector3 axis, float degrees )
        {
            Rotate( axis, degrees, TransformSpace.Local );
        }

        /// <summary>
        /// Rotate the node around an arbitrary axis using a Quaternion.
        /// </summary>
        public virtual void Rotate( Quaternion rotation, TransformSpace relativeTo )
        {
            switch ( relativeTo )
            {
                case TransformSpace.Parent:
                    // Rotations are normally relative to local axes, transform up
                    orientation = rotation * orientation;
                    break;

                case TransformSpace.World:
                    orientation = orientation * DerivedOrientation.Inverse() * rotation * DerivedOrientation;
                    break;

                case TransformSpace.Local:
                    // Note the order of the mult, i.e. q comes after
                    orientation = orientation * rotation;
                    break;
            }

            NeedUpdate();
        }

        /// <summary>
        /// Rotate the node around an arbitrary axis using a Quaternion.
        /// </summary>
        public virtual void Rotate( Quaternion rotation )
        {
            Rotate( rotation, TransformSpace.Local );
        }

        /// <summary>
        /// Resets the nodes orientation (local axes as world axes, no rotation).
        /// </summary>
        public virtual void ResetOrientation()
        {
            orientation = Quaternion.Identity;
            NeedUpdate();
        }

        /// <summary>
        /// Resets the position / orientation / scale of this node to it's initial state, see SetInitialState for more info.
        /// </summary>
        public virtual void ResetToInitialState()
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
        ///	If you never call this method, the initial state is the identity transform (do nothing) and a position of zero
        /// </remarks>
        public virtual void SetInitialState()
        {
            initialOrientation = orientation;
            initialPosition = position;
            initialScale = scale;
        }

        /// <summary>
        ///    Creates a new name child node.
        /// </summary>
        /// <param name="name"></param>
        public virtual Node CreateChild( string name )
        {
            return CreateChild( name, Vector3.Zero, Quaternion.Identity );
        }

        /// <summary>
        ///    Creates a new named child node.
        /// </summary>
        /// <param name="name">Name of the node.</param>
        /// <param name="translate">A vector to specify the position relative to the parent.</param>
        /// <returns></returns>
        public virtual Node CreateChild( string name, Vector3 translate )
        {
            return CreateChild( name, translate, Quaternion.Identity );
        }

        /// <summary>
        ///    Creates a new named child node.
        /// </summary>
        /// <param name="name">Name of the node.</param>
        /// <param name="translate">A vector to specify the position relative to the parent.</param>
        /// <param name="rotate">A quaternion to specify the orientation relative to the parent.</param>
        /// <returns></returns>
        public virtual Node CreateChild( string name, Vector3 translate, Quaternion rotate )
        {
            Node newChild = CreateChildImpl( name );
            newChild.Translate( translate );
            newChild.Rotate( rotate );
            AddChild( newChild );

            return newChild;
        }

        /// <summary>
        ///    Creates a new Child node.
        /// </summary>
        public virtual Node CreateChild()
        {
            return CreateChild( Vector3.Zero, Quaternion.Identity );
        }

        /// <summary>
        ///    Creates a new child node.
        /// </summary>
        /// <param name="translate">A vector to specify the position relative to the parent.</param>
        /// <returns></returns>
        public virtual Node CreateChild( Vector3 translate )
        {
            return CreateChild( translate, Quaternion.Identity );
        }

        /// <summary>
        ///    Creates a new child node.
        /// </summary>
        /// <param name="translate">A vector to specify the position relative to the parent.</param>
        /// <param name="rotate">A quaternion to specify the orientation relative to the parent.</param>
        /// <returns></returns>
        public virtual Node CreateChild( Vector3 translate, Quaternion rotate )
        {
            Node newChild = CreateChildImpl();
            newChild.Translate( translate );
            newChild.Rotate( rotate );
            AddChild( newChild );

            return newChild;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="camera"></param>
        /// <returns></returns>
        public float GetSquaredViewDepth( Camera camera )
        {
            Vector3 difference = this.DerivedPosition - camera.DerivedPosition;

            // return squared length to avoid doing a square root when it is not imperative
            return difference.LengthSquared;
        }

        /// <summary>
        ///    
        /// </summary>
        /// <param name="matrices"></param>
        public void GetWorldTransforms( Matrix4[] matrices )
        {
            matrices[0] = this.FullTransform;
        }

        /// <summary>
        ///		To be called in the event of transform changes to this node that require it's recalculation.
        /// </summary>
        /// <remarks>
        ///		This not only tags the node state as being 'dirty', it also requests it's parent to 
        ///		know about it's dirtiness so it will get an update next time.
        /// </remarks>
        public virtual void NeedUpdate()
        {
            needParentUpdate = true;
            needChildUpdate = true;
            needTransformUpdate = true;
            needRelativeTransformUpdate = true;

            // make sure we are not the root node
            if ( parent != null && !isParentNotified )
            {
                parent.RequestUpdate( this );
                isParentNotified = true;
            }

            // all children will be updated shortly
            childrenToUpdate.Clear();
        }


        /// <summary>
        ///		Called by children to notify their parent that they need an update.
        /// </summary>
        /// <param name="child"></param>
        public virtual void RequestUpdate( Node child )
        {
            // if we are already going to update everything, this wont matter
            if ( needChildUpdate )
                return;

            // add to the list of children that need updating
            if ( !childrenToUpdate.ContainsKey( child.name ) )
                childrenToUpdate.Add( child );

            // request to update me
            if ( parent != null && !isParentNotified )
            {
                parent.RequestUpdate( this );
                isParentNotified = true;
            }
        }

        /// <summary>
        ///		Called by children to notify their parent that they no longer need an update.
        /// </summary>
        /// <param name="child"></param>
        public virtual void CancelUpdate( Node child )
        {
            // remove this from the list of children to update
            childrenToUpdate.Remove( child );

            // propogate this changed if we are done
            if ( childrenToUpdate.Count == 0 && parent != null && !needChildUpdate )
            {
                parent.CancelUpdate( this );
                isParentNotified = false;
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///		Gets the number of children attached to this node.
        /// </summary>
        public int ChildCount
        {
            get
            {
                return childNodes.Count;
            }
        }

        /// <summary>
        /// Gets or sets the name of this Node object.
        /// </summary>
        /// <remarks>This is autogenerated initially, so setting it is optional.</remarks>
        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                if ( value == name )
                    return;
                string oldName = name;
                name = value;
                if ( parent != null )
                {
                    //ensure that it is keyed under this new name in its parent's collection
                    parent.RemoveChild( oldName );
                    parent.AddChild( this );
                }
                OnRename( oldName );

            }
        }

        /// <summary>
        /// Can be overriden in derived classes to fire an event or rekey this node in the collections which contain it
        /// </summary>
        /// <param name="oldName"></param>
        protected virtual void OnRename( string oldName )
        {
        }

        /// <summary>
        /// Get the Parent Node of the current Node.
        /// </summary>
        public virtual Node Parent
        {
            get
            {
                return parent;
            }
            set
            {
                if ( parent != value )
                {
                    if ( parent != null )
                        parent.RemoveChild( this );
                    if ( value != null )
                        value.AddChild( this );
                }
            }
        }

        protected virtual void NotifyOfNewParent( Node newParent )
        {
            parent = newParent;
            isParentNotified = false;
            NeedUpdate();
        }

        /// <summary>
        ///    A Quaternion representing the nodes orientation.
        /// </summary>
        public virtual Quaternion Orientation
        {
            get
            {
                return orientation;
            }
            set
            {
                orientation = value;
                NeedUpdate();
            }
        }

        /// <summary>
        /// The position of the node relative to its parent.
        /// </summary>
        public virtual Vector3 Position
        {
            get
            {
                return position;
            }
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
        public virtual Vector3 ScaleFactor
        {
            get
            {
                return scale;
            }
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
        public virtual bool InheritScale
        {
            get
            {
                return inheritsScale;
            }
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
        public virtual Matrix3 LocalAxes
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

                return new Matrix3( xAxis, yAxis, zAxis );
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
        // TODO This was previously protected.  Was made internal to allow access to custom collections.
        virtual internal void UpdateFromParent()
        {
            if ( parent != null )
            {
                // combine local orientation with parents
                Quaternion parentOrientation = parent.DerivedOrientation;
                derivedOrientation = parentOrientation * orientation;

                // change position vector based on parent's orientation
                derivedPosition = parentOrientation * position;

                // update scale
                if ( inheritsScale )
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

            needTransformUpdate = true;
            needRelativeTransformUpdate = true;
            if ( UpdatedFromParent != null )
                UpdatedFromParent( derivedPosition, derivedOrientation, derivedScale );
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
        protected void MakeTransform( Vector3 position, Vector3 scale, Quaternion orientation, ref Matrix4 destMatrix )
        {
            destMatrix = Matrix4.Identity;

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
            scale3x3.m00 = scale.x;
            scale3x3.m11 = scale.y;
            scale3x3.m22 = scale.z;

            destMatrix = rot3x3 * scale3x3;
            destMatrix.Translation = position;
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
        protected void MakeInverseTransform( Vector3 position, Vector3 scale, Quaternion orientation, ref Matrix4 destMatrix )
        {
            destMatrix = Matrix4.Identity;

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
            Matrix3 scale3x3 = Matrix3.Zero;

            scale3x3.m00 = invScale.x;
            scale3x3.m11 = invScale.y;
            scale3x3.m22 = invScale.z;

            // Set up final matrix with scale & rotation
            destMatrix = scale3x3 * rot3x3;

            destMatrix.Translation = invTranslate;
        }

        /// <summary>
        /// Must be overridden in subclasses.  Specifies how a Node is created.  CreateChild uses this to create a new one and add it
        /// to the list of child nodes.  This allows subclasses to not have to override CreateChild and duplicate all its functionality.
        /// </summary>
        protected abstract Node CreateChildImpl();

        /// <summary>
        /// Must be overridden in subclasses.  Specifies how a Node is created.  CreateChild uses this to create a new one and add it
        /// to the list of child nodes.  This allows subclasses to not have to override CreateChild and duplicate all its functionality.
        /// </summary>
        /// <param name="name">The name of the node to add.</param>
        protected abstract Node CreateChildImpl( string name );

        #endregion

        #region Internal engine properties

        /// <summary>
        /// Gets the orientation of the node as derived from all parents.
        /// </summary>
        public virtual Quaternion DerivedOrientation
        {
            get
            {
                if ( needParentUpdate )
                {
                    UpdateFromParent();
                    needParentUpdate = false;
                }

                return derivedOrientation;
            }
        }

        /// <summary>
        /// Gets the position of the node as derived from all parents.
        /// </summary>
        public virtual Vector3 DerivedPosition
        {
            get
            {
                if ( needParentUpdate )
                {
                    UpdateFromParent();
                    needParentUpdate = false;
                }

                return derivedPosition;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        Quaternion IRenderable.WorldOrientation
        {
            get
            {
                return this.DerivedOrientation;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        Vector3 IRenderable.WorldPosition
        {
            get
            {
                return this.DerivedPosition;
            }
        }

        /// <summary>
        /// Gets the scaling factor of the node as derived from all parents.
        /// </summary>
        public virtual Vector3 DerivedScale
        {
            get
            {
                return derivedScale;
            }
        }

        /// <summary>
        ///	Gets the full transformation matrix for this node.
        /// </summary>
        /// <remarks>
        /// This method returns the full transformation matrix
        /// for this node, including the effect of any parent node
        /// transformations, provided they have been updated using the Node.Update() method.
        /// This should only be called by a SceneManager which knows the
        /// derived transforms have been updated before calling this method.
        /// Applications using the engine should just use the relative transforms.
        /// </remarks>
        public virtual Matrix4 FullTransform
        {
            get
            {
                //if needs an update from parent or it has been updated from parent
                //yet this hasn't been called after that yet
                if ( needTransformUpdate )
                {
                    //derived properties may call Update() if needsParentUpdate is true and this will set needTransformUpdate to true
                    MakeTransform( this.DerivedPosition, this.DerivedScale, this.DerivedOrientation, ref cachedTransform );
                    //dont need to update this again until next invalidation
                    needTransformUpdate = false;
                }

                return cachedTransform;
            }
        }

        /// <summary>
        ///	Gets the full transformation matrix for this node.
        /// </summary>
        /// <remarks>
        /// This method returns the full transformation matrix
        /// for this node, including the effect of any parent node
        /// transformations, provided they have been updated using the Node.Update() method.
        /// This should only be called by a SceneManager which knows the
        /// derived transforms have been updated before calling this method.
        /// Applications using the engine should just use the relative transforms.
        /// </remarks>
        public virtual Matrix4 RelativeTransform
        {
            get
            {
                //if needs an update from parent or it has been updated from parent
                //yet this hasn't been called after that yet
                if ( needRelativeTransformUpdate )
                {
                    //derived properties may call Update() if needsParentUpdate is true and this will set needTransformUpdate to true
                    MakeTransform( this.Position, this.ScaleFactor, this.Orientation, ref cachedRelativeTransform );
                    //dont need to update this again until next invalidation
                    needRelativeTransformUpdate = false;
                }

                return cachedRelativeTransform;
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
        /// <param name="hasParentChanged">if true then this will update its derived properties (scale, orientation, position) accoarding to the parent's</param>
        protected internal virtual void Update( bool updateChildren, bool hasParentChanged )
        {
            isParentNotified = false;

            // skip update if not needed
            if ( !updateChildren && !needParentUpdate && !needChildUpdate && !hasParentChanged )
                return;

            // see if need to process everyone
            if ( needParentUpdate || hasParentChanged )
            {
                // update transforms from parent
                UpdateFromParent();
                needParentUpdate = false;
            }

            // see if we need to process all
            if ( needChildUpdate || hasParentChanged )
            {
                // update all children
                for ( int i = 0; i < childNodes.Count; i++ )
                {
                    Node child = childNodes[i];
                    child.Update( true, true );
                }

                childrenToUpdate.Clear();
            }
            else
            {
                // just update selected children
                for ( int i = 0; i < childrenToUpdate.Count; i++ )
                {
                    Node child = childrenToUpdate[i];
                    child.Update( true, false );
                }

                // clear the list
                childrenToUpdate.Clear();
            }

            // reset the flag
            needChildUpdate = false;
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
        internal virtual void WeightedTransform( float weight, Vector3 translate, Quaternion rotate, Vector3 scale )
        {
            WeightedTransform( weight, translate, rotate, scale, false );
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
        internal virtual void WeightedTransform( float weight, Vector3 translate, Quaternion rotate, Vector3 scale, bool lookInMovementDirection )
        {
            // If no previous transforms, we can just apply
            if ( accumAnimWeight == 0.0f )
            {
                rotationFromInitial = rotate;
                translationFromInitial = translate;
                scaleFromInitial = scale;
                accumAnimWeight = weight;
            }
            else
            {
                // Blend with existing
                float factor = weight / ( accumAnimWeight + weight );

                translationFromInitial += ( translate - translationFromInitial ) * factor;
                rotationFromInitial = Quaternion.Slerp( factor, rotationFromInitial, rotate );

                // For scale, find delta from 1.0, factor then add back before applying
                Vector3 scaleDiff = ( scale - Vector3.UnitScale ) * factor;
                scaleFromInitial = scaleFromInitial * ( scaleDiff + Vector3.UnitScale );
                accumAnimWeight += weight;

            }

            // Update final based on bind position + offsets
            orientation = initialOrientation * rotationFromInitial;
            position = initialPosition + translationFromInitial;
            scale = initialScale * scaleFromInitial;
            if ( lookInMovementDirection )
                orientation = -Vector3.UnitX.GetRotationTo( translate.ToNormalized() );


            NeedUpdate();
        }
        #endregion

        #region IRenderable implementation

        public bool CastsShadows
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        ///		This is only used if the SceneManager chooses to render the node. This option can be set
        ///		for SceneNodes at SceneManager.DisplaySceneNodes, and for entities based on skeletal 
        ///		models using Entity.DisplaySkeleton = true.
        ///	 </summary>
        public void GetRenderOperation( RenderOperation op )
        {
            if ( nodeSubMesh == null )
            {
                Mesh nodeMesh = MeshManager.Instance.Load( "axes.mesh" );
                nodeSubMesh = nodeMesh.GetSubMesh( 0 );
            }
            // return the render operation of the submesh itself
            nodeSubMesh.GetRenderOperation( op );
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
                if ( nodeMaterial == null )
                {
                    nodeMaterial = MaterialManager.Instance.GetByName( "Core/NodeMaterial" );

                    if ( nodeMaterial == null )
                    {
                        throw new Exception( "Could not find material 'Core/NodeMaterial'" );
                    }

                    // load, will ignore if already loaded
                    nodeMaterial.Load();
                }

                return nodeMaterial;
            }
        }

        public bool NormalizeNormals
        {
            get
            {
                return false;
            }
        }

        public Technique Technique
        {
            get
            {
                return this.Material.GetBestTechnique();
            }
        }


        /// <summary>
        /// 
        /// </summary>
        public ushort NumWorldTransforms
        {
            get
            {
                return 1;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool UseIdentityProjection
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool UseIdentityView
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public SceneDetailLevel RenderDetail
        {
            get
            {
                return SceneDetailLevel.Solid;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual LightList Lights
        {
            get
            {
                return emptyLightList;
            }
        }

        public Vector4 GetCustomParameter( int index )
        {
            if ( customParams[index] == null )
            {
                throw new Exception( "A parameter was not found at the given index" );
            }
            else
            {
                return (Vector4)customParams[index];
            }
        }

        public void SetCustomParameter( int index, Vector4 val )
        {
            customParams[index] = val;
        }

        public void UpdateCustomGpuParameter( GpuProgramParameters.AutoConstantEntry entry, GpuProgramParameters gpuParams )
        {
            if ( customParams[entry.data] != null )
            {
                gpuParams.SetConstant( entry.index, (Vector4)customParams[entry.data] );
            }
        }

        #endregion
    }

}
