using System;

namespace Axiom.Animating
{
	/// <summary>
	///		Types of interpolation used in animation.
	/// </summary>
	public enum InterpolationMode
	{
		/// <summary>
		///		More robotic movement, not as realistic.
		///	 </summary>
		Linear,
		/// <summary>
		///		Smooth movement between keyframes.
		///	 </summary>
		Spline
	}

	/// <summary>
	///		Used to specify how animations are applied to a skeleton.
	/// </summary>
	public enum SkeletalAnimBlendMode
	{
		/// <summary>
		///		Animations are applied by calculating a weighted average of all animations.
		///	 </summary>
		Average,
		/// <summary>
		///		Animations are applied bu calculating a weighted cumulative total.
		/// </summary>
		Cumulative
	}
}
