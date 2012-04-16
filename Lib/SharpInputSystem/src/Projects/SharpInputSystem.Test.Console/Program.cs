using System;
using System.Collections.Generic;
using System.Text;
using SWF = System.Windows.Forms;

using log4net;

using SharpInputSystem;

#if SIS_SDL_PLATFORM

using Tao.Sdl;

#endif

namespace SharpInputSystem.Test.Console
{
    class EventHandler : IKeyboardListener, IMouseListener, IJoystickListener
    {
		private static readonly ILog log = LogManager.GetLogger( typeof( Program ) );

        public bool appRunning = true;

        #region IKeyboardListener Members

        public bool KeyPressed( KeyEventArgs e )
        {
            log.Info( String.Format(  "KeyPressed : {0} {1}", e.Key, e.Text ));
            return true;
        }

        public bool KeyReleased( KeyEventArgs e )
        {
            if ( e.Key == KeyCode.Key_ESCAPE || e.Key == KeyCode.Key_Q )
                appRunning = false;
            return true;
        }

        #endregion

        #region IMouseListener Members

        public bool MouseMoved( MouseEventArgs arg )
        {
            log.Info(String.Format("MouseMoved : R( {0} , {1} , {4} ) A( {2} , {3}, {5} )", arg.State.X.Relative, arg.State.Y.Relative, arg.State.X.Absolute, arg.State.Y.Absolute, arg.State.Z.Relative, arg.State.Z.Absolute ));
            return true;
        }

        public bool MousePressed( MouseEventArgs arg, MouseButtonID id )
        {
            log.Info( String.Format(  "MousePressed : {0}", arg.State.Buttons ));
            return true;
        }

        public bool MouseReleased( MouseEventArgs arg, MouseButtonID id )
        {
            log.Info( String.Format(  "MouseReleased : {0}", arg.State.Buttons ));
            return true;
        }

        #endregion

        #region IJoystickListener Members

        public bool ButtonPressed(JoystickEventArgs arg, int button)
        {
            log.Info(String.Format("Joystick ButtonPressed : {0} )", button));
            return true;
        }
        public bool ButtonReleased(JoystickEventArgs arg, int button)
        {
            return true;
        }

        public bool AxisMoved(JoystickEventArgs arg, int axis)
        {
            int axisValue = arg.State.Axis[axis].Absolute;
            if (axisValue > 2500 || axisValue < -2500)
            {
                log.Info(String.Format("Joystick AxisMoved : {0} = {1} )", axis, axisValue));
            }
            return true;
        }

        public bool SliderMoved(JoystickEventArgs arg, int slider)
        {
            return true;
        }

        public bool PovMoved(JoystickEventArgs arg, int pov)
        {
            return true;
        }

        #endregion IJoystickListener Members
    }

    class Program
    {
        private static EventHandler _handler = new EventHandler();

        private static InputManager _inputManager;
        private static Keyboard _kb;
        private static Mouse _m;

        private static List<Joystick> _joys = new List<Joystick>();
        private static List<ForceFeedback> _ff = new List<ForceFeedback>();

		private static readonly ILog log = LogManager.GetLogger( typeof( Program ) );
		
#if SIS_SDL_PLATFORM
		static void InitSdl( Main frm )
		{
			Sdl.SDL_Init( Sdl.SDL_INIT_VIDEO ) ;
			Sdl.SDL_SetVideoMode( 100, 100, 32, Sdl.SDL_OPENGL | Sdl.SDL_HWPALETTE );
		}
#endif

