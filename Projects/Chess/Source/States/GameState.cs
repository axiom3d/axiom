using System;
using System.Collections;

using Axiom.Math;
using Axiom.Input;
using Axiom.Core;
using MouseEventArgs = Axiom.Input.MouseEventArgs;
using Vector3 = Axiom.Math.Vector3;

using Chess.Main;
using Chess.AI;
using Chess.Coldet;

namespace Chess.States
{
	/// <summary>
	/// Summary description for GameState.
	/// </summary>
	public class GameState: ChessState
	{

		#region Fields
		protected float savedCameraYaw;
		protected float savedCameraPitch;
		protected bool leftMouseDown;
		protected bool rightMouseDown;  

		protected RaySceneQuery raySceneQuery;  
		protected Plane plane;
		protected Piece selectedPiece;
		protected SceneManager sceneManager;
		protected InputManager inputManager;
		protected bool pieceIsSelected;
		protected int currentPosition;
		#endregion


		#region Constructors
		public GameState()
		{
			// get some pointers  
			sceneManager = ChessApplication.Instance.SceneManager;  
			inputManager = InputManager.Instance;

			// Create RaySceneQuery
			raySceneQuery = sceneManager.CreateRayQuery();
			raySceneQuery.SortByDistance = true;

			// Create a plane
			plane = new Plane(Vector3.UnitY, 0);

		}

		#endregion

		public override void Delete()
		{
			//plane = null;
			raySceneQuery = null;
			base.Delete();
		}
		public override void FrameStarted(float dt)
		{
			base.FrameStarted (dt);
		}
	
		public override void MouseReleased(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				leftMouseDown = false;
			}
			if (e.Button == MouseButtons.Left)
			{
				rightMouseDown = false;
			}
		}
	
		public override void MousePressed(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				leftMouseDown = true;
			}
			else
			{
				leftMouseDown = false;
			}
			if (e.Button == MouseButtons.Right)
			{
				rightMouseDown = true;
			}
			else
			{
				rightMouseDown = false;
			}


			ChessApplication.Instance.AttachRootNode();

