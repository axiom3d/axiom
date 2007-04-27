#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006  Axiom Project Team

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
using System.Text;

using Axiom.Core;
using Axiom.Math;
using Axiom.Controllers;

#endregion Namespace Declarations
			
namespace Axiom
{
	public class RibbonTrail : BillboardChain
	{
		public class TimeControllerValue : IControllerValue<float>
		{
			protected RibbonTrail trail;

			public TimeControllerValue(RibbonTrail trail)
			{
				this.trail = trail;
			}

			#region IControllerValue Members

			public float Value
			{
				get
				{
					return 0.0f; // not a source
				}
				set
				{
					trail.TimeUpdate(value);
				}
			}

			#endregion
		}

		#region Fields
		List<Node> nodeList = new List<Node>();
		float trailLength;
		float elementLength;
		float squaredElementLength;

		List<ColorEx> initialColor = new List<ColorEx>();
		List<ColorEx> deltaColor = new List<ColorEx>();

		List<float> initialWidth = new List<float>();
		List<float> deltaWidth = new List<float>();

		Controller<float> fadeController;
		IControllerValue<float> timeControllerValue;
		#endregion

		#region Constructors
		public RibbonTrail(string name, int maxElements, int numberOfChains, bool useTextureCoords, bool useColors)
			: base(name, maxElements, 0, useTextureCoords, useColors, true)
		{
			fadeController = null;
			timeControllerValue = new TimeControllerValue(this);

			TrailLength = 100;
			NumberOfChains = numberOfChains;

			// use V as varying texture coord, so we can use 1D textures to 'smear'
			TextureCoordDirection = TexCoordDirection.V;
		}

		public RibbonTrail(string name, int maxElements, int numberOfChains, bool useTextureCoords)
			: this(name, maxElements, numberOfChains, useTextureCoords, true)
		{
		}

		public RibbonTrail(string name, int maxElements, int numberOfChains)
			: this(name, maxElements, numberOfChains, true, true)
		{
		}

		public RibbonTrail(string name, int maxElements)
			: this(name, maxElements, 1, true, true)
		{
		}

		public RibbonTrail(string name)
			: this(name, 20, 1, true, true)
		{
		}
		#endregion

		#region Properties
		public virtual float TrailLength
		{
			get { return trailLength; }
			set
			{
				trailLength = value;
				elementLength = trailLength / maxElementsPerChain;
				squaredElementLength = elementLength * elementLength;
			}
		}
		#endregion

		#region Public Virtual Methods
		public virtual void AddNode(Node node)
		{
			if (nodeList.Count == NumberOfChains)
			{
				throw new InvalidOperationException("Cannot monitor any more nodes, chain count exceeded.");
			}
			int segmentIndex = nodeList.Count;
			ChainSegment segment = chainSegmentList[segmentIndex];

			// setup this segment
			segment.head = segment.tail = SEGMENT_EMPTY;
			// Create new element, v coord is always 0.0f
			Element e = new Element(node.DerivedPosition, initialWidth[segmentIndex], 0.0f, initialColor[segmentIndex]);
			// Add the start position
			AddChainElement(segmentIndex, e);
			e = new Element(node.DerivedPosition, initialWidth[segmentIndex], 0.0f, initialColor[segmentIndex]);
			// Add another on the same spot, this will extend
			AddChainElement(segmentIndex, e);

			nodeList.Add(node);
			node.NodeUpdated += new NodeUpdated(NodeUpdated);
			node.NodeDestroyed += new NodeDestroyed(NodeDestroyed);
		}

		public virtual void RemoveNode(Node node)
		{
			nodeList.Remove(node);
			node.NodeUpdated -= new NodeUpdated(NodeUpdated);
			node.NodeDestroyed -= new NodeDestroyed(NodeDestroyed);
		}

		public virtual IEnumerator<Node> GetEnumerator()
		{
			return nodeList.GetEnumerator();
		}

		public virtual void SetInitialColor(int chainIndex, ColorEx color)
		{
			if (chainIndex > chainCount)
			{
				throw new IndexOutOfRangeException();
			}
			initialColor[chainIndex] = color;
		}

		public virtual ColorEx GetInitialColor(int chainIndex)
		{
			if (chainIndex > chainCount)
			{
				throw new IndexOutOfRangeException();
			}
			return initialColor[chainIndex];
		}

		public virtual void SetColorChange(int chainIndex, ColorEx valuePerSecond)
		{
			if (chainIndex > chainCount)
			{
				throw new IndexOutOfRangeException();
			}
			deltaColor[chainIndex] = valuePerSecond;
			ManageController();
		}

		public virtual ColorEx GetColorChange(int chainIndex)
		{
			if (chainIndex > chainCount)
			{
				throw new IndexOutOfRangeException();
			}
			return deltaColor[chainIndex];
		}

		public virtual void SetInitialWidth(int chainIndex, float width)
		{
			if (chainIndex > chainCount)
			{
				throw new IndexOutOfRangeException();
			}
			initialWidth[chainIndex] = width;
		}

		public virtual float GetInitialWidth(int chainIndex)
		{
			if (chainIndex > chainCount)
			{
				throw new IndexOutOfRangeException();
			}
			return initialWidth[chainIndex];
		}

		public virtual void SetWidthChange(int chainIndex, float valuePerSecond)
		{
			if (chainIndex > chainCount)
			{
				throw new IndexOutOfRangeException();
			}
			deltaWidth[chainIndex] = valuePerSecond;
			ManageController();
		}

