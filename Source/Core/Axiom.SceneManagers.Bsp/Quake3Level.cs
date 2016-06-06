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
using System.Collections;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Axiom.Core;
using Axiom.Math;
using Axiom.Media;
using Axiom.Graphics;

#endregion Namespace Declarations

namespace Axiom.SceneManagers.Bsp
{
	public enum Quake3LumpType
	{
		Entities = 0,
		Shaders,
		Planes,
		Nodes,
		Leaves,
		LeafFaces,
		LeafBrushes,
		Models,
		Brushes,
		BrushSides,
		Vertices,
		Elements,
		Fog,
		Faces,
		Lightmaps,
		LightVolumes,
		Visibility,
		NumLumps
	}

	/// <summary>
	///		Support for loading and extracting data from a Quake3 level file.
	///	</summary>
	///	<remarks>
	///		This class implements the required methods for opening Quake3 level files
	///		and extracting the pertinent data within. Ogre supports BSP based levels
	///		through it's own BspLevel class, which is not specific to any file format,
	///		so this class is here to source that data from the Quake3 format.
	///		</p>
	///		Quake3 levels include far more than just data for rendering - typically the
	///		<strong>leaves</strong> of the tree are used for rendering, and <strong>brushes,</strong>
	///		are used to define convex hulls made of planes for collision detection. There are also
	///		<strong>entities</strong> which define non-visual elements like player start
	///		points, triggers etc and <strong>models</strong> which are used for movable
	///		scenery like doors and platforms. <strong>Shaders</strong> meanwhile are textures
	///		with extra effects and 'content flags' indicating special properties like
	///		water or lava.
	///		<p/>
	///		I will try to support as much of this as I can in Ogre, but I won't duplicate
	///		the structure or necesarily use the same terminology. Quake3 is designed for a very specific
	///		purpose and code structure, whereas Ogre is designed to be more flexible,
	///		so for example I'm likely to separate game-related properties like surface flags
	///		from the generics of materials in my implementation.</p>
	///		This is a utility class only - a single call to loadFromChunk should be
	///		enough. You should not expect the state of this object to be consistent
	///		between calls, since it uses pointers to memory which may no longer
	///		be valid after the original call. This is why it has no accessor methods
	///		for reading it's internal state.
	///	</remarks>
	public class Quake3Level
	{
		#region Internal storage

		// This is ALL temporary. Don't rely on it being static
		// NB no brushes, fog or local lightvolumes yet
		private Stream chunk;
		private InternalBspHeader header;

		private int[] elements;
		private string entities;
		private int[] leafBrushes;
		private int[] leafFaces;

		private InternalBspModel[] models;
		private InternalBspNode[] nodes;
		private InternalBspLeaf[] leaves;
		private InternalBspPlane[] planes;
		private InternalBspFace[] faces;
		private InternalBspVertex[] vertices;
		private InternalBspShader[] shaders;
		private InternalBspVis visData;
		private InternalBspBrush[] brushes;
		private InternalBspBrushSide[] brushSides;

		protected BspOptions options;

		#endregion Internal storage

		#region Properties

		public int NumVertices
		{
			get
			{
				return this.vertices.Length;
			}
		}

		public int NumFaces
		{
			get
			{
				return this.faces.Length;
			}
		}

		public int NumLeafFaces
		{
			get
			{
				return this.leafFaces.Length;
			}
		}

		public int NumElements
		{
			get
			{
				return this.elements.Length;
			}
		}

		public int NumNodes
		{
			get
			{
				return this.nodes.Length;
			}
		}

		public int NumLeaves
		{
			get
			{
				return this.leaves.Length;
			}
		}

		public int NumBrushes
		{
			get
			{
				return this.brushes.Length;
			}
		}

		public BspOptions Options
		{
			get
			{
				return this.options;
			}
		}

		public int[] LeafFaces
		{
			get
			{
				return this.leafFaces;
			}
		}

		public int[] LeafBrushes
		{
			get
			{
				return this.leafBrushes;
			}
		}

		public int[] Elements
		{
			get
			{
				return this.elements;
			}
		}

		public string Entities
		{
			get
			{
				return this.entities;
			}
		}

		public InternalBspVertex[] Vertices
		{
			get
			{
				return this.vertices;
			}
		}

