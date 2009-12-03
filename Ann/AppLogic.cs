// 2009-12-03
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Sgry.Azuki;
using Sgry.Azuki.Windows;
using Debug = System.Diagnostics.Debug;
using AzukiDocument = Sgry.Azuki.Document;
using CancelEventArgs = System.ComponentModel.CancelEventArgs;

namespace Sgry.Ann
{
	class AppLogic
	{
		#region Fields
		const string OpenFileFilter =
			"All files(*.*)|*.*|"
			+ "Supported files|*.txt;*.log;*.ini;*.inf;*.tex;*.htm;*.html;*.css;*.js;*.xml;*.c;*.cpp;*.cxx;*.h;*.hpp;*.hxx;*.cs;*.java;*.py;*.rb;*.pl;*.vbs;*.bat|"
			+ SaveFileFilter;
		const string SaveFileFilter =
			"Text file(*.txt, *.log, *.tex, ...)|*.txt;*.log;*.ini;*.inf;*.tex"
			+ "|HTML file(*.htm, *.html)|*.htm;*.html"
			+ "|CSS file(*.css)|*.css"
			+ "|Javascript file(*.js)|*.js"
			+ "|XML file(*.xml)|*.xml"
			+ "|C/C++ source(*.c, *.h, ...)|*.c;*.cpp;*.cxx;*.h;*.hpp;*.hxx"
			+ "|C# source(*.cs)|*.cs"
			+ "|Java source(*.java)|*.java"
			+ "|Python script(*.py)|*.py"
			+ "|Ruby script(*.rb)|*.rb"
			+ "|Perl script(*.pl)|*.pl"
			+ "|VB script(*.vbs)|*.vbs"
			+ "|Batch file(*.bat)|*.bat";

		AnnForm _MainForm = null;
		List<Document> _DAD_Documents = new List<Document>(); // Dont Access Directly
		Document _DAD_ActiveDocument = null; // Dont Access Directly
		int _UntitledFileCount = 1;
		string _InitOpenFilePath = null;
		SearchContext _SearchContext = new SearchContext();
		#endregion

		#region Init / Dispose
		public AppLogic( string initOpenFilePath )
		{
			_InitOpenFilePath = initOpenFilePath;
		}
		#endregion

		#region Properties
		/// <summary>
		/// Gets or sets application's main form.
		/// </summary>
		public AnnForm MainForm
		{
			get{ return _MainForm; }
			set
			{
				_MainForm = value;
				_MainForm.Load += MainForm_Load;
				_MainForm.Closing += MainForm_Closing;
				_MainForm.Closed += MainForm_Closed;
				_MainForm.Azuki.Resize += Azuki_Resize;
				_MainForm.SearchPanel.PatternUpdated += SearchPanel_PatternUpdated;
				_MainForm.TabPanel.Items = Documents;
				_MainForm.TabPanel.TabSelected += TabPanel_TabSelected;

				// handle initially set document
				Document doc = new Document();
				AddDocument( doc );
				ActiveDocument = doc;

				// give find panel reference to find context object 
				_MainForm.SearchPanel.SetContextRef( _SearchContext );
			}
		}

		/// <summary>
		/// Gets list of documents currently loaded.
		/// </summary>
		public List<Document> Documents
		{
			get{ return _DAD_Documents; }
		}

		/// <summary>
		/// Gets currently active document.
		/// </summary>
		public Document ActiveDocument
		{
			get
			{
				Debug.Assert( _DAD_ActiveDocument == null || _DAD_Documents.Contains(_DAD_ActiveDocument) );
				return _DAD_ActiveDocument;
			}
			set
			{
				Debug.Assert( _DAD_Documents.Contains(value) );
				if( _DAD_ActiveDocument == value )
					return;

				// activate document
				_DAD_ActiveDocument = value;
				MainForm.Azuki.Document = value;
				MainForm.Azuki.ScrollToCaret();
				MainForm.Azuki.UpdateCaretGraphic();
				MainForm.TabPanel.SelectedItem = value;

				// update UI
				MainForm.UpdateUI();
				MainForm.TabPanel.Invalidate();
			}
		}
		#endregion

