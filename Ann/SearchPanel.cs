// 2009-01-31
using System;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Sgry.Azuki;
using Sgry.Azuki.Windows;

namespace Sgry.Ann
{
	class FindContext
	{
		public bool PatternFixed = true;
		public string TextPattern = String.Empty;
		public StringComparison ComparisonType = StringComparison.OrdinalIgnoreCase;
		public Regex Regex = null;
	}

	class Finder : Panel
	{
		FindContext _Context = new FindContext();
		RegexOptions _RegexOptions = RegexOptions.IgnoreCase;

		#region Init / Dispose
		public Finder()
		{
			InitializeComponents();
		}
		#endregion

		#region Operations
		public void Activate()
		{
			Enabled = true;
			_Azuki_Find.SelectAll();
			Show();
			Focus();
		}
		#endregion

		#region Properties
		public FindContext Context
		{
			get{ return _Context; }
		}
		#endregion

		#region UI Properties
		public override Font Font
		{
			get{ return base.Font; }
			set
			{
				base.Font
					= _Label_Find.Font
					= _Azuki_Find.Font
					= _Button_Next.Font = value;
				LayoutComponents();
			}
		}
		#endregion

		#region Event Handlers
		void FixFindParameters( IUserInterface ui )
		{
			this.Hide();
			this.Enabled = false;
			_Context.PatternFixed = true;
			InvokePatternFixed();
		}

		void _Check_MatchCase_Clicked( object sender, EventArgs e )
		{
			if( _Check_MatchCase.Checked )
			{
				_Context.ComparisonType = StringComparison.Ordinal;
			}
			else
			{
				_Context.ComparisonType = StringComparison.OrdinalIgnoreCase;
			}
		}

		void _Check_Regex_Clicked( object sender, EventArgs e )
		{
			// set option flags
			_RegexOptions = RegexOptions.IgnoreCase;
			if( _Check_MatchCase.Checked )
				_RegexOptions ^= RegexOptions.IgnoreCase;
			if( _Check_Back.Checked )
				_RegexOptions |= RegexOptions.RightToLeft;
		}

		void _Azuki_Find_ContentChanged( object sender, ContentChangedEventArgs e )
		{
			if( _Check_Regex.Checked )
			{
				try
				{
					_Context.Regex = new Regex( _Azuki_Find.Text, _RegexOptions );
				}
				catch( ArgumentException )
				{}
			}
			else
			{
				_Context.TextPattern = _Azuki_Find.Text;
				_Context.Regex = null;
			}
			InvokePatternUpdated( true );
		}

		void FocusBackToPatternBox( object sender, EventArgs e )
		{
			_Azuki_Find.Focus();
		}
		#endregion

		#region Event
		public delegate void PatternUpdatedEventHandler( bool forward );
		public event PatternUpdatedEventHandler PatternUpdated;
		void InvokePatternUpdated( bool forward )
		{
			if( PatternUpdated != null )
				PatternUpdated( forward );
		}

		public event EventHandler PatternFixed;
		void InvokePatternFixed()
		{
			if( PatternFixed != null )
				PatternFixed( this, EventArgs.Empty );
		}
		#endregion

		#region UI Component Initialization
		void InitializeComponents()
		{
			// setup panel
			Dock = DockStyle.Bottom;
			Controls.Add( _Label_Find );
			Controls.Add( _Azuki_Find );
			Controls.Add( _Button_Next );
			Controls.Add( _Check_Back );
			Controls.Add( _Check_MatchCase );
			Controls.Add( _Check_Regex );
			GotFocus += delegate {
				_Azuki_Find.Focus();
			};

			// setup label
			_Label_Find.Text = "Find:";

			// setup text field
			_Azuki_Find.HighlightsCurrentLine = false;
			_Azuki_Find.ShowsHScrollBar = false;
			_Azuki_Find.ShowsLineNumber = false;
			_Azuki_Find.AcceptsTab = false;
			_Azuki_Find.AcceptsReturn = false;
			_Azuki_Find.SetKeyBind( Keys.Enter, FixFindParameters );
			_Azuki_Find.SetKeyBind( Keys.Escape, FixFindParameters );
			_Azuki_Find.SetKeyBind( Keys.C | Keys.Control, delegate{
				_Check_MatchCase_Clicked(this, EventArgs.Empty);
			});
			_Azuki_Find.SetKeyBind( Keys.R | Keys.Control,
				delegate( IUserInterface ui ) {
					_Check_Regex.Checked = !( _Check_Regex.Checked );
				}
			);
			_Azuki_Find.Document.ContentChanged += _Azuki_Find_ContentChanged;

			// setup button "Go"
			_Button_Next.Text = "&Go";
			_Button_Next.GotFocus += FocusBackToPatternBox;
			_Button_Next.Click += delegate {
				InvokePatternFixed();
				InvokePatternUpdated( !_Check_Back.Checked );
			};

			// setup check boxes
			_Check_Back.Text = "&Back";
			_Check_Back.GotFocus += FocusBackToPatternBox;
			_Check_MatchCase.Text = "m/&c";
			_Check_MatchCase.GotFocus += FocusBackToPatternBox;
			_Check_MatchCase.Click += _Check_MatchCase_Clicked;
			_Check_Regex.Text = "&Regex";
			_Check_Regex.GotFocus += FocusBackToPatternBox;
			_Check_Regex.Click += _Check_Regex_Clicked;

			// re-caulculate layout
			LayoutComponents();
		}

		void LayoutComponents()
		{
			IGraphics gra = Plat.Inst.GetGraphics( Handle );
			Size labelSize = gra.MeasureText( _Label_Find.Text );

			this.Height = labelSize.Height + 2;
			_Label_Find.Size = labelSize;
			_Azuki_Find.Size = new Size( labelSize.Width*3, labelSize.Height );
			_Azuki_Find.Location = new Point( labelSize.Width, 1 );
			_Button_Next.Size = new Size( labelSize.Width*2, labelSize.Height );
			_Button_Next.Location = new Point( _Azuki_Find.Right+2, 1 );
			_Check_Back.Size = new Size( labelSize.Width*2, labelSize.Height );
			_Check_Back.Location = new Point( _Button_Next.Right+2, 1 );
			_Check_MatchCase.Size = new Size(
				gra.MeasureText(_Check_MatchCase.Text).Width + labelSize.Height,
				labelSize.Height
			);
			_Check_MatchCase.Location = new Point( _Check_Back.Right+2, 1 );
			_Check_Regex.Size = new Size(
				gra.MeasureText(_Check_Regex.Text).Width + labelSize.Height,
				labelSize.Height
			);
			_Check_Regex.Location = new Point( _Check_MatchCase.Right, 1 );
		}
		#endregion

		#region UI Components
		Label _Label_Find = new Label();
		AzukiControl _Azuki_Find = new AzukiControl();
		Button _Button_Next = new Button();
		CheckBox _Check_Back = new CheckBox();
		CheckBox _Check_MatchCase = new CheckBox();
		CheckBox _Check_Regex = new CheckBox();
		#endregion
	}
}
