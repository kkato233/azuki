// 2009-11-15
using System;
using System.Drawing;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using Sgry.Azuki;
using Sgry.Azuki.Windows;
using Path = System.IO.Path;
using File = System.IO.File;
using Directory = System.IO.Directory;
using Debug = System.Diagnostics.Debug;

namespace Sgry.Ann
{
	class AnnForm : Form
	{
		#region Fields
		AppLogic _App;
		Dictionary<MenuItem, AnnAction> _MenuMap = new Dictionary<MenuItem,AnnAction>();
		Dictionary<Keys, AnnAction>	_KeyMap = new Dictionary<Keys, AnnAction>();
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
#			if !PocketPC
			_Azuki.UseCtrlTabToMoveFocus = false;
			Font = SystemInformation.MenuFont;
			AllowDrop = true;
			DragEnter += Form_DragEnter;
			DragDrop += Form_DragDrop;
#			endif
			_SearchPanel.SetFont( this.Font );
			_TabPanel.ActiveTabBackColor = _Azuki.ColorScheme.LineNumberBack;
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

			// apply read-only mode
			_MI_File_ReadOnly.Checked = readOnly;
			_MI_File_Save.Enabled
				= _MI_Edit_Undo.Enabled
				= _MI_Edit_Redo.Enabled
				= _MI_Edit_Cut.Enabled
				= _MI_Edit_Paste.Enabled = !readOnly;

			// apply wrap-line mode
			_MI_View_WrapLines.Checked
				= (_Azuki.ViewType == ViewType.WrappedProportional)
				? true : false;

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

			// update tab panel
			_TabPanel.Invalidate();
		}
		#endregion

		#region Action Mapping
		void HandleMenuAction( object sender, EventArgs e )
		{
			Debug.Assert( sender is MenuItem );
			AnnAction action;

			MenuItem mi = (MenuItem)sender;
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
			// Action of top level menu of CF version is same as File/Save menu item
			// but Ann's menu-action mapping does not handle multiple menu items for same action.
			// Here we directly set menu action to the menu to ensure that mapping logic properly works.
#			if PocketPC
			_MI_Save.Click += delegate{ Actions.SaveDocument(_App); };
#			endif

			_MenuMap[ _MI_File_New ]		= Actions.CreateNewDocument;
			_MenuMap[ _MI_File_Open ]		= Actions.OpenDocument;
			_MenuMap[ _MI_File_Save ]		= Actions.SaveDocument;
			_MenuMap[ _MI_File_SaveAs ]		= Actions.SaveDocumentAs;
			_MenuMap[ _MI_File_Close ]		= Actions.CloseDocument;
			_MenuMap[ _MI_File_ReadOnly ]	= Actions.ToggleReadOnlyMode;
			_MenuMap[ _MI_File_Exit ]		= Actions.Exit;

			_MenuMap[ _MI_Edit_Undo ]		= Actions.Undo;
			_MenuMap[ _MI_Edit_Redo ]		= Actions.Redo;
			_MenuMap[ _MI_Edit_Cut ]		= Actions.Cut;
			_MenuMap[ _MI_Edit_Copy ]		= Actions.Copy;
			_MenuMap[ _MI_Edit_Paste ]		= Actions.Paste;
			_MenuMap[ _MI_Edit_Find ]		= Actions.Find;
			_MenuMap[ _MI_Edit_FindNext ]	= Actions.FindNext;
			_MenuMap[ _MI_Edit_FindPrev ]	= Actions.FindPrev;
			_MenuMap[ _MI_Edit_SelectAll ]	= Actions.SelectAll;
			_MenuMap[ _MI_Edit_EolCode_CRLF ]	= Actions.SetEolCodeToCRLF;
			_MenuMap[ _MI_Edit_EolCode_LF ]		= Actions.SetEolCodeToLF;
			_MenuMap[ _MI_Edit_EolCode_CR ]		= Actions.SetEolCodeToCR;
			
			_MenuMap[ _MI_View_ShowSpecialChar ]	= Actions.SelectSpecialCharVisibility;
			_MenuMap[ _MI_View_WrapLines ]			= Actions.ToggleWrapLines;
			_MenuMap[ _MI_View_ShowTabPanel ]		= Actions.ToggleTabPanel;

			_MenuMap[ _MI_Mode_Text ]		= Actions.SetToTextMode;
			_MenuMap[ _MI_Mode_Latex ]		= Actions.SetToLatexMode;
			_MenuMap[ _MI_Mode_Cpp ]		= Actions.SetToCppMode;
			_MenuMap[ _MI_Mode_CSharp ]		= Actions.SetToCSharpMode;
			_MenuMap[ _MI_Mode_Java ]		= Actions.SetToJavaMode;
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
			_KeyMap[ Keys.F|Keys.Control ]				= Actions.Find;
			_KeyMap[ Keys.G|Keys.Control ]				= Actions.FindNext;
			_KeyMap[ Keys.G|Keys.Control|Keys.Shift ]	= Actions.FindPrev;
			_KeyMap[ Keys.A|Keys.Control ]				= Actions.SelectAll;

			_KeyMap[ Keys.PageDown|Keys.Control ]		= Actions.ActivateNextDocument;
			_KeyMap[ Keys.PageUp|Keys.Control ]			= Actions.ActivatePrevDocument;
			_KeyMap[ Keys.D|Keys.Control ]				= Actions.ShowDocumentList;

			_KeyMap[ Keys.Tab|Keys.Control ]			= Actions.ActivateNextDocument;
			_KeyMap[ Keys.Tab|Keys.Control|Keys.Shift ]	= Actions.ActivatePrevDocument;
		}
		#endregion

