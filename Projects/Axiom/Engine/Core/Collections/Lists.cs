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
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id: Lists.cs -1   $"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections;
using System.Collections.Generic;

using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.Core.Collections
{
	/// <summary>
	/// Represents a collection of <see cref="Light">Lights</see>.
	/// </summary>
	public class LightList : List<Light> {}

	/// <summary>
	/// Represents a collection of <see cref="Entity">Entities</see>
	/// </summary>
	public class EntityList : List<Entity> {}

	/// <summary>
	/// Represents a collection of <see cref="SubEntity"/> objects
	/// </summary>
	/// <remarks>
	/// The items are sorted by their implicit index, it is important that the order of  subentities in the collection maps to the order
	/// of submeshes in a <see cref="SubMeshList"/>
	/// </remarks>
	public class SubEntityList : List<SubEntity> {}

	/// <summary>
	/// Represents a collection of <see cref="SubMesh">SubMeshes</see>
	/// </summary>
	/// <remarks>
	/// The items are sorted by their implicit index, it is important that the order of  submeshes in the collection maps to the order
	/// of subentities in a <see cref="SubEntityList"/>
	/// </remarks>
	public class SubMeshList : List<SubMesh> {}

	/// <summary>
	///     Generics: List<MeshLodUsage>
	/// </summary>
	public class MeshLodUsageList : List<MeshLodUsage> {}

	/// <summary>
	/// 
	/// </summary>
	public class LodValueList : List<Real> {}

	/// <summary>
	///     Generics: List<int>
	/// </summary>
	public class IntList : List<int>
	{
		public void Resize( int size )
		{
			int[] data = this.ToArray();
			int[] newData = new int[size];
			Array.Copy( data, 0, newData, 0, size );
			Clear();
			AddRange( newData );
		}
	}

	/// <summary>
	///     Generics: List<float>
	/// </summary>
	public class FloatList : List<float>
	{
		public void Resize( int size )
		{
			float[] data = this.ToArray();
			float[] newData = new float[size];
			Array.Copy( data, 0, newData, 0, size );
			Clear();
			AddRange( newData );
		}
	}
}
