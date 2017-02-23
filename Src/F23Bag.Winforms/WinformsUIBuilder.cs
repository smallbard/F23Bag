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

        public WinformsUIBuilder(IEnumerable<IControlConvention> controlsConventions, bool isApplicationLaunch, Func<Type, object> resolve, I18n i18n, UIEngine engine)
        {
            ControlConventions = controlsConventions.OrderBy(c => c.GetType().Assembly == GetType().Assembly ? 1 : 0).ToList();
            _context = new WinformContext(this, i18n, engine, resolve);

            if (_isApplicationLaunch = isApplicationLaunch)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
            }
        }

        public IEnumerable<IControlConvention> ControlConventions { get; private set; }

        public void Display(Layout layout, object data, string label)
        {
            var control = GetControlForLayout(layout, data, null);
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

        private DataControl GetControlForLayout(Layout layout, object data, PropertyInfo ownerProperty)
        {
            if (layout.SelectorType != null && !layout.SelectorType.IsAssignableFrom(data.GetType()))
                data = _context.Resolve(layout.SelectorType);

            if (layout is FlowLayout)
            {
                var flowLayout = (FlowLayout)layout;
                var flowControl = new FlowControl(layout, _context, flowLayout.FlowDirection == FlowDirectionEnum.Vertical ? FlowDirection.TopDown : FlowDirection.LeftToRight);
                foreach (var subLayout in flowLayout.ChildLayout) flowControl.AddControl(GetControlForLayout(subLayout, data, ownerProperty));
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
                    if (idc != null) return idc.GetControl(data, property, memberLayout, _context);

                    if (property.PropertyType.IsClass)
                    {
                        var subLayout = layout.GetCreateUpdateLayout(property, data);
                        var container = GetControlForLayout(subLayout, data, property);
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
                    tableControl.AddControl(GetControlForLayout(layoutCellPosition.Layout, data, ownerProperty), layoutCellPosition.Column, layoutCellPosition.Row, layoutCellPosition.ColumnSpan, layoutCellPosition.RowSpan);

                return tableControl;
            }
            else if (layout is TabsLayout)
            {
                var tabsLayout = (TabsLayout)layout;
                var tabsControl = new TabsControl(layout, _context);
                foreach (var tab in tabsLayout.Tabs.Where(t => t.Item2 != null)) tabsControl.AddTab(tab.Item1, GetControlForLayout(tab.Item2, data, ownerProperty));
                return tabsControl;
            }
            else if (layout is TreeLayout)
                return new TreeControl((TreeLayout)layout, _context);

            throw new WinformsException("Layout not supported : " + layout.GetType().FullName);
        }
    }
}
