using System;
using Axiom.Core;

namespace Axiom.Controllers
{
	/// <summary>
	/// Summary description for FrameTimeControllerValue.
	/// </summary>
	public class FrameTimeControllerValue : IControllerValue
	{
		/// <summary>
		///		Stores the value of the time elapsed since the last frame.
		/// </summary>
		private float frameTime;

		/// <summary>
		///		Float value that should be used to scale controller time.
		/// </summary>
		private float timeFactor;

		public FrameTimeControllerValue()
		{
			// add a frame started event handler
			Engine.Instance.FrameStarted += new FrameEvent(RenderSystem_FrameStarted);

			frameTime = 0;

			// default to 1 for standard timing
			timeFactor = 1;
		}

		#region IControllerValue Members

		/// <summary>
		///		Gets a time scaled value to use for controller functions.
		/// </summary>
		float IControllerValue.Value
		{
			get
			{
				return frameTime;
			}
			set 
			{ 
				// Do nothing			
			}
		}

		#endregion

		#region Properties

		/// <summary>
		///		Float value that should be used to scale controller time.  This could be used
		///		to either speed up or slow down controller functions independent of slowing
		///		down the render loop.
		/// </summary>
		public float TimeFactor
		{
			get { return timeFactor; }
			set { timeFactor = value; }
		}

		#endregion

		/// <summary>
		///		Event handler to the Frame Started event so that we can capture the
		///		time since last frame to use for controller functions.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="e"></param>
		/// <returns></returns>
		private bool RenderSystem_FrameStarted(object source, FrameEventArgs e)
		{
			// apply the time factor to the time since last frame and save it
			frameTime = timeFactor * e.TimeSinceLastFrame;

			return true;
		}
	}
}
