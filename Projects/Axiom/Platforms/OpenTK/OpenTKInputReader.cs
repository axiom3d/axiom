#region Namespace Declarations

using System.Threading;
using System.Drawing;

using Axiom.Input;
using Axiom.Utilities;

using OpenTK.Input;
using OpenTK;

using Axiom.Graphics;

#endregion Namespace Declarations

namespace Axiom.Platforms.OpenTK
{
	/// <summary>
	///		Platform management specialization
	/// </summary>
	public class OpenTKInputReader : InputReader
	{
		#region Fields

		private RenderWindow parent;
		private Point center;
		private bool ownMouse = false;
		private KeyboardDevice keyboard = null;
		private MouseDevice mouse = null;

		/// <summary>
		///		Is the opentk window currently visible?
		/// </summary>
		protected bool isVisible;

		protected int oldX, oldY, oldZ;
		protected int mouseX, mouseY;
		protected int relMouseX, relMouseY, relMouseZ;
		protected MouseButtons mouseButtons;

		/// <summary>
		///
		/// </summary>
		protected const int WheelStep = 60;

		/// <summary>
		///  Size of the arrays used to hold buffered input data.
		/// </summary>
		protected const int BufferSize = 16;

		#endregion Fields

		#region Constructor

		/// <summary>
		///		Constructor.
		/// </summary>
		public OpenTKInputReader()
		{
			// start off assuming we are visible
			isVisible = true;
		}

		/// <summary>
		///		Destructor.
		/// </summary>
		~OpenTKInputReader()
		{
			System.Windows.Forms.Cursor.Show();
			keyboard = null;
			mouse = null;
		}

		#endregion Constructor

		#region InputReader Members

		#region Properties

		public override int AbsoluteMouseX { get { return mouseX; } }

		public override int AbsoluteMouseY { get { return mouseY; } }

		public override int AbsoluteMouseZ { get { return 0; } }

		public override int RelativeMouseX { get { return relMouseX; } }

		public override int RelativeMouseY { get { return relMouseY; } }

		public override int RelativeMouseZ { get { return relMouseZ; } }

		#endregion Properties

		#region Methods

