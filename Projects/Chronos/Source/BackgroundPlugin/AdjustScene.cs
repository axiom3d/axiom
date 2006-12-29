#region LGPL License
/*
Chronos World Editor
Copyright (C) 2004 Chris "Antiarc" Heald [antiarc@antiarc.net]

This application is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This application is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
*/
#endregion

using System;
using System.Drawing;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Forms;
using System.IO;
using Axiom.Core;
using Axiom.Math;
using Axiom.Math.Collections;
using Axiom.Graphics;

namespace BackgroundPlugin
{
	/// <summary>
	/// Summary description for AdjustScene.
	/// </summary>
	public class AdjustScene : Form
	{
		private System.Windows.Forms.ColorDialog colorDialog1;
		private System.Windows.Forms.TabControl tabControl1;
		private System.Windows.Forms.TabPage tabPage1;
		private System.Windows.Forms.TabPage tabPage2;
		private System.Windows.Forms.TreeView skyMaterialTree;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.Label label9;
		private System.Windows.Forms.Label label10;
		private System.Windows.Forms.Label label11;
		private System.Windows.Forms.GroupBox groupBox3;
		private System.Windows.Forms.GroupBox groupBox4;
		private System.Windows.Forms.ComboBox comboBox1;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.NumericUpDown skyTile;
		private System.Windows.Forms.NumericUpDown skyBow;
		private System.Windows.Forms.NumericUpDown skyScale;
		private System.Windows.Forms.NumericUpDown skyDistance;
		private System.Windows.Forms.PropertyGrid propertyGrid1;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public AdjustScene()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
			propertyGrid1.SelectedObject = new LightFogWrapper(Root.Instance.SceneManager);
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
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(AdjustScene));
			this.colorDialog1 = new System.Windows.Forms.ColorDialog();
			this.tabControl1 = new System.Windows.Forms.TabControl();
			this.tabPage1 = new System.Windows.Forms.TabPage();
			this.propertyGrid1 = new System.Windows.Forms.PropertyGrid();
			this.tabPage2 = new System.Windows.Forms.TabPage();
			this.groupBox4 = new System.Windows.Forms.GroupBox();
			this.skyMaterialTree = new System.Windows.Forms.TreeView();
			this.groupBox3 = new System.Windows.Forms.GroupBox();
			this.label8 = new System.Windows.Forms.Label();
			this.comboBox1 = new System.Windows.Forms.ComboBox();
			this.label11 = new System.Windows.Forms.Label();
			this.skyTile = new System.Windows.Forms.NumericUpDown();
			this.label10 = new System.Windows.Forms.Label();
			this.skyBow = new System.Windows.Forms.NumericUpDown();
			this.label9 = new System.Windows.Forms.Label();
			this.skyScale = new System.Windows.Forms.NumericUpDown();
			this.label7 = new System.Windows.Forms.Label();
			this.skyDistance = new System.Windows.Forms.NumericUpDown();
			this.tabControl1.SuspendLayout();
			this.tabPage1.SuspendLayout();
			this.tabPage2.SuspendLayout();
			this.groupBox4.SuspendLayout();
			this.groupBox3.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.skyTile)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.skyBow)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.skyScale)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.skyDistance)).BeginInit();
			this.SuspendLayout();
			// 
			// colorDialog1
			// 
			this.colorDialog1.AnyColor = true;
			// 
			// tabControl1
			// 
			this.tabControl1.Appearance = System.Windows.Forms.TabAppearance.FlatButtons;
			this.tabControl1.Controls.Add(this.tabPage1);
			this.tabControl1.Controls.Add(this.tabPage2);
			this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tabControl1.HotTrack = true;
			this.tabControl1.Location = new System.Drawing.Point(0, 0);
			this.tabControl1.Name = "tabControl1";
			this.tabControl1.SelectedIndex = 0;
			this.tabControl1.Size = new System.Drawing.Size(274, 456);
			this.tabControl1.TabIndex = 36;
			// 
			// tabPage1
			// 
			this.tabPage1.Controls.Add(this.propertyGrid1);
			this.tabPage1.Location = new System.Drawing.Point(4, 25);
			this.tabPage1.Name = "tabPage1";
			this.tabPage1.Size = new System.Drawing.Size(266, 427);
			this.tabPage1.TabIndex = 0;
			this.tabPage1.Text = "Light and Fog";
			// 
			// propertyGrid1
			// 
			this.propertyGrid1.CommandsVisibleIfAvailable = true;
			this.propertyGrid1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.propertyGrid1.HelpVisible = false;
			this.propertyGrid1.LargeButtons = false;
			this.propertyGrid1.LineColor = System.Drawing.SystemColors.ScrollBar;
			this.propertyGrid1.Location = new System.Drawing.Point(0, 0);
			this.propertyGrid1.Name = "propertyGrid1";
			this.propertyGrid1.Size = new System.Drawing.Size(266, 427);
			this.propertyGrid1.TabIndex = 36;
			this.propertyGrid1.Text = "propertyGrid1";
			this.propertyGrid1.ToolbarVisible = false;
			this.propertyGrid1.ViewBackColor = System.Drawing.SystemColors.Window;
			this.propertyGrid1.ViewForeColor = System.Drawing.SystemColors.WindowText;
			// 
			// tabPage2
			// 
			this.tabPage2.Controls.Add(this.groupBox4);
			this.tabPage2.Controls.Add(this.groupBox3);
			this.tabPage2.Location = new System.Drawing.Point(4, 25);
			this.tabPage2.Name = "tabPage2";
			this.tabPage2.Size = new System.Drawing.Size(266, 427);
			this.tabPage2.TabIndex = 1;
			this.tabPage2.Text = "Sky";
			// 
			// groupBox4
			// 
			this.groupBox4.Controls.Add(this.skyMaterialTree);
			this.groupBox4.Dock = System.Windows.Forms.DockStyle.Fill;
			this.groupBox4.Location = new System.Drawing.Point(0, 160);
			this.groupBox4.Name = "groupBox4";
			this.groupBox4.Size = new System.Drawing.Size(266, 267);
			this.groupBox4.TabIndex = 14;
			this.groupBox4.TabStop = false;
			this.groupBox4.Text = "Material";
			// 
			// skyMaterialTree
			// 
			this.skyMaterialTree.Dock = System.Windows.Forms.DockStyle.Fill;
			this.skyMaterialTree.HideSelection = false;
			this.skyMaterialTree.ImageIndex = -1;
			this.skyMaterialTree.Location = new System.Drawing.Point(3, 16);
			this.skyMaterialTree.Name = "skyMaterialTree";
			this.skyMaterialTree.SelectedImageIndex = -1;
			this.skyMaterialTree.Size = new System.Drawing.Size(260, 248);
			this.skyMaterialTree.Sorted = true;
			this.skyMaterialTree.TabIndex = 2;
			this.skyMaterialTree.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.skyMaterialTree_AfterSelect);
			// 
			// groupBox3
			// 
			this.groupBox3.Controls.Add(this.label8);
			this.groupBox3.Controls.Add(this.comboBox1);
			this.groupBox3.Controls.Add(this.label11);
			this.groupBox3.Controls.Add(this.skyTile);
			this.groupBox3.Controls.Add(this.label10);
			this.groupBox3.Controls.Add(this.skyBow);
			this.groupBox3.Controls.Add(this.label9);
			this.groupBox3.Controls.Add(this.skyScale);
			this.groupBox3.Controls.Add(this.label7);
			this.groupBox3.Controls.Add(this.skyDistance);
			this.groupBox3.Dock = System.Windows.Forms.DockStyle.Top;
			this.groupBox3.Location = new System.Drawing.Point(0, 0);
			this.groupBox3.Name = "groupBox3";
			this.groupBox3.Size = new System.Drawing.Size(266, 160);
			this.groupBox3.TabIndex = 13;
			this.groupBox3.TabStop = false;
			this.groupBox3.Text = "Parameters";
			// 
			// label8
			// 
			this.label8.AutoSize = true;
			this.label8.Location = new System.Drawing.Point(32, 16);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(26, 16);
			this.label8.TabIndex = 14;
			this.label8.Text = "Sky:";
			// 
			// comboBox1
			// 
			this.comboBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.comboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBox1.Items.AddRange(new object[] {
														   "None",
														   "Skybox",
														   "Skyplane",
														   "Skydome"});
			this.comboBox1.Location = new System.Drawing.Point(64, 16);
			this.comboBox1.Name = "comboBox1";
			this.comboBox1.Size = new System.Drawing.Size(194, 20);
			this.comboBox1.TabIndex = 13;
			this.comboBox1.SelectedIndexChanged += new System.EventHandler(this.comboBox1_SelectedIndexChanged);
			// 
			// label11
			// 
			this.label11.AutoSize = true;
			this.label11.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.label11.Location = new System.Drawing.Point(32, 96);
			this.label11.Name = "label11";
			this.label11.Size = new System.Drawing.Size(23, 16);
			this.label11.TabIndex = 12;
			this.label11.Text = "Tile";
			// 
			// skyTile
			// 
			this.skyTile.Enabled = false;
			this.skyTile.Location = new System.Drawing.Point(64, 96);
			this.skyTile.Maximum = new System.Decimal(new int[] {
																	100000000,
																	0,
																	0,
																	0});
			this.skyTile.Minimum = new System.Decimal(new int[] {
																	1,
																	0,
																	0,
																	0});
			this.skyTile.Name = "skyTile";
			this.skyTile.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
			this.skyTile.Size = new System.Drawing.Size(72, 20);
			this.skyTile.TabIndex = 11;
			this.skyTile.Value = new System.Decimal(new int[] {
																  1,
																  0,
																  0,
																  0});
			this.skyTile.ValueChanged += new System.EventHandler(this.skyPlaneValues_Changed);
			// 
			// label10
			// 
			this.label10.AutoSize = true;
			this.label10.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.label10.Location = new System.Drawing.Point(24, 120);
			this.label10.Name = "label10";
			this.label10.Size = new System.Drawing.Size(34, 16);
			this.label10.TabIndex = 10;
			this.label10.Text = "Curve";
			// 
			// skyBow
			// 
			this.skyBow.DecimalPlaces = 3;
			this.skyBow.Enabled = false;
			this.skyBow.Increment = new System.Decimal(new int[] {
																	 1,
																	 0,
																	 0,
																	 65536});
			this.skyBow.Location = new System.Drawing.Point(64, 120);
			this.skyBow.Maximum = new System.Decimal(new int[] {
																   100000000,
																   0,
																   0,
																   0});
			this.skyBow.Name = "skyBow";
			this.skyBow.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
			this.skyBow.Size = new System.Drawing.Size(72, 20);
			this.skyBow.TabIndex = 9;
			this.skyBow.ValueChanged += new System.EventHandler(this.skyPlaneValues_Changed);
			// 
			// label9
			// 
			this.label9.AutoSize = true;
			this.label9.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.label9.Location = new System.Drawing.Point(24, 72);
			this.label9.Name = "label9";
			this.label9.Size = new System.Drawing.Size(33, 16);
			this.label9.TabIndex = 8;
			this.label9.Text = "Scale";
			// 
			// skyScale
			// 
			this.skyScale.DecimalPlaces = 2;
			this.skyScale.Enabled = false;
			this.skyScale.Increment = new System.Decimal(new int[] {
																	   50,
																	   0,
																	   0,
																	   0});
			this.skyScale.Location = new System.Drawing.Point(64, 72);
			this.skyScale.Maximum = new System.Decimal(new int[] {
																	 100000000,
																	 0,
																	 0,
																	 0});
			this.skyScale.Minimum = new System.Decimal(new int[] {
																	 1,
																	 0,
																	 0,
																	 0});
			this.skyScale.Name = "skyScale";
			this.skyScale.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
			this.skyScale.Size = new System.Drawing.Size(72, 20);
			this.skyScale.TabIndex = 7;
			this.skyScale.Value = new System.Decimal(new int[] {
																   1,
																   0,
																   0,
																   0});
			this.skyScale.ValueChanged += new System.EventHandler(this.skyPlaneValues_Changed);
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.label7.Location = new System.Drawing.Point(8, 48);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(48, 16);
			this.label7.TabIndex = 6;
			this.label7.Text = "Distance";
			// 
			// skyDistance
			// 
			this.skyDistance.Enabled = false;
			this.skyDistance.Increment = new System.Decimal(new int[] {
																		  50,
																		  0,
																		  0,
																		  0});
			this.skyDistance.Location = new System.Drawing.Point(64, 48);
			this.skyDistance.Maximum = new System.Decimal(new int[] {
																		100000000,
																		0,
																		0,
																		0});
			this.skyDistance.Minimum = new System.Decimal(new int[] {
																		50,
																		0,
																		0,
																		0});
			this.skyDistance.Name = "skyDistance";
			this.skyDistance.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
			this.skyDistance.Size = new System.Drawing.Size(72, 20);
			this.skyDistance.TabIndex = 5;
			this.skyDistance.Value = new System.Decimal(new int[] {
																	  50,
																	  0,
																	  0,
																	  0});
			this.skyDistance.ValueChanged += new System.EventHandler(this.skyPlaneValues_Changed);
			// 
			// AdjustScene
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.BackColor = System.Drawing.SystemColors.Control;
			this.ClientSize = new System.Drawing.Size(274, 456);
			this.ControlBox = false;
			this.Controls.Add(this.tabControl1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "AdjustScene";
			this.Text = "Light and Sky";
			this.Closing += new System.ComponentModel.CancelEventHandler(this.AdjustScene_Closing);
			this.Load += new System.EventHandler(this.AdjustScene_Load);
			this.tabControl1.ResumeLayout(false);
			this.tabPage1.ResumeLayout(false);
			this.tabPage2.ResumeLayout(false);
			this.groupBox4.ResumeLayout(false);
			this.groupBox3.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.skyTile)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.skyBow)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.skyScale)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.skyDistance)).EndInit();
			this.ResumeLayout(false);

		}
		#endregion

		private void AdjustScene_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			e.Cancel = true;
			this.Visible = false;
		}

		private void AdjustScene_Load(object sender, System.EventArgs e)
		{
			StringCollection files = ResourceManager.GetAllCommonNamesLike("", "*.material");
			foreach(string file in files) 
			{
				Stream s = MaterialManager.Instance.FindResourceData(file);
				TreeNode n = new TreeNode(file);
				Chronos.Core.Utils.ParseMaterial(s, n, false);
				skyMaterialTree.Nodes.Add(n);
				s.Close();
			}
		}

		private void setSky() 
		{
			SceneManager s = Root.Instance.SceneManager;
			clearSky(s);
			if(comboBox1.SelectedIndex == 2) 
			{
				if(skyMaterialTree.SelectedNode == null) return;
				if(skyMaterialTree.SelectedNode.Nodes.Count > 0) return; 
				Plane p = new Plane();
				p.D = (float)this.skyDistance.Value;
				p.Normal = -Vector3.UnitY;

				try 
				{
					s.SetSkyPlane(true, p, skyMaterialTree.SelectedNode.Text, (float)this.skyScale.Value, (float)this.skyTile.Value, true, (float)this.skyBow.Value);
				} 
				catch 
				{
					/*Plane plane = new Plane();
					plane.D = 0;
					plane.Normal = -Vector3.UnitY;
					s.SetSkyPlane(true, plane, "BaseWhite");*/
				}
			} 
			else if(comboBox1.SelectedIndex == 1) 
			{
				if(skyMaterialTree.SelectedNode == null) return;
				if(skyMaterialTree.SelectedNode.Nodes.Count > 0) return; 
				try 
				{
					Axiom.Graphics.Material m = Axiom.Graphics.MaterialManager.Instance.GetByName(skyMaterialTree.SelectedNode.Text);
					if(!m.GetTechnique(0).GetPass(0).GetTextureUnitState(0).IsCubic) return;
					s.SetSkyBox(true, skyMaterialTree.SelectedNode.Text, (float)this.skyDistance.Value);
				} 
				catch 
				{
					/*Plane plane = new Plane();
					plane.D = 0;
					plane.Normal = -Vector3.UnitY;
					s.SetSkyPlane(true, plane, "BaseWhite");*/
				}
			}
			else if(comboBox1.SelectedIndex == 1) 
			{
				if(skyMaterialTree.SelectedNode == null) return;
				if(skyMaterialTree.SelectedNode.Nodes.Count > 0) return; 
				//try 
				//{
					s.SetSkyDome(true, skyMaterialTree.SelectedNode.Text, (float)this.skyBow.Value, (float)this.skyTile.Value); //, 5.0f, true, Vector3.UnitZ);
					//s.SetSkyDome(true, "Examples/CloudySky", 5, 8);
				//} 
				//catch {}
			}
			//RenderingWindow.Instance.RenderOneFrame();
		}

		private void clearSky(SceneManager s) 
		{
			try 
			{
				s.SetSkyPlane(false, new Plane(new Vector3(0,-1,0), new Vector3(0,0,0)), "BlankWhite");
				s.SetSkyBox(false, "BlankWhite", 1);
				s.SetSkyDome(false, "BlankWhite", 1, 1);
			} catch {}
		}

		private void comboBox1_SelectedIndexChanged(object sender, System.EventArgs e)
		{

			if(comboBox1.SelectedIndex == 0) 
			{
				this.skyBow.Enabled = false;
				this.skyDistance.Enabled = false;
				this.skyTile.Enabled = false;
				this.skyScale.Enabled = false;
			} 
			else if (comboBox1.SelectedIndex == 1) 
			{
				this.skyBow.Enabled = false;
				this.skyDistance.Enabled = true;
				this.skyTile.Enabled = false;
				this.skyScale.Enabled = false;
			}
			else if (comboBox1.SelectedIndex == 2) 
			{
				this.skyBow.Enabled = true;
				this.skyDistance.Enabled = true;
				this.skyTile.Enabled = true;
				this.skyScale.Enabled = true;
			} 
			else 
			{
				this.skyBow.Enabled = false;
				this.skyDistance.Enabled = true;
				this.skyTile.Enabled = true;
				this.skyScale.Enabled = true;
			}
			this.setSky();
		}

		private void skyPlaneValues_Changed(object sender, System.EventArgs e)
		{
			setSky();
		}

		private void skyMaterialTree_AfterSelect(object sender, System.Windows.Forms.TreeViewEventArgs e)
		{
			setSky();
		}

	}
}

