#region MIT/X11 License

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
using System.Collections.Generic;
using System.Text;

using Axiom.Core;
using Axiom.Math;
using Axiom.Overlays;
using Axiom.Overlays.Elements;

namespace Axiom.Samples
{
	/// <summary>
	/// Basic parameters panel widget.
	/// </summary>
	public class ParamsPanel : Widget
	{
		#region fields

		/// <summary>
		/// 
		/// </summary>
		protected TextArea namesArea;

		/// <summary>
		/// 
		/// </summary>
		protected TextArea valuesArea;

		/// <summary>
		/// 
		/// </summary>
		protected IList<String> names;

		/// <summary>
		/// 
		/// </summary>
		protected IList<String> values;

		#endregion fields

		#region properties

		/// <summary>
		/// 
		/// </summary>
		public IList<string> ParamNames
		{
			get { return names; }
			set
			{
				this.names = value;
				this.values.Clear();
				for( int i = 0; i < value.Count; i++ )
				{
					this.values.Add( "" );
				}
				element.Height = this.namesArea.Top * 2 + this.names.Count * this.namesArea.CharHeight;
				this.UpdateText();
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public IList<string> ParamValues
		{
			set
			{
				values = value;
				UpdateText();
			}
			get { return values; }
		}

		#endregion properties

		/// <summary>
		/// Do not instantiate any widgets directly. Use SdkTrayManager.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="width"></param>
		/// <param name="lines"></param>
		public ParamsPanel( String name, Real width, int lines )
		{
			element = OverlayManager.Instance.Elements.CreateElementFromTemplate( "SdkTrays/ParamsPanel", "BorderPanel", name );
			OverlayElementContainer c = (OverlayElementContainer)element;
			this.namesArea = (TextArea)c.Children[ Name + "/ParamsPanelNames" ];
			this.valuesArea = (TextArea)c.Children[ Name + "/ParamsPanelValues" ];
			element.Width = width;
			element.Height = this.namesArea.Top * 2 + lines * this.namesArea.CharHeight;
			values = new List<string>();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="paramName"></param>
		/// <param name="paramValue"></param>
		public void SetParamValue( String paramName, String paramValue )
		{
			for( int i = 0; i < this.names.Count; i++ )
			{
				if( this.names[ i ] == paramName )
				{
					this.values[ i ] = paramValue;
					this.UpdateText();
					return;
				}
			}

			String desc = "ParamsPanel \"" + Name + "\" has no parameter \"" + paramName + "\".";
			throw new System.IndexOutOfRangeException( desc );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="index"></param>
		/// <param name="paramValue"></param>
		public void SetParamValue( int index, String paramValue )
		{
			if( index < 0 || index >= this.names.Count )
			{
				String desc = "ParamsPanel \"" + Name + "\" has no parameter at position " + index + ".";
				throw new System.IndexOutOfRangeException( desc );
			}
			if( this.values.Count < index )
			{
				this.values.Insert( index, paramValue );
			}
			else
			{
				this.values[ index ] = paramValue;
			}
			this.UpdateText();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="paramName"></param>
		/// <returns></returns>
		public String GetParamValue( String paramName )
		{
			for( int i = 0; i < this.names.Count; i++ )
			{
				if( this.names[ i ] == paramName )
				{
					return this.values[ i ];
				}
			}

			String desc = "ParamsPanel \"" + Name + "\" has no parameter \"" + paramName + "\".";
			throw new System.IndexOutOfRangeException( desc );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public String GetParamValue( int index )
		{
			if( index < 0 || index >= this.names.Count )
			{
				String desc = "ParamsPanel \"" + Name + "\" has no parameter at position " + index + ".";
				throw new System.IndexOutOfRangeException( desc );
			}

			return this.values[ index ];
		}

		/// <summary>
		/// Internal method - updates text areas based on name and value lists.
		/// </summary>
		protected void UpdateText()
		{
			StringBuilder namesDS = new StringBuilder();
			StringBuilder valuesDS = new StringBuilder();

			for( int i = 0; i < this.names.Count; i++ )
			{
				namesDS.Append( this.names[ i ] + ":\n" );
				valuesDS.Append( this.values[ i ] + "\n" );
			}

			this.namesArea.Text = namesDS.ToString();
			this.valuesArea.Text = valuesDS.ToString();
		}
	}
}
