using System;

using Axiom.Core;
using Axiom.Demos;
using Axiom.Graphics;
using Axiom.Input;
using Axiom.Math;
using Axiom.SceneManagers.PortalConnected;

namespace PCZDemo
{
	public class PCZTestApp : TechDemo
	{
		private SceneNode CameraNode;
		private PCZSceneNode BuildingNode;
		private Vector3 BuildingTranslate;
		private RaySceneQuery RaySceneQuery = null;
		private float _MoveSpeed;
		private Listener _l = new Listener();

		public override void CreateScene()
		{
			// Set ambient light
			scene.AmbientLight = new ColorEx( 0.25f, 0.25f, 0.25f );

			// Create a skybox
			scene.SetSkyBox( true, "Examples/CloudyNoonSkyBox", 500 );
			// put the skybox node in the default zone
			( (PCZSceneManager)scene ).SetSkyZone( null );

			// Create a light
			Light l = scene.CreateLight( "MainLight" );
			l.Position = new Vector3( 0, 0, 0 );
			l.SetAttenuation( 500, 0.5f, 1.0f, 0.0f );
			// Accept default settings: point light, white diffuse, just set position
			// attach light to a scene node so the PCZSM can handle it properly (zone-wise)
			// IMPORTANT: Lights (just like cameras) MUST be connected to a scene node!
			SceneNode lightNode = CameraNode.CreateChildSceneNode( "light_Node" );
			lightNode.AttachObject( l );

			// Fog
			// NB it's VERY important to set this before calling setWorldGeometry
			// because the vertex program picked will be different
			ColorEx fadeColour = new ColorEx( 0.101f, 0.125f, 0.1836f );
			scene.SetFog( FogMode.Linear, fadeColour, .001f, 500, 1000 );
			window.GetViewport( 0 ).BackgroundColor = fadeColour;

			// create a terrain zone
			string terrain_cfg = "Terrain.xml";
			string zoneName = "Terrain1_Zone";
			PCZone terrainZone = createTerrainZone( zoneName, terrain_cfg );

			/*		// Create another terrain zone
                    terrain_cfg = "terrain.cfg";
                    zoneName = "Terrain2_Zone";
                    terrainZone = createTerrainZone(zoneName, terrain_cfg);
                    // move second terrain next to first terrain
                    terrainZone->getEnclosureNode()->setPosition(1500, 0, 0);

                    // Create another terrain zone
                    terrain_cfg = "terrain.cfg";
                    zoneName = "Terrain3_Zone";
                    terrainZone = createTerrainZone(zoneName, terrain_cfg);
                    // move terrain next to first terrain
                    terrainZone->getEnclosureNode()->setPosition(0, 0, 1500);

                    // Create another terrain zone
                    terrain_cfg = "terrain.cfg";
                    zoneName = "Terrain4_Zone";
                    terrainZone = createTerrainZone(zoneName, terrain_cfg);
                    // move terrain next to first terrain
                    terrainZone->getEnclosureNode()->setPosition(-1500, 0, 0);

                    // Create another terrain zone
                    terrain_cfg = "terrain.cfg";
                    zoneName = "Terrain5_Zone";
                    terrainZone = createTerrainZone(zoneName, terrain_cfg);
                    // move terrain next to first terrain
                    terrainZone->getEnclosureNode()->setPosition(0, 0, -1500);

                    // Create another terrain zone
                    terrain_cfg = "terrain.cfg";
                    zoneName = "Terrain6_Zone";
                    terrainZone = createTerrainZone(zoneName, terrain_cfg);
                    // move terrain next to first terrain
                    terrainZone->getEnclosureNode()->setPosition(1500, 0, 1500);

                    // Create another terrain zone
                    terrain_cfg = "terrain.cfg";
                    zoneName = "Terrain7_Zone";
                    terrainZone = createTerrainZone(zoneName, terrain_cfg);
                    // move terrain next to first terrain
                    terrainZone->getEnclosureNode()->setPosition(-1500, 0, -1500);

                    // Create another terrain zone
                    terrain_cfg = "terrain.cfg";
                    zoneName = "Terrain8_Zone";
                    terrainZone = createTerrainZone(zoneName, terrain_cfg);
                    // move terrain next to first terrain
                    terrainZone->getEnclosureNode()->setPosition(-1500, 0, 1500);

                    // Create another terrain zone
                    terrain_cfg = "terrain.cfg";
                    zoneName = "Terrain9_Zone";
                    terrainZone = createTerrainZone(zoneName, terrain_cfg);
                    // move terrain next to first terrain
                    terrainZone->getEnclosureNode()->setPosition(1500, 0, -1500);
            */
			// set far clip plane to one terrain zone width (we have a LOT of terrain here, so we need to do far clipping!)
			camera.Far = 1500;

			// create test buildinig
			RoomObject roomObj = new RoomObject();
			BuildingNode = roomObj.CreateTestBuilding( scene, "1" );
			BuildingNode.Position = new Vector3( 500, 165, 570 );
			//Ogre::Radian r = Radian(3.1416/7.0);
			//buildingNode->rotate(Vector3::UNIT_Y, r);

			// create another test buildinig
			RoomObject roomObj2 = new RoomObject();
			BuildingNode = roomObj2.CreateTestBuilding( scene, "2" );
			BuildingNode.Position = new Vector3( 400, 165, 570 );
			//Ogre::Radian r = Radian(3.1416/7.0);
			//buildingNode->rotate(Vector3::UNIT_Y, r);

			// Position camera in the center of the building
			CameraNode.Position = BuildingNode.Position;
			// Look back along -Z
			camera.LookAt( CameraNode.DerivedPosition + new Vector3( 0, 0, -300 ) );
			// Update bounds for camera
			//mCameraNode.->_updateBounds();

			// create the ray scene query
			RaySceneQuery = scene.CreateRayQuery(
			                                     new Ray( camera.ParentNode.Position, Vector3.NegativeUnitZ ) );
			RaySceneQuery.SortByDistance = true;
		}

