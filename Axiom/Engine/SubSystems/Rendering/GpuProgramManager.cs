#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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

using System;
using Axiom.Core;
using Axiom.Exceptions;

namespace Axiom.SubSystems.Rendering
{
	/// <summary>
	/// 	Summary description for GpuProgramManager.
	/// </summary>
	public abstract class GpuProgramManager : ResourceManager
	{
        #region Singleton implementation

        static GpuProgramManager() { Init(); }
        protected GpuProgramManager() { instance = this; }
        protected static GpuProgramManager instance;

        public static GpuProgramManager Instance {
            get { return instance; }
        }

        public static void Init() {
            instance = null;
        }
		
        #endregion

		#region Member variables
		
		#endregion
				
		#region Methods
		
        public override Resource Create(string name) {
            throw new AxiomException("You need to create a program using the Load method.");
        }

        /// <summary>
        ///    
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public abstract GpuProgram Create(string name, GpuProgramType type);

        /// <summary>
        ///    Creates a new GpuProgramParameters instance which can be used to bind parameters 
        ///    to your programs.
        /// </summary>
        /// <remarks>
        ///    Program parameters can be shared between multiple programs if you wish.
        /// </remarks>
        /// <returns></returns>
        public abstract GpuProgramParameters CreateParameters();

        /// <summary>
        ///    Loads a GPU program from a file of assembly.
        /// </summary>
        /// <remarks>
        ///    This method creates a new program of the type specified as the second parameter.
        ///    As with all types of ResourceManager, this class will search for the file in
        ///    all resource locations it has been configured to look in.
        /// </remarks>
        /// <param name="fileName">The file to load, which will also become the 
        ///    identifying name of the GpuProgram which is returned.</param>
        /// <param name="type">Type of program to create.</param>
        public virtual GpuProgram Load(string fileName, GpuProgramType type) {
            GpuProgram program = Create(fileName, type);
            base.Load(program, 1);
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
        /// <param name="name">Name used to identify this program.</param>
        /// <param name="type">Type of program to create.</param>
        public virtual GpuProgram Load(string name, string source, GpuProgramType type) {
            GpuProgram program = Create(name, type);
            base.Load(program, 1);
            program.Source = source;
            return program;
        }

		#endregion
		
		#region Properties
		
		#endregion

	}
}
