#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2007  Axiom Project Team

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
using System.Text;
using System.Windows.Forms;

using Axiom.Core;
using Axiom.Configuration;
using Axiom.Graphics;
using Axiom.Math;
using Axiom.Overlays;

using XNA = Microsoft.Xna.Framework;
using XFG = Microsoft.Xna.Framework.Graphics;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.Xna
{

	/// <summary>
	/// 
	/// </summary>
	public class XnaRenderSystem : RenderSystem
	{
		private XFG.EffectParameter projectionParameter;
		private XFG.EffectParameter viewParameter;
		private XFG.EffectParameter worldParameter;
		private XFG.EffectParameter lightColorParameter;
		private XFG.EffectParameter lightDirectionParameter;
		private XFG.EffectParameter ambientColorParameter;
		private XFG.EffectParameter modelTextureParameter;
		private XFG.EffectParameter modelTextureParameter2;


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

		private XFG.GraphicsDevice _device;
		private XFG.GraphicsDeviceCapabilities _capabilities;
		/// Saved last view matrix
		protected Matrix4 _viewMatrix = Matrix4.Identity;
		bool _isFirstFrame = true;
		// stores texture stage info locally for convenience
		internal XnaTextureStageDescription[] texStageDesc = new XnaTextureStageDescription[ Config.MaxTextureLayers ];
		protected XnaGpuProgramManager gpuProgramMgr;
		int numLastStreams = 0;

		XFG.Effect effect2;
		XFG.Effect effect;
		protected int primCount;
		// protected int renderCount = 0;

		#region Construction and Destruction

		public XnaRenderSystem()
		{
			_initConfigOptions();
			// init the texture stage descriptions
			for ( int i = 0; i < Config.MaxTextureLayers; i++ )
			{
				texStageDesc[ i ].autoTexCoordType = TexCoordCalcMethod.None;
				texStageDesc[ i ].coordIndex = 0;
				texStageDesc[ i ].texType = XnaTextureType.Normal;
				texStageDesc[ i ].tex = null;
			}

		}

		#endregion Construction and Destruction

		#region Helper Methods

		protected void _setVertexBufferBinding( VertexBufferBinding binding )
		{
			IEnumerator e = binding.Bindings;
			// TODO: Optimize to remove enumeration if possible, although with so few iterations it may never make a difference
			while ( e.MoveNext() )
			{
				DictionaryEntry entry = (DictionaryEntry)e.Current;
				XnaHardwareVertexBuffer buffer =
					(XnaHardwareVertexBuffer)entry.Value;

				short stream = (short)entry.Key;

				_device.Vertices[ stream ].SetSource( buffer.XnaVertexBuffer, 0, buffer.VertexSize );

				numLastStreams++;
			}

			// Unbind any unused sources
			for ( int i = binding.BindingCount; i < numLastStreams; i++ )
			{
				_device.Vertices[ i ].SetSource( null, 0, 0 );
			}

			numLastStreams = binding.BindingCount;
		}

		private void _initConfigOptions()
		{
			ConfigOption optDevice = new ConfigOption( "Rendering Device", "", false );
			ConfigOption optVideoMode = new ConfigOption( "Video Mode", "800 x 600 @ 32-bit colour", false );
			ConfigOption optFullScreen = new ConfigOption( "Full Screen", "No", false );
			ConfigOption optVSync = new ConfigOption( "VSync", "No", false );
			ConfigOption optAA = new ConfigOption( "Anti aliasing", "None", false );
			ConfigOption optFPUMode = new ConfigOption( "Floating-point mode", "Fastest", false );

			optDevice.PossibleValues.Clear();
			Driver driver = XnaHelper.GetDriverInfo();

			foreach ( VideoMode mode in driver.VideoModes )
			{
				string query = string.Format( "{0} x {1} @ {2}-bit colour", mode.Width, mode.Height, mode.ColorDepth.ToString() );
				// add a new row to the display settings table
				optVideoMode.PossibleValues.Add( query );
			}

			optFullScreen.PossibleValues.Add( "Yes" );
			optFullScreen.PossibleValues.Add( "No" );

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

		private XNA.Matrix _makeXnaMatrix( Axiom.Math.Matrix4 matrix )
		{
			XNA.Matrix xnaMat = new XNA.Matrix();

			// set it to a transposed matrix since Xna uses row vectors
			xnaMat.M11 = matrix.m00;
			xnaMat.M12 = matrix.m10;
			xnaMat.M13 = matrix.m20;
			xnaMat.M14 = matrix.m30;
			xnaMat.M21 = matrix.m01;
			xnaMat.M22 = matrix.m11;
			xnaMat.M23 = matrix.m21;
			xnaMat.M24 = matrix.m31;
			xnaMat.M31 = matrix.m02;
			xnaMat.M32 = matrix.m12;
			xnaMat.M33 = matrix.m22;
			xnaMat.M34 = matrix.m32;
			xnaMat.M41 = matrix.m03;
			xnaMat.M42 = matrix.m13;
			xnaMat.M43 = matrix.m23;
			xnaMat.M44 = matrix.m33;

			return xnaMat;
		}

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

		private void _checkCapabilities( XFG.GraphicsDevice device )
		{
			// get the number of possible texture units
			caps.TextureUnitCount = _capabilities.MaxSimultaneousTextures;

			// max active lights
			//caps.MaxLights = d3dCaps..MaxActiveLights;

			XFG.DepthStencilBuffer surface = device.DepthStencilBuffer;
			//XNA.TextureInformation surfaceDesc = surface.Description;
			//surface.Dispose();

			if ( surface.Format == XFG.DepthFormat.Depth24Stencil8 || surface.Format == XFG.DepthFormat.Depth24 )
			{
				caps.SetCap( Capabilities.StencilBuffer );
				// always 8 here
				caps.StencilBufferBits = 8;
			}

			// some cards, oddly enough, do not support this
			if ( _capabilities.DeclarationTypeCapabilities.SupportsByte4 )
			{
				caps.SetCap( Capabilities.VertexFormatUByte4 );
			}

			// Anisotropy?
			if ( _capabilities.MaxAnisotropy > 1 )
			{
				caps.SetCap( Capabilities.AnisotropicFiltering );
			}

			// Hardware mipmapping?
			if ( _capabilities.DriverCapabilities.CanAutoGenerateMipMap )
			{
				caps.SetCap( Capabilities.HardwareMipMaps );
			}

			// blending between stages is definately supported
			caps.SetCap( Capabilities.TextureBlending );
			caps.SetCap( Capabilities.MultiTexturing );

			// Cube mapping?
			if ( _capabilities.TextureCapabilities.SupportsCubeMap )
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
			if ( _capabilities.RasterCapabilities.SupportsScissorTest )
			{
				caps.SetCap( Capabilities.ScissorTest );
			}

			// 2 sided stencil
			if ( _capabilities.StencilCapabilities.SupportsTwoSided )
			{
				caps.SetCap( Capabilities.TwoSidedStencil );
			}

			// stencil wrap
			if ( _capabilities.StencilCapabilities.SupportsIncrement && _capabilities.StencilCapabilities.SupportsDecrement )
			{
				caps.SetCap( Capabilities.StencilWrap );
			}

			// Hardware Occlusion, none!
			/* try
			 {
				 D3D.Query test = new D3D.Query(device, D3D.QueryType.Occlusion);

				 // if we made it this far, it is supported
				 caps.SetCap(Capabilities.HardwareOcculusion);

				 test.Dispose();
			 }
			 catch
			 {
				 // eat it, this is not supported
				 // TODO: Isn't there a better way to check for D3D occlusion query support?
			 }*/

			if ( _capabilities.MaxUserClipPlanes > 0 )
			{
				caps.SetCap( Capabilities.UserClipPlanes );
			}

			int vpMajor = _capabilities.VertexShaderVersion.Major;
			int vpMinor = _capabilities.VertexShaderVersion.Minor;
			int fpMajor = _capabilities.PixelShaderVersion.Major;
			int fpMinor = _capabilities.PixelShaderVersion.Minor;

			// check vertex program caps
			switch ( vpMajor )
			{
				case 1:
					caps.MaxVertexProgramVersion = "vs_1_1";
					// 4d float vectors
					caps.VertexProgramConstantFloatCount = _capabilities.MaxVertexShaderConstants;
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
					caps.VertexProgramConstantFloatCount = _capabilities.MaxVertexShaderConstants;

					break;
				case 3:
					caps.MaxVertexProgramVersion = "vs_3_0";

					// 16 ints
					caps.VertexProgramConstantIntCount = 16 * 4;
					// 4d float vectors
					caps.VertexProgramConstantFloatCount = _capabilities.MaxVertexShaderConstants;

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

			// Fragment Program Caps
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

			// Infinite projection?
			// We have no capability for this, so we have to base this on our
			// experience and reports from users
			// Non-vertex program capable hardware does not appear to support it
			if ( caps.CheckCap( Capabilities.VertexPrograms ) )
			{
				// GeForce4 Ti (and presumably GeForce3) does not
				// render infinite projection properly, even though it does in GL
				// So exclude all cards prior to the FX range from doing infinite
				Driver driver = XnaHelper.GetDriverInfo();
				XFG.GraphicsAdapter details = null;
				foreach ( XFG.GraphicsAdapter ga in XFG.GraphicsAdapter.Adapters )
				{
					if ( ga.DeviceId == driver.AdapterNumber )
					{
						details = ga;
						break;
					}
				}

				if ( details != null )
				{
					// not nVidia or GeForceFX and above
					if ( details.VendorId != 0x10DE || details.DeviceId >= 0x0301 )
					{
						caps.SetCap( Capabilities.InfiniteFarPlane );
					}
				}
			}

			// write hardware capabilities to registered log listeners
			caps.Log();
		}

		#endregion Helper Methods

		#region Axiom.Core.RenderSystem Implementation

		#region Properties

		public override ColorEx AmbientLight
		{
			get
			{
				throw new Exception( "The method or operation is not implemented." );
			}
			set
			{
				ambientColorParameter.SetValue( new XNA.Vector4( value.r * 255, value.g * 255, value.b * 255, value.a * 255 ) );
				//              effect.AmbientLightColor = new XNA.Vector3(value.r * 255, value.g * 255, value.b * 255) ;
			}
		}

		public override CullingMode CullingMode
		{
			get
			{
				throw new Exception( "The method or operation is not implemented." );
			}
			set
			{
				cullingMode = value;

				bool flip = activeRenderTarget.RequiresTextureFlipping ^ invertVertexWinding;

				_device.RenderState.CullMode = XnaHelper.Convert( value, flip );
			}
		}

		public override bool DepthWrite
		{
			get
			{
				throw new Exception( "The method or operation is not implemented." );
			}
			set
			{
				_device.RenderState.DepthBufferWriteEnable = value;
			}
		}

		public override bool DepthCheck
		{
			get
			{
				throw new Exception( "The method or operation is not implemented." );
			}
			set
			{
				_device.RenderState.DepthBufferEnable = value;
			}
		}

		public override Axiom.Graphics.CompareFunction DepthFunction
		{
			get
			{
				throw new Exception( "The method or operation is not implemented." );
			}
			set
			{
				_device.RenderState.DepthBufferFunction = XnaHelper.Convert( value );
			}
		}

		public override int DepthBias
		{
			get
			{
				throw new Exception( "The method or operation is not implemented." );
			}
			set
			{
				_device.RenderState.DepthBias = (float)value;
			}
		}

		public override float HorizontalTexelOffset
		{
			// D3D considers the origin to be in the center of a pixel
			get
			{
				return -0.5f;
			}
		}

		public override bool LightingEnabled
		{
			get
			{
				throw new Exception( "The method or operation is not implemented." );
			}
			set
			{

				//effect.LightingEnabled = value; // throw new Exception("The method or operation is not implemented.");
			}
		}

		public override bool NormalizeNormals
		{
			get
			{
				throw new Exception( "The method or operation is not implemented." );
			}
			set
			{
				throw new Exception( "The method or operation is not implemented." );
			}
		}

		public override Axiom.Math.Matrix4 ProjectionMatrix
		{
			get
			{
				throw new Exception( "The method or operation is not implemented." );
			}
			set
			{
				XNA.Matrix mat = _makeXnaMatrix( value );

				if ( activeRenderTarget.RequiresTextureFlipping )
				{
					mat.M22 = -mat.M22;
				}

				//todo set the projection matrix in effect
				projectionParameter.SetValue( mat );
				// effect.Projection = mat;

				effect.CommitChanges();

			}
		}

		public override SceneDetailLevel RasterizationMode
		{
			get
			{
				throw new Exception( "The method or operation is not implemented." );
			}
			set
			{
				switch ( value )
				{
					case SceneDetailLevel.Points:
						_device.RenderState.FillMode = XFG.FillMode.Point;
						break;
					case SceneDetailLevel.Wireframe:
						_device.RenderState.FillMode = XFG.FillMode.WireFrame;
						break;
					case SceneDetailLevel.Solid:
						_device.RenderState.FillMode = XFG.FillMode.Solid;
						break;
				}
			}
		}

		public override Shading ShadingMode
		{
			get
			{
				throw new Exception( "The method or operation is not implemented." );
			}
			set
			{
				//throw new Exception("The method or operation is not implemented.");
			}
		}

		public override bool StencilCheckEnabled
		{
			get
			{
				throw new Exception( "The method or operation is not implemented." );
			}
			set
			{
				_device.RenderState.StencilEnable = value;
			}
		}

		public override float VerticalTexelOffset
		{
			get
			{
				// D3D considers the origin to be in the center of a pixel
				return -0.5f;
			}
		}

		public override Axiom.Math.Matrix4 ViewMatrix
		{
			get
			{
				throw new Exception( "The method or operation is not implemented." );
			}
			set
			{
				// flip the transform portion of the matrix for DX and its left-handed coord system
				// save latest view matrix
				_viewMatrix = value;
				_viewMatrix.m20 = -_viewMatrix.m20;
				_viewMatrix.m21 = -_viewMatrix.m21;
				_viewMatrix.m22 = -_viewMatrix.m22;
				_viewMatrix.m23 = -_viewMatrix.m23;

				XNA.Matrix dxView = _makeXnaMatrix( _viewMatrix );
				viewParameter.SetValue( dxView );
			}
		}

		public override Axiom.Math.Matrix4 WorldMatrix
		{
			get
			{
				throw new Exception( "The method or operation is not implemented." );
			}
			set
			{
				//throw new Exception("The method or operation is not implemented.");
				worldParameter.SetValue( _makeXnaMatrix( value ) );

			}
		}

		#endregion Properties

		#region Methods

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
			if ( forGpuProgram )
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

		public override void BeginFrame()
		{
			// clear the device if need be
			if ( activeViewport.ClearEveryFrame )
			{
				ClearFrameBuffer( FrameBuffer.Color | FrameBuffer.Depth, activeViewport.BackgroundColor );
			}
			// set initial render states if this is the first frame. we only want to do 
			//	this once since renderstate changes are expensive
			if ( _isFirstFrame )
			{
				// enable alpha blending and specular materials
				_device.RenderState.AlphaBlendEnable = true;
				//device.RenderState.SpecularEnable = true;
				_device.RenderState.DepthBufferEnable = true;
				_isFirstFrame = false;
			}
		}

		public override void BindGpuProgram( GpuProgram program )
		{

			switch ( program.Type )
			{
				case GpuProgramType.Vertex:
					_device.VertexShader = ( (XnaVertexProgram)program ).VertexShader;
					break;

				case GpuProgramType.Fragment:
					_device.PixelShader = ( (XnaFragmentProgram)program ).PixelShader;
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
								_device.SetVertexShaderConstant( index, entry.val );
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
								_device.SetVertexShaderConstant( index, entry.val );
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
								_device.SetPixelShaderConstant( index, entry.val );
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
								_device.SetPixelShaderConstant( index, entry.val );
							}
						}
					}
					break;
			}
		}

		public override void ClearFrameBuffer( FrameBuffer buffers, ColorEx color, float depth, int stencil )
		{
			XFG.ClearOptions flags = 0; //ClearFlags 

			if ( ( buffers & FrameBuffer.Color ) > 0 )
			{
				flags |= XFG.ClearOptions.Target;
			}
			if ( ( buffers & FrameBuffer.Depth ) > 0 )
			{
				flags |= XFG.ClearOptions.DepthBuffer;
			}
			// Only try to clear the stencil buffer if supported
			if ( ( buffers & FrameBuffer.Stencil ) > 0
				&& caps.CheckCap( Capabilities.StencilBuffer ) )
			{

				flags |= XFG.ClearOptions.Stencil;
			}
			XFG.Color col = new XFG.Color( (byte)( color.r * 255.0f ), (byte)( color.g * 255.0f ), (byte)( color.b * 255.0f ), (byte)( color.a * 255.0f ) );
			// color.ToXnaColor();

			// clear the device using the specified params
			_device.Clear( flags, col, depth, stencil );
		}

		//never used ?
		public override int ConvertColor( ColorEx color )
		{
			return color.ToARGB();
		}

		public override RenderTexture CreateRenderTexture( string name, int width, int height )
		{
			XnaRenderTexture renderTexture = new XnaRenderTexture( name, width, height );
			AttachRenderTarget( renderTexture );
			return renderTexture;
		}

		public override RenderWindow CreateRenderWindow( string name, int width, int height, int colorDepth, bool isFullscreen, int left, int top, bool depthBuffer, bool vsync, object target )
		{
			if ( _device == null )
			{
				if ( isFullscreen )
				{
					_device = InitDevice( isFullscreen, depthBuffer, width, height, colorDepth, (Control)target );
				}
				else
				{
					_device = InitDevice( isFullscreen, depthBuffer, width, height, colorDepth, new Control() );
				}

				//create a simple effect to draw textured stuff
				String strEffect = System.IO.File.ReadAllText( "TexturesAndColors.fx" );
				XFG.CompiledEffect compeffect = XFG.Effect.CompileEffectFromSource( strEffect, null, null, XFG.CompilerOptions.Debug, XNA.TargetPlatform.Windows );
				System.Diagnostics.Debug.Assert( compeffect.Success == true, compeffect.ErrorsAndWarnings );
				byte[] effectCode = compeffect.GetEffectCode();

				effect = new XFG.Effect( _device, effectCode, XFG.CompilerOptions.Debug, null );
				//effect = new BasicEffect( device, null );


				worldParameter = effect.Parameters[ "world" ];
				viewParameter = effect.Parameters[ "view" ];
				projectionParameter = effect.Parameters[ "projection" ];
				lightColorParameter = effect.Parameters[ "lightColor" ];
				lightDirectionParameter = effect.Parameters[ "lightDirection" ];
				ambientColorParameter = effect.Parameters[ "ambientColor" ];
				modelTextureParameter = effect.Parameters[ "modelTexture" ];
				modelTextureParameter2 = effect.Parameters[ "modelTexture2" ];

				effect.CurrentTechnique = effect.Techniques[ 0 ];

			}

			RenderWindow window = new XnaWindow();

			window.Handle = target;

			// create the window
			window.Create( name, width, height, colorDepth, isFullscreen, left, top, depthBuffer, (Control)target, _device );

			// add the new render target
			AttachRenderTarget( window );

			return window;
		}

		private XFG.GraphicsDevice InitDevice( bool isFullscreen, bool depthBuffer, int width, int height, int colorDepth, Control target )
		{
			if ( _device != null )
			{
				return _device;
			}

			// we don't care about event handlers
			//D3D.Device.IsUsingEventHandlers = false;

			XFG.GraphicsDevice newDevice;

			// if this is the first window, get the device and do other initialization
			// CMH - 4/24/2004 start
			/// get the Direct3D.Device params
			XFG.PresentationParameters presentParams = new XFG.PresentationParameters();
			presentParams.IsFullScreen = isFullscreen;
			presentParams.BackBufferCount = 1;
			presentParams.EnableAutoDepthStencil = depthBuffer;

			if ( isFullscreen )
			{
				presentParams.BackBufferWidth = width;
				presentParams.BackBufferHeight = height;
			}
			else
			{	// Save us some bytes.
				presentParams.BackBufferWidth = width;
				presentParams.BackBufferHeight = height;
			}

			presentParams.MultiSampleType = XFG.MultiSampleType.None;
			presentParams.SwapEffect = XFG.SwapEffect.Copy;
			// TODO: Check vsync setting
			presentParams.PresentationInterval = XFG.PresentInterval.Immediate;

			// supports 16 and 32 bit color
			if ( colorDepth == 16 )
			{
				presentParams.BackBufferFormat = XFG.SurfaceFormat.Bgr565;
			}
			else
			{
				presentParams.BackBufferFormat = XFG.SurfaceFormat.Color;
			}

			if ( colorDepth > 16 )
			{
				// check for 24 bit Z buffer with 8 bit stencil (optimal choice)
				if ( !XFG.GraphicsAdapter.DefaultAdapter.CheckDeviceFormat(
							 XFG.DeviceType.Hardware,
							 presentParams.BackBufferFormat,
							 XFG.TextureUsage.None,
							 XFG.QueryUsages.None,
							 XFG.ResourceType.DepthStencilBuffer,
							 XFG.DepthFormat.Depth24Stencil8 ) )
				{
					// doh, check for 32 bit Z buffer then

					if ( !XFG.GraphicsAdapter.DefaultAdapter.CheckDeviceFormat(
					XFG.DeviceType.Hardware,
					presentParams.BackBufferFormat,
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
					if ( !XFG.GraphicsAdapter.DefaultAdapter.CheckDepthStencilMatch(
							XFG.DeviceType.Hardware,
							presentParams.BackBufferFormat,
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
				newDevice = new XFG.GraphicsDevice
					(
						XFG.GraphicsAdapter.DefaultAdapter,
						XFG.DeviceType.Hardware,
						target.Handle,
					//XNA.CreateOptions.HardwareVertexProcessing,
						presentParams );
				//   D3D.Device(0, D3D.DeviceType.Hardware, target, D3D.CreateFlags.HardwareVertexProcessing, presentParams);
			}
			catch ( Exception )
			{
				try
				{
					// doh, how bout mixed vertex processing
					newDevice = new XFG.GraphicsDevice(
						XFG.GraphicsAdapter.DefaultAdapter,
						XFG.DeviceType.Hardware,
						target.Handle,
						//XNA.CreateOptions.MixedVertexProcessing,
						presentParams );
				}
				catch ( XFG.DeviceNotSupportedException )
				{
					// what the...ok, how bout software vertex procssing.  if this fails, then I don't even know how they are seeing
					// anything at all since they obviously don't have a video card installed
					newDevice = new XFG.GraphicsDevice(
						XFG.GraphicsAdapter.DefaultAdapter,
						XFG.DeviceType.Hardware,
						target.Handle,
						// XNA.CreateOptions.SoftwareVertexProcessing,
						presentParams );
				}
			}

			// CMH - end
			// save the device capabilites
			_capabilities = newDevice.GraphicsDeviceCapabilities;

			// by creating our texture manager, singleton TextureManager will hold our implementation
			textureMgr = new XnaTextureManager( newDevice );

			// by creating our Gpu program manager, singleton GpuProgramManager will hold our implementation
			gpuProgramMgr = new XnaGpuProgramManager( newDevice );

			// intializes the HardwareBufferManager singleton
			hardwareBufferManager = new XnaHardwareBufferManager( newDevice );
			_checkCapabilities( newDevice );


			return newDevice;
		}

		public override IHardwareOcclusionQuery CreateHardwareOcclusionQuery()
		{
			return new XnaHardwareOcclusionQuery( _device );
		}

		public override void EndFrame()
		{
			// end the D3D scene
			//device.EndScene();
		}

		public override RenderWindow Initialize( bool autoCreateWindow, string windowTitle )
		{
			RenderWindow renderWindow = null;

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

				//fullScreen = true;// (ConfigOptions["Full Screen"].Value == "Yes");

				// create a default form window
				DefaultForm newWindow = _createDefaultForm( windowTitle, 0, 0, width, height, fullScreen );

				// create the render window
				renderWindow = CreateRenderWindow( "Main Window", width, height, bpp, fullScreen, 0, 0, true, false, newWindow );

				// use W buffer when in 16 bit color mode
				//useWBuffer = (renderWindow.ColorDepth == 16);

				newWindow.Target.Visible = false;

				newWindow.Show();

				// set the default form's renderwindow so it can access it internally
				newWindow.RenderWindow = renderWindow;
			}

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

		public override Axiom.Math.Matrix4 MakeProjectionMatrix( float fov, float aspectRatio, float near, float far, bool forGpuProgram )
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

		public override void Render( RenderOperation op )
		{
			// don't even bother if there are no vertices to render, causes problems on some cards (FireGL 8800)
			if ( op.vertexData.vertexCount == 0 )
			{
				return;
			}

			// class base implementation first
			base.Render( op );

			XnaVertexDeclaration d3dVertDecl = (XnaVertexDeclaration)op.vertexData.vertexDeclaration;

			// set the vertex declaration and buffer binding
			//SetVertexDeclaration( op.vertexData.vertexDeclaration );
			_device.VertexDeclaration = d3dVertDecl.D3DVertexDecl;
			_setVertexBufferBinding( op.vertexData.vertexBufferBinding );

			XFG.PrimitiveType primType = 0;

			switch ( op.operationType )
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
			if ( op.useIndices )
			{
				XnaHardwareIndexBuffer idxBuffer =
					(XnaHardwareIndexBuffer)op.indexData.indexBuffer;
				_device.Indices = idxBuffer.XnaIndexBuffer;


				effect.CommitChanges();

				//XNA needs a vertexshader and pixel shader to draw something, check first if an effect has been given via fixed function
				if ( _device.VertexShader != null && _device.PixelShader != null )
					_device.DrawIndexedPrimitives( primType, op.vertexData.vertexStart, 0, op.vertexData.vertexCount, op.indexData.indexStart, primCount );
				//if not, so use the effect file which draws basic stuff
				else
				{
					effect.Begin();

					for ( int i = 0; i < this.effect.CurrentTechnique.Passes.Count; ++i )
					{
						this.effect.CurrentTechnique.Passes[ i ].Begin();
						_device.DrawIndexedPrimitives( primType, op.vertexData.vertexStart, 0, op.vertexData.vertexCount, op.indexData.indexStart, primCount );
						this.effect.CurrentTechnique.Passes[ i ].End();
					}
					effect.End();
					_device.VertexShader = null;
					_device.PixelShader = null;
				}

			}
			else
			{
				if ( _device.VertexShader != null || _device.PixelShader != null )
					_device.DrawPrimitives( primType, op.vertexData.vertexStart, primCount );
				else
				{
					effect.Begin();
					for ( int i = 0; i < this.effect.CurrentTechnique.Passes.Count; ++i )
					{
						this.effect.CurrentTechnique.Passes[ i ].Begin();
						// draw vertices without indices
						_device.DrawPrimitives( primType, op.vertexData.vertexStart, primCount );
						this.effect.CurrentTechnique.Passes[ i ].End();
					}
					effect.End();
					_device.VertexShader = null;
					_device.PixelShader = null;
				}
			}
			//crap hack, set the sources back to null to allow accessing vertices and indices buffers
			_device.Vertices[ 0 ].SetSource( null, 0, 0 );
			_device.Vertices[ 1 ].SetSource( null, 0, 0 );
			_device.Vertices[ 2 ].SetSource( null, 0, 0 );
			_device.Indices = null;

		}

		public override void SetAlphaRejectSettings( int stage, Axiom.Graphics.CompareFunction func, byte val )
		{
			//todo
			_device.RenderState.AlphaTestEnable = ( func != Axiom.Graphics.CompareFunction.AlwaysPass );
			_device.RenderState.AlphaFunction = XnaHelper.Convert( func );
			_device.RenderState.ReferenceAlpha = val;
		}

		public override void SetColorBufferWriteEnabled( bool red, bool green, bool blue, bool alpha )
		{
			//todo
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

			_device.RenderState.ColorWriteChannels = val;
		}

		public override void SetDepthBufferParams( bool depthTest, bool depthWrite, Axiom.Graphics.CompareFunction depthFunction )
		{
			//device.RenderState.DepthBufferEnable
			this.DepthCheck = depthTest;
			this.DepthWrite = depthWrite;
			this.DepthFunction = depthFunction;
		}

		//TODO , still problem with fog
		public override void SetFog( Axiom.Graphics.FogMode mode, ColorEx color, float density, float start, float end )
		{
			// disable fog if set to none
			if ( mode == Axiom.Graphics.FogMode.None )
			{
				_device.RenderState.FogTableMode = XFG.FogMode.None;
				_device.RenderState.FogEnable = false;
			}
			else
			{
				// enable fog
				XFG.Color col = new XFG.Color( (byte)( color.r * 255.0f ), (byte)( color.g * 255.0f ), (byte)( color.b * 255.0f ), (byte)( color.a * 255.0f ) );
				//color.ToXnaColor();

				_device.RenderState.FogEnable = true;
				_device.RenderState.FogVertexMode = XnaHelper.Convert( mode );
				_device.RenderState.FogTableMode = XnaHelper.Convert( mode );// XFG.FogMode.Linear;
				_device.RenderState.FogColor = col;
				_device.RenderState.FogStart = start;
				_device.RenderState.FogEnd = end;
				_device.RenderState.FogDensity = density;
				_device.RenderState.RangeFogEnable = true;

				/*  effect2.FogEnabled = true;
				  effect2.FogColor = new XNA.Vector3(col.R, col.G, col.B);// col;
				  effect2.FogEnd = end;
				  effect2.FogStart = start;*/
			}
		}

		public override void SetSceneBlending( SceneBlendFactor src, SceneBlendFactor dest )
		{
			//TODO
			// set the render states after converting the incoming values to D3D.Blend
			_device.RenderState.SourceBlend = XnaHelper.Convert( src );
			_device.RenderState.DestinationBlend = XnaHelper.Convert( dest );
		}

		public override void SetScissorTest( bool enable, int left, int top, int right, int bottom )
		{
			if ( enable )
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
			// 2 sided operation?
			if ( twoSidedOperation )
			{
				if ( !caps.CheckCap( Capabilities.TwoSidedStencil ) )
				{
					throw new AxiomException( "2-sided stencils are not supported on this hardware!" );
				}

				_device.RenderState.TwoSidedStencilMode = true;

				// use CCW version of the operations
				_device.RenderState.CounterClockwiseStencilFail = XnaHelper.Convert( stencilFailOp, true );
				_device.RenderState.CounterClockwiseStencilDepthBufferFail = XnaHelper.Convert( depthFailOp, true );
				_device.RenderState.CounterClockwiseStencilPass = XnaHelper.Convert( passOp, true );
			}
			else
			{
				_device.RenderState.TwoSidedStencilMode = false;
			}

			// configure standard version of the stencil operations
			_device.RenderState.StencilFunction = XnaHelper.Convert( function );
			_device.RenderState.ReferenceStencil = refValue;
			_device.RenderState.StencilMask = mask;
			_device.RenderState.StencilFail = XnaHelper.Convert( stencilFailOp );
			_device.RenderState.StencilDepthBufferFail = XnaHelper.Convert( depthFailOp );
			_device.RenderState.StencilPass = XnaHelper.Convert( passOp );
		}

		public override void SetSurfaceParams( ColorEx ambient, ColorEx diffuse, ColorEx specular, ColorEx emissive, float shininess )
		{
			XFG.Color col = new XFG.Color( (byte)( ambient.r * 255.0f ), (byte)( ambient.g * 255.0f ), (byte)( ambient.b * 255.0f ), (byte)( ambient.a * 255.0f ) );
			//ambient.ToXnaColor();
			ambientColorParameter.SetValue( new XNA.Vector4( col.R, col.G, col.B, col.A ) );


			/*   effect2.AmbientLightColor =new XNA.Vector3(col.R,col.G,col.B);
			   col = diffuse.ToXnaColor();
			   effect2.DiffuseColor = new XNA.Vector3(col.R, col.G, col.B);
			   col = specular.ToXnaColor();
			   effect2.SpecularColor = new XNA.Vector3(col.R, col.G, col.B);
			   col = emissive.ToXnaColor();
			   effect2.EmissiveColor = new XNA.Vector3(col.R, col.G, col.B);
			   effect2.SpecularPower = shininess;*/

			//todo
		}

		public override void SetTexture( int stage, bool enabled, string textureName )
		{
			//TODO, seems to work
			XnaTexture texture = (XnaTexture)TextureManager.Instance.GetByName( textureName );
			//texture.NormalTexture.Save("text.jpg", ImageFileFormat.Jpg);
			if ( enabled && texture != null )
			{
				//  modelTextureParameter.SetValue(texture.DXTexture);   
				_device.Textures[ stage ] = texture.DXTexture;
				//SetTexture(stage, texture.DXTexture);

				// set stage description
				texStageDesc[ stage ].tex = texture.DXTexture;
				texStageDesc[ stage ].texType = XnaHelper.Convert( texture.TextureType );
			}
			else
			{
				if ( texStageDesc[ stage ].tex != null )
				{

					//modelTextureParameter.SetValue((int)0);
					//effect.Texture = null;
					_device.Textures[ stage ] = null;
					//device.Textures[2].state.TextureState[stage].ColorOperation = XFG.TextureOperation.Disable;
				}

				// set stage description to defaults
				texStageDesc[ stage ].tex = null;
				texStageDesc[ stage ].autoTexCoordType = TexCoordCalcMethod.None;
				texStageDesc[ stage ].coordIndex = 0;
				texStageDesc[ stage ].texType = XnaTextureType.Normal;
			}
		}

		public override void SetTextureAddressingMode( int stage, TextureAddressing texAddressingMode )
		{
			XFG.TextureAddressMode d3dMode = XnaHelper.Convert( texAddressingMode );

			// set the device sampler states accordingly
			_device.SamplerStates[ stage ].AddressU = d3dMode;
			_device.SamplerStates[ stage ].AddressV = d3dMode;
			_device.SamplerStates[ stage ].AddressW = d3dMode;

		}

		public override void SetTextureBlendMode( int stage, LayerBlendModeEx blendMode )
		{
			XFG.BlendFunction d3dTexOp = XnaHelper.Convert( blendMode.operation );

			// TODO: Verify byte ordering
			if ( blendMode.operation == LayerBlendOperationEx.BlendManual )
			{
				_device.RenderState.BlendFactor = new XFG.Color( 0, 0, 0,
					Convert.ToByte( blendMode.blendFactor ) );// (new ColorEx(blendMode.blendFactor, 0, 0, 0)).ToARGB();
			}

			if ( blendMode.blendType == LayerBlendType.Color )
			{
				// Make call to set operation
				_device.RenderState.BlendFunction = d3dTexOp;
			}
			else if ( blendMode.blendType == LayerBlendType.Alpha )
			{
				// Make call to set operation
				_device.RenderState.AlphaBlendOperation = d3dTexOp;
				//  device.RenderState.AlphaFunction= d3dTexOp;
			}

			// Now set up sources
			System.Drawing.Color factor = System.Drawing.Color.FromArgb( _device.RenderState.BlendFactor.A,
																			_device.RenderState.BlendFactor.R,
																			_device.RenderState.BlendFactor.G,
																			_device.RenderState.BlendFactor.B );
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

			/*for (int i = 0; i < 2; i++)
			{
                
					device.RenderState.co.
				D3D.TextureArgument d3dTexArg = D3DHelper.ConvertEnum(blendSource);

				// set the texture blend factor if this is manual blending
				if (blendSource == LayerBlendSource.Manual)
				{
					device.RenderState.TextureFactor = manualD3D.ToARGB();
				}

				// pick proper argument settings
				if (blendMode.blendType == LayerBlendType.Color)
				{
					if (i == 0)
					{
						device.TextureState[stage].ColorArgument1 = d3dTexArg;
					}
					else if (i == 1)
					{
						device.TextureState[stage].ColorArgument2 = d3dTexArg;
					}
				}
				else if (blendMode.blendType == LayerBlendType.Alpha)
				{
					if (i == 0)
					{
						device.TextureState[stage].AlphaArgument1 = d3dTexArg;
					}
					else if (i == 1)
					{
						device.TextureState[stage].AlphaArgument2 = d3dTexArg;
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
			}TODO*/
		}

		public override void SetTextureCoordCalculation( int stage, TexCoordCalcMethod method, Frustum frustum )
		{
			//TODO, dont know what to do :S no fixed function equivalent, will have to make a big effect file that can handle all this
			// save this for texture matrix calcs later
			texStageDesc[ stage ].autoTexCoordType = method;
			texStageDesc[ stage ].frustum = frustum;



			//(TextureWrapCoordinates)(D3DHelper.ConvertEnum(method, d3dCaps) | texStageDesc[stage].coordIndex);

			//.TextureState[stage].TextureCoordinateIndex 
		}

		public override void SetTextureCoordSet( int stage, int index )
		{
			// store
			texStageDesc[ stage ].coordIndex = index;

			//TODO device.TextureState[stage].TextureCoordinateIndex = D3DHelper.ConvertEnum(texStageDesc[stage].autoTexCoordType, d3dCaps) | index;
		}

		public override void SetTextureLayerAnisotropy( int stage, int maxAnisotropy )
		{
			if ( maxAnisotropy > _capabilities.MaxAnisotropy )
			{
				maxAnisotropy = _capabilities.MaxAnisotropy;
			}

			if ( _device.SamplerStates[ stage ].MaxAnisotropy != maxAnisotropy )
			{
				_device.SamplerStates[ stage ].MaxAnisotropy = maxAnisotropy;
			}
		}

		public override void SetTextureMatrix( int stage, Axiom.Math.Matrix4 xform )
		{
			XNA.Matrix d3dMat = XNA.Matrix.Identity;
			Matrix4 newMat = xform;

			/* If envmap is applied, but device doesn't support spheremap,
			then we have to use texture transform to make the camera space normal
			reference the envmap properly. This isn't exactly the same as spheremap
			(it looks nasty on flat areas because the camera space normals are the same)
			but it's the best approximation we have in the absence of a proper spheremap 
		   * */
			if ( texStageDesc[ stage ].autoTexCoordType == TexCoordCalcMethod.EnvironmentMap )
			{
				if ( _capabilities.VertexProcessingCapabilities.SupportsTextureGenerationSphereMap )
				{
					// inverts the texture for a spheremap
					Matrix4 matEnvMap = Matrix4.Identity;
					matEnvMap.m11 = -1.0f;

					// concatenate 
					newMat = newMat * matEnvMap;
				}
				else
				{
					/*     If envmap is applied, but device doesn't support spheremap,
						 then we have to use texture transform to make the camera space normal
						 reference the envmap properly. This isn't exactly the same as spheremap
						 (it looks nasty on flat areas because the camera space normals are the same)
						 but it's the best approximation we have in the absence of a proper spheremap 
						 */
					// concatenate with the xForm
					newMat = newMat * Matrix4.ClipSpace2DToImageSpace;
				}
			}

			// If this is a cubic reflection, we need to modify using the view matrix

			if ( texStageDesc[ stage ].autoTexCoordType == TexCoordCalcMethod.EnvironmentMapReflection )
			{
				// get the current view matrix
				XNA.Matrix viewMatrix = viewParameter.GetValueMatrix();// effect.View;// this.viewMatrix;// device.Transform.View;

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
				newMat = _viewMatrix.Inverse() * newMat;
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
			d3dMat = _makeXnaMatrix( newMat );

			// need this if texture is a cube map, to invert D3D's z coord
			if ( texStageDesc[ stage ].autoTexCoordType != TexCoordCalcMethod.None )
			{
				d3dMat.M13 = -d3dMat.M13;
				d3dMat.M23 = -d3dMat.M23;
				d3dMat.M33 = -d3dMat.M33;
				d3dMat.M43 = -d3dMat.M43;
			}

			/*TODO
			 * D3D.TransformType d3dTransType = (D3D.TransformType)((int)(D3D.TransformType.Texture0) + stage);

			 // set the matrix if it is not the identity
			 if (!D3DHelper.IsIdentity(ref d3dMat))
			 {
				 // tell D3D the dimension of tex. coord
				 int texCoordDim = 0;
				 if (texStageDesc[stage].autoTexCoordType == TexCoordCalcMethod.ProjectiveTexture)
				 {
					 texCoordDim = (int)D3D.TextureTransform.Projected | (int)D3D.TextureTransform.Count3;
				 }
				 else
				 {
					 switch (texStageDesc[stage].texType)
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
				 device.Textures[stage].TextureTransform = (D3D.TextureTransform)texCoordDim;

				 // set the manually calculated texture matrix
				 device.SetTransform(d3dTransType, d3dMat);
			 }
			 else
			 {
				 // disable texture transformation
				 device.TextureState[stage].TextureTransform = D3D.TextureTransform.Disable;

				 // set as the identity matrix
				 device.SetTransform(d3dTransType, DX.Matrix.Identity);
			 }*/
		}

		public override void SetTextureUnitFiltering( int stage, FilterType type, Axiom.Graphics.FilterOptions filter )
		{
			XnaTextureType texType = texStageDesc[ stage ].texType;
			XFG.TextureFilter texFilter = XnaHelper.Convert( type, filter, _capabilities, texType );

			switch ( type )
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
		XFG.DepthStencilBuffer oriDSB;
		public override void SetViewport( Axiom.Core.Viewport viewport )
		{
			if ( activeViewport != viewport || viewport.IsUpdated )
			{
				// store this viewport and it's target
				activeViewport = viewport;
				activeRenderTarget = viewport.Target;

				//save the original dephstencil buffer
				if ( oriDSB == null )
				{
					oriDSB = _device.DepthStencilBuffer;
				}
				// get the back buffer surface for this viewport          
				XFG.RenderTarget2D back = (XFG.RenderTarget2D)activeRenderTarget.GetCustomAttribute( "D3DBACKBUFFER" );

				_device.SetRenderTarget( 0, back );

				XFG.DepthStencilBuffer depth = (XFG.DepthStencilBuffer)activeRenderTarget.GetCustomAttribute( "D3DZBUFFER" );

				// set the render target and depth stencil for the surfaces beloning to the viewport
				//dont know why the depthstencil buffer is disposing itself, have to keep it
				if ( depth.IsDisposed )
					_device.DepthStencilBuffer = oriDSB;// new DepthStencilBuffer(device, oriDSB.Width, oriDSB.Height, DepthFormat.Depth24Stencil8, MultiSampleType.None, 0);

				else
					_device.DepthStencilBuffer = depth;

				// set the culling mode, to make adjustments required for viewports
				// that may need inverted vertex winding or texture flipping
				this.CullingMode = cullingMode;

				XFG.Viewport d3dvp = new XFG.Viewport();

				// set viewport dimensions
				d3dvp.X = viewport.ActualLeft;
				d3dvp.Y = viewport.ActualTop;
				d3dvp.Width = viewport.ActualWidth;
				d3dvp.Height = viewport.ActualHeight;

				// Z-values from 0.0 to 1.0 (TODO: standardize with OpenGL)
				d3dvp.MinDepth = 0.0f;
				d3dvp.MaxDepth = 1.0f;

				// set the current D3D viewport
				_device.Viewport = d3dvp;

				// clear the updated flag
				viewport.IsUpdated = false;
			}
		}

		public override void UnbindGpuProgram( GpuProgramType type )
		{
			switch ( type )
			{
				case GpuProgramType.Vertex:
					_device.VertexShader = null;
					break;

				case GpuProgramType.Fragment:
					_device.PixelShader = null;
					break;
			}
		}

		public override void UseLights( Axiom.Collections.LightList lightList, int limit )
		{
			//throw new Exception("The method or operation is not implemented.");
		}

		#endregion Methods

		#endregion Axiom.Core.RenderSystem Implementation
	}
}