		/// <summary>
		///		Capture the current state of input.
		/// </summary>
		public override void Capture()
		{
			if( mouse == null )
			{
				return;
			}

			NativeWindow window = (NativeWindow)parent[ "nativewindow" ];

			isVisible = window.WindowState != WindowState.Minimized && window.Focused;

			// if we aren't active, wait
			if( window == null || !isVisible )
			{
				Thread.Sleep( 100 );
				return;
			}

			if( !useMouseEvents )
			{
				relMouseZ = mouse.Wheel - oldZ;
				oldZ = mouse.Wheel;
				mouseButtons = mouse[ MouseButton.Left ] == true ? MouseButtons.Left : 0;
				mouseButtons = mouse[ MouseButton.Right ] == true ? MouseButtons.Right : 0;
				mouseButtons = mouse[ MouseButton.Middle ] == true ? MouseButtons.Middle : 0;
			}
			if( ownMouse )
			{
				int mx = System.Windows.Forms.Cursor.Position.X;
				int my = System.Windows.Forms.Cursor.Position.Y;
				relMouseX = mx - center.X;
				relMouseY = my - center.Y;
				mouseX += relMouseX;
				mouseY += relMouseY;

				System.Windows.Forms.Cursor.Position = center;
			}
			else
			{
				int mx = mouse.X;
				int my = mouse.Y;
				relMouseX = mx - oldX;
				relMouseY = my - oldY;
				mouseX += relMouseX;
				mouseY += relMouseY;
				oldX = mx;
				oldY = my;
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="useKeyboard"></param>
		/// <param name="useMouse"></param>
		/// <param name="useGamepad"></param>
		/// <param name="ownMouse"></param>
		public override void Initialize( Axiom.Graphics.RenderWindow parent, bool useKeyboard, bool useMouse, bool useGamepad, bool ownMouse )
		{
			Contract.Requires( parent.GetType().Name == "OpenTKWindow", "RenderSystem", "OpenTK InputManager requires OpenTK OpenGL Renderer." );

			this.parent = parent;

			INativeWindow window = (INativeWindow)parent[ "nativewindow" ];

			if( window == null )
			{
				return;
			}

			keyboard = window.InputDriver.Keyboard[ 0 ];
			//keyboard = window.Keyboard;

			if( useMouse )
			{
				mouse = window.InputDriver.Mouse[ 0 ];
				if( ownMouse )
				{
					this.ownMouse = true;
					System.Windows.Forms.Cursor.Hide();
				}
				// mouse starts out in the center of the window
				center.X = parent.Width / 2;
				center.Y = parent.Height / 2;

				if( ownMouse )
				{
					center = window.PointToScreen( center );
					System.Windows.Forms.Cursor.Position = center;
					mouseX = oldX = center.X;
					mouseY = oldY = center.Y;
				}
				else
				{
					Point center2 = window.PointToScreen( center );
					System.Windows.Forms.Cursor.Position = center2;
					mouseX = oldX = center.X;
					mouseY = oldY = center.Y;
				}
			}
		}

		/// <summary>
		///		Checks the current keyboard state to see if the specified key is pressed.
		/// </summary>
		/// <param name="key">KeyCode to check.</param>
		/// <returns>true if the key is down, false otherwise.</returns>
		public override bool IsKeyPressed( KeyCodes key )
		{
			if( keyboard == null )
			{
				return false;
			}
			return keyboard[ ConvertKeyEnum( key ) ] == true;
		}

		public override bool IsMousePressed( MouseButtons button )
		{
			return ( mouseButtons & button ) != 0;
		}

		public override bool UseKeyboardEvents { get { return useKeyboardEvents; } set { useKeyboardEvents = value; } }

		public override bool UseMouseEvents { get { return useMouseEvents; } set { useMouseEvents = value; } }

		public override void Dispose() {}

		#endregion Methods

		#endregion InputReader Members

		#region Keycode Conversions

		/// <summary>
		///		Used to convert an Axiom.Input.KeyCodes enum val to a OpenTK enum val.
		/// </summary>
		/// <param name="key">Axiom keyboard code to query.</param>
		/// <returns>The equivalent enum value in the OpenTK enum.</returns>
		private Key ConvertKeyEnum( KeyCodes key )
		{
			Key k = 0;

			switch( key )
			{
				case KeyCodes.A:
					k = Key.A;
					break;
				case KeyCodes.B:
					k = Key.B;
					break;
				case KeyCodes.C:
					k = Key.C;
					break;
				case KeyCodes.D:
					k = Key.D;
					break;
				case KeyCodes.E:
					k = Key.E;
					break;
				case KeyCodes.F:
					k = Key.F;
					break;
				case KeyCodes.G:
					k = Key.G;
					break;
				case KeyCodes.H:
					k = Key.H;
					break;
				case KeyCodes.I:
					k = Key.I;
					break;
				case KeyCodes.J:
					k = Key.J;
					break;
				case KeyCodes.K:
					k = Key.K;
					break;
				case KeyCodes.L:
					k = Key.L;
					break;
				case KeyCodes.M:
					k = Key.M;
					break;
				case KeyCodes.N:
					k = Key.N;
					break;
				case KeyCodes.O:
					k = Key.O;
					break;
				case KeyCodes.P:
					k = Key.P;
					break;
				case KeyCodes.Q:
					k = Key.Q;
					break;
				case KeyCodes.R:
					k = Key.R;
					break;
				case KeyCodes.S:
					k = Key.S;
					break;
				case KeyCodes.T:
					k = Key.T;
					break;
				case KeyCodes.U:
					k = Key.U;
					break;
				case KeyCodes.V:
					k = Key.V;
					break;
				case KeyCodes.W:
					k = Key.W;
					break;
				case KeyCodes.X:
					k = Key.X;
					break;
				case KeyCodes.Y:
					k = Key.Y;
					break;
				case KeyCodes.Z:
					k = Key.Z;
					break;
				case KeyCodes.Left:
					k = Key.Left;
					break;
				case KeyCodes.Right:
					k = Key.Right;
					break;
				case KeyCodes.Up:
					k = Key.Up;
					break;
				case KeyCodes.Down:
					k = Key.Down;
					break;
				case KeyCodes.Escape:
					k = Key.Escape;
					break;
				case KeyCodes.F1:
					k = Key.F1;
					break;
				case KeyCodes.F2:
					k = Key.F2;
					break;
				case KeyCodes.F3:
					k = Key.F3;
					break;
				case KeyCodes.F4:
					k = Key.F4;
					break;
				case KeyCodes.F5:
					k = Key.F5;
					break;
				case KeyCodes.F6:
					k = Key.F6;
					break;
				case KeyCodes.F7:
					k = Key.F7;
					break;
				case KeyCodes.F8:
					k = Key.F8;
					break;
				case KeyCodes.F9:
					k = Key.F9;
					break;
				case KeyCodes.F10:
					k = Key.F10;
					break;
				case KeyCodes.D0:
					k = Key.Number0;
					break;
				case KeyCodes.D1:
					k = Key.Number1;
					break;
				case KeyCodes.D2:
					k = Key.Number2;
					break;
				case KeyCodes.D3:
					k = Key.Number3;
					break;
				case KeyCodes.D4:
					k = Key.Number4;
					break;
				case KeyCodes.D5:
					k = Key.Number5;
					break;
				case KeyCodes.D6:
					k = Key.Number6;
					break;
				case KeyCodes.D7:
					k = Key.Number7;
					break;
				case KeyCodes.D8:
					k = Key.Number8;
					break;
				case KeyCodes.D9:
					k = Key.Number9;
					break;
				case KeyCodes.F11:
					k = Key.F11;
					break;
				case KeyCodes.F12:
					k = Key.F12;
					break;
				case KeyCodes.Enter:
					k = Key.Enter;
					break;
				case KeyCodes.Tab:
					k = Key.Tab;
					break;
				case KeyCodes.LeftShift:
					k = Key.ShiftLeft;
					break;
				case KeyCodes.RightShift:
					k = Key.ShiftRight;
					break;
				case KeyCodes.LeftControl:
					k = Key.ControlLeft;
					break;
				case KeyCodes.RightControl:
					k = Key.ControlRight;
					break;
				case KeyCodes.Period:
					k = Key.Period;
					break;
				case KeyCodes.Comma:
					k = Key.Comma;
					break;
				case KeyCodes.Home:
					k = Key.Home;
					break;
				case KeyCodes.PageUp:
					k = Key.PageUp;
					break;
				case KeyCodes.PageDown:
					k = Key.PageDown;
					break;
				case KeyCodes.End:
					k = Key.End;
					break;
				case KeyCodes.Semicolon:
					k = Key.Semicolon;
					break;
				case KeyCodes.Subtract:
					k = Key.Minus;
					break;
				case KeyCodes.Add:
					k = Key.Plus;
					break;
				case KeyCodes.Backspace:
					k = Key.BackSpace;
					break;
				case KeyCodes.Delete:
					k = Key.Delete;
					break;
				case KeyCodes.Insert:
					k = Key.Insert;
					break;
				case KeyCodes.LeftAlt:
					k = Key.AltLeft;
					break;
				case KeyCodes.RightAlt:
					k = Key.AltRight;
					break;
				case KeyCodes.Space:
					k = Key.Space;
					break;
				case KeyCodes.Tilde:
					k = Key.Tilde;
					break;
				case KeyCodes.OpenBracket:
					k = Key.BracketLeft;
					break;
				case KeyCodes.CloseBracket:
					k = Key.BracketRight;
					break;
				case KeyCodes.Plus:
					k = Key.Plus;
					break;
				case KeyCodes.QuestionMark:
					k = Key.Slash;
					break;
				case KeyCodes.Quotes:
					k = Key.Quote;
					break;
				case KeyCodes.Backslash:
					k = Key.BackSlash;
					break;
			}

			return k;
		}

		#endregion Keycode Conversions
	}
}
