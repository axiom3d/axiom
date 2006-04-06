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


#region Namespace Declarations

using System;
using System.Collections;

using Axiom;
using DotNet3D.Math;

#endregion Namespace Declarations
			
namespace Axiom.SceneManagers.Octree
{
    /// <summary>
    /// Summary description for TerrainRenderable.
    /// </summary>
    public class TerrainRenderable : MovableObject, IRenderable
    {
        #region Fields

        protected Vector3 center;
        protected Material material;
        protected TerrainRenderable[] neighbors = new TerrainRenderable[4];
        protected AxisAlignedBox box = new AxisAlignedBox();
        protected TerrainOptions options;
        protected VertexData terrain;
        protected IndexData[,] levelIndex = new IndexData[16, 16];
        protected int renderLevel;
        protected int forcedRenderLevel;
        protected Real currentL;
        protected int numMipMaps;
        protected int size;
        protected Real[] minLevelDistSqr;
        protected Hashtable customParams = new Hashtable();

        const int POSITION = 0;
        const int NORMAL = 1;
        const int TEXCOORD = 2;
        const int COLORS = 3;

        #endregion Fields

        /// <summary>
        ///     Default constructor.
        /// </summary>
        public TerrainRenderable()
        {
            renderLevel = 1;
            forcedRenderLevel = -1;
        }

        #region Methods

        public void SetMaterial( Material mat )
        {
            this.material = mat;
        }

        public void Init( TerrainOptions options )
        {
            this.options = options;

            numMipMaps = options.maxMipmap;
            size = options.size;

            terrain = new VertexData();
            terrain.vertexStart = 0;
            // Turbo: appended factor 3
            //        Not sure about that, but without that the terrain manager seems
            //        to mess up memory because of buffer overruns
            //terrain.vertexCount = options.size * options.size;
            terrain.vertexCount = options.size * options.size * 3;

            VertexDeclaration decl = terrain.vertexDeclaration;
            VertexBufferBinding binding = terrain.vertexBufferBinding;

            int offset = 0;

            // Position/Normal
            decl.AddElement( POSITION, 0, VertexElementType.Float3, VertexElementSemantic.Position );
            decl.AddElement( NORMAL, 0, VertexElementType.Float3, VertexElementSemantic.Normal );

            // TexCoords
            decl.AddElement( TEXCOORD, offset, VertexElementType.Float2, VertexElementSemantic.TexCoords, 0 );
            offset += VertexElement.GetTypeSize( VertexElementType.Float2 );
            decl.AddElement( TEXCOORD, offset, VertexElementType.Float2, VertexElementSemantic.TexCoords, 1 );
            offset += VertexElement.GetTypeSize( VertexElementType.Float2 );
            // TODO Color

            HardwareVertexBuffer buffer =
                HardwareBufferManager.Instance.CreateVertexBuffer(
                decl.GetVertexSize( POSITION ),
                terrain.vertexCount,
                BufferUsage.StaticWriteOnly, true );

            binding.SetBinding( POSITION, buffer );


            buffer =
                HardwareBufferManager.Instance.CreateVertexBuffer(
                decl.GetVertexSize( NORMAL ),
                terrain.vertexCount,
                BufferUsage.StaticWriteOnly, true );

            binding.SetBinding( NORMAL, buffer );

            buffer =
                HardwareBufferManager.Instance.CreateVertexBuffer(
                offset,
                terrain.vertexCount,
                BufferUsage.StaticWriteOnly, true );

            binding.SetBinding( TEXCOORD, buffer );

            minLevelDistSqr = new Real[numMipMaps];

            int endx = options.startx + options.size;
            int endz = options.startz + options.size;

            // TODO name buffers different so we can unlock
            HardwareVertexBuffer posBuffer = binding.GetBuffer( POSITION );
            IntPtr pos = posBuffer.Lock( BufferLocking.Discard );

            HardwareVertexBuffer texBuffer = binding.GetBuffer( TEXCOORD );
            IntPtr tex = texBuffer.Lock( BufferLocking.Discard );

            Real min = 99999999, max = 0;

            unsafe
            {
                Real* posPtr = (Real*)pos.ToPointer();
                Real* texPtr = (Real*)tex.ToPointer();

                int posCount = 0;
                int texCount = 0;

                for ( int j = options.startz; j < endz; j++ )
                {
                    for ( int i = options.startx; i < endx; i++ )
                    {
                        Real height = options.GetWorldHeight( i, j ) * options.scaley;

                        posPtr[posCount++] = (Real)i * options.scalex;
                        posPtr[posCount++] = height;
                        posPtr[posCount++] = (Real)j * options.scalez;

                        texPtr[texCount++] = (Real)i / (Real)options.worldSize;
                        texPtr[texCount++] = (Real)j / (Real)options.worldSize;

                        texPtr[texCount++] = ( (Real)i / (Real)options.size ) * (Real)options.detailTile;
                        texPtr[texCount++] = ( (Real)j / (Real)options.size ) * (Real)options.detailTile;

                        if ( height < min )
                        {
                            min = height;
                        }

                        if ( height > max )
                        {
                            max = height;
                        }
                    } // for i
                } // for j
            } // unsafe

            // unlock the buffers
            posBuffer.Unlock();
            texBuffer.Unlock();

            box.SetExtents(
                new Vector3( (Real)options.startx * options.scalex, min, (Real)options.startz * options.scalez ),
                new Vector3( (Real)( endx - 1 ) * options.scalex, max, (Real)( endz - 1 ) * options.scalez ) );


            center = new Vector3( ( options.startx * options.scalex + endx - 1 ) / 2,
                ( min + max ) / 2,
                ( options.startz * options.scalez + endz - 1 ) / 2 );

            Real C = CalculateCFactor();

            CalculateMinLevelDist2( C );
        }

