#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006 Axiom Project Team

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
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Diagnostics;
using System.Reflection;

using Axiom.Core;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
    /// <summary>
    /// 	This serves as a way to query information about the capabilies of a 3D API and the
    /// 	users hardware configuration.  A RenderSystem should create and initialize an instance
    /// 	of this class during startup so that it will be available for use ASAP for checking caps.
    /// </summary>
    public class HardwareCapabilities
    {
        #region Member variables

        /// <summary>
        ///    Flag enum holding the bits that identify each supported feature.
        /// </summary>
        private Capabilities caps;
        /// <summary>
        ///    Max number of texture units available on the current hardware.
        /// </summary>
        private int numTextureUnits;
        /// <summary>
        ///    Max number of world matrices supported.
        /// </summary>
        private int numWorldMatrices;
        /// <summary>
        ///    The best vertex program version supported by the hardware.
        /// </summary>
        private string maxVertexProgramVersion;
        /// <summary>
        ///    The best fragment program version supported by the hardware.
        /// </summary>
        private string maxFragmentProgramVersion;
        /// <summary>
        ///    The number of floating point constants the current hardware supports for vertex programs.
        /// </summary>
        private int vertexProgramConstantFloatCount;
        /// <summary>
        ///    The number of integer constants the current hardware supports for vertex programs.
        /// </summary>
        private int vertexProgramConstantIntCount;
		/// <summary>
		///    The number of boolean constants the current hardware supports for vertex programs.
		/// </summary>
		private int vertexProgramConstantBoolCount;
		/// <summary>
        ///    The number of floating point constants the current hardware supports for fragment programs.
        /// </summary>
        private int fragmentProgramConstantFloatCount;
        /// <summary>
        ///    The number of integer constants the current hardware supports for fragment programs.
        /// </summary>
        private int fragmentProgramConstantIntCount;
		/// <summary>
		///    The number of boolean constants the current hardware supports for fragment programs.
		/// </summary>
		private int fragmentProgramConstantBoolCount;
		/// <summary>
        ///    Stencil buffer bits available.
        /// </summary>
        private int stencilBufferBits;
        /// <summary>
        ///    Maximum number of lights that can be active in the scene at any given time.
        /// </summary>
        private int maxLights;
		/// <summary>
		/// name of the adapter
		/// </summary>
		private string deviceName = "";
		/// <summary>
		/// version number of the driver
		/// </summary>
		private string driverVersion = "";
		/// <summary>
		/// The number of simultaneous render targets supported
		/// </summary>
		private int numMultiRenderTargets;
		/// <summary>
		/// The maximum point size
		/// </summary>
		private float maxPointSize;
		/// <summary>
		/// Are non-POW2 textures feature-limited?
		/// </summary>
		private bool nonPOW2TexturesLimited;
		/// <summary>
		/// The number of vertex texture units supported
		/// </summary>
		private int numVertexTextureUnits;
		/// <summary>
		/// Are vertex texture units shared with fragment processor?
		/// </summary>
		private bool vertexTextureUnitsShared;

        #endregion

        #region Constructors

        /// <summary>
        ///    Default constructor.
        /// </summary>
        public HardwareCapabilities()
        {
            caps = 0;
        }

        #endregion

        #region Properties

        /// <summary>
		/// Name of the display adapter
		/// </summary>
		public string DeviceName
		{
			get
			{
				return deviceName;
			}
			set
			{
				deviceName = value;
			}
		}
		/// <summary>
		/// The driver version string
		/// </summary>
		public string DriverVersion
		{
			get
			{
				return driverVersion;
			}
			set
			{
				driverVersion = value;
			}
		}

		/// <summary>
        ///    Max number of floating point constants supported by the hardware for fragment programs.
        /// </summary>
        public int FragmentProgramConstantFloatCount
        {
            get
            {
                return fragmentProgramConstantFloatCount;
            }
            set
            {
                fragmentProgramConstantFloatCount = value;
            }
        }

        /// <summary>
        ///    Max number of integer constants supported by the hardware for fragment programs.
        /// </summary>
        public int FragmentProgramConstantIntCount
        {
            get
            {
                return fragmentProgramConstantIntCount;
            }
            set
            {
                fragmentProgramConstantIntCount = value;
            }
        }

		/// <summary>
		///    Max number of boolean constants supported by the hardware for fragment programs.
		/// </summary>
		public int FragmentProgramConstantBoolCount
		{
			get
			{
				return fragmentProgramConstantBoolCount;
			}
			set
			{
				fragmentProgramConstantBoolCount = value;
			}
		}

		/// <summary>
        ///    Best fragment program version supported by the hardware.
        /// </summary>
        public string MaxFragmentProgramVersion
        {
            get
            {
                return maxFragmentProgramVersion;
            }
            set
            {
                maxFragmentProgramVersion = value;
            }
        }

        /// <summary>
        ///		Maximum number of lights that can be active in the scene at any given time.
        /// </summary>
        public int MaxLights
        {
            get
            {
                return maxLights;
            }
            set
            {
                maxLights = value;
            }
        }

        /// <summary>
        ///    Best vertex program version supported by the hardware.
        /// </summary>
        public string MaxVertexProgramVersion
        {
            get
            {
                return maxVertexProgramVersion;
            }
            set
            {
                maxVertexProgramVersion = value;
            }
        }

        /// <summary>
        ///		Reports on the number of texture units the graphics hardware has available.
        /// </summary>
        public int TextureUnitCount
        {
            get
            {
                return numTextureUnits;
            }
            set
            {
                numTextureUnits = value;
            }
        }

        /// <summary>
        ///    Max number of world matrices supported by the hardware.
        /// </summary>
        public int NumWorldMatrices
        {
            get
            {
                return numWorldMatrices;
            }
            set
            {
                numWorldMatrices = value;
            }
        }

        /// <summary>
        ///		Number of stencil buffer bits suppported by the hardware.
        /// </summary>
        public int StencilBufferBits
        {
            get
            {
                return stencilBufferBits;
            }
            set
            {
                stencilBufferBits = value;
            }
        }

        /// <summary>
        ///    Max number of floating point constants supported by the hardware for vertex programs.
        /// </summary>
        public int VertexProgramConstantFloatCount
        {
            get
            {
                return vertexProgramConstantFloatCount;
            }
            set
            {
                vertexProgramConstantFloatCount = value;
            }
        }

        /// <summary>
        ///    Max number of integer constants supported by the hardware for vertex programs.
        /// </summary>
        public int VertexProgramConstantIntCount
        {
            get
            {
                return vertexProgramConstantIntCount;
            }
            set
            {
                vertexProgramConstantIntCount = value;
            }
        }

		/// <summary>
		///    Max number of boolean constants supported by the hardware for vertex programs.
		/// </summary>
		public int VertexProgramConstantBoolCount
		{
			get
			{
				return vertexProgramConstantBoolCount;
			}
			set
			{
				vertexProgramConstantBoolCount = value;
			}
		}

		/// <summary>
		/// The number of simultaneous render targets supported
		/// </summary>
		public int NumMultiRenderTargets
		{
			get
			{
				return numMultiRenderTargets;
			}
			set
			{
				numMultiRenderTargets = value;
			}
		}

		/// <summary>
		/// The maximum point size
		/// </summary>
		public float MaxPointSize
		{
			get
			{
				return maxPointSize;
			}
			set
			{
				maxPointSize = value;
			}
		}

		/// <summary>
		/// Are non-POW2 textures feature-limited?
		/// </summary>
		public bool NonPOW2TexturesLimited
		{
			get
			{
				return nonPOW2TexturesLimited;
			}
			set
			{
				nonPOW2TexturesLimited = value;
			}
		}

		/// <summary>
		/// The number of vertex texture units supported
		/// </summary>
		public int NumVertexTextureUnits
		{
			get
			{
				return numVertexTextureUnits;
			}
			set
			{
				numVertexTextureUnits = value;
			}
		}

		/// <summary>
		/// Are vertex texture units shared with fragment processor?
		/// </summary>
		public bool VertexTextureUnitsShared
		{
			get
			{
				return vertexTextureUnitsShared;
			}
			set
			{
				vertexTextureUnitsShared = value;
			}
		}

		#endregion

        #region Methods

        /// <summary>
        ///    Returns true if the current hardware supports the requested feature.
        /// </summary>
        /// <param name="cap">Feature to query (i.e. Dot3 bump mapping)</param>
        /// <returns></returns>
        public bool HasCapability( Capabilities cap )
        {
            return ( caps & cap ) > 0;
        }

        /// <summary>
        ///    Sets a flag stating the specified feature is supported.
        /// </summary>
        /// <param name="cap"></param>
        public void SetCapability( Capabilities cap )
        {
            caps |= cap;
        }

        /// <summary>
        ///    Write all hardware capability information to registered listeners.
        /// </summary>
        public void Log()
        {
            LogManager logMgr = LogManager.Instance;

            logMgr.Write( "---RenderSystem capabilities---" );
			logMgr.Write( "\t-Adapter Name: {0}", deviceName );
			logMgr.Write( "\t-Driver Version: {0}", driverVersion );
            logMgr.Write( "\t-Available texture units: {0}", this.TextureUnitCount );
            logMgr.Write( "\t-Maximum lights available: {0}", this.MaxLights );
            logMgr.Write( "\t-Hardware generation of mip-maps: {0}", ConvertBool( HasCapability( Capabilities.HardwareMipMaps ) ) );
            logMgr.Write( "\t-Texture blending: {0}", ConvertBool( HasCapability( Capabilities.TextureBlending ) ) );
            logMgr.Write( "\t-Anisotropic texture filtering: {0}", ConvertBool( HasCapability( Capabilities.AnisotropicFiltering ) ) );
            logMgr.Write( "\t-Dot product texture operation: {0}", ConvertBool( HasCapability( Capabilities.Dot3 ) ) );
            logMgr.Write( "\t-Cube Mapping: {0}", ConvertBool( HasCapability( Capabilities.CubeMapping ) ) );

            logMgr.Write( "\t-Hardware stencil buffer: {0}", ConvertBool( HasCapability( Capabilities.StencilBuffer ) ) );

            if ( HasCapability( Capabilities.StencilBuffer ) )
            {
                logMgr.Write( "\t\t-Stencil depth: {0} bits", stencilBufferBits );
                logMgr.Write( "\t\t-Two sided stencil support: {0}", ConvertBool( HasCapability( Capabilities.TwoSidedStencil ) ) );
                logMgr.Write( "\t\t-Wrap stencil values: {0}", ConvertBool( HasCapability( Capabilities.StencilWrap ) ) );
            }

            logMgr.Write( "\t-Hardware vertex/index buffers: {0}", ConvertBool( HasCapability( Capabilities.VertexBuffer ) ) );

            logMgr.Write( "\t-Vertex programs: {0}", ConvertBool( HasCapability( Capabilities.VertexPrograms ) ) );

            if ( HasCapability( Capabilities.VertexPrograms ) )
            {
                logMgr.Write( "\t\t-Max vertex program version: {0}", this.MaxVertexProgramVersion );
            }

            logMgr.Write( "\t-Fragment programs: {0}", ConvertBool( HasCapability( Capabilities.FragmentPrograms ) ) );

            if ( HasCapability( Capabilities.FragmentPrograms ) )
            {
                logMgr.Write( "\t\t-Max fragment program version: {0}", this.MaxFragmentProgramVersion );
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
			if (HasCapability( Capabilities.VertexTextureFetch ))
			{
				logMgr.Write( "\t\t-Max vertex textures: {0}", NumVertexTextureUnits);
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

        #endregion
    }
}
