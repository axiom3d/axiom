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

using System;
using System.Collections;
using System.Diagnostics;
using System.Windows.Forms;
using Axiom.Collections;
using Axiom.Configuration;
using Axiom.Core;
using Axiom.MathLib;
using Axiom.Graphics;
using Axiom.Utility;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using DX = Microsoft.DirectX;
using D3D = Microsoft.DirectX.Direct3D;
using FogMode = Axiom.Graphics.FogMode;
using LightType = Axiom.Graphics.LightType;
using StencilOperation = Axiom.Graphics.StencilOperation;
using TextureFiltering = Axiom.Graphics.TextureFiltering;

namespace Axiom.RenderSystems.DirectX9 {
    /// <summary>
    /// DirectX9 Render System implementation.
    /// </summary>
    public class D3D9RenderSystem : RenderSystem, IPlugin {
        /// <summary>
        ///    Reference to the Direct3D device.
        /// </summary>
        protected D3D.Device device;
        /// <summary>
        ///    Direct3D capability structure.
        /// </summary>
        protected D3D.Caps d3dCaps;
        /// <summary>
        ///    Signifies whether the current frame being rendered is the first.
        /// </summary>
        protected bool isFirstFrame = true;

        protected bool isFirstWindow = true;
        /// <summary>
        ///    Number of streams used last frame, used to unbind any buffers not used during the current operation.
        /// </summary>
        protected int numLastStreams;

        // stores texture stage info locally for convenience
        // TODO: finish using this in all appropriate methods
        internal D3DTextureStageDesc[] texStageDesc = new D3DTextureStageDesc[Config.MaxTextureLayers];

        protected int primCount;
        protected int renderCount = 0;

        // temp fields for tracking render states
        protected bool lightingEnabled;

        protected D3DGpuProgramManager gpuProgramMgr;

        public D3D9RenderSystem() {
            InitConfigOptions();

            // init the texture stage descriptions
            for(int i = 0; i < Config.MaxTextureLayers; i++) {
                texStageDesc[i].autoTexCoordType = TexCoordCalcMethod.None;
                texStageDesc[i].coordIndex = 0;
                texStageDesc[i].texType = D3DTexType.Normal;
                texStageDesc[i].tex = null;
            }
        }

        #region Implementation of RenderSystem

        public override ColorEx AmbientLight {
            set {
                device.RenderState.Ambient = value.ToColor();
            }
        }
	
        public override bool LightingEnabled {
            set {
                if(lightingEnabled == value) {
                    //return;
                }

                device.RenderState.Lighting = lightingEnabled = value;
            }
        }
	
        /// <summary>
        /// 
        /// </summary>
        public override bool NormalizeNormals {
            set {
                device.RenderState.NormalizeNormals = value;
            }
        }

        public override Shading ShadingMode {
            set {
                device.RenderState.ShadeMode = D3DHelper.ConvertEnum(value);
            }
        }
	
        public override StencilOperation StencilBufferDepthFailOperation {
            set {
                device.RenderState.StencilZBufferFail = D3DHelper.ConvertEnum(value);
            }
        }
	
        public override StencilOperation StencilBufferFailOperation {
            set {
                device.RenderState.StencilFail = D3DHelper.ConvertEnum(value);
            }
        }
	
        public override CompareFunction StencilBufferFunction {
            set {
                device.RenderState.StencilFunction = D3DHelper.ConvertEnum(value);
            }
        }
	
        public override int StencilBufferMask {
            set {
                device.RenderState.StencilMask = value;
            }
        }
	
        public override Axiom.Graphics.StencilOperation StencilBufferPassOperation {
            set {
                device.RenderState.StencilPass = D3DHelper.ConvertEnum(value);
            }
        }
	
        public override int StencilBufferReferenceValue {
            set {
                device.RenderState.StencilWriteMask = value;
            }
        }
	
