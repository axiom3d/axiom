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
using System.Diagnostics;
using System.Reflection;
using Axiom;

namespace Axiom
{
    /// <summary>
    /// 	This serves as a way to query information about the capabilies of a 3D API and the
    /// 	users hardware configuration.  A RenderSystem should create and initialize an instance
    /// 	of this class during startup so that it will be available for use ASAP for checking caps.
    /// </summary>
    public class HardwareCaps
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
        ///    The number of floating point constants the current hardware supports for fragment programs.
        /// </summary>
        private int fragmentProgramConstantFloatCount;
        /// <summary>
        ///    The number of integer constants the current hardware supports for fragment programs.
        /// </summary>
        private int fragmentProgramConstantIntCount;
        /// <summary>
        ///    Stencil buffer bits available.
        /// </summary>
        private int stencilBufferBits;
        /// <summary>
        ///    Maximum number of lights that can be active in the scene at any given time.
        /// </summary>
        private int maxLights;

        #endregion

        #region Constructors

        /// <summary>
        ///    Default constructor.
        /// </summary>
        public HardwareCaps()
        {
            caps = 0;
        }

        #endregion

        #region Properties

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

        #endregion

        #region Methods

        /// <summary>
        ///    Returns true if the current hardware supports the requested feature.
        /// </summary>
        /// <param name="cap">Feature to query (i.e. Dot3 bump mapping)</param>
        /// <returns></returns>
        public bool CheckCap( Capabilities cap )
        {
            return ( caps & cap ) > 0;
        }

        /// <summary>
        ///    Sets a flag stating the specified feature is supported.
        /// </summary>
        /// <param name="cap"></param>
        public void SetCap( Capabilities cap )
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
            logMgr.Write( "\t-Available texture units: {0}", this.TextureUnitCount );
            logMgr.Write( "\t-Maximum lights available: {0}", this.MaxLights );
            logMgr.Write( "\t-Hardware generation of mip-maps: {0}", ConvertBool( CheckCap( Capabilities.HardwareMipMaps ) ) );
            logMgr.Write( "\t-Texture blending: {0}", ConvertBool( CheckCap( Capabilities.TextureBlending ) ) );
            logMgr.Write( "\t-Anisotropic texture filtering: {0}", ConvertBool( CheckCap( Capabilities.AnisotropicFiltering ) ) );
            logMgr.Write( "\t-Dot product texture operation: {0}", ConvertBool( CheckCap( Capabilities.Dot3 ) ) );
            logMgr.Write( "\t-Cube Mapping: {0}", ConvertBool( CheckCap( Capabilities.CubeMapping ) ) );

            logMgr.Write( "\t-Hardware stencil buffer: {0}", ConvertBool( CheckCap( Capabilities.StencilBuffer ) ) );

            if ( CheckCap( Capabilities.StencilBuffer ) )
            {
                logMgr.Write( "\t\t-Stencil depth: {0} bits", stencilBufferBits );
                logMgr.Write( "\t\t-Two sided stencil support: {0}", ConvertBool( CheckCap( Capabilities.TwoSidedStencil ) ) );
                logMgr.Write( "\t\t-Wrap stencil values: {0}", ConvertBool( CheckCap( Capabilities.StencilWrap ) ) );
            }

            logMgr.Write( "\t-Hardware vertex/index buffers: {0}", ConvertBool( CheckCap( Capabilities.VertexBuffer ) ) );

            logMgr.Write( "\t-Vertex programs: {0}", ConvertBool( CheckCap( Capabilities.VertexPrograms ) ) );

            if ( CheckCap( Capabilities.VertexPrograms ) )
            {
                logMgr.Write( "\t\t-Max vertex program version: {0}", this.MaxVertexProgramVersion );
            }

            logMgr.Write( "\t-Fragment programs: {0}", ConvertBool( CheckCap( Capabilities.FragmentPrograms ) ) );

            if ( CheckCap( Capabilities.FragmentPrograms ) )
            {
                logMgr.Write( "\t\t-Max fragment program version: {0}", this.MaxFragmentProgramVersion );
            }

            logMgr.Write( "\t-Texture compression: {0}", ConvertBool( CheckCap( Capabilities.TextureCompression ) ) );

            if ( CheckCap( Capabilities.TextureCompression ) )
            {
                logMgr.Write( "\t\t-DXT: {0}", ConvertBool( CheckCap( Capabilities.TextureCompressionDXT ) ) );
                logMgr.Write( "\t\t-VTC: {0}", ConvertBool( CheckCap( Capabilities.TextureCompressionVTC ) ) );
            }

            logMgr.Write( "\t-Scissor rectangle: {0}", ConvertBool( CheckCap( Capabilities.ScissorTest ) ) );
            logMgr.Write( "\t-Hardware Occlusion Query: {0}", ConvertBool( CheckCap( Capabilities.HardwareOcculusion ) ) );
            logMgr.Write( "\t-User clip planes: {0}", ConvertBool( CheckCap( Capabilities.UserClipPlanes ) ) );
            logMgr.Write( "\t-VertexElementType.UBYTE4: {0}", ConvertBool( CheckCap( Capabilities.VertexFormatUByte4 ) ) );
            logMgr.Write( "\t-Infinite far plane projection: {0}", ConvertBool( CheckCap( Capabilities.InfiniteFarPlane ) ) );
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