		#region Document Management
		/// <summary>
		/// Add a document to document list.
		/// </summary>
		public void AddDocument( Document doc )
		{
			Debug.Assert( _DAD_Documents.Contains(doc) == false );
			if( doc.FilePath == null )
			{
				doc.DisplayName = "Untitled" + _UntitledFileCount;
				_UntitledFileCount++;
			}
			doc.DirtyStateChanged += Doc_DirtyStateChanged;
			_DAD_Documents.Add( doc );
		}

		void Doc_DirtyStateChanged( object sender, EventArgs e )
		{
			MainForm.UpdateUI();
		}

		/// <summary>
		/// Removes a document from document list.
		/// </summary>
		public void RemoveDocument( Document doc )
		{
			Debug.Assert( _DAD_Documents.Contains(doc) );

			int index = _DAD_Documents.IndexOf( doc );
			_DAD_Documents.RemoveAt( index );
			if( _DAD_ActiveDocument == doc )
			{
				if( index < _DAD_Documents.Count )
					ActiveDocument = _DAD_Documents[ index ];
				else if( 0 < _DAD_Documents.Count )
					ActiveDocument = _DAD_Documents[ 0 ];
				else
					_DAD_ActiveDocument = null;
			}
			doc.DirtyStateChanged -= Doc_DirtyStateChanged;
		}

		/// <summary>
		/// Switch to next document.
		/// </summary>
		public void ActivateNextDocument()
		{
			if( ActiveDocument == null )
				return;

			int index = _DAD_Documents.IndexOf( _DAD_ActiveDocument );
			if( index+1 < _DAD_Documents.Count )
			{
				ActiveDocument = _DAD_Documents[ index+1 ];
			}
			else
			{
				ActiveDocument = _DAD_Documents[ 0 ];
			}
		}

		/// <summary>
		/// Switch to previous document.
		/// </summary>
		public void ActivatePrevDocument()
		{
			if( ActiveDocument == null )
				return;

			int index = _DAD_Documents.IndexOf( _DAD_ActiveDocument );
			if( 0 <= index-1 )
			{
				ActiveDocument = _DAD_Documents[ index-1 ];
			}
			else
			{
				ActiveDocument = _DAD_Documents[ _DAD_Documents.Count-1 ];
			}
		}

		/// <summary>
		/// Shows document list in a dialog.
		/// </summary>
		public void ShowDocumentList()
		{
			DocumentListForm dialog;
			DialogResult result;
			Document selectedDoc;

			using( dialog = new DocumentListForm() )
			{
				// prepare to show dialog
				dialog.Size = MainForm.Size;
				dialog.Documents = Documents;
				dialog.SelectedDocument = ActiveDocument;

				// show document list dialog
				result = dialog.ShowDialog();
				if( result != DialogResult.OK )
				{
					return;
				}

				// get user's selection
				selectedDoc = dialog.SelectedDocument;
				if( selectedDoc != null )
				{
					ActiveDocument = selectedDoc;
				}
			}
		}

		/// <summary>
		/// Sets file type to the document.
		/// </summary>
		public void SetFileType( Document doc, FileType fileType )
		{
			doc.FileType = fileType;
			doc.Highlighter = fileType.Highlighter;

			if( doc == ActiveDocument )
			{
				_MainForm.Azuki.AutoIndentHook = fileType.AutoIndentHook;
				_MainForm.UpdateUI();
				_MainForm.Azuki.Invalidate();
			}
		}

