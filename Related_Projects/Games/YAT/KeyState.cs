using System;
using Axiom;
using Axiom.Input;
namespace YAT
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	public class KeyState
	{
		private Axiom.Input.KeyCodes key;
		private InputReader input;
		private bool alreadyPressed = false;

		public KeyState(Axiom.Input.KeyCodes key)
		{
			this.key = key;
			input = TetrisApplication.Instance.Input;

		}
		public bool KeyDownEvent()
		{
			bool pressed = false;
			bool currentlyPressed = input.IsKeyPressed(key);
			if (currentlyPressed && !alreadyPressed)
			{
				pressed = true;
			}
			
			alreadyPressed = currentlyPressed;
			return pressed;
			
		}
	}
}
