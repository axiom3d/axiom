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
using System.Collections;
using System.Collections.Generic;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;
using Axiom.Scripting;
using Axiom.Core.Collections;

#endregion Namespace Declarations

#region Ogre Synchronization Information

// <ogresynchronization>
//     <file name="OgreOverlayElement.h"   revision="1.8" lastUpdated="10/5/2005" lastUpdatedBy="DanielH" />
//     <file name="OgreOverlayElement.cpp" revision="1.11.2.3" lastUpdated="10/5/2005" lastUpdatedBy="DanielH" />
// </ogresynchronization>

#endregion Ogre Synchronization Information

namespace Axiom.Overlays
{
	/// <summary>
	/// 	Abstract definition of a 2D element to be displayed in an Overlay.
	/// </summary>
	/// <remarks>
	/// 	This class abstracts all the details of a 2D element which will appear in
	/// 	an overlay. In fact, not all OverlayElement instances can be directly added to an
	/// 	Overlay, only those which are OverlayElementContainer instances (derived from this class) are able to be added,
	/// 	however they can contain any OverlayElement however. This is done to enforce some level of grouping of widgets.
	/// 	<br/>
	/// 	OverlayElements should be managed using OverlayElementManager. This class is responsible for
	/// 	instantiating / deleting elements, and also for accepting new types of element
	/// 	from plugins etc.
	/// 	<br/>
	/// 	Note that positions / dimensions of 2D screen elements are expressed as parametric
	/// 	values (0.0 - 1.0) because this makes them resolution-independent. However, most
	/// 	screen resolutions have an aspect ratio of 1.3333:1 (width : height) so note that
	/// 	in physical pixels 0.5 is wider than it is tall, so a 0.5x0.5 panel will not be
	/// 	square on the screen (but it will take up exactly half the screen in both dimensions).
	/// </remarks>
	public abstract class OverlayElement : ScriptableObject, IRenderable
	{
		#region Member variables

		protected string name;
		protected bool isVisible;
		protected bool isCloneable;
		protected float left, top, width, height;
		protected string materialName;
		protected Material material;
		protected string text;
		protected ColorEx color;
		protected Rectangle clippingRegion;

		protected MetricsMode metricsMode;
		protected HorizontalAlignment horzAlign;
		protected VerticalAlignment vertAlign;

		// Pixel-mode positions, used in GMM_PIXELS mode.
		protected float pixelTop;
		protected float pixelLeft;
		protected float pixelWidth;
		protected float pixelHeight;
		protected float pixelScaleX;
		protected float pixelScaleY;
		// parent container
		protected OverlayElementContainer parent;
		// overlay this element is attached to
		protected Overlay overlay;

		protected float derivedLeft, derivedTop;
		protected bool isDerivedOutOfDate;
		// Flag indicating if the vertex positons need recalculating
		protected bool isGeomPositionsOutOfDate;
		// Flag indicating if the vertex uvs need recalculating
		protected bool isGeomUVsOutOfDate;
		// Zorder for when sending to render queue
		// Derived from parent
		protected int zOrder;

		// world transforms
		protected Matrix4[] xform = new Matrix4[1]
		                            {
		                            	Matrix4.Identity
		                            };

		protected bool isEnabled;

		// is element initialised
		protected bool isInitialized;

		// Used to see if this element is created from a Template
		protected OverlayElement sourceTemplate;

		protected LightList emptyLightList = new LightList();
		protected List<Vector4> customParams = new List<Vector4>();

		#endregion Member variables

		#region Constructors

		/// <summary>
		///
		/// </summary>
		/// <param name="name"></param>
		protected internal OverlayElement( string name )
			: base()
		{
			this.name = name;
			this.isVisible = true;
			this.isCloneable = true;
			this.left = 0.0f;
			this.top = 0.0f;
			this.width = 1.0f;
			this.height = 1.0f;
			this.metricsMode = MetricsMode.Relative;
			this.horzAlign = HorizontalAlignment.Left;
			this.vertAlign = VerticalAlignment.Top;
			this.pixelTop = 0.0f;
			this.pixelLeft = 0.0f;
			this.pixelWidth = 1.0f;
			this.pixelHeight = 1.0f;
			this.pixelScaleX = 1.0f;
			this.pixelScaleY = 1.0f;
			this.parent = null;
			this.overlay = null;
			this.isDerivedOutOfDate = true;
			this.isGeomPositionsOutOfDate = true;
			this.isGeomUVsOutOfDate = true;
			this.zOrder = 0;
			this.isEnabled = true;
			this.isInitialized = false;
			this.sourceTemplate = null;
		}

		#endregion Constructors

		#region Methods

		/// <summary>
		///    Copys data from the template element to this element to clone it.
		/// </summary>
		/// <param name="template"></param>
		/// <returns></returns>
		public virtual void CopyFromTemplate( OverlayElement template )
		{
			template.CopyParametersTo( this );
			this.sourceTemplate = template;
		}

		public void CopyParametersTo( OverlayElement instance )
		{
			foreach ( var command in Commands )
			{
				var srcValue = command.Get( this );
				command.Set( instance, srcValue );
			}
		}

		public virtual OverlayElement Clone( string instanceName )
		{
			var newElement = OverlayElementManager.Instance.CreateElement( GetType().Name, instanceName + "/" + this.name );
			CopyParametersTo( newElement );

			return newElement;
		}

		/// <summary>
		///    Hides an element if it is currently visible.
		/// </summary>
		public void Hide()
		{
			this.isVisible = false;
		}

		/// <summary>
		///    Initialize the OverlayElement.
		/// </summary>
		public abstract void Initialize();

