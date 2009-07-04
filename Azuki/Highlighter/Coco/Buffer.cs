using System;
using System.Collections.Generic;
using System.Text;

namespace Sgry.Azuki.Highlighter.Coco
{
	class Buffer
	{
		public const int EOF = Char.MaxValue + 1;
		Document	_Document;
		int			_Position = 0;

		public Buffer( Document doc, int startIndex )
		{
			_Document = doc;
			_Position = startIndex;
		}

		public int Read()
		{
			if( _Position < _Document.Length )
				return _Document[ _Position++ ];
			else
				return EOF;
		}

		public int Peek()
		{
			if( _Position < _Document.Length )
				return _Document[ _Position ];
			else
				return EOF;
		}

		public int Pos
		{
			get{ return _Position; }
			set
			{
				if( _Position < 0 || _Document.Length < _Position )
					throw new ArgumentOutOfRangeException();

				_Position = value;
			}
		}

		public string GetString( int begin, int end )
		{
			return _Document.GetTextInRange( begin, end );
		}
	}
}
