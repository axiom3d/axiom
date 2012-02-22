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
using System.Collections.Generic;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Scripting;
using D3D9 = SlimDX.Direct3D9;
using ResourceHandle = System.UInt64;

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
        protected D3D9.ShaderBytecode microcode;

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

        protected readonly GpuProgramParameters.GpuConstantDefinitionMap parametersMap = new GpuProgramParameters.GpuConstantDefinitionMap();

        //protected int parametersMapSizeAsBuffer;

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
            columnMajorMatrices = true;
        }

        protected override void dispose( bool disposeManagedResources )
        {
            if ( !IsDisposed )
            {
                if ( disposeManagedResources )
                {
                    if ( microcode != null && !microcode.Disposed )
                        microcode.Dispose();
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

        #region CreateLowLevelImpl

        /// <summary>
        ///     Creates a low level implementation based on the results of the
        ///     high level shader compilation.
        /// </summary>
        [OgreVersion( 1, 7, 2790 )]
        protected override void CreateLowLevelImpl()
        {
            if ( !HasCompileError )
            {
                // create a new program, without source since we are setting the microcode manually
                assemblerProgram = GpuProgramManager.Instance.CreateProgramFromString(
                    Name,
                    Group,
                    "",// dummy source, since we'll be using microcode
                    Type,
                    target
                    );

                // set the microcode for this program
                ( (D3D9GpuProgram)assemblerProgram ).ExternalMicrocode = microcode;
            }
        }

        #endregion

        #region BuildConstantDefinitions

        [OgreVersion( 1, 7, 2790 )]
        protected override void BuildConstantDefinitions()
        {
            constantDefs.FloatBufferSize = floatLogicalToPhysical.BufferSize;
            constantDefs.IntBufferSize = intLogicalToPhysical.BufferSize;

            foreach ( var iter in parametersMap )
            {
                var paramName = iter.Key;
                var def = iter.Value;
                constantDefs.Map.Add( iter.Key, iter.Value );

                // Record logical / physical mapping
                if ( def.IsFloat )
                {
                    lock ( floatLogicalToPhysical.Mutex )
                    {
                        if ( !floatLogicalToPhysical.Map.ContainsKey( def.LogicalIndex ) )
                            floatLogicalToPhysical.Map.Add( def.LogicalIndex,
                                                            new GpuProgramParameters.GpuLogicalIndexUse(
                                                                def.PhysicalIndex,
                                                                def.ArraySize * def.ElementSize,
                                                                GpuProgramParameters.GpuParamVariability.Global ) );
                        floatLogicalToPhysical.BufferSize += def.ArraySize * def.ElementSize;
                    }
                }
                else
                {
                    lock ( intLogicalToPhysical.Mutex )
                    {
                        if ( !intLogicalToPhysical.Map.ContainsKey( def.LogicalIndex ) )
                            intLogicalToPhysical.Map.Add( def.LogicalIndex,
                                                          new GpuProgramParameters.GpuLogicalIndexUse(
                                                              def.PhysicalIndex,
                                                              def.ArraySize * def.ElementSize,
                                                              GpuProgramParameters.GpuParamVariability.Global ) );

                        intLogicalToPhysical.BufferSize += def.ArraySize * def.ElementSize;
                    }
                }

                // Deal with array indexing
                constantDefs.GenerateConstantDefinitionArrayEntries( paramName, def );
            }
        }

        #endregion

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

            parms.TransposeMatrices = true;

            return parms;
        }

        #region LoadFromSource

        /// <summary>
        ///     Compiles the high level shader source to low level microcode.
        /// </summary>
        [OgreVersion( 1, 7, 2790 )]
        protected override void LoadFromSource()
        {
            /*
            if (GpuProgramManager.Instance.IsMicrocodeAvailableInCache("D3D9_HLSL_" + _name))
            {
                GetMicrocodeFromCache();
            }
            else
             */
            {
                CompileMicrocode();
            }
        }

        #endregion

        private void CompileMicrocode()
        {
            D3D9.ConstantTable constantTable = null;
            string errors = null;
            var defines = buildDefines( preprocessorDefines );

            var compileFlags = D3D9.ShaderFlags.None;
            var parseFlags = D3D9.ShaderFlags.None;

            parseFlags |= columnMajorMatrices ? D3D9.ShaderFlags.PackMatrixColumnMajor : D3D9.ShaderFlags.PackMatrixRowMajor;

#if DEBUG
            compileFlags |= D3D9.ShaderFlags.Debug;
            parseFlags |= D3D9.ShaderFlags.Debug;
#endif
            switch ( optimizationLevel )
            {
                case OptimizationLevel.Default:
                    compileFlags |= D3D9.ShaderFlags.OptimizationLevel1;
                    parseFlags |= D3D9.ShaderFlags.OptimizationLevel1;
                    break;
                case OptimizationLevel.None:
                    compileFlags |= D3D9.ShaderFlags.SkipOptimization;
                    parseFlags |= D3D9.ShaderFlags.SkipOptimization;
                    break;
                case OptimizationLevel.LevelZero:
                    compileFlags |= D3D9.ShaderFlags.OptimizationLevel0;
                    parseFlags |= D3D9.ShaderFlags.OptimizationLevel0;
                    break;
                case OptimizationLevel.LevelOne:
                    compileFlags |= D3D9.ShaderFlags.OptimizationLevel1;
                    parseFlags |= D3D9.ShaderFlags.OptimizationLevel1;
                    break;
                case OptimizationLevel.LevelTwo:
                    compileFlags |= D3D9.ShaderFlags.OptimizationLevel2;
                    parseFlags |= D3D9.ShaderFlags.OptimizationLevel2;
                    break;
                case OptimizationLevel.LevelThree:
                    compileFlags |= D3D9.ShaderFlags.OptimizationLevel3;
                    parseFlags |= D3D9.ShaderFlags.OptimizationLevel3;
                    break;
            }

            // compile the high level shader to low level microcode
            // note, we need to pack matrices in row-major format for HLSL
            var effectCompiler = new D3D9.EffectCompiler( Source, defines.ToArray(), includeHandler, parseFlags );


            try
            {
                microcode = effectCompiler.CompileShader( new D3D9.EffectHandle( entry ),
                                                          target,
                                                          compileFlags,
                                                          out errors,
                                                          out constantTable );
            }
            catch ( D3D9.Direct3D9Exception ex )
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
                            LogManager.Instance.Write( "HLSL: Warnings while compiling high level shader {0}:\n{1}",
                                                       Name, errors );
                        }
                    }
                    else
                    {
                        throw new AxiomException( "HLSL: Unable to compile high level shader {0}:\n{1}", Name, errors );
                    }
                }


                // Get contents of the constant table
                var desc = constantTable.Description;
                CreateParameterMappingStructures( true );


                // Iterate over the constants
                for ( var i = 0; i < desc.Constants; ++i )
                {
                    // Recursively descend through the structure levels
                    ProcessParamElement( constantTable, null, "", i );
                }

                constantTable.Dispose();

                /*
                if ( GpuProgramManager.Instance.SaveMicrocodesToCache )
                {
                    AddMicrocodeToCache();
                }*/

                effectCompiler.Dispose();
            }

        }

        #region UnloadHighLevelImpl

        /// <summary>
        ///     Unloads data that is no longer needed.
        /// </summary>
        [OgreVersion( 1, 7, 2790 )]
        protected override void UnloadHighLevelImpl()
        {
            if ( microcode != null )
                microcode.Dispose();
            microcode = null;
        }

        #endregion

        /// <summary>
        /// Returns whether this program can be supported on the current renderer and hardware.
        /// </summary>
        public override bool IsSupported
        {
            get
            {
                if ( HasCompileError || !IsRequiredCapabilitiesSupported() )
                {
                    return false;
                }

                return GpuProgramManager.Instance.IsSyntaxSupported( target );
            }
        }

        #endregion GpuProgram Members

        #region Methods

        #region ProcessParamElement

        [OgreVersion( 1, 7, 2790 )]
        protected void ProcessParamElement( D3D9.ConstantTable constantTable, D3D9.EffectHandle parent, string prefix, int index )
        {
            var constant = constantTable.GetConstant( parent, index );

            // Since D3D HLSL doesn't deal with naming of array and struct parameters
            // automatically, we have to do it by hand
            var desc = constantTable.GetConstantDescription( constant );

            var paramName = desc.Name;

            // trim the odd '$' which appears at the start of the names in HLSL
            if ( paramName.StartsWith( "$" ) )
            {
                paramName = paramName.Remove( 0, 1 );
            }

            // Also trim the '[0]' suffix if it exists, we will add our own indexing later
            if ( paramName.EndsWith( "[0]" ) )
            {
                paramName.Remove( paramName.Length - 3 );
            }


            if ( desc.Class == D3D9.ParameterClass.Struct )
            {
                // work out a new prefix for the nextest members if its an array, we need the index
                prefix = prefix + paramName + ".";
                // Cascade into struct
                for ( var i = 0; i < desc.StructMembers; ++i )
                {
                    ProcessParamElement( constantTable, constant, prefix, i );
                }
            }
            else
            {
                // process params
                if ( desc.Type == D3D9.ParameterType.Float ||
                     desc.Type == D3D9.ParameterType.Int ||
                     desc.Type == D3D9.ParameterType.Bool )
                {

                    var paramIndex = desc.RegisterIndex;
                    var name = prefix + paramName;

                    var def = new GpuProgramParameters.GpuConstantDefinition();
                    def.LogicalIndex = paramIndex;
                    // populate type, array size & element size
                    PopulateDef( desc, def );
                    if ( def.IsFloat )
                    {
                        def.PhysicalIndex = floatLogicalToPhysical.BufferSize;
                        lock ( floatLogicalToPhysical.Mutex )
                        {
                            floatLogicalToPhysical.Map.Add( paramIndex,
                                                            new GpuProgramParameters.GpuLogicalIndexUse(
                                                                def.PhysicalIndex,
                                                                def.ArraySize * def.ElementSize,
                                                                GpuProgramParameters.GpuParamVariability.Global ) );


                            floatLogicalToPhysical.BufferSize += def.ArraySize * def.ElementSize;
                        }
                    }
                    else
                    {
                        def.PhysicalIndex = intLogicalToPhysical.BufferSize;
                        lock ( intLogicalToPhysical.Mutex )
                        {
                            intLogicalToPhysical.Map.Add( paramIndex,
                                                          new GpuProgramParameters.GpuLogicalIndexUse(
                                                              def.PhysicalIndex,
                                                              def.ArraySize * def.ElementSize,
                                                              GpuProgramParameters.GpuParamVariability.Global ) );
                            intLogicalToPhysical.BufferSize += def.ArraySize * def.ElementSize;
                        }
                    }

                    if ( !parametersMap.ContainsKey( paramName ) )
                    {
                        parametersMap.Add( paramName, def );
                        /*
                        parametersMapSizeAsBuffer += sizeof ( int );
                        parametersMapSizeAsBuffer += paramName.Length;
                        parametersMapSizeAsBuffer += Marshal.SizeOf( def );
                         */
                    }
                }
            }

        }

        #endregion

        #region PopulateDef

        [OgreVersion( 1, 7, 2790 )]
        protected void PopulateDef( D3D9.ConstantDescription d3DDesc, GpuProgramParameters.GpuConstantDefinition def )
        {
            def.ArraySize = d3DDesc.Elements;
            switch ( d3DDesc.Type )
            {
                case D3D9.ParameterType.Int:
                    switch ( d3DDesc.Columns )
                    {
                        case 1:
                            def.ConstantType = GpuProgramParameters.GpuConstantType.Int1;
                            break;
                        case 2:
                            def.ConstantType = GpuProgramParameters.GpuConstantType.Int2;
                            break;
                        case 3:
                            def.ConstantType = GpuProgramParameters.GpuConstantType.Int3;
                            break;
                        case 4:
                            def.ConstantType = GpuProgramParameters.GpuConstantType.Int4;
                            break;
                    } // columns
                    break;
                case D3D9.ParameterType.Float:
                    switch ( d3DDesc.Class )
                    {
                        case D3D9.ParameterClass.MatrixColumns:
                        case D3D9.ParameterClass.MatrixRows:
                            {
                                var firstDim = d3DDesc.RegisterCount / d3DDesc.Elements;
                                var secondDim = d3DDesc.Class == D3D9.ParameterClass.MatrixRows ? d3DDesc.Columns : d3DDesc.Rows;

                                switch ( firstDim )
                                {
                                    case 2:
                                        switch ( secondDim )
                                        {
                                            case 2:
                                                def.ConstantType = GpuProgramParameters.GpuConstantType.Matrix_2X2;
                                                def.ElementSize = 8; // HLSL always packs
                                                break;
                                            case 3:
                                                def.ConstantType = GpuProgramParameters.GpuConstantType.Matrix_2X3;
                                                def.ElementSize = 8; // HLSL always packs
                                                break;
                                            case 4:
                                                def.ConstantType = GpuProgramParameters.GpuConstantType.Matrix_2X4;
                                                def.ElementSize = 8;
                                                break;
                                        } // columns
                                        break;
                                    case 3:
                                        switch ( secondDim )
                                        {
                                            case 2:
                                                def.ConstantType = GpuProgramParameters.GpuConstantType.Matrix_3X2;
                                                def.ElementSize = 12; // HLSL always packs
                                                break;
                                            case 3:
                                                def.ConstantType = GpuProgramParameters.GpuConstantType.Matrix_3X3;
                                                def.ElementSize = 12; // HLSL always packs
                                                break;
                                            case 4:
                                                def.ConstantType = GpuProgramParameters.GpuConstantType.Matrix_3X4;
                                                def.ElementSize = 12;
                                                break;
                                        } // columns
                                        break;
                                    case 4:
                                        switch ( secondDim )
                                        {
                                            case 2:
                                                def.ConstantType = GpuProgramParameters.GpuConstantType.Matrix_4X2;
                                                def.ElementSize = 16; // HLSL always packs
                                                break;
                                            case 3:
                                                def.ConstantType = GpuProgramParameters.GpuConstantType.Matrix_4X3;
                                                def.ElementSize = 16; // HLSL always packs
                                                break;
                                            case 4:
                                                def.ConstantType = GpuProgramParameters.GpuConstantType.Matrix_4X4;
                                                def.ElementSize = 16;
                                                break;
                                        } // secondDim
                                        break;

                                } // firstDim
                            }
                            break;
                        case D3D9.ParameterClass.Scalar:
                        case D3D9.ParameterClass.Vector:
                            switch ( d3DDesc.Columns )
                            {
                                case 1:
                                    def.ConstantType = GpuProgramParameters.GpuConstantType.Float1;
                                    break;
                                case 2:
                                    def.ConstantType = GpuProgramParameters.GpuConstantType.Float2;
                                    break;
                                case 3:
                                    def.ConstantType = GpuProgramParameters.GpuConstantType.Float3;
                                    break;
                                case 4:
                                    def.ConstantType = GpuProgramParameters.GpuConstantType.Float4;
                                    break;
                            } // columns
                            break;
                    }
                    break;
                default:
                    // not mapping samplers, don't need to take the space 
                    break;
            };

            // D3D9 pads to 4 elements
            def.ElementSize = GpuProgramParameters.GpuConstantDefinition.GetElementSize( def.ConstantType, true );

        }

        #endregion

        private List<D3D9.Macro> buildDefines( string defines )
        {
            List<D3D9.Macro> definesList = new List<D3D9.Macro>();
            D3D9.Macro macro;
            string[] tmp = defines.Split( ' ', ',', ';' );
            foreach ( string define in tmp )
            {
                macro = new D3D9.Macro();
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

        #region Command Objects

        #region EntryPointCommand
        [OgreVersion( 1, 7, 2 )]
        [ScriptableProperty( "entry_point", "The entry point for the HLSL program." )]
        public class EntryPointCommand : IPropertyCommand
        {
            #region IPropertyCommand Members

            [OgreVersion( 1, 7, 2 )]
            public string Get( object target )
            {
                return ( (HLSLProgram)target ).entry;
            }

            [OgreVersion( 1, 7, 2 )]
            public void Set( object target, string val )
            {
                ( (HLSLProgram)target ).entry = val;
            }

            #endregion IPropertyCommand Members
        }
        #endregion EntryPointCommand

        #region TargetCommand
        [OgreVersion( 1, 7, 2 )]
        [ScriptableProperty( "target", "Name of the assembler target to compile down to." )]
        public class TargetCommand : IPropertyCommand
        {
            #region IPropertyCommand Members

            [OgreVersion( 1, 7, 2 )]
            public string Get( object target )
            {
                return ( (HLSLProgram)target ).Target;
            }

            [OgreVersion( 1, 7, 2 )]
            public void Set( object target, string val )
            {
                ( (HLSLProgram)target ).Target = val;
            }

            #endregion IPropertyCommand Members
        }
        #endregion TargetCommand

        #region PreProcessorDefinesCommand
        [OgreVersion( 1, 7, 2 )]
        [ScriptableProperty( "preprocessor_defines", "Preprocessor defines use to compile the program." )]
        public class PreProcessorDefinesCommand : IPropertyCommand
        {
            #region IPropertyCommand Members

            [OgreVersion( 1, 7, 2 )]
            public string Get( object target )
            {
                return ( (HLSLProgram)target ).preprocessorDefines;
            }

            [OgreVersion( 1, 7, 2 )]
            public void Set( object target, string val )
            {
                ( (HLSLProgram)target ).preprocessorDefines = val;
            }

            #endregion IPropertyCommand Members
        }
        #endregion PreProcessorDefinesCommand

        #region ColumnMajorMatricesCommand
        [OgreVersion( 1, 7, 2 )]
        [ScriptableProperty( "column_major_matrices", "Whether matrix packing in column-major order." )]
        public class ColumnMajorMatricesCommand : IPropertyCommand
        {
            #region IPropertyCommand Members

            [OgreVersion( 1, 7, 2 )]
            public string Get( object target )
            {
                return ( (HLSLProgram)target ).columnMajorMatrices.ToString();
            }

            [OgreVersion( 1, 7, 2 )]
            public void Set( object target, string val )
            {
                ( (HLSLProgram)target ).columnMajorMatrices = StringConverter.ParseBool( val );
            }

            #endregion IPropertyCommand Members
        }
        #endregion ColumnMajorMatricesCommand

        #region OptimisationCommand
        [OgreVersion( 1, 7, 2 )]
        [ScriptableProperty( "optimisation_level", "The optimisation level to use." )]
        public class OptimisationCommand : IPropertyCommand
        {
            #region IPropertyCommand Members

            [OgreVersion( 1, 7, 2 )]
            public string Get( object target )
            {
                OptimizationLevel level = ( (HLSLProgram)target ).OptimizationLevel;
                return ScriptEnumAttribute.GetScriptAttribute( (int)level, typeof( OptimizationLevel ) );
            }

            [OgreVersion( 1, 7, 2 )]
            public void Set( object target, string val )
            {
                ( (HLSLProgram)target ).OptimizationLevel = (OptimizationLevel)ScriptEnumAttribute.Lookup( val, typeof( OptimizationLevel ) );
            }

            #endregion IPropertyCommand Members
        }
        #endregion OptimisationCommand

        #region MicrocodeCommand
        [OgreVersion( 1, 7, 2 )]
        [ScriptableProperty( "micro_code", "the micro code." )]
        public class MicrocodeCommand : IPropertyCommand
        {
            #region IPropertyCommand Members

            [OgreVersion( 1, 7, 2 )]
            public string Get( object target )
            {
                var buffer = ( (HLSLProgram)target ).microcode;
                if ( buffer != null )
                {
                    //TODO
                    //char* str  =static_cast<Ogre::String::value_type*>(buffer->GetBufferPointer());
                    //size_t size=static_cast<size_t>(buffer->GetBufferSize());
                    //Ogre::String code;
                    //code.assign(str,size);
                    //return code;
                }

                return string.Empty;
            }

            [OgreVersion( 1, 7, 2 )]
            public void Set( object target, string val )
            {
                //nothing to do
            }

            #endregion IPropertyCommand Members
        }
        #endregion MicrocodeCommand

        #region AssemblerCodeCommand
        [OgreVersion( 1, 7, 2 )]
        [ScriptableProperty( "assemble_code", "the assemble code." )]
        public class AssemblerCodeCommand : IPropertyCommand
        {
            #region IPropertyCommand Members

            [OgreVersion( 1, 7, 2 )]
            public string Get( object target )
            {
                var buffer = ( (HLSLProgram)target ).microcode;
                if ( buffer != null )
                {
                    //TODO
                    //CONST DWORD* code =static_cast<CONST DWORD*>(buffer->GetBufferPointer());
                    //LPD3DXBUFFER pDisassembly=0;
                    //HRESULT hr=D3DXDisassembleShader(code,FALSE,"// assemble code from D3D9HLSLProgram\n",&pDisassembly);
                    //if(pDisassembly)
                    //{
                    //    char* str  =static_cast<Ogre::String::value_type*>(pDisassembly->GetBufferPointer());
                    //    size_t size=static_cast<size_t>(pDisassembly->GetBufferSize());
                    //    Ogre::String assemble_code;
                    //    assemble_code.assign(str,size);
                    //    pDisassembly->Release();
                    //    return assemble_code;
                    //}
                    //return String();
                }

                return string.Empty;
            }

            [OgreVersion( 1, 7, 2 )]
            public void Set( object target, string val )
            {
                //nothing to do
            }

            #endregion IPropertyCommand Members
        }
        #endregion AssemblerCodeCommand

        #endregion Command Objects
    };
}
