using System;
using System.Collections;

using Axiom;
using Axiom.MathLib;

namespace YAT 
{
	public class Game
	{

		#region Enums
		public enum UpdateFlags 
		{
			UpdateBricks = 0x1,
			UpdateStatistics = 0x2,
			UpdateNextPieceBricks = 0x4
		};
		#endregion

		#region Fields
		public int mPiece;
		public int mPieceRotation;
		public int mPieceX;
		public int mPieceY;
		public int mNextPiece;
		public int mNextPieceRotation;
		public int mPoints;
		public int mLines;
		public int mLevel;
		public float dropDelay;
		public Axiom.MathLib.Vector3 mCameraTarget;
		public float cameraWantedYaw;
		public float mCameraWantedPitch;
		public float mCameraWantedDistance;
		public float mCameraCorrection;
		public Highscores mHighscores;

	
		protected ArrayList states = new ArrayList();
		protected int mLevelWidth;
		protected int mLevelHeight;
		protected byte[] mLevelData;
		protected Camera camera;
		protected SceneNode levelRoot;
		protected SceneNode nextPieceRoot;
		protected Brick[] brickList;
		protected Brick[] nextPieceBricks;
		protected byte mUpdateFlags;

		#endregion

		#region Singleton implementation

		private static Game instance;
		public Game(Camera cam, SceneNode root, SceneNode nextPieceRoot)
		{
			if (instance == null) 
			{
				instance = this;
				this.camera = cam;
				this.levelRoot = root;
				this.nextPieceRoot = nextPieceRoot;
				// Initialize game
				CreateNextPieceBricks();
				ResizeLevel(10, 25);
				ResetGame();

				// Initialize camera
				mCameraTarget = new Vector3(0.0f, 12.5f, 0.0f);
				cameraWantedYaw = 0.0f;
				mCameraWantedPitch = -0.25f;
				mCameraWantedDistance = 40.0f;
				mCameraCorrection = 15.0f;

					
				camera.Orientation = (Quaternion.FromAngleAxis(Axiom.MathLib.MathUtil.DegreesToRadians(cameraWantedYaw), Vector3.UnitY) 
					* Quaternion.FromAngleAxis(Axiom.MathLib.MathUtil.DegreesToRadians(mCameraWantedPitch), Vector3.UnitX));
				camera.Position = (mCameraTarget - (mCameraWantedDistance*camera.Direction));

				// Create highscores object
				mHighscores = new Highscores(10, "highscores.cfg");

				// Create game states
				states.Add(new NewGameState());
				states.Add(new DropPieceState());
				states.Add(new RemoveLinesState());

				// Create menu states
				states.Add(new MainMenuState());
				states.Add(new HelpState());
				states.Add(new HighscoresState());
				states.Add(new GameMenuState());
				states.Add(new GameOverState());
				states.Add(new NewHighscoreState());

				// Set up state stack
				StateManager.Instance.AddState(NewGameState.Instance);
				StateManager.Instance.AddState(MainMenuState.Instance);

				// Reset next piece to hide it before first game
				mNextPiece = -1;
			}
		}
		public static Game Instance 
		{
			get 
			{
				return instance;
			}
		}
		#endregion

		#region Public Methods
		public void ResizeLevel(int width, int height)
		{
			// Delete old level data
			mLevelData = null;
			brickList = null;

			// Store new dimensions
			mLevelWidth = width;
			mLevelHeight = height;

			// Allocate new level data
			mLevelData = new byte[mLevelWidth*mLevelHeight];

			// Create new bricks
			brickList = new Brick[mLevelWidth*mLevelHeight];
			for (int i = 0; i < mLevelHeight; ++i)
			{
				for (int j = 0; j < mLevelWidth; ++j)
				{
					brickList[i*mLevelWidth + j] =  new Brick();
					brickList[i*mLevelWidth + j].Create(
						"Brick" + i.ToString() + ":" + j.ToString(),
						levelRoot, new Vector3((float)(j - 0.5f*(float)mLevelWidth + 0.5f), (float)(i + 0.5f), 0.0f));
				}
			}
		}
		public void ResetGame()
		{


			int size = mLevelWidth*mLevelHeight;

			// Clear level data
			for (int i = 0; i < size; ++i)
				mLevelData[i] = byte.MinValue;

			// Initialise game data
			mPiece = -1;
			mNextPiece = -1;
			mPoints = 0;
			mLines = 0;
			mLevel = 1;
			// dropDelay is calculated in DropPieceState::Initialize() according to current level

			// Force Update
			Invalidate((byte)Game.UpdateFlags.UpdateBricks | (byte)Game.UpdateFlags.UpdateStatistics | (byte)Game.UpdateFlags.UpdateNextPieceBricks);
		}

		public int getLevelWidth()
		{
			return mLevelWidth;
		}
		public int getLevelHeight()
		{
			return mLevelHeight;
		}
	
