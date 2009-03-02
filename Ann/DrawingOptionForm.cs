// 2009-03-02
using System;
using System.Drawing;
using System.Windows.Forms;
using Sgry.Azuki;
using Sgry.Azuki.Windows;
using Highlighters = Sgry.Azuki.Highlighter.Highlighters;

namespace Sgry.Ann
{
	class DrawingOptionForm : Form
	{
		/*
		 * Metrics:
		 * form.padding-right: 16
		 * form.padding-left: 16
		 * leftPane.padding-left: 0
		 * leftPane.width: 280
		 * leftPane.padding-right: 16
		 * (leftPane.right: 296)
		 */
		#region Fields
		DrawingOption _DrawingOption;
		int _TabWidth;
		#endregion

		#region Init / Dispose
		public DrawingOptionForm()
		{
			InitializeComponent();
#			if !PocketPC
			this._Azuki_Preview.Text = "#include <stdio.h>\n\nint main( int argc, char* argv[] ) {\n\tprintf( \"en: Hello, World!\\n\" );\n\tprintf( \"ja: \x3053\x3093\x306b\x3061\x306f\x3000\x4e16\x754c\xff01\\n\" );\n\treturn 0;\n}\n/*CR+LF*/\r\n/*LF*/\r";
			this._Azuki_Preview.ClearHistory();
			this._Azuki_Preview.Highlighter = Highlighters.Cpp;
			this.AutoScaleMode = AutoScaleMode.Font;
			this.Font = SystemInformation.MenuFont;
			this.StartPosition = FormStartPosition.CenterParent;
			this.AcceptButton = _Button_OK;
			this.CancelButton = _Button_Cancel;
#			else
			this.Controls.Remove( _Label_Preview );
			this.Controls.Remove( _Azuki_Preview );
			this.ClientSize = new Size( 296, ClientSize.Height );
#			endif

			Closing += Form_Closing;
		}

		void Form_Closing( object sender, EventArgs e )
		{
			_DrawingOption = _Azuki_Preview.DrawingOption;
			_TabWidth = _Azuki_Preview.TabWidth;
		}
		#endregion

		/// <summary>
		/// Gets or sets Azuki's drawing option used in this dialog.
		/// </summary>
		public DrawingOption DrawingOption
		{
			get{ return _DrawingOption; }
			set
			{
				_Azuki_Preview.DrawingOption = value;
				_Check_DrawsSpace.Checked = (value & DrawingOption.DrawsSpace) != 0;
				_Check_DrawsFullWidthSpace.Checked = (value & DrawingOption.DrawsFullWidthSpace) != 0;
				_Check_DrawsTab.Checked = (value & DrawingOption.DrawsTab) != 0;
				_Check_DrawsEolCode.Checked = (value & DrawingOption.DrawsEol) != 0;
				_Check_HighlightCurrentLine.Checked = (value & DrawingOption.HighlightCurrentLine) != 0;
				_Check_ShowsLineNumber.Checked = (value & DrawingOption.ShowsLineNumber) != 0;
				_DrawingOption = value;
			}
		}

		/// <summary>
		/// Gets or sets tab width used in this dialog.
		/// </summary>
		public int TabWidth
		{
			get{ return _TabWidth; }
			set
			{
				_Azuki_Preview.TabWidth = value;
				_Num_TabWidth.Value = value;
				_TabWidth = value;
			}
		}

		#region UI Event Handling
		void _CheckBox_CheckedChanged( object sender, EventArgs e )
		{
			CheckBox checkBox = (CheckBox)sender;
			if( checkBox == _Check_DrawsEolCode )
			{
				_Azuki_Preview.DrawsEolCode = _Check_DrawsEolCode.Checked;
			}
			else if( checkBox == _Check_DrawsFullWidthSpace )
			{
				_Azuki_Preview.DrawsFullWidthSpace = _Check_DrawsFullWidthSpace.Checked;
			}
			else if( checkBox == _Check_DrawsSpace )
			{
				_Azuki_Preview.DrawsSpace = _Check_DrawsSpace.Checked;
			}
			else if( checkBox == _Check_DrawsTab )
			{
				_Azuki_Preview.DrawsTab = _Check_DrawsTab.Checked;
			}
			else if( checkBox == _Check_HighlightCurrentLine )
			{
				_Azuki_Preview.HighlightsCurrentLine = _Check_HighlightCurrentLine.Checked;
			}
			else if( checkBox == _Check_ShowsLineNumber )
			{
				_Azuki_Preview.ShowsLineNumber = _Check_ShowsLineNumber.Checked;
			}
		}

