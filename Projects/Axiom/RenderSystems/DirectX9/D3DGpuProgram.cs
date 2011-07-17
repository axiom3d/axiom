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
using System.Collections.Generic;
using System.Diagnostics;

using Axiom.Core;
using Axiom.Math;
using Axiom.Graphics;
using Axiom.Utilities;
using SlimDX.Direct3D9;
using ResourceHandle = System.UInt64;

using DX = SlimDX;
using D3D = SlimDX.Direct3D9;
using ResourceManager = Axiom.Core.ResourceManager;

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
		///     Microsode set externally, most likely from the HLSL compiler.
		/// </summary>
		protected D3D.ShaderBytecode externalMicrocode;

        #region ColumnMajorMatrices

        [OgreVersion(1, 7, 2790)]
        public bool ColumnMajorMatrices
        {
            get;
            set;
        }

        #endregion

        #endregion Fields

        #region Construction and Destruction

        protected D3DGpuProgram( ResourceManager parent, string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader, D3D.Device device )
			: base( parent, name, handle, group, isManual, loader )
		{
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

        #region loadImpl

        /// <summary>
		///     Overridden to allow for loading microcode from external sources.
		/// </summary>
        [OgreVersion(1, 7, 2790)]
		protected override void load()
		{
            foreach (var dev in D3DRenderSystem.ResourceCreationDevices)
                LoadImpl( dev );
		}

        #endregion

        #region nonvirt LoadImpl

        /// <summary>
        /// Loads this program to specified device
        /// </summary>
        [OgreVersion(1, 7, 2790)]
        protected void LoadImpl(Device d3D9Device)
	    {
            if (externalMicrocode != null)
            {
                LoadFromMicrocode(d3D9Device, externalMicrocode);
            }
            else
            {
                // Normal load-from-source approach
			    if (LoadFromFile)
			    {
				    // find & load source code
                    var stream = 
					    ResourceGroupManager.Instance.OpenResource(
					    fileName, _group, true, this);
				    source = stream.AsString();
			    }

			    // Call polymorphic load
			    LoadFromSource(d3D9Device);
            }
	    }

        #endregion

        #region unload

        [OgreVersion(1, 7, 2790)]
        protected override void unload()
        {
            if (externalMicrocode != null)
                externalMicrocode.Dispose();
            externalMicrocode = null;
        }

        #endregion

        #region LoadFromSource

        [OgreVersion(1, 7, 2790)]
		protected override void LoadFromSource()
		{
		    LoadFromSource( null );
		}

        [OgreVersion(1, 7, 2790)]
        protected void LoadFromSource(Device d3D9Device)
        {
            /*
            if (GpuProgramManager.Instance.IsMicrocodeAvailableInCache(_name))
            {
                GetMicrocodeFromCache(d3d9Device);
            }
            else*/
            {
                CompileMicrocode( d3D9Device );
            }
        }

        #endregion

        #region CompileMicrocode

        [OgreVersion(1, 7, 2790)]
        protected void CompileMicrocode(Device d3D9Device)
        {
            string errors;

            // load the shader from the source string
            var microcode = ShaderBytecode.Assemble( Source, null, null, 0, out errors );

            if ( !string.IsNullOrEmpty( errors ) )
            {
                throw new AxiomException( "Error while compiling pixel shader '{0}':\n {1}", Name, errors );
            }

            /*
            if ( GpuProgramManager.Instance.SaveMicrocodesToCache )
			{
		        // create microcode
		        GpuProgramManager.Microcode newMicrocode = 
                    GpuProgramManager.Instance.CreateMicrocode(microcode.GetBufferSize());

        		// save microcode
				memcpy(newMicrocode->getPtr(), microcode->GetBufferPointer(), microcode->GetBufferSize());

				// add to the microcode to the cache
				GpuProgramManager.Instance.AddMicrocodeToCache(_name, newMicrocode);
			}*/

            // load the code into a shader object (polymorphic)
            LoadFromMicrocode(d3D9Device, microcode);

            microcode.Dispose();
        }

	    #endregion

        #region LoadFromMicrocode

        [OgreVersion(1, 7, 2790)]
        protected abstract void LoadFromMicrocode(Device d3D9Device, ShaderBytecode microcode);

        #endregion

        #region CreateParameters

        [OgreVersion(1, 7, 2790)]
        public override GpuProgramParameters CreateParameters()
        {
            // Call superclass
            var parms = base.CreateParameters();

            // Need to transpose matrices if compiled with column-major matrices
            parms.TransposeMatrices = ColumnMajorMatrices;

            return parms;
        }

        #endregion

        #endregion GpuProgram Members

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

        #region mapDeviceToVertexShader

        [OgreVersion(1, 7, 2790)]
	    protected readonly Dictionary<Device, VertexShader> mapDeviceToVertexShader = new Dictionary<Device, VertexShader>();

        #endregion

        #endregion Fields

        #region Construction and Destruction

        internal D3DVertexProgram( ResourceManager parent, string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader, D3D.Device device )
			: base( parent, name, handle, group, isManual, loader, device )
		{
			Type = GpuProgramType.Vertex;
		}

		protected override void dispose( bool disposeManagedResources )
		{
		    unload();
		}

		#endregion Construction and Destruction

        #region LoadFromMicrocode

        [OgreVersion(1, 7, 2790)]
        protected override void LoadFromMicrocode(Device d3D9Device, ShaderBytecode microcode)
        {
            if ( d3D9Device == null )
            {
                foreach ( var curD3D9Device in D3DRenderSystem.ResourceCreationDevices )
                {
                    LoadFromMicrocode( curD3D9Device, microcode );
                }
            }


            VertexShader it;
            if (mapDeviceToVertexShader.TryGetValue(d3D9Device, out it))
            {
                if (it != null)
                    it.Dispose();
            }

            if ( IsSupported )
            {
                // Create the shader
                var vertexShader = new VertexShader(d3D9Device, microcode);
                mapDeviceToVertexShader[ d3D9Device ] = vertexShader;
            }
            else
            {
                LogManager.Instance.Write( "Unsupported D3D9 vertex shader '" + _name + "' was not loaded." );
                mapDeviceToVertexShader[ d3D9Device ] = null;
            }
        }

        #endregion

        #region unload

        [OgreVersion(1, 7, 2790)]
		protected override void unload()
		{
            foreach ( var it in mapDeviceToVertexShader )
            {
                if (it.Value != null)
                    it.Value.Dispose();
            }
            mapDeviceToVertexShader.Clear();

            base.unload();
		}

        #endregion


        #region Properties

        #region VertexShader

        /// <summary>
		///    Used internally by the D3DRenderSystem to get a reference to the underlying
		///    VertexShader object.
		/// </summary>
		internal VertexShader VertexShader
		{
			get
			{
                var d3D9Device = D3DRenderSystem.ActiveD3D9Device;
                VertexShader it;

                // Find the shader of this device.
                if (!mapDeviceToVertexShader.TryGetValue(d3D9Device, out it))
                {
                    // Shader was not found -> load it.
                    LoadImpl(d3D9Device);
                    it = mapDeviceToVertexShader[d3D9Device];
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
	}

	/// <summary>
	///    Direct3D implementation of low-level vertex programs.
	/// </summary>
	public class D3DFragmentProgram : D3DGpuProgram
	{
		#region Fields

        #region mapDeviceToPixelShader

        [OgreVersion(1, 7, 2790)]
        protected readonly Dictionary<Device, PixelShader> mapDeviceToPixelShader = new Dictionary<Device, PixelShader>();

        #endregion


		#endregion Fields

		#region Construction and Destruction

		internal D3DFragmentProgram( ResourceManager parent, string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader, D3D.Device device )
			: base( parent, name, handle, group, isManual, loader, device )
		{
			Type = GpuProgramType.Fragment;
		}

		protected override void dispose( bool disposeManagedResources )
		{
            // have to call this here reather than in Resource destructor
            // since calling virtual methods in base destructors causes crash
            unload(); 
		}

		#endregion Construction and Destruction


        #region LoadFromMicrocode

        [OgreVersion(1, 7, 2790)]
        protected override void LoadFromMicrocode(Device d3D9Device, ShaderBytecode microcode)
        {
            if (d3D9Device == null)
            {
                foreach (var curD3D9Device in D3DRenderSystem.ResourceCreationDevices)
                {
                    LoadFromMicrocode(curD3D9Device, microcode);
                }
            }


            PixelShader it;
            if (mapDeviceToPixelShader.TryGetValue(d3D9Device, out it))
            {
                if (it != null)
                    it.Dispose();
            }

            if (IsSupported)
            {
                // Create the shader
                var vertexShader = new PixelShader(d3D9Device, microcode);
                mapDeviceToPixelShader[d3D9Device] = vertexShader;
            }
            else
            {
                LogManager.Instance.Write("Unsupported D3D9 pixel shader '" + _name + "' was not loaded.");
                mapDeviceToPixelShader[d3D9Device] = null;
            }
        }

        #endregion

        #region unload

        [OgreVersion(1, 7, 2790)]
        protected override void unload()
        {
            foreach (var it in mapDeviceToPixelShader)
            {
                if (it.Value != null)
                    it.Value.Dispose();
            }
            mapDeviceToPixelShader.Clear();

            base.unload();
        }

        #endregion


		#region Properties

        #region PixelShader

        /// <summary>
        ///    Used internally by the D3DRenderSystem to get a reference to the underlying
        ///    VertexShader object.
        /// </summary>
        internal PixelShader PixelShader
        {
            get
            {
                var d3D9Device = D3DRenderSystem.ActiveD3D9Device;
                PixelShader it;

                // Find the shader of this device.
                if (!mapDeviceToPixelShader.TryGetValue(d3D9Device, out it))
                {
                    // Shader was not found -> load it.
                    LoadImpl(d3D9Device);
                    it = mapDeviceToPixelShader[d3D9Device];
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
	}
}