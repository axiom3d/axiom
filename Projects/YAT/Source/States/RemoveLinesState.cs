using System;

using Axiom;

namespace YAT
{
    public class YATLine
    {
        public YATLine( int i )
        {
            this.i = i;
        }
        public int i;
    }

    public class RemoveLinesState : GameState
    {
        #region Enums
        protected enum RemoveState
        {
            HighlightLines,
            RemoveLines
        };
        #endregion

        #region Fields
        System.Collections.Queue mFullLines = new System.Collections.Queue();
        protected RemoveState mRemoveState;
        protected bool mHighlighted;
        protected int mHighlightCount;
        protected float mHighlightDuration;
        protected float mRemoveDuration;
        protected float mDelay;
        protected int mPointAccumulator;
        protected int mLineAccumulator;
        #endregion

        #region Singleton implementation

        private static RemoveLinesState instance;
        public RemoveLinesState()
        {
            if ( instance == null )
            {
                instance = this;
            }
        }
        public static RemoveLinesState Instance
        {
            get
            {
                return instance;
            }
        }
        #endregion

        #region Method State overrides
        public override void Initialize()
        {
            bool lastLineFull = false;

            // Initialize game state
            base.Initialize();

            // Check for full lines
            for ( int i = 0; i < game.getLevelHeight(); ++i )
            {
                if ( game.lineFull( i ) )
                {
                    if ( !lastLineFull )
                    {
                        // Split level pieces between the lines
                        game.SplitLine( i );
                    }

                    // Store the line index and highlight the line
                    mFullLines.Enqueue( new YATLine( i ) );
                    game.highlightLine( i, true );

                    lastLineFull = true;
                }
                else
                {
                    if ( lastLineFull )
                    {
                        // Split level pieces between the lines
                        game.SplitLine( i );
                    }

                    lastLineFull = false;
                }
            }

            if ( mFullLines.Count == 0 )
            {
                // No lines to remove. Continue game...
                ChangeState( DropPieceState.Instance );
            }
            else
            {
                mRemoveState = RemoveState.HighlightLines;
                mHighlighted = true;
                mHighlightCount = 0;
                mHighlightDuration = 0.25f * game.dropDelay;
                mRemoveDuration = 0.125f * game.dropDelay;
                mDelay = mHighlightDuration;
                mPointAccumulator = 0;
                mLineAccumulator = 0;
            }
        }
        public override void Cleanup()
        {
            mFullLines.Clear();
        }

        public override void FrameStarted( float dt )
        {
            mDelay -= dt;

            if ( mDelay <= 0.0 )
            {
                switch ( mRemoveState )
                {
                    case RemoveState.HighlightLines:
                        mHighlighted = !mHighlighted;
                        if ( !mHighlighted )
                            ++mHighlightCount;

                        if ( mHighlightCount < 2 )
                        {
                            // Highlight/un-highlight full lines
                            System.Collections.IEnumerator myEnumerator = mFullLines.GetEnumerator();
                            while ( myEnumerator.MoveNext() )
                            {
                                game.highlightLine( ( (YATLine)myEnumerator.Current ).i, mHighlighted );
                            }

                            mDelay = mHighlightDuration;
                        }
                        else
                        {
                            // Clear the full lines
                            System.Collections.IEnumerator myEnumerator = mFullLines.GetEnumerator();
                            while ( myEnumerator.MoveNext() )
                            {
                                game.ClearLine( ( (YATLine)myEnumerator.Current ).i );
                            }

                            // Next state is removing the lines
                            mRemoveState = RemoveState.RemoveLines;
                            mDelay = mRemoveDuration;
                        }
                        break;

                    case RemoveState.RemoveLines:
                        if ( mFullLines.Count == 0 )
                        {
                            // No more lines to remove. Add points and lines, and Update level
                            game.mPoints += mPointAccumulator * ( 9 + game.mLevel );
                            game.mLines += mLineAccumulator;

                            game.mLevel = 1 + game.mLines / 10;
                            game.Invalidate( (int)Game.UpdateFlags.UpdateStatistics );//issue

                            // Continue game...
                            ChangeState( DropPieceState.Instance );
                        }
                        else
                        {
                            // Remove the bottommost full line
                            game.RemoveLine( ( (YATLine)mFullLines.Peek() ).i );
                            mFullLines.Dequeue();

                            // Add points and lines
                            mPointAccumulator = 1 + 2 * mPointAccumulator;
                            ++mLineAccumulator;

                            // The index of the remaining lines must be decreased by one to compensate
                            // for the removed line.


                            System.Collections.IEnumerator myEnumerator = mFullLines.GetEnumerator();
                            while ( myEnumerator.MoveNext() )
                            {
                                --( (YATLine)myEnumerator.Current ).i;
                            }

                        }

                        mDelay = mRemoveDuration;
                        break;
                }
            }

            // Call inherited function to Update the game
            base.FrameStarted( dt );
        }

        #endregion

    }
}