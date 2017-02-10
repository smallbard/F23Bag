using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using F23Bag.Winforms.Controls;
using F23Bag.AutomaticUI.Layouts;
using F23Bag.AutomaticUI;
using System;
using System.Drawing;
using F23Bag.Domain;

namespace F23Bag.Winforms
{
    public class WinformsUIBuilder : IUIBuilder
    {
        private bool _isApplicationLaunch;
        private readonly WinformContext _context;

        public WinformsUIBuilder(IEnumerable<IControlConvention> controlsConventions, bool isApplicationLaunch, Func<Type, object> resolve, I18n i18n, Func<Type, IAuthorization> getAuthorization)
        {
            ControlConventions = controlsConventions.OrderBy(c => c.GetType().Assembly == GetType().Assembly ? 1 : 0).ToList();
            _context = new WinformContext(this, i18n, getAuthorization, resolve);

            if (_isApplicationLaunch = isApplicationLaunch)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
            }
        }

        public IEnumerable<IControlConvention> ControlConventions { get; private set; }

        public void Display(Layout layout, object data, string label)
        {
            var control = GetDataControl(layout, data, null);
            using (var form = new Form())
            {
                form.Controls.Add(control);
                form.AutoScroll = true;
                form.SizeGripStyle = SizeGripStyle.Hide;
                form.FormBorderStyle = FormBorderStyle.Sizable;
                form.BackColor = Color.White;
                form.Text = _context.I18n.GetTranslation(label);

                form.Shown += (s, e) => form.Location = new Point((Screen.FromControl(form).WorkingArea.Width - form.Width) / 2, (Screen.FromControl(form).WorkingArea.Height - form.Height) / 2);
                form.Load += (s, e) =>
                {
                    control.Display(data);
                    control.Dock = DockStyle.Fill;
                };

                if (_isApplicationLaunch)
                {
                    _isApplicationLaunch = false;
                    Application.Run(form);
                }
                else
                {
                    form.StartPosition = FormStartPosition.CenterParent;
                    form.ShowDialog();
                }
            }
        }

        private DataControl GetDataControl(Layout layout, object data, PropertyInfo ownerProperty)
        {
            if(layout.SelectorType != null)
            {
                _context.SelectorOwnerProperties[layout] = ownerProperty;
                if (!layout.SelectorType.IsAssignableFrom(data.GetType())) data = _context.Resolve(layout.SelectorType);
            }

            if (layout is FlowLayout)
            {
                var flowLayout = (FlowLayout)layout;
                var flowControl = new FlowControl(layout, _context, flowLayout.FlowDirection == FlowDirectionEnum.Vertical ? FlowDirection.TopDown : FlowDirection.LeftToRight);
                foreach (var subLayout in flowLayout.ChildLayout) flowControl.AddControl(GetDataControl(subLayout, data, ownerProperty));
                return flowControl;
            }
            else if (layout is OneMemberLayout)
            {
                var memberLayout = (OneMemberLayout)layout;
                var property = memberLayout.Member as PropertyInfo;
                var method = memberLayout.Member as MethodInfo;

                if (property != null)
                {
                    var idc = ControlConventions.FirstOrDefault(c => c.Accept(property, memberLayout));
                    if (idc != null) return idc.GetControl(property, memberLayout, _context);

                    if (typeof(System.Collections.IEnumerable).IsAssignableFrom(property.PropertyType))
                    {
                        var dataGridLayout = layout.LoadSubLayout(property.PropertyType.GetGenericArguments()[0], true, true).FirstOrDefault(l => l is DataGridLayout);
                        if (dataGridLayout == null) throw new WinformsException("No datagrid layout for " + property.PropertyType.GetGenericArguments()[0].FullName);

                        var dataGridControl = (DataGridControl)GetDataControl(dataGridLayout, data, property);
                        dataGridControl.Property = property;
                        dataGridControl.Label = memberLayout.Label;

                        return dataGridControl;
                    }
                    else if (property.PropertyType.IsClass)
                    {
                        var isSelector = property.Name == nameof(ISelector<object>.SelectedValue) && typeof(ISelector<>).MakeGenericType(property.PropertyType).IsAssignableFrom(property.DeclaringType);
                        var valueType = property.GetValue(data)?.GetType() ?? property.PropertyType;

                        var subLayout = layout.LoadSubLayout(valueType, true, isSelector).FirstOrDefault(l => !(l is DataGridLayout));

                        var container = GetDataControl(subLayout, data, isSelector ? ownerProperty : property);
                        if (container is FlowControl)
                        {
                            ((FlowControl)container).Property = property;
                            ((FlowControl)container).Label = memberLayout.Label;
                        }
                        else if (container is TableControl)
                        {
                            ((TableControl)container).Property = property;
                            ((TableControl)container).Label = memberLayout.Label;
                        }
                        else if (container is TabsControl)
                        {
                            ((TabsControl)container).Property = property;
                            ((TabsControl)container).Label = memberLayout.Label;
                        }
                        return container;
                    }
                }
                else if (method != null)
                    return new MethodCallControl(memberLayout, _context, method, memberLayout.HasCloseBehavior, memberLayout.Label);
            }
            else if (layout is GridLayout)
            {
                var gridLayout = (GridLayout)layout;
                var tableControl = new TableControl(layout, _context);

                foreach (var layoutCellPosition in gridLayout.LayoutCellPositions)
                    tableControl.AddControl(GetDataControl(layoutCellPosition.Layout, data, ownerProperty), layoutCellPosition.Column, layoutCellPosition.Row, layoutCellPosition.ColumnSpan, layoutCellPosition.RowSpan);

                return tableControl;
            }
            else if (layout is DataGridLayout)
            {
                var dataGridLayout = (DataGridLayout)layout;
                var dataGridControl = new DataGridControl(layout, _context, dataGridLayout.OpenAction);

                foreach (var subLayout in dataGridLayout.Columns)
                    if (subLayout.Member is PropertyInfo)
                        dataGridControl.AddColumn((PropertyInfo)subLayout.Member, subLayout.IsEditable, subLayout.Label);
                    else if (subLayout.Member is MethodInfo)
                        dataGridControl.AddColumn((MethodInfo)subLayout.Member, subLayout.Label);
                foreach (var subLayout in dataGridLayout.Actions.Where(l => l.Member is MethodInfo)) dataGridControl.AddAction(subLayout, (MethodInfo)subLayout.Member, subLayout.Label);

                return dataGridControl;
            }
            else if (layout is TabsLayout)
            {
                var tabsLayout = (TabsLayout)layout;
                var tabsControl = new TabsControl(layout, _context);
                foreach (var tab in tabsLayout.Tabs.Where(t => t.Item2 != null)) tabsControl.AddTab(tab.Item1, GetDataControl(tab.Item2, data, ownerProperty));
                return tabsControl;
            }
            else if (layout is TreeLayout)
                return new TreeControl((TreeLayout)layout, _context);

            throw new WinformsException("Layout not supported : " + layout.GetType().FullName);
        }
    }
}
