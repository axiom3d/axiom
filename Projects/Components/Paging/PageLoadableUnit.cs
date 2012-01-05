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

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

using Axiom.Serialization;

#endregion Namespace Declarations

namespace Axiom.Components.Paging
{
	/// <summary>
	/// Status of the Unit.
	/// </summary>
	public enum UnitStatus
	{
		/// <summary>
		/// Just defined, not loaded
		/// </summary>
		Unloaded,

		/// <summary>
		/// In the process of getting data from a stream, or generating it (background thread)
		/// </summary>
		Preparing,

		/// <summary>
		/// At this stage all data has been read, and all non-GPU tasks have been done. 
		/// This is the end of the background thread's involvement.
		/// </summary>
		Prepared,

		/// <summary>
		///  Finalising the load in the main render thread
		/// </summary>
		Loading,

		/// <summary>
		/// Data loaded / generated 
		/// </summary>
		Loaded,

		/// <summary>
		/// Unloading in main render thread (goes back to STATUS_PREPARED)
		/// </summary>
		Unloading,

		/// <summary>
		/// Unpreparing, potentially in a background thread (goes back to STATUS_UNLOADED)
		/// </summary>
		Unpreparing
	};

	/// <summary>
	/// 
	/// </summary>
	abstract public class PageLoadableUnit
	{
		/// <summary>
		/// presents the current unit status.
		/// </summary>
		protected UnitStatus mStatus = UnitStatus.Unloaded;

		/// <summary>
		/// Get's the current status.
		/// </summary>
		virtual public UnitStatus Status { get { return mStatus; } }

		/// <summary>
		/// Get's true if the unit has been fully loaded, false otherwise.
		/// </summary>
		virtual public bool IsLoaded { get { return mStatus == UnitStatus.Loading || mStatus == UnitStatus.Preparing; } }

		/// <summary>
		/// Returns whether the unit is currently in the process of loading.
		/// </summary>
		virtual public bool IsLoading { get { return ( Status == UnitStatus.Loading || Status == UnitStatus.Preparing ); } }

		/// <summary>
		/// Read data from a stream & prepare.
		/// </summary>
		/// <param name="StreamSerializer"></param>
		/// <returns></returns>
		virtual public bool Prepare( StreamSerializer stream )
		{
			if( !ChangeStatus( UnitStatus.Unloaded, UnitStatus.Preparing ) )
			{
				return false;
			}

			bool ret = PrepareImpl( stream );

			if( ret )
			{
				mStatus = UnitStatus.Prepared;
			}
			else
			{
				mStatus = UnitStatus.Unloaded;
			}

			return ret;
		}

		/// <summary>
		/// Finalise the loading of the data.
		/// </summary>
		/// <returns></returns>
		virtual public bool Load()
		{
			if( Status == UnitStatus.Unloaded )
			{
				throw new Exception( "Cannot load befor Prepare() is performed, PageLoadableUnit.Load()" );
			}

			if( !ChangeStatus( UnitStatus.Prepared, UnitStatus.Loading ) )
			{
				return false;
			}

			LoadImpl();

			mStatus = UnitStatus.Loaded;

			return true;
		}

		/// <summary>
		///  Unload the unit, deallocating any GPU resources. 
		/// </summary>
		virtual public void Unload()
		{
			if( !ChangeStatus( UnitStatus.Loaded, UnitStatus.Unloading ) )
			{
				return;
			}

			UnLoadImpl();

			mStatus = UnitStatus.Prepared;
		}

		/// <summary>
		/// Deallocate any background resources.
		/// </summary>
		virtual public void UnPrepare()
		{
			if( !ChangeStatus( UnitStatus.Prepared, UnitStatus.Unpreparing ) )
			{
				return;
			}

			// UnPrepareImpl();

			mStatus = UnitStatus.Unloaded;
		}

		/// <summary>
		/// Manually change a loadable unit's status - advanced use only.
		/// </summary>
		/// <param name="oldStatus"></param>
		/// <param name="newStatus"></param>
		/// <returns></returns>
		virtual public bool ChangeStatus( UnitStatus oldStatus, UnitStatus newStatus )
		{
			// Fast pre-check (no lock)
			if( mStatus != oldStatus )
			{
				return false;
			}

			if( oldStatus == newStatus )
			{
				return false;
			}

			mStatus = newStatus;

			return true;
		}

		/// <summary>
		/// Set this unit's status to STATUS_LOADED without going through the 
		/// load sequence.
		/// </summary>
		virtual public void SetLoaded()
		{
			ChangeStatus( mStatus, UnitStatus.Loaded );
		}

		/// <summary>
		/// Internal method, must be called by subclass destructors.
		/// </summary>
		protected void Destroy()
		{
			// unload if needed (main thread)
			if( Status == UnitStatus.Loaded )
			{
				Unload();
			}
			if( Status == UnitStatus.Prepared )
			{
				UnPrepare();
			}

			Debug.Assert( Status == UnitStatus.Unloaded, "Problems with unloading Unit" );
		}

		/// <summary>
		/// Should be overridden by subclasses to implement 'prepare' action
		/// </summary>
		/// <param name="stream"></param>
		/// <returns></returns>
		virtual protected bool PrepareImpl( StreamSerializer stream )
		{
			return false;
		}

		/// <summary>
		/// Should be overridden by subclasses to implement 'unprepare' action
		/// </summary>
		/// <param name="stream"></param>
		/// <returns></returns>
		virtual protected void UnPrepareImpl() {}

		/// <summary>
		/// Should be overridden by subclasses to implement 'load' action
		/// </summary>
		virtual protected void LoadImpl() {}

		/// <summary>
		/// Should be overridden by subclasses to implement 'unload' action
		/// </summary>
		virtual protected void UnLoadImpl() {}
	}
}