		/// <summary>
		///    Internal method for notifying the gui element of it's parent and ultimate overlay.
		/// </summary>
		/// <param name="parent">Parent of this element.</param>
		/// <param name="overlay">Overlay this element belongs to.</param>
		public virtual void NotifyParent( OverlayElementContainer parent, Overlay overlay )
		{
			this.parent = parent;
			this.overlay = overlay;

			if ( overlay != null && overlay.IsInitialized && !this.isInitialized )
			{
				Initialize();
			}

			this.isDerivedOutOfDate = true;
		}

		/// <summary>
		/// Internal method to notify the element when Zorder of parent overlay
		/// has changed.
		/// </summary>
		/// <param name="zOrder">The z order.</param>
		/// <remarks>
		/// Overlays have explicit Z orders. OverlayElements do not, they inherit the
		/// ZOrder of the overlay, and the Zorder is incremented for every container
		/// nested within this to ensure that containers are displayed behind contained
		/// items. This method is used internally to notify the element of a change in
		/// final zorder which is used to render the element.
		/// </remarks>
		/// <returns>
		/// Return the next zordering number availble. For single elements, this
		/// is simply zOrder + 1, but for containers, they increment it once for each
		/// child (more if those children are also containers).
		/// </returns>
		public virtual int NotifyZOrder( int zOrder )
		{
			this.zOrder = zOrder;
			return this.zOrder + 1;
		}

		/// <summary>
		/// Notifies the world transforms.
		/// </summary>
		/// <param name="xform">The xform.</param>
		public virtual void NotifyWorldTransforms( Matrix4[] xform )
		{
			this.xform = xform;
		}

		/// <summary>
		/// Notifies the viewport.
		/// </summary>
		public virtual void NotifyViewport()
		{
			switch ( this.metricsMode )
			{
				case MetricsMode.Pixels:
				{
					var oMgr = OverlayManager.Instance;
					float vpWidth = oMgr.ViewportWidth;
					float vpHeight = oMgr.ViewportHeight;

					// cope with temporarily zero dimensions, avoid divide by zero
					vpWidth = vpWidth == 0.0f ? 1.0f : vpWidth;
					vpHeight = vpHeight == 0.0f ? 1.0f : vpHeight;

					this.pixelScaleX = 1.0f/vpWidth;
					this.pixelScaleY = 1.0f/vpHeight;
				}
					break;

				case MetricsMode.Relative_Aspect_Adjusted:
				{
					var oMgr = OverlayManager.Instance;
					float vpWidth = oMgr.ViewportWidth;
					float vpHeight = oMgr.ViewportHeight;

					// cope with temporarily zero dimensions, avoid divide by zero
					vpWidth = vpWidth == 0.0f ? 1.0f : vpWidth;
					vpHeight = vpHeight == 0.0f ? 1.0f : vpHeight;

					this.pixelScaleX = 1.0f/( 10000.0f*( vpWidth/vpHeight ) );
					this.pixelScaleY = 1.0f/10000.0f;
				}
					break;

				case MetricsMode.Relative:
					this.pixelScaleX = 1.0f;
					this.pixelScaleY = 1.0f;
					this.pixelLeft = this.left;
					this.pixelTop = this.top;
					this.pixelWidth = this.width;
					this.pixelHeight = this.height;
					break;
			}

			this.left = this.pixelLeft*this.pixelScaleX;
			this.top = this.pixelTop*this.pixelScaleY;
			this.width = this.pixelWidth*this.pixelScaleX;
			this.height = this.pixelHeight*this.pixelScaleY;

			this.isGeomPositionsOutOfDate = true;
		}

		/// <summary>
		///    Tells this element to recaculate it's position.
		/// </summary>
		public virtual void PositionsOutOfDate()
		{
			this.isGeomPositionsOutOfDate = true;
		}

		/// <summary>
		/// Sets the dimensions.
		/// </summary>
		/// <param name="width">The width.</param>
		/// <param name="height">The height.</param>
		public void SetDimensions( float width, float height )
		{
			if ( this.metricsMode != MetricsMode.Relative )
			{
				this.pixelWidth = (int)width;
				this.pixelHeight = (int)height;
			}
			else
			{
				this.width = width;
				this.height = height;
			}

			this.isDerivedOutOfDate = true;
			PositionsOutOfDate();
		}

		/// <summary>
		///    Sets param values from script values.  Subclasses can define their own params in addition to what
		///    this base class already defines.
		/// </summary>
		/// <param name="param"></param>
		/// <param name="val"></param>
		public bool SetParam( string param, string val )
		{
			Properties[ param ] = val;
			return true;
		}

		/// <summary>
		/// Sets the position of this element.
		/// </summary>
		/// <param name="left">The left.</param>
		/// <param name="top">The top.</param>
		public void SetPosition( float left, float top )
		{
			if ( this.metricsMode != MetricsMode.Relative )
			{
				this.pixelLeft = (int)left;
				this.pixelTop = (int)top;
			}
			else
			{
				this.left = left;
				this.top = top;
			}

			this.isDerivedOutOfDate = true;
			PositionsOutOfDate();
		}

		/// <summary>
		///    Shows this element if it was previously hidden.
		/// </summary>
		public void Show()
		{
			this.isVisible = true;
		}

