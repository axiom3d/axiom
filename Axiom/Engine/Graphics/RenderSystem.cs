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
using System.Reflection;
using Axiom.Core;
using Axiom.Collections;
using Axiom.Configuration;
using Axiom.Graphics;
using Axiom.Utility;
using Axiom.MathLib;

namespace Axiom.Graphics {
    /// <summary>
    /// Defines the functionality of a 3D API
    /// </summary>
    ///	<remarks>
    ///		The RenderSystem class provides a base class
    ///		which abstracts the general functionality of the 3D API
    ///		e.g. Direct3D or OpenGL. Whilst a few of the general
    ///		methods have implementations, most of this class is
    ///		abstract, requiring a subclass based on a specific API
    ///		to be constructed to provide the full functionality.
    ///		<p/>
    ///		Note there are 2 levels to the interface - one which
    ///		will be used often by the caller of the engine library,
    ///		and one which is at a lower level and will be used by the
    ///		other classes provided by the engine. These lower level
    ///		methods are marked as internal, and are not accessible outside
    ///		of the Core library.
    ///	</remarks>
    // INC: In progress
    public abstract class RenderSystem : IDisposable {
        #region Member variables

        protected RenderWindowCollection renderWindows;
        protected TextureManager textureMgr;
        protected GpuProgramManager gpuProgramMgr;
        protected HardwareBufferManager hardwareBufferManager;
        protected CullingMode cullingMode;
        protected bool isVSync;
        protected bool depthWrite;
        protected int numCurrentLights;

        // Stored options
        protected EngineConfig engineConfig = new EngineConfig();

        // Active viewport (dest for future rendering operations) and target
        protected Viewport activeViewport;
        protected RenderTarget activeRenderTarget;

        protected int numFaces, numVertices;

        // used to determine capabilies of the hardware
        protected HardwareCaps caps = new HardwareCaps();

        /// Saved set of world matrices
        protected Matrix4[] worldMatrices = new Matrix4[256];

        #endregion

        #region Constructor

        public RenderSystem() {
            this.renderWindows = new RenderWindowCollection();
			
            // default to true
            isVSync = true;

            // default to true
            depthWrite = true;

            // This means CULL clockwise vertices, i.e. front of poly is counter-clockwise
            // This makes it the same as OpenGL and other right-handed systems
            this.cullingMode = Axiom.Graphics.CullingMode.Clockwise; 
        }

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the name of this RenderSystem based on it's assembly attribute Title.
        /// </summary>
        public virtual string Name {
            get {
                AssemblyTitleAttribute attribute = 
                    (AssemblyTitleAttribute)Attribute.GetCustomAttribute(this.GetType().Assembly, typeof(AssemblyTitleAttribute), false);

                if(attribute != null)
                    return attribute.Title;
                else
                    return "Not Found";
            }
        }

        /// <summary>
        /// Gets/Sets a value that determines whether or not to wait for the screen to finish refreshing
        /// before drawing the next frame.
        /// </summary>
        public bool IsVSync {
            get { return this.isVSync; }
            set { this.isVSync = value; }
        }

        /// <summary>
        ///		Gets a set of hardware capabilities queryed by the current render system.
        /// </summary>
        public HardwareCaps Caps {
            get { return caps; }
        }

        /// <summary>
        /// Gets a dataset with the options set for the rendering system.
        /// </summary>
        public EngineConfig ConfigOptions {
            get { return this.engineConfig; }
        }

        /// <summary>
        /// Gets a collection of the RenderSystems list of RenderWindows.
        /// </summary>
        public RenderWindowCollection RenderWindows {
            get { return this.renderWindows; }
        }

        /// <summary>
        /// 
        /// </summary>
        public int FacesRendered {
            get { return numFaces; }
        }

        #endregion

        #region Abstract properties

        /// <summary>
        ///		Sets the color & strength of the ambient (global directionless) light in the world.
        /// </summary>
        public abstract ColorEx AmbientLight { set; }

        /// <summary>
        ///    
        /// </summary>
        public abstract CullingMode CullingMode { get; set; }

