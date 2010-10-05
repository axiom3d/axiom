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
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Text;
using Axiom.Graphics;
using Axiom.ParticleSystems;
using Axiom.Scripting.Compiler.AST;

#endregion Namespace Declarations

namespace Axiom.Scripting.Compiler
{
	/// <summary>
	/// This is a listener for the compiler. The compiler can be customized with
	/// this listener. It lets you listen in on events occuring during compilation,
	/// hook them, and change the behavior.
	/// </summary>
	public abstract class ScriptCompilerListener
	{
		public ScriptCompilerListener()
		{
		}

		/// Returns the concrete node list from the given file
		public virtual IList<ConcreteNode> ImportFile( String name )
		{
			return null;
		}

		/// Allows for responding to and overriding behavior before a CST is translated into an AST
		public virtual void PreASTConversion( IList<ConcreteNode> nodes, Dictionary<string, uint> ids )
		{
		}

		/// Allows for overriding the translation of the given node into the concrete resource.
		public virtual ScriptCompiler.Translator PreObjectTranslation( ObjectAbstractNode obj )
		{
			return null;
		}

		/// Allows for overriding the translation of the given node into the concrete resource.
		public virtual ScriptCompiler.Translator PrePropertyTranslation( PropertyAbstractNode prop )
		{
			return null;
		}

		/// Called when an error occurred
		public virtual void Error( ScriptCompiler.CompileError err )
		{
		}

		/// Must return the requested material
		public virtual Material CreateMaterial( String name, String group )
		{
			return null;
		}

		/// Called before texture aliases are applied to a material
		public virtual void PreApplyTextureAliases( Dictionary<String, String> aliases )
		{
		}

		/// Called before texture names are used
		public virtual void GetTextureNames( String names )
		{
			GetTextureNames( names, 0 );
		}

		/// Called before texture names are used
		public virtual void GetTextureNames( String names, int count )
		{
		}

		/// Called before a gpu program name is used
		public virtual void GetGpuProgramName( String name )
		{
		}

		/// Called to return the requested GpuProgram
		public virtual GpuProgram CreateGpuProgram( String name, String group, String source, GpuProgramType type, String syntax )
		{
			return null;
		}

		/// Called to return a HighLevelGpuProgram
		public virtual HighLevelGpuProgram CreateHighLevelGpuProgram( String name, String group, String language, GpuProgramType type, String source )
		{
			return null;
		}

		/// Returns the requested particle system template
		public virtual ParticleSystem CreateParticleSystem( String name, String group )
		{
			return null;
		}

		/// Processes the name of the material
		public virtual void GetMaterialName( String name )
		{
		}

		/// Returns the compositor that is created
		public virtual Compositor CreateCompositor( String name, String group )
		{
			return null;
		}

	}
}