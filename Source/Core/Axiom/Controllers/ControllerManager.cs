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

using System.Collections.Generic;
using Axiom.Controllers.Canned;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;

#endregion Namespace Declarations

namespace Axiom.Controllers
{
	/// <summary>
	/// Summary description for ControllerManager.
	/// </summary>
	public sealed class ControllerManager : DisposableObject
	{
		#region Singleton implementation

		/// <summary>
		///     Singleton instance of this class.
		/// </summary>
		private static ControllerManager instance;

		/// <summary>
		///     Internal constructor.  This class cannot be instantiated externally.
		/// </summary>
		internal ControllerManager()
			: base()
		{
			if ( instance == null )
			{
				instance = this;
			}
		}

		/// <summary>
		///     Gets the singleton instance of this class.
		/// </summary>
		public static ControllerManager Instance
		{
			get
			{
				return instance;
			}
		}

		#endregion Singleton implementation

		#region Member variables

		/// <summary>
		///		List of references to controllers in a scene.
		/// </summary>
		private List<Controller<Real>> controllers = new List<Controller<Real>>();

		/// <summary>
		///		Local instance of a FrameTimeControllerValue to be used for time based controllers.
		/// </summary>
		private readonly IControllerValue<Real> frameTimeController = new FrameTimeControllerValue();

		private readonly IControllerFunction<Real> passthroughFunction = new PassthroughControllerFunction();
		private ulong lastFrameNumber = 0;

		/// <summary>
		/// Returns a ControllerValue which provides the time since the last frame as a control value source.
		/// </summary>
		/// <remarks>
		/// A common source value to use to feed into a controller is the time since the last frame. This method
		/// returns a pointer to a common source value which provides this information.
		/// @par
		/// Remember the value will only be up to date after the RenderSystem::beginFrame method is called.
		/// </remarks>
		/// <see cref="RenderSystem.BeginFrame"/>
		public IControllerValue<Real> FrameTimeSource
		{
			[OgreVersion( 1, 7, 2 )]
			get
			{
				return frameTimeController;
			}
		}

		#endregion

		#region Methods

		/// <summary>
		///		Overloaded method.  Creates a new controller, using a reference to a FrameTimeControllerValue as
		///		the source.
		/// </summary>
		/// <param name="destination">Controller value to use as the destination.</param>
		/// <param name="function">Controller funcion that will use the source value to set the destination.</param>
		/// <returns>A newly created controller object that will be updated during the main render loop.</returns>
		public Controller<Real> CreateController( IControllerValue<Real> destination, IControllerFunction<Real> function )
		{
			// call the overloaded method passing in our precreated frame time controller value as the source
			return CreateController( frameTimeController, destination, function );
		}

		/// <summary>
		///		Factory method for creating an instance of a controller based on the input provided.
		/// </summary>
		/// <param name="source">Controller value to use as the source.</param>
		/// <param name="destination">Controller value to use as the destination.</param>
		/// <param name="function">Controller funcion that will use the source value to set the destination.</param>
		/// <returns>A newly created controller object that will be updated during the main render loop.</returns>
		public Controller<Real> CreateController( IControllerValue<Real> source, IControllerValue<Real> destination,
		                                          IControllerFunction<Real> function )
		{
			// create a new controller object
			var controller = new Controller<Real>( source, destination, function );

			// add the new controller to our list
			controllers.Add( controller );

			return controller;
		}

		public void DestroyController( Controller<Real> controller )
		{
			controllers.Remove( controller );
		}

		public Controller<Real> CreateFrameTimePassthroughController( IControllerValue<Real> dest )
		{
			return CreateController( frameTimeController, dest, passthroughFunction );
		}

		[OgreVersion( 1, 7, 2 )]
		public Real GetElapsedTime()
		{
			return ( (FrameTimeControllerValue)frameTimeController ).ElapsedTime;
		}


		/// <summary>
		///     Creates a texture layer animator controller.
		/// </summary>
		/// <remarks>
		///     This helper method creates the Controller, IControllerValue and IControllerFunction classes required
		///     to animate a texture.
		/// </remarks>
		/// <param name="texUnit">The texture unit to animate.</param>
		/// <param name="sequenceTime">Length of the animation (in seconds).</param>
		/// <returns>A newly created controller object that will be updated during the main render loop.</returns>
		public Controller<Real> CreateTextureAnimator( TextureUnitState texUnit, Real sequenceTime )
		{
			IControllerValue<Real> val = new TextureFrameControllerValue( texUnit );
			IControllerFunction<Real> func = new AnimationControllerFunction( sequenceTime );

			return CreateController( val, func );
		}

