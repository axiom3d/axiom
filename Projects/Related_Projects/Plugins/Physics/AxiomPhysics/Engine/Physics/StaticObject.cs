using System;
using Axiom.Core;
using Axiom.Scripting;

namespace Axiom.Physics
{
	/// <summary>
	/// Zusammenfassung für DynamicObject.
	/// </summary>
	public class StaticObject : PhysicalObject
	{
		public StaticObject (SceneManager sceneManager, Entity entity, string name, string surface):base(sceneManager, entity, name, surface)
		{
		}

		public StaticObject (SceneManager sceneManager, Entity[] entities, string name, string surface):base(sceneManager, entities, name, surface)
		{
		}

		public override PhysicalType Type
		{
			get { return PhysicalType.STATIC; }
		}
	}
}