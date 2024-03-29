using System;
using System.Drawing;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using Sgry.Azuki;
using Sgry.Azuki.WinForms;
using Directory = System.IO.Directory;
using Debug = System.Diagnostics.Debug;

namespace Sgry.Ann
{
	class AnnForm : Form
	{
		#region Fields
		AppLogic _App;
		Dictionary<ToolStripMenuItem, AnnAction> _MenuMap = new Dictionary<ToolStripMenuItem,AnnAction>();
		Dictionary<Keys, AnnAction>	_KeyMap = new Dictionary<Keys, AnnAction>();
		Timer _TimerForDelayedActivatedEvent = new Timer();
		const string StatusMsg_CaretPos = "Position:{3:N0} (Line {0:N0}, Col {1:N0}, Char {2:N0})";
		#endregion

		#region Init / Dispose
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public AnnForm( AppLogic app )
		{
			_App = app;
			InitMenuComponents();
			InitUIComponent();
			InitMenuMap();
			InitKeyMap();
			ResetShortcutInMenu();

			Font = SystemInformation.MenuFont;
			AllowDrop = true;
			DragEnter += Form_DragEnter;
			DragDrop += Form_DragDrop;
			Shown += Form_Shown;
			Icon = Resource.AppIcon;

			_Azuki.UseCtrlTabToMoveFocus = false;
			_Azuki.CaretMoved += _Azuki_CaretMoved;
			_Azuki.ColorScheme.SetColor( CharClass.Number,
										 Color.Red,
										 Color.Transparent );

			_SearchPanel.SetFont( this.Font );
			_TabPanel.ActiveTabBackColor = _Azuki.ColorScheme.LineNumberBack;
			_TimerForDelayedActivatedEvent.Interval = 100;
			_TimerForDelayedActivatedEvent.Tick += Form_DelayedActivated;
		}
		#endregion

		#region UI Access
		/// <summary>
		/// Gets Azuki control.
		/// </summary>
		public AzukiControl Azuki
		{
			get{ return _Azuki; }
		}

		/// <summary>
		/// Gets text search panel.
		/// </summary>
		public SearchPanel SearchPanel
		{
			get{ return _SearchPanel; }
		}

		/// <summary>
		/// Gets tab panel.
		/// </summary>
		public TabPanel<Document> TabPanel
		{
			get{ return _TabPanel; }
		}

		/// <summary>
		/// Gets or sets whether tab panel is enabled or not.
		/// </summary>
		public bool TabPanelEnabled
		{
			get{ return _TabPanel.Visible; }
			set
			{
				_TabPanel.Visible = value;
				_MI_View_ShowTabPanel.Checked = value;
			}
		}

		/// <summary>
		/// Activates and set focus to text search panel.
		/// </summary>
		public void ActivateSearchPanel()
		{
			_SearchPanel.Activate( _Azuki.CaretIndex );
		}

		/// <summary>
		/// Deactivates and set focus to text search panel.
		/// </summary>
		public void DeactivateSearchPanel()
		{
			_SearchPanel.Deactivate();
			_Azuki.Focus();
		}

		/// <summary>
		/// Updates text and state of each UI component.
		/// </summary>
		public void UpdateUI()
		{
			Document doc = _App.ActiveDocument;
			bool readOnly = doc.IsReadOnly;
			StringBuilder text = new StringBuilder( 64 );

			// set form text
			text.Append( "Ann - " );
			text.Append( doc.DisplayNameWithFlags );
			text.Append( " [" );
			text.Append( Utl.ToString(doc.Encoding) );
			if( doc.WithBom )
			{
				text.Append( "+BOM" );
			}
			text.Append( ", " + doc.FileType.Name );
			if( readOnly )
			{
				text.Append( ", R/O" );
			}
			text.Append( "]" );
			this.Text = text.ToString();

			// update status bar
			switch( doc.SelectionMode )
			{
				case TextDataType.Line:
					_Status_SelectionMode.Text = "LINE";
					break;
				case TextDataType.Rectangle:
					_Status_SelectionMode.Text = "RECT";
					break;
				case TextDataType.Words:
					_Status_SelectionMode.Text = "WORD";
					break;
				default:
					_Status_SelectionMode.Text = "";
					break;
			}
			_Status_InsertionMode.Text = (Azuki.IsOverwriteMode) ? "O/W"
																 : "Ins";
			_Azuki_CaretMoved( this, EventArgs.Empty ); // update caret pos

			// apply read-only mode
			_MI_File_ReadOnly.Checked = readOnly;
			_MI_File_Save.Enabled = _MI_Edit_Undo.Enabled
								  = _MI_Edit_Redo.Enabled
								  = _MI_Edit_Cut.Enabled
								  = _MI_Edit_Paste.Enabled = !readOnly;

			// apply wrap-line mode
			_MI_View_WrapLines.Checked = (_Azuki.ViewType == ViewType.WrappedProportional);

			// update radio check of EOL code menu items
			if( _Azuki.Document.EolCode == "\r\n" )
			{
				_MI_Edit_EolCode_CRLF.Checked = true;
				_MI_Edit_EolCode_LF.Checked = false;
				_MI_Edit_EolCode_CR.Checked = false;
			}
			else if( _Azuki.Document.EolCode == "\n" )
			{
				_MI_Edit_EolCode_CRLF.Checked = false;
				_MI_Edit_EolCode_LF.Checked = true;
				_MI_Edit_EolCode_CR.Checked = false;
			}
			else
			{
				_MI_Edit_EolCode_CRLF.Checked = false;
				_MI_Edit_EolCode_LF.Checked = false;
				_MI_Edit_EolCode_CR.Checked = true;
			}

			// Disable encoding menu if the document is newly created one
			_MI_File_Encoding_Auto.Enabled = (doc.FilePath != null);

			// Update check of encoding menu
			foreach( ToolStripMenuItem mi in _MI_File_Encoding.DropDownItems)
			{
				mi.Checked = false;
			}
			ToolStripMenuItem encMI;
			switch( _App.ActiveDocument.Encoding.WebName )
			{
				case "euc-jp":
					encMI = _MI_File_Encoding_EUCJP;
					break;
				case "iso-2022-jp":
					encMI = _MI_File_Encoding_JIS;
					break;
				case "utf-8":
					encMI = (_App.ActiveDocument.WithBom) ? _MI_File_Encoding_UTF8B
														  : _MI_File_Encoding_UTF8;
					break;
				case "utf-16":
					encMI = (_App.ActiveDocument.WithBom) ? _MI_File_Encoding_UTF16LEB
														  : _MI_File_Encoding_UTF16LE;
					break;
				case "unicodeFFFE":
					encMI = (_App.ActiveDocument.WithBom) ? _MI_File_Encoding_UTF16BEB
														  : _MI_File_Encoding_UTF16BE;
					break;
				default:
					encMI = _MI_File_Encoding_SJIS;
					break;
			}
			encMI.Checked = true;

			// update radio check of file type menu
			foreach( ToolStripMenuItem mi in _MI_Mode.DropDownItems)
			{
				mi.Checked = false;
			}
			switch( _App.ActiveDocument.FileType.Name )
			{
				case FileType.BatchFileTypeName:
					_MI_Mode_BatchFile.Checked = true;
					break;
				case FileType.CppFileTypeName:
					_MI_Mode_Cpp.Checked = true;
					break;
				case FileType.CSharpFileTypeName:
					_MI_Mode_CSharp.Checked = true;
					break;
				case FileType.DiffFileTypeName:
					_MI_Mode_Diff.Checked = true;
					break;
				case FileType.IniFileTypeName:
					_MI_Mode_Ini.Checked = true;
					break;
				case FileType.JavaFileTypeName:
					_MI_Mode_Java.Checked = true;
					break;
				case FileType.JavaScriptFileTypeName:
					_MI_Mode_JavaScript.Checked = true;
					break;
				case FileType.LatexFileTypeName:
					_MI_Mode_Latex.Checked = true;
					break;
				case FileType.PythonFileTypeName:
					_MI_Mode_Python.Checked = true;
					break;
				case FileType.RubyFileTypeName:
					_MI_Mode_Ruby.Checked = true;
					break;
				case FileType.XmlFileTypeName:
					_MI_Mode_XML.Checked = true;
					break;
				default:
					_MI_Mode_Text.Checked = true;
					break;
			}

			// update tab panel
			_TabPanel.Invalidate();
		}
		#endregion

