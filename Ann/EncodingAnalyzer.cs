// file: EncodingAnalyzer.cs
// brief: Japanese charactor encoding analyzer.
// author: YAMAMOTO Suguru
// encoding: UTF-8
// version: 1.3.0
// platform: .NET 1.1
// create: 2006-05-17 YAMAMOTO Suguru
// update: 2009-03-29 YAMAMOTO Suguru
// license: zlib license (see END of this file)
//=========================================================
using System;
using System.IO;
using System.Text;
using Debug = System.Diagnostics.Debug;

namespace Sgry
{
	/// <summary>
	/// 日本語の文字エンコーディングを推定する機能を提供します。
	/// </summary>
	/// <remarks>
	/// 日本語の文字エンコーディングを推定する機能をします。
	/// 対応しているエンコーディングは
	/// ISO-2022-JP (JIS)、Shift_JIS、EUC-JP、UTF-8、UTF-16 です。
	/// なお UTF-16 については BOM (Byte Order Mark) の有無でしか判定していないため、
	/// BOM の無い UTF-16 エンコーディングされたデータは推定できません。
	/// </remarks>
	public class EncodingAnalyzer
	{
		/// <summary>
		/// （このクラスはインスタンス化できません。）
		/// </summary>
		EncodingAnalyzer()
		{}

		#region Public interface
		/// <summary>
		/// 日本語文字エンコーディングを推定します。
		/// </summary>
		/// <param name="text">推定対象となるバイト列。</param>
		/// <returns>推定されたエンコーディング。</returns>
		/// <exception cref="ArgumentNullException">引数 text が null です。</exception>
		/// <remarks>
		/// 指定したバイト列を日本語文字列と解釈して使われている文字エンコーディングを推定します。
		/// ASCII 文字しか含まれないようなバイト列はシステム標準のエンコーディングとみなします。
		/// またどんなデータでも何かの文字エンコーディングであると算出するため null は返しません。
		/// </remarks>
		public static Encoding Analyze( byte[] text )
		{
			bool dummy;
			return Analyze( text, out dummy );
		}

		/// <summary>
		/// 日本語文字エンコーディングを推定します。
		/// </summary>
		/// <param name="text">推定対象となるバイト列。</param>
		/// <param name="withBom">この変数に Byte Order Mark が付いているかどうかが格納されます。</param>
		/// <returns>推定されたエンコーディング。</returns>
		/// <exception cref="ArgumentNullException">引数 text が null です。</exception>
		/// <remarks>
		/// 指定したバイト列を日本語文字列と解釈して使われている文字エンコーディングを推定します。
		/// ASCII 文字しか含まれないようなバイト列はシステム標準のエンコーディングとみなします。
		/// またどんなデータでも何かの文字エンコーディングであると算出するため null は返しません。
		/// </remarks>
		public static Encoding Analyze( byte[] text, out bool withBom )
		{
			int eucPoint, sjisPoint;

			if( text == null )
				throw new ArgumentNullException( "text" );
			
			// UTF-16?
			if( IsUtf16Le(text) )
			{
				withBom = true;
				return Encoding.Unicode;
			}
			if( IsUtf16Be(text) )
			{
				withBom = true;
				return Encoding.BigEndianUnicode;
			}

			// JIS?
			if( IsJis(text) )
			{
				withBom = false;
				return Encoding.GetEncoding( "iso-2022-jp" );
			}
			
			// UTF-8 or ASCII?
			if( IsUtf8(text, out withBom) )
			{
				// possibly it is ASCII. check about it.
				if( IsAscii(text) )
				{
					withBom = false;
					return Encoding.Default; // for ASCII, use system default encoding.
				}

				return Encoding.UTF8;
			}

			// EUC-JP or Shift_JIS?
			// calculate improbability point for each encoding
			eucPoint = CalcEucImprobabilityPoint( text );
			sjisPoint = CalcSjisImprobabilityPoint( text );
			if( eucPoint < sjisPoint )
			{
				withBom = false;
				return Encoding.GetEncoding( "euc-jp" );
			}
			else
			{
				withBom = false;
				return Encoding.GetEncoding( "shift_jis" );
			}
		}

