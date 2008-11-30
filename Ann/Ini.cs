// file: Ini.cs
// brief: an INI file parser
// author: SGRY (YAMAMOTO Suguru)
// update: 2008-09-14
// version: 1.2.0
// license: zlib license (see END of this file)
//=========================================================
using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Specialized;

using Debug = System.Diagnostics.Debug;

namespace Sgry
{
	/// <summary>
	/// Ini 形式のファイルを扱うクラス
	/// </summary>
	/// <example>
	/// Ini ini = new Ini();
	/// 
	/// ini.LoadFromFile( "test.ini" );
	/// 
	/// // print "bar" entry in section "foo"
	/// Console.WriteLine( ini["foo"]["bar"] );
	/// 
	///	// print all entries
	///	foreach( Ini.Section section in ini )
	///	{
	///		foreach( Ini.Entry entry in section )
	///		{
	///			Console.WriteLine( entry.ToFormattedString() );
	///		}
	///	}
	///	
	///	// add new entry
	///	ini["foo"]["1"] = "2"; // add to existed section
	///	ini["NewSection"]["Name"] = "Value"; // add to not existed section
	///	
	///	// save to new file
	///	ini.SaveToFile( "test_save.ini" );
	/// </example>
	public class Ini : IEnumerable
	{
		SortedList _Sections = new SortedList( new StringComparer() );

		#region Load / Save
		/// <summary>
		/// INI ファイルの内容を読み出して初期化します。
		/// ファイルはシステム標準の文字エンコーディングで開きます。
		/// </summary>
		/// <param name="path">解析する INI ファイルのパス</param>
		/// <exception cref="FileNotFoundException">
		/// 指定されたファイルが見つかりません。
		/// </exception>
		/// <exception cref="DirectoryNotFoundException">
		/// 指定されたパスが無効でアクセスできません。
		/// </exception>
		/// <exception cref="IOException">
		/// ファイルを開くときにエラーが発生しました。
		/// </exception>
		public void LoadFromFile( string path )
		{
			LoadFromFile( path, Encoding.Default );
		}

		/// <summary>
		/// INI ファイルの内容を読み出して初期化します。
		/// </summary>
		/// <param name="path">解析する INI ファイルのパス</param>
		/// <param name="encoding">解析するファイルのエンコーディング</param>
		/// <exception cref="FileNotFoundException">
		/// 指定されたファイルが見つかりません。
		/// </exception>
		/// <exception cref="DirectoryNotFoundException">
		/// 指定されたパスが無効でアクセスできません。
		/// </exception>
		/// <exception cref="IOException">
		/// ファイルを開くときにエラーが発生しました。
		/// </exception>
		public void LoadFromFile( string path, Encoding encoding )
		{
			Stream stream = File.Open( path, FileMode.Open, FileAccess.Read );
			LoadFromFile( stream , encoding);
			stream.Close();
		}

		/// <summary>
		/// ストリームから INI 形式の内容を読み出して初期化します。
		/// </summary>
		/// <param name="stream">解析する INI 形式のテキストを含んだストリーム</param>
		/// <param name="encoding">ファイルのエンコーディング</param>
		/// <exception cref="FileNotFoundException">
		/// 指定されたファイルが見つかりません。
		/// </exception>
		/// <exception cref="DirectoryNotFoundException">
		/// 指定されたパスが無効でアクセスできません。
		/// </exception>
		/// <exception cref="IOException">
		/// ファイルを開くときにエラーが発生しました。
		/// </exception>
		public void LoadFromFile( Stream stream, Encoding encoding )
		{
			Parse( stream, encoding );
		}

		/// <summary>
		/// オブジェクトが保持している内容を INI 形式でファイルに保存します。
		/// </summary>
		/// <param name="path">保存するファイルのパス</param>
		/// <param name="encoding">保存するファイルのエンコーディング</param>
		public void SaveToFile( string path, Encoding encoding )
		{
			FileStream file;
			byte[] encodedStr;
			
			// ファイルを開き、指定エンコーディングに変換して保存
			file = File.Open( path, FileMode.Create, FileAccess.Write );
			encodedStr = encoding.GetBytes( ToFormattedString() );
			file.Write( encodedStr, 0, encodedStr.Length );
			file.Close();
		}
		#endregion

