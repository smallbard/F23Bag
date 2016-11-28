using System;
using System.Linq;
using System.Windows.Forms;
using F23Bag.AutomaticUI.Layouts;
using F23Bag.AutomaticUI;
using System.Reflection;

namespace F23Bag.Winforms.Controls
{
    public partial class TreeControl : DataControl
    {
        private readonly TreeLayout _treeLayout;

        public TreeControl(TreeLayout treeLayout)
        {
            InitializeComponent();

            _treeLayout = treeLayout;
            _tree.BeforeExpand += Tree_BeforeExpand;
        }

        protected override void CustomDisplay(object data, I18n i18n)
        {
            AddChildNode(_tree.Nodes, _treeLayout, data);
        }

        private void Tree_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            var dataLayout = (Tuple<object, Layout>)e.Node.Tag;
            if (dataLayout.Item2 is TreeLayout)
            {
                var treeLayout = (TreeLayout)dataLayout.Item2;
                var children = ((System.Collections.IEnumerable)((PropertyInfo)treeLayout.Children).GetValue(dataLayout.Item1)).OfType<object>().ToList();

                if (e.Node.Nodes.Count > 0)
                {
                    foreach (var node in e.Node.Nodes.OfType<TreeNode>().ToList())
                    {
                        var subDataLayout = (Tuple<object, Layout>)node.Tag;
                        if (!children.Contains(subDataLayout.Item1))
                            e.Node.Nodes.Remove(node);
                        else
                            children.Remove(subDataLayout.Item1);
                    }
                }

                foreach (var child in children)
                {
                    if (child == null) continue;
                    AddChildNode(e.Node.Nodes, treeLayout, child);
                }
            }
        }

        private void AddChildNode(TreeNodeCollection nodes, TreeLayout treeLayout, object child)
        {
            nodes.Add(new TreeNode(child.ToString()) { Tag = Tuple.Create(child, treeLayout.LoadSubLayout(child.GetType(), false, true).FirstOrDefault()) });
        }
    }
}
