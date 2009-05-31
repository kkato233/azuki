// 2009-05-31
using System;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Sgry.Azuki;
using Sgry.Azuki.Windows;

namespace Sgry.Ann
{
	#region Search context object
	class SearchContext
	{
		bool _UseRegex = false;
		Regex _Regex;
		RegexOptions _RegexOptions = RegexOptions.IgnoreCase;

		/// <summary>
		/// Gets or sets search anchor.
		/// </summary>
		public int AnchorIndex = -1;

		/// <summary>
		/// Gets or sets whether the search condition was fixed in SearchPanel or not.
		/// </summary>
		public bool PatternFixed = true;

		/// <summary>
		/// Gets or sets the text pattern to be found.
		/// This will not be used on regular expression search.
		/// </summary>
		public string TextPattern = String.Empty;

		/// <summary>
		/// Gets or sets whether to search text pattern by regular expression or not.
		/// </summary>
		public bool UseRegex
		{
			get{ return _UseRegex; }
			set{ _UseRegex = value; }
		}

		/// <summary>
		/// Gets or sets regular expression object used for text search.
		/// </summary>
		/// <remarks>
		/// If this is null, normal pattern matching will be performed.
		/// If this is not null, regular expression search will be performed.
		/// </remarks>
		public Regex Regex
		{
			set
			{
				_Regex = value;
				_RegexOptions = value.Options;
				TextPattern = _Regex.ToString();
			}
			get
			{
				// if regex object was not created or outdated, re-create it.
				if( _Regex == null
					|| _Regex.ToString() != TextPattern
					|| _Regex.Options != _RegexOptions )
				{
					_Regex = new Regex( TextPattern, _RegexOptions );
				}
				return _Regex;
			}
		}
		
		/// <summary>
		/// Gets or sets whether to search case sensitively or not.
		/// </summary>
		public bool MatchCase
		{
			get{ return (_RegexOptions & RegexOptions.IgnoreCase) == 0; }
			set
			{
				if( value == true )
					_RegexOptions &= ~( RegexOptions.IgnoreCase );
				else
					_RegexOptions |= RegexOptions.IgnoreCase;
			}
		}
	}
	#endregion

	#region Text search user interface
	class SearchPanel : Panel
	{
		SearchContext _ContextRef = null;
		int _LastLayoutWidth = 0;

		#region Init / Dispose
		public SearchPanel()
		{
			InitializeComponents();
			LayoutComponents();
/*Timer t = new Timer();
t.Interval = 1000;
t.Tick+=delegate{
if( _ContextRef != null )
	Console.WriteLine( "a:{0}, m/c:{1}, p:{2}", _ContextRef.AnchorIndex, _ContextRef.MatchCase, _ContextRef.TextPattern );
if( _ContextRef != null && _ContextRef.Regex != null )
	Console.WriteLine( "    r:{0}, o:{1}", _ContextRef.Regex.ToString(), _ContextRef.Regex.Options );
};
t.Start();*/
		}
		#endregion

		#region Operations
		public void Activate( int anchorIndex )
		{
			Enabled = true;
			_Azuki_Pattern.SelectAll();
			_ContextRef.AnchorIndex = anchorIndex;
			Show();
			Focus();
			_Azuki_Pattern.Focus();
		}

		public void Deactivate()
		{
			this.Hide();
			this.Enabled = false;
			_ContextRef.PatternFixed = true;
			_ContextRef.AnchorIndex = -1;
		}
		#endregion

		#region Properties
		public void SetContextRef( SearchContext context )
		{
			_ContextRef = context;
		}
		#endregion

