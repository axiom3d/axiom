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
using Axiom.Core;
using Axiom.Math;
using Axiom.Overlays;
using Axiom.Overlays.Elements;

namespace Axiom.Samples
{
	public delegate void SliderMovedHandler( object sender, Slider slider );

	/// <summary>
	/// Basic slider widget.
	/// </summary>
	public class Slider : Widget
	{
		#region events

		public event SliderMovedHandler SliderMoved;

		#endregion events

		#region fields

		/// <summary>
		/// 
		/// </summary>
		protected TextArea textArea;

		/// <summary>
		/// 
		/// </summary>
		protected TextArea valueTextArea;

		/// <summary>
		/// 
		/// </summary>
		protected BorderPanel track;

		/// <summary>
		/// 
		/// </summary>
		protected Panel handle;

		/// <summary>
		/// 
		/// </summary>
		protected bool isDragging;

		/// <summary>
		/// 
		/// </summary>
		protected bool isFitToContents;

		/// <summary>
		/// 
		/// </summary>
		protected Real dragOffset;

		/// <summary>
		/// 
		/// </summary>
		protected Real value;

		/// <summary>
		/// 
		/// </summary>
		protected Real minValue;

		/// <summary>
		/// 
		/// </summary>
		protected Real maxValue;

		/// <summary>
		/// 
		/// </summary>
		protected Real interval;

		#endregion fields

		#region properties

		/// <summary>
		/// Gets or sets the value caption
		/// </summary>
		public string ValueCaption
		{
			get
			{
				return valueTextArea.Text;
			}
			set
			{
				valueTextArea.Text = value;
			}
		}

		/// <summary>
		/// Gets or sets the current value
		/// </summary>
		public Real Value
		{
			get
			{
				return value;
			}
			set
			{
				SetValue( value, true );
			}
		}

		/// <summary>
		/// Gets or sets the caption
		/// </summary>
		public string Caption
		{
			get
			{
				return textArea.Text;
			}
			set
			{
				textArea.Text = value;
				if ( isFitToContents )
				{
					element.Width = GetCaptionWidth( value, textArea ) + valueTextArea.Parent.Width + track.Width + 26;
				}
			}
		}

		#endregion

		#region construction

		/// <summary>
		/// Do not instantiate any widgets directly. Use SdkTrayManager.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="caption"></param>
		/// <param name="width"></param>
		/// <param name="trackWidth"></param>
		/// <param name="valueBoxWidth"></param>
		/// <param name="minValue"></param>
		/// <param name="maxValue"></param>
		/// <param name="snaps"></param>
		public Slider( String name, String caption, Real width, Real trackWidth, Real valueBoxWidth, Real minValue,
		               Real maxValue, int snaps )
		{
			isDragging = false;
			isFitToContents = false;
			element = OverlayManager.Instance.Elements.CreateElementFromTemplate( "SdkTrays/Slider", "BorderPanel", name );
			element.Width = width;
			var c = (OverlayElementContainer)element;
			textArea = (TextArea)c.Children[ Name + "/SliderCaption" ];
			var valueBox = (OverlayElementContainer)c.Children[ Name + "/SliderValueBox" ];
			valueBox.Width = valueBoxWidth;
			valueBox.Left = -( valueBoxWidth + 5 );
			valueTextArea = (TextArea)valueBox.Children[ valueBox.Name + "/SliderValueText" ];
			track = (BorderPanel)c.Children[ Name + "/SliderTrack" ];
			handle = (Panel)track.Children[ track.Name + "/SliderHandle" ];

			if ( trackWidth <= 0 ) // tall style
			{
				track.Width = width - 16;
			}
			else // long style
			{
				if ( width <= 0 )
				{
					isFitToContents = true;
				}
				element.Height = 34;
				textArea.Top = 10;
				valueBox.Top = 2;
				track.Top = -23;
				track.Width = trackWidth;
				track.HorizontalAlignment = HorizontalAlignment.Right;
				track.Left = -( trackWidth + valueBoxWidth + 5 );
			}

			Caption = caption;
			SetRange( minValue, maxValue, snaps, false );
		}

		#endregion

		#region methods

