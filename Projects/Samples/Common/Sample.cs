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

using System.Collections;
using System.Collections.Generic;

using Axiom.Collections;
using Axiom.Core;
using Axiom.Graphics;

using SIS = SharpInputSystem;

namespace Axiom.Samples
{
	/// <summary>
	/// Base class responsible for everything specific to one sample.
	/// Designed to be subclassed for each sample.
	/// </summary>
	public class Sample
	{
		#region Fields and Properties

		/// <summary>
		/// Axiom root object
		/// </summary>
		protected Root Root;

		/// <summary>
		/// context render window
		/// </summary>
		protected RenderWindow Window;

		/// <summary>
		/// context keyboard device
		/// </summary>
		protected SIS.Keyboard Keyboard;

		/// <summary>
		/// context mouse device
		/// </summary>
		protected SIS.Mouse Mouse;

		/// <summary>
		/// whether or not resources have been loaded
		/// </summary>
		protected bool ResourcesLoaded;

		/// <summary>
		/// whether or not scene was created
		/// </summary>
		protected bool ContentSetup;

		#region Metadata Property

		protected readonly NameValuePairList _metadata = new NameValuePairList();

		/// <summary>
		///  Retrieves custom sample info.
		/// </summary>
		public NameValuePairList Metadata { get { return _metadata; } }

		#endregion Metadata Property

		#region RequiredRenderSystem Property

		/// <summary>
		/// If this sample requires a specific render system to run, this method will be used to return its name.
		/// </summary>
		virtual public string RequiredRenderSystem { get { return string.Empty; } }

		#endregion RequiredRenderSystem Property

		#region RequiredPlugins Property

		/// <summary>
		/// If this sample requires specific plugins to run, this method will be used to return their names.
		/// </summary>
		virtual public IList<string> RequiredPlugins { get { return new List<string>(); } }

		#endregion RequiredPlugins Property

		#region SceneManager Property

		private SceneManager _sceneManager; // scene manager for this sample

		/// <summary>
		/// <see cref="Axiom.Core.SceneManager"/> for this sample
		/// </summary>
		public SceneManager SceneManager { get { return this._sceneManager; } protected set { _sceneManager = value; } }

		#endregion SceneManager Property

		#region IsDone Property

		private bool _done; // flag to mark the end of the sample

		/// <summary>
		/// Has the sample ended
		/// </summary>
		public bool IsDone { get { return this._done; } protected set { _done = value; } }

		#endregion IsDone Property

		#endregion Fields and Properties

		/// <summary>
		/// 
		/// </summary>
		public Sample()
		{
			Root = Root.Instance;
			Window = null;
			this._sceneManager = null;
			this._done = true;
			ResourcesLoaded = false;
			ContentSetup = false;
		}

		/// <summary>
		/// Tests to see if target machine meets any special requirements of this sample. Signal a failure by throwing an exception.
		/// </summary>
		/// <param name="capabilities"></param>
		virtual public void TestCapabilities( RenderSystemCapabilities capabilities ) {}

		/// <summary>
		/// Sets up a sample. Used by the SampleContext class. Do not call directly.
		/// </summary>
		/// <param name="window"></param>
		/// <param name="keyboard"></param>
		/// <param name="mouse"></param>
		virtual protected internal void Setup( RenderWindow window, SIS.Keyboard keyboard, SIS.Mouse mouse )
		{
			Window = window;
			Keyboard = keyboard;
			Mouse = mouse;

			LocateResources();
			CreateSceneManager();
			SetupView();
			LoadResources();
			ResourcesLoaded = true;
			SetupContent();
			ContentSetup = true;

			this._done = false;
		}

		/// <summary>
		/// Shuts down a sample. Used by the SampleContext class. Do not call directly.
		/// </summary>
		virtual public void Shutdown()
		{
			if( this._sceneManager != null )
			{
				this._sceneManager.ClearScene();
			}

			if( ContentSetup )
			{
				CleanupContent();
			}
			ContentSetup = false;

			if( ResourcesLoaded )
			{
				UnloadResources();
			}
			ResourcesLoaded = false;

			if( this._sceneManager != null )
			{
				Root.DestroySceneManager( this._sceneManager );
			}
			this._sceneManager = null;

			this._done = true;
		}

		/*-----------------------------------------------------------------------------
		| Actions to perform when the context stops sending frame listener events
		| and input device events to this sample.
		-----------------------------------------------------------------------------*/

		virtual public void Paused() {}

