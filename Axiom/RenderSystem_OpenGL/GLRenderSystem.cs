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
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Axiom.Collections;
using Axiom.Configuration;
using Axiom.Core;
using Axiom.MathLib;
using Axiom.Graphics;
using Axiom.Utility;
using Tao.OpenGl;
using Tao.Platform.Windows;

namespace Axiom.RenderSystems.OpenGL {

    /// <summary>
    /// Summary description for OpenGLRenderer.
    /// </summary>
    public class GLRenderSystem : RenderSystem, IPlugin {
        #region Fields

        /// <summary>Retains initial screen settings.</summary>        
        protected Gdi.DEVMODE intialScreenSettings;
        /// <summary>GDI Device Context</summary>
        private static IntPtr hDC = IntPtr.Zero;
        /// <summary>Rendering context.</summary>
        private static IntPtr hRC = IntPtr.Zero;
        /// <summary>Window handle.</summary>
        private static IntPtr hWnd = IntPtr.Zero;

        /// <summary>Internal view matrix.</summary>
        protected Matrix4 viewMatrix;
        /// <summary>Internal world matrix.</summary>
        protected Matrix4 worldMatrix;
        /// <summary>Internal texture matrix.</summary>
        protected Matrix4 textureMatrix;

        // used for manual texture matrix calculations, for things like env mapping
        protected bool useAutoTextureMatrix;
        protected float[] autoTextureMatrix = new float[16];
        protected ushort[] texCoordIndex = new ushort[Config.MaxTextureLayers];

        // keeps track of type for each stage (2d, 3d, cube, etc)
        protected int[] textureTypes = new int[Config.MaxTextureLayers];

        // retained stencil buffer params vals, since we allow setting invidual params but GL
        // only lets you set them all at once, keep old values around to allow this to work
        protected int stencilFail, stencilZFail, stencilPass, stencilFunc, stencilRef, stencilMask;

        protected bool zTrickEven;      

        /// <summary>
        ///    Last min filtering option.
        /// </summary>
        protected FilterOptions minFilter;
        /// <summary>
        ///    Last mip filtering option.
        /// </summary>
        protected FilterOptions mipFilter;

        // render state redundency reduction settings
        protected SceneDetailLevel lastRasterizationMode;
        protected ColorEx lastDiffuse, lastAmbient, lastSpecular, lastEmissive;
        protected float lastShininess;
        protected TexCoordCalcMethod[] lastTexCalMethods = new TexCoordCalcMethod[Config.MaxTextureLayers];
        protected bool fogEnabled;
        protected bool lightingEnabled;
        protected SceneBlendFactor lastBlendSrc, lastBlendDest;
        protected LayerBlendOperationEx[] lastColorOp = new LayerBlendOperationEx[Config.MaxTextureLayers];
        protected LayerBlendOperationEx[] lastAlphaOp = new LayerBlendOperationEx[Config.MaxTextureLayers];
        protected LayerBlendType lastBlendType;
        protected TextureAddressing[] lastAddressingMode = new TextureAddressing[Config.MaxTextureLayers];
        protected int lastDepthBias;
        protected bool lastDepthCheck, lastDepthWrite;
        protected CompareFunction lastDepthFunc;
        
        // temp arrays to reduce runtime allocations
        protected float[] tempMatrix = new float[16];
        protected float[] tempColorVals = new float[4];
        protected float[] tempProgramFloats = new float[4];
        protected int[] colorWrite = new int[4];

        #endregion Fields

        #region Constructors

        /// <summary>
        ///		Default constructor.
        /// </summary>
        public GLRenderSystem() {
            viewMatrix = Matrix4.Identity;
            worldMatrix = Matrix4.Identity;
            textureMatrix = Matrix4.Identity;

            // init the stored stencil buffer params
            stencilFail = stencilZFail = stencilPass = Gl.GL_KEEP;
            stencilFunc = Gl.GL_ALWAYS;
            stencilRef = 0;
            stencilMask = unchecked((int)0xffffffff);

            colorWrite[0] = colorWrite[1] = colorWrite[2] = colorWrite[3] = 1;

            minFilter = FilterOptions.Linear;
            mipFilter = FilterOptions.Point;

            InitConfigOptions();
        }

        #endregion Constructors

        #region Implementation of RenderSystem

        public override RenderWindow CreateRenderWindow(string name, int width, int height, int colorDepth,
            bool isFullscreen, int left, int top, bool depthBuffer, object target) {

            RenderWindow window = new GLWindow();

            window.Handle = target;
            Control targetControl = (Control)target;

            // see if a OpenGLContext has been created yet
            if(renderWindows.Count == 0) {

                // grab the current display settings
                User.EnumDisplaySettings(null, User.ENUM_CURRENT_SETTINGS, out intialScreenSettings);

                if(isFullscreen) {

                    Gdi.DEVMODE screenSettings = new Gdi.DEVMODE();
                    screenSettings.dmSize = (short)Marshal.SizeOf(screenSettings);
                    screenSettings.dmPelsWidth = width;                         // Selected Screen Width
                    screenSettings.dmPelsHeight = height;                       // Selected Screen Height
                    screenSettings.dmBitsPerPel = colorDepth;                         // Selected Bits Per Pixel
                    screenSettings.dmFields = Gdi.DM_BITSPERPEL | Gdi.DM_PELSWIDTH | Gdi.DM_PELSHEIGHT;

                    // Try To Set Selected Mode And Get Results.  NOTE: CDS_FULLSCREEN Gets Rid Of Start Bar.
                    int result = User.ChangeDisplaySettings(ref screenSettings, User.CDS_FULLSCREEN);

                    if(result != User.DISP_CHANGE_SUCCESSFUL) {
                        throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "Unable to change user display settings.");
                    }
                }

                // grab the HWND from the supplied target control
                hWnd = (IntPtr)targetControl.Handle;
   
                Gdi.PIXELFORMATDESCRIPTOR pfd = new Gdi.PIXELFORMATDESCRIPTOR();
                pfd.Size = (short)Marshal.SizeOf(pfd);
                pfd.Version = 1;
                pfd.Flags = Gdi.PFD_DRAW_TO_WINDOW |
                                        Gdi.PFD_SUPPORT_OPENGL |
                                        Gdi.PFD_DOUBLEBUFFER;
                pfd.PixelType = (byte) Gdi.PFD_TYPE_RGBA;
                pfd.ColorBits = (byte) colorDepth;
                pfd.DepthBits = 24;
                // TODO: Find the best setting and use that
                pfd.StencilBits = 8;
                pfd.LayerType = (byte) Gdi.PFD_MAIN_PLANE;

                // get the device context
                hDC = User.GetDC(hWnd);

                if(hDC == IntPtr.Zero) {
                    throw new Exception("Cannot create a GL device context.");
                }

                // attempt to find an appropriate pixel format
                int pixelFormat = Gdi.ChoosePixelFormat(hDC, ref pfd);

                if(pixelFormat == 0) {
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "Unable to find a suitable pixel format.");
                }

                if(!Gdi.SetPixelFormat(hDC, pixelFormat, ref pfd)) {
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "Unable to set the pixel format.");
                }

                // attempt to get the rendering context
                hRC = Wgl.wglCreateContext(hDC);

