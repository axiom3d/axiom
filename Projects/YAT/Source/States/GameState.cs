using System;

using Axiom;
using Axiom.Input;

namespace YAT
{

    public class GameState : TetrisState
    {

        #region Fields
        protected float savedCameraYaw;
        protected float savedCameraPitch;


        #endregion

        #region Constructors
        public GameState()
        {

        }

        #endregion

        #region Methods
        // State overrides
        public override void Pause()
        {
            // Save game camera yaw and pitch so it can be restored, when game continues
            savedCameraYaw = game.cameraWantedYaw;
            savedCameraPitch = game.mCameraWantedPitch;
        }
        public override void Resume()
        {
            // Restore saved game camera yaw and pitch
            game.cameraWantedYaw = savedCameraYaw;
            game.mCameraWantedPitch = savedCameraPitch;
        }
        public override void KeyPressed( KeyEventArgs e )
        {
            // Handle keys common to all game states
            switch ( e.Key )
            {
                case Axiom.Input.KeyCodes.Escape:
                    StateManager.Instance.AddState( GameMenuState.Instance );
                    break;

                default:
                    // Default processing
                    base.KeyPressed( e );
                    break;
            }
        }
        public override void MouseMoved( MouseEventArgs e )
        {
            float maxYaw = 0.4f * (float)System.Math.PI;
            float maxPitch = 0.45f * (float)System.Math.PI;
            float minDistance = 22.5f;

            // Adjust and clamp wanted camera yaw
            game.cameraWantedYaw -= 2.5f * e.RelativeX;
            if ( game.cameraWantedYaw > maxYaw )
            {
                game.cameraWantedYaw = maxYaw;
            }
            else if ( game.cameraWantedYaw < -maxYaw )
            {
                game.cameraWantedYaw = -maxYaw;
            }

            // Adjust and clamp wanted camera pitch
            game.mCameraWantedPitch -= 2.5f * e.RelativeY;
            if ( game.mCameraWantedPitch > maxPitch )
            {
                game.mCameraWantedPitch = maxPitch;
            }
            else if ( game.mCameraWantedPitch < -maxPitch )
            {
                game.mCameraWantedPitch = -maxPitch;
            }

            // Adjust and clamp wanted camera distance
            game.mCameraWantedDistance -= 25.0f * e.RelativeZ;
            if ( game.mCameraWantedDistance < minDistance )
                game.mCameraWantedDistance = minDistance;
        }

        public override void HandleInput()
        {
            if ( escapeKey.KeyDownEvent() )
            {
                StateManager.Instance.AddState( GameMenuState.Instance );
            }


            float maxYaw = 0.4f * (float)System.Math.PI;
            float maxPitch = 0.45f * (float)System.Math.PI;
            float minDistance = 22.5f;

            float multiplier = .05f;

            // Adjust and clamp wanted camera yaw
            game.cameraWantedYaw -= multiplier * input.RelativeMouseX;
            if ( game.cameraWantedYaw > maxYaw )
            {
                game.cameraWantedYaw = maxYaw;
            }
            else if ( game.cameraWantedYaw < -maxYaw )
            {
                game.cameraWantedYaw = -maxYaw;
            }

            // Adjust and clamp wanted camera pitch
            game.mCameraWantedPitch -= multiplier * input.RelativeMouseY;
            if ( game.mCameraWantedPitch > maxPitch )
            {
                game.mCameraWantedPitch = maxPitch;
            }
            else if ( game.mCameraWantedPitch < -maxPitch )
            {
                game.mCameraWantedPitch = -maxPitch;
            }

            // Adjust and clamp wanted camera distance
            game.mCameraWantedDistance -= 10f * multiplier * input.RelativeMouseZ;
            if ( game.mCameraWantedDistance < minDistance )
                game.mCameraWantedDistance = minDistance;
        }
        #endregion

    }
}