using System;
using System.Windows.Forms;
using Debug = System.Diagnostics.Debug;

namespace Sgry.Ann
{
	public class GotoForm : Form
	{
		public GotoForm()
		{
			InitializeComponent();
			Font = SystemInformation.MenuFont;
		}

		public int LineNumber
		{
			get{ return Int32.Parse(_LineNumTextBox.Text); }
			set{ _LineNumTextBox.Text = value.ToString(); }
		}

		void _LineNumTextBox_Enter( object sender, EventArgs e )
		{
			_LineNumTextBox.SelectAll();
		}

		void _LineNumTextBox_KeyDown(object sender, KeyEventArgs e)
		{
			int lineNum;

			if( e.KeyCode == Keys.Up || e.KeyCode == Keys.Down )
			{
				if( _LineNumTextBox.Text == "" )
				{
					lineNum = 1;
				}
				else if( e.KeyData == Keys.Up )
				{
					lineNum = Math.Min( Int32.Parse(_LineNumTextBox.Text)+1,
										Int32.MaxValue );
				}
				else if( e.KeyData == (Keys.Up | Keys.Shift) )
				{
					lineNum = Math.Min( Int32.Parse(_LineNumTextBox.Text)+10,
										Int32.MaxValue );
				}
				else if( e.KeyData == Keys.Down )
				{
					lineNum = Math.Max( Int32.Parse(_LineNumTextBox.Text)-1,
										1 );
				}
				else// if( e.KeyData == (Keys.Down | Keys.Shift) )
				{
					Debug.Assert( e.KeyData == (Keys.Down | Keys.Shift) );
					lineNum = Math.Max( Int32.Parse(_LineNumTextBox.Text)-10,
										1 );
				}
				_LineNumTextBox.Text = lineNum.ToString();
				_LineNumTextBox.SelectAll();
				e.Handled = true;
			}
		}

		void _LineNumTextBox_KeyPress( object sender, KeyPressEventArgs e )
		{
			if( (e.KeyChar < '0' || '9' < e.KeyChar)
				&& e.KeyChar != '\b' )
				e.Handled = true;
		}

		void _OkButton_Click( object sender, EventArgs e )
		{
			if( MyValidate() == false )
			{
				MessageBox.Show( "Please enter a valid line number." );
				_LineNumTextBox.SelectAll();
				return;
			}

			DialogResult = DialogResult.OK;
		}

		bool MyValidate()
		{
			int lineNum;
			string lineNumStr = _LineNumTextBox.Text;

			try
			{
				lineNum = Int32.Parse( lineNumStr );
				if( 0 < lineNum )
				{
					return true;
				}
			}
			catch( FormatException )
			{}
			catch( OverflowException )
			{}

			return false;
		}

		#region Windows Form Designer generated code
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
			if( disposing && (components != null) )
			{
				components.Dispose();
			}
			base.Dispose( disposing );
		}

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this._LineNumLabel = new System.Windows.Forms.Label();
			this._LineNumTextBox = new System.Windows.Forms.TextBox();
			this._OkButton = new System.Windows.Forms.Button();
			this._CancelButton = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// _LineNumLabel
			// 
			this._LineNumLabel.Location = new System.Drawing.Point( 12, 9 );
			this._LineNumLabel.Name = "_LineNumLabel";
			this._LineNumLabel.Size = new System.Drawing.Size( 69, 12 );
			this._LineNumLabel.Text = "Line &number:";
			// 
			// _LineNumTextBox
			// 
			this._LineNumTextBox.Location = new System.Drawing.Point( 87, 6 );
			this._LineNumTextBox.Name = "_LineNumTextBox";
			this._LineNumTextBox.Size = new System.Drawing.Size( 100, 19 );
			this._LineNumTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
			this._LineNumTextBox.MaxLength = 10;
			this._LineNumTextBox.KeyDown += new KeyEventHandler(_LineNumTextBox_KeyDown);
			this._LineNumTextBox.KeyPress += new KeyPressEventHandler(_LineNumTextBox_KeyPress);
			this._LineNumTextBox.Enter += new System.EventHandler( this._LineNumTextBox_Enter );
			// 
			// _OkButton
			// 
			this._OkButton.Location = new System.Drawing.Point( 31, 31 );
			this._OkButton.Name = "_OkButton";
			this._OkButton.Size = new System.Drawing.Size( 75, 23 );
			this._OkButton.Text = "OK";
			this._OkButton.Click += new System.EventHandler( this._OkButton_Click );
			// 
			// _CancelButton
			// 
			this._CancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this._CancelButton.Location = new System.Drawing.Point( 112, 31 );
			this._CancelButton.Name = "_CancelButton";
			this._CancelButton.Size = new System.Drawing.Size( 75, 23 );
			this._CancelButton.Text = "Cancel";
			// 
			// GotoForm
			// 
			this.AcceptButton = this._OkButton;
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this._CancelButton;
			this.ImeMode = System.Windows.Forms.ImeMode.Disable;
			this.ShowIcon = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 12F );
			this.ClientSize = new System.Drawing.Size( 200, 62 );
			this.Controls.Add( this._LineNumLabel );
			this.Controls.Add( this._LineNumTextBox );
			this.Controls.Add( this._OkButton );
			this.Controls.Add( this._CancelButton );
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "GotoForm";
			this.Text = "Go to line";
			this.ResumeLayout( false );
			this.PerformLayout();
		}

		private System.Windows.Forms.Label _LineNumLabel;
		private System.Windows.Forms.Button _OkButton;
		private System.Windows.Forms.Button _CancelButton;
		private System.Windows.Forms.TextBox _LineNumTextBox;
		#endregion
	}
}
