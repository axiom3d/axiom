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

using Axiom.Core;
using D3D = SharpDX.Direct3D9;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9
{
	/// <summary>
	/// Summary description for D3DVideoMode.
	/// </summary>
	public class D3D9VideoMode : DisposableObject
	{
		#region Member variables

		private D3D.DisplayMode displayMode;
		private int modeNum;
		private static int modeCount = 0;

		#endregion Member variables

		#region Constructors

		/// <summary>
		///	Default constructor.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public D3D9VideoMode()
		{
			modeNum = ++modeCount;
			displayMode = new D3D.DisplayMode();
		}

		/// <summary>
		///	Accepts a existing D3DVideoMode object.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public D3D9VideoMode( D3D9VideoMode videoMode )
		{
			modeNum = ++modeCount;
			displayMode = videoMode.displayMode;
		}

		/// <summary>
		///	Accepts a existing Direct3D.DisplayMode object.
		/// </summary>
		public D3D9VideoMode( D3D.DisplayMode videoMode )
		{
			modeNum = ++modeCount;
			displayMode = videoMode;
		}

		#endregion Constructors

		[OgreVersion( 1, 7, 2, "~D3D9VideoMode" )]
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					modeCount--;
				}
			}
			base.dispose( disposeManagedResources );
		}

		/// <summary>
		///	Returns a string representation of this video mode.
		/// </summary>
		[OgreVersion( 1, 7, 2, "getDescription" )]
		public override string ToString()
		{
			return string.Format( "{0} x {1} @ {2}-bit color", displayMode.Width, displayMode.Height, ColorDepth );
		}

		#region Properties

		/// <summary>
		///	Width of this video mode.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public int Width
		{
			get
			{
				return displayMode.Width;
			}
		}

		/// <summary>
		///	Height of this video mode.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public int Height
		{
			get
			{
				return displayMode.Height;
			}
		}

		/// <summary>
		///	Format of this video mode.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public D3D.Format Format
		{
			get
			{
				return displayMode.Format;
			}
		}

		/// <summary>
		///	Refresh rate of this video mode.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public int RefreshRate
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return displayMode.RefreshRate;
			}

			[OgreVersion( 1, 7, 2, "increaseRefreshRate" )]
			set
			{
				displayMode.RefreshRate = value;
			}
		}

		/// <summary>
		///	Color depth of this video mode.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public int ColorDepth
		{
			get
			{
				var colorDepth = 16;

				if ( displayMode.Format == D3D.Format.X8R8G8B8 || displayMode.Format == D3D.Format.A8R8G8B8 ||
				     displayMode.Format == D3D.Format.R8G8B8 )
				{
					colorDepth = 32;
				}

				return colorDepth;
			}
		}

		/// <summary>
		///	Gets the Direct3D.DisplayMode object associated with this video mode.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public D3D.DisplayMode DisplayMode
		{
			get
			{
				return displayMode;
			}
		}

		/// <summary>
		/// Returns a string representation of this video mode.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public string Description
		{
			get
			{
				return ToString();
			}
		}

		#endregion Properties
	};
}