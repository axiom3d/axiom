using System;
using System.Collections.Generic;
using Axiom.Animating;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;
using Axiom.ParticleSystems;

namespace Axiom.Demos
{
    /// <summary>
    /// 
    /// </summary>
    public class LargeScene : TechDemo
    {

        const bool animateCamera = false; // set false to get keyboard control

        const bool liteSetup = false; // simpler scene for quicker startup


        public enum ResourceKind { None, Mesh, ManualMesh, ParticleSystem }


        List<AnimationState> robotStates;

        Random random = new Random();

        Spiral pathSpiral;

        List<AnimationState> flyingObjectStates = new List<AnimationState>();

        SceneNode groundSpiral1Node;

        SceneNode groundSpiral2Node;

        SceneNode robotNode;

        SceneNode firesNode;

        float cameraTime;



        protected override void ChooseSceneManager()
        {
            scene = engine.CreateSceneManager(SceneType.Generic, "TestSMInstance"); // note that Generic chooses th OctreeSM
            scene.ClearScene();
        }

        protected override void CreateScene()
        {
            SceneNode dummyNode = scene.RootSceneNode.CreateChildSceneNode();

            // environment

            scene.SetSkyBox(true, "SkyBox/Morning", 10000);

            // scene.SetFog( FogMode.Linear, ColorEx.White, .008f, 0, 30000 );

            // lighting and shadows

            scene.AmbientLight = ColorEx.Gray;

            Light l = scene.CreateLight("sun");
            l.Type = LightType.Point;
            //l.Direction = -Vector3.UnitScale;
            l.Position = Vector3.UnitY * 1000;
            l.CastShadows = true;
            l.Diffuse = ColorEx.White;
            l.Specular = ColorEx.Red;
            scene.RootSceneNode.CreateChildSceneNode().AttachObject(l);

            //scene.ShadowColor = ColorEx.Goldenrod;
            //scene.ShadowTechnique = ShadowTechnique.StencilAdditive;

            // ground

            Mesh groundMesh = MeshManager.Instance.CreatePlane("ground", ResourceGroupManager.DefaultResourceGroupName,
                new Plane(Vector3.UnitY, -100), 10000, 10000, 2, 2, true, 1, 5, 5, Vector3.UnitZ);

            Entity groundEnt = scene.CreateEntity("ground-entity", groundMesh);
            groundEnt.MaterialName = "Examples/RustySteel";
            groundEnt.CastShadows = false;
            scene.RootSceneNode.CreateChildSceneNode().AttachObject(groundEnt);

            // particle systems

            ParticleSystem ps = ParticleSystemManager.Instance.CreateSystem("system1", "ParticleSystems/GreenyNimbus", 1000);
            scene.RootSceneNode.CreateChildSceneNode().AttachObject(ps);

            Spiral particleSpiral = new Spiral();
            particleSpiral.TurnCount = 3;
            particleSpiral.Radius = 0.5f;
            particleSpiral.RadiusStep = 0.5f;
            particleSpiral.UpStep = 0;
            firesNode = liteSetup ? dummyNode : EmitObjects(particleSpiral, 24, ResourceKind.ParticleSystem, "Examples/Fireworks");
            firesNode.ScaleFactor = Vector3.UnitScale * 1.5f;

            // other objects

            Spiral wallSpiral;
            Spiral arenaSpiral;
            Spiral robotSpiral;

            wallSpiral = new Spiral();
            wallSpiral.TurnCount = 8;
            wallSpiral.Radius = 12;
            wallSpiral.RadiusStep = 0;
            wallSpiral.UpStep = 1;
            groundSpiral1Node = liteSetup ? dummyNode : EmitObjects(wallSpiral, 8 * 20, ResourceKind.ManualMesh, null);

            arenaSpiral = new Spiral();
            arenaSpiral.TurnCount = 4f;
            arenaSpiral.Radius = 10f;
            arenaSpiral.RadiusStep = -1;
            arenaSpiral.UpStep = -0.5f;
            groundSpiral2Node = EmitObjects(arenaSpiral, 8 * 20, ResourceKind.ManualMesh, null);
            groundSpiral2Node.Translate(Vector3.UnitY * 240);

            robotSpiral = new Spiral();
            robotSpiral.TurnCount = 4f;
            robotSpiral.Radius = 10f;
            robotSpiral.RadiusStep = -1;
            robotSpiral.UpStep = -0.5f;
            robotNode = liteSetup ? dummyNode : EmitObjects(robotSpiral, 10 * 4, ResourceKind.Mesh, "robot.mesh");
            robotNode.Translate(Vector3.UnitY * 270);

            // path for camera 

            pathSpiral = new Spiral();
            pathSpiral.Radius = 300;
            pathSpiral.RadiusStep = 2000f / 2;
            pathSpiral.UpStep = 2000f / 2;
            pathSpiral.TurnCount = 2;

            // set initial camera

            pathSpiral.Interpolate(0);
            camera.Position = pathSpiral.Position;
            camera.LookAt(Vector3.Zero);
            cameraTime = -4f; // delay before animation starts, seconds

            // retrieve "Walk" animation states of the entities to control them later

            robotStates = new List<AnimationState>();

            foreach (SceneNode child in robotNode.Children)
            {
                child.Scale(Vector3.UnitScale * 1.3f);

                AnimationState state = ((Entity)child.GetObject(0)).GetAnimationState("Walk");
                state.IsEnabled = true;
                robotStates.Add(state);
            }

            // create random animation tracks for flying objects

            Sphere boundingArea = new Sphere(Vector3.UnitY * 1000f, 1000f);
            int numTracks = 10;
            int numKeyFrames = 5;
            float animLength = 30;

            SceneNode helpNode = scene.RootSceneNode.CreateChildSceneNode();

            for (int i = 0; i < numTracks; i++)
            {
                // create entity and scene node

                SceneNode node = helpNode.CreateChildSceneNode();
                Entity e = scene.CreateEntity(GetUniqueName("entity"), "ogrehead.mesh");
                node.AttachObject(e);

                // create animation and animation track, link to scene node

                string animationName = GetUniqueName("animation");

                Animation anim = scene.CreateAnimation(animationName, animLength);
                anim.InterpolationMode = InterpolationMode.Spline;

                NodeAnimationTrack track = anim.CreateNodeTrack((ushort)i, node);

                float step = animLength / numKeyFrames;

                for (float t = 0; t < animLength; t += step)
                {
                    TransformKeyFrame keyf = track.CreateNodeKeyFrame(t);

                    // get random point inside the bounding sphere

                    Vector3 randomUnit = new Vector3(Utility.RangeRandom(-1, 1), Utility.RangeRandom(-1, 1), Utility.RangeRandom(-1, 1));
                    randomUnit.Normalize();

                    // upper half sphere only

                    if (randomUnit.y < 0)
                    {
                        randomUnit.y = -randomUnit.y;
                    }

                    node.Translate(Vector3.UnitY * 300);
                    keyf.Translate = boundingArea.Center / 2f + randomUnit * boundingArea.Radius;
                    keyf.Rotation = Quaternion.FromAngleAxis(Utility.RangeRandom(0, 2 * Utility.PI), randomUnit);
                }

                TransformKeyFrame first = track.GetNodeKeyFrame(0);
                TransformKeyFrame last = track.CreateNodeKeyFrame(animLength);
                last.Translate = first.Translate;
                last.Rotation = first.Rotation;
                last.Scale = first.Scale;

                // create animation state to get control

                AnimationState state = scene.CreateAnimationState(animationName);
                state.Length = animLength;
                state.IsEnabled = true;

                flyingObjectStates.Add(state);

                // create a ribbon trail for some more effect
                // TODO this doesn't seem to work well, not sure why

                RibbonTrail trail = scene.CreateRibbonTrail(GetUniqueName("ribbon-trail"));
                node.AttachObject(trail);
                trail.TrailLength = 400;
                trail.MaxChainElements = 100;
                trail.NumberOfChains = 1;

                trail.SetInitialColor(0, new ColorEx(0.5f, 0.4f, 0.7f, 1.0f));
                trail.SetColorChange(0, new ColorEx(0.5f, 0.5f, 0.5f, 0.5f));
                trail.SetInitialWidth(0, 50);
                trail.AddNode(node);
            }

            // play with post processing effects

            CompositorManager.Instance.AddCompositor(window.GetViewport(0), "Bloom");
            CompositorManager.Instance.AddCompositor(window.GetViewport(0), "Glass");
            CompositorManager.Instance.AddCompositor(window.GetViewport(0), "Tiling");
            CompositorManager.Instance.AddCompositor(window.GetViewport(0), "B&W");
            CompositorManager.Instance.AddCompositor(window.GetViewport(0), "Old TV");
            CompositorManager.Instance.AddCompositor(window.GetViewport(0), "Embossed");
            CompositorManager.Instance.AddCompositor(window.GetViewport(0), "Old Movie");

            //CompositorManager.Instance.SetCompositorEnabled(window.GetViewport(0), "Bloom", true);
        }

