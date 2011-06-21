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

using DX = SlimDX;
using D3D = SlimDX.Direct3D9;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9
{
	/// <summary>
	/// 	Direct3D implementation of a few things common to low-level vertex & fragment programs
	/// </summary>
	public abstract class D3DGpuProgram : GpuProgram
	{
		#region Fields

		/// <summary>
		///    Reference to the current D3D device object.
		/// </summary>
		protected D3D.Device device;
		/// <summary>
		///     Microsode set externally, most likely from the HLSL compiler.
		/// </summary>
		protected D3D.ShaderBytecode externalMicrocode;

		#endregion Fields

		#region Construction and Destruction

		protected D3DGpuProgram( ResourceManager parent, string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader, D3D.Device device )
			: base( parent, name, handle, group, isManual, loader )
		{
			this.device = device;
		}

		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					if ( externalMicrocode != null && !externalMicrocode.Disposed )
						externalMicrocode.Dispose();
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
			}

			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );
		}

		#endregion Construction and Destruction

		#region GpuProgram Members

		/// <summary>
		///     Overridden to allow for loading microcode from external sources.
		/// </summary>
		protected override void load()
		{
			if ( externalMicrocode != null )
			{
				// unload if needed
				if ( IsLoaded )
				{
					Unload();
				}

				// creates the shader from an external microcode source
				// for example, a compiled HLSL program
				LoadFromMicrocode( externalMicrocode );
			}
			else
			{
				// call base implementation
				base.load();
			}
		}

		/// <summary>
		///     Loads a D3D shader from the assembler source.
		/// </summary>
		protected override void LoadFromSource()
		{
			string errors = null;

			// load the shader from the source string
			DX.Direct3D9.ShaderBytecode microcode = D3D.ShaderBytecode.Assemble( source, null, null, 0, out errors );

			if ( !string.IsNullOrEmpty( errors ) )
			{
				LogManager.Instance.Write( "Error while compiling pixel shader '{0}':\n {1}", Name, errors );
				return;
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
		///     GraphicsStream that contains the assembler instructions for the program.
		/// </param>
		protected abstract void LoadFromMicrocode( D3D.ShaderBytecode microcode );

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
		internal D3D.ShaderBytecode ExternalMicrocode
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
	///    Direct3D implementation of low-level vertex programs.
	/// </summary>
	public class D3DVertexProgram : D3DGpuProgram
	{
		#region Fields

		/// <summary>
		///    Reference to the current D3D VertexShader object.
		/// </summary>
		protected D3D.VertexShader vertexShader;

		#endregion Fields

		#region Construction and Destruction

		internal D3DVertexProgram( ResourceManager parent, string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader, D3D.Device device )
			: base( parent, name, handle, group, isManual, loader, device )
		{
			type = GpuProgramType.Vertex;
		}

		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					if ( vertexShader != null && !vertexShader.Disposed )
						vertexShader.Dispose();
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
			}

			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );
		}

		#endregion Construction and Destruction

		#region D3DGpuProgram Memebers

		protected override void LoadFromMicrocode( D3D.ShaderBytecode microcode )
		{
			// create the new vertex shader
			vertexShader = new D3D.VertexShader( device, microcode );
		}

		#endregion D3DGpuProgram Memebers

		#region GpuProgram Members

		/// <summary>
		///     Unloads the VertexShader object.
		/// </summary>
		protected override void unload()
		{
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
		internal D3D.VertexShader VertexShader
		{
			get
			{
				return vertexShader;
			}
		}

		public override int SamplerCount
		{
			get
			{
				throw new AxiomException( "Attempted to query sample count for vertex shader." );
			}
		}

		#endregion Properties
	}

	/// <summary>
	///    Direct3D implementation of low-level vertex programs.
	/// </summary>
	public class D3DFragmentProgram : D3DGpuProgram
	{
		#region Fields

		/// <summary>
		///    Reference to the current D3D PixelShader object.
		/// </summary>
		protected D3D.PixelShader pixelShader;

		#endregion Fields

		#region Construction and Destruction

		internal D3DFragmentProgram( ResourceManager parent, string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader, D3D.Device device )
			: base( parent, name, handle, group, isManual, loader, device )
		{
			type = GpuProgramType.Fragment;
		}

		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					if ( pixelShader != null && !pixelShader.Disposed )
						pixelShader.Dispose();
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
			}

			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );
		}

		#endregion Construction and Destruction

		#region D3DGpuProgram Memebers

		protected override void LoadFromMicrocode( D3D.ShaderBytecode microcode )
		{
			// create a new pixel shader
			pixelShader = new D3D.PixelShader( device, microcode );
		}

		#endregion D3DGpuProgram Memebers

		#region GpuProgram Members

		/// <summary>
		///     Unloads the PixelShader object.
		/// </summary>
		protected override void unload()
		{
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
		internal D3D.PixelShader PixelShader
		{
			get
			{
				return pixelShader;
			}
		}

		public override int SamplerCount
		{
			get
			{
				//throw new AxiomException( "Attempted to query sample count for D3D Fragment Program." );
				return 1;
			}
		}

		#endregion Properties
	}
}