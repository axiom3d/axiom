using System;

namespace Axiom
{
	public class OgreVersionAttribute : Attribute
	{
		public OgreVersionAttribute( int major, int minor, int revision )
			: this( major, minor, revision, String.Empty )
		{
		}

		public OgreVersionAttribute( int major, int minor, int revision, string comment )
		{
		}
	}

	public class AxiomHelperAttribute : Attribute
	{
		public AxiomHelperAttribute( int major, int minor )
			: this( major, minor, String.Empty )
		{
		}

		public AxiomHelperAttribute( int major, int minor, string comment )
		{
		}
	}
}