		#region GUI Event Handlers
#		if !PocketPC
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
					_App.OpenDocument( filePath, null, false );
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
#		endif
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
			//
			// _TabPanel
			//
			_TabPanel.Dock = DockStyle.Top;
			//
			// _SearchPanel
			//
			_SearchPanel.Dock = DockStyle.Bottom;
			_SearchPanel.Enabled = false;
			_SearchPanel.PatternFixed += delegate {
				DeactivateSearchPanel();
			};
			//
			// AnnForm
			// 
			ClientSize = new Size( 360, 400 );
			Controls.Add( _Azuki );
			Controls.Add( _TabPanel );
			Controls.Add( _SearchPanel );
			Text = "Ann";
			ResumeLayout( false );
		}

		void InitMenuComponents()
		{
			// construct root menu structure
#			if PocketPC
			_MI_Save.Text = "Save";
			_MI_Menu.Text = "Menu";
			_MainMenu.MenuItems.Add( _MI_Save );
			_MainMenu.MenuItems.Add( _MI_Menu );

			_MI_Menu.MenuItems.Add( _MI_File );
			_MI_Menu.MenuItems.Add( _MI_Edit );
			_MI_Menu.MenuItems.Add( _MI_View );
			_MI_Menu.MenuItems.Add( _MI_Mode );
			_MI_Menu.MenuItems.Add( _MI_Window );
			_MI_Menu.MenuItems.Add( _MI_Help );
#			else
			_MainMenu.MenuItems.Add( _MI_File );
			_MainMenu.MenuItems.Add( _MI_Edit );
			_MainMenu.MenuItems.Add( _MI_View );
			_MainMenu.MenuItems.Add( _MI_Mode );
			_MainMenu.MenuItems.Add( _MI_Window );
			_MainMenu.MenuItems.Add( _MI_Help );
#			endif

			// construct descendant menu structure
			_MI_File.MenuItems.Add( _MI_File_New );
			_MI_File.MenuItems.Add( _MI_File_Open );
			_MI_File.MenuItems.Add( _MI_File_Save );
			_MI_File.MenuItems.Add( _MI_File_SaveAs );
			_MI_File.MenuItems.Add( _MI_File_Close );
			_MI_File.MenuItems.Add( _MI_File_Sep1 );
			_MI_File.MenuItems.Add( _MI_File_ReadOnly );
			_MI_File.MenuItems.Add( _MI_File_Sep2 );
			_MI_File.MenuItems.Add( _MI_File_Exit );

			_MI_Edit.MenuItems.Add( _MI_Edit_Undo );
			_MI_Edit.MenuItems.Add( _MI_Edit_Redo );
			_MI_Edit.MenuItems.Add( _MI_Edit_Sep0 );
			_MI_Edit.MenuItems.Add( _MI_Edit_Cut );
			_MI_Edit.MenuItems.Add( _MI_Edit_Copy );
			_MI_Edit.MenuItems.Add( _MI_Edit_Paste );
			_MI_Edit.MenuItems.Add( _MI_Edit_Sep1 );
			_MI_Edit.MenuItems.Add( _MI_Edit_Find );
			_MI_Edit.MenuItems.Add( _MI_Edit_FindNext );
			_MI_Edit.MenuItems.Add( _MI_Edit_FindPrev );
			_MI_Edit.MenuItems.Add( _MI_Edit_Sep2 );
			_MI_Edit.MenuItems.Add( _MI_Edit_SelectAll );
			_MI_Edit.MenuItems.Add( _MI_Edit_Sep3 );
			_MI_Edit.MenuItems.Add( _MI_Edit_EolCode );
			_MI_Edit_EolCode.MenuItems.Add( _MI_Edit_EolCode_CRLF );
			_MI_Edit_EolCode.MenuItems.Add( _MI_Edit_EolCode_LF );
			_MI_Edit_EolCode.MenuItems.Add( _MI_Edit_EolCode_CR );

			_MI_View.MenuItems.Add( _MI_View_ShowSpecialChar );
			_MI_View.MenuItems.Add( _MI_View_WrapLines );
			_MI_View.MenuItems.Add( _MI_View_ShowTabPanel );

			_MI_Mode.MenuItems.Add( _MI_Mode_Text );
			_MI_Mode.MenuItems.Add( _MI_Mode_Latex );
			_MI_Mode.MenuItems.Add( _MI_Mode_Cpp );
			_MI_Mode.MenuItems.Add( _MI_Mode_CSharp );
			_MI_Mode.MenuItems.Add( _MI_Mode_Java );	
			_MI_Mode.MenuItems.Add( _MI_Mode_Ruby );	
			_MI_Mode.MenuItems.Add( _MI_Mode_XML );

			_MI_Window.MenuItems.Add( _MI_Window_Next );
			_MI_Window.MenuItems.Add( _MI_Window_Prev );
			_MI_Window.MenuItems.Add( _MI_Window_List );

			_MI_Help.MenuItems.Add( _MI_Help_MemoryUsage );
			_MI_Help.MenuItems.Add( _MI_Help_About );

			// menu labels
			_MI_File.Text = "&File";
			_MI_File_New.Text = "&New...";
			_MI_File_Open.Text = "&Open...";
			_MI_File_Save.Text = "&Save";
			_MI_File_SaveAs.Text = "Save &as...";
			_MI_File_Close.Text = "&Close";
			_MI_File_Sep1.Text = "-";
			_MI_File_ReadOnly.Text = "&Read only";
			_MI_File_Sep2.Text = "-";
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
			_MI_Edit_SelectAll.Text = "Select A&ll";
			_MI_Edit_Sep3.Text = "-";
			_MI_Edit_EolCode.Text = "Set &line end code";
			_MI_Edit_EolCode_CRLF.Text = "&CR+LF";
			_MI_Edit_EolCode_LF.Text = "&LF";
			_MI_Edit_EolCode_CR.Text = "C&R";
			_MI_View.Text = "&View";
			_MI_View_ShowSpecialChar.Text = "Show &Special Chars...";
			_MI_View_WrapLines.Text = "&Wrap lines";
			_MI_View_ShowTabPanel.Text = "Show &tab panel";
			_MI_Mode.Text = "&Mode";
			_MI_Mode_Text.Text = "&Text";
			_MI_Mode_Latex.Text = "&LaTeX";
			_MI_Mode_Cpp.Text = "&C/C++";
			_MI_Mode_CSharp.Text = "C&#";
			_MI_Mode_Java.Text = "&Java";
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
#			if !PocketPC
			_MI_Edit_EolCode_CRLF.RadioCheck = true;
			_MI_Edit_EolCode_LF.RadioCheck = true;
			_MI_Edit_EolCode_CR.RadioCheck = true;
#			endif

			// bind menu actions
			EventHandler menuActionHandler = this.HandleMenuAction;
			foreach( MenuItem mi in _MainMenu.MenuItems )
			{
				foreach( MenuItem mi2 in mi.MenuItems )
				{
					foreach( MenuItem mi3 in mi2.MenuItems )
					{
						foreach( MenuItem mi4 in mi3.MenuItems )
						{
							mi4.Click += menuActionHandler;
						}
						mi3.Click += menuActionHandler;
					}
					mi2.Click += menuActionHandler;
				}
			}

			// set menu
			Menu = _MainMenu;
		}

		void ResetShortcutInMenu()
		{
			string newText;
			MenuItem mi;

			// find matched pair that both pair has same action from two dictionary
			foreach( KeyValuePair<Keys, AnnAction> keyEntry in _KeyMap )
			{
				foreach( KeyValuePair<MenuItem, AnnAction> menuEntry in _MenuMap )
				{
					// has same action?
					if( keyEntry.Value != menuEntry.Value )
					{
						continue;
					}

					// ok, a pair was found.
					// reset menu text with found shortcut-key.
					mi = menuEntry.Key;
					int tabPos = mi.Text.IndexOf( '\t' );
					if( tabPos == -1 )
					{
						newText = mi.Text + "\t" + Utl.ToString( keyEntry.Key );
					}
					else
					{
						newText = mi.Text.Substring(0, tabPos) + "\t" + Utl.ToString( keyEntry.Key );
					}
					mi.Text = newText;

					break;
				}
			}
		}
		#endregion

		#region UI Components
		MainMenu _MainMenu			= new MainMenu();