		// Called by DropPieceState
		public bool SpawnPiece()
		{
			Random rand = new Random();
			if (mNextPiece == -1)
			{
				mNextPiece =  rand.Next() % Globals.NUM_PIECES;
				mNextPieceRotation = rand.Next() % 4;
			}

			// Use stored values for next piece
			mPiece = mNextPiece;
			mPieceRotation = mNextPieceRotation;
			mPieceX = mLevelWidth/2 - 2;
			mPieceY = mLevelHeight + 3;

			// Find bottom of piece and drop it just above the level
			for (int i = 0; i < 4; ++i)
			{
				bool rowEmpty = true;

				for (int j = 0; j < 4; ++j)
				{
					if (Globals.Piece[mPiece,mPieceRotation,3 - i,j] != byte.MinValue)
					{
						rowEmpty = false;
						break;
					}
				}

				if (rowEmpty)
					--mPieceY;
				else
					break;
			}

			// Choose new random piece and rotation
			mNextPiece = rand.Next() % Globals.NUM_PIECES;
			mNextPieceRotation = rand.Next() % 4;

			Invalidate((byte)Game.UpdateFlags.UpdateNextPieceBricks);

			// Return false on game over
			return !CollidePiece(mPiece, mPieceRotation, mPieceX, mPieceY - 1);
		}
		public bool CollidePiece(int piece, int rotation, int x, int y)
		{

			bool collision = false;

			for (int i = 0; i < 4; ++i)
				for (int j = 0; j < 4; ++j)
				{
					if (Globals.Piece[piece,rotation,i,j] != byte.MinValue)
					{
						int _x = x + j;
						int _y = y - i;

						if ((_x >= 0) && (_x < mLevelWidth) && (_y >= 0))
						{
							if (_y < mLevelHeight)
							{
								if (mLevelData[_y*mLevelWidth + _x] != byte.MinValue)
									collision = true;
							}
						}
						else
						{
							collision = true;
						}
					}
				}

			return collision;
		}
		public void MergePiece()
		{
			// Merge the current piece into level data
			for (int i = 0; i < 4; ++i)
				for (int j = 0; j < 4; ++j)
				{
					if (Globals.Piece[mPiece,mPieceRotation,i,j] != byte.MinValue)
					{
						int x = mPieceX + j;
						int y = mPieceY - i;

						if ((x >= 0) && (x < mLevelWidth) && (y >= 0) && (y < mLevelHeight))
						{
							if (y == (mLevelHeight - 1))
							{
								// It's necessary to clear the top edge bit to avoid holes in the geometry
								// at the top row of the level.
								mLevelData[y*mLevelWidth + x] = (byte)(Globals.Piece[mPiece,mPieceRotation,i,j]
									& (~(byte)Brick.DataMasks.TopEdge | (byte)Brick.DataMasks.ColorMask));
							}
							else
							{
								mLevelData[y*mLevelWidth + x] = Globals.Piece[mPiece,mPieceRotation,i,j];
							}
						}
					}
				}

			mPiece = -1;
			Invalidate((byte)Game.UpdateFlags.UpdateBricks);
		}
	
		// Called by RemoveLinesState
		public bool lineFull(int line)
		{
			bool full = true;

			// Check if line is filled
			for (int i = 0; i < mLevelWidth; ++i)
			{
				if (mLevelData[line*mLevelWidth + i]==byte.MinValue)
				{
					full = false;
					break;
				}
			}

			return full;
		}
		public void SplitLine(int line)
		{
			if ((line <= 0) || (line >= mLevelHeight))
				return;

			// Split at the bottom of the specified line any merged pieces by clearing
			// the top/bottom bits of the sorrounding bricks.
			for (int i = 0; i < mLevelWidth; ++i)
			{
				if ( (mLevelData[line*mLevelWidth + i] & (byte)Brick.DataMasks.BottomEdge)!=byte.MinValue)
				{
					// Clear bottom edge bit of top brick, and top edge bit of bottom brick
					mLevelData[line*mLevelWidth + i] = (byte)(mLevelData[line*mLevelWidth + i] & (~(byte)Brick.DataMasks.BottomEdge));
					mLevelData[(line - 1)*mLevelWidth + i] = (byte)(mLevelData[(line - 1)*mLevelWidth + i] & (~(byte)Brick.DataMasks.TopEdge));
				}
			}

			Invalidate((byte)Game.UpdateFlags.UpdateBricks);
		}
		public void highlightLine(int line, bool highlight)
		{
			for (int i = 0; i < mLevelWidth; ++i)
			{
				if (mLevelData[line*mLevelWidth + i] != byte.MinValue)
				{
					// Set highligh bit of bricks in the line
					mLevelData[line*mLevelWidth + i] = (byte)(
						(mLevelData[line*mLevelWidth + i] & (~(byte)Brick.DataMasks.HighlightBit)) |
						(highlight ? (byte)Brick.DataMasks.HighlightBit : byte.MinValue));
				}
			}

			Invalidate((byte)Game.UpdateFlags.UpdateBricks);
		}
		public void ClearLine(int line)
		{
			for (int i = 0; i < mLevelWidth; ++i)
			{
				// Reset brick at this position
				mLevelData[line*mLevelWidth + i] = byte.MinValue;
			}

			Invalidate((byte)Game.UpdateFlags.UpdateBricks);
		}
		public void RemoveLine(int line)
		{
			// Move all lines above 'line' down
			for (int i = line; i < (mLevelHeight - 1); ++i)
			{
				for (int j = 0; j < mLevelWidth; ++j)
				{
					// Copy data from the line above
					mLevelData[i*mLevelWidth + j] = mLevelData[(i + 1)*mLevelWidth + j];
				}
			}

			// Clear the top row
			ClearLine(mLevelHeight - 1);

			Invalidate((byte)Game.UpdateFlags.UpdateBricks);
		}

