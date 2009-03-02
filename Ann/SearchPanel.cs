// 2009-03-02
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

		#region Init / Dispose
		public SearchPanel()
		{
			InitializeComponents();
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
		public override Font Font
		{
			get{ return base.Font; }
			set
			{
				base.Font
					= _Label_Pattern.Font
					= _Azuki_Pattern.Font
					= _Button_Next.Font = value;
				LayoutComponents();
			}
		}
		#endregion

		#region Event Handlers
		void FixParameters( IUserInterface ui )
		{
			Deactivate();
			InvokePatternFixed();
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
			// setup panel
			Dock = DockStyle.Bottom;
			Controls.Add( _Label_Pattern );
			Controls.Add( _Azuki_Pattern );
			Controls.Add( _Button_Next );
			Controls.Add( _Button_Prev );
			Controls.Add( _Check_MatchCase );
			Controls.Add( _Check_Regex );
			GotFocus += delegate {
				_Azuki_Pattern.Focus();
			};

			// setup label
			_Label_Pattern.Text = "Find:";

			// setup text field
			_Azuki_Pattern.HighlightsCurrentLine = false;
			_Azuki_Pattern.ShowsHScrollBar = false;
			_Azuki_Pattern.ShowsLineNumber = false;
			_Azuki_Pattern.AcceptsTab = false;
			_Azuki_Pattern.AcceptsReturn = false;
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

			// re-caulculate layout
			LayoutComponents();
		}

		void LayoutComponents()
		{
			IGraphics gra = Plat.Inst.GetGraphics( Handle );
			Size labelSize = gra.MeasureText( _Label_Pattern.Text );

			this.Height = labelSize.Height + 2;
			_Label_Pattern.Size = labelSize;
			_Azuki_Pattern.Size = new Size( labelSize.Width*3, labelSize.Height );
			_Azuki_Pattern.Location = new Point( labelSize.Width, 1 );
			_Button_Next.Size = new Size( labelSize.Width*2, labelSize.Height );
			_Button_Next.Location = new Point( _Azuki_Pattern.Right+2, 1 );
			_Button_Prev.Size = new Size( labelSize.Width*2, labelSize.Height );
			_Button_Prev.Location = new Point( _Button_Next.Right+2, 1 );
			_Check_MatchCase.Size = new Size(
				gra.MeasureText(_Check_MatchCase.Text).Width + labelSize.Height,
				labelSize.Height
			);
			_Check_MatchCase.Location = new Point( _Button_Prev.Right+2, 1 );
			_Check_Regex.Size = new Size(
				gra.MeasureText(_Check_Regex.Text).Width + labelSize.Height,
				labelSize.Height
			);
			_Check_Regex.Location = new Point( _Check_MatchCase.Right, 1 );
		}
		#endregion

		#region UI Components
		Label _Label_Pattern = new Label();
		AzukiControl _Azuki_Pattern = new AzukiControl();
		Button _Button_Next = new Button();
		Button _Button_Prev = new Button();
		CheckBox _Check_MatchCase = new CheckBox();
		CheckBox _Check_Regex = new CheckBox();
		#endregion
	}
	#endregion
}
