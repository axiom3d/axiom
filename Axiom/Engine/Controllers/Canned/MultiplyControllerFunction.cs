using System;

namespace Axiom.Controllers.Canned
{
	/// <summary>
	/// Summary description for MultiplyControllerValue.
	/// </summary>
	public class MultipyControllerFunction : BaseControllerFunction
	{
		private float rate = 10.0f;

		public MultipyControllerFunction(float rate) : base(false)
		{
			this.rate = rate;
		}

		public MultipyControllerFunction(float rate, bool useDelta) : base(useDelta)
		{
			this.rate = rate;
		}

		public override float Execute(float sourceValue)
		{
			return AdjustInput(sourceValue * rate);
		}

	}
}
