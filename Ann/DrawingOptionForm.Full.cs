using System;
using System.Drawing;
using System.Windows.Forms;

namespace Sgry.Ann
{
	partial class DrawingOptionForm : Form
	{
		void InitializeComponent()
		{
			this._Label_Fonts = new System.Windows.Forms.Label();
			this._Combo_Fonts = new System.Windows.Forms.ComboBox();
			this._Label_FontSize = new System.Windows.Forms.Label();
			this._Num_FontSize = new System.Windows.Forms.NumericUpDown();
			this._Check_DrawsSpace = new System.Windows.Forms.CheckBox();
			this._Check_DrawsTab = new System.Windows.Forms.CheckBox();
			this._Check_DrawsFullWidthSpace = new System.Windows.Forms.CheckBox();
			this._Check_DrawsEolCode = new System.Windows.Forms.CheckBox();
			this._Check_HighlightCurrentLine = new System.Windows.Forms.CheckBox();
			this._Check_ShowsLineNumber = new System.Windows.Forms.CheckBox();
			this._Check_ShowsHRuler = new System.Windows.Forms.CheckBox();
			this._Check_ShowsDirtBar = new System.Windows.Forms.CheckBox();
			this._Label_TabWidth = new System.Windows.Forms.Label();
			this._Num_TabWidth = new System.Windows.Forms.NumericUpDown();
			this._Button_OK = new System.Windows.Forms.Button();
			this._Button_Cancel = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this._Num_FontSize)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this._Num_TabWidth)).BeginInit();
			this.SuspendLayout();
			// 
			// _Label_Fonts
			// 
			this._Label_Fonts.Location = new System.Drawing.Point( 14, 16 );
			this._Label_Fonts.Name = "_Label_Fonts";
			this._Label_Fonts.Size = new System.Drawing.Size( 99, 19 );
			this._Label_Fonts.TabIndex = 0;
			this._Label_Fonts.Text = "Font name:";
			// 
			// _Combo_Fonts
			// 
			this._Combo_Fonts.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._Combo_Fonts.Location = new System.Drawing.Point( 116, 12 );
			this._Combo_Fonts.Name = "_Combo_Fonts";
			this._Combo_Fonts.Size = new System.Drawing.Size( 212, 23 );
			this._Combo_Fonts.TabIndex = 1;
			// 
			// _Label_FontSize
			// 
			this._Label_FontSize.Location = new System.Drawing.Point( 14, 49 );
			this._Label_FontSize.Name = "_Label_FontSize";
			this._Label_FontSize.Size = new System.Drawing.Size( 99, 19 );
			this._Label_FontSize.TabIndex = 2;
			this._Label_FontSize.Text = "Size:";
			// 
			// _Num_FontSize
			// 
			this._Num_FontSize.Location = new System.Drawing.Point( 116, 45 );
			this._Num_FontSize.Maximum = new decimal( new int[] {
            128,
            0,
            0,
            0} );
			this._Num_FontSize.Minimum = new decimal( new int[] {
            6,
            0,
            0,
            0} );
			this._Num_FontSize.Name = "_Num_FontSize";
			this._Num_FontSize.Size = new System.Drawing.Size( 67, 22 );
			this._Num_FontSize.TabIndex = 3;
			this._Num_FontSize.Value = new decimal( new int[] {
            6,
            0,
            0,
            0} );
			// 
			// _Check_DrawsSpace
			// 
			this._Check_DrawsSpace.Location = new System.Drawing.Point( 12, 78 );
			this._Check_DrawsSpace.Name = "_Check_DrawsSpace";
			this._Check_DrawsSpace.Size = new System.Drawing.Size( 326, 22 );
			this._Check_DrawsSpace.TabIndex = 4;
			this._Check_DrawsSpace.Text = "Draws &space";
			// 
			// _Check_DrawsTab
			// 
			this._Check_DrawsTab.Location = new System.Drawing.Point( 12, 103 );
			this._Check_DrawsTab.Name = "_Check_DrawsTab";
			this._Check_DrawsTab.Size = new System.Drawing.Size( 326, 22 );
			this._Check_DrawsTab.TabIndex = 5;
			this._Check_DrawsTab.Text = "Draws &tab";
			// 
			// _Check_DrawsFullWidthSpace
			// 
			this._Check_DrawsFullWidthSpace.Location = new System.Drawing.Point( 12, 128 );
			this._Check_DrawsFullWidthSpace.Name = "_Check_DrawsFullWidthSpace";
			this._Check_DrawsFullWidthSpace.Size = new System.Drawing.Size( 326, 22 );
			this._Check_DrawsFullWidthSpace.TabIndex = 6;
			this._Check_DrawsFullWidthSpace.Text = "Draws &full width space (U+3000)";
			// 
			// _Check_DrawsEolCode
			// 
			this._Check_DrawsEolCode.Location = new System.Drawing.Point( 12, 153 );
			this._Check_DrawsEolCode.Name = "_Check_DrawsEolCode";
			this._Check_DrawsEolCode.Size = new System.Drawing.Size( 326, 22 );
			this._Check_DrawsEolCode.TabIndex = 7;
			this._Check_DrawsEolCode.Text = "Draws EO&L (end of line) code";
			// 
			// _Check_HighlightCurrentLine
			// 
			this._Check_HighlightCurrentLine.Location = new System.Drawing.Point( 12, 178 );
			this._Check_HighlightCurrentLine.Name = "_Check_HighlightCurrentLine";
			this._Check_HighlightCurrentLine.Size = new System.Drawing.Size( 326, 22 );
			this._Check_HighlightCurrentLine.TabIndex = 8;
			this._Check_HighlightCurrentLine.Text = "Highlights &current line";
			// 
			// _Check_ShowsLineNumber
			// 
			this._Check_ShowsLineNumber.Location = new System.Drawing.Point( 12, 203 );
			this._Check_ShowsLineNumber.Name = "_Check_ShowsLineNumber";
			this._Check_ShowsLineNumber.Size = new System.Drawing.Size( 326, 22 );
			this._Check_ShowsLineNumber.TabIndex = 9;
			this._Check_ShowsLineNumber.Text = "Shows line &number";
			// 
			// _Check_ShowsHRuler
			// 
			this._Check_ShowsHRuler.Location = new System.Drawing.Point( 12, 228 );
			this._Check_ShowsHRuler.Name = "_Check_ShowsHRuler";
			this._Check_ShowsHRuler.Size = new System.Drawing.Size( 326, 22 );
			this._Check_ShowsHRuler.TabIndex = 10;
			this._Check_ShowsHRuler.Text = "Shows &horizontal ruler";
			// 
			// _Check_ShowsDirtBar
			// 
			this._Check_ShowsDirtBar.Location = new System.Drawing.Point( 12, 254 );
			this._Check_ShowsDirtBar.Name = "_Check_ShowsDirtBar";
			this._Check_ShowsDirtBar.Size = new System.Drawing.Size( 326, 22 );
			this._Check_ShowsDirtBar.TabIndex = 11;
			this._Check_ShowsDirtBar.Text = "Shows &dirt bar";
			// 
			// _Label_TabWidth
			// 
			this._Label_TabWidth.Location = new System.Drawing.Point( 12, 283 );
			this._Label_TabWidth.Name = "_Label_TabWidth";
			this._Label_TabWidth.Size = new System.Drawing.Size( 101, 19 );
			this._Label_TabWidth.TabIndex = 12;
			this._Label_TabWidth.Text = "Tab &width:";
			// 
			// _Num_TabWidth
			// 
			this._Num_TabWidth.Location = new System.Drawing.Point( 117, 279 );
			this._Num_TabWidth.Minimum = 1;
			this._Num_TabWidth.Name = "_Num_TabWidth";
			this._Num_TabWidth.Size = new System.Drawing.Size( 62, 22 );
			this._Num_TabWidth.TabIndex = 13;
			// 
			// _Button_OK
			// 
			this._Button_OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this._Button_OK.Location = new System.Drawing.Point( 142, 319 );
			this._Button_OK.Name = "_Button_OK";
			this._Button_OK.Size = new System.Drawing.Size( 90, 27 );
			this._Button_OK.TabIndex = 14;
			this._Button_OK.Text = "OK";
			// 
			// _Button_Cancel
			// 
			this._Button_Cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this._Button_Cancel.Location = new System.Drawing.Point( 238, 319 );
			this._Button_Cancel.Name = "_Button_Cancel";
			this._Button_Cancel.Size = new System.Drawing.Size( 90, 27 );
			this._Button_Cancel.TabIndex = 15;
			this._Button_Cancel.Text = "Cancel";
			// 
			// DrawingOptionForm
			// 
			this.ClientSize = new System.Drawing.Size( 340, 360 );
			this.Controls.Add( this._Label_Fonts );
			this.Controls.Add( this._Combo_Fonts );
			this.Controls.Add( this._Label_FontSize );
			this.Controls.Add( this._Num_FontSize );
			this.Controls.Add( this._Check_DrawsSpace );
			this.Controls.Add( this._Check_DrawsTab );
			this.Controls.Add( this._Check_DrawsFullWidthSpace );
			this.Controls.Add( this._Check_DrawsEolCode );
			this.Controls.Add( this._Check_HighlightCurrentLine );
			this.Controls.Add( this._Check_ShowsLineNumber );
			this.Controls.Add( this._Check_ShowsHRuler );
			this.Controls.Add( this._Check_ShowsDirtBar );
			this.Controls.Add( this._Label_TabWidth );
			this.Controls.Add( this._Num_TabWidth );
			this.Controls.Add( this._Button_OK );
			this.Controls.Add( this._Button_Cancel );
			this.MinimizeBox = false;
			this.Name = "DrawingOptionForm";
			this.Text = "Ann - Drawing Options";
			((System.ComponentModel.ISupportInitialize)(this._Num_FontSize)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this._Num_TabWidth)).EndInit();
			this.ResumeLayout( false );

		}

		#region UI Components
		Label _Label_Fonts;
		ComboBox _Combo_Fonts;
		Label _Label_FontSize;
		NumericUpDown _Num_FontSize;
		CheckBox _Check_DrawsSpace;
		CheckBox _Check_DrawsTab;
		CheckBox _Check_DrawsEolCode;
		CheckBox _Check_DrawsFullWidthSpace;
		CheckBox _Check_HighlightCurrentLine;
		CheckBox _Check_ShowsLineNumber;
		CheckBox _Check_ShowsHRuler;
		CheckBox _Check_ShowsDirtBar;
		Label _Label_TabWidth;
		NumericUpDown _Num_TabWidth;
		Button _Button_OK;
		Button _Button_Cancel;
		#endregion
	}
}
