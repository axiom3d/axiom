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
//     <id value="$Id:"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using SWF = System.Windows.Forms;

using Axiom.Configuration;
using Axiom.Graphics;
using Axiom.Core;

using XnaF = Microsoft.Xna.Framework;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.Xna
{
    public class RenderSystem : Axiom.Graphics.RenderSystem
    {
        /// <summary>
        ///    Reference to the Xna device.
        /// </summary>
        private XnaF.Graphics.GraphicsDevice _device;

        private XnaF.Graphics.GraphicsDeviceCapabilities _capabilities;

        // stores texture stage info locally for convenience
        internal XnaTextureStageDescription[] texStageDesc = new XnaTextureStageDescription[ Config.MaxTextureLayers ];

        /// <summary>
        ///    Number of streams used last frame, used to unbind any buffers not used during the current operation.
        /// </summary>
        protected int numLastStreams;

        protected int primCount;
        protected int renderCount = 0;
        
        public RenderSystem()
        {
            _initConfigOptions();

            // init the texture stage descriptions
            for ( int i = 0; i < Config.MaxTextureLayers; i++ )
            {
                texStageDesc[ i ].autoTexCoordType = TexCoordCalcMethod.None;
                texStageDesc[ i ].coordIndex = 0;
                texStageDesc[ i ].texType = XnaTextureType.Normal;
                texStageDesc[ i ].tex = null;
            }
        }

        private void _initConfigOptions()
        {
            ConfigOption optDevice = new ConfigOption( "Rendering Device", "", false );
            ConfigOption optVideoMode = new ConfigOption( "Video Mode", "800 x 600 @ 32-bit colour", false );
            ConfigOption optFullScreen = new ConfigOption( "Full Screen", "No", false );
            ConfigOption optVSync = new ConfigOption( "VSync", "No", false );
            ConfigOption optAA = new ConfigOption( "Anti aliasing", "None", false );
            ConfigOption optFPUMode = new ConfigOption( "Floating-point mode", "Fastest", false );

            optDevice.PossibleValues.Clear();
            DriverCollection driver = XnaHelper.GetDriverInfo();

            foreach ( VideoMode mode in driver[0].VideoModes )
            {
                string query = string.Format( "{0} x {1} @ {2}-bit colour", mode.Width, mode.Height, mode.ColorDepth.ToString() );
                // add a new row to the display settings table
                optVideoMode.PossibleValues.Add( query );
            }

            optFullScreen.PossibleValues.Add( "Yes" );
            optFullScreen.PossibleValues.Add( "No" );

            optVSync.PossibleValues.Add( "Yes" );
            optVSync.PossibleValues.Add( "No" );

            optAA.PossibleValues.Add( "None" );

            optFPUMode.PossibleValues.Clear();
            optFPUMode.PossibleValues.Add( "Fastest" );
            optFPUMode.PossibleValues.Add( "Consistent" );

            ConfigOptions.Add( optDevice );
            ConfigOptions.Add( optVideoMode );
            ConfigOptions.Add( optFullScreen );
            ConfigOptions.Add( optVSync );
            ConfigOptions.Add( optAA );
            ConfigOptions.Add( optFPUMode );

        }

        /// <summary>
        ///		Creates a default form to use for a rendering target.
        /// </summary>
        /// <remarks>
        ///		This is used internally whenever <see cref="Initialize"/> is called and autoCreateWindow is set to true.
        /// </remarks>
        /// <param name="windowTitle">Title of the window.</param>
        /// <param name="top">Top position of the window.</param>
        /// <param name="left">Left position of the window.</param>
        /// <param name="width">Width of the window.</param>
        /// <param name="height">Height of the window</param>
        /// <param name="fullScreen">Prepare the form for fullscreen mode?</param>
        /// <returns>A form suitable for using as a rendering target.</returns>
        private DefaultForm _createDefaultForm( string windowTitle, int top, int left, int width, int height, bool fullScreen )
        {
            DefaultForm form = new DefaultForm();

            form.ClientSize = new Size( width, height );
            form.MaximizeBox = false;
            form.MinimizeBox = false;
            form.StartPosition = SWF.FormStartPosition.CenterScreen;
            form.BringToFront();
            form.TopLevel = true;
            form.TopMost = true;

            if ( fullScreen )
            {
                form.Top = 0;
                form.Left = 0;
                form.FormBorderStyle = SWF.FormBorderStyle.None;
                form.WindowState = SWF.FormWindowState.Maximized;
                form.TopMost = true;
            }
            else
            {
                form.Top = top;
                form.Left = left;
                form.FormBorderStyle = SWF.FormBorderStyle.FixedSingle;
                form.WindowState = SWF.FormWindowState.Normal;
                form.Text = windowTitle;
            }

            return form;
        }

        private XnaF.Graphics.PresentationParameters _createPresentationParams( bool isFullscreen, bool depthBuffer, int width, int height, int colorDepth )
        {
            // if this is the first window, get the device and do other initialization

            XnaF.Graphics.PresentationParameters presentParams = new XnaF.Graphics.PresentationParameters();
            presentParams.IsFullScreen = isFullscreen;
            presentParams.BackBufferCount = 0;
            presentParams.EnableAutoDepthStencil = depthBuffer;
            presentParams.BackBufferWidth = width;
            presentParams.BackBufferHeight = height;
            presentParams.MultiSampleType = XnaF.Graphics.MultiSampleType.None;
            presentParams.SwapEffect = XnaF.Graphics.SwapEffect.Discard;

            // TODO: Check vsync setting
            presentParams.PresentationInterval = XnaF.Graphics.PresentInterval.Immediate;

            // supports 16 and 32 bit color
            if ( colorDepth == 16 )
            {
                presentParams.BackBufferFormat = XnaF.Graphics.SurfaceFormat.Bgr565;
            }
            else
            {
                presentParams.BackBufferFormat = XnaF.Graphics.SurfaceFormat.Bgr32;
            }

            if ( colorDepth > 16 )
            {
                // check for 24 bit Z buffer with 8 bit stencil (optimal choice)
                if ( !XnaF.Graphics.GraphicsAdapter.DefaultAdapter.CheckDeviceFormat( XnaF.Graphics.DeviceType.Hardware, presentParams.BackBufferFormat, XnaF.Graphics.ResourceUsage.None, XnaF.Graphics.QueryUsages.None, XnaF.Graphics.ResourceType.Texture2D, XnaF.Graphics.DepthFormat.Depth24Stencil8 ) )
                {
                    // doh, check for 32 bit Z buffer then
                    if ( !XnaF.Graphics.GraphicsAdapter.DefaultAdapter.CheckDeviceFormat( XnaF.Graphics.DeviceType.Hardware, presentParams.BackBufferFormat, XnaF.Graphics.ResourceUsage.None, XnaF.Graphics.QueryUsages.None, XnaF.Graphics.ResourceType.Texture2D, XnaF.Graphics.DepthFormat.Depth32 ) )
                    {
                        // float doh, just use 16 bit Z buffer
                        presentParams.AutoDepthStencilFormat = XnaF.Graphics.DepthFormat.Depth16;
                    }
                    else
                    {
                        // use 32 bit Z buffer
                        presentParams.AutoDepthStencilFormat = XnaF.Graphics.DepthFormat.Depth32;
                    }
                }
                else
                {
                    if ( XnaF.Graphics.GraphicsAdapter.DefaultAdapter.CheckDeviceFormat( XnaF.Graphics.DeviceType.Hardware, presentParams.BackBufferFormat, XnaF.Graphics.ResourceUsage.None, XnaF.Graphics.QueryUsages.None, XnaF.Graphics.ResourceType.Texture2D, XnaF.Graphics.DepthFormat.Depth24Stencil8 ) )
                    {
                        presentParams.AutoDepthStencilFormat = XnaF.Graphics.DepthFormat.Depth24Stencil8;
                    }
                    else
                    {
                        presentParams.AutoDepthStencilFormat = XnaF.Graphics.DepthFormat.Depth24;
                    }
                }
            }
            else
            {
                // use 16 bit Z buffer if they arent using true color
                presentParams.AutoDepthStencilFormat = XnaF.Graphics.DepthFormat.Depth16;
            }
            presentParams.AutoDepthStencilFormat = XnaF.Graphics.DepthFormat.Depth24Stencil8;

            return presentParams;
        }

        /// <summary>
        ///		Helper method to go through and interrogate hardware capabilities.
        /// </summary>
        private void _checkCaps( XnaF.Graphics.GraphicsDevice device )
        {
            // get the number of possible texture units
            caps.TextureUnitCount = _capabilities.MaxSimultaneousTextures;

            // max active lights
            //caps.MaxLights = d3dCaps.MaxActiveLights;

            //D3D.Surface surface = device.DepthStencilSurface;
            //D3D.SurfaceDescription surfaceDesc = surface.Description;
            //surface.Dispose();

            //if ( surfaceDesc.Format == D3D.Format.D24S8 || surfaceDesc.Format == D3D.Format.D24X8 )
            //{
            //    caps.SetCap( Capabilities.StencilBuffer );
            //    // always 8 here
            //    caps.StencilBufferBits = 8;
            //}

            //// some cards, oddly enough, do not support this
            //if ( d3dCaps.DeclarationTypes.SupportsUByte4 )
            //{
            //    caps.SetCap( Capabilities.VertexFormatUByte4 );
            //}

            //// Anisotropy?
            //if ( d3dCaps.MaxAnisotropy > 1 )
            //{
            //    caps.SetCap( Capabilities.AnisotropicFiltering );
            //}

            //// Hardware mipmapping?
            //if ( d3dCaps.DriverCaps.CanAutoGenerateMipMap )
            //{
            //    caps.SetCap( Capabilities.HardwareMipMaps );
            //}

            //// blending between stages is definately supported
            //caps.SetCap( Capabilities.TextureBlending );
            //caps.SetCap( Capabilities.MultiTexturing );

            //// Dot3 bump mapping?
            //if ( d3dCaps.TextureOperationCaps.SupportsDotProduct3 )
            //{
            //    caps.SetCap( Capabilities.Dot3 );
            //}

            //// Cube mapping?
            //if ( d3dCaps.TextureCaps.SupportsCubeMap )
            //{
            //    caps.SetCap( Capabilities.CubeMapping );
            //}

            //// Texture Compression
            //// We always support compression, D3DX will decompress if device does not support
            //caps.SetCap( Capabilities.TextureCompression );
            //caps.SetCap( Capabilities.TextureCompressionDXT );

            //// D3D uses vertex buffers for everything
            //caps.SetCap( Capabilities.VertexBuffer );

            //// Scissor test
            //if ( d3dCaps.RasterCaps.SupportsScissorTest )
            //{
            //    caps.SetCap( Capabilities.ScissorTest );
            //}

            //// 2 sided stencil
            //if ( d3dCaps.StencilCaps.SupportsTwoSided )
            //{
            //    caps.SetCap( Capabilities.TwoSidedStencil );
            //}

            //// stencil wrap
            //if ( d3dCaps.StencilCaps.SupportsIncrement && d3dCaps.StencilCaps.SupportsDecrement )
            //{
            //    caps.SetCap( Capabilities.StencilWrap );
            //}

            //// Hardware Occlusion
            //try
            //{
            //    D3D.Query test = new D3D.Query( device, D3D.QueryType.Occlusion );

            //    // if we made it this far, it is supported
            //    caps.SetCap( Capabilities.HardwareOcculusion );

            //    test.Dispose();
            //}
            //catch
            //{
            //    // eat it, this is not supported
            //    // TODO: Isn't there a better way to check for D3D occlusion query support?
            //}

            //if ( d3dCaps.MaxUserClipPlanes > 0 )
            //{
            //    caps.SetCap( Capabilities.UserClipPlanes );
            //}

            //CheckVertexProgramCaps();

            //CheckFragmentProgramCaps();

            //// Infinite projection?
            //// We have no capability for this, so we have to base this on our
            //// experience and reports from users
            //// Non-vertex program capable hardware does not appear to support it
            //if ( caps.CheckCap( Capabilities.VertexPrograms ) )
            //{
            //    // GeForce4 Ti (and presumably GeForce3) does not
            //    // render infinite projection properly, even though it does in GL
            //    // So exclude all cards prior to the FX range from doing infinite

            //    //Driver driver = D3DHelper.GetDriverInfo();

            //    D3D.AdapterDetails details = D3D.Manager.Adapters[ 0 ];

            //    //AdapterDetails details = Manager.Adapters[driver.AdapterNumber].Information;

            //    // not nVidia or GeForceFX and above
            //    if ( details.Information.VendorId != 0x10DE || details.Information.DeviceId >= 0x0301 )
            //    {
            //        caps.SetCap( Capabilities.InfiniteFarPlane );
            //    }
            //}

            // write hardware capabilities to registered log listeners
            caps.Log();
        }

        private XnaF.Graphics.GraphicsDevice _initDevice( bool isFullscreen, bool depthBuffer, int width, int height, int colorDepth, SWF.Control target )
        {
            if ( _device != null )
            {
                return _device;
            }

            XnaF.Graphics.GraphicsDevice newDevice = null;

            XnaF.Graphics.PresentationParameters presentParams = _createPresentationParams( isFullscreen, depthBuffer, width, height, colorDepth );

            // create the Xna Device, trying for the best vertex support first, and settling for less if necessary
            XnaF.Graphics.DeviceType type = XnaF.Graphics.DeviceType.Hardware;
            XnaF.Graphics.GraphicsAdapter adapter = XnaF.Graphics.GraphicsAdapter.DefaultAdapter;
            try
            {
                // hardware vertex processing
#if DEBUG
                //for ( int i = 0; i < D3D.Manager.Adapters.Count; i++ )
                //{
                //    if ( D3D.Manager.Adapters[ i ].Information.Description == "NVIDIA NVPerfHUD" )
                //    {
                //        adapterNum = i;
                //        type = D3D.DeviceType.Reference;
                //    }
                //}
#endif
                // use this with NVPerfHUD
                newDevice = new XnaF.Graphics.GraphicsDevice( adapter, type, target.Handle, XnaF.Graphics.CreateOptions.HardwareVertexProcessing, presentParams );
            }
            catch ( Exception )
            {
                try
                {
                    // doh, how bout mixed vertex processing
                    newDevice = new XnaF.Graphics.GraphicsDevice( adapter, type, target.Handle, XnaF.Graphics.CreateOptions.MixedVertexProcessing, presentParams );
                }
                catch ( Exception )
                {
                    // what the...ok, how bout software vertex procssing.  if this fails, then I don't even know how they are seeing
                    // anything at all since they obviously don't have a video card installed
                    newDevice = new XnaF.Graphics.GraphicsDevice( adapter, XnaF.Graphics.DeviceType.Reference, target.Handle, XnaF.Graphics.CreateOptions.SoftwareVertexProcessing, presentParams );
                }
            }

            // save the device capabilites
            _capabilities = newDevice.GraphicsDeviceCapabilities;

            // by creating our texture manager, singleton TextureManager will hold our implementation
            textureMgr = new XnaTextureManager( newDevice );

            // by creating our Gpu program manager, singleton GpuProgramManager will hold our implementation
            //gpuProgramMgr = new D3DGpuProgramManager( newDevice );

            // intializes the HardwareBufferManager singleton
            hardwareBufferManager = new XnaHardwareBufferManager( newDevice );

            _checkCaps( newDevice );

            return newDevice;
        }

        /// <summary>
        /// 
        /// </summary>
        protected void SetVertexBufferBinding( VertexBufferBinding binding )
        {
            IEnumerator e = binding.Bindings;

            // TODO: Optimize to remove enumeration if possible, although with so few iterations it may never make a difference
            while ( e.MoveNext() )
            {
                DictionaryEntry entry = (DictionaryEntry)e.Current;
                XnaHardwareVertexBuffer buffer = (XnaHardwareVertexBuffer)entry.Value;

                short stream = (short)entry.Key;

                _device.Vertices[ stream ].SetSource( buffer.XnaVertexBuffer, 0, buffer.VertexSize );

                numLastStreams++;
            }

            // Unbind any unused sources
            for ( int i = binding.BindingCount; i < numLastStreams; i++ )
            {
                _device.Vertices[ i ].SetSource( null, 0, 0 );
            }

            numLastStreams = binding.BindingCount;
        }

        /// <summary>
        ///		Helper method for setting the current vertex declaration.
        /// </summary>
        protected void SetVertexDeclaration( VertexDeclaration decl )
        {
            // TODO: Check for duplicate setting and avoid setting if dupe
            XnaVertexDeclaration vertDecl = (XnaVertexDeclaration)decl;

            _device.VertexDeclaration = vertDecl.XnaVertexDecl;
        }

        private XnaF.Matrix _makeXnaMatrix( Axiom.Math.Matrix4 matrix )
        {
            XnaF.Matrix xna = new XnaF.Matrix();

            // set it to a transposed matrix since Xna uses row vectors
            xna.M11 = matrix.m00;
            xna.M12 = matrix.m10;
            xna.M13 = matrix.m20;
            xna.M14 = matrix.m30;
            xna.M21 = matrix.m01;
            xna.M22 = matrix.m11;
            xna.M23 = matrix.m21;
            xna.M24 = matrix.m31;
            xna.M31 = matrix.m02;
            xna.M32 = matrix.m12;
            xna.M33 = matrix.m22;
            xna.M34 = matrix.m32;
            xna.M41 = matrix.m03;
            xna.M42 = matrix.m13;
            xna.M43 = matrix.m23;
            xna.M44 = matrix.m33;

            return xna;
        }

        #region Axiom.Graphics.RenderSystem Implementation

        #region Fields & Properties

        #region AmbientLight Property

        private Axiom.Core.ColorEx _ambientLight;
        public override Axiom.Core.ColorEx AmbientLight
        {
            get
            {
                return _ambientLight;
            }
            set
            {
                _ambientLight = value;
            }
        }

        #endregion AmbientLight Property
        /// Saved last view matrix
        private XnaF.Matrix _viewMatrix = XnaF.Matrix.Identity;
        public override Axiom.Math.Matrix4 ViewMatrix
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                Axiom.Math.Matrix4 viewMatrix;
                // flip the transform portion of the matrix for DX and its left-handed coord system
                // save latest view matrix
                viewMatrix = value;
                viewMatrix.m20 = -viewMatrix.m20;
                viewMatrix.m21 = -viewMatrix.m21;
                viewMatrix.m22 = -viewMatrix.m22;
                viewMatrix.m23 = -viewMatrix.m23;

                _viewMatrix = _makeXnaMatrix( viewMatrix );
            }
        }

        private XnaF.Matrix _worldMatrix = XnaF.Matrix.Identity;
        public override Axiom.Math.Matrix4 WorldMatrix
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                _worldMatrix = _makeXnaMatrix( value );
            }
        }

        public override Axiom.Graphics.CullingMode CullingMode
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                // ToDo:
                // throw new NotImplementedException();
            }
        }

        public override int DepthBias
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                // ToDo:
                // throw new NotImplementedException();
            }
        }

        public override bool DepthCheck
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                // ToDo:
                // throw new NotImplementedException();
            }
        }

        public override Axiom.Graphics.CompareFunction DepthFunction
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                // ToDo:
                // throw new NotImplementedException();
            }
        }

        public override bool DepthWrite
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                // ToDo:
                // throw new NotImplementedException();
            }
        }

        #region LightingEnabled Property

        private bool _lightingEnabled;
        public override bool LightingEnabled
        {
            get
            {
                return _lightingEnabled;
            }
            set
            {
                _lightingEnabled = value;
            }
        }

        #endregion LightingEnabled Property

        public override bool NormalizeNormals
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                // ToDo:
                // throw new NotImplementedException();
            }
        }

        public override Axiom.Math.Matrix4 ProjectionMatrix
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                // ToDo:
                // throw new NotImplementedException();
            }
        }

        public override Axiom.Graphics.SceneDetailLevel RasterizationMode
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                //ToDo: Need to Implement
                //throw new NotImplementedException();
            }
        }

        public override Axiom.Graphics.Shading ShadingMode
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                // ToDo:
                // throw new NotImplementedException();
            }
        }

        public override bool StencilCheckEnabled
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        #region HorizontalTexelOffset Property

        public override float HorizontalTexelOffset
        {
            get
            {
                // Xna considers the origin to be in the center of a pixel
                return -0.5f;
            }
        }

        #endregion HorizontalTexelOffset Property

        #region VerticleTexelOffset Property

        public override float VerticalTexelOffset
        {
            get
            {
                // Xna considers the origin to be in the center of a pixel
                return -0.5f;
            }
        }

        #endregion VerticleTexelOffset Property


        #endregion Fields & Properties

        #region Methods 

        public override Axiom.Graphics.RenderWindow Initialize( bool autoCreateWindow, string windowTitle )
        {
            Axiom.Graphics.RenderWindow renderWindow = null;

            if ( autoCreateWindow )
            {
                int width = 640;
                int height = 480;
                int bpp = 32;
                bool fullScreen = false;

                ConfigOption optVM = ConfigOptions[ "Video Mode" ];
                string vm = optVM.Value;
                width = int.Parse( vm.Substring( 0, vm.IndexOf( "x" ) ) );
                height = int.Parse( vm.Substring( vm.IndexOf( "x" ) + 1, vm.IndexOf( "@" ) - ( vm.IndexOf( "x" ) + 1 ) ) );
                bpp = int.Parse( vm.Substring( vm.IndexOf( "@" ) + 1, vm.IndexOf( "-" ) - ( vm.IndexOf( "@" ) + 1 ) ) );

                fullScreen = ( ConfigOptions[ "Full Screen" ].Value == "Yes" );

                // create a default form window
                DefaultForm newWindow = _createDefaultForm( windowTitle, 0, 0, width, height, fullScreen );

                // create the render window
                renderWindow = CreateRenderWindow( "Main Window", width, height, bpp, fullScreen, 0, 0, true, false, newWindow );

                newWindow.Target.Visible = false;

                newWindow.Show();

                // set the default form's renderwindow so it can access it internally
                newWindow.RenderWindow = renderWindow;
            }

            return renderWindow;
        }

        public override void ApplyObliqueDepthProjection( ref Axiom.Math.Matrix4 projMatrix, Axiom.Math.Plane plane, bool forGpuProgram )
        {
            throw new NotImplementedException();
        }

        public override void BeginFrame()
        {
            _device.Clear( XnaF.Graphics.Color.CornflowerBlue );
        }

        public override void EndFrame()
        {
            _device.Present();
        }

        /// <summary>
        ///		Renders the current render operation in XNA's own special way.
        /// </summary>
        /// <param name="op"></param>
        public override void Render( RenderOperation op )
        {
            // don't even bother if there are no vertices to render, causes problems on some cards (FireGL 8800)
            if (op.vertexData.vertexCount == 0)
            {
                return;
            }

            // class base implementation first
            base.Render( op );

            // set the vertex declaration and buffer binding
            SetVertexDeclaration( op.vertexData.vertexDeclaration );
            SetVertexBufferBinding( op.vertexData.vertexBufferBinding );

            XnaF.Graphics.PrimitiveType primType = XnaF.Graphics.PrimitiveType.TriangleStrip;

            switch ( op.operationType )
            {
                case OperationType.PointList:
                    primType = XnaF.Graphics.PrimitiveType.PointList;
                    primCount = op.useIndices ? op.indexData.indexCount : op.vertexData.vertexCount;
                    break;
                case OperationType.LineList:
                    primType = XnaF.Graphics.PrimitiveType.LineList;
                    primCount = ( op.useIndices ? op.indexData.indexCount : op.vertexData.vertexCount ) / 2;
                    break;
                case OperationType.LineStrip:
                    primType = XnaF.Graphics.PrimitiveType.LineStrip;
                    primCount = ( op.useIndices ? op.indexData.indexCount : op.vertexData.vertexCount ) - 1;
                    break;
                case OperationType.TriangleList:
                    primType = XnaF.Graphics.PrimitiveType.TriangleList;
                    primCount = ( op.useIndices ? op.indexData.indexCount : op.vertexData.vertexCount ) / 3;
                    break;
                case OperationType.TriangleStrip:
                    primType = XnaF.Graphics.PrimitiveType.TriangleStrip;
                    primCount = ( op.useIndices ? op.indexData.indexCount : op.vertexData.vertexCount ) - 2;
                    break;
                case OperationType.TriangleFan:
                    primType = XnaF.Graphics.PrimitiveType.TriangleFan;
                    primCount = ( op.useIndices ? op.indexData.indexCount : op.vertexData.vertexCount ) - 2;
                    break;
            } // switch(primType)

            XnaF.Graphics.BasicEffect effect = new XnaF.Graphics.BasicEffect( _device, null );
            effect.DiffuseColor = new XnaF.Vector3( 1.0f, 1.0f, 1.0f );
            effect.View = _viewMatrix;
            effect.World = _worldMatrix;
            effect.Projection = XnaF.Matrix.CreatePerspectiveFieldOfView(
                                    XnaF.MathHelper.ToRadians( 179 ),
                                    (float)_device.Viewport.Width /
                                    (float)_device.Viewport.Height,
                                    1.0f, 100.0f );
            effect.Begin( XnaF.Graphics.SaveStateMode.None );
            foreach ( XnaF.Graphics.EffectPass pass in effect.CurrentTechnique.Passes )
            {
                pass.Begin();

                // are we gonna use indices?
                if ( op.useIndices )
                {
                    XnaHardwareIndexBuffer idxBuffer = (XnaHardwareIndexBuffer)op.indexData.indexBuffer;

                    // set the index buffer on the device
                    _device.Indices = idxBuffer.XnaIndexBuffer;

                    // draw the indexed primitives
                    _device.DrawIndexedPrimitives( primType, op.vertexData.vertexStart, 0, op.vertexData.vertexCount, op.indexData.indexStart, primCount );
                }
                else
                {

                    // draw vertices without indices
                    _device.DrawPrimitives( primType, op.vertexData.vertexStart, primCount );
                }


                pass.End();
            }
            effect.End();

        }

        public override void BindGpuProgram( Axiom.Graphics.GpuProgram program )
        {
            throw new NotImplementedException();
        }

        public override void UnbindGpuProgram( Axiom.Graphics.GpuProgramType type )
        {
            throw new NotImplementedException();
        }

        public override void BindGpuProgramParameters( Axiom.Graphics.GpuProgramType type, Axiom.Graphics.GpuProgramParameters parms )
        {
            throw new NotImplementedException();
        }

        public override void ClearFrameBuffer( Axiom.Graphics.FrameBuffer buffers, Axiom.Core.ColorEx color, float depth, int stencil )
        {
            throw new NotImplementedException();
        }

        public override int ConvertColor( Axiom.Core.ColorEx color )
        {
            return color.ToARGB();
        }

        public override Axiom.Graphics.IHardwareOcclusionQuery CreateHardwareOcclusionQuery()
        {
            throw new NotImplementedException();
        }

        public override Axiom.Graphics.RenderTexture CreateRenderTexture( string name, int width, int height )
        {
            throw new NotImplementedException();
        }

        public override Axiom.Graphics.RenderWindow CreateRenderWindow( string name, int width, int height, int colorDepth, bool isFullscreen, int left, int top, bool depthBuffer, bool vsync, object target )
        {
            if ( _device == null )
            {
                _device = _initDevice( isFullscreen, depthBuffer, width, height, colorDepth, (SWF.Control)target );
            }

            Axiom.Graphics.RenderWindow window = new XnaWindow();

            window.Handle = target;

            try
            {
                // create the window
                window.Create( name, width, height, colorDepth, isFullscreen, left, top, depthBuffer, (SWF.Control)target, _device );
            }
            catch ( XnaF.Graphics.OutOfVideoMemoryException e )
            {
                throw new Axiom.Core.AxiomException( "The graphics card memory has all be used up to an issue an video memory leak when restarting Axiom's Xna rendering system. Please restart the application.", e );
            }

            // add the new render target
            AttachRenderTarget( window );

            return window;
        }


        public override Axiom.Math.Matrix4 MakeOrthoMatrix( float fov, float aspectRatio, float near, float far, bool forGpuPrograms )
        {
            throw new NotImplementedException();
        }

        public override Axiom.Math.Matrix4 MakeProjectionMatrix( float fov, float aspectRatio, float near, float far, bool forGpuProgram )
        {
            float theta = Axiom.Math.Utility.DegreesToRadians( fov * 0.5f );
            float h = 1 / Axiom.Math.Utility.Tan( theta );
            float w = h / aspectRatio;
            float q = 0;
            float qn = 0;

            if ( far == 0 )
            {
                q = 1 - Axiom.Core.Frustum.InfiniteFarPlaneAdjust;
                qn = near * ( Axiom.Core.Frustum.InfiniteFarPlaneAdjust - 1 );
            }
            else
            {
                q = far / ( far - near );
                qn = -q * near;
            }

            Axiom.Math.Matrix4 dest = Axiom.Math.Matrix4.Zero;

            dest.m00 = w;
            dest.m11 = h;

            if ( forGpuProgram )
            {
                dest.m22 = -q;
                dest.m32 = -1.0f;
            }
            else
            {
                dest.m22 = q;
                dest.m32 = 1.0f;
            }

            dest.m23 = qn;

            return dest;
        }

        public override void SetAlphaRejectSettings( int stage, Axiom.Graphics.CompareFunction func, byte val )
        {
            // ToDo:
            // throw new NotImplementedException();
        }

        public override void SetColorBufferWriteEnabled( bool red, bool green, bool blue, bool alpha )
        {
            // ToDo:
            // throw new NotImplementedException();
        }

        public override void SetDepthBufferParams( bool depthTest, bool depthWrite, Axiom.Graphics.CompareFunction depthFunction )
        {
            throw new NotImplementedException();
        }

        public override void SetFog( Axiom.Graphics.FogMode mode, Axiom.Core.ColorEx color, float density, float start, float end )
        {
            // ToDo: 
        }

        public override void SetSceneBlending( Axiom.Graphics.SceneBlendFactor src, Axiom.Graphics.SceneBlendFactor dest )
        {
            // ToDo:
        }

        public override void SetScissorTest( bool enable, int left, int top, int right, int bottom )
        {
            throw new NotImplementedException();
        }

        public override void SetStencilBufferParams( Axiom.Graphics.CompareFunction function, int refValue, int mask, Axiom.Graphics.StencilOperation stencilFailOp, Axiom.Graphics.StencilOperation depthFailOp, Axiom.Graphics.StencilOperation passOp, bool twoSidedOperation )
        {
            throw new NotImplementedException();
        }

        public override void SetSurfaceParams( Axiom.Core.ColorEx ambient, Axiom.Core.ColorEx diffuse, Axiom.Core.ColorEx specular, Axiom.Core.ColorEx emissive, float shininess )
        {
            throw new NotImplementedException();
        }

        public override void SetTexture( int stage, bool enabled, string textureName )
        {
            XnaTexture texture = (XnaTexture)TextureManager.Instance.GetByName( textureName );

            if ( enabled && texture != null )
            {
                _device.Textures[ stage ] = texture.Texture;

                // set stage description
                texStageDesc[ stage ].tex = texture.Texture;
                texStageDesc[ stage ].texType = XnaHelper.ConvertEnum( texture.TextureType );
            }
            else
            {
                if ( texStageDesc[ stage ].tex != null )
                {
                    _device.Textures[ stage ] = null;
                    //device.TextureStates[ stage ].ColorOperation = D3D.TextureOperation.Disable;
                }

                // set stage description to defaults
                texStageDesc[ stage ].tex = null;
                texStageDesc[ stage ].autoTexCoordType = TexCoordCalcMethod.None;
                texStageDesc[ stage ].coordIndex = 0;
                texStageDesc[ stage ].texType = XnaTextureType.Normal;
            }
        }

        public override void SetTextureAddressingMode( int stage, Axiom.Graphics.TextureAddressing texAddressingMode )
        {
            // ToDo:
            // throw new NotImplementedException();
        }

        public override void SetTextureBlendMode( int stage, Axiom.Graphics.LayerBlendModeEx blendMode )
        {
            // ToDo:
            // throw new NotImplementedException();
        }

        public override void SetTextureCoordCalculation( int stage, Axiom.Graphics.TexCoordCalcMethod method, Axiom.Core.Frustum frustum )
        {
            // ToDo:
            // throw new NotImplementedException();
        }

        public override void SetTextureCoordSet( int stage, int index )
        {
            // store
            texStageDesc[ stage ].coordIndex = index;

            // ToDo:
            //_device.Textures[ stage ].TextureCoordinateIndex = D3DHelper.ConvertEnum( texStageDesc[ stage ].autoTexCoordType, d3dCaps ) | index;
        }

        public override void SetTextureLayerAnisotropy( int stage, int maxAnisotropy )
        {
            // ToDo:
            // throw new NotImplementedException();
        }

        public override void SetTextureMatrix( int stage, Axiom.Math.Matrix4 xform )
        {
            XnaF.Matrix xnaMatrix = XnaF.Matrix.Identity;
            Axiom.Math.Matrix4 newMatrix = xform;

            /* If envmap is applied, but device doesn't support spheremap,
            then we have to use texture transform to make the camera space normal
            reference the envmap properly. This isn't exactly the same as spheremap
            (it looks nasty on flat areas because the camera space normals are the same)
            but it's the best approximation we have in the absence of a proper spheremap */
            if ( texStageDesc[ stage ].autoTexCoordType == TexCoordCalcMethod.EnvironmentMap )
            {
                if ( _capabilities.VertexProcessingCapabilities.SupportsTextureGenerationSphereMap )
                {
                    // inverts the texture for a spheremap
                    Axiom.Math.Matrix4 matEnvMap = Axiom.Math.Matrix4.Identity;
                    matEnvMap.m11 = -1.0f;

                    // concatenate 
                    newMatrix = newMatrix * matEnvMap;
                }
                else
                {
                    /* If envmap is applied, but device doesn't support spheremap,
                    then we have to use texture transform to make the camera space normal
                    reference the envmap properly. This isn't exactly the same as spheremap
                    (it looks nasty on flat areas because the camera space normals are the same)
                    but it's the best approximation we have in the absence of a proper spheremap */

                    // concatenate with the xForm
                    newMatrix = newMatrix * Axiom.Math.Matrix4.ClipSpace2DToImageSpace;
                }
            }

            // If this is a cubic reflection, we need to modify using the view matrix
            if ( texStageDesc[ stage ].autoTexCoordType == TexCoordCalcMethod.EnvironmentMapReflection )
            {
                // get the current view matrix
                XnaF.Matrix viewMatrix = _viewMatrix;
                
                // Get transposed 3x3, ie since D3D is transposed just copy
                // We want to transpose since that will invert an orthonormal matrix ie rotation
                Axiom.Math.Matrix4 viewTransposed = Axiom.Math.Matrix4.Identity;
                viewTransposed.m00 = viewMatrix.M11;
                viewTransposed.m01 = viewMatrix.M12;
                viewTransposed.m02 = viewMatrix.M13;
                viewTransposed.m03 = 0.0f;

                viewTransposed.m10 = viewMatrix.M21;
                viewTransposed.m11 = viewMatrix.M22;
                viewTransposed.m12 = viewMatrix.M23;
                viewTransposed.m13 = 0.0f;

                viewTransposed.m20 = viewMatrix.M31;
                viewTransposed.m21 = viewMatrix.M32;
                viewTransposed.m22 = viewMatrix.M33;
                viewTransposed.m23 = 0.0f;

                viewTransposed.m30 = viewMatrix.M41;
                viewTransposed.m31 = viewMatrix.M42;
                viewTransposed.m32 = viewMatrix.M43;
                viewTransposed.m33 = 1.0f;

                // concatenate
                newMatrix = newMatrix * viewTransposed;
            }
            /*
            if ( texStageDesc[ stage ].autoTexCoordType == TexCoordCalcMethod.ProjectiveTexture )
            {
                // Derive camera space to projector space transform
                // To do this, we need to undo the camera view matrix, then 
                // apply the projector view & projection matrices
                newMatrix = viewMatrix.Inverse() * newMatrix;
                newMatrix = texStageDesc[ stage ].frustum.ViewMatrix * newMatrix;
                newMatrix = texStageDesc[ stage ].frustum.ProjectionMatrix * newMatrix;

                if ( texStageDesc[ stage ].frustum.ProjectionType == Projection.Perspective )
                {
                    newMatrix = ProjectionClipSpace2DToImageSpacePerspective * newMatrix;
                }
                else
                {
                    newMatrix = ProjectionClipSpace2DToImageSpaceOrtho * newMatrix;
                }

            }
            */

            // convert to Xna format
            xnaMatrix = _makeXnaMatrix( newMatrix );

            // need this if texture is a cube map, to invert Xna's z coord
            if ( texStageDesc[ stage ].autoTexCoordType != TexCoordCalcMethod.None )
            {
                xnaMatrix.M13 = -xnaMatrix.M13;
                xnaMatrix.M23 = -xnaMatrix.M23;
                xnaMatrix.M33 = -xnaMatrix.M33;
                xnaMatrix.M43 = -xnaMatrix.M43;
            }

            //XnaF.Graphics..TransformType d3dTransType = (D3D.TransformType)( (int)( D3D.TransformType.Texture0 ) + stage );
            /*
            // set the matrix if it is not the identity
            if ( !XnaHelper.IsIdentity( ref xnaMatrix ) )
            {
                // tell Xna the dimension of tex. coord
                int texCoordDim = 0;
                if ( texStageDesc[ stage ].autoTexCoordType == TexCoordCalcMethod.ProjectiveTexture )
                {
                    texCoordDim = (int)D3D.TextureTransform.Projected | (int)D3D.TextureTransform.Count3;
                }
                else
                {
                    switch ( texStageDesc[ stage ].texType )
                    {
                        case XnaTextureType.Normal:
                            texCoordDim = (int)D3D.TextureTransform.Count2;
                            break;
                        case XnaTextureType.Cube:
                        case XnaTextureType.Volume:
                            texCoordDim = (int)D3D.TextureTransform.Count3;
                            break;
                    }
                }

                // note: int values of D3D.TextureTransform correspond directly with tex dimension, so direct conversion is possible
                // i.e. Count1 = 1, Count2 = 2, etc
                device.TextureState[ stage ].TextureTransform = (D3D.TextureTransform)texCoordDim;

                // set the manually calculated texture matrix
                device.SetTransform( d3dTransType, d3dMat );
            }
            else
            {
                // disable texture transformation
                device.TextureState[ stage ].TextureTransform = D3D.TextureTransform.Disable;

                // set as the identity matrix
                device.SetTransform( d3dTransType, DX.Matrix.Identity );
            }
            */
        }

        public override void SetTextureUnitFiltering( int stage, Axiom.Graphics.FilterType type, Axiom.Graphics.FilterOptions filter )
        {
            // ToDo:
            // throw new NotImplementedException();
        }

        public override void SetViewport( Axiom.Core.Viewport viewport )
        {
            if ( activeViewport != viewport || viewport.IsUpdated )
            {
                // store this viewport and it's target
                activeViewport = viewport;
                activeRenderTarget = viewport.Target;

                // get the back buffer surface for this viewport
                XnaF.Graphics.RenderTarget2D back = (XnaF.Graphics.RenderTarget2D)activeRenderTarget.GetCustomAttribute( "BACKBUFFER" );
                _device.SetRenderTarget( 0, back );

                // we cannot dipose of the back buffer in fullscreen mode, since we have a direct reference to
                // the main back buffer.  all other surfaces are safe to dispose
                bool disposeBackBuffer = true;

                if ( activeRenderTarget is XnaWindow )
                {
                    XnaWindow window = activeRenderTarget as XnaWindow;

                    if ( window.IsFullScreen )
                    {
                        disposeBackBuffer = false;
                    }
                }

                // be sure to destroy the surface we had
                if ( disposeBackBuffer )
                {
                    //back.Dispose();
                }

                XnaF.Graphics.DepthStencilBuffer depth = (XnaF.Graphics.DepthStencilBuffer)activeRenderTarget.GetCustomAttribute( "DEPTHBUFFER" );

                // set the render target and depth stencil for the surfaces beloning to the viewport
                _device.DepthStencilBuffer = depth;

                // set the culling mode, to make adjustments required for viewports
                // that may need inverted vertex winding or texture flipping
                this.CullingMode = cullingMode;

                XnaF.Graphics.Viewport vp = new XnaF.Graphics.Viewport();

                // set viewport dimensions
                vp.X = viewport.ActualLeft;
                vp.Y = viewport.ActualTop;
                vp.Width = viewport.ActualWidth;
                vp.Height = viewport.ActualHeight;

                // Z-values from 0.0 to 1.0 (TODO: standardize with OpenGL)
                vp.MinDepth = 0.0f;
                vp.MaxDepth = 1.0f;

                // set the current XNA viewport
                _device.Viewport = vp;

                // clear the updated flag
                viewport.IsUpdated = false;
            }
        }

        public override void UseLights( Axiom.Collections.LightList lightList, int limit )
        {
            throw new NotImplementedException();
        }

        #endregion Methods
			
        #endregion Axiom.Graphics.RenderSystem Implementation
    }
}
