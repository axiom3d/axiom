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

#endregion Namespace Declarations

namespace Axiom.RenderSystems.DirectX9.HLSL
{
    /// <summary>
    /// Specialisation of HighLevelGpuProgram to provide support for D3D9 
    /// High-Level Shader Language (HLSL).
    /// </summary>
    /// <remarks>
    /// Note that the syntax of D3D9 HLSL is identical to nVidia's Cg language, therefore
    /// unless you know you will only ever be deploying on Direct3D, or you have some specific
    /// reason for not wanting to use the Cg plugin, I suggest you use Cg instead since that
    /// can produce programs for OpenGL too.
    /// </remarks>
    public class D3D9HLSLProgram : HighLevelGpuProgram
    {
        #region Properties and Fields

        protected D3D9.ConstantTable constTable;
        protected readonly GpuProgramParameters.GpuConstantDefinitionMap parametersMap = new GpuProgramParameters.GpuConstantDefinitionMap();

        /// <summary>
        /// Returns whether this program can be supported on the current renderer and hardware.
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public override bool IsSupported
        {
            get
            {
                if ( HasCompileError || !IsRequiredCapabilitiesSupported() )
                    return false;

                return GpuProgramManager.Instance.IsSyntaxSupported( this.Target );
            }
        }

        /// <summary>
        /// Gets/Sets the shader profile to target for the compile (i.e. vs1.1, etc).
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public string Target
        {
            get;
            set;
        }

        /// <see cref="Axiom.Graphics.GpuProgram.Language"/>
        [OgreVersion( 1, 7, 2 )]
        public override string Language
        {
            get
            {
                return "hlsl";
            }
        }

        /// <summary>
        /// Gets/Sets the entry point to compile from the program.
        /// </summary>
        public string EntryPoint
        {
            get;
            set;
        }

        /// <summary>
        /// Holds the low level program instructions after the compile.
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        public D3D9.ShaderBytecode MicroCode
        {
            get;
            protected set;
        }

        /// <summary>
        /// Get/Sets the preprocessor definitions to use to compile the program
        /// </summary>
        public string PreprocessorDefines
        {
            get;
            set;
        }

        /// <summary>
        /// Gets/Sets the optimization level to use.
        /// </summary>
        public OptimizationLevel OptimizationLevel
        {
            get;
            set;
        }

        /// <summary>
        /// Gets/Sets which packing order to use for matrices
        /// </summary>
        public bool UseColumnMajorMatrices
        {
            get;
            set;
        }

        #endregion Properties and Fields

        #region Construction and Destruction

        /// <summary>
        /// Creates a new instance of <see cref="D3D9HLSLProgram"/>
        /// </summary>
        /// <param name="parent">the ResourceManager that owns this resource</param>
        /// <param name="name">Name of the program</param>
        /// <param name="handle">The resource id of the program</param>
        /// <param name="group">the resource group</param>
        /// <param name="isManual">is the program manually loaded?</param>
        /// <param name="loader">the loader responsible for this program</param>
        [OgreVersion( 1, 7, 2 )]
        public D3D9HLSLProgram( ResourceManager parent, string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader )
            : base( parent, name, handle, group, isManual, loader )
        {
            this.UseColumnMajorMatrices = true;
        }

