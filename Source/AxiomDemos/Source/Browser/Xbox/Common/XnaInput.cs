using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using XFG = Microsoft.Xna.Framework;

namespace Axiom.Demos.Browser.Xna
{
    abstract class XnaInput : Axiom.Input.InputReader
    {
        public abstract void CaptureMouseState();
        public abstract void CaptureKeyboardState();

        public abstract void CheckKeyPressed( Axiom.Input.KeyCodes key, out bool isPressed );
        public abstract void CheckMouseButtonPressed( Axiom.Input.MouseButtons button, out bool isPressed );

        #region Axiom.Input.InputReader Implementation

        protected XFG.Vector3 relativeMousePosition = XFG.Vector3.Zero;
        protected XFG.Vector3 absoluteMousePosition = XFG.Vector3.Zero;

        public override void Initialize( Axiom.Graphics.RenderWindow parent, bool useKeyboard, bool useMouse, bool useGamepad, bool ownMouse )
        {
        }

        public override void Capture()
        {
            CaptureMouseState();
            CaptureKeyboardState();
        }

        public override bool IsKeyPressed( Axiom.Input.KeyCodes key )
        {
            bool isPressed = false;
            CheckKeyPressed( key, out isPressed );
            return isPressed;
        }

        public override bool IsMousePressed( Axiom.Input.MouseButtons button )
        {
            bool isPressed = false;
            CheckMouseButtonPressed( button, out isPressed );
            return isPressed;
        }

        public override int RelativeMouseX
        {
            get
            {
                return (int)relativeMousePosition.X;
            }
        }

        public override int RelativeMouseY
        {
            get
            {
                return (int)relativeMousePosition.Y;
            }
        }

        public override int RelativeMouseZ
        {
            get
            {
                return (int)relativeMousePosition.Z;
            }
        }

        public override int AbsoluteMouseX
        {
            get
            {
                return (int)absoluteMousePosition.X;
            }
        }

        public override int AbsoluteMouseY
        {
            get
            {
                return (int)absoluteMousePosition.Y;
            }
        }

        public override int AbsoluteMouseZ
        {
            get
            {
                return (int)absoluteMousePosition.Z;
            }
        }

        public override bool UseKeyboardEvents
        {
            get
            {
                return useKeyboardEvents;
            }
            set
            {
                useKeyboardEvents = value;
            }
        }

        public override void Dispose()
        {
        }

        #endregion Axiom.Input.InputReader Implementation
    }
}