        public Real GetHeightAt( Real x, Real z )
        {
            Vector3 start, end;

            start.x = GetVertex( 0, 0, 0 );
            start.y = GetVertex( 0, 0, 1 );
            start.z = GetVertex( 0, 0, 2 );

            end.x = GetVertex( options.size - 1, options.size - 1, 0 );
            end.y = GetVertex( options.size - 1, options.size - 1, 1 );
            end.z = GetVertex( options.size - 1, options.size - 1, 2 );

            // safety catch.  if the point asked for is outside of this tile, ask a neighbor

            if ( x < start.x )
            {
                if ( GetNeighbor( Neighbor.West ) != null )
                {
                    return GetNeighbor( Neighbor.West ).GetHeightAt( x, z );
                }
                else
                {
                    x = start.x;
                }
            }

            if ( x > end.x )
            {
                if ( GetNeighbor( Neighbor.East ) != null )
                {
                    return GetNeighbor( Neighbor.East ).GetHeightAt( x, z );
                }
                else
                {
                    x = end.x;
                }
            }

            if ( z < start.z )
            {
                if ( GetNeighbor( Neighbor.North ) != null )
                {
                    return GetNeighbor( Neighbor.North ).GetHeightAt( x, z );
                }
                else
                {
                    z = start.z;
                }
            }

            if ( z > end.z )
            {
                if ( GetNeighbor( Neighbor.South ) != null )
                {
                    return GetNeighbor( Neighbor.South ).GetHeightAt( x, z );
                }
                else
                {
                    z = end.z;
                }
            }

            Real xPct = ( x - start.x ) / ( end.x - start.x );
            Real zPct = ( z - start.z ) / ( end.z - start.z );

            Real xPt = xPct * (Real)( options.size - 1 );
            Real zPt = zPct * (Real)( options.size - 1 );

            int xIndex = (int)xPt;
            int zIndex = (int)zPt;

            xPct = xPt - xIndex;
            zPct = zPt - zIndex;

            // bilinear interpolcation to find the height
            Real t1 = GetVertex( xIndex, zIndex, 1 );
            Real t2 = GetVertex( xIndex + 1, zIndex, 1 );
            Real b1 = GetVertex( xIndex, zIndex + 1, 1 );
            Real b2 = GetVertex( xIndex + 1, zIndex + 1, 1 );

            Real midpoint = ( b1 + b2 ) / 2;

            if ( ( xPct + zPct ) <= 1 )
            {
                b2 = midpoint + ( midpoint - t1 );
            }
            else
            {
                t1 = midpoint + ( midpoint - b2 );
            }

            Real t = ( t1 * ( 1 - xPct ) ) + ( t2 * ( xPct ) );
            Real b = ( b1 * ( 1 - xPct ) ) + ( b2 * ( xPct ) );
            Real h = ( t * ( 1 - zPct ) ) + ( b * ( zPct ) );

            return h;
        }