		#region Action Mapping
		void HandleMenuAction( object sender, EventArgs e )
		{
			Debug.Assert( sender is ToolStripMenuItem );
			AnnAction action;

			ToolStripMenuItem mi = (ToolStripMenuItem)sender;
			if( _MenuMap.TryGetValue(mi, out action) )
			{
				action( _App );
			}
		}

		void HandleKeyAction( object sender, KeyEventArgs e )
		{
			AnnAction action;

			if( _Azuki.GetKeyBind(e.KeyData) == null )
			{
				if( _KeyMap.TryGetValue(e.KeyData, out action) )
				{
					action( _App );
				}
			}
		}

		void InitMenuMap()
		{
			_MenuMap[ _MI_File_New ]		= Actions.CreateNewDocument;
			_MenuMap[ _MI_File_Open ]		= Actions.OpenDocument;
			_MenuMap[ _MI_File_Save ]		= Actions.SaveDocument;
			_MenuMap[ _MI_File_SaveAs ]		= Actions.SaveDocumentAs;
			_MenuMap[ _MI_File_Encoding_Auto ]		= Actions.ChangeEncoding_Auto;
			_MenuMap[ _MI_File_Encoding_SJIS ]		= Actions.ChangeEncoding_SJIS;
			_MenuMap[ _MI_File_Encoding_JIS ]		= Actions.ChangeEncoding_JIS;
			_MenuMap[ _MI_File_Encoding_EUCJP ]		= Actions.ChangeEncoding_EUCJP;
			_MenuMap[ _MI_File_Encoding_UTF8 ]		= Actions.ChangeEncoding_UTF8;
			_MenuMap[ _MI_File_Encoding_UTF8B ]		= Actions.ChangeEncoding_UTF8B;
			_MenuMap[ _MI_File_Encoding_UTF16LE ]	= Actions.ChangeEncoding_UTF16LE;
			_MenuMap[ _MI_File_Encoding_UTF16LEB ]	= Actions.ChangeEncoding_UTF16LEB;
			_MenuMap[ _MI_File_Encoding_UTF16BE ]	= Actions.ChangeEncoding_UTF16BE;
			_MenuMap[ _MI_File_Encoding_UTF16BEB ]	= Actions.ChangeEncoding_UTF16BEB;
			_MenuMap[ _MI_File_Close ]		= Actions.CloseDocument;
			_MenuMap[ _MI_File_ReadOnly ]	= Actions.ToggleReadOnlyMode;
			_MenuMap[ _MI_File_OpenSettingsFile ]		= Actions.OpenSettingsFile;
			_MenuMap[ _MI_File_Exit ]		= Actions.Exit;

			_MenuMap[ _MI_Edit_Undo ]		= Actions.Undo;
			_MenuMap[ _MI_Edit_Redo ]		= Actions.Redo;
			_MenuMap[ _MI_Edit_Cut ]		= Actions.Cut;
			_MenuMap[ _MI_Edit_Copy ]		= Actions.Copy;
			_MenuMap[ _MI_Edit_Paste ]		= Actions.Paste;
			_MenuMap[ _MI_Edit_Find ]		= Actions.ShowFindDialog;
			_MenuMap[ _MI_Edit_FindNext ]	= Actions.FindNext;
			_MenuMap[ _MI_Edit_FindPrev ]	= Actions.FindPrev;
			_MenuMap[ _MI_Edit_BlankOp_TrimTrailingSpace ]	= Actions.TrimTrailingSpace;
			_MenuMap[ _MI_Edit_BlankOp_TrimLeadingSpace ]	= Actions.TrimLeadingSpace;
			_MenuMap[ _MI_Edit_BlankOp_ConvertTabsToSpaces ]= Actions.ConvertTabsToSpaces;
			_MenuMap[ _MI_Edit_BlankOp_ConvertSpacesToTabs ]= Actions.ConvertSpacesToTabs;
			_MenuMap[ _MI_Edit_SelectAll ]	= Actions.SelectAll;
			_MenuMap[ _MI_Edit_GotoLine ]	= Actions.ShowGotoDialog;
			_MenuMap[ _MI_Edit_EolCode_CRLF ]	= Actions.SetEolCodeToCRLF;
			_MenuMap[ _MI_Edit_EolCode_LF ]		= Actions.SetEolCodeToLF;
			_MenuMap[ _MI_Edit_EolCode_CR ]		= Actions.SetEolCodeToCR;
			
			_MenuMap[ _MI_View_ShowSpecialChar ]	= Actions.SelectSpecialCharVisibility;
			_MenuMap[ _MI_View_WrapLines ]			= Actions.ToggleWrapLines;
			_MenuMap[ _MI_View_ShowTabPanel ]		= Actions.ToggleTabPanel;

			_MenuMap[ _MI_Mode_Text ]		= Actions.SetToTextMode;
			_MenuMap[ _MI_Mode_BatchFile ]	= Actions.SetToBatchFileMode;
			_MenuMap[ _MI_Mode_Cpp ]		= Actions.SetToCppMode;
			_MenuMap[ _MI_Mode_CSharp ]		= Actions.SetToCSharpMode;
			_MenuMap[ _MI_Mode_Diff ]		= Actions.SetToDiffMode;
			_MenuMap[ _MI_Mode_Ini ]		= Actions.SetToIniMode;
			_MenuMap[ _MI_Mode_Java ]		= Actions.SetToJavaMode;
			_MenuMap[ _MI_Mode_JavaScript ]	= Actions.SetToJavaScriptMode;
			_MenuMap[ _MI_Mode_Latex ]		= Actions.SetToLatexMode;
			_MenuMap[ _MI_Mode_Python ]		= Actions.SetToPythonMode;
			_MenuMap[ _MI_Mode_Ruby ]		= Actions.SetToRubyMode;
			_MenuMap[ _MI_Mode_XML ]		= Actions.SetToXmlMode;

			_MenuMap[ _MI_Window_Next ]		= Actions.ActivateNextDocument;
			_MenuMap[ _MI_Window_Prev ]		= Actions.ActivatePrevDocument;
			_MenuMap[ _MI_Window_List ]		= Actions.ShowDocumentList;

			_MenuMap[ _MI_Help_MemoryUsage ]	= Actions.ShowMemoryUsage;
			_MenuMap[ _MI_Help_About ]			= Actions.ShowAboutDialog;
		}

