using System;
using System.Windows.Forms;
using Axiom.Graphics;
using System.Drawing;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Windows.Forms.Design;
using System.Drawing.Drawing2D;
using System.Drawing.Design;

namespace MaterialLibraryPlugin {
	/// <summary>
	/// Summary description for MaterialWrapper.
	/// </summary>
	public class MaterialWrapper {
		Material m;
		public MaterialWrapper(Material material) {
			m = material;
		}

		public bool ReceiveShadows {
			// Todo
			get { return true ; } set { ; }
		}
	}

	public class TechniqueWrapper {
		Technique t;
		public TechniqueWrapper(Technique tech) {
			t = tech;
		}

		public int LOD_Index {
			// TODO
			get { return 1; } set { ; }
		}
	}

	public class TextureSelector : System.Drawing.Design.UITypeEditor {
		private TextureBrowser textureList;
		private IWindowsFormsEditorService edSvc = null;

		public TextureSelector() {
			textureList = new TextureBrowser();
		}

		public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value) {
			if (context != null
				&& context.Instance != null
				&& provider != null) {

				edSvc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));

				if (edSvc != null) {
					textureList.SelectPicture(value as string);
					edSvc.ShowDialog(textureList);
					if(textureList.SelectedTexture != null)
						value = textureList.SelectedTexture;
				}
			}
			return value;
		}

		public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) {
			if (context != null && context.Instance != null) {
				return UITypeEditorEditStyle.DropDown;
			}
			return base.GetEditStyle(context);
		}

		private void ValueChanged(object sender, EventArgs e) {
			if (edSvc != null) {
				edSvc.CloseDropDown();
			}
		}
	}
	
	public class TextureUnitStateWrapper {
		TextureUnitState t;
		private bool isEnvMap = false;

		public TextureUnitStateWrapper(TextureUnitState texUnit) {
			t = texUnit;
			isEnvMap = t.EnvironmentMapEnabled;
		}

		[Category("General")]
		[Description("Sets the bias applied to the depth value of this pass. Can be used to make coplanar polygons appear on top of others e.g. for decals. Value must be between 0.., the default being 0. The higher the value, the greater the offset (for if you want to do multiple overlapping decals).")]
		[Editor(typeof(TextureSelector), typeof(UITypeEditor))] 
		public string Texture {
			get { return t.TextureName; }
			set { t.SetTextureName(value); }
		}

		[Category("General")]
		public TextureType TextureType {
			get { return t.TextureType; }
			set { t.SetTextureName(t.TextureName, value); }
		}

		[Category("General")]
		public int TextureCoordinateSet {
			get { return t.TextureCoordSet; }
			set {
				if(value < 0) value = 0;
				t.TextureCoordSet = value;
			}
		}

		[Category("General")]
		public TextureAddressing TextureAddressing {
			get { return t.TextureAddressing; }
			set { t.TextureAddressing = value; }
		}

		[Category("Filtering")]
		public FilterOptions MagFiltering {
			get { return t.GetTextureFiltering(FilterType.Mag);}
			set { t.SetTextureFiltering(t.GetTextureFiltering(FilterType.Min), value, t.GetTextureFiltering(FilterType.Mip)); }
		}

		[Category("Filtering")]
		public FilterOptions MinFiltering {
			get { return t.GetTextureFiltering(FilterType.Min);}
			set { t.SetTextureFiltering(value, t.GetTextureFiltering(FilterType.Mag), t.GetTextureFiltering(FilterType.Mip)); }
		}
	
		[Category("Filtering")]
		public FilterOptions MipFiltering {
			get { return t.GetTextureFiltering(FilterType.Mip);}
			set { t.SetTextureFiltering(t.GetTextureFiltering(FilterType.Min), t.GetTextureFiltering(FilterType.Mag), value); }
		}

		[Category("Filtering")]
		public int MaxAnsiotropy {
			get { return t.TextureAnisotropy; }
			set { t.TextureAnisotropy = value; }
		}

		[Category("Blending")]
		public LayerBlendOperation ColorOperation {
			get { return t.ColorOperation; }
			set { t.SetColorOperation(value); }
		}

		[Category("Alpha Rejection")]
		public byte RejectionValue {
			get { return t.AlphaRejectValue; }
			set { t.SetAlphaRejectSettings(t.AlphaRejectFunction,value); }
		}

		[Category("Alpha Rejection")]
		public CompareFunction RejectMethod {
			get { return t.AlphaRejectFunction; }
			set { t.SetAlphaRejectSettings(value, t.AlphaRejectValue); }
		}

		[Category("Environment Mapping")]
		public bool IsEnvironmentMap {
			get { return this.isEnvMap; }
			set {
				this.isEnvMap = value;
				t.SetEnvironmentMap(value, t.GetEnvironmentMap());
			}
		}

		[Category("Environment Mapping")]
		public EnvironmentMap EnvironmentMapType {
			get { return t.GetEnvironmentMap(); }
			set { t.SetEnvironmentMap(this.isEnvMap, value); }
		}

		[Category("Transformation")]
		public float Offset_U {
			get { return t.TextureScrollU; }
			set { t.TextureScrollU = value; }
		}

		[Category("Transformation")]
		public float Offset_V {
			get { return t.TextureScrollV; }
			set { t.TextureScrollV = value; }
		}

		[Category("Transformation")]
		public float Rotation {
			get { return t.RotationSpeed; }
			set { t.RotationSpeed = value; }
		}

		[Category("Transformation")]
		public float Scale_U {
			get { return t.ScaleU; }
			set { t.ScaleU = value; }
		}

		[Category("Transformation")]
		public float Scale_V {
			get { return t.ScaleV; }
			set { t.ScaleV = value; }
		}

		[Category("Animation")]
		public float Scroll_U {
			get { return t.TextureAnimU; }
			set { t.TextureAnimU = value; }
		}

		[Category("Animation")]
		public float Scroll_V {
			get { return t.TextureAnimV; }
			set { t.TextureAnimV = value; }
		}

		[Category("Animation")]
		public float RotationsPerSecond {
			get { return t.RotationSpeed;}
			set { t.RotationSpeed = value; }
		}
	}
}