        public TerrainRenderable GetNeighbor( Neighbor n )
        {
            return neighbors[(int)n];
        }

        /// <summary>
        ///     Returns the vertex coord for the given coordinates.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="z"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        public Real GetVertex( int x, int z, int n )
        {
            try
            {
                HardwareVertexBuffer buffer = terrain.vertexBufferBinding.GetBuffer( POSITION );

                Real[] vertex = new Real[1];

                IntPtr ptr = System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement( vertex, 0 );

                int offset = ( x * 3 + z * options.size * 3 + n ) * 4;

                buffer.ReadData( offset, 4, ptr );

                return vertex[0];
            }
            catch
            {
                int a = 1;
                return 0;
            }
        }

        public void SetNeighbor( Neighbor n, TerrainRenderable t )
        {
            neighbors[(int)n] = t;
        }

        public void AdjustRenderLevel( int i )
        {
            renderLevel = i;
            AlignNeighbors();
        }

        public void AlignNeighbors()
        {
            //ensure that there aren't any gaps...
            for ( int i = 0; i < 4; i++ )
            {
                if ( neighbors[i] != null && neighbors[i].renderLevel + 1 < renderLevel )
                    neighbors[i].AdjustRenderLevel( renderLevel - 1 );
            }
        }

        public Real CalculateCFactor()
        {
            Real A, T;

            A = (Real)options.nearPlane / Utility.Abs( options.topCoord );

            T = 2 * (Real)options.maxPixelError / (Real)options.vertRes;

            return A / T;
        }

        public void CalculateMinLevelDist2( Real C )
        {
            // level 1 has no delta
            minLevelDistSqr[0] = 0;

            for ( int level = 1; level < numMipMaps; level++ )
            {
                minLevelDistSqr[level] = 0;

                int step = 1 << level;

                for ( int j = 0; j < size - step; j += step )
                {
                    for ( int i = 0; i < size - step; i += step )
                    {
                        //check each height inbetween the steps.
                        Real h1 = GetVertex( i, j, 1 );
                        Real h2 = GetVertex( i + step, j, 1 );
                        Real h3 = GetVertex( i + step, j + step, 1 );
                        Real h4 = GetVertex( i, j + step, 1 );

                        for ( int z = 1; z < step; z++ )
                        {
                            for ( int x = 1; x < step; x++ )
                            {

                                Real zpct = z / step;
                                Real xpct = x / step;

                                //interpolated height
                                Real top = h3 * ( 1.0f - xpct ) + xpct * h4;
                                Real bottom = h1 * ( 1.0f - xpct ) + xpct * h2;

                                Real interp_h = top * ( 1.0f - zpct ) + zpct * bottom;

                                Real actual_h = GetVertex( i + x, j + z, 1 );
                                Real delta = Utility.Abs( interp_h - actual_h );

                                Real D2 = delta * delta * C * C;

                                if ( minLevelDistSqr[level] < D2 )
                                    minLevelDistSqr[level] = D2;
                            }
                        }
                    }
                }
            }

            //make sure the levels are increasing...
            for ( int i = 1; i < numMipMaps; i++ )
            {
                if ( minLevelDistSqr[i] < minLevelDistSqr[i - 1] )
                    minLevelDistSqr[i] = minLevelDistSqr[i - 1] + 1;
            }
        }

        public unsafe void CalculateNormals()
        {
            Vector3 normal;

            HardwareVertexBuffer buffer = null;
            VertexBufferBinding binding = terrain.vertexBufferBinding;
            if ( binding != null )
            {
                buffer = binding.GetBuffer( NORMAL );
            }
            else
            {
                int i = 0;
            }


            IntPtr norm = buffer.Lock( BufferLocking.Discard );

            Real* normPtr = (Real*)norm.ToPointer();
            int count = 0;

            for ( int j = 0; j < size; j++ )
            {
                for ( int i = 0; i < size; i++ )
                {
                    GetNormalAt( GetVertex( i, j, 0 ), GetVertex( i, j, 2 ), out normal );

                    normPtr[count++] = normal.x;
                    normPtr[count++] = normal.y;
                    normPtr[count++] = normal.z;
                }
            }

            buffer.Unlock();
        }