		/// <summary>
		///    Internal method to update the element based on transforms applied.
		/// </summary>
		public virtual void Update()
		{
			// Check size if pixel-based
			switch ( this.metricsMode )
			{
				case MetricsMode.Pixels:
					if ( OverlayManager.Instance.HasViewportChanged || this.isGeomPositionsOutOfDate )
					{
						var oMgr = OverlayManager.Instance;
						float vpWidth = oMgr.ViewportWidth;
						float vpHeight = oMgr.ViewportHeight;

						// cope with temporarily zero dimensions, avoid divide by zero
						vpWidth = vpWidth == 0.0f ? 1.0f : vpWidth;
						vpHeight = vpHeight == 0.0f ? 1.0f : vpHeight;

						this.pixelScaleX = 1.0f/vpWidth;
						this.pixelScaleY = 1.0f/vpHeight;

						this.left = this.pixelLeft*this.pixelScaleX;
						this.top = this.pixelTop*this.pixelScaleY;
						this.width = this.pixelWidth*this.pixelScaleX;
						this.height = this.pixelHeight*this.pixelScaleY;
					}
					break;

				case MetricsMode.Relative_Aspect_Adjusted:
					if ( OverlayManager.Instance.HasViewportChanged || this.isGeomPositionsOutOfDate )
					{
						var oMgr = OverlayManager.Instance;
						float vpWidth = oMgr.ViewportWidth;
						float vpHeight = oMgr.ViewportHeight;

						// cope with temporarily zero dimensions, avoid divide by zero
						vpWidth = vpWidth == 0.0f ? 1.0f : vpWidth;
						vpHeight = vpHeight == 0.0f ? 1.0f : vpHeight;

						this.pixelScaleX = 1.0f/( 10000.0f*( vpWidth/vpHeight ) );
						this.pixelScaleY = 1.0f/10000.0f;

						this.left = this.pixelLeft*this.pixelScaleX;
						this.top = this.pixelTop*this.pixelScaleY;
						this.width = this.pixelWidth*this.pixelScaleX;
						this.height = this.pixelHeight*this.pixelScaleY;
					}
					break;
				default:
					break;
			}

			// container subclasses will update children too
			UpdateFromParent();

			// update our own position geometry
			if ( this.isGeomPositionsOutOfDate && this.isInitialized )
			{
				UpdatePositionGeometry();
				this.isGeomPositionsOutOfDate = false;
			}
			// Tell self to update own texture geometry
			if ( this.isGeomUVsOutOfDate && this.isInitialized )
			{
				UpdateTextureGeometry();
				this.isGeomUVsOutOfDate = false;
			}
		}

		/// <summary>
		/// Returns true if xy is within the constraints of the component
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		public virtual bool Contains( float x, float y )
		{
			return this.clippingRegion.Contains( (int)x, (int)y );
		}

		/// <summary>
		/// Returns true if xy is within the constraints of the component
		/// </summary>
		/// <param name="x">The x.</param>
		/// <param name="y">The y.</param>
		/// <returns></returns>
		public virtual OverlayElement FindElementAt( float x, float y )
		{
			OverlayElement ret = null;
			if ( Contains( x, y ) )
			{
				ret = this;
			}
			return ret;
		}

		/// <summary>
		///    Updates this elements transform based on it's parent.
		/// </summary>
		public virtual void UpdateFromParent()
		{
			float parentLeft, parentTop, parentBottom, parentRight;

			parentLeft = parentTop = parentBottom = parentRight = 0;

			if ( this.parent != null )
			{
				parentLeft = this.parent.DerivedLeft;
				parentTop = this.parent.DerivedTop;

				// derive right position
				if ( this.horzAlign == HorizontalAlignment.Center || this.horzAlign == HorizontalAlignment.Right )
				{
					parentRight = parentLeft + this.parent.width;
				}
				// derive bottom position
				if ( this.vertAlign == VerticalAlignment.Center || this.vertAlign == VerticalAlignment.Bottom )
				{
					parentBottom = parentTop + this.parent.height;
				}
			}
			else
			{
				// with no real parent, the "parent" is actually the full viewport size
				//                parentLeft = parentTop = 0.0f;
				//                parentRight = parentBottom = 1.0f;

				var rSys = Root.Instance.RenderSystem;
				var oMgr = OverlayManager.Instance;

				// Calculate offsets required for mapping texel origins to pixel origins in the
				// current rendersystem
				float hOffset = rSys.HorizontalTexelOffset/oMgr.ViewportWidth;
				float vOffset = rSys.VerticalTexelOffset/oMgr.ViewportHeight;

				parentLeft = 0.0f + hOffset;
				parentTop = 0.0f + vOffset;
				parentRight = 1.0f + hOffset;
				parentBottom = 1.0f + vOffset;
			}

			// sort out position based on alignment
			// all we do is derived the origin, we don't automatically sort out the position
			// This is more flexible than forcing absolute right & middle

			switch ( this.horzAlign )
			{
				case HorizontalAlignment.Center:
					this.derivedLeft = ( ( parentLeft + parentRight )*0.5f ) + this.left;
					break;

				case HorizontalAlignment.Left:
					this.derivedLeft = parentLeft + this.left;
					break;

				case HorizontalAlignment.Right:
					this.derivedLeft = parentRight + this.left;
					break;
			}

			switch ( this.vertAlign )
			{
				case VerticalAlignment.Center:
					this.derivedTop = ( ( parentTop + parentBottom )*0.5f ) + this.top;
					break;

				case VerticalAlignment.Top:
					this.derivedTop = parentTop + this.top;
					break;

				case VerticalAlignment.Bottom:
					this.derivedTop = parentBottom + this.top;
					break;
			}

			this.isDerivedOutOfDate = false;
			if ( this.parent != null )
			{
				Rectangle parentRect;

				parentRect = this.parent.ClippingRegion;

				var childRect = new Rectangle( (long)this.derivedLeft, (long)this.derivedTop,
				                               (long)( this.derivedLeft + this.width ),
				                               (long)( this.derivedTop + this.height ) );

				this.clippingRegion = Rectangle.Intersect( parentRect, childRect );
			}
			else
			{
				this.clippingRegion = new Rectangle( (long)this.derivedLeft, (long)this.derivedTop,
				                                     (long)( this.derivedLeft + this.width ),
				                                     (long)( this.derivedTop + this.height ) );
			}
		}

