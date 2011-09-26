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
using System.Collections.Generic;
using Axiom.Collections;
using Axiom.Controllers;
using Axiom.Core;
using Axiom.Math;

#endregion

namespace Axiom.Graphics
{
	public class RibbonTrail : BillboardChain
	{
		#region Nested type: TimeControllerValue

		public class TimeControllerValue : IControllerValue<float>
		{
			protected RibbonTrail trail;

			public TimeControllerValue( RibbonTrail trail )
			{
				this.trail = trail;
			}

			#region IControllerValue<float> Members

			public float Value
			{
				get
				{
					return 0.0f; // not a source
				}
				set
				{
					this.trail.TimeUpdate( value );
				}
			}

			#endregion
		}

		#endregion

		#region Fields

		private List<ColorEx> deltaColor = new List<ColorEx>();

		private List<float> deltaWidth = new List<float>();
		private float elementLength;

		private Controller<float> fadeController;
		private List<ColorEx> initialColor = new List<ColorEx>();
		private List<float> initialWidth = new List<float>();
		private List<Node> nodeList = new List<Node>();
		private float squaredElementLength;
		private IControllerValue<float> timeControllerValue;
		private float trailLength;

		#endregion

		#region Constructors

		public RibbonTrail( string name, int maxElements, int numberOfChains, bool useTextureCoords, bool useColors, bool dynamic )
			: base( name, maxElements, 0, useTextureCoords, useColors, dynamic )
		{
			this.fadeController = null;
			this.timeControllerValue = new TimeControllerValue( this );

			this.TrailLength = 100;
			this.NumberOfChains = numberOfChains;

			// use V as varying texture coord, so we can use 1D textures to 'smear'
			this.TextureCoordDirection = TexCoordDirection.V;
		}

		public RibbonTrail( string name, int maxElements, int numberOfChains, bool useTextureCoords, bool useColors )
			: this( name, maxElements, 0, useTextureCoords, useColors, true )
		{
		}

		public RibbonTrail( string name, int maxElements, int numberOfChains, bool useTextureCoords )
			: this( name, maxElements, numberOfChains, useTextureCoords, true )
		{
		}

		public RibbonTrail( string name, int maxElements, int numberOfChains )
			: this( name, maxElements, numberOfChains, true, true )
		{
		}

		public RibbonTrail( string name, int maxElements )
			: this( name, maxElements, 1, true, true )
		{
		}

		public RibbonTrail( string name )
			: this( name, 20, 1, true, true )
		{
		}

		#endregion

		#region Properties

		public virtual float TrailLength
		{
			get
			{
				return this.trailLength;
			}
			set
			{
				this.trailLength = value;
				this.elementLength = this.trailLength / this.maxElementsPerChain;
				this.squaredElementLength = this.elementLength * this.elementLength;
			}
		}

		#endregion

		#region Public Virtual Methods

		public virtual void AddNode( Node node )
		{
			if ( this.nodeList.Count == this.NumberOfChains )
			{
				throw new InvalidOperationException( "Cannot monitor any more nodes, chain count exceeded." );
			}
			var segmentIndex = this.nodeList.Count;
			var segment = this.chainSegmentList[ segmentIndex ];

			// setup this segment
			segment.head = segment.tail = SEGMENT_EMPTY;
			// Create new element, v coord is always 0.0f
			var e = new Element( node.DerivedPosition,
									 this.initialWidth[ segmentIndex ],
									 0.0f,
									 this.initialColor[ segmentIndex ] );
			// Add the start position
			this.AddChainElement( segmentIndex, e );
			e = new Element( node.DerivedPosition,
							 this.initialWidth[ segmentIndex ],
							 0.0f,
							 this.initialColor[ segmentIndex ] );
			// Add another on the same spot, this will extend
			this.AddChainElement( segmentIndex, e );

			this.nodeList.Add( node );
			node.NodeUpdated += new NodeUpdated( this.NodeUpdated );
			node.NodeDestroyed += new NodeDestroyed( this.NodeDestroyed );
		}

		public virtual void RemoveNode( Node node )
		{
			this.nodeList.Remove( node );
			node.NodeUpdated -= new NodeUpdated( this.NodeUpdated );
			node.NodeDestroyed -= new NodeDestroyed( this.NodeDestroyed );
		}

		public virtual IEnumerator<Node> GetEnumerator()
		{
			return this.nodeList.GetEnumerator();
		}

		public virtual void SetInitialColor( int chainIndex, ColorEx color )
		{
			if ( chainIndex > this.chainCount )
			{
				throw new IndexOutOfRangeException();
			}
			this.initialColor[ chainIndex ] = color;
		}

