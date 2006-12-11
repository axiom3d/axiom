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
using XnaF = Microsoft.Xna.Framework;
using Axiom.Graphics;
using Axiom.Core;

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

        protected bool lightingEnabled;

        // stores texture stage info locally for convenience
        internal XnaTextureStageDescription[] texStageDesc = new XnaTextureStageDescription[ Config.MaxTextureLayers ];

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

            if ( fullScreen )
            {
                form.Top = 0;
                form.Left = 0;
                form.FormBorderStyle = SWF.FormBorderStyle.None;
                form.WindowState = SWF.FormWindowState.Maximized;
                form.TopMost = true;
                form.TopLevel = true;
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

        #region Axiom.Graphics.RenderSystem Implementation

        public override Axiom.Core.ColorEx AmbientLight
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

        public override float VerticalTexelOffset
        {
            get
            {
                // Xna considers the origin to be in the center of a pixel
                return -0.5f;
            }
        }

        public override Axiom.Math.Matrix4 ViewMatrix
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

        public override Axiom.Math.Matrix4 WorldMatrix
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

        public override float HorizontalTexelOffset
        {
            get
            {
                // Xna considers the origin to be in the center of a pixel
                return -0.5f;
            }
        }

        public override bool LightingEnabled
        {
            get
            {
                return lightingEnabled;
            }
            set
            {
                lightingEnabled = value;
            }
        }

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
        ///		Renders the current render operation in D3D's own special way.
        /// </summary>
        /// <param name="op"></param>
        public override void Render( RenderOperation op )
        {
            _device.Clear( XnaF.Graphics.Color.CornflowerBlue );
            _device.Present();
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
           // _device.Textures[ stage ].TextureCoordinateIndex = D3DHelper.ConvertEnum( texStageDesc[ stage ].autoTexCoordType, d3dCaps ) | index;
        }

        public override void SetTextureLayerAnisotropy( int stage, int maxAnisotropy )
        {
            // ToDo:
            // throw new NotImplementedException();
        }

        public override void SetTextureMatrix( int stage, Axiom.Math.Matrix4 xform )
        {
            // ToDo:
            // throw new NotImplementedException();
        }

        public override void SetTextureUnitFiltering( int stage, Axiom.Graphics.FilterType type, Axiom.Graphics.FilterOptions filter )
        {
            // ToDo:
            // throw new NotImplementedException();
        }

        public override void SetViewport( Axiom.Core.Viewport viewport )
        {
            //ToDo: Need to Implement
            //throw new NotImplementedException();
        }

        public override void UseLights( Axiom.Collections.LightList lightList, int limit )
        {
            throw new NotImplementedException();
        }

        #endregion Axiom.Graphics.RenderSystem Implementation
    }
}
