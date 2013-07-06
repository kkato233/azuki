using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace Sgry.Ann
{
	class MruFileList : List<MruFile>
	{
		public bool TryGet( string path, out MruFile mru )
		{
			foreach( MruFile item in this )
			{
				if( item.Path == path )
				{
					mru = item;
					return true;
				}
			}
			mru = null;
			return false;
		}

		public void Put( string path )
		{
			Put( path, 0, 0 );
		}

		public void Put( string path, int line, int column )
		{
			// Remove existing its record if exists
			for( int i=0; i<Count; i++ )
			{
				if( this[i].Path == path )
				{
					RemoveAt( i );
				}
			}

			// Create new one
			Insert( 0, new MruFile(path, line, column) );
			for( int i=10; i<Count; i++ )
			{
				RemoveAt( 10 );
			}
		}

		public void Load( string text )
		{
			try
			{
				Clear();
				string[] tokens = text.Split( '|' );
				for( int i=0; i+2<tokens.Length; i+=3 )
				{
					Add( new MruFile(tokens[i], Int32.Parse(tokens[i+1]), Int32.Parse(tokens[i+2])) );
				}
			}
			catch( IndexOutOfRangeException )
			{
				Clear();
			}
			catch( FormatException )
			{
				Clear();
			}
		}

		public override string ToString()
		{
			StringBuilder buf = new StringBuilder();
			foreach( MruFile item in this )
				buf.Append( item.ToString() );
			return buf.ToString();
		}
	}

	class MruFile
	{
		public string Path;
		public int LineIndex;
		public int ColumnIndex;

		public MruFile()
			: this(null, 0, 0)
		{}

		public MruFile( string path, int line, int column )
		{
			Path = path;
			LineIndex = line;
			ColumnIndex = column;
		}

		public override string ToString()
		{
			if( String.IsNullOrEmpty(Path) )
				return "";

			return String.Join( "|",
								new string[]{Path,
											 LineIndex.ToString(),
											 ColumnIndex.ToString()} ) + "|";
		}
	}
}