                if(hRC == IntPtr.Zero) {
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "Unable to create a GL rendering context.");
                }

                if(!Wgl.wglMakeCurrent(hDC, hRC)) {
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "Unable to activate the GL rendering context.");
                }

                // intialize GL extensions and check capabilites
                GLHelper.InitializeExtensions();

                // log hardware info
                System.Diagnostics.Trace.WriteLine(string.Format("Vendor: {0}", GLHelper.Vendor));
                System.Diagnostics.Trace.WriteLine(string.Format("Video Board: {0}", GLHelper.VideoCard));
                System.Diagnostics.Trace.WriteLine(string.Format("Version: {0}", GLHelper.Version));
			
                System.Diagnostics.Trace.WriteLine("Extensions supported:");

                foreach(string ext in GLHelper.Extensions)
                    System.Diagnostics.Trace.WriteLine(ext);

                // init the GL context
                Gl.glShadeModel(Gl.GL_SMOOTH);							// Enable Smooth Shading
                Gl.glClearColor(0.0f, 0.0f, 0.0f, 0.5f);				// Black Background
                Gl.glClearDepth(1.0f);									// Depth Buffer Setup
                Gl.glEnable(Gl.GL_DEPTH_TEST);							// Enables Depth Testing
                Gl.glDepthFunc(Gl.GL_LEQUAL);								// The Type Of Depth Testing To Do
                Gl.glHint(Gl.GL_PERSPECTIVE_CORRECTION_HINT, Gl.GL_NICEST);	// Really Nice Perspective Calculations
            }

            // create the window
            window.Create(name, width, height, colorDepth, isFullscreen, left, top, depthBuffer, hDC, hRC);

            // add the new window to the RenderWindow collection
            this.renderWindows.Add(window);

            // by creating our texture manager, singleton TextureManager will hold our implementation
            textureMgr = new GLTextureManager();

            // create our special program manager
            gpuProgramMgr = new GLGpuProgramManager();

            // query hardware capabilites
            CheckCaps();

            // create a specialized instance, which registers itself as the singleton instance of HardwareBufferManager
            // use software buffers as a fallback, which operate as regular vertex arrays
            if(caps.CheckCap(Capabilities.VertexBuffer))
                hardwareBufferManager = new GLHardwareBufferManager();
            else
                hardwareBufferManager = new GLSoftwareBufferManager();

            // initialize the mesh manager
            MeshManager.Init();

            return window;
        }

        public override ColorEx AmbientLight {
            set {
                // create a float[4]  to contain the RGBA data
                value.ToArrayRGBA(tempColorVals);
                tempColorVals[3] = 0.0f;

                // set the ambient color
                Gl.glLightModelfv(Gl.GL_LIGHT_MODEL_AMBIENT, tempColorVals);
            }
        }

        /// <summary>
        ///		Gets/Sets the global lighting setting.
        /// </summary>
        public override bool LightingEnabled {
            set {
                if(lightingEnabled == value)
                    return;

                if(value)
                    Gl.glEnable(Gl.GL_LIGHTING);
                else
                    Gl.glDisable(Gl.GL_LIGHTING);

                lightingEnabled = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override bool NormalizeNormals {
            set {
                // TODO: Implement NormalizeNormals
            }
        }

        /// <summary>
        ///		Sets the mode to use for rendering
        /// </summary>
        protected override SceneDetailLevel RasterizationMode {
            set {
                if(value == lastRasterizationMode) {
                    return;
                }

                // default to fill to make compiler happy
                int mode = Gl.GL_FILL;

                switch(value) {
                    case SceneDetailLevel.Solid:
                        mode = Gl.GL_FILL;
                        break;
                    case SceneDetailLevel.Points:
                        mode = Gl.GL_POINT;
                        break;
                    case SceneDetailLevel.Wireframe:
                        mode = Gl.GL_LINE;
                        break;
                    default:
                        // if all else fails, just use fill
                        mode = Gl.GL_FILL;
                        break;
                }

                // set the specified polygon mode
                Gl.glPolygonMode(Gl.GL_FRONT_AND_BACK, mode);

                lastRasterizationMode = value;
            }
        }

        public override Shading ShadingMode {
            // OpenGL supports Flat and Smooth shaded primitives
            set {
                switch(value) {
                    case Shading.Flat:
                        Gl.glShadeModel(Gl.GL_FLAT);
                        break;
                    default:
                        Gl.glShadeModel(Gl.GL_SMOOTH);
                        break;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override StencilOperation StencilBufferDepthFailOperation {
            set {
                // Have to use saved values for other params since GL doesn't have 
                // individual setters
                stencilZFail = GLHelper.ConvertEnum(value);
                Gl.glStencilOp(stencilFail, stencilZFail, stencilPass);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override StencilOperation StencilBufferFailOperation {
            set {
                // Have to use saved values for other params since GL doesn't have 
                // individual setters
                stencilFail = GLHelper.ConvertEnum(value);
                Gl.glStencilOp(stencilFail, stencilZFail, stencilPass);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override CompareFunction StencilBufferFunction {
            set {
                // Have to use saved values for other params since GL doesn't have 
                // individual setters
                stencilFunc = GLHelper.ConvertEnum(value);
                Gl.glStencilFunc(stencilFunc, stencilRef, stencilMask);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override int StencilBufferMask {
            set {
                // Have to use saved values for other params since GL doesn't have 
                // individual setters
                stencilMask = value;
                Gl.glStencilFunc(stencilFunc, stencilRef, stencilMask);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override StencilOperation StencilBufferPassOperation {
            set {
                // Have to use saved values for other params since GL doesn't have 
                // individual setters
                stencilPass = GLHelper.ConvertEnum(value);
                Gl.glStencilOp(stencilFail, stencilZFail, stencilPass);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override int StencilBufferReferenceValue {
            set {
                // Have to use saved values for other params since GL doesn't have 
                // individual setters
                stencilRef = value;
                Gl.glStencilFunc(stencilFunc, stencilRef, stencilMask);
            }
        }

        /// <summary>
        ///		Specifies whether stencil check should be enabled or not.
        /// </summary>
        public override bool StencilCheckEnabled {
            set {
                if(value)
                    Gl.glEnable(Gl.GL_STENCIL_TEST);
                else
                    Gl.glDisable(Gl.GL_STENCIL_TEST);
            }
        }

        /// <summary>
        ///		Creates a projection matrix specific to OpenGL based on the given params.
        ///		Note: forGpuProgram is ignored because GL uses the same handed projection matrix
        ///		normally and for GPU programs.
        /// </summary>
        /// <param name="fov"></param>
        /// <param name="aspectRatio"></param>
        /// <param name="near"></param>
        /// <param name="far"></param>
        /// <returns></returns>
        public override Axiom.MathLib.Matrix4 MakeProjectionMatrix(float fov, float aspectRatio, float near, float far, bool forGpuProgram) {
            Matrix4 matrix = new Matrix4();

            float thetaY = MathUtil.DegreesToRadians(fov * 0.5f);
            float tanThetaY = MathUtil.Tan(thetaY);

            float w = (1.0f / tanThetaY) / aspectRatio;
            float h = 1.0f / tanThetaY;
            float q = -(far + near) / (far - near);
            float qn = -2 * (far * near) / (far - near);

            matrix.m00 = w;
            matrix.m11 = h;
            matrix.m22 = q;
            matrix.m23 = qn;
            matrix.m32 = -1.0f;

            return matrix;
        }

        /// <summary>
        ///		Executes right before each frame is rendered.
        /// </summary>
        protected override void BeginFrame() {
            Debug.Assert(activeViewport != null, "BeingFrame cannot run without an active viewport.");

            if(activeViewport.ClearEveryFrame) {
                activeViewport.BackgroundColor.ToArrayRGBA(tempColorVals);

                // clear the viewport
                Gl.glClearColor(tempColorVals[0], tempColorVals[1], tempColorVals[2], tempColorVals[3]);

                // disable depth write if it isnt
                if(!depthWrite)
                    Gl.glDepthMask(Gl.GL_TRUE);

                bool colorMask = colorWrite[0] == 0 || colorWrite[1] == 0 || colorWrite[2] == 0 || colorWrite[3] == 0;

                if(colorMask) {
                    Gl.glColorMask(1, 1, 1, 1);
                }

                // clear the color buffer and depth buffer bits
                Gl.glClear(Gl.GL_COLOR_BUFFER_BIT | Gl.GL_DEPTH_BUFFER_BIT);	

                // Reset depth write state if appropriate
                // Enable depth buffer for writing if it isn't
                if(!depthWrite)
                    Gl.glDepthMask(Gl.GL_FALSE);

                if(colorMask) {
                    Gl.glColorMask(1, 1, 1, 1);
                }
            }
            else {
                // Use Carmack's ztrick to avoid clearing the depth buffer every frame
                if(zTrickEven) {
                    this.DepthFunction = CompareFunction.LessEqual;
                    Gl.glDepthRange(0, 0.499999999);
                }
                else {
                    this.DepthFunction = CompareFunction.GreaterEqual;
                    Gl.glDepthRange(1, 0.5);
                }

                // swap the z trick flag
                zTrickEven = !zTrickEven;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void EndFrame() {
            // clear stored blend modes, to ensure they gets set properly in multi texturing scenarios
            // overall this will still reduce the number of blend mode changes
            for(int i = 1; i < Config.MaxTextureLayers; i++) {
                lastAlphaOp[i] = 0;
                lastColorOp[i] = 0;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="viewport"></param>
        protected override void SetViewport(Viewport viewport) {
            // TODO: Make sure to remember what happens to alter the viewport drawing behavior
            if(activeViewport != viewport || viewport.IsUpdated) {
                // store this viewport and it's target
                activeViewport = viewport;
                activeRenderTarget = viewport.Target;

                int x, y, width, height;

                // set viewport dimensions
                width = viewport.ActualWidth;
                height = viewport.ActualHeight;
                x = viewport.ActualLeft;
                // make up for the fact that GL's origin starts at the bottom left corner
                y = activeRenderTarget.Height - viewport.ActualTop - height;

                // enable scissor testing (for viewports)
                Gl.glEnable(Gl.GL_SCISSOR_TEST);

                // set the current GL viewport
                Gl.glViewport(x, y, width, height);

                // set the scissor area for the viewport
                Gl.glScissor(x, y, width, height);

                // clear the updated flag
                viewport.IsUpdated = false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ambient"></param>
        /// <param name="diffuse"></param>
        /// <param name="specular"></param>
        /// <param name="emissive"></param>
        /// <param name="shininess"></param>
        protected override void SetSurfaceParams(ColorEx ambient, ColorEx diffuse, ColorEx specular, ColorEx emissive, float shininess) {
            float[] vals = tempColorVals;
            
            // ambient
            //if(lastAmbient == null || lastAmbient.CompareTo(ambient) != 0) {
                ambient.ToArrayRGBA(vals);
                Gl.glMaterialfv(Gl.GL_FRONT_AND_BACK, Gl.GL_AMBIENT, vals);
                
                lastAmbient = ambient;
            //}

            // diffuse
            //if(lastDiffuse == null || lastDiffuse.CompareTo(diffuse) != 0) {
                vals[0] = diffuse.r; vals[1] = diffuse.g; vals[2] = diffuse.b; vals[3] = diffuse.a;
                Gl.glMaterialfv(Gl.GL_FRONT_AND_BACK, Gl.GL_DIFFUSE, vals);

                lastDiffuse = diffuse;
            //}

            // specular
            //if(lastSpecular == null || lastSpecular.CompareTo(specular) != 0) {
                vals[0] = specular.r; vals[1] = specular.g; vals[2] = specular.b; vals[3] = specular.a;
                Gl.glMaterialfv(Gl.GL_FRONT_AND_BACK, Gl.GL_SPECULAR, vals);

                lastSpecular = specular;
            //}

            // emissive
            //if(lastEmissive == null || lastEmissive.CompareTo(emissive) != 0) {
                vals[0] = emissive.r; vals[1] = emissive.g; vals[2] = emissive.b; vals[3] = emissive.a;
                Gl.glMaterialfv(Gl.GL_FRONT_AND_BACK, Gl.GL_EMISSION, vals);

                lastEmissive = emissive;
            //}

            // shininess
            //if(lastShininess != shininess) {
                Gl.glMaterialf(Gl.GL_FRONT_AND_BACK, Gl.GL_SHININESS, shininess);

                lastShininess = shininess;
            //}
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stage"></param>
        /// <param name="texAddressingMode"></param>
        protected override void SetTextureAddressingMode(int stage, TextureAddressing texAddressingMode) {
            if(lastAddressingMode[stage] == texAddressingMode) {
                //return;
            }

            lastAddressingMode[stage] = texAddressingMode;

            int type = 0;

            // find out the GL equivalent of out TextureAddressing enum
            switch(texAddressingMode) {
                case TextureAddressing.Wrap:
                    type = Gl.GL_REPEAT;
                    break;

                case TextureAddressing.Mirror:
                    type = Gl.GL_MIRRORED_REPEAT;
                    break;

                case TextureAddressing.Clamp:
                    type = Gl.GL_CLAMP_TO_EDGE;
                    break;
            } // end switch

            // set the GL texture wrap params for the specified unit
            Ext.glActiveTextureARB(Gl.GL_TEXTURE0 + stage);
            Gl.glTexParameteri(textureTypes[stage], Gl.GL_TEXTURE_WRAP_S, type);
            Gl.glTexParameteri(textureTypes[stage], Gl.GL_TEXTURE_WRAP_T, type);
            Gl.glTexParameteri(textureTypes[stage], Gl.GL_TEXTURE_WRAP_R, type);
            Ext.glActiveTextureARB(Gl.GL_TEXTURE0);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stage"></param>
        /// <param name="maxAnisotropy"></param>
        protected override void SetTextureLayerAnisotropy(int stage, int maxAnisotropy) {
            if(!caps.CheckCap(Capabilities.AnisotropicFiltering)) {
                return;
            }

            // get current setting to compare
            float currentAnisotropy = 0;
            float maxSupportedAnisotropy = 0;
            Gl.glGetFloatv(Gl.GL_TEXTURE_MAX_ANISOTROPY_EXT, out currentAnisotropy);
            Gl.glGetFloatv(Gl.GL_MAX_TEXTURE_MAX_ANISOTROPY_EXT, out maxSupportedAnisotropy);

            if(maxAnisotropy > maxSupportedAnisotropy) {
                maxAnisotropy = 
                    (int)maxSupportedAnisotropy > 0 ? (int)maxSupportedAnisotropy : 1;
            }

            if(currentAnisotropy != maxAnisotropy) {
                Gl.glTexParameterf(textureTypes[stage], Gl.GL_TEXTURE_MAX_ANISOTROPY_EXT, (float)maxAnisotropy);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stage"></param>
        /// <param name="blendMode"></param>
        public override void SetTextureBlendMode(int stage, LayerBlendModeEx blendMode) {
            if(!caps.CheckCap(Capabilities.TextureBlending)) {
                return;
            }

            LayerBlendOperationEx lastOp;

            if(blendMode.blendType == LayerBlendType.Alpha) {
                lastOp = lastAlphaOp[stage];
            }
            else {
                lastOp = lastColorOp[stage];
            }
            
            // ignore the new blend mode only if the last one for the current texture stage
            // is the same, and if no special texture coord calcs are required
            if( lastOp == blendMode.operation && 
                lastTexCalMethods[stage] == TexCoordCalcMethod.None)  {
               return;
            }

            // remember last setting
            if(blendMode.blendType == LayerBlendType.Alpha) {
                lastAlphaOp[stage] = blendMode.operation;
            }
            else {
                lastColorOp[stage] = blendMode.operation;
            }

            int src1op, src2op, cmd;

            src1op = src2op = cmd = 0;

            switch(blendMode.source1) {
                case LayerBlendSource.Current:
                    src1op = Gl.GL_PREVIOUS;
                    break;

                case LayerBlendSource.Texture:
                    src1op = Gl.GL_TEXTURE;
                    break;
			
                case LayerBlendSource.Manual:
                    src1op = Gl.GL_CONSTANT;
                    break;
			
                    // no diffuse or specular equivalent right now
                default:
                    src1op = 0;
                    break;
            }

            switch(blendMode.source2) {
                case LayerBlendSource.Current:
                    src2op = Gl.GL_PREVIOUS;
                    break;

                case LayerBlendSource.Texture:
                    src2op = Gl.GL_TEXTURE;
                    break;
			
                case LayerBlendSource.Manual:
                    src2op = Gl.GL_CONSTANT;
                    break;
			
                    // no diffuse or specular equivalent right now
                default:
                    src2op = 0;
                    break;
            }

            switch (blendMode.operation) {
                case LayerBlendOperationEx.Source1:
                    cmd = Gl.GL_REPLACE;
                    break;
                case LayerBlendOperationEx.Source2:
                    cmd = Gl.GL_REPLACE;
                    break;
                case LayerBlendOperationEx.Modulate:
                    cmd = Gl.GL_MODULATE;
                    break;
                case LayerBlendOperationEx.ModulateX2:
                    cmd = Gl.GL_MODULATE;
                    break;
                case LayerBlendOperationEx.ModulateX4:
                    cmd = Gl.GL_MODULATE;
                    break;
                case LayerBlendOperationEx.Add:
                    cmd = Gl.GL_ADD;
                    break;
                case LayerBlendOperationEx.AddSigned:
                    cmd = Gl.GL_ADD_SIGNED;
                    break;
                case LayerBlendOperationEx.BlendTextureAlpha:
                    cmd = Gl.GL_INTERPOLATE;
                    break;
                case LayerBlendOperationEx.BlendCurrentAlpha:
                    cmd = Gl.GL_INTERPOLATE;
                    break;
                case LayerBlendOperationEx.DotProduct:
                    // Check for Dot3 support
                    cmd = caps.CheckCap(Capabilities.Dot3Bump) ? Gl.GL_DOT3_RGB : Gl.GL_MODULATE;
                    break;

                default:
                    cmd = 0;
                    break;
            } // end switch

            Ext.glActiveTextureARB(Gl.GL_TEXTURE0 + stage);
            Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_TEXTURE_ENV_MODE, Gl.GL_COMBINE);

            if (blendMode.blendType == LayerBlendType.Color) {
                Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_COMBINE_RGB, cmd);
                Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_SOURCE0_RGB, src1op);
                Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_SOURCE1_RGB, src2op);
                Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_SOURCE2_RGB, Gl.GL_CONSTANT);
            }
            else {
                Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_COMBINE_ALPHA, cmd);
                Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_SOURCE0_ALPHA, src1op);
                Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_SOURCE1_ALPHA, src2op);
                Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_SOURCE2_ALPHA, Gl.GL_CONSTANT);
            }

            // handle blend types first
            switch (blendMode.operation) {
                case LayerBlendOperationEx.BlendDiffuseAlpha:
                    Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_SOURCE2_RGB, Gl.GL_PRIMARY_COLOR);
                    Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_SOURCE2_ALPHA, Gl.GL_PRIMARY_COLOR);
                    break;
                case LayerBlendOperationEx.BlendTextureAlpha:
                    Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_SOURCE2_RGB, Gl.GL_TEXTURE);
                    Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_SOURCE2_ALPHA, Gl.GL_TEXTURE);
                    break;
                case LayerBlendOperationEx.BlendCurrentAlpha:
                    Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_SOURCE2_RGB, Gl.GL_PREVIOUS);
                    Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_SOURCE2_ALPHA, Gl.GL_PREVIOUS);
                    break;
                default:
                    break;
            }

            // set alpha scale to 1 by default unless specifically requested to be higher
            // otherwise, textures that get switch from ModulateX2 or ModulateX4 down to Source1
            // for example, the alpha scale would still be high and overbrighten the texture
            switch (blendMode.operation) {
                case LayerBlendOperationEx.ModulateX2:
                    Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, blendMode.blendType == LayerBlendType.Color ? 
                        Gl.GL_RGB_SCALE : Gl.GL_ALPHA_SCALE, 2);
                    break;
                case LayerBlendOperationEx.ModulateX4:
                    Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, blendMode.blendType == LayerBlendType.Color ? 
                        Gl.GL_RGB_SCALE : Gl.GL_ALPHA_SCALE, 4);
                    break;
                default:
                    Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, blendMode.blendType == LayerBlendType.Color ? 
                        Gl.GL_RGB_SCALE : Gl.GL_ALPHA_SCALE, 1);
                    break;
            }

            Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_OPERAND0_RGB, Gl.GL_SRC_COLOR);
            Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_OPERAND1_RGB, Gl.GL_SRC_COLOR);
            Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_OPERAND2_RGB, Gl.GL_SRC_COLOR);
            Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_OPERAND0_ALPHA, Gl.GL_SRC_ALPHA);
            Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_OPERAND1_ALPHA, Gl.GL_SRC_ALPHA);
            Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_OPERAND2_ALPHA, Gl.GL_SRC_ALPHA);

            // check source2 and set colors values appropriately
            if (blendMode.source1 == LayerBlendSource.Manual) {
                if(blendMode.blendType == LayerBlendType.Color) {
                    // color value 1
                    blendMode.colorArg1.ToArrayRGBA(tempColorVals);
                }
                else {
                    // alpha value 1
                    tempColorVals[0] = 0.0f; tempColorVals[1] = 0.0f; tempColorVals[2] = 0.0f; tempColorVals[3] = blendMode.alphaArg1;
                }

                Gl.glTexEnvfv(Gl.GL_TEXTURE_ENV, Gl.GL_TEXTURE_ENV_COLOR, tempColorVals);
            }

            // check source2 and set colors values appropriately
            if (blendMode.source2 == LayerBlendSource.Manual) {
                if(blendMode.blendType == LayerBlendType.Color) {
                    // color value 2
                    blendMode.colorArg2.ToArrayRGBA(tempColorVals);
                }
                else {
                    // alpha value 2
                    tempColorVals[0] = 0.0f; tempColorVals[1] = 0.0f; tempColorVals[2] = 0.0f; tempColorVals[3] = blendMode.alphaArg2;
                }
                Gl.glTexEnvfv(Gl.GL_TEXTURE_ENV, Gl.GL_TEXTURE_ENV_COLOR, tempColorVals);
            }
        
            Ext.glActiveTextureARB(Gl.GL_TEXTURE0);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="type"></param>
        /// <param name="filter"></param>
        protected override void SetTextureUnitFiltering(int unit, FilterType type, FilterOptions filter) {
            // set the current texture unit
            Ext.glActiveTextureARB(Gl.GL_TEXTURE0 + unit);

            switch(type) {
                case FilterType.Min:
                    minFilter = filter;

                    // combine with exiting mip filter
                    Gl.glTexParameteri(
                        textureTypes[unit], 
                        Gl.GL_TEXTURE_MIN_FILTER, 
                        GetCombinedMinMipFilter());
                    break;

                case FilterType.Mag:
                    switch(filter) {
                        case FilterOptions.Anisotropic:
                        case FilterOptions.Linear:
                            Gl.glTexParameteri(
                                textureTypes[unit], 
                                Gl.GL_TEXTURE_MAG_FILTER, 
                                Gl.GL_LINEAR);
                            break;
                        case FilterOptions.Point:
                        case FilterOptions.None:
                            Gl.glTexParameteri(
                                textureTypes[unit], 
                                Gl.GL_TEXTURE_MAG_FILTER, 
                                Gl.GL_NEAREST);
                            break;
                    }
                    break;

                case FilterType.Mip:
                    mipFilter = filter;

                    // combine with exiting mip filter
                    Gl.glTexParameteri(
                        textureTypes[unit], 
                        Gl.GL_TEXTURE_MIN_FILTER, 
                        GetCombinedMinMipFilter());
                    break;
            }

            // reset to the first texture unit
            Ext.glActiveTextureARB(Gl.GL_TEXTURE0);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stage"></param>
        /// <param name="index"></param>
        protected override void SetTextureCoordSet(int stage, int index) {
            texCoordIndex[stage] = (ushort)index;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stage"></param>
        /// <param name="method"></param>
        protected override void SetTextureCoordCalculation(int stage, TexCoordCalcMethod method) {
            // Default to no extra auto texture matrix
            useAutoTextureMatrix = false;

            if(method == TexCoordCalcMethod.None && 
                lastTexCalMethods[stage] == method) {
                return;
            }

            // store for next checking next time around
            lastTexCalMethods[stage] = method;

            Ext.glActiveTextureARB(Gl.GL_TEXTURE0 + stage );

            switch(method) {
                case TexCoordCalcMethod.None:
                        Gl.glDisable( Gl.GL_TEXTURE_GEN_S );
                        Gl.glDisable( Gl.GL_TEXTURE_GEN_T );
                        Gl.glDisable( Gl.GL_TEXTURE_GEN_R );
                        Gl.glDisable( Gl.GL_TEXTURE_GEN_Q );
                    break;

                case TexCoordCalcMethod.EnvironmentMap:
                    Gl.glTexGeni( Gl.GL_S, Gl.GL_TEXTURE_GEN_MODE, Gl.GL_SPHERE_MAP );
                    Gl.glTexGeni( Gl.GL_T, Gl.GL_TEXTURE_GEN_MODE, Gl.GL_SPHERE_MAP );

                    Gl.glEnable( Gl.GL_TEXTURE_GEN_S );
                    Gl.glEnable( Gl.GL_TEXTURE_GEN_T );
                    Gl.glDisable( Gl.GL_TEXTURE_GEN_R );
                    Gl.glDisable( Gl.GL_TEXTURE_GEN_Q );
                    break;

                case TexCoordCalcMethod.EnvironmentMapPlanar:            
                    // XXX This doesn't seem right?!
                    if(GLHelper.CheckMinVersion("1.3")) {
                        Gl.glTexGeni( Gl.GL_S, Gl.GL_TEXTURE_GEN_MODE, Gl.GL_REFLECTION_MAP );
                        Gl.glTexGeni( Gl.GL_T, Gl.GL_TEXTURE_GEN_MODE, Gl.GL_REFLECTION_MAP );
                        Gl.glTexGeni( Gl.GL_R, Gl.GL_TEXTURE_GEN_MODE, Gl.GL_REFLECTION_MAP );

                        Gl.glEnable( Gl.GL_TEXTURE_GEN_S );
                        Gl.glEnable( Gl.GL_TEXTURE_GEN_T );
                        Gl.glEnable( Gl.GL_TEXTURE_GEN_R );
                        Gl.glDisable( Gl.GL_TEXTURE_GEN_Q );
                    }
                    else {
                        Gl.glTexGeni( Gl.GL_S, Gl.GL_TEXTURE_GEN_MODE, Gl.GL_SPHERE_MAP );
                        Gl.glTexGeni( Gl.GL_T, Gl.GL_TEXTURE_GEN_MODE, Gl.GL_SPHERE_MAP );

                        Gl.glEnable( Gl.GL_TEXTURE_GEN_S );
                        Gl.glEnable( Gl.GL_TEXTURE_GEN_T );
                        Gl.glDisable( Gl.GL_TEXTURE_GEN_R );
                        Gl.glDisable( Gl.GL_TEXTURE_GEN_Q );
                    }
                    break;

                case TexCoordCalcMethod.EnvironmentMapReflection:
        
                    Gl.glTexGeni( Gl.GL_S, Gl.GL_TEXTURE_GEN_MODE, Gl.GL_REFLECTION_MAP );
                    Gl.glTexGeni( Gl.GL_T, Gl.GL_TEXTURE_GEN_MODE, Gl.GL_REFLECTION_MAP );
                    Gl.glTexGeni( Gl.GL_R, Gl.GL_TEXTURE_GEN_MODE, Gl.GL_REFLECTION_MAP );

                    Gl.glEnable( Gl.GL_TEXTURE_GEN_S );
                    Gl.glEnable( Gl.GL_TEXTURE_GEN_T );
                    Gl.glEnable( Gl.GL_TEXTURE_GEN_R );
                    Gl.glDisable( Gl.GL_TEXTURE_GEN_Q );

                    // We need an extra texture matrix here
                    // This sets the texture matrix to be the inverse of the modelview matrix
                    useAutoTextureMatrix = true;

                    Gl.glGetFloatv(Gl.GL_MODELVIEW_MATRIX, tempMatrix);

                    // Transpose 3x3 in order to invert matrix (rotation)
                    // Note that we need to invert the Z _before_ the rotation
                    // No idea why we have to invert the Z at all, but reflection is wrong without it
                    autoTextureMatrix[0] = tempMatrix[0]; autoTextureMatrix[1] = tempMatrix[4]; autoTextureMatrix[2] = -tempMatrix[8];
                    autoTextureMatrix[4] = tempMatrix[1]; autoTextureMatrix[5] = tempMatrix[5]; autoTextureMatrix[6] = -tempMatrix[9];
                    autoTextureMatrix[8] = tempMatrix[2]; autoTextureMatrix[9] = tempMatrix[6]; autoTextureMatrix[10] = -tempMatrix[10];
                    autoTextureMatrix[3] = autoTextureMatrix[7] = autoTextureMatrix[11] = 0.0f;
                    autoTextureMatrix[12] = autoTextureMatrix[13] = autoTextureMatrix[14] = 0.0f;
                    autoTextureMatrix[15] = 1.0f;

                    break;

                case TexCoordCalcMethod.EnvironmentMapNormal:
                    Gl.glTexGeni( Gl.GL_S, Gl.GL_TEXTURE_GEN_MODE, Gl.GL_NORMAL_MAP );
                    Gl.glTexGeni( Gl.GL_T, Gl.GL_TEXTURE_GEN_MODE, Gl.GL_NORMAL_MAP );
                    Gl.glTexGeni( Gl.GL_R, Gl.GL_TEXTURE_GEN_MODE, Gl.GL_NORMAL_MAP );

                    Gl.glEnable( Gl.GL_TEXTURE_GEN_S );
                    Gl.glEnable( Gl.GL_TEXTURE_GEN_T );
                    Gl.glEnable( Gl.GL_TEXTURE_GEN_R );
                    Gl.glDisable( Gl.GL_TEXTURE_GEN_Q );
                    break;

                default:
                    break;
            }

            Ext.glActiveTextureARB(Gl.GL_TEXTURE0);		
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stage"></param>
        /// <param name="xform"></param>
        protected override void SetTextureMatrix(int stage, Matrix4 xform) {
            
            MakeGLMatrix(ref xform, tempMatrix);

            tempMatrix[12] = tempMatrix[8];
            tempMatrix[13] = tempMatrix[9];

            //float[] m = autoTextureMatrix;
            //Console.WriteLine("{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11} {12} {13} {14} {15}", m[0], m[1], m[2], m[3], m[4], m[5], m[6], m[7], m[8], m[9], m[10], m[11], m[12], m[13], m[14], m[15]);

            Ext.glActiveTextureARB(Gl.GL_TEXTURE0 + stage);
            Gl.glMatrixMode(Gl.GL_TEXTURE);

            // if texture matrix was precalced, use that
            if(useAutoTextureMatrix) {
                Gl.glLoadMatrixf(autoTextureMatrix);
                Gl.glMultMatrixf(tempMatrix);
            }
            else
                Gl.glLoadMatrixf(tempMatrix);

            // reset to mesh view matrix and to tex unit 0
            Gl.glMatrixMode(Gl.GL_MODELVIEW);
            Ext.glActiveTextureARB(Gl.GL_TEXTURE0);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="autoCreateWindow"></param>
        /// <returns></returns>
        public override RenderWindow Initialize(bool autoCreateWindow) {

            RenderWindow renderWindow = null;

            if(autoCreateWindow) {
                EngineConfig.DisplayModeRow[] modes = 
                    (EngineConfig.DisplayModeRow[])engineConfig.DisplayMode.Select("Selected = true");

                EngineConfig.DisplayModeRow mode = modes[0];

                DefaultForm newWindow = RenderWindow.CreateDefaultForm(0, 0, mode.Width, mode.Height, mode.FullScreen);

                // create a new render window
                renderWindow = this.CreateRenderWindow("Main Window", mode.Width, mode.Height, mode.Bpp, mode.FullScreen, 0, 0, true, newWindow.Target);

                // set the default form's renderwindow so it can access it internally
                newWindow.RenderWindow = renderWindow;

                // show the window
                newWindow.Show();
            }

            this.CullingMode = this.cullingMode;

            return renderWindow;
        }

        /// <summary>
        ///		Shutdown the render system.
        /// </summary>
        public override void Shutdown() {
            // call base Shutdown implementation
            base.Shutdown();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stage"></param>
        /// <param name="enabled"></param>
        /// <param name="textureName"></param>
        protected override void SetTexture(int stage, bool enabled, string textureName) {
            // load the texture
            GLTexture texture = (GLTexture)TextureManager.Instance.GetByName(textureName);

            int lastTextureType = textureTypes[stage];

            // set the active texture
            Ext.glActiveTextureARB(Gl.GL_TEXTURE0 + stage);

            // enable and bind the texture if necessary
            if(enabled && texture != null) {
                textureTypes[stage] = texture.GLTextureType;

                if(lastTextureType != textureTypes[stage] && lastTextureType != 0) {
                    Gl.glDisable(lastTextureType);
                }

                Gl.glEnable(textureTypes[stage]);
                Gl.glBindTexture(textureTypes[stage], texture.TextureID);
            }
            else {
                Gl.glDisable(textureTypes[stage]);
                Gl.glTexEnvf(Gl.GL_TEXTURE_ENV, Gl.GL_TEXTURE_ENV_MODE, Gl.GL_MODULATE);
            }

            // reset active texture to unit 0
            Ext.glActiveTextureARB(Gl.GL_TEXTURE0);
        }

        protected override void SetAlphaRejectSettings(int stage, CompareFunction func, byte val) {
            // TODO: Implement SetAlphaRejectSettings
        }

        protected override void SetColorBufferWriteEnabled(bool red, bool green, bool blue, bool alpha) {
            // record this for later
            colorWrite[0] = red ? 1 : 0;
            colorWrite[1] = green ? 1 : 0;
            colorWrite[2] = blue ? 1 : 0;
            colorWrite[3] = alpha ? 1 : 0;

            Gl.glColorMask(colorWrite[0], colorWrite[1], colorWrite[2], colorWrite[3]);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="color"></param>
        /// <param name="density"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        protected override void SetFog(FogMode mode, ColorEx color, float density, float start, float end) {
            int fogMode;

            switch(mode) {
                case FogMode.Exp:
                    fogMode = Gl.GL_EXP;
                    break;
                case FogMode.Exp2:
                    fogMode = Gl.GL_EXP2;
                    break;
                case FogMode.Linear:
                    fogMode = Gl.GL_LINEAR;
                    break;
                default:
                    if(fogEnabled) {
                        Gl.glDisable(Gl.GL_FOG);
                        fogEnabled = false;
                    }
                    return;
            } // switch

            Gl.glEnable(Gl.GL_FOG);
            Gl.glFogi(Gl.GL_FOG_MODE, fogMode);
            // fog color values
            color.ToArrayRGBA(tempColorVals);
            Gl.glFogfv(Gl.GL_FOG_COLOR, tempColorVals);
            Gl.glFogf(Gl.GL_FOG_DENSITY, density);
            Gl.glFogf(Gl.GL_FOG_START, start);
            Gl.glFogf(Gl.GL_FOG_END, end);

            // TODO: Fog hints maybe?
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="op"></param>
        public override void Render(RenderOperation op) {

			// don't even bother if there are no vertices to render, causes problems on some cards (FireGL 8800)
			if(op.vertexData.vertexCount == 0) {
				return;
			}

            // call base class method first
            base.Render (op);

            // get a list of the vertex elements for this render operation
            IList elements = op.vertexData.vertexDeclaration.Elements;

            // will be used to alia either the buffer offset (VBO's) or array data if VBO's are
            // not available
            IntPtr bufferData = IntPtr.Zero;
	
            // loop through and handle each element
            for(int i = 0; i < elements.Count; i++) {
                // get a reference to the current object in the collection
                VertexElement element = (VertexElement)elements[i];

                // get the current vertex buffer
                HardwareVertexBuffer vertexBuffer = 	op.vertexData.vertexBufferBinding.GetBuffer(element.Source);

                if(caps.CheckCap(Capabilities.VertexBuffer)) {
                    // get the buffer id
                    int bufferId = ((GLHardwareVertexBuffer)vertexBuffer).GLBufferID;

                    // bind the current vertex buffer
                    Ext.glBindBufferARB(Gl.GL_ARRAY_BUFFER_ARB, bufferId);
                    bufferData = BUFFER_OFFSET(element.Offset);
                }
                else {
                    // get a direct pointer to the software buffer data for using standard vertex arrays
                    bufferData = ((SoftwareVertexBuffer)vertexBuffer).GetDataPointer(element.Offset);
                }

                // get the type of this buffer
                int type = GLHelper.ConvertEnum(element.Type);

                // set pointer usage based on the use of this buffer
                switch(element.Semantic) {
                    case VertexElementSemantic.Position:
                        // set the pointer data
                        Gl.glVertexPointer(
                            VertexElement.GetTypeCount(element.Type),
                            type,
                            vertexBuffer.VertexSize,
                            bufferData);

                        // enable the vertex array client state
                        Gl.glEnableClientState(Gl.GL_VERTEX_ARRAY);

                        break;
				
                    case VertexElementSemantic.Normal:
                        // set the pointer data
                        Gl.glNormalPointer(
                            type, 
                            vertexBuffer.VertexSize,
                            bufferData);

                        // enable the normal array client state
                        Gl.glEnableClientState(Gl.GL_NORMAL_ARRAY);

                        break;
				
                    case VertexElementSemantic.Diffuse:
                        // set the pointer data
                        Gl.glColorPointer(
                            4,
                            type, 
                            vertexBuffer.VertexSize,
                            bufferData);

                        // enable the normal array client state
                        Gl.glEnableClientState(Gl.GL_COLOR_ARRAY);

                        break;
				
                    case VertexElementSemantic.Specular:
                        // TODO: Add glSecondaryColorPointer to Tao
                        break;

                    case VertexElementSemantic.TexCoords:
                        // this ignores vertex element index and sets tex array for each available texture unit
                        // this allows for multitexturing on entities whose mesh only has a single set of tex coords

                        for(int j = 0; j < caps.NumTextureUnits; j++) {
                            // only set if this textures index if it is supposed to
                            if(texCoordIndex[j] == element.Index) {
                                // set the current active texture unit
                                Ext.glClientActiveTextureARB(Gl.GL_TEXTURE0 + j); 

                                // set the tex coord pointer
                                Gl.glTexCoordPointer(
                                    VertexElement.GetTypeCount(element.Type),
                                    type,
                                    vertexBuffer.VertexSize,
                                    bufferData);

                                // enable texture coord state
                                Gl.glEnableClientState(Gl.GL_TEXTURE_COORD_ARRAY);
                            }
                        }
                        break;

                    default:
                        break;
                } // switch
            } // for

            // reset to texture unit 0
            Ext.glClientActiveTextureARB(Gl.GL_TEXTURE0); 

            int primType = 0;

            // which type of render operation is this?
            switch(op.operationType) {
                case RenderMode.PointList:
                    primType = Gl.GL_POINTS;
                    break;
                case RenderMode.LineList:
                    primType = Gl.GL_LINES;
                    break;
                case RenderMode.LineStrip:
                    primType = Gl.GL_LINE_STRIP;
                    break;
                case RenderMode.TriangleList:
                    primType = Gl.GL_TRIANGLES;
                    break;
                case RenderMode.TriangleStrip:
                    primType = Gl.GL_TRIANGLE_STRIP;
                    break;
                case RenderMode.TriangleFan:
                    primType = Gl.GL_TRIANGLE_FAN;
                    break;
            }

            if(op.useIndices) {
                // setup a pointer to the index data
                IntPtr indexPtr = IntPtr.Zero;

                // if hardware is supported, expect it is a hardware buffer.  else, fallback to software
                if(caps.CheckCap(Capabilities.VertexBuffer)) {
                    // get the index buffer id
                    int idxBufferID = ((GLHardwareIndexBuffer)op.indexData.indexBuffer).GLBufferID;

                    // bind the current index buffer
                    Ext.glBindBufferARB(Gl.GL_ELEMENT_ARRAY_BUFFER_ARB, idxBufferID);

                    // get the offset pointer to the data in the vbo
                    indexPtr = BUFFER_OFFSET(op.indexData.indexStart);
                }
                else {
                    // get the index data as a direct pointer to the software buffer data
                    indexPtr = ((SoftwareIndexBuffer)op.indexData.indexBuffer).GetDataPointer(op.indexData.indexStart);
                }

                // find what type of index buffer elements we are using
                int indexType = (op.indexData.indexBuffer.Type == IndexType.Size16) 
                    ? Gl.GL_UNSIGNED_SHORT : Gl.GL_UNSIGNED_INT;

                // draw the indexed vertex data
                Gl.glDrawElements(primType, op.indexData.indexCount, indexType, indexPtr);
                
                // TODO: Use glDrawRangeElements to allow indexStart, indexCount to be used
            }
            else {
                Gl.glDrawArrays(primType, 0, op.vertexData.vertexCount);
            }

            // disable all client states
            Gl.glDisableClientState( Gl.GL_VERTEX_ARRAY );
            Gl.glDisableClientState( Gl.GL_TEXTURE_COORD_ARRAY );
            Gl.glDisableClientState( Gl.GL_NORMAL_ARRAY );
            Gl.glDisableClientState( Gl.GL_COLOR_ARRAY );
            //Gl.glDisableClientState( Gl.GL_SECONDARY_COLOR_ARRAY );
            Gl.glColor4f(1.0f,1.0f,1.0f,1.0f);
        }

        /// <summary>
        ///		
        /// </summary>
        protected override Matrix4 ProjectionMatrix {
            set {
                // create a float[16] from our Matrix4
                MakeGLMatrix(ref value, tempMatrix);
			
                // set the matrix mode to Projection
                Gl.glMatrixMode(Gl.GL_PROJECTION);

                // load the float array into the projection matrix
                Gl.glLoadMatrixf(tempMatrix);

                // set the matrix mode back to ModelView
                Gl.glMatrixMode(Gl.GL_MODELVIEW);
            }
        }

        /// <summary>
        ///		
        /// </summary>
        protected override Matrix4 ViewMatrix {
            set {
                viewMatrix = value;

                // create a float[16] from our Matrix4
                MakeGLMatrix(ref viewMatrix, tempMatrix);
			
                // set the matrix mode to ModelView
                Gl.glMatrixMode(Gl.GL_MODELVIEW);
			
                // load the float array into the ModelView matrix
                Gl.glLoadMatrixf(tempMatrix);

                // convert the internal world matrix
                MakeGLMatrix(ref worldMatrix, tempMatrix);

                // multply the world matrix by the current ModelView matrix
                Gl.glMultMatrixf(tempMatrix);
            }
        }

        /// <summary>
        /// </summary>
        protected override Matrix4 WorldMatrix {
            set {
                //store the new world matrix locally
                worldMatrix = value;

                // multiply the view and world matrices, and convert it to GL format
                Matrix4 multMatrix = viewMatrix * worldMatrix;
                MakeGLMatrix(ref multMatrix, tempMatrix);

                // change the matrix mode to ModelView
                Gl.glMatrixMode(Gl.GL_MODELVIEW);

                // load the converted GL matrix
                Gl.glLoadMatrixf(tempMatrix);
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
                SetGLLight(i, lightList[i]);
            }

            for( ; i < numCurrentLights; i++) {
                SetGLLight(i, null);
            }

            numCurrentLights = (int)MathUtil.Min(limit, lightList.Count);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public override int ConvertColor(ColorEx color) {
            return color.ToABGR();
        }

        public override CullingMode CullingMode {
            get {
                return cullingMode;
            }
            set {
                // ignore dupe render state
                if(value == cullingMode) {
                   return;
                }

                cullingMode = value;

                int cullMode = Gl.GL_CW;

                switch(value) {
                    case CullingMode.None:
                        Gl.glDisable(Gl.GL_CULL_FACE);
                        return;
                    case CullingMode.Clockwise:
                        cullMode = Gl.GL_CCW;
                        break;
                    case CullingMode.CounterClockwise:
                        cullMode = Gl.GL_CW;
                        break;
                }

                Gl.glEnable(Gl.GL_CULL_FACE);
                Gl.glFrontFace(cullMode);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dest"></param>
        protected override void SetSceneBlending(SceneBlendFactor src, SceneBlendFactor dest) {
            if(src == lastBlendSrc && dest == lastBlendDest) {
                return;
            }

            int srcFactor = ConvertBlendFactor(src);
            int destFactor = ConvertBlendFactor(dest);

            // enable blending and set the blend function
            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(srcFactor, destFactor);

            lastBlendSrc = src;
            lastBlendDest = dest;
        }

        /// <summary>
        /// 
        /// </summary>
        protected override int DepthBias {
            set {
                // reduce dupe state changes
                if(lastDepthBias == value) {
                    return;
                }

                lastDepthBias = value;

                if (value > 0) {
                    Gl.glEnable(Gl.GL_POLYGON_OFFSET_FILL);
                    Gl.glEnable(Gl.GL_POLYGON_OFFSET_POINT);
                    Gl.glEnable(Gl.GL_POLYGON_OFFSET_LINE);
                    // Bias is in {0, 16}, scale the unit addition appropriately
                    Gl.glPolygonOffset(1.0f, value);
                }
                else {
                    Gl.glDisable(Gl.GL_POLYGON_OFFSET_FILL);
                    Gl.glDisable(Gl.GL_POLYGON_OFFSET_POINT);
                    Gl.glDisable(Gl.GL_POLYGON_OFFSET_LINE);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected override bool DepthCheck {
            set {
                // reduce dupe state changes
                if(lastDepthCheck == value) {
                    return;
                }

                lastDepthCheck = value;

                if(value) {
                    // clear the buffer and enable
                    Gl.glClearDepth(1.0f);
                    Gl.glEnable(Gl.GL_DEPTH_TEST);
                }
                else
                    Gl.glDisable(Gl.GL_DEPTH_TEST);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected override CompareFunction DepthFunction {
            set {
                // reduce dupe state changes
                if(lastDepthFunc == value) {
                    return;
                }
                lastDepthFunc = value;

                Gl.glDepthFunc(GLHelper.ConvertEnum(value));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected override bool DepthWrite {
            set {
                // reduce dupe state changes
                if(lastDepthWrite == value) {
                    return;
                }
                lastDepthWrite = value;

                int flag = value ? Gl.GL_TRUE : Gl.GL_FALSE;
                Gl.glDepthMask( flag );  

                // Store for reference in BeginFrame
                depthWrite = value;
            }
        }

        /// <summary>
        ///    Binds the specified GpuProgram to the future rendering operations.
        /// </summary>
        /// <param name="program"></param>
        public override void BindGpuProgram(GpuProgram program) {
            GLGpuProgram glProgram = (GLGpuProgram)program;

            Gl.glEnable(glProgram.GLProgramType);
            Ext.glBindProgramARB(glProgram.GLProgramType, glProgram.ProgramID);
        }

        /// <summary>
        ///    Binds the supplied parameters to programs of the specified type for future rendering operations.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="parms"></param>
        public override void BindGpuProgramParameters(GpuProgramType type, GpuProgramParameters parms) {
            int glType = GLHelper.ConvertEnum(type);

            if(parms.HasFloatConstants) {

                for(int i = 0; i < parms.FloatConstantCount; i++) {
                    int index = parms.GetFloatConstantIndex(i);
                    Axiom.MathLib.Vector4 vec4 = parms.GetFloatConstant(i);

                    tempProgramFloats[0] = vec4.x;
                    tempProgramFloats[1] = vec4.y;
                    tempProgramFloats[2] = vec4.z;
                    tempProgramFloats[3] = vec4.w;

                    // send the params 4 at a time
                    Ext.glProgramLocalParameter4vfARB(glType, index, tempProgramFloats);
                }
            }
        }

        /// <summary>
        ///    Unbinds programs of the specified type.
        /// </summary>
        /// <param name="type"></param>
        public override void UnbindGpuProgram(GpuProgramType type) {
            int glType = GLHelper.ConvertEnum(type);

            Gl.glDisable(glType);
        }

        #endregion Implementation of RenderSystem

        #region Private methods

        /// <summary>
        ///		Private method to convert our blend factors to that of Open GL
        /// </summary>
        /// <param name="factor"></param>
        /// <returns></returns>
        private int ConvertBlendFactor(SceneBlendFactor factor) {
            int glFactor = 0;

            switch(factor) {
                case SceneBlendFactor.One:
                    glFactor =  Gl.GL_ONE;
                    break;
                case SceneBlendFactor.Zero:
                    glFactor =  Gl.GL_ZERO;
                    break;
                case SceneBlendFactor.DestColor:
                    glFactor =  Gl.GL_DST_COLOR;
                    break;
                case SceneBlendFactor.SourceColor:
                    glFactor =  Gl.GL_SRC_COLOR;
                    break;
                case SceneBlendFactor.OneMinusDestColor:
                    glFactor =  Gl.GL_ONE_MINUS_DST_COLOR;
                    break;
                case SceneBlendFactor.OneMinusSourceColor:
                    glFactor =  Gl.GL_ONE_MINUS_SRC_COLOR;
                    break;
                case SceneBlendFactor.DestAlpha:
                    glFactor =  Gl.GL_DST_ALPHA;
                    break;
                case SceneBlendFactor.SourceAlpha:
                    glFactor =  Gl.GL_SRC_ALPHA;
                    break;
                case SceneBlendFactor.OneMinusDestAlpha:
                    glFactor =  Gl.GL_ONE_MINUS_DST_ALPHA;
                    break;
                case SceneBlendFactor.OneMinusSourceAlpha:
                    glFactor =  Gl.GL_ONE_MINUS_SRC_ALPHA;
                    break;
            }

            // return the GL equivalent
            return glFactor;
        }

        /// <summary>
        ///		Converts a Matrix4 object to a float[16] that contains the matrix
        ///		in top to bottom, left to right order.
        ///		i.e.	glMatrix[0] = matrix[0,0]
        ///				glMatrix[1] = matrix[1,0]
        ///				etc...
        /// </summary>
        /// <param name="matrix"></param>
        /// <returns></returns>
        private void MakeGLMatrix(ref Matrix4 matrix, float[] floats) {
            Matrix4 mat = matrix.Transpose();

            mat.MakeFloatArray(floats);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <param name="light"></param>
        private void SetGLLight(int index, Light light) {
            int lightIndex = Gl.GL_LIGHT0 + index;

            if(light == null) {
                // disable the light if it is not visible
                Gl.glDisable(lightIndex);
            }
            else {
                // set spotlight cutoff
                switch(light.Type) {
                    case LightType.Spotlight:
                        Gl.glLightf(lightIndex, Gl.GL_SPOT_CUTOFF, light.SpotlightOuterAngle);
                        break;
                    default:
                        Gl.glLightf(lightIndex, Gl.GL_SPOT_CUTOFF, 180.0f);
                        break;
                }

                // light color
                light.Diffuse.ToArrayRGBA(tempColorVals);
                Gl.glLightfv(lightIndex, Gl.GL_DIFFUSE, tempColorVals);

                // specular color
                light.Specular.ToArrayRGBA(tempColorVals);
                Gl.glLightfv(lightIndex, Gl.GL_SPECULAR, tempColorVals);

                // disable ambient light for objects
                // BUG: Why does this return GL ERROR 1280?
                //Gl.glLighti(lightIndex, Gl.GL_AMBIENT, 0);

                // position (not set for Directional lighting)
                Vector3 vec;

                if(light.Type != LightType.Directional) {
                    vec = light.DerivedPosition;
                    tempColorVals[0] = vec.x;
                    tempColorVals[1] = vec.y;
                    tempColorVals[2] = vec.z;
                    tempColorVals[3] = 1.0f;

                    Gl.glLightfv(lightIndex, Gl.GL_POSITION, tempColorVals);
                }
			
                // direction (not needed for point lights
                if(light.Type != LightType.Point) {
                    vec = light.DerivedDirection;
                    tempColorVals[0] = vec.x;
                    tempColorVals[1] = vec.y;
                    tempColorVals[2] = vec.z;
                    tempColorVals[3] = 1.0f;

                    Gl.glLightfv(lightIndex, Gl.GL_SPOT_DIRECTION, tempColorVals);
                }

                // light attenuation
                Gl.glLightf(lightIndex, Gl.GL_CONSTANT_ATTENUATION, light.AttenuationConstant);
                Gl.glLightf(lightIndex, Gl.GL_LINEAR_ATTENUATION, light.AttenuationLinear);
                Gl.glLightf(lightIndex, Gl.GL_QUADRATIC_ATTENUATION, light.AttenuationQuadratic);

                // enable the light
                Gl.glEnable(lightIndex);
            }
        }

        /// <summary>
        ///		Called in constructor to init configuration.
        /// </summary>
        private void InitConfigOptions() {

            Gdi.DEVMODE setting;
            int i = 0;
            int width, height, bpp, freq;
            
            bool go = User.EnumDisplaySettings(null, i++, out setting);

            while(go) {
                width = setting.dmPelsWidth;
                height = setting.dmPelsHeight;
                bpp = setting.dmBitsPerPel;
                freq = setting.dmDisplayFrequency;
			
                // filter out the lower resolutions and dupe frequencies
                if(width >= 640 && height >= 480 && bpp >= 16) {
                    string query = string.Format("Width = {0} AND Height= {1} AND Bpp = {2}", width, height, bpp);
                    if(engineConfig.DisplayMode.Select(query).Length == 0) {
                        // add a new row to the display settings table
                        engineConfig.DisplayMode.AddDisplayModeRow(width, height, bpp, false, false);
                    }
                }

                // grab the current display settings
                go = User.EnumDisplaySettings(null, i++, out setting);
            }
        }

        /// <summary>
        ///		Helper method to go through and interrogate hardware capabilities.
        /// </summary>
        private void CheckCaps() {

            // find out how many lights we have to play with, then create a light array to keep locally
            int maxLights;
            Gl.glGetIntegerv(Gl.GL_MAX_LIGHTS, out maxLights);
            caps.MaxLights = maxLights;

            // check the number of texture units available
            int numTextureUnits = 0;
            Gl.glGetIntegerv(Gl.GL_MAX_TEXTURE_UNITS, out numTextureUnits);
            caps.NumTextureUnits = numTextureUnits;

            // check multitexturing
            if(GLHelper.SupportsExtension("GL_ARB_multitexture"))
                caps.SetCap(Capabilities.MultiTexturing);

            // check texture blending
            if(GLHelper.SupportsExtension("GL_EXT_texture_env_combine") || GLHelper.SupportsExtension("GL_ARB_texture_env_combine"))
                caps.SetCap(Capabilities.TextureBlending);

            // anisotropic filtering
            if(GLHelper.SupportsExtension("GL_EXT_texture_filter_anisotropic")) {
                caps.SetCap(Capabilities.AnisotropicFiltering);
            }

            // check dot3 support
            if(GLHelper.SupportsExtension("GL_ARB_texture_env_dot3"))
                caps.SetCap(Capabilities.Dot3Bump);

            // check support for vertex buffers in hardware
            // NOTE: GeForce2 MX and GeForce3 report support, but simply doesn't work
            if(GLHelper.SupportsExtension("GL_ARB_vertex_buffer_object")
                && GLHelper.VideoCard.IndexOf("GeForce2 MX") == -1
                && GLHelper.VideoCard.IndexOf("GeForce3") == -1)
                caps.SetCap(Capabilities.VertexBuffer);

            if(GLHelper.SupportsExtension("GL_ARB_texture_cube_map")
                || GLHelper.SupportsExtension("GL_EXT_texture_cube_map")) {
                caps.SetCap(Capabilities.CubeMapping);
            }

            // check support for hardware vertex blending
            // TODO: Dont check this cap yet, wait for vertex shader support so that software blending is always used
            //if(GLHelper.SupportsExtension("GL_ARB_vertex_blend"))
            //    caps.SetCap(Capabilities.VertexBlending);

            // check if the hardware supports anisotropic filtering
            if(GLHelper.SupportsExtension("GL_EXT_texture_filter_anisotropic"))
                caps.SetCap(Capabilities.AnisotropicFiltering);

            // check hardware mip mapping
            // TODO: Only enable this for non-ATI cards temporarily until drivers are fixed
            if(GLHelper.SupportsExtension("GL_SGIS_generate_mipmap")
                && GLHelper.Vendor != "ATI")
                caps.SetCap(Capabilities.HardwareMipMaps);

            // check stencil buffer depth availability
            int stencilBits;
            Gl.glGetIntegerv(Gl.GL_STENCIL_BITS, out stencilBits);
            caps.StencilBufferBits = stencilBits;

            // if stencil bits are available, enable stencil buffering
            if(stencilBits > 0) {
                caps.SetCap(Capabilities.StencilBuffer);
            }

            // Vertex Programs
            if(GLHelper.SupportsExtension("GL_ARB_vertex_program")) {
                caps.SetCap(Capabilities.VertexPrograms);
                caps.MaxVertexProgramVersion = "arbvp1";
                caps.VertexProgramConstantIntCount = 0;
                int maxFloats;
                Gl.glGetIntegerv(Gl.GL_MAX_PROGRAM_LOCAL_PARAMETERS_ARB, out maxFloats);
                caps.VertexProgramConstantFloatCount = maxFloats;
                gpuProgramMgr.PushSyntaxCode("arbvp1");
            }

            // Fragment Programs
            if(GLHelper.SupportsExtension("GL_ARB_fragment_program")) {
                caps.SetCap(Capabilities.FragmentPrograms);
                caps.MaxFragmentProgramVersion = "arbfp1";
                caps.FragmentProgramConstantIntCount = 0;
                int maxFloats;
                Gl.glGetIntegerv(Gl.GL_MAX_PROGRAM_LOCAL_PARAMETERS_ARB, out maxFloats);
                caps.FragmentProgramConstantFloatCount = maxFloats;
                gpuProgramMgr.PushSyntaxCode("arbfp1");
            }

            // write info to logs
            caps.Log();
        }

        /// <summary>
        ///    
        /// </summary>
        /// <returns></returns>
        private int GetCombinedMinMipFilter() {
            switch(minFilter) {
                case FilterOptions.Anisotropic:
                case FilterOptions.Linear:
                    switch(mipFilter) {
                        case FilterOptions.Anisotropic:
                        case FilterOptions.Linear:
                            // linear min, linear map
                            return Gl.GL_LINEAR_MIPMAP_LINEAR;
                        case FilterOptions.Point:
                            // linear min, point mip
                            return Gl.GL_LINEAR_MIPMAP_NEAREST;
                        case FilterOptions.None:
                            // linear, no mip
                            return Gl.GL_LINEAR;
                    }
                    break;

                case FilterOptions.Point:
                case FilterOptions.None:
                    switch(mipFilter) {
                        case FilterOptions.Anisotropic:
                        case FilterOptions.Linear:
                            // nearest min, linear mip
                            return Gl.GL_NEAREST_MIPMAP_LINEAR;
                        case FilterOptions.Point:
                            // nearest min, point mip
                            return Gl.GL_NEAREST_MIPMAP_NEAREST;
                        case FilterOptions.None:
                            // nearest min, no mip
                            return Gl.GL_NEAREST;
                    }
                    break;
            }

            // should never get here, but make the compiler happy
            return 0;
        }

        /// <summary>
        ///		Convenience method for VBOs
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        private IntPtr BUFFER_OFFSET(int i) {
            return new IntPtr(i);
        }

        #endregion Private methods

        #region Implementation of IPlugin

        public void Start() {
            // add an instance of this plugin to the list of available RenderSystems
            Engine.Instance.RenderSystems.Add("OpenGL", this);
        }

        public void Stop() {
        }

        #endregion Implementation of IPlugin
    }
}
