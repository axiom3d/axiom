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
//     <id value="$Id: DistanceLodStrategy.cs 1762 2009-09-13 18:56:22Z bostich $"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Axiom.Math;
using Axiom.Core;
using Axiom.Graphics;
using MathHelper = Axiom.Math.Utility;
using Axiom.Core.Collections;

#endregion Namespace Declarations

namespace Axiom.Core
{
	/// <summary>
	/// Level of detail strategy based on distance from camera.
	/// </summary>
	public class DistanceLodStrategy : LodStrategy, ISingleton<DistanceLodStrategy>
	{
		#region Fields and Properties

		public const string StrategyName = "Distance";

		/// <summary>
		///
		/// </summary>
		protected Real ReferenceViewValue;

		/// <summary>
		///
		/// </summary>
		private bool _referenceViewEnabled;

		/// <summary>
		///
		/// </summary>
		public bool ReferenceViewEnabled
		{
			set
			{
				// Ensure reference value has been set before being enabled
				if ( value )
				{
					Debug.Assert( this.ReferenceViewValue != float.NaN, "Reference view must be set before being enabled!" );
				}

				this._referenceViewEnabled = value;
			}
			get
			{
				return this._referenceViewEnabled;
			}
		}

		#endregion Fields and Properties

		/// <summary>
		/// Default constructor.
		/// </summary>
		protected internal DistanceLodStrategy()
			: base( StrategyName )
		{
			if ( instance == null )
			{
				instance = this;
				this.ReferenceViewValue = float.NaN;
			}
			else
			{
				throw new AxiomException( "Cannot create another instance of {0}. Use Instance property instead", GetType().Name );
			}
		}

		/// <summary>
		/// Sets the reference view upon which the distances were based.
		/// </summary>
		/// <note>
		/// This automatically enables use of the reference view.
		///  There is no corresponding get method for these values as
		///    they are not saved, but used to compute a reference value.
		/// </note>
		/// <param name="viewportWidth"></param>
		/// <param name="viewportHeight"></param>
		/// <param name="fovY"></param>
		public virtual void SetReferenceView( float viewportWidth, float viewportHeight, Radian fovY )
		{
			// Determine x FOV based on aspect ratio
			var fovX = fovY*( (Real)viewportWidth/(Real)viewportHeight );

			// Determine viewport area
			var viewportArea = viewportHeight*viewportWidth;

			// Compute reference view value based on viewport area and FOVs
			this.ReferenceViewValue = viewportArea*MathHelper.Tan( fovX*(Real)0.5f )*MathHelper.Tan( fovY*(Real)0.5f );

			// Enable use of reference view
			this._referenceViewEnabled = true;
		}

		#region LodStrategy Implemention

		public override Real BaseValue
		{
			get
			{
				return 0;
			}
		}

		protected override Real getValue( MovableObject movableObject, Camera cam )
		{
			// Get squared depth taking into account bounding radius
			// (d - r) ^ 2 = d^2 - 2dr + r^2, but this requires a lot
			// more computation (including a sqrt) so we approximate
			// it with d^2 - r^2, which is good enough for determining
			// lod.
			Real squaredDepth = movableObject.ParentNode.GetSquaredViewDepth( cam ) -
			                    MathHelper.Sqr( movableObject.BoundingRadius );

			// Check if reference view needs to be taken into account
			if ( this._referenceViewEnabled )
			{
				// Reference view only applicable to perspective projection
				System.Diagnostics.Debug.Assert( cam.ProjectionType == Projection.Perspective,
				                                 "Camera projection type must be perspective!" );

				// Get camera viewport
				var viewport = cam.Viewport;

				// Get viewport area
				Real viewportArea = viewport.ActualWidth*viewport.ActualHeight;

				// Get projection matrix (this is done to avoid computation of tan(fov / 2))
				var projectionMatrix = cam.ProjectionMatrix;

				// Compute bias value (note that this is similar to the method used for PixelCountLodStrategy)
				Real biasValue = viewportArea*projectionMatrix[ 0, 0 ]*projectionMatrix[ 1, 1 ];

				// Scale squared depth appropriately
				squaredDepth *= ( this.ReferenceViewValue/biasValue );
			}

			// Squared depth should never be below 0, so clamp
			squaredDepth = MathHelper.Max( squaredDepth, 0 );

			// Now adjust it by the camera bias and return the computed value
			return squaredDepth*cam.InverseLodBias;
		}

		public override Real TransformBias( Real factor )
		{
			Debug.Assert( factor > 0.0f, "Bias factor must be > 0!" );
			return 1.0f/factor;
		}

		public override Real TransformUserValue( Real userValue )
		{
			return MathHelper.Sqr( userValue );
		}

		public override ushort GetIndex( Real value, MeshLodUsageList meshLodUsageList )
		{
			return GetIndexAscending( value, meshLodUsageList );
		}

		public override ushort GetIndex( Real value, LodValueList materialLodValueList )
		{
			return GetIndexAscending( value, materialLodValueList );
		}

		public override void Sort( MeshLodUsageList meshLodUsageList )
		{
			SortAscending( meshLodUsageList );
		}

		public override bool IsSorted( LodValueList values )
		{
			return IsSortedAscending( values );
		}

		#endregion LodStrategy Implemention

		#region ISingleton<DistanceLodStrategy> Members

		protected static DistanceLodStrategy instance;

		public static DistanceLodStrategy Instance
		{
			get
			{
				return instance;
			}
		}

		public bool Initialize( params object[] args )
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}