		/// <summary>
		/// Sets EOL code for input and unify existing EOL code to the one if user choses so.
		/// </summary>
		public void SetEolCode( string eolCode )
		{
			DialogResult reply;

			if( eolCode != "\r\n" && eolCode != "\r" && eolCode != "\n" )
				throw new ArgumentException( "EOL code must be one of the CR+LF, CR, LF.", "eolCode" );

			// if newly specified EOL code is same as currently set one, do nothing
			if( MainForm.Azuki.Document.EolCode == eolCode )
			{
				return;
			}

			// set input EOL code
			MainForm.Azuki.Document.EolCode = eolCode;

			// ask user whether to unify currently existing all EOL codes to the new one
			reply = AskUserToUnifyExistingEolOrNot( eolCode );
			if( reply == DialogResult.Yes )
			{
				// unify EOL code
				int lineHead, lineEnd;
				Document doc = ActiveDocument;

				doc.BeginUndo();
				for( int lineIndex=0; lineIndex+1<doc.LineCount; lineIndex++ )
				{
					int eolBegin;

					lineHead = doc.GetLineHeadIndex( lineIndex );
					lineEnd = doc.GetLineHeadIndex( lineIndex+1 );

					// set eol beginning index as one character before the line end
					eolBegin = lineEnd - 1;
					
					// if the line is terminated with a CR+LF, set eol beginning index back one more
					if( lineHead < lineEnd-2 )
					{
						string ch = doc.GetTextInRange( lineEnd-2, lineEnd );
						if( ch == "\r\n" )
						{
							eolBegin--;
						}
					}
					
					// replace the line terminator
					doc.Replace( eolCode, eolBegin, lineEnd );
				}
				doc.EndUndo();
			}

			MainForm.UpdateUI();
		}
		#endregion

		#region I/O
		/// <summary>
		/// Creates a new document.
		/// </summary>
		public void CreateNewDocument()
		{
			// create a document
			Document doc = new Document();
			AddDocument( doc );

			// activate it
			ActiveDocument = doc;
		}

		/// <summary>
		/// Opens a file with specified encoding and create a Document object.
		/// Give null as 'encoding' parameter to detect encoding automatically.
		/// </summary>
		/// <exception cref="DirectoryNotFoundException">The specified path is invalid, such as being on an unmapped drive.</exception>
		/// <exception cref="FileNotFoundException">The file cannot be found.</exception>
		/// <exception cref="IOException">Other I/O error has occured.</exception>
		Document CreateDocumentFromFile( string filePath, Encoding encoding, bool withBom )
		{
			Document doc;
			StreamReader file = null;
			char[] buf = null;
			int readCount = 0;

			// if specified file was already opened, just return the document
			foreach( Document d in Documents )
			{
				if( String.Compare(d.FilePath, filePath, true) == 0 )
				{
					return d;
				}
			}

			// create new document
			doc = new Document();
			
			// analyze encoding
			if( encoding == null )
			{
				Utl.AnalyzeEncoding( filePath, out encoding, out withBom );
			}
			doc.Encoding = encoding;
			doc.WithBom = withBom;

			// load file content
			using( file = new StreamReader(filePath, encoding) )
			{
				// expand internal buffer size before loading file
				// (estimating needed buffer size by a half of byte-count of file)
				doc.Capacity = (int)( file.BaseStream.Length / 2 );

				// prepare load buffer
				// (if the file is larger than 1MB, separate by 10 and load for each)
				if( file.BaseStream.Length < 1024*1024 )
				{
					buf = new char[ file.BaseStream.Length ];
				}
				else
				{
					buf = new char[ (file.BaseStream.Length+10) / 10 ];
				}

				// read
				while( !file.EndOfStream )
				{
					readCount = file.Read( buf, 0, buf.Length );
					doc.Replace( new String(buf, 0, readCount), doc.Length, doc.Length );
				}
			}

			// set document properties
			doc.ClearHistory();
			doc.IsDirty = false;
			doc.FilePath = filePath;
			if( (new FileInfo(filePath).Attributes & FileAttributes.ReadOnly) != 0 )
			{
				doc.IsReadOnly = true;
			}

			return doc;
		}

		/// <summary>
		/// Open existing file.
		/// </summary>
		public void OpenDocument()
		{
			OpenFileDialog dialog = null;
			DialogResult result;
			
			using( dialog = new OpenFileDialog() )
			{
				// setup dialog
				if( ActiveDocument.FilePath != null )
				{
					// set initial directory to directory containing currently active file if exists
					string dirPath = Path.GetDirectoryName( ActiveDocument.FilePath );
					if( Directory.Exists(dirPath) )
					{
						dialog.InitialDirectory = dirPath;
					}
				}
				dialog.Filter = OpenFileFilter;
				dialog.FilterIndex = 2;

				// show dialog
				result = dialog.ShowDialog();
				if( result != DialogResult.OK )
				{
					return;
				}

				// open the file
				OpenDocument( dialog.FileName, null, false );
			}
		}

