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
using Axiom.Graphics;
using Axiom.RenderSystems.Xna.Content;

using ResourceHandle = System.UInt64;
using XNA = Microsoft.Xna.Framework;
using XFG = Microsoft.Xna.Framework.Graphics;

using System.Collections.Generic;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.Xna.HLSL
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
		///     Entry point to compile from the program.
		/// </summary>
		protected string entry;

		/// <summary>
		/// preprocessor defines used to compile the program.
		/// </summary>
		protected string preprocessorDefines;

		/// <summary>
		///     Holds the low level program instructions after the compile.
		/// </summary>
#if !( XBOX || XBOX360 )
		protected XFG.CompiledShader microcode;
#endif
		protected HlslCompiledShader compiledShader;

		/// <summary>
		///     Holds information about shader constants.
		/// </summary>
		protected XFG.ShaderConstantTable constantTable;

#if !(XBOX || XBOX360 || SILVERLIGHT)
		private HLSLIncludeHandler _includeHandler = new HLSLIncludeHandler();
#endif

		#endregion Fields

		#region Constructor

		public HLSLProgram( ResourceManager parent, string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader )
			: base( parent, name, handle, group, isManual, loader )
		{
			preprocessorDefines = string.Empty;
		}

		#endregion Constructor

		#region GpuProgram Members

		/// <summary>
		///     Creates a low level implementation based on the results of the
		///     high level shader compilation.
		/// </summary>
		protected override void CreateLowLevelImpl()
		{
			// create a new program, without source since we are setting the microcode manually
			assemblerProgram = GpuProgramManager.Instance.CreateProgramFromString( Name, Group, "", type, target );

			// set the microcode for this program
#if !( XBOX || XBOX360 )
			if( Root.Instance.RenderSystem.ConfigOptions[ "Use Content Pipeline" ].Value != "Yes" )
			{
				( (XnaGpuProgram)assemblerProgram ).ShaderCode = microcode.GetShaderCode();
			}
			else
#endif
			{
				( (XnaGpuProgram)assemblerProgram ).ShaderCode = compiledShader.ShaderCode;
			}
		}

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
#if !(XBOX || XBOX360)
			// Populate preprocessor defines
			string stringBuffer = string.Empty;
			List<XFG.CompilerMacro> defines = new List<XFG.CompilerMacro>();
			if( preprocessorDefines != string.Empty )
			{
				stringBuffer = preprocessorDefines;

				// Split preprocessor defines and build up macro array

				if( stringBuffer.Contains( "," ) )
				{
					string[] definesArr = stringBuffer.Split( ',' );
					foreach( string def in definesArr )
					{
						XFG.CompilerMacro macro = new XFG.CompilerMacro();
						macro.Definition = "1\0";
						macro.Name = def + "\0";
						defines.Add( macro );
					}
				}
			}

			string errors = null;

			switch( type )
			{
				case GpuProgramType.Vertex:
					target = "vs_3_0";
					break;
				case GpuProgramType.Fragment:
					target = "ps_3_0";
					break;
			}

			// compile the high level shader to low level microcode
			// note, we need to pack matrices in row-major format for HLSL
			microcode = XFG.ShaderCompiler.CompileFromSource( source, defines.ToArray(), _includeHandler, XFG.CompilerOptions.PackMatrixRowMajor, entry, _convertTarget( target ), XNA.TargetPlatform.Windows );
			if( microcode.Success )
			{
				constantTable = new XFG.ShaderConstantTable( microcode.GetShaderCode() );
			}
			else
			{
				errors = microcode.ErrorsAndWarnings;
			}

			// check for errors
			if( !string.IsNullOrEmpty( errors ) )
			{
				throw new AxiomException( "HLSL: Unable to compile high level shader {0}:\n{1}", entry, errors );
			}
#else
			throw new AxiomException("HLSL: LoadFromSource not implemented on the 360");
