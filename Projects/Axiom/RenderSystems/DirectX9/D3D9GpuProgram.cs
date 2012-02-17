#region MIT/X11 License
//Copyright © 2003-2012 Axiom 3D Rendering Engine Project
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.
#endregion License

#region SVN Version Information
// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Scripting;
using Axiom.Utilities;
using D3D9 = SlimDX.Direct3D9;
using ResourceHandle = System.UInt64;
using ResourceManager = Axiom.Core.ResourceManager;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9
{
	/// <summary>
	/// Direct3D implementation of a few things common to low-level vertex and fragment programs
	/// </summary>
	public abstract class D3D9GpuProgram : GpuProgram, ID3D9Resource
	{
		#region Fields

		/// <summary>
		/// Microcode set externally, most likely from the HLSL compiler.
		/// </summary>
		protected D3D9.ShaderBytecode externalMicrocode;

		#endregion Fields

		#region Properties

		#region ColumnMajorMatrices

		[OgreVersion( 1, 7, 2790 )]
		public bool ColumnMajorMatrices
		{
			get;
			set;
		}

		#endregion ColumnMajorMatrices

		#region ExternalMicrocode

		/// <summary>
		/// Gets/Sets a prepared chunk of microcode to use during Load
		/// rather than loading from file or a string.
		/// </summary>
		/// <remarks>
		/// This is used by the HLSL compiler once it compiles down to low
		/// level microcode, which can then be loaded into a low level GPU
		/// program.
		/// </remarks>
		internal D3D9.ShaderBytecode ExternalMicrocode
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

		#endregion ExternalMicrocode

		#endregion Properties

		#region Construction and Destruction

		[OgreVersion( 1, 7, 2 )]
		protected D3D9GpuProgram( ResourceManager parent, string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader )
			: base( parent, name, handle, group, isManual, loader )
		{
		}

		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					externalMicrocode.SafeDispose();
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

		#region loadImpl

		/// <summary>
		/// Overridden to allow for loading microcode from external sources.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		protected override void load()
		{
			//Entering critical section
			this.LockDeviceAccess();

			foreach ( var dev in D3D9RenderSystem.ResourceCreationDevices )
				LoadImpl( dev );

			//Leaving critical section
			this.UnlockDeviceAccess();
		}

		#endregion loadImpl

		#region nonvirt LoadImpl

		/// <summary>
		/// Loads this program to specified device
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		protected void LoadImpl( D3D9.Device d3D9Device )
		{
			if ( externalMicrocode != null )
			{
				LoadFromMicrocode( d3D9Device, externalMicrocode );
			}
			else
			{
				// Normal load-from-source approach
				if ( LoadFromFile )
				{
					// find & load source code
					var stream = ResourceGroupManager.Instance.OpenResource( fileName, _group, true, this );
					source = stream.AsString();
				}

				// Call polymorphic load
				LoadFromSource( d3D9Device );
			}
		}

		#endregion nonvirt LoadImpl

		#region unload

		[OgreVersion( 1, 7, 2790 )]
		protected override void unload()
		{
			externalMicrocode.SafeDispose();
		}

		#endregion unload

		#region LoadFromSource

		[OgreVersion( 1, 7, 2790 )]
		protected override void LoadFromSource()
		{
			//Entering critical section
			this.LockDeviceAccess();

			foreach ( var dev in D3D9RenderSystem.ResourceCreationDevices )
				LoadFromSource( dev );

			//Leaving critical section
			this.UnlockDeviceAccess();
		}

		[OgreVersion( 1, 7, 2790 )]
		protected void LoadFromSource( D3D9.Device d3D9Device )
		{
			//Entering critical section
			this.LockDeviceAccess();

			string errors = string.Empty;
			D3D9.ShaderBytecode microcode = null;

			// Create the shader
			// Assemble source into microcode
			try
			{
				microcode = D3D9.ShaderBytecode.Assemble(
					Source,
					null,   // no #define support
					null,   // no #include support
					0,      // standard compile options
					out errors
					);
			}
			catch ( Exception e )
			{
				throw new AxiomException( "Cannot assemble D3D9 shader {0} Errors:\n{1}", e, Name, errors );
			}

			LoadFromMicrocode( d3D9Device, microcode );

			microcode.SafeDispose();

			//Leaving critical section
			this.UnlockDeviceAccess();
		}

		#endregion LoadFromSource

		#region LoadFromMicrocode

		[OgreVersion( 1, 7, 2790 )]
		protected abstract void LoadFromMicrocode( D3D9.Device d3D9Device, D3D9.ShaderBytecode microcode );

		#endregion LoadFromMicrocode

		#region CreateParameters

		[OgreVersion( 1, 7, 2790 )]
		public override GpuProgramParameters CreateParameters()
		{
			// Call superclass
			var parms = base.CreateParameters();

			// Need to transpose matrices if compiled with column-major matrices
			parms.TransposeMatrices = ColumnMajorMatrices;

			return parms;
		}

		#endregion CreateParameters

		#endregion GpuProgram Members

		#region Custom Parameters

		/// <summary>
		/// Command object for setting matrix packing in column-major order
		/// </summary>
		[ScriptableProperty( "column_major_matrices", "Whether matrix packing in column-major order." )]
		public class ColumnMajorMatricesCommand : IPropertyCommand
		{
			[OgreVersion( 1, 7, 2 )]
			public string Get( object target )
			{
				return ( ( (D3D9GpuProgram)target ).ColumnMajorMatrices ).ToString();
			}

			[OgreVersion( 1, 7, 2 )]
			public void Set( object target, string val )
			{
				( (D3D9GpuProgram)target ).ColumnMajorMatrices = bool.Parse( val );
			}
		};

		/// <summary>
		/// Command object for getting/setting external micro code (ShaderBytecode)
		/// </summary>
		[ScriptableProperty( "external_micro_code", "the cached external micro code data." )]
		public class ExternalMicrocodeCommand : IPropertyCommand
		{
			[OgreVersion( 1, 7, 2 )]
			public string Get( object target )
			{
				//nothing to do
				return string.Empty;
			}

			public void Set( object target, string val )
			{
				var program = (D3D9GpuProgram)target;

				//TODO
				//const void* buffer = val.data();
				//program->setExternalMicrocode( buffer, val.size() );
			}
		};

		#endregion Custom Parameters

		#region ID3D9Resource Members

		/// <see cref="ID3D9Resource.NotifyOnDeviceCreate"/>
		public virtual void NotifyOnDeviceCreate( D3D9.Device d3d9Device )
		{
		}

		/// <see cref="ID3D9Resource.NotifyOnDeviceDestroy"/>
		public virtual void NotifyOnDeviceDestroy( D3D9.Device d3d9Device )
		{
		}

		/// <see cref="ID3D9Resource.NotifyOnDeviceLost"/>
		public virtual void NotifyOnDeviceLost( D3D9.Device d3d9Device )
		{
		}

		/// <see cref="ID3D9Resource.NotifyOnDeviceReset"/>
		public virtual void NotifyOnDeviceReset( D3D9.Device d3d9Device )
		{
		}

		#endregion ID3D9Resource Members
	};

	/// <summary>
	/// Direct3D implementation of low-level vertex programs.
	/// </summary>
	public sealed class D3D9GpuVertexProgram : D3D9GpuProgram
	{
		#region Fields

		[OgreVersion( 1, 7, 2790 )]
		private readonly Dictionary<D3D9.Device, D3D9.VertexShader> _mapDeviceToVertexShader = new Dictionary<D3D9.Device, D3D9.VertexShader>();

		#endregion Fields

		#region Properties

		#region VertexShader

		/// <summary>
		/// Used internally by the D3DRenderSystem to get a reference to the underlying
		/// VertexShader object.
		/// </summary>
		internal D3D9.VertexShader VertexShader
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				var d3D9Device = D3D9RenderSystem.ActiveD3D9Device;
				D3D9.VertexShader it;

				// Find the shader of this device.
				if ( !_mapDeviceToVertexShader.TryGetValue( d3D9Device, out it ) )
				{
					// Shader was not found -> load it.
					LoadImpl( d3D9Device );
					it = _mapDeviceToVertexShader[ d3D9Device ];
				}

				return it;
			}
		}

		#endregion

		public override int SamplerCount
		{
			get
			{
				throw new AxiomException( "Attempted to query sample count for vertex shader." );
			}
		}

		#endregion Properties

		#region Construction and Destruction

		[OgreVersion( 1, 7, 2 )]
		internal D3D9GpuVertexProgram( ResourceManager parent, string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader )
			: base( parent, name, handle, group, isManual, loader )
		{
			Type = GpuProgramType.Vertex;
		}

		[OgreVersion( 1, 7, 2, "~D3D9GpuVertexProgram" )]
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !this.IsDisposed && disposeManagedResources )
			{
				// have to call this here rather than in Resource destructor
				// since calling virtual methods in base destructors causes crash
				unload();
			}
		}

		#endregion Construction and Destruction

		#region LoadFromMicrocode

		[OgreVersion( 1, 7, 2790 )]
		protected override void LoadFromMicrocode( D3D9.Device d3D9Device, D3D9.ShaderBytecode microcode )
		{
			D3D9.VertexShader vertexShader;
			var shaderWasFound = _mapDeviceToVertexShader.TryGetValue( d3D9Device, out vertexShader );
			if ( shaderWasFound )
				vertexShader.SafeDispose();

			if ( IsSupported )
			{
				// Create the shader
				vertexShader = new D3D9.VertexShader( d3D9Device, microcode );
			}
			else
			{
				LogManager.Instance.Write( "Unsupported D3D9 vertex shader '{0}' was not loaded.", _name );
				vertexShader = null;
			}

			if ( shaderWasFound )
				_mapDeviceToVertexShader[ d3D9Device ] = vertexShader;
			else
				_mapDeviceToVertexShader.Add( d3D9Device, vertexShader );
		}

		#endregion LoadFromMicrocode

		#region unload

		[OgreVersion( 1, 7, 2790 )]
		protected override void unload()
		{
			//Entering critical section
			this.LockDeviceAccess();

			foreach ( var it in _mapDeviceToVertexShader )
				it.SafeDispose();

			_mapDeviceToVertexShader.Clear();
			base.unload();

			//Leaving critical section
			this.UnlockDeviceAccess();
		}

		#endregion unload

		#region ID3D9Resource Members

		/// <see cref="ID3D9Resource.NotifyOnDeviceDestroy"/>
		[OgreVersion( 1, 7, 2 )]
		public override void NotifyOnDeviceDestroy( D3D9.Device d3d9Device )
		{
			//Entering critical section
			this.LockDeviceAccess();

			// Find the shader of this device.
			D3D9.VertexShader it;

			// Case shader found -> release it and erase from map.
			if ( _mapDeviceToVertexShader.TryGetValue( d3d9Device, out it ) )
			{
				it.SafeDispose();
				_mapDeviceToVertexShader.Remove( d3d9Device );
			}

			//Leaving critical section
			this.UnlockDeviceAccess();
		}

		#endregion ID3D9Resource Members
	};

	/// <summary>
	/// Direct3D implementation of low-level vertex programs.
	/// </summary>
	public sealed class D3D9GpuFragmentProgram : D3D9GpuProgram
	{
		#region Fields

		[OgreVersion( 1, 7, 2790 )]
		private readonly Dictionary<D3D9.Device, D3D9.PixelShader> _mapDeviceToPixelShader = new Dictionary<D3D9.Device, D3D9.PixelShader>();

		#endregion Fields

		#region Properties

		#region PixelShader

		/// <summary>
		/// Used internally by the D3DRenderSystem to get a reference to the underlying
		///  VertexShader object.
		/// </summary>
		internal D3D9.PixelShader PixelShader
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				var d3D9Device = D3D9RenderSystem.ActiveD3D9Device;
				D3D9.PixelShader it;

				// Find the shader of this device.
				if ( !_mapDeviceToPixelShader.TryGetValue( d3D9Device, out it ) )
				{
					// Shader was not found -> load it.
					LoadImpl( d3D9Device );
					it = _mapDeviceToPixelShader[ d3D9Device ];
				}

				return it;
			}
		}

		#endregion

		public override int SamplerCount
		{
			get
			{
				//throw new AxiomException( "Attempted to query sample count for D3D Fragment Program." );
				return 1;
			}
		}

		#endregion Properties

		#region Construction and Destruction

		[OgreVersion( 1, 7, 2 )]
		internal D3D9GpuFragmentProgram( ResourceManager parent, string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader )
			: base( parent, name, handle, group, isManual, loader )
		{
			Type = GpuProgramType.Fragment;
		}

		[OgreVersion( 1, 7, 2, "~D3D9GpuFragmentProgram" )]
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !this.IsDisposed && disposeManagedResources )
			{
				// have to call this here rather than in Resource destructor
				// since calling virtual methods in base destructors causes crash
				unload();
			}
		}

		#endregion Construction and Destruction

		#region LoadFromMicrocode

		[OgreVersion( 1, 7, 2790 )]
		protected override void LoadFromMicrocode( D3D9.Device d3D9Device, D3D9.ShaderBytecode microcode )
		{
			D3D9.PixelShader pixelShader;
			var shaderWasFound = _mapDeviceToPixelShader.TryGetValue( d3D9Device, out pixelShader );
			if ( shaderWasFound )
				pixelShader.SafeDispose();

			if ( IsSupported )
			{
				// Create the shader
				pixelShader = new D3D9.PixelShader( d3D9Device, microcode );
			}
			else
			{
				LogManager.Instance.Write( "Unsupported D3D9 pixel shader '{0}' was not loaded.", _name );
				pixelShader = null;
			}

			if ( shaderWasFound )
				_mapDeviceToPixelShader[ d3D9Device ] = pixelShader;
			else
				_mapDeviceToPixelShader.Add( d3D9Device, pixelShader );
		}

		#endregion LoadFromMicrocode

		#region unload

		[OgreVersion( 1, 7, 2790 )]
		protected override void unload()
		{
			//Entering critical section
			this.LockDeviceAccess();

			foreach ( var it in _mapDeviceToPixelShader )
				it.SafeDispose();

			_mapDeviceToPixelShader.Clear();
			base.unload();

			//Leaving critical section
			this.UnlockDeviceAccess();
		}

		#endregion unload

		#region ID3D9Resource Members

		/// <see cref="ID3D9Resource.NotifyOnDeviceDestroy"/>
		[OgreVersion( 1, 7, 2 )]
		public override void NotifyOnDeviceDestroy( D3D9.Device d3d9Device )
		{
			//Entering critical section
			this.LockDeviceAccess();

			// Find the shader of this device.
			D3D9.PixelShader it;

			// Case shader found -> release it and erase from map.
			if ( _mapDeviceToPixelShader.TryGetValue( d3d9Device, out it ) )
			{
				it.SafeDispose();
				_mapDeviceToPixelShader.Remove( d3d9Device );
			}

			//Leaving critical section
			this.UnlockDeviceAccess();
		}

		#endregion ID3D9Resource Members
	};
}