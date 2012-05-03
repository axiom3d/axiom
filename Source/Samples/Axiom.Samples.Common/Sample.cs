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

using System.Collections;
using System.Collections.Generic;
using Axiom.Collections;
using Axiom.Core;
using Axiom.Graphics;
using SIS = SharpInputSystem;

namespace Axiom.Samples
{
	/// <summary>
	///   Base class responsible for everything specific to one sample. Designed to be subclassed for each sample.
	/// </summary>
	public class Sample
	{
		/// <summary>
		///   Utility comparison structure for sorting samples using SampleSet.
		/// </summary>
		private class SampleComparer : IComparer
		{
			public int Compare( Sample x, Sample y )
			{
				string titleX;
				string titleY;

				x.Metadata.TryGetValue( "Title", out titleX );
				y.Metadata.TryGetValue( "Title", out titleY );
				/*
				Ogre::NameValuePairList::iterator aTitle = a->getInfo().find("Title");
				Ogre::NameValuePairList::iterator bTitle = b->getInfo().find("Title");
				
				if (aTitle != a->getInfo().end() && bTitle != b->getInfo().end())
					return aTitle->second.compare(bTitle->second) < 0;
				else return false;
				*/
				return 0;
			}

			#region IComparer Members

			public int Compare( object x, object y )
			{
				if ( x == null && y == null )
				{
					return 0;
				}
				if ( x == null )
				{
					return -1;
				}
				if ( y == null )
				{
					return 1;
				}

				if ( x is Sample && y is Sample )
				{
					return Compare( x as Sample, y as Sample );
				}
				return 0;
			}

			#endregion
		}

		#region Fields and Properties

		/// <summary>
		///   Axiom root object
		/// </summary>
		protected Root Root;

		/// <summary>
		///   context render window
		/// </summary>
		protected RenderWindow Window;

		/// <summary>
		///   context keyboard device
		/// </summary>
		protected SIS.Keyboard Keyboard;

		/// <summary>
		///   context mouse device
		/// </summary>
		protected SIS.Mouse Mouse;

		/// <summary>
		///   whether or not resources have been loaded
		/// </summary>
		protected bool ResourcesLoaded;

		/// <summary>
		///   whether or not scene was created
		/// </summary>
		protected bool ContentSetup;

		#region Metadata Property

		protected readonly NameValuePairList _metadata = new NameValuePairList();

		/// <summary>
		///   Retrieves custom sample info.
		/// </summary>
		public NameValuePairList Metadata
		{
			get
			{
				return _metadata;
			}
		}

		#endregion Metadata Property

		#region RequiredRenderSystem Property

		/// <summary>
		///   If this sample requires a specific render system to run, this method will be used to return its name.
		/// </summary>
		public virtual string RequiredRenderSystem
		{
			get
			{
				return string.Empty;
			}
		}

		#endregion RequiredRenderSystem Property

		#region RequiredPlugins Property

		private readonly List<string> _requiredPlugins = new List<string>();
		/// <summary>
		///   If this sample requires specific plugins to run, this method will be used to return their names.
		/// </summary>
		public virtual IList<string> RequiredPlugins
		{
			get
			{
				return _requiredPlugins;
			}
		}

		#endregion RequiredPlugins Property

		#region SceneManager Property

		private SceneManager _sceneManager; // scene manager for this sample

		/// <summary>
		///   <see cref="Axiom.Core.SceneManager" /> for this sample
		/// </summary>
		public SceneManager SceneManager
		{
			get
			{
				return _sceneManager;
			}
			protected set
			{
				_sceneManager = value;
			}
		}

		#endregion SceneManager Property

		#region IsDone Property

		private bool _done; // flag to mark the end of the sample

		/// <summary>
		///   Has the sample ended
		/// </summary>
		public bool IsDone
		{
			get
			{
				return _done;
			}
			protected set
			{
				_done = value;
			}
		}

		#endregion IsDone Property

		#endregion Fields and Properties

		/// <summary>
		/// </summary>
		public Sample()
		{
			Root = Root.Instance;
			Window = null;
			_sceneManager = null;
			_done = true;
			ResourcesLoaded = false;
			ContentSetup = false;
		}

		/// <summary>
		///   Tests to see if target machine meets any special requirements of this sample. Signal a failure by throwing an exception.
		/// </summary>
		/// <param name="capabilities"> </param>
		public virtual void TestCapabilities( RenderSystemCapabilities capabilities )
		{
		}

		/// <summary>
		///   Sets up a sample. Used by the SampleContext class. Do not call directly.
		/// </summary>
		/// <param name="window"> </param>
		/// <param name="keyboard"> </param>
		/// <param name="mouse"> </param>
		protected internal virtual void Setup( RenderWindow window, SIS.Keyboard keyboard, SIS.Mouse mouse )
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