		void _Num_TabWidth_ValueChanged( object sender, EventArgs e )
		{
			TabWidth = (int)_Num_TabWidth.Value;
		}

		void _Button_OK_Click( object sender, EventArgs e )
		{
			this.DialogResult = DialogResult.OK;
			this.Close();
		}

		void _Button_Cancel_Click( object sender, EventArgs e )
		{
			this.DialogResult = DialogResult.Cancel;
			this.Close();
		}
		#endregion

		#region UI Component Initialization
		void InitializeComponent()
		{
			_Check_DrawsSpace = new CheckBox();
			_Check_DrawsTab = new CheckBox();
			_Check_DrawsFullWidthSpace = new CheckBox();
			_Check_DrawsEolCode = new CheckBox();
			_Check_HighlightCurrentLine = new CheckBox();
			_Check_ShowsLineNumber = new CheckBox();
			_Label_TabWidth = new Label();
			_Num_TabWidth = new NumericUpDown();
			_Label_Preview = new Label();
			_Azuki_Preview = new AzukiControl();
			_Button_OK = new Button();
			_Button_Cancel = new Button();
			SuspendLayout();
			// 
			// _Check_DrawsSpace
			// 
			_Check_DrawsSpace.Location = new Point( 16, 12 );
			_Check_DrawsSpace.Name = "_Check_DrawsSpace";
			_Check_DrawsSpace.Size = new Size( 264, 17 );
			_Check_DrawsSpace.TabIndex = 0;
			_Check_DrawsSpace.Text = "Draws &space";
			_Check_DrawsSpace.Click += _CheckBox_CheckedChanged;
			// 
			// _Check_DrawsTab
			// 
			_Check_DrawsTab.Location = new Point( 16, 33 );
			_Check_DrawsTab.Name = "_Check_DrawsTab";
			_Check_DrawsTab.Size = new Size( 264, 17 );
			_Check_DrawsTab.Text = "Draws &tab";
			_Check_DrawsTab.Click += _CheckBox_CheckedChanged;
			// 
			// _Check_DrawsFullWidthSpace
			// 
			_Check_DrawsFullWidthSpace.Location = new Point( 16, 54 );
			_Check_DrawsFullWidthSpace.Name = "_Check_DrawsFullWidthSpace";
			_Check_DrawsFullWidthSpace.Size = new Size( 264, 17 );
			_Check_DrawsFullWidthSpace.Text = "Draws &full width space (U+3000)";
			_Check_DrawsFullWidthSpace.Click += _CheckBox_CheckedChanged;
			// 
			// _Check_DrawsEolCode
			// 
			_Check_DrawsEolCode.Location = new Point( 16, 75 );
			_Check_DrawsEolCode.Name = "_Check_DrawsEolCode";
			_Check_DrawsEolCode.Size = new Size( 264, 17 );
			_Check_DrawsEolCode.Text = "Draws EO&L (end of line) code";
			_Check_DrawsEolCode.Click += _CheckBox_CheckedChanged;
			// 
			// _Check_HighlightCurrentLine
			// 
			_Check_HighlightCurrentLine.Location = new Point( 16, 96 );
			_Check_HighlightCurrentLine.Name = "_Check_HighlightCurrentLine";
			_Check_HighlightCurrentLine.Size = new Size( 264, 17 );
			_Check_HighlightCurrentLine.Text = "Highlights &current line";
			_Check_HighlightCurrentLine.Click += _CheckBox_CheckedChanged;
			// 
			// _Check_ShowsLineNumber
			// 
			_Check_ShowsLineNumber.Location = new Point( 16, 117 );
			_Check_ShowsLineNumber.Name = "_Check_ShowsLineNumber";
			_Check_ShowsLineNumber.Size = new Size( 264, 17 );
			_Check_ShowsLineNumber.Text = "Shows line &number";
			_Check_ShowsLineNumber.Click += _CheckBox_CheckedChanged;
			// 
			// _Label_TabWidth
			// 
			_Label_TabWidth.Location = new Point( 16, 142 );
			_Label_TabWidth.Name = "_Label_TabWidth";
			_Label_TabWidth.Size = new Size( 80, 17 );
			_Label_TabWidth.Text = "Tab &width:";
			// 
			// _Num_TabWidth
			// 
			_Num_TabWidth.Location = new Point( 98, 140 );
			_Num_TabWidth.Name = "_Num_TabWidth";
			_Num_TabWidth.Size = new Size( 172, 24 );
			_Num_TabWidth.ValueChanged += _Num_TabWidth_ValueChanged;
			// 
			// _Button_OK
			// 
			_Button_OK.Location = new Point( 116, 168 );
			_Button_OK.Name = "_Button_OK";
			_Button_OK.Size = new Size( 75, 20 );
			_Button_OK.Text = "OK";
			_Button_OK.Click += _Button_OK_Click;
			// 
			// _Button_Cancel
			// 
			_Button_Cancel.Location = new Point( 197, 168 );
			_Button_Cancel.Name = "_Button_Cancel";
			_Button_Cancel.Size = new Size( 75, 20 );
			_Button_Cancel.Text = "Cancel";
			_Button_Cancel.Click += _Button_Cancel_Click;
#			if !PocketPC
			// 
			// _Label_Preview
			// 
			_Label_Preview.Location = new Point( 296, 12 );
			_Label_Preview.Name = "_Label_Preview";
			_Label_Preview.Size = new Size( 80, 17 );
			_Label_Preview.Text = "Preview:";
			// 
			// _Azuki_Preview
			// 
			_Azuki_Preview.AcceptsReturn = false;
			_Azuki_Preview.AcceptsTab = false;
			_Azuki_Preview.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
			_Azuki_Preview.Font = new Font( "Courier New", 10, FontStyle.Regular );
			_Azuki_Preview.Location = new Point( 296, 27 );
			_Azuki_Preview.Name = "_Azuki_Preview";
			_Azuki_Preview.Size = new Size( 244, 161 );
			_Azuki_Preview.TabIndex = 9;
			_Azuki_Preview.TabStop = false;
			_Azuki_Preview.ViewWidth = 328;
#			endif
			// 
			// DrawingOptionForm
			// 
			AutoScroll = true;
			ClientSize = new Size( 560, 198 );
			MinimizeBox = false;
			Controls.Add( _Check_DrawsSpace );
			Controls.Add( _Check_DrawsTab );
			Controls.Add( _Check_DrawsFullWidthSpace );
			Controls.Add( _Check_DrawsEolCode );
			Controls.Add( _Check_HighlightCurrentLine );
			Controls.Add( _Check_ShowsLineNumber );
			Controls.Add( _Label_TabWidth );
			Controls.Add( _Num_TabWidth );
			Controls.Add( _Button_OK );
			Controls.Add( _Button_Cancel );
			Controls.Add( _Azuki_Preview );
			Controls.Add( _Label_Preview );
			Name = "DrawingOptionForm";
			Text = "Ann - Drawing Options";
			ResumeLayout( false );
		}
		#endregion

		#region UI Components
		AzukiControl _Azuki_Preview;
		Button _Button_OK;
		Button _Button_Cancel;
		NumericUpDown _Num_TabWidth;
		Label _Label_TabWidth;
		CheckBox _Check_DrawsSpace;
		CheckBox _Check_DrawsTab;
		CheckBox _Check_DrawsFullWidthSpace;
		CheckBox _Check_HighlightCurrentLine;
		CheckBox _Check_ShowsLineNumber;
		Label _Label_Preview;
		CheckBox _Check_DrawsEolCode;
		#endregion
	}
}
