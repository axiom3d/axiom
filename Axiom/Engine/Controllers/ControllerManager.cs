#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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

using System;
using System.Collections;
using Axiom.Core;
using Axiom.Controllers.Canned;
using Axiom.SubSystems.Rendering;

namespace Axiom.Controllers
{
	/// <summary>
	/// Summary description for ControllerManager.
	/// </summary>
	public class ControllerManager : IDisposable
	{
		#region Singleton implementation

		static ControllerManager() { Init(); }
		protected ControllerManager() {}
		protected static ControllerManager instance;

		public static ControllerManager Instance
		{
			get { return instance; }
		}

		public static void Init()
		{
			instance = new ControllerManager();
		}
		
		#endregion

		#region Member variables

		/// <summary>
		///		List of references to controllers in a scene.
		/// </summary>
		protected ArrayList controllers = new ArrayList();

		/// <summary>
		///		Local instance of a FrameTimeControllerValue to be used for time based controllers.
		/// </summary>
		protected FrameTimeControllerValue frameTimeController = new FrameTimeControllerValue();

		#endregion

		#region Methods

		/// <summary>
		///		Overloaded method.  Creates a new controller, using a reference to a FrameTimeControllerValue as
		///		the source.
		/// </summary>
		/// <param name="destination">Controller value to use as the destination.</param>
		/// <param name="function">Controller funcion that will use the source value to set the destination.</param>
		/// <returns>A newly created controller object that will be updated during the main render loop.</returns>
		public Controller CreateController(IControllerValue destination, IControllerFunction function)
		{
			// call the overloaded method passing in our precreated frame time controller value as the source
			return CreateController(frameTimeController, destination, function);
		}

		/// <summary>
		///		Factory method for creating an instance of a controller based on the input provided.
		/// </summary>
		/// <param name="source">Controller value to use as the source.</param>
		/// <param name="destination">Controller value to use as the destination.</param>
		/// <param name="function">Controller funcion that will use the source value to set the destination.</param>
		/// <returns>A newly created controller object that will be updated during the main render loop.</returns>
		public Controller CreateController(IControllerValue source, IControllerValue destination, IControllerFunction function)
		{
			// create a new controller object
			Controller controller = new Controller(source, destination, function);

			// add the new controller to our list
			controllers.Add(controller);

			return controller;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="layer"></param>
		/// <param name="speed"></param>
		/// <returns></returns>
		public Controller CreateTextureRotator(TextureLayer layer, float speed)
		{
			IControllerValue val = new TexCoordModifierControllerValue(layer, false, false, false, false, true);
			IControllerFunction func = new MultipyControllerFunction(speed, true);

			return CreateController(val, func);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="layer"></param>
		/// <param name="speedU"></param>
		/// <param name="speedV"></param>
		/// <returns></returns>
		public Controller CreateTextureScroller(TextureLayer layer, float speedU, float speedV)
		{
			IControllerValue val = null;
			IControllerFunction func = null;
			Controller controller = null;

			// if both u and v speeds are the same, we can use a single controller for it
			if(speedU != 0 && (speedU == speedV))
			{
				// create the value and function
				val = new TexCoordModifierControllerValue(layer, true, true);
				func = new MultipyControllerFunction(speedU, true);

				// create the controller (uses FrameTime for source by default)
				controller = CreateController(val, func);
			}
			else
			{
				// create seperate for U
				if(speedU != 0)
				{
					// create the value and function
					val = new TexCoordModifierControllerValue(layer, true, false);
					func = new MultipyControllerFunction(speedU, true);

					// create the controller (uses FrameTime for source by default)
					controller = CreateController(val, func);
				}

				// create seperate for V
				if(speedV != 0)
				{
					// create the value and function
					val = new TexCoordModifierControllerValue(layer, false, true);
					func = new MultipyControllerFunction(speedV, true);

					// create the controller (uses FrameTime for source by default)
					controller = CreateController(val, func);
				}
			}

			// TODO: Revisit, since we can't return 2 controllers in the case of non equal U and V speeds
			return controller;
		}

		/// <summary>
		///		
		/// </summary>
		/// <param name="layer"></param>
		/// <param name="transformType"></param>
		/// <param name="waveType"></param>
		/// <param name="baseVal"></param>
		/// <param name="frequency"></param>
		/// <param name="phase"></param>
		/// <param name="amplitude"></param>
		/// <returns></returns>
		public Controller CreateTextureWaveTransformer(TextureLayer layer, TextureTransform type, WaveformType waveType, 
			float baseVal, float frequency, float phase, float amplitude)
		{
			IControllerValue val = null;
			IControllerFunction function = null;

			// determine which type of controller value this layer needs
			switch(type)
			{
				case TextureTransform.TranslateU:
					val = new TexCoordModifierControllerValue(layer, true, false);
					break;

				case TextureTransform.TranslateV:
					val = new TexCoordModifierControllerValue(layer, false, true);
					break;

				case TextureTransform.ScaleU:
					val = new TexCoordModifierControllerValue(layer, false, false, true, false, false);
					break;

				case TextureTransform.ScaleV:
					val = new TexCoordModifierControllerValue(layer, false, false, false, true, false);
					break;

				case TextureTransform.Rotate:
					val = new TexCoordModifierControllerValue(layer, false, false, false, false, true);
					break;
			} // switch

			// create a new waveform controller function
			function = new WaveformControllerFunction(waveType, baseVal, frequency, phase, amplitude, true);

			// finally, create the controller using frame time as the source value
			return CreateController(frameTimeController, val, function);
		}

		/// <summary>
		///		Causes all registered controllers to execute.  This will depend on RenderSystem.BeginScene already
		///		being called so that the time since last frame can be obtained for calculations.
		/// </summary>
		public void UpdateAll()
		{
			// loop through each controller and tell it to update
			for(int i = 0; i < controllers.Count; i++)
			{
				Controller controller = (Controller)controllers[i];
				controller.Update();
			}
		}

		#endregion

		#region IDisposable Members

		public void Dispose()
		{
			controllers.Clear();
		}

		#endregion
	}
}