		/// <summary>
		/// Open existing file.
		/// </summary>
		public void OpenDocument( string filePath, Encoding encoding, bool withBom )
		{
			Document doc;

			try
			{
				// load the file
				doc = CreateDocumentFromFile( filePath, null, false );
				if( Documents.Contains(doc) == false )
				{
					AddDocument( doc );
				}

				// activate it
				ActiveDocument = doc;
				SetFileType( doc, FileType.GetFileTypeByFileName(filePath) );
				MainForm.Azuki.SetSelection( 0, 0 );
				MainForm.Azuki.ScrollToCaret();
			}
			catch( IOException ex )
			{
				AlertException( ex );
			}
		}

		/// <summary>
		/// Save document.
		/// </summary>
		public void SaveDocument( Document doc )
		{
			FileStream file = null;
			string dirPath;

			// if the document is read-only, do nothing
			if( doc.IsReadOnly )
			{
				return;
			}

			// if no file path was associated, switch to SaveAs action
			if( doc.FilePath == null )
			{
				SaveDocumentAs( doc );
				return;
			}

			// ensure that destination directory exists
			dirPath = Path.GetDirectoryName( doc.FilePath );
			if( Directory.Exists(dirPath) == false )
			{
				try
				{
					Directory.CreateDirectory( dirPath );
				}
				catch( IOException ex )
				{
					// case ex: opened file has been on a removable drive
					// and the drive was ejected now
					AlertException( ex );
					return;
				}
				catch( UnauthorizedAccessException ex )
				{
					// case example: permission of parent directory was changed
					// and current user lost right to create directory
					AlertException( ex );
					return;
				}
			}

			// overwrite
			try
			{
				byte[] bomBytes = new byte[]{};
				byte[] contentBytes = null;

				// decode content to native encoding
				contentBytes = doc.Encoding.GetBytes( doc.Text );
				if( doc.WithBom )
				{
					if( doc.Encoding == Encoding.BigEndianUnicode
						|| doc.Encoding == Encoding.Unicode
						|| doc.Encoding == Encoding.UTF8 )
					{
						bomBytes = doc.Encoding.GetBytes( "\xFEFF" );
					}
				}

				// write file bytes
				using( file = File.OpenWrite(doc.FilePath) )
				{
					file.SetLength( 0 );
					file.Write( bomBytes, 0, bomBytes.Length );
					file.Write( contentBytes, 0, contentBytes.Length );
				}
				doc.IsDirty = false;
			}
			catch( UnauthorizedAccessException ex )
			{
				// case example: target file is readonly.
				// case example: target file was deleted and now there is a directory having same name
				AlertException( ex );
			}
			catch( IOException ex )
			{
				// case example: another process is opening the file and does not allow to write
				AlertException( ex );
			}
		}

		/// <summary>
		/// Save document content as another file.
		/// </summary>
		public void SaveDocumentAs( Document doc )
		{
			Debug.Assert( doc != null );
			SaveFileDialog dialog = null;
			DialogResult result;
			string fileName;
			
			using( dialog = new SaveFileDialog() )
			{
				// setup dialog
				if( doc.FilePath != null )
				{
					// set initial directory to that containing currently active file if exists
#					if !PocketPC
					string dirPath = Path.GetDirectoryName( doc.FilePath );
					if( Directory.Exists(dirPath) )
					{
						dialog.InitialDirectory = dirPath;
					}
#					endif

					// set initial file name
					dialog.FileName = Path.GetFileName( doc.FilePath );
				}
				dialog.Filter = SaveFileFilter;

				// show dialog
				result = dialog.ShowDialog();
				if( result != DialogResult.OK )
				{
					return;
				}

				fileName = dialog.FileName;
			}

			// associate the file path and reset attributes
#			if PocketPC
			// In Windows Mobile's SaveFileDialog,
			// if we select filter item "Text File|*.txt;*.log"
			// and enter file name "foo" and tap OK button, then,
			// FileName property value will be "foo.txt;*.log".
			// Of cource this is not expected so we cut off trailing garbages here.
			Match match = Regex.Match( fileName, @"(;\*\.[a-zA-Z0-9_#!$~]+)+" );
			if( match.Success )
			{
				fileName = fileName.Substring( 0, fileName.Length - match.Length );
			}
#			endif
			doc.FilePath = fileName;
			doc.IsReadOnly = false;

			// delegate to overwrite logic
			SaveDocument( doc );

			// finally, update UI because the name of the document was changed
			MainForm.UpdateUI();
		}

