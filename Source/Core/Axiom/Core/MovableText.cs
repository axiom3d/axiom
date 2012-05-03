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
using Axiom.Collections;
using Axiom.Fonts;
using Axiom.Graphics;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.Core
{
	public class MovableText : SimpleRenderable
	{
		#region Properties and Fields

		public enum HorizontalAlignment
		{
			Left,
			Center
		};

		public enum VerticalAlignment
		{
			Above,
			Below
		};

		// Vertex Buffer Binding Indexes
		private const int POS_TEX_BINDING = 0;
		private const int COLOR_BINDING = 1;

		private HorizontalAlignment _horizontalAlignment = HorizontalAlignment.Left;
		private VerticalAlignment _verticalAlignment = VerticalAlignment.Below;

		private float _additionalHeight = 0.0f;

		private bool _needUpdate;
		private bool _updateColor;

		//private float _timeUntilNextToggle;
		private Real _radius;

		private Font _font;
		private string _fontName;

		public string FontName
		{
			get
			{
				return _fontName;
			}

			set
			{
				if ( _fontName != value || material == null || _font == null )
				{
					_fontName = value;
					_font = (Font)FontManager.Instance[ _fontName ];
					if ( _font == null )
					{
						throw new AxiomException( String.Format( "Could not find font '{0}'.", _fontName ) );
					}
					_font.Load();
					if ( material != null )
					{
						if ( material.Name != "BaseWhite" )
						{
							MaterialManager.Instance.Unload( material );
						}
						material = null;
					}
					material = _font.Material.Clone( name + "Material", false, _font.Material.Group );
					if ( material.IsLoaded == true )
					{
						material.Load();
					}
					material.DepthCheck = !_onTop;
					material.Lighting = false;
					_needUpdate = true;
				}
			}
		}

		private string _caption;

		public string Caption
		{
			get
			{
				return _caption;
			}
			set
			{
				_caption = value;
				_needUpdate = true;
			}
		}

		private ColorEx _color;

		public ColorEx Color
		{
			get
			{
				return _color;
			}
			set
			{
				_color = value;
				_updateColor = true;
			}
		}

		private int _characterHeight;

		public int CharacterHeight
		{
			get
			{
				return _characterHeight;
			}
			set
			{
				_characterHeight = value;
				_needUpdate = true;
			}
		}

		private int _spaceWidth;

		public int SpaceWidth
		{
			get
			{
				return _spaceWidth;
			}
			set
			{
				_spaceWidth = value;
				_needUpdate = true;
			}
		}

		public HorizontalAlignment HorzAlignment
		{
			get
			{
				return _horizontalAlignment;
			}
			set
			{
				if ( _horizontalAlignment != value )
				{
					_needUpdate = true;
				}
				_horizontalAlignment = value;
			}
		}

		public VerticalAlignment VertAlignment
		{
			get
			{
				return _verticalAlignment;
			}
			set
			{
				if ( _verticalAlignment != value )
				{
					_needUpdate = true;
				}
				_verticalAlignment = value;
			}
		}

		public void SetTextAlignment( HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment )
		{
			if ( _horizontalAlignment != horizontalAlignment )
			{
				_horizontalAlignment = horizontalAlignment;
				_needUpdate = true;
			}

			if ( _verticalAlignment != verticalAlignment )
			{
				_verticalAlignment = verticalAlignment;
				_needUpdate = true;
			}
		}

		public float AdditionalHeight
		{
			get
			{
				return _additionalHeight;
			}
			set
			{
				if ( _additionalHeight != value )
				{
					_needUpdate = true;
				}
				_additionalHeight = value;
			}
		}

		private bool _onTop;

		public bool OnTop
		{
			get
			{
				return _onTop;
			}
			set
			{
				_onTop = value;
				if ( material != null )
				{
					material.DepthCheck = !_onTop;
				}
			}
		}

		#endregion Properties and Fields

		#region Construction and Destruction

		public MovableText( string name, string caption, string fontName )
			: this( name, caption, fontName, 12, ColorEx.White )
		{
		}

		public MovableText( string name, string caption, string fontName, int charHeight, ColorEx color )
			: base( name )
		{
			if ( string.IsNullOrEmpty( name ) )
			{
				throw new AxiomException( "Trying to create MovableText without name." );
			}
			if ( string.IsNullOrEmpty( caption ) )
			{
				throw new AxiomException( "Trying to create MovableText without caption." );
			}

			_caption = caption;
			_characterHeight = charHeight;
			_color = color;
			_spaceWidth = 0;
			_updateColor = true;
			_onTop = true;
			_horizontalAlignment = HorizontalAlignment.Center;

			FontName = fontName;
			_setupGeometry();
		}

		#endregion Construction and Destruction

		private Vector3 _translate3Dto2D( Camera camera, Vector3 vertex )
		{
			return camera.ProjectionMatrix*camera.ViewMatrix*vertex;
		}

		private void _translate3Dto2DPixels( Camera camera, Vector3 vertex, out int x, out int y )
		{
			// calculate hsc screen coordinates
			var hsc = _translate3Dto2D( camera, vertex );
			// convert to window position in pixels
			//RenderTarget *rt = Root.Instance.RenderTarget(in.getName());
			//if ( !rt )
			//    throw new AxiomException( string.Format( "Can't find '{0}' render target", mpWin.getName() ) );
			x = (int)( ( hsc.x + 1.0f )/2.0f*640.0f );
			y = (int)( ( -hsc.y + 1.0f )/2.0f*480.0f );
		}

		private void _setupGeometry()
		{
			var vertexCount = _caption.Length*6;
			if ( renderOperation.vertexData != null )
			{
				renderOperation.vertexData = null;
				_updateColor = true;
			}

			if ( renderOperation.vertexData == null )
			{
				renderOperation.vertexData = new VertexData();
			}

			renderOperation.indexData = null;
			renderOperation.vertexData.vertexStart = 0;
			renderOperation.vertexData.vertexCount = vertexCount;
			renderOperation.operationType = OperationType.TriangleList;
			renderOperation.useIndices = false;

			var decl = renderOperation.vertexData.vertexDeclaration;
			var bind = renderOperation.vertexData.vertexBufferBinding;
			var offset = 0;

			// create/bind positions/tex.ccord. buffer
			if ( decl.FindElementBySemantic( VertexElementSemantic.Position ) == null )
			{
				decl.AddElement( POS_TEX_BINDING, offset, VertexElementType.Float3, VertexElementSemantic.Position );
			}
			offset += VertexElement.GetTypeSize( VertexElementType.Float3 );

			if ( decl.FindElementBySemantic( VertexElementSemantic.TexCoords ) == null )
			{
				decl.AddElement( POS_TEX_BINDING, offset, VertexElementType.Float2, VertexElementSemantic.TexCoords, 0 );
			}

			var vbuf = HardwareBufferManager.Instance.CreateVertexBuffer( decl.Clone( POS_TEX_BINDING ),
			                                                              renderOperation.vertexData.vertexCount,
			                                                              BufferUsage.DynamicWriteOnly );
			bind.SetBinding( POS_TEX_BINDING, vbuf );

			// Colors - store these in a separate buffer because they change less often
			if ( decl.FindElementBySemantic( VertexElementSemantic.Diffuse ) == null )
			{
				decl.AddElement( COLOR_BINDING, 0, VertexElementType.Color, VertexElementSemantic.Diffuse );
			}
			var cbuf = HardwareBufferManager.Instance.CreateVertexBuffer( decl.Clone( COLOR_BINDING ),
			                                                              renderOperation.vertexData.vertexCount,
			                                                              BufferUsage.DynamicWriteOnly );
			bind.SetBinding( COLOR_BINDING, cbuf );

			var charlen = _caption.Length;

			var largestWidth = 0.0f;
			var left = 0f*2.0f - 1.0f;
			var top = -( ( 0f*2.0f ) - 1.0f );

			// Derive space with from a capital A
			if ( _spaceWidth == 0 )
			{
				_spaceWidth = (int)( _font.GetGlyphAspectRatio( 'A' )*_characterHeight*2.0f );
			}

			// for calculation of AABB
			Vector3 min, max, currPos;
			var maxSquaredRadius = 0.0f;
			var first = true;

			min = max = currPos = Vector3.NegativeUnitY;
			// Use iterator
			var newLine = true;
			var len = 0.0f;

			if ( _verticalAlignment == VerticalAlignment.Above )
			{
				// Raise the first line of the caption
				top += _characterHeight;
				for ( var i = 0; i != charlen; i++ )
				{
					if ( _caption[ i ] == '\n' )
					{
						top += _characterHeight*2.0f;
					}
				}
			}

			//Real *pPCBuff = static_cast<Real*>(ptbuf.lock(HardwareBuffer::HBL_DISCARD));
			var ipPos = vbuf.Lock( BufferLocking.Discard );
			var cntPos = 0;
#if !AXIOM_SAFE_ONLY
			unsafe
#endif
			{
				var pPCBuff = ipPos.ToFloatPointer();

				for ( var i = 0; i != charlen; i++ )
				{
					if ( newLine )
					{
						len = 0.0f;
						for ( var j = i; j != charlen && _caption[ j ] != '\n'; j++ )
						{
							if ( _caption[ j ] == ' ' )
							{
								len += _spaceWidth;
							}
							else
							{
								len += _font.GetGlyphAspectRatio( _caption[ j ] )*_characterHeight*2.0f;
							}
						}
						newLine = false;
					}

					if ( _caption[ i ] == '\n' )
					{
						left = 0f*2.0f - 1.0f;
						top -= _characterHeight*2.0f;
						newLine = true;
						continue;
					}

					if ( _caption[ i ] == ' ' )
					{
						// Just leave a gap, no tris
						left += _spaceWidth;
						// Also reduce tri count
						renderOperation.vertexData.vertexCount -= 6;
						continue;
					}

					var horiz_height = _font.GetGlyphAspectRatio( _caption[ i ] );
					Real u1, u2, v1, v2;
					_font.GetGlyphTexCoords( _caption[ i ], out u1, out v1, out u2, out v2 );

					// each vert is (x, y, z, u, v)
					//-------------------------------------------------------------------------------------
					// First tri
					//
					// Upper left
					if ( _horizontalAlignment == HorizontalAlignment.Left )
					{
						pPCBuff[ cntPos++ ] = left;
					}
					else
					{
						pPCBuff[ cntPos++ ] = left - ( len/2.0f );
					}
					pPCBuff[ cntPos++ ] = top;
					pPCBuff[ cntPos++ ] = -1.0f;
					pPCBuff[ cntPos++ ] = u1;
					pPCBuff[ cntPos++ ] = v1;

					// Deal with bounds
					if ( _horizontalAlignment == HorizontalAlignment.Left )
					{
						currPos = new Vector3( left, top, -1.0f );
					}
					else
					{
						currPos = new Vector3( left - ( len/2.0f ), top, -1.0f );
					}

					if ( first )
					{
						min = max = currPos;
						maxSquaredRadius = currPos.LengthSquared;
						first = false;
					}
					else
					{
						min.Floor( currPos );
						max.Ceil( currPos );
						maxSquaredRadius = Utility.Max( maxSquaredRadius, currPos.LengthSquared );
					}

					top -= _characterHeight*2.0f;

					// Bottom left
					if ( _horizontalAlignment == HorizontalAlignment.Left )
					{
						pPCBuff[ cntPos++ ] = left;
					}
					else
					{
						pPCBuff[ cntPos++ ] = left - ( len/2.0f );
					}
					pPCBuff[ cntPos++ ] = top;
					pPCBuff[ cntPos++ ] = -1.0f;
					pPCBuff[ cntPos++ ] = u1;
					pPCBuff[ cntPos++ ] = v2;

					// Deal with bounds
					if ( _horizontalAlignment == HorizontalAlignment.Left )
					{
						currPos = new Vector3( left, top, -1.0f );
					}
					else
					{
						currPos = new Vector3( left - ( len/2.0f ), top, -1.0f );
					}

					min.Floor( currPos );
					max.Ceil( currPos );
					maxSquaredRadius = Utility.Max( maxSquaredRadius, currPos.LengthSquared );

					top += _characterHeight*2.0f;
					left += horiz_height*_characterHeight*2.0f;

					// Top right
					if ( _horizontalAlignment == HorizontalAlignment.Left )
					{
						pPCBuff[ cntPos++ ] = left;
					}
					else
					{
						pPCBuff[ cntPos++ ] = left - ( len/2.0f );
					}
					pPCBuff[ cntPos++ ] = top;
					pPCBuff[ cntPos++ ] = -1.0f;
					pPCBuff[ cntPos++ ] = u2;
					pPCBuff[ cntPos++ ] = v1;
					//-------------------------------------------------------------------------------------

					// Deal with bounds
					if ( _horizontalAlignment == HorizontalAlignment.Left )
					{
						currPos = new Vector3( left, top, -1.0f );
					}
					else
					{
						currPos = new Vector3( left - ( len/2.0f ), top, -1.0f );
					}
					min.Floor( currPos );
					max.Ceil( currPos );
					maxSquaredRadius = Utility.Max( maxSquaredRadius, currPos.LengthSquared );

					//-------------------------------------------------------------------------------------
					// Second tri
					//
					// Top right (again)
					if ( _horizontalAlignment == HorizontalAlignment.Left )
					{
						pPCBuff[ cntPos++ ] = left;
					}
					else
					{
						pPCBuff[ cntPos++ ] = left - ( len/2.0f );
					}
					pPCBuff[ cntPos++ ] = top;
					pPCBuff[ cntPos++ ] = -1.0f;
					pPCBuff[ cntPos++ ] = u2;
					pPCBuff[ cntPos++ ] = v1;

					currPos = new Vector3( left, top, -1.0f );
					min.Floor( currPos );
					max.Ceil( currPos );
					maxSquaredRadius = Utility.Max( maxSquaredRadius, currPos.LengthSquared );

					top -= _characterHeight*2.0f;
					left -= horiz_height*_characterHeight*2.0f;

					// Bottom left (again)
					if ( _horizontalAlignment == HorizontalAlignment.Left )
					{
						pPCBuff[ cntPos++ ] = left;
					}
					else
					{
						pPCBuff[ cntPos++ ] = left - ( len/2.0f );
					}
					pPCBuff[ cntPos++ ] = top;
					pPCBuff[ cntPos++ ] = -1.0f;
					pPCBuff[ cntPos++ ] = u1;
					pPCBuff[ cntPos++ ] = v2;

					currPos = new Vector3( left, top, -1.0f );
					min.Floor( currPos );
					max.Ceil( currPos );
					maxSquaredRadius = Utility.Max( maxSquaredRadius, currPos.LengthSquared );

					left += horiz_height*_characterHeight*2.0f;

					// Bottom right
					if ( _horizontalAlignment == HorizontalAlignment.Left )
					{
						pPCBuff[ cntPos++ ] = left;
					}
					else
					{
						pPCBuff[ cntPos++ ] = left - ( len/2.0f );
					}
					pPCBuff[ cntPos++ ] = top;
					pPCBuff[ cntPos++ ] = -1.0f;
					pPCBuff[ cntPos++ ] = u2;
					pPCBuff[ cntPos++ ] = v2;
					//-------------------------------------------------------------------------------------

					currPos = new Vector3( left, top, -1.0f );
					min.Floor( currPos );
					max.Ceil( currPos );
					maxSquaredRadius = Utility.Max( maxSquaredRadius, currPos.LengthSquared );

					// Go back up with top
					top += _characterHeight*2.0f;

					var currentWidth = ( left + 1.0f )/2.0f - 0.0f;
					if ( currentWidth > largestWidth )
					{
						largestWidth = currentWidth;
					}
				}
			}
			// Unlock vertex buffer
			vbuf.Unlock();

			// update AABB/Sphere radius
			box = new AxisAlignedBox( min, max );
			_radius = Utility.Sqrt( maxSquaredRadius );

			if ( _updateColor )
			{
				_updateColors();
			}

			_needUpdate = false;
		}

		private void _updateColors()
		{
			//assert(mpFont);
			//assert(!mpMaterial.isNull());

			// Convert to system-specific
			int color;
			color = Root.Instance.ConvertColor( _color );
			var cbuf = renderOperation.vertexData.vertexBufferBinding.GetBuffer( COLOR_BINDING );
			var ipPos = cbuf.Lock( BufferLocking.Discard );
#if !AXIOM_SAFE_ONLY
			unsafe
#endif
			{
				var pPos = ipPos.ToIntPointer();
				for ( var i = 0; i < renderOperation.vertexData.vertexCount; i++ )
				{
					pPos[ i ] = color;
				}
			}
			cbuf.Unlock();
			_updateColor = false;
		}

		#region Implementation of SimpleRenderable

		public override void GetWorldTransforms( Matrix4[] matrices )
		{
			if ( IsVisible && camera != null )
			{
				var scale3x3 = Matrix3.Identity;

				// store rotation in a matrix
				var rot3x3 = camera.DerivedOrientation.ToRotationMatrix();

				// parent node position
				var ppos = ParentNode.DerivedPosition + Vector3.UnitY*_additionalHeight;

				// apply scale
				scale3x3.m00 = ParentNode.DerivedScale.x/2.0f;
				scale3x3.m11 = ParentNode.DerivedScale.y/2.0f;
				scale3x3.m22 = ParentNode.DerivedScale.z/2.0f;

				// apply all transforms to matrices
				matrices[ 0 ] = rot3x3*scale3x3;
				matrices[ 0 ].Translation = ppos;
			}
		}

		public override Real GetSquaredViewDepth( Camera camera )
		{
			return ( ParentNode.DerivedPosition - camera.DerivedPosition ).LengthSquared;
		}

		public override Real BoundingRadius
		{
			get
			{
				return _radius;
			}
		}

		public override RenderOperation RenderOperation
		{
			get
			{
				if ( _needUpdate )
				{
					_setupGeometry();
				}
				if ( _updateColor )
				{
					_updateColors();
				}
				return base.RenderOperation;
			}
		}

		#endregion Implementation of SimpleRenderable
	}

	#region MovableObjectFactory implementation

	public class MovableTextFactory : MovableObjectFactory
	{
		public new const string TypeName = "MovableText";

		public MovableTextFactory()
		{
			base.Type = MovableTextFactory.TypeName;
			base.TypeFlag = (uint)SceneQueryTypeMask.Entity;
		}

		protected override MovableObject _createInstance( string name, NamedParameterList param )
		{
			// must have mesh parameter
			string caption = null;
			string fontName = null;

			if ( param != null )
			{
				if ( param.ContainsKey( "caption" ) )
				{
					caption = (string)param[ "caption" ];
				}
				if ( param.ContainsKey( "fontName" ) )
				{
					fontName = (string)param[ "fontName" ];
				}
			}
			if ( caption == null )
			{
				throw new AxiomException( "'caption' parameter required when constructing MovableText." );
			}
			if ( fontName == null )
			{
				throw new AxiomException( "'fontName' parameter required when constructing MovableText." );
			}

			var text = new MovableText( name, caption, fontName );
			text.MovableType = Type;
			return text;
		}

		public override void DestroyInstance( ref MovableObject obj )
		{
			( (MovableText)obj ).Dispose();
			obj = null;
		}
	}

	#endregion MovableObjectFactory implementation
}