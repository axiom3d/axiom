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
#endregion LGPL License

#region SVN Version Information
// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using Axiom.Core;
using SlimDX.Direct3D9;
using DX = SlimDX;
using D3D = SlimDX.Direct3D9;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9
{
	/// <summary>
	///		Helper class for dealing with D3D Devices.
	/// </summary>
	public class Driver
	{
		#region Constructors

	    public Driver( int adapterNumber, Capabilities deviceCaps,
                AdapterDetails adapterIdentifier, DisplayMode desktopDisplayMode)
	    {
            _adapterNumber = adapterNumber;
            _d3D9DeviceCaps = deviceCaps;
		    _adapterIdentifier	= adapterIdentifier;
		    _desktopDisplayMode = desktopDisplayMode;
		    _videoModeList		= null;			
	    }

		#endregion Constructors

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

		public string DriverDescription
		{
			get
			{
				return _adapterIdentifier.Description;
			}
		}
		#endregion Description Property

		#region AdapterNumber Property

        [OgreVersion(1, 7, 2790)]
		private readonly int _adapterNumber;

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
                    _videoModeList = new VideoModeCollection();
				return _videoModeList;
			}
		}

		#endregion VideoModes Property

        #region D3D9DeviceCaps

        [OgreVersion(1, 7, 2790)]
	    private readonly Capabilities _d3D9DeviceCaps;

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
    }
}