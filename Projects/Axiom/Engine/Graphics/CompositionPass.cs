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
/*
 * Many thanks to the folks at Multiverse for providing the initial port for this class
 */

#endregion

#region SVN Version Information

// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;

using Axiom.Core;
using Axiom.Configuration;

using System.Collections.Generic;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
    /// <summary>
    ///  Object representing one pass or operation in a composition sequence. This provides a
    ///  method to conveniently interleave RenderSystem commands between Render Queues.
    /// </summary>
    public class CompositionPass
    {
        /// <summary>
        /// Inputs (for material used for rendering the quad)
        /// </summary>
        public struct InputTexture
        {
            /// <summary>
            /// Name (local) of the input texture (empty == no input)
            /// </summary>
            public string Name;

            /// <summary>
            /// MRT surface index if applicable
            /// </summary>
            public int MRTIndex;

            /// <summary>
            /// 
            /// </summary>
            /// <param name="name">Name (local) of the input texture (empty == no input)</param>
            public InputTexture( string name )
            {
                Name = name;
                MRTIndex = 0;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="name">Name (local) of the input texture (empty == no input)</param>
            /// <param name="mrtIndex">MRT surface index if applicable</param>
            public InputTexture( string name, int mrtIndex )
            {
                Name = name;
                MRTIndex = mrtIndex;
            }
        }

        /// <summary>
        /// Parent technique
        /// </summary>
        private CompositionTargetPass _parent;

        /// <summary>
        /// Type of composition pass
        /// </summary>
        private CompositorPassType _type;

        /// <summary>
        /// Identifier for this pass
        /// </summary>
        private uint _identifier;

        /// <summary>
        /// Material used for rendering
        /// </summary>
        private Material _material;

        /// <summary>
        /// first render queue to render this pass (in case of RENDERSCENE)
        /// </summary>
        private RenderQueueGroupID _firstRenderQueue;

        /// <summary>
        /// last render queue to render this pass (in case of RENDERSCENE)
        /// </summary>
        private RenderQueueGroupID _lastRenderQueue;

        /// <summary>
        /// Material scheme name
        /// </summary>
        private string _materialSchemeName;

        /// <summary>
        /// Clear buffers (in case of CLEAR)
        /// </summary>
        private FrameBufferType _clearBuffers;

        /// <summary>
        /// Clear colour (in case of CLEAR)
        /// </summary>
        private ColorEx _clearColor;

        /// <summary>
        /// Clear depth (in case of CLEAR)
        /// </summary>
        private float _clearDepth;

        /// <summary>
        /// Clear stencil value (in case of CLEAR)
        /// </summary>
        private int _clearStencil;

        /// <summary>
        /// Inputs (for material used for rendering the quad)
        /// An empty string signifies that no input is used
        /// </summary>
        private InputTexture[] _inputs = new InputTexture[Axiom.Configuration.Config.MaxTextureLayers];

        /// <summary>
        /// Stencil operation parameters
        /// </summary>
        private bool _stencilCheck;

        /// <summary>
        /// 
        /// </summary>
        private CompareFunction _stencilFunc;

        /// <summary>
        /// 
        /// </summary>
        private int _stencilRefValue;

        /// <summary>
        /// 
        /// </summary>
        private int _stencilMask;

        /// <summary>
        /// 
        /// </summary>
        private StencilOperation _stencilFailOp;

        /// <summary>
        /// 
        /// </summary>
        private StencilOperation _stencilDepthFailOp;

        /// <summary>
        /// 
        /// </summary>
        private StencilOperation _stencilPassOp;

        /// <summary>
        /// 
        /// </summary>
        private bool _stencilTwoSidedOperation;

        /// <summary>
        /// true if quad should not cover whole screen
        /// </summary>
        private bool _quadCornerModified;

        /// <summary>
        /// quad positions in normalised coordinates [-1;1]x[-1;1] (in case of RENDERQUAD)
        /// </summary>
        private float _quadLeft;

        /// <summary>
        /// quad positions in normalised coordinates [-1;1]x[-1;1] (in case of RENDERQUAD)
        /// </summary>
        private float _quadTop;

        /// <summary>
        /// quad positions in normalised coordinates [-1;1]x[-1;1] (in case of RENDERQUAD)
        /// </summary>
        private float _quadRight;

        /// <summary>
        /// quad positions in normalised coordinates [-1;1]x[-1;1] (in case of RENDERQUAD)
        /// </summary>
        private float _quadBottom;

        /// <summary>
        /// 
        /// </summary>
        private bool _quadFarCorners;

        /// <summary>
        /// 
        /// </summary>
        private bool _quadFarCornersViewSpace;

        /// <summary>
        /// 
        /// </summary>
        private string _customType;

        /// <summary>
        /// Get's or set's type of composition pass
        /// </summary>
        public CompositorPassType Type
        {
            get
            {
                return _type;
            }
            set
            {
                _type = value;
            }
        }

        /// <summary>
        /// Get's or set's an identifier for this pass. This identifier can be used to
        /// "listen in" on this pass with an CompositorInstance event.
        /// </summary>
        public uint Identifier
        {
            get
            {
                return _identifier;
            }
            set
            {
                _identifier = value;
            }
        }

        /// <summary>
        /// Get's or set's the material used by this pass
        /// </summary>
        /// <note>
        /// applies when PassType is RENDERQUAD
        /// </note>
        public Material Material
        {
            get
            {
                return _material;
            }
            set
            {
                _material = value;
            }
        }

        /// <summary>
        /// Get's or set's the first render queue to be rendered in this pass (inclusive)
        /// </summary>
        /// <note>
        /// applies when PassType is RENDERSCENE
        /// </note>
        public RenderQueueGroupID FirstRenderQueue
        {
            get
            {
                return _firstRenderQueue;
            }
            set
            {
                _firstRenderQueue = value;
            }
        }

        /// <summary>
        /// Get's or set's the last render queue to be rendered in this pass (inclusive)
        /// </summary>
        /// <note>
        /// applies when PassType is RENDERSCENE
        /// </note>
        public RenderQueueGroupID LastRenderQueue
        {
            get
            {
                return _lastRenderQueue;
            }
            set
            {
                _lastRenderQueue = value;
            }
        }

        /// <summary>
        /// Get's or set's the material scheme used by this pass
        /// </summary>
        /// <remarks>
        /// Only applicable to passes that render the scene.
        /// <see cref="Technique.Scheme"/>
        /// </remarks>
        public string MaterialScheme
        {
            get
            {
                return _materialSchemeName;
            }
            set
            {
                _materialSchemeName = value;
            }
        }

        /// <summary>
        /// Get's or Set's the viewport clear buffers  (defaults to FrameBufferType.Color | FrameBufferType.Depth)
        /// </summary>
        /// <note>
        /// value is a combination of FrameBufferType.Color, FrameBufferType.Depth, FrameBufferType.Stencil
        /// </note>
        public FrameBufferType ClearBuffers
        {
            get
            {
                return _clearBuffers;
            }
            set
            {
                _clearBuffers = value;
            }
        }

        /// <summary>
        /// Get's or set's the viewport clear color (defaults to 0,0,0,0)
        /// </summary>
        public ColorEx ClearColor
        {
            get
            {
                return _clearColor;
            }
            set
            {
                _clearColor = value;
            }
        }

        /// <summary>
        /// Get's or set's the viewport clear depth (defaults to 1.0)
        /// </summary>
        public float ClearDepth
        {
            get
            {
                return _clearDepth;
            }
            set
            {
                _clearDepth = value;
            }
        }

        /// <summary>
        /// Get's or set's the viewport clear stencil value (defaults to 0)
        /// </summary>
        public int ClearStencil
        {
            get
            {
                return _clearStencil;
            }
            set
            {
                _clearStencil = value;
            }
        }

        /// <summary>
        /// Get's or Set's stencil check on or off.
        /// </summary>
        public bool StencilCheck
        {
            get
            {
                return _stencilCheck;
            }
            set
            {
                _stencilCheck = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public CompareFunction StencilFunc
        {
            get
            {
                return _stencilFunc;
            }
            set
            {
                _stencilFunc = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public int StencilRefValue
        {
            get
            {
                return _stencilRefValue;
            }
            set
            {
                _stencilRefValue = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public int StencilMask
        {
            get
            {
                return _stencilMask;
            }
            set
            {
                _stencilMask = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public StencilOperation StencilFailOp
        {
            get
            {
                return _stencilFailOp;
            }
            set
            {
                _stencilFailOp = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public StencilOperation StencilDepthFailOp
        {
            get
            {
                return _stencilDepthFailOp;
            }
            set
            {
                _stencilDepthFailOp = value;
            }
        }

        /// <summary>
        /// Get's or Sets stencil pass operation.
        /// </summary>
        public StencilOperation StencilPassOp
        {
            get
            {
                return _stencilPassOp;
            }
            set
            {
                _stencilPassOp = value;
            }
        }

        /// <summary>
        /// Get's or set's two sided stencil operation.
        /// </summary>
        public bool StencilTwoSidedOperation
        {
            get
            {
                return _stencilTwoSidedOperation;
            }
            set
            {
                _stencilTwoSidedOperation = value;
            }
        }

        /// <summary>
        /// Get the number of inputs used.
        /// </summary>
        public int NumInputs
        {
            get
            {
                int count = 0;
                for ( int x = 0; x < _inputs.Length; x++ )
                {
                    if ( !string.IsNullOrEmpty( _inputs[ x ].Name ) )
                    {
                        count = x + 1;
                    }
                }
                return count;
            }
        }

        /// <summary>
        ///  Get parent object
        /// </summary>
        public CompositionTargetPass Parent
        {
            get
            {
                return _parent;
            }
        }

        /// <summary>
        /// Returns true if camera frustum far corners are provided in the quad.
        /// </summary>
        public bool QuadFarCorners
        {
            get
            {
                return _quadFarCorners;
            }
        }

        /// <summary>
        /// Returns true if the far corners provided in the quad are in view space
        /// </summary>
        public bool QuadFarCornersViewSpace
        {
            get
            {
                return _quadFarCornersViewSpace;
            }
        }

        /// <summary>
        /// Get's or set's the type name of this custom composition pass.
        /// </summary>
        /// <note>
        /// applies when PassType is RENDERCUSTOM
        /// </note>
        /// <see cref="CompositorManager.RegisterCustomCompositionPass"/>
        public string CustomType
        {
            get
            {
                return _customType;
            }
            set
            {
                _customType = value;
            }
        }

        /// <summary>
        /// Determine if this target pass is supported on the current rendering device.
        /// </summary>
        public bool IsSupported
        {
            get
            {
                // A pass is supported if material referenced have a supported technique
                if ( _type == CompositorPassType.RenderQuad )
                {
                    if ( _material == null )
                    {
                        return false;
                    }

                    _material.Compile();
                    if ( _material.SupportedTechniques.Count == 0 )
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parent"></param>
        public CompositionPass( CompositionTargetPass parent )
        {
            _parent = parent;
            _type = CompositorPassType.RenderQuad;
            _identifier = 0;
            _firstRenderQueue = RenderQueueGroupID.Background;
            _lastRenderQueue = RenderQueueGroupID.SkiesLate;
            _materialSchemeName = string.Empty;
            _clearBuffers = FrameBufferType.Color | FrameBufferType.Depth;
            _clearColor = new ColorEx( 0, 0, 0, 0 );
            _clearDepth = 1.0f;
            _clearStencil = 0;
            _stencilCheck = false;
            _stencilFunc = CompareFunction.AlwaysPass;
            _stencilRefValue = 0;
            _stencilMask = 0x7FFFFFFF;
            _stencilFailOp = StencilOperation.Keep;
            _stencilDepthFailOp = StencilOperation.Keep;
            _stencilPassOp = StencilOperation.Keep;
            _stencilTwoSidedOperation = false;
            _quadCornerModified = false;
            _quadLeft = -1;
            _quadTop = 1;
            _quadRight = 1;
            _quadBottom = -1;
            _quadFarCorners = false;
            _quadFarCornersViewSpace = false;
        }

        /// <summary>
        /// Set the material used by this pass
        /// </summary>
        /// <param name="name">name of the material to set</param>
        /// <note>
        /// applies when PassType is RENDERQUAD
        /// </note>
        public void SetMaterialName( string name )
        {
            _material = (Material)MaterialManager.Instance.GetByName( name );
        }

        /// <summary>
        /// Set an input local texture. An empty string clears the input.
        /// </summary>
        /// <param name="id">Input to set. Must be in 0..Config.MaxTextureLayers-1</param>
        public void SetInput( int id )
        {
            SetInput( id, string.Empty, 0 );
        }

        /// <summary>
        /// Set an input local texture. An empty string clears the input.
        /// </summary>
        /// <param name="id">Input to set. Must be in 0..Config.MaxTextureLayers-1</param>
        /// <param name="input">Which texture to bind to this input. An empty string clears the input.</param>
        public void SetInput( int id, string input )
        {
            SetInput( id, input, 0 );
        }

        /// <summary>
        /// Set an input local texture. An empty string clears the input.
        /// </summary>
        /// <param name="id">Input to set. Must be in 0..Config.MaxTextureLayers-1</param>
        /// <param name="input">Which texture to bind to this input. An empty string clears the input.</param>
        /// <param name="mrtIndex">mrtIndex Which surface of an MRT to retrieve</param>
        public void SetInput( int id, string input, int mrtIndex )
        {
            Debug.Assert( id < _inputs.Length );
            _inputs[ id ] = new InputTexture( input, mrtIndex );
        }

        /// <summary>
        ///  Get the value of an input.
        /// </summary>
        /// <param name="id">Input to get. Must be in 0..Config.MaxTextureLayers-1.</param>
        /// <returns></returns>
        public InputTexture GetInput( int id )
        {
            Debug.Assert( id < _inputs.Length );
            return _inputs[ id ];
        }

        /// <summary>
        /// Clear all inputs.
        /// </summary>
        public void ClearAllInputs()
        {
            for ( int i = 0; i < _inputs.Length; i++ )
            {
                _inputs[ i ].Name = string.Empty;
            }
        }

        /// <summary>
        /// Set quad normalised positions [-1;1]x[-1;1]
        /// </summary>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <param name="right"></param>
        /// <param name="bottom"></param>
        public void SetQuadCorners( float left, float top, float right, float bottom )
        {
            _quadCornerModified = true;
            _quadLeft = left;
            _quadRight = right;
            _quadTop = top;
            _quadBottom = bottom;
        }

        /// <summary>
        /// Get quad normalised positions [-1;1]x[-1;1]
        /// </summary>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <param name="right"></param>
        /// <param name="bottom"></param>
        /// <returns></returns>
        public bool GetQuadCorners( out float left, out float top, out float right, out float bottom )
        {
            left = _quadLeft;
            top = _quadTop;
            right = _quadRight;
            bottom = _quadBottom;
            return _quadCornerModified;
        }

        /// <summary>
        /// Sets the use of camera frustum far corners provided in the quad's normals
        /// </summary>
        /// <param name="farCorners"></param>
        /// <param name="farCornersViewSpace"></param>
        public void SetQuadFarCorners( bool farCorners, bool farCornersViewSpace )
        {
            _quadFarCorners = farCorners;
            _quadFarCornersViewSpace = farCornersViewSpace;
        }
    }
}