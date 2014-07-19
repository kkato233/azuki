// See LICENSE.md for license terms of usage.
// v3.0.0.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Debug = System.Diagnostics.Debug;

namespace Sgry.Ini
{
	/// <summary>
	/// An INI document.
	/// </summary>
	/// <remarks>
	///   <para>
	///   This class represents an INI document. An INI document is consisted with one or more
	///   sections and there are zero or more properties in a section. In most cases,
	///   <see cref="IniDocument.Get&lt;T&gt;">IniDocument.Get&lt;T&gt;</see> method and 
	///   <see cref="IniDocument.Set">IniDocument.Set&lt;T&gt;</see> method covers the needs.
	///   </para>
	///   <example>
	///      <code class="C#">
	///      //enum UserType // &lt;-- assume this is defined somewhere else...
	///      //{
	///      //    Permanent,
	///      //    Temporal
	///      //}
	///      
	///      var ini = new IniDocument();
	///      
	///      using( var file = new StreamReader("profile.ini", Encoding.UTF8) )
	///      {
	///          // Load the INI document in the file
	///          ini.Load( file );
	///          
	///          // Extract data in it
	///          string email = ini.Get( "UserProfile", "Email", "" );
	///          int age = ini.Get( "UserProfile", "Age", 0 );
	///          bool locked = ini.Get( "UserProfile", "Locked", false );
	///          UserType type = ini.Get( "UserProfile", "Type", UserType.Permanent ); // enum types can be used directly
	///          
	///          // Do anything with them...
	///          ...
	///      }
	///      </code>
	///   </example>
	///   <para>
	///   This implementations is effectively a sorted list of <see cref="IniSection"/>s. You can
	///   access sections and properties by using IniDocument.<see cref="Sections"/> property as:
	///   </para>
	///   <example>
	///     <code class="C#">
	///     foreach( var section in iniDocument )
	///         Console.WriteLine( "[{0}]", section.Name );
	///         foreach( var prop in seciton )
	///             Console.WriteLine( "{0}={1}", prop.Name, prop.Value );
	///     </code>
	///   </example>
	/// </remarks>
    public class IniDocument : IEnumerable<IniSection>
    {
		readonly List<IniSection> _Sections;
		readonly StringComparison _SectionNameCompType;
		readonly StringComparison _PropertyNameCompType;

		#region Init / Dispose
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public IniDocument()
			: this( null, StringComparison.OrdinalIgnoreCase, StringComparison.OrdinalIgnoreCase )
		{}

		/// <summary>
		/// Creates a new instance.
		/// </summary>
		/// <param name="sectionNameCompType">How to compare section names.</param>
		/// <param name="propNameCompType">How to compare property names.</param>
		public IniDocument( StringComparison sectionNameCompType,
							StringComparison propNameCompType )
			: this( null, sectionNameCompType, propNameCompType )
		{}

		/// <summary>
		/// Creates a new instance and load INI document from the given stream.
		/// </summary>
		/// <param name="src">The stream containing the INI data to load.</param>
		/// <exception cref="ArgumentNullException" />
		/// <exception cref="IOException" />
		/// <exception cref="ObjectDisposedException" />
		/// <exception cref="OutOfMemoryException" />
		public IniDocument( TextReader src )
			: this( src, StringComparison.OrdinalIgnoreCase, StringComparison.OrdinalIgnoreCase )
		{}

		/// <summary>
		/// Creates a new instance and load INI document from the given stream.
		/// </summary>
		/// <param name="src">The stream containing the INI data to load.</param>
		/// <param name="sectionNameCompType">How to compare section names.</param>
		/// <param name="propNameCompType">How to compare property names.</param>
		/// <exception cref="ArgumentNullException" />
		/// <exception cref="IOException" />
		/// <exception cref="ObjectDisposedException" />
		/// <exception cref="OutOfMemoryException" />
		public IniDocument( TextReader src,
							StringComparison sectionNameCompType,
							StringComparison propNameCompType )
		{
			_Sections = new List<IniSection>();
			_SectionNameCompType = sectionNameCompType;
			_PropertyNameCompType = propNameCompType;
			if( src != null )
				Load( src );
		}
		#endregion

