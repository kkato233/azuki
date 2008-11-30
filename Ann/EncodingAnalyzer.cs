// file: EncodingAnalyzer.cs
// brief: Japanese charactor encoding analyzer class
// author: SGRY (YAMAMOTO Suguru)
// encoding: UTF-8
// version: 1.2.2
// update: 2008-11-01 (SGRY)
// license: zlib License (see END of this file)
//=========================================================
using IndexOutOfRangeException  = System.IndexOutOfRangeException;
using Encoding                  = System.Text.Encoding;
using FileStream				= System.IO.FileStream;
using File						= System.IO.File;
using FileMode					= System.IO.FileMode;

namespace Sgry
{
	/// <summary>
	/// 日本語の文字エンコーディングを推定するクラス。
	/// 対応エンコーディングは(俗称で) JIS、S-JIS、EUC-JP、UTF-8、UTF-16。
	/// UTF-16 の判定は Byte Order Mark に依存。
	/// </summary>
	/// <example>
	///	// エンコーディングを自動推定して指定ファイルの内容を読み出す関数
	/// static string MyReadFile( string filePath )
	/// {
	///		Encoding encoding = EncodingAnalyzer.Analyze( filePath );
	///		return new String( encoding.GetChars(content) );
	/// }
	/// </example>
	public class EncodingAnalyzer
	{
		/// <summary>
		/// 日本語文字エンコーディングを推定
		/// </summary>
		/// <param name="filePath">推定対象となるファイルのパス</param>
		/// <returns>推定されたエンコーディング。失敗すると null</returns>
		public static Encoding Analyze( string filePath )
		{
			bool dummy;
			return Analyze( filePath, out dummy );
		}

		/// <summary>
		/// 日本語文字エンコーディングを推定
		/// </summary>
		/// <param name="filePath">推定対象となるファイルのパス</param>
		/// <param name="withBom">この引数に Byte Order Mark が付いているかどうかが格納されます。</param>
		/// <returns>推定されたエンコーディング。失敗すると null</returns>
		public static Encoding Analyze( string filePath, out bool withBom )
		{
			FileStream	file = null;
			Encoding	suggestedEncoding;
			
			try
			{
				file = File.Open( filePath, FileMode.Open );
				byte[] content = new byte[ file.Length ];
				
				// read all bytes
				int by = file.ReadByte();
				for( int i=0; (i<file.Length) && (by != -1) ; i++ )
				{
					content[i] = (byte)by;
					by = file.ReadByte();
				}
				
				suggestedEncoding = EncodingAnalyzer.Analyze( content, out withBom );
			}
			catch
			{
				withBom = false;
				suggestedEncoding = null;
			}

			if( file != null )
			{
				file.Close();
			}
			return suggestedEncoding;
		}
		
		/// <summary>
		/// 日本語文字エンコーディングを推定
		/// </summary>
		/// <param name="text">推定対象となるバイト列</param>
		/// <returns>推定されたエンコーディング。失敗すると null</returns>
		public static Encoding Analyze( byte[] text )
		{
			bool dummy;
			return Analyze( text, out dummy );
		}

		/// <summary>
		/// 日本語文字エンコーディングを推定
		/// </summary>
		/// <param name="text">推定対象となるバイト列</param>
		/// <param name="withBom">この引数に Byte Order Mark が付いているかどうかが格納されます。</param>
		/// <returns>推定されたエンコーディング。失敗すると null</returns>
		public static Encoding Analyze( byte[] text, out bool withBom )
		{
			int eucPoint, sjisPoint;
			
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
				// but possibly it is ASCII. check about it.
				if( IsAscii(text) )
				{
					withBom = false;
					return Encoding.Default; // ASCII なら標準エンコーディングに
				}

				return Encoding.UTF8;
			}

			// EUC-JP? or Shift_JIS?
			// calculate improbability point for each encoding
			eucPoint = CalcEucImprobabilityPoint( text );
			sjisPoint = CalcSjisImprobabilityPoint( text );
			if( eucPoint < sjisPoint )
			{
				withBom = false;
				return Encoding.GetEncoding( "euc-jp" );
			}
			else if( sjisPoint < eucPoint )
			{
				withBom = false;
				return Encoding.GetEncoding( "shift_jis" );
			}

			withBom = false;
			return null;
		}

