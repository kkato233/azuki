// file: TabPanel.cs
// brief: simple tab control
// create: 2006-01-08 YAMAMOTO Suguru
// update: 2010-07-13 YAMAMOTO Suguru
//=========================================================
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;

namespace Sgry.Azuki
{
	/// <summary>
	/// A simple tab control.
	/// </summary>
	class TabPanel<T> : Panel
	{
		#region Fields
		IList<T> _Items = new T[0];
		IGraphics _Gra;
		int[] _TabRightCoords = null;
		int _MaxTabTextWidth = 140;
		int _ElipsisWidth;
		Color _TabTextColor;
		Color _TabBackColor;
		Color _ActiveTabBackColor;
		Color _TabLineColor;
		Font _TabTextFont;
		T _SelectedItem;
		int _HalfHeight;
		int _QuarterHeight;
		int _OneEighthHeight;
		#endregion

		#region Init / Dispose
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public TabPanel()
		{
			_Gra = Plat.Inst.GetGraphics( this );
			TabTextColor = Color.Black;
			TabBackColor = Color.LightGray;
			BackColor = Color.LightGray;
			ActiveTabBackColor = Color.White;
			TabLineColor = Color.Gray;
			_TabTextFont = new Font( FontFamily.GenericSansSerif, 10, FontStyle.Regular );
			SetFont( _TabTextFont );
		}
		#endregion

		#region Appearence
		/// <summary>
		/// Gets or sets color of text on tabs.
		/// </summary>
		public Color TabTextColor
		{
			get{ return _TabTextColor; }
			set{ _TabTextColor = value; }
		}

		/// <summary>
		/// Gets or sets background color of tab area and inactive tabs.
		/// </summary>
		public Color TabBackColor
		{
			get{ return _TabBackColor; }
			set{ _TabBackColor = value; }
		}

		/// <summary>
		/// Gets or sets background color of active tab.
		/// </summary>
		public Color ActiveTabBackColor
		{
			get{ return _ActiveTabBackColor; }
			set{ _ActiveTabBackColor = value; }
		}

		/// <summary>
		/// Gets or sets color of tab graphic borders.
		/// </summary>
		public Color TabLineColor
		{
			get{ return _TabLineColor; }
			set{ _TabLineColor = value; }
		}

		/// <summary>
		/// Sets or gets font of text on tabs.
		/// </summary>
		public override Font Font
		{
			get{ return _TabTextFont; }
			set
			{
				_TabTextFont = value;
				SetFont( _TabTextFont );
			}
		}

		/// <summary>
		/// Sets font of text on tabs.
		/// </summary>
		void SetFont( Font font )
		{
			if( font == null )
				throw new ArgumentNullException( "font" );

			// calculate unit text sizes
			_Gra.Font = font;
			Size size = _Gra.MeasureText( "X" );
			_ElipsisWidth = _Gra.MeasureText( "..." ).Width;

			// update height if font height changed
			if( Height != size.Height )
			{
				Height = (int)size.Height + 5;
			}

			// update other metrics for drawing
			_HalfHeight = Height >> 1;
			_QuarterHeight = Height >> 2;
			_OneEighthHeight = Height >> 3;
		}
		#endregion

		#region Operations
		/// <summary>
		/// Gets or sets the items to be shown in this tab control.
		/// </summary>
		public IList<T> Items
		{
			get{ return _Items; }
			set{ _Items = value; }
		}

		/// <summary>
		/// Gets the item associated with selected tab.
		/// </summary>
		public T SelectedItem
		{
			get{ return _SelectedItem; }
			set
			{
				if( _Items.Contains(value) == false )
					throw new ArgumentException( "There is no tab associated with given item." );

				_SelectedItem = value;
				Invalidate();
			}
		}

		/// <summary>
		/// Gets whether there is a tab associated with specified item or not.
		/// </summary>
		public bool Contains( T item )
		{
			return _Items.Contains( item );
		}

		/// <summary>
		/// Selects next tab.
		/// </summary>
		public void SelectNextTab()
		{
			if( _Items.Count == 0 )
				return;

			// calculate index of tab to be selected
			int index = _Items.IndexOf( _SelectedItem );
			if( index+1 < _Items.Count )
				index++;
			else
				index = 0;
			_SelectedItem = _Items[index];

			// invoke selection event
			if( TabSelected != null )
			{
				TabSelected(
						new MouseEventArgs(MouseButtons.Left, 0, 0, 0, 0),
						_SelectedItem
					);
			}
		}

		/// <summary>
		/// Selects previous tab.
		/// </summary>
		public void SelectPreviousTab()
		{
			if( _Items.Count == 0 )
				return;

			// calculates index of tab to be selected
			int index = _Items.IndexOf( _SelectedItem );
			if( 0 < index )
				index--;
			else
				index = _Items.Count - 1;
			_SelectedItem = _Items[index];

			// invoke selection event
			if( TabSelected != null )
			{
				TabSelected( new MouseEventArgs(MouseButtons.Left, 0, 0, 0, 0), _SelectedItem );
			}
		}
		#endregion

		#region Events
		/// <summary>
		/// Occures when a tab was selected.
		/// </summary>
		public event TabSelectedEventHandler TabSelected;
		
		/// <summary>
		/// Delegate type for TabSelected event.
		/// </summary>
		public delegate void TabSelectedEventHandler( MouseEventArgs e, T item );
		#endregion

		#region Event Handlers
		protected override void OnResize( EventArgs e )
		{
			base.OnResize( e );
			Invalidate();
		}

