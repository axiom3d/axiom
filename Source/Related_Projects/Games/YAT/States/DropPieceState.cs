using System;

using Axiom;



namespace YAT 
{

	public class DropPieceState: GameState
	{
		#region Fields
		protected float dropDelay;
		#endregion

		#region Singleton implementation

		private static DropPieceState instance;
		public DropPieceState()
		{
			if (instance == null) 
			{
				instance = this;
				input = TetrisApplication.Instance.Input;
			}
		}
		public static DropPieceState Instance 
		{
			get 
			{
				return instance;
			}
		}
		#endregion

		#region Public Methods
		// State overrides
		public override void Initialize()
		{
			// Initialize game state
			base.Initialize();

			// Update Game::dropDelay according to current level
			if (game.mLevel < 15)
			{
				game.dropDelay = 0.6f * (float)System.Math.Pow(0.85, game.mLevel);
			}
			else
			{
				// No level will get faster than this (level 15)
				game.dropDelay = 0.6f * (float)System.Math.Pow(0.85, 15);
			}

			// Set variables
			dropDelay = game.dropDelay;

			// Spawn new piece
			if (!game.SpawnPiece())
			{
				// Game over!
				if (game.mHighscores.GetPlace(game.mPoints) >= 0)
				{
					// New highscore!
					StateManager.Instance.AddState(NewHighscoreState.Instance);
				}
				else
				{
					// No highscore...
					StateManager.Instance.AddState(GameOverState.Instance);
				}
			}
		}
		public override void Cleanup()
		{
			// Merge piece into level data
			game.MergePiece();
		}
		public override void KeyPressed(Axiom.KeyEventArgs e)
		{
			switch (e.Key)
			{
			case Axiom.Input.KeyCodes.Up:
			case Axiom.Input.KeyCodes.Space:
				if (!game.CollidePiece(game.mPiece, (game.mPieceRotation + 1) % 4, game.mPieceX, game.mPieceY))
				{
					game.mPieceRotation = (game.mPieceRotation + 1) % 4;
					game.Invalidate((int)Game.UpdateFlags.UpdateBricks);
				}
				break;

			// The following section has been commented out to use "slow" dropping of pieces instead.
			// See DropPieceState::FrameStarted()...

			//case KC_DOWN:
			//	// Drop the piece all the way
			//	while (!game.CollidePiece(game.mPiece, game.mPieceRotation, game.mPieceX, game.mPieceY - 1))
			//		--game.mPieceY;

			//	// Force game to continue immediately
			//	dropDelay = 0.0;

			//	game.Invalidate(Game::UpdateBricks | Game::UpdateStatistics);
			//	break;

			default:
				// Call inherited function to handle default keys
				base.KeyPressed(e);
				break;
			}
		}
		public override void KeyRepeated(Axiom.Input.KeyCodes kc)
		{
			switch (kc)
			{
			case Axiom.Input.KeyCodes.Left:
				if (!game.CollidePiece(game.mPiece, game.mPieceRotation, game.mPieceX - 1, game.mPieceY))
				{
					game.mPieceX -= 1;
					game.Invalidate((int)Game.UpdateFlags.UpdateBricks);
				}
				break;

			case Axiom.Input.KeyCodes.Right:
				if (!game.CollidePiece(game.mPiece, game.mPieceRotation, game.mPieceX + 1, game.mPieceY))
				{
					game.mPieceX += 1;
					game.Invalidate((int)Game.UpdateFlags.UpdateBricks);
				}
				break;
			}
		}
		public override void FrameStarted(float dt)
		{
			float speedFactor;

			// Determine drop speed factor based on whether KC_DOWN is pressed.
			// This is done instead of the pieces dropping all the way down immediately.
			if (IsKeyDown(Axiom.Input.KeyCodes.Down))
				speedFactor = 10.0f;
			else
				speedFactor = 1.0f;

			// Decrese time left before next drop.
			dropDelay -= speedFactor*dt;

			// Drop piece
			if (dropDelay <= 0.0f)
			{
				if (game.CollidePiece(game.mPiece, game.mPieceRotation, game.mPieceX, game.mPieceY - 1))
				{
					// Collision!
					ChangeState(RemoveLinesState.Instance);
				}
				else
				{
					// Drop piece one step
					--game.mPieceY;
					dropDelay = game.dropDelay;
					game.Invalidate((int)Game.UpdateFlags.UpdateBricks);
				}
			}

			// Call inherited function to Update the game
			base.FrameStarted(dt);
		}


		public override void HandleInput()
		{

			if(spaceKey.KeyDownEvent()) 
			{
				if (!game.CollidePiece(game.mPiece, (game.mPieceRotation + 1) % 4, game.mPieceX, game.mPieceY))
				{
					game.mPieceRotation = (game.mPieceRotation + 1) % 4;
					game.Invalidate((int)Game.UpdateFlags.UpdateBricks);
				}
			}
			if(leftKey.KeyDownEvent()) 
			{
				if (!game.CollidePiece(game.mPiece, game.mPieceRotation, game.mPieceX - 1, game.mPieceY))
				{
					game.mPieceX -= 1;
					game.Invalidate((int)Game.UpdateFlags.UpdateBricks);
				}
			}
			if(rightKey.KeyDownEvent()) 
			{
				if (!game.CollidePiece(game.mPiece, game.mPieceRotation, game.mPieceX + 1, game.mPieceY))
				{
					game.mPieceX += 1;
					game.Invalidate((int)Game.UpdateFlags.UpdateBricks);
				}
			}
			base.HandleInput();
		}
		#endregion

		

		
	}



}