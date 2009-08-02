// file: Ini.cs
// brief: An INI file parser.
// author: YAMAMOTO Suguru
// encoding: UTF-8
// version: 2.1.0
// platform: .NET 2.0
// create: 2006-09-24 YAMAMOTO Suguru
// update: 2009-08-02 YAMAMOTO Suguru
// license: zlib license (see END of this file)
//=========================================================
using System;
using System.Collections.Generic;
using System.IO;
using StringBuilder	= System.Text.StringBuilder;
using Encoding		= System.Text.Encoding;
using Debug			= System.Diagnostics.Debug;

namespace Sgry
{
	/// <summary>
	/// INI 形式のデータを扱うクラスです。
	/// </summary>
	/// <remarks>
	/// <para>
	/// このクラスのインスタンスは INI 形式のデータ構造を表現します。
	/// INI 形式のデータ構造は名前が付いた複数の「文字列と文字列の組の集合」が集まったものです。
	/// </para>
	/// <para>
	/// INI 形式では「AはBである」という情報を等号記号を使って「A=B」と一行で表し、
	/// この一行をエントリーと呼びます。
	/// 等号記号の前の部分（A の部分）をエントリー名、
	/// 等号記号の後ろの部分（B の部分）をエントリー値、または単純に値と呼びます。
	/// INI 形式はこれを複数集めたデータ構造です。
	/// また INI 形式では複数のエントリーをセクション（見出し）行を使って
	/// グループにまとめることができます。
	/// 具体的にはセクション名を大カッコで囲った行を書くと、
	/// それ以降のエントリーがそのセクションの中で定義されます。
	/// したがって、
	/// セクションとは名前が付いた「エントリーの集合」と考えることができ、
	/// エントリーは文字列と文字列の組と考えることができます。
	/// </para>
	/// </remarks>
	public class Ini
	{
		#region Fields
		SortedList< string, Section > _Sections = new SortedList<string, Section>( 8 );
		#endregion

		#region Properties
		/// <summary>
		/// INI 形式のセクションオブジェクトのコレクションを取得します。
		/// </summary>
		/// <remarks>
		/// このプロパティを使用すると INI データの構造に直接アクセスできます。
		/// 提供するメソッド群では実現できないような
		/// 高度な処理を行いたい場合に使用してください。
		/// </remarks>
		public virtual SortedList<string, Section> Sections
		{
			get{ return _Sections; }
		}
		#endregion

		#region Load / Save
		/// <summary>
		/// INI 形式のテキストからデータを読み出します。
		/// </summary>
		/// <param name="reader">INI 形式テキストを読みだすテキストリーダー。</param>
		/// <exception cref="ArgumentNullException">必要な引数が null でした。</exception>
		/// <exception cref="ObjectDisposedException">指定されたテキストライターはすでに閉じられており出力できません。</exception>
		/// <exception cref="IOException">ファイルを開くときにエラーが発生しました。</exception>
		/// <exception cref="OutOfMemoryException">メモリ不足で処理できませんでした。</exception>
		/// <remarks>
		/// 指定したテキストリーダーからテキストを読み出し、
		/// INI 形式のテキストとして解釈します。
		/// なお、読み出す前にオブジェクトが保持しているすべてのデータはクリアされます。
		/// </remarks>
		public virtual void Load( TextReader reader )
		{
			if( reader == null )
				throw new ArgumentNullException( "reader" );

			string	line;
			string	name, value;
			Section	section;
			Section	currentSection;

			// first of all, create unnamed section and select it
			_Sections.Clear();
			currentSection = new Section();
			_Sections.Add( "", currentSection );

			// read each lines
			line = reader.ReadLine();
			while( line != null )
			{
				// process this line according to the type of it
				if( line.StartsWith(";") )
				{
					; // this is a comment line. skip it
				}
				else if( IsSectionLine(line) )
				{
					// this is a section beginning line. extract section name
					ParseLineAsSection( line, out name );
					if( name == null )
					{
						goto next_line;
					}

					// get the section object and select it
					section = GetSection( name );
					if( section == null )
					{
						// no such section object was found; create and store it
						section = new Section();
						_Sections.Add( name, section );
					}
					currentSection = section;
				}
				else if( ParseLineAsEntry(line, out name, out value) )
				{
					// this is an entry line. extract its name and value
					if( name == null || value == null )
					{
						goto next_line;
					}

					// insert the entry into the selecting section
					currentSection[name] = value;
				}

			next_line:
				// read next line
				line = reader.ReadLine();
			}
		}

