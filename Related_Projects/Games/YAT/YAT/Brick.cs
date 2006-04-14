using System;

using Axiom;

using Axiom.MathLib;


namespace YAT 
{
	public class Brick
	{
		public enum DataMasks 
		{
			ColorMask = 0x0F,
			HighlightBit = 0x08,
			EdgeMask = 0xF0,
			TopEdge = 0x10,
			LeftEdge = 0x20,
			BottomEdge = 0x40,
			RightEdge = 0x80
		};


		public static string[] BrickNames = new string[]
			   {"brick0000.mesh", "brick0001.mesh", "brick0010.mesh", "brick0011.mesh",
				   "brick0100.mesh", "brick0101.mesh", "brick0110.mesh", "brick0111.mesh",
				   "brick1000.mesh", "brick1001.mesh", "brick1010.mesh", "brick1011.mesh",
				   "brick1100.mesh", "brick1101.mesh", "brick1110.mesh", "brick1111.mesh"};

		protected SceneNode sceneNode;
		protected Entity entity;
		protected byte data;
		protected bool needsUpdate;

		public Brick()
		{
			data = byte.MinValue;
			needsUpdate = false;
		}

		public void Create(string name, SceneNode parent, Vector3 position)
		{
			// Create scene node
			sceneNode = parent.CreateChildSceneNode(name, position);
		}

		public void SetData(byte data)
		{
			if (data != this.data)
			{
				this.data = data;
				needsUpdate = true;
			}
		}


		public byte Data
		{
			get {return data;}
			set
			{
				if (value != this.data)
				{
					this.data = value;
					needsUpdate = true;
				}
			}
		}
		public void SetEdges(byte edges)
		{
			if ((edges & (byte)DataMasks.EdgeMask) != (data & (byte)DataMasks.EdgeMask))
			{
				data = (byte)((edges & (byte)DataMasks.EdgeMask) | (data & (byte)DataMasks.ColorMask));
				needsUpdate = true;
			}
		}
		public void SetColor(byte color)
		{
			if ((color & (byte)DataMasks.ColorMask) != (data & (byte)DataMasks.ColorMask))
			{
				data = (byte)((data & ((byte)DataMasks.EdgeMask)) | (color & (byte)DataMasks.ColorMask));
				needsUpdate = true;
			}
		}
		public void SetHighlight(bool highlight)
		{
			if (highlight != ((data & (byte)DataMasks.HighlightBit) != 0))
			{
				byte rhsByte = (byte)(highlight ? (byte)DataMasks.HighlightBit : byte.MinValue);
				data = (byte)(data & ((~(byte)DataMasks.HighlightBit) | rhsByte));
				needsUpdate = true;
			}
		}

		public void Update()
		{
			if (needsUpdate)
			{
				SceneManager sceneManager = TetrisApplication.Instance.SceneManager;

				// Destroy old entity
				if (entity != null)
				{
					sceneNode.DetachObject(entity);
					sceneManager.RemoveEntity(sceneNode.Name);
					entity = null;
				}


				// Create new entity
				if (data!=byte.MinValue)
				{
					entity = sceneManager.CreateEntity(sceneNode.Name, BrickNames[(data & (byte)DataMasks.EdgeMask) >> 4]);
					sceneNode.AttachObject(entity);

					// Update material
					if ((data & (byte)DataMasks.HighlightBit)!=byte.MinValue)
					{
						byte someByte = (byte)(data & (byte)DataMasks.ColorMask & (~(byte)DataMasks.HighlightBit));
						entity.MaterialName = ("BrickHighlight" + someByte.ToString());
					}
					else
					{
						byte someByte= (byte)(data & (byte)DataMasks.ColorMask);
						entity.MaterialName = ("Brick" + someByte.ToString());
					}
				}

				// Clear Update flag
				needsUpdate = false;
			}
		}

		

	}
}