		/// <summary>
		/// Closes a document.
		/// </summary>
		public void CloseDocument( Document doc )
		{
			DialogResult result;

			// confirm to discard modification
			if( doc.IsDirty )
			{
				result = AlertDiscardModification( doc );
				if( result == DialogResult.Yes )
				{
					SaveDocument( doc );
				}
				else if( result == DialogResult.Cancel )
				{
					return;
				}
			}

			// close
			RemoveDocument( doc );
			if( Documents.Count == 0 )
			{
				MainForm.Close();
			}
		}

		void AlertException( Exception ex )
		{
			MessageBox.Show(
					ex.Message,
					"Ann",
					MessageBoxButtons.OK,
					MessageBoxIcon.Exclamation,
					MessageBoxDefaultButton.Button1
				);
		}

		public DialogResult AlertDiscardModification( Document doc )
		{
			return MessageBox.Show(
					doc.DisplayName + " is modified but not saved. Save changes?",
					"Ann",
					MessageBoxButtons.YesNoCancel,
					MessageBoxIcon.Exclamation,
					MessageBoxDefaultButton.Button2
				);
		}

		public DialogResult AskUserToUnifyExistingEolOrNot( string newEolCode )
		{
			string eolCodeName;

			switch( newEolCode )
			{
				case "\r\n":	eolCodeName = "CR+LF";	break;
				case "\n":		eolCodeName = "LF";		break;
				case "\r":		eolCodeName = "CR";		break;
				default:		throw new ArgumentException("EOL code must be one of CR+LF, LF, CR.", "newEolCode");
			}

			return MessageBox.Show(
					"Do you also want to change all existing line end code to "+eolCodeName+"?",
					"Ann",
					MessageBoxButtons.YesNo,
					MessageBoxIcon.Question,
					MessageBoxDefaultButton.Button2
				);
		}
		#endregion

		#region Text Search
		void SearchPanel_PatternUpdated( bool forward )
		{
			if( forward )
				FindNext();
			else
				FindPrev();
		}

		public void FindNext()
		{
			AzukiDocument doc = ActiveDocument;
			int startIndex;
			SearchResult result;
			Regex regex;

			// determine where to start text search
			if( 0 <= _SearchContext.AnchorIndex )
				startIndex = _SearchContext.AnchorIndex;
			else
				startIndex = Math.Max( doc.CaretIndex, doc.AnchorIndex );

			// find
			if( _SearchContext.UseRegex )
			{
				// Regular expression search.
				// get regex object from context
				regex = _SearchContext.Regex;
				if( regex == null )
				{
					// current text pattern was invalid as a regular expression.
					return;
				}

				// ensure that "RightToLeft" option of the regex object is NOT set
				RegexOptions opt = regex.Options;
				if( (opt & RegexOptions.RightToLeft) != 0 )
				{
					opt &= ~( RegexOptions.RightToLeft );
					regex = new Regex( regex.ToString(), opt );
					_SearchContext.Regex = regex;
				}
				result = doc.FindNext( regex, startIndex, doc.Length );
			}
			else
			{
				// normal text pattern matching.
				result = doc.FindNext( _SearchContext.TextPattern, startIndex, doc.Length, _SearchContext.MatchCase );
			}

			// select the result
			if( result != null )
			{
				MainForm.Azuki.Document.SetSelection( result.Begin, result.End );
				MainForm.Azuki.View.SetDesiredColumn();
				MainForm.Azuki.ScrollToCaret();
			}
		}

