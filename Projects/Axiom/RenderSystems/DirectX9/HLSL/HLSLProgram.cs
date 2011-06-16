#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2010 Axiom Project Team

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
using System.Collections.Generic;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Scripting;

using ResourceHandle = System.UInt64;
using DX = SlimDX;
using D3D = SlimDX.Direct3D9;
using ResourceManager = Axiom.Core.ResourceManager;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9.HLSL
{
	/// <summary>
	/// Summary description for HLSLProgram.
	/// </summary>
	public class HLSLProgram : HighLevelGpuProgram
	{
		#region Fields

		/// <summary>
		///     Shader profile to target for the compile (i.e. vs1.1, etc).
		/// </summary>
		protected string target;
		/// <summary>
		/// Gets/Sets the shader profile to target for the compile (i.e. vs1.1, etc).
		/// </summary>
		public string Target
		{
			get
			{
				return target;
			}
			set
			{
				target = value;
			}
		}

		/// <summary>
		///     Entry point to compile from the program.
		/// </summary>
		protected string entry;
		/// <summary>
		/// Gets/Sets the entry point to compile from the program.
		/// </summary>
		public string EntryPoint
		{
			get
			{
				return entry;
			}
			set
			{
				entry = value;
			}
		}

		/// <summary>
		///     Holds the low level program instructions after the compile.
		/// </summary>
		protected D3D.ShaderBytecode microcode;
		/// <summary>
		///     Holds information about shader constants.
		/// </summary>
		protected D3D.ConstantTable constantTable;

		/// <summary>
		/// the preprocessor definitions to use to compile the program
		/// </summary>
		protected string preprocessorDefines = string.Empty;
		/// <summary>
		/// Get/Sets the preprocessor definitions to use to compile the program
		/// </summary>
		public string PreprocessorDefinitions
		{
			get
			{
				return preprocessorDefines;
			}
			set
			{
				preprocessorDefines = value;
			}
		}

		/// <summary>
		/// the optimization level to use.
		/// </summary>
		protected OptimizationLevel optimizationLevel;
		/// <summary>
		/// Gets/Sets the optimization level to use.
		/// </summary>
		public OptimizationLevel OptimizationLevel
		{
			get
			{
				return optimizationLevel;
			}
			set
			{
				optimizationLevel = value;
			}
		}

		/// <summary>
		/// determines which packing order to use for matrices
		/// </summary>
		protected bool columnMajorMatrices;
		/// <summary>
		/// Gets/Sets which packing order to use for matrices
		/// </summary>
		public bool UseColumnMajorMatrices
		{
			get
			{
				return columnMajorMatrices;
			}
			set
			{
				columnMajorMatrices = value;
			}
		}

		/// <summary>
		/// Include handler to load additional files from <see cref="ResourceGroupManager"/>
		/// </summary>
		private HLSLIncludeHandler includeHandler;

		#endregion Fields

		#region Construction and Destruction

		/// <summary>
		/// Creates a new instance of <see cref="HLSLProgram"/>
		/// </summary>
		/// <param name="parent">the ResourceManager that owns this resource</param>
		/// <param name="name">Name of the program</param>
		/// <param name="handle">The resource id of the program</param>
		/// <param name="group">the resource group</param>
		/// <param name="isManual">is the program manually loaded?</param>
		/// <param name="loader">the loader responsible for this program</param>
		public HLSLProgram( ResourceManager parent, string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader )
			: base( parent, name, handle, group, isManual, loader )
		{
			includeHandler = new HLSLIncludeHandler( this );
		}

		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					if ( microcode != null && !microcode.Disposed )
						microcode.Dispose();
					if ( constantTable != null && !constantTable.Disposed )
						constantTable.Dispose();
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
		///     Creates a low level implementation based on the results of the
		///     high level shader compilation.
		/// </summary>
		protected override void CreateLowLevelImpl()
		{
			if ( !_compileError )
			{
				// create a new program, without source since we are setting the microcode manually
				assemblerProgram = GpuProgramManager.Instance.CreateProgramFromString( Name, Group, "", type, target );

				// set the microcode for this program
				( (D3DGpuProgram)assemblerProgram ).ExternalMicrocode = microcode;
			}
		}

	    protected override void BuildConstantDefinitions()
	    {
	        throw new NotImplementedException();
	    }

	    /// <summary>
		///    Creates a new parameters object compatible with this program definition.
		/// </summary>
		/// <remarks>
		///    Unlike low-level assembly programs, parameters objects are specific to the
		///    program and therefore must be created from it rather than by the 
		///    HighLevelGpuProgramManager. This method creates a new instance of a parameters
		///    object containing the definition of the parameters this program understands.
		/// </remarks>
		/// <returns>A new set of program parameters.</returns>
		public override GpuProgramParameters CreateParameters()
		{
			GpuProgramParameters parms = base.CreateParameters();

			//parms.TransposeMatrices = true;

			return parms;
		}

		/// <summary>
		///     Compiles the high level shader source to low level microcode.
		/// </summary>
		protected override void LoadFromSource()
		{
			string errors = null;
			List<D3D.Macro> defines = buildDefines( preprocessorDefines );

			D3D.ShaderFlags compileFlags = D3D.ShaderFlags.None;
			D3D.ShaderFlags parseFlags = D3D.ShaderFlags.None;

			parseFlags |= columnMajorMatrices ? D3D.ShaderFlags.PackMatrixColumnMajor : D3D.ShaderFlags.PackMatrixRowMajor;

#if DEBUG
			compileFlags |= D3D.ShaderFlags.Debug;
			parseFlags |= D3D.ShaderFlags.Debug;
#endif
			switch ( optimizationLevel )
			{
				case OptimizationLevel.Default:
					compileFlags |= D3D.ShaderFlags.OptimizationLevel1;
					parseFlags |= D3D.ShaderFlags.OptimizationLevel1;
					break;
				case OptimizationLevel.None:
					compileFlags |= D3D.ShaderFlags.SkipOptimization;
					parseFlags |= D3D.ShaderFlags.SkipOptimization;
					break;
				case OptimizationLevel.LevelZero:
					compileFlags |= D3D.ShaderFlags.OptimizationLevel0;
					parseFlags |= D3D.ShaderFlags.OptimizationLevel0;
					break;
				case OptimizationLevel.LevelOne:
					compileFlags |= D3D.ShaderFlags.OptimizationLevel1;
					parseFlags |= D3D.ShaderFlags.OptimizationLevel1;
					break;
				case OptimizationLevel.LevelTwo:
					compileFlags |= D3D.ShaderFlags.OptimizationLevel2;
					parseFlags |= D3D.ShaderFlags.OptimizationLevel2;
					break;
				case OptimizationLevel.LevelThree:
					compileFlags |= D3D.ShaderFlags.OptimizationLevel3;
					parseFlags |= D3D.ShaderFlags.OptimizationLevel3;
					break;
			}

			// compile the high level shader to low level microcode
			// note, we need to pack matrices in row-major format for HLSL
			D3D.EffectCompiler effectCompiler = new D3D.EffectCompiler( source, defines.ToArray(), includeHandler, parseFlags );

			try
			{
				microcode = effectCompiler.CompileShader( new D3D.EffectHandle( entry ),
														  target,
														  compileFlags,
														  out errors,
														  out constantTable );
			}
			catch ( D3D.Direct3D9Exception ex )
			{
				throw new AxiomException( "HLSL: Unable to compile high level shader {0}:\n{1}", ex, Name );
			}
			finally
			{
				// check for errors
				if ( !String.IsNullOrEmpty( errors ) )
				{
					if ( microcode != null )
					{
						if ( LogManager.Instance != null )
						{
							LogManager.Instance.Write( "HLSL: Warnings while compiling high level shader {0}:\n{1}", Name, errors );
						}
					}
					else
					{
						throw new AxiomException( "HLSL: Unable to compile high level shader {0}:\n{1}", Name, errors );
					}
				}
				effectCompiler.Dispose();
			}
		}

		/// <summary>
		///     Dervices parameter names from the constant table.
		/// </summary>
		/// <param name="parms"></param>
		protected override void PopulateParameterNames( GpuProgramParameters parms )
		{
			Debug.Assert( constantTable != null );

			D3D.ConstantTableDescription desc = constantTable.Description;

			// iterate over the constants
			for ( int i = 0; i < desc.Constants; i++ )
			{
				// Recursively descend through the structure levels
				// Since D3D9 has no nice 'leaf' method like Cg (sigh)
				ProcessParamElement( null, "", i, parms );
			}
		}

		/// <summary>
		///     Unloads data that is no longer needed.
		/// </summary>
		protected override void UnloadImpl()
		{
			if ( microcode != null )
			{
				microcode.Dispose();
				microcode = null;
			}
			if ( constantTable != null )
			{
				constantTable.Dispose();
				constantTable = null;
			}
		}

		/// <summary>
		/// Returns whether this program can be supported on the current renderer and hardware.
		/// </summary>
		public override bool IsSupported
		{
			get
			{
				if ( _compileError || !IsRequiredCapabilitiesSupported() )
				{
					return false;
				}

				return GpuProgramManager.Instance.IsSyntaxSupported( target );
			}
		}

		/// <summary>
		/// Returns the maximum number of samplers that this fragment program has access
		/// to, based on the fragment program profile it uses.
		/// </summary>
		public override int SamplerCount
		{
			get
			{
				switch ( target )
				{
					case "ps_1_1":
					case "ps_1_2":
					case "ps_1_3":
						return 4;
					case "ps_1_4":
						return 6;
					case "ps_2_0":
					case "ps_2_x":
					case "ps_3_0":
					case "ps_3_x":
						return 16;
					default:
						throw new AxiomException( "Attempted to query sample count for unknown shader profile({0}).", target );
				}

				// return 0;
			}
		}
		#endregion GpuProgram Members

		#region Methods

		/// <summary>
		///
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="prefix"></param>
		/// <param name="index"></param>
		/// <param name="parms"></param>
		protected void ProcessParamElement( D3D.EffectHandle parent, string prefix, int index, GpuProgramParameters parms )
		{
			D3D.EffectHandle constant = constantTable.GetConstant( parent, index );

			// Since D3D HLSL doesn't deal with naming of array and struct parameters
			// automatically, we have to do it by hand
			D3D.ConstantDescription desc = constantTable.GetConstantDescription( constant );

			string paramName = desc.Name;

			// trim the odd '$' which appears at the start of the names in HLSL
			if ( paramName.StartsWith( "$" ) )
			{
				paramName = paramName.Remove( 0, 1 );
			}

			// If it's an array, elements will be > 1
			for ( int e = 0; e < desc.Elements; e++ )
			{
				if ( desc.Class == D3D.ParameterClass.Struct )
				{
					// work out a new prefix for the nextest members
					// if its an array, we need the index
					if ( desc.Elements > 1 )
					{
						prefix += string.Format( "{0}[{1}].", paramName, e );
					}
					else
					{
						prefix += ".";
					}

					// cascade into the struct members
					for ( int i = 0; i < desc.StructMembers; i++ )
					{
						ProcessParamElement( constant, prefix, i, parms );
					}
				}
				else
				{
					// process params
					if ( desc.Type == D3D.ParameterType.Float ||
						desc.Type == D3D.ParameterType.Int ||
						desc.Type == D3D.ParameterType.Bool )
					{

						int paramIndex = desc.RegisterIndex;
						string newName = prefix + paramName;

						// if this is an array, we need to appent the element index
						if ( desc.Elements > 1 )
						{
							newName += string.Format( "[{0}]", e );
						}

						// map the named param to the index
						parms.MapParamNameToIndex( newName, paramIndex + e );
					}
				}
			}
		}

		private List<D3D.Macro> buildDefines( string defines )
		{
			List<D3D.Macro> definesList = new List<D3D.Macro>();
			D3D.Macro macro;
			string[] tmp = defines.Split( ' ', ',', ';' );
			foreach ( string define in tmp )
			{
				macro = new D3D.Macro();
				if ( define.Contains( "=" ) )
				{
					macro.Name = define.Split( '=' )[ 0 ];
					macro.Definition = define.Split( '=' )[ 1 ];
				}
				else
				{
					macro.Name = define;
					macro.Definition = "1";
				}
				definesList.Add( macro );
			}
			return definesList;
		}

		#endregion Methods

		#region IConfigurable Members

		/// <summary>
		///     Sets a param for this HLSL program.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="val"></param>
		/// <returns></returns>
		public override bool SetParam( string name, string val )
		{
			bool handled = true;

			switch ( name )
			{
				case "entry_point":
					entry = val;
					break;

				case "target":
					target = val.Split( ' ' )[ 0 ];
					break;

				case "preprocessor_defines":
					preprocessorDefines = val;
					break;

				case "column_major_matrices":
					columnMajorMatrices = StringConverter.ParseBool( val );
					break;

				case "optimisation_level":
				case "optimization_level":
					optimizationLevel = (OptimizationLevel)ScriptEnumAttribute.Lookup( val, typeof( OptimizationLevel ) );
					break;

				default:
					LogManager.Instance.Write( "HLSLProgram: Unrecognized parameter '{0}'", name );
					handled = false;
					break;
			}

			return handled;
		}

		#endregion IConfigurable Members
	}
}
