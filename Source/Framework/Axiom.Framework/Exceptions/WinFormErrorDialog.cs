#region MIT/X11 License

//Copyright © 2003-2012 Axiom 3D Rendering Engine Project
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.

#endregion License

using System;
using SWF = System.Windows.Forms;


namespace Axiom.Framework.Exceptions
{
	/// <summary>
	/// 
	/// </summary>
	public class WinFormErrorDialog : SWF.Form, SWF.IMessageFilter, IErrorDialog
	{
		protected SWF.Label lblHeader;
		protected SWF.TextBox txtMsg;
		protected SWF.Label lblFooter;
		protected SWF.Button cmdClose;

		public WinFormErrorDialog()
		{
			SetStyle( SWF.ControlStyles.DoubleBuffer, true );
			InitializeComponent();
		}

		private void InitializeComponent()
		{
			this.lblHeader = new System.Windows.Forms.Label();
			this.txtMsg = new System.Windows.Forms.TextBox();
			this.lblFooter = new System.Windows.Forms.Label();
			this.cmdClose = new System.Windows.Forms.Button();
			SuspendLayout();
			// 
			// lblHeader
			// 
			this.lblHeader.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.lblHeader.Font = new System.Drawing.Font( "Tahoma", 9F, System.Drawing.FontStyle.Regular,
			                                               System.Drawing.GraphicsUnit.Point, ( (byte)( 0 ) ) );
			this.lblHeader.ForeColor = System.Drawing.Color.FromArgb( ( (int)( ( (byte)( 25 ) ) ) ),
			                                                          ( (int)( ( (byte)( 35 ) ) ) ),
			                                                          ( (int)( ( (byte)( 75 ) ) ) ) );
			this.lblHeader.Location = new System.Drawing.Point( 12, 9 );
			this.lblHeader.Name = "lblHeader";
			this.lblHeader.Size = new System.Drawing.Size( 422, 40 );
			this.lblHeader.TabIndex = 9;
			this.lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.lblHeader.Text = global::Axiom.Framework.Properties.Resources.Axiom_Error_Header;
			// 
			// txtMsg
			// 
			this.txtMsg.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.txtMsg.Font = new System.Drawing.Font( "Tahoma", 9F, System.Drawing.FontStyle.Regular,
			                                            System.Drawing.GraphicsUnit.Point, ( (byte)( 0 ) ) );
			this.txtMsg.ForeColor = System.Drawing.Color.FromArgb( ( (int)( ( (byte)( 25 ) ) ) ), ( (int)( ( (byte)( 35 ) ) ) ),
			                                                       ( (int)( ( (byte)( 75 ) ) ) ) );
			this.txtMsg.Location = new System.Drawing.Point( 12, 49 );
			this.txtMsg.Name = "txtMsg";
			this.txtMsg.Size = new System.Drawing.Size( 422, 161 );
			this.txtMsg.TabIndex = 9;
			this.txtMsg.Multiline = true;
			this.txtMsg.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
			this.txtMsg.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.txtMsg.ReadOnly = true;
			// 
			// lblFooter
			// 
			this.lblFooter.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.lblFooter.Font = new System.Drawing.Font( "Tahoma", 9F, System.Drawing.FontStyle.Regular,
			                                               System.Drawing.GraphicsUnit.Point, ( (byte)( 0 ) ) );
			this.lblFooter.ForeColor = System.Drawing.Color.FromArgb( ( (int)( ( (byte)( 25 ) ) ) ),
			                                                          ( (int)( ( (byte)( 35 ) ) ) ),
			                                                          ( (int)( ( (byte)( 75 ) ) ) ) );
			this.lblFooter.Location = new System.Drawing.Point( 12, 220 );
			this.lblFooter.Name = "lblFooter";
			this.lblFooter.Size = new System.Drawing.Size( 422, 86 );
			this.lblFooter.TabIndex = 9;
			this.lblFooter.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.lblFooter.Text = global::Axiom.Framework.Properties.Resources.Axiom_Error_Footer;
			// 
			// cmdClose
			// 
			this.cmdClose.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
			this.cmdClose.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.cmdClose.Location = new System.Drawing.Point( 172, 313 );
			this.cmdClose.Name = "cmdClose";
			this.cmdClose.Size = new System.Drawing.Size( 80, 26 );
			this.cmdClose.TabIndex = 11;
			this.cmdClose.Text = "&Close";
			// 
			// WinFormErrorDialog
			// 
			ClientSize = new System.Drawing.Size( 446, 351 );
			ControlBox = false;
			Controls.Add( this.cmdClose );
			Controls.Add( this.lblHeader );
			Controls.Add( this.lblFooter );
			Controls.Add( this.txtMsg );
			Font = new System.Drawing.Font( "Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point,
			                                ( (byte)( 0 ) ) );
			FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			MaximizeBox = false;
			MinimizeBox = false;
			Name = "WinFormErrorDialog";
			StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			Text = global::Axiom.Framework.Properties.Resources.Axiom_Error_Title;
			ResumeLayout( false );
		}

		#region Implementation of IMessageFilter

		/// <summary>
		/// Filters out a message before it is dispatched.
		/// </summary>
		/// <returns>
		/// true to filter the message and stop it from being dispatched; false to allow the message to continue to the next filter or control.
		/// </returns>
		/// <param name="msg">The message to be dispatched. You cannot modify this message.</param>
		/// <filterpriority>1</filterpriority>
		public bool PreFilterMessage( ref SWF.Message msg )
		{
			const int WM_KEYDOWN = 0x100;

			SWF.Keys keyCode = (SWF.Keys)(int)msg.WParam & SWF.Keys.KeyCode;
			if ( ( msg.Msg == WM_KEYDOWN && keyCode == SWF.Keys.Return ) ||
			     ( msg.Msg == WM_KEYDOWN && keyCode == SWF.Keys.Escape ) )
			{
				Close();
				return true;
			}
			return false;
		}

		#endregion

		#region Implementation of IErrorDialog

		/// <summary>
		/// Causes the exception to be displayed on the screen
		/// </summary>
		/// <param name="exception">The exception to display</param>
		public void Show( Exception exception )
		{
			this.txtMsg.Text = exception.Message + Environment.NewLine + exception.StackTrace;
			this.cmdClose.Select();
			ShowDialog();
		}

		#endregion
	}
}