		/// <summary>
		/// INI 形式のテキストからデータを読み出します。
		/// </summary>
		/// <param name="filePath">解析する INI ファイルのパス。</param>
		/// <exception cref="ArgumentNullException">引数 filePath が null です。</exception>
		/// <exception cref="ObjectDisposedException">指定されたテキストライターはすでに閉じられており出力できません。</exception>
		/// <exception cref="PathTooLongException">指定されたパスが長すぎます。</exception>
		/// <exception cref="NotSupportedException">指定されたパスの形式はサポートしていません。</exception>
		/// <exception cref="DirectoryNotFoundException">指定されたパスが無効でアクセスできません。</exception>
		/// <exception cref="FileNotFoundException">指定されたファイルが見つかりません。</exception>
		/// <exception cref="UnauthorizedAccessException">
		///		指定したパスがディレクトリを指しているか、
		///		指定したファイルへのアクセス権がユーザにありません。
		/// </exception>
		/// <exception cref="IOException">ファイルを開くときにエラーが発生しました。</exception>
		/// <exception cref="OutOfMemoryException">メモリ不足で処理できませんでした。</exception>
		/// <remarks>
		/// 指定したパスのファイルをシステム標準のエンコーディングで開き、
		/// 内容を読み出して INI 形式のテキストとして解釈します。
		/// なお、読み出す前にオブジェクトが保持しているすべてのデータはクリアされます。
		/// </remarks>
		public virtual void Load( string filePath )
		{
			Load( filePath, Encoding.Default );
		}

		/// <summary>
		/// INI 形式のテキストからデータを読み出します。
		/// </summary>
		/// <param name="filePath">解析する INI ファイルのパス。</param>
		/// <param name="encoding">解析するファイルのエンコーディング。</param>
		/// <exception cref="ArgumentNullException">引数 filePath または encoding が null です。</exception>
		/// <exception cref="UnauthorizedAccessException">指定したパスがディレクトリであるか、指定したファイルへのアクセス権が実行ユーザにありません。</exception>
		/// <exception cref="ObjectDisposedException">指定されたテキストライターはすでに閉じられており出力できません。</exception>
		/// <exception cref="PathTooLongException">指定されたパスが長すぎます。</exception>
		/// <exception cref="NotSupportedException">指定されたパスの形式はサポートしていません。</exception>
		/// <exception cref="DirectoryNotFoundException">指定されたパスが無効でアクセスできません。</exception>
		/// <exception cref="FileNotFoundException">指定されたファイルが見つかりません。</exception>
		/// <exception cref="IOException">ファイル内容の読み出し中にエラーが発生しました。</exception>
		/// <exception cref="OutOfMemoryException">メモリ不足で処理できませんでした。</exception>
		/// <remarks>
		/// 指定したパスのファイルを指定した文字エンコーディングで開き、
		/// 内容を読み出して INI 形式のテキストとして解釈します。
		/// なお、読み出す前にオブジェクトが保持しているすべてのデータはクリアされます。
		/// </remarks>
		public virtual void Load( string filePath, Encoding encoding )
		{
			if( filePath == null )
				throw new ArgumentNullException( "filePath" );
			if( encoding == null )
				throw new ArgumentNullException( "encoding" );

			using( Stream stream = File.Open(filePath, FileMode.Open, FileAccess.Read) )
			{
				Load( new StreamReader(stream, encoding) );
			}
		}

		/// <summary>
		/// すべてのデータを INI 形式で出力します。
		/// </summary>
		/// <param name="writer">INI 形式のテキストを書き込むテキストライター。</param>
		/// <exception cref="ArgumentNullException">引数 writer が null です。</exception>
		/// <exception cref="ObjectDisposedException">指定されたテキストライターはすでに閉じられており出力できません。</exception>
		/// <exception cref="IOException">I/O エラーが発生しました。</exception>
		/// <remarks>
		/// 現在のオブジェクトが保持しているすべてのデータを
		/// INI 形式のテキストとして指定テキストライターを使って出力します。
		/// </remarks>
		/// <example>
		/// <code>
		/// Ini ini = new Ini();
		/// StringBuilder buf = new StringBuilder();
		/// 
		/// ...
		/// 
		/// // print INI data to console
		/// ini.Save( Console.Out );
		/// 
		/// // copy INI data to memory stream
		/// ini.Save( new StringWriter(buf) );
		/// Console.Write( buf.ToString() );
		/// </code>
		/// </example>
		public virtual void Save( TextWriter writer )
		{
			if( writer == null )
				throw new ArgumentNullException( "writer" );

			// write each sections
			foreach( KeyValuePair<string, Section> item in _Sections )
			{
				// write section beginning if this section has a name
				if( item.Key != String.Empty )
				{
					writer.WriteLine( "[{0}]", item.Key );
				}

				// then, write down all entries in this section
				foreach( KeyValuePair<string, string> section in item.Value )
				{
					writer.WriteLine( "{0}={1}", section.Key, section.Value );
				}
			}
		}

