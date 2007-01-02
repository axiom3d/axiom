using System;
using System.Drawing;
using System.Data;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;

namespace MaterialLibraryPlugin
{
	/// <summary>
	/// Summary description for ShaderEditor.
	/// </summary>
	///
	public class ShaderEditor : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.Button button2;
		private DataTable paramsTable;
		private Axiom.Graphics.GpuProgram program;
		private Pass pass;
		private GpuProgramType progType;
		private bool eligibleForProgramChange = false;

		private string[] autoParams = {
										  "ambient_light_colour",
										  "camera_position_object_space",
										  "inverse_world_matrix",
										  "inverse_worldview_matrix",
										  "light_attenuation",
										  "light_diffuse_colour",
										  "light_direction",
										  "light_direction_object_space",
										  "light_position",
										  "light_position_object_space",
										  "light_specular_colour",
										  "projection_matrix",
										  "texture_viewproj_matrix",
										  "time",
										  "view_matrix",
										  "viewproj_matrix",
										  "world_matrix",
										  "world_matrix_array_3x4",
										  "worldview_matrix",
										  "worldviewproj_matrix"
									  };
		private string[] manualParams = {
											"float4",
											"float8",
											"float12",
											"float16",
											"matrix4x4",
											"matrix8x8",
											"matrix12x12",
											"int4",
											"int8",
											"int12",
											"int16"
										};
		private int[] manualParamIndexSizes = {1,2,3,4,4,16,64,1,2,3,4};
		private System.Windows.Forms.PropertyGrid propertyGrid1;
		private System.Windows.Forms.DataGrid dataGrid1;
		private System.Windows.Forms.ComboBox comboBox1;
		private TD.SandBar.ToolBar toolBar1;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public ShaderEditor()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
			paramsTable = new DataTable("Shader Params Table");
			paramsTable.Columns.Add("Param Type");
			paramsTable.Columns.Add("Name/Index");
			this.dataGrid1.DataSource = paramsTable;

			DataGridTableStyle tableStyle = new DataGridTableStyle();
			tableStyle.MappingName = "Shader Params Table";
			string[] columnNames = {"Param Type", "Name/Index", "Data Type", "Data Size"};
			foreach(string s in columnNames) {
				DataGridNoActiveCellColumn dcs = new DataGridNoActiveCellColumn();
				dcs.MappingName = s;
				tableStyle.GridColumnStyles.Add(dcs);
			}

			tableStyle.GridLineStyle = System.Windows.Forms.DataGridLineStyle.None;

			tableStyle.HeaderForeColor = System.Drawing.SystemColors.ControlText;
			tableStyle.RowHeaderWidth = 15;
			tableStyle.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			tableStyle.HeaderForeColor = Color.Black;
			tableStyle.ColumnHeadersVisible = true;

