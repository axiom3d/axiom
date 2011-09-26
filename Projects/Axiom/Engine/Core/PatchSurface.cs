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
using System.Diagnostics;
using System.Runtime.InteropServices;

using Axiom.Core;
using Axiom.CrossPlatform;
using Axiom.Graphics;
using Axiom.Math;
using Axiom.Math.Collections;
using System.Collections.Generic;

#endregion Namespace Declarations

namespace Axiom.Core
{
	/// <summary>
	///     A surface which is defined by curves of some kind to form a patch, e.g. a Bezier patch.
	/// </summary>
	/// <remarks>
	///     This object will take a list of control points with various assorted data, and will
	///     subdivide it into a patch mesh. Currently only Bezier curves are supported for defining
	///     the surface, but other techniques such as NURBS would follow the same basic approach.
	/// </remarks>
	public class PatchSurface
	{
		#region Fields

		/// <summary>
		///     Vertex declaration describing the control point buffer.
		/// </summary>
		protected VertexDeclaration declaration;
		/// <summary>
		///     Buffer containing the system-memory control points.
		/// </summary>
		protected BufferBase controlPointBuffer;
		/// <summary>
		///     Type of surface.
		/// </summary>
		protected PatchSurfaceType type;
		/// <summary>
		///     Width in control points.
		/// </summary>
		protected int controlWidth;
		/// <summary>
		///     Height in control points.
		/// </summary>
		protected int controlHeight;
		/// <summary>
		///     Total number of control level.
		/// </summary>
		protected int controlCount;
		/// <summary>
		///     U-direction subdivision level.
		/// </summary>
		protected int uLevel;
		/// <summary>
		///    V-direction subdivision level.
		/// </summary>
		protected int vLevel;
		/// <summary>
		///     Max U subdivision level.
		/// </summary>
		protected int maxULevel;
		/// <summary>
		///     Max V subdivision level.
		/// </summary>
		protected int maxVLevel;
		/// <summary>
		///     Width of the subdivided mesh (big enough for max level).
		/// </summary>
		protected int meshWidth;
		/// <summary>
		///     Height of the subdivided mesh (big enough for max level).
		/// </summary>
		protected int meshHeight;
		/// <summary>
		///     Which side is visible.
		/// </summary>
		protected VisibleSide side;
		/// <summary>
		///     Mesh subdivision factor.
		/// </summary>
		protected float subdivisionFactor;
		/// <summary>
		///     List of control points.
		/// </summary>
		protected List<Vector3> controlPoints = new List<Vector3>();

		protected HardwareVertexBuffer vertexBuffer;
		protected HardwareIndexBuffer indexBuffer;
		protected int vertexOffset;
		protected int indexOffset;
		protected int requiredVertexCount;
		protected int requiredIndexCount;
		protected int currentIndexCount;
		protected AxisAlignedBox aabb = AxisAlignedBox.Null;
		protected float boundingSphereRadius;

		/// <summary>
		///     Constant for indicating automatic determination of subdivision level for patches.
		/// </summary>
		const int AUTO_LEVEL = -1;

		#endregion Fields

		#region Constructor

		/// <summary>
		///     Default contructor.
		/// </summary>
		public PatchSurface()
		{
			type = PatchSurfaceType.Bezier;
		}

		#endregion Constructor

		#region Finalizer

		~PatchSurface()
		{
		}

		#endregion Finalizer

		#region Methods

