using System;
using Axiom.Math;

using CollisionModel3D = System.Object;
using Axiom.Core;
namespace Chess.Coldet
{
	/// <summary>
	/// Summary description for CollisionEntity.
	/// </summary>
	public class CollisionEntity
	{
		public CollisionEntity()
		{
			//
			// TODO: Add constructor logic here
			//
		}
		
		public CollisionEntity(Entity ent)
		{}

		public void Delete()
		{
			//mCollisionModel.Delete();
		}
		public void Initialise()
		{

		}
		public void SetWorldTransform()
		{
			Matrix4[] world = new Matrix4[entity.ParentNode.NumWorldTransforms];
			entity.ParentNode.GetWorldTransforms(world);  

			float[] fMatrix = new float[16];

			fMatrix[0] = world[0][0];
			fMatrix[1] = world[1][0];
			fMatrix[2] = world[2][0];
			fMatrix[3] = world[3][0];
			fMatrix[4] = world[0][1];
			fMatrix[5] = world[1][1];
			fMatrix[6] = world[2][1];
			fMatrix[7] = world[3][1];
			fMatrix[8] = world[0][2];
			fMatrix[9] = world[1][2];
			fMatrix[10] = world[2][2];
			fMatrix[11] = world[3][2];
			fMatrix[12] = world[0][3];
			fMatrix[13] = world[1][3];
			fMatrix[14] = world[2][3];
			fMatrix[15] = world[3][3]; 

			//mCollisionModel.setTransform(fMatrix);  
		}
		public CollisionModel3D getCollisionModel()
		{
            return mCollisionModel;
		}

		   
		private CollisionModel3D mCollisionModel;      
		private Entity entity; 
	}
}
