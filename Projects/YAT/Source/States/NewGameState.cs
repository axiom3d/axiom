using System;

using Axiom;

namespace YAT
{
    public class NewGameState : GameState
    {
        #region Singleton implementation

        private static NewGameState instance;
        public NewGameState()
        {
            if ( instance == null )
            {
                instance = this;
            }
        }
        public static NewGameState Instance
        {
            get
            {
                return instance;
            }
        }
        #endregion


        #region Methods
        public override void Initialize()
        {
            // Initialize game state
            base.Initialize();

            // Start new game
            game.ResetGame();
            ChangeState( DropPieceState.Instance );
        }

        #endregion

    }
}