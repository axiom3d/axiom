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

#region Namespace Declarations

using System;
using System.Diagnostics;

using DX = Microsoft.Xna.Framework;
using D3D = Microsoft.Xna.Framework;

using Axiom;
using Axiom.Graphics;
using Axiom.Core;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.XNA
{
    /// <summary>
    /// 	XNA implementation of a few things common to low-level vertex & fragment programs
    /// </summary>
    public abstract class XNAGpuProgram : GpuProgram
    {
        #region Fields

        /// <summary>
        ///    Reference to the current D3D device object.
        /// </summary>
        protected D3D.Graphics.GraphicsDevice device;
        /// <summary>
        ///     Microcode set externally, most likely from the HLSL compiler.
        /// </summary>
        protected DX.Graphics.CompiledShader externalMicrocode;

        #endregion Fields

        #region Constructor

        public XNAGpuProgram( string name, GpuProgramType type, D3D.Graphics.GraphicsDevice device, string syntaxCode )
            : base( name, type, syntaxCode )
        {
            this.device = device;
        }

        #endregion Constructor

        #region GpuProgram Members

        /// <summary>
        ///     Overridden to allow for loading microcode from external sources.
        /// </summary>
        public override void Load()
        {
            if ( externalMicrocode.ShaderSize != 0 )
            {
                // unload if needed
                if ( isLoaded )
                {
                    Unload();
                }

                // creates the shader from an external microcode source
                // for example, a compiled HLSL program
                LoadFromMicrocode( externalMicrocode );
                isLoaded = true;
            }
            else
            {
                // call base implementation
                base.Load();
            }
        }

        /// <summary>
        ///     Loads a D3D shader from the assembler source.
        /// </summary>
        protected override void LoadFromSource()
        {
            string errors = null;

            // load the shader from the source string
            DX.Graphics.CompiledShader microcode = DX.Graphics.ShaderCompiler.AssembleFromSource( source, null, null, D3D.Graphics.CompilerOptions.None );

            if ( errors != null && errors != string.Empty )
            {
                string msg = string.Format( "Error while compiling pixel shader '{0}':\n {1}", name, errors );
                throw new AxiomException( msg );
            }

            // load the code into a shader object (polymorphic)
            LoadFromMicrocode( microcode );
        }

        #endregion GpuProgram Members

        #region Methods

        /// <summary>
        ///     Loads a shader object from the supplied microcode.
        /// </summary>
        /// <param name="microcode">
        ///     GraphicsBuffer that contains the assembler instructions for the program.
        /// </param>
        protected abstract void LoadFromMicrocode( DX.Graphics.CompiledShader microcode );

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
        internal DX.Graphics.CompiledShader ExternalMicrocode
        {
            get
            {
                return externalMicrocode;
            }
            set
            {
                externalMicrocode = value;
            }
        }

        #endregion Properties
    }

    /// <summary>
    ///    XNA implementation of low-level vertex programs.
    /// </summary>
    public class XNAVertexProgram : XNAGpuProgram
    {
        #region Fields

        /// <summary>
        ///    Reference to the current D3D VertexShader object.
        /// </summary>
        protected D3D.Graphics.VertexShader vertexShader;

        #endregion Fields

        #region Constructor

        internal XNAVertexProgram( string name, D3D.Graphics.GraphicsDevice device, string syntaxCode )
            : base( name, GpuProgramType.Vertex, device, syntaxCode )
        {
        }

        #endregion Constructor

        #region D3DGpuProgram Memebers

        protected override void LoadFromMicrocode( DX.Graphics.CompiledShader microcode )
        {
            // create the new vertex shader
            vertexShader = new D3D.Graphics.VertexShader( device, microcode.GetShaderCode() );
        }

        #endregion D3DGpuProgram Memebers

        #region GpuProgram Members

        /// <summary>
        ///     Unloads the VertexShader object.
        /// </summary>
        public override void Unload()
        {
            base.Unload();

            if ( vertexShader != null )
            {
                vertexShader.Dispose();
            }
        }

        #endregion GpuProgram Members

        #region Properties

        /// <summary>
        ///    Used internally by the D3DRenderSystem to get a reference to the underlying
        ///    VertexShader object.
        /// </summary>
        internal D3D.Graphics.VertexShader VertexShader
        {
            get
            {
                return vertexShader;
            }
        }

        #endregion Properties
    }

    /// <summary>
    ///    XNA implementation of low-level vertex programs.
    /// </summary>
    public class XNAFragmentProgram : XNAGpuProgram
    {
        #region Fields

        /// <summary>
        ///    Reference to the current D3D PixelShader object.
        /// </summary>
        protected D3D.Graphics.PixelShader pixelShader;

        #endregion Fields

        #region Constructors

        internal XNAFragmentProgram( string name, D3D.Graphics.GraphicsDevice device, string syntaxCode )
            : base( name, GpuProgramType.Fragment, device, syntaxCode )
        {
        }

        #endregion Constructors

        #region D3DGpuProgram Memebers

        protected override void LoadFromMicrocode( DX.Graphics.CompiledShader microcode )
        {
            // create a new pixel shader
            pixelShader = new D3D.Graphics.PixelShader( device, microcode.GetShaderCode() );
        }

        #endregion D3DGpuProgram Members

        #region GpuProgram Members

        /// <summary>
        ///     Unloads the PixelShader object.
        /// </summary>
        public override void Unload()
        {
            base.Unload();

            if ( pixelShader != null )
            {
                pixelShader.Dispose();
            }
        }

        #endregion GpuProgram Members

        #region Properties

        /// <summary>
        ///    Used internally by the D3DRenderSystem to get a reference to the underlying
        ///    PixelShader object.
        /// </summary>
        internal D3D.Graphics.PixelShader PixelShader
        {
            get
            {
                return pixelShader;
            }
        }

        #endregion Properties
    }
}
