// file: Highlighters.cs
// brief: highlighter object repository.
// author: YAMAMOTO Suguru
// update: 2008-11-03
//=========================================================
using System;

namespace Sgry.Azuki.Highlighter
{
	/// <summary>
	/// Repository to serve built-in highlighter objects.
	/// </summary>
	public static class Highlighters
	{
		#region Fields
		static CppHighlighter _CppHighlighter = null;
		static CSharpHighlighter _CSharpHighlighter = null;
		static XmlHighlighter _XmlHighlighter = null;
		static JavaHighlighter _JavaHighlighter = null;
		static RubyHighlighter _RubyHighlighter = null;
		#endregion

		#region Built-in Highlighters
		/// <summary>
		/// Gets a highlighter for C/C++.
		/// </summary>
		public static IHighlighter Cpp
		{
			get
			{
				if( _CppHighlighter == null )
				{
					_CppHighlighter = new CppHighlighter();
				}
				return _CppHighlighter;
			}
		}

		/// <summary>
		/// Gets a highlighter for C#.
		/// </summary>
		public static IHighlighter CSharp
		{
			get
			{
				if( _CSharpHighlighter == null )
				{
					_CSharpHighlighter = new CSharpHighlighter();
				}
				return _CSharpHighlighter;
			}
		}

		/// <summary>
		/// Gets a highlighter for Java.
		/// </summary>
		public static IHighlighter Java
		{
			get
			{
				if( _JavaHighlighter == null )
				{
					_JavaHighlighter = new JavaHighlighter();
				}
				return _JavaHighlighter;
			}
		}

		/// <summary>
		/// Gets a highlighter for Ruby.
		/// </summary>
		public static IHighlighter Ruby
		{
			get
			{
				if( _RubyHighlighter == null )
				{
					_RubyHighlighter = new RubyHighlighter();
				}
				return _RubyHighlighter;
			}
		}

		/// <summary>
		/// Gets a highlighter for XML.
		/// </summary>
		public static IHighlighter Xml
		{
			get
			{
				if( _XmlHighlighter == null )
				{
					_XmlHighlighter = new XmlHighlighter();
				}
				return _XmlHighlighter;
			}
		}
		#endregion
	}
}
