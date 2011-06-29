#region MIT/X11 License
//Copyright (c) 2009 Axiom 3D Rendering Engine Project
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

#region SVN Version Information
// <file>
// <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
// <id value="$Id:$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations
using System;
using System.Collections.Generic;
using System.Text;
using Axiom.Graphics;
using Axiom.Math;
using Axiom.SceneManagers.PortalConnected;
using Axiom.Core;
#endregion

namespace PCZDemo
{
    /// <summary>
    /// Room Walls Enum
    /// </summary>
	public enum RoomWalls
	{
		TopWall,
		BotWall,
		FrontWall,
		BackWall,
		LeftWall,
		RightWall
	};

    /// <summary>
    /// Room doors flag
    /// </summary>
	[Flags]
	enum RoomDoors : short
	{
		None = 0x00,
		Top = 0x01,
		Bot = 0x02,
		Front = 0x04,
		Back = 0x08,
		Left = 0x10,
		Right = 0x20,
		All = 0xFF
	};


    /// <summary>
    /// Room Object
    /// </summary>
	public class RoomObject
	{
		private Vector3[] _points = new Vector3[ 32 ];
		private int _PortalCount;
		static bool init = false;

		static int count = 0;

        /// <summary>
        /// Create Test Building
        /// </summary>
        /// <param name="scene">SceneManager</param>
        /// <param name="name">string</param>
        /// <returns>PCZSceneNode</returns>
		public PCZSceneNode CreateTestBuilding( SceneManager scene, string name )
		{
			count++;
			_PortalCount = 0;
			PCZSceneManager pczSM = (PCZSceneManager)scene;

			// set points to building exterior size
			CreatePoints( new Vector3( 60.0f, 40.0f, 60.0f ), new Vector3( 4.0f, 10.0f, 4.0f ) );

			// create the building exterior
			Entity exterior = pczSM.CreateEntity( name + "_building_exterior", "building_exterior.mesh" );

			// make the enclosure a child node of the root scene node
			PCZSceneNode exteriorNode;
			exteriorNode = (PCZSceneNode)scene.RootSceneNode.CreateChildSceneNode( name + "_building_exterior_node", new Vector3( 0.0f, 0.0f, 0.0f ) );
			exteriorNode.AttachObject( exterior );
			pczSM.AddPCZSceneNode( exteriorNode, pczSM.DefaultZone );

			// create portals for the building exterior
			CreatePortals( scene,
						  exterior,
						  exteriorNode,
						  pczSM.DefaultZone,
						  (short)( RoomDoors.Front | RoomDoors.Back | RoomDoors.Left | RoomDoors.Right ),
						  true );

			// reset points to room size
			CreatePoints( new Vector3( 20.0f, 10.0f, 20.0f ), new Vector3( 4.0f, 10.0f, 4.0f ) );

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
			newZone.EnclosureNode = roomNode ;
			pczSM.AddPCZSceneNode( roomNode, newZone );

			// create portals for the room
			CreatePortals( scene,
						  room,
						  roomNode,
						  newZone,
						  (short)( RoomDoors.Front | RoomDoors.Back ),
						  false );

			// create another interior room
			room = pczSM.CreateEntity( name + "_room2", "room_nxpxnypynzpz.mesh" );

			// add the room as a child node to the enclosure node
			roomNode = (PCZSceneNode)exteriorNode.CreateChildSceneNode( name + "_room2_node", new Vector3( 0.0f, 0.0f, 0.0f ) );
			roomNode.AttachObject( room );

			// room needs it's own zone
			zoneName = name + "_room2_zone";
			newZone = pczSM.CreateZone( zoneType, zoneName );
			newZone.EnclosureNode = roomNode ;
			pczSM.AddPCZSceneNode( roomNode, newZone );

			// create portals for the room
			CreatePortals( scene,
						  room,
						  roomNode,
						  newZone,
						  (short)( RoomDoors.Front | RoomDoors.Back | RoomDoors.Left | RoomDoors.Right | RoomDoors.Top | RoomDoors.Bot ),
						  false );

			// create another interior room
			room = pczSM.CreateEntity( name + "_room3", "room_nzpz.mesh" );

			// add the room as a child node to the enclosure node
			roomNode = (PCZSceneNode)exteriorNode.CreateChildSceneNode( name + "_room3_node", new Vector3( 0.0f, 0.0f, -20.0f ) );
			roomNode.AttachObject( room );

			// room needs it's own zone
			zoneName = name + "_room3_zone";
			newZone = pczSM.CreateZone( zoneType, zoneName );
			newZone.EnclosureNode = roomNode ;
			pczSM.AddPCZSceneNode( roomNode, newZone );

			// create portals for the room
			CreatePortals( scene,
						  room,
						  roomNode,
						  newZone,
						  (short)( RoomDoors.Front | RoomDoors.Back ),
						  false );

			// create another interior room
			room = pczSM.CreateEntity( name + "_room4", "room_nxpx.mesh" );

			// add the room as a child node to the enclosure node
			roomNode = (PCZSceneNode)exteriorNode.CreateChildSceneNode( name + "_room4_node", new Vector3( -20.0f, 0.0f, 0.0f ) );
			roomNode.AttachObject( room );

			// room needs it's own zone
			zoneName = name + "_room4_zone";
			newZone = pczSM.CreateZone( zoneType, zoneName );
			newZone.EnclosureNode  = roomNode ;
			pczSM.AddPCZSceneNode( roomNode, newZone );

			// create portals for the room
			CreatePortals( scene,
						  room,
						  roomNode,
						  newZone,
						  (short)( RoomDoors.Left | RoomDoors.Right ),
						  false );

			// create another interior room
			room = pczSM.CreateEntity( name + "_room5", "room_nxpx.mesh" );

			// add the room as a child node to the enclosure node
			roomNode = (PCZSceneNode)exteriorNode.CreateChildSceneNode( name + "_room5_node", new Vector3( 20.0f, 0.0f, 0.0f ) );
			roomNode.AttachObject( room );

			// room needs it's own zone
			zoneName = name + "_room5_zone";
			newZone = pczSM.CreateZone( zoneType, zoneName );
			newZone.EnclosureNode = roomNode ;
			pczSM.AddPCZSceneNode( roomNode, newZone );

			// create portals for the room
			CreatePortals( scene,
						  room,
						  roomNode,
						  newZone,
						  (short)( RoomDoors.Left | RoomDoors.Right ),
						  false );

			// create another interior room
			room = pczSM.CreateEntity( name + "_room6", "room_ny.mesh" );

			// add the room as a child node to the enclosure node
			roomNode = (PCZSceneNode)exteriorNode.CreateChildSceneNode( name + "_room6_node", new Vector3( 0.0f, 10.0f, 0.0f ) );
			roomNode.AttachObject( room );

			// room needs it's own zone
			zoneName = name + "_room6_zone";
			newZone = pczSM.CreateZone( zoneType, zoneName );
			newZone.EnclosureNode = roomNode ;
			pczSM.AddPCZSceneNode( roomNode, newZone );

			// create portals for the room
			CreatePortals( scene,
						  room,
						  roomNode,
						  newZone,
						  (short)RoomDoors.Bot,
						  false );

			// create another interior room
			room = pczSM.CreateEntity( name + "_room7", "room_py.mesh" );

			// add the room as a child node to the enclosure node
			roomNode = (PCZSceneNode)exteriorNode.CreateChildSceneNode( name + "_room7_node", new Vector3( 0.0f, -50.0f, 0.0f ) );
			roomNode.AttachObject( room );

			// room needs it's own zone
			zoneName = name + "_room7_zone";
			newZone = pczSM.CreateZone( zoneType, zoneName );
			newZone.EnclosureNode = roomNode;
			pczSM.AddPCZSceneNode( roomNode, newZone );

			// create portals for the room
			CreatePortals( scene,
						  room,
						  roomNode,
						  newZone,
						  (short)RoomDoors.Top,
						  false );

			// reset points to tall room size
			CreatePoints( new Vector3( 20.0f, 40.0f, 20.0f ), new Vector3( 4.0f, 10.0f, 4.0f ) );

			// create another interior room
			room = pczSM.CreateEntity( name + "_room8", "room_nypy_4y.mesh" );

			// add the room as a child node to the enclosure node
			roomNode = (PCZSceneNode)exteriorNode.CreateChildSceneNode( name + "_room8_node", new Vector3( 0.0f, -25.0f, 0.0f ) );
			roomNode.AttachObject( room );

			// room needs it's own zone
			zoneName = name + "_room8_zone";
			newZone = pczSM.CreateZone( zoneType, zoneName );
			newZone.EnclosureNode = roomNode;
			pczSM.AddPCZSceneNode( roomNode, newZone );

			// create portals for the room
			CreatePortals( scene,
						  room,
						  roomNode,
						  newZone,
						  (short)( RoomDoors.Bot | RoomDoors.Top ),
						  false );


			// resolve portal zone pointers
			pczSM.ConnectPortalsToTargetZonesByLocation();

			return exteriorNode;
		}

