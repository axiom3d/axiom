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
using Axiom.Configuration;
using Axiom.Core;
using Axiom.Enumerations;
using Axiom.MathLib;
using Axiom.SubSystems.Rendering;
using Axiom.Utility;
using CsGL.OpenGL;
using CsGL.Util;
using Tao.OpenGl;
using Tao.Platform.Windows;

namespace RenderSystem_OpenGL
{
	public class Ext : OpenGL_Extension
	{
	}
		/// <summary>
		/// Summary description for OpenGLRenderer.
		/// </summary>
		public class OpenGLRenderer : RenderSystem, IPlugin
		{
			/// <summary>OpenGL Context (from CsGL)</summary>
			protected OpenGLContext context;
			/// <summary>Object that allows for calls to OpenGL extensions.  Named all upper for consistency since GL calls are static through GL class.</summary>
			protected OpenGL_Extension Ext;

			protected IntPtr glClientActiveTextureARB;
			protected IntPtr glActiveTextureARB;

			/// <summary>Internal view matrix.</summary>
			protected Matrix4 viewMatrix;
			/// <summary>Internal world matrix.</summary>
			protected Matrix4 worldMatrix;
			/// <summary>Internal texture matrix.</summary>
			protected Matrix4 textureMatrix;

			// used for manual texture matrix calculations, for things like env mapping
			protected bool useAutoTextureMatrix;
			protected float[] autoTextureMatrix = new float[16];

			// retained stencil buffer params vals, since we allow setting invidual params but GL
			// only lets you set them all at once, keep old values around to allow this to work
			protected int stencilFail, stencilZFail, stencilPass, stencilFunc, stencilRef, stencilMask;

			// local array of light objects to reference during light updating, disabling, etc
			protected Light[] lights;

			protected bool zTrickEven;

			public OpenGLRenderer()
			{
				viewMatrix = Matrix4.Identity;
				worldMatrix = Matrix4.Identity;
				textureMatrix = Matrix4.Identity;

				// init the stored stencil buffer params
				stencilFail = stencilZFail = stencilPass = Gl.GL_KEEP;
				stencilFunc = Gl.GL_ALWAYS;
				stencilRef = 0;
				stencilMask = unchecked((int)0xffffffff);

				// create a new OpenGLExtensions object
				Ext = new Ext();

				InitConfigOptions();
			}

			#region Implementation of RenderSystem

			public override RenderWindow CreateRenderWindow(String name, System.Windows.Forms.Control target, int width, int height, int colorDepth,
				bool isFullscreen, int left, int top, bool depthBuffer, RenderWindow parent)
			{
				RenderWindow window = new GLWindow();

				// see if a OpenGLContext has been created yet
				if(context == null)
				{
					// assign a new context for this target and create it
					context = new ControlGLContext(target);
					context.Create(new DisplayType(0, 0), null);
					context.Grab();

					// intialize GL extensions and check capabilites
					GLHelper.InitializeExtensions();

					CheckCaps();

					// log hardware info
					System.Diagnostics.Trace.WriteLine(String.Format("Vendor: {0}", GLHelper.Vendor));
					System.Diagnostics.Trace.WriteLine(String.Format("Video Board: {0}", Gl.glGetString(Gl.GL_RENDERER)));
					System.Diagnostics.Trace.WriteLine(String.Format("Version: {0}", Gl.glGetString(Gl.GL_VERSION)));
				
					System.Diagnostics.Trace.WriteLine("Extensions supported:");

					foreach(String ext in GLHelper.Extensions)
						System.Diagnostics.Trace.WriteLine(ext);

					ScreenSetting[] settings = ScreenSetting.CompatibleDisplay;

					if(isFullscreen)
					{
						ScreenSetting setting = new ScreenSetting(width, height, colorDepth);
						setting.Set();
					}
	
					// init the GL context
					Gl.glShadeModel(Gl.GL_SMOOTH);							// Enable Smooth Shading
					Gl.glClearColor(0.0f, 0.0f, 0.0f, 0.5f);				// Black Background
					Gl.glClearDepth(1.0f);									// Depth Buffer Setup
					Gl.glEnable(Gl.GL_DEPTH_TEST);							// Enables Depth Testing
					Gl.glDepthFunc(Gl.GL_LEQUAL);								// The Type Of Depth Testing To Do
					Gl.glHint(Gl.GL_PERSPECTIVE_CORRECTION_HINT, Gl.GL_NICEST);	// Really Nice Perspective Calculations

					// swap out existing memory.  drops the memory consumption of the app drastically.  equivalent
					// to the ol' minimize/maximize trick.
					Kernel.SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, -1, -1);
				}

				// create the window
				window.Create(name, target, width, height, colorDepth, isFullscreen, left, top, depthBuffer, context);

				// add the new window to the RenderWindow collection
				this.renderWindows.Add(window);

				// by creating our texture manager, singleton TextureManager will hold our implementation
				textureMgr = new GLTextureManager();

				// create a specialized instance, which registers itself as the singleton instance of HardwareBufferManager
				if(caps.CheckCap(Capabilities.VertexBuffer))
					hardwareBufferManager = new GLHardwareBufferManager();
				else
					hardwareBufferManager = new GLSoftwareBufferManager();

				glActiveTextureARB = Wgl.wglGetProcAddress("glActiveTextureARB");
				glClientActiveTextureARB = Wgl.wglGetProcAddress("glClientActiveTextureARB");

				return window;
			}
	
			public override ColorEx AmbientLight
			{
				set
				{
					// create a float[4]  to contain the RGBA data
					float[] ambient = GlColorArray(value);
					ambient[3] = 0.0f;

					// set the ambient color
					Gl.glLightModelfv(Gl.GL_LIGHT_MODEL_AMBIENT, ambient);
				}
			}
	
