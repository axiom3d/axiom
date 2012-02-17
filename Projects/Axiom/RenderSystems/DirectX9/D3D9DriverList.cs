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
using D3D9 = SlimDX.Direct3D9;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9
{
    public class D3D9DriverList : List<D3D9Driver>, IDisposable
    {
        [OgreVersion( 1, 7, 2, "D3D9DriverList::item( const String &name )" )]
        public D3D9Driver this[ string description ]
        {
            get
            {
                return this.FirstOrDefault( x => x.DriverDescription == description );
            }
        }

        [OgreVersion( 1, 7, 2 )]
        public D3D9DriverList()
            : base()
        {
            Enumerate();
        }

        ~D3D9DriverList()
        {
            this.Dispose();
        }

        [OgreVersion( 1, 7, 2, "~D3D9DriverList" )]
        public void Dispose()
        {
            foreach ( var it in this )
                it.SafeDispose();

            this.Clear();
            GC.SuppressFinalize( this );
        }

        [OgreVersion( 1, 7, 2 )]
        public bool Enumerate()
        {
            var lpD3D9 = D3D9RenderSystem.Direct3D9;

            LogManager.Instance.Write( "D3D9: Driver Detection Starts" );

            for ( var iAdapter = 0; iAdapter < lpD3D9.AdapterCount; ++iAdapter )
            {
                var adapterIdentifier = lpD3D9.GetAdapterIdentifier( iAdapter );
                var d3ddm = lpD3D9.GetAdapterDisplayMode( iAdapter );
                var d3dcaps9 = lpD3D9.GetDeviceCaps( iAdapter, D3D9.DeviceType.Hardware );

                this.Add( new D3D9Driver( iAdapter, d3dcaps9, adapterIdentifier, d3ddm ) );
            }

            LogManager.Instance.Write( "D3D9: Driver Detection Ends" );

            return true;
        }
    };
}
