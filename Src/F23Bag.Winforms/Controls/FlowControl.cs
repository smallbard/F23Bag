using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Reflection;
using F23Bag.AutomaticUI;

namespace F23Bag.Winforms.Controls
{
    public partial class FlowControl : DataControl
    {
        private readonly Func<Type, IAuthorization> _getAuthorization;

        public FlowControl(FlowDirection flowDirection, Func<Type, IAuthorization> getAuthorization)
        {
            InitializeComponent();
            flowLayout.FlowDirection = flowDirection;
            _getAuthorization = getAuthorization;
        }

        public string Label { get; set; }

        public PropertyInfo Property
        {
            get { return (PropertyInfo)DisplayedMember; }
            set { DisplayedMember = value; }
        }

        public void AddControl(DataControl ctrl)
        {
            flowLayout.Controls.Add(ctrl);
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

            foreach (var ctrl in flowLayout.Controls.OfType<DataControl>()) ctrl.Display(data, i18n, _getAuthorization);
        }

        private void FlowControl_Load(object sender, EventArgs e)
        {
            if (flowLayout.FlowDirection == FlowDirection.TopDown)
                ClientSize = new Size(flowLayout.Controls.OfType<Control>().Max(c => c.Width) + 20, flowLayout.Controls.OfType<Control>().Sum(c => c.Height + 20));
            else
                ClientSize = new Size(flowLayout.Controls.OfType<Control>().Sum(c => c.Width) + 40, flowLayout.Controls.OfType<Control>().Max(c => c.Height) + 60);
            if (Parent is Form) Parent.ClientSize = new Size(ClientSize.Width, ClientSize.Height);
        }
    }
}
