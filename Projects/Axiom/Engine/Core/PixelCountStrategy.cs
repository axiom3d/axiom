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
//     <id value="$Id: PixelCountStrategy.cs 1762 2009-09-13 17:56:22Z bostich $"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;

using Axiom.Graphics;
using Axiom.Math;
using MathHelper = Axiom.Math.Utility;
using Axiom.Core.Collections;

#endregion Namespace Declarations

namespace Axiom.Core
{
	/// <summary>
	///
	/// </summary>
	public class PixelCountStrategy : LodStrategy
	{

		/// <summary>
		/// Default constructor.
		/// </summary>
		public PixelCountStrategy()
			: base( "PixelCount" )
		{
		}

		#region LodStrategy Implementation

		public override Real BaseValue
		{
			get
			{
				return float.MaxValue;
			}
		}

		public override Real TransformBias( Real factor )
		{
			// No transformation required for pixel count strategy
			return factor;
		}

		public override ushort GetIndex( Real value, MeshLodUsageList meshLodUsageList )
		{
			// Values are descending
			return GetIndexDescending( value, meshLodUsageList );
		}

		public override ushort GetIndex( Real value, LodValueList materialLodValueList )
		{
			// Values are descending
			return GetIndexDescending( value, materialLodValueList );
		}

		public override void Sort( MeshLodUsageList meshLodUsageList )
		{
			// Sort descending
			SortDescending( meshLodUsageList );
		}

		public override bool IsSorted( LodValueList values )
		{
			// Check if values are sorted descending
			return IsSortedDescending( values );
		}

		protected override Real getValue( MovableObject movableObject, Camera camera )
		{
			// Get viewport
			Viewport viewport = camera.Viewport;

			// Get viewport area
			float viewportArea = viewport.ActualWidth * viewport.ActualHeight;

			// Get area of unprojected circle with object bounding radius
			float boundingArea = MathHelper.PI * MathHelper.Sqr( movableObject.BoundingRadius );

			// Base computation on projection type
			switch ( camera.ProjectionType )
			{
				case Projection.Perspective:
					{
						// Get camera distance
						float distanceSquared = movableObject.ParentNode.GetSquaredViewDepth( camera );

						// Check for 0 distance
						if ( distanceSquared <= float.Epsilon )
							return BaseValue;

						// Get projection matrix (this is done to avoid computation of tan(fov / 2))
						Matrix4 projectionMatrix = camera.ProjectionMatrix;

						//estimate pixel count
						return ( boundingArea * viewportArea * projectionMatrix[ 0, 0 ] * projectionMatrix[ 1, 1 ] ) / distanceSquared;
					}
					// break;
				case Projection.Orthographic:
					{
						// Compute orthographic area
						float orthoArea = camera.OrthoWindowHeight * camera.OrthoWindowWidth;

						// Check for 0 orthographic area
						if ( orthoArea <= float.Epsilon )
							return BaseValue;

						// Estimate pixel count
						return ( boundingArea * viewportArea ) / orthoArea;
					}
					// break;
				default:
					{
						// This case is not covered for obvious reasons
						throw new NotSupportedException();
					}
			}
		}

		#endregion LodStrategy Implementation
	}
}