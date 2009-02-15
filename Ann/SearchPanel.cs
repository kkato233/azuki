// 2009-02-01
using System;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Sgry.Azuki;
using Sgry.Azuki.Windows;

namespace Sgry.Ann
{
	class SearchContext
	{
		public int AnchorIndex = -1;
		public bool PatternFixed = true;
		public string TextPattern = String.Empty;
		public bool MatchCase = false;
		public Regex Regex = null;
	}

	class SearchPanel : Panel
	{
		SearchContext _ContextRef = null;
		RegexOptions _RegexOptions = RegexOptions.IgnoreCase;

		#region Init / Dispose
		public SearchPanel()
		{
			InitializeComponents();
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

		void _Check_MatchCase_Clicked( object sender, EventArgs e )
		{
			_ContextRef.MatchCase = _Check_MatchCase.Checked;
		}

		void _Check_Regex_Clicked( object sender, EventArgs e )
		{
			if( _Check_Regex.Checked )
			{
				try
				{
					_ContextRef.Regex = new Regex( _Azuki_Pattern.Text, _RegexOptions );
				}
				catch( ArgumentException )
				{}
			}
			else
			{
				_ContextRef.Regex = null;
			}
		}

		void _Azuki_Pattern_ContentChanged( object sender, ContentChangedEventArgs e )
		{
			if( _Check_Regex.Checked )
			{
				try
				{
					_ContextRef.Regex = new Regex( _Azuki_Pattern.Text, _RegexOptions );
				}
				catch( ArgumentException )
				{}
			}
			else
			{
				_ContextRef.TextPattern = _Azuki_Pattern.Text;
				_ContextRef.Regex = null;
			}
			InvokePatternUpdated( true );
		}

		void FocusBackToPatternBox( object sender, EventArgs e )
		{
			_Azuki_Pattern.Focus();
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
			_Azuki_Pattern.SetKeyBind( Keys.Enter, FixParameters );
			_Azuki_Pattern.SetKeyBind( Keys.Escape, FixParameters );
			_Azuki_Pattern.SetKeyBind( Keys.C | Keys.Control, delegate{
				_Check_MatchCase_Clicked(this, EventArgs.Empty);
			});
			_Azuki_Pattern.SetKeyBind( Keys.R | Keys.Control,
				delegate( IUserInterface ui ) {
					_Check_Regex.Checked = !( _Check_Regex.Checked );
				}
			);
			_Azuki_Pattern.Document.ContentChanged += _Azuki_Pattern_ContentChanged;

			// setup button "next"
			_Button_Next.Text = "&Next";
			_Button_Next.GotFocus += FocusBackToPatternBox;
			_Button_Next.Click += delegate {
				InvokePatternFixed();
				InvokePatternUpdated( true );
			};

			// setup button "prev"
			_Button_Prev.Text = "&Prev";
			_Button_Prev.GotFocus += FocusBackToPatternBox;
			_Button_Prev.Click += delegate {
				InvokePatternFixed();
				InvokePatternUpdated( false );
			};

			// setup check boxes
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
}