		/// <summary>
		/// Gets a property value.
		/// </summary>
		/// <param name="sectionName">
		///   The name of the section in which the property is searched.
		/// </param>
		/// <returns>The section.</returns>
		/// <exception cref="ArgumentNullException" />
		public IniSection Get( string sectionName )
		{
			if( sectionName == null ) throw new ArgumentNullException( "sectionName" );

			int index = FindSection( sectionName );
			return (0 <= index) ? _Sections[index]
								: null;
		}

		/// <summary>
		/// Gets a property value.
		/// </summary>
		/// <param name="sectionName">
		///   The name of the section in which the value is searched.
		/// </param>
		/// <param name="propName">The name of the property whose value is to be retrieved.</param>
		/// <param name="defaultValue">
		///   The value which will be returned if specified property does not exist.
		/// </param>
		/// <returns>
		///   The value of the specified property if found, otherwise given 'defaultValue'.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///   Parameter 'sectionName' or 'propName' was null.
		/// </exception>
		/// <exception cref="ArgumentException">
		///   Parameter 'propName' was an empty string.
		/// </exception>
		public T Get<T>( string sectionName, string propName, T defaultValue )
		{
			T value;

			return TryGet(sectionName, propName, out value) ? value
															: defaultValue;
		}

		/// <summary>
		/// Parse specified property value as an integer in the specific range
		/// </summary>
		/// <param name="sectionName">
		///   The name of the section in which the value is searched.
		/// </param>
		/// <param name="propName">The name of the property whose value is to be retrieved.</param>
		/// <param name="min">The minimum acceptable number.</param>
		/// <param name="max">The maximum acceptable number.</param>
		/// <param name="defaultValue">
		///   The value which will be returned when the property was not found or not in the range.
		/// </param>
		/// <returns>Whether the value was found and matched the given condition or not.</returns>
		/// <exception cref="ArgumentNullException">
		///   Parameter 'sectionName' or 'propName' was null.
		/// </exception>
		/// <exception cref="ArgumentException">
		///   Parameter 'propName' was an empty string.
		/// </exception>
		public int GetInt( string sectionName, string propName, int min, int max,
						   int defaultValue )
		{
			int value;

			return TryGetInt(sectionName, propName, min, max, out value) ? value
																		 : defaultValue;
		}

		/// <summary>
		/// Gets a property value.
		/// </summary>
		/// <param name="sectionName">
		///   The name of the section in which the value is searched.
		/// </param>
		/// <param name="propName">The name of the property whose value is to be retrieved.</param>
		/// <param name="value">
		///   The value will be stored in this variable.
		/// </param>
		/// <returns>Whether the value was found or not.</returns>
		/// <exception cref="ArgumentNullException">
		///   Parameter 'sectionName' or 'propName' was null.
		/// </exception>
		/// <exception cref="ArgumentException">
		///   Parameter 'propName' was an empty string.
		/// </exception>
		public bool TryGet<T>( string sectionName, string propName, out T value )
		{
			if( sectionName == null ) throw new ArgumentNullException( "sectionName" );
			if( propName == null ) throw new ArgumentNullException( "propName" );
			if( propName == String.Empty )
				throw new ArgumentException( "Parameter propName must not be an empty string.",
											 "propName");

			// Get the section
			var section = Get( sectionName );
			if( section == null )
			{
				value = default(T);
				return false;
			}

			// Get the property value
			return section.TryGet( propName, out value );
		}

		/// <summary>
		/// Parse specified property value as an integer in the specific range.
		/// </summary>
		/// <param name="sectionName">
		///   The name of the section in which the value is searched.
		/// </param>
		/// <param name="propName">The name of the property whose value is to be retrieved.</param>
		/// <param name="min">The minimum acceptable number.</param>
		/// <param name="max">The maximum acceptable number.</param>
		/// <param name="value">
		///   The value will be stored in this variable.
		/// </param>
		/// <returns>Whether the property was found and it's in the specified range.</returns>
		/// <exception cref="ArgumentNullException">
		///   Parameter 'sectionName' or 'propName' was null.
		/// </exception>
		/// <exception cref="ArgumentException">
		///   Parameter 'propName' was an empty string.
		///   - OR -
		///   Parametr 'min' was greater than the parameter 'max'.
		/// </exception>
		public bool TryGetInt( string sectionName, string propName, int min, int max,
							   out int value )
		{
			if( max < min ) throw new ArgumentException( "The parameter 'min' must not be greater"
														 + " than the paramter 'max'." );
			return (TryGet(sectionName, propName, out value)
					&& min <= value && value <= max);
		}

