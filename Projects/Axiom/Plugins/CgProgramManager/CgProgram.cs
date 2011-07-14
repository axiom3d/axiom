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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using Axiom.Core;
using Axiom.Graphics;
using Axiom.Utilities;
using Tao.Cg;

#endregion Namespace Declarations

namespace Axiom.CgPrograms
{
	/// <summary>
	/// 	Specialization of HighLevelGpuProgram to provide support for nVidia's Cg language.
	/// </summary>
	/// <remarks>
	///    Cg can be used to compile common, high-level, C-like code down to assembler
	///    language for both GL and Direct3D, for multiple graphics cards. You must
	///    supply a list of profiles which your program must support using
	///    SetProfiles() before the program is loaded in order for this to work. The
	///    program will then negotiate with the renderer to compile the appropriate program
	///    for the API and graphics card capabilities.
	/// </remarks>
	public class CgProgram : HighLevelGpuProgram
	{
		#region Fields

        protected string[] cgArguments = new string[0];

		/// <summary>
		///    Current Cg context id.
		/// </summary>
		protected IntPtr cgContext;
		/// <summary>
		///    Current Cg program id.
		/// </summary>
		protected IntPtr cgProgram;
		/// <summary>
		///    Entry point of the Cg program.
		/// </summary>
		protected string entry;
		/// <summary>
		///    List of requested profiles for this program.
		/// </summary>
		protected string[] profiles;
		/// <summary>
		///    Chosen profile for this program.
		/// </summary>
		protected string selectedProfile;
		protected int selectedCgProfile;

	    private readonly GpuProgramParameters.GpuConstantDefinitionMap parametersMap =
	        new GpuProgramParameters.GpuConstantDefinitionMap();

	    private string programString;

	    #endregion Fields

		#region Constructors

		/// <summary>
		///    Constructor.
		/// </summary>
		/// <param name="name">Name of this program.</param>
		/// <param name="type">Type of this program, vertex or fragment program.</param>
		/// <param name="language">HLSL language of this program.</param>
		/// <param name="context">CG context id.</param>
		public CgProgram( ResourceManager parent, string name, ulong handle, string group, bool isManual, IManualResourceLoader loader, IntPtr context )
			: base( parent, name, handle, group, isManual, loader )
		{
			cgContext = context;
		    selectedCgProfile = Cg.CG_PROFILE_UNKNOWN;
		}

		#endregion Constructors

		#region Methods

        #region SelectProfile

        /// <summary>
        /// Internal method which works out which profile to use for this program
        /// </summary>
        [OgreVersion(1, 7, 2790)]
		protected void SelectProfile()
		{
			selectedProfile = "";
			selectedCgProfile = Cg.CG_PROFILE_UNKNOWN;

            if ( profiles != null )
            {
                foreach ( var i in profiles )
                {
                    if ( GpuProgramManager.Instance.IsSyntaxSupported( i ) )
                    {
                        selectedProfile = i;
                        selectedCgProfile = Cg.cgGetProfile( selectedProfile );

                        CgHelper.CheckCgError( "Unable to find Cg profile enum for program " + Name, cgContext );

                        break;
                    }
                }
            }
		}

        #endregion

        #region BuildArgs

        [OgreVersion(1, 7, 2790)]
        protected void BuildArgs()
        {
            var args = new List<string>();
            if ( !string.IsNullOrEmpty(CompileArguments) )
                args.AddRange(CompileArguments.Split(' '));

            
            if ( selectedCgProfile == Cg.CG_PROFILE_VS_1_1 )
            {
                // Need the 'dcls' argument whenever we use this profile
                // otherwise compilation of the assembler will fail
                var dclsFound = args.Contains( "dcls" );
              
                if ( !dclsFound )
                {
                    args.Add( "-profileopts" );
                    args.Add( "dcls" );
                }
            }

            args.Add( null );
            cgArguments = args.ToArray();
        }

        #endregion

        #region LoadFromSource

        [OgreVersion(1, 7, 2790)]
        protected override void LoadFromSource()
		{
		    SelectProfile();
            /*
		    if ( GpuProgramManager.Instance.IsMicrocodeAvailableInCache( "CG_" + _name ) )
		    {
		        GetMicrocodeFromCache();
		    }
		    else*/
		    {
		        CompileMicrocode();
		    }
		}