			/// <summary>
			///		Gets/Sets the global lighting switch.
			/// </summary>
			public override bool LightingEnabled
			{
				set
				{
					if(value)
						Gl.glEnable(Gl.GL_LIGHTING);
					else
						Gl.glDisable(Gl.GL_LIGHTING);
				}
			}

			/// <summary>
			///		Sets the mode to use for rendering
			/// </summary>
			protected override SceneDetailLevel RasterizationMode
			{
				set
				{
					// default to fill to make compiler happy
					int mode = Gl.GL_FILL;

					switch(value)
					{
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
				}
			}

			public override Shading ShadingType
			{
				// OpenGL supports Flat and Smooth shaded primitives
				set
				{
					switch(value)
					{
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
			public override StencilOperation StencilBufferDepthFailOperation
			{
				set
				{
					// Have to use saved values for other params since GL doesn't have 
					// individual setters
					stencilZFail = GLHelper.ConvertEnum(value);
					Gl.glStencilOp(stencilFail, stencilZFail, stencilPass);
				}
			}

			/// <summary>
			/// 
			/// </summary>
			public override StencilOperation StencilBufferFailOperation
			{
				set
				{
					// Have to use saved values for other params since GL doesn't have 
					// individual setters
					stencilFail = GLHelper.ConvertEnum(value);
					Gl.glStencilOp(stencilFail, stencilZFail, stencilPass);
				}
			}

			/// <summary>
			/// 
			/// </summary>
			public override CompareFunction StencilBufferFunction
			{
				set
				{
					// Have to use saved values for other params since GL doesn't have 
					// individual setters
					stencilFunc = GLHelper.ConvertEnum(value);
					Gl.glStencilFunc(stencilFunc, stencilRef, stencilMask);
				}
			}

			/// <summary>
			/// 
			/// </summary>
			public override int StencilBufferMask
			{
				set
				{
					// Have to use saved values for other params since GL doesn't have 
					// individual setters
					stencilMask = value;
					Gl.glStencilFunc(stencilFunc, stencilRef, stencilMask);
				}
			}

			/// <summary>
			/// 
			/// </summary>
			public override StencilOperation StencilBufferPassOperation
			{
				set
				{
					// Have to use saved values for other params since GL doesn't have 
					// individual setters
					stencilPass = GLHelper.ConvertEnum(value);
					Gl.glStencilOp(stencilFail, stencilZFail, stencilPass);
				}
			}

			/// <summary>
			/// 
			/// </summary>
			public override int StencilBufferReferenceValue
			{
				set
				{
					// Have to use saved values for other params since GL doesn't have 
					// individual setters
					stencilRef = value;
					Gl.glStencilFunc(stencilFunc, stencilRef, stencilMask);
				}
			}

			/// <summary>
			///		Specifies whether stencil check should be enabled or not.
			/// </summary>
			public override bool StencilCheckEnabled
			{
				set
				{
					if(value)
						Gl.glEnable(Gl.GL_STENCIL_TEST);
					else
						Gl.glDisable(Gl.GL_STENCIL_TEST);
				}
			}

			/// <summary>
			/// 
			/// </summary>
			public override TextureFiltering TextureFiltering
			{
				set
				{
					int numUnits = caps.NumTextureUnits;

					// set for all texture units
					for(int unit = 0; unit < numUnits; unit++)
					{
						Gl.glActiveTextureARB(glActiveTextureARB, Gl.GL_TEXTURE0 + unit);

						switch(value)
						{
							case Axiom.SubSystems.Rendering.TextureFiltering.Trilinear:
							{
								Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAG_FILTER, Gl.GL_LINEAR);
								Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MIN_FILTER, Gl.GL_LINEAR_MIPMAP_LINEAR);

							} break;

							case Axiom.SubSystems.Rendering.TextureFiltering.Bilinear:
							{
								Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAG_FILTER, Gl.GL_LINEAR);
								Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MIN_FILTER, Gl.GL_LINEAR_MIPMAP_NEAREST);

							} break;

							case Axiom.SubSystems.Rendering.TextureFiltering.None:
							{
								Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAG_FILTER, Gl.GL_NEAREST);
								Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MIN_FILTER, Gl.GL_NEAREST);

							} break;
						} // switch
					} // for

