using System;

using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;
using Axiom.SceneManagers.PortalConnected;

namespace PCZDemo
{
	public enum RoomWalls
	{
		TOP_WALL,
		BOT_WALL,
		FRONT_WALL,
		BACK_WALL,
		LEFT_WALL,
		RIGHT_WALL
	};

	[Flags]
	internal enum RoomDoors : short
	{
		DOOR_NONE = 0x00,
		DOOR_TOP = 0x01,
		DOOR_BOT = 0x02,
		DOOR_FRONT = 0x04,
		DOOR_BACK = 0x08,
		DOOR_LEFT = 0x10,
		DOOR_RIGHT = 0x20,
		DOOR_ALL = 0xFF
	};


	public class RoomObject
	{
		private static bool init;

		private static int count;
		private readonly Vector3[] points = new Vector3[ 32 ];
		private int mPortalCount;


		public PCZSceneNode createTestBuilding( SceneManager scene, string name )
		{
			count++;
			this.mPortalCount = 0;
			var pczSM = (PCZSceneManager)scene;

			// set points to building exterior size
			createPoints( new Vector3( 60.0f, 40.0f, 60.0f ), new Vector3( 4.0f, 10.0f, 4.0f ) );

			// create the building exterior
			Entity exterior = pczSM.CreateEntity( name + "_building_exterior", "building_exterior.mesh" );

			// make the enclosure a child node of the root scene node
			PCZSceneNode exteriorNode;
			exteriorNode = (PCZSceneNode)scene.RootSceneNode.CreateChildSceneNode( name + "_building_exterior_node", new Vector3( 0.0f, 0.0f, 0.0f ) );
			exteriorNode.AttachObject( exterior );
			pczSM.AddPCZSceneNode( exteriorNode, pczSM.DefaultZone );

			// create portals for the building exterior
			createPortals( scene, exterior, exteriorNode, pczSM.DefaultZone, (short)( RoomDoors.DOOR_FRONT | RoomDoors.DOOR_BACK | RoomDoors.DOOR_LEFT | RoomDoors.DOOR_RIGHT ), true );

			// reset points to room size
			createPoints( new Vector3( 20.0f, 10.0f, 20.0f ), new Vector3( 4.0f, 10.0f, 4.0f ) );

			// create an interior room
			Entity room = pczSM.CreateEntity( name + "_room1", "room_nzpz.mesh" );

			// add the room as a child node to the enclosure node
			PCZSceneNode roomNode;
			roomNode = (PCZSceneNode)exteriorNode.CreateChildSceneNode( name + "_room1_node", new Vector3( 0.0f, 0.0f, 20.0f ) );
			roomNode.AttachObject( room );

			// room needs it's own zone
			string zoneType = "ZoneType_Default";
			string zoneName = name + "_room1_zone";
			PCZone newZone = pczSM.CreateZone( zoneType, zoneName );
			newZone.SetEnclosureNode( roomNode );
			pczSM.AddPCZSceneNode( roomNode, newZone );

			// create portals for the room
			createPortals( scene, room, roomNode, newZone, (short)( RoomDoors.DOOR_FRONT | RoomDoors.DOOR_BACK ), false );

			// create another interior room
			room = pczSM.CreateEntity( name + "_room2", "room_nxpxnypynzpz.mesh" );

			// add the room as a child node to the enclosure node
			roomNode = (PCZSceneNode)exteriorNode.CreateChildSceneNode( name + "_room2_node", new Vector3( 0.0f, 0.0f, 0.0f ) );
			roomNode.AttachObject( room );

			// room needs it's own zone
			zoneName = name + "_room2_zone";
			newZone = pczSM.CreateZone( zoneType, zoneName );
			newZone.SetEnclosureNode( roomNode );
			pczSM.AddPCZSceneNode( roomNode, newZone );

			// create portals for the room
			createPortals( scene, room, roomNode, newZone, (short)( RoomDoors.DOOR_FRONT | RoomDoors.DOOR_BACK | RoomDoors.DOOR_LEFT | RoomDoors.DOOR_RIGHT | RoomDoors.DOOR_TOP | RoomDoors.DOOR_BOT ), false );

			// create another interior room
			room = pczSM.CreateEntity( name + "_room3", "room_nzpz.mesh" );

			// add the room as a child node to the enclosure node
			roomNode = (PCZSceneNode)exteriorNode.CreateChildSceneNode( name + "_room3_node", new Vector3( 0.0f, 0.0f, -20.0f ) );
			roomNode.AttachObject( room );

			// room needs it's own zone
			zoneName = name + "_room3_zone";
			newZone = pczSM.CreateZone( zoneType, zoneName );
			newZone.SetEnclosureNode( roomNode );
			pczSM.AddPCZSceneNode( roomNode, newZone );

			// create portals for the room
			createPortals( scene, room, roomNode, newZone, (short)( RoomDoors.DOOR_FRONT | RoomDoors.DOOR_BACK ), false );

			// create another interior room
			room = pczSM.CreateEntity( name + "_room4", "room_nxpx.mesh" );

			// add the room as a child node to the enclosure node
			roomNode = (PCZSceneNode)exteriorNode.CreateChildSceneNode( name + "_room4_node", new Vector3( -20.0f, 0.0f, 0.0f ) );
			roomNode.AttachObject( room );

			// room needs it's own zone
			zoneName = name + "_room4_zone";
			newZone = pczSM.CreateZone( zoneType, zoneName );
			newZone.SetEnclosureNode( roomNode );
			pczSM.AddPCZSceneNode( roomNode, newZone );

			// create portals for the room
			createPortals( scene, room, roomNode, newZone, (short)( RoomDoors.DOOR_LEFT | RoomDoors.DOOR_RIGHT ), false );

			// create another interior room
			room = pczSM.CreateEntity( name + "_room5", "room_nxpx.mesh" );

			// add the room as a child node to the enclosure node
			roomNode = (PCZSceneNode)exteriorNode.CreateChildSceneNode( name + "_room5_node", new Vector3( 20.0f, 0.0f, 0.0f ) );
			roomNode.AttachObject( room );

			// room needs it's own zone
			zoneName = name + "_room5_zone";
			newZone = pczSM.CreateZone( zoneType, zoneName );
			newZone.SetEnclosureNode( roomNode );
			pczSM.AddPCZSceneNode( roomNode, newZone );

			// create portals for the room
			createPortals( scene, room, roomNode, newZone, (short)( RoomDoors.DOOR_LEFT | RoomDoors.DOOR_RIGHT ), false );

			// create another interior room
			room = pczSM.CreateEntity( name + "_room6", "room_ny.mesh" );

			// add the room as a child node to the enclosure node
			roomNode = (PCZSceneNode)exteriorNode.CreateChildSceneNode( name + "_room6_node", new Vector3( 0.0f, 10.0f, 0.0f ) );
			roomNode.AttachObject( room );

			// room needs it's own zone
			zoneName = name + "_room6_zone";
			newZone = pczSM.CreateZone( zoneType, zoneName );
			newZone.SetEnclosureNode( roomNode );
			pczSM.AddPCZSceneNode( roomNode, newZone );

			// create portals for the room
			createPortals( scene, room, roomNode, newZone, (short)RoomDoors.DOOR_BOT, false );

			// create another interior room
			room = pczSM.CreateEntity( name + "_room7", "room_py.mesh" );

			// add the room as a child node to the enclosure node
			roomNode = (PCZSceneNode)exteriorNode.CreateChildSceneNode( name + "_room7_node", new Vector3( 0.0f, -50.0f, 0.0f ) );
			roomNode.AttachObject( room );

			// room needs it's own zone
			zoneName = name + "_room7_zone";
			newZone = pczSM.CreateZone( zoneType, zoneName );
			newZone.SetEnclosureNode( roomNode );
			pczSM.AddPCZSceneNode( roomNode, newZone );

			// create portals for the room
			createPortals( scene, room, roomNode, newZone, (short)RoomDoors.DOOR_TOP, false );

			// reset points to tall room size
			createPoints( new Vector3( 20.0f, 40.0f, 20.0f ), new Vector3( 4.0f, 10.0f, 4.0f ) );

			// create another interior room
			room = pczSM.CreateEntity( name + "_room8", "room_nypy_4y.mesh" );

			// add the room as a child node to the enclosure node
			roomNode = (PCZSceneNode)exteriorNode.CreateChildSceneNode( name + "_room8_node", new Vector3( 0.0f, -25.0f, 0.0f ) );
			roomNode.AttachObject( room );

			// room needs it's own zone
			zoneName = name + "_room8_zone";
			newZone = pczSM.CreateZone( zoneType, zoneName );
			newZone.SetEnclosureNode( roomNode );
			pczSM.AddPCZSceneNode( roomNode, newZone );

			// create portals for the room
			createPortals( scene, room, roomNode, newZone, (short)( RoomDoors.DOOR_BOT | RoomDoors.DOOR_TOP ), false );


			// resolve portal zone pointers
			pczSM.ConnectPortalsToTargetZonesByLocation();

			return exteriorNode;
		}