		/// <summary>
		/// 日本語文字エンコーディングを推定します。
		/// </summary>
		/// <param name="filePath">文字エンコーディングを推定するファイルのパス。</param>
		/// <returns>推定されたエンコーディング。</returns>
		/// <exception cref="ArgumentNullException">引数 path が null です。</exception>
		/// <exception cref="PathTooLongException">指定されたパスが長すぎます。</exception>
		/// <exception cref="NotSupportedException">指定されたパスの形式はサポートしていません。</exception>
		/// <exception cref="DirectoryNotFoundException">指定されたパスが無効でアクセスできません。</exception>
		/// <exception cref="FileNotFoundException">指定されたファイルが見つかりません。</exception>
		/// <exception cref="UnauthorizedAccessException">
		///		指定したパスがディレクトリを指しているか、
		///		指定したファイルへのアクセス権がユーザにありません。
		/// </exception>
		/// <exception cref="IOException">ファイルを開くときにエラーが発生しました。</exception>
		/// <remarks>
		/// 指定したファイルの内容を読み出して日本語文字列と解釈し、
		/// 使われている文字エンコーディングを推定します。
		/// 読み出したファイルの内容はメソッド内部で破棄されます。
		/// ASCII 文字しか含まれないようなバイト列は
		/// システム標準のエンコーディングとみなします。
		/// またどんなデータでも何かの文字エンコーディングであると算出するため null は返しません。
		/// </remarks>
		public static Encoding Analyze( string filePath )
		{
			bool dummy;
			byte[] fileContent;
			return Analyze( filePath, out dummy, out fileContent );
		}

		/// <summary>
		/// 日本語文字エンコーディングを推定します。
		/// </summary>
		/// <param name="filePath">文字エンコーディングを推定するファイルのパス。</param>
		/// <param name="withBom">この変数に Byte Order Mark が付いているかどうかが格納されます。</param>
		/// <returns>推定されたエンコーディング。</returns>
		/// <exception cref="ArgumentNullException">引数 path が null です。</exception>
		/// <exception cref="PathTooLongException">指定されたパスが長すぎます。</exception>
		/// <exception cref="NotSupportedException">指定されたパスの形式はサポートしていません。</exception>
		/// <exception cref="DirectoryNotFoundException">指定されたパスが無効でアクセスできません。</exception>
		/// <exception cref="FileNotFoundException">指定されたファイルが見つかりません。</exception>
		/// <exception cref="UnauthorizedAccessException">
		///		指定したパスがディレクトリを指しているか、
		///		指定したファイルへのアクセス権がユーザにありません。
		/// </exception>
		/// <exception cref="IOException">ファイルを開くときにエラーが発生しました。</exception>
		/// <remarks>
		/// 指定したファイルの内容を読み出して日本語文字列と解釈し、
		/// 使われている文字エンコーディングを推定します。
		/// 読み出したファイルの内容はメソッド内部で破棄されます。
		/// ASCII 文字しか含まれないようなバイト列は
		/// システム標準のエンコーディングとみなします。
		/// またどんなデータでも何かの文字エンコーディングであると算出するため null は返しません。
		/// </remarks>
		public static Encoding Analyze( string filePath, out bool withBom )
		{
			byte[] fileContent;
			return Analyze( filePath, out withBom, out fileContent );
		}

