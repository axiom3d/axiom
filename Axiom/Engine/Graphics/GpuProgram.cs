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
using System.IO;
using Axiom.Core;

namespace Axiom.Graphics
{
	/// <summary>
	/// 	Defines a program which runs on the GPU such as a vertex or fragment program.
	/// </summary>
	public abstract class GpuProgram : Resource
	{
		#region Fields
		
        /// <summary>
        ///    The assembler source of this program.
        /// </summary>
        protected string source;

        /// <summary>
        ///    Whether this source is being loaded from file or not.
        /// </summary>
        protected bool loadFromFile;

        /// <summary>
        ///    Type of program this represents (vertex or fragment).
        /// </summary>
        protected GpuProgramType type;

		#endregion Fields
		
		#region Constructors
		
        /// <summary>
        ///    Constructor for creating
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
		public GpuProgram(string name, GpuProgramType type) {
            this.type = type;
            this.name = name;
            this.loadFromFile = true;
		}
		
        #endregion Constructors
		
		#region Methods
		
        /// <summary>
        ///    
        /// </summary>
        public override void Load() {
            if(isLoaded) {
                Unload();
            }

            // load from file and get the source string from it
            if(loadFromFile) {
                Stream stream = GpuProgramManager.Instance.FindResourceData(name);
                StreamReader reader = new StreamReader(stream, System.Text.Encoding.ASCII);
                source = reader.ReadToEnd();
            }

            // call polymorphic load to read source
            LoadFromSource();

            isLoaded = true;
        }

        /// <summary>
        ///    Method which must be implemented by subclasses, loads the program from source.
        /// </summary>
        protected abstract void LoadFromSource();    
    
		#endregion
		
		#region Properties
		
        /// <summary>
        ///    Gets the source assembler code for this program.
        /// </summary>
        public string Source {
            get {
                return source;
            }
            set {
                source = value;
                loadFromFile = false;
            }
        }

        /// <summary>
        ///    Gets the type of GPU program this represents (vertex or fragment).
        /// </summary>
        public GpuProgramType Type {
            get {
                return type;
            }
        }

		#endregion

	}
}
