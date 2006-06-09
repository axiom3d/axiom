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

using DotNet3D.Math;

#endregion NamespaceDeclarations

namespace Axiom
{
    /// <summary>
    ///		Abstract class definining a movable object in a scene.
    /// </summary>
    /// <remarks>
    ///		Instances of this class are discrete, relatively small, movable objects
    ///		which are attached to SceneNode objects to define their position.						  
    /// </remarks>
    /// <ogre name="MovableObject">
    ///     <file name="OgreMovableObject.h" revision="1.37.2.2" lastUpdated="6/5/06" lastUpdatedBy="Lehuvyterz" />
    ///     <file name="OgreMovableObject.cpp" revision="1.27" lastUpdated="6/5/06" lastUpdatedBy="Lehuvyterz" />
    /// </ogre>
    public abstract class MovableObject : ShadowCaster
    {
        #region Fields and Properties

        #region renderQueueIDSet Property

        private bool _renderQueueIDSet = false;
        /// <summary>
        ///    Flags whether the RenderQueue's default should be used.
        /// </summary>
        /// <ogre name="mRenderQueueIDset" />
        protected bool renderQueueIDSet
        {
            get
            {
                return _renderQueueIDSet;
            }
            set
            {
                _renderQueueIDSet = value;
            }
        }

        #endregion renderQueueIDSet Property

        #region worldAABB Property

        private AxisAlignedBox _worldAABB;
        /// <summary>
        ///    Cached world bounding box of this object.
        /// </summary>
        /// <ogre name="mWorldAABB" />
        protected AxisAlignedBox worldAABB
        {
            get
            {
                return _worldAABB;
            }
            set
            {
                _worldAABB = value;
            }
        }

        #endregion worldAABB Property

        #region worldBoundingSphere Property

        private Sphere _worldBoundingSphere = new Sphere();
        /// <summary>
        ///    Cached world bounding spehere.
        /// </summary>
        /// <ogre name="mWorldBoundingSphere" />
        protected Sphere worldBoundingSphere
        {
            get
            {
                return _worldBoundingSphere;
            }
            set
            {
                _worldBoundingSphere = value;
            }
        }

        #endregion worldBoundingSphere Property

        #region parentIsTagPoint Property

        private bool _parentIsTagPoint;
        /// <summary>
        ///		Flag which indicates whether this objects parent is a <see cref="TagPoint"/>.
        /// </summary>
        /// <ogre name="mParentIsTagPoint" />
        protected bool parentIsTagPoint
        {
            get
            {
                return _parentIsTagPoint;
            }
            set
            {
                _parentIsTagPoint = value;
            }
        }

        #endregion parentIsTagPoint Property

        #region worldDarkCapBounds Property

        private AxisAlignedBox _worldDarkCapBounds = AxisAlignedBox.Null;
        /// <summary>
        ///		World space AABB of this object's dark cap.
        /// </summary>
        /// <ogre name="mWorldDarkCapBounds" />
        protected AxisAlignedBox worldDarkCapBounds
        {
            get
            {
                return _worldDarkCapBounds;
            }
            set
            {
                _worldDarkCapBounds;
            }
        }

        #endregion worldDarkCapBounds Property

        #region castShadows Property

        private bool _castShadows;
        /// <summary>
        ///		Does this object cast shadows?
        /// </summary>
        /// <ogre name="mCastShadows" />
        protected bool castShadows
        {
            get
            {
                return _castShadows;
            }
            set
            {
                _castShadows = value;
            }
        }

        #endregion castShadows Property

        #region ShadowRenderableList Property

        private ShadowRenderableList _dummyList = new ShadowRenderableList();
        protected ShadowRenderableList dummyList
        {
            get
            {
                return _dummyList;
            }
            set
            {
                _dummyList = value;
            }
        }

        #endregion ShadowRenderableList Property
			
        /// <summary>
        ///		An abstract method required by subclasses to return the bounding box of this object in local coordinates.
        /// </summary>
        /// <ogre name="getBoundingBox" />
        public abstract AxisAlignedBox BoundingBox
        {
            get;
        }

        /// <summary>
        ///		An abstract method required by subclasses to return the bounding box of this object in local coordinates.
        /// </summary>
        /// <ogre name="getBoundingRadius" />
        public abstract Real BoundingRadius
        {
            get;
        }


