using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using F23Bag.AutomaticUI;
using F23Bag.AutomaticUI.Layouts;
using F23Bag.Tests.AutomaticUITestElements;
using F23Bag.Domain;
using System.Reflection;

namespace F23Bag.Tests
{
    [TestClass]
    public class AutomaticUITests
    {
        [TestMethod]
        public void FlowLayout()
        {
            var builder = new TestUIBuilder();
            var engine = new UIEngine(GetLayoutProviders(), null, ga => builder);
            var data = new ObjectForFlowLayout();
            engine.Display(data);

            Assert.IsTrue(builder.Layouts.ContainsKey(data));
            Assert.IsInstanceOfType(builder.Layouts[data], typeof(FlowLayout));

            var layout = (FlowLayout)builder.Layouts[data];
            Assert.AreEqual(FlowDirectionEnum.Horizontal, layout.FlowDirection);
            Assert.AreEqual(2, layout.ChildLayout.Count());

            var oneMemberLayout1 = layout.ChildLayout.First() as OneMemberLayout;
            var oneMemberLayout2 = layout.ChildLayout.Last() as OneMemberLayout;

            Assert.IsNotNull(oneMemberLayout1);
            Assert.IsNotNull(oneMemberLayout2);

            Assert.AreEqual("P2", oneMemberLayout1.Member.Name);
            Assert.AreEqual("ObjectForFlowLayout.P2", oneMemberLayout1.Label);
            Assert.IsFalse(oneMemberLayout1.HasCloseBehavior);
            Assert.IsTrue(oneMemberLayout1.IsEditable);
            Assert.IsNull(oneMemberLayout1.ItemsSource);

            Assert.AreEqual("P1", oneMemberLayout2.Member.Name);
            Assert.AreEqual("ObjectForFlowLayout.P1", oneMemberLayout2.Label);
            Assert.IsFalse(oneMemberLayout2.HasCloseBehavior);
            Assert.IsTrue(oneMemberLayout2.IsEditable);
            Assert.IsNull(oneMemberLayout2.ItemsSource);
        }

        [TestMethod]
        public void TabsLayout()
        {
            var builder = new TestUIBuilder();
            var engine = new UIEngine(GetLayoutProviders(), null, ga => builder);
            var data = new ObjectForTabsLayout();
            engine.Display(data);

            Assert.IsTrue(builder.Layouts.ContainsKey(data));
            Assert.IsInstanceOfType(builder.Layouts[data], typeof(TabsLayout));

            var layout = (TabsLayout)builder.Layouts[data];
            Assert.AreEqual(2, layout.Tabs.Count());
            Assert.AreEqual("FirstTab", layout.Tabs.First().Item1);
            Assert.AreEqual("SecondTab", layout.Tabs.Last().Item1);
            Assert.IsInstanceOfType(layout.Tabs.First().Item2, typeof(FlowLayout));
            Assert.IsInstanceOfType(layout.Tabs.Last().Item2, typeof(FlowLayout));

            var tab1Layout = (FlowLayout)layout.Tabs.First().Item2;
            var tab2Layout = (FlowLayout)layout.Tabs.Last().Item2;

            Assert.AreEqual(FlowDirectionEnum.Vertical, tab1Layout.FlowDirection);
            Assert.AreEqual(FlowDirectionEnum.Horizontal, tab2Layout.FlowDirection);

            var tab1Childs = tab1Layout.ChildLayout.OfType<OneMemberLayout>().ToArray();
            var tab2Childs = tab2Layout.ChildLayout.OfType<OneMemberLayout>().ToArray();

            Assert.AreEqual(2, tab1Childs.Length);
            Assert.AreEqual(2, tab2Childs.Length);

            Assert.AreEqual("P1", tab1Childs[0].Member.Name);
            Assert.AreEqual("P3", tab1Childs[1].Member.Name);

            Assert.AreEqual("P2", tab2Childs[0].Member.Name);
            Assert.AreEqual("P4", tab2Childs[1].Member.Name);
        }

