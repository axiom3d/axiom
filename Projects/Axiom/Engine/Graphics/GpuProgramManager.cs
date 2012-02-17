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

using Axiom.Core;
using ResourceHandle = System.UInt64;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
	/// <summary>
	/// Summary description for GpuProgramManager.
	/// </summary>
	public abstract class GpuProgramManager : ResourceManager
	{
		#region Singleton implementation

		/// <summary>
		///     Singleton instance of this class.
		/// </summary>
		private static GpuProgramManager _instance;

		/// <summary>
		///     Internal constructor.  This class cannot be instantiated externally.
		/// </summary>
		/// <remarks>
		///     Protected internal because this singleton will actually hold the instance of a subclass
		///     created by a render system plugin.
		/// </remarks>
		protected internal GpuProgramManager()
			: base()
		{
			if ( _instance == null )
            {
                _instance = this;

				// Loading order
				LoadingOrder = 50.0f;
				// Resource type
				ResourceType = "GpuProgram";
			}
			else
				throw new AxiomException( "Cannot create another instance of {0}. Use Instance property instead", this.GetType().Name );

			// subclasses should register with resource group manager
		}

		/// <summary>
		///     Gets the singleton instance of this class.
		/// </summary>
		public static GpuProgramManager Instance
		{
			get
			{
				return _instance;
			}
		}

		#endregion Singleton implementation

		#region Methods

		/// <summary>
		///    Creates a new GpuProgram.
		/// </summary>
		/// <param name="name">
		///    Name of the program to create.
		/// </param>
		/// <param name="group"></param>
		/// <param name="type">
		///    Type of the program to create, i.e. vertex or fragment.
		/// </param>
		/// <param name="syntaxCode">
		///    Syntax of the program, i.e. vs_1_1, arbvp1, etc.
		/// </param>
		/// <returns>
		///    A new instance of GpuProgram.
		/// </returns>
		public GpuProgram Create( string name, string group, GpuProgramType type, string syntaxCode )
		{
			return Create( name, group, type, syntaxCode, false, null );
		}

	    /// <summary>
	    ///    Creates a new GpuProgram.
	    /// </summary>
	    /// <param name="name">
	    ///    Name of the program to create.
	    /// </param>
	    /// <param name="group"></param>
	    /// <param name="type">
	    ///    Type of the program to create, i.e. vertex or fragment.
	    /// </param>
	    /// <param name="syntaxCode">
	    ///    Syntax of the program, i.e. vs_1_1, arbvp1, etc.
	    /// </param>
	    /// <param name="isManual"></param>
	    /// <param name="loader"></param>
	    /// <returns>
	    ///    A new instance of GpuProgram.
	    /// </returns>
	    public virtual GpuProgram Create( string name, string group, GpuProgramType type, string syntaxCode, bool isManual, IManualResourceLoader loader )
		{
			// Call creation implementation
			var ret = (GpuProgram)_create( name, (ResourceHandle)name.ToLower().GetHashCode(), group, isManual, loader, type, syntaxCode );

			_add( ret );
			// Tell resource group manager
			ResourceGroupManager.Instance.notifyResourceCreated( ret );
			return ret;
		}

		/// <summary>
		/// Internal method for created programs, must be implemented by subclasses
		/// </summary>
		/// <param name="name"> Name of the program to create.</param>
		/// <param name="handle">Handle of the program</param>
		/// <param name="group">resource group of the program</param>
		/// <param name="isManual">is this program manually created</param>
		/// <param name="loader">The ManualResourceLoader if this program is manually loaded</param>
		/// <param name="type">Type of the program to create, i.e. vertex or fragment.</param>
		/// <param name="syntaxCode">Syntax of the program, i.e. vs_1_1, arbvp1, etc.</param>
		/// <returns>A new instance of GpuProgram.</returns>
		protected abstract Resource _create( string name, ResourceHandle handle, string group, bool isManual, IManualResourceLoader loader, GpuProgramType type, string syntaxCode );

	    /// <summary>
	    ///    Create a new, unloaded GpuProgram from a file of assembly.
	    /// </summary>
	    /// <remarks>
	    ///    Use this method in preference to the 'load' methods if you wish to define
	    ///    a GpuProgram, but not load it yet; useful for saving memory.
	    /// </remarks>
	    /// <param name="name">
	    ///    The name of the program.
	    /// </param>
	    /// <param name="group"></param>
	    /// <param name="fileName">
	    ///    The file to load.
	    /// </param>
	    /// <param name="type"></param>
	    /// <param name="syntaxCode">
	    ///    Name of the syntax to use for the program, i.e. vs_1_1, arbvp1, etc.
	    /// </param>
	    /// <returns>
	    ///    An unloaded GpuProgram instance.
	    /// </returns>
	    public virtual GpuProgram CreateProgram( string name, string group, string fileName, GpuProgramType type, string syntaxCode )
		{
			var program = Create( name, group, type, syntaxCode );

			program.Type = type;
			program.SyntaxCode = syntaxCode;
			program.SourceFile = fileName;

			return program;
		}

	    /// <summary>
	    ///    Create a new, unloaded GpuProgram from a string of assembly code.
	    /// </summary>
	    /// <remarks>
	    ///    Use this method in preference to the 'load' methods if you wish to define
	    ///    a GpuProgram, but not load it yet; useful for saving memory.
	    /// </remarks>
	    /// <param name="name">
	    ///    The name of the program.
	    /// </param>
	    /// <param name="group"></param>
	    /// <param name="source">
	    ///    The asm source of the program to create.
	    /// </param>
	    /// <param name="type"></param>
	    /// <param name="syntaxCode">
	    ///    Name of the syntax to use for the program, i.e. vs_1_1, arbvp1, etc.
	    /// </param>
	    /// <returns>An unloaded GpuProgram instance.</returns>
	    public virtual GpuProgram CreateProgramFromString( string name, string group, string source, GpuProgramType type, string syntaxCode )
		{
			var program = Create( name, group, type, syntaxCode );

			program.Type = type;
			program.SyntaxCode = syntaxCode;
			program.Source = source;

			return program;
		}

		/// <summary>
		///    Creates a new GpuProgramParameters instance which can be used to bind parameters 
		///    to your programs.
		/// </summary>
		/// <remarks>
		///    Program parameters can be shared between multiple programs if you wish.
		/// </remarks>
		/// <returns></returns>
		public virtual GpuProgramParameters CreateParameters()
		{
			return new GpuProgramParameters();
		}

		/// <summary>
		///    Returns whether a given syntax code (e.g. "ps_1_3", "fp20", "arbvp1") is supported. 
		/// </summary>
		/// <param name="syntaxCode"></param>
		/// <returns></returns>
		public bool IsSyntaxSupported( string syntaxCode )
		{
        	// Use the current render system
        	  var rs =  Root.Instance.RenderSystem;

            // Get the supported syntaxed from RenderSystemCapabilities 
            return rs.Capabilities.IsShaderProfileSupported( syntaxCode );
		}

	    /// <summary>
	    ///    Loads a GPU program from a file of assembly.
	    /// </summary>
	    /// <remarks>
	    ///    This method creates a new program of the type specified as the second parameter.
	    ///    As with all types of ResourceManager, this class will search for the file in
	    ///    all resource locations it has been configured to look in.
	    /// </remarks>
	    /// <param name="name">
	    ///    Identifying name of the program to load.
	    /// </param>
	    /// <param name="group"></param>
	    /// <param name="fileName">
	    ///    The file to load.
	    /// </param>
	    /// <param name="type">
	    ///    Type of program to create.
	    /// </param>
	    /// <param name="syntaxCode">
	    ///    Syntax code of the program, i.e. vs_1_1, arbvp1, etc.
	    /// </param>
	    public virtual GpuProgram Load( string name, string group, string fileName, GpuProgramType type, string syntaxCode )
		{
			var program = GetByName( name );

			if ( program == null )
			{
				program = CreateProgram( name, group, fileName, type, syntaxCode );
			}

			program.Load();
			return program;
		}

	    /// <summary>
	    ///    Loads a GPU program from a string containing the assembly source.
	    /// </summary>
	    /// <remarks>
	    ///    This method creates a new program of the type specified as the second parameter.
	    ///    As with all types of ResourceManager, this class will search for the file in
	    ///    all resource locations it has been configured to look in.
	    /// </remarks>
	    /// <param name="name">
	    ///    Name used to identify this program.
	    /// </param>
	    /// <param name="group"></param>
	    /// <param name="source">
	    ///    Source code of the program to load.
	    /// </param>
	    /// <param name="type">
	    ///    Type of program to create.
	    /// </param>
	    /// <param name="syntaxCode">
	    ///    Syntax code of the program, i.e. vs_1_1, arbvp1, etc.
	    /// </param>
	    public virtual GpuProgram LoadFromString( string name, string group, string source, GpuProgramType type, string syntaxCode )
		{
			var program = GetByName( name );

			if ( program == null )
			{
				program = CreateProgramFromString( name, group, source, type, syntaxCode );
			}

			program.Load();
			return program;
		}

		#endregion

		#region ResourceManager Implementation

		/// <summary>
		/// Gets a GpuProgram with the specified name.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public new GpuProgram GetByName( string name )
		{
			// look for a high level program first
			GpuProgram program = HighLevelGpuProgramManager.Instance[ name ];

			// return if found
			if ( program != null )
			{
				return program;
			}

			// return low level program
			return (GpuProgram)base[ name ];
		}

		/// <summary>
		///     Called when the engine is shutting down.
		/// </summary>
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !this.IsDisposed )
			{
				if ( disposeManagedResources )
				{
					_instance = null;
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
			}

			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );
		}
		#endregion  ResourceManager Implementation

        public bool SaveMicrocodesToCache
        {
            get { throw new System.NotImplementedException(); }
            set { throw new System.NotImplementedException(); }
        }

        public void LoadMicrocodeCache( System.IO.Stream stream )
        {
            throw new System.NotImplementedException();
        }

        public void SaveMicrocodeCache( System.IO.Stream stream )
        {
            throw new System.NotImplementedException();
        }
    }
}
