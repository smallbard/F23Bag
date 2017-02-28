using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Reflection;
using F23Bag.AutomaticUI;
using F23Bag.AutomaticUI.Layouts;

namespace F23Bag.Winforms.Controls
{
    public partial class FlowControl : DataControl
    {
        public FlowControl(Layout layout, WinformContext context, FlowDirection flowDirection)
            : base(layout, context)
        {
            InitializeComponent();
            flowLayout.FlowDirection = flowDirection;
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

        protected override void CustomDisplay(object data)
        {
            //var oldData = data;
            if (Property != null)
            {
                //data = Property.GetValue(data);
                lblTitle.Text = Context.I18n.GetTranslation(Label);
                //if (data == null) Property.SetValue(oldData, data = Context.Resolve(Property.PropertyType));
            }

            if (Property == null)
            {
                lblTitle.Text = data.ToString();
                lblTitle.Visible = Parent is Form;
            }

            foreach (var ctrl in flowLayout.Controls.OfType<DataControl>()) ctrl.Display(data);
        }

        private void FlowControl_Load(object sender, EventArgs e)
        {
            if (flowLayout.FlowDirection == FlowDirection.TopDown)
                ClientSize = new Size(flowLayout.Controls.OfType<Control>().Max(c => c.Width) + 15, flowLayout.Controls.OfType<Control>().Sum(c => c.Height + 20));
            else
                ClientSize = new Size(flowLayout.Controls.OfType<Control>().Sum(c => c.Width) + 20, flowLayout.Controls.OfType<Control>().Max(c => c.Height) + 15);
            if (Parent is Form) Parent.ClientSize = new Size(ClientSize.Width, ClientSize.Height);

            flowLayout.Dock = DockStyle.Fill;
        }
    }
}
