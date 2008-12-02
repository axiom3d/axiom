using System;
using System.Collections.Generic;
using System.Linq;
using XFG = Microsoft.Xna.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;

namespace Axiom.Demos.Browser.Xna
{
    public class Menu : DrawableGameComponent
    {
        public class ItemSelectedEventArgs : EventArgs
        {
            public int SelectedIndex;
            public string SelectedText;
        }

        public event EventHandler<ItemSelectedEventArgs> ItemSelected;
        SpriteBatch spriteBatch;

        List<MenuItem> items = new List<MenuItem>();

        public int SelectedItem
        {
            get;
            protected set;
        } 

        KeyboardState oldState = Keyboard.GetState();

        public SpriteFont Font
        {
            get;
            set;
        }

        public Color FontColor
        {
            get;
            set;
        }

        public Color SelectedColor
        {
            get;
            set;
        }

        public Menu( XFG.Game game)
            : base( game )
        {
            spriteBatch = new SpriteBatch( game.GraphicsDevice );

        }

        public void AddItem(string name, string text, Vector2 position)
        {
            items.Add( new MenuItem( name, text, position, Font, FontColor, SelectedColor, false ) );
        }


        public override void Update( GameTime gameTime )
        {
            foreach ( var item in items )
            {
                item.Selected = false;
            }

            KeyboardState kb = Keyboard.GetState();

            if ( ( kb.IsKeyDown( Keys.Up ) ) && ( oldState.IsKeyUp( Keys.Up ) ) )
            {
                SelectedItem -= 1;
                if ( SelectedItem == -1 )
                {
                    SelectedItem = items.Count - 1;
                }
            }

            if ( ( kb.IsKeyDown( Keys.Down ) ) && ( oldState.IsKeyUp( Keys.Down ) ) )
            {
                SelectedItem += 1;
                if ( SelectedItem == items.Count )
                {
                    SelectedItem = 0;
                }
            }

            if ( ( kb.IsKeyDown( Keys.Space ) ) && ( oldState.IsKeyUp( Keys.Space ) ) )
            {
                ItemSelectedEventArgs e = new ItemSelectedEventArgs();
                e.SelectedIndex = SelectedItem;
                e.SelectedText = items[ SelectedItem ].Name;
                ItemSelected( this, e );
            }

            oldState = kb;

            items[ SelectedItem ].Selected = true;

            base.Update( gameTime );
        }

        public override void Draw( GameTime gameTime )
        {
            spriteBatch.Begin();


            foreach ( MenuItem item in items) 
                item.Draw( spriteBatch );

            spriteBatch.DrawString( Font, items[ SelectedItem ].Name + " selected.", new Vector2( 50f, 525f ), Color.White );
            spriteBatch.DrawString( Font, "Press SPACE to attempt to activate the current menu item.", new Vector2( 50f, 550f ), Color.White );

            spriteBatch.End();

            base.Draw( gameTime );
        }
    }

    public class MenuItem
    {
        private string _name = "";
        private string _text = "";
        private SpriteFont _font;
        private Vector2 _position = Vector2.Zero;
        private Color _baseColor;
        private Color _selectedColor;

        private bool _selected = false;

        public bool Selected
        {
            get
            {
                return _selected;
            }
            set
            {
                _selected = value;
            }
        }

        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }

        public MenuItem( string name, string text,  Vector2 position, SpriteFont font, Color baseColor, Color selectedColor, bool selected )
        {
            _name = name;
            _text = text;
            _font = font;
            _position = position;
            _baseColor = baseColor;
            _selectedColor = selectedColor;
            _selected = selected;
        }

        public void Draw( SpriteBatch spriteBatch )
        {
            if ( _selected )
            {
                spriteBatch.DrawString( _font, _text, _position, _selectedColor );
            }
            else
            {
                spriteBatch.DrawString( _font, _text, _position, _baseColor );
            }
        }
    }

}