		/// <summary>
		/// Sets value of the specified property.
		/// </summary>
		/// <param name="sectionName">
		///   The name of the section in which the property is searched.
		/// </param>
		/// <param name="propName">The name of the property whose value is to be retrieved.</param>
		/// <param name="value">The new value.</param>
		/// <exception cref="ArgumentNullException">
		///   Parameter 'sectionName' or 'propName' was null.
		/// </exception>
		/// <exception cref="ArgumentException">
		///   Parameter 'propName' was an empty string.
		/// </exception>
		public void Set<T>( string sectionName, string propName, T value )
		{
			if( sectionName == null ) throw new ArgumentNullException( "sectionName" );
			if( propName == null ) throw new ArgumentNullException( "propName" );
			if( propName == String.Empty )
				throw new ArgumentException( "Parameter propName must not be an empty string.",
											 "propName");
			IniSection section;

			// Get or create the section
			int index = FindSection( sectionName );
			if( index < 0 )
			{
				section = new IniSection( sectionName, _PropertyNameCompType );
				_Sections.Insert( ~index, section );
			}
			else
			{
				section = _Sections[index];
			}

			// Set the property value
			section.Set( propName, value );
		}

		/// <summary>
		/// Removes the specified section. Nothing happens if the section was not found.
		/// </summary>
		/// <param name="sectionName">The name of the section to be removed.</param>
		/// <exception cref="ArgumentNullException">
		///   Parameter 'sectionName' was null.
		/// </exception>
		public void Remove( string sectionName )
		{
			if( sectionName == null ) throw new ArgumentNullException( "sectionName" );

			int index = FindSection( sectionName );
			if( 0 <= index )
				_Sections.RemoveAt( index );
		}

		/// <summary>
		/// Removes the specified property. Nothing happens if the property was not found.
		/// </summary>
		/// <param name="sectionName">
		///   The name of the section in which the property is searched.
		/// </param>
		/// <param name="propName">The name of the property to be removed.</param>
		/// <exception cref="ArgumentNullException">
		///   Parameter 'sectionName' was null.
		///   - OR -
		///   Parameter 'propName' was null.
		/// </exception>
		public void Remove( string sectionName, string propName )
		{
			if( sectionName == null ) throw new ArgumentNullException( "sectionName" );
			if( propName == null ) throw new ArgumentNullException( "propName" );

			int index = FindSection( sectionName );
			if( 0 <= index )
				_Sections[index].Remove( propName );
		}

		/// <summary>
		/// Removes every sections in the INI data.
		/// </summary>
		public void Clear()
		{
			_Sections.Clear();
		}

		/// <summary>
		/// Gets list of sections in the INI data.
		/// </summary>
		public IIniSectionList Sections
		{
			get{ return new SectionList(this); }
		}

		/// <summary>
		/// Gets the number of sections in the INI data.
		/// </summary>
		public int Count
		{
			get{ return _Sections.Count; }
		}

		/// <summary>
		/// Gets a section with specified name if found, otherwise null.
		/// </summary>
		/// <exception cref="ArgumentNullException"/>
		public IniSection this[ string name ]
		{
			get{ return Get(name); }
		}