        /// <summary>
        ///		Sets the type of light shading required (default = Gouraud).
        /// </summary>
        public abstract Shading ShadingMode { set; }

        /// <summary>
        ///		Sets the type of texture filtering used when rendering
        ///	</summary>
        ///	<remarks>
        ///		This method sets the kind of texture filtering applied when rendering textures onto
        ///		primitives. Filtering covers how the effects of minification and magnification are
        ///		disguised by resampling.
        /// </remarks>
        public abstract TextureFiltering TextureFiltering { set; }

        /// <summary>
        ///		Sets whether or not dynamic lighting is enabled.
        ///		<p/>
        ///		If true, dynamic lighting is performed on geometry with normals supplied, geometry without
        ///		normals will not be displayed. If false, no lighting is applied and all geometry will be full brightness.
        /// </summary>
        public abstract bool LightingEnabled { set; }

        /// <summary>
        ///		Turns stencil buffer checking on or off. 
        /// </summary>
        ///	<remarks>
        ///		Stencilling (masking off areas of the rendering target based on the stencil 
        ///		buffer) can be turned on or off using this method. By default, stencilling is
        ///		disabled.
        ///	</remarks>
        public abstract bool StencilCheckEnabled { set; }

        /// <summary>
        ///		Sets the stencil test function.
        /// </summary>
        /// <remarks>
        ///		The stencil test is:
        ///		(Reference Value & Mask) CompareFunction (Stencil Buffer Value & Mask)
        ///	</remarks>
        public abstract CompareFunction StencilBufferFunction { set; }

        /// <summary>
        ///		Sets the stencil buffer reference value.
        /// </summary>
        ///	<remarks>
        ///		This value is used in the stencil test:
        ///		(Reference Value & Mask) CompareFunction (Stencil Buffer Value & Mask)
        ///		It can also be used as the destination value for the stencil buffer if the
        ///		operation which is performed is StencilOperation.Replace.
        /// </remarks>
        public abstract int StencilBufferReferenceValue { set; }

        /// <summary>
        ///		Sets the stencil buffer mask value.
        /// </summary>
        ///<remarks>
        ///		This is applied thus:
        ///		(Reference Value & Mask) CompareFunction (Stencil Buffer Value & Mask)
        ///	</remarks>
        public abstract int StencilBufferMask { set; }

        /// <summary>
        ///		Sets the action to perform if the stencil test fails.
        /// </summary>
        public abstract StencilOperation StencilBufferFailOperation { set; }

        /// <summary>
        ///		Sets the action to perform if the stencil test passes, but the depth
        ///		buffer test fails.
        /// </summary>
        public abstract StencilOperation StencilBufferDepthFailOperation { set; }

        /// <summary>
        ///		Sets the action to perform if both the stencil test and the depth buffer 
        ///		test passes.
        /// </summary>
        public abstract StencilOperation StencilBufferPassOperation { set; }

        #endregion

        #region Overridable virtual methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="autoCreateWindow"></param>
        public abstract RenderWindow Initialize(bool autoCreateWindow);

        /// <summary>
        ///		Shuts down the RenderSystem.
        /// </summary>
        public virtual void Shutdown() {
            // destroy each render window
            foreach(RenderWindow window in renderWindows) {
                window.Destroy();
            }

            // Clear the render window list
            renderWindows.Clear();

            // dispose of the render system
            this.Dispose();
        }
        #endregion

        #region Abstract methods

        /// <summary>
        /// Creates a new rendering window.
        /// </summary>
        /// <remarks>
        ///	This method creates a new rendering window as specified
        ///	by the paramteters. The rendering system could be
        ///	responible for only a single window (e.g. in the case
        ///	of a game), or could be in charge of multiple ones (in the
        ///	case of a level editor). The option to create the window
        ///	as a child of another is therefore given.
        ///	This method will create an appropriate subclass of
        /// RenderWindow depending on the API and platform implementation.
        /// </remarks>
        /// <returns></returns>
        public abstract RenderWindow CreateRenderWindow(string name, System.Windows.Forms.Control target, int width, int height, int colorDepth,
            bool isFullscreen, int left, int top, bool depthBuffer, RenderWindow parent);