        #region userData Property

        /// <summary>
        ///    A link back to a GameObject (or subclass thereof) that may be associated with this SceneObject.
        /// </summary>
        /// <ogre name="mUserObject" />
        private object _userData;
        /// <summary>
        ///     Get/Sets a link back to a GameObject (or subclass thereof, such as Entity) that may be associated with this SceneObject.
        /// </summary>
        /// <ogre name="getuserObject" />
        /// <ogre name="setUserObject" />
        public object UserData
        {
            get
            {
                return _userData;
            }
            set
            {
                _userData = value;
            }
        }

        #endregion userData Property
			
        #region ParentNode Property

        /// <summary>
        ///    Node that this node is attached to.
        /// </summary>
        /// <ogre name="mParentNode" />
        private Node _parentNode;
        /// <summary>
        ///		Gets the parent node that this object is attached to.
        /// </summary>
        /// <ogre name="getParentNode" />
        public Node ParentNode
        {
            get
            {
                return _parentNode;
            }
            protected set
            {
                _parentNode = value;
            }
        }

        /// <summary>
        ///     Gets the scene node to which this object is attached.
        /// </summary>
        /// <ogre name="getParentSceneNode" />
        public SceneNode ParentSceneNode
        {
            get
            {
                if ( _parentIsTagPoint )
                {
                    TagPoint tp = (TagPoint)_parentNode;
                    return tp.ParentEntity.ParentSceneNode;
                }
                else
                {
                    return (SceneNode)_parentNode;
                }
            }
        }

        #endregion ParentNode Property

        /// <summary>
        ///		See if this object is attached to another node.
        /// </summary>
        /// <ogre name="isAttached" />
        public bool IsAttached
        {
            get
            {
                return ( _parentNode != null );
            }
        }

        public bool IsInScene
        {
            get
            {
                if ( _parentNode != null )
                {
                    if ( _parentIsTagPoint )
                    {
                        TagPoint tp = (TagPoint)_parentNode;
                        return tp.ParentEntity.IsInScene;
                    }
                    else
                    {
                        SceneNode sn = (SceneNode)_parentNode;
                        return sn.IsInSceneGraph;
                    }
                }
                else
                {
                    return false;
                }
            }
        }

        #region IsVisible Property

        /// <summary>
        ///    Is this object visible?
        /// </summary>
        /// <ogre name="mVisible" />
        private bool _isVisible;
        /// <summary>
        ///		States whether or not this object should be visible.
        /// </summary>
        /// <ogre name="isVisible" />
        /// <ogre name="setVisible" />
        public virtual bool IsVisible
        {
            get
            {
                return _isVisible;
            }
            set
            {
                _isVisible = value;
            }
        }

        #endregion IsVisible Property

        #region Name Property

        /// <summary>
        ///    Name of this object.
        /// </summary>
        private string _name;
        /// <summary>
        ///		Name of this SceneObject.
        /// </summary>
        /// <ogre name="getname" />
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }

        #endregion Name Property
			
        /// <summary>
        ///    		Returns the full transformation of the parent SceneNode or the attachingPoint node
        /// </summary>
        public virtual Matrix4 ParentFullTransform
        {
            get
            {
                if ( _parentNode != null )
                    return _parentNode.FullTransform;

                // identity if no parent
                return Matrix4.Identity;
            }
        }

        /// <summary>
        ///		Gets the full transformation of the parent SceneNode or TagPoint.
        /// </summary>
        /// <ogre name="_getParentNodeFullTransform" />
        public virtual Matrix4 ParentNodeFullTransform
        {
            get
            {
                if ( _parentNode != null )
                {
                    // object is attached to a node, so return the nodes transform
                    return _parentNode.FullTransform;
                }

                // fallback
                return Matrix4.Identity;
            }
        }

        #region QueryFlags Property

