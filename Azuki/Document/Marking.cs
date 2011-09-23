// file: Marking.cs
// brief: Classes related to marking which indicates attributes apart from syntax and grammar.
// author: YAMAMOTO Suguru
// update: 2011-09-23
//=========================================================
using System;
using System.Collections.Generic;

namespace Sgry.Azuki
{
	/// <summary>
	/// Information of marking.
	/// </summary>
	/// <remarks>
	///   <para>
	///   This class is a collection of non-graphical information about a marking type.
	///   How marked text parts should be drawn is
	///   determined not by this class but by <see cref="Sgry.Azuki.ColorScheme"/> class.
	///   </para>
	///   <para>
	///   Please refer to document of
	///   <see cref="Sgry.Azuki.Marking"/> class
	///   for detail about the marking feature of Azuki.
	///   </para>
	/// </remarks>
	/// <seealso cref="Sgry.Azuki.Marking">MarkingInfo class</seealso>
	/// <seealso cref="Sgry.Azuki.ColorScheme">ColorScheme class</seealso>
	public class MarkingInfo
	{
		#region Fields
		int _ID;
		string _Name;
		MouseCursor _MouseCursor = MouseCursor.IBeam;
		#endregion

		#region Init / Dispose
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		/// <param name="id">ID of the marking to be registered.</param>
		/// <param name="name">Name of the marking.</param>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///   Parameter '<paramref name="id"/>' is out of valid range.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		///   Parameter '<paramref name="name"/>' is null.
		/// </exception>
		public MarkingInfo( int id, string name )
		{
			if( id < 0 || Marking.MaxID < id )
				throw new ArgumentOutOfRangeException( "id" );
			if( name == null )
				throw new ArgumentNullException( "name" );

			_ID = id;
			_Name = name;
		}

		/// <summary>
		/// Creates a new instance.
		/// </summary>
		/// <param name="id">ID of the marking to be registered.</param>
		/// <param name="name">Name of the marking.</param>
		/// <param name="cursor">
		///   This type of mouse cursor will be used
		///   when user puts cursor on the text with marking ID
		///   specified by <paramref name="id"/>.
		/// </param>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///   Parameter '<paramref name="id"/>' is out of valid range.
		/// </exception>
		/// <exception cref="System.ArgumentNullException">
		///   Parameter '<paramref name="name"/>' is null.
		/// </exception>
		public MarkingInfo( int id, string name, MouseCursor cursor )
			: this( id, name )
		{
			_MouseCursor = cursor;
		}
		#endregion

		#region Public properties
		/// <summary>
		/// Gets or sets ID of this marking.
		/// </summary>
		public int ID
		{
			get{ return _ID; }
			set{ _ID = value; }
		}

		/// <summary>
		/// Gets or sets name of this marking.
		/// </summary>
		public string Name
		{
			get{ return _Name; }
			set{ _Name = value; }
		}

		/// <summary>
		/// Gets or sets type of mouse cursor associated with this marking.
		/// </summary>
		public MouseCursor MouseCursor
		{
			get{ return _MouseCursor; }
			set{ _MouseCursor = value; }
		}
		#endregion
	}

	/// <summary>
	/// Manager of marking information
	/// which is to indicate attributes apart from syntax or grammar.
	/// </summary>
	/// <remarks>
	///   <para>
	///   The 'marking' feature is provided for putting
	///   additional (meta) information on text ranges
	///   which is not related to syntax or grammar of document type
	///   such as XML file or C/C++ source file.
	///   One of the typical usages is to mark (put meta information on)
	///   misspelled words as 'this word seems to be misspelled.'
	///   Another typical usage is to mark
	///   words at where compile error was detected as
	///   'compile error has been occurred here.'
	///   </para>
	///   <para>
	///   Multiple markings can be put on any text part independently.
	///   To mark up text parts or remove already marked IDs from text parts,
	///   use <see cref="Sgry.Azuki.Document.Mark">Document.Mark method</see>
	///   and <see cref="Sgry.Azuki.Document.Unmark(int, int, int)">Document.Unmark method</see>.
	///   Once a text part was marked, it will graphically be decorated
	///   as specified by <see cref="Sgry.Azuki.ColorScheme"/> class.
	///   To get or set how marked text will be decorated, use methods next.
	///   </para>
	///   <list type="bullet">
	///     <item>
	///       <see cref="Sgry.Azuki.ColorScheme.GetMarkingDecorations(int[])">
	///       ColorScheme.GetMarkingDecorations(int[]) method
	///   	  </see>
	///     </item>
	///     <item>
	///       <see cref="Sgry.Azuki.ColorScheme.SetMarkingDecoration">
	///       ColorScheme.SetMarkingDecoration method
	///       </see>
	///     </item>
	///   </list>
	///   <para>
	///   Internally, marking IDs set for each character are stored as bit mask
	///   (currently 32-bit).
	///   Although all operations can be done without minding it,
	///   in some cases, using internal bit mask directly
	///   is more efficient than using array of IDs.
	///   To handle bit mask directly, use
	///   <see cref="Sgry.Azuki.Document.GetMarkingBitMaskAt">
	///   Document.GetMarkingBitMaskAt method</see> and
	///   <see cref="Sgry.Azuki.ColorScheme.GetMarkingDecorations(uint)">
	///   ColorScheme.GetMarkingDecorations(uint) method</see>.
	///   </para>
	///   <para>
	///   Note that marking ID '31' is used by built-in URI marker to mark URIs.
	///   Although the meaning of ID 31 can be overwritten
	///   with <see cref="Sgry.Azuki.Marking.Register">Register</see> method,
	///   doing so is discouraged
	///   unless the programmer wants to create and use URI marker by his/her own.
	///   </para>
	/// </remarks>
	/// <seealso cref="Sgry.Azuki.MarkingInfo">MarkingInfo class</seealso>
	/// <seealso cref="Sgry.Azuki.ColorScheme">ColorScheme class</seealso>
	public static class Marking
	{
		#region Pulic constants and properties
		/// <summary>
		/// Maximum number of marking IDs currently supported.
		/// </summary>
		public const int MaxID = 31;