        [TestMethod]
        public void GridLayout()
        {
            var builder = new TestUIBuilder();
            var engine = new UIEngine(GetLayoutProviders(), null, ga => builder);
            var data = new ObjectForGridLayout();
            engine.Display(data);

            Assert.IsTrue(builder.Layouts.ContainsKey(data));
            Assert.IsInstanceOfType(builder.Layouts[data], typeof(GridLayout));

            var layout = (GridLayout)builder.Layouts[data];
            Assert.AreEqual(2, layout.LayoutCellPositions.Count());

            var firstCell = layout.LayoutCellPositions.First();
            var secondCell = layout.LayoutCellPositions.Last();

            Assert.AreEqual(0, firstCell.Column);
            Assert.AreEqual(1, firstCell.ColumnSpan);
            Assert.AreEqual(0, firstCell.Row);
            Assert.AreEqual(1, firstCell.RowSpan);

            Assert.AreEqual(1, secondCell.Column);
            Assert.AreEqual(2, secondCell.ColumnSpan);
            Assert.AreEqual(0, secondCell.Row);
            Assert.AreEqual(4, secondCell.RowSpan);

            Assert.IsInstanceOfType(firstCell.Layout, typeof(FlowLayout));
            Assert.IsInstanceOfType(secondCell.Layout, typeof(FlowLayout));

            var tab1Layout = (FlowLayout)firstCell.Layout;
            var tab2Layout = (FlowLayout)secondCell.Layout;

            Assert.AreEqual(FlowDirectionEnum.Vertical, tab1Layout.FlowDirection);
            Assert.AreEqual(FlowDirectionEnum.Horizontal, tab2Layout.FlowDirection);

            var tab1Childs = tab1Layout.ChildLayout.OfType<OneMemberLayout>().ToArray();
            var tab2Childs = tab2Layout.ChildLayout.OfType<OneMemberLayout>().ToArray();

            Assert.AreEqual(2, tab1Childs.Length);
            Assert.AreEqual(2, tab2Childs.Length);

            Assert.AreEqual("P1", tab1Childs[0].Member.Name);
            Assert.AreEqual("P3", tab1Childs[1].Member.Name);

            Assert.AreEqual("P2", tab2Childs[0].Member.Name);
            Assert.AreEqual("P4", tab2Childs[1].Member.Name);
        }

        [TestMethod]
        public void DataGridLayout()
        {
            var builder = new TestUIBuilder();
            var engine = new UIEngine(GetLayoutProviders(), null, ga => builder);
            var data = new ObjectForDataGridLayout();
            engine.Display(data);

            Assert.IsTrue(builder.Layouts.ContainsKey(data));
            Assert.IsInstanceOfType(builder.Layouts[data], typeof(DataGridLayout));

            var layout = (DataGridLayout)builder.Layouts[data];
            Assert.AreEqual(3, layout.Columns.Count());
            Assert.AreEqual(1, layout.Actions.Count());
            Assert.IsNotNull(layout.OpenAction);
            Assert.AreEqual("Open", layout.OpenAction.Name);

            var columns = layout.Columns.ToArray();
            Assert.AreEqual("P1", columns[0].Member.Name);
            Assert.AreEqual("P3", columns[1].Member.Name);
            Assert.AreEqual("P2", columns[2].Member.Name);

            Assert.AreEqual("A1", layout.Actions.First().Member.Name);
            Assert.AreEqual("Action 1", layout.Actions.First().Label);
        }

        [TestMethod]
        public void TreeLayout()
        {
            var builder = new TestUIBuilder();
            var engine = new UIEngine(GetLayoutProviders(), null, ga => builder);
            var data = new ObjectForTreeLayout();
            engine.Display(data);

            Assert.IsTrue(builder.Layouts.ContainsKey(data));
            Assert.IsInstanceOfType(builder.Layouts[data], typeof(TreeLayout));

            var layout = (TreeLayout)builder.Layouts[data];
            Assert.AreEqual(nameof(ObjectForTreeLayout.Children), layout.Children.Name);
            Assert.IsInstanceOfType(layout.Children, typeof(PropertyInfo));
        }

