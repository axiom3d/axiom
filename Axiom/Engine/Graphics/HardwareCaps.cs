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

namespace Axiom.Graphics {
    /// <summary>
    /// 	This serves as a way to query information about the capabilies of a 3D API and the
    /// 	users hardware configuration.  A RenderSystem should create and initialize an instance
    /// 	of this class during startup so that it will be available for use ASAP for checking caps.
    /// </summary>
    public class HardwareCaps {
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
        public HardwareCaps() {
            caps = 0;
        }
		
        #endregion
		
        #region Properties

        /// <summary>
        ///    Max number of floating point constants supported by the hardware for fragment programs.
        /// </summary>
        public int FragmentProgramConstantFloatCount {
            get {
                return fragmentProgramConstantFloatCount;
            }
            set {
                fragmentProgramConstantFloatCount = value;
            }
        }

        /// <summary>
        ///    Max number of integer constants supported by the hardware for fragment programs.
        /// </summary>
        public int FragmentProgramConstantIntCount {
            get {
                return fragmentProgramConstantIntCount;
            }
            set {
                fragmentProgramConstantIntCount = value;
            }
        }

        /// <summary>
        ///    Best fragment program version supported by the hardware.
        /// </summary>
        public string MaxFragmentProgramVersion {
            get {
                return maxFragmentProgramVersion;
            }
            set {
                maxFragmentProgramVersion = value;
            }
        }

        /// <summary>
        ///		Maximum number of lights that can be active in the scene at any given time.
        /// </summary>
        public int MaxLights {
            get { 
                return maxLights; 
            }
            set { 
                maxLights = value; 
            }
        }

        /// <summary>
        ///    Best vertex program version supported by the hardware.
        /// </summary>
        public string MaxVertexProgramVersion {
            get {
                return maxVertexProgramVersion;
            }
            set {
                maxVertexProgramVersion = value;
            }
        }

        /// <summary>
        ///		Reports on the number of texture units the graphics hardware has available.
        /// </summary>
        public int TextureUnitCount {
            get { 
                return numTextureUnits; 
            }
            set { 
                numTextureUnits = value; 
            }
        }

        /// <summary>
        ///    Max number of world matrices supported by the hardware.
        /// </summary>
        public int NumWorldMatrices {
            get {
                return numWorldMatrices;
            }
            set {
                numWorldMatrices = value;
            }
        }

        /// <summary>
        ///		Number of stencil buffer bits suppported by the hardware.
        /// </summary>
        public int StencilBufferBits {
            get { 
                return stencilBufferBits; 
            }
            set { 
                stencilBufferBits = value; 
            }
        }

        /// <summary>
        ///    Max number of floating point constants supported by the hardware for vertex programs.
        /// </summary>
        public int VertexProgramConstantFloatCount {
            get {
                return vertexProgramConstantFloatCount;
            }
            set {
                vertexProgramConstantFloatCount = value;
            }
        }

        /// <summary>
        ///    Max number of integer constants supported by the hardware for vertex programs.
        /// </summary>
        public int VertexProgramConstantIntCount {
            get {
                return vertexProgramConstantIntCount;
            }
            set {
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
        public bool CheckCap(Capabilities cap) {
            return (caps & cap) > 0;
        }

        /// <summary>
        ///    Sets a flag stating the specified feature is supported.
        /// </summary>
        /// <param name="cap"></param>
        public void SetCap(Capabilities cap) {
            caps |= cap;
        }

        /// <summary>
        ///    Write all hardware capability information to registered listeners.
        /// </summary>
        public void Log() { 
            Trace.WriteLine("---Hardware Capabilities---");
            Trace.WriteLine("Available texture units: " + this.TextureUnitCount);
            Trace.WriteLine("Maximum lights available: " + this.MaxLights);
            Trace.WriteLineIf(CheckCap(Capabilities.AnisotropicFiltering), "\t-Anisotropic Filtering");
            Trace.WriteLineIf(CheckCap(Capabilities.CubeMapping), "\t-Cube Mapping");
            Trace.WriteLineIf(CheckCap(Capabilities.Dot3), "\t-Dot3 Bump Mapping");
            Trace.WriteLineIf(CheckCap(Capabilities.HardwareMipMaps), "\t-Hardware mip-mapping");
            Trace.WriteLineIf(CheckCap(Capabilities.MultiTexturing), "\t-Multi-texturing");
            Trace.WriteLineIf(CheckCap(Capabilities.TextureBlending), "\t-Texture Blending");
            Trace.WriteLineIf(CheckCap(Capabilities.TextureCompression), "\t-Texture Compression");
            Trace.WriteLineIf(CheckCap(Capabilities.TextureCompressionDXT), "\t-DXT Texture Compression");
            Trace.WriteLineIf(CheckCap(Capabilities.TextureCompressionVTC), "\t-VTC Texture Compression");
            Trace.WriteLineIf(CheckCap(Capabilities.VertexBuffer), "\t-Vertex Buffer Objects");
            Trace.WriteLineIf(CheckCap(Capabilities.VertexPrograms), string.Format("\t-Vertex Programs, max version: {0}", this.MaxVertexProgramVersion));
            Trace.WriteLineIf(CheckCap(Capabilities.FragmentPrograms), string.Format("\t-Fragment Programs, max version: {0}", this.MaxFragmentProgramVersion));
			Trace.WriteLineIf(CheckCap(Capabilities.StencilBuffer), string.Format("\t-Stencil Buffer: {0} bits", stencilBufferBits));
			Trace.WriteLineIf(CheckCap(Capabilities.TwoSidedStencil), "\t\t-Two Sided Stencil");
			Trace.WriteLineIf(CheckCap(Capabilities.StencilWrap), "\t\t-Stencil Wrap");
			Trace.WriteLineIf(CheckCap(Capabilities.UserClipPlanes), "\t-User Clip Planes");
			Trace.WriteLineIf(CheckCap(Capabilities.HardwareOcculusion), "\t-Hardware Occlusion Queries");
			Trace.WriteLineIf(CheckCap(Capabilities.InfiniteFarPlane), "\t-Infinite Far Plane");
        }

        #endregion
    }
}
