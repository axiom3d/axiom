#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2007 Axiom Project Team

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
//     <id value="$Id: ManualObject.cs 1085 2007-08-13 20:37:24Z jprice $"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using Axiom.Math;
using Axiom.Graphics;

#endregion

namespace Axiom.Core
{
	/// <summary>
	///    Class providing a much simplified interface to generating manual
	///    objects with custom geometry.
	///
	///    Building one-off geometry objects manually usually requires getting
	///    down and dirty with the vertex buffer and vertex declaration API, 
	///    which some people find a steep learning curve. This class gives you 
	///    a simpler interface specifically for the purpose of building a 
	///    3D object simply and quickly. Note that if you intend to instance your
	///    object you will still need to become familiar with the Mesh class. 
	///
	///    This class draws heavily on the interface for OpenGL 
	///    immediate-mode (glBegin, glVertex, glNormal etc), since this
	///    is generally well-liked by people. There are a couple of differences
	///    in the results though - internally this class still builds hardware 
	///    buffers which can be re-used, so you can render the resulting object
	///    multiple times without re-issuing all the same commands again. 
	///    Secondly, the rendering is not immediate, it is still queued just like
	///    all OGRE/Axiom objects. This makes this object more efficient than the 
	///    equivalent GL immediate-mode commands, so it's feasible to use it for
	///    large objects if you really want to.
	///
	///    To construct some geometry with this object:
	///      -# If you know roughly how many vertices (and indices, if you use them)
	///         you're going to submit, call estimateVertexCount and estimateIndexCount.
	///         This is not essential but will make the process more efficient by saving
	///         memory reallocations.
	///      -# Call begin() to begin entering data
	///      -# For each vertex, call position(), normal(), textureCoord(), colour()
	///         to define your vertex data. Note that each time you call position()
	///         you start a new vertex. Note that the first vertex defines the 
	///         components of the vertex - you can't add more after that. For example
	///         if you didn't call normal() in the first vertex, you cannot call it
	///         in any others. You ought to call the same combination of methods per
	///         vertex.
	///      -# If you want to define triangles (or lines/points) by indexing into the vertex list, 
	///         you can call index() as many times as you need to define them.
	///         If you don't do this, the class will assume you want triangles drawn
	///         directly as defined by the vertex list, ie non-indexed geometry. Note
	///         that stencil shadows are only supported on indexed geometry, and that
	///         indexed geometry is a little faster; so you should try to use it.
	///      -# Call end() to finish entering data.
	///      -# Optionally repeat the begin-end cycle if you want more geometry 
	///        using different rendering operation types, or different materials
	///    After calling end(), the class will organise the data for that section
	///    internally and make it ready to render with. Like any other 
	///    MovableObject you should attach the object to a SceneNode to make it 
	///    visible. Other aspects like the relative render order can be controlled
	///    using standard MovableObject methods like SetRenderQueueGroup.
	///
	///    You can also use beginUpdate() to alter the geometry later on if you wish.
	///    If you do this, you should call setDynamic(true) before your first call 
	///    to begin(), and also consider using estimateVertexCount / estimateIndexCount
	///    if your geometry is going to be growing, to avoid buffer recreation during
	///    growth.
	///
	///    Note that like all OGRE/Axiom geometry, triangles should be specified in 
	///    anti-clockwise winding order (whether you're doing it with just
	///    vertices, or using indexes too). That is to say that the front of the
	///    face is the one where the vertices are listed in anti-clockwise order.
	/// </summary>
	public class ManualObject : MovableObject
	{
		const int TEMP_INITIAL_SIZE = 50;
		const int TEMP_VERTEXSIZE_GUESS = sizeof( float ) * 12;
		const int TEMP_INITIAL_VERTEX_SIZE = TEMP_VERTEXSIZE_GUESS * TEMP_INITIAL_SIZE;
		const int TEMP_INITIAL_INDEX_SIZE = sizeof( UInt16 ) * TEMP_INITIAL_SIZE;

		public ManualObject( string name )
		{
			base.Name = name;
			_dynamic = false;
			_currentSection = null;
			_firstVertex = true;
			_tempVertexPending = false;
			_tempVertexBuffer = null;
			_tempVertexSize = TEMP_INITIAL_VERTEX_SIZE;
			_tempIndexBuffer = null;
			_tempIndexSize = TEMP_INITIAL_INDEX_SIZE;
			_declSize = 0;
			_estVertexCount = 0;
			_estIndexCount = 0;
			_texCoordIndex = 0;
			_radius = 0;
			_anyIndexed = false;
			_edgeList = null;
			_useIdentityProjection = false;
			_useIdentityView = false;

		}

		///<summary>
		///Clearing the contents of this object and rebuilding from scratch
		///is not the optimal way to manage dynamic vertex data, since the 
		///buffers are recreated. If you want to keep the same structure but
		///update the content within that structure, use beginUpdate() instead 
		///of clear() begin(). However if you do want to modify the structure 
		///from time to time you can do so by clearing and re-specifying the data.
		///</summary>
		public virtual void Clear()
		{
			ResetTempAreas();
			_sectionList.Clear();
			_radius = 0;
			_AABB = null;
			_edgeList = null;
			_anyIndexed = false;
			_shadowRenderables.Clear();
		}

		/// <summary>
		/// Delete temp buffers and reset init counts
		/// </summary>
		protected virtual void ResetTempAreas()
		{
			_tempVertexBuffer = null;
			_tempIndexBuffer = null;
			_tempVertexSize = TEMP_INITIAL_VERTEX_SIZE;
			_tempIndexSize = TEMP_INITIAL_INDEX_SIZE;
		}

