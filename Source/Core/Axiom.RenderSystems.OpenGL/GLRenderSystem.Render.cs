using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Axiom.Configuration;
using Axiom.Core;
using Axiom.Graphics;
using Tao.OpenGl;

#pragma warning disable 612,618

namespace Axiom.RenderSystems.OpenGL
{
	public partial class GLRenderSystem
	{
		#region Render

		[OgreVersion( 1, 7, 2790 )]
		public override void Render( RenderOperation op )
		{
			// Call super class
			base.Render( op );

			var globalInstanceVertexBuffer = GlobalInstanceVertexBuffer;
			var globalVertexDeclaration = GlobalInstanceVertexBufferVertexDeclaration;
			var hasInstanceData = op.useGlobalInstancingVertexBufferIsAvailable && globalInstanceVertexBuffer != null &&
			                      globalVertexDeclaration != null || op.vertexData.vertexBufferBinding.HasInstanceData;

			var numberOfInstances = op.numberOfInstances;

			if ( op.useGlobalInstancingVertexBufferIsAvailable )
			{
				numberOfInstances *= GlobalNumberOfInstances;
			}

			var decl = op.vertexData.vertexDeclaration.Elements;
			var attribsBound = new List<int>();
			var instanceAttribsBound = new List<int>();
			var maxSource = 0;

			foreach ( var elem in decl )
			{
				var source = elem.Source;
				if ( maxSource < source )
				{
					maxSource = source;
				}

				if ( !op.vertexData.vertexBufferBinding.IsBufferBound( source ) )
				{
					continue; // skip unbound elements
				}

				var vertexBuffer = op.vertexData.vertexBufferBinding.GetBuffer( source );

				BindVertexElementToGpu( elem, vertexBuffer, op.vertexData.vertexStart, attribsBound, instanceAttribsBound );
			}

			if ( globalInstanceVertexBuffer != null && globalVertexDeclaration != null )
			{
				foreach ( var elem in globalVertexDeclaration.Elements )
				{
					BindVertexElementToGpu( elem, globalInstanceVertexBuffer, 0, attribsBound, instanceAttribsBound );
				}
			}

			var multitexturing = ( Capabilities.TextureUnitCount > 1 );
			if ( multitexturing )
			{
				Gl.glClientActiveTextureARB( Gl.GL_TEXTURE0 );
			}

			// Find the correct type to render
			int primType;
			//Use adjacency if there is a geometry program and it requested adjacency info
			var useAdjacency = ( geometryProgramBound && this.currentGeometryProgram != null &&
			                     this.currentGeometryProgram.IsAdjacencyInfoRequired );
			switch ( op.operationType )
			{
				case OperationType.PointList:
					primType = Gl.GL_POINTS;
					break;
				case OperationType.LineList:
					primType = useAdjacency ? Gl.GL_LINES_ADJACENCY_EXT : Gl.GL_LINES;
					break;
				case OperationType.LineStrip:
					primType = useAdjacency ? Gl.GL_LINE_STRIP_ADJACENCY_EXT : Gl.GL_LINE_STRIP;
					break;
				default:
					//case OperationType.TriangleList:
					primType = useAdjacency ? Gl.GL_TRIANGLES_ADJACENCY_EXT : Gl.GL_TRIANGLES;
					break;
				case OperationType.TriangleStrip:
					primType = useAdjacency ? Gl.GL_TRIANGLE_STRIP_ADJACENCY_EXT : Gl.GL_TRIANGLE_STRIP;
					break;
				case OperationType.TriangleFan:
					primType = Gl.GL_TRIANGLE_FAN;
					break;
			}

			if ( op.useIndices )
			{
				IntPtr pBufferData;

				if ( currentCapabilities.HasCapability( Graphics.Capabilities.VertexBuffer ) )
				{
					Gl.glBindBufferARB( Gl.GL_ELEMENT_ARRAY_BUFFER_ARB, ( (GLHardwareIndexBuffer)( op.indexData.indexBuffer ) ).GLBufferID );

					pBufferData = BUFFER_OFFSET( op.indexData.indexStart*op.indexData.indexBuffer.IndexSize );
				}
				else
				{
					pBufferData = ( (GLDefaultHardwareIndexBuffer)( op.indexData.indexBuffer ) ).DataPtr( op.indexData.indexStart * op.indexData.indexBuffer.IndexSize );
				}

				var indexType = ( op.indexData.indexBuffer.Type == IndexType.Size16 ) ? Gl.GL_UNSIGNED_SHORT : Gl.GL_UNSIGNED_INT;

				do
				{
					// Update derived depth bias
					if ( derivedDepthBias && currentPassIterationNum > 0 )
					{
						SetDepthBias( derivedDepthBiasBase + derivedDepthBiasMultiplier*currentPassIterationNum,
						              derivedDepthBiasSlopeScale );
					}
					if ( hasInstanceData )
					{
						Gl.glDrawElementsInstancedEXT( primType, op.indexData.indexCount, indexType, pBufferData, numberOfInstances );
					}
					else
					{
						Gl.glDrawElements( primType, op.indexData.indexCount, indexType, pBufferData );
					}
				}
				while ( UpdatePassIterationRenderState() );
			}
			else
			{
				do
				{
					// Update derived depth bias
					if ( derivedDepthBias && currentPassIterationNum > 0 )
					{
						SetDepthBias( derivedDepthBiasBase + derivedDepthBiasMultiplier*currentPassIterationNum,
						              derivedDepthBiasSlopeScale );
					}

					if ( hasInstanceData )
					{
						Gl.glDrawArraysInstancedEXT( primType, 0, op.vertexData.vertexCount, numberOfInstances );
					}
					else
					{
						Gl.glDrawArrays( primType, 0, op.vertexData.vertexCount );
					}
				}
				while ( UpdatePassIterationRenderState() );
			}

			Gl.glDisableClientState( Gl.GL_VERTEX_ARRAY );
			// only valid up to GL_MAX_TEXTURE_UNITS, which is recorded in mFixedFunctionTextureUnits
			if ( multitexturing )
			{
				for ( var i = 0; i < this._fixedFunctionTextureUnits; i++ )
				{
					Gl.glClientActiveTextureARB( Gl.GL_TEXTURE0 + i );
					Gl.glDisableClientState( Gl.GL_TEXTURE_COORD_ARRAY );
				}
				Gl.glClientActiveTextureARB( Gl.GL_TEXTURE0 );
			}
			else
			{
				Gl.glDisableClientState( Gl.GL_TEXTURE_COORD_ARRAY );
			}
			Gl.glDisableClientState( Gl.GL_NORMAL_ARRAY );
			Gl.glDisableClientState( Gl.GL_COLOR_ARRAY );

			if ( this.GLEW_EXT_secondary_color )
			{
				Gl.glDisableClientState( Gl.GL_SECONDARY_COLOR_ARRAY );
			}
			// unbind any custom attributes
			foreach ( var ai in attribsBound )
			{
				Gl.glDisableVertexAttribArrayARB( ai );
			}

			// unbind any instance attributes
			foreach ( var ai in instanceAttribsBound )
			{
				glVertexAttribDivisor( ai, 0 );
			}

			Gl.glColor4f( 1, 1, 1, 1 );
			if ( this.GLEW_EXT_secondary_color )
			{
				Gl.glSecondaryColor3fEXT( 0.0f, 0.0f, 0.0f );
			}
		}