        /// <summary>
        /// Create Room
        /// </summary>
        /// <param name="scene">SceneManager</param>
        /// <param name="name">string</param>
        /// <param name="doorFlags">short</param>
        /// <param name="isEnclosure">bool</param>
        /// <param name="dimensions">Vector3</param>
        /// <param name="doorDimensions">Vector3</param>
        /// <returns>ManualObject</returns>
		public ManualObject CreateRoom( SceneManager scene,
												   string name,
												   short doorFlags,
												   bool isEnclosure,
												   Vector3 dimensions,
												   Vector3 doorDimensions )
		{
			AddMaterial( name, new ColorEx( .75f, 1f, 1f, 1f ), SceneBlendType.TransparentAlpha );

			ManualObject room = scene.CreateManualObject( name );

			room.Begin( name, OperationType.TriangleList );

			// create points
			CreatePoints( dimensions, doorDimensions );

			float fade = .5f;
			float solid = .8f;
			ColorEx color = new ColorEx( solid, 0, solid, 0 );

			// copy to room
			for ( int i = 0; i < 32; i++ )
			{
				room.Position( _points[ i ] );
				room.Color( color );
			}

			CreateWalls( room, doorFlags, isEnclosure );

			room.End();

			return room;
		}

        /// <summary>
        /// Add Material
        /// </summary>
        /// <param name="mat">string</param>
        /// <param name="clr">ColorEx</param>
        /// <param name="sbt">SceneBlendType</param>
		public void AddMaterial( string mat,
									 ColorEx clr,
									 SceneBlendType sbt )
		{
			if ( init )
				return;
			else
				init = true;

			Material matptr = (Material)MaterialManager.Instance.Create( mat, "General" );
			matptr.ReceiveShadows = false;
			matptr.GetTechnique( 0 ).LightingEnabled = true;
			matptr.GetTechnique( 0 ).GetPass( 0 ).Diffuse = clr;
			matptr.GetTechnique( 0 ).GetPass( 0 ).Ambient = clr;
			matptr.GetTechnique( 0 ).GetPass( 0 ).SelfIllumination = clr;
			matptr.GetTechnique( 0 ).GetPass( 0 ).SetSceneBlending( sbt );
			matptr.GetTechnique( 0 ).GetPass( 0 ).LightingEnabled = false;
			matptr.GetTechnique( 0 ).GetPass( 0 ).VertexColorTracking = TrackVertexColor.Diffuse;
		}