        public void GetNormalAt( Real x, Real z, out Vector3 result )
        {
            Vector3 here, left, down;
            here.x = x;
            here.y = GetHeightAt( x, z );
            here.z = z;

            left.x = x - 1;
            left.y = GetHeightAt( x - 1, z );
            left.z = z;

            down.x = x;
            down.y = GetHeightAt( x, z + 1 );
            down.z = z + 1;

            left = left - here;

            down = down - here;

            left.Normalize();
            down.Normalize();

            result = left.CrossProduct( down );
            result.Normalize();
        }

        #endregion Methods

        #region MovableObject Members

        public override AxisAlignedBox BoundingBox
        {
            get
            {
                return box;
            }
        }

        public override Real BoundingRadius
        {
            get
            {
                return 0;
            }
        }

        public override void NotifyCurrentCamera( Camera camera )
        {
            if ( forcedRenderLevel >= 0 )
            {
                renderLevel = forcedRenderLevel;
                return;
            }

            int oldLevel = renderLevel;

            Vector3 cpos = camera.Position;
            Vector3 diff = center - cpos;

            Real L = diff.LengthSquared;

            currentL = L;

            renderLevel = -1;

            for ( int i = 0; i < numMipMaps; i++ )
            {
                if ( minLevelDistSqr[i] > L )
                {
                    renderLevel = i - 1;
                    break;
                }
            }

            if ( renderLevel < 0 )
            {
                renderLevel = numMipMaps - 1;
            }
        }

        public override void UpdateRenderQueue( RenderQueue queue )
        {
            queue.AddRenderable( this );
        }

        #endregion MovableObject Members

        #region IRenderable Members

        public bool CastsShadows
        {
            get
            {
                return false;
            }
        }

        public Real GetSquaredViewDepth( Camera camera )
        {
            Vector3 diff = center - camera.DerivedPosition;

            return diff.LengthSquared;
        }

        public bool UseIdentityView
        {
            get
            {
                return false;
            }
        }

        public bool UseIdentityProjection
        {
            get
            {
                return false;
            }
        }

        public Vector3 WorldPosition
        {
            get
            {
                return parentNode.DerivedPosition;
            }
        }