		#region オブジェクトとしての属性
		/// <summary>
		/// 現在の内容を INI 形式の文字列に変換
		/// </summary>
		/// <returns>INI 形式の文字列に整形された現在の内容</returns>
		public string ToFormattedString()
		{
			StringBuilder str = new StringBuilder( 1024 );

			// 名前があるセクションを順に出力
			foreach( DictionaryEntry dicEntry in _Sections )
			{
				Section namedSection = (Section)dicEntry.Value;
				str.Append( namedSection.ToFormattedString() );
			}

			return str.ToString();
		}
		#endregion

/// <summary>
/// 
/// </summary>
		public string TryGetString( string section, string entry, string defaultValue )
		{
			if( this[section].Contains(entry) )
			{
				return this[section][entry];
			}
			else
			{
				return defaultValue;
			}
		}

/// <summary>
/// 
/// </summary>
		public Int32 TryGetInt( string section, string entry, Int32 defaultValue )
		{
			try
			{
				return Int32.Parse( this[section][entry] );
			}
			catch
			{
				return defaultValue;
			}
		}

/// <summary>
/// 
/// </summary>
		public Boolean TryGetBoolean( string section, string entry, Boolean defaultValue )
		{
			try
			{
				return Boolean.Parse( this[section][entry] );
			}
			catch
			{
				return defaultValue;
			}
		}

/// <summary>
/// 
/// </summary>
		public void Set<T>( string section, string entry, T value )
		{
			this[section][entry] = value.ToString();
		}

		#region コレクション インタフェース
		/// <summary>
		/// 新しいセクションを追加します。
		/// </summary>
		/// <param name="name">追加するセクションの名前</param>
		/// <returns>追加したセクション</returns>
		public void Add( string name )
		{
			_Sections.Add( name, new Section(this, name) );
		}

		/// <summary>
		/// 指定した名前を持つセクションを削除します。
		/// </summary>
		/// <param name="name">削除するセクションの名前</param>
		public void Remove( string name )
		{
			_Sections.Remove( name );
		}

		/// <summary>
		/// 指定したインデックスのセクションを削除します。
		/// </summary>
		/// <param name="index">削除するセクションのインデックス</param>
		public void RemoveAt( int index )
		{
			_Sections.RemoveAt( index );
		}

		/// <summary>
		/// すべてのセクションを削除します。
		/// </summary>
		public void Clear()
		{
			_Sections.Clear();
		}

		/// <summary>
		/// 指定した名前のセクションが存在するかを判定します。
		/// </summary>
		/// <param name="sectionName">存在を判定するセクションの名前</param>
		/// <returns>
		/// 指定した名前のセクションが存在する場合は true。
		/// 存在しなければ false。
		/// </returns>
		public bool Contains( string sectionName )
		{
			return _Sections.Contains( sectionName );
		}

		/// <summary>
		/// 指定した名前を持つセクションを取得します。
		/// </summary>
		/// <param name="sectionName">取得するセクションの名前</param>
		/// <returns>
		/// 指定した名前を持つセクションが存在すればそのセクションを返します。
		/// 存在しなければ null を返します。
		/// </returns>
		public Section Get( string sectionName )
		{
			int index;

			// find the section
			index = _Sections.IndexOfKey( sectionName );
			if( index == -1 )
			{
				return null; // not found
			}

			return (Section)_Sections.GetByIndex( index );
		}

		/// <summary>
		/// 指定したインデックスにあるセクションを取得します。
		/// </summary>
		/// <param name="index">取得するセクションのインデックス</param>
		/// <returns>
		/// 指定した名前を持つセクションが存在すればそのセクションを返します。
		/// 存在しなければ null を返します。
		/// </returns>
		public Section GetAt( int index )
		{
			return (Section)_Sections.GetByIndex( index );
		}
		
		/// <summary>
		/// オブジェクトが保持するセクションの列挙子を取得します。
		/// </summary>
		/// <returns>セクションの列挙子</returns>
		public IEnumerator GetEnumerator()
		{
			return new SectionEnumerator( this, _Sections );
		}

		/// <summary>
		/// 格納されているセクションの数を取得します。
		/// </summary>
		public int Count
		{
			get{ return _Sections.Count; }
		}

		/// <summary>
		/// 指定した名前を持つセクションのインデックスを取得します。
		/// </summary>
		/// <param name="sectionName">インデックスを取得するセクションの名前</param>
		/// <returns>指定した名前を持つセクションのインデックス</returns>
		public int IndexOf( string sectionName )
		{
			return _Sections.IndexOfKey( sectionName );
		}
		#endregion

