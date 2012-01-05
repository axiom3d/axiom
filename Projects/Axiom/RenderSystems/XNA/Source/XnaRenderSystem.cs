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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
#if !(XBOX || XBOX360 || SILVERLIGHT )
using System.Windows.Forms;
#endif
using Axiom.Core;
using Axiom.Configuration;
using Axiom.Graphics;
using Axiom.Math;
using Axiom.Overlays;

using Microsoft.Xna.Framework.GamerServices;

using XNA = Microsoft.Xna.Framework;
using XFG = Microsoft.Xna.Framework.Graphics;

using Axiom.Collections;
using Axiom.Core.Collections;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.Xna
{
	///<summary>
	///</summary>
	public class XnaRenderSystem : RenderSystem, IServiceProvider
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

		private Driver _activeDriver;

		private XFG.GraphicsDeviceCapabilities _capabilities;

		/// Saved last view matrix
		protected Matrix4 _viewMatrix = Matrix4.Identity;

		private bool _isFirstFrame = true;
		private int _primCount;
		private int _renderCount = 0;

		/// <summary>
		/// The one used to crfeate the device.
		/// </summary>
		private XnaRenderWindow _primaryWindow;

		private List<XnaRenderWindow> _secondaryWindows = new List<XnaRenderWindow>();

		// stores texture stage info locally for convenience
		internal XnaTextureStageDescription[] texStageDesc = new XnaTextureStageDescription[Config.MaxTextureLayers];
		protected XnaGpuProgramManager gpuProgramMgr;
		private int numLastStreams = 0;

		/// <summary>
		///    Number of streams used last frame, used to unbind any buffers not used during the current operation.
		/// </summary>
		private int _lastVertexSourceCount;

		// Fixed Function Emulation
#if !(XBOX || XBOX360 || SILVERLIGHT)
		private FixedFunctionEmulation.ShaderManager _shaderManager = new FixedFunctionEmulation.ShaderManager();
		private FixedFunctionEmulation.HLSLShaderGenerator _hlslShaderGenerator = new FixedFunctionEmulation.HLSLShaderGenerator();
		private FixedFunctionEmulation.FixedFunctionState _fixedFunctionState = new FixedFunctionEmulation.FixedFunctionState();
		private FixedFunctionEmulation.HLSLFixedFunctionProgram _fixedFunctionProgram; //= new Axiom.RenderSystems.Xna.FixedFunctionEmulation.HLSLFixedFunctionProgram();
		private FixedFunctionEmulation.FixedFunctionPrograms.FixedFunctionProgramsParameters _ffProgramParameters = new FixedFunctionEmulation.FixedFunctionPrograms.FixedFunctionProgramsParameters();
#endif
		private bool _useNVPerfHUD;
		private bool _vSync;
		private XFG.MultiSampleType _fsaaType = XFG.MultiSampleType.None;
		private int _fsaaQuality = 0;

		protected int primCount;
		// protected int renderCount = 0;

		#region Construction and Destruction

		public XnaRenderSystem()
			: base()
		{
			_initConfigOptions();
			// init the texture stage descriptions
			for( int i = 0; i < Config.MaxTextureLayers; i++ )
			{
				texStageDesc[ i ].autoTexCoordType = TexCoordCalcMethod.None;
				texStageDesc[ i ].coordIndex = 0;
				texStageDesc[ i ].texType = TextureType.OneD;
				texStageDesc[ i ].tex = null;
			}
#if !(XBOX || XBOX360 || SILVERLIGHT)
			_shaderManager.RegisterGenerator( _hlslShaderGenerator );
#endif
		}

		#endregion Construction and Destruction

		#region Helper Methods

		/// <summary>
		/// 
		/// </summary>
		private void _setVertexBufferBinding( VertexBufferBinding binding )
		{
			Dictionary<short, HardwareVertexBuffer> bindings = binding.Bindings;

			// TODO: Optimize to remove enumeration if possible, although with so few iterations it may never make a difference
			foreach( short stream in bindings.Keys )
			{
				XnaHardwareVertexBuffer buffer = (XnaHardwareVertexBuffer)bindings[ stream ];

				_device.Vertices[ stream ].SetSource( buffer.XnaVertexBuffer, 0, buffer.VertexSize );

				_lastVertexSourceCount++;
			}

			// Unbind any unused sources
			for( int i = binding.BindingCount; i < _lastVertexSourceCount; i++ )
			{
				_device.Vertices[ i ].SetSource( null, 0, 0 );
			}

			_lastVertexSourceCount = binding.BindingCount;
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

		public override void SetConfigOption( string name, string value )
		{
			if( ConfigOptions.ContainsKey( name ) )
			{
				ConfigOptions[ name ].Value = value;
			}
		}

		private void _configOptionChanged( string name, string value )
		{
			LogManager.Instance.Write( "XNA : RenderSystem Option: {0} = {1}", name, value );

			bool viewModeChanged = false;

			// Find option
			ConfigOption opt = ConfigOptions[ name ];

			// Refresh other options if D3DDriver changed
			if( name == "Rendering Device" )
			{
				_refreshXnaSettings();
			}

			if( name == "Full Screen" )
			{
				// Video mode is applicable
				opt = ConfigOptions[ "Video Mode" ];
				if( opt.Value == "" )
				{
					opt.Value = "800 x 600 @ 32-bit color";
					viewModeChanged = true;
				}
			}

			if( name == "Anti aliasing" )
			{
				if( value == "None" )
				{
					_setFSAA( XFG.MultiSampleType.None, 0 );
				}
				else
				{
					XFG.MultiSampleType fsaa = XFG.MultiSampleType.None;
					int level = 0;

					if( value.StartsWith( "NonMaskable" ) )
					{
						fsaa = XFG.MultiSampleType.NonMaskable;
						level = Int32.Parse( value.Substring( value.LastIndexOf( " " ) ) );
						level -= 1;
					}
					else if( value.StartsWith( "Level" ) )
					{
						fsaa = (XFG.MultiSampleType)Int32.Parse( value.Substring( value.LastIndexOf( " " ) ) );
					}

					_setFSAA( fsaa, level );
				}
			}

			if( name == "VSync" )
			{
				_vSync = ( value == "Yes" );
			}

			if( name == "Allow NVPerfHUD" )
			{
				_useNVPerfHUD = ( value == "Yes" );
			}

			if( viewModeChanged || name == "Video Mode" )
			{
				_refreshFSAAOptions();
			}
		}

		private void _setFSAA( XFG.MultiSampleType fsaa, int level )
		{
			if( _device == null )
			{
				_fsaaType = fsaa;
				_fsaaQuality = level;
			}
		}

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
			foreach( Driver driver in driverList )
			{
				if( !optDevice.PossibleValues.ContainsKey( driver.AdapterNumber ) )
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
			if( driver != null )
			{
				// Get Current Selection
				ConfigOption optVideoMode = ConfigOptions[ "Video Mode" ];
				string curMode = optVideoMode.Value;

				// Clear previous Modes
				optVideoMode.PossibleValues.Clear();

				// Get Video Modes for current device;
				foreach( VideoMode videoMode in driver.VideoModes )
				{
					optVideoMode.PossibleValues.Add( optVideoMode.PossibleValues.Count, videoMode.ToString() );
				}

				// Reset video mode to default if previous doesn't avail in new possible values

				if( optVideoMode.PossibleValues.Values.Contains( curMode ) == false )
				{
					optVideoMode.Value = "800 x 600 @ 32-bit color";
				}

				// Also refresh FSAA options
				_refreshFSAAOptions();
			}
		}

		private void _refreshFSAAOptions()
		{
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
			if( driver != null )
			{
				ConfigOption optVideoMode = ConfigOptions[ "Video Mode" ];
				VideoMode videoMode = driver.VideoModes[ optVideoMode.Value ];
				if( videoMode != null )
				{
					int numLevels = 0;

					// get non maskable levels supported for this VMODE
					if( driver.Adapter.CheckDeviceMultiSampleType( XFG.DeviceType.Hardware, videoMode.Format, windowed, XFG.MultiSampleType.NonMaskable, out numLevels ) )
					{
						for( int n = 0; n < numLevels; n++ )
						{
							optFSAA.PossibleValues.Add( optFSAA.PossibleValues.Count, String.Format( "NonMaskable {0}", n ) );
						}
					}

					// get maskable levels supported for this VMODE
					for( int n = 2; n < 17; n++ )
					{
						if( driver.Adapter.CheckDeviceMultiSampleType( XFG.DeviceType.Hardware, videoMode.Format, windowed, (XFG.MultiSampleType)n ) )
						{
							optFSAA.PossibleValues.Add( optFSAA.PossibleValues.Count, String.Format( "Level {0}", n ) );
						}
					}
				}
			}

			// Reset FSAA to none if previous doesn't avail in new possible values
			if( optFSAA.PossibleValues.Values.Contains( curFSAA ) == false )
			{
				optFSAA.Value = "None";
			}
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

			if( fullScreen )
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
		private void _checkHardwareCapabilities( XFG.GraphicsDevice device )
		{
			_capabilities = _device.GraphicsDeviceCapabilities;

			// get the number of possible texture units
			HardwareCapabilities.TextureUnitCount = _capabilities.MaxSimultaneousTextures;

			// max active lights
			HardwareCapabilities.MaxLights = 8;

			XFG.DepthStencilBuffer surface = device.DepthStencilBuffer;

			if( surface.Format == XFG.DepthFormat.Depth24Stencil8 || surface.Format == XFG.DepthFormat.Depth24 )
			{
				HardwareCapabilities.SetCapability( Capabilities.StencilBuffer );
				// always 8 here
				HardwareCapabilities.StencilBufferBitCount = 8;
			}

			// some cards, oddly enough, do not support this
			if( _capabilities.DeclarationTypeCapabilities.SupportsByte4 )
			{
				HardwareCapabilities.SetCapability( Capabilities.VertexFormatUByte4 );
			}

			// Anisotropy?
			if( _capabilities.MaxAnisotropy > 1 )
			{
				HardwareCapabilities.SetCapability( Capabilities.AnisotropicFiltering );
			}

			// Hardware mipmapping?
			if( _capabilities.DriverCapabilities.CanAutoGenerateMipMap )
			{
				HardwareCapabilities.SetCapability( Capabilities.HardwareMipMaps );
			}

			// blending between stages is definately supported
			HardwareCapabilities.SetCapability( Capabilities.TextureBlending );
			HardwareCapabilities.SetCapability( Capabilities.MultiTexturing );

			// Dot3 bump mapping?
			//if ( _capabilities.TextureCapabilities.SupportsDotProduct3 )
			//{
			//    HardwareCapabilities.SetCap( Capabilities.Dot3 );
			//}

			// Cube mapping?
			if( _capabilities.TextureCapabilities.SupportsCubeMap )
			{
				HardwareCapabilities.SetCapability( Capabilities.CubeMapping );
			}

			// Texture Compression
			// We always support compression, Xna will decompress if device does not support
			HardwareCapabilities.SetCapability( Capabilities.TextureCompression );
			HardwareCapabilities.SetCapability( Capabilities.TextureCompressionDXT );

			// Xna uses vertex buffers for everything
			HardwareCapabilities.SetCapability( Capabilities.VertexBuffer );

			// Scissor test
			if( _capabilities.RasterCapabilities.SupportsScissorTest )
			{
				HardwareCapabilities.SetCapability( Capabilities.ScissorTest );
			}

			// 2 sided stencil
			if( _capabilities.StencilCapabilities.SupportsTwoSided )
			{
				HardwareCapabilities.SetCapability( Capabilities.TwoSidedStencil );
			}

			// stencil wrap
			if( _capabilities.StencilCapabilities.SupportsIncrement && _capabilities.StencilCapabilities.SupportsDecrement )
			{
				HardwareCapabilities.SetCapability( Capabilities.StencilWrap );
			}

			// Hardware Occlusion, new!
			XFG.OcclusionQuery oQuery = new Microsoft.Xna.Framework.Graphics.OcclusionQuery( device );
			if( oQuery.IsSupported )
			{
				HardwareCapabilities.SetCapability( Capabilities.HardwareOcculusion );
			}
			oQuery.Dispose();

			if( _capabilities.MaxUserClipPlanes > 0 )
			{
				HardwareCapabilities.SetCapability( Capabilities.UserClipPlanes );
			}

			_checkVertexProgramCapabilities();

			_checkFragmentProgramCapabilities();

			// Infinite projection?
			// We have no capability for this, so we have to base this on our
			// experience and reports from users
			// Non-vertex program capable hardware does not appear to support it
			if( HardwareCapabilities.HasCapability( Capabilities.VertexPrograms ) )
			{
				// GeForce4 Ti (and presumably GeForce3) does not
				// render infinite projection properly, even though it does in GL
				// So exclude all cards prior to the FX range from doing infinite

				XFG.GraphicsAdapter details = XFG.GraphicsAdapter.Adapters[ 0 ];

				// not nVidia or GeForceFX and above
				if( details.VendorId != 0x10DE || details.DeviceId >= 0x0301 )
				{
					HardwareCapabilities.SetCapability( Capabilities.InfiniteFarPlane );
				}
			}

			// write hardware capabilities to registered log listeners
			HardwareCapabilities.Log();
		}

		private void _checkFragmentProgramCapabilities()
		{
			int fpMajor = _capabilities.PixelShaderVersion.Major;
			int fpMinor = _capabilities.PixelShaderVersion.Minor;

			switch( fpMajor )
			{
				case 1:
					HardwareCapabilities.MaxFragmentProgramVersion = string.Format( "ps_1_{0}", fpMinor );

					HardwareCapabilities.FragmentProgramConstantIntCount = 0;
					// 8 4d float values, entered as floats but stored as fixed
					HardwareCapabilities.FragmentProgramConstantFloatCount = 8;
					break;

				case 2:
					if( fpMinor > 0 )
					{
						HardwareCapabilities.MaxFragmentProgramVersion = "ps_2_x";
						//16 integer params allowed
						HardwareCapabilities.FragmentProgramConstantIntCount = 16 * 4;
						// 4d float params
						HardwareCapabilities.FragmentProgramConstantFloatCount = 224;
					}
					else
					{
						HardwareCapabilities.MaxFragmentProgramVersion = "ps_2_0";
						// no integer params allowed
						HardwareCapabilities.FragmentProgramConstantIntCount = 0;
						// 4d float params
						HardwareCapabilities.FragmentProgramConstantFloatCount = 32;
					}

					break;

				case 3:
					if( fpMinor > 0 )
					{
						HardwareCapabilities.MaxFragmentProgramVersion = "ps_3_x";
					}
					else
					{
						HardwareCapabilities.MaxFragmentProgramVersion = "ps_3_0";
					}

					// 16 integer params allowed
					HardwareCapabilities.FragmentProgramConstantIntCount = 16;
					HardwareCapabilities.FragmentProgramConstantFloatCount = 224;
					break;

				default:
					// doh, SOL
					HardwareCapabilities.MaxFragmentProgramVersion = "";
					break;
			}

			// Fragment Program syntax code checks
			if( fpMajor >= 1 )
			{
				HardwareCapabilities.SetCapability( Capabilities.FragmentPrograms );
				this.gpuProgramMgr.PushSyntaxCode( "ps_1_1" );

				if( fpMajor > 1 || fpMinor >= 2 )
				{
					this.gpuProgramMgr.PushSyntaxCode( "ps_1_2" );
				}
				if( fpMajor > 1 || fpMinor >= 3 )
				{
					this.gpuProgramMgr.PushSyntaxCode( "ps_1_3" );
				}
				if( fpMajor > 1 || fpMinor >= 4 )
				{
					this.gpuProgramMgr.PushSyntaxCode( "ps_1_4" );
				}
			}

			if( fpMajor >= 2 )
			{
				this.gpuProgramMgr.PushSyntaxCode( "ps_2_0" );

				if( fpMinor > 0 )
				{
					this.gpuProgramMgr.PushSyntaxCode( "ps_2_x" );
				}
			}

			if( fpMajor >= 3 )
			{
				this.gpuProgramMgr.PushSyntaxCode( "ps_3_0" );

				if( fpMinor > 0 )
				{
					this.gpuProgramMgr.PushSyntaxCode( "ps_3_x" );
				}
			}
		}

		private void _checkVertexProgramCapabilities()
		{
			int vpMajor = _capabilities.VertexShaderVersion.Major;
			int vpMinor = _capabilities.VertexShaderVersion.Minor;

			// check vertex program HardwareCapabilities
			switch( vpMajor )
			{
				case 1:
					HardwareCapabilities.MaxVertexProgramVersion = "vs_1_1";
					// 4d float vectors
					HardwareCapabilities.VertexProgramConstantFloatCount = _capabilities.MaxVertexShaderConstants;
					// no int params supports
					HardwareCapabilities.VertexProgramConstantIntCount = 0;
					break;
				case 2:
					if( vpMinor > 0 )
					{
						HardwareCapabilities.MaxVertexProgramVersion = "vs_2_x";
					}
					else
					{
						HardwareCapabilities.MaxVertexProgramVersion = "vs_2_0";
					}

					// 16 ints
					HardwareCapabilities.VertexProgramConstantIntCount = 16 * 4;
					// 4d float vectors
					HardwareCapabilities.VertexProgramConstantFloatCount = _capabilities.MaxVertexShaderConstants;

					break;
				case 3:
					HardwareCapabilities.MaxVertexProgramVersion = "vs_3_0";

					// 16 ints
					HardwareCapabilities.VertexProgramConstantIntCount = 16 * 4;
					// 4d float vectors
					HardwareCapabilities.VertexProgramConstantFloatCount = _capabilities.MaxVertexShaderConstants;

					break;
				default:
					// not gonna happen
					HardwareCapabilities.MaxVertexProgramVersion = "";
					break;
			}

			// check for supported vertex program syntax codes
			if( vpMajor >= 1 )
			{
				HardwareCapabilities.SetCapability( Capabilities.VertexPrograms );
				this.gpuProgramMgr.PushSyntaxCode( "vs_1_1" );
			}
			if( vpMajor >= 2 )
			{
				if( vpMajor > 2 || vpMinor > 0 )
				{
					this.gpuProgramMgr.PushSyntaxCode( "vs_2_x" );
				}
				this.gpuProgramMgr.PushSyntaxCode( "vs_2_0" );
			}
			if( vpMajor >= 3 )
			{
				this.gpuProgramMgr.PushSyntaxCode( "vs_3_0" );
			}
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

		#region Axiom.Core.RenderSystem Implementation

		#region Properties

		private ColorEx _ambientLight = ColorEx.White;

		public override ColorEx AmbientLight
		{
			get { return _ambientLight; }
			set
			{
				_ambientLight = value;
#if !(XBOX || XBOX360 || SILVERLIGHT)
				_ffProgramParameters.LightAmbient = value;
#endif
			}
		}

		public override CullingMode CullingMode
		{
			get { throw new Exception( "The method or operation is not implemented." ); }
			set
			{
				cullingMode = value;

				bool flip = activeRenderTarget.RequiresTextureFlipping ^ invertVertexWinding;
				_device.RenderState.CullMode = XnaHelper.Convert( value, flip );
			}
		}

		public override bool DepthWrite { get { throw new Exception( "The method or operation is not implemented." ); } set { _device.RenderState.DepthBufferWriteEnable = value; } }

		public override bool DepthCheck { get { throw new Exception( "The method or operation is not implemented." ); } set { _device.RenderState.DepthBufferEnable = value; } }

		public override CompareFunction DepthFunction { get { throw new Exception( "The method or operation is not implemented." ); } set { _device.RenderState.DepthBufferFunction = XnaHelper.Convert( value ); } }

		public override float DepthBias { get { throw new Exception( "The method or operation is not implemented." ); } set { _device.RenderState.DepthBias = (float)value; } }

		public override float HorizontalTexelOffset
		{
			// Xna considers the origin to be in the center of a pixel?
			get { return -0.5f; }
		}

		private bool _lightingEnabled;

		public override bool LightingEnabled
		{
			get { return _lightingEnabled; }
			set
			{
				_lightingEnabled = value;
#if !(XBOX || XBOX360 || SILVERLIGHT)
				_ffProgramParameters.LightingEnabled = value;
#endif
			}
		}

		public override bool NormalizeNormals
		{
			get { throw new Exception( "The method or operation is not implemented." ); }
			set
			{
				//throw new Exception( "The method or operation is not implemented." );
			}
		}

		private Matrix4 _projectionMatrix;

		public override Matrix4 ProjectionMatrix
		{
			get { return _projectionMatrix; }
			set
			{
				_projectionMatrix = value;

				/*XNA.Matrix mat = XnaHelper.Convert(value);

				if (activeRenderTarget.RequiresTextureFlipping)
				{
					mat.M22 = -mat.M22;
				}*/

#if !(XBOX || XBOX360 || SILVERLIGHT)
				_ffProgramParameters.ProjectionMatrix = value;
#endif
			}
		}

		public override PolygonMode PolygonMode
		{
			get { throw new Exception( "The method or operation is not implemented." ); }
			set
			{
				switch( value )
				{
					case PolygonMode.Points:
						_device.RenderState.FillMode = XFG.FillMode.Point;
						break;
					case PolygonMode.Wireframe:
						_device.RenderState.FillMode = XFG.FillMode.WireFrame;
						break;
					case PolygonMode.Solid:
						_device.RenderState.FillMode = XFG.FillMode.Solid;
						break;
				}
			}
		}

		public override Shading ShadingMode
		{
			get { throw new Exception( "The method or operation is not implemented." ); }
			set
			{
				//throw new Exception("The method or operation is not implemented.");
			}
		}

		public override bool StencilCheckEnabled { get { throw new Exception( "The method or operation is not implemented." ); } set { _device.RenderState.StencilEnable = value; } }

		public override float VerticalTexelOffset
		{
			get
			{
				// Xna considers the origin to be in the center of a pixel ?
				return -0.5f;
			}
		}

		public override Matrix4 ViewMatrix
		{
			get { return _viewMatrix; }
			set
			{
				// flip the transform portion of the matrix for DX and its left-handed coord system
				// save latest view matrix
				_viewMatrix = value;
				_viewMatrix.m20 = -_viewMatrix.m20;
				_viewMatrix.m21 = -_viewMatrix.m21;
				_viewMatrix.m22 = -_viewMatrix.m22;
				_viewMatrix.m23 = -_viewMatrix.m23;

#if !(XBOX || XBOX360 || SILVERLIGHT)
				_ffProgramParameters.ViewMatrix = _viewMatrix;
#endif
			}
		}

		private Matrix4 _worldMatrix;

		public override Axiom.Math.Matrix4 WorldMatrix
		{
			get { return _worldMatrix; }
			set
			{
				_worldMatrix = value;
#if !(XBOX || XBOX360 || SILVERLIGHT)
				_ffProgramParameters.WorldMatrix = _worldMatrix;
#endif
			}
		}

		#endregion Properties

		#region Methods

		public override Real MinimumDepthInputValue
		{
			get
			{
				// Range [0.0f, 1.0f]
				return 0.0f;
			}
		}

		public override Real MaximumDepthInputValue
		{
			get
			{
				// Range [0.0f, 1.0f]
				// XNA inverts even identity view matrixes so maximum INPUT is -1.0f
				return -1.0f;
			}
		}

		public override void ApplyObliqueDepthProjection( ref Axiom.Math.Matrix4 projMatrix, Axiom.Math.Plane plane, bool forGpuProgram )
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
			Axiom.Math.Vector4 q = new Axiom.Math.Vector4();
			q.x = System.Math.Sign( plane.Normal.x ) / projMatrix.m00;
			q.y = System.Math.Sign( plane.Normal.y ) / projMatrix.m11;
			q.z = 1.0f;

			// flip the next bit from Lengyel since we're right-handed
			if( forGpuProgram )
			{
				q.w = ( 1.0f - projMatrix.m22 ) / projMatrix.m23;
			}
			else
			{
				q.w = ( 1.0f + projMatrix.m22 ) / projMatrix.m23;
			}

			// Calculate the scaled plane vector
			Axiom.Math.Vector4 clipPlane4d =
				new Axiom.Math.Vector4( plane.Normal.x, plane.Normal.y, plane.Normal.z, plane.D );

			Axiom.Math.Vector4 c = clipPlane4d * ( 1.0f / ( clipPlane4d.Dot( q ) ) );

			// Replace the third row of the projection matrix
			projMatrix.m20 = c.x;
			projMatrix.m21 = c.y;

			// flip the next bit from Lengyel since we're right-handed
			if( forGpuProgram )
			{
				projMatrix.m22 = c.z;
			}
			else
			{
				projMatrix.m22 = -c.z;
			}

			projMatrix.m23 = c.w;
		}

		public override void BeginFrame()
		{
			Debug.Assert( activeViewport != null, "BeingFrame cannot run without an active viewport." );

			// set initial render states if this is the first frame. we only want to do 
			//	this once since renderstate changes are expensive
			if( _isFirstFrame )
			{
				// enable alpha blending and specular materials
				_device.RenderState.AlphaBlendEnable = true;
				//_device.RenderState.SpecularEnable = true;
				_device.RenderState.DepthBufferEnable = true;
				_isFirstFrame = false;
			}
		}

		private bool VertexShaderIsSet = false;
		private bool PixelShaderIsSet = false;

		public override void BindGpuProgram( GpuProgram program )
		{
			switch( program.Type )
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
		}

		public override void BindGpuProgramParameters( GpuProgramType type, GpuProgramParameters parms )
		{
			switch( type )
			{
				case GpuProgramType.Vertex:
					if( parms.HasIntConstants )
					{
						for( int index = 0; index < parms.IntConstantCount; index++ )
						{
							GpuProgramParameters.IntConstantEntry entry = parms.GetIntConstant( index );

							if( entry.isSet )
							{
								_device.SetVertexShaderConstant( index, entry.val );
							}
						}
					}
					if( parms.HasFloatConstants )
					{
						for( int index = 0; index < parms.FloatConstantCount; index++ )
						{
							GpuProgramParameters.FloatConstantEntry entry = parms.GetFloatConstant( index );

							if( entry.isSet )
							{
								_device.SetVertexShaderConstant( index, entry.val );
							}
						}
					}
					break;
				case GpuProgramType.Fragment:
					if( parms.HasIntConstants )
					{
						for( int index = 0; index < parms.IntConstantCount; index++ )
						{
							GpuProgramParameters.IntConstantEntry entry = parms.GetIntConstant( index );

							if( entry.isSet )
							{
								_device.SetPixelShaderConstant( index, entry.val );
							}
						}
					}

					if( parms.HasFloatConstants )
					{
						for( int index = 0; index < parms.FloatConstantCount; index++ )
						{
							GpuProgramParameters.FloatConstantEntry entry = parms.GetFloatConstant( index );

							if( entry.isSet )
							{
								_device.SetPixelShaderConstant( index, entry.val );
							}
						}
					}
					break;
			}
		}

		public override void ClearFrameBuffer( FrameBufferType buffers, ColorEx color, float depth, int stencil )
		{
			XFG.ClearOptions flags = 0; //ClearFlags 

			if( ( buffers & FrameBufferType.Color ) > 0 )
			{
				flags |= XFG.ClearOptions.Target;
			}
			if( ( buffers & FrameBufferType.Depth ) > 0 )
			{
				flags |= XFG.ClearOptions.DepthBuffer;
			}
			// Only try to clear the stencil buffer if supported
			if( ( buffers & FrameBufferType.Stencil ) > 0
			    && HardwareCapabilities.HasCapability( Capabilities.StencilBuffer ) )
			{
				flags |= XFG.ClearOptions.Stencil;
			}
			XFG.Color col = new XFG.Color( (byte)( color.r * 255.0f ), (byte)( color.g * 255.0f ), (byte)( color.b * 255.0f ), (byte)( color.a * 255.0f ) );

			// clear the device using the specified params
			_device.Clear( flags, col, depth, stencil );
		}

		public override int ConvertColor( ColorEx color )
		{
			return color.ToARGB();
		}

		public override ColorEx ConvertColor( int color )
		{
			ColorEx colorEx;
			colorEx.a = (float)( ( color >> 24 ) % 256 ) / 255;
			colorEx.r = (float)( ( color >> 16 ) % 256 ) / 255;
			colorEx.g = (float)( ( color >> 8 ) % 256 ) / 255;
			colorEx.b = (float)( ( color ) % 256 ) / 255;
			return colorEx;
		}

		public override RenderWindow CreateRenderWindow( string name, int width, int height, bool isFullScreen, NamedParameterList miscParams )
		{
			// Check we're not creating a secondary window when the primary
			// was fullscreen
			if( _primaryWindow != null && _primaryWindow.IsFullScreen )
			{
				throw new Exception( "Cannot create secondary windows when the primary is full screen." );
			}

			if( _primaryWindow != null && isFullScreen )
			{
				throw new ArgumentException( "Cannot create full screen secondary windows." );
			}

			// Log a message
			System.Text.StringBuilder strParams = new System.Text.StringBuilder();
			if( miscParams != null )
			{
				foreach( KeyValuePair<string, object> entry in miscParams )
				{
					strParams.AppendFormat( "{0} = {1}; ", entry.Key, entry.Value );
				}
			}
			LogManager.Instance.Write( "[XNA] : Creating RenderWindow \"{0}\", {1}x{2} {3} miscParams: {4}",
			                           name, width, height, isFullScreen ? "fullscreen" : "windowed", strParams.ToString() );

			// Make sure we don't already have a render target of the 
			// same name as the one supplied
			if( renderTargets.ContainsKey( name ) )
			{
				throw new Exception( String.Format( "A render target of the same name '{0}' already exists." +
				                                    "You cannot create a new window with this name.", name ) );
			}

			RenderWindow window = new XnaRenderWindow( _activeDriver, _primaryWindow != null ? this._device : null );

			// create the window
			window.Create( name, width, height, isFullScreen, miscParams );

			// add the new render target
			AttachRenderTarget( window );
			// If this is the first window, get the D3D device and create the texture manager
			if( _primaryWindow == null )
			{
				_primaryWindow = (XnaRenderWindow)window;
				_device = (XFG.GraphicsDevice)window[ "XNADEVICE" ];

				// Create the texture manager for use by others
				textureManager = new XnaTextureManager( _device );
				// Also create hardware buffer manager
				hardwareBufferManager = new XnaHardwareBufferManager( _device );

				// Create the GPU program manager
				gpuProgramMgr = new XnaGpuProgramManager( _device );
				// create & register HLSL factory
				//HLSLProgramFactory = new D3D9HLSLProgramFactory();
				//HighLevelGpuProgramManager::getSingleton().addFactory(mHLSLProgramFactory);
				gpuProgramMgr.PushSyntaxCode( "hlsl" );

				// Initialize the capabilities structures
				this._checkHardwareCapabilities( _device );
			}
			else
			{
				_secondaryWindows.Add( (XnaRenderWindow)window );
			}

			return window;
		}

		public override MultiRenderTarget CreateMultiRenderTarget( string name )
		{
			throw new NotImplementedException();
		}

		public override void Shutdown()
		{
			_activeDriver = null;

			// dispose of the device
			if( _device != null )
			{
				if( !_device.IsDisposed )
				{
					_device.Dispose();
				}

				_device = null;
			}

			if( gpuProgramMgr != null )
			{
				if( !gpuProgramMgr.IsDisposed )
				{
					gpuProgramMgr.Dispose();
				}

				gpuProgramMgr = null;
			}

			if( hardwareBufferManager != null )
			{
				if( !hardwareBufferManager.IsDisposed )
				{
					hardwareBufferManager.Dispose();
				}

				hardwareBufferManager = null;
			}

			if( textureManager != null )
			{
				if( !textureManager.IsDisposed )
				{
					textureManager.Dispose();
				}

				textureManager = null;
			}

			base.Shutdown();

			LogManager.Instance.Write( "[XNA] : " + Name + " shutdown." );
		}

		private XFG.GraphicsDevice InitDevice( bool isFullscreen, bool depthBuffer, int width, int height, int colorDepth, IntPtr target )
		{
			if( _device != null )
			{
				return _device;
			}

			XFG.GraphicsDevice newDevice;

			// if this is the first window, get the device and do other initialization
			XFG.PresentationParameters presentParams = new XFG.PresentationParameters();
			presentParams.IsFullScreen = isFullscreen;
			presentParams.BackBufferCount = 1;
			presentParams.EnableAutoDepthStencil = depthBuffer;
			presentParams.BackBufferWidth = width;
			presentParams.BackBufferHeight = height;
			presentParams.MultiSampleType = XFG.MultiSampleType.None;
			presentParams.SwapEffect = XFG.SwapEffect.Copy;

			// TODO: Check vsync setting
			presentParams.PresentationInterval = XFG.PresentInterval.Immediate;

			// supports 16 and 32 bit color
			if( colorDepth == 16 )
			{
				presentParams.BackBufferFormat = XFG.SurfaceFormat.Bgr565;
			}
			else
			{
				//could not create depth24stencil8 with "Color"
				presentParams.BackBufferFormat = XFG.SurfaceFormat.Bgr32;
			}

			if( colorDepth > 16 )
			{
				if( XFG.GraphicsAdapter.DefaultAdapter.CheckDepthStencilMatch( XFG.DeviceType.Hardware, presentParams.BackBufferFormat, presentParams.BackBufferFormat, XFG.DepthFormat.Depth24Stencil8 ) )
				{
					presentParams.AutoDepthStencilFormat = XFG.DepthFormat.Depth24Stencil8;
				}
				// check for 24 bit Z buffer with 8 bit stencil (optimal choice)
				if( !XFG.GraphicsAdapter.DefaultAdapter.CheckDeviceFormat(
				                                                          XFG.DeviceType.Hardware,
				                                                          XFG.GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Format,
				                                                          XFG.TextureUsage.None,
				                                                          XFG.QueryUsages.None,
				                                                          XFG.ResourceType.DepthStencilBuffer,
				                                                          XFG.DepthFormat.Depth24Stencil8 ) )
				{
					// doh, check for 32 bit Z buffer then

					if( !XFG.GraphicsAdapter.DefaultAdapter.CheckDeviceFormat(
					                                                          XFG.DeviceType.Hardware,
					                                                          XFG.GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Format,
					                                                          XFG.TextureUsage.None,
					                                                          XFG.QueryUsages.None,
					                                                          XFG.ResourceType.DepthStencilBuffer,
					                                                          XFG.DepthFormat.Depth32 ) )
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
					// <flair>Woooooooooo!</flair>
					if( XFG.GraphicsAdapter.DefaultAdapter.CheckDepthStencilMatch(
					                                                              XFG.DeviceType.Hardware,
					                                                              XFG.GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Format,
					                                                              presentParams.BackBufferFormat,
					                                                              XFG.DepthFormat.Depth24Stencil8 ) )
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

			// create the D3D Device, trying for the best vertex support first, and settling for less if necessary
			try
			{
				// hardware vertex processing
				newDevice = new XFG.GraphicsDevice( XFG.GraphicsAdapter.DefaultAdapter,
				                                    XFG.DeviceType.Hardware,
				                                    target,
				                                    presentParams
					);
			}
			catch( Exception )
			{
				try
				{
					// doh, how bout mixed vertex processing
					newDevice = new XFG.GraphicsDevice(
						XFG.GraphicsAdapter.DefaultAdapter,
						XFG.DeviceType.Hardware,
						target,
						presentParams
						);
				}
				catch( XFG.DeviceNotSupportedException )
				{
					// what the...ok, how bout software vertex procssing.  if this fails, then I don't even know how they are seeing
					// anything at all since they obviously don't have a video card installed
					newDevice = new XFG.GraphicsDevice(
						XFG.GraphicsAdapter.DefaultAdapter,
						XFG.DeviceType.Hardware,
						target,
						presentParams
						);
				}
			}

			// CMH - end
			// save the device capabilites
			_capabilities = newDevice.GraphicsDeviceCapabilities;

			// by creating our texture manager, singleton TextureManager will hold our implementation
			this.textureManager = new XnaTextureManager( newDevice );

			// by creating our Gpu program manager, singleton GpuProgramManager will hold our implementation
			gpuProgramMgr = new XnaGpuProgramManager( newDevice );

			// intializes the HardwareBufferManager singleton
			hardwareBufferManager = new XnaHardwareBufferManager( newDevice );

			this._checkHardwareCapabilities( newDevice );

			return newDevice;
		}

		public override HardwareOcclusionQuery CreateHardwareOcclusionQuery()
		{
			return new XnaHardwareOcclusionQuery( _device );
		}

		public override void EndFrame()
		{
			// end the scene
		}

		public override RenderWindow Initialize( bool autoCreateWindow, string windowTitle )
		{
			LogManager.Instance.Write( "[XNA] : Subsystem Initializing" );

#if !( XBOX || XBOX360 )
			WindowEventMonitor.Instance.MessagePump = Win32MessageHandling.MessagePump;
#endif
			_activeDriver = XnaHelper.GetDriverInfo()[ ConfigOptions[ "Rendering Device" ].Value ];
			if( _activeDriver == null )
			{
				throw new ArgumentException( "Problems finding requested Xna driver!" );
			}

			RenderWindow renderWindow = null;

			// register the HLSL program manager
			HighLevelGpuProgramManager.Instance.AddFactory( new HLSL.HLSLProgramFactory() );

			if( autoCreateWindow )
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

#if !(XBOX || XBOX360 || SILVERLIGHT )
				//
				fullScreen = ( ConfigOptions[ "Full Screen" ].Value == "Yes" );
#endif

				NamedParameterList miscParams = new NamedParameterList();
				miscParams.Add( "title", windowTitle );
				miscParams.Add( "colorDepth", bpp );
				miscParams.Add( "FSAA", this._fsaaType );
				miscParams.Add( "FSAAQuality", _fsaaQuality );
				miscParams.Add( "vsync", _vSync );
				miscParams.Add( "useNVPerfHUD", _useNVPerfHUD );

				// create the render window
				renderWindow = CreateRenderWindow( "Main Window", width, height, fullScreen, miscParams );
			}

			LogManager.Instance.Write( "[XNA] : Subsystem Initialized successfully." );
			return renderWindow;
		}

		public override Axiom.Math.Matrix4 MakeOrthoMatrix( float fov, float aspectRatio, float near, float far, bool forGpuPrograms )
		{
			float thetaY = Utility.DegreesToRadians( fov / 2.0f );
			float tanThetaY = Utility.Tan( thetaY );
			float tanThetaX = tanThetaY * aspectRatio;

			float halfW = tanThetaX * near;
			float halfH = tanThetaY * near;

			float w = 1.0f / ( halfW );
			float h = 1.0f / ( halfH );
			float q = 0;

			if( far != 0 )
			{
				q = 1.0f / ( far - near );
			}

			Matrix4 dest = Matrix4.Zero;
			dest.m00 = w;
			dest.m11 = h;
			dest.m22 = q;
			dest.m23 = -near / ( far - near );
			dest.m33 = 1;

			if( forGpuPrograms )
			{
				dest.m22 = -dest.m22;
			}

			return dest;
		}

		public override Matrix4 MakeProjectionMatrix( float fov, float aspectRatio, float near, float far, bool forGpuProgram )
		{
			float theta = Utility.DegreesToRadians( fov * 0.5f );
			float h = 1 / Utility.Tan( theta );
			float w = h / aspectRatio;
			float q = 0;
			float qn = 0;

			if( far == 0 )
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

			if( forGpuProgram )
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

		/// <summary>
		/// Builds a perspective projection matrix for the case when frustum is
		/// not centered around camera.
		/// <remarks>Viewport coordinates are in camera coordinate frame, i.e. camera is at the origin.</remarks>
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <param name="bottom"></param>
		/// <param name="top"></param>
		/// <param name="nearPlane"></param>
		/// <param name="farPlane"></param>
		/// <param name="forGpuProgram"></param>
		public override Matrix4 MakeProjectionMatrix( float left, float right, float bottom, float top, float nearPlane, float farPlane, bool forGpuProgram )
		{
			// Correct position for off-axis projection matrix
			if( !forGpuProgram )
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
			if( farPlane == 0 )
			{
				q = 1 - Frustum.InfiniteFarPlaneAdjust;
				qn = nearPlane * ( Frustum.InfiniteFarPlaneAdjust - 1 );
			}
			else
			{
				q = farPlane / ( farPlane - nearPlane );
				qn = -q * nearPlane;
			}
			Matrix4 dest = Matrix4.Zero;
			dest.m00 = 2 * nearPlane / width;
			dest.m02 = ( right + left ) / width;
			dest.m11 = 2 * nearPlane / height;
			dest.m12 = ( top + bottom ) / height;
			if( forGpuProgram )
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

		public override Matrix4 ConvertProjectionMatrix( Matrix4 mat, bool forGpuProgram )
		{
			Matrix4 dest = new Matrix4( mat.m00, mat.m01, mat.m02, mat.m03,
			                            mat.m10, mat.m11, mat.m12, mat.m13,
			                            mat.m20, mat.m21, mat.m22, mat.m23,
			                            mat.m30, mat.m31, mat.m32, mat.m33 );

			// Convert depth range from [-1,+1] to [0,1]
			dest.m20 = ( dest.m20 + dest.m30 ) / 2.0f;
			dest.m21 = ( dest.m21 + dest.m31 ) / 2.0f;
			dest.m22 = ( dest.m22 + dest.m32 ) / 2.0f;
			dest.m23 = ( dest.m23 + dest.m33 ) / 2.0f;

			if( !forGpuProgram )
			{
				// Convert right-handed to left-handed
				dest.m02 = -dest.m02;
				dest.m12 = -dest.m12;
				dest.m22 = -dest.m22;
				dest.m32 = -dest.m32;
			}

			return dest;
		}

		//XFG.BasicEffect ef;
		public override void SetClipPlane( ushort index, float A, float B, float C, float D )
		{
			throw new NotImplementedException();
		}

		public override void EnableClipPlane( ushort index, bool enable )
		{
			throw new NotImplementedException();
		}

		private bool needToUnmapVS = false;
		private bool needToUnmapFS = false;

		public override void Render( RenderOperation op )
		{
			// don't even bother if there are no vertices to render, causes problems on some cards (FireGL 8800)
			if( op.vertexData.vertexCount == 0 )
			{
				return;
			}

			// class base implementation first
			base.Render( op );

			/*---------------shaders generator part------*/
#if !(XBOX || XBOX360 || SILVERLIGHT )

			if( Root.Instance.RenderSystem.ConfigOptions[ "Use Content Pipeline" ].Value != "Yes" )
			{
				if( !VertexShaderIsSet || !PixelShaderIsSet )
				{
					FixedFunctionEmulation.VertexBufferDeclaration vbd = new FixedFunctionEmulation.VertexBufferDeclaration();
					List<FixedFunctionEmulation.VertexBufferElement> lvbe = new List<FixedFunctionEmulation.VertexBufferElement>( op.vertexData.vertexDeclaration.ElementCount );

					int textureLayer = 0;
					for( int i = 0; i < op.vertexData.vertexDeclaration.ElementCount; i++ )
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

					for( int i = 0; i < Config.MaxTextureLayers; i++ )
					{
						FixedFunctionEmulation.TextureLayerState tls = new FixedFunctionEmulation.TextureLayerState();

						if( texStageDesc[ i ].Enabled ) //if (texStageDesc[i].tex != null)
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
						foreach( Light l in _ffProgramParameters.Lights )
						{
							_fixedFunctionState.Lights.Add( l.Type );
						}

						_fixedFunctionProgram = (FixedFunctionEmulation.HLSLFixedFunctionProgram)_shaderManager.GetShaderPrograms( "hlsl", vbd, _fixedFunctionState );

						_fixedFunctionProgram.FragmentProgramUsage.Program.DefaultParameters.NamedParamCount.ToString();

						_fixedFunctionProgram.SetFixedFunctionProgramParameters( _ffProgramParameters );

						//Bind Vertex Program
						if( !VertexShaderIsSet )
						{
							BindGpuProgram( _fixedFunctionProgram.VertexProgramUsage.Program.BindingDelegate );
							BindGpuProgramParameters( GpuProgramType.Vertex, _fixedFunctionProgram.VertexProgramUsage.Params );
							needToUnmapVS = true;
						}
						// Bind Fragment Program 
						if( !PixelShaderIsSet )
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
			_device.VertexDeclaration = vertDecl.XnaVertexDecl;
			_setVertexBufferBinding( op.vertexData.vertexBufferBinding );

			XFG.PrimitiveType primType = 0;

			switch( op.operationType )
			{
				case OperationType.PointList:
					primType = XFG.PrimitiveType.PointList;
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
					primType = XFG.PrimitiveType.TriangleFan;
					primCount = ( op.useIndices ? op.indexData.indexCount : op.vertexData.vertexCount ) - 2;
					break;
			} // switch(primType)

			// are we gonna use indices?
			if( op.useIndices )
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

			//crap hack, set the sources back to null to allow accessing vertices and indices buffers
			_device.Vertices[ 0 ].SetSource( null, 0, 0 );
			_device.Vertices[ 1 ].SetSource( null, 0, 0 );
			_device.Vertices[ 2 ].SetSource( null, 0, 0 );
			_device.Indices = null;

#if !(XBOX || XBOX360 || SILVERLIGHT )
			/*---------------shaders generator part------*/
			if( needToUnmapVS )
			{
				UnbindGpuProgram( GpuProgramType.Vertex );
			}

			if( needToUnmapFS )
			{
				UnbindGpuProgram( GpuProgramType.Fragment );
			}
			/*--------------------------------------------*/
#endif
		}

		private bool lasta2c = false;

		public override void SetAlphaRejectSettings( CompareFunction func, int val, bool alphaToCoverage )
		{
			bool a2c = false;
			if( func != Axiom.Graphics.CompareFunction.AlwaysPass )
			{
				_device.RenderState.AlphaTestEnable = true;
				a2c = alphaToCoverage;
			}
			else
			{
				_device.RenderState.AlphaTestEnable = false;
			}

			_device.RenderState.AlphaFunction = XnaHelper.Convert( func );
			_device.RenderState.ReferenceAlpha = val;

			// Alpha to coverage
			if( lasta2c != a2c && this.HardwareCapabilities.HasCapability( Capabilities.AlphaToCoverage ) )
			{
				lasta2c = a2c;
			}
		}

		public override void SetColorBufferWriteEnabled( bool red, bool green, bool blue, bool alpha )
		{
			XFG.ColorWriteChannels val = 0;

			if( red )
			{
				val |= XFG.ColorWriteChannels.Red;
			}
			if( green )
			{
				val |= XFG.ColorWriteChannels.Green;
			}
			if( blue )
			{
				val |= XFG.ColorWriteChannels.Blue;
			}
			if( alpha )
			{
				val |= XFG.ColorWriteChannels.Alpha;
			}
			_device.RenderState.ColorWriteChannels = val;
		}

		public override void SetDepthBufferParams( bool depthTest, bool depthWrite, Axiom.Graphics.CompareFunction depthFunction )
		{
			_device.RenderState.DepthBufferEnable = depthTest;
			_device.RenderState.DepthBufferWriteEnable = depthWrite;
			_device.RenderState.DepthBufferFunction = XnaHelper.Convert( depthFunction );
		}

		public override void SetFog( Axiom.Graphics.FogMode mode, ColorEx color, float density, float start, float end )
		{
#if !(XBOX || XBOX360 || SILVERLIGHT )
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
			if( src == SceneBlendFactor.One && dest == SceneBlendFactor.Zero )
			{
				_device.RenderState.AlphaBlendEnable = false;
			}
			else
			{
				_device.RenderState.AlphaBlendEnable = true;
				_device.RenderState.SeparateAlphaBlendEnabled = false;
				_device.RenderState.SourceBlend = XnaHelper.Convert( src );
				_device.RenderState.DestinationBlend = XnaHelper.Convert( dest );
			}
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
			if( sourceFactor == SceneBlendFactor.One && destFactor == SceneBlendFactor.Zero &&
			    sourceFactorAlpha == SceneBlendFactor.One && destFactorAlpha == SceneBlendFactor.Zero )
			{
				_device.RenderState.AlphaBlendEnable = false;
			}
			else
			{
				_device.RenderState.AlphaBlendEnable = true;
				_device.RenderState.SeparateAlphaBlendEnabled = true;
				_device.RenderState.SourceBlend = XnaHelper.Convert( sourceFactor );
				_device.RenderState.DestinationBlend = XnaHelper.Convert( destFactor );
				_device.RenderState.AlphaSourceBlend = XnaHelper.Convert( sourceFactorAlpha );
				_device.RenderState.AlphaDestinationBlend = XnaHelper.Convert( destFactorAlpha );
			}
		}

		public override void SetScissorTest( bool enable, int left, int top, int right, int bottom )
		{
			if( enable )
			{
				_device.ScissorRectangle = new XNA.Rectangle( left, top, right - left, bottom - top );
				_device.RenderState.ScissorTestEnable = true;
			}
			else
			{
				_device.RenderState.ScissorTestEnable = false;
			}
		}

		public override void SetStencilBufferParams( Axiom.Graphics.CompareFunction function, int refValue, int mask, Axiom.Graphics.StencilOperation stencilFailOp, Axiom.Graphics.StencilOperation depthFailOp, Axiom.Graphics.StencilOperation passOp, bool twoSidedOperation )
		{
			bool flip;
			// 2 sided operation?
			if( twoSidedOperation )
			{
				if( !HardwareCapabilities.HasCapability( Capabilities.TwoSidedStencil ) )
				{
					throw new AxiomException( "2-sided stencils are not supported on this hardware!" );
				}
				_device.RenderState.TwoSidedStencilMode = true;
				flip = ( invertVertexWinding && activeRenderTarget.RequiresTextureFlipping ) ||
				       ( !invertVertexWinding && !activeRenderTarget.RequiresTextureFlipping );

				_device.RenderState.StencilFail = XnaHelper.Convert( stencilFailOp, !flip );
				_device.RenderState.StencilDepthBufferFail = XnaHelper.Convert( depthFailOp, !flip );
				_device.RenderState.StencilPass = XnaHelper.Convert( passOp, !flip );
			}
			else
			{
				_device.RenderState.TwoSidedStencilMode = false;
				flip = false;
			}

			// configure standard version of the stencil operations
			_device.RenderState.StencilFunction = XnaHelper.Convert( function );
			_device.RenderState.ReferenceStencil = refValue;
			_device.RenderState.StencilMask = mask;
			_device.RenderState.StencilFail = XnaHelper.Convert( stencilFailOp, flip );
			_device.RenderState.StencilDepthBufferFail = XnaHelper.Convert( depthFailOp, flip );
			_device.RenderState.StencilPass = XnaHelper.Convert( passOp, flip );
			//_device.RenderState.ColorWriteChannels = XFG.ColorWriteChannels.None;
		}

		public override void SetSurfaceParams( ColorEx ambient, ColorEx diffuse, ColorEx specular, ColorEx emissive, float shininess, TrackVertexColor tracking )
		{
#if (!(XBOX||XBOX360))
			if( //ambient == ColorEx.White &&
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
		/// Sets whether or not rendering points using PointList will 
		/// render point sprites (textured quads) or plain points.
		/// </summary>
		/// <value></value>
		public override bool PointSprites { set { _device.RenderState.PointSpriteEnable = value; } }

		/// <summary>
		/// Sets the size of points and how they are attenuated with distance.
		/// <remarks>
		/// When performing point rendering or point sprite rendering,
		/// point size can be attenuated with distance. The equation for
		/// doing this is attenuation = 1 / (constant + linear * dist + quadratic * d^2) .
		/// </remarks>
		/// </summary>
		/// <param name="size"></param>
		/// <param name="attenuationEnabled"></param>
		/// <param name="constant"></param>
		/// <param name="linear"></param>
		/// <param name="quadratic"></param>
		/// <param name="minSize"></param>
		/// <param name="maxSize"></param>
		public override void SetPointParameters( float size, bool attenuationEnabled, float constant, float linear, float quadratic, float minSize, float maxSize )
		{
			if( attenuationEnabled )
			{
				//scaling required
				_device.RenderState.PointSpriteEnable = true;
				// NOTE: PointSize scaling is FFP and Xna doesn't support them anymore
				// http://social.msdn.microsoft.com/forums/en-US/xnagamestudioexpress/thread/8adc396e-f7b6-47a9-98a9-7f94e840cd3b/
				// The constant, linear, and quadratic parameters need to be cached and passed into the ShaderGenerator
				// The ShaderGenerator will then need to generate the approtiate values in the vertex shader
			}
			else
			{
				//no scaling required
				_device.RenderState.PointSpriteEnable = false;
			}

			_device.RenderState.PointSize = size;
			_device.RenderState.PointSizeMin = minSize;
			if( maxSize == 0.0f )
			{
				maxSize = HardwareCapabilities.MaxPointSize;
			}
			_device.RenderState.PointSizeMax = maxSize;
		}

		public override void SetTexture( int stage, bool enabled, Texture texture )
		{
			XnaTexture xnaTexture = (XnaTexture)texture;
			texStageDesc[ stage ].Enabled = enabled;
			if( enabled && xnaTexture != null )
			{
				_device.Textures[ stage ] = xnaTexture.DXTexture;
				// set stage description
				texStageDesc[ stage ].tex = xnaTexture.DXTexture;
				texStageDesc[ stage ].texType = xnaTexture.TextureType;
			}
			else
			{
				if( texStageDesc[ stage ].tex != null )
				{
					_device.Textures[ stage ] = null;
				}
				// set stage description to defaults
				texStageDesc[ stage ].tex = null;
				texStageDesc[ stage ].autoTexCoordType = TexCoordCalcMethod.None;
				texStageDesc[ stage ].coordIndex = 0;
				texStageDesc[ stage ].texType = TextureType.OneD;
			}
#if !(XBOX || XBOX360 || SILVERLIGHT )
			_ffProgramParameters.SetTextureEnabled( stage, enabled );
#endif
		}

		public override void SetTextureAddressingMode( int stage, UVWAddressing uvw )
		{
			// set the device sampler states accordingly
			_device.SamplerStates[ stage ].AddressU = XnaHelper.Convert( uvw.U );
			_device.SamplerStates[ stage ].AddressV = XnaHelper.Convert( uvw.V );
			_device.SamplerStates[ stage ].AddressW = XnaHelper.Convert( uvw.W );
		}

		public override void SetTextureBorderColor( int stage, ColorEx borderColor )
		{
			//texStageDesc[ stage ].borderColor = borderColor;
		}

		public override void SetTextureBlendMode( int stage, LayerBlendModeEx blendMode )
		{
			if( blendMode.blendType == LayerBlendType.Color )
			{
				texStageDesc[ stage ].layerBlendMode = blendMode;
			}

			/*if (blendMode.operation == LayerBlendOperationEx.BlendManual)
			{
				_device.RenderState.BlendFactor = new Microsoft.Xna.Framework.Graphics.Color(blendMode.blendFactor, 0, 0, 0);
			}
			if (blendMode.blendType == LayerBlendType.Color)
			{
				_device.RenderState.AlphaBlendEnable = false;
			}
			else if (blendMode.blendType == LayerBlendType.Alpha)
			{
				_device.RenderState.AlphaBlendEnable = true;
			}

			ColorEx manualD3D = ColorEx.White;//XnaHelper.Convert(_device.RenderState.BlendFactor);
			if (blendMode.blendType == LayerBlendType.Color)
			{
				manualD3D = new ColorEx(blendMode.blendFactor, blendMode.colorArg1.r, blendMode.colorArg1.g, blendMode.colorArg1.b);
			}
			else if (blendMode.blendType == LayerBlendType.Alpha)
			{
				manualD3D = new ColorEx(blendMode.alphaArg1, blendMode.blendFactor, blendMode.blendFactor, blendMode.blendFactor);
			}

			LayerBlendSource blendSource = blendMode.source1;
			for (int i = 0; i < 2; i++)
			{
				// set the texture blend factor if this is manual blending
				if (blendSource == LayerBlendSource.Manual)
				{
					_device.RenderState.BlendFactor =  XnaHelper.Convert(manualD3D);
				}
				// pick proper argument settings
				if (blendMode.blendType == LayerBlendType.Color)
				{
					if (i == 0)
					{
						texStageDesc[stage].layerBlendMode.colorArg1 = blendMode.colorArg1; 
					}
					else if (i == 1)
					{
						texStageDesc[stage].layerBlendMode.colorArg2 =  blendMode.colorArg2; 
					}
				}
				else if (blendMode.blendType == LayerBlendType.Alpha)
				{
					if (i == 0)
					{
						   texStageDesc[stage].layerBlendMode.alphaArg1 = blendMode.alphaArg1; 
					}
					else if (i == 1)
					{
						texStageDesc[stage].layerBlendMode.alphaArg2 = blendMode.alphaArg2; 
					}
				}
				// Source2
				blendSource = blendMode.source2;
				if (blendMode.blendType == LayerBlendType.Color)
				{
					manualD3D = new ColorEx(manualD3D.a, blendMode.colorArg2.r, blendMode.colorArg2.g, blendMode.colorArg2.b);
				}
				else if (blendMode.blendType == LayerBlendType.Alpha)
				{
					manualD3D = new ColorEx(blendMode.alphaArg2, manualD3D.r, manualD3D.g, manualD3D.b);
				}
			}*/
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
			if( maxAnisotropy > _capabilities.MaxAnisotropy )
			{
				maxAnisotropy = _capabilities.MaxAnisotropy;
			}

			if( _device.SamplerStates[ stage ].MaxAnisotropy != maxAnisotropy )
			{
				_device.SamplerStates[ stage ].MaxAnisotropy = maxAnisotropy;
			}
		}

		public override void SetTextureMatrix( int stage, Axiom.Math.Matrix4 xform )
		{
			if( texStageDesc[ stage ].autoTexCoordType == TexCoordCalcMethod.ProjectiveTexture )
			{
				//seems like we have to apply a specific transform when we have the frustum
				//and a projective texture
				//from directx rendersystem

				// Derive camera space to projector space transform
				// To do this, we need to undo the camera view matrix, then 
				// apply the projector view & projection matrices

				/* Matrix4 newMat =_viewMatrix.Inverse();
				 texStageDesc[stage].frustum.ViewMatrix = texStageDesc[stage].frustum.ViewMatrix.Transpose();
				 //texStageDesc[stage].frustum.ProjectionMatrix = texStageDesc[stage].frustum.ProjectionMatrix.Transpose();
				 newMat = texStageDesc[stage].frustum.ViewMatrix * newMat;
				 newMat = texStageDesc[stage].frustum.ProjectionMatrix * newMat;
				 newMat = Matrix4.ClipSpace2DToImageSpace * newMat;
				 xform= xform * newMat;
				 */
			}
#if !(XBOX || XBOX360 || SILVERLIGHT )
			_ffProgramParameters.SetTextureMatrix( stage, xform.Transpose() );
#endif
		}

		public override void SetTextureUnitFiltering( int stage, FilterType type, Axiom.Graphics.FilterOptions filter )
		{
			XnaTextureType texType = XnaHelper.Convert( texStageDesc[ stage ].texType );
			XFG.TextureFilter texFilter = XnaHelper.Convert( type, filter, _capabilities, texType );
			switch( type )
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

		private XFG.DepthStencilBuffer oriDSB;

		public override void SetViewport( Axiom.Core.Viewport viewport )
		{
			if( oriDSB == null )
			{
				oriDSB = _device.DepthStencilBuffer;
			}
			if( activeViewport != viewport || viewport.IsUpdated )
			{
				// store this viewport and it's target
				activeViewport = viewport;
				activeRenderTarget = viewport.Target;

				// get the back buffer surface for this viewport
				XFG.RenderTarget2D[] back = (XFG.RenderTarget2D[])activeRenderTarget[ "XNABACKBUFFER" ];
				if( back == null )
				{
					_device.SetRenderTarget( 0, null );
					//the back buffer is null so it's not a render to texture,
					//we render directly to the screen,
					//set the original depth stencil buffer
					_device.DepthStencilBuffer = oriDSB;
					return;
				}
				else {}

				XFG.DepthStencilBuffer depth = (XFG.DepthStencilBuffer)activeRenderTarget[ "XNAZBUFFER" ];
				if( depth == null )
				{
					// No depth buffer provided, use our own
					// Request a depth stencil that is compatible with the format, multisample type and
					// dimensions of the render target.
					//it is probably a render to texture, so we create the first time a depth buffer
					depth = _getDepthStencilFor( back[ 0 ].Format, back[ 0 ].MultiSampleType, back[ 0 ].Width, back[ 0 ].Height );
				}

				if( depth.Format == _device.DepthStencilBuffer.Format )
				{
					/*MessageBox.Show("same:\n" + 
									depth.Width.ToString() + "-" + depth.Height.ToString() +"\n"+
									_device.DepthStencilBuffer.Width.ToString() + "-" + _device.DepthStencilBuffer.Height.ToString() + "\n"+
									depth.MultiSampleType.ToString() + " = "+_device.DepthStencilBuffer.MultiSampleType.ToString() + "\n" +
									depth.MultiSampleQuality.ToString() + " = " + _device.DepthStencilBuffer.MultiSampleQuality.ToString()+"\n"+
									depth.Format.ToString() + " = " + _device.DepthStencilBuffer.Format.ToString()
									);
					*/
					_device.DepthStencilBuffer = depth;
				}

				// Bind render targets
				int count = back.Length;
				for( int i = 0; i < count && back[ i ] != null; ++i )
				{
					_device.SetRenderTarget( i, back[ i ] );
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

		public struct ZBufferFormat
		{
			public ZBufferFormat( XFG.DepthFormat f, XFG.MultiSampleType m )
			{
				this.format = f;
				this.multisample = m;
			}

			public XFG.DepthFormat format;
			public XFG.MultiSampleType multisample;
		}

		protected Dictionary<ZBufferFormat, XFG.DepthStencilBuffer> zBufferCache = new Dictionary<ZBufferFormat, XFG.DepthStencilBuffer>();
		protected Dictionary<XFG.SurfaceFormat, XFG.DepthFormat> depthStencilCache = new Dictionary<XFG.SurfaceFormat, XFG.DepthFormat>();

		private static XFG.DepthFormat[] _preferredStencilFormats = {
		                                                            	XFG.DepthFormat.Depth24Stencil8Single,
		                                                            	XFG.DepthFormat.Depth24Stencil8,
		                                                            	XFG.DepthFormat.Depth24Stencil4,
		                                                            	XFG.DepthFormat.Depth24,
		                                                            	XFG.DepthFormat.Depth15Stencil1,
		                                                            	XFG.DepthFormat.Depth16,
		                                                            	XFG.DepthFormat.Depth32
		                                                            };

		private XFG.DepthFormat _getDepthStencilFormatFor( XFG.SurfaceFormat fmt )
		{
			XFG.DepthFormat dsfmt;

			// Check if result is cached
			if( depthStencilCache.TryGetValue( fmt, out dsfmt ) )
			{
				return dsfmt;
			}

			// If not, probe with CheckDepthStencilMatch
			dsfmt = XFG.DepthFormat.Unknown;

			// Get description of primary render target
			XFG.SurfaceFormat targetFormat = _primaryWindow.RenderSurfaceFormat;

			// Probe all depth stencil formats
			// Break on first one that matches
			foreach( XFG.DepthFormat df in _preferredStencilFormats )
			{
				// Verify that the depth format exists
				if( !XFG.GraphicsAdapter.DefaultAdapter.CheckDeviceFormat( XFG.DeviceType.Hardware, targetFormat, XFG.TextureUsage.None, XFG.QueryUsages.None, XFG.ResourceType.DepthStencilBuffer, df ) )
				{
					continue;
				}

				// Verify that the depth format is compatible
				if( XFG.GraphicsAdapter.DefaultAdapter.CheckDepthStencilMatch( XFG.DeviceType.Hardware, targetFormat, fmt, df ) )
				{
					dsfmt = df;
					break;
				}
			}

			// Cache result
			depthStencilCache[ fmt ] = dsfmt;
			return dsfmt;
		}

		private XFG.DepthStencilBuffer _getDepthStencilFor( XFG.SurfaceFormat fmt, XFG.MultiSampleType multisample, int width, int height )
		{
			XFG.DepthStencilBuffer zbuffer = null;

			XFG.DepthFormat dsfmt = _getDepthStencilFormatFor( fmt );
			if( dsfmt == XFG.DepthFormat.Unknown )
			{
				return null;
			}

			/// Check if result is cached
			ZBufferFormat zbfmt = new ZBufferFormat( dsfmt, multisample );
			XFG.DepthStencilBuffer cachedzBuffer;
			if( zBufferCache.TryGetValue( zbfmt, out cachedzBuffer ) )
			{
				/// Check if size is larger or equal
				if( cachedzBuffer.Width >= width &&
				    cachedzBuffer.Height >= height )
				{
					zbuffer = cachedzBuffer;
				}
				else
				{
					zBufferCache.Remove( zbfmt );
					cachedzBuffer.Dispose();
				}
			}

			if( zbuffer == null )
			{
				// If not, create the depthstencil surface
				zbuffer = new XFG.DepthStencilBuffer( _device, width, height, dsfmt, multisample, 0 );
				zBufferCache[ zbfmt ] = zbuffer;
			}

			return zbuffer;
		}

		public override void UnbindGpuProgram( GpuProgramType type )
		{
			switch( type )
			{
				case GpuProgramType.Vertex:
					_device.VertexShader = null;
					VertexShaderIsSet = false;
					break;

				case GpuProgramType.Fragment:
					_device.PixelShader = null;
					PixelShaderIsSet = false;
					break;
			}
		}

		public override void UseLights( LightList lights, int limit )
		{
			int currentLightCount = lights.Count < limit ? lights.Count : limit;

			List<Light> lightList = new List<Light>();
#if !(XBOX || XBOX360 || SILVERLIGHT )
			_fixedFunctionState.GeneralFixedFunctionState.ResetLightTypeCounts();
			for( int index = 0; index < currentLightCount; index++ )
			{
				Light light = lights[ index ];
				lightList.Add( light );
				_fixedFunctionState.GeneralFixedFunctionState.IncrementLightTypeCount( light.Type );
			}
			_ffProgramParameters.Lights = lightList;
#endif
		}

		#endregion Methods

		#endregion Axiom.Core.RenderSystem Implementation

		#region IServiceProvider Members

		public object GetService( Type serviceType )
		{
			if( serviceType == typeof( Microsoft.Xna.Framework.Graphics.IGraphicsDeviceService ) )
			{
				foreach( var item in this.renderTargets )
				{
					var renderTarget = item.Value as RenderTarget;
					XFG.IGraphicsDeviceService service = renderTarget as XFG.IGraphicsDeviceService;
					if( service != null )
					{
						return service;
					}
				}
			}

			return null;
		}

		#endregion
	}
}