        /// <summary>
        ///    Flags determining whether this object is included/excluded from scene queries.
        /// </summary>
        /// <ogre name="mQueryFlags" />
        private ulong _queryFlags;
        /// <summary>
        ///		Gets/Sets the query flags for this object.
        /// </summary>
        /// <remarks>
        ///		When performing a scene query, this object will be included or excluded according
        ///		to flags on the object and flags on the query. This is a bitwise value, so only when
        ///		a bit on these flags is set, will it be included in a query asking for that flag. The
        ///		meaning of the bits is application-specific.
        /// </remarks>
        /// <ogre name="getQueryFlags" />
        /// <ogre name="setQueryFlags" />
        public ulong QueryFlags
        {
            get
            {
                return _queryFlags;
            }
            set
            {
                _queryFlags = value;
            }
        }

        #endregion QueryFlags Property
			
        /// <summary>
        ///    Allows showing the bounding box of an invidual SceneObject.
        /// </summary>
        /// <remarks>
        ///    This shows the bounding box of the SceneNode that the SceneObject is currently attached to.
        /// </remarks>
        public bool ShowBoundingBox
        {
            get
            {
                return ( (SceneNode)_parentNode ).ShowBoundingBox;
            }
            set
            {
                ( (SceneNode)_parentNode ).ShowBoundingBox = value;
            }
        }

        #region RenderQueueGroup Property

        /// <summary>
        ///    The render queue to use when rendering this object.
        /// </summary>
        /// <ogre name="mRenderQueueID" />
        private RenderQueueGroupID _renderQueueID;
        /// <summary>
        ///		Gets/Sets the render queue group this entity will be rendered through.
        /// </summary>
        /// <remarks>
        ///		Render queues are grouped to allow you to more tightly control the ordering
        ///		of rendered objects. If you do not call this method, all Entity objects default
        ///		to <see cref="RenderQueueGroupID.Main"/> which is fine for most objects. You may want to alter this
        ///		if you want this entity to always appear in front of other objects, e.g. for
        ///		a 3D menu system or such.
        /// </remarks>
        /// <ogre name="getRenderQueueGroup" />
        /// <ogre name="setRenderQueueGroup" />
        public RenderQueueGroupID RenderQueueGroup
        {
            get
            {
                return _renderQueueID;
            }
            set
            {
                _renderQueueID = value;
                _renderQueueIDSet = true;
            }
        }

        #endregion RenderQueueGroup Property
			
        #endregion Fields and Properties

        #region Constructors