        protected override bool OnFrameStarted(object source, FrameEventArgs e)
        {
            if (Root.Instance.CurrentFrameCount < 3)
            {
                return true;
            }

            // animate flying objects

            foreach (AnimationState s in flyingObjectStates)
            {
                s.AddTime(e.TimeSinceLastFrame);
            }

            // animate robots

            foreach (AnimationState state in robotStates)
            {
                state.AddTime(e.TimeSinceLastFrame * 2);
            }

            // animate spirals

            groundSpiral1Node.Rotate(Vector3.UnitY, e.TimeSinceLastFrame);

            firesNode.Rotate(Vector3.UnitY, e.TimeSinceLastFrame);

            groundSpiral2Node.Rotate(Vector3.UnitY, -e.TimeSinceLastFrame);

            robotNode.Rotate(Vector3.UnitY, -e.TimeSinceLastFrame);

            // animate camera

            if (animateCamera)
            {
                cameraTime += e.TimeSinceLastFrame;

                if (cameraTime >= 0) // allows for initial delay
                {
                    pathSpiral.Interpolate((cameraTime / pathSpiral.TurnCount * 0.1f) % 1.0f); // TODO horrible use, check Spiral impl
                    camera.Position = pathSpiral.Position;
                    camera.LookAt(Vector3.Zero);
                }
            }

            return base.OnFrameStarted(source, e);
        }