#		if PocketPC
		MenuItem _MI_Menu = new MenuItem();
		MenuItem _MI_Save = new MenuItem();
#		endif
		MenuItem _MI_File			= new MenuItem();
		MenuItem _MI_File_New		= new MenuItem();
		MenuItem _MI_File_Open		= new MenuItem();
		MenuItem _MI_File_Save		= new MenuItem();
		MenuItem _MI_File_SaveAs	= new MenuItem();
		MenuItem _MI_File_Close		= new MenuItem();
		MenuItem _MI_File_Sep1		= new MenuItem();
		MenuItem _MI_File_ReadOnly	= new MenuItem();
		MenuItem _MI_File_Sep2		= new MenuItem();
		MenuItem _MI_File_Exit		= new MenuItem();
		MenuItem _MI_Edit			= new MenuItem();
		MenuItem _MI_Edit_Undo		= new MenuItem();
		MenuItem _MI_Edit_Redo		= new MenuItem();
		MenuItem _MI_Edit_Sep0		= new MenuItem();
		MenuItem _MI_Edit_Cut		= new MenuItem();
		MenuItem _MI_Edit_Copy		= new MenuItem();
		MenuItem _MI_Edit_Paste		= new MenuItem();
		MenuItem _MI_Edit_Sep1		= new MenuItem();
		MenuItem _MI_Edit_Find		= new MenuItem();
		MenuItem _MI_Edit_FindNext	= new MenuItem();
		MenuItem _MI_Edit_FindPrev	= new MenuItem();
		MenuItem _MI_Edit_Sep2		= new MenuItem();
		MenuItem _MI_Edit_SelectAll	= new MenuItem();
		MenuItem _MI_Edit_Sep3		= new MenuItem();
		MenuItem _MI_Edit_EolCode		= new MenuItem();
		MenuItem _MI_Edit_EolCode_CRLF	= new MenuItem();
		MenuItem _MI_Edit_EolCode_LF	= new MenuItem();
		MenuItem _MI_Edit_EolCode_CR	= new MenuItem();
		MenuItem _MI_View					= new MenuItem();
		MenuItem _MI_View_ShowSpecialChar	= new MenuItem();
		MenuItem _MI_View_WrapLines			= new MenuItem();
		MenuItem _MI_View_ShowTabPanel		= new MenuItem();
		MenuItem _MI_Mode			= new MenuItem();
		MenuItem _MI_Mode_Text		= new MenuItem();
		MenuItem _MI_Mode_Latex		= new MenuItem();
		MenuItem _MI_Mode_Cpp		= new MenuItem();
		MenuItem _MI_Mode_CSharp	= new MenuItem();
		MenuItem _MI_Mode_Java		= new MenuItem();
		MenuItem _MI_Mode_Ruby		= new MenuItem();
		MenuItem _MI_Mode_XML		= new MenuItem();
		MenuItem _MI_Window			= new MenuItem();
		MenuItem _MI_Window_Next	= new MenuItem();
		MenuItem _MI_Window_Prev	= new MenuItem();
		MenuItem _MI_Window_List	= new MenuItem();
		MenuItem _MI_Help				= new MenuItem();
		MenuItem _MI_Help_MemoryUsage	= new MenuItem();
		MenuItem _MI_Help_About			= new MenuItem();
		AzukiControl _Azuki;
		TabPanel<Document> _TabPanel = new TabPanel<Document>();
		SearchPanel _SearchPanel = new SearchPanel();
		#endregion

		#region Utilities
		static class Utl
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