		/// <summary>
		/// Sets the minimum value, maximum value, and the number of snapping points.
		/// </summary>
		/// <param name="minValue"></param>
		/// <param name="maxValue"></param>
		/// <param name="snaps"></param>
		public void SetRange( Real minValue, Real maxValue, int snaps )
		{
			SetRange( minValue, maxValue, snaps, true );
		}

		/// <summary>
		/// Sets the minimum value, maximum value, and the number of snapping points.
		/// </summary>
		/// <param name="minValue"></param>
		/// <param name="maxValue"></param>
		/// <param name="snaps"></param>
		/// <param name="notifyListener"></param>
		public void SetRange( Real minValue, Real maxValue, int snaps, bool notifyListener )
		{
			this.minValue = minValue;
			this.maxValue = maxValue;

			if ( snaps <= 1 || this.minValue >= this.maxValue )
			{
				interval = 0;
				handle.Hide();
				value = minValue;
				if ( snaps == 1 )
				{
					valueTextArea.Text = this.minValue.ToString();
				}
				else
				{
					valueTextArea.Text = "";
				}
			}
			else
			{
				handle.Show();
				interval = ( maxValue - minValue )/( snaps - 1 );
				SetValue( minValue, notifyListener );
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="value"></param>
		/// <param name="notifyListener"></param>
		public void SetValue( Real value, bool notifyListener )
		{
			if ( interval == 0 )
			{
				return;
			}

			this.value = Math.Utility.Clamp<Real>( value, maxValue, minValue );

			ValueCaption = this.value.ToString();

			if ( listener != null && notifyListener )
			{
				listener.SliderMoved( this );
			}
			OnSliderMoved( this, this );

			if ( !isDragging )
			{
				handle.Left = (int)( ( this.value - minValue )/( maxValue - minValue )*( track.Width - handle.Width ) );
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="cursorPos"></param>
		public override void OnCursorPressed( Vector2 cursorPos )
		{
			if ( !handle.IsVisible )
			{
				return;
			}

			Vector2 co = Widget.CursorOffset( handle, cursorPos );

			if ( co.LengthSquared <= 81 )
			{
				isDragging = true;
				dragOffset = co.x;
			}
			else if ( Widget.IsCursorOver( track, cursorPos ) )
			{
				Real newLeft = handle.Left + co.x;
				Real rightBoundary = track.Width - handle.Width;

				handle.Left = Math.Utility.Clamp<Real>( newLeft, rightBoundary, 0 );
				Value = GetSnappedValue( newLeft/rightBoundary );
			}

			base.OnCursorPressed( cursorPos );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="cursorPos"></param>
		public override void OnCursorReleased( Vector2 cursorPos )
		{
			if ( isDragging )
			{
				isDragging = false;
				handle.Left = (int)( ( value - minValue )/( maxValue - minValue )*( track.Width - handle.Width ) );
			}

			base.OnCursorReleased( cursorPos );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="cursorPos"></param>
		public override void OnCursorMoved( Vector2 cursorPos )
		{
			if ( isDragging )
			{
				Vector2 co = Widget.CursorOffset( handle, cursorPos );
				Real newLeft = handle.Left + co.x - dragOffset;
				Real rightBoundary = track.Width - handle.Width;

				handle.Left = Math.Utility.Clamp<Real>( newLeft, rightBoundary, 0 );
				Value = GetSnappedValue( newLeft/rightBoundary );
			}

			base.OnCursorMoved( cursorPos );
		}

		/// <summary>
		/// 
		/// </summary>
		public virtual void OnSliderMoved( object sender, Slider slider )
		{
			if ( SliderMoved != null )
			{
				SliderMoved( sender, slider );
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public override void OnLostFocus()
		{
			isDragging = false;
			base.OnLostFocus();
		}

		/// <summary>
		/// Internal method - given a percentage (from left to right), gets the
		/// value of the nearest marker.
		/// </summary>
		/// <param name="percentage"></param>
		/// <returns></returns>
		protected Real GetSnappedValue( Real percentage )
		{
			percentage = Math.Utility.Clamp<Real>( percentage, 1, 0 );
			var whichMarker = (int)( percentage*( maxValue - minValue )/interval + 0.5 );
			return whichMarker*interval + minValue;
		}

		#endregion
	}
}