using System;
using Axiom.Scripting;

namespace Axiom.Controllers
{
	/// <summary>
	/// Enumerates the wave types usable with the engine.
	/// </summary>
	public enum WaveformType
	{
		/// <summary>Standard sine wave which smoothly changes from low to high and back again.</summary>
		[ScriptEnum("sine")]
		Sine,
		/// <summary>An angular wave with a constant increase / decrease speed with pointed peaks.</summary>
		[ScriptEnum("triangle")]
		Triangle,
		/// <summary>Half of the time is spent at the min, half at the max with instant transition between. </summary>
		[ScriptEnum("square")]
		Square,
		/// <summary>Gradual steady increase from min to max over the period with an instant return to min at the end. </summary>
		[ScriptEnum("sawtooth")]
		Sawtooth,
		/// <summary>Gradual steady decrease from max to min over the period, with an instant return to max at the end. </summary>
		[ScriptEnum("inverse_sawtooth")]
		InverseSawtooth
	};
}