		/// <summary>
		/// Sets the left of this element in relation to the screen (where 1.0 = screen width)
		/// </summary>
		/// <param name="left"></param>
		/// <ogreequivilent>_setLeft</ogreequivilent>
		public void ScreenLeft( float left )
		{
			this.left = left;
			this.pixelLeft = left/this.pixelScaleX;

			this.isDerivedOutOfDate = true;
			PositionsOutOfDate();
		}

		/// <summary>
		/// Sets the top of this element in relation to the screen (where 1.0 = screen width)
		/// </summary>
		/// <param name="top"></param>
		/// <ogreequivilent>_setTop</ogreequivilent>
		public void ScreenTop( float top )
		{
			this.top = top;
			this.pixelTop = top/this.pixelScaleY;

			this.isDerivedOutOfDate = true;
			PositionsOutOfDate();
		}

		/// <summary>
		/// Sets the width of this element in relation to the screen (where 1.0 = screen width)
		/// </summary>
		/// <param name="width"></param>
		/// <ogreequivilent>_setWidth</ogreequivilent>
		public void ScreenWidth( float width )
		{
			this.width = width;
			this.pixelWidth = width/this.pixelScaleX;

			this.isDerivedOutOfDate = true;
			PositionsOutOfDate();
		}

		/// <summary>
		/// Sets the height of this element in relation to the screen (where 1.0 = screen width)
		/// </summary>
		/// <param name="height"></param>
		/// <ogreequivilent>_setHeight</ogreequivilent>
		public void ScreenHeight( float height )
		{
			this.height = height;
			this.pixelHeight = height/this.pixelScaleY;

			this.isDerivedOutOfDate = true;
			PositionsOutOfDate();
		}

		/// <summary>
		/// Sets the left and top of this element in relation to the screen (where 1.0 = screen width)
		/// </summary>
		/// <param name="left"></param>
		/// <param name="top"></param>
		/// <ogreequivilent>_setPosition</ogreequivilent>
		public void ScreenPosition( float left, float top )
		{
			this.left = left;
			this.top = top;
			this.pixelLeft = left/this.pixelScaleX;
			this.pixelTop = top/this.pixelScaleY;

			this.isDerivedOutOfDate = true;
			PositionsOutOfDate();
		}

		/// <summary>
		/// Sets the width and height of this element in relation to the screen (where 1.0 = screen width)
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <ogreequivilent>_setDimensions</ogreequivilent>
		public void ScreenDimensions( float width, float height )
		{
			this.width = width;
			this.height = height;
			this.pixelWidth = width/this.pixelScaleX;
			this.pixelHeight = height/this.pixelScaleY;

			this.isDerivedOutOfDate = true;
			PositionsOutOfDate();
		}

		/// <summary>
		///    Internal method which is triggered when the positions of the element get updated,
		///    meaning the element should be rebuilding it's mesh positions. Abstract since
		///    subclasses must implement this.
		/// </summary>
		protected abstract void UpdatePositionGeometry();

		/// <summary>
		/// Internal method which is triggered when the UVs of the element get updated,
		/// meaning the element should be rebuilding it's mesh UVs. Abstract since
		/// subclasses must implement this.
		/// </summary>
		protected abstract void UpdateTextureGeometry();

		/// <summary>
		///    Internal method to put the contents onto the render queue.
		/// </summary>
		/// <param name="queue">Current render queue.</param>
		public virtual void UpdateRenderQueue( RenderQueue queue )
		{
			if ( this.isVisible )
			{
				queue.AddRenderable( this, (ushort)this.zOrder, RenderQueueGroupID.Overlay );
			}
		}

		#endregion Methods

		#region Properties

		/// <summary>
		/// Usefuel to hold custom userdata.
		/// </summary>
		public object UserData { get; set; }

		/// <summary>
		/// Gets the SourceTemplate for this element
		/// </summary>
		public OverlayElement SourceTemplate
		{
			get
			{
				return this.sourceTemplate;
			}
		}

		/// <summary>
		///    Sets the color on elements that support it.
		/// </summary>
		/// <remarks>
		///    Note that not all elements support this, but it is still a relevant base class property.
		/// </remarks>
		public virtual ColorEx Color
		{
			get
			{
				return this.color;
			}
			set
			{
				this.color = value;
			}
		}

		/// <summary>
		///    Gets the 'left' position as derived from own left and that of parents.
		/// </summary>
		public virtual float DerivedLeft
		{
			get
			{
				if ( this.isDerivedOutOfDate )
				{
					UpdateFromParent();
				}
				return this.derivedLeft;
			}
		}

		/// <summary>
		///    Gets the 'top' position as derived from own top and that of parents.
		/// </summary>
		public virtual float DerivedTop
		{
			get
			{
				if ( this.isDerivedOutOfDate )
				{
					UpdateFromParent();
				}
				return this.derivedTop;
			}
		}

		/// <summary>
		///    Gets/Sets whether or not this element is enabled.
		/// </summary>
		public bool Enabled
		{
			get
			{
				return this.isEnabled;
			}
			set
			{
				this.isEnabled = value;
			}
		}

		/// <summary>
		///    Gets/Sets the height of this element.
		/// </summary>
		public float Height
		{
			get
			{
				if ( this.metricsMode != MetricsMode.Relative )
				{
					return this.pixelHeight;
				}
				else
				{
					return this.height;
				}
			}
			set
			{
				if ( this.metricsMode != MetricsMode.Relative )
				{
					this.pixelHeight = (int)value;
				}
				else
				{
					this.height = value;
				}
				this.isDerivedOutOfDate = true;
				PositionsOutOfDate();
			}
		}