		/// <summary>
		///     Creates a basic time-based texture coordinate modifier designed for creating rotating textures.
		/// </summary>
		/// <remarks>
		///     This simple method allows you to easily create constant-speed rotating textures. If you want more
		///     control, look up the ControllerManager.CreateTextureWaveTransformer for more complex wave-based
		///     scrollers / stretchers / rotaters.
		/// </remarks>
		/// <param name="layer">The texture unit to animate.</param>
		/// <param name="speed">Speed of the rotation, in counter-clockwise revolutions per second.</param>
		/// <returns>A newly created controller object that will be updated during the main render loop.</returns>
		public Controller<Real> CreateTextureRotator( TextureUnitState layer, Real speed )
		{
			IControllerValue<Real> val = new TexCoordModifierControllerValue( layer, false, false, false, false, true );
			IControllerFunction<Real> func = new MultipyControllerFunction( -speed, true );

			return CreateController( val, func );
		}

		/// <summary>
		///     Predefined controller value for setting a single floating-
		///     point value in a constant paramter of a vertex or fragment program.
		/// </summary>
		/// <remarks>
		///     Any value is accepted, it is propagated into the 'x'
		///     component of the constant register identified by the index. If you
		///     need to use named parameters, retrieve the index from the param
		///     object before setting this controller up.
		/// </remarks>
		/// <param name="parms"></param>
		/// <param name="index"></param>
		/// <param name="timeFactor"></param>
		/// <returns></returns>
		public Controller<Real> CreateGpuProgramTimerParam( GpuProgramParameters parms, int index, Real timeFactor )
		{
			IControllerValue<Real> val = new FloatGpuParamControllerValue( parms, index );
			IControllerFunction<Real> func = new MultipyControllerFunction( timeFactor, true );

			return CreateController( val, func );
		}

		/// <summary>
		///     Creates a basic time-based texture uv coordinate modifier designed for creating scrolling textures.
		/// </summary>
		/// <remarks>
		///     This simple method allows you to easily create constant-speed scrolling textures. If you want to
		///     specify differnt speed values for horizontil and vertical scroll, use the specific methods
		///     <see cref="CreateTextureUScroller"/> and <see cref="CreateTextureVScroller"/>. If you want more
		///     control, look up the <see cref="CreateTextureWaveTransformer"/> for more complex wave-based
		///     scrollers / stretchers / rotaters.
		/// </remarks>
		/// <param name="layer">The texture unit to animate.</param>
		/// <param name="speed">speed, in wraps per second.</param>
		/// <returns>A newly created controller object that will be updated during the main render loop.</returns>
		public Controller<Real> CreateTextureUVScroller( TextureUnitState layer, Real speed )
		{
			IControllerValue<Real> val = null;
			IControllerFunction<Real> func = null;
			Controller<Real> controller = null;

			// if both u and v speeds are the same, we can use a single controller for it
			if ( speed != 0 )
			{
				// create the value and function
				val = new TexCoordModifierControllerValue( layer, true, true );
				func = new MultipyControllerFunction( -speed, true );

				// create the controller (uses FrameTime for source by default)
				controller = CreateController( val, func );
			}

			return controller;
		}

		/// <summary>
		///     Creates a basic time-based texture u coordinate modifier designed for creating scrolling textures.
		/// </summary>
		/// <remarks>
		///     This simple method allows you to easily create constant-speed scrolling textures. If you want more
		///     control, look up the <see cref="CreateTextureWaveTransformer"/> for more complex wave-based
		///     scrollers / stretchers / rotaters.
		/// </remarks>
		/// <param name="layer">The texture unit to animate.</param>
		/// <param name="speed">speed, in wraps per second.</param>
		/// <returns>A newly created controller object that will be updated during the main render loop.</returns>
		public Controller<Real> CreateTextureUScroller( TextureUnitState layer, Real speed )
		{
			IControllerValue<Real> val = null;
			IControllerFunction<Real> func = null;
			Controller<Real> controller = null;

			// Don't create a controller if the speed is zero
			if ( speed != 0 )
			{
				// create the value and function
				val = new TexCoordModifierControllerValue( layer, true );
				func = new MultipyControllerFunction( -speed, true );

				// create the controller (uses FrameTime for source by default)
				controller = CreateController( val, func );
			}

			return controller;
		}

