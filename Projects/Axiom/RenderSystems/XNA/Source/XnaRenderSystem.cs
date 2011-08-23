#region LGPL License
/*
Axiom Graphics Engine Library
Copyright © 2003-2011 Axiom Project Team

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
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;


#if !(XBOX || XBOX360 || SILVERLIGHT )
using System.Windows.Forms;
#endif
using Axiom.Core;
using Axiom.Configuration;
using Axiom.Graphics;
using Axiom.Math;
using XNA = Microsoft.Xna.Framework;
using XFG = Microsoft.Xna.Framework.Graphics;
using Axiom.Collections;
using Axiom.Core.Collections;
using Axiom.Media;
using Axiom.Math.Collections;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.Xna
{
	public class XnaRenderSystem : RenderSystem, IServiceProvider
	{
		#region Constants

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

		#endregion Constants

		#region Inner Types

		public struct ZBufferFormat
		{
			public ZBufferFormat( XFG.DepthFormat f )
			{
				this.format = f;
			}
			public XFG.DepthFormat format;
		}

		//public struct ZBufferFormat
		//{
		//    public ZBufferFormat( XFG.DepthFormat f, XFG.MultiSampleType m )
		//    {
		//        this.format = f;
		//        this.multisample = m;
		//    }
		//    public XFG.DepthFormat format;
		//    public XFG.MultiSampleType multisample;
		//}
		//protected Dictionary<ZBufferFormat, XFG.DepthStencilBuffer> zBufferCache = new Dictionary<ZBufferFormat, XFG.DepthStencilBuffer>();
		//protected Dictionary<XFG.SurfaceFormat, XFG.DepthFormat> depthStencilCache = new Dictionary<XFG.SurfaceFormat, XFG.DepthFormat>();

		#endregion Inner Types

		#region Fields

		/// <summary>
		///    Reference to the Xna device.
		/// </summary>
		private XFG.GraphicsDevice _device;
		private Driver _activeDriver;

		//private XFG.GraphicsDeviceCapabilities _capabilities;
		/// Saved last view matrix
		protected Matrix4 _viewMatrix = Matrix4.Identity;
		bool _isFirstFrame = true;
		private int _primCount;
		private int _renderCount = 0;
		XFG.BasicEffect basicEffect;
		XFG.SkinnedEffect skinnedEffect;
		XFG.Effect effect;

		/// <summary>
		/// The one used to create the device.
		/// </summary>
		private XnaRenderWindow _primaryWindow;

		List<XnaRenderWindow> _secondaryWindows = new List<XnaRenderWindow>();

		// stores texture stage info locally for convenience
		internal XnaTextureStageDescription[] texStageDesc = new XnaTextureStageDescription[ Config.MaxTextureLayers ];
		protected XnaGpuProgramManager gpuProgramMgr;
		int numLastStreams = 0;

		/// <summary>
		///    Number of streams used last frame, used to unbind any buffers not used during the current operation.
		/// </summary>
		private int _lastVertexSourceCount;

		// Fixed Function Emulation
#if AXIOM_FF_EMULATION
		FixedFunctionEmulation.ShaderManager _shaderManager = new FixedFunctionEmulation.ShaderManager();
		FixedFunctionEmulation.HLSLShaderGenerator _hlslShaderGenerator = new FixedFunctionEmulation.HLSLShaderGenerator();
		FixedFunctionEmulation.FixedFunctionState _fixedFunctionState = new FixedFunctionEmulation.FixedFunctionState();
		FixedFunctionEmulation.HLSLFixedFunctionProgram _fixedFunctionProgram;//= new Axiom.RenderSystems.Xna.FixedFunctionEmulation.HLSLFixedFunctionProgram();
		FixedFunctionEmulation.FixedFunctionPrograms.FixedFunctionProgramsParameters _ffProgramParameters = new FixedFunctionEmulation.FixedFunctionPrograms.FixedFunctionProgramsParameters();
#endif
		private bool _useNVPerfHUD;
		private bool _vSync;
		//private XFG.MultiSampleType _fsaaType = XFG.MultiSampleType.None;
		private int _fsaaQuality = 0;
		private XFG.RasterizerState _rasterizerState = new XFG.RasterizerState();

		protected int primCount;
		// protected int renderCount = 0;

		bool VertexShaderIsSet = false;
		bool PixelShaderIsSet = false;
		bool useSkinnedEffect = false;

		bool needToUnmapVS = false;
		bool needToUnmapFS = false;

		private bool lasta2c = false;

		protected Dictionary<ZBufferFormat, XFG.RenderTarget2D> zBufferCache = new Dictionary<ZBufferFormat, XFG.RenderTarget2D>();
		//protected Dictionary<ZBufferFormat, XFG.DepthStencilBuffer> zBufferCache = new Dictionary<ZBufferFormat, XFG.DepthStencilBuffer>();
		protected Dictionary<XFG.SurfaceFormat, XFG.DepthFormat> depthStencilCache = new Dictionary<XFG.SurfaceFormat, XFG.DepthFormat>();

		private static XFG.DepthFormat[] _preferredStencilFormats = {
			//XFG.DepthFormat.Depth24Stencil8Single,
			XFG.DepthFormat.Depth24Stencil8,
			//XFG.DepthFormat.Depth24Stencil4,
			XFG.DepthFormat.Depth24,
			//XFG.DepthFormat.Depth15Stencil1,
			XFG.DepthFormat.Depth16,
			//XFG.DepthFormat.Depth32
		};

		private StateManagement StateManager;

		#endregion Fields

		#region Construction and Destruction

		public XnaRenderSystem()
			: base()
		{
			_initConfigOptions();
			// init the texture stage descriptions
			for ( int i = 0; i < Config.MaxTextureLayers; i++ )
			{
				texStageDesc[ i ].autoTexCoordType = TexCoordCalcMethod.None;
				texStageDesc[ i ].coordIndex = 0;
				texStageDesc[ i ].texType = TextureType.OneD;
				texStageDesc[ i ].tex = null;
			}
#if AXIOM_FF_EMULATION
			_shaderManager.RegisterGenerator( _hlslShaderGenerator );
#endif
		}

		#endregion Construction and Destruction

		#region Helper Methods

		private void _setVertexBufferBinding( VertexBufferBinding binding )
		{
			Dictionary<short, HardwareVertexBuffer> bindings = binding.Bindings;

			var xnaBindings = new XFG.VertexBufferBinding[ binding.BindingCount ];
			int index = 0;
			foreach ( short stream in bindings.Keys )
			{
				XnaHardwareVertexBuffer buffer = (XnaHardwareVertexBuffer)bindings[ stream ];
				xnaBindings[ index++ ] = new XFG.VertexBufferBinding( buffer.XnaVertexBuffer );
			}

			_device.SetVertexBuffers( xnaBindings );
		}

		public override void SetConfigOption( string name, string value )
		{
			if ( ConfigOptions.ContainsKey( name ) )
				ConfigOptions[ name ].Value = value;

		}

		private void _configOptionChanged( string name, string value )
		{
			LogManager.Instance.Write( "XNA : RenderSystem Option: {0} = {1}", name, value );

			bool viewModeChanged = false;

			// Find option
			ConfigOption opt = ConfigOptions[ name ];

			// Refresh other options if D3DDriver changed
			if ( name == "Rendering Device" )
				_refreshXnaSettings();

			if ( name == "Full Screen" )
			{
				// Video mode is applicable
				opt = ConfigOptions[ "Video Mode" ];
				if ( opt.Value == "" )
				{
					opt.Value = "800 x 600 @ 32-bit color";
					viewModeChanged = true;
				}
			}
			/*
						if ( name == "Anti aliasing" )
						{
							if ( value == "None" )
							{
								_setFSAA( XFG.MultiSampleType.None, 0 );
							}
							else
							{
								XFG.MultiSampleType fsaa = XFG.MultiSampleType.None;
								int level = 0;

								if ( value.StartsWith( "NonMaskable" ) )
								{
									fsaa = XFG.MultiSampleType.NonMaskable;
									level = Int32.Parse( value.Substring( value.LastIndexOf( " " ) ) );
									level -= 1;
								}
								else if ( value.StartsWith( "Level" ) )
								{
									fsaa = (XFG.MultiSampleType)Int32.Parse( value.Substring( value.LastIndexOf( " " ) ) );
								}

								_setFSAA( fsaa, level );
							}
 
						}
			*/
			if ( name == "VSync" )
			{
				_vSync = ( value == "Yes" );
			}

			if ( name == "Allow NVPerfHUD" )
			{
				_useNVPerfHUD = ( value == "Yes" );
			}

			if ( viewModeChanged || name == "Video Mode" )
			{
				_refreshFSAAOptions();
			}

		}
		/*
				private void _setFSAA( XFG.MultiSampleType fsaa, int level )
				{
					if ( _device == null )
					{
						_fsaaType = fsaa;
						_fsaaQuality = level;
					}
				}
		*/
		private void _initConfigOptions()
		{
			ConfigOption optDevice = new ConfigOption( "Rendering Device", "", false );
			ConfigOption optVideoMode = new ConfigOption( "Video Mode", "800 x 600 @ 32-bit color", false );
			ConfigOption optFullScreen = new ConfigOption( "Full Screen", "No", false );
			ConfigOption optVSync = new ConfigOption( "VSync", "No", false );
			ConfigOption optAA = new ConfigOption( "Anti aliasing", "None", false );
			ConfigOption optFPUMode = new ConfigOption( "Floating-point mode", "Fastest", false );
			ConfigOption optNVPerfHUD = new ConfigOption( "Allow NVPerfHUD", "No", false );
			ConfigOption optSaveShaders = new ConfigOption( "Save Generated Shaders", "No", false );
			ConfigOption optUseCP = new ConfigOption( "Use Content Pipeline", "No", false );

			optDevice.PossibleValues.Clear();

			DriverCollection driverList = XnaHelper.GetDriverInfo();
			foreach ( Driver driver in driverList )
			{
				if ( !optDevice.PossibleValues.ContainsKey( driver.AdapterNumber ) )
				{
					optDevice.PossibleValues.Add( driver.AdapterNumber, driver.Description );
				}
			}
			optDevice.Value = driverList[ 0 ].Description;

			optFullScreen.PossibleValues.Add( 0, "Yes" );
			optFullScreen.PossibleValues.Add( 1, "No" );

			optVSync.PossibleValues.Add( 0, "Yes" );
			optVSync.PossibleValues.Add( 1, "No" );

			optAA.PossibleValues.Add( 0, "None" );

			optFPUMode.PossibleValues.Clear();
			optFPUMode.PossibleValues.Add( 0, "Fastest" );
			optFPUMode.PossibleValues.Add( 1, "Consistent" );

			optNVPerfHUD.PossibleValues.Add( 0, "Yes" );
			optNVPerfHUD.PossibleValues.Add( 1, "No" );

			optSaveShaders.PossibleValues.Add( 0, "Yes" );
			optSaveShaders.PossibleValues.Add( 1, "No" );

			optUseCP.PossibleValues.Add( 0, "Yes" );
			optUseCP.PossibleValues.Add( 1, "No" );

			optFPUMode.ConfigValueChanged += new ConfigOption.ValueChanged( _configOptionChanged );
			optAA.ConfigValueChanged += new ConfigOption.ValueChanged( _configOptionChanged );
			optVSync.ConfigValueChanged += new ConfigOption.ValueChanged( _configOptionChanged );
			optFullScreen.ConfigValueChanged += new ConfigOption.ValueChanged( _configOptionChanged );
			optVideoMode.ConfigValueChanged += new ConfigOption.ValueChanged( _configOptionChanged );
			optDevice.ConfigValueChanged += new ConfigOption.ValueChanged( _configOptionChanged );
			optNVPerfHUD.ConfigValueChanged += new ConfigOption.ValueChanged( _configOptionChanged );
			optSaveShaders.ConfigValueChanged += new ConfigOption.ValueChanged( _configOptionChanged );
			optUseCP.ConfigValueChanged += new ConfigOption.ValueChanged( _configOptionChanged );

			ConfigOptions.Add( optDevice );
			ConfigOptions.Add( optVideoMode );
			ConfigOptions.Add( optFullScreen );
			ConfigOptions.Add( optVSync );
			ConfigOptions.Add( optAA );
			ConfigOptions.Add( optFPUMode );
			ConfigOptions.Add( optNVPerfHUD );
			ConfigOptions.Add( optSaveShaders );
			ConfigOptions.Add( optUseCP );

			_refreshXnaSettings();
		}

		private void _refreshXnaSettings()
		{
			DriverCollection drivers = XnaHelper.GetDriverInfo();

			ConfigOption optDevice = ConfigOptions[ "Rendering Device" ];
			Driver driver = drivers[ optDevice.Value ];
			if ( driver != null )
			{
				// Get Current Selection
				ConfigOption optVideoMode = ConfigOptions[ "Video Mode" ];
				string curMode = optVideoMode.Value;

				// Clear previous Modes
				optVideoMode.PossibleValues.Clear();

				// Get Video Modes for current device;
				foreach ( VideoMode videoMode in driver.VideoModes )
				{
					optVideoMode.PossibleValues.Add( optVideoMode.PossibleValues.Count, videoMode.ToString() );
				}

				// Reset video mode to default if previous doesn't avail in new possible values

				if ( optVideoMode.PossibleValues.Values.Contains( curMode ) == false )
				{
					optVideoMode.Value = "800 x 600 @ 32-bit color";
				}

				// Also refresh FSAA options
				_refreshFSAAOptions();
			}
		}

		private void _refreshFSAAOptions()
		{
			return;
			/*
			// Reset FSAA Options
			ConfigOption optFSAA = ConfigOptions[ "Anti aliasing" ];
			string curFSAA = optFSAA.Value;
			optFSAA.PossibleValues.Clear();
			optFSAA.PossibleValues.Add( 0, "None" );

			ConfigOption optFullScreen = ConfigOptions[ "Full Screen" ];
			bool windowed = optFullScreen.Value != "Yes";

			DriverCollection drivers = XnaHelper.GetDriverInfo();
			ConfigOption optDevice = ConfigOptions[ "Rendering Device" ];
			Driver driver = drivers[ optDevice.Value ];
			if ( driver != null )
			{
				ConfigOption optVideoMode = ConfigOptions[ "Video Mode" ];
				VideoMode videoMode = driver.VideoModes[ optVideoMode.Value ];
				if ( videoMode != null )
				{
					int numLevels = 0;

					// get non maskable levels supported for this VMODE
					if ( driver.Adapter.CheckDeviceMultiSampleType( XFG.DeviceType.Hardware, videoMode.Format, windowed, XFG.MultiSampleType.NonMaskable, out numLevels ) )
					{
						for ( int n = 0; n < numLevels; n++ )
						{
							optFSAA.PossibleValues.Add( optFSAA.PossibleValues.Count, String.Format( "NonMaskable {0}", n ) );
						}
					}


					// get maskable levels supported for this VMODE
					for ( int n = 2; n < 17; n++ )
					{
						if ( driver.Adapter.CheckDeviceMultiSampleType( XFG.DeviceType.Hardware, videoMode.Format, windowed, (XFG.MultiSampleType)n ) )
						{
							optFSAA.PossibleValues.Add( optFSAA.PossibleValues.Count, String.Format( "Level {0}", n ) );
						}
					}
				}
			}
			*/
			// Reset FSAA to none if previous doesn't avail in new possible values
			//if ( optFSAA.PossibleValues.Values.Contains( curFSAA ) == false )
			//{
			//    optFSAA.Value = "None";
			//}
		}