        /// <summary>
        ///		Default constructor.
        /// </summary>
        /// <ogre name="MovableObject" />
        public MovableObject()
        {
            isVisible = true;

            // set default RenderQueueGroupID for this movable object
            renderQueueID = RenderQueueGroupID.Main;

            queryFlags = unchecked( 0xffffffff );

            worldAABB = AxisAlignedBox.Null;

            castShadows = true;
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        ///		Appends the specified flags to the current flags for this object.
        /// </summary>
        /// <param name="flags"></param>
        /// <ogre name="addQueryFlags" />
        public void AddQueryFlags( ulong flags )
        {
            queryFlags |= flags;
        }
        
        /// <summary>
        ///    Retrieves the axis-aligned bounding box for this object in world coordinates.
        /// </summary>
        /// <returns></returns>
        /// <ogre name="getWorldBoundingSphere" />
        public override AxisAlignedBox GetWorldBoundingBox( bool derive )
        {
            if ( derive )
            {
                _worldAABB = this.BoundingBox;
                _worldAABB.Transform( this.ParentFullTransform );
            }

            return worldAABB;
        }

        /// <summary>
        ///    Overloaded method.  Calls the overload with a default of not deriving the transform.
        /// </summary>
        /// <returns></returns>
        /// <ogre name="getWorldBoundingSphere" />
        public Sphere GetWorldBoundingSphere()
        {
            return GetWorldBoundingSphere( false );
        }

        /// <summary>
        ///    Retrieves the worldspace bounding sphere for this object.
        /// </summary>
        /// <param name="derive">Whether or not to derive from parent transforms.</param>
        /// <returns></returns>
        /// <ogre name="getWorldBoundingSphere" />
        public virtual Sphere GetWorldBoundingSphere( bool derive )
        {
            if ( derive )
            {
                _worldBoundingSphere.Radius = this.BoundingRadius;
                _worldBoundingSphere.Center = _parentNode.DerivedPosition;
            }

            return _worldBoundingSphere;
        }

        /// <summary>
        ///		Removes the specified flags from the current flags for this object.
        /// </summary>
        /// <param name="flags"></param>
        /// <ogre name="removeQueryFlags" />
        public void RemoveQueryFlags( ulong flags )
        {
            queryFlags &= ~flags;
        }

        #endregion Methods

        #region ShadowCaster Implementation

        /// <summary>
        ///		Overridden.
        /// </summary>
        /// <ogre name="getCastShadows" />
        /// <ogre name="setCastShadows" />
        public override bool CastsShadows
        {
            get
            {
                return castShadows;
            }
            set
            {
                castShadows = value;
            }
        }
        
        /// <ogre name="getLightCapBounds" />
        public override AxisAlignedBox GetLightCapBounds()
        {
            // same as original bounds
            return GetWorldBoundingBox();
        }
        
        /// <summary>
        ///		
        /// </summary>
        /// <param name="light"></param>
        /// <param name="extrusionDistance"></param>
        /// <returns></returns>
        /// <ogre name="getDarkCapBounds" />
        public override AxisAlignedBox GetDarkCapBounds( Light light, Real extrusionDistance )
        {
            // Extrude own light cap bounds
            // need a clone to avoid modifying the original bounding box
            _worldDarkCapBounds = (AxisAlignedBox)GetLightCapBounds().Clone();

            ExtrudeBounds( _worldDarkCapBounds, light.GetAs4DVector(), extrusionDistance );

            return _worldDarkCapBounds;
        }
        
        /// <summary>
        ///		Overridden.  Returns null by default.
        /// </summary>
        public override EdgeData GetEdgeList( int lodIndex )
        {
            return null;
        }
        
        /// <ogre name="getShadowVolumeRenderableIterator" />
        public override IEnumerator GetShadowVolumeRenderableEnumerator( ShadowTechnique technique, Light light,
            HardwareIndexBuffer indexBuffer, bool extrudeVertices, Real extrusionDistance, int flags )
        {

            return dummyList.GetEnumerator();
        }

        public override IEnumerator GetLastShadowVolumeRenderableEnumerator()
        {
            return dummyList.GetEnumerator();
        }

        /// <summary>
        ///		Get the distance to extrude for a point/spot light
        /// </summary>
        /// <param name="light"></param>
        /// <returns></returns>
        /// <ogre name="getPointExtrusionDistance" />
        public override Real GetPointExtrusionDistance( Light light )
        {
            if ( parentNode != null )
            {
                return GetExtrusionDistance( parentNode.DerivedPosition, light );
            }
            else
            {
                return 0;
            }
        }

        #endregion ShadowCaster Implementation

        #region Internal engine methods

        /// <summary>
        ///		Internal method called to notify the object that it has been attached to a node.
        /// </summary>
        /// <param name="node">Scene node to notify.</param>
        /// <ogre name="_notifyAttached" />
        internal virtual void NotifyAttached( Node node )
        {
            NotifyAttached( node, false );
        }

        /// <summary>
        ///		Internal method called to notify the object that it has been attached to a node.
        /// </summary>
        /// <param name="node">Scene node to notify.</param>
        /// <ogre name="_notifyAttached" />
        internal virtual void NotifyAttached( Node node, bool isTagPoint )
        {
            _parentNode = node;
            _parentIsTagPoint = isTagPoint;
        }

        /// <summary>
        ///		Internal method to notify the object of the camera to be used for the next rendering operation.
        /// </summary>
        /// <remarks>
        ///		Certain objects may want to do specific processing based on the camera position. This method notifies
        ///		them incase they wish to do this.
        /// </remarks>
        /// <param name="camera">Reference to the Camera being used for the current rendering operation.</param>
        /// <ogre name="_notifyCurrentCamera" />
        public abstract void NotifyCurrentCamera( Camera camera );

        /// <summary>
        ///		An abstract method that causes the specified RenderQueue to update itself.  
        /// </summary>
        /// <remarks>This is an internal method used by the engine assembly only.</remarks>
        /// <param name="queue">The render queue that this object should be updated in.</param>
        /// <ogre name="_updateRenderQueue" />
        public abstract void UpdateRenderQueue( RenderQueue queue );

        #endregion Internal engine methods
    }
}
