using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using Axiom.Math.Collections;
using Axiom.Collections;
using Axiom.Core;
using Axiom.Animating;
using Chronos.Core;

namespace MeshPlugin
{
	/// <summary>
	/// Summary description for AnimationStates.
	/// </summary>

	public class AnimationStates : System.Windows.Forms.Form
	{
		private ArrayList animationStates;
		private Entity animationEntity;
		private System.Windows.Forms.CheckedListBox checkedListBox1;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public AnimationStates()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
			//Root.Instance.FrameStarted += new FrameEvent(Root_FrameStarted);
			animationStates = new ArrayList();
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
			Chronos.Core.SceneGraph.Instance.SelectedObjectChanged -=new Chronos.Core.SceneGraph.SelectedObjectChangedDelegate(Instance_SelectedObjectChanged);
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.checkedListBox1 = new System.Windows.Forms.CheckedListBox();
			this.SuspendLayout();
			// 
			// checkedListBox1
			// 
			this.checkedListBox1.CheckOnClick = true;
			this.checkedListBox1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.checkedListBox1.Location = new System.Drawing.Point(0, 0);
			this.checkedListBox1.Name = "checkedListBox1";
			this.checkedListBox1.Size = new System.Drawing.Size(376, 319);
			this.checkedListBox1.TabIndex = 0;
			this.checkedListBox1.ThreeDCheckBoxes = true;
			this.checkedListBox1.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.checkedListBox1_ItemCheck);
			// 
			// AnimationStates
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(376, 323);
			this.Controls.Add(this.checkedListBox1);
			this.Name = "AnimationStates";
			this.Text = "Entity Animation States";
			this.Load += new System.EventHandler(this.AnimationStates_Load);
			this.ResumeLayout(false);

		}
		#endregion

		private void AnimationStates_Load(object sender, System.EventArgs e)
		{
			Chronos.Core.SceneGraph.Instance.SelectedObjectChanged +=new Chronos.Core.SceneGraph.SelectedObjectChangedDelegate(Instance_SelectedObjectChanged);
		}

		private void checkedListBox1_ItemCheck(object sender, System.Windows.Forms.ItemCheckEventArgs e)
		{
			AnimationState a = animationEntity.GetAnimationState(checkedListBox1.Items[e.Index].ToString());
			if(e.NewValue==CheckState.Checked) 
			{
				a.IsEnabled = true;
				if(!animationStates.Contains(a))
					animationStates.Add(a);
			} 
			else 
			{
				a.IsEnabled = false;
				animationStates.Remove(a);
			}
		}

		private void Root_FrameStarted(object source, FrameEventArgs e)
		{
			foreach(AnimationState a in animationStates) 
			{
				a.AddTime(e.TimeSinceLastFrame);
			}
		}

		private void Instance_SelectedObjectChanged(object sender, EditorNode node) {
			animationEntity = null;
			MovableObject obj = null;
			checkedListBox1.Items.Clear();
			foreach(MovableObject o in node.AttachedObjects)
				if(o is Entity)
					obj = o as Entity;
			if(obj is Entity) {
				animationEntity = (Entity)obj;
				AnimationStateCollection aniCol = animationEntity.GetAllAnimationStates();
				foreach(Axiom.Animating.AnimationState a in aniCol) {
					checkedListBox1.Items.Add(a.Name);
					checkedListBox1.SetItemChecked(checkedListBox1.Items.Count-1, a.IsEnabled);
				}
			}
		}
	}
}