        protected SceneNode EmitObjects(Spiral spiral, int keyPointCount, ResourceKind resource, string resourceName)
        {
            SceneNode root = scene.RootSceneNode.CreateChildSceneNode();

            float timeStep = 1.0f / (float)keyPointCount;

            for (float time = 0; time < 1.0f; time += timeStep)
            {
                SceneNode node = root.CreateChildSceneNode();

                spiral.Interpolate(time);

                node.Position = spiral.Position * 100f;
                node.Orientation = spiral.Orientation;
                // ... keep unit scale.

                if (resource != ResourceKind.None)
                {
                    MovableObject obj;
                    string objName = GetUniqueName(resourceName);

                    switch (resource)
                    {
                        case ResourceKind.Mesh:
                            obj = scene.CreateEntity(objName, resourceName);
                            break;
                        case ResourceKind.ManualMesh:
                            obj = scene.CreateEntity(objName, CreateCube());
                            break;
                        case ResourceKind.ParticleSystem:
                            obj = ParticleSystemManager.Instance.CreateSystem(objName, resourceName);
                            break;
                        default:
                            throw new AxiomException("Enum entry {0} not supported", resource);
                    }

                    node.AttachObject(obj);
                }
            }

            return root;
        }

        const float halfSize = 30;

        Vector3[] cubeVertices = new Vector3[] 
        {
            new Vector3(-halfSize, -halfSize, -halfSize),
            new Vector3(halfSize, -halfSize, -halfSize),
            new Vector3(halfSize, halfSize, -halfSize),
            new Vector3(-halfSize, halfSize, -halfSize),

            new Vector3(-halfSize, -halfSize, halfSize),
            new Vector3(halfSize, -halfSize, halfSize),
            new Vector3(halfSize, halfSize, halfSize),
            new Vector3(-halfSize, halfSize, halfSize)
        };

        int[] cubeIndices = new int[6 * 3 * 2]
        {
            // bk
            0, 2, 1,
            0, 3, 2,
            // fr
            4, 5, 6,
            4, 6, 7,
            // up
            3, 7, 6,
            3, 6, 2,
            // dn
            0, 5, 4,
            0, 1, 5,
            // lf
            0, 4, 7,
            0, 7, 3,
            // rt
            1, 6, 5,
            1, 2, 6
        };

