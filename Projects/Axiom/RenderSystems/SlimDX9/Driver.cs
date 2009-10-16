#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006 Axiom Project Team

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
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
//     <id value="$Id: Driver.cs 884 2006-09-14 06:32:07Z borrillis $"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;

using DX = SlimDX;
using D3D = SlimDX.Direct3D9;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.SlimDX9
{
    /// <summary>
    ///		Helper class for dealing with D3D Drivers.
    /// </summary>
    public class Driver
    {
        #region Constructors

        /// <summary>
        ///		Default constructor.
        /// </summary>
        public Driver(D3D.AdapterInformation adapterInfo)
        {
            this._desktopMode = adapterInfo.CurrentDisplayMode;
            this._name = adapterInfo.Details.DriverName;
            this._description = adapterInfo.Details.Description;
            this._adapterNum = adapterInfo.Adapter;
            this._adapterIdentifier = adapterInfo.Details.DeviceIdentifier;

            _videoModeList = new VideoModeCollection();
        }

        #endregion

        #region Properties

        /// <summary>
        /// 
        /// </summary>
        public string Name
        {
            get { return _name; }
        }
        private string _name;

        /// <summary>
        /// 
        /// </summary>
        public string Description
        {
            get { return _description; }
        }
        private string _description;

        /// <summary>
        /// 
        /// </summary>
        public int AdapterNumber
        {
            get { return _adapterNum; }
        }
        private int _adapterNum;


        /// <summary>
        /// 
        /// </summary>
        public Guid AdapterIdentifier
        {
            get { return _adapterIdentifier; }
        }

        private Guid _adapterIdentifier;
        /// <summary>
        ///		
        /// </summary>
        public D3D.DisplayMode DesktopMode
        {
            get
            {
                return _desktopMode;
            }
        }
        private D3D.DisplayMode _desktopMode;

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
        private VideoModeCollection _videoModeList;

        public D3D.Device D3DDevice
        {
            get { return _device; }
            set { _device = value; }
        }

        private D3D.Device _device;

        public D3D.Direct3D Direct3D
        {
            get
            {
                return _direct3D;
            }
            set
            {
                _direct3D = value;
            }
        }

        private D3D.Direct3D _direct3D;

        #endregion
    }
}
