using System;
using RealmForge.Serialization;
using RealmForge;

namespace RealmForge.Reflection
{
    public class TypeResolverInfo : IParsable
    {
        #region Properties
        public string Type;
        public string AssemblyPath;
        protected const char delim = '@';
        public Type ClassType
        {
            get
            {
                return Reflector.GetTypeFrom( this );
            }
        }
        #endregion

        #region Methods

        public object CreateInstance( out Type type )
        {
            Type t = Reflector.GetTypeFrom( this );
            type = t;
            if ( t == null )
                return null;
            return Activator.CreateInstance( t );
        }
        public object CreateInstance()
        {
            Type t = Reflector.GetTypeFrom( this );
            if ( t == null )
                return null;
            return Activator.CreateInstance( t );
        }
        public string ToParsableText()
        {
            if ( Type == null )
                return string.Empty;
            if ( AssemblyPath != null && AssemblyPath != string.Empty )
                return Type + delim + AssemblyPath;
            return Type;
        }
        public override string ToString()
        {
            if ( AssemblyPath == null )
                return Type;
            return Type + "from " + AssemblyPath;
        }

        #endregion

        #region Constructors
        public TypeResolverInfo()
        {
        }
        public TypeResolverInfo( ParsingData data )
        {
            if ( data.Text != string.Empty )
            {
                string[] parts = data.Text.Split( delim );
                Type = parts[0];
                if ( parts.Length > 1 )
                    AssemblyPath = parts[1];
            }
        }
        public TypeResolverInfo( string type, string asmPath )
        {
            Type = type;
            AssemblyPath = asmPath;
        }

        public TypeResolverInfo( string type )
            : this( type, null )
        {
        }
        #endregion
    }
}
