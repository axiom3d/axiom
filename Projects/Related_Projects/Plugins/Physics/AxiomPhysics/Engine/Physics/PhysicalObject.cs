using System;
using Axiom.Core;
using Axiom.Scripting;

namespace Axiom.Physics
{
	/// <summary>
	/// Zusammenfassung für DynamicObject.
	/// </summary>
	public class PhysicalObject : GameObject, IPhysicalObject
	{
		protected string name;
		protected string surface;

		public PhysicalObject (SceneManager sceneManager, Entity entity, string name, string surface):base(sceneManager)
		{
			this.name = name;
			this.surface = surface;
	
			sceneObject = entity;
			node = sceneMgr.RootSceneNode.CreateChildSceneNode(name);
			node.AttachObject (sceneObject);
			NotifySceneObject(sceneObject);
		}

		public PhysicalObject (SceneManager sceneManager, Entity[] entities, string name, string surface):base(sceneManager)
		{
			this.name = name;
			this.surface = surface;
	
			sceneObject = entities[0];
			node = sceneMgr.RootSceneNode.CreateChildSceneNode(name);
			for (int i = 0; i < entities.Length; i++)
			{
				SceneNode n = node.CreateChildSceneNode(name + "_" + i.ToString());
				n.AttachObject (entities[i]);
			}
			NotifySceneObject(entities[0]);
		}

		public string Name
		{
			get { return name; }
		}

		public string Surface
		{
			get { return surface; }
		}

		public virtual PhysicalType Type
		{
			get { return PhysicalType.UNSPECIFIED; }
		}


	}
}