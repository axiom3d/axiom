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
using System.IO;
using Axiom.Core;
using Axiom.Scripting;

using ResourceHandle = System.UInt64;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
	/// <summary>
	/// 	Defines a program which runs on the GPU such as a vertex or fragment program.
	/// </summary>
	public abstract class GpuProgram : Resource
	{
		#region Fields and Properties

		#region BindingDelegate Property

		/// <summary>
		///    Returns the GpuProgram which should be bound to the pipeline.
		/// </summary>
		/// <remarks>
		///    This method is simply to allow some subclasses of GpuProgram to delegate
		///    the program which is bound to the pipeline to a delegate, if required.
		/// </remarks>
		[OgreVersion(1,7)]
		public virtual GpuProgram BindingDelegate
		{
			get
			{
				return this;
			}
		}

		#endregion BindingDelegate Property

	    /// <summary>
	    ///    Whether this source is being loaded from file or not.
	    /// </summary>
	    protected bool LoadFromFile { get; private set; }

		#region SourceFile Property

		/// <summary>
		///    The name of the file to load from source (may be blank).
		/// </summary>
		private string _fileName;
		/// <summary>
		///    Gets/Sets the source file for this program.
		/// </summary>
		/// <remarks>
		///    Setting this will have no effect until you (re)load the program.
		/// </remarks>
        [OgreVersion(1, 7)]
        public virtual string SourceFile
		{
			get
			{
				return _fileName;
			}
			set
			{
				_fileName = value;
				_source = "";
				LoadFromFile = true;
				_compileError = false;
			}
		}

		#endregion SourceFile Property

		#region Source Property

		/// <summary>
		///    The assembler source of this program.
		/// </summary>
		private string _source;
		/// <summary>
		///    Gets/Sets the source assembler code for this program.
		/// </summary>
		/// <remarks>
		///    Setting this will have no effect until you (re)load the program.
		/// </remarks>
        [OgreVersion(1, 7)]
        public virtual string Source
		{
			get
			{
				return _source;
			}
			set
			{
				_source = value;
				_fileName = "";
				LoadFromFile = false;
				_compileError = false;
			}
		}

		#endregion Source Property

		#region SyntaxCode Property

	    /// <summary>
	    ///   Syntax code (i.e. arbvp1, vs_2_0, etc.)
	    /// </summary>
        [OgreVersion(1, 7)]
        public virtual string SyntaxCode { get; set; }

		#endregion SyntaxCode Property

        #region Language Property

	    /// <summary>
	    ///    Gets the language of this program
	    /// </summary>
        [OgreVersion(1, 7)]
        public virtual string Language { get { return "asm"; } }

        #endregion SyntaxCode Property

		#region Type Property

	    private GpuProgramType _type;

	    /// <summary>
        ///   Type of program this represents (vertex or fragment).
	    /// </summary>
        [OgreVersion(1, 7)]
        public virtual GpuProgramType Type
        {
            get
            {
                return _type;
            }
            set
            {
                _type = value;
            }
        }

		#endregion Type Property

		#region IsSkeletalAnimationIncluded Property

	    private bool _isSkeletalAnimationIncluded;

	    /// <summary>
	    ///		Flag indicating whether this program is being used for hardware skinning.
	    /// </summary>
        [OgreVersion(1, 7)]
        public virtual bool IsSkeletalAnimationIncluded
        {
            get
            {
                return _isSkeletalAnimationIncluded;
            }
            set
            {
                _isSkeletalAnimationIncluded = value;
            }
        }

		#endregion IsSkeletalAnimationIncluded Property

		#region IsMorphAninimationIncluded Property

		/// <summary>
		///		Does this (vertex) program include morph animation?
		/// </summary>
		private bool _isMorphAnimationSupported;
		/// <summary>
		///		Gets/Sets whether a vertex program includes the required instructions
		///		to perform morph animation. 
		/// </summary>
        [OgreVersion(1, 7)]
        public virtual bool IsMorphAnimationIncluded
		{
			get
			{
				return _isMorphAnimationSupported;
			}
			set
			{
				_isMorphAnimationSupported = value;
			}
		}

		#endregion IsMorphAninimationIncluded Property

		#region IsVertexTextureFetchRequired Property

		/// <summary>
		///		Does this (vertex) program require vertex texture fetch?
		/// </summary>
		private bool _isVertexTextureFetchRequired;
		/// <summary>
		///		Gets/Sets whether this vertex program requires support for vertex 
		///		texture fetch from the hardware. 
		/// </summary>
        [OgreVersion(1, 7)]
        public virtual bool IsVertexTextureFetchRequired
		{
			get
			{
				return _isVertexTextureFetchRequired;
			}
			set
			{
				_isVertexTextureFetchRequired = value;
			}
		}

		#endregion IsVertexTextureFetchRequired Property

        #region IsAdjacencyInfoRequired Property

        /// <summary>
        ///		Does this (vertex) program require vertex texture fetch?
        /// </summary>
        private bool _isAdjacencyInfoRequired;
        /// <summary>
        ///		Gets/Sets whether this vertex program requires support for vertex 
        ///		texture fetch from the hardware. 
        /// </summary>
        [OgreVersion(1, 7)]
        public virtual bool IsAdjacencyInfoRequired
        {
            get
            {
                return _isAdjacencyInfoRequired;
            }
            set
            {
                _isAdjacencyInfoRequired = value;
            }
        }

        #endregion IsAdjacencyInfoRequired Property

        #region PoseAnimationCount Property

        /// <summary>
		///		Does this (vertex) program include morph animation?
		/// </summary>
		private ushort _poseAnimationCount;
		/// <summary>
		///		Gets/Sets whether a vertex program includes the required instructions
		///		to perform pose animation. 
		/// </summary>
        [OgreVersion(1, 7)]
        public virtual ushort PoseAnimationCount
		{
			get
			{
				return _poseAnimationCount;
			}
			set
			{
				_poseAnimationCount = value;
			}
		}

		#endregion PoseAnimationCount Property

        #region IsPoseAnimationIncluded Property

	    /// <summary>
	    /// 
	    /// </summary>
        [OgreVersion(1, 7)]
        public virtual bool IsPoseAnimationIncluded
	    {
	        get
	        {
	            return _poseAnimationCount > 0;
	        }
	    }

        #endregion SyntaxCode Property

		#region DefaultParameters Property

		/// <summary>
		///	List of default parameters, as gathered from the program definition.
		/// </summary>
        [OgreVersion(1, 7)]
        private GpuProgramParameters _defaultParams;
		/// <summary>
		///	Get a reference to the default parameters which are to be used for all uses of this program.
		/// </summary>
		/// <remarks>
		/// A program can be set up with a list of default parameters, which can save time when 
		/// using a program many times in a material with roughly the same settings. By 
		/// retrieving the default parameters and populating it with the most used options, 
		/// any new parameter objects created from this program afterwards will automatically include
		/// the default parameters; thus users of the program need only change the parameters
		/// which are unique to their own usage of the program.
		/// </remarks>
		public virtual GpuProgramParameters DefaultParameters
		{
			get
			{
			    return _defaultParams ?? ( _defaultParams = CreateParameters() );
			}
		}

		#endregion DefaultParameters Property

		#region HasDefaultParameters Property

        [OgreVersion(1, 7)]
		public virtual bool HasDefaultParameters
		{
			get
			{
				return _defaultParams != null;
			}
		}

		#endregion HasDefaultParameters Property

		#region PassSurfaceAndLightStates Property

		/// <summary>
		///		Determines whether a vertex program requires light and material states to be passed
		///		to through fixed pipeline low level API rendering calls.
		/// </summary>
		/// <remarks>
		///		If this is set to true, Axiom will pass all active light states to the fixed function
		///		pipeline.  This is useful for high level shaders like GLSL that can read the OpenGL
		///		light and material states.  This way the user does not have to use autoparameters to 
		///		pass light position, color etc.
		/// </remarks>
        [OgreVersion(1, 7)]
        public virtual bool PassSurfaceAndLightStates
		{
			get
			{
				return false;
			}
		}

        #endregion PassSurfaceAndLightStates Property

        #region PassFogStates Property

        /// <summary>
        ///		Determines whether a vertex program requires fog states to be passed
        ///		to through fixed pipeline low level API rendering calls.
        /// </summary>
        /// <remarks>
        ///		If this is set to true, Axiom will pass all fog states to the fixed function
        ///		pipeline.  This is useful for high level shaders like GLSL that can read the OpenGL
        ///		fog states.  This way the user does not have to use autoparameters to 
        ///		pass fog color etc.
        /// </remarks>
        [OgreVersion(1, 7)]
        public virtual bool PassFogStates
        {
            get
            {
                return true;
            }
        }

        #endregion PassFogStates Property

        #region PassTransformStates Property

        /// <summary>
        ///		Sets whether a vertex program requires transform states to be passed
        ///		to through fixed pipeline low level API rendering calls.
        /// </summary>
        /// <remarks>
        ///		If this is set to true, Axiom will pass all transform states to the fixed function
        ///		pipeline.  This is useful for high level shaders like GLSL that can read the OpenGL
        ///		transform states.  This way the user does not have to use autoparameters to 
        ///		pass position etc.
        /// </remarks>
        [OgreVersion(1, 7)]
        public virtual bool PassTransformStates
        {
            get
            {
                return false;
            }
        }

        #endregion PassTransformStates Property

        #region IsSupported Property

        /// <summary>
		///    Returns whether this program can be supported on the current renderer and hardware.
		/// </summary>
        [OgreVersion(1, 7)]
        public virtual bool IsSupported
		{
			get
			{
				if ( _compileError || !IsRequiredCapabilitiesSupported() )
				{
					return false;
				}

				return GpuProgramManager.Instance.IsSyntaxSupported( SyntaxCode );
			}
		}

		#endregion IsSupported Property

		#region SamplerCount Property

		/// <summary>
		/// Returns the maximum number of samplers that this fragment program has access
		/// to, based on the fragment program profile it uses.
		/// </summary>
        [OgreVersion(1, 7)]
        public abstract int SamplerCount
		{
			get;
		}

		#endregion SamplerCount Property

		#region CompilerError Property

		/// <summary>
		/// Did we encounter a compilation error?
		/// </summary>
		private bool _compileError;

		/// <summary>
		/// Did this program encounter a compile error when loading?
		/// </summary>
        [OgreVersion(1, 7)]
        public virtual bool HasCompileError
		{
			get
			{
				return _compileError;
			}
		}

		/// <summary>
		/// Reset a compile error if it occurred, allowing the load to be retried.
		/// </summary>
		public virtual void ResetCompileError()
		{
			_compileError = false;
		}

		#endregion CompilerError Property

		/// <summary>
		/// Record of logical to physical buffer maps. Mandatory for low-level
		/// programs or high-level programs which set their params the same way.
		/// </summary>
        private GpuProgramParameters.GpuLogicalBufferStruct _floatLogicalToPhysical ;

	    /// <summary>
	    /// Record of logical to physical buffer maps. Mandatory for low-level
	    /// programs or high-level programs which set their params the same way.
	    /// </summary>
	    private GpuProgramParameters.GpuLogicalBufferStruct _intLogicalToPhysical;

		#region ConstantDefinitions Property

		/// <summary>
		/// Parameter name -> ConstantDefinition map, shared instance used by all parameter objects
		/// </summary>
		private GpuProgramParameters.GpuNamedConstants _constantDefs;
		/// <summary>
		/// Get the full list of named constants.
		/// </summary>
		/// <note>
		/// Only available if this parameters object has named parameters, which means either
		/// a high-level program which loads them, or a low-level program which has them
		/// specified manually.
		/// </note>
        [OgreVersion(1, 7)]
        public GpuProgramParameters.GpuNamedConstants ConstantDefinitions
		{
			get
			{
				return _constantDefs;
			}
		}

		#endregion ConstantDefinitions Property

		#region ManualNamedConstants Property

		/// <summary>
		/// Allows you to manually provide a set of named parameter mappings
		/// to a program which would not be able to derive named parameters itself.
		/// </summary>
		/// <remarks>
		/// You may wish to use this if you have assembler programs that were compiled
		/// from a high-level source, and want the convenience of still being able
		/// to use the named parameters from the original high-level source.
		/// <seealso cref="ManualNamedConstantsFile"/>
		/// </remarks>
        [OgreVersion(1, 7, "_constantDefs = value needs to be a copy rather than reference set")]
		public GpuProgramParameters.GpuNamedConstants ManualNamedConstants
		{
			get
			{
				return _constantDefs;
			}
			set
			{
			    CreateParameterMappingStructures();
				_constantDefs = value; // TODO: 

				_floatLogicalToPhysical.BufferSize = _constantDefs.FloatBufferSize;
				_intLogicalToPhysical.BufferSize = _constantDefs.IntBufferSize;
				_floatLogicalToPhysical.Map.Clear();
				_intLogicalToPhysical.Map.Clear();

				// need to set up logical mappings too for some rendersystems
				foreach ( var pair in _constantDefs.Map )
				{
					var name = pair.Key;
					var def = pair.Value;
					// only consider non-array entries
					if ( name.Contains( "[" ) )
					{
						var val = new GpuProgramParameters.GpuLogicalIndexUse( def.PhysicalIndex, def.ArraySize * def.ElementSize, def.Variability );
						if ( def.IsFloat )
						{
							_floatLogicalToPhysical.Map.Add( def.LogicalIndex, val );
						}
						else
						{
							_intLogicalToPhysical.Map.Add( def.LogicalIndex, val );
						}
					}
				}
			}
		}

		#endregion

		#region ManualNamedConstantsFile Property

		/// <summary>   
		/// File from which to load named constants manually
		/// </summary>
		private string _manualNamedConstantsFile;
		/// <summary>
		/// Specifies the name of a file from which to load named parameters mapping
		/// for a program which would not be able to derive named parameters itself.
		/// </summary>
		/// <remarks>
		/// You may wish to use this if you have assembler programs that were compiled
		/// from a high-level source, and want the convenience of still being able
		/// to use the named parameters from the original high-level source. This
		/// method will make a low-level program search in the resource group of the
		/// program for the named file from which to load parameter names from. 
		/// The file must be in the format produced by <see>GpuNamedConstants.Save</see>.
		/// </remarks>
        [OgreVersion(1, 7)]
        public virtual string ManualNamedConstantsFile
		{
			get
			{
				return _manualNamedConstantsFile;
			}
			set
			{
				_manualNamedConstantsFile = value;
			}
		}

		#endregion ManualNamedConstantsFile Property

		private bool _loadedManualNamedConstants;

		#endregion Fields and Properties

		#region Construction and Destruction

	    /// <summary>
	    ///    Constructor for creating
	    /// </summary>
        [OgreVersion(1, 7)]
        protected GpuProgram( ResourceManager parent, string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader )
			: base( parent, name, handle, group, isManual, loader )
		{
            _type = GpuProgramType.Vertex;
            LoadFromFile = true;
            _isSkeletalAnimationIncluded = false;
            _isMorphAnimationSupported = false;
            _poseAnimationCount = 0;
            _isVertexTextureFetchRequired = false;
            _isAdjacencyInfoRequired = false;
            _compileError = false;
			_loadedManualNamedConstants = false;
            
            CreateParameterMappingStructures();
		}

		#endregion Construction and Destruction

		#region Methods

		/// <summary>
		///    Creates a new parameters object compatible with this program definition.
		/// </summary>
		/// <remarks>
		///    It is recommended that you use this method of creating parameters objects
		///    rather than going direct to GpuProgramManager, because this method will
		///    populate any implementation-specific extras (like named parameters) where
		///    they are appropriate.
		/// </remarks>
		/// <returns></returns>
        [OgreVersion(1, 7)]
        public virtual GpuProgramParameters CreateParameters()
		{
			GpuProgramParameters newParams = GpuProgramManager.Instance.CreateParameters();

			// optionally load manually supplied named constants
			if ( !String.IsNullOrEmpty( _manualNamedConstantsFile ) && !_loadedManualNamedConstants )
			{
				try
				{
					var namedConstants = new GpuProgramParameters.GpuNamedConstants();
					var stream = ResourceGroupManager.Instance.OpenResource( _manualNamedConstantsFile, Group, true, this );
					namedConstants.Load( stream );
					ManualNamedConstants = namedConstants;
				}
				catch ( Exception ex )
				{
					LogManager.Instance.Write( "Unable to load manual named constants for GpuProgram {0} : {1}", Name, LogManager.BuildExceptionString( ex ) );
				}
				_loadedManualNamedConstants = true;
			}

			/*
			// set up named parameters, if any
			if ( constantDefs.Map.Count != 0 )
			{
				newParams._setNamedConstants( constantDefs );
			}
			// link shared logical / physical map for low-level use
			newParams._setLogicalIndexes( floatLogicalToPhysical, intLogicalToPhysical );
			*/

			// Copy in default parameters if present
			if ( _defaultParams != null )
				newParams.CopyConstantsFrom( _defaultParams );

			return newParams;
		}

		/// <summary>
		///    Loads this Gpu Program.
		/// </summary>
        [OgreVersion(1,7, "original name loadImpl")]
		protected override void load()
		{
			// load from file and get the source string from it
			if ( LoadFromFile )
			{
				var stream = ResourceGroupManager.Instance.OpenResource( _fileName, Group, true, this );
				var reader = new StreamReader( stream, System.Text.Encoding.UTF8 );
				_source = reader.ReadToEnd();
			}

            // Call polymorphic load
			try
			{
				LoadFromSource();

				if ( _defaultParams != null )
				{
					// Keep a reference to old ones to copy
					var savedParams = _defaultParams;
                    // reset params to stop them being referenced in the next create
				    // _defaultParams.SetNull();

					// Create new params
					_defaultParams = CreateParameters();

					// Copy old (matching) values across
					// Don't use copyConstantsFrom since program may be different
					_defaultParams.CopyMatchingNamedConstantsFrom(savedParams);
				}

			}
			catch ( Exception ex )
			{
                LogManager.Instance.Write("Gpu program {0} encountered an error during loading and is thus not supported. Details: {1}", Name, ex.Message);
				_compileError = true;
			}
		}

        [OgreVersion(1, 0, "not overriden in 1.7")]
		protected override void unload()
		{
		}

		/// <summary>
		///    Method which must be implemented by subclasses, loads the program from source.
		/// </summary>
        [OgreVersion(1, 7)]
		protected abstract void LoadFromSource();

        [OgreVersion(1, 7)]
		protected bool IsRequiredCapabilitiesSupported()
		{
			var caps = Root.Instance.RenderSystem.HardwareCapabilities;
			// If skeletal animation is being done, we need support for UBYTE4
			if ( IsSkeletalAnimationIncluded &&
				!caps.HasCapability( Capabilities.VertexFormatUByte4 ) )
			{
				return false;
			}

			// Vertex texture fetch required?
			if ( IsVertexTextureFetchRequired &&
				!caps.HasCapability( Capabilities.VertexTextureFetch ) )
			{
				return false;
			}

			return true;
		}

        [OgreVersion(1, 7)]
        protected void CreateParameterMappingStructures(bool recreateIfExists = true)
        {
            CreateLogicalParameterMappingStructures(recreateIfExists);
            CreateNamedParameterMappingStructures(recreateIfExists);
        }

        [OgreVersion(1, 7)]
        private void CreateLogicalParameterMappingStructures(bool recreateIfExists)
        {
            if (recreateIfExists || _floatLogicalToPhysical == null)
                _floatLogicalToPhysical = new GpuProgramParameters.GpuLogicalBufferStruct();
            if (recreateIfExists || _intLogicalToPhysical == null)
                _intLogicalToPhysical = new GpuProgramParameters.GpuLogicalBufferStruct();
        }

        [OgreVersion(1, 7)]
        private void CreateNamedParameterMappingStructures(bool recreateIfExists)
        {
            if (recreateIfExists || _constantDefs == null)
                _constantDefs = new GpuProgramParameters.GpuNamedConstants();
        }

		#endregion

		#region Custom Parameters

        [OgreVersion(1, 7)]
		[ScriptableProperty( "includes_skeletal_animation" )]
        protected class IncludesSkeletalAnimationPropertyCommand : IPropertyCommand<GpuProgram>
		{
			#region IPropertyCommand Members

            public string Get(GpuProgram target)
			{
				return target.IsSkeletalAnimationIncluded.ToString();
			}

            public void Set(GpuProgram target, string val)
			{
				target.IsSkeletalAnimationIncluded = bool.Parse( val );
			}

			#endregion IPropertyCommand Members
		}

        [OgreVersion(1, 7)]
		[ScriptableProperty( "includes_morph_animation" )]
        protected class IncludesMorphAnimationPropertyCommand : IPropertyCommand<GpuProgram>
		{
			#region IPropertyCommand Members

            public string Get(GpuProgram target)
			{
				return target.IsMorphAnimationIncluded.ToString();
			}

            public void Set(GpuProgram target, string val)
			{
				target.IsMorphAnimationIncluded = bool.Parse( val );
			}

			#endregion IPropertyCommand Members
		}

        [OgreVersion(1, 7)]
		[ScriptableProperty( "includes_pose_animation" )]
        protected class IncludesPoseAnimationPropertyCommand : IPropertyCommand<GpuProgram>
		{
			#region IPropertyCommand Members

            public string Get(GpuProgram target)
			{
				return target._poseAnimationCount.ToString();
			}

            public void Set(GpuProgram target, string val)
			{
				target._poseAnimationCount = ushort.Parse( val );
			}

			#endregion IPropertyCommand Members
		}

        [OgreVersion(1, 7)]
        [ScriptableProperty( "uses_vertex_texture_fetch" )]
        protected class IsVertexTextureFetchRequiredPropertyCommand : IPropertyCommand<GpuProgram>
        {
            #region IPropertyCommand Members

            public string Get(GpuProgram target)
            {
                return target.IsVertexTextureFetchRequired.ToString();
            }

            public void Set(GpuProgram target, string val)
            {
                target.IsVertexTextureFetchRequired = bool.Parse(val);
            }

            #endregion IPropertyCommand Members
        }

        [OgreVersion(1, 7)]
        [ScriptableProperty( "manual_named_constants" )]
        protected class ManualNamedConstantsFilePropertyCommand : IPropertyCommand<GpuProgram>
        {
            #region IPropertyCommand Members

            public string Get(GpuProgram target)
            {
                return target.ManualNamedConstantsFile;
            }

            public void Set(GpuProgram target, string val)
            {
                target.ManualNamedConstantsFile = val;
            }

            #endregion IPropertyCommand Members
        }

		#endregion Custom Parameters
	}
}
