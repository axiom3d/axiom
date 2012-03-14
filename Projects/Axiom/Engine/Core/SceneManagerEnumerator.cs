#region LGPL License

/*
Axiom Graphics Engine Library
Copyright © 2003-2011 Axiom Project Team

The overall design, and a majority of the core engine and rendering code
contained within this library is a derivative of the open source Object Oriented
Graphics Engine OGRE, which can be found at http://ogre.sourceforge.net.
Many thanks to the OGRE team for maintaining such a high quality project.

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

#endregion LGPL License

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id: SceneManagerList.cs 1036 2007-04-27 02:56:41Z borrillis $"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System.Collections.Generic;

using Axiom.Collections;
using Axiom.Graphics;

#endregion Namespace Declarations

namespace Axiom.Core
{
	/// <summary>
	///		Factory for default scene manager.
	/// </summary>
	public class SceneManagerDefaultFactory : SceneManagerFactory
	{
		protected override void InitMetaData()
		{
			metaData.typeName = "DefaultSceneManager";
			metaData.description = "The default scene manager";
			metaData.sceneTypeMask = SceneType.Generic;
			metaData.worldGeometrySupported = false;
		}

		public override SceneManager CreateInstance( string name )
		{
			return new DefaultSceneManager( name );
		}

		public override void DestroyInstance( SceneManager instance )
		{
			instance.ClearScene();
		}
	}

	/// <summary>
	///		Default scene manager.
	/// </summary>
	public class DefaultSceneManager : SceneManager
	{
		public DefaultSceneManager( string name )
			: base( name ) { }

		public override string TypeName
		{
			get
			{
				return "DefaultSceneManager";
			}
		}
	}

	/// <summary>
	///     Enumerates the <see cref="SceneManager"/> classes available to applications.
	/// </summary>
	/// <remarks>
	///		As described in the SceneManager class, SceneManagers are responsible
	///     for organising the scene and issuing rendering commands to the
	///     <see cref="RenderSystem"/>. Certain scene types can benefit from different
	///     rendering approaches, and it is intended that subclasses will
	///     be created to special case this.
	/// <p/>
	///     In order to give applications easy access to these implementations,
	///     this class has a number of methods to create or retrieve a SceneManager
	///     which is appropriate to the scene type.
	///	<p/>
	///		SceneManagers are created by <see cref="SceneManagerFactory"/> instances. New factories
	///		for new types of SceneManager can be registered with this class to make
	///		them available to clients.
	///	<p/>
	///		Note that you can still plug in your own custom SceneManager without
	///		using a factory, should you choose, it's just not as flexible that way.
	///		Just instantiate your own SceneManager manually and use it directly.
	/// </remarks>
	public sealed class SceneManagerEnumerator : Singleton<SceneManagerEnumerator>
	{
		public SceneManagerEnumerator()
		{
			this._defaultFactory = new SceneManagerDefaultFactory();
			AddFactory( this._defaultFactory );
		}

		#region Fields

		private readonly SceneManagerDefaultFactory _defaultFactory;
		private readonly List<SceneManagerFactory> _factories = new List<SceneManagerFactory>();
		private readonly SceneManagerCollection _instances = new SceneManagerCollection();
		private readonly List<SceneManagerMetaData> _metaDataList = new List<SceneManagerMetaData>();
		private RenderSystem _currentRenderSystem;

		private ulong _instanceCreateCount;

		#endregion Fields

		#region Public Properties

		/// <summary>
		///		Notifies all SceneManagers of the destination rendering system.
		/// </summary>
		public RenderSystem RenderSytem
		{
			set
			{
				this._currentRenderSystem = value;

				foreach ( SceneManager instance in this._instances.Values )
				{
					instance.TargetRenderSystem = value;
				}
			}
		}

		/// <summary>
		///		A list of all types of SceneManager available for construction,
		///		providing some information about each one.
		/// </summary>
		public List<SceneManagerMetaData> MetaDataList
		{
			get
			{
				return this._metaDataList;
			}
		}

		/// <summary>
		///		A list of all types of SceneManager available for construction,
		///		providing some information about each one.
		/// </summary>
		public SceneManagerCollection SceneManagerList
		{
			get
			{
				return this._instances;
			}
		}

		#endregion Public Properties

		#region Public Methods

		/// <summary>
		///		Registers a new <see cref="SceneManagerFactory"/>.
		/// </summary>
		/// <remarks>
		///		Plugins should call this to register as new <see cref="SceneManager"/> providers.
		/// </remarks>
		public void AddFactory( SceneManagerFactory factory )
		{
			this._factories.Add( factory );
			this._metaDataList.Add( factory.MetaData );

			LogManager.Instance.Write( "SceneManagerFactory for type '{0}' registered", factory.MetaData.typeName );
		}

		///<summary>
		///		Removes a <see cref="SceneManagerFactory"/>.
		///</summary>
		public void RemoveFactory( SceneManagerFactory fact )
		{
			// destroy all instances for this factory
			var tempList = new SceneManagerCollection();
			tempList.AddRange( this._instances );
			foreach ( SceneManager sm in tempList.Values )
			{
				if ( sm.TypeName == fact.MetaData.typeName )
				{
					fact.DestroyInstance( sm );
					this._instances.Remove( sm.Name );
				}
			}

			// remove from metadata
			for ( int i = 0; i < this._metaDataList.Count; i++ )
			{
				if ( this._metaDataList[ i ].Equals( fact.MetaData ) )
				{
					this._metaDataList.Remove( this._metaDataList[ i ] );
					break;
				}
			}

			this._factories.Remove( fact );
		}

		/// <summary>
		///		Creates a SceneManager instance based on scene type support.
		/// </summary>
		/// <remarks>
		///		Creates an instance of a SceneManager which supports the scene types
		///		identified in the parameter. If more than one type of SceneManager
		///		has been registered as handling that combination of scene types,
		///		in instance of the last one registered is returned.
		/// <p/>
		///		Note that this method always succeeds, if a specific scene manager is not
		/// 	found, the default implementation is always returned.
		/// </remarks>
		/// <param name="sceneType">A mask containing one or more <see cref="SceneType"/> flags.</param>
		/// <param name="instanceName">
		///		Optional name to given the new instance that is created.
		///		If you leave this blank, an auto name will be assigned.
		/// </param>
		/// <returns></returns>
		public SceneManager CreateSceneManager( SceneType sceneType, string instanceName )
		{
			if ( this._instances.ContainsKey( instanceName ) )
			{
				throw new AxiomException( "SceneManager instance called '{0}' already exists", instanceName );
			}

			SceneManager instance = null;

			if ( instanceName == string.Empty )
			{
				instanceName = "SceneManagerInstance" + ( ++this._instanceCreateCount ).ToString();
			}

			// iterate backwards to find the matching factory registered last
			for ( int i = this._factories.Count - 1; i > -1; i-- )
			{
				if ( ( this._factories[ i ].MetaData.sceneTypeMask & sceneType ) > 0 )
				{
					instance = this._factories[ i ].CreateInstance( instanceName );
					break;
				}
			}

			// use default factory if none
			if ( instance == null )
			{
				instance = this._defaultFactory.CreateInstance( instanceName );
			}

			// assign render system if already configured
			if ( this._currentRenderSystem != null )
			{
				instance.TargetRenderSystem = this._currentRenderSystem;
			}

			this._instances.Add( instanceName, instance );

			return instance;
		}

		/// <summary>
		///		Creates a SceneManager instance of a given type.
		/// </summary>
		/// <remarks>
		///		You can use this method to create a SceneManager instance of a
		///		given specific type. You may know this type already, or you may
		///		have discovered it by looking at the results from <see cref="SceneManagerEnumerator.MetaDataList"/>.
		/// </remarks>
		/// <param name="typeName">String identifying a unique SceneManager type.</param>
		/// <param name="instanceName">
		///		Optional name to given the new instance that is
		///		created. If you leave this blank, an auto name will be assigned.
		/// </param>
		/// <exception cref="AxiomException">
		///		This method throws an exception if the named type is not found.
		/// </exception>
		/// <returns></returns>
		public SceneManager CreateSceneManager( string typeName, string instanceName )
		{
			if ( this._instances.ContainsKey( instanceName ) )
			{
				throw new AxiomException( "SceneManager instance called '{0}' already exists", instanceName );
			}

			SceneManager instance = null;

			foreach ( SceneManagerFactory factory in this._factories )
			{
				if ( factory.MetaData.typeName == typeName )
				{
					if ( instanceName == string.Empty )
					{
						instanceName = "SceneManagerInstance" + ( ++this._instanceCreateCount ).ToString();
					}

					instance = factory.CreateInstance( instanceName );
					break;
				}
			}

			if ( instance == null )
			{
				throw new AxiomException( "No factory found for scene manager of type '{0}'", typeName );
			}

			// assign render system if already configured
			if ( this._currentRenderSystem != null )
			{
				instance.TargetRenderSystem = this._currentRenderSystem;
			}

			this._instances.Add( instance.Name, instance );

			return instance;
		}


		/// <summary>
		///		Destroys an instance of a SceneManager.
		/// </summary>
		/// <param name="sm"></param>
		public void DestroySceneManager( SceneManager sm )
		{
			// erase instance from list
			this._instances.Remove( sm.Name );

			foreach ( SceneManagerFactory factory in this._factories )
			{
				if ( factory.MetaData.typeName == sm.TypeName )
				{
					factory.DestroyInstance( sm );
				}
			}
		}

		/// <summary>
		/// 	Gets an existing SceneManager instance that has already been created,
		///		identified by the instance name.
		/// </summary>
		/// <param name="instanceName"> The name of the instance to retrieve.</param>
		/// <exception cref="AxiomException">If the instance can't be retrieved.</exception>
		/// <returns></returns>
		public SceneManager GetSceneManager( string instanceName )
		{
			if ( !this._instances.ContainsKey( instanceName ) )
			{
				throw new AxiomException( "SceneManager instance with name '{0}' not found", instanceName );
			}

			SceneManager sceneManager = this._instances[ instanceName ];

			return sceneManager;
		}

		/// <summary>
		/// Identify if a SceneManager instance already exists.
		/// </summary>
		/// <param name="instanceName">The name of the instance to retrieve.</param>
		[OgreVersion( 1, 7, 2 )]
		public bool HasSceneManager( string instanceName )
		{
			return this._instances.ContainsKey( instanceName );
		}

		/// <summary>
		///		Gets more information about a given type of SceneManager.
		/// </summary>
		/// <remarks>
		///		The metadata returned tells you a few things about a given type
		///		of SceneManager, which can be created using a factory that has been
		///		registered already.
		/// </remarks>
		/// <param name="typeName">
		///		The type name of the SceneManager you want to enquire on.
		///		If you don't know the typeName already, you can iterate over the
		///		metadata for all types using getMetaDataIterator.
		/// </param>
		/// <returns></returns>
		public SceneManagerMetaData GetMetaData( string typeName )
		{
			foreach ( SceneManagerMetaData metaData in this._metaDataList )
			{
				if ( typeName == metaData.typeName )
				{
					return metaData;
				}
			}

			throw new AxiomException( "No metadata found for scene manager of type '{0}'.", typeName );
		}

		///<summary>
		///		Shuts down all registered scene managers.
		///</summary>
		public void ShutdownAll()
		{
			foreach ( SceneManager instance in this._instances.Values )
			{
				instance.ClearScene();
			}
		}

		#endregion Public Methods
	}
}