        /// <summary>
        /// Create Points
        /// </summary>
        /// <param name="dimensions">Vector3</param>
        /// <param name="doorDimensions">Vector3</param>
		public void CreatePoints( Vector3 dimensions, Vector3 doorDimensions )
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

			_points[ 0 ] = new Vector3( -l, h, w );//0
			_points[ 1 ] = new Vector3( -l, -h, w );//1
			_points[ 2 ] = new Vector3( l, -h, w );//2
			_points[ 3 ] = new Vector3( l, h, w );//3

			_points[ 4 ] = new Vector3( -l, h, -w );//4
			_points[ 5 ] = new Vector3( -l, -h, -w );//5
			_points[ 6 ] = new Vector3( l, -h, -w );//6
			_points[ 7 ] = new Vector3( l, h, -w );//7

			// doors
			Real l2 = doorDimensions.x / 2;
			Real h2 = doorDimensions.y / 2;
			Real w2 = doorDimensions.z / 2;

			// front door
			_points[ 8 ] = new Vector3( -l2, h2, w );//8
			_points[ 9 ] = new Vector3( -l2, -h2, w );//9
			_points[ 10 ] = new Vector3( l2, -h2, w );//10
			_points[ 11 ] = new Vector3( l2, h2, w );//11

			// back door
			_points[ 12 ] = new Vector3( -l2, h2, -w );//12
			_points[ 13 ] = new Vector3( -l2, -h2, -w );//13
			_points[ 14 ] = new Vector3( l2, -h2, -w );//14
			_points[ 15 ] = new Vector3( l2, h2, -w );//15

