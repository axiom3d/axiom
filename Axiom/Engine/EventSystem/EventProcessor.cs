using System;
using System.Collections;
using Axiom.Core;
using Axiom.SubSystems.Rendering;

namespace Axiom.EventSystem
{
	/// <summary>
	/// Summary description for EventProcessor.
	/// </summary>
	public class EventProcessor
	{
		#region Member variables

		/// <summary>Holds queued events in a FIFO manner.</summary>
		Queue eventQueue = new Queue();

		#endregion

		public EventProcessor()
		{
		}

		public void RegisterKeyTarget(IKeyTarget target)
		{
		}

		public void RegisterMouseTarget(IMouseTarget target)
		{
		}

		public void Initialize(RenderWindow window)
		{
		}

		public void Start()
		{
			// add a frame listener so that we can process events each frame
			Engine.Instance.FrameStarted += new FrameEvent(RenderSystem_FrameStarted);
		}

		public void Stop()
		{
		}

		private bool RenderSystem_FrameStarted(object source, FrameEventArgs e)
		{
			while(eventQueue.Count > 0)
			{
				// loop through and process each event
				
			}

			return true;
		}
	}
}