		/// <summary>
		/// すべてのデータを INI 形式で出力します。
		/// </summary>
		/// <param name="filePath">保存するファイルのパス。</param>
		/// <param name="encoding">保存するファイルのエンコーディング。</param>
		/// <exception cref="ArgumentNullException">引数の一つ以上が null です。</exception>
		/// <exception cref="ArgumentException">引数 newLineCode が空文字です。</exception>
		/// <exception cref="PathTooLongException">パス文字列が長すぎます。</exception>
		/// <exception cref="NotSupportedException">パス文字列として指定された文字列はサポートしている書式ではありません。</exception>
		/// <exception cref="DirectoryNotFoundException">パスで指定されたディレクトリが見つかりません。</exception>
		/// <exception cref="FileNotFoundException">パスで指定されたファイルが見つかりません。</exception>
		/// <exception cref="UnauthorizedAccessException">
		///		指定したパスがディレクトリを指しているか、
		///		指定したファイルを読み出す権限が実行ユーザにありません。
		/// </exception>
		/// <exception cref="IOException">ファイルを開くときに I/O エラーが発生しました。</exception>
		/// <exception cref="OutOfMemoryException">メモリ不足で処理できませんでした。</exception>
		public virtual void Save( string filePath, Encoding encoding )
		{
			Save( filePath, encoding, "\r\n" );
		}

		/// <summary>
		/// すべてのデータを INI 形式で出力します。
		/// </summary>
		/// <param name="filePath">保存するファイルのパス。</param>
		/// <param name="encoding">保存するファイルのエンコーディング。</param>
		/// <param name="newLineCode">使用する改行コード。</param>
		/// <exception cref="ArgumentNullException">引数の一つ以上が null です。</exception>
		/// <exception cref="ArgumentException">引数 newLineCode が空文字です。</exception>
		/// <exception cref="PathTooLongException">パス文字列が長すぎます。</exception>
		/// <exception cref="NotSupportedException">パス文字列として指定された文字列はサポートしている書式ではありません。</exception>
		/// <exception cref="DirectoryNotFoundException">パスで指定されたディレクトリが見つかりません。</exception>
		/// <exception cref="FileNotFoundException">パスで指定されたファイルが見つかりません。</exception>
		/// <exception cref="UnauthorizedAccessException">
		///		指定したパスがディレクトリを指しているか、
		///		指定したファイルを読み出す権限が実行ユーザにありません。
		/// </exception>
		/// <exception cref="IOException">ファイルを開くときに I/O エラーが発生しました。</exception>
		/// <exception cref="OutOfMemoryException">メモリ不足で処理できませんでした。</exception>
		public virtual void Save( string filePath, Encoding encoding, string newLineCode )
		{
			if( filePath == null )
				throw new ArgumentNullException( "filePath" );
			if( encoding == null )
				throw new ArgumentNullException( "encoding" );
			if( newLineCode == null )
				throw new ArgumentNullException( "newLineCode" );
			if( newLineCode == "" )
				throw new ArgumentException( "parameter newLineCode must not be an empty string." );

			// write INI formatted string into the file
			using( FileStream file = File.Open(filePath, FileMode.Create, FileAccess.Write) )
			{
				StreamWriter writer = new StreamWriter( file, encoding );
				writer.NewLine = newLineCode;
				Save( writer );
				writer.Close();
			}
		}
		#endregion

