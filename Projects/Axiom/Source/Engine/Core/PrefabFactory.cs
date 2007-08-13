#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006 Axiom Project Team

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
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
//     <id value="$Id: Mesh.cs 1044 2007-05-05 21:01:55Z borrillis $"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Text;

#endregion Namespace Declarations

namespace Axiom.Core
{
	/// <summary>
	/// A factory class that can create various mesh prefabs. 
	/// </summary>
	/// <remarks>
	/// This class is used by MeshManager to offload the loading of various prefab types 
	/// to a central location.
	/// </remarks>
	public class PrefabFactory
	{

		/// <summary>
		/// If the given mesh has a known prefab resource name (e.g "Prefab_Plane") 
		/// then this prefab will be created as a submesh of the given mesh.
		/// </summary>
		/// <param name="mesh">The mesh that the potential prefab will be created in.</param>
		/// <returns><c>true</c> if a prefab has been created, otherwise <c>false</c>.</returns>
		public static bool Create( Mesh mesh )
		{
			string resourceName = mesh.Name;

			if ( resourceName == "Prefab_Plane" )
			{
				_createPlane( mesh );
				return true;
			}
			else if ( resourceName == "Prefab_Cube" )
			{
				_createCube( mesh );
				return true;
			}
			else if ( resourceName == "Prefab_Sphere" )
			{
				_createSphere( mesh );
				return true;
			}

			return false;
		}

		/// <summary>
		/// Creates a plane as a submesh of the given mesh
		/// </summary>
		private static void _createPlane( Mesh mesh )
		{

			/*
		SubMesh sub = mesh.CreateSubMesh();
		float[] vertices = new float[32] {
			-100, -100, 0,	// pos
			0,0,1,			// normal
			0,1,			// texcoord
			100, -100, 0,
			0,0,1,
			1,1,
			100,  100, 0,
			0,0,1,
			1,0,
			-100,  100, 0 ,
			0,0,1,
			0,0 
		};
		mesh.SharedVertexData = new VertexData();
		mesh.SharedVertexData.vertexCount = 4;
		VertexDeclaration decl = mesh.SharedVertexData.vertexDeclaration;
		VertexBufferBinding bind = mesh.SharedVertexData.vertexBufferBinding;

		size_t offset = 0;
		decl.AddElement(0, offset, VET_FLOAT3, VES_POSITION);
		offset += VertexElement.getTypeSize(VET_FLOAT3);
		decl->addElement(0, offset, VET_FLOAT3, VES_NORMAL);
		offset += VertexElement.getTypeSize(VET_FLOAT3);
		decl->addElement(0, offset, VET_FLOAT2, VES_TEXTURE_COORDINATES, 0);
		offset += VertexElement.getTypeSize(VET_FLOAT2);

		HardwareVertexBufferSharedPtr vbuf = 
			HardwareBufferManager::getSingleton().createVertexBuffer(
			offset, 4, HardwareBuffer::HBU_STATIC_WRITE_ONLY);
		bind->setBinding(0, vbuf);

		vbuf->writeData(0, vbuf->getSizeInBytes(), vertices, true);

		sub->useSharedVertices = true;
		HardwareIndexBufferSharedPtr ibuf = HardwareBufferManager::getSingleton().
			createIndexBuffer(
			HardwareIndexBuffer::IT_16BIT, 
			6, 
			HardwareBuffer::HBU_STATIC_WRITE_ONLY);

		unsigned short faces[6] = {0,1,2,
			0,2,3 };
		sub->indexData->indexBuffer = ibuf;
		sub->indexData->indexCount = 6;
		sub->indexData->indexStart =0;
		ibuf->writeData(0, ibuf->getSizeInBytes(), faces, true);

		mesh->_setBounds(AxisAlignedBox(-100,-100,0,100,100,0), true);
		mesh->_setBoundingSphereRadius(Math::Sqrt(100*100+100*100));
			 * */
		}

		/// <summary>
		/// Creates a 100x100x100 cube as a submesh of the given mesh
		/// </summary>
		private static void _createCube( Mesh mesh )
		{
		}

		/// <summary>
		/// Creates a sphere with a diameter of 100 units as a submesh of the given mesh
		/// </summary>
		private static void _createSphere( Mesh mesh )
		{
		}

	}
}
