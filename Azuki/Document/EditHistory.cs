// file: EditHistory.cs
// brief: History managemer for UNDO.
// author: YAMAMOTO Suguru
// encoding: UTF-8
// update: 2011-05-04
//=========================================================
using System;

namespace Sgry.Azuki
{
	/// <summary>
	/// Stack object to hold editing actions.
	/// </summary>
	class EditHistory
	{
		#region Fields
		const int GrowSize = 32;
		EditAction[] _Stack;
		int _NextIndex = 0;
		EditAction _GroupingUndoChain = null;
		EditAction _LastSavedAction = null;
		#endregion

		#region Init / Dispose
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public EditHistory()
		{
			_Stack = new EditAction[ GrowSize ];
		}
		#endregion

		#region Operations
		/// <summary>
		/// Pushes new action to the stack.
		/// </summary>
		public void Add( EditAction action )
		{
			if( _GroupingUndoChain != null )
			{
				// put given action to the head of the chain
				action.Next = _GroupingUndoChain;
				_GroupingUndoChain = action;
			}
			else
			{
				// if there is no more space, expand buffer
				if( _Stack.Length <= _NextIndex )
				{
					Utl.ResizeArray( ref _Stack, _Stack.Length + GrowSize );
				}
	
				// stack up this action
				_Stack[ _NextIndex ] = action;
				_NextIndex++;
				if( _NextIndex < _Stack.Length )
				{
					_Stack[ _NextIndex ] = null;
				}
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

			// (all references must be nullified to allow GC collecting them)
			for( int i=0; i<_Stack.Length; i++ )
			{
				_Stack[i] = null;
			}
		}

		/// <summary>
		/// Begins grouping up editing actions into a single UNDO action.
		/// </summary>
		public void BeginUndo()
		{
			if( _GroupingUndoChain == null )
			{
				_GroupingUndoChain = new EditAction( null, 0, null, null, 0, 0, null );
			}
		}

		/// <summary>
		/// Ends grouping up editing actions.
		/// </summary>
		public void EndUndo()
		{
			if( _GroupingUndoChain != null )
			{
				EditAction groupedAction = _GroupingUndoChain;
				_GroupingUndoChain = null; // nullify this, otherwise Add() adds it to the end of the chain again
				Add( groupedAction );
			}
		}

		public bool IsSavedState
		{
			get
			{
				// if Azuki is grouping one or more actions,
				// it is safe to say the state must change
				// regardless whether current (before concluding grouped UNDO) state
				// is saved-state or not.
				if( _GroupingUndoChain != null && _GroupingUndoChain.Next != null )
				{
					return false;
				}

				if( 0 < _NextIndex )
				{
					return (_LastSavedAction == _Stack[_NextIndex-1]);
				}
				else
				{
					return (_LastSavedAction == null);
				}
			}
		}

		public void SetSavedState()
		{
			if( 0 < _NextIndex )
			{
				_LastSavedAction = _Stack[_NextIndex-1];
			}
			else
			{
				_LastSavedAction = null;
			}
		}
		#endregion

		#region Properties
		/// <summary>
		/// Whether an available UNDO action exists or not.
		/// </summary>
		public bool CanUndo
		{
			get
			{
				if( 0 < _NextIndex )
				{
					// UNDOable action exists
					return true;
				}
				else if( IsGroupingActions )
				{
					// group UNDO is going on
					// so there will be an UNDOable action when it ends
					return true;
				}
				return false;
			}
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

		/// <summary>
		/// Whether group UNDO/REDO is executing or not.
		/// </summary>
		public bool IsGroupingActions
		{
			get{ return (_GroupingUndoChain != null); }
		}

		/// <summary>
		/// Gets estimated memory size used by this object.
		/// </summary>
		public int MemoryUsage
		{
			get
			{
				int usage = 0;

				foreach( EditAction action in _Stack )
				{
					if( action != null )
					{
						usage += action.InsertedText.Length * sizeof(char);
						usage += action.DeletedText.Length * sizeof(char);
					}
				}

				return usage;
			}
		}
		#endregion

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
