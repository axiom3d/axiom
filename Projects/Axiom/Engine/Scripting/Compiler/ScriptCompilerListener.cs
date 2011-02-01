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

using System;
using System.Collections.Generic;
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

        /// <summary>
        /// Allows for responding to and overriding behavior before a CST is translated into an AST
        /// </summary>
        /// <param name="compiler"></param>
        /// <param name="nodes"></param>
        public virtual void PreConversion(ScriptCompiler compiler, IList<ConcreteNode> nodes)
        {
        }

        /// <summary>
        /// Allows vetoing of continued compilation after the entire AST conversion process finishes
        /// </summary>
        /// <remarks>
        /// Once the script is turned completely into an AST, including import
        /// and override handling, this function allows a listener to exit
        /// the compilation process.
        ///</remarks>
        /// <param name="compiler"></param>
        /// <param name="nodes"></param>
        /// <returns>True continues compilation, false aborts</returns>
        public virtual bool PostConversion(ScriptCompiler compiler, IList<AbstractNode> nodes)
        {
            return true;
        }


		/// <summary>
        /// Allows for overriding the translation of the given node into the concrete resource.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public virtual ScriptCompiler.Translator PreObjectTranslation( ObjectAbstractNode obj )
		{
			return null;
		}

		/// <summary>
        /// Allows for overriding the translation of the given node into the concrete resource.
		/// </summary>
		/// <param name="prop"></param>
		/// <returns></returns>
		public virtual ScriptCompiler.Translator PrePropertyTranslation( PropertyAbstractNode prop )
		{
			return null;
		}

		/// <summary>
        /// Called when an error occurred
		/// </summary>
		/// <param name="err"></param>
		public virtual void Error( ScriptCompiler.CompileError err )
		{
		}

		/// <summary>
        /// Must return the requested material
		/// </summary>
		/// <param name="name"></param>
		/// <param name="group"></param>
		/// <returns></returns>
		public virtual Material CreateMaterial( String name, String group )
		{
			return null;
		}

		/// <summary>
        /// Called before texture aliases are applied to a material
		/// </summary>
		/// <param name="aliases"></param>
		public virtual void PreApplyTextureAliases( Dictionary<String, String> aliases )
		{
		}

		/// <summary>
        /// Called before texture names are used
		/// </summary>
		/// <param name="names"></param>
		public virtual void GetTextureNames( String names )
		{
			GetTextureNames( names, 0 );
		}

		/// <summary>
        /// Called before texture names are used
		/// </summary>
		/// <param name="names"></param>
		/// <param name="count"></param>
		public virtual void GetTextureNames( String names, int count )
		{
		}

		/// <summary>
        /// Called before a gpu program name is used
		/// </summary>
		/// <param name="name"></param>
		public virtual void GetGpuProgramName( String name )
		{
		}

		/// <summary>
        /// Called to return the requested GpuProgram
		/// </summary>
		/// <param name="name"></param>
		/// <param name="group"></param>
		/// <param name="source"></param>
		/// <param name="type"></param>
		/// <param name="syntax"></param>
		/// <returns></returns>
		public virtual GpuProgram CreateGpuProgram( String name, String group, String source, GpuProgramType type, String syntax )
		{
			return null;
		}

		/// <summary>
        /// Called to return a HighLevelGpuProgram
		/// </summary>
		/// <param name="name"></param>
		/// <param name="group"></param>
		/// <param name="language"></param>
		/// <param name="type"></param>
		/// <param name="source"></param>
		/// <returns></returns>
		public virtual HighLevelGpuProgram CreateHighLevelGpuProgram( String name, String group, String language, GpuProgramType type, String source )
		{
			return null;
		}

		/// <summary>
        /// Returns the requested particle system template
		/// </summary>
		/// <param name="name"></param>
		/// <param name="group"></param>
		/// <returns></returns>
		public virtual ParticleSystem CreateParticleSystem( String name, String group )
		{
			return null;
		}

		/// <summary>
        /// Processes the name of the material
		/// </summary>
		/// <param name="name"></param>
		public virtual void GetMaterialName( String name )
		{
		}

		/// <summary>
        /// Returns the compositor that is created
		/// </summary>
		/// <param name="name"></param>
		/// <param name="group"></param>
		/// <returns></returns>
		public virtual Compositor CreateCompositor( String name, String group )
		{
			return null;
		}

	}
}