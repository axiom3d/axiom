#region MIT/X11 License

//Copyright © 2003-2012 Axiom 3D Rendering Engine Project
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.

#endregion License

using System;
using System.ComponentModel;
using Axiom.Configuration;
using Axiom.Core;

namespace Axiom.Framework.Configuration
{
	/// <summary>
	/// 
	/// </summary>
	public class OSXConfigurationDialog : IConfigurationDialog
	{
		#region Fields and Properties

		private const string _logoResourceNameDefault = "AxiomLogo.png";
		public string LogoResourceName { get; set; }

		private const string _iconResourceNameDefault = "AxiomIcon.ico";
		public string IconResourceName { get; set; }

		public Root Engine { get; set; }

		public ResourceGroupManager ResourceManager { get; set; }

		#endregion Fields and Properties

		public OSXConfigurationDialog( Root engine, ResourceGroupManager resourceManager )
		{
			Engine = engine;
			ResourceManager = resourceManager;

			Engine.RenderSystem = Engine.RenderSystems[0];

			LogoResourceName = _logoResourceNameDefault;
			IconResourceName = _iconResourceNameDefault;
		}

		#region Event Handlers
		#endregion Event Handlers

		#region IConfigurationDialog Implementation

		public Axiom.Graphics.RenderSystem RenderSystem
		{
			get
			{
				return Engine.RenderSystems[0] as Axiom.Graphics.RenderSystem;
			}
		}

		public virtual DialogResult Show()
		{
			return true ? Configuration.DialogResult.Ok : Configuration.DialogResult.Cancel;
		}

		#endregion IConfigurationDialog Implementation
	}
}