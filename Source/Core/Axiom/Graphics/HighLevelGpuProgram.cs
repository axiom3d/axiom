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
using ResourceHandle = System.UInt64;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
	/// <summary>
	///   Abstract base class representing a high-level program (a vertex or fragment program).
	/// </summary>
	/// <remarks>
	///   High-level programs are vertex and fragment programs written in a high-level language such as Cg or HLSL, and as such do not require you to write assembler code like GpuProgram does. However, the high-level program does eventually get converted (compiled) into assembler and then eventually microcode which is what runs on the GPU. As well as the convenience, some high-level languages like Cg allow you to write a program which will operate under both Direct3D and OpenGL, something which you cannot do with just GpuProgram (which requires you to write 2 programs and use each in a Technique to provide cross-API compatibility). The engine will be creating a GpuProgram for you based on the high-level program, which is compiled specifically for the API being used at the time, but this process is transparent. <p /> You cannot create high-level programs direct - use HighLevelGpuProgramManager instead. Plugins can register new implementations of HighLevelGpuProgramFactory in order to add support for new languages without requiring changes to the core engine API. To allow custom parameters to be set, this class implement IConfigurable - the application can query on the available custom parameters and get/set them without having to link specifically with it.
	/// </remarks>
	public abstract class HighLevelGpuProgram : GpuProgram
	{
		/// <summary>
		///   Whether the high-level program (and it's parameter defs) is loaded.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )] protected bool highLevelLoaded;

		/// <summary>
		///   The underlying assembler program.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )] protected GpuProgram assemblerProgram;

		[OgreVersion( 1, 7, 2790 )] protected bool constantDefsBuilt;

		#region BindingDelegate Property

		/// <summary>
		///   Gets the lowlevel assembler program based on this HighLevel program.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public override GpuProgram BindingDelegate
		{
			get
			{
				return assemblerProgram;
			}
		}

		#endregion BindingDelegate Property

		#region constructor

		/// <summary>
		///   Default constructor.
		/// </summary>
		protected HighLevelGpuProgram( ResourceManager parent, string name, ResourceHandle handle, string group, bool isManual,
		                               IManualResourceLoader loader )
			: base( parent, name, handle, group, isManual, loader )
		{
		}

		#endregion Construction and Destruction

		#region LoadHighLevel

		/// <summary>
		///   Internal load high-level portion if not loaded
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		protected virtual void LoadHighLevel()
		{
			if ( highLevelLoaded )
			{
				return;
			}
			try
			{
				LoadHighLevelImpl();
				highLevelLoaded = true;
				if ( defaultParams != null )
				{
					// Keep a reference to old ones to copy
					var savedParams = defaultParams;
					// reset params to stop them being referenced in the next create
					//defaultParams = null;

					// Create new params
					defaultParams = CreateParameters();

					// Copy old (matching) values across
					// Don't use copyConstantsFrom since program may be different
					defaultParams.CopyMatchingNamedConstantsFrom( savedParams );
				}
			}
			catch ( Exception e )
			{
				// will already have been logged
				LogManager.Instance.Write(
					"High-level program {0} encountered an error during loading and is thus not supported.\n{1}", _name, e.Message );
				compileError = true;
			}
		}

		#endregion

		#region LoadHighLevelImpl

		/// <summary>
		///   Internal load implementation, loads just the high-level portion, enough to get parameters.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		protected virtual void LoadHighLevelImpl()
		{
			if ( LoadFromFile )
			{
				// find & load source code
				using ( var stream = ResourceGroupManager.Instance.OpenResource( fileName, _group, true, this ) )
				{
					using ( var t = new StreamReader( stream ) )
					{
						source = t.ReadToEnd();
					}
				}
			}

			LoadFromSource();
		}

		#endregion

		#region UnloadHighLevel

		/// <summary>
		///   Internal unload high-level portion if loaded
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		protected virtual void UnloadHighLevel()
		{
			if ( !highLevelLoaded )
			{
				return;
			}

			UnloadHighLevelImpl();
			// Clear saved constant defs
			constantDefsBuilt = false;
			CreateParameterMappingStructures( true );

			highLevelLoaded = false;
		}

		#endregion

		#region CreateLowLevelImpl

		/// <summary>
		///   Internal method for creating an appropriate low-level program from this high-level program, must be implemented by subclasses.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		protected abstract void CreateLowLevelImpl();

		#endregion

		#region UnloadHighLevelImpl

		/// <summary>
		///   Internal unload implementation, must be implemented by subclasses.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		protected abstract void UnloadHighLevelImpl();

		#endregion

		#region PopulateParameterNames

		/// <summary>
		///   Populate the passed parameters with name->index map, must be overridden.
		/// </summary>
		/// <param name="parms"> </param>
		[OgreVersion( 1, 7, 2790 )]
		protected virtual void PopulateParameterNames( GpuProgramParameters parms )
		{
			var defs = ConstantDefinitions; // Axiom: Ogre has SIDE EFFECT here!!
			parms.NamedConstants = constantDefs;
			// also set logical / physical maps for programs which use this
			parms.SetLogicalIndexes( floatLogicalToPhysical, intLogicalToPhysical );
		}

		#endregion

		#region BuildConstantDefinitions

		/// <summary>
		///   Build the constant definition map, must be overridden.
		/// </summary>
		/// <remarks>
		///   The implementation must fill in the (inherited) mConstantDefs field at a minimum, and if the program requires that parameters are bound using logical parameter indexes then the mFloatLogicalToPhysical and mIntLogicalToPhysical maps must also be populated.
		/// </remarks>
		[OgreVersion( 1, 7, 2790 )]
		protected abstract void BuildConstantDefinitions();

		#endregion

		#region ConstantDefinitions

		[OgreVersion( 1, 7, 2790 )]
		public override GpuProgramParameters.GpuNamedConstants ConstantDefinitions
		{
			get
			{
				if ( !constantDefsBuilt )
				{
					BuildConstantDefinitions();
					constantDefsBuilt = true;
				}

				return constantDefs;
			}
		}

		#endregion

		#region NamedConstants

		/// <summary>
		///   Override GpuProgram::getNamedConstants to ensure built
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		public override GpuProgramParameters.GpuNamedConstants NamedConstants
		{
			get
			{
				return ConstantDefinitions;
			}
		}

		#endregion

		#region CreateParameters

		/// <summary>
		///   Creates a new parameters object compatible with this program definition.
		/// </summary>
		/// <remarks>
		///   Unlike low-level assembly programs, parameters objects are specific to the program and therefore must be created from it rather than by the HighLevelGpuProgramManager. This method creates a new instance of a parameters object containing the definition of the parameters this program understands.
		/// </remarks>
		/// <returns> A new set of program parameters. </returns>
		[OgreVersion( 1, 7, 2790 )]
		public override GpuProgramParameters CreateParameters()
		{
			// Lock mutex before allowing this since this is a top-level method
			// called outside of the load()
#if AXIOM_MULTITHREADED
            lock ( _autoMutex )
#endif
			{
				// Make sure param defs are loaded
				var newParams = GpuProgramManager.Instance.CreateParameters();

				// Only populate named parameters if we can support this program
				if ( IsSupported )
				{
					// Errors during load may have prevented compile
					LoadHighLevel();
					if ( IsSupported )
					{
						PopulateParameterNames( newParams );
					}
				}


				// copy in default parameters if present
				if ( defaultParams != null )
				{
					newParams.CopyConstantsFrom( DefaultParameters );
				}
				return newParams;
			}
		}

		#endregion

		#region loadImpl

		/// <summary>
		///   Implementation of Resource.load.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		protected override void load()
		{
			if ( !IsSupported )
			{
				return;
			}

			// load self 
			LoadHighLevel();

			// create low-level implementation
			CreateLowLevelImpl();
			// load constructed assembler program (if it exists)
			if ( assemblerProgram != null && assemblerProgram != this )
			{
				assemblerProgram.Load();
			}
		}

		#endregion

		#region unloadImpl

		/// <summary>
		///   Implementation of Resource.unload.
		/// </summary>
		[OgreVersion( 1, 7, 2790 )]
		protected override void unload()
		{
			if ( assemblerProgram != null && assemblerProgram != this )
			{
				assemblerProgram.Creator.Remove( assemblerProgram.Handle );
				assemblerProgram = null;
			}

			UnloadHighLevel();
			ResetCompileError();
		}

		#endregion
	}

	/// <summary>
	///   Interface definition for factories that create instances of HighLevelGpuProgram.
	/// </summary>
	public abstract class HighLevelGpuProgramFactory : AbstractFactory<HighLevelGpuProgram>
	{
		#region Properties

		/// <summary>
		///   Gets the name of the HLSL language that this factory creates programs for.
		/// </summary>
		public abstract string Language { get; }

		#endregion Properties

		#region Methods

		/// <summary>
		///   Create method which needs to be implemented to return an instance of a HighLevelGpuProgram.
		/// </summary>
		/// <returns> A newly created instance of HighLevelGpuProgram. </returns>
		public abstract HighLevelGpuProgram CreateInstance( ResourceManager creator, string name, ResourceHandle handle,
		                                                    string group, bool isManual, IManualResourceLoader loader );

		#endregion Methods

		#region AbstractFactory<HighLevelGpuProgram> Implementation

		/// <summary>
		///   For HighLevelGpuPrograms this simply returns the Language.
		/// </summary>
		public override string Type
		{
			get
			{
				return Language;
			}
		}

		/// <summary>
		///   Creates an instance of a HighLevelGpuProgram
		/// </summary>
		/// <param name="name"> </param>
		/// <returns> </returns>
		/// <remarks>
		///   This method cannot be used to create an instance of a HighLevelGpuProgram use CreateInstance( ResourceManager creator, string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader ) instead.
		/// </remarks>
		public override HighLevelGpuProgram CreateInstance( string name )
		{
			throw new AxiomException( "Cannot create a HighLevelGpuProgram without specifing the GpuProgramType." );
		}

		public override void DestroyInstance( ref HighLevelGpuProgram obj )
		{
			obj.SafeDispose();
			base.DestroyInstance( ref obj );
		}

		#endregion AbstractFactory<HighLevelGpuProgram> Implementation
	}
}