		/// <summary>
		///     Sets up the surface by defining it's control points, type and initial subdivision level.
		/// </summary>
		/// <remarks>
		///     This method initializes the surface by passing it a set of control points. The type of curves to be used
		///     are also defined here, although the only supported option currently is a bezier patch. You can also
		///     specify a global subdivision level here if you like, although it is recommended that the parameter
		///     is left as AUTO_LEVEL, which means the system decides how much subdivision is required (based on the
		///     curvature of the surface).
		/// </remarks>
		/// <param name="controlPointArray">
		///     An array containing the vertex data which define control points of the curves
		///     rather than actual vertices. Note that you are expected to provide not
		///     just position information, but potentially normals and texture coordinates too.
		///     The array is internally treated as a contiguous memory buffer without any gaps between the elements.
		///     The format of the buffer is defined in the VertexDeclaration parameter.
		/// </param>
		/// <param name="decl">
		///     VertexDeclaration describing the contents of the buffer.
		///     Note this declaration must _only_ draw on buffer source 0!
		/// </param>
		/// <param name="width">Specifies the width of the patch in control points.</param>
		/// <param name="height">Specifies the height of the patch in control points.</param>
		public void DefineSurface( Array controlPointArray, VertexDeclaration decl, int width, int height )
		{
			DefineSurface( controlPointArray, decl, width, height, PatchSurfaceType.Bezier, AUTO_LEVEL, AUTO_LEVEL, VisibleSide.Front );
		}

		/// <summary>
		///     Sets up the surface by defining it's control points, type and initial subdivision level.
		/// </summary>
		/// <remarks>
		///     This method initialises the surface by passing it a set of control points. The type of curves to be used
		///     are also defined here, although the only supported option currently is a bezier patch. You can also
		///     specify a global subdivision level here if you like, although it is recommended that the parameter
		///     is left as AUTO_LEVEL, which means the system decides how much subdivision is required (based on the
		///     curvature of the surface).
		/// </remarks>
		/// <param name="controlPointArray">
		///     An array containing the vertex data which define control points of the curves
		///     rather than actual vertices. Note that you are expected to provide not
		///     just position information, but potentially normals and texture coordinates too.
		///     The array is internally treated as a contiguous memory buffer without any gaps between the elements.
		///     The format of the buffer is defined in the VertexDeclaration parameter.
		/// </param>
        /// <param name="declaration">
		///     VertexDeclaration describing the contents of the buffer.
		///     Note this declaration must _only_ draw on buffer source 0!
		/// </param>
		/// <param name="width">Specifies the width of the patch in control points.</param>
		/// <param name="height">Specifies the height of the patch in control points.</param>
		/// <param name="type">The type of surface.</param>
		/// <param name="uMaxSubdivisionLevel">
		///     If you want to manually set the top level of subdivision,
		///     do it here, otherwise let the system decide.
		/// </param>
		/// <param name="vMaxSubdivisionLevel">
		///     If you want to manually set the top level of subdivision,
		///     do it here, otherwise let the system decide.
		/// </param>
        /// <param name="visibleSide">Determines which side of the patch (or both) triangles are generated for.</param>
		public void DefineSurface( Array controlPointArray, VertexDeclaration declaration, int width, int height,
			PatchSurfaceType type, int uMaxSubdivisionLevel, int vMaxSubdivisionLevel, VisibleSide visibleSide )
		{

			if ( height == 0 || width == 0 )
			{
				return; // Do nothing - garbage
			}

			//pin the input to have a pointer
			Memory.UnpinObject( controlPointArray );
			this.controlPointBuffer = Memory.PinObject( controlPointArray );

			this.type = type;
			this.controlWidth = width;
			this.controlHeight = height;
			this.controlCount = width * height;
			this.declaration = declaration;

			// Copy positions into Vector3 vector
			controlPoints.Clear();
			var elem = declaration.FindElementBySemantic( VertexElementSemantic.Position );
			var vertSize = declaration.GetVertexSize( 0 );

#if !AXIOM_SAFE_ONLY
			unsafe
#endif
			{
				var pVert = controlPointBuffer;
				for ( var i = 0; i < controlCount; i++ )
				{
					var pReal = ( pVert + (( i * vertSize ) + elem.Offset )).ToFloatPointer();
					controlPoints.Add( new Vector3( pReal[ 0 ], pReal[ 1 ], pReal[ 2 ] ) );
				}
			}
			this.side = visibleSide;

			// Determine max level
			// Initialize to 100% detail
			subdivisionFactor = 1.0f;

			if ( uMaxSubdivisionLevel == AUTO_LEVEL )
			{
				uLevel = maxULevel = GetAutoULevel();
			}
			else
			{
				uLevel = maxULevel = uMaxSubdivisionLevel;
			}

			if ( vMaxSubdivisionLevel == AUTO_LEVEL )
			{
				vLevel = maxVLevel = GetAutoVLevel();
			}
			else
			{
				vLevel = maxVLevel = vMaxSubdivisionLevel;
			}

			// Derive mesh width / height
			meshWidth = ( LevelWidth( maxULevel ) - 1 ) * ( ( controlWidth - 1 ) / 2 ) + 1;
			meshHeight = ( LevelWidth( maxVLevel ) - 1 ) * ( ( controlHeight - 1 ) / 2 ) + 1;

			// Calculate number of required vertices / indexes at max resolution
			requiredVertexCount = meshWidth * meshHeight;
			var iterations = ( side == VisibleSide.Both ) ? 2 : 1;
			requiredIndexCount = ( meshWidth - 1 ) * ( meshHeight - 1 ) * 2 * iterations * 3;

			// Calculate bounds based on control points
			var min = Vector3.Zero;
			var max = Vector3.Zero;
			var maxSqRadius = 0.0f;
			var first = true;

			for ( var i = 0; i < controlPoints.Count; i++ )
			{
				var vec = controlPoints[ i ];
				if ( first )
				{
					min = max = vec;
					maxSqRadius = vec.LengthSquared;
					first = false;
				}
				else
				{
					min.Floor( vec );
					max.Ceil( vec );
					maxSqRadius = Utility.Max( vec.LengthSquared, maxSqRadius );
				}
			}

			// set the bounds of the patch
			aabb.SetExtents( min, max );
			boundingSphereRadius = Utility.Sqrt( maxSqRadius );
		}