		public ManualObject createRoom( SceneManager scene, string name, short doorFlags, bool isEnclosure, Vector3 dimensions, Vector3 doorDimensions )
		{
			addMaterial( name, new ColorEx( .75f, 1f, 1f, 1f ), SceneBlendType.TransparentAlpha );

			ManualObject room = scene.CreateManualObject( name );

			room.Begin( name, OperationType.TriangleList );

			// create points
			createPoints( dimensions, doorDimensions );

			float fade = .5f;
			float solid = .8f;
			var color = new ColorEx( solid, 0, solid, 0 );

			// copy to room
			for ( int i = 0; i < 32; i++ )
			{
				room.Position( this.points[ i ] );
				room.Color( color );
			}

			createWalls( room, doorFlags, isEnclosure );

			room.End();

			return room;
		}

		public void addMaterial( string mat, ColorEx clr, SceneBlendType sbt )
		{
			if ( init )
			{
				return;
			}
			else
			{
				init = true;
			}

			var matptr = (Material)MaterialManager.Instance.Create( mat, "General" );
			matptr.ReceiveShadows = false;
			matptr.GetTechnique( 0 ).LightingEnabled = true;
			matptr.GetTechnique( 0 ).GetPass( 0 ).Diffuse = clr;
			matptr.GetTechnique( 0 ).GetPass( 0 ).Ambient = clr;
			matptr.GetTechnique( 0 ).GetPass( 0 ).SelfIllumination = clr;
			matptr.GetTechnique( 0 ).GetPass( 0 ).SetSceneBlending( sbt );
			matptr.GetTechnique( 0 ).GetPass( 0 ).LightingEnabled = false;
			matptr.GetTechnique( 0 ).GetPass( 0 ).VertexColorTracking = TrackVertexColor.Diffuse;
		}

