#region LGPL License

/*
Axiom Graphics Engine Library
Copyright © 2003-2011 Axiom Project Team

The overall design, and a majority of the core engine and rendering code
contained within this library is a derivative of the open source Object Oriented
Graphics Engine OGRE, which can be found at http://ogre.sourceforge.net.
Many thanks to the OGRE team for maintaining such a high quality project.

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
*/

#endregion LGPL License

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id: SceneManager.cs 1724 2009-08-24 21:57:43Z borrillis $"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

#endregion Namespace Declarations

namespace Axiom.Core
{
	/// <summary>
	/// Enumeration of the possible modes allowed for processing the special case render queue list.
	/// <see cref="SpecialCaseRenderQueue.RenderQueueMode" />
	/// </summary>
	public enum SpecialCaseRenderQueueMode
	{
		/// <summary>
		/// Render only the queues in the special case list
		/// </summary>
		Include,
		/// <summary>
		/// Render all except the queues in the special case list
		/// </summary>
		Exclude
	};

	public class SpecialCaseRenderQueue
	{
		#region Fields and Properties

		private SpecialCaseRenderQueueMode _mode = SpecialCaseRenderQueueMode.Exclude;
		private readonly List<RenderQueueGroupID> _queue = new List<RenderQueueGroupID>();

		#endregion Fields and Properties

		/// <summary>
		/// Adds an item to the 'special case' render queue list.
		/// </summary>
		/// <remarks>Normally all render queues are rendered, in their usual sequence,
		/// only varying if a RenderQueueListener nominates for the queue to be
		/// repeated or skipped. This method allows you to add a render queue to
		/// a 'special case' list, which varies the behaviour. The effect of this
		/// list depends on the 'mode' in which this list is in, which might be
		/// to exclude these render queues, or to include them alone (excluding
		/// all other queues). This allows you to perform broad selective
		/// rendering without requiring a RenderQueueListener.</remarks>
		/// <param name="queueId">The identifier of the queue which should be added to the
		///  special case list. Nothing happens if the queue is already in the list.</param>
		public virtual void AddRenderQueue( RenderQueueGroupID queueId )
		{
			_queue.Add( queueId );
		}

		/// <summary>
		/// Removes an item to the 'special case' render queue list
		/// </summary>
		/// <param name="queueId">The identifier of the queue which should be removed from the
		/// special case list. Nothing happens if the queue is not in the list.</param>
		public virtual void RemoveRenderQueue( RenderQueueGroupID queueId )
		{
			_queue.Remove( queueId );
		}

		/// <summary>
		/// Clears the 'special case' render queue list.
		/// </summary>
		public virtual void ClearRenderQueues()
		{
			_queue.Clear();
		}

		/// <summary>
		/// Gets the way the special case render queue list is processed.
		/// </summary>
		/// <returns></returns>
		public virtual SpecialCaseRenderQueueMode RenderQueueMode
		{
			get
			{
				return _mode;
			}

			set
			{
				_mode = value;
			}
		}

		/// <summary>
		/// Returns whether or not the named queue will be rendered based on the
		/// current 'special case' render queue list and mode.
		/// </summary>
		/// <param name="queueId">The identifier of the queue which should be tested</param>
		/// <returns>true if the queue will be rendered, false otherwise</returns>
		public virtual bool IsRenderQueueToBeProcessed( RenderQueueGroupID queueId )
		{
			bool inList = _queue.Contains( queueId );
			return ( inList && _mode == SpecialCaseRenderQueueMode.Include )
				|| ( !inList && _mode == SpecialCaseRenderQueueMode.Exclude );
		}

	}
}