			_done = false;
		}

		/// <summary>
		///   Shuts down a sample. Used by the SampleContext class. Do not call directly.
		/// </summary>
		public virtual void Shutdown()
		{
			if ( _sceneManager != null )
			{
				_sceneManager.ClearScene();
			}

			if ( ContentSetup )
			{
				CleanupContent();
			}
			ContentSetup = false;

			if ( ResourcesLoaded )
			{
				UnloadResources();
			}
			ResourcesLoaded = false;

			if ( _sceneManager != null )
			{
				Root.DestroySceneManager( _sceneManager );
			}
			_sceneManager = null;

			_done = true;
		}

		/*-----------------------------------------------------------------------------
		| Actions to perform when the context stops sending frame listener events
		| and input device events to this sample.
		-----------------------------------------------------------------------------*/

		public virtual void Paused()
		{
		}

		/*-----------------------------------------------------------------------------
		| Actions to perform when the context continues sending frame listener
		| events and input device events to this sample.
		-----------------------------------------------------------------------------*/

		public virtual void Unpaused()
		{
		}

		/*-----------------------------------------------------------------------------
		| Saves the sample state. Optional. Used during reconfiguration.
		-----------------------------------------------------------------------------*/

		public virtual void SaveState( NameValuePairList state )
		{
		}

		/*-----------------------------------------------------------------------------
		| Restores the sample state. Optional. Used during reconfiguration.
		-----------------------------------------------------------------------------*/

		public virtual void RestoreState( NameValuePairList state )
		{
		}

		// callback interface copied from various listeners to be used by SampleContext

		public virtual bool FrameStarted( FrameEventArgs evt )
		{
			return false;
		}

		public virtual bool FrameRenderingQueued( FrameEventArgs evt )
		{
			return false;
		}

		public virtual bool FrameEnded( FrameEventArgs evt )
		{
			return false;
		}

		public virtual void WindowMoved( RenderWindow rw )
		{
		}

		public virtual void WindowResized( RenderWindow rw )
		{
		}

		public virtual bool WindowClosing( RenderWindow rw )
		{
			return true;
		}

		public virtual void WindowClosed( RenderWindow rw )
		{
		}

		public virtual void WindowFocusChange( RenderWindow rw )
		{
		}

		public virtual bool KeyPressed( SIS.KeyEventArgs evt )
		{
			return true;
		}

		public virtual bool KeyReleased( SIS.KeyEventArgs evt )
		{
			return true;
		}

		public virtual bool MouseMoved( SIS.MouseEventArgs evt )
		{
			return true;
		}

		public virtual bool MousePressed( SIS.MouseEventArgs evt, SIS.MouseButtonID id )
		{
			return true;
		}

		public virtual bool MouseReleased( SIS.MouseEventArgs evt, SIS.MouseButtonID id )
		{
			return true;
		}


		/*-----------------------------------------------------------------------------
		| Finds sample-specific resources. No such effort is made for most samples,
		| but this is useful for special samples with large, exclusive resources.
		-----------------------------------------------------------------------------*/

		protected virtual void LocateResources()
		{
		}

		/*-----------------------------------------------------------------------------
		| Loads sample-specific resources. No such effort is made for most samples,
		| but this is useful for special samples with large, exclusive resources.
		-----------------------------------------------------------------------------*/

		protected virtual void LoadResources()
		{
		}

		/*-----------------------------------------------------------------------------
		| Creates a scene manager for the sample. A generic one is the default,
		| but many samples require a special kind of scene manager.
		-----------------------------------------------------------------------------*/

		protected virtual void CreateSceneManager()
		{
			_sceneManager = Root.Instance.CreateSceneManager( "DefaultSceneManager" );
		}

		/*-----------------------------------------------------------------------------
		| Sets up viewport layout and camera.
		-----------------------------------------------------------------------------*/

		protected virtual void SetupView()
		{
		}

		/*-----------------------------------------------------------------------------
		| Sets up the scene (and anything else you want for the sample).
		-----------------------------------------------------------------------------*/

		protected virtual void SetupContent()
		{
		}

		/*-----------------------------------------------------------------------------
		| Cleans up the scene (and anything else you used).
		-----------------------------------------------------------------------------*/

		protected virtual void CleanupContent()
		{
		}

		/*-----------------------------------------------------------------------------
		| Unloads sample-specific resources. My method here is simple and good
		| enough for most small samples, but your needs may vary.
		-----------------------------------------------------------------------------*/

		protected virtual void UnloadResources()
		{
			foreach ( ResourceManager manager in ResourceGroupManager.Instance.ResourceManagers.Values )
			{
				//manager.UnloadUnreferencedResources();
			}
		}
	};

	public class SampleSet : List<Sample>
	{
	}
}