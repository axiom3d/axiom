using System;
using Axiom.Math;
using Axiom.Core;
using Axiom.Graphics;

namespace Chess.Main
{
	/// <summary>
	/// Summary description for InputManager.
	/// </summary>
	public class InputManager
	{
		#region Fields
		protected SceneManager sceneManager;    
		protected Entity selectionEntity;
		protected Entity currentEntity;
		protected SceneNode selectionNode;
		protected SceneNode currentNode;  
		protected MovablePlane plane;
		#endregion

		#region Constructors
		#region Singleton implementation

		private static InputManager instance;
		public InputManager()
		{
			if (instance == null) 
			{
				instance = this;
				// keep a pointer to the scene manager for quickness-sake
				sceneManager = ChessApplication.Instance.SceneManager;    

				// create/get an entity      
				selectionEntity = sceneManager.CreateEntity("Selector", "Selector.mesh");            
				selectionEntity.MaterialName = ("Chess/SelectionMat");  

				// create a plane to show the current mouse position    
				plane = new MovablePlane("CurrentMPlane");
				plane.D = 0f;
				plane.Normal = Vector3.UnitY;
				MeshManager.Instance.CreatePlane("CurrentPlane",plane.Plane, 20, 20, 1, 1, true, 1, 1, 1, Vector3.UnitZ);
				currentEntity = sceneManager.CreateEntity("CurrentEntity", "CurrentPlane" );  

				// create the nodes to hang off          
				selectionNode = ChessApplication.Instance.GetGameRootNode().CreateChildSceneNode("SelectorNode");           
				selectionNode.AttachObject(selectionEntity);
				  
				currentNode = ChessApplication.Instance.GetGameRootNode().CreateChildSceneNode("CurrentNode");             
				currentNode.AttachObject(currentEntity);
				    
				selectionNode.Visible = (false);
				currentNode.Visible = (false);
			}
		}
		#endregion

		public static InputManager Instance 
		{
			get 
			{
				return instance;
			}
		}
		#endregion

		#region Methods
		public void Delete()
		{
			plane = null;

			// Destroy scene nodes
			if (selectionNode != null) 
			{    
				selectionNode.DetachAllObjects();
				selectionNode.Parent.RemoveChild(selectionNode);    
				selectionNode = null;
			}

			if (currentNode != null) 
			{    
				currentNode.DetachAllObjects();
				currentNode.Parent.RemoveChild(currentNode);    
				currentNode = null;
			}
			// get rid of the entities
			if (selectionEntity != null) 
			{
				sceneManager.RemoveEntity(selectionEntity);    
				selectionEntity = null;
			}

			if (currentEntity != null) 
			{
				sceneManager.RemoveEntity(currentEntity);    
				currentEntity = null;
			}
		}

		public void ShowSelectionNode(SceneNode node)
		{
			// move the node
			selectionNode.Position = node.Position;  

			// make it visible
			selectionNode.Visible = true;  

			// hide the movement node
			HideCurrentNode();
		}
		public void HideSelectionNode()
		{
			selectionNode.Visible = false;  
		}

		public void ShowCurrentNode(Vector3 position, Piece thePiece, out int currentPosition)
		{
			Vector3 showPos = new Vector3(0,0,0);
			int pos=0;

			if (!Functions.GetNearestPosition(position, out showPos, out pos))
				pos = -1;    
			else
			{
				// make sure there's not a piece of the same colour on this position
				Piece testPiece = Piece.GetPiece(pos);
				if (testPiece != null)
				if (thePiece.GetColour() == testPiece.GetColour())
					pos = -1;
			}

			// set which whether this is a legal move or not
			if (thePiece.IsMoveValid(pos))
				currentEntity.MaterialName = "Chess/GreenSel";  
			else
				currentEntity.MaterialName = "Chess/RedSel";  

			currentNode.Position = (showPos);

			// only show the node if we've moved away from the piece  
			currentNode.Visible = (thePiece.GetPosition() != pos && pos != -1);  

			// return the converted position
			currentPosition = pos;
		}
		public void HideCurrentNode()
		{
			currentNode.Visible = false;
		}

		public void Update(float dt)
		{
			// rotate the entity
			if (selectionNode != null)
				selectionNode.Yaw(-360 * dt,TransformSpace.Parent);
		}

		#endregion

		

	}
}
