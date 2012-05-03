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
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System.Collections.Generic;

using Axiom.Scripting.Compiler.AST;
using Axiom.Graphics;

#endregion Namespace Declarations

namespace Axiom.Scripting.Compiler
{
	partial class ScriptCompilerManager
	{
		/// Returns the concrete node list from the given file
		public delegate IList<ConcreteNode> ImportFileHandler( ScriptCompiler compiler, string filename );

		/// Returns the concrete node list from the given file
		public event ImportFileHandler OnImportFile;

		/// Allows for responding to and overriding behavior before a CST is translated into an AST
		public delegate void PreConversionHandler( ScriptCompiler compiler, IList<ConcreteNode> nodes );

		/// Allows for responding to and overriding behavior before a CST is translated into an AST
		public event PreConversionHandler OnPreConversion;

		/// <summary>
		/// Allows vetoing of continued compilation after the entire AST conversion process finishes
		/// </summary>
		/// <remarks>
		/// Once the script is turned completely into an AST, including import
		/// and override handling, this function allows a listener to exit
		/// the compilation process.
		/// </remarks>
		/// <returns>
		/// True continues compilation, false aborts
		/// </returns>
		public delegate bool PostConversionHandler( ScriptCompiler compiler, IList<AbstractNode> nodes );

		/// <summary>
		/// Allows vetoing of continued compilation after the entire AST conversion process finishes
		/// </summary>
		/// <remarks>
		/// Once the script is turned completely into an AST, including import
		/// and override handling, this function allows a listener to exit
		/// the compilation process.
		/// </remarks>
		/// <returns>
		/// True continues compilation, false aborts
		/// </returns>
		public event PostConversionHandler OnPostConversion;

		/// Called when an error occurred
		public delegate void CompilerErrorHandler( ScriptCompiler compiler, ScriptCompiler.CompileError err );

		/// Called when an error occurred
		public event CompilerErrorHandler OnCompileError;

		/// <summary>
		/// Called when an event occurs during translation, return true if handled
		/// </summary>
		/// <remarks>
		/// This function is called from the translators when an event occurs that
		/// that can be responded to. Often this is overriding names, or it can be a request for
		/// custom resource creation.
		/// </remarks>
		/// <param name="compiler">A reference to the compiler</param>
		/// <param name="evt">The event object holding information about the event to be processed</param>
		/// <param name="retval">A possible return value from handlers</param>
		/// <returns>True if the handler processed the event</returns>
		public delegate bool TransationEventHandler( ScriptCompiler compiler, ref ScriptCompilerEvent evt, out object retval );

		/// <summary>
		/// Called when an event occurs during translation, return true if handled
		/// </summary>
		/// <remarks>
		/// This function is called from the translators when an event occurs that
		/// that can be responded to. Often this is overriding names, or it can be a request for
		/// custom resource creation.
		/// </remarks>
		public event TransationEventHandler OnCompilerEvent;
	}

	public enum CompilerEventType
	{
		[ScriptEnum( "preApplyTextureAliases" )]
		PreApplyTextureAliases,

		[ScriptEnum( "processResourceName" )]
		ProcessResourceName,

		[ScriptEnum( "processNameExclusion" )]
		ProcessNameExclusion,

		[ScriptEnum( "createMaterial" )]
		CreateMaterial,

		[ScriptEnum( "createGpuProgram" )]
		CreateGpuProgram,

		[ScriptEnum( "createHighLevelGpuProgram" )]
		CreateHighLevelGpuProgram,

		[ScriptEnum( "createGpuSharedParameters" )]
		CreateGpuSharedParameters,

		[ScriptEnum( "createParticleSystem" )]
		CreateParticleSystem,

		[ScriptEnum( "createCompositor" )]
		CreateCompositor
	}

	#region ScriptCompiler Events

	#region ScriptCompilerEvent

	/// <summary>
	/// This struct is a base class for events which can be thrown by the compilers and caught by
	/// subscribers. There are a set number of standard events which are used by Ogre's core.
	/// New event types may be derived for more custom compiler processing.
	/// </summary>
	public abstract class ScriptCompilerEvent
	{
		/// <summary>
		/// Return the type of this ScriptCompilerEvent
		/// </summary>
		public CompilerEventType Type { get; private set; }

		protected ScriptCompilerEvent( CompilerEventType type )
		{
			Type = type;
		}
	};

	#endregion ScriptCompilerEvent

	// Standard event types

	#region PreApplyTextureAliasesScriptCompilerEvent

	public class PreApplyTextureAliasesScriptCompilerEvent : ScriptCompilerEvent
	{
		public Material Material;
		public Dictionary<string, string> Aliases = new Dictionary<string, string>();

		public PreApplyTextureAliasesScriptCompilerEvent( Material material, ref Dictionary<string, string> aliases )
			: base( CompilerEventType.PreApplyTextureAliases )
		{
			Material = material;
			Aliases = aliases;
		}
	};

