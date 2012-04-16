using System;
using System.Collections.Generic;
using System.Text;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;

namespace Axiom.Demos
{
    class DeferredShading : TechDemo
    {
        private MovablePlane _plane;
        private Entity _planeEnt;
        private SceneNode _planeNode;
        private DeferredShadingSystem.DeferredShadingSystem _system;

        public override void CreateScene()
        {
            RenderSystem rs = Root.Instance.RenderSystem;
            RenderSystemCapabilities caps = rs.HardwareCapabilities;
            if ( !caps.HasCapability( Capabilities.VertexPrograms ) || !( caps.HasCapability( Capabilities.FragmentPrograms ) ) )
            {
                throw new AxiomException( "Your card does not support vertex and fragment programs, so cannot run this demo. Sorry!" );
            }
            if ( caps.MultiRenderTargetCount < 2 )
            {
                throw new AxiomException( "Your card does not support at least two simulataneous render targets, so cannot run this demo. Sorry!" );
            }

            MovableObject.DefaultVisibilityFlags = 0x00000001;
            this.scene.VisibilityMask = 0x00000001;

            short srcIdx, destIdx;
            Mesh mesh = MeshManager.Instance.Load( "athene.mesh", ResourceGroupManager.DefaultResourceGroupName );
            // the athene mesh requires tangent vectors
            if ( !mesh.SuggestTangentVectorBuildParams( out srcIdx, out destIdx ) )
            {
                mesh.BuildTangentVectors( srcIdx, destIdx );
            }
            mesh = MeshManager.Instance.Load( "knot.mesh", ResourceGroupManager.DefaultResourceGroupName );
            // the athene mesh requires tangent vectors
            if ( !mesh.SuggestTangentVectorBuildParams( out srcIdx, out destIdx ) )
            {
                mesh.BuildTangentVectors( srcIdx, destIdx );
            }

            scene.AmbientLight = new ColorEx( 0.2f, 0.2f, 0.15f );
            scene.SetSkyBox( true, "Skybox/Morning", 5000 );

		    // Create "root" node
		    SceneNode rootNode = scene.RootSceneNode.CreateChildSceneNode();

		    Entity athena = scene.CreateEntity("Athena", "athene.mesh");
		    athena.MaterialName = "DeferredDemo/DeferredAthena";
		    SceneNode aNode = rootNode.CreateChildSceneNode();
		    aNode.AttachObject( athena );
		    aNode.Position = new Vector3(-100, 40, 100);

		    // Create a prefab plane
            Plane tmpPlane = new Plane();
            tmpPlane.D = 0;
            tmpPlane.Normal = Vector3.UnitY;

		    MeshManager.Instance.CreateCurvedPlane("ReflectionPlane", 
			    ResourceGroupManager.DefaultResourceGroupName, 
			    tmpPlane,
			    2000, 2000, -1000,
			    20, 20, 
			    true, 1, 10, 10, Vector3.UnitZ);

		    _planeEnt = scene.CreateEntity( "Plane", "ReflectionPlane" );
		    _planeNode = rootNode.CreateChildSceneNode();
            _planeNode.AttachObject( _planeEnt );
            _planeNode.Translate( new Vector3(-5, -30, 0 ) );
		    //mPlaneNode->roll(Degree(5));
            _planeEnt.MaterialName = "DeferredDemo/Ground";

		    // Create an entity from a model (will be loaded automatically)
            Entity knotEnt = scene.CreateEntity( "Knot", "knot.mesh" );
		    knotEnt.MaterialName = "DeferredDemo/RockWall";
		    knotEnt.SetMeshLodBias(0.25f,0,99);

            // Create an entity from a model (will be loaded automatically)
            Entity ogreHead = scene.CreateEntity( "Head", "ogrehead.mesh" );
            ogreHead.GetSubEntity( 0 ).MaterialName= "DeferredDemo/Ogre/Eyes" ;// eyes
            ogreHead.GetSubEntity( 1 ).MaterialName= "DeferredDemo/Ogre/Skin" ;
            ogreHead.GetSubEntity( 2 ).MaterialName= "DeferredDemo/Ogre/EarRing" ; // earrings
            ogreHead.GetSubEntity( 3 ).MaterialName= "DeferredDemo/Ogre/Tusks" ; // tusks
            rootNode.CreateChildSceneNode( "Head" ).AttachObject( ogreHead );

            _system = new DeferredShadingSystem.DeferredShadingSystem( window.GetViewport( 0 ), scene, camera );

        }
    }
}
