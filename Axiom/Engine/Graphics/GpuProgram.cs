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
using System.Collections;

namespace Axiom.Graphics
{
	/// <summary>
	/// 	Defines a program which runs on the GPU such as a vertex or fragment program.
	/// </summary>
    public abstract class GpuProgram : Resource {
		
		#region Fields
		
        /// <summary>
        ///    The name of the file to load from source (may be blank).
        /// </summary>
        protected string fileName;
        /// <summary>
        ///    The assembler source of this program.
        /// </summary>
        protected string source;
        /// <summary>
        ///    Whether this source is being loaded from file or not.
        /// </summary>
        protected bool loadFromFile;
        /// <summary>
        ///    Syntax code (i.e. arbvp1, vs_2_0, etc.)
        /// </summary>
        protected string syntaxCode;
        /// <summary>
        ///    Type of program this represents (vertex or fragment).
        /// </summary>
        protected GpuProgramType type;
		/// <summary>
		///		Flag indicating whether this program is being used for hardware skinning.
		/// </summary>
		protected bool isSkeletalAnimationSupported;

		/// <summary>
		///		List of default parameters, as gathered from the program definition.
		/// </summary>
		protected GpuProgramParameters defaultParams;

        #endregion Fields
		
        #region Constructors
		
        /// <summary>
        ///    Constructor for creating
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        public GpuProgram(string name, GpuProgramType type, string syntaxCode) {
            this.type = type;
            this.name = name;
            this.syntaxCode = syntaxCode;
            this.loadFromFile = true;
        }
		
        #endregion Constructors
		
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
        public virtual GpuProgramParameters CreateParameters() {
            return GpuProgramManager.Instance.CreateParameters();
        }
		
        /// <summary>
        ///    Loads this Gpu Program.
        /// </summary>
        public override void Load() {
            if(isLoaded) {
                Unload();
            }

            // load from file and get the source string from it
            if(loadFromFile) {
                Stream stream = GpuProgramManager.Instance.FindResourceData(fileName);
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
        ///    Returns the GpuProgram which should be bound to the pipeline.
        /// </summary>
        /// <remarks>
        ///    This method is simply to allow some subclasses of GpuProgram to delegate
        ///    the program which is bound to the pipeline to a delegate, if required.
        /// </remarks>
        public virtual GpuProgram BindingDelegate {
            get {
                return this;
            }
        }

		public GpuProgramParameters DefaultParameters {
			get {
				if (defaultParams == null) {
					defaultParams = this.CreateParameters();
				}
				return defaultParams;
			}
		}

		/// <summary>
		///		Gets/Sets whether a vertex program includes the required instructions
        ///		to perform skeletal animation. 
		/// </summary>
		public virtual bool IsSkeletalAnimationIncluded {
			get {
				return isSkeletalAnimationSupported;
			}
			set {
				isSkeletalAnimationSupported = value;
			}
		}

        /// <summary>
        ///    Returns whether this program can be supported on the current renderer and hardware.
        /// </summary>
        public virtual bool IsSupported {
            get {
				// If skeletal animation is being done, we need support for UBYTE4
				if(this.IsSkeletalAnimationIncluded &&
					!Root.Instance.RenderSystem.Caps.CheckCap(Capabilities.VertexFormatUByte4)) {

					return false;
				}

                return GpuProgramManager.Instance.IsSyntaxSupported(syntaxCode);
            }
        }
		
        /// <summary>
        ///    Gets/Sets the source assembler code for this program.
        /// </summary>
        /// <remarks>
        ///    Setting this will have no effect until you (re)load the program.
        /// </remarks>
        public string Source {
            get {
                return source;
            }
            set {
                source = value;
                fileName = "";
                loadFromFile = false;
            }
        }

        /// <summary>
        ///    Gets/Sets the source file for this program.
        /// </summary>
        /// <remarks>
        ///    Setting this will have no effect until you (re)load the program.
        /// </remarks>
        public string SourceFile {
            get {
                return fileName;
            }
            set {
                fileName = value;
                source = "";
                loadFromFile = true;
            }
        }

        /// <summary>
        ///    Gets the syntax code of this program (i.e. arbvp1, vs_1_1, etc).
        /// </summary>
        public string SyntaxCode {
            get {
                return syntaxCode;
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
