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
using Axiom.Core;
using Axiom.MathLib;
using Axiom.Graphics;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using D3D = Microsoft.DirectX.Direct3D;

namespace Axiom.RenderSystems.DirectX9 {
	/// <summary>
	/// 	Direct3D implementation of a few things common to low-level vertex & fragment programs
	/// </summary>
	public abstract class D3DGpuProgram : GpuProgram {
        #region Fields

        /// <summary>
        ///    Reference to the current D3D device object.
        /// </summary>
        protected D3D.Device device;
        /// <summary>
        ///     Microsode set externally, most likely from the HLSL compiler.
        /// </summary>
        protected D3D.GraphicsStream externalMicrocode;

        #endregion Fields

        #region Constructor
		
		public D3DGpuProgram(string name, GpuProgramType type, D3D.Device device, string syntaxCode) : base(name, type, syntaxCode) {
            this.device = device;
		}

        #endregion Constructor

        #region GpuProgram Members

        /// <summary>
        ///     Overridden to allow for loading microcode from external sources.
        /// </summary>
        public override void Load() {
            if(externalMicrocode != null) {
                // unload if needed
                if(isLoaded) {
                    Unload();
                }

                // creates the shader from an external microcode source
                // for example, a compiled HLSL program
                LoadFromMicrocode(externalMicrocode);
                isLoaded = true;
            }
            else {
                // call base implementation
                base.Load();
            }
        }

        /// <summary>
        ///     Loads a D3D shader from the assembler source.
        /// </summary>
        protected override void LoadFromSource() {
            string errors;

            // load the shader from the source string
            GraphicsStream microcode = ShaderLoader.FromString(source, null, 0, out errors);

            if(errors != null) {
                string msg = string.Format("Error while compiling pixel shader '{0}':\n {1}", name, errors);
                throw new AxiomException(msg);
            }

            // load the code into a shader object (polymorphic)
            LoadFromMicrocode(microcode);
        }

        #endregion GpuProgram Members

        #region Methods

        /// <summary>
        ///     Loads a shader object from the supplied microcode.
        /// </summary>
        /// <param name="microcode">
        ///     GraphicsStream that contains the assembler instructions for the program.
        /// </param>
        protected abstract void LoadFromMicrocode(D3D.GraphicsStream microcode);

        #endregion Methods

        #region Properties

        /// <summary>
        ///     Gets/Sets a prepared chunk of microcode to use during Load
        ///     rather than loading from file or a string.
        /// </summary>
        /// <remarks>
        ///     This is used by the HLSL compiler once it compiles down to low
        ///     level microcode, which can then be loaded into a low level GPU
        ///     program.
        /// </remarks>
        internal D3D.GraphicsStream ExternalMicrocode {
            get {
                return externalMicrocode;
            }
            set {
                externalMicrocode = value;
            }
        }

        #endregion Properties
	}

    /// <summary>
    ///    Direct3D implementation of low-level vertex programs.
    /// </summary>
    public class D3DVertexProgram : D3DGpuProgram {
        #region Fields

        /// <summary>
        ///    Reference to the current D3D VertexShader object.
        /// </summary>
        protected D3D.VertexShader vertexShader;

        #endregion Fields

        #region Constructor

        internal D3DVertexProgram(string name, D3D.Device device, string syntaxCode) : base(name, GpuProgramType.Vertex, device, syntaxCode) {}

        #endregion Constructor

        #region D3DGpuProgram Memebers

        protected override void LoadFromMicrocode(GraphicsStream microcode) {
            // create the new vertex shader
            vertexShader = new VertexShader(device, microcode);
        }

        #endregion D3DGpuProgram Memebers

        #region GpuProgram Members

        /// <summary>
        ///     Unloads the VertexShader object.
        /// </summary>
        public override void Unload() {
            base.Unload ();

            if(vertexShader != null) {
                vertexShader.Dispose();
            }
        }

        #endregion GpuProgram Members

        #region Properties

        /// <summary>
        ///    Used internally by the D3DRenderSystem to get a reference to the underlying
        ///    VertexShader object.
        /// </summary>
        internal D3D.VertexShader VertexShader {
            get {
                return vertexShader;
            }
        }

        #endregion Properties
    }

    /// <summary>
    ///    Direct3D implementation of low-level vertex programs.
    /// </summary>
    public class D3DFragmentProgram : D3DGpuProgram {
        #region Fields

        /// <summary>
        ///    Reference to the current D3D PixelShader object.
        /// </summary>
        protected D3D.PixelShader pixelShader;

        #endregion Fields

        #region Constructors

        internal D3DFragmentProgram(string name, D3D.Device device, string syntaxCode) : base(name, GpuProgramType.Fragment, device, syntaxCode) {}

        #endregion Constructors

        #region D3DGpuProgram Memebers

        protected override void LoadFromMicrocode(GraphicsStream microcode) {
            // create a new pixel shader
            pixelShader = new PixelShader(device, microcode);
        }

        #endregion D3DGpuProgram Members

        #region GpuProgram Members

        /// <summary>
        ///     Unloads the PixelShader object.
        /// </summary>
        public override void Unload() {
            base.Unload();

            if(pixelShader != null) {
                pixelShader.Dispose();
            }
        }

        #endregion GpuProgram Members

        #region Properties

        /// <summary>
        ///    Used internally by the D3DRenderSystem to get a reference to the underlying
        ///    PixelShader object.
        /// </summary>
        internal D3D.PixelShader PixelShader {
            get {
                return pixelShader;
            }
        }

        #endregion Properties
    }
}
