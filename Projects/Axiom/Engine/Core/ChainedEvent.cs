using System;
using System.Collections.Generic;
using System.Text;

namespace Axiom.Core
{
	/// <summary>
	///
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class ChainedEvent<T> where T : EventArgs
	{
		public EventHandler<T> EventSinks;

		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="arg"></param>
		/// <param name="compare"></param>
		/// <returns></returns>
		public virtual bool Fire( object sender, T arg, Predicate<T> compare )
		{
			bool continueChain = true;

			// Assuming the multicast delegate is not null...
			if ( EventSinks != null )
			{
				// Call the methods until one of them handles the event
				// or all the methods in the delegate list are processed.
				foreach ( EventHandler<T> sink in EventSinks.GetInvocationList() )
				{
					sink( sender, arg );
					continueChain = compare( arg );
					if ( !continueChain )
						break;
				}
			}
			// Return a flag indicating whether an event sink canceled the event.
			return continueChain;
		}
	}
}