        #endregion

        #region CreateLowLevelImpl

        [OgreVersion(1, 7, 2790)]
        protected override void CreateLowLevelImpl()
		{
		    // the hlsl 4 profiles are only supported in OGRE from CG 2.2

		    if ( false
		        /*Cg.CG_VERSION_NUM >= 2200 && 
                            (selectedCgProfile ==  Cg.CG_PROFILE_VS_4_0
                            || selectedCgProfile == Cg.CG_PROFILE_PS_4_0) */ )
		    {
		        // Create a high-level program, give it the same name as us
		        var vp =
		            HighLevelGpuProgramManager.Instance.CreateProgram(
		                _name, _group, "hlsl", type );
		        vp.Source = programString;

		        vp.SetParam("target", selectedProfile);
		        vp.SetParam("entry_point", "main");

		        vp.Load();

		        assemblerProgram = vp;
		    }
		    else
		    {
		        if ( type == GpuProgramType.Fragment )
		        {
		            //HACK : http://developer.nvidia.com/forums/index.php?showtopic=1063&pid=2378&mode=threaded&start=#entry2378
		            //Still happens in CG 2.2. Remove hack when fixed.
		            programString = programString.Replace( "oDepth.z", "oDepth" );
		        }
		        // Create a low-level program, give it the same name as us
		        assemblerProgram =
		            GpuProgramManager.Instance.CreateProgramFromString(
		                _name,
		                _group,
		                programString,
		                type,
		                selectedProfile );
		    }
		    // Shader params need to be forwarded to low level implementation
		    assemblerProgram.IsAdjacencyInfoRequired = IsAdjacencyInfoRequired;
		}

        #endregion

        #region RecurseParams