		public InternalBspFace[] Faces
		{
			get
			{
				return this.faces;
			}
		}

		public InternalBspShader[] Shaders
		{
			get
			{
				return this.shaders;
			}
		}

		public InternalBspNode[] Nodes
		{
			get
			{
				return this.nodes;
			}
		}

		public InternalBspPlane[] Planes
		{
			get
			{
				return this.planes;
			}
		}

		public InternalBspBrush[] Brushes
		{
			get
			{
				return this.brushes;
			}
		}

		public InternalBspBrushSide[] BrushSides
		{
			get
			{
				return this.brushSides;
			}
		}

		public InternalBspVis VisData
		{
			get
			{
				return this.visData;
			}
		}

		public InternalBspLeaf[] Leaves
		{
			get
			{
				return this.leaves;
			}
		}

		#endregion Properties

		#region Constructor

		public Quake3Level( BspOptions options )
		{
			this.options = options;
		}

		#endregion Constructor

		#region Methods

		/// <summary>
		///		Utility function read the header.
		/// </summary>
		public void Initialize()
		{
			Initialize( false );
		}

		public void Initialize( bool headerOnly )
		{
			var reader = new BinaryReader( this.chunk );

			this.header = new InternalBspHeader();
			this.header.magic = System.Text.Encoding.UTF8.GetChars( reader.ReadBytes( 4 ) );
			this.header.version = reader.ReadInt32();
			this.header.lumps = new InternalBspLump[(int)Quake3LumpType.NumLumps];

			for ( int i = 0; i < (int)Quake3LumpType.NumLumps; i++ )
			{
				this.header.lumps[ i ] = new InternalBspLump();
				this.header.lumps[ i ].offset = reader.ReadInt32();
				this.header.lumps[ i ].size = reader.ReadInt32();
			}

			InitializeCounts( reader );
			if ( headerOnly )
			{
			}
			else
			{
				InitializeData( reader );
			}
		}

		public void LoadHeaderFromStream( Stream inStream )
		{
			// Load just the header
			this.chunk = inStream;
			// Grab all the counts, header only
			Initialize( true );
			// Delete manually since delete and delete[] (as used by MemoryDataStream)
			// are not compatible
		}

		protected void InitializeCounts( BinaryReader reader )
		{
			this.brushes =
				new InternalBspBrush[
					this.header.lumps[ (int)Quake3LumpType.Brushes ].size/Memory.SizeOf( typeof ( InternalBspBrush ) )];
			this.leafBrushes = new int[this.header.lumps[ (int)Quake3LumpType.LeafBrushes ].size/Memory.SizeOf( typeof ( int ) )];
			this.vertices =
				new InternalBspVertex[
					this.header.lumps[ (int)Quake3LumpType.Vertices ].size/Memory.SizeOf( typeof ( InternalBspVertex ) )];
			this.planes =
				new InternalBspPlane[
					this.header.lumps[ (int)Quake3LumpType.Planes ].size/Memory.SizeOf( typeof ( InternalBspPlane ) )];
			this.nodes =
				new InternalBspNode[this.header.lumps[ (int)Quake3LumpType.Nodes ].size/Memory.SizeOf( typeof ( InternalBspNode ) )];
			this.models =
				new InternalBspModel[
					this.header.lumps[ (int)Quake3LumpType.Models ].size/Memory.SizeOf( typeof ( InternalBspModel ) )];
			this.leaves =
				new InternalBspLeaf[this.header.lumps[ (int)Quake3LumpType.Leaves ].size/Memory.SizeOf( typeof ( InternalBspLeaf ) )
					];
			this.leafFaces = new int[this.header.lumps[ (int)Quake3LumpType.LeafFaces ].size/Memory.SizeOf( typeof ( int ) )];
			this.faces =
				new InternalBspFace[this.header.lumps[ (int)Quake3LumpType.Faces ].size/Memory.SizeOf( typeof ( InternalBspFace ) )];
			this.elements = new int[this.header.lumps[ (int)Quake3LumpType.Elements ].size/Memory.SizeOf( typeof ( int ) )];
		}

