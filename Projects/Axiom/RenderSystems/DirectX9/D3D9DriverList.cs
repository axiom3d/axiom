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

using System;
using System.Collections.Generic;
using System.Linq;

using Axiom.Core;

using SharpDX.Direct3D9;

using D3D9 = SharpDX.Direct3D9;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9
{
	public class D3D9DriverList : List<D3D9Driver>, IDisposable
	{
		[OgreVersion( 1, 7, 2 )]
		public D3D9DriverList()
		{
			Enumerate();
		}

		[OgreVersion( 1, 7, 2, "D3D9DriverList::item( const String &name )" )]
		public D3D9Driver this[ string description ]
		{
			get
			{
				return this.FirstOrDefault( x => x.DriverDescription == description );
			}
		}

		#region IDisposable Members

		[OgreVersion( 1, 7, 2, "~D3D9DriverList" )]
		public void Dispose()
		{
			foreach ( D3D9Driver it in this )
			{
				it.SafeDispose();
			}

			Clear();
			GC.SuppressFinalize( this );
		}

		#endregion

		~D3D9DriverList()
		{
			Dispose();
		}

		[OgreVersion( 1, 7, 2 )]
		public bool Enumerate()
		{
			Direct3D lpD3D9 = D3D9RenderSystem.Direct3D9;

			LogManager.Instance.Write( "D3D9: Driver Detection Starts" );

			for ( int iAdapter = 0; iAdapter < lpD3D9.AdapterCount; ++iAdapter )
			{
				AdapterDetails adapterIdentifier = lpD3D9.GetAdapterIdentifier( iAdapter );
				DisplayMode d3ddm = lpD3D9.GetAdapterDisplayMode( iAdapter );
				Capabilities d3dcaps9 = lpD3D9.GetDeviceCaps( iAdapter, DeviceType.Hardware );

				Add( new D3D9Driver( iAdapter, d3dcaps9, adapterIdentifier, d3ddm ) );
			}

			LogManager.Instance.Write( "D3D9: Driver Detection Ends" );

			return true;
		}
	};
}
