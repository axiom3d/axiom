#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006 Axiom Project Team

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
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using DotNet3D.Math;
using DotNet3D.Math.Collections;

#endregion Namespace Declarations

namespace Axiom
{
    #region Delegates
    /// <summary>
    /// Signature for the Node.UpdatedFromParent event which provides the newly-updated derived properties for syncronization in a physics engine for instance
    /// </summary>
    public delegate void NodeUpdateHandler( Vector3 derivedPosition, Quaternion derivedOrientation, Vector3 derivedScale );

    #endregion
    
    #region Enums

    /// <summary>Enumeration denoting the spaces which a transform can be relative to.</summary>
    public enum TransformSpace
    {
        /// Transform is relative to the local space
        Local,
        /// Transform is relative to the space of the parent node
        Parent,
        /// Transform is relative to world space
        World
    }

    #endregion

    /// <summary>
    ///		Class representing a general-purpose node an articulated scene graph.
    /// </summary>
    /// <remarks>
    ///		A node in the scene graph is a node in a structured tree. A node contains
    ///		information about the transformation which will apply to
    ///		it and all of it's children. Child nodes can have transforms of their own, which
    ///		are combined with their parent's transformations.
    ///	    <para />
    ///		This is an abstract class - concrete classes are based on this for specific purposes,
    ///		e.g. SceneNode, Bone
    ///	</remarks>
    ///	<ogre headerVersion="1.44.2.3" sourceVersion="1.58.2.9" />
    /// <ogre name="Node">
    ///     <file name="OgreNode.h" revision="1.44.2.2" lastUpdated="6/5/2006" lastUpdatedBy="Lehuvyterz" />
    ///     <file name="OgreNode.cpp revision="1.58.2.7" lastUpdated="6/5/2006" lastUpdatedBy="Lehuvyterz" />
    public abstract class Node : IRenderable
    {
        #region Fields and Properties
        
        #region Static Fields
        
        protected static Material material = null;
        protected static SubMesh subMesh = null;
        
        /// <ogre name="msNextGeneratedNameExt" />
        protected static long nextUnnamedNodeExtNum = 1;
        
        #endregion Static Fields
        
        /// <summary>Collection of this nodes child nodes that need to be updated.</summary>
        /// <ogre name="mChildrenToUpdate" />
        private NodeCollection _childrenToUpdate;
        protected NodeCollection childrenToUpdate
        {
            get { return _childrenToUpdate; }
            set { _childrenToUpdate = value; }
        }
        
        /// <summary>Flag to indicate own transform from parent is out of date.</summary>
        /// <ogre name="mNeedParentUpdate" />
        private bool _needParentUpdate;
        protected bool needParentUpdate
        {
            get { return _needParentUpdate; }
            set { _needParentUpdate = value; }
        }
        
        /// <summary>Flag to indicate all children need to be updated.</summary>
        /// <ogre name="mNeedChildUpdate" />
        private bool _needChildUpdate;
        protected bool needChildUpdate
        {
            get { return _needChildUpdate; }
            set { _needChildUpdate = value; }
        }
        
        /// <summary>Flag indicating that parent has been notified about update request.</summary>
        /// <ogre name="mParentNotified" />
        private bool _isParentNotified;
        protected bool isParentNotified
        {
            get { return _isParentNotified; }
            set { _isParentNotified = value; }
        }
        
        
        /// <summary>The orientation to use as a base for keyframe animation</summary>
        /// <ogre name="mInitialOrientation" />
        private Quaternion _initialOrientation;
        /// <summary>Gets the initial orientation of this node, see InitialState for more info.</summary>
        /// <ogre name="getInitialOrientation" />
        public Quaternion initialOrientation
        {
            get { return _initialOrientation; }
            protected set { _initialOrientation = value; }
        }
        /// <summary>The total weighted rotation from the initial state so far</summary>
        /// <ogre name="mRotFromInitial" />
        private Quaternion _rotationFromInitial;
        protected Quaternion rotationFromInitial
        {
            get { return _rotationFromInitial; }
            set { _rotationFromInitial = value; }
        }
        
        
        /// <summary>The position to use as a base for keyframe animation</summary>
        /// <ogre name="mInitialPosition" />
        private Vector3 _initialPosition;
        /// <summary>Gets the initial position of this node, see InitialState for more info.</summary>
        /// <remarks>Also resets the cumulative animation weight used for blending</remarks>
        /// <ogre name="getInitialPosition" />
        public Vector3 initialPosition
        {
            get { return _initialPosition; }
            protected set { _initialPosition = value; }
        }

        /// <summary>The total weighted translation from the initial state so far</summary>
        /// <ogre name="mTransFromInitial" />
        private Vector3 _translationFromInitial;
        protected Vector3 translationFromInitial
        {
            get { return _translationFromInitial; }
            set { _translationFromInitial = value; }
        }
        
        
        /// <summary>The scale to use as a base for keyframe animation</summary>
        /// <ogre name="mInitialScale" />
        private Vector3 _initialScale;
        /// <summary>Gets the Initial position of this node, see InitialState for more info.
        /// <ogre name="getInitialScale" />
        public Vector3 initialScale
        {
            get { return _initialScale; }
            protected set { _initialScale = value; }
        }

        /// <summary>The total weighted scale from the initial state so far</summary>
        /// <ogre name="mScaleFromInitial" />
        private Vector3 _scaleFromInitial;
        protected Vector3 scaleFromInitial
        {
            get { return _scaleFromInitial; }
            set { _scaleFromInitial = value; }
        }
        
