using System;
using System.Collections.Generic;
using System.Text;

using Axiom.Core;
using Axiom.MathLib;

namespace Axiom
{
    public class MovableText : SimpleRenderable
    {
        #region Properties and Fields

        const int POS_TEX_BINDING = 0;
        const int COLOR_BINDING   = 1;

        private RenderOperation	_renderOperation;

		private bool			_needUpdate;
        private bool            _updateColor;

		private float			_timeUntilNextToggle;
		private float			_radius;

		private Font			_font;
        private string _fontName;
		public string FontName
        {
            get{ return _fontName;  }
            set
            {
		        if (_fontName != value || this.material == null || _font == null )
		        {
			        _fontName = value;
			        _font = (Font)FontManager.Instance.GetByName( _fontName );
			        if (_font == null)
				        throw new AxiomException( String.Format( "Could not find font '{0}'.", _fontName ) );
			        _font.Load();
                    if ( this.material != null )
			        {
                        if ( material.Name != "BaseWhite" )
                            MaterialManager.Instance.Unload( this.material );
                        this.material = null;
			        }
                    this.material = _font.Material.Clone( name + "Material" );
                    if ( this.material.IsLoaded == true )
                        this.material.Load();
                    this.material.DepthCheck = !_onTop;
                    this.material.Lighting = false;
			        _needUpdate = true;
		        }
            }
        }

        private string _caption;
		public string Caption
        {
            get{ return _caption;  }
            set
            {
                _caption = value;
                _needUpdate = true;
            }
        }

        private ColorEx _color;
		public ColorEx Color
        {
            get{ return _color;  }
            set
            {
                _color = value;
                _updateColor = true;
            }
        }

        private int _characterHeight;
		public int CharacterHeight
        {
            get{ return _characterHeight;  }
            set
            {
                _characterHeight = value;
                _needUpdate = true;
            }
        }

        private int _spaceWidth;
		public int SpaceWidth
        {
            get{ return _spaceWidth;  }
            set
            {
                _spaceWidth = value;
                _needUpdate = true;
            }
        }

        private bool _onTop;
		public bool OnTop
        {
            get{ return _onTop;  }
            set
            {
                _onTop = value;
                if ( this.material != null )
                {
                    this.material.DepthCheck = !_onTop;
                }
            }
        }

        #endregion Properties and Fields

        public MovableText(string name, string caption ) : this ( name, caption, "TrebuchetMSBold", 16, ColorEx.White ) {}
        public MovableText(string name, string caption, string fontName, int charHeight, ColorEx color )
        {
		    if (name == "") throw new AxiomException( "Trying to create MovableText without name." );
    		if (caption == "") throw new AxiomException( "Trying to create MovableText without caption." );

            _renderOperation = new RenderOperation();
            this.Name = name;
            _caption = caption;
            _characterHeight = charHeight;
            _color = color;
            _timeUntilNextToggle = 0;
            _spaceWidth = 0;
            _updateColor = true;
            _onTop = true;

		    this.FontName = fontName;
		    this._setupGeometry();
        }

        private Vector3 _translate3Dto2D( Camera camera, Vector3 vertex )
        {
            return camera.ProjectionMatrix * camera.ViewMatrix * vertex;
        }

        private void _translate3Dto2DPixels( Camera camera, Vector3 vertex, out int x, out int y )
        {
		    // calculate hsc screen coordinates
		    Vector3 hsc = _translate3Dto2D( camera, vertex);
		    // convert to window position in pixels
            //RenderTarget *rt = Root.Instance.RenderTarget(in.getName());
            //if ( !rt )
            //    throw new AxiomException( string.Format( "Can't find '{0}' render target", mpWin.getName() ) );
		    x = (int)((hsc.x + 1.0f) / 2.0f * 640 );
		    y = (int)((-hsc.y + 1.0f) / 2.0f * 480 );
        }