		void InitKeyMap()
		{
			_KeyMap[ Keys.F3 ]							= Actions.FindNext;
			_KeyMap[ Keys.F3|Keys.Shift ]				= Actions.FindPrev;

			_KeyMap[ Keys.N|Keys.Control ]				= Actions.CreateNewDocument;
			_KeyMap[ Keys.O|Keys.Control ]				= Actions.OpenDocument;
			_KeyMap[ Keys.S|Keys.Control ]				= Actions.SaveDocument;
			_KeyMap[ Keys.S|Keys.Control|Keys.Shift ]	= Actions.SaveDocumentAs;
			_KeyMap[ Keys.W|Keys.Control ]				= Actions.CloseDocument;
			_KeyMap[ Keys.Q|Keys.Control ]				= Actions.Exit;

			_KeyMap[ Keys.Z|Keys.Control ]				= Actions.Undo;
			_KeyMap[ Keys.Y|Keys.Control ]				= Actions.Redo;
			_KeyMap[ Keys.X|Keys.Control ]				= Actions.Cut;
			_KeyMap[ Keys.C|Keys.Control ]				= Actions.Copy;
			_KeyMap[ Keys.V|Keys.Control ]				= Actions.Paste;
			_KeyMap[ Keys.F|Keys.Control ]				= Actions.ShowFindDialog;
			_KeyMap[ Keys.G|Keys.Control ]				= Actions.FindNext;
			_KeyMap[ Keys.G|Keys.Control|Keys.Shift ]	= Actions.FindPrev;
			_KeyMap[ Keys.A|Keys.Control ]				= Actions.SelectAll;
			_KeyMap[ Keys.L|Keys.Control ]				= Actions.ShowGotoDialog;
			_KeyMap[ Keys.R|Keys.Control|Keys.Shift ]	= Actions.TrimTrailingSpace;
			_KeyMap[ Keys.L|Keys.Control|Keys.Shift ]	= Actions.TrimLeadingSpace;
			_KeyMap[ Keys.P|Keys.Control|Keys.Shift ]	= Actions.ConvertTabsToSpaces;
			_KeyMap[ Keys.T|Keys.Control|Keys.Shift ]	= Actions.ConvertSpacesToTabs;

			_KeyMap[ Keys.PageDown|Keys.Control ]		= Actions.ActivateNextDocument;
			_KeyMap[ Keys.PageUp|Keys.Control ]			= Actions.ActivatePrevDocument;
			_KeyMap[ Keys.D|Keys.Control ]				= Actions.ShowDocumentList;

			_KeyMap[ Keys.Tab|Keys.Control ]			= Actions.ActivateNextDocument;
			_KeyMap[ Keys.Tab|Keys.Control|Keys.Shift ]	= Actions.ActivatePrevDocument;
		}
		#endregion

