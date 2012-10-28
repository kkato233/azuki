// file: WatchPattern.cs
// brief: Represents watching text pattern.
//=========================================================
using System;
using System.Collections.Generic;
using Regex = System.Text.RegularExpressions.Regex;

namespace Sgry.Azuki
{
	/// <summary>
	/// Set of WatchPattern objects.
	/// </summary>
	public class WatchPatternSet : IEnumerable<WatchPattern>
	{
		List<WatchPattern> _Patterns = new List<WatchPattern>();

		/// <summary>
		/// Registers a text pattern to be watched and automatically marked.
		/// </summary>
		/// <param name="pattern">The pattern of the text to be watched and automatically marked.</param>
		/// <exception cref="System.ArgumentNullException">The argument 'pattern' was null.</exception>
		/// <seealso cref="Sgry.Azuki.WatchPatternSet.Unregister">Unregister method</seealso>
		public void Register( WatchPattern pattern )
		{
			if( pattern == null )
				throw new ArgumentNullException( "pattern" );

			// if the ID was already registered, overwrite it
			for( int i=0; i<_Patterns.Count; i++ )
			{
				if( _Patterns[i].MarkingID == pattern.MarkingID )
				{
					_Patterns[i] = pattern;
					return;
				}
			}

			// otherwise, add the pattern
			if( pattern.Pattern != null )
			{
				_Patterns.Add( pattern );
			}
		}

		/// <summary>
		/// Unregister a watch-pattern by markingID.
		/// </summary>
		public void Unregister( int markingID )
		{
			// if the ID was already registered, overwrite it
			for( int i=0; i<_Patterns.Count; i++ )
			{
				if( _Patterns[i].MarkingID == markingID )
				{
					_Patterns.RemoveAt( i );
					return;
				}
			}
		}

		/// <summary>
		/// Gets a watch-pattern by marking ID.
		/// </summary>
		public WatchPattern Get( int markingID )
		{
			// if the ID was already registered, overwrite it
			for( int i=0; i<_Patterns.Count; i++ )
			{
				if( _Patterns[i].MarkingID == markingID )
				{
					return _Patterns[ i ];
				}
			}
			return null;
		}

		/// <summary>
		/// Gets the enumerator that iterates through the WatchPatternSet.
		/// </summary>
		public IEnumerator<WatchPattern> GetEnumerator()
		{
			return _Patterns.GetEnumerator();
		}

		/// <summary>
		/// Gets the enumerator that iterates through the WatchPatternSet.
		/// </summary>
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return _Patterns.GetEnumerator();
		}
	}

	/// <summary>
	/// Text pattern to be watched and marked automatically.
	/// </summary>
	/// <remarks>
	///   <para>
	///   This class represents a text pattern which should always be watched by Azuki.
	///   By registering these watching patterns to
	///   <see cref="Sgry.Azuki.Document.WatchPatterns">Document.WatchPatterns</see>,
	///   such patterns will be automatically marked by Azuki as soon as it is graphically drawn
	///   so that such patterns will be able to distinguished visually and logically too.
	///   </para>
	///   <para>
	///   Most typical usage of this feature is emphasizing visually text patterns
	///   which the user is currently searching for.
	///   Another possible usage is emphasizing patterns which the user is interested in.
	///   </para>
	/// </remarks>
	/// <example>
	///   <para>
	///   Next example code illustrates how to use WatchPattern
	///   to emphasize text search results in a document.
	///   </para>
	///   <code lang="C#">
	///   //--- somewhere initializing Azuki ---
	///   // Variable 'azuki' is IUserInterface (AzukiControl) here.
	///   
	///   // Register marking ID and its visual decoration for search results
	///   Marking.Register( new MarkingInfo(30, "Text search result.") );
	///   azuki.ColorScheme.SetMarkingDecoration(
	///           30, new BgColorTextDecoration( Color.Yellow )
	///       );
	///   
	///   //--- somewhere after text search was started ---
	///   // Variable 'doc' is an object of Document here
	///   // Show a dialog to let user input the pattern to search
	///   Regex pattern;
	///   DialogResult result = ShowFindDialog( out pattern );
	///   if( result != DialogResult.OK )
	///       return;
	///   
	///   // Update the text patterns to be watched
	///   doc.WatchPatterns.Register(
	///           new WatchPattern( 30, pattern )
	///       );
	///   </code>
	/// </example>
	public class WatchPattern
	{
		int _MarkingID;
		Regex _Pattern;

		#region Init / Dispose
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public WatchPattern()
		{}

		/// <summary>
		/// Creates a new instance.
		/// </summary>
		/// <param name="markingID">
		///   The marking ID to be marked for each found matching patterns.
		/// </param>
		/// <param name="patternToBeWatched">
		///   The pattern to be watched and to be marked with '<paramref name="markingID"/>.'
		/// </param>
		/// <exception cref="System.ArgumentException">
		///   Parameter '<paramref name="markingID"/>' is invalid or not registered.
		/// </exception>
		public WatchPattern( int markingID, Regex patternToBeWatched )
		{
			if( Marking.GetMarkingInfo(markingID) == null )
				throw new ArgumentException( "Specified marking ID (" + markingID + ") is"
											 + " not registered.",
											 "markingID" );

			MarkingID = markingID;
			Pattern = patternToBeWatched;
		}
		#endregion

		#region Properties
		/// <summary>
		/// The marking ID to be marked for each found matching patterns.
		/// </summary>
		public int MarkingID
		{
			get{ return _MarkingID; }
			set{ _MarkingID = value; }
		}

		/// <summary>
		/// The pattern to be watched and to be marked automatically.
		/// (accepts null.)
		/// </summary>
		/// <remarks>
		///   <para>
		///   This property gets or sets the pattern to be watched.
		///   If the pattern is null or regular expression is an empty string,
		///   Azuki simply ignores the watch pattern.
		///   </para>
		/// </remarks>
		public Regex Pattern
		{
			get{ return _Pattern; }
			set{ _Pattern = value; }
		}
		#endregion
	}
}