		#region インデクサ等のシンタックスシュガー
		/// <summary>
		/// 指定した名前のセクションを取得します。
		/// GetSection と違い、
		/// 指定した名前のセクションが存在しない場合は作成して返します。
		/// </summary>
		public Section this[ string sectionName ]
		{
			get
			{
				// 指定名のセクションを取得
				Section section = Get( sectionName );
				if( section == null )
				{
					// 指定名のセクションが無いので
					// その名前のセクションを追加、それを返す
					Section newSection = new Section( this, sectionName );
					_Sections.Add( sectionName, newSection );
					return newSection;
				}

				return section;
			}
		}

		/// <summary>
		/// 指定したインデックスにあるセクションを取得します。
		/// </summary>
		public Section this[ int index ]
		{
			get
			{
				try
				{
					return GetAt( index );
				}
				catch( ArgumentOutOfRangeException ex )
				{
					// スタックトレースが汚くなるので投げ直す
					throw new ArgumentOutOfRangeException( ex.Message );
				}
			}
		}

		#endregion

		#region 解析ルーチン
		/// <summary>
		/// INI 形式のテキストが入ったストリームを解析。
		/// まず各行ごとに、コメント、セクション、エントリーのうちどの行か判断していく。
		/// セクションなら、内部データにセクションオブジェクトを作成してそれを選択（記憶）。
		/// エントリーなら、名前と値を抽出して、選択中セクションオブジェクトに追加する。
		/// </summary>
		/// <param name="stream">解析対象のストリーム</param>
		/// <param name="encoding">ストリームに格納された文字列のエンコーディング</param>
		/// <exception cref="IOException">
		/// ストリームの読み出し中にエラーが発生
		/// </exception>
		void Parse( Stream stream, Encoding encoding )
		{
			StreamReader	reader = new StreamReader( stream, encoding );
			string			line;
			string			name, value;
			Section			currentSection;

			// まず最初に無名セクションを作り、選択
			// （出力ロジックの都合上、無名セクションは最初に無いと困る）
			currentSection = this[""];

			// 一行ずつ読み出す
			line = reader.ReadLine();
			while( line != null )
			{
				// コメント行か？
				if( line.StartsWith(";") )
				{
					// スキップ
				}
				// セクション開始の行か？
				else if( IsSectionLine(line) )
				{
					// セクション名を抽出
					ParseLineAsSection( line, out name );
					if( name == null )
					{
						goto next_line;
					}

					// 該当セクションを取得（無ければ作成される）、それを選択
					currentSection = this[name];
				}
				// エントリーの行か？
				else if( IsEntryLine(line) )
				{
					// エントリーの行
					ParseLineAsEntry( line, out name, out value );
					if( name == null || value == null )
					{
						goto next_line; // エントリーではなかった
					}

					// 該当エントリーを選択中のセクションに挿入
					currentSection[name] = value;
				}

				next_line:
				// 次の行へ
				line = reader.ReadLine();
			}
		}