		protected void InitializeData( BinaryReader reader )
		{
			ReadEntities( this.header.lumps[ (int)Quake3LumpType.Entities ], reader );
			ReadElements( this.header.lumps[ (int)Quake3LumpType.Elements ], reader );
			ReadFaces( this.header.lumps[ (int)Quake3LumpType.Faces ], reader );
			ReadLeafFaces( this.header.lumps[ (int)Quake3LumpType.LeafFaces ], reader );
			ReadLeaves( this.header.lumps[ (int)Quake3LumpType.Leaves ], reader );
			ReadModels( this.header.lumps[ (int)Quake3LumpType.Models ], reader );
			ReadNodes( this.header.lumps[ (int)Quake3LumpType.Nodes ], reader );
			ReadPlanes( this.header.lumps[ (int)Quake3LumpType.Planes ], reader );
			ReadShaders( this.header.lumps[ (int)Quake3LumpType.Shaders ], reader );
			ReadVisData( this.header.lumps[ (int)Quake3LumpType.Visibility ], reader );
			ReadVertices( this.header.lumps[ (int)Quake3LumpType.Vertices ], reader );
			ReadLeafBrushes( this.header.lumps[ (int)Quake3LumpType.LeafBrushes ], reader );
			ReadBrushes( this.header.lumps[ (int)Quake3LumpType.Brushes ], reader );
			ReadBrushSides( this.header.lumps[ (int)Quake3LumpType.BrushSides ], reader );
		}

		/// <summary>
		///		Reads Quake3 bsp data from a chunk of memory as read from the file.
		///	</summary>
		///	<remarks>
		///		Since ResourceManagers generally locate data in a variety of
		///		places they typically manipulate them as a chunk of data, rather than
		///		a file pointer since this is unsupported through compressed archives.
		///		<p/>
		///		Quake3 files are made up of a header (which contains version info and
		///		a table of the contents) and 17 'lumps' i.e. sections of data,
		///		the offsets to which are kept in the table of contents. The 17 types
		///		are predefined.
		/// </remarks>
		/// <param name="chunk">Input stream containing Quake3 data.</param>
		public void LoadFromStream( Stream inChunk )
		{
			this.chunk = inChunk;

			Initialize();
			DumpContents();
		}

		/// <summary>
		///		Extracts the embedded lightmap texture data and loads them as textures.
		/// </summary>
		/// <remarks>
		///		Calling this method makes the lightmap texture data embedded in
		///		the .bsp file available to the renderer. Lightmaps are extracted
		///		and loaded as Texture objects (subclass specific to RenderSystem
		///		subclass) and are named "@lightmap1", "@lightmap2" etc.
		/// </remarks>
		public void ExtractLightmaps()
		{
			this.chunk.Seek( this.header.lumps[ (int)Quake3LumpType.Lightmaps ].offset, SeekOrigin.Begin );
			int numLightmaps = this.header.lumps[ (int)Quake3LumpType.Lightmaps ].size/BspLevel.LightmapSize;

			// Lightmaps are always 128x128x24 (RGB).
			for ( int i = 0; i < numLightmaps; i++ )
			{
				string name = String.Format( "@lightmap{0}", i );
				var buffer = new byte[BspLevel.LightmapSize];
				this.chunk.Read( buffer, 0, BspLevel.LightmapSize );

				// Load, no mipmaps, brighten by factor 4
				// Set gamma explicitly, OpenGL doesn't apply it
				// CHECK: Make OpenGL apply gamma at LoadImage
				Image.ApplyGamma( buffer, 4, buffer.Length, 24 );
				var stream = new MemoryStream( buffer );
				Image img = Image.FromRawStream( stream, 128, 128, PixelFormat.R8G8B8 );
				TextureManager.Instance.LoadImage( name, ResourceGroupManager.Instance.WorldResourceGroupName, img, TextureType.TwoD );
			}
		}

