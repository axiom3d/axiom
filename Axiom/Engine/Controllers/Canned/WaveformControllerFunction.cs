using System;
using Axiom.MathLib;

namespace Axiom.Controllers.Canned
{
	/// <summary>
	/// 	Summary description for WaveformControllerFunction.
	/// </summary>
	public class WaveformControllerFunction : BaseControllerFunction
	{
		#region Member variables
		
		protected WaveformType type;
		protected float baseVal = 0.0f;
		protected float frequency = 1.0f;
		protected float phase = 0.0f;
		protected float amplitude = 1.0f;

		#endregion
		
		#region Constructors
		
		public WaveformControllerFunction(WaveformType type, float baseVal, float frequency, float phase, float amplitude, bool useDelta) : base(useDelta)
		{
			this.type = type;
			this.baseVal = baseVal;
			this.frequency = frequency;
			this.phase = phase;
			this.amplitude = amplitude;
		}

		public WaveformControllerFunction(WaveformType type, float baseVal) : base(true)
		{
			this.type = type;
			this.baseVal = baseVal;
		}

		public WaveformControllerFunction(WaveformType type, float baseVal, float frequency) : base(true)
		{
			this.type = type;
			this.baseVal = baseVal;
			this.frequency = frequency;
		}

		public WaveformControllerFunction(WaveformType type, float baseVal, float frequency, float phase) : base(true)
		{
			this.type = type;
			this.baseVal = baseVal;
			this.frequency = frequency;
			this.phase = phase;
		}

		public WaveformControllerFunction(WaveformType type, float baseVal, float frequency, float phase, float amplitude) : base(true)
		{
			this.type = type;
			this.baseVal = baseVal;
			this.frequency = frequency;
			this.phase = phase;
			this.amplitude = amplitude;
		}

		public WaveformControllerFunction(WaveformType type) : base(true)
		{
			this.type = type;
		}
		
		#endregion
		
		#region Methods

		public override float Execute(float sourceValue)
		{
			float input = AdjustInput(sourceValue * frequency);
			float output = 0.0f;

			// factor down to ensure [0,1] 
			while(input >= 1.0f)
				input -= 1.0f;

			// first, get output in range [-1,1] (typical for waveforms)
			switch(type)
			{
				case WaveformType.Sine:
					output = MathUtil.Sin(input * MathUtil.TWO_PI);
					break;

				case WaveformType.Triangle:
					if(input < 0.25f)
						output = input * 4;
					else if(input >= 0.25f && input < 0.75f)
						output = 1.0f - ((input - 0.25f) * 4);
					else
						output = ((input - 0.75f) * 4) - 1.0f;

					break;

				case WaveformType.Square:
					if(input <= 0.5f)
						output = 1.0f;
					else
						output = -1.0f;
					break;

				case WaveformType.Sawtooth:
					output = (input * 2) - 1;
					break;

				case WaveformType.InverseSawtooth:
					output = -((input * 2) - 1);
					break;

			} // end switch

			// scale final output to range [0,1], and then by base and amplitude
			return baseVal + ((output + 1.0f) * 0.5f * amplitude);
		}

		protected override float AdjustInput(float input)
		{
			float adjusted = base.AdjustInput(input);

			// if not using delta accumulation, adjust by phase value
			if(!useDeltaInput)
				adjusted += phase;

			return adjusted;
		}
		
		#endregion
		
		#region Properties
		
		#endregion

	}
}
