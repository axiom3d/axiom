#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
*/
#endregion

using System;
using System.Collections;

using Axiom.Enumerations;
using Axiom.SubSystems.Rendering;

namespace Axiom.Core
{
	/// <summary>
	/// Summary description for SceneManagerEnumerator.
	/// </summary>
	public class SceneManagerList
	{
		#region Singleton implementation

		static SceneManagerList() { Init(); }
		protected SceneManagerList() {}
		protected static SceneManagerList instance;

		public static SceneManagerList Instance
		{
			get { return instance; }
		}

		public static void Init()
		{
			instance = new SceneManagerList();

			instance.Initialize();
		}

		#endregion

		private SceneManager defaultSceneManager;
		private Hashtable mSceneManagers = new Hashtable();

		#region Operator overloads

		/// <summary>
		/// Indexer to allow easy access to the scene manager list.
		/// </summary>
		public SceneManager this[SceneType pType]
		{
			get
			{
				return (SceneManager)mSceneManagers[pType];
			}
			set
			{
				mSceneManagers[pType] = value;
			}
		}

		#endregion

		#region Methods

		/// <summary>
		/// Register a new render system with the SceneManagerList.
		/// </summary>
		/// <param name="pSystem"></param>
		public void RegisterRenderSystem(RenderSystem pSystem)
		{
			// loop through each scene manager and set the new render system
			foreach(SceneManager sceneManager in mSceneManagers.Values)
			{
				sceneManager.TargetRenderSystem = pSystem;
			}
		}

		public void Initialize()
		{
			// by default, use the standard scene manager.
			defaultSceneManager = new SceneManager();

			// by default, all scenetypes use the default Scene Manager.  Note: These can be overridden by plugins.
			this[SceneType.Generic] = defaultSceneManager;
			this[SceneType.ExteriorClose] = defaultSceneManager;
			this[SceneType.ExteriorFar] = defaultSceneManager;
			this[SceneType.Interior] = defaultSceneManager;
			this[SceneType.Overhead] = defaultSceneManager;
		}

		#endregion
	}
}