		#region Get / Set
		/// <summary>
		/// Bool や Double、列挙子といった基本的な値型の値を取得します。
		/// </summary>
		/// <param name="sectionName">値を検索するセクション名。</param>
		/// <param name="entryName">値に関連付けられた名前（エントリー名）。</param>
		/// <param name="defaultValue">値が見つからなかった場合に使用する標準値。</param>
		/// <returns>見つかった値。見つからなかった場合は指定した標準値。</returns>
		/// <exception cref="ArgumentNullException">必要な引数に null が指定されました。</exception>
		/// <exception cref="ArgumentException">entryName に空文字列が指定されました。</exception>
		/// <remarks>
		/// このメソッドは、引数 <paramref name="sectionName"/> で指定した名前のセクション中から
		/// 引数 <paramref name="entryName"/> で指定した名前を持つエントリーを検索し、
		/// そのエントリー値を型 T のオブジェクトとして取得します。
		/// 指定したセクションやエントリーが見つからない場合は
		/// defaultValue パラメータで指定した値を返します。
		/// また、見つけた値が型 T として解釈不可能だった場合にも
		/// defaultValue パラメータで指定した値を返します。
		/// </remarks>
		public virtual T Get<T>( string sectionName, string entryName, T defaultValue ) where T : struct
		{
			string valueStr;
			
			if( sectionName == null )
				throw new ArgumentNullException( "sectionName" );
			if( entryName == null )
				throw new ArgumentNullException( "entryName" );
			if( entryName == "" )
				throw new ArgumentException( "parameter entryName cannot be an empty string." );

			// retrieve the value as string (or null if not found)
			valueStr = Get( sectionName, entryName, null );

			// parse it as the type T
			try
			{
				if( defaultValue is Enum )
					return (T)Enum.Parse( typeof(T), valueStr, false );
				else
					return (T)Convert.ChangeType( valueStr, typeof(T), null );
			}
			catch( FormatException )
			{}
			catch( ArgumentException )	// case of valueStr is not recognizable as the enum value etc.
			{}
			catch( InvalidCastException )	// case of Convert.ChangeType(null, ...) on .NET Framework
			{}

			return defaultValue;
		}

		/// <summary>
		/// 文字列値を取得します。
		/// </summary>
		/// <param name="sectionName">値を検索するセクションの名前。</param>
		/// <param name="entryName">検索するエントリーの名前。</param>
		/// <param name="defaultValue">値が見つからなかった場合に使用する標準値。</param>
		/// <exception cref="ArgumentNullException">必要な引数に null が指定されました。</exception>
		/// <exception cref="ArgumentException">entry に空文字列が指定されました。</exception>
		/// <returns>見つかった文字列値。見つからなかった場合は指定した標準値。</returns>
		public virtual string Get( string sectionName, string entryName, string defaultValue )
		{
			Section section;

			if( sectionName == null )
				throw new ArgumentNullException( "sectionName" );
			if( entryName == null )
				throw new ArgumentNullException( "entryName" );
			if( entryName == "" )
				throw new ArgumentException( "parameter entryName cannot be an empty string." );

			// get the section
			section = GetSection( sectionName );
			if( section == null )
			{
				// there is no such section so just return default value.
				return defaultValue;
			}

			// get value of the entry
			try
			{
				return section[ entryName ];
			}
			catch( KeyNotFoundException )
			{
				// there is no such entry so using default value.
				return defaultValue;
			}
		}

		/// <summary>
		/// 数値を取得します。
		/// </summary>
		/// <param name="sectionName">値を検索するセクションの名前。</param>
		/// <param name="entryName">検索するエントリーの名前。</param>
		/// <param name="minValue">取得する数値の期待される最小値。</param>
		/// <param name="maxValue">取得する数値の期待される最大値。</param>
		/// <param name="defaultValue">値が見つからなかった場合に使用する標準値。</param>
		/// <returns>見つかった数値。見つからなかった場合は指定した標準値。</returns>
		/// <exception cref="ArgumentNullException">引数 sectionName か entryName が null です。</exception>
		/// <exception cref="ArgumentException">entryName に空文字列が指定されました。</exception>
		/// <remarks>
		/// <para>
		/// このメソッドは、引数 <paramref name="sectionName"/> で指定した名前のセクション中から
		/// 引数 <paramref name="entryName"/> で指定した名前を持つエントリーを検索し、
		/// そのエントリー値を数値として解釈して取得します。
		/// <para>
		/// </para>
		///	指定したセクションやエントリーが見つからないか、
		///	見つかった値が数値として解釈できないか、
		///	見つかった数値が minValue 以上 maxValue 以下の範囲に収まっていない場合は、
		/// 引数 defaultValue で指定した値が返ります。
		/// </para>
		/// </remarks>
		public virtual int GetInt( string sectionName, string entryName, int minValue, int maxValue, int defaultValue )
		{
			string stringValue;
			int intValue;

			if( sectionName == null )
				throw new ArgumentNullException( "sectionName" );
			if( entryName == "" )
				throw new ArgumentException( "parameter entryName cannot be an empty string." );
			if( entryName == null )
				throw new ArgumentNullException( "entryName" );
			if( maxValue < minValue )
				throw new ArgumentException( "minValue must be equal or less than maxValue." );

			// get the value as a string
			stringValue = Get( sectionName, entryName, null );
			if( stringValue == null )
			{
				return defaultValue; // not such entry
			}

			// convert it to an integer
			try
			{
				intValue = Int32.Parse( stringValue );
			}
			catch( Exception ex )
			{
				Debug.Assert( ex is FormatException || ex is OverflowException );
				return defaultValue;
			}

			// check boundary
			if( intValue < minValue || maxValue < intValue )
			{
				return defaultValue;
			}

			return intValue;
		}