        public override bool StencilCheckEnabled {
            set {
                device.RenderState.StencilEnable = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// DOC
        protected void SetVertexBufferBinding(VertexBufferBinding binding) {
            // TODO: Optimize to remove foreach
            foreach(DictionaryEntry bufferBinding in binding.Bindings) {
                D3DHardwareVertexBuffer buffer = 
                    (D3DHardwareVertexBuffer)bufferBinding.Value;

                ushort stream = (ushort)bufferBinding.Key;

                device.SetStreamSource(stream, buffer.D3DVertexBuffer, 0, buffer.VertexSize);

                numLastStreams++;
            }

            // Unbind any unused sources
            for(int i = binding.Bindings.Count; i < numLastStreams; i++) {
                device.SetStreamSource(i, null, 0, 0);
            }
            
            numLastStreams = binding.Bindings.Count;
        }

        /// <summary>
        /// 
        /// </summary>
        /// DOC
        protected void SetVertexDeclaration(Axiom.Graphics.VertexDeclaration decl) {
            // TODO: Check for duplicate setting and avoid setting if dupe
            D3DVertexDeclaration d3dVertDecl = (D3DVertexDeclaration)decl;

            device.VertexDeclaration = d3dVertDecl.D3DVertexDecl;
        }
	
        /// <summary>
        ///     Create a D3D specific render texture.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public override RenderTexture CreateRenderTexture(string name, int width, int height) {
            D3DRenderTexture renderTexture = new D3DRenderTexture(name, width, height);
            AttachRenderTarget(renderTexture);
            return renderTexture;
        }

        public override RenderWindow CreateRenderWindow(string name, int width, int height, int colorDepth, bool isFullscreen, int left, int top, bool depthBuffer, object target) {
            RenderWindow window = new D3DWindow();

			window.Handle = target;

            // create the window
            window.Create(name, width, height, colorDepth, isFullscreen, left, top, depthBuffer, (Control)target); 

            // add the new render target
            AttachRenderTarget(window);

            // if this is the first window, get the device and do other initialization
            if(isFirstWindow) {
                device = (D3D.Device)window.GetCustomAttribute("D3DDEVICE");

                // save the device capabilites
                d3dCaps = device.DeviceCaps;

                // by creating our texture manager, singleton TextureManager will hold our implementation
                textureMgr = new D3DTextureManager(device);

                // by creating our Gpu program manager, singleton GpuProgramManager will hold our implementation
                gpuProgramMgr = new D3DGpuProgramManager(device);

                // intializes the HardwareBufferManager singleton
                hardwareBufferManager = new D3DHardwareBufferManager(device);

                CheckCaps();

                // initialize the mesh manager here, since it relies on the render system already establishing a
                // HardwareBufferManager
                MeshManager.Init();

                isFirstWindow = false;
            }

            return window;
        }

        public override void Shutdown() {
            base.Shutdown();

            // TODO: Re eval shutdown

            // TODO: Disposing of the device hung the app, read into this
            //if(device != null)
            //device.Dispose();
        }
		

        /// <summary>
        ///		Sets the rasterization mode to use during rendering.
        /// </summary>
        protected override SceneDetailLevel RasterizationMode {
            set {
                switch(value) {
                    case SceneDetailLevel.Points:
                        device.RenderState.FillMode = FillMode.Point;
                        break;
                    case SceneDetailLevel.Wireframe:
                        device.RenderState.FillMode = FillMode.WireFrame;
                        break;
                    case SceneDetailLevel.Solid:
                        device.RenderState.FillMode = FillMode.Solid;
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
        protected override void SetAlphaRejectSettings(int stage, CompareFunction func, byte val) {
            device.RenderState.AlphaTestEnable = (func != CompareFunction.AlwaysPass);
            device.RenderState.AlphaFunction = D3DHelper.ConvertEnum(func);
            device.RenderState.ReferenceAlpha = val;
        }

        protected override void SetColorBufferWriteEnabled(bool red, bool green, bool blue, bool alpha) {
            D3D.ColorWriteEnable val = 0;

            if(red) {
                val |= ColorWriteEnable.Red;
            }
            if(green) {
                val |= ColorWriteEnable.Green;
            }
            if(blue) {
                val |= ColorWriteEnable.Blue;
            }
            if(alpha) {
                val |= ColorWriteEnable.Alpha;
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
        protected override void SetFog(Axiom.Graphics.FogMode mode, ColorEx color, float density, float start, float end) {
            // disable fog if set to none
            if(mode == FogMode.None) {
                device.RenderState.FogTableMode = D3D.FogMode.None;
                device.RenderState.FogEnable = false;
            }
            else {
                // enable fog
                D3D.FogMode d3dFogMode = D3DHelper.ConvertEnum(mode);
                device.RenderState.FogEnable = true;
                device.RenderState.FogVertexMode = d3dFogMode; 
                device.RenderState.FogTableMode = D3D.FogMode.None;
                device.RenderState.FogColor = color.ToColor();
                device.RenderState.FogStart = start;
                device.RenderState.FogEnd = end;
                device.RenderState.FogDensity = density;
            }
        }

        public override RenderWindow Initialize(bool autoCreateWindow) {
            RenderWindow renderWindow = null;

            if(autoCreateWindow) {
            	System.Data.DataRow[] modes = engineConfig.DisplayMode.Select("Selected = true");
            	
            	if(modes == null || modes.Length == 0) {
        	    throw new Exception("No video mode is selected");
            	}

                EngineConfig.DisplayModeRow mode = (EngineConfig.DisplayModeRow)modes[0];

                // create a default form window
                DefaultForm newWindow = RenderWindow.CreateDefaultForm(0, 0, mode.Width, mode.Height, mode.FullScreen);

                // create the render window
                renderWindow = this.CreateRenderWindow("Main Window", mode.Width, mode.Height, mode.Bpp, mode.FullScreen, 0, 0, true, newWindow);
				
                newWindow.Target.Visible = false;

                newWindow.Show();
				
                // set the default form's renderwindow so it can access it internally
                newWindow.RenderWindow = renderWindow;

            }

            return renderWindow;
        }

        public override Axiom.MathLib.Matrix4 MakeProjectionMatrix(float fov, float aspectRatio, float near, float far, bool forGpuProgram) {

            Matrix d3dMatrix;

            if(forGpuProgram) {
                d3dMatrix = Matrix.PerspectiveFovRH(MathUtil.DegreesToRadians(fov), aspectRatio, near, far);
            }
            else {
                d3dMatrix = Matrix.PerspectiveFovLH(MathUtil.DegreesToRadians(fov), aspectRatio, near, far);
            }

            return ConvertD3DMatrix(ref d3dMatrix);

//            Matrix4 matrix = Matrix4.Zero;
//
//            float theta = MathUtil.DegreesToRadians(fov * 0.5f);
//            float h = 1 / MathUtil.Tan(theta);
//            float w = h / aspectRatio;
//            float q = far / (far - near);
//
//            matrix[0,0] = w;
//            matrix[1,1] = h;
//            matrix[2,2] = q;
//            matrix[2,3] = -q * near;
//            matrix[3,2] = 1.0f;
//
//            return matrix;
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void BeginFrame() {
            Debug.Assert(activeViewport != null, "BeingFrame cannot run without an active viewport.");

            if(activeViewport.ClearEveryFrame) {
                // clear the device
                device.Clear(ClearFlags.ZBuffer | ClearFlags.Target, activeViewport.BackgroundColor.ToColor(), 1.0f, 0);
            }

            // begin the D3D scene for the current viewport
            device.BeginScene();

            // set initial render states if this is the first frame. we only want to do 
            //	this once since renderstate changes are expensive
            if(isFirstFrame) {
                // enable alpha blending and specular materials
                device.RenderState.AlphaBlendEnable = true;
                device.RenderState.SpecularEnable = true;
                device.RenderState.ZBufferEnable = true;

                isFirstFrame = false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void EndFrame() {
            // end the D3D scene
            device.EndScene();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="viewport"></param>
        protected override void SetViewport(Axiom.Core.Viewport viewport) {
            if(activeViewport != viewport || viewport.IsUpdated) {
                // store this viewport and it's target
                activeViewport = viewport;
                activeRenderTarget = viewport.Target;

                // get the back buffer surface for this viewport
                D3D.Surface back = 
                    (D3D.Surface)activeRenderTarget.GetCustomAttribute("D3DBACKBUFFER");

                D3D.Surface depth =
                    (D3D.Surface)activeRenderTarget.GetCustomAttribute("D3DZBUFFER");

                // set the render target and depth stencil for the surfaces beloning to the viewport
                device.SetRenderTarget(0, back);
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
        public override void Render(RenderOperation op) {

            // don't even bother if there are no vertices to render, causes problems on some cards (FireGL 8800)
            if(op.vertexData.vertexCount == 0) {
                return;
            }

            // class base implementation first
            base.Render(op);

            // set the vertex declaration and buffer binding
            SetVertexDeclaration(op.vertexData.vertexDeclaration);
            SetVertexBufferBinding(op.vertexData.vertexBufferBinding);

            PrimitiveType primType = 0;

            switch(op.operationType) {
                case RenderMode.PointList:
                    primType = PrimitiveType.PointList;
                    primCount = op.useIndices ? op.indexData.indexCount : op.vertexData.vertexCount;
                    break;
                case RenderMode.LineList:
                    primType = PrimitiveType.LineList;
                    primCount = (op.useIndices ? op.indexData.indexCount : op.vertexData.vertexCount) / 2;
                    break;
                case RenderMode.LineStrip:
                    primType = PrimitiveType.LineStrip;
                    primCount = (op.useIndices ? op.indexData.indexCount : op.vertexData.vertexCount) - 1;
                    break;
                case RenderMode.TriangleList:
                    primType = PrimitiveType.TriangleList;
                    primCount = (op.useIndices ? op.indexData.indexCount : op.vertexData.vertexCount) / 3;
                    break;
                case RenderMode.TriangleStrip:
                    primType = PrimitiveType.TriangleStrip;
                    primCount = (op.useIndices ? op.indexData.indexCount : op.vertexData.vertexCount) - 2;
                    break;
                case RenderMode.TriangleFan:
                    primType = PrimitiveType.TriangleFan;
                    primCount = (op.useIndices ? op.indexData.indexCount : op.vertexData.vertexCount) - 2;
                    break;
            } // switch(primType)

            // are we gonna use indices?
            if(op.useIndices) {
                D3DHardwareIndexBuffer idxBuffer = 
                    (D3DHardwareIndexBuffer)op.indexData.indexBuffer;

                // set the index buffer on the device
                device.Indices = idxBuffer.D3DIndexBuffer;

                // draw the indexed primitives
                device.DrawIndexedPrimitives(
                    primType, op.vertexData.vertexStart, 0, op.vertexData.vertexCount, 
                    op.indexData.indexStart, primCount);
            }
            else {
                // draw vertices without indices
                device.DrawPrimitives(primType, op.vertexData.vertexStart, primCount);
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stage"></param>
        /// <param name="enabled"></param>
        /// <param name="textureName"></param>
        protected override void SetTexture(int stage, bool enabled, string textureName) {
            D3DTexture texture = (D3DTexture)TextureManager.Instance.GetByName(textureName);

            if(enabled && texture != null)
                device.SetTexture(stage, texture.DXTexture);
            else {
                device.SetTexture(stage, null);
                device.TextureState[stage].ColorOperation = D3D.TextureOperation.Disable;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stage"></param>
        /// <param name="maxAnisotropy"></param>
        protected override void SetTextureLayerAnisotropy(int stage, int maxAnisotropy) {
            if(maxAnisotropy > d3dCaps.MaxAnisotropy) {
                maxAnisotropy = d3dCaps.MaxAnisotropy;
            }

            if(device.SamplerState[stage].MaxAnisotropy != maxAnisotropy) {
                device.SamplerState[stage].MaxAnisotropy = maxAnisotropy;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stage"></param>
        /// <param name="method"></param>
        protected override void SetTextureCoordCalculation(int stage, TexCoordCalcMethod method) {
            // save this for texture matrix calcs later
            texStageDesc[stage].autoTexCoordType = method;

            // set auto texcoord gen mode if present
            // if not present we've already set it through SetTextureCoordSet
            if(method != TexCoordCalcMethod.None)
                device.TextureState[stage].TextureCoordinateIndex = D3DHelper.ConvertEnum(method, d3dCaps);
        }

        public override void BindGpuProgram(GpuProgram program) {
            switch(program.Type) {
                case GpuProgramType.Vertex:
                    device.VertexShader = ((D3DVertexProgram)program).VertexShader;
                    break;

                case GpuProgramType.Fragment:
                    device.PixelShader = ((D3DFragmentProgram)program).PixelShader;
                    break;
            }            
        }

        public override void BindGpuProgramParameters(GpuProgramType type, GpuProgramParameters parms) {

            switch(type) {
                case GpuProgramType.Vertex:
                    if(parms.HasIntConstants) { 
                        device.SetVertexShaderConstant(0, parms.IntConstants);
                    }
                    if(parms.HasFloatConstants) {
                        for(int i = 0; i < parms.FloatConstantCount; i++) {
                            int index = parms.GetFloatConstantIndex(i);
                            Axiom.MathLib.Vector4 vec4 = parms.GetFloatConstant(i);

                            device.SetVertexShaderConstant(index, new Microsoft.DirectX.Vector4(vec4.x, vec4.y, vec4.z, vec4.w));
                        }
                    }

                    break;

                case GpuProgramType.Fragment:
                    if(parms.HasIntConstants) { 
                        device.SetPixelShaderConstant(0, parms.IntConstants);
                    }
                    if(parms.HasFloatConstants) {
                        for(int i = 0; i < parms.FloatConstantCount; i++) {
                            int index = parms.GetFloatConstantIndex(i);
                            Axiom.MathLib.Vector4 vec4 = parms.GetFloatConstant(i);

                            device.SetPixelShaderConstant(index, new Microsoft.DirectX.Vector4(vec4.x, vec4.y, vec4.z, vec4.w));
                        }
                    }
                    break;
            }          
        }

        public override void UnbindGpuProgram(GpuProgramType type) {
            switch(type) {
                case GpuProgramType.Vertex:
                    device.VertexShader = null;
                    break;

                case GpuProgramType.Fragment:
                    device.PixelShader = null;
                    break;
            }
        }

        #endregion

        #region Implementation of IPlugin

        public void Start() {
            // add an instance of this plugin to the list of available RenderSystems
            Engine.Instance.RenderSystems.Add("Direct3D9", this);
        }
        public void Stop() {
            // dispose of the D3D device
            // TODO: Find out why this hangs
            //device.Dispose();
        }
        #endregion

        protected override Axiom.MathLib.Matrix4 WorldMatrix {
            set {
                device.Transform.World = MakeD3DMatrix(value);
            }
        }

        protected override Axiom.MathLib.Matrix4 ViewMatrix {
            set {
                // flip the transform portion of the matrix for DX and its left-handed coord system
                DX.Matrix dxView = MakeD3DMatrix(value);
                dxView.M13 = -dxView.M13;
                dxView.M23 = -dxView.M23;
                dxView.M33 = -dxView.M33;
                dxView.M43 = -dxView.M43;

                device.Transform.View = dxView;
            }
        }

        protected override Axiom.MathLib.Matrix4 ProjectionMatrix {
            set {
                Matrix mat = MakeD3DMatrix(value);

                if(activeRenderTarget.RequiresTextureFlipping) {
                    mat.M22 = - mat.M22;
                }

                device.Transform.Projection = mat;
            }
        }
	
        /// <summary>
        /// 
        /// </summary>
        /// <param name="lightList"></param>
        /// <param name="limit"></param>
        protected override void UseLights(LightList lightList, int limit) {
            int i = 0;

            for( ; i < limit && i < lightList.Count; i++) {
                SetD3DLight(i, lightList[i]);
            }

            for( ; i < numCurrentLights; i++) {
                SetD3DLight(i, null);
            }

            numCurrentLights = (int)MathUtil.Min(limit, lightList.Count);
        }

        public override int ConvertColor(ColorEx color) {
            return color.ToARGB();
        }

        protected override void SetSceneBlending(SceneBlendFactor src, SceneBlendFactor dest) {
            // set the render states after converting the incoming values to D3D.Blend
            device.RenderState.SourceBlend = D3DHelper.ConvertEnum(src);
            device.RenderState.DestinationBlend = D3DHelper.ConvertEnum(dest);
        }

        /// <summary>
        /// 
        /// </summary>
        // TODO: Modify for texture flipping and culling
        public override CullingMode CullingMode {
            get {
                return cullingMode;
            }
            set {
                cullingMode = value;

                bool flip = activeRenderTarget.RequiresTextureFlipping ^ invertVertexWinding;

                device.RenderState.CullMode = D3DHelper.ConvertEnum(value, flip);
            }
        }


        /// <summary>
        /// 
        /// </summary>
        protected override int DepthBias {
            set {
                device.RenderState.DepthBias = (float)value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected override bool DepthCheck {
            set {
                if(value) {
                    // use w-buffer if available
                    if(d3dCaps.RasterCaps.SupportsWBuffer) {
                        device.RenderState.UseWBuffer = true;
                    }
                    else {
                        device.RenderState.ZBufferEnable = true;
                    }
                }
                else {
                    device.RenderState.ZBufferEnable = false;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected override CompareFunction DepthFunction {
            set {
                device.RenderState.ZBufferFunction = D3DHelper.ConvertEnum(value);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected override bool DepthWrite {
            set {
                device.RenderState.ZBufferWriteEnable = value;
            }
        }

        #region Private methods

        /// <summary>
        ///		Sets up a light in D3D.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="light"></param>
        private void SetD3DLight(int index, Axiom.Core.Light light) {
            if(light == null) {
                device.Lights[index].Enabled = false;
            }
            else {
                switch(light.Type) {
                    case LightType.Point:
                        device.Lights[index].Type = D3D.LightType.Point;
                        break;
                    case LightType.Directional:
                        device.Lights[index].Type = D3D.LightType.Directional;
                        break;
                    case LightType.Spotlight:
                        device.Lights[index].Type = D3D.LightType.Spot;
                        device.Lights[index].Falloff = light.SpotlightFalloff;
                        device.Lights[index].InnerConeAngle = MathUtil.DegreesToRadians(light.SpotlightInnerAngle);
                        device.Lights[index].OuterConeAngle = MathUtil.DegreesToRadians(light.SpotlightOuterAngle);
                        break;
                } // switch

                // light colors
                device.Lights[index].Diffuse = light.Diffuse.ToColor();
                device.Lights[index].Specular = light.Specular.ToColor();

                Axiom.MathLib.Vector3 vec;
				
                if(light.Type != LightType.Directional) {
                    vec = light.DerivedPosition;
                    device.Lights[index].Position = new DX.Vector3(vec.x, vec.y, vec.z);
                }

                if(light.Type != LightType.Point) {
                    vec = light.DerivedDirection;
                    device.Lights[index].Direction = new DX.Vector3(vec.x, vec.y, vec.z);
                }

                // atenuation settings
                device.Lights[index].Range = light.AttenuationRange;
                device.Lights[index].Attenuation0 = light.AttenuationConstant;
                device.Lights[index].Attenuation1 = light.AttenuationLinear;
                device.Lights[index].Attenuation2 = light.AttenuationQuadratic;

                device.Lights[index].Commit();
                device.Lights[index].Enabled = true;
            } // if
        }

        /// <summary>
        ///		Called in constructor to init configuration.
        /// </summary>
        private void InitConfigOptions() {
            Driver driver = D3DHelper.GetDriverInfo();
			
            foreach(VideoMode mode in driver.VideoModes) {
                // add a new row to the display settings table
                engineConfig.DisplayMode.AddDisplayModeRow(mode.Width, mode.Height, mode.ColorDepth, false, false);
            }
        }

        private DX.Matrix MakeD3DMatrix(Axiom.MathLib.Matrix4 matrix) {
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
        private bool CompareVertexDecls(D3D.VertexElement[] a, D3D.VertexElement[] b) {
            // if b is null, return false
            if(b == null)
                return false;

            // compare lengths of the arrays
            if(a.Length != b.Length)
                return false;

            // continuing on, compare each property of each element.  if any differ, return false
            for(int i = 0; i < a.Length; i++) {
                if( a[i].DeclarationMethod != b[i].DeclarationMethod ||
                    a[i].Offset != b[i].Offset ||
                    a[i].Stream != b[i].Stream ||
                    a[i].DeclarationType != b[i].DeclarationType ||
                    a[i].DeclarationUsage != b[i].DeclarationUsage ||
                    a[i].UsageIndex != b[i].UsageIndex
                    )
                    return false;
            }

            // if we made it this far, they matched up
            return true;
        }

        #endregion
	
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ambient"></param>
        /// <param name="diffuse"></param>
        /// <param name="specular"></param>
        /// <param name="emissive"></param>
        /// <param name="shininess"></param>
        protected override void SetSurfaceParams(ColorEx ambient, ColorEx diffuse, ColorEx specular, ColorEx emissive, float shininess) {
            // create a new material based on the supplied params
            D3D.Material mat = new D3D.Material();
            mat.Ambient = ambient.ToColor();
            mat.Diffuse = diffuse.ToColor();
            mat.Specular = specular.ToColor();
            mat.SpecularSharpness = shininess;

            // set the current material
            device.Material = mat;
        }
	
        /// <summary>
        /// 
        /// </summary>
        /// <param name="stage"></param>
        /// <param name="texAddressingMode"></param>
        protected override void SetTextureAddressingMode(int stage, TextureAddressing texAddressingMode) {
            D3D.TextureAddress d3dMode = 0;

            // convert from ours to D3D
            switch(texAddressingMode) {
                case TextureAddressing.Wrap:
                    d3dMode = D3D.TextureAddress.Wrap;
                    break;

                case TextureAddressing.Mirror:
                    d3dMode = D3D.TextureAddress.Mirror;
                    break;

                case TextureAddressing.Clamp:
                    d3dMode = D3D.TextureAddress.Clamp;
                    break;
            } // end switch

            // set the device sampler states accordingly
            device.SamplerState[stage].AddressU = d3dMode;
            device.SamplerState[stage].AddressV = d3dMode;
        }
	
        public override void SetTextureBlendMode(int stage, LayerBlendModeEx blendMode) {
            D3D.TextureOperation d3dTexOp = D3DHelper.ConvertEnum(blendMode.operation);

            // TODO: Verify byte ordering
            if(blendMode.operation == LayerBlendOperationEx.BlendManual)
                device.RenderState.TextureFactor = (new ColorEx(blendMode.blendFactor, 0, 0, 0)).ToARGB();

            if( blendMode.blendType == LayerBlendType.Color ) {
                // Make call to set operation
                device.TextureState[stage].ColorOperation = d3dTexOp;
            }
            else if( blendMode.blendType == LayerBlendType.Alpha ) {
                // Make call to set operation
                device.TextureState[stage].AlphaOperation = d3dTexOp;
            }

            // Now set up sources
            ColorEx manualD3D = null;

            if( blendMode.blendType == LayerBlendType.Color ) {
                manualD3D = new ColorEx(1.0f, blendMode.colorArg1.r, blendMode.colorArg1.g, blendMode.colorArg1.b);
            }
            else if( blendMode.blendType == LayerBlendType.Alpha ) {
                manualD3D = new ColorEx(blendMode.alphaArg1, 0, 0, 0);
            }

            LayerBlendSource blendSource = blendMode.source1;

            for( int i=0; i < 2; i++ ) {
                D3D.TextureArgument d3dTexArg = D3DHelper.ConvertEnum(blendSource);

                // set the texture blend factor if this is manual blending
                if(blendSource == LayerBlendSource.Manual)
                    device.RenderState.TextureFactor = manualD3D.ToARGB();

                // pick proper argument settings
                if( blendMode.blendType == LayerBlendType.Color ) {
                    if(i == 0)
                        device.TextureState[stage].ColorArgument1 = d3dTexArg;
                    else if (i ==1)
                        device.TextureState[stage].ColorArgument2 = d3dTexArg;
                }
                else if( blendMode.blendType == LayerBlendType.Alpha ) {
                    if(i == 0)
                        device.TextureState[stage].AlphaArgument1 = d3dTexArg;
                    else if (i ==1)
                        device.TextureState[stage].AlphaArgument2 = d3dTexArg;
                }

                // Source2
                blendSource = blendMode.source2;
                if( blendMode.blendType == LayerBlendType.Color ) {
                    manualD3D = new ColorEx(1.0f, blendMode.colorArg2.r, blendMode.colorArg2.g, blendMode.colorArg2.b);
                }
                else if( blendMode.blendType == LayerBlendType.Alpha ) {
                    manualD3D = new ColorEx(blendMode.alphaArg2, 0, 0, 0);
                }
            }
        }
	
        /// <summary>
        /// 
        /// </summary>
        /// <param name="stage"></param>
        /// <param name="index"></param>
        protected override void SetTextureCoordSet(int stage, int index) {
            device.TextureState[stage].TextureCoordinateIndex = index;

            // store
            texStageDesc[stage].coordIndex = index;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="type"></param>
        /// <param name="filter"></param>
        protected override void SetTextureUnitFiltering(int unit, FilterType type, FilterOptions filter) {
            D3DTexType texType = texStageDesc[unit].texType;
            D3D.TextureFilter texFilter = D3DHelper.ConvertEnum(type, filter, d3dCaps, texType);

            switch(type) {
                case FilterType.Min:
                    device.SamplerState[unit].MinFilter = texFilter;
                    break;

                case FilterType.Mag:
                    device.SamplerState[unit].MagFilter = texFilter;
                    break;

                case FilterType.Mip:
                    device.SamplerState[unit].MipFilter = texFilter;
                    break;
            }
        }
	
        /// <summary>
        /// 
        /// </summary>
        /// <param name="stage"></param>
        /// <param name="xform"></param>
        protected override void SetTextureMatrix(int stage, Matrix4 xform) {
            DX.Matrix d3dMat = DX.Matrix.Identity;
            Matrix4 newMat = xform;

            /* If envmap is applied, but device doesn't support spheremap,
            then we have to use texture transform to make the camera space normal
            reference the envmap properly. This isn't exactly the same as spheremap
            (it looks nasty on flat areas because the camera space normals are the same)
            but it's the best approximation we have in the absence of a proper spheremap */
            if(texStageDesc[stage].autoTexCoordType == TexCoordCalcMethod.EnvironmentMap) {
                if(d3dCaps.VertexProcessingCaps.SupportsTextureGenerationSphereMap) {

                    // inverts the texture for a spheremap
                    Matrix4 matEnvMap = Matrix4.Identity;
                    matEnvMap.m11 = -1.0f;
                    newMat = newMat * matEnvMap;

                    // concatenate 
                    newMat = newMat * matEnvMap;
                }
                else {
                    /* If envmap is applied, but device doesn't support spheremap,
                    then we have to use texture transform to make the camera space normal
                    reference the envmap properly. This isn't exactly the same as spheremap
                    (it looks nasty on flat areas because the camera space normals are the same)
                    but it's the best approximation we have in the absence of a proper spheremap */
                    Matrix4 matEnvMap = Matrix4.Identity;
                    // set env_map values
                    matEnvMap.m00 = 0.5f;
                    matEnvMap.m03 = 0.5f;
                    matEnvMap.m11 = -0.5f;
                    matEnvMap.m13 = 0.5f;

                    // concatenate with the xForm
                    newMat = newMat * matEnvMap;
                }
            }

            // If this is a cubic reflection, we need to modify using the view matrix
            if(texStageDesc[stage].autoTexCoordType == TexCoordCalcMethod.EnvironmentMapReflection) {
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

            // convert to D3D format
            d3dMat = MakeD3DMatrix(newMat);

            // need this if texture is a cube map, to invert D3D's z coord
            if(texStageDesc[stage].autoTexCoordType != TexCoordCalcMethod.None) {
                d3dMat.M13 = -d3dMat.M13;
                d3dMat.M23 = -d3dMat.M23;
                d3dMat.M33 = -d3dMat.M33;
                d3dMat.M43 = -d3dMat.M43;
            }

            D3D.TransformType d3dTransType = (D3D.TransformType)((int)(D3D.TransformType.Texture0) + stage);

            // set the matrix if it is not the identity
            if(!D3DHelper.IsIdentity(ref d3dMat)) {
                // tell D3D the dimension of tex. coord
                int texCoordDim = 0;

                switch(texStageDesc[stage].texType) {
                    case D3DTexType.Normal:
                        texCoordDim = 2;
                        break;
                    case D3DTexType.Cube:
                    case D3DTexType.Volume:
                        texCoordDim = 3;
                        break;
                }

                // note: int values of D3D.TextureTransform correspond directly with tex dimension, so direct conversion is possible
                // i.e. Count1 = 1, Count2 = 2, etc
                device.TextureState[stage].TextureTransform = (D3D.TextureTransform)texCoordDim;

                // set the manually calculated texture matrix
                device.SetTransform(d3dTransType, d3dMat);
            }
            else {
                // disable texture transformation
                device.TextureState[stage].TextureTransform = D3D.TextureTransform.Disable;

                // set as the identity matrix
                device.SetTransform(d3dTransType, DX.Matrix.Identity);
            }
        }

        public override void SetScissorTest(bool enable, int left, int top, int right, int bottom) {
            if(enable) {
                device.ScissorRectangle = new System.Drawing.Rectangle(left, top, right - left, bottom - top);
                device.RenderState.ScissorTestEnable = true;
            }
            else {
                device.RenderState.ScissorTestEnable = false;
            }
        }
	
        /// <summary>
        ///		Helper method to go through and interrogate hardware capabilities.
        /// </summary>
        private void CheckCaps() {
            // get the number of possible texture units
            caps.NumTextureUnits = d3dCaps.MaxSimultaneousTextures;

            // max active lights
            caps.MaxLights = d3dCaps.MaxActiveLights;

            D3D.Surface surface = device.DepthStencilSurface;
     
            if(surface.Description.Format == D3D.Format.D24S8 || surface.Description.Format == D3D.Format.D24X8) {
                caps.SetCap(Capabilities.StencilBuffer);
                // always 8 here
                caps.StencilBufferBits = 8;
            }

            // Scissor test
            if(d3dCaps.RasterCaps.SupportsScissorTest) {
                caps.SetCap(Capabilities.ScissorTest);
            }

            // Anisotropy?
            if(d3dCaps.MaxAnisotropy > 1) {
                caps.SetCap(Capabilities.AnisotropicFiltering);
            }

            // Hardware mipmapping?
            if(d3dCaps.DriverCaps.CanAutoGenerateMipMap) {
                caps.SetCap(Capabilities.HardwareMipMaps);
            }

            // blending between stages is definately supported
            caps.SetCap(Capabilities.TextureBlending);
            caps.SetCap(Capabilities.MultiTexturing);

            // Dot3 bump mapping?
            if(d3dCaps.TextureOperationCaps.SupportsDotProduct3) {
                caps.SetCap(Capabilities.Dot3Bump);
            }

            // Cube mapping?
            if(d3dCaps.TextureCaps.SupportsCubeMap) {
                caps.SetCap(Capabilities.CubeMapping);
            }

            // D3D uses vertex buffers for everything
            caps.SetCap(Capabilities.VertexBuffer);

            // Texture Compression
            // We always support compression, D3DX will decompress if device does not support
            caps.SetCap(Capabilities.TextureCompression);
            caps.SetCap(Capabilities.TextureCompressionDXT);

            int vpMajor = d3dCaps.VertexShaderVersion.Major;
            int vpMinor = d3dCaps.VertexShaderVersion.Minor;
            int fpMajor = d3dCaps.PixelShaderVersion.Major;
            int fpMinor = d3dCaps.PixelShaderVersion.Minor;

            // check vertex program caps
            switch(vpMajor) {
                case 1:
                    caps.MaxVertexProgramVersion = "vs_1_1";
                    // 4d float vectors
                    caps.VertexProgramConstantFloatCount = d3dCaps.MaxVertexShaderConst;
                    // no int params supports
                    caps.VertexProgramConstantIntCount = 0;
                    break;
                case 2:
                    if(vpMinor > 0) {
                        caps.MaxVertexProgramVersion = "vs_2_x";
                    }
                    else {
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
            if(vpMajor >= 1) {
                caps.SetCap(Capabilities.VertexPrograms);
                gpuProgramMgr.PushSyntaxCode("vs_1_1");
            }
            if(vpMajor >= 2) {
                if(vpMajor > 2 || vpMinor > 0) {
                    gpuProgramMgr.PushSyntaxCode("vs_2_x");
                }
                gpuProgramMgr.PushSyntaxCode("vs_2_0");
            }
            if(vpMajor >= 3) {
                gpuProgramMgr.PushSyntaxCode("vs_3_0");
            }

            // Fragment Program Caps
            switch(fpMajor) {
                case 1:
                    caps.MaxFragmentProgramVersion = string.Format("ps_1_{0}", fpMinor);

                    caps.FragmentProgramConstantIntCount = 0;
                    // 8 4d float values, entered as floats but stored as fixed
                    caps.FragmentProgramConstantFloatCount = 8;
                    break;

                case 2:
                    if(fpMinor > 0) {
                        caps.MaxFragmentProgramVersion = "ps_2_x";
                        //16 integer params allowed
                        caps.FragmentProgramConstantIntCount = 16 * 4;
                        // 4d float params
                        caps.FragmentProgramConstantFloatCount = 224;
                    }
                    else {
                        caps.MaxFragmentProgramVersion = "ps_2_0";
                        // no integer params allowed
                        caps.FragmentProgramConstantIntCount = 0;
                        // 4d float params
                        caps.FragmentProgramConstantFloatCount = 32;
                    }

                    break;

                case 3:
                    if(fpMinor > 0) {
                        caps.MaxFragmentProgramVersion = "ps_3_x";
                    }
                    else {
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
            if(fpMajor >= 1) {
                caps.SetCap(Capabilities.FragmentPrograms);
                gpuProgramMgr.PushSyntaxCode("ps_1_1");

                if(fpMajor > 1 || fpMinor >= 2) {
                    gpuProgramMgr.PushSyntaxCode("ps_1_2");
                }
                if(fpMajor > 1 || fpMinor >= 3) {
                    gpuProgramMgr.PushSyntaxCode("ps_1_3");
                }
                if(fpMajor > 1 || fpMinor >= 4) {
                    gpuProgramMgr.PushSyntaxCode("ps_1_4");
                }
            }

            if(fpMajor >= 2) {
                gpuProgramMgr.PushSyntaxCode("ps_2_0");

                if(fpMinor > 0) {
                    gpuProgramMgr.PushSyntaxCode("ps_2_x");
                }
            }

            if(fpMajor >= 3) {
                gpuProgramMgr.PushSyntaxCode("ps_3_0");

                if(fpMinor > 0) {
                    gpuProgramMgr.PushSyntaxCode("ps_3_x");
                }
            }

            // register the HLSL program manager
            HighLevelGpuProgramManager.Instance.AddFactory(new HLSL.HLSLProgramFactory());

            // write hardware capabilities to registered log listeners
            caps.Log();
        }

        /// <summary>
        ///		Helper method that converts a DX Matrix to our Matrix4.
        /// </summary>
        /// <param name="d3dMat"></param>
        /// <returns></returns>
        private Matrix4 ConvertD3DMatrix(ref DX.Matrix d3dMat) {
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
    internal struct D3DTextureStageDesc {
        public D3DTexType texType;
        public int coordIndex;
        public TexCoordCalcMethod autoTexCoordType;
        public D3D.Texture tex;
    }

    /// <summary>
    ///		D3D texture types
    /// </summary>
    public enum D3DTexType {
        Normal,
        Cube,
        Volume,
        None
    }
}
