using System;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using System.Reflection;
using F23Bag.AutomaticUI;
using F23Bag.AutomaticUI.Layouts;

namespace F23Bag.Winforms.Controls
{
    public partial class TableControl : DataControl
    {
        public TableControl(Layout layout, WinformContext context)
            : base(layout, context)
        {
            InitializeComponent();
        }

        public string Label { get; set; }

        public PropertyInfo Property
        {
            get { return (PropertyInfo)DisplayedMember; }
            set { DisplayedMember = value; }
        }

        public void AddControl(DataControl ctrl, int column, int row, int columnSpan, int rowSpan)
        {
            if (column >= tableLayout.ColumnCount) tableLayout.ColumnCount = column + 1;
            if (row >= tableLayout.RowCount) tableLayout.RowCount = row + 1;

            tableLayout.Controls.Add(ctrl);
            tableLayout.SetCellPosition(ctrl, new TableLayoutPanelCellPosition(column, row));
            tableLayout.SetColumnSpan(ctrl, columnSpan);
            tableLayout.SetRowSpan(ctrl, rowSpan);
        }

        protected override void CustomDisplay(object data)
        {
            var oldData = data;
            if (Property != null)
            {
                data = Property.GetValue(data);
                lblTitle.Text = Context.I18n.GetTranslation(Label);
                if (data == null) Property.SetValue(oldData, data = Activator.CreateInstance(Property.PropertyType));
            }

            if (Property == null)
            {
                lblTitle.Text = data.ToString();
                lblTitle.Visible = Parent is Form;
            }

            foreach (var ctrl in tableLayout.Controls.OfType<DataControl>())
            {
                ctrl.Visible = true;
                ctrl.Display(data);
            }
        }

        private void TableControl_Load(object sender, EventArgs e)
        {
            var height = 30;
            var width = 20;
            tableLayout.ColumnStyles.Clear();
            for (var i = 0; i < tableLayout.ColumnCount; i++)
            {
                var max = tableLayout.Controls.OfType<DataControl>().Where(c => tableLayout.GetColumn(c) == i && tableLayout.GetColumnSpan(c) == 1).Max(c => c.Width);
                tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, max));
                width += max;
            }
            tableLayout.RowStyles.Clear();
            for (var i = 0; i < tableLayout.RowCount; i++)
            {
                var max = tableLayout.Controls.OfType<DataControl>().Where(c => tableLayout.GetRow(c) == i && tableLayout.GetRowSpan(c) == 1).Max(c => c.Height);
                tableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, max));
                height += max;
            }
            tableLayout.Refresh();
            ClientSize = new Size(width, height);
            if (Parent is Form) Parent.ClientSize = new Size(ClientSize.Width + 20, ClientSize.Height + 20);

            foreach (var ctrl in tableLayout.Controls.OfType<Control>()) ctrl.Dock = DockStyle.Fill;
        }
    }
}
