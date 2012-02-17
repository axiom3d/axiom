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
using D3D9 = SlimDX.Direct3D9;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9
{
    /// <summary>
    /// Specialisation of VertexDeclaration for D3D9
    /// </summary>
    public sealed class D3D9VertexDeclaration : VertexDeclaration, ID3D9Resource
    {
        #region Member variables

        private Dictionary<D3D9.Device, D3D9.VertexDeclaration> _mapDeviceToDeclaration = new Dictionary<D3D9.Device, D3D9.VertexDeclaration>();

        #endregion Member variables

        #region Properties

        /// <summary>
        /// Gets the D3D9-specific vertex declaration.
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public D3D9.VertexDeclaration D3DVertexDecl
        {
            get
            {
                var pCurDevice = D3D9RenderSystem.ActiveD3D9Device;
                D3D9.VertexDeclaration it, lpVertDecl;
                var declFound = _mapDeviceToDeclaration.TryGetValue( pCurDevice, out it );

                // Case we have to create the declaration for this device.
                if ( declFound = false || it == null )
                {
                    var d3dElements = new D3D9.VertexElement[ elements.Count + 1 ];

                    // loop through and configure each element for D3D
                    ushort idx;
                    for ( idx = 0; idx < elements.Count; ++idx )
                    {
                        var element = elements[ idx ];

                        d3dElements[ idx ].Method = D3D9.DeclarationMethod.Default;
                        d3dElements[ idx ].Offset = (short)element.Offset;
                        d3dElements[ idx ].Stream = element.Source;
                        d3dElements[ idx ].Type = D3D9Helper.ConvertEnum( element.Type );
                        d3dElements[ idx ].Usage = D3D9Helper.ConvertEnum( element.Semantic );
                        // NB force index if colours since D3D uses the same usage for 
                        // diffuse & specular
                        switch ( element.Semantic )
                        {
                            case VertexElementSemantic.Specular:
                                d3dElements[ idx ].UsageIndex = 1;
                                break;

                            case VertexElementSemantic.Diffuse:
                                d3dElements[ idx ].UsageIndex = 0;
                                break;

                            default:
                                d3dElements[ idx ].UsageIndex = (byte)element.Index;
                                break;
                        } //  switch
                    } // for

                    // Add terminator
                    d3dElements[ idx ].Stream = 0xff;
                    d3dElements[ idx ].Offset = 0;
                    d3dElements[ idx ].Type = D3D9.DeclarationType.Unused;
                    d3dElements[ idx ].Method = 0;
                    d3dElements[ idx ].Usage = 0;
                    d3dElements[ idx ].UsageIndex = 0;

                    lpVertDecl = new D3D9.VertexDeclaration( pCurDevice, d3dElements );

                    if ( declFound )
                        _mapDeviceToDeclaration[ pCurDevice ] = lpVertDecl;
                    else
                        _mapDeviceToDeclaration.Add( pCurDevice, lpVertDecl );
                }

                // Declaration already exits.
                else
                {
                    lpVertDecl = _mapDeviceToDeclaration[ pCurDevice ];
                }

                return lpVertDecl;
            }
        }

        #endregion Properties

        #region dispose

        [OgreVersion( 1, 7, 2, "~D3D9VertexDeclaration" )]
        protected override void dispose( bool disposeManagedResources )
        {
            if ( !this.IsDisposed )
            {
                if ( disposeManagedResources )
                {
                    _releaseDeclaration();
                }

                // There are no unmanaged resources to release, but
                // if we add them, they need to be released here.
            }

            // If it is available, make the call to the
            // base class's Dispose(Boolean) method
            base.dispose( disposeManagedResources );
        }

        #endregion dispose

        #region Methods

        /// <see cref="Axiom.Graphics.VertexDeclaration.AddElement(short, int, VertexElementType, VertexElementSemantic, int)"/>
        [OgreVersion( 1, 7, 2 )]
        public override Axiom.Graphics.VertexElement AddElement( short source, int offset, VertexElementType type, VertexElementSemantic semantic, int index )
        {
            _releaseDeclaration();
            return base.AddElement( source, offset, type, semantic, index );
        }

        /// <see cref="Axiom.Graphics.VertexDeclaration.InsertElement(int, short, int, VertexElementType, VertexElementSemantic, int)"/>
        [OgreVersion( 1, 7, 2 )]
        public override Axiom.Graphics.VertexElement InsertElement( int position, short source, int offset, VertexElementType type, VertexElementSemantic semantic, int index )
        {
            _releaseDeclaration();
            return base.InsertElement( position, source, offset, type, semantic, index );
        }

        /// <see cref="Axiom.Graphics.VertexDeclaration.RemoveElement(int)"/>
        [OgreVersion( 1, 7, 2 )]
        public override void RemoveElement( int index )
        {
            base.RemoveElement( index );
            _releaseDeclaration();
        }

        /// <see cref="Axiom.Graphics.VertexDeclaration.RemoveElement(VertexElementSemantic, int)"/>
        [OgreVersion( 1, 7, 2 )]
        public override void RemoveElement( VertexElementSemantic semantic, int index )
        {
            base.RemoveElement( semantic, index );
            _releaseDeclaration();
        }

        /// <see cref="Axiom.Graphics.VertexDeclaration.RemoveAllElements"/>
        [OgreVersion( 1, 7, 2 )]
        public override void RemoveAllElements()
        {
            base.RemoveAllElements();
            _releaseDeclaration();
        }

        /// <see cref="Axiom.Graphics.VertexDeclaration.ModifyElement(int, short, int, VertexElementType, VertexElementSemantic, int)"/>
        [OgreVersion( 1, 7, 2 )]
        public override void ModifyElement( int elemIndex, short source, int offset, VertexElementType type, VertexElementSemantic semantic, int index )
        {
            base.ModifyElement( elemIndex, source, offset, type, semantic, index );
            _releaseDeclaration();
        }

        [OgreVersion( 1, 7, 2 )]
        private void _releaseDeclaration()
        {
            //Entering critical section
            this.LockDeviceAccess();

            foreach ( var it in _mapDeviceToDeclaration.Values )
                it.SafeDispose();

            _mapDeviceToDeclaration.Clear();

            //Leaving critical section
            this.UnlockDeviceAccess();
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
            //Entering critical section
            this.LockDeviceAccess();

            if ( _mapDeviceToDeclaration.ContainsKey( d3d9Device ) )
            {
                _mapDeviceToDeclaration[ d3d9Device ].SafeDispose();
                _mapDeviceToDeclaration.Remove( d3d9Device );
            }

            //Leaving critical section
            this.UnlockDeviceAccess();
        }

        /// <see cref="ID3D9Resource.NotifyOnDeviceLost"/>
        [OgreVersion( 1, 7, 2 )]
        public void NotifyOnDeviceLost( D3D9.Device d3d9Device )
        {
        }

        /// <see cref="ID3D9Resource.NotifyOnDeviceReset"/>
        [OgreVersion( 1, 7, 2 )]
        public void NotifyOnDeviceReset( D3D9.Device d3d9Device )
        {
        }

        #endregion ID3D9Resource Members
    };
}