		public virtual ColorEx GetInitialColor( int chainIndex )
		{
			if ( chainIndex > this.chainCount )
			{
				throw new IndexOutOfRangeException();
			}
			return this.initialColor[ chainIndex ];
		}

		public virtual void SetColorChange( int chainIndex, ColorEx valuePerSecond )
		{
			if ( chainIndex > this.chainCount )
			{
				throw new IndexOutOfRangeException();
			}
			this.deltaColor[ chainIndex ] = valuePerSecond;
			this.ManageController();
		}

		public virtual ColorEx GetColorChange( int chainIndex )
		{
			if ( chainIndex > this.chainCount )
			{
				throw new IndexOutOfRangeException();
			}
			return this.deltaColor[ chainIndex ];
		}

		public virtual void SetInitialWidth( int chainIndex, float width )
		{
			if ( chainIndex > this.chainCount )
			{
				throw new IndexOutOfRangeException();
			}
			this.initialWidth[ chainIndex ] = width;
		}

		public virtual float GetInitialWidth( int chainIndex )
		{
			if ( chainIndex > this.chainCount )
			{
				throw new IndexOutOfRangeException();
			}
			return this.initialWidth[ chainIndex ];
		}

		public virtual void SetWidthChange( int chainIndex, float valuePerSecond )
		{
			if ( chainIndex > this.chainCount )
			{
				throw new IndexOutOfRangeException();
			}
			this.deltaWidth[ chainIndex ] = valuePerSecond;
			this.ManageController();
		}

		public virtual float GetWidthChange( int chainIndex )
		{
			if ( chainIndex > this.chainCount )
			{
				throw new IndexOutOfRangeException();
			}
			return this.deltaWidth[ chainIndex ];
		}

		public virtual void TimeUpdate( float time )
		{
			// Apply all segment effects
			for ( var s = 0; s < this.chainSegmentList.Count; ++s )
			{
				var segment = this.chainSegmentList[ s ];
				if ( segment.head != SEGMENT_EMPTY && segment.head != segment.tail )
				{
					for ( var e = segment.head + 1; ; ++e )
					{
						e = e % this.maxElementsPerChain;
						var element = this.chainElementList[ segment.start + e ];
						element.Width = element.Width - ( time * this.deltaWidth[ s ] );
						element.Width = Utility.Max( 0.0f, element.Width );
						element.Color = element.Color - ( this.deltaColor[ s ] * time );
						element.Color.Saturate();
						if ( e == segment.tail )
						{
							break;
						}
					}
				}
			}
		}

		#endregion

		#region Protected Virtual Methods

		protected virtual void ManageController()
		{
			var needController = false;
			for ( var i = 0; i < this.chainCount; ++i )
			{
				if ( this.deltaWidth[ i ] != 0 || this.deltaColor[ i ] != ColorEx.Black )
				{
					needController = true;
					break;
				}
			}

			if ( this.fadeController == null && needController )
			{
				// setup fading via frame time controller
				var mgr = ControllerManager.Instance;
				this.fadeController = mgr.CreateFrameTimePassthroughController( this.timeControllerValue );
			}
			else if ( this.fadeController != null && !needController )
			{
				this.fadeController = null;
			}
		}

		protected virtual void UpdateTrail( int index, Node node )
		{
			var done = false;
			// Repeat this entire process if chain is stretched beyond its natural length
			while ( !done )
			{
				// Node has changed somehow, we're only interested in the derived position
				var segment = this.chainSegmentList[ index ];
				var headElement = this.chainElementList[ segment.start + segment.head ];
				var nextElemIndex = segment.head + 1;
				//wrap
				if ( nextElemIndex == this.maxElementsPerChain )
				{
					nextElemIndex = 0;
				}
				var nextElement = this.chainElementList[ segment.start + nextElemIndex ];

				// Vary the head elem, but bake new version if that exceeds element len
				var newPos = node.DerivedPosition;
				if ( this.ParentNode != null )
				{
					// Transform position to ourself space
					newPos = this.ParentNode.DerivedOrientation.UnitInverse * ( newPos - this.ParentNode.DerivedPosition )
							 / this.ParentNode.DerivedScale;
				}
				var diff = newPos - nextElement.Position;
				float sqlen = diff.LengthSquared;
				if ( sqlen >= this.squaredElementLength )
				{
					// Move existing head to elemLength
					var scaledDiff = diff * (float)( this.elementLength / Utility.Sqrt( sqlen ) );
					headElement.Position = nextElement.Position + scaledDiff;
					// Add a new element to be the new head
					var newElem = new Element( newPos, this.initialWidth[ index ], 0.0f, this.initialColor[ index ] );
					this.AddChainElement( index, newElem );
					// alter diff to represent new head size
					diff = newPos - newElem.Position;
					// check whether another step is needed or not
					if ( diff.LengthSquared <= this.squaredElementLength )
						done = true;
				}
				else
				{
					// extend existing head
					headElement.Position = newPos;
					done = true;
				}

				// Is this segment full?
				if ( ( segment.tail + 1 ) % this.maxElementsPerChain == segment.head )
				{
					// If so, shrink tail gradually to match head extension
					var tailElement = this.chainElementList[ segment.start + segment.tail ];
					int preTailIndex;
					if ( segment.tail == 0 )
					{
						preTailIndex = this.maxElementsPerChain - 1;
					}
					else
					{
						preTailIndex = segment.tail - 1;
					}

					var preTailElement = this.chainElementList[ segment.start + preTailIndex ];

					// Measure tail diff from pretail to tail
					var tailDiff = tailElement.Position - preTailElement.Position;
					float tailLength = tailDiff.Length;

					if ( tailLength > 1e-06 )
					{
						float tailSize = this.elementLength - diff.Length;
						tailDiff *= tailSize / tailLength;
						tailElement.Position = preTailElement.Position + tailDiff;
					}
				}
			}

			this.boundsDirty = true;

			// Need to dirty the parent node, but can't do it using needUpdate() here 
			// since we're in the middle of the scene graph update (node listener), 
			// so re-entrant calls don't work. Queue.
			if ( this.parentNode != null )
			{
				Node.QueueNeedUpdate( this.parentNode );
			}
		}