		public void createPoints( Vector3 dimensions, Vector3 doorDimensions )
		{
			Real l = dimensions.x / 2;
			Real h = dimensions.y / 2;
			Real w = dimensions.z / 2;

			//			 4		 7
			//            *-------*
			//			 /|      /|
			//			/ |		/ |			y
			//		   / 5|	  3/ 6|			|
			//		 0*---*---*---*			*-- x
			//		  |  /    |  /		   /
			//        | /     | /		  z
			//        |/	  |/
			//		 1*-------*2

			this.points[ 0 ] = new Vector3( -l, h, w ); //0
			this.points[ 1 ] = new Vector3( -l, -h, w ); //1
			this.points[ 2 ] = new Vector3( l, -h, w ); //2
			this.points[ 3 ] = new Vector3( l, h, w ); //3

			this.points[ 4 ] = new Vector3( -l, h, -w ); //4
			this.points[ 5 ] = new Vector3( -l, -h, -w ); //5
			this.points[ 6 ] = new Vector3( l, -h, -w ); //6
			this.points[ 7 ] = new Vector3( l, h, -w ); //7

			// doors
			Real l2 = doorDimensions.x / 2;
			Real h2 = doorDimensions.y / 2;
			Real w2 = doorDimensions.z / 2;

			// front door
			this.points[ 8 ] = new Vector3( -l2, h2, w ); //8
			this.points[ 9 ] = new Vector3( -l2, -h2, w ); //9
			this.points[ 10 ] = new Vector3( l2, -h2, w ); //10
			this.points[ 11 ] = new Vector3( l2, h2, w ); //11

			// back door
			this.points[ 12 ] = new Vector3( -l2, h2, -w ); //12
			this.points[ 13 ] = new Vector3( -l2, -h2, -w ); //13
			this.points[ 14 ] = new Vector3( l2, -h2, -w ); //14
			this.points[ 15 ] = new Vector3( l2, h2, -w ); //15

			// top door
			this.points[ 16 ] = new Vector3( -l2, h, -w2 ); //16
			this.points[ 17 ] = new Vector3( -l2, h, w2 ); //17
			this.points[ 18 ] = new Vector3( l2, h, w2 ); //18
			this.points[ 19 ] = new Vector3( l2, h, -w2 ); //19

			// bottom door
			this.points[ 20 ] = new Vector3( -l2, -h, -w2 ); //20
			this.points[ 21 ] = new Vector3( -l2, -h, w2 ); //21
			this.points[ 22 ] = new Vector3( l2, -h, w2 ); //22
			this.points[ 23 ] = new Vector3( l2, -h, -w2 ); //23

			// left door
			this.points[ 24 ] = new Vector3( -l, h2, w2 ); //24
			this.points[ 25 ] = new Vector3( -l, -h2, w2 ); //25
			this.points[ 26 ] = new Vector3( -l, -h2, -w2 ); //26
			this.points[ 27 ] = new Vector3( -l, h2, -w2 ); //27

			// right door
			this.points[ 28 ] = new Vector3( l, h2, w2 ); //28
			this.points[ 29 ] = new Vector3( l, -h2, w2 ); //29
			this.points[ 30 ] = new Vector3( l, -h2, -w2 ); //30
			this.points[ 31 ] = new Vector3( l, h2, -w2 ); //31
		}