		/*-----------------------------------------------------------------------------
		| Actions to perform when the context continues sending frame listener
		| events and input device events to this sample.
		-----------------------------------------------------------------------------*/

		virtual public void Unpaused() {}

		/*-----------------------------------------------------------------------------
		| Saves the sample state. Optional. Used during reconfiguration.
		-----------------------------------------------------------------------------*/

		virtual public void SaveState( NameValuePairList state ) {}

		/*-----------------------------------------------------------------------------
		| Restores the sample state. Optional. Used during reconfiguration.
		-----------------------------------------------------------------------------*/

		virtual public void RestoreState( NameValuePairList state ) {}

		// callback interface copied from various listeners to be used by SampleContext

		virtual public bool FrameStarted( FrameEventArgs evt )
		{
			return false;
		}

		virtual public bool FrameRenderingQueued( FrameEventArgs evt )
		{
			return false;
		}

		virtual public bool FrameEnded( FrameEventArgs evt )
		{
			return false;
		}

		virtual public void WindowMoved( RenderWindow rw ) {}

		virtual public void WindowResized( RenderWindow rw ) {}

		virtual public bool WindowClosing( RenderWindow rw )
		{
			return true;
		}

		virtual public void WindowClosed( RenderWindow rw ) {}

		virtual public void WindowFocusChange( RenderWindow rw ) {}

		virtual public bool KeyPressed( SIS.KeyEventArgs evt )
		{
			return true;
		}

		virtual public bool KeyReleased( SIS.KeyEventArgs evt )
		{
			return true;
		}

		virtual public bool MouseMoved( SIS.MouseEventArgs evt )
		{
			return true;
		}

		virtual public bool MousePressed( SIS.MouseEventArgs evt, SIS.MouseButtonID id )
		{
			return true;
		}

		virtual public bool MouseReleased( SIS.MouseEventArgs evt, SIS.MouseButtonID id )
		{
			return true;
		}

		/*-----------------------------------------------------------------------------
		| Finds sample-specific resources. No such effort is made for most samples,
		| but this is useful for special samples with large, exclusive resources.
		-----------------------------------------------------------------------------*/

		virtual protected void LocateResources() {}

		/*-----------------------------------------------------------------------------
		| Loads sample-specific resources. No such effort is made for most samples,
		| but this is useful for special samples with large, exclusive resources.
		-----------------------------------------------------------------------------*/

		virtual protected void LoadResources() {}

		/*-----------------------------------------------------------------------------
		| Creates a scene manager for the sample. A generic one is the default,
		| but many samples require a special kind of scene manager.
		-----------------------------------------------------------------------------*/

		virtual protected void CreateSceneManager()
		{
			this._sceneManager = Root.Instance.CreateSceneManager( "DefaultSceneManager" );
		}

		/*-----------------------------------------------------------------------------
		| Sets up viewport layout and camera.
		-----------------------------------------------------------------------------*/

		virtual protected void SetupView() {}

		/*-----------------------------------------------------------------------------
		| Sets up the scene (and anything else you want for the sample).
		-----------------------------------------------------------------------------*/

		virtual protected void SetupContent() {}

		/*-----------------------------------------------------------------------------
		| Cleans up the scene (and anything else you used).
		-----------------------------------------------------------------------------*/

		virtual protected void CleanupContent() {}

		/*-----------------------------------------------------------------------------
		| Unloads sample-specific resources. My method here is simple and good
		| enough for most small samples, but your needs may vary.
		-----------------------------------------------------------------------------*/

		virtual protected void UnloadResources()
		{
			foreach( ResourceManager manager in ResourceGroupManager.Instance.ResourceManagers.Values )
			{
				//manager.UnloadUnreferencedResources();
			}
		}
	};

	public class SampleSet : List<Sample> {}

	/// <summary>
	/// Utility comparison structure for sorting samples using SampleSet.
	/// </summary>
	public class SampleComparer : IComparer<Sample>
	{
		#region IComparer<Sample> Members

		public int Compare( Sample x, Sample y )
		{
			if( x == null && y == null )
			{
				return 0;
			}
			if( x == null )
			{
				return -1;
			}
			if( y == null )
			{
				return 1;
			}

			string titleX;
			string titleY;

			x.Metadata.TryGetValue( "Title", out titleX );
			y.Metadata.TryGetValue( "Title", out titleY );

			return ( ( new CaseInsensitiveComparer() ).Compare( titleX, titleY ) );
		}

		#endregion IComparer<Sample> Members
	}
}