		/// <summary>
		/// ID of URI marking type.
		/// </summary>
		public static int Uri
		{
			get{ return 31; }
		}
		#endregion
		
		#region Fields
		static MarkingInfo[] _MarkingInfoAry = new MarkingInfo[MaxID+1];
		#endregion

		#region Init / Dispose
		static Marking()
		{
			Register( new MarkingInfo(Marking.Uri, "URI", MouseCursor.Hand) );
		}
		#endregion

		#region Operation
		/// <summary>
		/// Registers a marking ID.
		/// </summary>
		/// <param name="info">The information of the marking.</param>
		/// <remarks>
		///   <para>
		///   This method registers a marking ID and its information.
		///   If specified ID was already registered, existing information will be overwritten.
		///   </para>
		///   <para>
		///   Note that marking ID '31' is used by built-in URI marker to mark URIs.
		///   Although the meaning of ID 31 can be overwritten
		///   with this method,
		///   doing so is discouraged
		///   unless the programmer wants to create and use URI marker by his/her own.
		///   </para>
		/// </remarks>
		/// <exception cref="System.ArgumentNullException">
		///   Parameter <paramref name="info"/> is null.
		///	</exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///   ID of parameter <paramref name="info"/> is out of valid range.
		///	</exception>
		///	<seealso cref="Sgry.Azuki.Marking.Unregister">Marking.Unregister method</seealso>
		public static void Register( MarkingInfo info )
		{
			if( info == null )
				throw new ArgumentNullException( "info" );
			if( info.ID < 0 || MaxID < info.ID )
				throw new ArgumentOutOfRangeException( "Marking ID must be positive number and"
													   + " less than " + MaxID + "."
													   + " (info.ID:" + info.ID + ")" );

			_MarkingInfoAry[info.ID] = info;
		}

		/// <summary>
		/// Removes registation of a marking ID.
		/// </summary>
		/// <param name="id">The ID of the marking to be removed.</param>
		/// <remarks>
		///   <para>
		///   This method removes registration of a marking information.
		///   To register new marking information,
		///   use <see cref="Sgry.Azuki.Marking.Register">Register</see> method.
		///   </para>
		///   <para>
		///   This method cannot remove reserved marking IDs.
		///   </para>
		/// </remarks>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///   ID of parameter <paramref name="info"/> is out of valid range.
		///	</exception>
		///	<seealso cref="Sgry.Azuki.Marking.Register">Marking.Register method</seealso>
		public static void Unregister( int id )
		{
			if( id < 0 || MaxID < id )
				throw new ArgumentOutOfRangeException( "Marking ID must be positive number and"
													   + " less than " + MaxID + "."
													   + " (id:" + id + ")" );

			_MarkingInfoAry[id] = null;
		}

		/// <summary>
		/// Gets information about marking specified by ID.
		/// </summary>
		/// <returns>Information about specified marking.</returns>
		/// <exception cref="System.ArgumentOutOfRangeException">
		///   <paramref name="id"/> is out of valid range.
		/// </exception>
		public static MarkingInfo GetMarkingInfo( int id )
		{
			if( id < 0 || MaxID < id )
				throw new ArgumentOutOfRangeException( "Marking ID must be positive number and"
													   + " less than " + MaxID + "."
													   + " (id:" + id + ")" );

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
		#endregion
	}
}
