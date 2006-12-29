using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

namespace Chronos.Core
{

	/// <summary>
	/// Summary description for PropertyEditorForm.
	/// </summary>
	
	public class PropertyEditorForm : System.Windows.Forms.Form
	{
		/*
		#region Singleton implementation

		protected static PropertyEditorForm instance;

		public static PropertyEditorForm Instance 
		{
			get 
			{ 
				return instance; 
			}
		}

		public static void Init() 
		{
			if (instance != null) 
			{
				throw new ApplicationException("PropertyEditorForm.Instance is null!");
			}
			instance = new PropertyEditorForm();
			Axiom.Core.GarbageManager.Instance.Add(instance);
		}

		new public void Dispose() 
		{
			if (instance == this) 
			{
				instance = null;
			}
		}
		
		#endregion
		*/

		private PropertyGrid propertyGrid1;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public PropertyGrid PropertyEditor
		{
			get { return propertyGrid1; }
		}

		public PropertyEditorForm()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(PropertyEditorForm));
			this.propertyGrid1 = new System.Windows.Forms.PropertyGrid();
			this.SuspendLayout();
			// 
			// propertyGrid1
			// 
			this.propertyGrid1.CommandsVisibleIfAvailable = true;
			this.propertyGrid1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.propertyGrid1.LargeButtons = false;
			this.propertyGrid1.LineColor = System.Drawing.SystemColors.ScrollBar;
			this.propertyGrid1.Location = new System.Drawing.Point(0, 0);
			this.propertyGrid1.Name = "propertyGrid1";
			this.propertyGrid1.Size = new System.Drawing.Size(288, 342);
			this.propertyGrid1.TabIndex = 0;
			this.propertyGrid1.Text = "propertyGrid1";
			this.propertyGrid1.ViewBackColor = System.Drawing.SystemColors.Window;
			this.propertyGrid1.ViewForeColor = System.Drawing.SystemColors.WindowText;
			// 
			// PropertyEditorForm
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(288, 342);
			this.Controls.Add(this.propertyGrid1);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "PropertyEditorForm";
			this.Text = "Property Editor";
			this.ResumeLayout(false);

		}
		#endregion
	}
}