        private void _setupGeometry()
        {
		    int vertexCount = _caption.Length * 6;
		    if ( _renderOperation.vertexData != null )
		    {
			    if ( _renderOperation.vertexData.vertexCount != vertexCount )
			    {
				    //delete _renderOperation.vertexData;
				    _renderOperation.vertexData = null;
				    _updateColor = true;
			    }
		    }

		    if ( _renderOperation.vertexData == null )
			    _renderOperation.vertexData = new VertexData();

            _renderOperation.indexData = null;
		    _renderOperation.vertexData.vertexStart = 0;
		    _renderOperation.vertexData.vertexCount = vertexCount;
		    _renderOperation.operationType = OperationType.TriangleList; 
		    _renderOperation.useIndices = false; 

            VertexDeclaration	decl = _renderOperation.vertexData.vertexDeclaration;
            VertexBufferBinding	bind = _renderOperation.vertexData.vertexBufferBinding;
            int offset = 0;

		    // create/bind positions/tex.ccord. buffer
		    if ( decl.FindElementBySemantic( VertexElementSemantic.Position ) == null )
			    decl.AddElement( POS_TEX_BINDING, offset, VertexElementType.Float3,  VertexElementSemantic.Position );
            offset += VertexElement.GetTypeSize( VertexElementType.Float3 );

		    if ( decl.FindElementBySemantic( VertexElementSemantic.TexCoords ) == null )
			    decl.AddElement( POS_TEX_BINDING, offset, VertexElementType.Float2, VertexElementSemantic.TexCoords, 0);

            HardwareVertexBuffer vbuf = HardwareBufferManager.Instance.CreateVertexBuffer( decl.GetVertexSize( POS_TEX_BINDING ),
                                                                                           _renderOperation.vertexData.vertexCount,
				                                                                           BufferUsage.DynamicWriteOnly );
            bind.SetBinding( POS_TEX_BINDING, vbuf );

            // Colours - store these in a separate buffer because they change less often
		    if ( decl.FindElementBySemantic( VertexElementSemantic.Diffuse ) == null )
			    decl.AddElement( COLOR_BINDING, 0, VertexElementType.Color, VertexElementSemantic.Diffuse );
            HardwareVertexBuffer cbuf = HardwareBufferManager.Instance.CreateVertexBuffer( decl.GetVertexSize( COLOR_BINDING ),
                                                                                           _renderOperation.vertexData.vertexCount,
				                                                                           BufferUsage.DynamicWriteOnly );
            bind.SetBinding( COLOR_BINDING, cbuf );

		    int charlen = _caption.Length;

		    float largestWidth = 0;
            float left = 0f * 2.0f - 1.0f;
            float top = -( (0f * 2.0f ) - 1.0f );

            // Derive space with from a capital A
		    if ( _spaceWidth == 0 )
			    _spaceWidth = (int)(_font.GetGlyphAspectRatio( 'A' ) * _characterHeight * 2.0f);

		    // for calculation of AABB
		    Vector3 min, max, currPos;
		    float maxSquaredRadius = 0;
		    bool first = true;

            min = max = currPos = Vector3.NegativeUnitY;
		    // Use iterator
            bool newLine = true;

            //Real *pPCBuff = static_cast<Real*>(ptbuf.lock(HardwareBuffer::HBL_DISCARD));
            IntPtr ipPos = vbuf.Lock( BufferLocking.Discard );
            int cntPos = 0;
            unsafe
            {
                float *pPCBuff = (float*)ipPos.ToPointer();

                for ( int i = 0; i != charlen; i++ )
                {
                    if ( newLine )
                    {
                        float len = 0.0f;
                        for ( int j = i; j != charlen && _caption[ j ] != '\n'; j++ )
                        {
                            if ( _caption[ j ] == ' ' )
                                len += _spaceWidth;
                            else
                                len += _font.GetGlyphAspectRatio( _caption[ j ] ) * _characterHeight * 2.0f;
                        }
                        newLine = false;
                    }

                    if ( _caption[ i ] == '\n' )
                    {
                        left = 0f * 2.0f - 1.0f;
                        top -= _characterHeight * 2.0f;
                        newLine = true;
                        continue;
                    }

                    if ( _caption[ i ] == ' ' )
                    {
                        // Just leave a gap, no tris
                        left += _spaceWidth;
                        // Also reduce tri count
                        _renderOperation.vertexData.vertexCount -= 6;
                        continue;
                    }

                    float horiz_height = _font.GetGlyphAspectRatio( _caption[ i ] );
                    float u1, u2, v1, v2;
                    _font.GetGlyphTexCoords( _caption[ i ], out u1, out v1, out u2, out v2 );

                    // each vert is (x, y, z, u, v)
                    //-------------------------------------------------------------------------------------
                    // First tri
                    //
                    // Upper left
                    pPCBuff[ cntPos++ ] = left;
                    pPCBuff[ cntPos++ ] = top;
                    pPCBuff[ cntPos++ ] = -1.0f;
                    pPCBuff[ cntPos++ ] = u1;
                    pPCBuff[ cntPos++ ] = v1;

                    // Deal with bounds
                    currPos = new Vector3( left, top, -1.0f );
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
                        maxSquaredRadius = Math.Max( maxSquaredRadius, currPos.LengthSquared );
                    }

                    top -= _characterHeight * 2.0f;

                    // Bottom left
                    pPCBuff[ cntPos++ ] = left;
                    pPCBuff[ cntPos++ ] = top;
                    pPCBuff[ cntPos++ ] = -1.0f;
                    pPCBuff[ cntPos++ ] = u1;
                    pPCBuff[ cntPos++ ] = v2;

                    // Deal with bounds
                    currPos = new Vector3( left, top, -1.0f );
                    min.Floor( currPos );
                    max.Ceil( currPos );
                    maxSquaredRadius = Math.Max( maxSquaredRadius, currPos.LengthSquared );

                    top += _characterHeight * 2.0f;
                    left += horiz_height * _characterHeight * 2.0f;

                    // Top right
                    pPCBuff[ cntPos++ ] = left;
                    pPCBuff[ cntPos++ ] = top;
                    pPCBuff[ cntPos++ ] = -1.0f;
                    pPCBuff[ cntPos++ ] = u2;
                    pPCBuff[ cntPos++ ] = v1;
                    //-------------------------------------------------------------------------------------

                    // Deal with bounds
                    currPos = new Vector3( left, top, -1.0f );
                    min.Floor( currPos );
                    max.Ceil( currPos );
                    maxSquaredRadius = Math.Max( maxSquaredRadius, currPos.LengthSquared );

                    //-------------------------------------------------------------------------------------
                    // Second tri
                    //
                    // Top right (again)
                    pPCBuff[ cntPos++ ] = left;
                    pPCBuff[ cntPos++ ] = top;
                    pPCBuff[ cntPos++ ] = -1.0f;
                    pPCBuff[ cntPos++ ] = u2;
                    pPCBuff[ cntPos++ ] = v1;

                    currPos = new Vector3( left, top, -1.0f );
                    min.Floor( currPos );
                    max.Ceil( currPos );
                    maxSquaredRadius = Math.Max( maxSquaredRadius, currPos.LengthSquared );

                    top -= _characterHeight * 2.0f;
                    left -= horiz_height * _characterHeight * 2.0f;

                    // Bottom left (again)
                    pPCBuff[ cntPos++ ] = left;
                    pPCBuff[ cntPos++ ] = top;
                    pPCBuff[ cntPos++ ] = -1.0f;
                    pPCBuff[ cntPos++ ] = u1;
                    pPCBuff[ cntPos++ ] = v2;

                    currPos = new Vector3( left, top, -1.0f );
                    min.Floor( currPos );
                    max.Ceil( currPos );
                    maxSquaredRadius = Math.Max( maxSquaredRadius, currPos.LengthSquared );

                    left += horiz_height * _characterHeight * 2.0f;

                    // Bottom right
                    pPCBuff[ cntPos++ ] = left;
                    pPCBuff[ cntPos++ ] = top;
                    pPCBuff[ cntPos++ ] = -1.0f;
                    pPCBuff[ cntPos++ ] = u2;
                    pPCBuff[ cntPos++ ] = v2;
                    //-------------------------------------------------------------------------------------

                    currPos = new Vector3( left, top, -1.0f );
                    min.Floor( currPos );
                    max.Ceil( currPos );
                    maxSquaredRadius = Math.Max( maxSquaredRadius, currPos.LengthSquared );

                    // Go back up with top
                    top += _characterHeight * 2.0f;

                    float currentWidth = ( left + 1 ) / 2 - 0;
                    if ( currentWidth > largestWidth )
                        largestWidth = currentWidth;
                }
            }
            // Unlock vertex buffer
            vbuf.Unlock();