		/// <summary>
		/// 日本語文字エンコーディングを推定します。
		/// </summary>
		/// <param name="filePath">文字エンコーディングを推定するファイルのパス。</param>
		/// <param name="withBom">この変数に Byte Order Mark が付いているかどうかが格納されます。</param>
		/// <param name="fileContent">読みだしたファイルの内容がこの変数に格納されます。</param>
		/// <returns>推定されたエンコーディング。</returns>
		/// <exception cref="ArgumentNullException">引数 path が null です。</exception>
		/// <exception cref="PathTooLongException">指定されたパスが長すぎます。</exception>
		/// <exception cref="NotSupportedException">指定されたパスの形式はサポートしていません。</exception>
		/// <exception cref="DirectoryNotFoundException">指定されたパスが無効でアクセスできません。</exception>
		/// <exception cref="FileNotFoundException">指定されたファイルが見つかりません。</exception>
		/// <exception cref="UnauthorizedAccessException">
		///		指定したパスがディレクトリを指しているか、
		///		指定したファイルへのアクセス権がユーザにありません。
		/// </exception>
		/// <exception cref="IOException">ファイルを開くときにエラーが発生しました。</exception>
		/// <remarks>
		/// 指定したファイルの内容を読み出して日本語文字列と解釈し、
		/// 使われている文字エンコーディングを推定します。
		/// 読み出したファイルの内容はメソッド内部で破棄されず、
		/// 引数 fileContent で与えた変数に設定して返します。
		/// ASCII 文字しか含まれないようなバイト列は
		/// システム標準のエンコーディングとみなします。
		/// またどんなデータでも何かの文字エンコーディングであると算出するため null は返しません。
		/// </remarks>
		/// <example>
		/// <code>
		/// Encoding encoding;
		/// bool withBom;
		/// byte[] content;
		/// string text;
		/// 
		/// // 文字コードを推定
		/// encoding = EncodingAnalyzer.Analyze( "sample.txt", out withBom, out content );
		/// 
		/// // 指定文字コードとして Unicode に変換
		/// text = encoding.GetString( content );
		/// if( withBom )
		/// 	text = text.Substring( 1 ); // 効率悪いがサンプルなのでご勘弁・・・
		/// 
		/// // 結果を表示
		/// Console.WriteLine(
		/// 	"sample.txt: encoding={0}, BOM={1}, content=[{2}]",
		/// 	encoding.WebName,
		/// 	withBom,
		/// 	encoding.GetString(content).Substring( 0, Math.Min(8, content.Length) )
		/// );
		/// </code>
		/// </example>
		public static Encoding Analyze( string filePath, out bool withBom, out byte[] fileContent )
		{
			FileStream file = null;
			
			// read all bytes from the file
			using( file = File.Open(filePath, FileMode.Open, FileAccess.Read) )
			{
				fileContent = new byte[ file.Length ];
				file.Read( fileContent, 0, (int)file.Length );
			}

			// analyze it
			return Analyze( fileContent, out withBom );
		}
		#endregion

		#region Analysis Logic
		/// <summary>
		/// 指定バイト列が ASCII 文字列かどうかを判定します。
		/// </summary>
		static bool IsAscii( byte[] text )
		{
			Debug.Assert( text != null );

			// ASCII として不正な文字を検索
			foreach( byte by in text )
			{
				if( by < 0x20 || 0x7E < by )
				{
					if( by != 0x0A && by != 0x0D )
					{
						return false;
					}
				}
			}

			// すべて ASCII として正常だった
			return true;
		}

		/// <summary>
		/// 指定バイト列が JIS エンコーディングされた文字列かどうかを判定します。
		/// </summary>
		/// <remarks>
		/// JIS に特徴的な、漢字開始制御フラグ(KANJI_IN)が含まれるかどうかで判定します。
		/// </remarks>
		static bool IsJis( byte[] text )
		{
			Debug.Assert( text != null );

			bool isJis = false;
			int pos = 0;

			// ESC コードを検索し、その直後２バイトを読んで KANJI_IN か判定
			pos = Utl.FindFirstOf( text, pos, 0x1B );
			while( pos != -1 )
			{
				isJis = true;

				// 後続シーケンスが ISO-2022-JP として正しいか検証
				if( !Utl.IsValidIso2022jpEscSeq(text, pos) )
				{
					isJis = false; // 不正な ESC シーケンス
					break;
				}

				// 次の ESC コードを検索
				pos = Utl.FindFirstOf( text, pos+1, 0x1B );
			}

			return isJis;
		}