		/// <summary>
		/// Resize the temp vertex buffer
		/// </summary>
		/// <param name="numVerts">Number of vertices</param>
		public void ResizeTempVertexBufferIfNeeded( int numVerts )
		{
			// Calculate byte size
			// Use decl if we know it by now, otherwise default size to pos/norm/texcoord*2
			int newSize;
			if ( !_firstVertex )
			{
				newSize = _declSize * numVerts;
			}
			else
			{
				// estimate - size checks will deal for subsequent verts
				newSize = TEMP_VERTEXSIZE_GUESS * numVerts;
			}
			if ( newSize > _tempVertexSize || _tempVertexBuffer == null )
			{
				if ( _tempVertexBuffer == null )
				{
					// init
					newSize = _tempVertexSize;
				}
				else
				{
					// increase to at least double current
					newSize = (int)Math.Utility.Max( (float)newSize, (float)_tempVertexSize * 2.0f );
				}
				// copy old data
				byte[] tmp = _tempVertexBuffer;
				_tempVertexBuffer = new byte[ newSize ];
				if ( tmp != null )
				{
					tmp.CopyTo( _tempVertexBuffer, 0 );
					tmp = null;
				}
				_tempVertexSize = newSize;
			}
		}

		/// <summary>
		/// Resize the index buffer
		/// </summary>
		/// <param name="numInds">Number of indices</param>
		protected virtual void ResizeTempIndexBufferIfNeeded( int numInds )
		{
			int newSize = numInds * sizeof( UInt16 );
			if ( newSize > _tempIndexSize || _tempIndexBuffer == null )
			{
				if ( _tempIndexBuffer == null )
				{
					// init
					newSize = _tempIndexSize;
				}
				else
				{
					// increase to at least double current
					newSize = (int)Math.Utility.Max( (float)newSize, (float)_tempIndexSize * 2 );
				}
				numInds = newSize / sizeof( UInt16 );
				UInt16[] tmp = _tempIndexBuffer;
				_tempIndexBuffer = new UInt16[ numInds ];
				if ( tmp != null )
				{
					tmp.CopyTo( _tempIndexBuffer, 0 );
					tmp = null;
				}
				_tempIndexSize = newSize;
			}
		}

		///<summary>
		///   Calling this helps to avoid memory floatlocation when you define
		///   vertices. 
		///</summary>
		public virtual void EstimateVertexCount( int vcount )
		{
			ResizeTempVertexBufferIfNeeded( vcount );
			_estVertexCount = vcount;
		}

		///<summary>
		///  Calling this helps to avoid memory floatlocation when you define
		///  indices. 
		///</summary>
		public virtual void EstimateIndexCount( int icount )
		{
			ResizeTempIndexBufferIfNeeded( icount );
			_estIndexCount = icount;
		}

		///<summary>
		///  Each time you call this method, you start a new section of the
		///  object with its own material and potentially its own type of
		///  rendering operation (triangles, points or lines for example).
		///</summary>
		///<param name="materialName">The name of the material to render this part of the object with.</param>
		///<param name="opType">The type of operation to use to render.</param>
		public virtual void Begin( string materialName, OperationType opType )
		{
			if ( _currentSection != null )
			{
				throw new AxiomException( "ManualObject:Begin - You cannot call begin() again until after you call end()" );
			}

			_currentSection = new ManualObjectSection( this, materialName, opType );
			_currentUpdating = false;
			_currentSection.UseIdentityProjection = _useIdentityProjection;
			_currentSection.UseIdentityView = _useIdentityView;
			_sectionList.Add( _currentSection );
			_firstVertex = true;
			_declSize = 0;
			_texCoordIndex = 0;
		}

		///<summary>
		/// Using this method, you can update an existing section of the object
		/// efficiently. You do not have the option of changing the operation type
		/// obviously, since it must match the one that was used before. 
		/// </summary>
		/// <remarks>
		/// If your sections are changing size, particularly growing, use
		///	estimateVertexCount and estimateIndexCount to pre-size the buffers a little
		///	larger than the initial needs to avoid buffer reconstruction.
		/// </remarks>
		/// <param name="sectionIndex">The index of the section you want to update. The first
		///	call to begin() would have created section 0, the second section 1, etc.
		///	</param>
		public void BeginUpdate( int sectionIndex )
		{
			if ( _currentSection != null )
			{
				throw new AxiomException( "ManualObject.BeginUpdate - You cannot call begin() again until after you call end()" );
			}

			if ( sectionIndex >= _sectionList.Count )
			{
				throw new AxiomException( "ManualObject.BeginUpdate - Invalid section index - out of range." );
			}
			_currentSection = _sectionList[ sectionIndex ];
			_currentUpdating = true;
			_firstVertex = true;
			_texCoordIndex = 0;
			// reset vertex & index count
			RenderOperation rop = _currentSection.RenderOperation;
			rop.vertexData.vertexCount = 0;
			if ( rop.indexData != null )
				rop.indexData.indexCount = 0;
			rop.useIndices = false;
			_declSize = rop.vertexData.vertexDeclaration.GetVertexSize( 0 );
		}

		///<summary>
		/// A vertex position is slightly special among the other vertex data
		/// methods like normal() and textureCoord(), since calling it indicates
		/// the start of a new vertex. All other vertex data methods you call 
		/// after this are assumed to be adding more information (like normals or
		/// texture coordinates) to the last vertex started with position().
		/// </summary>
		/// <param name="pos">Position as a Vector3</param>
		public virtual void Position( Vector3 pos )
		{
			Position( pos.x, pos.y, pos.z );
		}

		///<summary>Vertex Position</summary>
		///<param name="x">x value of position as a float</param>
		///<param name="y">y value of position as a float</param>
		///<param name="z">z value of position as a float</param>
		public virtual void Position( float x, float y, float z )
		{

			if ( _currentSection == null )
			{
				throw new AxiomException( "ManualObject.Position - You must call begin() before this method" );
			}

			if ( _tempVertexPending )
			{
				// bake current vertex
				CopyTempVertexToBuffer();
				_firstVertex = false;
			}

			if ( _firstVertex && !_currentUpdating )
			{
				// defining declaration
				_currentSection.RenderOperation.vertexData.vertexDeclaration.AddElement( 0, _declSize, VertexElementType.Float3, VertexElementSemantic.Position );
				_declSize += VertexElement.GetTypeSize( VertexElementType.Float3 );
			}

			_tempVertex.position.x = x;
			_tempVertex.position.y = y;
			_tempVertex.position.z = z;

			// update bounds
			_AABB.Merge( _tempVertex.position );
			_radius = Math.Utility.Max( _radius, _tempVertex.position.Length );

			// reset current texture coord
			_texCoordIndex = 0;

			_tempVertexPending = true;
		}