        [OgreVersion(1, 7, 2790)]
        protected void RecurseParams(IntPtr parameter, int contextArraySize = 1)
		{
			// loop through the rest of the params
			while ( parameter != IntPtr.Zero )
			{

				// get the type of this param up front
                var paramType = Cg.cgGetParameterType(parameter);

				// Look for uniform parameters only
				// Don't bother enumerating unused parameters, especially since they will
				// be optimized out and therefore not in the indexed versions
                if (Cg.cgIsParameterReferenced(parameter) != 0
                    && Cg.cgGetParameterVariability(parameter) == Cg.CG_UNIFORM
                    && Cg.cgGetParameterDirection(parameter) != Cg.CG_OUT
					&& paramType != Cg.CG_SAMPLER1D
					&& paramType != Cg.CG_SAMPLER2D
					&& paramType != Cg.CG_SAMPLER3D
					&& paramType != Cg.CG_SAMPLERCUBE
					&& paramType != Cg.CG_SAMPLERRECT )
                {

                    int arraySize;

                    switch ( paramType )
                    {
                        case Cg.CG_STRUCT:
                            RecurseParams( Cg.cgGetFirstStructParameter( parameter ) );
                            break;
                        case Cg.CG_ARRAY:
                            // Support only 1-dimensional arrays
                            arraySize = Cg.cgGetArraySize( parameter, 0 );
                            RecurseParams( Cg.cgGetArrayParameter( parameter, 0 ), arraySize );
                            break;
                        default:
                            // Normal path (leaf)
                            var paramName = Cg.cgGetParameterName( parameter );
                            var logicalIndex = Cg.cgGetParameterResourceIndex( parameter );

                            // Get the parameter resource, to calculate the physical index
                            var res = Cg.cgGetParameterResource( parameter );
                            var isRegisterCombiner = false;
                            var regCombinerPhysicalIndex = 0;
                            switch ( res )
                            {
                                case Cg.CG_COMBINER_STAGE_CONST0:
                                    // register combiner, const 0
                                    // the index relates to the texture stage; store this as (stage * 2) + 0
                                    regCombinerPhysicalIndex = logicalIndex*2;
                                    isRegisterCombiner = true;
                                    break;
                                case Cg.CG_COMBINER_STAGE_CONST1:
                                    // register combiner, const 1
                                    // the index relates to the texture stage; store this as (stage * 2) + 1
                                    regCombinerPhysicalIndex = ( logicalIndex*2 ) + 1;
                                    isRegisterCombiner = true;
                                    break;
                                default:
                                    // normal constant
                                    break;
                            }

                            // Trim the '[0]' suffix if it exists, we will add our own indexing later
                            if ( paramName.EndsWith( "[0]" ) )
                            {
                                paramName.Remove( paramName.Length - 3 );
                            }


                            var def = new GpuProgramParameters.GpuConstantDefinition();
                            def.ArraySize = contextArraySize;
                            MapTypeAndElementSize( paramType, isRegisterCombiner, def );

                            if ( def.ConstantType == GpuProgramParameters.GpuConstantType.Unknown)
                            {
                                LogManager.Instance.Write(
                                    "Problem parsing the following Cg Uniform: '"
                                    + paramName + "' in file " + _name );
                                // next uniform
                                parameter = Cg.cgGetNextParameter( parameter );
                                continue;
                            }
                            if ( isRegisterCombiner )
                            {
                                def.PhysicalIndex = regCombinerPhysicalIndex;
                            }
                            else
                            {
                                // base position on existing buffer contents
                                if ( def.IsFloat )
                                {
                                    def.PhysicalIndex = floatLogicalToPhysical.BufferSize;
                                }
                                else
                                {
                                    def.PhysicalIndex = intLogicalToPhysical.BufferSize;
                                }
                            }

                            def.LogicalIndex = logicalIndex;

                            if ( !parametersMap.ContainsKey( paramName ) )
                            {
                                parametersMap.Add( paramName, def);
                                /*
                                mParametersMapSizeAsBuffer += sizeof ( size_t );
                                mParametersMapSizeAsBuffer += paramName.size();
                                mParametersMapSizeAsBuffer += sizeof ( GpuConstantDefinition );
                                 */
                            }

                            // Record logical / physical mapping
                            if ( def.IsFloat )
                            {
                                lock(floatLogicalToPhysical.Mutex)
                                {
                                    floatLogicalToPhysical.Map.Add( def.LogicalIndex,
                                                                    new GpuProgramParameters.GpuLogicalIndexUse(
                                                                        def.PhysicalIndex, def.ArraySize*def.ElementSize,
                                                                        GpuProgramParameters.GpuParamVariability.Global ) );

                                    floatLogicalToPhysical.BufferSize += def.ArraySize*def.ElementSize;
                                }
                            }
                            else
                            {
                                lock( intLogicalToPhysical.Mutex )
                                {
                                    intLogicalToPhysical.Map.Add(def.LogicalIndex,
                                                                    new GpuProgramParameters.GpuLogicalIndexUse(
                                                                        def.PhysicalIndex, def.ArraySize * def.ElementSize,
                                                                        GpuProgramParameters.GpuParamVariability.Global));

                                    intLogicalToPhysical.BufferSize += def.ArraySize * def.ElementSize;
                                }
                            }

                            break;
                    }
                }

			    // get the next param
				parameter = Cg.cgGetNextLeafParameter( parameter );
			}
		}

	    #endregion

        #region MapTypeAndElementSize

