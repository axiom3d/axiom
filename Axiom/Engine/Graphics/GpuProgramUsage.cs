using System;

namespace Axiom.Graphics
{
	/// <summary>
	/// 	This class makes the usage of a vertex and fragment programs (low-level or high-level), 
	/// 	with a given set of parameters, explicit.
	/// </summary>
	/// <remarks>
	/// 	Using a vertex or fragment program can get fairly complex; besides the fairly rudimentary
	/// 	process of binding a program to the GPU for rendering, managing usage has few
	/// 	complications, such as:
	/// 	<ul>
	/// 	<li>Programs can be high level (e.g. Cg, GLSlang) or low level (assembler). Using
	/// 	either should be relatively seamless, although high-level programs give you the advantage
	/// 	of being able to use named parameters, instead of just indexed registers</li>
	/// 	<li>Programs and parameters can be shared between multiple usages, in order to save
	/// 	memory</li>
	/// 	<li>When you define a user of a program, such as a material, you often want to be able to
	/// 	set up the definition but not load / compile / assemble the program at that stage, because
	/// 	it is not needed just yet. The program should be loaded when it is first needed, or
	/// 	earlier if specifically requested. The program may not be defined at this time, you
	/// 	may want to have scripts that can set up the definitions independent of the order in which
	/// 	those scripts are loaded.</li>
	/// 	</ul>
	/// 	This class packages up those details so you don't have to worry about them. For example,
	/// 	this class lets you define a high-level program and set up the parameters for it, without
	/// 	having loaded the program (which you normally could not do). When the program is loaded and
	/// 	compiled, this class will then validate the parameters you supplied earlier and turn them
	/// 	into runtime parameters.
	/// 	<p/>
	/// 	Just incase it wasn't clear from the above, this class provides linkage to both 
	/// 	GpuProgram and HighLevelGpuProgram, despite its name.
	/// </remarks>
	/// TODO: High level GPU program/params
	public class GpuProgramUsage {
		#region Member variables
		
        /// <summary>
        ///    Type of program (vertex or fragment) this usage is being specified for.
        /// </summary>
        protected GpuProgramType type;
        /// <summary>
        ///    Name of the program this usage is specified for.
        /// </summary>
        protected string programName;
        /// <summary>
        ///    Should parameter validation happen up front?
        /// </summary>
        protected bool deferValidation;
        /// <summary>
        ///    Reference to the program whose usage is being specified within this class.
        /// </summary>
        protected GpuProgram program;
        /// <summary>
        ///    Low level GPU program parameters.
        /// </summary>
        protected GpuProgramParameters lowLevelParams;

		#endregion
		
		#region Constructors
		
        /// <summary>
        ///    Default constructor.
        /// </summary>
        /// <param name="type">Type of program to link to.</param>
        /// <param name="programName">
        ///    The name of the program to use. Note that at this stage the program is
        ///    <strong>not</strong> looked up, so there is really no validation of this parameter
        ///    until you call Validate().
        /// </param>
        public GpuProgramUsage(GpuProgramType type, string programName) {
            this.type = type;
            this.programName = programName;
            this.deferValidation = true;

            // Create a set of parameters incase we don't want to share the params
            lowLevelParams = GpuProgramManager.Instance.CreateParameters();
        }

        /// <summary>
        ///    Default constructor.
        /// </summary>
        /// <param name="type">Type of program to link to.</param>
        /// <param name="programName">
        ///    The name of the program to use. Note that at this stage the program is
        ///    <strong>not</strong> looked up, so there is really no validation of this parameter
        ///    until you call Validate() or set the last parameter of this constructor to true.
        /// </param>
        /// <param name="validateImmediately">
        ///    Set this to true if you want to immediately check the 
        ///    program name and any named parameters that you set.
        /// </param>
		public GpuProgramUsage(GpuProgramType type, string programName, bool validateImmediately) {
            this.type = type;
            this.programName = programName;
            this.deferValidation = !validateImmediately;

            // Create a set of parameters incase we don't want to share the params
            lowLevelParams = GpuProgramManager.Instance.CreateParameters();
		}
		
		#endregion
		
		#region Methods

        /// <summary>
        ///    Turns on validation for this class, if it was left disabled on initial creation.
        /// </summary>
        /// <remarks>
        ///    Validation of the program name, and the named parameters which are used for
        ///    high-level programs can be deferred in order to relax the ordering of using this
        ///    class and defining the programs to which it refers. Eventually, however, these
        ///    things do have to be checked, and this method turns on that checking permanently.
        /// </remarks>
        public void EnableValidation() {
            if(deferValidation) {
                // validates, and gets a reference to the program
                ValidateName();
                ValidateNamedParams();
            }

            deferValidation = false;
        }

        /// <summary>
        ///    Load this usage (and ensure program is loaded).
        /// </summary>
        internal void Load() {
            EnableValidation();
            program.Load();
        }

        /// <summary>
        ///    Unload this usage.
        /// </summary>
        internal void Unload() {
            // TODO: Anything needed here?  The program cannot be destroyed since it is shared.
        }

        /// <summary>
        ///    Internal validation function - checks the name of the program (and links).
        /// </summary>
        protected void ValidateName() {
            program = GpuProgramManager.Instance[programName];

            if(program == null) {
                string error = string.Format("Unable to locate {0} program named '{1}'.", type.ToString(), programName);
                throw new Exception(error);
            }
        }

        /// <summary>
        ///    Internal validation function - checks the named parameters.
        /// </summary>
        protected void ValidateNamedParams() {
            // TODO: Implementation
        }
		
		#endregion
		
		#region Properties
		
        /// <summary>
        ///    Gets/Sets the low-level parameters that should be used; because parameters can be
        ///    shared between multiple usages for efficiency, this method is here for you
        ///    to register externally created parameter objects.
        /// </summary>
        public GpuProgramParameters Params {
            get {
                return lowLevelParams;
            }
            set {
                lowLevelParams = value;
            }
        }

        /// <summary>
        ///    Gets the program this usage is linked to; only available after the usage has been
        ///    validated either via enableValidation or by enabling validation on construction.
        /// </summary>
        public GpuProgram Program {
            get {
                if(program == null) {
                    // Need to validate to connect with program
                    EnableValidation();
                }

                return program;
            }
        }

        /// <summary>
        ///    Gets/Sets the name of the program we're trying to link to.
        /// </summary>
        public string ProgramName {
            get {
                return programName;
            }
            set {
                programName = value;

                if(!deferValidation) {
                    ValidateName();
                }
            }
        }

        /// <summary>
        ///    Gets the type of program we're trying to link to.
        /// </summary>
        public GpuProgramType Type {
            get {
                return type;
            }
        }

		#endregion
	}
}