		#region Load / Save
		/// <summary>
		/// Loads INI data from specified stream (file, string, etc.)
		/// </summary>
		/// <param name="src">The stream containing the INI data to load.</param>
		/// <exception cref="ArgumentNullException" />
		/// <exception cref="IOException" />
		/// <exception cref="ObjectDisposedException" />
		/// <exception cref="OutOfMemoryException" />
		/// <example>
		///   <code lang="C#">
		///   var ini = new IniDocument();
		///   
		///   // Load INI data in a file
		///   using( var file = new StreamReader("data.ini", Encoding.UTF8) )
		///       ini.Load( file );
		///   
		///   // Load INI data on the memory (String object)
		///   var str = "...(INI data here)...";
		///   ini.Load( new StringReader(str) );
		///   </code>
		/// </example>
		public void Load( TextReader src )
		{
			if( src == null ) throw new ArgumentNullException( "src" );

			string selectedSectionName = String.Empty;

			Clear();
			var line = src.ReadLine();
			while( line != null )
			{
				string sectionName;
				string propName, value;

				line = line.TrimStart();
				if( line == String.Empty || line.StartsWith(";") )
				{
					// Empty line or comment line
				}
				else if( TryParseSection(line, out sectionName) )
				{
					selectedSectionName = sectionName;
				}
				else if( TryParseProperty(line, out propName, out value) )
				{
					Set( selectedSectionName, propName, value );
				}

				line = src.ReadLine();
			}
		}

		/// <summary>
		/// Saves INI data to the specified stream (file, string, etc.)
		/// </summary>
		/// <param name="dest">The stream to which the INI data will be written.</param>
		/// <exception cref="ArgumentNullException" />
		/// <exception cref="ObjectDisposedException" />
		/// <exception cref="IOException" />
		/// <example>
		///   <code lang="C#">
		///   var ini = new IniDocument();
		///   //...(edit content of the INI object)...
		///   
		///   // Save INI data to a file
		///   using( var file = new StreamWriter("data.ini", false, Encoding.UTF8) )
		///   {
		///       file.NewLine = "\r\n";
		///       ini.Save( file );
		///   }
		///   
		///   // Save (serialize) INI data on the memory (as a StringBuilder object)
		///   var buf = new StringBuilder();
		///   var writer = new StringWriter( buf );
		///   writer.NewLine = "\r\n";
		///   ini.Save( new StringWriter(buf) );
		///   </code>
		/// </example>
		public void Save( TextWriter dest )
		{
			if( dest == null ) throw new ArgumentNullException( "dest" );

			foreach( var section in _Sections )
			{
				if( section.Count == 0 )
					continue; // ignore sections without a property

				// Write section starting line
				if( section.Name != String.Empty )
					dest.WriteLine( "[" + section.Name + "]" );

				// Write property lines
				foreach( var prop in section )
					dest.WriteLine( prop.Name + "=" + prop.Value );
			}
		}
		#endregion