		// Update functions
		public void Invalidate(byte updateFlags)
		{
			mUpdateFlags |= updateFlags;
		}
		public void Update(float dt)
		{
			if ( (mUpdateFlags & (byte)UpdateFlags.UpdateBricks) != byte.MinValue)
			{
				int size = mLevelWidth*mLevelHeight;

				// Copy level data to bricks
				for (int i = 0; i < size; ++i)
					brickList[i].SetData(mLevelData[i]);

				// Display current piece
				if (mPiece >= 0)
				{
					for (int i = 0; i < 4; ++i)
						for (int j = 0; j < 4; ++j)
						{
							//if (Globals.Piece[mPiece][mPieceRotation][i][j] != byte.MinValue)
							if (Globals.Piece[mPiece,mPieceRotation,i,j] != byte.MinValue)
							{
								int x = mPieceX + j;
								int y = mPieceY - i;

								if ((x >= 0) && (x < mLevelWidth) && (y >= 0) && (y < mLevelHeight))
								{
									if (y == (mLevelHeight - 1))
									{
										// It's necessary to clear the top edge bit to avoid holes in the geometry
										// at the top row of the level.
										brickList[y*mLevelWidth + x].SetData((byte)(Globals.Piece[mPiece,mPieceRotation,i,j]
											& (~ (byte)Brick.DataMasks.TopEdge | (byte)Brick.DataMasks.ColorMask)));
									}
									else
									{
										brickList[y*mLevelWidth + x].SetData(Globals.Piece[mPiece,mPieceRotation,i,j]);
									}
								}
							}
						}
				}

				// Update bricks
				for (int i = 0; i < size; ++i)
					brickList[i].Update();
			}

			if ((mUpdateFlags & (byte)Game.UpdateFlags.UpdateStatistics) != byte.MinValue)
			{
				// Update statistics
				OverlayElementManager.Instance.GetElement("Statistics/Points").Text = "Points: " + mPoints.ToString();
				OverlayElementManager.Instance.GetElement("Statistics/Lines").Text ="Lines: " + mLines.ToString();
				OverlayElementManager.Instance.GetElement("Statistics/Level").Text = "Level: " + mLevel.ToString();
			}

			if ((mUpdateFlags & (byte)Game.UpdateFlags.UpdateNextPieceBricks) != byte.MinValue)
			{
				// Update next piece bricks
				for (int i = 0; i < 4; ++i)
				{
					for (int j = 0; j < 4; ++j)
					{
						if (mNextPiece != -1)
							nextPieceBricks[i*4 + j].SetData(Globals.Piece[mNextPiece,mNextPieceRotation,i,j]);
						else
							nextPieceBricks[i*4 + j].SetData(0);

						nextPieceBricks[i*4 + j].Update();
					}
				}
			}

			// Update camera position
			{
				float t = dt * mCameraCorrection;
				Quaternion wantedOrientation;
				float currentDistance;
				float distance;

				// Clamp to one, so camera doesn't move too far
				if (t > 1.0f)
					t = 1.0f;

				// Create quaternion describing wanted orientation
				wantedOrientation = Quaternion.FromAngleAxis(Axiom.MathLib.MathUtil.DegreesToRadians(cameraWantedYaw), Vector3.UnitY) 
					* Quaternion.FromAngleAxis(Axiom.MathLib.MathUtil.DegreesToRadians(mCameraWantedPitch), Vector3.UnitX);


				Vector3 v = (camera.Position - mCameraTarget);
				currentDistance = v.Length;
				distance = currentDistance + t*(mCameraWantedDistance - currentDistance);

				// Move camera towards wanted position
				camera.Orientation = (Quaternion.Slerp(t, camera.Orientation, wantedOrientation, true));
				camera.Position = (mCameraTarget - distance*camera.Direction);
			}

			// Reset Update flags
			mUpdateFlags = byte.MinValue;
		}


		public void CreateNextPieceBricks()
		{
			// Create next piece bricks
			nextPieceBricks = new Brick[16];
			//			Brick brick;
			for (int i = 0; i < 4; ++i)
			{
				for (int j = 0; j < 4; ++j)
				{
					//					brick = new Brick();
					nextPieceBricks[i*4 + j] = new Brick();
					nextPieceBricks[i*4 + j].Create(
						"NextPieceBrick" + i.ToString() + ":" + j.ToString(),
						nextPieceRoot, new Vector3((float)j - 1.5f, 1.5f - (float)i, -6.0f));
				}
			}
		}

		#endregion


	}
}