		/// <summary>
		///     Tells the system to build the mesh relating to the surface into externally created buffers.
		/// </summary>
		/// <remarks>
		///     The VertexDeclaration of the vertex buffer must be identical to the one passed into
		///     <see cref="DefineSurface(Array, VertexDeclaration, int, int)"/>.  In addition, there must be enough space in the buffer to
		///     accommodate the patch at full detail level; you should check <see cref="RequiredVertexCount"/>
		///     and <see cref="RequiredIndexCount"/> to determine this. This method does not create an internal
		///     mesh for this patch and so GetMesh will return null if you call it after building the
		///     patch this way.
		/// </remarks>
		/// <param name="destVertexBuffer">The destination vertex buffer in which to build the patch.</param>
		/// <param name="vertexStart">The offset at which to start writing vertices for this patch.</param>
		/// <param name="destIndexBuffer">The destination index buffer in which to build the patch.</param>
		/// <param name="indexStart">The offset at which to start writing indexes for this patch.</param>
		public void Build( HardwareVertexBuffer destVertexBuffer, int vertexStart, HardwareIndexBuffer destIndexBuffer, int indexStart )
		{
			if ( controlPoints.Count == 0 )
			{
				return;
			}

			vertexBuffer = destVertexBuffer;
			vertexOffset = vertexStart;
			indexBuffer = destIndexBuffer;
			indexOffset = indexStart;

			// lock just the region we are interested in
			var lockedBuffer = vertexBuffer.Lock(
				vertexOffset * declaration.GetVertexSize( 0 ),
				requiredVertexCount * declaration.GetVertexSize( 0 ),
				BufferLocking.NoOverwrite );

			DistributeControlPoints( lockedBuffer );

			// subdivide the curves to the max
			// Do u direction first, so need to step over v levels not done yet
			var vStep = 1 << maxVLevel;
			var uStep = 1 << maxULevel;

			// subdivide this row in u
			for ( var v = 0; v < meshHeight; v += vStep )
			{
				SubdivideCurve( lockedBuffer, v * meshWidth, uStep, meshWidth / uStep, uLevel );
			}

			// Now subdivide in v direction, this time all the u direction points are there so no step
			for ( var u = 0; u < meshWidth; u++ )
			{
				SubdivideCurve( lockedBuffer, u, vStep * meshWidth, meshHeight / vStep, vLevel );
			}

			// don't forget to unlock!
			vertexBuffer.Unlock();

			// Make triangles from mesh at this current level of detail
			MakeTriangles();
		}

