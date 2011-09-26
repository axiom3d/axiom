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

#endregion

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id:"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Windows.Graphics;
using Microsoft.Xna.Framework.Graphics;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.Xna
{
    /// <summary>
    ///		Helper class for dealing with XNA Drivers.
    /// </summary>
    public class Driver
    {
        #region Constructors

        /// <summary>
        ///		Default constructor.
        /// </summary>
        public Driver( GraphicsAdapter adapterDetails )
        {
#if SILVERLIGHT
            _desktopMode = new DisplayMode
                           {
                               Width = (int)XnaRenderWindow.DrawingSurface.Width,
                               Height = (int)XnaRenderWindow.DrawingSurface.Height,
                               Format = SurfaceFormat.Color
                           };
            _name = "Silverlight XNA";
            _description = "Silverlight XNA Device";
            _adapterNum = 0;
            _adapterIdentifier = new Guid(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            _adapter = adapterDetails;

            _videoModeList = new VideoModeCollection
                             {
                                 new VideoMode( _desktopMode )
                             };
#else
            _desktopMode = adapterDetails.CurrentDisplayMode;
            _name = adapterDetails.DeviceName;
            _description = adapterDetails.Description;
            _adapterNum = adapterDetails.DeviceId;
            _adapterIdentifier = new Guid(adapterDetails.VendorId, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            _adapter = adapterDetails;

            _videoModeList = new VideoModeCollection();
#endif
        }

        #endregion Constructors

        #region Properties

        #region Name Property

        private readonly string _name;

        /// <summary>
        /// 
        /// </summary>
        public string Name
        {
            get
            {
                return _name;
            }
        }

        #endregion Name Property

        #region Description Property

        private readonly string _description;

        /// <summary>
        /// 
        /// </summary>
        public string Description
        {
            get
            {
                return _description;
            }
        }

        #endregion Description Property

        #region Adapter Property

        private readonly GraphicsAdapter _adapter;

        /// <summary>
        /// 
        /// </summary>
        public GraphicsAdapter Adapter
        {
            get
            {
                return _adapter;
            }
        }

        #endregion AdapterNumber Property

        #region AdapterNumber Property

        private readonly int _adapterNum;

        /// <summary>
        /// 
        /// </summary>
        public int AdapterNumber
        {
            get
            {
                return _adapterNum;
            }
        }

        #endregion AdapterNumber Property

        #region AdapterIdentifier Property

        private Guid _adapterIdentifier;

        /// <summary>
        /// 
        /// </summary>
        public Guid AdapterIdentifier
        {
            get
            {
                return _adapterIdentifier;
            }
        }

        #endregion AdapterIdentifier Property

        #region DesktopMode Property

        private readonly DisplayMode _desktopMode;

        /// <summary>
        ///		
        /// </summary>
        public DisplayMode DesktopMode
        {
            get
            {
                return _desktopMode;
            }
        }

        #endregion DesktopMode Property

        #region VideoModes Property

        private readonly VideoModeCollection _videoModeList;

        /// <summary>
        ///		
        /// </summary>
        public VideoModeCollection VideoModes
        {
            get
            {
                return _videoModeList;
            }
        }

        #endregion VideoModes Property

        #region XnaDevice Property

        /// <summary>
        ///		
        /// </summary>
#if SILVERLIGHT
        public GraphicsDevice XnaDevice
        {
            get { return GraphicsDeviceManager.Current.GraphicsDevice; }
            set { }
        }
#else
        public GraphicsDevice XnaDevice { get; set; }
#endif

        #endregion XnaDevice Property

        #endregion Properties
    }
}