        [OgreVersion(1, 7, 2790)]
        private void MapTypeAndElementSize(int cgType, bool isRegisterCombiner, GpuProgramParameters.GpuConstantDefinition def)
        {
            if ( isRegisterCombiner )
            {
                // register combiners are the only single-float entries in our buffer
                def.ConstantType = GpuProgramParameters.GpuConstantType.Float1;
                def.ElementSize = 1;
            }
            else
            {
                switch ( cgType )
                {
                    case Cg.CG_FLOAT:
                    case Cg.CG_FLOAT1:
                    case Cg.CG_HALF:
                    case Cg.CG_HALF1:
                        def.ConstantType = GpuProgramParameters.GpuConstantType.Float1;
                        break;
                    case Cg.CG_FLOAT2:
                    case Cg.CG_HALF2:
                        def.ConstantType = GpuProgramParameters.GpuConstantType.Float2;
                        break;
                    case Cg.CG_FLOAT3:
                    case Cg.CG_HALF3:
                        def.ConstantType = GpuProgramParameters.GpuConstantType.Float3;
                        break;
                    case Cg.CG_FLOAT4:
                    case Cg.CG_HALF4:
                        def.ConstantType = GpuProgramParameters.GpuConstantType.Float4;
                        break;
                    case Cg.CG_FLOAT2x2:
                    case Cg.CG_HALF2x2:
                        def.ConstantType = GpuProgramParameters.GpuConstantType.Matrix_2X2;
                        break;
                    case Cg.CG_FLOAT2x3:
                    case Cg.CG_HALF2x3:
                        def.ConstantType = GpuProgramParameters.GpuConstantType.Matrix_2X3;
                        break;
                    case Cg.CG_FLOAT2x4:
                    case Cg.CG_HALF2x4:
                        def.ConstantType = GpuProgramParameters.GpuConstantType.Matrix_2X4;
                        break;
                    case Cg.CG_FLOAT3x2:
                    case Cg.CG_HALF3x2:
                        def.ConstantType = GpuProgramParameters.GpuConstantType.Matrix_3X2;
                        break;
                    case Cg.CG_FLOAT3x3:
                    case Cg.CG_HALF3x3:
                        def.ConstantType = GpuProgramParameters.GpuConstantType.Matrix_3X3;
                        break;
                    case Cg.CG_FLOAT3x4:
                    case Cg.CG_HALF3x4:
                        def.ConstantType = GpuProgramParameters.GpuConstantType.Matrix_3X4;
                        break;
                    case Cg.CG_FLOAT4x2:
                    case Cg.CG_HALF4x2:
                        def.ConstantType = GpuProgramParameters.GpuConstantType.Matrix_4X2;
                        break;
                    case Cg.CG_FLOAT4x3:
                    case Cg.CG_HALF4x3:
                        def.ConstantType = GpuProgramParameters.GpuConstantType.Matrix_4X3;
                        break;
                    case Cg.CG_FLOAT4x4:
                    case Cg.CG_HALF4x4:
                        def.ConstantType = GpuProgramParameters.GpuConstantType.Matrix_4X4;
                        break;
                    case Cg.CG_INT:
                    case Cg.CG_INT1:
                        def.ConstantType = GpuProgramParameters.GpuConstantType.Int1;
                        break;
                    case Cg.CG_INT2:
                        def.ConstantType = GpuProgramParameters.GpuConstantType.Int2;
                        break;
                    case Cg.CG_INT3:
                        def.ConstantType = GpuProgramParameters.GpuConstantType.Int3;
                        break;
                    case Cg.CG_INT4:
                        def.ConstantType = GpuProgramParameters.GpuConstantType.Int4;
                        break;
                    default:
                        def.ConstantType = GpuProgramParameters.GpuConstantType.Unknown;
                        break;
                }
                // Cg pads
                def.ElementSize = GpuProgramParameters.GpuConstantDefinition.GetElementSize( def.ConstantType, true );
            }
        }

        #endregion

        #region CompileMicrocode

        [OgreVersion(1, 7, 2790)]
        protected void CompileMicrocode()
        {
            // Create Cg Program

            if ( selectedCgProfile == Cg.CG_PROFILE_UNKNOWN )
            {
                LogManager.Instance.Write(
                    "Attempted to load Cg program '" + _name + "', but no suported profile was found. " );
                return;
            }

            BuildArgs();
            // deal with includes
            String sourceToUse = ResolveCgIncludes( source, this, fileName );

            var cgProgram = Cg.cgCreateProgram( cgContext, Cg.CG_SOURCE, sourceToUse,
                                                selectedCgProfile, entry, cgArguments );

            // Test
            //LogManager::getSingleton().logMessage(cgGetProgramString(mCgProgram, CG_COMPILED_PROGRAM));

            // Check for errors
            CgHelper.CheckCgError( "Unable to compile Cg program " + _name + ": ", cgContext );

            var error = Cg.cgGetError();
            if ( error == Cg.CG_NO_ERROR )
            {
                // get program string (result of cg compile)
                programString = Cg.cgGetProgramString( cgProgram, Cg.CG_COMPILED_PROGRAM );

                // get params
                parametersMap.Clear();
                RecurseParams( Cg.cgGetFirstParameter( cgProgram, Cg.CG_PROGRAM ) );
                RecurseParams( Cg.cgGetFirstParameter( cgProgram, Cg.CG_GLOBAL ) );

                // Unload Cg Program - we don't need it anymore
                Cg.cgDestroyProgram( cgProgram );
                CgHelper.CheckCgError( "Error while unloading Cg program " + _name + ": ", cgContext );
                cgProgram = IntPtr.Zero;

                /*
                if ( GpuProgramManager.Instance.SaveMicrocodesToCache )
                {
                    AddMicrocodeToCache();
                }*/
            }
        }