		/// <summary>
		///		Debug method.
		/// </summary>
		public void DumpContents()
		{
			LogManager.Instance.Write( "Quake3 level statistics" );
			LogManager.Instance.Write( "-----------------------" );
			LogManager.Instance.Write( "Entities		: " + this.entities.Length.ToString() );
			LogManager.Instance.Write( "Faces			: " + this.faces.Length.ToString() );
			LogManager.Instance.Write( "Leaf Faces		: " + this.leafFaces.Length.ToString() );
			LogManager.Instance.Write( "Leaves			: " + this.leaves.Length.ToString() );
			LogManager.Instance.Write( "Lightmaps		: " +
			                           this.header.lumps[ (int)Quake3LumpType.Lightmaps ].size/BspLevel.LightmapSize );
			LogManager.Instance.Write( "Elements		: " + this.elements.Length.ToString() );
			LogManager.Instance.Write( "Models			: " + this.models.Length.ToString() );
			LogManager.Instance.Write( "Nodes			: " + this.nodes.Length.ToString() );
			LogManager.Instance.Write( "Planes			: " + this.planes.Length.ToString() );
			LogManager.Instance.Write( "Shaders		: " + this.shaders.Length.ToString() );
			LogManager.Instance.Write( "Vertices		: " + this.vertices.Length.ToString() );
			LogManager.Instance.Write( "Vis Clusters	: " + this.visData.clusterCount.ToString() );
			LogManager.Instance.Write( "" );
			LogManager.Instance.Write( "-= Shaders =-" );

			for ( int i = 0; i < this.shaders.Length; i++ )
			{
				LogManager.Instance.Write( String.Format( "Shader {0}: {1:x}", i, this.shaders[ i ].name ) );
			}

			LogManager.Instance.Write( "" );
			LogManager.Instance.Write( "-= Entities =-" );

			string[] ents = this.entities.Split( '\0' );

			for ( int i = 0; i < ents.Length; i++ )
			{
				LogManager.Instance.Write( ents[ i ] );
			}
		}

		private void ReadEntities( InternalBspLump lump, BinaryReader reader )
		{
			reader.BaseStream.Seek( lump.offset, SeekOrigin.Begin );
			this.entities = Encoding.UTF8.GetString( reader.ReadBytes( lump.size ), 0, lump.size );
		}

		private void ReadElements( InternalBspLump lump, BinaryReader reader )
		{
			reader.BaseStream.Seek( lump.offset, SeekOrigin.Begin );

			for ( int i = 0; i < this.elements.Length; i++ )
			{
				this.elements[ i ] = reader.ReadInt32();
			}
		}

		private void ReadFaces( InternalBspLump lump, BinaryReader reader )
		{
			reader.BaseStream.Seek( lump.offset, SeekOrigin.Begin );

			for ( int i = 0; i < this.faces.Length; i++ )
			{
				this.faces[ i ] = new InternalBspFace();
				this.faces[ i ].shader = reader.ReadInt32();
				this.faces[ i ].unknown = reader.ReadInt32();
				this.faces[ i ].type = (BspFaceType)Enum.Parse( typeof ( BspFaceType ), reader.ReadInt32().ToString(), false );
				this.faces[ i ].vertStart = reader.ReadInt32();
				this.faces[ i ].vertCount = reader.ReadInt32();
				this.faces[ i ].elemStart = reader.ReadInt32();
				this.faces[ i ].elemCount = reader.ReadInt32();
				this.faces[ i ].lmTexture = reader.ReadInt32();

				this.faces[ i ].lmOffset = new int[]
				                           {
				                           	reader.ReadInt32(), reader.ReadInt32()
				                           };
				this.faces[ i ].lmSize = new int[]
				                         {
				                         	reader.ReadInt32(), reader.ReadInt32()
				                         };
				this.faces[ i ].org = new float[]
				                      {
				                      	reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()
				                      };

				this.faces[ i ].bbox = new float[6];

				for ( int j = 0; j < this.faces[ i ].bbox.Length; j++ )
				{
					this.faces[ i ].bbox[ j ] = reader.ReadSingle();
				}

				this.faces[ i ].normal = new float[]
				                         {
				                         	reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()
				                         };
				this.faces[ i ].meshCtrl = new int[]
				                           {
				                           	reader.ReadInt32(), reader.ReadInt32()
				                           };

				TransformBoundingBox( this.faces[ i ].bbox );
				TransformVector( this.faces[ i ].org );
				TransformVector( this.faces[ i ].normal, true );
			}
		}

