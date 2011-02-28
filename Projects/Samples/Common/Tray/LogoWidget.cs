﻿#region MIT/X11 License
//Copyright © 2003-2011 Axiom 3D Rendering Engine Project
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

using Axiom.Overlays;

namespace Axiom.Samples
{
	/// <summary>
	/// Specialized decor widget to visualize a logo.
	/// </summary>
	public class LogoWidget : DecorWidget
	{
		#region construction
		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="typeName"></param>
		/// <param name="templateName"></param>
		public LogoWidget( string name, string typeName, string templateName )
			: base( name, typeName, templateName )
		{
			element = OverlayManager.Instance.Elements.CreateElement( typeName, name );
			element.MetricsMode = MetricsMode.Pixels;
			element.MaterialName = "SdkTrays/Logo";
			element.HorizontalAlignment = HorizontalAlignment.Center;
			element.Width = 128;
			element.Height = 39;
			element.Enabled = true;
			element.IsVisible = true;
			element.Text = "Test";
			element.Material.Load();
		}
		#endregion construction
	}
}
