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

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Linq;
using Axiom.Core;
using Axiom.Framework.Configuration;
using Axiom.Graphics;
using Axiom.Math;
using Axiom.Overlays;
using SIS = SharpInputSystem;

#endregion Namespace Declaration

namespace Axiom.Samples
{
	/// <summary>
	/// The Axiom Sample Browser. Features a menu accessible from all samples,
	/// dynamic configuration, resource reloading, node labelling, and more.
	/// </summary>
	public class SampleBrowser : SampleContext, ISdkTrayListener
	{
		#region events

		/// <summary>
		/// 
		/// </summary>
		public event ButtonHitDelegate ButtonHit;

		#endregion

		protected SdkTrayManager TrayManager; // SDK tray interface
		private readonly List<string> LoadedSamplePlugins = new List<string>(); // loaded sample plugins
		private readonly List<string> SampleCategories = new List<string>(); // sample categories
		private readonly SampleSet LoadedSamples = new SampleSet(); // loaded samples
		private SelectMenu CategoryMenu; // sample category select menu
		private SelectMenu SampleMenu; // sample select menu
		private Slider SampleSlider; // sample slider bar
		private Label TitleLabel; // sample title label
		private TextBox DescBox; // sample description box
		private SelectMenu RendererMenu; // render system selection menu
		private readonly List<Overlay> HiddenOverlays = new List<Overlay>(); // sample overlays hidden for pausing
		private readonly List<OverlayElementContainer> Thumbs = new List<OverlayElementContainer>(); // sample thumbnails
		private Real CarouselPlace; // current state of carousel
		private int LastViewTitle; // last sample title viewed
		private int LastViewCategory; // last sample category viewed
		private int LastSampleIndex; // index of last sample running
		private int childIndex = 0;

