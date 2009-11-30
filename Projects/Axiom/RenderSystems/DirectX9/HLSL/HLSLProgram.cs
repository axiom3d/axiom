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
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Diagnostics;
using System.Collections.Generic;
using Axiom.Core;
using Axiom.Graphics;
using ResourceHandle = System.UInt64;
using DX = SlimDX;
using D3D = SlimDX.Direct3D9;

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
        ///     Entry point to compile from the program.
        /// </summary>
        protected string entry;
        /// <summary>
        ///     Holds the low level program instructions after the compile.
        /// </summary>
        protected D3D.ShaderBytecode microcode;
        /// <summary>
        ///     Holds information about shader constants.
        /// </summary>
        protected D3D.ConstantTable constantTable;
        /// <summary>
        /// preprocessor defines used to compile the program.
        /// </summary>
        protected string preprocessorDefines;
        #endregion Fields

        #region Construction and Destruction

        public HLSLProgram( ResourceManager parent, string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader )
            : base( parent, name, handle, group, isManual, loader )
        {
            preprocessorDefines = string.Empty;
        }

        protected override void dispose( bool disposeManagedResources )
        {
            if ( !isDisposed )
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
            // Populate preprocessor defines
            string stringBuffer = string.Empty;
            List<D3D.Macro> defines = new List<D3D.Macro>();
            if ( preprocessorDefines != string.Empty )
            {
                stringBuffer = preprocessorDefines;

                // Split preprocessor defines and build up macro array

                if ( stringBuffer.Contains( "," ) )
                {
                    string[] definesArr = stringBuffer.Split( ',' );
                    foreach ( string def in definesArr )
                    {
                        D3D.Macro macro = new D3D.Macro();
                        macro.Definition = "1\0";
                        macro.Name = def + "\0";
                        defines.Add( macro );
                    }
                }
            }

            string errors = null;

            // compile the high level shader to low level microcode
            // note, we need to pack matrices in row-major format for HLSL
            HLSLIncludeHandler include = new HLSLIncludeHandler( this );
            D3D.EffectCompiler effectCompiler = new D3D.EffectCompiler( source, defines.ToArray(), include, D3D.ShaderFlags.PackMatrixColumnMajor );

            try
            {
                microcode = effectCompiler.CompileShader( new D3D.EffectHandle( entry ),
                                                          target,
                                                          D3D.ShaderFlags.None,
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

        #endregion Methods

        #region Properties

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
