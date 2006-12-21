using System;

using Axiom.Math;
using Axiom.Core;
using Axiom.ParticleSystems;

using Chess.Main;

namespace Chess
{
	/// <summary>
	/// Summary description for Hand.
	/// </summary>
	public class Hand
	{
		#region static methods
		private static bool IsInitialised = false;
		private static Hand[] mHands = new Hand[2];
		public static Hand GetHand(int colour) {return mHands[colour];}
		public static void CreateHands()
		{
			// get rid of previous hands
			if (IsInitialised)
				DestroyHands();

			// create new hands
			for (int i = 0; i < 2; i++) 
			{        
				mHands[i] = new Hand(i);  
			}  

			// reset these values  
			IsInitialised = true;
		}
		public static void DestroyHands()
		{
			// delete any previous hands    
			for (int i = 0; i < 2; i++)  
			{
				mHands[i].Delete();
				mHands[i] = null;  
			}
		}
		public static void UpdateHands(float dt)
		{
			HandState.StateTypes nextState = HandState.StateTypes.Static;
			if (!IsInitialised) return;
			// cycle through the hands
			for (int i = 0; i < 2; i++)    
			{
				// find out which state we are in - reflection(ish)
				HandState.StateTypes currentState = mHands[i].handState.StateType;
				
				// call the relevant update routine
				switch (currentState)
				{    
					case HandState.StateTypes.Static:

						nextState = mHands[i].handState.Update(dt);  
						break;
					case HandState.StateTypes.Pickup:      
						nextState = mHands[i].handState.Update(dt);  
						break;
					case HandState.StateTypes.MovePiece:      
						nextState = mHands[i].handState.Update(dt);        
						break;
					case HandState.StateTypes.Take:
						nextState = mHands[i].handState.Update(dt);  
						break;
					case HandState.StateTypes.Remove:
						nextState = mHands[i].handState.Update(dt);  
						break;
					case HandState.StateTypes.SimpleReturn:
					case HandState.StateTypes.CaptureReturn:
						nextState = mHands[i].handState.Update(dt);  
						break;  
					case HandState.StateTypes.NewPiece:
						nextState = mHands[i].handState.Update(dt);  
						break;
				}               
				// change states if the last one has ended
				if (currentState != nextState)
				{
					mHands[i].SetState(nextState);      
				}
			}
		}
		#endregion

		#region Fields
		protected  int piecesTaken;  
		protected  int colour;                     
		protected  Entity entity;
		protected  SceneNode scenenode;  
		protected  HandState handState;  
		private SceneManager sceneManager;    
		private ParticleSystem magicWandParticleSystem; 
		#endregion

		#region Constructors
		public Hand(int colour)
		{
			this.colour = 0;
			entity = null;
			scenenode=null;
			handState = null;
			piecesTaken = 0;

			// keep a pointer to the scene manager for quickness-sake
			sceneManager = ChessApplication.Instance.SceneManager;   

			// set which colour this hand represents
			this.colour = colour;  

			// create an entity  
			entity = sceneManager.CreateEntity("Hand" + colour.ToString(), "Hand.mesh");        

			// attach it to the scene node  
			scenenode = ChessApplication.Instance.GetGameRootNode().CreateChildSceneNode("Hand" + colour.ToString());
			scenenode.AttachObject(entity);              

			// if it's the black hand, rotate it 180
			if (colour!=0) {        
				scenenode.Orientation = Quaternion.Identity;
				scenenode.Yaw(180f);    
			}   

			// set the material and the shadow casting ...
			// ...we're not using shadows yet but in case we change our minds...
			entity.MaterialName = ("Chess/Hand");  
			entity.CastShadows = (false);       

			// set up the particle effects    
			magicWandParticleSystem = ParticleSystemManager.Instance.CreateSystem("MagicWand" + colour.ToString(), "MagicWand");      
			scenenode.AttachObject(magicWandParticleSystem);        

			// set the hand into a waiting state  
			SetAction(HandState.ActionTypes.Waiting);
		}

		#endregion

		#region Properties
		public int GetColour() {return colour;}
		public SceneManager GetSceneManagerPtr()
		{
			return this.sceneManager;
		}
		public Entity GetEntityPtr()
		{
			return entity;
		}
		public SceneNode GetSceneNodePtr()
		{
			return scenenode;
		}
		public HandState GetHandState()
		{
			return handState;
		}
		public int GetNextTakeSlot() {return piecesTaken++;}

		#endregion

		#region public methods
		public void ToggleParticle(bool toggle)
		{
			
            //for (int i = 0; i < magicWandParticleSystem.; i++)
            //{
            //    magicWandParticleSystem.Particles[i].IsEnabled = toggle;
            //}
		}