		/// <summary>
		/// 値を設定します。
		/// </summary>
		/// <typeparam name="T">値として使用するオブジェクトの型。</typeparam>
		/// <param name="sectionName">値を検索するセクションの名前。</param>
		/// <param name="entryName">検索するエントリーの名前。</param>
		/// <param name="value">新しいエントリー値。</param>
		/// <exception cref="ArgumentNullException">引数の一つ以上が null です。</exception>
		/// <exception cref="ArgumentException">引数 entryName に空文字列が指定されました。</exception>
		/// <remarks>
		/// このメソッドは、引数 <paramref name="sectionName"/> で指定した名前のセクション中から
		/// 引数 <paramref name="entryName"/> で指定した名前を持つエントリーを検索し、
		/// そのエントリー値を設定します。
		/// もし指定したセクションやエントリーが存在していない場合、
		/// このメソッドは内部で新たにセクションやエントリーを作成した上で値を設定します。
		/// なお、エントリー値としては <paramref name="value"/> の
		/// ToString メソッドを呼び出した結果の文字列が使用されます。
		/// </remarks>
		public virtual void Set<T>( string sectionName, string entryName, T value )
		{
			Section section;

			if( sectionName == null )
				throw new ArgumentNullException( "sectionName" );
			if( entryName == null )
				throw new ArgumentNullException( "entryName" );
			if( entryName == "" )
				throw new ArgumentException( "parameter entryName cannot be an empty string." );
			if( value == null )
				throw new ArgumentNullException( "value" );

			// get the section
			section = GetSection( sectionName );
			if( section == null )
			{
				section = new Section();
				_Sections.Add( sectionName, section );
			}

			// set the entry value
			section[ entryName ] = value.ToString();
		}
		#endregion

		#region Delete / Clear
		/// <summary>
		/// エントリーを削除します。
		/// </summary>
		/// <param name="sectionName">値が含まれるセクションの名前。</param>
		/// <param name="entryName">削除するエントリーの名前。</param>
		/// <exception cref="ArgumentNullException">引数に null が指定されました。</exception>
		/// <remarks>
		/// 指定した名前のエントリーを検索し、そのエントリーを削除します。
		/// 指定した名前のエントリー（セクション）が存在しない場合、何も起こりません。
		/// </remarks>
		public virtual void Remove( string sectionName, string entryName )
		{
			Section section;

			if( sectionName == null )
				throw new ArgumentNullException( "sectionName" );
			if( entryName == null )
				throw new ArgumentNullException( "entryName" );

			try
			{
				// 指定セクションを取得
				section = _Sections[sectionName];

				// 指定エントリーがあれば削除
				section.Remove( entryName );
				if( section.Count == 0 )
				{
					// もうエントリーが一つも無いのでセクションも削除
					_Sections.Remove( sectionName );
				}
			}
			catch( KeyNotFoundException )
			{}
		}

		/// <summary>
		/// すべてのデータを消去します。
		/// </summary>
		public virtual void Clear()
		{
			_Sections.Clear();
		}
		#endregion

		#region Parse logic
		/// <summary>
		/// セクションの行からセクション名を抽出します。
		/// </summary>
		/// <param name="line">解析する行</param>
		/// <param name="sectionName">セクション名</param>
		static void ParseLineAsSection( string line, out string sectionName )
		{
			int	nameBeginPos;
			int	nameEndPos;

			// find where the section name begins
			nameBeginPos = line.IndexOf( "[" );
			if( nameBeginPos == -1 )
			{
				sectionName = null;
				return;
			}

			// find where the section name ends
			nameEndPos = line.LastIndexOf( "]" );
			if( nameEndPos == -1 )
			{
				sectionName = null;
				return;
			}

			// extract section name
			sectionName = line.Substring( nameBeginPos+1, nameEndPos - nameBeginPos - 1 );
		}

