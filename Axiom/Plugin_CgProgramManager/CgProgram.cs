using System;
using System.Diagnostics;
using Axiom.Core;
using Axiom.Graphics;
using Tao.Cg;

namespace Plugin_CgProgramManager
{
	/// <summary>
	/// 	Specialization of HighLevelGpuProgram to provide support for nVidia's Cg language.
	/// </summary>
	/// <remarks>
	///    Cg can be used to compile common, high-level, C-like code down to assembler
	///    language for both GL and Direct3D, for multiple graphics cards. You must
	///    supply a list of profiles which your program must support using
	///    SetProfiles() before the program is loaded in order for this to work. The
	///    program will then negotiate with the renderer to compile the appropriate program
	///    for the API and graphics card capabilities.
	/// </remarks>
	public class CgProgram : HighLevelGpuProgram {
		#region Fields

        /// <summary>
        ///    Current Cg context id.
        /// </summary>
        protected IntPtr cgContext;
        /// <summary>
        ///    Current Cg program id.
        /// </summary>
        protected IntPtr cgProgram;
        /// <summary>
        ///    Entry point of the Cg program.
        /// </summary>
        protected string entry;
        /// <summary>
        ///    List of requested profiles for this program.
        /// </summary>
        protected string[] profiles;
        /// <summary>
        ///    Chosen profile for this program.
        /// </summary>
        protected string selectedProfile;
        protected int selectedCgProfile;
		
		#endregion Fields
		
		#region Constructors
		
        /// <summary>
        ///    Constructor.
        /// </summary>
        /// <param name="name">Name of this program.</param>
        /// <param name="type">Type of this program, vertex or fragment program.</param>
        /// <param name="language">HLSL language of this program.</param>
        /// <param name="context">CG context id.</param>
		public CgProgram(string name, GpuProgramType type, string language, IntPtr context) : base(name, type, language) {
			this.cgContext = context;
		}
		
		#endregion
		
		#region Methods

        protected void SelectProfile() {
            selectedProfile = "";
            selectedCgProfile = Cg.CG_PROFILE_UNKNOWN;

            for(int i = 0; i < profiles.Length; i++) {
                if(GpuProgramManager.Instance.IsSyntaxSupported(profiles[i])) {
                    selectedProfile = profiles[i];
                    selectedCgProfile = Cg.cgGetProfile(selectedProfile);

                    CgHelper.CheckCgError("Unable to find Cg profile enum for program " + name, cgContext);

                    break;
                }
            }
        }

        protected override void LoadFromSource() {
            SelectProfile();

            string[] args = null;

            if(selectedCgProfile == Cg.CG_PROFILE_VS_1_1) {
                args = new string[] {"-profileopts dcls", null};
            }

            // create the Cg program
            cgProgram = Cg.cgCreateProgram(cgContext, Cg.CG_SOURCE, source, selectedCgProfile, entry, args);

            CgHelper.CheckCgError("Unable to compile Cg program " + name, cgContext);
        }

        protected override void CreateLowLevelImpl() {  
            // create a low-level program, with the same name as this one
            //assemblerProgram = GpuProgramManager.Instance.Create((name, type, selectedProfile);
            assemblerProgram = GpuProgramManager.Instance.CreateProgramFromString(name, "", type, selectedProfile);
            assemblerProgram.Source = Cg.cgGetProgramString(cgProgram, Cg.CG_COMPILED_PROGRAM);
        }

        protected override void PopulateParameterNames(GpuProgramParameters parms) {
            Debug.Assert(cgProgram != IntPtr.Zero);

            // Note use of 'leaf' format so we only get bottom-level params, not structs
            IntPtr param = Cg.cgGetFirstLeafParameter(cgProgram, Cg.CG_PROGRAM);

            // loop through the rest of the params
            while(param != IntPtr.Zero) {

                // Look for uniform parameters only
                // Don't bother enumerating unused parameters, especially since they will
                // be optimized out and therefore not in the indexed versions
                if(Cg.cgIsParameterReferenced(param) != 0
                    && Cg.cgGetParameterVariability(param) == Cg.CG_UNIFORM
                    && Cg.cgGetParameterDirection(param) != Cg.CG_OUT
                    && Cg.cgGetParameterType(param) != Cg.CG_SAMPLER1D
                    && Cg.cgGetParameterType(param) != Cg.CG_SAMPLER2D
                    && Cg.cgGetParameterType(param) != Cg.CG_SAMPLER3D
                    && Cg.cgGetParameterType(param) != Cg.CG_SAMPLERCUBE
                    && Cg.cgGetParameterType(param) != Cg.CG_SAMPLERRECT) {                 

                    // get the name and index of the program param
                    string name = Cg.cgGetParameterName(param);
                    int index = Cg.cgGetParameterResourceIndex(param);

                    // map the param to the index
                    parms.MapParamNameToIndex(name, index);
                }

                // get the next param
                param = Cg.cgGetNextLeafParameter(param);
            }
        }

        protected override void UnloadImpl() {
            // destroy this program
            Cg.cgDestroyProgram(cgProgram);

            CgHelper.CheckCgError(string.Format("Error unloading CgProgram named '{0}'", this.name), cgContext);
        }

		#endregion
		
		#region Properties

        public override bool IsSupported {
            get {
                // see if any profiles are supported
                for(int i = 0; i < profiles.Length; i++) {
                    if(GpuProgramManager.Instance.IsSyntaxSupported(profiles[i])) {
                        return true;
                    }
                }

                // nope, SOL
                return false;
            }
        }

		
		#endregion

        #region IConfigurable Members

        /// <summary>
        ///    Method for passing parameters into the CgProgram.
        /// </summary>
        /// <param name="name">
        ///    Param name.
        /// </param>
        /// <param name="val">
        ///    Param value.
        /// </param>
        public override void SetParam(string name, string val) {
            switch(name) {
                case "entry":
                    entry = val;
                    break;
                
                case "profile":
                    profiles = val.Split(' ');
                    break;

                default:
                    Trace.WriteLine(string.Format("CgProgram: Unrecognized parameter '{0}'", name));
                    break;
            }
        }

        #endregion
    }
}