        [TestMethod]
        public void LayoutForBaseType()
        {
            var builder = new TestUIBuilder();
            var engine = new UIEngine(GetLayoutProviders(), null, ga => builder);
            var data = new ObjectInheritsGridLayout();
            engine.Display(data);

            Assert.IsTrue(builder.Layouts.ContainsKey(data));
            Assert.IsInstanceOfType(builder.Layouts[data], typeof(GridLayout));

            Assert.IsTrue(builder.Layouts.ContainsKey(data));
            Assert.IsInstanceOfType(builder.Layouts[data], typeof(GridLayout));

            var layout = (GridLayout)builder.Layouts[data];
            Assert.AreEqual(2, layout.LayoutCellPositions.Count());

            var firstCell = layout.LayoutCellPositions.First();
            var secondCell = layout.LayoutCellPositions.Last();

            Assert.AreEqual(0, firstCell.Column);
            Assert.AreEqual(1, firstCell.ColumnSpan);
            Assert.AreEqual(0, firstCell.Row);
            Assert.AreEqual(1, firstCell.RowSpan);

            Assert.AreEqual(1, secondCell.Column);
            Assert.AreEqual(2, secondCell.ColumnSpan);
            Assert.AreEqual(0, secondCell.Row);
            Assert.AreEqual(4, secondCell.RowSpan);

            Assert.IsInstanceOfType(firstCell.Layout, typeof(FlowLayout));
            Assert.IsInstanceOfType(secondCell.Layout, typeof(FlowLayout));

            var tab1Layout = (FlowLayout)firstCell.Layout;
            var tab2Layout = (FlowLayout)secondCell.Layout;

            Assert.AreEqual(FlowDirectionEnum.Vertical, tab1Layout.FlowDirection);
            Assert.AreEqual(FlowDirectionEnum.Horizontal, tab2Layout.FlowDirection);

            var tab1Childs = tab1Layout.ChildLayout.OfType<OneMemberLayout>().ToArray();
            var tab2Childs = tab2Layout.ChildLayout.OfType<OneMemberLayout>().ToArray();

            Assert.AreEqual(2, tab1Childs.Length);
            Assert.AreEqual(3, tab2Childs.Length);

            Assert.AreEqual("P1", tab1Childs[0].Member.Name);
            Assert.AreEqual("P3", tab1Childs[1].Member.Name);

            Assert.AreEqual("P2", tab2Childs[0].Member.Name);
            Assert.AreEqual("P4", tab2Childs[1].Member.Name);
            Assert.AreEqual("PH", tab2Childs[2].Member.Name);
        }

        [TestMethod]
        public void LayoutForGenericType()
        {
            var builder = new TestUIBuilder();
            var engine = new UIEngine(GetLayoutProviders(), null, ga => builder);
            var data = new GenericObject<int>();
            engine.Display(data);

            Assert.IsTrue(builder.Layouts.ContainsKey(data));
            Assert.IsInstanceOfType(builder.Layouts[data], typeof(GridLayout));
        }

        [TestMethod]
        public void LayoutForSelector()
        {
            var builder = new TestUIBuilder();
            var engine = new UIEngine(GetLayoutProviders(), null, ga => builder);
            var data = new ObjectForSelectorParent();
            engine.Display(data);

            Assert.IsTrue(builder.Layouts.ContainsKey(data));
            Assert.IsInstanceOfType(builder.Layouts[data], typeof(FlowLayout));
            var flowLayout = (FlowLayout)builder.Layouts[data];

            var layout = flowLayout.GetCreateUpdateLayout(typeof(ObjectForSelectorParent).GetProperty(nameof(ObjectForSelectorParent.Object)), data);
            Assert.IsInstanceOfType(layout, typeof(FlowLayout));
            var selectorLayout = (FlowLayout)layout;
            Assert.AreEqual(FlowDirectionEnum.Horizontal, selectorLayout.FlowDirection);
            Assert.AreEqual(1, selectorLayout.ChildLayout.Count());
            var selectedValueLayout = selectorLayout.ChildLayout.First() as OneMemberLayout;
            Assert.IsNotNull(selectedValueLayout);
            Assert.AreEqual(nameof(ISelector<ObjectForSelector>.SelectedValue), selectedValueLayout.Member.Name);
        }

        private IEnumerable<ILayoutProvider> GetLayoutProviders()
        {
            return new ILayoutProvider[] 
            {
                new GenericObjectLayout(),
                new ObjectForDataGridLayoutProvider(),
                new ObjectForFlowLayoutProvider(),
                new ObjectForGridLayoutProvider(),
                new ObjectForTabsLayoutProvider(),
                new SelectorForObjectForSelectorLayoutProvider(),
                new ObjectForTreeLayoutProvider()
            };
        }

        public class TestUIBuilder : IUIBuilder
        {
            public Dictionary<object, Layout> Layouts { get; } = new Dictionary<object, Layout>();

            public void Display(Layout layout, object data, string label)
            {
                Layouts[data] = layout;
            }
        }
    }
}