		protected override void OnFrameStarted( object source, FrameEventArgs evt )
		{
			BuildingTranslate = new Vector3( 0, 0, 0 );
			if( input.IsKeyPressed( KeyCodes.U ) )
			{
				BuildingTranslate = new Vector3( 0, -10, 0 );
			}
			if( input.IsKeyPressed( KeyCodes.I ) )
			{
				BuildingTranslate = new Vector3( 0, 10, 0 );
			}

			if( input.IsKeyPressed( KeyCodes.LeftShift ) ||
			    input.IsKeyPressed( KeyCodes.RightShift ) )
			{
				_MoveSpeed = 150;
			}
			else
			{
				_MoveSpeed = 15;
			}

			// test the ray scene query by showing bounding box of whatever the camera is pointing directly at
			// (takes furthest hit)
			Ray updateRay = new Ray();
			updateRay.Origin = camera.ParentSceneNode.Position;
			updateRay.Direction = camera.ParentSceneNode.Orientation * Vector3.NegativeUnitZ;
			RaySceneQuery.Ray = updateRay;
			PCZone zone = ( (PCZSceneNode)( camera.ParentSceneNode ) ).HomeZone;
			( (PCZRaySceneQuery)RaySceneQuery ).StartZone = zone;
			( (PCZRaySceneQuery)RaySceneQuery ).ExcludeNode = camera.ParentSceneNode;
			RaySceneQuery.Execute( _l );

			base.OnFrameStarted( source, evt );
		}

		public override void ChooseSceneManager()
		{
			// Create the SceneManager, in this case a generic one
			scene = Root.Instance.CreateSceneManager( "PCZSceneManager", "PCZSceneManager" );
			// initialize the scene manager using terrain as default zone
			string zoneTypeName = "ZoneType_Default";
			string zoneFilename = "none";
			( (PCZSceneManager)scene ).Init( zoneTypeName, zoneFilename );
			//scene.showBoundingBoxes(true);
		}