		public void createWalls( ManualObject room, short doorFlags, bool isEnclosure )
		{
			if ( isEnclosure )
			{
				if ( ( doorFlags & (short)RoomDoors.DOOR_FRONT ) != 0 )
				{
					// make front wall outward facing with door
					room.Quad( 0, 8, 11, 3 );
					room.Quad( 1, 9, 8, 0 );
					room.Quad( 2, 10, 9, 1 );
					room.Quad( 3, 11, 10, 2 );
				}
				else
				{
					// make front wall outward facing without door
					room.Quad( 0, 1, 2, 3 );
				}
				if ( ( doorFlags & (short)RoomDoors.DOOR_BACK ) != 0 )
				{
					// make back wall outward facing with door
					room.Quad( 7, 15, 12, 4 );
					room.Quad( 6, 14, 15, 7 );
					room.Quad( 5, 13, 14, 6 );
					room.Quad( 4, 12, 13, 5 );
				}
				else
				{
					// make back wall outward facing without door
					room.Quad( 7, 6, 5, 4 );
				}
				if ( ( doorFlags & (short)RoomDoors.DOOR_TOP ) != 0 )
				{
					// make top wall outward facing with door
					room.Quad( 0, 17, 16, 4 );
					room.Quad( 4, 16, 19, 7 );
					room.Quad( 7, 19, 18, 3 );
					room.Quad( 3, 18, 17, 0 );
				}
				else
				{
					// make top wall outward facing without door
					room.Quad( 0, 3, 7, 4 );
				}
				if ( ( doorFlags & (short)RoomDoors.DOOR_BOT ) != 0 )
				{
					// make bottom wall outward facing with door
					room.Quad( 5, 20, 21, 1 );
					room.Quad( 6, 23, 20, 5 );
					room.Quad( 2, 22, 23, 6 );
					room.Quad( 1, 21, 22, 2 );
				}
				else
				{
					// make bottom wall outward facing without door
					room.Quad( 2, 1, 5, 6 );
				}
				if ( ( doorFlags & (short)RoomDoors.DOOR_LEFT ) != 0 )
				{
					// make left wall outward facing with door
					room.Quad( 0, 24, 25, 1 );
					room.Quad( 4, 27, 24, 0 );
					room.Quad( 5, 26, 27, 4 );
					room.Quad( 1, 25, 26, 5 );
				}
				else
				{
					// make left side wall outward facing without door
					room.Quad( 1, 0, 4, 5 );
				}
				if ( ( doorFlags & (short)RoomDoors.DOOR_RIGHT ) != 0 )
				{
					// make right wall outward facing with door
					room.Quad( 2, 29, 28, 3 );
					room.Quad( 6, 30, 29, 2 );
					room.Quad( 7, 31, 30, 6 );
					room.Quad( 3, 28, 31, 7 );
				}
				else
				{
					// make right side wall outward facing without door
					room.Quad( 3, 2, 6, 7 );
				}
			}
			else
			{
				// front back
				if ( ( doorFlags & (short)RoomDoors.DOOR_FRONT ) != 0 )
				{
					// make front wall inward facing with door
					room.Quad( 3, 11, 8, 0 );
					room.Quad( 0, 8, 9, 1 );
					room.Quad( 1, 9, 10, 2 );
					room.Quad( 2, 10, 11, 3 );
				}
				else
				{
					// make front wall inward facing without door
					room.Quad( 3, 2, 1, 0 );
				}
				if ( ( doorFlags & (short)RoomDoors.DOOR_BACK ) != 0 )
				{
					// make back wall inward facing with door
					room.Quad( 4, 12, 15, 7 );
					room.Quad( 7, 15, 14, 6 );
					room.Quad( 6, 14, 13, 5 );
					room.Quad( 5, 13, 12, 4 );
				}
				else
				{
					// make back wall inward facing without door
					room.Quad( 4, 5, 6, 7 );
				}
				// top bottom
				if ( ( doorFlags & (short)RoomDoors.DOOR_TOP ) != 0 )
				{
					// make top wall inward facing with door
					room.Quad( 4, 16, 17, 0 );
					room.Quad( 7, 19, 16, 4 );
					room.Quad( 3, 18, 19, 7 );
					room.Quad( 0, 17, 18, 3 );
				}
				else
				{
					// make top wall inward facing without door
					room.Quad( 4, 7, 3, 0 );
				}
				if ( ( doorFlags & (short)RoomDoors.DOOR_BOT ) != 0 )
				{
					// make bottom wall inward facing with door
					room.Quad( 1, 21, 20, 5 );
					room.Quad( 5, 20, 23, 6 );
					room.Quad( 6, 23, 22, 2 );
					room.Quad( 2, 22, 21, 1 );
				}
				else
				{
					// make bottom wall inward facing without door
					room.Quad( 6, 5, 1, 2 );
				}
				// end caps
				if ( ( doorFlags & (short)RoomDoors.DOOR_LEFT ) != 0 )
				{
					// make left wall inward facing with door
					room.Quad( 1, 25, 24, 0 );
					room.Quad( 0, 24, 27, 4 );
					room.Quad( 4, 27, 26, 5 );
					room.Quad( 5, 26, 25, 1 );
				}
				else
				{
					// make left side wall inward facing without door
					room.Quad( 5, 4, 0, 1 );
				}
				if ( ( doorFlags & (short)RoomDoors.DOOR_RIGHT ) != 0 )
				{
					// make right wall inward facing with door
					room.Quad( 3, 28, 29, 2 );
					room.Quad( 2, 29, 30, 6 );
					room.Quad( 6, 30, 31, 7 );
					room.Quad( 7, 31, 28, 3 );
				}
				else
				{
					// make right side wall inward facing without door
					room.Quad( 7, 6, 2, 3 );
				}
			}
		}

