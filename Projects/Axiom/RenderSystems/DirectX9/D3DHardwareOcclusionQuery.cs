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
using Axiom.Graphics;

using DX = SlimDX;
using D3D = SlimDX.Direct3D9;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9
{
	/// <summary>
	///		Direct3D implementation of a hardware occlusion query.
	/// </summary>
	// Original Author: Lee Sandberg
	public class D3DHardwareOcclusionQuery : HardwareOcclusionQuery
	{
		#region Fields

		/// <summary>
		///		Reference to the current Direct3D device object.
		/// </summary>
		private D3D.Device device;

		/// <summary>
		///		Reference to the query object being used.
		/// </summary>
		private D3D.Query query;

		/// <summary>
		///		Flag that indicates whether hardware queries are supported
		/// </summary>
		private bool isSupported;

		private bool isQueryResultStillOutstanding;

		#endregion Fields

		#region Constructor

		/// <summary>
		///		Default constructor.
		/// </summary>
		/// <param name="device">Reference to a Direct3D device.</param>
		public D3DHardwareOcclusionQuery( D3D.Device device )
		{
			this.device = device;

			isQueryResultStillOutstanding = true;

			// check if queries are supported
			isSupported = Root.Instance.RenderSystem.HardwareCapabilities.HasCapability( Capabilities.HardwareOcculusion );

			if( isSupported )
			{
				// attempt to create an occlusion query
				query = new D3D.Query( device, D3D.QueryType.Occlusion );
			}
		}

		#endregion Constructor

		#region HardwareOcclusionQuery Members

		/// <summary>
		/// Starts the hardware occlusion query
		/// </summary>
		public override void Begin()
		{
			// proceed if supported, or silently fail otherwise
			if( isSupported )
			{
				query.Issue( D3D.Issue.Begin );
			}
			isQueryResultStillOutstanding = true;
		}

		/// <summary>
		/// Pulls the hardware occlusion query.
		/// </summary>
		/// <remarks>
		/// Waits until the query result is available; use <see cref="HardwareOcclusionQuery.IsStillOutstanding"/>
		/// if just want to test if the result is available.
		/// </remarks>
		/// <returns>the resulting number of fragments.</returns>
		public override int PullResults()
		{
			if( isQueryResultStillOutstanding )
			{
				// default to returning a high count.  will be set otherwise if the query runs
				LastFragmentCount = 100000;

				if( isSupported )
				{
					while( !query.CheckStatus( true ) )
					{
						;
					}
					LastFragmentCount = query.GetData<int>( true );
				}
				isQueryResultStillOutstanding = false;
			}
			return LastFragmentCount;
		}

		/// <summary>
		/// Ends the hardware occlusion test
		/// </summary>
		public override void End()
		{
			// proceed if supported, or silently fail otherwise
			if( isSupported )
			{
				query.Issue( D3D.Issue.End );
			}
		}

		/// <summary>
		/// Lets you know when query is done, or still be processed by the Hardware
		/// </summary>
		/// <returns>true if query isn't finished.</returns>
		public override bool IsStillOutstanding()
		{
			if( !isQueryResultStillOutstanding )
			{
				return false;
			}

			return query.CheckStatus( true );
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		/// <filterpriority>2</filterpriority>
		protected override void dispose( bool disposeManagedResources )
		{
			query.Dispose();
			base.dispose( disposeManagedResources );
		}

		#endregion HardwareOcclusionQuery Members
	}
}
