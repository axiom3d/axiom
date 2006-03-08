using System;
using System.Collections;
using System.Diagnostics;
using Axiom;
using Axiom.MathLib;
#region Ogre Synchronization Information
/// <ogresynchronization>
///     <file name="OgreOverlayContainer.h"   revision="1.4.2.1" lastUpdated="10/5/2005" lastUpdatedBy="DanielH" />
///     <file name="OgreOverlayContainer.cpp" revision="1.5.2.1" lastUpdated="10/5/2005" lastUpdatedBy="DanielH" />
/// </ogresynchronization>
#endregion


namespace Axiom
{
    /// <summary>
    /// 	A 2D element which contains other OverlayElement instances.
    /// </summary>
    /// <remarks>
    /// 	This is a specialization of OverlayElement for 2D elements that contain other
    /// 	elements. These are also the smallest elements that can be attached directly
    /// 	to an Overlay.
    /// 	<p/>
    /// 	OverlayElementContainers should be managed using GuiManager. This class is responsible for
    /// 	instantiating elements, and also for accepting new types of element
    /// 	from plugins etc.
    /// </remarks>
    public abstract class OverlayElementContainer : OverlayElement
    {
        #region Member variables

        protected Hashtable children = new Hashtable();
        protected ArrayList childList = new ArrayList();
        protected Hashtable childContainers = new Hashtable();

		protected bool childrenProcessEvents;

        #endregion

        #region Constructors

        /// <summary>
        ///    Don't use directly, create through GuiManager.CreateElement.
        /// </summary>
        /// <param name="name"></param>
        protected internal OverlayElementContainer( string name )
            : base( name )
        {
			childrenProcessEvents = true;
        }

        #endregion

        #region Methods

        /// <summary>
        ///    Adds another OverlayElement to this container.
        /// </summary>
        /// <param name="element"></param>
        public virtual void AddChild( OverlayElement element )
        {
            if ( element.IsContainer )
            {
                AddChildImpl( (OverlayElementContainer)element );
            }
            else
            {
                AddChildImpl( element );
            }
        }

        /// <summary>
        ///    Adds another OverlayElement to this container.
        /// </summary>
        /// <param name="element"></param>
        public virtual void AddChildImpl( OverlayElement element )
        {
            Debug.Assert( !children.ContainsKey( element.Name ), string.Format( "Child with name '{0}' already defined.", element.Name ) );

            // add to lookup table and list
            children.Add( element.Name, element );
            childList.Add( element );

            // inform this child about his/her parent and zorder
            element.NotifyParent( this, overlay );
            element.NotifyZOrder( zOrder + 1 );
			element.NotifyWorldTransforms(xform);
			element.NotifyViewport();
        }

        /// <summary>
        ///    Add a nested container to this container.
        /// </summary>
        /// <param name="container"></param>
        public virtual void AddChildImpl( OverlayElementContainer container )
        {
            // add this container to the main child list first
            OverlayElement element = container;
            AddChildImpl( element );
			/*
            element.NotifyParent( this, overlay );
            element.NotifyZOrder( zOrder + 1 );

            // inform container children of the current overlay
            // *gasp* it's a foreach!  this isn't a time critical method anyway
            foreach ( OverlayElement child in container.children )
            {
                child.NotifyParent( container, overlay );
                child.NotifyZOrder( container.ZOrder + 1 );
            }
        */
            // now add the container to the container collection
            childContainers.Add( container.Name, container );
        }
		public virtual void RemoveChild( string name )
		{
			Debug.Assert( !children.ContainsKey( name ), string.Format( "Child with name '{0}' not found.", name ) );

			OverlayElement element =  GetChild(name);
			children.Remove(name);

			// remove from container list (if found)
			if (childContainers.ContainsKey( name ))
			{
				childContainers.Remove(name);
			}
			element.Parent = null;


		}
        /// <summary>
        ///    Gets the named child of this container.
        /// </summary>
        /// <param name="name"></param>
        public virtual OverlayElement GetChild( string name )
        {
            Debug.Assert( children.ContainsKey( name ), "children.ContainsKey(name)" );

            return (OverlayElement)children[name];
        }

        /// <summary>
        ///    Tell the object and its children to recalculate their positions.
        /// </summary>
        public override void PositionsOutOfDate()
        {
            // call baseclass method
            base.PositionsOutOfDate();

            for ( int i = 0; i < childList.Count; i++ )
            {
                ( (OverlayElement)childList[i] ).PositionsOutOfDate();
            }
        }

        public override void Update()
        {
            // call base class method
            base.Update();

            for ( int i = 0; i < childList.Count; i++ )
            {
                ( (OverlayElement)childList[i] ).Update();
            }
        }

