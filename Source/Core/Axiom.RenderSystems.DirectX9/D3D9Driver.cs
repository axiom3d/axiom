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
using D3D9 = SharpDX.Direct3D9;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9
{
    /// <summary>
    ///	Helper class for dealing with D3D Devices.
    /// </summary>
    public class D3D9Driver : DisposableObject
    {
        #region Constructors

        /// <summary>
        /// Default constructor
        /// </summary>
        [OgreVersion(1, 7, 2)]
        public D3D9Driver()
            : base()
        {
        }

        [OgreVersion(1, 7, 2)]
        public D3D9Driver(int adapterNumber, D3D9.Capabilities deviceCaps, D3D9.AdapterDetails adapterIdentifier,
                           D3D9.DisplayMode desktopDisplayMode)
            : base()
        {
            this._adapterNumber = adapterNumber;
            this._d3D9DeviceCaps = deviceCaps;
            this._adapterIdentifier = adapterIdentifier;
            this._desktopDisplayMode = desktopDisplayMode;
            this._videoModeList = null;
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        [OgreVersion(1, 7, 2)]
        public D3D9Driver(D3D9Driver ob)
            : base()
        {
            this._adapterNumber = ob._adapterNumber;
            this._d3D9DeviceCaps = ob._d3D9DeviceCaps;
            this._adapterIdentifier = ob._adapterIdentifier;
            this._desktopDisplayMode = ob._desktopDisplayMode;
            this._videoModeList = null;
        }

        #endregion Constructors

        #region dispose

        [OgreVersion(1, 7, 2, "~D3D9Driver")]
        protected override void dispose(bool disposeManagedResources)
        {
            if (!IsDisposed && disposeManagedResources)
            {
                this._videoModeList.SafeDispose();
            }

            base.dispose(disposeManagedResources);
        }

        #endregion dispose

        #region Properties

        #region DriverName

        [OgreVersion(1, 7, 2790)]
        public string DriverName
        {
            get
            {
                return this._adapterIdentifier.Driver;
            }
        }

        #endregion Name Property

        #region DriverDescription

        [OgreVersion(1, 7, 2)]
        public string DriverDescription
        {
            get
            {
                return string.Format("Monitor-{0}-{1}", this._adapterNumber + 1, this._adapterIdentifier.Description);
            }
        }

        #endregion Description Property

        #region AdapterNumber Property

        [OgreVersion(1, 7, 2790)] private readonly int _adapterNumber;

        /// <summary>
        /// Get the adapter number
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        public int AdapterNumber
        {
            get
            {
                return this._adapterNumber;
            }
        }

        #endregion AdapterNumber Property

        #region AdapterIdentifier Property

        [OgreVersion(1, 7, 2790)] private readonly D3D9.AdapterDetails _adapterIdentifier;

        [OgreVersion(1, 7, 2790)]
        public D3D9.AdapterDetails AdapterIdentifier
        {
            get
            {
                return this._adapterIdentifier;
            }
        }

        #endregion AdapterIdentifier Property

        #region DesktopMode Property

        [OgreVersion(1, 7, 2790)] private readonly D3D9.DisplayMode _desktopDisplayMode;

        [OgreVersion(1, 7, 2790)]
        public D3D9.DisplayMode DesktopMode
        {
            get
            {
                return this._desktopDisplayMode;
            }
        }

        #endregion DesktopMode Property

        #region VideoModes Property

        [OgreVersion(1, 7, 2790)] private D3D9VideoModeList _videoModeList;

        [OgreVersion(1, 7, 2790)]
        public D3D9VideoModeList VideoModeList
        {
            get
            {
                if (this._videoModeList == null)
                {
                    this._videoModeList = new D3D9VideoModeList(this);
                }

                return this._videoModeList;
            }
        }

        #endregion VideoModes Property

        #region D3D9DeviceCaps

        [OgreVersion(1, 7, 2790)] private readonly D3D9.Capabilities _d3D9DeviceCaps;

        /// <summary>
        /// Get device capabilities
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        public D3D9.Capabilities D3D9DeviceCaps
        {
            get
            {
                return this._d3D9DeviceCaps;
            }
        }

        #endregion D3D9DeviceCaps

        #endregion Properties
    };
}