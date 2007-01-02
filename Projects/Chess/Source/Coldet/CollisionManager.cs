using System;

using Axiom.Math;
using Axiom.Core;

namespace Chess.Coldet
{
	/// <summary>
	/// Summary description for CollisionManager.
	/// </summary>
	public class CollisionManager
	{
		#region Singleton implementation

		private static CollisionManager instance;
		public CollisionManager()
		{
			if (instance == null) 
			{
				instance = this;
				createCollider();
			}
		}
		public static CollisionManager Instance 
		{
			get 
			{
				return instance;
			}
		}
		#endregion
		public void Delete()
		{
			mCollider.Delete();
		}
		public bool createCollider()
		{
			mCollider = new Collider();
			if(mCollider == null) return false;
			return true;
		}

		public CollisionEntity createEntity(Entity ent)
		{
			CollisionEntity ce = new CollisionEntity(ent);
			ce.Initialise();
			return ce;
		}
		public bool collide(Ray ray, CollisionEntity entity)
		{
			return mCollider.collide(ray, entity);
		}
		   
		private Collider mCollider;
	}
}
