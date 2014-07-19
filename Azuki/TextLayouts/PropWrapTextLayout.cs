using System;
using System.Drawing;
using Debug = System.Diagnostics.Debug;

namespace Sgry.Azuki.TextLayouts
{
	class PropWrapTextLayout : TextLayoutBase
	{
		readonly PropWrapView _View;

		public PropWrapTextLayout( PropWrapView view )
		{
			_View = view;
		}

		public override int GetLineCount()
		{
			return _View.PLHI.Count;
		}
	}
}