			// only show the selection fella if it's the players turn and they're not readjusting the view
			if (!gameAI.IsCurrentPlayerAI() && leftMouseDown)
			{    
				// create a ray    
				Ray mouseRay = game.CreateRay(); 

				// see if a move is being made
				if (pieceIsSelected)
				{                  
					// hide the current position node
					inputManager.HideCurrentNode();
					// get the intersection with the plane
					Axiom.Math.IntersectResult point = mouseRay.Intersects(plane);    
	
					if (point.Hit)
					{
						if (selectedPiece.IsMoveValid(currentPosition))
						{
							// feed the move into the game AI
							GameAI.Instance.CreateMove(selectedPiece.GetPosition(), currentPosition);
							// hide the rest
							inputManager.HideSelectionNode();          
							pieceIsSelected = false;
							selectedPiece = null;
							return;
						}
					}
				}    

				// hide the selection node to start
				inputManager.HideSelectionNode();
				pieceIsSelected = false;
				selectedPiece = null;

				// create a rayscenequery    
				raySceneQuery.Ray = mouseRay;       

				// see if we're hovering over a piece
				ArrayList result = raySceneQuery.Execute();
				RaySceneQueryResultEntry itr;
			    for (int i = 0; i < result.Count; i++)      
				{      
					itr = (RaySceneQueryResultEntry)result[i];
					if (itr.SceneObject.Name.Substring(0,5) == "Piece") 
					{        
						// drill a bit deeper and check for an accurate collision
						Piece thePiece = Piece.GetPiece(itr.SceneObject.Name);
						if (thePiece.GetColour() == gameAI.GetCurrentPlayerColour())
						{                    
							if(CollisionManager.Instance.collide(mouseRay, thePiece.GetCollisionEntity()))
							{
								// record this piece
								inputManager.ShowSelectionNode(thePiece.GetSceneNode());
								selectedPiece = thePiece;  
								thePiece.GetValidMoves();
								pieceIsSelected = true;
								break;
							} 
						}         
					}      
				}   
			}  
			ChessApplication.Instance.DetachRootNode();
		}
	
		public override void MouseDragged(object sender, MouseEventArgs e)
		{
			// Update CeGui with the mouse motion
			base.MouseMoved(sender,e);

			if (rightMouseDown)
			{  
//				const float maxYaw = 0.45f*(float)System.Math.PI;
				const float maxPitch = 0.05f*(float)System.Math.PI;
//				const float minPitch = 0.45f*(float)System.Math.PI;	  

				// Adjust and clamp wanted camera yaw
				game.cameraWantedYaw -= 100*e.RelativeX; 
			    
				// Adjust and clamp wanted camera pitch
				game.cameraWantedPitch -= 100*e.RelativeY; 

				float greaterDegress = Axiom.Math.Utility.RadiansToDegrees(maxPitch);
				float lesserDegress = Axiom.Math.Utility.RadiansToDegrees(maxPitch);
				if (game.cameraWantedPitch > -greaterDegress) 
				{
					game.cameraWantedPitch = -greaterDegress;
				} 
				else if (game.cameraWantedPitch < -lesserDegress) 
				{
					game.cameraWantedPitch = -lesserDegress;
				}	
			}   
		}
	
		public override void MouseMoved(object sender, MouseEventArgs e)
		{
			// update the mouse pointer
			base.MouseMoved(sender, e);

			ChessApplication.Instance.AttachRootNode();

			// Adjust and clamp wanted camera distance
			const float minDistance = 22.5f;
			const float maxDistance = 500f;

			game.cameraWantedDistance -= 100.0f*e.RelativeZ;

			if (game.cameraWantedDistance < minDistance)
				game.cameraWantedDistance = minDistance;

			if (game.cameraWantedDistance > maxDistance)
				game.cameraWantedDistance = maxDistance;  

			// move the current position
			if (!gameAI.IsCurrentPlayerAI() && pieceIsSelected)
			{
				// create a ray    
				Ray mouseRay = game.CreateRay();
			          
				// get the intersection with the plane
				IntersectResult point = mouseRay.Intersects(plane);        
				if (point.Hit)
				{
					
					//return Vector3(mOrigin + (mDirection * t));
					Vector3 position = (mouseRay.Origin + (mouseRay.Direction * point.Distance));
					//Vector3 position = mouseRay.Origin + mouseRay.Direction * (-mouseRay.Origin.y / mouseRay.Direction.y);
					inputManager.ShowCurrentNode(position, selectedPiece, out currentPosition);
				}
			}

			// hide / show the mouse pointer  
            if ( gameAI.IsCurrentPlayerAI() || rightMouseDown )
                CeGui.MouseCursor.Instance.Hide();
            else
                CeGui.MouseCursor.Instance.Show();  

			ChessApplication.Instance.DetachRootNode();
		}
	
		public override void KeyPressed(object sender, Axiom.Input.KeyEventArgs e)
		{
			// Handle keys common to all game states
			switch (e.Key)
			{
				case Axiom.Input.KeyCodes.Escape:
					StateManager.Instance.AddState(GameMenuState.Instance);
					break;
				default:
					base.KeyPressed(sender,e);
					break;
			}
		}
	
		public override void Resume()
		{
			// restart the AI thread
			gameAI.Start();  

			// Restore saved game camera yaw and pitch
			game.cameraWantedYaw = savedCameraYaw;
			game.cameraWantedPitch = savedCameraPitch;
		}
	
		public override void Pause()
		{
			// stop the AI thread if it is running
			gameAI.Pause();  

			// Save game camera yaw and pitch so it can be restored, when game continues
			savedCameraYaw = game.cameraWantedYaw;
			savedCameraPitch = game.cameraWantedPitch;
		}
	
		public override void Initialize()
		{
			base.Initialize();
		}
	}
}
