using System;
using System.Drawing;
using System.Drawing.Imaging;
using RealmForge;

namespace RealmForge.Serialization
{

    public class ColorParser : IObjectParser
    {
        #region IObjectParser Members

        public string GetParsableText( object instance )
        {
            return ( (Color)instance ).Name;//hex string if custom, name if is known
        }

        public object ParseObject( string data )
        {
            Color c = Color.FromName( data );
            if ( c.IsEmpty || !c.IsKnownColor )
            {
                try
                {
                    return Color.FromArgb( int.Parse( data, System.Globalization.NumberStyles.HexNumber ) );
                }
                catch ( Exception )
                {
                    return Color.Empty;
                }
            }
            return c;
        }

        #endregion

    }

    public class StaticPropertyEnumParser : IObjectParser
    {
        #region Fields
        public Type Type = null;
        #endregion

        #region Constructors
        public StaticPropertyEnumParser( Type insepctedType )
        {
            Type = insepctedType;
        }
        #endregion

        #region IObjectParser Members

        public string GetParsableText( object instance )
        {
            string propertyName = Reflector.GetStaticPropertyEqualTo( Type, instance );
            if ( propertyName == null )
                Log.DebugWarn( "Failed to find a static property of type {0} that matches instance {1}", Type, instance );
            return propertyName;
        }

        public object ParseObject( string data )
        {
            string[] names = Reflector.GetImageFormatNames();
            object val = Reflector.GetStaticPropertyValue( Type, data );
            if ( val == null )
                Log.DebugWarn( "Failed to find a static property of type {0} by the name {1}", Type, data );
            return val;
        }

        #endregion

    }

    public class PointParser : IObjectParser
    {

        public string GetParsableText( object instance )
        {
            Point vec = (Point)instance;
            return "(" + vec.X + ',' + vec.Y + ')';
        }
        public object ParseObject( string data )
        {
            try
            {
                string[] vals = data.TrimStart( '(' ).TrimEnd( ')' ).Split( ',' );
                return new Point(
                    int.Parse( vals[0] ),
                    int.Parse( vals[1] )
                    );
            }
            catch ( Exception e )
            {
                throw new GeneralException( "Could not parse Point from '{0}'", data );
            }
            //return null;
        }
    }

    public class SizeParser : IObjectParser
    {

        public string GetParsableText( object instance )
        {
            Size vec = (Size)instance;
            return "(" + vec.Width + ',' + vec.Height + ')';
        }
        public object ParseObject( string data )
        {
            try
            {
                string[] vals = data.TrimStart( '(' ).TrimEnd( ')' ).Split( ',' );
                return new Size(
                    int.Parse( vals[0] ),
                    int.Parse( vals[1] )
                    );
            }
            catch ( Exception e )
            {
                throw new GeneralException( "Could not parse Size from '{0}'", data );
            }
            //return null;
        }
    }


}