        /// <summary>
        ///		Builds a perspective projection matrix suitable for this render system.
        /// </summary>
        /// <remarks>
        ///		Because different APIs have different requirements (some incompatible) for the
        ///		projection matrix, this method allows each to implement their own correctly and pass
        ///		back a generic Matrix3 for storage in the engine.
        ///	 </remarks>
        /// <param name="fov"></param>
        /// <param name="aspectRatio"></param>
        /// <param name="near"></param>
        /// <param name="far"></param>
        /// <returns></returns>
        public abstract Matrix4 MakeProjectionMatrix(float fov, float aspectRatio, float near, float far);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stage"></param>
        /// <param name="func"></param>
        /// <param name="val"></param>
        internal protected abstract void SetAlphaRejectSettings(int stage, CompareFunction func, byte val);

        /// <summary>
        ///		Sets the fog with the given params.
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="color"></param>
        /// <param name="density"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        abstract internal protected void SetFog(FogMode mode, ColorEx color, float density, float start, float end);

        /// <summary>
        ///		Converts the System.Drawing.Color value to a uint.  Each API may need the 
        ///		bytes of the packed color data in different orders. i.e. OpenGL - ABGR, D3D - ARGB
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public abstract int ConvertColor(ColorEx color);

        /// <summary>
        ///		Sets the global blending factors for combining subsequent renders with the existing frame contents.
        ///		The result of the blending operation is:</p>
        ///		<p align="center">final = (texture * src) + (pixel * dest)</p>
        ///		Each of the factors is specified as one of a number of options, as specified in the SceneBlendFactor
        ///		enumerated type.
        /// </summary>
        /// <param name="src">The source factor in the above calculation, i.e. multiplied by the texture color components.</param>
        /// <param name="dest">The destination factor in the above calculation, i.e. multiplied by the pixel color components.</param>
        abstract internal protected void SetSceneBlending(SceneBlendFactor src, SceneBlendFactor dest);

        /// <summary>
        ///		Sets the surface parameters to be used during rendering an object.
        /// </summary>
        /// <param name="ambient"></param>
        /// <param name="diffuse"></param>
        /// <param name="specular"></param>
        /// <param name="emissive"></param>
        /// <param name="shininess"></param>
        abstract internal protected void SetSurfaceParams(ColorEx ambient, ColorEx diffuse, ColorEx specular, ColorEx emissive, float shininess);

        /// <summary>
        ///		Tells the hardware how to treat texture coordinates.
        /// </summary>
        /// <param name="stage"></param>
        /// <param name="texAddressingMode"></param>
        abstract internal protected void SetTextureAddressingMode(int stage, TextureAddressing texAddressingMode);

        /// <summary>
        ///    Tells the rendersystem to use the attached set of lights (and no others) 
        ///    up to the number specified (this allows the same list to be used with different
        ///    count limits).
        /// </summary>
        /// <param name="lightList">List of lights.</param>
        /// <param name="limit">Max number of lights that can be used from the list currently.</param>
        protected abstract internal void UseLights(LightList lightList, int limit);

        #endregion

        #region Protected methods