		public void FindPrev()
		{
			AzukiDocument doc = ActiveDocument;
			int startIndex;
			SearchResult result;
			Regex regex;

			// determine where to start text search
			if( 0 <= _SearchContext.AnchorIndex )
				startIndex = _SearchContext.AnchorIndex;
			else
				startIndex = Math.Min( doc.CaretIndex, doc.AnchorIndex );

			// find
			if( _SearchContext.UseRegex )
			{
				// Regular expression search.
				// get regex object from context
				regex = _SearchContext.Regex;
				if( regex == null )
				{
					// current text pattern was invalid as a regular expression.
					return;
				}

				// ensure that "RightToLeft" option of the regex object is set
				RegexOptions opt = _SearchContext.Regex.Options;
				if( (opt & RegexOptions.RightToLeft) == 0 )
				{
					opt |= RegexOptions.RightToLeft;
					_SearchContext.Regex = new Regex( _SearchContext.Regex.ToString(), opt );
				}
				result = doc.FindPrev( _SearchContext.Regex, 0, startIndex );
			}
			else
			{
				// normal text pattern matching.
				result = doc.FindPrev( _SearchContext.TextPattern, 0, startIndex, _SearchContext.MatchCase );
			}

			// select the result
			if( result != null )
			{
				MainForm.Azuki.Document.SetSelection( result.End, result.Begin );
				MainForm.Azuki.View.SetDesiredColumn();
				MainForm.Azuki.ScrollToCaret();
			}
		}
		#endregion

		#region Config
		public void LoadConfig()
		{
			// load config file
			AppConfig.Load();

			// apply config
			MainForm.Azuki.FontInfo				= AppConfig.FontInfo;
			MainForm.ClientSize					= AppConfig.WindowSize;
			if( AppConfig.WindowMaximized )
			{
				MainForm.WindowState = FormWindowState.Maximized;
			}
			MainForm.TabPanelEnabled			= AppConfig.TabPanelEnabled;

			MainForm.Azuki.DrawsEolCode			= AppConfig.DrawsEolCode;
			MainForm.Azuki.DrawsFullWidthSpace	= AppConfig.DrawsFullWidthSpace;
			MainForm.Azuki.DrawsSpace			= AppConfig.DrawsSpace;
			MainForm.Azuki.DrawsTab				= AppConfig.DrawsTab;
			MainForm.Azuki.DrawsEofMark			= AppConfig.DrawsEofMark;
			MainForm.Azuki.HighlightsCurrentLine= AppConfig.HighlightsCurrentLine;
			MainForm.Azuki.ShowsLineNumber		= AppConfig.ShowsLineNumber;
			MainForm.Azuki.ShowsHRuler			= AppConfig.ShowsHRuler;
			MainForm.Azuki.ShowsDirtBar			= AppConfig.ShowsDirtBar;
			MainForm.Azuki.TabWidth				= AppConfig.TabWidth;
			MainForm.Azuki.LinePadding			= AppConfig.LinePadding;
			MainForm.Azuki.LeftMargin			= AppConfig.LeftMargin;
			MainForm.Azuki.TopMargin			= AppConfig.TopMargin;
			MainForm.Azuki.ViewType				= AppConfig.ViewType;
			MainForm.Azuki.UsesTabForIndent		= AppConfig.UsesTabForIndent;
			MainForm.Azuki.ConvertsFullWidthSpaceToSpace = AppConfig.ConvertsFullWidthSpaceToSpace;
			MainForm.Azuki.HRulerIndicatorType	= AppConfig.HRulerIndicatorType;

			// update UI
			MainForm.UpdateUI();
		}

