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
using Axiom.Configuration;
using Axiom.Core;
using Axiom.Enumerations;
using Axiom.MathLib;
using Axiom.SubSystems.Rendering;
using Axiom.Utility;

using FogMode = Axiom.SubSystems.Rendering.FogMode;
using LightType = Axiom.SubSystems.Rendering.LightType;
using StencilOperation = Axiom.SubSystems.Rendering.StencilOperation;
using TextureFiltering = Axiom.SubSystems.Rendering.TextureFiltering;
using Microsoft.DirectX;
using DX = Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using D3D = Microsoft.DirectX.Direct3D;

namespace RenderSystem_DirectX9
{
	/// <summary>
	/// DirectX9 Render System implementation.
	/// </summary>
	public class DirectX9Renderer : RenderSystem, IPlugin
	{
		protected D3D.Device device;
		protected D3D.Caps d3dCaps;

		/// <summary>Signifies whether the current frame being rendered is the first.</summary>
		protected bool isFirstFrame = true;
		const int MAX_LIGHTS = 8;
		protected Axiom.Core.Light[] lights = new Axiom.Core.Light[MAX_LIGHTS];

		// stores texture stage info locally for convenience
		// TODO: finish using this in all appropriate methods
		internal D3DTextureStageDesc[] texStageDesc = new D3DTextureStageDesc[Config.MaxTextureLayers];

		protected int primCount;
		protected int renderCount = 0;

		public DirectX9Renderer()
		{
			InitConfigOptions();

			// init the texture stage descriptions
			for(int i = 0; i < Config.MaxTextureLayers; i++)
			{
				texStageDesc[i].autoTexCoordType = TexCoordCalcMethod.None;
				texStageDesc[i].coordIndex = 0;
				texStageDesc[i].texType = D3DTexType.Normal;
				texStageDesc[i].tex = null;
			}
		}

		#region Implementation of RenderSystem

		public override ColorEx AmbientLight
		{
			set
			{
				device.RenderState.Ambient = value.ToColor();
			}
		}
	
		public override bool LightingEnabled
		{
			set
			{
				device.RenderState.Lighting = value;
			}
		}
	
		public override Shading ShadingType
		{
			set
			{
				// TODO:  Add DirectX9Renderer.ShadingType setter implementation
			}
		}
	
		public override short StencilBufferBitDepth
		{
			get
			{
				// TODO:  Add DirectX9Renderer.StencilBufferBitDepth getter implementation
				return 0;
			}
		}
	
		public override StencilOperation StencilBufferDepthFailOperation
		{
			set
			{
				// TODO:  Add DirectX9Renderer.StencilBufferDepthFailOperation setter implementation
			}
		}
	
		public override StencilOperation StencilBufferFailOperation
		{
			set
			{
				// TODO:  Add DirectX9Renderer.StencilBufferFailOperation setter implementation
			}
		}
	
		public override CompareFunction StencilBufferFunction
		{
			set
			{
				// TODO:  Add DirectX9Renderer.StencilBufferFunction setter implementation
			}
		}
	
		public override long StencilBufferMask
		{
			set
			{
				// TODO:  Add DirectX9Renderer.StencilBufferMask setter implementation
			}
		}
	
		public override Axiom.SubSystems.Rendering.StencilOperation StencilBufferPassOperation
		{
			set
			{
				// TODO:  Add DirectX9Renderer.StencilBufferPassOperation setter implementation
			}
		}
	
		public override long StencilBufferReferenceValue
		{
			set
			{
				// TODO:  Add DirectX9Renderer.StencilBufferReferenceValue setter implementation
			}
		}
	
