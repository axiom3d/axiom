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

#endregion

#region SVN Version Information

// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id:$"/>
// </file>

#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Text;

using Axiom.Core;

#endregion Namespace Declarations

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
				{
					instance = new PCZoneFactoryManager();
				}

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
				throw new AxiomException( "No factory found for zone of type '" + zoneType + "' PCZoneFactoryManager.CreatePCZone" );
			}
			return inst;
		}
	}
}
