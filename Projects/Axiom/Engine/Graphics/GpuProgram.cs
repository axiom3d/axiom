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
using System.Collections.Generic;
using System.IO;
using System.Collections;

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
		protected bool loadFromFile;

		#region SourceFile Property

		/// <summary>
		///    The name of the file to load from source (may be blank).
		/// </summary>
		protected string fileName;
		/// <summary>
		///    Gets/Sets the source file for this program.
		/// </summary>
		/// <remarks>
		///    Setting this will have no effect until you (re)load the program.
		/// </remarks>
		public string SourceFile
		{
			get
			{
				return fileName;
			}
			set
			{
				fileName = value;
				source = "";
				loadFromFile = true;
				_compileError = false;
			}
		}

		#endregion SourceFile Property

		#region Source Property

		/// <summary>
		///    The assembler source of this program.
		/// </summary>
		protected string source;
		/// <summary>
		///    Gets/Sets the source assembler code for this program.
		/// </summary>
		/// <remarks>
		///    Setting this will have no effect until you (re)load the program.
		/// </remarks>
		public string Source
		{
			get
			{
				return source;
			}
			set
			{
				source = value;
				fileName = "";
				loadFromFile = false;
				_compileError = false;
			}
		}

		#endregion Source Property

		#region SyntaxCode Property

		/// <summary>
		///    Syntax code (i.e. arbvp1, vs_2_0, etc.)
		/// </summary>
		protected string syntaxCode;
		/// <summary>
		///    Gets the syntax code of this program (i.e. arbvp1, vs_1_1, etc).
		/// </summary>
		public string SyntaxCode
		{
			get
			{
				return syntaxCode;
			}
			set
			{
				syntaxCode = value;
			}
		}

		#endregion SyntaxCode Property

		#region Type Property

		/// <summary>
		///    Type of program this represents (vertex or fragment).
		/// </summary>
		protected GpuProgramType type;
		/// <summary>
		///    Gets the type of GPU program this represents (vertex or fragment).
		/// </summary>
		public virtual GpuProgramType Type
		{
			get
			{
				return type;
			}
			set
			{
				type = value;
			}
		}

		#endregion Type Property

		#region IsSkeletalAnimationIncluded Property

		/// <summary>
		///		Flag indicating whether this program is being used for hardware skinning.
		/// </summary>
		protected bool isSkeletalAnimationSupported;
		/// <summary>
		///		Gets/Sets whether a vertex program includes the required instructions
		///		to perform skeletal animation. 
		/// </summary>
		public virtual bool IsSkeletalAnimationIncluded
		{
			get
			{
				return isSkeletalAnimationSupported;
			}
			set
			{
				isSkeletalAnimationSupported = value;
			}
		}

		#endregion IsSkeletalAnimationIncluded Property

		#region IsMorphAninimationIncluded Property

		/// <summary>
		///		Does this (vertex) program include morph animation?
		/// </summary>
		protected bool isMorphAnimationSupported;
		/// <summary>
		///		Gets/Sets whether a vertex program includes the required instructions
		///		to perform morph animation. 
		/// </summary>
		public virtual bool IsMorphAnimationIncluded
		{
			get
			{
				return isMorphAnimationSupported;
			}
			set
			{
				isMorphAnimationSupported = value;
			}
		}

		#endregion IsMorphAninimationIncluded Property

		#region IsVertexTextureFetchRequired Property

		/// <summary>
		///		Does this (vertex) program require vertex texture fetch?
		/// </summary>
		protected bool _isVertexTextureFetchRequired;
		/// <summary>
		///		Gets/Sets whether this vertex program requires support for vertex 
		///		texture fetch from the hardware. 
		/// </summary>
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

		#region PoseAnimationCount Property

		/// <summary>
		///		Does this (vertex) program include morph animation?
		/// </summary>
		protected ushort poseAnimationCount;
		/// <summary>
		///		Gets/Sets whether a vertex program includes the required instructions
		///		to perform pose animation. 
		/// </summary>
		public virtual ushort PoseAnimationCount
		{
			get
			{
				return poseAnimationCount;
			}
			set
			{
				poseAnimationCount = value;
			}
		}

		#endregion PoseAnimationCount Property

		#region DefaultParameters Property

		/// <summary>
		///	List of default parameters, as gathered from the program definition.
		/// </summary>
		protected GpuProgramParameters defaultParams;
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
				if ( defaultParams == null )
				{
					defaultParams = this.CreateParameters();
				}
				return defaultParams;
			}
		}

		#endregion DefaultParameters Property

		#region HasDefaultParameters Property

		public virtual bool HasDefaultParameters
		{
			get
			{
				return defaultParams != null;
			}
		}

		#endregion HasDefaultParameters Property

		#region PassSurfaceAndLightStates Property

		/// <summary>
		///		Does this program want light states passed through fixed pipeline?
		/// </summary>
		protected bool passSurfaceAndLightStates;
		/// <summary>
		///		Sets whether a vertex program requires light and material states to be passed
		///		to through fixed pipeline low level API rendering calls.
		/// </summary>
		/// <remarks>
		///		If this is set to true, Axiom will pass all active light states to the fixed function
		///		pipeline.  This is useful for high level shaders like GLSL that can read the OpenGL
		///		light and material states.  This way the user does not have to use autoparameters to 
		///		pass light position, color etc.
		/// </remarks>
		public virtual bool PassSurfaceAndLightStates
		{
			get
			{
				return passSurfaceAndLightStates;
			}
			set
			{
				passSurfaceAndLightStates = value;
			}
		}

		#endregion PassSurfaceAndLightStates Property

		#region IsSupported Property

		/// <summary>
		///    Returns whether this program can be supported on the current renderer and hardware.
		/// </summary>
		public virtual bool IsSupported
		{
			get
			{
				if ( _compileError || !IsRequiredCapabilitiesSupported() )
				{

					return false;
				}

				return GpuProgramManager.Instance.IsSyntaxSupported( syntaxCode );
			}
		}

		#endregion IsSupported Property

		#region SamplerCount Property

		/// <summary>
		/// Returns the maximum number of samplers that this fragment program has access
		/// to, based on the fragment program profile it uses.
		/// </summary>
		public abstract int SamplerCount
		{
			get;
		}

		#endregion SamplerCount Property

		#region CompilerError Property

		/// <summary>
		/// Did we encounter a compilation error?
		/// </summary>
		protected bool _compileError = false;

		/// <summary>
		/// Did this program encounter a compile error when loading?
		/// </summary>
		public bool HasCompileError
		{
			get
			{
				return _compileError;
			}
		}

		/// <summary>
		/// Reset a compile error if it occurred, allowing the load to be retried.
		/// </summary>
		public void ResetCompileError()
		{
			_compileError = false;
		}

		#endregion CompilerError Property

		/// <summary>
		/// Record of logical to physical buffer maps. Mandatory for low-level
		/// programs or high-level programs which set their params the same way.
		/// </summary>
		//private GpuLogicalBufferStruct floatLogicalToPhysical = new GpuLogicalBufferStruct();
		/// <summary>
		/// Record of logical to physical buffer maps. Mandatory for low-level
		/// programs or high-level programs which set their params the same way.
		/// </summary>
		//private GpuLogicalBufferStruct intLogicalToPhysical = new GpuLogicalBufferStruct();

		#region ConstantDefinitions Property

		/// <summary>
		/// Parameter name -> ConstantDefinition map, shared instance used by all parameter objects
		/// </summary>
		private GpuProgramParameters.GpuNamedConstants constantDefs;
		/// <summary>
		/// Get the full list of named constants.
		/// </summary>
		/// <note>
		/// Only available if this parameters object has named parameters, which means either
		/// a high-level program which loads them, or a low-level program which has them
		/// specified manually.
		/// </note>
		public GpuProgramParameters.GpuNamedConstants ConstantDefinitions
		{
			get
			{
				return constantDefs;
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
		public GpuProgramParameters.GpuNamedConstants ManualNamedConstants
		{
			get
			{
				return constantDefs;
			}
			set
			{
				constantDefs = value;

				/*
				floatLogicalToPhysical.BufferSize = constantDefs.FloatBufferSize;
				intLogicalToPhysical.BufferSize = constantDefs.IntBufferSize;
				floatLogicalToPhysical.Map.Clear();
				intLogicalToPhysical.Map.Clear();

				// need to set up logical mappings too for some rendersystems
				foreach ( KeyValuePair<string, GpuProgramParameters.GpuConstantDefinition> pair in constantDefs.GpuConstantDefinitions )
				{
					string name = pair.Key;
					GpuProgramParameters.GpuConstantDefinition def = pair.Value;
					// only consider non-array entries
					if ( name.Contains( "[" ) )
					{
						GpuLogicalIndexUse val = new GpuLogicalIndexUse( def.PhysicalIndex, def.ArraySize * def.ElementSize, def.Variability );
						if ( def.IsFloat )
						{
							floatLogicalToPhysical.Map.Add( def.LogicalIndex, val );
						}
						else
						{
							intLogicalToPhysical.Map.Add( def.LogicalIndex, val );
						}
					}
				}
				*/
			}
		}

		#endregion

		#region ManualNamedConstantsFile Property

		/// <summary>   
		/// File from which to load named constants manually
		/// </summary>
		private string manualNamedConstantsFile;
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
		public string ManualNamedConstantsFile
		{
			get
			{
				return manualNamedConstantsFile;
			}
			set
			{
				manualNamedConstantsFile = value;
			}
		}

		#endregion ManualNamedConstantsFile Property

		private bool loadedManualNamedConstants;

		#endregion Fields and Properties

		#region Construction and Destruction

		/// <summary>
		///    Constructor for creating
		/// </summary>
		/// <param name="name"></param>
		/// <param name="type"></param>
		public GpuProgram( ResourceManager parent, string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader )
			: base( parent, name, handle, group, isManual, loader )
		{
			this.Name = name;

			this.type = GpuProgramType.Vertex;
			this.loadFromFile = true;
			this.loadedManualNamedConstants = false;
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
		public virtual GpuProgramParameters CreateParameters()
		{
			GpuProgramParameters newParams = GpuProgramManager.Instance.CreateParameters();

			// optionally load manually supplied named constants
			if ( !String.IsNullOrEmpty( manualNamedConstantsFile ) && !loadedManualNamedConstants )
			{
				try
				{
					GpuProgramParameters.GpuNamedConstants namedConstants = new GpuProgramParameters.GpuNamedConstants();
					Stream stream = ResourceGroupManager.Instance.OpenResource( manualNamedConstantsFile, Group, true, this );
					namedConstants.Load( stream );
					ManualNamedConstants = namedConstants;
				}
				catch ( Exception ex )
				{
					LogManager.Instance.Write( "Unable to load manual named constants for GpuProgram {0} : {1}", Name, LogManager.BuildExceptionString( ex ) );
				}
				loadedManualNamedConstants = true;
			}

			/*
			// set up named parameters, if any
			if ( constantDefs.GpuConstantDefinitions.Count != 0 )
			{
				newParams._setNamedConstants( constantDefs );
			}
			// link shared logical / physical map for low-level use
			newParams._setLogicalIndexes( floatLogicalToPhysical, intLogicalToPhysical );
			*/

			// Copy in default parameters if present
			if ( defaultParams != null )
				newParams.CopyConstantsFrom( defaultParams );

			return newParams;
		}

		/// <summary>
		///    Loads this Gpu Program.
		/// </summary>
		protected override void load()
		{
			// load from file and get the source string from it
			if ( loadFromFile )
			{
				Stream stream = ResourceGroupManager.Instance.OpenResource( fileName, this.Group );
				StreamReader reader = new StreamReader( stream, System.Text.Encoding.UTF8 );
				source = reader.ReadToEnd();
			}

			try
			{
				// call polymorphic load to read source
				LoadFromSource();

				if ( defaultParams != null )
				{
					// Keep a reference to old ones to copy
					GpuProgramParameters savedParams = defaultParams;

					// Create new params
					defaultParams = this.CreateParameters();

					// Copy old (matching) values across
					// Don't use copyConstantsFrom since program may be different
					//defaultParams.CopyMatchingNamedConstantsFrom(savedParams);
				}

			}
			catch ( Exception ex )
			{
				LogManager.Instance.Write( "Gpu program {0} encountered an error during loading and is thus not supported.", Name );
				_compileError = true;
			}
		}

		protected override void unload()
		{
		}

		/// <summary>
		///    Method which must be implemented by subclasses, loads the program from source.
		/// </summary>
		protected abstract void LoadFromSource();

		protected bool IsRequiredCapabilitiesSupported()
		{
			RenderSystemCapabilities caps = Root.Instance.RenderSystem.HardwareCapabilities;
			// If skeletal animation is being done, we need support for UBYTE4
			if ( this.IsSkeletalAnimationIncluded &&
				!caps.HasCapability( Capabilities.VertexFormatUByte4 ) )
			{
				return false;
			}

			// Vertex texture fetch required?
			if ( this.IsVertexTextureFetchRequired &&
				!caps.HasCapability( Capabilities.VertexTextureFetch ) )
			{
				return false;
			}

			return true;
		}

		#endregion

		#region Custom Parameters

		[ScriptableProperty( "includes_skeletal_animation" )]
		private class IncludesSkeletalAnimationPropertyCommand : Scripting.IPropertyCommand
		{
			#region IPropertyCommand Members

			public string Get( object target )
			{
				return ( (GpuProgram)target ).IsSkeletalAnimationIncluded.ToString();
			}

			public void Set( object target, string val )
			{
				( (GpuProgram)target ).IsSkeletalAnimationIncluded = bool.Parse( val );
			}

			#endregion IPropertyCommand Members
		}

		[ScriptableProperty( "includes_morph_animation" )]
		private class IncludesMorphAnimationPropertyCommand : Scripting.IPropertyCommand
		{
			#region IPropertyCommand Members

			public string Get( object target )
			{
				return ( (GpuProgram)target ).IsMorphAnimationIncluded.ToString();
			}

			public void Set( object target, string val )
			{
				( (GpuProgram)target ).IsMorphAnimationIncluded = bool.Parse( val );
			}

			#endregion IPropertyCommand Members
		}

		[ScriptableProperty( "includes_pose_animation" )]
		private class IncludesPoseAnimationPropertyCommand : Scripting.IPropertyCommand
		{
			#region IPropertyCommand Members

			public string Get( object target )
			{
				return ( (GpuProgram)target ).poseAnimationCount.ToString();
			}

			public void Set( object target, string val )
			{
				( (GpuProgram)target ).poseAnimationCount = ushort.Parse( val );
			}

			#endregion IPropertyCommand Members
		}

		#endregion Custom Parameters
	}
}