        public unsafe void GetRenderOperation( RenderOperation op )
        {
            int east = 0, west = 0, north = 0, south = 0;

            int step = 1 << renderLevel;

            int indexArray = 0;

            int numIndexes = 0;

            if ( neighbors[(int)Neighbor.East] != null && neighbors[(int)Neighbor.East].renderLevel > renderLevel )
            {
                east = step;
                indexArray |= (int)Tile.East;
            }

            if ( neighbors[(int)Neighbor.West] != null && neighbors[(int)Neighbor.West].renderLevel > renderLevel )
            {
                west = step;
                indexArray |= (int)Tile.West;
            }

            if ( neighbors[(int)Neighbor.North] != null && neighbors[(int)Neighbor.North].renderLevel > renderLevel )
            {
                north = step;
                indexArray |= (int)Tile.North;
            }

            if ( neighbors[(int)Neighbor.South] != null && neighbors[(int)Neighbor.South].renderLevel > renderLevel )
            {
                south = step;
                indexArray |= (int)Tile.South;
            }

            IndexData indexData = null;

            if ( levelIndex[renderLevel, indexArray] != null )
            {
                indexData = levelIndex[renderLevel, indexArray];
            }
            else
            {
                int newLength = ( size / step ) * ( size / step ) * 2 * 2 * 2;
                //this is the maximum for a level.  It wastes a little, but shouldn't be a problem.

                indexData = new IndexData();
                indexData.indexBuffer =
                HardwareBufferManager.Instance.CreateIndexBuffer(
                    IndexType.Size16,
                    newLength,
                    BufferUsage.StaticWriteOnly );

                //indexCache.Add(indexData);

                numIndexes = 0;

                IntPtr idx = indexData.indexBuffer.Lock( BufferLocking.Discard );

                short* idxPtr = (short*)idx.ToPointer();
                int count = 0;

                for ( int j = north; j < size - 1 - south; j += step )
                {
                    for ( int i = west; i < size - 1 - east; i += step )
                    {
                        //triangles
                        idxPtr[count++] = GetIndex( i, j );
                        numIndexes++;
                        idxPtr[count++] = GetIndex( i, j + step );
                        numIndexes++;
                        idxPtr[count++] = GetIndex( i + step, j );
                        numIndexes++;

                        idxPtr[count++] = GetIndex( i, j + step );
                        numIndexes++;
                        idxPtr[count++] = GetIndex( i + step, j + step );
                        numIndexes++;
                        idxPtr[count++] = GetIndex( i + step, j );
                        numIndexes++;
                    }
                }

                int substep = step << 1;

                if ( west > 0 )
                {

                    for ( int j = 0; j < size - 1; j += substep )
                    {
                        //skip the first bit of the corner if the north side is a different level as well.
                        if ( j > 0 || north == 0 )
                        {
                            idxPtr[count++] = GetIndex( 0, j );
                            numIndexes++;
                            idxPtr[count++] = GetIndex( step, j + step );
                            numIndexes++;
                            idxPtr[count++] = GetIndex( step, j );
                            numIndexes++;
                        }

                        idxPtr[count++] = GetIndex( step, j + step );
                        numIndexes++;
                        idxPtr[count++] = GetIndex( 0, j );
                        numIndexes++;
                        idxPtr[count++] = GetIndex( 0, j + step + step );
                        numIndexes++;

                        if ( j < options.size - 1 - substep || south == 0 )
                        {
                            idxPtr[count++] = GetIndex( step, j + step );
                            numIndexes++;
                            idxPtr[count++] = GetIndex( 0, j + step + step );
                            numIndexes++;
                            idxPtr[count++] = GetIndex( step, j + step + step );
                            numIndexes++;
                        }
                    }
                }

                if ( east > 0 )
                {
                    int x = options.size - 1;

                    for ( int j = 0; j < size - 1; j += substep )
                    {
                        //skip the first bit of the corner if the north side is a different level as well.
                        if ( j > 0 || north == 0 )
                        {
                            idxPtr[count++] = GetIndex( x, j );
                            numIndexes++;
                            idxPtr[count++] = GetIndex( x - step, j );
                            numIndexes++;
                            idxPtr[count++] = GetIndex( x - step, j + step );
                            numIndexes++;
                        }

                        idxPtr[count++] = GetIndex( x, j );
                        numIndexes++;
                        idxPtr[count++] = GetIndex( x - step, j + step );
                        numIndexes++;
                        idxPtr[count++] = GetIndex( x, j + step + step );
                        numIndexes++;

                        if ( j < options.size - 1 - substep || south == 0 )
                        {
                            idxPtr[count++] = GetIndex( x, j + step + step );
                            numIndexes++;
                            idxPtr[count++] = GetIndex( x - step, j + step );
                            numIndexes++;
                            idxPtr[count++] = GetIndex( x - step, j + step + step );
                            numIndexes++;
                        }
                    }
                }

                if ( south > 0 )
                {
                    int x = options.size - 1;

                    for ( int j = 0; j < size - 1; j += substep )
                    {
                        //skip the first bit of the corner if the north side is a different level as well.
                        if ( j > 0 || west == 0 )
                        {
                            idxPtr[count++] = GetIndex( j, x - step );
                            numIndexes++;
                            idxPtr[count++] = GetIndex( j, x );
                            numIndexes++;
                            idxPtr[count++] = GetIndex( j + step, x - step );
                            numIndexes++;
                        }

                        idxPtr[count++] = GetIndex( j + step, x - step );
                        numIndexes++;
                        idxPtr[count++] = GetIndex( j, x );
                        numIndexes++;
                        idxPtr[count++] = GetIndex( j + step + step, x );
                        numIndexes++;

                        if ( j < options.size - 1 - substep || east == 0 )
                        {
                            idxPtr[count++] = GetIndex( j + step, x - step );
                            numIndexes++;
                            idxPtr[count++] = GetIndex( j + step + step, x );
                            numIndexes++;
                            idxPtr[count++] = GetIndex( j + step + step, x - step );
                            numIndexes++;
                        }
                    }
                }

                if ( north > 0 )
                {
                    for ( int j = 0; j < size - 1; j += substep )
                    {
                        //skip the first bit of the corner if the north side is a different level as well.
                        if ( j > 0 || west == 0 )
                        {
                            idxPtr[count++] = GetIndex( j, 0 );
                            numIndexes++;
                            idxPtr[count++] = GetIndex( j, step );
                            numIndexes++;
                            idxPtr[count++] = GetIndex( j + step, step );
                            numIndexes++;
                        }

                        idxPtr[count++] = GetIndex( j, 0 );
                        numIndexes++;
                        idxPtr[count++] = GetIndex( j + step, step );
                        numIndexes++;
                        idxPtr[count++] = GetIndex( j + step + step, 0 );
                        numIndexes++;

                        if ( j < options.size - 1 - substep || east == 0 )
                        {
                            idxPtr[count++] = GetIndex( j + step + step, 0 );
                            numIndexes++;
                            idxPtr[count++] = GetIndex( j + step, step );
                            numIndexes++;
                            idxPtr[count++] = GetIndex( j + step + step, step );
                            numIndexes++;
                        }
                    }

                }

                indexData.indexBuffer.Unlock();
                indexData.indexCount = numIndexes;
                indexData.indexStart = 0;

                levelIndex[renderLevel, indexArray] = indexData;
            }

            op.useIndices = true;
            op.operationType = OperationType.TriangleList;
            op.vertexData = terrain;
            op.indexData = indexData;

            //renderedTris += ( indexData->indexCount / 3 );

            //mRenderLevelChanged = false;
        }

