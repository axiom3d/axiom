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

#endregion

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System.Collections.Generic;

using Axiom.Core;
using Axiom.Scripting;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
	/// <summary>
	/// Enumeration of GPU vendors.
	/// </summary>
	public enum GPUVendor
	{
		[ScriptEnum( "Unknown" )]
		Unknown = 0,

		[ScriptEnum( "Nvidia" )]
		Nvidia = 1,

		[ScriptEnum( "Ati" )]
		Ati = 2,

		[ScriptEnum( "Intel" )]
		Intel = 3,

		[ScriptEnum( "S3" )]
		S3 = 4,

		[ScriptEnum( "Matrox" )]
		Matrox = 5,

		[ScriptEnum( "3DLabs" )]
		_3DLabs = 6,

		[ScriptEnum( "Sis" )]
		Sis = 7,

		[ScriptEnum( "Imagination Technologies" )]
		ImaginationTechnologies = 8,

		// Apple Software Renderer
		[ScriptEnum( "Apple" )]
		Apple = 9,

		[ScriptEnum( "Nokia" )]
		Nokia = 10,
	};

	public class ShaderProfiles : List<string> { }

	/// <summary>
	/// 	This serves as a way to query information about the capabilies of a 3D API and the
	/// 	users hardware configuration.  A RenderSystem should create and initialize an instance
	/// 	of this class during startup so that it will be available for use ASAP for checking caps.
	/// </summary>
	public class RenderSystemCapabilities
	{
		#region Fields and Properties

		/// <summary>
		///    Flag enum holding the bits that identify each supported feature.
		/// </summary>
		private Capabilities _caps;

		#region RendersystemName

		/// <summary>
		/// Gets or sets the current rendersystem name.
		/// </summary>
		public string RendersystemName { get; set; }

		#endregion

		#region TextureUnitCount Property

		/// <summary>
		///		Reports on the number of texture units the graphics hardware has available.
		/// </summary>
		public int TextureUnitCount { get; set; }

		#endregion TextureUnitCount Property

		#region WorlMatrixCount Property

		/// <summary>
		///    Max number of world matrices supported by the hardware.
		/// </summary>
		public int WorldMatrixCount { get; set; }

		#endregion WorlMatrixCount Property

		#region MaxVertexProgramVersion Property

		/// <summary>
		///    Best vertex program version supported by the hardware.
		/// </summary>
		public string MaxVertexProgramVersion { get; set; }

		#endregion MaxVertexProgramVersion Property

		#region VertexProgramConstantFloatCount Property

		/// <summary>
		///    Max number of floating point constants supported by the hardware for vertex programs.
		/// </summary>
		public int VertexProgramConstantFloatCount { get; set; }

		#endregion VertexProgramConstantFloatCount Property

		#region VertexProgramConstantIntCount Property

		/// <summary>
		///    Max number of integer constants supported by the hardware for vertex programs.
		/// </summary>
		public int VertexProgramConstantIntCount { get; set; }

		#endregion VertexProgramConstantIntCount Property

		#region VertexProgramConstantBoolCount Property

		/// <summary>
		///    Max number of boolean constants supported by the hardware for vertex programs.
		/// </summary>
		public int VertexProgramConstantBoolCount { get; set; }

		#endregion VertexProgramConstantBoolCount Property

		#region MaxFragmentProgramVersion Property

		/// <summary>
		///    Best fragment program version supported by the hardware.
		/// </summary>
		public string MaxFragmentProgramVersion { get; set; }

		#endregion MaxFragmentProgramVersion Property

		#region FragmentProgramConstantFloatCount Property

		/// <summary>
		///    Max number of floating point constants supported by the hardware for fragment programs.
		/// </summary>
		public int FragmentProgramConstantFloatCount { get; set; }

		#endregion FragmentProgramConstantFloatCount Property

		#region FragmentProgramConstantIntCount Property

		/// <summary>
		///    Max number of integer constants supported by the hardware for fragment programs.
		/// </summary>
		public int FragmentProgramConstantIntCount { get; set; }

		#endregion FragmentProgramConstantIntCount Property

		#region FragmentProgramConstantBoolCount Property

		/// <summary>
		///    Max number of boolean constants supported by the hardware for fragment programs.
		/// </summary>
		public int FragmentProgramConstantBoolCount { get; set; }

		#endregion FragmentProgramConstantBoolCount Property

		public int GeometryProgramConstantFloatCount { get; set; }

		public int GeometryProgramConstantIntCount { get; set; }

		public int GeometryProgramConstantBoolCount { get; set; }

		public int GeometryProgramNumOutputVertices { get; set; }

		#region MultiRenderTargetCount Property

		/// <summary>
		/// The number of simultaneous render targets supported
		/// </summary>
		public int MultiRenderTargetCount { get; set; }

		#endregion MultiRenderTargetCount Property

		#region StencilBufferBitCount Property

		/// <summary>
		///    Stencil buffer bits available.
		/// </summary>
		private int _stencilBufferBitCount;

		/// <summary>
		///		Number of stencil buffer bits suppported by the hardware.
		/// </summary>
		public int StencilBufferBitCount
		{
			get
			{
				return this._stencilBufferBitCount;
			}
			set
			{
				this._stencilBufferBitCount = value;
			}
		}

		#endregion StencilBufferBitCount Property

		#region MaxLights Property

		/// <summary>
		///		Maximum number of lights that can be active in the scene at any given time.
		/// </summary>
		public int MaxLights { get; set; }

		#endregion MaxLights Property

		#region VendorName Property

		private GPUVendor _vendor = GPUVendor.Unknown;

		public GPUVendor Vendor
		{
			get
			{
				return this._vendor;
			}
			set
			{
				this._vendor = value;
			}
		}

		#endregion DeviceName Property

		#region DeviceName Property

		/// <summary>
		/// name of the adapter
		/// </summary>
		private string _deviceName = "";

		/// <summary>
		/// Name of the display adapter
		/// </summary>
		public string DeviceName
		{
			get
			{
				return this._deviceName;
			}
			set
			{
				this._deviceName = value;
			}
		}

		#endregion DeviceName Property

		#region DeviceVersion Property

		/// <summary>
		/// This is used to build a database of RSC's
		/// if a RSC with same name, but newer version is introduced, the older one 
		/// will be removed
		/// </summary>
		private DriverVersion _driverVersion;

		/// <summary>
		/// The driver version string
		/// </summary>
		public DriverVersion DriverVersion
		{
			get
			{
				return this._driverVersion;
			}
			set
			{
				this._driverVersion = value;
			}
		}

		#endregion DeviceVersion Property

		#region MaxPointSize Property

		/// <summary>
		/// The maximum point size
		/// </summary>
		public float MaxPointSize { get; set; }

		#endregion MaxPointSize Property

		#region NonPOW2TexturesLimited Property

		/// <summary>
		/// Are non-POW2 textures feature-limited?
		/// </summary>
		public bool NonPOW2TexturesLimited { get; set; }

		#endregion NonPOW2TexturesLimited Property

		#region VertexTextureUnitCount Property

		/// <summary>
		/// The number of vertex texture units supported
		/// </summary>
		public int VertexTextureUnitCount { get; set; }

		#endregion VertexTextureUnitCount Property

		#region VertexTextureUnitsShared Property

		/// <summary>
		/// Are vertex texture units shared with fragment processor?
		/// </summary>
		public bool VertexTextureUnitsShared { get; set; }

		#endregion VertexTextureUnitsShared Property

		#endregion Fields and Properties

		#region Construction and Destruction

		/// <summary>
		///    Default constructor.
		/// </summary>
		public RenderSystemCapabilities()
		{
			this._caps = 0;
		}

		#endregion Construction and Destruction

		#region Methods

		/// <summary>
		///    Returns true if the current hardware supports the requested feature.
		/// </summary>
		/// <param name="cap">Feature to query (i.e. Dot3 bump mapping)</param>
		/// <returns></returns>
		public bool HasCapability( Capabilities cap )
		{
			return ( this._caps & cap ) > 0;
		}

		/// <summary>
		///    Sets a flag stating the specified feature is supported.
		/// </summary>
		/// <param name="cap"></param>
		public void SetCapability( Capabilities cap )
		{
			this._caps |= cap;
		}

		/// <summary>
		///    Write all hardware capability information to registered listeners.
		/// </summary>
		public void Log()
		{
			Log( LogManager.Instance.DefaultLog );
		}

		/// <summary>
		///    Write all hardware capability information to registered listeners.
		/// </summary>
		public void Log( Log logMgr )
		{
			logMgr.Write( "---RenderSystem capabilities---" );
			logMgr.Write( "\t-GPU Vendor: {0}", VendorToString( Vendor ) );
			logMgr.Write( "\t-Device Name: {0}", this._deviceName );
			logMgr.Write( "\t-Driver Version: {0}", this._driverVersion.ToString() );
			logMgr.Write( "\t-Available texture units: {0}", TextureUnitCount );
			logMgr.Write( "\t-Maximum lights available: {0}", MaxLights );
			logMgr.Write( "\t-Hardware generation of mip-maps: {0}", ConvertBool( HasCapability( Capabilities.HardwareMipMaps ) ) );
			logMgr.Write( "\t-Texture blending: {0}", ConvertBool( HasCapability( Capabilities.Blending ) ) );
			logMgr.Write( "\t-Anisotropic texture filtering: {0}", ConvertBool( HasCapability( Capabilities.AnisotropicFiltering ) ) );
			logMgr.Write( "\t-Dot product texture operation: {0}", ConvertBool( HasCapability( Capabilities.Dot3 ) ) );
			logMgr.Write( "\t-Cube Mapping: {0}", ConvertBool( HasCapability( Capabilities.CubeMapping ) ) );

			logMgr.Write( "\t-Hardware stencil buffer: {0}", ConvertBool( HasCapability( Capabilities.StencilBuffer ) ) );

			if ( HasCapability( Capabilities.StencilBuffer ) )
			{
				logMgr.Write( "\t\t-Stencil depth: {0} bits", this._stencilBufferBitCount );
				logMgr.Write( "\t\t-Two sided stencil support: {0}", ConvertBool( HasCapability( Capabilities.TwoSidedStencil ) ) );
				logMgr.Write( "\t\t-Wrap stencil values: {0}", ConvertBool( HasCapability( Capabilities.StencilWrap ) ) );
			}

			logMgr.Write( "\t-Hardware vertex/index buffers: {0}", ConvertBool( HasCapability( Capabilities.VertexBuffer ) ) );

			logMgr.Write( "\t-Vertex programs: {0}", ConvertBool( HasCapability( Capabilities.VertexPrograms ) ) );

			if ( HasCapability( Capabilities.VertexPrograms ) )
			{
				logMgr.Write( "\t\t-Max vertex program version: {0}", MaxVertexProgramVersion );
			}

			logMgr.Write( "\t-Fragment programs: {0}", ConvertBool( HasCapability( Capabilities.FragmentPrograms ) ) );

			if ( HasCapability( Capabilities.FragmentPrograms ) )
			{
				logMgr.Write( "\t\t-Max fragment program version: {0}", MaxFragmentProgramVersion );
			}

			logMgr.Write( "\t-Texture compression: {0}", ConvertBool( HasCapability( Capabilities.TextureCompression ) ) );

			if ( HasCapability( Capabilities.TextureCompression ) )
			{
				logMgr.Write( "\t\t-DXT: {0}", ConvertBool( HasCapability( Capabilities.TextureCompressionDXT ) ) );
				logMgr.Write( "\t\t-VTC: {0}", ConvertBool( HasCapability( Capabilities.TextureCompressionVTC ) ) );
			}

			logMgr.Write( "\t-Scissor rectangle: {0}", ConvertBool( HasCapability( Capabilities.ScissorTest ) ) );
			logMgr.Write( "\t-Hardware Occlusion Query: {0}", ConvertBool( HasCapability( Capabilities.HardwareOcculusion ) ) );
			logMgr.Write( "\t-User clip planes: {0}", ConvertBool( HasCapability( Capabilities.UserClipPlanes ) ) );
			logMgr.Write( "\t-VertexElementType.UBYTE4: {0}", ConvertBool( HasCapability( Capabilities.VertexFormatUByte4 ) ) );
			logMgr.Write( "\t-Infinite far plane projection: {0}", ConvertBool( HasCapability( Capabilities.InfiniteFarPlane ) ) );

			logMgr.Write( "\t-Max Point Size: {0} ", MaxPointSize );
			logMgr.Write( "\t-Vertex texture fetch: {0} ", ConvertBool( HasCapability( Capabilities.VertexTextureFetch ) ) );
			if ( HasCapability( Capabilities.VertexTextureFetch ) )
			{
				logMgr.Write( "\t\t-Max vertex textures: {0}", VertexTextureUnitCount );
				logMgr.Write( "\t\t-Vertex textures shared: {0}", ConvertBool( VertexTextureUnitsShared ) );
			}
		}

		/// <summary>
		///     Helper method to convert true/false to yes/no.
		/// </summary>
		/// <param name="val">Bool bal.</param>
		/// <returns>"yes" if true, else "no".</returns>
		private string ConvertBool( bool val )
		{
			return val ? "yes" : "no";
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="vendorString"></param>
		/// <returns></returns>
		internal static GPUVendor VendorFromString( string vendorString )
		{
			GPUVendor ret = GPUVendor.Unknown;
			object lookUpResult = ScriptEnumAttribute.Lookup( vendorString, typeof( GPUVendor ) );

			if ( lookUpResult != null )
			{
				ret = (GPUVendor)lookUpResult;
			}

			return ret;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="v"></param>
		/// <returns></returns>
		internal static string VendorToString( GPUVendor v )
		{
			return ScriptEnumAttribute.GetScriptAttribute( (int)v, typeof( GPUVendor ) );
		}

		#endregion Methods

		#region ShaderProfiles

		/// <summary>
		/// Returns a set of all supported shader profiles
		/// </summary>
		public readonly ShaderProfiles ShaderProfiles = new ShaderProfiles();

		/// <summary>
		///  Adds the profile to the list of supported profiles
		/// </summary>
		public void AddShaderProfile( string profile )
		{
			this.ShaderProfiles.Add( profile );
		}

		/// <summary>
		/// Remove a given shader profile, if present.
		/// </summary>
		public void RemoveShaderProfile( string profile )
		{
			this.ShaderProfiles.Remove( profile );
		}

		/// <summary>
		/// Returns true if profile is in the list of supported profiles
		/// </summary>
		public bool IsShaderProfileSupported( string profile )
		{
			return this.ShaderProfiles.Contains( profile );
		}

		#endregion

		public void SetCategoryRelevant( CapabilitiesCategory d3D9, bool b )
		{
			// TODO: implement for IsCategoryRelevant()
		}

		public void UnsetCapability( Capabilities cap )
		{
			this._caps &= ~cap;
		}
	}
}
