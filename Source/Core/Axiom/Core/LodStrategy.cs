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
using System.Collections.Generic;
using Axiom.Controllers;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;
using Axiom.Core.Collections;

#endregion Namespace Declarations

namespace Axiom.Core
{
	/// <summary>
	/// Strategy for determining level of detail.
	/// </summary>
	/// <remarks>
	/// Generally, to create a new lod strategy, all of the following will
	/// need to be implemented: Value, BaseValue, TransformBias,
	/// Index, Sort, and IsSorted.
	/// In addition, TransformUserValue may be overridden.</remarks>
	public abstract class LodStrategy
	{
		#region Fields and Properties

		public string Name { get; protected set; }

		#endregion Fields and Properties

		#region Construction and Destruction

		/// <summary>
		/// Constructor accepting name.
		/// </summary>
		/// <param name="name"></param>
		public LodStrategy( string name )
		{
			Name = name;
		}

		#endregion Construction and Destruction

		#region Methods

		/// <summary>
		/// Transform user supplied value to internal value.
		/// </summary>
		/// By default, performs no transformation.
		/// Do not throw exceptions for invalid values here, as the lod strategy
		/// may be changed such that the values become valid.
		/// <param name="userValue"></param>
		/// <returns></returns>
		public virtual Real TransformUserValue( Real userValue )
		{
			// No transformation by default
			return userValue;
		}

		/// <summary>
		/// Compute the lod value for a given movable object relative to a given camera.
		/// </summary>
		/// <param name="movableObject"></param>
		/// <param name="cam"></param>
		/// <returns></returns>
		public float GetValue( MovableObject movableObject, Camera cam )
		{
			// Just return implementation with lod camera
			return getValue( movableObject, cam );
		}

		/// <summary>
		/// Implementation of isSorted suitable for ascending values.
		/// </summary>
		/// <param name="values"></param>
		/// <returns></returns>
		protected static bool IsSortedAscending( LodValueList values )
		{
			for ( var i = 0; i < values.Count; i++ )
			{
				float prev = values[ i ];
				if ( i + 1 < values.Count )
				{
					if ( values[ i + 1 ] < prev )
					{
						return false;
					}
				}
			}
			return true;
		}

		/// <summary>
		/// Implementation of isSorted suitable for descending values.
		/// </summary>
		/// <param name="values"></param>
		/// <returns></returns>
		protected static bool IsSortedDescending( LodValueList values )
		{
			for ( var i = 0; i < values.Count; i++ )
			{
				float prev = values[ i ];
				if ( i + 1 < values.Count )
				{
					if ( values[ i + 1 ] > prev )
					{
						return false;
					}
				}
			}
			return true;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="meshLodUsageList"></param>
		protected void SortAscending( MeshLodUsageList meshLodUsageList )
		{
			meshLodUsageList.Sort( 0, meshLodUsageList.Count, new LodUsageSortLess() );
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="meshLodUsageList"></param>
		protected void SortDescending( MeshLodUsageList meshLodUsageList )
		{
			meshLodUsageList.Sort( 0, meshLodUsageList.Count, new LodUsageSortGreater() );
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="value"></param>
		/// <param name="meshLodUsageList"></param>
		/// <returns></returns>
		protected static ushort GetIndexAscending( float value, MeshLodUsageList meshLodUsageList )
		{
			ushort index = 0;
			for ( var i = 0; i < meshLodUsageList.Count; i++, index++ )
			{
				if ( meshLodUsageList[ i ].Value > value )
				{
					return (ushort)( index - 1 );
				}
			}
			return (ushort)( meshLodUsageList.Count - 1 );
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="value"></param>
		/// <param name="meshLodUsageList"></param>
		/// <returns></returns>
		protected static ushort GetIndexDescending( float value, MeshLodUsageList meshLodUsageList )
		{
			ushort index = 0;
			for ( var i = 0; i < meshLodUsageList.Count; i++, index++ )
			{
				if ( meshLodUsageList[ i ].Value < value )
				{
					return (ushort)( index - 1 );
				}
			}
			return (ushort)( meshLodUsageList.Count - 1 );
		}

		/// <summary>
		/// </summary>
		protected static ushort GetIndexAscending( float value, LodValueList materialLodValueList )
		{
			ushort index = 0;
			for ( var i = 0; i < materialLodValueList.Count; i++, index++ )
			{
				if ( materialLodValueList[ i ] > value )
				{
					return (ushort)( index - 1 );
				}
			}
			return (ushort)( materialLodValueList.Count - 1 );
		}

		/// <summary>
		/// </summary>
		protected static ushort GetIndexDescending( float value, LodValueList materialLodValueList )
		{
			ushort index = 0;
			for ( var i = 0; i < materialLodValueList.Count; i++, index++ )
			{
				if ( materialLodValueList[ i ] < value )
				{
					return (ushort)( index - 1 );
				}
			}
			return (ushort)( materialLodValueList.Count - 1 );
		}

		#endregion Methods

		#region Abstract Definitions

		/// <summary>
		/// Get the value of the first (highest) level of detail.
		/// </summary>
		public abstract Real BaseValue { get; }

		/// <summary>
		/// Transform lod bias so it only needs to be multiplied by the lod value.
		/// </summary>
		/// <param name="factor"></param>
		/// <returns></returns>
		public abstract Real TransformBias( Real factor );

		public abstract ushort GetIndex( Real value, MeshLodUsageList meshLodUsageList );

		public abstract ushort GetIndex( Real value, LodValueList materialLodValueList );

		public abstract void Sort( MeshLodUsageList meshLodUsageList );

		public abstract bool IsSorted( LodValueList values );

		/// <summary>
		/// Compute the lod value for a given movable object relative to a given camera.
		/// </summary>
		/// <param name="movableObject"></param>
		/// <param name="camera"></param>
		/// <returns></returns>
		protected abstract Real getValue( MovableObject movableObject, Camera camera );

		#endregion Abstract Definitions
	}

	/// <summary>
	/// Small helper class to sort a MeshLodUsageList
	/// </summary>
	public class LodUsageSortGreater : IComparer<MeshLodUsage>
	{
		public int Compare( MeshLodUsage mesh1, MeshLodUsage mesh2 )
		{
			if ( mesh1.Value > mesh2.Value )
			{
				return 1;
			}
			else
			{
				return 0;
			}
		}
	}

	/// <summary>
	/// Small helper class to sort a MeshLodUsageList
	/// </summary>
	public class LodUsageSortLess : IComparer<MeshLodUsage>
	{
		public int Compare( MeshLodUsage mesh1, MeshLodUsage mesh2 )
		{
			if ( mesh1.Value < mesh2.Value )
			{
				return 1;
			}
			else
			{
				return 0;
			}
		}
	}
}