#if !(XBOX || XBOX360 || SILVERLIGHT)
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

			form.ClientSize = new System.Drawing.Size( width, height );
			form.MaximizeBox = false;
			form.MinimizeBox = false;
			form.StartPosition = FormStartPosition.CenterScreen;

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
#endif

		/// <summary>
		///		Helper method to go through and interrogate hardware capabilities.
		/// </summary>
		private void _checkHardwareCapabilities( XFG.GraphicsProfile profile )
		{
			_setCapabilitiesForAllProfiles();

			if ( profile == XFG.GraphicsProfile.HiDef )
			{
				_setCapabilitiesForHiDefProfile();
			}
			else if ( profile == XFG.GraphicsProfile.Reach )
			{
				_setCapabilitiesForReachProfile();
			}
		}

		private void _setCapabilitiesForAllProfiles()
		{
			// Texture Compression
			// We always support compression, Xna will decompress if device does not support
			HardwareCapabilities.SetCapability( Capabilities.TextureCompression );
			HardwareCapabilities.SetCapability( Capabilities.TextureCompressionDXT );

			// Xna uses vertex buffers for everything
			HardwareCapabilities.SetCapability( Capabilities.VertexBuffer );

			// blending between stages is definitely supported
			HardwareCapabilities.SetCapability( Capabilities.TextureBlending );
			HardwareCapabilities.SetCapability( Capabilities.MultiTexturing );

		}

		private void _setCapabilitiesForHiDefProfile()
		{
			// Fill in the HiDef profile requirements.

			HardwareCapabilities.SetCapability( Capabilities.HardwareOcculusion );

			//VertexShaderVersion = 0x300;
			HardwareCapabilities.SetCapability( Capabilities.VertexPrograms );
			HardwareCapabilities.MaxVertexProgramVersion = "vs_3_0";
			HardwareCapabilities.VertexProgramConstantIntCount = 16 * 4;
			HardwareCapabilities.VertexProgramConstantFloatCount = 256;
			this.gpuProgramMgr.PushSyntaxCode( "vs_1_1" );
			this.gpuProgramMgr.PushSyntaxCode( "vs_2_0" );
			this.gpuProgramMgr.PushSyntaxCode( "vs_2_x" );
			this.gpuProgramMgr.PushSyntaxCode( "vs_3_0" );

			//PixelShaderVersion = 0x300;
			HardwareCapabilities.SetCapability( Capabilities.FragmentPrograms );
			HardwareCapabilities.MaxFragmentProgramVersion = "ps_3_0";
			HardwareCapabilities.FragmentProgramConstantIntCount = 16;
			HardwareCapabilities.FragmentProgramConstantFloatCount = 224;
			this.gpuProgramMgr.PushSyntaxCode( "ps_1_1" );
			this.gpuProgramMgr.PushSyntaxCode( "ps_1_2" );
			this.gpuProgramMgr.PushSyntaxCode( "ps_1_3" );
			this.gpuProgramMgr.PushSyntaxCode( "ps_1_4" );
			this.gpuProgramMgr.PushSyntaxCode( "ps_2_0" );
			this.gpuProgramMgr.PushSyntaxCode( "ps_3_0" );

			//SeparateAlphaBlend = true;
			HardwareCapabilities.SetCapability( Capabilities.AdvancedBlendOperations );
			//DestBlendSrcAlphaSat = true;

			//MaxPrimitiveCount = 1048575;
			//IndexElementSize32 = true;
			//MaxVertexStreams = 16;
			//MaxStreamStride = 255;

			//MaxTextureSize = 4096;
			//MaxCubeSize = 4096;
			//MaxVolumeExtent = 256;
			//MaxTextureAspectRatio = 2048;
			//MaxVertexSamplers = 4;
			//MaxRenderTargets = 4;
			HardwareCapabilities.TextureUnitCount = 16;
			HardwareCapabilities.MultiRenderTargetCount = 4;

			//NonPow2Unconditional = true;
			//NonPow2Cube = true;
			//NonPow2Volume = true;

			//ValidTextureFormats       = MakeList(STANDARD_TEXTURE_FORMATS, COMPRESSED_TEXTURE_FORMATS, SIGNED_TEXTURE_FORMATS, HIDEF_TEXTURE_FORMATS, FLOAT_TEXTURE_FORMATS);
			//ValidCubeFormats          = MakeList(STANDARD_TEXTURE_FORMATS, COMPRESSED_TEXTURE_FORMATS, HIDEF_TEXTURE_FORMATS, FLOAT_TEXTURE_FORMATS);
			//ValidVolumeFormats        = MakeList(STANDARD_TEXTURE_FORMATS, HIDEF_TEXTURE_FORMATS, FLOAT_TEXTURE_FORMATS);
			//ValidVertexTextureFormats = MakeList(FLOAT_TEXTURE_FORMATS);
			//InvalidFilterFormats      = MakeList(FLOAT_TEXTURE_FORMATS);
			//InvalidBlendFormats       = MakeList(STANDARD_FLOAT_TEXTURE_FORMATS);
			//ValidVertexFormats        = MakeList(STANDARD_VERTEX_FORMATS, HIDEF_VERTEX_FORMATS);

		}

		private void _setCapabilitiesForReachProfile()
		{
			// Fill in the Reach profile requirements.
			// Texture Compression
			// We always support compression, Xna will decompress if device does not support
			HardwareCapabilities.SetCapability( Capabilities.TextureCompression );
			HardwareCapabilities.SetCapability( Capabilities.TextureCompressionDXT );

			// Xna uses vertex buffers for everything
			HardwareCapabilities.SetCapability( Capabilities.VertexBuffer );


			//VertexShaderVersion = 0x200;
			HardwareCapabilities.SetCapability( Capabilities.VertexPrograms );
			HardwareCapabilities.MaxVertexProgramVersion = "vs_2_0";
			HardwareCapabilities.VertexProgramConstantIntCount = 16 * 4;
			HardwareCapabilities.VertexProgramConstantFloatCount = 256;
			this.gpuProgramMgr.PushSyntaxCode( "vs_1_1" );
			this.gpuProgramMgr.PushSyntaxCode( "vs_2_0" );

			//PixelShaderVersion = 0x200;
			HardwareCapabilities.SetCapability( Capabilities.FragmentPrograms );
			HardwareCapabilities.MaxFragmentProgramVersion = "ps_2_0";
			HardwareCapabilities.FragmentProgramConstantIntCount = 0;
			HardwareCapabilities.FragmentProgramConstantFloatCount = 32;
			this.gpuProgramMgr.PushSyntaxCode( "ps_1_1" );
			this.gpuProgramMgr.PushSyntaxCode( "ps_1_2" );
			this.gpuProgramMgr.PushSyntaxCode( "ps_1_3" );
			this.gpuProgramMgr.PushSyntaxCode( "ps_1_4" );
			this.gpuProgramMgr.PushSyntaxCode( "ps_2_0" );

			//SeparateAlphaBlend = false;
			//DestBlendSrcAlphaSat = false;

			//MaxPrimitiveCount = 65535;
			//IndexElementSize32 = false;
			//MaxVertexStreams = 16;
			//MaxStreamStride = 255;

			//MaxTextureSize = 2048;
			//MaxCubeSize = 512;
			//MaxVolumeExtent = 0;
			//MaxTextureAspectRatio = 2048;
			//MaxVertexSamplers = 0;
			//MaxRenderTargets = 1;
			HardwareCapabilities.MultiRenderTargetCount = 1;

			//NonPow2Unconditional = false;
			//NonPow2Cube = false;
			//NonPow2Volume = false;

			//ValidTextureFormats       = MakeList(STANDARD_TEXTURE_FORMATS, COMPRESSED_TEXTURE_FORMATS, SIGNED_TEXTURE_FORMATS);
			//ValidCubeFormats          = MakeList(STANDARD_TEXTURE_FORMATS, COMPRESSED_TEXTURE_FORMATS);
			//ValidVolumeFormats        = MakeList<SurfaceFormat>();
			//ValidVertexTextureFormats = MakeList<SurfaceFormat>();
			//InvalidFilterFormats      = MakeList<SurfaceFormat>();
			//InvalidBlendFormats       = MakeList<SurfaceFormat>();
			//ValidVertexFormats        = MakeList(STANDARD_VERTEX_FORMATS);
		}

		/*
		private void unusedcode()
		{
			_capabilities = _device.GraphicsDeviceCapabilities;

			// get the number of possible texture units
			HardwareCapabilities.TextureUnitCount = _capabilities.MaxSimultaneousTextures;

			// max active lights
			HardwareCapabilities.MaxLights = 8;

			XFG.RenderTargetBinding rtb = null;

			//KLUDGE to get the first item
			foreach ( var item in _device.GetRenderTargets() )
			{
				rtb = item;
				break;
			}

			XFG.RenderTarget2D surface = (XFG.RenderTarget2D)rtb.RenderTarget;

			if ( surface.DepthStencilFormat == XFG.DepthFormat.Depth24Stencil8 || surface.DepthStencilFormat == XFG.DepthFormat.Depth24 )
			{
				HardwareCapabilities.SetCapability( Capabilities.StencilBuffer );
				// always 8 here
				HardwareCapabilities.StencilBufferBitCount = 8;
			}

			// some cards, oddly enough, do not support this
			if ( _capabilities.DeclarationTypeCapabilities.SupportsByte4 )
			{
				HardwareCapabilities.SetCapability( Capabilities.VertexFormatUByte4 );
			}

			// Anisotropy?
			if ( _capabilities.MaxAnisotropy > 1 )
			{
				HardwareCapabilities.SetCapability( Capabilities.AnisotropicFiltering );
			}

			// Hardware mipmapping?
			if ( _capabilities.DriverCapabilities.CanAutoGenerateMipMap )
			{
				HardwareCapabilities.SetCapability( Capabilities.HardwareMipMaps );
			}


			// Dot3 bump mapping?
			//if ( _capabilities.TextureCapabilities.SupportsDotProduct3 )
			//{
			//    HardwareCapabilities.SetCap( Capabilities.Dot3 );
			//}

			// Cube mapping?
			if ( _capabilities.TextureCapabilities.SupportsCubeMap )
			{
				HardwareCapabilities.SetCapability( Capabilities.CubeMapping );
			}

			// Scissor test
			if ( _capabilities.RasterCapabilities.SupportsScissorTest )
			{
				HardwareCapabilities.SetCapability( Capabilities.ScissorTest );
			}

			// 2 sided stencil
			if ( _capabilities.StencilCapabilities.SupportsTwoSided )
			{
				HardwareCapabilities.SetCapability( Capabilities.TwoSidedStencil );
			}

			// stencil wrap
			if ( _capabilities.StencilCapabilities.SupportsIncrement && _capabilities.StencilCapabilities.SupportsDecrement )
			{
				HardwareCapabilities.SetCapability( Capabilities.StencilWrap );
			}


			if ( _capabilities.MaxUserClipPlanes > 0 )
			{
				HardwareCapabilities.SetCapability( Capabilities.UserClipPlanes );
			}

			// Infinite projection?
			// We have no capability for this, so we have to base this on our
			// experience and reports from users
			// Non-vertex program capable hardware does not appear to support it
			if ( HardwareCapabilities.HasCapability( Capabilities.VertexPrograms ) )
			{
				// GeForce4 Ti (and presumably GeForce3) does not
				// render infinite projection properly, even though it does in GL
				// So exclude all cards prior to the FX range from doing infinite

				XFG.GraphicsAdapter details = XFG.GraphicsAdapter.Adapters[ 0 ];

				// not nVidia or GeForceFX and above
				if ( details.VendorId != 0x10DE || details.DeviceId >= 0x0301 )
				{
					HardwareCapabilities.SetCapability( Capabilities.InfiniteFarPlane );
				}
			}

			// write hardware capabilities to registered log listeners
			HardwareCapabilities.Log();
		}
		*/
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

		#region Axiom.Core.RenderSystem Implementation

		#region Properties

		#region RenderTarget

		/// <summary>
		/// Set current render target to target, enabling its device context if needed
		/// </summary>
		public override RenderTarget RenderTarget
		{
			set { throw new NotImplementedException(); }
		}

		#endregion

		#region PointSpritesEnabled

		/// <summary>
		/// Sets whether or not rendering points using OT_POINT_LIST will 
		/// render point sprites (textured quads) or plain points.
		/// </summary>
		public override bool PointSpritesEnabled
		{
			set
			{
				//if ( value )
				//    throw new AxiomException( "XNA does not support PointSprites." );
			}
		}

		#endregion

		#region CullingMode

		/// <summary>
		/// Gets/Sets the culling mode for the render system based on the 'vertex winding'.
		/// </summary>
		/// <remarks>
		/// A typical way for the rendering engine to cull triangles is based on the
		/// 'vertex winding' of triangles. Vertex winding refers to the direction in
		/// which the vertices are passed or indexed to in the rendering operation as viewed
		/// from the camera, and will wither be clockwise or counterclockwise.  The default is <see name="CullingMode.Clockwise"/>  
		/// i.e. that only triangles whose vertices are passed/indexed in counterclockwise order are rendered - this 
		/// is a common approach and is used in 3D studio models for example. You can alter this culling mode 
		/// if you wish but it is not advised unless you know what you are doing. You may wish to use the 
		/// <see cref="Graphics.CullingMode.None"/> option for mesh data that you cull yourself where the vertex winding is uncertain.
		/// </remarks>
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
				StateManager.RasterizerState.CullMode = XnaHelper.Convert( value, flip );
			}
		}

		#endregion

		#region DepthBufferCheckEnabled

		/// <summary>
		/// Sets whether or not the depth buffer check is performed before a pixel write
		/// </summary>
		public override bool DepthBufferCheckEnabled
		{
			set
			{
				StateManager.DepthStencilState.DepthBufferEnable = value;
			}
		}

		#endregion

		#region DepthBufferWriteEnabled

		/// <summary>
		/// Sets whether or not the depth buffer is updated after a pixel write.
		/// </summary>
		public override bool DepthBufferWriteEnabled
		{
			set
			{
				StateManager.DepthStencilState.DepthBufferWriteEnable = value;
			}
		}

		#endregion

		#region DepthBufferFunction

		/// <summary>
		/// Sets the comparison function for the depth buffer check.
		/// Advanced use only - allows you to choose the function applied to compare the depth values of
		/// new and existing pixels in the depth buffer. Only an issue if the depth buffer check is enabled
		/// <see cref="DepthBufferCheckEnabled"/>
		/// </summary>
		public override CompareFunction DepthBufferFunction
		{
			set
			{
				StateManager.DepthStencilState.DepthBufferFunction = XnaHelper.Convert( value );
			}
		}

		#endregion

		#region ColorVertexElementType

		public override VertexElementType ColorVertexElementType
		{
			get { throw new NotImplementedException(); }
		}

		#endregion

		#region VertexDeclaration

		/// <summary>
		/// Sets the current vertex declaration, ie the source of vertex data.
		/// </summary>
		public override VertexDeclaration VertexDeclaration
		{
			set { throw new NotImplementedException(); }
		}

		#endregion

		#region VertexBufferBinding

		/// <summary>
		/// Sets the current vertex buffer binding state.
		/// </summary>
		public override VertexBufferBinding VertexBufferBinding
		{
			set { throw new NotImplementedException(); }
		}

		#endregion

		#region DisplayMonitorCount

		/// <summary>
		/// Gets the number of display monitors. <see name="Root.DisplayMonitorCount"/>
		/// </summary>
		public override int DisplayMonitorCount
		{
			get { throw new NotImplementedException(); }
		}

		#endregion

		#region AmbientLight

		private ColorEx _ambientLight = ColorEx.White;

		/// <summary>
		/// Sets the color &amp; strength of the ambient (global directionless) light in the world.
		/// </summary>
		public override ColorEx AmbientLight
		{
			set
			{
				_ambientLight = value;
				basicEffect.AmbientLightColor = XnaHelper.Convert( _ambientLight ).ToVector3();
				skinnedEffect.AmbientLightColor = basicEffect.AmbientLightColor;
#if AXIOM_FF_EMULATION
				_ffProgramParameters.LightAmbient = value;
#endif
			}
		}

		#endregion

		#region ShadingType

		/// <summary>
		/// Sets the type of light shading required (default = Gouraud).
		/// </summary>
		public override ShadeOptions ShadingType
		{
			set { throw new NotImplementedException(); }
		}

		#endregion

		#region HorizontalTexelOffset

		/// <summary>
		/// Returns the horizontal texel offset value required for mapping 
		/// texel origins to pixel origins in this rendersystem.
		/// </summary>
		/// <remarks>
		/// Since rendersystems sometimes disagree on the origin of a texel, 
		/// mapping from texels to pixels can sometimes be problematic to 
		/// implement generically. This method allows you to retrieve the offset
		/// required to map the origin of a texel to the origin of a pixel in
		/// the horizontal direction.
		/// </remarks>
		public override Real HorizontalTexelOffset
		{
			// Xna considers the origin to be in the center of a pixel?
			get
			{
				return -0.5f;
			}
		}

		#endregion

		#region LightingEnabled

		private bool _lightingEnabled;

		/// <summary>
		/// Sets whether or not dynamic lighting is enabled.
		/// <p/>
		/// If true, dynamic lighting is performed on geometry with normals supplied, geometry without
		/// normals will not be displayed. If false, no lighting is applied and all geometry will be full brightness.
		/// </summary>
		public override bool LightingEnabled
		{
			set
			{
				_lightingEnabled = value;
				basicEffect.LightingEnabled = _lightingEnabled;
#if AXIOM_FF_EMULATION
				_ffProgramParameters.LightingEnabled = value;
#endif
			}
		}

		#endregion

		#region NormalizeNormals

		/// <summary>
		/// Sets whether or not normals are to be automatically normalized.
		/// </summary>
		/// <remarks>
		/// This is useful when, for example, you are scaling SceneNodes such that
		/// normals may not be unit-length anymore. Note though that this has an
		/// overhead so should not be turn on unless you really need it.
		/// <p/>
		/// You should not normally call this direct unless you are rendering
		/// world geometry; set it on the Renderable because otherwise it will be
		/// overridden by material settings. 
		/// </remarks>
		public override bool NormalizeNormals
		{
			set
			{
				throw new NotImplementedException();
			}
		}

		#endregion

		#region ProjectionMatrix

		private Matrix4 _projectionMatrix;

		/// <summary>
		/// Sets the current projection matrix.
		/// </summary>
		public override Matrix4 ProjectionMatrix
		{
			set
			{
				_projectionMatrix = value;

				basicEffect.Projection = XnaHelper.Convert( _projectionMatrix );
				skinnedEffect.Projection = basicEffect.Projection;
#if AXIOM_FF_EMULATION
				_ffProgramParameters.ProjectionMatrix = value;
#endif

			}
		}

		#endregion

		#region PolygonMode

		/// <summary>
		/// Sets how to rasterise triangles, as points, wireframe or solid polys.
		/// </summary>
		public override PolygonMode PolygonMode
		{
			set
			{
				switch ( value )
				{
					case PolygonMode.Points:
						StateManager.RasterizerState.FillMode = XFG.FillMode.WireFrame;
						break;

					case PolygonMode.Wireframe:
						StateManager.RasterizerState.FillMode = XFG.FillMode.WireFrame;
						break;

					case PolygonMode.Solid:
						StateManager.RasterizerState.FillMode = XFG.FillMode.Solid;
						break;
				}
			}
		}

		#endregion

		#region StencilCheckEnabled

		/// <summary>
		/// Turns stencil buffer checking on or off. 
		/// </summary>
		/// <remarks>
		/// Stencilling (masking off areas of the rendering target based on the stencil 
		/// buffer) can be turned on or off using this method. By default, stencilling is
		/// disabled.
		/// </remarks>
		public override bool StencilCheckEnabled
		{
			set
			{
				StateManager.DepthStencilState.StencilEnable = value;
			}
		}

		#endregion

		#region VerticalTexelOffset

		/// <summary>
		/// Returns the vertical texel offset value required for mapping 
		/// texel origins to pixel origins in this rendersystem.
		/// </summary>
		/// <remarks>
		/// Since rendersystems sometimes disagree on the origin of a texel, 
		/// mapping from texels to pixels can sometimes be problematic to 
		/// implement generically. This method allows you to retrieve the offset
		/// required to map the origin of a texel to the origin of a pixel in
		/// the vertical direction.
		/// </remarks>
		public override Real VerticalTexelOffset
		{
			get
			{
				// Xna considers the origin to be in the center of a pixel ?
				return -0.5f;
			}
		}

		#endregion

		#region ViewMatrix

		/// <summary>
		/// Sets the current view matrix.
		/// </summary>
		public override Matrix4 ViewMatrix
		{
			set
			{
				// flip the transform portion of the matrix for DX and its left-handed coord system
				// save latest view matrix
				_viewMatrix = value;
				_viewMatrix.m20 = -_viewMatrix.m20;
				_viewMatrix.m21 = -_viewMatrix.m21;
				_viewMatrix.m22 = -_viewMatrix.m22;
				_viewMatrix.m23 = -_viewMatrix.m23;

				basicEffect.View = XnaHelper.Convert( _viewMatrix );
				skinnedEffect.View = basicEffect.View;

#if AXIOM_FF_EMULATION
				_ffProgramParameters.ViewMatrix = _viewMatrix;
#endif
			}
		}

		#endregion

		#region WorldMatrix

		private Matrix4 _worldMatrix;

		/// <summary>
		/// Sets the current world matrix.
		/// </summary>
		public override Axiom.Math.Matrix4 WorldMatrix
		{
			get
			{
				return _worldMatrix;
			}
			set
			{
				_worldMatrix = value;
				basicEffect.World = XnaHelper.Convert( _worldMatrix );
				basicEffect.World = basicEffect.World;
#if AXIOM_FF_EMULATION
				_ffProgramParameters.WorldMatrix = _worldMatrix;
#endif
			}
		}

		#endregion

		#region MinimumDepthInputValue

		/// <summary>
		/// Gets the maximum (closest) depth value to be used when rendering using identity transforms.
		/// </summary>
		/// <remarks>
		/// When using identity transforms you can manually set the depth
		/// of a vertex; however the input values required differ per
		/// rendersystem. This method lets you retrieve the correct value.
		/// <see cref="SimpleRenderable.UseIdentityView"/>
		/// <see cref="SimpleRenderable.UseIdentityProjection"/>
		/// </remarks>
		public override Real MinimumDepthInputValue
		{
			get
			{
				// Range [0.0f, 1.0f]
				return 0.0f;
			}
		}

		#endregion

		#region MaximumDepthInputValue

		/// <summary>
		/// Gets the maximum (farthest) depth value to be used when rendering using identity transforms.
		/// </summary>
		/// <remarks>
		/// When using identity transforms you can manually set the depth
		/// of a vertex; however the input values required differ per
		/// rendersystem. This method lets you retrieve the correct value.
		/// <see cref="SimpleRenderable.UseIdentityView"/>
		/// <see cref="SimpleRenderable.UseIdentityProjection"/>
		/// </remarks>
		public override Real MaximumDepthInputValue
		{
			get
			{
				// Range [0.0f, 1.0f]
				// XNA inverts even identity view matrixes so maximum INPUT is -1.0f
				return -1.0f;
			}
		}

		#endregion

		#endregion Properties

		#region Methods

		#region PreExtraThreadsStarted

		/// <summary>
		/// Tell the rendersystem to perform any prep tasks it needs to directly
		/// before other threads which might access the rendering API are registered.
		/// </summary>
		/// <remarks>
		/// Call this from your main thread before starting your other threads
		/// (which themselves should call registerThread()). Note that if you
		/// start your own threads, there is a specific startup sequence which 
		/// must be respected and requires synchronisation between the threads:
		/// <ol>
		/// <li>[Main thread]Call <see cref="PreExtraThreadsStarted"/></li>
		/// <li>[Main thread]Start other thread, wait</li>
		/// <li>[Other thread]Call <see cref="RegisterThread"/>, notify main thread &amp; continue</li>
		/// <li>[Main thread]Wake up &amp; call <see cref="PostExtraThreadsStarted"/></li>
		/// </ol>
		/// Once this init sequence is completed the threads are independent but
		/// this startup sequence must be respected.
		/// </remarks>
		public override void PreExtraThreadsStarted()
		{
			throw new NotImplementedException();
		}

		#endregion

		#region PostExtraThreadsStarted

		/// <summary>
		/// Tell the rendersystem to perform any tasks it needs to directly
		/// after other threads which might access the rendering API are registered.
		/// <see cref="PreExtraThreadsStarted"/>
		/// </summary>
		public override void PostExtraThreadsStarted()
		{
			throw new NotImplementedException();
		}

		#endregion

		#region RegisterThread

		/// <summary>
		/// Register the an additional thread which may make calls to rendersystem-related objects.
		/// </summary>
		/// <remarks>
		/// This method should only be called by additional threads during their
		/// initialisation. If they intend to use hardware rendering system resources 
		/// they should call this method before doing anything related to the render system.
		/// Some rendering APIs require a per-thread setup and this method will sort that
		/// out. It is also necessary to call unregisterThread before the thread shuts down.
		/// </remarks>
		/// <note>
		/// This method takes no parameters - it must be called from the thread being
		/// registered and that context is enough.
		/// </note>        
		public override void RegisterThread()
		{
			throw new NotImplementedException();
		}

		#endregion

		#region UnregisterThread

		/// <summary>
		/// Unregister an additional thread which may make calls to rendersystem-related objects.
		/// <see cref="RegisterThread"/>
		/// </summary>
		public override void UnregisterThread()
		{
			throw new NotImplementedException();
		}

		#endregion

		#region SetDepthBias

		/// <summary>
		/// Sets the depth bias, NB you should use the Material version of this.
		/// </summary>
		/// <param name="constantBias"></param>
		/// <param name="slopeScaleBias"></param>
		public override void SetDepthBias( float constantBias, float slopeScaleBias )
		{
			//throw new Exception( "The method or operation is not implemented." );
			//StateManager.DepthStencilState.DepthBias = (float)value;
		}

		#endregion

		#region ValidateConfigOptions

		/// <summary>
		/// Validates the configuration of the rendering system
		/// </summary>
		/// <remarks>Calling this method can cause the rendering system to modify the ConfigOptions collection.</remarks>
		/// <returns>Error message is configuration is invalid <see cref="String.Empty"/> if valid.</returns>
		public override string ValidateConfigOptions()
		{
			throw new NotImplementedException();
		}

		#endregion

		#region GetErrorDescription

		/// <summary>
		/// Returns a description of an error code.
		/// </summary>
		public override string GetErrorDescription( int errorNumber )
		{
			throw new NotImplementedException();
		}

		#endregion

		#region SetClipPlanesImpl

		/// <summary>
		/// Internal method used to set the underlying clip planes when needed
		/// </summary>
		protected override void SetClipPlanesImpl( PlaneList clipPlanes )
		{
			throw new NotImplementedException();
		}

		#endregion

		#region Render

		/// <summary>
		/// Render something to the active viewport.
		/// </summary>
		/// <remarks>
		/// Low-level rendering interface to perform rendering
		/// operations. Unlikely to be used directly by client
		/// applications, since the <see cref="SceneManager"/> and various support
		/// classes will be responsible for calling this method.
		/// Can only be called between BeginScene and EndScene
		/// </remarks>
		/// <param name="op">
		/// A rendering operation instance, which contains details of the operation to be performed.
		/// </param>
		public override void Render( RenderOperation op )
		{
			//StateManager.RasterizerState.FillMode = XFG.FillMode.Solid;
			StateManager.CommitState( _device );
			StateManager.ResetState( _device );

			XFG.Effect effectToUse;

			if ( useSkinnedEffect )
			{
				XNA.Matrix[] boneMatrices = new XNA.Matrix[ Root.Instance.SceneManager.AutoParamData.WorldMatrixCount ];
				for ( int i = 0; i < Root.Instance.SceneManager.AutoParamData.WorldMatrixCount; i++ )
				{
#if!(XBOX || XBOX360)
					boneMatrices[i] = XnaHelper.Convert(Root.Instance.SceneManager.AutoParamData.WorldMatrixArray[i]);
#else
					Matrix4 matrix = Root.Instance.SceneManager.AutoParamData.WorldMatrixArray[ i ];
					boneMatrices[ i ] = XnaHelper.Convert( matrix );
#endif
				}
				skinnedEffect.SetBoneTransforms( boneMatrices );
				effectToUse = skinnedEffect;

			}
			else
			{
				basicEffect.VertexColorEnabled = op.vertexData.vertexDeclaration.FindElementBySemantic( VertexElementSemantic.Diffuse ) != null;
				effectToUse = basicEffect;
			}

			VertexElement ve = op.vertexData.vertexDeclaration.FindElementBySemantic( VertexElementSemantic.Normal );
			if ( ve != null ) //this operation has Normals
			{
				basicEffect.LightingEnabled = false; //turn off lighting
			}
			effectToUse.CurrentTechnique.Passes[ 0 ].Apply();
			XFG.DualTextureEffect dualTextureEffect;

			// don't even bother if there are no vertices to render, causes problems on some cards (FireGL 8800)
			if ( op.vertexData.vertexCount == 0 )
			{
				return;
			}

			// class base implementation first
			base.Render( op );


			/*---------------shaders generator part------*/
#if AXIOM_FF_EMULATION

			if ( Root.Instance.RenderSystem.ConfigOptions[ "Use Content Pipeline" ].Value != "Yes" )
			{

				if ( !VertexShaderIsSet || !PixelShaderIsSet )
				{
					FixedFunctionEmulation.VertexBufferDeclaration vbd = new FixedFunctionEmulation.VertexBufferDeclaration();
					List<FixedFunctionEmulation.VertexBufferElement> lvbe = new List<FixedFunctionEmulation.VertexBufferElement>( op.vertexData.vertexDeclaration.ElementCount );

					int textureLayer = 0;
					for ( int i = 0; i < op.vertexData.vertexDeclaration.ElementCount; i++ )
					{
						FixedFunctionEmulation.VertexBufferElement element = new FixedFunctionEmulation.VertexBufferElement();

						element.VertexElementIndex = (ushort)op.vertexData.vertexDeclaration[ i ].Index;
						element.VertexElementSemantic = op.vertexData.vertexDeclaration[ i ].Semantic;
						element.VertexElementType = op.vertexData.vertexDeclaration[ i ].Type;

						//uncomment this to see the texture shadow
						//the problem is that some texcoords are given but texture is not set
						//
						/*if (//op.vertexData.vertexDeclaration[i].Type == VertexElementType.Float1 &&
							op.vertexData.vertexDeclaration[ i ].Semantic == VertexElementSemantic.TexCoords )
						{
							if ( !texStageDesc[ textureLayer ].Enabled )
							{

								texStageDesc[ textureLayer ].layerBlendMode = new LayerBlendModeEx();
								texStageDesc[ textureLayer ].layerBlendMode.blendType = LayerBlendType.Color;
								texStageDesc[ textureLayer ].layerBlendMode.operation = LayerBlendOperationEx.Modulate;
								texStageDesc[ textureLayer ].layerBlendMode.source1 = LayerBlendSource.Texture;
								texStageDesc[ textureLayer ].layerBlendMode.source2 = LayerBlendSource.Current;

								texStageDesc[ textureLayer ].Enabled = true;
								//texStageDesc[ textureLayer ].autoTexCoordType = TexCoordCalcMethod.ProjectiveTexture;
								texStageDesc[ textureLayer ].coordIndex = textureLayer;
								switch ( op.vertexData.vertexDeclaration[ i ].Type )
								{
									case VertexElementType.Float1:
										texStageDesc[ textureLayer ].texType = TextureType.OneD;
										break;
									case VertexElementType.Float2:
										texStageDesc[ textureLayer ].texType = TextureType.TwoD;
										break;
									case VertexElementType.Float3:
										texStageDesc[ textureLayer ].texType = TextureType.ThreeD;
										break;
								}
								//texStageDesc[textureLayer].layerBlendMode = new LayerBlendModeEx();
							}
							textureLayer++;
						}*/

						lvbe.Add( element );
					}
					vbd.VertexBufferElements = lvbe;


					for ( int i = 0; i < Config.MaxTextureLayers; i++ )
					{
						FixedFunctionEmulation.TextureLayerState tls = new FixedFunctionEmulation.TextureLayerState();

						if ( texStageDesc[ i ].Enabled )
						//if (texStageDesc[i].tex != null)
						{
							tls.TextureType = texStageDesc[ i ].texType;
							tls.TexCoordCalcMethod = texStageDesc[ i ].autoTexCoordType;
							tls.CoordIndex = texStageDesc[ i ].coordIndex;
							tls.LayerBlendMode = texStageDesc[ i ].layerBlendMode;
							//TextureLayerStateList

							_fixedFunctionState.TextureLayerStates.Add( tls );
						}

						FixedFunctionEmulation.GeneralFixedFunctionState gff;
						gff = _fixedFunctionState.GeneralFixedFunctionState;

						gff.EnableLighting = _ffProgramParameters.LightingEnabled;
						gff.FogMode = _ffProgramParameters.FogMode;
						_fixedFunctionState.GeneralFixedFunctionState = gff;

						//lights
						foreach ( Light l in _ffProgramParameters.Lights )
							_fixedFunctionState.Lights.Add( l.Type );


						_fixedFunctionProgram = (FixedFunctionEmulation.HLSLFixedFunctionProgram)_shaderManager.GetShaderPrograms( "hlsl", vbd, _fixedFunctionState );

						_fixedFunctionProgram.FragmentProgramUsage.Program.DefaultParameters.NamedParamCount.ToString();


						_fixedFunctionProgram.SetFixedFunctionProgramParameters( _ffProgramParameters );

						//Bind Vertex Program
						if ( !VertexShaderIsSet )
						{
							BindGpuProgram( _fixedFunctionProgram.VertexProgramUsage.Program.BindingDelegate );
							BindGpuProgramParameters( GpuProgramType.Vertex, _fixedFunctionProgram.VertexProgramUsage.Params );
							needToUnmapVS = true;
						}
						// Bind Fragment Program 
						if ( !PixelShaderIsSet )
						{
							BindGpuProgram( _fixedFunctionProgram.FragmentProgramUsage.Program.BindingDelegate );
							BindGpuProgramParameters( GpuProgramType.Fragment, _fixedFunctionProgram.FragmentProgramUsage.Params );
							needToUnmapFS = true;
						}

						//clear parameters lists for next frame
						_fixedFunctionState.Lights.Clear();
						_fixedFunctionState.TextureLayerStates.Clear();
						//_fixedFunctionState.MaterialEnabled = false; 
						//_ffProgramParameters.FogMode = FogMode.None;


					}
				}
			}
			/*---------------------------------------------------------------------------------------------------------*/
#endif
			XnaVertexDeclaration vertDecl = (XnaVertexDeclaration)op.vertexData.vertexDeclaration;
			// set the vertex declaration and buffer binding 
			//_device.VertexDeclaration = vertDecl.XnaVertexDecl;
			_setVertexBufferBinding( op.vertexData.vertexBufferBinding );

			XFG.PrimitiveType primType = 0;
			switch ( op.operationType )
			{
				case OperationType.PointList:
					primType = XFG.PrimitiveType.LineList; /* XNA 4.0 doesn't support PointList so using LineList instead */
					primCount = op.useIndices ? op.indexData.indexCount : op.vertexData.vertexCount;
					break;

				case OperationType.LineList:
					primType = XFG.PrimitiveType.LineList;
					primCount = ( op.useIndices ? op.indexData.indexCount : op.vertexData.vertexCount ) / 2;
					break;

				case OperationType.LineStrip:
					primType = XFG.PrimitiveType.LineStrip;
					primCount = ( op.useIndices ? op.indexData.indexCount : op.vertexData.vertexCount ) - 1;
					break;

				case OperationType.TriangleList:
					primType = XFG.PrimitiveType.TriangleList;
					primCount = ( op.useIndices ? op.indexData.indexCount : op.vertexData.vertexCount ) / 3;
					break;

				case OperationType.TriangleStrip:
					primType = XFG.PrimitiveType.TriangleStrip;
					primCount = ( op.useIndices ? op.indexData.indexCount : op.vertexData.vertexCount ) - 2;
					break;

				case OperationType.TriangleFan:
					throw new Exception( "XNA 4.0 doesn't support TriangleFan" );
			} // switch(primType)

			try
			{
				// are we gonna use indices?
				if ( op.useIndices )
				{
					XnaHardwareIndexBuffer idxBuffer = (XnaHardwareIndexBuffer)op.indexData.indexBuffer;
					_device.Indices = idxBuffer.XnaIndexBuffer;
					_device.DrawIndexedPrimitives( primType, op.vertexData.vertexStart, 0, op.vertexData.vertexCount, op.indexData.indexStart, primCount );
				}
				else
				{
					// draw vertices without indices
					_device.DrawPrimitives( primType, op.vertexData.vertexStart, primCount );
				}
			}
			catch ( InvalidOperationException ioe )
			{
				LogManager.Instance.Write( "Failed to draw RenderOperation : ", LogManager.BuildExceptionString( ioe ) );
			}
			//crap hack, set the sources back to null to allow accessing vertices and indices buffers
			_device.SetVertexBuffer( null );
			_device.Indices = null;
			_device.Textures[ 0 ] = null;

#if AXIOM_FF_EMULATION
			/*---------------shaders generator part------*/
			if ( needToUnmapVS )
			{
				UnbindGpuProgram( GpuProgramType.Vertex );
			}

			if ( needToUnmapFS )
			{
				UnbindGpuProgram( GpuProgramType.Fragment );
			}
			/*--------------------------------------------*/
#endif
		}

		#endregion

		#region Shutdown

		/// <summary>
		/// Shuts down the RenderSystem.
		/// </summary>
		public override void Shutdown()
		{
			_activeDriver = null;

			// dispose of the device
			if ( _device != null )
			{
				if ( !_device.IsDisposed )
					_device.Dispose();

				_device = null;
			}

			if ( gpuProgramMgr != null )
			{
				if ( !gpuProgramMgr.IsDisposed )
					gpuProgramMgr.Dispose();

				gpuProgramMgr = null;
			}

			if ( hardwareBufferManager != null )
			{
				if ( !hardwareBufferManager.IsDisposed )
					hardwareBufferManager.Dispose();

				hardwareBufferManager = null;
			}

			if ( textureManager != null )
			{
				if ( !textureManager.IsDisposed )
					textureManager.Dispose();

				textureManager = null;
			}

			base.Shutdown();

			LogManager.Instance.Write( "[XNA] : " + Name + " shutdown." );
		}

		#endregion

		#region ApplyObliqueDepthProjection

		/// <summary>
		/// Update a perspective projection matrix to use 'oblique depth projection'.
		/// </summary>
		/// <remarks>
		/// This method can be used to change the nature of a perspective 
		/// transform in order to make the near plane not perpendicular to the 
		/// camera view direction, but to be at some different orientation. 
		/// This can be useful for performing arbitrary clipping (e.g. to a 
		/// reflection plane) which could otherwise only be done using user
		/// clip planes, which are more expensive, and not necessarily supported
		/// on all cards.
		/// </remarks>
		/// <param name="projMatrix">
		/// The existing projection matrix. Note that this must be a
		/// perspective transform (not orthographic), and must not have already
		/// been altered by this method. The matrix will be altered in-place.
		/// </param>
		/// <param name="plane">
		/// The plane which is to be used as the clipping plane. This
		/// plane must be in CAMERA (view) space.
		/// </param>
		/// <param name="forGpuProgram">Is this for use with a Gpu program or fixed-function transforms?</param>
		public override void ApplyObliqueDepthProjection( ref Matrix4 projMatrix, Plane plane, bool forGpuProgram )
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
			Vector4 q = new Vector4();
			q.x = System.Math.Sign( plane.Normal.x ) / projMatrix.m00;
			q.y = System.Math.Sign( plane.Normal.y ) / projMatrix.m11;
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
			Vector4 clipPlane4d = new Vector4( plane.Normal.x, plane.Normal.y, plane.Normal.z, plane.D );
			Vector4 c = clipPlane4d * ( 1.0f / ( clipPlane4d.Dot( q ) ) );

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

		#endregion

		#region BeginFrame

		/// <summary>
		/// Signifies the beginning of a frame, ie the start of rendering on a single viewport. Will occur
		/// several times per complete frame if multiple viewports exist.
		/// </summary>
		public override void BeginFrame()
		{
			Debug.Assert( activeViewport != null, "BeingFrame cannot run without an active viewport." );

			// set initial render states if this is the first frame. we only want to do 
			//	this once since renderstate changes are expensive

			if ( _isFirstFrame )
			{
				// enable alpha blending and specular materials
				var alphaBlend = new ManagedBlendState();
				alphaBlend.Reset( XFG.BlendState.AlphaBlend );
				StateManager.BlendState = alphaBlend;
				//_device.RenderState.SpecularEnable = true;
				var depthRead = new ManagedDepthStencilState();
				depthRead.Reset( XFG.DepthStencilState.DepthRead );
				StateManager.DepthStencilState = depthRead;

				var raster = new ManagedRasterizerState();
				raster.Reset( XFG.RasterizerState.CullClockwise );
				raster.FillMode = XFG.FillMode.Solid;
				StateManager.RasterizerState = raster;

				_isFirstFrame = false;
			}
		}

		#endregion

		#region BindGpuProgram

		/// <summary>
		/// Binds a given GpuProgram (but not the parameters). 
		/// </summary>
		/// <remarks>
		/// Only one GpuProgram of each type can be bound at once, binding another
		/// one will simply replace the existing one.
		/// </remarks>
		public override void BindGpuProgram( GpuProgram program )
		{
			switch ( program.Type )
			{
				case GpuProgramType.Vertex:

					if ( program != null && program.IsSkeletalAnimationIncluded )
					{
						useSkinnedEffect = true;
						//LogManager.Instance.Write("Using Skinning Effect.");
					}
					else
						useSkinnedEffect = false;
					break;
			}
			/*
			switch ( program.Type )
			{
				case GpuProgramType.Vertex:
					_device.VertexShader = ( (XnaVertexProgram)program ).VertexShader;
					VertexShaderIsSet = true;
					break;

				case GpuProgramType.Fragment:
					_device.PixelShader = ( (XnaFragmentProgram)program ).PixelShader;
					PixelShaderIsSet = true;
					break;
			}
			 */
		}

		#endregion

		#region BindGpuProgramParameters

		/// <summary>
		/// Bind Gpu program parameters.
		/// </summary>
		public override void BindGpuProgramParameters( GpuProgramType type, GpuProgramParameters parms, GpuProgramParameters.GpuParamVariability mask )
		{
			//switch ( type )
			//{
			//    case GpuProgramType.Vertex:
			//        if ( parms.HasIntConstants )
			//        {
			//            for ( int index = 0; index < parms.IntConstantCount; index++ )
			//            {
			//                GpuProgramParameters.IntConstantEntry entry = parms.GetIntConstant( index );

			//                if ( entry.isSet )
			//                {
			//                    _device.SetVertexShaderConstant( index, entry.val );
			//                }
			//            }
			//        }
			//        if ( parms.HasFloatConstants )
			//        {
			//            for ( int index = 0; index < parms.FloatConstantCount; index++ )
			//            {
			//                GpuProgramParameters.FloatConstantEntry entry = parms.GetFloatConstant( index );

			//                if ( entry.isSet )
			//                {
			//                    _device.SetVertexShaderConstant( index, entry.val );
			//                }
			//            }
			//        }
			//        break;
			//    case GpuProgramType.Fragment:
			//        if ( parms.HasIntConstants )
			//        {
			//            for ( int index = 0; index < parms.IntConstantCount; index++ )
			//            {
			//                GpuProgramParameters.IntConstantEntry entry = parms.GetIntConstant( index );

			//                if ( entry.isSet )
			//                {
			//                    _device.SetPixelShaderConstant( index, entry.val );
			//                }
			//            }
			//        }

			//        if ( parms.HasFloatConstants )
			//        {
			//            for ( int index = 0; index < parms.FloatConstantCount; index++ )
			//            {
			//                GpuProgramParameters.FloatConstantEntry entry = parms.GetFloatConstant( index );

			//                if ( entry.isSet )
			//                {
			//                    _device.SetPixelShaderConstant( index, entry.val );
			//                }
			//            }
			//        }
			//        break;
			//}
		}

		#endregion

		#region BindGpuProgramPassIterationParameters

		/// <summary>
		/// Only binds Gpu program parameters used for passes that have more than one iteration rendering
		/// </summary>
		/// <param name="gptype"></param>
		public override void BindGpuProgramPassIterationParameters( GpuProgramType gptype )
		{
			throw new NotImplementedException();
		}

		#endregion

		#region ClearFrameBuffer

		/// <summary>
		/// Clears one or more frame buffers on the active render target.
		/// </summary>
		/// <param name="buffers">
		///  Combination of one or more elements of <see cref="Graphics.RenderTarget.FrameBuffer"/>
		///  denoting which buffers are to be cleared.
		/// </param>
		/// <param name="color">The color to clear the color buffer with, if enabled.</param>
		/// <param name="depth">The value to initialize the depth buffer with, if enabled.</param>
		/// <param name="stencil">The value to initialize the stencil buffer with, if enabled.</param>
		public override void ClearFrameBuffer( FrameBufferType buffers, ColorEx color, Real depth, ushort stencil )
		{
			XFG.ClearOptions flags = 0; //ClearFlags 

			if ( ( buffers & FrameBufferType.Color ) > 0 )
			{
				flags |= XFG.ClearOptions.Target;
			}
			if ( ( buffers & FrameBufferType.Depth ) > 0 )
			{
				flags |= XFG.ClearOptions.DepthBuffer;
			}
			// Only try to clear the stencil buffer if supported
			if ( ( buffers & FrameBufferType.Stencil ) > 0 && Capabilities.HasCapability( Graphics.Capabilities.StencilBuffer ) )
			{
				flags |= XFG.ClearOptions.Stencil;
			}
			XNA.Color col = XnaHelper.Convert( color );

			// clear the device using the specified params
			_device.Clear( flags, col, depth, stencil );
		}

		#endregion

		#region ConvertColor

		/// <summary>
		/// Converts the Axiom.Core.ColorEx value to a int.  Each API may need the 
		/// bytes of the packed color data in different orders. i.e. OpenGL - ABGR, D3D - ARGB
		/// </summary>
		/// <param name="color"></param>
		/// <returns></returns>
		public override int ConvertColor( ColorEx color )
		{
			return color.ToARGB();
		}

		//public override ColorEx ConvertColor( int color )
		//{
		//    ColorEx colorEx;
		//    colorEx.a = (float)( ( color >> 24 ) % 256 ) / 255;
		//    colorEx.r = (float)( ( color >> 16 ) % 256 ) / 255;
		//    colorEx.g = (float)( ( color >> 8 ) % 256 ) / 255;
		//    colorEx.b = (float)( ( color ) % 256 ) / 255;
		//    return colorEx;
		//}

		#endregion

		#region CreateRenderWindow

		/// <summary>
		/// Creates a new render window.
		/// </summary>
		/// <remarks>
		/// This method creates a new rendering window as specified
		/// by the paramteters. The rendering system could be
		/// responible for only a single window (e.g. in the case
		/// of a game), or could be in charge of multiple ones (in the
		/// case of a level editor). The option to create the window
		/// as a child of another is therefore given.
		/// This method will create an appropriate subclass of
		/// RenderWindow depending on the API and platform implementation.
		/// </remarks>
		/// <param name="name"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="isFullScreen"></param>
		/// <param name="miscParams">
		/// A collection of addition rendersystem specific options.
		/// </param>
		public override RenderWindow CreateRenderWindow( string name, int width, int height, bool isFullScreen, NamedParameterList miscParams )
		{
			// Check we're not creating a secondary window when the primary
			// was fullscreen
			if ( _primaryWindow != null && _primaryWindow.IsFullScreen )
			{
				throw new Exception( "Cannot create secondary windows when the primary is full screen." );
			}

			if ( _primaryWindow != null && isFullScreen )
			{
				throw new ArgumentException( "Cannot create full screen secondary windows." );
			}

			// Log a message
			System.Text.StringBuilder strParams = new System.Text.StringBuilder();
			if ( miscParams != null )
			{
				foreach ( KeyValuePair<string, object> entry in miscParams )
				{
					strParams.AppendFormat( "{0} = {1}; ", entry.Key, entry.Value );
				}
			}
			LogManager.Instance.Write( "[XNA] : Creating RenderWindow \"{0}\", {1}x{2} {3} miscParams: {4}",
									   name, width, height, isFullScreen ? "fullscreen" : "windowed", strParams.ToString() );

			// Make sure we don't already have a render target of the 
			// same name as the one supplied
			if ( renderTargets.ContainsKey( name ) )
			{
				throw new Exception( String.Format( "A render target of the same name '{0}' already exists." +
													"You cannot create a new window with this name.", name ) );
			}

			RenderWindow window = new XnaRenderWindow( _activeDriver, _primaryWindow != null ? _device : null );

			// create the window
			window.Create( name, width, height, isFullScreen, miscParams );

			// add the new render target
			AttachRenderTarget( window );
			// If this is the first window, get the D3D device and create the texture manager
			if ( _primaryWindow == null )
			{
				_primaryWindow = (XnaRenderWindow)window;
				_device = (XFG.GraphicsDevice)window[ "XNADEVICE" ];

				basicEffect = new XFG.BasicEffect( _device );
				skinnedEffect = new XFG.SkinnedEffect( _device );
				// Create the texture manager for use by others
				textureManager = new XnaTextureManager( _device );
				// Also create hardware buffer manager
				hardwareBufferManager = new XnaHardwareBufferManager( _device );

				// Create the GPU program manager
				gpuProgramMgr = new XnaGpuProgramManager( _device );
				// create & register HLSL factory
				gpuProgramMgr.PushSyntaxCode( "hlsl" );

				// Initialize the capabilities structures
				_checkHardwareCapabilities( XFG.GraphicsProfile.HiDef );
			}
			else
			{
				_secondaryWindows.Add( (XnaRenderWindow)window );
			}

			return window;
		}

		#endregion

		#region CreateMultiRenderTarget

		/// <summary>
		/// Create a MultiRenderTarget, which is a render target that renders to multiple RenderTextures at once.
		/// </summary>
		/// <Remarks>
		/// Surfaces can be bound and unbound at will. This fails if Capabilities.MultiRenderTargetsCount is smaller than 2.
		/// </Remarks>
		/// <returns></returns>
		public override MultiRenderTarget CreateMultiRenderTarget( string name )
		{
			throw new NotImplementedException();
		}

		#endregion

		#region CreateHardwareOcclusionQuery

		/// <summary>
		/// Requests an API implementation of a hardware occlusion query used to test for the number
		/// of fragments rendered between calls to <see cref="HardwareOcclusionQuery.Begin"/> and 
		/// <see cref="HardwareOcclusionQuery.End"/> that pass the depth buffer test.
		/// </summary>
		/// <returns>An API specific implementation of an occlusion query.</returns>
		public override HardwareOcclusionQuery CreateHardwareOcclusionQuery()
		{
			return new XnaHardwareOcclusionQuery( _device );
		}

		#endregion

		#region EndFrame

		/// <summary>
		/// Ends rendering of a frame to the current viewport.
		/// </summary>
		public override void EndFrame()
		{
			// end the scene
		}

		#endregion

		#region Initialize

		/// <summary>
		/// Initialize the rendering engine.
		/// </summary>
		/// <param name="autoCreateWindow">If true, a default window is created to serve as a rendering target.</param>
		/// <param name="windowTitle">Text to display on the window caption if not fullscreen.</param>
		/// <returns>A RenderWindow implementation specific to this RenderSystem.</returns>
		/// <remarks>All subclasses should call this method from within thier own intialize methods.</remarks>
		public override RenderWindow Initialize( bool autoCreateWindow, string windowTitle )
		{
			LogManager.Instance.Write( "[XNA] : Subsystem Initializing" );

#if !( XBOX || XBOX360 )
			WindowEventMonitor.Instance.MessagePump = Win32MessageHandling.MessagePump;
#endif
			_activeDriver = XnaHelper.GetDriverInfo()[ ConfigOptions[ "Rendering Device" ].Value ];
			if ( _activeDriver == null )
			{
				throw new ArgumentException( "Problems finding requested Xna driver!" );
			}

			RenderWindow renderWindow = null;

			// register the HLSL program manager
			HighLevelGpuProgramManager.Instance.AddFactory( new HLSL.HLSLProgramFactory() );

			if ( autoCreateWindow )
			{
				int width = 800;
				int height = 600;
				int bpp = 32;
				bool fullScreen = false;

				ConfigOption optVM = ConfigOptions[ "Video Mode" ];
				string vm = optVM.Value;
				width = int.Parse( vm.Substring( 0, vm.IndexOf( "x" ) ) );
				height = int.Parse( vm.Substring( vm.IndexOf( "x" ) + 1, vm.IndexOf( "@" ) - ( vm.IndexOf( "x" ) + 1 ) ) );
				bpp = int.Parse( vm.Substring( vm.IndexOf( "@" ) + 1, vm.IndexOf( "-" ) - ( vm.IndexOf( "@" ) + 1 ) ) );

#if !(XBOX || XBOX360 || SILVERLIGHT ) //
				fullScreen = ( ConfigOptions[ "Full Screen" ].Value == "Yes" );
#endif

				NamedParameterList miscParams = new NamedParameterList();
				miscParams.Add( "title", windowTitle );
				miscParams.Add( "colorDepth", bpp );
				//miscParams.Add( "FSAA", this._fsaaType );
				miscParams.Add( "FSAAQuality", _fsaaQuality );
				miscParams.Add( "vsync", _vSync );
				miscParams.Add( "useNVPerfHUD", _useNVPerfHUD );

				// create the render window
				renderWindow = CreateRenderWindow( "Main Window", width, height, fullScreen, miscParams );
			}

			StateManager = new StateManagement();
			new XnaMaterialManager();

			LogManager.Instance.Write( "[XNA] : Subsystem Initialized successfully." );
			return renderWindow;
		}

		#endregion

		#region Reinitialize

		/// <summary>
		/// Reinitializes the Render System
		/// </summary>
		public override void Reinitialize()
		{
			throw new NotImplementedException();
		}

		#endregion

		#region CreateRenderSystemCapabilities

		/// <summary>
		/// Query the real capabilities of the GPU and driver in the RenderSystem
		/// </summary>
		public override RenderSystemCapabilities CreateRenderSystemCapabilities()
		{
			throw new NotImplementedException();
		}

		#endregion

		#region MakeOrthoMatrix

		/// <summary>
		/// Builds an orthographic projection matrix suitable for this render system.
		/// </summary>
		/// <remarks>
		/// Because different APIs have different requirements (some incompatible) for the
		/// projection matrix, this method allows each to implement their own correctly and pass
		/// back a generic Matrix4 for storage in the engine.
		/// </remarks>
		/// <param name="fov">Field of view angle.</param>
		/// <param name="aspectRatio">Aspect ratio.</param>
		/// <param name="near">Near clipping plane distance.</param>
		/// <param name="far">Far clipping plane distance.</param>
		/// <param name="dest"></param>
		/// <param name="forGpuPrograms"></param>
		public override void MakeOrthoMatrix( Radian fov, Real aspectRatio, Real near, Real far, out Matrix4 dest, bool forGpuPrograms )
		{
			float thetaY = Utility.DegreesToRadians( fov / 2.0f );
			float tanThetaY = Utility.Tan( thetaY );
			float tanThetaX = tanThetaY * aspectRatio;

			float halfW = tanThetaX * near;
			float halfH = tanThetaY * near;

			float w = 1.0f / ( halfW );
			float h = 1.0f / ( halfH );
			float q = 0;

			if ( far != 0 )
				q = 1.0f / ( far - near );

			dest = Matrix4.Zero;
			dest.m00 = w;
			dest.m11 = h;
			dest.m22 = q;
			dest.m23 = -near / ( far - near );
			dest.m33 = 1;

			if ( forGpuPrograms )
				dest.m22 = -dest.m22;
		}

		#endregion

		#region ConvertProjectionMatrix

		/// <summary>
		/// Converts a uniform projection matrix to one suitable for this render system.
		/// </summary>
		/// <remarks>
		/// Because different APIs have different requirements (some incompatible) for the
		/// projection matrix, this method allows each to implement their own correctly and pass
		/// back a generic Matrix4 for storage in the engine.
		/// </remarks>
		/// <param name="matrix"></param>
		/// <param name="dest"></param>
		/// <param name="forGpuProgram"></param>
		public override void ConvertProjectionMatrix( Matrix4 matrix, out Matrix4 dest, bool forGpuProgram )
		{
			dest = new Matrix4( matrix.m00, matrix.m01, matrix.m02, matrix.m03,
								matrix.m10, matrix.m11, matrix.m12, matrix.m13,
								matrix.m20, matrix.m21, matrix.m22, matrix.m23,
								matrix.m30, matrix.m31, matrix.m32, matrix.m33 );

			// Convert depth range from [-1,+1] to [0,1]
			dest.m20 = ( dest.m20 + dest.m30 ) / 2.0f;
			dest.m21 = ( dest.m21 + dest.m31 ) / 2.0f;
			dest.m22 = ( dest.m22 + dest.m32 ) / 2.0f;
			dest.m23 = ( dest.m23 + dest.m33 ) / 2.0f;

			if ( !forGpuProgram )
			{
				// Convert right-handed to left-handed
				dest.m02 = -dest.m02;
				dest.m12 = -dest.m12;
				dest.m22 = -dest.m22;
				dest.m32 = -dest.m32;
			}
		}

		#endregion

		#region MakeProjectionMatrix

		/// <summary>
		/// Builds a perspective projection matrix suitable for this render system.
		/// </summary>
		/// <remarks>
		/// Because different APIs have different requirements (some incompatible) for the
		/// projection matrix, this method allows each to implement their own correctly and pass
		/// back a generic Matrix4 for storage in the engine.
		/// </remarks>
		/// <param name="fov">Field of view angle.</param>
		/// <param name="aspectRatio">Aspect ratio.</param>
		/// <param name="near">Near clipping plane distance.</param>
		/// <param name="far">Far clipping plane distance.</param>
		/// <param name="dest"></param>
		/// <param name="forGpuProgram"></param>
		public override void MakeProjectionMatrix( Radian fov, Real aspectRatio, Real near, Real far, out Matrix4 dest, bool forGpuProgram )
		{
			float theta = Utility.DegreesToRadians( fov * 0.5f );
			float h = 1 / Utility.Tan( theta );
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

			dest = Matrix4.Zero;

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
		}

		/// <summary>
		/// Builds a perspective projection matrix for the case when frustum is
		/// not centered around camera.
		/// </summary>
		/// <remarks>
		/// Viewport coordinates are in camera coordinate frame, i.e. camera is 
		/// at the origin.
		/// </remarks>
		public override void MakeProjectionMatrix( Real left, Real right, Real bottom, Real top, Real nearPlane, Real farPlane, out Matrix4 dest, bool forGpuProgram )
		{
			// Correct position for off-axis projection matrix
			if ( !forGpuProgram )
			{
				Real offsetX = left + right;
				Real offsetY = top + bottom;

				left -= offsetX;
				right -= offsetX;
				top -= offsetY;
				bottom -= offsetY;
			}

			Real width = right - left;
			Real height = top - bottom;
			Real q, qn;
			if ( farPlane == 0 )
			{
				q = 1 - Frustum.InfiniteFarPlaneAdjust;
				qn = nearPlane * ( Frustum.InfiniteFarPlaneAdjust - 1 );
			}
			else
			{
				q = farPlane / ( farPlane - nearPlane );
				qn = -q * nearPlane;
			}
			
			dest = Matrix4.Zero;
			dest.m00 = 2 * nearPlane / width;
			dest.m02 = ( right + left ) / width;
			dest.m11 = 2 * nearPlane / height;
			dest.m12 = ( top + bottom ) / height;
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
		}

		#endregion

		#region SetAlphaRejectSettings

		/// <summary>
		///  Sets the global alpha rejection approach for future renders.
		/// </summary>
		/// <param name="func">The comparison function which must pass for a pixel to be written.</param>
		/// <param name="value">The value to compare each pixels alpha value to (0-255)</param>
		/// <param name="alphaToCoverage">Whether to enable alpha to coverage, if supported</param>
		public override void SetAlphaRejectSettings( CompareFunction func, byte value, bool alphaToCoverage )
		{
			bool a2c = false;
			if ( func != Axiom.Graphics.CompareFunction.AlwaysPass )
			{
				a2c = alphaToCoverage;
			}

			StateManager.BlendState.AlphaBlendFunction = XFG.BlendFunction.Add /* XnaHelper.Convert( func )*/;
			//StateManager.BlendState.ReferenceAlpha = value;

			// Alpha to coverage
			if ( lasta2c != a2c && this.Capabilities.HasCapability( Graphics.Capabilities.AlphaToCoverage ) )
			{
				lasta2c = a2c;
			}
		}

		#endregion

		public override void SetClipPlane( ushort index, float A, float B, float C, float D )
		{
			throw new NotImplementedException();
		}

		public override void EnableClipPlane( ushort index, bool enable )
		{
			throw new NotImplementedException();
		}

		public override void SetColorBufferWriteEnabled( bool red, bool green, bool blue, bool alpha )
		{
			XFG.ColorWriteChannels val = 0;

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
			StateManager.BlendState.ColorWriteChannels = val;
		}

		public override void SetDepthBufferParams( bool depthTest, bool depthWrite, CompareFunction depthFunction )
		{
			StateManager.DepthStencilState.DepthBufferEnable = depthTest;
			StateManager.DepthStencilState.DepthBufferWriteEnable = depthWrite;
			StateManager.DepthStencilState.DepthBufferFunction = XnaHelper.Convert( depthFunction );
		}

		public override void SetFog( FogMode mode, ColorEx color, float density, float start, float end )
		{
			basicEffect.FogEnabled = mode != FogMode.None;
			basicEffect.FogColor = XnaHelper.Convert( color ).ToVector3();
			basicEffect.FogStart = start;
			basicEffect.FogEnd = end;

			skinnedEffect.FogEnabled = mode != FogMode.None;
			skinnedEffect.FogColor = XnaHelper.Convert( color ).ToVector3();
			skinnedEffect.FogStart = start;
			skinnedEffect.FogEnd = end;
#if AXIOM_FF_EMULATION
			_ffProgramParameters.FogColor = color;
			_ffProgramParameters.FogDensity = density;
			_ffProgramParameters.FogEnd = end;
			_ffProgramParameters.FogStart = start;
			_ffProgramParameters.FogMode = mode;
#endif

			#region fog fixed function implementation
			// disable fog if set to none
			/*if ( mode == Axiom.Graphics.FogMode.None )
			{
				_device.RenderState.FogTableMode = Microsoft.Xna.Framework.Graphics.FogMode.None;
				  
				_device.RenderState.FogEnable = false;
			}
			else
			{
				// enable fog
				XFG.Color col = XnaHelper.Convert( color );
				_device.RenderState.FogEnable = true;
				_device.RenderState.FogVertexMode = XnaHelper.Convert(mode);
				_device.RenderState.FogTableMode= XnaHelper.Convert(mode);
				_device.RenderState.FogColor= col;
				_device.RenderState.FogStart= start;
				_device.RenderState.FogEnd= end;
				_device.RenderState.FogDensity= density;
				_device.RenderState.RangeFogEnable= true; 
			}*/
			#endregion

		}

		public override void SetSceneBlending( SceneBlendFactor src, SceneBlendFactor dest )
		{
			StateManager.BlendState.AlphaSourceBlend = XnaHelper.Convert( src );
			StateManager.BlendState.AlphaDestinationBlend = XnaHelper.Convert( dest );
			StateManager.BlendState.ColorSourceBlend = XnaHelper.Convert( src );
			StateManager.BlendState.ColorDestinationBlend = XnaHelper.Convert( dest );
		}

		/// <summary>
		/// Sets the global blending factors for combining subsequent renders with the existing frame contents.
		/// The result of the blending operation is:
		/// final = (texture * sourceFactor) + (pixel * destFactor).
		/// Each of the factors is specified as one of a number of options, as specified in the SceneBlendFactor
		/// enumerated type.
		/// </summary>
		/// <param name="sourceFactor">The source factor in the above calculation, i.e. multiplied by the texture colour components.</param>
		/// <param name="destFactor">The destination factor in the above calculation, i.e. multiplied by the pixel colour components.</param>
		/// <param name="sourceFactorAlpha">The source factor in the above calculation for the alpha channel, i.e. multiplied by the texture alpha components.</param>
		/// <param name="destFactorAlpha">The destination factor in the above calculation for the alpha channel, i.e. multiplied by the pixel alpha components.</param>
		public override void SetSeparateSceneBlending( SceneBlendFactor sourceFactor, SceneBlendFactor destFactor, SceneBlendFactor sourceFactorAlpha, SceneBlendFactor destFactorAlpha )
		{
			StateManager.BlendState.ColorSourceBlend = XnaHelper.Convert( sourceFactor );
			StateManager.BlendState.ColorDestinationBlend = XnaHelper.Convert( destFactor );
			StateManager.BlendState.AlphaSourceBlend = XnaHelper.Convert( sourceFactorAlpha );
			StateManager.BlendState.AlphaDestinationBlend = XnaHelper.Convert( destFactorAlpha );
		}

		public override void SetScissorTest( bool enable, int left, int top, int right, int bottom )
		{
			//if ( enable )
			//{
			//    _device.ScissorRectangle = new XNA.Rectangle( left, top, right - left, bottom - top );
			//    _device.RenderState.ScissorTestEnable = true;
			//}
			//else
			//{
			//    _device.RenderState.ScissorTestEnable = false;
			//}
		}

		public override void SetStencilBufferParams( CompareFunction function, int refValue, int mask, StencilOperation stencilFailOp, StencilOperation depthFailOp, StencilOperation passOp, bool twoSidedOperation )
		{
			bool flip;
			// 2 sided operation?
			if ( twoSidedOperation )
			{
				if ( !Capabilities.HasCapability( Graphics.Capabilities.TwoSidedStencil ) )
				{
					throw new AxiomException( "2-sided stencils are not supported on this hardware!" );
				}
				StateManager.DepthStencilState.TwoSidedStencilMode = true;
				flip = ( invertVertexWinding && activeRenderTarget.RequiresTextureFlipping ) ||
						( !invertVertexWinding && !activeRenderTarget.RequiresTextureFlipping );

				StateManager.DepthStencilState.StencilFail = XnaHelper.Convert( stencilFailOp, !flip );
				StateManager.DepthStencilState.StencilDepthBufferFail = XnaHelper.Convert( depthFailOp, !flip );
				StateManager.DepthStencilState.StencilPass = XnaHelper.Convert( passOp, !flip );
			}
			else
			{
				StateManager.DepthStencilState.TwoSidedStencilMode = false;
				flip = false;
			}

			// configure standard version of the stencil operations
			StateManager.DepthStencilState.StencilFunction = XnaHelper.Convert( function );
			StateManager.DepthStencilState.ReferenceStencil = refValue;
			StateManager.DepthStencilState.StencilMask = mask;
			StateManager.DepthStencilState.StencilFail = XnaHelper.Convert( stencilFailOp, flip );
			StateManager.DepthStencilState.StencilDepthBufferFail = XnaHelper.Convert( depthFailOp, flip );
			StateManager.DepthStencilState.StencilPass = XnaHelper.Convert( passOp, flip );
			StateManager.BlendState.ColorWriteChannels = XFG.ColorWriteChannels.None;
		}

		public override void SetSurfaceParams( ColorEx ambient, ColorEx diffuse, ColorEx specular, ColorEx emissive, float shininess, TrackVertexColor tracking )
		{
#if AXIOM_FF_EMULATION
			if (//ambient == ColorEx.White &&
				diffuse == ColorEx.Black //&&
				//emissive == ColorEx.Black &&
				//specular == ColorEx.Black &&
				//shininess == 0
				)
			{
				//_fixedFunctionState.MaterialEnabled = false;
				_ffProgramParameters.MaterialAmbient = new ColorEx( 0, 1, 1, 1 );
				_ffProgramParameters.MaterialDiffuse = ColorEx.White;
				_ffProgramParameters.MaterialSpecular = ColorEx.Black;
			}
			else
			{
				//_fixedFunctionState.MaterialEnabled = true;
				_ffProgramParameters.MaterialAmbient = ambient;
				_ffProgramParameters.MaterialDiffuse = diffuse;
				_ffProgramParameters.MaterialSpecular = specular;
				//_ffProgramParameters.MaterialEmissive = emissive;
				//_ffProgramParameters.MaterialShininess = shininess;
			}
#endif
		}

		/// <summary>
		/// Sets the size of points and how they are attenuated with distance.
		/// <remarks>
		/// When performing point rendering or point sprite rendering,
		/// point size can be attenuated with distance. The equation for
		/// doing this is attenuation = 1 / (constant + linear * dist + quadratic * d^2) .
		/// </remarks>
		/// </summary>
		public override void SetPointParameters( Real size, bool attenuationEnabled, Real constant, Real linear, Real quadratic, Real minSize, Real maxSize )
		{
			// throw new AxiomException( "XNA does not support PointSprites." );
		}

		public override void SetTexture( int stage, bool enabled, Texture texture )
		{
			XnaTexture xnaTexture = (XnaTexture)texture;
			bool compensateNPOT = false;

			if ( ( texture != null ) && ( !Bitwise.IsPow2( texture.Width ) || !Bitwise.IsPow2( texture.Height ) ) )
			{
				if ( Capabilities.HasCapability( Graphics.Capabilities.NonPowerOf2Textures ) )
				{
					if ( Capabilities.NonPOW2TexturesLimited )
						compensateNPOT = true;
				}
				else
					compensateNPOT = true;

				if ( compensateNPOT )
				{
					SetTextureAddressingMode( stage, new UVWAddressing( TextureAddressing.Clamp ) );
				}
			}

			texStageDesc[ stage ].Enabled = enabled;
			if ( enabled && xnaTexture != null )
			{
				_device.Textures[ stage ] = xnaTexture.DXTexture;
				basicEffect.Texture = (XFG.Texture2D)xnaTexture.DXTexture;
				basicEffect.TextureEnabled = enabled;

				skinnedEffect.Texture = (XFG.Texture2D)xnaTexture.DXTexture;

				// set stage description
				texStageDesc[ stage ].tex = xnaTexture.DXTexture;
				texStageDesc[ stage ].texType = xnaTexture.TextureType;
			}
			else
			{
				if ( texStageDesc[ stage ].tex != null )
				{
					_device.Textures[ stage ] = null;
				}
				// set stage description to defaults
				texStageDesc[ stage ].tex = null;
				texStageDesc[ stage ].autoTexCoordType = TexCoordCalcMethod.None;
				texStageDesc[ stage ].coordIndex = 0;
				texStageDesc[ stage ].texType = TextureType.OneD;
			}
#if AXIOM_FF_EMULATION
			_ffProgramParameters.SetTextureEnabled( stage, enabled );
#endif
		}

		public override void SetTextureAddressingMode( int stage, UVWAddressing texAddressingMode )
		{
			if ( _device.GetVertexBuffers().Length == 0 )
			{
				return;
			}
			if ( !( from XFG.VertexElement vde in _device.GetVertexBuffers()[ 0 ].VertexBuffer.VertexDeclaration.GetVertexElements()
					where vde.VertexElementUsage == XFG.VertexElementUsage.Normal
					select vde ).Any() )
			{
				return;
			}

			XFG.Texture2D xnaTexture = (XFG.Texture2D)_device.Textures[ stage ];
			bool compensateNPOT = false;

			if ( ( xnaTexture != null ) && ( !Bitwise.IsPow2( xnaTexture.Width ) || !Bitwise.IsPow2( xnaTexture.Height ) ) )
			{
				if ( Capabilities.HasCapability( Graphics.Capabilities.NonPowerOf2Textures ) )
				{
					if ( Capabilities.NonPOW2TexturesLimited )
						compensateNPOT = true;
				}
				else
					compensateNPOT = true;

				if ( compensateNPOT )
				{
					texAddressingMode = new UVWAddressing( TextureAddressing.Clamp );
				}
			}

			// set the device sampler states accordingly
			StateManager.SamplerStates[ stage ].AddressU = XnaHelper.Convert( texAddressingMode.U );
			StateManager.SamplerStates[ stage ].AddressV = XnaHelper.Convert( texAddressingMode.V );
			StateManager.SamplerStates[ stage ].AddressW = XnaHelper.Convert( texAddressingMode.W );
		}

		public override void SetTextureBorderColor( int stage, ColorEx borderColor )
		{
			//texStageDesc[ stage ].borderColor = borderColor;
		}

		public override void SetTextureBlendMode( int stage, LayerBlendModeEx blendMode )
		{
			basicEffect.Alpha = 1.0f;
			skinnedEffect.Alpha = 1.0f;

			if ( blendMode.blendType == LayerBlendType.Color )
			{
				texStageDesc[ stage ].layerBlendMode = blendMode;
			}
			/* TODO: use StateManager.BlendState */

			if ( blendMode.operation == LayerBlendOperationEx.BlendManual )
			{
				StateManager.BlendState.BlendFactor = new XNA.Color( blendMode.blendFactor, 0, 0, 0 );
			}
			if ( blendMode.blendType == LayerBlendType.Color )
			{
				//_device.RenderState.AlphaBlendEnable = false;
			}
			else if ( blendMode.blendType == LayerBlendType.Alpha )
			{
				//_device.RenderState.AlphaBlendEnable = true;
			}

			ColorEx manualD3D = XnaHelper.Convert( StateManager.BlendState.BlendFactor );
			if ( blendMode.blendType == LayerBlendType.Color )
			{
				manualD3D = new ColorEx( blendMode.blendFactor, blendMode.colorArg1.r, blendMode.colorArg1.g, blendMode.colorArg1.b );
			}
			else if ( blendMode.blendType == LayerBlendType.Alpha )
			{
				manualD3D = new ColorEx( blendMode.alphaArg1, blendMode.blendFactor, blendMode.blendFactor, blendMode.blendFactor );
			}

			LayerBlendSource blendSource = blendMode.source1;
			for ( int i = 0; i < 2; i++ )
			{
				// set the texture blend factor if this is manual blending
				if ( blendSource == LayerBlendSource.Manual )
				{
					StateManager.BlendState.BlendFactor = XnaHelper.Convert( manualD3D );
				}
				// pick proper argument settings
				if ( blendMode.blendType == LayerBlendType.Color )
				{
					if ( i == 0 )
					{
						texStageDesc[ stage ].layerBlendMode.colorArg1 = blendMode.colorArg1;
					}
					else if ( i == 1 )
					{
						texStageDesc[ stage ].layerBlendMode.colorArg2 = blendMode.colorArg2;
					}
				}
				else if ( blendMode.blendType == LayerBlendType.Alpha )
				{
					if ( i == 0 )
					{
						texStageDesc[ stage ].layerBlendMode.alphaArg1 = blendMode.alphaArg1;
						basicEffect.Alpha = blendMode.alphaArg1;
					}
					else if ( i == 1 )
					{
						texStageDesc[ stage ].layerBlendMode.alphaArg2 = blendMode.alphaArg2;
						//basicEffect.Alpha = blendMode.alphaArg2;
					}
					skinnedEffect.Alpha = basicEffect.Alpha;
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

		public override void SetTextureCoordCalculation( int stage, TexCoordCalcMethod method, Frustum frustum )
		{
			texStageDesc[ stage ].autoTexCoordType = method;
			texStageDesc[ stage ].frustum = frustum;
			//texStageDesc[stage].Enabled = true;
			//if (frustum != null) MessageBox.Show(texStageDesc[stage].Enabled.ToString());
		}

		public override void SetTextureCoordSet( int stage, int index )
		{
			texStageDesc[ stage ].coordIndex = index;
		}

		public override void SetTextureLayerAnisotropy( int stage, int maxAnisotropy )
		{
			//-if maxAnisotropy is higher than what the graphics device is capapble of
			//Xna 4.0 should magically clamp the value for us.
			//if ( maxAnisotropy > _capabilities.MaxAnisotropy )
			//{
			//    maxAnisotropy = _capabilities.MaxAnisotropy;
			//}

			StateManager.SamplerStates[ stage ].MaxAnisotropy = maxAnisotropy;
		}

		public override void SetTextureMatrix( int stage, Axiom.Math.Matrix4 xform )
		{
			if ( texStageDesc[ stage ].autoTexCoordType == TexCoordCalcMethod.ProjectiveTexture )
			{

				//seems like we have to apply a specific transform when we have the frustum
				//and a projective texture
				//from directx rendersystem

				// Derive camera space to projector space transform
				// To do this, we need to undo the camera view matrix, then 
				// apply the projector view & projection matrices

				//Matrix4 newMat = _viewMatrix.Inverse();
				//texStageDesc[ stage ].frustum.ViewMatrix = texStageDesc[ stage ].frustum.ViewMatrix.Transpose();
				//texStageDesc[stage].frustum.ProjectionMatrix = texStageDesc[stage].frustum.ProjectionMatrix.Transpose();
				//newMat = texStageDesc[ stage ].frustum.ViewMatrix * newMat;
				//newMat = texStageDesc[ stage ].frustum.ProjectionMatrix * newMat;
				//newMat = Matrix4.ClipSpace2DToImageSpace * newMat;
				//xform = xform * newMat;

			}
#if AXIOM_FF_EMULATION
			_ffProgramParameters.SetTextureMatrix( stage, xform.Transpose() );
#endif
		}

		public override void SetTextureUnitFiltering( int stage, FilterType type, FilterOptions filter )
		{
			/*
			 * TextureFilter enumeration now combines FilterType and TextureType in 4.0, 
			 */
			XnaTextureType texType = XnaHelper.Convert( texStageDesc[ stage ].texType );
			XFG.TextureFilter texFilter = XnaHelper.Convert( type, filter, texType );
			StateManager.SamplerStates[ stage ].Filter = texFilter;
		}

		public override void SetViewport( Viewport viewport )
		{
			if ( activeViewport != viewport || viewport.IsUpdated )
			{
				// store this viewport and it's target
				activeViewport = viewport;
				activeRenderTarget = viewport.Target;

				// get the back buffer surface for this viewport
				XFG.RenderTarget2D[] back = (XFG.RenderTarget2D[])activeRenderTarget[ "XNABACKBUFFER" ];
				if ( back == null )
				{
					_device.SetRenderTarget( null );
					//the back buffer is null so it's not a render to texture,
					//we render directly to the screen,
					//set the original depth stencil buffer
					//_device.DepthStencilBuffer = oriDSB;
					return;
				}
				else
				{

				}
				/*
				XFG.DepthStencilBuffer depth = (XFG.DepthStencilBuffer)activeRenderTarget[ "XNAZBUFFER" ];
				if ( depth == null )
				{
					// No depth buffer provided, use our own
					// Request a depth stencil that is compatible with the format, multisample type and
					// dimensions of the render target.
					//it is probably a render to texture, so we create the first time a depth buffer
					depth = _getDepthStencilFor( back[ 0 ].Format, back[ 0 ].MultiSampleType, back[ 0 ].Width, back[ 0 ].Height );
				}

				if ( depth.Format == _device.DepthStencilBuffer.Format )
				{
					_device.DepthStencilBuffer = depth;
				}
				*/


				// Bind render targets
				int count = back.Length;

				for ( int i = 0; i < count && back[ i ] != null; ++i )
				{
					_device.SetRenderTarget( back[ i ] );
				}

				// set the culling mode, to make adjustments required for viewports
				// that may need inverted vertex winding or texture flipping
				this.CullingMode = cullingMode;

				XFG.Viewport xnavp = new XFG.Viewport();

				// set viewport dimensions
				xnavp.X = viewport.ActualLeft;
				xnavp.Y = viewport.ActualTop;
				xnavp.Width = viewport.ActualWidth;
				xnavp.Height = viewport.ActualHeight;

				// Z-values from 0.0 to 1.0 (TODO: standardize with OpenGL)
				xnavp.MinDepth = 0.0f;
				xnavp.MaxDepth = 1.0f;

				// set the current D3D viewport
				_device.Viewport = xnavp;

				// clear the updated flag
				viewport.IsUpdated = false;
			}
		}

		public override void UnbindGpuProgram( GpuProgramType type )
		{
			useSkinnedEffect = false;
			switch ( type )
			{
				case GpuProgramType.Vertex:
					//_device.VertexShader = null;
					VertexShaderIsSet = false;
					break;

				case GpuProgramType.Fragment:
					//_device.PixelShader = null;
					PixelShaderIsSet = false;
					break;
			}
		}

		public override void UseLights( LightList lights, int limit )
		{
			int currentLightCount = lights.Count < limit ? lights.Count : limit;

			List<Light> lightList = new List<Light>();
#if AXIOM_FF_EMULATION
			_fixedFunctionState.GeneralFixedFunctionState.ResetLightTypeCounts();
			for ( int index = 0; index < currentLightCount; index++ )
			{
				Light light = lights[ index ];
				lightList.Add( light );
				_fixedFunctionState.GeneralFixedFunctionState.IncrementLightTypeCount( light.Type );
			}
			_ffProgramParameters.Lights = lightList;
#endif
		}

		public override void ClearFrameBuffer( FrameBufferType buffers, ColorEx color, Real depth, ushort stencil )
		{
			throw new NotImplementedException();
		}

		public override DepthBuffer CreateDepthBufferFor( RenderTarget renderTarget )
		{
			throw new NotImplementedException();
		}

		public override void SetFog( FogMode mode, ColorEx color, Real density, Real linearStart, Real linearEnd )
		{
			throw new NotImplementedException();
		}

		public override void SetSceneBlending( SceneBlendFactor src, SceneBlendFactor dest, SceneBlendOperation op )
		{
			throw new NotImplementedException();
		}

		public override void SetSeparateSceneBlending( SceneBlendFactor sourceFactor, SceneBlendFactor destFactor, SceneBlendFactor sourceFactorAlpha, SceneBlendFactor destFactorAlpha, SceneBlendOperation op, SceneBlendOperation alphaOp )
		{
			throw new NotImplementedException();
		}

		public override void SetSurfaceParams( ColorEx ambient, ColorEx diffuse, ColorEx specular, ColorEx emissive, Real shininess, TrackVertexColor tracking )
		{
			throw new NotImplementedException();
		}

		public override void SetTextureMipmapBias( int unit, float bias )
		{
			throw new NotImplementedException();
		}

		public override void InitializeFromRenderSystemCapabilities( RenderSystemCapabilities caps, RenderTarget primary )
		{
			throw new NotImplementedException();
		}

		private XFG.DepthFormat _getDepthStencilFormatFor( XFG.SurfaceFormat fmt, int multiSampleCount )
		{
			XFG.DepthFormat dsfmt;

			// Check if result is cached
			if ( depthStencilCache.TryGetValue( fmt, out dsfmt ) )
				return dsfmt;

			// If not, probe with CheckDepthStencilMatch
			dsfmt = XFG.DepthFormat.None;

			// Get description of primary render target
			XFG.SurfaceFormat targetFormat = _primaryWindow.RenderSurfaceFormat;

			// Probe all depth stencil formats
			// Break on first one that matches
			foreach ( XFG.DepthFormat df in _preferredStencilFormats )
			{
				// Verify that the depth format exists
				//if ( !XFG.GraphicsAdapter.DefaultAdapter.CheckDeviceFormat( XFG.DeviceType.Hardware, targetFormat, XFG.TextureUsage.None, XFG.QueryUsages.None, XFG.ResourceType.DepthStencilBuffer, df ) )
				//    continue;

				XFG.SurfaceFormat suggestedSurfaceFormat;
				XFG.DepthFormat suggestedDepthFormat;
				int suggestedNumMultiSamples;

				XFG.GraphicsAdapter.DefaultAdapter.QueryRenderTargetFormat( _device.GraphicsProfile, targetFormat, df, multiSampleCount, out suggestedSurfaceFormat, out suggestedDepthFormat, out suggestedNumMultiSamples );
				dsfmt = suggestedDepthFormat;
				// Verify that the depth format is compatible
				//if ( XFG.GraphicsAdapter.DefaultAdapter.CheckDepthStencilMatch( XFG.DeviceType.Hardware, targetFormat, fmt, df ) )
				//{
				//    dsfmt = df;
				//    break;
				//}
			}

			// Cache result
			depthStencilCache[ fmt ] = dsfmt;
			return dsfmt;
		}

		//private XFG.DepthStencilBuffer _getDepthStencilFor( XFG.SurfaceFormat fmt, XFG.MultiSampleType multisample, int width, int height )
		//{
		//    XFG.DepthStencilBuffer zbuffer = null;

		//    XFG.DepthFormat dsfmt = _getDepthStencilFormatFor( fmt );
		//    if ( dsfmt == XFG.DepthFormat.Unknown )
		//        return null;

		//    /// Check if result is cached
		//    ZBufferFormat zbfmt = new ZBufferFormat( dsfmt, multisample );
		//    XFG.DepthStencilBuffer cachedzBuffer;
		//    if ( zBufferCache.TryGetValue( zbfmt, out cachedzBuffer ) )
		//    {
		//        /// Check if size is larger or equal
		//        if ( cachedzBuffer.Width >= width &&
		//            cachedzBuffer.Height >= height )
		//        {
		//            zbuffer = cachedzBuffer;
		//        }
		//        else
		//        {
		//            zBufferCache.Remove( zbfmt );
		//            cachedzBuffer.Dispose();
		//        }
		//    }

		//    if ( zbuffer == null )
		//    {
		//        // If not, create the depthstencil surface
		//        zbuffer = new XFG.DepthStencilBuffer( _device, width, height, dsfmt, multisample, 0 );
		//        zBufferCache[ zbfmt ] = zbuffer;
		//    }

		//    return zbuffer;
		//}

		#endregion Methods

		#endregion Axiom.Core.RenderSystem Implementation

		#region IServiceProvider Members

		public object GetService( Type serviceType )
		{
			if ( serviceType == typeof( Microsoft.Xna.Framework.Graphics.IGraphicsDeviceService ) )
			{
				foreach ( var item in this.renderTargets )
				{
					var renderTarget = item.Value as RenderTarget;
					XFG.IGraphicsDeviceService service = renderTarget as XFG.IGraphicsDeviceService;
					if ( service != null )
						return service;
				}
			}

			return null;
		}

		#endregion
	}
}