		#endregion

		#region BindVertexElementToGpu

		[OgreVersion( 1, 7, 2790 )]
		protected void BindVertexElementToGpu( VertexElement elem, HardwareVertexBuffer vertexBuffer, int vertexStart,
		                                       IList<int> attribsBound, IList<int> instanceAttribsBound )
		{
			IntPtr pBufferData;
			var hwGlBuffer = (GLHardwareVertexBuffer)vertexBuffer;

			if ( currentCapabilities.HasCapability( Graphics.Capabilities.VertexBuffer ) )
			{
				Gl.glBindBufferARB( Gl.GL_ARRAY_BUFFER_ARB, hwGlBuffer.GLBufferID );
				pBufferData = BUFFER_OFFSET( elem.Offset );
			}
			else
			{
				// ReSharper disable PossibleInvalidCastException
				pBufferData = ( (GLDefaultHardwareVertexBuffer)( vertexBuffer ) ).DataPtr( elem.Offset );
				// ReSharper restore PossibleInvalidCastException
			}
			if ( vertexStart != 0 )
			{
				pBufferData = pBufferData.Offset( vertexStart*vertexBuffer.VertexSize );
			}

			var sem = elem.Semantic;
			var multitexturing = Capabilities.TextureUnitCount > 1;

			var isCustomAttrib = false;
			if ( this.currentVertexProgram != null )
			{
				isCustomAttrib = this.currentVertexProgram.IsAttributeValid( sem, (uint)elem.Index );

				if ( hwGlBuffer.IsInstanceData )
				{
					var attrib = this.currentVertexProgram.AttributeIndex( sem, (uint)elem.Index );
					glVertexAttribDivisor( (int)attrib, hwGlBuffer.InstanceDataStepRate );
					instanceAttribsBound.Add( (int)attrib );
				}
			}


			// Custom attribute support
			// tangents, binormals, blendweights etc always via this route
			// builtins may be done this way too
			if ( isCustomAttrib )
			{
				var attrib = this.currentVertexProgram.AttributeIndex( sem, (uint)elem.Index );
				var typeCount = VertexElement.GetTypeCount( elem.Type );
				var normalised = Gl.GL_FALSE;
				switch ( elem.Type )
				{
					case VertexElementType.Color:
					case VertexElementType.Color_ABGR:
					case VertexElementType.Color_ARGB:
						// Because GL takes these as a sequence of single unsigned bytes, count needs to be 4
						// VertexElement::getTypeCount treats them as 1 (RGBA)
						// Also need to normalise the fixed-point data
						typeCount = 4;
						normalised = Gl.GL_TRUE;
						break;
					default:
						break;
				}

				Gl.glVertexAttribPointerARB( attrib, typeCount, GLHardwareBufferManager.GetGLType( elem.Type ), normalised,
				                             vertexBuffer.VertexSize, pBufferData );
				Gl.glEnableVertexAttribArrayARB( attrib );

				attribsBound.Add( (int)attrib );
			}
			else
			{
				// fixed-function & builtin attribute support
				switch ( sem )
				{
					case VertexElementSemantic.Position:
						Gl.glVertexPointer( VertexElement.GetTypeCount( elem.Type ), GLHardwareBufferManager.GetGLType( elem.Type ),
						                    vertexBuffer.VertexSize, pBufferData );
						Gl.glEnableClientState( Gl.GL_VERTEX_ARRAY );
						break;
					case VertexElementSemantic.Normal:
						Gl.glNormalPointer( GLHardwareBufferManager.GetGLType( elem.Type ), vertexBuffer.VertexSize, pBufferData );
						Gl.glEnableClientState( Gl.GL_NORMAL_ARRAY );
						break;
					case VertexElementSemantic.Diffuse:
						Gl.glColorPointer( 4, GLHardwareBufferManager.GetGLType( elem.Type ), vertexBuffer.VertexSize, pBufferData );
						Gl.glEnableClientState( Gl.GL_COLOR_ARRAY );
						break;
					case VertexElementSemantic.Specular:
						if ( this.GLEW_EXT_secondary_color )
						{
							Gl.glSecondaryColorPointerEXT( 4, GLHardwareBufferManager.GetGLType( elem.Type ), vertexBuffer.VertexSize,
							                               pBufferData );
							Gl.glEnableClientState( Gl.GL_SECONDARY_COLOR_ARRAY );
						}
						break;
					case VertexElementSemantic.TexCoords:

						if ( this.currentVertexProgram != null )
						{
							// Programmable pipeline - direct UV assignment
							Gl.glClientActiveTextureARB( Gl.GL_TEXTURE0 + elem.Index );
							Gl.glTexCoordPointer( VertexElement.GetTypeCount( elem.Type ), GLHardwareBufferManager.GetGLType( elem.Type ),
							                      vertexBuffer.VertexSize, pBufferData );
							Gl.glEnableClientState( Gl.GL_TEXTURE_COORD_ARRAY );
						}
						else
						{
							// fixed function matching to units based on tex_coord_set
							for ( var i = 0; i < disabledTexUnitsFrom; i++ )
							{
								// Only set this texture unit's texcoord pointer if it
								// is supposed to be using this element's index
								if ( this.texCoordIndex[ i ] != elem.Index || i >= this._fixedFunctionTextureUnits )
								{
									continue;
								}

								if ( multitexturing )
								{
									Gl.glClientActiveTextureARB( Gl.GL_TEXTURE0 + i );
								}
								Gl.glTexCoordPointer( VertexElement.GetTypeCount( elem.Type ), GLHardwareBufferManager.GetGLType( elem.Type ),
								                      vertexBuffer.VertexSize, pBufferData );
								Gl.glEnableClientState( Gl.GL_TEXTURE_COORD_ARRAY );
							}
						}
						break;
					default:
						break;
				}
			} // isCustomAttrib
		}

		#endregion
	}
}

#pragma warning restore 612,618