		#region UI Properties
		public void SetFont( Font value )
		{
			IGraphics gra;
			Size labelSize = new Size();

			// apply fonts
			_Label_Pattern.Font
				= _Button_Next.Font = value;
			_Azuki_Pattern.Font = new Font( value.Name, value.Size-1, value.Style );

			// calculate size of child controls
			gra = Plat.Inst.GetGraphics( _Label_Pattern.Handle );
			gra.Font = value;
			labelSize.Width = gra.MeasureText( _Label_Pattern.Text ).Width + 2;
			labelSize.Height = gra.MeasureText( "Mp" ).Height;
			_Label_Pattern.Size = labelSize;
			_Azuki_Pattern.Size = new Size( labelSize.Width*3, labelSize.Height );
			_Button_Next.Size = new Size( gra.MeasureText(_Button_Next.Text).Width+2, labelSize.Height );
			_Button_Prev.Size = new Size( gra.MeasureText(_Button_Prev.Text).Width+2, labelSize.Height );
			_Check_MatchCase.Size = new Size(
				gra.MeasureText(_Check_MatchCase.Text).Width + labelSize.Height,
				labelSize.Height
			);
			_Check_Regex.Size = new Size(
				gra.MeasureText(_Check_Regex.Text).Width + labelSize.Height,
				labelSize.Height
			);
			_Panel_TextBox.Height
				= _Panel_Options.Height
				= _Panel_Actions.Height = _Label_Pattern.Height + 2;
			_Panel_TextBox.Width = _Label_Pattern.Width + _Azuki_Pattern.Width + 2;
			_Panel_Actions.Width = _Button_Next.Width + _Button_Prev.Width + 4;
			_Panel_Options.Width = _Check_MatchCase.Width + _Check_Regex.Width + 2;

			// layout child controls
			LayoutComponents();
		}
		#endregion

		#region Event Handlers
		void FixParameters( IUserInterface ui )
		{
			Deactivate();
			InvokePatternFixed();
		}

		void Panel_Resize( object sender, EventArgs e )
		{
			if( Width != _LastLayoutWidth )
			{
				_LastLayoutWidth = Width;
				LayoutComponents();
			}
		}

		void _Button_Next_Click( object sender, EventArgs e )
		{
			InvokePatternFixed();
			InvokePatternUpdated( true );
		}

		void _Button_Prev_Click( object sender, EventArgs e )
		{
			InvokePatternFixed();
			InvokePatternUpdated( false );
		}

		void _Check_MatchCase_Clicked( object sender, EventArgs e )
		{
			_ContextRef.MatchCase = _Check_MatchCase.Checked;
		}

		void _Check_Regex_Clicked( object sender, EventArgs e )
		{
			_ContextRef.UseRegex = _Check_Regex.Checked;
		}

		void _Azuki_Pattern_ContentChanged( object sender, ContentChangedEventArgs e )
		{
			_ContextRef.TextPattern = _Azuki_Pattern.Text;
			InvokePatternUpdated( true );
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
			_ContextRef.PatternFixed = true;
			_ContextRef.AnchorIndex = -1;
			if( PatternFixed != null )
				PatternFixed( this, EventArgs.Empty );
		}
		#endregion

