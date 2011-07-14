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
using System.Collections.Generic;
using System.Text;

using Axiom.Graphics;
using Axiom.Configuration;
using Axiom.Utilities;
using Axiom.Core;

using DX = SlimDX;
using D3D = SlimDX.Direct3D9;
using Axiom.Media;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9
{
	class D3DMultiRenderTarget : MultiRenderTarget
	{
		#region Fields and Properties

		protected D3DHardwarePixelBuffer[] _targets = new D3DHardwarePixelBuffer[ Config.MaxMultipleRenderTargets ];

		#endregion Fields and Properties

		#region Construction and Destruction

		public D3DMultiRenderTarget( string name )
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
		public override void BindSurface( int attachment, RenderTexture target )
		{
			Contract.Requires( attachment < Config.MaxMultipleRenderTargets );

			// Get buffer and surface to bind to
			D3DHardwarePixelBuffer buffer = (D3DHardwarePixelBuffer)( target[ "BUFFER" ] );
			Proclaim.NotNull( buffer );

			// Find first non null target
			int y;
			for ( y = 0; y < Config.MaxMultipleRenderTargets && this._targets[ y ] == null; y++ )
				;

			if ( y != Config.MaxMultipleRenderTargets )
			{
				if ( this._targets[ y ].Width != buffer.Width
					&& this._targets[ y ].Height != buffer.Height
					&& PixelUtil.GetNumElemBits( this._targets[ y ].Format ) != PixelUtil.GetNumElemBits( this._targets[ y ].Format ) )
				{
					throw new AxiomException( "MultiRenderTarget surfaces are not the same size or bit depth." );
				}
			}

			this._targets[ attachment ] = buffer;
			this.CheckAndUpdate();
		}

		/// <summary>
		/// Unbind Attachment
		/// </summary>
		/// <param name="attachment"></param>
		public override void UnbindSurface( int attachment )
		{
			Contract.Requires( attachment < Config.MaxMultipleRenderTargets );
			this._targets[ attachment ] = null;
			this.CheckAndUpdate();
		}

		private void CheckAndUpdate()
		{
			if ( this._targets[ 0 ] != null )
			{
				width = this._targets[ 0 ].Width;
				height = this._targets[ 0 ].Height;
			}
			else
			{
				width = 0;
				height = 0;
			}
		}

		#endregion Methods

		#region RenderTarget Implementation

        [OgreVersion(1, 7, 2790)]
        public override void Update(bool swapBuffers)
        {
            var deviceManager = D3DRenderSystem.DeviceManager;
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
                        ;
                    }
                }
            }
        }

	    public override object this[ string attribute ]
		{
			get
			{
				if ( attribute == "DDBACKBUFFER" )
				{
					var surfaces = new D3D.Surface[ Config.MaxMultipleRenderTargets ];
					// Transfer surfaces
					for ( var x = 0; x < Config.MaxMultipleRenderTargets; x++ )
					{
						if ( this._targets[ x ] != null )
							surfaces[ x ] = this._targets[ x ].Surface;
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
	}
}