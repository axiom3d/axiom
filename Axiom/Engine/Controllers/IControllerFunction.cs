using System;

namespace Axiom.Controllers
{
	/// <summary>
	///		Interface describing the required methods of a Controller Function.
	/// </summary>
	public interface IControllerFunction
	{
		/// <summary>
		///		Called by a controller every frame to have this function run and return on the supplied
		///		source value and return the result.
		/// </summary>
		/// <param name="val"></param>
		float Execute(float sourceValue);
	}
}