		/// <summary>
		///    Gets/Sets the horizontal origin for this element.
		/// </summary>
		/// <remarks>
		///    By default, the horizontal origin for a OverlayElement is the left edge of the parent container
		///    (or the screen if this is a root element). You can alter this by using this property, which is
		///    especially useful when you want to use pixel-based metrics (see MetricsMode) since in this
		///    mode you can't use relative positioning.
		///    <p/>
		///    For example, if you were using Pixels metrics mode, and you wanted to place a 30x30 pixel
		///    crosshair in the center of the screen, you would use Center with a 'left' property of -15.
		///    <p/>
		///    Note that neither Center nor Right alter the position of the element based
		///    on it's width, you have to alter the 'left' to a negative number to do that; all this
		///    does is establish the origin. This is because this way you can align multiple things
		///    in the center and right with different 'left' offsets for maximum flexibility.
		/// </remarks>
		public virtual HorizontalAlignment HorizontalAlignment
		{
			get
			{
				return this.horzAlign;
			}
			set
			{
				this.horzAlign = value;
				PositionsOutOfDate();
			}
		}

		/// <summary>
		///    Gets whether or not this element is a container type.
		/// </summary>
		public virtual bool IsContainer
		{
			get
			{
				return false;
			}
		}

		/// <summary>
		///    Gets/Sets whether or not this element can be cloned.
		/// </summary>
		public virtual bool IsCloneable
		{
			get
			{
				return this.isCloneable;
			}
			set
			{
				this.isCloneable = value;
			}
		}

		/// <summary>
		///    Returns whether or not this element is currently visible.
		/// </summary>
		public bool IsVisible
		{
			get
			{
				return this.isVisible;
			}
			set
			{
				this.isVisible = value;
			}
		}

		/// <summary>
		///    Gets/Sets the left position of this element.
		/// </summary>
		public float Left
		{
			get
			{
				if ( this.metricsMode != MetricsMode.Relative )
				{
					return this.pixelLeft;
				}
				else
				{
					return this.left;
				}
			}
			set
			{
				if ( this.metricsMode != MetricsMode.Relative )
				{
					this.pixelLeft = (int)value;
				}
				else
				{
					this.left = value;
				}

				this.isDerivedOutOfDate = true;
				PositionsOutOfDate();
			}
		}

		/// <summary>
		///    Gets/Sets the name of the material in use by this element.
		/// </summary>
		public virtual string MaterialName
		{
			get
			{
				return this.materialName;
			}
			set
			{
				this.materialName = value;
				this.material = (Material)MaterialManager.Instance[ this.materialName ];

				if ( this.material == null )
				{
					throw new Exception( string.Format( "Could not find material '{0}'.", this.materialName ) );
				}

				if ( !this.material.IsLoaded )
				{
					this.material.Load();
				}


				// Set some prerequisites to be sure
				this.material.Lighting = false;
				this.material.DepthCheck = false;
			}
		}

		/// <summary>
		///    Tells this element how to interpret the position and dimension values it is given.
		/// </summary>
		/// <remarks>
		///    By default, OverlayElements are positioned and sized according to relative dimensions
		///    of the screen. This is to ensure portability between different resolutions when you
		///    want things to be positioned and sized the same way across all resolutions. However,
		///    sometimes you want things to be sized according to fixed pixels. In order to do this,
		///    you can call this method with the parameter Pixels. Note that if you then want
		///    to place your element relative to the center, right or bottom of it's parent, you will
		///    need to use the HorizontalAlignment and VerticalAlignment properties.
		/// </remarks>
		public virtual MetricsMode MetricsMode
		{
			get
			{
				return this.metricsMode;
			}
			set
			{
				var localMetricsMode = value;
				switch ( localMetricsMode )
				{
					case MetricsMode.Pixels:
					{
						float vpWidth, vpHeight;
						var oMgr = OverlayManager.Instance;
						vpWidth = oMgr.ViewportWidth;
						vpHeight = oMgr.ViewportHeight;

						// cope with temporarily zero dimensions, avoid divide by zero
						vpWidth = vpWidth == 0.0f ? 1.0f : vpWidth;
						vpHeight = vpHeight == 0.0f ? 1.0f : vpHeight;

						this.pixelScaleX = 1.0f/vpWidth;
						this.pixelScaleY = 1.0f/vpHeight;

						if ( this.metricsMode == MetricsMode.Relative )
						{
							this.pixelLeft = this.left;
							this.pixelTop = this.top;
							this.pixelWidth = this.width;
							this.pixelHeight = this.height;
						}
					}
						break;

					case MetricsMode.Relative_Aspect_Adjusted:
					{
						float vpWidth, vpHeight;
						var oMgr = OverlayManager.Instance;
						vpWidth = oMgr.ViewportWidth;
						vpHeight = oMgr.ViewportHeight;

						// cope with temporarily zero dimensions, avoid divide by zero
						vpWidth = vpWidth == 0.0f ? 1.0f : vpWidth;
						vpHeight = vpHeight == 0.0f ? 1.0f : vpHeight;

						this.pixelScaleX = 1.0f/( 10000.0f*( vpWidth/vpHeight ) );
						this.pixelScaleY = 1.0f/10000.0f;

						if ( this.metricsMode == MetricsMode.Relative )
						{
							this.pixelLeft = this.left;
							this.pixelTop = this.top;
							this.pixelWidth = this.width;
							this.pixelHeight = this.height;
						}
					}
						break;

					case MetricsMode.Relative:
						this.pixelScaleX = 1.0f;
						this.pixelScaleY = 1.0f;
						this.pixelLeft = this.left;
						this.pixelTop = this.top;
						this.pixelWidth = this.width;
						this.pixelHeight = this.height;
						break;
				}

				this.left = this.pixelLeft*this.pixelScaleX;
				this.top = this.pixelTop*this.pixelScaleY;
				this.width = this.pixelWidth*this.pixelScaleX;
				this.height = this.pixelHeight*this.pixelScaleY;

				this.metricsMode = value;
				this.isDerivedOutOfDate = true;
				PositionsOutOfDate();
			}
		}

		/// <summary>
		///    Gets the name of this element.
		/// </summary>
		public string Name
		{
			get
			{
				return this.name;
			}
		}