		///<summary>
		/// Vertex normals are most often used for dynamic lighting, and 
		/// their components should be normalised.
		/// </summary>
		/// <param name="norm">Normal as Vector3</param>
		public virtual void Normal( Vector3 norm )
		{
			Normal( norm.x, norm.y, norm.z );
		}

		/// <summary>
		/// Normal value
		/// </summary>
		/// <param name="x">x value of vector as float</param>
		/// <param name="y">y value of vector as float</param>
		/// <param name="z">z value of vector as float</param>
		public virtual void Normal( float x, float y, float z )
		{
			if ( _currentSection == null )
			{
				throw new AxiomException( "ManualObject.Normal - You must call begin() before this method" );
			}

			if ( _firstVertex && !_currentUpdating )
			{
				// defining declaration
				_currentSection.RenderOperation.vertexData.vertexDeclaration.AddElement( 0, _declSize, VertexElementType.Float3, VertexElementSemantic.Normal );

				_declSize += VertexElement.GetTypeSize( VertexElementType.Float3 );
			}

			_tempVertex.normal.x = x;
			_tempVertex.normal.y = y;
			_tempVertex.normal.z = z;

		}

		///<summary>
		/// You can call this method multiple times between position() calls
		/// to add multiple texture coordinates to a vertex. Each one can have
		/// between 1 and 3 dimensions, depending on your needs, although 2 is
		/// most common. There are several versions of this method for the 
		/// variations in number of dimensions.
		///</summary>
		///<param name="u">u coordinate as float</param>
		public virtual void TextureCoord( float u )
		{
			if ( _currentSection == null )
			{
				throw new AxiomException( "ManualObject.TextureCoord - You must call begin() before this method" );
			}

			if ( _firstVertex && !_currentUpdating )
			{
				// defining declaration
				_currentSection.RenderOperation.vertexData.vertexDeclaration.AddElement( 0, _declSize, VertexElementType.Float1, VertexElementSemantic.TexCoords, _texCoordIndex );
				_declSize += VertexElement.GetTypeSize( VertexElementType.Float1 );
			}

			_tempVertex.texCoordDims[ _texCoordIndex ] = 1;
			_tempVertex.texCoord[ _texCoordIndex ].x = u;

			++_texCoordIndex;

		}

		/// <summary>
		/// Texture coordinate
		/// </summary>
		/// <param name="u">u coordinate as float</param>
		/// <param name="v">v coordinate as float</param>
		public virtual void TextureCoord( float u, float v )
		{
			if ( _currentSection == null )
			{
				throw new AxiomException( "ManualObject.TextureCoord - You must call begin() before this method" );
			}

			if ( _firstVertex && !_currentUpdating )
			{
				// defining declaration
				_currentSection.RenderOperation.vertexData.vertexDeclaration.AddElement( 0, _declSize, VertexElementType.Float2, VertexElementSemantic.TexCoords, _texCoordIndex );
				_declSize += VertexElement.GetTypeSize( VertexElementType.Float2 );
			}

			_tempVertex.texCoordDims[ _texCoordIndex ] = 2;
			_tempVertex.texCoord[ _texCoordIndex ].x = u;
			_tempVertex.texCoord[ _texCoordIndex ].y = v;

			++_texCoordIndex;

		}

		/// <summary>
		/// Texture Coordinate
		/// </summary>
		/// <param name="u">u coordinate as float</param>
		/// <param name="v">v coordinate as float</param>
		/// <param name="w">w coordinate as float</param>
		public virtual void TextureCoord( float u, float v, float w )
		{
			if ( _currentSection == null )
			{
				throw new AxiomException( "ManualObject.TextureCoord - You must call begin() before this method" );
			}

			if ( _firstVertex && !_currentUpdating )
			{
				// defining declaration
				_currentSection.RenderOperation.vertexData.vertexDeclaration.AddElement( 0, _declSize, VertexElementType.Float3, VertexElementSemantic.TexCoords, _texCoordIndex );
				_declSize += VertexElement.GetTypeSize( VertexElementType.Float3 );
			}

			_tempVertex.texCoordDims[ _texCoordIndex ] = 3;
			_tempVertex.texCoord[ _texCoordIndex ].x = u;
			_tempVertex.texCoord[ _texCoordIndex ].y = v;
			_tempVertex.texCoord[ _texCoordIndex ].z = w;

			++_texCoordIndex;

		}

		/// <summary>
		/// Texture coordinate
		/// </summary>
		/// <param name="uv">uv coordinate as Vector2</param>
		public virtual void TextureCoord( Vector2 uv )
		{
			TextureCoord( uv.x, uv.y );
		}

		/// <summary>
		/// Texture Coordinate
		/// </summary>
		/// <param name="uvw">uvw coordinate as Vector3</param>
		public virtual void TextureCoord( Vector3 uvw )
		{
			TextureCoord( uvw.x, uvw.y, uvw.z );
		}

		/// <summary>Add a vertex color to a vertex</summary>
		/// <param name="col">col as ColorEx object</param>
		public virtual void Color( ColorEx col )
		{
			Color( col.r, col.g, col.b, col.a );
		}

