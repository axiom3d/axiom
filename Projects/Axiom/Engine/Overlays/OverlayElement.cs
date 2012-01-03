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
		protected Matrix4[] xform = new Matrix4[ 1 ] { Matrix4.Identity };

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
			isVisible = true;
			isCloneable = true;
			left = 0.0f;
			top = 0.0f;
			width = 1.0f;
			height = 1.0f;
			metricsMode = MetricsMode.Relative;
			horzAlign = HorizontalAlignment.Left;
			vertAlign = VerticalAlignment.Top;
			pixelTop = 0.0f;
			pixelLeft = 0.0f;
			pixelWidth = 1.0f;
			pixelHeight = 1.0f;
			pixelScaleX = 1.0f;
			pixelScaleY = 1.0f;
			parent = null;
			overlay = null;
			isDerivedOutOfDate = true;
			isGeomPositionsOutOfDate = true;
			isGeomUVsOutOfDate = true;
			zOrder = 0;
			isEnabled = true;
			isInitialized = false;
			sourceTemplate = null;
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
			sourceTemplate = template;
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
			var newElement = OverlayElementManager.Instance.CreateElement( this.GetType().Name, instanceName + "/" + name );
			CopyParametersTo( newElement );

			return newElement;
		}

		/// <summary>
		///    Hides an element if it is currently visible.
		/// </summary>
		public void Hide()
		{
			isVisible = false;
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

			isDerivedOutOfDate = true;
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
			switch ( metricsMode )
			{
				case MetricsMode.Pixels:
					{
						var oMgr = OverlayManager.Instance;
						float vpWidth = oMgr.ViewportWidth;
						float vpHeight = oMgr.ViewportHeight;

						// cope with temporarily zero dimensions, avoid divide by zero
						vpWidth = vpWidth == 0.0f ? 1.0f : vpWidth;
						vpHeight = vpHeight == 0.0f ? 1.0f : vpHeight;

						pixelScaleX = 1.0f / vpWidth;
						pixelScaleY = 1.0f / vpHeight;
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

						pixelScaleX = 1.0f / ( 10000.0f * ( vpWidth / vpHeight ) );
						pixelScaleY = 1.0f / 10000.0f;
					}
					break;

				case MetricsMode.Relative:
					pixelScaleX = 1.0f;
					pixelScaleY = 1.0f;
					pixelLeft = left;
					pixelTop = top;
					pixelWidth = width;
					pixelHeight = height;
					break;
			}

			left = pixelLeft * pixelScaleX;
			top = pixelTop * pixelScaleY;
			width = pixelWidth * pixelScaleX;
			height = pixelHeight * pixelScaleY;

			isGeomPositionsOutOfDate = true;
		}

		/// <summary>
		///    Tells this element to recaculate it's position.
		/// </summary>
		public virtual void PositionsOutOfDate()
		{
			isGeomPositionsOutOfDate = true;
		}

		/// <summary>
		/// Sets the dimensions.
		/// </summary>
		/// <param name="width">The width.</param>
		/// <param name="height">The height.</param>
		public void SetDimensions( float width, float height )
		{
			if ( metricsMode != MetricsMode.Relative )
			{
				pixelWidth = (int)width;
				pixelHeight = (int)height;
			}
			else
			{
				this.width = width;
				this.height = height;
			}

			isDerivedOutOfDate = true;
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
			this.Properties[ param ] = val;
			return true;
		}

		/// <summary>
		/// Sets the position of this element.
		/// </summary>
		/// <param name="left">The left.</param>
		/// <param name="top">The top.</param>
		public void SetPosition( float left, float top )
		{
			if ( metricsMode != MetricsMode.Relative )
			{
				pixelLeft = (int)left;
				pixelTop = (int)top;
			}
			else
			{
				this.left = left;
				this.top = top;
			}

			isDerivedOutOfDate = true;
			PositionsOutOfDate();
		}

		/// <summary>
		///    Shows this element if it was previously hidden.
		/// </summary>
		public void Show()
		{
			isVisible = true;
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
					if ( OverlayManager.Instance.HasViewportChanged || isGeomPositionsOutOfDate )
					{
						var oMgr = OverlayManager.Instance;
						float vpWidth = oMgr.ViewportWidth;
						float vpHeight = oMgr.ViewportHeight;

						// cope with temporarily zero dimensions, avoid divide by zero
						vpWidth = vpWidth == 0.0f ? 1.0f : vpWidth;
						vpHeight = vpHeight == 0.0f ? 1.0f : vpHeight;

						pixelScaleX = 1.0f / vpWidth;
						pixelScaleY = 1.0f / vpHeight;

						left = pixelLeft * pixelScaleX;
						top = pixelTop * pixelScaleY;
						width = pixelWidth * pixelScaleX;
						height = pixelHeight * pixelScaleY;
					}
					break;

				case MetricsMode.Relative_Aspect_Adjusted:
					if ( OverlayManager.Instance.HasViewportChanged || isGeomPositionsOutOfDate )
					{
						var oMgr = OverlayManager.Instance;
						float vpWidth = oMgr.ViewportWidth;
						float vpHeight = oMgr.ViewportHeight;

						// cope with temporarily zero dimensions, avoid divide by zero
						vpWidth = vpWidth == 0.0f ? 1.0f : vpWidth;
						vpHeight = vpHeight == 0.0f ? 1.0f : vpHeight;

						pixelScaleX = 1.0f / ( 10000.0f * ( vpWidth / vpHeight ) );
						pixelScaleY = 1.0f / 10000.0f;

						left = pixelLeft * pixelScaleX;
						top = pixelTop * pixelScaleY;
						width = pixelWidth * pixelScaleX;
						height = pixelHeight * pixelScaleY;
					}
					break;
				default:
					break;
			}

			// container subclasses will update children too
			UpdateFromParent();

			// update our own position geometry
			if ( isGeomPositionsOutOfDate && this.isInitialized )
			{
				UpdatePositionGeometry();
				isGeomPositionsOutOfDate = false;
			}
			// Tell self to update own texture geometry
			if ( isGeomUVsOutOfDate && this.isInitialized )
			{
				UpdateTextureGeometry();
				isGeomUVsOutOfDate = false;
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
			return clippingRegion.Contains( (int)x, (int)y );
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

			if ( parent != null )
			{
				parentLeft = parent.DerivedLeft;
				parentTop = parent.DerivedTop;

				// derive right position
				if ( horzAlign == HorizontalAlignment.Center || horzAlign == HorizontalAlignment.Right )
				{
					parentRight = parentLeft + parent.width;
				}
				// derive bottom position
				if ( vertAlign == VerticalAlignment.Center || vertAlign == VerticalAlignment.Bottom )
				{
					parentBottom = parentTop + parent.height;
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
				float hOffset = rSys.HorizontalTexelOffset / oMgr.ViewportWidth;
				float vOffset = rSys.VerticalTexelOffset / oMgr.ViewportHeight;

				parentLeft = 0.0f + hOffset;
				parentTop = 0.0f + vOffset;
				parentRight = 1.0f + hOffset;
				parentBottom = 1.0f + vOffset;
			}

			// sort out position based on alignment
			// all we do is derived the origin, we don't automatically sort out the position
			// This is more flexible than forcing absolute right & middle

			switch ( horzAlign )
			{
				case HorizontalAlignment.Center:
					derivedLeft = ( ( parentLeft + parentRight ) * 0.5f ) + left;
					break;

				case HorizontalAlignment.Left:
					derivedLeft = parentLeft + left;
					break;

				case HorizontalAlignment.Right:
					derivedLeft = parentRight + left;
					break;
			}

			switch ( vertAlign )
			{
				case VerticalAlignment.Center:
					derivedTop = ( ( parentTop + parentBottom ) * 0.5f ) + top;
					break;

				case VerticalAlignment.Top:
					derivedTop = parentTop + top;
					break;

				case VerticalAlignment.Bottom:
					derivedTop = parentBottom + top;
					break;
			}

			isDerivedOutOfDate = false;
			if ( parent != null )
			{
				Rectangle parentRect;

				parentRect = parent.ClippingRegion;

				var childRect = new Rectangle( (long)derivedLeft, (long)derivedTop, (long)( derivedLeft + width ), (long)( derivedTop + height ) );

				this.clippingRegion = Rectangle.Intersect( parentRect, childRect );
			}
			else
			{
				clippingRegion = new Rectangle( (long)derivedLeft, (long)derivedTop, (long)( derivedLeft + width ), (long)( derivedTop + height ) );
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
			pixelLeft = left / pixelScaleX;

			isDerivedOutOfDate = true;
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
			pixelTop = top / pixelScaleY;

			isDerivedOutOfDate = true;
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
			pixelWidth = width / pixelScaleX;

			isDerivedOutOfDate = true;
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
			pixelHeight = height / pixelScaleY;

			isDerivedOutOfDate = true;
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
			pixelLeft = left / pixelScaleX;
			pixelTop = top / pixelScaleY;

			isDerivedOutOfDate = true;
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
			pixelWidth = width / pixelScaleX;
			pixelHeight = height / pixelScaleY;

			isDerivedOutOfDate = true;
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
			if ( isVisible )
			{
				queue.AddRenderable( this, (ushort)zOrder, RenderQueueGroupID.Overlay );
			}
		}

		#endregion Methods

		#region Properties

		/// <summary>
		/// Usefuel to hold custom userdata.
		/// </summary>
		public object UserData
		{
			get;
			set;
		}
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
				return color;
			}
			set
			{
				color = value;
			}
		}

		/// <summary>
		///    Gets the 'left' position as derived from own left and that of parents.
		/// </summary>
		public virtual float DerivedLeft
		{
			get
			{
				if ( isDerivedOutOfDate )
				{
					UpdateFromParent();
				}
				return derivedLeft;
			}
		}

		/// <summary>
		///    Gets the 'top' position as derived from own top and that of parents.
		/// </summary>
		public virtual float DerivedTop
		{
			get
			{
				if ( isDerivedOutOfDate )
				{
					UpdateFromParent();
				}
				return derivedTop;
			}
		}

		/// <summary>
		///    Gets/Sets whether or not this element is enabled.
		/// </summary>
		public bool Enabled
		{
			get
			{
				return isEnabled;
			}
			set
			{
				isEnabled = value;
			}
		}

		/// <summary>
		///    Gets/Sets the height of this element.
		/// </summary>
		public float Height
		{
			get
			{
				if ( metricsMode != MetricsMode.Relative )
				{
					return pixelHeight;
				}
				else
				{
					return height;
				}
			}
			set
			{
				if ( metricsMode != MetricsMode.Relative )
				{
					pixelHeight = (int)value;
				}
				else
				{
					height = value;
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
				return horzAlign;
			}
			set
			{
				horzAlign = value;
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
				return isCloneable;
			}
			set
			{
				isCloneable = value;
			}
		}

		/// <summary>
		///    Returns whether or not this element is currently visible.
		/// </summary>
		public bool IsVisible
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
		///    Gets/Sets the left position of this element.
		/// </summary>
		public float Left
		{
			get
			{
				if ( metricsMode != MetricsMode.Relative )
				{
					return pixelLeft;
				}
				else
				{
					return left;
				}
			}
			set
			{
				if ( metricsMode != MetricsMode.Relative )
				{
					pixelLeft = (int)value;
				}
				else
				{
					left = value;
				}

				isDerivedOutOfDate = true;
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
				return materialName;
			}
			set
			{
				materialName = value;
				material = (Material)MaterialManager.Instance[ materialName ];

				if ( material == null )
				{
					throw new Exception( string.Format( "Could not find material '{0}'.", materialName ) );
				}

				if (!material.IsLoaded)
					material.Load();


				// Set some prerequisites to be sure
				material.Lighting = false;
				material.DepthCheck = false;
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
				return metricsMode;
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

							pixelScaleX = 1.0f / vpWidth;
							pixelScaleY = 1.0f / vpHeight;

							if ( metricsMode == MetricsMode.Relative )
							{
								pixelLeft = left;
								pixelTop = top;
								pixelWidth = width;
								pixelHeight = height;
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

							pixelScaleX = 1.0f / ( 10000.0f * ( vpWidth / vpHeight ) );
							pixelScaleY = 1.0f / 10000.0f;

							if ( metricsMode == MetricsMode.Relative )
							{
								pixelLeft = left;
								pixelTop = top;
								pixelWidth = width;
								pixelHeight = height;
							}
						}
						break;

					case MetricsMode.Relative:
						pixelScaleX = 1.0f;
						pixelScaleY = 1.0f;
						pixelLeft = left;
						pixelTop = top;
						pixelWidth = width;
						pixelHeight = height;
						break;
				}

				left = pixelLeft * pixelScaleX;
				top = pixelTop * pixelScaleY;
				width = pixelWidth * pixelScaleX;
				height = pixelHeight * pixelScaleY;

				metricsMode = value;
				isDerivedOutOfDate = true;
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
				return name;
			}
		}

		/// <summary>
		/// Gets the clipping region of the element
		/// </summary>
		public virtual Rectangle ClippingRegion
		{
			get
			{
				if ( isDerivedOutOfDate )
				{
					UpdateFromParent();
				}
				return clippingRegion;
			}
		}

		/// <summary>
		///    Gets the parent container of this element.
		/// </summary>
		public OverlayElementContainer Parent
		{
			get
			{
				return parent;
			}
			set
			{
				parent = value;
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
				return text;
			}
			set
			{
				text = value;
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
				if ( metricsMode != MetricsMode.Relative )
				{
					return pixelTop;
				}
				else
				{
					return top;
				}
			}
			set
			{
				if ( metricsMode != MetricsMode.Relative )
				{
					pixelTop = (int)value;
				}
				else
				{
					top = value;
				}

				isDerivedOutOfDate = true;
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
				return vertAlign;
			}
			set
			{
				vertAlign = value;
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
				if ( metricsMode != MetricsMode.Relative )
				{
					return pixelWidth;
				}
				else
				{
					return width;
				}
			}
			set
			{
				if ( metricsMode != MetricsMode.Relative )
				{
					pixelWidth = (int)value;
				}
				else
				{
					width = value;
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
				return zOrder;
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
				return material;
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
				return material.GetBestTechnique();
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
				return renderOperation;
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="matrices"></param>
		public void GetWorldTransforms( Matrix4[] matrices )
		{
			overlay.GetWorldTransforms( matrices );
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public Quaternion GetWorldOrientation()
		{
			return overlay.GetWorldOrientation();
		}

		public Vector3 GetWorldPosition()
		{
			return overlay.GetWorldPosition();
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
			return 10000 - this.ZOrder;
		}

		/// <summary>
		///
		/// </summary>
		public Quaternion WorldOrientation
		{
			get
			{
				return overlay.DerivedOrientation;
			}
		}

		/// <summary>
		///
		/// </summary>
		public Vector3 WorldPosition
		{
			get
			{
				return overlay.DerivedPosition;
			}
		}

		public LightList Lights
		{
			get
			{
				return emptyLightList;
			}
		}

		public Vector4 GetCustomParameter( int index )
		{
			if ( customParams[ index ] == null )
			{
				throw new Exception( "A parameter was not found at the given index" );
			}
			else
			{
				return (Vector4)customParams[ index ];
			}
		}

		public void SetCustomParameter( int index, Vector4 val )
		{
			while ( customParams.Count <= index )
				customParams.Add( Vector4.Zero );
			customParams[ index ] = val;
		}

		public void UpdateCustomGpuParameter( GpuProgramParameters.AutoConstantEntry entry, GpuProgramParameters gpuParams )
		{
			if ( customParams[ entry.Data ] != null )
			{
				gpuParams.SetConstant( entry.PhysicalIndex, (Vector4)customParams[ entry.Data ] );
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
					if ( renderOperation != null )
					{
						if (!renderOperation.IsDisposed)
							renderOperation.Dispose();

						renderOperation = null;
					}
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
			}

			base.dispose(disposeManagedResources);
		}

		#endregion IDisposable Implementation

		#region ScriptableObject Interface Command Classes

		[ScriptableProperty( "metrics_mode", "The type of metrics to use, either 'relative' to the screen, 'pixels' or 'relative_aspect_adjusted'.", typeof( OverlayElement ) )]
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
					return ScriptEnumAttribute.GetScriptAttribute( (int)element.MetricsMode, typeof( MetricsMode ) );
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
					element.MetricsMode = (MetricsMode)ScriptEnumAttribute.Lookup( val, typeof( MetricsMode ) );
				}
			}

			#endregion Implementation of IPropertyCommand<object,string>
		}

		[ScriptableProperty( "horz_align", "The horizontal alignment, 'left', 'right' or 'center'.", typeof( OverlayElement ) )]
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
					return ScriptEnumAttribute.GetScriptAttribute( (int)element.HorizontalAlignment, typeof( HorizontalAlignment ) );
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
					element.HorizontalAlignment = (HorizontalAlignment)ScriptEnumAttribute.Lookup( val, typeof( HorizontalAlignment ) );
				}
			}

			#endregion Implementation of IPropertyCommand<object,string>
		}

		[ScriptableProperty( "vert_align", "The vertical alignment, 'top', 'bottom' or 'center'.", typeof( OverlayElement ) )]
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
					return ScriptEnumAttribute.GetScriptAttribute( (int)element.VerticalAlignment, typeof( VerticalAlignment ) );
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
					element.VerticalAlignment = (VerticalAlignment)ScriptEnumAttribute.Lookup( val, typeof( VerticalAlignment ) );
				}
			}

			#endregion Implementation of IPropertyCommand<object,string>
		}

		[ScriptableProperty( "top", "The position of the top border of the gui element.", typeof( OverlayElement ) )]
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

		[ScriptableProperty( "left", "The position of the left border of the gui element.", typeof( OverlayElement ) )]
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

		[ScriptableProperty( "width", "The width of the gui element.", typeof( OverlayElement ) )]
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

		[ScriptableProperty( "height", "The height of the gui element.", typeof( OverlayElement ) )]
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

		[ScriptableProperty( "visible", "Initial visibility of element, either 'true' or 'false' (default true).", typeof( OverlayElement ) )]
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

		[ScriptableProperty( "caption", "The element caption, if supported.", typeof( OverlayElement ) )]
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

		[ScriptableProperty( "material", "The name of the material to use.", typeof( OverlayElement ) )]
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