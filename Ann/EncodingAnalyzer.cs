// file: EncodingAnalyzer.cs
// brief: Japanese charactor encoding analyzer.
// author: YAMAMOTO Suguru
// encoding: UTF-8
// version: 2.0.0
// platform: .NET 2.0 (may work on .NET 1.1)
// create: 2006-05-17 YAMAMOTO Suguru
// update: 2009-10-18 YAMAMOTO Suguru
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
	/// ISO-2022-JP (JIS)、Shift_JIS、EUC-JP、UTF-8、UTF-16 に対応しています。
	/// </summary>
	/// <remarks>
	/// <para>
	/// 日本語の文字エンコーディングを推定する機能をします。
	/// 対応しているエンコーディングは
	/// ISO-2022-JP (JIS)、Shift_JIS、EUC-JP、UTF-8、UTF-16 です。
	/// </para>
	/// </remarks>
	public class EncodingAnalyzer
	{
		#region Fields
		const int LowerLimit			= 50;

		const int BasePoint				= 100;
		const int UnknownCharPoint		= 50;
		const int ControlCodePoint		= 10;
		const int HojoKanjiPoint		= 7;
		const int SecondLevelKanjiPoint	= 6;
		const int HankakuKanaPoint		= 4;
		const int OtherLanguagePoint	= 1;

		const int AsciiIndex	= 0;
		const int UniIndex		= 1;
		const int UniBigIndex	= 2;
		const int Utf8Index		= 3;
		const int JisIndex		= 4;
		const int EucIndex		= 5;
		const int SjisIndex		= 6;
		#endregion

		#region Public interface
		/// <summary>
		/// 日本語文字エンコーディングを推定します。
		/// </summary>
		/// <param name="filePath">推定対象となるファイルのパス</param>
		/// <returns>推定されたエンコーディング。</returns>
		/// <exception cref="System.IO.IOException">ストリームからの読み出し中に I/O エラーが発生しました。</exception>
		/// <exception cref="System.IO.PathTooLongException">指定したパスが長すぎます。</exception>
		/// <exception cref="System.IO.FileNotFoundException">指定したファイルが見つかりませんでした。</exception>
		/// <exception cref="System.IO.DirectoryNotFoundException">指定したパスが無効です。</exception>
		/// <exception cref="System.NotSupportedException">サポートしていないパス形式が指定されました。</exception>
		/// <exception cref="System.UnauthorizedAccessException">指定したファイルへの読み取りアクセスが拒否されました。</exception>
		/// <exception cref="System.ArgumentException">パス文字列として不正な値が指定されました。</exception>
		/// <exception cref="System.ArgumentNullException">引数 filePath が null です。</exception>
		public static Encoding Analyze( string filePath )
		{
			bool withBom, maybeBinary;
			return Analyze( filePath, out withBom, out maybeBinary );
		}

		/// <summary>
		/// 日本語文字エンコーディングを推定します。
		/// </summary>
		/// <param name="filePath">推定対象となるファイルのパス</param>
		/// <param name="withBom">この変数に Byte Order Mark が付いているかどうかが格納されます。</param>
		/// <returns>推定されたエンコーディング。</returns>
		/// <exception cref="System.IO.IOException">ストリームからの読み出し中に I/O エラーが発生しました。</exception>
		/// <exception cref="System.IO.PathTooLongException">指定したパスが長すぎます。</exception>
		/// <exception cref="System.IO.FileNotFoundException">指定したファイルが見つかりませんでした。</exception>
		/// <exception cref="System.IO.DirectoryNotFoundException">指定したパスが無効です。</exception>
		/// <exception cref="System.NotSupportedException">サポートしていないパス形式が指定されました。</exception>
		/// <exception cref="System.UnauthorizedAccessException">指定したファイルへの読み取りアクセスが拒否されました。</exception>
		/// <exception cref="System.ArgumentException">パス文字列として不正な値が指定されました。</exception>
		/// <exception cref="System.ArgumentNullException">引数 filePath が null です。</exception>
		public static Encoding Analyze( string filePath, out bool withBom )
		{
			bool maybeBinary;
			return Analyze( filePath, out withBom, out maybeBinary );
		}

		/// <summary>
		/// 日本語文字エンコーディングを推定します。
		/// </summary>
		/// <param name="filePath">推定対象となるファイルのパス</param>
		/// <param name="withBom">この変数に Byte Order Mark が付いているかどうかが格納されます。</param>
		/// <param name="maybeBinary">ストリームの内容がバイナリである疑いが強い場合 true が設定されます。</param>
		/// <returns>推定されたエンコーディング。失敗すると null。</returns>
		/// <exception cref="System.IO.IOException">ストリームからの読み出し中に I/O エラーが発生しました。</exception>
		/// <exception cref="System.IO.PathTooLongException">指定したパスが長すぎます。</exception>
		/// <exception cref="System.IO.FileNotFoundException">指定したファイルが見つかりませんでした。</exception>
		/// <exception cref="System.IO.DirectoryNotFoundException">指定したパスが無効です。</exception>
		/// <exception cref="System.NotSupportedException">サポートしていないパス形式が指定されました。</exception>
		/// <exception cref="System.UnauthorizedAccessException">指定したファイルへの読み取りアクセスが拒否されました。</exception>
		/// <exception cref="System.ArgumentException">パス文字列として不正な値が指定されました。</exception>
		/// <exception cref="System.ArgumentNullException">引数 filePath が null です。</exception>
		public static Encoding Analyze( string filePath, out bool withBom, out bool maybeBinary )
		{
			FileStream file;
			
			if( filePath == null )
				throw new ArgumentNullException( "filePath" );

			using( file = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite) )
			{
				return Analyze( file, out withBom, out maybeBinary );
			}
		}

		/// <summary>
		/// 日本語文字エンコーディングを推定します。
		/// </summary>
		/// <param name="stream">推定対象となるバイト列を読み出すストリーム</param>
		/// <returns>推定されたエンコーディング。失敗すると null。</returns>
		/// <exception cref="System.ObjectDisposedException">ストリームがすでに閉じられています。</exception>
		/// <exception cref="System.NotSupportedException">読み取りがサポートされないストリームが指定されました。</exception>
		/// <exception cref="System.IO.IOException">ストリームからの読み出し中に I/O エラーが発生しました。</exception>
		/// <exception cref="System.ArgumentNullException">引数 stream が null です。</exception>
		public static Encoding Analyze( Stream stream )
		{
			bool withBom, maybeBinary;
			return Analyze( stream, out withBom, out maybeBinary );
		}

		/// <summary>
		/// 日本語文字エンコーディングを推定します。
		/// </summary>
		/// <param name="stream">推定対象となるバイト列を読み出すストリーム</param>
		/// <param name="withBom">この変数に Byte Order Mark が付いているかどうかが格納されます。</param>
		/// <returns>推定されたエンコーディング。失敗すると null。</returns>
		/// <exception cref="System.ObjectDisposedException">ストリームがすでに閉じられています。</exception>
		/// <exception cref="System.NotSupportedException">読み取りがサポートされないストリームが指定されました。</exception>
		/// <exception cref="System.IO.IOException">ストリームからの読み出し中に I/O エラーが発生しました。</exception>
		/// <exception cref="System.ArgumentNullException">引数 stream が null です。</exception>
		public static Encoding Analyze( Stream stream, out bool withBom )
		{
			bool maybeBinary;
			return Analyze( stream, out withBom, out maybeBinary );
		}

		/// <summary>
		/// 日本語文字エンコーディングを推定します。
		/// </summary>
		/// <param name="stream">推定対象となるバイト列を読み出すストリーム</param>
		/// <param name="withBom">この変数に Byte Order Mark が付いているかどうかが格納されます。</param>
		/// <param name="maybeBinary">ストリームの内容がバイナリである疑いが強い場合 true が設定されます。</param>
		/// <returns>推定されたエンコーディング。失敗すると null。</returns>
		/// <exception cref="System.ObjectDisposedException">ストリームがすでに閉じられています。</exception>
		/// <exception cref="System.NotSupportedException">読み取りがサポートされないストリームが指定されました。</exception>
		/// <exception cref="System.IO.IOException">ストリームからの読み出し中に I/O エラーが発生しました。</exception>
		/// <exception cref="System.ArgumentNullException">引数 stream が null です。</exception>
		public static Encoding Analyze( Stream stream, out bool withBom, out bool maybeBinary )
		{
			byte[] buf;

			if( stream == null )
				throw new ArgumentNullException( "stream" );

			// prepare buffer to receive bytes
			buf = new byte[ stream.Length ];
			
			// read some bytes from the file
			stream.Read( buf, 0, buf.Length );

			// analyze it
			return Analyze( buf, out withBom, out maybeBinary );
		}

		/// <summary>
		/// 日本語文字エンコーディングを推定します。
		/// </summary>
		/// <param name="text">推定対象となるバイト列</param>
		/// <returns>推定されたエンコーディング。失敗すると null。</returns>
		/// <exception cref="ArgumentNullException">引数 text が null です。</exception>
		public static Encoding Analyze( byte[] text )
		{
			bool withBom, maybeBinary;
			return Analyze( text, out withBom, out maybeBinary );
		}

		/// <summary>
		/// 日本語文字エンコーディングを推定します。
		/// </summary>
		/// <param name="text">推定対象となるバイト列</param>
		/// <param name="withBom">この変数に Byte Order Mark が付いているかどうかが格納されます。</param>
		/// <returns>推定されたエンコーディング。失敗すると null。</returns>
		/// <exception cref="System.ArgumentNullException">引数 text が null です。</exception>
		public static Encoding Analyze( byte[] text, out bool withBom )
		{
			bool maybeBinary;
			return Analyze( text, out withBom, out maybeBinary );
		}

		/// <summary>
		/// 日本語文字エンコーディングを推定します。
		/// </summary>
		/// <param name="text">推定対象となるバイト列</param>
		/// <param name="withBom">この変数に Byte Order Mark が付いているかどうかが格納されます。</param>
		/// <param name="maybeBinary">ストリームの内容がバイナリである疑いが強い場合 true が設定されます。</param>
		/// <returns>推定されたエンコーディング。失敗すると null。</returns>
		/// <exception cref="System.ArgumentNullException">引数 text が null です。</exception>
		public static Encoding Analyze( byte[] text, out bool withBom, out bool maybeBinary )
		{
			if( text == null )
				throw new ArgumentNullException( "text" );

			int[] points = new int[7];
			int[] indexes = new int[7];
			int candidateIndex = -1;
			int nextCandidateIndex = -1;

			// らしさポイントを初期化
			for( int i=0; i<points.Length; i++ )
			{
				points[i] = BasePoint;
				indexes[i] = 0;
			}

			try
			{
				// BOM コードが付いていればすぐ文字コードを確定
				if( 1 < text.Length && text[0] == 0xfe && text[1] == 0xff )
				{
					withBom = true;
					maybeBinary = false;
					return Encoding.BigEndianUnicode;
				}
				if( 1 < text.Length && text[0] == 0xff && text[1] == 0xfe )
				{
					withBom = true;
					maybeBinary = false;
					return Encoding.Unicode;
				}
				if( 2 < text.Length && text[0] == 0xef && text[1] == 0xbb && text[2] == 0xbf )
				{
					withBom = true;
					maybeBinary = false;
					return Encoding.UTF8;
				}

				// 全バイトシーケンスを解析
				for(;;)
				{
					// ASCII らしさを計算
					if( 0 < points[AsciiIndex] )
					{
						UpdateAsciiPoint( text, ref indexes[AsciiIndex], ref points[AsciiIndex] );
					}

					// UTF-16 らしさを計算
					if( 0 < points[UniIndex] )
					{
						UpdateUniPoint( text, ref indexes[UniIndex], ref points[UniIndex] );
					}

					// UTF-16 (BigEndian) らしさを計算
					if( 0 < points[UniBigIndex] )
					{
						UpdateUniBigPoint( text, ref indexes[UniBigIndex], ref points[UniBigIndex] );
					}

					// UTF-8 らしさを計算
					if( 0 < points[Utf8Index] )
					{
						UpdateUtf8Point( text, ref indexes[Utf8Index], ref points[Utf8Index] );
					}

					// ISO-2022-JP らしさを計算
					if( 0 < points[JisIndex] )
					{
						UpdateJisPoint( text, ref indexes[JisIndex], ref points[JisIndex] );
					}

					// EUC-JP らしさを計算
					if( 0 < points[EucIndex] )
					{
						UpdateEucPoint( text, ref indexes[EucIndex], ref points[EucIndex] );
					}

					// Shift_JIS らしさを計算
					if( 0 < points[SjisIndex] )
					{
						UpdateSjisPoint( text, ref indexes[SjisIndex], ref points[SjisIndex] );
					}

#					if false
					{
						Console.Write("<{0,4}@{1,-4}", points[0], indexes[0]);
						for( int i=1; i<points.Length; i++ )
						{
							Console.Write( ", {0,4}@{1,-4}", points[i], indexes[i] );
						}
						Console.WriteLine(">");
					}
#					endif

					// らしさポイントを比較
					StatPoints( points, out candidateIndex, out nextCandidateIndex );
					if( points[nextCandidateIndex] <= LowerLimit )
					{
						break;
					}
				}
			}
			catch( IndexOutOfRangeException )
			{}

			// 結果を返す
			if( candidateIndex < 0 )
			{
				StatPoints( points, out candidateIndex, out nextCandidateIndex );
			}
			maybeBinary = (points[candidateIndex] < LowerLimit);
			withBom = false;
			return EncodingFromIndex( candidateIndex );
		}
		#endregion

		#region Analysis Logic
		/// <summary>
		/// ASCII 文字列らしさポイントを指定位置のデータから更新します。
		/// </summary>
		static void UpdateAsciiPoint( byte[] text, ref int index, ref Int32 point )
		{
			Debug.Assert( text != null );
			Debug.Assert( 0 < point );

			byte by;

			// ASCII コードの範囲内か確認
			by = text[ index ];
			index++;
			if( 0x7f < by )
			{
				point = 0;
				index++;
				return;
			}

			// ASCII コードであれば制御コードならばポイントを下げる
			if( Utl.IsControlCode(by) )
			{
				point -= ControlCodePoint;
			}
		}

		/// <summary>
		/// Shift_JIS 文字列らしさポイントを指定位置のデータから更新します。
		/// </summary>
		static void UpdateSjisPoint( byte[] text, ref int index, ref Int32 point )
		{
			Debug.Assert( text != null );
			Debug.Assert( 0 <= index );
			Debug.Assert( 0 < point );

			byte firstByte, secondByte;

			// １バイト目を取得
			firstByte = text[ index ];
			index++;
			if( Utl.IsControlCode(firstByte) )
			{
				point -= ControlCodePoint; // 制御文字
			}
			else if( firstByte <= 0x7e )
			{
				; // ASCII 文字
			}
			else if( firstByte <= 0x80 )
			{
				point -= UnknownCharPoint; // 不正値
			}
			else if( Utl.IsSjisKanjiFirstByte(firstByte) )
			{
				// 後続が漢字の２バイト目かどうか判定
				secondByte = text[ index ];
				index++;
				if( Utl.IsSjisKanjiSecondByte(secondByte) )
				{
					//--- 漢字 ---
					// 第二水準の漢字ならポイントを下げる
					if( Utl.IsSjisSecondLevelChar(firstByte, secondByte) )
					{
						point -= SecondLevelKanjiPoint;
					}
					return;
				}
				else
				{
					point -= UnknownCharPoint; // 不正値
				}
			}
			else if( firstByte <= 0xa0 )
			{
				point -= UnknownCharPoint; // 不正値
			}
			else if( firstByte <= 0xdf )
			{
				point -= HankakuKanaPoint; // 半角カナ
			}
			else if( firstByte <= 0xff )
			{
				point -= UnknownCharPoint; // 不正値
			}
		}

		/// <summary>
		/// EUC-JP 文字列らしさポイントを指定位置のデータから更新します。
		/// </summary>
		static void UpdateEucPoint( byte[] text, ref int index, ref Int32 point )
		{
			Debug.Assert( text != null );
			Debug.Assert( 0 <= index );
			Debug.Assert( 0 < point );

			byte firstByte, secondByte, thirdByte;

			// １バイト目を取得
			firstByte = text[ index ];
			index++;
			if( Utl.IsControlCode(firstByte) )
			{
				point -= ControlCodePoint; // 制御コード
			}
			else if( firstByte <= 0x7e )
			{
				; // ASCII文字
			}
			else if( firstByte <= 0x8d )
			{
				point -= UnknownCharPoint; // 不明な文字
			}
			else if( firstByte <= 0x8e )
			{
				// 半角カナかどうか確認
				secondByte = text[ index ];
				index++;
				if( 0xa1 <= secondByte && secondByte <= 0xdf )
				{
					point -= HankakuKanaPoint; // 半角カナ
				}
				else
				{
					point -= UnknownCharPoint; // 不明な文字
				}
			}
			else if( firstByte <= 0x8f )
			{
				// 補助漢字かどうか確認
				secondByte = text[ index ];
				index++;
				if( 0xa1 <= secondByte && secondByte <= 0xfe )
				{
					thirdByte = text[ index ];
					index++;
					if( 0xa1 <= thirdByte && thirdByte <= 0xfe )
					{
						point -= HojoKanjiPoint; // 補助漢字
					}
					else
					{
						point -= UnknownCharPoint; // 不明な文字
					}
				}
				else
				{
					point -= UnknownCharPoint; // 不明な文字
				}
			}
			else if( firstByte <= 0xa0 )
			{
				point -= UnknownCharPoint; // 不明な文字
			}
			else if( firstByte <= 0xa8 )
			{
				// 第一水準の漢字か確認
				secondByte = text[ index ];
				index++;
				if( 0xa1 <= secondByte && secondByte <= 0xfe )
				{
					; // 第一水準の漢字
				}
				else
				{
					point -= UnknownCharPoint; // 不明な文字
				}
			}
			else if( firstByte <= 0xfe )
			{
				// 第一水準でない漢字か確認
				secondByte = text[ index ];
				index++;
				if( 0xa1 <= secondByte && secondByte <= 0xfe )
				{
					; // 第一水準でない漢字
				}
				else
				{
					point -= UnknownCharPoint; // 不明な文字
				}
			}
			else if( firstByte <= 0xff )
			{
				point = 0; // 不明な文字・・・というよりバイナリ？
			}
		}

		/// <summary>
		/// JIS 文字列らしさポイントを指定位置のデータから更新します。
		/// </summary>
		static void UpdateJisPoint( byte[] text, ref int index, ref Int32 point )
		{
			Debug.Assert( text != null );
			Debug.Assert( 0 <= index );
			Debug.Assert( 0 < point );

			byte firstByte, secondByte, thirdByte;

			firstByte = text[ index ];
			index++;
			if( firstByte == 0x1b )
			{
				try
				{
					// エスケープシーケンスを解釈
					secondByte = text[ index ];
					index++;
					if( 0x20 <= secondByte && secondByte <= 0x2f )
					{
						thirdByte = text[ index ];
						index++;
						if( 0x30 <= thirdByte && thirdByte <= 0x7e )
						{
							; // 正しいエスケープシーケンス
						}
						else
						{
							point = 0; // まったくの不正値
						}
					}
					else
					{
						point = 0; // まったくの不正値
					}
				}
				catch( IndexOutOfRangeException )
				{
					point -= UnknownCharPoint;
				}
			}
			else if( firstByte < 0x20 )
			{
				if( firstByte != 0x09 && firstByte != 0x0a && firstByte != 0x0d )
				{
					point -= ControlCodePoint; // 制御コード
				}
			}
			else if( 0xa1 <= firstByte && firstByte <= 0xdf )
			{
				point -= HankakuKanaPoint; // 半角カナコード (8-bit JIS)
			}
			else if( 0x7f < firstByte )
			{
				point -= UnknownCharPoint; // 不正値
			}
		}

		/// <summary>
		/// UTF-8 文字列らしさポイントを指定位置のデータから更新します。
		/// </summary>
		static void UpdateUtf8Point( byte[] text, ref int index, ref Int32 point )
		{
			// [UTF-8 bit pattern]
			// 0xxxxxxx                                               (00-7f)
			// 110yyyxx 10xxxxxx                                      (c0-df)(80-bf)
			// 1110yyyy 10yyyyxx 10xxxxxx                             (e0-ef)(80-bf)(80-bf)
			// 11110zzz 10zzyyyy 10yyyyxx 10xxxxxx                    (f0-f7)(80-bf)(80-bf)(80-bf)
			// 111110ss 10zzzzzz 10zzyyyy 10yyyyxx 10xxxxxx           (f8-fb)(80-bf)(80-bf)(80-bf)(80-bf)
			// 1111110s 10ssssss 10zzzzzz 10zzyyyy 10yyyyxx 10xxxxxx  (fc-fd)(80-bf)(80-bf)(80-bf)(80-bf)(80-bf)
			Debug.Assert( text != null );
			Debug.Assert( 0 <= index );
			Debug.Assert( 0 < point );

			byte firstByte;
			byte by;
			char firstCh, secondCh;

			// 何バイトの文字か判定
			firstByte = text[ index ];
			index++;
			if( (firstByte & 0x80) == 0x00 )
			{
				// 1-byte character
				firstCh = (char)( firstByte & 0x7f );
				secondCh = '\x0000';
			}
			else if( (firstByte & 0xe0) == 0xc0 )
			{
				// 2-byte character
				firstCh = (char)( (firstByte & 0x1f) << 6 );
				secondCh = '\x0000';

				by = text[ index ];
				index++;
				firstCh |= (char)( by & 0x7f );
			}
			else if( (firstByte & 0xf0) == 0xe0 )
			{
				// 3-byte character
				firstCh = (char)( (firstByte & 0x0f) << 12 );
				secondCh = '\x0000';

				by = text[ index ];
				index++;
				firstCh |= (char)( (by & 0x7f) << 6 );

				by = text[ index ];
				index++;
				firstCh |= (char)( by & 0x7f );
			}
			else if( (firstByte & 0xf8) == 0xf0 )
			{
				// 4-byte character
				firstCh = '\x0000';
				secondCh = (char)( (firstByte & 0x07) << 2 );

				by = text[ index ];
				index++;
				secondCh |= (char)( (by & 0x30) >> 4 );
				firstCh |= (char)( (by & 0x0f) << 12 );

				by = text[ index ];
				index++;
				firstCh |= (char)( (by & 0x3f) << 6 );

				by = text[ index ];
				index++;
				firstCh |= (char)( by & 0x7f );
			}
			else if( (firstByte & 0xfc) == 0xf8 )
			{
				// 5-byte character
				point -= UnknownCharPoint; // 不正なシーケンス
				return;
			}
			else if( (firstByte & 0xfe) == 0xfc )
			{
				// 6-byte character
				point -= UnknownCharPoint; // 不正なシーケンス
				return;
			}
			else
			{
				point = 0; // まったく不正なシーケンス
				return;
			}

			// 文字を検証
			CalcUnicodePoint( firstCh, secondCh, ref point );
		}

		/// <summary>
		/// Unicode 文字列らしさポイントを指定位置のデータから更新します。
		/// </summary>
		static void UpdateUniPoint( byte[] text, ref int index, ref Int32 point )
		{
			Debug.Assert( text != null );
			Debug.Assert( 0 < point );

			byte firstByte, secondByte;
			char firstCh, secondCh;

			// １文字目を取得
			firstByte = text[ index++ ];
			secondByte = text[ index++ ];
			firstCh = (char)( firstByte | (secondByte << 8) );

			// ２文字目を取得
			secondCh = '\x0000';
			if( 0xd800 <= firstCh && firstCh <= 0xdbff )
			{
				try
				{
					firstByte = text[ index++ ];
					secondByte = text[ index++ ];
					secondCh = (char)( firstByte | (secondByte << 8) );
				}
				catch( IndexOutOfRangeException )
				{}
			}

			// らしさポイントを計算
			CalcUnicodePoint( firstCh, secondCh, ref point );
		}

		/// <summary>
		/// Unicode (big endian) 文字列らしさポイントを指定位置のデータから更新します。
		/// </summary>
		static void UpdateUniBigPoint( byte[] text, ref int index, ref Int32 point )
		{
			Debug.Assert( text != null );
			Debug.Assert( 0 <= index );
			Debug.Assert( 0 < point );

			byte firstByte, secondByte;
			char firstCh, secondCh;

			// １文字目を取得
			firstByte = text[ index++ ];
			secondByte = text[ index++ ];
			firstCh = (char)( (firstByte << 8) | secondByte );

			// ２文字目を取得
			secondCh = '\x0000';
			if( 0xd800 <= firstCh && firstCh <= 0xdbff )
			{
				try
				{
					firstByte = text[ index++ ];
					secondByte = text[ index++ ];
					secondCh = (char)( firstByte | (secondByte << 8) );
				}
				catch( IndexOutOfRangeException )
				{}
			}

			// らしさポイントを計算
			CalcUnicodePoint( firstCh, secondCh, ref point );
		}

		/// <summary>
		/// Unicode 文字列らしさポイントを計算します。
		/// </summary>
		static void CalcUnicodePoint( char firstCh, char secondCh, ref int point )
		{
			char[] uniChars = new char[2];

			if( firstCh <= 0x001f || firstCh == 0x007f )
			{
				if( firstCh != 0x09 && firstCh != 0x0a && firstCh != 0x0d )
				{
					point -= ControlCodePoint; // 制御コード
				}
			}
			else if( firstCh <= 0x007e )
			{
				; // ASCII文字
			}
			else if( 0x2150 <= firstCh && firstCh <= 0x22ff )
			{
				; // 記号類
			}
			else if( 0x3000 <= firstCh && firstCh <= 0x30ff )
			{
				; // ひらがな、カタカナ
			}
			else if( 0x4e00 <= firstCh && firstCh <= 0x9fff )
			{
				; // CJK統合漢字
			}
			else if( 0xff61 <= firstCh && firstCh <= 0xff9f )
			{
				point -= HankakuKanaPoint; // 半角カナ
			}
			else if( (0x07c0 <= firstCh && firstCh <= 0x08ff)
				|| (0x0fd0 <= firstCh && firstCh <= 0x10cf)
				|| (0x1678 <= firstCh && firstCh <= 0x177f)
				|| (0x18b0 <= firstCh && firstCh <= 0x19df)
				|| (0x1a00 <= firstCh && firstCh <= 0x1dbf)
				|| (0x20c0 <= firstCh && firstCh <= 0x2d7f)
				|| (0x2de0 <= firstCh && firstCh <= 0x2e7f)
				|| (0xa4c8 <= firstCh && firstCh <= 0xabff)
				|| (0xfa70 <= firstCh && firstCh <= 0xfaff)
				|| (0xfb50 <= firstCh && firstCh <= 0xfe0f)
				|| (0xfff0 <= firstCh && firstCh <= 0xfffd) )
			{
				// メイリオフォントにグリフが収録されていない範囲は
				// 日本語としてまず使われないと判断
				point -= UnknownCharPoint;
			}
			else
			{
				// 非日本語文字や記号類
				point -= SecondLevelKanjiPoint;
			}
		}
		#endregion

		#region Utilities
		static Encoding EncodingFromIndex( int index )
		{
			if( index == AsciiIndex )
				return Encoding.ASCII;
			if( index == SjisIndex )
				return Encoding.GetEncoding( "shift_jis" );
			if( index == EucIndex )
				return Encoding.GetEncoding( "euc-jp" );
			if( index == JisIndex )
				return Encoding.GetEncoding( "iso-2022-jp" );
			if( index == Utf8Index )
				return Encoding.UTF8;
			if( index == UniIndex )
				return Encoding.Unicode;
			if( index == UniBigIndex )
				return Encoding.BigEndianUnicode;
			
			return Encoding.Default;
		}

		static void StatPoints( int[] points, out int biggestIndex, out int nextBiggestIndex )
		{
			biggestIndex = nextBiggestIndex = 0;
			for( int i=1; i<points.Length; i++ )
			{
				if( points[biggestIndex] <= points[i] )
				{
					nextBiggestIndex = biggestIndex;
					biggestIndex = i;
				}
				else if( points[nextBiggestIndex] <= points[i]
					&& points[i] < points[biggestIndex] )
				{
					nextBiggestIndex = i;
				}
			}
		}

		class Utl
		{
			/// <summary>
			/// Shift_JIS 漢字の１バイト目である可能性があるか判定します。
			/// </summary>
			public static bool IsSjisKanjiFirstByte( byte ch )
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
			public static bool IsSjisKanjiSecondByte( byte ch )
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
			public static bool IsSjisSecondLevelChar( byte firstByte, byte secondByte )
			{
				if( 0x989f <= ((firstByte << 8) | secondByte) )
				{
					return true;
				}
				
				return false;
			}

			/// <summary>
			/// ASCII 制御コードかどうか判定します。
			/// </summary>
			public static bool IsControlCode( byte by )
			{
				if( by <= 0x1f || by == 0x7f )
				{
					if( by != 0x09 && by != 0x0a && by != 0x0d )
						return true;
					else
						return false;
				}
				else
				{
					return false;
				}
			}
		}
		#endregion

		#region Unit Test