		///<summary>Add a vertex color to a vertex</summary>
		///<param name="r">r color component as float</param>
		///<param name="g">g color component as float</param>
		///<param name="b">b color component as float</param>
		///<param name="a">a color component as float</param>
		public virtual void Color( float r, float g, float b, float a )
		{
			if ( _currentSection == null )
			{
				throw new AxiomException( "ManualObject.Color - You must call begin() before this method" );
			}

			if ( _firstVertex && !_currentUpdating )
			{
				// defining declaration
				_currentSection.RenderOperation.vertexData.vertexDeclaration.AddElement( 0, _declSize, VertexElementType.Color, VertexElementSemantic.Diffuse );
				_declSize += VertexElement.GetTypeSize( VertexElementType.Color );
			}

			_tempVertex.color.r = r;
			_tempVertex.color.g = g;
			_tempVertex.color.b = b;
			_tempVertex.color.a = a;

		}

		///<summary>
		///Add a vertex index to construct faces / lines / points via indexing
		/// rather than just by a simple list of vertices.
		/// <remarks>
		/// You will have to call this 3 times for each face for a triangle list, 
		/// or use the alternative 3-parameter version. Other operation types
		/// require different numbers of indexes, @see RenderOperation::OperationType.
		/// 32-bit indexes are not supported on all cards which is why this 
		/// class only allows 16-bit indexes, for simplicity and ease of use.
		/// </remarks>
		/// <param name="idx">A vertex index from 0 to 65535.</param>
		public virtual void Index( UInt16 idx )
		{
			if ( _currentSection == null )
			{
				throw new AxiomException( "ManualObject.Index - You must call begin() before this method" );
			}

			_anyIndexed = true;
			// make sure we have index data
			RenderOperation rop = _currentSection.RenderOperation;
			if ( rop.indexData == null )
			{
				rop.indexData = new IndexData();
				rop.indexData.indexCount = 0;
			}

			rop.useIndices = true;
			ResizeTempIndexBufferIfNeeded( ++rop.indexData.indexCount );

			_tempIndexBuffer[ rop.indexData.indexCount - 1 ] = idx;

		}

		/** 
		@note
			32-bit indexes are not supported on all cards which is why this 
			class only allows 16-bit indexes, for simplicity and ease of use.
		@param i1, i2, i3 3 vertex indices from 0 to 65535 defining a face. 
		*/

		///<summary>
		/// Add a set of 3 vertex indices to construct a triangle; this is a
		/// shortcut to calling index() 3 times. It is only valid for triangle 
		/// lists.
		///</summary>
		public virtual void Triangle( UInt16 i1, UInt16 i2, UInt16 i3 )
		{
			if ( _currentSection == null )
			{
				throw new AxiomException( "ManualObject.Triangle - You must call begin() before this method" );
			}

			if ( _currentSection.RenderOperation.operationType != OperationType.TriangleList )
			{
				throw new AxiomException( "ManualObject.Triangle - This method is only valid on triangle lists" );
			}

			Index( i1 );
			Index( i2 );
			Index( i3 );

		}

		///<summary>
		/// Add a set of 4 vertex indices to construct a quad (out of 2 
		/// triangles); this is a shortcut to calling index() 6 times, 
		/// or triangle() twice. It's only valid for triangle list operations.
		///</summary>
		///<param name="i1">vertex index from 0 to 65535 defining a face</param>
		///<param name="i2">vertex index from 0 to 65535 defining a face</param>
		///<param name="i3">vertex index from 0 to 65535 defining a face</param>
		///<param name="i4">vertex index from 0 to 65535 defining a face</param>
		public virtual void Quad( UInt16 i1, UInt16 i2, UInt16 i3, UInt16 i4 )
		{
			// first tri
			Triangle( i1, i2, i3 );
			// second tri
			Triangle( i3, i4, i1 );
		}

		/// <summary>
		/// Copies temporary vertex buffer to hardware buffer
		/// </summary>
		protected virtual void CopyTempVertexToBuffer()
		{
			_tempVertexPending = false;
			RenderOperation rop = _currentSection.RenderOperation;

			if ( rop.vertexData.vertexCount == 0 && !_currentUpdating )
			{
				// first vertex, autoorganise decl
				VertexDeclaration oldDcl = rop.vertexData.vertexDeclaration;
				rop.vertexData.vertexDeclaration =
					oldDcl.GetAutoOrganizedDeclaration( false, false );

				HardwareBufferManager.Instance.DestroyVertexDeclaration( oldDcl );
			}

			ResizeTempVertexBufferIfNeeded( ++rop.vertexData.vertexCount );

			List<VertexElement> elemList = rop.vertexData.vertexDeclaration.Elements;

			unsafe
			{
				// get base pointer
				fixed ( byte* pBase = &_tempVertexBuffer[ _declSize * ( rop.vertexData.vertexCount - 1 ) ] )
				{
					foreach ( VertexElement elem in elemList )
					{
						float* pFloat = null;
						UInt32* pRGBA = null;

						switch ( elem.Type )
						{
							case VertexElementType.Float1:
							case VertexElementType.Float2:
							case VertexElementType.Float3:
								pFloat = (float*)( (byte*)pBase + elem.Offset );
								break;

							case VertexElementType.Color:
							case VertexElementType.Color_ABGR:
							case VertexElementType.Color_ARGB:
								pRGBA = (uint*)( (byte*)pBase + elem.Offset );
								break;
							default:
								// nop ?
								break;
						}

						RenderSystem rs;
						int dims;
						switch ( elem.Semantic )
						{
							case VertexElementSemantic.Position:
								*pFloat++ = _tempVertex.position.x;
								*pFloat++ = _tempVertex.position.y;
								*pFloat++ = _tempVertex.position.z;
								break;
							case VertexElementSemantic.Normal:
								*pFloat++ = _tempVertex.normal.x;
								*pFloat++ = _tempVertex.normal.y;
								*pFloat++ = _tempVertex.normal.z;
								break;
							case VertexElementSemantic.TexCoords:
								dims = VertexElement.GetTypeCount( elem.Type );
								for ( int t = 0; t < dims; ++t )
									*pFloat++ = _tempVertex.texCoord[ elem.Index ][ t ];
								break;
							case VertexElementSemantic.Diffuse:
								rs = Root.Instance.RenderSystem;
								if ( rs != null )
									*pRGBA++ = (uint)rs.ConvertColor( _tempVertex.color );
								else
									*pRGBA++ = (uint)_tempVertex.color.ToRGBA(); // pick one!
								break;
							default:
								// nop ?
								break;
						}
					}

				}
			}

		}