		#region GUI Event Handlers
		void _MI_File_Popup( object sender, EventArgs e )
		{
			// Make the 'save' menu item disabled if it's not modified
			_MI_File_Save.Enabled = _Azuki.Document.IsDirty;

			// Refresh MRU menu items
			_MI_File_Mru.DropDownItems.Clear();
			for( int i=0; i<AppConfig.MruFiles.Count; i++ )
			{
				ToolStripMenuItem mi = new ToolStripMenuItem();
				mi.Text = "&" + i + " " + AppConfig.MruFiles[i].Path;
				mi.Click += _MI_File_Mru_Foo_Clicked;
				_MI_File_Mru.DropDownItems.Add( mi );
			}
			_MI_File_Mru.Enabled = (0 < _MI_File_Mru.DropDownItems.Count);
		}

		void _MI_File_Mru_Foo_Clicked( object sender, EventArgs e )
		{
			for( int i=0; i<_MI_File_Mru.DropDownItems.Count; i++ )
			{
				if( _MI_File_Mru.DropDownItems[i] == sender )
				{
					_App.OpenDocument( AppConfig.MruFiles[i].Path );
					break;
				}
			}
		}

		void _MI_Edit_Popup( object sender, EventArgs e )
		{
			_MI_Edit_Undo.Enabled = _Azuki.CanUndo;
			_MI_Edit_Redo.Enabled = _Azuki.CanRedo;
			_MI_Edit_Cut.Enabled = _Azuki.CanCut;
			_MI_Edit_Copy.Enabled = _Azuki.CanCopy;
			_MI_Edit_Paste.Enabled = _Azuki.CanPaste;
		}

		void _MI_Window_Popup( object sender, EventArgs e )
		{
			_MI_Window_Next.Enabled
				= _MI_Window_Prev.Enabled = (1 < _App.Documents.Count);
		}

		void Form_DragDrop( object sender, DragEventArgs e )
		{
			object dropData;

			// if dragging data is not file, ignore
			dropData = e.Data.GetData( DataFormats.FileDrop );
			if( dropData == null )
			{
				return;
			}

			// if there is a file in the data, prepare to accept dropping data
			foreach( string filePath in (string[])dropData )
			{
				if( !Directory.Exists(filePath) )
				{
					// load the file
					_App.OpenDocument( filePath );
				}
			}
		}

		void Form_DragEnter( object sender, DragEventArgs e )
		{
			object dropData;

			e.Effect = DragDropEffects.None;

			// if dragging data is not file, ignore
			dropData = e.Data.GetData( DataFormats.FileDrop );
			if( dropData == null )
			{
				return;
			}

			// if there is a file in the data, prepare to accept dropping data
			foreach( string filePath in (string[])dropData )
			{
				if( !Directory.Exists(filePath) )
				{
					e.Effect = DragDropEffects.Copy;
					break;
				}
			}
		}

		void Form_Shown( object sender, EventArgs e )
		{
			UpdateUI();
		}

		protected override void OnActivated( EventArgs e )
		{
			base.OnActivated( e );
			_TimerForDelayedActivatedEvent.Enabled = true;
		}

		void Form_DelayedActivated( object sender, EventArgs e )
		{
			_TimerForDelayedActivatedEvent.Enabled = false;
			_App.MainForm_DelayedActivated();
		}

		void _Azuki_OverwriteModeChanged( object sender, EventArgs e )
		{
			// update status bar
			UpdateUI();
		}

		void _Azuki_CaretMoved( object sender, EventArgs e )
		{
			int selLen;
			int line, columnInHRuler, columnInChar;

			selLen = _Azuki.GetSelectedTextLength();
			if( 0 < selLen )
			{
				// There are selection. Display how many characters/bytes are selected.
				int charCount;
				string columnCountStr;

				// calculate number of chars/bytes
				charCount = selLen;
				columnCountStr = "?";
				if( charCount < 1 * 1024 * 1024 )
				{
					try
					{
						columnCountStr =
							_App.ActiveDocument.Encoding.GetByteCount(
								_Azuki.GetSelectedText("")
							).ToString("N0");
					}
					catch
					{}
				}

				// Display the number
				_Status_Message.Text = String.Format(
						"{0:N0} chars ({1:N0} bytes) selected.",
						charCount, columnCountStr
					);
			}
			else
			{
				_Status_Message.Text = String.Empty;
			}

			// Display caret position on status bar
			_Azuki.GetLineColumnIndexFromCharIndex(
					_Azuki.CaretIndex, out line, out columnInChar
				);
			columnInHRuler = _Azuki.View.GetVirPosFromIndex( _Azuki.CaretIndex ).X
				/ _Azuki.View.HRulerUnitWidth;
			_Status_CaretPos.Text = String.Format( StatusMsg_CaretPos,
					line+1, columnInHRuler+1, columnInChar+1,
					_Azuki.CaretIndex
				);

			// expand status bar width if its text cannot be displayed
			using( Graphics g = CreateGraphics() )
			{
				int textWidth = (int)g.MeasureString( _Status_CaretPos.Text,
													  this.Font ).Width;
				if( _Status_CaretPos.Width < textWidth )
				{
					_Status_CaretPos.Width = textWidth;
				}
			}
		}
		#endregion

		#region Other
		public override Font Font
		{
			get{ return base.Font; }
			set
			{
				base.Font = value;
				using( Graphics g = CreateGraphics() )
				{
					// update size of status panels
					string sampleText = String.Format(
							StatusMsg_CaretPos, 9999, 9999, 9999, 9999
						);
					_Status_CaretPos.Width = (int)g.MeasureString(
							sampleText, value
						).Width;
					_Status_SelectionMode.Width = (int)g.MeasureString(
							"WORD|", value
						).Width;
					_Status_InsertionMode.Width = (int)g.MeasureString(
							"O/W|", value
						).Width;
				}
			}
		}
		#endregion