			// top door
			_points[ 16 ] = new Vector3( -l2, h, -w2 );//16
			_points[ 17 ] = new Vector3( -l2, h, w2 );//17
			_points[ 18 ] = new Vector3( l2, h, w2 );//18
			_points[ 19 ] = new Vector3( l2, h, -w2 );//19

			// bottom door
			_points[ 20 ] = new Vector3( -l2, -h, -w2 );//20
			_points[ 21 ] = new Vector3( -l2, -h, w2 );//21
			_points[ 22 ] = new Vector3( l2, -h, w2 );//22
			_points[ 23 ] = new Vector3( l2, -h, -w2 );//23

			// left door
			_points[ 24 ] = new Vector3( -l, h2, w2 );//24
			_points[ 25 ] = new Vector3( -l, -h2, w2 );//25
			_points[ 26 ] = new Vector3( -l, -h2, -w2 );//26
			_points[ 27 ] = new Vector3( -l, h2, -w2 );//27

			// right door
			_points[ 28 ] = new Vector3( l, h2, w2 );//28
			_points[ 29 ] = new Vector3( l, -h2, w2 );//29
			_points[ 30 ] = new Vector3( l, -h2, -w2 );//30
			_points[ 31 ] = new Vector3( l, h2, -w2 );//31
		}

