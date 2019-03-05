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
using static Axiom.Math.Utility;

#endregion

namespace Axiom.Graphics
{
	public class RibbonTrail : BillboardChain
	{
		#region Nested type: TimeControllerValue

		public class TimeControllerValue : IControllerValue<Real>
		{
			protected RibbonTrail trail;

			public TimeControllerValue( RibbonTrail trail )
			{
				this.trail = trail;
			}

			#region IControllerValue<Real> Members

			public Real Value
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

		private readonly List<ColorEx> deltaColor = new List<ColorEx>();

		private readonly List<Real> deltaWidth = new List<Real>();
		private Real elementLength;

		private Controller<Real> fadeController;
		private readonly List<ColorEx> initialColor = new List<ColorEx>();
		private readonly List<Real> initialWidth = new List<Real>();
		private readonly List<Node> nodeList = new List<Node>();
		private Real squaredElementLength;
		private readonly IControllerValue<Real> timeControllerValue;
		private Real trailLength;

		#endregion

		#region Constructors

		public RibbonTrail( string name, int maxElements, int numberOfChains, bool useTextureCoords, bool useColors,
		                    bool dynamic )
			: base( name, maxElements, 0, useTextureCoords, useColors, dynamic )
		{
			this.fadeController = null;
			this.timeControllerValue = new TimeControllerValue( this );

			TrailLength = 100;
			NumberOfChains = numberOfChains;

			// use V as varying texture coord, so we can use 1D textures to 'smear'
			TextureCoordDirection = TexCoordDirection.V;
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

		public virtual Real TrailLength
		{
			get
			{
				return this.trailLength;
			}
			set
			{
				this.trailLength = value;
				this.elementLength = this.trailLength/maxElementsPerChain;
				this.squaredElementLength = this.elementLength*this.elementLength;
			}
		}

		#endregion

		#region Public Virtual Methods

		public virtual void AddNode( Node node )
		{
			if ( this.nodeList.Count == NumberOfChains )
			{
				throw new InvalidOperationException( "Cannot monitor any more nodes, chain count exceeded." );
			}
			var segmentIndex = this.nodeList.Count;
			var segment = chainSegmentList[ segmentIndex ];

			// setup this segment
			segment.head = segment.tail = SEGMENT_EMPTY;
			// Create new element, v coord is always 0.0f
			var e = new Element( node.DerivedPosition, this.initialWidth[ segmentIndex ], 0.0f, this.initialColor[ segmentIndex ] );
			// Add the start position
			AddChainElement( segmentIndex, e );
			e = new Element( node.DerivedPosition, this.initialWidth[ segmentIndex ], 0.0f, this.initialColor[ segmentIndex ] );
			// Add another on the same spot, this will extend
			AddChainElement( segmentIndex, e );

			this.nodeList.Add( node );
			node.NodeUpdated += new NodeUpdated( NodeUpdated );
			node.NodeDestroyed += new NodeDestroyed( NodeDestroyed );
		}

		public virtual void RemoveNode( Node node )
		{
			this.nodeList.Remove( node );
			node.NodeUpdated -= new NodeUpdated( NodeUpdated );
			node.NodeDestroyed -= new NodeDestroyed( NodeDestroyed );
		}

		public virtual IEnumerator<Node> GetEnumerator()
		{
			return this.nodeList.GetEnumerator();
		}

		public virtual void SetInitialColor( int chainIndex, ColorEx color )
		{
			if ( chainIndex > chainCount )
			{
				throw new IndexOutOfRangeException();
			}
			this.initialColor[ chainIndex ] = color;
		}

		public virtual ColorEx GetInitialColor( int chainIndex )
		{
			if ( chainIndex > chainCount )
			{
				throw new IndexOutOfRangeException();
			}
			return this.initialColor[ chainIndex ];
		}

		public virtual void SetColorChange( int chainIndex, ColorEx valuePerSecond )
		{
			if ( chainIndex > chainCount )
			{
				throw new IndexOutOfRangeException();
			}
			this.deltaColor[ chainIndex ] = valuePerSecond;
			ManageController();
		}

		public virtual ColorEx GetColorChange( int chainIndex )
		{
			if ( chainIndex > chainCount )
			{
				throw new IndexOutOfRangeException();
			}
			return this.deltaColor[ chainIndex ];
		}

		public virtual void SetInitialWidth( int chainIndex, Real width )
		{
			if ( chainIndex > chainCount )
			{
				throw new IndexOutOfRangeException();
			}
			this.initialWidth[ chainIndex ] = width;
		}

		public virtual Real GetInitialWidth( int chainIndex )
		{
			if ( chainIndex > chainCount )
			{
				throw new IndexOutOfRangeException();
			}
			return this.initialWidth[ chainIndex ];
		}

		public virtual void SetWidthChange( int chainIndex, Real valuePerSecond )
		{
			if ( chainIndex > chainCount )
			{
				throw new IndexOutOfRangeException();
			}
			this.deltaWidth[ chainIndex ] = valuePerSecond;
			ManageController();
		}

		public virtual Real GetWidthChange( int chainIndex )
		{
			if ( chainIndex > chainCount )
			{
				throw new IndexOutOfRangeException();
			}
			return this.deltaWidth[ chainIndex ];
		}

		public virtual void TimeUpdate( Real time )
		{
			// Apply all segment effects
			for ( var s = 0; s < chainSegmentList.Count; ++s )
			{
				var segment = chainSegmentList[ s ];
				if ( segment.head != SEGMENT_EMPTY && segment.head != segment.tail )
				{
					for ( var e = segment.head + 1;; ++e )
					{
						e = e%maxElementsPerChain;
						var element = chainElementList[ segment.start + e ];
						element.Width = element.Width - ( time*this.deltaWidth[ s ] );
						element.Width = Max( 0.0f, element.Width );
						element.Color = element.Color - ( this.deltaColor[ s ]*time );
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
			for ( var i = 0; i < chainCount; ++i )
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
				var segment = chainSegmentList[ index ];
				var headElement = chainElementList[ segment.start + segment.head ];
				var nextElemIndex = segment.head + 1;
				//wrap
				if ( nextElemIndex == maxElementsPerChain )
				{
					nextElemIndex = 0;
				}
				var nextElement = chainElementList[ segment.start + nextElemIndex ];

				// Vary the head elem, but bake new version if that exceeds element len
				var newPos = node.DerivedPosition;
				if ( ParentNode != null )
				{
					// Transform position to ourself space
					newPos = ParentNode.DerivedOrientation.UnitInverse*( newPos - ParentNode.DerivedPosition )/ParentNode.DerivedScale;
				}
				var diff = newPos - nextElement.Position;
				float sqlen = diff.LengthSquared;
				if ( sqlen >= this.squaredElementLength )
				{
					// Move existing head to elemLength
					var scaledDiff = diff*(float)( this.elementLength/Sqrt( sqlen ) );
					headElement.Position = nextElement.Position + scaledDiff;
					// Add a new element to be the new head
					var newElem = new Element( newPos, this.initialWidth[ index ], 0.0f, this.initialColor[ index ] );
					AddChainElement( index, newElem );
					// alter diff to represent new head size
					diff = newPos - newElem.Position;
					// check whether another step is needed or not
					if ( diff.LengthSquared <= this.squaredElementLength )
					{
						done = true;
					}
				}
				else
				{
					// extend existing head
					headElement.Position = newPos;
					done = true;
				}

				// Is this segment full?
				if ( ( segment.tail + 1 )%maxElementsPerChain == segment.head )
				{
					// If so, shrink tail gradually to match head extension
					var tailElement = chainElementList[ segment.start + segment.tail ];
					int preTailIndex;
					if ( segment.tail == 0 )
					{
						preTailIndex = maxElementsPerChain - 1;
					}
					else
					{
						preTailIndex = segment.tail - 1;
					}

					var preTailElement = chainElementList[ segment.start + preTailIndex ];

					// Measure tail diff from pretail to tail
					var tailDiff = tailElement.Position - preTailElement.Position;
					float tailLength = tailDiff.Length;

					if ( tailLength > 1e-06 )
					{
						float tailSize = this.elementLength - diff.Length;
						tailDiff *= tailSize/tailLength;
						tailElement.Position = preTailElement.Position + tailDiff;
					}
				}
			}

			boundsDirty = true;

			// Need to dirty the parent node, but can't do it using needUpdate() here 
			// since we're in the middle of the scene graph update (node listener), 
			// so re-entrant calls don't work. Queue.
			if ( parentNode != null )
			{
				Node.QueueNeedUpdate( parentNode );
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
					UpdateTrail( i, node );
					break;
				}
			}
		}

		public void NodeDestroyed( Node node )
		{
			RemoveNode( node );
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
				this.elementLength = this.trailLength/maxElementsPerChain;
				this.squaredElementLength = this.elementLength*this.elementLength;
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

				this.initialColor.Capacity = NumberOfChains;
				this.deltaColor.Capacity = NumberOfChains;
				this.initialWidth.Capacity = NumberOfChains;
				this.deltaWidth.Capacity = NumberOfChains;
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
		public static string TypeName = "RibbonTrail";

		public RibbonTrailFactory()
		{
			base.TypeFlag = (uint)SceneQueryTypeMask.Fx;
            base._type = TypeName;
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