using System;

namespace Axiom.Core
{
	/// <summary>
	/// 	This is an interface that all engine subsystems should inherit from.  Subsystems will be
	/// 	made available to the engine via plugins, and can be requested in any application.
	/// 	<p/>
	/// 	<code>Engine.Instance.GetSubSystem(SubSystems.Rendering, "OpenGL");</code>
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public class SubSystemAttribute: Attribute
	{
		private SubSystems type;

		public SubSystemAttribute(SubSystems type)
		{
			this.type = type;
		}
	}

	public enum SubSystems
	{
		Rendering,
		Audio,
		Music,
		Physics,
		Input,
		Networking
	}
}
