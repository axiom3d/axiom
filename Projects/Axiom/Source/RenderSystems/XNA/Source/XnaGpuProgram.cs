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
//     <id value="$Id: D3DGpuProgram.cs 884 2006-09-14 06:32:07Z borrillis $"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Diagnostics;

using Axiom.Core;
using Axiom.Math;
using Axiom.Graphics;

using XNA = Microsoft.Xna.Framework;
using XFG = Microsoft.Xna.Framework.Graphics;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.Xna
{
	/// <summary>
	/// 	Xna implementation of a few things common to low-level vertex &amp; fragment programs
	/// </summary>
	public abstract class XnaGpuProgram : GpuProgram
	{
		#region Fields and Properties

		/// <summary>
		///    Reference to the current XNA device object.
		/// </summary>
		protected XFG.GraphicsDevice device;

		#region ExternalMicrocode Property
		/// <summary>
		///     Microsode set externally, most likely from the HLSL compiler.
		/// </summary>
		protected XFG.CompiledShader externalMicrocode;
		/// <summary>
		///     Gets/Sets a prepared chunk of microcode to use during Load
		///     rather than loading from file or a string.
		/// </summary>
		/// <remarks>
		///     This is used by the HLSL compiler once it compiles down to low
		///     level microcode, which can then be loaded into a low level GPU
		///     program.
		/// </remarks>
		internal XFG.CompiledShader ExternalMicrocode
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

		#endregion ExternalMicrocode Property
			
		#endregion Fields and Properties

		#region Constructor

		public XnaGpuProgram( string name, GpuProgramType type, XFG.GraphicsDevice device, string syntaxCode )
			: base( name, type, syntaxCode )
		{
			this.device = device;
		}

		#endregion Constructor

		#region Methods

		/// <summary>
		///     Loads a shader object from the supplied microcode.
		/// </summary>
		/// <param name="microcode">
		///     contains the assembler instructions for the program.
		/// </param>
		protected abstract void LoadFromMicrocode( XFG.CompiledShader microcode );

		#endregion Methods

		#region GpuProgram Members

		/// <summary>
		///     Overridden to allow for loading microcode from external sources.
		/// </summary>
		public override void Load()
		{
			if ( externalMicrocode.ShaderVersion != null )
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
		///     Loads a Xna shader from the assembler source.
		/// </summary>
		protected override void LoadFromSource()
		{
			string errors;

			// load the shader from the source string
			XFG.CompiledShader microcode = XFG.ShaderCompiler.AssembleFromSource( source, null, null, XFG.CompilerOptions.Debug, XNA.TargetPlatform.Windows );
			errors = microcode.ErrorsAndWarnings;
			if ( errors != null && errors.Length != 0 )
			{
				LogManager.Instance.Write( "Error while compiling pixel shader '{0}':\n {1}", name, errors );
				return;
			}

			// load the code into a shader object (polymorphic)
			LoadFromMicrocode( microcode );
		}

		#endregion GpuProgram Members

	}

	/// <summary>
	///    Xna implementation of low-level vertex programs.
	/// </summary>
	public class XnaVertexProgram : XnaGpuProgram
	{
		#region Fields and Properties

		#region VertexShader Property

		/// <summary>
		///    Reference to the current Xna VertexShader object.
		/// </summary>
		protected XFG.VertexShader vertexShader;
		/// <summary>
		///    Used internally by the XnaRenderSystem to get a reference to the underlying VertexShader object.
		/// </summary>
		internal XFG.VertexShader VertexShader
		{
			get
			{
				return vertexShader;
			}
		}
		#endregion VertexShader Property

		#endregion Fields and Properties

		#region Constructor

		internal XnaVertexProgram( string name, XFG.GraphicsDevice device, string syntaxCode )
			: base( name, GpuProgramType.Vertex, device, syntaxCode )
		{
		}

		#endregion Constructor

		#region XnaGpuProgram Memebers

		/// <summary>
		///     Loads a shader object from the supplied microcode.
		/// </summary>
		/// <param name="microcode">
		///     contains the assembler instructions for the program.
		/// </param>
		protected override void LoadFromMicrocode( XFG.CompiledShader microcode )
		{
			// create the new vertex shader
			vertexShader = new XFG.VertexShader( device, microcode.GetShaderCode() );
		}

		#endregion XnaGpuProgram Memebers

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

	}

	/// <summary>
	///    Xna implementation of low-level vertex programs.
	/// </summary>
	public class XnaFragmentProgram : XnaGpuProgram
	{
		#region Fields and Properties

		#region PixelShader Property

		/// <summary>
		///    Reference to the current Xna PixelShader object.
		/// </summary>
		protected XFG.PixelShader pixelShader;
		/// <summary>
		///    Used internally by the XnaRenderSystem to get a reference to the underlying
		///    PixelShader object.
		/// </summary>
		internal XFG.PixelShader PixelShader
		{
			get
			{
				return pixelShader;
			}
		}

		#endregion PixelShader Property
			
		#endregion Fields and Properties

		#region Constructors

		internal XnaFragmentProgram( string name, XFG.GraphicsDevice device, string syntaxCode )
			: base( name, GpuProgramType.Fragment, device, syntaxCode )
		{
		}

		#endregion Constructors

		#region XnaGpuProgram Memebers

		/// <summary>
		///     Loads a shader object from the supplied microcode.
		/// </summary>
		/// <param name="microcode">
		///     contains the assembler instructions for the program.
		/// </param>
		protected override void LoadFromMicrocode( XFG.CompiledShader microcode )
		{
			// create a new pixel shader
			pixelShader = new XFG.PixelShader( device, microcode.GetShaderCode() );
		}

		#endregion XnaGpuProgram Members

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

	}
}