		private void ReadLeafFaces( InternalBspLump lump, BinaryReader reader )
		{
			reader.BaseStream.Seek( lump.offset, SeekOrigin.Begin );

			for ( int i = 0; i < this.leafFaces.Length; i++ )
			{
				this.leafFaces[ i ] = reader.ReadInt32();
			}
		}

		private void ReadLeaves( InternalBspLump lump, BinaryReader reader )
		{
			reader.BaseStream.Seek( lump.offset, SeekOrigin.Begin );

			for ( int i = 0; i < this.leaves.Length; i++ )
			{
				this.leaves[ i ] = new InternalBspLeaf();
				this.leaves[ i ].cluster = reader.ReadInt32();
				this.leaves[ i ].area = reader.ReadInt32();

				this.leaves[ i ].bbox = new int[6];

				for ( int j = 0; j < this.leaves[ i ].bbox.Length; j++ )
				{
					this.leaves[ i ].bbox[ j ] = reader.ReadInt32();
				}

				this.leaves[ i ].faceStart = reader.ReadInt32();
				this.leaves[ i ].faceCount = reader.ReadInt32();
				this.leaves[ i ].brushStart = reader.ReadInt32();
				this.leaves[ i ].brushCount = reader.ReadInt32();

				TransformBoundingBox( this.leaves[ i ].bbox );
			}
		}

		private void ReadModels( InternalBspLump lump, BinaryReader reader )
		{
			reader.BaseStream.Seek( lump.offset, SeekOrigin.Begin );

			for ( int i = 0; i < this.models.Length; i++ )
			{
				this.models[ i ] = new InternalBspModel();
				this.models[ i ].bbox = new float[6];

				for ( int j = 0; j < this.models[ i ].bbox.Length; j++ )
				{
					this.models[ i ].bbox[ j ] = reader.ReadSingle();
				}

				this.models[ i ].faceStart = reader.ReadInt32();
				this.models[ i ].faceCount = reader.ReadInt32();
				this.models[ i ].brushStart = reader.ReadInt32();
				this.models[ i ].brushCount = reader.ReadInt32();

				TransformBoundingBox( this.models[ i ].bbox );
			}
		}

		private void ReadNodes( InternalBspLump lump, BinaryReader reader )
		{
			reader.BaseStream.Seek( lump.offset, SeekOrigin.Begin );

			for ( int i = 0; i < this.nodes.Length; i++ )
			{
				this.nodes[ i ] = new InternalBspNode();
				this.nodes[ i ].plane = reader.ReadInt32();
				this.nodes[ i ].front = reader.ReadInt32();
				this.nodes[ i ].back = reader.ReadInt32();
				this.nodes[ i ].bbox = new int[6];

				for ( int j = 0; j < this.nodes[ i ].bbox.Length; j++ )
				{
					this.nodes[ i ].bbox[ j ] = reader.ReadInt32();
				}

				TransformBoundingBox( this.nodes[ i ].bbox );
			}
		}

		private void ReadPlanes( InternalBspLump lump, BinaryReader reader )
		{
			reader.BaseStream.Seek( lump.offset, SeekOrigin.Begin );

			for ( int i = 0; i < this.planes.Length; i++ )
			{
				this.planes[ i ] = new InternalBspPlane();
				this.planes[ i ].normal = new float[]
				                          {
				                          	reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()
				                          };
				this.planes[ i ].distance = reader.ReadSingle();

				TransformPlane( this.planes[ i ].normal, ref this.planes[ i ].distance );
			}
		}

		private void ReadShaders( InternalBspLump lump, BinaryReader reader )
		{
			reader.BaseStream.Seek( lump.offset, SeekOrigin.Begin );
			this.shaders = new InternalBspShader[lump.size/typeof ( InternalBspShader ).Size()];

			for ( int i = 0; i < this.shaders.Length; i++ )
			{
				char[] name = Encoding.UTF8.GetChars( reader.ReadBytes( 64 ) );

				this.shaders[ i ] = new InternalBspShader();
				this.shaders[ i ].surfaceFlags =
					(SurfaceFlags)Enum.Parse( typeof ( SurfaceFlags ), reader.ReadInt32().ToString(), false );
				this.shaders[ i ].contentFlags =
					(ContentFlags)Enum.Parse( typeof ( ContentFlags ), reader.ReadInt32().ToString(), false );

				foreach ( char c in name )
				{
					if ( c == '\0' )
					{
						break;
					}

					this.shaders[ i ].name += c;
				}
			}
		}

