#region MIT/X11 License
//Copyright © 2003-2012 Axiom 3D Rendering Engine Project
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.
#endregion License

#region SVN Version Information
// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using Axiom.Configuration;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Media;
using Axiom.Utilities;
using D3D = SlimDX.Direct3D9;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9
{
	public sealed class D3D9MultiRenderTarget : MultiRenderTarget
	{
		#region Fields and Properties

		private D3D9HardwarePixelBuffer[] _renderTargets = new D3D9HardwarePixelBuffer[ Config.MaxMultipleRenderTargets ];

		#endregion Fields and Properties

		#region Construction and Destruction

        [OgreVersion( 1, 7, 2 )]
		public D3D9MultiRenderTarget( string name )
			: base( name )
		{
		}

		#endregion Construction and Destruction

		#region Methods

		/// <summary>
		/// Bind a surface to a certain attachment point.
		/// </summary>
		/// <param name="attachment">0 .. capabilities.MultiRenderTargetCount-1</param>
		/// <param name="target">RenderTexture to bind.</param>
		/// <remarks>
		/// It does not bind the surface and fails with an exception (ERR_INVALIDPARAMS) if:
		/// - Not all bound surfaces have the same size
		/// - Not all bound surfaces have the same internal format
		/// </remarks>
        [OgreVersion( 1, 7, 2 )]
		protected override void BindSurfaceImpl( int attachment, RenderTexture target )
		{
			Contract.Requires( attachment < Config.MaxMultipleRenderTargets );

			// Get buffer and surface to bind to
			var buffer = (D3D9HardwarePixelBuffer)( target[ "BUFFER" ] );
			Proclaim.NotNull( buffer );

			// Find first non null target
			int y;
            for ( y = 0; y < Config.MaxMultipleRenderTargets && _renderTargets[ y ] == null; ++y ) ;

			if ( y != Config.MaxMultipleRenderTargets )
			{
                // If there is another target bound, compare sizes
				if ( _renderTargets[ y ].Width != buffer.Width ||
                    _renderTargets[ y ].Height != buffer.Height )
				{
					throw new AxiomException( "MultiRenderTarget surfaces are not the same size." );
				}

                if ( !Root.Instance.RenderSystem.Capabilities.HasCapability( Capabilities.MRTDifferentBitDepths )
                    && ( PixelUtil.GetNumElemBits( _renderTargets[ y ].Format ) != PixelUtil.GetNumElemBits( buffer.Format ) ) )
                {
                    throw new AxiomException( "MultiRenderTarget surfaces are not of same bit depth and hardware requires it" );
                }
			}

			_renderTargets[ attachment ] = buffer;
			_checkAndUpdate();
		}

		/// <summary>
		/// Unbind Attachment
		/// </summary>
        [OgreVersion( 1, 7, 2 )]
		protected override void UnbindSurfaceImpl( int attachment )
		{
			Contract.Requires( attachment < Config.MaxMultipleRenderTargets );
			_renderTargets[ attachment ].SafeDispose();
            _renderTargets[ attachment ] = null;
			_checkAndUpdate();
		}

        /// <summary>
        /// Check surfaces and update RenderTarget extent
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
		private void _checkAndUpdate()
		{
			if ( _renderTargets[ 0 ] != null )
			{
				this.width = _renderTargets[ 0 ].Width;
				this.height = _renderTargets[ 0 ].Height;
			}
			else
			{
				this.width = 0;
				this.height = 0;
			}
		}

		#endregion Methods

		#region RenderTarget Implementation

        /// <see cref="Axiom.Graphics.RenderTarget.Update(bool)"/>
        [OgreVersion(1, 7, 2790)]
        public override void Update( bool swapBuffers )
        {
            var deviceManager = D3D9RenderSystem.DeviceManager;
            var currRenderWindowDevice = deviceManager.ActiveRenderTargetDevice;

            if ( currRenderWindowDevice != null )
            {
                if ( currRenderWindowDevice.IsDeviceLost == false )
                    base.Update( swapBuffers );
            }
            else
            {
                foreach ( var device in deviceManager )
                {
                    if ( device.IsDeviceLost == false )
                    {
                        deviceManager.ActiveRenderTargetDevice = device;
                        base.Update( swapBuffers );
                        deviceManager.ActiveRenderTargetDevice = null;
                    }
                }
            }
        }

	    public override object this[ string attribute ]
		{
            [OgreVersion( 1, 7, 2 )]
			get
			{
				if ( attribute.ToUpper() == "DDBACKBUFFER" )
				{
					var surfaces = new D3D.Surface[ Config.MaxMultipleRenderTargets ];
					// Transfer surfaces
					for ( var x = 0; x < Config.MaxMultipleRenderTargets; ++x )
					{
						if ( _renderTargets[ x ] != null )
                            surfaces[ x ] = _renderTargets[ x ].GetSurface( D3D9RenderSystem.ActiveD3D9Device );
					}
					return surfaces;
				}

				return null;
			}
		}

        [OgreVersion(1, 7, 2790)]
		public override bool RequiresTextureFlipping
		{
			get
			{
				return false;
			}
		}

		#endregion RenderTarget Implementation
	};
}