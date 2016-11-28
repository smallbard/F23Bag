using System;
using System.ComponentModel;
using System.Windows.Forms;
using System.Reflection;
using F23Bag.AutomaticUI;

namespace F23Bag.Winforms.Controls
{
    public partial class DateControl : DataControl
    {
        private readonly PropertyInfo _property;
        private readonly string _label;
        private object _data;

        public DateControl()
        {
            InitializeComponent();
        }

        public DateControl(PropertyInfo property, string label)
            : this()
        {
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

        protected override void CustomDisplay(object data, I18n i18n)
        {
            lblLabel.Text = i18n.GetTranslation(_label);
            if (data is INotifyPropertyChanged)
            {
                ((INotifyPropertyChanged)data).PropertyChanged -= StringControl_PropertyChanged;
                ((INotifyPropertyChanged)data).PropertyChanged += StringControl_PropertyChanged;
            }
            _data = data;
            dateValue.Value = (DateTime)_property.GetValue(data);
            dateValue.ValueChanged -= DateValue_ValueChanged;
            dateValue.ValueChanged += DateValue_ValueChanged;
        }

        private void StringControl_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == _property.Name)
            {
                Action act = () => dateValue.Value = (DateTime)_property.GetValue(_data);
                if (InvokeRequired)
                    Invoke(act);
                else
                    act();
            }
        }

        private void DateValue_ValueChanged(object sender, EventArgs e)
        {
            _property.SetValue(_data, dateValue.Value);
        }
    }
}
