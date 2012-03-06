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

        private Dictionary<D3D9.Device, D3D9.Query> _mapDeviceToQuery = new Dictionary<D3D9.Device, D3D9.Query>();

        #endregion Fields

        #region Construction and destruction

        /// <summary>
        ///	Default constructor.
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public D3D9HardwareOcclusionQuery()
            : base()
        {
            D3D9RenderSystem.ResourceManager.NotifyResourceCreated( this );
        }

        [OgreVersion( 1, 7, 2, "~D3D9HardwareOcclusionQuery" )]
        protected override void dispose( bool disposeManagedResources )
        {
            if ( !this.IsDisposed )
            {
                if ( disposeManagedResources )
                {
                    foreach ( var it in _mapDeviceToQuery )
                        it.SafeDispose();

                    _mapDeviceToQuery.Clear();
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
            var pCurDevice = D3D9RenderSystem.ActiveD3D9Device;
            D3D9.Query pOccQuery;
            var queryWasFound = _mapDeviceToQuery.TryGetValue( pCurDevice, out pOccQuery );

            // No resource exits for current device -> create it.
            if ( !queryWasFound || pOccQuery == null )
                _createQuery( pCurDevice );

            // Grab the query of the current device.
            pOccQuery = _mapDeviceToQuery[ pCurDevice ];

            if ( pOccQuery != null )
            {
                pOccQuery.Issue( D3D9.Issue.Begin );
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
            var pCurDevice = D3D9RenderSystem.ActiveD3D9Device;

            if ( !_mapDeviceToQuery.ContainsKey( pCurDevice ) )
                throw new AxiomException( "End occlusion called without matching begin call !!" );

            var pOccQuery = _mapDeviceToQuery[ pCurDevice ];

            if ( pOccQuery != null )
                pOccQuery.Issue( D3D9.Issue.End );
        }

        /// <see cref="Axiom.Graphics.HardwareOcclusionQuery.PullResults"/>
        [OgreVersion( 1, 7, 2 )]
        public override bool PullResults( out int NumOfFragments )
        {
            // default to returning a high count.  will be set otherwise if the query runs
            NumOfFragments = 100000;
            var pCurDevice = D3D9RenderSystem.ActiveD3D9Device;
            D3D9.Query pOccQuery;
            var queryWasFound = _mapDeviceToQuery.TryGetValue( pCurDevice, out pOccQuery );

            if ( !queryWasFound || pOccQuery == null )
                return false;

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
                        this.LastFragmentCount = pixels;
                        NumOfFragments = pixels;
                        break;
                    }
                    catch ( DX.SharpDXException ex )
                    {
                        if ( ex.ResultCode == D3D9.ResultCode.DeviceLost )
                        {
                            this.LastFragmentCount = NumOfFragments = 0;
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
                NumOfFragments = this.LastFragmentCount;
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
                return false;

            var pCurDevice = D3D9RenderSystem.ActiveD3D9Device;
            D3D9.Query pOccQuery;
            var queryWasFound = _mapDeviceToQuery.TryGetValue( pCurDevice, out pOccQuery );

            if ( !queryWasFound || pOccQuery == null )
                return false;

            try
            {
                var pixels = pOccQuery.GetData<int>( false );
                this.LastFragmentCount = pixels;
                isQueryResultStillOutstanding = false;

                return false;
            }
            catch ( DX.SharpDXException ex )
            {
                if ( ex.ResultCode == D3D9.ResultCode.DeviceLost )
                {
                    this.LastFragmentCount = 100000;
                    pOccQuery.SafeDispose();
                }

                return true;
            }
        }

        [OgreVersion( 1, 7, 2 )]
        private void _createQuery( D3D9.Device d3d9Device )
        {
            // Check if query supported.
            try
            {
                // create the occlusion query.
                _mapDeviceToQuery[ d3d9Device ] = new D3D9.Query( d3d9Device, D3D9.QueryType.Occlusion );
            }
            catch
            {
                _mapDeviceToQuery[ d3d9Device ] = null;
            }
        }

        [OgreVersion( 1, 7, 2 )]
        private void _releaseQuery( D3D9.Device d3d9Device )
        {
            if ( _mapDeviceToQuery.ContainsKey( d3d9Device ) )
            {
                // Remove from query resource map.
                _mapDeviceToQuery[ d3d9Device ].SafeDispose();
                _mapDeviceToQuery.Remove( d3d9Device );
            }
        }

        #endregion Methods

        #region ID3D9Resource Members

        /// <see cref="ID3D9Resource.NotifyOnDeviceCreate"/>
        [OgreVersion( 1, 7, 2 )]
        public void NotifyOnDeviceCreate( D3D9.Device d3d9Device )
        {
        }

        /// <see cref="ID3D9Resource.NotifyOnDeviceDestroy"/>
        [OgreVersion( 1, 7, 2 )]
        public void NotifyOnDeviceDestroy( D3D9.Device d3d9Device )
        {
            _releaseQuery( d3d9Device );
        }

        /// <see cref="ID3D9Resource.NotifyOnDeviceLost"/>
        [OgreVersion( 1, 7, 2 )]
        public void NotifyOnDeviceLost( D3D9.Device d3d9Device )
        {
            _releaseQuery( d3d9Device );
        }

        /// <see cref="ID3D9Resource.NotifyOnDeviceReset"/>
        [OgreVersion( 1, 7, 2 )]
        public void NotifyOnDeviceReset( D3D9.Device d3d9Device )
        {
        }

        #endregion ID3D9Resource Members
    };
}