namespace Axiom.Samples.MulitpleRenderWindows
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose( bool disposing )
        {
            if ( disposing && ( components != null ) )
            {
                components.Dispose();
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
            this.viewportOne = new System.Windows.Forms.PictureBox();
            this.viewportTwo = new System.Windows.Forms.PictureBox();
            ( (System.ComponentModel.ISupportInitialize)( this.viewportOne ) ).BeginInit();
            ( (System.ComponentModel.ISupportInitialize)( this.viewportTwo ) ).BeginInit();
            this.SuspendLayout();
            // 
            // viewportOne
            // 
            this.viewportOne.Location = new System.Drawing.Point( 3, 12 );
            this.viewportOne.Name = "viewportOne";
            this.viewportOne.Size = new System.Drawing.Size( 594, 625 );
            this.viewportOne.TabIndex = 0;
            this.viewportOne.TabStop = false;
            // 
            // viewportTwo
            // 
            this.viewportTwo.Location = new System.Drawing.Point( 603, 12 );
            this.viewportTwo.Name = "viewportTwo";
            this.viewportTwo.Size = new System.Drawing.Size( 422, 378 );
            this.viewportTwo.TabIndex = 1;
            this.viewportTwo.TabStop = false;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size( 1037, 700 );
            this.Controls.Add( this.viewportTwo );
            this.Controls.Add( this.viewportOne );
            this.Name = "Form1";
            this.Text = "Form1";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler( this.Form1_FormClosing );
            this.Load += new System.EventHandler( this.Form1_Load );
            ( (System.ComponentModel.ISupportInitialize)( this.viewportOne ) ).EndInit();
            ( (System.ComponentModel.ISupportInitialize)( this.viewportTwo ) ).EndInit();
            this.ResumeLayout( false );

        }

        #endregion

        private System.Windows.Forms.PictureBox viewportOne;
        private System.Windows.Forms.PictureBox viewportTwo;

    }
}