		// Create portals for every door
		public void createPortals( SceneManager scene, ManualObject room, SceneNode roomNode, PCZone zone, short doorFlags, bool isEnclosure )
		{
			string portalName;
			var corners = new Vector3[ 4 ];

			if ( isEnclosure )
			{
				if ( ( doorFlags & (short)RoomDoors.DOOR_FRONT ) != 0 )
				{
					// set the corners to the front door corners
					corners[ 0 ] = this.points[ 8 ];
					corners[ 1 ] = this.points[ 9 ];
					corners[ 2 ] = this.points[ 10 ];
					corners[ 3 ] = this.points[ 11 ];
					// create the portal
					portalName = room.Name + "_FrontDoorPortal";
					Portal p = ( (PCZSceneManager)scene ).CreatePortal( portalName, PORTAL_TYPE.PORTAL_TYPE_QUAD );
					p.setCorners( corners );
					// associate the portal with the roomnode
					p.setNode( roomNode );
					// add the portal to the zone
					zone.AddPortal( p );
					// update derived values for the portal
					p.updateDerivedValues();
				}
				if ( ( doorFlags & (short)RoomDoors.DOOR_BACK ) != 0 )
				{
					// set the corners to the front door corners
					corners[ 0 ] = this.points[ 15 ];
					corners[ 1 ] = this.points[ 14 ];
					corners[ 2 ] = this.points[ 13 ];
					corners[ 3 ] = this.points[ 12 ];
					// create the portal
					portalName = room.Name + ( "_BackDoorPortal" );
					Portal p = ( (PCZSceneManager)scene ).CreatePortal( portalName, PORTAL_TYPE.PORTAL_TYPE_QUAD );
					p.setCorners( corners );
					// associate the portal with the roomnode
					p.setNode( roomNode );
					// add the portal to the zone
					zone.AddPortal( p );
					// update derived values for the portal
					p.updateDerivedValues();
				}
				if ( ( doorFlags & (short)RoomDoors.DOOR_TOP ) != 0 )
				{
					// set the corners to the front door corners
					corners[ 0 ] = this.points[ 16 ];
					corners[ 1 ] = this.points[ 17 ];
					corners[ 2 ] = this.points[ 18 ];
					corners[ 3 ] = this.points[ 19 ];
					// create the portal
					portalName = room.Name + ( "_TopDoorPortal" );
					Portal p = ( (PCZSceneManager)scene ).CreatePortal( portalName, PORTAL_TYPE.PORTAL_TYPE_QUAD );
					p.setCorners( corners );
					// associate the portal with the roomnode
					p.setNode( roomNode );
					// add the portal to the zone
					zone.AddPortal( p );
					// update derived values for the portal
					p.updateDerivedValues();
				}
				if ( ( doorFlags & (short)RoomDoors.DOOR_BOT ) != 0 )
				{
					// set the corners to the front door corners
					corners[ 0 ] = this.points[ 23 ];
					corners[ 1 ] = this.points[ 22 ];
					corners[ 2 ] = this.points[ 21 ];
					corners[ 3 ] = this.points[ 20 ];
					// create the portal
					portalName = room.Name + ( "_BottomDoorPortal" );
					Portal p = ( (PCZSceneManager)scene ).CreatePortal( portalName, PORTAL_TYPE.PORTAL_TYPE_QUAD );
					p.setCorners( corners );
					// associate the portal with the roomnode
					p.setNode( roomNode );
					// add the portal to the zone
					zone.AddPortal( p );
					// update derived values for the portal
					p.updateDerivedValues();
				}
				if ( ( doorFlags & (short)RoomDoors.DOOR_LEFT ) != 0 )
				{
					// set the corners to the front door corners
					corners[ 0 ] = this.points[ 27 ];
					corners[ 1 ] = this.points[ 26 ];
					corners[ 2 ] = this.points[ 25 ];
					corners[ 3 ] = this.points[ 24 ];
					// create the portal
					portalName = room.Name + ( "_LeftDoorPortal" );
					Portal p = ( (PCZSceneManager)scene ).CreatePortal( portalName, PORTAL_TYPE.PORTAL_TYPE_QUAD );
					p.setCorners( corners );
					// associate the portal with the roomnode
					p.setNode( roomNode );
					// add the portal to the zone
					zone.AddPortal( p );
					// update derived values for the portal
					p.updateDerivedValues();
				}
				if ( ( doorFlags & (short)RoomDoors.DOOR_RIGHT ) != 0 )
				{
					// set the corners to the front door corners
					corners[ 0 ] = this.points[ 28 ];
					corners[ 1 ] = this.points[ 29 ];
					corners[ 2 ] = this.points[ 30 ];
					corners[ 3 ] = this.points[ 31 ];
					// create the portal
					portalName = room.Name + ( "_RightDoorPortal" );
					Portal p = ( (PCZSceneManager)scene ).CreatePortal( portalName, PORTAL_TYPE.PORTAL_TYPE_QUAD );
					p.setCorners( corners );
					// associate the portal with the roomnode
					p.setNode( roomNode );
					// add the portal to the zone
					zone.AddPortal( p );
					// update derived values for the portal
					p.updateDerivedValues();
				}
			}
			else
			{
				if ( ( doorFlags & (short)RoomDoors.DOOR_FRONT ) != 0 )
				{
					// set the corners to the front door corners
					corners[ 0 ] = this.points[ 11 ];
					corners[ 1 ] = this.points[ 10 ];
					corners[ 2 ] = this.points[ 9 ];
					corners[ 3 ] = this.points[ 8 ];
					// create the portal
					portalName = room.Name + ( "_FrontDoorPortal" );
					Portal p = ( (PCZSceneManager)scene ).CreatePortal( portalName, PORTAL_TYPE.PORTAL_TYPE_QUAD );
					p.setCorners( corners );
					// associate the portal with the roomnode
					p.setNode( roomNode );
					// add the portal to the zone
					zone.AddPortal( p );
					// update derived values for the portal
					p.updateDerivedValues();
				}
				if ( ( doorFlags & (short)RoomDoors.DOOR_BACK ) != 0 )
				{
					// set the corners to the front door corners
					corners[ 0 ] = this.points[ 12 ];
					corners[ 1 ] = this.points[ 13 ];
					corners[ 2 ] = this.points[ 14 ];
					corners[ 3 ] = this.points[ 15 ];
					// create the portal
					portalName = room.Name + ( "_BackDoorPortal" );
					Portal p = ( (PCZSceneManager)scene ).CreatePortal( portalName, PORTAL_TYPE.PORTAL_TYPE_QUAD );
					p.setCorners( corners );
					// associate the portal with the roomnode
					p.setNode( roomNode );
					// add the portal to the zone
					zone.AddPortal( p );
					// update derived values for the portal
					p.updateDerivedValues();
				}
				if ( ( doorFlags & (short)RoomDoors.DOOR_TOP ) != 0 )
				{
					// set the corners to the front door corners
					corners[ 0 ] = this.points[ 19 ];
					corners[ 1 ] = this.points[ 18 ];
					corners[ 2 ] = this.points[ 17 ];
					corners[ 3 ] = this.points[ 16 ];
					// create the portal
					portalName = room.Name + ( "_TopDoorPortal" );
					Portal p = ( (PCZSceneManager)scene ).CreatePortal( portalName, PORTAL_TYPE.PORTAL_TYPE_QUAD );
					p.setCorners( corners );
					// associate the portal with the roomnode
					p.setNode( roomNode );
					// add the portal to the zone
					zone.AddPortal( p );
					// update derived values for the portal
					p.updateDerivedValues();
				}
				if ( ( doorFlags & (short)RoomDoors.DOOR_BOT ) != 0 )
				{
					// set the corners to the front door corners
					corners[ 0 ] = this.points[ 20 ];
					corners[ 1 ] = this.points[ 21 ];
					corners[ 2 ] = this.points[ 22 ];
					corners[ 3 ] = this.points[ 23 ];
					// create the portal
					portalName = room.Name + ( "_BottomDoorPortal" );
					Portal p = ( (PCZSceneManager)scene ).CreatePortal( portalName, PORTAL_TYPE.PORTAL_TYPE_QUAD );
					p.setCorners( corners );
					// associate the portal with the roomnode
					p.setNode( roomNode );
					// add the portal to the zone
					zone.AddPortal( p );
					// update derived values for the portal
					p.updateDerivedValues();
				}
				if ( ( doorFlags & (short)RoomDoors.DOOR_LEFT ) != 0 )
				{
					// set the corners to the front door corners
					corners[ 0 ] = this.points[ 24 ];
					corners[ 1 ] = this.points[ 25 ];
					corners[ 2 ] = this.points[ 26 ];
					corners[ 3 ] = this.points[ 27 ];
					// create the portal
					portalName = room.Name + ( "_LeftDoorPortal" );
					Portal p = ( (PCZSceneManager)scene ).CreatePortal( portalName, PORTAL_TYPE.PORTAL_TYPE_QUAD );
					p.setCorners( corners );
					// associate the portal with the roomnode
					p.setNode( roomNode );
					// add the portal to the zone
					zone.AddPortal( p );
					// update derived values for the portal
					p.updateDerivedValues();
				}
				if ( ( doorFlags & (short)RoomDoors.DOOR_RIGHT ) != 0 )
				{
					// set the corners to the front door corners
					corners[ 0 ] = this.points[ 31 ];
					corners[ 1 ] = this.points[ 30 ];
					corners[ 2 ] = this.points[ 29 ];
					corners[ 3 ] = this.points[ 28 ];
					// create the portal
					portalName = room.Name + ( "_RightDoorPortal" );
					Portal p = ( (PCZSceneManager)scene ).CreatePortal( portalName, PORTAL_TYPE.PORTAL_TYPE_QUAD );
					p.setCorners( corners );
					// associate the portal with the roomnode
					p.setNode( roomNode );
					// add the portal to the zone
					zone.AddPortal( p );
					// update derived values for the portal
					p.updateDerivedValues();
				}
			}
		}

