using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Resources;

#if !PocketPC
[assembly: AssemblyTitle("Ann - a sample text editor")]
[assembly: AssemblyProduct("Ann")]
[assembly: AssemblyDescription("Ann - a sample text editor using Azuki for .NET Framework")]
#else
[assembly: AssemblyTitle("AnnCompact - a sample text editor")]
[assembly: AssemblyProduct("AnnCompact")]
[assembly: AssemblyDescription("AnnCompact - a sample text editor using Azuki for .NET Compact Framework")]
#endif

[assembly: AssemblyCompany("YAMAMOTO Suguru")]
[assembly: AssemblyCopyright("Copyright (C) 2008-2010, YAMAMOTO Suguru")]

[assembly: ComVisible(false)]
[assembly: AssemblyVersion("1.4.3.*")]
