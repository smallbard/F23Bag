using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Reflection;
using F23Bag.AutomaticUI;

namespace F23Bag.Winforms.Controls
{
    public partial class TabsControl : DataControl
    {
        private readonly Func<Type, IAuthorization> _getAuthorization;

        public TabsControl(Func<Type, IAuthorization> getAuthorization)
        {
            InitializeComponent();
            _getAuthorization = getAuthorization;
        }

        public string Label { get; set; }

        public PropertyInfo Property
        {
            get { return (PropertyInfo)DisplayedMember; }
            set { DisplayedMember = value; }
        }

        public void AddTab(string tabName, DataControl control)
        {
            var tab = new TabPage(tabName) { AutoScroll = true, BackColor = cstBackColor };
            tab.Controls.Add(control);
            tabs.TabPages.Add(tab);
        }

        protected override void CustomDisplay(object data, I18n i18n)
        {
            var oldData = data;
            if (Property != null)
            {
                data = Property.GetValue(data);
                lblTitle.Text = i18n.GetTranslation(Label);
                if (data == null) Property.SetValue(oldData, data = Activator.CreateInstance(Property.PropertyType));
            }

            if (Property == null)
            {
                lblTitle.Text = data.ToString();
                lblTitle.Visible = Parent is Form;
            }
            foreach (var tab in tabs.TabPages.OfType<TabPage>())
            {
                tab.Text = i18n.GetTranslation(tab.Text);
                foreach (var ctrl in tab.Controls.OfType<DataControl>())
                    ctrl.Display(data, i18n, _getAuthorization);
            }
        }

        private void TabsControl_Load(object sender, EventArgs e)
        {
            if (tabs.TabPages.Count == 0) return;

            foreach (var tab in tabs.TabPages.OfType<TabPage>()) tabs.SelectedTab = tab;
            tabs.SelectedIndex = 0;

            ClientSize = new Size(
                tabs.TabPages.OfType<TabPage>().SelectMany(tp => tp.Controls.OfType<Control>()).Max(c => c.Width) + 10,
                tabs.TabPages.OfType<TabPage>().SelectMany(tp => tp.Controls.OfType<Control>()).Max(c => c.Height) + 40);
            if (Parent is Form) Parent.ClientSize = new Size(ClientSize.Width, ClientSize.Height);
        }
    }
}