		///<summary>
		/// Finish defining the object and compile the final renderable version.
		///</summary>
		public virtual ManualObjectSection End()
		{
			if ( _currentSection == null )
			{
				throw new AxiomException( "ManualObject.End - You cannot call end() until after you call begin()" );
			}

			if ( _tempVertexPending )
			{
				// bake current vertex
				CopyTempVertexToBuffer();
			}

			// pointer that will be returned
			ManualObjectSection result = null;

			RenderOperation rop = _currentSection.RenderOperation;

			// Check for empty content
			if ( rop.vertexData.vertexCount == 0 || ( rop.useIndices && rop.indexData.indexCount == 0 ) )
			{
				// You're wasting my time sonny
				if ( _currentUpdating )
				{
					// Can't just undo / remove since may be in the middle
					// Just allow counts to be 0, will not be issued to renderer

					// return the finished section (though it has zero vertices)
					result = _currentSection;
				}
				else
				{
					// First creation, can really undo
					// Has already been added to section list end, so remove
					if ( _sectionList.Count > 0 )
						_sectionList.RemoveAt( _sectionList.Count - 1 );
				}
			}
			else // not an empty section
			{
				// Bake the real buffers
				HardwareVertexBuffer vbuf = null;
				// Check buffer sizes
				bool vbufNeedsCreating = true;
				bool ibufNeedsCreating = rop.useIndices;

				if ( _currentUpdating )
				{
					// May be able to reuse buffers, check sizes
					vbuf = rop.vertexData.vertexBufferBinding.GetBuffer( 0 );
					if ( vbuf.VertexCount >= rop.vertexData.vertexCount )
						vbufNeedsCreating = false;

					if ( rop.useIndices )
					{
						if ( rop.indexData.indexBuffer.IndexCount >= rop.indexData.indexCount )
							ibufNeedsCreating = false;
					}

				}

				if ( vbufNeedsCreating )
				{
					// Make the vertex buffer larger if estimated vertex count higher
					// to allow for user-configured growth area
					int vertexCount = (int)Math.Utility.Max( rop.vertexData.vertexCount, _estVertexCount );

					vbuf = HardwareBufferManager.Instance.CreateVertexBuffer(
							_declSize,
							vertexCount,
							_dynamic ? BufferUsage.DynamicWriteOnly :
								BufferUsage.StaticWriteOnly );

					rop.vertexData.vertexBufferBinding.SetBinding( 0, vbuf );
				}

				if ( ibufNeedsCreating )
				{
					// Make the index buffer larger if estimated index count higher
					// to allow for user-configured growth area
					int indexCount = (int)Utility.Max( rop.indexData.indexCount, _estIndexCount );
					rop.indexData.indexBuffer =
						HardwareBufferManager.Instance.CreateIndexBuffer(
							IndexType.Size16, indexCount, _dynamic ?
							BufferUsage.DynamicWriteOnly : BufferUsage.StaticWriteOnly );
				}

				// Write vertex data
				if ( vbuf != null )
					vbuf.WriteData( 0, rop.vertexData.vertexCount * vbuf.VertexSize, _tempVertexBuffer, true );

				// Write index data
				if ( rop.useIndices )
				{
					rop.indexData.indexBuffer.WriteData( 0, rop.indexData.indexCount * rop.indexData.indexBuffer.IndexSize,
						_tempIndexBuffer, true );
				}

				// return the finished section
				result = _currentSection;

			} // empty section check

			_currentSection = null;
			ResetTempAreas();

			// Tell parent if present
			if ( ParentNode != null )
			{
				ParentNode.NeedUpdate();
			}

			// will return the finished section or NULL if
			// the section was empty (i.e. zero vertices/indices)
			return result;

		}

		/// <summary>
		/// Alter the material for a subsection of this object after it has been
		///	specified.
		///	You specify the material to use on a section of this object during the
		///	call to begin(), however if you want to change the material afterwards
		///	you can do so by calling this method.
		/// </summary>
		/// <param name="idx">The index of the subsection to alter</param>
		/// <param name="name">The name of the new material to use</param>
		public void SetMaterialName( int idx, string name )
		{
			if ( idx >= _sectionList.Count )
			{
				throw new AxiomException( "ManualObject.SetMaterialName - Index out of bounds!" );
			}

			_sectionList[ idx ].MaterialName = name;

		}

		///<summary>
		/// After you've finished building this object, you may convert it to 
		/// a Mesh if you want in order to be able to create many instances of
		/// it in the world (via Entity). This is optional, since this instance
		/// can be directly attached to a SceneNode itself, but of course only
		/// one instance of it can exist that way. 
		///</summary>
		///<remarks>Only objects which use indexed geometry may be converted to a mesh.</remarks>
		///<param name="meshName">The name to give the mesh</param>
		///<param name="groupName">The resource group to create the mesh in</param>

		public virtual Mesh ConvertToMesh( string meshName, string groupName )
		{
			if ( _currentSection != null )
			{
				throw new AxiomException( "ManualObject.ConvertToMesh - You cannot call convertToMesh() whilst you are in the middle of defining the object; call end() first." );
			}

			if ( _sectionList.Count == 0 )
			{
				throw new AxiomException( "ManualObject.ConvertToMesh - No data defined to convert to a mesh." );
			}

			foreach ( ManualObjectSection sec in _sectionList )
			{
				if ( !sec.RenderOperation.useIndices )
				{
					throw new AxiomException( "ManualObject.ConvertToMesh - Only indexed geometry may be converted to a mesh." );
				}
			}

			Mesh m = MeshManager.Instance.CreateManual( meshName, groupName, null );

			foreach ( ManualObjectSection sec in _sectionList )
			{
				RenderOperation rop = sec.RenderOperation;
				SubMesh sm = m.CreateSubMesh();
				sm.useSharedVertices = false;
				sm.operationType = rop.operationType;
				sm.MaterialName = sec.MaterialName;
				// Copy vertex data; replicate buffers too
				sm.vertexData = rop.vertexData.Clone( true );
				// Copy index data; replicate buffers too
				sm.indexData = rop.indexData.Clone( true );
			}
			// update bounds
			m.BoundingBox = _AABB;
			m.BoundingSphereRadius = _radius;

			m.Load();

			return m;

		}