		public SampleBrowser()
			: base( ConfigurationManagerFactory.CreateDefault() )
		{
			this.LastSampleIndex = -1;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="s"></param>
		public override void RunSample( Sample s )
		{
			if ( CurrentSample != null ) //
			{
				CurrentSample.Shutdown();
				CurrentSample = null;
				IsSamplePaused = false; // don't pause next sample
				// create dummy scene and modify controls
				CreateDummyScene();
				this.TrayManager.ShowBackdrop( "SdkTrays/Bands" );
				this.TrayManager.ShowAll();
				( (Button)this.TrayManager.GetWidget( "StartStop" ) ).Caption = "Start Sample";
			}
			if ( s != null ) // sample starting
			{
				( (Button)this.TrayManager.GetWidget( "StartStop" ) ).Caption = "Stop Sample";
				this.TrayManager.ShowBackdrop( "SdkTrays/Shade" );
				this.TrayManager.HideAll();
				DestroyDummyScene();

				try
				{
					base.RunSample( s );
				}
				catch ( Exception ex ) // if failed to start, show error and fall back to menu
				{
					s.Shutdown();
					CreateDummyScene();
					this.TrayManager.ShowBackdrop( "SdkTrays/Bands" );
					this.TrayManager.ShowAll();
					( (Button)this.TrayManager.GetWidget( "StartStop" ) ).Caption = "Start Sample";

					this.TrayManager.ShowOkDialog( "Error!", ex.ToString() + "\nSource " + ToString() );
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="evt"></param>
		public override void FrameRenderingQueued( object sender, FrameEventArgs evt )
		{
			// don't do all these calculations when sample's running or when in configuration screen or when no samples loaded
			if ( !( this.LoadedSamples.Count == 0 ) && this.TitleLabel.TrayLocation != TrayLocation.None &&
			     ( CurrentSample == null || IsSamplePaused ) )
			{
				// makes the carousel spin smoothly toward its right position
				Real carouselOffset = this.SampleMenu.SelectionIndex - this.CarouselPlace;
				if ( carouselOffset <= 0.001 && ( carouselOffset >= -0.001 ) )
				{
					this.CarouselPlace = this.SampleMenu.SelectionIndex;
				}
				else
				{
					this.CarouselPlace += carouselOffset*Math.Utility.Clamp<Real>( evt.TimeSinceLastFrame*15, 1, -1 );
				}

				// update the thumbnail positions based on carousel state
				for ( int i = 0; i < this.Thumbs.Count; i++ )
				{
					Real thumbOffset = this.CarouselPlace - i;
					Real phase = ( thumbOffset/2 ) - 2.8;

					if ( thumbOffset < -5 || thumbOffset > 4 ) // prevent thumbnails from wrapping around in a circle
					{
						this.Thumbs[ i ].Hide();
						continue;
					}
					else
					{
						this.Thumbs[ i ].Show();
					}

					Real left = System.Math.Cos( phase )*200;
					Real top = System.Math.Sin( phase )*200;
					Real scale = 1.0f/System.Math.Pow( ( System.Math.Abs( thumbOffset ) + 1.0f ), 0.75 );

					OverlayElement[] childs = this.Thumbs[ i ].Children.Values.ToArray();
					if ( this.childIndex >= childs.Length )
					{
						this.childIndex = 0;
					}

					var frame = (Overlays.Elements.BorderPanel)childs[ this.childIndex++ ];

					this.Thumbs[ i ].SetDimensions( 128*scale, 96*scale );
					frame.SetDimensions( this.Thumbs[ i ].Width + 16, this.Thumbs[ i ].Height + 16 );
					this.Thumbs[ i ].SetPosition( (int)( left - 80 - this.Thumbs[ i ].Width/2 ),
					                              (int)( top - 5 - this.Thumbs[ i ].Height/2 ) );

					if ( i == this.SampleMenu.SelectionIndex )
					{
						frame.BorderMaterialName = "SdkTrays/Frame/Over";
					}
					else
					{
						frame.BorderMaterialName = "SdkTrays/Frame";
					}
				}
			}

			this.TrayManager.FrameRenderingQueued( evt );

			try
			{
				base.FrameRenderingQueued( sender, evt );
			}
			catch ( Exception e ) // show error and fall back to menu
			{
				RunSample( null );
				this.TrayManager.ShowOkDialog( "Error!", e.ToString() + "\nSource: " + e.StackTrace.ToString() );
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="question"></param>
		/// <param name="yesHit"></param>
		public void YesNoDialogClosed( string question, bool yesHit )
		{
			if ( question.Contains( "This will stop" ) && yesHit )
			{
				RunSample( null );
				OnButtonHit( this, (Button)this.TrayManager.GetWidget( "UnloadReload" ) );
			}
		}

		/// <summary>
		/// Handles button widget events.
		/// </summary>
		/// <param name="b"></param>
		public void OnButtonHit( object sender, Button b )
		{
            ButtonHit?.Invoke(sender, b);

            if ( b.Name == "StartStop" ) // start or stop sample
			{
				if ( b.Caption == "Start Sample" )
				{
					if ( this.LoadedSamples.Count == 0 )
					{
						this.TrayManager.ShowOkDialog( "Error!", "No sample selected!" );
					}
						// use the sample pointer we stored inside the thumbnail
					else
					{
						RunSample( (Sample)( this.Thumbs[ this.SampleMenu.SelectionIndex ].UserData ) );
					}
				}
				else
				{
					RunSample( null );
				}
			}
			else if ( b.Name == "UnloadReload" ) // unload or reload sample plugins and update controls
			{
				if ( b.Caption == "Unload Samples" )
				{
					if ( CurrentSample != null )
					{
						this.TrayManager.ShowYesNoDialog( "Warning!", "This will stop the current sample. Unload anyway?" );
					}
					else
					{
						// save off current view and try to restore it on the next reload
						this.LastViewTitle = this.SampleMenu.SelectionIndex;
						this.LastViewCategory = this.CategoryMenu.SelectionIndex;

						UnloadSamples();
						PopulateSampleMenus();
						b.Caption = "Reload Samples";
					}
				}
				else
				{
					LoadSamples();
					PopulateSampleMenus();
					if ( !( this.LoadedSamples.Count == 0 ) )
					{
						b.Caption = "Unload Samples";
					}

					try // attempt to restore the last view before unloading samples
					{
						this.CategoryMenu.SelectItem( this.LastViewCategory );
						this.SampleMenu.SelectItem( this.LastViewTitle );
					}
					catch ( Exception )
					{
						// swallowing Exception on purpose
					}
				}
			}
			else if ( b.Name == "Configure" ) // enter configuration screen
			{
				this.TrayManager.RemoveWidgetFromTray( "StartStop" );
				this.TrayManager.RemoveWidgetFromTray( "UnloadReload" );
				this.TrayManager.RemoveWidgetFromTray( "Configure" );
				this.TrayManager.RemoveWidgetFromTray( "Quit" );
				this.TrayManager.MoveWidgetToTray( "Apply", TrayLocation.Right );
				this.TrayManager.MoveWidgetToTray( "Back", TrayLocation.Right );

				for ( int i = 0; i < this.Thumbs.Count; i++ )
				{
					this.Thumbs[ i ].Hide();
				}

				while ( this.TrayManager.TrayContainer[ (int)TrayLocation.Center ].IsVisible )
				{
					this.TrayManager.RemoveWidgetFromTray( TrayLocation.Center, 0 );
				}

				while ( this.TrayManager.TrayContainer[ (int)TrayLocation.Left ].IsVisible )
				{
					this.TrayManager.RemoveWidgetFromTray( TrayLocation.Left, 0 );
				}

				this.TrayManager.MoveWidgetToTray( "ConfigLabel", TrayLocation.Left );
				this.TrayManager.MoveWidgetToTray( this.RendererMenu, TrayLocation.Left );
				this.TrayManager.MoveWidgetToTray( "ConfigSeparator", TrayLocation.Left );

				this.RendererMenu.SelectItem( Root.RenderSystem.Name );

				WindowResized( RenderWindow );
			}
			else if ( b.Name == "Back" ) // leave configuration screen
			{
				while ( this.TrayManager.GetWidgetCount( this.RendererMenu.TrayLocation ) > 3 )
				{
					this.TrayManager.DestroyWidget( this.RendererMenu.TrayLocation, 3 );
				}

				while ( this.TrayManager.GetWidgetCount( TrayLocation.None ) != 0 )
				{
					this.TrayManager.MoveWidgetToTray( TrayLocation.None, 0, TrayLocation.Left );
				}

				this.TrayManager.RemoveWidgetFromTray( "Apply" );
				this.TrayManager.RemoveWidgetFromTray( "Back" );
				this.TrayManager.RemoveWidgetFromTray( "ConfigLabel" );
				this.TrayManager.RemoveWidgetFromTray( this.RendererMenu );
				this.TrayManager.RemoveWidgetFromTray( "ConfigSeparator" );

				this.TrayManager.MoveWidgetToTray( "StartStop", TrayLocation.Right );
				this.TrayManager.MoveWidgetToTray( "UnloadReload", TrayLocation.Right );
				this.TrayManager.MoveWidgetToTray( "Configure", TrayLocation.Right );
				this.TrayManager.MoveWidgetToTray( "Quit", TrayLocation.Right );

				WindowResized( RenderWindow );
			}
			else if ( b.Name == "Apply" ) // apply any changes made in the configuration screen
			{
				bool reset = false;

				string selectedRenderSystem = string.Empty;
				switch ( this.RendererMenu.SelectedItem )
				{
					case "Axiom DirectX9 Rendering Subsystem":
						selectedRenderSystem = "DirectX9";
						break;
					case "Axiom Xna Rendering Subsystem":
						selectedRenderSystem = "Xna";
						break;
					case "Axiom OpenGL (OpenTK) Rendering Subsystem":
						selectedRenderSystem = "OpenGL";
						break;
					default:
						throw new NotImplementedException();
				}
				if ( selectedRenderSystem != string.Empty )
				{
					var options = Root.RenderSystems[ selectedRenderSystem ].ConfigOptions;

					var newOptions = new Collections.NameValuePairList();
					// collect new settings and decide if a reset is needed

					if ( this.RendererMenu.SelectedItem != Root.RenderSystem.Name )
					{
						reset = true;
					}

					for ( int i = 3; i < this.TrayManager.GetWidgetCount( this.RendererMenu.TrayLocation ); i++ )
					{
						var menu = (SelectMenu)this.TrayManager.GetWidget( this.RendererMenu.TrayLocation, i );
						if ( menu.SelectedItem != options[ menu.Caption ].Value )
						{
							reset = true;
						}
						newOptions[ menu.Caption ] = menu.SelectedItem;
					}

					// reset with new settings if necessary
					if ( reset )
					{
						Reconfigure( selectedRenderSystem, newOptions );
					}
				}
			}
			else
			{
				Root.QueueEndRendering(); // exit browser	
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="menu"></param>
		public virtual void ItemSelected( SelectMenu menu )
		{
			if ( menu == this.CategoryMenu ) // category changed, so update the sample menu, carousel, and slider
			{
				for ( int i = 0; i < this.Thumbs.Count; i++ ) // destroy all thumbnails in carousel
				{
					MaterialManager.Instance.Remove( this.Thumbs[ i ].Name );
					Widget.NukeOverlayElement( this.Thumbs[ i ] );
				}
				this.Thumbs.Clear();

				OverlayManager om = OverlayManager.Instance;
				String selectedCategory = string.Empty;

				if ( menu.SelectionIndex != -1 )
				{
					selectedCategory = menu.SelectedItem;
				}
				else
				{
					this.TitleLabel.Caption = "";
					this.DescBox.Text = "";
				}

				bool all = selectedCategory == "All";
				var sampleTitles = new List<string>();
				var templateMat = (Material)MaterialManager.Instance.GetByName( "SampleThumbnail" );

				// populate the sample menu and carousel with filtered samples
				foreach ( Sample i in this.LoadedSamples )
				{
					Collections.NameValuePairList info = i.Metadata;

					if ( all || info[ "Category" ] == selectedCategory )
					{
						String name = "SampleThumb" + sampleTitles.Count + 1;

						// clone a new material for sample thumbnail
						Material newMat = templateMat.Clone( name );

						TextureUnitState tus = newMat.GetTechnique( 0 ).GetPass( 0 ).GetTextureUnitState( 0 );
						if ( ResourceGroupManager.Instance.ResourceExists( "Essential", info[ "Thumbnail" ] ) )
						{
							tus.SetTextureName( info[ "Thumbnail" ] );
						}
						else
						{
							tus.SetTextureName( "thumb_error.png" );
						}

						// create sample thumbnail overlay
						var bp =
							(Overlays.Elements.BorderPanel)om.Elements.CreateElementFromTemplate( "SdkTrays/Picture", "BorderPanel", name );
						bp.HorizontalAlignment = HorizontalAlignment.Right;
						bp.VerticalAlignment = VerticalAlignment.Center;
						bp.MaterialName = name;
						bp.UserData = i;
						this.TrayManager.TraysLayer.AddElement( bp );

						// add sample thumbnail and title
						this.Thumbs.Add( bp );
						sampleTitles.Add( i.Metadata[ "Title" ] );
					}
				}

				this.CarouselPlace = 0; // reset carousel

				this.SampleMenu.Items = sampleTitles;
				if ( this.SampleMenu.ItemsCount != 0 )
				{
					ItemSelected( this.SampleMenu );
				}

				this.SampleSlider.SetRange( 1, sampleTitles.Count, sampleTitles.Count );
			}
			else if ( menu == this.SampleMenu ) // sample changed, so update slider, label and description
			{
				if ( this.SampleSlider.Value != menu.SelectionIndex + 1 )
				{
					this.SampleSlider.Value = menu.SelectionIndex + 1;
				}

				var s = (Sample)( this.Thumbs[ menu.SelectionIndex ].UserData );
				this.TitleLabel.Caption = menu.SelectedItem;
				this.DescBox.Text = "Category: " + s.Metadata[ "Category" ] + "\nDescription: " + s.Metadata[ "Description" ];

				if ( CurrentSample != s )
				{
					( (Button)this.TrayManager.GetWidget( "StartStop" ) ).Caption = "Start Sample";
				}
				else
				{
					( (Button)this.TrayManager.GetWidget( "StartStop" ) ).Caption = "Stop Sample";
				}
			}
			else if ( menu == this.RendererMenu ) // renderer selected, so update all settings
			{
				while ( this.TrayManager.GetWidgetCount( this.RendererMenu.TrayLocation ) > 3 )
				{
					this.TrayManager.DestroyWidget( this.RendererMenu.TrayLocation, 3 );
				}

				var options = Root.RenderSystems[ menu.SelectionIndex ].ConfigOptions;

				int i = 0;

				// create all the config option select menus
				foreach ( Configuration.ConfigOption it in options )
				{
					SelectMenu optionMenu = this.TrayManager.CreateLongSelectMenu( TrayLocation.Left, "ConfigOption" + i++, it.Name,
					                                                               450,
					                                                               240, 10 );
					optionMenu.Items = (List<string>)it.PossibleValues.Values.ToList();

					// if the current config value is not in the menu, add it
					try
					{
						optionMenu.SelectItem( it.Value );
					}
					catch ( Exception )
					{
						optionMenu.AddItem( it.Value );
						optionMenu.SelectItem( it.Value );
					}
				}

				WindowResized( RenderWindow );
			}
		}

		/// <summary>
		/// Handles sample slider changes.
		/// </summary>
		/// <param name="slider"></param>
		public virtual void SliderMoved( Slider slider )
		{
			// format the caption to be fraction style
			String denom = "/" + this.SampleMenu.ItemsCount;
			slider.ValueCaption = slider.ValueCaption + denom;

			// tell the sample menu to change if it hasn't already
			if ( this.SampleMenu.SelectionIndex != -1 && this.SampleMenu.SelectionIndex != slider.Value - 1 )
			{
				this.SampleMenu.SelectItem( (int)slider.Value - 1 );
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="evt"></param>
		/// <returns></returns>
		public override bool KeyPressed( SIS.KeyEventArgs evt )
		{
			if ( this.TrayManager.IsDialogVisible )
			{
				return true;
			}

			switch ( evt.Key )
			{
				case SIS.KeyCode.Key_ESCAPE:
					if ( this.TitleLabel.TrayLocation != TrayLocation.None )
					{
						if ( CurrentSample != null )
						{
							if ( IsSamplePaused )
							{
								this.TrayManager.HideAll();
								UnpauseCurrentSample();
							}
							else
							{
								PauseCurrentSample();
								this.TrayManager.ShowAll();
							}
						}
					}
					else
					{
						OnButtonHit( this, (Button)this.TrayManager.GetWidget( "Back" ) );
					}
					break;
				case SIS.KeyCode.Key_UP:
				case SIS.KeyCode.Key_DOWN:
				{
					//if ( evt.Key == SIS.KeyCode.Key_DOWN && TitleLabel.TrayLocation != TrayLocation.None )
					//{
					//    break;
					//}

					int newIndex = this.SampleMenu.SelectionIndex + ( evt.Key == SIS.KeyCode.Key_UP ? -1 : 1 );
					this.SampleMenu.SelectItem( Utility.Clamp<int>( newIndex, this.SampleMenu.ItemsCount - 1, 0 ) );
				}
					break;
				case SIS.KeyCode.Key_RETURN:
					if ( !( this.LoadedSamples.Count == 0 ) && ( IsSamplePaused || CurrentSample == null ) )
					{
						var newSample = (Sample)this.Thumbs[ this.SampleMenu.SelectionIndex ].UserData;
						RunSample( newSample == CurrentSample ? null : newSample );
					}
					break;
				case SIS.KeyCode.Key_F9: // toggle full screen
					Configuration.ConfigOption option = Root.RenderSystem.ConfigOptions[ "Video Mode" ];
					string[] vals = option.Value.Split( 'x' );
					int w = int.Parse( vals[ 0 ] );
#if !(XBOX || XBOX360)
					int h = int.Parse( vals[ 1 ].Remove( vals[ 1 ].IndexOf( '@' ) ) );
#else
					int h = int.Parse(vals[1].Remove(vals[1].IndexOf('@'), 1));
#endif
					//RenderWindow.IsFullScreen = ...;
					break;
				case SIS.KeyCode.Key_R:
					if ( CurrentSample != null )
					{
						switch ( RenderWindow.GetViewport( 0 ).Camera.PolygonMode )
						{
							case PolygonMode.Points:
								RenderWindow.GetViewport( 0 ).Camera.PolygonMode = PolygonMode.Solid;
								break;
							case PolygonMode.Solid:
								RenderWindow.GetViewport( 0 ).Camera.PolygonMode = PolygonMode.Wireframe;
								break;
							case PolygonMode.Wireframe:
								RenderWindow.GetViewport( 0 ).Camera.PolygonMode = PolygonMode.Points;
								break;
						}
					}
					break;
			}
			try
			{
				return base.KeyPressed( evt );
			}
			catch ( Exception ex )
			{
				RunSample( null );
				string msg = ex.Message + "\nSource: " + ex.InnerException;
				LogManager.Instance.Write( "[Samples] Error! " + msg );
				this.TrayManager.ShowOkDialog( "Error!", msg );
			}
			return true;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="evt"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		public override bool MousePressed( SIS.MouseEventArgs evt, SIS.MouseButtonID id )
		{
			if ( this.TrayManager.InjectMouseDown( evt, id ) )
			{
				return true;
			}

			if ( this.TitleLabel.TrayLocation != TrayLocation.None )
			{
				for ( int i = 0; i < this.Thumbs.Count; i++ )
				{
					if ( this.Thumbs[ i ].IsVisible &&
					     Widget.IsCursorOver( this.Thumbs[ i ],
					                          new Vector2( this.TrayManager.CursorContainer.Left, this.TrayManager.CursorContainer.Top ),
					                          0 ) )
					{
						this.SampleMenu.SelectItem( i );
						break;
					}
				}
			}
			try
			{
				return base.MousePressed( evt, id );
			}
			catch ( Exception ex )
			{
				RunSample( null );
				string msg = ex.Message + "\nSource: " + ex.InnerException;
				LogManager.Instance.Write( "[Samples] Error! " + msg );
				this.TrayManager.ShowOkDialog( "Error!", msg );
			}
			return true;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="evt"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		public override bool MouseReleased( SIS.MouseEventArgs evt, SIS.MouseButtonID id )
		{
			if ( this.TrayManager.InjectMouseUp( evt, id ) )
			{
				return true;
			}

			try
			{
				return base.MouseReleased( evt, id );
			}
			catch ( Exception ex )
			{
				RunSample( null );
				string msg = ex.Message + "\nSource: " + ex.InnerException;
				LogManager.Instance.Write( "[Samples] Error! " + msg );
				this.TrayManager.ShowOkDialog( "Error!", msg );
			}
			return true;
		}

		/// <summary>
		/// Extends mouseMoved to inject mouse position into tray manager, and checks
		/// for mouse wheel movements to slide the carousel, because we can.
		/// </summary>
		/// <param name="evt"></param>
		/// <returns></returns>
		public override bool MouseMoved( SIS.MouseEventArgs evt )
		{
			if ( this.TrayManager.InjectMouseMove( evt ) )
			{
				return true;
			}

			if ( !( CurrentSample != null && !IsSamplePaused ) && this.TitleLabel.TrayLocation != TrayLocation.None &&
			     evt.State.Z.Relative != 0 && this.SampleMenu.ItemsCount != 0 )
			{
				var newIndex = (int)( this.SampleMenu.SelectionIndex - evt.State.Z.Relative/Utility.Abs( evt.State.Z.Relative ) );
				this.SampleMenu.SelectItem( Utility.Clamp<int>( newIndex, this.SampleMenu.ItemsCount - 1, 0 ) );
			}
			try
			{
				return base.MouseMoved( evt );
			}
			catch ( Exception ex ) // show error and fall back to menu
			{
				RunSample( null );
				string msg = ex.Message + "\nSource: " + ex.InnerException;
				LogManager.Instance.Write( "[Samples] Error! " + msg );
				this.TrayManager.ShowOkDialog( "Error!", msg );
			}

			return true;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="rw"></param>
		public override void WindowResized( RenderWindow rw )
		{
			if ( this.TrayManager == null )
			{
				return;
			}

			OverlayElementContainer center = this.TrayManager.TrayContainer[ (int)TrayLocation.Center ];
			OverlayElementContainer left = this.TrayManager.TrayContainer[ (int)TrayLocation.Left ];

			if ( center.IsVisible && rw.Width < 1280 - center.Width )
			{
				while ( center.IsVisible )
				{
					this.TrayManager.MoveWidgetToTray( this.TrayManager.GetWidget( TrayLocation.Center, 0 ), TrayLocation.Left );
				}
			}
			else if ( left.IsVisible && rw.Width >= 1280 - left.Width )
			{
				while ( left.IsVisible )
				{
					this.TrayManager.MoveWidgetToTray( this.TrayManager.GetWidget( TrayLocation.Left, 0 ), TrayLocation.Center );
				}
			}
			base.WindowResized( rw );
		}

		/// <summary>
		/// 
		/// </summary>
		protected override void Setup()
		{
			CreateWindow();
			SetupInput();
			LocateResources();

			ResourceGroupManager.Instance.InitializeResourceGroup( "Essential" );

			this.TrayManager = new SdkTrayManager( "BrowserControls", RenderWindow, Mouse, this );
			this.TrayManager.ShowBackdrop( "SdkTrays/Bands" );
			this.TrayManager.TrayContainer[ (int)TrayLocation.None ].Hide();

			CreateDummyScene();
			LoadResources();

			Sample startupSample = LoadSamples();

			TextureManager.Instance.DefaultMipmapCount = 5;

			// adds context as listener to process context-level (above the sample level) events
			Root.FrameStarted += FrameStarted;
			Root.FrameEnded += FrameEnded;
			Root.FrameRenderingQueued += FrameRenderingQueued;
			WindowEventMonitor.Instance.RegisterListener( RenderWindow, this );

			// create template material for sample thumbnails
			var thumbMat = (Material)MaterialManager.Instance.Create( "SampleThumbnail", "Essential" );
			thumbMat.GetTechnique( 0 ).GetPass( 0 ).CreateTextureUnitState();

			SetupWidgets();
			WindowResized( RenderWindow ); // adjust menus for resolution

			if ( startupSample != null )
			{
				RunSample( startupSample );
			}
		}

		/// <summary>
		/// 
		/// </summary>
		protected override void CreateWindow()
		{
			base.RenderWindow = base.Root.Initialize( true, "Axiom Sample Browser" );
		}

		/// <summary>
		/// Initializes only the browser's resources and those most commonly used
		/// by samples. This way, additional special content can be initialized by
		/// the samples that use them, so startup time is unaffected.
		/// </summary>
		protected override void LoadResources()
		{
			this.TrayManager.ShowLoadingBar( 6, 0 );
			ResourceGroupManager.Instance.InitializeResourceGroup( "Popular" );
			this.TrayManager.HideLoadingBar();
		}

		/// <summary>
		///  Creates dummy scene to allow rendering GUI in viewport.
		/// </summary>
		protected virtual void CreateDummyScene()
		{
			RenderWindow.RemoveAllViewports();
			SceneManager sm = Root.CreateSceneManager( SceneType.Generic, "DummyScene" );
			Camera cam = sm.CreateCamera( "DummyCamera" );
			Viewport vp = RenderWindow.AddViewport( cam );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		protected virtual Sample LoadSamples()
		{
			string dir = "../samples";
			var samples = new SampleSet();

			PluginManager.Instance.LoadDirectory( dir );

			foreach ( IPlugin plugin in PluginManager.Instance.InstalledPlugins )
			{
                if (plugin is SamplePlugin pluginInstance)
                {
                    this.LoadedSamplePlugins.Add(pluginInstance.Name);
                    foreach (SdkSample sample in pluginInstance.Samples)
                    {
                        this.LoadedSamples.Add(sample);
                    }
                }
            }

			foreach ( SdkSample sample in this.LoadedSamples )
			{
				if ( !this.SampleCategories.Contains( sample.Metadata[ "Category" ] ) )
				{
					this.SampleCategories.Add( sample.Metadata[ "Category" ] );
				}
			}

			if ( this.LoadedSamples.Count > 0 )
			{
				this.SampleCategories.Add( "All" );
			}
			return null;
		}

		/// <summary>
		/// 
		/// </summary>
		protected virtual void UnloadSamples()
		{
			for ( int i = 0; i < this.LoadedSamplePlugins.Count; i++ )
			{
				//mRoot.unloadPlugin(mLoadedSamplePlugins[i]);
			}

			this.LoadedSamples.Clear();
			this.LoadedSamplePlugins.Clear();
			this.SampleCategories.Clear();
		}

		/// <summary>
		/// Sets up main page for browsing samples.
		/// </summary>
		protected virtual void SetupWidgets()
		{
			this.TrayManager.DestroyAllWidgets();

			// create main navigation tray
			this.TrayManager.ShowLogo( TrayLocation.Right );
			this.TrayManager.CreateSeparator( TrayLocation.Right, "LogoSep" );
			this.TrayManager.CreateButton( TrayLocation.Right, "StartStop", "Start Sample" );
			this.TrayManager.CreateButton( TrayLocation.Right, "UnloadReload",
			                               this.LoadedSamples.Count == 0 ? "Reload Samples" : "Unload Samples" );
			this.TrayManager.CreateButton( TrayLocation.Right, "Configure", "Configure" );
			this.TrayManager.CreateButton( TrayLocation.Right, "Quit", "Quit" );

			// // create sample viewing controls
			this.TitleLabel = this.TrayManager.CreateLabel( TrayLocation.Left, "SampleTitle", "" );
			this.DescBox = this.TrayManager.CreateTextBox( TrayLocation.Left, "SampleInfo", "Sample Info", 250, 208 );
			this.CategoryMenu = this.TrayManager.CreateThickSelectMenu( TrayLocation.Left, "CategoryMenu", "Select Category", 250,
			                                                            10 );
			this.SampleMenu = this.TrayManager.CreateThickSelectMenu( TrayLocation.Left, "SampleMenu", "Select Sample", 250, 10 );
			this.SampleSlider = this.TrayManager.CreateThickSlider( TrayLocation.Left, "SampleSlider", "Slide Samples", 250, 80,
			                                                        0, 0, 0 );

			/* Sliders do not notify their listeners on creation, so we manually call the callback here to format the slider value correctly. */
			SliderMoved( this.SampleSlider );

			// create configuration screen button tray
			this.TrayManager.CreateButton( TrayLocation.None, "Apply", "Apply Changes" );
			this.TrayManager.CreateButton( TrayLocation.None, "Back", "Go Back" );

			// create configuration screen label and renderer menu
			this.TrayManager.CreateLabel( TrayLocation.None, "ConfigLabel", "Configuration" );
			this.RendererMenu = this.TrayManager.CreateLongSelectMenu( TrayLocation.None, "RendererMenu", "Render System", 450,
			                                                           240, 10 );
			this.TrayManager.CreateSeparator( TrayLocation.None, "ConfigSeparator" );

			// populate render system names
			var rsNames = from rs in Root.RenderSystems.Values
			              select rs.Name;
			this.RendererMenu.Items = rsNames.ToList();

			PopulateSampleMenus();
		}

		/// <summary>
		/// Populates home menus with loaded samples.
		/// </summary>
		protected virtual void PopulateSampleMenus()
		{
			var categories = new List<string>();
			foreach ( string i in this.SampleCategories )
			{
				categories.Add( i );
			}
			categories.Sort();
			this.CategoryMenu.Items = categories;
			if ( this.CategoryMenu.ItemsCount != 0 )
			{
				this.CategoryMenu.SelectItem( 0 );
			}
			else
			{
				ItemSelected( this.CategoryMenu );
			}
		}

		/// <summary>
		/// Overrides to recover by last sample's index instead.
		/// </summary>
		protected override void RecoverLastSample()
		{
			// restore the view while we're at it too
			this.CategoryMenu.SelectItem( this.LastViewCategory );
			this.SampleMenu.SelectItem( this.LastViewTitle );
			if ( this.LastSampleIndex != -1 )
			{
				int index = -1;
				foreach ( Sample i in this.LoadedSamples )
				{
					index++;
					if ( index == this.LastSampleIndex )
					{
						RunSample( i );
						i.RestoreState( LastSampleState );
						LastSample = null;
						this.LastSampleIndex = -1;
						LastSampleState.Clear();
					}
				}

				PauseCurrentSample();
				this.TrayManager.ShowAll();
			}

			OnButtonHit( this, (Button)this.TrayManager.GetWidget( "Configure" ) );
		}

		/// <summary>
		/// Overrides to recover by last sample's index instead.
		/// </summary>
		/// <param name="renderer"></param>
		/// <param name="options"></param>
		protected override void Reconfigure( string renderer, Collections.NameValuePairList options )
		{
			this.LastViewCategory = this.CategoryMenu.SelectionIndex;
			this.LastViewTitle = this.SampleMenu.SelectionIndex;
			this.LastSampleIndex = -1;
			int index = -1;
			foreach ( Sample i in this.LoadedSamples )
			{
				index++;
				if ( i == CurrentSample )
				{
					this.LastSampleIndex = index;
					break;
				}
			}
			base.Reconfigure( renderer, options );
		}

		/// <summary>
		/// 
		/// </summary>
		protected override void Shutdown()
		{
			if ( this.TrayManager != null )
			{
				this.TrayManager = null;
			}
			if ( CurrentSample == null )
			{
				DestroyDummyScene();
			}

			this.CategoryMenu = null;
			this.SampleMenu = null;
			this.SampleSlider = null;
			this.TitleLabel = null;
			this.DescBox = null;
			this.RendererMenu = null;
			this.HiddenOverlays.Clear();
			this.Thumbs.Clear();
			this.CarouselPlace = 0;

			base.Shutdown();

			UnloadSamples();
		}

		/// <summary>
		/// 
		/// </summary>
		protected virtual void DestroyDummyScene()
		{
			RenderWindow.RemoveAllViewports();
			Root.DestroySceneManager( Root.GetSceneManager( "DummyScene" ) );
		}

		/// <summary>
		/// 
		/// </summary>
		public override void PauseCurrentSample()
		{
			base.PauseCurrentSample();

			this.HiddenOverlays.Clear();
			IEnumerator<Overlay> it = OverlayManager.Instance.Overlays;
			while ( it.MoveNext() )
			{
				Overlay o = it.Current;
				if ( o.IsVisible )
				{
					this.HiddenOverlays.Add( o );
					o.Hide();
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public override void UnpauseCurrentSample()
		{
			base.UnpauseCurrentSample();

			foreach ( Overlay o in this.HiddenOverlays )
			{
				o.Show();
			}

			this.HiddenOverlays.Clear();
		}

		#region ISdkTrayListener Implementation

		/// <summary>
		/// 
		/// </summary>
		/// <param name="message"></param>
		public void OkDialogClosed( string message )
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="label"></param>
		public void LabelHit( Label label )
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="box"></param>
		public void CheckboxToggled( CheckBox box )
		{
		}

		#endregion ISdkTrayListener Implementation
	}
}