		    // update AABB/Sphere radius
		    this.box = new AxisAlignedBox(min, max);
		    this._radius = (float)Math.Sqrt( (double)maxSquaredRadius );

		    if ( _updateColor )
			    this._updateColors();

		    _needUpdate = false;
        }

        private void _updateColors()
        {
            //assert(mpFont);
            //assert(!mpMaterial.isNull());

		    // Convert to system-specific
            int color;
            color = Root.Instance.ConvertColor( _color );
            HardwareVertexBuffer cbuf = _renderOperation.vertexData.vertexBufferBinding.GetBuffer( COLOR_BINDING );
            IntPtr ipPos = cbuf.Lock( BufferLocking.Discard );
            unsafe
            {
                int* pPos = (int*)ipPos.ToPointer();
                for ( int i = 0; i < _renderOperation.vertexData.vertexCount; i++ )
                    pPos[i] = color;
            }
            cbuf.Unlock();
		    _updateColor = false;
        }

        #region Implementation of SimpleRenderable
        public override void GetRenderOperation( RenderOperation op )
        {
            op.useIndices = this._renderOperation.useIndices;
            op.operationType = this._renderOperation.operationType;
            op.vertexData = this._renderOperation.vertexData;
            op.indexData = this._renderOperation.indexData;
        }

        public override float GetSquaredViewDepth( Camera camera )
        {
            return ( this.ParentNode.DerivedPosition - camera.DerivedPosition ).LengthSquared;
        }

        public override float BoundingRadius
        {
            get
            {
                return _radius;
            }
        }
        #endregion Implementation of SimpleRenderable
    }
}
