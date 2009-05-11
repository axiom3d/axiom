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
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections;
using System.Diagnostics;

using Axiom.Core;
using Axiom.Math;
using Axiom.Graphics;
using System.Collections.Generic;

#endregion Namespace Declarations

#region Ogre Synchronization Information
/// <ogresynchronization>
///     <file name="OgreOverlayContainer.h"   revision="1.4.2.1" lastUpdated="10/5/2005" lastUpdatedBy="DanielH" />
///     <file name="OgreOverlayContainer.cpp" revision="1.5.2.1" lastUpdated="10/5/2005" lastUpdatedBy="DanielH" />
/// </ogresynchronization>
#endregion

namespace Axiom.Overlays
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

        protected Dictionary<string, OverlayElement> children = new Dictionary<string, OverlayElement>();
        protected List<OverlayElement> childList = new List<OverlayElement>();
        protected Dictionary<string, OverlayElement> childContainers = new Dictionary<string, OverlayElement>();
        protected bool childrenProcessEvents;

        /// <summary>
        /// Gets the children OverlayElements as a Key-Value collection
        /// </summary>
        public IDictionary<string, OverlayElement> Children
        {
            get
            {
                return children;
            }
        }

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
            element.NotifyWorldTransforms( xform );
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
            Debug.Assert( children.ContainsKey( name ), string.Format( "Child with name '{0}' not found.", name ) );

            OverlayElement element = GetChild( name );
            children.Remove( name );
            childList.Remove( element );
            // remove from container list (if found)
            if ( childContainers.ContainsKey( name ) )
            {
                childContainers.Remove( name );
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

            return (OverlayElement)children[ name ];
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
                ( (OverlayElement)childList[ i ] ).PositionsOutOfDate();
            }
        }

        public override void Update()
        {
            // call base class method
            base.Update();

            for ( int i = 0; i < childList.Count; i++ )
            {
                ( (OverlayElement)childList[ i ] ).Update();
            }
        }

        public override int NotifyZOrder( int zOrder )
        {
            // call base class method
            base.NotifyZOrder( zOrder );

            //One for us
            zOrder++;

            for ( int i = 0; i < childList.Count; i++ )
            {
                zOrder = ( (OverlayElement)childList[ i ] ).NotifyZOrder( zOrder );
            }

            return zOrder;
        }
        

        public override void NotifyWorldTransforms( Matrix4[] xform )
        {
            base.NotifyWorldTransforms( xform );

            // Update children
            for ( int i = 0; i < childList.Count; i++ )
            {
                ( (OverlayElement)childList[ i ] ).NotifyWorldTransforms( xform );
            }
        }

        public override void NotifyViewport()
        {
            base.NotifyViewport();
            // Update children
            for ( int i = 0; i < childList.Count; i++ )
            {
                OverlayElement overlayElement = (OverlayElement)childList[ i ];
                overlayElement.NotifyViewport();
            }
        }

        public override void NotifyParent( OverlayElementContainer parent, Overlay overlay )
        {
            // call the base class method
            base.NotifyParent( parent, overlay );

            for ( int i = 0; i < childList.Count; i++ )
            {
                ( (OverlayElement)childList[ i ] ).NotifyParent( this, overlay );
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
                    ( (OverlayElement)childList[ i ] ).UpdateRenderQueue( queue );
                }
            }
        }
        public override OverlayElement FindElementAt( float x, float y )
        {
            OverlayElement ret = null;

            int currZ = -1;

            if ( isVisible )
            {
                ret = base.FindElementAt( x, y );	//default to the current container if no others are found
                if ( ret != null && childrenProcessEvents )
                {
                    for ( int i = 0; i < childList.Count; i++ )
                    {
                        OverlayElement currentOverlayElement = (OverlayElement)childList[ i ];

                        if ( currentOverlayElement.IsVisible && currentOverlayElement.Enabled )
                        {
                            int z = currentOverlayElement.ZOrder;
                            if ( z > currZ )
                            {
                                OverlayElement elementFound = currentOverlayElement.FindElementAt( x, y );
                                if ( elementFound != null )
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
            while ( en.MoveNext() )
            {
                ( (OverlayElementContainer)en.Value ).Initialize();
            }

            for ( int i = 0; i < childList.Count; i++ )
            {
                ( (OverlayElement)childList[ i ] ).Initialize();

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