		#region UI Component Initialization
		void InitUIComponent()
		{
			_Azuki = new AzukiControl();
			SuspendLayout();
			// 
			// _Azuki
			// 
			_Azuki.Dock = DockStyle.Fill;
			_Azuki.TabWidth = 8;
			_Azuki.BorderStyle = BorderStyle.None;
			_Azuki.KeyDown += HandleKeyAction;
			_Azuki.GotFocus += delegate {
				DeactivateSearchPanel();
			};
			_Azuki.OverwriteModeChanged += _Azuki_OverwriteModeChanged;
			//
			// _TabPanel
			//
			_TabPanel.Dock = DockStyle.Top;
			//
			// _SearchPanel
			//
			_SearchPanel.Dock = DockStyle.Bottom;
			_SearchPanel.Enabled = false;
			//
			// _StatusBar
			//
			_StatusBar.Dock = DockStyle.Bottom;
			#if NET6_0
			_StatusBar.Items.AddRange( new ToolStripStatusLabel[] {
				_Status_Message, _Status_CaretPos,
				_Status_SelectionMode, _Status_InsertionMode
			});
			_Status_Message.BorderSides = ToolStripStatusLabelBorderSides.Right;
			_Status_CaretPos.BorderSides = ToolStripStatusLabelBorderSides.Right;
			_Status_SelectionMode.BorderSides = ToolStripStatusLabelBorderSides.Right;
			_Status_InsertionMode.BorderSides = ToolStripStatusLabelBorderSides.Right;
			#else
			_StatusBar.Panels.AddRange( new StatusBarPanel[] {
				_Status_Message, _Status_CaretPos,
				_Status_SelectionMode, _Status_InsertionMode
			});
			_StatusBar.ShowPanels = true;
			_StatusBar.SizingGrip = true;
			#endif
			//
			// _Status_CaretPos
			//
			#if NET6_0
			_Status_CaretPos.Alignment = ToolStripItemAlignment.Right;
			#else
			_Status_CaretPos.Alignment = HorizontalAlignment.Right;
			#endif
			//
			// _Status_Message
			//
			#if NET6_0
			_Status_Message.Spring = true;
			#else
			_Status_Message.AutoSize = StatusBarPanelAutoSize.Spring;
			#endif
			//
			// _MI_File
			//
			_MI_File.DropDownOpened += _MI_File_Popup;
			//
			// _MI_Edit
			//
			_MI_Edit.DropDownOpened += _MI_Edit_Popup;
			//
			// _MI_Window
			//
			_MI_Window.DropDownOpened += _MI_Window_Popup;
			//
			// AnnForm
			// 
			ClientSize = new Size( 360, 400 );
			Controls.Add( _Azuki );
			Controls.Add( _TabPanel );
			Controls.Add( _SearchPanel );
			Controls.Add( _StatusBar );
			Controls.Add(_MainMenu);
			Text = "Ann";
			ResumeLayout( false );
		}