		protected override void OnMouseDown( MouseEventArgs e )
		{
			base.OnMouseDown( e );

			// if triangle at the right-end was clicked, show document list and exit
			if( Right-_HalfHeight-_QuarterHeight < e.X )
			{
				ContextMenu menu= new ContextMenu();
				foreach( T item in _Items )
				{
					MenuItem mi = new MenuItem();
					mi.Text = item.ToString();
					mi.Click += ContextMenuItem_Click;
					menu.MenuItems.Add( mi );
				}
				Point menuPos = new Point();
				menuPos.X = Right-_HalfHeight-_QuarterHeight;
				menuPos.Y = Height;
				menu.Show( this, menuPos );
				return;
			}

			// find out which tab was clicked by x-coord of click position
			for( int i=0; i<_Items.Count; i++ )
			{
				if( e.X <= _TabRightCoords[i] )
				{
					//--- found ---
					// invoke event and exit
					if( TabSelected != null )
					{
						TabSelected( e, _Items[i] );
					}
					break;
				}
			}
		}

		void ContextMenuItem_Click( object sender, EventArgs e )
		{
			Debug.Assert( sender is MenuItem );
			
			// get the clicked item and select it
			MenuItem mi = (MenuItem)sender;
			int miIndex = mi.Parent.MenuItems.IndexOf( mi );
			this.SelectedItem = _Items[miIndex];

			// invoke click event
			if( TabSelected != null )
			{
				TabSelected(
					new MouseEventArgs(MouseButtons.Left, 0, 0, 0, 0),
					this.SelectedItem
				);
			}
		}
		#endregion

		#region Drawing
		protected override void OnPaintBackground( PaintEventArgs e )
		{
			//DO_NOT//base.OnPaintBackground( e );
		}

		protected override void OnPaint( PaintEventArgs e )
		{
			/*
			   ********************
			  *                    *
			 *                      *
			*  +------------------+  *
			*  |    /|   /|   /|  |  *
			*  |   /-|  /-|  /-|  |  *
			*  |  /  | /  | /  |  |  *
			*  +------------------+  *
			*                        *
			**************************
			
			*/
			int left = 6;
			Size size;
			int drawableLength;

			_Gra.BeginPaint( e.ClipRectangle );

			// fill background
			_Gra.ForeColor = _TabLineColor;
			_Gra.BackColor = BackColor;
			_Gra.FillRectangle( 0, 0, Width, Height );
			_Gra.DrawLine( 0, Height-1, Width, Height-1 );

			// ensure capacity of x-coordinate array
			if( _TabRightCoords == null
				|| _TabRightCoords.Length < _Items.Count )
			{
				_TabRightCoords = new int[_Items.Count + 8]; // add little extra
			}

			// draw each tabs
			for( int i=0; i<_Items.Count; i++ )
			{
				// make label text
				string text = (string)_Items[i].ToString();
				size = _Gra.MeasureText(
						text,
						_MaxTabTextWidth,
						out drawableLength
					);
				
				// if the label is too long to draw, cut it and add ellipsis
				if( drawableLength < text.Length )
				{
					// measure width of text attached the ellipsis
					size = _Gra.MeasureText(
							text,
							_MaxTabTextWidth - _ElipsisWidth,
							out drawableLength
						);
					text = text.Substring( 0, drawableLength ) + "...";
					size.Width += _ElipsisWidth;
				}

				// if right end of the tab exceeds limit, stop drawing
				if( Right - DropTipWidth <= left + size.Width+8 )
				{
					break;
				}

				// draw background of this tab
				int w = size.Width;
				int h = size.Height;
				if( _Items[i].Equals(_SelectedItem) )
				{
					// for selected tab, before drawing background
					// draw a line to erase border between content area
					_Gra.ForeColor = _ActiveTabBackColor;
					_Gra.DrawLine( left+1, Height-1, left+w+7, Height-1 );

					_Gra.BackColor = _ActiveTabBackColor;
				}
				else
				{
					_Gra.BackColor = _TabBackColor;
				}
				_Gra.FillRectangle( left+1, Height-h-3, w+7, h+2 );
				_Gra.FillRectangle( left+2, Height-h-4, w+4, 1 );

				// draw boundary
				_Gra.ForeColor = _TabLineColor;
				_Gra.DrawLine( left, Height-1, left, Height-h-2 ); // left
				_Gra.DrawLine( left, Height-h-2, left+3, Height-h-5 ); // left-top
				_Gra.DrawLine( left+3, Height-h-5, left+w+5, Height-h-5 ); // top
				_Gra.DrawLine( left+w+5, Height-h-5, left+w+8, Height-h-2 ); // top-right
				_Gra.DrawLine( left+w+8, Height-h-2, left+w+8, Height-1 ); // right

				// draw text on the tab
				Point pos = new Point( left+4, Height-size.Height-2 );
				_Gra.DrawText( text, ref pos, _TabTextColor );

				// go to next tab
				_TabRightCoords[i] = left + w + 8;
				left += w + 8;
			}

			// draw line at left of triangle
			int half = _HalfHeight;
			int quarter = _QuarterHeight;
			int oneEighth = _OneEighthHeight;
			_Gra.ForeColor = Color.Gray;
			_Gra.DrawLine( Right-DropTipWidth, 0, Right-DropTipWidth, Height );

			// draw triangle
			_Gra.ForeColor = Color.Black;
			for( int i=0; i<=oneEighth+1; i++ )
			{
				_Gra.DrawLine(
						Right-half+i, Height-quarter-oneEighth+i,
						Right-quarter-i+1, Height-quarter-oneEighth+i
					);
			}

			_Gra.EndPaint();
		}
		#endregion

		#region Utilities
		int DropTipWidth
		{
			get{ return _HalfHeight + _QuarterHeight; }
		}
		#endregion
	}
}