			dataGrid1.PreferredColumnWidth = (dataGrid1.Width-dataGrid1.RowHeaderWidth) / 4;
			dataGrid1.Resize +=new EventHandler(dataGrid1_Resize);
			dataGrid1.ReadOnly = true;
			dataGrid1.TableStyles.Clear();
			dataGrid1.TableStyles.Add(tableStyle);
			comboBox1.SelectedIndexChanged +=new EventHandler(comboBox1_SelectedIndexChanged);
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
			this.label1 = new System.Windows.Forms.Label();
			this.button1 = new System.Windows.Forms.Button();
			this.button2 = new System.Windows.Forms.Button();
			this.propertyGrid1 = new System.Windows.Forms.PropertyGrid();
			this.dataGrid1 = new System.Windows.Forms.DataGrid();
			this.comboBox1 = new System.Windows.Forms.ComboBox();
			this.toolBar1 = new TD.SandBar.ToolBar();
			((System.ComponentModel.ISupportInitialize)(this.dataGrid1)).BeginInit();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(8, 32);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(44, 16);
			this.label1.TabIndex = 3;
			this.label1.Text = "Shader:";
			// 
			// button1
			// 
			this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.button1.Location = new System.Drawing.Point(568, 440);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(96, 24);
			this.button1.TabIndex = 5;
			this.button1.Text = "&OK";
			// 
			// button2
			// 
			this.button2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.button2.Location = new System.Drawing.Point(464, 440);
			this.button2.Name = "button2";
			this.button2.Size = new System.Drawing.Size(96, 24);
			this.button2.TabIndex = 6;
			this.button2.Text = "&Cancel";
			// 
			// propertyGrid1
			// 
			this.propertyGrid1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.propertyGrid1.CommandsVisibleIfAvailable = true;
			this.propertyGrid1.LargeButtons = false;
			this.propertyGrid1.LineColor = System.Drawing.SystemColors.ScrollBar;
			this.propertyGrid1.Location = new System.Drawing.Point(440, 56);
			this.propertyGrid1.Name = "propertyGrid1";
			this.propertyGrid1.Size = new System.Drawing.Size(224, 368);
			this.propertyGrid1.TabIndex = 11;
			this.propertyGrid1.Text = "propertyGrid1";
			this.propertyGrid1.ViewBackColor = System.Drawing.SystemColors.Window;
			this.propertyGrid1.ViewForeColor = System.Drawing.SystemColors.WindowText;
			// 
			// dataGrid1
			// 
			this.dataGrid1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
				| System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.dataGrid1.DataMember = "";
			this.dataGrid1.FlatMode = true;
			this.dataGrid1.GridLineStyle = System.Windows.Forms.DataGridLineStyle.None;
			this.dataGrid1.HeaderForeColor = System.Drawing.SystemColors.ControlText;
			this.dataGrid1.Location = new System.Drawing.Point(8, 56);
			this.dataGrid1.Name = "dataGrid1";
			this.dataGrid1.ParentRowsVisible = false;
			this.dataGrid1.PreferredColumnWidth = 104;
			this.dataGrid1.ReadOnly = true;
			this.dataGrid1.RowHeaderWidth = 15;
			this.dataGrid1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			this.dataGrid1.Size = new System.Drawing.Size(432, 368);
			this.dataGrid1.TabIndex = 12;
			// 
			// comboBox1
			// 
			this.comboBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.comboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBox1.Location = new System.Drawing.Point(56, 32);
			this.comboBox1.Name = "comboBox1";
			this.comboBox1.Size = new System.Drawing.Size(608, 21);
			this.comboBox1.TabIndex = 13;
			// 
			// toolBar1
			// 
			this.toolBar1.Guid = new System.Guid("9cde25a5-d4a6-4ca5-9b94-6d99c9950293");
			this.toolBar1.IsOpen = true;
			this.toolBar1.Location = new System.Drawing.Point(0, 0);
			this.toolBar1.Name = "toolBar1";
			this.toolBar1.Size = new System.Drawing.Size(672, 23);
			this.toolBar1.TabIndex = 14;
			this.toolBar1.Text = "toolBar1";
			// 
			// ShaderEditor
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(672, 475);
			this.Controls.Add(this.comboBox1);
			this.Controls.Add(this.dataGrid1);
			this.Controls.Add(this.propertyGrid1);
			this.Controls.Add(this.button2);
			this.Controls.Add(this.button1);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.toolBar1);
			this.Name = "ShaderEditor";
			this.ShowInTaskbar = false;
			this.Text = "Shader Parameter Editor";
			((System.ComponentModel.ISupportInitialize)(this.dataGrid1)).EndInit();
			this.ResumeLayout(false);

		}
		#endregion

		public string GpuProgramName {
			get { return program.Name; }
			set {
				program = GpuProgramManager.Instance.GetByName(value);
			}
		}

		public Pass Pass {
			set {
				pass = value;
				GpuProgramParameters p;
				if(progType == GpuProgramType.Vertex)
					 p = pass.VertexProgramParameters;
				else
					p = pass.FragmentProgramParameters;
				SetParameters(p);
			}
		}

		public void SetProgramType(GpuProgramType type) {
			progType = type;
		}

		public void SetProgram(string name) {
			string[] sl = HighLevelGpuProgramManager.Instance.ProgramNames;
			this.comboBox1.Items.Clear();
			foreach(string s in sl) {
				GpuProgram prog = GpuProgramManager.Instance.GetByName(s);
				if(prog.Type == progType) {
					this.comboBox1.Items.Add(s);
				}
			}
			eligibleForProgramChange = false;
			int selIndex = comboBox1.FindString(name);
			this.comboBox1.SelectedItem = comboBox1.Items[selIndex];
			eligibleForProgramChange = true;
		}

		public void SetParameters(GpuProgramParameters p) {
			paramsTable.Rows.Clear();
			ArrayList paramInfo = p.ParameterInfo;
			int lastIndex = 0;

			if(paramInfo.Count == 0) {
                return;}

			foreach(GpuProgramParameters.ParameterEntry pe in paramInfo) {
				DataRow row = paramsTable.NewRow();
                int li = -1; //p.GetIndexByName( pe.ParameterName );
				if(li != -1) {
					row["Name/Index"] = pe.ParameterName + " (" + (lastIndex - li).ToString() + ")";
					//row["Data Size"] = (lastIndex - li).ToString();
					lastIndex = li;
				} else {
					row["Name/Index"] = pe.ParameterName + " (???)";
					//row["Data Size"] = "???";
				}
				// row["Data Type"] = CgConsts.GetTypeForIndex(typeIndex, typeSubIndex).ToString();
				// row["Data Size"] = size.ToString();

				if(pe.ParameterType == GpuProgramParameterType.Indexed) {
					row["Param Type"] = "Indexed";
				} else if(pe.ParameterType == GpuProgramParameterType.IndexedAuto) {
					row["Param Type"] = "Indexed Auto";
				} else if(pe.ParameterType == GpuProgramParameterType.Named) {
					row["Param Type"] = "Named";
				} else if(pe.ParameterType == GpuProgramParameterType.NamedAuto) {
					row["Param Type"] = "Named Auto";
				}
				paramsTable.Rows.Add(row);
				paramsTable.AcceptChanges();
			}
		}

		private void dataGrid1_Resize(object sender, EventArgs e) {
			dataGrid1.PreferredColumnWidth = (dataGrid1.Width-dataGrid1.RowHeaderWidth) / 4;
		}

		private void comboBox1_SelectedIndexChanged(object sender, EventArgs e) {
			if(!eligibleForProgramChange) return;
			GpuProgram p = GpuProgramManager.Instance.GetByName((sender as ComboBox).SelectedItem as string);
			GpuProgramParameters param = p.DefaultParameters;
			this.SetProgramType(p.Type);
			eligibleForProgramChange = false;
			this.SetProgram((sender as ComboBox).SelectedItem as string);
			this.SetParameters(param);
			eligibleForProgramChange = true;
		}
	}

	// From http://www.syncfusion.com/faq/winforms/search/856.asp
	public class DataGridNoActiveCellColumn : DataGridTextBoxColumn {
		private int SelectedRow = -1;
		protected override void Edit(System.Windows.Forms.CurrencyManager source, int rowNum, System.Drawing.Rectangle bounds, bool readOnly,string instantText,bool cellIsVisible) {
			//make sure previous selection is valid
			if(SelectedRow > -1 && SelectedRow < source.List.Count + 1)
				this.DataGridTableStyle.DataGrid.UnSelect(SelectedRow);
			SelectedRow = rowNum;
			this.DataGridTableStyle.DataGrid.Select(SelectedRow);
		}
	}
}
