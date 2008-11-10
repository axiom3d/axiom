using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Configuration;
using Axiom.RenderSystems.Xna;
using Axiom.Controllers;
using Axiom.Controllers.Canned;
using System.Reflection;
using System.Collections;

namespace Axiom.Demos.CE
{


	/// <summary>
	///		A class for rendering lines in 3d.
	/// </summary>
	public class Line3d : SimpleRenderable
	{
		// constants for buffer source bindings
		const int POSITION = 0;
		const int COLOR = 1;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="startPoint">Point where the line will start.</param>
		/// <param name="direction">The direction the vector is heading in.</param>
		/// <param name="length">The length (magnitude) of the line vector.</param>
		/// <param name="color">The color which this line should be.</param>
		public Line3d(Axiom.Math.Vector3 startPoint, Axiom.Math.Vector3 direction, float length, ColorEx color)
		{
			// normalize the direction vector to ensure all elements fall in [0,1] range.
			direction.Normalize();

			// calculate the actual endpoint
			Axiom.Math.Vector3 endPoint = startPoint + (direction * length);

			vertexData = new Axiom.Graphics.VertexData();
			vertexData.vertexCount = 2;
			vertexData.vertexStart = 0;

			Axiom.Graphics.VertexDeclaration decl = vertexData.vertexDeclaration;
			VertexBufferBinding binding = vertexData.vertexBufferBinding;

			// add a position and color element to the declaration
			decl.AddElement(POSITION, 0, VertexElementType.Float3, VertexElementSemantic.Position);
			decl.AddElement(COLOR, 0, VertexElementType.Color, VertexElementSemantic.Diffuse);

			// create a vertex buffer for the position
			HardwareVertexBuffer buffer =
				HardwareBufferManager.Instance.CreateVertexBuffer(
				decl.GetVertexSize(POSITION),
				vertexData.vertexCount,
				Axiom.Graphics.BufferUsage.StaticWriteOnly);

			Axiom.Math.Vector3[] pos = new Axiom.Math.Vector3[] { startPoint, endPoint };

			// write the data to the position buffer
			buffer.WriteData(0, buffer.Size, pos, true);

			// bind the position buffer
			binding.SetBinding(POSITION, buffer);

			// create a color buffer
			buffer = HardwareBufferManager.Instance.CreateVertexBuffer(
				decl.GetVertexSize(COLOR),
				vertexData.vertexCount,
				Axiom.Graphics.BufferUsage.StaticWriteOnly);

			int colorValue = Root.Instance.RenderSystem.ConvertColor(color);

			int[] colors = new int[] { colorValue, colorValue };

			// write the data to the position buffer
			buffer.WriteData(0, buffer.Size, colors, true);

			// bind the color buffer
			binding.SetBinding(COLOR, buffer);

			// MATERIAL
			// grab a copy of the BaseWhite material for our use
			Material material = MaterialManager.Instance.GetByName("BaseWhite");
			material = material.Clone("LineMat");
			// disable lighting to vertex colors are used
			material.Lighting = false;
			// set culling to none so the triangle is drawn 2 sided
			material.CullingMode = CullingMode.None;

			this.Material = material;

			// set the bounding box of the line
			this.box = new Axiom.Math.AxisAlignedBox(startPoint, endPoint);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="camera"></param>
		/// <returns></returns>
		public override float GetSquaredViewDepth(Camera camera)
		{
			Axiom.Math.Vector3 min, max, mid, dist;
			min = box.Minimum;
			max = box.Maximum;
			mid = ((min - max) * 0.5f) + min;
			dist = camera.DerivedPosition - mid;

			return dist.LengthSquared;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="op"></param>
		public override void GetRenderOperation(RenderOperation op)
		{
			op.vertexData = vertexData;
			op.indexData = null;
			op.operationType = OperationType.LineList;
			op.useIndices = false;
		}

		public override float BoundingRadius
		{
			get
			{
				return 0;
			}
		}

	}


	/// <summary>
	///		A class for rendering a simple triangle with colored vertices.
	/// </summary>
	public class Triangle : SimpleRenderable
	{
		// constants for buffer source bindings
		const int POSITION = 0;
		const int COLOR = 1;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="v1"></param>
		/// <param name="v2"></param>
		/// <param name="v3"></param>
		public Triangle(Axiom.Math.Vector3 v1, Axiom.Math.Vector3 v2, Axiom.Math.Vector3 v3, ColorEx c1, ColorEx c2, ColorEx c3)
		{
			vertexData = new Axiom.Graphics.VertexData();
			vertexData.vertexCount = 3;
			vertexData.vertexStart = 0;

			Axiom.Graphics.VertexDeclaration decl = vertexData.vertexDeclaration;
			VertexBufferBinding binding = vertexData.vertexBufferBinding;

			// add a position and color element to the declaration
			decl.AddElement(POSITION, 0, VertexElementType.Float3, VertexElementSemantic.Position);
			decl.AddElement(COLOR, 0, VertexElementType.Color, VertexElementSemantic.Diffuse);

			// POSITIONS
			// create a vertex buffer for the position
			HardwareVertexBuffer buffer =
				HardwareBufferManager.Instance.CreateVertexBuffer(
				decl.GetVertexSize(POSITION),
				vertexData.vertexCount,
				Axiom.Graphics.BufferUsage.StaticWriteOnly);

			Axiom.Math.Vector3[] positions = new Axiom.Math.Vector3[] { v1, v2, v3 };

			// write the positions to the buffer
			buffer.WriteData(0, buffer.Size, positions, true);

			// bind the position buffer
			binding.SetBinding(POSITION, buffer);

			// COLORS
			// create a color buffer
			buffer = HardwareBufferManager.Instance.CreateVertexBuffer(
				decl.GetVertexSize(COLOR),
				vertexData.vertexCount,
				Axiom.Graphics.BufferUsage.StaticWriteOnly);

			// create an int array of the colors to use.
			// note: these must be converted to the current API's
			// preferred packed int format
			int[] colors = new int[] {
                Root.Instance.RenderSystem.ConvertColor(c1),
                Root.Instance.RenderSystem.ConvertColor(c2),
                Root.Instance.RenderSystem.ConvertColor(c3)
            };

			// write the colors to the color buffer
			buffer.WriteData(0, buffer.Size, colors, true);

			// bind the color buffer
			binding.SetBinding(COLOR, buffer);

			// MATERIAL
			// grab a copy of the BaseWhite material for our use
			Material material = MaterialManager.Instance.GetByName("BaseWhite");
			material = material.Clone("TriMat");

			// disable lighting to vertex colors are used
			material.Lighting = false;
			// set culling to none so the triangle is drawn 2 sided
			material.CullingMode = CullingMode.None;

			materialName = "TriMat";

			this.Material = material;

			// set the bounding box of the tri
			// TODO: not right, but good enough for now
			this.box = new Axiom.Math.AxisAlignedBox(new Axiom.Math.Vector3(25, 50, 0), new Axiom.Math.Vector3(-25, 0, 0));
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="camera"></param>
		/// <returns></returns>
		public override float GetSquaredViewDepth(Camera camera)
		{
			Axiom.Math.Vector3 min, max, mid, dist;
			min = box.Minimum;
			max = box.Maximum;
			mid = ((min - max) * 0.5f) + min;
			dist = camera.DerivedPosition - mid;

			return dist.LengthSquared;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="op"></param>
		public override void GetRenderOperation(RenderOperation op)
		{
			op.vertexData = vertexData;
			op.indexData = null;
			op.operationType = OperationType.TriangleList;
			op.useIndices = false;
		}

		public override float BoundingRadius
		{
			get
			{
				return 0;
			}
		}
	}

	/// <summary>
	/// This is the main type for your game
	/// </summary>
	public class Game1 : Microsoft.Xna.Framework.Game
	{
		protected const string CONFIG_FILE = @"EngineConfig.xml";
		private Root engine;
		private RenderSystem renderSystem;
		protected Camera camera;
		protected Axiom.Core.Viewport viewport;
		protected SceneManager scene;
		protected RenderWindow window;
        protected Axiom.Math.Vector3 camAccel = Axiom.Math.Vector3.Zero;
        protected Axiom.Math.Vector3 camVelocity = Axiom.Math.Vector3.Zero;
        protected float camSpeed = 2.5f;

		public Game1()
		{
            try
            {
                engine = new Root( CONFIG_FILE, "AxiomDemos.log" );
                Axiom.RenderSystems.Xna.Plugin renderSystemPlugin = new Axiom.RenderSystems.Xna.Plugin();
                renderSystemPlugin.Start();
                engine.RenderSystem = engine.RenderSystems[ 0 ];
                ResourceManager.AddCommonArchive( "Content", "Folder" );
                window = engine.Initialize( false );
                window = Root.Instance.CreateRenderWindow( "Main", this.Window.ClientBounds.Width, this.Window.ClientBounds.Height, 32, true );

                scene = engine.SceneManagers.GetSceneManager( SceneType.Generic );
                scene.ClearScene();
                // create a camera and initialize its position
                camera = scene.CreateCamera( "MainCamera" );
                camera.Position = new Axiom.Math.Vector3( 0, 0, 500 );
                camera.LookAt( new Axiom.Math.Vector3( 0, 0, -300 ) );
                // set the near clipping plane to be very close
                camera.Near = 5;

                viewport = window.AddViewport( camera, 0, 0, 1.0f, 1.0f, 100 );
                viewport.BackgroundColor = ColorEx.Black;

                TextureManager.Instance.DefaultNumMipMaps = 5;

                Content.RootDirectory = "Content";

                // create a 3d line
                Line3d line = new Line3d( new Axiom.Math.Vector3( 0, 0, 30 ), Axiom.Math.Vector3.UnitY, 50, ColorEx.Blue );
                line.Material = MaterialManager.Instance.GetByName("Simple");

                Triangle tri = new Triangle(
                    new Axiom.Math.Vector3( -25, 0, 0 ),
                    new Axiom.Math.Vector3( 0, 50, 0 ),
                    new Axiom.Math.Vector3( 25, 0, 0 ),
                    ColorEx.Red,
                    ColorEx.Blue,
                    ColorEx.Green );

                tri.Material = MaterialManager.Instance.GetByName( "Simple" );

                // create a node for the line
                SceneNode node = scene.RootSceneNode.CreateChildSceneNode();
                SceneNode lineNode = node.CreateChildSceneNode();
                SceneNode triNode = node.CreateChildSceneNode();
                triNode.Position = new Axiom.Math.Vector3( 50, 0, 0 );

                // add the line and triangle to the scene
                lineNode.AttachObject( line );
                triNode.AttachObject( tri );

                // create a node rotation controller value, which will mark the specified scene node as a target of the rotation
                // we want to rotate along the Y axis for the triangle and Z for the line (just for the hell of it)
                NodeRotationControllerValue rotate = new NodeRotationControllerValue( triNode, Axiom.Math.Vector3.UnitY );
                NodeRotationControllerValue rotate2 = new NodeRotationControllerValue( lineNode, Axiom.Math.Vector3.UnitZ );

                // the multiply controller function will multiply the source controller value by the specified value each frame.
                MultipyControllerFunction func = new MultipyControllerFunction( 50 );

                // create a new controller, using the rotate and func objects created above.  there are 2 overloads to this method.  the one being
                // used uses an internal FrameTimeControllerValue as the source value by default.  The destination value will be the node, which 
                // is implemented to simply call Rotate on the specified node along the specified axis.  The function will mutiply the given value
                // against the source value, which in this case is the current frame time.  The end result in this demo is that if 50 is specified in the 
                // MultiplyControllerValue, then the node will rotate 50 degrees per second.  since the value is scaled by the frame time, the speed
                // of the rotation will be consistent on all machines regardless of CPU speed.
                ControllerManager.Instance.CreateController( rotate, func );
                ControllerManager.Instance.CreateController( rotate2, func );

                // place the camera in an optimal position
                camera.Position = new Axiom.Math.Vector3( 30, 30, 220 );
            }
            catch ( Exception ex )
            {
                string msg = ex.Message;
            }
		}

		/// <summary>
		/// Allows the game to perform any initialization it needs to before starting to run.
		/// This is where it can query for any required services and load any non-graphic
		/// related content.  Calling base.Initialize will enumerate through any components
		/// and initialize them as well.
		/// </summary>
		protected override void Initialize()
		{
			// TODO: Add your initialization logic here

			base.Initialize();
		}

		/// <summary>
		/// LoadContent will be called once per game and is the place to load
		/// all of your content.
		/// </summary>
		protected override void LoadContent()
		{
			// TODO: use this.Content to load your game content here
		}

		/// <summary>
		/// UnloadContent will be called once per game and is the place to unload
		/// all content.
		/// </summary>
		protected override void UnloadContent()
		{
			// TODO: Unload any non ContentManager content here
		}

		/// <summary>
		/// Allows the game to run logic such as updating the world,
		/// checking for collisions, gathering input, and playing audio.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Update(GameTime gameTime)
		{
            GamePadState state = GamePad.GetState(PlayerIndex.One);

            // Allows the game to exit
            if(state.Buttons.Back == ButtonState.Pressed)
                this.Exit();

            float dt = ((float)gameTime.ElapsedRealTime.Milliseconds) * 0.001f;
            //clarabie - temporary. Something's wrong with the dt calculation above
            //so forcing it to 0.033 (30 fps)
            dt = 0.033f;

            camAccel = Axiom.Math.Vector3.Zero; // reset acceleration zero
            float scaleMove = 10 * dt; // motion scalar
            float scaleTurn = 20 * dt; // turn rate scalar

            camAccel.z = -1.0f * state.ThumbSticks.Left.Y;
            camAccel.x = state.ThumbSticks.Left.X;

            camVelocity += (camAccel * scaleMove * camSpeed);
            camera.MoveRelative(camVelocity);

            if (camAccel == Axiom.Math.Vector3.Zero)
            {
                camVelocity *= (1 - (4 * dt));
            }

            float yaw = -1.0f * scaleTurn * state.ThumbSticks.Right.X;
            float pitch = scaleTurn * state.ThumbSticks.Right.Y;
            if (yaw != 0)
            {
                camera.Yaw(yaw);
            }
            camera.Pitch(pitch);

            engine.RenderOneFrame();

            base.Update(gameTime); 
		}

		/// <summary>
		/// This is called when the game should draw itself.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Draw(GameTime gameTime)
		{
			//GraphicsDevice.Clear(Color.CornflowerBlue);

			// TODO: Add your drawing code here

			//base.Draw(gameTime);
            //engine.RenderOneFrame();
		}
	}
}
