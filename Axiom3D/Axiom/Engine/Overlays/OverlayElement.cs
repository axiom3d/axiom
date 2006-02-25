using System;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Drawing;

using Axiom.MathLib;
// This is coming from RealmForge.Utility
using Axiom.Core;
#region Ogre Synchronization Information
/// <ogresynchronization>
///     <file name="OgreOverlayElement.h"   revision="1.8" lastUpdated="10/5/2005" lastUpdatedBy="DanielH" />
///     <file name="OgreOverlayElement.cpp" revision="1.11.2.3" lastUpdated="10/5/2005" lastUpdatedBy="DanielH" />
/// </ogresynchronization>
#endregion

namespace Axiom
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
    public abstract class OverlayElement : IRenderable
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
		protected Matrix4[] xform = new Matrix4[256];

        protected bool isEnabled;

		// is element initialised
		protected bool isInitialised;

		// Used to see if this element is created from a Template
		protected OverlayElement sourceTemplate ;
        /// <summary>Parser method lookup for script parameters.</summary>
        protected Hashtable attribParsers = new Hashtable();
        protected LightList emptyLightList = new LightList();
        protected Hashtable customParams = new Hashtable();

        #endregion

        #region Constructors

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        protected internal OverlayElement( string name )
        {
            this.name = name;
            width = 1.0f;
            height = 1.0f;

            isVisible = true;
            isDerivedOutOfDate = true;
            isCloneable = true;
            metricsMode = MetricsMode.Relative;
            horzAlign = HorizontalAlignment.Left;
            vertAlign = VerticalAlignment.Top;
            isGeomPositionsOutOfDate = true;
			isGeomUVsOutOfDate = true;
            isEnabled = true;
			pixelWidth = 1.0f;
			pixelHeight = 1.0f;
			pixelScaleX = 1.0f;
			pixelScaleY = 1.0f;
			sourceTemplate = null;
			isInitialised = false;
            RegisterParsers();
        }

        #endregion

        #region Methods

        /// <summary>
        ///    Copys data from the template element to this element to clone it.
        /// </summary>
        /// <param name="template"></param>
        /// <returns></returns>
        public virtual void CopyFromTemplate( OverlayElement template )
        {
            PropertyInfo[] props = template.GetType().GetProperties();

            for ( int i = 0; i < props.Length; i++ )
            {
                PropertyInfo prop = props[i];

                // if the prop is not settable, then skip
                if ( !prop.CanWrite || !prop.CanRead )
                {
                    Console.WriteLine( prop.Name );
                    continue;
                }

                object srcVal = prop.GetValue( template, null );
                if ( srcVal != null )
                    prop.SetValue( this, srcVal, null );
            }
        }
		public OverlayElement Clone(string instanceName)
		{
			OverlayElement newElement = OverlayElementManager.Instance.CreateElement(this.GetType().Name, instanceName + "/" + name);
			//copyParametersTo(newElement);

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

			if (overlay != null && overlay.IsInitialized && !isInitialised)
			{
				Initialize();
			}

			isDerivedOutOfDate = true;
        }

        /// <summary>
        ///    Internal method to notify the element when Zorder of parent overlay
        ///    has changed.
        /// </summary>
        /// <remarks>
        ///    Overlays have explicit Z orders. OverlayElements do not, they inherit the 
        ///    ZOrder of the overlay, and the Zorder is incremented for every container
        ///    nested within this to ensure that containers are displayed behind contained
        ///    items. This method is used internally to notify the element of a change in
        ///    final zorder which is used to render the element.
        /// </remarks>
        /// <param name="zOrder"></param>
        public virtual void NotifyZOrder( int zOrder )
        {
            this.zOrder = zOrder;
        }


		public virtual void NotifyWorldTransforms(Matrix4[] xform)
		{
			this.xform = xform;
		}

		public virtual void NotifyViewport()
		{
			switch (metricsMode)
			{
				case MetricsMode.Pixels:
				{
					float vpWidth, vpHeight;
					OverlayManager oMgr = OverlayManager.Instance;
					vpWidth = (float) (oMgr.ViewportWidth);
					vpHeight = (float) (oMgr.ViewportHeight);

					pixelScaleX = (int)(1.0 / vpWidth);
					pixelScaleY = (int)(1.0 / vpHeight);
				}
					break;

				case MetricsMode.Relative_Aspect_Adjusted:
				{
					float vpWidth, vpHeight;
					OverlayManager oMgr = OverlayManager.Instance;
					vpWidth = (float) (oMgr.ViewportWidth);
					vpHeight = (float) (oMgr.ViewportHeight);

					pixelScaleX = (int)(1.0 / (10000.0 * (vpWidth / vpHeight)));
					pixelScaleY = (int)(1.0 /  10000.0);
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
        ///		Registers all attribute names with their respective parser.
        /// </summary>
        /// <remarks>
        ///		Methods meant to serve as attribute parsers should use a method attribute to 
        /// </remarks>
        protected virtual void RegisterParsers()
        {
            MethodInfo[] methods = this.GetType().GetMethods( BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Static );

            // loop through all methods and look for ones marked with attributes
            for ( int i = 0; i < methods.Length; i++ )
            {
                // get the current method in the loop
                MethodInfo method = methods[i];

                // see if the method should be used to parse one or more material attributes
                AttributeParserAttribute[] parserAtts =
                    (AttributeParserAttribute[])method.GetCustomAttributes( typeof( AttributeParserAttribute ), true );

                // loop through each one we found and register its parser
                for ( int j = 0; j < parserAtts.Length; j++ )
                {
                    AttributeParserAttribute parserAtt = parserAtts[j];

                    attribParsers.Add( parserAtt.Name, Delegate.CreateDelegate( typeof( AttributeParserMethod ), method ) );
                } // for
            } // for
        }

        /// <summary>
        ///    
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
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
            if ( !attribParsers.ContainsKey( param ) )
            {
                return false;
            }

            AttributeParserMethod parser = (AttributeParserMethod)attribParsers[param];

            // call the parser method, passing in an array of the split val param, and this element for the optional object
            // MONO: As of 1.0.5, complains if the second param is not explicitly passed as an object array
            parser( val.Split( ' ' ), new object[] { this } );

            return true;
        }

        /// <summary>
        ///    Sets the position of this element.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="top"></param>
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
			switch (this.metricsMode)
			{
				case MetricsMode.Pixels :
					if (OverlayManager.Instance.HasViewportChanged || isGeomPositionsOutOfDate)
					{
						float vpWidth, vpHeight;
						OverlayManager oMgr = OverlayManager.Instance;
						vpWidth = (float) (oMgr.ViewportWidth);
						vpHeight = (float) (oMgr.ViewportHeight);

						pixelScaleX = 1.0f / vpWidth;
						pixelScaleY = 1.0f / vpHeight;

						left = pixelLeft * pixelScaleX;
						top = pixelTop * pixelScaleY;
						width = pixelWidth * pixelScaleX;
						height = pixelHeight * pixelScaleY;
					}
					break;

				case MetricsMode.Relative_Aspect_Adjusted :
					if (OverlayManager.Instance.HasViewportChanged || isGeomPositionsOutOfDate)
					{
						float vpWidth, vpHeight;
						OverlayManager oMgr = OverlayManager.Instance;
						vpWidth = (float) (oMgr.ViewportWidth);
						vpHeight = (float) (oMgr.ViewportHeight);

						pixelScaleX = 1.0f / (10000.0f * (vpWidth / vpHeight));
						pixelScaleY = 1.0f /  10000.0f;

						left = pixelLeft * pixelScaleX;
						top = pixelTop * pixelScaleY;
						width = pixelWidth * pixelScaleX;
						height = pixelHeight * pixelScaleY;
					}
					break;
				default:
					break;
			}

			UpdateFromParent();
			// NB container subclasses will update children too

			// Tell self to update own position geometry
			if (isGeomPositionsOutOfDate && isInitialised)
			{
				UpdatePositionGeometry();
				isGeomPositionsOutOfDate = false;
			}
			// Tell self to update own texture geometry
			if (isGeomUVsOutOfDate && isInitialised)
			{

				UpdateTextureGeometry();
				isGeomPositionsOutOfDate = false;
			}
        }

		/// <summary>
		/// Internal method which is triggered when the UVs of the element get updated,
		/// meaning the element should be rebuilding it's mesh UVs. Abstract since
		/// subclasses must implement this.
		/// </summary>


		/// <summary>
		/// Returns true if xy is within the constraints of the component 
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		public virtual bool Contains(float x, float y)
		{
			return clippingRegion.Contains((int)x, (int)y);
		}

		/// <summary>
		///  Returns true if xy is within the constraints of the component
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		public virtual OverlayElement FindElementAt(float x, float y)
		{
			OverlayElement ret = null;
			if (Contains(x , y ))
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
                    parentRight = parentLeft + parent.Width;
                }
                // derive bottom position
                if ( vertAlign == VerticalAlignment.Center || vertAlign == VerticalAlignment.Bottom )
                {
                    parentBottom = parentTop + parent.Height;
                }
            }
            else
            {
                // with no real parent, the "parent" is actually the full viewport size
//                parentLeft = parentTop = 0.0f;
//                parentRight = parentBottom = 1.0f;


				RenderSystem rSys = Root.Instance.RenderSystem;
				OverlayManager oMgr = OverlayManager.Instance;

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
				Rectangle child;

				parentRect = parent.ClippingRegion;

				Rectangle childRect = new Rectangle((int)derivedLeft,(int)derivedTop,(int)width,(int)height);
//				child.Left   = derivedLeft;
//				child.Top    = derivedTop;
//				child.Right  = derivedLeft + width;
//				child.Bottom = derivedTop + height;

				this.clippingRegion = Rectangle.Intersect(parentRect, childRect);
			}
			else
			{
				clippingRegion = new Rectangle((int)derivedLeft,(int)derivedTop,(int)width,(int)height);
//				clippingRegion.Left   = derivedLeft;
//				clippingRegion.Top    = derivedTop;
//				clippingRegion.Right  = derivedLeft + width;
//				clippingRegion.Bottom = derivedTop + height;
			}
        }


		
		

		/// <summary>
		/// Sets the left of this element in relation to the screen (where 1.0 = screen width)
		/// </summary>
		/// <param name="left"></param>
		/// <ogreequivilent>_setLeft</ogreequivilent>
		public void ScreenLeft(float left)
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
		public void ScreenTop(float top)
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
		public void ScreenWidth(float width)
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
		public void ScreenHeight(float height)
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
		public void ScreenPosition(float left, float top)
		{
			this.left = left;
			this.top  = top;
			pixelLeft = left / pixelScaleX;
			pixelTop  = top / pixelScaleY;

			isDerivedOutOfDate = true;
			PositionsOutOfDate();
		}


		/// <summary>
		/// Sets the width and height of this element in relation to the screen (where 1.0 = screen width)
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <ogreequivilent>_setDimensions</ogreequivilent>
		public void ScreenDimensions(float width, float height)
		{
			this.width  = width;
			this.height = height;
			pixelWidth  = width / pixelScaleX;
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

        #endregion

        #region Properties
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
				this.isDerivedOutOfDate=true;
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
                material = MaterialManager.Instance.GetByName( materialName );

                if ( material == null )
                {
                    throw new Exception( string.Format( "Could not find material '{0}'.", materialName ) );
                }
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
                MetricsMode localMetricsMode = value;
				switch (localMetricsMode)
				{
					case MetricsMode.Pixels :
					{
						float vpWidth, vpHeight;
						OverlayManager oMgr = OverlayManager.Instance;
						vpWidth = (float) (oMgr.ViewportWidth);
						vpHeight = (float) (oMgr.ViewportHeight);

						pixelScaleX = 1.0f / vpWidth;
						pixelScaleY = 1.0f / vpHeight;

						if (metricsMode == MetricsMode.Relative)
						{
							pixelLeft = left;
							pixelTop = top;
							pixelWidth = width;
							pixelHeight = height;
						}
					}
						break;

					case MetricsMode.Relative_Aspect_Adjusted :
					{
						float vpWidth, vpHeight;
						OverlayManager oMgr = OverlayManager.Instance;
						vpWidth = (float) (oMgr.ViewportWidth);
						vpHeight = (float) (oMgr.ViewportHeight);

						pixelScaleX = 1.0f / (10000.0f * (vpWidth / vpHeight));
						pixelScaleY = 1.0f /  10000.0f;

						if (metricsMode == MetricsMode.Relative)
						{
							pixelLeft = left;
							pixelTop = top;
							pixelWidth = width;
							pixelHeight = height;
						}
					}
						break;

					case MetricsMode.Relative :
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
				if (isDerivedOutOfDate)
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
				this.isDerivedOutOfDate=true;
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

        #endregion

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

        /// <summary>
        ///    Abstract.  Force subclasses to implement this.
        /// </summary>
        /// <param name="op"></param>
        public abstract void GetRenderOperation( RenderOperation op );

        /// <summary>
        /// 
        /// </summary>
        /// <param name="matrices"></param>
        public void GetWorldTransforms( Axiom.MathLib.Matrix4[] matrices )
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
        ///    Implementation of IRenderable.
        /// </summary>
        /// <param name="camera"></param>
        /// <returns></returns>
        public float GetSquaredViewDepth( Camera camera )
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

        #region Script parser methods

        [AttributeParser( "metrics_mode", "OverlayElement" )]
        public static void ParseMetricsMode( string[] parms, params object[] objects )
        {
            OverlayElement element = (OverlayElement)objects[0];

            element.MetricsMode = (MetricsMode)ScriptEnumAttribute.Lookup( parms[0], typeof( MetricsMode ) );
        }

        [AttributeParser( "horz_align", "OverlayElement" )]
        public static void ParseHorzAlign( string[] parms, params object[] objects )
        {
            OverlayElement element = (OverlayElement)objects[0];

            element.HorizontalAlignment = (HorizontalAlignment)ScriptEnumAttribute.Lookup( parms[0], typeof( HorizontalAlignment ) );
        }

        [AttributeParser( "vert_align", "OverlayElement" )]
        public static void ParseVertAlign( string[] parms, params object[] objects )
        {
            OverlayElement element = (OverlayElement)objects[0];

            element.VerticalAlignment = (VerticalAlignment)ScriptEnumAttribute.Lookup( parms[0], typeof( VerticalAlignment ) );
        }

        [AttributeParser( "top", "OverlayElement" )]
        public static void ParseTop( string[] parms, params object[] objects )
        {
            OverlayElement element = (OverlayElement)objects[0];

            element.Top = StringConverter.ParseFloat( parms[0] );
        }

        [AttributeParser( "left", "OverlayElement" )]
        public static void ParseLeft( string[] parms, params object[] objects )
        {
            OverlayElement element = (OverlayElement)objects[0];

            element.Left = StringConverter.ParseFloat( parms[0] );
        }

        [AttributeParser( "width", "OverlayElement" )]
        public static void ParseWidth( string[] parms, params object[] objects )
        {
            OverlayElement element = (OverlayElement)objects[0];

            element.Width = StringConverter.ParseFloat( parms[0] );
        }

        [AttributeParser( "height", "OverlayElement" )]
        public static void ParseHeight( string[] parms, params object[] objects )
        {
            OverlayElement element = (OverlayElement)objects[0];

            element.Height = StringConverter.ParseFloat( parms[0] );
        }

        [AttributeParser( "caption", "OverlayElement" )]
        public static void ParseCaption( string[] parms, params object[] objects )
        {
            OverlayElement element = (OverlayElement)objects[0];

            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            // reconstruct the single string since the caption could have spaces
            for ( int i = 0; i < parms.Length; i++ )
            {
                sb.Append( parms[i] );
				if (i != parms.Length-1)
				{
					sb.Append( " " );
				}
            }

            element.Text = sb.ToString();
        }

        [AttributeParser( "material", "OverlayElement" )]
        public static void ParseMaterial( string[] parms, params object[] objects )
        {
            OverlayElement element = (OverlayElement)objects[0];

            element.MaterialName = parms[0];
        }

        #endregion Script parser methods
    }
}