		/// <summary>
		/// Gets the clipping region of the element
		/// </summary>
		public virtual Rectangle ClippingRegion
		{
			get
			{
				if ( this.isDerivedOutOfDate )
				{
					UpdateFromParent();
				}
				return this.clippingRegion;
			}
		}

		/// <summary>
		///    Gets the parent container of this element.
		/// </summary>
		public OverlayElementContainer Parent
		{
			get
			{
				return this.parent;
			}
			set
			{
				this.parent = value;
			}
		}

		/// <summary>
		///    Sets the caption on elements that support it.
		/// </summary>
		/// <remarks>
		///    Not all elements support this, but it is still a relevant base class property.
		/// </remarks>
		///<ogreequivilent>getCaption</ogreequivilent>
		public virtual string Text
		{
			get
			{
				return this.text;
			}
			set
			{
				this.text = value;
				PositionsOutOfDate();
			}
		}

		/// <summary>
		///    Gets/Sets the top position of this element.
		/// </summary>
		public float Top
		{
			get
			{
				if ( this.metricsMode != MetricsMode.Relative )
				{
					return this.pixelTop;
				}
				else
				{
					return this.top;
				}
			}
			set
			{
				if ( this.metricsMode != MetricsMode.Relative )
				{
					this.pixelTop = (int)value;
				}
				else
				{
					this.top = value;
				}

				this.isDerivedOutOfDate = true;
				PositionsOutOfDate();
			}
		}

		/// <summary>
		///    Sets the vertical origin for this element.
		/// </summary>
		/// <remarks>
		///    By default, the vertical origin for a OverlayElement is the top edge of the parent container
		///    (or the screen if this is a root element). You can alter this by using this property, which is
		///    especially useful when you want to use pixel-based metrics (see MetricsMode) since in this
		///    mode you can't use relative positioning.
		///    <p/>
		///    For example, if you were using Pixels metrics mode, and you wanted to place a 30x30 pixel
		///    crosshair in the center of the screen, you would use Center with a 'top' property of -15.
		///    <p/>
		///    Note that neither Center or Bottom alter the position of the element based
		///    on it's height, you have to alter the 'top' to a negative number to do that; all this
		///    does is establish the origin. This is because this way you can align multiple things
		///    in the center and bottom with different 'top' offsets for maximum flexibility.
		/// </remarks>
		public virtual VerticalAlignment VerticalAlignment
		{
			get
			{
				return this.vertAlign;
			}
			set
			{
				this.vertAlign = value;
				PositionsOutOfDate();
			}
		}

		/// <summary>
		///    Gets/Sets the width of this element.
		/// </summary>
		public float Width
		{
			get
			{
				if ( this.metricsMode != MetricsMode.Relative )
				{
					return this.pixelWidth;
				}
				else
				{
					return this.width;
				}
			}
			set
			{
				if ( this.metricsMode != MetricsMode.Relative )
				{
					this.pixelWidth = (int)value;
				}
				else
				{
					this.width = value;
				}
				this.isDerivedOutOfDate = true;
				PositionsOutOfDate();
			}
		}

		/// <summary>
		///    Gets the z ordering of this element.
		/// </summary>
		public int ZOrder
		{
			get
			{
				return this.zOrder;
			}
		}

		#endregion Properties

		#region IRenderable Members

		public bool CastsShadows
		{
			get
			{
				return false;
			}
		}

		public Material Material
		{
			get
			{
				return this.material;
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
				return this.material.GetBestTechnique();
			}
		}

		protected RenderOperation renderOperation = new RenderOperation();

		/// <summary>
		///
		/// </summary>
		/// <param name="value"></param>
		public virtual RenderOperation RenderOperation
		{
			get
			{
				return this.renderOperation;
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="matrices"></param>
		public void GetWorldTransforms( Matrix4[] matrices )
		{
			this.overlay.GetWorldTransforms( matrices );
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public Quaternion GetWorldOrientation()
		{
			return this.overlay.GetWorldOrientation();
		}

		public Vector3 GetWorldPosition()
		{
			return this.overlay.GetWorldPosition();
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
				return true;
			}
		}

		/// <summary>
		///
		/// </summary>
		public bool UseIdentityView
		{
			get
			{
				return true;
			}
		}

		public virtual bool PolygonModeOverrideable
		{
			get
			{
				return false;
			}
		}

		/// <summary>
		///    Implementation of IRenderable.
		/// </summary>
		/// <param name="camera"></param>
		/// <returns></returns>
		public Real GetSquaredViewDepth( Camera camera )
		{
			return 10000 - ZOrder;
		}

		/// <summary>
		///
		/// </summary>
		public Quaternion WorldOrientation
		{
			get
			{
				return this.overlay.DerivedOrientation;
			}
		}

		/// <summary>
		///
		/// </summary>
		public Vector3 WorldPosition
		{
			get
			{
				return this.overlay.DerivedPosition;
			}
		}

		public LightList Lights
		{
			get
			{
				return this.emptyLightList;
			}
		}

		public Vector4 GetCustomParameter( int index )
		{
			if ( this.customParams[ index ] == null )
			{
				throw new Exception( "A parameter was not found at the given index" );
			}
			else
			{
				return (Vector4)this.customParams[ index ];
			}
		}

		public void SetCustomParameter( int index, Vector4 val )
		{
			while ( this.customParams.Count <= index )
			{
				this.customParams.Add( Vector4.Zero );
			}
			this.customParams[ index ] = val;
		}

		public void UpdateCustomGpuParameter( GpuProgramParameters.AutoConstantEntry entry, GpuProgramParameters gpuParams )
		{
			if ( this.customParams[ entry.Data ] != null )
			{
				gpuParams.SetConstant( entry.PhysicalIndex, (Vector4)this.customParams[ entry.Data ] );
			}
		}

		#endregion IRenderable Members

		#region IDisposable Implementation

		/// <summary>
		/// Class level dispose method
		/// </summary>
		/// <remarks>
		/// When implementing this method in an inherited class the following template should be used;
		/// protected override void dispose( bool disposeManagedResources )
		/// {
		/// 	if ( !isDisposed )
		/// 	{
		/// 		if ( disposeManagedResources )
		/// 		{
		/// 			// Dispose managed resources.
		/// 		}
		///
		/// 		// There are no unmanaged resources to release, but
		/// 		// if we add them, they need to be released here.
		/// 	}
		///
		/// 	// If it is available, make the call to the
		/// 	// base class's Dispose(Boolean) method
		/// 	base.dispose( disposeManagedResources );
		/// }
		/// </remarks>
		/// <param name="disposeManagedResources">True if Unmanaged resources should be released.</param>
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					// Dispose managed resources.
					if ( this.renderOperation != null )
					{
						if ( !this.renderOperation.IsDisposed )
						{
							this.renderOperation.Dispose();
						}

						this.renderOperation = null;
					}
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
			}

			base.dispose( disposeManagedResources );
		}

