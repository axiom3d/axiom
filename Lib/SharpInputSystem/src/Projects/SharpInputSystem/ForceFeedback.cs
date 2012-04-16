#region LGPL License
/*
Sharp Input System Library
Copyright (C) 2007 Michael Cummings

The overall design, and a majority of the core code contained within 
this library is a derivative of the open source Open Input System ( OIS ) , 
which can be found at http://www.sourceforge.net/projects/wgois.  
Many thanks to the Phillip Castaneda for maintaining such a high quality project.

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

#region Namespace Declarations

using System;
using System.Collections.Generic;

#endregion Namespace Declarations

namespace SharpInputSystem
{
	/// <summary>
	/// Interface class for dealing with Force Feedback devices
	/// </summary>
	public abstract class ForceFeedback : IInputObjectInterface
	{
		#region Fields and Properties

		/// <summary>
		/// This is like setting the master volume of an audio device.
		/// Individual effects have gain levels; however, this affects all
		/// effects at once.
		/// </summary>
		/// <remarks>
		/// A value between 0.0 and 1.0 represent the percentage of gain. 1.0
		/// being the highest possible force level (means no scaling).
		/// </remarks>
		public abstract float MasterGain
		{
			set;
		}

		/// <summary>
		/// If using Force Feedback effects, this should be turned off
		/// before uploading any effects. Auto centering is the motor moving
		/// the joystick back to center. DirectInput only has an on/off setting,
		/// whereas linux has levels.. Though, we go with DI's on/off mode only
		/// </summary>
		/// <remarks>
		/// true to turn auto centering on, false to turn off.
		/// </remarks>
		public abstract bool AutoCenterMode
		{
			set;
		}

		/// <summary>
		/// Get the number of supported Axes for ForceFeedback usage
		/// </summary>
		public abstract int SupportedAxesCount
		{
			get;
		}

		/// <summary>
		/// a list of all supported effects
		/// </summary>
		private EffectsList _supportedEffects = new EffectsList();
		/// <summary>
		/// Get a list of all supported effects
		/// </summary>
		public EffectsList SupportedEffects
		{
			get
			{
				return _supportedEffects;
			}
		}

		#endregion Fields and Properties

		#region Methods

		/// <summary>
		/// Creates and Plays the effect immediately. If the device is full
		/// of effects, it will fail to be uploaded. You will know this by
		/// an invalid Effect Handle
		/// </summary>
		/// <param name="effect"></param>
		public abstract void Upload( Effect effect );

		/// <summary>
		/// Modifies an effect that is currently playing
		/// </summary>
		/// <param name="effect"></param>
		public abstract void Modify( Effect effect );

		/// <summary>
		/// Remove the effect from the device
		/// </summary>
		/// <param name="effect"></param>
		public abstract void Remove( Effect effect );

		public void AddEffectType( Effect.EForce force, Effect.EType type )
		{
			if ( force == Effect.EForce.UnknownForce || type == Effect.EType.Unknown )
				throw new ArgumentException( "Added Unknown force|type." );
			_supportedEffects.Add( force, type );
		}
		#endregion Methods

	}

	public sealed class EffectsList : Dictionary<Effect.EForce, Effect.EType>
	{
	}
}
