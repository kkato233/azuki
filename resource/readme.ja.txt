readme.ja.txt
                                                                     2016-10-21
                                                                Suguru Yamamoto
                                               https://osdn.net/projects/azuki/

== 同梱物の説明 ==

- Azuki.dll
	Azuki のエンジンファイルです。
	デスクトップアプリケーションで Azuki を使う場合は
	この DLL を参照します。

- Ann.exe
	Azuki の動作確認用サンプルプログラムです。

- Azuki.xml
	Azuki.dll の XML ドキュメントファイルです。
	Visual Studio などの統合開発環境で使う場合は
	Azuki.dll と同じディレクトリにセットでコピーします。

== Azuki.dll のアセンブリ署名について ==
パッケージに付属している Azuki.dll には「アセンブリ署名」を行っています。
この目的は、アセンブリ署名を行っている別プロジェクトから参照可能にすることであり、
アセンブリの不正な変更を防止することではありません。
もし、 Azuki.dll を使用するとそれが不正に変更されていないことを
証明しなければならなくなると予想される場合、
独自のキーを使って Azuki.dll をソースからビルドして署名すべきでしょう。
