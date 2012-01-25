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
using SlimDX.Direct3D9;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9
{
	/// <summary>
	///	Helper class for dealing with D3D Devices.
	/// </summary>
	public class Driver : DisposableObject
	{
		#region Constructors

        /// <summary>
        /// Default constructor
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public Driver()
            : base()
        {
        }

        [OgreVersion( 1, 7, 2 )]
	    public Driver( int adapterNumber, Capabilities deviceCaps,
                AdapterDetails adapterIdentifier, DisplayMode desktopDisplayMode)
            : base()
	    {
            _adapterNumber = adapterNumber;
            _d3D9DeviceCaps = deviceCaps;
		    _adapterIdentifier	= adapterIdentifier;
		    _desktopDisplayMode = desktopDisplayMode;
		    _videoModeList		= null;			
	    }

        /// <summary>
        /// Copy constructor
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public Driver( Driver ob )
            : base()
        {
            _adapterNumber = ob._adapterNumber;
            _d3D9DeviceCaps = ob._d3D9DeviceCaps;
            _adapterIdentifier = ob._adapterIdentifier;
            _desktopDisplayMode = ob._desktopDisplayMode;
            _videoModeList = null;	
        }

		#endregion Constructors

        #region dispose

        [OgreVersion( 1, 7, 2, "~D3D9Driver" )]
        protected override void dispose( bool disposeManagedResources )
        {
            if ( !this.IsDisposed && disposeManagedResources )
                _videoModeList.SafeDispose();

            base.dispose( disposeManagedResources );
        }

        #endregion dispose

        #region Properties

        #region DriverName

        [OgreVersion(1, 7, 2790)]
		public string DriverName
		{
			get
			{
				return _adapterIdentifier.DriverName;
			}
		}
		#endregion Name Property

		#region DriverDescription

        [OgreVersion( 1, 7, 2 )]
		public string DriverDescription
		{
			get
			{
                return string.Format( "Monitor-{0}-{1}", _adapterNumber + 1, _adapterIdentifier.Description );
			}
		}
		#endregion Description Property

		#region AdapterNumber Property

        [OgreVersion(1, 7, 2790)]
		private readonly int _adapterNumber;

        /// <summary>
        /// Get the adapter number
        /// </summary>
        [OgreVersion(1, 7, 2790)]
		public int AdapterNumber
		{
			get
			{
                return _adapterNumber;
			}
		}
		#endregion AdapterNumber Property

		#region AdapterIdentifier Property

        [OgreVersion(1, 7, 2790)]
        private readonly AdapterDetails _adapterIdentifier;

        [OgreVersion(1, 7, 2790)]
		public AdapterDetails AdapterIdentifier
		{
			get
			{
				return _adapterIdentifier;
			}
		}

		#endregion AdapterIdentifier Property

		#region DesktopMode Property

        [OgreVersion(1, 7, 2790)]
		private readonly DisplayMode _desktopDisplayMode;

        [OgreVersion(1, 7, 2790)]
		public DisplayMode DesktopMode
		{
			get
			{
				return _desktopDisplayMode;
			}
		}

		#endregion DesktopMode Property

		#region VideoModes Property

        [OgreVersion(1, 7, 2790)]
		private VideoModeCollection _videoModeList;
		
        [OgreVersion(1, 7, 2790)]
		public VideoModeCollection VideoModeList
		{
			get
			{
                if (_videoModeList == null)
                    _videoModeList = new VideoModeCollection( this );
				
                return _videoModeList;
			}
		}

		#endregion VideoModes Property

        #region D3D9DeviceCaps

        [OgreVersion(1, 7, 2790)]
	    private readonly Capabilities _d3D9DeviceCaps;

        /// <summary>
        /// Get device capabilities
        /// </summary>
        [OgreVersion(1, 7, 2790)]
	    public Capabilities D3D9DeviceCaps
	    {
	        get
	        {
		        return _d3D9DeviceCaps;
	        }
        }

        #endregion

        #endregion Properties
    };
}