		void InitMenuComponents()
		{
			// construct root menu structure
			_MainMenu.Items.Add( _MI_File );
			_MainMenu.Items.Add( _MI_Edit );
			_MainMenu.Items.Add( _MI_View );
			_MainMenu.Items.Add( _MI_Mode );
			_MainMenu.Items.Add( _MI_Window );
			_MainMenu.Items.Add( _MI_Help );

			// construct descendant menu structure
			_MI_File.DropDownItems.Add( _MI_File_New );
			_MI_File.DropDownItems.Add( _MI_File_Open );
			_MI_File.DropDownItems.Add( _MI_File_Save );
			_MI_File.DropDownItems.Add( _MI_File_SaveAs );
			_MI_File.DropDownItems.Add( _MI_File_Encoding );
			_MI_File_Encoding.DropDownItems.Add( _MI_File_Encoding_Auto );
			_MI_File_Encoding.DropDownItems.Add( _MI_File_Encoding_SJIS );
			_MI_File_Encoding.DropDownItems.Add( _MI_File_Encoding_JIS );
			_MI_File_Encoding.DropDownItems.Add( _MI_File_Encoding_EUCJP );
			_MI_File_Encoding.DropDownItems.Add( _MI_File_Encoding_UTF8 );
			_MI_File_Encoding.DropDownItems.Add( _MI_File_Encoding_UTF8B );
			_MI_File_Encoding.DropDownItems.Add( _MI_File_Encoding_UTF16LE );
			_MI_File_Encoding.DropDownItems.Add( _MI_File_Encoding_UTF16LEB );
			_MI_File_Encoding.DropDownItems.Add( _MI_File_Encoding_UTF16BE );
			_MI_File_Encoding.DropDownItems.Add( _MI_File_Encoding_UTF16BEB );
			_MI_File.DropDownItems.Add( _MI_File_Close );
			_MI_File.DropDownItems.Add( _MI_File_Sep1 );
			_MI_File.DropDownItems.Add( _MI_File_ReadOnly );
			_MI_File.DropDownItems.Add( _MI_File_Sep2 );
			_MI_File.DropDownItems.Add( _MI_File_Mru );
			_MI_File.DropDownItems.Add( _MI_File_OpenSettingsFile );
			_MI_File.DropDownItems.Add( _MI_File_Sep3 );
			_MI_File.DropDownItems.Add( _MI_File_Exit );

			_MI_Edit.DropDownItems.Add( _MI_Edit_Undo );
			_MI_Edit.DropDownItems.Add( _MI_Edit_Redo );
			_MI_Edit.DropDownItems.Add( _MI_Edit_Sep0 );
			_MI_Edit.DropDownItems.Add( _MI_Edit_Cut );
			_MI_Edit.DropDownItems.Add( _MI_Edit_Copy );
			_MI_Edit.DropDownItems.Add( _MI_Edit_Paste );
			_MI_Edit.DropDownItems.Add( _MI_Edit_Sep1 );
			_MI_Edit.DropDownItems.Add( _MI_Edit_Find );
			_MI_Edit.DropDownItems.Add( _MI_Edit_FindNext );
			_MI_Edit.DropDownItems.Add( _MI_Edit_FindPrev );
			_MI_Edit.DropDownItems.Add( _MI_Edit_Sep2 );
			_MI_Edit.DropDownItems.Add( _MI_Edit_SelectAll );
			_MI_Edit.DropDownItems.Add( _MI_Edit_GotoLine );
			_MI_Edit.DropDownItems.Add( _MI_Edit_Sep3 );
			_MI_Edit.DropDownItems.Add( _MI_Edit_EolCode );
			_MI_Edit_EolCode.DropDownItems.Add( _MI_Edit_EolCode_CRLF );
			_MI_Edit_EolCode.DropDownItems.Add( _MI_Edit_EolCode_LF );
			_MI_Edit_EolCode.DropDownItems.Add( _MI_Edit_EolCode_CR );
			_MI_Edit.DropDownItems.Add( _MI_Edit_BlankOp );
			_MI_Edit_BlankOp.DropDownItems.Add( _MI_Edit_BlankOp_TrimTrailingSpace );
			_MI_Edit_BlankOp.DropDownItems.Add( _MI_Edit_BlankOp_TrimLeadingSpace );
			_MI_Edit_BlankOp.DropDownItems.Add( _MI_Edit_BlankOp_ConvertTabsToSpaces );
			_MI_Edit_BlankOp.DropDownItems.Add( _MI_Edit_BlankOp_ConvertSpacesToTabs );

			_MI_View.DropDownItems.Add( _MI_View_ShowSpecialChar );
			_MI_View.DropDownItems.Add( _MI_View_WrapLines );
			_MI_View.DropDownItems.Add( _MI_View_ShowTabPanel );

			_MI_Mode.DropDownItems.Add( _MI_Mode_Text );
			_MI_Mode.DropDownItems.Add( _MI_Mode_BatchFile );
			_MI_Mode.DropDownItems.Add( _MI_Mode_Cpp );
			_MI_Mode.DropDownItems.Add( _MI_Mode_CSharp );
			_MI_Mode.DropDownItems.Add( _MI_Mode_Diff );
			_MI_Mode.DropDownItems.Add( _MI_Mode_Ini );
			_MI_Mode.DropDownItems.Add( _MI_Mode_Java );
			_MI_Mode.DropDownItems.Add( _MI_Mode_JavaScript );
			_MI_Mode.DropDownItems.Add( _MI_Mode_Latex );
			_MI_Mode.DropDownItems.Add( _MI_Mode_Python );
			_MI_Mode.DropDownItems.Add( _MI_Mode_Ruby );
			_MI_Mode.DropDownItems.Add( _MI_Mode_XML );

			_MI_Window.DropDownItems.Add( _MI_Window_Next );
			_MI_Window.DropDownItems.Add( _MI_Window_Prev );
			_MI_Window.DropDownItems.Add( _MI_Window_List );

			_MI_Help.DropDownItems.Add( _MI_Help_MemoryUsage );
			_MI_Help.DropDownItems.Add( _MI_Help_About );

			// menu labels
			_MI_File.Text = "&File";
			_MI_File_New.Text = "&New...";
			_MI_File_Open.Text = "&Open...";
			_MI_File_Save.Text = "&Save";
			_MI_File_SaveAs.Text = "Save &as...";
			_MI_File_Encoding.Text = "Encodin&g";
			_MI_File_Encoding_Auto.Text = "(&Auto-detect)";
			_MI_File_Encoding_SJIS.Text = "&Shift_JIS";
			_MI_File_Encoding_JIS.Text = "&JIS (iso-2022-jp)";
			_MI_File_Encoding_EUCJP.Text = "&EUC-JP";
			_MI_File_Encoding_UTF8.Text = "UTF-&8";
			_MI_File_Encoding_UTF8B.Text = "UTF-&8 + BOM";
			_MI_File_Encoding_UTF16LE.Text = "&UTF-16";
			_MI_File_Encoding_UTF16LEB.Text = "&UTF-16 + BOM";
			_MI_File_Encoding_UTF16BE.Text = "&UTF-16 (Big Endian)";
			_MI_File_Encoding_UTF16BEB.Text = "&UTF-16 + BOM (Big Endian)";
			_MI_File_Close.Text = "&Close";
			_MI_File_Sep1.Text = "-";
			_MI_File_ReadOnly.Text = "Read onl&y";
			_MI_File_Sep2.Text = "-";
			_MI_File_Mru.Text = "Recent &files";
			_MI_File_OpenSettingsFile.Text = "Open Se&ttings File";
			_MI_File_Sep3.Text = "-";
			_MI_File_Exit.Text = "E&xit";
			_MI_Edit.Text = "&Edit";
			_MI_Edit_Undo.Text = "&Undo";
			_MI_Edit_Redo.Text = "&Redo";
			_MI_Edit_Sep0.Text = "-";
			_MI_Edit_Cut.Text = "Cu&t";
			_MI_Edit_Copy.Text = "&Copy";
			_MI_Edit_Paste.Text = "&Paste";
			_MI_Edit_Sep1.Text = "-";
			_MI_Edit_Find.Text = "&Find...";
			_MI_Edit_FindNext.Text = "Find &next";
			_MI_Edit_FindPrev.Text = "Find &previous";
			_MI_Edit_Sep2.Text = "-";
			_MI_Edit_SelectAll.Text = "Select &All";
			_MI_Edit_GotoLine.Text = "&Goto line...";
			_MI_Edit_Sep3.Text = "-";
			_MI_Edit_EolCode.Text = "Set &line end code";
			_MI_Edit_EolCode_CRLF.Text = "&CR+LF";
			_MI_Edit_EolCode_LF.Text = "&LF";
			_MI_Edit_EolCode_CR.Text = "C&R";
			_MI_Edit_BlankOp.Text = "&Blank Operation";
			_MI_Edit_BlankOp_TrimTrailingSpace.Text = "Trim &trailing space";
			_MI_Edit_BlankOp_TrimLeadingSpace.Text = "Trim &leading space";
			_MI_Edit_BlankOp_ConvertTabsToSpaces.Text = "Convert tabs to &spaces";
			_MI_Edit_BlankOp_ConvertSpacesToTabs.Text = "Convert spaces to &tabs";
			_MI_View.Text = "&View";
			_MI_View_ShowSpecialChar.Text = "Show &Special Chars...";
			_MI_View_WrapLines.Text = "&Wrap lines";
			_MI_View_ShowTabPanel.Text = "Show &tab panel";
			_MI_Mode.Text = "&Mode";
			_MI_Mode_Text.Text = "&Text";
			_MI_Mode_BatchFile.Text = "&Batch file";
			_MI_Mode_Cpp.Text = "&C/C++";
			_MI_Mode_CSharp.Text = "C&#";
			_MI_Mode_Diff.Text = "&Diff";
			_MI_Mode_Ini.Text = "&INI / Properties";
			_MI_Mode_Java.Text = "&Java";
			_MI_Mode_JavaScript.Text = "Java&Script";
			_MI_Mode_Latex.Text = "&LaTeX";
			_MI_Mode_Python.Text = "&Python";
			_MI_Mode_Ruby.Text = "&Ruby";
			_MI_Mode_XML.Text = "&XML";
			_MI_Window.Text = "&Window";
			_MI_Window_Next.Text = "&Next window";
			_MI_Window_Prev.Text = "&Previous window";
			_MI_Window_List.Text = "Show &window list...";
			_MI_Help.Text = "&Help";
			_MI_Help_MemoryUsage.Text = "Show &memory usage...";
			_MI_Help_About.Text = "&About...";

			// other menu settings
			_MI_Edit_EolCode_CRLF.Checked = true;
			_MI_Edit_EolCode_LF.Checked = true;
			_MI_Edit_EolCode_CR.Checked = true;

			// bind menu actions
			EventHandler menuActionHandler = this.HandleMenuAction;
			foreach( ToolStripMenuItem mi in _MainMenu.Items )
			{
				foreach( ToolStripMenuItem mi2 in mi.DropDownItems )
				{
					foreach( ToolStripMenuItem mi3 in mi2.DropDownItems)
					{
						foreach( ToolStripMenuItem mi4 in mi3.DropDownItems)
						{
							mi4.Click += menuActionHandler;
						}
						mi3.Click += menuActionHandler;
					}
					mi2.Click += menuActionHandler;
				}
			}

			// set menu
			MainMenuStrip = _MainMenu;
		}

