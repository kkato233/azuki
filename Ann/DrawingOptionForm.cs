// 2010-03-14
using System;
using System.Drawing;
using System.Windows.Forms;
using Sgry.Azuki;
using Sgry.Azuki.Windows;
using InstalledFontCollection = System.Drawing.Text.InstalledFontCollection;
using Highlighters = Sgry.Azuki.Highlighter.Highlighters;

namespace Sgry.Ann
{
	partial class DrawingOptionForm : Form
	{
		#region Fields
		DrawingOption _DrawingOption;
		int _TabWidth;
		FontInfo _FontInfo = null;
		EventHandler _Handler = null;
		#endregion

		#region Init / Dispose
		public DrawingOptionForm()
		{
			// setup GUI components
			InitializeComponent();
			InitializeComponent2();

			// list up fonts
			foreach( FontFamily family in new InstalledFontCollection().Families )
			{
				_Combo_Fonts.Items.Add( family.Name );
			}

			// other initialization
			_FontInfo = new FontInfo( this.Font );
		}
		#endregion

		#region Setting Access
		/// <summary>
		/// This event handler will be called on
		/// on each time user changed a parameter on dialog.
		/// Use this for preview option values.
		/// </summary>
		public EventHandler OptionChangedHandler
		{
			get{ return _Handler; }
			set{ _Handler = value; }
		}

		/// <summary>
		/// Gets or sets font information to be used in Azuki.
		/// </summary>
		public FontInfo FontInfo
		{
			get{ return _FontInfo; }
			set
			{
				_FontInfo = new FontInfo( value );
				_Combo_Fonts.Text = _FontInfo.Name;
				_Num_FontSize.Value = _FontInfo.Size;
			}
		}

		/// <summary>
		/// Gets or sets Azuki's drawing option used in this dialog.
		/// </summary>
		public DrawingOption DrawingOption
		{
			get{ return _DrawingOption; }
			set
			{
				_Check_DrawsSpace.Checked = (value & DrawingOption.DrawsSpace) != 0;
				_Check_DrawsFullWidthSpace.Checked = (value & DrawingOption.DrawsFullWidthSpace) != 0;
				_Check_DrawsTab.Checked = (value & DrawingOption.DrawsTab) != 0;
				_Check_DrawsEolCode.Checked = (value & DrawingOption.DrawsEol) != 0;
				_Check_HighlightCurrentLine.Checked = (value & DrawingOption.HighlightCurrentLine) != 0;
				_Check_ShowsLineNumber.Checked = (value & DrawingOption.ShowsLineNumber) != 0;
				_Check_ShowsHRuler.Checked = (value & DrawingOption.ShowsHRuler) != 0;
				_Check_ShowsDirtBar.Checked = (value & DrawingOption.ShowsDirtBar) != 0;
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
				_Num_TabWidth.Value = value;
				_TabWidth = value;
			}
		}
		#endregion

		#region UI Event Handling
		void InvokeOptionChanged()
		{
			if( _Handler != null )
			{
				_Handler( this, EventArgs.Empty );
			}
		}

		void _Combo_Fonts_TextChanged( object sender, EventArgs e )
		{
			_FontInfo.Name = _Combo_Fonts.Text;
			InvokeOptionChanged();
		}