#		if false
		internal static void Test()
		{
			System.Diagnostics.Stopwatch stopwatch;
			long t = 0;
			Encoding actual;
			bool withBom;
			bool maybeBinary;
			byte[] fileContent;
			string exeDir;
			TextWriter output = TextWriter.Null;
			//DEBUG//output = Console.Out;

			Console.WriteLine( "[Sgry.EncodingAnalyzer]" );

			// execute a task to cache program
			EncodingAnalyzer.Analyze( new byte[]{0xe6, 0x96, 0x87, 0xe5, 0xad, 0x97, 0xe5, 0x8c, 0x96, 0xe3, 0x81, 0x91}, out withBom, out maybeBinary );

			// test special binary sequences
			t = 0;
			fileContent = new byte[]{ 0xff, 0xfe };
			{
				stopwatch = System.Diagnostics.Stopwatch.StartNew();
				{
					actual = EncodingAnalyzer.Analyze( fileContent, out withBom, out maybeBinary );
				}
				t += stopwatch.ElapsedTicks;
				Debug.Assert( actual == Encoding.Unicode );
				Debug.Assert( withBom == true );
			}
			fileContent = new byte[]{ 0xfe, 0xff };
			{
				stopwatch = System.Diagnostics.Stopwatch.StartNew();
				{
					actual = EncodingAnalyzer.Analyze( fileContent, out withBom, out maybeBinary );
				}
				t += stopwatch.ElapsedTicks;
				Debug.Assert( actual == Encoding.BigEndianUnicode );
				Debug.Assert( withBom == true );
			}
			fileContent = new byte[]{ 0xef, 0xbb, 0xbf };
			{
				stopwatch = System.Diagnostics.Stopwatch.StartNew();
				{
					actual = EncodingAnalyzer.Analyze( fileContent, out withBom, out maybeBinary );
				}
				t += stopwatch.ElapsedTicks;
				Debug.Assert( actual == Encoding.UTF8 );
				Debug.Assert( withBom == true );
			}
			fileContent = new byte[]{ 0xe6, 0x96, 0x87, 0xe5, 0xad, 0x97, 0xe5, 0x8c, 0x96, 0xe3, 0x81, 0x91 };
			{
				stopwatch = System.Diagnostics.Stopwatch.StartNew();
				{
					actual = EncodingAnalyzer.Analyze( fileContent, out withBom, out maybeBinary );
				}
				t += stopwatch.ElapsedTicks;
				Debug.Assert( actual == Encoding.UTF8 );
				Debug.Assert( withBom == false );
			}
			fileContent = new byte[]{ 0x1b, 0x24, 0x42 };
			{
				stopwatch = System.Diagnostics.Stopwatch.StartNew();
				{
					actual = EncodingAnalyzer.Analyze( fileContent, out withBom, out maybeBinary );
				}
				t += stopwatch.ElapsedTicks;
				Debug.Assert( actual == Encoding.GetEncoding("iso-2022-jp") );
				Debug.Assert( withBom == false );
			}
			fileContent = new byte[]{  };
			{
				stopwatch = System.Diagnostics.Stopwatch.StartNew();
				{
					actual = EncodingAnalyzer.Analyze( fileContent, out withBom, out maybeBinary );
				}
				t += stopwatch.ElapsedTicks;
				Debug.Assert( actual == Encoding.Default );
				Debug.Assert( withBom == false );
			}

			// get directory where this assembly exists
			exeDir = Path.GetDirectoryName(
				System.Reflection.Assembly.GetExecutingAssembly().Location
			);

			// test all test files
			foreach( string filePath in Directory.GetFiles(exeDir, "*.txt", SearchOption.AllDirectories) )
			{
				StringComparison ignoreCase = StringComparison.InvariantCultureIgnoreCase;
				string fileName = Path.GetFileName( filePath );
				output.Write( "{0,-36}... ", filePath.Substring(exeDir.Length+1) );
				{
					stopwatch = System.Diagnostics.Stopwatch.StartNew();
					{
						actual = EncodingAnalyzer.Analyze( filePath, out withBom, out maybeBinary );
					}
					t += stopwatch.ElapsedTicks;
					if( maybeBinary )
						output.Write( "binary ({0,-17})", actual.WebName+"(BOM)" );
					else if( withBom )
						output.Write( "{0,-17}", actual.WebName+"(BOM)" );
					else
						output.Write( "{0,-17}", actual.WebName );
				}
				if( (maybeBinary && fileName.EndsWith("binary.txt", ignoreCase) == false)
					|| (maybeBinary == false && fileName.EndsWith(actual.WebName+".txt", ignoreCase) == false) )
				{
					string msg = String.Format( "FAILED ({0} is not {1})", fileName, actual.WebName );
					//DEBUG//Debug.Fail( msg );
					output.Write( msg );
				}
				output.WriteLine( "" );
			}

			Console.WriteLine( "done. ({0} ms)", t / (System.Diagnostics.Stopwatch.Frequency / 1000) );
			Console.WriteLine();
		}
#		endif
		#endregion
	}
}

/*
Version History

[v2.0.0] 2009-10-17
- 独自ロジックで一から再実装
- 処理を高速化
- BOM 無しの UTF-16 でも解析可能に

[v1.3.0] 2009-03-15
- ライセンスを zlib license に変更
- ドキュメントに大幅加筆
- 単体テストを付属
- 特定条件を満たすバイナリパターンを解析中に例外が発生する問題を修正

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