		/// <summary>
		///     Internal method for finding the subdivision level given 3 control points.
		/// </summary>
		/// <param name="a">First control point.</param>
		/// <param name="b">Second control point.</param>
		/// <param name="c">Third control point.</param>
		/// <returns></returns>
		protected int FindLevel( ref Vector3 a, ref Vector3 b, ref Vector3 c )
		{
			// Derived from work by Bart Sekura in rogl
			// Apart from I think I fixed a bug - see below
			// I also commented the code, the only thing wrong with rogl is almost no comments!!
			const int maxLevels = 5;
			const float subdiv = 10;
			int level;

			var test = subdiv * subdiv;

			var s = Vector3.Zero;
			var t = Vector3.Zero;
			var d = Vector3.Zero;

			for ( level = 0; level < maxLevels - 1; level++ )
			{
				// Subdivide the 2 lines
				s = a.MidPoint( b );
				t = b.MidPoint( c );
				// Find the midpoint between the 2 midpoints
				c = s.MidPoint( t );
				// Get the vector between this subdivided midpoint and the middle point of the original line
				d = c - b;
				// Find the squared length, and break when small enough
				if ( d.Dot( d ) < test )
				{
					break;
				}

				b = a;
			}

			return level;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="lockedBuffer"></param>
		protected void DistributeControlPoints( BufferBase lockedBuffer )
		{
#if !AXIOM_SAFE_ONLY
            unsafe
#endif
            {
                // Insert original control points into expanded mesh
                var uStep = 1 << uLevel;
                var vStep = 1 << vLevel;


                var elemPos = declaration.FindElementBySemantic(VertexElementSemantic.Position);
                var elemNorm = declaration.FindElementBySemantic(VertexElementSemantic.Normal);
                var elemTex0 = declaration.FindElementBySemantic(VertexElementSemantic.TexCoords, 0);
                var elemTex1 = declaration.FindElementBySemantic(VertexElementSemantic.TexCoords, 1);
                var elemDiffuse = declaration.FindElementBySemantic(VertexElementSemantic.Diffuse);

                var pSrc = controlPointBuffer;
                var vertexSize = declaration.GetVertexSize(0);

                for (var v = 0; v < meshHeight; v += vStep)
                {
                    // set dest by v from base
                    var pDest = lockedBuffer + (vertexSize*meshWidth*v);

                    for (var u = 0; u < meshWidth; u += uStep)
                    {
                        // Copy Position
                        var pSrcReal = (pSrc + elemPos.Offset).ToFloatPointer();
                        var pDestReal = (pDest + elemPos.Offset).ToFloatPointer();
                        pDestReal[0] = pSrcReal[0];
                        pDestReal[1] = pSrcReal[1];
                        pDestReal[2] = pSrcReal[2];

                        // Copy Normals
                        if (elemNorm != null)
                        {
                            pSrcReal = (pSrc + elemNorm.Offset).ToFloatPointer();
                            pDestReal = (pDest + elemNorm.Offset).ToFloatPointer();
                            pDestReal[0] = pSrcReal[0];
                            pDestReal[1] = pSrcReal[1];
                            pDestReal[2] = pSrcReal[2];
                        }

                        // Copy Diffuse
                        if (elemDiffuse != null)
                        {
                            var pSrcRGBA = (pSrc + elemDiffuse.Offset).ToIntPointer();
                            var pDestRGBA = (pDest + elemDiffuse.Offset).ToIntPointer();
                            pDestRGBA[0] = pSrcRGBA[0];
                        }

                        // Copy texture coords
                        if (elemTex0 != null)
                        {
                            pSrcReal = (pSrc + elemTex0.Offset).ToFloatPointer();
                            pDestReal = (pDest + elemTex0.Offset).ToFloatPointer();
                            for (var dim = 0; dim < VertexElement.GetTypeCount(elemTex0.Type); dim++)
                            {
                                pDestReal[dim] = pSrcReal[dim];
                            }
                        }
                        if (elemTex1 != null)
                        {
                            pSrcReal = (pSrc + elemTex1.Offset).ToFloatPointer();
                            pDestReal = (pDest + elemTex1.Offset).ToFloatPointer();
                            for (var dim = 0; dim < VertexElement.GetTypeCount(elemTex1.Type); dim++)
                            {
                                pDestReal[dim] = pSrcReal[dim];
                            }
                        }

                        // Increment source by one vertex
                        pSrc += vertexSize;
                        // Increment dest by 1 vertex * uStep
                        pDest += vertexSize*uStep;
                    } // u
                } // v
            }
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="lockedBuffer"></param>
		/// <param name="startIdx"></param>
		/// <param name="stepSize"></param>
		/// <param name="numSteps"></param>
		/// <param name="iterations"></param>
		protected void SubdivideCurve( BufferBase lockedBuffer, int startIdx, int stepSize, int numSteps, int iterations )
		{
			// Subdivides a curve within a sparsely populated buffer (gaps are already there to be interpolated into)
			int leftIdx, rightIdx, destIdx, halfStep, maxIdx;
			bool firstSegment;

			maxIdx = startIdx + ( numSteps * stepSize );
			var step = stepSize;

			while ( iterations-- > 0 )
			{
				halfStep = step / 2;
				leftIdx = startIdx;
				destIdx = leftIdx + halfStep;
				rightIdx = leftIdx + step;
				firstSegment = true;

				while ( leftIdx < maxIdx )
				{
					// Interpolate
					InterpolateVertexData( lockedBuffer, leftIdx, rightIdx, destIdx );

					// If 2nd or more segment, interpolate current left between current and last mid points
					if ( !firstSegment )
					{
						InterpolateVertexData( lockedBuffer, leftIdx - halfStep, leftIdx + halfStep, leftIdx );
					}
					// Next segment
					leftIdx = rightIdx;
					destIdx = leftIdx + halfStep;
					rightIdx = leftIdx + step;
					firstSegment = false;
				}

				step = halfStep;
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="lockedBuffer"></param>
		/// <param name="leftIndex"></param>
		/// <param name="rightIndex"></param>
		/// <param name="destIndex"></param>
		protected void InterpolateVertexData( BufferBase lockedBuffer, int leftIndex, int rightIndex, int destIndex )
		{
#if !AXIOM_SAFE_ONLY
            unsafe
#endif
            {
                var vertexSize = declaration.GetVertexSize(0);
                var elemPos = declaration.FindElementBySemantic(VertexElementSemantic.Position);
                var elemNorm = declaration.FindElementBySemantic(VertexElementSemantic.Normal);
                var elemDiffuse = declaration.FindElementBySemantic(VertexElementSemantic.Diffuse);
                var elemTex0 = declaration.FindElementBySemantic(VertexElementSemantic.TexCoords, 0);
                var elemTex1 = declaration.FindElementBySemantic(VertexElementSemantic.TexCoords, 1);

                //byte* pDestChar, pLeftChar, pRightChar;

                // Set up pointers & interpolate
                var pDest = (lockedBuffer + (vertexSize*destIndex));
                var pLeft = (lockedBuffer + (vertexSize*leftIndex));
                var pRight = (lockedBuffer + (vertexSize*rightIndex));

                // Position
                var pDestReal = (pDest + elemPos.Offset).ToFloatPointer();
                var pLeftReal = (pLeft + elemPos.Offset).ToFloatPointer();
                var pRightReal = (pRight + elemPos.Offset).ToFloatPointer();

                pDestReal[0] = (pLeftReal[0] + pRightReal[0])*0.5f;
                pDestReal[1] = (pLeftReal[1] + pRightReal[1])*0.5f;
                pDestReal[2] = (pLeftReal[2] + pRightReal[2])*0.5f;

                if (elemNorm != null)
                {
                    // Normals
                    pDestReal = (pDest + elemNorm.Offset).ToFloatPointer();
                    pLeftReal = (pLeft + elemNorm.Offset).ToFloatPointer();
                    pRightReal = (pRight + elemNorm.Offset).ToFloatPointer();

                    var norm = Vector3.Zero;
                    norm.x = (pLeftReal[0] + pRightReal[0])*0.5f;
                    norm.y = (pLeftReal[1] + pRightReal[1])*0.5f;
                    norm.z = (pLeftReal[2] + pRightReal[2])*0.5f;
                    norm.Normalize();

                    pDestReal[0] = norm.x;
                    pDestReal[1] = norm.y;
                    pDestReal[2] = norm.z;
                }
                if (elemDiffuse != null)
                {
                    // Blend each byte individually
                    var pDestChar = (pDest + elemDiffuse.Offset).ToBytePointer();
                    var pLeftChar = (pLeft + elemDiffuse.Offset).ToBytePointer();
                    var pRightChar = (pRight + elemDiffuse.Offset).ToBytePointer();

                    // 4 bytes to RGBA
                    pDestChar[0] = (byte) ((pLeftChar[0] + pRightReal[0])*0.5f);
                    pDestChar[1] = (byte) ((pLeftChar[1] + pRightChar[1])*0.5f);
                    pDestChar[2] = (byte) ((pLeftChar[2] + pRightChar[2])*0.5f);
                    pDestChar[3] = (byte) ((pLeftChar[3] + pRightChar[3])*0.5f);
                }
                if (elemTex0 != null)
                {
                    // Blend each byte individually
                    pDestReal = (pDest + elemTex0.Offset).ToFloatPointer();
                    pLeftReal = (pLeft + elemTex0.Offset).ToFloatPointer();
                    pRightReal = (pRight + elemTex0.Offset).ToFloatPointer();

                    for (var dim = 0; dim < VertexElement.GetTypeCount(elemTex0.Type); dim++)
                    {
                        pDestReal[dim] = (pLeftReal[dim] + pRightReal[dim])*0.5f;
                    }
                }
                if (elemTex1 != null)
                {
                    // Blend each byte individually
                    pDestReal = (pDest + elemTex1.Offset).ToFloatPointer();
                    pLeftReal = (pLeft + elemTex1.Offset).ToFloatPointer();
                    pRightReal = (pRight + elemTex1.Offset).ToFloatPointer();

                    for (var dim = 0; dim < VertexElement.GetTypeCount(elemTex1.Type); dim++)
                    {
                        pDestReal[dim] = (pLeftReal[dim] + pRightReal[dim])*0.5f;
                    }
                }
            }
		}

		/// <summary>
		///
		/// </summary>
		protected void MakeTriangles()
		{
			// Our vertex buffer is subdivided to the highest level, we need to generate tris
			// which step over the vertices we don't need for this level of detail.

			// Calculate steps
			var vStep = 1 << ( maxVLevel - vLevel );
			var uStep = 1 << ( maxULevel - uLevel );
			var currentWidth = ( LevelWidth( uLevel ) - 1 ) * ( ( controlWidth - 1 ) / 2 ) + 1;
			var currentHeight = ( LevelWidth( vLevel ) - 1 ) * ( ( controlHeight - 1 ) / 2 ) + 1;

			var use32bitindexes = ( indexBuffer.Type == IndexType.Size32 );

			// The mesh is built, just make a list of indexes to spit out the triangles
			int vInc, uInc;

			int vCount, uCount, v, u, iterations;

			if ( side == VisibleSide.Both )
			{
				iterations = 2;
				vInc = vStep;
				v = 0; // Start with front
			}
			else
			{
				iterations = 1;
				if ( side == VisibleSide.Front )
				{
					vInc = vStep;
					v = 0;
				}
				else
				{
					vInc = -vStep;
					v = meshHeight - 1;
				}
			}

			// Calc num indexes
			currentIndexCount = ( currentWidth - 1 ) * ( currentHeight - 1 ) * 6 * iterations;

			int v1, v2, v3;
			var count = 0;

#if !AXIOM_SAFE_ONLY
            unsafe
#endif
            {
                // Lock just the section of the buffer we need
                var p32 = use32bitindexes
                              ? indexBuffer.Lock(
                                  indexOffset*sizeof(int),
                                  requiredIndexCount*sizeof(int),
                                  BufferLocking.NoOverwrite).ToIntPointer()
                              : null;
                var p16 = !use32bitindexes
                              ? indexBuffer.Lock(
                                  indexOffset*sizeof(short),
                                  requiredIndexCount*sizeof(short),
                                  BufferLocking.NoOverwrite).ToShortPointer()
                              : null;

                while (iterations-- > 0)
                {
                    // Make tris in a zigzag pattern (compatible with strips)
                    u = 0;
                    uInc = uStep; // Start with moving +u

                    vCount = currentHeight - 1;
                    while (vCount-- > 0)
                    {
                        uCount = currentWidth - 1;

                        while (uCount-- > 0)
                        {
                            // First Tri in cell
                            // -----------------
                            v1 = ((v + vInc)*meshWidth) + u;
                            v2 = (v*meshWidth) + u;
                            v3 = ((v + vInc)*meshWidth) + (u + uInc);

                            // Output indexes
                            if (use32bitindexes)
                            {
                                p32[count++] = v1;
                                p32[count++] = v2;
                                p32[count++] = v3;
                            }
                            else
                            {
                                p16[count++] = (short) v1;
                                p16[count++] = (short) v2;
                                p16[count++] = (short) v3;
                            }
                            // Second Tri in cell
                            // ------------------
                            v1 = ((v + vInc)*meshWidth) + (u + uInc);
                            v2 = (v*meshWidth) + u;
                            v3 = (v*meshWidth) + (u + uInc);

                            // Output indexes
                            if (use32bitindexes)
                            {
                                p32[count++] = v1;
                                p32[count++] = v2;
                                p32[count++] = v3;
                            }
                            else
                            {
                                p16[count++] = (short) v1;
                                p16[count++] = (short) v2;
                                p16[count++] = (short) v3;
                            }

                            // Next column
                            u += uInc;
                        }
                        // Next row
                        v += vInc;
                        u = 0;
                    }

                    // Reverse vInc for double sided
                    v = meshHeight - 1;
                    vInc = -vInc;

                }

                // don't forget to unlock!
                indexBuffer.Unlock();
            }
		}

		/// <summary>
		/// </summary>
		protected int GetAutoULevel()
		{
			return GetAutoULevel( false );
		}

		/// <summary>
		/// </summary>
		protected int GetAutoULevel( bool forMax )
		{
			// determine levels
			// Derived from work by Bart Sekura in Rogl
			var a = Vector3.Zero;
			var b = Vector3.Zero;
			var c = Vector3.Zero;

			var found = false;

			// Find u level
			for ( var v = 0; v < controlHeight; v++ )
			{
				for ( var u = 0; u < controlWidth - 1; u += 2 )
				{
					a = controlPoints[ v * controlWidth + u + 0 ];
					b = controlPoints[ v * controlWidth + u + 1 ];
					c = controlPoints[ v * controlWidth + u + 2 ];

					if ( a != c )
					{
						found = true;
						break;
					}
				}
				if ( found )
				{
					break;
				}
			}

			if ( !found )
			{
				throw new AxiomException( "Can't find suitable control points for determining U subdivision level" );
			}

			return FindLevel( ref a, ref b, ref c );
		}

		/// <summary>
		/// </summary>
		protected int GetAutoVLevel()
		{
			return GetAutoVLevel( false );
		}

		/// <summary>
		/// </summary>
		protected int GetAutoVLevel( bool forMax )
		{
			var a = Vector3.Zero;
			var b = Vector3.Zero;
			var c = Vector3.Zero;

			var found = false;

			for ( var u = 0; u < controlWidth; u++ )
			{
				for ( var v = 0; v < controlHeight - 1; v += 2 )
				{
					a = controlPoints[ v * controlWidth + u ];
					b = controlPoints[ ( v + 1 ) * controlWidth + u ];
					c = controlPoints[ ( v + 2 ) * controlWidth + u ];

					if ( a != c )
					{
						found = true;
						break;
					}
				}

				if ( found )
				{
					break;
				}
			}
			if ( !found )
			{
				throw new AxiomException( "Can't find suitable control points for determining U subdivision level" );
			}

			return FindLevel( ref a, ref b, ref c );
		}

		protected int LevelWidth( int level )
		{
			return ( 1 << ( level + 1 ) ) + 1;
		}

		#endregion Methods

		#region Properties

		/// <summary>
		///     Based on a previous call to 
        ///     <see cref="DefineSurface(Array, VertexDeclaration, int, int)"/>, 
        ///     establishes the number of vertices required
		///     to hold this patch at the maximum detail level.
		/// </summary>
		/// <remarks>
		///     This is useful when you wish to build the patch into external vertex / index buffers.
		/// </remarks>
		public int RequiredVertexCount
		{
			get
			{
				return requiredVertexCount;
			}
		}

		/// <summary>
		///     Based on a previous call to 
        ///     <see cref="DefineSurface(Array, VertexDeclaration, int, int)"/>, 
        ///     establishes the number of indexes required
		///     to hold this patch at the maximum detail level.
		/// </summary>
		public int RequiredIndexCount
		{
			get
			{
				return requiredIndexCount;
			}
		}

		/// <summary>
		///     Gets the current index count based on the current subdivision level.
		/// </summary>
		public int CurrentIndexCount
		{
			get
			{
				return currentIndexCount;
			}
		}

		/// <summary>
		///     Returns the index offset used by this buffer to write data into the buffer.
		/// </summary>
		public int IndexOffset
		{
			get
			{
				return indexOffset;
			}
		}

		/// <summary>
		///     Returns the vertex offset used by this buffer to write data into the buffer.
		/// </summary>
		public int VertexOffset
		{
			get
			{
				return vertexOffset;
			}
		}

		/// <summary>
        ///     Gets the bounds of this patch, only valid after calling 
        ///     <see cref="DefineSurface(Array, VertexDeclaration, int, int)"/>.
		/// </summary>
		public AxisAlignedBox Bounds
		{
			get
			{
				return aabb;
			}
		}

		/// <summary>
        ///     Gets the radius of the bounding sphere for this patch, only valid after 
        ///     <see cref="DefineSurface(Array, VertexDeclaration, int, int)"/>
		///     has been called.
		/// </summary>
		public float BoundingSphereRadius
		{
			get
			{
				return boundingSphereRadius;
			}
		}

		/// <summary>
		///     Gets/Sets the level of subdivision for this surface.
		/// </summary>
		/// <remarks>
		///     This method changes the proportionate detail level of the patch; since
		///     the U and V directions can have different subdivision levels, this property
		///     takes a single float value where 0 is the minimum detail (the control points)
		///     and 1 is the maximum detail level as supplied to the original call to
        ///     <see cref="DefineSurface(Array, VertexDeclaration, int, int)"/>.
		/// </remarks>
		public float SubdivisionFactor
		{
			get
			{
				return subdivisionFactor;
			}
			set
			{
				Debug.Assert( value >= 0.0f && value <= 1.0f );

				subdivisionFactor = value;

				uLevel = (int)( subdivisionFactor * maxULevel );
				vLevel = (int)( subdivisionFactor * maxVLevel );

				MakeTriangles();
			}
		}

		///// <summary>
		/////     Gets the control point buffer being used for this patch surface.
		/////     Contains pinned data, the pointer will become invalid once this class is garbage collected.
		/////     Use with care.
		///// </summary>
		//public IntPtr ControlPointBuffer
		//{
		//    get
		//    {
		//        return controlPointBuffer;
		//    }
		//}

		#endregion Properties
	}

	/// <summary>
	///
	/// </summary>
	public enum VisibleSide
	{
		/// <summary>
		///     The side from which u goes right and v goes up (as in texture coords).
		/// </summary>
		Front,
		/// <summary>
		///     The side from which u goes right and v goes down (reverse of texture coords).
		/// </summary>
		Back,
		/// <summary>
		///     Both sides are visible - warning this creates 2x the number of triangles and adds
		///     extra overhead for calculating normals.
		/// </summary>
		Both
	}

	/// <summary>
	///     A patch defined by a set of bezier curves.
	/// </summary>
	public enum PatchSurfaceType
	{
		Bezier
	}
}