using System;
using System.IO;
using System.Text;
using Axiom.Core;
using Axiom.MathLib;
using Axiom.SubSystems.Rendering;

namespace Axiom.Serialization
{
	/// <summary>
	/// 	Summary description for OgreMeshReader.
	/// </summary>
	public class OgreMeshReader : BinaryReader
	{
		#region Member variables
		const ushort HEADER_CHUNK_ID = 0x1000;
		const int CHUNK_OVERHEAD_SIZE = 6;

		protected int currentChunkLength;
		protected Mesh mesh;
		protected int subMeshAutoNumber = 0;

		#endregion
		
		#region Constructors
		
		public OgreMeshReader(Stream data) : base(data)
		{

		}
		
		#endregion
		
		#region Methods
		
		public void Import(Mesh mesh)
		{
			// store a local reference to the mesh for modification
			this.mesh = mesh;

			// start off by taking a look at the header
			ReadFileHeader();

			try
			{
				MeshChunkID chunkID = 0;

				bool parse = true;
				
				while(parse)
				{
					chunkID = ReadChunk();

					switch(chunkID)
					{
						case MeshChunkID.Mesh:
							ReadMesh();
							break;

						default:
							System.Diagnostics.Trace.Write("Can only parse meshes at the top level during mesh loading.");
							parse = false;
							break;
							//throw new Exception("Unknown chunk Id");
					} // switch
				}
			}
			catch(EndOfStreamException)
			{
				// do nothing, we are finished
			}

		}

		protected void ReadFileHeader()
		{
			short headerID = 0;

			// read the header ID
			headerID = ReadInt16();

			// better hope this is the header
			if(headerID == HEADER_CHUNK_ID)
			{
				string version = this.ReadString('\n');
			}
			else
				throw new Exception("Invalid mesh file, no header found.");

		}

		protected string ReadString(char delimiter)
		{
			StringBuilder sb = new StringBuilder();

			char c;

			// sift through each character until we hit the delimiter
			while((c = base.ReadChar()) != delimiter)
				sb.Append(c);

			// return the accumulated string
			return sb.ToString();
		}

		protected MeshChunkID ReadChunk()
		{
			// get the chunk id
			short id = ReadInt16();

			// read the length for this chunk
			currentChunkLength = ReadInt32();

			return (MeshChunkID)id;
		}

		protected void ReadMesh()
		{
			MeshChunkID chunkID;

			// see if this mesh has a skeleton
			bool skeletalAnimation = ReadBoolean();

			// BUG: Work out why dynamic write only buffer chokes when locked
			//if(skeletalAnimation)
			//	mesh.SetVertexBufferPolicy(BufferUsage.DynamicWriteOnly, true);

			// read the next chunk ID
			chunkID = ReadChunk();

			while(chunkID == MeshChunkID.Geometry ||
				chunkID == MeshChunkID.SubMesh ||
				chunkID == MeshChunkID.MeshSkeletonLink ||
				chunkID == MeshChunkID.MeshBoneAssignment ||
				chunkID == MeshChunkID.MeshLOD ||
				chunkID == MeshChunkID.MeshBounds)
			{

				switch(chunkID)
				{
					case MeshChunkID.Geometry:
						mesh.SharedVertexData = new VertexData();

						// read geometry into shared vertex data
						ReadGeometry(mesh.SharedVertexData);

						// TODO: trap errors here
						break;

					case MeshChunkID.SubMesh:
						// read the sub mesh data
						ReadSubMesh();
						break;

					case MeshChunkID.MeshSkeletonLink:
						// TODO: Handle meshes with skeletons, skip for now
						IgnoreCurrentChunk();
						break;

					case MeshChunkID.MeshBoneAssignment:
						// TODO: Handle meshes with bones, skip for now
						IgnoreCurrentChunk();
						break;

					case MeshChunkID.MeshLOD:
						// TODO: Handle meshes with LOD, skip for now
						IgnoreCurrentChunk();
						break;

					case MeshChunkID.MeshBounds:
						// read the pre-calculated bounding information
						ReadBoundsInfo();
						break;
				} // switch

				chunkID = ReadChunk();
			} // while
		}

		protected void IgnoreCurrentChunk()
		{
			Seek(currentChunkLength - CHUNK_OVERHEAD_SIZE);
		}

