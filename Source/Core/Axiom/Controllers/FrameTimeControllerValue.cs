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

#endregion

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using Axiom.Core;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.Controllers
{
	/// <summary>
	/// Summary description for FrameTimeControllerValue.
	/// </summary>
	public sealed class FrameTimeControllerValue : IControllerValue<Real>
	{
		/// <summary>
		///		Stores the value of the time elapsed since the last frame.
		/// </summary>
		private Real frameTime;

		private float frameDelay;

		/// <summary>
		///		Float value that should be used to scale controller time.
		/// </summary>
		private float timeFactor;

		private Real elapsedTime;

		public FrameTimeControllerValue()
		{
			// add a frame started event handler
			Root.Instance.FrameStarted += RenderSystem_FrameStarted;

			//frameTime = 0; //[FXCop Optimization : Do not initialize unnecessarily], Defaults to 0,  left here for clarity

			// default to 1 for standard timing
			this.timeFactor = 1;
			this.frameDelay = 0;
			this.elapsedTime = 0;
		}

		#region IControllerValue Members

		/// <summary>
		///		Gets a time scaled value to use for controller functions.
		/// </summary>
		Real IControllerValue<Real>.Value
		{
			get
			{
				return this.frameTime;
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
			get
			{
				return this.timeFactor;
			}
			set
			{
				if ( value >= 0 )
				{
					this.timeFactor = value;
					this.frameDelay = 0;
				}
			}
		}

		public float FrameDelay
		{
			get
			{
				return this.frameDelay;
			}
			set
			{
				this.timeFactor = 0;
				this.frameDelay = value;
			}
		}

		public Real ElapsedTime
		{
			get
			{
				return this.elapsedTime;
			}
			set
			{
				this.elapsedTime = value;
			}
		}

		#endregion

		/// <summary>
		///		Event handler to the Frame Started event so that we can capture the
		///		time since last frame to use for controller functions.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="e"></param>
		/// <returns></returns>
		private void RenderSystem_FrameStarted( object source, FrameEventArgs e )
		{
			if ( this.frameDelay != 0 )
			{
				// Fixed frame time
				this.frameTime = this.frameDelay;
				this.timeFactor = this.frameDelay/e.TimeSinceLastFrame;
			}
			else
			{
				// Save the time value after applying time factor
				this.frameTime = this.timeFactor*e.TimeSinceLastFrame;
			}
			// Accumulate the elapsed time
			this.elapsedTime += this.frameTime;
		}
	}
}