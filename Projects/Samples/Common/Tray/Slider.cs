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
		public string ValueCaption { get { return this.valueTextArea.Text; } set { this.valueTextArea.Text = value; } }

		/// <summary>
		/// Gets or sets the current value
		/// </summary>
		public Real Value { get { return value; } set { SetValue( value, true ); } }

		/// <summary>
		/// Gets or sets the caption
		/// </summary>
		public string Caption
		{
			get { return textArea.Text; }
			set
			{
				this.textArea.Text = value;
				if( this.isFitToContents )
				{
					element.Width = GetCaptionWidth( value, this.textArea ) +
					                this.valueTextArea.Parent.Width + this.track.Width + 26;
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
		public Slider( String name, String caption, Real width, Real trackWidth,
		               Real valueBoxWidth, Real minValue, Real maxValue, int snaps )
		{
			this.isDragging = false;
			this.isFitToContents = false;
			element = OverlayManager.Instance.Elements.CreateElementFromTemplate
				( "SdkTrays/Slider", "BorderPanel", name );
			element.Width = width;
			OverlayElementContainer c = (OverlayElementContainer)element;
			this.textArea = (TextArea)c.Children[ Name + "/SliderCaption" ];
			OverlayElementContainer valueBox = (OverlayElementContainer)c.Children[ Name + "/SliderValueBox" ];
			valueBox.Width = valueBoxWidth;
			valueBox.Left = -( valueBoxWidth + 5 );
			this.valueTextArea = (TextArea)valueBox.Children[ valueBox.Name + "/SliderValueText" ];
			this.track = (BorderPanel)c.Children[ Name + "/SliderTrack" ];
			this.handle = (Panel)this.track.Children[ this.track.Name + "/SliderHandle" ];

			if( trackWidth <= 0 ) // tall style
			{
				this.track.Width = width - 16;
			}
			else // long style
			{
				if( width <= 0 )
				{
					this.isFitToContents = true;
				}
				element.Height = 34;
				this.textArea.Top = 10;
				valueBox.Top = 2;
				this.track.Top = -23;
				this.track.Width = trackWidth;
				this.track.HorizontalAlignment = HorizontalAlignment.Right;
				this.track.Left = -( trackWidth + valueBoxWidth + 5 );
			}

			this.Caption = caption;
			this.SetRange( minValue, maxValue, snaps, false );
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

			if( snaps <= 1 || this.minValue >= this.maxValue )
			{
				this.interval = 0;
				this.handle.Hide();
				this.value = minValue;
				if( snaps == 1 )
				{
					this.valueTextArea.Text = this.minValue.ToString();
				}
				else
				{
					this.valueTextArea.Text = "";
				}
			}
			else
			{
				this.handle.Show();
				this.interval = ( maxValue - minValue ) / ( snaps - 1 );
				this.SetValue( minValue, notifyListener );
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="value"></param>
		/// <param name="notifyListener"></param>
		public void SetValue( Real value, bool notifyListener )
		{
			if( this.interval == 0 )
			{
				return;
			}

			this.value = Math.Utility.Clamp<Real>( value, this.maxValue, this.minValue );

			this.ValueCaption = this.value.ToString();

			if( listener != null && notifyListener )
			{
				listener.SliderMoved( this );
			}
			OnSliderMoved( this, this );

			if( !this.isDragging )
			{
				this.handle.Left = (int)( ( this.value - this.minValue ) / ( this.maxValue - this.minValue ) *
				                          ( this.track.Width - this.handle.Width ) );
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="cursorPos"></param>
		public override void OnCursorPressed( Vector2 cursorPos )
		{
			if( !this.handle.IsVisible )
			{
				return;
			}

			Vector2 co = Widget.CursorOffset( this.handle, cursorPos );

			if( co.LengthSquared <= 81 )
			{
				this.isDragging = true;
				this.dragOffset = co.x;
			}
			else if( Widget.IsCursorOver( this.track, cursorPos ) )
			{
				Real newLeft = this.handle.Left + co.x;
				Real rightBoundary = this.track.Width - this.handle.Width;

				this.handle.Left = Math.Utility.Clamp<Real>( newLeft, rightBoundary, 0 );
				Value = this.GetSnappedValue( newLeft / rightBoundary );
			}

			base.OnCursorPressed( cursorPos );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="cursorPos"></param>
		public override void OnCursorReleased( Vector2 cursorPos )
		{
			if( this.isDragging )
			{
				this.isDragging = false;
				this.handle.Left = (int)( ( this.value - this.minValue ) / ( this.maxValue - this.minValue ) *
				                          ( this.track.Width - this.handle.Width ) );
			}

			base.OnCursorReleased( cursorPos );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="cursorPos"></param>
		public override void OnCursorMoved( Vector2 cursorPos )
		{
			if( this.isDragging )
			{
				Vector2 co = Widget.CursorOffset( this.handle, cursorPos );
				Real newLeft = this.handle.Left + co.x - this.dragOffset;
				Real rightBoundary = this.track.Width - this.handle.Width;

				this.handle.Left = Math.Utility.Clamp<Real>( newLeft, rightBoundary, 0 );
				Value = this.GetSnappedValue( newLeft / rightBoundary );
			}

			base.OnCursorMoved( cursorPos );
		}

		/// <summary>
		/// 
		/// </summary>
		virtual public void OnSliderMoved( object sender, Slider slider )
		{
			if( SliderMoved != null )
			{
				SliderMoved( sender, slider );
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public override void OnLostFocus()
		{
			this.isDragging = false;
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
			int whichMarker = (int)( percentage * ( this.maxValue - this.minValue ) / this.interval + 0.5 );
			return whichMarker * this.interval + this.minValue;
		}

		#endregion
	}
}