		public void ReadBoundsInfo()
		{
			Vector3 min = Vector3.Zero;
			Vector3 max = Vector3.Zero;

			// min abb extent
			min.x = ReadSingle();
			min.y = ReadSingle();
			min.z = ReadSingle();

			// max abb extent
			max.x = ReadSingle();
			max.y = ReadSingle();
			max.z = ReadSingle();

			// set the mesh's aabb
			mesh.BoundingBox = new AxisAlignedBox(min, max);

			float radius = ReadSingle();

			// set the bounding sphere radius
			mesh.BoundingSphereRadius = radius;
		}

		public void ReadSubMesh()
		{
			MeshChunkID chunkID;

			SubMesh subMesh = mesh.CreateSubMesh("SubMesh" + subMeshAutoNumber++);

			// get the material name
			string materialName = ReadString('\n');
			subMesh.MaterialName = materialName;

			// use shared vertices?
			subMesh.useSharedVertices = ReadBoolean();

			subMesh.indexData.indexStart = 0;
			subMesh.indexData.indexCount = ReadInt32();

			// does this use 32 bit index buffer
			bool idx32bit = ReadBoolean();

			HardwareIndexBuffer idxBuffer = null;

			if(idx32bit)
			{
				// create the index buffer
				idxBuffer = 
					HardwareBufferManager.Instance.
						CreateIndexBuffer(
							IndexType.Size32,
							subMesh.indexData.indexCount,
							mesh.indexBufferUsage,
							mesh.indexShadowBuffer);

				IntPtr indices = idxBuffer.Lock(0, subMesh.indexData.indexCount, BufferLocking.Discard);

				// read the ints into the buffer data
				ReadInts(subMesh.indexData.indexCount, indices);
	
				// unlock the buffer to commit					
				idxBuffer.Unlock();
			}
			else // 16-bit
			{
				// create the index buffer
				idxBuffer = 
					HardwareBufferManager.Instance.
					CreateIndexBuffer(
					IndexType.Size16,
					subMesh.indexData.indexCount,
					mesh.indexBufferUsage,
					mesh.indexShadowBuffer);

				IntPtr indices = idxBuffer.Lock(0, subMesh.indexData.indexCount, BufferLocking.Discard);

				// read the shorts into the buffer data
				ReadShorts(subMesh.indexData.indexCount, indices);
						
				idxBuffer.Unlock();
			}

			// save the index buffer
			subMesh.indexData.indexBuffer = idxBuffer;

			if(!subMesh.useSharedVertices)
			{
				chunkID = ReadChunk();

				if(chunkID != MeshChunkID.Geometry)
					throw new Exception("Missing geometry data in subMesh file.");

				subMesh.vertexData = new VertexData();

				// read the geometry data
				ReadGeometry(subMesh.vertexData);
			}

			// get the next chunkID
			chunkID = ReadChunk();

			// walk through all the bone assignments for this submesh
			while(chunkID == MeshChunkID.SubMeshBoneAssignment)
			{
				switch(chunkID)
				{
					case MeshChunkID.SubMeshBoneAssignment:
						// TODO: Sub SubMesh Bone assignments, ignore for now
						IgnoreCurrentChunk();
						break;
				}

				// read the next chunkID
				chunkID = ReadChunk();
			} // while

			// walk back to the beginning of the last chunk ID read since
			// we already moved past it and it wasnt of interest to us
			Seek(-CHUNK_OVERHEAD_SIZE);
		}

