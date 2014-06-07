using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace Sgry.Ann
{
	class DocumentListForm : Form
	{
		#region Fields
		List<Document> _Documents = null;
		int _SelectedIndex = 0;
		#endregion

		#region Init / Dispose
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public DocumentListForm()
		{
			InitializeComponent();
			_ListView.ListViewItemSorter = new SimplestListViewItemComparer();
			StartPosition = FormStartPosition.CenterParent;
			AutoScaleMode = AutoScaleMode.Font;
			Font = SystemInformation.MenuFont;
			_ListView.KeyDown += CloseFormOnEscape;
			Load += Form_Load;
		}

		void Form_Load( object sender, EventArgs e )
		{
			Document activeDoc = _Documents[ _SelectedIndex ];
			foreach( ListViewItem lvItem in _ListView.Items )
			{
				if( lvItem.Tag == activeDoc )
				{
					lvItem.Focused = true;
					lvItem.Selected = true;
					break;
				}
			}
		}
		#endregion

		#region Properties
		/// <summary>
		/// Sets documents to be shown in this dialog.
		/// </summary>
		public List<Document> Documents
		{
			set
			{
				// keep reference
				_Documents = value;

				// reset list view
				_ListView.Items.Clear();
				foreach( Document doc in _Documents )
				{
					// add item for ths document
					ListViewItem lvItem = new ListViewItem();
					lvItem.Text = Path.GetFileName( doc.DisplayNameWithFlags );
					lvItem.Tag = doc;
					_ListView.Items.Add( lvItem );

					// set directory column value
					if( doc.FilePath != null )
					{
						lvItem.SubItems.Add( Path.GetDirectoryName(doc.FilePath) );
					}
				}

				// reset column width
				_CH_FileName.Width = -1; // expand minimum width to show all lines
				_CH_Directory.Width = -2; // expand for maximum width available

				// sort items
				_ListView.Sort();
			}
		}

		/// <summary>
		/// Gets the document selected by user;
		/// </summary>
		public Document SelectedDocument
		{
			get
			{
				if( _SelectedIndex < _Documents.Count )
					return _Documents[ _SelectedIndex ];
				else
					return null;
			}
			set
			{
				_SelectedIndex = _Documents.IndexOf( value );
				Debug.Assert( 0 <= _SelectedIndex, "Specified document was not found in list view" );
			}
		}
		#endregion

		#region UI Event Handlers
		void CloseFormOnEscape( object sender, KeyEventArgs e )
		{
			if( e.KeyData == (Keys.W | Keys.Control)
				|| e.KeyData == Keys.Escape )
			{
				this.Close();
			}
		}

		void _ListView_ItemActivate( object sender, EventArgs e )
		{
			DialogResult = DialogResult.OK;
			this.Close();
		}

		protected override void OnClosed( EventArgs e )
		{
			if( DialogResult == DialogResult.OK )
			{
				_SelectedIndex = _Documents.IndexOf( (Document)_ListView.FocusedItem.Tag );
			}

			base.OnClosed( e );
		}
		#endregion

		#region UI Component Initialization
		void InitializeComponent()
		{
			_ListView = new ListView();
			_CH_FileName = new ColumnHeader();
			_CH_Directory = new ColumnHeader();
			SuspendLayout();
			// 
			// _ListView
			// 
			_ListView.Columns.Add( _CH_FileName );
			_ListView.Columns.Add( _CH_Directory );
			_ListView.Dock = DockStyle.Fill;
			_ListView.FullRowSelect = true;
			_ListView.Name = "_ListView";
			_ListView.TabIndex = 0;
			_ListView.View = View.Details;
			_ListView.ItemActivate += _ListView_ItemActivate;
			// 
			// _CH_FileName
			// 
			_CH_FileName.Text = "File name";
			_CH_FileName.Width = 160;
			// 
			// _CH_Directory
			// 
			_CH_Directory.Text = "Directory";
			_CH_Directory.Width = 230;
			// 
			// DocumentListForm
			// 
			MinimizeBox = false;
			Controls.Add( _ListView );
			Name = "DocumentListForm";
			Text = "Ann - Document List";
			ResumeLayout( false );
		}
		#endregion

		#region UI Components
		ListView _ListView;
		ColumnHeader _CH_FileName;
		ColumnHeader _CH_Directory;
		#endregion

		#region Utilities
		class SimplestListViewItemComparer : System.Collections.IComparer
		{
			public int Compare( object x, object y )
			{
				return String.Compare( ((ListViewItem)x).SubItems[0].Text, ((ListViewItem)y).SubItems[0].Text );
			}
		}
		#endregion
	}
}