        [OgreVersion( 1, 7, 2, "~D3D9HLSLProgram" )]
        protected override void dispose( bool disposeManagedResources )
        {
            if ( !IsDisposed )
            {
                if ( disposeManagedResources )
                {
                    // have to call this here reather than in Resource destructor
                    // since calling virtual methods in base destructors causes crash
                    if ( this.IsLoaded )
                        unload();
                    else
                        UnloadHighLevel();
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
        /// Compiles the high level shader source to low level microcode.
        /// </summary>
        [OgreVersion( 1, 7, 2 )]
        protected override void LoadFromSource()
        {
            // Populate preprocessor defines
            var defines = new List<D3D9.Macro>();
            if ( !string.IsNullOrEmpty( this.PreprocessorDefines ) )
            {
                var tmp = this.PreprocessorDefines.Split( ' ', ',', ';' );
                foreach ( string define in tmp )
                {
                    var macro = new D3D9.Macro();
                    if ( define.Contains( "=" ) )
                    {
                        var split = define.Split( '=' );
                        macro.Name = split[ 0 ];
                        macro.Definition = split[ 1 ];
                    }
                    else
                    {
                        macro.Definition = "1";
                    }

                    if ( !string.IsNullOrEmpty( macro.Name ) )
                        defines.Add( macro );
                }
            }

            // Populate compile flags
            var compileFlags = this.UseColumnMajorMatrices ? D3D9.ShaderFlags.PackMatrixColumnMajor : D3D9.ShaderFlags.PackMatrixRowMajor;

#if DEBUG
            compileFlags |= D3D9.ShaderFlags.Debug;
#endif
            switch ( this.OptimizationLevel )
            {
                case OptimizationLevel.Default:
                    compileFlags |= D3D9.ShaderFlags.OptimizationLevel1;
                    break;

                case OptimizationLevel.None:
                    compileFlags |= D3D9.ShaderFlags.SkipOptimization;
                    break;

                case OptimizationLevel.LevelZero:
                    compileFlags |= D3D9.ShaderFlags.OptimizationLevel0;
                    break;

                case OptimizationLevel.LevelOne:
                    compileFlags |= D3D9.ShaderFlags.OptimizationLevel1;
                    break;

                case OptimizationLevel.LevelTwo:
                    compileFlags |= D3D9.ShaderFlags.OptimizationLevel2;
                    break;

                case OptimizationLevel.LevelThree:
                    compileFlags |= D3D9.ShaderFlags.OptimizationLevel3;
                    break;
            }

            var parseFlags = compileFlags;
            compileFlags ^= this.UseColumnMajorMatrices ? D3D9.ShaderFlags.PackMatrixColumnMajor : D3D9.ShaderFlags.PackMatrixRowMajor;

            // include handler
            var includeHandler = new HLSLIncludeHandler( this );

            // Compile & assemble into microcode
            var effectCompiler = new D3D9.EffectCompiler( Source, defines.ToArray(), includeHandler, parseFlags );

            string errors = null;

            try
            {
                this.MicroCode = effectCompiler.CompileShader( new D3D9.EffectHandle( this.EntryPoint ),
                                                          this.Target,
                                                          compileFlags,
                                                          out errors,
                                                          out constTable );
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
                    if ( this.MicroCode != null )
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

                effectCompiler.Dispose();
                includeHandler.Dispose();
            }
        }

        /// <summary>
        /// Internal method for creating an appropriate low-level program from this
        /// high-level program, must be implemented by subclasses.
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
                    this.Target
                    );

                // set the microcode for this program
                ( (D3D9GpuProgram)assemblerProgram ).ExternalMicrocode = this.MicroCode;
            }
        }

        /// <summary>
        /// Unloads data that is no longer needed.
        /// </summary>
        [OgreVersion( 1, 7, 2790 )]
        protected override void UnloadHighLevelImpl()
        {
            this.MicroCode.SafeDispose();
            this.MicroCode = null;

            constTable.SafeDispose();
            constTable = null;
        }

        [OgreVersion( 1, 7, 2790 )]
        protected override void BuildConstantDefinitions()
        {
            // Derive parameter names from const table
            Contract.RequiresNotNull( constTable, "Program not loaded!" );
            // Get contents of the constant table
            var desc = constTable.Description;

            CreateParameterMappingStructures( true );

            for ( var i = 0; i < desc.Constants; ++i )
            {
                // Recursively descend through the structure levels
                ProcessParamElement( null, string.Empty, i );
            }
        }

        /// <summary>
        /// Recursive utility method for buildParamNameMap
        /// </summary>
        [OgreVersion( 1, 7, 2790 )]
        protected void ProcessParamElement( D3D9.EffectHandle parent, string prefix, int index )
        {
            var constant = constTable.GetConstant( parent, index );

            // Since D3D HLSL doesn't deal with naming of array and struct parameters
            // automatically, we have to do it by hand
            var desc = constTable.GetConstantDescription( constant );

            var paramName = desc.Name;

            // trim the odd '$' which appears at the start of the names in HLSL
            if ( paramName.StartsWith( "$" ) )
                paramName = paramName.Remove( 0, 1 );

            // Also trim the '[0]' suffix if it exists, we will add our own indexing later
            if ( paramName.EndsWith( "[0]" ) )
                paramName.Remove( paramName.Length - 3 );


            if ( desc.Class == D3D9.ParameterClass.Struct )
            {
                // work out a new prefix for the nextest members if its an array, we need the index
                prefix += paramName + ".";
                // Cascade into struct
                for ( var i = 0; i < desc.StructMembers; ++i )
                    ProcessParamElement( constant, prefix, i );
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
                            constantDefs.FloatBufferSize = floatLogicalToPhysical.BufferSize;
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
                            constantDefs.IntBufferSize = intLogicalToPhysical.BufferSize;
                        }
                    }