		#region IEnumerable
		/// <summary>
		/// Gets an object to enumerate properties in the section.
		/// </summary>
		public IEnumerator<IniSection> GetEnumerator()
		{
			return Sections.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		#endregion

		#region Utilities
		int FindSection( string name )
		{
			Debug.Assert( name != null );
			return _Sections.BinarySearch( new IniSection(name, _PropertyNameCompType),
										   new NameComparer<IniSection>(_SectionNameCompType) );
		}

		static bool TryParseSection( string line, out string name )
		{
			Debug.Assert( line != String.Empty );

			int begin, end;
			name = null;

			// Find where the section name begins
			for( begin=0; begin<line.Length; begin++ )
			{
				if( line[begin] == '[' )
					break;
				else if( line[begin] == ' ' || line[begin] == '\t' )
					continue;
				else
					return false;
			}

			// Find where the section name ends
			for( end=line.Length-1; 0<=end; --end )
			{
				if( line[end] == ']' )
					break;
				else if( line[end] == ' ' || line[end] == '\t' )
					continue;
				else
					return false;
			}

			// Extract section name
			name = line.Substring( begin+1, end - begin - 1 );
			return true;
		}

		static bool TryParseProperty( string line, out string name, out string value )
		{
			Debug.Assert( line != String.Empty );

			name = value = null;

			// Find where the property name begins
			var nameBegin = IndexNotAnyOf( line, " \t=", 0 );
			if( nameBegin < 0 )
			{
				return false;
			}

			// Find where the property name ends
			var eqPos = line.IndexOf('=');
			if( eqPos < 0 )
			{
				return false;
			}
			var nameEnd = LastIndexNotAnyOf( line, " \t=", eqPos );
			if( nameEnd < 0 )
			{
				return false;
			}
			nameEnd++;

			// Find where the property value begins
			var valueBegin = eqPos + 1;

			// Find where the property value ends
			var valueEnd = line.Length - 1;
			Debug.Assert( 0 <= valueEnd );
			valueEnd++;

			// extract property's name and value
			name = line.Substring( nameBegin, nameEnd-nameBegin );
			value = line.Substring( valueBegin, valueEnd-valueBegin );
			return true;
		}

		static int IndexNotAnyOf( string str, string anyOf, int startIndex )
		{
			int	i, j;
			var	matchExists = false;

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

		static int LastIndexNotAnyOf( string str, string anyOf, int startIndex )
		{
			int	i, j;
			var	matchExists = false;

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

		class SectionList : IIniSectionList
		{
			readonly IniDocument _Ini;

			public SectionList( IniDocument ini )
			{
				_Ini = ini;
			}

			public IniSection this[ string name ]
			{
				get{ return _Ini.Get(name); }
			}

			public int Count
			{
				get{ return _Ini._Sections.Count; }
			}

			public IEnumerator<IniSection> GetEnumerator()
			{
				foreach( var section in _Ini._Sections )
					yield return section;
			}

			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}
		}
		#endregion
	}

	/// <summary>
	/// Section of INI data.
	/// </summary>
    public class IniSection : INamedObject, IEnumerable<IniProperty>
    {
		string _Name;
		readonly List<IniProperty> _Properties;
		readonly StringComparison _PropNameCompType;

		/// <summary>
		/// Creates a new instance.
		/// </summary>
		/// <exception cref="ArgumentNullException"/>
		public IniSection( string name, StringComparison propNameCompType )
		{
			_Properties = new List<IniProperty>();
			Name = name;
			_PropNameCompType = propNameCompType;
		}

		/// <summary>
		/// Gets a property with specified name.
		/// </summary>
		/// <param name="name">The name of the property to be retrieved.</param>
		/// <returns>The property with specified name if found, otherwise null.</returns>
		/// <exception cref="ArgumentNullException"/>
		/// <exception cref="ArgumentException">Parameter 'name' was an empty string.</exception>
		public IniProperty Get( string name )
		{
			if( name == null ) throw new ArgumentNullException();
			if( name == String.Empty ) throw new ArgumentException( "Parameter 'name' must not be"
																	+ " an empty string." );

			var index = FindProperty( name );
			return (0 <= index) ? _Properties[index]
								: null;
		}

		/// <summary>
		/// Gets value of a property with specified name.
		/// </summary>
		/// <param name="name">The name of the property whose value is to be retrieved.</param>
		/// <param name="defaultValue">
		///   The value which will be returned if specified property does not exist.
		/// </param>
		/// <returns>
		///   The value of the property with specified name, otherwise given 'defaultValue'.
		/// </returns>
		/// <exception cref="ArgumentNullException"/>
		/// <exception cref="ArgumentException">
		///   Parameter 'name' was an empty string.
		/// </exception>
		public T Get<T>( string name, T defaultValue )
		{
			T value;

			return TryGet(name, out value) ? value
										   : defaultValue;
		}

		/// <summary>
		/// Parse specified property value as an integer in the specific range.
		/// </summary>
		/// <param name="name">The name of the property whose value is to be retrieved.</param>
		/// <param name="min">The minimum acceptable value.</param>
		/// <param name="max">The maximum acceptable value.</param>
		/// <param name="defaultValue">
		///   The value which will be returned if specified property does not exist or not in the
		///   specified range.
		/// </param>
		/// <returns>
		///   The property value if every condition matches, otherwise 'defaultValue'.
		/// </returns>
		/// <exception cref="ArgumentNullException"/>
		/// <exception cref="ArgumentException">
		///   Parameter 'name' was an empty string.
		///   - OR -
		///   Parameter 'min' was greater than 'max'.
		/// </exception>
		public int GetInt( string name, int min, int max, int defaultValue )
		{
			int num;

			return TryGetInt(name, min, max, out num) ? num
													  : defaultValue;
		}

		/// <summary>
		/// Gets value of a property with specified name.
		/// </summary>
		/// <param name="name">The name of the property whose value is to be retrieved.</param>
		/// <param name="value">The value retrieved will be stored to this variable.</param>
		/// <returns>Whether the value could be retrieved or not.</returns>
		/// <exception cref="ArgumentNullException"/>
		/// <exception cref="ArgumentException">
		///   Parameter 'name' was an empty string.
		/// </exception>
		public bool TryGet<T>( string name, out T value )
		{
			if( name == null ) throw new ArgumentNullException( "name" );
			if( name == String.Empty ) throw new ArgumentException( "Parameter 'name' must not be"
																	+ " an empty string." );

			var index = FindProperty( name );
			if( 0 <= index )
			{
				try
				{
					var valueStr = _Properties[index].Value;
					if( typeof(T).IsEnum )
						value = (T)Enum.Parse( typeof(T), valueStr, false );
					else
						value = (T)Convert.ChangeType( valueStr, typeof(T), null );
					return true;
				}
				catch( FormatException )
				{}
				catch( ArgumentException )
				{}
				catch( InvalidCastException )
				{} // Can this happen?
			}

			value = default(T);
			return false;
		}

		/// <summary>
		/// Gets an integer value of the specified property.
		/// </summary>
		/// <param name="name">The name of the property whose value is to be retrieved.</param>
		/// <param name="min">The minimum acceptable value.</param>
		/// <param name="max">The maximum acceptable value.</param>
		/// <param name="value">The value retrieved will be stored to this variable.</param>
		/// <returns>Whether the value could be retrieved or not.</returns>
		/// <exception cref="ArgumentNullException"/>
		/// <exception cref="ArgumentException">
		///   Parameter 'name' was an empty string.
		///   - OR -
		///   Parameter 'min' was greater than 'max'.
		/// </exception>
		public bool TryGetInt( string name, int min, int max, out int value )
		{
			if( max < min ) throw new ArgumentException( "Parameter 'min' must be less than or"
														 + " equal to 'max'." );
			if( TryGet(name, out value)
				&& min <= value && value <= max )
				return true;

			return false;
		}

		/// <summary>
		/// Sets a property value in the section.
		/// </summary>
		/// <param name="name">The name of the property whose value is to be updated.</param>
		/// <param name="value">The new value.</param>
		/// <exception cref="ArgumentException">Parameter 'name' was an empty string.</exception>
		/// <exception cref="ArgumentNullException">
		///   Parameter 'name' or 'value' was null.
		/// </exception>
		public void Set<T>( string name, T value )
		{
			if( name == null ) throw new ArgumentNullException( "name" );
			if( name == "" ) throw new ArgumentException( "parameter name cannot be an empty"
														  + " string." );
			if( value == null ) throw new ArgumentNullException( "value" );

			var index = FindProperty( name );
			if( index < 0 )
				_Properties.Insert( ~index, new IniProperty(name, value.ToString()) );
			else
				_Properties[index].Value = value.ToString();
		}

		/// <summary>
		/// Removes a property with specified name in the section.
		/// </summary>
		/// <param name="name">The name of the property to be removed.</param>
		/// <returns>Whether a property was removed or not.</returns>
		/// <exception cref="ArgumentException">Parameter 'name' was an empty string.</exception>
		/// <exception cref="ArgumentNullException">Parameter 'name' was null.</exception>
		public bool Remove( string name )
		{
			if( name == null ) throw new ArgumentNullException( "name" );
			if( name == "" ) throw new ArgumentException( "parameter name cannot be an empty"
														  + " string." );

			var index = FindProperty( name );
			if( 0 <= index )
			{
				_Properties.RemoveAt( index );
				return true;
			}
			return false;
		}

		/// <summary>
		/// Removes every properties in the section.
		/// </summary>
		public void Clear()
		{
			_Properties.Clear();
		}

		/// <summary>
		/// Gets a list of properties in the section.
		/// </summary>
		public IIniPropertyList Properties
		{
			get{ return new PropertyListImpl(this); }
		}

		/// <summary>
		/// Gets the number properties in the section.
		/// </summary>
		public int Count
		{
			get{ return _Properties.Count; }
		}

		/// <summary>
		/// Gets or sets value of the property with specified value.
		/// </summary>
		/// <param name="name">The name of the property whose value is to be retrieved.</param>
		/// <returns>The property value or null if no property with specified name exist.</returns>
		/// <exception cref="ArgumentException">Given value was an empty string.</exception>
		/// <exception cref="ArgumentNullException">Given value was null.</exception>
		public string this[ string name ]
		{
			get{ return Get<string>(name, null); }
			set{ Set(name, value); }
		}

		/// <summary>
		/// Gets or sets the name of the section.
		/// </summary>
		/// <exception cref="ArgumentNullException" />
		public string Name
		{
			get{ return _Name; }
			set
			{
				if( value == null ) throw new ArgumentNullException( "value" );
				_Name = value;
			}
		}

		/// <summary>
		/// Gets an object to enumerate properties in the section.
		/// </summary>
		public IEnumerator<IniProperty> GetEnumerator()
		{
			foreach( var prop in _Properties )
				yield return prop;
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#region Utilities
		int FindProperty( string name )
		{
			Debug.Assert( !String.IsNullOrEmpty(name) );
			return _Properties.BinarySearch( new IniProperty(name, ""),
											 new NameComparer<IniProperty>(_PropNameCompType) );
		}

		class PropertyListImpl : IIniPropertyList
		{
			readonly IniSection _Section;

			public PropertyListImpl( IniSection section )
			{
				_Section = section;
			}

			public IniProperty this[ string name ]
			{
				get{ return _Section.Get(name); }
			}

			public int Count
			{
				get { return _Section._Properties.Count; }
			}

			public IEnumerator<IniProperty> GetEnumerator()
			{
				foreach( var prop in _Section._Properties )
					yield return prop;
			}

			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}
		}
		#endregion
	}

	/// <summary>
	/// Property in INI section.
	/// </summary>
	public class IniProperty : INamedObject
    {
		string _Name;
		string _Value;

		/// <summary>
		/// Creates a new instance.
		/// </summary>
		/// <exception cref="ArgumentNullException"/>
		/// <exception cref="ArgumentException">Parameter 'name' was an empty string.</exception>
		public IniProperty( string name, string value )
		{
			Name = name;
			Value = value;
		}

		/// <summary>
		/// Gets or sets the name of the property.
		/// </summary>
		/// <exception cref="ArgumentNullException"/>
		/// <exception cref="ArgumentException">An empty string was assigned.</exception>
        public string Name
		{
			get{ return _Name; }
			set
			{
				if( value == null ) throw new ArgumentNullException( "value" );
				if( value == String.Empty ) throw new ArgumentException( "value" );
				_Name = value;
			}
		}

		/// <summary>
		/// Gets or sets the value of the property.
		/// </summary>
		/// <exception cref="ArgumentNullException"/>
		public string Value
		{
			get{ return _Value; }
			set
			{
				if( value == null ) throw new ArgumentNullException( "value" );
				_Value = value;
			}
		}

		/// <summary>
		/// Gets or sets the value of the property as an integer.
		/// </summary>
		/// <exception cref="FormatException">
		///   Tried to convert value as an integer but its format was not recognizable.
		/// </exception>
		/// <exception cref="OverflowException">
		///   Tried to convert value as an Int32 but overflowed.
		/// </exception>
		public int IntValue
		{
			get{ return Int32.Parse(_Value); }
			set{ _Value = value.ToString(CultureInfo.InvariantCulture); }
		}
    }

	/// <summary>
	/// List of sections.
	/// </summary>
	public interface IIniSectionList : IEnumerable<IniSection>
	{
		/// <summary>Gets a section with specified name.</summary>
		IniSection this[ string name ] { get; }

		/// <summary>Gets the number of sections in the list.</summary>
		int Count { get; }
	}

	/// <summary>
	/// List of properties.
	/// </summary>
	public interface IIniPropertyList : IEnumerable<IniProperty>
	{
		/// <summary>Gets a property with specified name.</summary>
		IniProperty this[ string name ] { get; }

		/// <summary>Gets the number of properties in the list.</summary>
		int Count { get; }
	}

	#region Non-public types
	interface INamedObject
	{
		string Name { get; }
	}

	class NameComparer<T> : IComparer<T> where T : INamedObject
	{
		readonly StringComparison _ComparisonType;

		public NameComparer( StringComparison comparisonType )
		{
			_ComparisonType = comparisonType;
		}

		public int Compare( T x, T y )
		{
			return String.Compare( x.Name, y.Name, _ComparisonType );
		}
	}
	#endregion
}