		void _CheckBox_CheckedChanged( object sender, EventArgs e )
		{
			CheckBox checkBox = (CheckBox)sender;
			if( checkBox == _Check_DrawsEolCode )
			{
				if( _Check_DrawsEolCode.Checked )
					_DrawingOption |= DrawingOption.DrawsEol;
				else
					_DrawingOption &= ~(DrawingOption.DrawsEol);
			}
			else if( checkBox == _Check_DrawsFullWidthSpace )
			{
				if( _Check_DrawsFullWidthSpace.Checked )
					_DrawingOption |= DrawingOption.DrawsFullWidthSpace;
				else
					_DrawingOption &= ~(DrawingOption.DrawsFullWidthSpace);
			}
			else if( checkBox == _Check_DrawsSpace )
			{
				if( _Check_DrawsSpace.Checked )
					_DrawingOption |= DrawingOption.DrawsSpace;
				else
					_DrawingOption &= ~(DrawingOption.DrawsSpace);
			}
			else if( checkBox == _Check_DrawsTab )
			{
				if( _Check_DrawsTab.Checked )
					_DrawingOption |= DrawingOption.DrawsTab;
				else
					_DrawingOption &= ~(DrawingOption.DrawsTab);
			}
			else if( checkBox == _Check_HighlightCurrentLine )
			{
				if( _Check_HighlightCurrentLine.Checked )
					_DrawingOption |= DrawingOption.HighlightCurrentLine;
				else
					_DrawingOption &= ~(DrawingOption.HighlightCurrentLine);
			}
			else if( checkBox == _Check_ShowsLineNumber )
			{
				if( _Check_ShowsLineNumber.Checked )
					_DrawingOption |= DrawingOption.ShowsLineNumber;
				else
					_DrawingOption &= ~(DrawingOption.ShowsLineNumber);
			}
			else if( checkBox == _Check_ShowsHRuler )
			{
				if( _Check_ShowsHRuler.Checked )
					_DrawingOption |= DrawingOption.ShowsHRuler;
				else
					_DrawingOption &= ~(DrawingOption.ShowsHRuler);
			}
			else if( checkBox == _Check_ShowsDirtBar )
			{
				if( _Check_ShowsDirtBar.Checked )
					_DrawingOption |= DrawingOption.ShowsDirtBar;
				else
					_DrawingOption &= ~(DrawingOption.ShowsDirtBar);
			}
			InvokeOptionChanged();
		}

		void _Num_FontSize_ValueChanged( object sender, EventArgs e )
		{
			_FontInfo.Size = (int)_Num_FontSize.Value;
			InvokeOptionChanged();
		}

		void _Num_FontSize_GotFocus( object sender, EventArgs e )
		{
			_Num_FontSize.Select( 0, 100 );
		}

		void _Num_TabWidth_ValueChanged( object sender, EventArgs e )
		{
			TabWidth = (int)_Num_TabWidth.Value;
			InvokeOptionChanged();
		}

		void _Num_TabWidth_GotFocus( object sender, EventArgs e )
		{
			_Num_TabWidth.Select( 0, 100 );
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

		/// <summary>
		/// Initialization logic that should not be done by visual designer
		/// because v.d. sometimes erases parameters...
		/// </summary>
		void InitializeComponent2()
		{
#			if !PocketPC
			_Combo_Fonts.DropDownWidth = 300;
			StartPosition = FormStartPosition.CenterParent;
			AcceptButton = _Button_OK;
			CancelButton = _Button_Cancel;
			Font = SystemInformation.MenuFont;
#			else
			;
#			endif

			// install event handlers
			_Combo_Fonts.TextChanged += _Combo_Fonts_TextChanged;
			_Num_FontSize.ValueChanged += _Num_FontSize_ValueChanged;
			_Num_FontSize.GotFocus += _Num_FontSize_GotFocus;
			_Check_DrawsSpace.Click += _CheckBox_CheckedChanged;
			_Check_DrawsTab.Click += _CheckBox_CheckedChanged;
			_Check_DrawsFullWidthSpace.Click += _CheckBox_CheckedChanged;
			_Check_DrawsEolCode.Click += _CheckBox_CheckedChanged;
			_Check_HighlightCurrentLine.Click += _CheckBox_CheckedChanged;
			_Check_ShowsLineNumber.Click += _CheckBox_CheckedChanged;
			_Check_ShowsHRuler.Click += _CheckBox_CheckedChanged;
			_Check_ShowsDirtBar.Click += _CheckBox_CheckedChanged;
			_Num_TabWidth.ValueChanged += _Num_TabWidth_ValueChanged;
			_Num_TabWidth.GotFocus += _Num_TabWidth_GotFocus;
			_Button_OK.Click += _Button_OK_Click;
			_Button_Cancel.Click += _Button_Cancel_Click;
		}
	}
}
