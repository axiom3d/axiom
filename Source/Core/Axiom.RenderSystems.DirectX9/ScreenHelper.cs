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
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9.Helpers
{
	public static class ScreenHelper
	{
		/// <summary>
		/// Workaround method to get the right Screen associated
		/// with the input screen/monitor handle.
		/// </summary>
		[AxiomHelper( 0, 9 )]
		public static Screen FromHandle( IntPtr handle )
		{
			var s = Screen.AllScreens.Where( x => x.GetHashCode() == (int)handle ).FirstOrDefault();
			if ( s == null )
			{
				s = Screen.FromHandle( handle );
			}

			return s;
		}

		/// <summary>
		/// Returns the handle of a Screen from a point
		/// </summary>
		[AxiomHelper( 0, 9 )]
		public static IntPtr GetHandle( Point p )
		{
			var s = Screen.FromPoint( p );
			return new IntPtr( s.GetHashCode() );
		}

		/// <summary>
		/// Returns the handle of a Screen from a window handle
		/// </summary>
		[AxiomHelper( 0, 9 )]
		public static IntPtr GetHandle( IntPtr windowHandle )
		{
			var s = ScreenHelper.FromHandle( windowHandle );
			return new IntPtr( s.GetHashCode() );
		}
	};
}