		void ResetShortcutInMenu()
		{
			string newText;
			ToolStripMenuItem mi;

			// find matched pair that both pair has same action from two dictionary
			foreach( KeyValuePair<Keys, AnnAction> keyEntry in _KeyMap )
			{
				foreach( KeyValuePair<ToolStripMenuItem, AnnAction> menuEntry in _MenuMap )
				{
					// has same action?
					if( keyEntry.Value != menuEntry.Value )
					{
						continue;
					}

					// ok, a pair was found.
					// reset menu text with found shortcut-key.
					mi = menuEntry.Key;
					mi.ShortcutKeys = keyEntry.Key;

					break;
				}
			}
		}
		#endregion

		#region UI Components
		MenuStrip _MainMenu			= new MenuStrip();
		ToolStripMenuItem _MI_File			= new ToolStripMenuItem();
		ToolStripMenuItem _MI_File_New		= new ToolStripMenuItem();
		ToolStripMenuItem _MI_File_Open		= new ToolStripMenuItem();
		ToolStripMenuItem _MI_File_Save		= new ToolStripMenuItem();
		ToolStripMenuItem _MI_File_SaveAs	= new ToolStripMenuItem();
		ToolStripMenuItem _MI_File_Encoding			= new ToolStripMenuItem();
		ToolStripMenuItem _MI_File_Encoding_Auto		= new ToolStripMenuItem();
		ToolStripMenuItem _MI_File_Encoding_SJIS		= new ToolStripMenuItem();
		ToolStripMenuItem _MI_File_Encoding_JIS		= new ToolStripMenuItem();
		ToolStripMenuItem _MI_File_Encoding_EUCJP	= new ToolStripMenuItem();
		ToolStripMenuItem _MI_File_Encoding_UTF8		= new ToolStripMenuItem();
		ToolStripMenuItem _MI_File_Encoding_UTF8B	= new ToolStripMenuItem();
		ToolStripMenuItem _MI_File_Encoding_UTF16LE	= new ToolStripMenuItem();
		ToolStripMenuItem _MI_File_Encoding_UTF16LEB	= new ToolStripMenuItem();
		ToolStripMenuItem _MI_File_Encoding_UTF16BE	= new ToolStripMenuItem();
		ToolStripMenuItem _MI_File_Encoding_UTF16BEB	= new ToolStripMenuItem();
		ToolStripMenuItem _MI_File_Close		= new ToolStripMenuItem();
		ToolStripMenuItem _MI_File_Sep1		= new ToolStripMenuItem();
		ToolStripMenuItem _MI_File_ReadOnly	= new ToolStripMenuItem();
		ToolStripMenuItem _MI_File_OpenSettingsFile	= new ToolStripMenuItem();
		ToolStripMenuItem _MI_File_Sep2		= new ToolStripMenuItem();
		ToolStripMenuItem _MI_File_Mru		= new ToolStripMenuItem();
		ToolStripMenuItem _MI_File_Sep3		= new ToolStripMenuItem();
		ToolStripMenuItem _MI_File_Exit		= new ToolStripMenuItem();
		ToolStripMenuItem _MI_Edit			= new ToolStripMenuItem();
		ToolStripMenuItem _MI_Edit_Undo		= new ToolStripMenuItem();
		ToolStripMenuItem _MI_Edit_Redo		= new ToolStripMenuItem();
		ToolStripMenuItem _MI_Edit_Sep0		= new ToolStripMenuItem();
		ToolStripMenuItem _MI_Edit_Cut		= new ToolStripMenuItem();
		ToolStripMenuItem _MI_Edit_Copy		= new ToolStripMenuItem();
		ToolStripMenuItem _MI_Edit_Paste		= new ToolStripMenuItem();
		ToolStripMenuItem _MI_Edit_Sep1		= new ToolStripMenuItem();
		ToolStripMenuItem _MI_Edit_Find		= new ToolStripMenuItem();
		ToolStripMenuItem _MI_Edit_FindNext	= new ToolStripMenuItem();
		ToolStripMenuItem _MI_Edit_FindPrev	= new ToolStripMenuItem();
		ToolStripMenuItem _MI_Edit_Sep2		= new ToolStripMenuItem();
		ToolStripMenuItem _MI_Edit_SelectAll	= new ToolStripMenuItem();
		ToolStripMenuItem _MI_Edit_GotoLine	= new ToolStripMenuItem();
		ToolStripMenuItem _MI_Edit_Sep3		= new ToolStripMenuItem();
		ToolStripMenuItem _MI_Edit_EolCode		= new ToolStripMenuItem();
		ToolStripMenuItem _MI_Edit_EolCode_CRLF	= new ToolStripMenuItem();
		ToolStripMenuItem _MI_Edit_EolCode_LF	= new ToolStripMenuItem();
		ToolStripMenuItem _MI_Edit_EolCode_CR	= new ToolStripMenuItem();
		ToolStripMenuItem _MI_Edit_BlankOp						= new ToolStripMenuItem();
		ToolStripMenuItem _MI_Edit_BlankOp_TrimTrailingSpace		= new ToolStripMenuItem();
		ToolStripMenuItem _MI_Edit_BlankOp_TrimLeadingSpace		= new ToolStripMenuItem();
		ToolStripMenuItem _MI_Edit_BlankOp_ConvertTabsToSpaces	= new ToolStripMenuItem();
		ToolStripMenuItem _MI_Edit_BlankOp_ConvertSpacesToTabs	= new ToolStripMenuItem();
		ToolStripMenuItem _MI_View					= new ToolStripMenuItem();
		ToolStripMenuItem _MI_View_ShowSpecialChar	= new ToolStripMenuItem();
		ToolStripMenuItem _MI_View_WrapLines			= new ToolStripMenuItem();
		ToolStripMenuItem _MI_View_ShowTabPanel		= new ToolStripMenuItem();
		ToolStripMenuItem _MI_Mode			= new ToolStripMenuItem();
		ToolStripMenuItem _MI_Mode_Text		= new ToolStripMenuItem();
		ToolStripMenuItem _MI_Mode_BatchFile	= new ToolStripMenuItem();
		ToolStripMenuItem _MI_Mode_Cpp		= new ToolStripMenuItem();
		ToolStripMenuItem _MI_Mode_CSharp	= new ToolStripMenuItem();
		ToolStripMenuItem _MI_Mode_Diff		= new ToolStripMenuItem();
		ToolStripMenuItem _MI_Mode_Ini		= new ToolStripMenuItem();
		ToolStripMenuItem _MI_Mode_Java		= new ToolStripMenuItem();
		ToolStripMenuItem _MI_Mode_JavaScript= new ToolStripMenuItem();
		ToolStripMenuItem _MI_Mode_Latex		= new ToolStripMenuItem();
		ToolStripMenuItem _MI_Mode_Python	= new ToolStripMenuItem();
		ToolStripMenuItem _MI_Mode_Ruby		= new ToolStripMenuItem();
		ToolStripMenuItem _MI_Mode_XML		= new ToolStripMenuItem();
		ToolStripMenuItem _MI_Window			= new ToolStripMenuItem();
		ToolStripMenuItem _MI_Window_Next	= new ToolStripMenuItem();
		ToolStripMenuItem _MI_Window_Prev	= new ToolStripMenuItem();
		ToolStripMenuItem _MI_Window_List	= new ToolStripMenuItem();
		ToolStripMenuItem _MI_Help				= new ToolStripMenuItem();
		ToolStripMenuItem _MI_Help_MemoryUsage	= new ToolStripMenuItem();
		ToolStripMenuItem _MI_Help_About			= new ToolStripMenuItem();
		AzukiControl _Azuki;
		TabPanel<Document> _TabPanel = new TabPanel<Document>();
		SearchPanel _SearchPanel = new SearchPanel();
		#if NET6_0
		StatusStrip _StatusBar = new StatusStrip();
		ToolStripStatusLabel _Status_Message = new ToolStripStatusLabel();
		ToolStripStatusLabel _Status_CaretPos = new ToolStripStatusLabel();
		ToolStripStatusLabel _Status_SelectionMode = new ToolStripStatusLabel();
		ToolStripStatusLabel _Status_InsertionMode = new ToolStripStatusLabel();
		#else
		StatusBar _StatusBar = new StatusBar();
		StatusBarPanel _Status_Message = new StatusBarPanel();
		StatusBarPanel _Status_CaretPos = new StatusBarPanel();
		StatusBarPanel _Status_SelectionMode = new StatusBarPanel();
		StatusBarPanel _Status_InsertionMode = new StatusBarPanel();
		#endif
		#endregion

