using System;
using System.Drawing;
using System.Windows.Forms;
using Axiom.Core;
using Axiom.MathLib;
using Axiom.SubSystems.Rendering;

namespace Axiom.Utility
{
	/// <summary>
	/// Summary description for DefaultForm.
	/// </summary>
	public class DefaultForm : System.Windows.Forms.Form 
	{
		private System.Windows.Forms.PictureBox pictureBox1;
		private System.ComponentModel.IContainer components;
		private System.Windows.Forms.Timer timer;
		private RenderWindow renderWindow;		
		private Graphics g;
		private string stats;

		public DefaultForm()
		{
			InitializeComponent();

			this.Deactivate += new System.EventHandler(this.DefaultForm_Deactivate);
			this.Activated += new System.EventHandler(this.DefaultForm_Activated);
			this.Closing += new System.ComponentModel.CancelEventHandler(this.DefaultForm_Close);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="source"></param>
		/// <param name="e"></param>
		public void DefaultForm_Deactivate(object source, System.EventArgs e)
		{
			if(renderWindow != null)
				renderWindow.IsActive = false;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="source"></param>
		/// <param name="e"></param>
		public void DefaultForm_Activated(object source, System.EventArgs e)
		{
			if(renderWindow != null)
				renderWindow.IsActive = true;
		}

		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			this.timer = new System.Windows.Forms.Timer(this.components);
			this.SuspendLayout();
			// 
			// pictureBox1
			// 
			this.pictureBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
				| System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.pictureBox1.BackColor = System.Drawing.Color.Black;
			this.pictureBox1.Location = new System.Drawing.Point(0, 0);
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.Size = new System.Drawing.Size(292, 266);
			this.pictureBox1.TabIndex = 0;
			this.pictureBox1.TabStop = false;
			this.pictureBox1.Paint += new System.Windows.Forms.PaintEventHandler(this.pictureBox1_Paint);
			// 
			// timer
			// 
			this.timer.Enabled = true;
			this.timer.Interval = 1;
			this.timer.Tick += new System.EventHandler(this.timer_Tick);
			// 
			// DefaultForm
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.BackColor = System.Drawing.Color.Black;
			this.ClientSize = new System.Drawing.Size(292, 266);
			this.Controls.Add(this.pictureBox1);
			this.Name = "DefaultForm";
			this.Closing += new System.ComponentModel.CancelEventHandler(this.DefaultForm_Closing);
			this.Load += new System.EventHandler(this.DefaultForm_Load);
			this.ResumeLayout(false);

		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="source"></param>
		/// <param name="e"></param>
		public void DefaultForm_Close(object source, System.ComponentModel.CancelEventArgs e)
		{
			// set the window to inactive
			//window.IsActive = false;

			// remove it from the list of render windows, which will halt the rendering loop
			// since there should now be 0 windows left
			Engine.Instance.RenderSystem.RenderWindows.Remove(renderWindow);
		}

		private void timer_Tick(object sender, System.EventArgs e)
		{
			//stats = String.Format("Current FPS: {0} Poly Count: {1}", Engine.Instance.CurrentFPS, Engine.Instance.RenderSystem.FacesRendered);

			//if(g != null)
				//g.DrawString(stats, this.Font, Brushes.Red, 10, 10);
		}

		private void pictureBox1_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
		{
		}

		private void DefaultForm_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			timer.Enabled = false;
		}

		private void DefaultForm_Load(object sender, System.EventArgs e)
		{
			//g = pictureBox1.CreateGraphics();
		}

		/// <summary>
		///		Get/Set the RenderWindow associated with this form.
		/// </summary>
		public RenderWindow RenderWindow
		{
			get { return renderWindow; }
			set { 	renderWindow = value; }
		}

		/// <summary>
		///		
		/// </summary>
		public PictureBox Target
		{
			get { return pictureBox1; }
		}
	}
}