		public virtual float GetWidthChange(int chainIndex)
		{
			if (chainIndex > chainCount)
			{
				throw new IndexOutOfRangeException();
			}
			return deltaWidth[chainIndex];
		}

		public virtual void TimeUpdate(float time)
		{
			// Apply all segment effects
			for (int s = 0; s < chainSegmentList.Count; ++s)
			{
				ChainSegment segment = chainSegmentList[s];
				if (segment.head != SEGMENT_EMPTY && segment.head != segment.tail)
				{
					for (int e = segment.head + 1; ; ++e)
					{
						e = e % maxElementsPerChain;
						Element element = chainElementList[segment.start + e];
						element.Width = element.Width - (time * deltaWidth[s]);
						element.Width = Utility.Max(0.0f, element.Width);
						element.Color = element.Color - (deltaColor[s] * time);
						//element.Color.Saturate();
						if (e == segment.tail)
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
			bool needController = false;
			for (int i = 0; i < chainCount; ++i)
			{
				if (deltaWidth[i] != 0 || deltaColor[i] != ColorEx.Black)
				{
					needController = true;
					break;
				}
			}

			if (fadeController == null && needController)
			{
				// setup fading via frame time controller
				ControllerManager mgr = ControllerManager.Instance;
				fadeController = mgr.CreateFrameTimePassthroughController(timeControllerValue);
			}
			else if (fadeController != null && !needController)
			{
				// TODO: destroy controller
			}
		}

		protected virtual void UpdateTrail(int index, Node node)
		{
			// Node has changed somehow, we're only interested in the derived position
			ChainSegment segment = chainSegmentList[index];
			Element headElement = chainElementList[segment.start + segment.head];
			int nextElemIndex = segment.head + 1;
			//wrap
			if (nextElemIndex == maxElementsPerChain)
			{
				nextElemIndex = 0;
			}
			Element nextElement = chainElementList[segment.start + nextElemIndex];

			// Vary the head elem, but bake new version if that exceeds element len
			Vector3 newPos = node.DerivedPosition;
			if (ParentNode != null)
			{
				// Transform position to ourself space
				newPos = ParentNode.DerivedOrientation.UnitInverse * (newPos - ParentNode.DerivedPosition) / ParentNode.DerivedScale;
			}
			Vector3 diff = newPos - nextElement.Position;
			float sqlen = diff.LengthSquared;
			if (sqlen >= squaredElementLength)
			{
				// Move existing head to elemLength
				Vector3 scaledDiff = diff * (float)(elementLength / Utility.Sqrt(sqlen));
				headElement.Position = nextElement.Position + scaledDiff;
				// Add a new element to be the new head
				Element newElem = new Element(newPos, initialWidth[index], 0.0f, initialColor[index]);
				AddChainElement(index, newElem);
				// alter diff to represent new head size
				diff = newPos - newElem.Position;
			}
			else
			{
				// extend existing head
				headElement.Position = newPos;
			}

			// Is this segment full?
			if ((segment.tail + 1) % maxElementsPerChain == segment.head)
			{
				// If so, shrink tail gradually to match head extension
				Element tailElement = chainElementList[segment.start + segment.tail];
				int preTailIndex;
				if (segment.tail == 0)
					preTailIndex = maxElementsPerChain - 1;
				else
					preTailIndex = segment.tail - 1;

				Element preTailElement = chainElementList[segment.start + preTailIndex];

				// Measure tail diff from pretail to tail
				Vector3 tailDiff = tailElement.Position - preTailElement.Position;
				float tailLength = tailDiff.Length;

				if (tailLength > 1e-06)
				{
					float tailSize = elementLength - diff.Length;
					tailDiff *= tailSize / tailLength;
					tailElement.Position = preTailElement.Position + tailDiff;
				}
			}

			boundsDirty = true;

			if (parentNode != null)
			{
				parentNode.NeedUpdate();
				// Need to dirty the parent node, but can't do it using needUpdate() here 
				// since we're in the middle of the scene graph update (node listener), 
				// so re-entrant calls don't work. Queue.
				// TODO: Port the code to do this
			}
		}
		#endregion

		#region NodeListener Methods
		public void NodeUpdated(Node node)
		{
			for (int i = 0; i < nodeList.Count; ++i)
			{
				if (nodeList[i] == node)
				{
					UpdateTrail(i, node);
					break;
				}
			}
		}

		public void NodeDestroyed(Node node)
		{
			RemoveNode(node);
		}
		#endregion

		#region BillBoardChain overloads
		public override int MaxChainElements
		{
			get { return base.MaxChainElements; }
			set
			{
				base.MaxChainElements = value;
				elementLength = trailLength / maxElementsPerChain;
				squaredElementLength = elementLength * elementLength;
			}
		}

		public override int NumberOfChains
		{
			get { return base.NumberOfChains; }
			set
			{
				base.NumberOfChains = value;

				initialColor.Capacity = NumberOfChains;
				deltaColor.Capacity = NumberOfChains;
				initialWidth.Capacity = NumberOfChains;
				deltaWidth.Capacity = NumberOfChains;
				if (initialColor.Count < initialColor.Capacity)
				{
					for (int i = initialColor.Count; i < initialColor.Capacity; ++i)
					{
						initialColor.Add(ColorEx.White);
						deltaColor.Add(ColorEx.White);
						initialWidth.Add(5);
						deltaWidth.Add(5);
					}
				}
			}
		}
		#endregion
	}
}