using System;

namespace Axiom.Controllers
{
	/// <summary>
	///		Subclasses of this class are responsible for performing a function on an input value for a Controller.
	///	 </summary>
	///	 <remarks>
	///		This abstract class provides the interface that needs to be supported for a custom function which
	///		can be 'plugged in' to a Controller instance, which controls some object value based on an input value.
	///		For example, the WaveControllerFunction class provided by Ogre allows you to use various waveforms to
	///		translate an input value to an output value.
	///		<p/>
	///		This base class implements IControllerFunction, but leaves the implementation up to the subclasses.
	/// </remarks>
	public abstract class BaseControllerFunction : IControllerFunction
	{
		#region Member variables
		
		/// <summary>
		///		If true, function will add input values together and wrap at 1.0 before evaluating.
		/// </summary>
		protected bool useDeltaInput;

		/// <summary>
		///		Value to be added during evaluation.
		/// </summary>
		protected float deltaCount;
		
		#endregion

		#region Constructors

		public BaseControllerFunction(bool useDeltaInput)
		{
			this.useDeltaInput = useDeltaInput;
			deltaCount = 0;
		}

		#endregion

		#region Methods

		/// <summary>
		///		Adjusts the input value by a delta.
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		virtual protected float AdjustInput(float input)
		{
			if(useDeltaInput)
			{
				deltaCount += input;

				// wrap the value if it went past 1
				while(deltaCount >= 1.0f)
					deltaCount -= 1.0f;

				// return the adjusted input value
				return deltaCount;
			}
			else
			{
				// return the input value as is
				return input;
			}
		}

		#endregion

		#region IControllerFunction methods

		public abstract float Execute(float sourceValue);

		#endregion
	}
}
