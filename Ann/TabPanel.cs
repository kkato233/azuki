// file: TabPanel.cs
// brief: simple tab control
// create: 2006-01-08 YAMAMOTO Suguru
// update: 2010-11-14 YAMAMOTO Suguru
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

			Size sizeOfX;

			// calculate unit text sizes
			using( IGraphics g = Plat.Inst.GetGraphics(this) )
			{
				g.Font = font;
				sizeOfX = g.MeasureText( "X" );
				_ElipsisWidth = g.MeasureText( "..." ).Width;
			}

			// update height if font height changed
			if( Height != sizeOfX.Height )
			{
				Height = (int)sizeOfX.Height + 5;
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
				ContextMenuStrip menu = new ContextMenuStrip();
				foreach( T item in _Items )
				{
					ToolStripMenuItem mi = new ToolStripMenuItem();
					mi.Text = item.ToString();
					mi.Click += ContextMenuItem_Click;
					menu.Items.Add( mi );
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
			Debug.Assert( sender is ToolStripMenuItem);

			// get the clicked item and select it
			ToolStripMenuItem mi = (ToolStripMenuItem)sender;
			int miIndex = mi.Owner.Items.IndexOf( mi );
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

			using( IGraphics g = Plat.Inst.GetGraphics(this) )
			{
				g.BeginPaint( e.ClipRectangle );

				// fill background
				g.ForeColor = _TabLineColor;
				g.BackColor = BackColor;
				g.FillRectangle( 0, 0, Width, Height );
				g.DrawLine( 0, Height-1, Width, Height-1 );

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
					size = g.MeasureText(
							text,
							_MaxTabTextWidth,
							out drawableLength
						);
					
					// if the label is too long to draw, cut it and add ellipsis
					if( drawableLength < text.Length )
					{
						// measure width of text attached the ellipsis
						size = g.MeasureText(
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
						g.ForeColor = _ActiveTabBackColor;
						g.DrawLine( left+1, Height-1, left+w+7, Height-1 );

						g.BackColor = _ActiveTabBackColor;
					}
					else
					{
						g.BackColor = _TabBackColor;
					}
					g.FillRectangle( left+1, Height-h-3, w+7, h+2 );
					g.FillRectangle( left+2, Height-h-4, w+4, 1 );

					// draw boundary
					g.ForeColor = _TabLineColor;
					g.DrawLine( left, Height-1, left, Height-h-2 ); // left
					g.DrawLine( left, Height-h-2, left+3, Height-h-5 ); // left-top
					g.DrawLine( left+3, Height-h-5, left+w+5, Height-h-5 ); // top
					g.DrawLine( left+w+5, Height-h-5, left+w+8, Height-h-2 ); // top-right
					g.DrawLine( left+w+8, Height-h-2, left+w+8, Height-1 ); // right

					// draw text on the tab
					Point pos = new Point( left+4, Height-size.Height-2 );
					g.DrawText( text, ref pos, _TabTextColor );

					// go to next tab
					_TabRightCoords[i] = left + w + 8;
					left += w + 8;
				}

				// draw line at left of triangle
				int half = _HalfHeight;
				int quarter = _QuarterHeight;
				int oneEighth = _OneEighthHeight;
				g.ForeColor = Color.Gray;
				g.DrawLine( Right-DropTipWidth, 0, Right-DropTipWidth, Height );

				// draw triangle
				g.ForeColor = Color.Black;
				for( int i=0; i<=oneEighth+1; i++ )
				{
					g.DrawLine(
							Right-half+i, Height-quarter-oneEighth+i,
							Right-quarter-i+1, Height-quarter-oneEighth+i
						);
				}

				g.EndPaint();
			}
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