		#endregion IDisposable Implementation

		#region ScriptableObject Interface Command Classes

		[ScriptableProperty( "metrics_mode",
			"The type of metrics to use, either 'relative' to the screen, 'pixels' or 'relative_aspect_adjusted'.",
			typeof ( OverlayElement ) )]
		public class MetricsModeAttributeCommand : IPropertyCommand
		{
			#region Implementation of IPropertyCommand<object,string>

			/// <summary>
			///    Gets the value for this command from the target object.
			/// </summary>
			/// <param name="target"></param>
			/// <returns></returns>
			public string Get( object target )
			{
				var element = target as OverlayElement;
				if ( element != null )
				{
					return ScriptEnumAttribute.GetScriptAttribute( (int)element.MetricsMode, typeof ( MetricsMode ) );
				}
				else
				{
					return String.Empty;
				}
			}

			/// <summary>
			///    Sets the value for this command on the target object.
			/// </summary>
			/// <param name="target"></param>
			/// <param name="val"></param>
			public void Set( object target, string val )
			{
				var element = target as OverlayElement;
				if ( element != null )
				{
					element.MetricsMode = (MetricsMode)ScriptEnumAttribute.Lookup( val, typeof ( MetricsMode ) );
				}
			}

			#endregion Implementation of IPropertyCommand<object,string>
		}

		[ScriptableProperty( "horz_align", "The horizontal alignment, 'left', 'right' or 'center'.", typeof ( OverlayElement )
			)]
		public class HorizontalAlignmentAttributeCommand : IPropertyCommand
		{
			#region Implementation of IPropertyCommand<object,string>

			/// <summary>
			///    Gets the value for this command from the target object.
			/// </summary>
			/// <param name="target"></param>
			/// <returns></returns>
			public string Get( object target )
			{
				var element = target as OverlayElement;
				if ( element != null )
				{
					return ScriptEnumAttribute.GetScriptAttribute( (int)element.HorizontalAlignment, typeof ( HorizontalAlignment ) );
				}
				else
				{
					return String.Empty;
				}
			}

			/// <summary>
			///    Sets the value for this command on the target object.
			/// </summary>
			/// <param name="target"></param>
			/// <param name="val"></param>
			public void Set( object target, string val )
			{
				var element = target as OverlayElement;
				if ( element != null )
				{
					element.HorizontalAlignment =
						(HorizontalAlignment)ScriptEnumAttribute.Lookup( val, typeof ( HorizontalAlignment ) );
				}
			}

			#endregion Implementation of IPropertyCommand<object,string>
		}

		[ScriptableProperty( "vert_align", "The vertical alignment, 'top', 'bottom' or 'center'.", typeof ( OverlayElement ) )
		]
		public class VerticalAlignmentAttributeCommand : IPropertyCommand
		{
			#region Implementation of IPropertyCommand<object,string>

			/// <summary>
			///    Gets the value for this command from the target object.
			/// </summary>
			/// <param name="target"></param>
			/// <returns></returns>
			public string Get( object target )
			{
				var element = target as OverlayElement;
				if ( element != null )
				{
					return ScriptEnumAttribute.GetScriptAttribute( (int)element.VerticalAlignment, typeof ( VerticalAlignment ) );
				}
				else
				{
					return String.Empty;
				}
			}

			/// <summary>
			///    Sets the value for this command on the target object.
			/// </summary>
			/// <param name="target"></param>
			/// <param name="val"></param>
			public void Set( object target, string val )
			{
				var element = target as OverlayElement;
				if ( element != null )
				{
					element.VerticalAlignment = (VerticalAlignment)ScriptEnumAttribute.Lookup( val, typeof ( VerticalAlignment ) );
				}
			}

			#endregion Implementation of IPropertyCommand<object,string>
		}

		[ScriptableProperty( "top", "The position of the top border of the gui element.", typeof ( OverlayElement ) )]
		public class TopAttributeCommand : IPropertyCommand
		{
			#region Implementation of IPropertyCommand<object,string>

			/// <summary>
			///    Gets the value for this command from the target object.
			/// </summary>
			/// <param name="target"></param>
			/// <returns></returns>
			public string Get( object target )
			{
				var element = target as OverlayElement;
				if ( element != null )
				{
					return element.Top.ToString();
				}
				else
				{
					return String.Empty;
				}
			}

			/// <summary>
			///    Sets the value for this command on the target object.
			/// </summary>
			/// <param name="target"></param>
			/// <param name="val"></param>
			public void Set( object target, string val )
			{
				var element = target as OverlayElement;
				if ( element != null )
				{
					element.Top = StringConverter.ParseFloat( val );
				}
			}