		/// <summary>
		/// 指定バイト列が UTF-8 エンコーディングされた文字列かどうかを判定します。
		/// </summary>
		/// <param name="text">判定対象のバイト列</param>
		/// <param name="withBom">この変数に Byte Order Mark が付いているかどうかが格納されます。</param>
		/// <returns>UTF-8 ならば true</returns>
		static bool IsUtf8( byte[] text, out bool withBom )
		{
			// [UTF-8 bit pattern]
			// 0xxxxxxx                                               (00-7f)
			// 110xxxxx 10xxxxxx                                      (c0-df)(80-bf)
			// 1110xxxx 10xxxxxx 10xxxxxx                             (e0-ef)(80-bf)(80-bf)
			// 11110xxx 10xxxxxx 10xxxxxx 10xxxxxx                    (f0-f7)(80-bf)(80-bf)(80-bf)
			// 111110xx 10xxxxxx 10xxxxxx 10xxxxxx 10xxxxxx           (f8-fb)(80-bf)(80-bf)(80-bf)(80-bf)
			// 1111110x 10xxxxxx 10xxxxxx 10xxxxxx 10xxxxxx 10xxxxxx  (fc-fd)(80-bf)(80-bf)(80-bf)(80-bf)(80-bf)
			Debug.Assert( text != null );

			int offset = 0;
			int followingByteCount;

			// UTF-8 特有の BOM で始まるならば UTF-8 とする
			if( 2 < text.Length
				&& text[0] == 0xef
				&& text[1] == 0xbb
				&& text[2] == 0xbf )
			{
				withBom = true;
				return true;
			}
			withBom = false;
			
			// 先頭から UTF-8 のビットパターンになっているか確認していく
			while( offset < text.Length )
			{
				// 最初のバイトから後続バイトがいくつあるのか取得
				if(      (text[offset]&0x80) == 0x00 )	followingByteCount = 0;
				else if( (text[offset]&0xe0) == 0xc0 )	followingByteCount = 1;
				else if( (text[offset]&0xf0) == 0xe0 )	followingByteCount = 2;
				else if( (text[offset]&0xf8) == 0xf0 )	followingByteCount = 3;
				else if( (text[offset]&0xfc) == 0xf8 )	followingByteCount = 4;
				else if( (text[offset]&0xfe) == 0xfc )	followingByteCount = 5;
				else return false; // invalid for UTF-8
				
				// 後続バイトのビットパターンが 10xxxxxx か判定
				for( int i=1; i<=followingByteCount; i++ )
				{
					if( (text[offset+i]&0xc0) != 0x80 )
					{
						return false; // invalid for UTF-8
					}
				}
				
				// 判定する文字のあるオフセットを移動
				offset += followingByteCount + 1;
			}
			
			// 全バイトのビットパターンが UTF-8 として正常だった
			return true;
		}

		/// <summary>
		/// 指定バイト列が UTF-16 (Little Endian) エンコーディングされた文字列かどうかを判定します。
		/// </summary>
		/// <param name="text">判定対象のバイト列</param>
		/// <returns>UTF-16 (LE) ならば true</returns>
		/// <remarks>
		/// 先頭２バイトに BOM があるかどうかで判断します。
		/// </remarks>
		static bool IsUtf16Le( byte[] text )
		{
			Debug.Assert( text != null );
			if( text.Length <= 1 )
				return false;

			return (text[0] == 0xff && text[1] == 0xfe);
		}