		#endregion

		#region NodeListener Methods

		public void NodeUpdated( Node node )
		{
			for ( var i = 0; i < this.nodeList.Count; ++i )
			{
				if ( this.nodeList[ i ] == node )
				{
					this.UpdateTrail( i, node );
					break;
				}
			}
		}

		public void NodeDestroyed( Node node )
		{
			this.RemoveNode( node );
		}

		#endregion

		#region BillBoardChain overloads

		public override int MaxChainElements
		{
			get
			{
				return base.MaxChainElements;
			}
			set
			{
				base.MaxChainElements = value;
				this.elementLength = this.trailLength / this.maxElementsPerChain;
				this.squaredElementLength = this.elementLength * this.elementLength;
			}
		}

		public override int NumberOfChains
		{
			get
			{
				return base.NumberOfChains;
			}
			set
			{
				base.NumberOfChains = value;

				this.initialColor.Capacity = this.NumberOfChains;
				this.deltaColor.Capacity = this.NumberOfChains;
				this.initialWidth.Capacity = this.NumberOfChains;
				this.deltaWidth.Capacity = this.NumberOfChains;
				if ( this.initialColor.Count < this.initialColor.Capacity )
				{
					for ( var i = this.initialColor.Count; i < this.initialColor.Capacity; ++i )
					{
						this.initialColor.Add( ColorEx.White );
						this.deltaColor.Add( ColorEx.White );
						this.initialWidth.Add( 5 );
						this.deltaWidth.Add( 5 );
					}
				}
			}
		}

		#endregion

	}

	public class RibbonTrailFactory : MovableObjectFactory
	{
		public new static string TypeName = "RibbonTrail";

		public RibbonTrailFactory()
		{
			base.Type = TypeName;
			base.TypeFlag = (uint)SceneQueryTypeMask.Fx;
		}

		protected override MovableObject _createInstance( string name, NamedParameterList param )
		{
			var maxElements = 20;
			var numberOfChains = 1;
			var useTextureCoords = true;
			var useVertexColors = true;
			var isDynamic = true;

			// optional parameters
			if ( param != null )
			{
				if ( param.ContainsKey( "maxElements" ) )
				{
					maxElements = Convert.ToInt32( param[ "maxElements" ] );
				}
				if ( param.ContainsKey( "numberOfChains" ) )
				{
					numberOfChains = Convert.ToInt32( param[ "numberOfChains" ] );
				}
				if ( param.ContainsKey( "useTextureCoords" ) )
				{
					useTextureCoords = Convert.ToBoolean( param[ "useTextureCoords" ] );
				}
				if ( param.ContainsKey( "useVertexColours" ) )
				{
					useVertexColors = Convert.ToBoolean( param[ "useVertexColours" ] );
				}
				else if ( param.ContainsKey( "useVertexColors" ) )
				{
					useVertexColors = Convert.ToBoolean( param[ "useVertexColors" ] );
				}
				if ( param.ContainsKey( "isDynamic" ) )
				{
					isDynamic = Convert.ToBoolean( param[ "isDynamic" ] );
				}
			}

			return new RibbonTrail( name, maxElements, numberOfChains, useTextureCoords, useVertexColors, isDynamic );
		}

		public override void DestroyInstance( ref MovableObject obj )
		{
			obj = null;
		}
	}
}