		/// <summary>
		/// Usually ManualObjects will use a projection matrix as determined
		///	by the active camera. However, if they want they can cancel this out
		///	and use an identity projection, which effectively projects in 2D using
		///	a {-1, 1} view space. Useful for overlay rendering. Normally you don't
		//	need to change this. The default is false.
		/// </summary>
		public bool UseIdentityProjection
		{
			get
			{
				return _useIdentityProjection;
			}
			set
			{

				// Set existing
				foreach ( ManualObjectSection sec in _sectionList )
				{
					sec.UseIdentityProjection = value;
				}

				// Save setting for future sections
				_useIdentityProjection = value;
			}
		}

		/// <summary>
		/// Usually ManualObjects will use a view matrix as determined
		///	by the active camera. However, if they want they can cancel this out
		///	and use an identity matrix, which means all geometry is assumed
		///	to be relative to camera space already. Useful for overlay rendering. 
		///	Normally you don't need to change this. The default is false.
		/// </summary>
		public bool UseIdentityView
		{
			get
			{
				return _useIdentityView;
			}
			set
			{

				// Set existing
				foreach ( ManualObjectSection sec in _sectionList )
				{
					sec.UseIdentityView = value;
				}

				// Save setting for future sections
				_useIdentityView = value;
			}
		}

		/// <summary>
		/// Gets a reference to a ManualObjectSection, ie a part of a ManualObject.
		/// </summary>
		/// <param name="index">Index of section to get</param>
		/// <returns></returns>
		public ManualObjectSection GetSection( int index )
		{
			if ( index >= _sectionList.Count )
				throw new AxiomException( "ManualObject.GetSection - Index out of bounds." );

			return _sectionList[ index ];
		}

		/// <summary>
		/// Retrieves the number of ManualObjectSection objects making up this ManualObject.
		/// </summary>
		public int NumSections
		{
			get
			{
				return _sectionList.Count;
			}
		}

		// MovableObject overrides

		/// <summary>
		/// Movable type override
		/// </summary>
		public string MovableType
		{
			get
			{
				return "ManualObject";
			}
		}

		/// <summary>
		/// Get bounding box for this object
		/// </summary>
		public override AxisAlignedBox BoundingBox
		{
			get
			{
				return (AxisAlignedBox)_AABB.Clone();
			}
		}

		/// <summary>
		///    Local bounding radius of this object.
		/// </summary>
		public override float BoundingRadius
		{
			get
			{
				return _radius;
			}
		}

		/// <summary>
		///     Implemented to add ourself to the rendering queue.
		/// </summary>
		/// <param name="queue">Rendering queue to add this object</param>
		public override void UpdateRenderQueue( RenderQueue queue )
		{
			foreach ( ManualObjectSection sec in _sectionList )
			{
				// Skip empty sections (only happens if non-empty first, then updated)
				RenderOperation rop = sec.RenderOperation;
				if ( rop.vertexData.vertexCount == 0 ||
					( rop.useIndices && rop.indexData.indexCount == 0 ) )
					continue;

				if ( this.renderQueueIDSet )
					queue.AddRenderable( sec, this.renderQueueID );
				else
					queue.AddRenderable( sec );
			}

		}
		/// <summary>
		///		Implement this method to enable stencil shadows.
		/// </summary>
		public EdgeData GetEdgeList()
		{
			// Build on demand
			if ( _edgeList == null && _anyIndexed )
			{
				EdgeListBuilder eb = new EdgeListBuilder();
				int vertexSet = 0;
				bool anyBuilt = false;
				foreach ( ManualObjectSection sec in _sectionList )
				{
					RenderOperation rop = sec.RenderOperation;
					// Only indexed triangle geometry supported for stencil shadows
					if ( rop.useIndices && rop.indexData.indexCount != 0 &&
						( rop.operationType == OperationType.TriangleFan ||
						 rop.operationType == OperationType.TriangleList ||
						 rop.operationType == OperationType.TriangleStrip ) )
					{
						eb.AddVertexData( rop.vertexData );
						eb.AddIndexData( rop.indexData, vertexSet++ );
						anyBuilt = true;
					}
				}

				if ( anyBuilt )
					_edgeList = eb.Build();

			}

			return _edgeList;

		}

		/// <summary>
		/// Does the edge list exist?
		/// </summary>
		/// <returns>true if list exists</returns>
		public bool HasEdgeList()
		{
			return GetEdgeList() != null;
		}

