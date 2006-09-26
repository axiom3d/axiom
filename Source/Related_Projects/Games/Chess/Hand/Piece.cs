using System;
using System.Collections;

using Axiom.Math;
using Axiom.Core;

using Chess.Main;
using Chess.Coldet;
using Chess.AI;

using dong = System.Int64;

namespace Chess
{
	/// <summary>
	/// Summary description for Piece.
	/// </summary>
	public class Piece
	{
		#region Fields
		protected int mColour;
		protected int mPosition;  
		protected String mShortCode;          
		protected Vector3 mOrigin;
		protected Vector3 mDestination;              
		protected Entity entity;
		protected CollisionEntity mColEntity;
		protected SceneNode mScenenode;    

		    
		private ArrayList mValidMoves;
		private static int mPieceNumber;
		private static SceneManager sceneManager;
		private static Piece[] mPieces = new Piece[32];  
		#endregion

		// Creation/destruction
		public Piece(int colour, string type,  int position, int rotation, string shortcode)
		{
			mOrigin = Vector3.Zero;
			mDestination = Vector3.Zero;
			// , mColEntity(0)

			// get a pointer for quickness
			sceneManager = ChessApplication.Instance.SceneManager;  
			  
			// set the piece's position and rotation  
			mPosition = position;  
			mShortCode = shortcode;

			// create a scene node, position it
			float outX;
			float outZ;
			Functions.ConvertCoords(mPosition, out outX, out outZ);  
			mOrigin.x = outX;
			mDestination.x = outX;
			mOrigin.z = outZ;
			mDestination.z = outZ;

			SceneNode gameRoot = ChessApplication.Instance.GetGameRootNode();
			mScenenode = gameRoot.CreateChildSceneNode("Piece" + mPieceNumber.ToString(), mOrigin);           

			// load a mesh and attach the entity  
			entity = sceneManager.CreateEntity("Piece" + mPieceNumber.ToString(), type + ".mesh");        
			mScenenode.AttachObject(entity);           

			// set the material
			mColour = colour;
			if (mColour == Player.SIDE_WHITE) 
				entity.MaterialName = "Chess/White";
			else
				entity.MaterialName = "Chess/Black";
			entity.CastShadows = true;   

			// create an entity to check for collisions  
			mColEntity = CollisionManager.Instance.createEntity(entity);  

			// rotate the piece if necessary
			if (rotation!=0) {
				mScenenode.Orientation = Quaternion.Identity;
				mScenenode.Yaw(rotation);
			}

			// increment the static count to the next piece
			mPieceNumber++;
		}
		public void Delete()
		{
			// get rid of the collision entity  
			mColEntity.Delete();

			// Destroy scene node
			if (mScenenode != null)
			{
				mScenenode.DetachAllObjects();
				if (mScenenode.Parent != null)
				{
					//mScenenode.Parent.RemoveChild(mScenenode);
					ChessApplication.Instance.SceneManager.DestroySceneNode(mScenenode.Name);
				}
			}

			// Destroy entity
			if (entity != null)
				sceneManager.RemoveEntity(entity);
		}
		public static void DestroyPieces()
		{

			for (int i = 0; i < 32; i++)
			{

				mPieces[i].Delete();
				mPieces[i]=null;

				//ChessApplication.Instance.SceneManager.DestroySceneNode("Piece" + i.ToString());
				//ChessApplication.Instance.GetGameRootNode().RemoveChild("Piece" + i.ToString());   
			}
		}
		// accessors  
		public SceneNode GetSceneNode() {return mScenenode;}  
		public int GetColour() {return mColour;}
		public Entity GetEntity() {return entity;}  
		public CollisionEntity GetCollisionEntity()
		{
			return mColEntity;
		}
  
		public int GetPosition() {return mPosition;}
		public void SetPosition(int position)
		{
			mPosition = position;
			Functions.ConvertCoords(mPosition, out mDestination.x, out mDestination.z);    
			mOrigin = mDestination - mScenenode.Position;
		}
		public void PromotePiece(string type)
		{
			// record the name
			string name = entity.Name;

			// detach and remove the old mesh
			mScenenode.DetachObject(entity);
			sceneManager.RemoveEntity(entity);

			// load and attach a new mesh  
			entity = sceneManager.CreateEntity(name, type + ".mesh");
			if (mColour == Player.SIDE_WHITE) 
				entity.MaterialName = ("Chess/White");
			else
				entity.MaterialName = ("Chess/Black");

			entity.ShowBoundingBox = true;
			mScenenode.AttachObject(entity);  

			// get rid of the previous collision entity  
			mColEntity = null;

			// create an entity to check for collisions
			mColEntity = CollisionManager.Instance.createEntity(entity); 
		}
		public bool Update(float dt)
		{
			return true;
		}

		public bool IsMoveValid(int destPosition)
		{
			int testPos;
			for (int x = 0;x< mValidMoves.Count;x++)
			{
				testPos = (int)	mValidMoves[x];
				if (testPos == destPosition)
					return true;

			}
			return false;
		}