        /// <summary>
        ///		Performs a software vertex blend on the passed in operation. 
        ///	</summary>
        ///	<remarks>
        ///		This function is supplied to calculate a vertex blend when no hardware
        ///		support is available. The vertices contained in the passed in operation
        ///		will be modified by the matrices supplied according to the blending weights
        ///		also in the operation. To avoid accidentally modifying core vertex data, a
        ///		temporary vertex buffer is used for the result, which is then used in the
        ///		VertexBuffer instead of the original passed in vertex data.
        /// </remarks>
        protected unsafe void SoftwareVertexBlend(VertexData vertexData, Matrix4[] matrices) {

            Vector3 sourceVec = new Vector3();
            Vector3 accumVecPos;
            Vector3 accumVecNorm = new Vector3();
            Matrix3 rot3x3;

            // pointers to the locked vertex buffer data
            IntPtr destPosPtr, destNormPtr;
            float* pDestPos, pDestNorm = null;
            
            bool posNormShareBuffer = false;

            // look for the position and normal elements
            VertexElement posElement =
                vertexData.vertexDeclaration.FindElementBySemantic(VertexElementSemantic.Position);
            VertexElement normElement =
                vertexData.vertexDeclaration.FindElementBySemantic(VertexElementSemantic.Normal);

            HardwareVertexBuffer posBuffer, normBuffer = null;

            // get the position buffer
            posBuffer = vertexData.vertexBufferBinding.GetBuffer(posElement.Source);
            
            if(normElement != null) {
                normBuffer = vertexData.vertexBufferBinding.GetBuffer(normElement.Source);
                posNormShareBuffer = (posElement.Source == normElement.Source);
            }

            // lock buffers for writing
            Debug.Assert(posElement.Offset == 0, "Positions must be the first element in a dedicated buffer");

            // lock buffer writing
            destPosPtr = posBuffer.Lock(BufferLocking.Discard);
            pDestPos = (float*)destPosPtr.ToPointer();

            // deal with normal buffer if it is available
            if(normElement != null) {
                if(posNormShareBuffer) {
                    Debug.Assert(normElement.Offset == (sizeof(float) * 3), "Normals must be packed directly after positions in a shared buffer!");
                    // seperate normal buffer will not be used
                }
                else {
                    Debug.Assert(normElement.Offset == 0, "Normals must be first element in dedicated buffer!");
                    destNormPtr = normBuffer.Lock(BufferLocking.Discard);
                    pDestNorm = (float*)destNormPtr.ToPointer();
                } // if
            } // if

            // index values for walking through each array
            int srcPosIdx, srcNormIdx, blendIndicesIdx, blendWeightIdx; 
            int destPosIdx, destNormIdx;
            srcPosIdx = srcNormIdx = blendIndicesIdx = blendWeightIdx = destPosIdx = destNormIdx = 0; 

            // get references to the 4 array for convenience
            float[] srcPos = vertexData.softwareBlendInfo.srcPositions;
            float[] srcNorm = vertexData.softwareBlendInfo.srcNormals;
            float[] blendWeights = vertexData.softwareBlendInfo.blendWeights;
            byte[] blendIndices = vertexData.softwareBlendInfo.blendIndices;

            // time for some blending
            for(int i = 0; i < vertexData.vertexCount; i++) {
                sourceVec.x = srcPos[srcPosIdx++];
                sourceVec.y = srcPos[srcPosIdx++];
                sourceVec.z = srcPos[srcPosIdx++];

                if(normElement != null) {
                    accumVecNorm.x = srcNorm[srcNormIdx++];
                    accumVecNorm.y = srcNorm[srcNormIdx++];
                    accumVecNorm.z = srcNorm[srcNormIdx++];
                }

                // reset accumulator
                accumVecPos = Vector3.Zero;

                // loop per blend weight
                for(ushort blendIdx = 0; blendIdx < vertexData.softwareBlendInfo.numWeightsPerVertex; blendIdx++) {
                    // Blend by multiplying source by blend matrix and scaling by weight
                    // Add to accumulator
                    // weights must be normalized!!
                    if(blendWeights[blendWeightIdx] != 0.0f) {
                        int matrixIndex = blendIndices[blendIndicesIdx];
                        // blend position
                        accumVecPos += (matrices[matrixIndex] * sourceVec) * blendWeights[blendWeightIdx];

                        if(normElement != null) {
                            // Blend normal
                            // We should blend by inverse transpose here, but because we're assuming the 3x3
                            // aspect of the matrix is orthogonal (no non-uniform scaling), the inverse transpose
                            // is equal to the main 3x3 matrix
                            // Note because it's a normal we just extract the rotational part, saves us renormalising
                            rot3x3 = matrices[matrixIndex].GetMatrix3();
                            accumVecNorm = (rot3x3 * blendWeights[blendWeightIdx]) * accumVecNorm;
                        } // if
                    } // if
                    
                    // increment the index counters
                    blendWeightIdx++;
                    blendIndicesIdx++;
                } // for blend weights

                //store blended vertex in hardware buffer
                pDestPos[destPosIdx++] = accumVecPos.x;
                pDestPos[destPosIdx++] = accumVecPos.y;
                pDestPos[destPosIdx++] = accumVecPos.z;

                if(normElement != null) {
                    if(posNormShareBuffer) {
                        // pack into the same buffer
                        pDestPos[destPosIdx++] = accumVecNorm.x;
                        pDestPos[destPosIdx++] = accumVecNorm.y;
                        pDestPos[destPosIdx++] = accumVecNorm.z;
                    }
                    else {
                        pDestNorm[destNormIdx++] = accumVecNorm.x;
                        pDestNorm[destNormIdx++] = accumVecNorm.y;
                        pDestNorm[destNormIdx++] = accumVecNorm.z;
                    }
                } // if normElement
            } // for vertices

            // unlock position buffer
            posBuffer.Unlock();

            // unlock normal buffer if available
            if(normElement != null && !posNormShareBuffer) {
                normBuffer.Unlock();
            }
        }