		/// <summary>
		/// エントリーの行からパラメータを抽出します。
		/// </summary>
		/// <param name="line">解析する行</param>
		/// <param name="entryName">エントリー名</param>
		/// <param name="entryValue">エントリーの値</param>
		static bool ParseLineAsEntry( string line, out string entryName, out string entryValue )
		{
			int nameBegin, nameEnd;
			int valueBegin, valueEnd;

			// find where the entry name begins
			nameBegin = Utl.IndexNotOfAny( line, " \t=", 0 );
			if( nameBegin == -1 )
			{
				goto error;
			}

			// find where the entry name ends
			nameEnd = Utl.LastIndexNotOfAny( line, " \t=", line.IndexOf("=") );
			if( nameEnd == -1 )
			{
				goto error;
			}
			nameEnd++;

			// find where the entry value begins
			valueBegin = Utl.IndexNotOfAny( line, " \t=", nameEnd );
			if( valueBegin == -1 )
			{
				goto error;
			}

			// find where the entry value ends
			valueEnd = line.Length - 1;
			if( valueEnd == -1 )
			{
				Debug.Fail( "this case must not be occurred." );
				goto error;
			}
			valueEnd++;

			// extract entry's name and value
			entryName = line.Substring( nameBegin, nameEnd-nameBegin );
			entryValue = line.Substring( valueBegin, valueEnd-valueBegin );
			
			return true;

		error:
			// if an error has occured, clear output values and exit
			entryName = null;
			entryValue = null;

			return false;
		}

		/// <summary>
		/// 指定した行がエントリーの行かどうか判定します。
		/// </summary>
		/// <param name="line">判定する行</param>
		/// <returns>与えられた行がエントリーの行ならば true</returns>
		/// <remarks>
		/// 指定した行がエントリーの行かどうかを判定します。
		/// 先頭に '=' がある場合はエントリー名が無いエントリーと解釈できますが、
		/// これは不正と判定します。
		/// </remarks>
		static bool IsEntryLine( string line )
		{
			if( Utl.IndexNotOfAny(line," \t=") < line.IndexOf("=") )
			{
				return true;
			}
			return false;
		}

		/// <summary>
		/// 指定した行がセクションの行かどうかを判定します。
		/// </summary>
		/// <param name="line">判定する行</param>
		/// <returns>与えられた行がセクションの行ならば true。</returns>
		static bool IsSectionLine( string line )
		{
			if( line.StartsWith("[")
				&& line.LastIndexOf("]") != -1 )
			{
				return true;
			}
			return false;
		}
		#endregion

		#region Utilities
		/// <summary>
		/// INI 形式のセクションを表します。
		/// 実質は Dictionary&lt;string, string&gt; と何も違いません。
		/// </summary>
		public class Section : Dictionary<string, string>
		{}

		Section GetSection( string sectionName )
		{
			try
			{
				return _Sections[ sectionName ];
			}
			catch( KeyNotFoundException )
			{
				return null;
			}
		}

		static class Utl
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
					// check whether this character is not same of each characters given
					for( j=0; j<anyOf.Length; j++ )
					{
						if( str[i] == anyOf[j] )
						{
							matchExists = true; // one of the given characters were same
							break;
						}
					}

					// if there was a character matched, this index is the one to be returned
					if( matchExists != true )
					{
						return i;
					}

					// go to next character
					matchExists = false;
				}

				return -1;
			}

			public static int LastIndexNotOfAny( string str, string anyOf, int startIndex )
			{
				int		i, j;
				bool	matchExists = false;

				for( i=startIndex; i>=0 ; i-- )
				{
					// check whether this character is not same of each characters given
					for( j=0; j<anyOf.Length; j++ )
					{
						if( str[i] == anyOf[j] )
						{
							matchExists = true; // one of the given characters were same
							break;
						}
					}

					// if there was a character matched, this index is the one to be returned
					if( matchExists != true )
					{
						return i;
					}

					// go to next character
					matchExists = false;
				}

				return -1;
			}
		}
		#endregion

		#region Unit Test
