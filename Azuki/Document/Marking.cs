// file: Marking.cs
// brief: Classes related to marking which indicates attributes apart from syntax and grammer.
// author: YAMAMOTO Suguru
// update: 2010-11-27
//=========================================================
using System;
using System.Collections.Generic;

namespace Sgry.Azuki
{
	public class MarkingInfo
	{
		int _ID;
		string _Name;
		bool _Clickable;

		public MarkingInfo( int id, string name )
		{
			_ID = id;
			_Name = name;
		}

		public MarkingInfo( int id, string name, bool clickable )
			: this( id, name )
		{
			_Clickable = clickable;
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

		public bool Clickable
		{
			get{ return _Clickable; }
			set{ _Clickable = value; }
		}
	}

	/// <summary>
	/// Manager of marking information
	/// which is to indicate attributes apart from syntax or grammer.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The 'marking' feature is provided for putting
	/// additional (meta) information on text ranges.
	/// Typically, the information provided by marking is
	/// not related to syntax or grammer of document type
	/// such as indicating misspelled words
	/// or indicating words where syntax error was detected.
	/// </para>
	/// <para>
	/// Once one or more ID was marked on a text part,
	/// the text part can be graphically decorated.
	/// </para>
	/// <para>
	/// Note that there is reserved marking ID
	/// which is used for URI 
	/// </para>
	/// </remarks>
	public static class Marking
	{
		public const int MaxID = 8;
		static MarkingInfo[] _MarkingInfoAry = new MarkingInfo[MaxID];

		static Marking()
		{
			_MarkingInfoAry[0] = new MarkingInfo( 1, "URI", true );
			_MarkingInfoAry[1] = new MarkingInfo( 2, "Warning", false );
			_MarkingInfoAry[2] = new MarkingInfo( 3, "Error", false );
			_MarkingInfoAry[3] = new MarkingInfo( 4, "Syntax error", false );
			_MarkingInfoAry[4] = new MarkingInfo( 5, "Misspelling", false );
		}

		public static int Uri
		{
			get{ return 1; }
		}

		/// <summary>
		/// Registers a marking ID.
		/// </summary>
		/// <param name="id">ID of marking to be registered.</param>
		/// <param name="info">The information of the marking.</param>
		/// <remarks>
		/// <para>
/// Registers marking...
		/// </para>
		/// <para>
		/// Note that there are reserved marking IDs
		/// which cannot be changed by user.
		/// Next marking IDs are currently reserved.
		/// </para>
		/// <list style="bullet">
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