		/// <summary>
		/// セクションの行からセクション名を抽出
		/// </summary>
		/// <param name="line">解析する行</param>
		/// <param name="sectionName">セクション名</param>
		static void ParseLineAsSection( string line, out string sectionName )
		{
			int	nameBeginPos;
			int	nameEndPos;

			// セクション名開始位置を検出
			nameBeginPos = line.IndexOf( "[" );
			if( nameBeginPos == -1 )
			{
				sectionName = null;
				return; // セクションじゃないので無視
			}

			// セクション名終了位置を検出
			nameEndPos = line.LastIndexOf( "]" );
			if( nameEndPos == -1 )
			{
				sectionName = null;
				return; // セクションじゃないので無視
			}

			// セクション名を切り出す
			sectionName = line.Substring( nameBeginPos+1, nameEndPos - nameBeginPos - 1 );
		}

		
		/// <summary>
		/// エントリーの行からパラメータを抽出
		/// </summary>
		/// <param name="line">解析する行</param>
		/// <param name="entryName">エントリー名</param>
		/// <param name="entryValue">エントリーの値</param>
		static void ParseLineAsEntry( string line, out string entryName, out string entryValue )
		{
			int nameBegin, nameEnd;
			int valueBegin, valueEnd;

			// エントリー名の開始を探す
			nameBegin = Utl.IndexNotOfAny( line, " \t=", 0 );
			if( nameBegin == -1 )
			{
				goto error; // エントリー名が無い
			}

			// エントリー名の終了を探す
			nameEnd = Utl.LastIndexNotOfAny( line, " \t=", line.IndexOf("=") );
			if( nameEnd == -1 )
			{
				goto error; // エントリー名が無い
			}
			nameEnd++;

			// エントリー値の開始を探す
			valueBegin = Utl.IndexNotOfAny( line, " \t=", nameEnd );
			if( valueBegin == -1 )
			{
				goto error; // エントリー名が無い
			}

			// エントリー値の終了を探す
			valueEnd = line.Length-1;//Uty.LastIndexNotOfAny( lineIndex, " \t" );
			if( valueEnd == -1 )
			{
				goto error; // エントリー名が無い
			}
			valueEnd++;

			// エントリー名とその値を抽出
			entryName = line.Substring( nameBegin, nameEnd-nameBegin );
			entryValue = line.Substring( valueBegin, valueEnd-valueBegin );
			return;

			// エラー発生
			error:
			entryName = null;
			entryValue = null;
			return;
		}

		
		/// <summary>
		/// 指定した行がエントリーの行か判定
		/// </summary>
		/// <param name="line">判定する行</param>
		/// <returns>与えられた行がエントリーの行ならば true</returns>
		/// <remarks>'=' が先頭だとエントリー名が無い。それは不正とする</remarks>
		static bool IsEntryLine( string line )
		{
			if( Utl.IndexNotOfAny(line," \t=") < line.IndexOf("=") )
			{
				return true;
			}
			return false;
		}

		
		/// <summary>
		/// 指定した行がセクションの行か判定
		/// </summary>
		/// <param name="line">判定する行</param>
		/// <returns>与えられた行がセクションの行ならば true</returns>
		static bool IsSectionLine( string line )
		{
			if( line.StartsWith("[")
				&& line.LastIndexOf("]") != -1 )
			{
				return true;
			}
			return false;
		}
		#endregion // 解析ルーチン

		#region エントリー クラス
		/// <summary>
		/// INI 形式のエントリーを表します。
		/// </summary>
		public class Entry
		{
			string _Name, _Value;
			Section _SectionRef;
			
			/// <summary>
			/// 新しいインスタンスを生成します。
			/// Sgry.Ini の内部でしか使いません。
			/// </summary>
			public Entry( Section owner, string name, string value )
			{
				_SectionRef = owner;
				_Name = name;
				_Value = value;
			}

			/// <summary>
			/// エントリーの名前を取得または設定します。
			/// </summary>
			public string Name
			{
				get{ return _Name; }
				set{ _Name = value; }
			}
			
			/// <summary>
			/// エントリーの値を取得または設定します。
			/// </summary>
			public string Value
			{
				get{ return _Value; }
				set{ _Value = value; }
			}

			/// <summary>
			/// エントリーを INI 形式に整形した文字列に変換します。
			/// </summary>
			/// <returns>整形した文字列</returns>
			public string ToFormattedString()
			{
				return String.Format( "{0}={1}", Name, Value );
			}
		}

		#endregion

		#region セクション クラス
		/// <summary>
		/// INI 形式のセクションを表します。
		/// </summary>
		public class Section : IEnumerable
		{
			Ini			_IniRef;
			string		_Name;
			SortedList	_Dictionary = new SortedList( new StringComparer() );

			#region Init / Dispose
			/// <summary>
			/// セクションオブジェクトを生成
			/// </summary>
			/// <param name="iniRef">セクションを所有するINIオブジェクトへの参照</param>
			/// <param name="name">セクション名</param>
			public Section( Ini iniRef, string name )
			{
				_IniRef = iniRef;
				_Name = name;
			}
			#endregion

			#region メソッド型インタフェース
			/// <summary>
			/// エントリーを追加します。
			/// </summary>
			/// <param name="entryName">エントリー名</param>
			/// <param name="value">指定したエントリーに設定する値</param>
			/// <returns>追加したエントリー</returns>
			public void Add( string entryName, string value )
			{
				Entry entry = new Entry( this, entryName, value );
				_Dictionary.Add( entryName, entry );
			}

			/// <summary>
			/// 指定した名前を持つエントリーを取得します。
			/// </summary>
			/// <param name="entryName">取得するエントリー名</param>
			/// <returns>指定した名前のエントリーの値</returns>
			public Entry Get( string entryName )
			{
				if( _Dictionary.ContainsKey(entryName) )
				{
					return (Entry)_Dictionary[entryName];
				}
				else
				{
					return null;
				}
			}

