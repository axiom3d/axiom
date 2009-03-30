using System;
using System.Collections;

using Axiom;
using Axiom.Input;
using Axiom.Overlays;
using Axiom.Math;

namespace YAT
{
    public class MenuState : TetrisState
    {

        #region Fields
        protected Overlay menuOverlay;
        protected ArrayList menuItems = new ArrayList();
        protected int selectedItem;
        #endregion

        #region Constructors
        public MenuState()
        {
            menuOverlay = null;
        }
        #endregion

        #region Methods
        public override void Initialize()
        {
            base.Initialize();

            TetrisApplication.Instance.setMenuOverlay( menuOverlay );
            selectedItem = 0;

            for ( int i = 0; i <= menuItems.Count - 1; ++i )
            {
                SetItemState( i, i == selectedItem );
            }
        }
        public override void Cleanup()
        {
            TetrisApplication.Instance.setMenuOverlay( null );
        }
        public override void FrameStarted( float dt )
        {
            // Slowly rotate level
            // Slowly rotate level
            float Pi = (float)System.Math.PI;
            game.cameraWantedYaw += Utility.RadiansToDegrees( 0.25f * dt );
            while ( game.cameraWantedYaw > ( 2.0f * Pi ) )
                game.cameraWantedYaw -= Utility.RadiansToDegrees( 2.0f * Pi );

            base.FrameStarted( dt );
        }
        public override void KeyPressed( KeyEventArgs e )
        {
            switch ( e.Key )
            {
                case Axiom.Input.KeyCodes.Return:
                case Axiom.Input.KeyCodes.Enter://KC_NUMPADENTER
                    OnSelected( selectedItem );
                    break;

                default:
                    // Default processing
                    base.KeyPressed( e );
                    break;
            }


        }

        public override void KeyRepeated( Axiom.Input.KeyCodes kc )
        {
            // Handle keys common to all menu states
            switch ( kc )
            {
                case Axiom.Input.KeyCodes.Up:
                    SetItemState( selectedItem, false );
                    if ( selectedItem != 0 )
                        --selectedItem;
                    else
                        selectedItem = ( menuItems.Count - 1 ) - 1;

                    SetItemState( selectedItem, true );
                    break;

                case Axiom.Input.KeyCodes.Down:
                    SetItemState( selectedItem, false );
                    if ( ++selectedItem == menuItems.Count - 1 )
                        selectedItem = 0;

                    SetItemState( selectedItem, true );
                    break;
            }
        }



        //
        protected virtual void SetItemState( int item, bool selected )
        {
            OverlayElement element = (OverlayElement)menuItems[ item ];

            // element can be null temporaily when changing menus
            if ( element == null )
                return;


            // Apply hardcoded menu item colours
            if ( selected )
            {
                element.SetParam( "colour_top", "1.0 1.0 0.0" );
                element.SetParam( "colour_bottom", "0.8 0.8 0.0" );
            }
            else
            {
                element.SetParam( "colour_top", "1.0 1.0 1.0" );
                element.SetParam( "colour_bottom", "0.8 0.8 0.8" );

            }
        }
        protected virtual void OnSelected( int item ) //issue of = 0;
        {

        }

        public override void HandleInput()
        {

            if ( input.IsKeyPressed( Axiom.Input.KeyCodes.Enter ) )
            {
                OnSelected( selectedItem );
            }
            if ( input.IsKeyPressed( Axiom.Input.KeyCodes.Up ) )
            {
                SetItemState( selectedItem, false );
                if ( selectedItem != 0 )
                    --selectedItem;
                else
                    selectedItem = ( menuItems.Count ) - 1;

                SetItemState( selectedItem, true );
            }
            if ( input.IsKeyPressed( Axiom.Input.KeyCodes.Down ) )
            {
                SetItemState( selectedItem, false );
                if ( ++selectedItem == menuItems.Count )
                    selectedItem = 0;

                SetItemState( selectedItem, true );
            }
        }

        #endregion
    }
}