        public override void NotifyZOrder( int zOrder )
        {
            // call base class method
            base.NotifyZOrder( zOrder );

            for ( int i = 0; i < childList.Count; i++ )
            {
                ( (OverlayElement)childList[i] ).NotifyZOrder( zOrder + 1 );
            }
        }
		public override void NotifyWorldTransforms(Matrix4[] xform)
		{
			base.NotifyWorldTransforms(xform);

			// Update children
			for ( int i = 0; i < childList.Count; i++ )
			{
				( (OverlayElement)childList[i] ).NotifyWorldTransforms( xform );
			}
		}

		public override void NotifyViewport()
		{
			base.NotifyViewport();
			// Update children
			for ( int i = 0; i < childList.Count; i++ )
			{
				OverlayElement overlayElement = (OverlayElement)childList[i];
				overlayElement.NotifyViewport();
			}
		}

        public override void NotifyParent( OverlayElementContainer parent, Overlay overlay )
        {
            // call the base class method
            base.NotifyParent( parent, overlay );

            for ( int i = 0; i < childList.Count; i++ )
            {
                ( (OverlayElement)childList[i] ).NotifyParent( this, overlay );
            }
        }



        public override void UpdateRenderQueue( RenderQueue queue )
        {
            if ( isVisible )
            {
                // call base class method
                base.UpdateRenderQueue( queue );

                for ( int i = 0; i < childList.Count; i++ )
                {
                    ( (OverlayElement)childList[i] ).UpdateRenderQueue( queue );
                }
            }
        }
		public override OverlayElement FindElementAt(float x, float y)
		{
			OverlayElement ret = null;

			int currZ = -1;

			if (isVisible)
			{
				ret = base.FindElementAt(x,y);	//default to the current container if no others are found
				if (ret !=null && childrenProcessEvents)
				{
					for ( int i = 0; i < childList.Count; i++ )
					{
						OverlayElement currentOverlayElement = (OverlayElement)childList[i];

						if (currentOverlayElement.IsVisible && currentOverlayElement.Enabled)
						{
							int z = currentOverlayElement.ZOrder;
							if (z > currZ)
							{
								OverlayElement elementFound = currentOverlayElement.FindElementAt(x ,y );
								if (elementFound != null)
								{
									currZ = z;
									ret = elementFound;
								}
							}
						}
					}
				}
			}
			return ret;
		}
        #endregion

        #region Properties

        /// <summary>
        ///    This is most certainly a container.
        /// </summary>
        public override bool IsContainer
        {
            get
            {
                return true;
            }
        }
		/// <summary>
		///   Should this container pass events to their children 
		/// </summary>
		public bool IsChildrenProcessEvents
		{
			get
			{
				return true;
			}
			set
			{
				childrenProcessEvents = value;
			}
		}

        #endregion
    
		public override void Initialize()
		{
			IDictionaryEnumerator en = childContainers.GetEnumerator();
			while (en.MoveNext())
			{
				((OverlayElementContainer)en.Value ).Initialize();
			}

			for ( int i = 0; i < childList.Count; i++ )
			{
				((OverlayElement)childList[i] ).Initialize();

			}

		}
//		public void CopyFromTemplate(OverlayElement templateOverlay)
//		{
//			base.CopyFromTemplate(templateOverlay);
//
//			if (templateOverlay.IsContainer() && isContainer)
//			{
//				OverlayContainer::ChildIterator it = static_cast<OverlayContainer*>(templateOverlay).getChildIterator();
//				while (it.hasMoreElements())
//				{
//					OverlayElement* oldChildElement = it.getNext();
//					if (oldChildElement.isCloneable())
//					{
//						OverlayElement* newChildElement = 
//						OverlayManager::getSingleton().createOverlayElement(
//											oldChildElement.getTypeName(), 
//											mName+"/"+oldChildElement.getName());
//						oldChildElement.copyParametersTo(newChildElement);
//						addChild((OverlayContainer*)newChildElement);
//					}
//				}
//			}
//		}
//
//		public OverlayElement Clone(string instanceName)
//		{
//			OverlayElementContainer newContainer;
//
//			newContainer = static_cast<OverlayContainer*>(OverlayElement::clone(instanceName));
//
//			ChildIterator it = getChildIterator();
//			while (it.hasMoreElements())
//			{
//				OverlayElement* oldChildElement = it.getNext();
//				if (oldChildElement->isCloneable())
//				{
//					OverlayElement* newChildElement = oldChildElement->clone(instanceName);
//					newContainer->_addChild(newChildElement);
//				}
//			}
//
//			return newContainer;
//		}
	}
}
