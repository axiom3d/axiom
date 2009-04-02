using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using XFG = Microsoft.Xna.Framework;
using XInput = Microsoft.Xna.Framework.Input;

namespace Axiom.Demos.Browser.Xna
{
	class XBoxInput: XnaInput
	{
	    private XInput.GamePadState _gpState;
	    private XInput.KeyboardState _keyboardState;

        public override void CaptureMouseState()
        {         
            _gpState = XInput.GamePad.GetState( XFG.PlayerIndex.One );
        }

        public override void CaptureKeyboardState()
        {
            _keyboardState = XInput.Keyboard.GetState( XFG.PlayerIndex.One );
        }

        public override void CheckKeyPressed( Axiom.Input.KeyCodes key, out bool isPressed )
        {
            isPressed = false;
            //XFG.Input.Keys xnaKey = Convert( key );

            //isPressed = _keyboardState.IsKeyDown( xnaKey );
        }

        public override void CheckMouseButtonPressed( Axiom.Input.MouseButtons button, out bool isPressed )
        {
            isPressed = false;
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