		/// <summary>
		///		Reads geometry data into the specified VertexData object.
		/// </summary>
		/// <param name="vertexData"></param>
		public void ReadGeometry(VertexData vertexData)
		{
			ushort texCoordSet = 0;
			HardwareVertexBuffer vBuffer = null;
			ushort bindIdx = 0;
			
			vertexData.vertexStart = 0;

			vertexData.vertexCount = ReadInt32();

			// vertex buffers
			vertexData.vertexDeclaration.AddElement(new VertexElement(bindIdx, 0, VertexElementType.Float3, VertexElementSemantic.Position));
			vBuffer = HardwareBufferManager.Instance.
				CreateVertexBuffer(vertexData.vertexDeclaration.GetVertexSize(bindIdx), 
												vertexData.vertexCount, mesh.vertexBufferUsage, mesh.vertexShadowBuffer);

			IntPtr posData = vBuffer.Lock(0, vertexData.vertexCount * 3, BufferLocking.Discard);

			// ram the floats into the buffer data
			ReadFloats(vertexData.vertexCount * 3, posData);

			// unlock the buffer
			vBuffer.Unlock();

			// bind the position data
			vertexData.vertexBufferBinding.SetBinding(bindIdx, vBuffer);

			bindIdx++;

			// check out the next chunk
			MeshChunkID chunkID = ReadChunk();

			// keep going as long as we have more optional buffer chunks
			while(chunkID == MeshChunkID.GeometryNormals ||
				chunkID == MeshChunkID.GeometryColors ||
				chunkID == MeshChunkID.GeometryTexCoords)
			{
				switch(chunkID)
				{
					case MeshChunkID.GeometryNormals:
						// add an element for normals
						vertexData.vertexDeclaration.
							AddElement(new VertexElement(bindIdx, 0, VertexElementType.Float3, VertexElementSemantic.Normal));

						vBuffer = HardwareBufferManager.Instance.CreateVertexBuffer(
							vertexData.vertexDeclaration.GetVertexSize(bindIdx),
							vertexData.vertexCount, 
							mesh.vertexBufferUsage,
							mesh.vertexShadowBuffer);

						// lock the buffer for editing
						IntPtr normals = vBuffer.Lock(0, vertexData.vertexCount * 3, BufferLocking.Discard);

						// stuff the floats into the normal buffer
						ReadFloats(vertexData.vertexCount * 3, normals);

						// unlock the buffer to commit
						vBuffer.Unlock();

						// bind this buffer
						vertexData.vertexBufferBinding.SetBinding(bindIdx, vBuffer);

						bindIdx++;

						break;

					case MeshChunkID.GeometryColors:
						// TODO: Color geometry, skip for now
						IgnoreCurrentChunk();
						break;

					case MeshChunkID.GeometryTexCoords:
						// get the number of texture dimensions (1D, 2D, 3D, etc)
						short dim = ReadInt16();

						// add a vertex element for the current tex coord set
						vertexData.vertexDeclaration.AddElement(
							new VertexElement(bindIdx, 0,
								VertexElement.MultiplyTypeCount(VertexElementType.Float1, dim),
								VertexElementSemantic.TexCoords,
								texCoordSet));

						// create the vertex buffer for the tex coords
						vBuffer = HardwareBufferManager.Instance.CreateVertexBuffer(
							vertexData.vertexDeclaration.GetVertexSize(bindIdx),
							vertexData.vertexCount,
							mesh.vertexBufferUsage,
							mesh.vertexShadowBuffer);

						//float[] texCoords = (float[])vBuffer.Lock(0, vertexData.vertexCount * dim, BufferLocking.Discard);
						IntPtr texCoords = vBuffer.Lock(0, vertexData.vertexCount * dim, BufferLocking.Discard);

						// blast the tex coord data into the buffer
						ReadFloats(vertexData.vertexCount * dim, texCoords);

						// unlock the buffer to commit
						vBuffer.Unlock();

						// bind the tex coord buffer
						vertexData.vertexBufferBinding.SetBinding(bindIdx, vBuffer);

						// increment current texcoordset (used for vertex element index) and current bind index
						texCoordSet++;
						bindIdx++;

						break;

				} // switch

				// read the next chunk
				chunkID = ReadChunk();
			} // while

			// skip back so the continuation of the calling loop can look at the next chunk
			// since we already read past it
			Seek(-CHUNK_OVERHEAD_SIZE);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="length"></param>
		protected void Seek(long length)
		{
			if(base.BaseStream is FileStream)
			{
				FileStream fs = base.BaseStream as FileStream;
 
				fs.Seek(length, SeekOrigin.Current);
			}
			else if(base.BaseStream is MemoryStream)
			{
				MemoryStream ms = base.BaseStream as MemoryStream;

				ms.Seek(length, SeekOrigin.Current);
			}
			else
				throw new Exception("Unsupported stream type used to load a mesh.");
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="count"></param>
		/// <param name="dest"></param>
		protected void ReadFloats(int count, IntPtr dest)
		{
			// blast the data into the buffer
			unsafe
			{
				float* pFloats = (float*)dest.ToPointer();

				for(int i = 0; i < count; i++)
					*pFloats++ = ReadSingle();
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="count"></param>
		/// <param name="dest"></param>
		protected void ReadInts(int count, IntPtr dest)
		{
			// blast the data into the buffer
			unsafe
			{
				int* pInts = (int*)dest.ToPointer();

				for(int i = 0; i < count; i++)
					*pInts++ = ReadInt32();
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="count"></param>
		/// <param name="dest"></param>
		protected void ReadShorts(int count, IntPtr dest)
		{
			// blast the data into the buffer
			unsafe
			{
				short* pShorts = (short*)dest.ToPointer();

				for(int i = 0; i < count; i++)
					*pShorts++ = ReadInt16();
			}
		}

		#endregion
		
		#region Properties
		
		#endregion

	}

	/// <summary>
	///		Values that mark data chunks in the .mesh file.
	/// </summary>
	public enum MeshChunkID : ushort
	{
		Header                = 0x1000,
		// char*          version           : Version number check
		Mesh                = 0x3000,
		// bool skeletallyAnimated   // important flag which affects h/w buffer policies
		// Optional M_GEOMETRY chunk
		SubMesh             = 0x4000, 
		// char* materialName
		// bool useSharedVertices
		// unsigned int indexCount
		// bool indexes32Bit
		// unsigned int* faceVertexIndices (indexCount)
		// OR
		// unsigned short* faceVertexIndices (indexCount)
		// M_GEOMETRY chunk (Optional: present only if useSharedVertices = false)
		SubMeshBoneAssignment = 0x4100,
		// Optional bone weights (repeating section)
		// unsigned int vertexIndex;
		// unsigned short boneIndex;
		// Real weight;
		Geometry          = 0x5000, // NB this chunk is embedded within M_MESH and M_SUBMESH
		// unsigned int vertexCount
		// Real* pVertices (x, y, z order x numVertices)
		GeometryNormals = 0x5100,    //(Optional)
		// Real* pNormals (x, y, z order x numVertices)
		GeometryColors = 0x5200,    //(Optional)
		// unsigned long* pColours (RGBA 8888 format x numVertices)
		GeometryTexCoords = 0x5300,    //(Optional, REPEATABLE, each one adds an extra set)
		// unsigned short dimensions    (1 for 1D, 2 for 2D, 3 for 3D)
		// Real* pTexCoords  (u [v] [w] order, dimensions x numVertices)
		MeshSkeletonLink = 0x6000,
		// Optional link to skeleton
		// char* skeletonName           : name of .skeleton to use
		MeshBoneAssignment = 0x7000,
		// Optional bone weights (repeating section)
		// unsigned int vertexIndex;
		// unsigned short boneIndex;
		// Real weight;
		MeshLOD = 0x8000,
		// Optional LOD information
		// unsigned short numLevels;
		// bool manual;  (true for manual alternate meshes, false for generated)
		MeshLODUsage = 0x8100,
		// Repeating section, ordered in increasing depth
		// NB LOD 0 (full detail from 0 depth) is omitted
		// Real fromSquaredDepth;
		MeshLODManual = 0x8110,
		// Required if M_MESH_LOD section manual = true
		// String manualMeshName;
		MeshLODGenerated = 0x8120,
		// Required if M_MESH_LOD section manual = false
		// Repeating section (1 per submesh)
		// unsigned int indexCount;
		// bool indexes32Bit
		// unsigned short* faceIndexes;  (indexCount)
		// OR
		// unsigned int* faceIndexes;  (indexCount)
		MeshBounds = 0x9000,
		// Real minx, miny, minz
		// Real maxx, maxy, maxz
		// Real radius
                    
                    
        
		// --> Phased out definitions
		// Definitions required for loading 1.0 meshes, but no longer supported 
		// (see 1.0 format below)
		Material            = 0x2000,
		// char* name 
		// AMBIENT
		// Real r, g, b
		// DIFFUSE
		// Real r, g, b
		// SPECULAR
		// Real r, g, b
		// SHININESS
		// Real val;
		TextureLayer    = 0x2200 // optional, repeat per layer
		// char* name 
		// TODO - scale, offset, effects

	};
}
