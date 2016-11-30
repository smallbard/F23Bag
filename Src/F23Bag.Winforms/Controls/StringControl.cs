using System;
using System.ComponentModel;
using System.Windows.Forms;
using System.Reflection;
using F23Bag.AutomaticUI;
using F23Bag.AutomaticUI.Layouts;

namespace F23Bag.Winforms.Controls
{
    public partial class StringControl : DataControl
    {
        private readonly PropertyInfo _property;
        private readonly string _label;
        private object _data;

        public StringControl(Layout layout, WinformContext context, PropertyInfo property, string label)
            : base(layout, context)
        {
            InitializeComponent();

            DisplayedMember = property;
            _property = property;
            _label = label;
        }

        protected override PictureBox ValidationIcon
        {
            get
            {
                return _validationIcon;
            }
        }

        protected override void CustomDisplay(object data)
        {
            lblLabel.Text = Context.I18n.GetTranslation(_label);
            if (data is INotifyPropertyChanged)
            {
                ((INotifyPropertyChanged)data).PropertyChanged -= StringControl_PropertyChanged;
                ((INotifyPropertyChanged)data).PropertyChanged += StringControl_PropertyChanged;
            }
            _data = data;
            txtValue.Text = (string)_property.GetValue(data);
            txtValue.TextChanged -= TxtValue_TextChanged;
            txtValue.TextChanged += TxtValue_TextChanged;
        }

        private void StringControl_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == _property.Name)
            {
                Action act = () => txtValue.Text = (string)_property.GetValue(_data);
                if (InvokeRequired)
                    Invoke(act);
                else
                    act();
            }
        }

        private void TxtValue_TextChanged(object sender, EventArgs e)
        {
            _property.SetValue(_data, txtValue.Text);
        }
    }
}