		static bool IsAscii( byte[] text )
		{
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
		/// 指定バイト列が JIS エンコーディングされた文字列かどうか判定
		/// </summary>
		/// <param name="text">判定対象のバイト列</param>
		/// <returns>JIS ならば true</returns>
		/// <remarks>
		/// JISに特徴的な、漢字開始制御フラグが含まれるかどうかで判定
		/// 漢字開始制御フラグはコメント中で KANJI_IN と表記。
		/// </remarks>
		static bool IsJis( byte[] text )
		{
			int pos = 0;

			// ESC コードを検索し、その直後２バイトを読んでKANJI_INか判定
			pos = Utl.FindFirstOf( text, pos, 0x1B );
			while( pos != -1 )
			{
				// この ESC は KANJI_IN の ESC か？
				if( Utl.IsKanjiIn(text[pos+1], text[pos+2]) )
				{
					return true; // KANJI_IN 発見
				}

				// 次の ESC コードを検索
				pos = Utl.FindFirstOf( text, pos+1, 0x1B );
			}

			return false;
		}

		/// <summary>
		/// 指定バイト列が UTF-8 エンコーディングされた文字列かどうか判定
		/// </summary>
		/// <param name="text">判定対象のバイト列</param>
		/// <param name="withBom">この引数に Byte Order Mark が付いているかどうかが格納されます。</param>
		/// <returns>UTF-8 ならば true</returns>
		// [UTF-8 bit pattern]
		// 0xxxxxxx                                               (00-7f)
		// 110xxxxx 10xxxxxx                                      (c0-df)(80-bf)
		// 1110xxxx 10xxxxxx 10xxxxxx                             (e0-ef)(80-bf)(80-bf)
		// 11110xxx 10xxxxxx 10xxxxxx 10xxxxxx                    (f0-f7)(80-bf)(80-bf)(80-bf)
		// 111110xx 10xxxxxx 10xxxxxx 10xxxxxx 10xxxxxx           (f8-fb)(80-bf)(80-bf)(80-bf)(80-bf)
		// 1111110x 10xxxxxx 10xxxxxx 10xxxxxx 10xxxxxx 10xxxxxx  (fc-fd)(80-bf)(80-bf)(80-bf)(80-bf)(80-bf)
		static bool IsUtf8( byte[] text, out bool withBom )
		{
			int offset = 0;
			int followingByteCount;
			
			// UTF-8 特有の BOM で始まるならば UTF-8 とする
			if( 2 < text.Length
				&& text[0] == 0xEF
				&& text[1] == 0xBB
				&& text[2] == 0xBF )
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
		/// 指定バイト列が UTF-16 (Little Endian) エンコーディングされた文字列かどうか判定
		/// </summary>
		/// <param name="text">判定対象のバイト列</param>
		/// <returns>UTF-16 (LE) ならば true</returns>
		/// <remarks>BOMでのみ判断</remarks>
		static bool IsUtf16Le( byte[] text )
		{
			return (1 < text.Length && text[0] == 0xFF && text[1] == 0xFE);
		}

		/// <summary>
		/// 指定バイト列が UTF-16 (Big Endian) エンコーディングされた文字列かどうか判定
		/// </summary>
		/// <param name="text">判定対象のバイト列</param>
		/// <returns>UTF-16 (BE) ならば true</returns>
		/// <remarks>BOMでのみ判断</remarks>
		static bool IsUtf16Be( byte[] text )
		{
			return (1 < text.Length && text[0] == 0xFE && text[1] == 0xFF);
		}
		
		/// <summary>
		/// EUC でない可能性を点数にして計算
		/// </summary>
		/// <returns>EUC でない可能性を表す点数</returns>
		static int CalcEucImprobabilityPoint( byte[] text )
		{
			int point = 0;
			int japaneseCount = 0; // 見つけた日本語文字の数
			
			try
			{
				for( int i=0; i<text.Length; i++ )
				{
					// 漢字の１バイト目か？
					if( 0xA1 <= text[i] && text[i] <= 0xfe )
					{
						// ２バイト目が漢字として不正ならば、減点
						if( text[i+1] < 0xA1 || 0xfe < text[i+1] )
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
		/// Shift_JIS でない可能性を点数にして計算
		/// </summary>
		/// <returns>Shift_JIS でない可能性を表す点数</returns>
		static int CalcSjisImprobabilityPoint( byte[] text )
		{
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

		class Utl
		{
			/// <summary>
			/// KANJI_IN（３バイト）かどうか判定。
			/// バイト列中に現れたESCコード(0x1B)直後
			/// ２バイトの値をみている。
			/// </summary>
			/// <param name="secondByte">ESCコード直後のバイト値</param>
			/// <param name="thirdByte">ESCコードの２バイト後ろのバイト値</param>
			/// <returns>KANJI_INならtrue</returns>
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
			/// Shift_JIS 漢字の１バイト目である可能性があるか判定
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
			/// Shift_JIS 漢字の２バイト目である可能性があるか判定
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
			/// Shift_JIS の第二水準の漢字か判定
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
			/// array中で、startPos以降で、最初にelementが現れるインデックスを検索
			/// </summary>
			public static int FindFirstOf( byte[] array, int startPos, byte element )
			{
				for( int i=startPos; i<array.Length; i++ )
				{
					if( element.Equals(array[i]) )
					{
						return i;
					}
				}

				return -1;
			}
		}
	}
}

/*
Version History

[v1.2.2] 2008-11-01
・空のファイルを解析すると例外が発生する問題を修正
・ライセンスを zlib license に変更

[v1.2.1] 2007-01-25
・長いファイルで前半に ASCII 文字しか無いと UTF-8 でないと誤判定する問題を修正

[v1.2.0] 2007-01-01
・著作権表示を更新

[v1.2.0] 2006-10-01
・BOM の有無を取得できるように

[v1.1.2] 2006-09-08
・ASCII 文字しか無い場合、UTF-8 ではなく System.Default とするように

[v1.1.1] 2006-09-08
・ファイルパスを指定してエンコーディングを解析すると対象ファイルを開いたままにする問題を修正

[v1.1.0] 2006-09-05
・JIS かどうかを判定中に無限ループになりうる問題を修正
・UTF-8 の判定を BOM に頼らずに行うように
・全体的に精度と速度を向上

[v1.0.0] 2006-05-17
・リリース

*/

/**********************************************************
(zlib license)
Copyright (C) 2006-2008 YAMAMOTO Suguru

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
