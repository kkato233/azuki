// 2009-04-20
using System;
using System.Text;
using Sgry.Azuki;
using Debug = System.Diagnostics.Debug;
using Path = System.IO.Path;

namespace Sgry.Ann
{
	class Document
	{
		#region Fields
		Azuki.Document _AzukiDoc;
		string _FilePath = null;
		Encoding _Encoding = Encoding.Default;
		FileType _FileType;
		string _DisplayName = null;
		int _FirstVisibleLineIndex;
		#endregion

		#region Init / Dispose
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public Document( Azuki.Document azukiDoc )
		{
			_AzukiDoc = azukiDoc;
			_FileType = FileType.TextFileType;
		}
		#endregion

		#region Properties
		/// <summary>
		/// Gets Azuki's document object.
		/// </summary>
		public Azuki.Document AzukiDoc
		{
			get{ return _AzukiDoc; }
		}

		/// <summary>
		/// Gets or sets name for display.
		/// </summary>
		public string DisplayName
		{
			get
			{
				if( _FilePath != null )
				{
					return Path.GetFileName( _FilePath );
				}
				else
				{
					return _DisplayName;
				}
			}
			set
			{
				_DisplayName = value;
			}
		}

		/// <summary>
		/// Gets name for display with flags.
		/// </summary>
		public string DisplayNameWithFlags
		{
			get
			{
				if( AzukiDoc.IsDirty )
					return DisplayName + '*';
				else
					return DisplayName;
			}
		}

		/// <summary>
		/// Gets or sets index of the first visible line.
		/// </summary>
		public int FirstVisibleLineIndex
		{
			get{ return _FirstVisibleLineIndex; }
			set{ _FirstVisibleLineIndex = value; }
		}

		/// <summary>
		/// Gets associated file type object.
		/// </summary>
		public FileType FileType
		{
			get{ return _FileType; }
			set{ _FileType = value; }
		}

		/// <summary>
		/// Gets or sets the file path associated with this document.
		/// </summary>
		public string FilePath
		{
			get{ return _FilePath; }
			set
			{
				// once file path was associated with a document, set display name to the file name and lock it
				_DisplayName = Path.GetFileName( value );
				_FilePath = value;
			}
		}

		/// <summary>
		/// Gets or sets encoding of the document file.
		/// </summary>
		public Encoding Encoding
		{
			get{ return _Encoding; }
			set{ _Encoding = value; }
		}

		/// <summary>
		/// Gets or sets text content.
		/// </summary>
		public string Text
		{
			get{ return _AzukiDoc.Text; }
			set{ _AzukiDoc.Text = value; }
		}
		#endregion

		#region Utilities
#		if DEBUG
		/// <summary>
		/// ToString for debug.
		/// </summary>
		public override string ToString()
		{
			return DisplayNameWithFlags;
		}
#		endif
		#endregion
	}
}
