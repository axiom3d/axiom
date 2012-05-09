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
using System.Linq;
using Axiom.Collections;
using Axiom.Core;
using ResourceHandle = System.UInt64;

#endregion Namespace Declarations

namespace Axiom.Graphics
{
	/// <summary>
	/// 	This ResourceManager manages high-level vertex and fragment programs.
	/// </summary>
	/// <remarks>
	///    High-level vertex and fragment programs can be used instead of assembler programs
	///    as managed by <see cref="GpuProgramManager"/>; however they typically result in a
	///    <see cref="GpuProgram"/> being created as a derivative of the high-level program.
	///    High-level programs are easier to write, and can often be API-independent,
	///    unlike assembler programs.
	///    <p/>
	///    This class not only manages the programs themselves, it also manages the factory
	///    classes which allow the creation of high-level programs using a variety of high-level
	///    syntaxes. Plugins can be created which register themselves as high-level program
	///    factories and as such the engine can be extended to accept virtually any kind of
	///    program provided a plugin is written.
	/// </remarks>
	public class HighLevelGpuProgramManager : ResourceManager
	{
		#region Singleton implementation

		public const string NullLang = "null";

		/// <summary>
		///     Singleton instance of this class.
		/// </summary>
		private static HighLevelGpuProgramManager _instance;

		/// <summary>
		///     Internal constructor.  This class cannot be instantiated externally.
		/// </summary>
		internal HighLevelGpuProgramManager()
			: base()
		{
			if ( _instance == null )
			{
				_instance = this;
				LoadingOrder = 50.0f;
				ResourceType = "HighLevelGpuProgram";

				ResourceGroupManager.Instance.RegisterResourceManager( ResourceType, this );
				AddFactory( new NullProgramFactory() );
				AddFactory( new UnifiedHighLevelGpuProgramFactory() );
			}
		}

		/// <summary>
		///     Gets the singleton instance of this class.
		/// </summary>
		public static HighLevelGpuProgramManager Instance
		{
			get
			{
				return _instance;
			}
		}

		#endregion Singleton implementation

		#region Fields

		/// <summary>
		///    Lookup table for list of registered factories.
		/// </summary>
		protected AxiomCollection<HighLevelGpuProgramFactory> factories = new AxiomCollection<HighLevelGpuProgramFactory>();

		#endregion Fields

		#region Methods

		/// <summary>
		///    Add a new factory object for high-level programs of a given language.
		/// </summary>
		/// <param name="factory">
		///    The factory instance to register.
		/// </param>
		public void AddFactory( HighLevelGpuProgramFactory factory )
		{
			this.factories.Add( factory.Language, factory );
		}

		/// <summary>
		///     Unregisters a factory
		/// </summary>
		public void RemoveFactory( HighLevelGpuProgramFactory factory )
		{
			this.factories.Remove( factory.Language );
		}

		/// <summary>
		///    Creates a new, unloaded HighLevelGpuProgram instance.
		/// </summary>
		/// <remarks>
		///    This method creates a new program of the type specified as the second and third parameters.
		///    You will have to call further methods on the returned program in order to
		///    define the program fully before you can load it.
		/// </remarks>
		/// <param name="name">Name of the program to create.</param>
		/// <param name="group"></param>
		/// <param name="language">HLSL language to use.</param>
		/// <param name="type">Type of program, i.e. vertex or fragment.</param>
		/// <returns>An unloaded instance of HighLevelGpuProgram.</returns>
		public HighLevelGpuProgram CreateProgram( string name, string group, string language, GpuProgramType type )
		{
			// lookup the factory for the requested program language
			var factory = GetFactory( language );

			if ( factory == null )
			{
				throw new AxiomException( "Could not find HighLevelGpuProgramManager that can compile programs of type '{0}'",
				                          language );
			}

			// create the high level program using the factory
			var program = factory.CreateInstance( this, name, (ResourceHandle)name.ToLower().GetHashCode(), group, false, null );
			program.Type = type;
			program.SyntaxCode = language;

			_add( program );
			return program;
		}

		/// <summary>
		///    Retreives a factory instance capable of producing HighLevelGpuPrograms of the
		///    specified language.
		/// </summary>
		/// <param name="language">HLSL language.</param>
		/// <returns>A factory capable of creating a HighLevelGpuProgram of the specified language.</returns>
		public HighLevelGpuProgramFactory GetFactory( string language )
		{
			if ( !this.factories.ContainsKey( language ) )
			{
				// use the null factory to create programs that will never be supported
				if ( this.factories.ContainsKey( NullLang ) )
				{
					return (HighLevelGpuProgramFactory)this.factories[ NullLang ];
				}
			}
			else
			{
				return (HighLevelGpuProgramFactory)this.factories[ language ];
			}

			// wasn't found, so return null
			return null;
		}

		#endregion Methods

		#region Properties

		public bool IsLanguageSupported( string language )
		{
			return this.factories.ContainsKey( language );
		}

		#endregion Properties

		#region ResourceManager Implementation

		protected override Resource _create( string name, ResourceHandle handle, string group, bool isManual,
		                                     IManualResourceLoader loader, NameValuePairList createParams )
		{
			if ( createParams == null || !createParams.ContainsKey( "language" ) )
			{
				throw new Exception( "You must supply a 'language' parameter" );
			}
			return GetFactory( createParams[ "language" ] ).CreateInstance( this, name, handle, group, isManual, loader );
		}

		/// <summary>
		///     Gets a HighLevelGpuProgram with the specified name.
		/// </summary>
		/// <param name="name">Name of the program to retrieve.</param>
		/// <returns>The high level gpu program with the specified name.</returns>
		public new HighLevelGpuProgram this[ string name ]
		{
			get
			{
				return (HighLevelGpuProgram)base[ name ];
			}
		}

		/// <summary>
		///     Gets a HighLevelGpuProgram with the specified handle.
		/// </summary>
		/// <param name="handle">Handle of the program to retrieve.</param>
		/// <returns>The high level gpu program with the specified handle.</returns>
		public new HighLevelGpuProgram this[ ResourceHandle handle ]
		{
			get
			{
				return (HighLevelGpuProgram)base[ handle ];
			}
		}

		/// <summary>
		///     Called when the engine is shutting down.
		/// </summary>
		/// <summary>
		///     Called when the engine is shutting down.
		/// </summary>
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					ResourceGroupManager.Instance.UnregisterResourceManager( ResourceType );
					_instance = null;
				}

				// There are no unmanaged resources to release, but
				// if we add them, they need to be released here.
			}

			// If it is available, make the call to the
			// base class's Dispose(Boolean) method
			base.dispose( disposeManagedResources );
		}

		#endregion ResourceManager Implementation
	}
}