		public override bool StencilCheckEnabled
		{
			set
			{
				// TODO:  Add DirectX9Renderer.StencilCheckEnabled setter implementation
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// DOC
		protected override VertexBufferBinding VertexBufferBinding
		{
			get
			{
				return null;
			}
			set
			{
				// TODO: Optimize to remove foreach
				foreach(DictionaryEntry binding in value.Bindings)
				{
					D3DHardwareVertexBuffer buffer = 
						(D3DHardwareVertexBuffer)binding.Value;

					ushort stream = (ushort)binding.Key;

					device.SetStreamSource((int)stream, buffer.D3DVertexBuffer, 0, buffer.VertexSize);
				}

				// TODO: Unbind any unused sources
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// DOC
		protected override Axiom.SubSystems.Rendering.VertexDeclaration VertexDeclaration
		{
			get
			{
				return null;
			}
			set
			{
				// TODO: Check for duplicate setting and avoid setting if dupe
				D3DVertexDeclaration d3dVertDecl = (D3DVertexDeclaration)value;

				device.VertexDeclaration = d3dVertDecl.D3DVertexDecl;
			}
		}


	
		public override TextureFiltering TextureFiltering
		{
			set
			{
				// get the number of supported texture units
				int numUnits = caps.NumTextureUnits;

				for(int i = 0; i < numUnits; i++)
				{
					// get a reference to the current sampler for this tex unit
					D3D.Sampler sampler = device.SamplerState[i];

					// set the sampler states appropriately
					switch(value)
					{
						case Axiom.SubSystems.Rendering.TextureFiltering.None:
								sampler.MagFilter = TextureFilter.Point;
								sampler.MinFilter = TextureFilter.Point;
								sampler.MipFilter = TextureFilter.None;
								break;

						case Axiom.SubSystems.Rendering.TextureFiltering.Bilinear:
							if(d3dCaps.TextureFilterCaps.SupportsMinifyLinear)
							{
								sampler.MagFilter = TextureFilter.Linear;
								sampler.MinFilter = TextureFilter.Linear;
								sampler.MipFilter = TextureFilter.Point;
							}

							break;

						case Axiom.SubSystems.Rendering.TextureFiltering.Trilinear:
							if(d3dCaps.TextureFilterCaps.SupportsMipMapLinear)
							{
								sampler.MagFilter = TextureFilter.Linear;
								sampler.MinFilter = TextureFilter.Linear;
								sampler.MipFilter = TextureFilter.Linear;
							}

							break;
					}
				}
			}
		}
	
		public override RenderWindow CreateRenderWindow(string name, System.Windows.Forms.Control target, int width, int height, int colorDepth, bool isFullscreen, int left, int top, bool depthBuffer, RenderWindow parent)
		{
			RenderWindow window = new D3DWindow();

			// if the device has not been created, the create it
			if(device == null)
			{
				PresentParameters presentParams = new PresentParameters();

				presentParams.Windowed = !isFullscreen;
				presentParams.BackBufferCount = 1;
				// TODO: Look into using Copy and why Discard has nasty effects with multiple viewports.

				presentParams.DeviceWindow = target;
				presentParams.EnableAutoDepthStencil = depthBuffer;
				presentParams.BackBufferWidth = width;
				presentParams.BackBufferHeight = height;
				presentParams.MultiSample = MultiSampleType.None;
				presentParams.SwapEffect = SwapEffect.Discard;
				presentParams.PresentationInterval = PresentInterval.Immediate;

				if(isFullscreen)
				{
					// supports 16 and 32 bit color
					if(colorDepth == 16)
						presentParams.BackBufferFormat = Format.R5G6B5;
					else
						presentParams.BackBufferFormat = Format.X8R8G8B8;
				}
				else
				{
					// TODO: Set this up from the D3DDriver properties, which include current desktop format.
					// Hardcoded for 32 because i always use that for now.
					presentParams.BackBufferFormat = Format.X8R8G8B8;
					//presentParams.SwapEffect = SwapEffect.Copy;
				}

				if(colorDepth > 16)
				{
					// check for 24 bit Z buffer with 8 bit stencil (optimal choice)
					if(!D3D.Manager.CheckDeviceFormat(0, DeviceType.Hardware, presentParams.BackBufferFormat, Usage.DepthStencil, ResourceType.Surface, DepthFormat.D24S8))
					{
						// doh, check for 32 bit Z buffer then
						if(!D3D.Manager.CheckDeviceFormat(0, DeviceType.Hardware, presentParams.BackBufferFormat, Usage.DepthStencil, ResourceType.Surface, DepthFormat.D32))
						{
							// float doh, just use 16 bit Z buffer
							presentParams.AutoDepthStencilFormat = DepthFormat.D16;
						}
						else
						{
							// use 32 bit Z buffer
							presentParams.AutoDepthStencilFormat = DepthFormat.D32;
						}
					}
					else
					{
						// <flair>Woooooooooo!</flair>
						presentParams.AutoDepthStencilFormat = DepthFormat.D24S8;
					}
				}
				else
				{
					// use 16 bit Z buffer if they arent using true color
					presentParams.AutoDepthStencilFormat = DepthFormat.D16;
				}

				// create the D3D Device
				// TODO: Do testing to fall back to vertex processing other that Hardware (need to swap vid cards to test this)
				device = new D3D.Device(0, DeviceType.Hardware, target, CreateFlags.HardwareVertexProcessing | CreateFlags.PureDevice, presentParams);
				device.DeviceReset += new EventHandler(OnResetDevice);
				this.OnResetDevice(device, null);
				device.DeviceLost += new EventHandler(device_DeviceLost);

				// save the device capabilites
				d3dCaps = device.DeviceCaps;
			}

			// create the window
			window.Create(name, target, width, height, colorDepth, isFullscreen, left, top, depthBuffer, device); 

			// add the new window to the RenderWindow collection
			this.renderWindows.Add(window);

			// by creating our texture manager, singleton TextureManager will hold our implementation
			textureMgr = new D3DTextureManager(device);

			// intializes the HardwareBufferManager singleton
			hardwareBufferManager = new D3DHardwareBufferManager(device);

			return window;
		}

		public override void Shutdown()
		{
			base.Shutdown ();

			// TODO: Re eval shutdown

			// TODO: Disposing of the device hung the app, read into this
			//if(device != null)
				//device.Dispose();
		}
		

		/// <summary>
		///		Sets the rasterization mode to use during rendering.
		/// </summary>
		protected override SceneDetailLevel RasterizationMode
		{
			set
			{
				switch(value)
				{
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

		protected override void SetFog(Axiom.SubSystems.Rendering.FogMode mode, ColorEx color, float density, float start, float end)
		{
			// disable fog if set to none
			if(mode == FogMode.None)
			{
				device.RenderState.FogTableMode = D3D.FogMode.None;
				device.RenderState.FogEnable = false;
			}
			else
			{
				// enable fog
				device.RenderState.FogEnable = true;

				D3D.FogMode d3dFogMode = 0;

				// convert the fog mode value
				switch(mode)
				{
					case FogMode.Exp:
						d3dFogMode = D3D.FogMode.Exp;
						break;

					case FogMode.Exp2:
						d3dFogMode = D3D.FogMode.Exp2;
						break;

					case FogMode.Linear:
						d3dFogMode = D3D.FogMode.Linear;
						break;
				} // switch

				// set the rest of the fog render states
				device.RenderState.FogVertexMode = D3D.FogMode.None;
				device.RenderState.FogTableMode = d3dFogMode;
				device.RenderState.FogColor = color.ToColor();
				device.RenderState.FogStart = start;
				device.RenderState.FogEnd = end;
				device.RenderState.FogDensity = density;
			}
		}

		public override RenderWindow Initialize(bool autoCreateWindow)
		{
			base.Initialize (autoCreateWindow);

			RenderWindow renderWindow = null;

			if(autoCreateWindow)
			{
				EngineConfig.DisplayModeRow[] modes = 
					(EngineConfig.DisplayModeRow[])engineConfig.DisplayMode.Select("Selected = true");

				EngineConfig.DisplayModeRow mode = modes[0];

				// create a default form window
				DefaultForm newWindow = RenderWindow.CreateDefaultForm(0, 0, mode.Width, mode.Height, mode.FullScreen);

				// create the render window
				renderWindow = this.CreateRenderWindow("Main Window", newWindow, mode.Width, mode.Height, mode.Bpp, mode.FullScreen, 0, 0, true, null);
				
				newWindow.Target.Visible = false;

				newWindow.Show();
				
				// set the default form's renderwindow so it can access it internally
				newWindow.RenderWindow = renderWindow;

			}

			return renderWindow;
		}

		public override Axiom.MathLib.Matrix4 MakeProjectionMatrix(float fov, float aspectRatio, float near, float far)
		{
			Matrix4 matrix = Matrix4.Zero;

			float theta = MathUtil.DegreesToRadians(fov * 0.5f);
			float h = 1 / MathUtil.Tan(theta);
			float w = h / aspectRatio;
			float q = far / (far - near);

			matrix[0,0] = w;
			matrix[1,1] = h;
			matrix[2,2] = q;
			matrix[2,3] = -q * near;
			matrix[3,2] = 1.0f;

			return matrix;
		}

		/// <summary>
		/// 
		/// </summary>
		protected override void BeginFrame()
		{
			Debug.Assert(activeViewport != null, "BeingFrame cannot run without an active viewport.");

			// see if the current render target is a render window
			if(activeRenderTarget is RenderWindow)
			{
				RenderWindow window = (RenderWindow)activeRenderTarget;

				// if the window is not full screen, then use swap chains for rendering
				if(!window.IsFullScreen)
				{
					// get the swap chain associated with the current render target
					SwapChain swapChain = (SwapChain)activeRenderTarget.CustomAttributes["SwapChain"];

					// set the swap chains back buffer as the current render target of the device
					Surface back = swapChain.GetBackBuffer(0, BackBufferType.Mono);
					device.SetRenderTarget(0, back);

					// must be called manually in this scenario, because SetRenderTarget resets the viewport
					this.SetViewport(activeViewport);
				}
			}

			if(activeViewport.ClearEveryFrame)
			{
				// clear the device
				// TODO: Setting the rect works fine for windowed mode, but should not be necessary.  Revisit this later.
				System.Drawing.Rectangle rect = new System.Drawing.Rectangle(activeViewport.ActualLeft, activeViewport.ActualTop, activeViewport.ActualWidth, activeViewport.ActualHeight);
				device.Clear(ClearFlags.ZBuffer | ClearFlags.Target, activeViewport.BackgroundColor.ToColor(), 1.0f, 0, new System.Drawing.Rectangle[] {rect});
			}

			// begin the D3D scene for the current viewport
			device.BeginScene();

			// set initial render states if this is the first frame. we only want to do 
			//	this once since renderstate changes are expensive
			if(isFirstFrame)
			{
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
		protected override void EndFrame()
		{
			//HackGeometry();

			// end the D3D scene
			device.EndScene();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="viewport"></param>
		protected override void SetViewport(Axiom.Core.Viewport viewport)
		{
			if(activeViewport != viewport && viewport.IsUpdated)
			{
				// store this viewport and it's target
				activeViewport = viewport;
				activeRenderTarget = viewport.Target;

				D3D.Viewport d3dvp = new D3D.Viewport();

				// HACK: Do the right way
				device.RenderState.CullMode = Cull.None;

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

		public override void Render(RenderOperation op)
		{
			// class base implementation first
			base.Render (op);

			// set the vertex declaration and buffer binding
			this.VertexDeclaration = op.vertexData.vertexDeclaration;
			this.VertexBufferBinding = op.vertexData.vertexBufferBinding;

			PrimitiveType primType = 0;

			switch(op.operationType)
			{
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
			if(op.useIndices)
			{
				D3DHardwareIndexBuffer idxBuffer = 
					(D3DHardwareIndexBuffer)op.indexData.indexBuffer;

				// set the index buffer on the device
				device.Indices = idxBuffer.D3DIndexBuffer;

				// draw the indexed primitives
				device.DrawIndexedPrimitives(primType, op.vertexData.vertexStart, 0, op.vertexData.vertexCount, 
					op.indexData.indexStart, primCount);
			}
			else
			{
				// draw vertices as is
				device.DrawPrimitives(primType, op.vertexData.vertexStart, primCount);
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
			D3DTexture texture = (D3DTexture)TextureManager.Instance[textureName];

			if(enabled && texture != null)
				device.SetTexture(stage, texture.DXTexture);
			else
			{
				device.SetTexture(stage, null);
				device.TextureState[stage].ColorOperation = D3D.TextureOperation.Disable;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="stage"></param>
		/// <param name="method"></param>
		protected override void SetTextureCoordCalculation(int stage, TexCoordCalcMethod method)
		{
			// save this for texture matrix calcs later
			texStageDesc[stage].autoTexCoordType = method;

			// choose normalization method
			if(method == TexCoordCalcMethod.EnvironmentMapNormal ||
				method == TexCoordCalcMethod.EnvironmentMap)
			{
				device.RenderState.NormalizeNormals = true;
			}
			else
				device.RenderState.NormalizeNormals = false;

			// set auto texcoord gen mode if present
			// if not present we've already set it through SetTextureCoordSet
			if(method != TexCoordCalcMethod.None)
				device.TextureState[stage].TextureCoordinateIndex = D3DHelper.ConvertEnum(method, d3dCaps);
		}

		#endregion

		#region Implementation of IPlugin

		public void Start()
		{
			// add an instance of this plugin to the list of available RenderSystems
			Engine.Instance.RenderSystems.Add("DirectX9", this);
		}
		public void Stop()
		{
			// dispose of the D3D device
			// TODO: Find out why this hangs
			//device.Dispose();
		}
		#endregion

		protected override Axiom.MathLib.Matrix4 WorldMatrix
		{
			set
			{
				device.Transform.World = MakeD3DMatrix(value);
			}
		}

		protected override Axiom.MathLib.Matrix4 ViewMatrix
		{
			set
			{
				// flip the transform portion of the matrix for DX and its left-handed coord system
				DX.Matrix dxView = MakeD3DMatrix(value);
				dxView.M13 = -dxView.M13;
				dxView.M23 = -dxView.M23;
				dxView.M33 = -dxView.M33;
				dxView.M43 = -dxView.M43;

				device.Transform.View = dxView;
			}
		}

		protected override Axiom.MathLib.Matrix4 ProjectionMatrix
		{
			set
			{
				device.Transform.Projection = MakeD3DMatrix(value);
			}
		}
	
		protected override void AddLight(Axiom.Core.Light light)
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
			SetD3DLight(lightIndex, light);		
		}

		protected override void UpdateLight(Axiom.Core.Light light)
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
			SetD3DLight(lightIndex, light);
		}

		public override int ConvertColor(ColorEx color)
		{
			return color.ToARGB();
		}

		protected override void SetSceneBlending(SceneBlendFactor src, SceneBlendFactor dest)
		{
			// set the render states after converting the incoming values to D3D.Blend
			device.RenderState.SourceBlend = D3DHelper.ConvertEnum(src);
			device.RenderState.DestinationBlend = D3DHelper.ConvertEnum(dest);
		}

		/// <summary>
		/// 
		/// </summary>
		protected override ushort DepthBias
		{
			set
			{
				// TODO: D3D DepthBias
			}
		}

		/// <summary>
		/// 
		/// </summary>
		protected override bool DepthCheck
		{
			set
			{
				// TODO: D3D DepthCheck
			}
		}

		/// <summary>
		/// 
		/// </summary>
		protected override bool DepthFunction
		{
			set
			{
				// TODO: D3D DepthFunction
			}
		}

		/// <summary>
		/// 
		/// </summary>
		protected override bool DepthWrite
		{
			set
			{
				device.RenderState.ZBufferWriteEnable = value;
			}
		}

		#region Private methods

		/// <summary>
		///		Sets up a light in D3D.
		/// </summary>
		/// <param name="index"></param>
		/// <param name="light"></param>
		private void SetD3DLight(int index, Axiom.Core.Light light)
		{
			if(light.IsVisible)
			{
				switch(light.Type)
				{
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
				
				if(light.Type != LightType.Directional)
				{
					vec = light.DerivedPosition;
					device.Lights[index].Position = new DX.Vector3(vec.x, vec.y, vec.z);
				}

				if(light.Type != LightType.Point)
				{
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

				light.IsModified = false;
			} // if
		}

		/// <summary>
		///		Called in constructor to init configuration.
		/// </summary>
		private void InitConfigOptions()
		{
			Driver driver = D3DHelper.GetDriverInfo();
			
			foreach(VideoMode mode in driver.VideoModes)
			{
				// add a new row to the display settings table
				engineConfig.DisplayMode.AddDisplayModeRow(mode.Width, mode.Height, mode.ColorDepth, false, false);
			}
		}

		private DX.Matrix MakeD3DMatrix(Axiom.MathLib.Matrix4 matrix)
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
		private bool CompareVertexDecls(D3D.VertexElement[] a, D3D.VertexElement[] b)
		{
			// if b is null, return false
			if(b == null)
				return false;

			// compare lengths of the arrays
			if(a.Length != b.Length)
				return false;

			// continuing on, compare each property of each element.  if any differ, return false
			for(int i = 0; i < a.Length; i++)
			{
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

		private void OnResetDevice(object sender, EventArgs e)
		{
			Device resetDevice = (Device)sender;

			Console.WriteLine("Device has been reset!");

			// Turn off culling, so we see the front and back of the triangle
			resetDevice.RenderState.CullMode = Cull.None;
			// Turn on the ZBuffer
			resetDevice.RenderState.ZBufferEnable = true;
			resetDevice.RenderState.Lighting = true;    //make sure lighting is enabled
		}

		private void device_DeviceLost(object sender, EventArgs e)
		{
			Console.WriteLine("Device has been lost!");
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
		protected override void SetTextureAddressingMode(int stage, TextureAddressing texAddressingMode)
		{
			D3D.TextureAddress d3dMode = 0;

			// convert from ours to D3D
			switch(texAddressingMode)
			{
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
	
		public override void SetTextureBlendMode(int stage, LayerBlendModeEx blendMode)
		{
			D3D.TextureOperation d3dTexOp = D3DHelper.ConvertEnum(blendMode.operation);

			// TODO: Verify byte ordering
			if(blendMode.operation == LayerBlendOperationEx.BlendManual)
				device.RenderState.TextureFactor = (new ColorEx(blendMode.blendFactor, 0, 0, 0)).ToARGB();

			if( blendMode.blendType == LayerBlendType.Color )
			{
				// Make call to set operation
				device.TextureState[stage].ColorOperation = d3dTexOp;
			}
			else if( blendMode.blendType == LayerBlendType.Alpha )
			{
				// Make call to set operation
				device.TextureState[stage].AlphaOperation = d3dTexOp;
			}

			// Now set up sources
			ColorEx manualD3D = null;

			if( blendMode.blendType == LayerBlendType.Color )
			{
				manualD3D = new ColorEx(1.0f, blendMode.colorArg1.r, blendMode.colorArg1.g, blendMode.colorArg1.b);
			}
			else if( blendMode.blendType == LayerBlendType.Alpha )
			{
				manualD3D = new ColorEx(blendMode.alphaArg1, 0, 0, 0);
			}

			LayerBlendSource blendSource = blendMode.source1;

			for( int i=0; i < 2; i++ )
			{
				D3D.TextureArgument d3dTexArg = D3DHelper.ConvertEnum(blendSource);

				// set the texture blend factor if this is manual blending
				if(blendSource == LayerBlendSource.Manual)
					device.RenderState.TextureFactor = manualD3D.ToARGB();

				// pick proper argument settings
				if( blendMode.blendType == LayerBlendType.Color )
				{
					if(i == 0)
						device.TextureState[stage].ColorArgument1 = d3dTexArg;
					else if (i ==1)
						device.TextureState[stage].ColorArgument2 = d3dTexArg;
				}
				else if( blendMode.blendType == LayerBlendType.Alpha )
				{
					if(i == 0)
						device.TextureState[stage].AlphaArgument1 = d3dTexArg;
					else if (i ==1)
						device.TextureState[stage].AlphaArgument2 = d3dTexArg;
				}

				// Source2
				blendSource = blendMode.source2;
				if( blendMode.blendType == LayerBlendType.Color )
				{
					manualD3D = new ColorEx(1.0f, blendMode.colorArg2.r, blendMode.colorArg2.g, blendMode.colorArg2.b);
				}
				else if( blendMode.blendType == LayerBlendType.Alpha )
				{
					manualD3D = new ColorEx(blendMode.alphaArg2, 0, 0, 0);
				}
			}
		}
	
		/// <summary>
		/// 
		/// </summary>
		/// <param name="stage"></param>
		/// <param name="index"></param>
		protected override void SetTextureCoordSet(int stage, int index)
		{
			device.TextureState[stage].TextureCoordinateIndex = index;

			// store
			texStageDesc[stage].coordIndex = index;
		}
	
		/// <summary>
		/// 
		/// </summary>
		/// <param name="stage"></param>
		/// <param name="xform"></param>
		protected override void SetTextureMatrix(int stage, Matrix4 xform)
		{
			DX.Matrix d3dMat = DX.Matrix.Identity;
			Matrix4 newMat = xform;

			/* If envmap is applied, but device doesn't support spheremap,
			then we have to use texture transform to make the camera space normal
			reference the envmap properly. This isn't exactly the same as spheremap
			(it looks nasty on flat areas because the camera space normals are the same)
			but it's the best approximation we have in the absence of a proper spheremap */
			if(texStageDesc[stage].autoTexCoordType == TexCoordCalcMethod.EnvironmentMap &&
				!(d3dCaps.VertexProcessingCaps.SupportsTextureGenerationSphereMap))
			{
				DX.Matrix d3dMatEnvMap = DX.Matrix.Identity;
				// set env map values
				d3dMatEnvMap.M11 = 0.5f;
				d3dMatEnvMap.M41 = 0.5f;
				d3dMatEnvMap.M22 = -0.5f;
				d3dMatEnvMap.M42 = 0.5f;

				// convert to our format
				Matrix4 matEnvMap = ConvertD3DMatrix(ref d3dMatEnvMap);

				// concatenate 
				newMat = newMat * matEnvMap;
			}

			// If this is a cubic reflection, we need to modify using the view matrix
			if(texStageDesc[stage].autoTexCoordType == TexCoordCalcMethod.EnvironmentMapReflection)
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

			// convert to D3D format
			d3dMat = MakeD3DMatrix(newMat);

			// need this if texture is a cube map, to invert D3D's z coord
			if(texStageDesc[stage].autoTexCoordType != TexCoordCalcMethod.None)
			{
				d3dMat.M13 = -d3dMat.M13;
				d3dMat.M23 = -d3dMat.M23;
				d3dMat.M33 = -d3dMat.M33;
				d3dMat.M43 = -d3dMat.M43;
			}

			D3D.TransformType d3dTransType = (D3D.TransformType)((int)(D3D.TransformType.Texture0) + stage);

			// set the matrix if it is not the identity
			if(d3dMat != DX.Matrix.Identity)
			{
				// tell D3D the dimension of tex. coord
				int texCoordDim = 0;

				switch(texStageDesc[stage].texType)
				{
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
				if(texStageDesc[stage].autoTexCoordType == TexCoordCalcMethod.EnvironmentMapPlanar)
					device.TextureState[stage].TextureTransform = D3D.TextureTransform.Projected | (D3D.TextureTransform)texCoordDim;
				else
					device.TextureState[stage].TextureTransform = (D3D.TextureTransform)texCoordDim;

				// set the manually calculated texture matrix
				device.SetTransform(d3dTransType, d3dMat);
			}
			else
			{
				// disable texture transformation
				device.TextureState[stage].TextureTransform = D3D.TextureTransform.Disable;

				// set as the identity matrix
				device.SetTransform(d3dTransType, DX.Matrix.Identity);
			}
		}
	
		public override void CheckCaps()
		{
			// get the number of possible texture units
			caps.NumTextureUnits = d3dCaps.MaxSimultaneousTextures;

		}

		/// <summary>
		///		Helper method that converts a DX Matrix to our Matrix4.
		/// </summary>
		/// <param name="d3dMat"></param>
		/// <returns></returns>
		private Matrix4 ConvertD3DMatrix(ref DX.Matrix d3dMat)
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
		public D3DTexType texType;
		public int coordIndex;
		public TexCoordCalcMethod autoTexCoordType;
		public D3D.Texture tex;
	}

	/// <summary>
	///		D3D texture types
	/// </summary>
	internal enum D3DTexType
	{
		Normal,
		Cube,
		Volume,
		None
	}
}
