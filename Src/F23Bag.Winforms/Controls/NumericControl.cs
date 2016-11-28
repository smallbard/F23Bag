using System;
using System.ComponentModel;
using System.Windows.Forms;
using System.Reflection;
using F23Bag.AutomaticUI;

namespace F23Bag.Winforms.Controls
{
    public partial class NumericControl : DataControl
    {
        private readonly PropertyInfo _property;
        private readonly string _label;
        private object _data;

        public NumericControl()
        {
            InitializeComponent();
        }

        public NumericControl(PropertyInfo property, string label)
            : this()
        {
            DisplayedMember = property;
            _property = property;
            _label = label;
            txtValue.Maximum = decimal.MaxValue;

            if (_property.PropertyType == typeof(double) || _property.PropertyType == typeof(decimal)) txtValue.DecimalPlaces = 2;
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
                ((INotifyPropertyChanged)data).PropertyChanged -= NumericControl_PropertyChanged;
                ((INotifyPropertyChanged)data).PropertyChanged += NumericControl_PropertyChanged;
            }
            _data = data;
            txtValue.Value = Convert.ToDecimal(_property.GetValue(data));
            txtValue.ValueChanged -= TxtValue_ValueChanged;
            txtValue.ValueChanged += TxtValue_ValueChanged;
        }

        private void NumericControl_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == _property.Name)
            {
                Action act = () => txtValue.Value = Convert.ToDecimal(_property.GetValue(_data));
                if (InvokeRequired)
                    Invoke(act);
                else
                    act();
            }
        }

        private void TxtValue_ValueChanged(object sender, EventArgs e)
        {
            _property.SetValue(_data, Convert.ChangeType(txtValue.Value, _property.PropertyType));
        }
    }
}