#		if DEBUG
		internal static void Test()
		{
			string test_data_in = @"
AAA=aaa
aAa=BbB
WindowWidth=400
Number=-1200
; lines starting with semicolon will be treated as a comment line and ignored
; WindowHeight=300

[Section]
hoge	=  white spaces around the equal sign will be removed
Number=9.876543
[not a section = g
bar =
foo

; if multiple section has same name, their entires will be merged into single section object
[Section]
foo=bar
foo = value of foo will be overwritten by this
day_of_week=Sunday

[section]
hoge = section name is case-sensitive
";
			string test_data_out = @"AAA=aaa
aAa=BbB
WindowWidth=400
Number=-1200
[section]
hoge=section name is case-sensitive
[Section]
hoge=white spaces around the equal sign will be removed
Number=9.876543
[not a section=g
foo=value of foo will be overwritten by this
day_of_week=Sunday
";
			Ini ini = new Ini();

			Console.WriteLine( "[Sgry.Ini]" );

			// load / save
			{
				StringReader sr = new StringReader( test_data_in );
				StringBuilder buf = new StringBuilder();

				// load
				ini.Load( sr );
				Debug.Assert( ini.Get("", "AAA", "abc") == "aaa" );
				Debug.Assert( ini.Get("", "aAa", "abc") == "BbB" );
				Debug.Assert( ini.Get("", "WindowWidth", "hoge") == "400" );
				Debug.Assert( ini.Get("", "WindowHeight", "hoge") == "hoge" );
				Debug.Assert( ini.Get("Section", "hoge", "PIYO") == "white spaces around the equal sign will be removed" );
				Debug.Assert( ini.Get("Section", "foo", "PIYO") == "value of foo will be overwritten by this" );
				Debug.Assert( ini.Get("section", "hoge", "PIYO") == "section name is case-sensitive" );

				// save to string stream
				ini.Save( new StringWriter(buf) );
				Debug.Assert( buf.ToString() == test_data_out );

				// save to file （NUL文字区切りの変則書式を使ってみる）
				ini.Save( "Sgry.Ini.UnitTest.Reuslt.ini", Encoding.UTF8, "\0" );
				using( StreamReader reader = File.OpenText("Sgry.Ini.UnitTest.Reuslt.ini") )
				{
					Debug.Assert( reader.ReadToEnd().Replace("\0", "\r\n") == test_data_out );
				}
			}

			// setter
			{
				// null section name
				try{ ini.Set(null, "fuga", "hoge"); Debug.Fail("exception no thrown as expected."); }
				catch( Exception ex ){ Debug.Assert(ex.GetType() == typeof(ArgumentNullException) ); }

				// null entry name
				try{ ini.Set("fuga", null, "hoge"); Debug.Fail("exception no thrown as expected."); }
				catch( Exception ex ){ Debug.Assert(ex.GetType() == typeof(ArgumentNullException) ); }

				// null value
				try{ ini.Set("fuga", "hoge", (object)null); Debug.Fail("exception no thrown as expected."); }
				catch( Exception ex ){ Debug.Assert(ex.GetType() == typeof(ArgumentNullException) ); }

				// empty entry name
				try{ ini.Set("", "", "hoge"); Debug.Fail("exception no thrown as expected."); }
				catch( Exception ex ){ Debug.Assert(ex.GetType() == typeof(ArgumentException) ); }
			}

			// getter (string)
			{
				// null section name
				try{ ini.Get(null, "fuga", "hoge"); Debug.Fail("exception no thrown as expected."); }
				catch( Exception ex ){ Debug.Assert(ex.GetType() == typeof(ArgumentNullException) ); }

				// null entry name
				try{ ini.Get("fuga", null, "hoge"); Debug.Fail("exception no thrown as expected."); }
				catch( Exception ex ){ Debug.Assert(ex.GetType() == typeof(ArgumentNullException) ); }

				// null value
				Debug.Assert( ini.Get("fuga", "hoge", null) == null );

				// empty entry name
				try{ ini.Get("", "", "hoge"); Debug.Fail("exception no thrown as expected."); }
				catch( Exception ex ){ Debug.Assert(ex.GetType() == typeof(ArgumentException) ); }

				// non-existing section
				Debug.Assert( ini.Get("NoSuchSection", "hoge", "FUGA") == "FUGA" );

				// anonymous section
				Debug.Assert( ini.Get("", "hoge", "FUGA") == "FUGA" );
				ini.Set( "", "hoge", "HOGE" );
				Debug.Assert( ini.Get("", "hoge", "FUGA") == "HOGE" );

				// named section
				Debug.Assert( ini.Get("Sec", "hoge", 1) == 1 );
				ini.Set( "Sec", "hoge", 7 );
				Debug.Assert( ini.Get("Sec", "hoge", 7) == 7 );
			}

			// getter (int)
			{
				// null section name
				try{ ini.GetInt(null, "Section", -10, 10, 0); Debug.Fail("exception no thrown as expected."); }
				catch( Exception ex ){ Debug.Assert(ex.GetType() == typeof(ArgumentNullException) ); }

				// null entry name
				try{ ini.GetInt("Section", null, -10, 10, 0); Debug.Fail("exception no thrown as expected."); }
				catch( Exception ex ){ Debug.Assert(ex.GetType() == typeof(ArgumentNullException) ); }

				// default value out of range
				Debug.Assert( 3000 == ini.GetInt("", "WindowHeight", -1000, 1000, 3000) );

				// bounds
				Debug.Assert( 0 == ini.GetInt("", "Number", -1199, 1200, 0) );
				Debug.Assert( -1200 == ini.GetInt("", "Number", -1200, 1200, 0) );
				Debug.Assert( 400 == ini.GetInt("", "WindowWidth", -400, 400, 0) );
				Debug.Assert( 0 == ini.GetInt("", "WindowWidth", -400, 399, 0) );
			}

			// getter (generic)
			{
				// null section name
				try{ ini.Get(null, "fuga", 3.14); Debug.Fail("exception no thrown as expected."); }
				catch( Exception ex ){ Debug.Assert(ex.GetType() == typeof(ArgumentNullException) ); }

				// null entry name
				try{ ini.Get("fuga", null, 3.14); Debug.Fail("exception no thrown as expected."); }
				catch( Exception ex ){ Debug.Assert(ex.GetType() == typeof(ArgumentNullException) ); }

				// empty entry name
				try{ ini.Get("", "", 3.14); Debug.Fail("exception no thrown as expected."); }
				catch( Exception ex ){ Debug.Assert(ex.GetType() == typeof(ArgumentException) ); }

				// enum values
				Debug.Assert( ini.Get("Section", "day_of_week", DayOfWeek.Wednesday) == DayOfWeek.Sunday );
				Debug.Assert( ini.Get("Section", "Number", DayOfWeek.Wednesday) == DayOfWeek.Wednesday );
				Debug.Assert( ini.Get("Section", "NNNNN", DayOfWeek.Wednesday) == DayOfWeek.Wednesday );

				// anonymous section
				Debug.Assert( ini.Get("", "Number", 3.14) == -1200.0 );
				ini.Set( "", "Number", "HOGE" );
				Debug.Assert( ini.Get("", "Number", "FUGA") == "HOGE" );

				// named section
				Debug.Assert( ini.Get("Section", "Number", 3.14) == 9.876543 );
				Debug.Assert( ini.Get("Section", "Number", 7) == 7 );
			}

			// remove
			{
				// null section
				try{ ini.Remove(null, "entry_name"); Debug.Fail("exception no thrown as expected."); }
				catch( Exception ex ){ Debug.Assert(ex.GetType() == typeof(ArgumentNullException) ); }

				// null entry
				try{ ini.Remove("section", null); Debug.Fail("exception no thrown as expected."); }
				catch( Exception ex ){ Debug.Assert(ex.GetType() == typeof(ArgumentNullException) ); }

				// remove non-existing entry
				int prevCount = ini.Sections["section"].Count;
				ini.Remove( "section", "NoSuchEntry" );
				Debug.Assert( prevCount == ini.Sections["section"].Count );

				// remove existing entry (the entry is the last entry so the section will also be removed)
				ini.Remove( "section", "hoge" );
				Debug.Assert( ini.Sections.ContainsKey("section") == false );

				// clear
				ini.Clear();
				Debug.Assert( ini.Sections.Count == 0 );
			}

			Console.WriteLine( "done." );
			Console.WriteLine();
		}
#		endif
		#endregion
	}
}

/*********************************************************
Version History

[v2.1.0] 2009-08-02
・Ini.Get<T> を enum に対応させた

[v2.0.0] 2009-03-29
・ジェネリックを使ってフルスクラッチで書き換え

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
・MIT License の著作権表示を追記

[v1.0.0] 2006-09-24
・リリース

**********************************************************
Copyright (C) 2006-2009 YAMAMOTO Suguru

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