	    #endregion

        #region BuildConstantDefinitions

        [OgreVersion(1, 7, 2790)]
	    protected override void BuildConstantDefinitions()
	    {
	        // Derive parameter names from Cg
	        CreateParameterMappingStructures( true );

	        if ( string.IsNullOrEmpty(programString) )
	            return;

	        constantDefs.FloatBufferSize = floatLogicalToPhysical.BufferSize;
	        constantDefs.IntBufferSize = intLogicalToPhysical.BufferSize;

	        foreach (var iter in parametersMap)
	        {
	            var paramName = iter.Key;
	            var def = iter.Value;

	            constantDefs.Map.Add( iter.Key, iter.Value );

	            // Record logical / physical mapping
	            if ( def.IsFloat )
	            {
                    lock (floatLogicalToPhysical.Mutex)
                    {
                        floatLogicalToPhysical.Map.Add( def.LogicalIndex,
                                                               new GpuProgramParameters.GpuLogicalIndexUse( def.PhysicalIndex,
                                                                                   def.ArraySize*def.ElementSize,
                                                                                   GpuProgramParameters.GpuParamVariability.Global) );
                        floatLogicalToPhysical.BufferSize += def.ArraySize*def.ElementSize;
                    }
	            }
	            else
	            {
	                lock (intLogicalToPhysical.Mutex)
	                {
	                    intLogicalToPhysical.Map.Add( def.LogicalIndex,
	                                                           new GpuProgramParameters.GpuLogicalIndexUse( def.PhysicalIndex,
	                                                                               def.ArraySize*def.ElementSize,
                                                                                   GpuProgramParameters.GpuParamVariability.Global));
	                    intLogicalToPhysical.BufferSize += def.ArraySize*def.ElementSize;
	                }
	            }

	            // Deal with array indexing
	            constantDefs.GenerateConstantDefinitionArrayEntries( paramName, def );
	        }
	    }

        #endregion

        #region UnloadHighLevelImpl

        [OgreVersion(1, 7, 2790)]
		protected override void UnloadHighLevelImpl()
		{
		}

        #endregion

        #region ResolveCgIncludes

        [OgreVersion(1, 7, 2790)]
        private string ResolveCgIncludes(string inSource, Resource resourceBeingLoaded, string fileName)
        {
            var outSource = "";
            var startMarker = 0;
            var i = inSource.IndexOf( "#include" );
            while ( i != -1 )
            {
                var includePos = i;
                var afterIncludePos = includePos + 8;
                var newLineBefore = inSource.LastIndexOf( "\n", 0, includePos );

                // check we're not in a comment
                var lineCommentIt = inSource.LastIndexOf( "//", 0, includePos );
                if ( lineCommentIt != -1 )
                {
                    if ( newLineBefore == -1 || lineCommentIt > newLineBefore )
                    {
                        // commented
                        i = inSource.IndexOf( "#include", afterIncludePos );
                        continue;
                    }

                }

                var blockCommentIt = inSource.LastIndexOf( "/*", 0, includePos );
                if ( blockCommentIt != -1 )
                {
                    var closeCommentIt = inSource.LastIndexOf( "*/", 0, includePos );
                    if ( closeCommentIt == -1 || closeCommentIt < blockCommentIt )
                    {
                        // commented
                        i = inSource.IndexOf( "#include", afterIncludePos );
                        continue;
                    }

                }

                // find following newline (or EOF)
                var newLineAfter = inSource.IndexOf( "\n", afterIncludePos );
                // find include file string container
                var endDelimeter = "\"";
                var startIt = inSource.IndexOf( "\"", afterIncludePos );
                if ( startIt == -1 || startIt > newLineAfter )
                {
                    // try <>
                    startIt = inSource.IndexOf( "<", afterIncludePos );
                    if ( startIt == -1 || startIt > newLineAfter )
                    {
                        throw new AxiomException( "Badly formed #include directive (expected \" or <) in file "
                                                  + fileName + ": " +
                                                  inSource.Substring( includePos, newLineAfter - includePos ) );
                    }
                    else
                    {
                        endDelimeter = ">";
                    }
                }
                var endIt = inSource.IndexOf( endDelimeter, startIt + 1 );
                if ( endIt == -1 || endIt <= startIt )
                {
                    throw new AxiomException( "Badly formed #include directive (expected " + endDelimeter + ") in file "
                                              + fileName + ": " +
                                              inSource.Substring( includePos, newLineAfter - includePos ) );
                }

                // extract filename
                var filename = inSource.Substring( startIt + 1, endIt - startIt - 1 );

                // open included file
                var resource = ResourceGroupManager.Instance.OpenResource( filename, resourceBeingLoaded.Group, true,
                                                                           resourceBeingLoaded );

                // replace entire include directive line
                // copy up to just before include
                if ( newLineBefore != -1 && newLineBefore >= startMarker )
                    outSource += inSource.Substring( startMarker, newLineBefore - startMarker + 1 );

                var lineCount = 0;
                var lineCountPos = 0;

                // Count the line number of #include statement
                lineCountPos = outSource.IndexOf( '\n' );
                while ( lineCountPos != -1 )
                {
                    lineCountPos = outSource.IndexOf( '\n', lineCountPos + 1 );
                    lineCount++;
                }

                // Add #line to the start of the included file to correct the line count
                outSource += ( "#line 1 \"" + filename + "\"\n" );

                outSource += ( resource.AsString() );

                // Add #line to the end of the included file to correct the line count
                outSource += ( "\n#line " + lineCount +
                               "\"" + fileName + "\"\n" );

                startMarker = newLineAfter;

                if ( startMarker != -1 )
                    i = inSource.IndexOf( "#include", startMarker );
                else
                    i = -1;
            }

            // copy any remaining characters
            outSource += ( inSource.Substring( startMarker ) );

            return outSource;
        }

