using System;
using System.Collections.Generic;
using System.Text;

namespace Axiom.Core
{
    public class NameGenerator<T>
    {
        private static long _nextId;
        private static string _baseName;

        public long NextIdentifier
        {
            get
            {
                return _nextId;
            }
            set
            {
                _nextId = value;
            }
        }

        public NameGenerator()
            : this( typeof( T ).Name )
        {
        }

        public NameGenerator(string baseName)
        {
            if ( _baseName != null )
                _baseName = baseName;
        }

        public string GetNextUniqueName()
        {
            return GetNextUniqueName( String.Empty );
        }

        public string GetNextUniqueName( string prefix )
        {
            return String.Format( "{0}{1}{2}", prefix, _baseName, _nextId++ );
        }
    }
}
