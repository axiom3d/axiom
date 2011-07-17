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

        public override void CaptureMouseState()
        {
            _gpState = XInput.GamePad.GetState( XFG.PlayerIndex.One );

            relativeMousePosition.X = _gpState.ThumbSticks.Left.X;
            relativeMousePosition.Y = _gpState.ThumbSticks.Left.Y;
        }

        public override void CaptureKeyboardState()
        {
            _keyboardState = XInput.Keyboard.GetState( XFG.PlayerIndex.One );
        }

        public override void CheckKeyPressed( Axiom.Input.KeyCodes key, out bool isPressed )
        {
            isPressed = false;
            if ( /* key == Axiom.Input.KeyCodes.Escape || */( _gpState.IsButtonDown( XInput.Buttons.Back ) ) )
            {
                //isPressed = true;
                return;
            }

            /* XFG.Input.Keys xnaKey = Convert( key );

				isPressed = !_keyboardState.IsKeyUp( xnaKey );*/
        }

        private Microsoft.Xna.Framework.Input.Keys Convert( Axiom.Input.KeyCodes key )
        {
            switch ( key )
            {
                case Axiom.Input.KeyCodes.G:
                    return Microsoft.Xna.Framework.Input.Keys.G;
                default:
                    return Microsoft.Xna.Framework.Input.Keys.BrowserFavorites;
            }
        }

        public override void CheckMouseButtonPressed( Axiom.Input.MouseButtons button, out bool isPressed )
        {
            isPressed = false;

            if ( button == Axiom.Input.MouseButtons.Left || _gpState.IsButtonDown( XInput.Buttons.A ) )
                isPressed = true;

            if ( button == Axiom.Input.MouseButtons.Right || _gpState.IsButtonDown( XInput.Buttons.B ) )
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