		/// <summary>
		/// Implement this method to enable stencil shadows.
		/// </summary>
		/// <param name="technique">Render technique</param>
		/// <param name="light">Light source</param>
		/// <param name="indexBuffer">Index buffer</param>
		/// <param name="extrudeVertices">Extrude (true or false)</param>
		/// <param name="extrusionDistance">Extrusion distance</param>
		/// <param name="flags">Flag parameters</param>
		/// <returns></returns>
		public override System.Collections.IEnumerator GetShadowVolumeRenderableEnumerator( ShadowTechnique technique, Light light,
			HardwareIndexBuffer indexBuffer, bool extrudeVertices, float extrusionDistance, int flags )
		{
			Debug.Assert( indexBuffer != null, "Only external index buffers are supported right now" );
			Debug.Assert( indexBuffer.Type == IndexType.Size16, "Only 16-bit indexes supported for now" );

			EdgeData edgeList = GetEdgeList();

			if ( edgeList == null )
			{
				return _shadowRenderables.GetEnumerator();
			}

			// Calculate the object space light details
			Vector4 lightPos = light.GetAs4DVector();
			Matrix4 world2Obj = this.ParentNode.FullTransform.Inverse();
			lightPos = world2Obj.TransformAffine( lightPos );

			// Init shadow renderable list if required (only allow indexed)
			bool init = ( _shadowRenderables.Count == 0 && _anyIndexed );

			ManualObjectSectionShadowRenderable esr = null;
			ManualObjectSection seci = null;

			if ( init )
			{
				_shadowRenderables.Capacity = edgeList.edgeGroups.Count;
			}

			EdgeData.EdgeGroup egi;

			for ( int i = 0; i < _shadowRenderables.Capacity; i++ )
			{
				// Skip non-indexed geometry
				egi = (EdgeData.EdgeGroup)edgeList.edgeGroups[ i ];
				seci = _sectionList[ i ];

				if ( seci.RenderOperation.useIndices )
				{
					continue;
				}

				if ( init )
				{
					// Create a new renderable, create a separate light cap if
					// we're using a vertex program (either for this model, or
					// for extruding the shadow volume) since otherwise we can
					// get depth-fighting on the light cap
					Material mat = seci.Material;
					mat.Load();
					bool vertexProgram = false;
					Technique t = mat.GetBestTechnique();
					for ( int p = 0; p < t.PassCount; ++p )
					{
						Pass pass = t.GetPass( p );
						if ( pass.HasVertexProgram )
						{
							vertexProgram = true;
							break;
						}
					}

					esr = new ManualObjectSectionShadowRenderable( this, indexBuffer, egi.vertexData, vertexProgram || !extrudeVertices, false );
					_shadowRenderables.Add( esr );
				}
				// Get shadow renderable
				esr = (ManualObjectSectionShadowRenderable)_shadowRenderables[ i ];

				// Extrude vertices in software if required
				if ( extrudeVertices )
				{
					ExtrudeVertices( esr.PositionBuffer, egi.vertexData.vertexCount, lightPos, extrusionDistance );
				}

			}

			// Calc triangle light facing
			UpdateEdgeListLightFacing( edgeList, lightPos );

			// Generate indexes and update renderables
			GenerateShadowVolume( edgeList, indexBuffer, light, _shadowRenderables, flags );

			return _shadowRenderables.GetEnumerator();
		}

		///<summary>
		/// Built, renderable section of geometry
		///</summary>
		public class ManualObjectSection : IRenderable
		{
			protected ManualObject _parent = null;
			protected string _materialName;
			Material _material = null;
			bool _useIdentityProjection = false;
			bool _useIdentityView = false;
			protected RenderOperation _renderOperation = new RenderOperation();
			protected Hashtable _customParams = new Hashtable( 20 );

			public ManualObjectSection( ManualObject parent, string materialName,
				OperationType opType )
			{
				_parent = parent;
				_materialName = materialName;
				_renderOperation.operationType = opType;
				// default to no indexes unless we're told
				_renderOperation.useIndices = false;
				_renderOperation.vertexData = new VertexData();
				_renderOperation.vertexData.vertexCount = 0;
			}

			/// Retrieve render operation for manipulation
			public RenderOperation RenderOperation
			{
				get
				{
					return _renderOperation;
				}
			}

			/// Retrieve the material name in use
			/// 
			public string MaterialName
			{
				get
				{
					return _materialName;
				}

				set
				{
					if ( _materialName != value )
					{
						_materialName = value;
						_material = null;
					}
				}
			}

			#region IRenderable Members

			public bool CastsShadows
			{
				get
				{
					return false;
				}
			}

			public Material Material
			{
				get
				{
					if ( _material == null )
					{
						// Load from default group. If user wants to use alternate groups,
						// they can define it and preload
						_material = (Material)MaterialManager.Instance.Load( _materialName, ResourceGroupManager.DefaultResourceGroupName );
					}

					return _material;
				}
			}

			public Technique Technique
			{
				get
				{
					Material retMat = this.Material;
					if ( retMat != null )
						return retMat.GetBestTechnique();
					else
						throw new AxiomException( "ManualObject.Technique - couldn't get object material." );

					return null;
				}
			}

			public void GetRenderOperation( RenderOperation op )
			{
				op.useIndices = this._renderOperation.useIndices;
				op.operationType = this._renderOperation.operationType;
				op.vertexData = this._renderOperation.vertexData;
				op.indexData = this._renderOperation.indexData;
			}

			public void GetWorldTransforms( Matrix4[] matrices )
			{
				matrices[ 0 ] = _parent.ParentNode.FullTransform;
			}

