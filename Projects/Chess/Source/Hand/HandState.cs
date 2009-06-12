using System;

using Axiom.Math;
using Axiom.Core;
using Axiom.Animating;

using Chess.Main;

namespace Chess
{
	/// <summary>
	/// Summary description for HandState.
	/// </summary>
	public class HandState
	{
		#region enums
		public enum ActionTypes
		{
			Waiting,
			Move,
			Capture,
			EnPassant,
			Castle,
			Promotion,
			Win
		};
		public enum StateTypes
		{
			Static,
			Pickup,
			MovePiece,
			SimpleReturn,
			Take,
			Remove,
			CaptureReturn,
			Tap1,
			Tap2,
			Tap3,
			ThumbUp,
			NewPiece
		};
		#endregion

		#region Fields
		// data members
		protected Hand hand;
		protected float stateDuration;

		// holds the 00-63 positions of the current move
		protected int sourceSquare;
		protected int destinationSquare;
		// en passant stuff
		protected bool isEnPassantFirst;
		// used for promotion
		protected string promoType;
		// used to tweak track positioning
		protected Vector3 offsetVector;
		// holds the target position (in world coordinates) at the end of this current state's life
		protected Vector3 targetPositionVector;
		// sets where the hand goes between moves
		protected Vector3 startingPositionVector;
		// this is used to help calculate the animation tracks properly
		protected Vector3 movementVector;
		// holds which action the hand should be performing
		protected ActionTypes actionType;
		// holds which current state within the action the hand is at
		protected StateTypes stateType;
		// nabbed this to allow for smooth animation blending
		protected AnimationBlender animationBlender;
		// we need to hold a pointer to the entities that will be attached to the hand
		// and also their original scene node so we can move it into position and re-attach when required.
		protected Piece movingPiece;
		protected Piece capturedPiece;
		protected SceneNode movingPieceSceneNode;
		protected SceneNode capturedPieceSceneNode;

		// some other pointers for quickness-sake
		protected SceneManager sceneManager;
		protected SceneNode handSceneNode;
		protected Entity mHandEntity;

		// holds pointers to the current movement track - lazy
		protected AnimationState moveAnimationState;
		// used for castling
		protected int castlingSource;
		protected int castlingDestination;
		protected bool castleFirst;
		#endregion

		#region Constructors

		public HandState( Hand hand, ActionTypes action, int SourceSquare, int DestinationSquare, int CastlingSource, int CastlingDestination, string PromoType )
		{
			this.hand = hand;
			this.animationBlender = null;
			this.moveAnimationState = null;
			this.targetPositionVector = Vector3.Zero;
			this.startingPositionVector = new Vector3( 140f, -7.5f, 180f );
			this.movementVector = Vector3.Zero;
			this.actionType = action;
			this.stateType = StateTypes.Static;
			this.sourceSquare = SourceSquare;
			this.destinationSquare = DestinationSquare;
			this.stateDuration = 0f;
			this.movingPieceSceneNode = null;
			this.capturedPieceSceneNode = null;
			this.movingPiece = null;
			this.capturedPiece = null;
			this.offsetVector = Vector3.Zero;
			this.castleFirst = false;
			this.castlingSource = CastlingSource;
			this.castlingDestination = CastlingDestination;
			this.promoType = PromoType;
			this.isEnPassantFirst = false;

			/*
			Start 	End 	Secs 	Name
			0	25	1	Pickup
			25	35	0.5	Move
			35	60	1	SimpleReturn
			85	100	1	Capture1
			100	115	1	Capture2
			115	140	1	Remove
			140	170	1	CaptureReturn
			170	187	1	Tap1
			187	214	1	Tap2
			213	250	1	Tap3
			250	340	3	Thumb Up  <--includes movement
			340	480	4	Promotion    neither used
			*/
			Entity entity = hand.GetEntityPtr();

			//			AnimationStateCollection animationStateSet = entity.GetAllAnimationStates(); 
			//			AnimationState animState = new AnimationState();
			//			animState.Name = "Tap2";
			//			animationStateSet.Add(animState);

			// create a new animation blender and set the initial animation
			animationBlender = new AnimationBlender( entity );
			animationBlender.Init( "Tap2" );

			// get some other handy pointers
			sceneManager = hand.GetSceneManagerPtr();
			handSceneNode = hand.GetSceneNodePtr();
			mHandEntity = hand.GetEntityPtr();
			movingPiece = Piece.GetPiece( sourceSquare );

			if ( actionType == ActionTypes.Capture )
				capturedPiece = Piece.GetPiece( destinationSquare );
		}

		public virtual void Delete()
		{
			// get rid of the movement track
			if ( moveAnimationState != null )
			{
				sceneManager.DestroyAnimation( "HandTrack" + hand.GetColour().ToString() );
				moveAnimationState = null;
			}
			// and the animation blender
			animationBlender.Delete();
		}
		#endregion

		#region Properties
		public int CastlingDestination
		{
			get
			{
				return this.castlingDestination;
			}
		}
		public int SourceSquare
		{
			get
			{
				return sourceSquare;
			}
		}
		public int DestinationSquare
		{
			get
			{
				return destinationSquare;
			}
		}
		public int CastlingSource
		{
			get
			{
				return castlingSource;
			}
		}
		public string PromoType
		{
			get
			{
				return promoType;
			}
		}
		public SceneNode MovingPieceNode
		{
			get
			{
				return movingPieceSceneNode;
			}
			set
			{
				movingPieceSceneNode = value;
			}
		}
		public SceneNode CapturedPieceNode
		{
			get
			{
				return capturedPieceSceneNode;
			}
			set
			{
				capturedPieceSceneNode = value;
			}
		}
		public Piece MovingPiece
		{
			get
			{
				return movingPiece;
			}
			set
			{
				movingPiece = value;
			}
		}
		public Piece CapturedPiece
		{
			get
			{
				return capturedPiece;
			}
			set
			{
				capturedPiece = value;
			}
		}
		public StateTypes StateType
		{
			get
			{
				return stateType;
			}
		}
		public ActionTypes ActionType
		{
			get
			{
				return actionType;
			}
		}