		public void Delete()
		{
			// get rid of the hand state  
			handState.Delete();

			// Destroy scene node
			if (scenenode != null) 
			{
				ChessApplication.Instance.SceneManager.DestroySceneNode(scenenode.Name);
				//				scenenode.DetachAllObjects();
				//				scenenode.Parent.RemoveChild(scenenode);
				scenenode = null;
			}

			// destroy the particle system
			if (magicWandParticleSystem != null) 
			{
			
				ParticleSystemManager.Instance.ParticleSystems.Clear();
				magicWandParticleSystem = null;
			}

			// Destroy entity
			if (entity != null) 
			{
				sceneManager.RemoveEntity(entity);    
				entity = null;
			}
		}
		
		//SetAction only called during constructor of Hand and Board.ApplyMove
		public void SetAction(HandState.ActionTypes action)
		{
			SetAction(action,-1, -1, -1, -1,"");
		}
		public void SetAction(HandState.ActionTypes action, int Source, int Destination)
		{
			SetAction(action,Source, Destination, -1, -1,"");
		}
		public void SetAction(HandState.ActionTypes action, int Source, int Destination,int CastlingSource, int CastlingDestination)
		{
			SetAction(action,Source, Destination, CastlingSource, CastlingDestination,"");
		}
		public void SetAction(HandState.ActionTypes action, int Source, int Destination, int CastlingSource, int CastlingDestination, string PromoType)
		{
			// create a temporary state - this could be more elegant using casting, but I couldn't get it to work - 
			// I think I know how to do it now and will pursue it further next time
			if (handState != null)
			{
				handState.Delete();
			}
//			handState = new HandState(this, action, Source, Destination, CastlingSource, CastlingDestination, PromoType);    

			// set the starting state for this action
			switch (action)
			{
				case HandState.ActionTypes.Waiting:      
					SetState(HandState.StateTypes.Static,action, Source, Destination, CastlingSource, CastlingDestination, PromoType);      
					break;
				case HandState.ActionTypes.Move:            
				case HandState.ActionTypes.Capture:      
				case HandState.ActionTypes.Castle:
				case HandState.ActionTypes.EnPassant:      
				case HandState.ActionTypes.Promotion:    
					SetState(HandState.StateTypes.Pickup, action, Source, Destination, CastlingSource, CastlingDestination, PromoType);      
					break;        
			}    
		}
		#endregion

		#region Private Methods
		private void SetState(HandState.StateTypes state)
		{

			SetState(state, handState.ActionType, handState.SourceSquare, handState.DestinationSquare,handState.CastlingSource, handState.CastlingDestination, handState.PromoType);      
		}
		private void SetState(HandState.StateTypes state, HandState.ActionTypes action, int Source, int Destination, int CastlingSource, int CastlingDestination, string PromoType)
		{
			SceneNode mpn = null;
			SceneNode cpn = null;
			Piece mp = null;
			Piece cp = null;
			if (handState != null)
			{
				mpn = handState.MovingPieceNode;
				cpn = handState.CapturedPieceNode;
				//mp = handState.MovingPiece;
				//cp = handState.CapturedPiece;
			}
			// call the initialise routine for this state
			handState = null;
			switch (state)
			{    
				case HandState.StateTypes.Static:
					handState = new StaticState(this, action, Source, Destination, CastlingSource, CastlingDestination, PromoType);  
					break;
				case HandState.StateTypes.Pickup:    
					handState = new PickupState(this, action, Source, Destination, CastlingSource, CastlingDestination, PromoType);
					break;
				case HandState.StateTypes.MovePiece:      
					handState = new MovePieceState(this, action, Source, Destination, CastlingSource, CastlingDestination, PromoType);
					break;       
				case HandState.StateTypes.Take:

					handState = new CaptureState(this, action, Source, Destination, CastlingSource, CastlingDestination, PromoType);
					break;    
				case HandState.StateTypes.Remove:
					handState = new RemoveState(this, action, Source, Destination, CastlingSource, CastlingDestination, PromoType);
					break;   
				case HandState.StateTypes.SimpleReturn:
				case HandState.StateTypes.CaptureReturn:
					handState = new ReturnState(this, action, Source, Destination, CastlingSource, CastlingDestination, PromoType);
					break;
				case HandState.StateTypes.NewPiece:
					handState = new PromotionState(this, action, Source, Destination, CastlingSource, CastlingDestination, PromoType);

					break;
			} 
			handState.Init(mpn, cpn, mp, cp);
			handState.Initialise(); 
		}


		#endregion

	}
}
