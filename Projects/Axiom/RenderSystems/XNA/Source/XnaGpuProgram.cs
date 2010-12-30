#region LGPL License
/*
Axiom Graphics Engine Library
Copyright � 2003-2011 Axiom Project Team

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

using System;
using System.Diagnostics;

using Axiom.Core;
using Axiom.Math;
using Axiom.Graphics;
using ResourceHandle = System.UInt64;

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

		#region ShaderCode Property
		/// <summary>
		///     ShaderCode set externally, most likely from the HLSL compiler.
		/// </summary>

		protected byte[] shaderCode;



		/// <summary>
		///     Gets/Sets a prepared chunk of ShaderCode to use during Load
		///     rather than loading from file or a string.
		/// </summary>
		/// <remarks>
		///     This is used by the HLSL compiler once it compiles down to low
		///     level shader code, which can then be loaded into a low level GPU
		///     program.
		/// </remarks
		internal byte[] ShaderCode
		{
			get
			{
				return shaderCode;
			}
			set
			{
				shaderCode = value;
			}
		}


		#endregion ShaderCode Property

		#endregion Fields and Properties

		#region Constructor

		public XnaGpuProgram( ResourceManager parent, string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader, XFG.GraphicsDevice device )
			: base( parent, name, handle, group, isManual, loader )
		{
			this.device = device;
			this.shaderCode = null;
		}

		#endregion Constructor

		#region Methods

		/// <summary>
		///     Loads a shader object from the supplied shader code.
		/// </summary>
		protected abstract void LoadFromShaderCode();

		#endregion Methods

		#region GpuProgram Members

		/// <summary>
		///     Overridden to allow for loading microcode from external sources.
		/// </summary>
		protected override void load()
		{
			if ( shaderCode != null && shaderCode.Length > 0 )
			{
				// unload if needed
				if ( IsLoaded )
				{
					Unload();
				}


				// creates the shader from an external microcode source
				// for example, a compiled HLSL program
				LoadFromShaderCode();
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
			//we should never get here
		}

		#endregion GpuProgram Members

		#region Properties
		public override int SamplerCount
		{
			get
			{
				//switch (target)
				//{
				//    case "ps_1_1":
				//    case "ps_1_2":
				//    case "ps_1_3":
				return 4;
				//case "ps_1_4":
				//    return 6;
				//case "ps_2_0":
				//case "ps_2_x":
				//case "ps_3_0":
				//case "ps_3_x":
				//    return 16;
				//default:
				//    throw new AxiomException("Attempted to query sample count for unknown shader profile({0}).", target);
				//}

				// return 0;
			}
		}
		#endregion

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

		internal XnaVertexProgram( ResourceManager parent, string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader, XFG.GraphicsDevice device )
			: base( parent, name, handle, group, isManual, loader, device )
		{
		}

		#endregion Constructor

		#region XnaGpuProgram Members

		/// <summary>
		///     Loads a vertex shader from shaderCode member variable
		/// </summary>
		protected override void LoadFromShaderCode()
		{
			// create the new vertex shader
			vertexShader = new XFG.VertexShader( device, shaderCode );
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
                if (!vertexShader.IsDisposed)
				vertexShader.Dispose();

                vertexShader = null;
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

		internal XnaFragmentProgram( ResourceManager parent, string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader, XFG.GraphicsDevice device )
			: base( parent, name, handle, group, isManual, loader, device )
		{
		}

		#endregion Constructors

		#region XnaGpuProgram Members

		/// <summary>
		///     Loads a pixel shader from shaderCode member variable
		/// </summary>
		protected override void LoadFromShaderCode()
		{
			// create a new pixel shader
			pixelShader = new XFG.PixelShader( device, shaderCode );
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
                if (!pixelShader.IsDisposed)
				pixelShader.Dispose();

                pixelShader = null;
			}
		}

		#endregion GpuProgram Members

	}
}