			#endregion Implementation of IPropertyCommand<object,string>
		}

		[ScriptableProperty( "left", "The position of the left border of the gui element.", typeof ( OverlayElement ) )]
		public class LeftAttributeCommand : IPropertyCommand
		{
			#region Implementation of IPropertyCommand<object,string>

			/// <summary>
			///    Gets the value for this command from the target object.
			/// </summary>
			/// <param name="target"></param>
			/// <returns></returns>
			public string Get( object target )
			{
				var element = target as OverlayElement;
				if ( element != null )
				{
					return element.Left.ToString();
				}
				else
				{
					return String.Empty;
				}
			}

			/// <summary>
			///    Sets the value for this command on the target object.
			/// </summary>
			/// <param name="target"></param>
			/// <param name="val"></param>
			public void Set( object target, string val )
			{
				var element = target as OverlayElement;
				if ( element != null )
				{
					element.Left = StringConverter.ParseFloat( val );
				}
			}

			#endregion Implementation of IPropertyCommand<object,string>
		}

		[ScriptableProperty( "width", "The width of the gui element.", typeof ( OverlayElement ) )]
		public class WidthAttributeCommand : IPropertyCommand
		{
			#region Implementation of IPropertyCommand<object,string>

			/// <summary>
			///    Gets the value for this command from the target object.
			/// </summary>
			/// <param name="target"></param>
			/// <returns></returns>
			public string Get( object target )
			{
				var element = target as OverlayElement;
				if ( element != null )
				{
					return element.Width.ToString();
				}
				else
				{
					return String.Empty;
				}
			}

			/// <summary>
			///    Sets the value for this command on the target object.
			/// </summary>
			/// <param name="target"></param>
			/// <param name="val"></param>
			public void Set( object target, string val )
			{
				var element = target as OverlayElement;
				if ( element != null )
				{
					element.Width = StringConverter.ParseFloat( val );
				}
			}

			#endregion Implementation of IPropertyCommand<object,string>
		}

		[ScriptableProperty( "height", "The height of the gui element.", typeof ( OverlayElement ) )]
		public class HeightAttributeCommand : IPropertyCommand
		{
			#region Implementation of IPropertyCommand<object,string>

			/// <summary>
			///    Gets the value for this command from the target object.
			/// </summary>
			/// <param name="target"></param>
			/// <returns></returns>
			public string Get( object target )
			{
				var element = target as OverlayElement;
				if ( element != null )
				{
					return element.Height.ToString();
				}
				else
				{
					return String.Empty;
				}
			}

			/// <summary>
			///    Sets the value for this command on the target object.
			/// </summary>
			/// <param name="target"></param>
			/// <param name="val"></param>
			public void Set( object target, string val )
			{
				var element = target as OverlayElement;
				if ( element != null )
				{
					element.Height = StringConverter.ParseFloat( val );
				}
			}

			#endregion Implementation of IPropertyCommand<object,string>
		}

		[ScriptableProperty( "visible", "Initial visibility of element, either 'true' or 'false' (default true).",
			typeof ( OverlayElement ) )]
		public class VisibleAttributeCommand : IPropertyCommand
		{
			#region Implementation of IPropertyCommand<object,string>

			/// <summary>
			///    Gets the value for this command from the target object.
			/// </summary>
			/// <param name="target"></param>
			/// <returns></returns>
			public string Get( object target )
			{
				var element = target as OverlayElement;
				if ( element != null )
				{
					return element.IsVisible.ToString();
				}
				else
				{
					return String.Empty;
				}
			}

			/// <summary>
			///    Sets the value for this command on the target object.
			/// </summary>
			/// <param name="target"></param>
			/// <param name="val"></param>
			public void Set( object target, string val )
			{
				var element = target as OverlayElement;
				if ( element != null )
				{
					element.IsVisible = StringConverter.ParseBool( val );
				}
			}

			#endregion Implementation of IPropertyCommand<object,string>
		}

		[ScriptableProperty( "caption", "The element caption, if supported.", typeof ( OverlayElement ) )]
		public class CaptionAttributeCommand : IPropertyCommand
		{
			#region Implementation of IPropertyCommand<object,string>

			/// <summary>
			///    Gets the value for this command from the target object.
			/// </summary>
			/// <param name="target"></param>
			/// <returns></returns>
			public string Get( object target )
			{
				var element = target as OverlayElement;
				if ( element != null )
				{
					return element.Text;
				}
				else
				{
					return String.Empty;
				}
			}

			/// <summary>
			///    Sets the value for this command on the target object.
			/// </summary>
			/// <param name="target"></param>
			/// <param name="val"></param>
			public void Set( object target, string val )
			{
				var element = target as OverlayElement;
				if ( element != null )
				{
					element.Text = val;
				}
			}

			#endregion Implementation of IPropertyCommand<object,string>
		}

		[ScriptableProperty( "material", "The name of the material to use.", typeof ( OverlayElement ) )]
		public class MaterialAttributeCommand : IPropertyCommand
		{
			#region Implementation of IPropertyCommand<object,string>

			/// <summary>
			///    Gets the value for this command from the target object.
			/// </summary>
			/// <param name="target"></param>
			/// <returns></returns>
			public string Get( object target )
			{
				var element = target as OverlayElement;
				if ( element != null )
				{
					return element.MaterialName;
				}
				else
				{
					return String.Empty;
				}
			}

			/// <summary>
			///    Sets the value for this command on the target object.
			/// </summary>
			/// <param name="target"></param>
			/// <param name="val"></param>
			public void Set( object target, string val )
			{
				var element = target as OverlayElement;
				if ( element != null && val != null )
				{
					element.MaterialName = val;
				}
			}

			#endregion Implementation of IPropertyCommand<object,string>
		}

		#endregion ScriptableObject Interface Command Classes
	}
}