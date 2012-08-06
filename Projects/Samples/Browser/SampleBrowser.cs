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
using System.Linq;

using Axiom.Core;
using Axiom.Framework.Configuration;
using Axiom.Graphics;
using Axiom.Math;
using Axiom.Overlays;

using SIS = SharpInputSystem;

namespace Axiom.Samples
{
	/// <summary>
	/// The Axiom Sample Browser. Features a menu accessible from all samples,
	/// dynamic configuration, resource reloading, node labeling, and more.
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
		private List<string> LoadedSamplePlugins = new List<string>(); // loaded sample plugins
		private List<string> SampleCategories = new List<string>(); // sample categories
		private SampleSet LoadedSamples = new SampleSet(); // loaded samples
		private SelectMenu CategoryMenu; // sample category select menu
		private SelectMenu SampleMenu; // sample select menu
		private Slider SampleSlider; // sample slider bar
		private Label TitleLabel; // sample title label
		private TextBox DescBox; // sample description box
		private SelectMenu RendererMenu; // render system selection menu
		private List<Overlay> HiddenOverlays = new List<Overlay>(); // sample overlays hidden for pausing
		private List<OverlayElementContainer> Thumbs = new List<OverlayElementContainer>(); // sample thumbnails
		private Real CarouselPlace; // current state of carousel
		private int LastViewTitle; // last sample title viewed
		private int LastViewCategory; // last sample category viewed
		private int LastSampleIndex; // index of last sample running
		private int childIndex = 0;