#endif
		}

		protected override void LoadHighLevelImpl()
		{
			if( Root.Instance.RenderSystem.ConfigOptions[ "Use Content Pipeline" ].Value == "Yes" && !String.IsNullOrEmpty( fileName ) )
			{
				if( !isHighLevelLoaded )
				{
					//get the CompiledShader from ContentManager
					AxiomContentManager acm = new AxiomContentManager( (XnaRenderSystem)Root.Instance.RenderSystem, "" );
					HlslCompiledShaders compiledShaders = acm.Load<HlslCompiledShaders>( fileName );
					//find compiled shader with matching entry point
					for( int i = 0; i < compiledShaders.Count; ++i )
					{
						if( compiledShaders[ i ].EntryPoint == entry )
						{
							compiledShader = compiledShaders[ i ];
							break;
						}
					}
					this.constantTable = new XFG.ShaderConstantTable( compiledShader.ShaderCode );
					isHighLevelLoaded = true;
				}
			}
			else
			{
				base.LoadHighLevelImpl();
			}
		}

		/// <summary>
		///     Derives parameter names from the constant table.
		/// </summary>
		/// <param name="parms"></param>
		protected override void PopulateParameterNames( GpuProgramParameters parms )
		{
			Debug.Assert( constantTable != null );

			XFG.ShaderConstantTable desc = constantTable;

			// iterate over the constants
			for( int i = 0; i < constantTable.Constants.Count; i++ )
			{
				// Recursively descend through the structure levels
				// Since XNA has no nice 'leaf' method like Cg (sigh)
				ProcessParamElement( null, "", i, parms );
			}
		}

		/// <summary>
		///     Unloads data that is no longer needed.
		/// </summary>
		protected override void UnloadImpl()
		{
			constantTable = null;
		}

		public override bool IsSupported
		{
			get
			{
				// If skeletal animation is being done, we need support for UBYTE4
				if( this.IsSkeletalAnimationIncluded &&
				    !Root.Instance.RenderSystem.HardwareCapabilities.HasCapability( Capabilities.VertexFormatUByte4 ) )
				{
					return false;
				}

				return GpuProgramManager.Instance.IsSyntaxSupported( target );
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
		protected void ProcessParamElement( XFG.ShaderConstant parent, string prefix, int index, GpuProgramParameters parms )
		{
			XFG.ShaderConstant constant = constantTable.Constants[ index ];

			string paramName = constant.Name;

			// trim the odd '$' which appears at the start of the names in HLSL
			if( paramName.StartsWith( "$" ) )
			{
				paramName = paramName.Remove( 0, 1 );
			}

			// If it's an array, elements will be > 1
			for( int e = 0; e < constant.ElementCount; e++ )
			{
				if( constant.ParameterClass == XFG.EffectParameterClass.Struct )
				{
					// work out a new prefix for the nextest members
					// if its an array, we need the index
					if( constant.ElementCount > 1 )
					{
						prefix += string.Format( "{0}[{1}].", paramName, e );
					}
					else
					{
						prefix += ".";
					}

					// cascade into the struct members
					for( int i = 0; i < constant.StructureMemberCount; i++ )
					{
						ProcessParamElement( constant, prefix, i, parms );
					}
				}
				else
				{
					// process params
					if( constant.ParameterType == XFG.EffectParameterType.Single ||
					    constant.ParameterType == XFG.EffectParameterType.Int32 ||
					    constant.ParameterType == XFG.EffectParameterType.Bool )
					{
						int paramIndex = constant.RegisterIndex;
						string newName = prefix + paramName;

						// if this is an array, we need to appent the element index
						if( constant.ElementCount > 1 )
						{
							newName += string.Format( "[{0}]", e );
						}

						// map the named param to the index
						parms.MapParamNameToIndex( newName, paramIndex );
					}
				}
			}
		}

		private XFG.ShaderProfile _convertTarget( string target )
		{
			switch( target.ToLower() )
			{
				case "vs_1_1":
					return XFG.ShaderProfile.VS_1_1;
				case "vs_2_0":
					return XFG.ShaderProfile.VS_2_0;
				case "vs_2_a":
					return XFG.ShaderProfile.VS_2_A;
				case "vs_2_sw":
					return XFG.ShaderProfile.VS_2_SW;
				case "vs_3_0":
					return XFG.ShaderProfile.VS_3_0;
				case "ps_1_1":
					return XFG.ShaderProfile.PS_1_1;
				case "ps_1_2":
					return XFG.ShaderProfile.PS_1_2;
				case "ps_1_3":
					return XFG.ShaderProfile.PS_1_3;
				case "ps_1_4":
					return XFG.ShaderProfile.PS_1_4;
				case "ps_2_0":
					return XFG.ShaderProfile.PS_2_0;
				case "ps_2_a":
					return XFG.ShaderProfile.PS_2_A;
				case "ps_2_b":
					return XFG.ShaderProfile.PS_2_B;
				case "ps_2_sw":
					return XFG.ShaderProfile.PS_2_SW;
				case "ps_3_0":
					return XFG.ShaderProfile.PS_3_0;
				case "xps_3_0":
					return XFG.ShaderProfile.XPS_3_0;
				case "xvs_3_0":
					return XFG.ShaderProfile.XVS_3_0;
			}
			return XFG.ShaderProfile.Unknown; // This will cause the ShaderCompiler to barf.
		}

		#endregion Methods

		#region Properties

		public override int SamplerCount
		{
			get
			{
				switch( target )
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

		#endregion

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

			switch( name )
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
