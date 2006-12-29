using System;
using System.Collections;
using System.Drawing;
using System.ComponentModel;
using System.Globalization;
using System.Drawing.Design;
using System.Windows.Forms;
using Axiom.Core;
using Axiom.Graphics;
using MaterialLibraryPlugin;
using Chronos.Core;
using TD.SandBar;

namespace MeshPlugin
{
	/// <summary>
	/// Summary description for MeshWrapper.
	/// </summary>

	[PropertyTab(typeof(SubEntityPropertyTab), PropertyTabScope.Document)]
	public class MeshWrapper : IPropertiesWrapper
	{
		private Entity e;
		private ArrayList entityList;

		public MeshWrapper(Entity entity)
		{
			e = entity;
			entityList = new ArrayList();
			for(int i=0; i<e.SubEntityCount; i++) 
			{
				entityList.Add(e.GetSubEntity(i));
			}
			MaterialLibraryPlugin.MaterialLibraryPlugin.Instance.MaterialChanged += new MaterialLibraryPlugin.MaterialLibraryPlugin.ApplyMaterialDelegate(MaterialLibrary_ApplyMaterial);
		}

		[Category("Display")]
		public bool CastShadows 
		{
			get { return e.CastShadows; }
			set { e.CastShadows = value; }
		}

		[Category("Display")]
		public bool IsVisible 
		{
			get { return e.IsVisible; }
			set { e.IsVisible = value; }
		}

		[Category("Display")]
		public string Material 
		{
			get { return "To Implement"; }
			// TODO: Set
		}

		[Category("General")]
		public string Name 
		{
			get { return e.Name; }
			set { e.Name = value; }
		}

		[Category("Sub materials")]
		//[TypeConverter(typeof(ExpandableObjectConverter))]
		public ArrayList SubEntites
		{
			get { return entityList; }
			// TODO: Set
		}

		[Category("Animation")]
		public bool HasSkeleton 
		{
			get { return e.HasSkeleton; }
		}

		[Category("Animation")]
		public bool DisplaySkeleton 
		{
			get { return e.HasSkeleton ? e.DisplaySkeleton: false; }
			set { e.DisplaySkeleton = e.HasSkeleton ? value : false; }
		}

		private void MaterialLibrary_ApplyMaterial(object sender, Axiom.Graphics.Material material) {
			this.e.MaterialName = material.Name;
		}

		#region IDisposable Members

		public void Dispose() {
			MaterialLibraryPlugin.MaterialLibraryPlugin.Instance.MaterialChanged -= new MaterialLibraryPlugin.MaterialLibraryPlugin.ApplyMaterialDelegate(MaterialLibrary_ApplyMaterial);
		}

		#endregion

		public TD.SandBar.ToolBar GetContextualToolBar() {
			return null;
		}
	}
}