		/// <summary>
		/// 指定バイト列が UTF-16 (Big Endian) エンコーディングされた文字列かどうかをを判定します。
		/// </summary>
		/// <param name="text">判定対象のバイト列</param>
		/// <returns>UTF-16 (BE) ならば true</returns>
		/// <remarks>
		/// 先頭２バイトに BOM があるかどうかで判断します。
		/// </remarks>
		static bool IsUtf16Be( byte[] text )
		{
			Debug.Assert( text != null );
			if( text.Length <= 1 )
				return false;

			return (text[0] == 0xfe && text[1] == 0xff);
		}
		
		/// <summary>
		/// EUC でない可能性を点数にして計算します。
		/// </summary>
		/// <returns>EUC でない可能性を表す点数</returns>
		static int CalcEucImprobabilityPoint( byte[] text )
		{
			Debug.Assert( text != null );

			int point = 0;
			int japaneseCount = 0; // 見つけた日本語文字の数
			
			try
			{
				for( int i=0; i<text.Length; i++ )
				{
					// 漢字の１バイト目か？
					if( 0xa1 <= text[i] && text[i] <= 0xfe )
					{
						// ２バイト目が漢字として不正ならば、減点
						if( text[i+1] < 0xa1 || 0xfe < text[i+1] )
						{
							point += 10;
						}
						// 第二水準の漢字なら、小さく減点 (不頻出)
						else if( 0xd0 <= text[i] )
						{
							point += 1;
							i++;
							japaneseCount++;
						}
						// 第一水準の漢字だった
						else
						{
							i++;
							japaneseCount++;
						}
					}
					// 半角カナの１バイト目か？
					else if( text[i] == 0x8e )
					{
						// 半角カナの２バイト目か？
						if( 0xa1 <= text[i+1] && text[i+1] <= 0xdf )
						{
							point += 1; // 半角カナは嫌われている
							i++;
							japaneseCount++;
						}
						else
						{
							point += 10; // ２バイト目が不正
						}
					}
					// 半角カナ以外で１バイト目が 0x80 以上なら、減点
					else if( 0x80 <= text[i] )
					{
						point += 10;
					}
					
					// 十分な量を確認したため、もう判断を終了
					if( 100 < japaneseCount )
					{
						return point;
					}
				}
			}
			catch( IndexOutOfRangeException )
			{
				// 規格上あるはずの後続バイトが無かった
				point += 30;
			}
			
			return point;
		}

		/// <summary>
		/// Shift_JIS でない可能性を点数にして計算します。
		/// </summary>
		/// <returns>Shift_JIS でない可能性を表す点数</returns>
		static int CalcSjisImprobabilityPoint( byte[] text )
		{
			Debug.Assert( text != null );

			int point = 0;
			int japaneseCount = 0; // 見つけた日本語文字の数
			
			try
			{
				for( int i=0; i<text.Length; i++ )
				{
					// 漢字の１バイト目か？
					if( Utl.IsSjisFirstByte(text[i]) )
					{
						// 漢字の２バイト目として不正なら、減点
						if( Utl.IsSjisSecondByte(text[i+1]) == false )
						{
							point += 10;
						}
						// 第二水準の漢字の漢字なら、小さく原点 (不頻出)
						else if( Utl.IsSjisSecondLevelChar(text[i], text[i+1]) )
						{
							point += 1;
							i++;
							japaneseCount++;
						}
						// 第一水準の漢字
						else
						{
							i++;
							japaneseCount++;
						}
					}
					// 0x80 以上？
					else if( 0x80 <= text[i] )
					{
						// 半角カナなら、小さく減点。でなければ減点。
						if( 0xa1 <= text[i+1] && text[i+1] <= 0xdf )
						{
							point += 1;
							i++;
							japaneseCount++;
						}
						else
						{
							point += 10; // 半角カナでもない：謎の文字
						}
					}
					
					// 十分な量を確認したため、もう判断を終了
					if( 100 < japaneseCount )
					{
						return point;
					}
				}
			}
			catch( IndexOutOfRangeException )
			{
				// 規格上あるはずの後続バイトが無かった
				point += 30;
			}
			
			return point;
		}
		#endregion