		// Create portals for every door
		public void createPortals( SceneManager scene, Entity room, SceneNode roomNode, PCZone zone, short doorFlags, bool isEnclosure )
		{
			string portalName;
			var corners = new Vector3[ 4 ];

			if ( isEnclosure )
			{
				if ( ( doorFlags & (short)RoomDoors.DOOR_FRONT ) != 0 )
				{
					// set the corners to the front door corners
					corners[ 0 ] = this.points[ 8 ];
					corners[ 1 ] = this.points[ 9 ];
					corners[ 2 ] = this.points[ 10 ];
					corners[ 3 ] = this.points[ 11 ];
					// create the portal
					portalName = room.Name + ( "_FrontDoorPortal" );
					Portal p = ( (PCZSceneManager)scene ).CreatePortal( portalName, PORTAL_TYPE.PORTAL_TYPE_QUAD );
					p.setCorners( corners );
					// associate the portal with the roomnode
					p.setNode( roomNode );
					// add the portal to the zone
					zone.AddPortal( p );
					// update derived values for the portal
					p.updateDerivedValues();
				}
				if ( ( doorFlags & (short)RoomDoors.DOOR_BACK ) != 0 )
				{
					// set the corners to the front door corners
					corners[ 0 ] = this.points[ 15 ];
					corners[ 1 ] = this.points[ 14 ];
					corners[ 2 ] = this.points[ 13 ];
					corners[ 3 ] = this.points[ 12 ];
					// create the portal
					portalName = room.Name + ( "_BackDoorPortal" );
					Portal p = ( (PCZSceneManager)scene ).CreatePortal( portalName, PORTAL_TYPE.PORTAL_TYPE_QUAD );
					p.setCorners( corners );
					// associate the portal with the roomnode
					p.setNode( roomNode );
					// add the portal to the zone
					zone.AddPortal( p );
					// update derived values for the portal
					p.updateDerivedValues();
				}
				if ( ( doorFlags & (short)RoomDoors.DOOR_TOP ) != 0 )
				{
					// set the corners to the front door corners
					corners[ 0 ] = this.points[ 16 ];
					corners[ 1 ] = this.points[ 17 ];
					corners[ 2 ] = this.points[ 18 ];
					corners[ 3 ] = this.points[ 19 ];
					// create the portal
					portalName = room.Name + ( "_TopDoorPortal" );
					Portal p = ( (PCZSceneManager)scene ).CreatePortal( portalName, PORTAL_TYPE.PORTAL_TYPE_QUAD );
					p.setCorners( corners );
					// associate the portal with the roomnode
					p.setNode( roomNode );
					// add the portal to the zone
					zone.AddPortal( p );
					// update derived values for the portal
					p.updateDerivedValues();
				}
				if ( ( doorFlags & (short)RoomDoors.DOOR_BOT ) != 0 )
				{
					// set the corners to the front door corners
					corners[ 0 ] = this.points[ 23 ];
					corners[ 1 ] = this.points[ 22 ];
					corners[ 2 ] = this.points[ 21 ];
					corners[ 3 ] = this.points[ 20 ];
					// create the portal
					portalName = room.Name + ( "_BottomDoorPortal" );
					Portal p = ( (PCZSceneManager)scene ).CreatePortal( portalName, PORTAL_TYPE.PORTAL_TYPE_QUAD );
					p.setCorners( corners );
					// associate the portal with the roomnode
					p.setNode( roomNode );
					// add the portal to the zone
					zone.AddPortal( p );
					// update derived values for the portal
					p.updateDerivedValues();
				}
				if ( ( doorFlags & (short)RoomDoors.DOOR_LEFT ) != 0 )
				{
					// set the corners to the front door corners
					corners[ 0 ] = this.points[ 27 ];
					corners[ 1 ] = this.points[ 26 ];
					corners[ 2 ] = this.points[ 25 ];
					corners[ 3 ] = this.points[ 24 ];
					// create the portal
					portalName = room.Name + ( "_LeftDoorPortal" );
					Portal p = ( (PCZSceneManager)scene ).CreatePortal( portalName, PORTAL_TYPE.PORTAL_TYPE_QUAD );
					p.setCorners( corners );
					// associate the portal with the roomnode
					p.setNode( roomNode );
					// add the portal to the zone
					zone.AddPortal( p );
					// update derived values for the portal
					p.updateDerivedValues();
				}
				if ( ( doorFlags & (short)RoomDoors.DOOR_RIGHT ) != 0 )
				{
					// set the corners to the front door corners
					corners[ 0 ] = this.points[ 28 ];
					corners[ 1 ] = this.points[ 29 ];
					corners[ 2 ] = this.points[ 30 ];
					corners[ 3 ] = this.points[ 31 ];
					// create the portal
					portalName = room.Name + ( "_RightDoorPortal" );
					Portal p = ( (PCZSceneManager)scene ).CreatePortal( portalName, PORTAL_TYPE.PORTAL_TYPE_QUAD );
					p.setCorners( corners );
					// associate the portal with the roomnode
					p.setNode( roomNode );
					// add the portal to the zone
					zone.AddPortal( p );
					// update derived values for the portal
					p.updateDerivedValues();
				}
			}
			else
			{
				if ( ( doorFlags & (short)RoomDoors.DOOR_FRONT ) != 0 )
				{
					// set the corners to the front door corners
					corners[ 0 ] = this.points[ 11 ];
					corners[ 1 ] = this.points[ 10 ];
					corners[ 2 ] = this.points[ 9 ];
					corners[ 3 ] = this.points[ 8 ];
					// create the portal
					portalName = room.Name + ( "_FrontDoorPortal" );
					Portal p = ( (PCZSceneManager)scene ).CreatePortal( portalName, PORTAL_TYPE.PORTAL_TYPE_QUAD );
					p.setCorners( corners );
					// associate the portal with the roomnode
					p.setNode( roomNode );
					// add the portal to the zone
					zone.AddPortal( p );
					// update derived values for the portal
					p.updateDerivedValues();
				}
				if ( ( doorFlags & (short)RoomDoors.DOOR_BACK ) != 0 )
				{
					// set the corners to the front door corners
					corners[ 0 ] = this.points[ 12 ];
					corners[ 1 ] = this.points[ 13 ];
					corners[ 2 ] = this.points[ 14 ];
					corners[ 3 ] = this.points[ 15 ];
					// create the portal
					portalName = room.Name + ( "_BackDoorPortal" );
					Portal p = ( (PCZSceneManager)scene ).CreatePortal( portalName, PORTAL_TYPE.PORTAL_TYPE_QUAD );
					p.setCorners( corners );
					// associate the portal with the roomnode
					p.setNode( roomNode );
					// add the portal to the zone
					zone.AddPortal( p );
					// update derived values for the portal
					p.updateDerivedValues();
				}
				if ( ( doorFlags & (short)RoomDoors.DOOR_TOP ) != 0 )
				{
					// set the corners to the front door corners
					corners[ 0 ] = this.points[ 19 ];
					corners[ 1 ] = this.points[ 18 ];
					corners[ 2 ] = this.points[ 17 ];
					corners[ 3 ] = this.points[ 16 ];
					// create the portal
					portalName = room.Name + ( "_TopDoorPortal" );
					Portal p = ( (PCZSceneManager)scene ).CreatePortal( portalName, PORTAL_TYPE.PORTAL_TYPE_QUAD );
					p.setCorners( corners );
					// associate the portal with the roomnode
					p.setNode( roomNode );
					// add the portal to the zone
					zone.AddPortal( p );
					// update derived values for the portal
					p.updateDerivedValues();
				}
				if ( ( doorFlags & (short)RoomDoors.DOOR_BOT ) != 0 )
				{
					// set the corners to the front door corners
					corners[ 0 ] = this.points[ 20 ];
					corners[ 1 ] = this.points[ 21 ];
					corners[ 2 ] = this.points[ 22 ];
					corners[ 3 ] = this.points[ 23 ];
					// create the portal
					portalName = room.Name + ( "_BottomDoorPortal" );
					Portal p = ( (PCZSceneManager)scene ).CreatePortal( portalName, PORTAL_TYPE.PORTAL_TYPE_QUAD );
					p.setCorners( corners );
					// associate the portal with the roomnode
					p.setNode( roomNode );
					// add the portal to the zone
					zone.AddPortal( p );
					// update derived values for the portal
					p.updateDerivedValues();
				}
				if ( ( doorFlags & (short)RoomDoors.DOOR_LEFT ) != 0 )
				{
					// set the corners to the front door corners
					corners[ 0 ] = this.points[ 24 ];
					corners[ 1 ] = this.points[ 25 ];
					corners[ 2 ] = this.points[ 26 ];
					corners[ 3 ] = this.points[ 27 ];
					// create the portal
					portalName = room.Name + ( "_LeftDoorPortal" );
					Portal p = ( (PCZSceneManager)scene ).CreatePortal( portalName, PORTAL_TYPE.PORTAL_TYPE_QUAD );
					p.setCorners( corners );
					// associate the portal with the roomnode
					p.setNode( roomNode );
					// add the portal to the zone
					zone.AddPortal( p );
					// update derived values for the portal
					p.updateDerivedValues();
				}
				if ( ( doorFlags & (short)RoomDoors.DOOR_RIGHT ) != 0 )
				{
					// set the corners to the front door corners
					corners[ 0 ] = this.points[ 31 ];
					corners[ 1 ] = this.points[ 30 ];
					corners[ 2 ] = this.points[ 29 ];
					corners[ 3 ] = this.points[ 28 ];
					// create the portal
					portalName = room.Name + ( "_RightDoorPortal" );
					Portal p = ( (PCZSceneManager)scene ).CreatePortal( portalName, PORTAL_TYPE.PORTAL_TYPE_QUAD );
					p.setCorners( corners );
					// associate the portal with the roomnode
					p.setNode( roomNode );
					// add the portal to the zone
					zone.AddPortal( p );
					// update derived values for the portal
					p.updateDerivedValues();
				}
			}
		}
	}
}
