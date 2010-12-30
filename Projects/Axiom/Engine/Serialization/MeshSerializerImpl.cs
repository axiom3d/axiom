#region LGPL License
/*
Axiom Graphics Engine Library
Copyright � 2003-2011 Axiom Project Team

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
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Axiom.Animating;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.Serialization
{
	public class DependencyInfo
	{
		public List<string> meshes = new List<string>();
		public List<string> materials = new List<string>();
		public List<string> skeletons = new List<string>();
	}

	/// <summary>
	/// Summary description for MeshSerializerImpl.
	/// </summary>
	public class MeshSerializerImpl : Serializer
	{
		#region Fields

		/// <summary>
		///		Target mesh for importing/exporting.
		/// </summary>
		protected Mesh mesh;

		/// <summary>
		///		Is this mesh animated with a skeleton?
		/// </summary>
		protected bool isSkeletallyAnimated;

		#endregion Fields

		#region Constructor
		/// <summary>
		///		Default constructor.
		/// </summary>
		public MeshSerializerImpl()
		{
			version = "[MeshSerializer_v1.41]";
		}
		#endregion Constructor

		#region Methods

		/// <summary>
		///		Exports a mesh to the file specified.
		/// </summary>
		/// <remarks>
		///		This method takes an externally created Mesh object, and exports both it
		///		to a .mesh file.
		/// </remarks>
		/// <param name="mesh">Reference to the mesh to export.</param>
		/// <param name="fileName">The destination file name.</param>
		public void ExportMesh( Mesh mesh, string fileName )
		{
			LogManager.Instance.Write( "MeshSerializer writing mesh data to {0} ...", fileName );
			this.mesh = mesh;
			// Check that the mesh has it's bounds set
			if ( mesh.BoundingBox.IsNull || mesh.BoundingSphereRadius == 0.0F )
			{
				throw new AxiomException( "The mesh you supplied does not have its bounds completely defined. Define them first before exporting." );
			}

			FileStream stream = new FileStream( fileName, FileMode.Create );
			try
			{
				BinaryWriter writer = new BinaryWriter( stream );
				WriteFileHeader( writer, version );
				LogManager.Instance.Write( "File header written." );
				LogManager.Instance.Write( "Writing mesh data..." );
				WriteMesh( writer );
				LogManager.Instance.Write( "Mesh data exported." );
			}
			finally
			{
				if ( stream != null )
				{
					stream.Close();
					LogManager.Instance.Write( "MeshSerializer export successful." );
				}

			}
		}

		/// <summary>
		/// Multiverse Extension
		/// </summary>
		/// <param name="stream"></param>
		/// <param name="mesh"></param>
		/// <returns></returns>
		public DependencyInfo GetDependencyInfo( Stream stream, Mesh mesh )
		{
			BinaryReader reader = new BinaryReader( stream, System.Text.Encoding.ASCII );

			// check header
			ReadFileHeader( reader );

			MeshChunkID chunkID = 0;

			// read until the end
			while ( !IsEOF( reader ) )
			{
				chunkID = ReadChunk( reader );
				if ( chunkID == MeshChunkID.DependencyInfo )
				{
					DependencyInfo info = new DependencyInfo();
					ReadDependencyInfo( reader, info );
					return info;
				}
				else
				{
					break;
				}
			}
			return null;
		}

		/// <summary>
		///		Imports mesh data from a .mesh file.
		/// </summary>
		/// <param name="stream">A stream containing the .mesh data.</param>
		/// <param name="mesh">Mesh to populate with the data.</param>
		public void ImportMesh( Stream stream, Mesh mesh )
		{
			this.mesh = mesh;

			BinaryReader reader = new BinaryReader( stream, System.Text.Encoding.ASCII );

			// check header
			ReadFileHeader( reader );

			MeshChunkID chunkID = 0;

			// read until the end
			while ( !IsEOF( reader ) )
			{
				chunkID = ReadChunk( reader );

				switch ( chunkID )
				{
					case MeshChunkID.DependencyInfo: // NOTE: This case and read is not in Ogre, why is it here?
						DependencyInfo info = new DependencyInfo();
						ReadDependencyInfo( reader, info );
						break;
					case MeshChunkID.Mesh:
						ReadMesh( reader );
						break;
				}
			}
		}

		#region Protected

		protected MeshChunkID ReadChunk( BinaryReader reader )
		{
			return (MeshChunkID)ReadFileChunk( reader );
		}

		/// <summary>
		/// Multiuverse Extension
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="depends"></param>
		protected void ReadDependencyInfo( BinaryReader reader, DependencyInfo depends )
		{
			if ( !IsEOF( reader ) )
			{
				// check out the next chunk
				MeshChunkID chunkID = ReadChunk( reader );

				while ( !IsEOF( reader ) &&
					   ( chunkID == MeshChunkID.MeshDependency ||
						chunkID == MeshChunkID.SkeletonDependency ||
						chunkID == MeshChunkID.MaterialDependency ) )
				{
					switch ( chunkID )
					{
						case MeshChunkID.MeshDependency:
							ReadMeshDependency( reader, depends );
							break;
						case MeshChunkID.SkeletonDependency:
							ReadSkeletonDependency( reader, depends );
							break;
						case MeshChunkID.MaterialDependency:
							ReadMaterialDependency( reader, depends );
							break;
					}
				} // while
				if ( !IsEOF( reader ) )
				{
					// skip back so the continuation of the calling loop can look at the next chunk
					// since we already read past it
					Seek( reader, -ChunkOverheadSize );
				}
			}
		}

		protected void ReadMeshDependency( BinaryReader reader, DependencyInfo depends )
		{
			int count = reader.ReadInt16();
			for ( int i = 0; i < count; ++i )
			{
				string name = reader.ReadString();
				depends.meshes.Add( name );
			}
		}

		protected void ReadSkeletonDependency( BinaryReader reader, DependencyInfo depends )
		{
			int count = reader.ReadInt16();
			for ( int i = 0; i < count; ++i )
			{
				string name = reader.ReadString();
				depends.skeletons.Add( name );
			}
		}

		protected void ReadMaterialDependency( BinaryReader reader, DependencyInfo depends )
		{
			int count = reader.ReadInt16();
			for ( int i = 0; i < count; ++i )
			{
				string name = reader.ReadString();
				depends.materials.Add( name );
			}
		}

		protected virtual void ReadSubMeshNameTable( BinaryReader reader )
		{
			if ( !IsEOF( reader ) )
			{
				MeshChunkID chunkID = ReadChunk( reader );

				while ( !IsEOF( reader ) && ( chunkID == MeshChunkID.SubMeshNameTableElement ) )
				{
					// i'm not bothering with the name table business here, I don't see what the purpose is
					// since we can simply name the submesh.  it appears this section always comes after all submeshes
					// are read, so it should be safe
					short index = ReadShort( reader );
					string name = ReadString( reader );

					SubMesh sub = mesh.GetSubMesh( index );

					if ( sub != null )
					{
						sub.Name = name;
					}

					// If we're not end of file get the next chunk ID
					if ( !IsEOF( reader ) )
					{
						chunkID = ReadChunk( reader );
					}
				}

				// backpedal to the start of the chunk
				if ( !IsEOF( reader ) )
				{
					Seek( reader, -ChunkOverheadSize );
				}
			}
		}

		protected virtual void ReadMesh( BinaryReader reader )
		{
			MeshChunkID chunkID;

			// Never automatically build edge lists for this version
			// expect them in the file or not at all
			mesh.AutoBuildEdgeLists = false;

			// is this mesh animated?
			isSkeletallyAnimated = ReadBool( reader );

			// find all sub chunks
			if ( !IsEOF( reader ) )
			{
				chunkID = ReadChunk( reader );

				while ( !IsEOF( reader ) &&
					( chunkID == MeshChunkID.Geometry ||
					chunkID == MeshChunkID.SubMesh ||
					chunkID == MeshChunkID.MeshSkeletonLink ||
					chunkID == MeshChunkID.MeshBoneAssignment ||
					chunkID == MeshChunkID.MeshLOD ||
					chunkID == MeshChunkID.MeshBounds ||
					chunkID == MeshChunkID.SubMeshNameTable ||
					chunkID == MeshChunkID.EdgeLists ||
					chunkID == MeshChunkID.Poses ||
					chunkID == MeshChunkID.Animations ||
					//chunkID == MeshChunkID.TableExtremes ||
					chunkID == MeshChunkID.AttachmentPoint ) )
				{

					switch ( chunkID )
					{
						case MeshChunkID.Geometry:
							mesh.SharedVertexData = new VertexData();

							// read geometry into shared vertex data
							ReadGeometry( reader, mesh.SharedVertexData );

							// TODO: trap errors here
							break;

						case MeshChunkID.SubMesh:
							// read the sub mesh data
							ReadSubMesh( reader );
							break;

						case MeshChunkID.MeshSkeletonLink:
							// read skeleton link
							ReadSkeletonLink( reader );
							break;

						case MeshChunkID.MeshBoneAssignment:
							// read mesh bone assignments
							ReadMeshBoneAssignment( reader );
							break;

						case MeshChunkID.MeshLOD:
							// Handle meshes with LOD
							ReadMeshLodInfo( reader );
							break;

						case MeshChunkID.MeshBounds:
							// read the pre-calculated bounding information
							ReadBoundsInfo( reader );
							break;

						case MeshChunkID.SubMeshNameTable:
							ReadSubMeshNameTable( reader );
							break;

						case MeshChunkID.EdgeLists:
							ReadEdgeList( reader );
							break;

						case MeshChunkID.Poses:
							ReadPoses( reader );
							break;

						case MeshChunkID.Animations:
							ReadAnimations( reader );
							break;

						//case MeshChunkID.TableExtremes:
						//    ReadExtremes( reader );
						//    break;

						case MeshChunkID.AttachmentPoint:
							ReadAttachmentPoint( reader );
							break;
					} // switch

					// grab the next chunk
					if ( !IsEOF( reader ) )
					{
						chunkID = ReadChunk( reader );
					}
				} // while

				// backpedal to the start of the chunk
				if ( !IsEOF( reader ) )
				{
					Seek( reader, -ChunkOverheadSize );
				}
			}
		}

		protected void WriteMesh( BinaryWriter writer )
		{
			// cache header location
			long start_offset = writer.Seek( 0, SeekOrigin.Current );

			// Header
			WriteChunk( writer, MeshChunkID.Mesh, 0 );

			// bool skeletallyAnimated
			WriteBool( writer, mesh.HasSkeleton );

			// Write shared geometry
			if ( mesh.SharedVertexData != null )
			{
				WriteGeometry( writer, mesh.SharedVertexData );
			}

			// Write Submeshes
			for ( int i = 0; i < mesh.SubMeshCount; ++i )
			{
				SubMesh subMesh = mesh.GetSubMesh( i );
				LogManager.Instance.Write( "Writing submesh {0} ...", subMesh.Name );
				WriteSubMesh( writer, subMesh );
				LogManager.Instance.Write( "Submesh exported." );
			}

			// Write skeleton info if required
			if ( mesh.Skeleton != null )
			{
				// Write skeleton link
				LogManager.Instance.Write( "Exporting skeleton link..." );
				WriteSkeletonLink( writer );
				LogManager.Instance.Write( "Skeleton link exported." );

				// Write bone assignments
				LogManager.Instance.Write( "Exporting shared geometry bone assignments..." );
				Dictionary<int, List<VertexBoneAssignment>> weights = mesh.BoneAssignmentList;
				foreach ( int v in weights.Keys )
				{
					List<VertexBoneAssignment> vbaList = weights[ v ];
					foreach ( VertexBoneAssignment vba in vbaList )
						WriteMeshBoneAssignment( writer, vba );
				}
				LogManager.Instance.Write( "Shared geometry bone assignments exported." );
			}

			// Write LOD data if any
			if ( mesh.LodLevelCount > 1 )
			{
				LogManager.Instance.Write( "Exporting LOD information..." );
				this.WriteMeshLodInfo( writer );
				LogManager.Instance.Write( "LOD information exported." );
			}

			// Write Bounds information
			LogManager.Instance.Write( "Exporting bounds Information..." );
			WriteMeshBounds( writer );
			LogManager.Instance.Write( "Bounds information exported." );

			// Write submesh name table
			LogManager.Instance.Write( "Exporting submesh name table..." );
			WriteSubMeshNameTable( writer );
			LogManager.Instance.Write( "Submesh name table exported." );

			// Write edge lists
			LogManager.Instance.Write( "Exporting edge lists..." );
			//WriteEdgeLists( writer );
			LogManager.Instance.Write( "Edge lists exported." );

			//Write morph animation
			LogManager.Instance.Write( "Exporting morph animations..." );
			if ( mesh.PoseList.Count > 0 )
				WritePoses( writer );
			if ( mesh.HasVertexAnimation )
				WriteAnimations( writer );
			LogManager.Instance.Write( "Morph animations exported." );

			// Write submesh extremes
			LogManager.Instance.Write( "Exporting submesh extremes..." );
			//WriteExtremes( writer );
			LogManager.Instance.Write( "Submesh extremes information exported." );

			// Write Attachment Points
			LogManager.Instance.Write( "Exporting attachment points..." );
			foreach ( AttachmentPoint ap in mesh.AttachmentPoints )
				WriteAttachmentPoint( writer, ap );
			LogManager.Instance.Write( "Attachment points exported." );

			// Some ending stuff...
			long end_offset = writer.Seek( 0, SeekOrigin.Current );
			writer.Seek( (int)start_offset, SeekOrigin.Begin );
			WriteChunk( writer, MeshChunkID.Mesh, (int)( end_offset - start_offset ) );
			writer.Seek( (int)end_offset, SeekOrigin.Begin );
		}

		protected void WriteSubMesh( BinaryWriter writer, SubMesh subMesh )
		{
			// cache header location
			long start_offset = writer.Seek( 0, SeekOrigin.Current );

			// Header 
			WriteChunk( writer, MeshChunkID.SubMesh, 0 );

			// Name
			WriteString( writer, subMesh.MaterialName );

			// useSharedVertices
			WriteBool( writer, subMesh.useSharedVertices );

			// indexCount
			WriteUInt( writer, (uint)subMesh.indexData.indexCount );

			// indexes32bit
			bool indexes32bit = ( subMesh.indexData.indexBuffer.Type == IndexType.Size32 );
			WriteBool( writer, indexes32bit );

			IntPtr buf = subMesh.indexData.indexBuffer.Lock( BufferLocking.Discard );
			try
			{
				if ( indexes32bit )
					WriteInts( writer, subMesh.indexData.indexCount, buf );
				else
					WriteShorts( writer, subMesh.indexData.indexCount, buf );
			}
			finally
			{
				subMesh.indexData.indexBuffer.Unlock();
			}
			if ( !subMesh.useSharedVertices )
				WriteGeometry( writer, subMesh.vertexData );
			WriteSubMeshOperation( writer, subMesh );

			Dictionary<int, List<VertexBoneAssignment>> weights = subMesh.BoneAssignmentList;
			foreach ( int v in weights.Keys )
			{
				List<VertexBoneAssignment> vbaList = weights[ v ];
				foreach ( VertexBoneAssignment vba in vbaList )
					WriteSubMeshBoneAssignment( writer, vba );
			}

			// Write the texture alias (not currently supported)

			long end_offset = writer.Seek( 0, SeekOrigin.Current );
			writer.Seek( (int)start_offset, SeekOrigin.Begin );
			WriteChunk( writer, MeshChunkID.SubMesh, (int)( end_offset - start_offset ) );
			writer.Seek( (int)end_offset, SeekOrigin.Begin );
		}

		protected void WriteSubMeshBoneAssignment( BinaryWriter writer, VertexBoneAssignment vba )
		{
			long start_offset = writer.Seek( 0, SeekOrigin.Current );
			WriteChunk( writer, MeshChunkID.SubMeshBoneAssignment, 0 );

			WriteUInt( writer, (uint)vba.vertexIndex );
			WriteUShort( writer, (ushort)vba.boneIndex );
			WriteFloat( writer, vba.weight );

			long end_offset = writer.Seek( 0, SeekOrigin.Current );
			writer.Seek( (int)start_offset, SeekOrigin.Begin );
			WriteChunk( writer, MeshChunkID.SubMeshBoneAssignment, (int)( end_offset - start_offset ) );
			writer.Seek( (int)end_offset, SeekOrigin.Begin );
		}

		protected void WriteMeshBoneAssignment( BinaryWriter writer, VertexBoneAssignment vba )
		{
			long start_offset = writer.Seek( 0, SeekOrigin.Current );
			WriteChunk( writer, MeshChunkID.MeshBoneAssignment, 0 );

			WriteUInt( writer, (uint)vba.vertexIndex );
			WriteUShort( writer, (ushort)vba.boneIndex );
			WriteFloat( writer, vba.weight );

			long end_offset = writer.Seek( 0, SeekOrigin.Current );
			writer.Seek( (int)start_offset, SeekOrigin.Begin );
			WriteChunk( writer, MeshChunkID.MeshBoneAssignment, (int)( end_offset - start_offset ) );
			writer.Seek( (int)end_offset, SeekOrigin.Begin );
		}

		protected void WriteSubMeshOperation( BinaryWriter writer, SubMesh subMesh )
		{
			long start_offset = writer.Seek( 0, SeekOrigin.Current );
			WriteChunk( writer, MeshChunkID.SubMeshOperation, 0 );

			WriteUShort( writer, (ushort)subMesh.operationType );

			long end_offset = writer.Seek( 0, SeekOrigin.Current );
			writer.Seek( (int)start_offset, SeekOrigin.Begin );
			WriteChunk( writer, MeshChunkID.SubMeshOperation, (int)( end_offset - start_offset ) );
			writer.Seek( (int)end_offset, SeekOrigin.Begin );
		}

		protected void WriteGeometry( BinaryWriter writer, VertexData vertexData )
		{
			long start_offset = writer.Seek( 0, SeekOrigin.Current );
			WriteChunk( writer, MeshChunkID.Geometry, 0 );

			WriteUInt( writer, (uint)vertexData.vertexCount );
			WriteGeometryVertexDeclaration( writer, vertexData.vertexDeclaration );
			for ( short i = 0; i < vertexData.vertexBufferBinding.BindingCount; ++i )
				WriteGeometryVertexBuffer( writer, i, vertexData.vertexBufferBinding.GetBuffer( i ) );

			long end_offset = writer.Seek( 0, SeekOrigin.Current );
			writer.Seek( (int)start_offset, SeekOrigin.Begin );
			WriteChunk( writer, MeshChunkID.Geometry, (int)( end_offset - start_offset ) );
			writer.Seek( (int)end_offset, SeekOrigin.Begin );
		}

		protected void WriteGeometryVertexDeclaration( BinaryWriter writer, VertexDeclaration vertexDeclaration )
		{
			long start_offset = writer.Seek( 0, SeekOrigin.Current );
			WriteChunk( writer, MeshChunkID.GeometryVertexDeclaration, 0 );

			for ( int i = 0; i < vertexDeclaration.ElementCount; ++i )
				WriteGeometryVertexElement( writer, vertexDeclaration.GetElement( i ) );

			long end_offset = writer.Seek( 0, SeekOrigin.Current );
			writer.Seek( (int)start_offset, SeekOrigin.Begin );
			WriteChunk( writer, MeshChunkID.GeometryVertexDeclaration, (int)( end_offset - start_offset ) );
			writer.Seek( (int)end_offset, SeekOrigin.Begin );
		}

		protected void WriteGeometryVertexElement( BinaryWriter writer, VertexElement vertexElement )
		{
			long start_offset = writer.Seek( 0, SeekOrigin.Current );
			WriteChunk( writer, MeshChunkID.GeometryVertexElement, 0 );

			WriteUShort( writer, (ushort)vertexElement.Source );
			WriteUShort( writer, (ushort)vertexElement.Type );
			WriteUShort( writer, (ushort)vertexElement.Semantic );
			WriteUShort( writer, (ushort)vertexElement.Offset );
			WriteUShort( writer, (ushort)vertexElement.Index );

			long end_offset = writer.Seek( 0, SeekOrigin.Current );
			writer.Seek( (int)start_offset, SeekOrigin.Begin );
			WriteChunk( writer, MeshChunkID.GeometryVertexElement, (int)( end_offset - start_offset ) );
			writer.Seek( (int)end_offset, SeekOrigin.Begin );
		}

		protected void WriteGeometryVertexBuffer( BinaryWriter writer, short bindIndex, HardwareVertexBuffer vertexBuffer )
		{
			long start_offset = writer.Seek( 0, SeekOrigin.Current );
			WriteChunk( writer, MeshChunkID.GeometryVertexBuffer, 0 );

			WriteShort( writer, bindIndex );
			WriteShort( writer, (short)vertexBuffer.VertexSize );
			IntPtr buf = vertexBuffer.Lock( BufferLocking.Discard );
			try
			{
				WriteGeometryVertexBufferData( writer, vertexBuffer.Size, buf );
			}
			finally
			{
				vertexBuffer.Unlock();
			}

			long end_offset = writer.Seek( 0, SeekOrigin.Current );
			writer.Seek( (int)start_offset, SeekOrigin.Begin );
			WriteChunk( writer, MeshChunkID.GeometryVertexBuffer, (int)( end_offset - start_offset ) );
			writer.Seek( (int)end_offset, SeekOrigin.Begin );
		}

		protected void WriteGeometryVertexBufferData( BinaryWriter writer, int count, IntPtr buf )
		{
			long start_offset = writer.Seek( 0, SeekOrigin.Current );
			WriteChunk( writer, MeshChunkID.GeometryVertexBufferData, 0 );

			WriteBytes( writer, count, buf );

			long end_offset = writer.Seek( 0, SeekOrigin.Current );
			writer.Seek( (int)start_offset, SeekOrigin.Begin );
			WriteChunk( writer, MeshChunkID.GeometryVertexBufferData, (int)( end_offset - start_offset ) );
			writer.Seek( (int)end_offset, SeekOrigin.Begin );
		}

		protected void WriteSkeletonLink( BinaryWriter writer )
		{
			long start_offset = writer.Seek( 0, SeekOrigin.Current );
			WriteChunk( writer, MeshChunkID.MeshSkeletonLink, 0 );

			WriteString( writer, mesh.SkeletonName );

			long end_offset = writer.Seek( 0, SeekOrigin.Current );
			writer.Seek( (int)start_offset, SeekOrigin.Begin );
			WriteChunk( writer, MeshChunkID.MeshSkeletonLink, (int)( end_offset - start_offset ) );
			writer.Seek( (int)end_offset, SeekOrigin.Begin );
		}

		protected void WriteMeshLodInfo( BinaryWriter writer )
		{
			long start_offset = writer.Seek( 0, SeekOrigin.Current );
			this.WriteMeshLodSummary( writer );

			// Start from 1 to skip the LOD 0 entry
			for ( int i = 1; i < mesh.LodLevelCount; ++i )
			{
				MeshLodUsage usage = mesh.GetLodLevel( i );
				WriteMeshLodUsage( writer, usage, i );
			}

			long end_offset = writer.Seek( 0, SeekOrigin.Current );
			writer.Seek( (int)start_offset, SeekOrigin.Begin );
			WriteChunk( writer, MeshChunkID.MeshLOD, (int)( end_offset - start_offset ) );
			writer.Seek( (int)end_offset, SeekOrigin.Begin );
		}

		protected virtual void WriteMeshLodSummary( BinaryWriter writer )
		{
			WriteChunk( writer, MeshChunkID.MeshLOD, 0 );
			WriteString( writer, mesh.LodStrategy.Name );
			WriteShort( writer, (short)mesh.LodLevelCount );
			WriteBool( writer, mesh.IsLodManual );
		}

		protected void WriteMeshLodUsage( BinaryWriter writer, MeshLodUsage usage, int usageIndex )
		{
			long start_offset = writer.Seek( 0, SeekOrigin.Current );
			WriteChunk( writer, MeshChunkID.MeshLODUsage, 0 );

			if ( mesh.IsLodManual )
				WriteMeshLodManual( writer, usage );
			else
			{
				for ( int i = 0; i < mesh.SubMeshCount; ++i )
				{
					SubMesh subMesh = mesh.GetSubMesh( i );
					WriteMeshLodGenerated( writer, subMesh, usageIndex );
				}
			}

			long end_offset = writer.Seek( 0, SeekOrigin.Current );
			writer.Seek( (int)start_offset, SeekOrigin.Begin );
			WriteChunk( writer, MeshChunkID.MeshLODUsage, (int)( end_offset - start_offset ) );
			writer.Seek( (int)end_offset, SeekOrigin.Begin );
		}

		protected void WriteMeshLodManual( BinaryWriter writer, MeshLodUsage usage )
		{
			long start_offset = writer.Seek( 0, SeekOrigin.Current );
			WriteChunk( writer, MeshChunkID.MeshLODManual, 0 );

			WriteString( writer, usage.ManualName );

			long end_offset = writer.Seek( 0, SeekOrigin.Current );
			writer.Seek( (int)start_offset, SeekOrigin.Begin );
			WriteChunk( writer, MeshChunkID.MeshLODManual, (int)( end_offset - start_offset ) );
			writer.Seek( (int)end_offset, SeekOrigin.Begin );
		}

		protected void WriteMeshLodGenerated( BinaryWriter writer, SubMesh subMesh, int usageIndex )
		{
			long start_offset = writer.Seek( 0, SeekOrigin.Current );
			WriteChunk( writer, MeshChunkID.MeshLODGenerated, 0 );

			IndexData indexData = subMesh.lodFaceList[ usageIndex - 1 ];
			bool indexes32bit = ( indexData.indexBuffer.Type == IndexType.Size32 );

			WriteInt( writer, indexData.indexCount );
			WriteBool( writer, indexes32bit );

			// lock the buffer
			IntPtr data = indexData.indexBuffer.Lock( BufferLocking.ReadOnly );

			if ( indexes32bit )
				WriteInts( writer, indexData.indexCount, data );
			else
				WriteShorts( writer, indexData.indexCount, data );

			indexData.indexBuffer.Unlock();

			long end_offset = writer.Seek( 0, SeekOrigin.Current );
			writer.Seek( (int)start_offset, SeekOrigin.Begin );
			WriteChunk( writer, MeshChunkID.MeshLODGenerated, (int)( end_offset - start_offset ) );
			writer.Seek( (int)end_offset, SeekOrigin.Begin );
		}

		protected void WriteMeshBounds( BinaryWriter writer )
		{
			long start_offset = writer.Seek( 0, SeekOrigin.Current );
			WriteChunk( writer, MeshChunkID.MeshBounds, 0 );

			WriteVector3( writer, mesh.BoundingBox.Minimum );
			WriteVector3( writer, mesh.BoundingBox.Maximum );
			WriteFloat( writer, mesh.BoundingSphereRadius );

			// Save chunk size back into Header
			long end_offset = writer.Seek( 0, SeekOrigin.Current );
			writer.Seek( (int)start_offset, SeekOrigin.Begin );
			WriteChunk( writer, MeshChunkID.MeshBounds, (int)( end_offset - start_offset ) );
			writer.Seek( (int)end_offset, SeekOrigin.Begin );
		}

		protected void WriteSubMeshNameTable( BinaryWriter writer )
		{
			// cache header location
			long start_offset = writer.Seek( 0, SeekOrigin.Current );

			// Header
			WriteChunk( writer, MeshChunkID.SubMeshNameTable, 0 );

			// Loop through and save out the index and names
			for ( short i = 0; i < mesh.SubMeshCount; ++i )
			{
				SubMesh subMesh = mesh.GetSubMesh( i );
				WriteSubMeshNameTableElement( writer, i, subMesh.Name );
			}

			// Save chunk size back into Header
			long end_offset = writer.Seek( 0, SeekOrigin.Current );
			writer.Seek( (int)start_offset, SeekOrigin.Begin );
			WriteChunk( writer, MeshChunkID.SubMeshNameTable, (int)( end_offset - start_offset ) );
			writer.Seek( (int)end_offset, SeekOrigin.Begin );
		}

		protected void WriteSubMeshNameTableElement( BinaryWriter writer, short i, string name )
		{
			long start_offset = writer.Seek( 0, SeekOrigin.Current );
			WriteChunk( writer, MeshChunkID.SubMeshNameTableElement, 0 );

			WriteShort( writer, i );
			WriteString( writer, name );

			long end_offset = writer.Seek( 0, SeekOrigin.Current );
			writer.Seek( (int)start_offset, SeekOrigin.Begin );
			WriteChunk( writer, MeshChunkID.SubMeshNameTableElement, (int)( end_offset - start_offset ) );
			writer.Seek( (int)end_offset, SeekOrigin.Begin );
		}

		protected void WritePoses( BinaryWriter writer )
		{
			long start_offset = writer.Seek( 0, SeekOrigin.Current );
			WriteChunk( writer, MeshChunkID.Poses, 0 );

			foreach ( Pose pose in mesh.PoseList )
				WritePose( writer, pose );

			long end_offset = writer.Seek( 0, SeekOrigin.Current );
			writer.Seek( (int)start_offset, SeekOrigin.Begin );
			WriteChunk( writer, MeshChunkID.Poses, (int)( end_offset - start_offset ) );
			writer.Seek( (int)end_offset, SeekOrigin.Begin );
		}

		protected void WritePose( BinaryWriter writer, Pose pose )
		{
			long start_offset = writer.Seek( 0, SeekOrigin.Current );
			WriteChunk( writer, MeshChunkID.Pose, 0 );

			WriteString( writer, pose.Name );
			WriteUShort( writer, pose.Target );
			foreach ( KeyValuePair<int, Vector3> kvp in pose.VertexOffsetMap )
				WritePoseVertex( writer, kvp.Key, kvp.Value );

			long end_offset = writer.Seek( 0, SeekOrigin.Current );
			writer.Seek( (int)start_offset, SeekOrigin.Begin );
			WriteChunk( writer, MeshChunkID.Pose, (int)( end_offset - start_offset ) );
			writer.Seek( (int)end_offset, SeekOrigin.Begin );
		}

		protected void WritePoseVertex( BinaryWriter writer, int vertexId, Vector3 offset )
		{
			long start_offset = writer.Seek( 0, SeekOrigin.Current );
			WriteChunk( writer, MeshChunkID.PoseVertex, 0 );

			WriteInt( writer, vertexId );
			WriteVector3( writer, offset );

			long end_offset = writer.Seek( 0, SeekOrigin.Current );
			writer.Seek( (int)start_offset, SeekOrigin.Begin );
			WriteChunk( writer, MeshChunkID.PoseVertex, (int)( end_offset - start_offset ) );
			writer.Seek( (int)end_offset, SeekOrigin.Begin );
		}

		protected void WriteAnimations( BinaryWriter writer )
		{
			long start_offset = writer.Seek( 0, SeekOrigin.Current );
			WriteChunk( writer, MeshChunkID.Animations, 0 );

			for ( ushort animIndex = 0; animIndex < mesh.AnimationCount; ++animIndex )
			{
				Animation anim = mesh.GetAnimation( animIndex );
				WriteAnimation( writer, anim );
			}

			long end_offset = writer.Seek( 0, SeekOrigin.Current );
			writer.Seek( (int)start_offset, SeekOrigin.Begin );
			WriteChunk( writer, MeshChunkID.Animations, (int)( end_offset - start_offset ) );
			writer.Seek( (int)end_offset, SeekOrigin.Begin );
		}

		protected void WriteAnimation( BinaryWriter writer, Animation anim )
		{
			long start_offset = writer.Seek( 0, SeekOrigin.Current );
			WriteChunk( writer, MeshChunkID.Animation, 0 );

			WriteString( writer, anim.Name );
			WriteFloat( writer, anim.Length );
			foreach ( VertexAnimationTrack track in anim.VertexTracks.Values )
				WriteAnimationTrack( writer, track );

			long end_offset = writer.Seek( 0, SeekOrigin.Current );
			writer.Seek( (int)start_offset, SeekOrigin.Begin );
			WriteChunk( writer, MeshChunkID.Animation, (int)( end_offset - start_offset ) );
			writer.Seek( (int)end_offset, SeekOrigin.Begin );
		}

		protected void WriteAnimationTrack( BinaryWriter writer, VertexAnimationTrack track )
		{
			long start_offset = writer.Seek( 0, SeekOrigin.Current );
			WriteChunk( writer, MeshChunkID.AnimationTrack, 0 );

			WriteUShort( writer, (ushort)track.AnimationType );
			WriteUShort( writer, track.Handle );
			foreach ( KeyFrame keyFrame in track.KeyFrames )
			{
				if ( keyFrame is VertexMorphKeyFrame )
				{
					VertexMorphKeyFrame vmkf = keyFrame as VertexMorphKeyFrame;
					WriteMorphKeyframe( writer, vmkf );
				}
				else if ( keyFrame is VertexPoseKeyFrame )
				{
					VertexPoseKeyFrame vpkf = keyFrame as VertexPoseKeyFrame;
					WritePoseKeyframe( writer, vpkf );
				}
			}

			long end_offset = writer.Seek( 0, SeekOrigin.Current );
			writer.Seek( (int)start_offset, SeekOrigin.Begin );
			WriteChunk( writer, MeshChunkID.AnimationTrack, (int)( end_offset - start_offset ) );
			writer.Seek( (int)end_offset, SeekOrigin.Begin );
		}

		protected void WriteMorphKeyframe( BinaryWriter writer, VertexMorphKeyFrame keyFrame )
		{
			long start_offset = writer.Seek( 0, SeekOrigin.Current );
			WriteChunk( writer, MeshChunkID.AnimationMorphKeyframe, 0 );

			WriteFloat( writer, keyFrame.Time );
			HardwareVertexBuffer vBuffer = keyFrame.VertexBuffer;
			IntPtr vBufferPtr = vBuffer.Lock( BufferLocking.ReadOnly );
			WriteFloats( writer, vBuffer.VertexCount * 3, vBufferPtr );
			vBuffer.Unlock();

			long end_offset = writer.Seek( 0, SeekOrigin.Current );
			writer.Seek( (int)start_offset, SeekOrigin.Begin );
			WriteChunk( writer, MeshChunkID.AnimationMorphKeyframe, (int)( end_offset - start_offset ) );
			writer.Seek( (int)end_offset, SeekOrigin.Begin );
		}

		protected void WritePoseKeyframe( BinaryWriter writer, VertexPoseKeyFrame keyFrame )
		{
			long start_offset = writer.Seek( 0, SeekOrigin.Current );
			WriteChunk( writer, MeshChunkID.AnimationPoseKeyframe, 0 );

			WriteFloat( writer, keyFrame.Time );
			foreach ( PoseRef poseRef in keyFrame.PoseRefs )
				WriteAnimationPoseRef( writer, poseRef );

			long end_offset = writer.Seek( 0, SeekOrigin.Current );
			writer.Seek( (int)start_offset, SeekOrigin.Begin );
			WriteChunk( writer, MeshChunkID.AnimationPoseKeyframe, (int)( end_offset - start_offset ) );
			writer.Seek( (int)end_offset, SeekOrigin.Begin );
		}

		protected void WriteAnimationPoseRef( BinaryWriter writer, PoseRef poseRef )
		{
			long start_offset = writer.Seek( 0, SeekOrigin.Current );
			WriteChunk( writer, MeshChunkID.AnimationPoseRef, 0 );

			WriteUShort( writer, poseRef.PoseIndex );
			WriteFloat( writer, poseRef.Influence );

			long end_offset = writer.Seek( 0, SeekOrigin.Current );
			writer.Seek( (int)start_offset, SeekOrigin.Begin );
			WriteChunk( writer, MeshChunkID.AnimationPoseRef, (int)( end_offset - start_offset ) );
			writer.Seek( (int)end_offset, SeekOrigin.Begin );
		}

		protected void WriteAttachmentPoint( BinaryWriter writer, AttachmentPoint ap )
		{
			long start_offset = writer.Seek( 0, SeekOrigin.Current );
			WriteChunk( writer, MeshChunkID.AttachmentPoint, 0 );

			WriteString( writer, ap.Name );
			WriteVector3( writer, ap.Position );
			WriteQuat( writer, ap.Orientation );

			long end_offset = writer.Seek( 0, SeekOrigin.Current );
			writer.Seek( (int)start_offset, SeekOrigin.Begin );
			WriteChunk( writer, MeshChunkID.AttachmentPoint, (int)( end_offset - start_offset ) );
			writer.Seek( (int)end_offset, SeekOrigin.Begin );
		}

		protected virtual void ReadSubMesh( BinaryReader reader )
		{
			MeshChunkID chunkID;

			SubMesh subMesh = mesh.CreateSubMesh();

			// get the material name
			string materialName = ReadString( reader );

			MeshManager.Instance.FireProcessMaterialName( this.mesh, materialName );

			subMesh.MaterialName = materialName;

			// use shared vertices?
			subMesh.useSharedVertices = ReadBool( reader );

			subMesh.indexData.indexStart = 0;
			subMesh.indexData.indexCount = ReadInt( reader );

			// does this use 32 bit index buffer
			bool idx32bit = ReadBool( reader );

			HardwareIndexBuffer idxBuffer = null;

			if ( idx32bit )
			{
				// create the index buffer
				idxBuffer =
					HardwareBufferManager.Instance.
					CreateIndexBuffer(
					IndexType.Size32,
					subMesh.indexData.indexCount,
					mesh.IndexBufferUsage,
					mesh.UseIndexShadowBuffer );

				IntPtr indices = idxBuffer.Lock( BufferLocking.Discard );

				// read the ints into the buffer data
				ReadInts( reader, subMesh.indexData.indexCount, indices );

				// unlock the buffer to commit					
				idxBuffer.Unlock();
			}
			else
			{ // 16-bit
				// create the index buffer
				idxBuffer =
					HardwareBufferManager.Instance.
					CreateIndexBuffer(
					IndexType.Size16,
					subMesh.indexData.indexCount,
					mesh.IndexBufferUsage,
					mesh.UseIndexShadowBuffer );

				IntPtr indices = idxBuffer.Lock( BufferLocking.Discard );

				// read the shorts into the buffer data
				ReadShorts( reader, subMesh.indexData.indexCount, indices );

				idxBuffer.Unlock();
			}

			// save the index buffer
			subMesh.indexData.indexBuffer = idxBuffer;

			// Geometry chunk (optional, only present if useSharedVertices = false)
			if ( !subMesh.useSharedVertices )
			{
				chunkID = ReadChunk( reader );

				if ( chunkID != MeshChunkID.Geometry )
				{
					throw new AxiomException( "Missing geometry data in mesh file." );
				}

				subMesh.vertexData = new VertexData();

				// read the geometry data
				ReadGeometry( reader, subMesh.vertexData );
			}

			// get the next chunkID
			chunkID = ReadChunk( reader );

			// walk through all the bone assignments for this submesh
			while ( !IsEOF( reader ) &&
				( chunkID == MeshChunkID.SubMeshBoneAssignment ||
				chunkID == MeshChunkID.SubMeshOperation ) )
			{

				switch ( chunkID )
				{
					case MeshChunkID.SubMeshBoneAssignment:
						ReadSubMeshBoneAssignment( reader, subMesh );
						break;

					case MeshChunkID.SubMeshOperation:
						ReadSubMeshOperation( reader, subMesh );
						break;
				}

				// read the next chunkID
				if ( !IsEOF( reader ) )
				{
					chunkID = ReadChunk( reader );
				}
			} // while

			// walk back to the beginning of the last chunk ID read since
			// we already moved past it and it wasnt of interest to us
			if ( !IsEOF( reader ) )
			{
				Seek( reader, -ChunkOverheadSize );
			}
		}

		protected virtual void ReadSubMeshOperation( BinaryReader reader, SubMesh sub )
		{
			sub.operationType = (OperationType)ReadShort( reader );
		}

		protected virtual void ReadGeometry( BinaryReader reader, VertexData data )
		{
			data.vertexStart = 0;
			data.vertexCount = ReadInt( reader );

			// find optional geometry chunks
			if ( !IsEOF( reader ) )
			{
				MeshChunkID chunkID = ReadChunk( reader );

				while ( !IsEOF( reader ) &&
					( chunkID == MeshChunkID.GeometryVertexDeclaration ||
					chunkID == MeshChunkID.GeometryVertexBuffer ) )
				{

					switch ( chunkID )
					{
						case MeshChunkID.GeometryVertexDeclaration:
							ReadGeometryVertexDeclaration( reader, data );
							break;

						case MeshChunkID.GeometryVertexBuffer:
							ReadGeometryVertexBuffer( reader, data );
							break;
					}

					// get the next chunk
					if ( !IsEOF( reader ) )
					{
						chunkID = ReadChunk( reader );
					}
				}

				if ( !IsEOF( reader ) )
				{
					// backpedal to start of non-submesh chunk
					Seek( reader, -ChunkOverheadSize );
				}

				// TODO : Implement color conversions
				// Perform any necessary color conversion for an active rendersystem
				//if ( Root.Instance != null && Root.Instance.RenderSystem != null )
				//{
				//    // We don't know the source type if it's VET_COLOUR, but assume ARGB
				//    // since that's the most common. Won't get used unless the mesh is
				//    // ambiguous anyway, which will have been warned about in the log
				//    data.ConvertPackedColor( VertexElementType.Color, VertexElement.BestColorVertexElementType );
				//}
			}
		}

		protected virtual void ReadGeometryVertexDeclaration( BinaryReader reader, VertexData data )
		{
			// find optional geometry chunks
			if ( !IsEOF( reader ) )
			{
				MeshChunkID chunkID = ReadChunk( reader );

				while ( !IsEOF( reader ) &&
					( chunkID == MeshChunkID.GeometryVertexElement ) )
				{

					switch ( chunkID )
					{
						case MeshChunkID.GeometryVertexElement:
							ReadGeometryVertexElement( reader, data );
							break;
					}

					// get the next chunk
					if ( !IsEOF( reader ) )
					{
						chunkID = ReadChunk( reader );
					}
				}

				if ( !IsEOF( reader ) )
				{
					// backpedal to start of non-submesh chunk
					Seek( reader, -ChunkOverheadSize );
				}
			}
		}

		protected virtual void ReadGeometryVertexElement( BinaryReader reader, VertexData data )
		{
			short source = ReadShort( reader );
			VertexElementType type = (VertexElementType)ReadUShort( reader );
			VertexElementSemantic semantic = (VertexElementSemantic)ReadUShort( reader );
			short offset = ReadShort( reader );
			short index = ReadShort( reader );

			// add the element to the declaration for the current vertex data
			data.vertexDeclaration.AddElement( source, offset, type, semantic, index );

			if ( type == VertexElementType.Color )
			{
				LogManager.Instance.Write( "Warning: VET_COLOUR element type is deprecated, you should use " +
										   "one of the more specific types to indicate the byte order. " +
										   "Use OgreMeshUpgrade on {0} as soon as possible. ", mesh.Name );
			}
		}

		protected virtual void ReadGeometryVertexBuffer( BinaryReader reader, VertexData data )
		{
			// Index to bind this buffer to
			short bindIdx = ReadShort( reader );

			// Per-vertex size, must agree with declaration at this index
			short vertexSize = ReadShort( reader );

			// check for vertex data header
			MeshChunkID chunkID = ReadChunk( reader );

			if ( chunkID != MeshChunkID.GeometryVertexBufferData )
			{
				throw new AxiomException( "Can't find vertex buffer data area!" );
			}

			// check that vertex size agrees
			if ( data.vertexDeclaration.GetVertexSize( bindIdx ) != vertexSize )
			{
				throw new AxiomException( "Vertex buffer size does not agree with vertex declaration!" );
			}

			// create/populate vertex buffer
			HardwareVertexBuffer buffer =
				HardwareBufferManager.Instance.CreateVertexBuffer(
					vertexSize,
					data.vertexCount,
					mesh.VertexBufferUsage,
					mesh.UseVertexShadowBuffer );

			IntPtr bufferPtr = buffer.Lock( BufferLocking.Discard );

			ReadBytes( reader, data.vertexCount * vertexSize, bufferPtr );

			buffer.Unlock();

			// set binding
			data.vertexBufferBinding.SetBinding( bindIdx, buffer );
		}

		protected virtual void ReadSkeletonLink( BinaryReader reader )
		{
			mesh.SkeletonName = ReadString( reader );

			MeshManager.Instance.FireProcessSkeletonName( this.mesh, this.mesh.SkeletonName );
		}

		protected virtual void ReadMeshBoneAssignment( BinaryReader reader )
		{
			VertexBoneAssignment assignment = new VertexBoneAssignment();

			// read the data from the file
			assignment.vertexIndex = ReadInt( reader );
			assignment.boneIndex = ReadUShort( reader );
			assignment.weight = ReadFloat( reader );

			// add the assignment to the mesh
			mesh.AddBoneAssignment( assignment );
		}

		protected virtual void ReadSubMeshBoneAssignment( BinaryReader reader, SubMesh sub )
		{
			VertexBoneAssignment assignment = new VertexBoneAssignment();

			// read the data from the file
			assignment.vertexIndex = ReadInt( reader );
			assignment.boneIndex = ReadUShort( reader );
			assignment.weight = ReadFloat( reader );

			// add the assignment to the mesh
			sub.AddBoneAssignment( assignment );
		}

		protected virtual void ReadMeshLodInfo( BinaryReader reader )
		{
			// Read the strategy to be used for this mesh
			string strategyName = this.ReadString( reader );
			LodStrategy strategy = LodStrategyManager.Instance.GetStrategy( strategyName );
			this.mesh.LodStrategy = strategy;

			// number of lod levels
			short lodLevelCount = ReadShort( reader );
			// bool manual;  (true for manual alternate meshes, false for generated)
			this.mesh.IsLodManual = this.ReadBool( reader ); //readBools(stream, &(pMesh->mIsLodManual), 1);

			// Preallocate submesh lod face data if not manual
			if ( !this.mesh.IsLodManual )
			{
				for ( ushort i = 0; i < this.mesh.SubMeshCount; ++i )
				{
					SubMesh sm = this.mesh.GetSubMesh( i );

					// TODO: Create typed collection and implement resize
					for ( ushort j = 1; j < lodLevelCount; j++ )
						sm.lodFaceList.Add( null );

					//sm.lodFaceList.resize(pMesh->mNumLods-1);
					}
				}

			MeshChunkID chunkId;
			// Loop from 1 rather than 0 (full detail index is not in file)
			for ( ushort i = 1; i < lodLevelCount; ++i )
			{
				chunkId = this.ReadChunk( reader );

				if ( chunkId != MeshChunkID.MeshLODUsage )
					throw new AxiomException( "Missing MeshLODUsage stream in '{0}'.", this.mesh.Name );

				// Read depth
				MeshLodUsage usage = new MeshLodUsage();
				usage.UserValue = this.ReadFloat( reader );

				if ( this.mesh.IsLodManual )
				{
					this.ReadMeshLodUsageManual( reader, i, ref usage );
				}
				else //(!pMesh->isLodManual)
				{
					this.ReadMeshLodUsageGenerated( reader, i, ref usage );
				}
				usage.EdgeData = null;

				// Save usage
				this.mesh.MeshLodUsageList.Add( usage );
			}
		}

		protected virtual void ReadMeshLodUsageManual( BinaryReader reader, int lodNum, ref MeshLodUsage usage )
		{
			MeshChunkID chunkId = ReadChunk( reader );

			if ( chunkId != MeshChunkID.MeshLODManual )
			{
				throw new AxiomException( "Missing MeshLODManual chunk in '{0}'.", mesh.Name );
			}

			usage.ManualName = ReadString( reader );

			// clearing the reference just in case
			usage.ManualMesh = null;
		}

		protected virtual void ReadMeshLodUsageGenerated( BinaryReader reader, int lodNum, ref MeshLodUsage usage )
		{
			usage.ManualName = "";
			usage.ManualMesh = null;

			// get one set of detail per submesh
			MeshChunkID chunkId;

			for ( int i = 0; i < mesh.SubMeshCount; i++ )
			{
				chunkId = ReadChunk( reader );

				if ( chunkId != MeshChunkID.MeshLODGenerated )
				{
					throw new AxiomException( "Missing MeshLodGenerated chunk in '{0}'", mesh.Name );
				}

				// get the current submesh
				SubMesh sm = mesh.GetSubMesh( i );

				// drop another index data object into the list
				IndexData indexData = new IndexData();
				sm.lodFaceList[ lodNum - 1 ] = indexData;

				// number of indices
				indexData.indexCount = ReadInt( reader );

				bool is32bit = ReadBool( reader );

				// create an appropriate index buffer and stuff in the data
				if ( is32bit )
				{
					indexData.indexBuffer =
						HardwareBufferManager.Instance.CreateIndexBuffer(
						IndexType.Size32,
						indexData.indexCount,
						mesh.IndexBufferUsage,
						mesh.UseIndexShadowBuffer );

					// lock the buffer
					IntPtr data = indexData.indexBuffer.Lock( BufferLocking.Discard );

					// stuff the data into the index buffer
					ReadInts( reader, indexData.indexCount, data );

					// unlock the index buffer
					indexData.indexBuffer.Unlock();
				}
				else
				{
					indexData.indexBuffer =
						HardwareBufferManager.Instance.CreateIndexBuffer(
						IndexType.Size16,
						indexData.indexCount,
						mesh.IndexBufferUsage,
						mesh.UseIndexShadowBuffer );

					// lock the buffer
					IntPtr data = indexData.indexBuffer.Lock( BufferLocking.Discard );

					// stuff the data into the index buffer
					ReadShorts( reader, indexData.indexCount, data );

					// unlock the index buffer
					indexData.indexBuffer.Unlock();
				}
			}
		}

		protected virtual void ReadBoundsInfo( BinaryReader reader )
		{
			// min abb extent
			Vector3 min = ReadVector3( reader );

			// max abb extent
			Vector3 max = ReadVector3( reader );

			// set the mesh's aabb
			mesh.BoundingBox = new AxisAlignedBox( min, max );

			// set the bounding sphere radius
			mesh.BoundingSphereRadius = ReadFloat( reader );
		}

		protected virtual void ReadEdgeList( BinaryReader reader )
		{
			if ( !IsEOF( reader ) )
			{
				MeshChunkID chunkID = ReadChunk( reader );

				while ( !IsEOF( reader ) &&
					chunkID == MeshChunkID.EdgeListLOD )
				{

					// process single LOD
					short lodIndex = ReadShort( reader );

					// If manual, no edge data here, loaded from manual mesh
					bool isManual = ReadBool( reader );

					// Only load in non-manual levels; others will be connected up by Mesh on demand
					if ( !isManual )
					{
						MeshLodUsage usage = mesh.GetLodLevel( lodIndex );

						usage.EdgeData = new EdgeData();

						// ToDo Assign to usage.EdgeData.IsClosed
						bool isClosed = ReadBool( reader );

						int triCount = ReadInt( reader );
						int edgeGroupCount = ReadInt( reader );

						// TODO: Resize triangle list
						// TODO: Resize edge groups

						for ( int i = 0; i < triCount; i++ )
						{
							EdgeData.Triangle tri = new EdgeData.Triangle();

							tri.indexSet = ReadInt( reader );
							tri.vertexSet = ReadInt( reader );

							tri.vertIndex[ 0 ] = ReadInt( reader );
							tri.vertIndex[ 1 ] = ReadInt( reader );
							tri.vertIndex[ 2 ] = ReadInt( reader );

							tri.sharedVertIndex[ 0 ] = ReadInt( reader );
							tri.sharedVertIndex[ 1 ] = ReadInt( reader );
							tri.sharedVertIndex[ 2 ] = ReadInt( reader );

							tri.normal = ReadVector4( reader );

							usage.EdgeData.triangles.Add( tri );
						}

						for ( int eg = 0; eg < edgeGroupCount; eg++ )
						{
							chunkID = ReadChunk( reader );

							if ( chunkID != MeshChunkID.EdgeListGroup )
							{
								throw new AxiomException( "Missing EdgeListGroup chunk." );
							}

							EdgeData.EdgeGroup edgeGroup = new EdgeData.EdgeGroup();

							edgeGroup.vertexSet = ReadInt( reader );

							int egtriStart = ReadInt( reader );
							int egTriCount = ReadInt( reader );

							int edgeCount = ReadInt( reader );

							// TODO: Resize the edge group list

							for ( int e = 0; e < edgeCount; e++ )
							{
								EdgeData.Edge edge = new EdgeData.Edge();

								edge.triIndex[ 0 ] = ReadInt( reader );
								edge.triIndex[ 1 ] = ReadInt( reader );

								edge.vertIndex[ 0 ] = ReadInt( reader );
								edge.vertIndex[ 1 ] = ReadInt( reader );

								edge.sharedVertIndex[ 0 ] = ReadInt( reader );
								edge.sharedVertIndex[ 1 ] = ReadInt( reader );

								edge.isDegenerate = ReadBool( reader );

								// add the edge to the list
								edgeGroup.edges.Add( edge );
							}

							// Populate edgeGroup.vertexData references
							// If there is shared vertex data, vertexSet 0 is that, 
							// otherwise 0 is first dedicated
							if ( mesh.SharedVertexData != null )
							{
								if ( edgeGroup.vertexSet == 0 )
								{
									edgeGroup.vertexData = mesh.SharedVertexData;
								}
								else
								{
									edgeGroup.vertexData = mesh.GetSubMesh( edgeGroup.vertexSet - 1 ).vertexData;
								}
							}
							else
							{
								edgeGroup.vertexData = mesh.GetSubMesh( edgeGroup.vertexSet ).vertexData;
							}

							// add the edge group to the list
							usage.EdgeData.edgeGroups.Add( edgeGroup );
						}
					}

					// grab the next chunk
					if ( !IsEOF( reader ) )
					{
						chunkID = ReadChunk( reader );
					}
				}

				// grab the next chunk
				if ( !IsEOF( reader ) )
				{
					// backpedal to the start of chunk
					Seek( reader, -ChunkOverheadSize );
				}
			}

			mesh.IsEdgeListBuilt = true;
		}

		protected virtual void ReadPoses( BinaryReader reader )
		{
			if ( !IsEOF( reader ) )
			{
				MeshChunkID chunkID = ReadChunk( reader );

				while ( !IsEOF( reader ) &&
					chunkID == MeshChunkID.Pose )
				{

					string name = ReadString( reader );
					ushort target = ReadUShort( reader );
					Pose pose = mesh.CreatePose( target, name );

					while ( !IsEOF( reader ) &&
						   ( chunkID = ReadChunk( reader ) ) == MeshChunkID.PoseVertex )
					{

						int vertexIndex = ReadInt( reader );
						Vector3 offset = ReadVector3( reader );
						pose.VertexOffsetMap[ vertexIndex ] = offset;
					}
				}

				// grab the next chunk
				if ( !IsEOF( reader ) )
				{
					// backpedal to the start of chunk
					Seek( reader, -ChunkOverheadSize );
				}
			}
		}

		protected virtual void ReadAnimations( BinaryReader reader )
		{
			if ( !IsEOF( reader ) )
			{
				MeshChunkID chunkID = ReadChunk( reader );

				while ( !IsEOF( reader ) &&
					chunkID == MeshChunkID.Animation )
				{

					switch ( chunkID )
					{
						case MeshChunkID.Animation:
							ReadAnimation( reader );
							break;
					}
					if ( !IsEOF( reader ) )
						chunkID = ReadChunk( reader );
				}
				if ( !IsEOF( reader ) )
				{
					// backpedal to the start of chunk
					Seek( reader, -ChunkOverheadSize );
				}
			}
		}

		protected void ReadAnimation( BinaryReader reader )
		{
			string name = ReadString( reader );
			float length = ReadFloat( reader );
			Animation anim = mesh.CreateAnimation( name, length );

			// Read the tracks for this animation
			if ( !IsEOF( reader ) )
			{
				MeshChunkID chunkID = ReadChunk( reader );
				while ( !IsEOF( reader ) &&
					   chunkID == MeshChunkID.AnimationTrack )
				{

					switch ( chunkID )
					{
						case MeshChunkID.AnimationTrack:
							ReadAnimationTrack( reader, anim );
							break;
					}
					if ( !IsEOF( reader ) )
						chunkID = ReadChunk( reader );
				}
				if ( !IsEOF( reader ) )
				{
					// backpedal to the start of chunk
					Seek( reader, -ChunkOverheadSize );
				}
			}
		}

		protected void ReadAnimationTrack( BinaryReader reader, Animation anim )
		{
			ushort type = ReadUShort( reader );
			ushort target = ReadUShort( reader );

			VertexAnimationTrack track = anim.CreateVertexTrack( target,
																mesh.GetVertexDataByTrackHandle( target ),
																(VertexAnimationType)type );
			// Now read the key frames for this track
			if ( !IsEOF( reader ) )
			{
				MeshChunkID chunkID = ReadChunk( reader );
				while ( !IsEOF( reader ) &&
					   ( chunkID == MeshChunkID.AnimationMorphKeyframe ||
						chunkID == MeshChunkID.AnimationPoseKeyframe ) )
				{
					switch ( chunkID )
					{
						case MeshChunkID.AnimationMorphKeyframe:
							ReadMorphKeyframe( reader, track );
							break;
						case MeshChunkID.AnimationPoseKeyframe:
							ReadPoseKeyframe( reader, track );
							break;
					}
					if ( !IsEOF( reader ) )
						chunkID = ReadChunk( reader );
				}
				if ( !IsEOF( reader ) )
				{
					// backpedal to the start of chunk
					Seek( reader, -ChunkOverheadSize );
				}
			}
		}

		protected void ReadMorphKeyframe( BinaryReader reader, VertexAnimationTrack track )
		{
			float time = ReadFloat( reader );
			VertexMorphKeyFrame mkf = track.CreateVertexMorphKeyFrame( time );
			int vertexCount = track.TargetVertexData.vertexCount;
			// create/populate vertex buffer
			HardwareVertexBuffer buffer =
				HardwareBufferManager.Instance.CreateVertexBuffer(
						VertexElement.GetTypeSize( VertexElementType.Float3 ),
						vertexCount, BufferUsage.Static, true );
			// lock the buffer for editing
			IntPtr vertices = buffer.Lock( BufferLocking.Discard );
			// stuff the floats into the normal buffer
			ReadFloats( reader, vertexCount * 3, vertices );
			// unlock the buffer to commit
			buffer.Unlock();
			mkf.VertexBuffer = buffer;
		}

		protected void ReadPoseKeyframe( BinaryReader reader, VertexAnimationTrack track )
		{
			float time = ReadFloat( reader );
			VertexPoseKeyFrame vkf = track.CreateVertexPoseKeyFrame( time );

			if ( !IsEOF( reader ) )
			{
				MeshChunkID chunkID = ReadChunk( reader );
				while ( !IsEOF( reader ) &&
					   chunkID == MeshChunkID.AnimationPoseRef )
				{
					switch ( chunkID )
					{
						case MeshChunkID.AnimationPoseRef:
							{
								ushort poseIndex = ReadUShort( reader );
								float influence = ReadFloat( reader );
								vkf.AddPoseReference( poseIndex, influence );
								break;
							}
					}
					if ( !IsEOF( reader ) )
						chunkID = ReadChunk( reader );
				}
				if ( !IsEOF( reader ) )
				{
					// backpedal to the start of chunk
					Seek( reader, -ChunkOverheadSize );
				}
			}
		}

		/// <summary>
		///    Reads attachment point information from the file.
		/// </summary>
		protected void ReadAttachmentPoint( BinaryReader reader )
		{
			// attachment point name
			string name = ReadString( reader );

			// read and set the position of the bone
			Vector3 position = ReadVector3( reader );

			// read and set the orientation of the bone
			Quaternion q = ReadQuat( reader );

			// create the attachment point
			AttachmentPoint ap = mesh.CreateAttachmentPoint( name, q, position );
		}

		#endregion Protected

		#endregion Methods
	}

	/// <summary>
	/// Summary description for MeshSerializerImpl.
	/// </summary>
	public class MeshSerializerImplv14 : MeshSerializerImpl
	{
		#region Constructor

		/// <summary>
		///	Default constructor.
		/// </summary>
		public MeshSerializerImplv14()
		{
			version = "[MeshSerializer_v1.40]";
		}

		#endregion Constructor

		#region Methods

		protected override void ReadMeshLodInfo( BinaryReader reader )
		{
			MeshChunkID chunkId;

			// number of lod levels
			short lodLevelCount = ReadShort( reader );

			// load manual?
			mesh.IsLodManual = ReadBool( reader );

			// preallocate submesh lod face data if not manual
			if ( !mesh.IsLodManual )
			{
				for ( int i = 0; i < mesh.SubMeshCount; i++ )
				{
					SubMesh sub = mesh.GetSubMesh( i );

					// TODO: Create typed collection and implement resize
					for ( int j = 1; j < lodLevelCount; j++ )
					{
						sub.lodFaceList.Add( null );
					}
					//sub.lodFaceList.Resize(mesh.lodCount - 1);
				}
			}

			// Loop from 1 rather than 0 (full detail index is not in file)
			for ( int i = 1; i < lodLevelCount; i++ )
			{
				chunkId = ReadChunk( reader );

				if ( chunkId != MeshChunkID.MeshLODUsage )
				{
					throw new AxiomException( "Missing MeshLodUsage chunk in mesh '{0}'", mesh.Name );
				}

				// camera depth
				MeshLodUsage usage = new MeshLodUsage();
				usage.Value = ReadFloat( reader );
				usage.UserValue = Utility.Sqrt( usage.Value );

				if ( mesh.IsLodManual )
				{
					ReadMeshLodUsageManual( reader, i, ref usage );
				}
				else
				{
					ReadMeshLodUsageGenerated( reader, i, ref usage );
				}

				// push lod usage onto the mesh lod list
				mesh.MeshLodUsageList.Add( usage );
			}
			Debug.Assert( mesh.LodLevelCount == lodLevelCount );
		}

		protected override void WriteMeshLodSummary( BinaryWriter writer )
		{
			WriteChunk( writer, MeshChunkID.MeshLOD, 0 );
			WriteShort( writer, (short)mesh.LodLevelCount );
			WriteBool( writer, mesh.IsLodManual );
		}

		#endregion Methods
	}

	/// <summary>
	/// Summary description for MeshSerializerImpl.
	/// </summary>
	public class MeshSerializerImplv13 : MeshSerializerImplv14
	{
		#region Constructor

		/// <summary>
		///		Default constructor.
		/// </summary>
		public MeshSerializerImplv13()
		{
			version = "[MeshSerializer_v1.30]";
		}

		#endregion Constructor

		#region Methods

		protected override void ReadEdgeList( BinaryReader reader )
		{
			if ( !IsEOF( reader ) )
			{
				MeshChunkID chunkID = ReadChunk( reader );

				while ( !IsEOF( reader ) &&
					chunkID == MeshChunkID.EdgeListLOD )
				{

					// process single LOD
					short lodIndex = ReadShort( reader );

					// If manual, no edge data here, loaded from manual mesh
					bool isManual = ReadBool( reader );

					// Only load in non-manual levels; others will be connected up by Mesh on demand
					if ( !isManual )
					{
						MeshLodUsage usage = mesh.GetLodLevel( lodIndex );

						usage.EdgeData = new EdgeData();

						int triCount = ReadInt( reader );
						int edgeGroupCount = ReadInt( reader );

						// TODO: Resize triangle list
						// TODO: Resize edge groups

						for ( int i = 0; i < triCount; i++ )
						{
							EdgeData.Triangle tri = new EdgeData.Triangle();

							tri.indexSet = ReadInt( reader );
							tri.vertexSet = ReadInt( reader );

							tri.vertIndex[ 0 ] = ReadInt( reader );
							tri.vertIndex[ 1 ] = ReadInt( reader );
							tri.vertIndex[ 2 ] = ReadInt( reader );

							tri.sharedVertIndex[ 0 ] = ReadInt( reader );
							tri.sharedVertIndex[ 1 ] = ReadInt( reader );
							tri.sharedVertIndex[ 2 ] = ReadInt( reader );

							tri.normal = ReadVector4( reader );

							usage.EdgeData.triangles.Add( tri );
						}

						for ( int eg = 0; eg < edgeGroupCount; eg++ )
						{
							chunkID = ReadChunk( reader );

							if ( chunkID != MeshChunkID.EdgeListGroup )
							{
								throw new AxiomException( "Missing EdgeListGroup chunk." );
							}

							EdgeData.EdgeGroup edgeGroup = new EdgeData.EdgeGroup();

							edgeGroup.vertexSet = ReadInt( reader );

							int edgeCount = ReadInt( reader );

							// TODO: Resize the edge group list

							for ( int e = 0; e < edgeCount; e++ )
							{
								EdgeData.Edge edge = new EdgeData.Edge();

								edge.triIndex[ 0 ] = ReadInt( reader );
								edge.triIndex[ 1 ] = ReadInt( reader );

								edge.vertIndex[ 0 ] = ReadInt( reader );
								edge.vertIndex[ 1 ] = ReadInt( reader );

								edge.sharedVertIndex[ 0 ] = ReadInt( reader );
								edge.sharedVertIndex[ 1 ] = ReadInt( reader );

								edge.isDegenerate = ReadBool( reader );

								// add the edge to the list
								edgeGroup.edges.Add( edge );
							}

							// Populate edgeGroup.vertexData references
							// If there is shared vertex data, vertexSet 0 is that, 
							// otherwise 0 is first dedicated
							if ( mesh.SharedVertexData != null )
							{
								if ( edgeGroup.vertexSet == 0 )
								{
									edgeGroup.vertexData = mesh.SharedVertexData;
								}
								else
								{
									edgeGroup.vertexData = mesh.GetSubMesh( edgeGroup.vertexSet - 1 ).vertexData;
								}
							}
							else
							{
								edgeGroup.vertexData = mesh.GetSubMesh( edgeGroup.vertexSet ).vertexData;
							}

							// add the edge group to the list
							usage.EdgeData.edgeGroups.Add( edgeGroup );
						}
					}

					// grab the next chunk
					if ( !IsEOF( reader ) )
					{
						chunkID = ReadChunk( reader );
					}
				}

				// grab the next chunk
				if ( !IsEOF( reader ) )
				{
					// backpedal to the start of chunk
					Seek( reader, -ChunkOverheadSize );
				}
			}

			mesh.IsEdgeListBuilt = true;
		}

		#endregion Methods
	}

	/// <summary>
	///     Mesh serializer for supporint OGRE 1.20 meshes.
	/// </summary>
	public class MeshSerializerImplv12 : MeshSerializerImplv13
	{
		#region Constructor

		public MeshSerializerImplv12()
		{
			version = "[MeshSerializer_v1.20]";
		}

		#endregion Constructor

		#region Methods

		protected override void ReadMesh( BinaryReader reader )
		{
			base.ReadMesh( reader );

			// always automatically build edge lists for this version
			mesh.AutoBuildEdgeLists = true;
		}

		protected override void ReadGeometry( BinaryReader reader, VertexData data )
		{
			short texCoordSet = 0;

			short bindIdx = 0;

			data.vertexStart = 0;
			data.vertexCount = ReadInt( reader );

			ReadGeometryPositions( bindIdx++, reader, data );

			if ( !IsEOF( reader ) )
			{
				// check out the next chunk
				MeshChunkID chunkID = ReadChunk( reader );

				// keep going as long as we have more optional buffer chunks
				while ( !IsEOF( reader ) &&
					( chunkID == MeshChunkID.GeometryNormals ||
					chunkID == MeshChunkID.GeometryColors ||
					chunkID == MeshChunkID.GeometryTexCoords ) )
				{

					switch ( chunkID )
					{
						case MeshChunkID.GeometryNormals:
							ReadGeometryNormals( bindIdx++, reader, data );
							break;

						case MeshChunkID.GeometryColors:
							ReadGeometryColors( bindIdx++, reader, data );
							break;

						case MeshChunkID.GeometryTexCoords:
							ReadGeometryTexCoords( bindIdx++, reader, data, texCoordSet++ );
							break;

					} // switch

					// read the next chunk
					if ( !IsEOF( reader ) )
					{
						chunkID = ReadChunk( reader );
					}
				} // while

				if ( !IsEOF( reader ) )
				{
					// skip back so the continuation of the calling loop can look at the next chunk
					// since we already read past it
					Seek( reader, -ChunkOverheadSize );
				}
			}
		}

		protected virtual void ReadGeometryPositions( short bindIdx, BinaryReader reader, VertexData data )
		{
			data.vertexDeclaration.AddElement( bindIdx, 0, VertexElementType.Float3, VertexElementSemantic.Position );

			// vertex buffers
			HardwareVertexBuffer vBuffer = HardwareBufferManager.Instance.
				CreateVertexBuffer( data.vertexDeclaration.GetVertexSize( bindIdx ),
				data.vertexCount, mesh.VertexBufferUsage, mesh.UseVertexShadowBuffer );

			IntPtr posData = vBuffer.Lock( BufferLocking.Discard );

			// ram the floats into the buffer data
			ReadFloats( reader, data.vertexCount * 3, posData );

			// unlock the buffer
			vBuffer.Unlock();

			// bind the position data
			data.vertexBufferBinding.SetBinding( bindIdx, vBuffer );
		}

		protected virtual void ReadGeometryNormals( short bindIdx, BinaryReader reader, VertexData data )
		{
			// add an element for normals
			data.vertexDeclaration.AddElement( bindIdx, 0, VertexElementType.Float3, VertexElementSemantic.Normal );

			HardwareVertexBuffer vBuffer = HardwareBufferManager.Instance.CreateVertexBuffer(
				data.vertexDeclaration.GetVertexSize( bindIdx ),
				data.vertexCount,
				mesh.VertexBufferUsage,
				mesh.UseVertexShadowBuffer );

			// lock the buffer for editing
			IntPtr normals = vBuffer.Lock( BufferLocking.Discard );

			// stuff the floats into the normal buffer
			ReadFloats( reader, data.vertexCount * 3, normals );

			// unlock the buffer to commit
			vBuffer.Unlock();

			// bind this buffer
			data.vertexBufferBinding.SetBinding( bindIdx, vBuffer );
		}
		protected virtual void ReadGeometryTangents( short bindIdx, BinaryReader reader, VertexData data )
		{
			// add an element for normals
			data.vertexDeclaration.AddElement( bindIdx, 0, VertexElementType.Float3, VertexElementSemantic.Tangent );

			HardwareVertexBuffer vBuffer = HardwareBufferManager.Instance.CreateVertexBuffer(
				data.vertexDeclaration.GetVertexSize( bindIdx ),
				data.vertexCount,
				mesh.VertexBufferUsage,
				mesh.UseVertexShadowBuffer );

			// lock the buffer for editing
			IntPtr buf = vBuffer.Lock( BufferLocking.Discard );

			// stuff the floats into the buffer
			ReadFloats( reader, data.vertexCount * 3, buf );

			// unlock the buffer to commit
			vBuffer.Unlock();

			// bind this buffer
			data.vertexBufferBinding.SetBinding( bindIdx, vBuffer );
		}
		protected virtual void ReadGeometryBinormals( short bindIdx, BinaryReader reader, VertexData data )
		{
			// add an element for normals
			data.vertexDeclaration.AddElement( bindIdx, 0, VertexElementType.Float3, VertexElementSemantic.Binormal );

			HardwareVertexBuffer vBuffer = HardwareBufferManager.Instance.CreateVertexBuffer(
				data.vertexDeclaration.GetVertexSize( bindIdx ),
				data.vertexCount,
				mesh.VertexBufferUsage,
				mesh.UseVertexShadowBuffer );

			// lock the buffer for editing
			IntPtr buf = vBuffer.Lock( BufferLocking.Discard );

			// stuff the floats into the buffer
			ReadFloats( reader, data.vertexCount * 3, buf );

			// unlock the buffer to commit
			vBuffer.Unlock();

			// bind this buffer
			data.vertexBufferBinding.SetBinding( bindIdx, vBuffer );
		}

		protected virtual void ReadGeometryColors( short bindIdx, BinaryReader reader, VertexData data )
		{
			// add an element for normals
			data.vertexDeclaration.AddElement( bindIdx, 0, VertexElementType.Color, VertexElementSemantic.Diffuse );

			HardwareVertexBuffer vBuffer = HardwareBufferManager.Instance.CreateVertexBuffer(
				data.vertexDeclaration.GetVertexSize( bindIdx ),
				data.vertexCount,
				mesh.VertexBufferUsage,
				mesh.UseVertexShadowBuffer );

			// lock the buffer for editing
			IntPtr colors = vBuffer.Lock( BufferLocking.Discard );

			// stuff the floats into the normal buffer
			ReadInts( reader, data.vertexCount, colors );

			// unlock the buffer to commit
			vBuffer.Unlock();

			// bind this buffer
			data.vertexBufferBinding.SetBinding( bindIdx, vBuffer );
		}

		protected virtual void ReadGeometryTexCoords( short bindIdx, BinaryReader reader, VertexData data, int coordSet )
		{
			// get the number of texture dimensions (1D, 2D, 3D, etc)
			short dim = ReadShort( reader );

			// add a vertex element for the current tex coord set
			data.vertexDeclaration.AddElement(
				bindIdx, 0,
				VertexElement.MultiplyTypeCount( VertexElementType.Float1, dim ),
				VertexElementSemantic.TexCoords,
				coordSet );

			// create the vertex buffer for the tex coords
			HardwareVertexBuffer vBuffer = HardwareBufferManager.Instance.CreateVertexBuffer(
				data.vertexDeclaration.GetVertexSize( bindIdx ),
				data.vertexCount,
				mesh.VertexBufferUsage,
				mesh.UseVertexShadowBuffer );

			// lock the vertex buffer
			IntPtr texCoords = vBuffer.Lock( BufferLocking.Discard );

			// blast the tex coord data into the buffer
			ReadFloats( reader, data.vertexCount * dim, texCoords );

			// unlock the buffer to commit
			vBuffer.Unlock();

			// bind the tex coord buffer
			data.vertexBufferBinding.SetBinding( bindIdx, vBuffer );
		}

		#endregion Methods
	}

	/// <summary>
	///     Mesh serializer for supporint OGRE 1.10 meshes.
	/// </summary>
	public class MeshSerializerImplv11 : MeshSerializerImplv12
	{
		#region Constructor

		public MeshSerializerImplv11()
		{
			version = "[MeshSerializer_v1.10]";
		}

		#endregion Constructor

		#region Methods

		protected override void ReadGeometryTexCoords( short bindIdx, BinaryReader reader, VertexData data, int coordSet )
		{
			// get the number of texture dimensions (1D, 2D, 3D, etc)
			ushort dim = ReadUShort( reader );

			// add a vertex element for the current tex coord set
			data.vertexDeclaration.AddElement(
				bindIdx, 0,
				VertexElement.MultiplyTypeCount( VertexElementType.Float1, dim ),
				VertexElementSemantic.TexCoords,
				coordSet );

			// create the vertex buffer for the tex coords
			HardwareVertexBuffer vBuffer = HardwareBufferManager.Instance.CreateVertexBuffer(
				data.vertexDeclaration.GetVertexSize( bindIdx ),
				data.vertexCount,
				mesh.VertexBufferUsage,
				mesh.UseVertexShadowBuffer );

			// lock the vertex buffer
			IntPtr texCoords = vBuffer.Lock( BufferLocking.Discard );

			// blast the tex coord data into the buffer
			ReadFloats( reader, data.vertexCount * dim, texCoords );

			// Adjust individual v values to (1 - v)
			if ( dim == 2 )
			{
				int count = 0;

				unsafe
				{
					float* pTex = (float*)texCoords.ToPointer();

					for ( int i = 0; i < data.vertexCount; i++ )
					{
						count++; // skip u
						pTex[ count ] = 1.0f - pTex[ count ]; // v = 1 - v
						count++;
					}
				}
			}

			// unlock the buffer to commit
			vBuffer.Unlock();

			// bind the tex coord buffer
			data.vertexBufferBinding.SetBinding( bindIdx, vBuffer );
		}

		#endregion Methods
	}
}
