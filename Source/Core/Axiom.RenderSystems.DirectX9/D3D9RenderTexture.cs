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
using D3D9 = SharpDX.Direct3D9;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9
{
	/// <summary>
	/// RenderTexture implementation for D3D9
	/// </summary>
	public class D3D9RenderTexture : RenderTexture
	{
		[OgreVersion( 1, 7, 2 )]
		public override bool RequiresTextureFlipping
		{
			get
			{
				return false;
			}
		}

		[OgreVersion( 1, 7, 2 )]
		public D3D9RenderTexture( string name, D3D9HardwarePixelBuffer buffer, bool writeGamma, int fsaa )
			: base( buffer, 0 )
		{
			this.name = name;
			hwGamma = writeGamma;
			this.fsaa = fsaa;
		}

		[OgreVersion( 1, 7, 2790 )]
		public override void Update( bool swapBuffers )
		{
			var deviceManager = D3D9RenderSystem.DeviceManager;
			var currRenderWindowDevice = deviceManager.ActiveRenderTargetDevice;

			if ( currRenderWindowDevice != null )
			{
				if ( currRenderWindowDevice.IsDeviceLost == false )
				{
					base.Update( swapBuffers );
				}
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

		[OgreVersion( 1, 7, 2 )]
		public override object this[ string attribute ]
		{
			get
			{
				switch ( attribute.ToUpper() )
				{
					case "DDBACKBUFFER":
						var surface = new D3D9.Surface[Config.MaxMultipleRenderTargets];
						if ( fsaa > 0 )
						{
							surface[ 0 ] = ( (D3D9HardwarePixelBuffer)pixelBuffer ).GetFSAASurface( D3D9RenderSystem.ActiveD3D9Device );
						}
						else
						{
							surface[ 0 ] = ( (D3D9HardwarePixelBuffer)pixelBuffer ).GetSurface( D3D9RenderSystem.ActiveD3D9Device );
						}

						return surface;

					case "HWND":
						return null;

					case "BUFFER":
						return (HardwarePixelBuffer)pixelBuffer;

					default:
						return null;
				}
			}
		}

		/// <summary>
		/// Override needed to deal with FSAA
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public override void SwapBuffers( bool waitForVSync )
		{
			// Only needed if we have to blit from AA surface
			if ( fsaa > 0 )
			{
				var deviceManager = D3D9RenderSystem.DeviceManager;
				var buf = (D3D9HardwarePixelBuffer)( pixelBuffer );

				foreach ( var device in deviceManager )
				{
					if ( device.IsDeviceLost == false )
					{
						var d3d9Device = device.D3DDevice;
						var res = d3d9Device.StretchRectangle( buf.GetFSAASurface( d3d9Device ), buf.GetSurface( d3d9Device ),
						                                       D3D9.TextureFilter.None );

						if ( res.Failure )
						{
							throw new AxiomException( "Unable to copy AA buffer to final buffer: {0}", res.ToString() );
						}
					}
				}
			}
		}
	};
}