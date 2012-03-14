#region Namespace Declarations

using System.Drawing;
using System.Threading;
using System.Windows.Forms;

using Axiom.Graphics;
using Axiom.Input;
using Axiom.Utilities;

using OpenTK;
using OpenTK.Input;

using MouseButtons = Axiom.Input.MouseButtons;
using NativeWindow = OpenTK.NativeWindow;

#endregion Namespace Declarations

namespace Axiom.Platforms.OpenTK
{
	/// <summary>
	///		Platform management specialization
	/// </summary>
	public class OpenTKInputReader : InputReader
	{
		#region Fields

		/// <summary>
		///
		/// </summary>
		protected const int WheelStep = 60;

		/// <summary>
		///  Size of the arrays used to hold buffered input data.
		/// </summary>
		protected const int BufferSize = 16;

		private Point center;

		/// <summary>
		///		Is the opentk window currently visible?
		/// </summary>
		protected bool isVisible;

		private KeyboardDevice keyboard;
		private MouseDevice mouse;
		protected MouseButtons mouseButtons;
		protected int mouseX, mouseY;

		protected int oldX, oldY, oldZ;
		private bool ownMouse;
		private RenderWindow parent;
		protected int relMouseX, relMouseY, relMouseZ;

		#endregion Fields

		#region Constructor

		/// <summary>
		///		Constructor.
		/// </summary>
		public OpenTKInputReader()
		{
			// start off assuming we are visible
			this.isVisible = true;
		}

		/// <summary>
		///		Destructor.
		/// </summary>
		~OpenTKInputReader()
		{
			Cursor.Show();
			this.keyboard = null;
			this.mouse = null;
		}

		#endregion Constructor

		#region Properties

		public override int AbsoluteMouseX
		{
			get
			{
				return this.mouseX;
			}
		}

		public override int AbsoluteMouseY
		{
			get
			{
				return this.mouseY;
			}
		}

		public override int AbsoluteMouseZ
		{
			get
			{
				return 0;
			}
		}

		public override int RelativeMouseX
		{
			get
			{
				return this.relMouseX;
			}
		}

		public override int RelativeMouseY
		{
			get
			{
				return this.relMouseY;
			}
		}

		public override int RelativeMouseZ
		{
			get
			{
				return this.relMouseZ;
			}
		}

		#endregion Properties

		#region Methods

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

		public override bool UseMouseEvents
		{
			get
			{
				return useMouseEvents;
			}
			set
			{
				useMouseEvents = value;
			}
		}

		/// <summary>
		///		Capture the current state of input.
		/// </summary>
		public override void Capture()
		{
			if ( this.mouse == null )
			{
				return;
			}

			var window = (NativeWindow)this.parent[ "nativewindow" ];

			this.isVisible = window.WindowState != WindowState.Minimized && window.Focused;

			// if we aren't active, wait
			if ( window == null || !this.isVisible )
			{
				Thread.Sleep( 100 );
				return;
			}

			if ( !useMouseEvents )
			{
				this.relMouseZ = this.mouse.Wheel - this.oldZ;
				this.oldZ = this.mouse.Wheel;
				this.mouseButtons = this.mouse[ MouseButton.Left ] ? MouseButtons.Left : 0;
				this.mouseButtons = this.mouse[ MouseButton.Right ] ? MouseButtons.Right : 0;
				this.mouseButtons = this.mouse[ MouseButton.Middle ] ? MouseButtons.Middle : 0;
			}
			if ( this.ownMouse )
			{
				int mx = Cursor.Position.X;
				int my = Cursor.Position.Y;
				this.relMouseX = mx - this.center.X;
				this.relMouseY = my - this.center.Y;
				this.mouseX += this.relMouseX;
				this.mouseY += this.relMouseY;

				Cursor.Position = this.center;
			}
			else
			{
				int mx = this.mouse.X;
				int my = this.mouse.Y;
				this.relMouseX = mx - this.oldX;
				this.relMouseY = my - this.oldY;
				this.mouseX += this.relMouseX;
				this.mouseY += this.relMouseY;
				this.oldX = mx;
				this.oldY = my;
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
		public override void Initialize( RenderWindow parent, bool useKeyboard, bool useMouse, bool useGamepad, bool ownMouse )
		{
			Contract.Requires( parent.GetType().Name == "OpenTKWindow", "RenderSystem", "OpenTK InputManager requires OpenTK OpenGL Renderer." );

			this.parent = parent;

			var window = (INativeWindow)parent[ "nativewindow" ];

			if ( window == null )
			{
				return;
			}

			this.keyboard = window.InputDriver.Keyboard[ 0 ];
			//keyboard = window.Keyboard;

			if ( useMouse )
			{
				this.mouse = window.InputDriver.Mouse[ 0 ];
				if ( ownMouse )
				{
					this.ownMouse = true;
					Cursor.Hide();
				}
				// mouse starts out in the center of the window
				this.center.X = parent.Width / 2;
				this.center.Y = parent.Height / 2;

				if ( ownMouse )
				{
					this.center = window.PointToScreen( this.center );
					Cursor.Position = this.center;
					this.mouseX = this.oldX = this.center.X;
					this.mouseY = this.oldY = this.center.Y;
				}
				else
				{
					Point center2 = window.PointToScreen( this.center );
					Cursor.Position = center2;
					this.mouseX = this.oldX = this.center.X;
					this.mouseY = this.oldY = this.center.Y;
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
			if ( this.keyboard == null )
			{
				return false;
			}
			return this.keyboard[ ConvertKeyEnum( key ) ];
		}

		public override bool IsMousePressed( MouseButtons button )
		{
			return ( this.mouseButtons & button ) != 0;
		}

		public override void Dispose() { }

		#endregion Methods

		#region Keycode Conversions

		/// <summary>
		///		Used to convert an Axiom.Input.KeyCodes enum val to a OpenTK enum val.
		/// </summary>
		/// <param name="key">Axiom keyboard code to query.</param>
		/// <returns>The equivalent enum value in the OpenTK enum.</returns>
		private Key ConvertKeyEnum( KeyCodes key )
		{
			Key k = 0;

			switch ( key )
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