		public SampleBrowser()
			: base( ConfigurationManagerFactory.CreateDefault() )
		{
			LastSampleIndex = -1;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="s"></param>
		public override void RunSample( Sample s )
		{
			if( CurrentSample != null ) //
			{
				CurrentSample.Shutdown();
				CurrentSample = null;
				IsSamplePaused = false; // don't pause next sample
				// create dummy scene and modify controls
				CreateDummyScene();
				TrayManager.ShowBackdrop( "SdkTrays/Bands" );
				TrayManager.ShowAll();
				( (Button)TrayManager.GetWidget( "StartStop" ) ).Caption = "Start Sample";
			}
			if( s != null ) // sample starting
			{
				( (Button)TrayManager.GetWidget( "StartStop" ) ).Caption = "Stop Sample";
				TrayManager.ShowBackdrop( "SdkTrays/Shade" );
				TrayManager.HideAll();
				DestroyDummyScene();

				try
				{
					base.RunSample( s );
				}
				catch( Exception ex ) // if failed to start, show error and fall back to menu
				{
					s.Shutdown();
					CreateDummyScene();
					TrayManager.ShowBackdrop( "SdkTrays/Bands" );
					TrayManager.ShowAll();
					( (Button)TrayManager.GetWidget( "StartStop" ) ).Caption = "Start Sample";

					TrayManager.ShowOkDialog( "Error!", ex.ToString() + "\nSource " + this.ToString() );
					LogManager.Instance.Write( LogManager.BuildExceptionString( ex ) );

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
			if( !( LoadedSamples.Count == 0 ) && TitleLabel.TrayLocation != TrayLocation.None && ( CurrentSample == null || IsSamplePaused ) )
			{
				// makes the carousel spin smoothly toward its right position
				Real carouselOffset = SampleMenu.SelectionIndex - CarouselPlace;
				if( carouselOffset <= 0.001 && ( carouselOffset >= -0.001 ) )
				{
					CarouselPlace = SampleMenu.SelectionIndex;
				}
				else
				{
					CarouselPlace += carouselOffset * Math.Utility.Clamp<Real>( evt.TimeSinceLastFrame * 15, 1, -1 );
				}

				// update the thumbnail positions based on carousel state
				for( int i = 0; i < Thumbs.Count; i++ )
				{
					Real thumbOffset = CarouselPlace - i;
					Real phase = ( thumbOffset / 2 ) - 2.8;

					if( thumbOffset < -5 || thumbOffset > 4 ) // prevent thumbnails from wrapping around in a circle
					{
						Thumbs[ i ].Hide();
						continue;
					}
					else
					{
						Thumbs[ i ].Show();
					}

					Real left = System.Math.Cos( phase ) * 200;
					Real top = System.Math.Sin( phase ) * 200;
					Real scale = 1.0f / System.Math.Pow( ( System.Math.Abs( thumbOffset ) + 1.0f ), 0.75 );

					OverlayElement[] childs = Thumbs[ i ].Children.Values.ToArray();
					if( childIndex >= childs.Length )
					{
						childIndex = 0;
					}

					Overlays.Elements.BorderPanel frame =
						(Overlays.Elements.BorderPanel)childs[ childIndex++ ];

					Thumbs[ i ].SetDimensions( 128 * scale, 96 * scale );
					frame.SetDimensions( Thumbs[ i ].Width + 16, Thumbs[ i ].Height + 16 );
					Thumbs[ i ].SetPosition( (int)( left - 80 - Thumbs[ i ].Width / 2 ), (int)( top - 5 - Thumbs[ i ].Height / 2 ) );

					if( i == SampleMenu.SelectionIndex )
					{
						frame.BorderMaterialName = "SdkTrays/Frame/Over";
					}
					else
					{
						frame.BorderMaterialName = "SdkTrays/Frame";
					}
				}
			}

			TrayManager.FrameRenderingQueued( evt );

			try
			{
				base.FrameRenderingQueued( sender, evt );
			}
			catch( Exception e ) // show error and fall back to menu
			{
				RunSample( null );
				TrayManager.ShowOkDialog( "Error!", e.ToString() + "\nSource: " + e.StackTrace.ToString() );
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="question"></param>
		/// <param name="yesHit"></param>
		public void YesNoDialogClosed( string question, bool yesHit )
		{
			if( question.Contains( "This will stop" ) && yesHit )
			{
				RunSample( null );
				OnButtonHit( this, (Button)TrayManager.GetWidget( "UnloadReload" ) );
			}
		}

		/// <summary>
		/// Handles button widget events.
		/// </summary>
		/// <param name="b"></param>
		public void OnButtonHit( object sender, Button b )
		{
			if( ButtonHit != null )
			{
				ButtonHit( sender, b );
			}

			if( b.Name == "StartStop" ) // start or stop sample
			{
				if( b.Caption == "Start Sample" )
				{
					if( LoadedSamples.Count == 0 )
					{
						TrayManager.ShowOkDialog( "Error!", "No sample selected!" );
					}
						// use the sample pointer we stored inside the thumbnail
					else
					{
						RunSample( (Sample)( Thumbs[ SampleMenu.SelectionIndex ].UserData ) );
					}
				}
				else
				{
					RunSample( null );
				}
			}
			else if( b.Name == "UnloadReload" ) // unload or reload sample plugins and update controls
			{
				if( b.Caption == "Unload Samples" )
				{
					if( CurrentSample != null )
					{
						TrayManager.ShowYesNoDialog( "Warning!", "This will stop the current sample. Unload anyway?" );
					}
					else
					{
						// save off current view and try to restore it on the next reload
						LastViewTitle = SampleMenu.SelectionIndex;
						LastViewCategory = CategoryMenu.SelectionIndex;

						UnloadSamples();
						PopulateSampleMenus();
						b.Caption = "Reload Samples";
					}
				}
				else
				{
					LoadSamples();
					PopulateSampleMenus();
					if( !( LoadedSamples.Count == 0 ) )
					{
						b.Caption = "Unload Samples";
					}

					try // attempt to restore the last view before unloading samples
					{
						CategoryMenu.SelectItem( LastViewCategory );
						SampleMenu.SelectItem( LastViewTitle );
					}
					catch( Exception )
					{
						// swallowing Exception on purpose
					}
				}
			}
			else if( b.Name == "Configure" ) // enter configuration screen
			{
				TrayManager.RemoveWidgetFromTray( "StartStop" );
				TrayManager.RemoveWidgetFromTray( "UnloadReload" );
				TrayManager.RemoveWidgetFromTray( "Configure" );
				TrayManager.RemoveWidgetFromTray( "Quit" );
				TrayManager.MoveWidgetToTray( "Apply", TrayLocation.Right );
				TrayManager.MoveWidgetToTray( "Back", TrayLocation.Right );

				for( int i = 0; i < Thumbs.Count; i++ )
				{
					Thumbs[ i ].Hide();
				}

				while( TrayManager.TrayContainer[ (int)TrayLocation.Center ].IsVisible )
				{
					TrayManager.RemoveWidgetFromTray( TrayLocation.Center, 0 );
				}

				while( TrayManager.TrayContainer[ (int)TrayLocation.Left ].IsVisible )
				{
					TrayManager.RemoveWidgetFromTray( TrayLocation.Left, 0 );
				}

				TrayManager.MoveWidgetToTray( "ConfigLabel", TrayLocation.Left );
				TrayManager.MoveWidgetToTray( RendererMenu, TrayLocation.Left );
				TrayManager.MoveWidgetToTray( "ConfigSeparator", TrayLocation.Left );

				RendererMenu.SelectItem( Root.RenderSystem.Name );

				WindowResized( RenderWindow );
			}
			else if( b.Name == "Back" ) // leave configuration screen
			{
				while( TrayManager.GetWidgetCount( RendererMenu.TrayLocation ) > 3 )
				{
					TrayManager.DestroyWidget( RendererMenu.TrayLocation, 3 );
				}

				while( TrayManager.GetWidgetCount( TrayLocation.None ) != 0 )
				{
					TrayManager.MoveWidgetToTray( TrayLocation.None, 0, TrayLocation.Left );
				}

				TrayManager.RemoveWidgetFromTray( "Apply" );
				TrayManager.RemoveWidgetFromTray( "Back" );
				TrayManager.RemoveWidgetFromTray( "ConfigLabel" );
				TrayManager.RemoveWidgetFromTray( RendererMenu );
				TrayManager.RemoveWidgetFromTray( "ConfigSeparator" );

				TrayManager.MoveWidgetToTray( "StartStop", TrayLocation.Right );
				TrayManager.MoveWidgetToTray( "UnloadReload", TrayLocation.Right );
				TrayManager.MoveWidgetToTray( "Configure", TrayLocation.Right );
				TrayManager.MoveWidgetToTray( "Quit", TrayLocation.Right );

				WindowResized( RenderWindow );
			}
			else if( b.Name == "Apply" ) // apply any changes made in the configuration screen
			{
				bool reset = false;

				string selectedRenderSystem = string.Empty;
				switch( RendererMenu.SelectedItem )
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
				if( selectedRenderSystem != string.Empty )
				{
					Graphics.Collections.ConfigOptionCollection options =
						Root.RenderSystems[ selectedRenderSystem ].ConfigOptions;

					Axiom.Collections.NameValuePairList newOptions = new Collections.NameValuePairList();
					// collect new settings and decide if a reset is needed

					if( RendererMenu.SelectedItem != Root.RenderSystem.Name )
					{
						reset = true;
					}

					for( int i = 3; i < TrayManager.GetWidgetCount( RendererMenu.TrayLocation ); i++ )
					{
						SelectMenu menu = (SelectMenu)TrayManager.GetWidget( RendererMenu.TrayLocation, i );
						if( menu.SelectedItem != options[ menu.Caption ].Value )
						{
							reset = true;
						}
						newOptions[ menu.Caption ] = menu.SelectedItem;
					}

					// reset with new settings if necessary
					if( reset )
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
		virtual public void ItemSelected( SelectMenu menu )
		{
			if( menu == CategoryMenu ) // category changed, so update the sample menu, carousel, and slider
			{
				for( int i = 0; i < Thumbs.Count; i++ ) // destroy all thumbnails in carousel
				{
					MaterialManager.Instance.Remove( Thumbs[ i ].Name );
					Widget.NukeOverlayElement( Thumbs[ i ] );
				}
				Thumbs.Clear();

				OverlayManager om = OverlayManager.Instance;
				String selectedCategory = string.Empty;

				if( menu.SelectionIndex != -1 )
				{
					selectedCategory = menu.SelectedItem;
				}
				else
				{
					TitleLabel.Caption = "";
					DescBox.Text = "";
				}

				bool all = selectedCategory == "All";
				List<string> sampleTitles = new List<string>();
				Material templateMat = (Material)MaterialManager.Instance.GetByName( "SampleThumbnail" );

				// populate the sample menu and carousel with filtered samples
				foreach( Sample i in LoadedSamples )
				{
					Collections.NameValuePairList info = i.Metadata;

					if( all || info[ "Category" ] == selectedCategory )
					{
						String name = "SampleThumb" + sampleTitles.Count + 1;

						// clone a new material for sample thumbnail
						Material newMat = templateMat.Clone( name );

						TextureUnitState tus = newMat.GetTechnique( 0 ).GetPass( 0 ).GetTextureUnitState( 0 );
						if( ResourceGroupManager.Instance.ResourceExists( "Essential", info[ "Thumbnail" ] ) )
						{
							tus.SetTextureName( info[ "Thumbnail" ] );
						}
						else
						{
							tus.SetTextureName( "thumb_error.png" );
						}

						// create sample thumbnail overlay
						Overlays.Elements.BorderPanel bp = (Overlays.Elements.BorderPanel)om.Elements.CreateElementFromTemplate( "SdkTrays/Picture", "BorderPanel", name );
						bp.HorizontalAlignment = HorizontalAlignment.Right;
						bp.VerticalAlignment = VerticalAlignment.Center;
						bp.MaterialName = name;
						bp.UserData = i;
						TrayManager.TraysLayer.AddElement( bp );

						// add sample thumbnail and title
						Thumbs.Add( bp );
						sampleTitles.Add( i.Metadata[ "Title" ] );
					}
				}

				CarouselPlace = 0; // reset carousel

				SampleMenu.Items = sampleTitles;
				if( SampleMenu.ItemsCount != 0 )
				{
					ItemSelected( SampleMenu );
				}

				SampleSlider.SetRange( 1, sampleTitles.Count, sampleTitles.Count );
			}
			else if( menu == SampleMenu ) // sample changed, so update slider, label and description
			{
				if( SampleSlider.Value != menu.SelectionIndex + 1 )
				{
					SampleSlider.Value = menu.SelectionIndex + 1;
				}

				Sample s = (Sample)( Thumbs[ menu.SelectionIndex ].UserData );
				TitleLabel.Caption = menu.SelectedItem;
				DescBox.Text = "Category: " + s.Metadata[ "Category" ] + "\nDescription: " + s.Metadata[ "Description" ];

				if( CurrentSample != s )
				{
					( (Button)TrayManager.GetWidget( "StartStop" ) ).Caption = "Start Sample";
				}
				else
				{
					( (Button)TrayManager.GetWidget( "StartStop" ) ).Caption = "Stop Sample";
				}
			}
			else if( menu == RendererMenu ) // renderer selected, so update all settings
			{
				while( TrayManager.GetWidgetCount( RendererMenu.TrayLocation ) > 3 )
				{
					TrayManager.DestroyWidget( RendererMenu.TrayLocation, 3 );
				}

				Graphics.Collections.ConfigOptionCollection options = Root.RenderSystems[ menu.SelectionIndex ].ConfigOptions;

				int i = 0;

				// create all the config option select menus
				foreach( Configuration.ConfigOption it in options )
				{
					SelectMenu optionMenu = TrayManager.CreateLongSelectMenu( TrayLocation.Left, "ConfigOption" + i++, it.Name, 450, 240, 10 );
					optionMenu.Items = (List<string>)it.PossibleValues.Values.ToList();

					// if the current config value is not in the menu, add it
					try
					{
						optionMenu.SelectItem( it.Value );
					}
					catch( Exception )
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
		virtual public void SliderMoved( Slider slider )
		{
			// format the caption to be fraction style
			String denom = "/" + SampleMenu.ItemsCount;
			slider.ValueCaption = slider.ValueCaption + denom;

			// tell the sample menu to change if it hasn't already
			if( SampleMenu.SelectionIndex != -1 && SampleMenu.SelectionIndex != slider.Value - 1 )
			{
				SampleMenu.SelectItem( (int)slider.Value - 1 );
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="evt"></param>
		/// <returns></returns>
		public override bool KeyPressed( SIS.KeyEventArgs evt )
		{
			if( TrayManager.IsDialogVisible )
			{
				return true;
			}

			switch( evt.Key )
			{
				case SIS.KeyCode.Key_ESCAPE:
					if( TitleLabel.TrayLocation != TrayLocation.None )
					{
						if( CurrentSample != null )
						{
							if( IsSamplePaused )
							{
								TrayManager.HideAll();
								UnpauseCurrentSample();
							}
							else
							{
								PauseCurrentSample();
								TrayManager.ShowAll();
							}
						}
					}
					else
					{
						OnButtonHit( this, (Button)TrayManager.GetWidget( "Back" ) );
					}
					break;
				case SIS.KeyCode.Key_UP:
				case SIS.KeyCode.Key_DOWN:
				{
					//if ( evt.Key == SIS.KeyCode.Key_DOWN && TitleLabel.TrayLocation != TrayLocation.None )
					//{
					//    break;
					//}

					int newIndex = SampleMenu.SelectionIndex + ( evt.Key == SIS.KeyCode.Key_UP ? -1 : 1 );
					SampleMenu.SelectItem( Utility.Clamp<int>( newIndex, SampleMenu.ItemsCount - 1, 0 ) );
				}
					break;
				case SIS.KeyCode.Key_RETURN:
					if( !( LoadedSamples.Count == 0 ) && ( IsSamplePaused || CurrentSample == null ) )
					{
						Sample newSample = (Sample)Thumbs[ SampleMenu.SelectionIndex ].UserData;
						RunSample( newSample == CurrentSample ? null : newSample );
					}
					break;
				case SIS.KeyCode.Key_F9: // toggle full screen
					Configuration.ConfigOption option = Root.RenderSystem.ConfigOptions[ "Video Mode" ];
					string[] vals = option.Value.Split( 'x' );
					int w = int.Parse( vals[ 0 ] );
					int h = int.Parse( vals[ 1 ].Remove( vals[ 1 ].IndexOf( '@' ), 1 ) );
					RenderWindow.SetFullscreen( !RenderWindow.IsFullScreen, w, h );
					break;
				case SIS.KeyCode.Key_R:
					if( CurrentSample != null )
					{
						switch( RenderWindow.GetViewport( 0 ).Camera.PolygonMode )
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
			catch( Exception ex )
			{
				RunSample( null );
				string msg = ex.Message + "\nSource: " + ex.InnerException;
				LogManager.Instance.Write( "[Samples] Error! " + msg );
				TrayManager.ShowOkDialog( "Error!", msg );
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
			if( TrayManager.InjectMouseDown( evt, id ) )
			{
				return true;
			}

			if( TitleLabel.TrayLocation != TrayLocation.None )
			{
				for( int i = 0; i < Thumbs.Count; i++ )
				{
					if( Thumbs[ i ].IsVisible && Widget.IsCursorOver( Thumbs[ i ],
					                                                  new Vector2( TrayManager.CursorContainer.Left, TrayManager.CursorContainer.Top ), 0 ) )
					{
						SampleMenu.SelectItem( i );
						break;
					}
				}
			}
			try
			{
				return base.MousePressed( evt, id );
			}
			catch( Exception ex )
			{
				RunSample( null );
				string msg = ex.Message + "\nSource: " + ex.InnerException;
				LogManager.Instance.Write( "[Samples] Error! " + msg );
				TrayManager.ShowOkDialog( "Error!", msg );
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
			if( TrayManager.InjectMouseUp( evt, id ) )
			{
				return true;
			}

			try
			{
				return base.MouseReleased( evt, id );
			}
			catch( Exception ex )
			{
				RunSample( null );
				string msg = ex.Message + "\nSource: " + ex.InnerException;
				LogManager.Instance.Write( "[Samples] Error! " + msg );
				TrayManager.ShowOkDialog( "Error!", msg );
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
			if( TrayManager.InjectMouseMove( evt ) )
			{
				return true;
			}

			if( !( CurrentSample != null && !IsSamplePaused ) && TitleLabel.TrayLocation != TrayLocation.None &&
			    evt.State.Z.Relative != 0 && SampleMenu.ItemsCount != 0 )
			{
				int newIndex = (int)( SampleMenu.SelectionIndex - evt.State.Z.Relative / Utility.Abs( evt.State.Z.Relative ) );
				SampleMenu.SelectItem( Utility.Clamp<int>( newIndex, SampleMenu.ItemsCount - 1, 0 ) );
			}
			try
			{
				return base.MouseMoved( evt );
			}
			catch( Exception ex ) // show error and fall back to menu
			{
				RunSample( null );
				string msg = ex.Message + "\nSource: " + ex.InnerException;
				LogManager.Instance.Write( "[Samples] Error! " + msg );
				TrayManager.ShowOkDialog( "Error!", msg );
			}

			return true;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="rw"></param>
		public override void WindowResized( RenderWindow rw )
		{
			if( TrayManager == null )
			{
				return;
			}

			OverlayElementContainer center = TrayManager.TrayContainer[ (int)TrayLocation.Center ];
			OverlayElementContainer left = TrayManager.TrayContainer[ (int)TrayLocation.Left ];

			if( center.IsVisible && rw.Width < 1280 - center.Width )
			{
				while( center.IsVisible )
				{
					TrayManager.MoveWidgetToTray( TrayManager.GetWidget( TrayLocation.Center, 0 ), TrayLocation.Left );
				}
			}
			else if( left.IsVisible && rw.Width >= 1280 - left.Width )
			{
				while( left.IsVisible )
				{
					TrayManager.MoveWidgetToTray( TrayManager.GetWidget( TrayLocation.Left, 0 ), TrayLocation.Center );
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

			TrayManager = new SdkTrayManager( "BrowserControls", RenderWindow, Mouse, this );
			TrayManager.ShowBackdrop( "SdkTrays/Bands" );
			TrayManager.TrayContainer[ (int)TrayLocation.None ].Hide();

			CreateDummyScene();
			LoadResources();

			Sample startupSample = LoadSamples();

			TextureManager.Instance.DefaultMipmapCount = 5;

			// adds context as listener to process context-level (above the sample level) events
			this.Root.FrameStarted += this.FrameStarted;
			this.Root.FrameEnded += this.FrameEnded;
			this.Root.FrameRenderingQueued += this.FrameRenderingQueued;
			WindowEventMonitor.Instance.RegisterListener( RenderWindow, this );

			// create template material for sample thumbnails
			Material thumbMat = (Material)MaterialManager.Instance.Create( "SampleThumbnail", "Essential" );
			thumbMat.GetTechnique( 0 ).GetPass( 0 ).CreateTextureUnitState();

			SetupWidgets();
			WindowResized( RenderWindow ); // adjust menus for resolution

			if( startupSample != null )
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
			TrayManager.ShowLoadingBar( 6, 0 );
			ResourceGroupManager.Instance.InitializeResourceGroup( "Popular" );
			TrayManager.HideLoadingBar();
		}

		/// <summary>
		///  Creates dummy scene to allow rendering GUI in viewport.
		/// </summary>
		virtual protected void CreateDummyScene()
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
		virtual protected Sample LoadSamples()
		{
			string dir = "../samples";
			SampleSet samples = new SampleSet();

			PluginManager.Instance.LoadDirectory( dir );

			foreach( IPlugin plugin in PluginManager.Instance.InstalledPlugins )
			{
				if( plugin is SamplePlugin )
				{
					SamplePlugin pluginInstance = (SamplePlugin)plugin;
					LoadedSamplePlugins.Add( pluginInstance.Name );
					foreach( SdkSample sample in pluginInstance.Samples )
					{
						LoadedSamples.Add( sample );
					}
				}
			}

			foreach( SdkSample sample in LoadedSamples )
			{
				if( !SampleCategories.Contains( sample.Metadata[ "Category" ] ) )
				{
					SampleCategories.Add( sample.Metadata[ "Category" ] );
				}
			}

			if( LoadedSamples.Count > 0 )
			{
				SampleCategories.Add( "All" );
			}
			LoadedSamples.Sort( new SampleComparer() );
			return null;
		}

		/// <summary>
		/// 
		/// </summary>
		virtual protected void UnloadSamples()
		{
			for( int i = 0; i < LoadedSamplePlugins.Count; i++ )
			{
				//mRoot.unloadPlugin(mLoadedSamplePlugins[i]);
			}

			LoadedSamples.Clear();
			LoadedSamplePlugins.Clear();
			SampleCategories.Clear();
		}

		/// <summary>
		/// Sets up main page for browsing samples.
		/// </summary>
		virtual protected void SetupWidgets()
		{
			TrayManager.DestroyAllWidgets();

			// create main navigation tray
			TrayManager.ShowLogo( TrayLocation.Right );
			TrayManager.CreateSeparator( TrayLocation.Right, "LogoSep" );
			TrayManager.CreateButton( TrayLocation.Right, "StartStop", "Start Sample" );
			TrayManager.CreateButton( TrayLocation.Right, "UnloadReload", LoadedSamples.Count == 0 ? "Reload Samples" : "Unload Samples" );
			TrayManager.CreateButton( TrayLocation.Right, "Configure", "Configure" );
			TrayManager.CreateButton( TrayLocation.Right, "Quit", "Quit" );

			// // create sample viewing controls
			TitleLabel = TrayManager.CreateLabel( TrayLocation.Left, "SampleTitle", "" );
			DescBox = TrayManager.CreateTextBox( TrayLocation.Left, "SampleInfo", "Sample Info", 250, 208 );
			CategoryMenu = TrayManager.CreateThickSelectMenu( TrayLocation.Left, "CategoryMenu", "Select Category", 250, 10 );
			SampleMenu = TrayManager.CreateThickSelectMenu( TrayLocation.Left, "SampleMenu", "Select Sample", 250, 10 );
			SampleSlider = TrayManager.CreateThickSlider( TrayLocation.Left, "SampleSlider", "Slide Samples", 250, 80, 0, 0, 0 );

			/* Sliders do not notify their listeners on creation, so we manually call the callback here to format the slider value correctly. */
			SliderMoved( SampleSlider );

			// create configuration screen button tray
			TrayManager.CreateButton( TrayLocation.None, "Apply", "Apply Changes" );
			TrayManager.CreateButton( TrayLocation.None, "Back", "Go Back" );

			// create configuration screen label and renderer menu
			TrayManager.CreateLabel( TrayLocation.None, "ConfigLabel", "Configuration" );
			RendererMenu = TrayManager.CreateLongSelectMenu( TrayLocation.None, "RendererMenu", "Render System", 450, 240, 10 );
			TrayManager.CreateSeparator( TrayLocation.None, "ConfigSeparator" );

			// populate render system names
			var rsNames = from rs in this.Root.RenderSystems.Values
			              select rs.Name;
			RendererMenu.Items = rsNames.ToList();

			PopulateSampleMenus();
		}

		/// <summary>
		/// Populates home menus with loaded samples.
		/// </summary>
		virtual protected void PopulateSampleMenus()
		{
			List<string> categories = new List<string>();
			foreach( string i in SampleCategories )
			{
				categories.Add( i );
			}
			categories.Sort();
			CategoryMenu.Items = categories;
			if( CategoryMenu.ItemsCount != 0 )
			{
				CategoryMenu.SelectItem( 0 );
			}
			else
			{
				ItemSelected( CategoryMenu );
			}
		}

		/// <summary>
		/// Overrides to recover by last sample's index instead.
		/// </summary>
		protected override void RecoverLastSample()
		{
			// restore the view while we're at it too
			CategoryMenu.SelectItem( LastViewCategory );
			SampleMenu.SelectItem( LastViewTitle );
			if( LastSampleIndex != -1 )
			{
				int index = -1;
				foreach( Sample i in LoadedSamples )
				{
					index++;
					if( index == LastSampleIndex )
					{
						RunSample( i );
						i.RestoreState( LastSampleState );
						LastSample = null;
						LastSampleIndex = -1;
						LastSampleState.Clear();
					}
				}

				PauseCurrentSample();
				TrayManager.ShowAll();
			}

			OnButtonHit( this, (Button)TrayManager.GetWidget( "Configure" ) );
		}

		/// <summary>
		/// Overrides to recover by last sample's index instead.
		/// </summary>
		/// <param name="renderer"></param>
		/// <param name="options"></param>
		protected override void Reconfigure( string renderer, Collections.NameValuePairList options )
		{
			LastViewCategory = CategoryMenu.SelectionIndex;
			LastViewTitle = SampleMenu.SelectionIndex;
			LastSampleIndex = -1;
			int index = -1;
			foreach( Sample i in LoadedSamples )
			{
				index++;
				if( i == CurrentSample )
				{
					LastSampleIndex = index;
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
			if( TrayManager != null )
			{
				TrayManager = null;
			}
			if( CurrentSample == null )
			{
				DestroyDummyScene();
			}

			CategoryMenu = null;
			SampleMenu = null;
			SampleSlider = null;
			TitleLabel = null;
			DescBox = null;
			RendererMenu = null;
			HiddenOverlays.Clear();
			Thumbs.Clear();
			CarouselPlace = 0;

			base.Shutdown();

			UnloadSamples();
		}

		/// <summary>
		/// 
		/// </summary>
		virtual protected void DestroyDummyScene()
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

			HiddenOverlays.Clear();
			IEnumerator<Overlay> it = OverlayManager.Instance.Overlays;
			while( it.MoveNext() )
			{
				Overlay o = it.Current;
				if( o.IsVisible )
				{
					HiddenOverlays.Add( o );
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

			foreach( Overlay o in HiddenOverlays )
			{
				o.Show();
			}

			HiddenOverlays.Clear();
		}

		#region ISdkTrayListener Implementation

		/// <summary>
		/// 
		/// </summary>
		/// <param name="message"></param>
		public void OkDialogClosed( string message ) {}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="label"></param>
		public void LabelHit( Label label ) {}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="box"></param>
		public void CheckboxToggled( CheckBox box ) {}

		#endregion ISdkTrayListener Implementation
	}
}
