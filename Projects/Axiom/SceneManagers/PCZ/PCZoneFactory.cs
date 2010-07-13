using System;
using System.Collections.Generic;
using System.Text;
using Axiom.Core;

namespace Axiom.SceneManagers.PortalConnected
{
	public abstract class PCZoneFactory
	{
		/// Factory type name
		protected string factoryTypeName;

		public PCZoneFactory( string typeName )
		{
			factoryTypeName = typeName;
		}

		public abstract bool SupportsPCZoneType( string zoneType );
		public abstract PCZone CreatePCZone( PCZSceneManager pczsm, string zoneName );

		public string FactoryTypeName
		{
			get
			{
				return factoryTypeName;
			}
		}
	}

	public class DefaultZoneFactory : PCZoneFactory
	{
		public DefaultZoneFactory( string typeName )
			: base( typeName )
		{
			factoryTypeName = typeName;
		}

		public override bool SupportsPCZoneType( string zoneType )
		{
			return zoneType == factoryTypeName;
		}

		public override PCZone CreatePCZone( PCZSceneManager pczsm, string zoneName )
		{
			return new DefaultZone( pczsm, zoneName );
		}
	}

	public class PCZoneFactoryManager
	{
		private static PCZoneFactoryManager instance;
		private Dictionary<string, PCZoneFactory> pCZoneFactories = new Dictionary<string, PCZoneFactory>();
		private DefaultZoneFactory defaultFactory = new DefaultZoneFactory( "ZoneType_Default" );

		private PCZoneFactoryManager()
		{
			RegisterPCZoneFactory( defaultFactory );
		}

		public static PCZoneFactoryManager Instance
		{
			get
			{
				if ( instance == null )
					instance = new PCZoneFactoryManager();

				return instance;
			}
		}

		public void RegisterPCZoneFactory( PCZoneFactory factory )
		{
			String name = factory.FactoryTypeName;
			pCZoneFactories.Add( name, factory );
			LogManager.Instance.Write( "PCZone Factory Type '" + name + "' registered" );
		}

		public void UnregisterPCZoneFactory( PCZoneFactory factory )
		{
			if ( null != factory )
			{
				//find and remove factory from mPCZoneFactories
				// Note that this does not free the factory from memory, just removes from the factory manager
				string name = factory.FactoryTypeName;
				if ( pCZoneFactories.ContainsKey( name ) )
				{
					pCZoneFactories.Remove( name );
					LogManager.Instance.Write( "PCZone Factory Type '" + name + "' unregistered" );
				}
			}
		}


		public PCZone CreatePCZone( PCZSceneManager pczsm, string zoneType, string zoneName )
		{
			//find a factory that supports this zone type and then call createPCZone() on it
			PCZone inst = null;
			foreach ( PCZoneFactory factory in pCZoneFactories.Values )
			{
				if ( factory.SupportsPCZoneType( zoneType ) )
				{
					// use this factory
					inst = factory.CreatePCZone( pczsm, zoneName );
				}
			}
			if ( null == inst )
			{
				// Error!
				throw new AxiomException( "No factory found for zone of type '" + zoneType +
										 "' PCZoneFactoryManager.CreatePCZone" );
			}
			return inst;
		}
	}
}