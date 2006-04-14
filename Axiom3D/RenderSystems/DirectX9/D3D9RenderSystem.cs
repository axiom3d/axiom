#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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

#region Namespace Declarations

using System;
using System.Collections;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

using Axiom;
using Axiom.Core;
using Axiom.MathLib;

using FogMode = Axiom.FogMode;
using LightType = Axiom.LightType;
using StencilOperation = Axiom.StencilOperation;
using TextureFiltering = Axiom.TextureFiltering;

using DX = Microsoft.DirectX;
using D3D = Microsoft.DirectX.Direct3D;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9
{
    /// <summary>
    /// DirectX9 Render System implementation.
    /// </summary>
    public class D3D9RenderSystem : RenderSystem
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
        ///    Reference to the Direct3D device.
        /// </summary>
        protected D3D.Device device;
        /// <summary>
        ///    Direct3D capability structure.
        /// </summary>
        protected D3D.Capabilities d3dCaps;
        /// <summary>
        ///    Signifies whether the current frame being rendered is the first.
        /// </summary>
        protected bool isFirstFrame = true;

        protected bool isFirstWindow = true;

        /// <summary>
        ///		Should we use the W buffer? (16 bit color only).
        /// </summary>
        protected bool useWBuffer;

        /// <summary>
        ///    Number of streams used last frame, used to unbind any buffers not used during the current operation.
        /// </summary>
        protected int numLastStreams;

        // stores texture stage info locally for convenience
        internal D3DTextureStageDesc[] texStageDesc = new D3DTextureStageDesc[ Config.MaxTextureLayers ];

        protected int primCount;
        protected int renderCount = 0;

        // temp fields for tracking render states
        protected bool lightingEnabled;

        const int MAX_LIGHTS = 8;
        protected Axiom.Light[] lights = new Axiom.Light[ MAX_LIGHTS ];

        protected D3DGpuProgramManager gpuProgramMgr;

        /// Saved last view matrix
        protected Matrix4 viewMatrix = Matrix4.Identity;

        /// <summary>
        ///		Temp D3D vector to avoid constant allocations.
        /// </summary>
        private Microsoft.DirectX.Vector4 tempVec = new Microsoft.DirectX.Vector4();

        public D3D9RenderSystem()
        {
            InitConfigOptions();

            // init the texture stage descriptions
            for ( int i = 0; i < Config.MaxTextureLayers; i++ )
            {
                texStageDesc[ i ].autoTexCoordType = TexCoordCalcMethod.None;
                texStageDesc[ i ].coordIndex = 0;
                texStageDesc[ i ].texType = D3DTexType.Normal;
                texStageDesc[ i ].tex = null;
            }
        }

        #region Implementation of RenderSystem

        public override ColorEx AmbientLight
        {
            get
            {
                return ColorEx.FromColor( device.RenderState.Ambient );
            }
            set
            {
                device.RenderState.Ambient = value.ToColor();
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
                if ( lightingEnabled != value )
                {
                    device.RenderState.Lighting = lightingEnabled = value;
                }

            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override bool NormalizeNormals
        {
            get
            {
                return device.RenderState.NormalizeNormals;
            }
            set
            {
                device.RenderState.NormalizeNormals = value;
            }
        }

        public override Shading ShadingMode
        {
            get
            {
                return D3DHelper.ConvertEnum( device.RenderState.ShadeMode );
            }
            set
            {
                device.RenderState.ShadeMode = D3DHelper.ConvertEnum( value );
            }
        }

        public override bool StencilCheckEnabled
        {
            get
            {
                return device.RenderState.StencilEnable;
            }
            set
            {
                device.RenderState.StencilEnable = value;
            }
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
                D3DHardwareVertexBuffer buffer =
                    (D3DHardwareVertexBuffer)entry.Value;

                short stream = (short)entry.Key;

                device.SetStreamSource( stream, buffer.D3DVertexBuffer, 0, buffer.VertexSize );

                numLastStreams++;
            }

            // Unbind any unused sources
            for ( int i = binding.BindingCount; i < numLastStreams; i++ )
            {
                device.SetStreamSource( i, null, 0, 0 );
            }

            numLastStreams = binding.BindingCount;
        }

        /// <summary>
        ///		Helper method for setting the current vertex declaration.
        /// </summary>
        protected void SetVertexDeclaration( VertexDeclaration decl )
        {
            // TODO: Check for duplicate setting and avoid setting if dupe
            D3DVertexDeclaration d3dVertDecl = (D3DVertexDeclaration)decl;

            device.VertexDeclaration = d3dVertDecl.D3DVertexDecl;
        }

        /// <summary>
        ///		
        /// </summary>
        /// <param name="buffers"></param>
        /// <param name="color"></param>
        /// <param name="depth"></param>
        /// <param name="stencil"></param>
        public override void ClearFrameBuffer( FrameBuffer buffers, ColorEx color, float depth, int stencil )
        {
            D3D.ClearFlags flags = 0;

            if ( ( buffers & FrameBuffer.Color ) > 0 )
            {
                flags |= D3D.ClearFlags.Target;
            }
            if ( ( buffers & FrameBuffer.Depth ) > 0 )
            {
                flags |= D3D.ClearFlags.ZBuffer;
            }
            // Only try to clear the stencil buffer if supported
            if ( ( buffers & FrameBuffer.Stencil ) > 0
                && caps.CheckCap( Capabilities.StencilBuffer ) )
            {

                flags |= D3D.ClearFlags.Stencil;
            }

            // clear the device using the specified params
            device.Clear( flags, color.ToARGB(), depth, stencil );
        }

        /// <summary>
        ///     Create a D3D specific render texture.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public override RenderTexture CreateRenderTexture( string name, int width, int height )
        {
            D3DRenderTexture renderTexture = new D3DRenderTexture( name, width, height );
            AttachRenderTarget( renderTexture );
            return renderTexture;
        }

        /// <summary>
        ///		Returns a Direct3D implementation of a hardware occlusion query.
        /// </summary>
        /// <returns></returns>
        public override IHardwareOcclusionQuery CreateHardwareOcclusionQuery()
        {
            return new D3DHardwareOcclusionQuery( device );
        }

        public override RenderWindow CreateRenderWindow( string name, int width, int height, int colorDepth, bool isFullscreen, int left, int top, bool depthBuffer, bool vsync, object target )
        {
            if ( device == null )
            {
                if ( isFullscreen )
                {
                    device = InitDevice( isFullscreen, depthBuffer, width, height, colorDepth, (Control)target );
                }
                else
                {
                    device = InitDevice( isFullscreen, depthBuffer, width, height, colorDepth, new Control() );
                }
            }

            RenderWindow window = new D3DWindow();

            window.Handle = target;

            try
            {
                // create the window
                window.Create( name, width, height, colorDepth, isFullscreen, left, top, depthBuffer, (Control)target, device );
            }
            catch ( D3D.OutOfVideoMemoryException e )
            {
                throw new AxiomException( "The graphics card memory has all be used up to an issue an video memory leak when restarting Axiom's DirectX rendering system. Please restart the application.", e );
            }

            // add the new render target
            AttachRenderTarget( window );

            return window;
        }

        private D3D.Device InitDevice( bool isFullscreen, bool depthBuffer, int width, int height, int colorDepth, Control target )
        {
            if ( device != null )
            {
                return device;
            }

            // we don't care about event handlers
            D3D.Device.IsUsingEventHandlers = false;

            D3D.Device newDevice;

            D3D.PresentParameters presentParams = CreatePresentationParams( isFullscreen, depthBuffer, width, height, colorDepth );

            // create the D3D Device, trying for the best vertex support first, and settling for less if necessary
            try
            {
                // hardware vertex processing
                int adapterNum = 0;
                D3D.DeviceType type = D3D.DeviceType.Hardware;
#if DEBUG
                for ( int i = 0; i < D3D.Manager.Adapters.Count; i++ )
                {
                    if ( D3D.Manager.Adapters[ i ].Information.Description == "NVIDIA NVPerfHUD" )
                    {
                        adapterNum = i;
                        type = D3D.DeviceType.Reference;
                    }
                }
#endif
				// use this with NVPerfHUD
                newDevice = new D3D.Device( adapterNum, type, target.Handle, D3D.CreateFlags.HardwareVertexProcessing, presentParams );
                // newDevice = new D3D.Device(0, D3D.DeviceType.Hardware, target.Handle, D3D.CreateFlags.HardwareVertexProcessing, presentParams);
            }
            catch ( Exception )
            {
                try
                {
                    // doh, how bout mixed vertex processing
                    newDevice = new D3D.Device( 0, D3D.DeviceType.Hardware, target.Handle, D3D.CreateFlags.MixedVertexProcessing, presentParams );
                }
                catch ( Exception )
                {
                    // what the...ok, how bout software vertex procssing.  if this fails, then I don't even know how they are seeing
                    // anything at all since they obviously don't have a video card installed
                    newDevice = new D3D.Device( 0, D3D.DeviceType.Hardware, target.Handle, D3D.CreateFlags.SoftwareVertexProcessing, presentParams );
                }
            }

            // CMH - end


            // save the device capabilites
            d3dCaps = newDevice.Capabilities;

            // by creating our texture manager, singleton TextureManager will hold our implementation
            textureMgr = new D3DTextureManager( newDevice );

            // by creating our Gpu program manager, singleton GpuProgramManager will hold our implementation
            gpuProgramMgr = new D3DGpuProgramManager( newDevice );

            // intializes the HardwareBufferManager singleton
            hardwareBufferManager = new D3DHardwareBufferManager( newDevice );

            CheckCaps( newDevice );

            return newDevice;
        }

        private static D3D.PresentParameters CreatePresentationParams( bool isFullscreen, bool depthBuffer, int width, int height, int colorDepth )
        {
            // if this is the first window, get the device and do other initialization
            // CMH - 4/24/2004 start
            /// get the Direct3D.Device params
            D3D.PresentParameters presentParams = new D3D.PresentParameters();
            presentParams.IsWindowed = !isFullscreen;
            presentParams.BackBufferCount = 0;
            presentParams.EnableAutoDepthStencil = depthBuffer;

            if ( isFullscreen )
            {
                presentParams.BackBufferWidth = width;
                presentParams.BackBufferHeight = height;
            }
            else
            { // Save us some bytes.
                presentParams.BackBufferWidth = 16;
                presentParams.BackBufferHeight = 16;
            }

            presentParams.MultiSampleType = D3D.MultiSampleType.None;
            presentParams.SwapEffect = D3D.SwapEffect.Discard;
            // TODO: Check vsync setting
            presentParams.PresentationInterval = D3D.PresentInterval.Immediate;

            // supports 16 and 32 bit color
            if ( colorDepth == 16 )
            {
                presentParams.BackBufferFormat = D3D.Format.R5G6B5;
            }
            else
            {
                presentParams.BackBufferFormat = D3D.Format.X8R8G8B8;
            }

            if ( colorDepth > 16 )
            {
                // check for 24 bit Z buffer with 8 bit stencil (optimal choice)
                if ( !D3D.Manager.CheckDeviceFormat( 0, D3D.DeviceType.Hardware, presentParams.BackBufferFormat, D3D.Usage.DepthStencil, D3D.ResourceType.Surface, D3D.DepthFormat.D24S8 ) )
                {
                    // doh, check for 32 bit Z buffer then
                    if ( !D3D.Manager.CheckDeviceFormat( 0, D3D.DeviceType.Hardware, presentParams.BackBufferFormat, D3D.Usage.DepthStencil, D3D.ResourceType.Surface, D3D.DepthFormat.D32 ) )
                    {
                        // float doh, just use 16 bit Z buffer
                        presentParams.AutoDepthStencilFormat = D3D.DepthFormat.D16;
                    }
                    else
                    {
                        // use 32 bit Z buffer
                        presentParams.AutoDepthStencilFormat = D3D.DepthFormat.D32;
                    }
                }
                else
                {
                    if ( D3D.Manager.CheckDepthStencilMatch( 0, D3D.DeviceType.Hardware, presentParams.BackBufferFormat, presentParams.BackBufferFormat, D3D.DepthFormat.D24S8 ) )
                    {
                        presentParams.AutoDepthStencilFormat = D3D.DepthFormat.D24S8;
                    }
                    else
                    {
                        presentParams.AutoDepthStencilFormat = D3D.DepthFormat.D24X8;
                    }
                }
            }
            else
            {
                // use 16 bit Z buffer if they arent using true color
                presentParams.AutoDepthStencilFormat = D3D.DepthFormat.D16;
            }
            return presentParams;
        }

        public override void Shutdown()
        {
            base.Shutdown();

            // dispose of the device
            if ( device != null )
            {
                device.Dispose();
            }

            if ( gpuProgramMgr != null )
            {
                gpuProgramMgr.Dispose();
            }
            if ( hardwareBufferManager != null )
            {
                hardwareBufferManager.Dispose();
            }
            if ( textureMgr != null )
            {
                textureMgr.Dispose();
            }
        }

        /// <summary>
        ///		Sets the rasterization mode to use during rendering.
        /// </summary>
        public override SceneDetailLevel RasterizationMode
        {
            get
            {

                switch ( device.RenderState.FillMode )
                {
                    case D3D.FillMode.Point:
                        return SceneDetailLevel.Points;
                    case D3D.FillMode.WireFrame:
                        return SceneDetailLevel.Wireframe;
                    case D3D.FillMode.Solid:
                        return SceneDetailLevel.Solid;
                    default:
                        throw new NotSupportedException();
                }
            }
            set
            {
                switch ( value )
                {
                    case SceneDetailLevel.Points:
                        device.RenderState.FillMode = D3D.FillMode.Point;
                        break;
                    case SceneDetailLevel.Wireframe:
                        device.RenderState.FillMode = D3D.FillMode.WireFrame;
                        break;
                    case SceneDetailLevel.Solid:
                        device.RenderState.FillMode = D3D.FillMode.Solid;
                        break;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stage"></param>
        /// <param name="func"></param>
        /// <param name="val"></param>
        public override void SetAlphaRejectSettings( int stage, CompareFunction func, byte val )
        {
            device.RenderState.AlphaTestEnable = ( func != CompareFunction.AlwaysPass );
            device.RenderState.AlphaFunction = D3DHelper.ConvertEnum( func );
            device.RenderState.ReferenceAlpha = val;
        }

        public override void SetColorBufferWriteEnabled( bool red, bool green, bool blue, bool alpha )
        {
            D3D.ColorWriteEnable val = 0;

            if ( red )
            {
                val |= D3D.ColorWriteEnable.Red;
            }
            if ( green )
            {
                val |= D3D.ColorWriteEnable.Green;
            }
            if ( blue )
            {
                val |= D3D.ColorWriteEnable.Blue;
            }
            if ( alpha )
            {
                val |= D3D.ColorWriteEnable.Alpha;
            }

            device.RenderState.ColorWriteEnable = val;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="color"></param>
        /// <param name="density"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        public override void SetFog( FogMode mode, ColorEx color, float density, float start, float end )
        {
            // disable fog if set to none
            if ( mode == FogMode.None )
            {
                device.RenderState.FogTableMode = Microsoft.DirectX.Direct3D.FogMode.None;
                device.RenderState.FogEnable = false;
            }
            else
            {
                // enable fog
                Microsoft.DirectX.Direct3D.FogMode d3dFogMode = D3DHelper.ConvertEnum( mode );
                device.RenderState.FogEnable = true;
                device.RenderState.FogVertexMode = d3dFogMode;
                device.RenderState.FogTableMode = Microsoft.DirectX.Direct3D.FogMode.None;
                device.RenderState.FogColor = color.ToColor();
                device.RenderState.FogStart = start;
                device.RenderState.FogEnd = end;
                device.RenderState.FogDensity = density;
            }
        }

        public override RenderWindow Initialize( bool autoCreateWindow, string windowTitle )
        {
            RenderWindow renderWindow = null;

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
                DefaultForm newWindow = CreateDefaultForm( windowTitle, 0, 0, width, height, fullScreen );

                // create the render window
                renderWindow = CreateRenderWindow( "Main Window", width, height, bpp, fullScreen, 0, 0, true, false, newWindow );

                // use W buffer when in 16 bit color mode
                useWBuffer = ( renderWindow.ColorDepth == 16 );

                newWindow.Target.Visible = false;

                newWindow.Show();

                // set the default form's renderwindow so it can access it internally
                newWindow.RenderWindow = renderWindow;
            }

            return renderWindow;
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
        private DefaultForm CreateDefaultForm( string windowTitle, int top, int left, int width, int height, bool fullScreen )
        {
            DefaultForm form = new DefaultForm();

            form.ClientSize = new Size( width, height );
            form.MaximizeBox = false;
            form.MinimizeBox = false;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.BringToFront();

            if ( fullScreen )
            {
                form.Top = 0;
                form.Left = 0;
                form.FormBorderStyle = FormBorderStyle.None;
                form.WindowState = FormWindowState.Maximized;
                form.TopMost = true;
                form.TopLevel = true;
            }
            else
            {
                form.Top = top;
                form.Left = left;
                form.FormBorderStyle = FormBorderStyle.FixedSingle;
                form.WindowState = FormWindowState.Normal;
                form.Text = windowTitle;
            }

            return form;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fov"></param>
        /// <param name="aspectRatio"></param>
        /// <param name="near"></param>
        /// <param name="far"></param>
        /// <param name="forGpuPrograms"></param>
        /// <returns></returns>
        public override Matrix4 MakeOrthoMatrix( float fov, float aspectRatio, float near, float far, bool forGpuPrograms )
        {
            float thetaY = MathUtil.DegreesToRadians( fov / 2.0f );
            float tanThetaY = MathUtil.Tan( thetaY );
            float tanThetaX = tanThetaY * aspectRatio;

            float halfW = tanThetaX * near;
            float halfH = tanThetaY * near;

            float w = 1.0f / ( halfW );
            float h = 1.0f / ( halfH );
            float q = 0;

            if ( far != 0 )
            {
                q = 1.0f / ( far - near );
            }

            Matrix4 dest = Matrix4.Zero;
            dest.m00 = w;
            dest.m11 = h;
            dest.m22 = q;
            dest.m23 = -near / ( far - near );
            dest.m33 = 1;

            if ( forGpuPrograms )
            {
                dest.m22 = -dest.m22;
            }

            return dest;
        }

        /// <summary>
        ///		
        /// </summary>
        /// <param name="fov"></param>
        /// <param name="aspectRatio"></param>
        /// <param name="near"></param>
        /// <param name="far"></param>
        /// <param name="forGpuProgram"></param>
        /// <returns></returns>
        public override Matrix4 MakeProjectionMatrix( float fov, float aspectRatio, float near, float far, bool forGpuProgram )
        {
            float theta = MathUtil.DegreesToRadians( fov * 0.5f );
            float h = 1 / MathUtil.Tan( theta );
            float w = h / aspectRatio;
            float q = 0;
            float qn = 0;

            if ( far == 0 )
            {
                q = 1 - Frustum.InfiniteFarPlaneAdjust;
                qn = near * ( Frustum.InfiniteFarPlaneAdjust - 1 );
            }
            else
            {
                q = far / ( far - near );
                qn = -q * near;
            }

            Matrix4 dest = Matrix4.Zero;

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

        public override void ApplyObliqueDepthProjection( ref Matrix4 projMatrix, Axiom.MathLib.Plane plane, bool forGpuProgram )
        {
            // Thanks to Eric Lenyel for posting this calculation at www.terathon.com

            // Calculate the clip-space corner point opposite the clipping plane
            // as (sgn(clipPlane.x), sgn(clipPlane.y), 1, 1) and
            // transform it into camera space by multiplying it
            // by the inverse of the projection matrix

            /* generalised version
			Vector4 q = matrix.inverse() * 
				Vector4(Math::Sign(plane.normal.x), Math::Sign(plane.normal.y), 1.0f, 1.0f);
			*/
            MathLib.Vector4 q = new MathLib.Vector4();
            q.x = Math.Sign( plane.Normal.x ) / projMatrix.m00;
            q.y = Math.Sign( plane.Normal.y ) / projMatrix.m11;
            q.z = 1.0f;

            // flip the next bit from Lengyel since we're right-handed
            if ( forGpuProgram )
            {
                q.w = ( 1.0f - projMatrix.m22 ) / projMatrix.m23;
            }
            else
            {
                q.w = ( 1.0f + projMatrix.m22 ) / projMatrix.m23;
            }

            // Calculate the scaled plane vector
            MathLib.Vector4 clipPlane4d =
                new MathLib.Vector4( plane.Normal.x, plane.Normal.y, plane.Normal.z, plane.D );

            MathLib.Vector4 c = clipPlane4d * ( 1.0f / ( clipPlane4d.Dot( q ) ) );

            // Replace the third row of the projection matrix
            projMatrix.m20 = c.x;
            projMatrix.m21 = c.y;

            // flip the next bit from Lengyel since we're right-handed
            if ( forGpuProgram )
            {
                projMatrix.m22 = c.z;
            }
            else
            {
                projMatrix.m22 = -c.z;
            }

            projMatrix.m23 = c.w;
        }

        /// <summary>
        /// 
        /// </summary>
        public override void BeginFrame()
        {
            Debug.Assert( activeViewport != null, "BeingFrame cannot run without an active viewport." );

            // clear the device if need be
            if ( activeViewport.ClearEveryFrame )
            {
                ClearFrameBuffer( FrameBuffer.Color | FrameBuffer.Depth, activeViewport.BackgroundColor );
            }

            // begin the D3D scene for the current viewport
            device.BeginScene();

            // set initial render states if this is the first frame. we only want to do 
            //	this once since renderstate changes are expensive
            if ( isFirstFrame )
            {
                // enable alpha blending and specular materials
                device.RenderState.AlphaBlendEnable = true;
                device.RenderState.SpecularEnable = true;
                //device.RenderState.ZBufferEnable = true;
                isFirstFrame = false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override void EndFrame()
        {
            // end the D3D scene
            device.EndScene();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="viewport"></param>
        public override void SetViewport( Axiom.Viewport viewport )
        {
            if ( activeViewport != viewport || viewport.IsUpdated )
            {
                // store this viewport and it's target
                activeViewport = viewport;
                activeRenderTarget = viewport.Target;

                // get the back buffer surface for this viewport
                D3D.Surface back = (D3D.Surface)activeRenderTarget.GetCustomAttribute( "D3DBACKBUFFER" );
                device.SetRenderTarget( 0, back );

                // we cannot dipose of the back buffer in fullscreen mode, since we have a direct reference to
                // the main back buffer.  all other surfaces are safe to dispose
                bool disposeBackBuffer = true;

                if ( activeRenderTarget is D3DWindow )
                {
                    D3DWindow window = activeRenderTarget as D3DWindow;

                    if ( window.IsFullScreen )
                    {
                        disposeBackBuffer = false;
                    }
                }

                // be sure to destroy the surface we had
                if ( disposeBackBuffer )
                {
                    back.Dispose();
                }

                D3D.Surface depth = (D3D.Surface)activeRenderTarget.GetCustomAttribute( "D3DZBUFFER" );

                // set the render target and depth stencil for the surfaces beloning to the viewport
                device.DepthStencilSurface = depth;

                // set the culling mode, to make adjustments required for viewports
                // that may need inverted vertex winding or texture flipping
                this.CullingMode = cullingMode;

                D3D.Viewport d3dvp = new D3D.Viewport();

                // set viewport dimensions
                d3dvp.X = viewport.ActualLeft;
                d3dvp.Y = viewport.ActualTop;
                d3dvp.Width = viewport.ActualWidth;
                d3dvp.Height = viewport.ActualHeight;

                // Z-values from 0.0 to 1.0 (TODO: standardize with OpenGL)
                d3dvp.MinZ = 0.0f;
                d3dvp.MaxZ = 1.0f;

                // set the current D3D viewport
                device.Viewport = d3dvp;

                // clear the updated flag
                viewport.IsUpdated = false;
            }
        }

        /// <summary>
        ///		Renders the current render operation in D3D's own special way.
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
            SetVertexDeclaration( op.vertexData.vertexDeclaration );
            SetVertexBufferBinding( op.vertexData.vertexBufferBinding );

            D3D.PrimitiveType primType = 0;

            switch ( op.operationType )
            {
                case OperationType.PointList:
                    primType = D3D.PrimitiveType.PointList;
                    primCount = op.useIndices ? op.indexData.indexCount : op.vertexData.vertexCount;
                    break;
                case OperationType.LineList:
                    primType = D3D.PrimitiveType.LineList;
                    primCount = ( op.useIndices ? op.indexData.indexCount : op.vertexData.vertexCount ) / 2;
                    break;
                case OperationType.LineStrip:
                    primType = D3D.PrimitiveType.LineStrip;
                    primCount = ( op.useIndices ? op.indexData.indexCount : op.vertexData.vertexCount ) - 1;
                    break;
                case OperationType.TriangleList:
                    primType = D3D.PrimitiveType.TriangleList;
                    primCount = ( op.useIndices ? op.indexData.indexCount : op.vertexData.vertexCount ) / 3;
                    break;
                case OperationType.TriangleStrip:
                    primType = D3D.PrimitiveType.TriangleStrip;
                    primCount = ( op.useIndices ? op.indexData.indexCount : op.vertexData.vertexCount ) - 2;
                    break;
                case OperationType.TriangleFan:
                    primType = D3D.PrimitiveType.TriangleFan;
                    primCount = ( op.useIndices ? op.indexData.indexCount : op.vertexData.vertexCount ) - 2;
                    break;
            } // switch(primType)

            // are we gonna use indices?
            if ( op.useIndices )
            {
                D3DHardwareIndexBuffer idxBuffer =
                    (D3DHardwareIndexBuffer)op.indexData.indexBuffer;

                // set the index buffer on the device
                device.Indices = idxBuffer.D3DIndexBuffer;

                // draw the indexed primitives
                device.DrawIndexedPrimitives(
                    primType, op.vertexData.vertexStart, 0, op.vertexData.vertexCount,
                    op.indexData.indexStart, primCount );
            }
            else
            {
                // draw vertices without indices
                device.DrawPrimitives( primType, op.vertexData.vertexStart, primCount );
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stage"></param>
        /// <param name="enabled"></param>
        /// <param name="textureName"></param>
        public override void SetTexture( int stage, bool enabled, string textureName )
        {
            D3DTexture texture = (D3DTexture)TextureManager.Instance.GetByName( textureName );

            if ( enabled && texture != null )
            {
                device.SetTexture( stage, texture.DXTexture );

                // set stage description
                texStageDesc[ stage ].tex = texture.DXTexture;
                texStageDesc[ stage ].texType = D3DHelper.ConvertEnum( texture.TextureType );
            }
            else
            {
                if ( texStageDesc[ stage ].tex != null )
                {
                    device.SetTexture( stage, null );
                    device.TextureState[ stage ].ColorOperation = D3D.TextureOperation.Disable;
                }

                // set stage description to defaults
                texStageDesc[ stage ].tex = null;
                texStageDesc[ stage ].autoTexCoordType = TexCoordCalcMethod.None;
                texStageDesc[ stage ].coordIndex = 0;
                texStageDesc[ stage ].texType = D3DTexType.Normal;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stage"></param>
        /// <param name="maxAnisotropy"></param>
        public override void SetTextureLayerAnisotropy( int stage, int maxAnisotropy )
        {
            if ( maxAnisotropy > d3dCaps.MaxAnisotropy )
            {
                maxAnisotropy = d3dCaps.MaxAnisotropy;
            }

            if ( device.SamplerState[ stage ].MaxAnisotropy != maxAnisotropy )
            {
                device.SamplerState[ stage ].MaxAnisotropy = maxAnisotropy;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stage"></param>
        /// <param name="method"></param>
        public override void SetTextureCoordCalculation( int stage, TexCoordCalcMethod method, Frustum frustum )
        {
            // save this for texture matrix calcs later
            texStageDesc[ stage ].autoTexCoordType = method;
            texStageDesc[ stage ].frustum = frustum;

            device.TextureState[ stage ].TextureCoordinateIndex = D3DHelper.ConvertEnum( method, d3dCaps ) | texStageDesc[ stage ].coordIndex;
        }

        public override void BindGpuProgram( GpuProgram program )
        {
            switch ( program.Type )
            {
                case GpuProgramType.Vertex:
                    device.VertexShader = ( (D3DVertexProgram)program ).VertexShader;
                    break;

                case GpuProgramType.Fragment:
                    device.PixelShader = ( (D3DFragmentProgram)program ).PixelShader;
                    break;
            }
        }

        public override void BindGpuProgramParameters( GpuProgramType type, GpuProgramParameters parms )
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
                                device.SetVertexShaderConstant( index, entry.val );
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
                                device.SetVertexShaderConstant( index, entry.val );
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
                                device.SetPixelShaderConstant( index, entry.val );
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
                                device.SetPixelShaderConstant( index, entry.val );
                            }
                        }
                    }
                    break;
            }
        }

        public override void UnbindGpuProgram( GpuProgramType type )
        {
            switch ( type )
            {
                case GpuProgramType.Vertex:
                    device.VertexShader = null;
                    break;

                case GpuProgramType.Fragment:
                    device.PixelShader = null;
                    break;
            }
        }

        #endregion

        public override Matrix4 WorldMatrix
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                device.Transform.World = MakeD3DMatrix( value );
            }
        }

        public override Matrix4 ViewMatrix
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                // flip the transform portion of the matrix for DX and its left-handed coord system
                // save latest view matrix
                viewMatrix = value;
                viewMatrix.m20 = -viewMatrix.m20;
                viewMatrix.m21 = -viewMatrix.m21;
                viewMatrix.m22 = -viewMatrix.m22;
                viewMatrix.m23 = -viewMatrix.m23;

                DX.Matrix dxView = MakeD3DMatrix( viewMatrix );
                device.Transform.View = dxView;
            }
        }

        public override Matrix4 ProjectionMatrix
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                DX.Matrix mat = MakeD3DMatrix( value );

                if ( activeRenderTarget.RequiresTextureFlipping )
                {
                    mat.M22 = -mat.M22;
                }

                device.Transform.Projection = mat;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lightList"></param>
        /// <param name="limit"></param>
        public override void UseLights( LightList lightList, int limit )
        {
            int i = 0;

            for ( ; i < limit && i < lightList.Count; i++ )
            {
                SetD3DLight( i, lightList[ i ] );
            }

            for ( ; i < numCurrentLights; i++ )
            {
                SetD3DLight( i, null );
            }

            numCurrentLights = (int)MathUtil.Min( limit, lightList.Count );
        }

        public override int ConvertColor( ColorEx color )
        {
            return color.ToARGB();
        }

        public override void SetSceneBlending( SceneBlendFactor src, SceneBlendFactor dest )
        {
            // set the render states after converting the incoming values to D3D.Blend
            device.RenderState.SourceBlend = D3DHelper.ConvertEnum( src );
            device.RenderState.DestinationBlend = D3DHelper.ConvertEnum( dest );
        }

        /// <summary>
        /// 
        /// </summary>
        public override CullingMode CullingMode
        {
            get
            {
                return cullingMode;
            }
            set
            {
                cullingMode = value;

                bool flip = activeRenderTarget.RequiresTextureFlipping ^ invertVertexWinding;

                device.RenderState.CullMode = D3DHelper.ConvertEnum( value, flip );
            }
        }


        /// <summary>
        /// 
        /// </summary>
        public override int DepthBias
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                device.RenderState.DepthBias = (float)value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override bool DepthCheck
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                if ( value )
                {
                    // use w-buffer if available
                    if ( useWBuffer && d3dCaps.RasterCaps.SupportsWBuffer )
                    {
                        device.RenderState.UseWBuffer = true;
                    }
                    else
                    {
                        device.RenderState.ZBufferEnable = true;
                    }
                }
                else
                {
                    device.RenderState.ZBufferEnable = false;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override CompareFunction DepthFunction
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                device.RenderState.ZBufferFunction = D3DHelper.ConvertEnum( value );
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override bool DepthWrite
        {
            get
            {
                return device.RenderState.ZBufferWriteEnable;
            }
            set
            {
                device.RenderState.ZBufferWriteEnable = value;
            }
        }

        /// <summary>
        ///		
        /// </summary>
        public override float HorizontalTexelOffset
        {
            get
            {
                // D3D considers the origin to be in the center of a pixel
                return -0.5f;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override float VerticalTexelOffset
        {
            get
            {
                // D3D considers the origin to be in the center of a pixel
                return -0.5f;
            }
        }

        #region Private methods

        /// <summary>
        ///		Sets up a light in D3D.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="light"></param>
        private void SetD3DLight( int index, Axiom.Light light )
        {
            if ( light == null )
            {
                device.Lights[ index ].Enabled = false;
            }
            else
            {
                switch ( light.Type )
                {
                    case LightType.Point:
                        device.Lights[ index ].LightType = Microsoft.DirectX.Direct3D.LightType.Point;
                        break;
                    case LightType.Directional:
                        device.Lights[ index ].LightType = Microsoft.DirectX.Direct3D.LightType.Directional;
                        break;
                    case LightType.Spotlight:
                        device.Lights[ index ].LightType = Microsoft.DirectX.Direct3D.LightType.Spot;
                        device.Lights[ index ].Falloff = light.SpotlightFalloff;
                        device.Lights[ index ].InnerConeAngle = MathUtil.DegreesToRadians( light.SpotlightInnerAngle );
                        device.Lights[ index ].OuterConeAngle = MathUtil.DegreesToRadians( light.SpotlightOuterAngle );
                        break;
                } // switch

                // light colors
                device.Lights[ index ].Diffuse = light.Diffuse.ToColor();
                device.Lights[ index ].Specular = light.Specular.ToColor();

                Vector3 vec;

                if ( light.Type != LightType.Directional )
                {
                    vec = light.DerivedPosition;
                    device.Lights[ index ].Position = new Microsoft.DirectX.Vector3( vec.x, vec.y, vec.z );
                }

                if ( light.Type != LightType.Point )
                {
                    vec = light.DerivedDirection;
                    device.Lights[ index ].Direction = new Microsoft.DirectX.Vector3( vec.x, vec.y, vec.z );
                }

                // atenuation settings
                device.Lights[ index ].Range = light.AttenuationRange;
                device.Lights[ index ].Attenuation0 = light.AttenuationConstant;
                device.Lights[ index ].Attenuation1 = light.AttenuationLinear;
                device.Lights[ index ].Attenuation2 = light.AttenuationQuadratic;

                device.Lights[ index ].Update();
                device.Lights[ index ].Enabled = true;
            } // if
        }

        /// <summary>
        ///		Called in constructor to init configuration.
        /// </summary>
        private void InitConfigOptions()
        {
            ConfigOption optDevice = new ConfigOption( "Rendering Device", "", false );
            ConfigOption optVideoMode = new ConfigOption( "Video Mode", "800 x 600 @ 32-bit colour", false );
            ConfigOption optFullScreen = new ConfigOption( "Full Screen", "Yes", false );
            ConfigOption optVSync = new ConfigOption( "VSync", "No", false );
            ConfigOption optAA = new ConfigOption( "Anti aliasing", "None", false );
            ConfigOption optFPUMode = new ConfigOption( "Floating-point mode", "Fastest", false );

            //driverList = this->getDirect3DDrivers();

            optDevice.PossibleValues.Clear();

            DriverCollection driverList = D3DHelper.GetDriverInfo();
            foreach ( Driver driver in driverList )
            {
                //string query = string.Format( "{0} x {1} @ {2}-bit colour", mode.Width, mode.Height, mode.ColorDepth.ToString );
                // add a new row to the display settings table
                optDevice.PossibleValues.Add( driver.Description );
                foreach ( VideoMode mode in driver.VideoModes )
                {
                    string query = string.Format( "{0} x {1} @ {2}-bit colour", mode.Width, mode.Height, mode.ColorDepth.ToString() );
                    // add a new row to the display settings table
                    optVideoMode.PossibleValues.Add( query );
                }
            }


            optFullScreen.PossibleValues.Add( "Yes" );
            optFullScreen.PossibleValues.Add( "No" );

            //for ( unsigned j = 0; j < driverList->count(); j++ )
            //{
            //    driver = driverList->item( j );
            //    optDevice.PossibleValues.push_back( driver->DriverDescription() );
            //    // Make first one default
            //    if ( j == 0 )
            //        optDevice.Value = driver->DriverDescription();
            //}

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

        private DX.Matrix MakeD3DMatrix( Matrix4 matrix )
        {
            DX.Matrix dxMat = new DX.Matrix();

            // set it to a transposed matrix since DX uses row vectors
            dxMat.M11 = matrix.m00;
            dxMat.M12 = matrix.m10;
            dxMat.M13 = matrix.m20;
            dxMat.M14 = matrix.m30;
            dxMat.M21 = matrix.m01;
            dxMat.M22 = matrix.m11;
            dxMat.M23 = matrix.m21;
            dxMat.M24 = matrix.m31;
            dxMat.M31 = matrix.m02;
            dxMat.M32 = matrix.m12;
            dxMat.M33 = matrix.m22;
            dxMat.M34 = matrix.m32;
            dxMat.M41 = matrix.m03;
            dxMat.M42 = matrix.m13;
            dxMat.M43 = matrix.m23;
            dxMat.M44 = matrix.m33;

            return dxMat;
        }

        /// <summary>
        ///		Helper method to compare 2 vertex element arrays for equality.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private bool CompareVertexDecls( D3D.VertexElement[] a, D3D.VertexElement[] b )
        {
            // if b is null, return false
            if ( b == null )
                return false;

            // compare lengths of the arrays
            if ( a.Length != b.Length )
                return false;

            // continuing on, compare each property of each element.  if any differ, return false
            for ( int i = 0; i < a.Length; i++ )
            {
                if ( a[ i ].DeclarationMethod != b[ i ].DeclarationMethod ||
                    a[ i ].Offset != b[ i ].Offset ||
                    a[ i ].Stream != b[ i ].Stream ||
                    a[ i ].DeclarationType != b[ i ].DeclarationType ||
                    a[ i ].DeclarationUsage != b[ i ].DeclarationUsage ||
                    a[ i ].UsageIndex != b[ i ].UsageIndex
                    )
                    return false;
            }

            // if we made it this far, they matched up
            return true;
        }

        #endregion

        public override void SetDepthBufferParams( bool depthTest, bool depthWrite, CompareFunction depthFunction )
        {
            this.DepthCheck = depthTest;
            this.DepthWrite = depthWrite;
            this.DepthFunction = depthFunction;
        }

        public override void SetStencilBufferParams( CompareFunction function, int refValue, int mask, StencilOperation stencilFailOp, StencilOperation depthFailOp, StencilOperation passOp, bool twoSidedOperation )
        {
            // 2 sided operation?
            if ( twoSidedOperation )
            {
                if ( !caps.CheckCap( Capabilities.TwoSidedStencil ) )
                {
                    throw new AxiomException( "2-sided stencils are not supported on this hardware!" );
                }

                device.RenderState.TwoSidedStencilMode = true;

                // use CCW version of the operations
                device.RenderState.CounterClockwiseStencilFail = D3DHelper.ConvertEnum( stencilFailOp, true );
                device.RenderState.CounterClockwiseStencilZBufferFail = D3DHelper.ConvertEnum( depthFailOp, true );
                device.RenderState.CounterClockwiseStencilPass = D3DHelper.ConvertEnum( passOp, true );
            }
            else
            {
                device.RenderState.TwoSidedStencilMode = false;
            }

            // configure standard version of the stencil operations
            device.RenderState.StencilFunction = D3DHelper.ConvertEnum( function );
            device.RenderState.ReferenceStencil = refValue;
            device.RenderState.StencilMask = mask;
            device.RenderState.StencilFail = D3DHelper.ConvertEnum( stencilFailOp );
            device.RenderState.StencilZBufferFail = D3DHelper.ConvertEnum( depthFailOp );
            device.RenderState.StencilPass = D3DHelper.ConvertEnum( passOp );
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="ambient"></param>
        /// <param name="diffuse"></param>
        /// <param name="specular"></param>
        /// <param name="emissive"></param>
        /// <param name="shininess"></param>
        public override void SetSurfaceParams( ColorEx ambient, ColorEx diffuse, ColorEx specular, ColorEx emissive, float shininess )
        {
            // TODO: Cache color values to prune unneccessary setting

            // create a new material based on the supplied params
            D3D.Material mat = new D3D.Material();
            mat.AmbientColor = DX.ColorValue.FromColor( ambient.ToColor() );
            mat.DiffuseColor = DX.ColorValue.FromColor( diffuse.ToColor() );
            mat.EmissiveColor = DX.ColorValue.FromColor( emissive.ToColor() );
            mat.SpecularColor = DX.ColorValue.FromColor( specular.ToColor() );
            mat.Power = shininess;

            // set the current material
            device.Material = mat;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stage"></param>
        /// <param name="texAddressingMode"></param>
        public override void SetTextureAddressingMode( int stage, TextureAddressing texAddressingMode )
        {
            D3D.TextureAddress d3dMode = D3DHelper.ConvertEnum( texAddressingMode );

            // set the device sampler states accordingly
            device.SamplerState[ stage ].AddressU = d3dMode;
            device.SamplerState[ stage ].AddressV = d3dMode;
            device.SamplerState[ stage ].AddressW = d3dMode;
        }

        public override void SetTextureBlendMode( int stage, LayerBlendModeEx blendMode )
        {
            D3D.TextureOperation d3dTexOp = D3DHelper.ConvertEnum( blendMode.operation );

            // TODO: Verify byte ordering
            if ( blendMode.operation == LayerBlendOperationEx.BlendManual )
            {
                device.RenderState.TextureFactor = ( new ColorEx( blendMode.blendFactor, 0, 0, 0 ) ).ToARGB();
            }

            if ( blendMode.blendType == LayerBlendType.Color )
            {
                // Make call to set operation
                device.TextureState[ stage ].ColorOperation = d3dTexOp;
            }
            else if ( blendMode.blendType == LayerBlendType.Alpha )
            {
                // Make call to set operation
                device.TextureState[ stage ].AlphaOperation = d3dTexOp;
            }

            // Now set up sources
            Color factor = Color.FromArgb( device.RenderState.TextureFactor );
            ColorEx manualD3D = ColorEx.FromColor( factor );

            if ( blendMode.blendType == LayerBlendType.Color )
            {
                manualD3D = new ColorEx( manualD3D.a, blendMode.colorArg1.r, blendMode.colorArg1.g, blendMode.colorArg1.b );
            }
            else if ( blendMode.blendType == LayerBlendType.Alpha )
            {
                manualD3D = new ColorEx( blendMode.alphaArg1, manualD3D.r, manualD3D.g, manualD3D.b );
            }

            LayerBlendSource blendSource = blendMode.source1;

            for ( int i = 0; i < 2; i++ )
            {
                D3D.TextureArgument d3dTexArg = D3DHelper.ConvertEnum( blendSource );

                // set the texture blend factor if this is manual blending
                if ( blendSource == LayerBlendSource.Manual )
                {
                    device.RenderState.TextureFactor = manualD3D.ToARGB();
                }

                // pick proper argument settings
                if ( blendMode.blendType == LayerBlendType.Color )
                {
                    if ( i == 0 )
                    {
                        device.TextureState[ stage ].ColorArgument1 = d3dTexArg;
                    }
                    else if ( i == 1 )
                    {
                        device.TextureState[ stage ].ColorArgument2 = d3dTexArg;
                    }
                }
                else if ( blendMode.blendType == LayerBlendType.Alpha )
                {
                    if ( i == 0 )
                    {
                        device.TextureState[ stage ].AlphaArgument1 = d3dTexArg;
                    }
                    else if ( i == 1 )
                    {
                        device.TextureState[ stage ].AlphaArgument2 = d3dTexArg;
                    }
                }

                // Source2
                blendSource = blendMode.source2;

                if ( blendMode.blendType == LayerBlendType.Color )
                {
                    manualD3D = new ColorEx( manualD3D.a, blendMode.colorArg2.r, blendMode.colorArg2.g, blendMode.colorArg2.b );
                }
                else if ( blendMode.blendType == LayerBlendType.Alpha )
                {
                    manualD3D = new ColorEx( blendMode.alphaArg2, manualD3D.r, manualD3D.g, manualD3D.b );
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stage"></param>
        /// <param name="index"></param>
        public override void SetTextureCoordSet( int stage, int index )
        {
            // store
            texStageDesc[ stage ].coordIndex = index;

            device.TextureState[ stage ].TextureCoordinateIndex = D3DHelper.ConvertEnum( texStageDesc[ stage ].autoTexCoordType, d3dCaps ) | index;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="type"></param>
        /// <param name="filter"></param>
        public override void SetTextureUnitFiltering( int stage, FilterType type, FilterOptions filter )
        {
            D3DTexType texType = texStageDesc[ stage ].texType;
            D3D.TextureFilter texFilter = D3DHelper.ConvertEnum( type, filter, d3dCaps, texType );

            switch ( type )
            {
                case FilterType.Min:
                    device.SamplerState[ stage ].MinFilter = texFilter;
                    break;

                case FilterType.Mag:
                    device.SamplerState[ stage ].MagFilter = texFilter;
                    break;

                case FilterType.Mip:
                    device.SamplerState[ stage ].MipFilter = texFilter;
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stage"></param>
        /// <param name="xform"></param>
        public override void SetTextureMatrix( int stage, Matrix4 xform )
        {
            DX.Matrix d3dMat = DX.Matrix.Identity;
            Matrix4 newMat = xform;

            /* If envmap is applied, but device doesn't support spheremap,
			then we have to use texture transform to make the camera space normal
			reference the envmap properly. This isn't exactly the same as spheremap
			(it looks nasty on flat areas because the camera space normals are the same)
			but it's the best approximation we have in the absence of a proper spheremap */
            if ( texStageDesc[ stage ].autoTexCoordType == TexCoordCalcMethod.EnvironmentMap )
            {
                if ( d3dCaps.VertexProcessingCaps.SupportsTextureGenerationSphereMap )
                {
                    // inverts the texture for a spheremap
                    Matrix4 matEnvMap = Matrix4.Identity;
                    matEnvMap.m11 = -1.0f;

                    // concatenate 
                    newMat = newMat * matEnvMap;
                }
                else
                {
                    /* If envmap is applied, but device doesn't support spheremap,
					then we have to use texture transform to make the camera space normal
					reference the envmap properly. This isn't exactly the same as spheremap
					(it looks nasty on flat areas because the camera space normals are the same)
					but it's the best approximation we have in the absence of a proper spheremap */

                    // concatenate with the xForm
                    newMat = newMat * Matrix4.ClipSpace2DToImageSpace;
                }
            }

            // If this is a cubic reflection, we need to modify using the view matrix
            if ( texStageDesc[ stage ].autoTexCoordType == TexCoordCalcMethod.EnvironmentMapReflection )
            {
                // get the current view matrix
                DX.Matrix viewMatrix = device.Transform.View;

                // Get transposed 3x3, ie since D3D is transposed just copy
                // We want to transpose since that will invert an orthonormal matrix ie rotation
                Matrix4 viewTransposed = Matrix4.Identity;
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
                newMat = newMat * viewTransposed;
            }

            if ( texStageDesc[ stage ].autoTexCoordType == TexCoordCalcMethod.ProjectiveTexture )
            {
                // Derive camera space to projector space transform
                // To do this, we need to undo the camera view matrix, then 
                // apply the projector view & projection matrices
                newMat = viewMatrix.Inverse() * newMat;
                newMat = texStageDesc[ stage ].frustum.ViewMatrix * newMat;
                newMat = texStageDesc[ stage ].frustum.ProjectionMatrix * newMat;

                if ( texStageDesc[ stage ].frustum.ProjectionType == Projection.Perspective )
                {
                    newMat = ProjectionClipSpace2DToImageSpacePerspective * newMat;
                }
                else
                {
                    newMat = ProjectionClipSpace2DToImageSpaceOrtho * newMat;
                }

            }

            // convert to D3D format
            d3dMat = MakeD3DMatrix( newMat );

            // need this if texture is a cube map, to invert D3D's z coord
            if ( texStageDesc[ stage ].autoTexCoordType != TexCoordCalcMethod.None )
            {
                d3dMat.M13 = -d3dMat.M13;
                d3dMat.M23 = -d3dMat.M23;
                d3dMat.M33 = -d3dMat.M33;
                d3dMat.M43 = -d3dMat.M43;
            }

            D3D.TransformType d3dTransType = (D3D.TransformType)( (int)( D3D.TransformType.Texture0 ) + stage );

            // set the matrix if it is not the identity
            if ( !D3DHelper.IsIdentity( ref d3dMat ) )
            {
                // tell D3D the dimension of tex. coord
                int texCoordDim = 0;
                if ( texStageDesc[ stage ].autoTexCoordType == TexCoordCalcMethod.ProjectiveTexture )
                {
                    texCoordDim = (int)D3D.TextureTransform.Projected | (int)D3D.TextureTransform.Count3;
                }
                else
                {
                    switch ( texStageDesc[ stage ].texType )
                    {
                        case D3DTexType.Normal:
                            texCoordDim = (int)D3D.TextureTransform.Count2;
                            break;
                        case D3DTexType.Cube:
                        case D3DTexType.Volume:
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
        }

        public override void SetScissorTest( bool enable, int left, int top, int right, int bottom )
        {
            if ( enable )
            {
                device.ScissorRectangle = new Rectangle( left, top, right - left, bottom - top );
                device.RenderState.ScissorTestEnable = true;
            }
            else
            {
                device.RenderState.ScissorTestEnable = false;
            }
        }

        /// <summary>
        ///		Helper method to go through and interrogate hardware capabilities.
        /// </summary>
        private void CheckCaps( D3D.Device device )
        {
            // get the number of possible texture units
            caps.TextureUnitCount = d3dCaps.MaxSimultaneousTextures;

            // max active lights
            caps.MaxLights = d3dCaps.MaxActiveLights;

            D3D.Surface surface = device.DepthStencilSurface;
            D3D.SurfaceDescription surfaceDesc = surface.Description;
            surface.Dispose();

            if ( surfaceDesc.Format == D3D.Format.D24S8 || surfaceDesc.Format == D3D.Format.D24X8 )
            {
                caps.SetCap( Capabilities.StencilBuffer );
                // always 8 here
                caps.StencilBufferBits = 8;
            }

            // some cards, oddly enough, do not support this
            if ( d3dCaps.DeclarationTypes.SupportsUByte4 )
            {
                caps.SetCap( Capabilities.VertexFormatUByte4 );
            }

            // Anisotropy?
            if ( d3dCaps.MaxAnisotropy > 1 )
            {
                caps.SetCap( Capabilities.AnisotropicFiltering );
            }

            // Hardware mipmapping?
            if ( d3dCaps.DriverCaps.CanAutoGenerateMipMap )
            {
                caps.SetCap( Capabilities.HardwareMipMaps );
            }

            // blending between stages is definately supported
            caps.SetCap( Capabilities.TextureBlending );
            caps.SetCap( Capabilities.MultiTexturing );

            // Dot3 bump mapping?
            if ( d3dCaps.TextureOperationCaps.SupportsDotProduct3 )
            {
                caps.SetCap( Capabilities.Dot3 );
            }

            // Cube mapping?
            if ( d3dCaps.TextureCaps.SupportsCubeMap )
            {
                caps.SetCap( Capabilities.CubeMapping );
            }

            // Texture Compression
            // We always support compression, D3DX will decompress if device does not support
            caps.SetCap( Capabilities.TextureCompression );
            caps.SetCap( Capabilities.TextureCompressionDXT );

            // D3D uses vertex buffers for everything
            caps.SetCap( Capabilities.VertexBuffer );

            // Scissor test
            if ( d3dCaps.RasterCaps.SupportsScissorTest )
            {
                caps.SetCap( Capabilities.ScissorTest );
            }

            // 2 sided stencil
            if ( d3dCaps.StencilCaps.SupportsTwoSided )
            {
                caps.SetCap( Capabilities.TwoSidedStencil );
            }

            // stencil wrap
            if ( d3dCaps.StencilCaps.SupportsIncrement && d3dCaps.StencilCaps.SupportsDecrement )
            {
                caps.SetCap( Capabilities.StencilWrap );
            }

            // Hardware Occlusion
            try
            {
                D3D.Query test = new D3D.Query( device, D3D.QueryType.Occlusion );

                // if we made it this far, it is supported
                caps.SetCap( Capabilities.HardwareOcculusion );

                test.Dispose();
            }
            catch
            {
                // eat it, this is not supported
                // TODO: Isn't there a better way to check for D3D occlusion query support?
            }

            if ( d3dCaps.MaxUserClipPlanes > 0 )
            {
                caps.SetCap( Capabilities.UserClipPlanes );
            }

            CheckVertexProgramCaps();

            CheckFragmentProgramCaps();

            // Infinite projection?
            // We have no capability for this, so we have to base this on our
            // experience and reports from users
            // Non-vertex program capable hardware does not appear to support it
            if ( caps.CheckCap( Capabilities.VertexPrograms ) )
            {
                // GeForce4 Ti (and presumably GeForce3) does not
                // render infinite projection properly, even though it does in GL
                // So exclude all cards prior to the FX range from doing infinite

                //Driver driver = D3DHelper.GetDriverInfo();

                D3D.AdapterDetails details = D3D.Manager.Adapters[ 0 ];

                //AdapterDetails details = Manager.Adapters[driver.AdapterNumber].Information;

                // not nVidia or GeForceFX and above
                if ( details.Information.VendorId != 0x10DE || details.Information.DeviceId >= 0x0301 )
                {
                    caps.SetCap( Capabilities.InfiniteFarPlane );
                }
            }

            // write hardware capabilities to registered log listeners
            caps.Log();
        }

        private void CheckFragmentProgramCaps()
        {
            int fpMajor = d3dCaps.PixelShaderVersion.Major;
            int fpMinor = d3dCaps.PixelShaderVersion.Minor;

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
                gpuProgramMgr.PushSyntaxCode( "ps_1_1" );

                if ( fpMajor > 1 || fpMinor >= 2 )
                {
                    gpuProgramMgr.PushSyntaxCode( "ps_1_2" );
                }
                if ( fpMajor > 1 || fpMinor >= 3 )
                {
                    gpuProgramMgr.PushSyntaxCode( "ps_1_3" );
                }
                if ( fpMajor > 1 || fpMinor >= 4 )
                {
                    gpuProgramMgr.PushSyntaxCode( "ps_1_4" );
                }
            }

            if ( fpMajor >= 2 )
            {
                gpuProgramMgr.PushSyntaxCode( "ps_2_0" );

                if ( fpMinor > 0 )
                {
                    gpuProgramMgr.PushSyntaxCode( "ps_2_x" );
                }
            }

            if ( fpMajor >= 3 )
            {
                gpuProgramMgr.PushSyntaxCode( "ps_3_0" );

                if ( fpMinor > 0 )
                {
                    gpuProgramMgr.PushSyntaxCode( "ps_3_x" );
                }
            }
        }

        private void CheckVertexProgramCaps()
        {
            int vpMajor = d3dCaps.VertexShaderVersion.Major;
            int vpMinor = d3dCaps.VertexShaderVersion.Minor;

            // check vertex program caps
            switch ( vpMajor )
            {
                case 1:
                    caps.MaxVertexProgramVersion = "vs_1_1";
                    // 4d float vectors
                    caps.VertexProgramConstantFloatCount = d3dCaps.MaxVertexShaderConst;
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
                    caps.VertexProgramConstantFloatCount = d3dCaps.MaxVertexShaderConst;

                    break;
                case 3:
                    caps.MaxVertexProgramVersion = "vs_3_0";

                    // 16 ints
                    caps.VertexProgramConstantIntCount = 16 * 4;
                    // 4d float vectors
                    caps.VertexProgramConstantFloatCount = d3dCaps.MaxVertexShaderConst;

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
                gpuProgramMgr.PushSyntaxCode( "vs_1_1" );
            }
            if ( vpMajor >= 2 )
            {
                if ( vpMajor > 2 || vpMinor > 0 )
                {
                    gpuProgramMgr.PushSyntaxCode( "vs_2_x" );
                }
                gpuProgramMgr.PushSyntaxCode( "vs_2_0" );
            }
            if ( vpMajor >= 3 )
            {
                gpuProgramMgr.PushSyntaxCode( "vs_3_0" );
            }
        }

        /// <summary>
        ///		Helper method that converts a DX Matrix to our Matrix4.
        /// </summary>
        /// <param name="d3dMat"></param>
        /// <returns></returns>
        private Matrix4 ConvertD3DMatrix( ref DX.Matrix d3dMat )
        {
            Matrix4 mat = Matrix4.Zero;

            mat.m00 = d3dMat.M11;
            mat.m10 = d3dMat.M12;
            mat.m20 = d3dMat.M13;
            mat.m30 = d3dMat.M14;

            mat.m01 = d3dMat.M21;
            mat.m11 = d3dMat.M22;
            mat.m21 = d3dMat.M23;
            mat.m31 = d3dMat.M24;

            mat.m02 = d3dMat.M31;
            mat.m12 = d3dMat.M32;
            mat.m22 = d3dMat.M33;
            mat.m32 = d3dMat.M34;

            mat.m03 = d3dMat.M41;
            mat.m13 = d3dMat.M42;
            mat.m23 = d3dMat.M43;
            mat.m33 = d3dMat.M44;

            return mat;
        }
    }

    /// <summary>
    ///		Structure holding texture unit settings for every stage
    /// </summary>
    internal struct D3DTextureStageDesc
    {
        /// the type of the texture
        public D3DTexType texType;
        /// wich texCoordIndex to use
        public int coordIndex;
        /// type of auto tex. calc. used
        public TexCoordCalcMethod autoTexCoordType;
        /// Frustum, used if the above is projection
        public Frustum frustum;
        /// texture 
        public D3D.BaseTexture tex;
    }

    /// <summary>
    ///		D3D texture types
    /// </summary>
    public enum D3DTexType
    {
        Normal,
        Cube,
        Volume,
        None
    }
}