		/// <summary>
		///     Creates a basic time-based texture v coordinate modifier designed for creating scrolling textures.
		/// </summary>
		/// <remarks>
		///     This simple method allows you to easily create constant-speed scrolling textures. If you want more
		///     control, look up the <see cref="CreateTextureWaveTransformer"/> for more complex wave-based
		///     scrollers / stretchers / rotaters.
		/// </remarks>
		/// <param name="layer">The texture unit to animate.</param>
		/// <param name="speed">speed, in wraps per second.</param>
		/// <returns>A newly created controller object that will be updated during the main render loop.</returns>
		public Controller<Real> CreateTextureVScroller( TextureUnitState layer, Real speed )
		{
			IControllerValue<Real> val = null;
			IControllerFunction<Real> func = null;
			Controller<Real> controller = null;

			// if both u and v speeds are the same, we can use a single controller for it
			if ( speed != 0 )
			{
				// create the value and function
				val = new TexCoordModifierControllerValue( layer, false, true );
				func = new MultipyControllerFunction( -speed, true );

				// create the controller (uses FrameTime for source by default)
				controller = CreateController( val, func );
			}

			return controller;
		}

		/// <summary>
		///	    Creates a very flexible time-based texture transformation which can alter the scale, position or
		///	    rotation of a texture based on a wave function.	
		/// </summary>
		/// <param name="layer">The texture unit to effect.</param>
		/// <param name="type">The type of transform, either translate (scroll), scale (stretch) or rotate (spin).</param>
		/// <param name="waveType">The shape of the wave, see WaveformType enum for details.</param>
		/// <param name="baseVal">The base value of the output.</param>
		/// <param name="frequency">The speed of the wave in cycles per second.</param>
		/// <param name="phase">The offset of the start of the wave, e.g. 0.5 to start half-way through the wave.</param>
		/// <param name="amplitude">Scales the output so that instead of lying within 0..1 it lies within 0..(1 * amplitude) for exaggerated effects</param>
		/// <returns>A newly created controller object that will be updated during the main render loop.</returns>
		public Controller<Real> CreateTextureWaveTransformer( TextureUnitState layer, TextureTransform type,
		                                                      WaveformType waveType, Real baseVal, Real frequency, Real phase,
		                                                      Real amplitude )
		{
			IControllerValue<Real> val = null;
			IControllerFunction<Real> function = null;

			// determine which type of controller value this layer needs
			switch ( type )
			{
				case TextureTransform.TranslateU:
					val = new TexCoordModifierControllerValue( layer, true, false );
					break;

				case TextureTransform.TranslateV:
					val = new TexCoordModifierControllerValue( layer, false, true );
					break;

				case TextureTransform.ScaleU:
					val = new TexCoordModifierControllerValue( layer, false, false, true, false, false );
					break;

				case TextureTransform.ScaleV:
					val = new TexCoordModifierControllerValue( layer, false, false, false, true, false );
					break;

				case TextureTransform.Rotate:
					val = new TexCoordModifierControllerValue( layer, false, false, false, false, true );
					break;
			} // switch

			// create a new waveform controller function
			function = new WaveformControllerFunction( waveType, baseVal, frequency, phase, amplitude, true );

			// finally, create the controller using frame time as the source value
			return CreateController( frameTimeController, val, function );
		}

		/// <summary>
		///		Causes all registered controllers to execute.  This will depend on RenderSystem.BeginScene already
		///		being called so that the time since last frame can be obtained for calculations.
		/// </summary>
		public void UpdateAll()
		{
			var thisFrameNumber = Root.Instance.CurrentFrameCount;
			if ( thisFrameNumber != lastFrameNumber )
			{
				// loop through each controller and tell it to update
				foreach ( var controller in controllers )
				{
					controller.Update();
				}
				lastFrameNumber = thisFrameNumber;
			}
		}

		#endregion

		#region IDisposable Implementation

		/// <summary>
		///     Called when the engine is shutting down.
		/// </summary>
		protected override void dispose( bool disposeManagedResources )
		{
			if ( !IsDisposed )
			{
				if ( disposeManagedResources )
				{
					controllers.Clear();
					controllers = null;
					instance = null;
				}
			}
			base.dispose( disposeManagedResources );
		}

		#endregion IDisposable Implementation
	}
}