        #endregion

        #region Object overrides

        /// <summary>
        /// Returns the name of this RenderSystem.
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            return this.Name;
        }

        #endregion

        #region Internal engine methods and properties

        /// <summary>
        /// 
        /// </summary>
        abstract protected internal bool DepthWrite { set; }

        /// <summary>
        /// 
        /// </summary>
        abstract protected internal bool DepthCheck { set; }

        /// <summary>
        /// 
        /// </summary>
        abstract protected internal CompareFunction DepthFunction { set; }

        /// <summary>
        /// 
        /// </summary>
        abstract protected internal int DepthBias { set; }

        /// <summary>Sets the current view matrix.</summary>
        abstract protected internal Matrix4 ViewMatrix	{ set; }

        /// <summary>Sets the current world matrix.</summary>
        abstract protected internal Matrix4 WorldMatrix { set; }

        /// <summary>Sets the current projection matrix.</summary>
        abstract protected internal Matrix4 ProjectionMatrix { set; }

        /// <summary>
        ///		Sets how to rasterise triangles, as points, wireframe or solid polys.
        /// </summary>
        abstract protected internal SceneDetailLevel RasterizationMode { set; }

        /// <summary>
        ///		Signifies the beginning of a frame, ie the start of rendering on a single viewport. Will occur
        ///		several times per complete frame if multiple viewports exist.
        /// </summary>
        abstract protected internal void BeginFrame();

        /// <summary>
        ///    Binds a given GpuProgram (but not the parameters). 
        /// </summary>
        /// <remarks>
        ///    Only one GpuProgram of each type can be bound at once, binding another
        ///    one will simply replace the existing one.
        /// </remarks>
        /// <param name="program"></param>
        public abstract void BindGpuProgram(GpuProgram program);

        /// <summary>
        ///    Bind Gpu program parameters.
        /// </summary>
        /// <param name="parms"></param>
        public abstract void BindGpuProgramParameters(GpuProgramType type, GpuProgramParameters parms);

        /// <summary>
        ///    Unbinds the current GpuProgram of a given GpuProgramType.
        /// </summary>
        /// <param name="type"></param>
        public abstract void UnbindGpuProgram(GpuProgramType type);

        /// <summary>
        ///		Ends rendering of a frame to the current viewport.
        /// </summary>
        abstract protected internal void EndFrame();

        /// <summary>
        ///		Sets the details of a texture stage, to be used for all primitives
        ///		rendered afterwards. User processes would
        ///		not normally call this direct unless rendering
        ///		primitives themselves - the SubEntity class
        ///		is designed to manage materials for objects.
        ///		Note that this method is called by SetMaterial.
        /// </summary>
        /// <param name="stage">The index of the texture unit to modify. Multitexturing hardware
        //		can support multiple units (see NumTextureUnits)</param>
        /// <param name="enabled">Boolean to turn the unit on/off</param>
        /// <param name="textureName">The name of the texture to use - this should have
        ///		already been loaded with TextureManager.Load.</param>
        abstract protected internal void SetTexture(int stage, bool enabled, string textureName);

