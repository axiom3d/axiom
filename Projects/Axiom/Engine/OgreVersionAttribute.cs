using System;

namespace Axiom
{
    public class OgreVersionAttribute : Attribute
    {
        public OgreVersionAttribute( int major, int minor, int revision, string comment="")
        {
        }
    }

    public class AxiomHelperAttribute: Attribute
    {
        public AxiomHelperAttribute(int major, int minor, string comment="")
        {
        }
    }
}