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
	/// <summary>
	/// Summary description for VideoModeCollection.
	/// </summary>
	public class D3D9VideoModeList : List<D3D9VideoMode>, IDisposable
	{
		private D3D9Driver _mpDriver;

		[OgreVersion( 1, 7, 2 )]
		public D3D9VideoModeList( D3D9Driver pDriver )
		{
			if ( pDriver == null )
			{
				throw new AxiomException( "pDriver parameter is NULL" );
			}

			this._mpDriver = pDriver;
			Enumerate();
		}

		[OgreVersion( 1, 7, 2, "D3D9VideoModeList::item( const String &name )" )]
		public D3D9VideoMode this[ string description ]
		{
			get
			{
				return this.FirstOrDefault( x => x.Description == description );
			}
		}

		#region IDisposable Members

		[OgreVersion( 1, 7, 2 )]
		public void Dispose()
		{
			this._mpDriver = null;
			Clear();

			GC.SuppressFinalize( this );
		}

		#endregion

		~D3D9VideoModeList()
		{
			Dispose();
		}

		[OgreVersion( 1, 7, 2 )]
		public bool Enumerate()
		{
			_enumerateByFormat( Format.R5G6B5 );
			_enumerateByFormat( Format.X8R8G8B8 );

			return true;
		}

		[AxiomHelper( 0, 9 )]
		private void _enumerateByFormat( Format format )
		{
			Direct3D pD3D = D3D9RenderSystem.Direct3D9;
			int adapter = this._mpDriver.AdapterNumber;

			for ( int iMode = 0; iMode < pD3D.GetAdapterModeCount( adapter, format ); iMode++ )
			{
				DisplayMode displayMode = pD3D.EnumAdapterModes( adapter, format, iMode );

				// Filter out low-resolutions
				if ( displayMode.Width < 640 || displayMode.Height < 400 )
				{
					continue;
				}

				// Check to see if it is already in the list (to filter out refresh rates)
				bool found = false;
				for ( int it = 0; it < Count; it++ )
				{
					DisplayMode oldDisp = this[ it ].DisplayMode;
					if ( oldDisp.Width == displayMode.Width && oldDisp.Height == displayMode.Height && oldDisp.Format == displayMode.Format )
					{
						// Check refresh rate and favour higher if poss
						if ( oldDisp.RefreshRate < displayMode.RefreshRate )
						{
							this[ it ].RefreshRate = displayMode.RefreshRate;
						}

						found = true;
						break;
					}
				}

				if ( !found )
				{
					Add( new D3D9VideoMode( displayMode ) );
				}
			}
		}
	};
}