        /// <summary>
        ///		Sets a method for automatically calculating texture coordinates for a stage.
        /// </summary>
        /// <param name="stage">Texture stage to modify.</param>
        /// <param name="method">Calculation method to use</param>
        abstract protected internal void SetTextureCoordCalculation(int stage, TexCoordCalcMethod method);

        /// <summary>
        ///		Sets the index into the set of tex coords that will be currently used by the render system.
        /// </summary>
        /// <param name="stage"></param>
        /// <param name="index"></param>
        abstract protected internal void SetTextureCoordSet(int stage, int index);

        /// <summary>
        ///		Sets the filtering level to use for textures in the specified unit.
        /// </summary>
        /// <param name="stage"></param>
        /// <param name="index"></param>
        abstract protected internal void SetTextureLayerFiltering(int stage, TextureFiltering filtering);

        /// <summary>
        ///		Sets the maximal anisotropy for the specified texture unit.
        /// </summary>
        /// <param name="stage"></param>
        /// <param name="index">maxAnisotropy</param>
        abstract protected internal void SetTextureLayerAnisotropy(int stage, int maxAnisotropy);

        /// <summary>
        ///		Sets the texture matrix for the specified stage.  Used to apply rotations, translations,
        ///		and scaling to textures.
        /// </summary>
        /// <param name="stage"></param>
        /// <param name="xform"></param>
        abstract protected internal void SetTextureMatrix(int stage, Matrix4 xform);

        /// <summary>
        ///		Sets the current viewport that will be rendered to.
        /// </summary>
        /// <param name="viewport"></param>
        abstract protected internal void SetViewport(Viewport viewport);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="op"></param>
        /// DOC
        public virtual void Render(RenderOperation op) {
            int val;

            if(op.useIndices)
                val = op.indexData.indexCount;
            else
                val = op.vertexData.vertexCount;

            // calculate faces
            switch(op.operationType) {
                case RenderMode.TriangleList:
                    numFaces += val / 3;
                    break;
                case RenderMode.TriangleStrip:
                case RenderMode.TriangleFan:
                    numFaces += val - 2;
                    break;
                case RenderMode.PointList:
                case RenderMode.LineList:
                case RenderMode.LineStrip:
                    break;
            }

            // increment running vertex count
            numVertices += op.vertexData.vertexCount;

            // if hardware vertex blending isn't supported, check if we need to do
            // it in software
            /*if(!Caps.CheckCap(Capabilities.VertexBlending)) {
                bool vertexBlend = false;

                IList elements = op.vertexData.vertexDeclaration.Elements;
				
                // see if we need to calc ver
                for(int i = 0; i < elements.Count; i++) {
                    VertexElement element = (VertexElement)elements[i];

                    // if we found a blend weights element, flag and break
                    if(element.Semantic == VertexElementSemantic.BlendWeights) {
                        vertexBlend = true;
                        break;
                    }
                }

                if(vertexBlend)
                    SoftwareVertexBlend(op, worldMatrices);
               */
                
            if(op.vertexData.softwareBlendInfo != null && op.vertexData.softwareBlendInfo.automaticBlend) {
                SoftwareVertexBlend(op.vertexData, worldMatrices);
            }
        }