        #endregion

        /// <summary>
		///		Only bother with supported programs.
		/// </summary>
		public override void Touch()
		{
			if ( this.IsSupported )
			{
				base.Touch();
			}
		}


		#endregion Methods

		#region Properties

		/// <summary>
		///    Returns whether or not this high level gpu program is supported on the current hardware.
		/// </summary>
		public override bool IsSupported
		{
			get
			{
                if (HasCompileError || !IsRequiredCapabilitiesSupported())
					return false;

				// If skeletal animation is being done, we need support for UBYTE4
				if ( this.IsSkeletalAnimationIncluded &&
					!Root.Instance.RenderSystem.Capabilities.HasCapability( Capabilities.VertexFormatUByte4 ) )
				{

					return false;
				}

				// see if any profiles are supported
                if ( profiles != null )
                {
                    for ( int i = 0; i < profiles.Length; i++ )
                    {
                        if ( GpuProgramManager.Instance.IsSyntaxSupported( profiles[ i ] ) )
                        {
                            return true;
                        }
                    }
                }

				// nope, SOL
				return false;
			}
		}

		public override int SamplerCount
		{
			get
			{
				switch ( selectedProfile )
				{
					case "ps_1_1":
					case "ps_1_2":
					case "ps_1_3":
					case "fp20":
						return 4;
					case "ps_1_4":
						return 6;
					case "ps_2_0":
					case "ps_2_x":
					case "ps_3_0":
					case "ps_3_x":
					case "arbfp1":
					case "fp30":
					case "fp40":
						return 16;
					default:
						throw new AxiomException( "Attempted to query sample count for unknown shader profile({0}).", selectedProfile );
				}

				return 0;
			}
		}

	    public string CompileArguments { get; set; }

		#endregion Properties

		#region IConfigurable Members

		/// <summary>
		///    Method for passing parameters into the CgProgram.
		/// </summary>
		/// <param name="name">
		///    Param name.
		/// </param>
		/// <param name="val">
		///    Param value.
		/// </param>
		public override bool SetParam( string name, string val )
		{
			bool handled = true;

			switch ( name )
			{
				case "entry_point":
					entry = val;
					break;

				case "profiles":
					profiles = val.Split( ' ' );
					break;

                case "compile_arguments":
                    CompileArguments = val;
                    break;

				default:
					LogManager.Instance.Write( "CgProgram: Unrecognized parameter '{0}'", name );
					handled = false;
					break;
			}

			return handled;
		}

		#endregion IConfigurable Members
	}
}