		#region Utilities
		public static class Utl
		{
			public static string ToString( Keys keyData )
			{
				StringBuilder text = new StringBuilder();

				if( (keyData & Keys.Control) != 0 )
				{
					text.Append( "Ctrl+" );
				}
				if( (keyData & Keys.Alt) != 0 )
				{
					text.Append( "Alt+" );
				}
				if( (keyData & Keys.Shift) != 0 )
				{
					text.Append( "Shift+" );
				}
				text.Append( keyData & (~Keys.Modifiers) );

				return text.ToString();
			}

			public static string ToString( Encoding encoding )
			{
				if( encoding == Encoding.UTF8 )
				{
					return "UTF-8";
				}
				else if( encoding == Encoding.UTF7 )
				{
					return "UTF-7";
				}
				else if( encoding == Encoding.Unicode )
				{
					return "UTF-16";
				}
				else if( encoding == Encoding.BigEndianUnicode )
				{
					return "UTF-16BE";
				}
				else if( encoding.WebName == "shift_jis" )
				{
					return "Shift_JIS";
				}
				else if( encoding.WebName == "euc-jp" )
				{
					return "EUC-JP";
				}
				else if( encoding.WebName == "iso-2022-jp" )
				{
					return "JIS";
				}

				return encoding.WebName;
			}
		}
		#endregion
	}
}