		static void DoStartup()
        {
	        ParameterList pl = new ParameterList();
            Main frm = new Main();
            pl.Add( new Parameter( "WINDOW", frm) );

#if SIS_SDL_PLATFORM
			InitSdl( frm );
#endif
            //Default mode is foreground exclusive..but, we want to show mouse - so nonexclusive
            //pl.Add( new Parameter( "w32_mouse", "CLF_FOREGROUND" ) );
            //pl.Add( new Parameter( "w32_mouse", "CLF_NONEXCLUSIVE" ) );

	        //This never returns null.. it will raise an exception on errors
	        _inputManager = InputManager.CreateInputSystem(pl);

			log.Info( String.Format( "SIS Version : {0}", _inputManager.Version ) );
			log.Info( String.Format( "Platform : {0}", _inputManager.InputSystemName ) );
			log.Info( String.Format( "Number of Mice : {0}", _inputManager.DeviceCount<Mouse>() ) );
			log.Info( String.Format( "Number of Keyboards : {0}", _inputManager.DeviceCount<Keyboard>()) );
			log.Info( String.Format( "Number of Joys/Pads: {0}", _inputManager.DeviceCount<Joystick>() ) );

            bool buffered = true;

            if ( _inputManager.DeviceCount<Keyboard>() > 0 )
            {
                _kb = _inputManager.CreateInputObject<Keyboard>( buffered, "" );
                log.Info( String.Format( "Created {0}buffered keyboard", buffered ? "" : "un" ) );
                _kb.EventListener = _handler;
            }

			if ( _inputManager.DeviceCount<Mouse>() > 0 )
            {
				_m = _inputManager.CreateInputObject<Mouse>( buffered, "" );
                log.Info( String.Format( "Created {0}buffered mouse", buffered ? "" : "un" ) );
                _m.EventListener = _handler;

                MouseState ms = _m.MouseState;
                ms.Width = 100;
                ms.Height = 100;
            }

            ////This demo only uses at max 4 joys
            int numSticks = _inputManager.DeviceCount<Joystick>();
            if( numSticks > 4 )	numSticks = 4;

            for( int i = 0; i < numSticks; ++i )
            {
                _joys.Insert(i, _inputManager.CreateInputObject<Joystick>(true,""));
                _joys[i].EventListener = _handler;

                _ff.Insert(i, (ForceFeedback)_joys[i].QueryInterface<ForceFeedback>());
                if( _ff[i] != null )
                {
                    log.Info( String.Format( "Created buffered joystick with ForceFeedback support." ) );
                      //Dump out all the supported effects:
            //        const ForceFeedback::SupportedEffectList &list = g_ff[i]->getSupportedEffects();
            //        ForceFeedback::SupportedEffectList::const_iterator i = list.begin(),
            //            e = list.end();
            //        for( ; i != e; ++i)
            //            std::cout << "Force =  " << i->first << " Type = " << i->second << std::endl;
                }
                else
                    log.Info( String.Format( "Created buffered joystick. **without** FF support" ) );
            }
        }

        static void Main( string[] args )
        {
            log.Info( "SharpInputSystem Console Application" );
            try
            {
                DoStartup();
 
                while ( _handler.appRunning )
                {
                    //Throttle down CPU usage

                    if ( _kb != null )
                    {
                        _kb.Capture();
                        if ( !_kb.IsBuffered )
                            handleNonBufferedKeys();
                    }

                    if ( _m != null )
                    {
                        _m.Capture();
                        if ( !_m.IsBuffered )
                            handleNonBufferedMouse();
                    }

                    foreach( Joystick joy in _joys )
                    {
                        if( joy != null )
                        {
                            joy.Capture();
                            if( !joy.IsBuffered )
                                handleNonBufferedJoystick( joy );
                        }
                    }
                }
            }
            catch ( Exception e )
            {
				log.Error( "SIS Exception Caught!", e );
				log.Info( "Press any key to exit." );
                System.Console.ReadKey();
            }

	        if( _inputManager !=null )
	        {
		        _inputManager.DestroyInputObject( _kb );
		        _inputManager.DestroyInputObject( _m );

                foreach (Joystick joy in _joys)
                {
                    _inputManager.DestroyInputObject( joy );
                }
	        }

			log.Info( "Goodbye" );
	        return;

        }

        private static void handleNonBufferedKeys()
        {
            if ( _kb.IsKeyDown( KeyCode.Key_ESCAPE ) || _kb.IsKeyDown( KeyCode.Key_Q ) )
                _handler.appRunning = false;
            if ( _kb.IsShiftState( Keyboard.ShiftState.Alt ) ) System.Console.Write( " ALT " );
            if ( _kb.IsShiftState( Keyboard.ShiftState.Shift ) ) System.Console.Write( " SHIFT " );
            if ( _kb.IsShiftState( Keyboard.ShiftState.Ctrl ) ) System.Console.Write( " CTRL " );

            int [] ks = _kb.KeyStates;
            for ( int i = 0; i < ks.Length; i++ )
            {
                if ( ks[ i ] != 0 )
                {
                    log.Info( String.Format( "KeyPressed : {0} {1}", (KeyCode)i, i ));
                }
            }
        }

        private static void handleNonBufferedMouse()
        { }

        private static void handleNonBufferedJoystick( Joystick joy )
        { }

    }
}