        //TODO naming
        /// <summary>Weight of applied animations so far, used for blending.</summary>
        /// <ogre name="mAccumAnimWeight" />
        private Real _accumAnimWeight;
        protected Real accumAnimWeight
        {
            get { return _accumAnimWeight; }
            set { _accumAnimWeight = value; }
        }

        #region Name Property

        /// <summary>Name of this node.</summary>
        /// <ogre name=nName" />
        private string _name;
        /// <summary>
        /// Gets or sets the name of this Node object.
        /// </summary>
        /// <remarks>This is autogenerated initially, so setting it is optional.</remarks>
        /// <ogre name="getname" />
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                if ( value == _name )
                    return;
                string oldName = _name;
                _name = value;
                if ( parent != null )
                {
                    //ensure that it is keyed under this new name in its parent's collection
                    parent.RemoveChild( oldName );
                    parent.AddChild( this );
                }
                OnRename( oldName );

            }
        }

        #endregion Name Property

        #region Parent Property

        /// <summary>Parent node (if any)</summary>
        /// <ogre name="mParent" />
        private Node _parent;
        /// <summary>
        /// Get the Parent Node of the current Node.
        /// </summary>
        /// <remarks>
        ///     Set only available internally - notification of parent.
        /// </remarks>
        /// <ogre name="getParent" />
        /// <ogre name="setParent" />
        public virtual Node Parent
        {
            get
            {
                return _parent;
            }
            internal protected set
            {
                if ( _parent != value )
                {
                    if ( _parent != null )
                        _parent.RemoveChild( this );
                    if ( value != null )
                    {
                        value.AddChild( this );
                        _parent = value;
                        isParentNotified = false;
                        NeedUpdate();
                    }
                }
            }
        }

        #endregion Parent Property

        #region Children Property
        //TODO is a childnode property necessary?
        /// <summary>Collection of this nodes child nodes.</summary>
        /// <ogre name="mChildren" />
        private NodeCollection _childNodes;
        protected NodeCollection childNodes
        {
            get { return _childNodes; }
            set { _childNodes = value; }
        }
        /// <summary>Enumerator over this nodes child nodes.</summary>
        /// <ogre name="getChildIterator" />
        public IEnumerator<NodeCollection> Children
        {
            get
            {
                return (IEnumerator<NodeCollection>)_childNodes.GetEnumerator();
            }
        }
        
        /// <summary>Reports the number of child nodes under this one</summary>
        /// <ogre name="numChildren" />
        public int ChildCount
        {
            get
            {
                return _childNodes.Count;
            }
        }

        #endregion Children Property

        #region Orientation Property

        /// <summary>Orientation of this node relative to it's parent.</summary>
        /// <ogre name="mOrientation" />
        private Quaternion _orientation;
        
        /// <summary>
        ///    A Quaternion representing the nodes orientation.
        /// </summary>
        /// <ogre name="getOrientation" />
        /// <ogre name="setOrientation" />
        public virtual Quaternion Orientation
        {
            get
            {
                return _orientation;
            }
            set
            {
                _orientation = value;
                NeedUpdate();
            }
        }
        
        #endregion Orientation Property

        #region Position Property

        /// <summary>Position of this node relative to it's parent.</summary>
        /// <ogre name="mPosition" />
        private Vector3 _position;
        
        /// <summary>
        /// The position of the node relative to its parent.
        /// </summary>
        /// <ogre name="getPosition" />
        /// <ogre name="setPosition" />
        public virtual Vector3 Position
        {
            get
            {
                return _position;
            }
            set
            {
                _position = value;
                NeedUpdate();
            }
        }
        
        #endregion Position Property

        #region ScalingFactor Property

        /// <summary>The scaling factor applied to this node.</summary>
        /// <ogre name="mScale" />
        protected Vector3 _scale;
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
        /// <ogre name="getScale" />
        /// <ogre name="setScale" />
        public virtual Vector3 ScalingFactor
        {
            get
            {
                return _scale;
            }
            set
            {
                _scale = value;
                NeedUpdate();
            }
        }
        
        #endregion ScalingFactor Property

        #region InheritScale Property

        /// <summary>Tells the node whether it should inherit scaling factors from it's parent node.</summary>
        /// <ogre name="mInheritScale" />
        protected bool _inheritsScale;
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
        /// </remarks>
        /// <ogre name="getInheritScale" />
        /// <ogre name="setInheirtScale" />
        public virtual bool InheritScale
        {
            get
            {
                return _inheritsScale;
            }
            set
            {
                _inheritsScale = value;
                NeedUpdate();
            }
        }

        #endregion InheritScale Property

        #region LocalAxes Property

        /// <summary>
        /// Gets a matrix whose columns are the local axes based on
        /// the nodes orientation relative to it's parent.
        /// </summary>
        /// <ogre name="getLocalAxes" />
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

        #endregion LocalAxes Property

        #region DerivedOrientation Property

        /// <summary>World orientation of this node based on parents orientation.</summary>
        /// <remarks>
        ///     This member is the position derived by combining the
        ///     local transformations and thos of it's parents.
        ///     This is updated when UpdateFromParent is called by the
        ///     SceneManager of the nodes parent.
        /// </remarks>
        /// <ogre name="mDerivedOrientation" />
        private Quaternion _derivedOrientation;
        /// <summary>
        /// Gets the orientation of the node as derived from all parents.
        /// </summary>
        /// <ogre name=_getDerivedOrientation" />
        public virtual Quaternion DerivedOrientation
        {
            get
            {
                if ( needParentUpdate )
                {
                    UpdateFromParent();
                    _needParentUpdate = false;
                }

                return _derivedOrientation;
            }
        }

        #endregion DerivedOrientation Property

        #region DerivedPosition Property

        /// <summary>Gets the position of the node as derived from all parents.</summary>
        /// <remarks>
        ///     This memeber is the position derived by combining the
        ///     local transformations and thos of it's parents.
        ///     This is updated when UpdateFromParent is called by the
        ///     SceneManger or the nodes parent.
        /// </remarks>
        /// <ogre name="mDerivedPosition" />
        private Vector3 _derivedPosition;
        /// <summary>Gets the position of the node as derived from all parents.</summary>
        /// <ogre name="_getDerivedPosition" />
        public virtual Vector3 DerivedPosition
        {
            get
            {
                if ( _needParentUpdate )
                {
                    UpdateFromParent();
                    _needParentUpdate = false;
                }

                return _derivedPosition;
            }
        }

        #endregion DerivedPosition Property

        #region DerivedScale Property

        /// <summary>Gets the scaling factor of the node as derived from all parents.</summary>
        /// <remarks>
        ///     This member is the position derived by combining the
        ///     local transformations and those of it's parents.
        ///     This is updated when UpdateFromParent is called by the
        ///     SceneManager or the nodes parent.
        /// </remarks>
        /// <ogre name="mDerivedScale" />
        private Vector3 _derivedScale;
        /// <summary>
        /// Gets the scaling factor of the node as derived from all parents.
        /// </summary>
        /// <ogre name="_getDerivedScale" />
        public virtual Vector3 DerivedScale
        {
            get
            {
                if ( _needParentUpdate )
                {
                    UpdateFromParent();
                    _needParentUpdate = false;
                }
                return _derivedScale;
            }
        }

        #endregion DerivedScale Property

        #region FullTransform Property
        /// <summary>if needs an update from parent or it has been updated from parent.</summary>
        /// <ogre name="mCachedTransformOutOfDate" />
        private bool _needTransformUpdate;
        protected bool needTransformUpdate
        {
            get { return _needTransformUpdate; }
            set { _needTransformUpdate = value; }
        }

        /// <summary>Cached derived transform as a 4x4 matrix.</summary>
        /// <ogre name="mCachedTransform" />
        private Matrix4 _cachedTransform;
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
        /// <ogre name="_getFullTransform" />
        public virtual Matrix4 FullTransform
        {
            get
            {
                //if needs an update from parent or it has been updated from parent
                //yet this hasn't been called after that yet
                if ( _needTransformUpdate )
                {
                    //derived properties may call Update() if needsParentUpdate is true and this will set needTransformUpdate to true
                    MakeTransform( this.DerivedPosition, this.DerivedScale, this.DerivedOrientation, ref _cachedTransform );
                    //dont need to update this again until next invalidation
                    _needTransformUpdate = false;
                }

                return _cachedTransform;
            }
        }

        #endregion FullTransform Property

        #region RelativeTransform Property
        //TODO not in ogre

        /// <summary>needs an update from parent or it has been updated from parent</summary>
        private bool _needRelativeTransformUpdate;
        /// <summary>Cached relative transform as a 4x4 matrix.</summary>
        private Matrix4 _cachedRelativeTransform;
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
                if ( _needRelativeTransformUpdate )
                {
                    //derived properties may call Update() if needsParentUpdate is true and this will set needTransformUpdate to true
                    MakeTransform( this.Position, this.ScalingFactor, this.Orientation, ref cachedRelativeTransform );
                    //dont need to update this again until next invalidation
                    needRelativeTransformUpdate = false;
                }

                return _cachedRelativeTransform;
            }
        }

        #endregion RelativeTransform Property
			
        #endregion Fields and Properties

        #region Constructors and Destructor

        /// <summary>
        ///     Default Constructor
        /// </summary>
        /// <ogre name="Node" />
        public Node()
        {
            this.name = "Unnamed_" + nextUnnamedNodeExtNum++;

            _parent = null;

            // initialize objects
            _orientation = initialOrientation = _derivedOrientation = Quaternion.Identity;
            _position = initialPosition = _derivedPosition = Vector3.Zero;
            _scale = initialScale = _derivedScale = Vector3.UnitScale;
            _cachedTransform = Matrix4.Identity;

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
        /// <ogre name="Node" />
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

        ~Node()
        {
            Clear();
            if ( parent != null )
                RemoveFromParent();
        }

        #endregion

        #region Events
        /// <summary>
        /// Event which provides the newly-updated derived properties for syncronization in a physics engine for instance
        /// </summary>
        public event NodeUpdateHandler UpdatedFromParent;
        #endregion

        #region Protected methods

        /// <summary>
        ///	Triggers the node to update it's combined transforms.
        ///
        ///	This method is called internally by the engine to ask the node
        ///	to update it's complete transformation based on it's parents
        ///	derived transform.
        /// </summary>
        /// <ogre name="_updateFromParent" />
        virtual protected void UpdateFromParent()
        {
            if ( parent != null )
            {
                // combine local orientation with parents
                Quaternion parentOrientation = _parent.DerivedOrientation;
                _derivedOrientation = _parentOrientation * _orientation;

                // change position vector based on parent's orientation & scale
                _derivedPosition = _parentOrientation * ( _position * _parent.DerivedScale );

                // update scale
                if ( _inheritsScale )
                {
                    // set out own position by parent scale
                    Vector3 parentScale = _parent.DerivedScale;

                    // set own scale, just combine as equivalent axes, no shearing
                    _derivedScale = _scale * parentScale;
                }
                else
                {
                    // do not inherit parents scale
                    _derivedScale = _scale;
                }

                // add parents positition to local altered position
                _derivedPosition += _parent.DerivedPosition;
            }
            else
            {
                // Root node, no parent
                _derivedOrientation = _orientation;
                _derivedPosition = _position;
                _derivedScale = _scale;
            }

            _needTransformUpdate = true;
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
        /// <ogre name="makeTransform" />
        internal protected void MakeTransform( Vector3 position, Vector3 scale, Quaternion orientation, ref Matrix4 destMatrix )
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
        /// <ogre name="makeInverseTransform" />
        internal protected void MakeInverseTransform( Vector3 position, Vector3 scale, Quaternion orientation, ref Matrix4 destMatrix )
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
        /// <ogre name="createChildImpl" />
        internal protected abstract Node CreateChildImpl();

        /// <summary>
        /// Must be overridden in subclasses.  Specifies how a Node is created.  CreateChild uses this to create a new one and add it
        /// to the list of child nodes.  This allows subclasses to not have to override CreateChild and duplicate all its functionality.
        /// </summary>
        /// <param name="name">The name of the node to add.</param>
        /// <ogre name="createChildImpl" />
        internal protected abstract Node CreateChildImpl( string name );

        /// <summary>
        /// Can be overriden in derived classes to fire an event or rekey this node in the collections which contain it
        /// </summary>
        /// <param name="oldName"></param>
        protected virtual void OnRename( string oldName )
        {
        }

        #endregion

        #region Public methods
        /// <summary>
        /// Removes the node from it's parent node collection.
        /// </summary>
        public void RemoveFromParent()
        {
            if ( parent != null )
                parent.RemoveChild( name );//if this errors, then the parent is out of sync with the child
        }

        /// <summary>
        ///    Adds a node to the list of children of this node. If it is attached to another node, it must be detached first.
        /// </summary>
        /// <param name="child"></param>
        /// <ogre name="addChild" />
        public void AddChild( Node child )
        {
            Debug.Assert( child.Parent == null );

            string childName = child.Name;
            if ( child == this )
                throw new ArgumentException( string.Format( "Node '{0}' cannot be added as a child of itself.", childName ) );
            if ( childNodes.ContainsKey( childName ) )
                throw new ArgumentException( string.Format( "Node '{0}' already has a child node with the name '{1}'.", this.name, childName ) );

            childNodes.Add( childName, child );

            child.Parent = this;
        }
        
        /// <summary>
        /// Removes all child nodes from this node.
        /// </summary>
        /// <ogre name="removeAllChildren" />
        public virtual void Clear()
        {
            for ( int i = 0; i < childNodes.Count; i++ )
            {
                childNodes[ 0 ].Parent = null;
            }

            childNodes.Clear();
            childrenToUpdate.Clear();
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
        /// <ogre name="getChild" />
        public Node GetChild( int index )
        {
            return childNodes[index];
        }

        /// <summary>
        ///    Gets a child node by name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <ogre name="getChild" />
        public Node GetChild( string name )
        {
            return childNodes[name];
        }

        /// <summary>
        ///    Removes the specifed node as a child of this node.
        /// </summary>
        /// <remarks>
        ///     Deos not delete the node, just detaches it from
        ///     this parent, potentially to be reattached elsewhere.
        ///     There is also an alternative version which drops a named
        ///     child from this node.
        /// </remarks>
        /// <param name="child"></param>
        /// <ogre name="removeChild" />
        public virtual Node RemoveChild( Node child )
        {
            int index = childNodes.IndexOfKey( child.Name );
            if ( index != -1 )
            {
                RemoveChild( child, index );
            }
            return child;
        }

        /// <summary>
        ///     Removes the child node with the specified name.
        /// </summary>
        /// <remarks>
        ///     Does not delete the node, just detaches it from
        ///     this parent, potentially to be reattached elsewhere.
        /// </remarks>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <ogre name="removeChild" />
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
        ///     Drops the specified child from this node.
        /// </summary>
        /// <remarks>
        ///     Does not delete the node, just detaches it from
        ///     this parent, potentially to be reattached elsewhere.
        ///     There is also an alternative version which drops a named
        ///     child from this node.
        /// </remarks>
        /// <ogre name="removeChild" />
        /// <param name="index"></param>
        /// <returns></returns>
        /// <ogre name="removeChild" />
        public virtual Node RemoveChild( int index )
        {
            if ( index < 0 || index >= childNodes.Count )
                throw new ArgumentOutOfRangeException( string.Format( "The index must be greater then or equal to 0 and less then {0}, the number of items.", childNodes.Count ) );
            Node child = childNodes[index];
            RemoveChild( child, index );
            return child;
        }
        
        /// <summary>Helper method for RemoveChild</summary>
        protected virtual void RemoveChild( Node child, int index )
        {
            CancelUpdate( child );
            child.NotifyOfNewParent( null );
            _childNodes.RemoveAt( index );
        }

        /// <summary>
        ///     Scales the node, combining it's current scale with the passed in scaling factor. 
        /// </summary>
        /// <remarks>
        ///	    This method applies an extra scaling factor to the node's existing scale, (unlike setScale
        ///	    which overwrites it) combining it's current scale with the new one. E.g. calling this 
        ///	    method twice with Vector3(2,2,2) would have the same effect as setScale(Vector3(4,4,4)) if
        ///     the existing scale was 1.
        ///     <para />
        ///	    Note that like rotations, scalings are oriented around the node's origin.
        /// </remarks>
        /// <param name="factor"></param>
        /// <ogre name="scale" />
        public virtual void Scale( Vector3 factor )
        {
            _scale = _scale * factor;
            NeedUpdate();
        }
        
        /// <summary>
        ///     Scales the node, combining it's current scale with the passed in scaling factor.
        /// </summary>
        /// <remarks>
        ///     This method applies an extra scaling factor to the node's existing scale, (unlike setScale
        ///     which overwrites it) combining it's current scale with the new one. E.g. calling this
        ///     method twice with Vector3(2,2,2) would have the same affect as SetScale(Vector3(4,4,4)) if
        ///     the exisiting scale was 1.
        ///     <para />
        ///     Note that like rotations, scalings are oriented around the node's origin.
        /// </remarks>
        /// <ogre name="scale" />
        public virtual void Scale( Real x, Real y, Real z )
        {
            _scale.x *= x;
            _scale.y *= y;
            _scale.z *= z;
            NeedUpdate();
        }
        
        /// <summary>
        ///     Moves the node along the cartesian axes.
        ///     <para />
        ///	    This method moves the node by the supplied vector along the
        ///	    world cartesian axes, i.e. along world x,y,z
        /// </summary>
        /// <param name="translate">Vector with x,y,z values representing the translation.</param>
        /// <ogre name="translate" />
        public virtual void Translate( Vector3 translate )
        {
            Translate( translate, TransformSpace.Parent );
        }

        /// <summary>
        ///     Moves the node along the cartesian axes.
        ///     <para />
        ///	    This method moves the node by the supplied vector along the
        ///	    world cartesian axes, i.e. along world x,y,z
        /// </summary>
        /// <param name="translate">Vector with x,y,z values representing the translation.</param>
        /// <ogre name="translate" />
        public virtual void Translate( Vector3 translate, TransformSpace relativeTo )
        {
            switch ( relativeTo )
            {
                case TransformSpace.Local:
                    // position is relative to parent so transform downwards
                    _position += _orientation * translate;
                    break;

                case TransformSpace.World:
                    if ( _parent != null )
                    {
                        _position += parent.DerivedOrientation.Inverse() * translate;
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
        ///     Moves the node along the cartesian axes.
        ///     <para />
        ///	    This method moves the node by the supplied vector along the
        ///	    world cartesian axes, i.e. along world x,y,z
        /// </summary>
        /// <ogre name="translate" />
        public virtual void Translate( Real x, Real y, Real z )
        {
            Translate( x, y, z, TransformSpace.Parent );
        }
        
        /// <summary>
        ///     Moves the node along the cartesian axes.
        ///     <para />
        ///	    This method moves the node by the supplied vector along the
        ///	    world cartesian axes, i.e. along world x,y,z
        /// </summary>
        /// <ogre name="translate" />
        public virtual void Translate( Real x, Real y, Real z, TransformSpace relativeTo )
        {
            Vector3 v = new Vector3( x, y, z );
            Translate( v, relativeTo );
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
        /// <ogre name="translate" />
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
        /// <ogre name="translate" />
        public virtual void Translate( Matrix3 axes, Vector3 move, TransformSpace relativeTo )
        {
            Vector3 derived = axes * move;
            Translate( derived, relativeTo );
        }
        
        /// <ogre name="translate" />
        public virtual void Translate( Matrix3 axes, Real x, Real y, Real z, TransformSpace relativeTo )
        {
            Vector3 derived = new Vector3( x, y, z );
            Translate( axes, derived, relateveTo );
        }
        
        /// <summary>
        /// Rotate the node around the X-axis.
        /// </summary>
        /// <param name="degrees"></param>
        /// <ogre name="pitch" />
        public virtual void Pitch( Real degrees, TransformSpace relativeTo )
        {
            Rotate( Vector3.UnitX, degrees, relativeTo );
        }

        /// <summary>
        /// Rotate the node around the X-axis.
        /// </summary>
        /// <param name="degrees"></param>
        /// <ogre name="Pitch" />
        public virtual void Pitch( Real degrees )
        {
            Rotate( Vector3.UnitX, degrees, TransformSpace.Local );
        }

        /// <summary>
        /// Rotate the node around the Z-axis.
        /// </summary>
        /// <param name="degrees"></param>
        /// <ogre name="roll" />
        public virtual void Roll( Real degrees, TransformSpace relativeTo )
        {
            Rotate( Vector3.UnitZ, degrees, relativeTo );
        }

        /// <summary>
        /// Rotate the node around the Z-axis.
        /// </summary>
        /// <param name="degrees"></param>
        /// <ogre name="roll" />
        public virtual void Roll( Real degrees )
        {
            Rotate( Vector3.UnitZ, degrees, TransformSpace.Local );
        }

        /// <summary>
        /// Rotate the node around the Y-axis.
        /// </summary>
        /// <param name="degrees"></param>
        /// <ogre name="yaw" />
        public virtual void Yaw( Real degrees, TransformSpace relativeTo )
        {
            Rotate( Vector3.UnitY, degrees, relativeTo );
        }

        /// <summary>
        /// Rotate the node around the Y-axis.
        /// </summary>
        /// <param name="degrees"></param>
        /// <ogre name="yaw" />
        public virtual void Yaw( Real degrees )
        {
            Rotate( Vector3.UnitY, degrees, TransformSpace.Local );
        }
        
        /// <summary>
        /// Rotate the node around an arbitrary axis.
        /// </summary>
        /// <ogre name="rotate" />
        public virtual void Rotate( Vector3 axis, Real degrees, TransformSpace relativeTo )
        {
            Quaternion q = Quaternion.FromAngleAxis( new Degree( (Real) degrees), axis );
            Rotate( q, relativeTo );
        }

        /// <summary>
        /// Rotate the node around an arbitrary axis.
        /// </summary>
        /// <ogre name="rotate" />
        public virtual void Rotate( Vector3 axis, Real degrees )
        {
            Rotate( axis, degrees, TransformSpace.Local );
        }

        /// <summary>
        /// Rotate the node around an arbitrary axis using a Quaternion.
        /// </summary>
        /// <ogre name="rotate" />
        public virtual void Rotate( Quaternion rotation, TransformSpace relativeTo )
        {
            switch ( relativeTo )
            {
                case TransformSpace.Parent:
                    // Rotations are normally relative to local axes, transform up
                    orientation = rotation * _orientation;
                    break;

                case TransformSpace.World:
                    orientation = _orientation * DerivedOrientation.Inverse() * rotation * DerivedOrientation;
                    break;

                case TransformSpace.Local:
                    // Note the order of the mult, i.e. rotation comes after
                    _orientation = _orientation * rotation;
                    break;
            }

            NeedUpdate();
        }

        /// <summary>
        /// Rotate the node around an arbitrary axis using a Quaternion.
        /// </summary>
        /// <ogre name="rotate" />
        public virtual void Rotate( Quaternion rotation )
        {
            Rotate( rotation, TransformSpace.Local );
        }

        /// <summary>
        /// Resets the nodes orientation (local axes as world axes, no rotation).
        /// </summary>
        /// <ogre name="resetOrientation" />
        public virtual void ResetOrientation()
        {
            orientation = Quaternion.Identity;
            NeedUpdate();
        }

        /// <summary>
        /// Resets the position / orientation / scale of this node to it's initial state, see SetInitialState for more info.
        /// </summary>
        /// <ogre name="resetToInitialState" />
        public virtual void ResetToInitialState()
        {
            _position = _initialPosition;
            _orientation = _initialOrientation;
            _scale = _initialScale;

            // Reset weights
            _accumAnimWeight = 0.0f;
            _translationFromInitial = Vector3.Zero;
            _rotationFromInitial = Quaternion.Identity;
            _scaleFromInitial = Vector3.UnitScale;

            NeedUpdate();
        }

        /// <summary>
        ///     Sets the current transform of this node to be the 'initial state' ie that
        ///	    position / orientation / scale to be used as a basis for delta values used
        ///     in keyframe animation.
        /// </summary>
        /// <remarks>
        ///	    You never need to call this method unless you plan to animate this node. If you do
        ///	    plan to animate it, call this method once you've loaded the node with it's base state,
        ///	    ie the state on which all keyframes are based.
        ///     <para />
        ///	    If you never call this method, the initial state is the identity transform (do nothing) and a position of zero
        /// </remarks>
        /// <ogre name="setInitialState" />
        public virtual void SetInitialState()
        {
            _initialOrientation = _orientation;
            _initialPosition = _position;
            _initialScale = _scale;
        }

        /// <summary>
        ///    Creates a new name child node.
        /// </summary>
        /// <param name="name"></param>
        /// <ogre name="createChild" />
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
        /// <ogre name="createChild" />
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
        /// <ogre name="createChild" />
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
        /// <ogre name="createChild" />
        public virtual Node CreateChild()
        {
            return CreateChild( Vector3.Zero, Quaternion.Identity );
        }

        /// <summary>
        ///    Creates a new child node.
        /// </summary>
        /// <param name="translate">A vector to specify the position relative to the parent.</param>
        /// <returns></returns>
        /// <ogre name="createChild" />
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
        /// <ogre name="createChild" />
        public virtual Node CreateChild( Vector3 translate, Quaternion rotate )
        {
            Node newChild = CreateChildImpl();
            newChild.Translate( translate );
            newChild.Rotate( rotate );
            AddChild( newChild );

            return newChild;
        }

        /// <summary>
        ///		To be called in the event of transform changes to this node that require it's recalculation.
        /// </summary>
        /// <remarks>
        ///		This not only tags the node state as being 'dirty', it also requests it's parent to 
        ///		know about it's dirtiness so it will get an update next time.
        /// </remarks>
        /// <ogre name="needUpdate" />
        public virtual void NeedUpdate()
        {
            _needParentUpdate = true;
            _needChildUpdate = true;
            _needTransformUpdate = true;
            
            _needRelativeTransformUpdate = true;

            // make sure we are not the root node and parent hasn't been notified before
            if ( _parent != null && !_isParentNotified )
            {
                _parent.RequestUpdate( this );
                _isParentNotified = true;
            }

            // all children will be updated shortly
            _childrenToUpdate.Clear();
        }


        /// <summary>
        ///		Called by children to notify their parent that they need an update.
        /// </summary>
        /// <param name="child"></param>
        /// <ogre name="requestUpdate" />
        public virtual void RequestUpdate( Node child )
        {
            // if we are already going to update everything, this wont matter
            if ( _needChildUpdate )
                return;

            // add to the list of children that need updating
            if ( !_childrenToUpdate.ContainsKey( child.name ) )
                _childrenToUpdate.Add( child );

            // request to update me
            if ( _parent != null && !_isParentNotified )
            {
                _parent.RequestUpdate( this );
                _isParentNotified = true;
            }
        }

        /// <summary>
        ///		Called by children to notify their parent that they no longer need an update.
        /// </summary>
        /// <param name="child"></param>
        /// <ogre name="cancelUpdate" />
        public virtual void CancelUpdate( Node child )
        {
            // remove this from the list of children to update
            _childrenToUpdate.Remove( child );

            // propogate this changed if we are done
            if ( _childrenToUpdate.Count == 0 && _parent != null && !_needChildUpdate )
            {
                _parent.CancelUpdate( this );
                _isParentNotified = false;
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
        /// <ogre name="-update" />
        protected internal virtual void Update( bool updateChildren, bool hasParentChanged )
        {
            // always clear information about parent notification
            isParentNotified = false;

            // skip update if not needed
            if ( !updateChildren && !needParentUpdate && !needChildUpdate && !hasParentChanged )
                return;

            // see if need to process everyone
            if ( _needParentUpdate || hasParentChanged )
            {
                // update transforms from parent
                UpdateFromParent();
                _needParentUpdate = false;
            }

            // see if we need to process all
            if ( _needChildUpdate || hasParentChanged )
            {
                // update all children
                /*
                for ( int i = 0; i < childNodes.Count; i++ )
                {
                    Node child = childNodes[i];
                    child.Update( true, true );
                }
                */
                foreach ( Node child in _childNodes )
                {
                    child.Update( true, true );
                }
                childrenToUpdate.Clear();
            }
            else
            {
                // just update selected children
                /*
                for ( int i = 0; i < childrenToUpdate.Count; i++ )
                {
                    Node child = childrenToUpdate[i];
                    child.Update( true, false );
                }
                */
                foreach ( Node child in _childrenToUpdate )
                {
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
        /// <ogre name="_weightedTransform" />
        internal virtual void WeightedTransform( Real weight, Vector3 translate, Quaternion rotate, Vector3 scale )
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
        internal virtual void WeightedTransform( Real weight, Vector3 translate, Quaternion rotate, Vector3 scale, bool lookInMovementDirection )
        {
            // If no previous transforms, we can just apply
            if ( _accumAnimWeight == 0.0f )
            {
                _rotationFromInitial = rotate;
                _translationFromInitial = translate;
                _scaleFromInitial = scale;
                _accumAnimWeight = weight;
            }
            else
            {
                // Blend with existing
                Real factor = weight / ( _accumAnimWeight + weight );

                _translationFromInitial += ( translate - _translationFromInitial ) * factor;
                _rotationFromInitial = Quaternion.Slerp( factor, _rotationFromInitial, rotate );

                // For scale, find delta from 1.0, factor then add back before applying
                Vector3 scaleDiff = ( scale - Vector3.UnitScale ) * factor;
                _scaleFromInitial = _scaleFromInitial * ( scaleDiff + Vector3.UnitScale );
                _accumAnimWeight += weight;

            }

            // Update final based on bind position + offsets
            _orientation = _initialOrientation * _rotationFromInitial;
            _position = _initialPosition + _translationFromInitial;
            _scale = _initialScale * _scaleFromInitial;
            
            if ( lookInMovementDirection )
                orientation = -Vector3.UnitX.GetRotationTo( translate.ToNormalized() );


            NeedUpdate();
        }
        #endregion

        #region IRenderable Implementation

        #region Fields and Properties
        
        /// <summary>SubMesh to be used is this node itself will be rendered (axes, or bones).</summary>
        private SubMesh _nodeSubMesh;
        protected SubMesh nodeSubMesh
        {
            get { return _nodeSubMesh; }
            set { _nodeSubMesh = value; }
        }
        
        private Dictionary<int, Vector4> _customParameters = new Dictionary<int, Vector4>();
        protected Hashtable customParameters
        {
            get { return _customParameters; }
            set { _customParameters = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <ogre name="getWorldOrientation" />
        public Quaternion WorldOrientation
        {
            get
            {
                return this.DerivedOrientation;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <ogre name="getWorldPosition" />
        public Vector3 WorldPosition
        {
            get
            {
                return this.DerivedPosition;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool CastsShadows
        {
            get
            {
                return false;
            }
        }

        /// <summary>Material to be used is this node itself will be rendered (axes, or bones).</summary>
        private Material _nodeMaterial;
        /// <summary>
        ///		
        /// </summary>
        /// <remarks>
        ///		This is only used if the SceneManager chooses to render the node. This option can be set
        ///		for SceneNodes at SceneManager.DisplaySceneNodes, and for entities based on skeletal 
        ///		models using Entity.DisplaySkeleton = true.
        /// </remarks>
        /// <ogre name="getmaterial" />
        public Material Material
        {
            get
            {
                if ( _nodeMaterial == null )
                {
                    _nodeMaterial = MaterialManager.Instance.GetByName( "Core/NodeMaterial" );

                    if ( _nodeMaterial == null )
                    {
                        throw new Exception( "Could not find material 'Core/NodeMaterial'" );
                    }

                    // load, will ignore if already loaded
                    _nodeMaterial.Load();
                }

                return nodeMaterial;
            }

            protected set
            {
                _nodeMaterial = value;
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
        
        /// <summary>Empty list of Planes to return for IRenderable.ClipPlanes.</summary>
        private PlaneList _dummyPlaneList = new PlaneList();
        public PlaneList ClipPlanes
        {
            get
            {
                return _dummyPlaneList;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public int WorldTransformCount
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

        /// <summary> </summary>
        private bool _renderDetailOverridable = true;
        /// <summary>
        /// 
        /// </summary>
        public bool RenderDetailOverrideable
        {
            get
            {
                return _renderDetailOverridable;
            }
            set
            {
                _renderDetailOverridable = value;
            }
        }

        /// <summary>Empty list of lights to return for IRenderable.Lights, since nodes are not lit.</summary>
        private LightList _emptyLightList = new LightList();  
        /// <summary>
        /// 
        /// </summary>
        /// <ogre name="getLights" />
        public virtual LightList Lights
        {
            get
            {
                return _emptyLightList;
            }
        }

        #endregion Fields and Properties

        #region Methods

        /// <summary>
        ///		This is only used if the SceneManager chooses to render the node. This option can be set
        ///		for SceneNodes at SceneManager.DisplaySceneNodes, and for entities based on skeletal 
        ///		models using Entity.DisplaySkeleton = true.
        ///	 </summary>
        /// <ogre name="getRenderOperation" />
        public void GetRenderOperation( RenderOperation op )
        {
            if ( _nodeSubMesh == null )
            {
                Mesh nodeMesh = MeshManager.Instance.Load( "axes.mesh", ResourceGroupManager.BootstrapResourceGroupName);
                _nodeSubMesh = nodeMesh.GetSubMesh( 0 );
            }
            // return the render operation of the submesh itself
            _nodeSubMesh.GetRenderOperation( op );
        }

        /// <summary>
        ///    
        /// </summary>
        /// <remarks>
        ///     This is only used if the SceneManager chooses to render the node. This option can be set
        ///     for SceneNodes at SceneManager.DisplaySceneNodes, and for entities based on skeletal
        ///     models using Entity.DisplaySkeleton
        /// </remarks>
        /// <param name="matrices"></param>
        /// <ogre name="getWorldTransforms" />
        public void GetWorldTransforms( Matrix4[] matrices )
        {
            matrices[ 0 ] = this.FullTransform;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_camera"></param>
        /// <returns></returns>
        /// <ogre name="getSquaredViewDepth" />
        public Real GetSquaredViewDepth( Camera camera )
        {
            Vector3 difference = this.DerivedPosition - camera.DerivedPosition;

            // return squared length to avoid doing a square root when it is not imperative
            return difference.LengthSquared;
        }

        /// <summary>
        ///		Gets the custom value associated with this Renderable at the given index. 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Vector4 GetCustomParameter( int index )
        {
            if ( _customParameters[index] == null )
            {
                throw new Exception( "A parameter was not found at the given index" );
            }
            else
            {
                return (Vector4)_customParameters[index];
            }
        }

        /// <summary>
        ///		Sets a custom parameter for this Renderable, which may be used to 
        ///		drive calculations for this specific Renderable, like GPU program parameters.
        /// </summary>
        /// <remarks>
        ///		Calling this method simply associates a numeric index with a 4-dimensional
        ///		value for this specific Renderable. This is most useful if the material
        ///		which this Renderable uses a vertex or fragment program, and has an 
        ///		AutoConstant.Custom parameter entry. This parameter entry can refer to the
        ///		index you specify as part of this call, thereby mapping a custom
        ///		parameter for this renderable to a program parameter.
        /// </remarks>
        /// <param name="index">
        ///		The index with which to associate the value. Note that this
        ///		does not have to start at 0, and can include gaps. It also has no direct
        ///		correlation with a GPU program parameter index - the mapping between the
        ///		two is performed by the AutoConstant.Custom entry, if that is used.
        /// </param>
        /// <param name="val">The value to associate.</param>
        public void SetCustomParameter( int index, Vector4 val )
        {
            customParams[index] = val;
        }

        /// <summary>
        ///		Update a custom GpuProgramParameters constant which is derived from 
        ///		information only this Renderable knows.
        /// </summary>
        /// <remarks>
        ///		This method allows a Renderable to map in a custom GPU program parameter
        ///		based on it's own data. This is represented by a GPU auto parameter
        ///		of AutoConstants.Custom, and to allow there to be more than one of these per
        ///		Renderable, the 'data' field on the auto parameter will identify
        ///		which parameter is being updated. The implementation of this method
        ///		must identify the parameter being updated, and call a 'SetConstant' 
        ///		method on the passed in <see cref="GpuProgramParameters"/> object, using the details
        ///		provided in the incoming auto constant setting to identify the index
        ///		at which to set the parameter.
        /// </remarks>
        /// <param name="constant">The auto constant entry referring to the parameter being updated.</param>
        /// <param name="parameters">The parameters object which this method should call to set the updated parameters.</param>
        public void UpdateCustomGpuParameter( GpuProgramParameters.AutoConstantEntry entry, GpuProgramParameters gpuParams )
        {
            if ( customParams[entry.data] != null )
            {
                gpuParams.SetConstant( entry.index, (Vector4)customParams[entry.data] );
            }
        }
        #endregion Methods

        #endregion IRenderable Implementation
    }

}
