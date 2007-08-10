#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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

using System;
using Ode;
using Axiom.Core;
using Axiom.Physics;
using Axiom.Graphics;

namespace Axiom.Dynamics.ODE 
{
    /// <summary>
    /// Summary description for OdeDynamics.
    /// </summary>
    public class OdeDynamics : DynamicsSystem, IPlugin {
        #region Constructors

        /// <summary>
        ///		Default constructor.
        /// </summary>
        /// <remarks>
        ///		Upon creation, the inherited constructor will register this instance as the singleton instance.
        /// </remarks>
        public OdeDynamics() {
        }

        #endregion

        #region IDynamicsSystem Members

        /// <summary>
        ///		
        /// </summary>
        /// <returns></returns>
        public override IWorld CreateWorld() {
            return new OdeWorld();
        }

        #endregion

        #region IPlugin Members

        public void Start() {
        }

        public void Stop() {
            // TODO:  Add ODEDynamics.Stop implementation
        }

        #endregion

        #region Static methods


        static internal Ode.Vector3 MakeOdeVector(Axiom.MathLib.Vector3 vec) {
            return new Ode.Vector3(vec.x, vec.y, vec.z);
        }

        static internal Axiom.MathLib.Vector3 MakeDIVector(Ode.Vector3 vec) {
            return new Axiom.MathLib.Vector3((float)vec.X, (float)vec.Y, (float)vec.Z);
        }

        static internal Ode.Quaternion MakeOdeQuat(Axiom.MathLib.Quaternion quat) {
            // convert the quat
            Ode.Quaternion odeQuat = new Ode.Quaternion();
            odeQuat.W = (float)quat.w;
            odeQuat.X = (float)quat.x;
            odeQuat.Y = (float)quat.y;
            odeQuat.Z = (float)quat.z;

            return odeQuat;
        }

        static internal Axiom.MathLib.Quaternion MakeDIQuat(Ode.Quaternion quat) {
            return new Axiom.MathLib.Quaternion((float)quat.W, (float)quat.X, (float)quat.Y, (float)quat.Z);
        }

		static internal Ode.TriMeshData MakeTriMesh(Mesh mesh)
		{
			int totalverts = 0;   // the total number of vertices in all submeshes
			int totalindices = 0; // the total number of indices in all submeshes

			// first travel through all submeshes, to get the total number of indices und vertices
			for (int i = 0; i < mesh.SubMeshCount; i++)
			{
				// vertices
				VertexData vertexData = mesh.GetSubMesh (i).vertexData;
				short originalPosBufferBinding = vertexData.vertexDeclaration.FindElementBySemantic(VertexElementSemantic.Position).Source;
				HardwareVertexBuffer positionBuffer = vertexData.vertexBufferBinding.GetBuffer(originalPosBufferBinding);
				
				totalverts += positionBuffer.VertexCount;

				// indices
				IndexData indexData = mesh.GetSubMesh(i).indexData;
				HardwareIndexBuffer indexBuffer = indexData.indexBuffer;

				totalindices += indexData.indexCount;
			}

			// prepare arrays
			Ode.Vector3[] vertices = new Ode.Vector3 [totalverts];
			short[] indices = new short[totalindices];

			int actualvertex = 0;
			int actualindex = 0;

			// travel through the submeshes ...
			for (int i = 0; i < mesh.SubMeshCount; i++)
			{
				VertexData vertexData = mesh.GetSubMesh (i).vertexData;
				short originalPosBufferBinding = vertexData.vertexDeclaration.FindElementBySemantic(VertexElementSemantic.Position).Source;

				HardwareVertexBuffer positionBuffer = vertexData.vertexBufferBinding.GetBuffer(originalPosBufferBinding);
				float[] vertex = new float[positionBuffer.VertexCount * 3];
				//IntPtr ptr = System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(vertex, 0);
                unsafe
                {
                    fixed (void* ptr = vertex)
                    {
				float f = 0;
                        positionBuffer.ReadData(0, positionBuffer.VertexCount * 3 * System.Runtime.InteropServices.Marshal.SizeOf(f.GetType()), (IntPtr)ptr);
                    }
                }
				for (int j = 0; j < positionBuffer.VertexCount; j++)
				{
					vertices[actualvertex+j] = new Ode.Vector3 (vertex[j * 3],vertex[j*3+1],vertex[j*3+2]);
				}
	
				IndexData indexData = mesh.GetSubMesh(i).indexData;
				HardwareIndexBuffer indexBuffer = indexData.indexBuffer;
				short[] subindices = new short[indexData.indexCount];
				//ptr = System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(subindices, 0);
                unsafe
                {
                    fixed (void* ptr = subindices)
                    {
				short s = 0;
                        indexBuffer.ReadData(0, indexData.indexCount * System.Runtime.InteropServices.Marshal.SizeOf(s.GetType()), (IntPtr)ptr);
                    }
                }
				for (int j = 0; j < subindices.Length; j++)
				{
					indices[actualindex+j] = (short)(subindices[j]+actualvertex);
				}

				actualvertex += positionBuffer.VertexCount;
				actualindex += indexData.indexCount;
			}

			// build the trimesh data
			Ode.TriMeshData tridat = new Ode.TriMeshData();
			tridat.Build (vertices, indices);

			return tridat;
		}

        #endregion
    }
}