        /// <summary>
        ///		Utility function for setting all the properties of a texture unit at once.
        ///		This method is also worth using over the individual texture unit settings because it
        ///		only sets those settings which are different from the current settings for this
        ///		unit, thus minimising render state changes.
        /// </summary>
        /// <param name="textureUnit">Index of the texture unit to configure</param>
        /// <param name="layer">Reference to a TextureLayer object which defines all the settings.</param>
        protected virtual internal void SetTextureUnit(int stage, TextureUnitState layer) {
            // set the texture if it is different from the current
            SetTexture(stage, true, layer.TextureName);

            // Tex Coord Set
            SetTextureCoordSet(stage, layer.TextureCoordSet);

            // Texture layer filtering
            SetTextureLayerFiltering(stage, layer.TextureFiltering);

            // Texture layer anistropy
            SetTextureLayerAnisotropy(stage, layer.TextureAnisotropy);

            // set the texture blending mode
            SetTextureBlendMode(stage, layer.ColorBlendMode);

            // set the texture blending mode
            SetTextureBlendMode(stage, layer.AlphaBlendMode);

            // this must always be set for OpenGL.  DX9 will ignore dupe render states like this (observed in the
            // output window when debugging with high verbosity), so there is no harm
            SetTextureAddressingMode(stage, layer.TextureAddressing);

            bool anyCalcs = false;

            for(int i = 0; i < layer.NumEffects; i++) {
                TextureEffect effect = layer.GetEffect(i);

                switch(effect.type) {
                    case TextureEffectType.EnvironmentMap:
                        if((EnvironmentMap)effect.subtype == EnvironmentMap.Curved) {
                            SetTextureCoordCalculation(stage, TexCoordCalcMethod.EnvironmentMap);
                            anyCalcs = true;
                        }
                        else if((EnvironmentMap)effect.subtype == EnvironmentMap.Planar) {
                            SetTextureCoordCalculation(stage, TexCoordCalcMethod.EnvironmentMapPlanar);
                            anyCalcs = true;
                        }
                        else if((EnvironmentMap)effect.subtype == EnvironmentMap.Reflection) {
                            SetTextureCoordCalculation(stage, TexCoordCalcMethod.EnvironmentMapReflection);
                            anyCalcs = true;
                        }
                        else if((EnvironmentMap)effect.subtype == EnvironmentMap.Normal) {
                            SetTextureCoordCalculation(stage, TexCoordCalcMethod.EnvironmentMapNormal);
                            anyCalcs = true;
                        }
                        break;

                    case TextureEffectType.BumpMap:
                    case TextureEffectType.Scroll:
                    case TextureEffectType.Rotate:
                    case TextureEffectType.Transform:
                        break;
                } // switch
            } // for

            // Ensure any previous texcoord calc settings are reset if there are now none
            if(!anyCalcs) {
                SetTextureCoordCalculation(stage, TexCoordCalcMethod.None);
                SetTextureCoordSet(stage, layer.TextureCoordSet);
            }

            // set the texture matrix to that of the current layer for any transformations
            SetTextureMatrix(stage, layer.TextureMatrix);

            // set alpha rejection
            SetAlphaRejectSettings(stage, layer.AlphaRejectFunction, layer.AlphaRejectValue);
        }

        /// <summary>
        ///		Sets the texture blend modes from a TextureLayer record.
        ///		Meant for use internally only - apps should use the Material
        ///		and TextureLayer classes.
        /// </summary>
        /// <param name="stage">Texture unit.</param>
        /// <param name="blendMode">Details of the blending modes.</param>
        public abstract void SetTextureBlendMode(int stage, LayerBlendModeEx blendMode);

        /// <summary>
        ///		Turns off a texture unit if not needed.
        /// </summary>
        /// <param name="textureUnit"></param>
        protected virtual internal void DisableTextureUnit(int textureUnit) {
            SetTexture(textureUnit, false, "");
        }

        /// <summary>
        ///	
        /// </summary>
        /// <param name="matrices"></param>
        /// <param name="count"></param>
        virtual internal void SetWorldMatrices(Matrix4[] matrices, ushort count) {
            if(!caps.CheckCap(Capabilities.VertexBlending)) {
                // save these for later during software vertex blending
                for(int i = 0; i < count; i++)
                    worldMatrices[i] = matrices[i];

                // reset the hardware world matrix to identity
                WorldMatrix = Matrix4.Identity;
            }

            // TODO: Implement hardware vertex blending in the API's
        }

        /// <summary>
        /// 
        /// </summary>
        internal void BeginGeometryCount() {
            numFaces = 0;
        }

        #endregion

        #region IDisposable Members

        public void Dispose() {
            //if(textureMgr != null)
            //	textureMgr.Dispose();

        }

        #endregion
    }

}