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

        public WinformsUIBuilder(IEnumerable<IControlConvention> controlsConventions, bool isApplicationLaunch)
        {
            ControlConventions = controlsConventions.OrderBy(c => c.GetType().Assembly == GetType().Assembly ? 1 : 0).ToList();

            if (_isApplicationLaunch = isApplicationLaunch)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
            }
        }

        public IEnumerable<IControlConvention> ControlConventions { get; private set; }

        public void Display(Layout layout, object data, string label, I18n i18n, Func<Type, IAuthorization> getAuthorization)
        {
            var control = GetDataControl(layout, data, i18n, getAuthorization);
            using (var form = new Form())
            {
                form.Controls.Add(control);
                form.AutoScroll = true;
                form.SizeGripStyle = SizeGripStyle.Hide;
                form.FormBorderStyle = FormBorderStyle.FixedSingle;
                form.BackColor = System.Drawing.Color.White;
                form.Text = i18n.GetTranslation(label);

                form.Shown += (s, e) => form.Location = new Point((Screen.FromControl(form).WorkingArea.Width - form.Width) / 2, (Screen.FromControl(form).WorkingArea.Height - form.Height) / 2);
                form.Load += (s, e) => control.Display(data, i18n, getAuthorization);

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

        private DataControl GetDataControl(Layout layout, object data, I18n i18n, Func<Type, IAuthorization> getAuthorization)
        {
            if (layout is FlowLayout)
            {
                var flowLayout = (FlowLayout)layout;
                var flowControl = new FlowControl(flowLayout.FlowDirection == FlowDirectionEnum.Vertical ? FlowDirection.TopDown : FlowDirection.LeftToRight, getAuthorization);
                foreach (var subLayout in flowLayout.ChildLayout) flowControl.AddControl(GetDataControl(subLayout, data, i18n, getAuthorization));
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
                    if (idc != null) return idc.GetControl(property, memberLayout);

                    if (typeof(System.Collections.IEnumerable).IsAssignableFrom(property.PropertyType))
                    {
                        var dataGridLayout = layout.LoadSubLayout(property.PropertyType.GetGenericArguments()[0], true, true).FirstOrDefault(l => l is DataGridLayout);
                        if (dataGridLayout == null) throw new WinformsException("No datagrid layout for " + property.PropertyType.GetGenericArguments()[0].FullName);

                        var dataGridControl = (DataGridControl)GetDataControl(dataGridLayout, data, i18n, getAuthorization);
                        dataGridControl.Property = property;
                        dataGridControl.Label = memberLayout.Label;

                        return dataGridControl;
                    }
                    else if (property.PropertyType.IsClass)
                    {
                        var isSelector = property.Name == nameof(ISelector<object>.SelectedValue) && typeof(ISelector<>).MakeGenericType(property.PropertyType).IsAssignableFrom(property.DeclaringType);
                        var valueType = property.GetValue(data)?.GetType() ?? property.PropertyType;

                        var subLayout = layout.LoadSubLayout(valueType, true, isSelector).FirstOrDefault(l => !(l is DataGridLayout));

                        var container = GetDataControl(subLayout, data, i18n, getAuthorization);
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
                    return new MethodCallControl(memberLayout, method, memberLayout.HasCloseBehavior, this, memberLayout.Label, getAuthorization);
            }
            else if (layout is GridLayout)
            {
                var gridLayout = (GridLayout)layout;
                var tableControl = new TableControl(getAuthorization);

                foreach (var layoutCellPosition in gridLayout.LayoutCellPositions)
                    tableControl.AddControl(GetDataControl(layoutCellPosition.Layout, data, i18n, getAuthorization), layoutCellPosition.Column, layoutCellPosition.Row, layoutCellPosition.ColumnSpan, layoutCellPosition.RowSpan);

                return tableControl;
            }
            else if (layout is DataGridLayout)
            {
                var dataGridLayout = (DataGridLayout)layout;
                var dataGridControl = new DataGridControl(layout, this, dataGridLayout.OpenAction, getAuthorization);

                foreach (var subLayout in dataGridLayout.Columns)
                    if (subLayout.Member is PropertyInfo)
                        dataGridControl.AddColumn((PropertyInfo)subLayout.Member, subLayout.IsEditable, i18n, subLayout.Label);
                    else if (subLayout.Member is MethodInfo)
                        dataGridControl.AddColumn((MethodInfo)subLayout.Member, i18n, subLayout.Label);
                foreach (var subLayout in dataGridLayout.Actions.Where(l => l.Member is MethodInfo)) dataGridControl.AddAction(subLayout, (MethodInfo)subLayout.Member, i18n, subLayout.Label);

                return dataGridControl;
            }
            else if (layout is TabsLayout)
            {
                var tabsLayout = (TabsLayout)layout;
                var tabsControl = new TabsControl(getAuthorization);
                foreach (var tab in tabsLayout.Tabs) tabsControl.AddTab(tab.Item1, GetDataControl(tab.Item2, data, i18n, getAuthorization));
                return tabsControl;
            }
            else if (layout is TreeLayout)
                return new TreeControl((TreeLayout)layout);

            throw new WinformsException("Layout not supported : " + layout.GetType().FullName);
        }
    }
}