		#region UI Component Initialization
		void InitializeComponents()
		{
			this.SuspendLayout();

			// setup this panel
			Dock = DockStyle.Bottom;
			GotFocus += delegate {
				_Azuki_Pattern.Focus();
			};
			Resize += Panel_Resize;

			// setup child controls
			_Panel_TextBox.Controls.Add( _Label_Pattern );
			_Panel_TextBox.Controls.Add( _Azuki_Pattern );

			_Panel_Options.Controls.Add( _Check_MatchCase );
			_Panel_Options.Controls.Add( _Check_Regex );

			_Panel_Actions.Controls.Add( _Button_Next );
			_Panel_Actions.Controls.Add( _Button_Prev );

			Controls.Add( _Panel_Options );
			Controls.Add( _Panel_Actions );
			Controls.Add( _Panel_TextBox );

			// setup label
			_Label_Pattern.Text = "Find:";

			// setup text field
			_Azuki_Pattern.HighlightsCurrentLine = false;
			_Azuki_Pattern.ShowsHScrollBar = false;
			_Azuki_Pattern.ShowsLineNumber = false;
			_Azuki_Pattern.AcceptsTab = false;
			_Azuki_Pattern.AcceptsReturn = false;
			_Azuki_Pattern.BorderStyle = BorderStyle.Fixed3D;
			_Azuki_Pattern.Document.ContentChanged += _Azuki_Pattern_ContentChanged;
			_Azuki_Pattern.SetKeyBind( Keys.Enter, FixParameters );
			_Azuki_Pattern.SetKeyBind( Keys.Escape, FixParameters );
			_Azuki_Pattern.SetKeyBind( Keys.N | Keys.Control,
				delegate {
					_Button_Next_Click( this, EventArgs.Empty );
				}
			);
			_Azuki_Pattern.SetKeyBind( Keys.P | Keys.Control,
				delegate {
					_Button_Prev_Click( this, EventArgs.Empty );
				}
			);
			_Azuki_Pattern.SetKeyBind( Keys.C | Keys.Control,
				delegate {
					_Check_MatchCase.Checked = !( _Check_MatchCase.Checked );
					_Check_MatchCase_Clicked( this, EventArgs.Empty );
				}
			);
			_Azuki_Pattern.SetKeyBind( Keys.R | Keys.Control,
				delegate {
					_Check_Regex.Checked = !( _Check_Regex.Checked );
					_Check_Regex_Clicked( this, EventArgs.Empty );
				}
			);

			// setup button "next"
			_Button_Next.Text = "&Next";
			_Button_Next.Click += _Button_Next_Click;

			// setup button "prev"
			_Button_Prev.Text = "&Prev";
			_Button_Prev.Click += _Button_Prev_Click;

			// setup check boxes
			_Check_MatchCase.Text = "m/&c";
			_Check_MatchCase.Click += _Check_MatchCase_Clicked;
			_Check_MatchCase.KeyDown += delegate( object sender, KeyEventArgs e ) {
				if( e.KeyCode == Keys.Enter || e.KeyCode == Keys.Escape )
					FixParameters( _Azuki_Pattern );
			};
			_Check_Regex.Text = "&Regex";
			_Check_Regex.Click += _Check_Regex_Clicked;
			_Check_Regex.KeyDown += delegate( object sender, KeyEventArgs e ) {
				if( e.KeyCode == Keys.Enter || e.KeyCode == Keys.Escape )
					FixParameters( _Azuki_Pattern );
			};

			ResumeLayout();
		}

		void LayoutComponents()
		{
			SuspendLayout();

			// setup panels
			_Panel_TextBox.Location = new Point( 0, 0 );
			_Panel_Actions.Location = new Point( _Panel_TextBox.Right, 0 );
			if( _Panel_Actions.Right + _Panel_Options.Width < Width )
			{
				_Panel_Options.Location = new Point( _Panel_Actions.Right, 0 );
			}
			else
			{
				_Panel_Options.Location = new Point( 0, _Panel_TextBox.Bottom );
			}
			this.Height = _Panel_Options.Bottom - _Panel_TextBox.Top;

			// text box and label
			_Label_Pattern.Top = 3;
			_Azuki_Pattern.Location = new Point( _Label_Pattern.Width, 1 );

			// action buttons
			_Button_Next.Location = new Point( 0, 1 );
			_Button_Prev.Location = new Point( _Button_Next.Right+2, 1 );

			// options
			_Check_MatchCase.Location = new Point( 0, 1 );
			_Check_Regex.Location = new Point( _Check_MatchCase.Right, _Check_MatchCase.Top );

			ResumeLayout();
		}
		#endregion

		#region UI Components
		Panel _Panel_TextBox = new Panel();
		Label _Label_Pattern = new Label();
		AzukiControl _Azuki_Pattern = new AzukiControl();

		Panel _Panel_Actions = new Panel();
		Button _Button_Next = new Button();
		Button _Button_Prev = new Button();

		Panel _Panel_Options = new Panel();
		CheckBox _Check_MatchCase = new CheckBox();
		CheckBox _Check_Regex = new CheckBox();
		#endregion
	}
	#endregion
}
