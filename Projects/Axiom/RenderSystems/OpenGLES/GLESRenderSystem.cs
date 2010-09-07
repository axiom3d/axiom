#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2010 Axiom Project Team
This file is part of Axiom.RenderSystems.OpenGLES
C# version developed by bostich.

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
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information
using System;
using Axiom.Graphics;
using Axiom.Graphics.Collections;
using Axiom.Core;
using Axiom.Media;
using Axiom.Math;
using Axiom.Configuration;
using OpenTK.Graphics.ES11;
using Axiom.RenderSystems.OpenGLES.OpenTKGLES;
#region Namespace Declarations
#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGLES
{
    /// <summary>
    /// 
    /// </summary>
	public class GLESRenderSystem : RenderSystem
	{
        public const int MaxLights = 8;

        #region - private -
        Light[] _lights = new Light[MaxLights];
        short _currentLights;
        /// View matrix to set world against
        Matrix4 _ViewMatrix;
        Matrix4 _worldMatrix;
        Matrix4 _textureMatrix;
        /// Last min & mip filtering options, so we can combine them
        FilterOptions mMinFilter;
        FilterOptions _mipFilter;
        /// <summary>
        /// 
        /// </summary>
        int _textureMimmapCount;
        /// <summary>
        /// What texture coord set each texture unit is using
        /// </summary>
        int[] _textureCoodIndex = new int[Config.MaxTextureLayers];
        /// <summary>
        /// Number of fixed-function texture units
        /// </summary>
        short _fixedFunctionTextureUnits;
        /// <summary>
        /// Store last colour write state
        /// </summary>
        bool[] _colorWrite = new bool[4];
        /// <summary>
        /// Store last depth write state
        /// </summary>
        bool _depthWrite;
        /// <summary>
        /// Store last stencil mask state
        /// </summary>
        uint _stencilMask;
        /// <summary>
        /// 
        /// </summary>
        float[] _autoTextureMatrix = new float[16];
        bool _useAutoTextureMatrix;
        int _textureCount;
        bool _textureEnabled;
        /// <summary>
        /// GL support class, used for creating windows etc.
        /// </summary>
        GLESSupport _glSupport;
        /// <summary>
        /// The main GL context - main thread only
        /// </summary>
        GLESContext _mainContext;
        /// <summary>
        /// The current GL context  - main thread only
        /// </summary>
        GLESContext _currentContext;
        /// <summary>
        /// 
        /// </summary>
        GLESGpuProgramManager _gpuProgramManager;
        HardwareBufferManager _hardwareBufferManager;
        /// <summary>
        /// Manager object for creating render textures.
        /// direct render to texture via GL_OES_framebuffer_object is preferable 
        /// to pbuffers, which depend on the GL support used and are generally 
        /// unwieldy and slow. However, FBO support for stencil buffers is poor.
        /// </summary>
        GLESRTTManager _rttManager;
        short _activeTextureUnit;
        short _activeClientTextureUnit;
        //Check if the GL system has already been initialized
        bool _glInitialized;
        /// <summary>
        /// OpenGL ES doesn't support setting the PolygonMode like desktop GL
        /// So we will cache the value and set it manually
        /// </summary>
        All _polygonMode;


        /// <summary>
        /// 
        /// </summary>
        private int CombindedMinMipFilter
        {
            get { throw new NotImplementedException(); }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tam"></param>
        /// <returns></returns>
        private int GetTextureAddressingMode(TextureAddressing tam)
        {
            throw new NotImplementedException(); 
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="blend"></param>
        /// <returns></returns>
        private int GetBlendMode(SceneBlendFactor blend)
        {
            throw new NotImplementedException(); 
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="glMatrix"></param>
        /// <param name="m"></param>
        private void MakeGLMatrix(float[] glMatrix, Matrix4 m)
        {
            throw new NotImplementedException(); 
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <param name="l"></param>
        private void SetGLLight(int index, Light l)
        {
            throw new NotImplementedException(); 
        }
        private void SetGLLightPositionDirection(Light lt, All lightindex)
        {
            throw new NotImplementedException(); 
        }
        private void SetLights()
        {
            throw new NotImplementedException(); 
        }

        private bool ActivateGLTextureUnit(int unit)
        {
            throw new NotImplementedException(); 
        }
        private bool ActivateGLClientTextureUnit(int unit)
        {
            throw new NotImplementedException(); 
        }
        void SetGLTexEnvi(All target, All name, int param)
        {
            throw new NotImplementedException(); 
        }
        void SetGLTexEnvf(All target, All name, float param)
        {
            throw new NotImplementedException(); 
        }
        void SetGLTexEnvfv(All target, All name, float param)
        {
            throw new NotImplementedException(); 
        }
        void SetGLPointParamf(All name, float param)
        {
            throw new NotImplementedException(); 
        }
        void SetGLPointParamfv(All name, float param)
        {
            throw new NotImplementedException(); 
        }
        void SetGLMaterialfv(All face, All name, float param)
        {
            throw new NotImplementedException(); 
        }
        void SetGLMatrixMode(All mode)
        {
            throw new NotImplementedException(); 
        }
        void SetGLDepthMask(bool flag)
        {
            throw new NotImplementedException(); 
        }
            //void setGLClearDepthf(OpenTK.Graphics.ES11. depth);
        void SetGLColorMask(bool red, bool green, bool blue, bool alpha)
        {
            throw new NotImplementedException(); 
        }
        void SetGLLightf(All light, All name, float param)
        {
            throw new NotImplementedException(); 
        }
        void SetGLLightfv(All light, All name, float param)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region - Abstracts -
        /// <summary>
        /// 
        /// </summary>
        public override string Name
        {
            get
            {
                return base.Name;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public override ConfigOptionCollection ConfigOptions
        {
            get
            {
                return base.ConfigOptions;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public override RenderSystemCapabilities HardwareCapabilities
        {
            get
            {
                return base.HardwareCapabilities;
            }
        }
        public override ColorEx AmbientLight
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
        public override Shading ShadingMode
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
        public override bool LightingEnabled
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
        public override bool NormalizeNormals
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public override void SetConfigOption(string name, string value)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ValidateConfiguration()
        {
            return base.ValidateConfiguration();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="autoCreateWindow"></param>
        /// <param name="windowTitle"></param>
        /// <returns></returns>
        public override RenderWindow Initialize(bool autoCreateWindow, string windowTitle)
        {
            return base.Initialize(autoCreateWindow, windowTitle);
        }
        /// <summary>
        /// 
        /// </summary>
        public override void Shutdown()
        {
            base.Shutdown();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="isFullScreen"></param>
        /// <param name="miscParams"></param>
        /// <returns></returns>
        public override RenderWindow CreateRenderWindow(string name, int width, int height, bool isFullScreen, Collections.NamedParameterList miscParams)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public override MultiRenderTarget CreateMultiRenderTarget(string name)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        public override void DestroyRenderWindow(string name)
        {
            base.DestroyRenderWindow(name);
        }
        public override void ApplyObliqueDepthProjection(ref Matrix4 projMatrix, Plane plane, bool forGpuProgram)
        {
            throw new NotImplementedException();
        }
        public override void AttachRenderTarget(RenderTarget target)
        {
            base.AttachRenderTarget(target);
        }
        public override void BeginFrame()
        {
            throw new NotImplementedException();
        }
        public override void BeginGeometryCount()
        {
            base.BeginGeometryCount();
        }
        public override void BindGpuProgram(GpuProgram program)
        {
            base.BindGpuProgram(program);
        }
        public override void BindGpuProgramParameters(GpuProgramType type, GpuProgramParameters parms)
        {
            throw new NotImplementedException();
        }
        public override void ClearFrameBuffer(FrameBufferType buffers, ColorEx color, float depth, int stencil)
        {
            throw new NotImplementedException();
        }
        public override int ConvertColor(ColorEx color)
        {
            throw new NotImplementedException();
        }
        public override ColorEx ConvertColor(int color)
        {
            throw new NotImplementedException();
        }
        public override Matrix4 ConvertProjectionMatrix(Matrix4 matrix, bool forGpuProgram)
        {
            throw new NotImplementedException();
        }
        public override IHardwareOcclusionQuery CreateHardwareOcclusionQuery()
        {
            throw new NotImplementedException();
        }
        public override CullingMode CullingMode
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
        public override float DepthBias
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
        public override bool DepthCheck
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
        public override CompareFunction DepthFunction
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
        public override bool DepthWrite
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
        public override void DestroyRenderTarget(string name)
        {
            base.DestroyRenderTarget(name);
        }
        public override void DestroyRenderTexture(string name)
        {
            base.DestroyRenderTexture(name);
        }
        public override RenderTarget DetachRenderTarget(RenderTarget target)
        {
            return base.DetachRenderTarget(target);
        }
        public override void DisableTextureUnit(int stage)
        {
            base.DisableTextureUnit(stage);
        }
        public override void DisableTextureUnitsFrom(int texUnit)
        {
            base.DisableTextureUnitsFrom(texUnit);
        }
        public override void Dispose()
        {
            base.Dispose();
        }
        public override void EnableClipPlane(ushort index, bool enable)
        {
            throw new NotImplementedException();
        }
        public override void EndFrame()
        {
            throw new NotImplementedException();
        }
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public override float HorizontalTexelOffset
        {
            get { throw new NotImplementedException(); }
        }
        public override void InitRenderTargets()
        {
            base.InitRenderTargets();
        }
        public override bool InvertVertexWinding
        {
            get
            {
                return base.InvertVertexWinding;
            }

            set
            {
                base.InvertVertexWinding = value;
            }
        }
        public override bool IsVSync
        {
            get
            {
                return base.IsVSync;
            }
            set
            {
                base.IsVSync = value;
            }
        }
        public override Matrix4 MakeOrthoMatrix(float fov, float aspectRatio, float near, float far, bool forGpuPrograms)
        {
            throw new NotImplementedException();
        }
        public override Matrix4 MakeProjectionMatrix(float fov, float aspectRatio, float near, float far, bool forGpuProgram)
        {
            throw new NotImplementedException();
        }
        public override Matrix4 MakeProjectionMatrix(float left, float right, float bottom, float top, float nearPlane, float farPlane, bool forGpuProgram)
        {
            throw new NotImplementedException();
        }
        public override Real MaximumDepthInputValue
        {
            get { throw new NotImplementedException(); }
        }
        public override Real MinimumDepthInputValue
        {
            get { throw new NotImplementedException(); }
        }
        public override bool PointSprites
        {
            set { throw new NotImplementedException(); }
        }
        public override PolygonMode PolygonMode
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
        public override Matrix4 ProjectionMatrix
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
        public override void RemoveRenderTargets()
        {
            base.RemoveRenderTargets();
        }
        public override void Render(RenderOperation op)
        {
            base.Render(op);
        }
        public override void SetAlphaRejectSettings(CompareFunction func, int val, bool alphaToCoverage)
        {
            throw new NotImplementedException();
        }
        public override void SetClipPlane(ushort index, float A, float B, float C, float D)
        {
            throw new NotImplementedException();
        }
        public override void UseLights(Core.Collections.LightList lightList, int limit)
        {
            throw new NotImplementedException();
        }
        public override void SetViewport(Viewport viewport)
        {
            throw new NotImplementedException();
        }
        public override void SetWorldMatrices(Matrix4[] matrices, ushort count)
        {
            base.SetWorldMatrices(matrices, count);
        }
        public override void SetColorBufferWriteEnabled(bool red, bool green, bool blue, bool alpha)
        {
            throw new NotImplementedException();
        }
        public override void SetTextureUnitFiltering(int stage, FilterType type, FilterOptions filter)
        {
            throw new NotImplementedException();
        }
        public override void SetTextureMatrix(int stage, Matrix4 xform)
        {
            throw new NotImplementedException();
        }
        public override void SetDepthBufferParams(bool depthTest, bool depthWrite, CompareFunction depthFunction)
        {
            throw new NotImplementedException();
        }
        public override void SetFog(Graphics.FogMode mode, ColorEx color, float density, float start, float end)
        {
            throw new NotImplementedException();
        }
        public override void SetPointParameters(float size, bool attenuationEnabled, float constant, float linear, float quadratic, float minSize, float maxSize)
        {
            throw new NotImplementedException();
        }
        public override void SetSceneBlending(SceneBlendFactor src, SceneBlendFactor dest)
        {
            throw new NotImplementedException();
        }
        public override void SetScissorTest(bool enable, int left, int top, int right, int bottom)
        {
            throw new NotImplementedException();
        }
        public override void SetSeparateSceneBlending(SceneBlendFactor sourceFactor, SceneBlendFactor destFactor, SceneBlendFactor sourceFactorAlpha, SceneBlendFactor destFactorAlpha)
        {
            throw new NotImplementedException();
        }
        public override void SetStencilBufferParams(CompareFunction function, int refValue, int mask, StencilOperation stencilFailOp, StencilOperation depthFailOp, StencilOperation passOp, bool twoSidedOperation)
        {
            throw new NotImplementedException();
        }
        public override void SetSurfaceParams(ColorEx ambient, ColorEx diffuse, ColorEx specular, ColorEx emissive, float shininess, TrackVertexColor tracking)
        {
            throw new NotImplementedException();
        }
        public override void SetTexture(int stage, bool enabled, string textureName)
        {
            throw new NotImplementedException();
        }
        public override void SetTextureAddressingMode(int stage, TextureAddressing texAddressingMode)
        {
            throw new NotImplementedException();
        }
        public override void SetTextureBlendMode(int stage, LayerBlendModeEx blendMode)
        {
            throw new NotImplementedException();
        }
        public override void SetTextureBorderColor(int stage, ColorEx borderColor)
        {
            throw new NotImplementedException();
        }
        public override void SetTextureCoordCalculation(int stage, TexCoordCalcMethod method, Frustum frustum)
        {
            throw new NotImplementedException();
        }
        public override void SetTextureCoordSet(int stage, int index)
        {
            throw new NotImplementedException();
        }
        public override void SetTextureLayerAnisotropy(int stage, int maxAnisotropy)
        {
            throw new NotImplementedException();
        }
        public override Matrix4 WorldMatrix
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
        public override Matrix4 ViewMatrix
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
        public override float VerticalTexelOffset
        {
            get { throw new NotImplementedException(); }
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
        
        #endregion
        /// <summary>
        /// Default ctor.
        /// </summary>
        public GLESRenderSystem()
        {
            _depthWrite = true;
            _stencilMask = 0xFFFFFFFF;
            int i;

            LogManager.Instance.Write(string.Format("{0} created.", Name));

            _glSupport = OpenTKGLESUtil.GLESSupport;

            for (i = 0; i < MaxLights; i++)
                _lights[i] = null;

            _worldMatrix = Matrix4.Identity;
            _ViewMatrix = Matrix4.Identity;


        }
    }
}

