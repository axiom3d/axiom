#region MIT/X11 License

//Copyright � 2003-2012 Axiom 3D Rendering Engine Project
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
using Axiom.Math;
using Axiom.Overlays;

namespace Axiom.Samples
{
    /// <summary>
    /// Basic separator widget.
    /// </summary>
    public class Separator : Widget
    {
        #region properties

        /// <summary>
        /// 
        /// </summary>
        public bool IsFitToTray { get; protected set; }

        #endregion properties

        #region construction

        /// <summary>
        /// Do not instantiate any widgets directly. Use SdkTrayManager.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="width"></param>
        public Separator(String name, Real width)
        {
            element = OverlayManager.Instance.Elements.CreateElementFromTemplate("SdkTrays/Separator", "Panel", name);
            if (width <= 0)
            {
                IsFitToTray = true;
            }
            else
            {
                IsFitToTray = false;
                element.Width = width;
            }
        }

        #endregion construction
    }
}