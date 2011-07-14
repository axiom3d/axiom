using System;

namespace Axiom
{
    public class OgreVersionAttribute : Attribute
    {
        public OgreVersionAttribute( int major, int minor, string comment="")
        {
        }
    }
}