	#endregion PreApplyTextureAliasesScriptCompilerEvent

	#region ProcessResourceNameScriptCompilerEvent

	public class ProcessResourceNameScriptCompilerEvent : ScriptCompilerEvent
	{
		public enum ResourceType
		{
			Texture,
			Material,
			GpuProgram,
			Compositor
		}

		public ResourceType ResType;
		public string Name;

		public ProcessResourceNameScriptCompilerEvent( ResourceType resType, string name )
			: base( CompilerEventType.ProcessResourceName )
		{
			ResType = resType;
			Name = name;
		}
	};

	#endregion ProcessResourceNameScriptCompilerEvent

	#region ProcessNameExclusionScriptCompilerEvent

	public class ProcessNameExclusionScriptCompilerEvent : ScriptCompilerEvent
	{
		public string Class;
		public AbstractNode Parent;

		public ProcessNameExclusionScriptCompilerEvent( string cls, AbstractNode parent )
			: base( CompilerEventType.ProcessNameExclusion )
		{
			Class = cls;
			Parent = parent;
		}
	};

	#endregion ProcessNameExclusionScriptCompilerEvent

	#region CreateMaterialScriptCompilerEvent

	public class CreateMaterialScriptCompilerEvent : ScriptCompilerEvent
	{
		public string File;
		public string Name;
		public string ResourceGroup;

		public CreateMaterialScriptCompilerEvent( string file, string name, string resGroup )
			: base( CompilerEventType.CreateMaterial )
		{
			File = file;
			Name = name;
			ResourceGroup = resGroup;
		}
	};

	#endregion CreateMaterialScriptCompilerEvent

	#region CreateGpuProgramScriptCompilerEvent

	public class CreateGpuProgramScriptCompilerEvent : ScriptCompilerEvent
	{
		public string File;
		public string Name;
		public string ResourceGroup;
		public string Source;
		public string Syntax;
		public GpuProgramType ProgramType;

		public CreateGpuProgramScriptCompilerEvent( string file, string name, string resGroup, string source, string syntax, GpuProgramType prgType )
			: base( CompilerEventType.CreateGpuProgram )
		{
			File = file;
			Name = name;
			ResourceGroup = resGroup;
			Source = source;
			Syntax = syntax;
			ProgramType = prgType;
		}
	};

	#endregion CreateGpuProgramScriptCompilerEvent

	#region CreateHighLevelGpuProgramScriptCompilerEvent

	public class CreateHighLevelGpuProgramScriptCompilerEvent : ScriptCompilerEvent
	{
		public string File;
		public string Name;
		public string ResourceGroup;
		public string Source;
		public string Language;
		public GpuProgramType ProgramType;

		public CreateHighLevelGpuProgramScriptCompilerEvent( string file, string name, string resGroup, string source, string language, GpuProgramType prgType )
			: base( CompilerEventType.CreateHighLevelGpuProgram )
		{
			File = file;
			Name = name;
			ResourceGroup = resGroup;
			Source = source;
			Language = language;
			ProgramType = prgType;
		}
	};

	#endregion CreateHighLevelGpuProgramScriptCompilerEvent

	#region CreateGpuSharedParametersScriptCompilerEvent

	public class CreateGpuSharedParametersScriptCompilerEvent : ScriptCompilerEvent
	{
		public string File;
		public string Name;
		public string ResourceGroup;

		public CreateGpuSharedParametersScriptCompilerEvent( string file, string name, string resGroup )
			: base( CompilerEventType.CreateGpuSharedParameters )
		{
			File = file;
			Name = name;
			ResourceGroup = resGroup;
		}
	};

	#endregion CreateGpuSharedParametersScriptCompilerEvent

	#region CreateParticleSystemScriptCompilerEvent

	public class CreateParticleSystemScriptCompilerEvent : ScriptCompilerEvent
	{
		public string File;
		public string Name;
		public string ResourceGroup;

		public CreateParticleSystemScriptCompilerEvent( string file, string name, string resGroup )
			: base( CompilerEventType.CreateParticleSystem )
		{
			File = file;
			Name = name;
			ResourceGroup = resGroup;
		}
	};

	#endregion CreateParticleSystemScriptCompilerEvent

	#region CreateCompositorScriptCompilerEvent

	public class CreateCompositorScriptCompilerEvent : ScriptCompilerEvent
	{
		public string File;
		public string Name;
		public string ResourceGroup;

		public CreateCompositorScriptCompilerEvent( string file, string name, string resGroup )
			: base( CompilerEventType.CreateCompositor )
		{
			File = file;
			Name = name;
			ResourceGroup = resGroup;
		}
	};

	#endregion CreateCompositorScriptCompilerEvent

	#endregion ScriptCompiler Events
}
