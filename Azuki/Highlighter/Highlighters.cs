// file: Highlighters.cs
// brief: highlighter object factory.
//=========================================================
using System;

namespace Sgry.Azuki.Highlighter
{
	/// <summary>
	/// Factory to create built-in highlighter objects.
	/// </summary>
	public static class Highlighters
	{
		#region Built-in Highlighters
		/// <summary>
		/// Gets a new highlighter for LaTeX.
		/// </summary>
		public static IHighlighter Latex
		{
			get{ return new LatexHighlighter(); }
		}

		/// <summary>
		/// Gets a new highlighter for C/C++.
		/// </summary>
		public static IHighlighter Cpp
		{
			get{ return new CppHighlighter(); }
		}

		/// <summary>
		/// Gets a new highlighter for C#.
		/// </summary>
		public static IHighlighter CSharp
		{
			get{ return new CSharpHighlighter(); }
		}

		/// <summary>
		/// Gets a new highlighter for INI.
		/// </summary>
		public static IHighlighter Ini
		{
			get{ return new IniHighlighter(); }
		}

		/// <summary>
		/// Gets a new highlighter for Java.
		/// </summary>
		public static IHighlighter Java
		{
			get{ return new JavaHighlighter(); }
		}

		/// <summary>
		/// Gets a new highlighter for Ruby.
		/// </summary>
		public static IHighlighter Ruby
		{
			get{ return new RubyHighlighter(); }
		}

		/// <summary>
		/// Gets a new highlighter for JavaScript.
		/// </summary>
		public static IHighlighter JavaScript
		{
			get{ return new JavaScriptHighlighter(); }
		}

		/// <summary>
		/// Gets a new highlighter for XML.
		/// </summary>
		public static IHighlighter Xml
		{
			get{ return new XmlHighlighter(); }
		}
		#endregion
	}
}