		public override void CreateCamera()
		{
			// Create the camera
			camera = scene.CreateCamera( "PlayerCam" );

			// NEW: create a node for the camera and control that instead of camera directly.
			// We do this because PCZSceneManager requires camera to have a node
			CameraNode = scene.RootSceneNode.CreateChildSceneNode( "PlayerCamNode" );
			// attach the camera to the node
			CameraNode.AttachObject( camera );
			// fix the yaw axis of the camera
			CameraNode.SetFixedYawAxis( true );

			camera.Near = 2;
			camera.Far = 1000;
		}

		// utility function to create terrain zones easily
		private PCZone createTerrainZone( string zoneName, string terrain_cfg )
		{
			// load terrain into the terrain zone
			PCZone terrainZone = ( (PCZSceneManager)scene ).CreateZone( "ZoneType_Terrain", zoneName );
			terrainZone.NotifyCameraCreated( camera );
			( (PCZSceneManager)scene ).SetZoneGeometry( zoneName, (PCZSceneNode)scene.RootSceneNode, terrain_cfg );

			// create aab portal(s) around the terrain
			String portalName;
			Vector3[] corners = new Vector3[2];
			AxisAlignedBox aabb = AxisAlignedBox.Null;

			// make portal from terrain to default
			Portal p;
			terrainZone.GetAABB( ref aabb );
			portalName = "PortalFrom" + zoneName + "ToDefault_Zone";
			p = ( (PCZSceneManager)scene ).CreatePortal( portalName, PortalType.AABB );
			corners[ 0 ] = aabb.Minimum;
			corners[ 1 ] = aabb.Maximum;
			p.Corners[ 0 ] = corners[ 0 ];
			p.Corners[ 1 ] = corners[ 1 ];
			p.Direction = Vector3.NegativeUnitZ; // this indicates an "inward" pointing normal
			// associate the portal with the terrain's main node
			p.setNode( terrainZone.EnclosureNode );
			// IMPORTANT: Update the derived values of the portal
			p.updateDerivedValues();
			// add the portal to the zone
			terrainZone.AddPortal( p );

			// make portal from default to terrain
			portalName = "PortalFromDefault_ZoneTo" + zoneName;
			Portal p2;
			p2 = ( (PCZSceneManager)scene ).CreatePortal( portalName, PortalType.AABB );
			corners[ 0 ] = aabb.Minimum;
			corners[ 1 ] = aabb.Maximum;
			p2.Corners[ 0 ] = corners[ 0 ];
			p2.Corners[ 1 ] = corners[ 1 ];
			p2.Direction = Vector3.UnitZ; // this indicates an "outward" pointing normal
			// associate the portal with the terrain's main node
			p2.setNode( terrainZone.EnclosureNode );
			// IMPORTANT: Update the derived values of the portal
			p2.updateDerivedValues();
			// add the portal to the zone
			( (PCZSceneManager)scene ).DefaultZone.AddPortal( p2 );

			// connect the portals manually
			p.TargetPortal = p2;
			p2.TargetPortal = p;
			p.TargetZone = ( (PCZSceneManager)scene ).DefaultZone;
			p2.TargetZone = terrainZone;

			return terrainZone;
		}
	}

	public class Listener : IRaySceneQueryListener
	{
		private MovableObject targetMO = null;

		public bool OnQueryResult( MovableObject sceneObject, float distance )
		{
			if( sceneObject != null )
			{
				if( sceneObject != targetMO )
				{
					sceneObject.ParentSceneNode.ShowBoundingBox = true;
				}

				targetMO = sceneObject;
			}

			return false;
		}

		public bool OnQueryResult( SceneQuery.WorldFragment fragment, float distance )
		{
			throw new NotImplementedException();
		}
	}
}
