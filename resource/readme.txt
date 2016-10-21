readme.txt
                                                                     2016-10-21
                                                                Suguru Yamamoto
                                               https://osdn.net/projects/azuki/

== Package contents ==

- Azuki.dll
	To use Azuki in a desktop application,
	please reference this DLL.

- Ann.exe
	Sample program for testing Azuki.

- Azuki.xml
	XML document of Azuki.dll.
	In case of developing an application
	using Azuki with IDE like Visual Studio,
	please copy this file with Azuki.dll to same directory.

== about assembly sign of Azuki.dll ==
This package contains Azuki.dll which are digitally signed.
The reason why it is signed is,
they can be referenced by other signed assembly if it is signed.
Ensuring that the assemblies are not altered is NOT the reason.
If you will be needed to prove that
the Azuki.dll you are using is not altered,
you SHOULD build another Azuki.dll from source code package
with your own key file.