        protected Mesh CreateCube()
        {
            ManualObject mo = new ManualObject(GetUniqueName("manual-cube"));
            mo.Begin(CreateMaterial().Name, OperationType.TriangleList);

            for (int i = 0; i < cubeVertices.Length; i++)
            {
                Vector3 v = cubeVertices[i];
                mo.Position(v);
                v.Normalize();
                mo.Normal(v);

                //mo.Color(new ColorEx(0.6f, 
                //                     Utility.RangeRandom(0,1),
                //                     Utility.RangeRandom(0,1),
                //                     Utility.RangeRandom(0,1)));

                mo.TextureCoord(Utility.RangeRandom(0, 1), Utility.RangeRandom(0, 1));
            }

            for (int i = 0; i < cubeIndices.Length; i++)
            {
                mo.Index((ushort)cubeIndices[i]);
            }

            mo.End();

            return mo.ConvertToMesh(GetUniqueName("manual-cube-mesh"), ResourceGroupManager.DefaultResourceGroupName);
        }

        protected Material CreateMaterial()
        {
            Material mat = (Material)MaterialManager.Instance.Create(GetUniqueName("material"), ResourceGroupManager.DefaultResourceGroupName);
            mat.Load();

            // apply texture

            Pass pass = mat.GetBestTechnique().GetPass(0);
            pass.CreateTextureUnitState("Rock.jpg");

            // add color pass

            pass = mat.GetBestTechnique().CreatePass();
            pass.Ambient = new ColorEx(0.6f,
                                      Utility.RangeRandom(0, 1),
                                      Utility.RangeRandom(0, 1),
                                      Utility.RangeRandom(0, 1));

            pass.SetSceneBlending(SceneBlendType.TransparentColor);

            // other properties

            mat.Lighting = true;
            mat.ReceiveShadows = false;

            return mat;
        }

        static uint uniqueKeyCount = 0;

        protected string GetUniqueName(string prefix)
        {
            // TODO: might be nice to have this in Axiom.Utility
            // idea: two properties, UniqueName for strings and UniqueId for ints.
            // or: a generic GetId<T>() method
            return prefix + uniqueKeyCount++;
        }
    }

    /// <summary>
    /// A simple spiral implementation.
    /// </summary>
    public class Spiral
    {

        // TODO: FollowCurve doesn't work
        // TODO: Interpolation time match curve length

        #region Constructors

        public Spiral()
        {
            this.radius = 4.0f;
            this.radiusStep = -0.5f;
            this.upStep = 0.8f;
            this.start = 0;
            this.turnCount = 6.0f;
            this.followCurve = false; // TODO set true when it works
        }

        #endregion

        #region Properties

        #region Parameters

        public float Radius
        {
            get { return radius; }
            set { radius = value; }
        }

        protected float radius;

        public float RadiusStep
        {
            get { return radiusStep; }
            set { radiusStep = value; }
        }

        protected float radiusStep;

        public float UpStep
        {
            get { return upStep; }
            set { upStep = value; }
        }

        protected float upStep;

        public float Start
        {
            get { return start; }
            set { start = value; }
        }

        protected float start;

        /// <summary>
        /// Specifies how many times the spiral turns by 360 degrees around the local y axis.
        /// Since it is a float, values like 1.5f can be specified, this would be interpreted as one-and-half turns.
        /// </summary>
        public float TurnCount
        {
            get { return turnCount; }
            set { turnCount = value; }
        }

        protected float turnCount;

        /// <summary>
        /// Interpolated orientation follows curve orientation.
        /// </summary>
        public bool FollowCurve
        {
            get { return followCurve; }
            set { followCurve = value; }
        }

        protected bool followCurve;

        #endregion

        public Vector3 Position
        {
            get { return position; }
            set { position = value; }
        }

        protected Vector3 position;

        public Quaternion Orientation
        {
            get { return orientation; }
            set { orientation = value; }
        }

        protected Quaternion orientation;

        #endregion

        #region Methods

        public void Interpolate(float time)
        {
            time *= turnCount;

            // get emit point transforms

            float r = radius + time * radiusStep;
            float circAngle = time * 2 * Utility.PI;

            Vector3 pos = Vector3.Zero;
            pos.x = r * Utility.Sin(circAngle);
            pos.z = r * Utility.Cos(circAngle);
            pos.y = time * upStep;

            Quaternion ori;

            if (followCurve)
            {
                // TODO why the hell this doesn't work

                Vector3 axis = new Vector3(pos.x, 0, pos.z);
                axis.Normalize();

                float angle = (upStep / 10.0f) * (float)System.Math.PI; // HACK fake calc

                ori = Quaternion.FromAngleAxis(angle, axis);
            }
            else
            {
                ori = Quaternion.Identity;
            }

            this.position = pos;
            this.orientation = ori;
        }

        #endregion
    }
}