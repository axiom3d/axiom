#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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

namespace RenderSystem_OpenGL
{
	/// <summary>
	/// Summary description for OpenGLRenderer.
	/// </summary>
	[SubSystem(SubSystems.Rendering)]
	public class OpenGLRenderer : RenderSystem, IPlugin
	{
		/// <summary>OpenGL Context (from CsGL)</summary>
		protected OpenGLContext context;
		/// <summary>Object that allows for calls to OpenGL extensions.  Named all upper for consistency since GL calls are static through GL class.</summary>
		protected OpenGL_Extension EXT;

		/// <summary>Internal view matrix.</summary>
		protected Matrix4 viewMatrix;
		/// <summary>Internal world matrix.</summary>
		protected Matrix4 worldMatrix;
		/// <summary>Internal texture matrix.</summary>
		protected Matrix4 textureMatrix;

		// used for manual texture matrix calculations, for things like env mapping
		protected bool useAutoTextureMatrix;
		protected float[] autoTextureMatrix = new float[16];

		// OpenGL supports 8 lights max
		const int MAX_LIGHTS = 8;
		const string OPENGL_LIB = "opengl32.dll";
		const string GLU_LIB = "glu32.dll";
		protected Light[] lights = new Light[MAX_LIGHTS];

		public OpenGLRenderer()
		{
			viewMatrix = Matrix4.Identity;
			worldMatrix = Matrix4.Identity;
			textureMatrix = Matrix4.Identity;

			// create a new OpenGLExtensions object
			EXT = new OpenGLExtensions();

			InitConfigOptions();
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

				// log hardware info
				System.Diagnostics.Trace.WriteLine(String.Format("Vendor: {0}", glGetString(GL_VENDOR)));
				System.Diagnostics.Trace.WriteLine(String.Format("Video Board: {0}", glGetString(GL_RENDERER)));
				System.Diagnostics.Trace.WriteLine(String.Format("Version: {0}", glGetString(GL_VERSION)));
				
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
				glShadeModel(GL_SMOOTH);							// Enable Smooth Shading
				glClearColor(0.0f, 0.0f, 0.0f, 0.5f);				// Black Background
				glClearDepth(1.0f);									// Depth Buffer Setup
				glEnable(GL_DEPTH_TEST);							// Enables Depth Testing
				glDepthFunc(GL_LEQUAL);								// The Type Of Depth Testing To Do
				glHint(GL_PERSPECTIVE_CORRECTION_HINT, GL_NICEST);	// Really Nice Perspective Calculations
			}

			// create the window
			window.Create(name, target, width, height, colorDepth, isFullscreen, left, top, depthBuffer, context);

			// add the new window to the RenderWindow collection
			this.renderWindows.Add(window);

			// by creating our texture manager, singleton TextureManager will hold our implementation
			textureMgr = new GLTextureManager();

			// create a specialized instance, which registers itself as the singleton instance of HardwareBufferManager
			hardwareBufferManager = new GLHardwareBufferManager();

			return window;
		}
	
		public override ColorEx AmbientLight
		{
			set
			{
				// create a float[4]  to contain the RGBA data
				float[] ambient = GLColorArray(value);
				ambient[3] = 0.0f;

				// set the ambient color
				glLightModelfv(GL_LIGHT_MODEL_AMBIENT, ambient);
			}
		}
	
		public override bool LightingEnabled
		{
			set
			{
				if(value)
					glEnable(GL_LIGHTING);
				else
					glDisable(GL_LIGHTING);
			}
		}

		protected override SceneDetailLevel RasterizationMode
		{
			set
			{
				// default to fill to make compiler happy
				uint mode = GL_FILL;

				switch(value)
				{
					case SceneDetailLevel.Solid:
						mode = GL_FILL;
						break;
					case SceneDetailLevel.Points:
						mode = GL_POINT;
						break;
					case SceneDetailLevel.Wireframe:
						mode = GL_LINE;
						break;
					default:
						// if all else fails, just use fill
						mode = GL_FILL;
						break;
				}

				// set the specified polygon mode
				glPolygonMode(GL_FRONT_AND_BACK, mode);
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
						glShadeModel(GL_FLAT);
						break;
					default:
						glShadeModel(GL_SMOOTH);
						break;
				}
			}
		}

		public override short StencilBufferBitDepth
		{
			get
			{
				// TODO:  Add OpenGLRenderer.StencilBufferBitDepth getter implementation
				return 0;
			}
		}

		public override StencilOperation StencilBufferDepthFailOperation
		{
			set
			{
				// TODO:  Add OpenGLRenderer.StencilBufferDepthFailOperation setter implementation
			}
		}

		public override StencilOperation StencilBufferFailOperation
		{
			set
			{
				// TODO:  Add OpenGLRenderer.StencilBufferFailOperation setter implementation
			}
		}

		public override CompareFunction StencilBufferFunction
		{
			set
			{
				// TODO:  Add OpenGLRenderer.StencilBufferFunction setter implementation
			}
		}

		public override long StencilBufferMask
		{
			set
			{
				// TODO:  Add OpenGLRenderer.StencilBufferMask setter implementation
			}
		}

		public override StencilOperation StencilBufferPassOperation
		{
			set
			{
				// TODO:  Add OpenGLRenderer.StencilBufferPassOperation setter implementation
			}
		}

		public override long StencilBufferReferenceValue
		{
			set
			{
				// TODO:  Add OpenGLRenderer.StencilBufferReferenceValue setter implementation
			}
		}

		public override bool StencilCheckEnabled
		{
			set
			{
				// TODO:  Add OpenGLRenderer.StencilCheckEnabled setter implementation
			}
		}

		public override TextureFiltering TextureFiltering
		{
			set
			{
				int numUnits = caps.NumTextureUnits;

				// set for all texture units
				for(uint unit = 0; unit < numUnits; unit++)
				{
					EXT.glActiveTextureARB(GL_TEXTURE0 + unit);

					switch(value)
					{
						case Axiom.SubSystems.Rendering.TextureFiltering.Trilinear:
						{
							glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
							glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR_MIPMAP_LINEAR);

						} break;

						case Axiom.SubSystems.Rendering.TextureFiltering.Bilinear:
						{
							glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
							glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR_MIPMAP_NEAREST);

						} break;

						case Axiom.SubSystems.Rendering.TextureFiltering.None:
						{
							glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_NEAREST);
							glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_NEAREST);

						} break;
					} // switch
				} // for

				// reset texture unit
				EXT.glActiveTextureARB( GL_TEXTURE0 );
			}
		}

		public override Axiom.MathLib.Matrix4 MakeProjectionMatrix(float fov, float aspectRatio, float near, float far)
		{
			Matrix4 matrix = new Matrix4();

			float thetaY = MathUtil.DegreesToRadians(fov * 0.5f);
			float tanThetaY = MathUtil.Tan(thetaY);

			float w = (1.0f / tanThetaY) / aspectRatio;
			float h = 1.0f / tanThetaY;
			float q = -(far + near) / (far - near);
			float qn = -2 * (far * near) / (far - near);

			matrix[0,0] = w;
			matrix[1,1] = h;
			matrix[2,2] = q;
			matrix[2,3] = qn;
			matrix[3,2] = -1.0f;

			return matrix;
		}

		protected override void BeginFrame()
		{
			Debug.Assert(activeViewport != null, "BeingFrame cannot run without an active viewport.");

			if(activeViewport.ClearEveryFrame)
			{
				float[] color = GLColorArray(activeViewport.BackgroundColor);

				// clear the viewport
				glClearColor(color[0], color[1], color[2], color[3]);

				// disable depth write if it isnt
				if(!depthWrite)
					glDepthMask((byte)GL_TRUE);

				// clear the color buffer and depth buffer bits
				glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);	

				// Reset depth write state if appropriate
				// Enable depth buffer for writing if it isn't
				if(!depthWrite)
					glDepthMask((byte)GL_FALSE);
			}

			// Reset all lights
			ResetLights();
		}

		protected override void EndFrame()
		{
			// TODO: See if we should do something here.
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
				glEnable(GL_SCISSOR_TEST);

				// set the current GL viewport
				glViewport(x, y, width, height);

				// set the scissor area for the viewport
				glScissor(x, y, width, height);

				// clear the updated flag
				viewport.IsUpdated = false;
			}
		}
		#endregion

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

		protected override void SetTexture(int stage, bool enabled, string textureName)
		{
			// load the texture
			GLTexture texture = (GLTexture)TextureManager.Instance[textureName];

			// set the active texture
			EXT.glActiveTextureARB(GL_TEXTURE0 + (uint)stage);

			// enable and bind the texture if necessary
			if(enabled && texture != null)
			{
				glEnable(GL_TEXTURE_2D);
				glBindTexture(GL_TEXTURE_2D, texture.TextureID);
			}
			else
			{
				glDisable(GL_TEXTURE_2D);
				glTexEnvf(GL_TEXTURE_ENV, GL_TEXTURE_ENV_MODE, GL_MODULATE);
			}

			// reset active texture to unit 0
			EXT.glActiveTextureARB(GL_TEXTURE0);
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
					fogMode = GL_EXP;
					break;
				case FogMode.Exp2:
					fogMode = GL_EXP2;
					break;
				case FogMode.Linear:
					fogMode = GL_LINEAR;
					break;
				default:
					glDisable(GL_FOG);
					return;
			} // switch

			glEnable(GL_FOG);
			glFogi(GL_FOG_MODE, (int)fogMode);
			float[] fogColor = GLColorArray(color);
			glFogfv(GL_FOG_COLOR, fogColor);
			glFogf(GL_FOG_DENSITY, density);
			glFogf(GL_FOG_START, start);
			glFogf(GL_FOG_END, end);

			// TODO: Fog hints maybe?
		}

		public override void Render(RenderOperation op)
		{
			// call base class method first
			base.Render (op);
	
			IList elements = op.vertexData.vertexDeclaration.Elements;
		
			// loop through and handle each element
			for(int i = 0; i < elements.Count; i++)
			{
				// get a reference to the current object in the collection
				VertexElement element = (VertexElement)elements[i];

				// get the current vertex buffer
				GLHardwareVertexBuffer vertexBuffer = 
					(GLHardwareVertexBuffer)op.vertexData.vertexBufferBinding.GetBuffer(element.Source);

				// bind the current vertex buffer
				EXT.glBindBufferARB(OpenGLExtensions.GL_ARRAY_BUFFER_ARB, vertexBuffer.GLBufferID);

				// get the type of this buffer
				uint type = GLHelper.ConvertEnum(element.Type);

				unsafe
				{
					// set pointer usage based on the use of this buffer
					switch(element.Semantic)
					{
						case VertexElementSemantic.Position:
							// set the pointer data
							glVertexPointer(
								VertexElement.GetTypeCount(element.Type),
								type,
								vertexBuffer.VertexSize,
								BUFFER_OFFSET(element.Offset));

							// enable the vertex array client state
							glEnableClientState(GL_VERTEX_ARRAY);

							break;
					
						case VertexElementSemantic.Normal:
							// set the pointer data
							glNormalPointer(
								type, 
								vertexBuffer.VertexSize,
								BUFFER_OFFSET(element.Offset));

							// enable the normal array client state
							glEnableClientState(GL_NORMAL_ARRAY);

							break;
					
						case VertexElementSemantic.Diffuse:
							// set the pointer data
							glColorPointer(
								4,
								type, 
								vertexBuffer.VertexSize,
								BUFFER_OFFSET(element.Offset));

							// enable the normal array client state
							glEnableClientState(GL_COLOR_ARRAY);

							break;
					
						case VertexElementSemantic.Specular:
							// TODO: Add glSecondaryColorPointer to CsGL
							break;

						case VertexElementSemantic.TexCoords:
							// this ignores vertex element index and sets tex array for each available texture unit
							// this allows for multitexturing on entities whose mesh only has a single set of tex coords
 
							for(uint j = 0; j < caps.NumTextureUnits; j++)
							{
								// set the current active texture unit
								EXT.glClientActiveTextureARB(GL_TEXTURE0 + j);

								if(glIsEnabled(GL_TEXTURE_2D) > 0)
								{
									// set the tex coord pointer
									glTexCoordPointer(
										VertexElement.GetTypeCount(element.Type),
										type,
										vertexBuffer.VertexSize,
										BUFFER_OFFSET(element.Offset));
								}

								// enable texture coord state
								glEnableClientState(GL_TEXTURE_COORD_ARRAY);
							}
							break;

						default:
							break;
					} // switch
				} // unsafe
			} // for

			// reset to texture unit 0
			EXT.glClientActiveTextureARB(GL_TEXTURE0);

			uint primType = 0;

			// which type of render operation is this?
			switch(op.operationType)
			{
				case RenderMode.PointList:
					primType = GL_POINTS;
					break;
				case RenderMode.LineList:
					primType = GL_LINES;
					break;
				case RenderMode.LineStrip:
					primType = GL_LINE_STRIP;
					break;
				case RenderMode.TriangleList:
					primType = GL_TRIANGLES;
					break;
				case RenderMode.TriangleStrip:
					primType = GL_TRIANGLE_STRIP;
					break;
				case RenderMode.TriangleFan:
					primType = GL_TRIANGLE_FAN;
					break;
			}

			unsafe
			{
				if(op.useIndices)
				{
					uint idxBufferID = ((GLHardwareIndexBuffer)op.indexData.indexBuffer).GLBufferID;

					EXT.glBindBufferARB(OpenGLExtensions.GL_ELEMENT_ARRAY_BUFFER_ARB, idxBufferID);

					glDrawElements(primType, op.indexData.indexCount, GL_UNSIGNED_SHORT, BUFFER_OFFSET(0));
				}
				else
				{
					glDrawArrays(primType, 0, op.vertexData.vertexCount);
				}
			}

			// disable all client states
			glDisableClientState( GL_VERTEX_ARRAY );
			glDisableClientState( GL_TEXTURE_COORD_ARRAY );
			glDisableClientState( GL_NORMAL_ARRAY );
			glDisableClientState( GL_COLOR_ARRAY );
			//glDisableClientState( GL_SECONDARY_COLOR_ARRAY );
			glColor4f(1.0f,1.0f,1.0f,1.0f);
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
				glMatrixMode(GL_PROJECTION);

				// load the float array into the projection matrix
				glLoadMatrixf(glMatrix);

				// set the matrix mode back to ModelView
				glMatrixMode(GL_MODELVIEW);
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
				glMatrixMode(GL_MODELVIEW);
				
				// load the float array into the ModelView matrix
				glLoadMatrixf(glMatrix);

				// Reset lights here after a view change
				ResetLights();

				// convert the internal world matrix
				glMatrix = MakeGLMatrix(worldMatrix);

				// multply the world matrix by the current ModelView matrix
				glMultMatrixf(glMatrix);
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
				glMatrixMode(GL_MODELVIEW);

				// load the converted GL matrix
				glLoadMatrixf(glMatrix);
			}
		}

		protected override void AddLight(Light light)
		{
			int lightIndex;

			// look for a free slot and add the light
			for(lightIndex = 0; lightIndex < MAX_LIGHTS; lightIndex++)
			{
				if(lights[lightIndex] == null)
				{
					lights[lightIndex] = light;
					break;
				}
			}

			if(lightIndex == MAX_LIGHTS)
				throw new Exception("Maximum hardware light count has been reached.");

			// update light
			SetGLLight(lightIndex, light);			
		}
	
		protected override void UpdateLight(Light light)
		{
			int lightIndex;

			for(lightIndex = 0; lightIndex < MAX_LIGHTS; lightIndex++)
			{
				if(lights[lightIndex].Name == light.Name)
					break;
			}

			if(lightIndex == MAX_LIGHTS)
				throw new Exception("An attempt was made to update an invalid light.");

			// update light
			SetGLLight(lightIndex, light);
		}


		public override int ConvertColor(ColorEx color)
		{
			return color.ToABGR();
		}

		protected override void SetSceneBlending(SceneBlendFactor src, SceneBlendFactor dest)
		{
			uint srcFactor = ConvertBlendFactor(src);
			uint destFactor = ConvertBlendFactor(dest);

			// enable blending and set the blend function
			glEnable(GL_BLEND);
			glBlendFunc(srcFactor, destFactor);
		}

		/// <summary>
		/// 
		/// </summary>
		protected override ushort DepthBias
		{
			set
			{
			}
		}

		/// <summary>
		/// 
		/// </summary>
		protected override bool DepthCheck
		{
			set
			{
			}
		}

		/// <summary>
		/// 
		/// </summary>
		protected override bool DepthFunction
		{
			set
			{
			}
		}

		/// <summary>
		/// 
		/// </summary>
		protected override bool DepthWrite
		{
			set
			{
				byte flag = value ? (byte)GL_TRUE : (byte)GL_FALSE;
				glDepthMask( flag );  

				// Store for reference in BeginFrame
				depthWrite = value;
			}
		}

		protected override VertexBufferBinding VertexBufferBinding
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		protected override VertexDeclaration VertexDeclaration
		{
			get
			{
				return null;
			}
			set
			{
			}
		}


		#region Private methods

		/// <summary>
		///		Private method to convert our blend factors to that of Open GL
		/// </summary>
		/// <param name="factor"></param>
		/// <returns></returns>
		private uint ConvertBlendFactor(SceneBlendFactor factor)
		{
			uint glFactor = 0;

			switch(factor)
			{
				case SceneBlendFactor.One:
					glFactor =  GL_ONE;
					break;
				case SceneBlendFactor.Zero:
					glFactor =  GL_ZERO;
					break;
				case SceneBlendFactor.DestColor:
					glFactor =  GL_DST_COLOR;
					break;
				case SceneBlendFactor.SourceColor:
					glFactor =  GL_SRC_COLOR;
					break;
				case SceneBlendFactor.OneMinusDestColor:
					glFactor =  GL_ONE_MINUS_DST_COLOR;
					break;
				case SceneBlendFactor.OneMinusSourceColor:
					glFactor =  GL_ONE_MINUS_SRC_COLOR;
					break;
				case SceneBlendFactor.DestAlpha:
					glFactor =  GL_DST_ALPHA;
					break;
				case SceneBlendFactor.SourceAlpha:
					glFactor =  GL_SRC_ALPHA;
					break;
				case SceneBlendFactor.OneMinusDestAlpha:
					glFactor =  GL_ONE_MINUS_DST_ALPHA;
					break;
				case SceneBlendFactor.OneMinusSourceAlpha:
					glFactor =  GL_ONE_MINUS_SRC_ALPHA;
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
		private float[] GLColorArray(ColorEx color)
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
			uint lightIndex = GL_LIGHT0 + (uint)index;

			if(index == 1)
				lightIndex = GL_LIGHT1;

			if(light.IsVisible)
			{
				// set spotlight cutoff
				switch(light.Type)
				{
					case LightType.Spotlight:
						glLightf(lightIndex, GL_SPOT_CUTOFF, light.SpotlightOuterAngle);
						break;
					default:
						glLightf(lightIndex, GL_SPOT_CUTOFF, 180.0f);
						break;
				}

				// light color
				float[] color = GLColorArray(light.Diffuse);
				glLightfv(lightIndex, GL_DIFFUSE, color);

				// specular color
				float[] specular = GLColorArray(light.Specular);
				glLightfv(lightIndex, GL_SPECULAR, specular);

				// disable ambient light for objects
				// BUG: Why does this return GL ERROR 1280?
				//glLighti(lightIndex, 0x1200/*GL_AMBIENT*/, 0);

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

					glLightfv(lightIndex, GL_POSITION, vals);
				}
				
				// direction (not needed for point lights
				if(light.Type != LightType.Point)
				{
					vec = light.DerivedDirection;
					vals[0] = vec.x;
					vals[1] = vec.y;
					vals[2] = vec.z;
					vals[3] = 1.0f;

					glLightfv(lightIndex, GL_SPOT_DIRECTION, vals);
				}

				// light attenuation
				glLightf(lightIndex, GL_CONSTANT_ATTENUATION, light.AttenuationConstant);
				glLightf(lightIndex, GL_LINEAR_ATTENUATION, light.AttenuationLinear);
				glLightf(lightIndex, GL_QUADRATIC_ATTENUATION, light.AttenuationQuadratic);

				// enable the light
				glEnable(lightIndex);
			}
			else
			{
				// disable the light if it is not visible
				glDisable(lightIndex);
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

			for (int i = 0; i < MAX_LIGHTS; i++)
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
						glLightfv(GL_LIGHT0 + (uint)i, GL_POSITION, f4vals);
					}
					// Direction (not needed for point lights)
					if (lt.Type != LightType.Point)
					{
						vec = lt.DerivedDirection;
						f4vals[0] = vec.x;
						f4vals[1] = vec.y;
						f4vals[2] = vec.z;
						f4vals[3] = 0.0f;
						glLightfv(GL_LIGHT0 + (uint)i, GL_SPOT_DIRECTION, f4vals);
					}
				}
			}
		}

		#endregion

		#region OpenGL Imports

		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexImage1D(uint target, int level, int components, int width, int border, uint format, uint type, IntPtr pixels);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexImage1D(uint target, int level, int components, int width, int border, uint format, uint type, float[] pixels);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexImage1D(uint target, int level, int components, int width, int border, uint format, uint type, int[] pixels);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexImage1D(uint target, int level, int components, int width, int border, uint format, uint type, byte[] pixels);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexImage1D(uint target, int level, int components, int width, int border, uint format, uint type, uint[] pixels);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexImage1D(uint target, int level, int components, int width, int border, uint format, uint type, float[,] pixels);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexImage2D(uint target, int level, int components, int width, int height, int border, uint format, uint type, IntPtr pixels);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glGetTexImage(uint target, int level, uint format, uint type, IntPtr pixels);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexSubImage1D(uint target, int level, int xoffset, int width, uint format, uint type, IntPtr pixels);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexSubImage2D(uint target, int level, int xoffset, int yoffset, int width, int height, uint format, uint type, IntPtr pixels);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexImage2D(uint target, int level, int components, int width, int height, int border, uint format, uint type, byte[] pixels);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glGetTexImage(uint target, int level, uint format, uint type, byte[] pixels);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexSubImage1D(uint target, int level, int xoffset, int width, uint format, uint type, byte[] pixels);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexSubImage2D(uint target, int level, int xoffset, int yoffset, int width, int height, uint format, uint type, byte[] pixels);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexSubImage2D(uint target, int level, int xoffset, int yoffset, int width, int height, uint format, uint type, uint[] pixels);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexSubImage2D(uint target, int level, int xoffset, int yoffset, int width, int height, uint format, uint type, byte[,,] pixels);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexImage2D(uint target, int level, int components, int width, int height, int border, uint format, uint type, uint[] pixels);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glGetTexImage(uint target, int level, uint format, uint type, uint[] pixels);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexSubImage1D(uint target, int level, int xoffset, int width, uint format, uint type, uint[] pixels);

		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glBitmap(int width, int height, float xorig, float yorig, float xmove, float ymove, byte[] bitmap);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glBitmap(int width, int height, float xorig, float yorig, float xmove, float ymove, IntPtr bitmap);
		
		///////////////////////////////////////////////////////////////////////////////////

		// sign of const solution
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexParameteri(uint target, uint pname, uint param);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glGetTexLevelParameterfv(uint target, int level, uint pname, float[] someParams);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glGetTexLevelParameteriv(uint target, int level, uint pname, int[] someParams);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glGetTexParameterfv(uint target, uint pname, float[] someParams);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glGetTexParameteriv(uint target, uint pname, int[] someParams);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glGetTexLevelParameterfv(uint target, int level, uint pname, out float someParams);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glGetTexLevelParameteriv(uint target, int level, uint pname, out int someParams);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glGetTexParameterfv(uint target, uint pname, out float someParams);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glGetTexParameteriv(uint target, uint pname, out int someParams);
	
		// pointer solution
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glCallLists(int n, uint type, byte[] lists);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glCallLists(int n, uint type, sbyte[] lists);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glCallLists(int n, uint type, short[] lists);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glCallLists(int n, uint type, ushort[] lists);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glCallLists(int n, uint type, int[] lists);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glCallLists(int n, uint type, uint[] lists);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glCallLists(int n, uint type, float[] lists);
		[DllImport(OPENGL_LIB, CharSet=CharSet.Unicode)]
		public static extern void glCallLists(int n, uint type, char[] lists);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glCallLists(int n, uint type, [MarshalAs(UnmanagedType.LPWStr)] string lists);
   
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glRasterPos2dv(double[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glRasterPos2fv(float[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glRasterPos2iv(int[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glRasterPos2sv(short[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glRasterPos3dv(double[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glRasterPos3fv(float[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glRasterPos3iv(int[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glRasterPos3sv(short[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glRasterPos4dv(double[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glRasterPos4fv(float[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glRasterPos4iv(int[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glRasterPos4sv(short[] v);
		
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glReadPixels(int x, int y, int width, int height, uint format, uint type, IntPtr pixels);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glReadPixels(int x, int y, int width, int height, uint format, uint type, byte[] pixels);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glReadPixels(int x, int y, int width, int height, uint format, uint type, ushort[] pixels);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glReadPixels(int x, int y, int width, int height, uint format, uint type, uint[] pixels);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glDrawPixels(int width, int height, uint format, uint type, IntPtr pixels);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glDrawPixels(int width, int height, uint format, uint type, byte[] pixels);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glDrawPixels(int width, int height, uint format, uint type, ushort[] pixels);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glDrawPixels(int width, int height, uint format, uint type, uint[] pixels);
		
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glGenTextures(int n, uint[] textures);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glDeleteTextures(int n,  uint[] textures);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glNormal3bv(sbyte[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glNormal3dv(double[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glNormal3fv(float[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glNormal3iv(int[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glNormal3sv(short[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glVertex2dv(double[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glVertex2fv(float[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glVertex2iv(int[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glVertex2sv(short[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glVertex3dv(double[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glVertex3fv(float[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glVertex3iv(int[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glVertex3sv(short[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glVertex4dv(double[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glVertex4fv(float[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glVertex4iv(int[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glVertex4sv(short[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glLightModelfv(uint pname, float[] someParams);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glLightModeliv(uint pname, int[] someParams);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glLightfv(uint light, uint pname, float[] someParams);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glLightiv(uint light, uint pname, int[] someParams);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glColor3bv(sbyte[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glColor3dv(double[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glColor3fv(float[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glColor3iv(int[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glColor3sv(short[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glColor3ubv(byte[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glColor3uiv(uint[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glColor3usv(ushort[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glColor4bv(sbyte[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glColor4dv(double[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glColor4fv(float[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glColor4iv(int[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glColor4sv(short[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glColor4ubv(byte[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glColor4uiv(uint[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glColor4usv(ushort[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glGetBooleanv(uint pname, byte[] someParams );
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glGetDoublev(uint pname, double[] someParams);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glGetFloatv(uint pname, float[] someParams);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glGetIntegerv(uint pname, int[] someParams);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glGetIntegerv(uint pname, uint[] someParams);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glGetBooleanv(uint pname, out byte someParam);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glGetDoublev(uint pname, out double someParam);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glGetFloatv(uint pname, out float someParam);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glGetIntegerv(uint pname, out int someParam);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glGetIntegerv(uint pname, out uint someParam);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glLoadMatrixd(double[] m);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glLoadMatrixf(float[] m);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glMultMatrixd(double[] m);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glMultMatrixf(float[] m);

		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glEdgeFlagv(ref byte flag);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glEdgeFlagv(byte[] flag);
		
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glMaterialfv(uint face, uint pname,  ref float someParams);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glMaterialfv(uint face, uint pname,  float[] someParams);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glMaterialfv(uint face, uint pname,  IntPtr someParams);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glMaterialiv(uint face, uint pname,  ref int someParams);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glMaterialiv(uint face, uint pname,  int[] someParams);
		
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexCoord1dv(double[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexCoord1fv(float[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexCoord1iv(int[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexCoord1sv(short[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexCoord2dv(double[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexCoord2fv(float[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexCoord2iv(int[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexCoord2sv(short[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexCoord3dv(double[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexCoord3fv(float[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexCoord3iv(int[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexCoord3sv(short[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexCoord4dv(double[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexCoord4fv(float[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexCoord4iv(int[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexCoord4sv(short[] v);
		
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern string glGetString(uint name);
		
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glSelectBuffer(int size, uint[] buffer);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glSelectBuffer(int size, IntPtr buffer);
		
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glFeedbackBuffer(int size, uint type, float[] buffer);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glFeedbackBuffer(int size, uint type, IntPtr buffer);
		
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern byte glAreTexturesResident(int n, out uint textures, out byte residences);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern byte glAreTexturesResident(int n, uint[] textures, byte[] residences);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glPrioritizeTextures(int n, uint[] textures, float[] priorities);
		
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glClipPlane(uint plane, double[] equation);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glGetClipPlane(uint plane, double[] equation);
		
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glEvalCoord2dv(double[] u);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glEvalCoord2fv(float[] u);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glEvalCoord1dv(double[] u);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glEvalCoord1fv(float[] u);
		
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glFogfv(uint pname, float[] someParams);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glFogiv(uint pname, int[] someParams);
		
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glGetLightfv(uint light, uint pname, float[] someParams);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glGetLightiv(uint light, uint pname, int[] someParams);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glGetLightfv(uint light, uint pname, out float someParams);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glGetLightiv(uint light, uint pname, out int someParams);
		
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glGetMaterialfv(uint face, uint pname, out float someParams);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glGetMaterialiv(uint face, uint pname, out int someParams);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glGetMaterialfv(uint face, uint pname, float[] someParams);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glGetMaterialiv(uint face, uint pname, int[] someParams);
		
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glGetPolygonStipple(byte[] mask);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glPolygonStipple(byte[] mask);

		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glGetTexEnvfv(uint target, uint pname, out float someParams);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glGetTexEnviv(uint target, uint pname, out int someParams);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glGetTexEnvfv(uint target, uint pname, float[] someParams);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glGetTexEnviv(uint target, uint pname, int[] someParams);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexEnvfv(uint target, uint pname,  float[] someParams);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexEnviv(uint target, uint pname,  int[] someParams);

		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glGetTexGendv(uint coord, uint pname, out double someParams);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glGetTexGenfv(uint coord, uint pname, out float someParams);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glGetTexGeniv(uint coord, uint pname, out int someParams);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glGetTexGendv(uint coord, uint pname, double[] someParams);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glGetTexGenfv(uint coord, uint pname, float[] someParams);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glGetTexGeniv(uint coord, uint pname, int[] someParams);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexGendv(uint coord, uint pname,  double[] someParams);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexGenfv(uint coord, uint pname,  float[] someParams);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexGeniv(uint coord, uint pname,  int[] someParams);

		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glIndexdv(double[] c);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glIndexfv(float[] c);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glIndexiv(int[] c);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glIndexsv(short[] c);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glIndexubv(byte[] c);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glIndexdv(ref double c);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glIndexfv(ref float c);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glIndexiv(ref int c);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glIndexsv(ref short c);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glIndexubv(ref byte c);
		
		// MAP
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glGetMapdv(uint target, uint query, double[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glGetMapfv(uint target, uint query, float[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glGetMapiv(uint target, uint query, int[] v);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glMap1d(uint target, double u1, double u2, int stride, int order,  double[] points);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glMap1f(uint target, float u1, float u2, int stride, int order,  float[] points);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glMap2d(uint target, double u1, double u2, int ustride, int uorder, double v1, double v2, int vstride, int vorder, double[] points);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glMap2f(uint target, float u1, float u2, int ustride, int uorder, float v1, float v2, int vstride, int vorder,  float[] points);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glPixelMapfv(uint map, int mapsize,  float[] values);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glPixelMapuiv(uint map, int mapsize,  uint[] values);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glPixelMapusv(uint map, int mapsize,  ushort[] values);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glGetPixelMapfv(uint map, float[] values);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glGetPixelMapuiv(uint map, uint[] values);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glGetPixelMapusv(uint map, ushort[] values);

		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glRectdv(double[] v1, double[] v2);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glRectfv(float[] v1, float[] v2);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glRectiv(int[] v1, int[] v2);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glRectsv(short[] v1, short[] v2);
		
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexParameterfv(uint target, uint pname,  float[] someParams);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexParameteriv(uint target, uint pname,  int[] someParams);
	
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glColorPointer(int size, uint type, int stride, IntPtr pointer);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glColorPointer(int size, uint type, int stride, float[,] pointer);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glEdgeFlagPointer(int stride,  IntPtr pointer);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glIndexPointer(uint type, int stride, IntPtr pointer);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glNormalPointer(uint type, int stride,  IntPtr pointer);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexCoordPointer(int size, uint type, int stride, IntPtr pointer);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glVertexPointer(int size, uint type, int stride, IntPtr pointer);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glVertexPointer(int size, uint type, int stride, float[,] pointer);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glInterleavedArrays(uint format, int stride,  IntPtr pointer);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glDrawElements(uint mode, int count, uint type, IntPtr indices);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glDrawElements(uint mode, int count, uint type, byte[,] indices);
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glGetPointerv(uint pname, out IntPtr someParams);

		// -----------------------
		// GLU
		// -----------------------
		[DllImport(GLU_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern string gluErrorString(uint error);
		[DllImport(GLU_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern string gluGetString(uint name);
		
		[DllImport(GLU_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void gluPickMatrix(double x, double y, double delX, double delY, int[] viewport);
		[DllImport(GLU_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern int gluScaleImage(uint format, int wIn, int hIn, uint typeIn, IntPtr dataIn, int wOut, int hOut, uint typeOut, IntPtr dataOut);
		[DllImport(GLU_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern int gluScaleImage(uint format, int wIn, int hIn, uint typeIn, IntPtr dataIn, int wOut, int hOut, uint typeOut, byte[] dataOut);
		[DllImport(GLU_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern int gluScaleImage(uint format, int wIn, int hIn, uint typeIn, byte[] dataIn, int wOut, int hOut, uint typeOut, IntPtr dataOut);
		[DllImport(GLU_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern int gluScaleImage(uint format, int wIn, int hIn, uint typeIn, byte[] dataIn, int wOut, int hOut, uint typeOut, byte[] dataOut);
		
		[DllImport(GLU_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern int gluProject(double objX, double objY, double objZ, double[] mesh, double[] proj, int[] view, out double winX, out double winY, out double winZ);
		[DllImport(GLU_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern int gluUnProject(double winX, double winY, double winZ, double[] mesh, double[] proj, int[] view, out double objX, out double objY, out double objZ);
		
		[DllImport(GLU_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern int gluBuild1DMipmaps(uint target, int component, int width, uint format, uint type, IntPtr data);
		[DllImport(GLU_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern int gluBuild2DMipmaps(uint target, int component, int width, int height, uint format, uint type, IntPtr data);
		[DllImport(GLU_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern int gluBuild2DMipmaps(uint target, int component, int width, int height, uint format, uint type, byte[] data);
		[DllImport(GLU_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern int gluBuild2DMipmaps(uint target, int component, int width, int height, uint format, uint type, int[] data);
		
		[DllImport(GLU_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void gluLoadSamplingMatrices(GLUnurbs nurb, float[] mesh, float[] perspective, int[] view);
		[DllImport(GLU_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void gluNurbsCurve(GLUnurbs nurb, int knotCount, float[] knots, int stride, float[] control, int order, uint type);
		[DllImport(GLU_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void gluNurbsSurface(GLUnurbs nurb, int sKnotCount, float[] sKnots, int tKnotCount, float[] tKnots, int sStride, int tStride, float[] control, int sOrder, int tOrder, uint type);
		[DllImport(GLU_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void gluGetNurbsProperty(GLUnurbs nurb, uint property, float[] data);
		[DllImport(GLU_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void gluPwlCurve(GLUnurbs nurb, int count, float[] data, int stride, uint type);
		
		[DllImport(GLU_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void gluTessBeginPolygon(GLUtesselator tess, IntPtr data);
		[DllImport(GLU_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void gluTessVertex(GLUtesselator tess, double[] location, IntPtr data);
		[DllImport(GLU_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void gluTessVertex(GLUtesselator tess, IntPtr location, IntPtr data);

		// --- Methods ---
		#region OpenGL Methods
		#region glAccum(uint op, float value)
		/// <summary>
		/// Operate on the accumulation buffer.
		/// </summary>
		/// <param name="op">
		///	Specifies the accumulation buffer operation.  Symbolic constants <see cref="GL_ACCUM" />, <see cref="GL_LOAD" />, <see cref="GL_ADD" />, <see cref="GL_MULT" />, 
		///	and <see cref="GL_RETURN" /> are accepted.
		///	</param>
		/// <param name="value">
		/// Specifies a floating-point value used in the accumulation buffer operation.  <paramref name="op" /> determines how <paramref name="value" /> is used.
		/// </param>
		/// <remarks>
		/// <para>
		/// The accumulation buffer is an extended-range color buffer.  Images are not rendered into it.  Rather, images rendered into one of the color	buffers	are added 
		/// to the contents of the accumulation buffer after rendering.  Effects such as antialiasing (of points, lines, and polygons), motion blur, and depth of field 
		/// can be created by accumulating images generated with different transformation matrices.
		/// </para>
		/// <para>
		/// Each pixel in the accumulation buffer consists of red, green, blue, and alpha values.  The number of bits per component in the accumulation	buffer depends on 
		/// the implementation.  You can examine this number by calling <see cref="glGetIntegerv" /> four times, with arguments <see cref="GL_ACCUM_RED_BITS" />, 
		/// <see cref="GL_ACCUM_GREEN_BITS" />, <see cref="GL_ACCUM_BLUE_BITS" />, and <see cref="GL_ACCUM_ALPHA_BITS" />.  Regardless of the number of bits per component, 
		/// the range of values stored by each component is [-1, 1].  The accumulation buffer pixels are mapped one-to-one with frame buffer pixels.
		/// </para>
		/// <para>
		/// <b>glAccum</b> operates on the accumulation buffer.  The first argument, <paramref name="op" />, is a symbolic constant that selects an accumulation buffer 
		/// operation.  The second argument, <paramref name="value" />, is a floating-point value to be used in that operation.  Five operations are specified:  
		/// <see cref="GL_ACCUM" />, <see cref="GL_LOAD" />, <see cref="GL_ADD" />, <see cref="GL_MULT" />, and <see cref="GL_RETURN" />.
		/// </para>
		/// <para>
		/// All accumulation buffer operations are limited to the area of the current scissor box and applied identically to the red, green, blue, and alpha components of 
		/// each pixel.  If a <b>glAccum</b> operation results in a value outside the range [-1, 1], the contents of an accumulation buffer pixel component are undefined.
		/// </para>
		/// <para>
		/// <list type="table">
		/// <listheader>
		/// <term>
		/// The operations are as follows:  
		/// </term>
		/// </listheader>
		/// <item>
		/// <term>
		/// <see cref="GL_ACCUM" />
		/// </term>
		/// <description>
		/// Obtains R, G, B, and A values from the buffer currently selected for reading (see <see cref="glReadBuffer" />).  Each component value is divided by 2n-1, 
		/// where n is the number of bits allocated to each color component in the currently selected buffer.  The result is a floating-point value in the range [0, 1], 
		/// which is multiplied by <paramref name="value" /> and added to the corresponding pixel component in the accumulation buffer, thereby updating the accumulation buffer.
		/// </description>
		/// </item>
		/// <item>
		/// <term>
		/// <see cref="GL_LOAD" />
		/// </term>
		/// <description>
		/// Similar	to <see cref="GL_ACCUM" />, except that the current value in the accumulation buffer is not used in the calculation of the new value.  That is, the R, G, B, 
		/// and A values from the currently selected buffer are divided by 2n-1, multiplied by <paramref name="value" />, and then stored in the corresponding accumulation 
		/// buffer cell, overwriting the current value.
		/// </description>
		/// </item>
		/// <item>
		/// <term>
		/// <see cref="GL_ADD" />
		/// </term>
		/// <description>
		/// Adds <paramref name="value" /> to each R, G, B, and A in the accumulation buffer.
		/// </description>
		/// </item>
		/// <item>
		/// <term>
		/// <see cref="GL_MULT" />
		/// </term>
		/// <description>
		/// Multiplies each	R, G, B, and A in the accumulation buffer by <paramref name="value" /> and returns the scaled component to its corresponding 
		/// accumulation buffer location.
		/// </description>
		/// </item>
		/// <item>
		/// <term>
		/// <see cref="GL_RETURN" />
		/// </term>
		/// <description>
		/// Transfers accumulation buffer values to	the color buffer or buffers currently selected for writing.  Each R, G, B, and A component is multiplied by 
		/// <paramref name="value" />, then multiplied by 2n-1, clamped to the range [0, 2n-1], and stored in the corresponding display buffer cell.  The 
		/// only fragment operations that are applied to this transfer are pixel ownership, scissor, dithering, and color writemasks.
		/// </description>
		/// </item>
		/// </list>
		/// </para>
		/// <para>
		/// To clear the accumulation buffer, call <see cref="glClearAccum" /> with R, G, B, and A values to set it to, then call <see cref="glClear" /> with the 
		/// accumulation buffer enabled.
		/// </para>
		/// <para>
		/// Only pixels within the current scissor box are updated by a <b>glAccum</b> operation.
		/// </para>
		/// <para>
		/// <list type="table">
		/// <listheader>
		/// <term>
		/// ERRORS
		/// </term>
		/// </listheader>
		/// <item>
		/// <term>
		/// <see cref="GL_INVALID_ENUM" />
		/// </term>
		/// <description>
		/// Generated if <paramref name="op" /> is not an accepted value.
		/// </description>
		/// </item>
		/// <item>
		/// <term>
		/// <see cref="GL_INVALID_OPERATION"/>
		/// </term>
		/// <description>
		/// Generated if there is no accumulation buffer.
		/// </description>
		/// </item>
		/// <item>
		/// <term>
		/// <see cref="GL_INVALID_OPERATION" />
		/// </term>
		/// <description>
		/// Generated if <b>glAccum</b> is executed between the execution of <see cref="glBegin" /> and the corresponding execution of <see cref="glEnd" />.
		/// </description>
		/// </item>
		/// </list>
		/// </para>
		/// <para>
		/// <list type="table">
		/// <listheader>
		/// <term>
		/// Associated Gets
		/// </term>
		/// </listheader>
		/// <item>
		/// <term>
		/// <see cref="glGetIntegerv" />
		/// </term>
		/// <description>
		/// With argument <see cref="GL_ACCUM_RED_BITS" />
		/// </description>
		/// </item>
		/// <item>
		/// <term>
		/// <see cref="glGetIntegerv" />
		/// </term>
		/// <description>
		/// With argument <see cref="GL_ACCUM_GREEN_BITS" />
		/// </description>
		/// </item>
		/// <item>
		/// <term>
		/// <see cref="glGetIntegerv" />
		/// </term>
		/// <description>
		/// With argument <see cref="GL_ACCUM_BLUE_BITS" />
		/// </description>
		/// </item>
		/// <item>
		/// <term>
		/// <see cref="glGetIntegerv" />
		/// </term>
		/// <description>
		/// With argument <see cref="GL_ACCUM_ALPHA_BITS" />
		/// </description>
		/// </item>
		/// </list>
		/// </para>
		/// </remarks>
		/// <seealso cref="glBlendFunc" />
		/// <seealso cref="glClear" />
		/// <seealso cref="glClearAccum" />
		/// <seealso cref="glCopyPixels" />
		/// <seealso cref="glGetBooleanv" />
		/// <seealso cref="glGetDoublev" />
		/// <seealso cref="glGetFloatv" />
		/// <seealso cref="glGetIntegerv" />
		/// <seealso cref="glLogicOp" />
		/// <seealso cref="glPixelStoref" />
		/// <seealso cref="glPixelStorei" />
		/// <seealso cref="glPixelTransferf" />
		/// <seealso cref="glPixelTransferi" />
		/// <seealso cref="glReadBuffer" />
		/// <seealso cref="glReadPixels" />
		/// <seealso cref="glScissor" />
		/// <seealso cref="glStencilOp" />
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glAccum(uint op, float value);
		#endregion glAccum(uint op, float value

		#region glAlphaFunc(uint func, float aRef)
		/// <summary>
		/// Specify the alpha test function
		/// </summary>
		/// <param name="func">
		/// Specifies the alpha comparison function.  Symbolic constants <see cref="GL_NEVER" />, <see cref="GL_LESS" />, <see cref="GL_EQUAL" />, <see cref="GL_LEQUAL" />, 
		/// <see cref="GL_GREATER" />, <see cref="GL_NOTEQUAL" />, <see cref="GL_GEQUAL" />, and <see cref="GL_ALWAYS" /> are accepted.  The initial value is 
		/// <see cref="GL_ALWAYS" />.
		/// </param>
		/// <param name="aRef">
		/// Specifies the reference value that incoming alpha values are compared to.  This value is clamped to the range 0 through 1, where 0 represents the lowest possible 
		/// alpha value and 1 the highest possible value.  The initial reference value is 0.
		/// </param>
		/// <remarks>
		/// <para>
		/// The alpha test discards fragments depending on the outcome of a comparison between an incoming fragment's alpha value and a constant reference value.  
		/// <b>glAlphaFunc</b> specifies the reference value and the comparison function.  The comparison is performed only if alpha testing is enabled.  By default,
		/// it is not enabled.  (See <see cref="glEnable" /> and <see cref="glDisable" /> of <see cref="GL_ALPHA_TEST" />.)
		/// </para>
		/// <para>
		/// <paramref name="func" /> and <paramref name="aRef" /> specify the conditions under which the pixel is drawn.  The incoming alpha value is compared to 
		/// <paramref name="aRef" /> using the function specified by <paramref name="func" />.  If the value passes the comparison, the incoming fragment is drawn if it 
		/// also passes subsequent stencil and depth buffer tests.  If the value fails the comparison, no change is made to the frame buffer at that pixel location.
		/// </para>
		/// <para>
		/// <list type="table">
		/// <listheader>
		/// <term>
		/// The comparison functions are as follows:  
		/// </term>
		/// </listheader>
		/// <item>
		/// <term>
		/// <see cref="GL_NEVER" />
		/// </term>
		/// <description>
		/// Never passes.
		/// </description>
		/// </item>
		/// <item>
		/// <term>
		/// <see cref="GL_LESS" />
		/// </term>
		/// <description>
		/// Passes if the incoming alpha value is less than the reference value.
		/// </description>
		/// </item>
		/// <item>
		/// <term>
		/// <see cref="GL_EQUAL" />
		/// </term>
		/// <description>
		/// Passes if the incoming alpha value is equal to the reference value.
		/// </description>
		/// </item>
		/// <item>
		/// <term>
		/// <see cref="GL_LEQUAL" />
		/// </term>
		/// <description>
		/// Passes if the incoming alpha value is less than or equal to the reference value.
		/// </description>
		/// </item>
		/// <item>
		/// <term>
		/// <see cref="GL_GREATER" />
		/// </term>
		/// <description>
		/// Passes if the incoming alpha value is greater than the reference value.
		/// </description>
		/// </item>
		/// <item>
		/// <term>
		/// <see cref="GL_NOTEQUAL" />
		/// </term>
		/// <description>
		/// Passes if the incoming alpha value is not equal to the reference value.
		/// </description>
		/// </item>
		/// <item>
		/// <term>
		/// <see cref="GL_GEQUAL" />
		/// </term>
		/// <description>
		/// Passes if the incoming alpha value is greater than or equal to the reference value.
		/// </description>
		/// </item>
		/// <item>
		/// <term>
		/// <see cref="GL_ALWAYS" />
		/// </term>
		/// <description>
		/// Always passes (initial value).
		/// </description>
		/// </item>
		/// </list>
		/// </para>
		/// <para>
		/// <b>glAlphaFunc</b> operates on all pixel write operations, including those resulting from the scan conversion of points, lines, polygons, and bitmaps, and from 
		/// pixel draw and copy operations.  <b>glAlphaFunc</b> does not affect screen clear operations.
		/// </para>
		/// <para>
		/// Alpha testing is performed only in RGBA mode.
		/// </para>
		/// <para>
		/// <list type="table">
		/// <listheader>
		/// <term>
		/// ERRORS
		/// </term>
		/// </listheader>
		/// <item>
		/// <term>
		/// <see cref="GL_INVALID_ENUM" />
		/// </term>
		/// <description>
		/// Generated if <paramref name="func" /> is not an accepted value.
		/// </description>
		/// </item>
		/// <item>
		/// <term>
		/// <see cref="GL_INVALID_OPERATION" />
		/// </term>
		/// <description>
		/// Generated if <b>glAlphaFunc</b> is executed between the execution of <see cref="glBegin" /> and the corresponding execution of <see cref="glEnd" />.
		/// </description>
		/// </item>
		/// </list>
		/// </para>
		/// <para>
		/// <list type="table">
		/// <listheader>
		/// <term>
		/// Associated Gets
		/// </term>
		/// </listheader>
		/// <item>
		/// <term>
		/// <see cref="glGetIntegerv" />
		/// </term>
		/// <description>
		/// With argument <see cref="GL_ALPHA_TEST_FUNC" />
		/// </description>
		/// </item>
		/// <item>
		/// <term>
		/// <see cref="glGetIntegerv" />
		/// </term>
		/// <description>
		/// With argument <see cref="GL_ALPHA_TEST_REF" />
		/// </description>
		/// </item>
		/// <item>
		/// <term>
		/// <see cref="glIsEnabled" />
		/// </term>
		/// <description>
		/// With argument <see cref="GL_ALPHA_TEST" />
		/// </description>
		/// </item>
		/// </list>
		/// </para>
		/// </remarks>
		/// <seealso cref="glBlendFunc" />
		/// <seealso cref="glClear" />
		/// <seealso cref="glDepthFunc" />
		/// <seealso cref="glEnable" />
		/// <seealso cref="glStencilFunc" />
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glAlphaFunc(uint func, float aRef);
		#endregion glAlphaFunc(uint func, float aRef)

		#region glAreTexturesResident(int n, uint *textures, byte *residences)
		/// <summary>
		/// Determine if textures are loaded in texture memory
		/// </summary>
		/// <param name="n">
		/// Specifies the number of textures to be queried.
		/// </param>
		/// <param name="textures">
		/// Specifies an array containing the names of the textures to be queried.
		/// </param>
		/// <param name="residences">
		/// Specifies an array in which the texture residence status is returned.  The residence status of a texture named by an element of <paramref name="textures" /> is 
		/// returned in the corresponding element of <paramref name="residences" />.
		/// </param>
		/// <remarks>
		/// <para>
		/// GL establishes a "working set" of textures that are resident in texture memory.  These textures can be bound to a texture target much more efficiently than textures 
		/// that are not resident.
		/// </para>
		/// <para>
		/// <b>glAreTexturesResident</b> queries the texture residence status of the <paramref name="n" /> textures named by the elements of <paramref name="textures" />.  If 
		/// all the named textures are resident, <b>glAreTexturesResident</b> returns <see cref="GL_TRUE" />, and the contents of <paramref name="residences" /> are undisturbed.  
		/// If not all the named textures are resident, <b>glAreTexturesResident</b> returns <see cref="GL_FALSE" />, and detailed status is returned in the <paramref name="n" /> 
		/// elements of <paramref name="residences" />.  If an element of <paramref name="residences" /> is <see cref="GL_TRUE" />, then the texture named by the corresponding 
		/// element of <paramref name="textures" /> is resident.
		/// </para>
		/// <para>
		/// The residence status of a single bound texture may also be queried by calling <see cref="glGetTexParameteriv" /> with the <paramref name="target" /> argument set to 
		/// the target to which the texture is bound, and the <paramref name="pname" /> argument set to <see cref="GL_TEXTURE_RESIDENT" />.  This is the only way that the 
		/// residence status of a default texture can be queried.
		/// </para>
		/// <para>
		/// <b>glAreTexturesResident</b> is available only if the GL version is 1.1 or greater.
		/// </para>
		/// <para>
		/// <b>glAreTexturesResident</b> returns the residency status of the textures at the time of invocation.  It does not guarantee that the textures will remain resident 
		/// at any other time.
		/// </para>
		/// <para>
		/// If textures reside in virtual memory (there is no texture memory), they are considered always resident.
		/// </para>
		/// <para>
		/// Some implementations may not load a texture until the first use of that texture.
		/// </para>
		/// <para>
		/// <list type="table">
		/// <listheader>
		/// <term>
		/// ERRORS
		/// </term>
		/// </listheader>
		/// <item>
		/// <term>
		/// <see cref="GL_INVALID_VALUE" />
		/// </term>
		/// <description>
		/// Generated if <paramref name="n" /> is negative.
		/// </description>
		/// </item>
		/// <item>
		/// <term>
		/// <see cref="GL_INVALID_VALUE" />
		/// </term>
		/// <description>
		/// Generated if any element in <paramref name="textures" /> is 0 or does not name a texture.  In that case, the function returns <see cref="GL_FALSE" /> and the contents 
		/// of <paramref name="residences" /> is indeterminate.
		/// </description>
		/// </item>
		/// <item>
		/// <term>
		/// <see cref="GL_INVALID_OPERATION" />
		/// </term>
		/// <description>
		/// Generated if <b>glAreTexturesResident</b> is executed between the execution of <see cref="glBegin" /> and the corresponding execution of <see cref="glEnd" />.
		/// </description>
		/// </item>
		/// </list>
		/// </para>
		/// <para>
		/// <list type="table">
		/// <listheader>
		/// <term>
		/// Associated Gets
		/// </term>
		/// </listheader>
		/// <item>
		/// <term>
		/// <see cref="glGetTexParameteriv" />
		/// </term>
		/// <description>
		/// With parameter name <see cref="GL_TEXTURE_RESIDENT" /> retrieves the residence status of a currently bound texture.
		/// </description>
		/// </item>
		/// </list>
		/// </para>
		/// </remarks>
		/// <seealso cref="glBindTexture" />
		/// <seealso cref="glGetTexParameterfv" />
		/// <seealso cref="glGetTexParameteriv" />
		/// <seealso cref="glPrioritizeTextures" />
		/// <seealso cref="glTexImage1D" />
		/// <seealso cref="glTexImage2D" />
		/// <seealso cref="glTexParameterf" />
		/// <seealso cref="glTexParameterfv" />
		/// <seealso cref="glTexParameteri" />
		/// <seealso cref="glTexParameteriv" />
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern byte glAreTexturesResident(int n, uint *textures, byte *residences);
		#endregion glAreTexturesResident(int n, uint *textures, byte *residences)

		#region glArrayElement(int i)
		/// <summary>
		/// Render a vertex using the specified vertex array element
		/// </summary>
		/// <param name="i">
		/// Specifies an index into the enabled vertex data arrays.
		/// </param>
		/// <remarks>
		/// <para>
		/// <b>glArrayElement</b> commands are used within <see cref="glBegin" />/<see cref="glEnd" /> pairs to specify vertex and attribute data for point, line, and 
		/// polygon primitives.  If <see cref="GL_VERTEX_ARRAY" /> is enabled when <b>glArrayElement</b> is called, a single vertex is drawn, using vertex and attribute 
		/// data taken from location <paramref name="i" /> of the enabled arrays.  If <see cref="GL_VERTEX_ARRAY" /> is not enabled, no drawing occurs but the attributes 
		/// corresponding to the enabled arrays are modified.
		/// </para>
		/// <para>
		/// Use <b>glArrayElement</b> to construct primitives by indexing vertex data, rather than by streaming through arrays of data in first-to-last order.  Because each 
		/// call specifies only a single vertex, it is possible to explicitly specify per-primitive attributes such as a single normal per individual triangle.
		/// </para>
		/// <para>
		/// Changes made to array data between the execution of <see cref="glBegin" /> and the corresponding execution of <see cref="glEnd" /> may affect calls to 
		/// <b>glArrayElement</b> that are made within the same <see cref="glBegin" />/<see cref="glEnd" /> period in non-sequential ways.  That is, a call to
		/// <b>glArrayElement</b> that precedes a change to array data may access the changed data, and a call that follows a change to array data may access original data.
		/// </para>
		/// <para>
		/// <b>glArrayElement</b> is available only if the GL version is 1.1 or greater.
		/// </para>
		/// <para>
		/// <b>glArrayElement</b> is included in display lists.  If <b>glArrayElement</b> is entered into a display list, the necessary array data (determined by the array 
		/// pointers and enables) is also entered into the display list.  Because the array pointers and enables are client-side state, their values affect display lists when 
		/// the lists are created, not when the lists are executed.
		/// </para>
		/// </remarks>
		/// <seealso cref="glColorPointer" />
		/// <seealso cref="glDrawArrays" />
		/// <seealso cref="glEdgeFlagPointer" />
		/// <seealso cref="glGetPointerv" />
		/// <seealso cref="glIndexPointer" />
		/// <seealso cref="glInterleavedArrays" />
		/// <seealso cref="glNormalPointer" />
		/// <seealso cref="glTexCoordPointer" />
		/// <seealso cref="glVertexPointer" />
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glArrayElement(int i);
		#endregion glArrayElement(int i)

		#region glBegin(uint mode)
		/// <summary>
		/// Delimit the vertices of a primitive or a group of like primitives
		/// </summary>
		/// <param name="mode">
		/// Specifies the primitive or primitives that will be created from vertices presented between <b>glBegin</b> and the subsequent <see cref="glEnd" />.  Ten symbolic 
		/// constants are accepted:  <see cref="GL_POINTS" />, <see cref="GL_LINES" />, <see cref="GL_LINE_STRIP" />, <see cref="GL_LINE_LOOP" />, <see cref="GL_TRIANGLES" />, 
		/// <see cref="GL_TRIANGLE_STRIP" />, <see cref="GL_TRIANGLE_FAN" />, <see cref="GL_QUADS" />, <see cref="GL_QUAD_STRIP" />, and <see cref="GL_POLYGON" />.
		/// </param>
		/// <remarks>
		/// <para>
		/// <b>glBegin</b> and <see cref="glEnd" /> delimit the vertices that define a primitive or a group of like primitives.  <b>glBegin</b> accepts a single argument that 
		/// specifies in which of ten ways the vertices are interpreted.  Taking n as an integer count starting at one, and N as the total number of vertices specified.
		/// </para>
		/// <para>
		/// <list type="table">
		/// <listheader>
		/// <term>
		/// The interpretations are as follows:  
		/// </term>
		/// </listheader>
		/// <item>
		/// <term>
		/// <see cref="GL_POINTS" />
		/// </term>
		/// <description>
		/// Treats each vertex as a single point.  Vertex n defines point n.  N points are drawn.
		/// </description>
		/// </item>
		/// <item>
		/// <term>
		/// <see cref="GL_LINES" />
		/// </term>
		/// <description>
		/// Treats each pair of vertices as an independent line segment.  Vertices 2n-1 and 2n define line n.  N/2 lines are drawn.
		/// </description>
		/// </item>
		/// <item>
		/// <term>
		/// <see cref="GL_LINE_STRIP" />
		/// </term>
		/// <description>
		/// Draws a connected group of line segments from the first vertex to the last.  Vertices n and n+1 define line n.  N-1 lines are drawn.
		/// </description>
		/// </item>
		/// <item>
		/// <term>
		/// <see cref="GL_LINE_LOOP"/>
		/// </term>
		/// <description>
		/// Draws a connected group of line segments from the first vertex to the last, then back to the first.  Vertices n and n+1 define line n.  The last line, however, 
		/// is defined by vertices N and 1.  N lines are drawn.
		/// </description>
		/// </item>
		/// <item>
		/// <term>
		/// <see cref="GL_TRIANGLES" />
		/// </term>
		/// <description>
		/// Treats each triplet of vertices as an independent triangle.  Vertices 3n-2, 3n-1, and 3n define triangle n.  N/3 triangles are drawn.
		/// </description>
		/// </item>
		/// <item>
		/// <term>
		/// <see cref="GL_TRIANGLE_STRIP" />
		/// </term>
		/// <description>
		/// Draws a connected group of triangles.  One triangle is defined for each vertex presented after the first two vertices.  For odd n, vertices n, n+1, and n+2 
		/// define triangle n.  For even n, vertices n+1, n, and n+2 define triangle n.  N-2 triangles are drawn.
		/// </description>
		/// </item>
		/// <item>
		/// <term>
		/// <see cref="GL_TRIANGLE_FAN" />
		/// </term>
		/// <description>
		/// Draws a connected group of triangles.  One triangle is defined for each vertex presented after the first two vertices.  Vertices 1, n+1, and n+2 define triangle n.  
		/// N-2 triangles are drawn.
		/// </description>
		/// </item>
		/// <item>
		/// <term>
		/// <see cref="GL_QUADS" />
		/// </term>
		/// <description>
		/// Treats each group of four vertices as an independent quadrilateral.  Vertices 4n-3, 4n-2, 4n-1, and 4n define quadrilateral n.  N/4 quadrilaterals are drawn.
		/// </description>
		/// </item>
		/// <item>
		/// <term>
		/// <see cref="GL_QUAD_STRIP" />
		/// </term>
		/// <description>
		/// Draws a connected group of quadrilaterals.  One quadrilateral is defined for each pair of vertices presented after the first pair.  Vertices 2n-1, 2n, 2n+2, and 
		/// 2n+1 define quadrilateral n.  N/2-1 quadrilaterals are drawn.  Note that the order in which vertices are used to construct a quadrilateral from strip data is 
		/// different from that used with independent data.
		/// </description>
		/// </item>
		/// <item>
		/// <term>
		/// <see cref="GL_POLYGON" />
		/// </term>
		/// <description>
		/// Draws a single, convex polygon.  Vertices 1 through N define this polygon.
		/// </description>
		/// </item>
		/// </list>
		/// </para>
		/// <para>
		/// Only a subset of GL commands can be used between <b>glBegin</b> and <see cref="glEnd" />.  The commands are <see cref="glVertex" />, <see cref="glColor" />, 
		/// <see cref="glIndex" />, <see cref="glNormal" />, <see cref="glTexCoord" />, <see cref="glEvalCoord" />, <see cref="glEvalPoint" />, <see cref="glArrayElement"/>, 
		/// <see cref="glMaterial" />, and <see cref="glEdgeFlag" />.  Also, it is acceptable to use <see cref="glCallList" /> or <see cref="glCallLists" /> to execute 
		/// display lists that include only the preceding commands.  If any other GL command is executed between <b>glBegin</b> and <see cref="glEnd" />, the error flag is set 
		/// and the command is ignored.
		/// </para>
		/// <para>
		/// Regardless of the value chosen for <paramref name="mode" />, there is no limit to the number of vertices that can be defined between <b>glBegin</b> and 
		/// <see cref="glEnd" />.  Lines, triangles, quadrilaterals, and polygons that are incompletely specified are not drawn.  Incomplete specification results when either 
		/// too few vertices are provided to specify even a single primitive or when an incorrect multiple of vertices is specified.  The incomplete primitive is ignored; the 
		/// rest are drawn.
		/// </para>
		/// <para>
		/// The minimum specification of vertices for each primitive is as follows:  1 for a point, 2 for a line, 3 for a triangle, 4 for a quadrilateral, and 3 for a polygon.  
		/// Modes that require a certain multiple of vertices are <see cref="GL_LINES" /> (2), <see cref="GL_TRIANGLES" /> (3), <see cref="GL_QUADS" /> (4), and 
		/// <see cref="GL_QUAD_STRIP" /> (2).
		/// </para>
		/// <para>
		/// <list type="table">
		/// <listheader>
		/// <term>
		/// ERRORS
		/// </term>
		/// </listheader>
		/// <item>
		/// <term>
		/// <see cref="GL_INVALID_ENUM" />
		/// </term>
		/// <description>
		/// Generated if <paramref name="mode" /> is set to an unaccepted value.
		/// </description>
		/// </item>
		/// <item>
		/// <term>
		/// <see cref="GL_INVALID_OPERATION" />
		/// </term>
		/// <description>
		/// Generated if <b>glBegin</b> is executed between a <b>glBegin</b> and the corresponding execution of <see cref="glEnd" />.
		/// </description>
		/// </item>
		/// <item>
		/// <term>
		/// <see cref="GL_INVALID_OPERATION" />
		/// </term>
		/// <description>
		/// Generated if <see cref="glEnd" /> is executed without being preceded by a <b>glBegin</b>.
		/// </description>
		/// </item>
		/// <item>
		/// <term>
		/// <see cref="GL_INVALID_OPERATION" />
		/// </term>
		/// <description>
		/// Generated if a command other than <see cref="glVertex" />, <see cref="glColor" />, <see cref="glIndex" />, <see cref="glNormal" />, <see cref="glTexCoord" />, 
		/// <see cref="glEvalCoord" />, <see cref="glEvalPoint" />, <see cref="glArrayElement" />, <see cref="glMaterial" />, <see cref="glEdgeFlag" />, <see cref="glCallList" />, 
		/// or <see cref="glCallLists" /> is executed between the execution of <b>glBegin</b> and the corresponding execution <see cref="glEnd" />.
		/// </description>
		/// </item>
		/// </list>
		/// </para>
		/// <para>
		/// Execution of <see cref="glEnableClientState" />, <see cref="glDisableClientState" />, <see cref="glEdgeFlagPointer" />, <see cref="glTexCoordPointer" />, 
		/// <see cref="glColorPointer" />, <see cref="glIndexPointer" />, <see cref="glNormalPointer" />, <see cref="glVertexPointer" />, <see cref="glInterleavedArrays" />, 
		/// or <see cref="glPixelStore" /> is not allowed after a call to <b>glBegin</b> and before the corresponding call to <see cref="glEnd" />, but an error may or may not 
		/// be generated.
		/// </para>
		/// </remarks>
		/// <seealso cref="glArrayElement" />
		/// <seealso cref="glCallList" />
		/// <seealso cref="glCallLists" />
		/// <seealso cref="glColor" />
		/// <seealso cref="glEdgeFlag" />
		/// <seealso cref="glEnd" />
		/// <seealso cref="glEvalCoord" />
		/// <seealso cref="glEvalPoint" />
		/// <seealso cref="glIndex" />
		/// <seealso cref="glMaterial" />
		/// <seealso cref="glNormal" />
		/// <seealso cref="glTexCoord" />
		/// <seealso cref="glVertex" />
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glBegin(uint mode);
		#endregion glBegin(uint mode)

		#region glBindTexture(uint target, uint texture)
		/// <summary>
		/// Bind a named texture to a texturing target
		/// </summary>
		/// <param name="target">
		/// Specifies the target to which the texture is bound.  Must be either <see cref="GL_TEXTURE_1D" /> or <see cref="GL_TEXTURE_2D" />.
		/// </param>
		/// <param name="texture">
		/// Specifies the name of a texture.
		/// </param>
		/// <remarks>
		/// <para>
		/// <b>glBindTexture</b> lets you create or use a named texture.  Calling <b>glBindTexture</b> with <paramref name="target" /> set to <see cref="GL_TEXTURE_1D"/> 
		/// or <see cref="GL_TEXTURE_2D"/> and <paramref name="texture" /> set to the name of the newtexture binds the texture name to the target.  When a texture is bound 
		/// to a target, the previous binding for that target is automatically broken.
		/// </para>
		/// <para>
		/// Texture names are unsigned integers.  The value zero is reserved to represent the default texture for each texture target.  Texture names and the corresponding 
		/// texture contents are local to the shared display-list space of the current GL rendering context; two rendering contexts share texture names only if they also share 
		/// display lists.
		/// </para>
		/// <para>
		/// You may use <see cref="glGenTextures" /> to generate a set of new texture names.
		/// </para>
		/// <para>
		/// When a texture is first bound, it assumes the dimensionality of its target:  A texture first bound to <see cref="GL_TEXTURE_1D" /> becomes 1-dimensional and a 
		/// texture first bound to <see cref="GL_TEXTURE_2D" /> becomes 2-dimensional.  The state of a 1-dimensional texture immediately after it is first bound is equivalent 
		/// to the state of the default <see cref="GL_TEXTURE_1D" /> at GL initialization, and similarly for 2-dimensional textures.
		/// </para>
		/// <para>
		/// While a texture is bound, GL operations on the target to which it is bound affect the bound texture, and queries of the target to which it is bound return state 
		/// from the bound texture.  If texture mapping of the dimensionality of the target to which a texture is bound is active, the bound texture is used.  In effect, the 
		/// texture targets become aliases for the textures currently bound to them, and the texture name zero refers to the default textures that were bound to them at 
		/// initialization.
		/// </para>
		/// <para>
		/// A texture binding created with <b>glBindTexture</b> remains active until a different texture is bound to the same target, or until the bound texture is deleted 
		/// with <see cref="glDeleteTextures" />.
		/// </para>
		/// <para>
		/// Once created, a named texture may be re-bound to the target of the matching dimensionality as often as needed.  It is usually much faster to use <b>glBindTexture</b> 
		/// to bind an existing named texture to one of the texture targets than it is to reload the texture image using <see cref="glTexImage1D" /> or <see cref="glTexImage2D" />.  
		/// For additional control over performance, use <see cref="glPrioritizeTextures" />.
		/// </para>
		/// <para>
		/// <b>glBindTexture</b> is included in display lists.
		/// </para>
		/// <para>
		/// <b>glBindTexture</b> is available only if the GL version is 1.1 or greater.
		/// </para>
		/// <para>
		/// <list type="table">
		/// <listheader>
		/// <term>
		/// ERRORS
		/// </term>
		/// </listheader>
		/// <item>
		/// <term>
		/// <see cref="GL_INVALID_ENUM" />
		/// </term>
		/// <description>
		/// Generated if <paramref name="target" /> is not one of the allowable values.
		/// </description>
		/// </item>
		/// <item>
		/// <term>
		/// <see cref="GL_INVALID_OPERATION" />
		/// </term>
		/// <description>
		/// Generated if texture has a dimensionality which doesn't match that of <paramref name="target" />.
		/// </description>
		/// </item>
		/// <item>
		/// <term>
		/// <see cref="GL_INVALID_OPERATION" />
		/// </term>
		/// <description>
		/// Generated if <b>glBindTexture</b> is executed between the execution of <see cref="glBegin" /> and the corresponding execution of <see cref="glEnd" />.
		/// </description>
		/// </item>
		/// </list>
		/// </para>
		/// <para>
		/// <list type="table">
		/// <listheader>
		/// <term>
		/// Associated Gets
		/// </term>
		/// </listheader>
		/// <item>
		/// <term>
		/// <see cref="glGetIntegerv" />
		/// </term>
		/// <description>
		/// With argument <see cref="GL_TEXTURE_1D_BINDING" />
		/// </description>
		/// </item>
		/// <item>
		/// <term>
		/// <see cref="glGetIntegerv" />
		/// </term>
		/// <description>
		/// With argument <see cref="GL_TEXTURE_2D_BINDING" />
		/// </description>
		/// </item>
		/// </list>
		/// </para>
		/// </remarks>
		/// <seealso cref="glAreTexturesResident" />
		/// <seealso cref="glDeleteTextures" />
		/// <seealso cref="glGenTextures" />
		/// <seealso cref="glGetIntegerv" />
		/// <seealso cref="glGetTexParameter" />
		/// <seealso cref="glIsTexture" />
		/// <seealso cref="glPrioritizeTextures" />
		/// <seealso cref="glTexImage1D" />
		/// <seealso cref="glTexImage2D" />
		/// <seealso cref="glTexParameter" />
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glBindTexture(uint target, uint texture);
		#endregion glBindTexture(uint target, uint texture)

		#region glBitmap(int width, int height, float xorig, float yorig, float xmove, float ymove, byte *bitmap)
		/// <summary>
		/// Draw a bitmap
		/// </summary>
		/// <param name="width">
		/// Specify the pixel width of the bitmap image.
		/// </param>
		/// <param name="height">
		/// Specify the pixel height of the bitmap image.
		/// </param>
		/// <param name="xorig">
		/// Specify the location of the origin in the bitmap image.  The origin is measured from the lower left corner of the bitmap, with right and up being the positive axes.
		/// </param>
		/// <param name="yorig">
		/// Specify the location of the origin in the bitmap image.  The origin is measured from the lower left corner of the bitmap, with right and up being the positive axes.
		/// </param>
		/// <param name="xmove">
		/// Specify the x offset to be added to the current raster position after the bitmap is drawn.
		/// </param>
		/// <param name="ymove">
		/// Specify the y offset to be added to the current raster position after the bitmap is drawn.
		/// </param>
		/// <param name="bitmap">
		/// Specifies the address of the bitmap image.
		/// </param>
		/// <remarks>
		/// <para>
		/// A bitmap is a binary image.  When drawn, the bitmap is positioned relative to the current raster position, and frame buffer pixels corresponding to 1's in the bitmap 
		/// are written using the current raster color or index.  Frame buffer pixels corresponding to 0's in the bitmap are not modified.
		/// </para>
		/// <para>
		/// <b>glBitmap</b> takes seven arguments.  The first pair specifies the width and height of the bitmap image.  The second pair specifies the location of the bitmap origin 
		/// relative to the lower left corner of the bitmap image.  The third pair of arguments specifies x and y offsets to be added to the current raster position after the 
		/// bitmap has been drawn.  The final argument is a pointer to the bitmap image itself.
		/// </para>
		/// <para>
		/// The bitmap image is interpreted like image data for the <see cref="glDrawPixels" /> command, with <paramref name="width" /> and <paramref name="height" /> 
		/// corresponding to the width and height arguments of that command, and with type set to <see cref="GL_BITMAP" /> and format set to <see cref="GL_COLOR_INDEX" />.  
		/// Modes specified using <see cref="glPixelStore" /> affect the interpretation of bitmap image data; modes specified using <see cref="glPixelTransfer" /> do not.
		/// </para>
		/// <para>
		/// If the current raster position is invalid, <b>glBitmap</b> is ignored.  Otherwise, the lower left corner of the bitmap image is positioned at the window coordinates 
		/// <code>
		/// x<sub>w</sub> = <font face="Symbol"></font>x<sub>r</sub> - x<sub>o</sub><font face="Symbol"></font>
		/// y<sub>w</sub> = <font face="Symbol"></font>y<sub>r</sub> - y<sub>o</sub><font face="Symbol"></font>
		/// </code>
		/// </para>
		/// <para>
		/// Where (x<sub>r</sub>, y<sub>r</sub>) is the raster position and (x<sub>o</sub>, y<sub>o</sub>) is the bitmap origin.  Fragments are then generated for each pixel 
		/// corresponding to a 1 (one) in the bitmap image.  These fragments are generated using the current raster z coordinate, color or color index, and current raster texture 
		/// coordinates.  They are then treated just as if they had been generated by a point, line, or polygon, including texture mapping, fogging, and all per-fragment 
		/// operations such as alpha and depth testing.
		/// </para>
		/// <para>
		/// After the bitmap has been drawn, the x and y coordinates of the current raster position are offset by <paramref name="xmove" /> and <paramref name="ymove" />.  No 
		/// change is made to the z coordinate of the current raster position, or to the current raster color, texture coordinates, or index.
		/// </para>
		/// <para>
		/// To set a valid raster position outside the viewport, first set a valid raster position inside the viewport, then call <b>glBitmap</b> with NULL as the 
		/// <paramref name="bitmap" /> parameter and with <paramref name="xmove" /> and <paramref name="ymove" /> set to the offsets of the new raster position.  This technique 
		/// is useful when panning an image around the viewport.
		/// </para>
		/// <para>
		/// <list type="table">
		/// <listheader>
		/// <term>
		/// ERRORS
		/// </term>
		/// </listheader>
		/// <item>
		/// <term>
		/// <see cref="GL_INVALID_VALUE" />
		/// </term>
		/// <description>
		/// Generated if <paramref name="width" /> or <paramref name="height" /> is negative.
		/// </description>
		/// </item>
		/// <item>
		/// <term>
		/// <see cref="GL_INVALID_OPERATION" />
		/// </term>
		/// <description>
		/// Generated if <b>glBitmap</b> is executed between the execution of <see cref="glBegin" /> and the corresponding execution of <see cref="glEnd" />.
		/// </description>
		/// </item>
		/// </list>
		/// </para>
		/// <para>
		/// <list type="table">
		/// <listheader>
		/// <term>
		/// Associated Gets
		/// </term>
		/// </listheader>
		/// <item>
		/// <term>
		/// <see cref="glGetIntegerv" />
		/// </term>
		/// <description>
		/// With argument <see cref="GL_CURRENT_RASTER_POSITION" />
		/// </description>
		/// </item>
		/// <item>
		/// <term>
		/// <see cref="glGetIntegerv" />
		/// </term>
		/// <description>
		/// With argument <see cref="GL_CURRENT_RASTER_COLOR" />
		/// </description>
		/// </item>
		/// <item>
		/// <term>
		/// <see cref="glGetIntegerv" />
		/// </term>
		/// <description>
		/// With argument <see cref="GL_CURRENT_RASTER_INDEX" />
		/// </description>
		/// </item>
		/// <item>
		/// <term>
		/// <see cref="glGetIntegerv" />
		/// </term>
		/// <description>
		/// With argument <see cref="GL_CURRENT_RASTER_TEXTURE_COORDS" />
		/// </description>
		/// </item>
		/// <item>
		/// <term>
		/// <see cref="glGetIntegerv" />
		/// </term>
		/// <description>
		/// With argument <see cref="GL_CURRENT_RASTER_POSITION_VALID" />
		/// </description>
		/// </item>
		/// </list>
		/// </para>
		/// </remarks>
		/// <seealso cref="glDrawPixels" />
		/// <seealso cref="glPixelStore" />
		/// <seealso cref="glPixelTransfer" />
		/// <seealso cref="glRasterPos" />
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glBitmap(int width, int height, float xorig, float yorig, float xmove, float ymove, byte *bitmap);
		#endregion glBitmap(int width, int height, float xorig, float yorig, float xmove, float ymove, byte *bitmap)

		/// <summary>
		/// Specify pixel arithmetic
		/// </summary>
		/// <param name="sfactor">
		/// Specifies how the red, green, blue, and alpha source blending factors are computed.  Nine symbolic constants are accepted:  <see cref="GL_ZERO" />, 
		/// <see cref="GL_ONE" />, <see cref="GL_DST_COLOR" />, <see cref="GL_ONE_MINUS_DST_COLOR" />, <see cref="GL_SRC_ALPHA" />, <see cref="GL_ONE_MINUS_SRC_ALPHA" />, 
		/// <see cref="GL_DST_ALPHA" />, <see cref="GL_ONE_MINUS_DST_ALPHA" />, and <see cref="GL_SRC_ALPHA_SATURATE" />.  The initial value is <see cref="GL_ONE" />.
		/// </param>
		/// <param name="dfactor">
		/// Specifies how the red, green, blue, and alpha destination blending factors are computed.  Eight symbolic constants are accepted:  <see cref="GL_ZERO" />, 
		/// <see cref="GL_ONE" />, <see cref="GL_SRC_COLOR" />, <see cref="GL_ONE_MINUS_SRC_COLOR" />, <see cref="GL_SRC_ALPHA" />, <see cref="GL_ONE_MINUS_SRC_ALPHA" />, 
		/// <see cref="GL_DST_ALPHA" />, and <see cref="GL_ONE_MINUS_DST_ALPHA" />.  The initial value is <see cref="GL_ZERO" />.
		/// </param>
		/// <remarks>
		/// <para>
		/// In RGBA mode, pixels can be drawn using a function that blends the incoming (source) RGBA values with the RGBA values that are already in the frame buffer 
		/// (the destination values).  Blending is initially disabled.  Use <see cref="glEnable" /> and <see cref="glDisable" /> with argument <see cref="GL_BLEND" /> to enable 
		/// and disable blending.
		/// </para>
		/// <para>
		/// <b>glBlendFunc</b> defines the operation of blending when it is enabled.  <paramref name="sfactor" /> specifies which of nine methods is used to scale the source 
		/// color components.  <paramref name="dfactor" /> specifies which of eight methods is used to scale the destination color components.  The eleven possible methods are 
		/// described in the following table.  Each method defines four scale factors, one each for red, green, blue, and alpha.
		/// </para>
		/// <para>
		/// 
		/// </para>
		/// </remarks>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glBlendFunc(uint sfactor, uint dfactor);

		/// <summary>
		/// Execute a display list
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glCallList(uint list);

		/// <summary>
		/// Execute a list of display lists
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glCallLists(int n, uint type,  void *lists);

		/// <summary>
		/// Clear buffers to preset values
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glClear(uint mask);

		/// <summary>
		/// Specify clear values for the accumulation buffer
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glClearAccum(float red, float green, float blue, float alpha);

		/// <summary>
		/// Specify clear values for the color buffers
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glClearColor(float red, float green, float blue, float alpha);

		/// <summary>
		/// Specify the clear value for the depth buffer
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glClearDepth(double depth);

		/// <summary>
		/// Specify the clear value for the color index buffers
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glClearIndex(float c);

		/// <summary>
		/// Specify the clear value for the stencil buffer
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glClearStencil(int s);

		/// <summary>
		/// Specify a plane against which all geometry is clipped
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glClipPlane(uint plane,  double *equation);

		/// <summary>
		/// Set the current color
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glColor3b(sbyte red, sbyte green, sbyte blue);

		/// <summary>
		/// Set the current color
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glColor3bv( sbyte *v);

		/// <summary>
		/// Set the current color
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glColor3d(double red, double green, double blue);

		/// <summary>
		/// Set the current color
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glColor3dv( double *v);

		/// <summary>
		/// Set the current color
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glColor3f(float red, float green, float blue);

		/// <summary>
		/// Set the current color
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glColor3fv( float *v);

		/// <summary>
		/// Set the current color
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glColor3i(int red, int green, int blue);

		/// <summary>
		/// Set the current color
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glColor3iv( int *v);

		/// <summary>
		/// Set the current color
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glColor3s(short red, short green, short blue);

		/// <summary>
		/// Set the current color
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glColor3sv( short *v);

		/// <summary>
		/// Set the current color
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glColor3ub(byte red, byte green, byte blue);

		/// <summary>
		/// Set the current color
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glColor3ubv( byte *v);

		/// <summary>
		/// Set the current color
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glColor3ui(uint red, uint green, uint blue);

		/// <summary>
		/// Set the current color
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glColor3uiv( uint *v);

		/// <summary>
		/// Set the current color
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glColor3us(ushort red, ushort green, ushort blue);

		/// <summary>
		/// Set the current color
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glColor3usv( ushort *v);

		/// <summary>
		/// Set the current color
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glColor4b(sbyte red, sbyte green, sbyte blue, sbyte alpha);

		/// <summary>
		/// Set the current color
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glColor4bv( sbyte *v);

		/// <summary>
		/// Set the current color
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glColor4d(double red, double green, double blue, double alpha);

		/// <summary>
		/// Set the current color
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glColor4dv( double *v);

		/// <summary>
		/// Set the current color
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glColor4f(float red, float green, float blue, float alpha);

		/// <summary>
		/// Set the current color
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glColor4fv( float *v);

		/// <summary>
		/// Set the current color
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glColor4i(int red, int green, int blue, int alpha);

		/// <summary>
		/// Set the current color
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glColor4iv( int *v);

		/// <summary>
		/// Set the current color
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glColor4s(short red, short green, short blue, short alpha);

		/// <summary>
		/// Set the current color
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glColor4sv( short *v);

		/// <summary>
		/// Set the current color
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glColor4ub(byte red, byte green, byte blue, byte alpha);

		/// <summary>
		/// Set the current color
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glColor4ubv( byte *v);

		/// <summary>
		/// Set the current color
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glColor4ui(uint red, uint green, uint blue, uint alpha);

		/// <summary>
		/// Set the current color
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glColor4uiv( uint *v);

		/// <summary>
		/// Set the current color
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glColor4us(ushort red, ushort green, ushort blue, ushort alpha);

		/// <summary>
		/// Set the current color
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glColor4usv( ushort *v);

		/// <summary>
		/// Enable and disable writing of frame buffer color components
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glColorMask(byte red, byte green, byte blue, byte alpha);

		/// <summary>
		/// Cause a material color to track the current color
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glColorMaterial(uint face, uint mode);

		/// <summary>
		/// Define an array of colors
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glColorPointer(int size, uint type, int stride,  void *pointer);

		/// <summary>
		/// Copy pixels in the frame buffer
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glCopyPixels(int x, int y, int width, int height, uint type);

		/// <summary>
		/// Copy pixels into a 1D texture image
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glCopyTexImage1D(uint target, int level, uint internalformat, int x, int y, int width, int border);

		/// <summary>
		/// Copy pixels into a 2D texture image
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glCopyTexImage2D(uint target, int level, uint internalformat, int x, int y, int width, int height, int border);

		/// <summary>
		/// Copy a one-dimensional texture subimage
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glCopyTexSubImage1D(uint target, int level, int xoffset, int x, int y, int width);

		/// <summary>
		/// Copy a two-dimensional texture subimage
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glCopyTexSubImage2D(uint target, int level, int xoffset, int yoffset, int x, int y, int width, int height);

		/// <summary>
		/// Specify whether front- or back-facing facets can be culled
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glCullFace(uint mode);

		/// <summary>
		/// Delete a contiguous group of display lists
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glDeleteLists(uint list, int range);

		/// <summary>
		/// Delete named textures
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glDeleteTextures(int n,  uint *textures);

		/// <summary>
		/// Specify the value used for depth buffer comparisons
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glDepthFunc(uint func);

		/// <summary>
		/// Enable or disable writing into the depth buffer
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glDepthMask(byte flag);

		/// <summary>
		/// Specify mapping of depth values from normalized device coordinates to window coordinates
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glDepthRange(double zNear, double zFar);

		/// <summary>
		/// Disable server-side GL capabilities 
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glDisable(uint cap);

		/// <summary>
		/// Disable client-side capability
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glDisableClientState(uint array);

		/// <summary>
		/// Render primitives from array data
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glDrawArrays(uint mode, int first, int count);

		/// <summary>
		/// Specify which color buffers are to be drawn into
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glDrawBuffer(uint mode);

		/// <summary>
		/// Render primitives from array data
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glDrawElements(uint mode, int count, uint type,  void *indices);

		/// <summary>
		/// Write a block of pixels to the frame buffer
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glDrawPixels(int width, int height, uint format, uint type,  void *pixels);

		/// <summary>
		/// Flag edges as either boundary or nonboundary
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glEdgeFlag(byte flag);

		/// <summary>
		/// Define an array of edge flags
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glEdgeFlagPointer(int stride,  byte *pointer);

		/// <summary>
		/// Flag edges as either boundary or nonboundary
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glEdgeFlagv( byte *flag);

		/// <summary>
		/// Enable server-side GL capabilities
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glEnable(uint cap);

		/// <summary>
		/// Enable client-side capability
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glEnableClientState(uint array);

		/// <summary>
		/// Delimit the vertices of a primitive or a group of like primitives
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glEnd();

		/// <summary>
		/// Create or replace a display list
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glEndList();

		/// <summary>
		/// Evaluate enabled one-dimensional maps
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glEvalCoord1d(double u);

		/// <summary>
		/// Evaluate enabled one-dimensional maps
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glEvalCoord1dv( double *u);

		/// <summary>
		/// Evaluate enabled one-dimensional maps
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glEvalCoord1f(float u);

		/// <summary>
		/// Evaluate enabled one-dimensional maps
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glEvalCoord1fv( float *u);

		/// <summary>
		/// Evaluate enabled two-dimensional maps
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glEvalCoord2d(double u, double v);

		/// <summary>
		/// Evaluate enabled two-dimensional maps
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glEvalCoord2dv( double *u);

		/// <summary>
		/// Evaluate enabled two-dimensional maps
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glEvalCoord2f(float u, float v);

		/// <summary>
		/// Evaluate enabled two-dimensional maps
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glEvalCoord2fv( float *u);

		/// <summary>
		/// Compute a one-dimensional grid of points or lines
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glEvalMesh1(uint mode, int i1, int i2);

		/// <summary>
		/// Compute a two-dimensional grid of points or lines
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glEvalMesh2(uint mode, int i1, int i2, int j1, int j2);

		/// <summary>
		/// Generate and evaluate a single point in a mesh
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glEvalPoint1(int i);

		/// <summary>
		/// Generate and evaluate a single point in a mesh
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glEvalPoint2(int i, int j);

		/// <summary>
		/// Controls feedback mode
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glFeedbackBuffer(int size, uint type, float *buffer);

		/// <summary>
		/// Block until all GL execution is complete
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glFinish();

		/// <summary>
		/// Force execution of GL commands in finite time
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glFlush();

		/// <summary>
		/// Specify fog parameters
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glFogf(uint pname, float param);

		/// <summary>
		/// Specify fog parameters
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glFogfv(uint pname,  float *someParams);

		/// <summary>
		/// Specify fog parameters
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glFogi(uint pname, int param);

		/// <summary>
		/// Specify fog parameters
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glFogiv(uint pname,  int *someParams);

		/// <summary>
		/// Define front- and back-facing polygons
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glFrontFace(uint mode);

		/// <summary>
		/// Multiply the current matrix by a perspective matrix
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glFrustum(double left, double right, double bottom, double top, double zNear, double zFar);

		/// <summary>
		/// Generate a contiguous set of empty display lists
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern uint glGenLists(int range);

		/// <summary>
		/// Generate texture names
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glGenTextures(int n, uint *textures);

		/// <summary>
		/// Return the value or values of a selected parameter
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glGetBooleanv(uint pname, byte *someParams);

		/// <summary>
		/// Return the coefficients of the specified clipping plane
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glGetClipPlane(uint plane, double *equation);

		/// <summary>
		/// Return the value or values of a selected parameter
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glGetDoublev(uint pname, double *someParams);

		/// <summary>
		/// Return error information
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern uint glGetError();

		/// <summary>
		/// Return the value or values of a selected parameter
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glGetFloatv(uint pname, float *someParams);

		/// <summary>
		/// Return the value or values of a selected parameter
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glGetIntegerv(uint pname, int *someParams);

		/// <summary>
		/// Return light source parameter values
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glGetLightfv(uint light, uint pname, float *someParams);

		/// <summary>
		/// Return light source parameter values
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glGetLightiv(uint light, uint pname, int *someParams);

		/// <summary>
		/// Return evaluator parameters
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glGetMapdv(uint target, uint query, double *v);

		/// <summary>
		/// Return evaluator parameters
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glGetMapfv(uint target, uint query, float *v);

		/// <summary>
		/// Return evaluator parameters
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glGetMapiv(uint target, uint query, int *v);

		/// <summary>
		/// Return material parameters
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glGetMaterialfv(uint face, uint pname, float *someParams);

		/// <summary>
		/// Return material parameters
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glGetMaterialiv(uint face, uint pname, int *someParams);

		/// <summary>
		/// Return the specified pixel map
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glGetPixelMapfv(uint map, float *values);

		/// <summary>
		/// Return the specified pixel map
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glGetPixelMapuiv(uint map, uint *values);

		/// <summary>
		/// Return the specified pixel map
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glGetPixelMapusv(uint map, ushort *values);

		/// <summary>
		/// Return the address of the specified pointer
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glGetPointerv(uint pname, void* *someParams);

		/// <summary>
		/// Return the polygon stipple pattern
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glGetPolygonStipple(byte *mask);

		/// <summary>
		/// Return texture environment parameters
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glGetTexEnvfv(uint target, uint pname, float *someParams);

		/// <summary>
		/// Return texture environment parameters
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glGetTexEnviv(uint target, uint pname, int *someParams);

		/// <summary>
		/// Return texture coordinate generation parameters
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glGetTexGendv(uint coord, uint pname, double *someParams);

		/// <summary>
		/// Return texture coordinate generation parameters
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glGetTexGenfv(uint coord, uint pname, float *someParams);

		/// <summary>
		/// Return texture coordinate generation parameters
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glGetTexGeniv(uint coord, uint pname, int *someParams);

		/// <summary>
		/// Return a texture image
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glGetTexImage(uint target, int level, uint format, uint type, void *pixels);

		/// <summary>
		/// Return texture parameter values for a specific level of detail
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glGetTexLevelParameterfv(uint target, int level, uint pname, float *someParams);

		/// <summary>
		/// Return texture parameter values for a specific level of detail
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glGetTexLevelParameteriv(uint target, int level, uint pname, int *someParams);

		/// <summary>
		/// Return texture parameter values
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glGetTexParameterfv(uint target, uint pname, float *someParams);

		/// <summary>
		/// Return texture parameter values
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glGetTexParameteriv(uint target, uint pname, int *someParams);

		/// <summary>
		/// Specify implementation-specific hints
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glHint(uint target, uint mode);

		/// <summary>
		/// Control the writing of individual bits in the color index buffers
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glIndexMask(uint mask);

		/// <summary>
		/// Define an array of color indexes
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glIndexPointer(uint type, int stride,  void *pointer);

		/// <summary>
		/// Set the current color index
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glIndexd(double c);

		/// <summary>
		/// Set the current color index
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glIndexdv( double *c);

		/// <summary>
		/// Set the current color index
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glIndexf(float c);

		/// <summary>
		/// Set the current color index
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glIndexfv( float *c);

		/// <summary>
		/// Set the current color index
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glIndexi(int c);

		/// <summary>
		/// Set the current color index
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glIndexiv( int *c);

		/// <summary>
		/// Set the current color index
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glIndexs(short c);

		/// <summary>
		/// Set the current color index
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glIndexsv( short *c);

		/// <summary>
		/// Set the current color index
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glIndexub(byte c);

		/// <summary>
		/// Set the current color index
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glIndexubv( byte *c);

		/// <summary>
		/// Initialize the name stack
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glInitNames();

		/// <summary>
		/// Simultaneously specify and enable several interleaved arrays
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glInterleavedArrays(uint format, int stride,  void *pointer);

		/// <summary>
		/// Test whether a capability is enabled
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern byte glIsEnabled(uint cap);

		/// <summary>
		/// Determine if a name corresponds to a display-list
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern byte glIsList(uint list);

		/// <summary>
		/// Determine if a name corresponds to a texture
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern byte glIsTexture(uint texture);

		/// <summary>
		/// Set the lighting mesh parameters
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glLightModelf(uint pname, float param);

		/// <summary>
		/// Set the lighting mesh parameters
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glLightModelfv(uint pname,  float *someParams);

		/// <summary>
		/// Set the lighting mesh parameters
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glLightModeli(uint pname, int param);

		/// <summary>
		/// Set the lighting mesh parameters
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glLightModeliv(uint pname,  int *someParams);

		/// <summary>
		/// Set light source parameters
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glLightf(uint light, uint pname, float param);

		/// <summary>
		/// Set light source parameters
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glLightfv(uint light, uint pname,  float *someParams);

		/// <summary>
		/// Set light source parameters
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glLighti(uint light, uint pname, int param);

		/// <summary>
		/// Set light source parameters
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glLightiv(uint light, uint pname,  int *someParams);

		/// <summary>
		/// Specify the line stipple pattern
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glLineStipple(int factor, ushort pattern);

		/// <summary>
		/// Specify the width of rasterized lines
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glLineWidth(float width);

		/// <summary>
		/// Set the display-list base for <see cref="glCallLists" />
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glListBase(uint aBase);

		/// <summary>
		/// Replace the current matrix with the identity matrix
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glLoadIdentity();

		/// <summary>
		/// Replace the current matrix with the specified matrix
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glLoadMatrixd(double *m);

		/// <summary>
		/// Replace the current matrix with the specified matrix
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glLoadMatrixf(float *m);

		/// <summary>
		/// Load a name onto the name stack
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glLoadName(uint name);

		/// <summary>
		/// Specify a logical pixel operation for color index rendering
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glLogicOp(uint opcode);

		/// <summary>
		/// Define a one-dimensional evaluator
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glMap1d(uint target, double u1, double u2, int stride, int order,  double *points);

		/// <summary>
		/// Define a one-dimensional evaluator
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glMap1f(uint target, float u1, float u2, int stride, int order,  float *points);

		/// <summary>
		/// Define a two-dimensional evaluator
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glMap2d(uint target, double u1, double u2, int ustride, int uorder, double v1, double v2, int vstride, int vorder,  double *points);

		/// <summary>
		/// Define a two-dimensional evaluator
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glMap2f(uint target, float u1, float u2, int ustride, int uorder, float v1, float v2, int vstride, int vorder,  float *points);

		/// <summary>
		/// Define a one-dimensional mesh
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glMapGrid1d(int un, double u1, double u2);

		/// <summary>
		/// Define a one-dimensional mesh
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glMapGrid1f(int un, float u1, float u2);

		/// <summary>
		/// Define a two-dimensional mesh
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glMapGrid2d(int un, double u1, double u2, int vn, double v1, double v2);

		/// <summary>
		/// Define a two-dimensional mesh
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glMapGrid2f(int un, float u1, float u2, int vn, float v1, float v2);

		/// <summary>
		/// Specify material parameters for the lighting mesh
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glMaterialf(uint face, uint pname, float param);

		/// <summary>
		/// Specify material parameters for the lighting mesh
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glMaterialfv(uint face, uint pname,  float *someParams);

		/// <summary>
		/// Specify material parameters for the lighting mesh
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glMateriali(uint face, uint pname, int param);

		/// <summary>
		/// Specify material parameters for the lighting mesh
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glMaterialiv(uint face, uint pname,  int *someParams);

		/// <summary>
		/// Specify which matrix is the current matrix
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glMatrixMode(uint mode);

		/// <summary>
		/// Multiply the current matrix with the specified matrix
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glMultMatrixd( double *m);

		/// <summary>
		/// Multiply the current matrix with the specified matrix
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glMultMatrixf( float *m);

		/// <summary>
		/// Create or replace a display list
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glNewList(uint list, uint mode);

		/// <summary>
		/// Set the current normal vector
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glNormal3b(sbyte nx, sbyte ny, sbyte nz);

		/// <summary>
		/// Set the current normal vector
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glNormal3bv( sbyte *v);

		/// <summary>
		/// Set the current normal vector
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glNormal3d(double nx, double ny, double nz);

		/// <summary>
		/// Set the current normal vector
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glNormal3dv( double *v);

		/// <summary>
		/// Set the current normal vector
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glNormal3f(float nx, float ny, float nz);

		/// <summary>
		/// Set the current normal vector
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glNormal3fv( float *v);

		/// <summary>
		/// Set the current normal vector
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glNormal3i(int nx, int ny, int nz);

		/// <summary>
		/// Set the current normal vector
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glNormal3iv( int *v);

		/// <summary>
		/// Set the current normal vector
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glNormal3s(short nx, short ny, short nz);

		/// <summary>
		/// Set the current normal vector
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glNormal3sv( short *v);

		/// <summary>
		/// Define an array of normals
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glNormalPointer(uint type, int stride,  void *pointer);

		/// <summary>
		/// Multiply the current matrix with an orthographic matrix
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glOrtho(double left, double right, double bottom, double top, double zNear, double zFar);

		/// <summary>
		/// Place a marker in the feedback buffer
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glPassThrough(float token);

		/// <summary>
		/// Set up pixel transfer maps
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glPixelMapfv(uint map, int mapsize,  float *values);

		/// <summary>
		/// Set up pixel transfer maps
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glPixelMapuiv(uint map, int mapsize,  uint *values);

		/// <summary>
		/// Set up pixel transfer maps
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glPixelMapusv(uint map, int mapsize,  ushort *values);

		/// <summary>
		/// Set pixel storage modes
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glPixelStoref(uint pname, float param);

		/// <summary>
		/// Set pixel storage modes
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glPixelStorei(uint pname, int param);

		/// <summary>
		/// Set pixel transfer modes
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glPixelTransferf(uint pname, float param);

		/// <summary>
		/// Set pixel transfer modes
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glPixelTransferi(uint pname, int param);

		/// <summary>
		/// Specify the pixel zoom factors
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glPixelZoom(float xfactor, float yfactor);

		/// <summary>
		/// Specify the diameter of rasterized points
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glPointSize(float size);

		/// <summary>
		/// Select a polygon rasterization mode
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glPolygonMode(uint face, uint mode);

		/// <summary>
		/// Set the scale and units used to calculate depth values
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glPolygonOffset(float factor, float units);

		/// <summary>
		/// Set the polygon stippling pattern
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glPolygonStipple( byte *mask);

		/// <summary>
		/// Pop the server attribute stack
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glPopAttrib();

		/// <summary>
		/// Pop the client attribute stack
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glPopClientAttrib();

		/// <summary>
		/// Pop the current matrix stack
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glPopMatrix();

		/// <summary>
		/// Pop the name stack
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glPopName();

		/// <summary>
		/// Set texture residence priority
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glPrioritizeTextures(int n,  uint *textures,  float *priorities);

		/// <summary>
		/// Push the server attribute stack
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glPushAttrib(uint mask);

		/// <summary>
		/// Push the client attribute stack
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glPushClientAttrib(uint mask);

		/// <summary>
		/// Push the current matrix stack
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glPushMatrix();

		/// <summary>
		/// Push the name stack
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glPushName(uint name);

		/// <summary>
		/// Specify the raster position for pixel operations
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glRasterPos2d(double x, double y);

		/// <summary>
		/// Specify the raster position for pixel operations
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glRasterPos2dv( double *v);

		/// <summary>
		/// Specify the raster position for pixel operations
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glRasterPos2f(float x, float y);

		/// <summary>
		/// Specify the raster position for pixel operations
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glRasterPos2fv( float *v);

		/// <summary>
		/// Specify the raster position for pixel operations
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glRasterPos2i(int x, int y);

		/// <summary>
		/// Specify the raster position for pixel operations
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glRasterPos2iv( int *v);

		/// <summary>
		/// Specify the raster position for pixel operations
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glRasterPos2s(short x, short y);

		/// <summary>
		/// Specify the raster position for pixel operations
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glRasterPos2sv( short *v);

		/// <summary>
		/// Specify the raster position for pixel operations
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glRasterPos3d(double x, double y, double z);

		/// <summary>
		/// Specify the raster position for pixel operations
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glRasterPos3dv( double *v);

		/// <summary>
		/// Specify the raster position for pixel operations
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glRasterPos3f(float x, float y, float z);

		/// <summary>
		/// Specify the raster position for pixel operations
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glRasterPos3fv( float *v);

		/// <summary>
		/// Specify the raster position for pixel operations
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glRasterPos3i(int x, int y, int z);

		/// <summary>
		/// Specify the raster position for pixel operations
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glRasterPos3iv( int *v);

		/// <summary>
		/// Specify the raster position for pixel operations
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glRasterPos3s(short x, short y, short z);

		/// <summary>
		/// Specify the raster position for pixel operations
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glRasterPos3sv( short *v);

		/// <summary>
		/// Specify the raster position for pixel operations
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glRasterPos4d(double x, double y, double z, double w);

		/// <summary>
		/// Specify the raster position for pixel operations
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glRasterPos4dv( double *v);

		/// <summary>
		/// Specify the raster position for pixel operations
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glRasterPos4f(float x, float y, float z, float w);

		/// <summary>
		/// Specify the raster position for pixel operations
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glRasterPos4fv( float *v);

		/// <summary>
		/// Specify the raster position for pixel operations
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glRasterPos4i(int x, int y, int z, int w);

		/// <summary>
		/// Specify the raster position for pixel operations
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glRasterPos4iv( int *v);

		/// <summary>
		/// Specify the raster position for pixel operations
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glRasterPos4s(short x, short y, short z, short w);

		/// <summary>
		/// Specify the raster position for pixel operations
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glRasterPos4sv( short *v);

		/// <summary>
		/// Select a color buffer source for pixels
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glReadBuffer(uint mode);

		/// <summary>
		/// Read a block of pixels from the frame buffer
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glReadPixels(int x, int y, int width, int height, uint format, uint type, void *pixels);

		/// <summary>
		/// Draw a rectangle
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glRectd(double x1, double y1, double x2, double y2);

		/// <summary>
		/// Draw a rectangle
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glRectdv( double *v1,  double *v2);

		/// <summary>
		/// Draw a rectangle
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glRectf(float x1, float y1, float x2, float y2);

		/// <summary>
		/// Draw a rectangle
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glRectfv( float *v1,  float *v2);

		/// <summary>
		/// Draw a rectangle
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glRecti(int x1, int y1, int x2, int y2);

		/// <summary>
		/// Draw a rectangle
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glRectiv( int *v1,  int *v2);

		/// <summary>
		/// Draw a rectangle
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glRects(short x1, short y1, short x2, short y2);

		/// <summary>
		/// Draw a rectangle
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glRectsv( short *v1,  short *v2);

		/// <summary>
		/// Set rasterization mode
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern int glRenderMode(uint mode);

		/// <summary>
		/// Multiply the current matrix by a rotation matrix
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glRotated(double angle, double x, double y, double z);

		/// <summary>
		/// Multiply the current matrix by a rotation matrix
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glRotatef(float angle, float x, float y, float z);

		/// <summary>
		/// Multiply the current matrix by a general scaling matrix
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glScaled(double x, double y, double z);

		/// <summary>
		/// Multiply the current matrix by a general scaling matrix
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glScalef(float x, float y, float z);

		/// <summary>
		/// Define the scissor box
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glScissor(int x, int y, int width, int height);

		/// <summary>
		/// Establish a buffer for selection mode values
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glSelectBuffer(int size, uint *buffer);

		/// <summary>
		/// Select flat or smooth shading
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glShadeModel(uint mode);

		/// <summary>
		/// Set function and reference value for stencil testing
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glStencilFunc(uint func, int aRef, uint mask);

		/// <summary>
		/// Control the writing of individual bits in the stencil planes
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glStencilMask(uint mask);

		/// <summary>
		/// Set stencil test actions
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glStencilOp(uint fail, uint zfail, uint zpass);

		/// <summary>
		/// Set the current texture coordinates
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexCoord1d(double s);

		/// <summary>
		/// Set the current texture coordinates
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glTexCoord1dv( double *v);

		/// <summary>
		/// Set the current texture coordinates
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexCoord1f(float s);

		/// <summary>
		/// Set the current texture coordinates
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glTexCoord1fv( float *v);

		/// <summary>
		/// Set the current texture coordinates
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexCoord1i(int s);

		/// <summary>
		/// Set the current texture coordinates
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glTexCoord1iv( int *v);

		/// <summary>
		/// Set the current texture coordinates
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexCoord1s(short s);

		/// <summary>
		/// Set the current texture coordinates
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glTexCoord1sv( short *v);

		/// <summary>
		/// Set the current texture coordinates
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexCoord2d(double s, double t);

		/// <summary>
		/// Set the current texture coordinates
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glTexCoord2dv( double *v);

		/// <summary>
		/// Set the current texture coordinates
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexCoord2f(float s, float t);

		/// <summary>
		/// Set the current texture coordinates
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glTexCoord2fv( float *v);

		/// <summary>
		/// Set the current texture coordinates
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexCoord2i(int s, int t);

		/// <summary>
		/// Set the current texture coordinates
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glTexCoord2iv( int *v);

		/// <summary>
		/// Set the current texture coordinates
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexCoord2s(short s, short t);

		/// <summary>
		/// Set the current texture coordinates
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glTexCoord2sv( short *v);

		/// <summary>
		/// Set the current texture coordinates
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexCoord3d(double s, double t, double r);

		/// <summary>
		/// Set the current texture coordinates
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glTexCoord3dv( double *v);

		/// <summary>
		/// Set the current texture coordinates
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexCoord3f(float s, float t, float r);

		/// <summary>
		/// Set the current texture coordinates
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glTexCoord3fv( float *v);

		/// <summary>
		/// Set the current texture coordinates
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexCoord3i(int s, int t, int r);

		/// <summary>
		/// Set the current texture coordinates
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glTexCoord3iv( int *v);

		/// <summary>
		/// Set the current texture coordinates
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexCoord3s(short s, short t, short r);

		/// <summary>
		/// Set the current texture coordinates
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glTexCoord3sv( short *v);

		/// <summary>
		/// Set the current texture coordinates
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexCoord4d(double s, double t, double r, double q);

		/// <summary>
		/// Set the current texture coordinates
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glTexCoord4dv( double *v);

		/// <summary>
		/// Set the current texture coordinates
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexCoord4f(float s, float t, float r, float q);

		/// <summary>
		/// Set the current texture coordinates
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glTexCoord4fv( float *v);

		/// <summary>
		/// Set the current texture coordinates
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexCoord4i(int s, int t, int r, int q);

		/// <summary>
		/// Set the current texture coordinates
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glTexCoord4iv( int *v);

		/// <summary>
		/// Set the current texture coordinates
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexCoord4s(short s, short t, short r, short q);

		/// <summary>
		/// Set the current texture coordinates
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glTexCoord4sv( short *v);

		/// <summary>
		/// Define an array of texture coordinates
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glTexCoordPointer(int size, uint type, int stride,  void *pointer);

		/// <summary>
		/// Set texture environment parameters
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexEnvf(uint target, uint pname, float param);

		/// <summary>
		/// Set texture environment parameters
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glTexEnvfv(uint target, uint pname,  float *someParams);

		/// <summary>
		/// Set texture environment parameters
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexEnvi(uint target, uint pname, uint param);

		/// <summary>
		/// Set texture environment parameters
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glTexEnviv(uint target, uint pname,  int *someParams);

		/// <summary>
		/// Control the generation of texture coordinates
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexGend(uint coord, uint pname, double param);

		/// <summary>
		/// Control the generation of texture coordinates
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glTexGendv(uint coord, uint pname,  double *someParams);

		/// <summary>
		/// Control the generation of texture coordinates
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexGenf(uint coord, uint pname, float param);

		/// <summary>
		/// Control the generation of texture coordinates
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glTexGenfv(uint coord, uint pname,  float *someParams);

		/// <summary>
		/// Control the generation of texture coordinates
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexGeni(uint coord, uint pname, int param);

		/// <summary>
		/// Control the generation of texture coordinates
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glTexGeniv(uint coord, uint pname,  int *someParams);

		/// <summary>
		/// Specify a one-dimensional texture image
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glTexImage1D(uint target, int level, int components, int width, int border, uint format, uint type,  void *pixels);

		/// <summary>
		/// Specify a two-dimensional texture image
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glTexImage2D(uint target, int level, int components, int width, int height, int border, uint format, uint type,  void *pixels);

		/// <summary>
		/// Set texture parameters
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexParameterf(uint target, uint pname, float param);

		/// <summary>
		/// Set texture parameters
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glTexParameterfv(uint target, uint pname,  float *someParams);

		/// <summary>
		/// Set texture parameters
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTexParameteri(uint target, uint pname, int param);

		/// <summary>
		/// Set texture parameters
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glTexParameteriv(uint target, uint pname,  int *someParams);

		/// <summary>
		/// Specify a one-dimensional texture subimage
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glTexSubImage1D(uint target, int level, int xoffset, int width, uint format, uint type,  void *pixels);

		/// <summary>
		/// Specify a two-dimensional texture subimage
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glTexSubImage2D(uint target, int level, int xoffset, int yoffset, int width, int height, uint format, uint type,  void *pixels);

		/// <summary>
		/// Multiply the current matrix by a translation matrix
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTranslated(double x, double y, double z);

		/// <summary>
		/// Multiply the current matrix by a translation matrix
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glTranslatef(float x, float y, float z);

		/// <summary>
		/// Specify a vertex
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glVertex2d(double x, double y);

		/// <summary>
		/// Specify a vertex
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glVertex2dv( double *v);

		/// <summary>
		/// Specify a vertex
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glVertex2f(float x, float y);

		/// <summary>
		/// Specify a vertex
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glVertex2fv( float *v);

		/// <summary>
		/// Specify a vertex
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glVertex2i(int x, int y);

		/// <summary>
		/// Specify a vertex
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glVertex2iv( int *v);

		/// <summary>
		/// Specify a vertex
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glVertex2s(short x, short y);

		/// <summary>
		/// Specify a vertex
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glVertex2sv( short *v);

		/// <summary>
		/// Specify a vertex
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glVertex3d(double x, double y, double z);

		/// <summary>
		/// Specify a vertex
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glVertex3dv( double *v);

		/// <summary>
		/// Specify a vertex
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glVertex3f(float x, float y, float z);

		/// <summary>
		/// Specify a vertex
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glVertex3fv( float *v);

		/// <summary>
		/// Specify a vertex
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glVertex3i(int x, int y, int z);

		/// <summary>
		/// Specify a vertex
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glVertex3iv( int *v);

		/// <summary>
		/// Specify a vertex
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glVertex3s(short x, short y, short z);

		/// <summary>
		/// Specify a vertex
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glVertex3sv( short *v);

		/// <summary>
		/// Specify a vertex
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glVertex4d(double x, double y, double z, double w);

		/// <summary>
		/// Specify a vertex
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glVertex4dv( double *v);

		/// <summary>
		/// Specify a vertex
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glVertex4f(float x, float y, float z, float w);

		/// <summary>
		/// Specify a vertex
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glVertex4fv( float *v);

		/// <summary>
		/// Specify a vertex
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glVertex4i(int x, int y, int z, int w);

		/// <summary>
		/// Specify a vertex
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glVertex4iv( int *v);

		/// <summary>
		/// Specify a vertex
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glVertex4s(short x, short y, short z, short w);

		/// <summary>
		/// Specify a vertex
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glVertex4sv( short *v);

		/// <summary>
		/// Define an array of vertex data
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		unsafe public static extern void glVertexPointer(int size, uint type, int stride,  void *pointer);

		/// <summary>
		/// Set the viewport
		/// </summary>
		[DllImport(OPENGL_LIB, CallingConvention=CallingConvention.Winapi)]
		public static extern void glViewport(int x, int y, int width, int height);
		#endregion OpenGL Methods

		#endregion

		#region OpenGL Constants

		#region AccumOp
		public const uint GL_ACCUM								= 0x0100;
		public const uint GL_LOAD								= 0x0101;
		public const uint GL_RETURN								= 0x0102;
		public const uint GL_MULT								= 0x0103;
		public const uint GL_ADD								= 0x0104;
		#endregion AccumOp

		#region AlphaFunction
		public const uint GL_NEVER								= 0x0200;
		public const uint GL_LESS								= 0x0201;
		public const uint GL_EQUAL								= 0x0202;
		public const uint GL_LEQUAL								= 0x0203;
		public const uint GL_GREATER							= 0x0204;
		public const uint GL_NOTEQUAL							= 0x0205;
		public const uint GL_GEQUAL								= 0x0206;
		public const uint GL_ALWAYS								= 0x0207;
		#endregion AlphaFunction

		#region AttribMask
		public const uint GL_CURRENT_BIT						= 0x00000001;
		public const uint GL_POINT_BIT							= 0x00000002;
		public const uint GL_LINE_BIT							= 0x00000004;
		public const uint GL_POLYGON_BIT						= 0x00000008;
		public const uint GL_POLYGON_STIPPLE_BIT				= 0x00000010;
		public const uint GL_PIXEL_MODE_BIT						= 0x00000020;
		public const uint GL_LIGHTING_BIT						= 0x00000040;
		public const uint GL_FOG_BIT							= 0x00000080;
		public const uint GL_DEPTH_BUFFER_BIT					= 0x00000100;
		public const uint GL_ACCUM_BUFFER_BIT					= 0x00000200;
		public const uint GL_STENCIL_BUFFER_BIT					= 0x00000400;
		public const uint GL_VIEWPORT_BIT						= 0x00000800;
		public const uint GL_TRANSFORM_BIT						= 0x00001000;
		public const uint GL_ENABLE_BIT							= 0x00002000;
		public const uint GL_COLOR_BUFFER_BIT					= 0x00004000;
		public const uint GL_HINT_BIT							= 0x00008000;
		public const uint GL_EVAL_BIT							= 0x00010000;
		public const uint GL_LIST_BIT							= 0x00020000;
		public const uint GL_TEXTURE_BIT						= 0x00040000;
		public const uint GL_SCISSOR_BIT						= 0x00080000;
		public const uint GL_ALL_ATTRIB_BITS					= 0x000FFFFF;
		#endregion AttribMask

		#region BeginMode
		public const uint GL_POINTS								= 0x0000;
		public const uint GL_LINES								= 0x0001;
		public const uint GL_LINE_LOOP							= 0x0002;
		public const uint GL_LINE_STRIP							= 0x0003;
		public const uint GL_TRIANGLES							= 0x0004;
		public const uint GL_TRIANGLE_STRIP						= 0x0005;
		public const uint GL_TRIANGLE_FAN						= 0x0006;
		public const uint GL_QUADS								= 0x0007;
		public const uint GL_QUAD_STRIP							= 0x0008;
		public const uint GL_POLYGON							= 0x0009;
		#endregion BeginMode

		#region BlendingFactorDest
		public const uint GL_ZERO								= 0;
		public const uint GL_ONE								= 1;
		public const uint GL_SRC_COLOR							= 0x0300;
		public const uint GL_ONE_MINUS_SRC_COLOR				= 0x0301;
		public const uint GL_SRC_ALPHA							= 0x0302;
		public const uint GL_ONE_MINUS_SRC_ALPHA				= 0x0303;
		public const uint GL_DST_ALPHA							= 0x0304;
		public const uint GL_ONE_MINUS_DST_ALPHA				= 0x0305;
		#endregion BlendingFactorDest

		#region BlendingFactorSrc
		/*      GL_ZERO */
		/*      GL_ONE */
		public const uint GL_DST_COLOR							= 0x0306;
		public const uint GL_ONE_MINUS_DST_COLOR				= 0x0307;
		public const uint GL_SRC_ALPHA_SATURATE					= 0x0308;
		/*      GL_SRC_ALPHA */
		/*      GL_ONE_MINUS_SRC_ALPHA */
		/*      GL_DST_ALPHA */
		/*      GL_ONE_MINUS_DST_ALPHA */
		#endregion BlendingFactorSrc

		#region Boolean
		public const uint GL_TRUE								= 1;
		public const uint GL_FALSE								= 0;
		#endregion Boolean

		#region ClearBufferMask
		/*      GL_COLOR_BUFFER_BIT */
		/*      GL_ACCUM_BUFFER_BIT */
		/*      GL_STENCIL_BUFFER_BIT */
		/*      GL_DEPTH_BUFFER_BIT */
		#endregion ClearBufferMask

		#region ClientArrayType
		/*      GL_VERTEX_ARRAY */
		/*      GL_NORMAL_ARRAY */
		/*      GL_COLOR_ARRAY */
		/*      GL_INDEX_ARRAY */
		/*      GL_TEXTURE_COORD_ARRAY */
		/*      GL_EDGE_FLAG_ARRAY */
		#endregion ClientArrayType

		#region ClipPlaneName
		#region GL_CLIP_PLANE0
		/// <summary>
		/// User clipping plane coefficients OR 0th user clipping plane enabled
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: transform OR transform/enable
		/// </para>
		/// <para>
		/// Initial value: 0,0,0,0 OR <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetClipPlane" /> OR <see cref="glIsEnabled" />
		/// </para>
		/// </remarks>
		public const uint GL_CLIP_PLANE0						= 0x3000;
		#endregion GL_CLIP_PLANE0

		#region GL_CLIP_PLANE1
		/// <summary>
		/// User clipping plane coefficients OR 1st user clipping plane enabled
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: transform OR transform/enable
		/// </para>
		/// <para>
		/// Initial value: 0,0,0,0 OR <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetClipPlane" /> OR <see cref="glIsEnabled" />
		/// </para>
		/// </remarks>
		public const uint GL_CLIP_PLANE1						= 0x3001;
		#endregion GL_CLIP_PLANE1

		#region GL_CLIP_PLANE2
		/// <summary>
		/// User clipping plane coefficients OR 2nd user clipping plane enabled
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: transform OR transform/enable
		/// </para>
		/// <para>
		/// Initial value: 0,0,0,0 OR <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetClipPlane" /> OR <see cref="glIsEnabled" />
		/// </para>
		/// </remarks>
		public const uint GL_CLIP_PLANE2						= 0x3002;
		#endregion GL_CLIP_PLANE2

		#region GL_CLIP_PLANE3
		/// <summary>
		/// User clipping plane coefficients OR 3rd user clipping plane enabled
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: transform OR transform/enable
		/// </para>
		/// <para>
		/// Initial value: 0,0,0,0 OR <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetClipPlane" /> OR <see cref="glIsEnabled" />
		/// </para>
		/// </remarks>
		public const uint GL_CLIP_PLANE3						= 0x3003;
		#endregion GL_CLIP_PLANE3

		#region GL_CLIP_PLANE4
		/// <summary>
		/// User clipping plane coefficients OR 4th user clipping plane enabled
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: transform OR transform/enable
		/// </para>
		/// <para>
		/// Initial value: 0,0,0,0 OR <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetClipPlane" /> OR <see cref="glIsEnabled" />
		/// </para>
		/// </remarks>
		public const uint GL_CLIP_PLANE4						= 0x3004;
		#endregion GL_CLIP_PLANE4

		#region GL_CLIP_PLANE5
		/// <summary>
		/// User clipping plane coefficients OR 5th user clipping plane enabled
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: transform OR transform/enable
		/// </para>
		/// <para>
		/// Initial value: 0,0,0,0 OR <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetClipPlane" /> OR <see cref="glIsEnabled" />
		/// </para>
		/// </remarks>
		public const uint GL_CLIP_PLANE5						= 0x3005;
		#endregion GL_CLIP_PLANE5
		#endregion ClipPlaneName

		#region ColorMaterialFace
		/*      GL_FRONT */
		/*      GL_BACK */
		/*      GL_FRONT_AND_BACK */
		#endregion ColorMaterialFace

		#region ColorMaterialParameter
		/*      GL_AMBIENT */
		/*      GL_DIFFUSE */
		/*      GL_SPECULAR */
		/*      GL_EMISSION */
		/*      GL_AMBIENT_AND_DIFFUSE */
		#endregion ColorMaterialParameter

		#region ColorPointerType
		/*      GL_BYTE */
		/*      GL_UNSIGNED_BYTE */
		/*      GL_SHORT */
		/*      GL_UNSIGNED_SHORT */
		/*      GL_INT */
		/*      GL_UNSIGNED_INT */
		/*      GL_FLOAT */
		/*      GL_DOUBLE */
		#endregion ColorPointerType

		#region CullFaceMode
		/*      GL_FRONT */
		/*      GL_BACK */
		/*      GL_FRONT_AND_BACK */
		#endregion CullFaceMode

		#region DataType
		public const uint GL_BYTE								= 0x1400;
		public const uint GL_UNSIGNED_BYTE						= 0x1401;
		public const uint GL_SHORT								= 0x1402;
		public const uint GL_UNSIGNED_SHORT						= 0x1403;
		public const uint GL_INT								= 0x1404;
		public const uint GL_UNSIGNED_INT						= 0x1405;
		public const uint GL_FLOAT								= 0x1406;
		public const uint GL_2_BYTES							= 0x1407;
		public const uint GL_3_BYTES							= 0x1408;
		public const uint GL_4_BYTES							= 0x1409;
		public const uint GL_DOUBLE								= 0x140A;
		#endregion DataType

		#region DepthFunction
		/*      GL_NEVER */
		/*      GL_LESS */
		/*      GL_EQUAL */
		/*      GL_LEQUAL */
		/*      GL_GREATER */
		/*      GL_NOTEQUAL */
		/*      GL_GEQUAL */
		/*      GL_ALWAYS */
		#endregion DepthFunction

		#region DrawBufferMode
		public const uint GL_NONE								= 0;
		public const uint GL_FRONT_LEFT							= 0x0400;
		public const uint GL_FRONT_RIGHT						= 0x0401;
		public const uint GL_BACK_LEFT							= 0x0402;
		public const uint GL_BACK_RIGHT							= 0x0403;
		public const uint GL_FRONT								= 0x0404;
		public const uint GL_BACK								= 0x0405;
		public const uint GL_LEFT								= 0x0406;
		public const uint GL_RIGHT								= 0x0407;
		public const uint GL_FRONT_AND_BACK						= 0x0408;
		public const uint GL_AUX0								= 0x0409;
		public const uint GL_AUX1								= 0x040A;
		public const uint GL_AUX2								= 0x040B;
		public const uint GL_AUX3								= 0x040C;

		#endregion DrawBufferMode

		#region Enable
		/*      GL_FOG */
		/*      GL_LIGHTING */
		/*      GL_TEXTURE_1D */
		/*      GL_TEXTURE_2D */
		/*      GL_LINE_STIPPLE */
		/*      GL_POLYGON_STIPPLE */
		/*      GL_CULL_FACE */
		/*      GL_ALPHA_TEST */
		/*      GL_BLEND */
		/*      GL_INDEX_LOGIC_OP */
		/*      GL_COLOR_LOGIC_OP */
		/*      GL_DITHER */
		/*      GL_STENCIL_TEST */
		/*      GL_DEPTH_TEST */
		/*      GL_CLIP_PLANE0 */
		/*      GL_CLIP_PLANE1 */
		/*      GL_CLIP_PLANE2 */
		/*      GL_CLIP_PLANE3 */
		/*      GL_CLIP_PLANE4 */
		/*      GL_CLIP_PLANE5 */
		/*      GL_LIGHT0 */
		/*      GL_LIGHT1 */
		/*      GL_LIGHT2 */
		/*      GL_LIGHT3 */
		/*      GL_LIGHT4 */
		/*      GL_LIGHT5 */
		/*      GL_LIGHT6 */
		/*      GL_LIGHT7 */
		/*      GL_TEXTURE_GEN_S */
		/*      GL_TEXTURE_GEN_T */
		/*      GL_TEXTURE_GEN_R */
		/*      GL_TEXTURE_GEN_Q */
		/*      GL_MAP1_VERTEX_3 */
		/*      GL_MAP1_VERTEX_4 */
		/*      GL_MAP1_COLOR_4 */
		/*      GL_MAP1_INDEX */
		/*      GL_MAP1_NORMAL */
		/*      GL_MAP1_TEXTURE_COORD_1 */
		/*      GL_MAP1_TEXTURE_COORD_2 */
		/*      GL_MAP1_TEXTURE_COORD_3 */
		/*      GL_MAP1_TEXTURE_COORD_4 */
		/*      GL_MAP2_VERTEX_3 */
		/*      GL_MAP2_VERTEX_4 */
		/*      GL_MAP2_COLOR_4 */
		/*      GL_MAP2_INDEX */
		/*      GL_MAP2_NORMAL */
		/*      GL_MAP2_TEXTURE_COORD_1 */
		/*      GL_MAP2_TEXTURE_COORD_2 */
		/*      GL_MAP2_TEXTURE_COORD_3 */
		/*      GL_MAP2_TEXTURE_COORD_4 */
		/*      GL_POINT_SMOOTH */
		/*      GL_LINE_SMOOTH */
		/*      GL_POLYGON_SMOOTH */
		/*      GL_SCISSOR_TEST */
		/*      GL_COLOR_MATERIAL */
		/*      GL_NORMALIZE */
		/*      GL_AUTO_NORMAL */
		/*      GL_VERTEX_ARRAY */
		/*      GL_NORMAL_ARRAY */
		/*      GL_COLOR_ARRAY */
		/*      GL_INDEX_ARRAY */
		/*      GL_TEXTURE_COORD_ARRAY */
		/*      GL_EDGE_FLAG_ARRAY */
		/*      GL_POLYGON_OFFSET_POINT */
		/*      GL_POLYGON_OFFSET_LINE */
		/*      GL_POLYGON_OFFSET_FILL */
		#endregion Enable

		#region ErrorCode
		public const uint GL_NO_ERROR							= 0;
		public const uint GL_INVALID_ENUM						= 0x0500;
		public const uint GL_INVALID_VALUE						= 0x0501;
		public const uint GL_INVALID_OPERATION					= 0x0502;
		public const uint GL_STACK_OVERFLOW						= 0x0503;
		public const uint GL_STACK_UNDERFLOW					= 0x0504;
		public const uint GL_OUT_OF_MEMORY						= 0x0505;
		/*      GL_TABLE_TOO_LARGE_EXT */
		#endregion ErrorCode

		#region FeedBackMode
		public const uint GL_2D									= 0x0600;
		public const uint GL_3D									= 0x0601;
		public const uint GL_3D_COLOR							= 0x0602;
		public const uint GL_3D_COLOR_TEXTURE					= 0x0603;
		public const uint GL_4D_COLOR_TEXTURE					= 0x0604;
		#endregion FeedBackMode

		#region FeedBackToken
		public const uint GL_PASS_THROUGH_TOKEN					= 0x0700;
		public const uint GL_POINT_TOKEN						= 0x0701;
		public const uint GL_LINE_TOKEN							= 0x0702;
		public const uint GL_POLYGON_TOKEN						= 0x0703;
		public const uint GL_BITMAP_TOKEN						= 0x0704;
		public const uint GL_DRAW_PIXEL_TOKEN					= 0x0705;
		public const uint GL_COPY_PIXEL_TOKEN					= 0x0706;
		public const uint GL_LINE_RESET_TOKEN					= 0x0707;
		#endregion FeedBackToken

		#region FogMode
		/*      GL_LINEAR */
		public const uint GL_EXP								= 0x0800;
		public const uint GL_EXP2								= 0x0801;
		#endregion FogMode

		#region FogParameter
		/*      GL_FOG_COLOR */
		/*      GL_FOG_DENSITY */
		/*      GL_FOG_END */
		/*      GL_FOG_INDEX */
		/*      GL_FOG_MODE */
		/*      GL_FOG_START */
		#endregion FogParameter

		#region FrontFaceDirection
		public const uint GL_CW									= 0x0900;
		public const uint GL_CCW								= 0x0901;
		#endregion FrontFaceDirection

		#region GetMapTarget
		#region GL_COEFF
		/// <summary>
		/// 1-D control points OR 2-D control points
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: -- OR --
		/// </para>
		/// <para>
		/// Initial value: -- OR --
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetMapfv" /> OR <see cref="glGetMapfv" />
		/// </para>
		/// </remarks>
		public const uint GL_COEFF								= 0x0A00;
		#endregion GL_COEFF
		
		#region GL_ORDER
		/// <summary>
		/// 1-D map order OR 2-D map orders
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: -- OR --
		/// </para>
		/// <para>
		/// Initial value: 1 OR 1, 1
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetMapiv" /> OR <see cref="glGetMapiv" />
		/// </para>
		/// </remarks>
		public const uint GL_ORDER								= 0x0A01;
		#endregion GL_ORDER

		#region GL_DOMAIN
		/// <summary>
		/// 1-D domain endpoints OR 2-D domain endpoints
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: -- OR --
		/// </para>
		/// <para>
		/// Initial value: -- OR --
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetMapfv" /> OR <see cref="glGetMapfv" />
		/// </para>
		/// </remarks>
		public const uint GL_DOMAIN								= 0x0A02;
		#endregion GL_DOMAIN
		#endregion GetMapTarget

		#region GetPixelMap
		/*      GL_PIXEL_MAP_I_TO_I */
		/*      GL_PIXEL_MAP_S_TO_S */
		/*      GL_PIXEL_MAP_I_TO_R */
		/*      GL_PIXEL_MAP_I_TO_G */
		/*      GL_PIXEL_MAP_I_TO_B */
		/*      GL_PIXEL_MAP_I_TO_A */
		/*      GL_PIXEL_MAP_R_TO_R */
		/*      GL_PIXEL_MAP_G_TO_G */
		/*      GL_PIXEL_MAP_B_TO_B */
		/*      GL_PIXEL_MAP_A_TO_A */
		#endregion GetPixelMap

		#region GetPointerTarget
		/*      GL_VERTEX_ARRAY_POINTER */
		/*      GL_NORMAL_ARRAY_POINTER */
		/*      GL_COLOR_ARRAY_POINTER */
		/*      GL_INDEX_ARRAY_POINTER */
		/*      GL_TEXTURE_COORD_ARRAY_POINTER */
		/*      GL_EDGE_FLAG_ARRAY_POINTER */
		#endregion GetPointerTarget

		#region GetTarget
		#region GL_CURRENT_COLOR
		/// <summary>
		/// Current color
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: current
		/// </para>
		/// <para>
		/// Initial value: 1,1,1,1
		/// </para>
		/// <para>
		/// Get commands: <see cref="glGetIntegerv" />, <see cref="glGetFloatv" />
		/// </para>
		/// </remarks>
		public const uint GL_CURRENT_COLOR						= 0x0B00;
		#endregion GL_CURRENT_COLOR

		#region GL_CURRENT_INDEX
		/// <summary>
		/// Current color index
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: current
		/// </para>
		/// <para>
		/// Initial value: 1
		/// </para>
		/// <para>
		/// Get commands: <see cref="glGetIntegerv" />, <see cref="glGetFloatv" />
		/// </para>
		/// </remarks>
		public const uint GL_CURRENT_INDEX						= 0x0B01;
		#endregion GL_CURRENT_INDEX

		#region GL_CURRENT_NORMAL
		/// <summary>
		/// Current normal
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: current
		/// </para>
		/// <para>
		/// Initial value: 0,0,1
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetFloatv" />
		/// </para>
		/// </remarks>
		public const uint GL_CURRENT_NORMAL						= 0x0B02;
		#endregion GL_CURRENT_NORMAL

		#region GL_CURRENT_TEXTURE_COORDS
		/// <summary>
		/// Current texture coordinates
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: current
		/// </para>
		/// <para>
		/// Initial value: 0,0,0,1
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetFloatv" />
		/// </para>
		/// </remarks>
		public const uint GL_CURRENT_TEXTURE_COORDS				= 0x0B03;
		#endregion GL_CURRENT_TEXTURE_COORDS

		#region GL_CURRENT_RASTER_COLOR
		/// <summary>
		/// Color associated with raster position
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: current
		/// </para>
		/// <para>
		/// Initial value: 1,1,1,1
		/// </para>
		/// <para>
		/// Get commands: <see cref="glGetIntegerv" />, <see cref="glGetFloatv" />
		/// </para>
		/// </remarks>
		public const uint GL_CURRENT_RASTER_COLOR				= 0x0B04;
		#endregion GL_CURRENT_RASTER_COLOR

		#region GL_CURRENT_RASTER_INDEX
		/// <summary>
		/// Color index associated with raster position
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: current
		/// </para>
		/// <para>
		/// Initial value: 1
		/// </para>
		/// <para>
		/// Get commands: <see cref="glGetIntegerv" />, <see cref="glGetFloatv" />
		/// </para>
		/// </remarks>
		public const uint GL_CURRENT_RASTER_INDEX				= 0x0B05;
		#endregion GL_CURRENT_RASTER_INDEX

		#region GL_CURRENT_RASTER_TEXTURE_COORDS
		/// <summary>
		/// Texture coordinates associated with raster position
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: current
		/// </para>
		/// <para>
		/// Initial value: 0,0,0,1
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetFloatv" />
		/// </para>
		/// </remarks>
		public const uint GL_CURRENT_RASTER_TEXTURE_COORDS		= 0x0B06;
		#endregion GL_CURRENT_RASTER_TEXTURE_COORDS

		#region GL_CURRENT_RASTER_POSITION
		/// <summary>
		/// Current raster position
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: current
		/// </para>
		/// <para>
		/// Initial value: 0,0,0,1
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetFloatv" />
		/// </para>
		/// </remarks>
		public const uint GL_CURRENT_RASTER_POSITION			= 0x0B07;
		#endregion GL_CURRENT_RASTER_POSITION

		#region GL_CURRENT_RASTER_POSITION_VALID
		/// <summary>
		/// Raster position valid bit
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: current
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_TRUE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetBooleanv" />
		/// </para>
		/// </remarks>
		public const uint GL_CURRENT_RASTER_POSITION_VALID		= 0x0B08;
		#endregion GL_CURRENT_RASTER_POSITION_VALID

		#region GL_CURRENT_RASTER_DISTANCE
		/// <summary>
		/// Current raster distance
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: current
		/// </para>
		/// <para>
		/// Initial value: 0
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetFloatv" />
		/// </para>
		/// </remarks>
		public const uint GL_CURRENT_RASTER_DISTANCE			= 0x0B09;
		#endregion GL_CURRENT_RASTER_DISTANCE

		#region GL_POINT_SMOOTH
		/// <summary>
		/// Point aliasing on
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: point/enable
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glIsEnabled" />
		/// </para>
		/// </remarks>
		public const uint GL_POINT_SMOOTH						= 0x0B10;
		#endregion GL_POINT_SMOOTH

		#region GL_POINT_SIZE
		/// <summary>
		/// Point size
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: point
		/// </para>
		/// <para>
		/// Initial value: 1.0
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetFloatv" />
		/// </para>
		/// </remarks>
		public const uint GL_POINT_SIZE							= 0x0B11;
		#endregion GL_POINT_SIZE

		#region GL_POINT_SIZE_RANGE
		/// <summary>
		/// Range (low to high) of antialiased point sizes
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: 1, 1
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetFloatv" />
		/// </para>
		/// </remarks>
		public const uint GL_POINT_SIZE_RANGE					= 0x0B12;
		#endregion GL_POINT_SIZE_RANGE

		#region GL_POINT_SIZE_GRANULARITY
		/// <summary>
		/// Antialiased point size granularity
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: --
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetFloatv" />
		/// </para>
		/// </remarks>
		public const uint GL_POINT_SIZE_GRANULARITY				= 0x0B13;
		#endregion GL_POINT_SIZE_GRANULARITY

		#region GL_LINE_SMOOTH
		/// <summary>
		/// Line antialiasing on
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: line/enable
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glIsEnabled" />
		/// </para>
		/// </remarks>
		public const uint GL_LINE_SMOOTH						= 0x0B20;
		#endregion GL_LINE_SMOOTH

		#region GL_LINE_WIDTH
		/// <summary>
		/// Line width
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: line
		/// </para>
		/// <para>
		/// Initial value: 1.0
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetFloatv" />
		/// </para>
		/// </remarks>
		public const uint GL_LINE_WIDTH							= 0x0B21;
		#endregion GL_LINE_WIDTH

		#region GL_LINE_WIDTH_RANGE
		/// <summary>
		/// Range (low to high) of antialiased line widths
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: 1, 1
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetFloatv" />
		/// </para>
		/// </remarks>
		public const uint GL_LINE_WIDTH_RANGE					= 0x0B22;
		#endregion GL_LINE_WIDTH_RANGE

		#region GL_LINE_WIDTH_GRANULARITY
		/// <summary>
		/// Antialiased line-width granularity
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: --
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetFloatv" />
		/// </para>
		/// </remarks>
		public const uint GL_LINE_WIDTH_GRANULARITY				= 0x0B23;
		#endregion GL_LINE_WIDTH_GRANULARITY

		#region GL_LINE_STIPPLE
		/// <summary>
		/// Line stipple enable
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: line/enable
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glIsEnabled" />
		/// </para>
		/// </remarks>
		public const uint GL_LINE_STIPPLE						= 0x0B24;
		#endregion GL_LINE_STIPPLE

		#region GL_LINE_STIPPLE_PATTERN
		/// <summary>
		/// Line stipple
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: line
		/// </para>
		/// <para>
		/// Initial value: 1's
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_LINE_STIPPLE_PATTERN				= 0x0B25;
		#endregion GL_LINE_STIPPLE_PATTERN

		#region GL_LINE_STIPPLE_REPEAT
		/// <summary>
		/// Line stipple repeat
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: line
		/// </para>
		/// <para>
		/// Initial value: 1
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_LINE_STIPPLE_REPEAT				= 0x0B26;
		#endregion GL_LINE_STIPPLE_REPEAT

		#region GL_LIST_MODE
		/// <summary>
		/// Mode of display list under construction; undefined if none
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: 0
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_LIST_MODE							= 0x0B30;
		#endregion GL_LIST_MODE
		
		#region GL_MAX_LIST_NESTING
		/// <summary>
		/// Maximum display-list call nesting
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: 64
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_MAX_LIST_NESTING					= 0x0B31;
		#endregion GL_MAX_LIST_NESTING

		#region GL_LIST_BASE
		/// <summary>
		/// Setting of <see cref="glListBase" />
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: list
		/// </para>
		/// <para>
		/// Initial value: 0
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_LIST_BASE							= 0x0B32;
		#endregion GL_LIST_BASE

		#region GL_LIST_INDEX
		/// <summary>
		/// Number of display lists under construction; 0 if none
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: 0
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_LIST_INDEX							= 0x0B33;
		#endregion GL_LIST_INDEX

		#region GL_POLYGON_MODE
		/// <summary>
		/// Polygon rasterization mode (front and back)
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: polygon
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_FILL" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_POLYGON_MODE						= 0x0B40;
		#endregion GL_POLYGON_MODE

		#region GL_POLYGON_SMOOTH
		/// <summary>
		/// Polygon antialiasing on
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: polygon/enable
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glIsEnabled" />
		/// </para>
		/// </remarks>
		public const uint GL_POLYGON_SMOOTH						= 0x0B41;
		#endregion GL_POLYGON_SMOOTH

		#region GL_POLYGON_STIPPLE
		/// <summary>
		/// Polygon stipple enable OR Polygon stipple pattern
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: polygon/enable OR polygon-stipple
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_FALSE" /> OR 1's
		/// </para>
		/// <para>
		/// Get command: <see cref="glIsEnabled" /> OR <see cref="glGetPolygonStipple" />
		/// </para>
		/// </remarks>
		public const uint GL_POLYGON_STIPPLE					= 0x0B42;
		#endregion GL_POLYGON_STIPPLE

		#region GL_EDGE_FLAG
		/// <summary>
		/// Edge flag
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: current
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_TRUE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetBooleanv" />
		/// </para>
		/// </remarks>
		public const uint GL_EDGE_FLAG							= 0x0B43;
		#endregion GL_EDGE_FLAG

		#region GL_CULL_FACE
		/// <summary>
		/// Polygon culling enabled
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: polygon/enable
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glIsEnabled" />
		/// </para>
		/// </remarks>
		public const uint GL_CULL_FACE							= 0x0B44;
		#endregion GL_CULL_FACE

		#region GL_CULL_FACE_MODE
		/// <summary>
		/// Cull front-/back-facing polygons
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: polygon
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_BACK" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_CULL_FACE_MODE						= 0x0B45;
		#endregion GL_CULL_FACE_MODE

		#region GL_FRONT_FACE
		/// <summary>
		/// Polygon front-face CW/CCW indicator
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: polygon
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_CCW" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_FRONT_FACE							= 0x0B46;
		#endregion GL_FRONT_FACE

		#region GL_LIGHTING
		/// <summary>
		/// True if lighting is enabled
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: lighting/enable
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glIsEnabled" />
		/// </para>
		/// </remarks>
		public const uint GL_LIGHTING							= 0x0B50;
		#endregion GL_LIGHTING

		#region GL_LIGHT_MODEL_LOCAL_VIEWER
		/// <summary>
		/// Viewer is local
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: lighting
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetBooleanv" />
		/// </para>
		/// </remarks>
		public const uint GL_LIGHT_MODEL_LOCAL_VIEWER			= 0x0B51;
		#endregion GL_LIGHT_MODEL_LOCAL_VIEWER

		#region GL_LIGHT_MODEL_TWO_SIDE
		/// <summary>
		/// Use two-sided lighting
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: lighting
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetBooleanv" />
		/// </para>
		/// </remarks>
		public const uint GL_LIGHT_MODEL_TWO_SIDE				= 0x0B52;
		#endregion GL_LIGHT_MODEL_TWO_SIDE

		#region GL_LIGHT_MODEL_AMBIENT
		/// <summary>
		/// Ambient scene color
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: lighting
		/// </para>
		/// <para>
		/// Initial value: (0.2, 0.2, 0.2, 0.1)
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetFloatv" />
		/// </para>
		/// </remarks>
		public const uint GL_LIGHT_MODEL_AMBIENT				= 0x0B53;
		#endregion GL_LIGHT_MODEL_AMBIENT

		#region GL_SHADE_MODEL
		/// <summary>
		/// <see cref="glShadeModel" /> setting
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: lighting
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_SMOOTH" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_SHADE_MODEL						= 0x0B54;
		#endregion GL_SHADE_MODEL

		#region GL_COLOR_MATERIAL_FACE
		/// <summary>
		/// Face(s) affected by color tracking
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: lighting
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_FRONT_AND_BACK" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_COLOR_MATERIAL_FACE				= 0x0B55;
		#endregion GL_COLOR_MATERIAL_FACE

		#region GL_COLOR_MATERIAL_PARAMETER
		/// <summary>
		/// Material properties tracking current color
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: lighting
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_AMBIENT_AND_DIFFUSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_COLOR_MATERIAL_PARAMETER			= 0x0B56;
		#endregion GL_COLOR_MATERIAL_PARAMETER

		#region GL_COLOR_MATERIAL
		/// <summary>
		/// True if color tracking is enabled
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: lighting
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glIsEnabled" />
		/// </para>
		/// </remarks>
		public const uint GL_COLOR_MATERIAL						= 0x0B57;
		#endregion GL_COLOR_MATERIAL

		#region GL_FOG
		/// <summary>
		/// True if fog enabled
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: fog/enable
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glIsEnabled" />
		/// </para>
		/// </remarks>
		public const uint GL_FOG								= 0x0B60;
		#endregion GL_FOG

		#region GL_FOG_INDEX
		/// <summary>
		/// Fog index
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: fog
		/// </para>
		/// <para>
		/// Initial value: 0
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetFloatv" />
		/// </para>
		/// </remarks>
		public const uint GL_FOG_INDEX							= 0x0B61;
		#endregion GL_FOG_INDEX

		#region GL_FOG_DENSITY
		/// <summary>
		/// Exponential fog density
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: fog
		/// </para>
		/// <para>
		/// Initial value: 1.0
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetFloatv" />
		/// </para>
		/// </remarks>
		public const uint GL_FOG_DENSITY						= 0x0B62;
		#endregion GL_FOG_DENSITY

		#region GL_FOG_START
		/// <summary>
		/// Linear fog start
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: fog
		/// </para>
		/// <para>
		/// Initial value: 0.0
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetFloatv" />
		/// </para>
		/// </remarks>
		public const uint GL_FOG_START							= 0x0B63;
		#endregion GL_FOG_START

		#region GL_FOG_END
		/// <summary>
		/// Linear fog end
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: fog
		/// </para>
		/// <para>
		/// Initial value: 1.0
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetFloatv" />
		/// </para>
		/// </remarks>
		public const uint GL_FOG_END							= 0x0B64;
		#endregion GL_FOG_END

		#region GL_FOG_MODE
		/// <summary>
		/// Fog mode
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: fog
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_EXP" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_FOG_MODE							= 0x0B65;
		#endregion GL_FOG_MODE

		#region GL_FOG_COLOR
		/// <summary>
		/// Fog color
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: fog
		/// </para>
		/// <para>
		/// Initial value: 0,0,0,0
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetFloatv" />
		/// </para>
		/// </remarks>
		public const uint GL_FOG_COLOR							= 0x0B66;
		#endregion GL_FOG_COLOR

		#region GL_DEPTH_RANGE
		/// <summary>
		/// Depth range near and far
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: viewport
		/// </para>
		/// <para>
		/// Initial value: 0,1
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetFloatv" />
		/// </para>
		/// </remarks>
		public const uint GL_DEPTH_RANGE						= 0x0B70;
		#endregion GL_DEPTH_RANGE

		#region GL_DEPTH_TEST
		/// <summary>
		/// Depth buffer enabled
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: depth-buffer/enable
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glIsEnabled" />
		/// </para>
		/// </remarks>
		public const uint GL_DEPTH_TEST							= 0x0B71;
		#endregion GL_DEPTH_TEST

		#region GL_DEPTH_WRITEMASK
		/// <summary>
		/// Depth buffer enabled for writing
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: depth-buffer
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_TRUE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetBooleanv" />
		/// </para>
		/// </remarks>
		public const uint GL_DEPTH_WRITEMASK					= 0x0B72;
		#endregion GL_DEPTH_WRITEMASK

		#region GL_DEPTH_CLEAR_VALUE
		/// <summary>
		/// Depth-buffer clear value
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: depth-buffer
		/// </para>
		/// <para>
		/// Initial value: 1
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_DEPTH_CLEAR_VALUE					= 0x0B73;
		#endregion GL_DEPTH_CLEAR_VALUE
		
		#region GL_DEPTH_FUNC
		/// <summary>
		/// Depth buffer test function
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: depth-buffer
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_LESS" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_DEPTH_FUNC							= 0x0B74;
		#endregion GL_DEPTH_FUNC

		#region GL_ACCUM_CLEAR_VALUE
		/// <summary>
		/// Accumulation-buffer clear value
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: accum-buffer
		/// </para>
		/// <para>
		/// Initial value: 0
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetFloatv" />
		/// </para>
		/// </remarks>
		public const uint GL_ACCUM_CLEAR_VALUE					= 0x0B80;
		#endregion GL_ACCUM_CLEAR_VALUE

		#region GL_STENCIL_TEST
		/// <summary>
		/// Stenciling enabled
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: stencil-buffer/enable
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glIsEnabled" />
		/// </para>
		/// </remarks>
		public const uint GL_STENCIL_TEST						= 0x0B90;
		#endregion GL_STENCIL_TEST

		#region GL_STENCIL_CLEAR_VALUE
		/// <summary>
		/// Stencil-buffer clear value
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: stencil-buffer
		/// </para>
		/// <para>
		/// Initial value: 0
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_STENCIL_CLEAR_VALUE				= 0x0B91;
		#endregion GL_STENCIL_CLEAR_VALUE

		#region GL_STENCIL_FUNC
		/// <summary>
		/// Stencil function
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: stencil-buffer
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_ALWAYS" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_STENCIL_FUNC						= 0x0B92;
		#endregion GL_STENCIL_FUNC

		#region GL_STENCIL_VALUE_MASK
		/// <summary>
		/// Stencil mask
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: stencil-buffer
		/// </para>
		/// <para>
		/// Initial value: 1's
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_STENCIL_VALUE_MASK					= 0x0B93;
		#endregion GL_STENCIL_VALUE_MASK

		#region GL_STENCIL_FAIL
		/// <summary>
		/// Stencil fail action
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: stencil-buffer
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_KEEP" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_STENCIL_FAIL						= 0x0B94;
		#endregion GL_STENCIL_FAIL

		#region GL_STENCIL_PASS_DEPTH_FAIL
		/// <summary>
		/// Stencil depth buffer fail action
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: stencil-buffer
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_KEEP" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_STENCIL_PASS_DEPTH_FAIL			= 0x0B95;
		#endregion GL_STENCIL_PASS_DEPTH_FAIL

		#region GL_STENCIL_PASS_DEPTH_PASS
		/// <summary>
		/// Stencil depth buffer pass action
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: stencil-buffer
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_KEEP" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_STENCIL_PASS_DEPTH_PASS			= 0x0B96;
		#endregion GL_STENCIL_PASS_DEPTH_PASS

		#region GL_STENCIL_REF
		/// <summary>
		/// Stencil reference value
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: stencil-buffer
		/// </para>
		/// <para>
		/// Initial value: 0
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_STENCIL_REF						= 0x0B97;
		#endregion GL_STENCIL_REF

		#region GL_STENCIL_WRITEMASK
		/// <summary>
		/// Stencil-buffer writemask
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: stencil-buffer
		/// </para>
		/// <para>
		/// Initial value: 1's
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_STENCIL_WRITEMASK					= 0x0B98;
		#endregion GL_STENCIL_WRITEMASK

		#region GL_MATRIX_MODE
		/// <summary>
		/// Current matrix mode
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: transform
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_MODELVIEW" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_MATRIX_MODE						= 0x0BA0;
		#endregion GL_MATRIX_MODE

		#region GL_NORMALIZE
		/// <summary>
		/// Current normal normalization on/off
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: transform/enable
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glIsEnabled" />
		/// </para>
		/// </remarks>
		public const uint GL_NORMALIZE							= 0x0BA1;
		#endregion GL_NORMALIZE

		#region GL_VIEWPORT
		/// <summary>
		/// Viewport origin and extent
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: viewport
		/// </para>
		/// <para>
		/// Initial value: --
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_VIEWPORT							= 0x0BA2;
		#endregion GL_VIEWPORT

		#region GL_MODELVIEW_STACK_DEPTH
		/// <summary>
		/// Modelview matrix stack pointer
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: 1
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_MODELVIEW_STACK_DEPTH				= 0x0BA3;
		#endregion GL_MODELVIEW_STACK_DEPTH

		#region GL_PROJECTION_STACK_DEPTH
		/// <summary>
		/// Projection matrix stack pointer
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: 1
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_PROJECTION_STACK_DEPTH				= 0x0BA4;
		#endregion GL_PROJECTION_STACK_DEPTH

		#region GL_TEXTURE_STACK_DEPTH
		/// <summary>
		/// Texture matrix stack pointer
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: 1
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_TEXTURE_STACK_DEPTH				= 0x0BA5;
		#endregion GL_TEXTURE_STACK_DEPTH

		#region GL_MODELVIEW_MATRIX
		/// <summary>
		/// Modelview matrix stack
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: Identity
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetFloatv" />
		/// </para>
		/// </remarks>
		public const uint GL_MODELVIEW_MATRIX					= 0x0BA6;
		#endregion GL_MODELVIEW_MATRIX

		#region GL_PROJECTION_MATRIX
		/// <summary>
		/// Projection matrix stack
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: Identity
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetFloatv" />
		/// </para>
		/// </remarks>
		public const uint GL_PROJECTION_MATRIX					= 0x0BA7;
		#endregion GL_PROJECTION_MATRIX

		#region GL_TEXTURE_MATRIX
		/// <summary>
		/// Texture matrix stack
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: Identity
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetFloatv" />
		/// </para>
		/// </remarks>
		public const uint GL_TEXTURE_MATRIX						= 0x0BA8;
		#endregion GL_TEXTURE_MATRIX

		#region GL_ATTRIB_STACK_DEPTH
		/// <summary>
		/// Attribute stack pointer
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: 0
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_ATTRIB_STACK_DEPTH					= 0x0BB0;
		#endregion GL_ATTRIB_STACK_DEPTH

		public const uint GL_CLIENT_ATTRIB_STACK_DEPTH			= 0x0BB1;

		#region GL_ALPHA_TEST
		/// <summary>
		/// Alpha test enabled
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: color-buffer/enable
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glIsEnabled" />
		/// </para>
		/// </remarks>
		public const uint GL_ALPHA_TEST							= 0x0BC0;
		#endregion GL_ALPHA_TEST

		#region GL_ALPHA_TEST_FUNC
		/// <summary>
		/// Alpha test function
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: color-buffer
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_ALWAYS" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_ALPHA_TEST_FUNC					= 0x0BC1;
		#endregion GL_ALPHA_TEST_FUNC

		#region GL_ALPHA_TEST_REF
		/// <summary>
		/// Alpha test reference value
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: color-buffer
		/// </para>
		/// <para>
		/// Initial value: 0
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_ALPHA_TEST_REF						= 0x0BC2;
		#endregion GL_ALPHA_TEST_REF

		#region GL_DITHER
		/// <summary>
		/// Dithering enabled
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: color-buffer/enable
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_TRUE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glIsEnabled" />
		/// </para>
		/// </remarks>
		public const uint GL_DITHER								= 0x0BD0;
		#endregion GL_DITHER
		
		#region GL_BLEND_DST
		/// <summary>
		/// Blending destination function
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: color-buffer
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_ZERO" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_BLEND_DST							= 0x0BE0;
		#endregion GL_BLEND_DST
		
		#region GL_BLEND_SRC
		/// <summary>
		/// Blending source function
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: color-buffer
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_ONE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_BLEND_SRC							= 0x0BE1;
		#endregion GL_BLEND_SRC

		#region GL_BLEND
		/// <summary>
		/// Blending enabled
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: color-buffer/enable
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glIsEnabled" />
		/// </para>
		/// </remarks>
		public const uint GL_BLEND								= 0x0BE2;
		#endregion GL_BLEND

		#region GL_LOGIC_OP_MODE
		/// <summary>
		/// Logical operation function
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: color-buffer
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_COPY" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_LOGIC_OP_MODE						= 0x0BF0;
		#endregion GL_LOGIC_OP_MODE

		public const uint GL_INDEX_LOGIC_OP						= 0x0BF1;

		#region GL_COLOR_LOGIC_OP
		/// <summary>
		/// Logical operation enabled
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: color-buffer/enable
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glIsEnabled" />
		/// </para>
		/// </remarks>
		public const uint GL_COLOR_LOGIC_OP						= 0x0BF2;
		#endregion GL_COLOR_LOGIC_OP

		#region GL_AUX_BUFFERS
		/// <summary>
		/// Number of auxiliary buffers
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: 0
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetBooleanv" />
		/// </para>
		/// </remarks>
		public const uint GL_AUX_BUFFERS						= 0x0C00;
		#endregion GL_AUX_BUFFERS
		
		#region GL_DRAW_BUFFER
		/// <summary>
		/// Buffers selected for drawing
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: color-buffer
		/// </para>
		/// <para>
		/// Initial value: --
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_DRAW_BUFFER						= 0x0C01;
		#endregion GL_DRAW_BUFFER

		#region GL_READ_BUFFER
		/// <summary>
		/// Read source buffer
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: pixel
		/// </para>
		/// <para>
		/// Initial value: --
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_READ_BUFFER						= 0x0C02;
		#endregion GL_READ_BUFFER

		#region GL_SCISSOR_BOX
		/// <summary>
		/// Scissor box
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: scissor
		/// </para>
		/// <para>
		/// Initial value: --
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_SCISSOR_BOX						= 0x0C10;
		#endregion GL_SCISSOR_BOX

		#region GL_SCISSOR_TEST
		/// <summary>
		/// Scissoring enabled
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: scissor/enable
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glIsEnabled" />
		/// </para>
		/// </remarks>
		public const uint GL_SCISSOR_TEST						= 0x0C11;
		#endregion GL_SCISSOR_TEST

		#region GL_INDEX_CLEAR_VALUE
		/// <summary>
		/// Color-buffer clear value (color-index mode)
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: color-buffer
		/// </para>
		/// <para>
		/// Initial value: 0
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetFloatv" />
		/// </para>
		/// </remarks>
		public const uint GL_INDEX_CLEAR_VALUE					= 0x0C20;
		#endregion GL_INDEX_CLEAR_VALUE
		
		#region GL_INDEX_WRITEMASK
		/// <summary>
		/// Color-index writemask
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: color-buffer
		/// </para>
		/// <para>
		/// Initial value: 1's
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_INDEX_WRITEMASK					= 0x0C21;
		#endregion GL_INDEX_WRITEMASK

		#region GL_COLOR_CLEAR_VALUE
		/// <summary>
		/// Color-buffer clear value (RGBA mode)
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: color-buffer
		/// </para>
		/// <para>
		/// Initial value: 0, 0, 0, 0
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetFloatv" />
		/// </para>
		/// </remarks>
		public const uint GL_COLOR_CLEAR_VALUE					= 0x0C22;
		#endregion GL_COLOR_CLEAR_VALUE
		
		#region GL_COLOR_WRITEMASK
		/// <summary>
		/// Color write enables; R, G, B, or A
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: color-buffer
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_TRUE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetBooleanv" />
		/// </para>
		/// </remarks>
		public const uint GL_COLOR_WRITEMASK					= 0x0C23;
		#endregion GL_COLOR_WRITEMASK

		#region GL_INDEX_MODE
		/// <summary>
		/// True if color buffers store indexes
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: --
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetBooleanv" />
		/// </para>
		/// </remarks>
		public const uint GL_INDEX_MODE							= 0x0C30;
		#endregion GL_INDEX_MODE
		
		#region GL_RGBA_MODE
		/// <summary>
		/// True if color buffers store RGBA
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: --
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetBooleanv" />
		/// </para>
		/// </remarks>
		public const uint GL_RGBA_MODE							= 0x0C31;
		#endregion GL_RGBA_MODE

		#region GL_DOUBLEBUFFER
		/// <summary>
		/// True if front and back buffers exist
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: --
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetBooleanv" />
		/// </para>
		/// </remarks>
		public const uint GL_DOUBLEBUFFER						= 0x0C32;
		#endregion GL_DOUBLEBUFFER

		#region GL_STEREO
		/// <summary>
		/// True if left and right buffers exist
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: --
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetFloatv" />
		/// </para>
		/// </remarks>
		public const uint GL_STEREO								= 0x0C33;
		#endregion GL_STEREO

		#region GL_RENDER_MODE
		/// <summary>
		/// <see cref="glRenderMode" /> setting
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_RENDER" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_RENDER_MODE						= 0x0C40;
		#endregion GL_RENDER_MODE
		
		#region GL_PERSPECTIVE_CORRECTION_HINT
		/// <summary>
		/// Perspective correction hint
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: hint
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_DONT_CARE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_PERSPECTIVE_CORRECTION_HINT		= 0x0C50;
		#endregion GL_PERSPECTIVE_CORRECTION_HINT

		#region GL_POINT_SMOOTH_HINT
		/// <summary>
		/// Point smooth hint
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: hint
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_DONT_CARE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_POINT_SMOOTH_HINT					= 0x0C51;
		#endregion GL_POINT_SMOOTH_HINT

		#region GL_LINE_SMOOTH_HINT
		/// <summary>
		/// Line smooth hint
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: hint
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_DONT_CARE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_LINE_SMOOTH_HINT					= 0x0C52;
		#endregion GL_LINE_SMOOTH_HINT

		#region GL_POLYGON_SMOOTH_HINT
		/// <summary>
		/// Polygon smooth hint
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: hint
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_DONT_CARE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_POLYGON_SMOOTH_HINT				= 0x0C53;
		#endregion GL_POLYGON_SMOOTH_HINT

		#region GL_FOG_HINT
		/// <summary>
		/// Fog hint
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: hint
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_DONT_CARE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_FOG_HINT							= 0x0C54;
		#endregion GL_FOG_HINT

		#region GL_TEXTURE_GEN_S
		/// <summary>
		/// Texgen is enabled
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: texture/enable
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glIsEnabled" />
		/// </para>
		/// </remarks>
		public const uint GL_TEXTURE_GEN_S						= 0x0C60;
		#endregion GL_TEXTURE_GEN_S

		#region GL_TEXTURE_GEN_T
		/// <summary>
		/// Texgen is enabled
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: texture/enable
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glIsEnabled" />
		/// </para>
		/// </remarks>
		public const uint GL_TEXTURE_GEN_T						= 0x0C61;
		#endregion GL_TEXTURE_GEN_T

		#region GL_TEXTURE_GEN_R
		/// <summary>
		/// Texgen is enabled
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: texture/enable
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glIsEnabled" />
		/// </para>
		/// </remarks>
		public const uint GL_TEXTURE_GEN_R						= 0x0C62;
		#endregion GL_TEXTURE_GEN_R

		#region GL_TEXTURE_GEN_Q
		/// <summary>
		/// Texgen is enabled
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: texture/enable
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glIsEnabled" />
		/// </para>
		/// </remarks>
		public const uint GL_TEXTURE_GEN_Q						= 0x0C63;
		#endregion GL_TEXTURE_GEN_Q

		#region GL_PIXEL_MAP_I_TO_I
		/// <summary>
		/// <see cref="glPixelMap" /> translation tables
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: pixel
		/// </para>
		/// <para>
		/// Initial value: 0's
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetPixelMap" />
		/// </para>
		/// </remarks>
		public const uint GL_PIXEL_MAP_I_TO_I					= 0x0C70;
		#endregion GL_PIXEL_MAP_I_TO_I

		#region GL_PIXEL_MAP_S_TO_S
		/// <summary>
		/// <see cref="glPixelMap" /> translation tables
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: pixel
		/// </para>
		/// <para>
		/// Initial value: 0's
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetPixelMap" />
		/// </para>
		/// </remarks>
		public const uint GL_PIXEL_MAP_S_TO_S					= 0x0C71;
		#endregion GL_PIXEL_MAP_S_TO_S

		#region GL_PIXEL_MAP_I_TO_R
		/// <summary>
		/// <see cref="glPixelMap" /> translation tables
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: pixel
		/// </para>
		/// <para>
		/// Initial value: 0's
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetPixelMap" />
		/// </para>
		/// </remarks>
		public const uint GL_PIXEL_MAP_I_TO_R					= 0x0C72;
		#endregion GL_PIXEL_MAP_I_TO_R

		#region GL_PIXEL_MAP_I_TO_G
		/// <summary>
		/// <see cref="glPixelMap" /> translation tables
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: pixel
		/// </para>
		/// <para>
		/// Initial value: 0's
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetPixelMap" />
		/// </para>
		/// </remarks>
		public const uint GL_PIXEL_MAP_I_TO_G					= 0x0C73;
		#endregion GL_PIXEL_MAP_I_TO_G

		#region GL_PIXEL_MAP_I_TO_B
		/// <summary>
		/// <see cref="glPixelMap" /> translation tables
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: pixel
		/// </para>
		/// <para>
		/// Initial value: 0's
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetPixelMap" />
		/// </para>
		/// </remarks>
		public const uint GL_PIXEL_MAP_I_TO_B					= 0x0C74;
		#endregion GL_PIXEL_MAP_I_TO_B

		#region GL_PIXEL_MAP_I_TO_A
		/// <summary>
		/// <see cref="glPixelMap" /> translation tables
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: pixel
		/// </para>
		/// <para>
		/// Initial value: 0's
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetPixelMap" />
		/// </para>
		/// </remarks>
		public const uint GL_PIXEL_MAP_I_TO_A					= 0x0C75;
		#endregion GL_PIXEL_MAP_I_TO_A

		#region GL_PIXEL_MAP_R_TO_R
		/// <summary>
		/// <see cref="glPixelMap" /> translation tables
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: pixel
		/// </para>
		/// <para>
		/// Initial value: 0's
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetPixelMap" />
		/// </para>
		/// </remarks>
		public const uint GL_PIXEL_MAP_R_TO_R					= 0x0C76;
		#endregion GL_PIXEL_MAP_R_TO_R

		#region GL_PIXEL_MAP_G_TO_G
		/// <summary>
		/// <see cref="glPixelMap" /> translation tables
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: pixel
		/// </para>
		/// <para>
		/// Initial value: 0's
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetPixelMap" />
		/// </para>
		/// </remarks>
		public const uint GL_PIXEL_MAP_G_TO_G					= 0x0C77;
		#endregion GL_PIXEL_MAP_G_TO_G

		#region GL_PIXEL_MAP_B_TO_B
		/// <summary>
		/// <see cref="glPixelMap" /> translation tables
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: pixel
		/// </para>
		/// <para>
		/// Initial value: 0's
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetPixelMap" />
		/// </para>
		/// </remarks>
		public const uint GL_PIXEL_MAP_B_TO_B					= 0x0C78;
		#endregion GL_PIXEL_MAP_B_TO_B

		#region GL_PIXEL_MAP_A_TO_A
		/// <summary>
		/// <see cref="glPixelMap" /> translation tables
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: pixel
		/// </para>
		/// <para>
		/// Initial value: 0's
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetPixelMap" />
		/// </para>
		/// </remarks>
		public const uint GL_PIXEL_MAP_A_TO_A					= 0x0C79;
		#endregion GL_PIXEL_MAP_A_TO_A

		#region GL_PIXEL_MAP_I_TO_I_SIZE
		/// <summary>
		/// Size of table GL_PIXEL_MAP_I_TO_I_SIZE
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: pixel
		/// </para>
		/// <para>
		/// Initial value: 1
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_PIXEL_MAP_I_TO_I_SIZE				= 0x0CB0;
		#endregion GL_PIXEL_MAP_I_TO_I_SIZE

		#region GL_PIXEL_MAP_S_TO_S_SIZE
		/// <summary>
		/// Size of table GL_PIXEL_MAP_S_TO_S_SIZE
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: pixel
		/// </para>
		/// <para>
		/// Initial value: 1
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_PIXEL_MAP_S_TO_S_SIZE				= 0x0CB1;
		#endregion GL_PIXEL_MAP_S_TO_S_SIZE

		#region GL_PIXEL_MAP_I_TO_R_SIZE
		/// <summary>
		/// Size of table GL_PIXEL_MAP_I_TO_R_SIZE
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: pixel
		/// </para>
		/// <para>
		/// Initial value: 1
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_PIXEL_MAP_I_TO_R_SIZE				= 0x0CB2;
		#endregion GL_PIXEL_MAP_I_TO_R_SIZE

		#region GL_PIXEL_MAP_I_TO_G_SIZE
		/// <summary>
		/// Size of table GL_PIXEL_MAP_I_TO_G_SIZE
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: pixel
		/// </para>
		/// <para>
		/// Initial value: 1
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_PIXEL_MAP_I_TO_G_SIZE				= 0x0CB3;
		#endregion GL_PIXEL_MAP_I_TO_G_SIZE

		#region GL_PIXEL_MAP_I_TO_B_SIZE
		/// <summary>
		/// Size of table GL_PIXEL_MAP_I_TO_B_SIZE
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: pixel
		/// </para>
		/// <para>
		/// Initial value: 1
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_PIXEL_MAP_I_TO_B_SIZE				= 0x0CB4;
		#endregion GL_PIXEL_MAP_I_TO_B_SIZE

		#region GL_PIXEL_MAP_I_TO_A_SIZE
		/// <summary>
		/// Size of table GL_PIXEL_MAP_I_TO_A_SIZE
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: pixel
		/// </para>
		/// <para>
		/// Initial value: 1
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_PIXEL_MAP_I_TO_A_SIZE				= 0x0CB5;
		#endregion GL_PIXEL_MAP_I_TO_A_SIZE

		#region GL_PIXEL_MAP_R_TO_R_SIZE
		/// <summary>
		/// Size of table GL_PIXEL_MAP_R_TO_R_SIZE
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: pixel
		/// </para>
		/// <para>
		/// Initial value: 1
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_PIXEL_MAP_R_TO_R_SIZE				= 0x0CB6;
		#endregion GL_PIXEL_MAP_R_TO_R_SIZE

		#region GL_PIXEL_MAP_G_TO_G_SIZE
		/// <summary>
		/// Size of table GL_PIXEL_MAP_G_TO_G_SIZE
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: pixel
		/// </para>
		/// <para>
		/// Initial value: 1
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_PIXEL_MAP_G_TO_G_SIZE				= 0x0CB7;
		#endregion GL_PIXEL_MAP_G_TO_G_SIZE

		#region GL_PIXEL_MAP_B_TO_B_SIZE
		/// <summary>
		/// Size of table GL_PIXEL_MAP_B_TO_B_SIZE
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: pixel
		/// </para>
		/// <para>
		/// Initial value: 1
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_PIXEL_MAP_B_TO_B_SIZE				= 0x0CB8;
		#endregion GL_PIXEL_MAP_B_TO_B_SIZE

		#region GL_PIXEL_MAP_A_TO_A_SIZE
		/// <summary>
		/// Size of table GL_PIXEL_MAP_A_TO_A_SIZE
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: pixel
		/// </para>
		/// <para>
		/// Initial value: 1
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_PIXEL_MAP_A_TO_A_SIZE				= 0x0CB9;
		#endregion GL_PIXEL_MAP_A_TO_A_SIZE
		
		#region GL_UNPACK_SWAP_BYTES
		/// <summary>
		/// Value of GL_UNPACK_SWAP_BYTES
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetBooleanv" />
		/// </para>
		/// </remarks>
		public const uint GL_UNPACK_SWAP_BYTES					= 0x0CF0;
		#endregion GL_UNPACK_SWAP_BYTES

		#region GL_UNPACK_LSB_FIRST
		/// <summary>
		/// Value of GL_UNPACK_LSB_FIRST
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetBooleanv" />
		/// </para>
		/// </remarks>
		public const uint GL_UNPACK_LSB_FIRST					= 0x0CF1;
		#endregion GL_UNPACK_LSB_FIRST

		#region GL_UNPACK_ROW_LENGTH
		/// <summary>
		/// Value of GL_UNPACK_ROW_LENGTH
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: 0
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_UNPACK_ROW_LENGTH					= 0x0CF2;
		#endregion GL_UNPACK_ROW_LENGTH

		#region GL_UNPACK_SKIP_ROWS
		/// <summary>
		/// Value of GL_UNPACK_SKIP_ROWS
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: 0
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_UNPACK_SKIP_ROWS					= 0x0CF3;
		#endregion GL_UNPACK_SKIP_ROWS

		#region GL_UNPACK_SKIP_PIXELS
		/// <summary>
		/// Value of GL_UNPACK_SKIP_PIXELS
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: 0
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_UNPACK_SKIP_PIXELS					= 0x0CF4;
		#endregion GL_UNPACK_SKIP_PIXELS 

		#region GL_UNPACK_ALIGNMENT
		/// <summary>
		/// Value of GL_UNPACK_ALIGNMENT
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: 4
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_UNPACK_ALIGNMENT					= 0x0CF5;
		#endregion GL_UNPACK_ALIGNMENT

		#region GL_PACK_SWAP_BYTES
		/// <summary>
		/// Value of GL_PACK_SWAP_BYTES
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetBooleanv" />
		/// </para>
		/// </remarks>
		public const uint GL_PACK_SWAP_BYTES					= 0x0D00;
		#endregion GL_PACK_SWAP_BYTES

		#region GL_PACK_LSB_FIRST
		/// <summary>
		/// Value of GL_PACK_LSB_FIRST
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetBooleanv" />
		/// </para>
		/// </remarks>
		public const uint GL_PACK_LSB_FIRST						= 0x0D01;
		#endregion GL_PACK_LSB_FIRST

		#region GL_PACK_ROW_LENGTH
		/// <summary>
		/// Value of GL_PACK_ROW_LENGTH
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: 0
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_PACK_ROW_LENGTH					= 0x0D02;
		#endregion GL_PACK_ROW_LENGTH

		#region GL_PACK_SKIP_ROWS
		/// <summary>
		/// Value of GL_PACK_SKIP_ROWS
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: 0
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_PACK_SKIP_ROWS						= 0x0D03;
		#endregion GL_PACK_SKIP_ROWS

		#region GL_PACK_SKIP_PIXELS
		/// <summary>
		/// Value of GL_PACK_SKIP_PIXELS
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: 0
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_PACK_SKIP_PIXELS					= 0x0D04;
		#endregion GL_PACK_SKIP_PIXELS

		#region GL_PACK_ALIGNMENT
		/// <summary>
		/// Value of GL_PACK_ALIGNMENT
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: 4
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_PACK_ALIGNMENT						= 0x0D05;
		#endregion GL_PACK_ALIGNMENT

		#region GL_MAP_COLOR
		/// <summary>
		/// True if colors are mapped
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: pixel
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetBooleanv" />
		/// </para>
		/// </remarks>
		public const uint GL_MAP_COLOR							= 0x0D10;
		#endregion GL_MAP_COLOR

		#region GL_MAP_STENCIL
		/// <summary>
		/// True if stencil values are mapped
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: pixel
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetBooleanv" />
		/// </para>
		/// </remarks>
		public const uint GL_MAP_STENCIL						= 0x0D11;
		#endregion GL_MAP_STENCIL

		#region GL_INDEX_SHIFT
		/// <summary>
		/// Value of GL_INDEX_SHIFT
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: pixel
		/// </para>
		/// <para>
		/// Initial value: 0
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_INDEX_SHIFT						= 0x0D12;
		#endregion GL_INDEX_SHIFT

		#region GL_INDEX_OFFSET
		/// <summary>
		/// Value of GL_INDEX_OFFSET
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: pixel
		/// </para>
		/// <para>
		/// Initial value: 0
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_INDEX_OFFSET						= 0x0D13;
		#endregion GL_INDEX_OFFSET

		#region GL_RED_SCALE
		/// <summary>
		/// Value of GL_RED_SCALE
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: pixel
		/// </para>
		/// <para>
		/// Initial value: 1
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetFloatv" />
		/// </para>
		/// </remarks>
		public const uint GL_RED_SCALE							= 0x0D14;
		#endregion GL_RED_SCALE

		#region GL_RED_BIAS
		/// <summary>
		/// Value of GL_RED_BIAS
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: pixel
		/// </para>
		/// <para>
		/// Initial value: 0
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetFloatv" />
		/// </para>
		/// </remarks>
		public const uint GL_RED_BIAS							= 0x0D15;
		#endregion GL_RED_BIAS

		#region GL_ZOOM_X
		/// <summary>
		/// x zoom factor
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: pixel
		/// </para>
		/// <para>
		/// Initial value: 1.0
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetFloatv" />
		/// </para>
		/// </remarks>
		public const uint GL_ZOOM_X								= 0x0D16;
		#endregion GL_ZOOM_X

		#region GL_ZOOM_Y
		/// <summary>
		/// y zoom factor
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: pixel
		/// </para>
		/// <para>
		/// Initial value: 1.0
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetFloatv" />
		/// </para>
		/// </remarks>
		public const uint GL_ZOOM_Y								= 0x0D17;
		#endregion GL_ZOOM_Y
		
		#region GL_GREEN_SCALE
		/// <summary>
		/// Value of GL_GREEN_SCALE
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: pixel
		/// </para>
		/// <para>
		/// Initial value: 1
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetFloatv" />
		/// </para>
		/// </remarks>
		public const uint GL_GREEN_SCALE						= 0x0D18;
		#endregion GL_GREEN_SCALE

		#region GL_GREEN_BIAS
		/// <summary>
		/// Value of GL_GREEN_BIAS
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: pixel
		/// </para>
		/// <para>
		/// Initial value: 0
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetFloatv" />
		/// </para>
		/// </remarks>
		public const uint GL_GREEN_BIAS							= 0x0D19;
		#endregion GL_GREEN_BIAS
		
		#region GL_BLUE_SCALE
		/// <summary>
		/// Value of GL_BLUE_SCALE
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: pixel
		/// </para>
		/// <para>
		/// Initial value: 1
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetFloatv" />
		/// </para>
		/// </remarks>
		public const uint GL_BLUE_SCALE							= 0x0D1A;
		#endregion GL_BLUE_SCALE

		#region GL_BLUE_BIAS
		/// <summary>
		/// Value of GL_BLUE_BIAS
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: pixel
		/// </para>
		/// <para>
		/// Initial value: 0
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetFloatv" />
		/// </para>
		/// </remarks>
		public const uint GL_BLUE_BIAS							= 0x0D1B;
		#endregion GL_BLUE_BIAS
		
		#region GL_ALPHA_SCALE
		/// <summary>
		/// Value of GL_ALPHA_SCALE
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: pixel
		/// </para>
		/// <para>
		/// Initial value: 1
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetFloatv" />
		/// </para>
		/// </remarks>
		public const uint GL_ALPHA_SCALE						= 0x0D1C;
		#endregion GL_ALPHA_SCALE

		#region GL_ALPHA_BIAS
		/// <summary>
		/// Value of GL_ALPHA_BIAS
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: pixel
		/// </para>
		/// <para>
		/// Initial value: 0
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetFloatv" />
		/// </para>
		/// </remarks>
		public const uint GL_ALPHA_BIAS							= 0x0D1D;
		#endregion GL_ALPHA_BIAS
		
		#region GL_DEPTH_SCALE
		/// <summary>
		/// Value of GL_DEPTH_SCALE
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: pixel
		/// </para>
		/// <para>
		/// Initial value: 1
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetFloatv" />
		/// </para>
		/// </remarks>
		public const uint GL_DEPTH_SCALE						= 0x0D1E;
		#endregion GL_DEPTH_SCALE

		#region GL_DEPTH_BIAS
		/// <summary>
		/// Value of GL_DEPTH_BIAS
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: pixel
		/// </para>
		/// <para>
		/// Initial value: 0
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetFloatv" />
		/// </para>
		/// </remarks>
		public const uint GL_DEPTH_BIAS							= 0x0D1F;
		#endregion GL_DEPTH_BIAS

		#region GL_MAX_EVAL_ORDER
		/// <summary>
		/// Maximum evaluator polynomial order
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: 8
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_MAX_EVAL_ORDER						= 0x0D30;
		#endregion GL_MAX_EVAL_ORDER
		
		#region GL_MAX_LIGHTS
		/// <summary>
		/// Maximum number of lights
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: 8
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_MAX_LIGHTS							= 0x0D31;
		#endregion GL_MAX_LIGHTS

		#region GL_MAX_CLIP_PLANES
		/// <summary>
		/// Maximum number of user clipping planes
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: 6
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_MAX_CLIP_PLANES					= 0x0D32;
		#endregion GL_MAX_CLIP_PLANES

		#region GL_MAX_TEXTURE_SIZE
		/// <summary>
		/// Maximum height or width of a texture image (without borders)
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: 64
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_MAX_TEXTURE_SIZE					= 0x0D33;
		#endregion GL_MAX_TEXTURE_SIZE

		#region GL_MAX_PIXEL_MAP_TABLE
		/// <summary>
		/// Maximum size of a <see cref="glPixelMap" /> translation table
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: 32
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_MAX_PIXEL_MAP_TABLE				= 0x0D34;
		#endregion GL_MAX_PIXEL_MAP_TABLE

		#region GL_MAX_ATTRIB_STACK_DEPTH
		/// <summary>
		/// Maximum depth of the attribute stack
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: 16
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_MAX_ATTRIB_STACK_DEPTH				= 0x0D35;
		#endregion GL_MAX_ATTRIB_STACK_DEPTH
		
		#region GL_MAX_MODELVIEW_STACK_DEPTH
		/// <summary>
		/// Maximum modelview-matrix stack depth
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: 32
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_MAX_MODELVIEW_STACK_DEPTH			= 0x0D36;
		#endregion GL_MAX_MODELVIEW_STACK_DEPTH

		#region GL_MAX_NAME_STACK_DEPTH
		/// <summary>
		/// Maximum selection-name stack depth
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: 64
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_MAX_NAME_STACK_DEPTH				= 0x0D37;
		#endregion GL_MAX_NAME_STACK_DEPTH
		
		#region GL_MAX_PROJECTION_STACK_DEPTH
		/// <summary>
		/// Maximum projection-matrix stack depth
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: 2
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_MAX_PROJECTION_STACK_DEPTH			= 0x0D38;
		#endregion GL_MAX_PROJECTION_STACK_DEPTH

		#region GL_MAX_TEXTURE_STACK_DEPTH
		/// <summary>
		/// Maximum depth of texture matrix stack
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: 2
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_MAX_TEXTURE_STACK_DEPTH			= 0x0D39;
		#endregion GL_MAX_TEXTURE_STACK_DEPTH

		#region GL_MAX_VIEWPORT_DIMS
		/// <summary>
		/// Maximum viewport dimensions
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: --
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_MAX_VIEWPORT_DIMS					= 0x0D3A;
		#endregion GL_MAX_VIEWPORT_DIMS

		public const uint GL_MAX_CLIENT_ATTRIB_STACK_DEPTH		= 0x0D3B;
		
		#region GL_SUBPIXEL_BITS
		/// <summary>
		/// Number of bits of subpixel precision in x and y
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: 4
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_SUBPIXEL_BITS						= 0x0D50;
		#endregion GL_SUBPIXEL_BITS

		#region GL_INDEX_BITS
		/// <summary>
		/// Number of bits per index in color buffers
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: --
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_INDEX_BITS							= 0x0D51;
		#endregion GL_INDEX_BITS
		
		#region GL_RED_BITS
		/// <summary>
		/// Number of bits per red component in color buffers
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: --
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_RED_BITS							= 0x0D52;
		#endregion GL_RED_BITS

		#region GL_GREEN_BITS
		/// <summary>
		/// Number of bits per green component in color buffers
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: --
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_GREEN_BITS							= 0x0D53;
		#endregion GL_GREEN_BITS

		#region GL_BLUE_BITS
		/// <summary>
		/// Number of bits per blue component in color buffers
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: --
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_BLUE_BITS							= 0x0D54;
		#endregion GL_BLUE_BITS

		#region GL_ALPHA_BITS
		/// <summary>
		/// Number of bits per alpha component in color buffers
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: --
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_ALPHA_BITS							= 0x0D55;
		#endregion GL_ALPHA_BITS

		#region GL_DEPTH_BITS
		/// <summary>
		/// Number of depth-buffer bitplanes
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: --
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_DEPTH_BITS							= 0x0D56;
		#endregion GL_DEPTH_BITS

		#region GL_STENCIL_BITS
		/// <summary>
		/// Number of stencil bitplanes
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: --
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_STENCIL_BITS						= 0x0D57;
		#endregion GL_STENCIL_BITS

		#region GL_ACCUM_RED_BITS
		/// <summary>
		/// Number of bits per red component in the accumulation buffer
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: --
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_ACCUM_RED_BITS						= 0x0D58;
		#endregion GL_ACCUM_RED_BITS

		#region GL_ACCUM_GREEN_BITS
		/// <summary>
		/// Number of bits per green component in the accumulation buffer
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: --
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_ACCUM_GREEN_BITS					= 0x0D59;
		#endregion GL_ACCUM_GREEN_BITS

		#region GL_ACCUM_BLUE_BITS
		/// <summary>
		/// Number of bits per blue component in the accumulation buffer
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: --
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_ACCUM_BLUE_BITS					= 0x0D5A;
		#endregion GL_ACCUM_BLUE_BITS

		#region GL_ACCUM_ALPHA_BITS
		/// <summary>
		/// Number of bits per alpha component in the accumulation buffer
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: --
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_ACCUM_ALPHA_BITS					= 0x0D5B;
		#endregion GL_ACCUM_ALPHA_BITS

		#region GL_NAME_STACK_DEPTH
		/// <summary>
		/// Name stack depth
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: 0
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetIntegerv" />
		/// </para>
		/// </remarks>
		public const uint GL_NAME_STACK_DEPTH					= 0x0D70;
		#endregion GL_NAME_STACK_DEPTH
		
		#region GL_AUTO_NORMAL
		/// <summary>
		/// True if automatic normal generation enabled
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: eval
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glIsEnabled" />
		/// </para>
		/// </remarks>
		public const uint GL_AUTO_NORMAL						= 0x0D80;
		#endregion GL_AUTO_NORMAL
		
		#region GL_MAP1_COLOR_4
		/// <summary>
		/// 1-D map enables: COLOR_4 is map type
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: eval/enable
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glIsEnabled" />
		/// </para>
		/// </remarks>
		public const uint GL_MAP1_COLOR_4						= 0x0D90;
		#endregion GL_MAP1_COLOR_4

		#region GL_MAP1_INDEX
		/// <summary>
		/// 1-D map enables: INDEX is map type
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: eval/enable
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glIsEnabled" />
		/// </para>
		/// </remarks>
		public const uint GL_MAP1_INDEX							= 0x0D91;
		#endregion GL_MAP1_INDEX

		#region GL_MAP1_NORMAL
		/// <summary>
		/// 1-D map enables: NORMAL is map type
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: eval/enable
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glIsEnabled" />
		/// </para>
		/// </remarks>
		public const uint GL_MAP1_NORMAL						= 0x0D92;
		#endregion GL_MAP1_NORMAL

		#region GL_MAP1_TEXTURE_COORD_1
		/// <summary>
		/// 1-D map enables: TEXTURE_COORD_1 is map type
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: eval/enable
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glIsEnabled" />
		/// </para>
		/// </remarks>
		public const uint GL_MAP1_TEXTURE_COORD_1				= 0x0D93;
		#endregion GL_MAP1_TEXTURE_COORD_1

		#region GL_MAP1_TEXTURE_COORD_2
		/// <summary>
		/// 1-D map enables: TEXTURE_COORD_2 is map type
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: eval/enable
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glIsEnabled" />
		/// </para>
		/// </remarks>
		public const uint GL_MAP1_TEXTURE_COORD_2				= 0x0D94;
		#endregion GL_MAP1_TEXTURE_COORD_2

		#region GL_MAP1_TEXTURE_COORD_3
		/// <summary>
		/// 1-D map enables: TEXTURE_COORD_3 is map type
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: eval/enable
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glIsEnabled" />
		/// </para>
		/// </remarks>
		public const uint GL_MAP1_TEXTURE_COORD_3				= 0x0D95;
		#endregion GL_MAP1_TEXTURE_COORD_3

		#region GL_MAP1_TEXTURE_COORD_4
		/// <summary>
		/// 1-D map enables: TEXTURE_COORD_4 is map type
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: eval/enable
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glIsEnabled" />
		/// </para>
		/// </remarks>
		public const uint GL_MAP1_TEXTURE_COORD_4				= 0x0D96;
		#endregion GL_MAP1_TEXTURE_COORD_4

		#region GL_MAP1_VERTEX_3
		/// <summary>
		/// 1-D map enables: VERTEX_3 is map type
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: eval/enable
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glIsEnabled" />
		/// </para>
		/// </remarks>
		public const uint GL_MAP1_VERTEX_3						= 0x0D97;
		#endregion GL_MAP1_VERTEX_3

		#region GL_MAP1_VERTEX_4
		/// <summary>
		/// 1-D map enables: VERTEX_4 is map type
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: eval/enable
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glIsEnabled" />
		/// </para>
		/// </remarks>
		public const uint GL_MAP1_VERTEX_4						= 0x0D98;
		#endregion GL_MAP1_VERTEX_4

		#region GL_MAP2_COLOR_4
		/// <summary>
		/// 2-D map enables: COLOR_4 is map type
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: eval/enable
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glIsEnabled" />
		/// </para>
		/// </remarks>
		public const uint GL_MAP2_COLOR_4						= 0x0DB0;
		#endregion GL_MAP2_COLOR_4

		#region GL_MAP2_INDEX
		/// <summary>
		/// 2-D map enables: INDEX is map type
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: eval/enable
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glIsEnabled" />
		/// </para>
		/// </remarks>
		public const uint GL_MAP2_INDEX							= 0x0DB1;
		#endregion GL_MAP2_INDEX

		#region GL_MAP2_NORMAL
		/// <summary>
		/// 2-D map enables: NORMAL is map type
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: eval/enable
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glIsEnabled" />
		/// </para>
		/// </remarks>
		public const uint GL_MAP2_NORMAL						= 0x0DB2;
		#endregion GL_MAP2_NORMAL

		#region GL_MAP2_TEXTURE_COORD_1
		/// <summary>
		/// 2-D map enables: TEXTURE_COORD_1 is map type
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: eval/enable
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glIsEnabled" />
		/// </para>
		/// </remarks>
		public const uint GL_MAP2_TEXTURE_COORD_1				= 0x0DB3;
		#endregion GL_MAP2_TEXTURE_COORD_1

		#region GL_MAP2_TEXTURE_COORD_2
		/// <summary>
		/// 2-D map enables: TEXTURE_COORD_2 is map type
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: eval/enable
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glIsEnabled" />
		/// </para>
		/// </remarks>
		public const uint GL_MAP2_TEXTURE_COORD_2				= 0x0DB4;
		#endregion GL_MAP2_TEXTURE_COORD_2

		#region GL_MAP2_TEXTURE_COORD_3
		/// <summary>
		/// 2-D map enables: TEXTURE_COORD_3 is map type
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: eval/enable
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glIsEnabled" />
		/// </para>
		/// </remarks>
		public const uint GL_MAP2_TEXTURE_COORD_3				= 0x0DB5;
		#endregion GL_MAP2_TEXTURE_COORD_3

		#region GL_MAP2_TEXTURE_COORD_4
		/// <summary>
		/// 2-D map enables: TEXTURE_COORD_4 is map type
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: eval/enable
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glIsEnabled" />
		/// </para>
		/// </remarks>
		public const uint GL_MAP2_TEXTURE_COORD_4				= 0x0DB6;
		#endregion GL_MAP2_TEXTURE_COORD_4

		#region GL_MAP2_VERTEX_3
		/// <summary>
		/// 2-D map enables: VERTEX_3 is map type
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: eval/enable
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glIsEnabled" />
		/// </para>
		/// </remarks>
		public const uint GL_MAP2_VERTEX_3						= 0x0DB7;
		#endregion GL_MAP2_VERTEX_3

		#region GL_MAP2_VERTEX_4
		/// <summary>
		/// 2-D map enables: VERTEX_4 is map type
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: eval/enable
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glIsEnabled" />
		/// </para>
		/// </remarks>
		public const uint GL_MAP2_VERTEX_4						= 0x0DB8;
		#endregion GL_MAP2_VERTEX_4

		#region GL_MAP1_GRID_DOMAIN
		/// <summary>
		/// 1-D grid endpoints
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: eval
		/// </para>
		/// <para>
		/// Initial value: 0, 1
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetFloatv" />
		/// </para>
		/// </remarks>
		public const uint GL_MAP1_GRID_DOMAIN					= 0x0DD0;
		#endregion GL_MAP1_GRID_DOMAIN

		#region GL_MAP1_GRID_SEGMENTS
		/// <summary>
		/// 1-D grid divisions
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: eval
		/// </para>
		/// <para>
		/// Initial value: 1
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetFloatv" />
		/// </para>
		/// </remarks>
		public const uint GL_MAP1_GRID_SEGMENTS					= 0x0DD1;
		#endregion GL_MAP1_GRID_SEGMENTS

		#region GL_MAP2_GRID_DOMAIN
		/// <summary>
		/// 2-D grid endpoints
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: eval
		/// </para>
		/// <para>
		/// Initial value: 0, 1; 0, 1
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetFloatv" />
		/// </para>
		/// </remarks>
		public const uint GL_MAP2_GRID_DOMAIN					= 0x0DD2;
		#endregion GL_MAP2_GRID_DOMAIN

		#region GL_MAP2_GRID_SEGMENTS
		/// <summary>
		/// 2-D grid segments
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: eval
		/// </para>
		/// <para>
		/// Initial value: 1, 1
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetFloatv" />
		/// </para>
		/// </remarks>
		public const uint GL_MAP2_GRID_SEGMENTS					= 0x0DD3;
		#endregion GL_MAP2_GRID_SEGMENTS

		#region GL_TEXTURE_1D
		/// <summary>
		/// True if 1-D texturing enabled
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: texture/enable
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glIsEnabled" />
		/// </para>
		/// </remarks>
		public const uint GL_TEXTURE_1D							= 0x0DE0;
		#endregion GL_TEXTURE_1D

		#region GL_TEXTURE_2D
		/// <summary>
		/// True if 2-D texturing enabled
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: texture/enable
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glIsEnabled" />
		/// </para>
		/// </remarks>
		public const uint GL_TEXTURE_2D							= 0x0DE1;
		#endregion GL_TEXTURE_2D

		public const uint GL_FEEDBACK_BUFFER_POINTER			= 0x0DF0;
		public const uint GL_FEEDBACK_BUFFER_SIZE				= 0x0DF1;
		public const uint GL_FEEDBACK_BUFFER_TYPE				= 0x0DF2;
		public const uint GL_SELECTION_BUFFER_POINTER			= 0x0DF3;
		public const uint GL_SELECTION_BUFFER_SIZE				= 0x0DF4;
		/*      GL_TEXTURE_BINDING_1D */
		/*      GL_TEXTURE_BINDING_2D */
		/*      GL_VERTEX_ARRAY */
		/*      GL_NORMAL_ARRAY */
		/*      GL_COLOR_ARRAY */
		/*      GL_INDEX_ARRAY */
		/*      GL_TEXTURE_COORD_ARRAY */
		/*      GL_EDGE_FLAG_ARRAY */
		/*      GL_VERTEX_ARRAY_SIZE */
		/*      GL_VERTEX_ARRAY_TYPE */
		/*      GL_VERTEX_ARRAY_STRIDE */
		/*      GL_NORMAL_ARRAY_TYPE */
		/*      GL_NORMAL_ARRAY_STRIDE */
		/*      GL_COLOR_ARRAY_SIZE */
		/*      GL_COLOR_ARRAY_TYPE */
		/*      GL_COLOR_ARRAY_STRIDE */
		/*      GL_INDEX_ARRAY_TYPE */
		/*      GL_INDEX_ARRAY_STRIDE */
		/*      GL_TEXTURE_COORD_ARRAY_SIZE */
		/*      GL_TEXTURE_COORD_ARRAY_TYPE */
		/*      GL_TEXTURE_COORD_ARRAY_STRIDE */
		/*      GL_EDGE_FLAG_ARRAY_STRIDE */
		/*      GL_POLYGON_OFFSET_FACTOR */
		/*      GL_POLYGON_OFFSET_UNITS */
		#endregion GetTarget

		#region GetTextureParameter
		/*      GL_TEXTURE_MAG_FILTER */
		/*      GL_TEXTURE_MIN_FILTER */
		/*      GL_TEXTURE_WRAP_S */
		/*      GL_TEXTURE_WRAP_T */
		
		#region GL_TEXTURE_WIDTH
		/// <summary>
		/// x-D texture image i's width
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: 0
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetTexLevelParameter" />
		/// </para>
		/// </remarks>
		public const uint GL_TEXTURE_WIDTH						= 0x1000;
		#endregion GL_TEXTURE_WIDTH

		#region GL_TEXTURE_HEIGHT
		/// <summary>
		/// x-D texture image i's height
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: 0
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetTexLevelParameter" />
		/// </para>
		/// </remarks>
		public const uint GL_TEXTURE_HEIGHT						= 0x1001;
		#endregion GL_TEXTURE_HEIGHT

		public const uint GL_TEXTURE_INTERNAL_FORMAT			= 0x1003;

		#region GL_TEXTURE_BORDER_COLOR
		/// <summary>
		/// Texture border color
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: texture
		/// </para>
		/// <para>
		/// Initial value: 0,0,0,0
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetTexParameter" />
		/// </para>
		/// </remarks>
		public const uint GL_TEXTURE_BORDER_COLOR				= 0x1004;
		#endregion GL_TEXTURE_BORDER_COLOR

		#region GL_TEXTURE_BORDER
		/// <summary>
		/// x-D texture image i's border
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: 0
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetTexLevelParameter" />
		/// </para>
		/// </remarks>
		public const uint GL_TEXTURE_BORDER						= 0x1005;
		#endregion GL_TEXTURE_BORDER

		/*      GL_TEXTURE_RED_SIZE */
		/*      GL_TEXTURE_GREEN_SIZE */
		/*      GL_TEXTURE_BLUE_SIZE */
		/*      GL_TEXTURE_ALPHA_SIZE */
		/*      GL_TEXTURE_LUMINANCE_SIZE */
		/*      GL_TEXTURE_INTENSITY_SIZE */
		/*      GL_TEXTURE_PRIORITY */
		/*      GL_TEXTURE_RESIDENT */
		#endregion GetTextureParameter

		#region HintMode
		public const uint GL_DONT_CARE							= 0x1100;
		public const uint GL_FASTEST							= 0x1101;
		public const uint GL_NICEST								= 0x1102;
		#endregion HintMode

		#region HintTarget
		/*      GL_PERSPECTIVE_CORRECTION_HINT */
		/*      GL_POINT_SMOOTH_HINT */
		/*      GL_LINE_SMOOTH_HINT */
		/*      GL_POLYGON_SMOOTH_HINT */
		/*      GL_FOG_HINT */
		/*      GL_PHONG_HINT */
		#endregion HintTarget

		#region IndexPointerType
		/*      GL_SHORT */
		/*      GL_INT */
		/*      GL_FLOAT */
		/*      GL_DOUBLE */
		#endregion IndexPointerType

		#region LightModelParameter
		/*      GL_LIGHT_MODEL_AMBIENT */
		/*      GL_LIGHT_MODEL_LOCAL_VIEWER */
		/*      GL_LIGHT_MODEL_TWO_SIDE */
		#endregion LightModelParameter

		#region LightName
		#region GL_LIGHT0
		/// <summary>
		/// True if light 0 is enabled
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: lighting/enable
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glIsEnabled" />
		/// </para>
		/// </remarks>
		public const uint GL_LIGHT0								= 0x4000;
		#endregion GL_LIGHT0

		#region GL_LIGHT1
		/// <summary>
		/// True if light 1 is enabled
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: lighting/enable
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glIsEnabled" />
		/// </para>
		/// </remarks>
		public const uint GL_LIGHT1								= 0x4001;
		#endregion GL_LIGHT1

		#region GL_LIGHT2
		/// <summary>
		/// True if light 2 is enabled
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: lighting/enable
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glIsEnabled" />
		/// </para>
		/// </remarks>
		public const uint GL_LIGHT2								= 0x4002;
		#endregion GL_LIGHT2

		#region GL_LIGHT3
		/// <summary>
		/// True if light 3 is enabled
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: lighting/enable
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glIsEnabled" />
		/// </para>
		/// </remarks>
		public const uint GL_LIGHT3								= 0x4003;
		#endregion GL_LIGHT3

		#region GL_LIGHT4
		/// <summary>
		/// True if light 4 is enabled
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: lighting/enable
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glIsEnabled" />
		/// </para>
		/// </remarks>
		public const uint GL_LIGHT4								= 0x4004;
		#endregion GL_LIGHT4

		#region GL_LIGHT5
		/// <summary>
		/// True if light 5 is enabled
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: lighting/enable
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glIsEnabled" />
		/// </para>
		/// </remarks>
		public const uint GL_LIGHT5								= 0x4005;
		#endregion GL_LIGHT5

		#region GL_LIGHT6
		/// <summary>
		/// True if light 6 is enabled
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: lighting/enable
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glIsEnabled" />
		/// </para>
		/// </remarks>
		public const uint GL_LIGHT6								= 0x4006;
		#endregion GL_LIGHT6

		#region GL_LIGHT7
		/// <summary>
		/// True if light 7 is enabled
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: lighting/enable
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_FALSE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glIsEnabled" />
		/// </para>
		/// </remarks>
		public const uint GL_LIGHT7								= 0x4007;
		#endregion GL_LIGHT7
		#endregion LightName

		#region LightParameter
		#region GL_AMBIENT
		/// <summary>
		/// Ambient material color OR Ambient intensity of light i
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: lighting
		/// </para>
		/// <para>
		/// Initial value: (0.2, 0.2, 0.2, 1.0) OR (0.0, 0.0, 0.0, 1.0)
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetMaterialfv" /> OR <see cref="glGetLightfv" />
		/// </para>
		/// </remarks>
		public const uint GL_AMBIENT							= 0x1200;
		#endregion GL_AMBIENT

		#region GL_DIFFUSE
		/// <summary>
		/// Diffuse material color OR Diffuse intensity of light i
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: lighting
		/// </para>
		/// <para>
		/// Initial value: (0.8, 0.8, 0.8, 1.0) OR --
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetMaterialfv" /> OR <see cref="glGetLightfv" />
		/// </para>
		/// </remarks>
		public const uint GL_DIFFUSE							= 0x1201;
		#endregion GL_DIFFUSE

		#region GL_SPECULAR
		/// <summary>
		/// Specular material color OR Specular intensity of light i
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: lighting
		/// </para>
		/// <para>
		/// Initial value: (0.0, 0.0, 0.0, 1.0) OR --
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetMaterialfv" /> OR <see cref="glGetLightfv" />
		/// </para>
		/// </remarks>
		public const uint GL_SPECULAR							= 0x1202;
		#endregion GL_SPECULAR

		#region GL_POSITION
		/// <summary>
		/// Position of light i
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: lighting
		/// </para>
		/// <para>
		/// Initial value: (0.0, 0.0, 1.0, 0.0)
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetLightfv" />
		/// </para>
		/// </remarks>
		public const uint GL_POSITION							= 0x1203;
		#endregion GL_POSITION

		#region GL_SPOT_DIRECTION
		/// <summary>
		/// Spotlight direction of light i
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: lighting
		/// </para>
		/// <para>
		/// Initial value: (0.0, 0.0, 1.0)
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetLightfv" />
		/// </para>
		/// </remarks>
		public const uint GL_SPOT_DIRECTION						= 0x1204;
		#endregion GL_SPOT_DIRECTION

		#region GL_SPOT_EXPONENT
		/// <summary>
		/// Spotlight exponent of light i
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: lighting
		/// </para>
		/// <para>
		/// Initial value: 0.0
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetLightfv" />
		/// </para>
		/// </remarks>
		public const uint GL_SPOT_EXPONENT						= 0x1205;
		#endregion GL_SPOT_EXPONENT

		#region GL_SPOT_CUTOFF
		/// <summary>
		/// Spotlight angle of light i
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: lighting
		/// </para>
		/// <para>
		/// Initial value: 180.0
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetLightfv" />
		/// </para>
		/// </remarks>
		public const uint GL_SPOT_CUTOFF						= 0x1206;
		#endregion GL_SPOT_CUTOFF

		#region GL_CONSTANT_ATTENUATION
		/// <summary>
		/// Constant attenuation factor
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: lighting
		/// </para>
		/// <para>
		/// Initial value: 1.0
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetLightfv" />
		/// </para>
		/// </remarks>
		public const uint GL_CONSTANT_ATTENUATION				= 0x1207;
		#endregion GL_CONSTANT_ATTENUATION

		#region GL_LINEAR_ATTENUATION
		/// <summary>
		/// Linear attenuation factor
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: lighting
		/// </para>
		/// <para>
		/// Initial value: 0.0
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetLightfv" />
		/// </para>
		/// </remarks>
		public const uint GL_LINEAR_ATTENUATION					= 0x1208;
		#endregion GL_LINEAR_ATTENUATION

		#region GL_QUADRATIC_ATTENUATION
		/// <summary>
		/// Quadratic attenuation factor
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: lighting
		/// </para>
		/// <para>
		/// Initial value: 0.0
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetLightfv" />
		/// </para>
		/// </remarks>
		public const uint GL_QUADRATIC_ATTENUATION				= 0x1209;
		#endregion GL_QUADRATIC_ATTENUATION
		#endregion LightParameter

		#region InterleavedArrays
		/*      GL_V2F */
		/*      GL_V3F */
		/*      GL_C4UB_V2F */
		/*      GL_C4UB_V3F */
		/*      GL_C3F_V3F */
		/*      GL_N3F_V3F */
		/*      GL_C4F_N3F_V3F */
		/*      GL_T2F_V3F */
		/*      GL_T4F_V4F */
		/*      GL_T2F_C4UB_V3F */
		/*      GL_T2F_C3F_V3F */
		/*      GL_T2F_N3F_V3F */
		/*      GL_T2F_C4F_N3F_V3F */
		/*      GL_T4F_C4F_N3F_V4F */
		#endregion InterleavedArrays

		#region ListMode
		public const uint GL_COMPILE							= 0x1300;
		public const uint GL_COMPILE_AND_EXECUTE				= 0x1301;
		#endregion ListMode

		#region ListNameType
		/*      GL_BYTE */
		/*      GL_UNSIGNED_BYTE */
		/*      GL_SHORT */
		/*      GL_UNSIGNED_SHORT */
		/*      GL_INT */
		/*      GL_UNSIGNED_INT */
		/*      GL_FLOAT */
		/*      GL_2_BYTES */
		/*      GL_3_BYTES */
		/*      GL_4_BYTES */
		#endregion ListNameType

		#region LogicOp
		public const uint GL_CLEAR								= 0x1500;
		public const uint GL_AND								= 0x1501;
		public const uint GL_AND_REVERSE						= 0x1502;
		public const uint GL_COPY								= 0x1503;
		public const uint GL_AND_INVERTED						= 0x1504;
		public const uint GL_NOOP								= 0x1505;
		public const uint GL_XOR								= 0x1506;
		public const uint GL_OR									= 0x1507;
		public const uint GL_NOR								= 0x1508;
		public const uint GL_EQUIV								= 0x1509;
		public const uint GL_INVERT								= 0x150A;
		public const uint GL_OR_REVERSE							= 0x150B;
		public const uint GL_COPY_INVERTED						= 0x150C;
		public const uint GL_OR_INVERTED						= 0x150D;
		public const uint GL_NAND								= 0x150E;
		public const uint GL_SET								= 0x150F;
		#endregion LogicOp

		#region MapTarget
		/*      GL_MAP1_COLOR_4 */
		/*      GL_MAP1_INDEX */
		/*      GL_MAP1_NORMAL */
		/*      GL_MAP1_TEXTURE_COORD_1 */
		/*      GL_MAP1_TEXTURE_COORD_2 */
		/*      GL_MAP1_TEXTURE_COORD_3 */
		/*      GL_MAP1_TEXTURE_COORD_4 */
		/*      GL_MAP1_VERTEX_3 */
		/*      GL_MAP1_VERTEX_4 */
		/*      GL_MAP2_COLOR_4 */
		/*      GL_MAP2_INDEX */
		/*      GL_MAP2_NORMAL */
		/*      GL_MAP2_TEXTURE_COORD_1 */
		/*      GL_MAP2_TEXTURE_COORD_2 */
		/*      GL_MAP2_TEXTURE_COORD_3 */
		/*      GL_MAP2_TEXTURE_COORD_4 */
		/*      GL_MAP2_VERTEX_3 */
		/*      GL_MAP2_VERTEX_4 */
		#endregion MapTarget

		#region MaterialFace
		/*      GL_FRONT */
		/*      GL_BACK */
		/*      GL_FRONT_AND_BACK */
		#endregion MaterialFace

		#region MaterialParameter
		#region GL_EMISSION
		/// <summary>
		/// Emissive material color
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: lighting
		/// </para>
		/// <para>
		/// Initial value: (0.0, 0.0, 0.0, 1.0)
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetMaterialfv" />
		/// </para>
		/// </remarks>
		public const uint GL_EMISSION							= 0x1600;
		#endregion GL_EMISSION

		#region GL_SHININESS
		/// <summary>
		/// Specular exponent of material
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: lighting
		/// </para>
		/// <para>
		/// Initial value: 0.0
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetMaterialfv" />
		/// </para>
		/// </remarks>
		public const uint GL_SHININESS							= 0x1601;
		#endregion GL_SHININESS

		public const uint GL_AMBIENT_AND_DIFFUSE				= 0x1602;

		#region GL_COLOR_INDEXES
		/// <summary>
		/// C (a) , C (d) , and C (s) for color-index lighting
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: lighting/enable
		/// </para>
		/// <para>
		/// Initial value: 0, 1, 1
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetFloatv" />
		/// </para>
		/// </remarks>
		public const uint GL_COLOR_INDEXES						= 0x1603;
		#endregion GL_COLOR_INDEXES
		/*      GL_AMBIENT */
		/*      GL_DIFFUSE */
		/*      GL_SPECULAR */
		#endregion MaterialParameter

		#region MatrixMode
		public const uint GL_MODELVIEW							= 0x1700;
		public const uint GL_PROJECTION							= 0x1701;

		#region GL_TEXTURE
		/// <summary>
		/// x-D texture image at level of detail i
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: --
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetTexImage" />
		/// </para>
		/// </remarks>
		public const uint GL_TEXTURE							= 0x1702;
		#endregion GL_TEXTURE
		#endregion MatrixMode

		#region MeshMode1
		/*      GL_POINT */
		/*      GL_LINE */
		#endregion MeshMode1

		#region MeshMode2
		/*      GL_POINT */
		/*      GL_LINE */
		/*      GL_FILL */
		#endregion MeshMode2

		#region NormalPointerType
		/*      GL_BYTE */
		/*      GL_SHORT */
		/*      GL_INT */
		/*      GL_FLOAT */
		/*      GL_DOUBLE */
		#endregion NormalPointerType

		#region PixelCopyType
		public const uint GL_COLOR								= 0x1800;
		public const uint GL_DEPTH								= 0x1801;
		public const uint GL_STENCIL							= 0x1802;
		#endregion PixelCopyType

		#region PixelFormat
		public const uint GL_COLOR_INDEX						= 0x1900;
		public const uint GL_STENCIL_INDEX						= 0x1901;
		public const uint GL_DEPTH_COMPONENT					= 0x1902;
		public const uint GL_RED								= 0x1903;
		public const uint GL_GREEN								= 0x1904;
		public const uint GL_BLUE								= 0x1905;
		public const uint GL_ALPHA								= 0x1906;
		public const uint GL_RGB								= 0x1907;
		public const uint GL_RGBA								= 0x1908;
		public const uint GL_LUMINANCE							= 0x1909;
		public const uint GL_LUMINANCE_ALPHA					= 0x190A;

		/*      GL_ABGR_EXT */						// WARNING
		/*      GL_BGR_EXT */
		/*      GL_BGRA_EXT */
		#endregion PixelFormat

		#region PixelMap
		/*      GL_PIXEL_MAP_I_TO_I */
		/*      GL_PIXEL_MAP_S_TO_S */
		/*      GL_PIXEL_MAP_I_TO_R */
		/*      GL_PIXEL_MAP_I_TO_G */
		/*      GL_PIXEL_MAP_I_TO_B */
		/*      GL_PIXEL_MAP_I_TO_A */
		/*      GL_PIXEL_MAP_R_TO_R */
		/*      GL_PIXEL_MAP_G_TO_G */
		/*      GL_PIXEL_MAP_B_TO_B */
		/*      GL_PIXEL_MAP_A_TO_A */
		#endregion PixelMap

		#region PixelStore
		/*      GL_UNPACK_SWAP_BYTES */
		/*      GL_UNPACK_LSB_FIRST */
		/*      GL_UNPACK_ROW_LENGTH */
		/*      GL_UNPACK_SKIP_ROWS */
		/*      GL_UNPACK_SKIP_PIXELS */
		/*      GL_UNPACK_ALIGNMENT */
		/*      GL_PACK_SWAP_BYTES */
		/*      GL_PACK_LSB_FIRST */
		/*      GL_PACK_ROW_LENGTH */
		/*      GL_PACK_SKIP_ROWS */
		/*      GL_PACK_SKIP_PIXELS */
		/*      GL_PACK_ALIGNMENT */
		#endregion PixelStore

		#region PixelTransfer
		/*      GL_MAP_COLOR */
		/*      GL_MAP_STENCIL */
		/*      GL_INDEX_SHIFT */
		/*      GL_INDEX_OFFSET */
		/*      GL_RED_SCALE */
		/*      GL_RED_BIAS */
		/*      GL_GREEN_SCALE */
		/*      GL_GREEN_BIAS */
		/*      GL_BLUE_SCALE */
		/*      GL_BLUE_BIAS */
		/*      GL_ALPHA_SCALE */
		/*      GL_ALPHA_BIAS */
		/*      GL_DEPTH_SCALE */
		/*      GL_DEPTH_BIAS */
		#endregion PixelTransfer

		#region PixelType
		public const uint GL_BITMAP								= 0x1A00;
		/*      GL_BYTE */
		/*      GL_UNSIGNED_BYTE */
		/*      GL_SHORT */
		/*      GL_UNSIGNED_SHORT */
		/*      GL_INT */
		/*      GL_UNSIGNED_INT */
		/*      GL_FLOAT */

		// ADDED FROM OpenGL.cs !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
		/*      GL_UNSIGNED_BYTE_3_3_2_EXT */			// WARNING
		/*      GL_UNSIGNED_SHORT_4_4_4_4_EXT */
		/*      GL_UNSIGNED_SHORT_5_5_5_1_EXT */
		/*      GL_UNSIGNED_INT_8_8_8_8_EXT */
		/*      GL_UNSIGNED_INT_10_10_10_2_EXT */
		#endregion PixelType

		#region PolygonMode
		public const uint GL_POINT								= 0x1B00;
		public const uint GL_LINE								= 0x1B01;
		public const uint GL_FILL								= 0x1B02;
		#endregion PolygonMode

		#region ReadBufferMode
		/*      GL_FRONT_LEFT */
		/*      GL_FRONT_RIGHT */
		/*      GL_BACK_LEFT */
		/*      GL_BACK_RIGHT */
		/*      GL_FRONT */
		/*      GL_BACK */
		/*      GL_LEFT */
		/*      GL_RIGHT */
		/*      GL_AUX0 */
		/*      GL_AUX1 */
		/*      GL_AUX2 */
		/*      GL_AUX3 */
		#endregion ReadBufferMode

		#region RenderingMode
		public const uint GL_RENDER								= 0x1C00;
		public const uint GL_FEEDBACK							= 0x1C01;
		public const uint GL_SELECT								= 0x1C02;
		#endregion RenderingMode

		#region ShadingModel
		public const uint GL_FLAT								= 0x1D00;
		public const uint GL_SMOOTH								= 0x1D01;
		#endregion ShadingModel

		#region StencilFunction
		/*      GL_NEVER */
		/*      GL_LESS */
		/*      GL_EQUAL */
		/*      GL_LEQUAL */
		/*      GL_GREATER */
		/*      GL_NOTEQUAL */
		/*      GL_GEQUAL */
		/*      GL_ALWAYS */
		#endregion StencilFunction

		#region StencilOp
		/*      GL_ZERO */
		public const uint GL_KEEP								= 0x1E00;
		public const uint GL_REPLACE							= 0x1E01;
		public const uint GL_INCR								= 0x1E02;
		public const uint GL_DECR								= 0x1E03;
		/*      GL_INVERT */
		#endregion StencilOp

		#region StringName
		public const uint GL_VENDOR								= 0x1F00;
		public const uint GL_RENDERER							= 0x1F01;
		public const uint GL_VERSION							= 0x1F02;
		public const uint GL_EXTENSIONS							= 0x1F03;
		#endregion StringName

		#region TextureCoordName
		public const uint GL_S									= 0x2000;
		public const uint GL_T									= 0x2001;
		public const uint GL_R									= 0x2002;
		public const uint GL_Q									= 0x2003;
		#endregion TextureCoordName

		#region TexCoordPointerType
		/*      GL_SHORT */
		/*      GL_INT */
		/*      GL_FLOAT */
		/*      GL_DOUBLE */
		#endregion TexCoordPointerType

		#region TextureEnvMode
		public const uint GL_MODULATE							= 0x2100;
		public const uint GL_DECAL								= 0x2101;
		/*      GL_BLEND */
		/*      GL_REPLACE */

		// ADDED FROM OpenGL.cs !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
		/*      GL_ADD */
		#endregion TextureEnvMode

		#region TextureEnvParameter
		#region GL_TEXTURE_ENV_MODE
		/// <summary>
		/// Texture application function
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: texture
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_MODULATE" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetTexEnviv" />
		/// </para>
		/// </remarks>
		public const uint GL_TEXTURE_ENV_MODE					= 0x2200;
		#endregion GL_TEXTURE_ENV_MODE

		#region GL_TEXTURE_ENV_COLOR
		/// <summary>
		/// Texture environment color
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: texture
		/// </para>
		/// <para>
		/// Initial value: 0,0,0,0
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetTexEnvfv" />
		/// </para>
		/// </remarks>
		public const uint GL_TEXTURE_ENV_COLOR					= 0x2201;
		#endregion GL_TEXTURE_ENV_COLOR
		#endregion TextureEnvParameter

		#region TextureEnvTarget
		public const uint GL_TEXTURE_ENV						= 0x2300;
		#endregion TextureEnvTarget

		#region TextureGenMode
		#region GL_EYE_LINEAR
		/// <summary>
		/// Texgen plane equation coefficients
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: texture
		/// </para>
		/// <para>
		/// Initial value: --
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetTexGenfv" />
		/// </para>
		/// </remarks>
		public const uint GL_EYE_LINEAR							= 0x2400;
		#endregion GL_EYE_LINEAR

		#region GL_OBJECT_LINEAR
		/// <summary>
		/// Texgen object linear coefficients
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: texture
		/// </para>
		/// <para>
		/// Initial value: --
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetTexGenfv" />
		/// </para>
		/// </remarks>
		public const uint GL_OBJECT_LINEAR						= 0x2401;
		#endregion GL_OBJECT_LINEAR

		public const uint GL_SPHERE_MAP							= 0x2402;
		#endregion TextureGenMode

		#region TextureGenParameter
		#region GL_TEXTURE_GEN_MODE
		/// <summary>
		/// Function used for texgen
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: texture
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_EYTE_LINEAR" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetTexGeniv" />
		/// </para>
		/// </remarks>
		public const uint GL_TEXTURE_GEN_MODE					= 0x2500;
		#endregion GL_TEXTURE_GEN_MODE

		public const uint GL_OBJECT_PLANE						= 0x2501;
		public const uint GL_EYE_PLANE							= 0x2502;
		#endregion TextureGenParameter

		#region TextureMagFilter
		public const uint GL_NEAREST							= 0x2600;
		public const uint GL_LINEAR								= 0x2601;
		#endregion TextureMagFilter

		#region TextureMinFilter
		/*      GL_NEAREST */
		/*      GL_LINEAR */
		public const uint GL_NEAREST_MIPMAP_NEAREST				= 0x2700;
		public const uint GL_LINEAR_MIPMAP_NEAREST				= 0x2701;
		public const uint GL_NEAREST_MIPMAP_LINEAR				= 0x2702;
		public const uint GL_LINEAR_MIPMAP_LINEAR				= 0x2703;
		#endregion TextureMinFilter

		#region TextureParamenterName
		#region GL_TEXTURE_MAG_FILTER
		/// <summary>
		/// Texture magnification function
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: texture
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_LINEAR" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetTexParameter" />
		/// </para>
		/// </remarks>
		public const uint GL_TEXTURE_MAG_FILTER					= 0x2800;
		#endregion GL_TEXTURE_MAG_FILTER

		#region GL_TEXTURE_MIN_FILTER
		/// <summary>
		/// Texture minification function
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: texture
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_NEAREST_MIPMAP_LINEAR" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetTexParameter" />
		/// </para>
		/// </remarks>
		public const uint GL_TEXTURE_MIN_FILTER					= 0x2801;
		#endregion GL_TEXTURE_MIN_FILTER

		#region GL_TEXTURE_WRAP_S
		/// <summary>
		/// Texture wrap mode
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: texture
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_REPEAT" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetTexParameter" />
		/// </para>
		/// </remarks>
		public const uint GL_TEXTURE_WRAP_S						= 0x2802;
		#endregion GL_TEXTURE_WRAP_S

		#region GL_TEXTURE_WRAP_T
		/// <summary>
		/// Texture wrap mode
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: texture
		/// </para>
		/// <para>
		/// Initial value: <see cref="GL_REPEAT" />
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetTexParameter" />
		/// </para>
		/// </remarks>
		public const uint GL_TEXTURE_WRAP_T						= 0x2803;
		#endregion GL_TEXTURE_WRAP_T

		/*      GL_TEXTURE_BORDER_COLOR */
		/*      GL_TEXTURE_PRIORITY */
		#endregion TextureParamenterName

		#region TextureTarget
		/*      GL_TEXTURE_1D */
		/*      GL_TEXTURE_2D */
		/*      GL_PROXY_TEXTURE_1D */
		/*      GL_PROXY_TEXTURE_2D */
		#endregion TextureTarget

		#region TextureWrapMode
		public const uint GL_CLAMP								= 0x2900;
		public const uint GL_REPEAT								= 0x2901;
		public const uint GL_MIRRORED_REPEAT     = 0x8370;
		#endregion TextureWrapMode

		#region VertexPointerType
		/*      GL_SHORT */
		/*      GL_INT */
		/*      GL_FLOAT */
		/*      GL_DOUBLE */
		#endregion VertexPointerType

		#region ClientAttribMask
		public const uint GL_CLIENT_PIXEL_STORE_BIT				= 0x00000001;
		public const uint GL_CLIENT_VERTEX_ARRAY_BIT			= 0x00000002;
		public const uint GL_CLIENT_ALL_ATTRIB_BITS				= 0xFFFFFFFF;
		#endregion ClientAttribMask

		#region Polygon_Offset
		public const uint GL_POLYGON_OFFSET_FACTOR				= 0x8038;
		public const uint GL_POLYGON_OFFSET_UNITS				= 0x2A00;
		public const uint GL_POLYGON_OFFSET_POINT				= 0x2A01;
		public const uint GL_POLYGON_OFFSET_LINE				= 0x2A02;
		public const uint GL_POLYGON_OFFSET_FILL				= 0x8037;
		#endregion Polygon_Offset

		#region Texture
		public const uint GL_ALPHA4								= 0x803B;
		public const uint GL_ALPHA8								= 0x803C;
		public const uint GL_ALPHA12							= 0x803D;
		public const uint GL_ALPHA16							= 0x803E;
		public const uint GL_LUMINANCE4							= 0x803F;
		public const uint GL_LUMINANCE8							= 0x8040;
		public const uint GL_LUMINANCE12						= 0x8041;
		public const uint GL_LUMINANCE16						= 0x8042;
		public const uint GL_LUMINANCE4_ALPHA4					= 0x8043;
		public const uint GL_LUMINANCE6_ALPHA2					= 0x8044;
		public const uint GL_LUMINANCE8_ALPHA8					= 0x8045;
		public const uint GL_LUMINANCE12_ALPHA4					= 0x8046;
		public const uint GL_LUMINANCE12_ALPHA12				= 0x8047;
		public const uint GL_LUMINANCE16_ALPHA16				= 0x8048;
		public const uint GL_INTENSITY							= 0x8049;
		public const uint GL_INTENSITY4							= 0x804A;
		public const uint GL_INTENSITY8							= 0x804B;
		public const uint GL_INTENSITY12						= 0x804C;
		public const uint GL_INTENSITY16						= 0x804D;
		public const uint GL_R3_G3_B2							= 0x2A10;
		public const uint GL_RGB4								= 0x804F;
		public const uint GL_RGB5								= 0x8050;
		public const uint GL_RGB8								= 0x8051;
		public const uint GL_RGB10								= 0x8052;
		public const uint GL_RGB12								= 0x8053;
		public const uint GL_RGB16								= 0x8054;
		public const uint GL_RGBA2								= 0x8055;
		public const uint GL_RGBA4								= 0x8056;
		public const uint GL_RGB5_A1							= 0x8057;
		public const uint GL_RGBA8								= 0x8058;
		public const uint GL_RGB10_A2							= 0x8059;
		public const uint GL_RGBA12								= 0x805A;
		public const uint GL_RGBA16								= 0x805B;
		public const uint GL_TEXTURE_RED_SIZE					= 0x805C;
		public const uint GL_TEXTURE_GREEN_SIZE					= 0x805D;
		public const uint GL_TEXTURE_BLUE_SIZE					= 0x805E;
		public const uint GL_TEXTURE_ALPHA_SIZE					= 0x805F;
		public const uint GL_TEXTURE_LUMINANCE_SIZE				= 0x8060;
		public const uint GL_TEXTURE_INTENSITY_SIZE				= 0x8061;
		public const uint GL_PROXY_TEXTURE_1D					= 0x8063;
		public const uint GL_PROXY_TEXTURE_2D					= 0x8064;
		#endregion Texture

		#region Texture_object
		public const uint GL_TEXTURE_PRIORITY					= 0x8066;
		public const uint GL_TEXTURE_RESIDENT					= 0x8067;
		public const uint GL_TEXTURE_BINDING_1D					= 0x8068;
		public const uint GL_TEXTURE_BINDING_2D					= 0x8069;
		#endregion Texture_object

		#region Vertex_array
		public const uint GL_VERTEX_ARRAY						= 0x8074;
		public const uint GL_NORMAL_ARRAY						= 0x8075;
		public const uint GL_COLOR_ARRAY						= 0x8076;
		public const uint GL_INDEX_ARRAY						= 0x8077;
		public const uint GL_TEXTURE_COORD_ARRAY				= 0x8078;
		public const uint GL_EDGE_FLAG_ARRAY					= 0x8079;
		public const uint GL_VERTEX_ARRAY_SIZE					= 0x807A;
		public const uint GL_VERTEX_ARRAY_TYPE					= 0x807B;
		public const uint GL_VERTEX_ARRAY_STRIDE				= 0x807C;
		public const uint GL_NORMAL_ARRAY_TYPE					= 0x807E;
		public const uint GL_NORMAL_ARRAY_STRIDE				= 0x807F;
		public const uint GL_COLOR_ARRAY_SIZE					= 0x8081;
		public const uint GL_COLOR_ARRAY_TYPE					= 0x8082;
		public const uint GL_COLOR_ARRAY_STRIDE					= 0x8083;
		public const uint GL_INDEX_ARRAY_TYPE					= 0x8085;
		public const uint GL_INDEX_ARRAY_STRIDE					= 0x8086;
		public const uint GL_TEXTURE_COORD_ARRAY_SIZE			= 0x8088;
		public const uint GL_TEXTURE_COORD_ARRAY_TYPE			= 0x8089;
		public const uint GL_TEXTURE_COORD_ARRAY_STRIDE			= 0x808A;
		public const uint GL_EDGE_FLAG_ARRAY_STRIDE				= 0x808C;
		public const uint GL_VERTEX_ARRAY_POINTER				= 0x808E;
		public const uint GL_NORMAL_ARRAY_POINTER				= 0x808F;
		public const uint GL_COLOR_ARRAY_POINTER				= 0x8090;
		public const uint GL_INDEX_ARRAY_POINTER				= 0x8091;
		public const uint GL_TEXTURE_COORD_ARRAY_POINTER		= 0x8092;
		public const uint GL_EDGE_FLAG_ARRAY_POINTER			= 0x8093;
		public const uint GL_V2F								= 0x2A20;
		public const uint GL_V3F								= 0x2A21;
		public const uint GL_C4UB_V2F							= 0x2A22;
		public const uint GL_C4UB_V3F							= 0x2A23;
		public const uint GL_C3F_V3F							= 0x2A24;
		public const uint GL_N3F_V3F							= 0x2A25;
		public const uint GL_C4F_N3F_V3F						= 0x2A26;
		public const uint GL_T2F_V3F							= 0x2A27;
		public const uint GL_T4F_V4F							= 0x2A28;
		public const uint GL_T2F_C4UB_V3F						= 0x2A29;
		public const uint GL_T2F_C3F_V3F						= 0x2A2A;
		public const uint GL_T2F_N3F_V3F						= 0x2A2B;
		public const uint GL_T2F_C4F_N3F_V3F					= 0x2A2C;
		public const uint GL_T4F_C4F_N3F_V4F					= 0x2A2D;
		#endregion Vertex_array

		#region Extensions
		public const uint GL_EXT_vertex_array					= 1;
		public const uint GL_EXT_bgra							= 1;
		public const uint GL_EXT_paletted_texture				= 1;
		public const uint GL_WIN_swap_hint						= 1;
		public const uint GL_WIN_draw_range_elements			= 1;
		// #define GL_WIN_phong_shading              1
		// #define GL_WIN_specular_fog               1
		#endregion Extensions

		#region EXT_bgra
		public const uint GL_BGR_EXT							= 0x80E0;
		public const uint GL_BGRA_EXT							= 0x80E1;
		#endregion EXT_bgra

		#region EXT_paletted_texture
		// These must match the GL_COLOR_TABLE_*_SGI enumerants
		public const uint GL_COLOR_TABLE_FORMAT_EXT				= 0x80D8;
		public const uint GL_COLOR_TABLE_WIDTH_EXT				= 0x80D9;
		public const uint GL_COLOR_TABLE_RED_SIZE_EXT			= 0x80DA;
		public const uint GL_COLOR_TABLE_GREEN_SIZE_EXT			= 0x80DB;
		public const uint GL_COLOR_TABLE_BLUE_SIZE_EXT			= 0x80DC;
		public const uint GL_COLOR_TABLE_ALPHA_SIZE_EXT			= 0x80DD;
		public const uint GL_COLOR_TABLE_LUMINANCE_SIZE_EXT		= 0x80DE;
		public const uint GL_COLOR_TABLE_INTENSITY_SIZE_EXT		= 0x80DF;

		public const uint GL_COLOR_INDEX1_EXT					= 0x80E2;
		public const uint GL_COLOR_INDEX2_EXT					= 0x80E3;
		public const uint GL_COLOR_INDEX4_EXT					= 0x80E4;
		public const uint GL_COLOR_INDEX8_EXT					= 0x80E5;
		public const uint GL_COLOR_INDEX12_EXT					= 0x80E6;
		public const uint GL_COLOR_INDEX16_EXT					= 0x80E7;
		#endregion EXT_paletted_texture

		#region WIN_draw_range_elements
		public const uint GL_MAX_ELEMENTS_VERTICES_WIN			= 0x80E8;
		public const uint GL_MAX_ELEMENTS_INDICES_WIN			= 0x80E9;
		#endregion WIN_draw_range_elements

		#region WIN_phong_shading
		public const uint GL_PHONG_WIN							= 0x80EA;
		public const uint GL_PHONG_HINT_WIN						= 0x80EB;
		#endregion WIN_phong_shading

		#region WIN_specular_fog
		public const uint GL_FOG_SPECULAR_TEXTURE_WIN			= 0x80EC;
		#endregion WIN_specular_fog

		#region For compatibility with OpenGL v1.0
		public const uint GL_LOGIC_OP							= GL_INDEX_LOGIC_OP;

		#region GL_TEXTURE_COMPONENTS
		/// <summary>
		/// Texture image components
		/// </summary>
		/// <remarks>
		/// <para>
		/// Attribute group: --
		/// </para>
		/// <para>
		/// Initial value: 1
		/// </para>
		/// <para>
		/// Get command: <see cref="glGetTexLevelParameter" />
		/// </para>
		/// </remarks>
		public const uint GL_TEXTURE_COMPONENTS					= GL_TEXTURE_INTERNAL_FORMAT;
		#endregion GL_TEXTURE_COMPONENTS
		#endregion For compatibility with OpenGL v1.0

		#region OpenGL Extension Constants

		public const uint GL_RESCALE_NORMAL = 0x803A;
		public const uint GL_CLAMP_TO_EDGE = 0x812F;
		public const uint GL_MAX_ELEMENTS_VERTICES = 0x80E8;
		public const uint GL_MAX_ELEMENTS_INDICES = 0x80E9;
		public const uint GL_BGR = 0x80E0;
		public const uint GL_BGRA = 0x80E1;
		public const uint GL_UNSIGNED_BYTE_3_3_2 = 0x8032;
		public const uint GL_UNSIGNED_BYTE_2_3_3_REV = 0x8362;
		public const uint GL_UNSIGNED_SHORT_5_6_5 = 0x8363;
		public const uint GL_UNSIGNED_SHORT_5_6_5_REV = 0x8364;
		public const uint GL_UNSIGNED_SHORT_4_4_4_4 = 0x8033;
		public const uint GL_UNSIGNED_SHORT_4_4_4_4_REV = 0x8365;
		public const uint GL_UNSIGNED_SHORT_5_5_5_1 = 0x8034;
		public const uint GL_UNSIGNED_SHORT_1_5_5_5_REV = 0x8366;
		public const uint GL_UNSIGNED_INT_8_8_8_8 = 0x8035;
		public const uint GL_UNSIGNED_INT_8_8_8_8_REV = 0x8367;
		public const uint GL_UNSIGNED_INT_10_10_10_2 = 0x8036;
		public const uint GL_UNSIGNED_INT_2_10_10_10_REV = 0x8368;
		public const uint GL_LIGHT_MODEL_COLOR_CONTROL = 0x81F8;
		public const uint GL_SINGLE_COLOR = 0x81F9;
		public const uint GL_SEPARATE_SPECULAR_COLOR = 0x81FA;
		public const uint GL_TEXTURE_MIN_LOD = 0x813A;
		public const uint GL_TEXTURE_MAX_LOD = 0x813B;
		public const uint GL_TEXTURE_BASE_LEVEL = 0x813C;
		public const uint GL_TEXTURE_MAX_LEVEL = 0x813D;
		public const uint GL_SMOOTH_POINT_SIZE_RANGE = 0x0B12;
		public const uint GL_SMOOTH_POINT_SIZE_GRANULARITY = 0x0B13;
		public const uint GL_SMOOTH_LINE_WIDTH_RANGE = 0x0B22;
		public const uint GL_SMOOTH_LINE_WIDTH_GRANULARITY = 0x0B23;
		public const uint GL_ALIASED_POINT_SIZE_RANGE = 0x846D;
		public const uint GL_ALIASED_LINE_WIDTH_RANGE = 0x846E;
		public const uint GL_PACK_SKIP_IMAGES = 0x806B;
		public const uint GL_PACK_IMAGE_HEIGHT = 0x806C;
		public const uint GL_UNPACK_SKIP_IMAGES = 0x806D;
		public const uint GL_UNPACK_IMAGE_HEIGHT = 0x806E;
		public const uint GL_TEXTURE_3D = 0x806F;
		public const uint GL_PROXY_TEXTURE_3D = 0x8070;
		public const uint GL_TEXTURE_DEPTH = 0x8071;
		public const uint GL_TEXTURE_WRAP_R = 0x8072;
		public const uint GL_MAX_3D_TEXTURE_SIZE = 0x8073;
		public const uint GL_TEXTURE_BINDING_3D = 0x806A;
		public const uint GL_COLOR_TABLE = 0x80D0;
		public const uint GL_POST_CONVOLUTION_COLOR_TABLE = 0x80D1;
		public const uint GL_POST_COLOR_MATRIX_COLOR_TABLE = 0x80D2;
		public const uint GL_PROXY_COLOR_TABLE = 0x80D3;
		public const uint GL_PROXY_POST_CONVOLUTION_COLOR_TABLE = 0x80D4;
		public const uint GL_PROXY_POST_COLOR_MATRIX_COLOR_TABLE = 0x80D5;
		public const uint GL_COLOR_TABLE_SCALE = 0x80D6;
		public const uint GL_COLOR_TABLE_BIAS = 0x80D7;
		public const uint GL_COLOR_TABLE_FORMAT = 0x80D8;
		public const uint GL_COLOR_TABLE_WIDTH = 0x80D9;
		public const uint GL_COLOR_TABLE_RED_SIZE = 0x80DA;
		public const uint GL_COLOR_TABLE_GREEN_SIZE = 0x80DB;
		public const uint GL_COLOR_TABLE_BLUE_SIZE = 0x80DC;
		public const uint GL_COLOR_TABLE_ALPHA_SIZE = 0x80DD;
		public const uint GL_COLOR_TABLE_LUMINANCE_SIZE = 0x80DE;
		public const uint GL_COLOR_TABLE_INTENSITY_SIZE = 0x80DF;
		public const uint GL_CONVOLUTION_1D = 0x8010;
		public const uint GL_CONVOLUTION_2D = 0x8011;
		public const uint GL_SEPARABLE_2D = 0x8012;
		public const uint GL_CONVOLUTION_BORDER_MODE = 0x8013;
		public const uint GL_CONVOLUTION_FILTER_SCALE = 0x8014;
		public const uint GL_CONVOLUTION_FILTER_BIAS = 0x8015;
		public const uint GL_REDUCE = 0x8016;
		public const uint GL_CONVOLUTION_FORMAT = 0x8017;
		public const uint GL_CONVOLUTION_WIDTH = 0x8018;
		public const uint GL_CONVOLUTION_HEIGHT = 0x8019;
		public const uint GL_MAX_CONVOLUTION_WIDTH = 0x801A;
		public const uint GL_MAX_CONVOLUTION_HEIGHT = 0x801B;
		public const uint GL_POST_CONVOLUTION_RED_SCALE = 0x801C;
		public const uint GL_POST_CONVOLUTION_GREEN_SCALE = 0x801D;
		public const uint GL_POST_CONVOLUTION_BLUE_SCALE = 0x801E;
		public const uint GL_POST_CONVOLUTION_ALPHA_SCALE = 0x801F;
		public const uint GL_POST_CONVOLUTION_RED_BIAS = 0x8020;
		public const uint GL_POST_CONVOLUTION_GREEN_BIAS = 0x8021;
		public const uint GL_POST_CONVOLUTION_BLUE_BIAS = 0x8022;
		public const uint GL_POST_CONVOLUTION_ALPHA_BIAS = 0x8023;
		public const uint GL_CONSTANT_BORDER = 0x8151;
		public const uint GL_REPLICATE_BORDER = 0x8153;
		public const uint GL_CONVOLUTION_BORDER_COLOR = 0x8154;
		public const uint GL_COLOR_MATRIX = 0x80B1;
		public const uint GL_COLOR_MATRIX_STACK_DEPTH = 0x80B2;
		public const uint GL_MAX_COLOR_MATRIX_STACK_DEPTH = 0x80B3;
		public const uint GL_POST_COLOR_MATRIX_RED_SCALE = 0x80B4;
		public const uint GL_POST_COLOR_MATRIX_GREEN_SCALE = 0x80B5;
		public const uint GL_POST_COLOR_MATRIX_BLUE_SCALE = 0x80B6;
		public const uint GL_POST_COLOR_MATRIX_ALPHA_SCALE = 0x80B7;
		public const uint GL_POST_COLOR_MATRIX_RED_BIAS = 0x80B8;
		public const uint GL_POST_COLOR_MATRIX_GREEN_BIAS = 0x80B9;
		public const uint GL_POST_COLOR_MATRIX_BLUE_BIAS = 0x80BA;
		public const uint GL_POST_COLOR_MATRIX_ALPHA_BIAS = 0x80BB;
		public const uint GL_HISTOGRAM = 0x8024;
		public const uint GL_PROXY_HISTOGRAM = 0x8025;
		public const uint GL_HISTOGRAM_WIDTH = 0x8026;
		public const uint GL_HISTOGRAM_FORMAT = 0x8027;
		public const uint GL_HISTOGRAM_RED_SIZE = 0x8028;
		public const uint GL_HISTOGRAM_GREEN_SIZE = 0x8029;
		public const uint GL_HISTOGRAM_BLUE_SIZE = 0x802A;
		public const uint GL_HISTOGRAM_ALPHA_SIZE = 0x802B;
		public const uint GL_HISTOGRAM_LUMINANCE_SIZE = 0x802C;
		public const uint GL_HISTOGRAM_SINK = 0x802D;
		public const uint GL_MINMAX = 0x802E;
		public const uint GL_MINMAX_FORMAT = 0x802F;
		public const uint GL_MINMAX_SINK = 0x8030;
		public const uint GL_TABLE_TOO_LARGE = 0x8031;
		public const uint GL_BLEND_EQUATION = 0x8009;
		public const uint GL_MIN = 0x8007;
		public const uint GL_MAX = 0x8008;
		public const uint GL_FUNC_ADD = 0x8006;
		public const uint GL_FUNC_SUBTRACT = 0x800A;
		public const uint GL_FUNC_REVERSE_SUBTRACT = 0x800B;
		public const uint GL_BLEND_COLOR = 0x8005;
		public const uint GL_CONSTANT_COLOR = 0x8001;
		public const uint GL_ONE_MINUS_CONSTANT_COLOR = 0x8002;
		public const uint GL_CONSTANT_ALPHA = 0x8003;
		public const uint GL_ONE_MINUS_CONSTANT_ALPHA = 0x8004;
		public const uint GL_TEXTURE0 = 0x84C0;
		public const uint GL_TEXTURE1 = 0x84C1;
		public const uint GL_TEXTURE2 = 0x84C2;
		public const uint GL_TEXTURE3 = 0x84C3;
		public const uint GL_TEXTURE4 = 0x84C4;
		public const uint GL_TEXTURE5 = 0x84C5;
		public const uint GL_TEXTURE6 = 0x84C6;
		public const uint GL_TEXTURE7 = 0x84C7;
		public const uint GL_TEXTURE8 = 0x84C8;
		public const uint GL_TEXTURE9 = 0x84C9;
		public const uint GL_TEXTURE10 = 0x84CA;
		public const uint GL_TEXTURE11 = 0x84CB;
		public const uint GL_TEXTURE12 = 0x84CC;
		public const uint GL_TEXTURE13 = 0x84CD;
		public const uint GL_TEXTURE14 = 0x84CE;
		public const uint GL_TEXTURE15 = 0x84CF;
		public const uint GL_TEXTURE16 = 0x84D0;
		public const uint GL_TEXTURE17 = 0x84D1;
		public const uint GL_TEXTURE18 = 0x84D2;
		public const uint GL_TEXTURE19 = 0x84D3;
		public const uint GL_TEXTURE20 = 0x84D4;
		public const uint GL_TEXTURE21 = 0x84D5;
		public const uint GL_TEXTURE22 = 0x84D6;
		public const uint GL_TEXTURE23 = 0x84D7;
		public const uint GL_TEXTURE24 = 0x84D8;
		public const uint GL_TEXTURE25 = 0x84D9;
		public const uint GL_TEXTURE26 = 0x84DA;
		public const uint GL_TEXTURE27 = 0x84DB;
		public const uint GL_TEXTURE28 = 0x84DC;
		public const uint GL_TEXTURE29 = 0x84DD;
		public const uint GL_TEXTURE30 = 0x84DE;
		public const uint GL_TEXTURE31 = 0x84DF;
		public const uint GL_ACTIVE_TEXTURE = 0x84E0;
		public const uint GL_CLIENT_ACTIVE_TEXTURE = 0x84E1;
		public const uint GL_MAX_TEXTURE_UNITS = 0x84E2;
		public const uint GL_NORMAL_MAP = 0x8511;
		public const uint GL_REFLECTION_MAP = 0x8512;
		public const uint GL_TEXTURE_CUBE_MAP = 0x8513;
		public const uint GL_TEXTURE_BINDING_CUBE_MAP = 0x8514;
		public const uint GL_TEXTURE_CUBE_MAP_POSITIVE_X = 0x8515;
		public const uint GL_TEXTURE_CUBE_MAP_NEGATIVE_X = 0x8516;
		public const uint GL_TEXTURE_CUBE_MAP_POSITIVE_Y = 0x8517;
		public const uint GL_TEXTURE_CUBE_MAP_NEGATIVE_Y = 0x8518;
		public const uint GL_TEXTURE_CUBE_MAP_POSITIVE_Z = 0x8519;
		public const uint GL_TEXTURE_CUBE_MAP_NEGATIVE_Z = 0x851A;
		public const uint GL_PROXY_TEXTURE_CUBE_MAP = 0x851B;
		public const uint GL_MAX_CUBE_MAP_TEXTURE_SIZE = 0x851C;
		public const uint GL_COMPRESSED_ALPHA = 0x84E9;
		public const uint GL_COMPRESSED_LUMINANCE = 0x84EA;
		public const uint GL_COMPRESSED_LUMINANCE_ALPHA = 0x84EB;
		public const uint GL_COMPRESSED_INTENSITY = 0x84EC;
		public const uint GL_COMPRESSED_RGB = 0x84ED;
		public const uint GL_COMPRESSED_RGBA = 0x84EE;
		public const uint GL_TEXTURE_COMPRESSION_HINT = 0x84EF;
		public const uint GL_TEXTURE_COMPRESSED_IMAGE_SIZE = 0x86A0;
		public const uint GL_TEXTURE_COMPRESSED = 0x86A1;
		public const uint GL_NUM_COMPRESSED_TEXTURE_FORMATS = 0x86A2;
		public const uint GL_COMPRESSED_TEXTURE_FORMATS = 0x86A3;
		public const uint GL_MULTISAMPLE = 0x809D;
		public const uint GL_SAMPLE_ALPHA_TO_COVERAGE = 0x809E;
		public const uint GL_SAMPLE_ALPHA_TO_ONE = 0x809F;
		public const uint GL_SAMPLE_COVERAGE = 0x80A0;
		public const uint GL_SAMPLE_BUFFERS = 0x80A8;
		public const uint GL_SAMPLES = 0x80A9;
		public const uint GL_SAMPLE_COVERAGE_VALUE = 0x80AA;
		public const uint GL_SAMPLE_COVERAGE_INVERT = 0x80AB;
		public const uint GL_MULTISAMPLE_BIT = 0x20000000;
		public const uint GL_TRANSPOSE_MODELVIEW_MATRIX = 0x84E3;
		public const uint GL_TRANSPOSE_PROJECTION_MATRIX = 0x84E4;
		public const uint GL_TRANSPOSE_TEXTURE_MATRIX = 0x84E5;
		public const uint GL_TRANSPOSE_COLOR_MATRIX = 0x84E6;
		public const uint GL_COMBINE = 0x8570;
		public const uint GL_COMBINE_RGB = 0x8571;
		public const uint GL_COMBINE_ALPHA = 0x8572;
		public const uint GL_SOURCE0_RGB = 0x8580;
		public const uint GL_SOURCE1_RGB = 0x8581;
		public const uint GL_SOURCE2_RGB = 0x8582;
		public const uint GL_SOURCE0_ALPHA = 0x8588;
		public const uint GL_SOURCE1_ALPHA = 0x8589;
		public const uint GL_SOURCE2_ALPHA = 0x858A;
		public const uint GL_OPERAND0_RGB = 0x8590;
		public const uint GL_OPERAND1_RGB = 0x8591;
		public const uint GL_OPERAND2_RGB = 0x8592;
		public const uint GL_OPERAND0_ALPHA = 0x8598;
		public const uint GL_OPERAND1_ALPHA = 0x8599;
		public const uint GL_OPERAND2_ALPHA = 0x859A;
		public const uint GL_RGB_SCALE = 0x8573;
		public const uint GL_ADD_SIGNED = 0x8574;
		public const uint GL_INTERPOLATE = 0x8575;
		public const uint GL_SUBTRACT = 0x84E7;
		public const uint GL_CONSTANT = 0x8576;
		public const uint GL_PRIMARY_COLOR = 0x8577;
		public const uint GL_PREVIOUS = 0x8578;
		public const uint GL_DOT3_RGB = 0x86AE;
		public const uint GL_DOT3_RGBA = 0x86AF;
		public const uint GL_CLAMP_TO_BORDER = 0x812D;
		public const uint GL_ARB_multitexture = 1;
		public const uint GL_TEXTURE0_ARB = 0x84C0;
		public const uint GL_TEXTURE1_ARB = 0x84C1;
		public const uint GL_TEXTURE2_ARB = 0x84C2;
		public const uint GL_TEXTURE3_ARB = 0x84C3;
		public const uint GL_TEXTURE4_ARB = 0x84C4;
		public const uint GL_TEXTURE5_ARB = 0x84C5;
		public const uint GL_TEXTURE6_ARB = 0x84C6;
		public const uint GL_TEXTURE7_ARB = 0x84C7;
		public const uint GL_TEXTURE8_ARB = 0x84C8;
		public const uint GL_TEXTURE9_ARB = 0x84C9;
		public const uint GL_TEXTURE10_ARB = 0x84CA;
		public const uint GL_TEXTURE11_ARB = 0x84CB;
		public const uint GL_TEXTURE12_ARB = 0x84CC;
		public const uint GL_TEXTURE13_ARB = 0x84CD;
		public const uint GL_TEXTURE14_ARB = 0x84CE;
		public const uint GL_TEXTURE15_ARB = 0x84CF;
		public const uint GL_TEXTURE16_ARB = 0x84D0;
		public const uint GL_TEXTURE17_ARB = 0x84D1;
		public const uint GL_TEXTURE18_ARB = 0x84D2;
		public const uint GL_TEXTURE19_ARB = 0x84D3;
		public const uint GL_TEXTURE20_ARB = 0x84D4;
		public const uint GL_TEXTURE21_ARB = 0x84D5;
		public const uint GL_TEXTURE22_ARB = 0x84D6;
		public const uint GL_TEXTURE23_ARB = 0x84D7;
		public const uint GL_TEXTURE24_ARB = 0x84D8;
		public const uint GL_TEXTURE25_ARB = 0x84D9;
		public const uint GL_TEXTURE26_ARB = 0x84DA;
		public const uint GL_TEXTURE27_ARB = 0x84DB;
		public const uint GL_TEXTURE28_ARB = 0x84DC;
		public const uint GL_TEXTURE29_ARB = 0x84DD;
		public const uint GL_TEXTURE30_ARB = 0x84DE;
		public const uint GL_TEXTURE31_ARB = 0x84DF;
		public const uint GL_ACTIVE_TEXTURE_ARB = 0x84E0;
		public const uint GL_CLIENT_ACTIVE_TEXTURE_ARB = 0x84E1;
		public const uint GL_MAX_TEXTURE_UNITS_ARB = 0x84E2;
		public const uint GL_ARB_transpose_matrix = 1;
		public const uint GL_TRANSPOSE_MODELVIEW_MATRIX_ARB = 0x84E3;
		public const uint GL_TRANSPOSE_PROJECTION_MATRIX_ARB = 0x84E4;
		public const uint GL_TRANSPOSE_TEXTURE_MATRIX_ARB = 0x84E5;
		public const uint GL_TRANSPOSE_COLOR_MATRIX_ARB = 0x84E6;
		public const uint GL_ARB_texture_compression = 1;
		public const uint GL_COMPRESSED_ALPHA_ARB = 0x84E9;
		public const uint GL_COMPRESSED_LUMINANCE_ARB = 0x84EA;
		public const uint GL_COMPRESSED_LUMINANCE_ALPHA_ARB = 0x84EB;
		public const uint GL_COMPRESSED_INTENSITY_ARB = 0x84EC;
		public const uint GL_COMPRESSED_RGB_ARB = 0x84ED;
		public const uint GL_COMPRESSED_RGBA_ARB = 0x84EE;
		public const uint GL_TEXTURE_COMPRESSION_HINT_ARB = 0x84EF;
		public const uint GL_TEXTURE_IMAGE_SIZE_ARB = 0x86A0;
		public const uint GL_TEXTURE_COMPRESSED_ARB = 0x86A1;
		public const uint GL_NUM_COMPRESSED_TEXTURE_FORMATS_ARB = 0x86A2;
		public const uint GL_COMPRESSED_TEXTURE_FORMATS_ARB = 0x86A3;
		public const uint GL_ARB_texture_cube_map = 1;
		public const uint GL_NORMAL_MAP_ARB = 0x8511;
		public const uint GL_REFLECTION_MAP_ARB = 0x8512;
		public const uint GL_TEXTURE_CUBE_MAP_ARB = 0x8513;
		public const uint GL_TEXTURE_BINDING_CUBE_MAP_ARB = 0x8514;
		public const uint GL_TEXTURE_CUBE_MAP_POSITIVE_X_ARB = 0x8515;
		public const uint GL_TEXTURE_CUBE_MAP_NEGATIVE_X_ARB = 0x8516;
		public const uint GL_TEXTURE_CUBE_MAP_POSITIVE_Y_ARB = 0x8517;
		public const uint GL_TEXTURE_CUBE_MAP_NEGATIVE_Y_ARB = 0x8518;
		public const uint GL_TEXTURE_CUBE_MAP_POSITIVE_Z_ARB = 0x8519;
		public const uint GL_TEXTURE_CUBE_MAP_NEGATIVE_Z_ARB = 0x851A;
		public const uint GL_PROXY_TEXTURE_CUBE_MAP_ARB = 0x851B;
		public const uint GL_MAX_CUBE_MAP_TEXTURE_SIZE_ARB = 0x851C;
		public const uint GL_SGIX_shadow = 1;
		public const uint GL_TEXTURE_COMPARE_SGIX = 0x819A;
		public const uint GL_TEXTURE_COMPARE_OPERATOR_SGIX = 0x819B;
		public const uint GL_TEXTURE_LEQUAL_R_SGIX = 0x819C;
		public const uint GL_TEXTURE_GEQUAL_R_SGIX = 0x819D;
		public const uint GL_SGIX_depth_texture = 1;
		public const uint GL_DEPTH_COMPONENT16_SGIX = 0x81A5;
		public const uint GL_DEPTH_COMPONENT24_SGIX = 0x81A6;
		public const uint GL_DEPTH_COMPONENT32_SGIX = 0x81A7;
		public const uint GL_EXT_compiled_vertex_array = 1;
		public const uint GL_ARRAY_ELEMENT_LOCK_FIRST_EXT = 0x81A8;
		public const uint GL_ARRAY_ELEMENT_LOCK_COUNT_EXT = 0x81A9;
		public const uint GL_ARB_texture_env_combine = 1;
		public const uint GL_COMBINE_ARB = 0x8570;
		public const uint GL_COMBINE_RGB_ARB = 0x8571;
		public const uint GL_COMBINE_ALPHA_ARB = 0x8572;
		public const uint GL_RGB_SCALE_ARB = 0x8573;
		public const uint GL_ADD_SIGNED_ARB = 0x8574;
		public const uint GL_INTERPOLATE_ARB = 0x8575;
		public const uint GL_CONSTANT_ARB = 0x8576;
		public const uint GL_PRIMARY_COLOR_ARB = 0x8577;
		public const uint GL_PREVIOUS_ARB = 0x8578;
		public const uint GL_SOURCE0_RGB_ARB = 0x8580;
		public const uint GL_SOURCE1_RGB_ARB = 0x8581;
		public const uint GL_SOURCE2_RGB_ARB = 0x8582;
		public const uint GL_SOURCE0_ALPHA_ARB = 0x8588;
		public const uint GL_SOURCE1_ALPHA_ARB = 0x8589;
		public const uint GL_SOURCE2_ALPHA_ARB = 0x858A;
		public const uint GL_OPERAND0_RGB_ARB = 0x8590;
		public const uint GL_OPERAND1_RGB_ARB = 0x8591;
		public const uint GL_OPERAND2_RGB_ARB = 0x8592;
		public const uint GL_OPERAND0_ALPHA_ARB = 0x8598;
		public const uint GL_OPERAND1_ALPHA_ARB = 0x8599;
		public const uint GL_OPERAND2_ALPHA_ARB = 0x859A;
		public const uint GL_ARB_texture_env_dot3 = 1;
		public const uint GL_DOT3_RGB_ARB = 0x86AE;
		public const uint GL_DOT3_RGBA_ARB = 0x86AF;
		public const uint GL_ARB_texture_border_clamp = 1;
		public const uint GL_CLAMP_TO_BORDER_ARB = 0x812D;
		public const uint GL_ARB_texture_env_add = 1;
		public const uint GL_EXT_secondary_color = 1;
		public const uint GL_COLOR_SUM_EXT = 0x8458;
		public const uint GL_CURRENT_SECONDARY_COLOR_EXT = 0x8459;
		public const uint GL_SECONDARY_COLOR_ARRAY_SIZE_EXT = 0x845A;
		public const uint GL_SECONDARY_COLOR_ARRAY_TYPE_EXT = 0x845B;
		public const uint GL_SECONDARY_COLOR_ARRAY_STRIDE_EXT = 0x845C;
		public const uint GL_SECONDARY_COLOR_ARRAY_POINTER_EXT = 0x845D;
		public const uint GL_SECONDARY_COLOR_ARRAY_EXT = 0x845E;
		public const uint GL_EXT_fog_coord = 1;
		public const uint GL_FOG_COORDINATE_SOURCE_EXT = 0x8450;
		public const uint GL_FOG_COORDINATE_EXT = 0x8451;
		public const uint GL_FRAGMENT_DEPTH_EXT = 0x8452;
		public const uint GL_CURRENT_FOG_COORDINATE_EXT = 0x8453;
		public const uint GL_FOG_COORDINATE_ARRAY_TYPE_EXT = 0x8454;
		public const uint GL_FOG_COORDINATE_ARRAY_STRIDE_EXT = 0x8455;
		public const uint GL_FOG_COORDINATE_ARRAY_POINTER_EXT = 0x8456;
		public const uint GL_FOG_COORDINATE_ARRAY_EXT = 0x8457;
		public const uint GL_NV_vertex_array_range = 1;
		public const uint GL_VERTEX_ARRAY_RANGE_NV = 0x851D;
		public const uint GL_VERTEX_ARRAY_RANGE_LENGTH_NV = 0x851E;
		public const uint GL_VERTEX_ARRAY_RANGE_VALID_NV = 0x851F;
		public const uint GL_MAX_VERTEX_ARRAY_RANGE_ELEMENT_NV = 0x8520;
		public const uint GL_VERTEX_ARRAY_RANGE_POINTER_NV = 0x8521;
		public const uint GL_NV_vertex_array_range2 = 1;
		public const uint GL_VERTEX_ARRAY_RANGE_WITHOUT_FLUSH_NV = 0x8533;
		public const uint GL_EXT_point_parameters = 1;
		public const uint GL_POINT_SIZE_MIN_EXT = 0x8126;
		public const uint GL_POINT_SIZE_MAX_EXT = 0x8127;
		public const uint GL_POINT_FADE_THRESHOLD_SIZE_EXT = 0x8128;
		public const uint GL_DISTANCE_ATTENUATION_EXT = 0x8129;
		public const uint GL_NV_register_combiners = 1;
		public const uint GL_REGISTER_COMBINERS_NV = 0x8522;
		public const uint GL_COMBINER0_NV = 0x8550;
		public const uint GL_COMBINER1_NV = 0x8551;
		public const uint GL_COMBINER2_NV = 0x8552;
		public const uint GL_COMBINER3_NV = 0x8553;
		public const uint GL_COMBINER4_NV = 0x8554;
		public const uint GL_COMBINER5_NV = 0x8555;
		public const uint GL_COMBINER6_NV = 0x8556;
		public const uint GL_COMBINER7_NV = 0x8557;
		public const uint GL_VARIABLE_A_NV = 0x8523;
		public const uint GL_VARIABLE_B_NV = 0x8524;
		public const uint GL_VARIABLE_C_NV = 0x8525;
		public const uint GL_VARIABLE_D_NV = 0x8526;
		public const uint GL_VARIABLE_E_NV = 0x8527;
		public const uint GL_VARIABLE_F_NV = 0x8528;
		public const uint GL_VARIABLE_G_NV = 0x8529;
		public const uint GL_CONSTANT_COLOR0_NV = 0x852A;
		public const uint GL_CONSTANT_COLOR1_NV = 0x852B;
		public const uint GL_PRIMARY_COLOR_NV = 0x852C;
		public const uint GL_SECONDARY_COLOR_NV = 0x852D;
		public const uint GL_SPARE0_NV = 0x852E;
		public const uint GL_SPARE1_NV = 0x852F;
		public const uint GL_UNSIGNED_IDENTITY_NV = 0x8536;
		public const uint GL_UNSIGNED_INVERT_NV = 0x8537;
		public const uint GL_EXPAND_NORMAL_NV = 0x8538;
		public const uint GL_EXPAND_NEGATE_NV = 0x8539;
		public const uint GL_HALF_BIAS_NORMAL_NV = 0x853A;
		public const uint GL_HALF_BIAS_NEGATE_NV = 0x853B;
		public const uint GL_SIGNED_IDENTITY_NV = 0x853C;
		public const uint GL_SIGNED_NEGATE_NV = 0x853D;
		public const uint GL_E_TIMES_F_NV = 0x8531;
		public const uint GL_SPARE0_PLUS_SECONDARY_COLOR_NV = 0x8532;
		public const uint GL_SCALE_BY_TWO_NV = 0x853E;
		public const uint GL_SCALE_BY_FOUR_NV = 0x853F;
		public const uint GL_SCALE_BY_ONE_HALF_NV = 0x8540;
		public const uint GL_BIAS_BY_NEGATIVE_ONE_HALF_NV = 0x8541;
		public const uint GL_DISCARD_NV = 0x8530;
		public const uint GL_COMBINER_INPUT_NV = 0x8542;
		public const uint GL_COMBINER_MAPPING_NV = 0x8543;
		public const uint GL_COMBINER_COMPONENT_USAGE_NV = 0x8544;
		public const uint GL_COMBINER_AB_DOT_PRODUCT_NV = 0x8545;
		public const uint GL_COMBINER_CD_DOT_PRODUCT_NV = 0x8546;
		public const uint GL_COMBINER_MUX_SUM_NV = 0x8547;
		public const uint GL_COMBINER_SCALE_NV = 0x8548;
		public const uint GL_COMBINER_BIAS_NV = 0x8549;
		public const uint GL_COMBINER_AB_OUTPUT_NV = 0x854A;
		public const uint GL_COMBINER_CD_OUTPUT_NV = 0x854B;
		public const uint GL_COMBINER_SUM_OUTPUT_NV = 0x854C;
		public const uint GL_NUM_GENERAL_COMBINERS_NV = 0x854E;
		public const uint GL_COLOR_SUM_CLAMP_NV = 0x854F;
		public const uint GL_MAX_GENERAL_COMBINERS_NV = 0x854D;
		public const uint GL_ARB_multisample = 1;
		public const uint GL_MULTISAMPLE_ARB = 0x809D;
		public const uint GL_SAMPLE_ALPHA_TO_COVERAGE_ARB = 0x809E;
		public const uint GL_SAMPLE_ALPHA_TO_ONE_ARB = 0x809F;
		public const uint GL_SAMPLE_COVERAGE_ARB = 0x80A0;
		public const uint GL_SAMPLE_BUFFERS_ARB = 0x80A8;
		public const uint GL_SAMPLES_ARB = 0x80A9;
		public const uint GL_SAMPLE_COVERAGE_VALUE_ARB = 0x80AA;
		public const uint GL_SAMPLE_COVERAGE_INVERT_ARB = 0x80AB;
		public const uint GL_MULTISAMPLE_BIT_ARB = 0x20000000;
		public const uint GL_NV_texture_material = 1;
		public const uint GL_TEXTURE_SHADER_NV = 0x86DE;
		public const uint GL_RGBA_UNSIGNED_DOT_PRODUCT_MAPPING_NV = 0x86D9;
		public const uint GL_SHADER_OPERATION_NV = 0x86DF;
		public const uint GL_CULL_MODES_NV = 0x86E0;
		public const uint GL_OFFSET_TEXTURE_MATRIX_NV = 0x86E1;
		public const uint GL_OFFSET_TEXTURE_SCALE_NV = 0x86E2;
		public const uint GL_OFFSET_TEXTURE_BIAS_NV = 0x86E3;
		public const uint GL_PREVIOUS_TEXTURE_INPUT_NV = 0x86E4;
		public const uint GL_CONST_EYE_NV = 0x86E5;
		public const uint GL_SHADER_CONSISTENT_NV = 0x86DD;
		public const uint GL_PASS_THROUGH_NV = 0x86E6;
		public const uint GL_CULL_FRAGMENT_NV = 0x86E7;
		public const uint GL_OFFSET_TEXTURE_2D_NV = 0x86E8;
		public const uint GL_OFFSET_TEXTURE_RECTANGLE_NV = 0x864C;
		public const uint GL_OFFSET_TEXTURE_RECTANGLE_SCALE_NV = 0x864D;
		public const uint GL_DEPENDENT_AR_TEXTURE_2D_NV = 0x86E9;
		public const uint GL_DEPENDENT_GB_TEXTURE_2D_NV = 0x86EA;
		public const uint GL_DOT_PRODUCT_NV = 0x86EC;
		public const uint GL_DOT_PRODUCT_DEPTH_REPLACE_NV = 0x86ED;
		public const uint GL_DOT_PRODUCT_TEXTURE_2D_NV = 0x86EE;
		public const uint GL_DOT_PRODUCT_TEXTURE_RECTANGLE_NV = 0x864E;
		public const uint GL_DOT_PRODUCT_TEXTURE_CUBE_MAP_NV = 0x86F0;
		public const uint GL_DOT_PRODUCT_DIFFUSE_CUBE_MAP_NV = 0x86F1;
		public const uint GL_DOT_PRODUCT_REFLECT_CUBE_MAP_NV = 0x86F2;
		public const uint GL_DOT_PRODUCT_CONST_EYE_REFLECT_CUBE_MAP_NV = 0x86F3;
		public const uint GL_HILO_NV = 0x86F4;
		public const uint GL_DSDT_NV = 0x86F5;
		public const uint GL_DSDT_MAG_NV = 0x86F6;
		public const uint GL_DSDT_MAG_VIB_NV = 0x86F7;
		public const uint GL_UNSIGNED_INT_S8_S8_8_8_NV = 0x86DA;
		public const uint GL_UNSIGNED_INT_8_8_S8_S8_REV_NV = 0x86DB;
		public const uint GL_SIGNED_RGBA_NV = 0x86FB;
		public const uint GL_SIGNED_RGBA8_NV = 0x86FC;
		public const uint GL_SIGNED_RGB_NV = 0x86FE;
		public const uint GL_SIGNED_RGB8_NV = 0x86FF;
		public const uint GL_SIGNED_LUMINANCE_NV = 0x8701;
		public const uint GL_SIGNED_LUMINANCE8_NV = 0x8702;
		public const uint GL_SIGNED_LUMINANCE_ALPHA_NV = 0x8703;
		public const uint GL_SIGNED_LUMINANCE8_ALPHA8_NV = 0x8704;
		public const uint GL_SIGNED_ALPHA_NV = 0x8705;
		public const uint GL_SIGNED_ALPHA8_NV = 0x8706;
		public const uint GL_SIGNED_INTENSITY_NV = 0x8707;
		public const uint GL_SIGNED_INTENSITY8_NV = 0x8708;
		public const uint GL_SIGNED_RGB_UNSIGNED_ALPHA_NV = 0x870C;
		public const uint GL_SIGNED_RGB8_UNSIGNED_ALPHA8_NV = 0x870D;
		public const uint GL_HILO16_NV = 0x86F8;
		public const uint GL_SIGNED_HILO_NV = 0x86F9;
		public const uint GL_SIGNED_HILO16_NV = 0x86FA;
		public const uint GL_DSDT8_NV = 0x8709;
		public const uint GL_DSDT8_MAG8_NV = 0x870A;
		public const uint GL_DSDT_MAG_INTENSITY_NV = 0x86DC;
		public const uint GL_DSDT8_MAG8_INTENSITY8_NV = 0x870B;
		public const uint GL_HI_SCALE_NV = 0x870E;
		public const uint GL_LO_SCALE_NV = 0x870F;
		public const uint GL_DS_SCALE_NV = 0x8710;
		public const uint GL_DT_SCALE_NV = 0x8711;
		public const uint GL_MAGNITUDE_SCALE_NV = 0x8712;
		public const uint GL_VIBRANCE_SCALE_NV = 0x8713;
		public const uint GL_HI_BIAS_NV = 0x8714;
		public const uint GL_LO_BIAS_NV = 0x8715;
		public const uint GL_DS_BIAS_NV = 0x8716;
		public const uint GL_DT_BIAS_NV = 0x8717;
		public const uint GL_MAGNITUDE_BIAS_NV = 0x8718;
		public const uint GL_VIBRANCE_BIAS_NV = 0x8719;
		public const uint GL_TEXTURE_BORDER_VALUES_NV = 0x871A;
		public const uint GL_TEXTURE_HI_SIZE_NV = 0x871B;
		public const uint GL_TEXTURE_LO_SIZE_NV = 0x871C;
		public const uint GL_TEXTURE_DS_SIZE_NV = 0x871D;
		public const uint GL_TEXTURE_DT_SIZE_NV = 0x871E;
		public const uint GL_TEXTURE_MAG_SIZE_NV = 0x871F;
		public const uint GL_NV_texture_rectangle = 1;
		public const uint GL_TEXTURE_RECTANGLE_NV = 0x84F5;
		public const uint GL_TEXTURE_BINDING_RECTANGLE_NV = 0x84F6;
		public const uint GL_PROXY_TEXTURE_RECTANGLE_NV = 0x84F7;
		public const uint GL_MAX_RECTANGLE_TEXTURE_SIZE_NV = 0x84F8;
		public const uint GL_NV_texture_env_combine4 = 1;
		public const uint GL_COMBINE4_NV = 0x8503;
		public const uint GL_SOURCE3_RGB_NV = 0x8583;
		public const uint GL_SOURCE3_ALPHA_NV = 0x858B;
		public const uint GL_OPERAND3_RGB_NV = 0x8593;
		public const uint GL_OPERAND3_ALPHA_NV = 0x859B;
		public const uint GL_NV_fog_distance = 1;
		public const uint GL_FOG_DISTANCE_MODE_NV = 0x855A;
		public const uint GL_EYE_RADIAL_NV = 0x855B;
		public const uint GL_EYE_PLANE_ABSOLUTE_NV = 0x855C;
		public const uint GL_EXT_texture_filter_anisotropic = 1;
		public const uint GL_TEXTURE_MAX_ANISOTROPY_EXT = 0x84FE;
		public const uint GL_MAX_TEXTURE_MAX_ANISOTROPY_EXT = 0x84FF;
		public const uint GL_SGIS_generate_mipmap = 1;
		public const uint GL_GENERATE_MIPMAP_SGIS = 0x8191;
		public const uint GL_GENERATE_MIPMAP_HINT_SGIS = 0x8192;
		public const uint GL_NV_texgen_reflection = 1;
		public const uint GL_NORMAL_MAP_NV = 0x8511;
		public const uint GL_REFLECTION_MAP_NV = 0x8512;
		public const uint GL_EXT_vertex_weighting = 1;
		public const uint GL_MODELVIEW0_STACK_DEPTH_EXT = 0x0BA3;
		public const uint GL_MODELVIEW1_STACK_DEPTH_EXT = 0x8502;
		public const uint GL_MODELVIEW0_MATRIX_EXT = 0x0BA6;
		public const uint GL_MODELVIEW1_MATRIX_EXT = 0x8506;
		public const uint GL_VERTEX_WEIGHTING_EXT = 0x8509;
		public const uint GL_MODELVIEW0_EXT = 0x1700;
		public const uint GL_MODELVIEW1_EXT = 0x850A;
		public const uint GL_CURRENT_VERTEX_WEIGHT_EXT = 0x850B;
		public const uint GL_VERTEX_WEIGHT_ARRAY_EXT = 0x850C;
		public const uint GL_VERTEX_WEIGHT_ARRAY_SIZE_EXT = 0x850D;
		public const uint GL_VERTEX_WEIGHT_ARRAY_TYPE_EXT = 0x850E;
		public const uint GL_VERTEX_WEIGHT_ARRAY_STRIDE_EXT = 0x850F;
		public const uint GL_VERTEX_WEIGHT_ARRAY_POINTER_EXT = 0x8510;
		public const uint GL_NV_vertex_program = 1;
		public const uint GL_VERTEX_PROGRAM_NV = 0x8620;
		public const uint GL_VERTEX_PROGRAM_POINT_SIZE_NV = 0x8642;
		public const uint GL_VERTEX_PROGRAM_TWO_SIDE_NV = 0x8643;
		public const uint GL_VERTEX_STATE_PROGRAM_NV = 0x8621;
		public const uint GL_ATTRIB_ARRAY_SIZE_NV = 0x8623;
		public const uint GL_ATTRIB_ARRAY_STRIDE_NV = 0x8624;
		public const uint GL_ATTRIB_ARRAY_TYPE_NV = 0x8625;
		public const uint GL_CURRENT_ATTRIB_NV = 0x8626;
		public const uint GL_PROGRAM_PARAMETER_NV = 0x8644;
		public const uint GL_ATTRIB_ARRAY_POINTER_NV = 0x8645;
		public const uint GL_PROGRAM_TARGET_NV = 0x8646;
		public const uint GL_PROGRAM_LENGTH_NV = 0x8627;
		public const uint GL_PROGRAM_RESIDENT_NV = 0x8647;
		public const uint GL_PROGRAM_STRING_NV = 0x8628;
		public const uint GL_TRACK_MATRIX_NV = 0x8648;
		public const uint GL_TRACK_MATRIX_TRANSFORM_NV = 0x8649;
		public const uint GL_MAX_TRACK_MATRIX_STACK_DEPTH_NV = 0x862E;
		public const uint GL_MAX_TRACK_MATRICES_NV = 0x862F;
		public const uint GL_CURRENT_MATRIX_STACK_DEPTH_NV = 0x8640;
		public const uint GL_CURRENT_MATRIX_NV = 0x8641;
		public const uint GL_VERTEX_PROGRAM_BINDING_NV = 0x864A;
		public const uint GL_PROGRAM_ERROR_POSITION_NV = 0x864B;
		public const uint GL_MODELVIEW_PROJECTION_NV = 0x8629;
		public const uint GL_MATRIX0_NV = 0x8630;
		public const uint GL_MATRIX1_NV = 0x8631;
		public const uint GL_MATRIX2_NV = 0x8632;
		public const uint GL_MATRIX3_NV = 0x8633;
		public const uint GL_MATRIX4_NV = 0x8634;
		public const uint GL_MATRIX5_NV = 0x8635;
		public const uint GL_MATRIX6_NV = 0x8636;
		public const uint GL_MATRIX7_NV = 0x8637;
		public const uint GL_IDENTITY_NV = 0x862A;
		public const uint GL_INVERSE_NV = 0x862B;
		public const uint GL_TRANSPOSE_NV = 0x862C;
		public const uint GL_INVERSE_TRANSPOSE_NV = 0x862D;
		public const uint GL_VERTEX_ATTRIB_ARRAY0_NV = 0x8650;
		public const uint GL_VERTEX_ATTRIB_ARRAY1_NV = 0x8651;
		public const uint GL_VERTEX_ATTRIB_ARRAY2_NV = 0x8652;
		public const uint GL_VERTEX_ATTRIB_ARRAY3_NV = 0x8653;
		public const uint GL_VERTEX_ATTRIB_ARRAY4_NV = 0x8654;
		public const uint GL_VERTEX_ATTRIB_ARRAY5_NV = 0x8655;
		public const uint GL_VERTEX_ATTRIB_ARRAY6_NV = 0x8656;
		public const uint GL_VERTEX_ATTRIB_ARRAY7_NV = 0x8657;
		public const uint GL_VERTEX_ATTRIB_ARRAY8_NV = 0x8658;
		public const uint GL_VERTEX_ATTRIB_ARRAY9_NV = 0x8659;
		public const uint GL_VERTEX_ATTRIB_ARRAY10_NV = 0x865A;
		public const uint GL_VERTEX_ATTRIB_ARRAY11_NV = 0x865B;
		public const uint GL_VERTEX_ATTRIB_ARRAY12_NV = 0x865C;
		public const uint GL_VERTEX_ATTRIB_ARRAY13_NV = 0x865D;
		public const uint GL_VERTEX_ATTRIB_ARRAY14_NV = 0x865E;
		public const uint GL_VERTEX_ATTRIB_ARRAY15_NV = 0x865F;
		public const uint GL_MAP1_VERTEX_ATTRIB0_4_NV = 0x8660;
		public const uint GL_MAP1_VERTEX_ATTRIB1_4_NV = 0x8661;
		public const uint GL_MAP1_VERTEX_ATTRIB2_4_NV = 0x8662;
		public const uint GL_MAP1_VERTEX_ATTRIB3_4_NV = 0x8663;
		public const uint GL_MAP1_VERTEX_ATTRIB4_4_NV = 0x8664;
		public const uint GL_MAP1_VERTEX_ATTRIB5_4_NV = 0x8665;
		public const uint GL_MAP1_VERTEX_ATTRIB6_4_NV = 0x8666;
		public const uint GL_MAP1_VERTEX_ATTRIB7_4_NV = 0x8667;
		public const uint GL_MAP1_VERTEX_ATTRIB8_4_NV = 0x8668;
		public const uint GL_MAP1_VERTEX_ATTRIB9_4_NV = 0x8669;
		public const uint GL_MAP1_VERTEX_ATTRIB10_4_NV = 0x866A;
		public const uint GL_MAP1_VERTEX_ATTRIB11_4_NV = 0x866B;
		public const uint GL_MAP1_VERTEX_ATTRIB12_4_NV = 0x866C;
		public const uint GL_MAP1_VERTEX_ATTRIB13_4_NV = 0x866D;
		public const uint GL_MAP1_VERTEX_ATTRIB14_4_NV = 0x866E;
		public const uint GL_MAP1_VERTEX_ATTRIB15_4_NV = 0x866F;
		public const uint GL_MAP2_VERTEX_ATTRIB0_4_NV = 0x8670;
		public const uint GL_MAP2_VERTEX_ATTRIB1_4_NV = 0x8671;
		public const uint GL_MAP2_VERTEX_ATTRIB2_4_NV = 0x8672;
		public const uint GL_MAP2_VERTEX_ATTRIB3_4_NV = 0x8673;
		public const uint GL_MAP2_VERTEX_ATTRIB4_4_NV = 0x8674;
		public const uint GL_MAP2_VERTEX_ATTRIB5_4_NV = 0x8675;
		public const uint GL_MAP2_VERTEX_ATTRIB6_4_NV = 0x8676;
		public const uint GL_MAP2_VERTEX_ATTRIB7_4_NV = 0x8677;
		public const uint GL_MAP2_VERTEX_ATTRIB8_4_NV = 0x8678;
		public const uint GL_MAP2_VERTEX_ATTRIB9_4_NV = 0x8679;
		public const uint GL_MAP2_VERTEX_ATTRIB10_4_NV = 0x867A;
		public const uint GL_MAP2_VERTEX_ATTRIB11_4_NV = 0x867B;
		public const uint GL_MAP2_VERTEX_ATTRIB12_4_NV = 0x867C;
		public const uint GL_MAP2_VERTEX_ATTRIB13_4_NV = 0x867D;
		public const uint GL_MAP2_VERTEX_ATTRIB14_4_NV = 0x867E;
		public const uint GL_MAP2_VERTEX_ATTRIB15_4_NV = 0x867F;
		public const uint GL_NV_fance = 1;
		public const uint GL_ALL_COMPLETED_NV = 0x84F2;
		public const uint GL_FENCE_STATUS_NV = 0x84F3;
		public const uint GL_FENCE_CONDITION_NV = 0x84F4;
		public const uint GL_DOT_PRODUCT_TEXTURE_3D_NV = 0x86EF;
		public const uint GL_NV_blend_square = 1;
		public const uint GL_NV_light_max_exponent = 1;
		public const uint GL_MAX_SHININESS_NV = 0x8504;
		public const uint GL_MAX_SPOT_EXPONENT_NV = 0x8505;
		public const uint GL_NV_packed_depth_stencil = 1;
		public const uint GL_DEPTH_STENCIL_NV = 0x84F9;
		public const uint GL_UNSIGNED_INT_24_8_NV = 0x84FA;
		public const uint GL_PER_STAGE_CONSTANTS_NV = 0x8535;
		public const uint GL_EXT_abgr = 1;
		public const uint GL_ABGR_EXT = 0x8000;
		public const uint GL_EXT_stencil_wrap = 1;
		public const uint GL_INCR_WRAP_EXT = 0x8507;
		public const uint GL_DECR_WRAP_EXT = 0x8508;
		public const uint GL_EXT_texture_lod_bias = 1;
		public const uint GL_TEXTURE_FILTER_CONTROL_EXT = 0x8500;
		public const uint GL_TEXTURE_LOD_BIAS_EXT = 0x8501;
		public const uint GL_MAX_TEXTURE_LOD_BIAS_EXT = 0x84FD;
		public const uint GL_NV_evaluators = 1;
		public const uint GL_EVAL_2D_NV = 0x86C0;
		public const uint GL_EVAL_TRIANGULAR_2D_NV = 0x86C1;
		public const uint GL_MAP_TESSELLATION_NV = 0x86C2;
		public const uint GL_MAP_ATTRIB_U_ORDER_NV = 0x86C3;
		public const uint GL_MAP_ATTRIB_V_ORDER_NV = 0x86C4;
		public const uint GL_EVAL_FRACTIONAL_TESSELLATION_NV = 0x86C5;
		public const uint GL_EVAL_VERTEX_ATTRIB0_NV = 0x86C6;
		public const uint GL_EVAL_VERTEX_ATTRIB1_NV = 0x86C7;
		public const uint GL_EVAL_VERTEX_ATTRIB2_NV = 0x86C8;
		public const uint GL_EVAL_VERTEX_ATTRIB3_NV = 0x86C9;
		public const uint GL_EVAL_VERTEX_ATTRIB4_NV = 0x86CA;
		public const uint GL_EVAL_VERTEX_ATTRIB5_NV = 0x86CB;
		public const uint GL_EVAL_VERTEX_ATTRIB6_NV = 0x86CC;
		public const uint GL_EVAL_VERTEX_ATTRIB7_NV = 0x86CD;
		public const uint GL_EVAL_VERTEX_ATTRIB8_NV = 0x86CE;
		public const uint GL_EVAL_VERTEX_ATTRIB9_NV = 0x86CF;
		public const uint GL_EVAL_VERTEX_ATTRIB10_NV = 0x86D0;
		public const uint GL_EVAL_VERTEX_ATTRIB11_NV = 0x86D1;
		public const uint GL_EVAL_VERTEX_ATTRIB12_NV = 0x86D2;
		public const uint GL_EVAL_VERTEX_ATTRIB13_NV = 0x86D3;
		public const uint GL_EVAL_VERTEX_ATTRIB14_NV = 0x86D4;
		public const uint GL_EVAL_VERTEX_ATTRIB15_NV = 0x86D5;
		public const uint GL_MAX_MAP_TESSELLATION_NV = 0x86D6;
		public const uint GL_MAX_RATIONAL_EVAL_ORDER_NV = 0x86D7;
		public const uint GL_NV_copy_depth_to_color = 1;
		public const uint GL_DEPTH_STENCIL_TO_RGBA_NV = 0x886E;
		public const uint GL_DEPTH_STENCIL_TO_BGRA_NV = 0x886F;
		public const uint GL_ATI_pn_triangles = 1;
		public const uint GL_PN_TRIANGLES_ATI = 0x87F0;
		public const uint GL_MAX_PN_TRIANGLES_TESSELATION_LEVEL_ATI = 0x87F1;
		public const uint GL_PN_TRIANGLES_POINT_MODE_ATI = 0x87F2;
		public const uint GL_PN_TRIANGLES_NORMAL_MODE_ATI = 0x87F3;
		public const uint GL_PN_TRIANGLES_TESSELATION_LEVEL_ATI = 0x87F4;
		public const uint GL_PN_TRIANGLES_POINT_MODE_LINEAR_ATI = 0x87F5;
		public const uint GL_PN_TRIANGLES_POINT_MODE_CUBIC_ATI = 0x87F6;
		public const uint GL_PN_TRIANGLES_NORMAL_MODE_LINEAR_ATI = 0x87F7;
		public const uint GL_PN_TRIANGLES_NORMAL_MODE_QUADRATIC_ATI = 0x87F8;
		public const uint GL_ARB_point_parameters = 1;
		public const uint GL_POINT_SIZE_MIN_ARB = 0x8126;
		public const uint GL_POINT_SIZE_MAX_ARB = 0x8127;
		public const uint GL_POINT_FADE_THRESHOLD_SIZE_ARB = 0x8128;
		public const uint GL_POINT_DISTANCE_ATTENUATION_ARB = 0x8129;
		public const uint GL_ARB_texture_env_crossbar = 1;
		public const uint GL_ARB_vertex_blend = 1;
		public const uint GL_MAX_VERTEX_UNITS_ARB = 0x86A4;
		public const uint GL_ACTIVE_VERTEX_UNITS_ARB = 0x86A5;
		public const uint GL_WEIGHT_SUM_UNITY_ARB = 0x86A6;
		public const uint GL_VERTEX_BLEND_ARB = 0x86A7;
		public const uint GL_CURRENT_WEIGHT_ARB = 0x86A8;
		public const uint GL_WEIGHT_ARRAY_TYPE_ARB = 0x86A9;
		public const uint GL_WEIGHT_ARRAY_STRIDE_ARB = 0x86AA;
		public const uint GL_WEIGHT_ARRAY_SIZE_ARB = 0x86AB;
		public const uint GL_WEIGHT_ARRAY_POINTER_ARB = 0x86AC;
		public const uint GL_WEIGHT_ARRAY_ARB = 0x86AD;
		public const uint GL_MODELVIEW0_ARB = 0x1700;
		public const uint GL_MODELVIEW1_ARB = 0x850a;
		public const uint GL_MODELVIEW2_ARB = 0x8722;
		public const uint GL_MODELVIEW3_ARB = 0x8723;
		public const uint GL_MODELVIEW4_ARB = 0x8724;
		public const uint GL_MODELVIEW5_ARB = 0x8725;
		public const uint GL_MODELVIEW6_ARB = 0x8726;
		public const uint GL_MODELVIEW7_ARB = 0x8727;
		public const uint GL_MODELVIEW8_ARB = 0x8728;
		public const uint GL_MODELVIEW9_ARB = 0x8729;
		public const uint GL_MODELVIEW10_ARB = 0x872A;
		public const uint GL_MODELVIEW11_ARB = 0x872B;
		public const uint GL_MODELVIEW12_ARB = 0x872C;
		public const uint GL_MODELVIEW13_ARB = 0x872D;
		public const uint GL_MODELVIEW14_ARB = 0x872E;
		public const uint GL_MODELVIEW15_ARB = 0x872F;
		public const uint GL_MODELVIEW16_ARB = 0x8730;
		public const uint GL_MODELVIEW17_ARB = 0x8731;
		public const uint GL_MODELVIEW18_ARB = 0x8732;
		public const uint GL_MODELVIEW19_ARB = 0x8733;
		public const uint GL_MODELVIEW20_ARB = 0x8734;
		public const uint GL_MODELVIEW21_ARB = 0x8735;
		public const uint GL_MODELVIEW22_ARB = 0x8736;
		public const uint GL_MODELVIEW23_ARB = 0x8737;
		public const uint GL_MODELVIEW24_ARB = 0x8738;
		public const uint GL_MODELVIEW25_ARB = 0x8739;
		public const uint GL_MODELVIEW26_ARB = 0x873A;
		public const uint GL_MODELVIEW27_ARB = 0x873B;
		public const uint GL_MODELVIEW28_ARB = 0x873C;
		public const uint GL_MODELVIEW29_ARB = 0x873D;
		public const uint GL_MODELVIEW30_ARB = 0x873E;
		public const uint GL_MODELVIEW31_ARB = 0x873F;
		public const uint GL_EXT_multi_draw_arrays = 1;
		public const uint GL_ARB_matrix_palette = 1;
		public const uint GL_MATRIX_PALETTE_ARB = 0x8840;
		public const uint GL_MAX_MATRIX_PALETTE_STACK_DEPTH_ARB = 0x8841;
		public const uint GL_MAX_PALETTE_MATRICES_ARB = 0x8842;
		public const uint GL_CURRENT_PALETTE_MATRIX_ARB = 0x8843;
		public const uint GL_MATRIX_INDEX_ARRAY_ARB = 0x8844;
		public const uint GL_CURRENT_MATRIX_INDEX_ARB = 0x8845;
		public const uint GL_MATRIX_INDEX_ARRAY_SIZE_ARB = 0x8846;
		public const uint GL_MATRIX_INDEX_ARRAY_TYPE_ARB = 0x8847;
		public const uint GL_MATRIX_INDEX_ARRAY_STRIDE_ARB = 0x8848;
		public const uint GL_MATRIX_INDEX_ARRAY_POINTER_ARB = 0x8849;
		public const uint GL_EXT_vertex_material = 1;
		public const uint GL_VERTEX_SHADER_EXT = 0x8780;
		public const uint GL_VERTEX_SHADER_BINDING_EXT = 0x8781;
		public const uint GL_OP_INDEX_EXT = 0x8782;
		public const uint GL_OP_NEGATE_EXT = 0x8783;
		public const uint GL_OP_DOT3_EXT = 0x8784;
		public const uint GL_OP_DOT4_EXT = 0x8785;
		public const uint GL_OP_MUL_EXT = 0x8786;
		public const uint GL_OP_ADD_EXT = 0x8787;
		public const uint GL_OP_MADD_EXT = 0x8788;
		public const uint GL_OP_FRAC_EXT = 0x8789;
		public const uint GL_OP_MAX_EXT = 0x878A;
		public const uint GL_OP_MIN_EXT = 0x878B;
		public const uint GL_OP_SET_GE_EXT = 0x878C;
		public const uint GL_OP_SET_LT_EXT = 0x878D;
		public const uint GL_OP_CLAMP_EXT = 0x878E;
		public const uint GL_OP_FLOOR_EXT = 0x878F;
		public const uint GL_OP_ROUND_EXT = 0x8790;
		public const uint GL_OP_EXP_BASE_2_EXT = 0x8791;
		public const uint GL_OP_LOG_BASE_2_EXT = 0x8792;
		public const uint GL_OP_POWER_EXT = 0x8793;
		public const uint GL_OP_RECIP_EXT = 0x8794;
		public const uint GL_OP_RECIP_SQRT_EXT = 0x8795;
		public const uint GL_OP_SUB_EXT = 0x8796;
		public const uint GL_OP_CROSS_PRODUCT_EXT = 0x8797;
		public const uint GL_OP_MULTIPLY_MATRIX_EXT = 0x8798;
		public const uint GL_OP_MOV_EXT = 0x8799;
		public const uint GL_OUTPUT_VERTEX_EXT = 0x879A;
		public const uint GL_OUTPUT_COLOR0_EXT = 0x879B;
		public const uint GL_OUTPUT_COLOR1_EXT = 0x879C;
		public const uint GL_OUTPUT_TEXTURE_COORD0_EXT = 0x879D;
		public const uint GL_OUTPUT_TEXTURE_COORD1_EXT = 0x879E;
		public const uint GL_OUTPUT_TEXTURE_COORD2_EXT = 0x879F;
		public const uint GL_OUTPUT_TEXTURE_COORD3_EXT = 0x87A0;
		public const uint GL_OUTPUT_TEXTURE_COORD4_EXT = 0x87A1;
		public const uint GL_OUTPUT_TEXTURE_COORD5_EXT = 0x87A2;
		public const uint GL_OUTPUT_TEXTURE_COORD6_EXT = 0x87A3;
		public const uint GL_OUTPUT_TEXTURE_COORD7_EXT = 0x87A4;
		public const uint GL_OUTPUT_TEXTURE_COORD8_EXT = 0x87A5;
		public const uint GL_OUTPUT_TEXTURE_COORD9_EXT = 0x87A6;
		public const uint GL_OUTPUT_TEXTURE_COORD10_EXT = 0x87A7;
		public const uint GL_OUTPUT_TEXTURE_COORD11_EXT = 0x87A8;
		public const uint GL_OUTPUT_TEXTURE_COORD12_EXT = 0x87A9;
		public const uint GL_OUTPUT_TEXTURE_COORD13_EXT = 0x87AA;
		public const uint GL_OUTPUT_TEXTURE_COORD14_EXT = 0x87AB;
		public const uint GL_OUTPUT_TEXTURE_COORD15_EXT = 0x87AC;
		public const uint GL_OUTPUT_TEXTURE_COORD16_EXT = 0x87AD;
		public const uint GL_OUTPUT_TEXTURE_COORD17_EXT = 0x87AE;
		public const uint GL_OUTPUT_TEXTURE_COORD18_EXT = 0x87AF;
		public const uint GL_OUTPUT_TEXTURE_COORD19_EXT = 0x87B0;
		public const uint GL_OUTPUT_TEXTURE_COORD20_EXT = 0x87B1;
		public const uint GL_OUTPUT_TEXTURE_COORD21_EXT = 0x87B2;
		public const uint GL_OUTPUT_TEXTURE_COORD22_EXT = 0x87B3;
		public const uint GL_OUTPUT_TEXTURE_COORD23_EXT = 0x87B4;
		public const uint GL_OUTPUT_TEXTURE_COORD24_EXT = 0x87B5;
		public const uint GL_OUTPUT_TEXTURE_COORD25_EXT = 0x87B6;
		public const uint GL_OUTPUT_TEXTURE_COORD26_EXT = 0x87B7;
		public const uint GL_OUTPUT_TEXTURE_COORD27_EXT = 0x87B8;
		public const uint GL_OUTPUT_TEXTURE_COORD28_EXT = 0x87B9;
		public const uint GL_OUTPUT_TEXTURE_COORD29_EXT = 0x87BA;
		public const uint GL_OUTPUT_TEXTURE_COORD30_EXT = 0x87BB;
		public const uint GL_OUTPUT_TEXTURE_COORD31_EXT = 0x87BC;
		public const uint GL_OUTPUT_FOG_EXT = 0x87BD;
		public const uint GL_SCALAR_EXT = 0x87BE;
		public const uint GL_VECTOR_EXT = 0x87BF;
		public const uint GL_MATRIX_EXT = 0x87C0;
		public const uint GL_VARIANT_EXT = 0x87C1;
		public const uint GL_INVARIANT_EXT = 0x87C2;
		public const uint GL_LOCAL_CONSTANT_EXT = 0x87C3;
		public const uint GL_LOCAL_EXT = 0x87C4;
		public const uint GL_MAX_VERTEX_SHADER_INSTRUCTIONS_EXT = 0x87C5;
		public const uint GL_MAX_VERTEX_SHADER_VARIANTS_EXT = 0x87C6;
		public const uint GL_MAX_VERTEX_SHADER_INVARIANTS_EXT = 0x87C7;
		public const uint GL_MAX_VERTEX_SHADER_LOCAL_CONSTANTS_EXT = 0x87C8;
		public const uint GL_MAX_VERTEX_SHADER_LOCALS_EXT = 0x87C9;
		public const uint GL_MAX_OPTIMIZED_VERTEX_SHADER_INSTRUCTIONS_EXT = 0x87CA;
		public const uint GL_MAX_OPTIMIZED_VERTEX_SHADER_VARIANTS_EXT = 0x87CB;
		public const uint GL_MAX_OPTIMIZED_VERTEX_SHADER_INVARIANTS_EXT = 0x87CC;
		public const uint GL_MAX_OPTIMIZED_VERTEX_SHADER_LOCAL_CONSTANTS_EXT = 0x87CD;
		public const uint GL_MAX_OPTIMIZED_VERTEX_SHADER_LOCALS_EXT = 0x87CE;
		public const uint GL_VERTEX_SHADER_INSTRUCTIONS_EXT = 0x87CF;
		public const uint GL_VERTEX_SHADER_VARIANTS_EXT = 0x87D0;
		public const uint GL_VERTEX_SHADER_INVARIANTS_EXT = 0x87D1;
		public const uint GL_VERTEX_SHADER_LOCAL_CONSTANTS_EXT = 0x87D2;
		public const uint GL_VERTEX_SHADER_LOCALS_EXT = 0x87D3;
		public const uint GL_VERTEX_SHADER_OPTIMIZED_EXT = 0x87D4;
		public const uint GL_X_EXT = 0x87D5;
		public const uint GL_Y_EXT = 0x87D6;
		public const uint GL_Z_EXT = 0x87D7;
		public const uint GL_W_EXT = 0x87D8;
		public const uint GL_NEGATIVE_X_EXT = 0x87D9;
		public const uint GL_NEGATIVE_Y_EXT = 0x87DA;
		public const uint GL_NEGATIVE_Z_EXT = 0x87DB;
		public const uint GL_NEGATIVE_W_EXT = 0x87DC;
		public const uint GL_ZERO_EXT = 0x87DD;
		public const uint GL_ONE_EXT = 0x87DE;
		public const uint GL_NEGATIVE_ONE_EXT = 0x87DF;
		public const uint GL_NORMALIZED_RANGE_EXT = 0x87E0;
		public const uint GL_FULL_RANGE_EXT = 0x87E1;
		public const uint GL_CURRENT_VERTEX_EXT = 0x87E2;
		public const uint GL_MVP_MATRIX_EXT = 0x87E3;
		public const uint GL_VARIANT_VALUE_EXT = 0x87E4;
		public const uint GL_VARIANT_DATATYPE_EXT = 0x87E5;
		public const uint GL_VARIANT_ARRAY_STRIDE_EXT = 0x87E6;
		public const uint GL_VARIANT_ARRAY_TYPE_EXT = 0x87E7;
		public const uint GL_VARIANT_ARRAY_EXT = 0x87E8;
		public const uint GL_VARIANT_ARRAY_POINTER_EXT = 0x87E9;
		public const uint GL_INVARIANT_VALUE_EXT = 0x87EA;
		public const uint GL_INVARIANT_DATATYPE_EXT = 0x87EB;
		public const uint GL_LOCAL_CONSTANT_VALUE_EXT = 0x87EC;
		public const uint GL_LOCAL_CONSTANT_DATATYPE_EXT = 0x87ED;
		public const uint GL_ATI_envmap_bumpmap = 1;
		public const uint GL_BUMP_ROT_MATRIX_ATI = 0x8775;
		public const uint GL_BUMP_ROT_MATRIX_SIZE_ATI = 0x8776;
		public const uint GL_BUMP_NUM_TEX_UNITS_ATI = 0x8777;
		public const uint GL_BUMP_TEX_UNITS_ATI = 0x8778;
		public const uint GL_DUDV_ATI = 0x8779;
		public const uint GL_DU8DV8_ATI = 0x877A;
		public const uint GL_BUMP_ENVMAP_ATI = 0x877B;
		public const uint GL_BUMP_TARGET_ATI = 0x877C;
		public const uint GL_ATI_fragment_material = 1;
		public const uint GL_FRAGMENT_SHADER_ATI = 0x8920;
		public const uint GL_REG_0_ATI = 0x8921;
		public const uint GL_REG_1_ATI = 0x8922;
		public const uint GL_REG_2_ATI = 0x8923;
		public const uint GL_REG_3_ATI = 0x8924;
		public const uint GL_REG_4_ATI = 0x8925;
		public const uint GL_REG_5_ATI = 0x8926;
		public const uint GL_REG_6_ATI = 0x8927;
		public const uint GL_REG_7_ATI = 0x8928;
		public const uint GL_REG_8_ATI = 0x8929;
		public const uint GL_REG_9_ATI = 0x892A;
		public const uint GL_REG_10_ATI = 0x892B;
		public const uint GL_REG_11_ATI = 0x892C;
		public const uint GL_REG_12_ATI = 0x892D;
		public const uint GL_REG_13_ATI = 0x892E;
		public const uint GL_REG_14_ATI = 0x892F;
		public const uint GL_REG_15_ATI = 0x8930;
		public const uint GL_REG_16_ATI = 0x8931;
		public const uint GL_REG_17_ATI = 0x8932;
		public const uint GL_REG_18_ATI = 0x8933;
		public const uint GL_REG_19_ATI = 0x8934;
		public const uint GL_REG_20_ATI = 0x8935;
		public const uint GL_REG_21_ATI = 0x8936;
		public const uint GL_REG_22_ATI = 0x8937;
		public const uint GL_REG_23_ATI = 0x8938;
		public const uint GL_REG_24_ATI = 0x8939;
		public const uint GL_REG_25_ATI = 0x893A;
		public const uint GL_REG_26_ATI = 0x893B;
		public const uint GL_REG_27_ATI = 0x893C;
		public const uint GL_REG_28_ATI = 0x893D;
		public const uint GL_REG_29_ATI = 0x893E;
		public const uint GL_REG_30_ATI = 0x893F;
		public const uint GL_REG_31_ATI = 0x8940;
		public const uint GL_CON_0_ATI = 0x8941;
		public const uint GL_CON_1_ATI = 0x8942;
		public const uint GL_CON_2_ATI = 0x8943;
		public const uint GL_CON_3_ATI = 0x8944;
		public const uint GL_CON_4_ATI = 0x8945;
		public const uint GL_CON_5_ATI = 0x8946;
		public const uint GL_CON_6_ATI = 0x8947;
		public const uint GL_CON_7_ATI = 0x8948;
		public const uint GL_CON_8_ATI = 0x8949;
		public const uint GL_CON_9_ATI = 0x894A;
		public const uint GL_CON_10_ATI = 0x894B;
		public const uint GL_CON_11_ATI = 0x894C;
		public const uint GL_CON_12_ATI = 0x894D;
		public const uint GL_CON_13_ATI = 0x894E;
		public const uint GL_CON_14_ATI = 0x894F;
		public const uint GL_CON_15_ATI = 0x8950;
		public const uint GL_CON_16_ATI = 0x8951;
		public const uint GL_CON_17_ATI = 0x8952;
		public const uint GL_CON_18_ATI = 0x8953;
		public const uint GL_CON_19_ATI = 0x8954;
		public const uint GL_CON_20_ATI = 0x8955;
		public const uint GL_CON_21_ATI = 0x8956;
		public const uint GL_CON_22_ATI = 0x8957;
		public const uint GL_CON_23_ATI = 0x8958;
		public const uint GL_CON_24_ATI = 0x8959;
		public const uint GL_CON_25_ATI = 0x895A;
		public const uint GL_CON_26_ATI = 0x895B;
		public const uint GL_CON_27_ATI = 0x895C;
		public const uint GL_CON_28_ATI = 0x895D;
		public const uint GL_CON_29_ATI = 0x895E;
		public const uint GL_CON_30_ATI = 0x895F;
		public const uint GL_CON_31_ATI = 0x8960;
		public const uint GL_MOV_ATI = 0x8961;
		public const uint GL_ADD_ATI = 0x8963;
		public const uint GL_MUL_ATI = 0x8964;
		public const uint GL_SUB_ATI = 0x8965;
		public const uint GL_DOT3_ATI = 0x8966;
		public const uint GL_DOT4_ATI = 0x8967;
		public const uint GL_MAD_ATI = 0x8968;
		public const uint GL_LERP_ATI = 0x8969;
		public const uint GL_CND_ATI = 0x896A;
		public const uint GL_CND0_ATI = 0x896B;
		public const uint GL_DOT2_ADD_ATI = 0x896C;
		public const uint GL_SECONDARY_INTERPOLATOR_ATI = 0x896D;
		public const uint GL_NUM_FRAGMENT_REGISTERS_ATI = 0x896E;
		public const uint GL_NUM_FRAGMENT_CONSTANTS_ATI = 0x896F;
		public const uint GL_NUM_PASSES_ATI = 0x8970;
		public const uint GL_NUM_INSTRUCTIONS_PER_PASS_ATI = 0x8971;
		public const uint GL_NUM_INSTRUCTIONS_TOTAL_ATI = 0x8972;
		public const uint GL_NUM_INPUT_INTERPOLATOR_COMPONENTS_ATI = 0x8973;
		public const uint GL_NUM_LOOPBACK_COMPONENTS_ATI = 0x8974;
		public const uint GL_COLOR_ALPHA_PAIRING_ATI = 0x8975;
		public const uint GL_SWIZZLE_STR_ATI = 0x8976;
		public const uint GL_SWIZZLE_STQ_ATI = 0x8977;
		public const uint GL_SWIZZLE_STR_DR_ATI = 0x8978;
		public const uint GL_SWIZZLE_STQ_DQ_ATI = 0x8979;
		public const uint GL_SWIZZLE_STRQ_ATI = 0x897A;
		public const uint GL_SWIZZLE_STRQ_DQ_ATI = 0x897B;
		public const uint GL_RED_BIT_ATI = 0x00000001;
		public const uint GL_GREEN_BIT_ATI = 0x00000002;
		public const uint GL_BLUE_BIT_ATI = 0x00000004;
		public const uint GL_2X_BIT_ATI = 0x00000001;
		public const uint GL_4X_BIT_ATI = 0x00000002;
		public const uint GL_8X_BIT_ATI = 0x00000004;
		public const uint GL_HALF_BIT_ATI = 0x00000008;
		public const uint GL_QUARTER_BIT_ATI = 0x00000010;
		public const uint GL_EIGHTH_BIT_ATI = 0x00000020;
		public const uint GL_SATURATE_BIT_ATI = 0x00000040;
		public const uint GL_COMP_BIT_ATI = 0x00000002;
		public const uint GL_NEGATE_BIT_ATI = 0x00000004;
		public const uint GL_BIAS_BIT_ATI = 0x00000008;
		public const uint GL_ATI_texture_mirror_once = 1;
		public const uint GL_MIRROR_CLAMP_ATI = 0x8742;
		public const uint GL_MIRROR_CLAMP_TO_EDGE_ATI = 0x8743;
		public const uint GL_ATI_element_array = 1;
		public const uint GL_ELEMENT_ARRAY_ATI = 0x8768;
		public const uint GL_ELEMENT_ARRAY_TYPE_ATI = 0x8769;
		public const uint GL_ELEMENT_ARRAY_POINTER_ATI = 0x876A;
		public const uint GL_ATI_vertex_streams = 1;
		public const uint GL_MAX_VERTEX_STREAMS_ATI = 0x876B;
		public const uint GL_VERTEX_SOURCE_ATI = 0x876C;
		public const uint GL_VERTEX_STREAM0_ATI = 0x876D;
		public const uint GL_VERTEX_STREAM1_ATI = 0x876E;
		public const uint GL_VERTEX_STREAM2_ATI = 0x876F;
		public const uint GL_VERTEX_STREAM3_ATI = 0x8770;
		public const uint GL_VERTEX_STREAM4_ATI = 0x8771;
		public const uint GL_VERTEX_STREAM5_ATI = 0x8772;
		public const uint GL_VERTEX_STREAM6_ATI = 0x8773;
		public const uint GL_VERTEX_STREAM7_ATI = 0x8774;
		public const uint GL_ATI_vertex_array_object = 1;
		public const uint GL_STATIC_ATI = 0x8760;
		public const uint GL_DYNAMIC_ATI = 0x8761;
		public const uint GL_PRESERVE_ATI = 0x8762;
		public const uint GL_DISCARD_ATI = 0x8763;
		public const uint GL_OBJECT_BUFFER_SIZE_ATI = 0x8764;
		public const uint GL_OBJECT_BUFFER_USAGE_ATI = 0x8765;
		public const uint GL_ARRAY_OBJECT_BUFFER_ATI = 0x8766;
		public const uint GL_ARRAY_OBJECT_OFFSET_ATI = 0x8767;
		public const uint GL_HP_occlusion_test = 1;
		public const uint GL_OCCLUSION_TEST_HP = 0x8165;

		#endregion OpenGL Extension Constants

		#endregion
	
		protected override void SetSurfaceParams(ColorEx ambient, ColorEx diffuse, ColorEx specular, ColorEx emissive, float shininess)
		{
			// ambient
			float[] vals = GLColorArray(ambient);
			glMaterialfv(GL_FRONT_AND_BACK, GL_AMBIENT, vals);

			// diffuse
			vals[0] = diffuse.r; vals[1] = diffuse.g; vals[2] = diffuse.b; vals[3] = diffuse.a;
			glMaterialfv(GL_FRONT_AND_BACK, GL_DIFFUSE, vals);

			// specular
			vals[0] = specular.r; vals[1] = specular.g; vals[2] = specular.b; vals[3] = specular.a;
			glMaterialfv(GL_FRONT_AND_BACK, GL_SPECULAR, vals);

			// emissive
			vals[0] = emissive.r; vals[1] = emissive.g; vals[2] = emissive.b; vals[3] = emissive.a;
			glMaterialfv(GL_FRONT_AND_BACK, GL_EMISSION, vals);

			// shininess
			glMaterialf(GL_FRONT_AND_BACK, GL_SHININESS, shininess);
		}
	
		protected override void SetTextureAddressingMode(int stage, TextureAddressing texAddressingMode)
		{
			uint type = 0;

			// find out the GL equivalent of out TextureAddressing enum
			switch(texAddressingMode)
			{
				case TextureAddressing.Wrap:
					type = GL_REPEAT;
					break;

				case TextureAddressing.Mirror:
					type = GL_MIRRORED_REPEAT;
					break;

				case TextureAddressing.Clamp:
					type = GL_CLAMP_TO_EDGE;
					break;
			} // end switch

			// set the GL texture wrap params for the specified unit
			EXT.glActiveTextureARB(GL_TEXTURE0 + (uint)stage);
			glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, type);
			glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, type);
			EXT.glActiveTextureARB(GL_TEXTURE0);
		}
	
		public override void SetTextureBlendMode(int stage, LayerBlendModeEx blendMode)
		{
			float[] cv1 = blendMode.colorArg1.ToArrayRGBA();
			float[] cv2 = blendMode.colorArg2.ToArrayRGBA();
			float[] av1 = new float[4] {0.0f, 0.0f, 0.0f, blendMode.alphaArg1};
			float[] av2 = new float[4] {0.0f, 0.0f, 0.0f, blendMode.alphaArg2};

			uint src1op, src2op, cmd;

			src1op = src2op = cmd = 0;

			switch(blendMode.source1)
			{
				case LayerBlendSource.Current:
					src1op = GL_PREVIOUS;
					break;

				case LayerBlendSource.Texture:
					src1op = GL_TEXTURE;
					break;
				
				case LayerBlendSource.Manual:
					src1op = GL_CONSTANT;
					break;
				
					// no diffuse or specular equivalent right now
				default:
					src1op = 0;
					break;
			}

			switch(blendMode.source2)
			{
				case LayerBlendSource.Current:
					src2op = GL_PREVIOUS;
					break;

				case LayerBlendSource.Texture:
					src2op = GL_TEXTURE;
					break;
				
				case LayerBlendSource.Manual:
					src2op = GL_CONSTANT;
					break;
				
					// no diffuse or specular equivalent right now
				default:
					src2op = 0;
					break;
			}

			switch (blendMode.operation)
			{
				case LayerBlendOperationEx.Source1:
					cmd = GL_REPLACE;
					break;
				case LayerBlendOperationEx.Source2:
					cmd = GL_REPLACE;
					break;
				case LayerBlendOperationEx.Modulate:
					cmd = GL_MODULATE;
					break;
				case LayerBlendOperationEx.ModulateX2:
					cmd = GL_MODULATE;
					break;
				case LayerBlendOperationEx.ModulateX4:
					cmd = GL_MODULATE;
					break;
				case LayerBlendOperationEx.Add:
					cmd = GL_ADD;
					break;
				case LayerBlendOperationEx.AddSigned:
					cmd = GL_ADD_SIGNED;
					break;
				case LayerBlendOperationEx.BlendTextureAlpha:
					cmd = GL_INTERPOLATE;
					break;
				case LayerBlendOperationEx.BlendCurrentAlpha:
					cmd = GL_INTERPOLATE;
					break;
				case LayerBlendOperationEx.DotProduct:
					// Check for Dot3 support
					cmd = caps.CheckCap(Capabilities.Dot3Bump) ? GL_DOT3_RGB : GL_MODULATE;
					break;

				default:
					cmd = 0;
					break;
			} // end switch

			EXT.glActiveTextureARB(GL_TEXTURE0 + (uint)stage);
			glTexEnvi(GL_TEXTURE_ENV, GL_TEXTURE_ENV_MODE, GL_COMBINE);

			if (blendMode.blendType == LayerBlendType.Color)
			{
				glTexEnvi(GL_TEXTURE_ENV, GL_COMBINE_RGB, cmd);
				glTexEnvi(GL_TEXTURE_ENV, GL_SOURCE0_RGB, src1op);
				glTexEnvi(GL_TEXTURE_ENV, GL_SOURCE1_RGB, src2op);
				glTexEnvi(GL_TEXTURE_ENV, GL_SOURCE2_RGB, GL_CONSTANT);
			}
			else
			{
				if (cmd != GL_DOT3_RGB)
					glTexEnvi(GL_TEXTURE_ENV, GL_COMBINE_ALPHA, cmd);

				glTexEnvi(GL_TEXTURE_ENV, GL_SOURCE0_ALPHA, src1op);
				glTexEnvi(GL_TEXTURE_ENV, GL_SOURCE1_ALPHA, src2op);
				glTexEnvi(GL_TEXTURE_ENV, GL_SOURCE2_ALPHA, GL_CONSTANT);
			}

			switch (blendMode.operation)
			{
				case LayerBlendOperationEx.BlendTextureAlpha:
					glTexEnvi(GL_TEXTURE_ENV, GL_SOURCE2_RGB, GL_TEXTURE);
					glTexEnvi(GL_TEXTURE_ENV, GL_SOURCE2_ALPHA, GL_TEXTURE);
					break;
				case LayerBlendOperationEx.BlendCurrentAlpha:
					glTexEnvi(GL_TEXTURE_ENV, GL_SOURCE2_RGB, GL_PREVIOUS);
					glTexEnvi(GL_TEXTURE_ENV, GL_SOURCE2_ALPHA, GL_PREVIOUS);
					break;
				case LayerBlendOperationEx.Modulate:
					glTexEnvi(GL_TEXTURE_ENV, blendMode.blendType == LayerBlendType.Color ? 
					GL_RGB_SCALE : GL_ALPHA_SCALE, 1);
					break;
				case LayerBlendOperationEx.ModulateX2:
					glTexEnvi(GL_TEXTURE_ENV, blendMode.blendType == LayerBlendType.Color ? 
					GL_RGB_SCALE : GL_ALPHA_SCALE, 2);
					break;
				case LayerBlendOperationEx.ModulateX4:
					glTexEnvi(GL_TEXTURE_ENV, blendMode.blendType == LayerBlendType.Color ? 
					GL_RGB_SCALE : GL_ALPHA_SCALE, 4);
					break;
				default:
					break;
			}

			glTexEnvi(GL_TEXTURE_ENV, GL_OPERAND0_RGB, GL_SRC_COLOR);
			glTexEnvi(GL_TEXTURE_ENV, GL_OPERAND1_RGB, GL_SRC_COLOR);
			glTexEnvi(GL_TEXTURE_ENV, GL_OPERAND2_RGB, GL_SRC_COLOR);
			glTexEnvi(GL_TEXTURE_ENV, GL_OPERAND0_ALPHA, GL_SRC_ALPHA);
			glTexEnvi(GL_TEXTURE_ENV, GL_OPERAND1_ALPHA, GL_SRC_ALPHA);
			glTexEnvi(GL_TEXTURE_ENV, GL_OPERAND2_ALPHA, GL_SRC_ALPHA);

			if (blendMode.blendType == LayerBlendType.Color && blendMode.source1 == LayerBlendSource.Manual)
				glTexEnvfv(GL_TEXTURE_ENV, GL_TEXTURE_ENV_COLOR, cv1);
			if (blendMode.blendType == LayerBlendType.Color && blendMode.source2 == LayerBlendSource.Manual)
				glTexEnvfv(GL_TEXTURE_ENV, GL_TEXTURE_ENV_COLOR, cv2);
            
			EXT.glActiveTextureARB(GL_TEXTURE0);
		}
	
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

			EXT.glActiveTextureARB( GL_TEXTURE0 + (uint)stage );

			switch(method)
			{
				case TexCoordCalcMethod.None:
					glDisable( GL_TEXTURE_GEN_S );
					glDisable( GL_TEXTURE_GEN_T );
					glDisable( GL_TEXTURE_GEN_R );
					glDisable( GL_TEXTURE_GEN_Q );
					break;

				case TexCoordCalcMethod.EnvironmentMap:
					glTexGeni( GL_S, GL_TEXTURE_GEN_MODE, (int)GL_SPHERE_MAP );
					glTexGeni( GL_T, GL_TEXTURE_GEN_MODE, (int)GL_SPHERE_MAP );

					glEnable( GL_TEXTURE_GEN_S );
					glEnable( GL_TEXTURE_GEN_T );
					glDisable( GL_TEXTURE_GEN_R );
					glDisable( GL_TEXTURE_GEN_Q );
					break;

				case TexCoordCalcMethod.EnvironmentMapPlanar:            
					// TODO: Check GL Version here?
					// XXX This doesn't seem right?!
					/*#ifdef GL_VERSION_1_3
					glTexGeni( GL_S, GL_TEXTURE_GEN_MODE, (int)GL_REFLECTION_MAP );
					glTexGeni( GL_T, GL_TEXTURE_GEN_MODE, (int)GL_REFLECTION_MAP );
					glTexGeni( GL_R, GL_TEXTURE_GEN_MODE, (int)GL_REFLECTION_MAP );

					glEnable( GL_TEXTURE_GEN_S );
					glEnable( GL_TEXTURE_GEN_T );
					glEnable( GL_TEXTURE_GEN_R );
					glDisable( GL_TEXTURE_GEN_Q ); */
					//#else
					glTexGeni( GL_S, GL_TEXTURE_GEN_MODE, (int)GL_SPHERE_MAP );
					glTexGeni( GL_T, GL_TEXTURE_GEN_MODE, (int)GL_SPHERE_MAP );

					glEnable( GL_TEXTURE_GEN_S );
					glEnable( GL_TEXTURE_GEN_T );
					glDisable( GL_TEXTURE_GEN_R );
					glDisable( GL_TEXTURE_GEN_Q );
					//#endif
					break;

				case TexCoordCalcMethod.EnvironmentMapReflection:
            
					glTexGeni( GL_S, GL_TEXTURE_GEN_MODE, (int)GL_REFLECTION_MAP );
					glTexGeni( GL_T, GL_TEXTURE_GEN_MODE, (int)GL_REFLECTION_MAP );
					glTexGeni( GL_R, GL_TEXTURE_GEN_MODE, (int)GL_REFLECTION_MAP );

					glEnable( GL_TEXTURE_GEN_S );
					glEnable( GL_TEXTURE_GEN_T );
					glEnable( GL_TEXTURE_GEN_R );
					glDisable( GL_TEXTURE_GEN_Q );

					// We need an extra texture matrix here
					// This sets the texture matrix to be the inverse of the modelview matrix
					useAutoTextureMatrix = true;

					glGetFloatv( GL_MODELVIEW_MATRIX, m);

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
					glTexGeni( GL_S, GL_TEXTURE_GEN_MODE, (int)GL_NORMAL_MAP );
					glTexGeni( GL_T, GL_TEXTURE_GEN_MODE, (int)GL_NORMAL_MAP );
					glTexGeni( GL_R, GL_TEXTURE_GEN_MODE, (int)GL_NORMAL_MAP );

					glEnable( GL_TEXTURE_GEN_S );
					glEnable( GL_TEXTURE_GEN_T );
					glEnable( GL_TEXTURE_GEN_R );
					glDisable( GL_TEXTURE_GEN_Q );
					break;

				default:
					break;
			}

			EXT.glActiveTextureARB(GL_TEXTURE0);		
		}
	
		protected override void SetTextureMatrix(int stage, Matrix4 xform)
		{
			float[] glMatrix = MakeGLMatrix(xform);

			glMatrix[12] = glMatrix[8];
			glMatrix[13] = glMatrix[9];

			EXT.glActiveTextureARB(GL_TEXTURE0 + (uint)stage);
			glMatrixMode(GL_TEXTURE);

			// if texture matrix was precalced, use that
			if(useAutoTextureMatrix)
			{
				glLoadMatrixf(autoTextureMatrix);
				glMultMatrixf(glMatrix);
			}
			else
				glLoadMatrixf(glMatrix);

			// reset to mesh view matrix and to tex unit 0
			glMatrixMode(GL_MODELVIEW);
			EXT.glActiveTextureARB(GL_TEXTURE0);
		}
	
		public override void CheckCaps()
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
			glGetIntegerv(GL_MAX_TEXTURE_UNITS, out numTextureUnits);
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
			if(GLHelper.SupportsExtension("GL_SGIS_generate_mipmap"))
				caps.SetCap(Capabilities.HardwareMipMaps);

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
	}
}
