using System;
using System.IO;
using Axiom.Core;

namespace Axiom.SubSystems.Rendering
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