		#endregion

		#region Methods
		public void Init( SceneNode mpn, SceneNode cpn, Piece mp, Piece cp )
		{
			if ( mpn != null )
				this.movingPieceSceneNode = mpn;

			if ( cpn != null )
				this.capturedPieceSceneNode = cpn;

			if ( mp != null )
				this.movingPiece = mp;

			if ( cp != null )
				this.capturedPiece = cp;
		}
		public virtual void Initialise()
		{

		}
		public virtual StateTypes Update( float dt )
		{
			// update the movement track if it is enabled
			if ( moveAnimationState != null )
			{
				if ( moveAnimationState.IsEnabled )
					moveAnimationState.Time += dt;
				//moveAnimationState.AddTime(dt);
			}

			// update the animation
			if ( animationBlender != null )
				animationBlender.AddTime( dt );

			return StateTypes.Static;
		}
		protected void GetNextRemoveSlot( float x, float z )
		{
			Random random = new Random();
			float rand;
			rand = random.Next( 0, 8 );
			rand = 1.0f;

			// i'm shat at maths - i'm sure there's a *much* better way to do this...  
			int PiecesTaken = hand.GetNextTakeSlot();
			z = -70 + ( 20 * ( (int)( PiecesTaken / 3 ) ) ) + ( rand - 4 ); // the random bit adds a bit of uneven-ness
			while ( PiecesTaken > 2 )
				PiecesTaken -= 3;
			x = 110 + ( 20 * PiecesTaken ) + ( rand - 4 );
		}
		protected virtual void CreateMovementTrack()
		{
			// make sure the scene node's rotation is taken into account
			Matrix3 nodeRot = handSceneNode.DerivedOrientation.ToRotationMatrix();

			targetPositionVector = ( startingPositionVector * nodeRot );

			// store the movement vector (in world coordinates) 
			// of the target position from the entities current position
			movementVector = targetPositionVector - handSceneNode.Position;

			// create the path to be followed   
			Animation anim = sceneManager.CreateAnimation( "HandTrack" + hand.GetColour().ToString(), stateDuration );

			// Spline it for nice curves
			anim.InterpolationMode = InterpolationMode.Spline;

			// Create a track to animate the hand
			AnimationTrack track = anim.CreateNodeTrack( 0, handSceneNode );

			// Setup keyframes
			TransformKeyFrame key = (TransformKeyFrame)track.CreateKeyFrame( 0 ); // startposition
			key.Translate = handSceneNode.Position;
			key.Rotation = handSceneNode.Orientation;

			key = (TransformKeyFrame)track.CreateKeyFrame( stateDuration * 0.5f );
			key.Translate = new Vector3( targetPositionVector.x - ( movementVector.x * 0.5f ), targetPositionVector.y + 65f, targetPositionVector.z - ( movementVector.z * 0.5f ) );
			key.Rotation = ( handSceneNode.Orientation );

			key = (TransformKeyFrame)track.CreateKeyFrame( stateDuration );
			key.Translate = ( targetPositionVector );
			key.Rotation = ( handSceneNode.Orientation );
		}
		protected virtual void StartAnimation()
		{
			// Create a new animation state to track this
			string animationName = "HandTrack" + hand.GetColour().ToString();


			moveAnimationState = sceneManager.CreateAnimationState( animationName );
			moveAnimationState.Loop = false;
			moveAnimationState.Time = 0;
			moveAnimationState.IsEnabled = ( true );
			//			moveAnimationState.setLoop(false);
		}
		protected virtual void AttachPiece( string bonename, Piece piece, SceneNode pieceNode )
		{
			// record the previous scene node so it can be re-attached later
			pieceNode = piece.GetSceneNode();

			// remove the piece from the original node  
			pieceNode.DetachObject( piece.GetEntity() );
			mHandEntity.DetachObjectFromBone( piece.GetEntity() );

			// attach the piece to the index finger bone      
			Bone bone = mHandEntity.Skeleton.GetBone( bonename );

			// work out the offset orientation
			Quaternion offsetRotation = ( bone.DerivedOrientation.Inverse() * pieceNode.Orientation );

			// work out the offset position
			Matrix3 boneRot;
			Matrix3 nodeRot;

			Vector3 nodePos = pieceNode.Position;
			Vector3 handPos = handSceneNode.Position;
			Vector3 bonePos = bone.DerivedPosition;

			boneRot = bone.DerivedOrientation.ToRotationMatrix();
			nodeRot = handSceneNode.Orientation.ToRotationMatrix();

			Vector3 offsetPosition = ( nodePos - ( handPos + ( nodeRot * bonePos ) ) );
			offsetPosition = offsetPosition * ( nodeRot * boneRot );

			// finally, attach the object to the bone  
			mHandEntity.AttachObjectToBone( bonename, piece.GetEntity(), offsetRotation, offsetPosition );
		}

		protected virtual void DetachPiece( Piece piece, SceneNode pieceNode )
		{
			// remove the piece from the bone
			mHandEntity.DetachObjectFromBone( piece.GetEntity() );



			// re-attach the piece to it's original scene node  
			if ( pieceNode != null )
				pieceNode.AttachObject( piece.GetEntity() );
		}

		#endregion


	}
}
