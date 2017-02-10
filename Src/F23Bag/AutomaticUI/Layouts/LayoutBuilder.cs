using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace F23Bag.AutomaticUI.Layouts
{
    /// <summary>
    /// Utilities for fluent build of layout.
    /// </summary>
    /// <typeparam name="TData">Type corresponding to the layout.</typeparam>
    public class LayoutBuilder<TData>
    {
        private readonly List<Func<Layout>> _getLayouts = new List<Func<Layout>>();
        private readonly IEnumerable<ILayoutProvider> _layoutProviders;
        private readonly Type _dataType;
        private readonly Dictionary<string, object> _options;

        public LayoutBuilder(Type dataType, IEnumerable<ILayoutProvider> layoutProviders, Dictionary<string, object> options)
        {
            _dataType = dataType;
            _layoutProviders = layoutProviders;
            _options = options;
        }

        public IEnumerable<Layout> GetLayouts()
        {
            return _getLayouts.Select(gl =>
            {
                var layout = gl();
                layout.Options = _options;
                return layout;
            });
        }

        public LayoutBuilder<TData> Grid(Action<GridLayoutBuilder> defineGrid)
        {
            var glb = new GridLayoutBuilder(_dataType, _layoutProviders, _options);
            _getLayouts.Add(glb.GetLayout);
            defineGrid(glb);
            return this;
        }

        public LayoutBuilder<TData> Horizontal(Action<LayoutBuilder<TData>> defineFlow)
        {
            var layoutBuilder = new LayoutBuilder<TData>(_dataType, _layoutProviders, _options);
            _getLayouts.Add(() => new FlowLayout(_layoutProviders, FlowDirectionEnum.Horizontal, layoutBuilder.GetLayouts()));
            defineFlow(layoutBuilder);
            return this;
        }

        public LayoutBuilder<TData> Vertical(Action<LayoutBuilder<TData>> defineFlow)
        {
            var layoutBuilder = new LayoutBuilder<TData>(_dataType, _layoutProviders, _options);
            _getLayouts.Add(() => new FlowLayout(_layoutProviders, FlowDirectionEnum.Vertical, layoutBuilder.GetLayouts()));
            defineFlow(layoutBuilder);
            return this;
        }

        public LayoutBuilder<TData> DataGrid(Action<DataGridLayoutBuilder> defineDataGrid)
        {
            var dglb = new DataGridLayoutBuilder(_dataType, _layoutProviders, _options);
            _getLayouts.Add(dglb.GetLayout);
            defineDataGrid(dglb);
            return this;
        }

        public LayoutBuilder<TData> Tabs(Action<TabsLayoutBuilder> defineTabs)
        {
            var tlb = new TabsLayoutBuilder(_dataType, _layoutProviders, _options);
            _getLayouts.Add(tlb.GetLayout);
            defineTabs(tlb);
            return this;
        }

        public LayoutBuilder<TData> Tree(Action<TreeLayoutbuilder> defineTree)
        {
            var tlb = new TreeLayoutbuilder(_dataType, _layoutProviders);
            _getLayouts.Add(tlb.GetLayout);
            defineTree(tlb);
            return this;
        }

        public LayoutBuilder<TData> Property(Expression<Func<TData, object>> property, string label, Expression<Func<TData, System.Collections.IEnumerable>> itemsSource)
        {
            if (property == null) throw new ArgumentNullException(nameof(property));

            var mbAccess = property.Body as MemberExpression ?? (property.Body as UnaryExpression)?.Operand as MemberExpression;
            if (mbAccess == null) throw new ArgumentException("property must be a property access expression.", nameof(property));

            MemberInfo itemsSourceMb = null;
            if (itemsSource != null)
            {
                var itemsMbAccess = itemsSource.Body as MemberExpression;
                if (itemsMbAccess == null) throw new ArgumentException("itemsSource must be a property access expression.", nameof(itemsSource));
                itemsSourceMb = itemsMbAccess.Member;
            }

            _getLayouts.Add(() => new OneMemberLayout(_layoutProviders, mbAccess.Member, false, label, itemsSourceMb));

            return this;
        }

        public LayoutBuilder<TData> Property(Expression<Func<TData, object>> property, string label)
        {
            return Property(property, label, null);
        }

        public LayoutBuilder<TData> Property(Expression<Func<TData, object>> property)
        {
            return Property(property, null, null);
        }

        public LayoutBuilder<TData> Method(Expression<Func<TData, Delegate>> method, string label, bool hasCloseBehavior)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));

            var methodInfo = (((method.Body as UnaryExpression)?.Operand as MethodCallExpression)?.Object as ConstantExpression).Value as MethodInfo;
            if (methodInfo == null) throw new ArgumentException("method must be a delegate instanciation.", nameof(method));

            _getLayouts.Add(() => new OneMemberLayout(_layoutProviders, methodInfo, hasCloseBehavior, label, null));

            return this;
        }

        public LayoutBuilder<TData> Method(Expression<Func<TData, Delegate>> method, string label)
        {
            return Method(method, label, false);
        }

        public LayoutBuilder<TData> Method(Expression<Func<TData, Delegate>> method)
        {
            return Method(method, null, false);
        }

        public LayoutBuilder<TData> Property<TInheritsData>(Expression<Func<TInheritsData, object>> property, string label, Expression<Func<TInheritsData, System.Collections.IEnumerable>> itemsSource)
            where TInheritsData : TData
        {
            if (!typeof(TInheritsData).IsAssignableFrom(_dataType)) return this;
            if (property == null) throw new ArgumentNullException(nameof(property));

            var mbAccess = property.Body as MemberExpression ?? (property.Body as UnaryExpression)?.Operand as MemberExpression;
            if (mbAccess == null) throw new ArgumentException("property must be a property access expression.", nameof(property));

            MemberInfo itemsSourceMb = null;
            if (itemsSource != null)
            {
                var itemsMbAccess = itemsSource.Body as MemberExpression;
                if (itemsMbAccess == null) throw new ArgumentException("itemsSource must be a property access expression.", nameof(itemsSource));
                itemsSourceMb = itemsMbAccess.Member;
            }

            _getLayouts.Add(() => new OneMemberLayout(_layoutProviders, mbAccess.Member, false, label, itemsSourceMb));

            return this;
        }

        public LayoutBuilder<TData> Property<TInheritsData>(Expression<Func<TInheritsData, object>> property, string label)
            where TInheritsData : TData
        {
            return Property(property, label, null);
        }

        public LayoutBuilder<TData> Property<TInheritsData>(Expression<Func<TInheritsData, object>> property)
            where TInheritsData : TData
        {
            return Property(property, null, null);
        }

        public LayoutBuilder<TData> Method<TInheritsData>(Expression<Func<TInheritsData, Delegate>> method, string label, bool hasCloseBehavior)
            where TInheritsData : TData
        {
            if (!typeof(TInheritsData).IsAssignableFrom(_dataType)) return this;
            if (method == null) throw new ArgumentNullException(nameof(method));

            var methodInfo = (((method.Body as UnaryExpression)?.Operand as MethodCallExpression)?.Object as ConstantExpression).Value as MethodInfo;
            if (methodInfo == null) throw new ArgumentException("method must be a delegate instanciation.", nameof(method));

            _getLayouts.Add(() => new OneMemberLayout(_layoutProviders, methodInfo, hasCloseBehavior, label, null));

            return this;
        }

        public LayoutBuilder<TData> Method<TInheritsData>(Expression<Func<TInheritsData, Delegate>> method, string label)
            where TInheritsData : TData
        {
            return Method(method, label, false);
        }

        public LayoutBuilder<TData> Method<TInheritsData>(Expression<Func<TInheritsData, Delegate>> method)
            where TInheritsData : TData
        {
            return Method(method, null, false);
        }

        public LayoutBuilder<TData> If(bool condition, Action defineLayout)
        {
            if (condition) defineLayout();
            return this;
        }

        internal Layout GetLastLayout()
        {
            return GetLayouts().Last();
        }

        public class GridLayoutBuilder
        {
            private readonly List<Func<LayoutCellPosition>> _getCells;
            private readonly IEnumerable<ILayoutProvider> _layoutProviders;
            private readonly Type _dataType;
            private readonly Dictionary<string, object> _options;

            internal GridLayoutBuilder(Type dataType, IEnumerable<ILayoutProvider> layoutProviders, Dictionary<string,object> options)
            {
                _dataType = dataType;
                _getCells = new List<Func<Layouts.LayoutCellPosition>>();
                _layoutProviders = layoutProviders;
                _options = options;
            }

            public GridLayoutBuilder Cell(int column, int row, int colSpan, int rowSpan, Action<LayoutBuilder<TData>> defineCell)
            {
                var layoutBuilder = new LayoutBuilder<TData>(_dataType, _layoutProviders, _options);
                _getCells.Add(() => new LayoutCellPosition(layoutBuilder.GetLastLayout(), column, row, colSpan, rowSpan));
                defineCell(layoutBuilder);
                return this;
            }

            public GridLayoutBuilder Cell(int column, int row, Action<LayoutBuilder<TData>> defineCell)
            {
                return Cell(column, row, 1, 1, defineCell);
            }

            public GridLayoutBuilder If(bool condition, Action defineLayout)
            {
                if (condition) defineLayout();
                return this;
            }

            internal GridLayout GetLayout()
            {
                return new GridLayout(_layoutProviders, _getCells.Select(gc => gc()));
            }
        }

        public class DataGridLayoutBuilder
        {
            private readonly LayoutBuilder<TData> _layoutBuilder;
            private readonly List<OneMemberLayout> _columns;
            private readonly List<OneMemberLayout> _actions;
            private readonly IEnumerable<ILayoutProvider> _layoutProviders;
            private readonly Type _dataType;
            private MethodInfo _openAction;

            internal DataGridLayoutBuilder(Type dataType, IEnumerable<ILayoutProvider> layoutProviders, Dictionary<string, object> options)
            {
                _dataType = dataType;
                _layoutProviders = layoutProviders;
                _layoutBuilder = new LayoutBuilder<TData>(dataType, layoutProviders, options);
                _columns = new List<OneMemberLayout>();
                _actions = new List<OneMemberLayout>();
            }

            public DataGridLayoutBuilder Column(Expression<Func<TData, object>> property, string label, Expression<Func<TData, System.Collections.IEnumerable>> itemsSource)
            {
                _layoutBuilder.Property(property, label, itemsSource);
                _columns.Add((OneMemberLayout)_layoutBuilder.GetLastLayout());
                return this;
            }

            public DataGridLayoutBuilder Column(Expression<Func<TData, object>> property, string label)
            {
                return Column(property, label, null);
            }

            public DataGridLayoutBuilder Column(Expression<Func<TData, object>> property)
            {
                return Column(property, null, null);
            }

            public DataGridLayoutBuilder Column(Expression<Func<TData, Delegate>> method, string label, bool hasCloseBehavior)
            {
                _layoutBuilder.Method(method, label, hasCloseBehavior);
                _columns.Add((OneMemberLayout)_layoutBuilder.GetLastLayout());
                return this;
            }

            public DataGridLayoutBuilder Column(Expression<Func<TData, Delegate>> method, string label)
            {
                return Column(method, label, false);
            }

            public DataGridLayoutBuilder Column(Expression<Func<TData, Delegate>> method)
            {
                return Column(method, null, false);
            }

            public DataGridLayoutBuilder Action(Expression<Func<TData, Delegate>> method, string label, bool hasCloseBehavior)
            {
                _layoutBuilder.Method(method, label, hasCloseBehavior);
                _actions.Add((OneMemberLayout)_layoutBuilder.GetLastLayout());
                return this;
            }

            public DataGridLayoutBuilder Action(Expression<Func<TData, Delegate>> method, string label)
            {
                return Action(method, label, false);
            }

            public DataGridLayoutBuilder Action(Expression<Func<TData, Delegate>> method)
            {
                return Action(method, null, false);
            }

            public DataGridLayoutBuilder Column<TInheritsData>(Expression<Func<TInheritsData, object>> property, string label, Expression<Func<TInheritsData, System.Collections.IEnumerable>> itemsSource)
                where TInheritsData : TData
            {
                if (!typeof(TInheritsData).IsAssignableFrom(_dataType)) return this;
                _layoutBuilder.Property(property, label, itemsSource);
                _columns.Add((OneMemberLayout)_layoutBuilder.GetLastLayout());
                return this;
            }

            public DataGridLayoutBuilder Column<TInheritsData>(Expression<Func<TInheritsData, object>> property, string label)
                where TInheritsData : TData
            {
                return Column(property, label, null);
            }

            public DataGridLayoutBuilder Column<TInheritsData>(Expression<Func<TInheritsData, object>> property)
                where TInheritsData : TData
            {
                return Column(property, null, null);
            }

            public DataGridLayoutBuilder Column<TInheritsData>(Expression<Func<TInheritsData, Delegate>> method, string label, bool hasCloseBehavior)
                where TInheritsData : TData
            {
                if (!typeof(TInheritsData).IsAssignableFrom(_dataType)) return this;
                _layoutBuilder.Method(method, label, hasCloseBehavior);
                _columns.Add((OneMemberLayout)_layoutBuilder.GetLastLayout());
                return this;
            }

            public DataGridLayoutBuilder Column<TInheritsData>(Expression<Func<TInheritsData, Delegate>> method, string label)
                where TInheritsData : TData
            {
                return Column(method, label, false);
            }

            public DataGridLayoutBuilder Column<TInheritsData>(Expression<Func<TInheritsData, Delegate>> method)
                where TInheritsData : TData
            {
                return Column(method, null, false);
            }

            public DataGridLayoutBuilder Action<TInheritsData>(Expression<Func<TInheritsData, Delegate>> method, string label, bool hasCloseBehavior)
                where TInheritsData : TData
            {
                if (!typeof(TInheritsData).IsAssignableFrom(_dataType)) return this;
                _layoutBuilder.Method(method, label, hasCloseBehavior);
                _actions.Add((OneMemberLayout)_layoutBuilder.GetLastLayout());
                return this;
            }

            public DataGridLayoutBuilder Action<TInheritsData>(Expression<Func<TInheritsData, Delegate>> method, string label)
                where TInheritsData : TData
            {
                return Action(method, label, false);
            }

            public DataGridLayoutBuilder Action<TInheritsData>(Expression<Func<TInheritsData, Delegate>> method)
                where TInheritsData : TData
            {
                return Action(method, null, false);
            }

            public DataGridLayoutBuilder Open(Expression<Func<TData, Delegate>> method)
            {
                var methodInfo = (((method.Body as UnaryExpression)?.Operand as MethodCallExpression)?.Object as ConstantExpression).Value as MethodInfo;
                if (methodInfo == null) throw new ArgumentException("method must be a delegate instanciation.", nameof(method));
                _openAction = methodInfo;

                return this;
            }

            public DataGridLayoutBuilder If(bool condition, Action defineLayout)
            {
                if (condition) defineLayout();
                return this;
            }

            internal DataGridLayout GetLayout()
            {
                return new DataGridLayout(_layoutProviders, _columns, _actions, _openAction);
            }
        }

        public class TabsLayoutBuilder
        {
            private readonly Type _dataType;
            private readonly List<Tuple<string, Func<Layout>>> _tabs;
            private readonly IEnumerable<ILayoutProvider> _layoutProviders;
            private readonly Dictionary<string, object> _options;

            internal TabsLayoutBuilder(Type dataType, IEnumerable<ILayoutProvider> layoutProviders, Dictionary<string, object> options)
            {
                _dataType = dataType;
                _layoutProviders = layoutProviders;
                _tabs = new List<Tuple<string, Func<Layouts.Layout>>>();
                _options = options;
            }

            public TabsLayoutBuilder Tab(string tabName, Action<LayoutBuilder<TData>> defineTab)
            {
                var layoutBuilder = new LayoutBuilder<TData>(_dataType, _layoutProviders, _options);
                _tabs.Add(Tuple.Create(tabName, new Func<Layout>(layoutBuilder.GetLastLayout)));
                defineTab(layoutBuilder);
                return this;
            }

            public TabsLayoutBuilder If(bool condition, Action defineLayout)
            {
                if (condition) defineLayout();
                return this;
            }

            internal TabsLayout GetLayout()
            {
                return new TabsLayout(_layoutProviders, _tabs.Select(t => Tuple.Create(t.Item1, t.Item2())));
            }
        }

        public class TreeLayoutbuilder
        {
            private readonly Type _dataType;
            private readonly IEnumerable<ILayoutProvider> _layoutProviders;
            private MemberInfo _children;

            internal TreeLayoutbuilder(Type dataType, IEnumerable<ILayoutProvider> layoutProviders)
            {
                _dataType = dataType;
                _layoutProviders = layoutProviders;
            }

            public void Children(Expression<Func<TData, System.Collections.IEnumerable>> childrenProperty)
            {
                if (childrenProperty == null) throw new ArgumentNullException(nameof(childrenProperty));

                var mbAccess = childrenProperty.Body as MemberExpression ?? (childrenProperty.Body as UnaryExpression)?.Operand as MemberExpression;
                if (mbAccess == null) throw new ArgumentException("property must be a property access expression.", nameof(childrenProperty));

                _children = mbAccess.Member;
            }

            public TreeLayoutbuilder If(bool condition, Action defineLayout)
            {
                if (condition) defineLayout();
                return this;
            }
            
            internal TreeLayout GetLayout()
            {
                return new TreeLayout(_layoutProviders, _children);
            }
        }
    }
}
