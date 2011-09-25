// file: WatchPattern.cs
// brief: Represents watching text pattern.
// author: YAMAMOTO Suguru
// update: 2011-09-25
//=========================================================
using System;
using Regex = System.Text.RegularExpressions.Regex;

namespace Sgry.Azuki
{
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
	///   Another usage is emphasizing already known keywords
	///   such as 'ERROR' or 'FATAL' in application displyaing its or other application's log data.
	///   </para>
	/// </remarks>
	/// <example>
	///   <para>
	///   Next code registers two text patterns to emphasize some keywords.
	///   First pattern is "ERROR" and they are marked with ID 1.
	///   Second pattern is "WARN" or "WARNING" and they will be marked with ID 2.
	///   </para>
	///   <code lang="C#">
	///   //--- somewhere initializing Azuki ---
	///   // Variable 'azuki' is IUserInterface (AzukiControl) here.
	///   
	///   // Register marking ID and its visual decoration for warning log headers
	///   Marking.Register( new MarkingInfo(1, "Warning") );
	///   azuki.ColorScheme.SetMarkingDecoration(
	///           1, new OutlineTextDecoration( Color.Orange )
	///       );
	///   
	///   // Register marking ID and its visual decoration for error log headers
	///   Marking.Register( new MarkingInfo(2, "Error") );
	///   azuki.ColorScheme.SetMarkingDecoration(
	///           2, new OutlineTextDecoration( Color.Red )
	///       );
	///   
	///   //--- somewhere initializing document ---
	///   // Variable 'doc' is an object of Document here.
	///   
	///   // Register text patterns to be watched
	///   doc.WatchPatterns.Add(
	///           new WatchPattern( 1, new Regex(@"ERROR") )
	///       );
	///   doc.WatchPatterns.Add(
	///           new WatchPattern( 2, new Regex(@"WARN(ING)?") )
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