					// reset texture unit
					Gl.glActiveTextureARB(glActiveTextureARB, Gl.GL_TEXTURE0 );
				}
			}

			/// <summary>
			///		Creates a projection matrix specific to OpenGL based on the given params.
			/// </summary>
			/// <param name="fov"></param>
			/// <param name="aspectRatio"></param>
			/// <param name="near"></param>
			/// <param name="far"></param>
			/// <returns></returns>
			public override Axiom.MathLib.Matrix4 MakeProjectionMatrix(float fov, float aspectRatio, float near, float far)
			{
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
			protected override void BeginFrame()
			{
				Debug.Assert(activeViewport != null, "BeingFrame cannot run without an active viewport.");

				if(activeViewport.ClearEveryFrame)
				{
					float[] color = GlColorArray(activeViewport.BackgroundColor);

					// clear the viewport
					Gl.glClearColor(color[0], color[1], color[2], color[3]);

					// disable depth write if it isnt
					if(!depthWrite)
						Gl.glDepthMask(Gl.GL_TRUE);

					// clear the color buffer and depth buffer bits
					Gl.glClear(Gl.GL_COLOR_BUFFER_BIT | Gl.GL_DEPTH_BUFFER_BIT);	

					// Reset depth write state if appropriate
					// Enable depth buffer for writing if it isn't
					if(!depthWrite)
						Gl.glDepthMask(Gl.GL_FALSE);
				}
				else
				{
					// Use Carmack's ztrick to avoid clearing the depth buffer every frame
					if(zTrickEven)
					{
						this.DepthFunction = CompareFunction.LessEqual;
						Gl.glDepthRange(0, 0.499999999);
					}
					else
					{
						this.DepthFunction = CompareFunction.GreaterEqual;
						Gl.glDepthRange(1, 0.5);
					}

					// swap the z trick flag
					zTrickEven = !zTrickEven;
				}

				// Reset all lights
				ResetLights();
			}

			/// <summary>
			/// 
			/// </summary>
			protected override void EndFrame()
			{
				// Nothing to do here really
			}

			/// <summary>
			/// 
			/// </summary>
			/// <param name="viewport"></param>
			protected override void SetViewport(Viewport viewport)
			{
				// TODO: Make sure to remember what happens to alter the viewport drawing behavior
				if(activeViewport != viewport || viewport.IsUpdated)
				{
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
			protected override void SetSurfaceParams(ColorEx ambient, ColorEx diffuse, ColorEx specular, ColorEx emissive, float shininess)
			{
				// ambient
				float[] vals = GlColorArray(ambient);
				Gl.glMaterialfv(Gl.GL_FRONT_AND_BACK, Gl.GL_AMBIENT, vals);

				// diffuse
				vals[0] = diffuse.r; vals[1] = diffuse.g; vals[2] = diffuse.b; vals[3] = diffuse.a;
				Gl.glMaterialfv(Gl.GL_FRONT_AND_BACK, Gl.GL_DIFFUSE, vals);

				// specular
				vals[0] = specular.r; vals[1] = specular.g; vals[2] = specular.b; vals[3] = specular.a;
				Gl.glMaterialfv(Gl.GL_FRONT_AND_BACK, Gl.GL_SPECULAR, vals);

				// emissive
				vals[0] = emissive.r; vals[1] = emissive.g; vals[2] = emissive.b; vals[3] = emissive.a;
				Gl.glMaterialfv(Gl.GL_FRONT_AND_BACK, Gl.GL_EMISSION, vals);

				// shininess
				Gl.glMaterialf(Gl.GL_FRONT_AND_BACK, Gl.GL_SHININESS, shininess);
			}
	
			/// <summary>
			/// 
			/// </summary>
			/// <param name="stage"></param>
			/// <param name="texAddressingMode"></param>
			protected override void SetTextureAddressingMode(int stage, TextureAddressing texAddressingMode)
			{
				int type = 0;

				// find out the GL equivalent of out TextureAddressing enum
				switch(texAddressingMode)
				{
					case TextureAddressing.Wrap:
						type = Gl.GL_REPEAT;
						break;

					case TextureAddressing.Mirror:
						// TODO: Re-add prefix after switching to Tao
						type = Gl.GL_MIRRORED_REPEAT;
						break;

					case TextureAddressing.Clamp:
						type = Gl.GL_CLAMP_TO_EDGE;
						break;
				} // end switch

				// set the GL texture wrap params for the specified unit
				Gl.glActiveTextureARB(glActiveTextureARB,Gl.GL_TEXTURE0 + stage);
				Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_S, type);
				Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_T, type);
				Gl.glActiveTextureARB(glActiveTextureARB,Gl.GL_TEXTURE0);
			}
	
			/// <summary>
			/// 
			/// </summary>
			/// <param name="stage"></param>
			/// <param name="blendMode"></param>
			public override void SetTextureBlendMode(int stage, LayerBlendModeEx blendMode)
			{
				float[] cv1 = blendMode.colorArg1.ToArrayRGBA();
				float[] cv2 = blendMode.colorArg2.ToArrayRGBA();
				float[] av1 = new float[4] {0.0f, 0.0f, 0.0f, blendMode.alphaArg1};
				float[] av2 = new float[4] {0.0f, 0.0f, 0.0f, blendMode.alphaArg2};

				int src1op, src2op, cmd;

				src1op = src2op = cmd = 0;

				switch(blendMode.source1)
				{
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

				switch(blendMode.source2)
				{
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

				switch (blendMode.operation)
				{
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

				Gl.glActiveTextureARB(glActiveTextureARB,Gl.GL_TEXTURE0 + stage);
				Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_TEXTURE_ENV_MODE, (int)Gl.GL_COMBINE);

				if (blendMode.blendType == LayerBlendType.Color)
				{
					Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_COMBINE_RGB, (int)cmd);
					Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_SOURCE0_RGB, (int)src1op);
					Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_SOURCE1_RGB, (int)src2op);
					Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_SOURCE2_RGB, (int)Gl.GL_CONSTANT);
				}
				else
				{
					if (cmd != Gl.GL_DOT3_RGB)
						Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_COMBINE_ALPHA, (int)cmd);

					Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_SOURCE0_ALPHA, (int)src1op);
					Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_SOURCE1_ALPHA, (int)src2op);
					Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_SOURCE2_ALPHA, (int)Gl.GL_CONSTANT);
				}

				switch (blendMode.operation)
				{
					case LayerBlendOperationEx.BlendTextureAlpha:
						Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_SOURCE2_RGB, (int)Gl.GL_TEXTURE);
						Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_SOURCE2_ALPHA, (int)Gl.GL_TEXTURE);
						break;
					case LayerBlendOperationEx.BlendCurrentAlpha:
						Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_SOURCE2_RGB, (int)Gl.GL_PREVIOUS);
						Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_SOURCE2_ALPHA, (int)Gl.GL_PREVIOUS);
						break;
					case LayerBlendOperationEx.Modulate:
						Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, blendMode.blendType == LayerBlendType.Color ? 
							Gl.GL_RGB_SCALE : Gl.GL_ALPHA_SCALE, 1);
						break;
					case LayerBlendOperationEx.ModulateX2:
						Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, blendMode.blendType == LayerBlendType.Color ? 
							Gl.GL_RGB_SCALE : Gl.GL_ALPHA_SCALE, 2);
						break;
					case LayerBlendOperationEx.ModulateX4:
						Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, blendMode.blendType == LayerBlendType.Color ? 
							Gl.GL_RGB_SCALE : Gl.GL_ALPHA_SCALE, 4);
						break;
					default:
						break;
				}

				Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_OPERAND0_RGB, Gl.GL_SRC_COLOR);
				Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_OPERAND1_RGB, Gl.GL_SRC_COLOR);
				Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_OPERAND2_RGB, Gl.GL_SRC_COLOR);
				Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_OPERAND0_ALPHA, Gl.GL_SRC_ALPHA);
				Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_OPERAND1_ALPHA, Gl.GL_SRC_ALPHA);
				Gl.glTexEnvi(Gl.GL_TEXTURE_ENV, Gl.GL_OPERAND2_ALPHA, Gl.GL_SRC_ALPHA);

				if (blendMode.blendType == LayerBlendType.Color && blendMode.source1 == LayerBlendSource.Manual)
					Gl.glTexEnvfv(Gl.GL_TEXTURE_ENV, Gl.GL_TEXTURE_ENV_COLOR, cv1);
				if (blendMode.blendType == LayerBlendType.Color && blendMode.source2 == LayerBlendSource.Manual)
					Gl.glTexEnvfv(Gl.GL_TEXTURE_ENV, Gl.GL_TEXTURE_ENV_COLOR, cv2);
            
				Gl.glActiveTextureARB(glActiveTextureARB,Gl.GL_TEXTURE0);
			}
	
			/// <summary>
			/// 
			/// </summary>
			/// <param name="stage"></param>
			/// <param name="index"></param>
			protected override void SetTextureCoordSet(int stage, int index)
			{
				// TODO:  Add OpenGLRenderer.SetTextureCoordSet implementation
			}

			/// <summary>
			/// 
			/// </summary>
			/// <param name="stage"></param>
			/// <param name="method"></param>
			protected override void SetTextureCoordCalculation(int stage, TexCoordCalcMethod method)
			{
				float[] m = new float[16];
 
				// Default to no extra auto texture matrix
				useAutoTextureMatrix = false;

				Gl.glActiveTextureARB(glActiveTextureARB, Gl.GL_TEXTURE0 + stage );

				switch(method)
				{
					case TexCoordCalcMethod.None:
						Gl.glDisable( Gl.GL_TEXTURE_GEN_S );
						Gl.glDisable( Gl.GL_TEXTURE_GEN_T );
						Gl.glDisable( Gl.GL_TEXTURE_GEN_R );
						Gl.glDisable( Gl.GL_TEXTURE_GEN_Q );
						break;

					case TexCoordCalcMethod.EnvironmentMap:
						Gl.glTexGeni( Gl.GL_S, Gl.GL_TEXTURE_GEN_MODE, (int)Gl.GL_SPHERE_MAP );
						Gl.glTexGeni( Gl.GL_T, Gl.GL_TEXTURE_GEN_MODE, (int)Gl.GL_SPHERE_MAP );

						Gl.glEnable( Gl.GL_TEXTURE_GEN_S );
						Gl.glEnable( Gl.GL_TEXTURE_GEN_T );
						Gl.glDisable( Gl.GL_TEXTURE_GEN_R );
						Gl.glDisable( Gl.GL_TEXTURE_GEN_Q );
						break;

					case TexCoordCalcMethod.EnvironmentMapPlanar:            
						// XXX This doesn't seem right?!
						if(GLHelper.CheckMinVersion("1.3"))
						{
							Gl.glTexGeni( Gl.GL_S, Gl.GL_TEXTURE_GEN_MODE, (int)Gl.GL_REFLECTION_MAP );
							Gl.glTexGeni( Gl.GL_T, Gl.GL_TEXTURE_GEN_MODE, (int)Gl.GL_REFLECTION_MAP );
							Gl.glTexGeni( Gl.GL_R, Gl.GL_TEXTURE_GEN_MODE, (int)Gl.GL_REFLECTION_MAP );

							Gl.glEnable( Gl.GL_TEXTURE_GEN_S );
							Gl.glEnable( Gl.GL_TEXTURE_GEN_T );
							Gl.glEnable( Gl.GL_TEXTURE_GEN_R );
							Gl.glDisable( Gl.GL_TEXTURE_GEN_Q );
						}
						else
						{
							Gl.glTexGeni( Gl.GL_S, Gl.GL_TEXTURE_GEN_MODE, (int)Gl.GL_SPHERE_MAP );
							Gl.glTexGeni( Gl.GL_T, Gl.GL_TEXTURE_GEN_MODE, (int)Gl.GL_SPHERE_MAP );

							Gl.glEnable( Gl.GL_TEXTURE_GEN_S );
							Gl.glEnable( Gl.GL_TEXTURE_GEN_T );
							Gl.glDisable( Gl.GL_TEXTURE_GEN_R );
							Gl.glDisable( Gl.GL_TEXTURE_GEN_Q );
						}
						break;

					case TexCoordCalcMethod.EnvironmentMapReflection:
            
						Gl.glTexGeni( Gl.GL_S, Gl.GL_TEXTURE_GEN_MODE, (int)Gl.GL_REFLECTION_MAP );
						Gl.glTexGeni( Gl.GL_T, Gl.GL_TEXTURE_GEN_MODE, (int)Gl.GL_REFLECTION_MAP );
						Gl.glTexGeni( Gl.GL_R, Gl.GL_TEXTURE_GEN_MODE, (int)Gl.GL_REFLECTION_MAP );

						Gl.glEnable( Gl.GL_TEXTURE_GEN_S );
						Gl.glEnable( Gl.GL_TEXTURE_GEN_T );
						Gl.glEnable( Gl.GL_TEXTURE_GEN_R );
						Gl.glDisable( Gl.GL_TEXTURE_GEN_Q );

						// We need an extra texture matrix here
						// This sets the texture matrix to be the inverse of the modelview matrix
						useAutoTextureMatrix = true;

						Gl.glGetFloatv( Gl.GL_MODELVIEW_MATRIX, m);

						// Transpose 3x3 in order to invert matrix (rotation)
						// Note that we need to invert the Z _before_ the rotation
						// No idea why we have to invert the Z at all, but reflection is wrong without it
						autoTextureMatrix[0] = m[0]; autoTextureMatrix[1] = m[4]; autoTextureMatrix[2] = -m[8];
						autoTextureMatrix[4] = m[1]; autoTextureMatrix[5] = m[5]; autoTextureMatrix[6] = -m[9];
						autoTextureMatrix[8] = m[2]; autoTextureMatrix[9] = m[6]; autoTextureMatrix[10] = -m[10];
						autoTextureMatrix[3] = autoTextureMatrix[7] = autoTextureMatrix[11] = 0.0f;
						autoTextureMatrix[12] = autoTextureMatrix[13] = autoTextureMatrix[14] = 0.0f;
						autoTextureMatrix[15] = 1.0f;

						break;

					case TexCoordCalcMethod.EnvironmentMapNormal:
						Gl.glTexGeni( Gl.GL_S, Gl.GL_TEXTURE_GEN_MODE, (int)Gl.GL_NORMAL_MAP );
						Gl.glTexGeni( Gl.GL_T, Gl.GL_TEXTURE_GEN_MODE, (int)Gl.GL_NORMAL_MAP );
						Gl.glTexGeni( Gl.GL_R, Gl.GL_TEXTURE_GEN_MODE, (int)Gl.GL_NORMAL_MAP );

						Gl.glEnable( Gl.GL_TEXTURE_GEN_S );
						Gl.glEnable( Gl.GL_TEXTURE_GEN_T );
						Gl.glEnable( Gl.GL_TEXTURE_GEN_R );
						Gl.glDisable( Gl.GL_TEXTURE_GEN_Q );
						break;

					default:
						break;
				}

				Gl.glActiveTextureARB(glActiveTextureARB,Gl.GL_TEXTURE0);		
			}
	
			/// <summary>
			/// 
			/// </summary>
			/// <param name="stage"></param>
			/// <param name="xform"></param>
			protected override void SetTextureMatrix(int stage, Matrix4 xform)
			{
				float[] glMatrix = MakeGLMatrix(xform);

				glMatrix[12] = glMatrix[8];
				glMatrix[13] = glMatrix[9];

				Gl.glActiveTextureARB(glActiveTextureARB,Gl.GL_TEXTURE0 + stage);
				Gl.glMatrixMode(Gl.GL_TEXTURE);

				// if texture matrix was precalced, use that
				if(useAutoTextureMatrix)
				{
					Gl.glLoadMatrixf(autoTextureMatrix);
					Gl.glMultMatrixf(glMatrix);
				}
				else
					Gl.glLoadMatrixf(glMatrix);

				// reset to mesh view matrix and to tex unit 0
				Gl.glMatrixMode(Gl.GL_MODELVIEW);
				Gl.glActiveTextureARB(glActiveTextureARB,Gl.GL_TEXTURE0);
			}

			#endregion

			/// <summary>
			/// 
			/// </summary>
			/// <param name="autoCreateWindow"></param>
			/// <returns></returns>
			public override RenderWindow Initialize(bool autoCreateWindow)
			{
				base.Initialize (autoCreateWindow);

				RenderWindow renderWindow = null;

				if(autoCreateWindow)
				{
					EngineConfig.DisplayModeRow[] modes = 
						(EngineConfig.DisplayModeRow[])engineConfig.DisplayMode.Select("Selected = true");

					EngineConfig.DisplayModeRow mode = modes[0];

					DefaultForm newWindow = RenderWindow.CreateDefaultForm(0, 0, mode.Width, mode.Height, mode.FullScreen);

					// create a new render window
					renderWindow = this.CreateRenderWindow("Main Window", newWindow.Target, mode.Width, mode.Height, mode.Bpp, mode.FullScreen, 0, 0, true, null);

					// set the default form's renderwindow so it can access it internally
					newWindow.RenderWindow = renderWindow;

					// show the window
					newWindow.Show();
				}

				return renderWindow;
			}

			/// <summary>
			///		Shutdown the render system.
			/// </summary>
			public override void Shutdown()
			{
				// call base Shutdown implementation
				base.Shutdown();

				if(context != null)
				{
					// drop and dispose of the context
					context.Drop();
					context.Dispose();
				}

			}

			/// <summary>
			/// 
			/// </summary>
			/// <param name="stage"></param>
			/// <param name="enabled"></param>
			/// <param name="textureName"></param>
			protected override void SetTexture(int stage, bool enabled, string textureName)
			{
				// load the texture
				GLTexture texture = (GLTexture)TextureManager.Instance[textureName];

				// set the active texture
				Gl.glActiveTextureARB(glActiveTextureARB,Gl.GL_TEXTURE0 + stage);

				// enable and bind the texture if necessary
				if(enabled && texture != null)
				{
					Gl.glEnable(Gl.GL_TEXTURE_2D);
					Gl.glBindTexture(Gl.GL_TEXTURE_2D, (int)texture.TextureID);
				}
				else
				{
					Gl.glDisable(Gl.GL_TEXTURE_2D);
					Gl.glTexEnvf(Gl.GL_TEXTURE_ENV, Gl.GL_TEXTURE_ENV_MODE, Gl.GL_MODULATE);
				}

				// reset active texture to unit 0
				Gl.glActiveTextureARB(glActiveTextureARB,Gl.GL_TEXTURE0);
			}
	
			/// <summary>
			/// 
			/// </summary>
			/// <param name="mode"></param>
			/// <param name="color"></param>
			/// <param name="density"></param>
			/// <param name="start"></param>
			/// <param name="end"></param>
			protected override void SetFog(FogMode mode, ColorEx color, float density, float start, float end)
			{
				uint fogMode;

				switch(mode)
				{
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
						Gl.glDisable(Gl.GL_FOG);
						return;
				} // switch

				Gl.glEnable(Gl.GL_FOG);
				Gl.glFogi(Gl.GL_FOG_MODE, (int)fogMode);
				float[] fogColor = GlColorArray(color);
				Gl.glFogfv(Gl.GL_FOG_COLOR, fogColor);
				Gl.glFogf(Gl.GL_FOG_DENSITY, density);
				Gl.glFogf(Gl.GL_FOG_START, start);
				Gl.glFogf(Gl.GL_FOG_END, end);

				// TODO: Fog hints maybe?
			}

			/// <summary>
			/// 
			/// </summary>
			/// <param name="op"></param>
			public override void Render(RenderOperation op)
			{
				// call base class method first
				base.Render (op);
	
				// get a list of the vertex elements for this render operation
				IList elements = op.vertexData.vertexDeclaration.Elements;

				// will be used to alia either the buffer offset (VBO's) or array data if VBO's are
				// not available
				IntPtr bufferData = IntPtr.Zero;
		
				// loop through and handle each element
				for(int i = 0; i < elements.Count; i++)
				{
					// get a reference to the current object in the collection
					VertexElement element = (VertexElement)elements[i];

					// get the current vertex buffer
					HardwareVertexBuffer vertexBuffer = 	op.vertexData.vertexBufferBinding.GetBuffer(element.Source);

					if(caps.CheckCap(Capabilities.VertexBuffer))
					{
						// get the buffer id
						uint bufferId = ((GLHardwareVertexBuffer)vertexBuffer).GLBufferID;

						// bind the current vertex buffer
						Ext.glBindBufferARB(Gl.GL_ARRAY_BUFFER_ARB, bufferId);
						bufferData = BUFFER_OFFSET(element.Offset);
					}
					else
					{
						// get a direct pointer to the software buffer data for using standard vertex arrays
						bufferData = ((SoftwareVertexBuffer)vertexBuffer).GetDataPointer(element.Offset);
					}

					// get the type of this buffer
					int type = GLHelper.ConvertEnum(element.Type);

					unsafe
					{
						// set pointer usage based on the use of this buffer
						switch(element.Semantic)
						{
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
								// TODO: Add glSecondaryColorPointer to CsGL
								break;

							case VertexElementSemantic.TexCoords:
								// this ignores vertex element index and sets tex array for each available texture unit
								// this allows for multitexturing on entities whose mesh only has a single set of tex coords
 
								for(int j = 0; j < caps.NumTextureUnits; j++)
								{
									// set the current active texture unit
									Ext.glClientActiveTextureARB(Gl.GL_TEXTURE0 + (uint)j);

									int tmp = Gl.glIsEnabled(Gl.GL_TEXTURE_2D);

									if(Gl.glIsEnabled(Gl.GL_TEXTURE_2D) != 0)
									{
										// set the tex coord pointer
										Gl.glTexCoordPointer(
											VertexElement.GetTypeCount(element.Type),
											type,
											vertexBuffer.VertexSize,
											bufferData);
									}

									// enable texture coord state
									Gl.glEnableClientState(Gl.GL_TEXTURE_COORD_ARRAY);
								}
								break;

							default:
								break;
						} // switch
					} // unsafe
				} // for

				// reset to texture unit 0
				Ext.glClientActiveTextureARB(Gl.GL_TEXTURE0);

				int primType = 0;

				// which type of render operation is this?
				switch(op.operationType)
				{
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

				unsafe
				{
					if(op.useIndices)
					{
						// setup a pointer to the index data
						IntPtr indexPtr = IntPtr.Zero;

						// if hardware is supported, expect it is a hardware buffer.  else, fallback to software
						if(caps.CheckCap(Capabilities.VertexBuffer))
						{
							// get the index buffer id
							uint idxBufferID = ((GLHardwareIndexBuffer)op.indexData.indexBuffer).GLBufferID;

							// bind the current index buffer
							Ext.glBindBufferARB(Gl.GL_ELEMENT_ARRAY_BUFFER_ARB, idxBufferID);

							// get the offset pointer to the data in the vbo
							indexPtr = BUFFER_OFFSET(op.indexData.indexStart);
						}
						else
						{
							// get the index data as a direct pointer to the software buffer data
							indexPtr = ((SoftwareIndexBuffer)op.indexData.indexBuffer).GetDataPointer(op.indexData.indexStart);
						}

						// find what type of index buffer elements we are using
						int indexType = (op.indexData.indexBuffer.Type == IndexType.Size16) 
							? Gl.GL_UNSIGNED_SHORT : Gl.GL_UNSIGNED_INT;

						// draw the indexed vertex data
						Gl.glDrawElements(primType, op.indexData.indexCount, indexType, indexPtr);
					}
					else
					{
						Gl.glDrawArrays(primType, 0, op.vertexData.vertexCount);
					}
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
			protected override Matrix4 ProjectionMatrix
			{
				set
				{
					// create a float[16] from our Matrix4
					float[] glMatrix = MakeGLMatrix(value);
				
					// set the matrix mode to Projection
					Gl.glMatrixMode(Gl.GL_PROJECTION);

					// load the float array into the projection matrix
					Gl.glLoadMatrixf(glMatrix);

					// set the matrix mode back to ModelView
					Gl.glMatrixMode(Gl.GL_MODELVIEW);
				}
			}

			/// <summary>
			///		
			/// </summary>
			protected override Matrix4 ViewMatrix
			{
				set
				{
					viewMatrix = value;

					// create a float[16] from our Matrix4
					float[] glMatrix = MakeGLMatrix(viewMatrix);
				
					// set the matrix mode to ModelView
					Gl.glMatrixMode(Gl.GL_MODELVIEW);
				
					// load the float array into the ModelView matrix
					Gl.glLoadMatrixf(glMatrix);

					// Reset lights here after a view change
					ResetLights();

					// convert the internal world matrix
					glMatrix = MakeGLMatrix(worldMatrix);

					// multply the world matrix by the current ModelView matrix
					Gl.glMultMatrixf(glMatrix);
				}
			}

			/// <summary>
			/// </summary>
			protected override Matrix4 WorldMatrix
			{
				set
				{
					//store the new world matrix locally
					worldMatrix = value;

					// multiply the view and world matrices, and convert it to GL format
					float[] glMatrix = MakeGLMatrix(viewMatrix * worldMatrix);

					// change the matrix mode to ModelView
					Gl.glMatrixMode(Gl.GL_MODELVIEW);

					// load the converted GL matrix
					Gl.glLoadMatrixf(glMatrix);
				}
			}

			/// <summary>
			/// 
			/// </summary>
			/// <param name="light"></param>
			protected override void AddLight(Light light)
			{
				int lightIndex;

				// look for a free slot and add the light
				for(lightIndex = 0; lightIndex < caps.MaxLights; lightIndex++)
				{
					if(lights[lightIndex] == null)
					{
						lights[lightIndex] = light;
						break;
					}
				}

				if(lightIndex == caps.MaxLights)
					throw new Exception("Maximum hardware light count has been reached.");

				// update light
				SetGLLight(lightIndex, light);			
			}
	
			/// <summary>
			/// 
			/// </summary>
			/// <param name="light"></param>
			protected override void UpdateLight(Light light)
			{
				int lightIndex;

				for(lightIndex = 0; lightIndex < caps.MaxLights; lightIndex++)
				{
					if(lights[lightIndex].Name == light.Name)
						break;
				}

				if(lightIndex == caps.MaxLights)
					throw new Exception("An attempt was made to update an invalid light.");

				// update light
				SetGLLight(lightIndex, light);
			}

			/// <summary>
			/// 
			/// </summary>
			/// <param name="color"></param>
			/// <returns></returns>
			public override int ConvertColor(ColorEx color)
			{
				return color.ToABGR();
			}

			/// <summary>
			/// 
			/// </summary>
			/// <param name="src"></param>
			/// <param name="dest"></param>
			protected override void SetSceneBlending(SceneBlendFactor src, SceneBlendFactor dest)
			{
				int srcFactor = ConvertBlendFactor(src);
				int destFactor = ConvertBlendFactor(dest);

				// enable blending and set the blend function
				Gl.glEnable(Gl.GL_BLEND);
				Gl.glBlendFunc(srcFactor, destFactor);
			}

			/// <summary>
			/// 
			/// </summary>
			protected override ushort DepthBias
			{
				set
				{
					ushort bias = value;

					if (bias > 0)
					{
						Gl.glEnable(Gl.GL_POLYGON_OFFSET_FILL);
						Gl.glEnable(Gl.GL_POLYGON_OFFSET_POINT);
						Gl.glEnable(Gl.GL_POLYGON_OFFSET_LINE);
						// Bias is in {0, 16}, scale the unit addition appropriately
						Gl.glPolygonOffset(1.0f, bias);
					}
					else
					{
						Gl.glDisable(Gl.GL_POLYGON_OFFSET_FILL);
						Gl.glDisable(Gl.GL_POLYGON_OFFSET_POINT);
						Gl.glDisable(Gl.GL_POLYGON_OFFSET_LINE);
					}
				}
			}

			/// <summary>
			/// 
			/// </summary>
			protected override bool DepthCheck
			{
				set
				{
					if(value)
					{
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
			protected override CompareFunction DepthFunction
			{
				set
				{
					Gl.glDepthFunc(GLHelper.ConvertEnum(value));
				}
			}

			/// <summary>
			/// 
			/// </summary>
			protected override bool DepthWrite
			{
				set
				{
					byte flag = value ? (byte)Gl.GL_TRUE : (byte)Gl.GL_FALSE;
					Gl.glDepthMask( flag );  

					// Store for reference in BeginFrame
					depthWrite = value;
				}
			}

			#region Private methods

			/// <summary>
			///		Private method to convert our blend factors to that of Open GL
			/// </summary>
			/// <param name="factor"></param>
			/// <returns></returns>
			private int ConvertBlendFactor(SceneBlendFactor factor)
			{
				int glFactor = 0;

				switch(factor)
				{
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
			private float[] MakeGLMatrix(Matrix4 matrix)
			{
				Matrix4 mat = matrix.Transpose();

				return mat.MakeFloatArray();
			}

			/// <summary>
			///		
			/// </summary>
			/// <param name="color"></param>
			/// <returns></returns>
			private float[] GlColorArray(ColorEx color)
			{
				return new float[] {color.r, color.g, color.b, color.a};
			}

			/// <summary>
			/// 
			/// </summary>
			/// <param name="index"></param>
			/// <param name="light"></param>
			private void SetGLLight(int index, Light light)
			{
				int lightIndex = Gl.GL_LIGHT0 + index;

				if(index == 1)
					lightIndex = Gl.GL_LIGHT1;

				if(light.IsVisible)
				{
					// set spotlight cutoff
					switch(light.Type)
					{
						case LightType.Spotlight:
							Gl.glLightf(lightIndex, Gl.GL_SPOT_CUTOFF, light.SpotlightOuterAngle);
							break;
						default:
							Gl.glLightf(lightIndex, Gl.GL_SPOT_CUTOFF, 180.0f);
							break;
					}

					// light color
					float[] color = GlColorArray(light.Diffuse);
					Gl.glLightfv(lightIndex, Gl.GL_DIFFUSE, color);

					// specular color
					float[] specular = GlColorArray(light.Specular);
					Gl.glLightfv(lightIndex, Gl.GL_SPECULAR, specular);

					// disable ambient light for objects
					// BUG: Why does this return GL ERROR 1280?
					//Gl.glLighti(lightIndex, 0x1200/*GL_AMBIENT*/, 0);

					// position (not set for Directional lighting)
					Vector3 vec;
					float[] vals = new float[4];

					if(light.Type != LightType.Directional)
					{
						vec = light.DerivedPosition;
						vals[0] = vec.x;
						vals[1] = vec.y;
						vals[2] = vec.z;
						vals[3] = 1.0f;

						Gl.glLightfv(lightIndex, Gl.GL_POSITION, vals);
					}
				
					// direction (not needed for point lights
					if(light.Type != LightType.Point)
					{
						vec = light.DerivedDirection;
						vals[0] = vec.x;
						vals[1] = vec.y;
						vals[2] = vec.z;
						vals[3] = 1.0f;

						Gl.glLightfv(lightIndex, Gl.GL_SPOT_DIRECTION, vals);
					}

					// light attenuation
					Gl.glLightf(lightIndex, Gl.GL_CONSTANT_ATTENUATION, light.AttenuationConstant);
					Gl.glLightf(lightIndex, Gl.GL_LINEAR_ATTENUATION, light.AttenuationLinear);
					Gl.glLightf(lightIndex, Gl.GL_QUADRATIC_ATTENUATION, light.AttenuationQuadratic);

					// enable the light
					Gl.glEnable(lightIndex);
				}
				else
				{
					// disable the light if it is not visible
					Gl.glDisable(lightIndex);
				}
			}

			/// <summary>
			///		Called in constructor to init configuration.
			/// </summary>
			private void InitConfigOptions()
			{
				ScreenSetting[] settings = ScreenSetting.CompatibleDisplay;

				for(int i = 0; i < settings.Length; i++)
				{
					ScreenSetting setting = settings[i];
				
					// filter out the lower resolutions
					if(setting.Width >= 640 && setting.Height >= 480)
					{
						// add a new row to the display settings table
						engineConfig.DisplayMode.AddDisplayModeRow(setting.Width, setting.Height, setting.CDepth, false, false);
					}
				}
			}

			private void ResetLights()
			{
				float[] f4vals = new float[4];

				for (int i = 0; i < caps.MaxLights; i++)
				{
					if (lights[i] != null)
					{
						Light lt = lights[i];
						// Position (don't set for directional)
						Vector3 vec = new Vector3();

						if (lt.Type != LightType.Directional)
						{
							vec = lt.DerivedPosition;
							f4vals[0] = vec.x;
							f4vals[1] = vec.y;
							f4vals[2] = vec.z;
							f4vals[3] = 1.0f;
							Gl.glLightfv(Gl.GL_LIGHT0 + i, Gl.GL_POSITION, f4vals);
						}
						// Direction (not needed for point lights)
						if (lt.Type != LightType.Point)
						{
							vec = lt.DerivedDirection;
							f4vals[0] = vec.x;
							f4vals[1] = vec.y;
							f4vals[2] = vec.z;
							f4vals[3] = 0.0f;
							Gl.glLightfv(Gl.GL_LIGHT0 + i, Gl.GL_SPOT_DIRECTION, f4vals);
						}
					}
				}
			}

			#endregion
		
			/// <summary>
			///		Helper method to go through and interrogate hardware capabilities.
			/// </summary>
			private void CheckCaps()
			{
				// check multitexturing
				if(GLHelper.SupportsExtension("GL_ARB_multitexture"))
					caps.SetCap(Capabilities.MultiTexturing);

				// check texture blending
				if(GLHelper.SupportsExtension("GL_EXT_texture_env_combine") || GLHelper.SupportsExtension("GL_ARB_texture_env_combine"))
					caps.SetCap(Capabilities.TextureBlending);

				// check dot3 support
				if(GLHelper.SupportsExtension("GL_ARB_texture_env_dot3"))
					caps.SetCap(Capabilities.Dot3Bump);

				// check the number of texture units available
				int numTextureUnits = 0;
				Gl.glGetIntegerv(Gl.GL_MAX_TEXTURE_UNITS, out numTextureUnits);
				caps.NumTextureUnits = numTextureUnits;

				// check support for vertex buffers in hardware
				if(GLHelper.SupportsExtension("GL_ARB_vertex_buffer_object"))
					caps.SetCap(Capabilities.VertexBuffer);

				// check support for hardware vertex blending
				if(GLHelper.SupportsExtension("GL_ARB_vertex_blend"))
					caps.SetCap(Capabilities.VertexBlending);

				// check if the hardware supports anisotropic filtering
				if(GLHelper.SupportsExtension("GL_EXT_texture_filter_anisotropic"))
					caps.SetCap(Capabilities.AnisotropicFiltering);

				// check hardware mip mapping
				// TODO: Only enable this for non-ATI cards temporarily until drivers are fixed
				if(GLHelper.Vendor != "ATI" && GLHelper.SupportsExtension("GL_SGIS_generate_mipmap"))
					caps.SetCap(Capabilities.HardwareMipMaps);

				// find out how many lights we have to play with, then create a light array to keep locally
				int maxLights;
				Gl.glGetIntegerv(Gl.GL_MAX_LIGHTS, out maxLights);
				caps.MaxLights = maxLights;
				lights = new Light[caps.MaxLights];

				// check stencil buffer depth availability
				int stencilBits;
				Gl.glGetIntegerv(Gl.GL_STENCIL_BITS, out stencilBits);
				caps.StencilBufferBits = stencilBits;

				// if stencil bits are available, enable stencil buffering
				if(stencilBits > 0)
					caps.SetCap(Capabilities.StencilBuffer);
			}

			/// <summary>
			///		Convenience method for VBOs
			/// </summary>
			/// <param name="i"></param>
			/// <returns></returns>
			private IntPtr BUFFER_OFFSET(int i)
			{
				return new IntPtr(i);
			}

			#region Implementation of IPlugin

			public void Start()
			{
				// add an instance of this plugin to the list of available RenderSystems
				Engine.Instance.RenderSystems.Add("OpenGL", this);
			}

			public void Stop()
			{
			}

			#endregion
		}
	}
