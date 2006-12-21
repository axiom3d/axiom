using System;
using Axiom.Core;
using Axiom.Scripting;

namespace Axiom.Physics
{
	/// <summary>
	/// Zusammenfassung für DynamicObject.
	/// </summary>
	public class DynamicObject : PhysicalObject
	{
		protected object _implementation;
		private float mass;
		private float angulardamping = 0;
		private float lineardamping = 0;

		public DynamicObject (SceneManager sceneManager, Entity entity, string name, float mass, string surface):base(sceneManager,entity,name,surface)
		{
			this.mass = mass;
		}

		public DynamicObject (SceneManager sceneManager, Entity[] entities, string name, float mass, string surface):base(sceneManager,entities,name,surface)
		{
			this.mass = mass;
		}

		public override PhysicalType Type
		{
			get { return PhysicalType.DYNAMIC; }
		}

		public Object implementation
		{
			get { return _implementation; }
			set { _implementation = value; }
		}

		public float Mass
		{
			get { return mass; }
		}

		public float AngularDamping
		{
			get { return angulardamping; }
			set { angulardamping = value; }
		}

		public float LinearDamping
		{
			get { return lineardamping; }
			set { lineardamping = value; }
		}
	}
}


