using System;

namespace F23Bag.AutomaticUI.Layouts
{
    /// <summary>
    /// A grid cell definition.
    /// </summary>
    public class LayoutCellPosition
    {
        public LayoutCellPosition(Layout layout, int column, int row, int colSpan, int rowSpan)
        {
            if (layout == null) throw new ArgumentNullException(nameof(layout));

            Layout = layout;
            Column = column;
            Row = row;
            ColumnSpan = colSpan;
            RowSpan = rowSpan;
        }

        public int ColumnSpan { get; private set; }

        public int Column { get; private set; }

        public Layout Layout { get; private set; }

        public int Row { get; private set; }

        public int RowSpan { get; private set; }
    }
}