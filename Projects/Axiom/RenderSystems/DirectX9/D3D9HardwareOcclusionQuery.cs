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

using System.Collections.Generic;

using Axiom.Core;
using Axiom.Graphics;

using SharpDX;
using SharpDX.Direct3D9;

using D3D9 = SharpDX.Direct3D9;
using DX = SharpDX;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9
{
	// If you use multiple rendering passes you can test only the first pass and all other passes don't have to be rendered 
	// if the first pass results has too few pixels visible.

	// Be sure to render all occluder first and whats out so the RenderQue don't switch places on 
	// the occluding objects and the tested objects because it thinks it's more effective..


	/// <summary>
	///	Direct3D implementation of a hardware occlusion query.
	/// </summary>
	/// <remarks>
	/// @author Lee Sandberg, email lee@abcmedia.se
	/// 
	/// Updated on 12/7/2004 by Chris McGuirk
	/// Updated on 4/8/2005 by Tuan Kuranes email: tuan.kuranes@free.fr
	/// </remarks>
	public class D3D9HardwareOcclusionQuery : HardwareOcclusionQuery, ID3D9Resource
	{
		#region Fields

		private readonly Dictionary<Device, Query> _mapDeviceToQuery = new Dictionary<Device, Query>();

		#endregion Fields

		#region Construction and destruction

		/// <summary>
		///	Default constructor.
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public D3D9HardwareOcclusionQuery()
		{
			D3D9RenderSystem.ResourceManager.NotifyResourceCreated( this );
		}

		[OgreVersion( 1, 7, 2, "~D3D9HardwareOcclusionQuery" )]
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					foreach ( var it in this._mapDeviceToQuery )
					{
						it.SafeDispose();
					}

					this._mapDeviceToQuery.Clear();
					D3D9RenderSystem.ResourceManager.NotifyResourceDestroyed( this );
				}
			}

			base.dispose( disposeManagedResources );
		}

		#endregion Construction and destruction

		#region Methods

		/// <summary>
		/// Starts the hardware occlusion query
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public override void Begin()
		{
			Device pCurDevice = D3D9RenderSystem.ActiveD3D9Device;
			Query pOccQuery;
			bool queryWasFound = this._mapDeviceToQuery.TryGetValue( pCurDevice, out pOccQuery );

			// No resource exits for current device -> create it.
			if ( !queryWasFound || pOccQuery == null )
			{
				_createQuery( pCurDevice );
			}

			// Grab the query of the current device.
			pOccQuery = this._mapDeviceToQuery[ pCurDevice ];

			if ( pOccQuery != null )
			{
				pOccQuery.Issue( Issue.Begin );
				isQueryResultStillOutstanding = true;
				LastFragmentCount = 0;
			}
		}

		/// <summary>
		/// Ends the hardware occlusion test
		/// </summary>
		[OgreVersion( 1, 7, 2 )]
		public override void End()
		{
			Device pCurDevice = D3D9RenderSystem.ActiveD3D9Device;

			if ( !this._mapDeviceToQuery.ContainsKey( pCurDevice ) )
			{
				throw new AxiomException( "End occlusion called without matching begin call !!" );
			}

			Query pOccQuery = this._mapDeviceToQuery[ pCurDevice ];

			if ( pOccQuery != null )
			{
				pOccQuery.Issue( Issue.End );
			}
		}

		/// <see cref="Axiom.Graphics.HardwareOcclusionQuery.PullResults"/>
		[OgreVersion( 1, 7, 2 )]
		public override bool PullResults( out int NumOfFragments )
		{
			// default to returning a high count.  will be set otherwise if the query runs
			NumOfFragments = 100000;
			Device pCurDevice = D3D9RenderSystem.ActiveD3D9Device;
			Query pOccQuery;
			bool queryWasFound = this._mapDeviceToQuery.TryGetValue( pCurDevice, out pOccQuery );

			if ( !queryWasFound || pOccQuery == null )
			{
				return false;
			}

			// in case you didn't check if query arrived and want the result now.
			if ( isQueryResultStillOutstanding )
			{
				int pixels = 0;
				// Loop until the data becomes available
				while ( true )
				{
					try
					{
						pixels = pOccQuery.GetData<int>( true );
						LastFragmentCount = pixels;
						NumOfFragments = pixels;
						break;
					}
					catch ( SharpDXException ex )
					{
						if ( ex.ResultCode == ResultCode.DeviceLost )
						{
							LastFragmentCount = NumOfFragments = 0;
							pOccQuery.SafeDispose();
							break;
						}
					}
				}

				isQueryResultStillOutstanding = false;
			}
			else
			{
				// we already stored result from last frames.
				NumOfFragments = LastFragmentCount;
			}

			return true;
		}

		/// <summary>
		/// Lets you know when query is done, or still be processed by the Hardware
		/// </summary>
		/// <returns>true if query isn't finished.</returns>
		[OgreVersion( 1, 7, 2 )]
		public override bool IsStillOutstanding()
		{
			// in case you already asked for this query
			if ( !isQueryResultStillOutstanding )
			{
				return false;
			}

			Device pCurDevice = D3D9RenderSystem.ActiveD3D9Device;
			Query pOccQuery;
			bool queryWasFound = this._mapDeviceToQuery.TryGetValue( pCurDevice, out pOccQuery );

			if ( !queryWasFound || pOccQuery == null )
			{
				return false;
			}

			try
			{
				var pixels = pOccQuery.GetData<int>( false );
				LastFragmentCount = pixels;
				isQueryResultStillOutstanding = false;

				return false;
			}
			catch ( SharpDXException ex )
			{
				if ( ex.ResultCode == ResultCode.DeviceLost )
				{
					LastFragmentCount = 100000;
					pOccQuery.SafeDispose();
				}

				return true;
			}
		}

		[OgreVersion( 1, 7, 2 )]
		private void _createQuery( Device d3d9Device )
		{
			// Check if query supported.
			try
			{
				// create the occlusion query.
				this._mapDeviceToQuery[ d3d9Device ] = new Query( d3d9Device, QueryType.Occlusion );
			}
			catch
			{
				this._mapDeviceToQuery[ d3d9Device ] = null;
			}
		}

		[OgreVersion( 1, 7, 2 )]
		private void _releaseQuery( Device d3d9Device )
		{
			if ( this._mapDeviceToQuery.ContainsKey( d3d9Device ) )
			{
				// Remove from query resource map.
				this._mapDeviceToQuery[ d3d9Device ].SafeDispose();
				this._mapDeviceToQuery.Remove( d3d9Device );
			}
		}

		#endregion Methods

		#region ID3D9Resource Members

		/// <see cref="ID3D9Resource.NotifyOnDeviceCreate"/>
		[OgreVersion( 1, 7, 2 )]
		public void NotifyOnDeviceCreate( Device d3d9Device ) { }

		/// <see cref="ID3D9Resource.NotifyOnDeviceDestroy"/>
		[OgreVersion( 1, 7, 2 )]
		public void NotifyOnDeviceDestroy( Device d3d9Device )
		{
			_releaseQuery( d3d9Device );
		}

		/// <see cref="ID3D9Resource.NotifyOnDeviceLost"/>
		[OgreVersion( 1, 7, 2 )]
		public void NotifyOnDeviceLost( Device d3d9Device )
		{
			_releaseQuery( d3d9Device );
		}

		/// <see cref="ID3D9Resource.NotifyOnDeviceReset"/>
		[OgreVersion( 1, 7, 2 )]
		public void NotifyOnDeviceReset( Device d3d9Device ) { }

		#endregion
	};
}
