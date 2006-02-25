using System;
using System.Collections;

namespace RealmForge
{
    /// <summary>
    /// A static class that enumerates all the different meshes that come with the RealmForge Media Library
    /// </summary>
    public class Meshes
    {
        #region Standard
        public const string Sphere = "sphere.mesh";
        public const string Sphere1 = "sphere100.mesh";
        public const string Sphere2 = "geosphere1000.mesh";
        public const string Sphere3 = "geosphere2000.mesh";
        public const string Sphere4 = "geosphere4500.mesh";
        public const string Sphere5 = "geosphere8000.mesh";
        public const string Sphere6 = "geosphere12500.mesh";
        public const string Sphere7 = "geosphere19220.mesh";
        public const string Teapot = "teapot.mesh";
        public const string Box = "box.mesh";
        public const string Cube = "cube.mesh";
        public const string PhysicsCube = "cube100.mesh";
        public const string Ball = "ball.mesh";
        public const string AxisArrows = "axes.mesh";
        public const string RealmForgeLogo3DS = "RealmForge.3ds";
        public const string RealmForgeLogo = "RealmForge.mesh";
        public const string Cylinder = "cylinder100.mesh";
        public const string Plane = "Prefab_Plane";

        #endregion

        #region Demos

        public const string AtheneStatue = "athene.mesh";
        public const string Barrier = "barrier.mesh";
        public const string BspDemoLevel = "ogretestmap.bsp";
        public const string Column = "column.mesh";
        public const string Dragon = "dragon.mesh";
        public const string Fish = "fish.mesh";
        public const string Iglu = "iglu.mesh";
        public const string Knot = "knot.mesh";
        public const string OgreHead = "ogrehead.mesh";
        public const string OgreHeadStatue = "head1.mesh";
        public const string Ninja = "ninja.mesh";
        public const string Penguin = "penguin.mesh";
        public const string Ramp = "ramp.mesh";
        public const string Robot = "robot.mesh";
        public const string Sled = "sled.mesh";
        public const string SnowBall = "SnowBall.mesh";
        public const string SpaceShip = "razor.mesh";
        public const string SpaceShip2 = "RZR-002.mesh";
        public const string Wheel = "wheel.mesh";
        public const string Zombie = "zombie.mesh";
        public const string PoolFloor = "PoolFloor.mesh";
        public const string PoolRim = "UpperSurround.mesh";
        public const string PoolSides = "LowerSurround.mesh";

        public const string Ferrari = "Ferrari.mesh";
        public const string LowerSnowmanBall = "SnowMan1.mesh";
        public const string UpperSnowmanBall = "SnowMan2.mesh";
        public const string TopHat = "SnowManHat.mesh";
        public const string Carrot = "SnowManNose.mesh";
        public const string MiniCooper = "mini.mesh";
        public const string CarWheel = "wheel.mesh";
        public const string Terrain = "terrain.mesh";
        public const string Terrain2 = "carterrain.mesh";
        public const string TrafficCone = "pylone.mesh";
        public const string Pillar = "Pillar.mesh";
        #endregion

        /*
			 * BobaFett1.mesh
			 * BobaFett.mesh
			 * BobaFettHead.mesh
			 * head1.mesh
			 * head2.mesh
			 */

        public static string[] AllMeshes
        {
            get
            {
                return Reflector.GetConstantFieldStringValues( typeof( Meshes ) );
            }
        }


        public static string[] AllNames
        {
            get
            {
                return Reflector.GetConstantFieldNames( typeof( Meshes ) );
            }
        }


        public static IDictionary NameMeshTable
        {
            get
            {
                return Reflector.GetConstantValueTable( typeof( Meshes ) );
            }
        }



    }
}