		private void ReadVisData( InternalBspLump lump, BinaryReader reader )
		{
			reader.BaseStream.Seek( lump.offset, SeekOrigin.Begin );

			this.visData = new InternalBspVis();
			this.visData.clusterCount = reader.ReadInt32();
			this.visData.rowSize = reader.ReadInt32();
			this.visData.data = reader.ReadBytes( lump.offset - ( typeof ( int ).Size()*2 ) );
		}

		private void ReadVertices( InternalBspLump lump, BinaryReader reader )
		{
			reader.BaseStream.Seek( lump.offset, SeekOrigin.Begin );

			for ( int i = 0; i < this.vertices.Length; i++ )
			{
				this.vertices[ i ] = new InternalBspVertex();
				this.vertices[ i ].point = new float[]
				                           {
				                           	reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()
				                           };
				this.vertices[ i ].texture = new float[]
				                             {
				                             	reader.ReadSingle(), reader.ReadSingle()
				                             };
				this.vertices[ i ].lightMap = new float[]
				                              {
				                              	reader.ReadSingle(), reader.ReadSingle()
				                              };
				this.vertices[ i ].normal = new float[]
				                            {
				                            	reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()
				                            };
				this.vertices[ i ].color = reader.ReadInt32();

				TransformVector( this.vertices[ i ].point );
				TransformVector( this.vertices[ i ].normal, true );
			}
		}

		private void ReadLeafBrushes( InternalBspLump lump, BinaryReader reader )
		{
			reader.BaseStream.Seek( lump.offset, SeekOrigin.Begin );

			for ( int i = 0; i < this.leafBrushes.Length; i++ )
			{
				this.leafBrushes[ i ] = reader.ReadInt32();
			}
		}

		private void ReadBrushes( InternalBspLump lump, BinaryReader reader )
		{
			reader.BaseStream.Seek( lump.offset, SeekOrigin.Begin );

			for ( int i = 0; i < this.brushes.Length; i++ )
			{
				this.brushes[ i ] = new InternalBspBrush();
				this.brushes[ i ].firstSide = reader.ReadInt32();
				this.brushes[ i ].numSides = reader.ReadInt32();
				this.brushes[ i ].shaderIndex = reader.ReadInt32();
			}
		}

		private void ReadBrushSides( InternalBspLump lump, BinaryReader reader )
		{
			reader.BaseStream.Seek( lump.offset, SeekOrigin.Begin );
			this.brushSides = new InternalBspBrushSide[lump.size/typeof ( InternalBspBrushSide ).Size()];

			for ( int i = 0; i < this.brushSides.Length; i++ )
			{
				this.brushSides[ i ] = new InternalBspBrushSide();
				this.brushSides[ i ].planeNum = reader.ReadInt32();
				this.brushSides[ i ].content = reader.ReadInt32();
			}
		}

		internal void TransformVector( float[] v, bool isNormal, int pos )
		{
			if ( this.options.setYAxisUp )
			{
				Swap( ref v[ pos + 1 ], ref v[ pos + 2 ] );
				v[ pos + 2 ] = -v[ pos + 2 ];
			}

			if ( !isNormal )
			{
				for ( int i = pos; i < pos + 3; i++ )
				{
					v[ i ] *= this.options.scale;
				}

				Vector3 move = this.options.move;
				v[ pos ] += this.options.move.x;
				v[ pos + 1 ] += this.options.move.y;
				v[ pos + 2 ] += this.options.move.z;
			}
		}

		internal void TransformVector( float[] v, bool isNormal )
		{
			TransformVector( v, isNormal, 0 );
		}

		internal void TransformVector( float[] v, int pos )
		{
			TransformVector( v, false, pos );
		}

		internal void TransformVector( float[] v )
		{
			TransformVector( v, false, 0 );
		}

		internal void TransformPlane( float[] norm, ref float dist )
		{
			TransformVector( norm, true );
			dist *= this.options.scale;
			var normal = new Vector3( norm[ 0 ], norm[ 1 ], norm[ 2 ] );
			Vector3 point = normal*dist;
			point += this.options.move;
			dist = normal.Dot( point );
		}

