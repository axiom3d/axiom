using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using XFG = Microsoft.Xna.Framework;
using XInput = Microsoft.Xna.Framework.Input;

namespace Axiom.Demos.Browser.Xna
{
    class XBoxInput : XnaInput
    {
        private XInput.GamePadState _gpState;
        private XInput.KeyboardState _keyboardState;
        private XInput.MouseState _previousMouseState = XInput.Mouse.GetState();
        private XInput.MouseState _mouseState;

        public override void CaptureMouseState()
        {
            _previousMouseState = _mouseState;
            _mouseState = XInput.Mouse.GetState();

            relativeMousePosition.X = _previousMouseState.X - _mouseState.X;
            relativeMousePosition.Y = _previousMouseState.Y - _mouseState.Y;
        }

        public override void CaptureKeyboardState()
        {
            _keyboardState = XInput.Keyboard.GetState( XFG.PlayerIndex.One );
        }

        public override void CheckKeyPressed( Axiom.Input.KeyCodes key, out bool isPressed )
        {
            isPressed = false;
            XFG.Input.Keys xnaKey = Convert( key );
            isPressed = _keyboardState.IsKeyDown( xnaKey );
        }

        private Microsoft.Xna.Framework.Input.Keys Convert( Axiom.Input.KeyCodes key )
        {
            switch ( key )
            {
                case Axiom.Input.KeyCodes.Escape:
                    return Microsoft.Xna.Framework.Input.Keys.Escape;
                case Axiom.Input.KeyCodes.G:
                    return Microsoft.Xna.Framework.Input.Keys.G;
                case Axiom.Input.KeyCodes.A:
                    return Microsoft.Xna.Framework.Input.Keys.A;
                case Axiom.Input.KeyCodes.S:
                    return Microsoft.Xna.Framework.Input.Keys.S;
                case Axiom.Input.KeyCodes.D:
                    return Microsoft.Xna.Framework.Input.Keys.D;
                case Axiom.Input.KeyCodes.W:
                    return Microsoft.Xna.Framework.Input.Keys.W;
                    
                default:
                    return Microsoft.Xna.Framework.Input.Keys.Execute;
            }
        }

        public override void CheckMouseButtonPressed( Axiom.Input.MouseButtons button, out bool isPressed )
        {
            isPressed = false;

            if ( button == Axiom.Input.MouseButtons.Left && _mouseState.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed )
                isPressed = true;

            if ( button == Axiom.Input.MouseButtons.Right && _mouseState.RightButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed )
                isPressed = true;
        }

        public override bool UseMouseEvents
        {
            get
            {
                return false;
            }
            set
            {
                throw new NotImplementedException();
            }
        }
    }
}