			public Axiom.Collections.LightList Lights
			{
				get
				{
					return _parent.ParentNode.Lights;
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

			public bool UseIdentityProjection
			{
				get
				{
					return _useIdentityProjection;
				}
				set
				{
					_useIdentityProjection = value;
				}
			}

			public bool UseIdentityView
			{
				get
				{
					return _useIdentityView;
				}
				set
				{
					_useIdentityView = value;
				}
			}

            public virtual bool PolygonModeOverrideable
            {
                get
                {
                    return true;
                }
            }

			public Quaternion WorldOrientation
			{
				get
				{
					return _parent.ParentNode.DerivedOrientation;
				}
			}

			public Vector3 WorldPosition
			{
				get
				{
					return _parent.ParentNode.DerivedPosition;
				}
			}

			public float GetSquaredViewDepth( Camera camera )
			{
				if ( _parent.ParentNode != null )
				{
					return _parent.ParentNode.GetSquaredViewDepth( camera );
				}
				else
					return 0.0f;
			}

			public Vector4 GetCustomParameter( int index )
			{
				if ( _customParams[ index ] == null )
				{
					throw new Exception( "A parameter was not found at the given index" );
				}
				else
				{
					return (Vector4)_customParams[ index ];
				}
			}

			public void SetCustomParameter( int index, Vector4 val )
			{
				_customParams[ index ] = val;
			}

			public void UpdateCustomGpuParameter( GpuProgramParameters.AutoConstantEntry entry, GpuProgramParameters gpuParams )
			{
				if ( _customParams[ entry.data ] != null )
				{
					gpuParams.SetConstant( entry.index, (Vector4)_customParams[ entry.data ] );
				}
			}
			#endregion

		}

		/// <summary>
		/// Nested class to allow shadows.
		/// </summary>
		public class ManualObjectSectionShadowRenderable : ShadowRenderable
		{
			protected ManualObject _parent;
			// Shared link to position buffer
			protected HardwareVertexBuffer _positionBuffer;
			// Shared link to w-coord buffer (optional)
			protected HardwareVertexBuffer _wBuffer;

			public ManualObjectSectionShadowRenderable( ManualObject parent,
				HardwareIndexBuffer indexBuffer, VertexData vertexData,
				bool createSeparateLightCap, bool isLightCap )
			{
				_parent = parent;
				// Initialise render op
				renderOp.indexData = new IndexData();
				renderOp.indexData.indexBuffer = indexBuffer;
				renderOp.indexData.indexStart = 0;
				// index start and count are sorted out later

				// Create vertex data which just references position component (and 2 component)
				renderOp.vertexData = new VertexData();
				// Map in position data
				renderOp.vertexData.vertexDeclaration.AddElement( 0, 0, VertexElementType.Float3, VertexElementSemantic.Position );
				short origPosBind =
				vertexData.vertexDeclaration.FindElementBySemantic( VertexElementSemantic.Position ).Source;

				_positionBuffer = vertexData.vertexBufferBinding.GetBuffer( origPosBind );

				renderOp.vertexData.vertexBufferBinding.SetBinding( 0, _positionBuffer );
				// Map in w-coord buffer (if present)
				if ( vertexData.hardwareShadowVolWBuffer != null )
				{
					renderOp.vertexData.vertexDeclaration.AddElement( 1, 0, VertexElementType.Float1, VertexElementSemantic.TexCoords, 0 );
					_wBuffer = vertexData.hardwareShadowVolWBuffer;
					renderOp.vertexData.vertexBufferBinding.SetBinding( 1, _wBuffer );
				}

				// Use same vertex start as input
				renderOp.vertexData.vertexStart = vertexData.vertexStart;

				if ( isLightCap )
				{
					// Use original vertex count, no extrusion
					renderOp.vertexData.vertexCount = vertexData.vertexCount;
				}
				else
				{
					// Vertex count must take into account the doubling of the buffer,
					// because second half of the buffer is the extruded copy
					renderOp.vertexData.vertexCount = vertexData.vertexCount * 2;
					if ( createSeparateLightCap )
					{
						// Create child light cap
						this.lightCap = new ManualObjectSectionShadowRenderable( parent,
						indexBuffer, vertexData, false, true );
					}
				}
			}

			public HardwareVertexBuffer PositionBuffer
			{
				get
				{
					return _positionBuffer;
				}
			}

			public HardwareVertexBuffer WBuffer
			{
				get
				{
					return _wBuffer;
				}
			}

			public override void GetWorldTransforms( Matrix4[] matrices )
			{
				matrices[ 0 ] = _parent.ParentNode.FullTransform;
			}

			public override Quaternion WorldOrientation
			{
				get
				{
					return _parent.ParentNode.DerivedOrientation;
				}
			}

			public override Vector3 WorldPosition
			{
				get
				{
					return _parent.ParentNode.DerivedPosition;
				}
			}

		} // end class

		public class SectionList : List<ManualObjectSection>
		{
		}

		/// <summary>
		/// Use before defining geometry to indicate that you intend to update the
		///	geometry regularly and want the internal structure to reflect that.
		/// </summary>

		public bool Dynamic
		{
			get
			{
				return _dynamic;
			}
			set
			{
				_dynamic = value;
			}
		}

		public override void NotifyCurrentCamera( Camera camera )
		{
		}

		protected bool _dynamic;

		/// List of subsections
		protected SectionList _sectionList = new SectionList();

		/// Current section
		protected ManualObjectSection _currentSection;

		// Are we updating?
		protected bool _currentUpdating;

		/// Temporary vertex structure
		protected class TempVertex
		{
			public Vector3 position = new Vector3();
			public Vector3 normal = new Vector3();
			public Vector3[] texCoord = new Vector3[ Axiom.Configuration.Config.MaxTextureCoordSets ];
			public ushort[] texCoordDims = new ushort[ Axiom.Configuration.Config.MaxTextureCoordSets ];
			public ColorEx color = new ColorEx();
		} // end TempVertex struct


		/// Temp storage
		protected TempVertex _tempVertex = new TempVertex();
		/// First vertex indicator
		protected bool _firstVertex;
		/// Temp vertex data to copy?
		protected bool _tempVertexPending;
		/// System-memory buffer whilst we establish the size required
		protected byte[] _tempVertexBuffer;
		/// System memory allocation size, in bytes
		protected int _tempVertexSize;
		/// System-memory buffer whilst we establish the size required
		protected UInt16[] _tempIndexBuffer;
		/// System memory allocation size, in bytes
		protected int _tempIndexSize;
		/// Current declaration vertex size
		protected int _declSize;
		/// Estimated vertex count
		int _estVertexCount;
		/// Estimated index count
		int _estIndexCount;
		/// Current texture coordinate
		protected ushort _texCoordIndex;
		/// Bounding box
		protected AxisAlignedBox _AABB = new AxisAlignedBox();
		/// Bounding sphere
		protected float _radius;
		/// Any indexed geoemtry on any sections?
		protected bool _anyIndexed;
		/// Edge list, used if stencil shadow casting is enabled 
		protected EdgeData _edgeList;
		/// List of shadow renderables
		protected ShadowRenderableList _shadowRenderables;
		/// Whether to use identity projection for sections
		protected bool _useIdentityProjection;
		/// Whether to use identity view for sections
		protected bool _useIdentityView;


	}
}