		public void SaveConfig()
		{
			// update config fields
			AppConfig.FontInfo				= new FontInfo( MainForm.Azuki.Font );
			AppConfig.WindowMaximized		= (MainForm.WindowState == FormWindowState.Maximized);
			if( MainForm.WindowState == FormWindowState.Normal )
			{
				AppConfig.WindowSize = MainForm.ClientSize;
			}
			AppConfig.TabPanelEnabled		= MainForm.TabPanelEnabled;

			AppConfig.DrawsEolCode			= MainForm.Azuki.DrawsEolCode;
			AppConfig.DrawsFullWidthSpace	= MainForm.Azuki.DrawsFullWidthSpace;
			AppConfig.DrawsSpace			= MainForm.Azuki.DrawsSpace;
			AppConfig.DrawsTab				= MainForm.Azuki.DrawsTab;
			AppConfig.DrawsEofMark			= MainForm.Azuki.DrawsEofMark;
			AppConfig.HighlightsCurrentLine	= MainForm.Azuki.HighlightsCurrentLine;
			AppConfig.ShowsLineNumber		= MainForm.Azuki.ShowsLineNumber;
			AppConfig.ShowsHRuler			= MainForm.Azuki.ShowsHRuler;
			AppConfig.ShowsDirtBar			= MainForm.Azuki.ShowsDirtBar;
			AppConfig.TabWidth				= MainForm.Azuki.TabWidth;
			AppConfig.LinePadding			= MainForm.Azuki.LinePadding;
			AppConfig.LeftMargin			= MainForm.Azuki.LeftMargin;
			AppConfig.TopMargin				= MainForm.Azuki.TopMargin;
			AppConfig.ViewType				= MainForm.Azuki.ViewType;
			AppConfig.UsesTabForIndent		= MainForm.Azuki.UsesTabForIndent;
			AppConfig.ConvertsFullWidthSpaceToSpace = MainForm.Azuki.ConvertsFullWidthSpaceToSpace;
			AppConfig.HRulerIndicatorType	= MainForm.Azuki.HRulerIndicatorType;

			// save to file
			AppConfig.Save();
		}
		#endregion

		#region UI Event Handlers
		void MainForm_Load( object sender, EventArgs e )
		{
			if( _InitOpenFilePath == null )
				return;

			Document prevActiveDoc;

			// try to open initial document
			prevActiveDoc = ActiveDocument;
			OpenDocument( _InitOpenFilePath, null, false );

			// close default empty document if successfully opened
			if( prevActiveDoc != ActiveDocument )
			{
				CloseDocument( prevActiveDoc );
			}
		}

		void MainForm_Closing( object sender, CancelEventArgs e )
		{
			DialogResult result;
			Document activeDoc = ActiveDocument;

			// confirm all document's discard
			foreach( Document doc in Documents )
			{
				// if it's modified, ask to save the document
				if( doc.IsDirty )
				{
					// before showing dialog, activate the document
					this.ActiveDocument = doc;

					// then, show dialog
					result = AlertDiscardModification( doc );
					if( result == DialogResult.Yes )
					{
						SaveDocument( doc );
						if( doc.IsDirty )
						{
							// canceled or failed. cancel closing
							e.Cancel = true;
							ActiveDocument = activeDoc;
							return;
						}
					}
					else if( result == DialogResult.Cancel )
					{
						e.Cancel = true;
						ActiveDocument = activeDoc;
						return;
					}
				}
			}
		}

		void MainForm_Closed( object sender, EventArgs e )
		{
			SaveConfig();
		}

		void TabPanel_TabSelected( MouseEventArgs e, Document item )
		{
			if( e.Button == MouseButtons.Left )
			{
				ActiveDocument = item;
			}
			else if( e.Button == MouseButtons.Middle )
			{
				MainForm.TabPanel.Invalidate();
				CloseDocument( item );
			}
		}

		void Azuki_Resize( object sender, EventArgs e )
		{
			if( MainForm.Azuki.ViewType == ViewType.WrappedProportional )
			{
				MainForm.Azuki.ViewWidth = MainForm.Azuki.ClientSize.Width;
			}
		}
		#endregion

		#region Utilities
		static class Utl
		{
			public static void AnalyzeEncoding( string filePath, out Encoding encoding, out bool withBom )
			{
				const int MaxSize = 50 * 1024;
				Stream file = null;
				byte[] data;
				int dataSize;

				try
				{
					using( file = File.OpenRead(filePath) )
					{
						// prepare buffer
						if( MaxSize < file.Length )
							dataSize = MaxSize;
						else
							dataSize = (int)file.Length;
						data = new byte[ dataSize ];

						// read data at maximum 50KB
						file.Read( data, 0, dataSize );
						encoding = EncodingAnalyzer.Analyze( data, out withBom );
						if( encoding == null )
						{
							encoding = Encoding.Default;
							withBom = false;
						}
					}
				}
				catch( IOException )
				{
					encoding = Encoding.Default;
					withBom = false;
				}
			}
		}
		#endregion
	}
}