        /// <summary>
        /// Create Walls
        /// </summary>
        /// <param name="room">ManualObject</param>
        /// <param name="doorFlags">short</param>
        /// <param name="isEnclosure">bool</param>
		public void CreateWalls( ManualObject room,
									 short doorFlags,
									 bool isEnclosure )
		{

			if ( isEnclosure )
			{
				if ( ( doorFlags & (short)RoomDoors.Front ) != 0 )
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
				if ( ( doorFlags & (short)RoomDoors.Back ) != 0 )
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
				if ( ( doorFlags & (short)RoomDoors.Top ) != 0 )
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
				if ( ( doorFlags & (short)RoomDoors.Bot ) != 0 )
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
				if ( ( doorFlags & (short)RoomDoors.Left ) != 0 )
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
				if ( ( doorFlags & (short)RoomDoors.Right ) != 0 )
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
				if ( ( doorFlags & (short)RoomDoors.Front ) != 0 )
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
				if ( ( doorFlags & (short)RoomDoors.Back ) != 0 )
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
				if ( ( doorFlags & (short)RoomDoors.Top ) != 0 )
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
				if ( ( doorFlags & (short)RoomDoors.Bot ) != 0 )
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
				if ( ( doorFlags & (short)RoomDoors.Left ) != 0 )
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
				if ( ( doorFlags & (short)RoomDoors.Right ) != 0 )
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

		/// <summary>
        /// Create portals for every door
		/// </summary>
        /// <param name="scene">SceneManager</param>
        /// <param name="room">ManualObject</param>
        /// <param name="roomNode">SceneNode</param>
        /// <param name="zone">PCZone</param>
        /// <param name="doorFlags">short</param>
        /// <param name="isEnclosure">bool</param>
		public void CreatePortals( SceneManager scene,
									   ManualObject room,
									   SceneNode roomNode,
									   PCZone zone,
									   short doorFlags,
									   bool isEnclosure )
		{
			string portalName;
            List<Vector3> corners = new List<Vector3>(4);
			if ( isEnclosure )
			{
				if ( ( doorFlags & (short)RoomDoors.Front ) != 0 )
				{
					// set the corners to the front door corners
					corners[ 0 ] = _points[ 8 ];
					corners[ 1 ] = _points[ 9 ];
					corners[ 2 ] = _points[ 10 ];
					corners[ 3 ] = _points[ 11 ];
					// create the portal
					portalName = room.Name + "_FrontDoorPortal";
					Portal p = ( (PCZSceneManager)scene ).CreatePortal( portalName, PortalType.Quad );
					p.Corners = corners  ;
					// associate the portal with the roomnode
					p.setNode( roomNode );
					// add the portal to the zone
					zone.AddPortal( p );
					// update derived values for the portal
					p.updateDerivedValues();
				}
				if ( ( doorFlags & (short)RoomDoors.Back ) != 0 )
				{
					// set the corners to the front door corners
					corners[ 0 ] = _points[ 15 ];
					corners[ 1 ] = _points[ 14 ];
					corners[ 2 ] = _points[ 13 ];
					corners[ 3 ] = _points[ 12 ];
					// create the portal
					portalName = room.Name + ( "_BackDoorPortal" );
					Portal p = ( (PCZSceneManager)scene ).CreatePortal( portalName, PortalType.Quad );
					p.Corners = corners  ;
					// associate the portal with the roomnode
					p.setNode( roomNode );
					// add the portal to the zone
					zone.AddPortal( p );
					// update derived values for the portal
					p.updateDerivedValues();
				}
				if ( ( doorFlags & (short)RoomDoors.Top ) != 0 )
				{
					// set the corners to the front door corners
					corners[ 0 ] = _points[ 16 ];
					corners[ 1 ] = _points[ 17 ];
					corners[ 2 ] = _points[ 18 ];
					corners[ 3 ] = _points[ 19 ];
					// create the portal
					portalName = room.Name + ( "_TopDoorPortal" );
					Portal p = ( (PCZSceneManager)scene ).CreatePortal( portalName, PortalType.Quad );
					p.Corners = corners  ;
					// associate the portal with the roomnode
					p.setNode( roomNode );
					// add the portal to the zone
					zone.AddPortal( p );
					// update derived values for the portal
					p.updateDerivedValues();
				}
				if ( ( doorFlags & (short)RoomDoors.Bot ) != 0 )
				{
					// set the corners to the front door corners
					corners[ 0 ] = _points[ 23 ];
					corners[ 1 ] = _points[ 22 ];
					corners[ 2 ] = _points[ 21 ];
					corners[ 3 ] = _points[ 20 ];
					// create the portal
					portalName = room.Name + ( "_BottomDoorPortal" );
					Portal p = ( (PCZSceneManager)scene ).CreatePortal( portalName, PortalType.Quad );
					p.Corners = corners  ;
					// associate the portal with the roomnode
					p.setNode( roomNode );
					// add the portal to the zone
					zone.AddPortal( p );
					// update derived values for the portal
					p.updateDerivedValues();
				}
				if ( ( doorFlags & (short)RoomDoors.Left ) != 0 )
				{
					// set the corners to the front door corners
					corners[ 0 ] = _points[ 27 ];
					corners[ 1 ] = _points[ 26 ];
					corners[ 2 ] = _points[ 25 ];
					corners[ 3 ] = _points[ 24 ];
					// create the portal
					portalName = room.Name + ( "_LeftDoorPortal" );
					Portal p = ( (PCZSceneManager)scene ).CreatePortal( portalName, PortalType.Quad );
					p.Corners = corners  ;
					// associate the portal with the roomnode
					p.setNode( roomNode );
					// add the portal to the zone
					zone.AddPortal( p );
					// update derived values for the portal
					p.updateDerivedValues();
				}
				if ( ( doorFlags & (short)RoomDoors.Right ) != 0 )
				{
					// set the corners to the front door corners
					corners[ 0 ] = _points[ 28 ];
					corners[ 1 ] = _points[ 29 ];
					corners[ 2 ] = _points[ 30 ];
					corners[ 3 ] = _points[ 31 ];
					// create the portal
					portalName = room.Name + ( "_RightDoorPortal" );
					Portal p = ( (PCZSceneManager)scene ).CreatePortal( portalName, PortalType.Quad );
					p.Corners = corners  ;
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
				if ( ( doorFlags & (short)RoomDoors.Front ) != 0 )
				{
					// set the corners to the front door corners
					corners[ 0 ] = _points[ 11 ];
					corners[ 1 ] = _points[ 10 ];
					corners[ 2 ] = _points[ 9 ];
					corners[ 3 ] = _points[ 8 ];
					// create the portal
					portalName = room.Name + ( "_FrontDoorPortal" );
					Portal p = ( (PCZSceneManager)scene ).CreatePortal( portalName, PortalType.Quad );
					p.Corners = corners  ;
					// associate the portal with the roomnode
					p.setNode( roomNode );
					// add the portal to the zone
					zone.AddPortal( p );
					// update derived values for the portal
					p.updateDerivedValues();
				}
				if ( ( doorFlags & (short)RoomDoors.Back ) != 0 )
				{
					// set the corners to the front door corners
					corners[ 0 ] = _points[ 12 ];
					corners[ 1 ] = _points[ 13 ];
					corners[ 2 ] = _points[ 14 ];
					corners[ 3 ] = _points[ 15 ];
					// create the portal
					portalName = room.Name + ( "_BackDoorPortal" );
					Portal p = ( (PCZSceneManager)scene ).CreatePortal( portalName, PortalType.Quad );
					p.Corners = corners  ;
					// associate the portal with the roomnode
					p.setNode( roomNode );
					// add the portal to the zone
					zone.AddPortal( p );
					// update derived values for the portal
					p.updateDerivedValues();
				}
				if ( ( doorFlags & (short)RoomDoors.Top ) != 0 )
				{
					// set the corners to the front door corners
					corners[ 0 ] = _points[ 19 ];
					corners[ 1 ] = _points[ 18 ];
					corners[ 2 ] = _points[ 17 ];
					corners[ 3 ] = _points[ 16 ];
					// create the portal
					portalName = room.Name + ( "_TopDoorPortal" );
					Portal p = ( (PCZSceneManager)scene ).CreatePortal( portalName, PortalType.Quad );
					p.Corners = corners  ;
					// associate the portal with the roomnode
					p.setNode( roomNode );
					// add the portal to the zone
					zone.AddPortal( p );
					// update derived values for the portal
					p.updateDerivedValues();
				}
				if ( ( doorFlags & (short)RoomDoors.Bot ) != 0 )
				{
					// set the corners to the front door corners
					corners[ 0 ] = _points[ 20 ];
					corners[ 1 ] = _points[ 21 ];
					corners[ 2 ] = _points[ 22 ];
					corners[ 3 ] = _points[ 23 ];
					// create the portal
					portalName = room.Name + ( "_BottomDoorPortal" );
					Portal p = ( (PCZSceneManager)scene ).CreatePortal( portalName, PortalType.Quad );
					p.Corners = corners  ;
					// associate the portal with the roomnode
					p.setNode( roomNode );
					// add the portal to the zone
					zone.AddPortal( p );
					// update derived values for the portal
					p.updateDerivedValues();
				}
				if ( ( doorFlags & (short)RoomDoors.Left ) != 0 )
				{
					// set the corners to the front door corners
					corners[ 0 ] = _points[ 24 ];
					corners[ 1 ] = _points[ 25 ];
					corners[ 2 ] = _points[ 26 ];
					corners[ 3 ] = _points[ 27 ];
					// create the portal
					portalName = room.Name + ( "_LeftDoorPortal" );
					Portal p = ( (PCZSceneManager)scene ).CreatePortal( portalName, PortalType.Quad );
					p.Corners = corners  ;
					// associate the portal with the roomnode
					p.setNode( roomNode );
					// add the portal to the zone
					zone.AddPortal( p );
					// update derived values for the portal
					p.updateDerivedValues();
				}
				if ( ( doorFlags & (short)RoomDoors.Right ) != 0 )
				{
					// set the corners to the front door corners
					corners[ 0 ] = _points[ 31 ];
					corners[ 1 ] = _points[ 30 ];
					corners[ 2 ] = _points[ 29 ];
					corners[ 3 ] = _points[ 28 ];
					// create the portal
					portalName = room.Name + ( "_RightDoorPortal" );
					Portal p = ( (PCZSceneManager)scene ).CreatePortal( portalName, PortalType.Quad );
					p.Corners = corners  ;
					// associate the portal with the roomnode
					p.setNode( roomNode );
					// add the portal to the zone
					zone.AddPortal( p );
					// update derived values for the portal
					p.updateDerivedValues();
				}
			}
		}

		/// <summary>
        /// Create portals for every door
		/// </summary>
        /// <param name="scene">SceneManager</param>
        /// <param name="room">Entity</param>
        /// <param name="roomNode">SceneNode</param>
        /// <param name="zone">PCZone</param>
        /// <param name="doorFlags">short</param>
        /// <param name="isEnclosure">bool</param>
		public void CreatePortals( SceneManager scene,
									   Entity room,
									   SceneNode roomNode,
									   PCZone zone,
									   short doorFlags,
									   bool isEnclosure )
		{
			string portalName;
			List<Vector3> corners = new List<Vector3>(4);
            // TODO Better way to do this??
            corners.Add(Vector3.Zero);
            corners.Add(Vector3.Zero);
            corners.Add(Vector3.Zero);
            corners.Add(Vector3.Zero);
 
			if ( isEnclosure )
			{
				if ( ( doorFlags & (short)RoomDoors.Front ) != 0 )
				{
					// set the corners to the front door corners
					corners[ 0 ] = _points[ 8 ];
					corners[ 1 ] = _points[ 9 ];
					corners[ 2 ] = _points[ 10 ];
					corners[ 3 ] = _points[ 11 ];
					// create the portal
					portalName = room.Name + ( "_FrontDoorPortal" );
					Portal p = ( (PCZSceneManager)scene ).CreatePortal( portalName, PortalType.Quad );
					p.Corners = corners  ;
					// associate the portal with the roomnode
					p.setNode( roomNode );
					// add the portal to the zone
					zone.AddPortal( p );
					// update derived values for the portal
					p.updateDerivedValues();
				}
				if ( ( doorFlags & (short)RoomDoors.Back ) != 0 )
				{
					// set the corners to the front door corners
					corners[ 0 ] = _points[ 15 ];
					corners[ 1 ] = _points[ 14 ];
					corners[ 2 ] = _points[ 13 ];
					corners[ 3 ] = _points[ 12 ];
					// create the portal
					portalName = room.Name + ( "_BackDoorPortal" );
					Portal p = ( (PCZSceneManager)scene ).CreatePortal( portalName, PortalType.Quad );
					p.Corners = corners  ;
					// associate the portal with the roomnode
					p.setNode( roomNode );
					// add the portal to the zone
					zone.AddPortal( p );
					// update derived values for the portal
					p.updateDerivedValues();
				}
				if ( ( doorFlags & (short)RoomDoors.Top ) != 0 )
				{
					// set the corners to the front door corners
					corners[ 0 ] = _points[ 16 ];
					corners[ 1 ] = _points[ 17 ];
					corners[ 2 ] = _points[ 18 ];
					corners[ 3 ] = _points[ 19 ];
					// create the portal
					portalName = room.Name + ( "_TopDoorPortal" );
					Portal p = ( (PCZSceneManager)scene ).CreatePortal( portalName, PortalType.Quad );
					p.Corners = corners  ;
					// associate the portal with the roomnode
					p.setNode( roomNode );
					// add the portal to the zone
					zone.AddPortal( p );
					// update derived values for the portal
					p.updateDerivedValues();
				}
				if ( ( doorFlags & (short)RoomDoors.Bot ) != 0 )
				{
					// set the corners to the front door corners
					corners[ 0 ] = _points[ 23 ];
					corners[ 1 ] = _points[ 22 ];
					corners[ 2 ] = _points[ 21 ];
					corners[ 3 ] = _points[ 20 ];
					// create the portal
					portalName = room.Name + ( "_BottomDoorPortal" );
					Portal p = ( (PCZSceneManager)scene ).CreatePortal( portalName, PortalType.Quad );
					p.Corners = corners  ;
					// associate the portal with the roomnode
					p.setNode( roomNode );
					// add the portal to the zone
					zone.AddPortal( p );
					// update derived values for the portal
					p.updateDerivedValues();
				}
				if ( ( doorFlags & (short)RoomDoors.Left ) != 0 )
				{
					// set the corners to the front door corners
					corners[ 0 ] = _points[ 27 ];
					corners[ 1 ] = _points[ 26 ];
					corners[ 2 ] = _points[ 25 ];
					corners[ 3 ] = _points[ 24 ];
					// create the portal
					portalName = room.Name + ( "_LeftDoorPortal" );
					Portal p = ( (PCZSceneManager)scene ).CreatePortal( portalName, PortalType.Quad );
					p.Corners = corners  ;
					// associate the portal with the roomnode
					p.setNode( roomNode );
					// add the portal to the zone
					zone.AddPortal( p );
					// update derived values for the portal
					p.updateDerivedValues();
				}
				if ( ( doorFlags & (short)RoomDoors.Right ) != 0 )
				{
					// set the corners to the front door corners
					corners[ 0 ] = _points[ 28 ];
					corners[ 1 ] = _points[ 29 ];
					corners[ 2 ] = _points[ 30 ];
					corners[ 3 ] = _points[ 31 ];
					// create the portal
					portalName = room.Name + ( "_RightDoorPortal" );
					Portal p = ( (PCZSceneManager)scene ).CreatePortal( portalName, PortalType.Quad );
					p.Corners = corners  ;
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
				if ( ( doorFlags & (short)RoomDoors.Front ) != 0 )
				{
					// set the corners to the front door corners
					corners[ 0 ] = _points[ 11 ];
					corners[ 1 ] = _points[ 10 ];
					corners[ 2 ] = _points[ 9 ];
					corners[ 3 ] = _points[ 8 ];
					// create the portal
					portalName = room.Name + ( "_FrontDoorPortal" );
					Portal p = ( (PCZSceneManager)scene ).CreatePortal( portalName, PortalType.Quad );
					p.Corners = corners  ;
					// associate the portal with the roomnode
					p.setNode( roomNode );
					// add the portal to the zone
					zone.AddPortal( p );
					// update derived values for the portal
					p.updateDerivedValues();
				}
				if ( ( doorFlags & (short)RoomDoors.Back ) != 0 )
				{
					// set the corners to the front door corners
					corners[ 0 ] = _points[ 12 ];
					corners[ 1 ] = _points[ 13 ];
					corners[ 2 ] = _points[ 14 ];
					corners[ 3 ] = _points[ 15 ];
					// create the portal
					portalName = room.Name + ( "_BackDoorPortal" );
					Portal p = ( (PCZSceneManager)scene ).CreatePortal( portalName, PortalType.Quad );
					p.Corners = corners  ;
					// associate the portal with the roomnode
					p.setNode( roomNode );
					// add the portal to the zone
					zone.AddPortal( p );
					// update derived values for the portal
					p.updateDerivedValues();
				}
				if ( ( doorFlags & (short)RoomDoors.Top ) != 0 )
				{
					// set the corners to the front door corners
					corners[ 0 ] = _points[ 19 ];
					corners[ 1 ] = _points[ 18 ];
					corners[ 2 ] = _points[ 17 ];
					corners[ 3 ] = _points[ 16 ];
					// create the portal
					portalName = room.Name + ( "_TopDoorPortal" );
					Portal p = ( (PCZSceneManager)scene ).CreatePortal( portalName, PortalType.Quad );
					p.Corners = corners  ;
					// associate the portal with the roomnode
					p.setNode( roomNode );
					// add the portal to the zone
					zone.AddPortal( p );
					// update derived values for the portal
					p.updateDerivedValues();
				}
				if ( ( doorFlags & (short)RoomDoors.Bot ) != 0 )
				{
					// set the corners to the front door corners
					corners[ 0 ] = _points[ 20 ];
					corners[ 1 ] = _points[ 21 ];
					corners[ 2 ] = _points[ 22 ];
					corners[ 3 ] = _points[ 23 ];
					// create the portal
					portalName = room.Name + ( "_BottomDoorPortal" );
					Portal p = ( (PCZSceneManager)scene ).CreatePortal( portalName, PortalType.Quad );
					p.Corners = corners  ;
					// associate the portal with the roomnode
					p.setNode( roomNode );
					// add the portal to the zone
					zone.AddPortal( p );
					// update derived values for the portal
					p.updateDerivedValues();
				}
				if ( ( doorFlags & (short)RoomDoors.Left ) != 0 )
				{
					// set the corners to the front door corners
					corners[ 0 ] = _points[ 24 ];
					corners[ 1 ] = _points[ 25 ];
					corners[ 2 ] = _points[ 26 ];
					corners[ 3 ] = _points[ 27 ];
					// create the portal
					portalName = room.Name + ( "_LeftDoorPortal" );
					Portal p = ( (PCZSceneManager)scene ).CreatePortal( portalName, PortalType.Quad );
					p.Corners = corners  ;
					// associate the portal with the roomnode
					p.setNode( roomNode );
					// add the portal to the zone
					zone.AddPortal( p );
					// update derived values for the portal
					p.updateDerivedValues();
				}
				if ( ( doorFlags & (short)RoomDoors.Right ) != 0 )
				{
					// set the corners to the front door corners
					corners[ 0 ] = _points[ 31 ];
					corners[ 1 ] = _points[ 30 ];
					corners[ 2 ] = _points[ 29 ];
					corners[ 3 ] = _points[ 28 ];
					// create the portal
					portalName = room.Name + ( "_RightDoorPortal" );
					Portal p = ( (PCZSceneManager)scene ).CreatePortal( portalName, PortalType.Quad );
					p.Corners = corners  ;
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