			/// <summary>
			/// 指定したインデックスのエントリーを取得します。
			/// </summary>
			/// <param name="index">取得するエントリーのインデックス</param>
			/// <returns>指定したインデックスのエントリー</returns>
			public Entry GetAt( int index )
			{
				return (Entry)_Dictionary.GetByIndex( index );
			}

			/// <summary>
			/// 指定した名前を持つエントリーのインデックスを取得します。
			/// </summary>
			/// <param name="entryName">インデックスを取得するエントリーの名前</param>
			/// <returns>
			/// 指定した名前を持つエントリーが含まれればそのインデックス。
			/// 含まれなければ -1。
			/// </returns>
			public int IndexOf( string entryName )
			{
				return _Dictionary.IndexOfKey( entryName );
			}
			/// <summary>
			/// 指定した名前のエントリーが格納されているかどうかを判断します。
			/// </summary>
			/// <param name="entryName">存在を確認するエントリー名</param>
			/// <returns>
			/// 指定した名前のエントリーが格納されている場合は true。
			/// 格納されていなければ false。
			/// </returns>
			public bool Contains( string entryName )
			{
				return _Dictionary.Contains( entryName );
			}

			/// <summary>
			/// 指定した名前のエントリーを削除します。
			/// </summary>
			/// <param name="entryName">削除するエントリー名</param>
			public void Remove( String entryName )
			{
				_Dictionary.Remove( entryName );
			}

			/// <summary>
			/// 指定したインデックスのエントリーを削除します。
			/// </summary>
			/// <param name="index">削除するエントリーのインデックス</param>
			public void RemoveAt( int index )
			{
				_Dictionary.RemoveAt( index );
			}

			/// <summary>
			/// 格納されているエントリーの数を取得します。
			/// </summary>
			public int Count
			{
				get{ return _Dictionary.Count; }
			}

			/// <summary>
			/// エントリーの列挙子を取得します。
			/// </summary>
			/// <returns>エントリーの列挙子</returns>
			public IEnumerator GetEnumerator()
			{
				return new EntryEnumerator( _Dictionary );
			}
			#endregion

			#region コレクション インタフェース
			/// <summary>
			/// 指定した名前のエントリーの値を取得あるいは設定します。
			/// </summary>
			public string this[ string entryName ]
			{
				get
				{
					// 指定した名前のエントリーが存在するならそれを返す
					Entry entry = (Entry)_Dictionary[ entryName ];
					if( entry == null )
					{
						return String.Empty;
					}

					return entry.Value;
				}
				set
				{
					// 設定する値が空や null なら指定エントリーを削除
					if( value == null || value == String.Empty )
					{
						_Dictionary.Remove( entryName );
						if( _Dictionary.Count == 0 )
						{
							// エントリーを削除した結果、
							// セクションも空になったので自分自身を削除
							_IniRef._Sections.Remove( this.Name );
						}
					}
					// 普通の値ならばエントリーの値を更新
					else
					{
						Entry entry = new Entry( this, entryName, value );
						_Dictionary[ entryName ] = entry;
					}
				}
			}

			/// <summary>
			/// 指定インデックスのエントリーを取得します。
			/// </summary>
			public Entry this[ int index ]
			{
				get{ return GetAt( index ); }
			}
			#endregion

			#region プロパティ
			/// <summary>
			/// セクションの名前を取得します。
			/// </summary>
			public string Name
			{
				get{ return _Name; }
			}

			/// <summary>
			/// INI におけるセクションの位置をインデックスで取得します。
			/// </summary>
			public int Index
			{
				get{ return _IniRef.IndexOf( Name ); }
			}

			/// <summary>
			/// セクションを INI 形式に整形した文字列に変換します。
			/// </summary>
			/// <returns>整形した文字列</returns>
			public string ToFormattedString()
			{
				const string	nlCode	= "\r\n";
				StringBuilder	str = new StringBuilder( 256 );

				// セクションの開始を出力
				if( Name != String.Empty )
				{
					str.Append( "[" + Name + "]" + nlCode );
				}

				// エントリー群を書いていく
				foreach( DictionaryEntry dicEntry in _Dictionary )
				{
					Entry entry = (Entry)dicEntry.Value;
					str.Append( entry.ToFormattedString() + nlCode );
				}

				return str.ToString();
			}

