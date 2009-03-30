using System;

using Axiom;
using Axiom.Input;

namespace YAT
{
    public abstract class State
    {
        #region Fields
        protected InputReader input;
        #endregion

        #region Constructors
        public State()
        {
            input = TetrisApplication.Instance.Input;
        }

        #endregion


        #region Virtual Methods
        public virtual void Initialize()
        {

        }
        public virtual void Cleanup()
        {

        }

        public virtual void Pause()
        {

        }
        public virtual void Resume()
        {

        }

        public virtual void FrameStarted( float dt )
        {

        }
        public virtual void FrameEnded( float dt )
        {

        }
        #endregion

        static public void ChangeState( State state )
        {
            StateManager.Instance.ChangeState( state );
        }

        #region Keyboard Input Methods
        public virtual void KeyClicked( KeyEventArgs e )
        {

        }
        public virtual void KeyPressed( KeyEventArgs e )
        {

        }
        public virtual void KeyReleased( KeyEventArgs e )
        {

        }
        public virtual void KeyRepeated( Axiom.Input.KeyCodes kc )
        {

        }
        #endregion


        protected bool IsKeyDown( Axiom.Input.KeyCodes kc )
        {
            return StateManager.Instance.Input.IsKeyPressed( kc );
        }

        #region Mouse Input Methods
        public virtual void MouseMoved( MouseEventArgs e )
        {

        }
        public virtual void MouseDragged( MouseEventArgs e )
        {

        }
        public virtual void HandleInput()
        {

        }

        #endregion

    }
}