		#region Utilities
		class Utl
		{
			public static bool IsValidIso2022jpEscSeq( byte[] bytes, int escIndex )
			{
				// Reference:
				// - ISO-2022-JP-1... RFC 2237
				// - ISO-2022-JP-2... RFC 1544
				byte secondByte;
				byte thirdByte;

				try
				{
					secondByte = bytes[ escIndex + 1 ];
					if( secondByte == '$' )
					{
						thirdByte = bytes[ escIndex + 2 ];
						if( thirdByte == 'A' || thirdByte == 'B'
							|| thirdByte == '@' || thirdByte == '(' )
							return true;
						else
							return false;
					}
					else if( secondByte == '(' )
					{
						thirdByte = bytes[ escIndex + 2 ];
						if( thirdByte == 'B' || thirdByte == 'I' || thirdByte == 'J' )
							return true;
						else
							return false;
					}
					else if( secondByte == '.' )
					{
						thirdByte = bytes[ escIndex + 2 ];
						if( thirdByte == 'A' || thirdByte == 'F' )
							return true;
						else
							return false;
					}
					else if( secondByte == 'N' )
					{
						return true;
					}
					else
					{
						return false;
					}
				}
				catch( IndexOutOfRangeException )
				{
					return false;
				}
			}

			/// <summary>
			/// ESC コードの後続２バイトを判定して
			/// その３バイトが JIS の KANJI_IN（３バイト）かどうかを判定します。
			/// </summary>
			/// <param name="secondByte">ESC コード直後のバイト値</param>
			/// <param name="thirdByte">ESC コードの２バイト後ろのバイト値</param>
			/// <returns>KANJI_IN なら true</returns>
			public static bool IsKanjiIn( byte secondByte, byte thirdByte )
			{
				// JIS-1982 の KANJI_IN は <ESC>$B
				const byte KANJI_IN_1 = 0x24; // $
				const byte KANJI_IN_2 = 0x42; // B
				
				// JIS-1978 の KANJI_IN は <ESC>$@
				const byte KANJI_IN_2_old = 0x40; // @

				if( KANJI_IN_1 == secondByte
					&& KANJI_IN_2 == thirdByte )
				{
					return true; // JIS-1982
				}
				else if( KANJI_IN_1 == secondByte
					&& KANJI_IN_2_old == thirdByte )
				{
					return true; // JIS-1978
				}

				return false;
			}

			/// <summary>
			/// Shift_JIS 漢字の１バイト目である可能性があるか判定します。
			/// </summary>
			/// <returns>可能性があれば true</returns>
			public static bool IsSjisFirstByte( byte ch )
			{
				if( 0x81 <= ch && ch <= 0x9f )
				{
					return true;
				}
				if( 0xe0 <= ch && ch <= 0xfc )
				{
					return true;
				}
				
				return false;
			}
			
			/// <summary>
			/// Shift_JIS 漢字の２バイト目である可能性があるか判定します。
			/// </summary>
			/// <returns>可能性があれば true</returns>
			public static bool IsSjisSecondByte( byte ch )
			{
				if( 0x40 <= ch && ch <= 0x7e )
				{
					return true;
				}
				if( 0x80 <= ch && ch <= 0xfc )
				{
					return true;
				}
				
				return false;
			}
			
			/// <summary>
			/// Shift_JIS の第二水準の漢字か判定します。
			/// </summary>
			/// <returns>第二水準の漢字なら true</returns>
			public static bool IsSjisSecondLevelChar( byte by1, byte by2 )
			{
				if( 0x989f <= (by1 * 256 + by2) )
				{
					return true;
				}
				
				return false;
			}
			
			/// <summary>
			/// 最初に指定バイトが現れる位置を検索します。
			/// </summary>
			public static int FindFirstOf( byte[] array, int startIndex, byte element )
			{
				Debug.Assert( array != null );
				Debug.Assert( 0 <= startIndex );

				for( int i=startIndex; i<array.Length; i++ )
				{
					if( element.Equals(array[i]) )
					{
						return i;
					}
				}

				return -1;
			}
		}
		#endregion