		internal void TransformBoundingBox( float[] bb )
		{
			TransformVector( bb, 0 );
			TransformVector( bb, 3 );
			if ( this.options.setYAxisUp )
			{
				Swap( ref bb[ 2 ], ref bb[ 5 ] );
			}
		}

		internal void TransformBoundingBox( int[] bb )
		{
			var floatbb = new float[6];
			for ( int i = 0; i < 6; i++ )
			{
				floatbb[ i ] = (float)bb[ i ];
			}

			TransformBoundingBox( floatbb );

			for ( int i = 0; i < 6; i++ )
			{
				bb[ i ] = Convert.ToInt32( floatbb[ i ] );
			}
		}

		private void Swap( ref float num1, ref float num2 )
		{
			float tmp = num1;
			num1 = num2;
			num2 = tmp;
		}

		private void Swap( ref int num1, ref int num2 )
		{
			int tmp = num1;
			num1 = num2;
			num2 = tmp;
		}

		#endregion Methods
	}

	[StructLayout( LayoutKind.Sequential )]
	public struct InternalBspPlane
	{
		[MarshalAs( UnmanagedType.ByValArray, SizeConst = 3 )] public float[] normal;

		public float distance;
	}

	[StructLayout( LayoutKind.Sequential )]
	public struct InternalBspModel
	{
		[MarshalAs( UnmanagedType.ByValArray, SizeConst = 6 )] public float[] bbox;

		public int faceStart;
		public int faceCount;
		public int brushStart;
		public int brushCount;
	}

	[StructLayout( LayoutKind.Sequential )]
	public struct InternalBspNode
	{
		public int plane; // dividing plane
		//int children[2];    // left and right nodes,
		// negative are leaves
		public int front;
		public int back;

		[MarshalAs( UnmanagedType.ByValArray, SizeConst = 6 )] public int[] bbox;
	}

	[StructLayout( LayoutKind.Sequential )]
	public struct InternalBspLeaf
	{
		public int cluster; // visibility cluster number
		public int area;

		[MarshalAs( UnmanagedType.ByValArray, SizeConst = 6 )] public int[] bbox;

		public int faceStart;
		public int faceCount;
		public int brushStart;
		public int brushCount;
	}

	[StructLayout( LayoutKind.Sequential )]
	public struct InternalBspFace
	{
		public int shader; // shader ref
		public int unknown;
		public BspFaceType type; // face type
		public int vertStart;
		public int vertCount;
		public int elemStart;
		public int elemCount;
		public int lmTexture; // lightmap

		[MarshalAs( UnmanagedType.ByValArray, SizeConst = 2 )] public int[] lmOffset;

		[MarshalAs( UnmanagedType.ByValArray, SizeConst = 2 )] public int[] lmSize;

		[MarshalAs( UnmanagedType.ByValArray, SizeConst = 3 )] public float[] org; // facetype_normal only

		[MarshalAs( UnmanagedType.ByValArray, SizeConst = 6 )] public float[] bbox; // facetype_patch only

		[MarshalAs( UnmanagedType.ByValArray, SizeConst = 3 )] public float[] normal; // facetype_normal only

		[MarshalAs( UnmanagedType.ByValArray, SizeConst = 2 )] public int[] meshCtrl; // patch control point dims
	}

	[StructLayout( LayoutKind.Explicit )]
	public struct InternalBspShader
	{
		[FieldOffset( 0 )] //[MarshalAs( UnmanagedType.LPStr )]
		[MarshalAs( UnmanagedType.ByValTStr, SizeConst = 64 )] public string name;

		[FieldOffset( 64 )] public SurfaceFlags surfaceFlags;

		[FieldOffset( 68 )] public ContentFlags contentFlags;
	}

	[StructLayout( LayoutKind.Sequential )]
	public struct InternalBspVertex
	{
		[MarshalAs( UnmanagedType.ByValArray, SizeConst = 3 )] public float[] point;

		[MarshalAs( UnmanagedType.ByValArray, SizeConst = 2 )] public float[] texture;

		[MarshalAs( UnmanagedType.ByValArray, SizeConst = 2 )] public float[] lightMap;

		[MarshalAs( UnmanagedType.ByValArray, SizeConst = 3 )] public float[] normal;

