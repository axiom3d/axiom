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
using System.IO;
using Axiom.Core;

using ResourceHandle = System.UInt64;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
	/// <summary>
	/// 	Abstract base class representing a high-level program (a vertex or
	/// 	fragment program).
	/// </summary>
	/// <remarks>
	///    High-level programs are vertex and fragment programs written in a high-level
	///    language such as Cg or HLSL, and as such do not require you to write assembler code
	///    like GpuProgram does. However, the high-level program does eventually 
	///    get converted (compiled) into assembler and then eventually microcode which is
	///    what runs on the GPU. As well as the convenience, some high-level languages like Cg allow
	///    you to write a program which will operate under both Direct3D and OpenGL, something
	///    which you cannot do with just GpuProgram (which requires you to write 2 programs and
	///    use each in a Technique to provide cross-API compatibility). The engine will be creating
	///    a GpuProgram for you based on the high-level program, which is compiled specifically 
	///    for the API being used at the time, but this process is transparent.
	///    <p/>
	///    You cannot create high-level programs direct - use HighLevelGpuProgramManager instead.
	///    Plugins can register new implementations of HighLevelGpuProgramFactory in order to add
	///    support for new languages without requiring changes to the core engine API. To allow 
	///    custom parameters to be set, this class implement IConfigurable - the application
	///    can query on the available custom parameters and get/set them without having to 
	///    link specifically with it.
	/// </remarks>
	public abstract class HighLevelGpuProgram : GpuProgram, IConfigurable
	{
		#region Fields and Properties

		/// <summary>
		///    Whether the high-level program (and it's parameter defs) is loaded.
		/// </summary>
		protected bool isHighLevelLoaded;

		#region BindingDelegate Property

		/// <summary>
		///    The underlying assembler program.
		/// </summary>
		protected GpuProgram assemblerProgram;
		/// <summary>
		///    Gets the lowlevel assembler program based on this HighLevel program.
		/// </summary>
		public override GpuProgram BindingDelegate
		{
			get
			{
				return assemblerProgram;
			}
		}
		#endregion BindingDelegate Property

		#endregion Fields and Properties

		#region Construction and Destruction

		/// <summary>
		///    Default constructor.
		/// </summary>
		/// <param name="name">Name of the high level program.</param>
		/// <param name="type">Type of program, vertex or fragment.</param>
		/// <param name="language">HLSL language this program is written in.</param>
		public HighLevelGpuProgram( ResourceManager parent, string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader )
			: base( parent, name, handle, group, isManual, loader )
		{
		}

		#endregion Construction and Destruction

		#region Methods

		/// <summary>
		///    Implementation of Resource.load.
		/// </summary>
		protected override void load()
		{
			if ( IsLoaded )
			{
				Unload();
			}

			// polymorphic load 
			LoadHighLevelImpl();

			// polymorphic creation of the low level program
			CreateLowLevelImpl();

			Debug.Assert( assemblerProgram != null, "Subclasses of HighLevelGpuProgram MUST initialize the low level assembler program." );

			// load the low level assembler program
			assemblerProgram.Load();
		}

		/// <summary>
		///    Internal load implementation, loads just the high-level portion, enough to 
		///    get parameters.
		/// </summary>
		protected virtual void LoadHighLevelImpl()
		{
			if ( !isHighLevelLoaded )
			{
				if ( LoadFromFile )
				{
                    var stream = ResourceGroupManager.Instance.OpenResource(SourceFile);
					var reader = new StreamReader( stream, System.Text.Encoding.UTF8 );
					Source = reader.ReadToEnd();
					stream.Close();
				}

				LoadFromSource();
				isHighLevelLoaded = true;
			}
		}

		/// <summary>
		///    Internal method for creating an appropriate low-level program from this
		///    high-level program, must be implemented by subclasses.
		/// </summary>
		protected abstract void CreateLowLevelImpl();

		/// <summary>
		///    Implementation of Resource.unload.
		/// </summary>
		protected override void unload()
		{
			if ( assemblerProgram != null )
			{
				assemblerProgram.Unload();
			}

			// polymorphic unload
			UnloadImpl();

			isHighLevelLoaded = false;
		}

		/// <summary>
		///    Internal unload implementation, must be implemented by subclasses.
		/// </summary>
		protected abstract void UnloadImpl();

		/// <summary>
		///    Populate the passed parameters with name->index map, must be overridden.
		/// </summary>
		/// <param name="parms"></param>
		protected abstract void PopulateParameterNames( GpuProgramParameters parms );

        protected abstract void BuildConstantDefinitions();

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
			// create and load named parameters
			GpuProgramParameters newParams = GpuProgramManager.Instance.CreateParameters();

			// load high level program and parameters if required
			if ( IsSupported )
			{
				// make sure parameter definitions are loaded
				LoadHighLevelImpl();

				PopulateParameterNames( newParams );
			}

			// copy in default parameters if present
            if (HasDefaultParameters)
			{
                newParams.CopyConstantsFrom(DefaultParameters);
			}

			return newParams;
		}

		#endregion

		#region IConfigurable Members

		/// <summary>
		///    Must be implemented by subclasses.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="val"></param>
		public abstract bool SetParam( string name, string val );

		#endregion
	}

	/// <summary>
	///    Interface definition for factories that create instances of HighLevelGpuProgram.
	/// </summary>
	public abstract class HighLevelGpuProgramFactory : AbstractFactory<HighLevelGpuProgram>
	{
		#region Properties

		/// <summary>
		///    Gets the name of the HLSL language that this factory creates programs for.
		/// </summary>
		public abstract string Language
		{
			get;
		}

		#endregion Properties

		#region Methods

		/// <summary>
		///    Create method which needs to be implemented to return an
		///    instance of a HighLevelGpuProgram.
		/// </summary>
		/// <param name="name">
		///    Name of the program to create.
		/// </param>
		/// <param name="type">
		///    Type of program to create, i.e. vertex or fragment.
		/// </param>
		/// <returns>
		///    A newly created instance of HighLevelGpuProgram.
		/// </returns>
		public abstract HighLevelGpuProgram CreateInstance( ResourceManager creator, string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader );


		#endregion Methods

		#region AbstractFactory<HighLevelGpuProgram> Implementation

		/// <summary>
		/// For HighLevelGpuPrograms this simply returns the Language.
		/// </summary>
		public string Type
		{
			get
			{
				return Language;
			}
		}

		/// <summary>
		/// Creates an instance of a HighLevelGpuProgram
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		/// <remarks>This method cannot be used to create an instance of a HighLevelGpuProgram use CreateInstance( ResourceManager creator, string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader ) instead.</remarks>
		public HighLevelGpuProgram CreateInstance( string name )
		{
			throw new Exception( "Cannot create a HighLevelGpuProgram without specifing the GpuProgramType." );
		}

		public virtual void DestroyInstance( HighLevelGpuProgram obj )
		{
            if (!obj.IsDisposed)
			    obj.Dispose();

			obj = null;
		}

		#endregion AbstractFactory<HighLevelGpuProgram> Implementation
	}

}