                    //mConstantDefs->map.insert(GpuConstantDefinitionMap::value_type(name, def));
                    if ( !parametersMap.ContainsKey( paramName ) )
                        parametersMap.Add( paramName, def );

                    // Now deal with arrays
                    constantDefs.GenerateConstantDefinitionArrayEntries( name, def );
                }
            }
        }

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

        /// <summary>
        /// Creates a new parameters object compatible with this program definition.
        /// </summary>
        /// <remarks>
        /// Unlike low-level assembly programs, parameters objects are specific to the
        /// program and therefore must be created from it rather than by the 
        /// HighLevelGpuProgramManager. This method creates a new instance of a parameters
        /// object containing the definition of the parameters this program understands.
        /// </remarks>
        /// <returns>A new set of program parameters.</returns>
        [OgreVersion( 1, 7, 2 )]
        public override GpuProgramParameters CreateParameters()
        {
            // Call superclass
            var parms = base.CreateParameters();

            // Need to transpose matrices if compiled with column-major matrices
            parms.TransposeMatrices = this.UseColumnMajorMatrices;

            return parms;
        }

        #endregion GpuProgram Members

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
                return ( (D3D9HLSLProgram)target ).EntryPoint;
            }

            [OgreVersion( 1, 7, 2 )]
            public void Set( object target, string val )
            {
                ( (D3D9HLSLProgram)target ).EntryPoint = val;
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
                return ( (D3D9HLSLProgram)target ).Target;
            }

            [OgreVersion( 1, 7, 2 )]
            public void Set( object target, string val )
            {
                ( (D3D9HLSLProgram)target ).Target = val;
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
                return ( (D3D9HLSLProgram)target ).PreprocessorDefines;
            }

            [OgreVersion( 1, 7, 2 )]
            public void Set( object target, string val )
            {
                ( (D3D9HLSLProgram)target ).PreprocessorDefines = val;
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
                return ( (D3D9HLSLProgram)target ).UseColumnMajorMatrices.ToString();
            }

            [OgreVersion( 1, 7, 2 )]
            public void Set( object target, string val )
            {
                ( (D3D9HLSLProgram)target ).UseColumnMajorMatrices = StringConverter.ParseBool( val );
            }

            #endregion IPropertyCommand Members
        }
        #endregion ColumnMajorMatricesCommand

        #region OptimisationCommand
        [OgreVersion( 1, 7, 2 )]
        [ScriptableProperty( "optimisation_level" )]
        [ScriptableProperty( "optimization_level", "The optimisation level to use." )]
        public class OptimizationCommand : IPropertyCommand
        {
            #region IPropertyCommand Members

            [OgreVersion( 1, 7, 2 )]
            public string Get( object target )
            {
                OptimizationLevel level = ( (D3D9HLSLProgram)target ).OptimizationLevel;
                return ScriptEnumAttribute.GetScriptAttribute( (int)level, typeof( OptimizationLevel ) );
            }

            [OgreVersion( 1, 7, 2 )]
            public void Set( object target, string val )
            {
                ( (D3D9HLSLProgram)target ).OptimizationLevel = (OptimizationLevel)ScriptEnumAttribute.Lookup( val, typeof( OptimizationLevel ) );
            }

            #endregion IPropertyCommand Members
        }
        #endregion OptimizationCommand

        #region MicrocodeCommand
        [OgreVersion( 1, 7, 2 )]
        [ScriptableProperty( "micro_code", "the micro code." )]
        public class MicrocodeCommand : IPropertyCommand
        {
            #region IPropertyCommand Members

            [OgreVersion( 1, 7, 2 )]
            public string Get( object target )
            {
                var buffer = ( (D3D9HLSLProgram)target ).MicroCode;
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
                var buffer = ( (D3D9HLSLProgram)target ).MicroCode;
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
