using System;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Windows.Forms;
using System.Reflection;
using System.Collections.Specialized;
using F23Bag.AutomaticUI;
using F23Bag.AutomaticUI.Layouts;

namespace F23Bag.Winforms.Controls
{
    public partial class DataGridControl : DataControl
    {
        private readonly Layout _dataGridLayout;
        private readonly MethodInfo _openAction;
        private ListSortDirection? _sortDirection;

        public DataGridControl(Layout dataGridLayout, WinformContext context, MethodInfo openAction)
            : base(dataGridLayout, context)
        {
            InitializeComponent();

            _dataGridLayout = dataGridLayout;
            _openAction = openAction;

            gridView.RowHeadersVisible = false;
            gridView.AutoGenerateColumns = false;
            gridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            gridView.BackgroundColor = cstBackColor;
            gridView.ColumnHeadersDefaultCellStyle.BackColor = cstForeColor;
            gridView.ColumnHeadersDefaultCellStyle.ForeColor = cstBackColor;
            gridView.EnableHeadersVisualStyles = false;

            gridView.CellDoubleClick += GridView_CellDoubleClick;
            gridView.CellClick += GridView_CellClick;
            gridView.KeyUp += GridView_KeyUp;
        }

        public PropertyInfo Property
        {
            get { return (PropertyInfo)DisplayedMember; }
            set { DisplayedMember = value; }
        }

        public string Label { get; set; }

        public void AddColumn(PropertyInfo property, bool isEditable, string label)
        {
            gridView.Columns.Add(new DataGridViewColumn()
            {
                ValueType = property.PropertyType,
                DataPropertyName = property.Name,
                CellTemplate = new DataGridViewTextBoxCell(),
                ReadOnly = true,
                HeaderText = Context.I18n.GetTranslation(label),
                Tag = property,
                SortMode = DataGridViewColumnSortMode.Automatic
            });
        }

        public void AddColumn(MethodInfo method, string label)
        {
            gridView.Columns.Add(new DataGridViewLinkColumn()
            {
                UseColumnTextForLinkValue = true,
                Text = Context.I18n.GetTranslation(label),
                TrackVisitedState = false,
                Tag = method
            });
        }

        public void AddAction(OneMemberLayout layout, MethodInfo method, string label)
        {
            flowActions.Controls.Add(new MethodCallControl(layout, Context, method, false, label));
        }

        protected override void CustomDisplay(object data)
        {
            Visible = true;
            var authorization = Context.Engine.GetAuthorization(data.GetType());
            foreach (var column in gridView.Columns.OfType<DataGridViewColumn>())
            {
                column.Visible = authorization.IsVisible(data, (MemberInfo)column.Tag);
                if (column.Tag is PropertyInfo) column.ReadOnly = column.ReadOnly || authorization.IsEditable(data, (PropertyInfo)column.Tag);
            }

            foreach (var ctrl in flowActions.Controls.OfType<DataControl>())
                ctrl.Display(new Func<System.Collections.IEnumerable>(() => gridView.SelectedRows.OfType<DataGridViewRow>().Select(r => r.DataBoundItem).ToList()));
            flowActions.Visible = flowActions.Controls.OfType<DataControl>().Any(c => c.Visible);

            BindCollection(data);

            if (data is INotifyPropertyChanged)
            {
                ((INotifyPropertyChanged)data).PropertyChanged -= GridControl_PropertyChanged;
                ((INotifyPropertyChanged)data).PropertyChanged += GridControl_PropertyChanged;
            }
        }

        private void GridControl_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Property.Name)
            {
                BindCollection(sender);
            }
        }

        private void BindCollection(object data)
        {
            var collection = Property.GetValue(data);
            if (collection is INotifyCollectionChanged)
            {
                ((INotifyCollectionChanged)collection).CollectionChanged -= GridControl_CollectionChanged;
                ((INotifyCollectionChanged)collection).CollectionChanged += GridControl_CollectionChanged;
            }

            if (collection != null)
            {
                gridView.DataSource = new BindingSource() { DataSource = collection, AllowNew = false };
                BindPropertyChanged(collection);
            }
        }

        private void BindPropertyChanged(object collection)
        {
            foreach (var elt in (System.Collections.IEnumerable)collection)
                if (elt is INotifyPropertyChanged)
                {
                    ((INotifyPropertyChanged)elt).PropertyChanged -= DataGridControl_PropertyChanged;
                    ((INotifyPropertyChanged)elt).PropertyChanged += DataGridControl_PropertyChanged;
                }
        }

        private void GridView_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && gridView.SelectedRows.Count == 1)
            {
                DisplayData(gridView.SelectedRows[0].DataBoundItem);
                e.SuppressKeyPress = true;
            }
        }

        private void GridView_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var data = gridView.Rows[e.RowIndex].DataBoundItem;

            if (_openAction == null)
                DisplayData(data);
            else
                MethodCallControl.CallMethod(_dataGridLayout, data, MethodCallControl.AskParameters(_dataGridLayout, _openAction, Context), _openAction, false, Context);
        }

        private void DisplayData(object data)
        {
            Context.UIBuilder.Display(_dataGridLayout.GetCreateUpdateLayout(data.GetType()), data, data.ToString());
        }

        private void GridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex == -1 && gridView.Columns[e.ColumnIndex].Tag is PropertyInfo)
            {
                var sortableCollection = ((BindingSource)gridView.DataSource).DataSource as IQueryable;
                if (sortableCollection != null)
                {
                    _sortDirection = _sortDirection == ListSortDirection.Ascending || !_sortDirection.HasValue ? ListSortDirection.Descending : ListSortDirection.Ascending;
                    var methodName = "OrderBy";
                    if (_sortDirection == ListSortDirection.Descending) methodName = "OrderByDescending";

                    var entityType = sortableCollection.GetType().GetGenericArguments()[0];
                    var columnProperty = (PropertyInfo)gridView.Columns[e.ColumnIndex].Tag;
                    var o = Expression.Parameter(entityType);
                    ((BindingSource)gridView.DataSource).DataSource = typeof(Queryable).GetMethods().First(m => m.Name == methodName && m.GetParameters().Length == 2)
                        .MakeGenericMethod(entityType, columnProperty.PropertyType)
                        .Invoke(null, new object[] { sortableCollection, Expression.Lambda(Expression.MakeMemberAccess(o, columnProperty), o) });
                }
            }
            else if (e.RowIndex > -1 && e.ColumnIndex > -1 && gridView.Columns[e.ColumnIndex].Tag is MethodInfo)
            {
                var method = (MethodInfo)gridView.Columns[e.ColumnIndex].Tag;
                MethodCallControl.CallMethod(_dataGridLayout, gridView.Rows[e.RowIndex].DataBoundItem, MethodCallControl.AskParameters(_dataGridLayout, method, Context), method, false, Context);
            }
        }

        private void GridControl_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Action act = () =>
            {
                ((BindingSource)gridView.DataSource).ResetBindings(true);
                ((BindingSource)gridView.DataSource).CurrencyManager.Refresh();
                BindPropertyChanged(((BindingSource)gridView.DataSource).DataSource);
            };

            if (InvokeRequired)
                Invoke(act);
            else
                act();
        }

        private void DataGridControl_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Action act = () =>
            {
                ((BindingSource)gridView.DataSource).ResetBindings(true);
                ((BindingSource)gridView.DataSource).CurrencyManager.Refresh();
            };

            if (InvokeRequired)
                Invoke(act);
            else
                act();
        }
    }
}
