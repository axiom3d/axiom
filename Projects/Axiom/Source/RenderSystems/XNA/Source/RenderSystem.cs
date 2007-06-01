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
using System.Diagnostics;
using System.Drawing;
using SWF = System.Windows.Forms;

using Axiom.Configuration;
using Axiom.Graphics;
using Axiom.Core;
using Axiom.Math;

using XNA = Microsoft.Xna.Framework;
using XFG = Microsoft.Xna.Framework.Graphics;
using Axiom.Collections;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.Xna
{
    public sealed class RenderSystem : Axiom.Graphics.RenderSystem
    {
        public static readonly Matrix4 ProjectionClipSpace2DToImageSpacePerspective = new Matrix4(
            0.5f, 0, 0, -0.5f,
            0, -0.5f, 0, -0.5f,
            0, 0, 0, 1f,
            0, 0, 0, 1f );

        public static readonly Matrix4 ProjectionClipSpace2DToImageSpaceOrtho = new Matrix4(
            -0.5f, 0, 0, -0.5f,
            0, 0.5f, 0, -0.5f,
            0, 0, 0, 1f,
            0, 0, 0, 1f );

        /// <summary>
        ///    Reference to the Xna device.
        /// </summary>
        private XFG.GraphicsDevice _device;

        private XFG.GraphicsDeviceCapabilities _capabilities;

        // stores texture stage info locally for convenience
        internal XnaTextureStageDescription[] texStageDesc = new XnaTextureStageDescription[Config.MaxTextureLayers];

        /// <summary>
        ///    Number of streams used last frame, used to unbind any buffers not used during the current operation.
        /// </summary>
        private int _numLastStreams;

        /// <summary>
        ///    Signifies whether the current frame being rendered is the first.
        /// </summary>
        private bool _isFirstFrame = true;

        private int _primCount;
        private int _renderCount = 0;

		/// <summary>
		/// 
		/// </summary>
		private int _lightCount;

        private XnaGpuProgramManager _gpuProgramMgr;

        public RenderSystem()
        {
            _initConfigOptions();

            // init the texture stage descriptions
            for ( int i = 0; i < Config.MaxTextureLayers; i++ )
            {
                texStageDesc[i].autoTexCoordType = TexCoordCalcMethod.None;
                texStageDesc[i].coordIndex = 0;
                texStageDesc[i].texType = XnaTextureType.Normal;
                texStageDesc[i].tex = null;
            }
        }

        #region Private Helper Functions

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
            //form.BringToFront();
            form.TopLevel = true;
            form.TopMost = false;

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

        private XFG.PresentationParameters _createPresentationParams( bool isFullscreen, bool depthBuffer, int width, int height, int colorDepth )
        {
            // if this is the first window, get the device and do other initialization

            XFG.PresentationParameters presentParams = new XFG.PresentationParameters();
            presentParams.IsFullScreen = isFullscreen;
            presentParams.BackBufferCount = 0;
            presentParams.EnableAutoDepthStencil = depthBuffer;
            presentParams.BackBufferWidth = width;
            presentParams.BackBufferHeight = height;
            presentParams.MultiSampleType = XFG.MultiSampleType.None;
            presentParams.SwapEffect = XFG.SwapEffect.Discard;

            // TODO: Check vsync setting
            presentParams.PresentationInterval = XFG.PresentInterval.Immediate;

            // supports 16 and 32 bit color
            if ( colorDepth == 16 )
            {
                presentParams.BackBufferFormat = XFG.SurfaceFormat.Bgr565;
            }
            else
            {
                presentParams.BackBufferFormat = XFG.SurfaceFormat.Bgr32;
            }

            if ( colorDepth > 16 )
            {
                // check for 24 bit Z buffer with 8 bit stencil (optimal choice)
                if ( !XFG.GraphicsAdapter.DefaultAdapter.CheckDeviceFormat( XFG.DeviceType.Hardware, presentParams.BackBufferFormat, XFG.ResourceUsage.None, XFG.QueryUsages.None, XFG.ResourceType.Texture2D, XFG.DepthFormat.Depth24Stencil8 ) )
                {
                    // doh, check for 32 bit Z buffer then
                    if ( !XFG.GraphicsAdapter.DefaultAdapter.CheckDeviceFormat( XFG.DeviceType.Hardware, presentParams.BackBufferFormat, XFG.ResourceUsage.None, XFG.QueryUsages.None, XFG.ResourceType.Texture2D, XFG.DepthFormat.Depth32 ) )
                    {
                        // float doh, just use 16 bit Z buffer
                        presentParams.AutoDepthStencilFormat = XFG.DepthFormat.Depth16;
                    }
                    else
                    {
                        // use 32 bit Z buffer
                        presentParams.AutoDepthStencilFormat = XFG.DepthFormat.Depth32;
                    }
                }
                else
                {
                    if ( XFG.GraphicsAdapter.DefaultAdapter.CheckDeviceFormat( XFG.DeviceType.Hardware, presentParams.BackBufferFormat, XFG.ResourceUsage.None, XFG.QueryUsages.None, XFG.ResourceType.Texture2D, XFG.DepthFormat.Depth24Stencil8 ) )
                    {
                        presentParams.AutoDepthStencilFormat = XFG.DepthFormat.Depth24Stencil8;
                    }
                    else
                    {
                        presentParams.AutoDepthStencilFormat = XFG.DepthFormat.Depth24;
                    }
                }
            }
            else
            {
                // use 16 bit Z buffer if they arent using true color
                presentParams.AutoDepthStencilFormat = XFG.DepthFormat.Depth16;
            }
            presentParams.AutoDepthStencilFormat = XFG.DepthFormat.Depth24Stencil8;

            return presentParams;
        }

        /// <summary>
        ///		Helper method to go through and interrogate hardware capabilities.
        /// </summary>
        private void _checkCaps( XFG.GraphicsDevice device )
        {
            // get the number of possible texture units
            caps.TextureUnitCount = _capabilities.MaxSimultaneousTextures;

            // max active lights
            caps.MaxLights = 8; // d3dCaps.MaxActiveLights;

            XFG.DepthStencilBuffer surfaceDesc = device.DepthStencilBuffer;

            if ( surfaceDesc.Format == XFG.DepthFormat.Depth24Stencil8 || surfaceDesc.Format == XFG.DepthFormat.Depth24Stencil8Single )
            {
                caps.SetCap( Capabilities.StencilBuffer );
                // always 8 here
                caps.StencilBufferBits = 8;
            }

            // some cards, oddly enough, do not support this
            if ( _capabilities.DeclarationTypeCapabilities.SupportsByte4 )
            {
                caps.SetCap( Capabilities.VertexFormatUByte4 );
            }

            // Anisotropy?
            if ( _capabilities.MaxAnisotropy > 1 )
            {
                caps.SetCap( Capabilities.AnisotropicFiltering );
            }

            // Hardware mipmapping?
            if ( _capabilities.DriverCapabilities.CanAutoGenerateMipMap )
            {
                caps.SetCap( Capabilities.HardwareMipMaps );
            }

            // blending between stages is definately supported
            caps.SetCap( Capabilities.TextureBlending );
            caps.SetCap( Capabilities.MultiTexturing );

            // Dot3 bump mapping?
            //if ( _capabilities.TextureCapabilities.SupportsDotProduct3 )
            //{
            //    caps.SetCap( Capabilities.Dot3 );
            //}

            // Cube mapping?
            if ( _capabilities.TextureCapabilities.SupportsCubeMap )
            {
                caps.SetCap( Capabilities.CubeMapping );
            }

            // Texture Compression
            // We always support compression, Xna will decompress if device does not support
            caps.SetCap( Capabilities.TextureCompression );
            caps.SetCap( Capabilities.TextureCompressionDXT );

            // Xna uses vertex buffers for everything
            caps.SetCap( Capabilities.VertexBuffer );

            // Scissor test
            if ( _capabilities.RasterCapabilities.SupportsScissorTest )
            {
                caps.SetCap( Capabilities.ScissorTest );
            }

            // 2 sided stencil
            if ( _capabilities.StencilCapabilities.SupportsTwoSided )
            {
                caps.SetCap( Capabilities.TwoSidedStencil );
            }

            // stencil wrap
            if ( _capabilities.StencilCapabilities.SupportsIncrement && _capabilities.StencilCapabilities.SupportsDecrement )
            {
                caps.SetCap( Capabilities.StencilWrap );
            }

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

            if ( _capabilities.MaxUserClipPlanes > 0 )
            {
                caps.SetCap( Capabilities.UserClipPlanes );
            }

            _checkVertexProgramCapabilities();

            _checkFragmentProgramCapabilities();

            // Infinite projection?
            // We have no capability for this, so we have to base this on our
            // experience and reports from users
            // Non-vertex program capable hardware does not appear to support it
            if ( caps.CheckCap( Capabilities.VertexPrograms ) )
            {
                // GeForce4 Ti (and presumably GeForce3) does not
                // render infinite projection properly, even though it does in GL
                // So exclude all cards prior to the FX range from doing infinite

                XFG.GraphicsAdapter details = XFG.GraphicsAdapter.Adapters[ 0 ];

                // not nVidia or GeForceFX and above
                if ( details.VendorId != 0x10DE || details.DeviceId >= 0x0301 )
                {
                    caps.SetCap( Capabilities.InfiniteFarPlane );
                }
            }

            // write hardware capabilities to registered log listeners
            caps.Log();
        }

        private void _checkFragmentProgramCapabilities()
        {
            int fpMajor = _capabilities.PixelShaderVersion.Major;
            int fpMinor = _capabilities.PixelShaderVersion.Minor;

            switch ( fpMajor )
            {
                case 1:
                    caps.MaxFragmentProgramVersion = string.Format( "ps_1_{0}", fpMinor );

                    caps.FragmentProgramConstantIntCount = 0;
                    // 8 4d float values, entered as floats but stored as fixed
                    caps.FragmentProgramConstantFloatCount = 8;
                    break;

                case 2:
                    if ( fpMinor > 0 )
                    {
                        caps.MaxFragmentProgramVersion = "ps_2_x";
                        //16 integer params allowed
                        caps.FragmentProgramConstantIntCount = 16 * 4;
                        // 4d float params
                        caps.FragmentProgramConstantFloatCount = 224;
                    }
                    else
                    {
                        caps.MaxFragmentProgramVersion = "ps_2_0";
                        // no integer params allowed
                        caps.FragmentProgramConstantIntCount = 0;
                        // 4d float params
                        caps.FragmentProgramConstantFloatCount = 32;
                    }

                    break;

                case 3:
                    if ( fpMinor > 0 )
                    {
                        caps.MaxFragmentProgramVersion = "ps_3_x";
                    }
                    else
                    {
                        caps.MaxFragmentProgramVersion = "ps_3_0";
                    }

                    // 16 integer params allowed
                    caps.FragmentProgramConstantIntCount = 16;
                    caps.FragmentProgramConstantFloatCount = 224;
                    break;

                default:
                    // doh, SOL
                    caps.MaxFragmentProgramVersion = "";
                    break;
            }

            // Fragment Program syntax code checks
            if ( fpMajor >= 1 )
            {
                caps.SetCap( Capabilities.FragmentPrograms );
                _gpuProgramMgr.PushSyntaxCode( "ps_1_1" );

                if ( fpMajor > 1 || fpMinor >= 2 )
                {
                    _gpuProgramMgr.PushSyntaxCode( "ps_1_2" );
                }
                if ( fpMajor > 1 || fpMinor >= 3 )
                {
                    _gpuProgramMgr.PushSyntaxCode( "ps_1_3" );
                }
                if ( fpMajor > 1 || fpMinor >= 4 )
                {
                    _gpuProgramMgr.PushSyntaxCode( "ps_1_4" );
                }
            }

            if ( fpMajor >= 2 )
            {
                _gpuProgramMgr.PushSyntaxCode( "ps_2_0" );

                if ( fpMinor > 0 )
                {
                    _gpuProgramMgr.PushSyntaxCode( "ps_2_x" );
                }
            }

            if ( fpMajor >= 3 )
            {
                _gpuProgramMgr.PushSyntaxCode( "ps_3_0" );

                if ( fpMinor > 0 )
                {
                    _gpuProgramMgr.PushSyntaxCode( "ps_3_x" );
                }
            }
        }

        private void _checkVertexProgramCapabilities()
        {
            int vpMajor = _capabilities.VertexShaderVersion.Major;
            int vpMinor = _capabilities.VertexShaderVersion.Minor;

            // check vertex program caps
            switch ( vpMajor )
            {
                case 1:
                    caps.MaxVertexProgramVersion = "vs_1_1";
                    // 4d float vectors
                    caps.VertexProgramConstantFloatCount = _capabilities.MaxVertexShaderConstants;
                    // no int params supports
                    caps.VertexProgramConstantIntCount = 0;
                    break;
                case 2:
                    if ( vpMinor > 0 )
                    {
                        caps.MaxVertexProgramVersion = "vs_2_x";
                    }
                    else
                    {
                        caps.MaxVertexProgramVersion = "vs_2_0";
                    }

                    // 16 ints
                    caps.VertexProgramConstantIntCount = 16 * 4;
                    // 4d float vectors
                    caps.VertexProgramConstantFloatCount = _capabilities.MaxVertexShaderConstants;

                    break;
                case 3:
                    caps.MaxVertexProgramVersion = "vs_3_0";

                    // 16 ints
                    caps.VertexProgramConstantIntCount = 16 * 4;
                    // 4d float vectors
                    caps.VertexProgramConstantFloatCount = _capabilities.MaxVertexShaderConstants;

                    break;
                default:
                    // not gonna happen
                    caps.MaxVertexProgramVersion = "";
                    break;
            }

            // check for supported vertex program syntax codes
            if ( vpMajor >= 1 )
            {
                caps.SetCap( Capabilities.VertexPrograms );
                _gpuProgramMgr.PushSyntaxCode( "vs_1_1" );
            }
            if ( vpMajor >= 2 )
            {
                if ( vpMajor > 2 || vpMinor > 0 )
                {
                    _gpuProgramMgr.PushSyntaxCode( "vs_2_x" );
                }
                _gpuProgramMgr.PushSyntaxCode( "vs_2_0" );
            }
            if ( vpMajor >= 3 )
            {
                _gpuProgramMgr.PushSyntaxCode( "vs_3_0" );
            }
        }

        private XFG.GraphicsDevice _initDevice( bool isFullscreen, bool depthBuffer, int width, int height, int colorDepth, SWF.Control target )
        {
            if ( _device != null )
            {
                return _device;
            }

            XFG.GraphicsDevice newDevice = null;

            XFG.PresentationParameters presentParams = _createPresentationParams( isFullscreen, depthBuffer, width, height, colorDepth );

            // create the Xna Device, trying for the best vertex support first, and settling for less if necessary
            XFG.DeviceType type = XFG.DeviceType.Hardware;
            XFG.GraphicsAdapter adapter = XFG.GraphicsAdapter.DefaultAdapter;
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
                newDevice = new XFG.GraphicsDevice( adapter, type, target.Handle, XFG.CreateOptions.HardwareVertexProcessing, presentParams );
            }
            catch ( Exception )
            {
                try
                {
                    // doh, how bout mixed vertex processing
                    newDevice = new XFG.GraphicsDevice( adapter, type, target.Handle, XFG.CreateOptions.MixedVertexProcessing, presentParams );
                }
                catch ( Exception )
                {
                    // what the...ok, how bout software vertex procssing.  if this fails, then I don't even know how they are seeing
                    // anything at all since they obviously don't have a video card installed
                    newDevice = new XFG.GraphicsDevice( adapter, XFG.DeviceType.Reference, target.Handle, XFG.CreateOptions.SoftwareVertexProcessing, presentParams );
                }
            }

            // save the device capabilites
            _capabilities = newDevice.GraphicsDeviceCapabilities;

            // by creating our texture manager, singleton TextureManager will hold our implementation
            textureMgr = new XnaTextureManager( newDevice );

            // by creating our Gpu program manager, singleton GpuProgramManager will hold our implementation
            _gpuProgramMgr = new XnaGpuProgramManager( newDevice );

            // intializes the HardwareBufferManager singleton
            hardwareBufferManager = new XnaHardwareBufferManager( newDevice );

            _checkCaps( newDevice );

            return newDevice;
        }

        /// <summary>
        /// 
        /// </summary>
        private void _setVertexBufferBinding( VertexBufferBinding binding )
        {
            IEnumerator e = binding.Bindings;

            // TODO: Optimize to remove enumeration if possible, although with so few iterations it may never make a difference
            while ( e.MoveNext() )
            {
                DictionaryEntry entry = (DictionaryEntry)e.Current;
                XnaHardwareVertexBuffer buffer = (XnaHardwareVertexBuffer)entry.Value;

                short stream = (short)entry.Key;

                _device.Vertices[stream].SetSource( buffer.XnaVertexBuffer, 0, buffer.VertexSize );

                _numLastStreams++;
            }

            // Unbind any unused sources
            for ( int i = binding.BindingCount; i < _numLastStreams; i++ )
            {
                _device.Vertices[i].SetSource( null, 0, 0 );
            }

            _numLastStreams = binding.BindingCount;
        }

        /// <summary>
        ///		Helper method for setting the current vertex declaration.
        /// </summary>
        private void _setVertexDeclaration( VertexDeclaration decl )
        {
            // TODO: Check for duplicate setting and avoid setting if dupe
            XnaVertexDeclaration vertDecl = (XnaVertexDeclaration)decl;

            _device.VertexDeclaration = vertDecl.XnaVertexDecl;
        }

        private XNA.Matrix _makeXnaMatrix( Axiom.Math.Matrix4 matrix )
        {
            XNA.Matrix xna = new XNA.Matrix();

            xna.M11 = matrix.m00;
            xna.M12 = matrix.m01;
            xna.M13 = matrix.m02;
            xna.M14 = matrix.m03;
            xna.M21 = matrix.m10;
            xna.M22 = matrix.m11;
            xna.M23 = matrix.m12;
            xna.M24 = matrix.m13;
            xna.M31 = matrix.m20;
            xna.M32 = matrix.m21;
            xna.M33 = matrix.m22;
            xna.M34 = matrix.m23;
            xna.M41 = matrix.m30;
            xna.M42 = matrix.m31;
            xna.M43 = matrix.m32;
            xna.M44 = matrix.m33;

            return xna;
        }

        #endregion Private Helper Functions

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
        private XNA.Matrix _viewMatrix = XNA.Matrix.Identity;
        public override Axiom.Math.Matrix4 ViewMatrix
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                _viewMatrix = _makeXnaMatrix( value );
            }
        }

        private XNA.Matrix _worldMatrix = XNA.Matrix.Identity;
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

                ConfigOption optVM = ConfigOptions["Video Mode"];
                string vm = optVM.Value;
                width = int.Parse( vm.Substring( 0, vm.IndexOf( "x" ) ) );
                height = int.Parse( vm.Substring( vm.IndexOf( "x" ) + 1, vm.IndexOf( "@" ) - ( vm.IndexOf( "x" ) + 1 ) ) );
                bpp = int.Parse( vm.Substring( vm.IndexOf( "@" ) + 1, vm.IndexOf( "-" ) - ( vm.IndexOf( "@" ) + 1 ) ) );

                fullScreen = ( ConfigOptions["Full Screen"].Value == "Yes" );

                // create a default form window
                DefaultForm newWindow = _createDefaultForm( windowTitle, 0, 0, width, height, fullScreen );

				NamedParameterList miscParams = new NamedParameterList();
				miscParams.Add( "title", windowTitle );
				miscParams.Add( "colorDepth", bpp );
				//miscParams.Add( "FSAA", _fsaaType );
				//miscParams.Add( "FSAAQuality", _fsaaQuality );
				//miscParams.Add( "vsync", _vSync );
				//miscParams.Add( "useNVPerfHUD", _useNVPerfHUD );

				// create the render window
				renderWindow = CreateRenderWindow( "Main Window", width, height, fullScreen, miscParams );

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
            Debug.Assert( activeViewport != null, "BeginFrame cannot run without an active viewport." );

            // clear the device if need be
            if ( activeViewport.GetClearEveryFrame() )
            {
                ClearFrameBuffer( FrameBuffer.Color | FrameBuffer.Depth, activeViewport.BackgroundColor );
            }

            // set initial render states if this is the first frame. we only want to do 
            //	this once since renderstate changes are expensive
            if ( _isFirstFrame )
            {
                // enable alpha blending 
                _device.RenderState.AlphaBlendEnable = true;

                _isFirstFrame = false;
            }
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
            if ( op.vertexData.vertexCount == 0 )
            {
                return;
            }

            // class base implementation first
            base.Render( op );

            // set the vertex declaration and buffer binding
            _setVertexDeclaration( op.vertexData.vertexDeclaration );
            _setVertexBufferBinding( op.vertexData.vertexBufferBinding );

            XFG.PrimitiveType primType = XFG.PrimitiveType.TriangleStrip;

            switch ( op.operationType )
            {
                case OperationType.PointList:
                    primType = XFG.PrimitiveType.PointList;
                    _primCount = op.useIndices ? op.indexData.indexCount : op.vertexData.vertexCount;
                    break;
                case OperationType.LineList:
                    primType = XFG.PrimitiveType.LineList;
                    _primCount = ( op.useIndices ? op.indexData.indexCount : op.vertexData.vertexCount ) / 2;
                    break;
                case OperationType.LineStrip:
                    primType = XFG.PrimitiveType.LineStrip;
                    _primCount = ( op.useIndices ? op.indexData.indexCount : op.vertexData.vertexCount ) - 1;
                    break;
                case OperationType.TriangleList:
                    primType = XFG.PrimitiveType.TriangleList;
                    _primCount = ( op.useIndices ? op.indexData.indexCount : op.vertexData.vertexCount ) / 3;
                    break;
                case OperationType.TriangleStrip:
                    primType = XFG.PrimitiveType.TriangleStrip;
                    _primCount = ( op.useIndices ? op.indexData.indexCount : op.vertexData.vertexCount ) - 2;
                    break;
                case OperationType.TriangleFan:
                    primType = XFG.PrimitiveType.TriangleFan;
                    _primCount = ( op.useIndices ? op.indexData.indexCount : op.vertexData.vertexCount ) - 2;
                    break;
            } // switch(primType)

            // are we gonna use indices?
            if ( op.useIndices )
            {
                XnaHardwareIndexBuffer idxBuffer = (XnaHardwareIndexBuffer)op.indexData.indexBuffer;

                // set the index buffer on the device
                _device.Indices = idxBuffer.XnaIndexBuffer;

                // draw the indexed primitives
                _device.DrawIndexedPrimitives( primType, op.vertexData.vertexStart, 0, op.vertexData.vertexCount, op.indexData.indexStart, _primCount );
            }
            else
            {

                // draw vertices without indices
                _device.DrawPrimitives( primType, op.vertexData.vertexStart, _primCount );
            }

        }

        public override void BindGpuProgram( Axiom.Graphics.GpuProgram program )
        {
            switch ( program.Type )
            {
                case GpuProgramType.Vertex:
                    _device.VertexShader = ( (XnaVertexProgram)program ).VertexShader;
                    break;

                case GpuProgramType.Fragment:
                    _device.PixelShader = ( (XNAragmentProgram)program ).PixelShader;
                    break;
            }
        }

        public override void UnbindGpuProgram( Axiom.Graphics.GpuProgramType type )
        {
            switch ( type )
            {
                case GpuProgramType.Vertex:
                    _device.VertexShader = null;
                    break;

                case GpuProgramType.Fragment:
                    _device.PixelShader = null;
                    break;
            }
        }

        public override void BindGpuProgramParameters( Axiom.Graphics.GpuProgramType type, Axiom.Graphics.GpuProgramParameters parms )
        {
            switch ( type )
            {
                case GpuProgramType.Vertex:
                    if ( parms.HasIntConstants )
                    {
                        for ( int index = 0; index < parms.FloatConstantCount; index++ )
                        {
                            GpuProgramParameters.IntConstantEntry entry = parms.GetIntConstant( index );

                            if ( entry.isSet )
                            {
                                _device.SetVertexShaderConstant( index, entry.val );
                            }
                        }
                    }

                    if ( parms.HasFloatConstants )
                    {
                        for ( int index = 0; index < parms.FloatConstantCount; index++ )
                        {
                            GpuProgramParameters.FloatConstantEntry entry = parms.GetFloatConstant( index );

                            if ( entry.isSet )
                            {
                                _device.SetVertexShaderConstant( index, entry.val );
                            }
                        }
                    }

                    break;

                case GpuProgramType.Fragment:
                    if ( parms.HasIntConstants )
                    {
                        for ( int index = 0; index < parms.FloatConstantCount; index++ )
                        {
                            GpuProgramParameters.IntConstantEntry entry = parms.GetIntConstant( index );

                            if ( entry.isSet )
                            {
                                _device.SetPixelShaderConstant( index, entry.val );
                            }
                        }
                    }

                    if ( parms.HasFloatConstants )
                    {
                        for ( int index = 0; index < parms.FloatConstantCount; index++ )
                        {
                            GpuProgramParameters.FloatConstantEntry entry = parms.GetFloatConstant( index );

                            if ( entry.isSet )
                            {
                                _device.SetPixelShaderConstant( index, entry.val );
                            }
                        }
                    }
                    break;
            }
        }

        public override void ClearFrameBuffer( Axiom.Graphics.FrameBuffer buffers, Axiom.Core.ColorEx color, float depth, int stencil )
        {
            XFG.ClearOptions flags = 0;

            if ( ( buffers & FrameBuffer.Color ) > 0 )
            {
                flags |= XFG.ClearOptions.Target;
            }
            if ( ( buffers & FrameBuffer.Depth ) > 0 )
            {
                flags |= XFG.ClearOptions.DepthBuffer;
            }
            // Only try to clear the stencil buffer if supported
            if ( ( buffers & FrameBuffer.Stencil ) > 0
                && caps.CheckCap( Capabilities.StencilBuffer ) )
            {

                flags |= XFG.ClearOptions.Stencil;
            }

            // clear the device using the specified params
            _device.Clear( flags, XnaHelper.ConvertColorEx( color ), depth, stencil );
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

        //public override Axiom.Graphics.RenderWindow CreateRenderWindow( string name, int width, int height, int colorDepth, bool isFullscreen, int left, int top, bool depthBuffer, bool vsync, object target )
		public override RenderWindow CreateRenderWindow( string name, int width, int height, bool isFullscreen, Axiom.Collections.NamedParameterList miscParams )
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
            catch ( XFG.OutOfVideoMemoryException e )
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
            _device.RenderState.AlphaTestEnable = ( func != CompareFunction.AlwaysPass );
            _device.RenderState.AlphaFunction = XnaHelper.ConvertEnum( func );
            _device.RenderState.ReferenceAlpha = val;
        }

        public override void SetColorBufferWriteEnabled( bool red, bool green, bool blue, bool alpha )
        {
            XFG.ColorWriteChannels val = XFG.ColorWriteChannels.None;

            if ( red )
            {
                val |= XFG.ColorWriteChannels.Red;
            }

            if ( green )
            {
                val |= XFG.ColorWriteChannels.Green;
            }

            if ( blue )
            {
                val |= XFG.ColorWriteChannels.Blue;
            }

            if ( alpha )
            {
                val |= XFG.ColorWriteChannels.Alpha;
            }

            _device.RenderState.ColorWriteChannels = val;
        }

        public override void SetDepthBufferParams( bool depthTest, bool depthWrite, Axiom.Graphics.CompareFunction depthFunction )
        {
            this.DepthCheck = depthTest;
            this.DepthWrite = depthWrite;
            this.DepthFunction = depthFunction;
        }

        public override void SetFog( Axiom.Graphics.FogMode mode, Axiom.Core.ColorEx color, float density, float start, float end )
        {
            // disable fog if set to none
            if ( mode == FogMode.None )
            {
                _device.RenderState.FogTableMode = XFG.FogMode.None;
                _device.RenderState.FogEnable = false;
            }
            else
            {
                // enable fog
                XFG.FogMode xnaFogMode = XnaHelper.ConvertEnum( mode );
                _device.RenderState.FogEnable = true;
                _device.RenderState.FogVertexMode = xnaFogMode;
                _device.RenderState.FogTableMode = XFG.FogMode.None;
                _device.RenderState.FogColor = XnaHelper.ConvertColorEx( color );
                _device.RenderState.FogStart = start;
                _device.RenderState.FogEnd = end;
                _device.RenderState.FogDensity = density;
            }
        }

        public override void SetSceneBlending( Axiom.Graphics.SceneBlendFactor src, Axiom.Graphics.SceneBlendFactor dest )
        {
            // set the render states after converting the incoming values to D3D.Blend
            _device.RenderState.SourceBlend = XnaHelper.ConvertEnum( src );
            _device.RenderState.DestinationBlend = XnaHelper.ConvertEnum( dest );
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
			LogManager.Instance.Write( "XNA : XNARenderSystem.SetSurfaceParams( {0}, {1}, {2}, {3}, {4}, {5} );", ambient, diffuse, specular, emissive, shininess );
        }

        public override void SetTexture( int stage, bool enabled, string textureName )
        {
            XnaTexture texture = (XnaTexture)TextureManager.Instance.GetByName( textureName );

            if ( enabled && texture != null )
            {
                _device.Textures[stage] = texture.Texture;

                // set stage description
                texStageDesc[stage].tex = texture.Texture;
                texStageDesc[stage].texType = XnaHelper.ConvertEnum( texture.TextureType );
            }
            else
            {
                if ( texStageDesc[stage].tex != null )
                {
                    _device.Textures[stage] = null;
                    //device.TextureStates[ stage ].ColorOperation = D3D.TextureOperation.Disable;
                }

                // set stage description to defaults
                texStageDesc[stage].tex = null;
                texStageDesc[stage].autoTexCoordType = TexCoordCalcMethod.None;
                texStageDesc[stage].coordIndex = 0;
                texStageDesc[stage].texType = XnaTextureType.Normal;
            }
        }

        public override void SetTextureAddressingMode( int stage, Axiom.Graphics.TextureAddressing texAddressingMode )
        {
            XFG.TextureAddressMode xnaMode = XnaHelper.ConvertEnum( texAddressingMode );

            // set the device sampler states accordingly
            _device.SamplerStates[ stage ].AddressU = xnaMode;
            _device.SamplerStates[ stage ].AddressV = xnaMode;
            _device.SamplerStates[ stage ].AddressW = xnaMode;
        }

        public override void SetTextureBlendMode( int stage, Axiom.Graphics.LayerBlendModeEx blendMode )
        {
            // TODO: Verify byte ordering
            if ( blendMode.operation == LayerBlendOperationEx.BlendManual )
            {
                _device.RenderState.BlendFactor = XnaHelper.ConvertColorEx( new ColorEx( blendMode.blendFactor, 0, 0, 0 ) );
            }
#if FixedFunction
            if (blendMode.blendType == LayerBlendType.Color)
            {
                // Make call to set operation
                _device.TextureStates[stage].ColorOperation = d3dTexOp;
            }
            else if (blendMode.blendType == LayerBlendType.Alpha)
            {
                // Make call to set operation
                _device.TextureStates[stage].AlphaOperation = d3dTexOp;
            }
#endif
            // Now set up sources
            XFG.Color blendFactor = _device.RenderState.BlendFactor;
            Color factor = Color.FromArgb( blendFactor.A, blendFactor.R, blendFactor.G, blendFactor.B );
            ColorEx manualColor = ColorEx.FromColor( factor );

            if ( blendMode.blendType == LayerBlendType.Color )
            {
                manualColor = new ColorEx( manualColor.a, blendMode.colorArg1.r, blendMode.colorArg1.g, blendMode.colorArg1.b );
            }
            else if ( blendMode.blendType == LayerBlendType.Alpha )
            {
                manualColor = new ColorEx( blendMode.alphaArg1, manualColor.r, manualColor.g, manualColor.b );
            }

            LayerBlendSource blendSource = blendMode.source1;

            for ( int i = 0; i < 2; i++ )
            {
                //D3D.TextureArgument d3dTexArg = D3DHelper.ConvertEnum( blendSource );

                // set the texture blend factor if this is manual blending
                if ( blendSource == LayerBlendSource.Manual )
                {
                    _device.RenderState.BlendFactor = XnaHelper.ConvertColorEx( manualColor );
                }
#if FixedFunction
                // pick proper argument settings
                if (blendMode.blendType == LayerBlendType.Color)
                {
                    if (i == 0)
                    {
                        _device.TextureState[stage].ColorArgument1 = d3dTexArg;
                    }
                    else if (i == 1)
                    {
                        _device.TextureState[stage].ColorArgument2 = d3dTexArg;
                    }
                }
                else if (blendMode.blendType == LayerBlendType.Alpha)
                {
                    if (i == 0)
                    {
                        _device.TextureState[stage].AlphaArgument1 = d3dTexArg;
                    }
                    else if (i == 1)
                    {
                        _device.TextureState[stage].AlphaArgument2 = d3dTexArg;
                    }
                }
#endif
                // Source2
                blendSource = blendMode.source2;

                if ( blendMode.blendType == LayerBlendType.Color )
                {
                    manualColor = new ColorEx( manualColor.a, blendMode.colorArg2.r, blendMode.colorArg2.g, blendMode.colorArg2.b );
                }
                else if ( blendMode.blendType == LayerBlendType.Alpha )
                {
                    manualColor = new ColorEx( blendMode.alphaArg2, manualColor.r, manualColor.g, manualColor.b );
                }
            }
        }

        public override void SetTextureCoordCalculation( int stage, Axiom.Graphics.TexCoordCalcMethod method, Axiom.Core.Frustum frustum )
        {
            // save this for texture matrix calcs later
            texStageDesc[ stage ].autoTexCoordType = method;
            texStageDesc[ stage ].frustum = frustum;
        }

        public override void SetTextureCoordSet( int stage, int index )
        {
            // store
            texStageDesc[stage].coordIndex = index;

            // ToDo:
            //_device.Textures[ stage ].TextureCoordinateIndex = D3DHelper.ConvertEnum( texStageDesc[ stage ].autoTexCoordType, d3dCaps ) | index;
        }

        public override void SetTextureLayerAnisotropy( int stage, int maxAnisotropy )
        {
            if ( maxAnisotropy > _capabilities.MaxAnisotropy )
            {
                maxAnisotropy = _capabilities.MaxAnisotropy;
            }

            if ( _device.SamplerStates[ stage ].MaxAnisotropy != maxAnisotropy )
            {
                _device.SamplerStates[ stage ].MaxAnisotropy = maxAnisotropy;
            }
        }

        public override void SetTextureMatrix( int stage, Axiom.Math.Matrix4 xform )
        {
#if FixedFunction
            XNA.Matrix xnaMatrix = XNA.Matrix.Identity;
            Axiom.Math.Matrix4 newMatrix = xform;

            /* If envmap is applied, but device doesn't support spheremap,
            then we have to use texture transform to make the camera space normal
            reference the envmap properly. This isn't exactly the same as spheremap
            (it looks nasty on flat areas because the camera space normals are the same)
            but it's the best approximation we have in the absence of a proper spheremap */
            if ( texStageDesc[stage].autoTexCoordType == TexCoordCalcMethod.EnvironmentMap )
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
            if ( texStageDesc[stage].autoTexCoordType == TexCoordCalcMethod.EnvironmentMapReflection )
            {
                // get the current view matrix
                XNA.Matrix viewMatrix = _viewMatrix;

                // Get transposed 3x3, ie since Xna is transposed just copy
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
            if ( texStageDesc[stage].autoTexCoordType != TexCoordCalcMethod.None )
            {
                xnaMatrix.M13 = -xnaMatrix.M13;
                xnaMatrix.M23 = -xnaMatrix.M23;
                xnaMatrix.M33 = -xnaMatrix.M33;
                xnaMatrix.M43 = -xnaMatrix.M43;
            }

            //XFG..TransformType d3dTransType = (D3D.TransformType)( (int)( D3D.TransformType.Texture0 ) + stage );
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
#endif
        }

        public override void SetTextureUnitFiltering( int stage, Axiom.Graphics.FilterType type, Axiom.Graphics.FilterOptions filter )
        {
            XnaTextureType texType = texStageDesc[ stage ].texType;
            XFG.TextureFilter texFilter = XnaHelper.ConvertEnum( type, filter, this._capabilities, texType );

            switch ( type )
            {
                case FilterType.Min:
                    _device.SamplerStates[ stage ].MinFilter = texFilter;
                    break;

                case FilterType.Mag:
                    _device.SamplerStates[ stage ].MagFilter = texFilter;
                    break;

                case FilterType.Mip:
                    _device.SamplerStates[ stage ].MipFilter = texFilter;
                    break;
            }
        }

        public override void SetViewport( Axiom.Core.Viewport viewport )
        {
            if ( activeViewport != viewport || viewport.IsUpdated )
            {
                // store this viewport and it's target
                activeViewport = viewport;
                activeRenderTarget = viewport.Target;

                // get the back buffer surface for this viewport
                XFG.RenderTarget2D back = (XFG.RenderTarget2D)activeRenderTarget.GetCustomAttribute( "BACKBUFFER" );
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

                XFG.DepthStencilBuffer depth = (XFG.DepthStencilBuffer)activeRenderTarget.GetCustomAttribute( "DEPTHBUFFER" );

                // set the render target and depth stencil for the surfaces beloning to the viewport
                _device.DepthStencilBuffer = depth;

                // set the culling mode, to make adjustments required for viewports
                // that may need inverted vertex winding or texture flipping
                this.CullingMode = cullingMode;

                XFG.Viewport vp = new XFG.Viewport();

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
			_lightCount = (int)Utility.Min( limit, lightList.Count );
		}

        #endregion Methods

        #endregion Axiom.Graphics.RenderSystem Implementation
    }
}