			#endregion
		}
		#endregion

		#region カスタム 列挙子
		class SectionEnumerator : IEnumerator
		{
			Ini _Ini;
			SortedList _Sections;
			int _Index;

			public SectionEnumerator( Ini ini, SortedList sections )
			{
				_Ini = ini;
				_Sections = sections;
				Reset();
			}

			public void Reset()
			{
				_Index = -1;
			}

			public object Current
			{
				get{ return (Section)_Sections.GetByIndex( _Index ); }
			}

			public bool MoveNext()
			{
				if( _Sections.Count <= _Index + 1 )
				{
					return false;
				}
				
				_Index++;
				return true;
			}
		}

		class EntryEnumerator : IEnumerator
		{
			SortedList _Entries;
			int _Index;

			public EntryEnumerator( SortedList entries )
			{
				_Entries = entries;
				Reset();
			}

			public void Reset()
			{
				_Index = -1;
			}

			public object Current
			{
				get
				{
					return _Entries.GetByIndex( _Index );
				}
			}

			public bool MoveNext()
			{
				if( _Entries.Count <= _Index + 1 )
				{
					return false;
				}
				
				_Index++;
				return true;
			}
		}
		#endregion

		#region その他内部クラス
		/// <summary>
		/// 極めて単純な文字列比較オブジェクトのクラス
		/// </summary>
		class StringComparer : IComparer
		{
			public int Compare( object x, object y )
			{
				string str1 = (string)x;
				string str2 = (string)y;

				return String.Compare( str1, str2, false );
			}
		}

		class Utl
		{
			public static int IndexNotOfAny( string str, string anyOf )
			{
				return IndexNotOfAny( str, anyOf, 0 );
			}

			public static int IndexNotOfAny( string str, string anyOf, int startIndex )
			{
				int		i, j;
				bool	matchExists = false;

				for( i=startIndex; i<str.Length; i++ )
				{
					// 全該当文字について、同じかどうか確認
					for( j=0; j<anyOf.Length; j++ )
					{
						if( str[i] == anyOf[j] )
						{
							matchExists = true; // 該当文字の一つと同じ文字が見つかった
						}
					}

					if( matchExists != true )
					{
						return i;
					}

					// 次の文字へ
					matchExists = false;
				}

				return -1;
			}

			public static int LastIndexNotOfAny( string str, string anyOf )
			{
				return LastIndexNotOfAny( str, anyOf, str.Length-1 );
			}
		
			public static int LastIndexNotOfAny( string str, string anyOf, int startIndex )
			{
				int		i, j;
				bool	matchExists = false;

				for( i=startIndex; i>=0 ; i-- )
				{
					// 全該当文字について、同じかどうか確認
					for( j=0; j<anyOf.Length; j++ )
					{
						if( str[i] == anyOf[j] )
						{
							matchExists = true; // 該当文字の一つと同じ文字が見つかった
						}
					}

					if( matchExists != true )
					{
						return i;
					}

					// 次の文字へ
					matchExists = false;
				}

				return -1;
			}
		}
		#endregion // Inner classes
	}
}

/*********************************************************
Version History

[v1.2.0] 2008-09-??
・Getter を用意

[v1.1.4] 2008-08-24
・zlib license に切り替え

[v1.1.3] 2008-05-03
・.NET Compact Framework でも使えるように

[v1.1.2] 2007-04-01
・インデクサでセクションを削除すると問題が起こっていたのを修正

[v1.1.1] 2007-02-05
・インタフェースの実装抜けを修正

[v1.1.0] 2007-02-03
・インタフェースを再設計

[v1.0.0] 2007-01-01
・著作権表示の更新

[v1.0.0] 2006-09-24
・リリース

**********************************************************
Copyright (C) 2005-2008 YAMAMOTO Suguru

This software is provided 'as-is', without any express or implied
warranty. In no event will the authors be held liable for any damages
arising from the use of this software.

Permission is granted to anyone to use this software for any purpose,
including commercial applications, and to alter it and redistribute it
freely, subject to the following restrictions:

1. The origin of this software must not be misrepresented; you must not
   claim that you wrote the original software. If you use this software
   in a product, an acknowledgment in the product documentation would be
   appreciated but is not required.

2. Altered source versions must be plainly marked as such, and must not be
   misrepresented as being the original software.

3. This notice may not be removed or altered from any source distribution.
**********************************************************/
