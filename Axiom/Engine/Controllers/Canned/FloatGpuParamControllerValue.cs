using System;
using Axiom.Controllers;
using Axiom.Graphics;
using Axiom.MathLib;

namespace Axiom.Controllers.Canned {
	/// <summary>
	/// Summary description for FloatGpuParamControllerValue.
	/// </summary>
	public class FloatGpuParamControllerValue : IControllerValue {
        private GpuProgramParameters parms;
        private int index;
        private Vector4 vec4 = new Vector4(0, 0, 0, 0);

		public FloatGpuParamControllerValue(GpuProgramParameters parms, int index) {
            this.parms = parms;
            this.index = index;
		}
	
        #region IControllerValue Members

        public float Value {
            get {
                // Return 0, no reason to read this value back
                return 0;
            }
            set {
                // set the x component, since this is a single value only
                vec4.x = value;

                // send the vector along to the gpu program params
                parms.SetConstant(index, vec4);
            }
        }

        #endregion
    }
}