        public short GetIndex( int x, int z )
        {
            return (short)( x + z * options.size );
        }

        public void GetWorldTransforms( Matrix4[] matrices )
        {
            // TODO Add Node.FullTransform?
            parentNode.GetWorldTransforms( matrices );
        }

        public Quaternion WorldOrientation
        {
            get
            {
                return parentNode.DerivedOrientation;
            }
        }

        public SceneDetailLevel RenderDetail
        {
            get
            {
                return SceneDetailLevel.Solid;
            }
        }

        public Material Material
        {
            get
            {
                return material;
            }
        }

        public LightList Lights
        {
            get
            {
                return parentNode.Lights;
            }
        }

        public Technique Technique
        {
            get
            {
                return material.GetBestTechnique();
            }
        }

        public bool NormalizeNormals
        {
            get
            {
                return false;
            }
        }

        public ushort NumWorldTransforms
        {
            get
            {
                return 1;
            }
        }

        public Vector4 GetCustomParameter( int index )
        {
            if ( customParams[index] == null )
            {
                throw new Exception( "A parameter was not found at the given index" );
            }
            else
            {
                return (Vector4)customParams[index];
            }
        }

        public void SetCustomParameter( int index, Vector4 val )
        {
            customParams[index] = val;
        }

        public void UpdateCustomGpuParameter( GpuProgramParameters.AutoConstantEntry entry, GpuProgramParameters gpuParams )
        {
            if ( customParams[entry.data] != null )
            {
                gpuParams.SetConstant( entry.index, (Vector4)customParams[entry.data] );
            }
        }

        #endregion
    }

    public class TerrainOptions
    {
        public TerrainOptions()
        {
            size = 0;
            worldSize = 0;
            startx = 0;
            startz = 0;
            maxMipmap = 0;
            scalex = 1;
            scaley = 1;
            scalez = 1;
            maxPixelError = 4;
            vertRes = 768;
            topCoord = 1;
            nearPlane = 1;
            detailTile = 1;
            isLit = false;
            isColored = false;
        }


        public int GetWorldHeight( int x, int z )
        {
            return data[( ( z * worldSize ) + x )];
        }

        public byte[] data;     //pointer to the world 2D data.
        public int size;         //size of this square block
        public int worldSize;   //size of the world.
        public int startx;
        public int startz; //starting coords of this block.
        public int maxMipmap;  //max mip_map level
        public Real scalex, scaley, scalez;

        public int maxPixelError;
        public int nearPlane;
        public int vertRes;
        public int detailTile;
        public Real topCoord;

        public bool isLit;
        public bool isColored;

    }

}
