using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace Demos {
    public class DemoPreview : Form {
        private PictureBox pictureBox;
        private Container components = null;

        private DemoPreview() {
        }

        public DemoPreview(string title, string imageFile) {
            InitializeComponent();
            this.Text = title;
            pictureBox.Image = new Bitmap(imageFile);
            if(SystemInformation.WorkingArea.Size.Width > pictureBox.Image.Size.Width &&
                SystemInformation.WorkingArea.Size.Height > pictureBox.Image.Size.Height) {
                this.ClientSize = pictureBox.Image.Size;
            }
            else {
                this.ClientSize = SystemInformation.WorkingArea.Size;
            }
        }

        protected override void Dispose(bool disposing) {
            if(disposing) {
                if(components != null) {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.pictureBox = new System.Windows.Forms.PictureBox();
            this.SuspendLayout();
            // 
            // pictureBox
            // 
            this.pictureBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pictureBox.Location = new System.Drawing.Point(0, 0);
            this.pictureBox.Name = "pictureBox";
            this.pictureBox.Size = new System.Drawing.Size(292, 266);
            this.pictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox.TabIndex = 0;
            this.pictureBox.TabStop = false;
            // 
            // DemoPreview
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(292, 266);
            this.Controls.Add(this.pictureBox);
            this.Name = "DemoPreview";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "DemoPreview";
            this.ResumeLayout(false);

        }
        #endregion
    }
}