		public static Piece GetPiece(int position)
		{
			for (int i = 0; i < 32; i++) 
			{
				if (mPieces[i].mPosition == position)
				{      
					return mPieces[i];
				}
			}    
			return null; // no piece
		}
		public static Piece GetPiece(string name)
		{
			for (int i = 0; i < 32; i++) 
			{
				if (mPieces[i].GetEntity().Name == name)
				{      
					return mPieces[i];
				}
			}    
			return null; // no piece
		}

		public void GetValidMoves()
		{
			GameAI game = GameAI.Instance;
			PlayerHuman playerHuman = (PlayerHuman)game.GetCurrentPlayer();
			mValidMoves = playerHuman.GetValidMoves(game.GetGameBoard(), mPosition);  
		}
 
		public static void SetupPieces(Board theBoard)
		{

			// clear out the old array
			if (mPieceNumber != 0)
			{
				DestroyPieces();
			}

			mPieceNumber = 0;
    
			for( int line = 0; line < 8; line++ )
			{    
				for( int col = 0; col < 8; col++ )
				{
					dong bits = Board.SquareBits[ line * 8 + col ];

					// Scan the bitboards to find a piece, if any
					int piece = 0;
					while ((piece < Board.ALL_PIECES) && ((bits & theBoard.BitBoards[piece]) == 0))
						piece++;

					// One exception: don't show the "phantom kings" which the program places
					// on the board to detect illegal attempts at castling over an attacked
					// square
					if ( ( piece == Board.BLACK_KING ) && ( ( theBoard.ExtraKings[ Player.SIDE_BLACK ] & Board.SquareBits[ line * 8 + col ] ) != 0 ) )
						piece = Board.EMPTY_SQUARE;
					if ( ( piece == Board.WHITE_KING ) && ( ( theBoard.ExtraKings[ Player.SIDE_WHITE ] & Board.SquareBits[ line * 8 + col ] ) != 0 ) )
						piece = Board.EMPTY_SQUARE;

					// Create the piece
					Piece newPiece;
					switch (piece)
					{
						case (int)Board.BoardPieces.BLACK_KING:
							newPiece = new Piece((piece % 2), "King", (line * 8) + col, 0, "BK");
							mPieces[mPieceNumber-1] = newPiece;
							break;
						case (int)Board.BoardPieces.BLACK_QUEEN:
							newPiece = new Piece((piece % 2), "Queen", (line * 8) + col, 0, "BQ");
							mPieces[mPieceNumber-1] = newPiece;
							break;
						case (int)Board.BoardPieces.BLACK_BISHOP:
							newPiece = new Piece((piece % 2), "Bishop", (line * 8) + col, 90, "BB");
							mPieces[mPieceNumber-1] = newPiece;
							break;
						case (int)Board.BoardPieces.BLACK_KNIGHT:
							newPiece = new Piece((piece % 2), "Knight", (line * 8) + col, 180, "BN");
							mPieces[mPieceNumber-1] = newPiece;
							break;        
						case (int)Board.BoardPieces.BLACK_ROOK:
							newPiece = new Piece((piece % 2), "Rook", (line * 8) + col, 0, "BR");
							mPieces[mPieceNumber-1] = newPiece;
							break;
						case (int)Board.BoardPieces.BLACK_PAWN:
							newPiece = new Piece((piece % 2), "Pawn", (line * 8) + col, 0, "BP");
							mPieces[mPieceNumber-1] = newPiece;
							break;
						case (int)Board.BoardPieces.WHITE_KING:
							newPiece = new Piece((piece % 2), "King", (line * 8) + col, 0, "WK");
							mPieces[mPieceNumber-1] = newPiece;
							break;
						case (int)Board.BoardPieces.WHITE_QUEEN:
							newPiece = new Piece((piece % 2), "Queen", (line * 8) + col, 0, "WQ");
							mPieces[mPieceNumber-1] = newPiece;
							break;
						case (int)Board.BoardPieces.WHITE_BISHOP:
							newPiece = new Piece((piece % 2), "Bishop", (line * 8) + col, 90, "WB");
							mPieces[mPieceNumber-1] = newPiece;
							break;
						case (int)Board.BoardPieces.WHITE_KNIGHT:
							newPiece = new Piece((piece % 2), "Knight", (line * 8) + col, 0, "WN");
							mPieces[mPieceNumber-1] = newPiece;
							break;
						case (int)Board.BoardPieces.WHITE_ROOK:
							newPiece = new Piece((piece % 2), "Rook", (line * 8) + col, 0, "WR");
							mPieces[mPieceNumber-1] = newPiece;
							break;
						case (int)Board.BoardPieces.WHITE_PAWN:
							newPiece = new Piece((piece % 2), "Pawn", (line * 8) + col, 0, "WP");
							mPieces[mPieceNumber-1] = newPiece;
							break;
					}      
				}    
			}  
		}

  
		 

	}
}
