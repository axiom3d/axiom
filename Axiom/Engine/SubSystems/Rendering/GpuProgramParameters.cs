using System;
using System.Collections;
using Axiom.MathLib;

namespace Axiom.SubSystems.Rendering
{
	/// <summary>
	/// 	Collects together the program parameters used for a GpuProgram.
	/// </summary>
	/// <remarks>
	///    Gpu program state includes constant parameters used by the program, and
	///    bindings to render system state which is propagated into the constants 
	///    by the engine automatically if requested.
	///    <p/>
	///    GpuProgramParameters objects should be created through the GpuProgramManager and
	///    may be shared between multiple GpuProgram instances. For this reason they
	///    are managed using a shared pointer, which will ensure they are automatically
	///    deleted when no program is using them anymore.
	///    <p/>
	///    Different types of GPU programs support different types of constant parameters.
	///    For example, it's relatively common to find that vertex programs only support
	///    floating point constants, and that fragment programs only support integer (fixed point)
	///    parameters. This can vary depending on the program version supported by the
	///    graphics card being used. You should consult the documentation for the type of
	///    low level program you are using, or alternatively use the methods
	///    provided on Capabilities to determine the options.
	///    <p/>
	///    Another possible limitation is that some systems only allow constants to be set
	///    on certain boundaries, e.g. in sets of 4 values for example. Again, see
	///    Capabilities for full details.
	/// </remarks>
	public abstract class GpuProgramParameters
	{
		#region Fields
        /// <summary>
        ///    Packed list of integer constants
        /// </summary>
		protected ArrayList intContants = new ArrayList();

        /// <summary>
        ///    Packed list of floating-point constants
        /// </summary>
        protected ArrayList floatContants = new ArrayList();

		#endregion
		
		#region Constructors
		
		public GpuProgramParameters(){
		}
		
		#endregion
		
		#region Methods

        /// <summary>
        ///    Sends a single value constant floating-point parameter to the program.
        /// </summary>
        /// <param name="index">Index of the contant register.</param>
        /// <param name="val">Single value to set.</param>
        public virtual void SetConstant(int index, float val) {
            if(index >= floatContants.Count) {
                floatContants.Insert(index, val);
            }
            else {
                floatContants[index] = val;
            }
        }
		
        /// <summary>
        ///    Sends a single value constant integer parameter to the program.
        /// </summary>
        /// <param name="index">Index of the contant register.</param>
        /// <param name="val">Single value to set.</param>
        public virtual void SetConstant(int index, int val) {
            intContants[index] = val;
        }

        /// <summary>
        ///    Sends 4 packed floating-point values to the program.
        /// </summary>
        /// <param name="index">Index of the contant register.</param>
        /// <param name="val">Structure containing 4 packed float values.</param>
        public virtual void SetConstant(int index, ref Vector4 val) {
            SetConstant(index++, val.x);
            SetConstant(index++, val.y);
            SetConstant(index++, val.z);
            SetConstant(index++, val.w);
        }

        /// <summary>
        ///    Sends 3 packed floating-point values to the program.
        /// </summary>
        /// <param name="index">Index of the contant register.</param>
        /// <param name="val">Structure containing 3 packed float values.</param>
        public virtual void SetConstant(int index, ref Vector3 val) {
            SetConstant(index++, val.x);
            SetConstant(index++, val.y);
            SetConstant(index++, val.z);
        }

        /// <summary>
        ///    Sends a multiple value constant floating-point parameter to the program.
        /// </summary>
        /// <remarks>
        ///    Implementation left to the rendersystem since column / row vectors matter.
        /// </remarks>
        /// <param name="index">Index of the contant register.</param>
        /// <param name="val">Structure containing 3 packed float values.</param>
        public abstract void SetConstant(int index, ref Matrix4 val);

        /// <summary>
        ///    Sets an array of int values starting at the specified index.
        /// </summary>
        /// <param name="index">Index of the contant register to start at.</param>
        /// <param name="ints">Array of ints.</param>
        public virtual void SetConstant(int index, int[] ints) {
            intContants.SetRange(index, ints);
        }

        /// <summary>
        ///    Sets an array of float values starting at the specified index.
        /// </summary>
        /// <param name="index">Index of the contant register to start at.</param>
        /// <param name="floats">Array of floats.</param>
        public virtual void SetConstant(int index, float[] floats) {
            floatContants.SetRange(index, floats);
        }

		#endregion
		
		#region Properties
		
        /// <summary>
        ///    Returns true if int constants have been set.
        /// </summary>
        public bool HasIntConstants {
            get {
                return intContants.Count > 0;
            }
        }

        /// <summary>
        ///    Returns true if floating-point constants have been set.
        /// </summary>
        public bool HasFloatConstants {
            get {
                return floatContants.Count > 0;
            }
        }

        /// <summary>
        ///    Gets a packed array of all current integer contants.
        /// </summary>
        public int[] IntConstants {
            get {
                int[] ints = new int[intContants.Count];
                intContants.CopyTo(ints);
                return ints;
            }
        }

        /// <summary>
        ///    Gets the number of int contants values currently set.
        /// </summary>
        public int IntConstantCount {
            get {
                return intContants.Count;
            }
        }

        /// <summary>
        ///    Gets a packed array of all current floating-point contants.
        /// </summary>
        public float[] FloatConstants {
            get {
                float[] floats = new float[floatContants.Count];
                floatContants.CopyTo(floats);
                return floats;
            }
        }

        /// <summary>
        ///    Gets the number of floating-point contant values currently set.
        /// </summary>
        public int FloatConstantCount {
            get {
                return floatContants.Count;
            }
        }

		#endregion Properties
	}
}