		#region Unit Test
#		if DEBUG
		internal static void Test()
		{
			string[] test_file_names = new string[]{
				"euc.txt", "euc-2.txt",
				"jis.txt", "jis-2.txt",
				"sjis.txt", "sjis-2.txt",
				"utf8.txt", "utf8b.txt",
				"utf16le.txt", "utf16be.txt",
				"empty.txt", "binary.txt"
			};
			string[] expected_results = new string[]{
				"euc-jp", "euc-jp",
				"iso-2022-jp", "iso-2022-jp",
				"shift_jis", "shift_jis",
				Encoding.UTF8.WebName, Encoding.UTF8.WebName,
				Encoding.Unicode.WebName, Encoding.BigEndianUnicode.WebName,
				Encoding.Default.WebName, Encoding.Default.WebName
			};
			Encoding actual;
			bool withBom;
			byte[] fileContent;
			string appDirPath;
			string filePath;

			Console.WriteLine( "[Sgry.EncodingAnalyzer]" );

			// move to assembly's directory
			appDirPath = Path.GetDirectoryName(
				System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName
			);

			// test all test files
			for( int i=0; i<test_file_names.Length; i++ )
			{
				Console.Write( "{0}...\t", test_file_names[i] );
				{
					filePath = Path.Combine( appDirPath, test_file_names[i] );
					actual = EncodingAnalyzer.Analyze( filePath, out withBom, out fileContent );
					if( withBom )
						Console.WriteLine( "{0} (BOM)", actual.WebName );
					else
						Console.WriteLine( "{0}", actual.WebName );
				}
				Debug.Assert( actual.WebName == expected_results[i] );
			}

			// test all test files
			fileContent = new byte[]{ 0x1b, 0x24, 0x42 };
			{
				actual = EncodingAnalyzer.Analyze( fileContent, out withBom );
				Debug.Assert( actual.WebName == "iso-2022-jp" );
				Debug.Assert( withBom == false );
			}
			fileContent = new byte[]{ 0x1b, 0x24 };
			{
				actual = EncodingAnalyzer.Analyze( fileContent, out withBom );
				Debug.Assert( actual.WebName == "utf-8" );
				Debug.Assert( withBom == false );
			}

			Console.WriteLine( "done." );
			Console.WriteLine();
		}
#		endif
		#endregion
	}
}

/*
Version History

[v1.3.0] 2009-03-29
- ライセンスを zlib license に変更
- ドキュメントに大幅加筆
- 単体テストを付属
- 特定条件を満たすバイナリパターンを解析中に例外が発生する問題を修正
- .NET Compact Framework 2.0 でも動作確認を行うように

[v1.2.2] 2008-11-01
- 空のファイルを解析すると例外が発生する問題を修正

[v1.2.1] 2007-01-25
- 長いファイルで前半に ASCII 文字しか無いと UTF-8 でないと誤判定する問題を修正

[v1.2.0] 2007-01-01
- 著作権表示を更新

[v1.2.0] 2006-10-01
- BOM の有無を取得できるように

[v1.1.2] 2006-09-08
- ASCII 文字しか無い場合、UTF-8 ではなく System.Default とするように

[v1.1.1] 2006-09-08
- ファイルパスを指定してエンコーディングを解析すると対象ファイルを開いたままにする問題を修正

[v1.1.0] 2006-09-05
- JIS かどうかを判定中に無限ループになりうる問題を修正
- UTF-8 の判定を BOM に頼らずに行うように
- 全体的に精度と速度を向上

[v1.0.0] 2006-05-17
- 曽田さんの ttPage [http://www2.biglobe.ne.jp/~sota/ttpage.html] の
  エンコーディング判定処理と同じ内容で C# プログラムとして作成。

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
