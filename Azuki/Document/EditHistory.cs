// file: EditHistory.cs
// brief: History managemer for UNDO.
// author: YAMAMOTO Suguru
// encoding: UTF-8
// update: 2008-05-31
//=========================================================
using System;

namespace Sgry.Azuki
{
	class EditHistory
	{
		#region Fields
		const int GrowSize = 32;
		EditAction[] _Stack;
		int _Capacity = 32;
		int _NextIndex = 0;
//		int _MaximumCount = 2;
		#endregion

		#region Init / Dispose
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public EditHistory()
		{
			_Stack = new EditAction[ _Capacity ];
		}
		#endregion

		/// <summary>
		/// Pushes new action to the stack.
		/// </summary>
		public void Add( EditAction action )
		{
			// if there is no more space, expand buffer
			if( _Capacity <= _NextIndex )
			{
				Utl.ResizeArray( ref _Stack, _Capacity + GrowSize );
				_Capacity = _Capacity + GrowSize;
			}

			// stack up this action
			_Stack[ _NextIndex ] = action;
			_NextIndex++;
			if( _NextIndex < _Stack.Length )
			{
				_Stack[ _NextIndex ] = null;
			}
		}

		/// <summary>
		/// Gets the action most recently done and remove it from stack.
		/// </summary>
		public EditAction GetUndoAction()
		{
			if( _NextIndex <= 0 )
				return null;

			// return an action which is on top logically of the stack
			_NextIndex--;
			return _Stack[ _NextIndex ];
		}

		/// <summary>
		/// Gets the action most recently done and remove it from stack.
		/// </summary>
		public EditAction GetRedoAction()
		{
			if( _NextIndex < _Stack.Length
				&& _Stack[_NextIndex] != null )
			{
				EditAction redoAction = _Stack[ _NextIndex ];
				_NextIndex++;
				return redoAction;
			}

			return null;
		}

		/// <summary>
		/// Clears all containing actions.
		/// </summary>
		public void Clear()
		{
			_NextIndex = 0;
			_Stack[0] = null;
		}

		/// <summary>
		/// Whether an available UNDO action exists or not.
		/// </summary>
		public bool CanUndo
		{
			get{ return (0 < _NextIndex); }
		}

		/// <summary>
		/// Whether an available REDO action exists or not.
		/// </summary>
		public bool CanRedo
		{
			get
			{
				return (_NextIndex < _Stack.Length
					&& _Stack[_NextIndex] != null);
			}
		}

		#region Utilities
		static class Utl
		{
			public static void ResizeArray<T>( ref T[] array, int newSize )
			{
				T[] value = new T[ newSize ];
				int minSize = Math.Min( array.Length, newSize );
				
				if( 0 < minSize )
				{
					Array.Copy( array, value, minSize );
				}

				array = value;
			}
		}
		#endregion
	}
}
