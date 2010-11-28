// file: Marking.cs
// brief: Classes related to marking which indicates attributes apart from syntax and grammer.
// author: YAMAMOTO Suguru
// update: 2010-11-27
//=========================================================
using System;
using System.Collections.Generic;

namespace Sgry.Azuki
{
	/// <summary>
	/// Information of marking.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This class is a collection of non-graphical information about a marking type.
	/// How marked text parts should be drawn is
	/// determined not by this class but by <see cref="Sgry.Azuki.ColorScheme"/> class.
	/// </para>
	/// <para>
	/// Please refer to document of
	/// <see cref="Sgry.Azuki.Marking"/> class
	/// for detail about the marking feature of Azuki.
	/// </para>
	/// </remarks>
	/// <seealso cref="Sgry.Azuki.Marking">MarkingInfo class</seealso>
	/// <seealso cref="Sgry.Azuki.ColorScheme">ColorScheme class</seealso>
	public class MarkingInfo
	{
		int _ID;
		string _Name;
		MouseCursor _MouseCursor;

		public MarkingInfo( int id, string name )
		{
			_ID = id;
			_Name = name;
			_MouseCursor = MouseCursor.IBeam;
		}

		public MarkingInfo( int id, string name, MouseCursor cursor )
			: this( id, name )
		{
			_MouseCursor = cursor;
		}

		public int ID
		{
			get{ return _ID; }
			set{ _ID = value; }
		}

		public string Name
		{
			get{ return _Name; }
			set{ _Name = value; }
		}

		public MouseCursor MouseCursor
		{
			get{ return _MouseCursor; }
			set{ _MouseCursor = value; }
		}
	}

	/// <summary>
	/// Manager of marking information
	/// which is to indicate attributes apart from syntax or grammer.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The 'marking' feature is provided for putting
	/// additional (meta) information on text ranges
	/// which is not related to syntax or grammer of document type.
	/// Typical usage is to indicate misspelled words
	/// or words where syntax error was detected.
	/// </para>
	/// <para>
	/// Multiple markings can be put on any text part independently.
	/// To mark up text parts or remove already marked IDs from text parts,
	/// use <see cref="Sgry.Azuki.Document.Mark">Document.Mark method</see>
	/// and <see cref="Sgry.Azuki.Document.Unmark(int, int, int)">Document.Unmark method</see>.
	/// 
	/// Once a text part was marked, it will graphically be decorated
	/// as specified by <see cref="Sgry.Azuki.ColorScheme"/> class.
	/// To get or set how marked text will be decorated, use method next.
	/// </para>
	/// <list type="bullet">
	///		<item><see cref="Sgry.Azuki.ColorScheme.GetMarkingDecorations(int[])">ColorScheme.GetMarkingDecorations(int[]) method</see></item>
	///		<item><see cref="Sgry.Azuki.ColorScheme.SetMarkingDecoration">ColorScheme.SetMarkingDecoration method</see></item>
	/// </list>
	/// <para>
	/// Internally, marking IDs set for each character are stored as bit mask (currently 8-bit).
	/// Although all operations can be done without minding it,
	/// in some cases, using internal bit mask directly is more efficient than using array of IDs.
	/// To handle bit mask directly, use
	/// <see cref="Sgry.Azuki.Document.GetMarkingBitMaskAt">
	/// Document.GetMarkingBitMaskAt method</see> and
	/// <see cref="Sgry.Azuki.ColorScheme.GetMarkingDecorations(uint)">
	/// ColorScheme.GetMarkingDecorations method</see>.
	/// </para>
	/// <para>
	/// Marking ID '0' is reserved and used to mark URI internally.
	/// Users can use ID from 1 to 7 for any use.
	/// </para>
	/// </remarks>
	/// <seealso cref="Sgry.Azuki.MarkingInfo">MarkingInfo class</seealso>
	/// <seealso cref="Sgry.Azuki.ColorScheme">ColorScheme class</seealso>
	public static class Marking
	{
		#region Pulic constants and properties
		/// <summary>
		/// Maximum number of marking ID currently supported.
		/// </summary>
		public const int MaxID = 8;

		/// <summary>
		/// ID of URI marking type.
		/// </summary>
		public static int Uri
		{
			get{ return 0; }
		}
		#endregion
		
		#region Fields
		static MarkingInfo[] _MarkingInfoAry = new MarkingInfo[MaxID];
		#endregion

		#region Init / Dispose
		static Marking()
		{
			_MarkingInfoAry[0] = new MarkingInfo( 0, "URI", MouseCursor.Hand );
			_MarkingInfoAry[1] = new MarkingInfo( 1, "Warning" );
			_MarkingInfoAry[2] = new MarkingInfo( 2, "Error" );
			_MarkingInfoAry[3] = new MarkingInfo( 3, "Syntax error" );
			_MarkingInfoAry[4] = new MarkingInfo( 4, "Misspelling" );
		}
		#endregion

		/// <summary>
		/// Registers a marking ID.
		/// </summary>
		/// <param name="info">The information of the marking.</param>
		/// <remarks>
		/// <para>
		/// This method registers a marking ID and its information.
		/// If specified ID was already registered, existing information will be overwritten.
		/// </para>
		/// <para>
		/// Note that there are reserved marking IDs
		/// which cannot be changed by user.
		/// Next marking IDs are currently reserved.
		/// </para>
		/// <list type="bullet">
		///		<item>0: URI</item>
		/// </list>
		/// </remarks>
		/// <exception cref="System.ArgumentNullException">
		///		Parameter <paramref name="info"/> is null.
		///	</exception>
		/// <exception cref="System.ArgumentException">
		///		ID of parameter <paramref name="info"/> is out of range.
		///	</exception>
		public static void Register( MarkingInfo info )
		{
			if( info == null )
				throw new ArgumentNullException( "info" );
			if( info.ID <= 1 )
				throw new ArgumentException( "Marking ID must be greater than 1. (info.ID:"+info.ID+")" );
			if( MaxID <= info.ID )
				throw new ArgumentException( "Marking ID must be less than "+MaxID+". (info.ID:"+info.ID+")" );

			_MarkingInfoAry[info.ID] = info;
		}

		/// <summary>
		/// Gets information about marking specified by ID.
		/// </summary>
		/// <returns>Information about specified marking.</returns>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///		<paramref name="id"/> is out of valid range.
		/// </exception>
		public static MarkingInfo GetMarkingInfo( int id )
		{
			if( id < 0 || MaxID <= id )
				throw new ArgumentOutOfRangeException( "id", "Marking ID must be greater than 0 and equal or less than Marking.MaxID. (id:"+id+")" );

			return _MarkingInfoAry[id];
		}

		/// <summary>
		/// Gets an enumerator to list up all registered marking information.
		/// </summary>
		/// <returns>An enumerator for marking information.</returns>
		public static IEnumerable<MarkingInfo> GetEnumerator()
		{
			for( int i=0; i<_MarkingInfoAry.Length; i++ )
			{
				if( _MarkingInfoAry[i] != null )
				{
					yield return _MarkingInfoAry[i];
				}
			}
		}
	}
}