		public int color;
	}

	[StructLayout( LayoutKind.Sequential )]
	public struct InternalBspVis
	{
		public int clusterCount;
		public int rowSize;
		public byte[] data;
	}


	[StructLayout( LayoutKind.Sequential )]
	public struct InternalBspLump
	{
		public int offset;
		public int size;
	}

	[StructLayout( LayoutKind.Sequential )]
	public struct InternalBspHeader
	{
		[MarshalAs( UnmanagedType.ByValArray, SizeConst = 4 )] public char[] magic;

		public int version;

		[MarshalAs( UnmanagedType.ByValArray, SizeConst = 17 )] public InternalBspLump[] lumps;
	}


	[StructLayout( LayoutKind.Sequential )]
	public struct InternalBspBrushSide
	{
		public int planeNum;
		public int content; // ¿?shader¿?
	}

	[StructLayout( LayoutKind.Sequential )]
	public struct InternalBspBrush
	{
		public int firstSide;
		public int numSides;
		public int shaderIndex;
	}

	[Flags]
	public enum ContentFlags : uint
	{
		/// <summary>An eye is never valid in a solid.</summary>
		Solid = 1,
		Lava = 8,
		Slime = 16,
		Water = 32,
		Fog = 64,

		AreaPortal = 0x8000,
		PlayerClip = 0x10000,
		MonsterClip = 0x20000,

		/// <summary>Bot specific.</summary>
		Teleporter = 0x40000,

		/// <summary>Bot specific.</summary>
		JumpPad = 0x80000,

		/// <summary>Bot specific.</summary>
		ClusterPortal = 0x100000,

		/// <summary>Bot specific.</summary>
		DoNotEnter = 0x200000,

		/// <summary>Removed before bsping an entity.</summary>
		Origin = 0x1000000,

		/// <summary>Should never be on a brush, only in game.</summary>
		Body = 0x2000000,
		Corpse = 0x4000000,

		/// <summary>Brushes not used for the bsp.</summary>
		Detail = 0x8000000,

		/// <summary>Brushes used for the bsp.</summary>
		Structural = 0x10000000,

		/// <summary>Don't consume surface fragments inside.</summary>
		Translucent = 0x20000000,
		Trigger = 0x40000000,

		/// <summary>Don't leave bodies or items (death fog, lava).</summary>
		NoDrop = 0x80000000
	}

	[Flags]
	public enum SurfaceFlags
	{
		/// <summary>Never give falling damage.</summary>
		NoDamage = 0x1,

		/// <summary>Effects game physics.</summary>
		Slick = 0x2,

		/// <summary>Lighting from environment map.</summary>
		Sky = 0x4,
		Ladder = 0x8,

		/// <summary>Don't make missile explosions.</summary>
		NoImpact = 0x10,

		/// <summary>Don't leave missile marks.</summary>
		NoMarks = 0x20,

		/// <summary>Make flesh sounds and effects.</summary>
		Flesh = 0x40,

		/// <summary>Don't generate a drawsurface at all.</summary>
		NoDraw = 0x80,

		/// <summary>Make a primary bsp splitter.</summary>
		Hint = 0x100,

		/// <summary>Completely ignore, allowing non-closed brushes.</summary>
		Skip = 0x200,

		/// <summary>Surface doesn't need a lightmap.</summary>
		NoLightmap = 0x400,

		/// <summary>Generate lighting info at vertexes.</summary>
		PointLight = 0x800,

		/// <summary>Clanking footsteps.</summary>
		MetalSteps = 0x1000,

		/// <summary>No footstep sounds.</summary>
		NoSteps = 0x2000,

		/// <summary>Don't collide against curves with this set.</summary>
		NonSolid = 0x4000,

		/// <summary>Act as a light filter during q3map -light.</summary>
		LightFilter = 0x8000,

		/// <summary>Do per-pixel light shadow casting in q3map.</summary>
		AlphaShadow = 0x10000,

		/// <summary>Don't dlight even if solid (solid lava, skies).</summary>
		NoDLight = 0x20000
	}

	public enum BspFaceType
	{
		Normal = 1,
		Patch = 2,
		Mesh = 3,
		Flare = 4
	}
}